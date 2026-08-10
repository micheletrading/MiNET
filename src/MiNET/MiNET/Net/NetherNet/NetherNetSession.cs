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
using System.Collections.Generic;
using System.Net;
using System.Threading;
using log4net;
using MiNET.Net.RakNet;
using SIPSorcery.Net;

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

		private readonly RTCPeerConnection _peerConnection;
		private readonly RTCDataChannel _reliable;
		private readonly RTCDataChannel _unreliable;

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

		public NetherNetSession(RTCPeerConnection peerConnection, RTCDataChannel reliable, RTCDataChannel unreliable, IPEndPoint endPoint, string networkId)
		{
			_peerConnection = peerConnection ?? throw new ArgumentNullException(nameof(peerConnection));
			_reliable = reliable ?? throw new ArgumentNullException(nameof(reliable));
			_unreliable = unreliable;

			EndPoint = endPoint;
			NetworkId = networkId;
			NetworkIdentifier = long.TryParse(networkId, out long id) ? id : networkId?.GetHashCode() ?? 0;

			// Losing the reliable channel is losing the session. Mojang's guide has the client open
			// both, so a missing unreliable channel is unexpected but not fatal.
			_reliable.onmessage += (channel, protocol, data) => OnDataChannelMessage(_reliableReassembler, data, reliableChannel: true);
			_reliable.onclose += () => Disconnect("Data channel closed", false);
			_reliable.onerror += error => Log.Warn($"NetherNet reliable channel error for {Username ?? NetworkId}: {error}");

			if (_unreliable != null)
			{
				_unreliable.onmessage += (channel, protocol, data) => OnDataChannelMessage(_unreliableReassembler, data, reliableChannel: false);
				_unreliable.onclose += () => Log.Info($"NetherNet unreliable channel closed for {Username ?? NetworkId}, session continues");
				_unreliable.onerror += error => Log.Warn($"NetherNet unreliable channel error for {Username ?? NetworkId}: {error}");
			}
			else
			{
				Log.Warn($"NetherNet client {networkId} opened no unreliable channel");
			}

			// The onclose above only ever fires for closes this side initiates: SIPSorcery has no
			// SCTP stream reset, so a remote's channel close arrives as nothing at all. Connection
			// state is the transport's own liveness signal; the listener's inactivity sweep is the
			// backstop for peers that vanish without one. Note that "disconnected" is NOT fatal:
			// it means missed STUN checks and may recover (under load-test squeeze it regularly
			// does); killing on it cost ~465 sessions in one 1000-bot run. The sweep decides.
			_peerConnection.onconnectionstatechange += state =>
			{
				if (state is RTCPeerConnectionState.closed or RTCPeerConnectionState.failed)
				{
					Disconnect($"Connection {state}", false);
				}
				else if (state == RTCPeerConnectionState.disconnected)
				{
					Log.Info($"NetherNet connection for {Username ?? NetworkId} reports disconnected; waiting for recovery or the inactivity sweep");
				}
			};
		}

		private void OnDataChannelMessage(NetherNetSegmentReassembler reassembler, byte[] data, bool reliableChannel)
		{
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
					Log.Warn($"NetherNet client {Username ?? NetworkId} is sending on the UNRELIABLE channel, first message {data.Length} bytes, header byte 0x{(data.Length > 0 ? data[0] : 0):X2}");
				}

				// False means the message is still being assembled from segments. The payload is a
				// view onto either the channel's own buffer or the reassembler's pooled one, valid
				// only for the duration of HandlePayload, which is all it needs.
				if (Log.IsDebugEnabled) Log.Debug($"NetherNet recv {data.Length} bytes on {(reliableChannel ? "reliable" : "unreliable")}: {Packet.HexDump(data.AsSpan(0, Math.Min(32, data.Length)).ToArray(), 32)}");

				if (!reassembler.TryAccept(data, out ReadOnlyMemory<byte> payload)) return;

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

				// maxMessageSize is negotiated in the SDP; the sender must split anything larger.
				int maxMessageSize = (int) Math.Min(int.MaxValue, _peerConnection.sctp?.maxMessageSize ?? 262144);

				if (Log.IsDebugEnabled) Log.Debug($"NetherNet send {encoded.Length} bytes: {Packet.HexDump(encoded.Slice(0, Math.Min(32, encoded.Length)).ToArray(), 32)}");

				lock (_sendLock)
				{
					// One pooled buffer, one copy, and send takes an offset and count so the buffer
					// does not have to be exactly the segment's size.
					NetherNetSegments.ForEachSegment(encoded, maxMessageSize, _reliable,
						static (channel, buffer, length) => channel.send(buffer, 0, length));
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
			// Close arrives from the data channel, from the player and from teardown, so it has to be
			// idempotent the way RakSession's is.
			if (Interlocked.Exchange(ref _closed, 1) != 0) return;

			CustomMessageHandler = null;

			try
			{
				_reliable.close();
				_unreliable?.close();
			}
			catch (Exception e)
			{
				Log.Debug("Closing NetherNet data channels", e);
			}

			try
			{
				_peerConnection.close();
			}
			catch (Exception e)
			{
				Log.Debug("Closing NetherNet peer connection", e);
			}

			Log.Info($"NetherNet session closed for {Username ?? NetworkId}");

			OnClosed?.Invoke(this);
		}
	}
}
