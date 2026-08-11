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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     DCEP (RFC 8832) negotiation and message dispatch, exercised the same way
	///     <see cref="SctpSendTests" /> and <see cref="SctpAssociationHandshakeTests" /> exercise the
	///     association underneath: two <see cref="SctpAssociation" />s wired back-to-back via
	///     <see cref="SctpAssociation.PacketSender" />, one <see cref="RtcChannelManager" /> per side.
	/// </summary>
	[TestClass]
	public class RtcDataChannelTests
	{
		/// <summary>DATA_CHANNEL_OPEN, RFC 8832 5.1's fixed layout, built field by field independently of <see cref="RtcChannelManager" />'s own encoder - this is the ground-truth vector the codec is checked against in both directions.</summary>
		private static byte[] BuildRfc8832OpenVector(byte channelType, ushort priority, uint reliabilityParameter, string label)
		{
			byte[] labelBytes = Encoding.UTF8.GetBytes(label);
			byte[] vector = new byte[12 + labelBytes.Length];
			vector[0] = 0x03; // DATA_CHANNEL_OPEN
			vector[1] = channelType;
			BinaryPrimitives.WriteUInt16BigEndian(vector.AsSpan(2, 2), priority);
			BinaryPrimitives.WriteUInt32BigEndian(vector.AsSpan(4, 4), reliabilityParameter);
			BinaryPrimitives.WriteUInt16BigEndian(vector.AsSpan(8, 2), (ushort) labelBytes.Length);
			BinaryPrimitives.WriteUInt16BigEndian(vector.AsSpan(10, 2), 0); // protocol length: NetherNet never sets one
			labelBytes.CopyTo(vector, 12);
			return vector;
		}

		/// <summary>Scans a captured outbound packet for its first DATA chunk (skipping a bundled SACK, if any - see <see cref="SctpAssociation.Flush" />'s own remarks on bundling), and returns its stream id, PPID, payload bytes, and the wire-level Unordered (U) flag.</summary>
		private static (ushort StreamId, uint Ppid, byte[] Payload, bool Unordered) ExtractFirstDataChunk(byte[] packet)
		{
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(packet);
			while (enumerator.MoveNext())
			{
				(byte type, byte flags, ReadOnlySpan<byte> value) = enumerator.Current;
				if (type != SctpChunkType.Data) continue;

				Assert.IsTrue(DataChunkHeader.TryParse(flags, value, out DataChunkHeader header, out ReadOnlySpan<byte> payload));
				return (header.StreamId, header.Ppid, payload.ToArray(), header.Unordered);
			}

			Assert.Fail("No DATA chunk found in the captured packet.");
			return default;
		}

		private static (SctpAssociation Client, SctpAssociation Server, List<byte[]> ClientSent, List<byte[]> ServerSent) CreateEstablishedPair()
		{
			var clientSent = new List<byte[]>();
			var serverSent = new List<byte[]>();

			SctpAssociation server = null;
			SctpAssociation client = null;
			client = new SctpAssociation(true, 5000, 262144, p =>
			{
				clientSent.Add(p.ToArray());
				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 262144, p =>
			{
				serverSent.Add(p.ToArray());
				client.OnPacketReceived(p.ToArray());
			});

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			clientSent.Clear();
			serverSent.Clear();
			return (client, server, clientSent, serverSent);
		}

		[TestMethod]
		public void CreateChannel_Reliable_SendsOpenByteExactToHandBuiltRfc8832Vector()
		{
			(SctpAssociation client, SctpAssociation _, List<byte[]> clientSent, List<byte[]> _) = CreateEstablishedPair();
			var clientManager = new RtcChannelManager(client, isClient: true);

			clientManager.CreateChannel("ReliableDataChannel", ordered: true, maxRetransmits: -1);

			Assert.AreEqual(1, clientSent.Count);
			(ushort streamId, uint ppid, byte[] payload, bool _) = ExtractFirstDataChunk(clientSent[0]);
			Assert.AreEqual((ushort) 0, streamId); // DTLS client (isClient: true): even stream ids from 0
			Assert.AreEqual(50u, ppid); // DCEP PPID

			byte[] expected = BuildRfc8832OpenVector(channelType: 0x00, priority: 0, reliabilityParameter: 0, "ReliableDataChannel");
			CollectionAssert.AreEqual(expected, payload);
		}

		[TestMethod]
		public void CreateChannel_UnreliableUnordered_SendsOpenByteExactToHandBuiltRfc8832Vector()
		{
			(SctpAssociation client, SctpAssociation _, List<byte[]> clientSent, List<byte[]> _) = CreateEstablishedPair();
			var clientManager = new RtcChannelManager(client, isClient: true);

			clientManager.CreateChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);

			Assert.AreEqual(1, clientSent.Count);
			(ushort streamId, uint ppid, byte[] payload, bool _) = ExtractFirstDataChunk(clientSent[0]);
			Assert.AreEqual((ushort) 0, streamId);
			Assert.AreEqual(50u, ppid);

			byte[] expected = BuildRfc8832OpenVector(channelType: 0x81, priority: 0, reliabilityParameter: 0, "UnreliableDataChannel");
			CollectionAssert.AreEqual(expected, payload);
		}

		[TestMethod]
		public void HandBuiltOpenVector_DeliveredToServer_CreatesChannelOpenImmediately_AndAcksOnSameStream()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> serverSent) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			// Hand-built per RFC 8832's layout, independent of RtcChannelManager's own WriteOpen - the
			// decode-side half of the round trip. Carried over the client's own (already independently
			// tested) SCTP send path so the DATA chunk framing around it is real, not hand-forged.
			byte[] vector = BuildRfc8832OpenVector(channelType: 0x81, priority: 0, reliabilityParameter: 0, "UnreliableDataChannel");
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, vector, unordered: false, maxRetransmits: -1));

			Assert.IsNotNull(createdChannel);
			Assert.AreEqual("UnreliableDataChannel", createdChannel.Label);
			Assert.AreEqual((ushort) 0, createdChannel.StreamId);
			Assert.IsFalse(createdChannel.Ordered);
			Assert.AreEqual(0, createdChannel.MaxRetransmits);
			Assert.IsTrue(createdChannel.IsOpen); // RFC 8832: usable immediately, does not wait on its own ACK being acked

			Assert.AreEqual(1, serverSent.Count);
			(ushort ackStreamId, uint ackPpid, byte[] ackPayload, bool _) = ExtractFirstDataChunk(serverSent[0]);
			Assert.AreEqual((ushort) 0, ackStreamId); // same stream as the OPEN
			Assert.AreEqual(50u, ackPpid);
			CollectionAssert.AreEqual(new byte[] {0x02}, ackPayload); // DATA_CHANNEL_ACK, RFC 8832 5.2
		}

		[TestMethod]
		public void ChannelNegotiation_TwoChannelsFromClient_ServerFiresOnDataChannelTwice_BothOpen_MessagesEchoBothWays_EmptyMessagesRoundTrip()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var clientManager = new RtcChannelManager(client, isClient: true);
			var serverManager = new RtcChannelManager(server, isClient: false);

			var serverChannels = new List<RtcDataChannel>();
			serverManager.OnDataChannel += ch => serverChannels.Add(ch);

			RtcDataChannel clientReliable = clientManager.CreateChannel("ReliableDataChannel", ordered: true, maxRetransmits: -1);
			RtcDataChannel clientUnreliable = clientManager.CreateChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);

			Assert.AreEqual(2, serverChannels.Count);
			Assert.IsTrue(clientReliable.IsOpen);
			Assert.IsTrue(clientUnreliable.IsOpen);

			RtcDataChannel serverReliable = serverChannels.Single(c => c.Label == "ReliableDataChannel");
			RtcDataChannel serverUnreliable = serverChannels.Single(c => c.Label == "UnreliableDataChannel");
			Assert.IsTrue(serverReliable.IsOpen);
			Assert.IsTrue(serverUnreliable.IsOpen);

			// Both channels were opened by the client (DTLS role client: even stream ids from 0), so both
			// live on even ids, and the server-side channel objects share the exact same stream id.
			Assert.AreEqual((ushort) 0, clientReliable.StreamId);
			Assert.AreEqual((ushort) 2, clientUnreliable.StreamId);
			Assert.AreEqual(clientReliable.StreamId, serverReliable.StreamId);
			Assert.AreEqual(clientUnreliable.StreamId, serverUnreliable.StreamId);

			// string client -> server
			string receivedString = null;
			serverReliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsTrue(isString);
				receivedString = Encoding.UTF8.GetString(data.ToArray());
			};
			clientReliable.Send(Encoding.UTF8.GetBytes("hello"), asString: true);
			Assert.AreEqual("hello", receivedString);

			// binary server -> client
			byte[] receivedBinary = null;
			clientReliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				receivedBinary = data.ToArray();
			};
			byte[] binary = {1, 2, 3, 4};
			serverReliable.Send(binary, asString: false);
			CollectionAssert.AreEqual(binary, receivedBinary);

			// empty string client -> server (PPID 56, one wire padding byte reconstructed as empty)
			bool emptyStringSeen = false;
			serverUnreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				emptyStringSeen = true;
				Assert.IsTrue(isString);
				Assert.AreEqual(0, data.Length);
			};
			clientUnreliable.Send(ReadOnlySpan<byte>.Empty, asString: true);
			Assert.IsTrue(emptyStringSeen);

			// empty binary server -> client (PPID 57)
			bool emptyBinarySeen = false;
			clientUnreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				emptyBinarySeen = true;
				Assert.IsFalse(isString);
				Assert.AreEqual(0, data.Length);
			};
			serverUnreliable.Send(ReadOnlySpan<byte>.Empty, asString: false);
			Assert.IsTrue(emptyBinarySeen);
		}

		[TestMethod]
		public void TruncatedOpen_IsDroppedAndCounted_AssociationKeepsWorkingForALaterWellFormedOpen()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var clientManager = new RtcChannelManager(client, isClient: true);
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			byte[] truncated = {0x03, 0x00, 0x00}; // far short of the 12-byte OPEN header
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, truncated, unordered: false, maxRetransmits: -1));

			Assert.IsNull(createdChannel);
			Assert.AreEqual(1L, serverManager.IgnoredMessageCount);

			// The hostile input did not wedge anything: a fresh, well-formed OPEN still negotiates.
			clientManager.CreateChannel("ReliableDataChannel", ordered: true, maxRetransmits: -1);
			Assert.IsNotNull(createdChannel);
			Assert.AreEqual("ReliableDataChannel", createdChannel.Label);
		}

		[TestMethod]
		public void OpenWithUnsupportedChannelType_IsDroppedAndCounted_NoChannelCreated()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			// 0xFF names no DCEP channel type this stack (or RFC 8832) recognises.
			byte[] vector = BuildRfc8832OpenVector(channelType: 0xFF, priority: 0, reliabilityParameter: 0, "Whatever");
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, vector, unordered: false, maxRetransmits: -1));

			Assert.IsNull(createdChannel);
			Assert.AreEqual(1L, serverManager.IgnoredMessageCount);
		}

		/// <summary>Fix-round Critical 1: a retransmitted/duplicate OPEN for a stream already negotiated (the peer likely lost our first ACK) must re-ACK - RFC-friendly, idempotent - without rebuilding the channel object or re-firing <see cref="RtcChannelManager.OnDataChannel" />, which would silently orphan whatever the application already subscribed to the original channel.</summary>
		[TestMethod]
		public void DuplicateOpenOnSameStream_DoesNotHijackTheOriginalChannel_ButDoesReAck()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> serverSent) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			var firedChannels = new List<RtcDataChannel>();
			serverManager.OnDataChannel += ch => firedChannels.Add(ch);

			byte[] vector = BuildRfc8832OpenVector(channelType: 0x00, priority: 0, reliabilityParameter: 0, "ReliableDataChannel");

			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, vector, unordered: false, maxRetransmits: -1));
			Assert.AreEqual(1, firedChannels.Count);
			RtcDataChannel original = firedChannels[0];

			string originalReceivedMessage = null;
			original.OnMessage += (in ReadOnlySequence<byte> data, bool isString) => originalReceivedMessage = Encoding.UTF8.GetString(data.ToArray());

			serverSent.Clear();

			// The peer retransmits the identical OPEN on the same stream - a lost first ACK is the
			// ordinary reason this happens on a real network.
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, vector, unordered: false, maxRetransmits: -1));

			Assert.AreEqual(1, firedChannels.Count); // OnDataChannel fired exactly once, total
			Assert.AreSame(original, firedChannels[0]);

			Assert.AreEqual(1, serverSent.Count); // still answers with an ACK
			(ushort ackStreamId, uint ackPpid, byte[] ackPayload, bool _) = ExtractFirstDataChunk(serverSent[0]);
			Assert.AreEqual((ushort) 0, ackStreamId);
			Assert.AreEqual(50u, ackPpid);
			CollectionAssert.AreEqual(new byte[] {0x02}, ackPayload);

			// The ORIGINAL channel object - with the application's own subscription - is still the one
			// dispatching on this stream, not a stray twin the duplicate OPEN silently swapped in.
			Assert.IsTrue(client.Send(streamId: 0, ppid: 53, Encoding.UTF8.GetBytes("still me"), unordered: false, maxRetransmits: -1));
			Assert.AreEqual("still me", originalReceivedMessage);
		}

		/// <summary>Fix-round Critical 1: RFC 8832 6 fixes each side's stream id parity by DTLS role. An inbound OPEN naming a stream id of OUR OWN parity (the server here is odd, isClient: false) can never be a peer-initiated channel and must be ignored and counted outright, not answered.</summary>
		[TestMethod]
		public void OpenWithOurOwnParityStreamId_IsIgnoredAndCounted_NoChannelCreated()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			// The server's own parity is odd (isClient: false starts at stream id 1); stream id 1 is a
			// stream id the SERVER itself would allocate, never a valid id for a peer-initiated channel.
			byte[] vector = BuildRfc8832OpenVector(channelType: 0x00, priority: 0, reliabilityParameter: 0, "ReliableDataChannel");
			Assert.IsTrue(client.Send(streamId: 1, ppid: 50, vector, unordered: false, maxRetransmits: -1));

			Assert.IsNull(createdChannel);
			Assert.AreEqual(1L, serverManager.IgnoredMessageCount);
		}

		/// <summary>Fix-round Important 2: an OPEN whose label is long enough to fragment at the SCTP layer (above <c>FragmentThreshold</c>, 1024 bytes) must still negotiate: the multi-segment fallback in <see cref="RtcChannelManager" />'s DCEP parsing has to actually be reachable by a message this stack's own fragmentation threshold can produce, not just in principle.</summary>
		[TestMethod]
		public void OpenWithLabelLargeEnoughToFragmentAtSctpLayer_StillNegotiatesSuccessfully()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			// 1100-byte label -> 1112-byte OPEN, above the association's own 1024-byte FragmentThreshold,
			// so this genuinely arrives as a multi-segment ReadOnlySequence<byte>, not a single-segment one.
			string label = new string('L', 1100);
			byte[] vector = BuildRfc8832OpenVector(channelType: 0x00, priority: 0, reliabilityParameter: 0, label);
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, vector, unordered: false, maxRetransmits: -1));

			Assert.IsNotNull(createdChannel);
			Assert.AreEqual(label, createdChannel.Label);
			Assert.IsTrue(createdChannel.IsOpen);
		}

		/// <summary>Fix-round Important 2: an OPEN too large to fit even the fragmented-message scratch buffer is dropped and counted by design (documented at <c>DcepScratchSize</c>), and the association keeps working afterward for a normal-sized OPEN.</summary>
		[TestMethod]
		public void OpenLargerThanScratchBuffer_IsDroppedAndCounted_AssociationStillWorks()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			string oversizedLabel = new string('L', 2100); // 2112-byte OPEN, past the 2048-byte scratch cap
			byte[] oversizedVector = BuildRfc8832OpenVector(channelType: 0x00, priority: 0, reliabilityParameter: 0, oversizedLabel);
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, oversizedVector, unordered: false, maxRetransmits: -1));

			Assert.IsNull(createdChannel);
			Assert.AreEqual(1L, serverManager.IgnoredMessageCount);

			byte[] normalVector = BuildRfc8832OpenVector(channelType: 0x00, priority: 0, reliabilityParameter: 0, "ReliableDataChannel");
			Assert.IsTrue(client.Send(streamId: 2, ppid: 50, normalVector, unordered: false, maxRetransmits: -1));
			Assert.IsNotNull(createdChannel);
			Assert.AreEqual("ReliableDataChannel", createdChannel.Label);
		}

		/// <summary>Fix-round Important 3 (RFC 8832 6 MUST): a channel's own unordered flag must not apply to a message sent before this side has processed the channel's own ACK - riding ordered on the same stream as the OPEN is what guarantees the message cannot be delivered (or rejected as an unknown stream) ahead of it. The real reordering this guards against cannot be produced by the synchronous loopback wiring these tests use, so this observes the proxy the fix-round asked for instead: the wire-level U flag on the DATA chunk, captured directly off the client's send delegate, before vs. after the ACK is actually processed.</summary>
		[TestMethod]
		public void UnorderedChannel_SendBeforeAckProcessed_RidesOrdered_ThenUnorderedOnceAckProcessed()
		{
			var heldServerToClient = new List<byte[]>();
			bool holdServerToClient = false;

			SctpAssociation server = null;
			SctpAssociation client = null;
			var clientSent = new List<byte[]>();

			client = new SctpAssociation(true, 5000, 262144, p =>
			{
				clientSent.Add(p.ToArray());
				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 262144, p =>
			{
				if (holdServerToClient)
				{
					heldServerToClient.Add(p.ToArray());
					return;
				}

				client.OnPacketReceived(p.ToArray());
			});

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			var clientManager = new RtcChannelManager(client, isClient: true);
			_ = new RtcChannelManager(server, isClient: false);

			holdServerToClient = true; // withhold the server's ACK reply so the channel stays un-opened
			RtcDataChannel channel = clientManager.CreateChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);
			Assert.IsFalse(channel.IsOpen);

			clientSent.Clear();
			channel.Send(new byte[] {1, 2, 3}, asString: false);
			Assert.AreEqual(1, clientSent.Count);
			(ushort _, uint _, byte[] _, bool unorderedPreAck) = ExtractFirstDataChunk(clientSent[0]);
			Assert.IsFalse(unorderedPreAck); // upgraded to ordered while this channel's own ACK is still outstanding

			holdServerToClient = false;
			foreach (byte[] held in heldServerToClient) client.OnPacketReceived(held);
			Assert.IsTrue(channel.IsOpen);

			clientSent.Clear();
			channel.Send(new byte[] {4, 5, 6}, asString: false);
			Assert.AreEqual(1, clientSent.Count);
			(ushort _, uint _, byte[] _, bool unorderedPostAck) = ExtractFirstDataChunk(clientSent[0]);
			Assert.IsTrue(unorderedPostAck); // reverted to this channel's true (unordered) negotiated semantics
		}

		/// <summary>Fix-round Important 3: this stack's own conservative superset beyond the RFC 8832 6 MUST (which mandates ordered, not reliable, before ACK) - a message sent before this channel's own ACK has been processed is also held reliable regardless of the channel's real <see cref="RtcDataChannel.MaxRetransmits" />, so it cannot be abandoned (RFC 3758 FORWARD-TSN) before the peer has even confirmed the channel exists. Not wire-observable (MaxRetransmits never appears on the wire, only in local send-queue bookkeeping), so this exercises the actual observable effect instead: simulated loss plus a T3-rtx timeout must retransmit, never abandon, a pre-ACK send on a maxRetransmits: 0 channel; the identical scenario after the ACK has been processed abandons normally, proving the override does not outlive the pre-ACK window.</summary>
		[TestMethod]
		public void UnorderedChannel_SendBeforeAckProcessed_SurvivesLossAndTimeout_ThenAbandonsNormallyOnceAckProcessed()
		{
			var heldServerToClient = new List<byte[]>();
			bool holdServerToClient = false;
			bool dropNextClientToServer = false;

			SctpAssociation server = null;
			SctpAssociation client = null;

			client = new SctpAssociation(true, 5000, 262144, p =>
			{
				if (dropNextClientToServer)
				{
					dropNextClientToServer = false;
					return;
				}

				server.OnPacketReceived(p.ToArray());
			});
			server = new SctpAssociation(false, 5000, 262144, p =>
			{
				if (holdServerToClient)
				{
					heldServerToClient.Add(p.ToArray());
					return;
				}

				client.OnPacketReceived(p.ToArray());
			});

			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);

			long fakeNow = 10_000_000;
			client.ClockNowMillis = () => fakeNow;

			var clientManager = new RtcChannelManager(client, isClient: true);
			_ = new RtcChannelManager(server, isClient: false);

			holdServerToClient = true;
			RtcDataChannel channel = clientManager.CreateChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);
			Assert.IsFalse(channel.IsOpen);

			// Pre-ACK send, lost, then a T3-rtx timeout: a real maxRetransmits: 0 send would be abandoned
			// the instant this timer fires once. It must not be here - the pre-ACK reliable override.
			dropNextClientToServer = true;
			channel.Send(new byte[] {1, 2, 3}, asString: false);

			fakeNow += 1500; // past the initial 1000ms RTO
			client.OnTick();

			Assert.AreEqual(0L, client.SendAbandonedCount);
			Assert.IsTrue(client.SendRetransmitCount >= 1);

			holdServerToClient = false;
			foreach (byte[] held in heldServerToClient) client.OnPacketReceived(held);
			Assert.IsTrue(channel.IsOpen);

			// Post-ACK send, lost, then a T3-rtx timeout: now rides the channel's TRUE maxRetransmits: 0,
			// so this one abandons normally the moment the timer fires - the override did not stick. The
			// first round above already backed the association's RTO off past its 1000ms initial value, so
			// this wait is sized off the association's own current RTO rather than a fixed guess.
			dropNextClientToServer = true;
			channel.Send(new byte[] {4, 5, 6}, asString: false);

			fakeNow += client.SendRtoMillis + 500;
			client.OnTick();

			Assert.AreEqual(1L, client.SendAbandonedCount);
		}

		/// <summary>Fix-round Important 3: a data message (non-DCEP PPID) addressed to a stream id with no registered channel - stale, or a peer bug - is dropped-and-counted rather than silently discarded; the silent-by-design version of this drop is exactly what let the pre-ACK race in the two tests above go unnoticed.</summary>
		[TestMethod]
		public void DataForUnregisteredStream_IsCountedAndDropped()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			Assert.IsTrue(client.Send(streamId: 4, ppid: 53, new byte[] {9, 9}, unordered: true, maxRetransmits: -1));

			Assert.AreEqual(1L, serverManager.IgnoredMessageCount);
		}

		/// <summary>Fix-round drive-by: priority and reliability parameter sit in adjacent fields of the OPEN header (offsets 2-4 and 4-8); a vector with both nonzero at once catches a transposed-field decode bug that an all-zero vector (every other test in this file) could never expose.</summary>
		[TestMethod]
		public void HandBuiltOpenVector_NonzeroPriorityAndReliabilityParameter_DecodesCorrectly()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			RtcDataChannel createdChannel = null;
			serverManager.OnDataChannel += ch => createdChannel = ch;

			// Channel type 0x01 (ordered, partial-reliable-rexmit): priority 7 (nonzero, unused by this
			// stack but must not corrupt the adjacent field), reliability parameter 5 (-> MaxRetransmits).
			byte[] vector = BuildRfc8832OpenVector(channelType: 0x01, priority: 7, reliabilityParameter: 5, "PriorityCheck");
			Assert.IsTrue(client.Send(streamId: 0, ppid: 50, vector, unordered: false, maxRetransmits: -1));

			Assert.IsNotNull(createdChannel);
			Assert.AreEqual("PriorityCheck", createdChannel.Label);
			Assert.IsTrue(createdChannel.Ordered);
			Assert.AreEqual(5, createdChannel.MaxRetransmits);
		}

		/// <summary>Fix-round drive-by: pins the existing (unchanged by this fix round) behaviour of a bare DATA_CHANNEL_ACK for a stream this side never opened - not stale-but-known, never known at all - ignored and counted, same as every other unresolvable DCEP message.</summary>
		[TestMethod]
		public void BareAckForNeverOpenedStream_IsIgnoredAndCounted()
		{
			(SctpAssociation client, SctpAssociation server, List<byte[]> _, List<byte[]> _) = CreateEstablishedPair();
			var serverManager = new RtcChannelManager(server, isClient: false);

			byte[] ackVector = {0x02};
			Assert.IsTrue(client.Send(streamId: 4, ppid: 50, ackVector, unordered: false, maxRetransmits: -1));

			Assert.AreEqual(1L, serverManager.IgnoredMessageCount);
		}
	}
}