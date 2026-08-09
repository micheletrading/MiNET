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

using log4net;
using MiNET.Net;
using MiNET.Net.RakNet;

namespace MiNET.Tunnel
{
	/// <summary>
	///     The upstream half of the tunnel. Only the login/crypto packets are handled locally
	///     (network settings negotiate compression and trigger the offline login, the handshake
	///     starts encryption). Everything the upstream server sends after that is forwarded to the
	///     real client verbatim and dumped.
	/// </summary>
	public class TunnelUpstreamHandler : BedrockClientMessageHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TunnelUpstreamHandler));

		private readonly TunnelPlayer _downstream;
		private bool _readySignalled;

		public TunnelUpstreamHandler(INetworkHandler session, IMcpeClientMessageHandler handler, TunnelPlayer downstream) : base(session, handler)
		{
			_downstream = downstream;
		}

		public override void HandleCustomPacket(Packet message)
		{
			switch (message)
			{
				case McpeNetworkSettings:
					// Compression on, then the offline login goes out. Local only.
					base.HandleCustomPacket(message);
					return;

				case McpeServerToClientHandshake:
					// Starts encryption and sends ClientToServerHandshake upstream. After this the
					// upstream session is live, so start forwarding the client's held frames.
					base.HandleCustomPacket(message);
					SignalReady();
					return;

				case McpeDisconnect disconnect:
					Log.Warn($"Tunnel: upstream disconnected: {disconnect.message}");
					_downstream.SendDownstreamRaw(message);
					_downstream.Disconnect("Upstream closed the connection", false);
					return;

				default:
					// Some offline servers skip encryption entirely: network settings, login, then
					// straight to PlayStatus with no handshake. The first real gameplay packet is
					// the signal that login succeeded.
					SignalReady();
					_downstream.SendDownstreamRaw(message);
					return;
			}
		}

		private void SignalReady()
		{
			if (_readySignalled) return;
			_readySignalled = true;
			_downstream.OnUpstreamReady();
		}
	}
}
