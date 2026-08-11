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

		/// <summary>Establishes a client/server pair, exactly like <see cref="SctpAssociationHandshakeTests" />'s own handshake test, but also captures every packet the server sends so a test can inspect the server's replies directly.</summary>
		private static (SctpAssociation Client, SctpAssociation Server, List<byte[]> ServerSent) EstablishPair()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var serverSent = new List<byte[]>();

			client = new SctpAssociation(true, Port, ArwndBudget, p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(false, Port, ArwndBudget, p =>
			{
				serverSent.Add(p.ToArray());
				client.OnPacketReceived(p.ToArray());
			});

			client.Start();

			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(SctpState.Established, server.State);
			serverSent.Clear();

			return (client, server, serverSent);
		}

		[TestMethod]
		public void Heartbeat_OnEstablishedAssociation_IsAnsweredWithVerbatimInfoBytes()
		{
			(_, SctpAssociation server, List<byte[]> serverSent) = EstablishPair();

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
			(_, SctpAssociation server, List<byte[]> serverSent) = EstablishPair();

			byte[] truncated = BuildRawChunkPacket(server.LocalVerificationTag, SctpChunkType.Heartbeat, stackalloc byte[2]);
			long ignoredBefore = server.IgnoredPacketCount;

			server.OnPacketReceived(truncated);

			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
			Assert.AreEqual(0, serverSent.Count);
			Assert.AreEqual(SctpState.Established, server.State);
		}

		[TestMethod]
		public void Abort_TearsDownAssociation_FiresOnAbortedExactlyOnce_AndReleasesSendQueueLeases()
		{
			(_, SctpAssociation server, List<byte[]> _) = EstablishPair();

			// Give the server outstanding, never-acked send-side data: its own sendPacket feeds the
			// client, but nothing here ever drives the client's SACK back once the server is torn down
			// mid-flight, so this chunk stays resident (leased) unless teardown releases it.
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
			(_, SctpAssociation server, List<byte[]> _) = EstablishPair();

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
			(_, SctpAssociation server, List<byte[]> serverSent) = EstablishPair();

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
		///     Fix round, Critical finding 1: <see cref="SctpAssociation.Teardown" /> never invalidates
		///     <see cref="SctpAssociation.LocalVerificationTag" />, and a signed cookie stays valid for up
		///     to 60s regardless of what has happened to the association since it was minted, so a
		///     network-level retransmit of the client's own, still-perfectly-signed original COOKIE-ECHO -
		///     carrying the correct tag - can arrive well after this side has already torn down. Before the
		///     fix, <c>HandleCookieEcho</c> computed "already established" only from
		///     <see cref="SctpState.Established" />, so this replay resurrected the association: reset the
		///     send/receive buffers <see cref="SctpAssociation.Abort" /> had just released, flipped state
		///     back to Established, resent COOKIE-ACK, and re-fired <see cref="SctpAssociation.OnEstablished" />
		///     a second time on an association its owner already considers dead.
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
		///     Fix round, Critical finding 1's audit: <c>HandleInit</c> answered every well-formed INIT
		///     with a fresh INIT-ACK regardless of state, by design, for every state except
		///     <see cref="SctpState.Aborted" /> - which it never checked. Deliberately aborts a server
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
