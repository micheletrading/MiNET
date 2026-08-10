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

				var runningTime = Stopwatch.StartNew();

				// Movement, one PlayerAuthInput per iteration with the absolute position and a
				// forward move vector, the way a real 1.26 client drives it. Steps advance from
				// measured elapsed time, so the pace along the path is exactly the vanilla walking
				// speed regardless of send cadence or load.
				const float WalkSpeed = 4.317f; // vanilla walking, blocks per second
				const float Height = 50f;
				const double ClimbPerRevolution = 4.0; // a walkable ramp, ~12.5 turns to the top

				PlayerLocation startLocation = client.CurrentLocation;
				var lastPosition = new Vector3(startLocation.X, startLocation.Y, startLocation.Z);
				var forward = new Vector2(0, 1);
				long tick = 1;
				float lastYaw = 0f;
				TimeSpan lastSent = runningTime.Elapsed;

				// The step budget for this iteration. Capped so a stalled bot (receive flood, GC,
				// scheduling) resumes walking from where it was instead of teleporting to where
				// the clock says it should be.
				float NextStepSeconds()
				{
					TimeSpan now = runningTime.Elapsed;
					float dt = (float) (now - lastSent).TotalSeconds;
					lastSent = now;
					return Math.Min(dt, 0.2f);
				}

				// Sends one movement step facing the direction of travel, and paces the loop.
				void SendStep(Vector3 position)
				{
					Vector3 posDelta = position - lastPosition;
					float length = posDelta.Length();

					float yaw = lastYaw;
					float pitch = 0f;
					if (length > 0.0001f)
					{
						yaw = (float) ((Math.Atan2(-posDelta.X, posDelta.Z).ToDegrees() + 360) % 360);
						pitch = (float) (-Math.Asin(posDelta.Y / length)).ToDegrees();
					}
					lastYaw = yaw;

					var input = McpePlayerAuthInput.CreateObject();
					input.playerRotation = new Vector2(pitch, yaw);
					input.playerHeadRotation = yaw;
					input.position = position;
					input.moveVector = forward;
					input.inputData = AuthInputFlags.BlockBreakingDelayEnabled | AuthInputFlags.WalkForwards;
					input.inputMode = McpePlayerAuthInput.InputMode.Mouse;
					input.playMode = McpePlayerAuthInput.ClientPlayMode.Normal;
					input.newInteractionModel = McpePlayerAuthInput.NewInteractionModel.Touch;
					input.interactRotation = new Vector2(pitch, yaw);
					input.clientTick = tick++;
					input.posDelta = posDelta;
					input.analogMoveVector = forward;
					input.cameraOrientation = new Vector3(0, yaw, 0);
					input.rawMoveVector = forward;
					client.SendPacket(input);

					lastPosition = position;
					client.CurrentLocation = new PlayerLocation(position, yaw, yaw, pitch);

					int timeout = RanMin == RanMax ? RanMin : Random.Next(RanMin, RanMax);
					if (timeout > 0) Thread.Sleep(timeout);
				}

				bool KeepWalking() => Emulator.Running && runningTime.Elapsed < TimeToRun && client.IsConnected;

				// Phase 1: walk home. Plugins (Plotter) persist player positions, so bots rejoin
				// scattered wherever their last run ended, drifting further out every run. Every
				// walk therefore starts by heading back to the level spawn; the helix always
				// dances around the same pole.
				Vector3 worldSpawn = client.WorldSpawn != default ? client.WorldSpawn : lastPosition;
				while (KeepWalking())
				{
					Vector3 toSpawn = worldSpawn - lastPosition;
					float distance = toSpawn.Length();
					if (distance < 0.5f) break;

					float step = Math.Min(distance, WalkSpeed * NextStepSeconds());
					SendStep(lastPosition + toSpawn * (step / distance));
				}

				// Phase 2: the helix. From the level spawn up to Height above it, turn around and
				// walk the same corkscrew back down, back and forth for the rest of the run.
				PlayerLocation center = client.CurrentLocation;
				float radius = Random.Next(5, 20);
				double climbSlope = ClimbPerRevolution / (2 * Math.PI); // dy/dAngle, constant
				// Constant because the ramp is linear: |dPos/dAngle| = sqrt(radius^2 + slope^2).
				double stepPerAngle = Math.Sqrt(radius * radius + climbSlope * climbSlope);

				// The band is absolute, anchored on the level spawn: floor at the spawn itself,
				// ceiling at Height above it. The approach ends within half a block of the spawn,
				// so these angles only square up the turnarounds.
				double floorAngle = (worldSpawn.Y - center.Y) / climbSlope;
				double topAngle = (worldSpawn.Y + Height - center.Y) / climbSlope;

				// The helix axis sits one radius to the side, so the path's first position (angle 0)
				// is exactly where the bot stands. Without this the first input teleports the bot
				// sideways onto the circle, which reads as a jump the moment it starts moving.
				// The side is a random bearing per bot: with a fixed one, every circle leaves the
				// spawn in the same direction and the fleet braids into a single visible rope.
				double startBearing = Random.NextDouble() * 2 * Math.PI;
				float axisX = center.X - radius * (float) Math.Cos(startBearing);
				float axisZ = center.Z - radius * (float) Math.Sin(startBearing);

				double angle = 0.0;
				int direction = 1;

				while (KeepWalking())
				{
					angle += direction * WalkSpeed * NextStepSeconds() / stepPerAngle;

					// Ceiling reached: turn around and retrace. Floor reached: head back up. Only
					// the direction flips; the position is never clamped.
					if (direction > 0 && angle >= topAngle) direction = -1;
					else if (direction < 0 && angle <= floorAngle) direction = 1;

					float x = axisX + (float) (radius * Math.Cos(angle + startBearing));
					float z = axisZ + (float) (radius * Math.Sin(angle + startBearing));
					float y = center.Y + (float) (climbSlope * angle);
					SendStep(new Vector3(x, y, z));
				}

				if (client.IsConnected)
				{
					client.SendChat("Shadow gov agent BREXITING!");
					// RakNet-level goodbye; on NetherNet closing the data channel is the disconnect,
					// which StopClient below does.
					if (!UseNetherNet) client.SendDisconnectionNotification();
				}

				client.StopClient();
				Console.WriteLine($"{runningTime.ElapsedMilliseconds} Client stopped. {client.IsConnected}, {Emulator.Running}");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}