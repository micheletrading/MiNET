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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MiNET.Net.RakNet;
using MiNET.Utils;
using SIPSorcery.Net;
using SIPSorcery.Sys;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     Accepts NetherNet connections, the counterpart to <see cref="RakConnection" />.
	///     <para>
	///         Signaling is a single HTTP round trip on a TCP port, so this is a small web server
	///         rather than a packet loop: <c>GET /v1/join</c> answers whether we speak NetherNet at
	///         all, and <c>POST /v1/join/{networkId}</c> carries the client's SDP offer and returns
	///         our answer. Everything after that is WebRTC, and the data channels the client opens
	///         become a <see cref="NetherNetSession" />.
	///     </para>
	///     <para>
	///         Written on a raw TcpListener rather than HttpListener because the latter needs a URL
	///         reservation or elevation on Windows for anything but a loopback prefix, and two fixed
	///         routes do not justify that.
	///     </para>
	/// </summary>
	public class NetherNetListener
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetherNetListener));

		private static readonly Regex JoinRoute = new(@"^/v1/join/(?<networkId>[^/\s?]+)", RegexOptions.Compiled);

		private readonly IPEndPoint _endPoint;
		private TcpListener _listener;
		private CancellationTokenSource _cancellation;
		private PortRange _portRange;
		private Timer _inactivityTimer;

		// Same knob as RakSession's, so the two transports evict a silent client on the same clock.
		private readonly int _inactivityTimeout = Config.GetProperty("InactivityTimeout", 8500);

		// A client that has connected but not yet spoken gets longer: 8.5s here turns a slow join
		// into a failed one, and a session that never speaks is only holding a port, not a slot in
		// anyone's game. Matches the 30s spawn budget the emulator itself allows a join.
		private readonly int _connectingTimeout = Config.GetProperty("NetherNetConnectingTimeout", 30000);

		/// <summary>Live sessions by the client's NetworkID.</summary>
		public ConcurrentDictionary<string, NetherNetSession> Sessions { get; } = new();

		/// <summary>
		///     Builds the handler that sits above the transport, exactly as RakConnection does, so
		///     both transports share the batching, compression and login path.
		/// </summary>
		public Func<NetherNetSession, ICustomMessageHandler> CustomMessageHandlerFactory { get; set; }

		/// <summary>
		///     The long-lived key clients pin us by. Persisted, because regenerating it makes every
		///     returning player see a first-use prompt again.
		/// </summary>
		public NetherNetServerIdentity ServerIdentity { get; set; }

		/// <summary>Where gameplay UDP binds, and what address clients are told to dial.</summary>
		public NetherNetPortMapping PortMapping { get; set; }

		public NetherNetListener(IPEndPoint endPoint, NetherNetServerIdentity serverIdentity = null, NetherNetPortMapping portMapping = null)
		{
			_endPoint = endPoint;
			ServerIdentity = serverIdentity ?? new NetherNetServerIdentity();
			PortMapping = portMapping ?? NetherNetPortMapping.Parse(Config.GetProperty("server-udp-ports", ""));
		}

		public void Start()
		{
			// SIPSorcery's ICE liveness defaults (8s to "disconnected", 16s to the terminal
			// "failed", on missed 3s STUN checks) are tuned for a browser call. A loaded game
			// server misses checks without being dead, and the inactivity sweep owns real death
			// detection here, so ICE gets far more patience. Process-wide statics, config-tunable.
			RtpIceChannel.DISCONNECTED_TIMEOUT_PERIOD = Config.GetProperty("NetherNet.IceDisconnectedTimeout", 60);
			RtpIceChannel.FAILED_TIMEOUT_PERIOD = Config.GetProperty("NetherNet.IceFailedTimeout", 120);

			_cancellation = new CancellationTokenSource();

			// Dual stack when no specific address was asked for, which is what BDS does: one IPv6
			// socket with DualMode serves both families. Binding 0.0.0.0 instead means a client that
			// resolves the address to ::1 finds nothing listening and the join simply never arrives,
			// with no error anywhere to explain it.
			if (_endPoint.Address.Equals(IPAddress.Any) && Socket.OSSupportsIPv6)
			{
				_listener = new TcpListener(IPAddress.IPv6Any, _endPoint.Port);
				_listener.Server.DualMode = true;
			}
			else
			{
				_listener = new TcpListener(_endPoint);
			}

			_listener.Start();

			// One PortRange for the listener's lifetime. SIPSorcery walks it from an internal cursor
			// and gives up after 25 bind attempts, so a fresh instance per connection re-walks the
			// same occupied ports from the start of the range and caps the server at 25 sessions no
			// matter how wide the range is.
			_portRange = PortMapping.RangeStart.HasValue
				? new PortRange(PortMapping.RangeStart.Value, PortMapping.RangeEnd.Value)
				: null;

			_inactivityTimer = new Timer(_ => SweepInactiveSessions(), null, 2500, 2500);

			Log.Info($"NetherNet signaling listening on tcp {_listener.LocalEndpoint} (dual stack: {_listener.Server.DualMode})");

			_ = Task.Run(() => AcceptLoop(_cancellation.Token));
		}

		public void Stop()
		{
			_cancellation?.Cancel();
			_listener?.Stop();
			_inactivityTimer?.Dispose();
			_inactivityTimer = null;

			foreach (NetherNetSession session in Sessions.Values) session.Close();
			Sessions.Clear();
		}

		/// <summary>
		///     The backstop that actually notices a vanished client. SCTP surfaces no remote close and
		///     ICE state can sit in connected forever, but a live client is never silent, so silence
		///     past the timeout is the one signal that always arrives. Closing the session is also what
		///     returns its gameplay UDP port to the range.
		/// </summary>
		private void SweepInactiveSessions()
		{
			foreach (NetherNetSession session in Sessions.Values)
			{
				// A session that closed before OnClosed was wired up would otherwise sit here forever.
				if (session.IsClosed)
				{
					Sessions.TryRemove(new KeyValuePair<string, NetherNetSession>(session.NetworkId, session));
					continue;
				}

				// Connecting means "until login completes", not "until the first byte": a client
				// stuck fetching its auth token has typically already sent RequestNetworkSettings,
				// and judging it on the in-game clock cuts it off mid-recovery.
				bool loggedIn = session.Username != null;
				int timeout = loggedIn ? _inactivityTimeout : _connectingTimeout;
				if (session.MillisSinceLastReceive <= timeout) continue;

				string phase = loggedIn ? "" : session.HasReceived ? " (pre-login)" : " (never spoke)";
				Log.Warn($"NetherNet session for {session.Username ?? session.NetworkId} timed out, no traffic for {timeout}ms{phase}");
				session.Disconnect("Network timeout", false);
			}
		}

		private async Task AcceptLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
					// Signaling is one request per connection, so each is handled and dropped.
					_ = Task.Run(() => HandleSignaling(client), cancellationToken);
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception e)
				{
					if (!cancellationToken.IsCancellationRequested) Log.Error("NetherNet signaling accept failed", e);
				}
			}
		}

		private async Task HandleSignaling(TcpClient client)
		{
			using (client)
			{
				try
				{
					NetworkStream stream = client.GetStream();

					// The real client tries TLS before plaintext. It must be told no in a way it
					// understands, or it never falls back: a reset or silence leaves it with a broken
					// handshake rather than a refusal. BDS answers a ClientHello with an alert 40,
					// handshake_failure, and closes, which is what makes the client retry in the
					// clear and reach the trust-on-first-use path.
					if (await RefuseTlsIfOffered(stream)) return;

					(string method, string path, string headers, string body) = await ReadRequest(stream);

					// Signaling is one round trip per connection and the whole negotiation lives in
					// it, so the full exchange is logged. A client that refuses us leaves no other
					// trace: there is no error packet, it simply stops.
					Log.Info($"NetherNet signaling <<< {client.Client.RemoteEndPoint}\n{headers}\n{body}");

					if (method == "GET" && path.StartsWith("/v1/join", StringComparison.Ordinal))
					{
						// Any 2xx means "yes, we speak NetherNet". The body is ignored by the client.
						await Respond(stream, 200, "text/plain", "");
						return;
					}

					Match route = JoinRoute.Match(path);
					if (method != "POST" || !route.Success)
					{
						await Respond(stream, 404, "text/plain", "");
						return;
					}

					if (string.IsNullOrWhiteSpace(body))
					{
						await Respond(stream, 400, "text/plain", "Missing SDP offer in request body");
						return;
					}

					string networkId = route.Groups["networkId"].Value;
					string answer = await Negotiate(networkId, body, IsLoopbackPeer(client));

					await Respond(stream, 200, "application/sdp", answer);
				}
				catch (Exception e)
				{
					// Naming the peer matters more than the exception: a bare reset is what a probe
					// looks like, and without an address there is no way to tell a real client
					// checking us from a port scanner.
					Log.Warn($"NetherNet signaling request from {SafePeer(client)} failed: {e.Message}");
				}
			}
		}

		/// <summary>
		///     Peeks at the first byte and, if it is a TLS handshake record, answers with a fatal
		///     handshake_failure alert and gives up the connection. Mirrors BDS, which does exactly
		///     this rather than serving TLS or ignoring it.
		/// </summary>
		private static async Task<bool> RefuseTlsIfOffered(NetworkStream stream)
		{
			var first = new byte[1];
			// Peek rather than read, so a plain HTTP request keeps its first byte.
			int peeked = stream.Socket.Receive(first, SocketFlags.Peek);
			if (peeked == 0 || first[0] != 0x16) return false;

			Log.Info("NetherNet signaling: client offered TLS, refusing with handshake_failure so it falls back to plaintext");

			// Alert record: content type 21, TLS 1.0 version for maximum compatibility with a peer
			// whose negotiated version is not yet known, length 2, level fatal (2), handshake_failure (40).
			await stream.WriteAsync(new byte[] {0x15, 0x03, 0x01, 0x00, 0x02, 0x02, 0x28});
			await stream.FlushAsync();

			return true;
		}

		/// <summary>
		///     Whether the signaling connection came from this machine. Same mapped-address
		///     normalization as elsewhere: a dual stack listener reports a v4 loopback peer as
		///     ::ffff:127.0.0.1. Note that a LAN client dialing the public name arrives hairpinned
		///     with the router's address, so it is correctly NOT loopback.
		/// </summary>
		private static bool IsLoopbackPeer(TcpClient client)
		{
			try
			{
				if (client.Client?.RemoteEndPoint is not IPEndPoint remote) return false;

				IPAddress address = remote.Address;
				if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
				return IPAddress.IsLoopback(address);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>A disposed or reset socket throws on RemoteEndPoint, which must not hide the log line.</summary>
		private static string SafePeer(TcpClient client)
		{
			try
			{
				return client.Client?.RemoteEndPoint?.ToString() ?? "unknown";
			}
			catch
			{
				return "unknown";
			}
		}

		private async Task<string> Negotiate(string networkId, string offerSdp, bool loopbackPeer)
		{
			// No STUN or TURN: we publish our own addresses and the client dials them, which is what
			// a directly reachable server needs and what the real client expects on this path.
			// Pinning the range is what makes the gameplay path forwardable: the default is an
			// ephemeral port, and you cannot open a hole in a firewall for a port you cannot predict.
			// A loopback peer (the emulator fleet) needs none of that: it gets an OS-ephemeral port,
			// which never contends and never exhausts, and leaves the whole pinned range to peers
			// that actually dial through the firewall.
			var peerConnection = new RTCPeerConnection(new RTCConfiguration {iceServers = null}, 0, loopbackPeer ? null : _portRange);

			var reliable = new TaskCompletionSource<RTCDataChannel>(TaskCreationOptions.RunContinuationsAsynchronously);
			RTCDataChannel unreliable = null;

			peerConnection.ondatachannel += channel =>
			{
				Log.Debug($"NetherNet data channel opened by {networkId}: {channel.label}");

				if (channel.label == NetherNetClientConnector.UnreliableChannelLabel) unreliable = channel;
				else reliable.TrySetResult(channel);
			};

			var gatheringComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			peerConnection.onicegatheringstatechange += state =>
			{
				if (state == RTCIceGatheringState.complete) gatheringComplete.TrySetResult(true);
			};

			// The identity assertion is ours to validate or ignore by policy, but it must come out
			// before setRemoteDescription either way: a=identity is not an attribute WebRTC knows,
			// and implementations reject an SDP carrying attributes they cannot parse.
			string strippedOffer = StripIdentity(offerSdp, out string assertion);
			if (assertion != null) Log.Debug($"NetherNet offer from {networkId} carries an identity assertion ({assertion.Length} chars)");

			SetDescriptionResultEnum result = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
			{
				type = RTCSdpType.offer,
				sdp = strippedOffer
			});

			if (result != SetDescriptionResultEnum.OK) throw new IOException($"NetherNet offer rejected: {result}");

			RTCSessionDescriptionInit answer = peerConnection.createAnswer();
			await peerConnection.setLocalDescription(answer);

			// Full ICE: the answer has to carry every candidate we will ever offer, because there is
			// no second message in which to send more.
			await gatheringComplete.Task;

			_ = Task.Run(async () =>
			{
				try
				{
					RTCDataChannel channel = await reliable.Task;
					AttachSession(networkId, peerConnection, channel, unreliable);
				}
				catch (Exception e)
				{
					Log.Error($"NetherNet session setup failed for {networkId}", e);
				}
			});

			// Required in every answer, over HTTP and HTTPS alike: a client that finds no valid
			// a=identity refuses the connection outright.
			// Rewrite candidates before signing: the assertion covers the fingerprints, not the
			// candidates, but the client must receive one coherent answer and the addresses it dials
			// have to be the mapped ones. Except for a loopback peer: its ephemeral port has no
			// mapping, and rewriting would hand it public addresses whose ports are not forwarded.
			string answerSdp = peerConnection.localDescription.sdp.ToString();
			if (!loopbackPeer) answerSdp = PortMapping.Apply(answerSdp);

			return NetherNetIdentityAssertion.AddServerAssertionTo(
				answerSdp, ServerIdentity.Key, ServerIdentity.Domain, ServerIdentity.Issuer);
		}

		private void AttachSession(string networkId, RTCPeerConnection peerConnection, RTCDataChannel reliable, RTCDataChannel unreliable)
		{
			// NetherNetSession now takes an RtcPeer/RtcDataChannel pair (the in-house Rtc stack), and
			// this listener still negotiates over SIPSorcery's RTCPeerConnection/RTCDataChannel. The
			// listener's own rebuild onto RtcPeer is a separate, later change; until then it cannot
			// hand NetherNetSession the objects its constructor now requires.
			throw new NotSupportedException("NetherNetListener still negotiates over SIPSorcery and cannot construct a NetherNetSession until it is rebuilt onto RtcPeer.");
		}

		/// <summary>
		///     Removes the session level a=identity line, returning it separately so a caller that
		///     wants to authenticate can, while the SDP handed to WebRTC stays parseable.
		/// </summary>
		public static string StripIdentity(string sdp, out string assertion)
		{
			assertion = null;

			var kept = new StringBuilder(sdp.Length);
			foreach (string line in sdp.Split('\n'))
			{
				string trimmed = line.TrimEnd('\r');
				if (trimmed.StartsWith("a=identity:", StringComparison.Ordinal))
				{
					assertion = trimmed.Substring("a=identity:".Length);
					continue;
				}

				kept.Append(trimmed).Append("\r\n");
			}

			return kept.ToString();
		}

		private static async Task<(string method, string path, string headers, string body)> ReadRequest(NetworkStream stream)
		{
			var buffer = new byte[64 * 1024];
			var raw = new StringBuilder();
			int contentLength = 0;
			int headerEnd = -1;

			while (true)
			{
				int read;
				try
				{
					read = await stream.ReadAsync(buffer);
				}
				catch (IOException) when (raw.Length > 0)
				{
					// A peer that resets mid-request has still told us something. Losing those bytes
					// to the exception is what made this look like no contact at all.
					Log.Warn($"NetherNet signaling connection reset after {raw.Length} bytes:\n{raw}");
					throw;
				}

				if (read == 0) break;

				raw.Append(Encoding.UTF8.GetString(buffer, 0, read));
				string text = raw.ToString();

				if (headerEnd < 0)
				{
					headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
					if (headerEnd < 0) continue;

					Match length = Regex.Match(text.Substring(0, headerEnd), @"Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
					contentLength = length.Success ? int.Parse(length.Groups[1].Value) : 0;
				}

				if (text.Length - (headerEnd + 4) >= contentLength) break;
			}

			string request = raw.ToString();

			// Whatever arrived is returned even when it is not a request we can parse, because a
			// client that gives up mid-handshake leaves nothing else to look at.
			if (headerEnd < 0) return (null, null, request, null);

			string headers = request.Substring(0, headerEnd);
			string[] requestLine = headers.Substring(0, headers.IndexOf("\r\n", StringComparison.Ordinal)).Split(' ');

			return (requestLine[0], requestLine.Length > 1 ? requestLine[1] : "", headers, request.Substring(headerEnd + 4));
		}

		private static async Task Respond(NetworkStream stream, int status, string contentType, string body)
		{
			byte[] payload = Encoding.UTF8.GetBytes(body);
			string head = $"HTTP/1.1 {status} {(status == 200 ? "OK" : status == 404 ? "Not Found" : "Bad Request")}\r\n"
						+ $"Content-Type: {contentType}\r\n"
						+ $"Content-Length: {payload.Length}\r\n"
						+ "Connection: close\r\n\r\n";

			Log.Info($"NetherNet signaling >>> {status}{Environment.NewLine}{head}{body}");

			await stream.WriteAsync(Encoding.UTF8.GetBytes(head));
			await stream.WriteAsync(payload);
			await stream.FlushAsync();
		}
	}
}
