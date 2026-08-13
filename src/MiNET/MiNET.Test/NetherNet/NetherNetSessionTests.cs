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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net;
using MiNET.Net.NetherNet;
using MiNET.Net.Rtc;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     <see cref="NetherNetSession" /> wired onto a real, connected <see cref="RtcPeer" /> pair over
	///     loopback UDP, the same two-mux topology <see cref="Rtc.RtcPeerDataChannelTests" /> uses. No
	///     SIPSorcery anywhere here: both sides are the in-house Rtc stack, matching how a real
	///     NetherNet listener/connector pair will hand a session its already-negotiated peer and
	///     channels once Tasks 3 and 4 rebuild them.
	/// </summary>
	[TestClass]
	public class NetherNetSessionTests
	{
		private const int MaxSegmentBytes = 262144;

		private static async Task<(RtcPeer Offerer, RtcPeer Answerer)> ConnectAsync(UdpMux offererMux, UdpMux answererMux)
		{
			var answerer = RtcPeer.CreateAnswerer(answererMux, RtcCertificate.CreateSelfSigned());
			var offerer = RtcPeer.CreateOfferer(offererMux, RtcCertificate.CreateSelfSigned());

			string offerSdp = offerer.CreateOffer();
			string answerSdp = answerer.AcceptOffer(offerSdp);
			offerer.AcceptAnswer(answerSdp);

			Assert.IsTrue(await offerer.WaitForTransportAsync(TimeSpan.FromSeconds(20)), "offerer transport never completed");
			Assert.IsTrue(await answerer.WaitForTransportAsync(TimeSpan.FromSeconds(20)), "answerer transport never completed");

			return (offerer, answerer);
		}

		private static Task<bool> WaitForOpenAsync(RtcDataChannel channel, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			channel.OnOpen += () => tcs.TrySetResult(true);
			if (channel.IsOpen) tcs.TrySetResult(true);
			return tcs.Task.WaitAsync(timeout);
		}

		/// <summary>
		///     Client creates both NetherNet channels the instant the transport is ready, mirroring
		///     <see cref="Rtc.RtcPeerDataChannelTests" />, and waits for both to open on both ends before
		///     handing them back, exactly what a real session attach point needs.
		/// </summary>
		private static async Task<(RtcDataChannel ClientReliable, RtcDataChannel ClientUnreliable, RtcDataChannel ServerReliable, RtcDataChannel ServerUnreliable)> OpenChannelPairAsync(RtcPeer client, RtcPeer server)
		{
			var serverChannels = new List<RtcDataChannel>();
			var bothSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDataChannel += ch =>
			{
				lock (serverChannels)
				{
					serverChannels.Add(ch);
					if (serverChannels.Count >= 2) bothSeen.TrySetResult(true);
				}
			};

			RtcDataChannel clientReliable = client.CreateDataChannel("ReliableDataChannel", ordered: true, maxRetransmits: -1);
			RtcDataChannel clientUnreliable = client.CreateDataChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);

			Assert.IsTrue(await bothSeen.Task.WaitAsync(TimeSpan.FromSeconds(20)), "server never saw both channels");
			Assert.IsTrue(await WaitForOpenAsync(clientReliable, TimeSpan.FromSeconds(20)), "client reliable channel never opened");
			Assert.IsTrue(await WaitForOpenAsync(clientUnreliable, TimeSpan.FromSeconds(20)), "client unreliable channel never opened");

			RtcDataChannel serverReliable = serverChannels.Single(c => c.Label == "ReliableDataChannel");
			RtcDataChannel serverUnreliable = serverChannels.Single(c => c.Label == "UnreliableDataChannel");
			Assert.IsTrue(serverReliable.IsOpen);
			Assert.IsTrue(serverUnreliable.IsOpen);

			return (clientReliable, clientUnreliable, serverReliable, serverUnreliable);
		}

		/// <summary>Polls rather than waiting on one signal, since more than one of these tests needs
		/// to observe a count settle rather than a single event fire.</summary>
		private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
		{
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline)
			{
				if (condition()) return true;
				await Task.Delay(10);
			}

			return condition();
		}

		/// <summary>Records every call this session's <see cref="ICustomMessageHandler" /> seam makes,
		/// standing in for <see cref="BedrockMessageHandler" /> without any of its batching/compression.</summary>
		private class RecordingMessageHandler : ICustomMessageHandler
		{
			public readonly List<byte[]> ReceivedPayloads = new();
			public readonly List<(string Reason, bool SendDisconnect)> DisconnectCalls = new();

			public void Connected()
			{
			}

			public void Disconnect(string reason, bool sendDisconnect = true)
			{
				lock (DisconnectCalls) DisconnectCalls.Add((reason, sendDisconnect));
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
		///     Scenario (a): several raw segments arriving on the reliable channel, sent the way a real
		///     peer would (not through <see cref="NetherNetSession.SendPacket" />, so this isolates the
		///     receiving session's own reassembly and <c>HandlePayload</c> from its own send path),
		///     reassemble into exactly one <see cref="McpeWrapper" /> delivered to the recorded handler,
		///     byte-identical to the original payload.
		/// </summary>
		[TestMethod]
		public async Task InboundSegmentedMessage_ReassemblesIntoOneMcpeWrapper()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			(RtcDataChannel clientReliable, RtcDataChannel clientUnreliable, RtcDataChannel serverReliable, RtcDataChannel serverUnreliable) = await OpenChannelPairAsync(client, server);

			var serverSession = new NetherNetSession(server, serverReliable, serverUnreliable, new IPEndPoint(IPAddress.Loopback, 0), "1001");
			var handler = new RecordingMessageHandler();
			serverSession.CustomMessageHandler = handler;

			byte[] payload = new byte[300000];
			new Random(42).NextBytes(payload);

			NetherNetSegments.ForEachSegment(payload, MaxSegmentBytes, clientReliable,
				static (channel, buffer, length) => channel.Send(buffer.AsSpan(0, length), asString: false));

			Assert.IsTrue(await WaitUntilAsync(() => { lock (handler.ReceivedPayloads) return handler.ReceivedPayloads.Count > 0; }, TimeSpan.FromSeconds(10)),
				"server session never delivered a reassembled McpeWrapper");

			await Task.Delay(TimeSpan.FromMilliseconds(200));

			byte[][] received;
			lock (handler.ReceivedPayloads) received = handler.ReceivedPayloads.ToArray();

			Assert.AreEqual(1, received.Length, "the segmented message must reassemble into exactly one delivered wrapper");
			CollectionAssert.AreEqual(payload, received[0]);
		}

		/// <summary>
		///     Scenario (b): a payload well over <see cref="NetherNetSession" />'s own
		///     <c>MaxSegmentBytes</c> sent through <see cref="NetherNetSession.SendPacket" /> leaves the
		///     reliable channel as more than one wire message, and a standalone
		///     <see cref="NetherNetSegmentReassembler" /> on the receiving end (no session involved on
		///     that side, to isolate the sender's own segmenting) reconstitutes it byte-identical to the
		///     original.
		/// </summary>
		[TestMethod]
		public async Task OutboundOversizedPayload_LeavesMultipleSegments_ReassemblesByteIdentical()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			(RtcDataChannel clientReliable, RtcDataChannel clientUnreliable, RtcDataChannel serverReliable, RtcDataChannel serverUnreliable) = await OpenChannelPairAsync(client, server);

			var clientSession = new NetherNetSession(client, clientReliable, clientUnreliable, new IPEndPoint(IPAddress.Loopback, 0), "1002");
			clientSession.CustomMessageHandler = new RecordingMessageHandler();

			byte[] payload = new byte[300000];
			new Random(7).NextBytes(payload);

			var rawSegments = new List<byte[]>();
			serverReliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				lock (rawSegments) rawSegments.Add(data.ToArray());
			};

			McpeWrapper wrapper = McpeWrapper.CreateObject();
			wrapper.payload = payload;
			clientSession.SendPacket(wrapper);

			Assert.IsTrue(await WaitUntilAsync(() => { lock (rawSegments) return rawSegments.Count >= 2; }, TimeSpan.FromSeconds(10)),
				"an over-MaxSegmentBytes payload must leave the wire as more than one segment");

			byte[][] segments;
			lock (rawSegments) segments = rawSegments.ToArray();

			var reassembler = new NetherNetSegmentReassembler();
			byte[] reconstructed = null;
			foreach (byte[] segment in segments)
			{
				if (reassembler.TryAccept(segment, out ReadOnlyMemory<byte> message))
				{
					reconstructed = message.ToArray();
					break;
				}
			}

			Assert.IsNotNull(reconstructed, "the segments never reassembled into a complete message");
			CollectionAssert.AreEqual(payload, reconstructed);
		}

		/// <summary>
		///     Scenario (c): the server aborting its SCTP association is a real ABORT chunk on the wire,
		///     which surfaces on the client's <see cref="RtcPeer.OnTransportClosed" /> and must tear the
		///     client-side session down through exactly one <see cref="ICustomMessageHandler.Disconnect" />
		///     call, not a repeated one, matching <see cref="RtcPeer" />'s own once-only contract.
		/// </summary>
		[TestMethod]
		public async Task PeerAbort_TearsSessionDown_DisconnectCalledExactlyOnce()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			(RtcDataChannel clientReliable, RtcDataChannel clientUnreliable, RtcDataChannel serverReliable, RtcDataChannel serverUnreliable) = await OpenChannelPairAsync(client, server);

			var clientSession = new NetherNetSession(client, clientReliable, clientUnreliable, new IPEndPoint(IPAddress.Loopback, 0), "1003");
			var handler = new RecordingMessageHandler();
			clientSession.CustomMessageHandler = handler;

			// A real ABORT chunk to the client, exactly as RtcPeerDataChannelTests' own abort scenario
			// sends it (Association is test-visible via InternalsVisibleTo).
			server.Association.Abort();

			Assert.IsTrue(await WaitUntilAsync(() => { lock (handler.DisconnectCalls) return handler.DisconnectCalls.Count > 0; }, TimeSpan.FromSeconds(10)),
				"session never disconnected after the peer's ABORT");

			// A settle window: proves the guard in Close()/RtcPeer's own once-only event lets no
			// second, delayed delivery re-fire Disconnect.
			await Task.Delay(TimeSpan.FromMilliseconds(200));

			lock (handler.DisconnectCalls) Assert.AreEqual(1, handler.DisconnectCalls.Count);
			Assert.AreEqual("Connection closed", handler.DisconnectCalls[0].Reason);
			Assert.IsFalse(handler.DisconnectCalls[0].SendDisconnect);
			Assert.IsTrue(clientSession.IsClosed);
		}
	}
}
