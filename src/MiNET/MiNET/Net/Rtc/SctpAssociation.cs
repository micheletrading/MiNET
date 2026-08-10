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

		// RFC 4960 6.2 SACK policy: a SACK goes out on the second packet carrying DATA, or 200ms after
		// the first unacked one, whichever comes first (plus the immediate triggers HandleData/MaybeSendSack
		// check for separately).
		private const long SackDelayMillis = 200;

		// RFC 4960 3.2 defines COOKIE-ACK as chunk type 11, empty value. SctpChunks.cs has no struct
		// for it (there is nothing to parse or write beyond the shared 4-byte chunk header), so it is
		// handled here directly through the internal SctpChunkCodec both files already share.
		private const byte CookieAckChunkType = 11;

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

		private readonly bool _isClient;
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

		// Handshake retransmit state (client role only; the server never arms a timer, see the class
		// remarks). Reset to a fresh RTO cycle whenever a new chunk starts waiting for a reply (INIT at
		// Start, COOKIE-ECHO once the INIT-ACK arrives).
		private int _attemptCount;
		private long _rtoMillis;
		private long _lastSentAtTicks;

		private long _ignoredPacketCount;

		private readonly SctpReceiveBuffer _receiveBuffer;
		private int _dataPacketsSinceSack;
		private bool _sackTimerArmed;
		private long _sackTimerArmedAtTicks;

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

		/// <summary>
		///     Client role: sends the opening INIT and arms the retransmit timer. Server role: nothing to
		///     do, it only ever reacts to <see cref="OnPacketReceived" />.
		/// </summary>
		public void Start()
		{
			if (!_isClient) return;

			lock (_gate)
			{
				_localTag = RandomUInt32();
				_localInitialTsn = RandomUInt32();
				_state = SctpState.CookieWait;
				ResetRetransmitState();
				SendInitPacket();
			}
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
		///     Handshake retransmit backoff for the client role only: the server never owns a timer here,
		///     see the class remarks. Runs on whatever thread the owner calls it from (a different one
		///     than <see cref="OnPacketReceived" /> in the real mux wiring), so both share <see cref="_gate" />.
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
					MaybeSendForwardTsn();
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
		/// </summary>
		public void OnPacketReceived(ReadOnlyMemory<byte> packet)
		{
			ReadOnlySpan<byte> packetSpan = packet.Span;

			if (!SctpPacket.TryReadHeader(packetSpan, out _, out _, out uint verificationTag))
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
							packetHadData = true;
							hasZeroCopyDelivery = HandleData(flags, value, packet, packetSpan, out zcStreamId, out zcPpid, out zcPayload, out bool chunkWantsImmediateSack);
							if (chunkWantsImmediateSack) immediateSackRequested = true;
							break;

						case SctpChunkType.Sack:
							HandleSack(value);
							break;

						case SctpChunkType.ForwardTsn:
							// A FORWARD-TSN moves our receive cumulative just like DATA can, so the peer
							// should get a SACK reflecting the new point.
							packetHadData = HandleForwardTsn(value) || packetHadData;
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
		/// </summary>
		private bool HandleData(byte flags, ReadOnlySpan<byte> value, ReadOnlyMemory<byte> packet, ReadOnlySpan<byte> packetSpan, out ushort streamId, out uint ppid, out ReadOnlyMemory<byte> zcPayload, out bool immediateSackRequested)
		{
			streamId = 0;
			ppid = 0;
			zcPayload = default;
			immediateSackRequested = false;

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
		///     retransmit abandoning a chunk, or the window opening back up).
		/// </summary>
		private void HandleSack(ReadOnlySpan<byte> value)
		{
			if (!SackChunk.TryParse(value, out SackChunk sack))
			{
				CountIgnored();
				return;
			}

			if (_state != SctpState.Established)
			{
				CountIgnored();
				return;
			}

			_peerArwnd = sack.Arwnd;
			_sendQueue.OnSackReceived(sack.CumulativeTsnAck, sack.GapBlocks, ClockNowMillis());

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
			Span<(ushort StreamId, ushort StreamSeq)> pairs = stackalloc (ushort, ushort)[pairCount];
			for (int i = 0; i < pairCount; i++) pairs[i] = chunk.GetPair(i);

			_receiveBuffer.AdvanceCumulative(chunk.NewCumulativeTsn, pairs);
			return true;
		}

		/// <summary>
		///     Called under <see cref="_gate" /> after anything that might have abandoned a chunk (T3-rtx in
		///     <see cref="OnTick" />, fast retransmit inside <see cref="HandleSack" />): sends a FORWARD-TSN
		///     when <see cref="_sendQueue" /> can now advertise further than it already has.
		/// </summary>
		private void MaybeSendForwardTsn()
		{
			if (_sendQueue.TryComputeForwardTsnAdvance(_forwardTsnPairsScratch, out uint newTarget))
			{
				SendForwardTsnPacket(newTarget, _forwardTsnPairsScratch);
				_sendQueue.MarkForwardTsnAdvertised(newTarget);
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

		private void SendForwardTsnPacket(uint newCumulativeTsn, List<(ushort StreamId, ushort StreamSeq)> pairs)
		{
			Span<byte> pairBytes = stackalloc byte[pairs.Count * 4];
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
		///     messages. Raised the same way <see cref="OnEstablished" /> is, outside <see cref="_gate" />.
		///     Every leased fragment buffer and pooled segment node backing a delivery's sequence is
		///     returned exactly once, in a <c>finally</c>, regardless of whether the subscriber threw.
		/// </summary>
		private void DeliverLeasedMessages()
		{
			List<SctpReceiveBuffer.LeasedDelivery> deliveries = _receiveBuffer.Deliveries;
			for (int i = 0; i < deliveries.Count; i++)
			{
				SctpReceiveBuffer.LeasedDelivery delivery = deliveries[i];
				try
				{
					ReadOnlySequence<byte> sequence = delivery.Sequence;
					SafeInvokeOnMessage(delivery.StreamId, delivery.Ppid, in sequence);
				}
				finally
				{
					_receiveBuffer.ReleaseDelivery(delivery);
				}
			}

			deliveries.Clear();
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
		/// </summary>
		private void MaybeSendSack(bool immediateSackRequested)
		{
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
		///     Server role, called under <see cref="_gate" />. Answers every well-formed INIT with a fresh
		///     INIT-ACK regardless of <see cref="_state" />: the server commits nothing to memory here (see
		///     class remarks), so there is no "already in progress" state to protect against a retransmitted
		///     or duplicated INIT, only a fresh cookie each time.
		/// </summary>
		private void HandleInit(ReadOnlySpan<byte> value, uint verificationTag)
		{
			if (_isClient)
			{
				CountIgnored();
				return;
			}

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

			uint ourTag = RandomUInt32();
			uint ourInitialTsn = RandomUInt32();
			ushort outboundStreams = Math.Min(StreamCount, init.InboundStreams);
			ushort inboundStreams = Math.Min(StreamCount, init.OutboundStreams);

			byte[] cookie = CreateCookie(init.InitiateTag, ourTag, init.Arwnd, init.OutboundStreams, init.InboundStreams, init.InitialTsn, ourInitialTsn, Environment.TickCount64);
			var initAck = new InitChunk(ourTag, _arwndBudget, outboundStreams, inboundStreams, ourInitialTsn, forwardTsnSupported: true, cookie);

			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, init.InitiateTag);
			n += initAck.WriteTo(buffer.Slice(n));
			SctpPacket.FinishChecksum(buffer.Slice(0, n));
			_sendPacket(buffer.Slice(0, n));
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
		/// </summary>
		private bool HandleCookieEcho(ReadOnlySpan<byte> value, uint verificationTag)
		{
			if (_isClient)
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

		/// <summary>Shared by <see cref="SendSackPacket" /> and <see cref="Flush" />'s bundling case: writes one SACK chunk (current cumulative ack, gap blocks, duplicate TSNs) into <paramref name="destination" />, returning the padded length written.</summary>
		private int WriteSackChunkInto(Span<byte> destination)
		{
			Span<SackChunk.GapBlock> gapBlocks = stackalloc SackChunk.GapBlock[SackChunk.MaxGapBlocks];
			int gapCount = _receiveBuffer.BuildGapBlocks(gapBlocks);

			Span<uint> duplicateTsns = stackalloc uint[SackChunk.MaxDuplicateTsns];
			int duplicateCount = _receiveBuffer.DrainDuplicateTsns(duplicateTsns);

			var sack = new SackChunk(_receiveBuffer.CumulativeTsnAck, _receiveBuffer.CurrentArwnd,
				gapBlocks.Slice(0, gapCount).ToArray(), duplicateTsns.Slice(0, duplicateCount).ToArray());

			return sack.WriteTo(destination);
		}

		private void SendCookieAckPacket()
		{
			Span<byte> buffer = stackalloc byte[SctpPacket.MaxSize];
			int n = SctpPacket.WriteHeader(buffer, _sctpPort, _sctpPort, _peerTag);
			n += SctpChunkCodec.FinishChunk(buffer.Slice(n), CookieAckChunkType, 0, 0);
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