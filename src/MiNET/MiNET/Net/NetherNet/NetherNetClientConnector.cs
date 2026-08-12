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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using SIPSorcery.Net;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     Dials a NetherNet server, which means being the WebRTC offerer. The client owns the two
	///     data channels and the offer; the server only answers. Signaling is a single HTTP round
	///     trip, so there is no socket to keep open and no trickle: ICE gathering has to finish
	///     before the offer is worth sending, because every candidate we will ever have must be
	///     inside it.
	/// </summary>
	public static class NetherNetClientConnector
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetherNetClientConnector));

		public const string ReliableChannelLabel = "ReliableDataChannel";
		public const string UnreliableChannelLabel = "UnreliableDataChannel";

		/// <summary>
		///     Connects to a NetherNet server and returns a session with both data channels open.
		/// </summary>
		/// <param name="host">Host running the signaling endpoint, which is the BDS server-port.</param>
		/// <param name="port">The signaling port. Under NetherNet, server-port is TCP, not UDP.</param>
		/// <param name="networkId">Our own NetworkID. Opaque to the server; generated when omitted.</param>
		public static async Task<NetherNetSession> ConnectAsync(string host, int port, string networkId = null, CancellationToken cancellationToken = default,
			XboxIdentity identity = null, string issuerDomain = "authorization.franchise.minecraft-services.net")
		{
			// Same ICE patience as the listener sets, for the same reason: a loaded emulator
			// process misses STUN checks without being dead. Process-wide statics; setting them
			// here covers client-only processes (ServiceKiller, MiNET.Client).
			RtpIceChannel.DISCONNECTED_TIMEOUT_PERIOD = Config.GetProperty("NetherNet.IceDisconnectedTimeout", 60);
			RtpIceChannel.FAILED_TIMEOUT_PERIOD = Config.GetProperty("NetherNet.IceFailedTimeout", 120);

			networkId ??= NewNetworkId();

			string baseUrl = $"http://{host}:{port}";
			using var http = new HttpClient {Timeout = TimeSpan.FromSeconds(15)};

			// The client checks for the endpoint before it spends anything on WebRTC. A non-2xx here
			// means the server is not speaking NetherNet, which on BDS means transport=raknet.
			HttpResponseMessage capability = await http.GetAsync($"{baseUrl}/v1/join", cancellationToken);
			if (!capability.IsSuccessStatusCode) throw new IOException($"{host}:{port} does not accept NetherNet connections (GET /v1/join returned {(int) capability.StatusCode})");

			// No STUN and no TURN: only host candidates are gathered, which is what the real client
			// does. A server reachable at the address we already dialled needs nothing more.
			var peerConnection = new RTCPeerConnection(new RTCConfiguration
			{
				iceServers = null
			});

			try
			{
				RTCDataChannel reliable = await peerConnection.createDataChannel(ReliableChannelLabel);
				RTCDataChannel unreliable = await peerConnection.createDataChannel(UnreliableChannelLabel, new RTCDataChannelInit
				{
					ordered = false,
					maxRetransmits = 0
				});

				var gatheringComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				peerConnection.onicegatheringstatechange += state =>
				{
					if (state == RTCIceGatheringState.complete) gatheringComplete.TrySetResult(true);
				};

				var channelOpen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				reliable.onopen += () => channelOpen.TrySetResult(true);

				RTCSessionDescriptionInit offer = peerConnection.createOffer();
				await peerConnection.setLocalDescription(offer);

				// Full ICE. Trickle is disabled for this signaling protocol, so an offer sent before
				// gathering finishes would carry a candidate set the server can never complete.
				await WithCancellation(gatheringComplete.Task, cancellationToken);

				string offerSdp = peerConnection.localDescription.sdp.ToString();

				// The assertion has to be added after the local description is set, because it signs
				// the DTLS fingerprints, and those do not exist until the offer is generated.
				offerSdp = NetherNetIdentityAssertion.AddTo(offerSdp, identity, issuerDomain);

				if (Log.IsDebugEnabled) Log.Debug($"NetherNet offer for {networkId}:\n{offerSdp}");

				var content = new StringContent(offerSdp);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");

				HttpResponseMessage response = await http.PostAsync($"{baseUrl}/v1/join/{networkId}", content, cancellationToken);
				if (!response.IsSuccessStatusCode) throw new IOException($"NetherNet signaling rejected the offer with {(int) response.StatusCode} {response.ReasonPhrase}");

				string answerSdp = await response.Content.ReadAsStringAsync(cancellationToken);
				if (Log.IsDebugEnabled) Log.Debug($"NetherNet answer for {networkId}:\n{answerSdp}");

				SetDescriptionResultEnum result = peerConnection.setRemoteDescription(new RTCSessionDescriptionInit
				{
					type = RTCSdpType.answer,
					sdp = answerSdp
				});

				if (result != SetDescriptionResultEnum.OK) throw new IOException($"NetherNet answer rejected: {result}");

				// ICE checks, DTLS and SCTP all happen here, and the channel opening is the only
				// signal that the whole stack came up.
				await WithCancellation(channelOpen.Task, cancellationToken);

				IPEndPoint remote = ResolveRemote(peerConnection, host, port);
				Log.Info($"NetherNet connected to {remote} as network id {networkId}");

				// NetherNetSession now takes an RtcPeer/RtcDataChannel pair (the in-house Rtc stack),
				// and this connector still negotiates over SIPSorcery's RTCPeerConnection/RTCDataChannel.
				// The connector's own rebuild onto RtcPeer is a separate, later change; until then it
				// cannot hand NetherNetSession the objects its constructor now requires.
				throw new NotSupportedException("NetherNetClientConnector still negotiates over SIPSorcery and cannot construct a NetherNetSession until it is rebuilt onto RtcPeer.");
			}
			catch
			{
				peerConnection.close();
				throw;
			}
		}

		/// <summary>
		///     The NetworkID is documented as opaque and currently a 64 bit unsigned integer rendered
		///     as decimal, so it is generated in that shape without depending on it meaning anything.
		/// </summary>
		public static string NewNetworkId()
		{
			Span<byte> bytes = stackalloc byte[8];
			RandomNumberGenerator.Fill(bytes);
			return BitConverter.ToUInt64(bytes).ToString();
		}

		/// <summary>
		///     The gameplay path is the ICE-nominated pair, not the address we dialled for signaling,
		///     because signaling is TCP on server-port and gameplay is UDP on a separate port.
		/// </summary>
		private static IPEndPoint ResolveRemote(RTCPeerConnection peerConnection, string host, int port)
		{
			try
			{
				IPEndPoint nominated = peerConnection.AudioDestinationEndPoint ?? peerConnection.VideoDestinationEndPoint;
				if (nominated != null) return nominated;
			}
			catch (Exception e)
			{
				Log.Debug("Could not read the nominated ICE endpoint", e);
			}

			return IPEndPoint.TryParse($"{host}:{port}", out IPEndPoint fallback) ? fallback : new IPEndPoint(IPAddress.Any, port);
		}

		private static async Task WithCancellation(Task task, CancellationToken cancellationToken)
		{
			var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			await using (cancellationToken.Register(() => cancelled.TrySetCanceled()))
			{
				await await Task.WhenAny(task, cancelled.Task);
			}
		}
	}
}
