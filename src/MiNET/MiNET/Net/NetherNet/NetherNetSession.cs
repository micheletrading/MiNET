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
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using log4net;
using MiNET.Net.Rtc;
using MiNET.Utils.Diagnostics;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     A NetherNet session. The transport's reliability work is done by the time a message
	///     surfaces here: ordering, retransmission, acknowledgement and datagram splitting all
	///     happen below, in SCTP. What is left is Bedrock's own batching and compression, reached
	///     through <see cref="ICustomMessageHandler" />.
	/// </summary>
	public class NetherNetSession : INetworkHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetherNetSession));

		// The client-side SCTP association's max-message-size ceiling (libwebrtc's own default).
		// There is no on-wire negotiation of this value, so it is a fixed constant rather than
		// something read off a live association, and anything larger has to be split by the segment
		// layer below.
		private const int MaxSegmentBytes = 262144;

		private readonly RtcPeer _peer;
		private readonly RtcDataChannel _reliable;

		// Not readonly: a returning client sometimes opens the unreliable channel after the session
		// already attached on the reliable one, so this is plugged in later through
		// AttachUnreliableChannel rather than only ever set from the constructor.
		private RtcDataChannel _unreliable;

		// One reassembler per channel. The two are independent streams with their own segment
		// counters, so sharing one would splice a half-built message from one into the other.
		private readonly NetherNetSegmentReassembler _reliableReassembler = new NetherNetSegmentReassembler();
		private readonly NetherNetSegmentReassembler _unreliableReassembler = new NetherNetSegmentReassembler();

		// Outgoing packets waiting for the send lane, the send-side mirror of _dispatchQueue: one
		// consumer per session (SendLoopAsync), so sends are serialized without a contended lock and
		// producers (broadcast fan-outs, game logic) only ever enqueue. The coalescing matters as
		// much as the ordering: everything that accumulates while one send is in flight drains into
		// a single PrepareSend, which batches it into one wrapper, one compress and one syscall,
		// instead of one of each per packet. Many writers: broadcasts arrive from any thread.
		private readonly Channel<Packet> _sendQueue = Channel.CreateUnbounded<Packet>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false
		});

		private readonly Task _sendLoop;

		// Decoded-but-not-yet-dispatched packets, consumed by this session's single async reader
		// (DispatchLoopAsync): no dedicated thread, the reader's WaitToReadAsync parks as a
		// ValueTask (IValueTaskSource-backed) and runs on the pool only while messages exist.
		// Single reader preserves per-session ordering; sessions parallelize across each other.
		// Unbounded: SCTP flow control (a_rwnd) is the backpressure that keeps a peer from growing
		// it without limit; a local slow handler grows it briefly and drains, which is strictly
		// better than stalling the shared mux thread.
		private readonly Channel<Packet> _dispatchQueue = Channel.CreateUnbounded<Packet>(new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = true
		});

		private int _closed;

		// Completed by Close the instant teardown starts, so a send lane parked on the window
		// signal wakes NOW instead of on its 500ms backstop tick. Without this, closing a session
		// whose lane is parked against a dead peer's full window costs ~250ms on average - and a
		// sweep tearing down a thousand such corpses turned that into minutes of blocked pool
		// threads and starved every joining player's login (the 85-bot loss, 2026-08-13).
		private readonly TaskCompletionSource _closedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _sawUnreliableTraffic;
		private int _hasReceived;
		private long _lastReceiveTicks = Environment.TickCount64;

		// Session lifetime, for transport.sessions.duration. Stopwatch rather than TickCount because
		// the counter-discipline rule is that every DURATION comes off the monotonic timestamp; only
		// coarse arrival stamps, like _lastReceiveTicks above, use the tick count.
		private readonly long _openedAt = Stopwatch.GetTimestamp();

		public ICustomMessageHandler CustomMessageHandler { get; set; }

		/// <summary>How long ago the last bytes arrived from this client, for the listener's
		/// inactivity sweep. A live client is never silent: PlayerAuthInput alone is one per tick.
		/// Before any bytes have arrived this counts from session creation.</summary>
		public long MillisSinceLastReceive => Environment.TickCount64 - Volatile.Read(ref _lastReceiveTicks);

		/// <summary>Whether the client has ever sent application data. Separates a session still
		/// connecting (judged on the longer connection-phase timeout) from one that went silent
		/// in-game.</summary>
		public bool HasReceived => _hasReceived != 0;

		/// <summary>Fires once when the session closes, however it closes, so the listener can drop
		/// it from its session table.</summary>
		public Action<NetherNetSession> OnClosed { get; set; }

		public bool IsClosed => _closed != 0;

		// Counted rather than read off the channel: an unbounded channel with SingleReader uses the
		// single-consumer implementation, whose Reader.Count throws NotSupportedException. Mirrors
		// _dispatchPending's shape on the receive side.
		private int _sendPending;

		/// <summary>Packets accepted for send and not yet drained by this session's lane.</summary>
		public int SendQueueDepth => Volatile.Read(ref _sendPending);

		/// <summary>Packets decoded and not yet handled. This is the same field the direct-dispatch ordering guard reads.</summary>
		public int DispatchQueueDepth => Volatile.Read(ref _dispatchPending);

		public string Username { get; set; }

		/// <summary>
		///     DTLS has already encrypted and authenticated everything on this channel, so Bedrock's
		///     session cipher is not applied on top. Confirmed against Mojang: the transport handles it
		///     before a payload reaches us.
		/// </summary>
		public bool IsTransportEncrypted => true;

		public string TransportName => "NetherNet";

		/// <summary>
		///     The ICE-nominated remote address: not known until the connection is established, and
		///     it can be a relay rather than the player's own address.
		/// </summary>
		public IPEndPoint EndPoint { get; }

		/// <summary>
		///     The client's NetworkID from the signaling request. Opaque by contract, currently a
		///     decimal uint64, which is why it is parsed leniently rather than assumed.
		/// </summary>
		public string NetworkId { get; }

		public long NetworkIdentifier { get; }

		public NetherNetSession(RtcPeer peer, RtcDataChannel reliable, RtcDataChannel unreliable, IPEndPoint endPoint, string networkId)
		{
			_peer = peer ?? throw new ArgumentNullException(nameof(peer));
			_reliable = reliable ?? throw new ArgumentNullException(nameof(reliable));

			EndPoint = endPoint;
			NetworkId = networkId;
			NetworkIdentifier = long.TryParse(networkId, out long id) ? id : networkId?.GetHashCode() ?? 0;

			_ = DispatchLoopAsync();
			_sendLoop = SendLoopAsync();

			// Losing the reliable channel is losing the session. Mojang's guide has the client open
			// both, so a missing unreliable channel at this point is unexpected but not fatal: it may
			// still arrive and be wired in through AttachUnreliableChannel.
			_reliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) => OnDataChannelMessage(_reliableReassembler, in data, reliableChannel: true);

			if (unreliable != null)
			{
				AttachUnreliableChannel(unreliable);
			}
			else
			{
				Log.Warn($"NetherNet client {networkId} opened no unreliable channel yet");
			}

			// OnTransportClosed is the one teardown signal: it fires exactly once for every terminal
			// outcome after a successful handshake (ICE failure, a peer SCTP ABORT or SHUTDOWN, an
			// inbound DTLS close_notify or fatal alert), and never for this side's own RtcPeer.Dispose
			// (see Close() below). There is no transient "disconnected" state to tolerate here: the
			// listener's inactivity sweep is the only backstop for a peer that goes silent without
			// ever tearing the transport down cleanly.
			//
			// Wrapped: this can run from RtcPeer's own mux tick chain, which has no catch of its own
			// around raising this event, and Disconnect reaches arbitrary application code
			// (CustomMessageHandler, Player) through it. An unguarded throw here would not just be
			// lost, it would abort that whole tick invocation, taking every OTHER peer's RTO/SACK/T3
			// timers on the same mux down with it for that interval.
			_peer.OnTransportClosed += () =>
			{
				try
				{
					Disconnect("Connection closed", false);
				}
				catch (Exception e)
				{
					Log.Error($"NetherNet OnTransportClosed handler threw for {Username ?? NetworkId}", e);
				}
			};
		}

		/// <summary>
		///     Wires the unreliable channel, whether it arrives at construction or later, once a
		///     returning client's second channel opens after the session already attached on the
		///     reliable one. Safe to call at most once: a second call for an already-attached channel
		///     is ignored, so a stray retry can never splice a second reassembler onto it.
		/// </summary>
		public void AttachUnreliableChannel(RtcDataChannel unreliable)
		{
			if (unreliable == null) return;
			if (Interlocked.CompareExchange(ref _unreliable, unreliable, null) != null) return;

			unreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) => OnDataChannelMessage(_unreliableReassembler, in data, reliableChannel: false);
		}

		private void OnDataChannelMessage(NetherNetSegmentReassembler reassembler, in ReadOnlySequence<byte> data, bool reliableChannel)
		{
			byte[] rented = null;
			try
			{
				_hasReceived = 1;
				Volatile.Write(ref _lastReceiveTicks, Environment.TickCount64);

				if (!reliableChannel && Interlocked.Exchange(ref _sawUnreliableTraffic, 1) == 0)
				{
					// Nobody has documented what the client actually sends here, and there is reason to
					// expect the answer is nothing. Movement is server-authoritative, so a client
					// cannot afford to drop its own PlayerAuthInput; the loss-tolerant direction is
					// ours, broadcasting other entities where the next update supersedes the last.
					// go-nethernet calls this channel's behaviour "less defined" and never uses it.
					// So an arrival here is a genuine surprise and worth saying out loud once.
					Log.Warn($"NetherNet client {Username ?? NetworkId} is sending on the UNRELIABLE channel, first message {data.Length} bytes");
				}

				// Single-segment is the common case (a Bedrock batch under one SCTP fragmentation
				// window) and costs nothing: a view straight onto the association's own buffer. A
				// multi-segment sequence needs one contiguous copy because the reassembler's own
				// contract takes a single memory view, not a sequence.
				ReadOnlyMemory<byte> framed;
				if (data.IsSingleSegment)
				{
					framed = data.First;
				}
				else
				{
					rented = ArrayPool<byte>.Shared.Rent((int) data.Length);
					data.CopyTo(rented);
					framed = rented.AsMemory(0, (int) data.Length);
				}

				if (Log.IsDebugEnabled) Log.Debug($"NetherNet recv {framed.Length} bytes on {(reliableChannel ? "reliable" : "unreliable")}: {Packet.HexDump(framed.Span.Slice(0, Math.Min(32, framed.Length)).ToArray(), 32)}");

				// False means the message is still being assembled from segments. The payload is a
				// view onto either the buffer above or the reassembler's own pooled one, valid only
				// for the duration of HandlePayload, which is all it needs.
				if (!reassembler.TryAccept(framed, out ReadOnlyMemory<byte> payload)) return;

				HandlePayload(payload);
			}
			catch (Exception e)
			{
				if (!reliableChannel)
				{
					// Loss is expected here, so a broken message says nothing about the session. The
					// reassembler has already dropped its half-built buffer.
					Log.Warn($"NetherNet unreliable message discarded for {Username ?? NetworkId}: {e.Message}");
					return;
				}

				// On the reliable channel a frame we cannot parse means the stream is no longer
				// trustworthy. Dropping it and carrying on would desynchronise everything after it.
				Log.Error($"NetherNet receive failed for {Username ?? NetworkId}, closing session", e);
				Disconnect("Malformed packet", false);
			}
			finally
			{
				if (rented != null) ArrayPool<byte>.Shared.Return(rented);
			}
		}

		private void HandlePayload(ReadOnlyMemory<byte> payload)
		{
			ICustomMessageHandler handler = CustomMessageHandler;
			if (handler == null) return;

			// There is no 0xFE to parse: the reassembled bytes are the wrapper payload itself, starting
			// at the compressor id byte. Rebuild the wrapper around them as a VIEW, no copy: the
			// span-based decode consumes it synchronously right here on the receive thread, and only
			// the decoded packet objects, which own their memory, cross to the dispatch thread. One
			// player's login burst or slow handler must never stall every other session's inbound
			// behind it on the shared mux thread.
			var wrapper = McpeWrapper.CreateObject();
			wrapper.payload = payload;

			if (handler is BedrockMessageHandlerBase bedrock)
			{
				foreach (Packet msg in bedrock.DecodeBatch(wrapper))
				{
					// The transport.messages.in counting point: one complete game packet, post-reassembly
					// and post-ordering, counted before the inline/queued split so the number is the same
					// whichever path it takes.
					TransportMetrics.MessageIn();

					// A handler method the startup scan labeled verified (provably lock-free, no
					// plugin interceptor) runs right here, no queue hop and no wake - but only
					// while nothing is queued ahead, so per-session arrival order can never invert
					// between the two paths.
					//
					// It does run inside SctpAssociation.OnPacketReceived, ahead of that packet's
					// SACK, so a handler that earns the label must stay short as well as lock-free.
					if (Volatile.Read(ref _dispatchPending) == 0 && _closed == 0 && bedrock.CanDispatchInline(msg))
					{
						bedrock.HandleDecoded(msg);
					}
					else
					{
						Enqueue(msg);
					}
				}

				return;
			}

			// A handler outside the Bedrock base (a test recorder) has no decode/dispatch split;
			// the payload is copied so the whole wrapper can cross to the dispatch thread intact.
			TransportMetrics.MessageIn();
			wrapper.payload = payload.ToArray();
			Enqueue(wrapper);
		}

		// How many packets sit in _dispatchQueue not yet handled: the ordering guard for direct
		// dispatch (a verified packet may only run inline while this is zero, or it would overtake
		// queued predecessors). Incremented on enqueue, decremented by the dispatch loop after
		// each packet is handled.
		private int _dispatchPending;

		private void Enqueue(Packet packet)
		{
			Interlocked.Increment(ref _dispatchPending);

			// TryWrite on an unbounded channel only ever fails once the writer is completed: the
			// session closed between the handler check and here. The packet goes back to the pool
			// instead of leaking.
			if (!_dispatchQueue.Writer.TryWrite(packet))
			{
				Interlocked.Decrement(ref _dispatchPending);
				packet.PutPool();
			}
		}

		/// <summary>
		///     The per-session dispatch loop: everything above the transport (decompression, packet
		///     decode, login, game logic) runs here, one thread per session, so sessions are isolated
		///     from each other and the mux receive thread never executes game code. Ordering per
		///     session is preserved (single consumer); cross-session parallelism is the point.
		/// </summary>
		private async Task DispatchLoopAsync()
		{
			ChannelReader<Packet> reader = _dispatchQueue.Reader;
			while (await reader.WaitToReadAsync().ConfigureAwait(false))
			{
				while (reader.TryRead(out Packet packet))
				{
					try
					{
						ICustomMessageHandler handler = CustomMessageHandler;
						if (handler == null || _closed != 0)
						{
							packet.PutPool();
							continue;
						}

						if (handler is BedrockMessageHandlerBase bedrock) bedrock.HandleDecoded(packet);
						else handler.HandlePacket(packet);
					}
					catch (Exception e)
					{
						Log.Error($"NetherNet dispatch failed for {Username ?? NetworkId}; the session keeps serving.", e);
					}
					finally
					{
						// After handling, not after read: the direct-dispatch ordering guard reads
						// this as "queued ahead of you", which a packet still being handled is.
						Interlocked.Decrement(ref _dispatchPending);
					}
				}
			}
		}

		public void SendPacket(Packet packet)
		{
			if (packet == null) return;

			if (CustomMessageHandler == null || _closed != 0)
			{
				packet.PutPool();
				return;
			}

			// Producers only enqueue; the lane does the batching, compression and transport work.
			// TryWrite fails only once the writer is completed (the session closed between the check
			// above and here), and then the packet goes back to the pool instead of leaking.
			if (!_sendQueue.Writer.TryWrite(packet))
			{
				packet.PutPool();
				return;
			}

			Interlocked.Increment(ref _sendPending);
			TransportMetrics.MessageOut();
		}

		/// <summary>
		///     The per-session send lane: drains whatever has accumulated and runs it through the
		///     handler pipeline. PrepareSend folds the whole drain into as few wrappers
		///     as its rules allow (one batch per run of ordinary packets, pre-encoded wrappers pass
		///     through in order), HandleOrderedSend is where encryption would apply and is a no-op here
		///     because IsTransportEncrypted is true. Under light traffic each packet still leaves
		///     immediately; coalescing only kicks in exactly when sends back up, which is when it pays.
		/// </summary>
		private async Task SendLoopAsync()
		{
			ChannelReader<Packet> reader = _sendQueue.Reader;
			var pending = new List<Packet>();

			while (await reader.WaitToReadAsync().ConfigureAwait(false))
			{
				pending.Clear();
				while (reader.TryRead(out Packet packet)) pending.Add(packet);
				if (pending.Count > 0) Interlocked.Add(ref _sendPending, -pending.Count);

				// The drain-time upsert: everything that accumulated collapses to the last packet per
				// coalesce key before any of it is encoded or compressed. Consumer-private list, so
				// this needs no lock; see CoalescePending for why drain time is equivalent to an
				// in-queue upsert.
				CoalescePending(pending);

				ICustomMessageHandler handler = CustomMessageHandler;
				if (handler == null)
				{
					foreach (Packet packet in pending) packet.PutPool();
					continue;
				}

				try
				{
					foreach (Packet prepared in handler.PrepareSend(pending))
					{
						Packet message = handler.HandleOrderedSend(prepared);
						await SendRawAsync(message).ConfigureAwait(false);
					}
				}
				catch (Exception e)
				{
					// A throw here has already cost this drain's packets; what it must never cost is
					// the lane itself, which would silence the session with no error to the client.
					Log.Error($"NetherNet send lane failed a batch for {Username ?? NetworkId}", e);
				}
			}
		}

		/// <summary>
		///     There is no direct path on this transport: SCTP orders everything on the channel
		///     regardless, so both mean the same thing here. The distinction is a leftover from
		///     transports whose unordered sends bypassed their sequencing machinery.
		/// </summary>
		public void SendDirectPacket(Packet packet) => SendPacket(packet);

		/// <summary>
		///     The drain-time upsert. A packet carrying a <see cref="Packet.CoalesceKey" /> declares
		///     itself wholly superseded by any later packet with the same key: only the LAST one per key
		///     survives, in its own queue position; everything unkeyed is untouched. Done here, on the
		///     lane's private drain, instead of in the queue, because an in-queue upsert is only
		///     observable when the queue has depth, and the queue only has depth when the lane is behind,
		///     at which point the next drain collapses exactly the same survivors, with zero added
		///     synchronization. Superseded packets go back to the pool, which for a shared refcounted
		///     broadcast batch is this session's own reference, exactly as if it had been sent.
		/// </summary>
		internal static void CoalescePending(List<Packet> pending)
		{
			if (pending.Count < 2) return;

			Dictionary<object, int> lastIndexByKey = null;
			for (int i = 0; i < pending.Count; i++)
			{
				object key = pending[i].CoalesceKey;
				if (key == null) continue;

				lastIndexByKey ??= new Dictionary<object, int>();
				lastIndexByKey[key] = i;
			}

			if (lastIndexByKey == null) return;

			bool dropped = false;
			for (int i = 0; i < pending.Count; i++)
			{
				object key = pending[i].CoalesceKey;
				if (key == null || lastIndexByKey[key] == i) continue;

				pending[i].PutPool();
				pending[i] = null;
				dropped = true;
			}

			if (dropped) pending.RemoveAll(p => p == null);
		}

		/// <summary>
		///     Sends one prepared batch. The <see cref="McpeWrapper" />'s 0xFE id is deliberately not
		///     written: it exists to tell a Minecraft batch apart from RakNet's own control messages
		///     sharing one channel, and NetherNet has nothing to tell it apart from. Confirmed against
		///     df-mc/go-nethernet, whose Conn implements gophertunnel's BatchHeaderer and returns a nil
		///     batch header for exactly this reason. So the segment byte is followed straight by the
		///     wrapper payload: compressor id byte, then the deflated batch.
		///     <para>
		///     Before anything is handed to the channel, the lane parks here while the association's
		///     send window is full: this is where backpressure terminates. Nothing above ever learns
		///     (SendPacket always succeeds; the game cannot pause), nothing below ever drops (the
		///     association has no budget to refuse on), the lane just waits for SACKs to open the
		///     window and the queue absorbs meanwhile, with the upsert keeping supersedable traffic
		///     flat. A dead peer never parks it forever: teardown signals the same wake, HasSendRoom
		///     reports true off-Established, and the channel send then fails fast and logs. The
		///     500ms re-check is belt and braces against a lost wake, not the mechanism.
		///     </para>
		/// </summary>
		private async Task SendRawAsync(Packet message)
		{
			try
			{
				ReadOnlyMemory<byte> encoded = message is McpeWrapper wrapper ? wrapper.payload : message.EncodeAsMemory();
				if (encoded.Length == 0) return;

				if (Log.IsDebugEnabled) Log.Debug($"NetherNet send {encoded.Length} bytes: {Packet.HexDump(encoded.Span.Slice(0, Math.Min(32, encoded.Length)).ToArray(), 32)}");

				// Fast path first: one volatile read, no subscription, no allocation. Only a full
				// window enters the park loop, and there the order is subscribe-then-check, because
				// the lazy signal (see SctpAssociation.WhenSendRoom) is consumed by whoever signals:
				// checking before subscribing could lose a wake that landed in the gap, and then only
				// the 500ms backstop would save the lane.
				if (!_reliable.HasSendRoom)
				{
					while (_closed == 0)
					{
						Task roomSignal = _reliable.WhenSendRoom();
						if (_reliable.HasSendRoom) break;

						// The close signal is what lets Close's drain finish in microseconds on a
						// dead transport; the 500ms delay stays as the lost-wake backstop only.
						await Task.WhenAny(roomSignal, _closedSignal.Task, Task.Delay(500)).ConfigureAwait(false);
					}
				}

				if (_closed != 0) return; // finally still pools the message

				// No lock: the send lane is the only caller, and one consumer per session is the
				// serialization. One pooled buffer, one copy, and send takes an offset and count so
				// the buffer does not have to be exactly the segment's size.
				NetherNetSegments.ForEachSegment(encoded.Span, MaxSegmentBytes, _reliable,
					static (channel, buffer, length) => channel.Send(buffer.AsSpan(0, length), asString: false));
			}
			catch (Exception e)
			{
				Log.Error($"NetherNet send failed for {Username ?? NetworkId}", e);
			}
			finally
			{
				message.PutPool();
			}
		}

		public IPEndPoint GetClientEndPoint() => EndPoint;

		public long GetNetworkNetworkIdentifier() => NetworkIdentifier;

		public virtual void Disconnect(string reason, bool sendDisconnect = true)
		{
			CustomMessageHandler?.Disconnect(reason, sendDisconnect);
			Close();
		}

		public void Close()
		{
			// Close arrives from the transport, from the player and from teardown, so it has to be
			// idempotent.
			if (Interlocked.Exchange(ref _closed, 1) != 0) return;

			// Wake a parked send lane immediately: with _closed set it bails on this wake, so the
			// drain below returns in microseconds instead of eating the lane's 500ms backstop.
			_closedSignal.TrySetResult();

			// Drain the send lane while the session can still send: Player.Disconnect enqueues the
			// McpeDisconnect and calls Close right behind it, so tearing the peer down before the
			// lane has flushed would eat the kick reason every time and the client would only ever
			// see a generic transport error.
			// Completing the writer ends the lane's loop once the queue is empty; the wait is
			// bounded because a dead transport just makes the remaining sends fail-and-log.
			_sendQueue.Writer.TryComplete();
			try
			{
				_sendLoop?.Wait(500);
			}
			catch (Exception e)
			{
				Log.Debug("Draining NetherNet send lane on close", e);
			}

			CustomMessageHandler = null;

			// Completes the channel: the reader drains what remains to the pool (the handler is
			// already null) and its loop ends.
			_dispatchQueue.Writer.TryComplete();

			try
			{
				// Disposing the peer tears down ICE, DTLS and the SCTP association together, which
				// takes both data channels with it. This never re-raises OnTransportClosed: RtcPeer's
				// own disposed guard refuses to fire it once Dispose has started.
				_peer.Dispose();
			}
			catch (Exception e)
			{
				Log.Debug("Closing NetherNet peer", e);
			}

			TransportMetrics.SessionClosed((Stopwatch.GetTimestamp() - _openedAt) / (double) Stopwatch.Frequency);

			Log.Info($"NetherNet session closed for {Username ?? NetworkId}");

			OnClosed?.Invoke(this);
		}
	}
}
