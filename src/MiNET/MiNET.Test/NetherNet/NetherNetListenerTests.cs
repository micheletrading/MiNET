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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net;
using MiNET.Net.NetherNet;
using MiNET.Net.Rtc;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     <see cref="NetherNetListener" /> answering real signaling over loopback TCP, the counterpart
	///     to <see cref="Rtc.InteropTests" />: this is the same cross-stack proof (a real SIPSorcery
	///     peer against our stack) but exercised through the listener's own HTTP round trip rather than
	///     calling <see cref="RtcPeer.AcceptOffer" /> directly, so it also proves the pending-peer
	///     table, the port-mapping skip for loopback, and <see cref="NetherNetListener.AttachSession" />
	///     wiring in their real housing. SIPSorcery is allowed here only, as in InteropTests.
	/// </summary>
	[TestClass]
	public class NetherNetListenerTests
	{
		private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
		{
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline)
			{
				if (condition()) return true;
				await Task.Delay(20);
			}

			return condition();
		}

		private static Task<bool> WaitForOpenAsync(RtcDataChannel channel, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			channel.OnOpen += () => tcs.TrySetResult(true);
			if (channel.IsOpen) tcs.TrySetResult(true);
			return tcs.Task.WaitAsync(timeout);
		}

		private static Task<bool> WaitForSipSorceryOpenAsync(SIPSorcery.Net.RTCDataChannel channel, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			channel.onopen += () => tcs.TrySetResult(true);
			if (channel.IsOpened) tcs.TrySetResult(true);
			return tcs.Task.WaitAsync(timeout);
		}

		/// <summary>Starts a listener on loopback with an OS-chosen TCP port, for a test to dial without racing another test's port.</summary>
		private static NetherNetListener StartListener(int? connectingTimeout = null)
		{
			var identity = new NetherNetServerIdentity(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "identity.pem"));
			var listener = new NetherNetListener(new IPEndPoint(IPAddress.Loopback, 0), identity, portMapping: null, connectingTimeout: connectingTimeout);
			listener.Start();
			return listener;
		}

		private static HttpClient NewHttpClient() => new HttpClient {Timeout = TimeSpan.FromSeconds(15)};

		private static async Task<string> ProbeAndNegotiateAsync(HttpClient http, int port, string networkId, string offerSdp)
		{
			HttpResponseMessage probe = await http.GetAsync($"http://127.0.0.1:{port}/v1/join");
			Assert.IsTrue(probe.IsSuccessStatusCode, "GET /v1/join must answer with a 2xx capability probe");

			var content = new StringContent(offerSdp);
			content.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");
			HttpResponseMessage response = await http.PostAsync($"http://127.0.0.1:{port}/v1/join/{networkId}", content);
			Assert.IsTrue(response.IsSuccessStatusCode, $"POST /v1/join/{networkId} must accept the offer");

			return await response.Content.ReadAsStringAsync();
		}

		/// <summary>Records every call this session's <see cref="ICustomMessageHandler" /> seam makes, standing in for <see cref="BedrockMessageHandler" /> without any of its batching/compression.</summary>
		private class RecordingMessageHandler : ICustomMessageHandler
		{
			public readonly List<byte[]> ReceivedPayloads = new();
			public volatile bool ConnectedCalled;

			public void Connected() => ConnectedCalled = true;

			public void Disconnect(string reason, bool sendDisconnect = true)
			{
			}

			public void HandlePacket(Packet message)
			{
				if (message is McpeWrapper wrapper)
				{
					lock (ReceivedPayloads) ReceivedPayloads.Add(wrapper.payload.ToArray());
				}

				message.PutPool();
			}

			public Packet HandleOrderedSend(Packet packet) => packet;

			public List<Packet> PrepareSend(List<Packet> packetsToSend) => packetsToSend;
		}

		/// <summary>
		///     Step 1's centerpiece: a real SIPSorcery client dials the listener exactly the way a
		///     NetherNet client does (probe, offer, POST, apply answer), with no code here reaching into
		///     <see cref="RtcPeer" /> directly. Proves the whole signaling-to-attach path: the HTTP round
		///     trip, <see cref="NetherNetListener.StripIdentity" />, synchronous
		///     <see cref="RtcPeer.AcceptOffer" />, the identity assertion added to the answer, and
		///     <see cref="NetherNetListener.AttachSession" /> reaching a factory-made handler's
		///     <see cref="ICustomMessageHandler.Connected" />.
		/// </summary>
		[TestMethod]
		public async Task SipSorceryClient_NegotiatesThroughRealSignaling_SessionAttaches()
		{
			NetherNetListener listener = StartListener();
			try
			{
				RecordingMessageHandler handler = null;
				listener.CustomMessageHandlerFactory = session =>
				{
					handler = new RecordingMessageHandler();
					return handler;
				};

				var client = new SIPSorcery.Net.RTCPeerConnection(new SIPSorcery.Net.RTCConfiguration());
				SIPSorcery.Net.RTCDataChannel clientReliable = await client.createDataChannel("ReliableDataChannel");
				SIPSorcery.Net.RTCDataChannel clientUnreliable = await client.createDataChannel("UnreliableDataChannel",
					new SIPSorcery.Net.RTCDataChannelInit {ordered = false, maxRetransmits = 0});

				var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				client.onconnectionstatechange += state =>
				{
					if (state == SIPSorcery.Net.RTCPeerConnectionState.connected) connected.TrySetResult(true);
					if (state == SIPSorcery.Net.RTCPeerConnectionState.failed) connected.TrySetException(new Exception("SIPSorcery reported failed"));
				};

				var offer = client.createOffer();
				await client.setLocalDescription(offer);

				using HttpClient http = NewHttpClient();
				string answerSdp = await ProbeAndNegotiateAsync(http, listener.LocalEndPoint.Port, "1", client.localDescription.sdp.ToString());

				var result = client.setRemoteDescription(new SIPSorcery.Net.RTCSessionDescriptionInit
				{
					type = SIPSorcery.Net.RTCSdpType.answer,
					sdp = answerSdp
				});
				Assert.AreEqual(SIPSorcery.Net.SetDescriptionResultEnum.OK, result, "SIPSorcery rejected our answer SDP");

				Assert.IsTrue(await connected.Task.WaitAsync(TimeSpan.FromSeconds(20)), "SIPSorcery never reached connected");
				Assert.IsTrue(await WaitForSipSorceryOpenAsync(clientReliable, TimeSpan.FromSeconds(20)), "SIPSorcery's reliable channel never reached onopen");
				Assert.IsTrue(await WaitForSipSorceryOpenAsync(clientUnreliable, TimeSpan.FromSeconds(20)), "SIPSorcery's unreliable channel never reached onopen");

				Assert.IsTrue(await WaitUntilAsync(() => handler != null, TimeSpan.FromSeconds(20)), "no session ever attached");
				Assert.IsTrue(await WaitUntilAsync(() => handler.ConnectedCalled, TimeSpan.FromSeconds(20)), "AttachSession never called Connected() on the factory-made handler");
				Assert.AreEqual(1, listener.Sessions.Count);
				Assert.AreEqual(0, listener.PendingPeerCount, "the peer must leave the pending table once its session attaches");

				client.close();
			}
			finally
			{
				listener.Stop();
			}
		}

		/// <summary>
		///     Step 1's scenario (b): a peer that negotiates an answer but never completes ICE/DTLS (the
		///     offerer here never calls <see cref="RtcPeer.AcceptAnswer" />, so it never starts ICE
		///     checks and the listener's answerer never nominates) must not sit in the pending table
		///     forever. The connecting deadline is configured short so the sweep's own 2.5s tick is the
		///     only real wait this test pays for.
		/// </summary>
		[TestMethod]
		public async Task NeverNominatedPeer_IsSweptFromThePendingTable()
		{
			NetherNetListener listener = StartListener(connectingTimeout: 200);
			try
			{
				using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
				using var offerer = RtcPeer.CreateOfferer(offererMux, RtcCertificate.CreateSelfSigned());
				string offerSdp = offerer.CreateOffer();

				using HttpClient http = NewHttpClient();
				await ProbeAndNegotiateAsync(http, listener.LocalEndPoint.Port, "2", offerSdp);

				Assert.AreEqual(1, listener.PendingPeerCount, "the negotiated peer must be pending right after the offer/answer round trip");

				Assert.IsTrue(await WaitUntilAsync(() => listener.PendingPeerCount == 0, TimeSpan.FromSeconds(10)),
					"a peer that never nominates must be swept from the pending table once its connecting deadline passes");
				Assert.AreEqual(0, listener.Sessions.Count, "a never-nominated peer must never attach a session");
			}
			finally
			{
				listener.Stop();
			}
		}

		/// <summary>
		///     Step 1's scenario (c): the unreliable channel is free to open after the session has
		///     already attached on the reliable one (the 18/25 AttachSession race this must not
		///     reintroduce - see the plan's own remarks), and a message sent on it afterward must still
		///     reach the session, proving <see cref="NetherNetSession.AttachUnreliableChannel" /> wired
		///     it rather than the message being silently dropped.
		/// </summary>
		[TestMethod]
		public async Task UnreliableChannelAfterAttach_ReachesTheSession()
		{
			const int maxSegmentBytes = 262144;

			NetherNetListener listener = StartListener();
			try
			{
				RecordingMessageHandler handler = null;
				listener.CustomMessageHandlerFactory = session =>
				{
					handler = new RecordingMessageHandler();
					return handler;
				};

				using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
				offererMux.Start();
				using var offerer = RtcPeer.CreateOfferer(offererMux, RtcCertificate.CreateSelfSigned());

				string offerSdp = offerer.CreateOffer();

				using HttpClient http = NewHttpClient();
				string answerSdp = await ProbeAndNegotiateAsync(http, listener.LocalEndPoint.Port, "3", offerSdp);

				offerer.AcceptAnswer(answerSdp);

				// Only the reliable channel is opened here, so the session attaches with no unreliable
				// channel yet, exactly the case AttachUnreliableChannel exists for. CreateDataChannel is
				// valid once AcceptAnswer has built the association, even before it reaches Established:
				// the DATA_CHANNEL_OPEN is queued and sent once it does.
				RtcDataChannel clientReliable = offerer.CreateDataChannel("ReliableDataChannel", ordered: true);

				Assert.IsTrue(await offerer.WaitForTransportAsync(TimeSpan.FromSeconds(20)), "offerer transport never completed");
				Assert.IsTrue(await WaitForOpenAsync(clientReliable, TimeSpan.FromSeconds(20)), "client reliable channel never opened");
				Assert.IsTrue(await WaitUntilAsync(() => handler != null, TimeSpan.FromSeconds(20)), "no session ever attached on the reliable channel alone");
				Assert.IsTrue(await WaitUntilAsync(() => handler.ConnectedCalled, TimeSpan.FromSeconds(20)));

				// Now open the unreliable channel, strictly after attach.
				RtcDataChannel clientUnreliable = offerer.CreateDataChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);
				Assert.IsTrue(await WaitForOpenAsync(clientUnreliable, TimeSpan.FromSeconds(20)), "client unreliable channel never opened");

				byte[] payload = new byte[64];
				new Random(11).NextBytes(payload);
				NetherNetSegments.ForEachSegment(payload, maxSegmentBytes, clientUnreliable,
					static (channel, buffer, length) => channel.Send(buffer.AsSpan(0, length), asString: false));

				Assert.IsTrue(await WaitUntilAsync(() => { lock (handler.ReceivedPayloads) return handler.ReceivedPayloads.Count > 0; }, TimeSpan.FromSeconds(10)),
					"a message sent on the unreliable channel after attach never reached the session, AttachUnreliableChannel was not wired");

				byte[] received;
				lock (handler.ReceivedPayloads) received = handler.ReceivedPayloads[0];
				CollectionAssert.AreEqual(payload, received);
			}
			finally
			{
				listener.Stop();
			}
		}

		/// <summary>
		///     Both ends ours, end to end: <see cref="NetherNetClient" /> dials the listener
		///     through real signaling and real loopback UDP, and a Bedrock-shaped payload crosses from
		///     the connected client session to the server session's handler. This is the full
		///     integration path a ServiceKiller bot takes, in one process.
		/// </summary>
		[TestMethod]
		public async Task OwnConnector_DialsOwnListener_PayloadCrossesEndToEnd()
		{
			NetherNetListener listener = StartListener();
			NetherNetClient client = null;
			try
			{
				RecordingMessageHandler handler = null;
				listener.CustomMessageHandlerFactory = session =>
				{
					handler = new RecordingMessageHandler();
					return handler;
				};

				client = await NetherNetClient.ConnectAsync("127.0.0.1", listener.LocalEndPoint.Port);
				NetherNetSession clientSession = client.Session;

				// SendPacket routes through the handler's PrepareSend seam and no-ops without one,
				// exactly like a real client, which always wires its Bedrock handler before sending.
				clientSession.CustomMessageHandler = new RecordingMessageHandler();

				Assert.IsTrue(await WaitUntilAsync(() => handler != null && handler.ConnectedCalled, TimeSpan.FromSeconds(10)),
					"the server session never attached and called Connected() on the factory-made handler");

				byte[] payload = new byte[512];
				new Random(12).NextBytes(payload);
				McpeWrapper wrapper = McpeWrapper.CreateObject();
				wrapper.payload = payload;
				clientSession.SendPacket(wrapper);

				Assert.IsTrue(await WaitUntilAsync(() => { lock (handler.ReceivedPayloads) return handler.ReceivedPayloads.Count > 0; }, TimeSpan.FromSeconds(10)),
					"the client's payload never reached the server session's handler");

				byte[] received;
				lock (handler.ReceivedPayloads) received = handler.ReceivedPayloads[0];
				CollectionAssert.AreEqual(payload, received);
			}
			finally
			{
				client?.Dispose();
				listener.Stop();
			}
		}
	}
}
