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
	///     soon as its own fragments are complete, even while an unrelated gap sits open elsewhere in the
	///     window; an ordered message additionally waits for its stream sequence number's turn.
	///     </para>
	///     <para>
	///     Fragment reassembly design (as of the round-3 fix, superseding round 2's abandon-on-new-Begin):
	///     UDP guarantees nothing about arrival order, so a peer's second message on a stream routinely
	///     arrives, and even completes, before an earlier one on the same stream (RFC 4960 6.9 forbids the
	///     SENDER from interleaving two messages of one stream in TSN space, but says nothing about the
	///     NETWORK's delivery order). Multiple incomplete fragment runs per stream therefore coexist as
	///     the normal case, not an edge case: <see cref="_fragments" /> is the single TSN-keyed source of
	///     truth, each entry carrying its own stream and ordering affinity, and completion is checked by
	///     scanning outward from whichever TSN just arrived (see <see cref="TryCompleteAround" />). The
	///     only bound on how much incomplete data can pile up is the byte budget itself; when it is spent,
	///     <see cref="TryRenegeForSpace" /> discards the single oldest run that RFC 4960's reneging rules
	///     allow discarding (see its own remarks) rather than any run touching data already covered by the
	///     cumulative TSN ack, which would be unrecoverable loss.
	///     </para>
	/// </summary>
	internal sealed class SctpReceiveBuffer
	{
		// A bound on how many fragments a single message's backward/forward completion scan (and a
		// single reneged run) can span, purely to cap the cost of a hostile or wildly out-of-spec TSN
		// gap; ordinary traffic never gets close to it (the byte budget already limits how many
		// fragments can be resident at all).
		private const int MaxFragmentsPerMessage = 65536;

		// Caps the out-of-order TSN set independent of the byte budget: a peer sending sparse,
		// non-contiguous TSNs (e.g. every other one) can otherwise grow _gapTsns forever without ever
		// touching _bufferedBytes, since a single-chunk unordered message delivers zero-copy and is
		// never leased. internal (not private) so tests can loop exactly this many times rather than
		// hardcoding the bound.
		internal const int MaxOutOfOrderTsns = 256;

		private readonly uint _budgetBytes;
		private uint _bufferedBytes;
		private uint _cumulativeTsn;
		private bool _initialized;

		// TSNs newer than _cumulativeTsn that have been received but not yet folded into it, kept in
		// TSN order (via SctpTsn.Compare, so the order survives wraparound) for gap-block building and
		// duplicate detection.
		private readonly SortedSet<uint> _gapTsns = new(TsnOrder.Instance);

		// Every fragment (a DATA chunk that is not itself a complete Begin+End message) still waiting
		// on its message to complete, keyed by its own TSN. The single source of truth for reassembly:
		// nothing here is inferred from a per-stream "current run" structure, because arrival order is
		// UDP's, not TSN order's, and a stream can have more than one incomplete message resident at
		// once (see the class remarks).
		private readonly Dictionary<uint, FragmentEntry> _fragments = new();

		// Next stream sequence number an ordered stream is due to deliver.
		private readonly Dictionary<ushort, ushort> _nextOrderedSeq = new();

		// Complete ordered messages buffered because an earlier stream sequence number hasn't arrived
		// yet, keyed by stream then by stream sequence number.
		private readonly Dictionary<ushort, Dictionary<ushort, (byte[] Buffer, int Length, uint Ppid)>> _orderedPending = new();

		private readonly List<uint> _duplicateTsns = new();
		private long _droppedByBudgetCount;
		private long _droppedByGapCapCount;
		private long _renegedFragmentRunCount;

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

		/// <summary>Test visibility only: how many DATA chunks were dropped for arriving when the byte budget was already spent and nothing could be reneged to make room.</summary>
		public long DroppedByBudgetCount => Interlocked.Read(ref _droppedByBudgetCount);

		/// <summary>Test visibility only: how many DATA chunks were dropped because <see cref="MaxOutOfOrderTsns" /> was already spent.</summary>
		public long DroppedByGapCapCount => Interlocked.Read(ref _droppedByGapCapCount);

		/// <summary>Test visibility only: how many incomplete fragment runs were reneged (discarded early, RFC 4960 6.2) to make room under budget pressure.</summary>
		public long RenegedFragmentRunCount => Interlocked.Read(ref _renegedFragmentRunCount);

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

			foreach (FragmentEntry fragment in _fragments.Values) ArrayPool<byte>.Shared.Return(fragment.Buffer);
			_fragments.Clear();
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

			// A chunk that is not exactly the next contiguous TSN needs a new slot in the out-of-order
			// set (mirrors RecordTsnReceived's own contiguity test below). That set is capped
			// independently of the byte budget: an unordered single-chunk message delivers zero-copy and
			// is never leased, so a peer sending only sparse, non-contiguous TSNs could otherwise grow
			// _gapTsns forever without ever touching _bufferedBytes. Dropped and counted the same way
			// budget-exceeding DATA already is, before any lease or delivery decision is made.
			bool isNextContiguous = tsn == unchecked(_cumulativeTsn + 1);
			if (!isNextContiguous && _gapTsns.Count >= MaxOutOfOrderTsns)
			{
				Interlocked.Increment(ref _droppedByGapCapCount);
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

			if (_bufferedBytes + (uint) payload.Length > _budgetBytes && !TryRenegeForSpace((uint) payload.Length))
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
				_fragments[tsn] = new FragmentEntry(header.StreamId, header.Unordered, header.Begin, header.End, header.Ppid, header.StreamSeq, leased, payload.Length, Environment.TickCount64);
				TryCompleteAround(tsn);
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

			// TSN dedup only rejects a TSN already seen; it does nothing to stop two DIFFERENT, fresh
			// TSNs from carrying the same (streamId, seq) - a peer bug or a hostile peer trying to drain
			// a_rwnd can reach this. The second overwrites the first: its lease is returned AND its
			// bytes are backed out of _bufferedBytes, matching the increment Receive already applied for
			// it, or CurrentArwnd would drain toward zero forever on every such overwrite.
			if (pending.TryGetValue(seq, out (byte[] Buffer, int Length, uint Ppid) existing))
			{
				ArrayPool<byte>.Shared.Return(existing.Buffer);
				_bufferedBytes -= (uint) existing.Length;
			}

			pending[seq] = (buffer, length, ppid);
		}

		/// <summary>
		///     Checked on every fragment arrival, around the TSN that just arrived: scans backward to a
		///     Begin and forward to an End (bounded by <see cref="MaxFragmentsPerMessage" />), requiring
		///     every TSN in between to actually be present, on this same stream, with the same unordered
		///     affinity, and never a stray Begin or End inside the span. Any hole or mismatch means "still
		///     incomplete" and touches nothing - this is what makes it safe for more than one message on
		///     the same stream, and messages on different streams, to sit here incomplete at once: a
		///     foreign chunk sitting in the middle of the scan is never mistaken for a piece of this
		///     message (round 1's cross-stream fix), and completion never fires from list bookkeeping
		///     alone without this walk confirming the window itself is intact (round 2's regression, since
		///     removed along with the abandon-on-new-Begin rule it depended on: see the class remarks for
		///     why that rule was unsafe).
		/// </summary>
		private void TryCompleteAround(uint arrivedTsn)
		{
			if (!_fragments.TryGetValue(arrivedTsn, out FragmentEntry arrived)) return;

			ushort streamId = arrived.StreamId;
			bool unordered = arrived.Unordered;

			uint begin = arrivedTsn;
			if (!arrived.Begin)
			{
				int steps = 0;
				while (true)
				{
					uint candidate = unchecked(begin - 1);
					if (!_fragments.TryGetValue(candidate, out FragmentEntry e) || e.StreamId != streamId || e.Unordered != unordered) return; // hole or a foreign/mismatched chunk: incomplete
					if (e.End) return; // an End sitting before us with no Begin between: inconsistent, fail safe
					begin = candidate;
					if (e.Begin) break;
					if (++steps > MaxFragmentsPerMessage) return;
				}
			}

			uint end = arrivedTsn;
			if (!arrived.End)
			{
				int steps = 0;
				while (true)
				{
					uint candidate = unchecked(end + 1);
					if (!_fragments.TryGetValue(candidate, out FragmentEntry e) || e.StreamId != streamId || e.Unordered != unordered) return; // hole or a foreign/mismatched chunk: incomplete
					if (e.Begin) return; // a Begin sitting after us with no End between: inconsistent, fail safe
					end = candidate;
					if (e.End) break;
					if (++steps > MaxFragmentsPerMessage) return;
				}
			}

			int pieceCount = SctpTsn.Compare(end, begin) + 1;
			if (pieceCount <= 0 || pieceCount > MaxFragmentsPerMessage) return; // should be unreachable given the bounded walks above; fail safe regardless

			FragmentEntry beginEntry = _fragments[begin];
			uint ppid = beginEntry.Ppid;
			ushort streamSeq = beginEntry.StreamSeq;

			int totalLength = 0;
			uint scan = begin;
			for (int i = 0; i < pieceCount; i++)
			{
				totalLength += _fragments[scan].Length;
				scan = unchecked(scan + 1);
			}

			byte[] combined = ArrayPool<byte>.Shared.Rent(totalLength);
			int offset = 0;
			scan = begin;
			for (int i = 0; i < pieceCount; i++)
			{
				FragmentEntry piece = _fragments[scan];
				// A single memcpy per fragment run. If fragmentation ever turns out to be a real cost in
				// practice, the alternative is delivering the message as a ReadOnlySequence<byte> chained
				// over the individual fragment buffers instead of concatenating here, trading this one
				// copy for segment-aware parsing in consumers.
				Array.Copy(piece.Buffer, 0, combined, offset, piece.Length);
				offset += piece.Length;
				ArrayPool<byte>.Shared.Return(piece.Buffer);
				_fragments.Remove(scan);
				_bufferedBytes -= (uint) piece.Length;
				scan = unchecked(scan + 1);
			}

			_bufferedBytes += (uint) totalLength;

			if (unordered)
			{
				Deliveries.Add(new LeasedDelivery(streamId, ppid, combined, totalLength));
				_bufferedBytes -= (uint) totalLength;
				return;
			}

			switch (ClassifyOrdered(streamId, streamSeq))
			{
				case OrderedDisposition.DueNow:
					Deliveries.Add(new LeasedDelivery(streamId, ppid, combined, totalLength));
					_bufferedBytes -= (uint) totalLength;
					AdvanceOrderedSeqAndDrain(streamId, streamSeq);
					break;

				case OrderedDisposition.Stale:
					ArrayPool<byte>.Shared.Return(combined);
					_bufferedBytes -= (uint) totalLength;
					break;

				default: // WaitForTurn
					EnqueueOrderedPending(streamId, streamSeq, ppid, combined, totalLength);
					break;
			}
		}

		/// <summary>
		///     Called only when a new chunk needs <paramref name="neededBytes" /> more room than the
		///     budget currently has free. Reneges (RFC 4960 6.2: a receiver MAY discard data it has only
		///     gap-acked, never data covered by the cumulative TSN ack, and MUST stop reporting a reneged
		///     TSN's gap so the sender knows to retransmit it) the single oldest eligible fragment run,
		///     repeating against the next-oldest if one run's worth still isn't enough, until either
		///     enough space exists or nothing eligible is left to discard.
		/// </summary>
		private bool TryRenegeForSpace(uint neededBytes)
		{
			while (_bufferedBytes + neededBytes > _budgetBytes)
			{
				if (!RenegeOldestEligibleRun()) return false;
			}

			return true;
		}

		/// <summary>
		///     Finds every maximal contiguous run of fragments sharing (streamId, unordered) - the same
		///     affinity <see cref="TryCompleteAround" /> matches on, so a "run" here means exactly what
		///     could eventually complete together - discards the one with the oldest arrival tick among
		///     those where every fragment's TSN is still strictly newer than the cumulative ack point
		///     (a run with any fragment at or below it is never a candidate: that TSN's arrival is already
		///     committed via the cumulative ack, which is not revocable, so discarding it would be
		///     unrecoverable data loss rather than a legal renege), and returns true. Returns false if no
		///     eligible run exists.
		/// </summary>
		private bool RenegeOldestEligibleRun()
		{
			var groups = new Dictionary<(ushort StreamId, bool Unordered), List<uint>>();
			foreach (KeyValuePair<uint, FragmentEntry> kv in _fragments)
			{
				var key = (kv.Value.StreamId, kv.Value.Unordered);
				if (!groups.TryGetValue(key, out List<uint> list))
				{
					list = new List<uint>();
					groups[key] = list;
				}

				list.Add(kv.Key);
			}

			List<uint> bestRun = null;
			long bestOldestTick = long.MaxValue;

			foreach (List<uint> tsns in groups.Values)
			{
				tsns.Sort(SctpTsn.Compare);

				int i = 0;
				while (i < tsns.Count)
				{
					int j = i;
					while (j + 1 < tsns.Count && tsns[j + 1] == unchecked(tsns[j] + 1)) j++;

					bool eligible = true;
					long oldestTick = long.MaxValue;
					for (int k = i; k <= j; k++)
					{
						FragmentEntry entry = _fragments[tsns[k]];
						if (!SctpTsn.IsNewer(tsns[k], _cumulativeTsn)) eligible = false;
						if (entry.ArrivalTick < oldestTick) oldestTick = entry.ArrivalTick;
					}

					if (eligible && oldestTick < bestOldestTick)
					{
						bestOldestTick = oldestTick;
						bestRun = tsns.GetRange(i, j - i + 1);
					}

					i = j + 1;
				}
			}

			if (bestRun == null) return false;

			foreach (uint tsn in bestRun)
			{
				if (_fragments.Remove(tsn, out FragmentEntry entry))
				{
					ArrayPool<byte>.Shared.Return(entry.Buffer);
					_bufferedBytes -= (uint) entry.Length;
				}

				// Stop reporting this TSN as a gap: reneging is only legal because the sender is told to
				// retransmit it again, and it must also pass Receive's duplicate check on that retransmit.
				_gapTsns.Remove(tsn);
			}

			Interlocked.Increment(ref _renegedFragmentRunCount);
			return true;
		}

		private enum OrderedDisposition
		{
			DueNow,
			WaitForTurn,
			Stale
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

		/// <summary>
		///     One stored DATA fragment: everything <see cref="TryCompleteAround" /> and
		///     <see cref="RenegeOldestEligibleRun" /> need without consulting anything else. Carries its
		///     own stream and ordering affinity rather than relying on a shared per-stream structure,
		///     since more than one message per stream can be incomplete here at once.
		/// </summary>
		private readonly struct FragmentEntry
		{
			public readonly ushort StreamId;
			public readonly bool Unordered;
			public readonly bool Begin;
			public readonly bool End;
			public readonly uint Ppid;
			public readonly ushort StreamSeq;
			public readonly byte[] Buffer;
			public readonly int Length;
			public readonly long ArrivalTick;

			public FragmentEntry(ushort streamId, bool unordered, bool begin, bool end, uint ppid, ushort streamSeq, byte[] buffer, int length, long arrivalTick)
			{
				StreamId = streamId;
				Unordered = unordered;
				Begin = begin;
				End = end;
				Ppid = ppid;
				StreamSeq = streamSeq;
				Buffer = buffer;
				Length = length;
				ArrivalTick = arrivalTick;
			}
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