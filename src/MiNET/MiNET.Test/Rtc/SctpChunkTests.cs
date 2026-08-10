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
using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class SctpChunkTests
	{
		[TestMethod]
		public void DataChunkHeader_RoundTrips_AllFlagCombosAcrossTwoCases()
		{
			// Case 1: every flag set.
			{
				var header = new DataChunkHeader(0x11223344, 7, 42, 0x51000000, unordered: true, begin: true, end: true, immediateSack: true);
				ReadOnlySpan<byte> userData = "hello"u8;

				Span<byte> packet = stackalloc byte[64];
				int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
				n += header.WriteTo(packet.Slice(n), userData);
				SctpPacket.FinishChecksum(packet.Slice(0, n));

				var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
				Assert.IsTrue(enumerator.MoveNext());
				var (type, flags, value) = enumerator.Current;
				Assert.AreEqual((byte) 0, type);

				Assert.IsTrue(DataChunkHeader.TryParse(flags, value, out DataChunkHeader parsed, out ReadOnlySpan<byte> payload));
				Assert.AreEqual(0x11223344u, parsed.Tsn);
				Assert.AreEqual((ushort) 7, parsed.StreamId);
				Assert.AreEqual((ushort) 42, parsed.StreamSeq);
				Assert.AreEqual(0x51000000u, parsed.Ppid);
				Assert.IsTrue(parsed.Unordered);
				Assert.IsTrue(parsed.Begin);
				Assert.IsTrue(parsed.End);
				Assert.IsTrue(parsed.ImmediateSack);
				CollectionAssert.AreEqual(userData.ToArray(), payload.ToArray());
			}

			// Case 2: every flag clear, different TSN/data (a middle DATA fragment).
			{
				var header = new DataChunkHeader(99, 3, 1, 0, unordered: false, begin: false, end: false, immediateSack: false);
				ReadOnlySpan<byte> userData = "middle-fragment-bytes"u8;

				Span<byte> packet = stackalloc byte[64];
				int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
				n += header.WriteTo(packet.Slice(n), userData);
				SctpPacket.FinishChecksum(packet.Slice(0, n));

				var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
				Assert.IsTrue(enumerator.MoveNext());
				var (_, flags, value) = enumerator.Current;

				Assert.IsTrue(DataChunkHeader.TryParse(flags, value, out DataChunkHeader parsed, out ReadOnlySpan<byte> payload));
				Assert.AreEqual(99u, parsed.Tsn);
				Assert.IsFalse(parsed.Unordered);
				Assert.IsFalse(parsed.Begin);
				Assert.IsFalse(parsed.End);
				Assert.IsFalse(parsed.ImmediateSack);
				CollectionAssert.AreEqual(userData.ToArray(), payload.ToArray());
			}
		}

		[TestMethod]
		public void SackChunk_RoundTrips_WithGapBlocksAndDuplicate()
		{
			var gapBlocks = new[] { new SackChunk.GapBlock(2, 5), new SackChunk.GapBlock(10, 10) };
			var duplicateTsns = new uint[] { 777 };
			var sack = new SackChunk(1000, 65536, gapBlocks, duplicateTsns);

			Span<byte> packet = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
			n += sack.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 3, type);

			Assert.IsTrue(SackChunk.TryParse(value, out SackChunk parsed));
			Assert.AreEqual(1000u, parsed.CumulativeTsnAck);
			Assert.AreEqual(65536u, parsed.Arwnd);
			Assert.AreEqual(2, parsed.GapBlocks.Length);
			Assert.AreEqual((ushort) 2, parsed.GapBlocks[0].Start);
			Assert.AreEqual((ushort) 5, parsed.GapBlocks[0].End);
			Assert.AreEqual((ushort) 10, parsed.GapBlocks[1].Start);
			Assert.AreEqual((ushort) 10, parsed.GapBlocks[1].End);
			Assert.AreEqual(1, parsed.DuplicateTsns.Length);
			Assert.AreEqual(777u, parsed.DuplicateTsns[0]);
		}

		[TestMethod]
		public void InitChunk_RoundTrips_AsInitWithForwardTsnSupported()
		{
			var init = new InitChunk(0xAABBCCDD, 131072, 65, 65, 12345, forwardTsnSupported: true);

			Span<byte> packet = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 0);
			n += init.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 1, type); // INIT, not INIT-ACK: no state cookie was set

			Assert.IsTrue(InitChunk.TryParse(value, out InitChunk parsed));
			Assert.AreEqual(0xAABBCCDDu, parsed.InitiateTag);
			Assert.AreEqual(131072u, parsed.Arwnd);
			Assert.AreEqual((ushort) 65, parsed.OutboundStreams);
			Assert.AreEqual((ushort) 65, parsed.InboundStreams);
			Assert.AreEqual(12345u, parsed.InitialTsn);
			Assert.IsTrue(parsed.ForwardTsnSupported);
			Assert.IsTrue(parsed.StateCookie.IsEmpty);
		}

		[TestMethod]
		public void InitChunk_RoundTrips_AsInitAckWithA40ByteStateCookie()
		{
			byte[] cookie = new byte[40];
			for (int i = 0; i < cookie.Length; i++) cookie[i] = (byte) (i + 1);

			var initAck = new InitChunk(0x1000, 65536, 10, 10, 500, forwardTsnSupported: false, stateCookie: cookie);

			Span<byte> packet = stackalloc byte[128];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 0x1000);
			n += initAck.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 2, type); // INIT-ACK: state cookie was set

			Assert.IsTrue(InitChunk.TryParse(value, out InitChunk parsed));
			Assert.AreEqual(0x1000u, parsed.InitiateTag);
			Assert.IsFalse(parsed.ForwardTsnSupported);
			CollectionAssert.AreEqual(cookie, parsed.StateCookie.ToArray());
		}

		[TestMethod]
		public void ForwardTsnChunk_RoundTrips_WithTwoStreamPairs()
		{
			Span<byte> pairs = stackalloc byte[8];
			BinaryPrimitives.WriteUInt16BigEndian(pairs.Slice(0, 2), 3); // stream id
			BinaryPrimitives.WriteUInt16BigEndian(pairs.Slice(2, 2), 9); // stream seq
			BinaryPrimitives.WriteUInt16BigEndian(pairs.Slice(4, 2), 4);
			BinaryPrimitives.WriteUInt16BigEndian(pairs.Slice(6, 2), 12);
			var forwardTsn = new ForwardTsnChunk(88888, pairs);

			Span<byte> packet = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
			n += forwardTsn.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 192, type);

			Assert.IsTrue(ForwardTsnChunk.TryParse(value, out ForwardTsnChunk parsed));
			Assert.AreEqual(88888u, parsed.NewCumulativeTsn);
			Assert.AreEqual(2, parsed.PairCount);
			Assert.AreEqual((3, 9), parsed.GetPair(0));
			Assert.AreEqual((4, 12), parsed.GetPair(1));
		}

		[TestMethod]
		public void AbortChunk_RoundTrips_WithARawCauseBlob()
		{
			ReadOnlySpan<byte> causeBlob = new byte[] { 0x00, 0x01, 0x00, 0x08, 0xDE, 0xAD, 0xBE, 0xEF };
			var abort = new AbortChunk(causeBlob);

			Span<byte> packet = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
			n += abort.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 6, type);

			Assert.IsTrue(AbortChunk.TryParse(value, out AbortChunk parsed));
			CollectionAssert.AreEqual(causeBlob.ToArray(), parsed.CauseData.ToArray());
		}

		[TestMethod]
		public void HeartbeatChunk_RoundTrips_WithOpaqueInfo()
		{
			ReadOnlySpan<byte> info = "opaque-heartbeat-info"u8;
			var heartbeat = new HeartbeatChunk(info);

			Span<byte> packet = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
			n += heartbeat.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 4, type);

			Assert.IsTrue(HeartbeatChunk.TryParse(value, out HeartbeatChunk parsed));
			CollectionAssert.AreEqual(info.ToArray(), parsed.Info.ToArray());

			// Echoed verbatim in the ACK: same info, different chunk type.
			var heartbeatAck = new HeartbeatChunk(parsed.Info, isAck: true);
			Span<byte> ackPacket = stackalloc byte[64];
			int ackN = SctpPacket.WriteHeader(ackPacket, 5000, 5000, 1);
			ackN += heartbeatAck.WriteTo(ackPacket.Slice(ackN));
			SctpPacket.FinishChecksum(ackPacket.Slice(0, ackN));

			var ackEnumerator = SctpPacket.EnumerateChunks(ackPacket.Slice(0, ackN));
			Assert.IsTrue(ackEnumerator.MoveNext());
			var (ackType, _, ackValue) = ackEnumerator.Current;
			Assert.AreEqual((byte) 5, ackType);
			Assert.IsTrue(HeartbeatChunk.TryParse(ackValue, out HeartbeatChunk parsedAck));
			CollectionAssert.AreEqual(info.ToArray(), parsedAck.Info.ToArray());
		}

		[TestMethod]
		public void CookieEchoChunk_RoundTrips_WithAnOpaqueCookie()
		{
			ReadOnlySpan<byte> cookie = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
			var cookieEcho = new CookieEchoChunk(cookie);

			Span<byte> packet = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 1);
			n += cookieEcho.WriteTo(packet.Slice(n));
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 10, type);

			Assert.IsTrue(CookieEchoChunk.TryParse(value, out CookieEchoChunk parsed));
			CollectionAssert.AreEqual(cookie.ToArray(), parsed.Cookie.ToArray());
		}

		// Oracle test: a real SIPSorcery INIT chunk, built the same way SctpAssociation.SendInit
		// builds one (SctpInitChunk with the INIT chunk type, fixed fields, and its own optional
		// parameters), must parse through our InitChunk.TryParse. SctpAssociation itself needs a
		// live ISctpTransport to drive, which is more machinery than this test needs; SctpInitChunk
		// is the same class SendInit constructs and serialises, so building and serialising it
		// directly reaches the identical wire bytes without that indirection. SupportedAddressTypes
		// and CookiePreservative are parameter types InitChunk does not recognise (it only acts on
		// Forward-TSN-Supported and State Cookie), so including them proves the TLV walk tolerates
		// real, well-formed, but unrecognised parameters rather than only tolerating an empty list.
		[TestMethod]
		public void InitChunk_TryParse_ToleratesARealSipSorceryInit()
		{
			var theirInit = new SIPSorcery.Net.SctpInitChunk(
				SIPSorcery.Net.SctpChunkType.INIT,
				initiateTag: 0x76543210,
				initialTSN: 999,
				arwnd: 262144,
				numberOutboundStreams: 65,
				numberInboundStreams: 65);
			theirInit.SupportedAddressTypes.Add(SIPSorcery.Net.SctpInitChunkParameterType.IPv4Address);
			theirInit.CookiePreservative = 30000;

			byte[] theirBytes = new byte[theirInit.GetChunkLength(true)];
			theirInit.WriteTo(theirBytes, 0);

			Span<byte> packet = stackalloc byte[256];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, 0);
			theirBytes.CopyTo(packet.Slice(n));
			n += theirBytes.Length;
			SctpPacket.FinishChecksum(packet.Slice(0, n));

			var enumerator = SctpPacket.EnumerateChunks(packet.Slice(0, n));
			Assert.IsTrue(enumerator.MoveNext());
			var (type, _, value) = enumerator.Current;
			Assert.AreEqual((byte) 1, type);

			Assert.IsTrue(InitChunk.TryParse(value, out InitChunk parsed));
			Assert.AreEqual(0x76543210u, parsed.InitiateTag);
			Assert.AreEqual((ushort) 65, parsed.OutboundStreams);
			Assert.AreEqual((ushort) 65, parsed.InboundStreams);
			Assert.AreEqual(999u, parsed.InitialTsn);
			Assert.IsFalse(parsed.ForwardTsnSupported); // SIPSorcery's INIT never sets it; unrecognised params were tolerated, not misread as this
			Assert.IsTrue(parsed.StateCookie.IsEmpty);
		}

		[TestMethod]
		public void HostileInput_TryParseReturnsFalseWithoutThrowing()
		{
			// A truncated INIT value: shorter than the 16-byte fixed part.
			Span<byte> truncatedInit = stackalloc byte[10];
			Assert.IsFalse(InitChunk.TryParse(truncatedInit, out _));

			// A SACK whose gap count claims more blocks than the value actually holds.
			Span<byte> shortSack = stackalloc byte[16]; // fixed(12) + room for one gap block only
			BinaryPrimitives.WriteUInt32BigEndian(shortSack.Slice(0, 4), 1);
			BinaryPrimitives.WriteUInt32BigEndian(shortSack.Slice(4, 4), 65536);
			BinaryPrimitives.WriteUInt16BigEndian(shortSack.Slice(8, 2), 5); // claims 5 gap blocks
			BinaryPrimitives.WriteUInt16BigEndian(shortSack.Slice(10, 2), 0);
			Assert.IsFalse(SackChunk.TryParse(shortSack, out _));
		}
	}
}