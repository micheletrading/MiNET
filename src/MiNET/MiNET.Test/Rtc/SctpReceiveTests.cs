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
	}
}