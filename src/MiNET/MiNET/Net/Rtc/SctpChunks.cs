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
	///     Chunk type byte values (RFC 4960 section 3.2, plus the FORWARD-TSN extension). Only the
	///     values this file's structs need to write are listed.
	/// </summary>
	internal static class SctpChunkType
	{
		public const byte Data = 0;
		public const byte Init = 1;
		public const byte InitAck = 2;
		public const byte Sack = 3;
		public const byte Heartbeat = 4;
		public const byte HeartbeatAck = 5;
		public const byte Abort = 6;
		public const byte CookieEcho = 10;
		public const byte ForwardTsn = 192;
	}

	/// <summary>
	///     Shared chunk-framing writer: every chunk shares the same 4-byte type/flags/length header
	///     and the same pad-to-4 tail, so this is the one place that logic lives.
	/// </summary>
	internal static class SctpChunkCodec
	{
		public const int HeaderLength = 4;

		/// <summary>
		///     Writes the 4-byte chunk header (type, flags, and the length field, which excludes the
		///     trailing pad this method also writes) at the start of <paramref name="destination" />.
		///     The caller has already written <paramref name="valueLength" /> bytes of value content
		///     directly after the header. Returns the total padded length, header included.
		/// </summary>
		public static int FinishChunk(Span<byte> destination, byte type, byte flags, int valueLength)
		{
			destination[0] = type;
			destination[1] = flags;
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2, 2), (ushort) (HeaderLength + valueLength));

			int total = HeaderLength + valueLength;
			int padded = total + ((4 - total % 4) % 4);
			if (padded > total) destination.Slice(total, padded - total).Clear();
			return padded;
		}
	}

	/// <summary>
	///     INIT and INIT-ACK share this shape; the only structural difference is the State Cookie
	///     parameter, which only ever appears on the ACK. A non-empty <see cref="StateCookie" /> is
	///     therefore what makes <see cref="WriteTo" /> emit chunk type INIT-ACK instead of INIT.
	/// </summary>
	public readonly struct InitChunk
	{
		private const ushort ForwardTsnSupportedParameterType = 0xC000;
		private const ushort StateCookieParameterType = 0x0007;
		private const int FixedLength = 16;

		public readonly uint InitiateTag;
		public readonly uint Arwnd;
		public readonly ushort OutboundStreams;
		public readonly ushort InboundStreams;
		public readonly uint InitialTsn;
		public readonly bool ForwardTsnSupported;
		public readonly ReadOnlyMemory<byte> StateCookie;

		public InitChunk(uint initiateTag, uint arwnd, ushort outboundStreams, ushort inboundStreams, uint initialTsn, bool forwardTsnSupported, ReadOnlyMemory<byte> stateCookie = default)
		{
			InitiateTag = initiateTag;
			Arwnd = arwnd;
			OutboundStreams = outboundStreams;
			InboundStreams = inboundStreams;
			InitialTsn = initialTsn;
			ForwardTsnSupported = forwardTsnSupported;
			StateCookie = stateCookie;
		}

		/// <summary>
		///     Reads the 16-byte fixed part, then walks the TLV parameter list. A parameter shorter
		///     than its own 4-byte header, or one whose declared length reaches past the value, ends
		///     the walk in place rather than throwing or reading out of bounds, the same defensive
		///     shape as <see cref="SctpPacket.ChunkEnumerator" />. Anything that isn't
		///     Forward-TSN-Supported or State Cookie is unrecognised and is silently skipped, which is
		///     what lets this tolerate every parameter a real SCTP stack's INIT carries (IPv4 address,
		///     cookie preservative, supported address types, and so on).
		/// </summary>
		public static bool TryParse(ReadOnlySpan<byte> value, out InitChunk chunk)
		{
			chunk = default;
			if (value.Length < FixedLength) return false;

			uint initiateTag = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(0, 4));
			uint arwnd = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(4, 4));
			ushort outboundStreams = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(8, 2));
			ushort inboundStreams = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(10, 2));
			uint initialTsn = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(12, 4));

			bool forwardTsnSupported = false;
			byte[] stateCookie = null;

			int position = FixedLength;
			while (position + 4 <= value.Length)
			{
				ushort parameterType = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(position, 2));
				ushort parameterLength = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(position + 2, 2));
				if (parameterLength < 4 || position + parameterLength > value.Length) break;

				if (parameterType == ForwardTsnSupportedParameterType)
				{
					forwardTsnSupported = true;
				}
				else if (parameterType == StateCookieParameterType)
				{
					stateCookie = value.Slice(position + 4, parameterLength - 4).ToArray();
				}

				int padded = parameterLength + ((4 - parameterLength % 4) % 4);
				position += padded;
			}

			chunk = new InitChunk(initiateTag, arwnd, outboundStreams, inboundStreams, initialTsn, forwardTsnSupported, stateCookie);
			return true;
		}

		public int WriteTo(Span<byte> destination)
		{
			int valueLength = FixedLength;
			if (ForwardTsnSupported) valueLength += 4;

			int cookieParameterLength = 0;
			if (!StateCookie.IsEmpty)
			{
				cookieParameterLength = 4 + StateCookie.Length;
				valueLength += cookieParameterLength + ((4 - cookieParameterLength % 4) % 4);
			}

			Span<byte> value = destination.Slice(4, valueLength);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(0, 4), InitiateTag);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(4, 4), Arwnd);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(8, 2), OutboundStreams);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(10, 2), InboundStreams);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(12, 4), InitialTsn);

			int position = FixedLength;
			if (ForwardTsnSupported)
			{
				BinaryPrimitives.WriteUInt16BigEndian(value.Slice(position, 2), ForwardTsnSupportedParameterType);
				BinaryPrimitives.WriteUInt16BigEndian(value.Slice(position + 2, 2), 4);
				position += 4;
			}

			if (!StateCookie.IsEmpty)
			{
				BinaryPrimitives.WriteUInt16BigEndian(value.Slice(position, 2), StateCookieParameterType);
				BinaryPrimitives.WriteUInt16BigEndian(value.Slice(position + 2, 2), (ushort) cookieParameterLength);
				StateCookie.Span.CopyTo(value.Slice(position + 4));

				int padded = cookieParameterLength + ((4 - cookieParameterLength % 4) % 4);
				if (padded > cookieParameterLength) value.Slice(position + cookieParameterLength, padded - cookieParameterLength).Clear();
				position += padded;
			}

			byte type = StateCookie.IsEmpty ? SctpChunkType.Init : SctpChunkType.InitAck;
			return SctpChunkCodec.FinishChunk(destination, type, 0, valueLength);
		}
	}

	/// <summary>
	///     SACK: cumulative TSN ack, advertised receiver window, then gap-ack blocks (offsets
	///     relative to the cumulative TSN) and duplicate TSNs. Parsed counts beyond the cap are
	///     dropped rather than failing the parse; a declared count the value doesn't have room for
	///     fails the parse, since that is a truncated/hostile packet rather than a large-but-legal one.
	/// </summary>
	public readonly struct SackChunk
	{
		public const int MaxGapBlocks = 64;
		public const int MaxDuplicateTsns = 32;

		public readonly uint CumulativeTsnAck;
		public readonly uint Arwnd;
		public readonly GapBlock[] GapBlocks;
		public readonly uint[] DuplicateTsns;

		public SackChunk(uint cumulativeTsnAck, uint arwnd, GapBlock[] gapBlocks, uint[] duplicateTsns)
		{
			CumulativeTsnAck = cumulativeTsnAck;
			Arwnd = arwnd;
			GapBlocks = gapBlocks ?? Array.Empty<GapBlock>();
			DuplicateTsns = duplicateTsns ?? Array.Empty<uint>();
		}

		public readonly struct GapBlock
		{
			public readonly ushort Start;
			public readonly ushort End;

			public GapBlock(ushort start, ushort end)
			{
				Start = start;
				End = end;
			}
		}

		public static bool TryParse(ReadOnlySpan<byte> value, out SackChunk chunk)
		{
			chunk = default;
			if (value.Length < 12) return false;

			uint cumulativeTsnAck = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(0, 4));
			uint arwnd = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(4, 4));
			ushort gapCount = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(8, 2));
			ushort dupCount = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(10, 2));

			int needed = 12 + gapCount * 4 + dupCount * 4;
			if (value.Length < needed) return false;

			int position = 12;
			var gapBlocks = new GapBlock[Math.Min((int) gapCount, MaxGapBlocks)];
			for (int i = 0; i < gapCount; i++)
			{
				if (i < MaxGapBlocks)
				{
					ushort start = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(position, 2));
					ushort end = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(position + 2, 2));
					gapBlocks[i] = new GapBlock(start, end);
				}
				position += 4;
			}

			var duplicateTsns = new uint[Math.Min((int) dupCount, MaxDuplicateTsns)];
			for (int i = 0; i < dupCount; i++)
			{
				if (i < MaxDuplicateTsns) duplicateTsns[i] = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(position, 4));
				position += 4;
			}

			chunk = new SackChunk(cumulativeTsnAck, arwnd, gapBlocks, duplicateTsns);
			return true;
		}

		public int WriteTo(Span<byte> destination)
		{
			int gapCount = GapBlocks.Length;
			int dupCount = DuplicateTsns.Length;
			int valueLength = 12 + gapCount * 4 + dupCount * 4;

			Span<byte> value = destination.Slice(4, valueLength);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(0, 4), CumulativeTsnAck);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(4, 4), Arwnd);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(8, 2), (ushort) gapCount);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(10, 2), (ushort) dupCount);

			int position = 12;
			for (int i = 0; i < gapCount; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(value.Slice(position, 2), GapBlocks[i].Start);
				BinaryPrimitives.WriteUInt16BigEndian(value.Slice(position + 2, 2), GapBlocks[i].End);
				position += 4;
			}

			for (int i = 0; i < dupCount; i++)
			{
				BinaryPrimitives.WriteUInt32BigEndian(value.Slice(position, 4), DuplicateTsns[i]);
				position += 4;
			}

			return SctpChunkCodec.FinishChunk(destination, SctpChunkType.Sack, 0, valueLength);
		}
	}

	/// <summary>
	///     The 12-byte fixed part of a DATA chunk (TSN, stream id, stream sequence, PPID) plus the
	///     four flag bits packed into the chunk's flags byte. User data is never copied into this
	///     struct; it rides as a separate span on both <see cref="WriteTo" /> and <see cref="TryParse" />.
	///     The I/U/B/E flags live in the chunk header's flags byte, which
	///     <see cref="SctpPacket.EnumerateChunks" /> already separates from the value, so
	///     <see cref="TryParse" /> takes it as an explicit parameter rather than the single-span shape
	///     every other chunk in this file uses.
	/// </summary>
	public readonly struct DataChunkHeader
	{
		private const byte FlagImmediateSack = 0x08;
		private const byte FlagUnordered = 0x04;
		private const byte FlagBegin = 0x02;
		private const byte FlagEnd = 0x01;

		public readonly uint Tsn;
		public readonly ushort StreamId;
		public readonly ushort StreamSeq;
		public readonly uint Ppid;
		public readonly bool Unordered;
		public readonly bool Begin;
		public readonly bool End;
		public readonly bool ImmediateSack;

		public DataChunkHeader(uint tsn, ushort streamId, ushort streamSeq, uint ppid, bool unordered, bool begin, bool end, bool immediateSack)
		{
			Tsn = tsn;
			StreamId = streamId;
			StreamSeq = streamSeq;
			Ppid = ppid;
			Unordered = unordered;
			Begin = begin;
			End = end;
			ImmediateSack = immediateSack;
		}

		public static bool TryParse(byte flags, ReadOnlySpan<byte> value, out DataChunkHeader header, out ReadOnlySpan<byte> payload)
		{
			header = default;
			payload = default;
			if (value.Length < 12) return false;

			uint tsn = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(0, 4));
			ushort streamId = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(4, 2));
			ushort streamSeq = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(6, 2));
			uint ppid = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(8, 4));

			header = new DataChunkHeader(tsn, streamId, streamSeq, ppid,
				(flags & FlagUnordered) != 0, (flags & FlagBegin) != 0, (flags & FlagEnd) != 0, (flags & FlagImmediateSack) != 0);
			payload = value.Slice(12);
			return true;
		}

		public int WriteTo(Span<byte> destination, ReadOnlySpan<byte> userData)
		{
			int valueLength = 12 + userData.Length;
			Span<byte> value = destination.Slice(4, valueLength);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(0, 4), Tsn);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(4, 2), StreamId);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(6, 2), StreamSeq);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(8, 4), Ppid);
			userData.CopyTo(value.Slice(12));

			byte flags = 0;
			if (ImmediateSack) flags |= FlagImmediateSack;
			if (Unordered) flags |= FlagUnordered;
			if (Begin) flags |= FlagBegin;
			if (End) flags |= FlagEnd;

			return SctpChunkCodec.FinishChunk(destination, SctpChunkType.Data, flags, valueLength);
		}
	}

	/// <summary>
	///     COOKIE-ECHO: the state cookie a COOKIE-ACK's INIT-ACK issued, echoed back verbatim. Opaque
	///     to this codec; it never allocates, so it borrows the value span directly.
	/// </summary>
	public readonly ref struct CookieEchoChunk
	{
		public readonly ReadOnlySpan<byte> Cookie;

		public CookieEchoChunk(ReadOnlySpan<byte> cookie)
		{
			Cookie = cookie;
		}

		public static bool TryParse(ReadOnlySpan<byte> value, out CookieEchoChunk chunk)
		{
			chunk = new CookieEchoChunk(value);
			return true;
		}

		public int WriteTo(Span<byte> destination)
		{
			Cookie.CopyTo(destination.Slice(4));
			return SctpChunkCodec.FinishChunk(destination, SctpChunkType.CookieEcho, 0, Cookie.Length);
		}
	}

	/// <summary>
	///     ABORT: zero or more error-cause TLVs. No cause taxonomy is needed here, so the raw value
	///     span is exposed as-is; a caller that cares about individual causes can walk it separately.
	/// </summary>
	public readonly ref struct AbortChunk
	{
		public readonly ReadOnlySpan<byte> CauseData;

		public AbortChunk(ReadOnlySpan<byte> causeData)
		{
			CauseData = causeData;
		}

		public static bool TryParse(ReadOnlySpan<byte> value, out AbortChunk chunk)
		{
			chunk = new AbortChunk(value);
			return true;
		}

		public int WriteTo(Span<byte> destination)
		{
			CauseData.CopyTo(destination.Slice(4));
			return SctpChunkCodec.FinishChunk(destination, SctpChunkType.Abort, 0, CauseData.Length);
		}
	}

	/// <summary>
	///     HEARTBEAT / HEARTBEAT-ACK: an opaque Heartbeat Info parameter (type 1), echoed back
	///     verbatim in the ACK. <see cref="IsAck" /> selects which of the two chunk types
	///     <see cref="WriteTo" /> emits.
	/// </summary>
	public readonly ref struct HeartbeatChunk
	{
		private const ushort HeartbeatInfoParameterType = 1;

		public readonly ReadOnlySpan<byte> Info;
		public readonly bool IsAck;

		public HeartbeatChunk(ReadOnlySpan<byte> info, bool isAck = false)
		{
			Info = info;
			IsAck = isAck;
		}

		public static bool TryParse(ReadOnlySpan<byte> value, out HeartbeatChunk chunk)
		{
			chunk = default;
			if (value.Length < 4) return false;

			ushort parameterType = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(0, 2));
			ushort parameterLength = BinaryPrimitives.ReadUInt16BigEndian(value.Slice(2, 2));
			if (parameterType != HeartbeatInfoParameterType || parameterLength < 4 || parameterLength > value.Length) return false;

			chunk = new HeartbeatChunk(value.Slice(4, parameterLength - 4));
			return true;
		}

		public int WriteTo(Span<byte> destination)
		{
			int parameterLength = 4 + Info.Length;
			int valueLength = parameterLength + ((4 - parameterLength % 4) % 4);

			Span<byte> value = destination.Slice(4, valueLength);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(0, 2), HeartbeatInfoParameterType);
			BinaryPrimitives.WriteUInt16BigEndian(value.Slice(2, 2), (ushort) parameterLength);
			Info.CopyTo(value.Slice(4));
			if (valueLength > parameterLength) value.Slice(parameterLength, valueLength - parameterLength).Clear();

			byte type = IsAck ? SctpChunkType.HeartbeatAck : SctpChunkType.Heartbeat;
			return SctpChunkCodec.FinishChunk(destination, type, 0, valueLength);
		}
	}

	/// <summary>
	///     FORWARD-TSN: a new cumulative TSN plus zero or more (stream id, stream sequence) skip
	///     pairs. The pairs ride as a raw 4-byte-per-pair span, read out with <see cref="GetPair" />,
	///     rather than being copied into an array.
	/// </summary>
	public readonly ref struct ForwardTsnChunk
	{
		public readonly uint NewCumulativeTsn;
		private readonly ReadOnlySpan<byte> _pairs;

		public int PairCount => _pairs.Length / 4;

		public ForwardTsnChunk(uint newCumulativeTsn, ReadOnlySpan<byte> pairs)
		{
			NewCumulativeTsn = newCumulativeTsn;
			_pairs = pairs;
		}

		public (ushort StreamId, ushort StreamSeq) GetPair(int index)
		{
			int offset = index * 4;
			return (BinaryPrimitives.ReadUInt16BigEndian(_pairs.Slice(offset, 2)), BinaryPrimitives.ReadUInt16BigEndian(_pairs.Slice(offset + 2, 2)));
		}

		/// <summary>
		///     A trailing partial pair (value length not a multiple of 4 past the cumulative TSN) is
		///     dropped rather than failing the whole parse; every complete pair is still exposed.
		/// </summary>
		public static bool TryParse(ReadOnlySpan<byte> value, out ForwardTsnChunk chunk)
		{
			chunk = default;
			if (value.Length < 4) return false;

			uint newCumulativeTsn = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(0, 4));
			int pairsLength = (value.Length - 4) / 4 * 4;
			chunk = new ForwardTsnChunk(newCumulativeTsn, value.Slice(4, pairsLength));
			return true;
		}

		public int WriteTo(Span<byte> destination)
		{
			int valueLength = 4 + _pairs.Length;
			Span<byte> value = destination.Slice(4, valueLength);
			BinaryPrimitives.WriteUInt32BigEndian(value.Slice(0, 4), NewCumulativeTsn);
			_pairs.CopyTo(value.Slice(4));
			return SctpChunkCodec.FinishChunk(destination, SctpChunkType.ForwardTsn, 0, valueLength);
		}
	}
}