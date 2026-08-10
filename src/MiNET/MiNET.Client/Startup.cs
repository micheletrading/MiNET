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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Client
{
	public class Startup
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Startup));

		// ReSharper disable once InconsistentNaming
		private const string MiNET = "\r\n __   __  ___   __    _  _______  _______ \r\n|  |_|  ||   | |  |  | ||       ||       |\r\n|       ||   | |   |_| ||    ___||_     _|\r\n|       ||   | |       ||   |___   |   |  \r\n|       ||   | |  _    ||    ___|  |   |  \r\n| ||_|| ||   | | | |   ||   |___   |   |  \r\n|_|   |_||___| |_|  |__||_______|  |___|  \r\n";

		/// <summary>Places one of every block that keeps a block entity, in a line beside the bot, by
		/// asking the server to do it. Nothing is read back here: the server sends the block entity
		/// data by itself, and BedrockTraceHandler prints the tag it arrives in.</summary>
		private static void ProbeContainerBlocks(MiNetClient client)
		{
			string[] blocks =
			{
				"chest", "trapped_chest", "copper_chest", "waxed_oxidized_copper_chest",
				"undyed_shulker_box", "red_shulker_box", "barrel", "smoker", "brewing_stand",
				"hopper", "dispenser", "dropper", "crafter", "furnace", "blast_furnace",
				"ender_chest", "lectern", "chiseled_bookshelf", "decorated_pot", "enchanting_table",
				"beacon"
			};

			Action<Task, string> send = BotHelpers.DoSendCommand(client);

			var origin = (BlockCoordinates) client.CurrentLocation;
			Log.Warn($"Block probe: placing {blocks.Length} blocks from {origin}");

			for (int i = 0; i < blocks.Length; i++)
			{
				int x = origin.X + 2 + i * 2;
				send(null, $"/setblock {x} {origin.Y} {origin.Z + 2} {blocks[i]}");
				Thread.Sleep(500);

				// A container the server considers empty may not be worth a block entity to it.
				send(null, $"/replaceitem block {x} {origin.Y} {origin.Z + 2} slot.container 0 diamond 1");
				Thread.Sleep(500);
			}

			Log.Warn("Block probe: placed, waiting for the server to answer");
			Thread.Sleep(10000);
			Log.Warn("Block probe: done");
		}

		/// <summary>Moves the bot to a place and reports the block the server actually sent for each
		/// coordinate asked about. This is the client's side of a ghost block: when the world file and
		/// the server agree that a chest is there and the player sees an outline with nothing in it,
		/// the question is what went over the wire, and this answers it without a real client.</summary>
		private static void InspectBlocks(MiNetClient client, string spec)
		{
			List<BlockCoordinates> wanted = spec
				.Split(';', StringSplitOptions.RemoveEmptyEntries)
				.Select(part => part.Split(','))
				.Select(p => new BlockCoordinates(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2])))
				.ToList();

			if (wanted.Count == 0) return;

			// The server sends chunks around where it thinks the player is, so stand there first.
			BlockCoordinates first = wanted[0];
			client.CurrentLocation = new PlayerLocation(first.X, first.Y + 2, first.Z);
			for (int i = 0; i < 20; i++)
			{
				client.SendMcpeMovePlayer();
				Thread.Sleep(500);
			}

			Log.Warn($"Inspect: {client.Chunks.Count} chunks received, standing at {client.CurrentLocation}");

			foreach (BlockCoordinates coordinates in wanted)
			{
				var chunkCoordinates = new ChunkCoordinates(coordinates.X >> 4, coordinates.Z >> 4);
				// A chunk that failed to decode is still put in the dictionary, as null.
				if (!client.Chunks.TryGetValue(chunkCoordinates, out ChunkColumn chunk) || chunk == null)
				{
					Log.Warn($"Inspect {coordinates}: chunk {chunkCoordinates} never arrived or did not decode");
					continue;
				}

				int runtimeId = chunk.GetBlockRuntimeId(coordinates.X & 0x0f, coordinates.Y, coordinates.Z & 0x0f);
				Block block = chunk.GetBlockObject(coordinates.X & 0x0f, coordinates.Y, coordinates.Z & 0x0f);
				bool hasBlockEntity = chunk.GetBlockEntity(coordinates) != null;

				Log.Warn($"Inspect {coordinates}: runtimeId={runtimeId} block={block?.Name ?? "(null)"} blockEntity={hasBlockEntity}");
			}
		}

		/// <summary>Right-clicks a block the way a server-authoritative client does: the use-item
		/// transaction folded into PlayerAuthInput, not a standalone McpeInventoryTransaction. This is
		/// the only way to exercise that path without a real client.</summary>
		private static void InteractWithBlock(MiNetClient client, string spec)
		{
			string[] parts = spec.Split(',');
			var target = new BlockCoordinates(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

			// Stand next to it, looking at it.
			var position = new Vector3(target.X + 0.5f, target.Y + 1, target.Z + 2.5f);
			client.CurrentLocation = new PlayerLocation(position, 0, 0, 20);

			Log.Warn($"Interact: standing at {position}, right-clicking {target}");

			Vector3 previous = position;
			for (int i = 0; i < 80; i++)
			{
				var input = McpePlayerAuthInput.CreateObject();
				input.playerRotation = new Vector2(20, 0);
				input.playerHeadRotation = 0;
				input.position = position;
				input.moveVector = Vector2.Zero;
				input.inputData = 0;
				input.inputMode = McpePlayerAuthInput.InputMode.Mouse;
				input.playMode = McpePlayerAuthInput.ClientPlayMode.Normal;
				input.newInteractionModel = McpePlayerAuthInput.NewInteractionModel.Touch;
				input.interactRotation = new Vector2(20, 0);
				input.clientTick = i;
				input.posDelta = position - previous;
				input.analogMoveVector = Vector2.Zero;
				input.cameraOrientation = new Vector3(20, 0, 0);
				input.rawMoveVector = Vector2.Zero;

				// Give the server time to send the chunks first, then click once.
				if (i == 60)
				{
					input.itemUseTransaction = new PackedItemUseLegacyInventoryTransaction
					{
						itemUseTransaction = new ItemUseInventoryTransaction
						{
							actionType = ItemUseInventoryTransaction.ItemUseActionType.Place,
							triggerType = ItemUseInventoryTransaction.ItemUseTriggerType.PlayerInput,
							position = target,
							face = (byte) BlockFace.Up,
							slot = 0,
							item = new ItemAir(),
							fromPosition = position,
							clickPosition = new Vector3(0.5f, 1.0f, 0.5f),
							targetBlockId = 0,
							clientInteractPrediction = ItemUseInventoryTransaction.ItemUsePredictedResult.Success,
							clientCooldownState = ItemUseInventoryTransaction.ItemUseClientCooldownState.Off
						}
					};

					Log.Warn($"Interact: sending the use-item transaction inside auth input for {target}");
				}

				client.SendPacket(input);
				Thread.Sleep(50);
			}

			Log.Warn("Interact: sent, waiting for the server to answer");
			Thread.Sleep(5000);
			Log.Warn("Interact: done");
		}

		static void Main(string[] args)
		{
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log4net.xml")));

			Log.Info(MiNET);
			Console.WriteLine(MiNET);
			Console.WriteLine("Starting client...");

			// Username from the environment so several bots can be on a server at once. Questions
			// about who appears in a player list, and how many records each join produces, cannot be
			// answered with a single connection: with one player "everyone" and "everyone but me"
			// are the same list.
			string username = Environment.GetEnvironmentVariable("MINET_USERNAME") ?? "TheGrey";

			// MINET_TARGET=host:port points the bot somewhere other than the local BDS, e.g. at
			// MiNET itself or at MiNET.Tunnel.
			string targetEnv = Environment.GetEnvironmentVariable("MINET_TARGET");
			IPEndPoint target = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 19132);
			if (!string.IsNullOrWhiteSpace(targetEnv))
			{
				string[] parts = targetEnv.Split(':');
				IPAddress ip = IPAddress.TryParse(parts[0], out var parsed) ? parsed : Dns.GetHostAddresses(parts[0])[0];
				target = new IPEndPoint(ip, parts.Length > 1 ? int.Parse(parts[1]) : 19132);
			}

			var client = new MiNetClient(target, username);
			//var client = new MiNetClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 19132), "TheGrey");
			//var client = new MiNetClient(new IPEndPoint(Dns.GetHostEntry("test.pmmp.io").AddressList[0], 19132), "TheGrey", new DedicatedThreadPool(new DedicatedThreadPoolSettings(Environment.ProcessorCount)));
			//var client = new MiNetClient(new IPEndPoint(IPAddress.Parse("192.168.0.4"), 19162), "TheGrey", new DedicatedThreadPool(new DedicatedThreadPoolSettings(Environment.ProcessorCount)));
			//var client = new MiNetClient(new IPEndPoint(IPAddress.Parse("213.89.103.206"), 19132), "TheGrey", new DedicatedThreadPool(new DedicatedThreadPoolSettings(Environment.ProcessorCount)));
			//var client = new MiNetClient(new IPEndPoint(Dns.GetHostEntry("yodamine.com").AddressList[0], 19132), "TheGrey");
			//var client = new MiNetClient(new IPEndPoint(IPAddress.Loopback, 19132), "TheGrey", new DedicatedThreadPool(new DedicatedThreadPoolSettings(Environment.ProcessorCount)));

			client.MessageHandler = new BedrockTraceHandler(client);
			// MINET_BLOB_CACHE=1 makes the bot announce ClientCacheStatus enabled, the way every
			// real client does; vanilla then serves blob-addressed chunks. Off keeps the plain flow.
			client.UseBlobCache = Environment.GetEnvironmentVariable("MINET_BLOB_CACHE") == "1";

			// MINET_XBL=1 logs in with a real Xbox Live account instead of an offline identity, which
			// is what an online-mode server requires. The first run prints a code to enter in a
			// browser; after that the saved refresh token is used and nothing is asked.
			if (Environment.GetEnvironmentVariable("MINET_XBL") == "1")
			{
				var authentication = new XboxAuthentication();
				authentication.DeviceCodeRequired += (uri, code) =>
				{
					Console.WriteLine();
					Console.WriteLine("=======================================================");
					Console.WriteLine($"  Sign in at : {uri}");
					Console.WriteLine($"  Enter code : {code}");
					Console.WriteLine("=======================================================");
					Console.WriteLine();
				};

				client.XboxIdentity = authentication.AuthenticateAsync().GetAwaiter().GetResult();
				Console.WriteLine($"Authenticated as {client.XboxIdentity.DisplayName}");
			}

			// MINET_TRANSPORT=nethernet dials over WebRTC instead of RakNet. There is no offline
			// ping and no connection handshake on that path: signaling is one HTTP round trip and
			// the data channel opening is the connection.
			bool netherNet = Environment.GetEnvironmentVariable("MINET_TRANSPORT") == "nethernet";

			if (netherNet)
			{
				Console.WriteLine($"Connecting over NetherNet to {client.ServerEndPoint} ...");

				if (!client.ConnectNetherNetAsync().GetAwaiter().GetResult())
				{
					Console.WriteLine("Failed to connect over NetherNet");
					return;
				}

				Console.WriteLine("NetherNet data channel open, logging in ...");
			}
			else
			{
				client.StartClient();
				Log.Warn("Client listening for connecting on: " + client.ClientEndpoint);

				Console.WriteLine("Looking for server...");

				if (!client.Connection.TryLocate(client.ServerEndPoint, out (IPEndPoint serverEndPoint, string serverName) info))
				{
					Console.WriteLine($"Failed to locate server at {client.ServerEndPoint}");
					return;
				}

				Console.WriteLine($"... YEAH! FOUND SERVER! It's at {info.serverEndPoint}, \"{info.serverName}\"");

				if (!client.Connection.TryConnect(info.serverEndPoint))
				{
					Console.WriteLine($"Failed to connect to server at {info.serverEndPoint}");
					return;
				}
			}

			Console.WriteLine("Waiting for spawn...");
			client.PlayerStatusChangedWaitHandle.WaitOne();
			Console.WriteLine("... spawned");

			client.HasSpawned = true;

			if (Environment.GetEnvironmentVariable("MINET_EMULATE") == "1")
			{
				RealClientEmulator.Run(client);
			}

			// MINET_BLOCK_PROBE=1 has the server place one of every container block next to the bot
			// and leaves what the server answers with in the log. The block entity data that comes
			// back carries the savegame id of each one, which is the only way to know what vanilla
			// calls a barrel's or a copper chest's tile rather than inferring it from convention.
			if (Environment.GetEnvironmentVariable("MINET_BLOCK_PROBE") == "1")
			{
				ProbeContainerBlocks(client);
			}

			// MINET_INSPECT="x,y,z;x,y,z" walks the bot to the first coordinate and reports what the
			// server sent for each: the runtime id, the block it decodes to, and whether a block entity
			// came with it. That is the only view of a ghost block that is neither the world file nor
			// the server's own memory.
			string inspect = Environment.GetEnvironmentVariable("MINET_INSPECT");
			if (!string.IsNullOrWhiteSpace(inspect))
			{
				InspectBlocks(client, inspect);
			}

			// MINET_INTERACT="x,y,z" right-clicks that block through the auth-input path.
			string interact = Environment.GetEnvironmentVariable("MINET_INTERACT");
			if (!string.IsNullOrWhiteSpace(interact))
			{
				InteractWithBlock(client, interact);
			}

			Action<Task, PlayerLocation> doMoveTo = BotHelpers.DoMoveTo(client);

			Action<Task, string> doSendCommand = BotHelpers.DoSendCommand(client);

			Task.Run(BotHelpers.DoWaitForSpawn(client))
				// Bot commands disabled: McpeCommandRequest is not updated to the current
				// protocol yet, so keep un-modernized packets off the wire.
				//.ContinueWith(t => doSendCommand(t, $"/me says \"I spawned at {client.CurrentLocation}\""))
				//.ContinueWith(task =>
				//{
				//	var request = new McpeCommandRequest();
				//	request.command = "/setblock ~ ~-1 ~ log 0 replace";
				//	request.unknownUuid = new UUID(Guid.NewGuid().ToString());
				//	client.SendPacket(request);

				//	var coord =  (BlockCoordinates) client.CurrentLocation;
				//	var pick = McpeBlockPickRequest.CreateObject();
				//	pick.x = coord.X;
				//	pick.y = coord.Y;
				//	pick.z = coord.Z;
				//	client.SendPacket(request);
				//})

				//.ContinueWith(t => BotHelpers.DoMobEquipment(client)(t, new ItemBlock(new Cobblestone()) {Count = 64}, 0))
				//.ContinueWith(t => BotHelpers.DoMoveTo(client)(t, new PlayerLocation(client.CurrentLocation.ToVector3() - new Vector3(0, 1, 0), 180, 180, 180)))
				//.ContinueWith(t => doMoveTo(t, new PlayerLocation(40, 5.62f, -20, 180, 180, 180)))
				//.ContinueWith(t => doMoveTo(t, new PlayerLocation(0, 5.62, 0, 180 + 45, 180 + 45, 180)))
				//.ContinueWith(t => doMoveTo(t, new PlayerLocation(0, 5.62, 0, 180 + 45, 180 + 45, 180)))
				//.ContinueWith(t => doMoveTo(t, new PlayerLocation(22, 5.62, 40, 180 + 45, 180 + 45, 180)))
				//.ContinueWith(t => doMoveTo(t, new PlayerLocation(50, 5.62f, 17, 180, 180, 180)))
				//.ContinueWith(t => doSendCommand(t, "/me says \"Hi guys! It is I!!\""))
				//.ContinueWith(t => Task.Delay(500).Wait())
				//.ContinueWith(t => doSendCommand(t, "/summon sheep"))
				//.ContinueWith(t => Task.Delay(500).Wait())
				//.ContinueWith(t => doSendCommand(t, "/kill @e[type=sheep]"))
				.ContinueWith(t => Task.Delay(5000).Wait())
				// Skin switching test: sends a recoloured skin at the given frames per second.
				//.ContinueWith(t => BotHelpers.DoCycleSkinColors(client)(t, 20))
				//.ContinueWith(t =>
				//{
				//	Random rnd = new Random();
				//	while (true)
				//	{
				//		doMoveTo(t, new PlayerLocation(rnd.Next(10, 40), 5.62f, rnd.Next(-50, -10), 180, 180, 180));
				//		//doMoveTo(t, new PlayerLocation(50, 5.62f, 17, 180, 180, 180));
				//		doMoveTo(t, new PlayerLocation(rnd.Next(40, 50), 5.62f, rnd.Next(0, 20), 180, 180, 180));
				//	}
				//})
				;

			//string fileName = Path.GetTempPath() + "MobSpawns_" + Guid.NewGuid() + ".txt";
			//FileStream file = File.OpenWrite(fileName);
			//Log.Info($"Writing mob spawns to file:\n{fileName}");
			//_mobWriter = new IndentedTextWriter(new StreamWriter(file));
			//Task.Run(BotHelpers.DoWaitForSpawn(client))
			//	.ContinueWith(task =>
			//	{
			//		foreach (EntityType entityType in Enum.GetValues(typeof(EntityType)))
			//		{
			//			if (entityType == EntityType.Wither) continue;
			//			if (entityType == EntityType.Dragon) continue;
			//			if (entityType == EntityType.Slime) continue;

			//			string entityName = entityType.ToString();
			//			entityName = Regex.Replace(entityName, "([A-Z])", "_$1").TrimStart('_').ToLower();
			//			{
			//				string command = $"/summon {entityName}";
			//				McpeCommandRequest request = new McpeCommandRequest();
			//				request.command = command;
			//				request.unknownUuid = new UUID(Guid.NewGuid().ToString());
			//				client.SendPackage(request);
			//			}

			//			Task.Delay(500).Wait();

			//			{
			//				McpeCommandRequest request = new McpeCommandRequest();
			//				request.command = $"/kill @e[type={entityName}]";
			//				request.unknownUuid = new UUID(Guid.NewGuid().ToString());
			//				client.SendPackage(request);
			//			}
			//		}

			//		{
			//			McpeCommandRequest request = new McpeCommandRequest();
			//			request.command = $"/kill @e[type=!player]";
			//			request.unknownUuid = new UUID(Guid.NewGuid().ToString());
			//			client.SendPackage(request);
			//		}

			//	});

			using var stopped = new ManualResetEventSlim();
			int shuttingDown = 0;

			// The same leave the client does on <Enter>, reachable from a signal so a run with no
			// console still says goodbye. Without it the server only notices on the RakNet timeout,
			// which leaves the player standing in the world for another half minute. Idempotent
			// because several of the hooks below can fire for one stop.
			void Shutdown()
			{
				if (Interlocked.Exchange(ref shuttingDown, 1) != 0) return;

				if (client.IsConnected) client.SendDisconnectionNotification();
				Thread.Sleep(50);
				client.StopClient();
				stopped.Set();
			}

			// Cancel the signal so the runtime lets us finish rather than tearing the process down
			// mid-disconnect. Nothing can hook an outright kill, so that path still costs a timeout.
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				Shutdown();
			};

			using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
			{
				context.Cancel = true;
				Shutdown();
			});

			using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
			{
				context.Cancel = true;
				Shutdown();
			});

			AppDomain.CurrentDomain.ProcessExit += (_, _) => Shutdown();

			// Started without a console (background process, service, redirected stdin) ReadLine
			// returns immediately on EOF and the bot would quit the moment it finished logging in.
			if (Console.IsInputRedirected)
			{
				Console.WriteLine("Running until terminated.");
				stopped.Wait();
			}
			else
			{
				Console.WriteLine("<Enter> to exit!");
				Console.ReadLine();
				Shutdown();
			}
		}
	}
}