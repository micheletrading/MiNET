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
	public class SctpPacketTests
	{
		[TestMethod]
		public void Crc32c_MatchesTheStandardVector()
		{
			Assert.AreEqual(0xE3069283u, SctpCrc32c.Compute("123456789"u8));
		}

		// The oracle test that settles checksum byte order: SIPSorcery builds a packet, we must
		// validate it; we build one, SIPSorcery must validate it. Their SctpPacket lives in
		// SIPSorcery.Net. There is no SctpHeartbeatChunk class in the referenced SIPSorcery build
		// (10.0.13): a HEARTBEAT chunk with no value is just the base SctpChunk constructed with
		// SctpChunkType.HEARTBEAT, which is exactly the raw 4-byte chunk this test builds by hand
		// for the reverse direction.
		//
		// Two halves, two different things proven:
		//  - "theirs, read by us" compares checksums (our TryReadHeader recomputes and checks
		//    against the checksum they wrote), so this half is the one that actually settles the
		//    byte order.
		//  - "ours, read by them" calls both SctpPacket.VerifyChecksum (their real receive-path
		//    checksum check, used at SctpUdpTransport.cs:72 in their source) and SctpPacket.Parse.
		//    VerifyChecksum is what makes this half check checksum agreement too, symmetric with
		//    the first half; Parse on top additionally proves the header/chunk framing (ports,
		//    tag, chunk type/flags/length layout) round-trips through a real SCTP stack, which
		//    VerifyChecksum alone would not exercise since it never looks at the chunks.
		[TestMethod]
		public void ChecksumByteOrder_AgreesWithSipSorcery()
		{
			var theirs = new SIPSorcery.Net.SctpPacket(5000, 5000, 0x11223344);
			theirs.AddChunk(new SIPSorcery.Net.SctpChunk(SIPSorcery.Net.SctpChunkType.HEARTBEAT));
			byte[] theirBytes = theirs.GetBytes();
			Assert.IsTrue(SctpPacket.TryReadHeader(theirBytes, out ushort src, out _, out uint tag));
			Assert.AreEqual((ushort) 5000, src);
			Assert.AreEqual(0x11223344u, tag);

			Span<byte> ours = stackalloc byte[64];
			int n = SctpPacket.WriteHeader(ours, 5000, 5000, 0x11223344);
			// one HEARTBEAT chunk, type 4, no value
			ours[n] = 4; ours[n + 1] = 0;
			BinaryPrimitives.WriteUInt16BigEndian(ours.Slice(n + 2), 4);
			n += 4;
			SctpPacket.FinishChecksum(ours.Slice(0, n));
			byte[] oursBytes = ours.Slice(0, n).ToArray();
			Assert.IsTrue(SIPSorcery.Net.SctpPacket.VerifyChecksum(oursBytes, 0, n));
			var parsed = SIPSorcery.Net.SctpPacket.Parse(oursBytes, 0, n);
			Assert.IsNotNull(parsed);
		}

		[TestMethod]
		public void HostileChunkLengths_TerminateCleanly()
		{
			// Zero-length chunk: shorter than even the chunk's own 4-byte header.
			Span<byte> zeroLength = stackalloc byte[32];
			SctpPacket.WriteHeader(zeroLength, 5000, 5000, 1);
			zeroLength[12] = 0; zeroLength[13] = 0;
			BinaryPrimitives.WriteUInt16BigEndian(zeroLength.Slice(14), 0); // zero-length chunk
			SctpPacket.FinishChecksum(zeroLength);
			int zeroLengthCount = 0;
			var zeroLengthEnumerator = SctpPacket.EnumerateChunks(zeroLength);
			while (zeroLengthEnumerator.MoveNext()) zeroLengthCount++;
			Assert.AreEqual(0, zeroLengthCount); // malformed chunk terminates enumeration, no infinite loop

			// Oversized length: the chunk's declared length reaches past the end of the packet.
			Span<byte> oversized = stackalloc byte[32];
			SctpPacket.WriteHeader(oversized, 5000, 5000, 1);
			oversized[12] = 7; oversized[13] = 0;
			BinaryPrimitives.WriteUInt16BigEndian(oversized.Slice(14), 1000); // claims 1000 bytes, packet is 32
			SctpPacket.FinishChecksum(oversized);
			int oversizedCount = 0;
			var oversizedEnumerator = SctpPacket.EnumerateChunks(oversized);
			while (oversizedEnumerator.MoveNext()) oversizedCount++;
			Assert.AreEqual(0, oversizedCount); // terminates cleanly instead of slicing out of bounds

			// Unpadded tail: one valid chunk, then a second, final chunk whose declared length is
			// valid (>= 4) and fits the buffer exactly unpadded (a spec-legal last chunk: RFC 4960
			// only pads to keep a FOLLOWING chunk aligned, so nothing sent after the last chunk is
			// required to pad it out). Both chunks must still yield; the interesting part is what
			// happens next, since the enumerator's internal cursor steps by the PADDED length and so
			// lands past the end of the buffer. The following call must still terminate cleanly
			// rather than reading out of bounds.
			Span<byte> unpaddedTail = stackalloc byte[21];
			SctpPacket.WriteHeader(unpaddedTail, 5000, 5000, 1);
			unpaddedTail[12] = 4; unpaddedTail[13] = 0; // first chunk: type 4 (HEARTBEAT), no value
			BinaryPrimitives.WriteUInt16BigEndian(unpaddedTail.Slice(14), 4);
			unpaddedTail[16] = 9; unpaddedTail[17] = 0; // second chunk: type 9, length 5 (1-byte value)
			BinaryPrimitives.WriteUInt16BigEndian(unpaddedTail.Slice(18), 5);
			unpaddedTail[20] = 0xAB; // the second chunk's single value byte; its padding (3 bytes) is absent
			SctpPacket.FinishChecksum(unpaddedTail);
			int unpaddedTailCount = 0;
			var unpaddedTailEnumerator = SctpPacket.EnumerateChunks(unpaddedTail);
			while (unpaddedTailEnumerator.MoveNext()) unpaddedTailCount++;
			Assert.AreEqual(2, unpaddedTailCount); // both chunks yield; the cursor overrun after the last one does not throw
		}
	}
}