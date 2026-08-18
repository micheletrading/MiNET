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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MiNET.Console
{
	/// <summary>
	///     The other end of <see cref="RemoteConsole" />. Invoked as
	///     <c>MiNET.Console remote [--host H] [--port P] --secret S &lt;command&gt;</c>, or with no
	///     command to read command lines from stdin until end of input.
	/// </summary>
	public static class RemoteConsoleClient
	{
		public static async Task<int> Run(string[] args)
		{
			string host = "127.0.0.1";
			int port = 19140;
			string secret = Environment.GetEnvironmentVariable("MINET_REMOTE_SECRET");
			var command = new List<string>();

			for (int i = 1; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "--host" when i + 1 < args.Length:
						host = args[++i];
						break;
					case "--port" when i + 1 < args.Length:
						port = int.Parse(args[++i]);
						break;
					case "--secret" when i + 1 < args.Length:
						secret = args[++i];
						break;
					default:
						command.Add(args[i]);
						break;
				}
			}

			if (string.IsNullOrWhiteSpace(secret))
			{
				System.Console.Error.WriteLine("No secret. Pass --secret <value> or set MINET_REMOTE_SECRET.");
				return 2;
			}

			try
			{
				using var client = new TcpClient();
				await client.ConnectAsync(host, port);
				await using var stream = client.GetStream();

				var cancellation = CancellationToken.None;

				string nonce = await RemoteConsoleProtocol.ReadFrameAsync(stream, cancellation);
				await RemoteConsoleProtocol.WriteFrameAsync(stream, RemoteConsoleProtocol.Answer(secret, nonce), cancellation);

				string verdict = await RemoteConsoleProtocol.ReadFrameAsync(stream, cancellation);
				if (verdict != RemoteConsoleProtocol.Accepted)
				{
					System.Console.Error.WriteLine("Rejected by server: bad secret.");
					return 3;
				}

				if (command.Count > 0) return await Send(stream, string.Join(' ', command), cancellation);

				// No command on the line, so act as a session and take one command per input line.
				string line;
				while ((line = System.Console.ReadLine()) != null)
				{
					if (line.Trim().Length == 0) continue;
					int code = await Send(stream, line, cancellation);
					if (code != 0) return code;
				}

				return 0;
			}
			catch (Exception e)
			{
				System.Console.Error.WriteLine($"Remote console failed: {e.Message}");
				return 1;
			}
		}

		private static async Task<int> Send(NetworkStream stream, string line, CancellationToken cancellation)
		{
			await RemoteConsoleProtocol.WriteFrameAsync(stream, line, cancellation);

			string response = await RemoteConsoleProtocol.ReadFrameAsync(stream, cancellation);
			if (response == null)
			{
				System.Console.Error.WriteLine("Server closed the connection.");
				return 1;
			}

			System.Console.WriteLine(response);
			return 0;
		}
	}
}
