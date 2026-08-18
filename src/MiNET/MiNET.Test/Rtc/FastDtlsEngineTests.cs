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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc.FastDtls;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     Self-interop proofs for the FastDtls handshake engine: two engines driven against each other
	///     over an in-memory queue, covering fragmentation, retransmission after loss, MTU discovery,
	///     fingerprint rejection, and the epoch-1 sequence handoff the production record layer relies on.
	/// </summary>
	[TestClass]
	public class FastDtlsEngineTests
	{
		[TestMethod]
		public void SelfInterop_Mtu1400_HandshakeCompletesWithMatchingKeys()
		{
			SelfInterop(mtu: 1400, lossy: false);
		}

		[TestMethod]
		public void SelfInterop_Mtu180_ForcesFragmentation_HandshakeCompletesWithMatchingKeys()
		{
			SelfInterop(mtu: 180, lossy: false);
		}

		[TestMethod]
		public void SelfInterop_LossyTransport_RetransmissionRecoversHandshake()
		{
			SelfInterop(mtu: 1400, lossy: true);
		}

		[TestMethod]
		public void FingerprintMismatch_HandshakeIsRejected()
		{
			using DtlsCertificate clientCert = DtlsCertificate.Generate();
			using DtlsCertificate serverCert = DtlsCertificate.Generate();
			using DtlsCertificate impostorCert = DtlsCertificate.Generate();

			var toServer = new Queue<byte[]>();
			var toClient = new Queue<byte[]>();
			// the client expects the impostor's fingerprint, so the real server's Certificate must be rejected
			using var client = new DtlsEngine(true, clientCert, toServer.Enqueue, 1400, impostorCert.Fingerprint);
			using var server = new DtlsEngine(false, serverCert, toClient.Enqueue, 1400, clientCert.Fingerprint);

			client.Start();
			DtlsHandshakeException caught = null;
			try
			{
				for (int i = 0; i < 20; i++)
				{
					while (toServer.TryDequeue(out byte[] d)) server.HandleDatagram(d);
					while (toClient.TryDequeue(out byte[] d)) client.HandleDatagram(d);
				}
			}
			catch (DtlsHandshakeException e)
			{
				caught = e;
			}

			Assert.IsNotNull(caught, "the handshake did not fail on a wrong fingerprint");
			Assert.IsTrue(caught.Message.Contains("fingerprint"), $"unexpected failure reason: {caught.Message}");
		}

		/// <summary>
		///     A path that silently eats every datagram over 700 bytes. The client's padded ClientHello
		///     is a genuine 1472-byte probe, so it vanishes and the timeout ladder has to walk the
		///     client down to 576 before the handshake can start. The server's flights are content-sized
		///     (they fit under 700), so the server correctly keeps its size: in-handshake probing covers
		///     the client's outbound; the server's outbound is the SCTP layer's PADDING-chunk job later.
		/// </summary>
		[TestMethod]
		public void MtuLadder_PathCapsAt700Bytes_ClientNegotiatesDownTo576()
		{
			using DtlsCertificate clientCert = DtlsCertificate.Generate();
			using DtlsCertificate serverCert = DtlsCertificate.Generate();

			const int pathMtu = 700;
			var toServer = new Queue<byte[]>();
			var toClient = new Queue<byte[]>();
			int dropped = 0;

			using var client = new DtlsEngine(true, clientCert, d => { if (d.Length <= pathMtu) toServer.Enqueue(d); else dropped++; }, 1472, serverCert.Fingerprint);
			using var server = new DtlsEngine(false, serverCert, d => { if (d.Length <= pathMtu) toClient.Enqueue(d); else dropped++; }, 1472, clientCert.Fingerprint);

			client.Start();

			int timeouts = 0;
			while (!client.IsComplete || !server.IsComplete)
			{
				bool progressed = false;
				while (toServer.TryDequeue(out byte[] d)) { server.HandleDatagram(d); progressed = true; }
				while (toClient.TryDequeue(out byte[] d)) { client.HandleDatagram(d); progressed = true; }
				if (progressed) continue;

				Assert.IsTrue(++timeouts <= 100, $"handshake did not converge; client mtu {client.NegotiatedMtu}, server mtu {server.NegotiatedMtu}");
				client.OnTimeout();
				server.OnTimeout();
			}

			Assert.IsTrue(dropped > 0, "the path never dropped anything, so nothing was probed");
			Assert.AreEqual(576, client.NegotiatedMtu, "the client did not negotiate down to the conservative floor");
			AssertKeysMatch(client, server);
			ProveKeysUsable(client, server);
		}

		/// <summary>
		///     After completion, <see cref="DtlsEngine.NextEpoch1SendSequence" /> is the exact sequence a
		///     production record layer must seed itself with: a record protected at that sequence, under
		///     the sender's negotiated key, decrypts cleanly on the peer's side.
		/// </summary>
		[TestMethod]
		public void NextEpoch1SendSequence_EncryptedRecordAtThatSequence_DecryptsOnPeer()
		{
			using DtlsCertificate clientCert = DtlsCertificate.Generate();
			using DtlsCertificate serverCert = DtlsCertificate.Generate();
			var toServer = new Queue<byte[]>();
			var toClient = new Queue<byte[]>();
			using var client = new DtlsEngine(true, clientCert, toServer.Enqueue, 1400, serverCert.Fingerprint);
			using var server = new DtlsEngine(false, serverCert, toClient.Enqueue, 1400, clientCert.Fingerprint);

			client.Start();
			RunToCompletion(client, server, toServer, toClient);

			ulong sequence = client.NextEpoch1SendSequence;
			using var clientSend = new RecordCipher(client.Keys.ClientWriteKey, client.Keys.ClientWriteSalt);
			using var serverReceive = new RecordCipher(server.Keys.ClientWriteKey, server.Keys.ClientWriteSalt);

			byte[] message = Encoding.UTF8.GetBytes("application data at the seeded sequence");
			byte[] wire = new byte[message.Length + 8 + 16];
			clientSend.Encrypt(1, sequence, ContentType.ApplicationData, message, wire);

			byte[] plain = new byte[wire.Length];
			int n = serverReceive.Decrypt(1, sequence, ContentType.ApplicationData, wire, plain);

			Assert.IsTrue(n == message.Length && plain.AsSpan(0, n).SequenceEqual(message), "the record at NextEpoch1SendSequence did not round trip through the peer's cipher");
		}

		[TestMethod]
		public void SeedEpoch1SendSequence_IsForwardOnly()
		{
			using DtlsCertificate clientCert = DtlsCertificate.Generate();
			using DtlsCertificate serverCert = DtlsCertificate.Generate();
			var toServer = new Queue<byte[]>();
			var toClient = new Queue<byte[]>();
			using var client = new DtlsEngine(true, clientCert, toServer.Enqueue, 1400, serverCert.Fingerprint);
			using var server = new DtlsEngine(false, serverCert, toClient.Enqueue, 1400, clientCert.Fingerprint);

			client.Start();
			RunToCompletion(client, server, toServer, toClient);

			ulong sequence = client.NextEpoch1SendSequence;
			Assert.IsTrue(sequence > 0, "the Finished flight should have consumed at least one epoch-1 sequence number");

			client.SeedEpoch1SendSequence(sequence - 1);
			Assert.AreEqual(sequence, client.NextEpoch1SendSequence, "seeding backward moved the sequence");

			client.SeedEpoch1SendSequence(sequence + 100);
			Assert.AreEqual(sequence + 100, client.NextEpoch1SendSequence, "seeding forward did not move the sequence");
		}

		// ---- shared harness ----

		private static void SelfInterop(int mtu, bool lossy)
		{
			using DtlsCertificate clientCert = DtlsCertificate.Generate();
			using DtlsCertificate serverCert = DtlsCertificate.Generate();

			var toServer = new Queue<byte[]>();
			var toClient = new Queue<byte[]>();
			int datagramCounter = 0;
			// deterministic loss: every 4th datagram vanishes on first transmission
			bool Lost() => lossy && ++datagramCounter % 4 == 0;

			using var client = new DtlsEngine(true, clientCert, d => { if (!Lost()) toServer.Enqueue(d); }, mtu, serverCert.Fingerprint);
			using var server = new DtlsEngine(false, serverCert, d => { if (!Lost()) toClient.Enqueue(d); }, mtu, clientCert.Fingerprint);

			client.Start();
			RunToCompletion(client, server, toServer, toClient);

			AssertKeysMatch(client, server);
			Assert.IsTrue(client.PeerFingerprint.SequenceEqual(serverCert.Fingerprint), "client saw wrong server fingerprint");
			Assert.IsTrue(server.PeerFingerprint.SequenceEqual(clientCert.Fingerprint), "server saw wrong client fingerprint");
			ProveKeysUsable(client, server);
		}

		private static void RunToCompletion(DtlsEngine client, DtlsEngine server, Queue<byte[]> toServer, Queue<byte[]> toClient)
		{
			int stalls = 0;
			while (!client.IsComplete || !server.IsComplete)
			{
				bool progressed = false;
				while (toServer.TryDequeue(out byte[] d)) { server.HandleDatagram(d); progressed = true; }
				while (toClient.TryDequeue(out byte[] d)) { client.HandleDatagram(d); progressed = true; }
				if (progressed) continue;

				// both queues drained without completing: a datagram was lost, fire the "timers"
				Assert.IsTrue(++stalls <= 50, "handshake did not converge after 50 retransmission rounds");
				client.Retransmit();
				server.Retransmit();
			}
		}

		private static void AssertKeysMatch(DtlsEngine client, DtlsEngine server)
		{
			Assert.IsTrue(client.Keys != null && server.Keys != null, "keys missing after completion");
			Assert.IsTrue(client.Keys.ClientWriteKey.SequenceEqual(server.Keys.ClientWriteKey), "client write key mismatch");
			Assert.IsTrue(client.Keys.ServerWriteKey.SequenceEqual(server.Keys.ServerWriteKey), "server write key mismatch");
			Assert.IsTrue(client.Keys.ClientWriteSalt.SequenceEqual(server.Keys.ClientWriteSalt), "client salt mismatch");
			Assert.IsTrue(client.Keys.ServerWriteSalt.SequenceEqual(server.Keys.ServerWriteSalt), "server salt mismatch");
		}

		/// <summary>The whole point of the engine: the keys it hands over drive a working record layer.</summary>
		private static void ProveKeysUsable(DtlsEngine client, DtlsEngine server)
		{
			using var clientSend = new RecordCipher(client.Keys.ClientWriteKey, client.Keys.ClientWriteSalt);
			using var serverReceive = new RecordCipher(server.Keys.ClientWriteKey, server.Keys.ClientWriteSalt);

			byte[] message = Encoding.UTF8.GetBytes("application data over the negotiated keys");
			byte[] wire = new byte[message.Length + 8 + 16];
			clientSend.Encrypt(1, 7, ContentType.ApplicationData, message, wire);
			byte[] plain = new byte[wire.Length];
			int n = serverReceive.Decrypt(1, 7, ContentType.ApplicationData, wire, plain);
			Assert.IsTrue(n == message.Length && plain.AsSpan(0, n).SequenceEqual(message), "app-data round trip over negotiated keys failed");
		}
	}
}