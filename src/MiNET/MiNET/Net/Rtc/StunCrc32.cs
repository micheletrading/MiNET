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

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     CRC-32/ISO-HDLC (reflected polynomial 0xEDB88320, init and final XOR 0xFFFFFFFF), the
	///     checksum STUN's FINGERPRINT attribute is built from (RFC 5389 section 15.5).
	/// </summary>
	internal static class StunCrc32
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
					crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
				}
				table[i] = crc;
			}
			return table;
		}

		public static uint Compute(ReadOnlySpan<byte> data)
		{
			uint crc = 0xFFFFFFFF;
			foreach (byte b in data)
			{
				crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
			}
			return crc ^ 0xFFFFFFFF;
		}
	}
}
