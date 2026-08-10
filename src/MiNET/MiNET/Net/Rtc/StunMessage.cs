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
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MiNET.Net.Rtc
{
	public enum StunMessageType : ushort
	{
		BindingRequest = 0x0001,
		BindingSuccessResponse = 0x0101,
		BindingErrorResponse = 0x0111
	}

	/// <summary>
	///     A span-first STUN (RFC 5389/8445) message codec: header plus the small set of
	///     attributes the ICE connectivity checks in this stack need. Task 2 appends
	///     MESSAGE-INTEGRITY and FINGERPRINT on top of <see cref="WriteTo" />.
	/// </summary>
	public class StunMessage
	{
		public const uint MagicCookie = 0x2112A442;
		public const int HeaderSize = 20;

		/// <summary>
		///     Largest message this stack emits. Callers size their stackalloc/lease with it.
		/// </summary>
		public const int MaxSize = 548;

		private const ushort AttributeUsername = 0x0006;
		private const ushort AttributeXorMappedAddress = 0x0020;
		private const ushort AttributePriority = 0x0024;
		private const ushort AttributeUseCandidate = 0x0025;
		private const ushort AttributeIceControlled = 0x8029;
		private const ushort AttributeIceControlling = 0x802A;

		private const byte FamilyIPv4 = 0x01;
		private const byte FamilyIPv6 = 0x02;

		public StunMessageType Type { get; set; }
		public byte[] TransactionId { get; set; }
		public string Username { get; set; }
		public IPEndPoint XorMappedAddress { get; set; }
		public uint? Priority { get; set; }
		public bool UseCandidate { get; set; }
		public ulong? IceControlling { get; set; }
		public ulong? IceControlled { get; set; }

		/// <summary>
		///     Cheap pre-filter for demultiplexing STUN from other traffic on the same socket
		///     (e.g. DTLS): first byte below 4, at least a full header, and the magic cookie
		///     in place. Does not validate the rest of the message.
		/// </summary>
		public static bool LooksLikeStun(ReadOnlySpan<byte> packet)
		{
			return packet.Length >= HeaderSize
				&& packet[0] < 4
				&& BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(4)) == MagicCookie;
		}

		public static StunMessage Parse(ReadOnlySpan<byte> packet)
		{
			if (!LooksLikeStun(packet)) throw new FormatException("Not a STUN message");

			int attributesLength = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(2));
			if (HeaderSize + attributesLength > packet.Length) throw new FormatException("STUN message length exceeds packet");

			var message = new StunMessage
			{
				Type = (StunMessageType) BinaryPrimitives.ReadUInt16BigEndian(packet),
				TransactionId = packet.Slice(8, 12).ToArray()
			};

			int offset = HeaderSize;
			int end = HeaderSize + attributesLength;
			while (offset + 4 <= end)
			{
				ushort attributeType = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(offset));
				ushort attributeLength = BinaryPrimitives.ReadUInt16BigEndian(packet.Slice(offset + 2));
				int valueOffset = offset + 4;
				if (valueOffset + attributeLength > end) throw new FormatException("STUN attribute exceeds message");

				ReadOnlySpan<byte> value = packet.Slice(valueOffset, attributeLength);

				switch (attributeType)
				{
					case AttributeUsername:
						message.Username = Encoding.UTF8.GetString(value);
						break;

					case AttributePriority:
						if (attributeLength >= 4) message.Priority = BinaryPrimitives.ReadUInt32BigEndian(value);
						break;

					case AttributeUseCandidate:
						message.UseCandidate = true;
						break;

					case AttributeIceControlling:
						if (attributeLength >= 8) message.IceControlling = BinaryPrimitives.ReadUInt64BigEndian(value);
						break;

					case AttributeIceControlled:
						if (attributeLength >= 8) message.IceControlled = BinaryPrimitives.ReadUInt64BigEndian(value);
						break;

					case AttributeXorMappedAddress:
						message.XorMappedAddress = ParseXorMappedAddress(value, message.TransactionId);
						break;

					default:
						// Unknown attribute. Comprehension-optional (type >= 0x8000) attributes are
						// meant to be skipped; unknown comprehension-required attributes below 0x8000
						// also just skip here, we are not a general purpose STUN server.
						break;
				}

				int padded = (attributeLength + 3) & ~3;
				offset = valueOffset + padded;
			}

			return message;
		}

		/// <summary>
		///     Writes the header and attributes in a fixed order: USERNAME, PRIORITY,
		///     USE-CANDIDATE, ICE-CONTROLLING, ICE-CONTROLLED, XOR-MAPPED-ADDRESS. Task 2's
		///     overload appends MESSAGE-INTEGRITY then FINGERPRINT after this and re-patches
		///     the length field.
		/// </summary>
		public int WriteTo(Span<byte> destination)
		{
			BinaryPrimitives.WriteUInt16BigEndian(destination, (ushort) Type);
			BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4), MagicCookie);
			TransactionId.AsSpan().CopyTo(destination.Slice(8, 12));

			int offset = HeaderSize;

			if (Username != null)
			{
				int valueLength = Encoding.UTF8.GetByteCount(Username);
				offset += WriteAttributeHeader(destination.Slice(offset), AttributeUsername, (ushort) valueLength);
				Encoding.UTF8.GetBytes(Username.AsSpan(), destination.Slice(offset, valueLength));

				int padding = (4 - valueLength % 4) % 4;
				if (padding > 0) destination.Slice(offset + valueLength, padding).Clear();
				offset += valueLength + padding;
			}

			if (Priority.HasValue)
			{
				offset += WriteAttributeHeader(destination.Slice(offset), AttributePriority, 4);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(offset), Priority.Value);
				offset += 4;
			}

			if (UseCandidate)
			{
				offset += WriteAttributeHeader(destination.Slice(offset), AttributeUseCandidate, 0);
			}

			if (IceControlling.HasValue)
			{
				offset += WriteAttributeHeader(destination.Slice(offset), AttributeIceControlling, 8);
				BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset), IceControlling.Value);
				offset += 8;
			}

			if (IceControlled.HasValue)
			{
				offset += WriteAttributeHeader(destination.Slice(offset), AttributeIceControlled, 8);
				BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(offset), IceControlled.Value);
				offset += 8;
			}

			if (XorMappedAddress != null)
			{
				offset += WriteXorMappedAddress(destination.Slice(offset), XorMappedAddress, TransactionId);
			}

			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2), (ushort) (offset - HeaderSize));

			return offset;
		}

		private static int WriteAttributeHeader(Span<byte> destination, ushort attributeType, ushort valueLength)
		{
			BinaryPrimitives.WriteUInt16BigEndian(destination, attributeType);
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2), valueLength);
			return 4;
		}

		private static int WriteXorMappedAddress(Span<byte> destination, IPEndPoint endPoint, byte[] transactionId)
		{
			bool isIPv4 = endPoint.AddressFamily == AddressFamily.InterNetwork;
			int addressLength = isIPv4 ? 4 : 16;
			int valueLength = 4 + addressLength;

			int written = WriteAttributeHeader(destination, AttributeXorMappedAddress, (ushort) valueLength);
			Span<byte> value = destination.Slice(written, valueLength);

			value[0] = 0;
			value[1] = isIPv4 ? FamilyIPv4 : FamilyIPv6;
			ushort xorPort = (ushort) (endPoint.Port ^ (int) (MagicCookie >> 16));
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(2), xorPort);

			Span<byte> xorPad = stackalloc byte[16];
			BinaryPrimitives.WriteUInt32BigEndian(xorPad, MagicCookie);
			if (!isIPv4) transactionId.AsSpan().CopyTo(xorPad.Slice(4));

			Span<byte> addressBytes = stackalloc byte[16];
			endPoint.Address.TryWriteBytes(addressBytes, out _);

			for (int i = 0; i < addressLength; i++)
			{
				value[4 + i] = (byte) (addressBytes[i] ^ xorPad[i]);
			}

			return written + valueLength;
		}

		private static IPEndPoint ParseXorMappedAddress(ReadOnlySpan<byte> value, byte[] transactionId)
		{
			if (value.Length < 4) throw new FormatException("XOR-MAPPED-ADDRESS attribute too short");

			byte family = value[1];
			int addressLength = family switch
			{
				FamilyIPv4 => 4,
				FamilyIPv6 => 16,
				_ => throw new FormatException($"Unknown XOR-MAPPED-ADDRESS family {family}")
			};

			if (value.Length < 4 + addressLength) throw new FormatException("XOR-MAPPED-ADDRESS attribute too short");

			int port = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2)) ^ (int) (MagicCookie >> 16);

			Span<byte> xorPad = stackalloc byte[16];
			BinaryPrimitives.WriteUInt32BigEndian(xorPad, MagicCookie);
			if (family == FamilyIPv6) transactionId.AsSpan().CopyTo(xorPad.Slice(4));

			Span<byte> addressBytes = stackalloc byte[16];
			for (int i = 0; i < addressLength; i++)
			{
				addressBytes[i] = (byte) (value[4 + i] ^ xorPad[i]);
			}

			var address = new IPAddress(addressBytes.Slice(0, addressLength));
			return new IPEndPoint(address, port);
		}
	}
}