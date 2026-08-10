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
	public class IceSessionTests
	{
		[TestMethod]
		public async Task LiteServer_And_ControllingClient_Nominate_EachOther()
		{
			using var serverMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var clientMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));

			string serverUfrag = IceSession.NewUfrag(), serverPwd = IceSession.NewPassword();
			string clientUfrag = IceSession.NewUfrag(), clientPwd = IceSession.NewPassword();

			var server = new IceSession(serverMux, IceRole.ControlledLite, serverUfrag, serverPwd);
			server.SetRemoteCredentials(clientUfrag, clientPwd);
			serverMux.RegisterUfrag(serverUfrag, _ => server);

			var client = new IceSession(clientMux, IceRole.Controlling, clientUfrag, clientPwd);
			client.SetRemoteCredentials(serverUfrag, serverPwd);
			clientMux.RegisterUfrag(clientUfrag, _ => client);

			var serverNominated = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
			var clientNominated = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnNominated += ep => serverNominated.TrySetResult(ep);
			client.OnNominated += ep => clientNominated.TrySetResult(ep);

			serverMux.Start();
			clientMux.Start();
			client.AddRemoteCandidate(serverMux.LocalEndPoint);
			client.StartChecks();

			IPEndPoint clientSawServer = await clientNominated.Task.WaitAsync(TimeSpan.FromSeconds(10));
			IPEndPoint serverSawClient = await serverNominated.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.AreEqual(serverMux.LocalEndPoint.Port, clientSawServer.Port);
			Assert.AreEqual(clientMux.LocalEndPoint.Port, serverSawClient.Port);
		}

		[TestMethod]
		public async Task WrongPassword_IsIgnored()
		{
			using var serverMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));
			using var clientMux = new UdpMux(new IPEndPoint(IPAddress.Loopback, 0));

			var server = new IceSession(serverMux, IceRole.ControlledLite, "sfrag", IceSession.NewPassword());
			server.SetRemoteCredentials("cfrag", IceSession.NewPassword());
			serverMux.RegisterUfrag("sfrag", _ => server);

			var client = new IceSession(clientMux, IceRole.Controlling, "cfrag", IceSession.NewPassword());
			client.SetRemoteCredentials("sfrag", IceSession.NewPassword()); // wrong: not the server's real password
			clientMux.RegisterUfrag("cfrag", _ => client);

			var nominated = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnNominated += ep => nominated.TrySetResult(ep);
			serverMux.Start();
			clientMux.Start();
			client.AddRemoteCandidate(serverMux.LocalEndPoint);
			client.StartChecks();

			await Assert.ThrowsExactlyAsync<TimeoutException>(() => nominated.Task.WaitAsync(TimeSpan.FromSeconds(3)));
		}
	}
}