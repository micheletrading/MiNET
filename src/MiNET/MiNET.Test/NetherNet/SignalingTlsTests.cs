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
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.NetherNet;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     The signaling port's TLS and ACME seams: with a certificate provider the listener answers
	///     the client's TLS offer instead of refusing it, and with a challenge handler it serves the
	///     ACME HTTP-01 path so certificate issuance can validate through the same port the router
	///     already forwards. Without either seam set, behavior must stay exactly as before: TLS is
	///     refused with a fatal handshake_failure alert so the real client falls back to plaintext.
	/// </summary>
	[TestClass]
	public class SignalingTlsTests
	{
		private static NetherNetListener StartListener()
		{
			var identity = new NetherNetServerIdentity(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "identity.pem"));
			var listener = new NetherNetListener(new IPEndPoint(IPAddress.Loopback, 0), identity, portMapping: null);
			listener.Start();
			return listener;
		}

		/// <summary>A server-usable self-signed certificate: SslStream on Windows cannot use an ephemeral private key, so the fresh certificate is round-tripped through PFX.</summary>
		internal static X509Certificate2 CreateTlsCapableCertificate(string dnsName)
		{
			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			var request = new CertificateRequest($"CN={dnsName}", key, HashAlgorithmName.SHA256);
			var san = new SubjectAlternativeNameBuilder();
			san.AddDnsName(dnsName);
			request.CertificateExtensions.Add(san.Build());

			using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(90));
			return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null);
		}

		private static async Task<string> ReadToEndAsync(Stream stream)
		{
			using var memory = new MemoryStream();
			var buffer = new byte[4096];
			int read;
			while ((read = await stream.ReadAsync(buffer)) > 0) memory.Write(buffer, 0, read);
			return Encoding.UTF8.GetString(memory.ToArray());
		}

		[TestMethod]
		public async Task AcmeChallenge_KnownToken_AnswersKeyAuthorization()
		{
			NetherNetListener listener = StartListener();
			try
			{
				listener.AcmeChallengeHandler = token => token == "probe-token" ? "probe-token.thumbprint" : null;
				int port = listener.LocalEndPoint.Port;

				using var http = new HttpClient {Timeout = TimeSpan.FromSeconds(15)};
				HttpResponseMessage response = await http.GetAsync($"http://127.0.0.1:{port}/.well-known/acme-challenge/probe-token");

				Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
				Assert.AreEqual("probe-token.thumbprint", await response.Content.ReadAsStringAsync());
			}
			finally
			{
				listener.Stop();
			}
		}

		[TestMethod]
		public async Task AcmeChallenge_UnknownToken_Is404()
		{
			NetherNetListener listener = StartListener();
			try
			{
				listener.AcmeChallengeHandler = _ => null;
				int port = listener.LocalEndPoint.Port;

				using var http = new HttpClient {Timeout = TimeSpan.FromSeconds(15)};
				HttpResponseMessage response = await http.GetAsync($"http://127.0.0.1:{port}/.well-known/acme-challenge/nobody-registered-this");

				Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
			}
			finally
			{
				listener.Stop();
			}
		}

		[TestMethod]
		public async Task TlsOffer_WithCertificate_ServesSignalingOverTls()
		{
			NetherNetListener listener = StartListener();
			try
			{
				using X509Certificate2 certificate = CreateTlsCapableCertificate("yodamine.test");
				SslStreamCertificateContext context = SslStreamCertificateContext.Create(certificate, null);
				listener.TlsCertificateProvider = (sni, remote) => "yodamine.test".Equals(sni, StringComparison.OrdinalIgnoreCase) ? context : null;
				int port = listener.LocalEndPoint.Port;

				using var tcp = new TcpClient();
				await tcp.ConnectAsync(IPAddress.Loopback, port);
				await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
				await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions {TargetHost = "yodamine.test"}).WaitAsync(TimeSpan.FromSeconds(10));

				await ssl.WriteAsync(Encoding.ASCII.GetBytes("GET /v1/join HTTP/1.1\r\nHost: yodamine.test\r\n\r\n"));
				string response = await ReadToEndAsync(ssl).WaitAsync(TimeSpan.FromSeconds(10));

				StringAssert.StartsWith(response, "HTTP/1.1 200");
			}
			finally
			{
				listener.Stop();
			}
		}

		/// <summary>
		///     The ClientHello description is the forensic record of what a client's TLS stack asked
		///     for; it exists because the real Bedrock client completes a handshake and then abandons
		///     the connection, and the offered versions/ALPN are the only clues to why. Captured from
		///     a real SslStream ClientHello so the parser is proven against genuine bytes.
		/// </summary>
		[TestMethod]
		public async Task ClientHelloDescription_NamesVersionsAlpnAndSni()
		{
			var raw = new TcpListener(IPAddress.Loopback, 0);
			raw.Start();
			try
			{
				int port = ((IPEndPoint) raw.LocalEndpoint).Port;

				using var tcp = new TcpClient();
				await tcp.ConnectAsync(IPAddress.Loopback, port);
				await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
				Task handshake = ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = "yodamine.test",
					ApplicationProtocols = new List<SslApplicationProtocol> {SslApplicationProtocol.Http11},
				});

				using TcpClient accepted = await raw.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(10));
				var head = new byte[4096];
				int read = await accepted.GetStream().ReadAsync(head).AsTask().WaitAsync(TimeSpan.FromSeconds(10));

				string description = NetherNetListener.DescribeClientHello(head, read);

				StringAssert.Contains(description, "1.3", "a current TLS stack offers 1.3");
				StringAssert.Contains(description, "http/1.1", "the ALPN protocol list must be readable");
				StringAssert.Contains(description, "yodamine.test", "SNI belongs in the description when present");

				accepted.Close();
				try { await handshake.WaitAsync(TimeSpan.FromSeconds(5)); } catch { /* the deliberately headless server never answers; only the ClientHello bytes matter */ }
			}
			finally
			{
				raw.Stop();
			}
		}

		/// <summary>
		///     A client asking for [h2, http/1.1] gets http/1.1: signaling speaks HTTP/1.1 only, on
		///     word from Mojang that the client's encrypted path arrives in a coming release, and
		///     the negotiation must land on the protocol this server actually parses rather than
		///     honoring the client's h2 preference and stranding it on an unspoken protocol.
		/// </summary>
		[TestMethod]
		public async Task TlsOffer_FromH2Client_NegotiatesHttp11()
		{
			NetherNetListener listener = StartListener();
			try
			{
				using X509Certificate2 certificate = CreateTlsCapableCertificate("yodamine.test");
				SslStreamCertificateContext context = SslStreamCertificateContext.Create(certificate, null);
				listener.TlsCertificateProvider = (sni, remote) => context;
				int port = listener.LocalEndPoint.Port;

				using var tcp = new TcpClient();
				await tcp.ConnectAsync(IPAddress.Loopback, port);
				await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
				await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
				{
					TargetHost = "yodamine.test",
					ApplicationProtocols = new List<SslApplicationProtocol> {SslApplicationProtocol.Http2, SslApplicationProtocol.Http11},
				}).WaitAsync(TimeSpan.FromSeconds(10));

				Assert.AreEqual(SslApplicationProtocol.Http11, ssl.NegotiatedApplicationProtocol);
			}
			finally
			{
				listener.Stop();
			}
		}

		[TestMethod]
		public async Task TlsOffer_WithoutCertificate_IsStillRefusedWithHandshakeFailure()
		{
			NetherNetListener listener = StartListener();
			try
			{
				int port = listener.LocalEndPoint.Port;

				using var tcp = new TcpClient();
				await tcp.ConnectAsync(IPAddress.Loopback, port);
				NetworkStream stream = tcp.GetStream();

				// The first byte of a TLS record (0x16, handshake) is all the refusal path keys on.
				await stream.WriteAsync(new byte[] {0x16, 0x03, 0x01, 0x00, 0x01, 0x01});

				var alert = new byte[7];
				int read = 0;
				while (read < alert.Length)
				{
					int got = await stream.ReadAsync(alert.AsMemory(read)).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
					if (got == 0) break;
					read += got;
				}

				CollectionAssert.AreEqual(new byte[] {0x15, 0x03, 0x01, 0x00, 0x02, 0x02, 0x28}, alert, "expected a fatal handshake_failure alert so the client falls back to plaintext");
			}
			finally
			{
				listener.Stop();
			}
		}
	}
}