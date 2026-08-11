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
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     Both interop directions extend past DTLS <c>connected</c>
	///     into full DCEP negotiation and data-channel exchange against a real SIPSorcery peer (the
	///     oracle earlier, DTLS-only tests never had time to disagree with, since those tests closed
	///     within half a second of connecting). SIPSorcery is allowed here only: production code under
	///     <see cref="MiNET.Net.Rtc" /> and its comments never name it.
	/// </summary>
	[TestClass]
	public class InteropTests
	{
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

		/// <summary>
		///     Subscribes to exactly the next delivery on <paramref name="channel" /> and unsubscribes
		///     itself immediately, so callers can await one message per <c>Send</c> without a prior
		///     expectation's handler staying attached to misfire (and, for <c>Assert</c> inside a handler,
		///     get silently swallowed by <see cref="RtcDataChannel.DeliverData" />'s own subscriber-exception
		///     guard) on every later delivery for the rest of the test. Must be called - and therefore
		///     subscribed - before whatever triggers the send, exactly like every other TaskCompletionSource
		///     wait in this file and in <see cref="RtcPeerDataChannelTests" />.
		/// </summary>
		private static Task<(byte[] Data, bool IsString)> ReceiveNextAsync(RtcDataChannel channel, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<(byte[], bool)>(TaskCreationOptions.RunContinuationsAsynchronously);
			ChannelMessageHandler handler = null;
			handler = (in ReadOnlySequence<byte> data, bool isString) =>
			{
				channel.OnMessage -= handler;
				tcs.TrySetResult((data.ToArray(), isString));
			};
			channel.OnMessage += handler;
			return tcs.Task.WaitAsync(timeout);
		}

		/// <summary>The SIPSorcery-side equivalent of <see cref="ReceiveNextAsync(RtcDataChannel,TimeSpan)" />, same self-unsubscribing shape, same ordering requirement.</summary>
		private static Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> ReceiveNextAsync(SIPSorcery.Net.RTCDataChannel channel, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<(byte[], SIPSorcery.Net.DataChannelPayloadProtocols)>(TaskCreationOptions.RunContinuationsAsynchronously);
			SIPSorcery.Net.OnDataChannelMessageDelegate handler = null;
			handler = (dc, protocol, data) =>
			{
				channel.onmessage -= handler;
				tcs.TrySetResult((data, protocol));
			};
			channel.onmessage += handler;
			return tcs.Task.WaitAsync(timeout);
		}

		/// <summary>
		///     No event on either side reports "the peer's SCTP association tore down cleanly":
		///     <see cref="RtcPeer" /> never forwards <see cref="SctpAssociation.OnAborted" /> anywhere, so
		///     a post-close teardown check has nothing to await and has to poll instead.
		///     Bounded by <paramref name="timeout" />, short interval, not this test's primary
		///     synchronization mechanism (that is still the TaskCompletionSource waits everywhere else in
		///     this file) - only a way to observe a state transition nothing else exposes as an event.
		/// </summary>
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

		private static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

		// Exit criterion 1: SIPSorcery dials us. Their client is the offerer, exactly like a NetherNet client.
		[TestMethod]
		public async Task SipSorceryClient_Connects_ToOurServer()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.Start();
			using var ourServer = RtcPeer.CreateAnswerer(mux, RtcCertificate.CreateSelfSigned());
			ourServer.RemapCandidatesForSameMachine = true; // no-op here (AcceptOffer never adds remote candidates); set for symmetry with the offerer direction below.

			var theirClient = new SIPSorcery.Net.RTCPeerConnection(new SIPSorcery.Net.RTCConfiguration());

			SIPSorcery.Net.RTCDataChannel theirReliable = await theirClient.createDataChannel("ReliableDataChannel");
			SIPSorcery.Net.RTCDataChannel theirUnreliable = await theirClient.createDataChannel("UnreliableDataChannel",
				new SIPSorcery.Net.RTCDataChannelInit {ordered = false, maxRetransmits = 0});

			var ourChannelsByLabel = new Dictionary<string, RtcDataChannel>();
			var bothOursSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			ourServer.OnDataChannel += channel =>
			{
				lock (ourChannelsByLabel)
				{
					ourChannelsByLabel[channel.Label] = channel;
					if (ourChannelsByLabel.Count >= 2) bothOursSeen.TrySetResult(true);
				}
			};

			var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			theirClient.onconnectionstatechange += state =>
			{
				if (state == SIPSorcery.Net.RTCPeerConnectionState.connected) connected.TrySetResult(true);
				if (state == SIPSorcery.Net.RTCPeerConnectionState.failed) connected.TrySetException(new Exception("SIPSorcery reported failed"));
			};

			var offer = theirClient.createOffer();
			await theirClient.setLocalDescription(offer);

			string answerSdp = ourServer.AcceptOffer(theirClient.localDescription.sdp.ToString());
			var result = theirClient.setRemoteDescription(new SIPSorcery.Net.RTCSessionDescriptionInit
			{
				type = SIPSorcery.Net.RTCSdpType.answer,
				sdp = answerSdp
			});
			Assert.AreEqual(SIPSorcery.Net.SetDescriptionResultEnum.OK, result);

			Assert.IsTrue(await connected.Task.WaitAsync(TimeSpan.FromSeconds(20)), "SIPSorcery never reached connected");
			Assert.IsTrue(await ourServer.WaitForTransportAsync(TimeSpan.FromSeconds(20)), "our transport never completed");

			Assert.IsTrue(await WaitForSipSorceryOpenAsync(theirReliable, TimeSpan.FromSeconds(20)), "SIPSorcery's reliable channel never reached onopen");
			Assert.IsTrue(await WaitForSipSorceryOpenAsync(theirUnreliable, TimeSpan.FromSeconds(20)), "SIPSorcery's unreliable channel never reached onopen");
			Assert.IsTrue(await bothOursSeen.Task.WaitAsync(TimeSpan.FromSeconds(20)), "our server never saw both data channels");

			RtcDataChannel ourReliable = ourChannelsByLabel["ReliableDataChannel"];
			RtcDataChannel ourUnreliable = ourChannelsByLabel["UnreliableDataChannel"];
			Assert.IsTrue(ourReliable.IsOpen);
			Assert.IsTrue(ourUnreliable.IsOpen);

			// Reliable, string, SIPSorcery -> us.
			Task<(byte[] Data, bool IsString)> weGotString = ReceiveNextAsync(ourReliable, TimeSpan.FromSeconds(10));
			theirReliable.send("hello from SIPSorcery");
			(byte[] Data, bool IsString) weString = await weGotString;
			Assert.IsTrue(weString.IsString);
			Assert.AreEqual("hello from SIPSorcery", Encoding.UTF8.GetString(weString.Data));

			// Reliable, string, us -> SIPSorcery.
			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotString = ReceiveNextAsync(theirReliable, TimeSpan.FromSeconds(10));
			ourReliable.Send(Utf8("hello from MiNET"), asString: true);
			(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol) theyString = await theyGotString;
			Assert.AreEqual(SIPSorcery.Net.DataChannelPayloadProtocols.WebRTC_String, theyString.Protocol);
			Assert.AreEqual("hello from MiNET", Encoding.UTF8.GetString(theyString.Data));

			// Reliable, binary, SIPSorcery -> us.
			Task<(byte[] Data, bool IsString)> weGotBinary = ReceiveNextAsync(ourReliable, TimeSpan.FromSeconds(10));
			byte[] binaryToUs = {1, 2, 3, 4};
			theirReliable.send(binaryToUs, 0, binaryToUs.Length);
			(byte[] Data, bool IsString) weBinary = await weGotBinary;
			Assert.IsFalse(weBinary.IsString);
			CollectionAssert.AreEqual(binaryToUs, weBinary.Data);

			// Reliable, binary, us -> SIPSorcery.
			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotBinary = ReceiveNextAsync(theirReliable, TimeSpan.FromSeconds(10));
			byte[] binaryFromUs = {9, 8, 7};
			ourReliable.Send(binaryFromUs, asString: false);
			(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol) theyBinary = await theyGotBinary;
			Assert.AreEqual(SIPSorcery.Net.DataChannelPayloadProtocols.WebRTC_Binary, theyBinary.Protocol);
			CollectionAssert.AreEqual(binaryFromUs, theyBinary.Data);

			// Unreliable, binary both ways: clean loopback has nothing to lose, so it must still arrive,
			// matching RtcPeerDataChannelTests's own our-vs-our coverage of the same channel shape.
			Task<(byte[] Data, bool IsString)> weGotUnreliable = ReceiveNextAsync(ourUnreliable, TimeSpan.FromSeconds(10));
			byte[] unreliableToUs = {5, 6};
			theirUnreliable.send(unreliableToUs, 0, unreliableToUs.Length);
			CollectionAssert.AreEqual(unreliableToUs, (await weGotUnreliable).Data);

			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotUnreliable = ReceiveNextAsync(theirUnreliable, TimeSpan.FromSeconds(10));
			byte[] unreliableFromUs = {3, 4};
			ourUnreliable.Send(unreliableFromUs, asString: false);
			CollectionAssert.AreEqual(unreliableFromUs, (await theyGotUnreliable).Data);

			// Idle the association for a few seconds, then prove it is still alive with one more round
			// trip - not a heartbeat round trip itself. SIPSorcery 10.0.13 never
			// originates an SCTP HEARTBEAT (confirmed by reading its source: RTCSctpTransport/SctpAssociation
			// wire nothing to a timer, and SctpTransport.RequestHeartbeat/ChangeHeartbeat are unused stub
			// primitives), so there is no heartbeat traffic for this oracle to exercise on our
			// HandleHeartbeat echo path in either role; this idle-then-roundtrip is the closest honest
			// substitute, proving the association does not silently die sitting idle.
			await Task.Delay(TimeSpan.FromSeconds(3));
			Task<(byte[] Data, bool IsString)> weGotAfterIdle = ReceiveNextAsync(ourReliable, TimeSpan.FromSeconds(10));
			byte[] afterIdle = {42};
			theirReliable.send(afterIdle, 0, afterIdle.Length);
			CollectionAssert.AreEqual(afterIdle, (await weGotAfterIdle).Data);

			// SIPSorcery's close() on an already-connected transport sends a DTLS close_notify ahead of
			// its own SCTP-level graceful shutdown finishing over the same channel. RFC 5246 7.2.1
			// requires us to end the DTLS connection in both directions immediately on receiving it, which
			// means never processing whatever SIPSorcery sends afterward on this transport - including its
			// SCTP SHUTDOWN-COMPLETE, so this side's SctpAssociation does not, and is not expected to,
			// reach SctpState.Aborted here; DTLS closure is the terminal event this proves, not an
			// SCTP-level handshake. Bounded by the same WaitUntilAsync timeout pattern used for
			// SIPSorcery's other quirks elsewhere in this file, in case dropping its final chunk leaves
			// something in its own close() path waiting.
			theirClient.close();
			Assert.IsTrue(await WaitUntilAsync(() => ourServer.DtlsSessionClosed, TimeSpan.FromSeconds(10)),
				"our DTLS session never closed after SIPSorcery's close_notify");

			// SctpState.Aborted is no longer reachable once DTLS has closed: the SHUTDOWN-COMPLETE that
			// would drive it there is dropped along with everything else the instant FeedDatagram's own
			// _closed guard fires, which also means nothing SIPSorcery sends into the void after close()
			// can ever move AssociationIgnoredPacketCount - an assertion resting on that alone could never
			// fail for the reason it names. Driving the association directly with a packet carrying the
			// wrong verification tag proves its own drop-and-count path still works, independently of
			// whatever DTLS does or does not deliver to it.
			AssertAssociationDropsAndCountsAWrongTagPacket(ourServer);
		}

		// Exit criterion 2: we dial SIPSorcery. Our client is the offerer.
		[TestMethod]
		public async Task OurClient_Connects_ToSipSorceryServer()
		{
			var theirServer = new SIPSorcery.Net.RTCPeerConnection(new SIPSorcery.Net.RTCConfiguration());
			var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			theirServer.onconnectionstatechange += state =>
			{
				if (state == SIPSorcery.Net.RTCPeerConnectionState.connected) connected.TrySetResult(true);
				if (state == SIPSorcery.Net.RTCPeerConnectionState.failed) connected.TrySetException(new Exception("SIPSorcery reported failed"));
			};

			var theirChannelsByLabel = new Dictionary<string, SIPSorcery.Net.RTCDataChannel>();
			var bothTheirsSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			theirServer.ondatachannel += channel =>
			{
				lock (theirChannelsByLabel)
				{
					theirChannelsByLabel[channel.label] = channel;
					if (theirChannelsByLabel.Count >= 2) bothTheirsSeen.TrySetResult(true);
				}
			};

			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.Start();
			using var ourClient = RtcPeer.CreateOfferer(mux, RtcCertificate.CreateSelfSigned());
			ourClient.RemapCandidatesForSameMachine = true;

			string offerSdp = ourClient.CreateOffer();
			var result = theirServer.setRemoteDescription(new SIPSorcery.Net.RTCSessionDescriptionInit
			{
				type = SIPSorcery.Net.RTCSdpType.offer,
				sdp = offerSdp
			});
			Assert.AreEqual(SIPSorcery.Net.SetDescriptionResultEnum.OK, result, "SIPSorcery rejected our offer SDP");

			var answer = theirServer.createAnswer();
			await theirServer.setLocalDescription(answer);
			ourClient.AcceptAnswer(theirServer.localDescription.sdp.ToString());

			Assert.IsTrue(await ourClient.WaitForTransportAsync(TimeSpan.FromSeconds(20)), "our transport never completed");
			Assert.IsTrue(await connected.Task.WaitAsync(TimeSpan.FromSeconds(20)), "SIPSorcery never reached connected");

			RtcDataChannel ourReliable = ourClient.CreateDataChannel("ReliableDataChannel", ordered: true, maxRetransmits: -1);
			RtcDataChannel ourUnreliable = ourClient.CreateDataChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);

			Assert.IsTrue(await WaitForOpenAsync(ourReliable, TimeSpan.FromSeconds(20)), "our reliable channel never opened");
			Assert.IsTrue(await WaitForOpenAsync(ourUnreliable, TimeSpan.FromSeconds(20)), "our unreliable channel never opened");
			Assert.IsTrue(await bothTheirsSeen.Task.WaitAsync(TimeSpan.FromSeconds(20)), "SIPSorcery never saw both data channels");

			SIPSorcery.Net.RTCDataChannel theirReliable = theirChannelsByLabel["ReliableDataChannel"];
			SIPSorcery.Net.RTCDataChannel theirUnreliable = theirChannelsByLabel["UnreliableDataChannel"];
			Assert.IsTrue(theirReliable.IsOpened);
			Assert.IsTrue(theirUnreliable.IsOpened);

			// Reliable, string, us -> SIPSorcery.
			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotString = ReceiveNextAsync(theirReliable, TimeSpan.FromSeconds(10));
			ourReliable.Send(Utf8("hello from MiNET"), asString: true);
			(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol) theyString = await theyGotString;
			Assert.AreEqual(SIPSorcery.Net.DataChannelPayloadProtocols.WebRTC_String, theyString.Protocol);
			Assert.AreEqual("hello from MiNET", Encoding.UTF8.GetString(theyString.Data));

			// Reliable, string, SIPSorcery -> us.
			Task<(byte[] Data, bool IsString)> weGotString = ReceiveNextAsync(ourReliable, TimeSpan.FromSeconds(10));
			theirReliable.send("hello from SIPSorcery");
			(byte[] Data, bool IsString) weString = await weGotString;
			Assert.IsTrue(weString.IsString);
			Assert.AreEqual("hello from SIPSorcery", Encoding.UTF8.GetString(weString.Data));

			// Reliable, binary, us -> SIPSorcery.
			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotBinary = ReceiveNextAsync(theirReliable, TimeSpan.FromSeconds(10));
			byte[] binaryFromUs = {9, 8, 7};
			ourReliable.Send(binaryFromUs, asString: false);
			(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol) theyBinary = await theyGotBinary;
			Assert.AreEqual(SIPSorcery.Net.DataChannelPayloadProtocols.WebRTC_Binary, theyBinary.Protocol);
			CollectionAssert.AreEqual(binaryFromUs, theyBinary.Data);

			// Reliable, binary, SIPSorcery -> us.
			Task<(byte[] Data, bool IsString)> weGotBinary = ReceiveNextAsync(ourReliable, TimeSpan.FromSeconds(10));
			byte[] binaryToUs = {1, 2, 3, 4};
			theirReliable.send(binaryToUs, 0, binaryToUs.Length);
			(byte[] Data, bool IsString) weBinary = await weGotBinary;
			Assert.IsFalse(weBinary.IsString);
			CollectionAssert.AreEqual(binaryToUs, weBinary.Data);

			// Unreliable, binary both ways.
			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotUnreliable = ReceiveNextAsync(theirUnreliable, TimeSpan.FromSeconds(10));
			byte[] unreliableFromUs = {3, 4};
			ourUnreliable.Send(unreliableFromUs, asString: false);
			CollectionAssert.AreEqual(unreliableFromUs, (await theyGotUnreliable).Data);

			Task<(byte[] Data, bool IsString)> weGotUnreliable = ReceiveNextAsync(ourUnreliable, TimeSpan.FromSeconds(10));
			byte[] unreliableToUs = {5, 6};
			theirUnreliable.send(unreliableToUs, 0, unreliableToUs.Length);
			CollectionAssert.AreEqual(unreliableToUs, (await weGotUnreliable).Data);

			// See the identical comment in the other direction's test - SIPSorcery never originates a
			// heartbeat in either role, so this idle-then-roundtrip is liveness evidence, not a heartbeat
			// round trip.
			await Task.Delay(TimeSpan.FromSeconds(3));
			Task<(byte[] Data, SIPSorcery.Net.DataChannelPayloadProtocols Protocol)> theyGotAfterIdle = ReceiveNextAsync(theirReliable, TimeSpan.FromSeconds(10));
			byte[] afterIdle = {42};
			ourReliable.Send(afterIdle, asString: false);
			CollectionAssert.AreEqual(afterIdle, (await theyGotAfterIdle).Data);

			// See the identical comment in the other direction's test.
			theirServer.close();
			Assert.IsTrue(await WaitUntilAsync(() => ourClient.DtlsSessionClosed, TimeSpan.FromSeconds(10)),
				"our DTLS session never closed after SIPSorcery's close_notify");

			AssertAssociationDropsAndCountsAWrongTagPacket(ourClient);
		}

		/// <summary>
		///     Drives <paramref name="peer" />'s underlying association directly with a packet carrying
		///     the wrong verification tag - a real drop-and-count case per
		///     <see cref="SctpAssociation.OnPacketReceived" />'s own remarks - and asserts
		///     <see cref="SctpAssociation.IgnoredPacketCount" /> moves by exactly one. Exists because
		///     <see cref="DtlsSession.FeedDatagram" />'s own <c>_closed</c> early return means nothing a
		///     peer sends after DTLS closure can ever reach the association, so
		///     <see cref="RtcPeer.AssociationIgnoredPacketCount" /> alone can never fail for "the
		///     association ignores post-close traffic": this proves the association's own counting path
		///     independently of whatever DTLS does or does not deliver to it.
		/// </summary>
		private static void AssertAssociationDropsAndCountsAWrongTagPacket(RtcPeer peer)
		{
			SctpAssociation association = peer.Association;
			long before = association.IgnoredPacketCount;

			uint wrongTag = unchecked(association.LocalVerificationTag + 1);
			byte[] packet = new byte[12];
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, wrongTag);
			SctpPacket.FinishChecksum(packet.AsSpan(0, n));

			association.OnPacketReceived(packet.AsMemory(0, n));

			Assert.AreEqual(before + 1, association.IgnoredPacketCount, "expected a packet carrying the wrong verification tag to be dropped and counted");
		}
	}
}
