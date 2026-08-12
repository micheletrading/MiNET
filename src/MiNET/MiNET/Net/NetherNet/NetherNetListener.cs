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
using MiNET.Net.Rtc;
using MiNET.Utils;

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
	///     <para>
	///         The gameplay path runs on one <see cref="UdpMux" /> for the listener's whole lifetime,
	///         shared by every negotiated peer, and one <see cref="RtcCertificate" /> is the DTLS
	///         identity every one of them answers with. Negotiating a peer no longer waits on
	///         anything: <see cref="RtcPeer.AcceptOffer" /> has nothing to gather (the mux is already
	///         bound, so there is exactly one local candidate) and returns the answer synchronously.
	///     </para>
	/// </summary>
	public class NetherNetListener
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetherNetListener));

		private static readonly Regex JoinRoute = new(@"^/v1/join/(?<networkId>[^/\s?]+)", RegexOptions.Compiled);

		private readonly IPEndPoint _endPoint;
		private TcpListener _listener;
		private CancellationTokenSource _cancellation;
		private UdpMux _mux;
		private RtcCertificate _certificate;
		private Timer _sweepTimer;

		// Same knob as RakSession's, so the two transports evict a silent client on the same clock.
		private readonly int _inactivityTimeout = Config.GetProperty("InactivityTimeout", 8500);

		// A client that has connected but not yet spoken gets longer: 8.5s here turns a slow join
		// into a failed one, and a session that never speaks is only holding a port, not a slot in
		// anyone's game. Matches the 30s spawn budget the emulator itself allows a join. Also the
		// deadline a negotiated-but-not-yet-attached RtcPeer gets in the pending-peer table below.
		// A constructor override exists so a test can shorten it without touching server.conf.
		private readonly int _connectingTimeout;

		/// <summary>Live sessions by the client's NetworkID.</summary>
		public ConcurrentDictionary<string, NetherNetSession> Sessions { get; } = new();

		/// <summary>
		///     Negotiated peers whose reliable data channel has not opened yet, keyed by the peer
		///     itself (nothing about a client that never gets this far is worth keying on) with the
		///     tick each one must attach by. <see cref="AttachSession" /> removes an entry the moment
		///     it promotes it to a session; <see cref="SweepExpiredPendingPeers" /> disposes whatever
		///     is left past its deadline, so a negotiation nobody ever finishes - ICE that never
		///     nominates, DTLS that never completes, a reliable channel that never opens - cannot hold
		///     its slice of <see cref="_mux" /> forever.
		/// </summary>
		private readonly ConcurrentDictionary<RtcPeer, long> _pendingPeers = new();

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): how many negotiated peers are still waiting for their reliable channel to open.</summary>
		internal int PendingPeerCount => _pendingPeers.Count;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): the TCP endpoint actually bound, once <see cref="Start" /> has run - lets a test bind an ephemeral port (0) and discover which one the OS chose.</summary>
		internal IPEndPoint LocalEndPoint => (IPEndPoint) _listener?.LocalEndpoint;

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

		public NetherNetListener(IPEndPoint endPoint, NetherNetServerIdentity serverIdentity = null, NetherNetPortMapping portMapping = null, int? connectingTimeout = null)
		{
			_endPoint = endPoint;
			ServerIdentity = serverIdentity ?? new NetherNetServerIdentity();
			PortMapping = portMapping ?? NetherNetPortMapping.Parse(Config.GetProperty("server-udp-ports", ""));
			_connectingTimeout = connectingTimeout ?? Config.GetProperty("NetherNetConnectingTimeout", 30000);
		}

		public void Start()
		{
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

			// One UdpMux and one RtcCertificate for the listener's whole lifetime: every RtcPeer
			// this listener answers with shares both, which is what lets one UDP socket carry every
			// session (ICE demultiplexes by ufrag, DTLS/SCTP by the nominated remote endpoint).
			// Bound to the first port of the configured range: unlike a per-connection socket there
			// is no bind-cursor to walk further into a wider one, port 0 leaves the choice to the OS
			// when nothing was configured.
			_mux = new UdpMux(new IPEndPoint(_endPoint.Address, PortMapping.BindPort ?? 0));
			_mux.Start();
			_certificate = RtcCertificate.CreateSelfSigned();

			Log.Info($"NetherNet gameplay UDP bound to {_mux.LocalEndPoint}");
			if (PortMapping.RangeStart.HasValue && PortMapping.RangeEnd.HasValue && PortMapping.RangeEnd.Value > PortMapping.RangeStart.Value)
			{
				Log.Warn($"server-udp-ports configures {PortMapping.RangeStart}-{PortMapping.RangeEnd}, but one shared UdpMux binds a single UDP port; only {_mux.LocalEndPoint.Port} is used.");
			}

			_sweepTimer = new Timer(_ => Sweep(), null, 2500, 2500);

			Log.Info($"NetherNet signaling listening on tcp {_listener.LocalEndpoint} (dual stack: {_listener.Server.DualMode})");

			_ = Task.Run(() => AcceptLoop(_cancellation.Token));
		}

		public void Stop()
		{
			_cancellation?.Cancel();
			_listener?.Stop();
			_sweepTimer?.Dispose();
			_sweepTimer = null;

			foreach (NetherNetSession session in Sessions.Values) session.Close();
			Sessions.Clear();

			foreach (RtcPeer peer in _pendingPeers.Keys)
			{
				if (_pendingPeers.TryRemove(peer, out _)) DisposePendingPeer(peer);
			}

			// Every peer above has already released its slice of the mux (RtcPeer.Dispose removes its
			// own ufrag registration), so the mux and the certificate they all shared can go last.
			_mux?.Dispose();
			_mux = null;
			_certificate?.Dispose();
			_certificate = null;
		}

		/// <summary>One timer callback covers both liveness backstops: a live session gone silent, and a negotiation that never attached one at all. Both run off the same clock, so one timer serves both.</summary>
		private void Sweep()
		{
			SweepInactiveSessions();
			SweepExpiredPendingPeers();
		}

		/// <summary>
		///     The backstop that actually notices a vanished client. SCTP surfaces no remote close and
		///     ICE state can sit in connected forever, but a live client is never silent, so silence
		///     past the timeout is the one signal that always arrives.
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

		/// <summary>A negotiation whose reliable channel never opened in time never gets a session, so nothing else ever disposes its <see cref="RtcPeer" />; this is the only thing that does.</summary>
		private void SweepExpiredPendingPeers()
		{
			long now = Environment.TickCount64;
			foreach (KeyValuePair<RtcPeer, long> entry in _pendingPeers)
			{
				if (now < entry.Value) continue;
				if (!_pendingPeers.TryRemove(entry.Key, out _)) continue;

				Log.Warn("NetherNet negotiation timed out before its reliable channel opened, discarding the peer");
				DisposePendingPeer(entry.Key);
			}
		}

		private static void DisposePendingPeer(RtcPeer peer)
		{
			try
			{
				peer.Dispose();
			}
			catch (Exception e)
			{
				Log.Debug("Disposing an abandoned NetherNet negotiation", e);
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
					string answer = Negotiate(networkId, body, IsLoopbackPeer(client));

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

		/// <summary>
		///     Answers an offer synchronously: <see cref="RtcPeer.AcceptOffer" /> has nothing to
		///     gather (the mux's one local candidate is already known) so there is no wait between
		///     accepting the offer and having a complete answer to sign and return.
		///     <para>
		///         Subscribes <see cref="RtcPeer.OnDataChannel" /> before calling
		///         <see cref="RtcPeer.AcceptOffer" />: the client can start opening its data channels
		///         the instant its own ICE/DTLS/SCTP stack comes up, which can race arbitrarily far
		///         ahead of this method returning the answer, let alone the caller responding with it.
		///     </para>
		/// </summary>
		private string Negotiate(string networkId, string offerSdp, bool loopbackPeer)
		{
			// The identity assertion is ours to validate or ignore by policy, but it must come out
			// before the offer is parsed either way: a=identity is not an attribute WebRTC knows,
			// and implementations reject an SDP carrying attributes they cannot parse.
			string strippedOffer = StripIdentity(offerSdp, out string assertion);
			if (assertion != null) Log.Debug($"NetherNet offer from {networkId} carries an identity assertion ({assertion.Length} chars)");

			RtcPeer peer = RtcPeer.CreateAnswerer(_mux, _certificate);
			_pendingPeers[peer] = Environment.TickCount64 + _connectingTimeout;

			// Guards session/pendingUnreliable below: OnDataChannel and OnTransportClosed can both
			// run on the mux's own receive/tick threads, concurrently with each other.
			var gate = new object();
			NetherNetSession session = null;
			RtcDataChannel pendingUnreliable = null;

			peer.OnDataChannel += channel =>
			{
				try
				{
					lock (gate)
					{
						if (channel.Label == NetherNetClientConnector.UnreliableChannelLabel)
						{
							// May arrive before or after the reliable channel attaches a session; either
							// way it is not lost, only its destination differs.
							if (session != null) session.AttachUnreliableChannel(channel);
							else pendingUnreliable = channel;
							return;
						}

						// Anything not labeled unreliable is the reliable channel, matching the two
						// labels both connectors ever open. A second one, which should never happen,
						// is ignored rather than replacing an already-attached session.
						if (session != null) return;
						if (!_pendingPeers.TryRemove(peer, out _)) return; // already expired or removed

						session = AttachSession(networkId, peer, channel, pendingUnreliable);
					}
				}
				catch (Exception e)
				{
					Log.Error($"NetherNet data channel handling failed for {networkId}", e);
				}
			};

			// A peer whose transport came up and then tore down again before its reliable channel
			// ever opened would otherwise sit in the pending table until the sweep's own timeout:
			// harmless, but pointless. OnTransportClosed never fires for a handshake that never
			// succeeded in the first place (RtcPeer's own contract), so the sweep is still what
			// catches those; this only shortens the wait for the ones that did come up.
			peer.OnTransportClosed += () =>
			{
				lock (gate)
				{
					if (session != null) return; // NetherNetSession owns teardown from here
					if (!_pendingPeers.TryRemove(peer, out _)) return;
				}

				Log.Debug($"NetherNet negotiation for {networkId} tore down before its reliable channel opened");
			};

			string answerSdp = peer.AcceptOffer(strippedOffer);

			// Required in every answer, over HTTP and HTTPS alike: a client that finds no valid
			// a=identity refuses the connection outright.
			// Rewrite the candidate before signing: the assertion covers the fingerprints, not the
			// candidate, but the client must receive one coherent answer and the address it dials
			// has to be the mapped one. Except for a loopback peer: the mux's bound port has no
			// mapping meaningful to it, and rewriting would hand it a public address it cannot dial.
			if (!loopbackPeer) answerSdp = PortMapping.Apply(answerSdp);

			return NetherNetIdentityAssertion.AddServerAssertionTo(
				answerSdp, ServerIdentity.Key, ServerIdentity.Domain, ServerIdentity.Issuer);
		}

		private NetherNetSession AttachSession(string networkId, RtcPeer peer, RtcDataChannel reliable, RtcDataChannel unreliable)
		{
			// A returning client reuses its NetworkID, so an entry here is a ghost of a dead
			// connection. Close it rather than overwrite it: overwritten, it would no longer be
			// swept and its RtcPeer would never dispose.
			if (Sessions.TryRemove(networkId, out NetherNetSession stale))
			{
				Log.Warn($"NetherNet session for {networkId} replaced by a new connection, closing the old one");
				stale.Close();
			}

			// The ICE-nominated remote endpoint has no accessor on RtcPeer today, unlike the SIPSorcery
			// path this replaces (peerConnection.AudioDestinationEndPoint); GetClientEndPoint() on this
			// session is therefore not the client's real address yet. Logged, not hidden.
			var session = new NetherNetSession(peer, reliable, unreliable, new IPEndPoint(IPAddress.Any, 0), networkId);
			session.CustomMessageHandler = CustomMessageHandlerFactory?.Invoke(session) ?? new DefaultMessageHandler();
			session.OnClosed = closed => Sessions.TryRemove(new KeyValuePair<string, NetherNetSession>(closed.NetworkId, closed));

			Sessions[networkId] = session;

			Log.Info($"NetherNet session accepted from {networkId}");
			session.CustomMessageHandler.Connected();

			return session;
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
