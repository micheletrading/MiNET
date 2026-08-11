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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     The send half of an established SCTP association: fragmentation/reassembly round-tripped
	///     through two associations wired back-to-back via <see cref="SctpAssociation.PacketSender" />
	///     (the same synchronous loopback shape <see cref="SctpAssociationHandshakeTests" /> and
	///     <see cref="SctpReceiveTests" /> already use), retransmission under a lossy pump delegate, and
	///     RTO/T3-rtx timing driven through <see cref="SctpAssociation.ClockNowMillis" /> - a settable
	///     seam so these tests advance a fake clock instantly instead of paying real wall-clock delay up
	///     to 10s (RTO max) per retransmit round.
	/// </summary>
	[TestClass]
	public class SctpSendTests
	{
		/// <summary>Decodes the single DATA chunk's TSN out of a captured outbound packet (every packet in these tests carries at most one, per <see cref="SctpAssociation.Flush" />'s own remarks on chunk sizing).</summary>
		private static uint ExtractDataTsn(byte[] packet)
		{
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(packet);
			Assert.IsTrue(enumerator.MoveNext());
			(byte type, byte flags, ReadOnlySpan<byte> value) = enumerator.Current;
			Assert.AreEqual(SctpChunkType.Data, type);
			Assert.IsTrue(DataChunkHeader.TryParse(flags, value, out DataChunkHeader header, out _));
			return header.Tsn;
		}

		/// <summary>Hand-builds and delivers a bare SACK packet, the same shape <see cref="SctpReceiveTests" />'s <c>FeedForwardTsn</c> uses for FORWARD-TSN - lets a test act as a hostile or merely out-of-order peer, which no well-behaved association here would ever produce on its own.</summary>
		private static void FeedSack(SctpAssociation receiver, uint verificationTag, uint cumulativeTsnAck, uint arwnd, SackChunk.GapBlock[] gapBlocks = null)
		{
			var sack = new SackChunk(cumulativeTsnAck, arwnd, gapBlocks ?? Array.Empty<SackChunk.GapBlock>(), Array.Empty<uint>());

			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, verificationTag);
			n += sack.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			receiver.OnPacketReceived(packetArray.AsMemory(0, n));
		}

		[TestMethod]
		public void LargeMessage_FragmentsAndReassembles_RoundTripsThroughTwoAssociations()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var serverReceived = new List<(ushort StreamId, uint Ppid, byte[] Payload)>();

			client = new SctpAssociation(true, 5000, 262144, p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(false, 5000, 262144, p => client.OnPacketReceived(p.ToArray()));
			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> message) => serverReceived.Add((streamId, ppid, message.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			byte[] message = new byte[10 * 1024];
			new Random(42).NextBytes(message);

			Assert.IsTrue(client.Send(streamId: 3, ppid: 99, message, unordered: false, maxRetransmits: -1));

			// The congestion window (starts at 4*MTU, grows one MTU per acking SACK) should cascade this
			// synchronously inside Send() itself via the loopback wiring's reentrant SACK replies; this is
			// a safety net against real-clock timing assumptions this test does not want to depend on.
			for (int i = 0; i < 20 && serverReceived.Count == 0; i++)
			{
				client.OnTick();
				server.OnTick();
			}

			Assert.AreEqual(1, serverReceived.Count);
			Assert.AreEqual((ushort) 3, serverReceived[0].StreamId);
			Assert.AreEqual(99u, serverReceived[0].Ppid);
			CollectionAssert.AreEqual(message, serverReceived[0].Payload);
		}

		[TestMethod]
		public void MessageLost_IsRetransmitted_AndStillArrives()
		{
			int clientToServerCount = 0;
			bool dropEnabled = false;

			SctpAssociation server = null;
			SctpAssociation client = null;
			var serverReceived = new List<byte[]>();

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				clientToServerCount++;
				if (dropEnabled && clientToServerCount % 3 == 0) return; // drop every 3rd packet from the client
				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));
			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> message) => serverReceived.Add(message.ToArray());

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			long fakeNow = 1_000_000;
			client.ClockNowMillis = () => fakeNow;

			// The handshake already consumed two client->server packets (INIT, COOKIE-ECHO), so the very
			// next one - this message's only DATA chunk - lands on the 3rd and is dropped.
			dropEnabled = true;

			byte[] message = new byte[500];
			new Random(1).NextBytes(message);
			Assert.IsTrue(client.Send(streamId: 1, ppid: 42, message, unordered: false, maxRetransmits: -1));
			Assert.AreEqual(0, serverReceived.Count); // the only transmission so far was dropped

			// Advance past the initial 1000ms RTO so T3-rtx fires and retransmits.
			fakeNow += 1500;
			client.OnTick();

			Assert.AreEqual(1, serverReceived.Count);
			CollectionAssert.AreEqual(message, serverReceived[0]);
			Assert.IsTrue(client.SendRetransmitCount >= 1);
		}

		[TestMethod]
		public void UnreliableMessage_AbandonedAfterLoss_ProducesForwardTsn_ReceiverCumulativeAdvances_WithoutDelivery()
		{
			bool dropNext = false;

			SctpAssociation server = null;
			SctpAssociation client = null;
			var serverReceived = new List<byte[]>();

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				if (dropNext)
				{
					dropNext = false;
					return;
				}

				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));
			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> message) => serverReceived.Add(message.ToArray());

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			long fakeNow = 3_000_000;
			client.ClockNowMillis = () => fakeNow;

			uint cumulativeBefore = server.CumulativeTsnAck;

			dropNext = true; // drops this message's one and only DATA chunk
			Assert.IsTrue(client.Send(streamId: 2, ppid: 5, "gone"u8, unordered: true, maxRetransmits: 0));

			// maxRetransmits 0: the moment T3-rtx fires once, the retransmit count (1) already exceeds the
			// budget (0), so the chunk is abandoned outright rather than resent.
			fakeNow += 1500;
			client.OnTick();

			Assert.AreEqual(0, serverReceived.Count); // never delivered
			Assert.AreEqual(1L, client.SendAbandonedCount);
			Assert.AreEqual(unchecked(cumulativeBefore + 1), server.CumulativeTsnAck); // FORWARD-TSN carried the receiver past it anyway
		}

		/// <summary>
		///     A peer that advertises a permanently zero (or too-tiny) a_rwnd must not stall the
		///     head-of-line message forever: RFC 4960 6.1 rule A's zero-window probe means that once
		///     nothing is in flight, the association may always put one chunk on the wire regardless of
		///     the advertised window, so the peer gets a chance to ack and reopen it. The message here is
		///     small, unordered, and under the fragmentation threshold, so it delivers zero-copy on arrival
		///     regardless of the server's own (zero) buffering budget - that isolates this test to the
		///     SEND-side window gate, not receive-side budget accounting (covered elsewhere in this file's
		///     sibling, SctpReceiveTests.cs).
		/// </summary>
		[TestMethod]
		public void PeerAdvertisesZeroWindow_ZeroWindowProbeStillDeliversTheMessage()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var sentByClient = new List<byte[]>();
			var serverReceived = new List<byte[]>();

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				sentByClient.Add(p.ToArray());
				server.OnPacketReceived(p.ToArray());
			});
			// The server's own arwndBudget is what it advertises to the client during the handshake and in
			// every SACK: a permanently zero receive window from the client's point of view.
			server = new SctpAssociation(false, 5000, 0, p => client.OnPacketReceived(p.ToArray()));
			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> message) => serverReceived.Add(message.ToArray());

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			sentByClient.Clear(); // drop the handshake packets from the count below

			byte[] message = new byte[20];
			new Random(7).NextBytes(message);
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, message, unordered: true, maxRetransmits: -1));

			// Without the zero-window probe, a_rwnd 0 gates the head-of-line chunk forever and this never
			// sends at all, so the server never sees it.
			Assert.AreEqual(1, sentByClient.Count);
			Assert.AreEqual(1, serverReceived.Count);
			CollectionAssert.AreEqual(message, serverReceived[0]);
		}

		[TestMethod]
		public void NoAckEverArrives_RtoBacksOffExponentially_ObservableViaCounters()
		{
			bool dropEnabled = false;

			SctpAssociation server = null;
			SctpAssociation client = null;

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				if (dropEnabled) return;
				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			long fakeNow = 5_000_000;
			client.ClockNowMillis = () => fakeNow;

			dropEnabled = true; // every packet from the client vanishes: the peer can never ack
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "silence"u8, unordered: true, maxRetransmits: -1));

			fakeNow += 1500; // past the initial 1000ms RTO
			client.OnTick();
			Assert.AreEqual(1L, client.SendTimeoutCount);
			Assert.AreEqual(1L, client.SendRetransmitCount);
			long rtoAfterFirstTimeout = client.SendRtoMillis;
			Assert.IsTrue(rtoAfterFirstTimeout > 1000, "RTO must have backed off past its 1000ms initial value.");

			fakeNow += rtoAfterFirstTimeout + 500;
			client.OnTick();
			Assert.AreEqual(2L, client.SendTimeoutCount);
			Assert.AreEqual(2L, client.SendRetransmitCount);
			Assert.IsTrue(client.SendRtoMillis > rtoAfterFirstTimeout, "RTO must keep backing off across repeated timeouts.");

			// Fully reliable (maxRetransmits -1): never abandoned no matter how many timeouts occur.
			Assert.AreEqual(0L, client.SendAbandonedCount);
		}

		/// <summary>
		///     RFC 4960 6.2.1: a SACK acking a TSN newer than anything this queue has ever actually
		///     transmitted - hostile, corrupt, or misdelivered, since a well-behaved peer can only ack what
		///     it received - is dropped whole, before any processing, rather than freeing queued-but-never-
		///     sent chunks and corrupting the ack point into believing data was delivered that never left
		///     the machine. Proven two ways: nothing is freed by the hostile SACK (queued bytes unchanged),
		///     and a later LEGITIMATE SACK acking the real highest transmitted TSN still works normally -
		///     if the hostile one had corrupted the ack point forward, this genuinely-older ack would
		///     itself now read as stale and be rejected too, so the still-queued chunk would never
		///     transmit.
		/// </summary>
		[TestMethod]
		public void Sack_CumAckNewerThanAnythingTransmitted_IsDroppedWhole_QueueAndAckPointUnaffected()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var sentByClient = new List<byte[]>();
			bool relayToServer = true;

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				sentByClient.Add(p.ToArray());
				if (relayToServer) server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			sentByClient.Clear();

			// From here on the real server is disconnected: only the hand-crafted SACKs below reach the
			// client, so cwnd growth is fully under this test's control - a real, responsive server would
			// otherwise SACK the 2nd DATA packet immediately (its own every-2nd-packet rule) and grow cwnd
			// synchronously mid-flush, letting more than 4 chunks out in the very first burst below.
			relayToServer = false;

			// 5 fragments of 1024/1024/1024/1024/924 bytes: cwnd (4*1200=4800) admits exactly the first 4
			// (4*1040 on-wire bytes = 4160 <= 4800; a 5th would need 5200 > 4800), leaving the 5th genuinely
			// queued, never transmitted.
			byte[] message = new byte[5 * 1024 - 100];
			new Random(3).NextBytes(message);
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, message, unordered: false, maxRetransmits: -1));

			Assert.AreEqual(4, sentByClient.Count); // only 4 chunks actually left the wire
			uint queuedBefore = client.SendQueuedBytes;
			Assert.AreEqual((uint) message.Length, queuedBefore); // nothing acked yet: all 5 pieces still resident

			uint highestTransmittedTsn = ExtractDataTsn(sentByClient[^1]);
			// A packet addressed TO the client carries the client's OWN tag (RFC 4960 5.1) - the value
			// the server was told to use when sending to it - not the tag the client uses when addressing
			// the server. Now that OnPacketReceived's generic tag gate actually enforces this (see the
			// coordinator's round-2 review), a hand-crafted SACK using the wrong one would be dropped by
			// that gate before ever reaching HandleSack, exercising the wrong thing entirely.
			uint tag = client.LocalVerificationTag;

			// A hostile SACK: cumAck 5 beyond anything this association ever put on the wire.
			FeedSack(client, tag, unchecked(highestTransmittedTsn + 5), arwnd: 131072);

			Assert.AreEqual(1L, client.SacksDroppedFutureCumAck);
			Assert.AreEqual(queuedBefore, client.SendQueuedBytes); // nothing freed

			// A legitimate SACK acking exactly what was really transmitted still works normally afterward.
			sentByClient.Clear();
			FeedSack(client, tag, highestTransmittedTsn, arwnd: 131072);

			Assert.AreEqual(queuedBefore - 4 * 1024u, client.SendQueuedBytes); // the 4 real chunks freed; the 5th (924 bytes) still resident
			Assert.AreEqual(1, sentByClient.Count); // the previously-queued 5th chunk transmitted normally
		}

		/// <summary>
		///     RFC 4960 6.2.1: a SACK whose cumulative ack is OLDER than the current ack point is a stale,
		///     reordered duplicate and is dropped entirely - its gap reports must not feed the
		///     fast-retransmit duplicate counter or mark any chunk, which could otherwise fire a spurious
		///     fast retransmit off a stale anchor. Proven by requiring exactly three LEGITIMATE
		///     same-cumAck gap SACKs after the stale one to trigger fast retransmit: if the stale SACK had
		///     counted toward the duplicate tally, only two more would be needed. The stale SACK's advertised
		///     rwnd (deliberately different from the real one) must also never be adopted. An EQUAL cumAck
		///     (the ordinary duplicate-report shape) is a different case, covered by the "three legitimate"
		///     half of this same test actually working at all.
		/// </summary>
		[TestMethod]
		public void Sack_StaleCumAck_IsDroppedWhole_DupCounterAndRwndUnaffected_ThreeLegitimateDupsStillFastRetransmit()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var sentByClient = new List<byte[]>();
			bool relayToServer = true;

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				sentByClient.Add(p.ToArray());
				if (relayToServer) server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			sentByClient.Clear();

			// From here on the real server is disconnected: only the hand-crafted SACKs below reach the
			// client, so a real server's own SACK (its every-2nd-packet rule would fire after chunk 2)
			// cannot interfere with this test's own duplicate-report count.
			relayToServer = false;

			// Three small unordered single-chunk messages: three separate DATA chunks, all comfortably
			// within cwnd, so all three transmit in these three calls.
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "aaa"u8, unordered: true, maxRetransmits: -1));
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "bbb"u8, unordered: true, maxRetransmits: -1));
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "ccc"u8, unordered: true, maxRetransmits: -1));
			Assert.AreEqual(3, sentByClient.Count);

			uint tsn1 = ExtractDataTsn(sentByClient[0]);
			uint tsn3 = ExtractDataTsn(sentByClient[2]);
			// See the sibling test's own remark: a packet addressed TO the client must carry the
			// client's own tag, not the tag it uses to address the server.
			uint tag = client.LocalVerificationTag;

			// Establish an ack point: chunk 1 acked, chunks 2 and 3 remain outstanding.
			FeedSack(client, tag, tsn1, arwnd: 131072);
			uint originalArwnd = client.PeerArwnd;

			// A stale SACK: cumAck OLDER than the current ack point (tsn1), carrying a gap report and a
			// wildly different rwnd - none of it may be trusted.
			var gap = new[] {new SackChunk.GapBlock((ushort) (tsn3 - tsn1), (ushort) (tsn3 - tsn1))};
			FeedSack(client, tag, unchecked(tsn1 - 1), arwnd: 1, gap);

			Assert.AreEqual(1L, client.SacksDroppedStale);
			Assert.AreEqual(0L, client.SendFastRetransmitCount); // not fired by the stale report
			Assert.AreEqual(originalArwnd, client.PeerArwnd); // rwnd untouched by the stale SACK's advertised value

			// Three legitimate same-cumAck (stuck) gap SACKs are still required to trigger fast retransmit.
			FeedSack(client, tag, tsn1, arwnd: 131072, gap);
			Assert.AreEqual(0L, client.SendFastRetransmitCount);
			FeedSack(client, tag, tsn1, arwnd: 131072, gap);
			Assert.AreEqual(0L, client.SendFastRetransmitCount);
			FeedSack(client, tag, tsn1, arwnd: 131072, gap);
			Assert.AreEqual(1L, client.SendFastRetransmitCount);
		}

		/// <summary>
		///     Fast retransmit must actually put the marked chunk back on the wire, not merely increment
		///     its own counter: the counter fires regardless of whether <c>PeekReadyToSend</c> can still
		///     reach the chunk it was just told is due again. Arranged so the send-side resume cursor has
		///     already advanced past the head chunk (via the first, acking SACK's own trailing
		///     <c>Flush</c>) before the third duplicate SACK marks it - the exact shape
		///     <see cref="Sack_StaleCumAck_IsDroppedWhole_DupCounterAndRwndUnaffected_ThreeLegitimateDupsStillFastRetransmit" />
		///     already builds, extended to assert the resend itself.
		/// </summary>
		[TestMethod]
		public void FastRetransmit_ResendsTheMarkedChunkThroughThePump_EvenWhenTheCursorHasAdvancedPastIt()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var sentByClient = new List<byte[]>();
			bool relayToServer = true;

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				sentByClient.Add(p.ToArray());
				if (relayToServer) server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			sentByClient.Clear();
			relayToServer = false; // the real server is disconnected: only the hand-crafted SACKs below reach the client

			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "aaa"u8, unordered: true, maxRetransmits: -1));
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "bbb"u8, unordered: true, maxRetransmits: -1));
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "ccc"u8, unordered: true, maxRetransmits: -1));
			Assert.AreEqual(3, sentByClient.Count);

			uint tsn1 = ExtractDataTsn(sentByClient[0]);
			uint tsn2 = ExtractDataTsn(sentByClient[1]);
			uint tsn3 = ExtractDataTsn(sentByClient[2]);
			uint tag = client.LocalVerificationTag;

			// Acks chunk 1: the cumulative-ack removal loop resets the cursor to the new head (chunk 2),
			// then this SACK's own trailing Flush (HandleSack always calls it once a SACK is accepted)
			// finds nothing due from chunk 2 onward and advances the cursor past both remaining chunks to
			// the tail - the cursor is now sitting past chunk 2 before chunk 2 is ever marked.
			FeedSack(client, tag, tsn1, arwnd: 131072);

			var gap = new[] {new SackChunk.GapBlock((ushort) (tsn3 - tsn1), (ushort) (tsn3 - tsn1))};
			FeedSack(client, tag, tsn1, arwnd: 131072, gap);
			FeedSack(client, tag, tsn1, arwnd: 131072, gap);

			sentByClient.Clear();
			FeedSack(client, tag, tsn1, arwnd: 131072, gap); // third duplicate: fast retransmit fires

			Assert.AreEqual(1L, client.SendFastRetransmitCount);
			Assert.AreEqual(1, sentByClient.Count, "the fast-retransmitted chunk must actually reappear on the wire, not just increment the counter");
			Assert.AreEqual(tsn2, ExtractDataTsn(sentByClient[0]));
		}

		/// <summary>Same shape as <see cref="SctpTeardownTests" />'s own private helper: whether a captured packet carries a chunk of the given type.</summary>
		private static bool ContainsChunkType(ReadOnlySpan<byte> packet, byte chunkType)
		{
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(packet);
			while (enumerator.MoveNext())
			{
				(byte type, byte _, ReadOnlySpan<byte> _) = enumerator.Current;
				if (type == chunkType) return true;
			}

			return false;
		}

		/// <summary>
		///     A batch of ordered, unreliable (maxRetransmits: 0) small messages, all abandoned together
		///     by a single T3 timeout, produces one valid FORWARD-TSN: the pair list is bounded by what one
		///     SctpPacket.MaxSize packet holds, but the cumulative target it advertises still covers every
		///     abandoned message, truncated pairs or not.
		/// </summary>
		[TestMethod]
		public void ManySmallOrderedMessagesAbandonedTogether_ForwardTsn_CumulativeAdvancesFully_WithBoundedPairsAndNoThrow()
		{
			const int messageCount = 400; // exceeds the ~295 pairs one FORWARD-TSN chunk can hold

			bool dropEnabled = false;
			SctpAssociation server = null;
			SctpAssociation client = null;

			client = new SctpAssociation(true, 5000, 1_048_576, p =>
			{
				if (dropEnabled) return;
				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 1_048_576, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			long fakeNow = 10_000_000;
			client.ClockNowMillis = () => fakeNow;

			uint baseline = server.CumulativeTsnAck;

			dropEnabled = true; // every DATA chunk from here on vanishes: the server never acks any of them
			for (int i = 0; i < messageCount; i++)
			{
				Assert.IsTrue(client.Send(streamId: 1, ppid: 1, new[] {(byte) i}, unordered: false, maxRetransmits: 0));
			}

			fakeNow += 1500; // past the initial 1000ms RTO
			dropEnabled = false; // let the resulting FORWARD-TSN (nothing else is due) reach the server
			client.OnTick();

			Assert.AreEqual((long) messageCount, client.SendAbandonedCount);
			Assert.AreEqual(unchecked(baseline + (uint) messageCount), server.CumulativeTsnAck);
		}

		/// <summary>
		///     RFC 3758 5.2: a FORWARD-TSN this side already advertised is retransmitted, like a lost DATA
		///     chunk, whenever the peer's own SACK still has not caught up to it - so a single lost
		///     FORWARD-TSN does not leave the association permanently wedged with a gap neither side can
		///     ever clear.
		/// </summary>
		[TestMethod]
		public void LostForwardTsn_IsRetransmittedOnTimeout_ReceiverCumulativeEventuallyAdvances()
		{
			bool dropData = false;
			bool dropForwardTsn = false;

			SctpAssociation server = null;
			SctpAssociation client = null;

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				if (dropData && ContainsChunkType(p, SctpChunkType.Data))
				{
					dropData = false;
					return;
				}

				if (dropForwardTsn && ContainsChunkType(p, SctpChunkType.ForwardTsn))
				{
					dropForwardTsn = false;
					return;
				}

				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			long fakeNow = 4_000_000;
			client.ClockNowMillis = () => fakeNow;

			uint cumulativeBefore = server.CumulativeTsnAck;

			dropData = true; // this message's one and only DATA chunk never arrives
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, "gone"u8, unordered: true, maxRetransmits: 0));

			dropForwardTsn = true; // neither does the FORWARD-TSN its abandonment produces
			fakeNow += 1500; // past the initial RTO: T3 fires, abandons the chunk, sends (and loses) FORWARD-TSN #1
			client.OnTick();

			Assert.AreEqual(1L, client.SendAbandonedCount);
			Assert.AreEqual(cumulativeBefore, server.CumulativeTsnAck); // the lost FORWARD-TSN never reached the server

			// Past another RTO: nothing is left to time out (the chunk is already abandoned), so only a
			// FORWARD-TSN-specific re-arm keeps T3 alive here at all - proving it does is the point of this
			// second tick.
			fakeNow += client.SendRtoMillis + 500;
			client.OnTick();

			Assert.AreEqual(unchecked(cumulativeBefore + 1), server.CumulativeTsnAck);
		}
	}
}