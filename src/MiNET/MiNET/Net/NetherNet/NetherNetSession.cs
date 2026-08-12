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
using System.Net;
using System.Threading;
using log4net;
using MiNET.Net.RakNet;
using MiNET.Net.Rtc;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     A NetherNet session, the counterpart to <see cref="RakSession" />. Almost everything
	///     RakSession does is reliability work that SCTP has already done by the time a message
	///     surfaces here: ordering, retransmission, acknowledgement and datagram splitting all happen
	///     below. What is left is the part RakNet never owned, which is Bedrock's own batching and
	///     compression, and that is reached through the same <see cref="ICustomMessageHandler" /> the
	///     RakNet path uses, so both transports put identical bytes on the wire above the framing.
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

		private readonly object _sendLock = new object();

		private int _closed;
		private int _sawUnreliableTraffic;
		private int _hasReceived;
		private long _lastReceiveTicks = Environment.TickCount64;

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

		public string Username { get; set; }

		/// <summary>
		///     DTLS has already encrypted and authenticated everything on this channel, so Bedrock's
		///     session cipher is not applied on top. Confirmed against Mojang: the transport handles it
		///     before a payload reaches us.
		/// </summary>
		public bool IsTransportEncrypted => true;

		public string TransportName => "NetherNet";

		/// <summary>
		///     The ICE-nominated remote address. Unlike RakNet this is not known until the connection
		///     is established, and it can be a relay rather than the player's own address.
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
			// at the compressor id byte. So rebuild the wrapper around them and let the message handler
			// decompress and split the batch exactly as it does for RakNet.
			var wrapper = McpeWrapper.CreateObject();
			wrapper.payload = payload;

			handler.HandlePacket(wrapper);
		}

		public void SendPacket(Packet packet)
		{
			if (packet == null) return;

			ICustomMessageHandler handler = CustomMessageHandler;
			if (handler == null || _closed != 0)
			{
				packet.PutPool();
				return;
			}

			// Same pipeline the RakNet path runs: PrepareSend batches and compresses, HandleOrderedSend
			// is where encryption would apply and is a no-op here because IsTransportEncrypted is true.
			foreach (Packet prepared in handler.PrepareSend(new List<Packet> {packet}))
			{
				Packet message = handler.HandleOrderedSend(prepared);
				SendRaw(message);
			}
		}

		/// <summary>
		///     There is no direct path on this transport. RakNet distinguishes them because ordered
		///     sends go through its sequencing machinery and unordered ones bypass it; SCTP orders
		///     everything on the channel regardless, so both mean the same thing here.
		/// </summary>
		public void SendDirectPacket(Packet packet) => SendPacket(packet);

		/// <summary>
		///     Sends one prepared batch. The <see cref="McpeWrapper" />'s 0xFE id is deliberately not
		///     written: it exists to tell a Minecraft batch apart from RakNet's own control messages
		///     sharing one channel, and NetherNet has nothing to tell it apart from. Confirmed against
		///     df-mc/go-nethernet, whose Conn implements gophertunnel's BatchHeaderer and returns a nil
		///     batch header for exactly this reason. So the segment byte is followed straight by the
		///     wrapper payload: compressor id byte, then the deflated batch.
		/// </summary>
		private void SendRaw(Packet message)
		{
			try
			{
				// Kept as a span onto the existing buffer. Materialising it would copy the whole batch
				// for no reason: the only thing that has to move is the one header byte in front.
				ReadOnlySpan<byte> encoded = message is McpeWrapper wrapper ? wrapper.payload.Span : message.Encode();
				if (encoded.Length == 0) return;

				if (Log.IsDebugEnabled) Log.Debug($"NetherNet send {encoded.Length} bytes: {Packet.HexDump(encoded.Slice(0, Math.Min(32, encoded.Length)).ToArray(), 32)}");

				lock (_sendLock)
				{
					// One pooled buffer, one copy, and send takes an offset and count so the buffer
					// does not have to be exactly the segment's size.
					NetherNetSegments.ForEachSegment(encoded, MaxSegmentBytes, _reliable,
						static (channel, buffer, length) => channel.Send(buffer.AsSpan(0, length), asString: false));
				}
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
			// idempotent the way RakSession's is.
			if (Interlocked.Exchange(ref _closed, 1) != 0) return;

			CustomMessageHandler = null;

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

			Log.Info($"NetherNet session closed for {Username ?? NetworkId}");

			OnClosed?.Invoke(this);
		}
	}
}
