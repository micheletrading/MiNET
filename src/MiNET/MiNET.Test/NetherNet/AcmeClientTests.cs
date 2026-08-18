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
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Utils.Cryptography;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     The from-scratch ACME (RFC 8555) client, BCL crypto only: it exists so BouncyCastle can
	///     leave the repository entirely, so nothing here may touch it. The cryptographic pieces
	///     (RFC 7638 JWK thumbprint canonicalization, ES256 flattened JWS, the CSR) are pinned as
	///     units, and the account flow runs against an in-test ACME directory so the wire shape,
	///     nonce discipline and jwk-then-kid switch are proven without spending a real CA's
	///     rate limits.
	/// </summary>
	[TestClass]
	public class AcmeClientTests
	{
		[TestMethod]
		public void JwkThumbprint_FollowsRfc7638Canonicalization()
		{
			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			ECParameters parameters = key.ExportParameters(false);

			// RFC 7638: SHA-256 over the JWK's required members only, lexicographic order,
			// no whitespace. Built independently here, so the helper cannot agree by accident.
			string expectedJson = "{\"crv\":\"P-256\",\"kty\":\"EC\""
								+ $",\"x\":\"{AcmeClient.Base64UrlEncode(parameters.Q.X)}\""
								+ $",\"y\":\"{AcmeClient.Base64UrlEncode(parameters.Q.Y)}\"}}";
			byte[] expected = SHA256.HashData(Encoding.ASCII.GetBytes(expectedJson));

			CollectionAssert.AreEqual(expected, AcmeClient.JwkThumbprint(key));
		}

		[TestMethod]
		public void KeyAuthorization_IsTokenDotThumbprint()
		{
			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			using var client = new AcmeClient(new Uri("https://acme.invalid/directory"), key);

			string expected = "some-token." + AcmeClient.Base64UrlEncode(AcmeClient.JwkThumbprint(key));

			Assert.AreEqual(expected, client.KeyAuthorization("some-token"));
		}

		[TestMethod]
		public void FlattenedJws_CarriesHeaderAndVerifiableSignature()
		{
			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			using var client = new AcmeClient(new Uri("https://acme.invalid/directory"), key);

			string jws = client.SignFlattenedJws("https://acme.invalid/new-account", "{\"a\":1}", "nonce-123", useJwk: true);

			using JsonDocument document = JsonDocument.Parse(jws);
			string protectedB64 = document.RootElement.GetProperty("protected").GetString();
			string payloadB64 = document.RootElement.GetProperty("payload").GetString();
			byte[] signature = AcmeClient.Base64UrlDecode(document.RootElement.GetProperty("signature").GetString());

			using JsonDocument header = JsonDocument.Parse(AcmeClient.Base64UrlDecode(protectedB64));
			Assert.AreEqual("ES256", header.RootElement.GetProperty("alg").GetString());
			Assert.AreEqual("nonce-123", header.RootElement.GetProperty("nonce").GetString());
			Assert.AreEqual("https://acme.invalid/new-account", header.RootElement.GetProperty("url").GetString());
			Assert.AreEqual("P-256", header.RootElement.GetProperty("jwk").GetProperty("crv").GetString());
			Assert.AreEqual("{\"a\":1}", Encoding.UTF8.GetString(AcmeClient.Base64UrlDecode(payloadB64)));

			// ES256 over ASCII(protected "." payload) in IEEE P1363 form, verified with the same
			// key: proves the signing input construction and the signature format together.
			byte[] signingInput = Encoding.ASCII.GetBytes(protectedB64 + "." + payloadB64);
			Assert.IsTrue(key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256));
		}

		[TestMethod]
		public void CertificateRequest_CarriesCommonNameAndSan()
		{
			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

			byte[] der = AcmeCertificateManager.BuildCertificateRequest("yodamine.test", key);

			CertificateRequest loaded = CertificateRequest.LoadSigningRequest(
				der, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

			Assert.AreEqual("CN=yodamine.test", loaded.SubjectName.Name);
			bool sanCarriesDomain = false;
			foreach (X509Extension extension in loaded.CertificateExtensions)
			{
				if (extension is X509SubjectAlternativeNameExtension san)
				{
					foreach (string name in san.EnumerateDnsNames())
					{
						if (name == "yodamine.test") sanCarriesDomain = true;
					}
				}
			}
			Assert.IsTrue(sanCarriesDomain, "Let's Encrypt validates the SAN, not the CN; the CSR must carry the domain there");
		}

		/// <summary>
		///     A minimal in-test ACME directory: directory document, nonce endpoint, and a
		///     new-account endpoint that validates the JWS it receives the way a CA would
		///     (signature included), then hands back an account URL. Proves the client's wire
		///     behavior end to end without a network.
		/// </summary>
		[TestMethod]
		public async Task AccountFlow_SendsWellFormedSignedRequests()
		{
			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

			// HttpListener needs an explicit free port; ask the OS for one and accept the tiny
			// bind race, the same pattern the listener tests use.
			int port;
			{
				var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
				probe.Start();
				port = ((IPEndPoint) probe.LocalEndpoint).Port;
				probe.Stop();
			}
			string baseUrl = $"http://127.0.0.1:{port}/";

			using var server = new HttpListener();
			server.Prefixes.Add(baseUrl);
			server.Start();

			string issuedNonce = "nonce-0";
			string capturedProtected = null;
			string capturedPayload = null;
			bool signatureValid = false;

			Task serving = Task.Run(async () =>
			{
				while (server.IsListening)
				{
					HttpListenerContext context;
					try
					{
						context = await server.GetContextAsync();
					}
					catch (Exception)
					{
						return; // listener stopped
					}

					string path = context.Request.Url.AbsolutePath;
					context.Response.Headers["Replay-Nonce"] = issuedNonce;

					if (path == "/directory")
					{
						byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
						{
							newNonce = baseUrl + "new-nonce",
							newAccount = baseUrl + "new-account",
							newOrder = baseUrl + "new-order",
						}));
						context.Response.StatusCode = 200;
						await context.Response.OutputStream.WriteAsync(body);
					}
					else if (path == "/new-nonce")
					{
						context.Response.StatusCode = 200;
					}
					else if (path == "/new-account")
					{
						using var reader = new System.IO.StreamReader(context.Request.InputStream);
						using JsonDocument jws = JsonDocument.Parse(await reader.ReadToEndAsync());
						capturedProtected = jws.RootElement.GetProperty("protected").GetString();
						capturedPayload = jws.RootElement.GetProperty("payload").GetString();
						byte[] signature = AcmeClient.Base64UrlDecode(jws.RootElement.GetProperty("signature").GetString());
						byte[] signingInput = Encoding.ASCII.GetBytes(capturedProtected + "." + capturedPayload);
						signatureValid = key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256);

						context.Response.StatusCode = 201;
						context.Response.Headers["Location"] = baseUrl + "account/1";
					}
					else
					{
						context.Response.StatusCode = 404;
					}

					context.Response.Close();
				}
			});

			try
			{
				using var client = new AcmeClient(new Uri(baseUrl + "directory"), key);
				await client.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(10));
				await client.EnsureAccountAsync("test@yodamine.test").WaitAsync(TimeSpan.FromSeconds(10));

				Assert.AreEqual(baseUrl + "account/1", client.AccountUrl, "the Location of new-account is the kid for every later request");
				Assert.IsTrue(signatureValid, "the CA verifies the JWS signature; ours must hold up");

				using JsonDocument header = JsonDocument.Parse(AcmeClient.Base64UrlDecode(capturedProtected));
				Assert.AreEqual("ES256", header.RootElement.GetProperty("alg").GetString());
				Assert.AreEqual(issuedNonce, header.RootElement.GetProperty("nonce").GetString());
				Assert.AreEqual(baseUrl + "new-account", header.RootElement.GetProperty("url").GetString());
				Assert.IsTrue(header.RootElement.TryGetProperty("jwk", out _), "new-account is the one request signed with the bare jwk; there is no kid yet");

				using JsonDocument payload = JsonDocument.Parse(AcmeClient.Base64UrlDecode(capturedPayload));
				Assert.IsTrue(payload.RootElement.GetProperty("termsOfServiceAgreed").GetBoolean());
				Assert.AreEqual("mailto:test@yodamine.test", payload.RootElement.GetProperty("contact")[0].GetString());
			}
			finally
			{
				server.Stop();
				await serving.WaitAsync(TimeSpan.FromSeconds(5));
			}
		}
	}
}