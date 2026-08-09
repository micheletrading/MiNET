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
using System.IO;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     NetherNet's entire framing, which is one byte. SCTP already gives reliability, ordering and
	///     per-message boundaries, so the only job left is splitting a message that exceeds the
	///     negotiated SCTP maximum, and that byte is the countdown of segments still to come.
	///     Zero means either a whole message or the last segment of one.
	///     Segments therefore arrive with a descending header: a three part message is 2, 1, 0.
	/// </summary>
	public static class NetherNetSegments
	{
		public const int HeaderSize = 1;

		/// <summary>
		///     The largest a single segment header can count, so the largest message that can be sent
		///     is 255 segments plus the final one.
		/// </summary>
		public const int MaxSegments = byte.MaxValue + 1;

		/// <summary>
		///     Receives one wire-ready segment. Takes the buffer and a length rather than a sized
		///     array because the buffer is pooled and deliberately larger than the segment.
		/// </summary>
		public delegate void SegmentHandler<in TState>(TState state, byte[] buffer, int length);

		/// <summary>
		///     Writes each segment of a payload in wire order, reusing a single pooled buffer for all
		///     of them. Prepending one header byte is the only reason a copy happens at all, so this
		///     is deliberately the one copy: no per-segment array, no list, and the state parameter
		///     keeps the handler from capturing anything.
		/// </summary>
		public static void ForEachSegment<TState>(ReadOnlySpan<byte> payload, int maxMessageSize, TState state, SegmentHandler<TState> handler)
		{
			if (maxMessageSize <= HeaderSize) throw new ArgumentOutOfRangeException(nameof(maxMessageSize), $"Max message size {maxMessageSize} leaves no room for a payload");

			int usable = maxMessageSize - HeaderSize;
			int count = Math.Max(1, (payload.Length + usable - 1) / usable);

			if (count > MaxSegments) throw new ArgumentOutOfRangeException(nameof(payload), $"Payload of {payload.Length} bytes needs {count} segments, more than the {MaxSegments} a one byte counter can express");

			// TODO: remove the copy below. It exists only to get one header byte in front of the
			// payload, so every batch is copied in full on every send for the sake of one byte. The
			// fix is upstream: have Compression.CompressPacketsForWrapper reserve HeaderSize bytes of
			// headroom at the front of the buffer it builds, so this can write the header in place
			// and hand the same buffer straight to send(buffer, offset, count) with no copy at all.
			// Not done inline because that buffer is shared with the RakNet path, which wants the
			// payload at offset zero, so the headroom has to become part of the contract rather than
			// a surprise. Until then this is one pooled buffer and one copy, which is the floor
			// without touching that contract.

			// Every segment but the last is exactly usable bytes, so one buffer of the largest size
			// serves all of them.
			byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Min(usable, Math.Max(payload.Length, 1)) + HeaderSize);
			try
			{
				for (int i = 0; i < count; i++)
				{
					int offset = i * usable;
					int length = Math.Min(usable, payload.Length - offset);

					// Header counts down: how many segments still follow this one.
					buffer[0] = (byte) (count - 1 - i);
					payload.Slice(offset, length).CopyTo(buffer.AsSpan(HeaderSize));

					handler(state, buffer, HeaderSize + length);
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		/// <summary>
		///     The allocating form, for tests and for anywhere a materialised list is genuinely
		///     wanted. Production sends go through <see cref="ForEachSegment{TState}" />.
		/// </summary>
		public static List<byte[]> Split(ReadOnlySpan<byte> payload, int maxMessageSize)
		{
			var segments = new List<byte[]>();
			ForEachSegment(payload, maxMessageSize, segments, static (list, buffer, length) => list.Add(buffer.AsSpan(0, length).ToArray()));
			return segments;
		}
	}

	/// <summary>
	///     Reassembles the segments of one data channel. A channel is ordered and reliable, so the
	///     segments of a message arrive together and in order, which is why this needs no keying by
	///     message id: there is only ever one message in flight.
	/// </summary>
	public class NetherNetSegmentReassembler
	{
		// A rented buffer and a write position, not a stream. The size is known from the first
		// segment, so there is nothing to grow and nothing to discover: every segment but the last
		// is exactly one payload long by construction, which makes (remaining + 1) * length either
		// exact or one short segment over.
		private byte[] _pending;
		private int _length;
		private int _expected = -1;

		/// <summary>
		///     Feeds one received data channel message. Returns false while a message is still being
		///     assembled from segments, true with the complete payload once the last one arrives.
		///     <para>
		///         The returned memory is a view, never a copy, so it is valid only until the next
		///         call on this reassembler. That is enough because the caller hands it straight to
		///         <c>HandlePacket</c>, which reads it through a <see cref="Utils.IO.MemoryStreamReader" />
		///         and copies out before returning. Nothing downstream slices it into a packet.
		///     </para>
		/// </summary>
		public bool TryAccept(ReadOnlyMemory<byte> framed, out ReadOnlyMemory<byte> message)
		{
			if (framed.Length < NetherNetSegments.HeaderSize) throw new IOException("NetherNet message arrived with no header byte");

			int remaining = framed.Span[0];
			ReadOnlyMemory<byte> payload = framed.Slice(NetherNetSegments.HeaderSize);

			// The common case by far, and the only one on a Bedrock batch under 256KB. The header is
			// skipped by an offset rather than by moving the batch, so this costs nothing at all:
			// no copy, no allocation, just a view onto the buffer the data channel already handed us.
			// _expected, not _pending, says whether a message is in progress: the buffer is kept
			// rented between messages so a fragmenting peer does not re-rent on every one.
			if (remaining == 0 && _expected < 0)
			{
				message = payload;
				return true;
			}

			if (_expected < 0)
			{
				int needed = (remaining + 1) * payload.Length;
				if (_pending == null || _pending.Length < needed)
				{
					if (_pending != null) ArrayPool<byte>.Shared.Return(_pending);
					_pending = ArrayPool<byte>.Shared.Rent(needed);
				}

				_length = 0;
			}
			else if (remaining != _expected)
			{
				// The counter is the only integrity check the format has. A gap means the channel
				// delivered out of order, which reliable ordered SCTP must not do, so the session is
				// no longer trustworthy rather than merely missing a packet.
				int expected = _expected;
				Reset();
				throw new IOException($"NetherNet segment counter jumped: expected {expected}, got {remaining}");
			}

			// A peer is not obliged to make its segments uniform, so the estimate is a strong hint
			// rather than a guarantee, and being wrong must not corrupt the message.
			if (_length + payload.Length > _pending.Length) Grow(_length + payload.Length);

			payload.Span.CopyTo(_pending.AsSpan(_length));
			_length += payload.Length;

			if (remaining > 0)
			{
				_expected = remaining - 1;
				message = default;
				return false;
			}

			// A view onto the pooled buffer, not a copy. The buffer is deliberately not returned to
			// the pool here: it stays rented until the next call, which is exactly as long as the
			// contract above promises the memory stays valid.
			message = _pending.AsMemory(0, _length);
			_expected = -1;
			return true;
		}

		private void Grow(int required)
		{
			byte[] bigger = ArrayPool<byte>.Shared.Rent(required);
			_pending.AsSpan(0, _length).CopyTo(bigger);
			ArrayPool<byte>.Shared.Return(_pending);
			_pending = bigger;
		}

		private void Reset()
		{
			if (_pending != null) ArrayPool<byte>.Shared.Return(_pending);
			_pending = null;
			_length = 0;
			_expected = -1;
		}
	}
}
