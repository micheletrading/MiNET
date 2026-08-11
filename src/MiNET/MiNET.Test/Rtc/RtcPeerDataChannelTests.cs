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
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     Task 7: the finished SCTP association and DCEP channel manager wired end to end into
	///     <see cref="RtcPeer" /> on top of the real DTLS/ICE transport, exercised the same two-mux
	///     loopback topology <see cref="IceSessionTests" /> already uses for an our-vs-our pair (as
	///     opposed to <see cref="InteropTests" />'s SIPSorcery-vs-us topology). Unlike the synchronous,
	///     directly-wired <see cref="SctpAssociation" /> pairs <see cref="RtcDataChannelTests" /> uses,
	///     everything here rides real loopback UDP sockets, so channel creation genuinely can, and does,
	///     race ahead of the SCTP handshake completing - exactly the case <see cref="RtcPeer.CreateDataChannel" />'s
	///     pre-establishment queue exists for.
	/// </summary>
	[TestClass]
	public class RtcPeerDataChannelTests
	{
		private static async Task<(RtcPeer Offerer, RtcPeer Answerer)> ConnectAsync(UdpMux offererMux, UdpMux answererMux)
		{
			// Not `using`: both peers outlive this helper, returned to the caller, which owns their
			// disposal. A `using var` here would tear the answerer down the instant this method
			// returns - right after WaitForTransportAsync succeeds - long before the caller gets to do
			// anything with it.
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
		///     The brief's Step 1 scenario end to end: two <see cref="RtcPeer" />s over two real loopback
		///     <see cref="UdpMux" />es, offer/answer, both NetherNet channels (reliable-ordered and
		///     unreliable-unordered) created by the client the instant the transport is ready - before the
		///     SCTP handshake this transport now kicks off has any chance to finish, since that handshake
		///     rides real (if loopback) UDP round trips rather than the synchronous wiring
		///     <see cref="RtcDataChannelTests" /> uses - both open, echo both ways, and the unreliable
		///     channel still delivers under clean loopback (nothing to lose).
		/// </summary>
		[TestMethod]
		public async Task TwoPeers_OverLoopback_CreateBothNetherNetChannels_BothOpen_EchoBothWays()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			var serverChannels = new List<RtcDataChannel>();
			var bothServerChannelsSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDataChannel += ch =>
			{
				lock (serverChannels)
				{
					serverChannels.Add(ch);
					if (serverChannels.Count >= 2) bothServerChannelsSeen.TrySetResult(true);
				}
			};

			// Fired the instant WaitForTransportAsync above resolved: the SCTP association was only just
			// constructed and Start()-ed (client role), so this races ahead of Established and exercises
			// RtcChannelManager's pre-establishment queue, not the already-established fast path
			// RtcDataChannelTests covers directly.
			RtcDataChannel clientReliable = client.CreateDataChannel("ReliableDataChannel", ordered: true, maxRetransmits: -1);
			RtcDataChannel clientUnreliable = client.CreateDataChannel("UnreliableDataChannel", ordered: false, maxRetransmits: 0);

			Assert.IsTrue(await bothServerChannelsSeen.Task.WaitAsync(TimeSpan.FromSeconds(20)), "server never saw both channels");
			Assert.IsTrue(await WaitForOpenAsync(clientReliable, TimeSpan.FromSeconds(20)), "client reliable channel never opened");
			Assert.IsTrue(await WaitForOpenAsync(clientUnreliable, TimeSpan.FromSeconds(20)), "client unreliable channel never opened");

			RtcDataChannel serverReliable = serverChannels.Single(c => c.Label == "ReliableDataChannel");
			RtcDataChannel serverUnreliable = serverChannels.Single(c => c.Label == "UnreliableDataChannel");
			Assert.IsTrue(serverReliable.IsOpen);
			Assert.IsTrue(serverUnreliable.IsOpen);

			// Reliable, client -> server.
			var serverGotReliable = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
			serverReliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsTrue(isString);
				serverGotReliable.TrySetResult(Encoding.UTF8.GetString(data.ToArray()));
			};
			clientReliable.Send(Encoding.UTF8.GetBytes("hello from client"), asString: true);
			Assert.AreEqual("hello from client", await serverGotReliable.Task.WaitAsync(TimeSpan.FromSeconds(10)));

			// Reliable, server -> client.
			var clientGotReliable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			clientReliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				clientGotReliable.TrySetResult(data.ToArray());
			};
			byte[] reply = {9, 8, 7, 6};
			serverReliable.Send(reply, asString: false);
			CollectionAssert.AreEqual(reply, await clientGotReliable.Task.WaitAsync(TimeSpan.FromSeconds(10)));

			// Unreliable, client -> server: under clean loopback (nothing to lose) it must still arrive.
			var serverGotUnreliable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			serverUnreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				serverGotUnreliable.TrySetResult(data.ToArray());
			};
			byte[] unreliablePayload = {1, 2, 3};
			clientUnreliable.Send(unreliablePayload, asString: false);
			CollectionAssert.AreEqual(unreliablePayload, await serverGotUnreliable.Task.WaitAsync(TimeSpan.FromSeconds(10)));

			// Unreliable, server -> client: same, the other direction.
			var clientGotUnreliable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			clientUnreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				clientGotUnreliable.TrySetResult(data.ToArray());
			};
			byte[] unreliableReply = {4, 5, 6};
			serverUnreliable.Send(unreliableReply, asString: false);
			CollectionAssert.AreEqual(unreliableReply, await clientGotUnreliable.Task.WaitAsync(TimeSpan.FromSeconds(10)));
		}

		/// <summary>
		///     <see cref="RtcPeer.CreateDataChannel" /> before the transport is even up (no DTLS session
		///     negotiated yet, let alone an SCTP association constructed) has nothing to queue into - fails
		///     loudly, matching the codebase's "an undiagnosable silent drop is worse than a thrown
		///     exception" stance <see cref="DtlsSession.SendApplicationData" />'s own pre-handshake guard
		///     just adopted for the same reason.
		/// </summary>
		[TestMethod]
		public void CreateDataChannel_BeforeTransportReady_Throws()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var peer = RtcPeer.CreateOfferer(mux, RtcCertificate.CreateSelfSigned());

			Assert.ThrowsExactly<InvalidOperationException>(() => peer.CreateDataChannel("TooEarly"));
		}
	}
}