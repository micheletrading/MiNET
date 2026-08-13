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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     The transport-culture ruling on the send side: an established association never refuses and
	///     never drops a queued message (reliable is absolute), and pressure surfaces to the one bulk
	///     producer as <see cref="SctpAssociation.HasSendRoom" /> going false plus a
	///     <see cref="SctpAssociation.WhenSendRoom" /> wake when SACKs open the window again - park,
	///     never lose. The withheld-SACK harness is the same manual-pump shape
	///     <see cref="SctpSendTests" /> uses for loss.
	/// </summary>
	[TestClass]
	public class SctpBackpressureTests
	{
		/// <summary>
		///     Wires a client whose outbound packets flow to the server normally, while the server's
		///     replies queue in <paramref name="serverOutbox" /> until pumped: SACKs are withheld, so
		///     sent bytes stay resident and the send window genuinely closes.
		/// </summary>
		private static (SctpAssociation Client, SctpAssociation Server) BuildPair(List<byte[]> serverOutbox)
		{
			SctpAssociation server = null;
			SctpAssociation client = null;

			client = new SctpAssociation(true, 5000, 262144, p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(false, 5000, 262144, p => serverOutbox.Add(p.ToArray()));

			client.Start();

			// The handshake needs the server's replies (INIT-ACK, COOKIE-ACK) to actually arrive.
			for (int i = 0; i < 10 && client.State != SctpState.Established; i++)
			{
				PumpAll(serverOutbox, client);
			}

			Assert.AreEqual(SctpState.Established, client.State);
			return (client, server);
		}

		private static void PumpAll(List<byte[]> outbox, SctpAssociation to)
		{
			List<byte[]> batch = new(outbox);
			outbox.Clear();
			foreach (byte[] packet in batch) to.OnPacketReceived(packet);
		}

		[TestMethod]
		public void WindowFull_HasSendRoomGoesFalse_SackReopensAndSignals()
		{
			var serverOutbox = new List<byte[]>();
			(SctpAssociation client, SctpAssociation server) = BuildPair(serverOutbox);

			// The receive-only server's in-order SACKs only leave via the 200ms fallback now (they
			// piggyback on outbound data otherwise, and this server has none), so its clock is faked
			// and advanced per pump round to fire that path deterministically.
			long fakeNow = 1_000_000;
			server.ClockNowMillis = () => fakeNow;

			Assert.IsTrue(client.HasSendRoom, "an idle established association has room");

			// Larger than the initial window (cwnd starts at 4 MTU = 4800 bytes), so with every SACK
			// withheld the resident bytes exceed the window and the room must report closed.
			byte[] message = new byte[10 * 1024];
			new Random(11).NextBytes(message);
			Assert.IsTrue(client.Send(streamId: 1, ppid: 53, message, unordered: false, maxRetransmits: -1), "an established association never refuses a message");

			Assert.IsFalse(client.HasSendRoom, "resident bytes past the window mean no room until acked");

			Task roomSignal = client.WhenSendRoom();
			Assert.IsFalse(roomSignal.IsCompleted, "no SACK has arrived yet");

			// Each round: advance the server past the SACK delay so it acks what it holds, then pump
			// its replies to the client, whose window walks the rest of the message out.
			for (int i = 0; i < 40 && !client.HasSendRoom; i++)
			{
				fakeNow += 250;
				server.OnTick();
				PumpAll(serverOutbox, client);
			}

			Assert.IsTrue(client.HasSendRoom, "acked bytes reopen the window");
			Assert.IsTrue(roomSignal.IsCompleted, "the wake fired with the SACK that freed room");
		}

		[TestMethod]
		public void Teardown_SignalsParkedWaiter_AndReportsRoom()
		{
			var serverOutbox = new List<byte[]>();
			(SctpAssociation client, SctpAssociation server) = BuildPair(serverOutbox);

			byte[] message = new byte[10 * 1024];
			Assert.IsTrue(client.Send(streamId: 1, ppid: 53, message, unordered: false, maxRetransmits: -1));
			Assert.IsFalse(client.HasSendRoom);

			Task roomSignal = client.WhenSendRoom();
			client.Abort("test teardown");

			Assert.IsTrue(roomSignal.IsCompleted, "teardown wakes a parked waiter instead of leaving it forever");
			Assert.IsTrue(client.HasSendRoom, "off-Established reports room so a waiter fails its next send fast instead of parking");
			Assert.IsFalse(client.Send(streamId: 1, ppid: 53, new byte[8], unordered: false, maxRetransmits: -1), "the failed-fast send");
		}

		[TestMethod]
		public void NoBudget_HugeBacklogIsAcceptedWhole_AndFullyDelivered()
		{
			var serverOutbox = new List<byte[]>();
			(SctpAssociation client, SctpAssociation server) = BuildPair(serverOutbox);

			long deliveredBytes = 0;
			int deliveredCount = 0;
			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> msg) =>
			{
				deliveredBytes += msg.Length;
				deliveredCount++;
			};

			// The receive-only server acks via the 200ms fallback only (see WindowFull's remarks);
			// fake both clocks and advance per round so acks and the client's tick both run.
			long fakeNow = 1_000_000;
			client.ClockNowMillis = () => fakeNow;
			server.ClockNowMillis = () => fakeNow;

			// Far past the old 4MB budget's window-of-one-message equivalent and past the whole
			// window many times over: every message must be accepted (no refusal surface exists)
			// and every byte must arrive once the SACKs flow.
			const int messageCount = 64;
			byte[] message = new byte[128 * 1024];
			new Random(23).NextBytes(message);

			for (int i = 0; i < messageCount; i++)
			{
				Assert.IsTrue(client.Send(streamId: 1, ppid: 53, message, unordered: false, maxRetransmits: -1), $"message {i} refused: a budget is back");
			}

			for (int i = 0; i < 100_000 && deliveredCount < messageCount; i++)
			{
				fakeNow += 250;
				server.OnTick();
				PumpAll(serverOutbox, client);
				if (serverOutbox.Count == 0 && deliveredCount < messageCount) client.OnTick();
			}

			Assert.AreEqual(messageCount, deliveredCount, "every queued message arrived, none dropped");
			Assert.AreEqual((long) messageCount * message.Length, deliveredBytes);
		}
	}
}
