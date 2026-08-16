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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using System.Security.Cryptography;
using MiNET.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiNET.Utils.Cryptography
{
	/// <summary>
	///     The identity a real Xbox Live account logs in with, and the key that proves it belongs to
	///     this connection.
	/// </summary>
	public sealed class XboxIdentity
	{
		/// <summary>
		///     The keypair the authorization service named in the token's cpk claim. The login packet
		///     and the encryption handshake must both use this key: the token says who you are, the
		///     key proves the connection is yours.
		/// </summary>
		public ECDsa IdentityKey { get; init; }

		/// <summary>The signed login token, for the authentication envelope's Token field.</summary>
		public string LoginToken { get; init; }

		public string DisplayName { get; init; }
		public string Xuid { get; init; }
		public DateTimeOffset ExpiresAt { get; init; }
	}

	/// <summary>
	///     Obtains a real Bedrock login token by walking Microsoft account, Xbox Live, PlayFab and the
	///     franchise authorization service. The mirror image of <see cref="FranchiseTokenValidator" />,
	///     which checks the token this produces.
	///     <para>
	///         See <c>docs/bedrock-authentication.md</c> for the flow, the history and why the cpk
	///         binding matters.
	///     </para>
	/// </summary>
	public class XboxAuthentication
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(XboxAuthentication));

		// The Minecraft PE client id, which is what makes Xbox issue tokens a Minecraft title accepts.
		private const string ClientId = "00000000441cc96b";
		private const string XboxScope = "service::user.auth.xboxlive.com::MBI_SSL";

		private const string DeviceCodeStart = "https://login.live.com/oauth20_connect.srf";
		private const string TokenEndpoint = "https://login.live.com/oauth20_token.srf";
		private const string UserAuth = "https://user.auth.xboxlive.com/user/authenticate";
		private const string DeviceAuth = "https://device.auth.xboxlive.com/device/authenticate";
		private const string TitleAuth = "https://title.auth.xboxlive.com/title/authenticate";
		private const string XstsAuth = "https://xsts.auth.xboxlive.com/xsts/authorize";
		private const string PlayFabRelyingParty = "rp://playfabapi.com/";
		private const string DiscoveryUri = "https://client.discovery.minecraft-services.net/api/v1.0/discovery/MinecraftPE/builds/";

		private readonly IXboxSessionStore _store;
		private readonly HttpClient _http;

		private ECDsa _authKey;
		private ECDsa _identityKey;
		private string _proofX, _proofY;

		/// <summary>
		///     Raised when a human has to visit a URL and type a code. Only fires when there is no
		///     usable saved session, so in normal operation it never fires at all.
		/// </summary>
		public event Action<string, string> DeviceCodeRequired;

		public XboxAuthentication(IXboxSessionStore store = null, HttpClient httpClient = null)
		{
			_store = store ?? new ProtectedFileSessionStore();
			_http = httpClient ?? new HttpClient {Timeout = TimeSpan.FromSeconds(60)};
		}

		public async Task<XboxIdentity> AuthenticateAsync(CancellationToken cancellationToken = default)
		{
			XboxSession saved = _store.Load();

			_identityKey = saved?.IdentityPrivateKey != null ? RestoreKey(saved.IdentityPrivateKey) : GenerateKey(Curves.P384);

			// The device id is only usable with the key it was registered against, so a session
			// missing either half gets a new pair rather than an orphaned id.
			bool devicePairIntact = saved?.AuthPrivateKey != null && saved.DeviceId != null;
			_authKey = devicePairIntact ? RestoreKey(saved.AuthPrivateKey) : GenerateKey(Curves.P256);
			SetProofKey();

			string deviceId = devicePairIntact ? saved.DeviceId : Guid.NewGuid().ToString();
			string refreshToken = saved?.RefreshToken;

			string accessToken = refreshToken == null ? null : await TryRefresh(refreshToken, cancellationToken);
			if (accessToken == null)
			{
				(accessToken, refreshToken) = await DeviceCodeLogin(cancellationToken);
			}
			else if (saved != null)
			{
				refreshToken = saved.RefreshToken;
			}

			JObject userToken = await UserToken(accessToken, cancellationToken);

			JObject deviceToken;
			try
			{
				deviceToken = await DeviceToken(deviceId, cancellationToken);
			}
			catch (IOException e) when (e.Message.Contains("403"))
			{
				// Xbox refuses a device id presented with a proof key other than the one that
				// registered it, and says so only with a bare 403. The pair is unusable, so take a
				// new one. The user token was signed with the old key and has to be redone.
				Log.Info("Xbox rejected the stored device id, registering a new device");

				deviceId = Guid.NewGuid().ToString();
				_authKey = GenerateKey(Curves.P256);
				SetProofKey();

				userToken = await UserToken(accessToken, cancellationToken);
				deviceToken = await DeviceToken(deviceId, cancellationToken);
			}

			JObject titleToken = await TitleToken(deviceToken, accessToken, cancellationToken);

			Save(refreshToken, deviceId);

			JObject xsts = await Xsts(PlayFabRelyingParty, userToken, deviceToken, titleToken, cancellationToken);

			(string authUri, string playFabTitleId) = await Discover(cancellationToken);

			string sessionTicket = await PlayFabLogin(playFabTitleId, xsts, cancellationToken);
			string mcToken = await StartSession(authUri, playFabTitleId, sessionTicket, cancellationToken);
			string loginToken = await StartMultiplayerSession(authUri, mcToken, cancellationToken);

			var claims = JObject.Parse(Encoding.UTF8.GetString(Base64UrlDecode(loginToken.Split('.')[1])));

			string cpk = (string) claims["cpk"];
			if (cpk != PublicKeyBase64(_identityKey))
			{
				// Without this the login is worthless: the server keys its handshake on cpk, so a
				// token naming a key we do not hold cannot complete a connection.
				throw new IOException("The issued token names a different public key than the one we sent");
			}

			var identity = new XboxIdentity
			{
				IdentityKey = _identityKey,
				LoginToken = loginToken,
				DisplayName = (string) claims["xname"],
				Xuid = (string) claims["xid"],
				ExpiresAt = DateTimeOffset.FromUnixTimeSeconds((long?) claims["exp"] ?? 0)
			};

			Log.Info($"Authenticated as {identity.DisplayName} (xuid {identity.Xuid}), token valid until {identity.ExpiresAt:u}");
			return identity;
		}

		private async Task<string> TryRefresh(string refreshToken, CancellationToken cancellationToken)
		{
			JObject response = await PostForm(TokenEndpoint, new Dictionary<string, string>
			{
				["client_id"] = ClientId,
				["grant_type"] = "refresh_token",
				// Required. Without it the endpoint refuses the grant.
				["scope"] = XboxScope,
				["refresh_token"] = refreshToken
			}, cancellationToken, allowError: true);

			string accessToken = (string) response["access_token"];
			if (accessToken == null) Log.Info($"Saved Xbox session could not be refreshed ({response["error"]}), signing in again");

			return accessToken;
		}

		private async Task<(string accessToken, string refreshToken)> DeviceCodeLogin(CancellationToken cancellationToken)
		{
			JObject start = await PostForm($"{DeviceCodeStart}?client_id={ClientId}", new Dictionary<string, string>
			{
				["client_id"] = ClientId,
				["scope"] = XboxScope,
				["response_type"] = "device_code"
			}, cancellationToken);

			string verificationUri = (string) start["verification_uri"];
			string userCode = (string) start["user_code"];

			Log.Warn($"Xbox sign-in required: open {verificationUri} and enter {userCode}");
			DeviceCodeRequired?.Invoke(verificationUri, userCode);

			var interval = TimeSpan.FromSeconds((int?) start["interval"] ?? 5);
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await Task.Delay(interval, cancellationToken);

				JObject poll = await PostForm($"{TokenEndpoint}?client_id={ClientId}", new Dictionary<string, string>
				{
					["client_id"] = ClientId,
					["device_code"] = (string) start["device_code"],
					["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
				}, cancellationToken, allowError: true);

				string error = (string) poll["error"];
				if (error == null) return ((string) poll["access_token"], (string) poll["refresh_token"]);
				if (error != "authorization_pending") throw new IOException($"Xbox sign-in failed: {error} {poll["error_description"]}");
			}
		}

		private Task<JObject> UserToken(string accessToken, CancellationToken cancellationToken) =>
			SignedPost(UserAuth, new
			{
				RelyingParty = "http://auth.xboxlive.com",
				TokenType = "JWT",
				Properties = new Dictionary<string, object>
				{
					["AuthMethod"] = "RPS",
					["RpsTicket"] = "t=" + accessToken,
					["SiteName"] = "user.auth.xboxlive.com",
					["ProofKey"] = ProofKey()
				}
			}, cancellationToken);

		private Task<JObject> DeviceToken(string deviceId, CancellationToken cancellationToken) =>
			SignedPost(DeviceAuth, new
			{
				RelyingParty = "http://auth.xboxlive.com",
				TokenType = "JWT",
				Properties = new Dictionary<string, object>
				{
					["AuthMethod"] = "ProofOfPossession",
					["Id"] = deviceId,
					["DeviceType"] = "Nintendo",
					["SerialNumber"] = Guid.NewGuid().ToString(),
					["Version"] = "0.0.0.0",
					["ProofKey"] = ProofKey()
				}
			}, cancellationToken);

		private Task<JObject> TitleToken(JObject deviceToken, string accessToken, CancellationToken cancellationToken) =>
			SignedPost(TitleAuth, new
			{
				RelyingParty = "http://auth.xboxlive.com",
				TokenType = "JWT",
				Properties = new Dictionary<string, object>
				{
					["AuthMethod"] = "RPS",
					["DeviceToken"] = (string) deviceToken["Token"],
					["RpsTicket"] = "t=" + accessToken,
					["SiteName"] = "user.auth.xboxlive.com",
					["ProofKey"] = ProofKey()
				}
			}, cancellationToken);

		/// <summary>
		///     An XSTS token is encrypted for one relying party, so each downstream service needs its
		///     own. Reusing one elsewhere fails with "Unable to decrypt token body", which names
		///     neither the token nor the relying party.
		/// </summary>
		private Task<JObject> Xsts(string relyingParty, JObject userToken, JObject deviceToken, JObject titleToken, CancellationToken cancellationToken) =>
			SignedPost(XstsAuth, new
			{
				RelyingParty = relyingParty,
				TokenType = "JWT",
				Properties = new Dictionary<string, object>
				{
					["UserTokens"] = new[] {(string) userToken["Token"]},
					["DeviceToken"] = (string) deviceToken["Token"],
					["TitleToken"] = (string) titleToken["Token"],
					["SandboxId"] = "RETAIL",
					["ProofKey"] = ProofKey()
				}
			}, cancellationToken, withClientVersion: true);

		/// <summary>
		///     Service endpoints come from discovery rather than constants: it is keyed by game build,
		///     so hardcoded URLs are a thing that breaks on a protocol bump.
		/// </summary>
		private async Task<(string authUri, string playFabTitleId)> Discover(CancellationToken cancellationToken)
		{
			string json = await _http.GetStringAsync(DiscoveryUri + McpeProtocolInfo.GameVersion, cancellationToken);
			var auth = JObject.Parse(json)["result"]?["serviceEnvironments"]?["auth"]?["prod"];

			string authUri = ((string) auth?["serviceUri"])?.TrimEnd('/');
			string titleId = (string) auth?["playfabTitleId"];

			if (authUri == null || titleId == null) throw new IOException("Discovery returned no auth service for this build");

			return (authUri, titleId);
		}

		private async Task<string> PlayFabLogin(string titleId, JObject xsts, CancellationToken cancellationToken)
		{
			string userHash = (string) xsts["DisplayClaims"]?["xui"]?[0]?["uhs"];

			JObject response = await PostJson($"https://{titleId}.playfabapi.com/Client/LoginWithXbox", new
			{
				CreateAccount = true,
				TitleId = titleId,
				XboxToken = $"XBL3.0 x={userHash};{(string) xsts["Token"]}"
			}, cancellationToken);

			return (string) response["data"]?["SessionTicket"] ?? throw new IOException("PlayFab returned no session ticket");
		}

		private async Task<string> StartSession(string authUri, string playFabTitleId, string sessionTicket, CancellationToken cancellationToken)
		{
			JObject response = await PostJson($"{authUri}/api/v1.0/session/start", new
			{
				device = new
				{
					applicationType = "MinecraftPE",
					capabilities = Array.Empty<string>(),
					gameVersion = McpeProtocolInfo.GameVersion,
					id = Guid.NewGuid().ToString(),
					isPreview = false,
					memory = "8589934592",
					platform = "Windows10",
					playFabTitleId,
					storePlatform = "uwp.store",
					treatmentOverrides = (object) null,
					type = "Windows10"
				},
				user = new
				{
					language = "en",
					languageCode = "en-US",
					regionCode = "US",
					token = sessionTicket,
					tokenType = "PlayFab"
				}
			}, cancellationToken);

			return (string) response["result"]?["authorizationHeader"] ?? throw new IOException("Authorization service returned no MCToken");
		}

		private async Task<string> StartMultiplayerSession(string authUri, string mcToken, CancellationToken cancellationToken)
		{
			JObject response = await PostJson($"{authUri}/api/v1.0/multiplayer/session/start",
				new {publicKey = PublicKeyBase64(_identityKey)}, cancellationToken, mcToken);

			return (string) response["result"]?["signedToken"] ?? throw new IOException("Authorization service returned no login token");
		}

		private void Save(string refreshToken, string deviceId) => _store.Save(new XboxSession
		{
			RefreshToken = refreshToken,
			DeviceId = deviceId,
			AuthPrivateKey = PrivateKeyBase64(_authKey),
			IdentityPrivateKey = PrivateKeyBase64(_identityKey)
		});

		private object ProofKey() => new {crv = "P-256", alg = "ES256", use = "sig", kty = "EC", x = _proofX, y = _proofY};

		private void SetProofKey()
		{
			ECParameters pub = _authKey.ExportParameters(false);
			_proofX = Base64Url(pub.Q.X);
			_proofY = Base64Url(pub.Q.Y);
		}

		/// <summary>
		///     Xbox requires a signature over a canonical buffer of method, path, authorization header
		///     and body, signed with the proof key whose public half rides in the same request.
		/// </summary>
		private void Sign(HttpRequestMessage request, byte[] body)
		{
			long time = DateTime.UtcNow.ToFileTimeUtc();
			byte[] stamp = new byte[8];
			for (int i = 0; i < 8; i++) stamp[i] = (byte) (time >> ((7 - i) * 8));

			using var buffer = new MemoryStream();
			buffer.Write(new byte[] {0, 0, 0, 1, 0});
			buffer.Write(stamp);
			buffer.WriteByte(0);
			buffer.Write(Encoding.UTF8.GetBytes("POST"));
			buffer.WriteByte(0);
			buffer.Write(Encoding.UTF8.GetBytes(request.RequestUri!.PathAndQuery));
			buffer.WriteByte(0);
			buffer.Write(Encoding.UTF8.GetBytes(request.Headers.Authorization?.ToString() ?? ""));
			buffer.WriteByte(0);
			buffer.Write(body);
			buffer.WriteByte(0);

			// Xbox calls this PLAIN-ECDSA: the fixed-width r||s pair, not the ASN.1 DER default,
			// which is exactly what IeeeP1363FixedFieldConcatenation produces.
			byte[] input = buffer.ToArray();
			byte[] signature = _authKey.SignData(input, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

			using var final = new MemoryStream();
			final.Write(new byte[] {0, 0, 0, 1});
			final.Write(stamp);
			final.Write(signature);

			request.Headers.Add("Signature", Convert.ToBase64String(final.ToArray()));
		}

		private async Task<JObject> SignedPost(string url, object body, CancellationToken cancellationToken, bool withClientVersion = false)
		{
			byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));

			using var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Accept.ParseAdd("application/json");
			if (withClientVersion)
			{
				request.Headers.Add("User-Agent", "MCPE/Android");
				request.Headers.Add("Client-Version", McpeProtocolInfo.ProtocolVersion.ToString());
			}

			request.Content = new ByteArrayContent(json);
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			Sign(request, json);

			return await Send(request, url, cancellationToken);
		}

		private async Task<JObject> PostJson(string url, object body, CancellationToken cancellationToken, string authorization = null)
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Accept.ParseAdd("application/json");
			request.Headers.Add("User-Agent", "libhttpclient/1.0.0.0");
			request.Headers.Add("Client-Version", McpeProtocolInfo.ProtocolVersion.ToString());
			if (authorization != null) request.Headers.TryAddWithoutValidation("Authorization", authorization);

			request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8);
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

			return await Send(request, url, cancellationToken);
		}

		private async Task<JObject> Send(HttpRequestMessage request, string url, CancellationToken cancellationToken)
		{
			using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
			string text = await response.Content.ReadAsStringAsync(cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				// Xbox reports the reason in headers rather than the body, and often sends no body.
				string headers = string.Join(", ", response.Headers
					.Where(h => h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase) || h.Key == "WWW-Authenticate")
					.Select(h => $"{h.Key}={string.Join('|', h.Value)}"));

				throw new IOException($"{url} returned {(int) response.StatusCode} {response.ReasonPhrase}; headers[{headers}]; body[{Truncate(text)}]");
			}

			return JObject.Parse(text);
		}

		private async Task<JObject> PostForm(string url, Dictionary<string, string> form, CancellationToken cancellationToken, bool allowError = false)
		{
			using HttpResponseMessage response = await _http.PostAsync(url, new FormUrlEncodedContent(form), cancellationToken);
			string text = await response.Content.ReadAsStringAsync(cancellationToken);

			if (!response.IsSuccessStatusCode && !allowError)
				throw new IOException($"{url} returned {(int) response.StatusCode}: {Truncate(text)}");

			return JObject.Parse(text);
		}

		private static class Curves
		{
			public const string P256 = "1.2.840.10045.3.1.7";
			public const string P384 = "1.3.132.0.34";
		}

		private static ECDsa GenerateKey(string curveOid)
		{
			return ECDsa.Create(ECCurve.CreateFromValue(curveOid));
		}

		/// <summary>Rebuilds a key from its private half. The public point is derivable: Q = G * d.</summary>
		private static ECDsa RestoreKey(string base64Pkcs8)
		{
			var key = ECDsa.Create();
			key.ImportPkcs8PrivateKey(Convert.FromBase64String(base64Pkcs8), out _);

			return key;
		}

		private static string PrivateKeyBase64(ECDsa key) =>
			Convert.ToBase64String(key.ExportPkcs8PrivateKey());

		public static string PublicKeyBase64(ECDsa key) =>
			Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());

		private static string Base64Url(byte[] data) =>
			Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

		private static byte[] Base64UrlDecode(string value)
		{
			string s = value.Replace('-', '+').Replace('_', '/');
			return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
		}

		private static string Truncate(string value) =>
			value == null ? "" : value.Length <= 300 ? value : value.Substring(0, 300) + "...";
	}
}
