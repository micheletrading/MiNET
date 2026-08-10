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

		[TestMethod]
		public void PeerAdvertisesTinyWindow_SenderQueuesInsteadOfSending()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var sentByClient = new List<byte[]>();

			client = new SctpAssociation(true, 5000, 131072, p =>
			{
				sentByClient.Add(p.ToArray());
				server.OnPacketReceived(p.ToArray());
			});
			// The server's own arwndBudget is what it advertises to the client during the handshake; the
			// client's peer-arwnd becomes this tiny value the moment the association is established.
			server = new SctpAssociation(false, 5000, 10, p => client.OnPacketReceived(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			sentByClient.Clear(); // drop the handshake packets from the count below

			byte[] message = new byte[50];
			Assert.IsTrue(client.Send(streamId: 1, ppid: 1, message, unordered: true, maxRetransmits: -1));

			// Accepted into the send queue (Send returns true - the send-queue budget is independent of
			// the peer's window), but the peer's 10-byte advertised window cannot fit even this one
			// below-threshold, single-chunk message: nothing was actually put on the wire.
			Assert.AreEqual(0, sentByClient.Count);
			Assert.AreEqual(50u, client.SendQueuedBytes);
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
	}
}