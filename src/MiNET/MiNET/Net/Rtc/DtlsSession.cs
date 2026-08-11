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
	///     receive thread for the life of the session. Once the handshake is done,
	///     <see cref="FeedDatagram" /> decrypts application data and alerts natively, in place, straight
	///     out of the caller's own span, via <see cref="_recordCrypto" /> and
	///     <see cref="ProcessApplicationDatagramLocked" />: BouncyCastle is out of the loop entirely, and
	///     nothing it built is retained past the handshake. A post-handshake datagram carrying an
	///     epoch-0 record - a peer retransmitting a final handshake flight it believes was lost - is
	///     handled natively too: <see cref="ProcessApplicationDatagramLocked" /> re-emits
	///     <see cref="_finalFlightCache" />, the raw bytes of our own last outgoing flight, verbatim to
	///     the wire, rate-limited, and drops the record itself without delivering it anywhere. No
	///     background thread ever exists after the handshake.
	///     Invariant: <see cref="_gate" /> serializes every access to
	///     <see cref="_recordCrypto" />, <see cref="_receiveScratch" />, and <see cref="_directFeedBuffer" />
	///     between <see cref="FeedDatagram" />'s drain section and <see cref="Dispose" />, so a concurrent
	///     teardown from another thread can never return either buffer to the pool while a drain still
	///     holds a span over one - EXCEPT for the one deliberate window
	///     <see cref="DrainQueueLocked" /> opens around each <see cref="OnDecrypted" /> call: the gate is
	///     explicitly released for that one call and reacquired right after, so a subscriber's own
	///     work never runs while blocking a concurrent <see cref="FeedDatagram" /> or
	///     <see cref="SendApplicationData" /> caller.
	///     <see cref="_draining" /> is what keeps the buffer-lifetime half of this invariant true across
	///     that window regardless: set <see langword="true" /> before the window ever opens and not
	///     cleared until the drain loop has fully stopped, so a <see cref="Dispose" /> that reaches
	///     <see cref="_gate" /> during the window - reentrant on this same thread (a subscriber calling
	///     <see cref="Dispose" /> synchronously from inside <see cref="OnDecrypted" />) or genuinely
	///     concurrent on another thread (only possible now that the gate is not held for the callback's
	///     whole duration) - always observes <see cref="_draining" /> true and defers both buffers'
	///     return to <see cref="FeedDatagram" />'s own unwind instead of freeing them out from under the
	///     drain loop still running further up the call stack (or, in the concurrent case, on another
	///     thread entirely). See <see cref="DrainQueueLocked" />'s own remarks for the mechanics.
	///     Threading: <see cref="FeedDatagram" /> itself is safe to call concurrently from multiple
	///     threads, even though nothing in today's topology actually does so, a single
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

		/// <summary>
		///     <see cref="ReadOnlyMemory{T}" />, not <see cref="ReadOnlySpan{T}" />, because the
		///     receive pipeline this feeds (<see cref="SctpAssociation.OnPacketReceived" />, ultimately
		///     <see cref="SctpAssociation.OnMessage" />) delivers a <see cref="System.Buffers.ReadOnlySequence{T}" />
		///     end to end, and a sequence cannot wrap a span. <paramref name="payload" /> is still only
		///     valid for the duration of the callback: it is a slice of <see cref="_receiveScratch" />, our
		///     own pooled array, unchanged from when this was a span.
		/// </summary>
		public delegate void DecryptedHandler(ReadOnlyMemory<byte> payload);

		/// <summary>
		///     Takes the outgoing datagram as a span, not a <see cref="ReadOnlyMemory{T}" />,
		///     since the whole call chain from BouncyCastle's <see cref="DtlsTransport.Send(ReadOnlySpan{byte})" />
		///     down to <see cref="UdpMux.Send" /> is synchronous. Nothing here needs a heap reference to
		///     the datagram after the call returns, so a <see cref="ReadOnlyMemory{T}" /> parameter would
		///     force an intermediate ArrayPool lease (rent, copy the span into it, hand out the memory,
		///     return the lease) for no reason: one full copy and one pool round trip per outgoing datagram.
		/// </summary>
		public delegate void WireSender(ReadOnlySpan<byte> datagram);

		public event DecryptedHandler OnDecrypted;

		private readonly RtcCertificate _localCertificate;
		private readonly string _expectedRemoteFingerprint;
		private readonly bool _isServer;
		private readonly WireSender _sendToWire;
		private readonly int[] _cipherSuites;
		private readonly Channel<(byte[] Leased, int Length)> _inbound = Channel.CreateUnbounded<(byte[] Leased, int Length)>();
		private readonly byte[] _receiveScratch = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);
		private readonly DatagramTransportAdapter _transportAdapter;
		private readonly object _gate = new object();

		// Deliberately separate from _gate, which the receive path holds across OnDecrypted:
		// serializing SendApplicationData on _gate would block application writes behind whatever
		// a subscriber does inside OnDecrypted. This lock only ever needs to exclude concurrent
		// callers of SendApplicationData itself from each other.
		private readonly object _sendGate = new object();

		// A persistent (allocated once, never per-call), reused staging buffer for
		// FeedDatagram's no-backlog fast path. See FeedDatagram and TryReadNow for the full mechanism.
		private readonly byte[] _directFeedBuffer = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);
		private int _directFeedLength = -1;

		private CapturingTlsCrypto _capturingCrypto;
		private DtlsRecordCrypto _recordCrypto;
		private volatile bool _handshakeDone;
		private volatile bool _closed;
		private int _disposed;
		private long _droppedRecords;

		// The raw bytes of our own final handshake flight, one entry per outgoing wire datagram,
		// oldest first: everything BouncyCastle sent since it last received something. Written only
		// from the single handshake thread (SendToWire/ReceiveFromQueue, both reachable only from
		// BouncyCastle's blocking Accept/Connect - see RunHandshake), so it needs no lock of its own
		// while the handshake is running; DoHandshakeAsync's existing safe-publication argument for
		// _recordCrypto (the handshake thread's writes all happen strictly before the volatile
		// _handshakeDone write on that same thread) applies here too, so ProcessApplicationDatagramLocked
		// reading it under _gate post-handshake can never race the handshake thread's own writes. Capped
		// at MaxFinalFlightDatagrams, keeping the most recent on overflow: a real final flight is 2-3
		// small records, so the cap rarely matters for a genuine peer, but it is also the actual bound
		// on what a bare, unauthenticated 13-byte epoch-0 header can elicit from this session - at most
		// 16 cached datagrams per trigger, itself rate-limited to once a second
		// (HandleEpochZeroRecordLocked), and reflected only back at the one address this session's
		// WireSender targets, never anywhere attacker-chosen.
		private readonly List<byte[]> _finalFlightCache = new List<byte[]>();
		private const int MaxFinalFlightDatagrams = 16;

		// The clock seam for the epoch-0 resend rate limit: every "now" read on that path goes through
		// this instead of Environment.TickCount64 directly, so a test can drive the 1-second window
		// deterministically. Defaults to the real clock; only tests (via the assembly's
		// InternalsVisibleTo) ever replace it. Matches SctpAssociation.ClockNowMillis's own seam.
		internal Func<long> ClockNowMillis = () => Environment.TickCount64;
		private long? _lastResendAtMillis;
		private const long ResendRateLimitMillis = 1000;
		private long _resendsPerformed;
		private long _epochZeroRecordsDropped;

		// Touched only under _sendGate (see TrySendCloseNotifyLocked's own remarks), by both of that
		// method's call sites: guards the whole session, not one call, against ever putting a second
		// close_notify on the wire.
		private bool _closeNotifySent;

		// DTLS 1.2 content types (RFC 6347 4.1) this session dispatches on post-handshake; every other
		// value at epoch 1 is drop-and-count without ever being handed to _recordCrypto.
		private const byte ContentTypeAlert = 21;
		private const byte ContentTypeApplicationData = 23;

		// RFC 5246 7.2 / 7.2.1: any alert with description close_notify (0), at any level, and any alert
		// at level fatal (2) both end the connection immediately. close_notify additionally requires
		// sending a close_notify of our own back before closing; a fatal alert does not.
		private const byte AlertLevelWarning = 1;
		private const byte AlertLevelFatal = 2;
		private const byte AlertDescriptionCloseNotify = 0;

		// Every legitimate SendApplicationData payload rides inside one SCTP packet, which
		// SctpAssociation already caps at SctpPacket.MaxSize: this is a caller-bug ceiling, not a size a
		// real send would ever approach, checked before the stackalloc below is ever sized from it. The
		// DTLS layer must accept anything SCTP can ever hand it, so this can never be smaller than
		// SctpPacket.MaxSize; internal so a test can pin that relationship directly.
		internal const int MaxSendPayloadLength = SctpPacket.MaxSize;

		/// <summary>
		///     The epoch-1 key block <see cref="CapturingTlsCrypto" /> captured out of this handshake.
		///     Null until <see cref="RunHandshake" /> has run and BouncyCastle has actually created its
		///     cipher (the point in the handshake where the key block first exists), which happens
		///     strictly before <see cref="DoHandshakeAsync" /> can observe the handshake task as
		///     complete, so a caller that has awaited a successful <see cref="DoHandshakeAsync" /> always
		///     sees a non-null value here.
		/// </summary>
		internal CapturedDtlsKeys CapturedKeys => _capturingCrypto?.Captured;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): the native record layer built from this session's captured keys once the handshake completes, null before that.</summary>
		internal DtlsRecordCrypto RecordCrypto => _recordCrypto;

		/// <summary>Records at epoch 1 that were deliberately never acted on: a content type other than application-data or alert, or a non-fatal alert whose description is not close_notify. Not the same as <see cref="DtlsRecordCrypto" />'s own malformed/replay/decrypt-failure counters, and not the epoch-0 counters below.</summary>
		internal long DroppedRecords => Interlocked.Read(ref _droppedRecords);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many times an epoch-0 record triggered a verbatim re-emit of <see cref="_finalFlightCache" />.</summary>
		internal long ResendsPerformed => Interlocked.Read(ref _resendsPerformed);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many epoch-0 records were dropped without a resend, because <see cref="_finalFlightCache" /> was empty, the 1-second resend rate limit was still active, a resend already answered this same datagram, or the session was already closed.</summary>
		internal long EpochZeroRecordsDropped => Interlocked.Read(ref _epochZeroRecordsDropped);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many datagrams <see cref="_finalFlightCache" /> currently holds. Zero for a client-role handshake (its last handshake event is a receive, not a send) and non-zero for a server-role one, once the handshake has completed.</summary>
		internal int FinalFlightCacheCount => _finalFlightCache.Count;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): whether this session has been torn down, by either <see cref="Dispose" /> or an alert-driven <see cref="RequestClose" />. <see cref="RtcPeer" /> exposes this to interop tests as <c>DtlsSessionClosed</c>.</summary>
		internal bool IsClosed => _closed;

		// Both touched only while holding _gate (see the class doc comment's Invariant paragraph),
		// so neither needs volatile or Interlocked: _gate's acquire/release already provides the
		// necessary visibility across the two threads that can ever reach either field.
		private bool _draining;
		private bool _deferredScratchReturn;

		public DtlsSession(RtcCertificate localCertificate, string expectedRemoteFingerprint, bool isServer, WireSender sendToWire)
			: this(localCertificate, expectedRemoteFingerprint, isServer, sendToWire, cipherSuites: null)
		{
		}

		/// <summary>
		///     Test visibility only (assembly's InternalsVisibleTo to MiNETTests):
		///     <paramref name="cipherSuites" /> narrows what <see cref="DtlsHandshakeServer" />/
		///     <see cref="DtlsHandshakeClient" /> offer, letting a test force one specific suite (e.g.
		///     AES-256-GCM) to prove the native record layer against both key lengths this stack
		///     negotiates. The public constructor above always passes null, which offers both.
		/// </summary>
		internal DtlsSession(RtcCertificate localCertificate, string expectedRemoteFingerprint, bool isServer, WireSender sendToWire, int[] cipherSuites)
		{
			_localCertificate = localCertificate;
			_expectedRemoteFingerprint = expectedRemoteFingerprint;
			_isServer = isServer;
			_sendToWire = sendToWire;
			_cipherSuites = cipherSuites;
			_transportAdapter = new DatagramTransportAdapter(this);
		}

		/// <summary>
		///     Feeds one raw datagram demuxed as DTLS by <see cref="IceSession.OnDtlsDatagram" />.
		///     Before the handshake completes this only queues the datagram for the blocking
		///     <see cref="Task.Run" /> handshake thread to pick up. Afterwards it also drains it
		///     immediately, inline, on the calling thread, under <see cref="_gate" /> so a concurrent
		///     <see cref="Dispose" /> can never free <see cref="_receiveScratch" /> or dispose
		///     <see cref="_recordCrypto" /> out from under it (the disposed check alone is check-then-act,
		///     not a real exclusion). Draining always means native decode, via
		///     <see cref="ProcessApplicationDatagramLocked" />: an epoch-0 record within the datagram (a
		///     peer retransmitting a final handshake flight it believes was lost) is handled inline there
		///     too, by re-emitting the cached final flight rather than delivering the record anywhere (see
		///     that method's own remarks).
		///     A <see cref="DtlsSessionClosedException" /> raised by the drain (Dispose or a cancelled
		///     handshake closed the session while this call was in flight) is swallowed here: it is an
		///     expected race during teardown, not a caller-visible failure. <see cref="_draining" />
		///     brackets the call so a <em>reentrant</em> <see cref="Dispose" />, called synchronously
		///     from inside an <see cref="OnDecrypted" /> subscriber, defers returning
		///     <see cref="_receiveScratch" /> to the pool until this method's own <c>finally</c> below,
		///     after the drain loop (further up this same call stack) has stopped touching it (the lock
		///     alone is reentrant on this thread and does not by itself prevent that).
		///     <para>
		///     The steady-state case is a datagram that is about to be consumed
		///     inline, on this exact call, by <see cref="DrainQueueLocked" />'s very first iteration a
		///     few instructions from now, with nothing else backlogged. Leasing it from
		///     <see cref="ArrayPool{T}" /> and round-tripping it through <see cref="_inbound" /> just to
		///     read it straight back out again bought nothing in that case. When the handshake is done,
		///     no drain from this same thread is already running (<see cref="_draining" />), the channel
		///     is empty, and the datagram fits <see cref="_directFeedBuffer" />, this copies straight into
		///     that persistent buffer (allocated once, at construction, never per-call; a plain memory
		///     copy, not a heap allocation) instead and lets <see cref="TryReadNow" /> pick it up first.
		///     <see cref="_directFeedLength" /> is the only state that says the slot is live; it is
		///     cleared by <see cref="TryReadNow" /> the instant it copies the bytes into
		///     <see cref="DrainQueueLocked" />'s own local buffer (synchronously, nested inside this same
		///     call, well before this method returns), and <see cref="DrainLocked" />'s own <c>finally</c>
		///     clears it unconditionally besides, so the slot can never outlive this call on any path,
		///     including one that never reaches <see cref="TryReadNow" /> at all. Anything that does not
		///     fit this fast path (mid-handshake, an existing backlog, a reentrant drain, or a datagram
		///     too large for the staging buffer) falls through to the unchanged lease-and-<see cref="_inbound" />
		///     path below.
		///     </para>
		///     <para>
		///     For concurrent <see cref="FeedDatagram" /> calls: the guard, the copy into
		///     <see cref="_directFeedBuffer" />, and the <see cref="_directFeedLength" /> set are all
		///     inside <see cref="_gate" /> below, not before it, so the whole decide-copy-drain sequence
		///     is atomic per caller. Two concurrent callers on different threads (unreachable under
		///     today's single-receive-loop-per-mux topology, but not guaranteed by anything this class
		///     enforces on its own) simply serialize on <see cref="_gate" /> rather than racing to
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
			if (_closed) return;

			lock (_gate)
			{
				if (Volatile.Read(ref _disposed) != 0 || _closed) return;

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
		///     Decrypts every record of one post-handshake datagram, natively, record by record. A leaf
		///     step of <see cref="DrainQueueLocked" />'s single-flight loop, not a second entry point of
		///     its own: must be called only from there, which is what guarantees only one thread is ever
		///     inside this method at a time (it decrypts into the shared <see cref="_receiveScratch" />
		///     buffer, so two overlapping calls would corrupt each other's plaintext out from under a
		///     concurrent <see cref="OnDecrypted" /> delivery - exactly the failure mode
		///     <see cref="_draining" /> exists to rule out, and which <see cref="DrainQueueLocked" />'s doc
		///     comment explains in full). Releases <see cref="_gate" /> around each
		///     <see cref="OnDecrypted" /> call, via the explicit <see cref="Monitor.Exit(object)" />/
		///     <see cref="Monitor.Enter(object)" /> pair below rather than a nested <c>lock</c> (which is
		///     reentrant and would keep the gate held across the whole callback). The disposed/closed
		///     check immediately after each re-acquire is load-bearing: a subscriber, or a genuinely
		///     concurrent thread, may have disposed the session or torn it down via
		///     <see cref="RequestClose" /> while the gate was released, and <see cref="_recordCrypto" />
		///     must not be touched again once that has happened.
		///     <para>
		///     Walks records left to right using their own declared length
		///     (<see cref="DtlsRecordCrypto.TryReadRecordHeader" />), so a record whose content is
		///     rejected (wrong epoch, replay, bad tag) does not stop the walk: only a header that does
		///     not fit does, since at that point there is no trustworthy length to skip by. A record
		///     declaring epoch 0 - a peer retransmitting a final handshake flight it believes was lost -
		///     is never handed to <see cref="_recordCrypto" /> at all: <see cref="HandleEpochZeroRecordLocked" />
		///     decides whether to answer it, and the walk moves on to whatever follows in the same
		///     datagram, so a junk-prefix or coalesced-retransmission datagram never costs the peer its
		///     other, legitimate records. Content type 23 (application data) decrypts and delivers;
		///     content type 21 (alert) decrypts and, on close_notify (any level) or a fatal level, calls
		///     <see cref="RequestClose" /> and stops processing the rest of the datagram outright -
		///     close_notify additionally sends a close_notify of our own first (RFC 5246 7.2.1); every
		///     other alert, and every other content type, is drop-and-count via
		///     <see cref="_droppedRecords" /> without ever reaching
		///     <see cref="DtlsRecordCrypto.TryDecryptRecord" />. A decrypt rejection on either content
		///     type is already counted by <see cref="_recordCrypto" /> itself (replay, malformed, or bad
		///     tag) and simply moves on to the next record.
		///     </para>
		/// </summary>
		private void ProcessApplicationDatagramLocked(ReadOnlySpan<byte> datagram)
		{
			bool resentThisDatagram = false;
			int offset = 0;
			while (offset < datagram.Length)
			{
				if (!DtlsRecordCrypto.TryReadRecordHeader(datagram.Slice(offset), out byte contentType, out int epoch, out int fragmentLength)) return;

				int recordLength = DtlsRecordCrypto.HeaderLength + fragmentLength;
				ReadOnlySpan<byte> record = datagram.Slice(offset, recordLength);
				offset += recordLength;

				if (epoch == 0)
				{
					HandleEpochZeroRecordLocked(ref resentThisDatagram);
				}
				else if (contentType == ContentTypeApplicationData)
				{
					if (!_recordCrypto.TryDecryptRecord(record, _receiveScratch, out _, out int length)) continue;

					int deliveredLength = length;
					Monitor.Exit(_gate);
					try
					{
						OnDecrypted?.Invoke(_receiveScratch.AsMemory(0, deliveredLength));
					}
					finally
					{
						Monitor.Enter(_gate);
					}

					if (Volatile.Read(ref _disposed) != 0 || _closed) return;
				}
				else if (contentType == ContentTypeAlert)
				{
					if (!_recordCrypto.TryDecryptRecord(record, _receiveScratch, out _, out int length)) continue;

					if (length < 2)
					{
						Interlocked.Increment(ref _droppedRecords);
						continue;
					}

					byte level = _receiveScratch[0];
					byte description = _receiveScratch[1];

					// RFC 5246 7.2.1: close_notify ends the connection in both directions immediately,
					// and the recipient must send its own close_notify back before closing. The response
					// rides the native record layer's live sequence counter (EncryptRecord, not BC's own
					// Close), so a peer whose replay window our own application data has already advanced
					// still accepts it: BC's Finished-flight sequence would fall behind that window and be
					// dropped as a replay.
					if (description == AlertDescriptionCloseNotify)
					{
						lock (_sendGate)
						{
							TrySendCloseNotifyLocked();
						}

						RequestClose();
						ReclaimAbandonedLeasesLocked();
						return;
					}

					// RFC 5246 7.2: any fatal-level alert ends the connection immediately; unlike
					// close_notify, no response is expected or sent.
					if (level == AlertLevelFatal)
					{
						RequestClose();
						ReclaimAbandonedLeasesLocked();
						return;
					}

					Interlocked.Increment(ref _droppedRecords);
				}
				else
				{
					Interlocked.Increment(ref _droppedRecords);
				}
			}
		}

		/// <summary>
		///     Answers one epoch-0 record found by <see cref="ProcessApplicationDatagramLocked" />'s walk:
		///     the record itself is never delivered anywhere, only ever counted or answered with a
		///     verbatim resend of <see cref="_finalFlightCache" />. A peer still asking for the final
		///     flight never received it, so the sequence numbers those cached datagrams carry are absent
		///     from its anti-replay window - replaying them verbatim is safe. Drop-and-count, no resend,
		///     when the cache is empty (this side is the DTLS client, or this side's own last flight was a
		///     receive, not a send - see the class doc comment), when the 1-second rate limit
		///     (<see cref="ResendRateLimitMillis" />, read through <see cref="ClockNowMillis" />) is still
		///     active, when <paramref name="resentThisDatagram" /> is already <see langword="true" />
		///     (a resend already answered an earlier epoch-0 record in this same datagram, and answering
		///     twice for one datagram buys the peer nothing further), or when <see cref="_closed" /> is
		///     set: <see cref="RequestClose" /> (<see cref="Dispose" />, or a concurrent alert-driven
		///     close) can flip it true from another thread without ever needing <see cref="_gate" />
		///     itself, so a drain already inside this method can still observe it turn true out from
		///     under it. Re-checked here, immediately before the resend would actually reach the wire,
		///     for the same reason <see cref="SendApplicationData" /> and <see cref="SendToWire" /> both
		///     check it at their own last possible moment: RFC 5246 7.2.1 requires nothing further on the
		///     wire once closed, and a resent handshake flight is no exception.
		///     <para>
		///     Each cached datagram is sent inside its own <see langword="try" />/<see langword="catch" />,
		///     the same shape <see cref="TrySendCloseNotifyLocked" /> already uses for the identical
		///     failure (the peer's socket is gone; a prior ICMP port-unreachable can surface as a
		///     <see cref="System.Net.Sockets.SocketException" /> on the very next send): a lost resend
		///     datagram is droppable by nature, since a peer that never received the final flight the
		///     first time simply asks again, so one failure must not stop the rest of the flight from
		///     going out, abort the record walk still further up the call stack in
		///     <see cref="ProcessApplicationDatagramLocked" />, or unwind out of
		///     <see cref="FeedDatagram" /> entirely.
		///     </para>
		/// </summary>
		private void HandleEpochZeroRecordLocked(ref bool resentThisDatagram)
		{
			if (resentThisDatagram || _finalFlightCache.Count == 0)
			{
				Interlocked.Increment(ref _epochZeroRecordsDropped);
				return;
			}

			long now = ClockNowMillis();
			if (_lastResendAtMillis is long lastResendAt && now - lastResendAt < ResendRateLimitMillis)
			{
				Interlocked.Increment(ref _epochZeroRecordsDropped);
				return;
			}

			if (_closed)
			{
				Interlocked.Increment(ref _epochZeroRecordsDropped);
				return;
			}

			_lastResendAtMillis = now;
			resentThisDatagram = true;
			foreach (byte[] flightDatagram in _finalFlightCache)
			{
				try
				{
					_sendToWire(flightDatagram);
				}
				catch (Exception ex)
				{
					Log.Debug($"Failed to resend a cached final-flight datagram ({(_isServer ? "server" : "client")}).", ex);
				}
			}

			Interlocked.Increment(ref _resendsPerformed);
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

				// A drain already in progress on another thread releases _gate for the duration of
				// each OnDecrypted call (see DrainQueueLocked), so this call can reach here concurrently
				// with one already under way. Starting a second, overlapping drain here would decrypt,
				// and write _receiveScratch, from two threads at once. The datagram this call just
				// enqueued onto _inbound is not lost: the in-progress drain's own outer loop re-checks
				// _inbound.Reader.Count after every item it finishes and will pick it up.
				if (_draining) return;

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
				DrainQueueLocked();
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
		///     The single-flight loop behind <see cref="DrainLocked" />: keeps pulling one raw datagram
		///     at a time - the direct-feed slot first, then <see cref="_inbound" />, both via
		///     <see cref="TryReadNow" /> - into <see cref="_directFeedBuffer" /> (reused here purely as
		///     scratch space for the raw, still-undecrypted bytes; a plain memory copy, never a heap
		///     allocation) and dispatching each one to <see cref="ProcessApplicationDatagramLocked" />,
		///     until nothing is left queued. Must be called only from <see cref="DrainLocked" />, with
		///     <see cref="_draining" /> already set: that is what lets a second, concurrent
		///     <see cref="FeedDatagram" /> call safely just enqueue and return (see
		///     <see cref="DrainUnderGate" />) instead of starting an overlapping drain of its own, and
		///     this loop's own re-check of <see cref="TryReadNow" /> after each item is what picks up
		///     whatever queued while <see cref="_gate" /> was released for an <see cref="OnDecrypted" />
		///     call.
		///     <para>
		///     <see cref="ProcessApplicationDatagramLocked" /> decrypts into the shared
		///     <see cref="_receiveScratch" /> buffer and releases <see cref="_gate" /> around its own
		///     <see cref="OnDecrypted" /> call; routing every datagram through this one loop, rather than
		///     letting native decode run inline from <see cref="FeedDatagram" /> directly, is what keeps
		///     that sharing safe - two datagrams decrypting concurrently on different threads would
		///     otherwise be free to clobber each other's plaintext out from under a delivery still
		///     reading it. The disposed/closed check after each dispatch stops the loop the instant
		///     either fires mid-iteration (a subscriber disposing the session, or an alert record calling
		///     <see cref="RequestClose" />): the remaining queued datagrams, if any, are abandoned rather
		///     than processed, exactly like <see cref="Dispose" /> abandoning whatever is still queued in
		///     <see cref="_inbound" />.
		///     </para>
		/// </summary>
		private void DrainQueueLocked()
		{
			while (TryReadNow(_directFeedBuffer, out int length))
			{
				ProcessApplicationDatagramLocked(_directFeedBuffer.AsSpan(0, length));

				if (Volatile.Read(ref _disposed) != 0 || _closed) return;
			}
		}

		/// <summary>
		///     Runs BouncyCastle's blocking handshake protocol on a pool thread; this is the one
		///     place this session ever blocks a thread. Resolves false, rather than throwing, on any
		///     handshake failure, including a fingerprint mismatch raised from the server or client
		///     certificate callbacks in <see cref="DtlsHandshakeServer" />/<see cref="DtlsHandshakeClient" />,
		///     or <paramref name="cancellationToken" /> being cancelled. The token only gates this one
		///     call: <see cref="RequestClose" /> unblocks a handshake in-flight the same way
		///     <see cref="Dispose" /> does (the token alone only prevents starting
		///     <see cref="Task.Run" />; it does nothing once BouncyCastle's blocking Accept/Connect is
		///     already running), and the registration is removed again once this call returns so a
		///     later cancellation of the same token has no effect on the now-established session.
		///     <para>
		///     Ordering: <see cref="RequestClose" /> firing (cancellation,
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
		///     has either already run to completion or can never run at all: the read is deterministic,
		///     not a race. The transport is closed directly: this session never retains a reference to
		///     it at all, on either the success or the raced-close path (see <see cref="RunHandshake" />'s
		///     own remarks).
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
				// and guarantees none can start afterward, so the read below cannot race it.
				registration.Dispose();

				if (_closed)
				{
					lock (_gate)
					{
						transport.Close();
					}

					return false;
				}

				// Assigned strictly before _handshakeDone's volatile write a few lines down publishes
				// it, on this same thread: a caller observing _handshakeDone true is guaranteed to see a
				// non-null _recordCrypto as well. _finalFlightCache needs the identical argument: the
				// handshake thread's last write to it (SendToWire, still on this same call stack inside
				// RunHandshake above) happens strictly before this same write, so it too is safely
				// published by the time any reader can observe _handshakeDone true.
				_recordCrypto = new DtlsRecordCrypto(CapturedKeys, _isServer);

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

		/// <summary>
		///     Drives BouncyCastle's blocking Accept/Connect to completion and returns the resulting
		///     <see cref="DtlsTransport" /> to <see cref="DoHandshakeAsync" />, which never stores it into
		///     a field: nothing outside this one call ever touches BouncyCastle again, so the transport,
		///     the handshake protocol state, and everything else BouncyCastle built are free to be
		///     collected the moment this method's caller is done with the local variable. Every
		///     <see cref="DatagramTransportAdapter.Send" />/<see cref="DatagramTransportAdapter.Receive(Span{byte},int)" />
		///     call BouncyCastle makes while this method runs - the only two places that touch
		///     <see cref="_finalFlightCache" /> - happens synchronously on this same thread, so that cache
		///     needs no lock of its own for the whole span this method is running.
		/// </summary>
		private DtlsTransport RunHandshake()
		{
			_capturingCrypto = new CapturingTlsCrypto();

			if (_isServer)
			{
				var server = new DtlsHandshakeServer(_capturingCrypto, _localCertificate, _expectedRemoteFingerprint, _cipherSuites);
				return new DtlsServerProtocol().Accept(server, _transportAdapter);
			}

			var client = new DtlsHandshakeClient(_capturingCrypto, _localCertificate, _expectedRemoteFingerprint, _cipherSuites);
			return new DtlsClientProtocol().Connect(client, _transportAdapter);
		}

		/// <summary>
		///     Thread-safe: this stack has at least three concurrent senders (SACKs from inside
		///     <see cref="OnDecrypted" /> on the receive thread, RTO retransmits on a tick thread,
		///     and application writes), and <see cref="DtlsRecordCrypto.EncryptRecord" /> mutates the
		///     send sequence with no synchronization of its own (see that class's own remarks). Serialized
		///     on <see cref="_sendGate" />, a lock of its own rather than <see cref="_gate" />, which the
		///     receive path does not hold across <see cref="OnDecrypted" /> at all (see this class's own
		///     Invariant remarks): sharing <see cref="_gate" /> here would still have serialized every
		///     send behind whatever a subscriber does in that callback.
		///     <para>
		///     Throws <see cref="InvalidOperationException" /> before the handshake has completed, rather
		///     than silently doing nothing: a caller sending before <see cref="_handshakeDone" /> (the
		///     <see cref="SctpAssociation" /> handing this straight to its constructor as <c>sendPacket</c>)
		///     would otherwise have its opening SCTP INIT dropped on the floor with no diagnostic at all,
		///     an undiagnosable hang rather than a fixable bug. <see cref="_handshakeDone" /> is set only
		///     after <see cref="_recordCrypto" /> is assigned in <see cref="DoHandshakeAsync" />, and both
		///     writes happen in that order on one thread with <see cref="_handshakeDone" /> volatile, so a
		///     caller observing it true here is guaranteed to see a non-null <see cref="_recordCrypto" />.
		///     </para>
		///     <para>
		///     Silently drops, rather than throwing, once <see cref="_disposed" /> is set, or once
		///     <see cref="_closed" /> alone is set without a full <see cref="Dispose" /> (an inbound
		///     fatal alert - see <see cref="ProcessApplicationDatagramLocked" />) -
		///     deliberately the opposite of the pre-handshake case above. The two are different in kind:
		///     sending before the handshake completes is a caller bug (there was never anything to race,
		///     the caller simply called this too early), while a send racing teardown is a benign,
		///     EXPECTED race inherent to this class's design - an application thread and a concurrent (or
		///     reentrant) <see cref="Dispose" /> call are both first-class, documented callers elsewhere in
		///     this class (see the class remarks' Invariant paragraph), so throwing here would turn an
		///     ordinary shutdown race into a caller-visible exception for code that did nothing wrong.
		///     Without the disposed guard, the method could call straight into a <see cref="DtlsRecordCrypto" />
		///     that <see cref="Dispose" /> is concurrently disposing on another thread, since
		///     <see cref="_sendGate" /> and <see cref="_gate" /> (which <see cref="Dispose" /> uses for the
		///     rest of its own teardown) are deliberately disjoint locks: <see cref="System.Security.Cryptography.AesGcm" />,
		///     which <see cref="DtlsRecordCrypto" /> disposes, is not documented safe against a concurrent
		///     encrypt call on the instance being disposed.
		///     </para>
		///     <para>
		///     The disposed guard and <see cref="Dispose" />'s own <c>_recordCrypto?.Dispose()</c>
		///     call are synchronized on <see cref="_sendGate" /> itself
		///     (chosen over a second, separate flag-only scheme: <see cref="_disposed" /> already exists
		///     and is set atomically, first thing, in <see cref="Dispose" />, before anything else -
		///     reusing it needs no new field). Lock-order proof: both the disposed check below and
		///     <see cref="Dispose" />'s teardown calls run inside a <c>lock (_sendGate)</c> critical
		///     section. Two call sites nest that <c>lock (_sendGate)</c> inside an already-held
		///     <c>lock (_gate)</c> - <see cref="Dispose" /> inside its own, and
		///     <see cref="ProcessApplicationDatagramLocked" />'s close_notify branch inside the one
		///     <see cref="FeedDatagram" />/<see cref="DrainUnderGate" /> already holds around the whole
		///     drain - and nothing in this class ever takes <see cref="_sendGate" /> first and then waits
		///     on <see cref="_gate" />, so the order is never reversed and neither nesting site introduces
		///     a lock-order cycle. Whichever of this method or one of those two reaches
		///     <see cref="_sendGate" /> first runs to completion
		///     before the other can start: if this method wins, the encrypt-and-send fully finishes before
		///     <see cref="Dispose" /> can even call <c>Dispose</c>/<c>Close</c>; if <see cref="Dispose" />
		///     wins, <see cref="_disposed" /> is already 1 (its <c>Interlocked.Exchange</c> at the very top
		///     of <see cref="Dispose" /> strictly precedes the <see cref="_sendGate" /> section) by the
		///     time this method's check ever runs, so <see cref="DtlsRecordCrypto.EncryptRecord" /> is
		///     never even attempted. Either way, encrypting and disposing can never execute concurrently on
		///     <see cref="_recordCrypto" />.
		///     </para>
		/// </summary>
		public void SendApplicationData(ReadOnlySpan<byte> payload)
		{
			if (!_handshakeDone) throw new InvalidOperationException("DtlsSession.SendApplicationData called before the handshake completed.");
			if (payload.Length > MaxSendPayloadLength) throw new ArgumentOutOfRangeException(nameof(payload), payload.Length, $"DtlsSession.SendApplicationData payload exceeds the {MaxSendPayloadLength}-byte SCTP packet ceiling.");

			lock (_sendGate)
			{
				if (Volatile.Read(ref _disposed) != 0) return;
				if (_closed) return;

				Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
				int length = _recordCrypto.EncryptRecord(ContentTypeApplicationData, payload, wire);
				if (length == -1)
				{
					// The only way this call site can produce -1: SctpPacket.MaxSize bounds payload well
					// under the 16-bit fragment length limit, and the destination above is always sized
					// exactly to fit, so the one remaining cause is the 48-bit send sequence (RFC 6347
					// 4.1) being exhausted. There is no renegotiation in this stack to recover from that,
					// so the association ends here rather than reusing a sequence number.
					Log.Warn($"DTLS send sequence exhausted; tearing down the association ({(_isServer ? "server" : "client")}).");
					RequestClose();
					return;
				}

				_sendToWire(wire.Slice(0, length));
			}
		}

		/// <summary>
		///     Emits at most one close_notify record (level warning, description 0) through the native
		///     record layer's own live send sequence - never through BouncyCastle, whose sequence stalled
		///     at wherever its Finished flight left off and would be dropped as a replay by any peer whose
		///     window our own application data has since advanced. Called both when this side initiates
		///     closure (<see cref="Dispose" />) and when it answers a peer's close_notify
		///     (<see cref="ProcessApplicationDatagramLocked" />), per RFC 5246 7.2.1's requirement that a
		///     close_notify recipient send one back before closing; either caller may run first, and
		///     whichever does is the only one that ever reaches the wire - <see cref="_closeNotifySent" />
		///     is set the instant a send is attempted, before the encrypt even runs, so an inbound
		///     close_notify's own response and a later <see cref="Dispose" /> on that same now-closed
		///     session never both put a record out: RFC 5246 7.2.1 requires nothing further on the wire
		///     once a close_notify has gone out, and a second one is exactly that. Must be called only
		///     while holding <see cref="_sendGate" />, which is also what <see cref="_closeNotifySent" />
		///     relies on for its own thread safety - both call sites already hold it. A no-op, silently,
		///     in every direction this can legitimately fail: already sent, <see cref="_recordCrypto" />
		///     still null (the handshake never completed, so there was never anything to say goodbye
		///     over), <see cref="DtlsRecordCrypto.EncryptRecord" /> returning -1 (the 48-bit send sequence
		///     is exhausted), or <see cref="WireSender" /> throwing (the peer's socket is gone; a prior
		///     ICMP port-unreachable can surface as a <see cref="System.Net.Sockets.SocketException" /> on
		///     the very next send). Every caller treats all of those as expected, not a reason to skip its
		///     own teardown, so the throwing case is swallowed here rather than left to unwind into
		///     <see cref="Dispose" /> or the receive path and skip whatever runs after it.
		/// </summary>
		private void TrySendCloseNotifyLocked()
		{
			if (_closeNotifySent || _recordCrypto == null) return;
			_closeNotifySent = true;

			Span<byte> body = stackalloc byte[2] {AlertLevelWarning, AlertDescriptionCloseNotify};
			Span<byte> wire = stackalloc byte[body.Length + DtlsRecordCrypto.RecordOverhead];
			int length = _recordCrypto.EncryptRecord(ContentTypeAlert, body, wire);
			if (length == -1) return;

			try
			{
				_sendToWire(wire.Slice(0, length));
			}
			catch (Exception ex)
			{
				Log.Debug($"Failed to send the closing close_notify ({(_isServer ? "server" : "client")}).", ex);
			}
		}

		/// <summary>
		///     BouncyCastle's own pull, reached only from <see cref="DatagramTransportAdapter.Receive(Span{byte},int)" />
		///     while <see cref="RunHandshake" /> is still on the stack: nothing calls this once the
		///     handshake has finished and BouncyCastle's transport has been dropped. Every "nothing
		///     available" exit funnels through <see cref="NoDataOrThrow" /> so a session closed mid-wait
		///     (<see cref="RequestClose" />) aborts the caller instead of returning an ordinary -1 for
		///     it to retry. Every datagram this method actually hands back clears
		///     <see cref="_finalFlightCache" /> first: BouncyCastle is about to process something it
		///     received, so whatever it sends from here on is a new flight, not a continuation of
		///     whatever was captured before this receive.
		///     <para>
		///     The zero-allocation read (<see cref="TryReadNow" />) is always attempted
		///     first, regardless of <paramref name="waitMillis" />. The bounded wait below is reached
		///     only when the queue is genuinely empty: BouncyCastle's own discard-retry case or the
		///     handshake's network-bound retransmission wait, both already rare/already-waiting-on-the-network, so
		///     paying for a timer there is acceptable. <see cref="CancellationTokenSource.TryReset" />
		///     was considered for reuse (verified against its Microsoft Learn documentation) but
		///     rejected: it is documented for "the sole owner... when the operation... has completed
		///     [and] no-one else will attempt to cancel it", explicitly warns reuse concurrently with a
		///     pending cancellation is not thread-safe, and does not itself rearm a new delay (a
		///     millisecond-delay <see cref="CancellationTokenSource" /> still needs <c>CancelAfter</c>
		///     after every reset): extra bookkeeping this rare branch does not need.
		///     </para>
		/// </summary>
		private int ReceiveFromQueue(Span<byte> buffer, int waitMillis)
		{
			if (TryReadNow(buffer, out int length))
			{
				_finalFlightCache.Clear();
				return length;
			}

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

			if (canRead && TryReadNow(buffer, out length))
			{
				_finalFlightCache.Clear();
				return length;
			}

			return NoDataOrThrow();
		}

		/// <summary>
		///     Checks <see cref="FeedDatagram" />'s direct-feed slot first. It is
		///     only ever live for the duration of one <see cref="FeedDatagram" /> call (see that
		///     method's doc comment), so consuming and clearing it here, the instant it is used, is what
		///     makes that guarantee hold: the caller has the bytes copied into its own buffer
		///     (<paramref name="buffer" />, owned by the caller further up this same synchronous call
		///     chain) before this method returns, so nothing outside this call ever observes the slot.
		///     <see cref="DrainQueueLocked" /> calls this with <see cref="_directFeedBuffer" /> itself as
		///     <paramref name="buffer" />, so the direct-feed branch below copies that buffer onto itself;
		///     deliberate and safe (<see cref="Span{T}.CopyTo(Span{T})" /> is a <c>memmove</c> and the
		///     source and destination ranges are identical), reusing the buffer as generic staging space
		///     for the raw, still-undecrypted bytes rather than adding a second one.
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
		///     Shared exit: a plain "nothing queued right now" is a normal -1 BouncyCastle
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
		///     BouncyCastle's own write path, reached only from <see cref="DatagramTransportAdapter.Send(ReadOnlySpan{byte})" />
		///     while <see cref="RunHandshake" /> is still on the stack - our own sends
		///     (<see cref="SendApplicationData" />, <see cref="TrySendCloseNotifyLocked" />,
		///     <see cref="HandleEpochZeroRecordLocked" />) call <see cref="_sendToWire" /> directly and
		///     never come through here. Drops the datagram once <see cref="_closed" /> is set instead of
		///     passing it through: a handshake can be cancelled, or the session disposed, while
		///     BouncyCastle is still mid-Accept/Connect on its own thread and about to call
		///     <c>transport.Close()</c> (see <see cref="DoHandshakeAsync" />'s raced-cancellation branch),
		///     which generates and sends one more record of its own on the way out - this is the one
		///     place able to catch that.
		///     <para>
		///     Every datagram actually forwarded is also appended to <see cref="_finalFlightCache" />,
		///     capped at <see cref="MaxFinalFlightDatagrams" /> (oldest dropped first): this is how the
		///     cache comes to hold our final flight once the handshake completes, since
		///     <see cref="ReceiveFromQueue" /> clears it on every inbound datagram BouncyCastle
		///     processes, leaving only what was sent since the last receive.
		///     </para>
		///     <para>
		///     The whole call chain, from <see cref="DtlsTransport.Send(ReadOnlySpan{byte})" /> down to
		///     <see cref="UdpMux.Send" />, is synchronous, so there is no reason to lease a copy just to
		///     hand out a <see cref="ReadOnlyMemory{T}" /> nobody keeps past the call; the copy taken here
		///     for <see cref="_finalFlightCache" /> is the one exception, since that cache does outlive
		///     the call.
		///     </para>
		/// </summary>
		private void SendToWire(ReadOnlySpan<byte> buffer)
		{
			if (_closed) return;

			if (_finalFlightCache.Count >= MaxFinalFlightDatagrams) _finalFlightCache.RemoveAt(0);
			_finalFlightCache.Add(buffer.ToArray());

			_sendToWire(buffer);
		}

		/// <summary>
		///     Shared unblock signal for <see cref="Dispose" />, a cancelled <see cref="DoHandshakeAsync" />,
		///     and <see cref="ProcessApplicationDatagramLocked" />'s close_notify/fatal-alert branches:
		///     marks the session closed and completes the inbound channel's writer, so any
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
		///     Returns every lease still queued in <see cref="_inbound" /> to the pool without processing
		///     it. <see cref="Dispose" /> always reaches this eventually, but a session closed by an
		///     inbound close_notify or fatal alert may never be disposed for a while (or at all, on a
		///     short-lived test session), so calling this immediately after <see cref="RequestClose" /> on
		///     that path is what actually reclaims those leases rather than leaving them pinned until
		///     something else happens to dispose the session. Must be called only while holding
		///     <see cref="_gate" /> - every call site already does, since <see cref="_inbound" /> is only
		///     ever safe to drain destructively under it (see the class remarks' Invariant paragraph).
		/// </summary>
		private void ReclaimAbandonedLeasesLocked()
		{
			while (_inbound.Reader.TryRead(out (byte[] Leased, int Length) item))
			{
				ArrayPool<byte>.Shared.Return(item.Leased);
			}
		}

		/// <summary>
		///     Marks the session closed (<see cref="RequestClose" />), unblocking a handshake still
		///     running on the <see cref="Task.Run" /> thread, then, under <see cref="_gate" />, sends a
		///     close_notify of our own (<see cref="TrySendCloseNotifyLocked" />, best effort - a no-op if
		///     the native layer was never established) and disposes the native record layer if the
		///     handshake reached that far, and returns every still-queued lease to the pool. Never closes
		///     BouncyCastle's transport: it was never retained past the handshake in the first place (see
		///     <see cref="RunHandshake" />'s own remarks), so there is nothing here to close, and nothing
		///     BC-originated ever touches the wire once the handshake is done. The gate excludes a concurrently running
		///     <see cref="FeedDatagram" /> drain called from another thread: either the
		///     drain finishes and releases the gate before this section runs, or this section runs
		///     first and the drain's own disposed check, repeated inside its own gate acquisition,
		///     bails out before touching the now-freed <see cref="_receiveScratch" />. A same-thread
		///     <em>reentrant</em> call (Dispose invoked synchronously from an <see cref="OnDecrypted" />
		///     subscriber) is different: the gate is already held by this same thread,
		///     so it does not block, and <see cref="_draining" /> being true means the drain loop is
		///     still further up this exact call stack, still about to touch <see cref="_receiveScratch" />
		///     once this call returns control to it. In that case the buffer is not returned here;
		///     <see cref="_deferredScratchReturn" /> is set instead, and <see cref="FeedDatagram" />'s
		///     own <c>finally</c> performs the return once the drain has actually stopped.
		///     <see cref="_directFeedBuffer" /> shares <see cref="_receiveScratch" />'s
		///     lifetime exactly, so it is returned, or deferred, alongside it on both branches.
		///     <para>
		///     <c>TrySendCloseNotifyLocked()</c> and <c>_recordCrypto.Dispose()</c> run inside one nested
		///     <c>lock (_sendGate)</c>, in that order (the close_notify needs <see cref="_recordCrypto" />
		///     still alive), the same lock
		///     <see cref="SendApplicationData" /> takes around its own disposed check and
		///     encrypt-and-send call - see that method's own remarks for the full
		///     lock-order proof, which also covers <see cref="ProcessApplicationDatagramLocked" />'s own
		///     <c>_gate</c>-then-<c>_sendGate</c> nesting for its close_notify response, the other site
		///     besides this one. Nested inside the pre-existing <c>lock (_gate)</c> here (an order,
		///     <see cref="_gate" /> then <see cref="_sendGate" />, never used in reverse anywhere in this
		///     class), so this adds no new lock-order cycle: <see cref="SendApplicationData" /> only ever
		///     takes <see cref="_sendGate" /> alone, never <see cref="_gate" />, so it cannot be waiting on
		///     this method to release <see cref="_gate" /> while this method waits on it for
		///     <see cref="_sendGate" />.
		///     </para>
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

			RequestClose();

			lock (_gate)
			{
				lock (_sendGate)
				{
					TrySendCloseNotifyLocked();
					_recordCrypto?.Dispose();

					// Only safe once _recordCrypto is disposed: it holds the two IV arrays here as its
					// live send/receive salts for as long as it runs (see CapturedDtlsKeys.Zero's own
					// remarks), and this same _sendGate section is what guarantees encrypting and
					// disposing/zeroing can never race (see SendApplicationData's own lock-order proof).
					_capturingCrypto?.Captured?.Zero();
				}

				ReclaimAbandonedLeasesLocked();

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