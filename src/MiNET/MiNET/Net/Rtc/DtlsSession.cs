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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MiNET.Net.Rtc.FastDtls;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     One DTLS 1.2 association securing a WebRTC data channel, pinned to a peer fingerprint
	///     exchanged out of band over SDP rather than a certificate chain. There is no SRTP key
	///     export. The handshake itself is driven by <see cref="DtlsEngine" />: every inbound
	///     handshake datagram reaches it synchronously through <see cref="FeedDatagram" />, every
	///     outbound one leaves synchronously through the engine's own transmit callback
	///     (<see cref="TransmitHandshakeDatagram" />), and <see cref="OnTick" /> drives its
	///     retransmission timer. No thread of this class's own ever blocks for the handshake.
	///     Once the handshake is done, <see cref="FeedDatagram" /> decrypts application data and
	///     alerts natively, in place, straight out of the caller's own span, via
	///     <see cref="_recordCrypto" /> and <see cref="ProcessApplicationDatagramLocked" />: the engine
	///     is out of the loop for application data entirely, though it stays alive (see
	///     <see cref="_engine" />'s remarks) to answer a peer that never received our final flight. A
	///     post-handshake datagram carrying an epoch-0 record - a peer retransmitting a final
	///     handshake flight it believes was lost - is handled natively too:
	///     <see cref="HandleEpochZeroRecordLocked" /> re-seeds the engine's epoch-1 counter forward to
	///     the record layer's own and asks it to rebuild and re-send its last flight, rate-limited, and
	///     drops the record itself without delivering it anywhere.
	///     <see cref="FeedDatagram" /> reads straight out of the caller's own buffer: the only copy
	///     on the receive path is the decrypt itself, ciphertext from the caller's span into
	///     <see cref="_receiveScratch" />. Nothing pools, leases, or queues a raw datagram.
	///     Invariant: <see cref="_gate" /> serializes every access to
	///     <see cref="_recordCrypto" />, <see cref="_receiveScratch" />, and <see cref="_engine" />
	///     between <see cref="FeedDatagram" /> and <see cref="Dispose" />, so a concurrent teardown from
	///     another thread can never return <see cref="_receiveScratch" /> to the pool while a call into
	///     <see cref="ProcessApplicationDatagramLocked" /> still holds a span over it - EXCEPT for the
	///     one deliberate window that method opens around each <see cref="OnDecrypted" /> call: the gate
	///     is explicitly released for that one call and reacquired right after, so a subscriber's own
	///     work never runs while blocking a concurrent <see cref="FeedDatagram" /> or
	///     <see cref="SendApplicationData" /> caller.
	///     <see cref="_processing" /> is what keeps the buffer-lifetime half of this invariant true
	///     across that window regardless: set <see langword="true" /> before the window ever opens and
	///     not cleared until the call has fully finished, so a <see cref="Dispose" /> that reaches
	///     <see cref="_gate" /> during the window - reentrant on this same thread (a subscriber calling
	///     <see cref="Dispose" /> synchronously from inside <see cref="OnDecrypted" />) or genuinely
	///     concurrent on another thread (only possible now that the gate is not held for the callback's
	///     whole duration) - always observes <see cref="_processing" /> true and defers
	///     <see cref="_receiveScratch" />'s return to <see cref="FeedDatagram" />'s own unwind instead
	///     of freeing it out from under the call still running further up the call stack (or, in the
	///     concurrent case, on another thread entirely).
	///     <see cref="_processing" /> also guards against a second call reaching the same shared state
	///     while the first is still in flight: a call to <see cref="FeedDatagram" /> that finds it
	///     already set - whether reentrant on this same thread from inside an <see cref="OnDecrypted" />
	///     subscriber, or, in principle, a genuinely concurrent caller on another thread arriving during
	///     that same window - is dropped and counted (<see cref="ReentrantFeedsDropped" />) rather than
	///     touching <see cref="_receiveScratch" /> or <see cref="_engine" /> a second time. This is a
	///     fail-safe for a programming error, not a queue: the production topology is a single receive
	///     loop per <see cref="UdpMux" /> feeding one datagram at a time, so the guard is never expected
	///     to fire outside a subscriber that reenters this session from its own callback.
	/// </summary>
	public sealed class DtlsSession : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DtlsSession));

		// 1500 (typical path MTU) - 20 (IPv4) - 8 (UDP) = 1472. The scratch buffer is sized above
		// that to accommodate more than one plaintext record per receive.
		private const int WireMtu = 1472;
		private const int ScratchBufferSize = 4096;

		// 300ms retransmission cadence over a 10ms host tick (UdpMux.TickIntervalMs): see OnTick.
		private const int TicksPerRetransmit = 30;

		/// <summary>
		///     <see cref="ReadOnlyMemory{T}" />, not <see cref="ReadOnlySpan{T}" />, because the
		///     receive pipeline this feeds (<see cref="SctpAssociation.OnPacketReceived" />, ultimately
		///     <see cref="SctpAssociation.OnMessage" />) delivers a <see cref="System.Buffers.ReadOnlySequence{T}" />
		///     end to end, and a sequence cannot wrap a span. <paramref name="payload" /> is still only
		///     OWNED by the subscriber: it is a slice of a buffer rented from <see cref="ArrayPool{T}.Shared" />
		///     for this one datagram, so it may cross threads freely, and the owner returns the backing
		///     array (<see cref="System.Runtime.InteropServices.MemoryMarshal.TryGetArray{T}" />) when
		///     done; not returning it only costs pool efficiency, never correctness. It is NOT a slice of <see cref="_receiveScratch" />, our
		///     own pooled array, unchanged from when this was a span.
		/// </summary>
		public delegate void DecryptedHandler(ReadOnlyMemory<byte> payload);

		/// <summary>
		///     Takes the outgoing datagram as a span, not a <see cref="ReadOnlyMemory{T}" />,
		///     since the whole call chain down to <see cref="UdpMux.Send" /> is synchronous. Nothing here
		///     needs a heap reference to the datagram after the call returns, so a
		///     <see cref="ReadOnlyMemory{T}" /> parameter would force an intermediate ArrayPool lease
		///     (rent, copy the span into it, hand out the memory, return the lease) for no reason: one
		///     full copy and one pool round trip per outgoing datagram.
		/// </summary>
		public delegate void WireSender(ReadOnlySpan<byte> datagram);

		public event DecryptedHandler OnDecrypted;

		private readonly RtcCertificate _localCertificate;
		private readonly bool _isServer;
		private readonly WireSender _sendToWire;
		private readonly byte[] _receiveScratch = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);
		private readonly object _gate = new object();

		// Deliberately separate from _gate: the receive path releases _gate around OnDecrypted, so a
		// subscriber can call SendApplicationData from inside that callback while another thread is
		// concurrently sending. Serializing SendApplicationData on _gate instead would deadlock that
		// case. This lock only ever needs to exclude concurrent callers of SendApplicationData itself
		// from each other.
		private readonly object _sendGate = new object();

		// Owns the whole handshake; not thread-safe on its own, so every call to it below is made
		// under _gate, including retransmission via Retransmit() post-establishment (reached only from
		// HandleEpochZeroRecordLocked, itself reached only from FeedDatagram's lock (_gate)); that path
		// is additionally serialized on _sendGate, alongside the record layer's own send sequence it
		// shares a key with. Kept alive for the session's whole lifetime, not just until the handshake
		// completes: a peer that never received our final flight still needs it to rebuild and re-send
		// that flight. Disposed only from Dispose.
		private readonly DtlsEngine _engine;
		private readonly TaskCompletionSource<bool> _handshakeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// Guards DoHandshakeAsync against a second call reaching _engine.Start() a second time (see
		// that method's own remarks): 0 until the first call, exchanged to 1 there, checked with
		// Interlocked so two overlapping first calls from different threads still only ever start the
		// engine once.
		private int _handshakeStarted;
		private int _tickCount;
		private long _handshakeFailures;

		private DtlsRecordCrypto _recordCrypto;
		private volatile bool _handshakeDone;
		private volatile bool _closed;
		private int _disposed;
		private long _droppedRecords;
		private long _reentrantFeedsDropped;

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
		///     Test visibility only (assembly's InternalsVisibleTo to MiNETTests): a copy of the key
		///     material <see cref="_engine" /> negotiated, taken the instant <see cref="EstablishRecordLayerLocked" />
		///     builds <see cref="_recordCrypto" /> from it - before the engine's own copy is zeroed (see
		///     that method's remarks). <see langword="null" /> until the handshake completes. A separate
		///     copy, not a view onto the engine's own <see cref="DtlsEngine.Keys" />, exists solely so a
		///     test can build an independent <see cref="DtlsRecordCrypto" /> against the same key
		///     material after that zeroing has already happened.
		/// </summary>
		internal DtlsNegotiatedKeys CapturedKeys { get; private set; }

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): the native record layer built from this session's negotiated keys once the handshake completes, null before that.</summary>
		internal DtlsRecordCrypto RecordCrypto => _recordCrypto;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): the handshake engine's own next epoch-1 send sequence, live - proves <see cref="RecordCrypto" /> was seeded from exactly this value at establishment, not from 0.</summary>
		internal ulong EngineNextEpoch1SendSequence => _engine.NextEpoch1SendSequence;

		/// <summary>Records at epoch 1 that were deliberately never acted on: a content type other than application-data or alert, or a non-fatal alert whose description is not close_notify. Not the same as <see cref="DtlsRecordCrypto" />'s own malformed/replay/decrypt-failure counters, and not the epoch-0 counters below.</summary>
		internal long DroppedRecords => Interlocked.Read(ref _droppedRecords);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many times an epoch-0 record triggered a re-send of the handshake engine's last flight.</summary>
		internal long ResendsPerformed => Interlocked.Read(ref _resendsPerformed);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many epoch-0 records were dropped without a resend, because the 1-second resend rate limit was still active, a resend already answered this same datagram, or the session was already closed.</summary>
		internal long EpochZeroRecordsDropped => Interlocked.Read(ref _epochZeroRecordsDropped);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many inbound datagrams the handshake engine rejected with a fatal alert of its own, before the handshake completed (a bad fingerprint, a bad signature, a malformed message). Never set once the handshake has completed.</summary>
		internal long HandshakeFailures => Interlocked.Read(ref _handshakeFailures);

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): whether this session has been torn down, by either <see cref="Dispose" /> or an alert-driven <see cref="RequestClose" />. <see cref="RtcPeer" /> exposes this to interop tests as <c>DtlsSessionClosed</c>.</summary>
		internal bool IsClosed => _closed;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many datagrams <see cref="FeedDatagram" /> dropped because a call into it was already in progress on this session - a reentrant call from inside an <see cref="OnDecrypted" /> subscriber, or, in principle, a genuinely concurrent caller arriving while another call's callback window is open. Neither case ever delivers or touches session-owned scratch state.</summary>
		internal long ReentrantFeedsDropped => Interlocked.Read(ref _reentrantFeedsDropped);

		// Both touched only while holding _gate (see the class doc comment's Invariant paragraph),
		// so neither needs volatile or Interlocked: _gate's acquire/release already provides the
		// necessary visibility across the two threads that can ever reach either field.
		private bool _processing;
		private bool _deferredScratchReturn;

		public DtlsSession(RtcCertificate localCertificate, string expectedRemoteFingerprint, bool isServer, WireSender sendToWire)
		{
			_localCertificate = localCertificate;
			_isServer = isServer;
			_sendToWire = sendToWire;

			byte[] expectedFingerprint = expectedRemoteFingerprint == null ? null : Convert.FromHexString(expectedRemoteFingerprint.Replace(":", ""));
			_engine = new DtlsEngine(!isServer, localCertificate.DtlsCertificate, TransmitHandshakeDatagram, WireMtu, expectedFingerprint);
		}

		/// <summary>
		///     Feeds one raw datagram demuxed as DTLS by <see cref="IceSession.OnDtlsDatagram" />,
		///     decrypting straight out of <paramref name="datagram" /> with no intermediate copy or
		///     queue: the caller's buffer is borrowed only for the synchronous duration of this call, and
		///     any plaintext this call produces lands in <see cref="_receiveScratch" />, session-owned
		///     scratch, before the call returns.
		///     <para>
		///     Before the handshake completes this hands <paramref name="datagram" /> straight to
		///     <see cref="_engine" /> (see <see cref="HandleHandshakeDatagramLocked" />): the engine
		///     consumes it synchronously and never blocks a thread waiting for one, and this branch is
		///     naturally, harmlessly reentrant on <see cref="_gate" /> - the engine's own transmit
		///     callback can synchronously trigger the peer's own reply back into this same call stack (a
		///     loopback wiring, as every handshake test in this assembly uses), and <see cref="Monitor" />
		///     is reentrant for the thread that already owns it. Datagrams that arrive before
		///     <see cref="DoHandshakeAsync" /> has even been called are not lost either: <see cref="_engine" />
		///     already exists by the time this constructor returns, so there is always somewhere for them
		///     to go.
		///     </para>
		///     <para>
		///     Once the handshake is done, <see cref="ProcessApplicationDatagramLocked" /> walks
		///     <paramref name="datagram" />'s records natively; an epoch-0 record within it (a peer
		///     retransmitting a final handshake flight it believes was lost) is handled there too, by
		///     re-sending the engine's last flight rather than delivering the record anywhere (see that
		///     method's own remarks). Unlike the handshake branch, this one is never expected to reenter:
		///     <see cref="OnDecrypted" /> is a real delivery callback a subscriber's own code runs inside,
		///     so a call that finds <see cref="_processing" /> already set - reentrant on this same thread
		///     from inside that subscriber, or, in principle, a genuinely concurrent caller on another
		///     thread arriving during that same callback's window - is dropped and counted
		///     (<see cref="ReentrantFeedsDropped" />) rather than touching <see cref="_receiveScratch" />
		///     a second time (see the class doc comment's Invariant paragraph for why that window can be
		///     open at all). Both branches run under <see cref="_gate" /> so a concurrent
		///     <see cref="Dispose" /> can never free <see cref="_receiveScratch" /> or dispose
		///     <see cref="_recordCrypto" /> out from under this call (the disposed check alone is
		///     check-then-act, not a real exclusion).
		///     </para>
		/// </summary>
		public void FeedDatagram(ReadOnlySpan<byte> datagram)
		{
			if (Volatile.Read(ref _disposed) != 0) return;
			if (_closed) return;

			lock (_gate)
			{
				if (Volatile.Read(ref _disposed) != 0 || _closed) return;

				if (!_handshakeDone)
				{
					HandleHandshakeDatagramLocked(datagram);
					return;
				}

				if (_processing)
				{
					Interlocked.Increment(ref _reentrantFeedsDropped);
					return;
				}

				_processing = true;
				try
				{
					ProcessApplicationDatagramLocked(datagram);
				}
				finally
				{
					_processing = false;
					if (_deferredScratchReturn)
					{
						_deferredScratchReturn = false;
						ArrayPool<byte>.Shared.Return(_receiveScratch);
					}
				}
			}
		}

		/// <summary>
		///     Hands one datagram to <see cref="_engine" /> before the handshake has completed. Must be
		///     called only while already holding <see cref="_gate" />. Catches <see cref="Exception" />,
		///     not only <see cref="DtlsHandshakeException" />: the engine's own signal for a message that
		///     fails the handshake outright (a bad fingerprint, a bad signature, a malformed message, a
		///     fatal alert from the peer) is <see cref="DtlsHandshakeException" />, but a well-formed
		///     message carrying content that is invalid one layer deeper - an off-curve ECDHE point, a
		///     signature the platform's own crypto rejects by throwing rather than returning false - can
		///     surface as a different, platform-dependent exception type from underneath the engine. Both
		///     cases get the identical treatment: counted, logged at Info - a handshake failure against a
		///     hostile or broken peer is normal server life, not a defect - and closed through
		///     <see cref="RequestClose" />. Never rethrown: an unhandled throw on this receive path is
		///     exactly the class of defect this project treats as a hard stop, and a malformed or
		///     adversarial handshake message is exactly the input a real network condition will
		///     eventually deliver.
		///     <see cref="EstablishRecordLayerLocked" /> runs right here, still under <see cref="_gate" />,
		///     the instant <see cref="DtlsEngine.IsComplete" /> is first observed true: no other caller
		///     can observe a half-published <see cref="_recordCrypto" />, since every reader of it either
		///     takes <see cref="_gate" /> itself or reads the volatile <see cref="_handshakeDone" /> this
		///     method sets only after <see cref="_recordCrypto" /> is fully constructed.
		/// </summary>
		private void HandleHandshakeDatagramLocked(ReadOnlySpan<byte> datagram)
		{
			try
			{
				_engine.HandleDatagram(datagram);
			}
			catch (Exception e)
			{
				Interlocked.Increment(ref _handshakeFailures);
				Log.Info($"DTLS handshake failed ({(_isServer ? "server" : "client")}): {e.Message}");
				RequestClose();
				return;
			}

			if (!_handshakeDone && _engine.IsComplete)
			{
				EstablishRecordLayerLocked();
			}
		}

		/// <summary>
		///     Builds <see cref="_recordCrypto" /> from <see cref="DtlsEngine.Keys" /> at the exact seed
		///     <see cref="DtlsEngine.NextEpoch1SendSequence" />: the engine's own Finished flight (and any
		///     retransmission of it) already consumed epoch-1 sequences under this same key, so the
		///     record layer must continue from exactly where the engine left off, never from 0. Takes its
		///     own copy for <see cref="CapturedKeys" /> before zeroing the engine's arrays: the engine's
		///     own ciphers already hold their key schedules internally and stay retransmit-capable
		///     without the raw bytes, and <see cref="DtlsRecordCrypto" />'s constructor takes its own
		///     copy too, so nothing needs this raw key material to survive past this method except a
		///     test wanting to forge an independent record layer against the same keys. Must be called
		///     only while already holding <see cref="_gate" />, and only once: <see cref="DtlsEngine.IsComplete" />
		///     is a one-way transition, so the caller's own <c>!_handshakeDone</c> guard already ensures
		///     this never runs twice.
		/// </summary>
		private void EstablishRecordLayerLocked()
		{
			DtlsNegotiatedKeys keys = _engine.Keys;
			ulong seed = _engine.NextEpoch1SendSequence;

			_recordCrypto = new DtlsRecordCrypto(keys, _isServer, seed);
			CapturedKeys = CopyKeysForTesting(keys);

			CryptographicOperations.ZeroMemory(keys.ClientWriteKey);
			CryptographicOperations.ZeroMemory(keys.ServerWriteKey);
			CryptographicOperations.ZeroMemory(keys.ClientWriteSalt);
			CryptographicOperations.ZeroMemory(keys.ServerWriteSalt);

			_handshakeDone = true;
			_handshakeCompletion.TrySetResult(true);
		}

		private static DtlsNegotiatedKeys CopyKeysForTesting(DtlsNegotiatedKeys source)
		{
			var copy = new DtlsNegotiatedKeys();
			source.ClientWriteKey.CopyTo(copy.ClientWriteKey, 0);
			source.ServerWriteKey.CopyTo(copy.ServerWriteKey, 0);
			source.ClientWriteSalt.CopyTo(copy.ClientWriteSalt, 0);
			source.ServerWriteSalt.CopyTo(copy.ServerWriteSalt, 0);
			return copy;
		}

		/// <summary>
		///     Decrypts every record of one post-handshake datagram, natively, record by record, straight
		///     out of <paramref name="datagram" /> - a slice of the caller's own buffer, valid only for
		///     this synchronous call. Called only from <see cref="FeedDatagram" />, under
		///     <see cref="_gate" />, with <see cref="_processing" /> already set: that is what guarantees
		///     only one call is ever inside this method at a time (it decrypts into the shared
		///     <see cref="_receiveScratch" /> buffer, so two overlapping calls would corrupt each other's
		///     plaintext out from under a concurrent <see cref="OnDecrypted" /> delivery - exactly the
		///     failure mode <see cref="_processing" /> exists to rule out, and which <see cref="FeedDatagram" />'s
		///     own doc comment explains in full). Releases <see cref="_gate" /> around each
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
					// The plaintext lands directly in a pool-rented buffer the SUBSCRIBER then owns
					// (the delegate's contract): the cipher writes into the receiving stage's memory
					// the same way the kernel writes into the receive buffer handed to it, so a
					// subscriber that hands the datagram to another thread (a per-session lane) does
					// so with zero copies. Steady state stays GC-free as long as owners return the
					// buffer to ArrayPool.Shared.
					byte[] rented = ArrayPool<byte>.Shared.Rent(fragmentLength);
					if (!_recordCrypto.TryDecryptRecord(record, rented, out _, out int length))
					{
						ArrayPool<byte>.Shared.Return(rented);
						continue;
					}

					Monitor.Exit(_gate);
					try
					{
						OnDecrypted?.Invoke(rented.AsMemory(0, length));
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
					// rides the native record layer's live sequence counter (EncryptRecord, not the
					// engine's own), so a peer whose replay window our own application data has already
					// advanced still accepts it.
					if (description == AlertDescriptionCloseNotify)
					{
						lock (_sendGate)
						{
							TrySendCloseNotifyLocked();
						}

						RequestClose();
						return;
					}

					// RFC 5246 7.2: any fatal-level alert ends the connection immediately; unlike
					// close_notify, no response is expected or sent.
					if (level == AlertLevelFatal)
					{
						RequestClose();
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
		///     re-send of the handshake engine's last flight. Drop-and-count, no resend, when the
		///     1-second rate limit (<see cref="ResendRateLimitMillis" />, read through
		///     <see cref="ClockNowMillis" />) is still active, when <paramref name="resentThisDatagram" />
		///     is already <see langword="true" /> (a resend already answered an earlier epoch-0 record in
		///     this same datagram, and answering twice for one datagram buys the peer nothing further),
		///     or when <see cref="_closed" /> is set: <see cref="RequestClose" /> (<see cref="Dispose" />,
		///     or a concurrent alert-driven close) can flip it true from another thread without ever
		///     needing <see cref="_gate" /> itself, so a call already inside this method can still
		///     observe it turn true out from under it. Re-checked here, immediately before the resend
		///     would actually reach the wire, for the same reason <see cref="SendApplicationData" /> and
		///     <see cref="TransmitHandshakeDatagram" /> both check it at their own last possible moment:
		///     RFC 5246 7.2.1 requires nothing further on the wire once closed, and a resent handshake
		///     flight is no exception.
		///     <para>
		///     The single-owner invariant that keeps this safe: <see cref="_engine" /> and
		///     <see cref="_recordCrypto" /> protect records under the same AES-GCM key (the engine's
		///     Finished flight and everything the record layer sends afterward share epoch 1), so handing
		///     out the same (epoch, sequence) pair twice would be a nonce reuse, a cryptographic break.
		///     Both counters only ever move forward, and both moves happen here, under
		///     <see cref="_sendGate" /> - the same lock <see cref="SendApplicationData" /> and
		///     <see cref="TrySendCloseNotifyLocked" /> already serialize on - so no sequence the record
		///     layer might concurrently be allocating for an application-data send can ever be handed out
		///     a second time by this resend, or vice versa.
		///     </para>
		/// </summary>
		private void HandleEpochZeroRecordLocked(ref bool resentThisDatagram)
		{
			if (resentThisDatagram)
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

			lock (_sendGate)
			{
				if (_closed)
				{
					Interlocked.Increment(ref _epochZeroRecordsDropped);
					return;
				}

				_lastResendAtMillis = now;
				resentThisDatagram = true;

				_engine.SeedEpoch1SendSequence(_recordCrypto.NextSendSequence);
				_engine.Retransmit(); // rebuilds the flight; its transmit callback already try/catches per datagram
				_recordCrypto.SeedSendSequenceForward(_engine.NextEpoch1SendSequence);

				Interlocked.Increment(ref _resendsPerformed);
			}
		}

		/// <summary>
		///     Registers <paramref name="cancellationToken" />'s teardown (see <see cref="RequestClose" />)
		///     and, for the client role, starts the handshake by calling <see cref="DtlsEngine.Start" />
		///     under <see cref="_gate" />: this call never blocks a thread, since <see cref="_engine" />
		///     is push-driven end to end (see the class remarks); its own outgoing datagram reaches the
		///     wire inline, through <see cref="TransmitHandshakeDatagram" />. The server role does nothing
		///     active here - it already answers whatever the
		///     client sends via <see cref="FeedDatagram" />, which works regardless of whether this method
		///     has even been called (<see cref="_engine" /> exists from construction).
		///     <para>
		///     A second or later call returns the same <see cref="_handshakeCompletion" /> task without
		///     touching <see cref="_engine" /> again: calling <see cref="DtlsEngine.Start" /> a second time
		///     throws <see cref="InvalidOperationException" /> from the engine itself
		///     ("Already started."), an unhelpful diagnostic for a caller of this class to see, and there
		///     is nothing meaningful for a second, redundant call to do once the handshake is already
		///     under way - the first caller's own <paramref name="cancellationToken" /> already governs
		///     cancellation, so a different token passed to a later call is not wired to anything.
		///     </para>
		///     <para>
		///     Resolves <see langword="false" />, never throws, on any handshake failure: a bad
		///     fingerprint or a malformed peer message reaches <see cref="_handshakeCompletion" /> through
		///     <see cref="HandleHandshakeDatagramLocked" />'s own catch calling <see cref="RequestClose" />,
		///     which resolves it there; cancellation reaches it the same way, through the registration
		///     below, disposed once the handshake settles (whichever of completion, failure, or
		///     cancellation reaches <see cref="_handshakeCompletion" /> first) rather than left to leak for
		///     the life of <paramref name="cancellationToken" />'s own source.
		///     <see cref="TaskCompletionSource{TResult}.TrySetResult" /> is idempotent, so whichever of
		///     those three reaches <see cref="_handshakeCompletion" /> first is the result every caller of
		///     this method observes; nothing here needs to race the registration against an in-flight
		///     blocking call the way the previous design did, because nothing in this design ever blocks
		///     one.
		///     </para>
		/// </summary>
		public Task<bool> DoHandshakeAsync(CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref _handshakeStarted, 1) != 0) return _handshakeCompletion.Task;

			CancellationTokenRegistration registration = cancellationToken.CanBeCanceled
				? cancellationToken.Register(RequestClose)
				: default;
			_handshakeCompletion.Task.ContinueWith(_ => registration.Dispose(), CancellationToken.None);

			lock (_gate)
			{
				if (Volatile.Read(ref _disposed) != 0 || _closed)
				{
					_handshakeCompletion.TrySetResult(false);
				}
				else if (!_isServer)
				{
					_engine.Start();
				}
			}

			return _handshakeCompletion.Task;
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
		///     after <see cref="_recordCrypto" /> is assigned in <see cref="EstablishRecordLayerLocked" />,
		///     and both writes happen in that order on one thread with <see cref="_handshakeDone" />
		///     volatile, so a caller observing it true here is guaranteed to see a non-null
		///     <see cref="_recordCrypto" />.
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
		///     <c>lock (_gate)</c> - <see cref="Dispose" /> inside its own,
		///     <see cref="ProcessApplicationDatagramLocked" />'s close_notify branch inside the one
		///     <see cref="FeedDatagram" /> already holds around the whole call, and
		///     <see cref="HandleEpochZeroRecordLocked" /> the same way - and nothing in this
		///     class ever takes <see cref="_sendGate" /> first and then waits on <see cref="_gate" />, so
		///     the order is never reversed and none of those nesting sites introduces a lock-order cycle.
		///     Whichever of this method or one of those reaches <see cref="_sendGate" /> first runs to
		///     completion before the other can start: if this method wins, the encrypt-and-send fully
		///     finishes before <see cref="Dispose" /> can even call <c>Dispose</c> on
		///     <see cref="_recordCrypto" />; if <see cref="Dispose" /> wins, <see cref="_disposed" /> is
		///     already 1 (its <c>Interlocked.Exchange</c> at the very top of <see cref="Dispose" />
		///     strictly precedes the <see cref="_sendGate" /> section) by the time this method's check
		///     ever runs, so <see cref="DtlsRecordCrypto.EncryptRecord" /> is never even attempted. Either
		///     way, encrypting and disposing can never execute concurrently on <see cref="_recordCrypto" />.
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
		///     record layer's own live send sequence - never through the handshake engine, whose flight
		///     was built for the handshake itself, not for an arbitrary later alert. Called both when
		///     this side initiates closure (<see cref="Dispose" />) and when it answers a peer's
		///     close_notify (<see cref="ProcessApplicationDatagramLocked" />), per RFC 5246 7.2.1's
		///     requirement that a close_notify recipient send one back before closing; either caller may
		///     run first, and whichever does is the only one that ever reaches the wire -
		///     <see cref="_closeNotifySent" /> is set the instant a send is attempted, before the encrypt
		///     even runs, so an inbound close_notify's own response and a later <see cref="Dispose" /> on
		///     that same now-closed session never both put a record out: RFC 5246 7.2.1 requires nothing
		///     further on the wire once a close_notify has gone out, and a second one is exactly that.
		///     Must be called only while holding <see cref="_sendGate" />, which is also what
		///     <see cref="_closeNotifySent" /> relies on for its own thread safety - both call sites
		///     already hold it. A no-op, silently, in every direction this can legitimately fail: already
		///     sent, <see cref="_recordCrypto" /> still null (the handshake never completed, so there was
		///     never anything to say goodbye over), <see cref="DtlsRecordCrypto.EncryptRecord" /> returning
		///     -1 (the 48-bit send sequence is exhausted), or <see cref="WireSender" /> throwing (the
		///     peer's socket is gone; a prior ICMP port-unreachable can surface as a
		///     <see cref="System.Net.Sockets.SocketException" /> on the very next send). Every caller
		///     treats all of those as expected, not a reason to skip its own teardown, so the throwing
		///     case is swallowed here rather than left to unwind into <see cref="Dispose" /> or the
		///     receive path and skip whatever runs after it.
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
		///     The handshake engine's transmit callback (passed to its constructor): wraps the existing
		///     wire-send with the same per-datagram try/catch shape used elsewhere on this send path (see
		///     <see cref="HandleEpochZeroRecordLocked" /> and <see cref="TrySendCloseNotifyLocked" />) -
		///     a lost handshake datagram is droppable by nature (the peer's own retransmission logic, or
		///     ours via <see cref="OnTick" />, covers it), so one failed send must never abort the flight
		///     still being built, or unwind out of whichever of <see cref="DoHandshakeAsync" />,
		///     <see cref="FeedDatagram" />, or <see cref="OnTick" /> is currently on the stack calling
		///     into <see cref="_engine" />. Sends inline, synchronously: safe against a host whose
		///     <see cref="WireSender" /> is not itself asynchronous and delivers the peer's reply back
		///     into this same call chain, because <see cref="DtlsEngine.TransmitFlight" /> only ever calls
		///     this once its own flight-building state has fully settled (see that method's own remarks),
		///     never mid-build. Drops the datagram once <see cref="_closed" /> is set instead of sending
		///     it: a handshake can be cancelled, or the session disposed, from another thread while a call
		///     into <see cref="_engine" /> is still building a flight.
		/// </summary>
		private void TransmitHandshakeDatagram(byte[] datagram)
		{
			if (_closed) return;

			try
			{
				_sendToWire(datagram);
			}
			catch (Exception ex)
			{
				Log.Debug($"Failed to send a DTLS handshake datagram ({(_isServer ? "server" : "client")}).", ex);
			}
		}

		/// <summary>
		///     Drives handshake retransmission from the host's tick (<see cref="UdpMux.OnTick" />, wired
		///     by <see cref="RtcPeer" /> alongside <see cref="SctpAssociation.OnTick" />): counts ticks
		///     rather than using a timer of its own, and calls <see cref="DtlsEngine.OnTimeout" /> once
		///     every <see cref="TicksPerRetransmit" /> of them, a 300ms cadence over the mux's 10ms tick.
		///     A no-op once the handshake has completed or the session is closed - checked both before
		///     and, cheaply, after acquiring <see cref="_gate" />, since a concurrent completion or
		///     teardown between the two is otherwise possible - so a completed handshake never pays for
		///     the lock on every tick for the rest of the session's life.
		/// </summary>
		public void OnTick()
		{
			if (_handshakeDone || _closed) return;

			lock (_gate)
			{
				if (_handshakeDone || _closed || Volatile.Read(ref _disposed) != 0) return;

				if (++_tickCount < TicksPerRetransmit) return;
				_tickCount = 0;

				_engine.OnTimeout();
			}
		}

		/// <summary>
		///     Shared unblock signal for <see cref="Dispose" />, a cancelled <see cref="DoHandshakeAsync" />,
		///     a handshake failure (<see cref="HandleHandshakeDatagramLocked" />), and
		///     <see cref="ProcessApplicationDatagramLocked" />'s close_notify/fatal-alert branches: marks
		///     the session closed and resolves <see cref="_handshakeCompletion" /> to <see langword="false" />
		///     if nothing has resolved it yet (a no-op, via <see cref="TaskCompletionSource{TResult}.TrySetResult" />,
		///     once the handshake has already completed successfully). Idempotent in every other respect
		///     too: setting an already-<see langword="true" /> <see cref="_closed" /> is harmless.
		/// </summary>
		private void RequestClose()
		{
			_closed = true;
			_handshakeCompletion.TrySetResult(false);
		}

		/// <summary>
		///     Marks the session closed (<see cref="RequestClose" />), then, under <see cref="_gate" />,
		///     sends a close_notify of our own (<see cref="TrySendCloseNotifyLocked" />, best effort - a
		///     no-op if the native layer was never established) and disposes the native record layer and
		///     the handshake engine if the handshake reached that far. Never disposes
		///     <see cref="_localCertificate" />'s own <see cref="FastDtls.DtlsCertificate" />: ownership of
		///     the certificate stays with whoever constructed the <see cref="RtcCertificate" /> and passed
		///     it into this session's constructor, not with the session itself, since one certificate is
		///     the normal WebRTC shape for a server across many peers, and each peer's own
		///     <see cref="DtlsSession" /> disposing it would break every other peer still using it (or
		///     about to). The gate excludes a concurrently running <see cref="FeedDatagram" /> call on
		///     another thread: either that call finishes and releases the gate before this section runs,
		///     or this section runs first and the other call's own disposed check, repeated inside its own
		///     gate acquisition, bails out before touching the now-freed <see cref="_receiveScratch" />. A
		///     same-thread <em>reentrant</em> call (Dispose invoked synchronously from an
		///     <see cref="OnDecrypted" /> subscriber) is different: the receive path releases
		///     <see cref="_gate" /> around that callback, so this call acquires it fresh rather than
		///     blocking on itself, and what actually keeps the buffer safe is <see cref="_processing" />
		///     being true, which means <see cref="ProcessApplicationDatagramLocked" />'s walk is still
		///     further up this exact call stack, still about to touch <see cref="_receiveScratch" /> once
		///     this call returns control to it. In that case the buffer is not returned here;
		///     <see cref="_deferredScratchReturn" /> is set instead, and <see cref="FeedDatagram" />'s own
		///     <c>finally</c> performs the return once that call has actually finished.
		///     <para>
		///     <c>TrySendCloseNotifyLocked()</c>, <c>_recordCrypto.Dispose()</c>, and
		///     <c>_engine.Dispose()</c> run inside one nested <c>lock (_sendGate)</c>, in that order (the
		///     close_notify needs <see cref="_recordCrypto" /> still alive), the same lock
		///     <see cref="SendApplicationData" /> takes around its own disposed check and
		///     encrypt-and-send call - see that method's own remarks for the full
		///     lock-order proof, which also covers <see cref="ProcessApplicationDatagramLocked" />'s own
		///     <c>_gate</c>-then-<c>_sendGate</c> nesting for its close_notify response, and
		///     <see cref="HandleEpochZeroRecordLocked" />'s identical nesting for its resend. Nested
		///     inside the pre-existing <c>lock (_gate)</c> here (an order, <see cref="_gate" /> then
		///     <see cref="_sendGate" />, never used in reverse anywhere in this class), so this adds no
		///     new lock-order cycle: <see cref="SendApplicationData" /> only ever takes
		///     <see cref="_sendGate" /> alone, never <see cref="_gate" />, so it cannot be waiting on this
		///     method to release <see cref="_gate" /> while this method waits on it for
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
					_engine.Dispose();

					if (CapturedKeys != null)
					{
						CryptographicOperations.ZeroMemory(CapturedKeys.ClientWriteKey);
						CryptographicOperations.ZeroMemory(CapturedKeys.ServerWriteKey);
						CryptographicOperations.ZeroMemory(CapturedKeys.ClientWriteSalt);
						CryptographicOperations.ZeroMemory(CapturedKeys.ServerWriteSalt);
					}
				}

				if (_processing)
				{
					_deferredScratchReturn = true;
				}
				else
				{
					ArrayPool<byte>.Shared.Return(_receiveScratch);
				}
			}
		}
	}
}