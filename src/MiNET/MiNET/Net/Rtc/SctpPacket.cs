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

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     A span-first SCTP (RFC 4960) common-header codec plus a bounds-safe chunk walker.
	///     Every other header field is network (big-endian) byte order, but the checksum is not:
	///     proven against a reference SCTP stack's wire output, the checksum is stored
	///     little-endian, byte-swapped relative to the rest of the header.
	/// </summary>
	public static class SctpPacket
	{
		/// <summary>
		///     Largest packet this stack sends; matches the SCTP association's outbound PMTU
		///     assumption (RFC 8831 recommends 1200 for WebRTC data channels).
		/// </summary>
		public const int MaxSize = 1200;

		private const int HeaderSize = 12;
		private const int ChunkHeaderSize = 4;

		public static int WriteHeader(Span<byte> destination, ushort sourcePort, ushort destinationPort, uint verificationTag)
		{
			BinaryPrimitives.WriteUInt16BigEndian(destination, sourcePort);
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2, 2), destinationPort);
			BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), verificationTag);
			BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), 0); // checksum, filled in by FinishChecksum
			return HeaderSize;
		}

		public static void FinishChecksum(Span<byte> packet)
		{
			BinaryPrimitives.WriteUInt32BigEndian(packet.Slice(8, 4), 0);
			uint crc = SctpCrc32c.Compute(packet);
			// The checksum quirk: stored little-endian, unlike every other field in this header.
			BinaryPrimitives.WriteUInt32LittleEndian(packet.Slice(8, 4), crc);
		}

		public static bool TryReadHeader(ReadOnlySpan<byte> packet, out ushort sourcePort, out ushort destinationPort, out uint verificationTag)
		{
			sourcePort = 0;
			destinationPort = 0;
			verificationTag = 0;

			if (packet.Length < HeaderSize) return false;

			uint storedChecksum = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(8, 4));

			Span<byte> header = stackalloc byte[HeaderSize];
			packet.Slice(0, HeaderSize).CopyTo(header);
			BinaryPrimitives.WriteUInt32BigEndian(header.Slice(8, 4), 0);

			uint crc = SctpCrc32c.Continue(0xFFFFFFFF, header);
			crc = SctpCrc32c.Continue(crc, packet.Slice(HeaderSize));
			crc ^= 0xFFFFFFFF;

			if (crc != storedChecksum) return false;

			sourcePort = BinaryPrimitives.ReadUInt16BigEndian(packet);
			destinationPort = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(2, 2));
			verificationTag = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(4, 4));
			return true;
		}

		public static ChunkEnumerator EnumerateChunks(ReadOnlySpan<byte> packet)
		{
			return new ChunkEnumerator(packet);
		}

		/// <summary>
		///     One decoded chunk: its type, flags, and value (the chunk's own 4-byte header
		///     stripped off). Borrows from the packet span given to <see cref="EnumerateChunks" />.
		/// </summary>
		public readonly ref struct Chunk
		{
			public readonly byte Type;
			public readonly byte Flags;
			public readonly ReadOnlySpan<byte> Value;

			internal Chunk(byte type, byte flags, ReadOnlySpan<byte> value)
			{
				Type = type;
				Flags = flags;
				Value = value;
			}

			public void Deconstruct(out byte type, out byte flags, out ReadOnlySpan<byte> value)
			{
				type = Type;
				flags = Flags;
				value = Value;
			}
		}

		/// <summary>
		///     Walks the chunks after the 12-byte common header. Never throws: a chunk whose
		///     declared length is shorter than its own header, or reaches past the packet end,
		///     terminates enumeration on the spot, the same way a truncated (unpadded) tail does
		///     on the following call. This sits on the receive path, so hostile input is routine
		///     input, not an edge case.
		/// </summary>
		public ref struct ChunkEnumerator
		{
			private readonly ReadOnlySpan<byte> _packet;
			private int _position;
			private Chunk _current;

			internal ChunkEnumerator(ReadOnlySpan<byte> packet)
			{
				_packet = packet;
				_position = HeaderSize;
				_current = default;
			}

			public readonly Chunk Current => _current;

			public bool MoveNext()
			{
				ReadOnlySpan<byte> packet = _packet;
				int position = _position;

				if (position + ChunkHeaderSize > packet.Length) return false;

				byte type = packet[position];
				byte flags = packet[position + 1];
				ushort length = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(position + 2, 2));

				// A chunk is at minimum its own 4-byte header; a shorter (including zero) declared
				// length is malformed and ends enumeration instead of looping or reading past it.
				if (length < ChunkHeaderSize) return false;

				// A declared length reaching past the packet end is equally malformed.
				if (position + length > packet.Length) return false;

				_current = new Chunk(type, flags, packet.Slice(position + ChunkHeaderSize, length - ChunkHeaderSize));

				int padded = length + ((4 - length % 4) % 4);
				_position = position + padded;
				return true;
			}
		}
	}
}