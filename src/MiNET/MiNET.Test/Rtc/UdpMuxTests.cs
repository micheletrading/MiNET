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
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class UdpMuxTests
	{
		private sealed class RecordingPeer : IMuxPeer
		{
			public readonly TaskCompletionSource<StunMessage> Stun = new(TaskCreationOptions.RunContinuationsAsynchronously);
			public readonly TaskCompletionSource<byte[]> Dtls = new(TaskCreationOptions.RunContinuationsAsynchronously);
			public void OnStun(StunMessage message, ReadOnlySpan<byte> raw, IPEndPoint from) => Stun.TrySetResult(message);
			public void OnDtls(ReadOnlySpan<byte> datagram, IPEndPoint from) => Dtls.TrySetResult(datagram.ToArray());
		}

		[TestMethod]
		public async Task FirstContactStun_ResolvesByUfrag_ThenRoutesByEndpoint()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			var peer = new RecordingPeer();
			mux.RegisterUfrag("srvUfrag", _ => peer);
			mux.Start();

			using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
			var request = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = RandomNumberGenerator.GetBytes(12),
				Username = "srvUfrag:cliUfrag"
			};
			byte[] wire = new byte[StunMessage.MaxSize];
			int written = request.WriteTo(wire);
			await sender.SendAsync(wire.AsMemory(0, written), mux.LocalEndPoint);

			StunMessage seen = await peer.Stun.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.AreEqual("srvUfrag:cliUfrag", seen.Username);

			// Same endpoint now routes DTLS-looking bytes straight to the peer.
			byte[] dtlsish = {22, 0xfe, 0xfd, 1, 2, 3};
			await sender.SendAsync(dtlsish, mux.LocalEndPoint);
			byte[] dtlsSeen = await peer.Dtls.Task.WaitAsync(TimeSpan.FromSeconds(5));
			CollectionAssert.AreEqual(dtlsish, dtlsSeen);
		}

		[TestMethod]
		public async Task UnknownEndpointDtls_IsDropped()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.Start();
			using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
			await sender.SendAsync(new byte[] {22, 0xfe, 0xfd}, mux.LocalEndPoint);
			// No peer to observe; assert via the counter, polled with a timeout.
			var deadline = DateTime.UtcNow.AddSeconds(5);
			while (mux.DroppedDatagrams == 0 && DateTime.UtcNow < deadline) await Task.Delay(10);
			Assert.AreEqual(1, mux.DroppedDatagrams);
		}

		[TestMethod]
		public async Task Send_ToRegisteredPeer_DeliversUsingCachedSocketAddress()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			var peer = new RecordingPeer();
			mux.RegisterUfrag("sendUfrag", _ => peer);
			mux.Start();

			using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
			var senderEndPoint = (IPEndPoint) sender.Client.LocalEndPoint;

			var request = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = RandomNumberGenerator.GetBytes(12),
				Username = "sendUfrag:cliUfrag"
			};
			byte[] wire = new byte[StunMessage.MaxSize];
			int written = request.WriteTo(wire);
			await sender.SendAsync(wire.AsMemory(0, written), mux.LocalEndPoint);

			// First contact registers the peer, which also populates the send-address cache.
			await peer.Stun.Task.WaitAsync(TimeSpan.FromSeconds(5));

			byte[] payload = {9, 8, 7, 6};
			mux.Send(senderEndPoint, payload);

			UdpReceiveResult result = await sender.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
			CollectionAssert.AreEqual(payload, result.Buffer);
		}

		[TestMethod]
		public async Task Tick_Fires()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			var ticked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			mux.OnTick += () => ticked.TrySetResult(true);
			mux.Start();
			await ticked.Task.WaitAsync(TimeSpan.FromSeconds(5));
		}

		private sealed class ThrowOnceThenRecordPeer : IMuxPeer
		{
			private int _calls;
			public readonly TaskCompletionSource<StunMessage> Stun = new(TaskCreationOptions.RunContinuationsAsynchronously);

			public void OnStun(StunMessage message, ReadOnlySpan<byte> raw, IPEndPoint from)
			{
				if (Interlocked.Increment(ref _calls) == 1) throw new InvalidOperationException("Simulated peer callback failure");
				Stun.TrySetResult(message);
			}

			public void OnDtls(ReadOnlySpan<byte> datagram, IPEndPoint from)
			{
			}
		}

		[TestMethod]
		public async Task PeerCallbackException_DoesNotKillTheReceiveLoop()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			var peer = new ThrowOnceThenRecordPeer();
			mux.RegisterUfrag("throwUfrag", _ => peer);
			mux.Start();

			using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
			byte[] wire = new byte[StunMessage.MaxSize];

			StunMessage MakeRequest() => new()
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = RandomNumberGenerator.GetBytes(12),
				Username = "throwUfrag:cliUfrag"
			};

			// First datagram: first-contact registers the peer, then its OnStun throws.
			int written = MakeRequest().WriteTo(wire);
			await sender.SendAsync(wire.AsMemory(0, written), mux.LocalEndPoint);

			// Second datagram, same endpoint: proves the receive loop survived the throw above,
			// since it still routes to the now-registered peer and completes normally.
			written = MakeRequest().WriteTo(wire);
			await sender.SendAsync(wire.AsMemory(0, written), mux.LocalEndPoint);

			StunMessage seen = await peer.Stun.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.AreEqual("throwUfrag:cliUfrag", seen.Username);
			Assert.AreEqual(1, mux.DispatchFailures);
		}

		/// <summary>
		///     Stage 3 Task 1's brief scenario (c): first contact registers an endpoint pre-integrity (no
		///     MESSAGE-INTEGRITY check happens until the resolved <see cref="IMuxPeer" /> itself gets a
		///     chance to look at the binding request), so a flood that knows a live ufrag but spoofs many
		///     distinct source endpoints must not grow the peer table without limit. Each of
		///     <see cref="UdpMux.MaxEndpointsPerUfrag" /> plus a few more distinct <see cref="UdpClient" />s
		///     (a distinct local port each, the practical stand-in for a distinct source endpoint a unit
		///     test can actually produce) sends one binding request for the same ufrag; the resolver -
		///     invoked only for an admitted endpoint - must plateau at the cap, and every request beyond it
		///     must land on <see cref="UdpMux.AdmissionCapDrops" /> instead.
		/// </summary>
		[TestMethod]
		public async Task FirstContactStun_BeyondPerUfragCap_DropsAndCountsWithoutGrowingThePeerTable()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			long admittedCount = 0;
			mux.RegisterUfrag("capUfrag", _ =>
			{
				Interlocked.Increment(ref admittedCount);
				return new RecordingPeer();
			});
			mux.Start();

			const int overflow = 5;
			int attempts = UdpMux.MaxEndpointsPerUfrag + overflow;
			var senders = new List<UdpClient>();
			try
			{
				for (int i = 0; i < attempts; i++)
				{
					var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
					senders.Add(sender);

					var request = new StunMessage
					{
						Type = StunMessageType.BindingRequest,
						TransactionId = RandomNumberGenerator.GetBytes(12),
						Username = "capUfrag:cliUfrag"
					};
					byte[] wire = new byte[StunMessage.MaxSize];
					int written = request.WriteTo(wire);
					await sender.SendAsync(wire.AsMemory(0, written), mux.LocalEndPoint);
				}

				var deadline = DateTime.UtcNow.AddSeconds(5);
				while (mux.AdmissionCapDrops < overflow && DateTime.UtcNow < deadline) await Task.Delay(10);

				Assert.AreEqual(UdpMux.MaxEndpointsPerUfrag, Interlocked.Read(ref admittedCount), "the resolver must not be invoked beyond the per-ufrag cap");
				Assert.AreEqual(overflow, mux.AdmissionCapDrops);
			}
			finally
			{
				foreach (UdpClient sender in senders) sender.Dispose();
			}
		}

		/// <summary>
		///     A peer advertises candidates in every family it holds, and vanilla holds IPv6. Sending
		///     to one this socket cannot address raises WSAEAFNOSUPPORT, and every caller of Send is a
		///     tick or a send path where that exception takes healthy work down with it: an ICE tick
		///     stops checking the candidates behind it, an SCTP retransmit is lost. So it drops.
		/// </summary>
		[TestMethod]
		public void Send_ToAFamilyThisSocketCannotAddress_DropsInsteadOfThrowing()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.Start();

			Assert.IsFalse(mux.CanSendTo(IPAddress.IPv6Loopback), "a v4 socket cannot address a v6 peer");

			mux.Send(new IPEndPoint(IPAddress.IPv6Loopback, 19132), new byte[] {1, 2, 3});

			Assert.AreEqual(1, mux.UnreachableFamilyDrops);
		}
	}
}