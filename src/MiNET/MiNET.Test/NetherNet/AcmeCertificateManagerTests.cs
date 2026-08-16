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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.NetherNet;
using MiNET.Utils.Cryptography;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     The local half of <see cref="AcmeCertificateManager" />: loading a PEM pair from its
	///     directory, matching SNI against the configured domain, the renewal window, and the
	///     preflight probe that must round-trip through our own responder before a real ACME order
	///     is allowed to spend one of Let's Encrypt's failed-validation slots. The ACME order flow
	///     itself talks to a live CA and is deliberately not covered here.
	/// </summary>
	[TestClass]
	public class AcmeCertificateManagerTests
	{
		private static string NewTempDirectory()
		{
			string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(directory);
			return directory;
		}

		/// <summary>Writes a self-signed cert.pem/key.pem pair for a domain into a fresh directory, the same layout the manager itself persists after an order.</summary>
		private static string WritePemPair(string dnsName)
		{
			string directory = NewTempDirectory();

			using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			var request = new CertificateRequest($"CN={dnsName}", key, HashAlgorithmName.SHA256);
			var san = new SubjectAlternativeNameBuilder();
			san.AddDnsName(dnsName);
			request.CertificateExtensions.Add(san.Build());
			using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(90));

			File.WriteAllText(Path.Combine(directory, $"{dnsName}.cert.pem"), certificate.ExportCertificatePem());
			File.WriteAllText(Path.Combine(directory, $"{dnsName}.key.pem"), key.ExportPkcs8PrivateKeyPem());

			return directory;
		}

		[TestMethod]
		public void LoadedCertificate_AnswersOnlyTheConfiguredDomain()
		{
			string directory = WritePemPair("yodamine.test");
			var manager = new AcmeCertificateManager("yodamine.test", directory);

			manager.LoadCertificateFromDisk();

			Assert.IsNotNull(manager.GetCertificateContext("yodamine.test", IPAddress.Loopback));
			Assert.IsNotNull(manager.GetCertificateContext("YODAMINE.TEST", IPAddress.Loopback), "SNI comparison must be case insensitive, host names are");
			Assert.IsNull(manager.GetCertificateContext("other.test", IPAddress.Loopback));
		}

		/// <summary>
		///     The Bedrock client never sends SNI (verified live 2026-08-16: a join with the name
		///     typed in still offers a nameless ClientHello), so the certificate is served on the
		///     source of the connection instead: a hairpin through the NAT router (source is a
		///     gateway) or a genuinely external client (public source) dialled the external address
		///     and takes the TLS experiment; LAN and loopback sources keep the proven refusal.
		/// </summary>
		[TestMethod]
		public void MissingSni_ServesByConnectionSource()
		{
			string directory = WritePemPair("yodamine.test");
			var manager = new AcmeCertificateManager("yodamine.test", directory, gatewayOverride: new[] {IPAddress.Parse("192.168.10.1")});

			manager.LoadCertificateFromDisk();

			Assert.IsNotNull(manager.GetCertificateContext(null, IPAddress.Parse("192.168.10.1")), "hairpin via the NAT router means the client dialled the external address");
			Assert.IsNotNull(manager.GetCertificateContext(null, IPAddress.Parse("203.0.113.7")), "a public source is an external client");
			Assert.IsNotNull(manager.GetCertificateContext(null, IPAddress.Parse("192.168.10.1").MapToIPv6()), "dual-stack sockets report v4-mapped addresses");
			Assert.IsNull(manager.GetCertificateContext(null, IPAddress.Parse("192.168.10.55")), "a LAN client did not dial the external address; it keeps the plaintext fallback");
			Assert.IsNull(manager.GetCertificateContext(null, IPAddress.Loopback), "local tools without SNI keep the refusal");
			Assert.IsNull(manager.GetCertificateContext(null, null));
		}

		[TestMethod]
		public void NoFilesOnDisk_MeansNoCertificate()
		{
			var manager = new AcmeCertificateManager("yodamine.test", NewTempDirectory());

			manager.LoadCertificateFromDisk();

			Assert.IsNull(manager.GetCertificateContext("yodamine.test", IPAddress.Loopback));
		}

		[TestMethod]
		public void RenewalStartsThirtyDaysBeforeExpiry()
		{
			string directory = WritePemPair("yodamine.test");
			var manager = new AcmeCertificateManager("yodamine.test", directory);
			manager.LoadCertificateFromDisk();
			X509Certificate2 certificate = manager.GetCertificateContext("yodamine.test", IPAddress.Loopback).TargetCertificate;

			Assert.IsFalse(AcmeCertificateManager.NeedsRenewal(certificate, certificate.NotAfter.AddDays(-31)));
			Assert.IsTrue(AcmeCertificateManager.NeedsRenewal(certificate, certificate.NotAfter.AddDays(-29)));
			Assert.IsTrue(AcmeCertificateManager.NeedsRenewal(null, DateTime.UtcNow), "no certificate at all is the most urgent renewal there is");
		}

		[TestMethod]
		public async Task Preflight_RoundTripsThroughOwnResponder()
		{
			var identity = new NetherNetServerIdentity(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "identity.pem"));
			var listener = new NetherNetListener(new IPEndPoint(IPAddress.Loopback, 0), identity, portMapping: null);
			listener.Start();
			try
			{
				var manager = new AcmeCertificateManager("127.0.0.1", NewTempDirectory(), probePort: listener.LocalEndPoint.Port);
				listener.AcmeChallengeHandler = manager.GetChallengeResponse;

				Assert.IsTrue(await manager.PreflightAsync(), "the probe must reach our own responder through a real HTTP round trip");
			}
			finally
			{
				listener.Stop();
			}
		}

		[TestMethod]
		public async Task Preflight_FailsWhenNothingAnswers()
		{
			// Bind and immediately close a port, so it is known to answer nothing.
			var closed = new TcpListener(IPAddress.Loopback, 0);
			closed.Start();
			int port = ((IPEndPoint) closed.LocalEndpoint).Port;
			closed.Stop();

			var manager = new AcmeCertificateManager("127.0.0.1", NewTempDirectory(), probePort: port);

			Assert.IsFalse(await manager.PreflightAsync(), "a preflight that cannot reach the responder must fail rather than let an ACME order burn a rate-limited validation");
		}
	}
}