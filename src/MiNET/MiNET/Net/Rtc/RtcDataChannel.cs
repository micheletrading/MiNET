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
using System.Text;
using System.Threading;
using log4net;

namespace MiNET.Net.Rtc
{
	/// <summary>Raised per delivered application message on a <see cref="RtcDataChannel" />, mirroring <see cref="SctpAssociation.OnMessage" />'s delivery contract: <paramref name="data" /> is valid only for the duration of the callback. <paramref name="isString" /> reflects the DCEP/RFC 8831 PPID the message rode in on (string PPIDs 51/56 vs. binary 53/57), not anything inspected in the bytes themselves.</summary>
	public delegate void ChannelMessageHandler(in ReadOnlySequence<byte> data, bool isString);

	/// <summary>
	///     One negotiated DCEP (RFC 8832) data channel riding a single SCTP stream of an established
	///     <see cref="SctpAssociation" />. Constructed only by <see cref="RtcChannelManager" />, which
	///     owns the DCEP handshake (OPEN/ACK) and stream id bookkeeping; this class is just the public
	///     face a consumer sends and receives through, plus the RFC 8831 data-PPID bookkeeping
	///     (string/binary, and the -empty variants SCTP's inability to carry zero-length user data
	///     forces on the wire) that only concerns messages, not negotiation.
	/// </summary>
	public class RtcDataChannel
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RtcDataChannel));

		// RFC 8831 3.2: PPIDs for data messages. A channel's own reliability/ordering semantics (not
		// these PPIDs) govern how the message rides the wire; the PPID only tells the far end how to
		// interpret the payload (and, for the -empty pair, that there IS no payload - SCTP cannot carry
		// a zero-length DATA chunk, so an empty message goes out as one padding byte under one of these
		// two PPIDs instead, and is reconstructed as empty again on arrival here).
		private const uint PpidString = 51;
		private const uint PpidBinary = 53;
		private const uint PpidStringEmpty = 56;
		private const uint PpidBinaryEmpty = 57;

		private readonly SctpAssociation _association;

		public string Label { get; }
		public ushort StreamId { get; }
		public bool Ordered { get; }

		/// <summary>-1 means fully reliable; a non-negative value is the RFC 3758 partial-reliability retransmit budget, passed straight through to <see cref="SctpAssociation.Send" />.</summary>
		public int MaxRetransmits { get; }

		/// <summary>True once negotiation completed: immediately for an inbound channel (RFC 8832: the OPEN receiver may use the channel right away), or once the peer's ACK arrives for an outbound one.</summary>
		public bool IsOpen { get; private set; }

		/// <summary>
		///     RFC 8832 6 MUST: "the sending side MUST NOT send messages out of order until the
		///     DATA_CHANNEL_ACK message, or any message, has been received on the channel" - both triggers
		///     end this window (<see cref="RaiseOpen" /> for the ACK case, <see cref="DeliverData" /> for
		///     the any-message case), not only <see cref="IsOpen" />, which is why this is its own field
		///     rather than reusing that property. See <see cref="Send" /> for what riding inside this
		///     window actually changes.
		/// </summary>
		private bool _canUseNegotiatedSemantics;

		/// <summary>Raised at most once, exactly when <see cref="IsOpen" /> flips to true.</summary>
		public event Action OnOpen;

		/// <summary>Raised inline, outside any lock, once per delivered message. See <see cref="ChannelMessageHandler" />'s own remarks for the delivery contract.</summary>
		public event ChannelMessageHandler OnMessage;

		internal RtcDataChannel(SctpAssociation association, ushort streamId, string label, bool ordered, int maxRetransmits)
		{
			_association = association;
			StreamId = streamId;
			Label = label;
			Ordered = ordered;
			MaxRetransmits = maxRetransmits;
		}

		/// <summary>Called by <see cref="RtcChannelManager" /> once negotiation completes for this channel (immediately for an inbound channel, on ACK receipt for an outbound one). Idempotent is not needed: the manager only ever calls this once per channel.</summary>
		internal void RaiseOpen()
		{
			IsOpen = true;
			_canUseNegotiatedSemantics = true;
			try
			{
				OnOpen?.Invoke();
			}
			catch (Exception ex)
			{
				Log.Error($"RtcDataChannel '{Label}' (stream {StreamId}): OnOpen subscriber threw.", ex);
			}
		}

		/// <summary>
		///     Sends <paramref name="data" /> on this channel. An empty payload cannot go out as a
		///     zero-length SCTP DATA chunk, so it is sent as a single zero byte under the matching -empty
		///     PPID instead (RFC 8831 3.2); the peer's <see cref="RtcChannelManager" /> reconstructs it back
		///     to an empty sequence on arrival, this padding byte never reaches a consumer's
		///     <see cref="OnMessage" />. A failed send (association not established, or its send-queue
		///     budget exhausted) is dropped and logged, matching this method's fixed <see langword="void" />
		///     signature: the caller has no return value to inspect.
		///     <para>
		///     Two different rules govern how a message rides the wire before this channel's own
		///     pre-negotiation window ends (<see cref="_canUseNegotiatedSemantics" />), and they are NOT the
		///     same kind of rule:
		///     </para>
		///     <para>
		///     Ordered is RFC 8832 6's own MUST, independent of <see cref="Ordered" />: DCEP's OPEN rides
		///     ordered-reliable ahead of any data on the same stream, but SCTP's ordering guarantee only
		///     covers ordered chunks - an unordered send in that same gap could overtake the OPEN, or even
		///     be rejected by the peer as an unknown stream before it has processed the OPEN at all. This
		///     half can never be relaxed without breaking the spec.
		///     </para>
		///     <para>
		///     Reliable (forcing <see cref="MaxRetransmits" /> to -1 for the duration) is NOT required by
		///     the RFC - ordered delivery alone already guarantees the message rides behind the OPEN - it is
		///     this stack's own deliberate, conservative superset: abandoning (RFC 3758 FORWARD-TSN) a
		///     channel's very first messages before the peer has even confirmed the channel exists is a
		///     pointless risk at the traffic volumes this stack deals in, so this stack chooses not to allow
		///     it. This half is free to be simplified away later; the ordered half is not.
		///     </para>
		/// </summary>
		public void Send(ReadOnlySpan<byte> data, bool asString = false)
		{
			bool empty = data.IsEmpty;
			uint ppid = asString ? (empty ? PpidStringEmpty : PpidString) : (empty ? PpidBinaryEmpty : PpidBinary);

			bool preNegotiation = !_canUseNegotiatedSemantics;
			bool unordered = preNegotiation ? false : !Ordered; // RFC 8832 6 MUST while preNegotiation
			int maxRetransmits = preNegotiation ? -1 : MaxRetransmits; // this stack's own conservative superset

			bool sent;
			if (empty)
			{
				Span<byte> padding = stackalloc byte[1];
				padding[0] = 0;
				sent = _association.Send(StreamId, ppid, padding, unordered, maxRetransmits);
			}
			else
			{
				sent = _association.Send(StreamId, ppid, data, unordered, maxRetransmits);
			}

			if (!sent) Log.Warn($"RtcDataChannel '{Label}' (stream {StreamId}): send dropped (association not established, or send-queue budget exhausted).");
		}

		/// <summary>Called by <see cref="RtcChannelManager" /> for every inbound message on this channel's stream: derives <c>isString</c>/emptiness from <paramref name="ppid" /> and hands <paramref name="message" /> to <see cref="OnMessage" /> unchanged (zero-copy) for the non-empty case, per this codebase's established sequence-validity-during-the-callback contract. Also ends <see cref="Send" />'s pre-negotiation window: RFC 8832 6's MUST is scoped to "until a DATA_CHANNEL_ACK message, or any message, has been received on the channel" - inbound traffic on this stream is proof the peer has already processed the OPEN, exactly like an ACK is.</summary>
		internal void DeliverData(uint ppid, in ReadOnlySequence<byte> message)
		{
			_canUseNegotiatedSemantics = true;

			bool isString = ppid == PpidString || ppid == PpidStringEmpty;
			bool isEmpty = ppid == PpidStringEmpty || ppid == PpidBinaryEmpty;
			ReadOnlySequence<byte> payload = isEmpty ? ReadOnlySequence<byte>.Empty : message;

			try
			{
				OnMessage?.Invoke(in payload, isString);
			}
			catch (Exception ex)
			{
				Log.Error($"RtcDataChannel '{Label}' (stream {StreamId}): OnMessage subscriber threw; message dropped, channel continues.", ex);
			}
		}
	}

	/// <summary>
	///     DCEP (RFC 8832) negotiation and message dispatch for one <see cref="SctpAssociation" />. Built
	///     as its own small class for this task rather than folded into <see cref="RtcPeer" />: the plan's
	///     Task 7 is what actually owns an <see cref="SctpAssociation" /> end to end (association lifetime,
	///     the loopback/mux wiring, teardown), and this task's brief explicitly keeps this task off
	///     <see cref="RtcPeer" />. One instance wraps exactly one association and is directly testable
	///     against it, which is how this task's own tests exercise it: two instances, one per side of a
	///     synchronously-wired pair of associations, exactly like <see cref="SctpAssociationHandshakeTests" />
	///     wires the associations themselves. Task 7 constructs one of these per established association and
	///     forwards <see cref="OnDataChannel" /> to whatever surface it exposes upward.
	///     <para>
	///     The DCEP OPEN/ACK codec lives here as private statics (this class is the only one that ever
	///     builds or parses that wire format; <see cref="RtcDataChannel" /> itself never touches DCEP,
	///     only the data-message PPIDs). A fragmented OPEN is legal (RFC 8832 does not bound the label
	///     length below what needs fragmenting at the SCTP layer), but every OPEN this stack actually
	///     negotiates is a handful of bytes, so multi-segment delivery is handled by copying into a small
	///     stack buffer rather than by a sequence-aware field-by-field reader: simpler, and the copy is a
	///     one-time cost per channel negotiated, not a steady-state one. A message too large to fit that
	///     buffer is tolerated as hostile/truncated input (dropped and counted), never a crash.
	///     </para>
	/// </summary>
	public class RtcChannelManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RtcChannelManager));

		// RFC 8832 6: DCEP always rides PPID 50, reliable and ordered, regardless of the channel's own
		// negotiated semantics - it is control plane, not data plane.
		private const uint DcepPpid = 50;

		private const byte OpenMessageType = 0x03;
		private const byte AckMessageType = 0x02;

		// RFC 8832 5.1: type(1) + channel type(1) + priority(2) + reliability parameter(4) + label
		// length(2) + protocol length(2), before the variable-length label/protocol bytes.
		private const int OpenHeaderLength = 12;

		// The four DCEP channel types this stack's own channel model (Ordered + MaxRetransmits, no
		// "timed" partial reliability) can represent. RFC 8832 5.1 also defines timed-reliability
		// variants (0x02/0x82); an OPEN naming one of those has no representation in this model and is
		// tolerated as an unsupported channel type (dropped and counted), same as a genuinely malformed
		// one - see TryFromChannelType.
		private const byte ChannelTypeReliable = 0x00;
		private const byte ChannelTypeReliableUnordered = 0x80;
		private const byte ChannelTypePartialReliableRexmit = 0x01;
		private const byte ChannelTypePartialReliableRexmitUnordered = 0x81;

		// Sized to actually cover the multi-segment fallback TryGetContiguous exists for: SctpAssociation
		// only fragments a message above FragmentThreshold (1024 bytes, see SctpAssociation's own
		// remarks), so a scratch buffer at or below that never receives a genuinely multi-segment
		// sequence to fall back for in the first place - 2048 gives headroom above that threshold for a
		// deliberately large label while staying a fixed, cheap stack buffer. NetherNet's own two labels
		// (19 and 21 bytes) never come close to fragmenting at all. This is a hard cap, not a soft one:
		// RFC 8832's label/protocol lengths are 16-bit fields (up to 65535 bytes each), so an OPEN is
		// legal in principle far beyond what fits here, and one that does not fit is dropped and counted
		// by design, not a bug - see TryGetContiguous.
		private const int DcepScratchSize = 2048;

		private readonly SctpAssociation _association;
		private readonly bool _isClient;
		private readonly object _gate = new();
		private readonly Dictionary<ushort, RtcDataChannel> _channelsByStreamId = new();

		// RFC 8832 6: the DTLS client claims even stream ids starting at 0 for the channels IT opens,
		// the DTLS server odd ids starting at 1 - independently of which side opens more channels, so
		// this counter only ever steps by 2 from its role's own starting id.
		private ushort _nextStreamId;

		// Task 7: CreateChannel can now genuinely race SctpAssociation reaching Established - riding a
		// real (if loopback) UdpMux/DTLS transport, an application can call it the instant RtcPeer's
		// transport is ready, well before the SCTP four-way handshake that same readiness kicks off has
		// actually finished. Sending DATA_CHANNEL_OPEN before Established would just be dropped
		// (SctpAssociation.Send returns false pre-Established, and this class never inspects that return
		// value), silently stranding the channel forever. _pendingOpens holds the already-built OPEN
		// bytes for exactly that window; _established mirrors _association.State under this class's own
		// _gate (rather than CreateChannel reading _association.State directly) so the two possible
		// orderings between a CreateChannel call and the association's own OnEstablished firing are both
		// race-free: whichever of CreateChannel's or OnAssociationEstablished's own critical section
		// (both under _gate) runs first is authoritative, with no gap in between where an enqueue could
		// be missed by an OnEstablished that already ran.
		private readonly List<(ushort StreamId, byte[] OpenBytes)> _pendingOpens = new();
		private bool _established;

		private long _ignoredMessageCount;

		/// <summary>Raised outside <see cref="_gate" />, once per inbound OPEN accepted: the reply ACK has already been sent by the time this fires (see <see cref="HandleOpen" />), and the channel is already <see cref="RtcDataChannel.IsOpen" />.</summary>
		public event Action<RtcDataChannel> OnDataChannel;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): every message this class drops rather than acting on - a malformed, truncated, wrong-parity, or unsupported-channel-type DCEP control message (<see cref="HandleDcep" />), a duplicate OPEN or a stray ACK for a stream already resolved one way or the other, and a data-plane message (non-DCEP PPID) addressed to a stream id with no registered channel (<see cref="OnAssociationMessage" />). Was DCEP-only ("IgnoredDcepMessageCount") until the fix round that added the last case - a silent, uncounted drop there is exactly what let the pre-ACK ordering race go unnoticed.</summary>
		internal long IgnoredMessageCount => Interlocked.Read(ref _ignoredMessageCount);

		/// <summary><paramref name="isClient" /> is the DTLS role, not an application-level concept - <see cref="SctpAssociation" />'s own constructor takes the identical flag with the identical meaning (see Task 7's wiring), which is what fixes this side's stream id parity per RFC 8832 6.</summary>
		public RtcChannelManager(SctpAssociation association, bool isClient)
		{
			_association = association;
			_isClient = isClient;
			_nextStreamId = isClient ? (ushort) 0 : (ushort) 1;

			_association.OnMessage += OnAssociationMessage;
			_association.OnEstablished += OnAssociationEstablished;

			// SctpAssociation.OnEstablished fires at most once, ever, and does not replay past
			// invocations to a subscriber that registers after the fact - a real case, not just this
			// class's own synchronous-loopback tests: a caller can pass in an association that already
			// reached Established before constructing this manager at all (Task 7's RtcPeer builds both
			// together, but nothing enforces that ordering on this constructor's own contract). No lock
			// is needed for this read: nothing outside this constructor holds a reference to `this` yet
			// to race CreateChannel against it, and State is a plain volatile read, so an interleaving
			// with OnEstablished firing on another thread right around this line is still race-free -
			// either this check or the live event catches the transition, and if both do, the second is
			// a harmless no-op (OnAssociationEstablished's own drain finds an empty queue).
			if (_association.State == SctpState.Established) _established = true;
		}

		/// <summary>
		///     Claims the next stream id of this side's own parity, sends DATA_CHANNEL_OPEN on it (PPID
		///     50, reliable ordered regardless of <paramref name="ordered" />/<paramref name="maxRetransmits" />,
		///     which describe the DATA plane this channel will carry, not the OPEN/ACK control messages
		///     themselves), and returns the channel immediately, not yet <see cref="RtcDataChannel.IsOpen" />:
		///     it opens once the peer's ACK arrives (see <see cref="HandleAck" />), which in a synchronous
		///     loopback wiring - this task's tests, and NetherNet's own same-process topology in stage 2 -
		///     already happened by the time this call returns.
		///     <para>
		///     Task 7 (single-lock fix): the stream-id claim and the channel's dictionary registration
		///     used to be two separate <c>lock (_gate)</c> statements. Every real call path into this
		///     class was, and still is, single-threaded per association on the receive side (driven
		///     synchronously off <see cref="SctpAssociation.OnPacketReceived" />), but Task 7 gives
		///     <see cref="CreateChannel" /> itself a second, genuinely concurrent caller: an application
		///     thread, via <see cref="RtcPeer.CreateDataChannel" />, running independently of the mux
		///     receive thread and the tick thread. Two separate critical sections left a window - between
		///     claiming <paramref name="label" />'s stream id and registering the channel object under it
		///     - during which <see cref="OnAssociationMessage" /> would find no channel for a stream id
		///     this side had already claimed but not yet published; combining both into one critical
		///     section below closes it, matching the flush/enqueue decision two paragraphs down, which
		///     needs the same treatment for the same reason (see <see cref="OnAssociationEstablished" />'s
		///     own remarks).
		///     </para>
		///     <para>
		///     If <see cref="_association" /> is not yet <see cref="SctpState.Established" /> when this
		///     is called - a real possibility once this rides an actual (if loopback) transport rather
		///     than the synchronous wiring this class's own tests use, since an application can call this
		///     the instant <see cref="RtcPeer" />'s transport is ready, before the SCTP handshake that
		///     readiness just kicked off has finished - the built OPEN bytes are queued instead of sent,
		///     and flushed once <see cref="OnAssociationEstablished" /> fires. The channel object and its
		///     stream id are still handed back immediately either way: nothing about a caller's own use of
		///     the returned <see cref="RtcDataChannel" /> depends on the OPEN having actually reached the
		///     peer yet.
		///     </para>
		/// </summary>
		public RtcDataChannel CreateChannel(string label, bool ordered, int maxRetransmits)
		{
			byte[] labelBytes = Encoding.UTF8.GetBytes(label);

			RtcDataChannel channel;
			ushort streamId;
			lock (_gate)
			{
				streamId = _nextStreamId;
				_nextStreamId = unchecked((ushort) (_nextStreamId + 2));

				channel = new RtcDataChannel(_association, streamId, label, ordered, maxRetransmits);
				_channelsByStreamId[streamId] = channel;
			}

			(byte channelType, uint reliabilityParameter) = ToChannelType(ordered, maxRetransmits);

			Span<byte> buffer = stackalloc byte[OpenHeaderLength + labelBytes.Length];
			int written = WriteOpen(buffer, channelType, priority: 0, reliabilityParameter, labelBytes);

			bool sendNow;
			lock (_gate)
			{
				sendNow = _established;
				if (!sendNow) _pendingOpens.Add((streamId, buffer.Slice(0, written).ToArray()));
			}

			if (sendNow)
			{
				_association.Send(streamId, DcepPpid, buffer.Slice(0, written), unordered: false, maxRetransmits: -1);
			}
			else
			{
				// Task 8: local demand for a channel is reason enough to try starting the handshake,
				// regardless of which side RtcPeer's own DTLS-role-driven eagerness already tried on -
				// see SctpAssociation.Start's own remarks for why a real interop peer makes this second
				// trigger necessary rather than redundant. Unconditional and safe: Start is idempotent, a
				// no-op once the association has already left SctpState.Closed by any means, including
				// that same DTLS-role-driven eagerness winning the race first.
				_association.Start();
			}

			return channel;
		}

		/// <summary>
		///     Raised outside <see cref="SctpAssociation._gate" /> the instant <see cref="_association" />
		///     reaches <see cref="SctpState.Established" /> (fires at most once per association's
		///     lifetime). Flushes every OPEN <see cref="CreateChannel" /> had to queue because it ran
		///     ahead of that moment, in the order those calls happened. See <see cref="CreateChannel" />'s
		///     own remarks for why the established-check-then-maybe-enqueue and this method's own
		///     established-flip-then-drain both need to run under this class's <see cref="_gate" /> as one
		///     atomic step each: that, not <see cref="SctpAssociation" />'s own internal locking, is what
		///     guarantees neither ordering between the two calls can lose a queued OPEN.
		/// </summary>
		private void OnAssociationEstablished()
		{
			List<(ushort StreamId, byte[] OpenBytes)> pending = null;
			lock (_gate)
			{
				_established = true;
				if (_pendingOpens.Count > 0)
				{
					pending = new List<(ushort, byte[])>(_pendingOpens);
					_pendingOpens.Clear();
				}
			}

			if (pending == null) return;

			for (int i = 0; i < pending.Count; i++)
			{
				(ushort streamId, byte[] openBytes) = pending[i];
				_association.Send(streamId, DcepPpid, openBytes, unordered: false, maxRetransmits: -1);
			}
		}

		/// <summary>Dispatches every inbound message on <see cref="_association" />: DCEP control traffic (PPID 50) to <see cref="HandleDcep" />, everything else to whichever channel owns that stream id. A message for a stream id with no registered channel - stale, or a peer bug - is dropped and counted (was silent before this fix round; see <see cref="IgnoredMessageCount" />'s own remarks for why that hid a real bug), never dispatched.</summary>
		private void OnAssociationMessage(ushort streamId, uint ppid, in ReadOnlySequence<byte> message)
		{
			if (ppid == DcepPpid)
			{
				HandleDcep(streamId, message);
				return;
			}

			RtcDataChannel channel;
			lock (_gate) _channelsByStreamId.TryGetValue(streamId, out channel);
			if (channel == null)
			{
				CountIgnored();
				return;
			}

			channel.DeliverData(ppid, in message);
		}

		private void HandleDcep(ushort streamId, in ReadOnlySequence<byte> message)
		{
			Span<byte> scratch = stackalloc byte[DcepScratchSize];
			if (!TryGetContiguous(message, scratch, out ReadOnlySpan<byte> bytes) || bytes.Length == 0)
			{
				CountIgnored();
				return;
			}

			switch (bytes[0])
			{
				case OpenMessageType:
					HandleOpen(streamId, bytes);
					break;

				case AckMessageType:
					HandleAck(streamId);
					break;

				default:
					CountIgnored();
					break;
			}
		}

		/// <summary>
		///     Inbound DATA_CHANNEL_OPEN: creates the channel, marks it open immediately (RFC 8832: the
		///     receiver may use the channel right away, it does not wait on its own ACK to be acknowledged
		///     back), replies ACK on the same stream, then raises <see cref="OnDataChannel" /> - in that
		///     order, so the wire reply is never delayed behind a subscriber (matches
		///     <see cref="SctpAssociation" />'s own COOKIE-ACK-before-OnEstablished ordering one file over).
		///     A malformed OPEN (too short for the fixed header, or claiming a label/protocol length that
		///     runs past the message), one naming a stream id of OUR OWN parity (RFC 8832 6 - never a valid
		///     id for a peer-initiated channel, see <see cref="HasPeerParity" />), or one naming a channel
		///     type this stack's model cannot represent is dropped and counted, never answered - this is
		///     the hostile-input path the class remarks describe. An OPEN for a stream id already carrying
		///     a channel (the peer retransmitting because it lost our first ACK - ordinary on a real
		///     network, not hostile) is counted but still re-ACKed: idempotent and RFC-friendly, but the
		///     existing channel object - and whatever the application already subscribed to it - is never
		///     rebuilt or silently swapped out, and <see cref="OnDataChannel" /> never fires twice for it.
		///     <para>
		///     Task 7 (single-lock fix): the duplicate-stream check and the new channel's registration
		///     used to be two separate <c>lock (_gate)</c> statements (flagged in the task-6 report as a
		///     TOCTOU believed unreachable only because every call into this class was, at the time,
		///     single-threaded per association). That belief no longer holds once <see cref="CreateChannel" />
		///     gets a genuinely concurrent caller (Task 7's application thread, via
		///     <see cref="RtcPeer.CreateDataChannel" />) - not because two inbound OPENs for the same
		///     stream id can now race each other (they still can't: every call into this method is still
		///     driven synchronously, one datagram at a time, off <see cref="SctpAssociation.OnPacketReceived" />),
		///     but because this method's own lookup-or-register step and <see cref="CreateChannel" />'s
		///     claim-and-register step now touch the same dictionary from what could be two different
		///     threads. Combined into one critical section below so there is no window between them.
		///     </para>
		/// </summary>
		private void HandleOpen(ushort streamId, ReadOnlySpan<byte> bytes)
		{
			if (!TryParseOpen(bytes, out byte channelType, out uint reliabilityParameter, out ReadOnlySpan<byte> labelBytes))
			{
				CountIgnored();
				return;
			}

			if (!HasPeerParity(streamId))
			{
				CountIgnored();
				return;
			}

			// Parsed before the lock: TryFromChannelType and the UTF-8 decode are pure and only need
			// bytes.Length/labelBytes, which are stable for this whole synchronous call - no reason to
			// hold _gate across them.
			bool typeSupported = TryFromChannelType(channelType, reliabilityParameter, out bool ordered, out int maxRetransmits);
			string label = typeSupported ? Encoding.UTF8.GetString(labelBytes) : null;

			RtcDataChannel channel;
			bool isNewChannel;
			lock (_gate)
			{
				if (_channelsByStreamId.TryGetValue(streamId, out RtcDataChannel existing))
				{
					channel = existing;
					isNewChannel = false;
				}
				else if (!typeSupported)
				{
					channel = null;
					isNewChannel = false;
				}
				else
				{
					channel = new RtcDataChannel(_association, streamId, label, ordered, maxRetransmits);
					_channelsByStreamId[streamId] = channel;
					isNewChannel = true;
				}
			}

			if (channel == null)
			{
				CountIgnored();
				return;
			}

			if (!isNewChannel)
			{
				CountIgnored();
				SendAck(streamId);
				return;
			}

			SendAck(streamId);
			channel.RaiseOpen();

			try
			{
				OnDataChannel?.Invoke(channel);
			}
			catch (Exception ex)
			{
				Log.Error($"RtcChannelManager: OnDataChannel subscriber threw for channel '{label}' (stream {streamId}).", ex);
			}
		}

		/// <summary>RFC 8832 6: the DTLS client's own channels sit on even stream ids, the DTLS server's on odd ones, so a well-behaved peer's OPEN always names a stream id of the OPPOSITE parity to ours. Used only to validate an inbound OPEN in <see cref="HandleOpen" />; nothing else in this class needs it, since every other id this class touches (<see cref="CreateChannel" />'s own counter, an OPEN's or a data message's stream id once a channel already exists for it) is already known-good by construction or by dictionary lookup.</summary>
		private bool HasPeerParity(ushort streamId)
		{
			int ourParity = _isClient ? 0 : 1;
			return (streamId % 2) != ourParity;
		}

		/// <summary>Inbound DATA_CHANNEL_ACK: opens the matching outbound channel this side created earlier. An ACK for an unknown stream, or one already open (a stray duplicate), is dropped and counted rather than acted on twice.</summary>
		private void HandleAck(ushort streamId)
		{
			RtcDataChannel channel;
			lock (_gate) _channelsByStreamId.TryGetValue(streamId, out channel);

			if (channel == null || channel.IsOpen)
			{
				CountIgnored();
				return;
			}

			channel.RaiseOpen();
		}

		private void SendAck(ushort streamId)
		{
			Span<byte> buffer = stackalloc byte[1];
			int written = WriteAck(buffer);
			_association.Send(streamId, DcepPpid, buffer.Slice(0, written), unordered: false, maxRetransmits: -1);
		}

		private void CountIgnored()
		{
			Interlocked.Increment(ref _ignoredMessageCount);
		}

		/// <summary>Copies <paramref name="sequence" /> into <paramref name="scratch" /> when it spans more than one segment (a fragmented DCEP message); the single-segment case is the zero-copy fast path, per this class's remarks on why a control message this small does not need a sequence-aware reader. Fails (rather than copying a partial prefix) when the message does not fit <paramref name="scratch" /> at all.</summary>
		private static bool TryGetContiguous(in ReadOnlySequence<byte> sequence, Span<byte> scratch, out ReadOnlySpan<byte> contiguous)
		{
			if (sequence.IsSingleSegment)
			{
				contiguous = sequence.FirstSpan;
				return true;
			}

			if (sequence.Length > scratch.Length)
			{
				contiguous = default;
				return false;
			}

			sequence.CopyTo(scratch);
			contiguous = scratch.Slice(0, (int) sequence.Length);
			return true;
		}

		/// <summary>This stack's channel model (<see cref="RtcDataChannel.Ordered" /> + <see cref="RtcDataChannel.MaxRetransmits" />) maps onto exactly the four non-timed DCEP channel types (RFC 8832 5.1); reliability parameter is the RFC 3758 retransmit budget for the two partial-reliability types, 0 (unused) for the two fully-reliable ones.</summary>
		private static (byte ChannelType, uint ReliabilityParameter) ToChannelType(bool ordered, int maxRetransmits)
		{
			bool reliable = maxRetransmits < 0;
			byte channelType = (ordered, reliable) switch
			{
				(true, true) => ChannelTypeReliable,
				(false, true) => ChannelTypeReliableUnordered,
				(true, false) => ChannelTypePartialReliableRexmit,
				(false, false) => ChannelTypePartialReliableRexmitUnordered
			};
			uint reliabilityParameter = reliable ? 0u : (uint) maxRetransmits;
			return (channelType, reliabilityParameter);
		}

		/// <summary>The inverse of <see cref="ToChannelType" />. Returns false for anything this stack's channel model has no representation for: RFC 8832's timed-reliability types (0x02/0x82), or any other byte value - both are hostile/unsupported input from this stack's point of view, handled identically by the caller.</summary>
		private static bool TryFromChannelType(byte channelType, uint reliabilityParameter, out bool ordered, out int maxRetransmits)
		{
			switch (channelType)
			{
				case ChannelTypeReliable:
					ordered = true;
					maxRetransmits = -1;
					return true;

				case ChannelTypeReliableUnordered:
					ordered = false;
					maxRetransmits = -1;
					return true;

				case ChannelTypePartialReliableRexmit:
					ordered = true;
					maxRetransmits = (int) Math.Min(reliabilityParameter, (uint) int.MaxValue);
					return true;

				case ChannelTypePartialReliableRexmitUnordered:
					ordered = false;
					maxRetransmits = (int) Math.Min(reliabilityParameter, (uint) int.MaxValue);
					return true;

				default:
					ordered = false;
					maxRetransmits = 0;
					return false;
			}
		}

		/// <summary>RFC 8832 5.1 DATA_CHANNEL_OPEN, fixed layout: type(1)=0x03, channel type(1), priority(2), reliability parameter(4), label length(2), protocol length(2), label bytes, protocol bytes. This stack never sets a protocol string (NetherNet does not use one), so protocol length is always written as 0 and no protocol bytes follow. Returns the total length written.</summary>
		private static int WriteOpen(Span<byte> destination, byte channelType, ushort priority, uint reliabilityParameter, ReadOnlySpan<byte> label)
		{
			destination[0] = OpenMessageType;
			destination[1] = channelType;
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2, 2), priority);
			BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), reliabilityParameter);
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(8, 2), (ushort) label.Length);
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(10, 2), 0);
			label.CopyTo(destination.Slice(OpenHeaderLength));
			return OpenHeaderLength + label.Length;
		}

		/// <summary>The inverse of <see cref="WriteOpen" />, tolerant of hostile input per this codebase's hot-path law: false for a message shorter than the fixed header, or one whose declared label/protocol length runs past the end of <paramref name="data" /> (a truncated OPEN, or a label length lying about a message that was never fragmented in the first place). Protocol bytes are skipped, never returned: this stack has no use for them.</summary>
		private static bool TryParseOpen(ReadOnlySpan<byte> data, out byte channelType, out uint reliabilityParameter, out ReadOnlySpan<byte> label)
		{
			channelType = 0;
			reliabilityParameter = 0;
			label = default;

			if (data.Length < OpenHeaderLength || data[0] != OpenMessageType) return false;

			channelType = data[1];
			reliabilityParameter = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4));
			ushort labelLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(8, 2));
			ushort protocolLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(10, 2));

			int needed = OpenHeaderLength + labelLength + protocolLength;
			if (data.Length < needed) return false;

			label = data.Slice(OpenHeaderLength, labelLength);
			return true;
		}

		/// <summary>RFC 8832 5.2 DATA_CHANNEL_ACK: a single byte, 0x02, no other fields. Returns the total length written (always 1).</summary>
		private static int WriteAck(Span<byte> destination)
		{
			destination[0] = AckMessageType;
			return 1;
		}
	}
}