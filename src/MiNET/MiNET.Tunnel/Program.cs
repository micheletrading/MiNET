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
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using MiNET.Utils;

namespace MiNET.Tunnel
{
	internal class Program
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

		private static void Main(string[] args)
		{
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log4net.xml")));

			IPEndPoint target = ParseTarget(args);
			Log.Warn($"MiNET.Tunnel: real client -> :{Config.GetProperty("port", 19134)} -> upstream {target}");
			Console.WriteLine($"MiNET.Tunnel: forwarding to upstream {target}");

			var server = new MiNetServer();
			server.PlayerFactory = new TunnelPlayerFactory(target);
			server.StartServer();

			Console.WriteLine("MiNET.Tunnel running. Press <enter> to stop.");
			if (Console.ReadLine() == null) Thread.Sleep(Timeout.Infinite);
			server.StopServer();
		}

		private static IPEndPoint ParseTarget(string[] args)
		{
			// Priority: CLI arg, then MINET_TUNNEL_TARGET, then localhost BDS default.
			string raw = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("MINET_TUNNEL_TARGET");
			if (string.IsNullOrWhiteSpace(raw)) return new IPEndPoint(IPAddress.Loopback, 19132);

			string[] parts = raw.Split(':');
			IPAddress ip = IPAddress.TryParse(parts[0], out var parsed) ? parsed : Dns.GetHostAddresses(parts[0])[0];
			int port = parts.Length > 1 ? int.Parse(parts[1]) : 19132;
			return new IPEndPoint(ip, port);
		}
	}
}
