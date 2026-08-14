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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc.FastDtls;
using ContentType = MiNET.Net.Rtc.FastDtls.ContentType;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.X509;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     Interop proof against BouncyCastle's DTLS stack, the reference implementation the production
	///     code uses elsewhere. Self-interop cannot catch a mirrored wire bug (both sides sharing the
	///     same wrong idea still agree); a handshake against an independent stack can. Each direction
	///     ends with an application-data record crossing the boundary: BC protects with its negotiated
	///     keys, the FastDtls side decrypts with the keys its engine reports, and vice versa.
	/// </summary>
	[TestClass]
	public class FastDtlsBcInteropTests
	{
		[TestMethod]
		public void FastDtlsClient_AgainstBouncyCastleServer_HandshakeCompletesAndAppDataRoundTrips()
		{
			FastClientAgainstBcServer();
		}

		[TestMethod]
		public void BouncyCastleClient_AgainstFastDtlsServer_HandshakeCompletesAndAppDataRoundTrips()
		{
			BcClientAgainstFastServer();
		}

		/// <summary>
		///     <paramref name="clientPathMtu" /> simulates a path that silently drops client-to-server
		///     datagrams above the cap: the FastDtls client's certificate flight has to shrink down the
		///     MTU ladder until it fits, and BC then reassembles a re-fragmented retransmission carrying
		///     the same message_seq values, the exact peer behavior the probing design relies on.
		/// </summary>
		[TestMethod]
		public void MtuLadder_AgainstBouncyCastleServer_PathCapsAt700_ClientNegotiatesUnderCap()
		{
			FastClientAgainstBcServer(clientPathMtu: 700);
		}

		/// <summary>
		///     Reinstates the record-layer-against-BC coverage the DTLS engine rewire dropped when it
		///     retired <see cref="DtlsSession" />'s own BouncyCastle handshake driver: the production
		///     post-handshake record layer, <see cref="MiNET.Net.Rtc.DtlsRecordCrypto" /> - what
		///     <see cref="DtlsSession" /> actually protects application data with, not
		///     <see cref="RecordCipher" /> (this harness's own single-direction internal test cipher) -
		///     cross-checked against BC's own record layer, both directions, over the keys this same
		///     handshake negotiated. Self-interop within this project's own two record-layer
		///     implementations could still agree on a mirrored wire bug; BC is the independent check.
		///     AES-128-GCM only, matching the one cipher suite the engine ever negotiates.
		/// </summary>
		[TestMethod]
		public void FastDtlsClient_AgainstBouncyCastleServer_ProductionRecordLayerCrossChecksBothDirections()
		{
			FastClientAgainstBcServer(extraCrossCheck: CrossCheckProductionRecordLayer);
		}

		// ---- harness ----

		private static void FastClientAgainstBcServer(int clientPathMtu = int.MaxValue, Action<DtlsTransport, BlockingCollection<byte[]>, BlockingCollection<byte[]>, DtlsEngine> extraCrossCheck = null)
		{
			using DtlsCertificate fastCert = DtlsCertificate.Generate();
			var crypto = new BcTlsCrypto(new SecureRandom(new CryptoApiRandomGenerator()));
			BcTestCertificate bcCert = BcTestCertificate.Create(crypto);

			var toBc = new BlockingCollection<byte[]>();
			var toFast = new BlockingCollection<byte[]>();

			DtlsTransport bcTransport = null;
			Exception bcError = null;
			var bcThread = new Thread(() =>
			{
				try
				{
					var server = new BcServerPeer(crypto, bcCert, fastCert.Fingerprint);
					bcTransport = new DtlsServerProtocol().Accept(server, new QueueTransport(toBc, toFast.Add));
				}
				catch (Exception e)
				{
					bcError = e;
				}
			}) { IsBackground = true };
			bcThread.Start();

			using var fast = new DtlsEngine(true, fastCert, d => { if (d.Length <= clientPathMtu) toBc.Add(d); }, 1472, bcCert.Fingerprint);
			fast.Start();
			PumpUntilComplete(fast, toFast, () => bcError);
			Assert.IsTrue(bcThread.Join(5000), "BC server thread did not finish");
			if (bcError != null) Assert.Fail($"BC server failed: {bcError.Message}");
			if (clientPathMtu != int.MaxValue && fast.NegotiatedMtu > clientPathMtu)
			{
				Assert.Fail($"ladder never engaged: negotiated {fast.NegotiatedMtu} over a {clientPathMtu}-byte path");
			}

			try
			{
				// BC (server role) -> FastDtls keys
				byte[] fromBc = ReceiveThroughFastKeys(bcTransport, toFast, fast.Keys.ServerWriteKey, fast.Keys.ServerWriteSalt);
				AssertPayload(fromBc, "bc server to fast client");

				// FastDtls keys -> BC (server role receives with the client write key)
				SendThroughFastKeys(bcTransport, toBc, fast.Keys.ClientWriteKey, fast.Keys.ClientWriteSalt);

				extraCrossCheck?.Invoke(bcTransport, toBc, toFast, fast);
			}
			finally
			{
				bcTransport.Close();
			}
		}

		private static void BcClientAgainstFastServer()
		{
			using DtlsCertificate fastCert = DtlsCertificate.Generate();
			var crypto = new BcTlsCrypto(new SecureRandom(new CryptoApiRandomGenerator()));
			BcTestCertificate bcCert = BcTestCertificate.Create(crypto);

			var toBc = new BlockingCollection<byte[]>();
			var toFast = new BlockingCollection<byte[]>();

			DtlsTransport bcTransport = null;
			Exception bcError = null;
			var bcThread = new Thread(() =>
			{
				try
				{
					var client = new BcClientPeer(crypto, bcCert, fastCert.Fingerprint);
					bcTransport = new DtlsClientProtocol().Connect(client, new QueueTransport(toBc, toFast.Add));
				}
				catch (Exception e)
				{
					bcError = e;
				}
			}) { IsBackground = true };
			bcThread.Start();

			using var fast = new DtlsEngine(false, fastCert, toBc.Add, 1472, bcCert.Fingerprint);
			PumpUntilComplete(fast, toFast, () => bcError);
			Assert.IsTrue(bcThread.Join(5000), "BC client thread did not finish");
			if (bcError != null) Assert.Fail($"BC client failed: {bcError.Message}");

			try
			{
				// BC (client role) -> FastDtls keys
				byte[] fromBc = ReceiveThroughFastKeys(bcTransport, toFast, fast.Keys.ClientWriteKey, fast.Keys.ClientWriteSalt);
				AssertPayload(fromBc, "bc client to fast server");

				// FastDtls keys -> BC (client role receives with the server write key)
				SendThroughFastKeys(bcTransport, toBc, fast.Keys.ServerWriteKey, fast.Keys.ServerWriteSalt);
			}
			finally
			{
				bcTransport.Close();
			}
		}

		private static readonly byte[] ProbePayload = System.Text.Encoding.UTF8.GetBytes("interop probe across independent stacks");

		private static void PumpUntilComplete(DtlsEngine fast, BlockingCollection<byte[]> toFast, Func<Exception> bcError)
		{
			var stopwatch = Stopwatch.StartNew();
			while (!fast.IsComplete)
			{
				if (toFast.TryTake(out byte[] datagram, 100))
				{
					fast.HandleDatagram(datagram);
					continue;
				}
				Exception e = bcError();
				if (e != null) Assert.Fail($"BC peer failed mid-handshake: {e.Message}");
				Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 15000, "handshake against BC timed out");
				fast.OnTimeout(); // 100ms idle stands in for the host's retransmission timer
			}
		}

		/// <summary>
		///     BC protects a record with its own stack; we decrypt it with nothing but the engine-reported
		///     keys.
		///     <para>
		///     Searches for the epoch-1 application-data record rather than asserting on whatever arrives
		///     first. <see cref="PumpUntilComplete" /> stops the instant the engine reports complete, which
		///     can leave BC's last handshake flight - or a retransmission of it - still sitting in
		///     <paramref name="toFast" />; taking that datagram and asserting it is application data is a
		///     race the test lost only when a full-suite run changed the thread timing, never solo. A
		///     datagram may also carry several records, so this walks records within one, not just across
		///     datagrams.
		///     </para>
		/// </summary>
		private static byte[] ReceiveThroughFastKeys(DtlsTransport bcTransport, BlockingCollection<byte[]> toFast, byte[] bcWriteKey, byte[] bcWriteSalt)
		{
			bcTransport.Send(ProbePayload, 0, ProbePayload.Length);

			using var cipher = new RecordCipher(bcWriteKey, bcWriteSalt);
			var deadline = Stopwatch.StartNew();

			while (deadline.ElapsedMilliseconds < 5000)
			{
				if (!toFast.TryTake(out byte[] datagram, 250)) continue;

				int offset = 0;
				while (offset < datagram.Length &&
					DtlsRecords.TryReadHeader(datagram.AsSpan(offset), out ContentType type, out ushort epoch, out ulong seq48, out int payloadLength) &&
					offset + DtlsRecords.HeaderLength + payloadLength <= datagram.Length)
				{
					if (type == ContentType.ApplicationData && epoch == 1)
					{
						byte[] plaintext = new byte[payloadLength];
						int n = cipher.Decrypt(epoch, seq48, type, datagram.AsSpan(offset + DtlsRecords.HeaderLength, payloadLength), plaintext);
						Assert.IsTrue(n >= 0, "BC record failed to decrypt under the engine-reported keys");
						return plaintext.AsSpan(0, n).ToArray();
					}

					offset += DtlsRecords.HeaderLength + payloadLength;
				}
			}

			Assert.Fail("no epoch-1 application data record arrived from BC within 5s");
			return null;
		}

		/// <summary>We protect a record with nothing but the engine-reported keys; BC's stack must accept it.</summary>
		private static void SendThroughFastKeys(DtlsTransport bcTransport, BlockingCollection<byte[]> toBc, byte[] fastWriteKey, byte[] fastWriteSalt)
		{
			using var cipher = new RecordCipher(fastWriteKey, fastWriteSalt);
			const ulong seq = 100; // safely above anything the handshake consumed, inside BC's replay window
			byte[] datagram = new byte[DtlsRecords.HeaderLength + ProbePayload.Length + 8 + 16];
			DtlsRecords.WriteHeader(datagram, ContentType.ApplicationData, 1, seq, ProbePayload.Length + 8 + 16);
			cipher.Encrypt(1, seq, ContentType.ApplicationData, ProbePayload, datagram.AsSpan(DtlsRecords.HeaderLength));
			toBc.Add(datagram);

			byte[] buffer = new byte[1500];
			int n = bcTransport.Receive(buffer, 0, buffer.Length, 3000);
			Assert.IsTrue(n == ProbePayload.Length && buffer.AsSpan(0, n).SequenceEqual(ProbePayload), "BC did not accept the record protected with the engine-reported keys");
		}

		private static void AssertPayload(byte[] received, string direction)
		{
			Assert.IsTrue(received.AsSpan().SequenceEqual(ProbePayload), $"payload corrupted: {direction}");
		}

		private static readonly byte[] ProductionLayerProbePayload = System.Text.Encoding.UTF8.GetBytes("production record layer cross-check");

		/// <summary>
		///     One <see cref="MiNET.Net.Rtc.DtlsRecordCrypto" /> instance, client role, doing both
		///     directions exactly as <see cref="DtlsSession" /> does: <see cref="MiNET.Net.Rtc.DtlsRecordCrypto.EncryptRecord" />
		///     with the client write key, <see cref="MiNET.Net.Rtc.DtlsRecordCrypto.TryDecryptRecord" />
		///     with the server write key. The seed is arbitrary but must clear whatever
		///     <see cref="SendThroughFastKeys" /> already put on BC's receive window in this same session
		///     (100), so 500 here, not the production seed <see cref="DtlsSession" /> would actually use
		///     (this harness has no <see cref="DtlsSession" /> in the loop at all - only the engine and a
		///     bare record layer instance built directly from its keys).
		/// </summary>
		private static void CrossCheckProductionRecordLayer(DtlsTransport bcTransport, BlockingCollection<byte[]> toBc, BlockingCollection<byte[]> toFast, DtlsEngine fast)
		{
			const byte ApplicationData = 23;

			using var ours = new MiNET.Net.Rtc.DtlsRecordCrypto(fast.Keys, isServer: false, sendSequenceSeed: 500);

			// We encrypt (client role) with the production record layer, BC decrypts.
			Span<byte> wire = stackalloc byte[ProductionLayerProbePayload.Length + MiNET.Net.Rtc.DtlsRecordCrypto.RecordOverhead];
			int wireLength = ours.EncryptRecord(ApplicationData, ProductionLayerProbePayload, wire);
			Assert.AreNotEqual(-1, wireLength);
			toBc.Add(wire.Slice(0, wireLength).ToArray());

			byte[] buffer = new byte[1500];
			int n = bcTransport.Receive(buffer, 0, buffer.Length, 3000);
			Assert.IsTrue(n == ProductionLayerProbePayload.Length && buffer.AsSpan(0, n).SequenceEqual(ProductionLayerProbePayload), "BC did not accept a record protected by the production record layer");

			// BC encrypts, the production record layer decrypts.
			bcTransport.Send(ProductionLayerProbePayload, 0, ProductionLayerProbePayload.Length);
			Assert.IsTrue(toFast.TryTake(out byte[] datagram, 3000), "no record arrived from BC");

			Span<byte> plaintext = stackalloc byte[ProductionLayerProbePayload.Length];
			bool ok = ours.TryDecryptRecord(datagram, plaintext, out byte contentType, out int length);
			Assert.IsTrue(ok, "the production record layer failed to decrypt a genuine BC-produced record");
			Assert.AreEqual(ApplicationData, contentType);
			Assert.IsTrue(plaintext.Slice(0, length).SequenceEqual(ProductionLayerProbePayload), "payload corrupted crossing the production record layer");
		}

		// ---- BC plumbing ----

		private sealed class QueueTransport : DatagramTransport
		{
			private readonly BlockingCollection<byte[]> _incoming;
			private readonly Action<byte[]> _send;

			public QueueTransport(BlockingCollection<byte[]> incoming, Action<byte[]> send)
			{
				_incoming = incoming;
				_send = send;
			}

			public int GetReceiveLimit() => 1400;
			public int GetSendLimit() => 1400;

			public int Receive(byte[] buf, int off, int len, int waitMillis) => Receive(buf.AsSpan(off, len), waitMillis);

			public int Receive(Span<byte> buffer, int waitMillis)
			{
				if (!_incoming.TryTake(out byte[] datagram, waitMillis)) return -1;
				int n = Math.Min(buffer.Length, datagram.Length);
				datagram.AsSpan(0, n).CopyTo(buffer);
				return n;
			}

			public void Send(byte[] buf, int off, int len) => Send(buf.AsSpan(off, len));

			public void Send(ReadOnlySpan<byte> buffer) => _send(buffer.ToArray());

			public void Close()
			{
			}
		}

		private sealed class BcTestCertificate
		{
			public Certificate Certificate { get; private init; }
			public AsymmetricKeyParameter PrivateKey { get; private init; }
			public byte[] Fingerprint { get; private init; }

			public static BcTestCertificate Create(BcTlsCrypto crypto)
			{
				var random = new SecureRandom(new CryptoApiRandomGenerator());

				X9ECParameters curve = ECNamedCurveTable.GetByName("secp256r1");
				var keyPairGenerator = new ECKeyPairGenerator("ECDSA");
				keyPairGenerator.Init(new ECKeyGenerationParameters(new ECDomainParameters(curve), random));
				AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();

				var subject = new X509Name(new List<DerObjectIdentifier> { X509Name.CN }, new Dictionary<DerObjectIdentifier, string> { { X509Name.CN, "BC-Interop" } });
				var generator = new X509V3CertificateGenerator();
				generator.SetIssuerDN(subject);
				generator.SetSubjectDN(subject);
				generator.SetPublicKey(keyPair.Public);
				generator.SetNotBefore(DateTime.UtcNow.AddDays(-1));
				generator.SetNotAfter(DateTime.UtcNow.AddDays(30));
				byte[] serial = new byte[16];
				random.NextBytes(serial);
				serial[0] &= 0x7F;
				generator.SetSerialNumber(new BigInteger(1, serial));

				X509Certificate x509 = generator.Generate(new Asn1SignatureFactory("SHA256withECDSA", keyPair.Private, random));
				byte[] der = x509.GetEncoded();

				return new BcTestCertificate
				{
					Certificate = new Certificate(null, new[] { new CertificateEntry(crypto.CreateCertificate(der), null) }),
					PrivateKey = keyPair.Private,
					Fingerprint = System.Security.Cryptography.SHA256.HashData(der),
				};
			}
		}

		private static void VerifyFingerprint(Certificate presented, byte[] expected)
		{
			if (presented == null || presented.IsEmpty) throw new TlsFatalAlert(AlertDescription.bad_certificate);
			byte[] actual = System.Security.Cryptography.SHA256.HashData(presented.GetCertificateAt(0).GetEncoded());
			if (!actual.AsSpan().SequenceEqual(expected)) throw new TlsFatalAlert(AlertDescription.bad_certificate);
		}

		private static SignatureAndHashAlgorithm SelectEcdsaSha256(IList<SignatureAndHashAlgorithm> peerAlgorithms)
		{
			if (peerAlgorithms != null)
			{
				foreach (SignatureAndHashAlgorithm algorithm in peerAlgorithms)
				{
					if (algorithm.Signature == SignatureAlgorithm.ecdsa && algorithm.Hash == HashAlgorithm.sha256) return algorithm;
				}
			}
			return new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.ecdsa);
		}

		private sealed class BcServerPeer : DefaultTlsServer
		{
			private readonly BcTestCertificate _localCertificate;
			private readonly byte[] _expectedPeerFingerprint;

			public BcServerPeer(BcTlsCrypto crypto, BcTestCertificate localCertificate, byte[] expectedPeerFingerprint) : base(crypto)
			{
				_localCertificate = localCertificate;
				_expectedPeerFingerprint = expectedPeerFingerprint;
			}

			public override bool RequiresExtendedMasterSecret() => true;
			public override int GetHandshakeTimeoutMillis() => 10000;
			protected override ProtocolVersion[] GetSupportedVersions() => ProtocolVersion.DTLSv12.Only();
			protected override int[] GetSupportedCipherSuites() => new[] { CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 };

			public override CertificateRequest GetCertificateRequest()
			{
				IList<SignatureAndHashAlgorithm> serverSigAlgs = null;
				if (TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(m_context.ServerVersion))
				{
					serverSigAlgs = TlsUtilities.GetDefaultSupportedSignatureAlgorithms(m_context);
				}
				return new CertificateRequest(new[] { ClientCertificateType.ecdsa_sign }, serverSigAlgs, null);
			}

			public override void NotifyClientCertificate(Certificate clientCertificate) => VerifyFingerprint(clientCertificate, _expectedPeerFingerprint);

			protected override TlsCredentialedSigner GetECDsaSignerCredentials()
			{
				SignatureAndHashAlgorithm algorithm = SelectEcdsaSha256(m_context.SecurityParameters.ClientSigAlgs);
				return new BcDefaultTlsCredentialedSigner(new TlsCryptoParameters(m_context), (BcTlsCrypto) m_context.Crypto, _localCertificate.PrivateKey, _localCertificate.Certificate, algorithm);
			}
		}

		private sealed class BcClientPeer : DefaultTlsClient
		{
			private readonly BcTestCertificate _localCertificate;
			private readonly byte[] _expectedPeerFingerprint;

			public BcClientPeer(BcTlsCrypto crypto, BcTestCertificate localCertificate, byte[] expectedPeerFingerprint) : base(crypto)
			{
				_localCertificate = localCertificate;
				_expectedPeerFingerprint = expectedPeerFingerprint;
			}

			public override bool RequiresExtendedMasterSecret() => true;
			public override int GetHandshakeTimeoutMillis() => 10000;
			protected override ProtocolVersion[] GetSupportedVersions() => ProtocolVersion.DTLSv12.Only();
			protected override int[] GetSupportedCipherSuites() => new[] { CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 };

			public override TlsAuthentication GetAuthentication() => new Authentication(this);

			private sealed class Authentication : TlsAuthentication
			{
				private readonly BcClientPeer _peer;

				public Authentication(BcClientPeer peer)
				{
					_peer = peer;
				}

				public void NotifyServerCertificate(TlsServerCertificate serverCertificate) => VerifyFingerprint(serverCertificate?.Certificate, _peer._expectedPeerFingerprint);

				public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
				{
					SignatureAndHashAlgorithm algorithm = SelectEcdsaSha256(certificateRequest.SupportedSignatureAlgorithms);
					return new BcDefaultTlsCredentialedSigner(new TlsCryptoParameters(_peer.m_context), (BcTlsCrypto) _peer.m_context.Crypto, _peer._localCertificate.PrivateKey, _peer._localCertificate.Certificate, algorithm);
				}
			}
		}
	}
}