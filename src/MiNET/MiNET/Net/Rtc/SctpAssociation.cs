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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using log4net;

namespace MiNET.Net.Rtc
{
	public enum SctpState
	{
		Closed,
		CookieWait,
		CookieEchoed,
		Established,
		Aborted
	}

	/// <summary>
	///     A fully built outbound SCTP packet, ready for the wire (or the loopback wiring a test uses
	///     instead of one). Matches <see cref="DtlsSession.SendApplicationData" />'s signature, so
	///     Task 7 wires this delegate straight to it.
	///     <para>
	///     Leaf contract: <see cref="SctpAssociation" /> calls this delegate while holding its own
	///     internal gate (Task 5's retransmit machinery wants sends interleaved with state under that
	///     lock). The delegate must therefore behave like <see cref="DtlsSession.SendApplicationData" />
	///     does: take only its own leaf-level lock (<c>_sendGate</c>), never call back into this
	///     association, and never block on anything that could itself be waiting on this association.
	///     A delegate that violates this can deadlock the association against itself from a different
	///     thread; the codebase's synchronous loopback tests do call back into the peer association
	///     from inside this delegate, which only works because a .NET <c>lock</c> is reentrant for the
	///     calling thread, not because the contract is optional.
	///     </para>
	/// </summary>
	public delegate void PacketSender(ReadOnlySpan<byte> packet);

	/// <summary>
	///     One complete, reassembled application message. <see cref="SctpAssociation.OnMessage" />'s
	///     established contract (stage 1's <see cref="PacketSender" /> doc comment set the precedent):
	///     <paramref name="message" /> is valid only for the duration of the callback. Round 4b: a
	///     <see cref="ReadOnlySequence{T}" />, not a span - a span cannot represent a fragmented message
	///     chained over more than one leased buffer without copying them together first, and Kestrel-style
	///     pipelines pay that copy's cost on every fragmented message forever rather than once, up front,
	///     to build this contract. A single-chunk message still delivers zero-copy, as a single-segment
	///     sequence wrapping the incoming datagram directly (<c>message.IsSingleSegment</c> is true); a
	///     fragmented one is a multi-segment sequence chained over its individual leased fragment buffers,
	///     never concatenated. Either way, every buffer backing the sequence is returned the instant the
	///     callback returns, so holding onto the sequence past the call is a use-after-free exactly like a
	///     span would have been. Fast-path consumers: <c>if (message.IsSingleSegment) { var span =
	///     message.FirstSpan; ... }</c>.
	/// </summary>
	public delegate void SctpMessageHandler(ushort streamId, uint ppid, in ReadOnlySequence<byte> message);

