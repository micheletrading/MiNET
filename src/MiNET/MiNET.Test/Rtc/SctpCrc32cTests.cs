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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class SctpCrc32cTests
	{
		// RFC 3720 Appendix B.4 "CRC Examples". The RFC prints each expected CRC as four bytes in
		// wire (transmission) order, least-significant byte first; reassembled below as a
		// little-endian uint32 (e.g. bytes "aa 36 91 8a" -> 0x8a9136aa).

		[TestMethod]
		public void Compute_MatchesRfc3720B4_ThirtyTwoZeroBytes()
		{
			byte[] data = new byte[32];
			Assert.AreEqual(0x8a9136aau, SctpCrc32c.Compute(data));
			Assert.AreEqual(0x8a9136aau, ComputeViaTable(data));
		}

		[TestMethod]
		public void Compute_MatchesRfc3720B4_ThirtyTwoOxFFBytes()
		{
			byte[] data = new byte[32];
			Array.Fill(data, (byte) 0xFF);
			Assert.AreEqual(0x62a8ab43u, SctpCrc32c.Compute(data));
			Assert.AreEqual(0x62a8ab43u, ComputeViaTable(data));
		}

		[TestMethod]
		public void Compute_MatchesRfc3720B4_ThirtyTwoAscendingBytes()
		{
			byte[] data = new byte[32];
			for (int i = 0; i < data.Length; i++) data[i] = (byte) i;
			Assert.AreEqual(0x46dd794eu, SctpCrc32c.Compute(data));
			Assert.AreEqual(0x46dd794eu, ComputeViaTable(data));
		}

		[TestMethod]
		public void Compute_MatchesRfc3720B4_ThirtyTwoDescendingBytes()
		{
			byte[] data = new byte[32];
			for (int i = 0; i < data.Length; i++) data[i] = (byte) (0x1f - i);
			Assert.AreEqual(0x113fdb5cu, SctpCrc32c.Compute(data));
			Assert.AreEqual(0x113fdb5cu, ComputeViaTable(data));
		}

		[TestMethod]
		public void Compute_MatchesRfc3720B4_IscsiReadCommandPdu()
		{
			byte[] data =
			{
				0x01, 0xc0, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x14, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x04, 0x00,
				0x00, 0x00, 0x00, 0x14,
				0x00, 0x00, 0x00, 0x18,
				0x28, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x02, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00
			};
			Assert.AreEqual(0xd9963a56u, SctpCrc32c.Compute(data));
			Assert.AreEqual(0xd9963a56u, ComputeViaTable(data));
		}

		// Routes through the table path only (bypassing whatever Continue dispatches to on this
		// machine), with the same init/final XOR Compute applies, so the RFC vectors above are
		// verified against the table algorithm directly rather than transitively through agreement
		// with the hardware path.
		private static uint ComputeViaTable(ReadOnlySpan<byte> data)
		{
			return SctpCrc32c.ContinueTable(0xFFFFFFFF, data) ^ 0xFFFFFFFF;
		}

		// Every length 0..70 at every starting offset 0..8 inside a larger buffer, so the hardware
		// paths' 8-byte (SSE4.2 x64 / ARM64) and 4-byte (SSE4.2 32-bit) strides are each exercised
		// with a head, one or more full strides, and every possible tail remainder against the
		// same input the table path sees.
		[TestMethod]
		public void Compute_AgreesWithTablePath_AcrossLengthsAndOffsets()
		{
			var random = new Random(11);
			byte[] buffer = new byte[128];
			random.NextBytes(buffer);

			for (int offset = 0; offset <= 8; offset++)
			{
				for (int length = 0; length <= 70; length++)
				{
					if (offset + length > buffer.Length) continue;
					ReadOnlySpan<byte> span = buffer.AsSpan(offset, length);

					uint viaDispatch = SctpCrc32c.Continue(0xFFFFFFFF, span) ^ 0xFFFFFFFF;
					uint viaTable = SctpCrc32c.ContinueTable(0xFFFFFFFF, span) ^ 0xFFFFFFFF;

					Assert.AreEqual(viaTable, viaDispatch, $"offset={offset}, length={length}");
				}
			}
		}

		// Every split point through Continue must fold to the same running state as one call,
		// proving Continue's segmented contract (arbitrary split points compose) holds on
		// whichever path Continue dispatches to on the machine running the test.
		[TestMethod]
		public void Continue_AgreesWithCompute_AcrossEverySplitPoint()
		{
			var random = new Random(23);
			byte[] buffer = new byte[257];
			random.NextBytes(buffer);

			uint expected = SctpCrc32c.Compute(buffer);

			for (int split = 0; split <= buffer.Length; split++)
			{
				uint crc = SctpCrc32c.Continue(0xFFFFFFFF, buffer.AsSpan(0, split));
				crc = SctpCrc32c.Continue(crc, buffer.AsSpan(split));
				crc ^= 0xFFFFFFFF;

				Assert.AreEqual(expected, crc, $"split={split}");
			}
		}
	}
}
