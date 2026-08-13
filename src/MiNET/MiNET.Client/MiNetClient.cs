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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fNbt;
using fNbt.Tags;
using Jose;
using log4net;
using MiNET.Blocks;
using MiNET.Crafting;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Net;
using MiNET.Net.NetherNet;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.IO;
using MiNET.Utils.Metadata;
using MiNET.Utils.Vectors;
using MiNET.Worlds;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SicStream;

//[assembly: XmlConfigurator(Watch = true)]
// This will cause log4net to look for a configuration file
// called TestApp.exe.config in the application base
// directory (i.e. the directory containing TestApp.exe)
// The config file will be watched for changes.

namespace MiNET.Client
{
	public class MiNetClient
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(MiNetClient));

		public IPEndPoint ServerEndPoint { get; set; }

		public bool IsEmulator { get; set; }

		/// <summary>
		///     What an emulator bot never reads, skipped before decode (see
		///     BedrockMessageHandlerBase.DropPacketIds): SubChunk block data, and the entity
		///     broadcast traffic (movement, actor state, sounds, effects) that scales O(N²) with
		///     fleet size and otherwise dominates the bot process. The one self-directed loss is
		///     MovePlayer position corrections, which a load bot ignores anyway.
		/// </summary>
		private static readonly HashSet<int> EmulatorDropPacketIds = new()
		{
			0x12, // McpeMoveEntity (MoveActorAbsolute)
			0x13, // McpeMovePlayer
			0x19, // McpeLevelEvent
			0x1d, // McpeUpdateAttributes
			0x27, // McpeSetEntityData
			0x28, // McpeSetEntityMotion
			0x2c, // McpeAnimate
			0x6f, // McpeMoveEntityDelta
			0x7b, // McpeLevelSoundEvent
			0xae, // McpeSubChunkPacket
		};

		/// <summary>The wrapper-level handler for the active connection. Exposed so the emulator
		/// can flip its post-spawn receive-and-ack-only mode.</summary>
		public BedrockMessageHandlerBase WrapperHandler { get; private set; }

		public INetworkHandler Session => _netherNetSession;

		private NetherNetClient _netherNetClient;
		private NetherNetSession _netherNetSession;

		public bool IsConnected => _netherNetSession != null;

		public Vector3 SpawnPoint { get; set; }

		/// <summary>The level's fixed spawn from StartGame (defaultSpawnBlockPosition), as opposed
		/// to SpawnPoint which is wherever this player spawned (plugins persist per-player spawns).</summary>
		public Vector3 WorldSpawn { get; set; }
		public long EntityId { get; set; }
		public long NetworkEntityId { get; set; }
		public int ChunkRadius { get; set; } = 5;

		public LevelInfo LevelInfo { get; } = new LevelInfo();

		public ConcurrentDictionary<long, Entity> Entities { get; private set; } = new ConcurrentDictionary<long, Entity>();
		public BlockPalette BlockPalette { get; set; } = new BlockPalette();

		public PlayerLocation CurrentLocation { get; set; }


		public string Username { get; set; }
		public int ClientId { get; set; }

		public McpePlayStatus.PlayStatus PlayerStatus { get; set; }
		public bool UseBlobCache { get; set; }
		public bool BlockNetworkIdsAreHashes { get; set; }
		public Dictionary<ulong, byte[]> BlobCache { get; set; } = new Dictionary<ulong, byte[]>();

		public IMcpeClientMessageHandler MessageHandler { get; set; }

		/// <summary>
		///     Overrides the ICustomMessageHandler wired onto the session, for callers that need to
		///     intercept raw frames before client-side dispatch; null keeps the default handler.
		/// </summary>
		public Func<INetworkHandler, IMcpeClientMessageHandler, BedrockClientMessageHandler> ClientMessageHandlerFactory { get; set; }

		/// <summary>
		///     A real Xbox Live identity from <see cref="XboxAuthentication" />. When set, the client
		///     logs in as that account (AuthenticationType 0) instead of minting an offline identity,
		///     which is what an online-mode server requires.
		/// </summary>
		public XboxIdentity XboxIdentity { get; set; }

		public McpeClientMessageDispatcher MessageDispatcher
		{
			get => throw new NotSupportedException("Use ClientMessageHandlerFactory instead");
			set => throw new NotSupportedException("Use ClientMessageHandlerFactory instead");
		}

		public MiNetClient(IPEndPoint endPoint, string username)
		{
			Username = username;
			ClientId = new Random().Next();
			ServerEndPoint = endPoint;
			if (ServerEndPoint != null) Log.Info("Connecting to: " + ServerEndPoint);
		}

		private static bool _cryptoWarmedUp;

		private static void WarmUpCrypto()
		{
			if (_cryptoWarmedUp) return;
			_cryptoWarmedUp = true;

			// First use of the crypto stack costs more than a second in JIT and
			// static initialization; pay that before connecting instead of inside
			// the join sequence, where the server drops sessions that stall.
			var keyPair = CryptoUtils.GenerateClientKey();
			var agreement = new ECDHBasicAgreement();
			agreement.Init(keyPair.Private);
			agreement.CalculateAgreement((ECPublicKeyParameters) keyPair.Public);
			using var sha = SHA256.Create();
			sha.ComputeHash(new byte[] {0});
		}

		/// <summary>
		///     Connects over NetherNet: the session is an INetworkHandler, the
		///     BedrockClientMessageHandler on top of it batches and compresses, and the login
		///     sequence follows from the handler's Connected().
		/// </summary>
		public async Task<bool> ConnectNetherNetAsync(CancellationToken cancellationToken = default)
		{
			WarmUpCrypto();

			_netherNetClient = await NetherNetClient.ConnectAsync(
				ServerEndPoint.Address.ToString(), ServerEndPoint.Port,
				cancellationToken: cancellationToken, identity: XboxIdentity);
			_netherNetSession = _netherNetClient.Session;

			var handler = ClientMessageHandlerFactory?.Invoke(_netherNetSession, MessageHandler ?? new DefaultMessageHandler(this))
						?? new BedrockClientMessageHandler(_netherNetSession, MessageHandler ?? new DefaultMessageHandler(this));
			if (IsEmulator) handler.DropPacketIds = EmulatorDropPacketIds;
			WrapperHandler = handler;

			_netherNetSession.CustomMessageHandler = handler;

			// The data channel opening is the connection, so the sequence starts now.
			handler.ConnectionAction = () => SendRequestNetworkSettings();
			handler.Connected();

			return true;
		}

		public bool StopClient()
		{
			// Disposing the client closes the session and its socket.
			_netherNetClient?.Dispose();
			_netherNetClient = null;
			_netherNetSession = null;
			return true;
		}

		public void SendRequestNetworkSettings()
		{
			var packet = McpeRequestNetworkSettings.CreateObject();
			packet.protocolVersion = McpeProtocolInfo.ProtocolVersion;
			SendPacket(packet);
		}

		public void SendLogin(string username)
		{
			JWT.JsonMapper = new NewtonsoftMapper();

			string identityJson;
			AsymmetricCipherKeyPair clientKey;

			// 1.21.90+ wraps login identity in an authentication envelope; since protocol 944 the
			// identity is an OIDC-style JWT in Token rather than a certificate chain.
			// AuthenticationType: 0 = full auth, 1 = self-signed, 2 = offline.
			if (XboxIdentity != null)
			{
				// The keypair is not ours to choose here: the token names it in cpk, and the server
				// keys its handshake on that, so signing the skin with any other key fails the login.
				clientKey = XboxIdentity.IdentityKey;
				username = XboxIdentity.DisplayName ?? username;

				// Full auth sends no certificate chain at all; the token is the whole identity.
				identityJson = JsonConvert.SerializeObject(new
				{
					AuthenticationType = 0,
					Token = XboxIdentity.LoginToken
				});
			}
			else
			{
				clientKey = CryptoUtils.GenerateClientKey();

				identityJson = JsonConvert.SerializeObject(new
				{
					Certificate = JsonConvert.SerializeObject(new {chain = new[] {""}}),
					AuthenticationType = 2,
					Token = CryptoUtils.EncodeOfflineMultiplayerToken(username, clientKey)
				});
			}

			byte[] data = CryptoUtils.CompressJwtBytes(Encoding.UTF8.GetBytes(identityJson), CryptoUtils.EncodeSkinJwt(clientKey, username), CompressionLevel.Fastest);

			McpeLogin loginPacket = new McpeLogin
			{
				protocolVersion = Config.GetProperty("EnableEdu", false) ? 111 : McpeProtocolInfo.ProtocolVersion,
				payload = data
			};

			var bedrockHandler = (BedrockClientMessageHandler) Session.CustomMessageHandler;
			bedrockHandler.CryptoContext = new CryptoContext
			{
				ClientKey = clientKey,
				UseEncryption = false,
			};

			SendPacket(loginPacket);
		}

		public void InitiateEncryption(byte[] serverKey, byte[] randomKeyToken)
		{
			try
			{
				ECPublicKeyParameters remotePublicKey = (ECPublicKeyParameters)
					PublicKeyFactory.CreateKey(serverKey);

				//ECDiffieHellmanPublicKey publicKey = CryptoUtils.FromDerEncoded(serverKey);
				//Log.Debug("ServerKey (b64):\n" + serverKey);
				//Log.Debug($"Cert:\n{publicKey.ToXmlString()}");

				Log.Debug($"RANDOM TOKEN (raw):\n\n{Encoding.UTF8.GetString(randomKeyToken)}");

				//if (randomKeyToken.Length != 0)
				//{
				//	Log.Error("Lenght of random bytes: " + randomKeyToken.Length);
				//}

				var bedrockHandler = (BedrockClientMessageHandler) Session.CustomMessageHandler;

				var agreement = new ECDHBasicAgreement();
				agreement.Init(bedrockHandler.CryptoContext.ClientKey.Private);
				byte[] secret;
				using (var sha = SHA256.Create())
				{
					secret = sha.ComputeHash(randomKeyToken.Concat(agreement.CalculateAgreement(remotePublicKey).ToByteArrayUnsigned()).ToArray());
				}

				Log.Debug($"SECRET KEY (raw):\n{Encoding.UTF8.GetString(secret)}");

				// Create a decrytor to perform the stream transform.
				var encryptor = new StreamingSicBlockCipher(new SicBlockCipher(new AesEngine()));
				var decryptor = new StreamingSicBlockCipher(new SicBlockCipher(new AesEngine()));
				decryptor.Init(false, new ParametersWithIV(new KeyParameter(secret), secret.Take(12).Concat(new byte[] {0, 0, 0, 2}).ToArray()));
				encryptor.Init(true, new ParametersWithIV(new KeyParameter(secret), secret.Take(12).Concat(new byte[] {0, 0, 0, 2}).ToArray()));

				// A transport that already encrypts below us means the peer is not expecting a second
				// layer, so the handshake is still acknowledged but nothing is enciphered. Same rule
				// the server side applies in LoginMessageHandler.
				bool encrypt = !Session.IsTransportEncrypted;

				bedrockHandler.CryptoContext = new CryptoContext
				{
					Decryptor = decryptor,
					Encryptor = encryptor,
					UseEncryption = encrypt,
					Key = secret
				};

				if (!encrypt) Log.Info("Transport is already encrypted, skipping the Bedrock session cipher");

				McpeClientToServerHandshake magic = new McpeClientToServerHandshake();
				SendPacket(magic);
			}
			catch (Exception e)
			{
				Log.Error("Initiate encryption", e);
			}
		}

		public AutoResetEvent FirstEncryptedPacketWaitHandle = new AutoResetEvent(false);

		public AutoResetEvent FirstPacketWaitHandle = new AutoResetEvent(false);

		public CommandPermission UserPermission { get; set; }

		public AutoResetEvent PlayerStatusChangedWaitHandle = new AutoResetEvent(false);

		public bool HasSpawned { get; set; }

		private string SerializeCompound(NbtCompound compound)
		{
			if (compound == null)
				return "null";

			StringBuilder sb = new StringBuilder();

			sb.Append("new NbtCompound { ");
			var array = compound.ToArray();

			for (var index = 0; index < array.Length; index++)
			{
				var tag = array[index];
				WriteTag(sb, tag, index == array.Length - 1);
			}

			sb.Append(" }");
			
			return sb.ToString();
		}

		private void WriteTag(StringBuilder sb, NbtTag tag, bool isLast = false)
		{
			var parameters = new List<string>();
			
			if (!string.IsNullOrWhiteSpace(tag.Name))
			{
				parameters.Add($"\"{tag.Name}\"");
			}
			
			bool hasProperties = false;
			switch (tag)
			{
				case NbtByte nd:
				{
					parameters.Add($"{nd.Value}");
				} break;

				case NbtInt nd:
				{
					parameters.Add($"{nd.Value}");
				} break;
				
				case NbtDouble nd:
				{
					parameters.Add($"{nd.Value}");
				} break;
				
				case NbtLong ni:
				{
					parameters.Add($"{ni.Value}l");
				} break;
				
				case NbtShort ni:
				{
					parameters.Add($"{ni.Value}");
				} break;
				
				case NbtFloat ni:
				{
					parameters.Add($"{ni.Value}f");
				} break;

				case NbtString ns:
				{
					parameters.Add($"\"{ns.Value}\"");
				} break;

				case NbtByteArray array:
				{
					StringBuilder subBuilder = new StringBuilder();
					subBuilder.Append($"new byte[{array.Value.Length}]{{");
					subBuilder.Append(string.Join(",", array.Value));
					subBuilder.Append("}");
					
					parameters.Add(subBuilder.ToString());
				} break;
				
				case NbtLongArray array:
				{
					StringBuilder subBuilder = new StringBuilder();
					subBuilder.Append($"new long[{array.Value.Length}]{{");

					for (var index = 0; index < array.Value.Length; index++)
					{
						var l = array.Value[index];
						subBuilder.Append($"{l}l");

						if (index != array.Value.Length - 1)
						{
							subBuilder.Append(",");
						}
					}
					
					subBuilder.Append("}");
					
					parameters.Add(subBuilder.ToString());
				} break;
				
				case NbtIntArray array:
				{
					StringBuilder subBuilder = new StringBuilder();
					subBuilder.Append($"new int[{array.Value.Length}]{{");
					subBuilder.Append(string.Join(",", array.Value));
					subBuilder.Append("}");
					
					parameters.Add(subBuilder.ToString());
				} break;

				case NbtList nbtList:
				{
					parameters.Add($"(NbtTagType){((int)nbtList.ListType)}");
					hasProperties = nbtList.Count > 0;
				} break;

				case IEnumerable:
				{
					hasProperties = true;
				} break;
			}

			sb.Append($"new {tag.GetType().Name}");
			if (parameters.Count > 0)
			{
				sb.Append("(");
				string joinedParams = string.Join(", ", parameters);
				sb.Append(joinedParams);
				sb.Append(")");
			}

			if (hasProperties)
			{
				sb.Append(" { ");
			}
			
			if (tag is IEnumerable<NbtTag> enumerable)
			{
				var array = enumerable.ToArray();

				for (var index = 0; index < array.Length; index++)
				{
					var t = array[index];
					WriteTag(sb, t, index == array.Length - 1);
				}
			}

			if (hasProperties)
			{
				sb.Append(" }");
			}

			if (!isLast)
			{
				sb.Append(", ");
			}
		}
		
		public void WriteInventoryToFile(string fileName, ItemStacks slots)
		{
			Log.Warn($"Writing inventory to filename: {fileName}");
			FileStream file = File.OpenWrite(fileName);

			IndentedTextWriter writer = new IndentedTextWriter(new StreamWriter(file));

			writer.WriteLine("// GENERATED CODE. DON'T EDIT BY HAND");
			writer.Indent++;
			writer.Indent++;
			writer.WriteLine("public static List<Item> CreativeInventoryItems = new List<Item>()");
			writer.WriteLine("{");
			writer.Indent++;

			foreach (var entry in slots)
			{
				var slot = entry;

				NbtCompound extraData = slot.ExtraData;

				
				//var matchingBlock = BlockFactory.BlockPalette[slot.RuntimeId];
				
				var serialized = SerializeCompound(extraData);
				writer.WriteLine($"new Item(\"{slot.Name}\", {slot.Metadata}, {slot.Count}){{ RuntimeId={slot.RuntimeId}, NetworkId={slot.NetworkId}, ExtraData = {serialized} }}, /*{slot.Name}*/");
			}

			// Template
			new ItemAir
			{
				ExtraData = new NbtCompound
				{
					new NbtList("ench")
					{
						new NbtCompound
						{
							new NbtShort("id", 0),
							new NbtShort("lvl", 0)
						}
					}
				}
			};
			//var compound = new NbtCompound(string.Empty) { new NbtList("ench", new NbtCompound()) {new NbtShort("id", 0),new NbtShort("lvl", 0),}, };

			writer.Indent--;
			writer.WriteLine("};");
			writer.Indent--;
			writer.Indent--;

			writer.Flush();
			file.Close();
		}

		public string MetadataToCode(MetadataDictionary metadata)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine();

			foreach (var kvp in metadata._entries)
			{
				int idx = kvp.Key;
				MetadataEntry entry = kvp.Value;

				sb.Append($"metadata[{idx}] = new ");
				switch (entry.Identifier)
				{
					case 0:
					{
						var e = (MetadataByte) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						break;
					}
					case 1:
					{
						var e = (MetadataShort) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						break;
					}
					case 2:
					{
						var e = (MetadataInt) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						break;
					}
					case 3:
					{
						var e = (MetadataFloat) entry;
						sb.Append($"{e.GetType().Name}({e.Value.ToString(NumberFormatInfo.InvariantInfo)}f);");
						break;
					}
					case 4:
					{
						var e = (MetadataString) entry;
						sb.Append($"{e.GetType().Name}(\"{e.Value}\");");
						break;
					}
					case 5:
					{
						var e = (MetadataNbt) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						break;
					}
					case 6:
					{
						var e = (MetadataIntCoordinates) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						break;
					}
					case 7:
					{
						var e = (MetadataLong) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						if (idx == 0)
						{
							sb.Append($" // {Convert.ToString((long) e.Value, 2)}; {FlagsToString(e.Value)}");
						}
						break;
					}
					case 8:
					{
						var e = (MetadataVector3) entry;
						sb.Append($"{e.GetType().Name}({e.Value});");
						break;
					}
				}
				sb.Append($" // {(Entity.MetadataFlags) idx}");
				sb.AppendLine();
			}

			return sb.ToString();
		}

		private static string FlagsToString(long input)
		{
			BitArray bits = new BitArray(BitConverter.GetBytes(input));

			byte[] bytes = new byte[8];
			bits.CopyTo(bytes, 0);

			List<Entity.DataFlags> flags = new List<Entity.DataFlags>();
			foreach (var val in Enum.GetValues(typeof(Entity.DataFlags)))
			{
				if (bits[(int) val]) flags.Add((Entity.DataFlags) val);
			}

			StringBuilder sb = new StringBuilder();
			sb.Append(string.Join(", ", flags));
			sb.Append("; ");
			for (var i = 0; i < bits.Count; i++)
			{
				if (bits[i]) sb.Append($"{i}, ");
			}

			return sb.ToString();
		}

		public string CodeName(string name, bool firstUpper = false)
		{
			bool upperCase = firstUpper;

			var result = string.Empty;
			for (int i = 0; i < name.Length; i++)
			{
				if (name[i] == ' ' || name[i] == '_')
				{
					upperCase = true;
				}
				else
				{
					if ((i == 0 && firstUpper) || upperCase)
					{
						result += name[i].ToString().ToUpperInvariant();
						upperCase = false;
					}
					else
					{
						result += name[i];
					}
				}
			}

			result = result.Replace(@"[]", "s");
			return result;
		}

		private int _numberOfChunks = 0;

		public ConcurrentDictionary<ChunkCoordinates, ChunkColumn> Chunks { get; } = new ConcurrentDictionary<ChunkCoordinates, ChunkColumn>();
		public IndentedTextWriter _mobWriter;

		public void SendPacket(Packet packet)
		{
			Session?.SendPacket(packet);
		}

		public void SendChat(string text)
		{
			var packet = McpeText.CreateObject();
			packet.type = (byte) MessageType.Chat;
			packet.source = Username;
			packet.message = text;

			SendPacket(packet);
		}

		public void SendMcpeMovePlayer()
		{
			if (CurrentLocation == null) return;

			if (CurrentLocation.Y < 0)
				CurrentLocation.Y = 64f;

			var movePlayerPacket = McpeMovePlayer.CreateObject();
			movePlayerPacket.runtimeEntityId = EntityId;
			movePlayerPacket.position = new Vector3(CurrentLocation.X, CurrentLocation.Y, CurrentLocation.Z);
			movePlayerPacket.rotation = new Vector2(CurrentLocation.Pitch, CurrentLocation.Yaw);
			movePlayerPacket.headYaw = CurrentLocation.HeadYaw;
			movePlayerPacket.mode = McpeMovePlayer.PositionMode.Respawn;
			movePlayerPacket.onGround = false;

			SendPacket(movePlayerPacket);
		}

		public Task SendCurrentPlayerPositionAsync()
		{
			if (CurrentLocation == null) return Task.CompletedTask;

			if (CurrentLocation.Y < 0) CurrentLocation.Y = 64f;

			var movePlayerPacket = McpeMovePlayer.CreateObject();
			movePlayerPacket.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
			movePlayerPacket.runtimeEntityId = EntityId;
			movePlayerPacket.position = new Vector3(CurrentLocation.X, CurrentLocation.Y, CurrentLocation.Z);
			movePlayerPacket.rotation = new Vector2(CurrentLocation.Pitch, CurrentLocation.Yaw);
			movePlayerPacket.headYaw = CurrentLocation.HeadYaw;
			movePlayerPacket.mode = McpeMovePlayer.PositionMode.Respawn;
			movePlayerPacket.onGround = false;

			Session.SendPacket(movePlayerPacket);
			return Task.CompletedTask;
		}
	}
}

