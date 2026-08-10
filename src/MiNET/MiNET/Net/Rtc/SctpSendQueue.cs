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
	///     The send half of one SCTP association: per-chunk retransmission state, RTO/RTT tracking
	///     (RFC 6298), slow-start-shaped congestion control, and RFC 3758 partial reliability
	///     (abandon-and-forward). Owned exclusively by one <see cref="SctpAssociation" /> and driven
	///     only under its <c>_gate</c>, matching <see cref="SctpReceiveBuffer" />'s own single-owner
	///     assumption, so nothing here takes a lock of its own.
	///     <para>
	///     Chunks are kept in one singly-linked, TSN-ordered list from <see cref="Enqueue" /> (append
	///     at the tail; TSNs are assigned by the caller in increasing order, so the list is always
	///     already TSN-sorted) through to removal, which only ever happens at the head, either because
	///     the peer's cumulative TSN ack covered it (<see cref="OnSackReceived" />) or because it was
	///     covered by our own advancing FORWARD-TSN cumulative once the peer catches up. A node is
	///     never removed from the middle: an abandoned chunk (<see cref="PendingChunk.Abandoned" />)
	///     frees its leased payload immediately but stays resident as a bare marker so
	///     <see cref="TryComputeForwardTsnAdvance" /> can still see it while walking forward from the
	///     last point already advertised to the peer.
	///     </para>
	///     <para>
	///     Both the <see cref="PendingChunk" /> nodes and their leased payload buffers are pooled
	///     (<see cref="ArrayPool{T}.Shared" /> for the buffer, an intrusive free list here for the node
	///     itself), so steady-state send/ack traffic allocates nothing on the heap - the same shape
	///     <see cref="SctpReceiveBuffer.PooledSegment" /> already uses on the receive side.
	///     </para>
	/// </summary>
	internal sealed class SctpSendQueue
	{
		// RFC 6298 shape, with the constants the task brief calls for verbatim: RtoMin here is 200ms,
		// not RFC 4960's 1s, which is WebRTC practice (browsers use the same lower floor to keep data
		// channels responsive on fast local paths) rather than the classic TCP-oriented RFC value.
		private const long RtoInitialMillis = 1000;
		private const long RtoMinMillis = 200;
		private const long RtoMaxMillis = 10000;

		// This stack's own outbound packet size (SctpPacket.MaxSize), standing in for path MTU: slow
		// start's initial/growth increment and the SACK-driven window bundling both assume it.
		private const int Mtu = SctpPacket.MaxSize;

		// Congestion window cap the task brief specifies (128 KB); full RFC 4960 congestion avoidance
		// (a ssthresh phase past slow start) is explicitly deferred by the plan, so cwnd only ever grows
		// by one MTU per acking SACK, capped here, and halves on a retransmit timeout.
		private const uint CwndCap = 131072;

		private readonly uint _queueBudgetBytes;
		private uint _queuedBytes;

		private PendingChunk _head;
		private PendingChunk _tail;
		private PendingChunk _freeList;

		// Our own send-side TSN bookkeeping: the highest TSN the peer has told us (via a SACK's
		// cumulative ack) it has fully received, and the highest TSN we have told the peer (via our own
		// FORWARD-TSN) to skip past regardless of whether it ever arrives. The two converge once the
		// peer's own AdvanceCumulative (Task 4's receive buffer) processes that FORWARD-TSN and reports
		// the new point back in its next SACK.
		private uint _cumulativeTsnAck;
		private uint _forwardTsnAdvertised;

		private uint _cwnd;
		private long _rtoMillis;
		private long _srttMillis = -1; // -1: no RTT sample taken yet (RFC 6298 2.2's "first measurement")
		private long _rttvarMillis;

		// RFC 4960 7.2.4 fast retransmit: three SACKs in a row reporting a gap (data received beyond a
		// hole) while the cumulative ack point does not move are treated as a loss signal for the chunk
		// immediately after that point, without waiting for T3-rtx.
		private int _duplicateCumulativeReports;

		private bool _timerArmed;
		private long _timerDeadlineMillis;

		private long _retransmitCount;
		private long _abandonedCount;
		private long _fastRetransmitCount;
		private long _timeoutCount;

		public SctpSendQueue(uint queueBudgetBytes)
		{
			_queueBudgetBytes = queueBudgetBytes;
		}

		public uint Cwnd => _cwnd;
		public long RtoMillis => _rtoMillis;
		public uint QueuedBytes => _queuedBytes;

		/// <summary>Test visibility only: every chunk that left the wire more than once, whether from T3-rtx or fast retransmit.</summary>
		public long RetransmitCount => Interlocked.Read(ref _retransmitCount);

		/// <summary>Test visibility only: chunks given up on (RFC 3758) after exceeding their own <see cref="PendingChunk.MaxRetransmits" />.</summary>
		public long AbandonedCount => Interlocked.Read(ref _abandonedCount);

		/// <summary>Test visibility only: how many times three duplicate gap-carrying SACKs triggered an early retransmit ahead of T3-rtx.</summary>
		public long FastRetransmitCount => Interlocked.Read(ref _fastRetransmitCount);

		/// <summary>Test visibility only: how many times T3-rtx actually fired (RTO elapsed with data still outstanding).</summary>
		public long TimeoutCount => Interlocked.Read(ref _timeoutCount);

		/// <summary>(Re)arms the queue for a fresh association: releases anything still resident from a previous lifetime, then seeds TSN/cwnd/RTO state for the local Initial TSN just negotiated.</summary>
		public void Reset(uint localInitialTsn)
		{
			PendingChunk node = _head;
			while (node != null)
			{
				PendingChunk next = node.Next;
				ReturnBuffer(node);
				ReturnNodeToFreeList(node);
				node = next;
			}

			_head = null;
			_tail = null;
			_queuedBytes = 0;

			_cumulativeTsnAck = unchecked(localInitialTsn - 1);
			_forwardTsnAdvertised = _cumulativeTsnAck;

			_cwnd = 4 * (uint) Mtu;
			_rtoMillis = RtoInitialMillis;
			_srttMillis = -1;
			_rttvarMillis = 0;
			_duplicateCumulativeReports = 0;

			_timerArmed = false;
			_timerDeadlineMillis = 0;
		}

		/// <summary>Whether <paramref name="totalBytes" /> more resident payload would still fit under the send-queue budget (queued and in-flight, not yet cumulatively acked, together).</summary>
		public bool HasRoomFor(uint totalBytes) => (ulong) _queuedBytes + totalBytes <= _queueBudgetBytes;

		/// <summary>
		///     Leases a copy of <paramref name="payload" /> (the caller's span is transient; this copy is
		///     the one the zero-alloc contract accepts, per the task brief) and appends a new chunk at the
		///     tail, never yet transmitted.
		/// </summary>
		public void Enqueue(uint tsn, ushort streamId, ushort streamSeq, uint ppid, bool unordered, bool begin, bool end, ReadOnlySpan<byte> payload, int maxRetransmits)
		{
			PendingChunk node = RentNode();
			node.Tsn = tsn;
			node.StreamId = streamId;
			node.StreamSeq = streamSeq;
			node.Ppid = ppid;
			node.Unordered = unordered;
			node.Begin = begin;
			node.End = end;
			node.Buffer = payload.Length == 0 ? Array.Empty<byte>() : ArrayPool<byte>.Shared.Rent(payload.Length);
			payload.CopyTo(node.Buffer);
			node.Length = payload.Length;
			node.MaxRetransmits = maxRetransmits;
			node.RetransmitCount = 0;
			node.SentAtTicks = 0;
			node.PendingRetransmit = false;
			node.GapAcked = false;
			node.Abandoned = false;
			node.Next = null;

			if (_tail == null) _head = node;
			else _tail.Next = node;
			_tail = node;

			_queuedBytes += (uint) payload.Length;
		}

		/// <summary>How many bytes the current send window (<c>min(peer a_rwnd, cwnd)</c>) still has room for beyond what is already in flight.</summary>
		public uint AvailableWindowBytes(uint peerArwnd)
		{
			uint windowCap = Math.Min(peerArwnd, _cwnd);
			uint inFlight = InFlightBytes();
			return inFlight >= windowCap ? 0 : windowCap - inFlight;
		}

		private uint InFlightBytes()
		{
			uint sum = 0;
			for (PendingChunk n = _head; n != null; n = n.Next)
			{
				if (n.SentAtTicks != 0 && !n.Abandoned && !n.PendingRetransmit) sum += (uint) n.Length;
			}

			return sum;
		}

		/// <summary>
		///     The next chunk due to go out - either never transmitted, or explicitly marked for
		///     retransmission (T3-rtx or fast retransmit) - that still fits <paramref name="windowBudget" />.
		///     Already-in-flight chunks awaiting their first ack are skipped over (sending a later, newer
		///     chunk while an earlier one is still outstanding is ordinary SCTP pipelining), but the walk
		///     stops, rather than skipping ahead to a smaller one, the moment a due chunk does not fit:
		///     first transmissions stay in TSN order.
		/// </summary>
		public PendingChunk PeekReadyToSend(uint windowBudget)
		{
			for (PendingChunk n = _head; n != null; n = n.Next)
			{
				if (n.Abandoned) continue;
				bool due = n.SentAtTicks == 0 || n.PendingRetransmit;
				if (!due) continue;
				if (n.Length > windowBudget) return null;
				return n;
			}

			return null;
		}

		/// <summary>
		///     Records that <paramref name="chunk" /> just went out on the wire. A first transmission arms
		///     T3-rtx if it was not already running (RFC 6298 5.1); a retransmission's own count and RTO
		///     backoff were already applied by whatever marked it <see cref="PendingChunk.PendingRetransmit" />
		///     (<see cref="HandleTimeout" /> or the fast-retransmit branch of <see cref="OnSackReceived" />),
		///     not here, so this never double-counts.
		/// </summary>
		public void MarkTransmitted(PendingChunk chunk, long nowMillis)
		{
			chunk.SentAtTicks = nowMillis;
			chunk.PendingRetransmit = false;

			if (!_timerArmed)
			{
				_timerArmed = true;
				_timerDeadlineMillis = nowMillis + _rtoMillis;
			}
		}

		public bool IsTimerExpired(long nowMillis) => _timerArmed && nowMillis >= _timerDeadlineMillis;

		/// <summary>
		///     T3-rtx firing (RFC 4960 6.3.3): backs off RTO, halves cwnd (floored at one MTU), and marks
		///     every still-outstanding, not-yet-abandoned chunk for retransmission - or abandons it outright
		///     if this retransmission would exceed its own <see cref="PendingChunk.MaxRetransmits" />. SCTP
		///     has one timer per association (there is only ever one destination address here), not one per
		///     chunk, so a single expiry covers everything currently in flight.
		/// </summary>
		public void HandleTimeout(long nowMillis)
		{
			Interlocked.Increment(ref _timeoutCount);

			_rtoMillis = Math.Clamp(_rtoMillis * 2, RtoMinMillis, RtoMaxMillis);
			_cwnd = Math.Max(_cwnd / 2, (uint) Mtu);
			_duplicateCumulativeReports = 0;

			bool anyOutstanding = false;
			for (PendingChunk n = _head; n != null; n = n.Next)
			{
				if (n.Abandoned) continue;
				if (n.SentAtTicks == 0) continue; // never sent yet: nothing to time out
				MarkForRetransmitOrAbandon(n);
				if (!n.Abandoned) anyOutstanding = true;
			}

			if (anyOutstanding)
			{
				_timerArmed = true;
				_timerDeadlineMillis = nowMillis + _rtoMillis;
			}
			else
			{
				_timerArmed = false;
			}
		}

		/// <summary>
		///     Applies one inbound SACK: frees every chunk the cumulative ack now covers (taking one RTT
		///     sample from the first eligible chunk per Karn's rule - never from one that was ever
		///     retransmitted), grows cwnd by one MTU when the cumulative ack actually advanced, marks
		///     gap-acked chunks without freeing them (RFC 4960's renege rule), and counts three
		///     cumulative-stuck, gap-carrying SACKs in a row as a fast-retransmit signal for the chunk right
		///     after the ack point. Restarts (or disarms, if nothing remains outstanding) T3-rtx whenever
		///     the cumulative ack advances, per RFC 6298 5.3.
		/// </summary>
		public bool OnSackReceived(uint sackCumulativeTsnAck, ReadOnlySpan<SackChunk.GapBlock> gapBlocks, long nowMillis)
		{
			bool advanced = SctpTsn.IsNewer(sackCumulativeTsnAck, _cumulativeTsnAck);

			if (advanced)
			{
				bool rttSampleTaken = false;
				long rttSampleMillis = 0;

				while (_head != null && !SctpTsn.IsNewer(_head.Tsn, sackCumulativeTsnAck))
				{
					PendingChunk node = _head;

					if (!rttSampleTaken && node.RetransmitCount == 0 && node.SentAtTicks != 0 && !node.Abandoned)
					{
						rttSampleTaken = true;
						rttSampleMillis = nowMillis - node.SentAtTicks;
					}

					_head = node.Next;
					if (_head == null) _tail = null;

					_queuedBytes -= (uint) node.Length;
					ReturnBuffer(node);
					ReturnNodeToFreeList(node);
				}

				_cumulativeTsnAck = sackCumulativeTsnAck;
				if (SctpTsn.IsNewer(_cumulativeTsnAck, _forwardTsnAdvertised)) _forwardTsnAdvertised = _cumulativeTsnAck;

				_cwnd = Math.Min(_cwnd + (uint) Mtu, CwndCap);
				_duplicateCumulativeReports = 0;

				if (rttSampleTaken) ApplyRttSample(rttSampleMillis);

				if (_head != null)
				{
					_timerArmed = true;
					_timerDeadlineMillis = nowMillis + _rtoMillis;
				}
				else
				{
					_timerArmed = false;
				}
			}
			else if (gapBlocks.Length > 0)
			{
				_duplicateCumulativeReports++;
				if (_duplicateCumulativeReports >= 3)
				{
					_duplicateCumulativeReports = 0;
					Interlocked.Increment(ref _fastRetransmitCount);
					if (_head != null && !_head.Abandoned) MarkForRetransmitOrAbandon(_head);
				}
			}

			// RFC 4960 6.2 renege rule: gap-acked data is marked, never freed here - only cumulative
			// catching up (above) actually returns its lease. Chunks are stored TSN-ascending, so each
			// block's scan can stop as soon as it passes the block's end.
			foreach (SackChunk.GapBlock block in gapBlocks)
			{
				uint start = unchecked(sackCumulativeTsnAck + block.Start);
				uint end = unchecked(sackCumulativeTsnAck + block.End);
				for (PendingChunk n = _head; n != null; n = n.Next)
				{
					if (SctpTsn.IsNewer(n.Tsn, end)) break;
					if (!SctpTsn.IsNewer(start, n.Tsn)) n.GapAcked = true;
				}
			}

			return advanced;
		}

		private void ApplyRttSample(long rttSampleMillis)
		{
			long r = Math.Max(0, rttSampleMillis);

			if (_srttMillis < 0)
			{
				_srttMillis = r;
				_rttvarMillis = r / 2;
			}
			else
			{
				long diff = Math.Abs(_srttMillis - r);
				_rttvarMillis = (long) (0.75 * _rttvarMillis + 0.25 * diff);
				_srttMillis = (long) (0.875 * _srttMillis + 0.125 * r);
			}

			_rtoMillis = Math.Clamp(_srttMillis + 4 * _rttvarMillis, RtoMinMillis, RtoMaxMillis);
		}

		/// <summary>Shared by <see cref="HandleTimeout" /> and the fast-retransmit branch of <see cref="OnSackReceived" />: counts the retransmit, then either schedules it or, past its own retransmit budget, abandons it for good (RFC 3758).</summary>
		private void MarkForRetransmitOrAbandon(PendingChunk n)
		{
			n.RetransmitCount++;
			Interlocked.Increment(ref _retransmitCount);

			if (n.MaxRetransmits >= 0 && n.RetransmitCount > n.MaxRetransmits) AbandonChunk(n);
			else n.PendingRetransmit = true;
		}

		private void AbandonChunk(PendingChunk n)
		{
			if (n.Abandoned) return;

			n.Abandoned = true;
			n.PendingRetransmit = false;
			Interlocked.Increment(ref _abandonedCount);

			_queuedBytes -= (uint) n.Length;
			ReturnBuffer(n);
			n.Length = 0;
		}

		/// <summary>
		///     Walks forward from the last point already advertised to the peer while every next chunk in
		///     TSN order is abandoned, collecting (streamId, streamSeq) for each ordered one that was also
		///     the End of its message (RFC 3758's advisory pairs, so the peer's ordered delivery on that
		///     stream does not stall waiting for a message that will never complete). Returns false (leaving
		///     <paramref name="pairs" /> cleared and <paramref name="newTarget" /> unchanged) when nothing
		///     new can be advertised - either nothing is abandoned yet, or the very next chunk is still
		///     genuinely outstanding.
		/// </summary>
		public bool TryComputeForwardTsnAdvance(List<(ushort StreamId, ushort StreamSeq)> pairs, out uint newTarget)
		{
			pairs?.Clear();
			newTarget = _forwardTsnAdvertised;

			bool moved = false;
			uint expected = unchecked(_forwardTsnAdvertised + 1);

			for (PendingChunk n = _head; n != null && n.Tsn == expected && n.Abandoned; n = n.Next)
			{
				newTarget = n.Tsn;
				moved = true;
				if (!n.Unordered && n.End) pairs?.Add((n.StreamId, n.StreamSeq));
				expected = unchecked(expected + 1);
			}

			return moved;
		}

		/// <summary>Records that a FORWARD-TSN advertising up to <paramref name="target" /> has actually gone out; the queue no longer needs to re-offer it on the next call.</summary>
		public void MarkForwardTsnAdvertised(uint target)
		{
			_forwardTsnAdvertised = target;
		}

		private static void ReturnBuffer(PendingChunk n)
		{
			if (n.Buffer != null && n.Buffer.Length > 0) ArrayPool<byte>.Shared.Return(n.Buffer);
			n.Buffer = null;
		}

		private PendingChunk RentNode()
		{
			if (_freeList == null) return new PendingChunk();

			PendingChunk n = _freeList;
			_freeList = n.FreeListNext;
			n.FreeListNext = null;
			return n;
		}

		private void ReturnNodeToFreeList(PendingChunk n)
		{
			n.Next = null;
			n.FreeListNext = _freeList;
			_freeList = n;
		}

		/// <summary>One outbound DATA chunk's full retransmission state, resident from <see cref="Enqueue" /> until its TSN is covered by the peer's cumulative ack.</summary>
		internal sealed class PendingChunk
		{
			public uint Tsn;
			public ushort StreamId;
			public ushort StreamSeq;
			public uint Ppid;
			public bool Unordered;
			public bool Begin;
			public bool End;

			/// <summary>Leased from <see cref="ArrayPool{T}.Shared" />; returned on ack, on abandon, or when the queue is <see cref="Reset" />.</summary>
			public byte[] Buffer;

			public int Length;

			/// <summary>Negative means fully reliable (never abandoned); the task brief's <c>maxRetransmits &lt; 0</c> contract.</summary>
			public int MaxRetransmits;

			public int RetransmitCount;

			/// <summary>0 means never yet transmitted.</summary>
			public long SentAtTicks;

			/// <summary>Set by <see cref="HandleTimeout" /> or a fast retransmit; cleared once <see cref="MarkTransmitted" /> actually sends it again.</summary>
			public bool PendingRetransmit;

			/// <summary>RFC 4960 6.2 renege rule: marked by a SACK gap block, never freed by it - only an advancing cumulative ack actually returns the lease.</summary>
			public bool GapAcked;

			/// <summary>RFC 3758: given up on past its own <see cref="MaxRetransmits" />. <see cref="Buffer" /> is already returned; the node stays resident only as a marker for <see cref="TryComputeForwardTsnAdvance" />.</summary>
			public bool Abandoned;

			internal PendingChunk Next;
			internal PendingChunk FreeListNext;
		}
	}
}