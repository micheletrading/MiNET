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
		///     Sends <paramref name="data" /> on this channel, riding its own negotiated semantics
		///     (unordered flag, <see cref="MaxRetransmits" />) rather than DCEP's always-reliable-ordered
		///     control stream. An empty payload cannot go out as a zero-length SCTP DATA chunk, so it is
		///     sent as a single zero byte under the matching -empty PPID instead (RFC 8831 3.2); the peer's
		///     <see cref="RtcChannelManager" /> reconstructs it back to an empty sequence on arrival, this
		///     padding byte never reaches a consumer's <see cref="OnMessage" />. A failed send (association
		///     not established, or its send-queue budget exhausted) is dropped and logged, matching this
		///     method's fixed <see langword="void" /> signature: the caller has no return value to inspect.
		/// </summary>
		public void Send(ReadOnlySpan<byte> data, bool asString = false)
		{
			bool empty = data.IsEmpty;
			uint ppid = asString ? (empty ? PpidStringEmpty : PpidString) : (empty ? PpidBinaryEmpty : PpidBinary);
			bool unordered = !Ordered;

			bool sent;
			if (empty)
			{
				Span<byte> padding = stackalloc byte[1];
				padding[0] = 0;
				sent = _association.Send(StreamId, ppid, padding, unordered, MaxRetransmits);
			}
			else
			{
				sent = _association.Send(StreamId, ppid, data, unordered, MaxRetransmits);
			}

			if (!sent) Log.Warn($"RtcDataChannel '{Label}' (stream {StreamId}): send dropped (association not established, or send-queue budget exhausted).");
		}

		/// <summary>Called by <see cref="RtcChannelManager" /> for every inbound message on this channel's stream: derives <c>isString</c>/emptiness from <paramref name="ppid" /> and hands <paramref name="message" /> to <see cref="OnMessage" /> unchanged (zero-copy) for the non-empty case, per this codebase's established sequence-validity-during-the-callback contract.</summary>
		internal void DeliverData(uint ppid, in ReadOnlySequence<byte> message)
		{
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

		// Large enough for any label this stack negotiates (NetherNet's two are 19 and 21 bytes) with
		// generous headroom; a legitimate OPEN never approaches this, so hitting the limit only ever
		// means hostile or corrupt input.
		private const int DcepScratchSize = 512;

		private readonly SctpAssociation _association;
		private readonly bool _isClient;
		private readonly object _gate = new();
		private readonly Dictionary<ushort, RtcDataChannel> _channelsByStreamId = new();

		// RFC 8832 6: the DTLS client claims even stream ids starting at 0 for the channels IT opens,
		// the DTLS server odd ids starting at 1 - independently of which side opens more channels, so
		// this counter only ever steps by 2 from its role's own starting id.
		private ushort _nextStreamId;

		private long _ignoredDcepMessageCount;

		/// <summary>Raised outside <see cref="_gate" />, once per inbound OPEN accepted: the reply ACK has already been sent by the time this fires (see <see cref="HandleOpen" />), and the channel is already <see cref="RtcDataChannel.IsOpen" />.</summary>
		public event Action<RtcDataChannel> OnDataChannel;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many inbound DCEP messages on <see cref="DcepPpid" /> were dropped for being malformed, truncated, naming an unsupported channel type, or an unrecognised DCEP message type.</summary>
		internal long IgnoredDcepMessageCount => Interlocked.Read(ref _ignoredDcepMessageCount);

		/// <summary><paramref name="isClient" /> is the DTLS role, not an application-level concept - <see cref="SctpAssociation" />'s own constructor takes the identical flag with the identical meaning (see Task 7's wiring), which is what fixes this side's stream id parity per RFC 8832 6.</summary>
		public RtcChannelManager(SctpAssociation association, bool isClient)
		{
			_association = association;
			_isClient = isClient;
			_nextStreamId = isClient ? (ushort) 0 : (ushort) 1;

			_association.OnMessage += OnAssociationMessage;
		}

		/// <summary>
		///     Claims the next stream id of this side's own parity, sends DATA_CHANNEL_OPEN on it (PPID
		///     50, reliable ordered regardless of <paramref name="ordered" />/<paramref name="maxRetransmits" />,
		///     which describe the DATA plane this channel will carry, not the OPEN/ACK control messages
		///     themselves), and returns the channel immediately, not yet <see cref="RtcDataChannel.IsOpen" />:
		///     it opens once the peer's ACK arrives (see <see cref="HandleAck" />), which in a synchronous
		///     loopback wiring - this task's tests, and NetherNet's own same-process topology in stage 2 -
		///     already happened by the time this call returns.
		/// </summary>
		public RtcDataChannel CreateChannel(string label, bool ordered, int maxRetransmits)
		{
			byte[] labelBytes = Encoding.UTF8.GetBytes(label);

			ushort streamId;
			lock (_gate)
			{
				streamId = _nextStreamId;
				_nextStreamId = unchecked((ushort) (_nextStreamId + 2));
			}

			var channel = new RtcDataChannel(_association, streamId, label, ordered, maxRetransmits);
			lock (_gate) _channelsByStreamId[streamId] = channel;

			(byte channelType, uint reliabilityParameter) = ToChannelType(ordered, maxRetransmits);

			Span<byte> buffer = stackalloc byte[OpenHeaderLength + labelBytes.Length];
			int written = WriteOpen(buffer, channelType, priority: 0, reliabilityParameter, labelBytes);
			_association.Send(streamId, DcepPpid, buffer.Slice(0, written), unordered: false, maxRetransmits: -1);

			return channel;
		}

		/// <summary>Dispatches every inbound message on <see cref="_association" />: DCEP control traffic (PPID 50) to <see cref="HandleDcep" />, everything else to whichever channel owns that stream id (a message for a stream id with no channel - stale, or a peer bug - is dropped silently, not counted: it is not DCEP traffic this class is responsible for policing).</summary>
		private void OnAssociationMessage(ushort streamId, uint ppid, in ReadOnlySequence<byte> message)
		{
			if (ppid == DcepPpid)
			{
				HandleDcep(streamId, message);
				return;
			}

			RtcDataChannel channel;
			lock (_gate) _channelsByStreamId.TryGetValue(streamId, out channel);
			channel?.DeliverData(ppid, in message);
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
		///     runs past the message) or one naming a channel type this stack's model cannot represent is
		///     dropped and counted, never answered - this is the hostile-input path the class remarks
		///     describe.
		/// </summary>
		private void HandleOpen(ushort streamId, ReadOnlySpan<byte> bytes)
		{
			if (!TryParseOpen(bytes, out byte channelType, out uint reliabilityParameter, out ReadOnlySpan<byte> labelBytes))
			{
				CountIgnored();
				return;
			}

			if (!TryFromChannelType(channelType, reliabilityParameter, out bool ordered, out int maxRetransmits))
			{
				CountIgnored();
				return;
			}

			string label = Encoding.UTF8.GetString(labelBytes);
			var channel = new RtcDataChannel(_association, streamId, label, ordered, maxRetransmits);
			lock (_gate) _channelsByStreamId[streamId] = channel;

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
			Interlocked.Increment(ref _ignoredDcepMessageCount);
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