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

		/// <summary>Scans a captured outbound packet for its first DATA chunk (skipping a bundled SACK, if any - see <see cref="SctpAssociation.Flush" />'s own remarks on bundling), and returns its stream id, PPID, and payload bytes.</summary>
		private static (ushort StreamId, uint Ppid, byte[] Payload) ExtractFirstDataChunk(byte[] packet)
		{
			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(packet);
			while (enumerator.MoveNext())
			{
				(byte type, byte flags, ReadOnlySpan<byte> value) = enumerator.Current;
				if (type != SctpChunkType.Data) continue;

				Assert.IsTrue(DataChunkHeader.TryParse(flags, value, out DataChunkHeader header, out ReadOnlySpan<byte> payload));
				return (header.StreamId, header.Ppid, payload.ToArray());
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
			(ushort streamId, uint ppid, byte[] payload) = ExtractFirstDataChunk(clientSent[0]);
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
			(ushort streamId, uint ppid, byte[] payload) = ExtractFirstDataChunk(clientSent[0]);
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
			(ushort ackStreamId, uint ackPpid, byte[] ackPayload) = ExtractFirstDataChunk(serverSent[0]);
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
			Assert.AreEqual(1L, serverManager.IgnoredDcepMessageCount);

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
			Assert.AreEqual(1L, serverManager.IgnoredDcepMessageCount);
		}
	}
}