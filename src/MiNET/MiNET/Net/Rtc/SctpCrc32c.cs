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
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     CRC-32C/Castagnoli (reflected polynomial 0x82F63B78, init and final XOR 0xFFFFFFFF), the
	///     checksum SCTP's common header carries (RFC 4960 Appendix B).
	/// </summary>
	internal static class SctpCrc32c
	{
		private static readonly uint[] Table = BuildTable();

		private static uint[] BuildTable()
		{
			var table = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint crc = i;
				for (int bit = 0; bit < 8; bit++)
				{
					crc = (crc & 1) != 0 ? (crc >> 1) ^ 0x82F63B78 : crc >> 1;
				}
				table[i] = crc;
			}
			return table;
		}

		public static uint Compute(ReadOnlySpan<byte> data)
		{
			return Continue(0xFFFFFFFF, data) ^ 0xFFFFFFFF;
		}

		/// <summary>
		///     Folds another span into a running (not yet finalized) CRC state, so a caller can
		///     compute a checksum across non-contiguous segments (e.g. a header copy plus the rest
		///     of the original buffer) without concatenating them into one allocation first.
		///     The SSE4.2 and ARM CRC32C instructions consume and produce the same reflected
		///     accumulator as the table method, so the running state is identical bit-for-bit
		///     regardless of which path advanced it; segments can freely mix paths call to call.
		/// </summary>
		internal static uint Continue(uint crc, ReadOnlySpan<byte> data)
		{
			if (Sse42.X64.IsSupported) return ContinueSse42X64(crc, data);
			if (Sse42.IsSupported) return ContinueSse42(crc, data);
			if (Crc32.Arm64.IsSupported) return ContinueArm64(crc, data);
			if (Crc32.IsSupported) return ContinueArm32(crc, data);
			return ContinueTable(crc, data);
		}

		// Eight bytes per CRC32 instruction on the 64-bit accumulator; the final 0-7 bytes that
		// do not fill a ulong fall through to the byte-wise form, one instruction per leftover byte.
		private static uint ContinueSse42X64(uint crc, ReadOnlySpan<byte> data)
		{
			ulong acc = crc;
			while (data.Length >= 8)
			{
				acc = Sse42.X64.Crc32(acc, BinaryPrimitives.ReadUInt64LittleEndian(data));
				data = data.Slice(8);
			}
			crc = (uint) acc;
			foreach (byte b in data)
			{
				crc = Sse42.Crc32(crc, b);
			}
			return crc;
		}

		// Four bytes per CRC32 instruction where the 64-bit accumulator form is unavailable (32-bit
		// process); the final 0-3 bytes fall through to the byte-wise form.
		private static uint ContinueSse42(uint crc, ReadOnlySpan<byte> data)
		{
			while (data.Length >= 4)
			{
				crc = Sse42.Crc32(crc, BinaryPrimitives.ReadUInt32LittleEndian(data));
				data = data.Slice(4);
			}
			foreach (byte b in data)
			{
				crc = Sse42.Crc32(crc, b);
			}
			return crc;
		}

		// Eight bytes per CRC32C instruction on aarch64; the final 0-7 bytes fall through to the
		// byte-wise form.
		private static uint ContinueArm64(uint crc, ReadOnlySpan<byte> data)
		{
			while (data.Length >= 8)
			{
				crc = Crc32.Arm64.ComputeCrc32C(crc, BinaryPrimitives.ReadUInt64LittleEndian(data));
				data = data.Slice(8);
			}
			foreach (byte b in data)
			{
				crc = Crc32.ComputeCrc32C(crc, b);
			}
			return crc;
		}

		// 32-bit ARM has no wide accumulator form; every byte goes through one CRC32C instruction.
		private static uint ContinueArm32(uint crc, ReadOnlySpan<byte> data)
		{
			foreach (byte b in data)
			{
				crc = Crc32.ComputeCrc32C(crc, b);
			}
			return crc;
		}

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): the table path, callable directly so a test can assert it agrees with whichever path <see cref="Continue" /> dispatches to on the machine actually running the test.</summary>
		internal static uint ContinueTable(uint crc, ReadOnlySpan<byte> data)
		{
			foreach (byte b in data)
			{
				crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
			}
			return crc;
		}
	}
}