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
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET;
using MiNET.Net.NetherNet;
using MiNET.Net.RakNet;
using MiNET.Net.Rtc;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     Server-list discovery is the one RakNet exchange that survives RakNet's retirement, so
	///     what matters is that a NetherNet-only mux really answers the legacy ping with a pong the
	///     legacy parser accepts, and that nothing else sneaks through the offline seam. The
	///     round trip runs the real client half against the real mux hook over a real socket.
	/// </summary>
	[TestClass]
	public class NetherNetDiscoveryTests
	{
		private static NetherNetDiscovery MakeDiscovery(out MotdProvider motd)
		{
			motd = new MotdProvider();
			var connectionInfo = new ConnectionInfo(new ConcurrentDictionary<IPEndPoint, RakSession>())
			{
				MaxNumberOfPlayers = 10
			};
			return new NetherNetDiscovery(motd, connectionInfo, () => 7);
		}

		[TestMethod]
		public async Task UnconnectedPing_AgainstTheMux_ReturnsParsableMotd()
		{
			NetherNetDiscovery discovery = MakeDiscovery(out MotdProvider motd);

			using var mux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			mux.OfflineResponder = discovery.HandleOffline;
			mux.Start();

			string motdString = await NetherNetDiscovery.PingAsync("127.0.0.1", mux.LocalEndPoint.Port, TimeSpan.FromSeconds(10));

			Assert.IsNotNull(motdString, "the mux must answer a legacy ping when a responder is attached");
			StringAssert.Contains(motdString, $";{motd.Motd};");
			StringAssert.Contains(motdString, ";7;10;"); // live player count from the transport, max from config
		}

		[TestMethod]
		public void HandleOffline_IgnoresGarbage()
		{
			NetherNetDiscovery discovery = MakeDiscovery(out _);

			var garbage = new byte[64];
			new Random(1234).NextBytes(garbage);
			garbage[0] = 0x01; // right id, wrong magic

			Assert.IsNull(discovery.HandleOffline(garbage, new IPEndPoint(IPAddress.Loopback, 12345)));
			Assert.IsNull(discovery.HandleOffline(new byte[4], new IPEndPoint(IPAddress.Loopback, 12345)));
		}

		[TestMethod]
		public void LooksLikeUnconnectedPing_AcceptsOnlyTheRealShape()
		{
			byte[] ping = NetherNetDiscovery.BuildPing();
			Assert.IsTrue(NetherNetDiscovery.LooksLikeUnconnectedPing(ping));

			byte[] stunLike = new byte[64]; // leading 0x00, no RakNet magic
			Assert.IsFalse(NetherNetDiscovery.LooksLikeUnconnectedPing(stunLike));
		}
	}
}
