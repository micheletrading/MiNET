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
	[TestClass]
	public class SctpAssociationHandshakeTests
	{
		/// <summary>
		///     Two associations wired back-to-back through their <see cref="SctpAssociation.PacketSender" />
		///     delegates: the client's outbound packets feed the server's <see cref="SctpAssociation.OnPacketReceived" />
		///     and vice versa. Since nothing here is asynchronous, the whole INIT -> INIT-ACK ->
		///     COOKIE-ECHO -> COOKIE-ACK exchange runs to completion synchronously inside
		///     <see cref="SctpAssociation.Start" />.
		/// </summary>
		[TestMethod]
		public void TwoAssociations_CompleteHandshake_AndAgreeOnVerificationTags()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			client = new SctpAssociation(isClient: true, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => client.OnPacketReceived(p.ToArray()));

			int clientEstablished = 0;
			int serverEstablished = 0;
			client.OnEstablished += () => clientEstablished++;
			server.OnEstablished += () => serverEstablished++;

			client.Start();

			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(SctpState.Established, server.State);
			Assert.AreEqual(1, clientEstablished);
			Assert.AreEqual(1, serverEstablished);

			// RFC 4960 5.1: each side's outgoing tag is the OTHER side's chosen tag, so the two
			// associations must agree on both tags once established.
			Assert.AreEqual(client.LocalVerificationTag, server.PeerVerificationTag);
			Assert.AreEqual(server.LocalVerificationTag, client.PeerVerificationTag);
			Assert.AreNotEqual(0u, client.LocalVerificationTag);
			Assert.AreNotEqual(0u, server.LocalVerificationTag);
		}

		/// <summary>
		///     The testable seam for cookie age: <see cref="SctpAssociation.CreateCookie" /> is the same
		///     internal factory the server uses when answering a real INIT, exposed so a test can fabricate
		///     an otherwise-valid, correctly HMAC-signed cookie whose embedded timestamp is already 61
		///     seconds old. Validation runs against the real clock, so this proves rejection is driven by
		///     the cookie's age, not by a mismatched signature or tag.
		/// </summary>
		[TestMethod]
		public void StaleCookie_IsRejected_HandshakeDoesNotComplete()
		{
			var server = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: _ => Assert.Fail("server must not answer a stale COOKIE-ECHO"));

			long staleTimestamp = Environment.TickCount64 - 61_000;
			byte[] cookie = SctpAssociation.CreateCookie(
				peerInitiateTag: 111,
				ourTag: 222,
				peerArwnd: 131072,
				peerOutboundStreams: 1024,
				peerInboundStreams: 1024,
				peerInitialTsn: 5000,
				ourInitialTsn: 9000,
				timestampMillis: staleTimestamp);

			byte[] packetArray = new byte[256];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 222); // header tag = ourTag as embedded in the cookie
			n += new CookieEchoChunk(cookie).WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			int established = 0;
			server.OnEstablished += () => established++;

			server.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.AreEqual(SctpState.Closed, server.State);
			Assert.AreEqual(0, established);
			Assert.AreEqual(1L, server.IgnoredPacketCount);
		}

		/// <summary>
		///     Four combinations of hostile input (checksum-garbage / well-formed-but-wrong-tag, each
		///     before and after the handshake completes), none of which may throw, change state, or (for
		///     the pre-establishment INIT case) provoke a reply.
		/// </summary>
		[TestMethod]
		public void GarbageAndBadTagPackets_AreIgnored_BeforeAndAfterEstablishment()
		{
			byte[] garbage = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16};

			// Before establishment: checksum-garbage on a fresh server.
			var freshServer = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: _ => Assert.Fail("must not reply to garbage"));
			freshServer.OnPacketReceived(garbage);
			Assert.AreEqual(SctpState.Closed, freshServer.State);
			Assert.AreEqual(1L, freshServer.IgnoredPacketCount);

			// Before establishment: a well-formed INIT chunk, but the packet's verification tag is
			// not zero, which RFC 4960 5.1 requires for the packet carrying an INIT.
			var freshServer2 = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: _ => Assert.Fail("must not reply to a bad-tag INIT"));
			var init = new InitChunk(initiateTag: 555, arwnd: 131072, outboundStreams: 1024, inboundStreams: 1024, initialTsn: 42, forwardTsnSupported: true);
			byte[] badTagInitArray = new byte[128];
			Span<byte> badTagInit = badTagInitArray;
			int n1 = SctpPacket.WriteHeader(badTagInit, 5000, 5000, 999);
			n1 += init.WriteTo(badTagInit.Slice(n1));
			SctpPacket.FinishChecksum(badTagInit.Slice(0, n1));
			freshServer2.OnPacketReceived(badTagInitArray.AsMemory(0, n1));
			Assert.AreEqual(SctpState.Closed, freshServer2.State);
			Assert.AreEqual(1L, freshServer2.IgnoredPacketCount);

			// Complete a real handshake so the "after establishment" half exercises live associations.
			SctpAssociation server = null;
			SctpAssociation client = null;
			client = new SctpAssociation(true, 5000, 131072, p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(false, 5000, 131072, p => client.OnPacketReceived(p.ToArray()));
			client.Start();
			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(SctpState.Established, server.State);

			long serverIgnoredBefore = server.IgnoredPacketCount;
			long clientIgnoredBefore = client.IgnoredPacketCount;

			// After establishment: checksum-garbage on both sides.
			server.OnPacketReceived(garbage);
			client.OnPacketReceived(garbage);
			Assert.AreEqual(SctpState.Established, server.State);
			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(serverIgnoredBefore + 1, server.IgnoredPacketCount);
			Assert.AreEqual(clientIgnoredBefore + 1, client.IgnoredPacketCount);

			// After establishment: a well-formed COOKIE-ACK chunk (type 11, RFC 4960 3.2) carrying a
			// tag that matches neither side's tag.
			byte[] badTagCookieAckArray = new byte[32];
			Span<byte> badTagCookieAck = badTagCookieAckArray;
			int n2 = SctpPacket.WriteHeader(badTagCookieAck, 5000, 5000, 0xDEADBEEF);
			n2 += SctpChunkCodec.FinishChunk(badTagCookieAck.Slice(n2), 11, 0, 0);
			SctpPacket.FinishChecksum(badTagCookieAck.Slice(0, n2));
			client.OnPacketReceived(badTagCookieAckArray.AsMemory(0, n2));
			Assert.AreEqual(SctpState.Established, client.State);
			Assert.AreEqual(clientIgnoredBefore + 2, client.IgnoredPacketCount);
		}

		/// <summary>
		///     RFC 4960 8.5: unlike the COOKIE-ACK case above (which was already rejected on its own,
		///     chunk-type-specific tag check), this proves the rejection is general - a well-formed DATA
		///     chunk, a chunk type with no tag check of its own, is still dropped whole when the packet's
		///     verification tag does not match the established association's own tag: no delivery, and no
		///     SACK reaction (nothing sent back at all), just the ignored counter advancing.
		/// </summary>
		[TestMethod]
		public void WrongTagDataPacket_OnEstablishedAssociation_IsDroppedWhole_NoDeliveryNoSackReaction()
		{
			SctpAssociation server = null;
			SctpAssociation client = null;
			var serverReceived = new List<byte[]>();
			var serverSent = new List<byte[]>();

			client = new SctpAssociation(true, 5000, 131072, p => server.OnPacketReceived(p.ToArray()));
			server = new SctpAssociation(false, 5000, 131072, p =>
			{
				serverSent.Add(p.ToArray());
				client.OnPacketReceived(p.ToArray());
			});
			server.OnMessage += (ushort streamId, uint ppid, in ReadOnlySequence<byte> message) => serverReceived.Add(message.ToArray());

			client.Start();
			Assert.AreEqual(SctpState.Established, server.State);
			serverSent.Clear(); // drop the handshake replies (INIT-ACK, COOKIE-ACK) already captured above

			uint wrongTag = unchecked(server.LocalVerificationTag + 1);
			var header = new DataChunkHeader(unchecked(server.CumulativeTsnAck + 1), streamId: 1, streamSeq: 0, ppid: 7, unordered: false, begin: true, end: true, immediateSack: false);
			byte[] payload = {1, 2, 3, 4};

			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, wrongTag);
			n += header.WriteTo(packet.Slice(n), payload);
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			long ignoredBefore = server.IgnoredPacketCount;
			uint cumulativeBefore = server.CumulativeTsnAck;

			server.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.AreEqual(ignoredBefore + 1, server.IgnoredPacketCount);
			Assert.AreEqual(0, serverReceived.Count);
			Assert.AreEqual(cumulativeBefore, server.CumulativeTsnAck);
			Assert.AreEqual(0, serverSent.Count);
			Assert.AreEqual(SctpState.Established, server.State);
		}

		/// <summary>
		///     A State Cookie our own outbound COOKIE-ECHO could never carry is rejected at HandleInitAck
		///     before it is retained, so it can never resurface as a throw building that packet - not on
		///     receipt, and not on any later handshake retransmit tick either.
		/// </summary>
		[TestMethod]
		public void InitAck_OversizedStateCookie_IsRejectedAndCounted_NoThrowOnReceiveOrLaterTick()
		{
			var clientSent = new List<byte[]>();
			var client = new SctpAssociation(isClient: true, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => clientSent.Add(p.ToArray()));

			client.Start();
			Assert.AreEqual(SctpState.CookieWait, client.State);
			uint clientTag = client.LocalVerificationTag;

			// Far larger than any cookie an outbound COOKIE-ECHO could ever carry inside SctpPacket.MaxSize.
			byte[] hostileCookie = new byte[2000];
			var initAck = new InitChunk(4242, 131072, 1024, 1024, 500, forwardTsnSupported: true, hostileCookie);

			byte[] packetArray = new byte[4096];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, clientTag);
			n += initAck.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			long ignoredBefore = client.IgnoredPacketCount;

			client.OnPacketReceived(packetArray.AsMemory(0, n));

			Assert.AreEqual(SctpState.CookieWait, client.State, "an unusable INIT-ACK must not be adopted");
			Assert.AreEqual(ignoredBefore + 1, client.IgnoredPacketCount);

			// A later handshake retransmit tick must not throw either - nothing was ever retained to echo.
			client.OnTick();
		}
	}
}