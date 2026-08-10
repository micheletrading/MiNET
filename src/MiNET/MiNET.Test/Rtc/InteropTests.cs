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
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class InteropTests
	{
		// Exit criterion 1: SIPSorcery dials us. Their client is the offerer, exactly like a NetherNet client.
		[TestMethod]
		public async Task SipSorceryClient_Connects_ToOurServer()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.Start();
			using var ourServer = RtcPeer.CreateAnswerer(mux, RtcCertificate.CreateSelfSigned());

			var theirClient = new SIPSorcery.Net.RTCPeerConnection(new SIPSorcery.Net.RTCConfiguration());
			await theirClient.createDataChannel("ReliableDataChannel");

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
			theirClient.close();
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

			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.Start();
			using var ourClient = RtcPeer.CreateOfferer(mux, RtcCertificate.CreateSelfSigned());

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
			theirServer.close();
		}
	}
}