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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Threading;
using log4net;
using MiNET.Client;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.IO;
using MiNET.Utils.Vectors;

#pragma warning disable 1591

namespace MiNET.ServiceKiller
{
	public class EmulatorClient
	{
		private readonly DedicatedThreadPool _threadPool;

		public int RanMin { get; set; }
		public int RanMax { get; set; }
		public int ChunkRadius { get; set; }

		private static readonly ILog Log = LogManager.GetLogger(typeof(EmulatorClient));
		public IPEndPoint EndPoint { get; }

		public Emulator Emulator { get; private set; }
		public string Name { get; set; }
		public int ClientId { get; set; }
		public Random Random { get; set; } = new Random();
		public TimeSpan TimeToRun { get; set; }
		public bool UseNetherNet { get; set; }

		public EmulatorClient(DedicatedThreadPool threadPool, Emulator emulator, TimeSpan timeToRun, string name, int clientId, IPEndPoint endPoint, int ranMin = 150, int ranMax = 450, int chunkRadius = 8, bool useNetherNet = false)
		{
			_threadPool = threadPool;
			Emulator = emulator;
			TimeToRun = timeToRun;
			Name = name;
			ClientId = clientId;
			EndPoint = endPoint;
			RanMin = ranMin;
			RanMax = ranMax;
			ChunkRadius = chunkRadius;
			UseNetherNet = useNetherNet;
		}

		public void EmulateClient()
		{
			try
			{
				Console.WriteLine($"Client {Name} connecting...");

				var client = new MiNetClient(EndPoint, Name, _threadPool);
				client.ChunkRadius = ChunkRadius;
				client.IsEmulator = true;
				client.UseBlobCache = false;
				client.ClientId = ClientId;

				if (UseNetherNet)
				{
					// Signaling plus data channel opening is the whole connection; the login
					// sequence starts from the handler's Connected() inside ConnectNetherNetAsync.
					if (!client.ConnectNetherNetAsync().GetAwaiter().GetResult())
					{
						Console.WriteLine($"Client {Name} failed to connect over NetherNet to {EndPoint}");
						Emulator.ConcurrentSpawnWaitHandle.Set();
						client.StopClient();
						return;
					}
				}
				else
				{
					client.StartClient();
					// Emulator mode: ACK every datagram inline at receive (so the server's RTO never
					// fires against a bot whose pool is busy decoding chunks) and skip the bot's own
					// resend tracking. Everything else runs as a real client: decode, ordering, send tick.
					client.Connection.ConnectionInfo.IsEmulator = true;

					if (!client.Connection.TryConnect(EndPoint, 20))
					{
						Console.WriteLine($"Client {Name} failed to connect to {EndPoint}");
						Emulator.ConcurrentSpawnWaitHandle.Set();
						client.StopClient();
						return;
					}
				}

				// Fires on PlayStatus(PlayerSpawn), after the handler has run the real spawn tail:
				// chunk radius request, subchunk requests per LevelChunk, loading screen close and
				// set_local_player_as_initialized.
				if (!client.PlayerStatusChangedWaitHandle.WaitOne(TimeSpan.FromSeconds(30)))
				{
					Console.WriteLine($"Client {Name} connected but never spawned");
					Emulator.ConcurrentSpawnWaitHandle.Set();
					client.StopClient();
					return;
				}

				Emulator.ConcurrentSpawnWaitHandle.Set();
				Console.WriteLine($"Client {Name} spawned, emulating...");

				// Everything the bot needed to hear (login, chunks, spawn) has been heard. From here
				// it only receives and ACKs: incoming batches are dropped whole before decrypt and
				// decompress. RakNet-level disconnects still land, so IsConnected stays honest.
				client.WrapperHandler.IgnoreIncoming = true;

				// Hold the walk until the join burst has drained: subchunk responses keep streaming
				// in after spawn, and a bot that walks while its socket is drowning stutters and
				// then catches up in a jump. Half a second of silence means the world is in; the
				// cap keeps a noisy server from holding the bot at spawn forever.
				var quietWait = Stopwatch.StartNew();
				while (quietWait.ElapsedMilliseconds < 10000 && client.WrapperHandler.MillisSinceLastIncoming < 500 && client.IsConnected)
				{
					Thread.Sleep(100);
				}

				// The walk itself runs on the fleet's one shared clock, not on this thread: a
				// sleeping thread per bot is tens of thousands of scheduler wakes per second at
				// fleet scale, and that scheduling was measured to dwarf the actual protocol work.
				// Registration is the handoff; this spawn thread's job ends here.
				Emulator.WalkClock.Register(new BotWalker(client, Emulator, TimeToRun, Name, RanMin, RanMax, UseNetherNet, Random));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}