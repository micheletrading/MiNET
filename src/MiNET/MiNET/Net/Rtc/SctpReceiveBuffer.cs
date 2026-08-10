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
using System.Threading;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     RFC 1982 serial-number arithmetic for 32-bit TSNs. A plain <c>&lt;</c>/<c>&gt;</c> breaks the
	///     moment a TSN wraps past <see cref="uint.MaxValue" />, which a long-lived association will
	///     eventually do; every TSN comparison in this file goes through here instead.
	/// </summary>
	internal static class SctpTsn
	{
		/// <summary>Signed distance a-b in TSN space (positive when a is later than b).</summary>
		public static int Compare(uint a, uint b) => unchecked((int) (a - b));

		public static bool IsNewer(uint a, uint b) => Compare(a, b) > 0;
	}

	/// <summary>
	///     The same RFC 1982 serial arithmetic, sized for the 16-bit stream sequence numbers RFC 4960
	///     uses for per-stream ordering.
	/// </summary>
	internal static class SctpSeq
	{
		public static bool IsNewer(ushort a, ushort b) => unchecked((short) (a - b)) > 0;
	}

	/// <summary>
	///     The receive half of one SCTP association: TSN bookkeeping (cumulative ack point, the
	///     out-of-order gap set, duplicate detection), fragment reassembly, and per-stream ordered
	///     delivery, all bounded by a byte budget standing in for a_rwnd. Owned exclusively by one
	///     <see cref="SctpAssociation" />, driven only from its <see cref="SctpAssociation.OnPacketReceived" />
	///     (never concurrently, matching that method's own single-mux-thread assumption), so nothing
	///     here takes a lock of its own.
	///     <para>
	///     TSN accounting and message delivery are deliberately independent: a TSN is folded into the
	///     cumulative ack point (or the gap set) the moment its chunk is accepted, regardless of when -
	///     or whether - the message it belongs to becomes deliverable. An unordered message delivers as
	///     soon as its own fragments (which are always TSN-contiguous) are complete, even while an
	///     unrelated gap sits open elsewhere in the window; an ordered message additionally waits for
	///     its stream sequence number's turn.
	///     </para>
	/// </summary>
	internal sealed class SctpReceiveBuffer
	{
		// RFC 4960 6.9: an endpoint does not interleave fragments of two different user messages on the
		// same stream, so one in-progress reassembly run per stream is all a well-behaved peer ever
		// produces; a hostile peer that violates this just has its earlier run silently abandoned (the
		// straggler TSNs still occupy budget until Reset, but nothing throws or wedges).
		private const int MaxFragmentsPerMessage = 65536;

		private readonly uint _budgetBytes;
		private uint _bufferedBytes;
		private uint _cumulativeTsn;
		private bool _initialized;

		// TSNs newer than _cumulativeTsn that have been received but not yet folded into it, kept in
		// TSN order (via SctpTsn.Compare, so the order survives wraparound) for gap-block building and
		// duplicate detection.
		private readonly SortedSet<uint> _gapTsns = new(TsnOrder.Instance);

		// Individual DATA chunk payloads still waiting on their message to complete: mid-reassembly
		// fragments, keyed by their own TSN.
		private readonly Dictionary<uint, (byte[] Buffer, int Length)> _fragments = new();

		// One in-progress B../E run per stream.
		private readonly Dictionary<ushort, ReassemblyRun> _runs = new();

		// Next stream sequence number an ordered stream is due to deliver.
		private readonly Dictionary<ushort, ushort> _nextOrderedSeq = new();

		// Complete ordered messages buffered because an earlier stream sequence number hasn't arrived
		// yet, keyed by stream then by stream sequence number.
		private readonly Dictionary<ushort, Dictionary<ushort, (byte[] Buffer, int Length, uint Ppid)>> _orderedPending = new();

		private readonly List<uint> _duplicateTsns = new();
		private long _droppedByBudgetCount;

		/// <summary>
		///     Deliveries produced by the most recent <see cref="Receive" /> call: fragment-reassembly
		///     completions and any ordered messages a cascade unblocked. Cleared at the start of every
		///     <see cref="Receive" /> call, so the caller must drain it before calling <see cref="Receive" />
		///     again. Each buffer was leased from <see cref="ArrayPool{T}.Shared" /> and is the caller's to
		///     return once its callback has run. A single-chunk message that can be delivered the instant
		///     it arrives never appears here at all: <see cref="Receive" /> reports that case through its
		///     own return value instead, so the caller can hand the original incoming span straight to the
		///     application with no lease and no copy.
		/// </summary>
		public readonly List<LeasedDelivery> Deliveries = new();

		public SctpReceiveBuffer(uint budgetBytes)
		{
			_budgetBytes = budgetBytes;
		}

		/// <summary>Cumulative TSN Ack Point: the highest TSN such that every TSN up to and including it has been received.</summary>
		public uint CumulativeTsnAck => _cumulativeTsn;

		/// <summary>Budget minus buffered bytes; what an outgoing SACK reports as a_rwnd.</summary>
		public uint CurrentArwnd => _bufferedBytes >= _budgetBytes ? 0 : _budgetBytes - _bufferedBytes;

		/// <summary>Whether any TSN above the cumulative ack point is outstanding, i.e. a SACK would carry at least one gap block.</summary>
		public bool HasGap => _gapTsns.Count > 0;

		/// <summary>Test visibility only: how many DATA chunks were dropped for arriving when the byte budget was already spent.</summary>
		public long DroppedByBudgetCount => Interlocked.Read(ref _droppedByBudgetCount);

		/// <summary>
		///     (Re)arms the buffer for a fresh association: the peer's Initial TSN sets the cumulative ack
		///     point one behind the first DATA chunk's expected TSN. Any buffer this instance was already
		///     holding is released back to the pool first, so this is also what a reused instance would need
		///     between associations (stage 2 never reuses one, but nothing here assumes it won't).
		/// </summary>
		public void Reset(uint peerInitialTsn)
		{
			_cumulativeTsn = unchecked(peerInitialTsn - 1);
			_initialized = true;
			_bufferedBytes = 0;
			_gapTsns.Clear();

			foreach ((byte[] buffer, int _) in _fragments.Values) ArrayPool<byte>.Shared.Return(buffer);
			_fragments.Clear();
			_runs.Clear();
			_nextOrderedSeq.Clear();

			foreach (Dictionary<ushort, (byte[] Buffer, int Length, uint Ppid)> perStream in _orderedPending.Values)
			foreach ((byte[] buffer, int _, uint _) in perStream.Values)
				ArrayPool<byte>.Shared.Return(buffer);
			_orderedPending.Clear();

			_duplicateTsns.Clear();
			Deliveries.Clear();
		}

		/// <summary>
		///     Processes one DATA chunk. Returns true when <paramref name="payload" /> is a complete
		///     message the caller should deliver immediately, zero-copy, straight from that span (the
		///     chunk was Begin+End and either unordered or exactly the next due ordered message on its
		///     stream). Any other outcome - still-incomplete fragment, an ordered message not yet due, a
		///     completed reassembly, a duplicate, or a chunk dropped for exceeding the byte budget - is
		///     false; <see cref="Deliveries" /> may still gain entries in that case; the caller checks it
		///     regardless of the return value.
		/// </summary>
		public bool Receive(in DataChunkHeader header, ReadOnlySpan<byte> payload)
		{
			Deliveries.Clear();
			if (!_initialized) return false;

			uint tsn = header.Tsn;

			// Already delivered (at or behind the cumulative ack point), or already sitting in the
			// out-of-order set / fragment store: a retransmit, reported as a duplicate and otherwise
			// ignored rather than reprocessed.
			if (!SctpTsn.IsNewer(tsn, _cumulativeTsn) || _gapTsns.Contains(tsn) || _fragments.ContainsKey(tsn))
			{
				_duplicateTsns.Add(tsn);
				return false;
			}

			bool singleChunk = header.Begin && header.End;
			if (singleChunk)
			{
				if (header.Unordered)
				{
					RecordTsnReceived(tsn);
					return true;
				}

				switch (ClassifyOrdered(header.StreamId, header.StreamSeq))
				{
					case OrderedDisposition.DueNow:
						RecordTsnReceived(tsn);
						AdvanceOrderedSeqAndDrain(header.StreamId, header.StreamSeq);
						return true;

					case OrderedDisposition.Stale:
						// A stream sequence number older than what this stream has already delivered:
						// TSN dedup above should ordinarily have caught this as a retransmit, so treat
						// it the same way here - accept the TSN so the peer stops retransmitting it, but
						// there is nothing left to deliver.
						RecordTsnReceived(tsn);
						return false;

					// OrderedDisposition.WaitForTurn: falls through to buffering below.
				}
			}

			if (_bufferedBytes + (uint) payload.Length > _budgetBytes)
			{
				Interlocked.Increment(ref _droppedByBudgetCount);
				// Not recorded as received: the peer times out and retransmits it, exactly what a_rwnd
				// exists to make happen.
				return false;
			}

			byte[] leased = ArrayPool<byte>.Shared.Rent(payload.Length);
			payload.CopyTo(leased);
			_bufferedBytes += (uint) payload.Length;
			RecordTsnReceived(tsn);

			if (singleChunk)
			{
				// Complete ordered message, just not its turn yet.
				EnqueueOrderedPending(header.StreamId, header.StreamSeq, header.Ppid, leased, payload.Length);
			}
			else
			{
				BufferFragment(tsn, header, leased, payload.Length);
				TryCompleteRun(header.StreamId);
			}

			return false;
		}

		/// <summary>
		///     Fills <paramref name="destination" /> with gap-ack blocks (offsets relative to the
		///     cumulative TSN ack point, RFC 4960 3.3.4), ascending from the ack point and capped at
		///     <paramref name="destination" />'s length. Returns the number written.
		/// </summary>
		public int BuildGapBlocks(Span<SackChunk.GapBlock> destination)
		{
			int count = 0;
			bool inRun = false;
			uint runStart = 0;
			uint prev = 0;

			foreach (uint tsn in _gapTsns)
			{
				if (!inRun)
				{
					runStart = tsn;
					inRun = true;
				}
				else if (tsn != unchecked(prev + 1))
				{
					if (count >= destination.Length) return count;
					destination[count++] = new SackChunk.GapBlock((ushort) unchecked(runStart - _cumulativeTsn), (ushort) unchecked(prev - _cumulativeTsn));
					runStart = tsn;
				}

				prev = tsn;
			}

			if (inRun && count < destination.Length) destination[count++] = new SackChunk.GapBlock((ushort) unchecked(runStart - _cumulativeTsn), (ushort) unchecked(prev - _cumulativeTsn));

			return count;
		}

		/// <summary>
		///     Fills <paramref name="destination" /> with TSNs reported as duplicates since the last call
		///     and clears the pending list (RFC 4960 6.2: the duplicate report resets once a SACK carries
		///     it), capped at <paramref name="destination" />'s length. Returns the number written.
		/// </summary>
		public int DrainDuplicateTsns(Span<uint> destination)
		{
			int count = Math.Min(_duplicateTsns.Count, destination.Length);
			for (int i = 0; i < count; i++) destination[i] = _duplicateTsns[i];
			_duplicateTsns.Clear();
			return count;
		}

		private void RecordTsnReceived(uint tsn)
		{
			if (tsn == unchecked(_cumulativeTsn + 1))
			{
				_cumulativeTsn = tsn;

				uint next = unchecked(_cumulativeTsn + 1);
				while (_gapTsns.Remove(next))
				{
					_cumulativeTsn = next;
					next = unchecked(next + 1);
				}
			}
			else
			{
				_gapTsns.Add(tsn);
			}
		}

		private OrderedDisposition ClassifyOrdered(ushort streamId, ushort seq)
		{
			ushort expected = _nextOrderedSeq.TryGetValue(streamId, out ushort v) ? v : (ushort) 0;
			if (seq == expected) return OrderedDisposition.DueNow;
			return SctpSeq.IsNewer(seq, expected) ? OrderedDisposition.WaitForTurn : OrderedDisposition.Stale;
		}

		private void AdvanceOrderedSeqAndDrain(ushort streamId, ushort deliveredSeq)
		{
			ushort next = unchecked((ushort) (deliveredSeq + 1));
			_nextOrderedSeq[streamId] = next;

			if (!_orderedPending.TryGetValue(streamId, out Dictionary<ushort, (byte[] Buffer, int Length, uint Ppid)> pending)) return;

			while (pending.TryGetValue(next, out (byte[] Buffer, int Length, uint Ppid) msg))
			{
				pending.Remove(next);
				Deliveries.Add(new LeasedDelivery(streamId, msg.Ppid, msg.Buffer, msg.Length));
				_bufferedBytes -= (uint) msg.Length;

				next = unchecked((ushort) (next + 1));
				_nextOrderedSeq[streamId] = next;
			}
		}

		private void EnqueueOrderedPending(ushort streamId, ushort seq, uint ppid, byte[] buffer, int length)
		{
			if (!_orderedPending.TryGetValue(streamId, out Dictionary<ushort, (byte[] Buffer, int Length, uint Ppid)> pending))
			{
				pending = new Dictionary<ushort, (byte[] Buffer, int Length, uint Ppid)>();
				_orderedPending[streamId] = pending;
			}

			// A duplicate stream sequence number under a different TSN should not be reachable (TSN
			// dedup runs first), but if a hostile peer manages it anyway, release the entry it would
			// otherwise silently leak rather than ever throwing.
			if (pending.TryGetValue(seq, out (byte[] Buffer, int Length, uint Ppid) existing)) ArrayPool<byte>.Shared.Return(existing.Buffer);

			pending[seq] = (buffer, length, ppid);
		}

		private void BufferFragment(uint tsn, in DataChunkHeader header, byte[] leased, int length)
		{
			_fragments[tsn] = (leased, length);

			_runs.TryGetValue(header.StreamId, out ReassemblyRun run);
			run.Unordered = header.Unordered;
			run.StreamSeq = header.StreamSeq;
			run.Ppid = header.Ppid;
			if (header.Begin) run.BeginTsn = tsn;
			if (header.End) run.EndTsn = tsn;
			_runs[header.StreamId] = run;
		}

		private void TryCompleteRun(ushort streamId)
		{
			if (!_runs.TryGetValue(streamId, out ReassemblyRun run)) return;
			if (run.BeginTsn == null || run.EndTsn == null) return;

			uint begin = run.BeginTsn.Value;
			uint end = run.EndTsn.Value;
			int pieceCount = unchecked((int) (end - begin)) + 1;
			if (pieceCount <= 0 || pieceCount > MaxFragmentsPerMessage)
			{
				// Malformed (End TSN behind Begin TSN, or an absurd span): abandon this run rather than
				// looping over a hostile length. The individual fragment bytes stay accounted for in
				// _fragments/_bufferedBytes until budget pressure or the next Reset reclaims them.
				_runs.Remove(streamId);
				return;
			}

			int totalLength = 0;
			uint scan = begin;
			for (int i = 0; i < pieceCount; i++)
			{
				if (!_fragments.TryGetValue(scan, out (byte[] Buffer, int Length) piece)) return; // still incomplete
				totalLength += piece.Length;
				scan = unchecked(scan + 1);
			}

			byte[] combined = ArrayPool<byte>.Shared.Rent(totalLength);
			int offset = 0;
			scan = begin;
			for (int i = 0; i < pieceCount; i++)
			{
				(byte[] Buffer, int Length) piece = _fragments[scan];
				Array.Copy(piece.Buffer, 0, combined, offset, piece.Length);
				offset += piece.Length;
				ArrayPool<byte>.Shared.Return(piece.Buffer);
				_fragments.Remove(scan);
				_bufferedBytes -= (uint) piece.Length;
				scan = unchecked(scan + 1);
			}

			_runs.Remove(streamId);
			_bufferedBytes += (uint) totalLength;

			if (run.Unordered)
			{
				Deliveries.Add(new LeasedDelivery(streamId, run.Ppid, combined, totalLength));
				_bufferedBytes -= (uint) totalLength;
				return;
			}

			switch (ClassifyOrdered(streamId, run.StreamSeq))
			{
				case OrderedDisposition.DueNow:
					Deliveries.Add(new LeasedDelivery(streamId, run.Ppid, combined, totalLength));
					_bufferedBytes -= (uint) totalLength;
					AdvanceOrderedSeqAndDrain(streamId, run.StreamSeq);
					break;

				case OrderedDisposition.Stale:
					ArrayPool<byte>.Shared.Return(combined);
					_bufferedBytes -= (uint) totalLength;
					break;

				default: // WaitForTurn
					EnqueueOrderedPending(streamId, run.StreamSeq, run.Ppid, combined, totalLength);
					break;
			}
		}

		private enum OrderedDisposition
		{
			DueNow,
			WaitForTurn,
			Stale
		}

		private struct ReassemblyRun
		{
			public uint? BeginTsn;
			public uint? EndTsn;
			public bool Unordered;
			public ushort StreamSeq;
			public uint Ppid;
		}

		/// <summary>
		///     A comparer over the signed TSN distance (<see cref="SctpTsn.Compare" />) rather than raw
		///     uint order, so a <see cref="SortedSet{T}" /> built on it stays correctly ordered across a
		///     TSN wraparound. The comparison is translation-invariant (shifting every element's distance
		///     by however much the cumulative ack point has advanced preserves their relative order), so a
		///     single stateless instance works for the buffer's whole lifetime.
		/// </summary>
		private sealed class TsnOrder : IComparer<uint>
		{
			public static readonly TsnOrder Instance = new();

			public int Compare(uint x, uint y) => SctpTsn.Compare(x, y);
		}

		/// <summary>One message ready for delivery from a leased buffer; see <see cref="Deliveries" />.</summary>
		public readonly struct LeasedDelivery
		{
			public readonly ushort StreamId;
			public readonly uint Ppid;
			public readonly byte[] Buffer;
			public readonly int Length;

			public LeasedDelivery(ushort streamId, uint ppid, byte[] buffer, int length)
			{
				StreamId = streamId;
				Ppid = ppid;
				Buffer = buffer;
				Length = length;
			}
		}
	}
}