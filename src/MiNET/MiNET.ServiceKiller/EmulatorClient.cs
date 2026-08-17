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
using System.Collections.Concurrent;
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

		public EmulatorClient(Emulator emulator, TimeSpan timeToRun, string name, int clientId, IPEndPoint endPoint, int ranMin = 150, int ranMax = 450, int chunkRadius = 8)
		{
			Emulator = emulator;
			TimeToRun = timeToRun;
			Name = name;
			ClientId = clientId;
			EndPoint = endPoint;
			RanMin = ranMin;
			RanMax = ranMax;
			ChunkRadius = chunkRadius;
		}

		public void EmulateClient()
		{
			try
			{
				Console.WriteLine($"Client {Name} connecting...");

				var client = new MiNetClient(EndPoint, Name);
				client.ChunkRadius = ChunkRadius;
				client.IsEmulator = true;
				// Like every real client, and not optional: the server refuses a client that says it
				// does not cache. A bot that lies about this is testing a path no player takes.
				client.UseBlobCache = true;
				// Bots mimic a real client's chunk manners: cold cache (every join pays full
				// price, which is what a load test should stress), only the surface band of
				// sections requested (the top of each column, where a real client's view lives:
				// towers and boats ride the column's own limit), and verdicts plus requests
				// batched on the walk timer instead of fired per packet.
				client.BatchChunkResponses = true;
				client.RequestTopSections = 4;
				client.ClientId = ClientId;

				// Signaling plus data channel opening is the whole connection; the login
				// sequence starts from the handler's Connected() inside ConnectNetherNetAsync.
				if (!client.ConnectNetherNetAsync().GetAwaiter().GetResult())
				{
					Console.WriteLine($"Client {Name} failed to connect over NetherNet to {EndPoint}");
					Emulator.ConcurrentSpawnWaitHandle.Set();
					client.StopClient();
					return;
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

				// The bot stays listening after spawn: the chunk dance (skeletons at the rim,
				// sub-chunk responses, blob announcements) continues for as long as it walks, and
				// answering it is the whole point of mimicking a real client. The old deaf mode
				// (IgnoreIncoming, batches dropped before decrypt) made bots pure senders and
				// silently killed every post-spawn chunk flow. Fleet-scale noise is still cheap:
				// the broadcast spam is dropped pre-decode via DropPacketIds.

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
				Emulator.WalkClock.Register(new BotWalker(client, Emulator, TimeToRun, Name, RanMin, RanMax, Random));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}