	/// <summary>
	///     RFC 4960 the four-way handshake only (INIT / INIT-ACK / COOKIE-ECHO / COOKIE-ACK); Tasks 4-6
	///     grow this class with DATA/SACK, retransmission, and stream management. The server side never
	///     commits an association's tags, TSNs, or stream counts to memory before a COOKIE-ECHO validates:
	///     everything the server would otherwise have to remember is folded into the HMAC-signed state
	///     cookie it hands back in the INIT-ACK and gets echoed verbatim in the COOKIE-ECHO, so a flood of
	///     spoofed INITs costs nothing more than generating and forgetting a cookie. Every timed behaviour
	///     (INIT/COOKIE-ECHO retransmit backoff) rides <see cref="OnTick" />, called by the owner
	///     (<see cref="UdpMux.OnTick" /> in Task 7's wiring) on a different thread than
	///     <see cref="OnPacketReceived" />, so both are guarded by <see cref="_gate" />.
	///     <see cref="OnEstablished" /> and <see cref="OnAborted" /> are always raised after
	///     <see cref="_gate" /> is released (<see cref="IceSession.Nominate" />/<c>Fail</c>'s pattern
	///     one file over), so a subscriber is free to call back into this association, or into anything
	///     else that might itself be waiting on <see cref="_gate" />, without risking a cross-thread
	///     deadlock. <see cref="PacketSender" /> is the one delegate still called under the lock; see its
	///     own doc comment for the leaf contract that makes that safe.
	/// </summary>
	public class SctpAssociation
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(SctpAssociation));

		// RFC 8831 section 6.2 recommends an SCTP endpoint used for WebRTC data channels support at
		// least 65535 streams in each direction; there is no per-caller configuration for this in
		// stage 2, so both sides simply ask for the maximum.
		private const ushort StreamCount = 65535;

		private const long RtoInitialMillis = 1000;
		private const long RtoMinMillis = 200;
		private const long RtoMaxMillis = 10000;
		private const int MaxAttempts = 5;

		// Task 5's send path. Messages at or under this size go out as a single DATA chunk; above it
		// they split into pieces of exactly this size (the last one shorter). Deliberately well under
		// the 1172-byte hard per-chunk ceiling (MaxSize 1200 - common header 12 - DATA header 16), so a
		// full-size DATA chunk still leaves headroom for a bundled SACK (16 bytes plus up to 16 bytes of
		// gap blocks) in the same packet.
		private const int FragmentThreshold = 1024;

		// The send-side queue budget (bytes of payload resident - queued plus in-flight, not yet
		// cumulatively acked - across the whole association): generous relative to the 128 KB congestion
		// window cap (SctpSendQueue.CwndCap) so a healthy peer under ordinary jitter never has a
		// fully-reliable Send blocked on it, but still bounded so a stalled or hostile peer cannot grow
		// this association's memory without limit. Documented in the task report; callers needing a
		// different budget pass their own value.
		private const uint DefaultSendQueueBudgetBytes = 1_048_576;

		// An inbound FORWARD-TSN's pair count is already implicitly bounded by SctpPacket.MaxSize (about
		// 290 pairs' worth fits at all), but HandleForwardTsn's stackalloc sizes stack space straight from
		// the wire-supplied count, so this caps it explicitly and locally rather than relying on that
		// outer bound alone.
		private const int MaxForwardTsnPairs = 512;

		// Outbound counterpart to MaxForwardTsnPairs: caps how many (streamId, streamSeq) pairs
		// SendForwardTsnPacket ever builds into one chunk, computed from what one SctpPacket.MaxSize
		// packet can actually hold - packet header(12), the FORWARD-TSN chunk's own header(4), and its
		// fixed New Cumulative TSN field(4), leaving the rest for 4-byte pairs. RFC 3758 permits the pair list to be advisory:
		// When more pairs exist than this, the FORWARD-TSN still
		// advertises the full, real cumulative target - only the pair list is truncated, never split across multiple chunks.
		private const int MaxOutboundForwardTsnPairs = (SctpPacket.MaxSize - 12 - 4 - 4) / 4;

		// The largest inbound Heartbeat Info this side can ever echo back verbatim: SctpPacket.MaxSize
		// minus the packet's own common header(12) and the Heartbeat chunk's header(4) plus its
		// Heartbeat Info parameter's own 4-byte TLV header(4). An Info any larger could never fit the
		// ack SendHeartbeatAckPacket builds, so HandleHeartbeat drops and counts it up front rather than
		// attempting (and failing) to answer.
		private const int MaxHeartbeatInfoLength = SctpPacket.MaxSize - 12 - 8;

		// The largest State Cookie an INIT-ACK can hand us that our own outbound COOKIE-ECHO could ever
		// carry: SctpPacket.MaxSize minus the packet's own common header(12) and CookieEchoChunk's
		// header(4) - CookieEchoChunk writes the cookie raw, with no further TLV framing of its own. A
		// cookie longer than this can never round-trip back to the peer, so HandleInitAck rejects it
		// before ever retaining it.
		private const int MaxCookieEchoCookieLength = SctpPacket.MaxSize - 12 - 4;

		// RFC 4960 6.2 SACK policy: a SACK goes out on the second packet carrying DATA, or 200ms after
		// the first unacked one, whichever comes first (plus the immediate triggers HandleData/MaybeSendSack
		// check for separately).
		private const long SackDelayMillis = 200;

		// RFC 4960 3.2 defines COOKIE-ACK as chunk type 11, empty value. SctpChunks.cs has no struct
		// for it (there is nothing to parse or write beyond the shared 4-byte chunk header), so it is
		// handled here directly through the internal SctpChunkCodec both files already share.
		private const byte CookieAckChunkType = 11;

		// RFC 4960 3.2/9.2: SHUTDOWN (7, carries a Cumulative TSN Ack this association never reads -
		// no retransmission-aware teardown is in scope here, only tearing down on receipt), SHUTDOWN-ACK
		// (8, empty value, same "no dedicated codec struct" shape as COOKIE-ACK above), and
		// SHUTDOWN-COMPLETE (14, empty value, never sent by this association since it never initiates a
		// graceful shutdown itself - only answered here so an inbound one is dropped-and-counted rather
		// than falling through to the unrecognised-type default).
		private const byte ShutdownChunkType = 7;
		private const byte ShutdownAckChunkType = 8;
		private const byte ShutdownCompleteChunkType = 14;

		// RFC 4960 3.2/3.3.10: ERROR (9), one or more error-cause TLVs this association never needs to
		// read - Task 8's interop against a real peer observed a real, benign one (cause code 8,
		// "Unrecognized Parameters", naming our RFC 3758 Forward-TSN-Supported INIT/INIT-ACK parameter,
		// which that peer's implementation does not recognize): the parameter's own type value (0xC000)
		// already encodes "skip and report, keep processing" in its top two bits, so the peer sending
		// this ERROR is itself proof nothing
		// more than dropped-and-counted is owed here, never a torn-down association. Given its own case
		// (rather than falling through to the unrecognised-type default) purely so a real, expected chunk
		// type is not miscounted as if it were hostile/malformed input, same reasoning as
		// SHUTDOWN-COMPLETE above.
		private const byte ErrorChunkType = 9;

		// Cookie layout (RFC 4960 5.1.3's "stateless cookie" technique): a fixed 32-byte plaintext
		// snapshot of everything the server would otherwise have to remember between INIT-ACK and
		// COOKIE-ECHO, followed by a 32-byte HMAC-SHA256 over that snapshot. Peer initiate tag(4),
		// our tag(4), peer a_rwnd(4), peer outbound/inbound streams(2+2), peer initial TSN(4), our own
		// initial TSN(4, not in the plan's literal list but required for the same statelessness: our
		// own InitialTsn is announced in the INIT-ACK we send, so it must survive the stateless gap
		// exactly like every other value we would otherwise forget), timestamp(8).
		private const int CookiePlainLength = 32;
		private const int CookieHmacLength = 32;
		internal const int CookieLength = CookiePlainLength + CookieHmacLength;
		private const long CookieMaxAgeMillis = 60_000;

		// Static per process is acceptable for stage 2 (per the task brief): every SctpAssociation
		// instance signs and verifies cookies with the same key, which is fine since the cookie's
		// only job is proving WE minted it a COOKIE-ECHO is echoing back, not authenticating a peer.
		private static readonly byte[] CookieHmacKey = RandomNumberGenerator.GetBytes(32);

		// Not readonly: starts as the DTLS-role-derived designation the constructor is given, but
		// Start() (Task 8) can flip a false one to true the moment this side self-initiates the SCTP
		// handshake on its own demand, rather than waiting on the DTLS-client side to do it. See
		// Start()'s own remarks for why a real interop peer makes that flip necessary.
		private bool _isClient;
		private readonly ushort _sctpPort;
		private readonly uint _arwndBudget;
		private readonly PacketSender _sendPacket;
		private readonly object _gate = new();

		private volatile SctpState _state = SctpState.Closed;

		private uint _localTag;
		private uint _peerTag;
		private uint _localInitialTsn;
		private uint _peerInitialTsn;
		private uint _peerArwnd;
		private ushort _peerOutboundStreams;
		private ushort _peerInboundStreams;
		private byte[] _cookie;

		/// <summary>
		///     Fix-round (Critical, Task 8 review): set by <see cref="HandleInit" /> every time this side
		///     answers a peer's INIT with an INIT-ACK, <see langword="null" /> until the first one.
		///     Deliberately NOT protocol state: this class stays a stateless INIT responder exactly as the
		///     class remarks describe (nothing commits until a COOKIE-ECHO validates), and this field never
		///     influences what this side accepts or rejects on the wire - it only suppresses THIS side's
		///     own <see cref="Start" /> for as long as the cookie just handed out could still be echoed
		///     back (<see cref="CookieMaxAgeMillis" />, the same window <see cref="TryValidateCookie" />
		///     itself enforces).
		///     <para>
		///     Without this, a corruption was reachable with no adversary involved: a peer sends INIT, this
		///     side answers INIT-ACK and (correctly, by design) commits nothing - <see cref="_state" /> stays
		///     <see cref="SctpState.Closed" />. If this side's OWN application then calls
		///     <see cref="RtcChannelManager.CreateChannel" /> before the peer's COOKIE-ECHO arrives,
		///     <see cref="Start" />'s <see cref="SctpState.Closed" /> check alone could not tell a genuinely
		///     idle association from one with a responder handshake already in flight, so it would flip
		///     <see cref="_isClient" /> and mint a competing INIT of its own - and the peer's still-valid,
		///     still-in-flight COOKIE-ECHO would then hit <see cref="HandleCookieEcho" />'s <c>_isClient</c>
		///     gate and be dropped on every retransmit, corrupting a handshake that had nothing wrong with
		///     it. A stale hint is harmless (an expired one just costs <see cref="Start" /> one extra check
		///     before it initiates normally), so this is never cleared early - only read against the clock.
		///     </para>
		/// </summary>
		private long? _lastRespondedInitAtTicks;

		// Handshake retransmit state: armed only while this side is itself waiting on a reply to
		// something it sent (CookieWait/CookieEchoed, see OnTick), which after Task 8 is no longer
		// exclusively the DTLS-client-designated side - see Start()'s own remarks. Reset to a fresh RTO
		// cycle whenever a new chunk starts waiting for a reply (INIT at Start, COOKIE-ECHO once the
		// INIT-ACK arrives).
		private int _attemptCount;
		private long _rtoMillis;
		private long _lastSentAtTicks;

		private long _ignoredPacketCount;

		private readonly SctpReceiveBuffer _receiveBuffer;
		private int _dataPacketsSinceSack;
		private bool _sackTimerArmed;
		private long _sackTimerArmedAtTicks;

		// True for exactly the span of one outside-the-lock delivery drain (set and cleared under _gate
		// by DeliverLeasedMessages around that span). Teardown reads this, also under _gate, to decide
		// whether it may reset _receiveBuffer inline (no drain in flight) or must hand the reset off to
		// the drain itself via _pendingReset (a drain may be running on a different thread).
		//
		// A plain bool, not a depth counter: a same-thread reentrant OnPacketReceived call from inside a
		// subscriber's own callback (a real, expected shape - see DeliverLeasedMessages' own remarks) can
		// nest a second DeliverLeasedMessages call, whose finally block clears this flag while the outer
		// call's frame is still further up the stack. If Teardown lands on a different thread in exactly
		// that window, it sees this false and resets _receiveBuffer inline instead of deferring, even
		// though a drain is still, in the ordinary sense, in flight. This is harmless under the receive
		// buffer's current shape, not by a guard here: Reset never touches _freeSegments, so a
		// ReleaseDelivery call landing after an inline Reset cannot corrupt or double-return anything on
		// it; a delivery's buffer is already detached from _fragments/_orderedPending the moment it
		// becomes a LeasedDelivery, so Reset cannot double-return it either; and _bufferedBytes is
		// decremented at delivery-creation time, not at release time, so Reset zeroing it is unaffected
		// by release ordering. A future change to Reset or ReleaseDelivery that stops holding one of
		// those three properties needs this reasoned through again.
		private bool _drainInFlight;
		private bool _pendingReset;

		// Send path (Task 5).
		private readonly SctpSendQueue _sendQueue;
		private uint _nextOutboundTsn;
		private readonly Dictionary<ushort, ushort> _nextOutboundSeqByStream = new();
		private readonly List<(ushort StreamId, ushort StreamSeq)> _forwardTsnPairsScratch = new();
		private bool _flushing;

		/// <summary>
		///     The clock seam for RTO/T3-rtx: every "now" read on the send path goes through this instead
		///     of <see cref="Environment.TickCount64" /> directly, so a test can drive RTO backoff and
		///     timeout deterministically (advance a fake clock, call <see cref="OnTick" />) instead of
		///     paying real wall-clock delay up to <c>RtoMaxMillis</c> (10s) per retransmit round. Defaults
		///     to the real clock; only tests (via the assembly's InternalsVisibleTo) ever replace it. The
		///     existing handshake retransmit logic above intentionally still reads
		///     <see cref="Environment.TickCount64" /> directly - it predates this seam and is out of this
		///     task's scope, so it is left alone rather than switched over incidentally.
		/// </summary>
		internal Func<long> ClockNowMillis = () => Environment.TickCount64;

		public SctpState State => _state;

		public event Action OnEstablished;
		public event Action<string> OnAborted;

		/// <summary>Raised inline from <see cref="OnPacketReceived" />, outside <see cref="_gate" />, once per complete reassembled message.</summary>
		public event SctpMessageHandler OnMessage;

		/// <summary>
		///     Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many inbound
		///     packets this association has dropped, for any reason (bad checksum, unparseable chunk,
		///     wrong verification tag, an invalid or stale cookie, or a chunk that made no sense in the
		///     current state). Proves hostile input is counted and ignored rather than silently lost.
		/// </summary>
		internal long IgnoredPacketCount => Interlocked.Read(ref _ignoredPacketCount);

		/// <summary>Test visibility only: this association's own chosen verification tag.</summary>
		internal uint LocalVerificationTag
		{
			get
			{
				lock (_gate) return _localTag;
			}
		}

		/// <summary>Test visibility only: the peer's verification tag, as learned during the handshake.</summary>
		internal uint PeerVerificationTag
		{
			get
			{
				lock (_gate) return _peerTag;
			}
		}

		public SctpAssociation(bool isClient, ushort sctpPort, uint arwndBudget, PacketSender sendPacket, uint sendQueueBudgetBytes = DefaultSendQueueBudgetBytes)
		{
			_isClient = isClient;
			_sctpPort = sctpPort;
			_arwndBudget = arwndBudget;
			_sendPacket = sendPacket;
			_receiveBuffer = new SctpReceiveBuffer(arwndBudget);
			_sendQueue = new SctpSendQueue(sendQueueBudgetBytes);
		}

		/// <summary>Test visibility only: budget minus buffered bytes, what the next outgoing SACK would carry as a_rwnd.</summary>
		internal uint CurrentArwnd
		{
			get
			{
				lock (_gate) return _receiveBuffer.CurrentArwnd;
			}
		}

		/// <summary>Test visibility only: how many DATA chunks were dropped for arriving when the byte budget was already spent.</summary>
		internal long DataDroppedByBudgetCount => _receiveBuffer.DroppedByBudgetCount;

		/// <summary>Test visibility only: how many DATA chunks were dropped because the out-of-order TSN set was already full.</summary>
		internal long DataDroppedByGapCapCount => _receiveBuffer.DroppedByGapCapCount;

		/// <summary>Test visibility only: how many DATA chunks were dropped for arriving more than 65535 TSNs ahead of the cumulative ack (unrepresentable in any SACK gap block).</summary>
		internal long DataDroppedBeyondHorizonCount => _receiveBuffer.DroppedBeyondHorizonCount;

		/// <summary>Test visibility only: how many incomplete fragment runs were reneged (discarded early, RFC 4960 6.2) to make room under budget pressure.</summary>
		internal long DataRenegedFragmentRunCount => _receiveBuffer.RenegedFragmentRunCount;

		/// <summary>Test visibility only: the receive-side cumulative TSN ack point (highest TSN received with nothing missing before it).</summary>
		internal uint CumulativeTsnAck
		{
			get
			{
				lock (_gate) return _receiveBuffer.CumulativeTsnAck;
			}
		}

		/// <summary>Test visibility only: bytes of outbound payload still resident (queued or in-flight, not yet cumulatively acked by the peer).</summary>
		internal uint SendQueuedBytes
		{
			get
			{
				lock (_gate) return _sendQueue.QueuedBytes;
			}
		}

		/// <summary>Test visibility only: current send-side congestion window.</summary>
		internal uint SendCwnd
		{
			get
			{
				lock (_gate) return _sendQueue.Cwnd;
			}
		}

		/// <summary>Test visibility only: the peer's most recently advertised receive window, as last accepted from a valid SACK (or the handshake).</summary>
		internal uint PeerArwnd
		{
			get
			{
				lock (_gate) return _peerArwnd;
			}
		}

		/// <summary>Test visibility only: current send-side RTO.</summary>
		internal long SendRtoMillis
		{
			get
			{
				lock (_gate) return _sendQueue.RtoMillis;
			}
		}

		/// <summary>Test visibility only: how many outbound DATA chunks were retransmitted (T3-rtx or fast retransmit), whether or not the retransmission itself was later abandoned.</summary>
		internal long SendRetransmitCount => _sendQueue.RetransmitCount;

		/// <summary>Test visibility only: how many outbound DATA chunks were abandoned (RFC 3758) after exceeding their own maxRetransmits.</summary>
		internal long SendAbandonedCount => _sendQueue.AbandonedCount;

		/// <summary>Test visibility only: how many times three duplicate gap-carrying SACKs triggered a fast retransmit ahead of T3-rtx.</summary>
		internal long SendFastRetransmitCount => _sendQueue.FastRetransmitCount;

		/// <summary>Test visibility only: how many times T3-rtx actually fired.</summary>
		internal long SendTimeoutCount => _sendQueue.TimeoutCount;

		/// <summary>Test visibility only: SACKs dropped whole (RFC 4960 6.2.1) for acking a TSN newer than anything actually transmitted.</summary>
		internal long SacksDroppedFutureCumAck => _sendQueue.SacksDroppedFutureCumAck;

		/// <summary>Test visibility only: SACKs dropped whole (RFC 4960 6.2.1) for acking a cumulative TSN older than the current ack point.</summary>
		internal long SacksDroppedStale => _sendQueue.SacksDroppedStale;

		/// <summary>
		///     Sends the opening INIT and arms the retransmit timer, claiming the SCTP-initiator role for
		///     this side (<see cref="_isClient" />, independent of whatever DTLS-role designation this
		///     instance was constructed with - see that field's own remarks). Idempotent: a no-op once
		///     <see cref="_state" /> has already left <see cref="SctpState.Closed" /> by self-initiating
		///     earlier, and ALSO a no-op while a responder handshake this side answered is still possibly
		///     in flight (<see cref="_lastRespondedInitAtTicks" /> - <see cref="HandleInit" /> deliberately
		///     never changes <see cref="_state" /> when answering as a stateless responder, so that state
		///     alone cannot distinguish "genuinely idle" from "already offered a cookie to a peer whose
		///     COOKIE-ECHO just has not arrived yet" - see that field's own remarks for the corruption this
		///     closes). Either way, every caller can call this unconditionally and "maybe" rather than
		///     needing to know which.
		///     <para>
		///     Task 7 had exactly one caller (<see cref="RtcPeer.RunHandshakeAsync" />, gated on
		///     <c>_dtlsIsClient</c>): RFC 8841 does not mandate who initiates, but NetherNet has the DTLS
		///     client do it, so the codebase followed that as a convention baked into the caller, not this
		///     method. Task 8's interop against a real WebRTC peer falsified the assumption that convention
		///     is universal: that peer's own SCTP association only ever starts reactively, the moment its
		///     OWN application asks for a data channel - never eagerly on DTLS connecting, regardless of
		///     which side is the DTLS client. Paired with this side's own DTLS-client-only eagerness, the
		///     result was a genuine deadlock: whenever the peer held the DTLS-client designation but never
		///     created a channel of its own, neither side ever sent the opening INIT, and an association
		///     this side had local demand for (<see cref="RtcChannelManager.CreateChannel" /> already had a
		///     channel queued) sat in <see cref="SctpState.Closed" /> forever. This method's idempotency is
		///     what lets <see cref="RtcChannelManager.CreateChannel" /> now also call it - unconditionally,
		///     the moment local demand exists and nothing has started yet - as a second, demand-driven
		///     trigger alongside the original DTLS-client eagerness, without either caller needing to
		///     coordinate with or even know about the other. The remaining case that leaves - both sides
		///     genuinely self-initiating with nothing received yet from the other - is not blocked here at
		///     all: see <see cref="HandleInit" />/<see cref="HandleCookieEcho" />'s own remarks for how that
		///     collision converges instead of deadlocking.
		///     </para>
		/// </summary>
		public void Start()
		{
			lock (_gate)
			{
				if (_state != SctpState.Closed) return;

				if (_lastRespondedInitAtTicks is long respondedAt && Environment.TickCount64 - respondedAt < CookieMaxAgeMillis) return;

				_isClient = true;
				_localTag = RandomUInt32();
				_localInitialTsn = RandomUInt32();
				_state = SctpState.CookieWait;
				ResetRetransmitState();
				SendInitPacket();
			}
		}

		/// <summary>
		///     Public, deliberate teardown entry point (Task 7: <see cref="RtcPeer.Dispose" /> calls this
		///     so the association's own send/receive leases are released on the same path an inbound
		///     ABORT/SHUTDOWN already uses, rather than just being abandoned to the GC). Sends a
		///     best-effort ABORT to the peer - only when <see cref="SctpState.Established" />, since
		///     nothing before that point has a peer verification tag worth addressing a packet to, or any
		///     peer-side state worth notifying about (the stateless-cookie design in this class's own
		///     remarks means the peer commits nothing until COOKIE-ECHO validates) - then runs the exact
		///     same <see cref="Teardown" /> path <see cref="HandleAbort" />/<see cref="HandleShutdown" />
		///     already use. Idempotent, the same way those are: a second call once already
		///     <see cref="SctpState.Aborted" /> is a no-op, not a second ABORT on the wire or a second
		///     <see cref="OnAborted" /> firing.
		/// </summary>
		public void Abort(string reason = "Local abort.")
		{
			string teardownReason;
			lock (_gate)
			{
				if (_state == SctpState.Aborted) return;

				if (_state == SctpState.Established) SendAbortPacket();

				teardownReason = Teardown(reason);
			}

			// Raised outside _gate: see the class remarks and OnTick's identical pattern for its own
			// abortReason.
			OnAborted?.Invoke(teardownReason);
		}

		/// <summary>
		///     Queues <paramref name="message" /> for delivery on <paramref name="streamId" />, fragmenting
		///     it above <see cref="FragmentThreshold" /> bytes into consecutive-TSN, one-streamSeq B/middle/E
		///     chunks, and flushes whatever the current send window (<c>min(peer a_rwnd, cwnd)</c>) allows
		///     right away. Returns false, never blocking, when the association is not
		///     <see cref="SctpState.Established" /> or the send-queue budget is already spent by
		///     already-queued data (the caller's problem: back off and retry, this is not itself a
		///     transport error). <paramref name="maxRetransmits" /> negative means fully reliable; a
		///     non-negative value is the partial-reliability budget (RFC 3758) before this message's
		///     remaining unacked chunks are abandoned and FORWARD-TSN carries the peer past them.
		/// </summary>
		public bool Send(ushort streamId, uint ppid, ReadOnlySpan<byte> message, bool unordered, int maxRetransmits)
		{
			lock (_gate)
			{
				if (_state != SctpState.Established) return false;
				if (!_sendQueue.HasRoomFor((uint) message.Length)) return false;

				ushort streamSeq = 0;
				if (!unordered)
				{
					streamSeq = _nextOutboundSeqByStream.TryGetValue(streamId, out ushort v) ? v : (ushort) 0;
					_nextOutboundSeqByStream[streamId] = unchecked((ushort) (streamSeq + 1));
				}

				int pieceCount = Math.Max(1, (message.Length + FragmentThreshold - 1) / FragmentThreshold);
				int offset = 0;
				for (int i = 0; i < pieceCount; i++)
				{
					int len = Math.Min(FragmentThreshold, message.Length - offset);
					bool begin = i == 0;
					bool end = i == pieceCount - 1;
					uint tsn = _nextOutboundTsn;
					_nextOutboundTsn = unchecked(_nextOutboundTsn + 1);

					_sendQueue.Enqueue(tsn, streamId, streamSeq, ppid, unordered, begin, end, message.Slice(offset, len), maxRetransmits);
					offset += len;
				}

				Flush();
				return true;
			}
		}

		/// <summary>
		///     Handshake retransmit backoff, armed only while this side is itself waiting on a reply to
		///     something it sent (<see cref="_isClient" /> - see <see cref="Start" />'s own remarks for why
		///     that is no longer purely a fixed DTLS-role designation). A side that only ever answers the
		///     peer's INIT, never sending its own, has nothing here to retransmit. Runs on whatever thread
		///     the owner calls it from (a different one than <see cref="OnPacketReceived" /> in the real mux
		///     wiring), so both share <see cref="_gate" />.
		/// </summary>
		public void OnTick()
		{
			string abortReason = null;

			lock (_gate)
			{
				if (_isClient && (_state == SctpState.CookieWait || _state == SctpState.CookieEchoed))
				{
					long now = Environment.TickCount64;
					if (now - _lastSentAtTicks >= _rtoMillis)
					{
						if (_attemptCount >= MaxAttempts)
						{
							SctpState abandonedState = _state;
							_state = SctpState.Aborted;
							abortReason = $"SCTP handshake abandoned in {abandonedState} after {MaxAttempts} attempts.";
							Log.Warn(abortReason);
						}
						else
						{
							_attemptCount++;
							_rtoMillis = Math.Clamp(_rtoMillis * 2, RtoMinMillis, RtoMaxMillis);
							_lastSentAtTicks = now;

							if (_state == SctpState.CookieWait) SendInitPacket();
							else SendCookieEchoPacket();
						}
					}
				}

				// The 200ms SACK fallback (RFC 4960 6.2): role-agnostic, unlike the handshake retransmit
				// above, since either side of an established association can be sitting on unacked DATA.
				if (_sackTimerArmed && Environment.TickCount64 - _sackTimerArmedAtTicks >= SackDelayMillis)
				{
					SendSackPacket();
					_dataPacketsSinceSack = 0;
					_sackTimerArmed = false;
				}

				// T3-rtx (RFC 4960 6.3.3): role-agnostic, same as the SACK fallback above - either side
				// can have outstanding outbound DATA once established.
				if (_state == SctpState.Established && _sendQueue.IsTimerExpired(ClockNowMillis()))
				{
					long now = ClockNowMillis();
					_sendQueue.HandleTimeout(now);
					MaybeSendForwardTsn(retransmitIfOutstanding: true);

					// RFC 3758 5.2: a FORWARD-TSN this side already advertised is not necessarily covered
					// by HandleTimeout's own "anyOutstanding" re-arm above - once every DATA chunk behind
					// it is abandoned there is nothing left for that check to see, but the peer's SACK may
					// still not have caught up (the FORWARD-TSN itself is still in flight, or was lost).
					// Keeping the timer armed here, independent of anyOutstanding, is what lets a lost
					// FORWARD-TSN actually get retried above on a later tick instead of the association
					// going quiet with a hole neither side can ever clear on its own.
					_sendQueue.ArmTimerIfForwardTsnOutstanding(now);

					Flush();
				}
			}

			// Raised outside _gate: IceSession.Nominate/Fail's pattern, so a subscriber (Task 7's
			// RtcPeer) can safely call back into this association, or anything else, from a different
			// thread than the one that just released the lock.
			if (abortReason != null) OnAborted?.Invoke(abortReason);
		}

		/// <summary>
		///     Validates the common header (bounds, checksum), then walks every chunk in the packet and
		///     dispatches each by type: SCTP routinely bundles more than one chunk per packet (a DATA
		///     chunk riding alongside a SACK is normal), so this cannot stop at the first one. Never
		///     throws on hostile input: a bad checksum, an empty or malformed chunk list, an unrecognised
		///     chunk type, a wrong verification tag, or an invalid cookie all fall through to a
		///     dropped-and-counted packet (or chunk) rather than an exception, per this codebase's
		///     hot-path rule for the mux receive thread.
		///     <para>
		///     RFC 4960 8.5: once this association has a tag of its own to be checked against
		///     (<see cref="SctpState.Established" /> or <see cref="SctpState.Aborted" />), a packet whose
		///     verification tag does not match <see cref="_localTag" /> is dropped whole here, before the
		///     chunk loop even starts - no chunk in it is inspected or dispatched, so DATA/SACK/FORWARD-TSN
		///     and the liveness/teardown chunk types below all get this protection uniformly, not just the
		///     handshake chunk types that already checked the tag themselves. Pre-establishment packets
		///     (<see cref="SctpState.Closed" />, <see cref="SctpState.CookieWait" />,
		///     <see cref="SctpState.CookieEchoed" />) skip this gate and keep flowing straight to the
		///     per-chunk handshake checks exactly as before: the stateless COOKIE-ECHO path (see the class
		///     remarks) has no tag of its own to gate on until a cookie is actually validated, and each
		///     handshake chunk type already validates the tag it expects on its own terms (see
		///     <see cref="HandleInit" />, <see cref="HandleInitAck" />, <see cref="HandleCookieEcho" />,
		///     <see cref="HandleCookieAck" />). RFC 4960 8.5.1's special T-bit acceptance rule (an ABORT or
		///     SHUTDOWN-COMPLETE whose tag echoes the PEER's own, rather than ours, is also acceptable) is
		///     not implemented by this gate - out of this task's scope, see <see cref="HandleAbort" />'s own
		///     remarks.
		///     </para>
		/// </summary>
		public void OnPacketReceived(ReadOnlyMemory<byte> packet)
		{
			ReadOnlySpan<byte> packetSpan = packet.Span;

			if (!SctpPacket.TryReadHeader(packetSpan, out _, out _, out uint verificationTag))
			{
				CountIgnored();
				return;
			}

			bool dropWholePacketForWrongTag;
			lock (_gate)
			{
				dropWholePacketForWrongTag = (_state == SctpState.Established || _state == SctpState.Aborted) && verificationTag != _localTag;
			}

			if (dropWholePacketForWrongTag)
			{
				CountIgnored();
				return;
			}

			SctpPacket.ChunkEnumerator enumerator = SctpPacket.EnumerateChunks(packetSpan);
			bool anyChunk = false;
			bool packetHadData = false;
			bool immediateSackRequested = false;

			while (enumerator.MoveNext())
			{
				anyChunk = true;
				(byte type, byte flags, ReadOnlySpan<byte> value) = enumerator.Current;

				bool becameEstablished = false;
				bool hasZeroCopyDelivery = false;
				ushort zcStreamId = 0;
				uint zcPpid = 0;
				ReadOnlyMemory<byte> zcPayload = default;
				string teardownReason = null;

				lock (_gate)
				{
					switch (type)
					{
						case SctpChunkType.Init:
							HandleInit(value, verificationTag);
							break;

						case SctpChunkType.InitAck:
							HandleInitAck(value, verificationTag);
							break;

						case SctpChunkType.CookieEcho:
							becameEstablished = HandleCookieEcho(value, verificationTag);
							break;

						case CookieAckChunkType:
							becameEstablished = HandleCookieAck(verificationTag);
							break;

						case SctpChunkType.Data:
							// packetHadData reflects whether this chunk was actually
							// admitted (parsed and reached the receive buffer), not merely that its chunk
							// type was DATA - a chunk HandleData rejected (malformed, or the association was
							// not yet/no longer Established) must not feed MaybeSendSack's cadence
							// bookkeeping, exactly as it must not feed a real send (see that method's own
							// state gate).
							hasZeroCopyDelivery = HandleData(flags, value, packet, packetSpan, out zcStreamId, out zcPpid, out zcPayload, out bool chunkWantsImmediateSack, out bool chunkAdmitted);
							if (chunkWantsImmediateSack) immediateSackRequested = true;
							if (chunkAdmitted) packetHadData = true;
							break;

						case SctpChunkType.Sack:
							HandleSack(value);
							break;

						case SctpChunkType.ForwardTsn:
							// A FORWARD-TSN moves our receive cumulative just like DATA can, so the peer
							// should get a SACK reflecting the new point.
							packetHadData = HandleForwardTsn(value) || packetHadData;
							break;

						case SctpChunkType.Heartbeat:
							HandleHeartbeat(value);
							break;

						case SctpChunkType.Abort:
							teardownReason = HandleAbort();
							break;

						case ShutdownChunkType:
							teardownReason = HandleShutdown();
							break;

						case ShutdownCompleteChunkType:
							// Never sent by this association (it never initiates a graceful shutdown), so
							// one arriving is either a stray retransmit of our own SHUTDOWN-ACK's peer reply
							// after we already tore down, or hostile - either way, dropped and counted like
							// any other post-teardown packet, per the task brief.
							CountIgnored();
							break;

						case ErrorChunkType:
							// See ErrorChunkType's own remarks: a real, benign chunk this association has no
							// cause taxonomy for and nothing to do about, dropped and counted like any other
							// chunk this class only ever observes, never acts on.
							CountIgnored();
							break;

						default:
							CountIgnored();
							break;
					}
				}

				// Raised/delivered outside _gate: see the class remarks and PacketSender's doc comment.
				// zcPayload is a slice of `packet`, which is still on this method's stack for the whole
				// call, so the memory (and the single-segment sequence built over it) stays valid here.
				if (becameEstablished) OnEstablished?.Invoke();
				if (teardownReason != null) OnAborted?.Invoke(teardownReason);
				if (hasZeroCopyDelivery)
				{
					var sequence = new ReadOnlySequence<byte>(zcPayload);
					SafeInvokeOnMessage(zcStreamId, zcPpid, in sequence);
				}

				DeliverLeasedMessages();
			}

			if (!anyChunk)
			{
				CountIgnored();
				return;
			}

			if (packetHadData)
			{
				lock (_gate)
				{
					MaybeSendSack(immediateSackRequested);
				}
			}
		}

		/// <summary>
		///     Server or client role, called under <see cref="_gate" />: decodes one DATA chunk and hands
		///     it to <see cref="_receiveBuffer" />. Returns true when the chunk alone is a complete,
		///     immediately deliverable message (unordered, or exactly the next due ordered message on its
		///     stream): <paramref name="zcPayload" /> is then the slice of <paramref name="packet" /> (the
		///     original memory <see cref="OnPacketReceived" /> was given, not a copy) matching
		///     <paramref name="value" />'s payload, the zero-copy path the delivery contract requires. Any
		///     other completed delivery (a reassembled fragment run, or an ordered message a cascade
		///     unblocked) lands in <see cref="SctpReceiveBuffer.Deliveries" /> instead, which the caller
		///     drains once outside the lock regardless of this method's return value. DATA received before
		///     <see cref="SctpState.Established" /> is dropped and counted, never buffered.
		///     <paramref name="admitted" /> is true once, and only once, this chunk actually
		///     reached <see cref="SctpReceiveBuffer.Receive" /> - a malformed chunk, or one that arrived
		///     outside <see cref="SctpState.Established" />, leaves it false, which is what
		///     <see cref="OnPacketReceived" /> uses to keep such a chunk from feeding
		///     <see cref="MaybeSendSack" />'s cadence bookkeeping.
		/// </summary>
		private bool HandleData(byte flags, ReadOnlySpan<byte> value, ReadOnlyMemory<byte> packet, ReadOnlySpan<byte> packetSpan, out ushort streamId, out uint ppid, out ReadOnlyMemory<byte> zcPayload, out bool immediateSackRequested, out bool admitted)
		{
			streamId = 0;
			ppid = 0;
			zcPayload = default;
			immediateSackRequested = false;
			admitted = false;

			if (!DataChunkHeader.TryParse(flags, value, out DataChunkHeader header, out ReadOnlySpan<byte> payload))
			{
				CountIgnored();
				return false;
			}

			if (header.ImmediateSack) immediateSackRequested = true;

			if (_state != SctpState.Established)
			{
				CountIgnored();
				return false;
			}

			admitted = true;
			bool zeroCopy = _receiveBuffer.Receive(header, payload);
			if (zeroCopy)
			{
				streamId = header.StreamId;
				ppid = header.Ppid;
				zcPayload = SliceMemoryFor(packet, packetSpan, payload);
			}

			return zeroCopy;
		}

		/// <summary>
		///     Server or client role, called under <see cref="_gate" />: applies an inbound SACK to the send
		///     side (<see cref="_sendQueue" />), refreshes the peer's advertised receive window, and follows
		///     up with a FORWARD-TSN and/or an outbound flush if the SACK made either possible (fast
		///     retransmit abandoning a chunk, or the window opening back up). A SACK <see cref="_sendQueue" />
		///     rejects outright (RFC 4960 6.2.1: acking a TSN never transmitted, or older than the current
		///     ack point) updates nothing at all here either - not even the advertised window - since a
		///     rejected SACK is not trusted for anything it carries, not just the parts
		///     <see cref="SctpSendQueue.OnSackReceived" /> itself acts on.
		/// </summary>
		private void HandleSack(ReadOnlySpan<byte> value)
		{
			Span<SackChunk.GapBlock> gapBlocks = stackalloc SackChunk.GapBlock[SackChunk.MaxGapBlocks];
			if (!SackChunk.TryParseGapBlocks(value, gapBlocks, out uint cumulativeTsnAck, out uint arwnd, out int gapCount))
			{
				CountIgnored();
				return;
			}

			if (_state != SctpState.Established)
			{
				CountIgnored();
				return;
			}

			bool accepted = _sendQueue.OnSackReceived(cumulativeTsnAck, gapBlocks.Slice(0, gapCount), ClockNowMillis());
			if (!accepted) return;

			_peerArwnd = arwnd;

			MaybeSendForwardTsn();
			Flush();
		}

		/// <summary>
		///     Server or client role, called under <see cref="_gate" />: an inbound FORWARD-TSN moves our
		///     receive cumulative ack point exactly like DATA arriving can (RFC 3758), so it needs the same
		///     "does the caller owe the peer a SACK now" signal <see cref="HandleData" /> gives via its own
		///     return value. Returns false (and counts the chunk as ignored) for a malformed chunk or one
		///     that arrives before <see cref="SctpState.Established" />.
		/// </summary>
		private bool HandleForwardTsn(ReadOnlySpan<byte> value)
		{
			if (_state != SctpState.Established)
			{
				CountIgnored();
				return false;
			}

			if (!ForwardTsnChunk.TryParse(value, out ForwardTsnChunk chunk))
			{
				CountIgnored();
				return false;
			}

			int pairCount = chunk.PairCount;

			// Wire-bounded in practice (a chunk this size cannot carry more than ~290 pairs), but the
			// stackalloc below sizes stack space straight from that count, so this keeps the bound local
			// and visible rather than relying on the outer packet-size limit alone.
			if (pairCount > MaxForwardTsnPairs)
			{
				CountIgnored();
				return false;
			}

			Span<(ushort StreamId, ushort StreamSeq)> pairs = stackalloc (ushort, ushort)[pairCount];
			for (int i = 0; i < pairCount; i++) pairs[i] = chunk.GetPair(i);

			_receiveBuffer.AdvanceCumulative(chunk.NewCumulativeTsn, pairs);
			return true;
		}

		/// <summary>
		///     Server or client role, called under <see cref="_gate" />: RFC 4960 8.3 path-liveness probe,
		///     answered only once <see cref="SctpState.Established" /> - one arriving during the handshake or
		///     after teardown is dropped and counted, not answered. The Heartbeat Info parameter is echoed
		///     back VERBATIM (opaque bytes, no interpretation): a compliant WebRTC peer heartbeats during a
		///     session and treats silence as path death. A malformed chunk (missing or
		///     truncated TLV) is dropped and counted rather than answered - this codebase's hot-path law
		///     (never throw on hostile input) applies here exactly as everywhere else on this receive path.
		/// </summary>
		private void HandleHeartbeat(ReadOnlySpan<byte> value)
		{
			if (_state != SctpState.Established)
			{
				CountIgnored();
				return;
			}

			if (!HeartbeatChunk.TryParse(value, out HeartbeatChunk heartbeat))
			{
				CountIgnored();
				return;
			}

			// A peer-supplied Info this side could never echo back within one SctpPacket.MaxSize packet:
			// dropped and counted rather than attempted, the hot-path law applied to a length nothing
			// upstream of this bounds (see MaxHeartbeatInfoLength's own remarks).
			if (heartbeat.Info.Length > MaxHeartbeatInfoLength)
			{
				CountIgnored();
				return;
			}

			SendHeartbeatAckPacket(heartbeat.Info);
		}

		/// <summary>
		///     Server or client role, called under <see cref="_gate" />: an inbound ABORT tears the
		///     association down unconditionally - no cause taxonomy is read (<see cref="AbortChunk" />'s own
		///     remarks: nothing here needs the cause data, so a garbage or empty value is tolerated without
		///     even being parsed). Idempotent: an ABORT (or anything else) arriving after this association is
		///     already <see cref="SctpState.Aborted" /> is dropped and counted instead, which is what keeps
		///     <see cref="OnAborted" /> firing exactly once no matter how many teardown-triggering chunks
		///     arrive. This chunk is only ever reached once <see cref="OnPacketReceived" />'s own
		///     packet-level gate has already confirmed the packet's verification tag matches
		///     <see cref="_localTag" /> (that gate covers <see cref="SctpState.Established" /> and
		///     <see cref="SctpState.Aborted" /> alike, so it also guards this idempotency check itself). RFC
		///     4960 8.5.1's special verification-tag acceptance rule for ABORT and SHUTDOWN-COMPLETE (accept
		///     if the T bit is set and the tag echoes the PEER's own tag instead of ours, in addition to the
		///     ordinary exact-match case the packet-level gate enforces) is deliberately NOT implemented -
		///     out of this task's scope, not something the gate weakens or works around.
		/// </summary>
		private string HandleAbort()
		{
			if (_state == SctpState.Aborted)
			{
				CountIgnored();
				return null;
			}

			return Teardown("Peer sent ABORT.");
		}

		/// <summary>
		///     Server or client role, called under <see cref="_gate" />: an inbound SHUTDOWN answers with
		///     SHUTDOWN-ACK, then tears down exactly like <see cref="HandleAbort" /> does. The chunk's own
		///     Cumulative TSN Ack value is never read - no retransmission-aware graceful shutdown is in scope
		///     here (see the task report), only tearing down on receipt - so any content there, garbage or
		///     otherwise, is tolerated without being parsed. Idempotent the same way <see cref="HandleAbort" />
		///     is: a SHUTDOWN arriving after teardown is dropped and counted instead of answered again.
		/// </summary>
		private string HandleShutdown()
		{
			if (_state == SctpState.Aborted)
			{
				CountIgnored();
				return null;
			}

			SendShutdownAckPacket();
			return Teardown("Peer sent SHUTDOWN.");
		}

		/// <summary>
		///     The common ABORT/SHUTDOWN teardown path, called under <see cref="_gate" /> only after the
		///     caller has already confirmed <see cref="_state" /> is not yet <see cref="SctpState.Aborted" />
		///     (so this never runs twice): flips the state, releases every outstanding send-queue lease
		///     (<see cref="SctpSendQueue.ReleaseAll" />) so a mid-flight association does not leak its leased
		///     buffers, and lets the receive buffer release its own parked state - reassembly fragments,
		///     buffered out-of-turn ordered messages, the out-of-order TSN set - via
		///     <see cref="SctpReceiveBuffer.Reset" />, reusing <see cref="_peerInitialTsn" /> since the exact
		///     value no longer matters (this buffer will never process another chunk). Also disarms the
		///     200ms delayed-SACK fallback (<see cref="_sackTimerArmed" />): that timer is not gated by
		///     <see cref="_state" /> in <see cref="OnTick" />, so left armed it would otherwise fire a stray
		///     SACK off a dead association. Returns <paramref name="reason" /> unchanged, for the caller to
		///     raise <see cref="OnAborted" /> with once <see cref="_gate" /> is released (the established
		///     outside-the-lock pattern; see the class remarks and <see cref="OnTick" />'s own
		///     <c>abortReason</c> variable).
		///     <para>
		///     This can run on a different thread than <see cref="OnPacketReceived" /> (<see cref="Abort" />
		///     from <see cref="RtcPeer.Dispose" />, an application thread) while
		///     <see cref="DeliverLeasedMessages" /> has an outside-the-lock delivery drain in flight on the
		///     mux thread; calling <see cref="SctpReceiveBuffer.Reset" /> here unconditionally would mutate
		///     the same <see cref="SctpReceiveBuffer" /> state (the free list, leased buffers already handed
		///     out as part of a delivery) that drain is concurrently walking, from two threads with no lock
		///     common to both at that instant. So: while a drain is in flight
		///     (<see cref="_drainInFlight" />), this only requests the reset (<see cref="_pendingReset" />)
		///     and returns - the drain performs it, under <see cref="_gate" />, once it finishes and
		///     observes the flag. When no drain is in flight, this resets inline.
		///     </para>
		/// </summary>
		private string Teardown(string reason)
		{
			_state = SctpState.Aborted;
			_sendQueue.ReleaseAll();

			if (_drainInFlight) _pendingReset = true;
			else _receiveBuffer.Reset(_peerInitialTsn);

			_sackTimerArmed = false;
			return reason;
		}

		/// <summary>
		///     Called under <see cref="_gate" /> after anything that might have abandoned a chunk (T3-rtx in
		///     <see cref="OnTick" />, fast retransmit inside <see cref="HandleSack" />): sends a FORWARD-TSN
		///     when <see cref="_sendQueue" /> can now advertise further than it already has.
		///     <paramref name="retransmitIfOutstanding" /> is true only from <see cref="OnTick" />'s T3-rtx
		///     branch (RFC 3758 5.2): when nothing NEW has become advanceable, but the last FORWARD-TSN this
		///     side advertised is still ahead of what the peer's own SACK has acknowledged - either it is
		///     still in flight, or the packet carrying it was lost - this re-sends that exact same target
		///     and pair list, the FORWARD-TSN's own equivalent of a T3 DATA retransmission. Left false from
		///     <see cref="HandleSack" />'s own call so an ordinary SACK that simply has not caught up yet
		///     does not itself provoke a resend on every packet; only the timer path does.
		/// </summary>
		private void MaybeSendForwardTsn(bool retransmitIfOutstanding = false)
		{
			if (_sendQueue.TryComputeForwardTsnAdvance(_forwardTsnPairsScratch, MaxOutboundForwardTsnPairs, out uint newTarget))
			{
				SendForwardTsnPacket(newTarget, _forwardTsnPairsScratch);
				_sendQueue.MarkForwardTsnAdvertised(newTarget);
			}
			else if (retransmitIfOutstanding && _sendQueue.TryGetOutstandingForwardTsn(_forwardTsnPairsScratch, MaxOutboundForwardTsnPairs, out uint outstandingTarget))
			{
				SendForwardTsnPacket(outstandingTarget, _forwardTsnPairsScratch);
			}
		}

		/// <summary>
		///     Called under <see cref="_gate" /> from <see cref="Send" />, <see cref="HandleSack" />, and
		///     <see cref="OnTick" />'s T3-rtx branch: packs every chunk <see cref="_sendQueue" /> currently
		///     has ready (never yet sent, or marked for retransmission) into as many <see cref="SctpPacket.MaxSize" />
		///     packets as the send window allows, oldest TSN first. A pending delayed SACK
		///     (<see cref="_sackTimerArmed" />) bundles into the very first packet of the flush, if any -
		///     "a pending SACK bundles with outgoing DATA" is this task's job, a standalone one with no data
		///     to ride along is still <see cref="SendSackPacket" />'s and <see cref="MaybeSendSack" />'s.
		///     <para>
		///     Guarded by <see cref="_flushing" /> against the reentrancy this codebase's synchronous
		///     loopback wiring produces: sending a packet here can synchronously walk all the way into the
		///     peer's own reply landing back on <see cref="OnPacketReceived" />, which reacquires
		///     <see cref="_gate" /> (reentrant on this thread) and may call this method again. Rather than
		///     nesting - which would let an inner call free or repool linked-list nodes the outer call's
		///     loop still expects to advance through - the inner call is a no-op; the outer loop already
		///     re-reads live state (window, queue) on every iteration, so it picks up whatever the reentrant
		///     SACK processing just changed on its own next pass.
		///     </para>
		/// </summary>
		private void Flush()
		{
			if (_flushing) return;
			if (_sendQueue.PeekReadyToSend(_sendQueue.AvailableWindowBytes(_peerArwnd)) == null) return;

			_flushing = true;
			try
			{
				byte[] scratch = ArrayPool<byte>.Shared.Rent(SctpPacket.MaxSize);
				try
				{
					Span<byte> packet = scratch.AsSpan(0, SctpPacket.MaxSize);
					bool sackBundled = false;

					SctpSendQueue.PendingChunk chunk = _sendQueue.PeekReadyToSend(_sendQueue.AvailableWindowBytes(_peerArwnd));
					while (chunk != null)
					{
						int n = SctpPacket.WriteHeader(packet, _sctpPort, _sctpPort, _peerTag);
						int used = n;

						if (!sackBundled && _sackTimerArmed)
						{
							used += WriteSackChunkInto(packet.Slice(used));
							sackBundled = true;
							_sackTimerArmed = false;
							_dataPacketsSinceSack = 0;
						}

						long now = ClockNowMillis();
						while (chunk != null)
						{
							int valueLength = 12 + chunk.Length;
							int totalLength = SctpChunkCodec.HeaderLength + valueLength;
							int paddedLength = totalLength + ((4 - totalLength % 4) % 4);
							if (used + paddedLength > SctpPacket.MaxSize) break;

							var header = new DataChunkHeader(chunk.Tsn, chunk.StreamId, chunk.StreamSeq, chunk.Ppid, chunk.Unordered, chunk.Begin, chunk.End, immediateSack: false);
							used += header.WriteTo(packet.Slice(used), chunk.Buffer.AsSpan(0, chunk.Length));

							_sendQueue.MarkTransmitted(chunk, now);
							chunk = _sendQueue.PeekReadyToSend(_sendQueue.AvailableWindowBytes(_peerArwnd));
						}

						SctpPacket.FinishChecksum(packet.Slice(0, used));
						_sendPacket(packet.Slice(0, used));
					}
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(scratch);
				}
			}
			finally
			{
				_flushing = false;
			}
		}

		/// <summary>
		///     <paramref name="pairs" /> is already capped at <see cref="MaxOutboundForwardTsnPairs" /> by
		///     both of <see cref="MaybeSendForwardTsn" />'s callers into <see cref="SctpSendQueue" />
		///     (<see cref="SctpSendQueue.TryComputeForwardTsnAdvance" />, <see cref="SctpSendQueue.TryGetOutstandingForwardTsn" />),
		///     so the stack allocation below is sized from that fixed, compile-time bound - never from
		///     <paramref name="pairs" />.<c>Count</c> itself - and then sliced down to what is actually
		///     used, the same shape every other fixed-size stackalloc buffer in this class already uses.
		/// </summary>
		private void SendForwardTsnPacket(uint newCumulativeTsn, List<(ushort StreamId, ushort StreamSeq)> pairs)
		{
			Span<byte> pairBytes = stackalloc byte[MaxOutboundForwardTsnPairs * 4];
			pairBytes = pairBytes.Slice(0, pairs.Count * 4);
			for (int i = 0; i < pairs.Count; i++)
			{
				BinaryPrimitives.WriteUInt16BigEndian(pairBytes.Slice(i * 4, 2), pairs[i].StreamId);
				BinaryPrimitives.WriteUInt16BigEndian(pairBytes.Slice(i * 4 + 2, 2), pairs[i].StreamSeq);
			}

			var forwardTsn = new ForwardTsnChunk(newCumulativeTsn, pairBytes);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += forwardTsn.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		/// <summary>
		///     The chunk codecs (<see cref="SctpPacket.EnumerateChunks" />, <see cref="DataChunkHeader.TryParse" />)
		///     are span-only and untouched by the round-4b memory/sequence contract change: they hand back
		///     <paramref name="inner" /> as a span slice of <paramref name="packetSpan" />, not an offset.
		///     This locates that slice by reference (both spans are views over the same backing array) and
		///     returns the equivalent slice of <paramref name="packet" />, the original memory, so the
		///     zero-copy delivery path can hand out a <see cref="ReadOnlySequence{T}" /> (which cannot wrap
		///     a span) without copying the payload.
		/// </summary>
		private static ReadOnlyMemory<byte> SliceMemoryFor(ReadOnlyMemory<byte> packet, ReadOnlySpan<byte> packetSpan, ReadOnlySpan<byte> inner)
		{
			int offset = (int) Unsafe.ByteOffset(ref MemoryMarshal.GetReference(packetSpan), ref MemoryMarshal.GetReference(inner));
			return packet.Slice(offset, inner.Length);
		}

		/// <summary>
		///     Drains <see cref="SctpReceiveBuffer.Deliveries" /> (fragment-reassembly completions and any
		///     ordered messages a cascade unblocked), called once per chunk processed regardless of chunk
		///     type: the list is empty except right after a DATA chunk that completed one or more
		///     messages. <see cref="SafeInvokeOnMessage" /> is raised the same way <see cref="OnEstablished" />
		///     is, outside <see cref="_gate" /> - that discipline is unchanged.
		///     <para>
		///     The snapshot below (list contents copied out, and <see cref="SctpReceiveBuffer.Deliveries" />
		///     cleared) and every <see cref="SctpReceiveBuffer.ReleaseDelivery" /> call run under
		///     <see cref="_gate" />, so <see cref="SctpReceiveBuffer" />'s own free list is never mutated
		///     concurrently with anything else touching it - the callback itself stays outside the lock,
		///     since releasing a lease invokes no user code but the callback does. A
		///     reentrant <see cref="OnPacketReceived" /> from inside a subscriber's own
		///     callback (the shape every loopback test in this codebase uses) cannot truncate this
		///     drain by clearing the shared <see cref="SctpReceiveBuffer.Deliveries" /> list out from under
		///     it, because this method already copied its own batch out before releasing <see cref="_gate" />.
		///     <see cref="_drainInFlight" /> brackets the outside-the-lock section so <see cref="Teardown" />
		///     (possibly running on a different thread) can tell a drain is in progress and hand its own
		///     <see cref="SctpReceiveBuffer.Reset" /> off via <see cref="_pendingReset" /> instead of racing
		///     it; see that method's own remarks.
		///     </para>
		/// </summary>
		private void DeliverLeasedMessages()
		{
			List<SctpReceiveBuffer.LeasedDelivery> snapshot;
			lock (_gate)
			{
				if (_receiveBuffer.Deliveries.Count == 0) return;

				_drainInFlight = true;
				snapshot = new List<SctpReceiveBuffer.LeasedDelivery>(_receiveBuffer.Deliveries);
				_receiveBuffer.Deliveries.Clear();
			}

			try
			{
				for (int i = 0; i < snapshot.Count; i++)
				{
					SctpReceiveBuffer.LeasedDelivery delivery = snapshot[i];
					try
					{
						ReadOnlySequence<byte> sequence = delivery.Sequence;
						SafeInvokeOnMessage(delivery.StreamId, delivery.Ppid, in sequence);
					}
					finally
					{
						lock (_gate) _receiveBuffer.ReleaseDelivery(delivery);
					}
				}
			}
			finally
			{
				lock (_gate)
				{
					_drainInFlight = false;
					if (_pendingReset)
					{
						_pendingReset = false;
						_receiveBuffer.Reset(_peerInitialTsn);
					}
				}
			}
		}

		/// <summary>
		///     The hot-path law (see this file's class remarks and CLAUDE.md: an unhandled throw on a
		///     receive path is a hard defect) applied to a third party's code we do not control: a
		///     subscriber that throws must not kill the transport. Caught and logged here, then the caller
		///     continues - the next delivery in the same batch, the rest of this packet's chunks, and
		///     every later packet all keep flowing. The exception is deliberately NOT rethrown after
		///     cleanup: <see cref="OnPacketReceived" /> has no way to report a subscriber's bug back to its
		///     own caller (the mux receive loop) without also making every well-behaved caller's session
		///     die for a fault outside this class's own code.
		/// </summary>
		private void SafeInvokeOnMessage(ushort streamId, uint ppid, in ReadOnlySequence<byte> message)
		{
			try
			{
				OnMessage?.Invoke(streamId, ppid, in message);
			}
			catch (Exception ex)
			{
				Log.Error("SctpAssociation.OnMessage subscriber threw; message dropped, transport continues.", ex);
			}
		}

		/// <summary>
		///     RFC 4960 6.2 SACK policy, called under <see cref="_gate" /> once per received packet that
		///     carried at least one DATA chunk: sends immediately when <paramref name="immediateSackRequested" />
		///     (the I-flag was set on some DATA chunk in the packet) or a gap is outstanding; otherwise
		///     every second such packet, with the 200ms fallback armed here and enforced by <see cref="OnTick" />.
		///     Gated on <see cref="SctpState.Established" />: a DATA or FORWARD-TSN chunk arriving on an
		///     association that is not (yet, or no longer) Established must never provoke a SACK off
		///     <see cref="_receiveBuffer" /> state that may be stale, reset, or not yet negotiated.
		///     <see cref="Teardown" /> disarms the 200ms fallback separately; this is the gate on the
		///     receive-driven trigger.
		/// </summary>
		private void MaybeSendSack(bool immediateSackRequested)
		{
			if (_state != SctpState.Established) return;

			bool sendNow = immediateSackRequested || _receiveBuffer.HasGap;

			if (!sendNow)
			{
				_dataPacketsSinceSack++;
				sendNow = _dataPacketsSinceSack >= 2;
			}

			if (sendNow)
			{
				SendSackPacket();
				_dataPacketsSinceSack = 0;
				_sackTimerArmed = false;
			}
			else if (!_sackTimerArmed)
			{
				_sackTimerArmed = true;
				_sackTimerArmedAtTicks = Environment.TickCount64;
			}
		}

		/// <summary>
		///     Called under <see cref="_gate" />. Answers every well-formed INIT with a fresh INIT-ACK
		///     regardless of <see cref="_state" />, EXCEPT <see cref="SctpState.Aborted" />: the responder
		///     path commits nothing to memory here for any state before that (see class remarks), so there
		///     is no "already in progress" state to protect against a retransmitted or duplicated INIT,
		///     only a fresh cookie each time. <see cref="SctpState.Aborted" /> is different: this object
		///     maps 1:1 to one <see cref="RtcPeer" />'s single-use transport for its whole life, so once
		///     torn down it must never answer anything again - a fresh INIT arriving after that point (a
		///     genuinely new connection attempt on the same tag/ports, or hostile replay) gets no reply,
		///     matching "once Aborted, no handler may transition state or send" (fix-round: a resurrection
		///     hazard found and closed for <see cref="HandleCookieEcho" /> first; audited onto every other
		///     handshake handler, this one included, since a stateless responder handler is exactly the
		///     kind of code most likely to be an overlooked exception to that rule).
		///     <para>
		///     Fix-round (Critical, Task 8 review): a self-initiated instance is not unconditionally
		///     excluded from answering an INIT the way it was before this round. RFC 4960 5.2.1's
		///     simultaneous-INIT collision - both sides start before receiving anything from the other -
		///     lands exactly here: an INIT arriving while this side is itself in
		///     <see cref="SctpState.CookieWait" /> (already sent its own, still awaiting the reply) is the
		///     peer's own INIT crossing ours in flight, not hostile or stale, and answering it is what lets
		///     both sides converge instead of each dropping the other's opening chunk and retrying to
		///     exhaustion. This is the convergent subset of 5.2.1 this stack implements, not the fuller
		///     duplicate-association tie-break RFC 4960 5.2.2-5.2.4 describe for a restart after one side
		///     already reached <see cref="SctpState.Established" /> or <see cref="SctpState.CookieEchoed" />
		///     - those remain dropped-and-counted exactly as before. The collision answer reuses this
		///     side's OWN EXISTING tag and initial TSN (never a fresh pair) per RFC 4960 5.2.1's own rule:
		///     the peer must be able to recognize the reply as completing the SAME identity it already has
		///     half of, and <see cref="_state" /> is deliberately left at <see cref="SctpState.CookieWait" />
		///     rather than touched here - this side's own outstanding INIT/COOKIE-ECHO round trip
		///     (<see cref="HandleInitAck" />/<see cref="HandleCookieAck" />) is left to run its course and
		///     either completes or quietly finds itself already <see cref="SctpState.Established" /> once
		///     the peer's own COOKIE-ECHO reaches <see cref="HandleCookieEcho" /> instead (see that
		///     method's own remarks for its matching half of this convergence).
		///     </para>
		///     <para>
		///     Deliberately gated on <see cref="_state" /> alone, never <see cref="_isClient" />: a first
		///     attempt at this fix read <c>_isClient</c> here and broke on its own test, because
		///     <c>_isClient</c> can be <see langword="true" /> from construction (the DTLS-role designation
		///     a caller passes in) long before <see cref="Start" /> itself ever runs - a real shape once
		///     <see cref="Start" /> stopped being called unconditionally for every DTLS-client-designated
		///     instance (see that method's own remarks): a peer's association can sit at
		///     <see cref="SctpState.Closed" /> with <c>_isClient</c> already true from birth, having still
		///     never actually chosen a tag or TSN. <see cref="_state" /> has no such ambiguity - only
		///     <see cref="Start" /> ever sets <see cref="SctpState.CookieWait" />, and it does so atomically
		///     with everything a collision answer needs (<see cref="_localTag" />, <see cref="_localInitialTsn" />)
		///     already validly chosen - so it alone is what "has this instance genuinely self-initiated"
		///     actually means here.
		///     </para>
		/// </summary>
		private void HandleInit(ReadOnlySpan<byte> value, uint verificationTag)
		{
			if (_state == SctpState.Aborted)
			{
				CountIgnored();
				return;
			}

			if (_state == SctpState.CookieEchoed || _state == SctpState.Established)
			{
				// Already progressed this side's own self-initiated handshake past the point an incoming
				// INIT can converge with (RFC 4960 5.2.2-5.2.4's fuller duplicate-association tie-break,
				// out of scope - see this method's own remarks). Only reachable by an instance that itself
				// already called Start (nothing else ever reaches these two states), so this never touches
				// a pure responder.
				CountIgnored();
				return;
			}

			bool isCollision = _state == SctpState.CookieWait;

			// RFC 4960 5.1: the packet carrying an INIT chunk MUST set the verification tag to 0.
			if (verificationTag != 0)
			{
				CountIgnored();
				return;
			}

			if (!InitChunk.TryParse(value, out InitChunk init))
			{
				CountIgnored();
				return;
			}

			// Collision: reuse our own existing identity (RFC 4960 5.2.1). Otherwise: the ordinary
			// stateless-responder path, a fresh identity per INIT answered.
			uint ourTag = isCollision ? _localTag : RandomUInt32();
			uint ourInitialTsn = isCollision ? _localInitialTsn : RandomUInt32();
			ushort outboundStreams = Math.Min(StreamCount, init.InboundStreams);
			ushort inboundStreams = Math.Min(StreamCount, init.OutboundStreams);

			byte[] cookie = CreateCookie(init.InitiateTag, ourTag, init.Arwnd, init.OutboundStreams, init.InboundStreams, init.InitialTsn, ourInitialTsn, Environment.TickCount64);
			var initAck = new InitChunk(ourTag, _arwndBudget, outboundStreams, inboundStreams, ourInitialTsn, forwardTsnSupported: true, cookie);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, init.InitiateTag);
			n += initAck.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));

			_lastRespondedInitAtTicks = Environment.TickCount64;
		}

		/// <summary>Client role, called under <see cref="_gate" />: answers an INIT-ACK with COOKIE-ECHO.</summary>
		private void HandleInitAck(ReadOnlySpan<byte> value, uint verificationTag)
		{
			if (!_isClient || _state != SctpState.CookieWait)
			{
				CountIgnored();
				return;
			}

			// RFC 4960 5.1: the INIT-ACK packet carries Tag_A, the tag we chose for our own INIT.
			if (verificationTag != _localTag)
			{
				CountIgnored();
				return;
			}

			if (!InitChunk.TryParse(value, out InitChunk initAck) || initAck.StateCookie.IsEmpty)
			{
				CountIgnored();
				return;
			}

			// A State Cookie this side could never echo back in a COOKIE-ECHO of its own: rejected before
			// it is ever retained, rather than retained and only failing later - on every retransmit -
			// once SendCookieEchoPacket actually tries to write it (see MaxCookieEchoCookieLength's own
			// remarks).
			if (initAck.StateCookie.Length > MaxCookieEchoCookieLength)
			{
				CountIgnored();
				return;
			}

			_peerTag = initAck.InitiateTag;
			_peerInitialTsn = initAck.InitialTsn;
			_peerArwnd = initAck.Arwnd;
			_peerOutboundStreams = initAck.OutboundStreams;
			_peerInboundStreams = initAck.InboundStreams;
			_cookie = initAck.StateCookie.ToArray();

			_state = SctpState.CookieEchoed;
			ResetRetransmitState();
			SendCookieEchoPacket();
		}

		/// <summary>
		///     Server role, called under <see cref="_gate" />: the only place association state actually
		///     materializes. A duplicate, already-established COOKIE-ECHO (the client retransmitting
		///     because our COOKIE-ACK was lost) is answered again without re-firing <see cref="OnEstablished" />.
		///     Returns true when this call is the one that transitions to <see cref="SctpState.Established" />;
		///     the caller raises <see cref="OnEstablished" /> itself, after releasing <see cref="_gate" />.
		///     <para>
		///     Fix round (resurrection hazard): <see cref="_localTag" /> is never invalidated by
		///     <see cref="Teardown" />, and a signed cookie stays HMAC-valid for <see cref="CookieMaxAgeMillis" />
		///     (60s) regardless of what has happened to this association since it was minted, so a
		///     network-level retransmit of the client's own, still-perfectly-valid original COOKIE-ECHO can
		///     arrive well after this side has already torn down (a local <see cref="Abort" />, or an
		///     inbound ABORT/SHUTDOWN). Before this fix, <c>alreadyEstablished</c> below was computed only
		///     from <c>_state == Established</c>, which is false once <see cref="SctpState.Aborted" />, so
		///     the "not already established" branch ran again: overwriting the tags TryValidateCookie just
		///     re-derived (harmless, since they are the SAME values by construction) but also calling
		///     <see cref="SctpReceiveBuffer.Reset" />/<see cref="SctpSendQueue.Reset" /> (wiping
		///     <see cref="Teardown" />'s own cleanup) and flipping <see cref="_state" /> back to
		///     <see cref="SctpState.Established" />, resending COOKIE-ACK and re-firing
		///     <see cref="OnEstablished" /> a second time on an association <see cref="RtcPeer.Dispose" />
		///     already considers dead. The explicit <see cref="SctpState.Aborted" /> check below closes
		///     this the same way <see cref="HandleCookieAck" /> already closed it on the client side (its
		///     own <c>_state != CookieEchoed</c> guard already excludes <see cref="SctpState.Aborted" />
		///     implicitly, which is why it needed no equivalent fix): once Aborted, this handler now
		///     ignores-and-counts unconditionally, before even validating the cookie, so nothing about a
		///     replayed COOKIE-ECHO - correct tag or not, still-valid cookie or not - can ever resurrect a
		///     torn-down association.
		///     </para>
		///     <para>
		///     Fix-round (Critical, Task 8 review): a self-initiated instance is no longer unconditionally
		///     excluded here either, matching <see cref="HandleInit" />'s own collision-convergence half.
		///     When both sides start before receiving anything from each other (see that method's own
		///     remarks), each answers the other's INIT with its own EXISTING identity, so the COOKIE-ECHO
		///     this side eventually gets back is the peer choosing to complete THIS side's offered
		///     identity's mirror - legitimate, not the "only servers receive COOKIE-ECHO" case the old
		///     unconditional guard assumed. No extra state check is needed to allow it: unlike
		///     <see cref="HandleInit" />, this handler already ran (before this fix-round, and still) with
		///     no gate here beyond <see cref="SctpState.Aborted" /> for the ordinary responder path,
		///     because <see cref="TryValidateCookie" /> - a valid, still-fresh, correctly-HMAC-signed
		///     cookie this exact instance minted - is already the real legitimacy check; a self-initiated
		///     instance reaching this method with a valid cookie for its own identity is no different in
		///     kind. (Gated on <see cref="_state" />, deliberately not <see cref="_isClient" />, for the
		///     exact reason <see cref="HandleInit" />'s own remarks give: the former is always accurate,
		///     the latter can be <see langword="true" /> from construction long before anything has
		///     actually been sent.)
		///     </para>
		/// </summary>
		private bool HandleCookieEcho(ReadOnlySpan<byte> value, uint verificationTag)
		{
			if (_state == SctpState.Aborted)
			{
				CountIgnored();
				return false;
			}

			if (!TryValidateCookie(value, Environment.TickCount64, out uint peerInitiateTag, out uint ourTag, out uint peerArwnd,
					out ushort peerOutboundStreams, out ushort peerInboundStreams, out uint peerInitialTsn, out uint ourInitialTsn))
			{
				CountIgnored();
				return false;
			}

			// RFC 4960 5.1: the COOKIE-ECHO packet carries Tag_B, the tag the cookie says we chose.
			if (verificationTag != ourTag)
			{
				CountIgnored();
				return false;
			}

			bool alreadyEstablished = _state == SctpState.Established && _localTag == ourTag && _peerTag == peerInitiateTag;

			if (!alreadyEstablished)
			{
				_localTag = ourTag;
				_peerTag = peerInitiateTag;
				_localInitialTsn = ourInitialTsn;
				_peerInitialTsn = peerInitialTsn;
				_peerArwnd = peerArwnd;
				_peerOutboundStreams = peerOutboundStreams;
				_peerInboundStreams = peerInboundStreams;
			}

			// Sent before OnEstablished fires (the caller raises it only after this method returns and
			// _gate is released), so the wire order is preserved: COOKIE-ACK still goes out before the
			// established callback ever runs.
			SendCookieAckPacket();

			if (alreadyEstablished) return false;

			_receiveBuffer.Reset(peerInitialTsn);
			_dataPacketsSinceSack = 0;
			_sackTimerArmed = false;

			_nextOutboundTsn = _localInitialTsn;
			_nextOutboundSeqByStream.Clear();
			_sendQueue.Reset(_localInitialTsn);

			_state = SctpState.Established;
			return true;
		}

		/// <summary>
		///     Client role, called under <see cref="_gate" />. Returns true when this call is the one that
		///     transitions to <see cref="SctpState.Established" />; the caller raises
		///     <see cref="OnEstablished" /> itself, after releasing <see cref="_gate" />.
		/// </summary>
		private bool HandleCookieAck(uint verificationTag)
		{
			if (!_isClient)
			{
				CountIgnored();
				return false;
			}

			if (_state != SctpState.CookieEchoed)
			{
				// A duplicate COOKIE-ACK (our own retransmitted COOKIE-ECHO drew a second reply) is
				// harmless once already established; anything else at this point is unexpected.
				if (_state != SctpState.Established || verificationTag != _localTag) CountIgnored();
				return false;
			}

			// RFC 4960 5.1: the COOKIE-ACK packet carries Tag_A, our own tag.
			if (verificationTag != _localTag)
			{
				CountIgnored();
				return false;
			}

			_receiveBuffer.Reset(_peerInitialTsn);
			_dataPacketsSinceSack = 0;
			_sackTimerArmed = false;

			_nextOutboundTsn = _localInitialTsn;
			_nextOutboundSeqByStream.Clear();
			_sendQueue.Reset(_localInitialTsn);

			_state = SctpState.Established;
			return true;
		}

		private void ResetRetransmitState()
		{
			_attemptCount = 1;
			_rtoMillis = RtoInitialMillis;
			_lastSentAtTicks = Environment.TickCount64;
		}

		private void SendInitPacket()
		{
			var init = new InitChunk(_localTag, _arwndBudget, StreamCount, StreamCount, _localInitialTsn, forwardTsnSupported: true);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, 0); // RFC 4960 5.1: tag 0 on the packet carrying INIT
			n += init.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		private void SendCookieEchoPacket()
		{
			var cookieEcho = new CookieEchoChunk(_cookie);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += cookieEcho.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		/// <summary>Called under <see cref="_gate" /> by <see cref="MaybeSendSack" /> and <see cref="OnTick" />'s 200ms fallback: a standalone SACK packet, with no outbound DATA to ride along.</summary>
		private void SendSackPacket()
		{
			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += WriteSackChunkInto(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		/// <summary>Shared by <see cref="SendSackPacket" /> and <see cref="Flush" />'s bundling case: writes one SACK chunk (current cumulative ack, gap blocks, duplicate TSNs) into <paramref name="destination" />, returning the padded length written. Goes straight through <see cref="SackChunk" />'s span-taking static <c>WriteTo</c> overload rather than boxing the stack-allocated spans below into a <see cref="SackChunk" /> instance first: this runs on every SACK (RFC 4960 6.2's every-other-packet cadence makes that steady-state, not occasional), so the two <c>ToArray()</c> calls that shape used to cost here were a real per-message heap allocation, not a one-time cost.</summary>
		private int WriteSackChunkInto(Span<byte> destination)
		{
			Span<SackChunk.GapBlock> gapBlocks = stackalloc SackChunk.GapBlock[SackChunk.MaxGapBlocks];
			int gapCount = _receiveBuffer.BuildGapBlocks(gapBlocks);

			Span<uint> duplicateTsns = stackalloc uint[SackChunk.MaxDuplicateTsns];
			int duplicateCount = _receiveBuffer.DrainDuplicateTsns(duplicateTsns);

			return SackChunk.WriteTo(destination, _receiveBuffer.CumulativeTsnAck, _receiveBuffer.CurrentArwnd,
				gapBlocks.Slice(0, gapCount), duplicateTsns.Slice(0, duplicateCount));
		}

		private void SendCookieAckPacket()
		{
			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += SctpChunkCodec.FinishChunk(buffer.Slice(n), CookieAckChunkType, 0, 0);
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		/// <summary>
		///     Called under <see cref="_gate" /> from <see cref="HandleHeartbeat" />: <paramref name="info" />
		///     is the peer's own Heartbeat Info parameter, echoed back verbatim per RFC 4960 8.3 - it is
		///     still a slice of the packet <see cref="OnPacketReceived" /> is currently processing, valid for
		///     this whole call chain, so no copy or retention is needed before it is written into the reply.
		/// </summary>
		private void SendHeartbeatAckPacket(ReadOnlySpan<byte> info)
		{
			var ack = new HeartbeatChunk(info, isAck: true);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += ack.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		/// <summary>Called under <see cref="_gate" /> from <see cref="HandleShutdown" />: SHUTDOWN-ACK (RFC 4960 3.2, type 8), empty value - the same generic-chunk-writer shape <see cref="SendCookieAckPacket" /> already uses for COOKIE-ACK, no dedicated codec struct needed.</summary>
		private void SendShutdownAckPacket()
		{
			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += SctpChunkCodec.FinishChunk(buffer.Slice(n), ShutdownAckChunkType, 0, 0);
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		/// <summary>Called under <see cref="_gate" /> from <see cref="Abort" />: ABORT (RFC 4960 3.2, type 6), no error-cause TLVs - this side never has anything more specific than "the owner tore this down" to report.</summary>
		private void SendAbortPacket()
		{
			var abort = new AbortChunk(ReadOnlySpan<byte>.Empty);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += abort.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
		}

		private void CountIgnored()
		{
			Interlocked.Increment(ref _ignoredPacketCount);
		}

		private static uint RandomUInt32()
		{
			Span<byte> bytes = stackalloc byte[4];
			RandomNumberGenerator.Fill(bytes);
			return BinaryPrimitives.ReadUInt32BigEndian(bytes);
		}

		/// <summary>
		///     The testable seam for cookie age (documented in the task report): the same factory the
		///     server uses to answer a real INIT, exposed internally so a test can fabricate an otherwise
		///     valid, correctly signed cookie whose embedded timestamp is already older than
		///     <see cref="CookieMaxAgeMillis" />. Validation always runs against the real clock
		///     (<see cref="Environment.TickCount64" /> at the point of receipt), never a shimmed one.
		/// </summary>
		internal static byte[] CreateCookie(uint peerInitiateTag, uint ourTag, uint peerArwnd, ushort peerOutboundStreams, ushort peerInboundStreams, uint peerInitialTsn, uint ourInitialTsn, long timestampMillis)
		{
			var cookie = new byte[CookieLength];
			Span<byte> plain = cookie.AsSpan(0, CookiePlainLength);
			BinaryPrimitives.WriteUInt32BigEndian(plain.Slice(0, 4), peerInitiateTag);
			BinaryPrimitives.WriteUInt32BigEndian(plain.Slice(4, 4), ourTag);
			BinaryPrimitives.WriteUInt32BigEndian(plain.Slice(8, 4), peerArwnd);
			BinaryPrimitives.WriteUInt16BigEndian(plain.Slice(12, 2), peerOutboundStreams);
			BinaryPrimitives.WriteUInt16BigEndian(plain.Slice(14, 2), peerInboundStreams);
			BinaryPrimitives.WriteUInt32BigEndian(plain.Slice(16, 4), peerInitialTsn);
			BinaryPrimitives.WriteUInt32BigEndian(plain.Slice(20, 4), ourInitialTsn);
			BinaryPrimitives.WriteInt64BigEndian(plain.Slice(24, 8), timestampMillis);

			HMACSHA256.HashData(CookieHmacKey, plain, cookie.AsSpan(CookiePlainLength, CookieHmacLength));
			return cookie;
		}

		private static bool TryValidateCookie(ReadOnlySpan<byte> cookie, long nowMillis, out uint peerInitiateTag, out uint ourTag, out uint peerArwnd,
			out ushort peerOutboundStreams, out ushort peerInboundStreams, out uint peerInitialTsn, out uint ourInitialTsn)
		{
			peerInitiateTag = 0;
			ourTag = 0;
			peerArwnd = 0;
			peerOutboundStreams = 0;
			peerInboundStreams = 0;
			peerInitialTsn = 0;
			ourInitialTsn = 0;

			if (cookie.Length != CookieLength) return false;

			ReadOnlySpan<byte> plain = cookie.Slice(0, CookiePlainLength);
			ReadOnlySpan<byte> receivedHmac = cookie.Slice(CookiePlainLength, CookieHmacLength);

			Span<byte> expectedHmac = stackalloc byte[CookieHmacLength];
			HMACSHA256.HashData(CookieHmacKey, plain, expectedHmac);
			if (!CryptographicOperations.FixedTimeEquals(receivedHmac, expectedHmac)) return false;

			long timestampMillis = BinaryPrimitives.ReadInt64BigEndian(plain.Slice(24, 8));
			if (nowMillis - timestampMillis > CookieMaxAgeMillis) return false;

			peerInitiateTag = BinaryPrimitives.ReadUInt32BigEndian(plain.Slice(0, 4));
			ourTag = BinaryPrimitives.ReadUInt32BigEndian(plain.Slice(4, 4));
			peerArwnd = BinaryPrimitives.ReadUInt32BigEndian(plain.Slice(8, 4));
			peerOutboundStreams = BinaryPrimitives.ReadUInt16BigEndian(plain.Slice(12, 2));
			peerInboundStreams = BinaryPrimitives.ReadUInt16BigEndian(plain.Slice(14, 2));
			peerInitialTsn = BinaryPrimitives.ReadUInt32BigEndian(plain.Slice(16, 4));
			ourInitialTsn = BinaryPrimitives.ReadUInt32BigEndian(plain.Slice(20, 4));
			return true;
		}
	}
}