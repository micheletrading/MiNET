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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     The receive side of one established SCTP association, driven directly through
	///     <see cref="SctpAssociation.OnPacketReceived" /> with hand-built DATA packets (the Task 2
	///     codecs), never through a peer association's own outbound DATA path (Task 6's job). A real
	///     client/server handshake still runs first, the same shape <see cref="SctpAssociationHandshakeTests" />
	///     uses, purely to reach <see cref="SctpState.Established" /> with a real, negotiated peer
	///     Initial TSN; everything after that point is hand-crafted.
	/// </summary>
	[TestClass]
	public class SctpReceiveTests
	{
		private static (SctpAssociation Server, List<byte[]> SentByServer, List<(ushort StreamId, uint Ppid, byte[] Payload)> Received) CreateEstablishedPair(uint arwndBudget = 131072)
		{
			var sent = new List<byte[]>();
			var received = new List<(ushort StreamId, uint Ppid, byte[] Payload)>();

			SctpAssociation server = null;
			SctpAssociation client = null;
			client = new SctpAssociation(true, 5000, arwndBudget, p => server.OnPacketReceived(p));
			server = new SctpAssociation(false, 5000, arwndBudget, p =>
			{
				sent.Add(p.ToArray());
				client.OnPacketReceived(p);
			});

			server.OnMessage += (streamId, ppid, message) => received.Add((streamId, ppid, message.ToArray()));

			client.Start();

			Assert.AreEqual(SctpState.Established, server.State);

			// Drop the handshake noise (INIT-ACK, COOKIE-ACK) captured on the way to Established: every
			// test below cares only about what the server sends in reaction to the DATA it feeds it.
			sent.Clear();

			return (server, sent, received);
		}

		private static void FeedData(SctpAssociation receiver, uint verificationTag, uint tsn, ushort streamId, ushort streamSeq, uint ppid, bool unordered, bool begin, bool end, ReadOnlySpan<byte> payload, bool immediateSack = false)
		{
			var header = new DataChunkHeader(tsn, streamId, streamSeq, ppid, unordered, begin, end, immediateSack);

			Span<byte> packet = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, verificationTag);
			n += header.WriteTo(packet.Slice(n), payload);
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			receiver.OnPacketReceived(packet.Slice(0, n));
		}

		[TestMethod]
		public void SingleChunkMessages_DeliverInOrder_OnAnOrderedStream()
		{
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			FeedData(server, tag, tsn, 1, 0, 51, unordered: false, begin: true, end: true, "hello"u8);
			FeedData(server, tag, tsn + 1, 1, 1, 51, unordered: false, begin: true, end: true, "world"u8);

			Assert.AreEqual(2, received.Count);
			Assert.AreEqual((ushort) 1, received[0].StreamId);
			Assert.AreEqual(51u, received[0].Ppid);
			CollectionAssert.AreEqual("hello"u8.ToArray(), received[0].Payload);
			CollectionAssert.AreEqual("world"u8.ToArray(), received[1].Payload);
		}

		[TestMethod]
		public void FragmentedMessage_ReassemblesAcrossThreeChunks_BeginMiddleEnd()
		{
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			FeedData(server, tag, tsn, 2, 0, 77, unordered: false, begin: true, end: false, "AAA"u8);
			FeedData(server, tag, tsn + 1, 2, 0, 77, unordered: false, begin: false, end: false, "BBB"u8);
			FeedData(server, tag, tsn + 2, 2, 0, 77, unordered: false, begin: false, end: true, "CCC"u8);

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual((ushort) 2, received[0].StreamId);
			Assert.AreEqual(77u, received[0].Ppid);
			CollectionAssert.AreEqual("AAABBBCCC"u8.ToArray(), received[0].Payload);
		}

		[TestMethod]
		public void OutOfOrderArrival_DeliversInStreamSequenceOrder_OnAnOrderedStream()
		{
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// Stream sequence 1 arrives first; the stream still expects sequence 0, so this must wait.
			FeedData(server, tag, tsn + 1, 3, 1, 10, unordered: false, begin: true, end: true, "second"u8);
			Assert.AreEqual(0, received.Count);

			// Sequence 0 arrives, unblocking both in stream-sequence order.
			FeedData(server, tag, tsn, 3, 0, 10, unordered: false, begin: true, end: true, "first"u8);

			Assert.AreEqual(2, received.Count);
			CollectionAssert.AreEqual("first"u8.ToArray(), received[0].Payload);
			CollectionAssert.AreEqual("second"u8.ToArray(), received[1].Payload);
		}

		[TestMethod]
		public void UnorderedMessages_DeliverImmediately_RegardlessOfArrivalOrder()
		{
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// The later TSN arrives first; unordered delivery does not wait for the earlier one.
			FeedData(server, tag, tsn + 1, 4, 0, 20, unordered: true, begin: true, end: true, "second"u8);
			Assert.AreEqual(1, received.Count);
			CollectionAssert.AreEqual("second"u8.ToArray(), received[0].Payload);

			FeedData(server, tag, tsn, 4, 0, 20, unordered: true, begin: true, end: true, "first"u8);
			Assert.AreEqual(2, received.Count);
			CollectionAssert.AreEqual("first"u8.ToArray(), received[1].Payload);
		}

		[TestMethod]
		public void Sack_ReportsAGapBlock_AssoonAsATsnIsMissing()
		{
			(SctpAssociation server, List<byte[]> sent, _) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint baseline = server.CumulativeTsnAck; // one behind the peer's Initial TSN
			uint tsn = baseline + 1;

			// tsn itself never arrives; tsn+1 does. A gap is outstanding after this single packet, which
			// is an immediate SACK trigger on its own (isolated from the "every second packet" counter,
			// since this is the only packet sent).
			FeedData(server, tag, tsn + 1, 5, 0, 1, unordered: true, begin: true, end: true, "b"u8);

			Assert.AreEqual(1, sent.Count);

			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(sent[0]);
			Assert.IsTrue(enumerator.MoveNext());
			(byte type, byte _, ReadOnlySpan<byte> value) = enumerator.Current;
			Assert.AreEqual((byte) 3, type); // SACK

			Assert.IsTrue(SackChunk.TryParse(value, out SackChunk sack));
			Assert.AreEqual(baseline, sack.CumulativeTsnAck);
			Assert.AreEqual(1, sack.GapBlocks.Length);
			Assert.AreEqual((ushort) 2, sack.GapBlocks[0].Start); // (tsn+1) - baseline
			Assert.AreEqual((ushort) 2, sack.GapBlocks[0].End);
		}

		[TestMethod]
		public void BudgetExhaustion_DropsData_AndArwndHitsZero()
		{
			// A 10-byte budget: exactly enough to buffer one 10-byte chunk that cannot yet be delivered.
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair(arwndBudget: 10);
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// Ordered, single-chunk, but stream sequence 1 while the stream still expects 0: must be
			// buffered rather than delivered, consuming the entire budget.
			FeedData(server, tag, tsn, 6, 1, 1, unordered: false, begin: true, end: true, new byte[10]);
			Assert.AreEqual(0u, server.CurrentArwnd);
			Assert.AreEqual(0, received.Count);

			// A second such chunk cannot fit: dropped and counted, not buffered, its TSN not recorded as
			// received (the peer is expected to retransmit it).
			FeedData(server, tag, tsn + 1, 6, 2, 1, unordered: false, begin: true, end: true, new byte[10]);
			Assert.AreEqual(1L, server.DataDroppedByBudgetCount);
			Assert.AreEqual(0u, server.CurrentArwnd);
			Assert.AreEqual(0, received.Count);
		}

		/// <summary>
		///     Fix-round regression for Critical finding 1: reassembly must never pull a fragment that
		///     belongs to a different stream into a message just because its TSN falls inside the
		///     [Begin, End] span. Stream 1 sends Begin@T and End@T+2 without ever sending a real T+1
		///     fragment of its own (T+1 belongs to an unrelated, still in-flight stream 2 message); stream
		///     1's message must therefore never complete (it is genuinely missing a piece it never sent),
		///     and - the actual "wedge" the finding describes - stream 2's own in-flight fragment at T+1
		///     must not be silently freed out from under it. Non-theft is proven two ways: no corrupted
		///     stream-1 delivery, and all three fragments' bytes (A, B, C) are still individually held
		///     (nothing was combined-and-freed as a bogus completion), which a byte-accounting bug would
		///     not show but the buggy code's actual behavior does: it wrongly completes, delivers, and
		///     frees all three, so <see cref="SctpAssociation.CurrentArwnd" /> would read back to full budget.
		/// </summary>
		[TestMethod]
		public void FragmentReassembly_NeverStealsAnotherStreamsFragment()
		{
			const uint budget = 100;
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair(budget);
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			FeedData(server, tag, tsn, 1, 0, 1, unordered: true, begin: true, end: false, "A"u8); // stream 1, Begin
			FeedData(server, tag, tsn + 1, 2, 0, 2, unordered: true, begin: true, end: false, "B"u8); // stream 2, Begin - still in-flight
			FeedData(server, tag, tsn + 2, 1, 0, 1, unordered: true, begin: false, end: true, "C"u8); // stream 1, End - missing its own middle

			// Stream 1 must not have delivered a corrupted "A" + stolen "B" + "C": it is genuinely
			// incomplete (its own middle fragment never arrived) and must stay that way.
			Assert.AreEqual(0, received.Count);

			// All three 1-byte fragments (A, B, C) are still individually buffered: stream 1's failed
			// completion attempt did not combine-and-free B (stream 2's own in-flight piece) as if it
			// belonged to stream 1's message.
			Assert.AreEqual(budget - 3, server.CurrentArwnd);
		}

		/// <summary>
		///     Fix-round regression for Important finding 2: the out-of-order TSN set must not grow
		///     without bound. A peer that only ever sends non-contiguous TSNs (skipping the one the
		///     cumulative ack point is actually waiting for) can otherwise grow <c>_gapTsns</c> forever
		///     without ever touching the byte budget (these are all single-chunk unordered messages,
		///     delivered zero-copy, so no bytes are ever buffered). Once the cap is reached, further
		///     distinct gap TSNs are dropped and counted instead.
		/// </summary>
		[TestMethod]
		public void OutOfOrderTsnSet_IsCapped_FurtherGapsAreDroppedAndCounted()
		{
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// `tsn` itself is never sent, so every one of these is a distinct out-of-order TSN.
			for (int i = 0; i < SctpReceiveBuffer.MaxOutOfOrderTsns; i++)
			{
				FeedData(server, tag, tsn + 1 + (uint) i, 9, 0, 1, unordered: true, begin: true, end: true, "x"u8);
			}

			Assert.AreEqual(SctpReceiveBuffer.MaxOutOfOrderTsns, received.Count);
			Assert.AreEqual(0L, server.DataDroppedByGapCapCount);

			// The set is now full: one more distinct gap TSN is dropped and counted, not tracked.
			FeedData(server, tag, tsn + 1 + (uint) SctpReceiveBuffer.MaxOutOfOrderTsns, 9, 0, 1, unordered: true, begin: true, end: true, "y"u8);

			Assert.AreEqual(SctpReceiveBuffer.MaxOutOfOrderTsns, received.Count); // not delivered
			Assert.AreEqual(1L, server.DataDroppedByGapCapCount);
		}

		/// <summary>
		///     Fix-round regression for Important finding 3: two ordered chunks that carry the same
		///     (streamId, streamSeq) under different, fresh TSNs both land in the same ordered-pending
		///     slot; the second overwrites the first. The buffered-byte accounting must reflect only the
		///     surviving entry, not both, or a_rwnd drains toward zero permanently.
		/// </summary>
		[TestMethod]
		public void EnqueueOrderedPending_DuplicateSeqUnderAFreshTsn_DoesNotDoubleCountBufferedBytes()
		{
			(SctpAssociation server, _, _) = CreateEstablishedPair(arwndBudget: 20);
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// Both stream sequence 1 (the stream still expects 0, so neither is due): two different,
			// fresh TSNs land in the same (streamId=7, seq=1) pending slot, the second overwriting the
			// first.
			FeedData(server, tag, tsn, 7, 1, 1, unordered: false, begin: true, end: true, new byte[10]);
			FeedData(server, tag, tsn + 1, 7, 1, 1, unordered: false, begin: true, end: true, new byte[10]);

			// Only the second 10-byte entry is actually still buffered; the first's lease was returned
			// and must not still be counted.
			Assert.AreEqual(10u, server.CurrentArwnd); // budget(20) - buffered(10)
		}

		/// <summary>
		///     Fix-round regression for Important finding 4: a throwing <see cref="SctpAssociation.OnMessage" />
		///     subscriber must not leak the leased buffer, stop later deliveries in the same
		///     <see cref="SctpAssociation.OnPacketReceived" /> call, or propagate out of it (the hot-path
		///     law: a subscriber throw must not kill the transport). One incoming packet (stream sequence 0
		///     arriving after 1 was already buffered) produces two deliveries in a single call: the
		///     zero-copy one and the leased cascade-drained one. The subscriber records what it was called
		///     with and then always throws; both must still be recorded.
		/// </summary>
		[TestMethod]
		public void ThrowingOnMessageSubscriber_DoesNotStopLaterDeliveriesInTheSameBatch()
		{
			(SctpAssociation server, _, _) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			var invokedFor = new List<string>();
			server.OnMessage += (streamId, ppid, message) =>
			{
				invokedFor.Add(System.Text.Encoding.UTF8.GetString(message));
				throw new InvalidOperationException("boom from a subscriber");
			};

			// Sequence 1 arrives first and must be buffered (leased); sequence 0 then arrives, itself
			// delivered zero-copy AND draining the already-buffered sequence 1 as a second, leased
			// delivery - two OnMessage invocations from one incoming packet.
			FeedData(server, tag, tsn + 1, 8, 1, 1, unordered: false, begin: true, end: true, "second"u8);
			FeedData(server, tag, tsn, 8, 0, 1, unordered: false, begin: true, end: true, "first"u8);

			CollectionAssert.AreEqual(new[] { "first", "second" }, invokedFor);
		}

		/// <summary>
		///     Fix-round-3 rework: round 2's "abandon the stale run on a new Begin" rule turned out to be
		///     unsafe under ordinary UDP reordering (see <see cref="SctpReceiveBuffer" />'s class remarks)
		///     and was removed. Under the round-3 design, a same-stream stale Begin simply sits incomplete
		///     holding its own share of the budget - never spliced into a later, unrelated completion (the
		///     invariant round 1 and round 2 both cared about is unchanged), and never discarded just
		///     because a newer message on the same stream showed up. Discard only happens under real
		///     budget pressure via reneging, covered by the tests below.
		/// </summary>
		[TestMethod]
		public void FragmentReassembly_SecondBeginOnSameStream_StaleFirstRunStaysIncompleteAndHoldsBudget()
		{
			const uint budget = 100;
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair(budget);
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			FeedData(server, tag, tsn, 9, 0, 1, unordered: true, begin: true, end: false, "X"u8); // message 1, Begin - stalls, no End ever arrives
			FeedData(server, tag, tsn + 5, 9, 0, 1, unordered: true, begin: true, end: false, "Y"u8); // message 2, Begin - a distinct, later message on the same stream
			FeedData(server, tag, tsn + 6, 9, 0, 1, unordered: true, begin: false, end: true, "Z"u8); // message 2, End - completes

			// Message 2 delivers exactly its own two pieces; message 1's stale "X" is never spliced in.
			Assert.AreEqual(1, received.Count);
			Assert.AreEqual((ushort) 9, received[0].StreamId);
			CollectionAssert.AreEqual("YZ"u8.ToArray(), received[0].Payload);

			// "X" is not discarded: it still holds its 1 byte of budget, waiting for either a real End
			// or reneging under actual budget pressure (neither happens here).
			Assert.AreEqual(budget - 1, server.CurrentArwnd);
			Assert.AreEqual(0L, server.DataRenegedFragmentRunCount);
		}

		/// <summary>
		///     Fix-round-3 new RED test (a): the scenario that proved round 2's abandon-on-new-Begin rule
		///     was actually unsafe. UDP guarantees nothing about arrival order; RFC 4960 6.9 only promises
		///     the SENDER never interleaves two messages of one stream in TSN space, so Begin(msg2)
		///     arriving before End(msg1) is a completely ordinary reorder, not hostile input. Both messages
		///     must still deliver, with correct content, in stream-sequence order.
		/// </summary>
		[TestMethod]
		public void ReorderedFragments_BothMessagesDeliver_EvenWhenSecondBeginArrivesBeforeFirstEnd()
		{
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair();
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// Arrival order: Begin(msg1)@T, Begin(msg2)@T+2, End(msg2)@T+3, End(msg1)@T+1.
			FeedData(server, tag, tsn, 10, 0, 30, unordered: false, begin: true, end: false, "A1"u8); // msg1 Begin, streamSeq 0
			FeedData(server, tag, tsn + 2, 10, 1, 31, unordered: false, begin: true, end: false, "B1"u8); // msg2 Begin, streamSeq 1
			FeedData(server, tag, tsn + 3, 10, 1, 31, unordered: false, begin: false, end: true, "B2"u8); // msg2 End
			FeedData(server, tag, tsn + 1, 10, 0, 30, unordered: false, begin: false, end: true, "A2"u8); // msg1 End

			// Both messages deliver, in stream-sequence order, each with its own correct content.
			Assert.AreEqual(2, received.Count);
			Assert.AreEqual((ushort) 10, received[0].StreamId);
			Assert.AreEqual(30u, received[0].Ppid);
			CollectionAssert.AreEqual("A1A2"u8.ToArray(), received[0].Payload);
			Assert.AreEqual((ushort) 10, received[1].StreamId);
			Assert.AreEqual(31u, received[1].Ppid);
			CollectionAssert.AreEqual("B1B2"u8.ToArray(), received[1].Payload);
		}

		/// <summary>
		///     Fix-round-3 new RED test (b): under real budget pressure, the oldest renegable incomplete
		///     run is discarded to make room - leases returned, its TSNs struck from the next SACK's gap
		///     blocks (RFC 4960 6.2: this is what makes reneging legal, since the peer must be told to
		///     retransmit), and a later retransmit of those TSNs is accepted as novel data rather than
		///     dropped as a duplicate.
		/// </summary>
		[TestMethod]
		public void BudgetPressure_RenegesOldestEligibleRun_ThenAdmitsTheNewData()
		{
			const uint budget = 10;
			(SctpAssociation server, List<byte[]> sent, _) = CreateEstablishedPair(budget);
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// A stalled, renegable fragment: far ahead of the cumulative ack (a real gap in front of it,
			// so it never folds into cumulative), Begin only, no End ever arrives. 5 bytes.
			uint staleTsn = tsn + 10;
			FeedData(server, tag, staleTsn, 20, 0, 1, unordered: true, begin: true, end: false, "AAAAA"u8);
			Assert.AreEqual(budget - 5, server.CurrentArwnd);

			// New data needing 6 more bytes: 5 + 6 = 11 > 10, so it does not fit until the stalled run
			// is reneged; once reneged, the new fragment is admitted.
			sent.Clear();
			uint newTsn = tsn + 20;
			FeedData(server, tag, newTsn, 21, 0, 2, unordered: true, begin: true, end: false, "BBBBBB"u8);

			Assert.AreEqual(1L, server.DataRenegedFragmentRunCount);
			Assert.AreEqual(budget - 6, server.CurrentArwnd); // only the new fragment remains buffered

			// A gap is always outstanding here (cumulative never advances past its starting point), so
			// this packet triggered an immediate SACK: the reneged TSN must no longer appear in it.
			Assert.AreEqual(1, sent.Count);
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(sent[0]);
			Assert.IsTrue(enumerator.MoveNext());
			(byte type, byte _, ReadOnlySpan<byte> value) = enumerator.Current;
			Assert.AreEqual((byte) 3, type);
			Assert.IsTrue(SackChunk.TryParse(value, out SackChunk sack));

			bool staleStillReported = false;
			foreach (SackChunk.GapBlock block in sack.GapBlocks)
			{
				if (staleTsn >= sack.CumulativeTsnAck + block.Start && staleTsn <= sack.CumulativeTsnAck + block.End) staleStillReported = true;
			}
			Assert.IsFalse(staleStillReported);

			// The peer eventually retransmits the reneged TSN: it is accepted as genuinely novel data
			// (needing its own room, triggering a second reneging of what is now the oldest run) rather
			// than silently dropped as a duplicate.
			FeedData(server, tag, staleTsn, 20, 0, 1, unordered: true, begin: true, end: false, "AAAAA"u8);
			Assert.AreEqual(2L, server.DataRenegedFragmentRunCount);
		}

		/// <summary>
		///     Fix-round-3 new RED test (c): a run is only ever a reneging candidate if none of its
		///     fragments' TSNs is at or below the cumulative ack. A TSN that folds directly into the
		///     cumulative ack the instant it arrives (because it happened to be exactly the next expected
		///     one) is binding - the peer will never retransmit it again - so a run holding one must
		///     survive budget pressure untouched; the incoming chunk that couldn't be admitted is dropped
		///     and counted the ordinary way instead.
		/// </summary>
		[TestMethod]
		public void BudgetPressure_NeverRenegesARunCoveredByTheCumulativeAck()
		{
			const uint budget = 10;
			(SctpAssociation server, _, List<(ushort StreamId, uint Ppid, byte[] Payload)> received) = CreateEstablishedPair(budget);
			uint tag = server.LocalVerificationTag;
			uint tsn = server.CumulativeTsnAck + 1;

			// This fragment's TSN is exactly the next expected, so it folds straight into the cumulative
			// ack point the instant it is recorded, even though the message (Begin only, no End) is
			// still incomplete.
			FeedData(server, tag, tsn, 30, 0, 1, unordered: true, begin: true, end: false, "AAAAA"u8); // 5 bytes
			Assert.AreEqual(tsn, server.CumulativeTsnAck);
			Assert.AreEqual(budget - 5, server.CurrentArwnd);

			// New data needing 6 more bytes: 5 + 6 = 11 > 10, but the only incomplete run is not
			// renegable, so nothing is discarded and the new fragment is dropped instead.
			uint newTsn = tsn + 20;
			FeedData(server, tag, newTsn, 31, 0, 2, unordered: true, begin: true, end: false, "BBBBBB"u8);

			Assert.AreEqual(0L, server.DataRenegedFragmentRunCount);
			Assert.AreEqual(1L, server.DataDroppedByBudgetCount);
			Assert.AreEqual(budget - 5, server.CurrentArwnd); // unchanged: still just the protected fragment

			// The protected run survives intact and still completes normally later.
			FeedData(server, tag, tsn + 1, 30, 0, 1, unordered: true, begin: false, end: true, "ZZZZZ"u8);
			Assert.AreEqual(1, received.Count);
			CollectionAssert.AreEqual("AAAAAZZZZZ"u8.ToArray(), received[0].Payload);
		}
	}
}