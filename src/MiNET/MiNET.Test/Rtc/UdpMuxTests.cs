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
using System.Net.Sockets;
using System.Security.Cryptography;
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
		public async Task Tick_Fires()
		{
			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			var ticked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			mux.OnTick += () => ticked.TrySetResult(true);
			mux.Start();
			await ticked.Task.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}
}