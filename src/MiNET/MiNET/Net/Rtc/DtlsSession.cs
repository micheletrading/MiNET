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
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using log4net;
using Org.BouncyCastle.Tls;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     One DTLS 1.2 association securing a WebRTC data channel, pinned to a peer fingerprint
	///     exchanged out of band over SDP rather than a certificate chain. There is no SRTP key
	///     export: this class blocks a thread only for the handshake (<see cref="DoHandshakeAsync" />,
	///     via <see cref="Task.Run" />) and is entirely receive-driven afterwards, with no dedicated
	///     receive thread for the life of the session. <see cref="FeedDatagram" /> both queues the raw
	///     datagram for BouncyCastle's <see cref="DatagramTransport" /> and, once the handshake is
	///     done, immediately pumps it through <see cref="DtlsTransport.Receive(Span{byte}, int)" /> and
	///     drains any records it bundled via <see cref="DtlsTransport.ReceivePending(Span{byte}, DtlsRecordCallback)" />,
	///     all inline on the caller's thread. No background thread ever exists after the handshake.
	///     Invariant: <see cref="_gate" /> serializes every access to <see cref="_dtlsTransport" />,
	///     <see cref="_receiveScratch" />, and <see cref="_directFeedBuffer" /> between
	///     <see cref="FeedDatagram" />'s drain section and <see cref="Dispose" />, so a concurrent
	///     teardown from another thread can never return either buffer to the pool (or close the
	///     transport) while a drain still holds a span over one. A <c>lock</c> is a .NET Monitor, which
	///     is reentrant on the thread that already owns it, so this alone does not stop a subscriber
	///     calling <see cref="Dispose" /> synchronously from inside <see cref="OnDecrypted" /> while
	///     <see cref="FeedDatagram" /> is still on the stack holding the gate: <see cref="_draining" />
	///     tracks that case (touched only while holding <see cref="_gate" />, so it needs no
	///     synchronization of its own) so a reentrant <see cref="Dispose" /> defers both buffers'
	///     return to <see cref="FeedDatagram" />'s own unwind instead of freeing them out from under
	///     the drain loop still running further up the same call stack.
	///     Threading: <see cref="FeedDatagram" /> itself is safe to call concurrently from multiple
	///     threads (round 5), even though nothing in today's topology actually does so, a single
	///     receive loop per <see cref="UdpMux" /> feeding one datagram at a time. Every datagram passed
	///     to it is either drained inline or queued onto <see cref="_inbound" />, on exactly one of
	///     those two paths, and is never silently dropped or corrupted by an overlapping call: the
	///     direct-feed fast path's guard-check-through-drain sequence runs entirely under
	///     <see cref="_gate" />, and the lease-and-<see cref="_inbound" /> path was already safe under
	///     concurrency on its own (a fresh per-call <see cref="ArrayPool{T}" /> lease, and
	///     <see cref="System.Threading.Channels.Channel{T}" /> is internally thread-safe for concurrent
	///     writers).
	/// </summary>
	public sealed class DtlsSession : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DtlsSession));

		// 1500 (typical path MTU) - 20 (IPv4) - 8 (UDP) = 1472. The scratch buffer is sized above
		// that to accommodate BouncyCastle bundling more than one plaintext record per receive.
		private const int WireLimit = 1472;
		private const int ScratchBufferSize = 4096;

		public delegate void DecryptedHandler(ReadOnlySpan<byte> payload);

		/// <summary>
		///     Round-4 Finding A: takes the outgoing datagram as a span, not a <see cref="ReadOnlyMemory{T}" />,
		///     since the whole call chain from BouncyCastle's <see cref="DtlsTransport.Send(ReadOnlySpan{byte})" />
		///     down to <see cref="UdpMux.Send" /> is synchronous. A <see cref="ReadOnlyMemory{T}" />
		///     parameter existed only so <see cref="SendToWire" /> could hand it a heap reference after
		///     the call returned; nothing here ever does that, so the intermediate ArrayPool lease that
		///     type forced (rent, copy the span into it, hand out the memory, return the lease) was pure
		///     overhead: one full copy and one pool round trip per outgoing datagram, for no reason.
		/// </summary>
		public delegate void WireSender(ReadOnlySpan<byte> datagram);

		public event DecryptedHandler OnDecrypted;

		private readonly RtcCertificate _localCertificate;
		private readonly string _expectedRemoteFingerprint;
		private readonly bool _isServer;
		private readonly WireSender _sendToWire;
		private readonly Channel<(byte[] Leased, int Length)> _inbound = Channel.CreateUnbounded<(byte[] Leased, int Length)>();
		private readonly byte[] _receiveScratch = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);
		private readonly DatagramTransportAdapter _transportAdapter;
		private readonly object _gate = new object();

		// Round-4 Finding B: a persistent (allocated once, never per-call), reused staging buffer for
		// FeedDatagram's no-backlog fast path. See FeedDatagram and TryReadNow for the full mechanism.
		private readonly byte[] _directFeedBuffer = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);
		private int _directFeedLength = -1;

		private DtlsTransport _dtlsTransport;
		private volatile bool _handshakeDone;
		private volatile bool _closed;
		private int _disposed;

		// Both touched only while holding _gate (see the class doc comment's Invariant paragraph),
		// so neither needs volatile or Interlocked: _gate's acquire/release already provides the
		// necessary visibility across the two threads that can ever reach either field.
		private bool _draining;
		private bool _deferredScratchReturn;

		public DtlsSession(RtcCertificate localCertificate, string expectedRemoteFingerprint, bool isServer, WireSender sendToWire)
		{
			_localCertificate = localCertificate;
			_expectedRemoteFingerprint = expectedRemoteFingerprint;
			_isServer = isServer;
			_sendToWire = sendToWire;
			_transportAdapter = new DatagramTransportAdapter(this);
		}

		/// <summary>
		///     Feeds one raw datagram demuxed as DTLS by <see cref="IceSession.OnDtlsDatagram" />.
		///     Before the handshake completes this only queues the datagram for the blocking
		///     <see cref="Task.Run" /> handshake thread to pick up. Afterwards it also drains it
		///     immediately, inline, on the calling thread, under <see cref="_gate" /> so a concurrent
		///     <see cref="Dispose" /> can never free <see cref="_receiveScratch" /> out from under it
		///     (Finding 3: the disposed check alone is check-then-act, not a real exclusion). A
		///     <see cref="DtlsSessionClosedException" /> raised by the drain (Dispose or a cancelled
		///     handshake closed the session while this call was in flight) is swallowed here: it is an
		///     expected race during teardown, not a caller-visible failure. <see cref="_draining" />
		///     brackets the call so a <em>reentrant</em> <see cref="Dispose" />, called synchronously
		///     from inside an <see cref="OnDecrypted" /> subscriber, defers returning
		///     <see cref="_receiveScratch" /> to the pool until this method's own <c>finally</c> below,
		///     after the drain loop (further up this same call stack) has stopped touching it (round-2
		///     Item 1: the lock alone is reentrant on this thread and does not by itself prevent that).
		///     <para>
		///     Round-4 Finding B: the steady-state case is a datagram that is about to be consumed
		///     inline, on this exact call, by <see cref="DrainPending" />'s very first
		///     <see cref="DtlsTransport.Receive(Span{byte}, int)" /> a few instructions from now, with
		///     nothing else backlogged. Leasing it from <see cref="ArrayPool{T}" /> and round-tripping it
		///     through <see cref="_inbound" /> just to read it straight back out again bought nothing in
		///     that case. When the handshake is done, no drain from this same thread is already running
		///     (<see cref="_draining" />), the channel is empty, and the datagram fits
		///     <see cref="_directFeedBuffer" />, this copies straight into that persistent buffer
		///     (allocated once, at construction, never per-call) instead and lets
		///     <see cref="TryReadNow" /> pick it up first. <see cref="_directFeedLength" /> is the only
		///     state that says the slot is live; it is cleared by <see cref="TryReadNow" /> the instant
		///     it copies the bytes into BouncyCastle's own buffer (synchronously, nested inside this same
		///     call, well before this method returns: traced through <see cref="DrainPending" />'s single
		///     <c>Receive</c> call down to <see cref="DatagramTransportAdapter.Receive(Span{byte}, int)" />),
		///     and <see cref="DrainLocked" />'s own <c>finally</c> clears it unconditionally besides, so
		///     the slot can never outlive this call on any path, including one that never reaches
		///     <see cref="TryReadNow" /> at all. Anything that does not fit this fast path (mid-handshake,
		///     an existing backlog, a reentrant drain, or a datagram too large for the staging buffer)
		///     falls through to the unchanged lease-and-<see cref="_inbound" /> path below.
		///     </para>
		///     <para>
		///     Round-5 (concurrent <see cref="FeedDatagram" /> calls): the guard, the copy into
		///     <see cref="_directFeedBuffer" />, and the <see cref="_directFeedLength" /> set are all
		///     inside <see cref="_gate" /> below, not before it, so the whole decide-copy-drain sequence
		///     is atomic per caller. Two concurrent callers on different threads (unreachable under
		///     today's single-receive-loop-per-mux topology, but not guaranteed by anything this class
		///     enforces on its own) now simply serialize on <see cref="_gate" /> rather than racing to
		///     clobber the shared staging buffer: the first caller's guard-through-drain runs to
		///     completion, including clearing <see cref="_directFeedLength" />, before the second caller
		///     can even evaluate its own guard. <see cref="_inbound" />'s lease-and-write path was already
		///     safe under concurrency on its own (a fresh <see cref="ArrayPool{T}" /> lease per call, and
		///     <see cref="Channel{T}" /> is internally thread-safe for concurrent writers), so it stays
		///     outside the lock; only the shared, mutable staging buffer needed it. Concurrent
		///     <see cref="FeedDatagram" /> calls are therefore safe: every datagram is either drained
		///     inline or queued, on exactly one path, never silently dropped or corrupted, regardless of
		///     how many callers overlap.
		///     </para>
		/// </summary>
		public void FeedDatagram(ReadOnlySpan<byte> datagram)
		{
			if (Volatile.Read(ref _disposed) != 0) return;

			lock (_gate)
			{
				if (Volatile.Read(ref _disposed) != 0) return;

				if (_handshakeDone && !_draining && _inbound.Reader.Count == 0 && datagram.Length <= _directFeedBuffer.Length)
				{
					datagram.CopyTo(_directFeedBuffer);
					_directFeedLength = datagram.Length;
					DrainLocked();
					return;
				}
			}

			byte[] leased = ArrayPool<byte>.Shared.Rent(datagram.Length);
			datagram.CopyTo(leased);

			if (!_inbound.Writer.TryWrite((leased, datagram.Length)))
			{
				ArrayPool<byte>.Shared.Return(leased);
				return;
			}

			if (!_handshakeDone) return;

			DrainUnderGate();
		}

		/// <summary>
		///     Acquires <see cref="_gate" /> and runs <see cref="DrainLocked" />. Used by the
		///     lease-and-<see cref="_inbound" /> path, which does not already hold the gate when it
		///     decides to drain (unlike the direct-feed fast path in <see cref="FeedDatagram" />, which
		///     calls <see cref="DrainLocked" /> directly since it is already inside the lock).
		/// </summary>
		private void DrainUnderGate()
		{
			lock (_gate)
			{
				if (Volatile.Read(ref _disposed) != 0)
				{
					_directFeedLength = -1;
					return;
				}

				DrainLocked();
			}
		}

		/// <summary>
		///     The actual drain-and-cleanup body shared by both of <see cref="FeedDatagram" />'s paths.
		///     Must only be called while already holding <see cref="_gate" />. See
		///     <see cref="FeedDatagram" />'s own doc comment for the invariants this preserves.
		/// </summary>
		private void DrainLocked()
		{
			_draining = true;
			try
			{
				DrainPending();
			}
			catch (DtlsSessionClosedException)
			{
			}
			finally
			{
				_draining = false;
				_directFeedLength = -1;
				if (_deferredScratchReturn)
				{
					_deferredScratchReturn = false;
					ArrayPool<byte>.Shared.Return(_receiveScratch);
					ArrayPool<byte>.Shared.Return(_directFeedBuffer);
				}
			}
		}

		/// <summary>
		///     Runs BouncyCastle's blocking handshake protocol on a pool thread; this is the one
		///     place this session ever blocks a thread. Resolves false, rather than throwing, on any
		///     handshake failure, including a fingerprint mismatch raised from the server or client
		///     certificate callbacks in <see cref="DtlsHandshakeServer" />/<see cref="DtlsHandshakeClient" />,
		///     or <paramref name="cancellationToken" /> being cancelled. The token only gates this one
		///     call: <see cref="RequestClose" /> unblocks a handshake in-flight the same way
		///     <see cref="Dispose" /> does (Finding 2: the token alone only prevents starting
		///     <see cref="Task.Run" />, it does nothing once BouncyCastle's blocking Accept/Connect is
		///     already running), and the registration is removed again once this call returns so a
		///     later cancellation of the same token has no effect on the now-established session.
		///     <para>
		///     Round-2 Item 3 / round-3 (ordering): <see cref="RequestClose" /> firing (cancellation,
		///     or a concurrent <see cref="Dispose" />) at the exact instant Accept/Connect was already
		///     returning successfully is a real, if low-probability, race: the <c>await</c> below can
		///     complete with a valid transport even though a concurrent <see cref="RequestClose" /> call
		///     is about to set, or is in the middle of setting, <see cref="_closed" />. Handing back
		///     <see langword="true" /> in that case would leave a "successful" session whose
		///     <see cref="FeedDatagram" /> writes into an already-completed channel forever, silently
		///     going nowhere. Reading <see cref="_closed" /> right after the <c>await</c> is not enough
		///     by itself: with the registration still live (a plain <c>using</c>, disposed only as the
		///     method unwinds), that read can still race a <see cref="RequestClose" /> call running
		///     concurrently on the token's callback thread. The fix is the explicit
		///     <c>registration.Dispose()</c> calls below, placed BEFORE the <see cref="_closed" /> read
		///     on every path: <see cref="CancellationTokenRegistration.Dispose" /> is documented to
		///     block until any in-flight callback has fully completed, and after it returns no callback
		///     can start later either, so by the time <see cref="_closed" /> is read, <see cref="RequestClose" />
		///     has either already run to completion or can never run at all — the read is deterministic,
		///     not a race. The transport is closed directly since it was never stored into
		///     <see cref="_dtlsTransport" />.
		///     </para>
		/// </summary>
		public async Task<bool> DoHandshakeAsync(CancellationToken cancellationToken)
		{
			CancellationTokenRegistration registration = cancellationToken.CanBeCanceled
				? cancellationToken.Register(RequestClose)
				: default;
			try
			{
				DtlsTransport transport = await Task.Run(RunHandshake, cancellationToken).ConfigureAwait(false);

				// Disposing here, before reading _closed, is load-bearing: it blocks until a
				// concurrently-running RequestClose (from this same registration) has fully finished,
				// and guarantees none can start afterward, so the read below can no longer race it.
				registration.Dispose();

				if (_closed)
				{
					lock (_gate)
					{
						transport.Close();
					}

					return false;
				}

				_dtlsTransport = transport;
				_handshakeDone = true;
				return true;
			}
			catch (Exception ex)
			{
				registration.Dispose();
				Log.Debug($"DTLS handshake failed ({(_isServer ? "server" : "client")}).", ex);
				return false;
			}
		}

		private DtlsTransport RunHandshake()
		{
			if (_isServer)
			{
				var server = new DtlsHandshakeServer(_localCertificate, _expectedRemoteFingerprint);
				return new DtlsServerProtocol().Accept(server, _transportAdapter);
			}

			var client = new DtlsHandshakeClient(_localCertificate, _expectedRemoteFingerprint);
			return new DtlsClientProtocol().Connect(client, _transportAdapter);
		}

		public void SendApplicationData(ReadOnlySpan<byte> payload)
		{
			_dtlsTransport?.Send(payload);
		}

		/// <summary>
		///     One receive-plus-drain cycle per queued raw datagram: <see cref="DtlsTransport.Receive" />
		///     pulls the next queued datagram and decodes its first record; <see cref="DtlsTransport.ReceivePending" />
		///     then drains any further records BouncyCastle bundled from that same datagram without
		///     touching the queue again. The outer loop repeats for any datagram left queued from a
		///     previous call, so the session self-heals rather than falling behind.
		///     <para>
		///     The 1 passed to <c>Receive</c> is load-bearing, not a rounding choice (Finding 1).
		///     BouncyCastle's own <c>Timeout.ForWaitMillis</c> treats 0 as "no deadline" (returns
		///     null), and <c>Timeout.GetWaitMillis(null, ...)</c> always returns 0, so
		///     <c>DtlsRecordLayer.Receive(buf, 0)</c> never builds a real deadline. If it discards a
		///     record (wrong epoch, bad MAC, replay, retransmitted Finished per RFC 9146) it retries
		///     internally against that same always-0, never-expiring wait and, once the queue is
		///     genuinely empty, spins at CPU speed on this thread forever (verified against BC 2.7.0's
		///     <c>DtlsRecordLayer.cs</c> and <c>Timeout.cs</c> source directly, not inferred). A
		///     waitMillis of 1 makes <c>Timeout.ForWaitMillis</c> build a real, non-null deadline, so
		///     BouncyCastle's retry loop provably re-checks a real elapsed-time computation each pass
		///     and exits once that ~1 ms has genuinely elapsed; <see cref="ReceiveFromQueue" /> honours
		///     it by actually blocking up to that long (via the same <see cref="CancellationTokenSource" />
		///     already used for the handshake), so the wait is a bounded yield, not a busy spin.
		///     </para>
		/// </summary>
		private void DrainPending()
		{
			DtlsTransport transport = _dtlsTransport;
			if (transport == null) return;

			do
			{
				int n = transport.Receive(_receiveScratch.AsSpan(), 1);
				while (n > 0)
				{
					OnDecrypted?.Invoke(_receiveScratch.AsSpan(0, n));

					// Round-2 Item 1: a subscriber may have called Dispose() from inside that
					// invocation. Dispose is reentrant-safe on this thread (see the class doc
					// comment's Invariant paragraph) and, when it runs while _draining is true,
					// closes the transport and defers the scratch buffer's return rather than
					// freeing it immediately, but it still closes the transport right away, so
					// this loop must not call anything on it again once disposal has happened.
					if (Volatile.Read(ref _disposed) != 0) return;

					n = transport.ReceivePending(_receiveScratch.AsSpan(), null);
				}
			} while (_inbound.Reader.Count > 0);
		}

		/// <summary>
		///     Serves both the blocking handshake thread (positive waitMillis, BouncyCastle's own
		///     retransmission schedule) and <see cref="DrainPending" />'s post-handshake drain (always
		///     1, never 0; see that method's doc comment). Every "nothing available" exit funnels
		///     through <see cref="NoDataOrThrow" /> so a session closed mid-wait
		///     (<see cref="RequestClose" />) aborts the caller instead of returning an ordinary -1 for
		///     it to retry.
		///     <para>
		///     Round-2 Item 2: the zero-allocation read (<see cref="TryReadNow" />) is always attempted
		///     first, regardless of <paramref name="waitMillis" />. The steady-state plan requires zero
		///     per-datagram allocation; <see cref="DrainPending" /> passing 1 rather than 0 (Finding 1)
		///     must not, by itself, move every post-handshake receive onto the
		///     <see cref="CancellationTokenSource" />-and-OS-timer path when the very datagram being
		///     drained is already sitting in <see cref="_inbound" /> (it always is, on
		///     <see cref="DrainPending" />'s first call: <see cref="FeedDatagram" /> enqueues it
		///     immediately before draining). The bounded wait below is now reached only when the queue
		///     is genuinely empty: BouncyCastle's own discard-retry case (Finding 1) or the handshake's
		///     network-bound retransmission wait, both already rare/already-waiting-on-the-network, so
		///     paying for a timer there is acceptable. <see cref="CancellationTokenSource.TryReset" />
		///     was considered for reuse (verified against its Microsoft Learn documentation) but
		///     rejected: it is documented for "the sole owner... when the operation... has completed
		///     [and] no-one else will attempt to cancel it", explicitly warns reuse concurrently with a
		///     pending cancellation is not thread-safe, and does not itself rearm a new delay (a
		///     millisecond-delay <see cref="CancellationTokenSource" /> still needs <c>CancelAfter</c>
		///     after every reset) — extra bookkeeping this now-rare branch does not need.
		///     </para>
		/// </summary>
		private int ReceiveFromQueue(Span<byte> buffer, int waitMillis)
		{
			if (TryReadNow(buffer, out int length)) return length;

			if (waitMillis <= 0) return NoDataOrThrow();

			bool canRead;
			using (var cts = new CancellationTokenSource(waitMillis))
			{
				try
				{
					canRead = _inbound.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();
				}
				catch (OperationCanceledException)
				{
					return NoDataOrThrow();
				}
			}

			if (canRead && TryReadNow(buffer, out length)) return length;

			return NoDataOrThrow();
		}

		/// <summary>
		///     Round-4 Finding B: checks <see cref="FeedDatagram" />'s direct-feed slot first. It is
		///     only ever live for the duration of one <see cref="FeedDatagram" /> call (see that
		///     method's doc comment), so consuming and clearing it here, the instant it is used, is what
		///     makes that guarantee hold: BouncyCastle has the bytes copied into its own buffer
		///     (<paramref name="buffer" />, owned by the caller further up this same synchronous call
		///     chain) before this method returns, so nothing outside this call ever observes the slot.
		/// </summary>
		private bool TryReadNow(Span<byte> buffer, out int length)
		{
			if (_directFeedLength >= 0)
			{
				length = Math.Min(_directFeedLength, buffer.Length);
				_directFeedBuffer.AsSpan(0, length).CopyTo(buffer);
				_directFeedLength = -1;
				return true;
			}

			if (!_inbound.Reader.TryRead(out (byte[] Leased, int Length) item))
			{
				length = 0;
				return false;
			}

			try
			{
				length = Math.Min(item.Length, buffer.Length);
				item.Leased.AsSpan(0, length).CopyTo(buffer);
				return true;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(item.Leased);
			}
		}

		/// <summary>
		///     Finding 2/3's shared exit: a plain "nothing queued right now" is a normal -1 BouncyCastle
		///     is expected to retry on its own schedule, but once <see cref="RequestClose" /> has fired
		///     (<see cref="Dispose" />, or a cancelled <see cref="DoHandshakeAsync" />) the same "nothing
		///     queued" moment must abort immediately instead, since nothing will ever arrive again.
		/// </summary>
		private int NoDataOrThrow()
		{
			if (_closed) throw new DtlsSessionClosedException();
			return -1;
		}

		/// <summary>
		///     Round-4 Finding A: passes BouncyCastle's span straight through to <see cref="_sendToWire" />.
		///     The whole call chain, from <see cref="DtlsTransport.Send(ReadOnlySpan{byte})" /> down to
		///     <see cref="UdpMux.Send" />, is synchronous, so there was never a reason to lease a copy
		///     just to hand out a <see cref="ReadOnlyMemory{T}" /> nobody kept past the call.
		/// </summary>
		private void SendToWire(ReadOnlySpan<byte> buffer)
		{
			_sendToWire(buffer);
		}

		/// <summary>
		///     Shared unblock signal for <see cref="Dispose" /> and a cancelled <see cref="DoHandshakeAsync" />
		///     (Finding 2): marks the session closed and completes the inbound channel's writer, so any
		///     in-flight or future <see cref="ReceiveFromQueue" /> call that finds no datagram raises
		///     <see cref="DtlsSessionClosedException" /> through <see cref="NoDataOrThrow" /> instead of
		///     returning an ordinary -1 for BouncyCastle to retry on its own schedule. Idempotent:
		///     completing an already-completed channel writer is a harmless no-op.
		/// </summary>
		private void RequestClose()
		{
			_closed = true;
			_inbound.Writer.TryComplete();
		}

		/// <summary>
		///     Marks the session closed (<see cref="RequestClose" />), unblocking a handshake still
		///     running on the <see cref="Task.Run" /> thread, then, under <see cref="_gate" />, closes
		///     the BouncyCastle transport if the handshake reached that far and returns every
		///     still-queued lease to the pool. The gate excludes a concurrently running
		///     <see cref="FeedDatagram" /> drain called from another thread (Finding 3): either the
		///     drain finishes and releases the gate before this section runs, or this section runs
		///     first and the drain's own disposed check, repeated inside its own gate acquisition,
		///     bails out before touching the now-freed <see cref="_receiveScratch" />. A same-thread
		///     <em>reentrant</em> call (Dispose invoked synchronously from an <see cref="OnDecrypted" />
		///     subscriber, round-2 Item 1) is different: the gate is already held by this same thread,
		///     so it does not block, and <see cref="_draining" /> being true means the drain loop is
		///     still further up this exact call stack, still about to touch <see cref="_receiveScratch" />
		///     once this call returns control to it. In that case the buffer is not returned here;
		///     <see cref="_deferredScratchReturn" /> is set instead, and <see cref="FeedDatagram" />'s
		///     own <c>finally</c> performs the return once the drain has actually stopped.
		///     <see cref="_directFeedBuffer" /> (round-4 Finding B) shares <see cref="_receiveScratch" />'s
		///     lifetime exactly, so it is returned, or deferred, alongside it on both branches.
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

			RequestClose();

			lock (_gate)
			{
				_dtlsTransport?.Close();

				while (_inbound.Reader.TryRead(out (byte[] Leased, int Length) item))
				{
					ArrayPool<byte>.Shared.Return(item.Leased);
				}

				if (_draining)
				{
					_deferredScratchReturn = true;
				}
				else
				{
					ArrayPool<byte>.Shared.Return(_receiveScratch);
					ArrayPool<byte>.Shared.Return(_directFeedBuffer);
				}
			}
		}

		/// <summary>
		///     Thrown by <see cref="NoDataOrThrow" /> once the session has been closed
		///     (<see cref="RequestClose" />) and no datagram is available. Deriving from
		///     <see cref="IOException" /> is deliberate, verified against BC 2.7.0 source: BouncyCastle's
		///     <c>DtlsTransport.Receive</c> catch ladder rethrows an <see cref="IOException" /> as-is
		///     after failing the record layer, so this propagates out of Accept/Connect unwrapped,
		///     unlike a <see cref="TlsTimeoutException" /> (silently absorbed by
		///     <c>DtlsRecordLayer.ReceiveDatagram</c>'s own catch, one layer further down, and
		///     indistinguishable there from an ordinary retry) or any other exception type (wrapped into
		///     an opaque <see cref="TlsFatalAlert" />).
		/// </summary>
		private sealed class DtlsSessionClosedException : IOException
		{
		}

		/// <summary>
		///     BouncyCastle's <see cref="DatagramTransport" /> as seen from this session: reads come
		///     from <see cref="_inbound" />, writes go straight to <see cref="_sendToWire" />. Kept as
		///     a nested adapter rather than having <see cref="DtlsSession" /> implement the interface
		///     directly so the BouncyCastle-facing surface (byte[] AND Span overloads of every member)
		///     does not leak onto the session's own public API.
		/// </summary>
		private sealed class DatagramTransportAdapter : DatagramTransport
		{
			private readonly DtlsSession _session;

			public DatagramTransportAdapter(DtlsSession session)
			{
				_session = session;
			}

			public int GetReceiveLimit()
			{
				return WireLimit;
			}

			public int GetSendLimit()
			{
				return WireLimit;
			}

			public int Receive(byte[] buf, int off, int len, int waitMillis)
			{
				return _session.ReceiveFromQueue(buf.AsSpan(off, len), waitMillis);
			}

			public int Receive(Span<byte> buffer, int waitMillis)
			{
				return _session.ReceiveFromQueue(buffer, waitMillis);
			}

			public void Send(byte[] buf, int off, int len)
			{
				_session.SendToWire(buf.AsSpan(off, len));
			}

			public void Send(ReadOnlySpan<byte> buffer)
			{
				_session.SendToWire(buffer);
			}

			public void Close()
			{
			}
		}
	}
}