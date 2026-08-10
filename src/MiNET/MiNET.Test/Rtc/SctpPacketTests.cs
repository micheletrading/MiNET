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
			var parsed = SIPSorcery.Net.SctpPacket.Parse(ours.Slice(0, n).ToArray(), 0, n);
			Assert.IsNotNull(parsed);
		}

		[TestMethod]
		public void HostileChunkLengths_TerminateCleanly()
		{
			Span<byte> packet = stackalloc byte[32];
			SctpPacket.WriteHeader(packet, 5000, 5000, 1);
			packet[12] = 0; packet[13] = 0;
			BinaryPrimitives.WriteUInt16BigEndian(packet.Slice(14), 0); // zero-length chunk
			SctpPacket.FinishChecksum(packet);
			int count = 0;
			var e = SctpPacket.EnumerateChunks(packet);
			while (e.MoveNext()) count++;
			Assert.AreEqual(0, count); // malformed chunk terminates enumeration, no infinite loop
		}
	}
}