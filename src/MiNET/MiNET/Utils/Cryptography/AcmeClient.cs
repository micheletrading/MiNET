#region LICENSE

// The contents of this file are subject to the Common Public Attribution
// License Version 1.0. (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// https://github.com/NiclasOlofsson/MiNET/blob/master/LICENSE.
// The License is based on the Mozilla Public License Version 1.1, but Sections 14
// and 15 have been added to cover use of software over a computer network and
// provide for limited attribution for the Original Developer. In addition, Exhibit A has
// been modified to be consistent with Exhibit B.
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
// the specific language governing rights and limitations under the License.
//
// The Original Code is MiNET.
//
// The Original Developer is the Initial Developer.  The Initial Developer of
// the Original Code is Niclas Olofsson.
//
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2026 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using log4net;

namespace MiNET.Utils.Cryptography
{
	/// <summary>
	///     A from-scratch ACME (RFC 8555) client on BCL crypto only: <see cref="ECDsa" /> signs the
	///     requests, <see cref="Base64Url" /> encodes them, HttpClient carries them. It exists
	///     because BouncyCastle is leaving this repository entirely and every published .NET ACME
	///     package drags some edition of it in. Scope is exactly what
	///     <see cref="AcmeCertificateManager" /> needs: one account, one dns identifier per order,
	///     http-01, finalize, download. The protocol itself is small: every POST is a flattened
	///     JWS over a server-issued nonce, new-account signs with the bare public key (jwk), and
	///     everything after signs with the account URL (kid) that new-account returned.
	/// </summary>
	public class AcmeClient : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(AcmeClient));

		private readonly Uri _directoryUri;
		private readonly ECDsa _accountKey;
		private readonly HttpClient _http;

		private string _newNonceUrl;
		private string _newAccountUrl;
		private string _newOrderUrl;
		private string _nonce;

		/// <summary>The account's URL at the CA, assigned by new-account: the kid every later request signs with.</summary>
		public string AccountUrl { get; private set; }

		/// <summary>Takes ownership of the key: the account key IS the ACME identity and lives exactly as long as something can sign with it.</summary>
		public AcmeClient(Uri directoryUri, ECDsa accountKey, HttpClient http = null)
		{
			_directoryUri = directoryUri;
			_accountKey = accountKey;
			_http = http ?? new HttpClient {Timeout = TimeSpan.FromSeconds(30)};
		}

		public void Dispose()
		{
			_http.Dispose();
			_accountKey.Dispose();
		}

		/// <summary>Fetches the directory document, the only unauthenticated request in the protocol.</summary>
		public async Task InitializeAsync()
		{
			using HttpResponseMessage response = await _http.GetAsync(_directoryUri);
			response.EnsureSuccessStatusCode();
			using JsonDocument directory = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			_newNonceUrl = directory.RootElement.GetProperty("newNonce").GetString();
			_newAccountUrl = directory.RootElement.GetProperty("newAccount").GetString();
			_newOrderUrl = directory.RootElement.GetProperty("newOrder").GetString();
		}

		/// <summary>
		///     Creates the account, or finds the existing one: posting new-account with a key the CA
		///     has seen before returns that account rather than a duplicate, so this is idempotent
		///     and the persisted key is the whole identity.
		/// </summary>
		public async Task EnsureAccountAsync(string contactEmail)
		{
			var payload = new Dictionary<string, object> {["termsOfServiceAgreed"] = true};
			if (contactEmail != null) payload["contact"] = new[] {$"mailto:{contactEmail}"};

			using HttpResponseMessage response = await PostJwsAsync(_newAccountUrl, JsonSerializer.Serialize(payload), useJwk: true);
			AccountUrl = response.Headers.Location?.ToString() ?? throw new IOException("ACME new-account returned no Location; there is no kid to sign with");
		}

		/// <summary>One dns identifier, one order.</summary>
		public async Task<AcmeOrder> CreateOrderAsync(string domain)
		{
			string payload = JsonSerializer.Serialize(new {identifiers = new[] {new {type = "dns", value = domain}}});

			using HttpResponseMessage response = await PostJwsAsync(_newOrderUrl, payload);
			string orderUrl = response.Headers.Location?.ToString();
			using JsonDocument order = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			return new AcmeOrder(
				orderUrl,
				order.RootElement.GetProperty("finalize").GetString(),
				order.RootElement.GetProperty("authorizations").EnumerateArray().Select(a => a.GetString()).ToArray());
		}

		/// <summary>The http-01 challenge of an authorization: its URL to trigger, its token to serve.</summary>
		public async Task<(string challengeUrl, string token)> GetHttpChallengeAsync(string authorizationUrl)
		{
			using HttpResponseMessage response = await PostJwsAsync(authorizationUrl, payload: "");
			using JsonDocument authorization = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			foreach (JsonElement challenge in authorization.RootElement.GetProperty("challenges").EnumerateArray())
			{
				if (challenge.GetProperty("type").GetString() != "http-01") continue;

				return (challenge.GetProperty("url").GetString(), challenge.GetProperty("token").GetString());
			}

			throw new IOException($"The authorization at {authorizationUrl} offers no http-01 challenge");
		}

		/// <summary>The exact body the validator must read back: token, dot, account key thumbprint.</summary>
		public string KeyAuthorization(string token) => token + "." + Base64UrlEncode(JwkThumbprint(_accountKey));

		/// <summary>Tells the CA to validate now. The empty JSON object is the protocol's "go".</summary>
		public async Task TriggerChallengeAsync(string challengeUrl)
		{
			using HttpResponseMessage response = await PostJwsAsync(challengeUrl, payload: "{}");
			await response.Content.ReadAsStringAsync();
		}

		/// <summary>The challenge's current status, with the CA's error detail when it has one.</summary>
		public async Task<(string status, string error)> GetChallengeStatusAsync(string challengeUrl)
		{
			using HttpResponseMessage response = await PostJwsAsync(challengeUrl, payload: "");
			using JsonDocument challenge = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			string status = challenge.RootElement.GetProperty("status").GetString();
			string error = challenge.RootElement.TryGetProperty("error", out JsonElement problem) && problem.TryGetProperty("detail", out JsonElement detail)
				? detail.GetString()
				: null;

			return (status, error);
		}

		/// <summary>
		///     Submits the CSR, waits for the order to become valid, and downloads the certificate
		///     chain as PEM. The order passes through processing on its way; only invalid is a
		///     failure, everything else is patience.
		/// </summary>
		public async Task<string> FinalizeAsync(AcmeOrder order, byte[] csrDer, TimeSpan timeout)
		{
			string payload = JsonSerializer.Serialize(new {csr = Base64UrlEncode(csrDer)});
			using (HttpResponseMessage response = await PostJwsAsync(order.FinalizeUrl, payload))
			{
				await response.Content.ReadAsStringAsync();
			}

			DateTime deadline = DateTime.UtcNow + timeout;
			while (true)
			{
				using HttpResponseMessage response = await PostJwsAsync(order.OrderUrl, payload: "");
				using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

				string status = document.RootElement.GetProperty("status").GetString();
				if (status == "valid")
				{
					string certificateUrl = document.RootElement.GetProperty("certificate").GetString();
					using HttpResponseMessage certificate = await PostJwsAsync(certificateUrl, payload: "", accept: "application/pem-certificate-chain");
					return await certificate.Content.ReadAsStringAsync();
				}

				if (status == "invalid") throw new IOException("ACME order became invalid after finalize");
				if (DateTime.UtcNow > deadline) throw new TimeoutException($"ACME order did not become valid within {timeout.TotalSeconds:0}s (status {status})");

				await Task.Delay(1000);
			}
		}

		/// <summary>
		///     Signs and posts one request. Payload "" is POST-as-GET (RFC 8555's authenticated
		///     read). Every response replenishes the nonce; a badNonce rejection is retried once
		///     with a fresh one, which is the protocol's own prescription for it.
		/// </summary>
		private async Task<HttpResponseMessage> PostJwsAsync(string url, string payload, bool useJwk = false, string accept = null)
		{
			for (int attempt = 0; ; attempt++)
			{
				string body = SignFlattenedJws(url, payload, await TakeNonceAsync(), useJwk);

				var content = new StringContent(body);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/jose+json");

				var request = new HttpRequestMessage(HttpMethod.Post, url) {Content = content};
				if (accept != null) request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

				HttpResponseMessage response = await _http.SendAsync(request);
				if (response.Headers.TryGetValues("Replay-Nonce", out IEnumerable<string> nonces)) _nonce = nonces.FirstOrDefault();

				if (response.IsSuccessStatusCode) return response;

				string problemBody = await response.Content.ReadAsStringAsync();
				response.Dispose();

				(string type, string detail) = ParseProblem(problemBody);
				if (type == "urn:ietf:params:acme:error:badNonce" && attempt == 0)
				{
					Log.Debug($"ACME nonce went stale at {url}; retrying with a fresh one");
					continue;
				}

				throw new IOException($"ACME request to {url} failed: {type ?? "no problem type"}: {detail ?? problemBody}");
			}
		}

		private static (string type, string detail) ParseProblem(string body)
		{
			try
			{
				using JsonDocument problem = JsonDocument.Parse(body);
				return (
					problem.RootElement.TryGetProperty("type", out JsonElement type) ? type.GetString() : null,
					problem.RootElement.TryGetProperty("detail", out JsonElement detail) ? detail.GetString() : null);
			}
			catch (JsonException)
			{
				return (null, null);
			}
		}

		/// <summary>Nonces are single-use; the stash holds the one the latest response replenished, and the dedicated endpoint refills an empty stash.</summary>
		private async Task<string> TakeNonceAsync()
		{
			string nonce = _nonce;
			_nonce = null;
			if (nonce != null) return nonce;

			using HttpResponseMessage response = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Head, _newNonceUrl));
			return response.Headers.TryGetValues("Replay-Nonce", out IEnumerable<string> nonces)
				? nonces.First()
				: throw new IOException("The ACME new-nonce endpoint returned no Replay-Nonce");
		}

		/// <summary>
		///     One flattened-JSON JWS: ES256 over ASCII(base64url(protected) "." base64url(payload)),
		///     signature in IEEE P1363 form, which is what <see cref="ECDsa.SignData(byte[], HashAlgorithmName)" />
		///     emits natively. Internal for the tests that pin this format.
		/// </summary>
		internal string SignFlattenedJws(string url, string payload, string nonce, bool useJwk)
		{
			var header = new Dictionary<string, object>
			{
				["alg"] = "ES256",
				["nonce"] = nonce,
				["url"] = url,
			};
			if (useJwk) header["jwk"] = Jwk(_accountKey);
			else header["kid"] = AccountUrl ?? throw new InvalidOperationException("No account URL yet; only new-account may sign with the bare jwk");

			string protectedB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
			string payloadB64 = payload.Length == 0 ? "" : Base64UrlEncode(Encoding.UTF8.GetBytes(payload));

			byte[] signature = _accountKey.SignData(Encoding.ASCII.GetBytes(protectedB64 + "." + payloadB64), HashAlgorithmName.SHA256);

			return JsonSerializer.Serialize(new
			{
				@protected = protectedB64,
				payload = payloadB64,
				signature = Base64UrlEncode(signature),
			});
		}

		private static Dictionary<string, string> Jwk(ECDsa key)
		{
			ECParameters parameters = key.ExportParameters(false);
			return new Dictionary<string, string>
			{
				["kty"] = "EC",
				["crv"] = "P-256",
				["x"] = Base64UrlEncode(parameters.Q.X),
				["y"] = Base64UrlEncode(parameters.Q.Y),
			};
		}

		/// <summary>
		///     RFC 7638: SHA-256 over the JWK's required members only, in lexicographic order, no
		///     whitespace. Serialized by hand because the canonical form IS the contract; a JSON
		///     library's member order is not part of any spec.
		/// </summary>
		internal static byte[] JwkThumbprint(ECDsa key)
		{
			ECParameters parameters = key.ExportParameters(false);
			string canonical = "{\"crv\":\"P-256\",\"kty\":\"EC\""
							+ $",\"x\":\"{Base64UrlEncode(parameters.Q.X)}\""
							+ $",\"y\":\"{Base64UrlEncode(parameters.Q.Y)}\"}}";

			return SHA256.HashData(Encoding.ASCII.GetBytes(canonical));
		}

		internal static string Base64UrlEncode(byte[] data) => Base64Url.EncodeToString(data);

		internal static byte[] Base64UrlDecode(string data) => Base64Url.DecodeFromChars(data);
	}

	/// <summary>The three URLs of an order in flight; the CA's state machine lives behind them.</summary>
	public record AcmeOrder(string OrderUrl, string FinalizeUrl, IReadOnlyList<string> AuthorizationUrls);
}