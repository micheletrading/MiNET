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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     Liveness (HEARTBEAT) and teardown (ABORT, SHUTDOWN, SHUTDOWN-COMPLETE) chunk handling on an
	///     established <see cref="SctpAssociation" />, wired the same synchronous loopback way
	///     <see cref="SctpAssociationHandshakeTests" /> and <see cref="SctpSendTests" /> already do.
	/// </summary>
	[TestClass]
	public class SctpTeardownTests
	{
		private const ushort Port = 5000;
		private const uint ArwndBudget = 131072;

		private static byte[] BuildRawChunkPacket(uint verificationTag, byte chunkType, ReadOnlySpan<byte> value)
		{
			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, Port, Port, verificationTag);
			value.CopyTo(packet.Slice(n + 4));
			n += SctpChunkCodec.FinishChunk(packet.Slice(n), chunkType, 0, value.Length);
			SctpPacket.FinishChecksum(packet.Slice(0, n));
			return packetArray.AsSpan(0, n).ToArray();
		}

		private static byte[] BuildAbortPacket(uint verificationTag, ReadOnlySpan<byte> causeData = default) => BuildRawChunkPacket(verificationTag, SctpChunkType.Abort, causeData);

		private static byte[] BuildShutdownPacket(uint verificationTag, ReadOnlySpan<byte> value = default) => BuildRawChunkPacket(verificationTag, 7 /* SHUTDOWN */, value);

		private static byte[] BuildShutdownCompletePacket(uint verificationTag) => BuildRawChunkPacket(verificationTag, 14 /* SHUTDOWN-COMPLETE */, ReadOnlySpan<byte>.Empty);

		/// <summary>Same shape as <see cref="SctpReceiveTests" />'s own private helper: hand-builds and delivers one DATA chunk.</summary>
		private static void FeedData(SctpAssociation receiver, uint verificationTag, uint tsn, ushort streamId, ushort streamSeq, uint ppid, bool unordered, bool begin, bool end, ReadOnlySpan<byte> payload)
		{
			var header = new DataChunkHeader(tsn, streamId, streamSeq, ppid, unordered, begin, end, immediateSack: false);

			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, Port, Port, verificationTag);
			n += header.WriteTo(packet.Slice(n), payload);
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			receiver.OnPacketReceived(packetArray.AsMemory(0, n));
		}

		/// <summary>Establishes a client/server pair, exactly like <see cref="SctpAssociationHandshakeTests" />'s own handshake test, but also captures every packet the server sends so a test can inspect the server's replies directly.</summary>
		/// <summary>
		///     Switch on the server-to-client leg of the loopback. Set false and the server's packets
		///     are still captured but never reach the client, so nothing acknowledges them: that is the
		///     only way to leave a chunk outstanding on this harness now that acknowledgement is
		///     immediate. Before that it happened by accident, because the client's SACK sat on a timer
		///     the test never let run, which made the test depend on the acknowledgement policy without
		///     saying so.
		/// </summary>
		private sealed class Loopback
		{
			public bool DeliverToClient = true;
		}

		private static (SctpAssociation Client, SctpAssociation Server, List<byte[]> ServerSent, Loopback Link) EstablishPair()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var serverSent = new List<byte[]>();
			var link = new Loopback();

			client = new SctpAssociation(true, Port, ArwndBudget, p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(false, Port, ArwndBudget, p =>
			{
				serverSent.Add(p.ToArray());
				if (link.DeliverToClient) client.OnPacketReceived(p.ToArray());
			});

			client.Start();

			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(SctpState.Established, server.State);
			serverSent.Clear();

			return (client, server, serverSent, link);
		}

		[TestMethod]
		public void Heartbeat_OnEstablishedAssociation_IsAnsweredWithVerbatimInfoBytes()
		{
			(_, SctpAssociation server, List<byte[]> serverSent, _) = EstablishPair();

			byte[] info = {0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x01, 0x02, 0x03};
			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, Port, Port, server.LocalVerificationTag);
			n += new HeartbeatChunk(info).WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			server.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.AreEqual(1, serverSent.Count);
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(serverSent[0]);
			Assert.IsTrue(enumerator.MoveNext());
			(byte type, byte _, ReadOnlySpan<byte> value) = enumerator.Current;
			Assert.AreEqual(SctpChunkType.HeartbeatAck, type);
			Assert.IsTrue(HeartbeatChunk.TryParse(value, out HeartbeatChunk ack));
			CollectionAssert.AreEqual(info, ack.Info.ToArray());
			Assert.AreEqual(SctpState.Established, server.State);
		}

		/// <summary>A HEARTBEAT whose Heartbeat Info TLV is truncated below its own 4-byte header must not throw, must not be answered, and must be counted - the hot-path law applied to hostile input on this chunk type.</summary>
		[TestMethod]
		public void Heartbeat_TruncatedInfoTlv_IsIgnored_NoThrowNoReply()
		{
			(_, SctpAssociation server, List<byte[]> serverSent, _) = EstablishPair();

			byte[] truncated = BuildRawChunkPacket(server.LocalVerificationTag, SctpChunkType.Heartbeat, stackalloc byte[2]);
			long ignoredBefore = server.IgnoredPacketCount;

			server.OnPacketReceived(truncated);

			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
			Assert.AreEqual(0, serverSent.Count);
			Assert.AreEqual(SctpState.Established, server.State);
		}

		/// <summary>
		///     A HEARTBEAT whose Info this side could never echo back within one SctpPacket.MaxSize reply
		///     packet is dropped and counted rather than attempted: the hot-path law applied to a length
		///     nothing upstream of HandleHeartbeat bounds.
		/// </summary>
		[TestMethod]
		public void Heartbeat_OversizedInfo_IsDroppedAndCounted_NoThrowNoReply()
		{
			(_, SctpAssociation server, List<byte[]> serverSent, _) = EstablishPair();

			// Larger than any reply built inside SctpPacket.MaxSize (1200 bytes) could ever hold, so this
			// must be rejected on receipt rather than attempted and failing later.
			byte[] info = new byte[1400];
			byte[] packetArray = new byte[2048];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, Port, Port, server.LocalVerificationTag);
			n += new HeartbeatChunk(info).WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			long ignoredBefore = server.IgnoredPacketCount;

			server.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
			Assert.AreEqual(0, serverSent.Count);
			Assert.AreEqual(SctpState.Established, server.State);
		}

		/// <summary>
		///     Teardown never invalidates the association's own verification tag, so a DATA chunk carrying
		///     it still passes the packet-level tag gate and reaches HandleData on an Aborted association.
		///     MaybeSendSack's own cadence fires on the second such packet, so this sends two to prove
		///     neither provokes a SACK off a torn-down receive buffer.
		/// </summary>
		[TestMethod]
		public void Data_OnAbortedAssociation_WithMatchingTag_NeverEmitsASack_NotEvenOnTheSecondPacket()
		{
			(_, SctpAssociation server, List<byte[]> serverSent, _) = EstablishPair();

			uint tag = server.LocalVerificationTag;
			uint tsn = unchecked(server.CumulativeTsnAck + 1);

			server.Abort();
			Assert.AreEqual(SctpState.Aborted, server.State);
			serverSent.Clear();

			FeedData(server, tag, tsn, 1, 0, 1, unordered: true, begin: true, end: true, new byte[] {1, 2, 3});
			FeedData(server, tag, unchecked(tsn + 1), 1, 0, 1, unordered: true, begin: true, end: true, new byte[] {4, 5, 6});

			Assert.AreEqual(0, serverSent.Count, "an Aborted association never emits a SACK, no matter how many post-teardown DATA packets arrive");
			Assert.AreEqual(SctpState.Aborted, server.State);
		}

		/// <summary>
		///     Teardown never touches the receive buffer from a foreign thread while a delivery drain is in
		///     flight; the drain performs the deferred reset itself once it finishes. Proven with a
		///     deterministic interleaving rather than a stress loop: the drain processes "second" and
		///     "third" as two leased deliveries in the same batch (the zero-copy "first" delivery never
		///     goes through the drain at all), and the OnMessage subscriber for "second" runs Abort() to
		///     completion on a separate thread, synchronously, before returning - while the drain still has
		///     "third" left to process. Without the snapshot the drain takes under its own gate, Abort's
		///     Reset clearing the shared delivery list out from under the still-running drain loop would
		///     truncate it: "third" would never be delivered, its lease leaked forever.
		/// </summary>
		[TestMethod]
		public void Abort_FromAnotherThread_DuringAnInFlightDeliveryDrain_HandsOffTheResetInsteadOfRacingIt()
		{
			(_, SctpAssociation server, List<byte[]> _, _) = EstablishPair();

			uint tag = server.LocalVerificationTag;
			uint tsn = unchecked(server.CumulativeTsnAck + 1);

			var deliveredTexts = new List<string>();
			Exception abortException = null;

			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> message) =>
			{
				string text = System.Text.Encoding.UTF8.GetString(message.ToArray());
				deliveredTexts.Add(text);

				if (text == "second")
				{
					var abortThread = new System.Threading.Thread(() =>
					{
						try { server.Abort(); }
						catch (Exception ex) { abortException = ex; }
					});
					abortThread.Start();
					abortThread.Join();
				}
			};

			// Sequences 1 and 2 arrive first and both buffer (leased, WaitForTurn); sequence 0 then
			// arrives, delivered zero-copy AND cascading sequences 1 and 2 into the SAME leased-delivery
			// batch the drain processes - the same shape SctpReceiveTests' ThrowingOnMessageSubscriber
			// test uses, extended to two cascaded deliveries so a truncated drain is provable rather than
			// merely plausible.
			FeedData(server, tag, unchecked(tsn + 2), 12, 2, 1, unordered: false, begin: true, end: true, "third"u8);
			FeedData(server, tag, unchecked(tsn + 1), 12, 1, 1, unordered: false, begin: true, end: true, "second"u8);
			FeedData(server, tag, tsn, 12, 0, 1, unordered: false, begin: true, end: true, "first"u8);

			Assert.IsNull(abortException, "Abort() must not throw while a delivery drain is in flight");
			CollectionAssert.AreEqual(new[] {"first", "second", "third"}, deliveredTexts, "the drain must not be truncated by a concurrent Teardown - a dropped \"third\" is a leaked lease");
			Assert.AreEqual(SctpState.Aborted, server.State);

			// The deferred reset actually ran once the drain finished: the receive buffer's budget is back
			// to full, the same signal Abort_ReleasesParkedReceiveBufferState uses for the inline case.
			Assert.AreEqual(ArwndBudget, server.CurrentArwnd);
		}

		[TestMethod]
		public void Abort_TearsDownAssociation_FiresOnAbortedExactlyOnce_AndReleasesSendQueueLeases()
		{
			(_, SctpAssociation server, List<byte[]> _, Loopback link) = EstablishPair();

			// Give the server outstanding, never-acked send-side data. The client leg is cut first, so
			// nothing can acknowledge the chunk and it stays resident (leased) unless teardown releases
			// it. Cut explicitly rather than relying on the client being slow to ack: whether an ack
			// comes back is the acknowledgement policy's business, and this test is about teardown.
			link.DeliverToClient = false;

			Assert.IsTrue(server.Send(streamId: 1, ppid: 1, new byte[512], unordered: false, maxRetransmits: -1));
			Assert.IsTrue(server.SendQueuedBytes > 0);

			int abortedCount = 0;
			string reason = null;
			server.OnAborted += r =>
			{
				abortedCount++;
				reason = r;
			};

			// Garbage cause data: this association reads no cause taxonomy at all, so it must be
			// tolerated exactly like an empty value would be (hot-path law: never throw on hostile input).
			byte[] abortPacket = BuildAbortPacket(server.LocalVerificationTag, stackalloc byte[] {0xDE, 0xAD, 0xBE, 0xEF, 0x00});
			server.OnPacketReceived(abortPacket);

			Assert.AreEqual(SctpState.Aborted, server.State);
			Assert.AreEqual(1, abortedCount);
			Assert.IsNotNull(reason);
			Assert.AreEqual(0u, server.SendQueuedBytes);

			// Post-teardown: Send must refuse rather than silently queueing into a dead association.
			Assert.IsFalse(server.Send(streamId: 1, ppid: 1, new byte[16], unordered: false, maxRetransmits: -1));

			// A second ABORT (any further teardown-triggering chunk) must not re-fire OnAborted; it lands
			// on the dropped-and-counted path instead - no zombies, but no double-fire either.
			long ignoredBefore = server.IgnoredPacketCount;
			server.OnPacketReceived(BuildAbortPacket(server.LocalVerificationTag));
			Assert.AreEqual(1, abortedCount);
			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
		}

		[TestMethod]
		public void Abort_ReleasesParkedReceiveBufferState()
		{
			(_, SctpAssociation server, List<byte[]> _, _) = EstablishPair();

			// Park a complete, single-chunk ordered message the server cannot deliver yet (stream
			// sequence 5 while the stream's expected next is 0), forcing it into _orderedPending rather
			// than delivering zero-copy - the receive-side lease this test proves teardown releases.
			uint tsn = unchecked(server.CumulativeTsnAck + 1);
			var header = new DataChunkHeader(tsn, streamId: 1, streamSeq: 5, ppid: 7, unordered: false, begin: true, end: true, immediateSack: false);
			byte[] payload = new byte[256];

			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, Port, Port, server.LocalVerificationTag);
			n += header.WriteTo(packet.Slice(n), payload);
			SctpPacket.FinishChecksum(packet.Slice(0, n));
			server.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.IsTrue(server.CurrentArwnd < ArwndBudget, "the parked message should have consumed receive-buffer budget");

			server.OnPacketReceived(BuildAbortPacket(server.LocalVerificationTag));

			Assert.AreEqual(SctpState.Aborted, server.State);
			Assert.AreEqual(ArwndBudget, server.CurrentArwnd);
		}

		[TestMethod]
		public void Shutdown_RepliesWithShutdownAck_AndTearsDownAssociation()
		{
			(_, SctpAssociation server, List<byte[]> serverSent, _) = EstablishPair();

			int abortedCount = 0;
			server.OnAborted += _ => abortedCount++;

			// Unexpected/garbage bytes in the value this association never reads (the real RFC 4960
			// SHUTDOWN chunk carries a Cumulative TSN Ack field; no retransmission-aware teardown is in
			// scope here, so the content must be tolerated regardless).
			byte[] shutdownPacket = BuildShutdownPacket(server.LocalVerificationTag, stackalloc byte[] {0x01, 0x02, 0x03, 0x04});
			server.OnPacketReceived(shutdownPacket);

			Assert.AreEqual(SctpState.Aborted, server.State);
			Assert.AreEqual(1, abortedCount);
			Assert.AreEqual(1, serverSent.Count);

			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(serverSent[0]);
			Assert.IsTrue(enumerator.MoveNext());
			(byte type, byte _, ReadOnlySpan<byte> value) = enumerator.Current;
			Assert.AreEqual((byte) 8, type); // SHUTDOWN-ACK
			Assert.AreEqual(0, value.Length);

			// A SHUTDOWN-COMPLETE arriving afterward is ignored-and-counted like any post-teardown packet,
			// not treated specially, and must not re-fire OnAborted.
			long ignoredBefore = server.IgnoredPacketCount;
			server.OnPacketReceived(BuildShutdownCompletePacket(server.LocalVerificationTag));
			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
			Assert.AreEqual(1, abortedCount);
		}

		/// <summary>
		///     <see cref="SctpAssociation.Teardown" /> never invalidates
		///     <see cref="SctpAssociation.LocalVerificationTag" />, and a signed cookie stays valid for up
		///     to 60s regardless of what has happened to the association since it was minted, so a
		///     network-level retransmit of the client's own, still-perfectly-signed original COOKIE-ECHO -
		///     carrying the correct tag - can arrive well after this side has already torn down. Pins that
		///     <c>HandleCookieEcho</c> rejects such a replay unconditionally once
		///     <see cref="SctpState.Aborted" />, rather than computing "already established" from
		///     <see cref="SctpState.Established" /> alone, which would let the replay resurrect the
		///     association: resetting the send/receive buffers <see cref="SctpAssociation.Abort" /> had
		///     just released, flipping state back to Established, resending COOKIE-ACK, and re-firing
		///     <see cref="SctpAssociation.OnEstablished" /> a second time on an association its owner
		///     already considers dead.
		/// </summary>
		[TestMethod]
		public void RetransmittedCookieEcho_AfterAbort_DoesNotResurrectTheAssociation()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var clientSent = new List<byte[]>();
			var serverSent = new List<byte[]>();

			client = new SctpAssociation(true, Port, ArwndBudget, p =>
			{
				clientSent.Add(p.ToArray());
				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, Port, ArwndBudget, p =>
			{
				serverSent.Add(p.ToArray());
				client.OnPacketReceived(p.ToArray());
			});

			int establishedCount = 0;
			server.OnEstablished += () => establishedCount++;

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(SctpState.Established, server.State);
			Assert.AreEqual(1, establishedCount);

			// The client's own COOKIE-ECHO from the real handshake above, captured so it can be replayed
			// verbatim - a genuine network-level retransmit, not a hand-forged packet.
			byte[] cookieEchoPacket = clientSent.Single(p => ContainsChunkType(p, SctpChunkType.CookieEcho));

			server.Abort();
			Assert.AreEqual(SctpState.Aborted, server.State);

			serverSent.Clear();
			long ignoredBefore = server.IgnoredPacketCount;

			server.OnPacketReceived(cookieEchoPacket);

			Assert.AreEqual(SctpState.Aborted, server.State, "a replayed COOKIE-ECHO must not resurrect an aborted association");
			Assert.AreEqual(1, establishedCount, "OnEstablished must not fire a second time");
			Assert.AreEqual(0, serverSent.Count, "no COOKIE-ACK, or anything else, must be sent once aborted");
			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
		}

		/// <summary>
		///     <c>HandleInit</c> answers every well-formed INIT
		///     with a fresh INIT-ACK regardless of state, by design, for every state except
		///     <see cref="SctpState.Aborted" />, which it must reject. Deliberately aborts a server
		///     association that never validated a COOKIE-ECHO (<see cref="SctpAssociation.LocalVerificationTag" />
		///     is therefore still its default, 0), not an already-established one: an INIT's own
		///     packet-level verification tag is always 0 per RFC 4960 5.1, so against an
		///     already-established-then-aborted server (nonzero tag) the pre-existing packet-level
		///     wrong-tag gate in <c>OnPacketReceived</c> already drops it before <c>HandleInit</c> is ever
		///     reached, which would make this test pass for the wrong reason regardless of whether
		///     <c>HandleInit</c>'s own check exists. With <see cref="SctpAssociation.LocalVerificationTag" />
		///     still 0, that gate does not fire (0 != 0 is false), so the packet does reach
		///     <c>HandleInit</c>, and only its own check can stop it - a real, reachable path: any server
		///     association aborted before its handshake ever completed (an inbound ABORT/SHUTDOWN, or a
		///     local <see cref="SctpAssociation.Abort" />, arriving before a COOKIE-ECHO ever validated).
		/// </summary>
		[TestMethod]
		public void Init_AfterAbortBeforeEverEstablishing_IsIgnoredAndCounted_NoInitAckSent()
		{
			var serverSent = new List<byte[]>();
			var server = new SctpAssociation(false, Port, ArwndBudget, p => serverSent.Add(p.ToArray()));

			server.Abort();
			Assert.AreEqual(SctpState.Aborted, server.State);
			Assert.AreEqual((uint) 0, server.LocalVerificationTag, "precondition: never having validated a cookie, the tag is still its default");
			long ignoredBefore = server.IgnoredPacketCount;

			var freshInit = new InitChunk(RandomTag(), ArwndBudget, ushort.MaxValue, ushort.MaxValue, RandomTag(), forwardTsnSupported: true);
			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, Port, Port, 0); // RFC 4960 5.1: tag 0 on the packet carrying INIT
			n += freshInit.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			server.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.AreEqual(SctpState.Aborted, server.State);
			Assert.AreEqual(0, serverSent.Count, "no INIT-ACK must be sent once aborted, even when the packet-level tag gate does not itself catch it");
			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
		}

		private static uint RandomTag()
		{
			Span<byte> bytes = stackalloc byte[4];
			System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
			return System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes);
		}

		private static bool ContainsChunkType(byte[] packet, byte chunkType)
		{
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(packet);
			while (enumerator.MoveNext())
			{
				(byte type, byte _, ReadOnlySpan<byte> _) = enumerator.Current;
				if (type == chunkType) return true;
			}

			return false;
		}
	}
}
