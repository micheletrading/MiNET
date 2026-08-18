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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     The finished SCTP association and DCEP channel manager wired end to end into
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
			Assert.AreEqual("hello from client", await serverGotReliable.Task.WaitAsync(TimeSpan.FromSeconds(30)));

			// Reliable, server -> client.
			var clientGotReliable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			clientReliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				clientGotReliable.TrySetResult(data.ToArray());
			};
			byte[] reply = {9, 8, 7, 6};
			serverReliable.Send(reply, asString: false);
			CollectionAssert.AreEqual(reply, await clientGotReliable.Task.WaitAsync(TimeSpan.FromSeconds(30)));

			// Unreliable, client -> server: under clean loopback (nothing to lose) it must still arrive.
			var serverGotUnreliable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			serverUnreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				serverGotUnreliable.TrySetResult(data.ToArray());
			};
			byte[] unreliablePayload = {1, 2, 3};
			clientUnreliable.Send(unreliablePayload, asString: false);
			CollectionAssert.AreEqual(unreliablePayload, await serverGotUnreliable.Task.WaitAsync(TimeSpan.FromSeconds(30)));

			// Unreliable, server -> client: same, the other direction.
			var clientGotUnreliable = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			clientUnreliable.OnMessage += (in ReadOnlySequence<byte> data, bool isString) =>
			{
				Assert.IsFalse(isString);
				clientGotUnreliable.TrySetResult(data.ToArray());
			};
			byte[] unreliableReply = {4, 5, 6};
			serverUnreliable.Send(unreliableReply, asString: false);
			CollectionAssert.AreEqual(unreliableReply, await clientGotUnreliable.Task.WaitAsync(TimeSpan.FromSeconds(30)));
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

		/// <summary>
		///     <see cref="RtcPeer.SendApplicationData" /> is public, and its own size ceiling is only ever
		///     enforced one layer down, in <see cref="DtlsSession.SendApplicationData" />: this proves the
		///     public path actually reaches that guard and fails clearly - naming the limit in the message,
		///     never a corrupted record - rather than silently truncating or throwing something opaque.
		///     Every internal <see cref="SctpAssociation" /> send path already builds into an
		///     <see cref="SctpPacket.MaxSize" /> buffer, so this call shape (a payload larger than that)
		///     never happens from inside this codebase; only a misbehaving caller of the public API reaches it.
		/// </summary>
		[TestMethod]
		public async Task SendApplicationData_PayloadLargerThanTheSctpPacketCeiling_ThrowsNamingTheLimit()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			byte[] oversized = new byte[DtlsSession.MaxSendPayloadLength + 1];
			ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => client.SendApplicationData(oversized));
			StringAssert.Contains(ex.Message, DtlsSession.MaxSendPayloadLength.ToString());
		}

		/// <summary>Same shape as <see cref="SctpTeardownTests" />'s own private helper: hand-builds one raw, checksummed SCTP packet carrying a single chunk, addressed by the RECEIVING association's own verification tag (RFC 4960 8.5: what a real peer would have been told to address it by).</summary>
		private static byte[] BuildRawChunkPacket(uint verificationTag, byte chunkType, ReadOnlySpan<byte> value)
		{
			byte[] packetArray = new byte[SctpPacket.MaxSize];
			Span<byte> packet = packetArray;
			int n = SctpPacket.WriteHeader(packet, 5000, 5000, verificationTag);
			value.CopyTo(packet.Slice(n + 4));
			n += SctpChunkCodec.FinishChunk(packet.Slice(n), chunkType, 0, value.Length);
			SctpPacket.FinishChecksum(packet.Slice(0, n));
			return packetArray.AsSpan(0, n).ToArray();
		}

		private static byte[] BuildShutdownPacket(uint verificationTag) => BuildRawChunkPacket(verificationTag, 7 /* SHUTDOWN */, ReadOnlySpan<byte>.Empty);

		/// <summary>
		///     Stage 3 Task 1's brief scenario (a): once a real peer pair is up, one side aborting its
		///     own association - a real ABORT chunk on the wire, not a local <see cref="RtcPeer.Dispose" />
		///     - must reach the OTHER side's association as an inbound ABORT and surface there as
		///     <see cref="RtcPeer.OnTransportClosed" />, exactly once. The aborting side's own
		///     <see cref="RtcPeer.OnTransportClosed" /> fires too (<see cref="SctpAssociation.OnAborted" />
		///     fires locally for a self-initiated <see cref="SctpAssociation.Abort" /> just as it does for
		///     an inbound one - see that event's own remarks), asserted here as well since both are
		///     legitimate and this is the signal <c>NetherNetSession</c> keys teardown off on either end.
		/// </summary>
		[TestMethod]
		public async Task AssociationAbort_SurfacesOnBothPeers_OnTransportClosed_ExactlyOnce()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			int clientClosedCount = 0;
			int serverClosedCount = 0;
			var clientClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var serverClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			client.OnTransportClosed += () =>
			{
				Interlocked.Increment(ref clientClosedCount);
				clientClosed.TrySetResult(true);
			};
			server.OnTransportClosed += () =>
			{
				Interlocked.Increment(ref serverClosedCount);
				serverClosed.TrySetResult(true);
			};

			// Sends a real ABORT chunk to the client over the wire; also fires OnAborted locally on the
			// server's own association (SctpAssociation.Abort's own remarks).
			server.Association.Abort();

			Assert.IsTrue(await clientClosed.Task.WaitAsync(TimeSpan.FromSeconds(30)), "client's OnTransportClosed never fired after the server aborted");
			Assert.IsTrue(await serverClosed.Task.WaitAsync(TimeSpan.FromSeconds(30)), "server's own OnTransportClosed never fired for its own local Abort()");

			// A settle window: proves neither side's guard lets a second, delayed delivery re-fire.
			await Task.Delay(TimeSpan.FromMilliseconds(200));
			Assert.AreEqual(1, clientClosedCount);
			Assert.AreEqual(1, serverClosedCount);
		}

		/// <summary>
		///     Stage 3 Task 1's brief scenario (b): the same wiring, for a clean SHUTDOWN instead of an
		///     ABORT. <see cref="SctpAssociation" /> never initiates a graceful shutdown itself (only ever
		///     answers one), so this hand-builds a real inbound SHUTDOWN chunk - the same technique
		///     <see cref="SctpTeardownTests" /> uses at the association level - and feeds it directly to
		///     the server's association, simulating what a real peer-initiated graceful close puts on the
		///     wire. A SHUTDOWN tears the association down exactly like an inbound ABORT does, so this
		///     proves <see cref="RtcPeer" />'s subscription covers that path too, not only an ABORT chunk.
		/// </summary>
		[TestMethod]
		public async Task AssociationReceivesShutdown_OnTransportClosed_FiresOnce()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			int serverClosedCount = 0;
			var serverClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnTransportClosed += () =>
			{
				Interlocked.Increment(ref serverClosedCount);
				serverClosed.TrySetResult(true);
			};

			// ConnectAsync returns once the CLIENT side is usable; the server's own association reaches
			// Established a moment later, and a SHUTDOWN that arrives before it does is dropped as an
			// out-of-state packet (RFC 4960 8.5) rather than tearing anything down. That is the race
			// this test lost only in full-suite runs: the failure reported state Closed before the
			// SHUTDOWN and one extra ignored packet after it. Waiting on the actual precondition, not
			// on a duration.
			var established = Stopwatch.StartNew();
			while (server.AssociationState != SctpState.Established && established.ElapsedMilliseconds < 10000)
			{
				await Task.Delay(10);
			}

			Assert.AreEqual(SctpState.Established, server.AssociationState, "server association never reached Established after ConnectAsync");

			SctpState? stateBeforeShutdown = server.AssociationState;
			long ignoredBeforeShutdown = server.AssociationIgnoredPacketCount;

			byte[] shutdownPacket = BuildShutdownPacket(server.Association.LocalVerificationTag);
			server.Association.OnPacketReceived(shutdownPacket);

			// The forward chain is synchronous on this thread (OnPacketReceived -> Teardown ->
			// OnAborted -> RaiseTransportClosed), so a timeout here means the SHUTDOWN was dropped or
			// the once-only raise guard was consumed before this test subscribed. This test has timed
			// out in full-suite runs without ever reproducing solo, so on timeout it reports the
			// state that discriminates those causes instead of a bare TimeoutException.
			try
			{
				await serverClosed.Task.WaitAsync(TimeSpan.FromSeconds(30));
			}
			catch (TimeoutException)
			{
				Assert.Fail(
					"server's OnTransportClosed never fired after receiving SHUTDOWN. " +
					$"State before SHUTDOWN: {stateBeforeShutdown}, after: {server.AssociationState}; " +
					$"association ignored-packet count before: {ignoredBeforeShutdown}, after: {server.AssociationIgnoredPacketCount}.");
			}

			Assert.AreEqual(SctpState.Aborted, server.AssociationState);

			await Task.Delay(TimeSpan.FromMilliseconds(200));
			Assert.AreEqual(1, serverClosedCount);
		}

		/// <summary>
		///     The fourth <see cref="RtcPeer.OnTransportClosed" /> source in isolation: an inbound DTLS
		///     close_notify with NO SCTP teardown alongside it. Disposing one side's
		///     <see cref="DtlsSession" /> directly (not the <see cref="RtcPeer" />, whose Dispose would
		///     also put an SCTP ABORT on the wire first) sends only the close_notify, so the peer's
		///     association stays <see cref="SctpState.Established" /> while its DTLS session closes -
		///     proving the tick-poll of <see cref="DtlsSession.IsClosed" /> is what raised the event,
		///     not the association forwarder.
		/// </summary>
		[TestMethod]
		public async Task DtlsCloseNotifyAlone_PeerRaisesOnTransportClosed_AssociationStillEstablished()
		{
			using var offererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var answererMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			offererMux.Start();
			answererMux.Start();

			(RtcPeer client, RtcPeer server) = await ConnectAsync(offererMux, answererMux);
			using var clientDisposable = client;
			using var serverDisposable = server;

			// ConnectAsync gates only on DTLS; the SCTP handshake rides real loopback UDP after it.
			// This test's whole premise is "the DTLS closes while the association is Established and
			// untouched", so the handshake must actually have finished before the close_notify goes
			// out - disposing earlier just races it, and the assertion below would then measure the
			// race, not the poll path it exists to prove.
			for (int i = 0; i < 200 && (client.AssociationState != SctpState.Established || server.AssociationState != SctpState.Established); i++)
			{
				await Task.Delay(10);
			}
			Assert.AreEqual(SctpState.Established, server.AssociationState, "the SCTP handshake never completed after the transport came up");

			int serverClosedCount = 0;
			var serverClosed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnTransportClosed += () =>
			{
				Interlocked.Increment(ref serverClosedCount);
				serverClosed.TrySetResult(true);
			};

			// Sends close_notify to the server and nothing else: the client's association is bypassed,
			// so no ABORT chunk ever goes on the wire.
			client.Dtls.Dispose();

			Assert.IsTrue(await serverClosed.Task.WaitAsync(TimeSpan.FromSeconds(30)), "server's OnTransportClosed never fired after the client's close_notify");
			Assert.IsTrue(server.DtlsSessionClosed, "the server's DTLS session should have closed on the inbound close_notify");
			Assert.AreEqual(SctpState.Established, server.AssociationState, "the association must be untouched; only the DTLS poll may have raised the event");

			await Task.Delay(TimeSpan.FromMilliseconds(200));
			Assert.AreEqual(1, serverClosedCount);
		}
	}
}