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
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MiNET.Client;
using MiNET.Net;
using MiNET.Utils.IO;

namespace MiNET.Tunnel
{
	/// <summary>
	///     The downstream half of the tunnel. Handles the real client's login locally (RakNet,
	///     network settings, encryption), then logs in to the upstream server with its own offline
	///     identity and from that point forwards every frame verbatim in both directions.
	/// </summary>
	public class TunnelPlayer : Player, IRawPacketHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TunnelPlayer));

		private readonly IPEndPoint _target;
		private readonly TunnelDump _dump;
		private MiNetClient _upstream;
		private readonly object _upstreamSync = new object();
		private readonly Queue<byte[]> _pendingUpstream = new Queue<byte[]>();
		private bool _upstreamReady;

		public TunnelPlayer(MiNetServer server, IPEndPoint endPoint, IPEndPoint target, TunnelDump dump) : base(server, endPoint)
		{
			_target = target;
			_dump = dump;
		}

		public override void HandleMcpeClientToServerHandshake(McpeClientToServerHandshake message)
		{
			// The vanilla Player answers with PlayStatus and ResourcePacksInfo here. The tunnel
			// instead logs in upstream; the upstream server's own responses get forwarded down.
			Log.Warn($"Tunnel: {Username} completed downstream login, connecting upstream to {_target}");
			Task.Run(ConnectUpstream);
		}

		private void ConnectUpstream()
		{
			try
			{
				var client = new MiNetClient(_target, Username ?? "TunnelUser");
				client.ClientMessageHandlerFactory = (session, fallback) => new TunnelUpstreamHandler(session, fallback, this);
				_upstream = client;
				client.StartClient();

				if (!client.Connection.TryLocate(_target, out (IPEndPoint serverEndPoint, string serverName) info, 10))
				{
					Disconnect("Tunnel: upstream server not responding to pings");
					return;
				}

				Log.Warn($"Tunnel: located upstream '{info.serverName?.Split(';')[1]}' at {info.serverEndPoint}");

				if (!client.Connection.TryConnect(info.serverEndPoint, 10))
				{
					Disconnect("Tunnel: upstream connect failed");
				}
				// Connected. The handler factory takes it from here: network settings, offline
				// login, encryption, then OnUpstreamReady() when the upstream accepts the login.
			}
			catch (Exception e)
			{
				Log.Error("Tunnel: upstream connect", e);
				Disconnect("Tunnel: upstream error");
			}
		}

		public bool HandleRawPacket(Packet message)
		{
			ReadOnlyMemory<byte> frame = TunnelDump.FrameOf(message);
			_dump.Write("c2s", message, frame);

			lock (_upstreamSync)
			{
				if (!_upstreamReady)
				{
					// The real client fires ClientCacheStatus and friends the instant its login
					// completes, before the upstream leg exists. Hold them in order.
					_pendingUpstream.Enqueue(frame.ToArray());
					return true;
				}
			}

			SendUpstream(frame);
			return true;
		}

		internal void OnUpstreamReady()
		{
			int flushed;
			lock (_upstreamSync)
			{
				_upstreamReady = true;
				flushed = _pendingUpstream.Count;
				while (_pendingUpstream.Count > 0) SendUpstream(_pendingUpstream.Dequeue());
			}

			Log.Warn($"Tunnel: upstream login complete for {Username}, flushed {flushed} queued frames");
		}

		private void SendUpstream(ReadOnlyMemory<byte> frame)
		{
			var session = _upstream?.Session;
			if (session == null) return;

			McpeWrapper batch = BatchUtils.CreateBatchPacket(MemoryMarshal.AsMemory(frame), CompressionLevel.Fastest, true);
			session.SendPacket(batch);
		}

		internal void SendDownstreamRaw(Packet message)
		{
			ReadOnlyMemory<byte> frame = TunnelDump.FrameOf(message);
			_dump.Write("s2c", message, frame);

			var handler = NetworkHandler;
			if (handler == null) return;

			McpeWrapper batch = BatchUtils.CreateBatchPacket(MemoryMarshal.AsMemory(frame), CompressionLevel.Fastest, true);
			handler.SendPacket(batch);
		}

		public override void Disconnect(string reason, bool sendDisconnect = true)
		{
			var upstream = Interlocked.Exchange(ref _upstream, null);
			if (upstream != null)
			{
				Log.Warn($"Tunnel: closing upstream for {Username}: {reason}");
				try
				{
					upstream.StopClient();
				}
				catch (Exception e)
				{
					Log.Warn("Tunnel: upstream stop", e);
				}
			}

			base.Disconnect(reason, sendDisconnect);
		}
	}
}
