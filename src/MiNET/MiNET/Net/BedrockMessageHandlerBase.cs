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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using log4net;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.Diagnostics;
using MiNET.Utils.IO;

namespace MiNET.Net
{
	public abstract class BedrockMessageHandlerBase : ICustomMessageHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BedrockMessageHandlerBase));

		// Wire-content tracing: when MINET_PACKET_DUMP is set to a directory, every received
		// packet's raw payload is written there as <seq>-<name>.bin. Used to capture what BDS
		// sends our client vs what MiNET sends the same client, and diff the two byte-for-byte.
		// Empty counts as unset: an exported-but-blank variable would otherwise reach
		// Directory.CreateDirectory and throw on every packet.
		private static readonly string PacketDumpDir =
			Environment.GetEnvironmentVariable("MINET_PACKET_DUMP") is {Length: > 0} dir ? dir : null;
		private static int _packetDumpSeq;

		private protected readonly INetworkHandler _session;

		public CryptoContext CryptoContext { get; set; }

		// Compression is off until the NetworkSettings exchange completes, then every wrapper
		// payload carries a leading compressor id byte (0x00=zlib, 0x01=snappy, 0xff=none).
		public bool CompressionEnabled { get; set; }
		public ushort CompressionThreshold { get; set; } = 1;

		/// <summary>
		///     Packet ids to skip without decoding when they arrive in a batch. Set by the emulator
		///     client for payloads a bot has no use for (SubChunk block data); null for everyone else.
		/// </summary>
		public HashSet<int> DropPacketIds { get; set; }

		/// <summary>
		///     When set, incoming batches are dropped whole: no decrypt, no decompress, no decode.
		///     A spawned emulator bot flips this on, because past spawn it only receives, and the
		///     acknowledgement happens below this layer in the transport. Transport-level teardown
		///     is unaffected, so the bot still notices being dropped.
		/// </summary>
		public bool IgnoreIncoming { get; set; }

		private long _lastIncomingTicks = Environment.TickCount64;

		/// <summary>
		///     How long ago the last batch arrived, counted even for batches IgnoreIncoming drops.
		///     Lets an emulator bot wait out the post-spawn chunk flood before it starts walking.
		/// </summary>
		public long MillisSinceLastIncoming => Environment.TickCount64 - Volatile.Read(ref _lastIncomingTicks);

		protected BedrockMessageHandlerBase(INetworkHandler session)
		{
			_session = session;
		}

		public abstract void Connected();
		public abstract void Disconnect(string reason, bool sendDisconnect = true);

		public List<Packet> PrepareSend(List<Packet> packetsToSend)
		{
			var sendList = new List<Packet>();
			var sendInBatch = new List<Packet>();

			// The batch is one packet built from many, so it can only be added once the run of
			// ordinary packets ends. Anything that goes straight to sendList has to close that run
			// first or it overtakes packets queued before it. Pre-encoded wrappers are the reason
			// this matters: chunks, player lists and crafting data are packed off the send path
			// and handed over finished, but they are queued in order with everything else and the
			// client holds them to it. A roster that overtakes StartGame is silently dropped.
			void FlushBatch()
			{
				if (sendInBatch.Count == 0) return;

				var pending = McpeWrapper.CreateObject();
				pending.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
				pending.SetPayload(CompressionEnabled
					? Compression.CompressPacketsForWrapper(sendInBatch)
					: Compression.PackPacketsForWrapper(sendInBatch));
				pending.EncodeAsMemory(); // prepare
				sendList.Add(pending);
				sendInBatch.Clear();
			}

			foreach (Packet packet in packetsToSend)
			{
				// We must send forced clear messages in single message batch because
				// we can't mix them with un-encrypted messages for obvious reasons.
				// If need be, we could put these in a batch of it's own, but too rare 
				// to bother.
				if (packet.ForceClear)
				{
					FlushBatch();

					var wrapper = McpeWrapper.CreateObject();
					wrapper.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
					wrapper.ForceClear = true;
					wrapper.SetPayload(CompressionEnabled
						? Compression.CompressPacketsForWrapper(new List<Packet> {packet})
						: Compression.PackPacketsForWrapper(new List<Packet> {packet}));
					wrapper.EncodeAsMemory(); // prepare
					packet.PutPool();
					sendList.Add(wrapper);
					continue;
				}

				if (packet is McpeWrapper)
				{
					FlushBatch();

					packet.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
					sendList.Add(packet);
					continue;
				}

				if (!packet.IsMcpe)
				{
					FlushBatch();

					packet.ReliabilityHeader.Reliability = packet.ReliabilityHeader.Reliability != Reliability.Undefined ? packet.ReliabilityHeader.Reliability : Reliability.Reliable;
					sendList.Add(packet);
					continue;
				}

				packet.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;

				sendInBatch.Add(OnSendCustomPacket(packet));
			}

			FlushBatch();

			return sendList;
		}

		public Packet HandleOrderedSend(Packet packet)
		{
			if (!packet.ForceClear && CryptoContext != null && CryptoContext.UseEncryption && packet is McpeWrapper wrapper)
			{
				var encryptedWrapper = McpeWrapper.CreateObject();
				encryptedWrapper.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
				encryptedWrapper.payload = CryptoUtils.Encrypt(wrapper.payload, CryptoContext);
				encryptedWrapper.Encode();

				return encryptedWrapper;
			}

			return packet;
		}

		public void HandlePacket(Packet message)
		{
			if (message == null) throw new NullReferenceException();

			if (message is McpeWrapper wrapper)
			{
				foreach (Packet msg in DecodeBatch(wrapper))
				{
					HandleDecoded(msg);
				}

				return;
			}

			HandleNonWrapper(message);
		}

		/// <summary>
		///     The transport-thread half of batch handling: decrypt, decompress and decode the wrapper
		///     into packet objects, consuming the wrapper's payload synchronously (a transport may hand
		///     in a borrowed view valid only for this call). The returned packets reference only the
		///     decompression buffer, plain GC-owned memory, so they are safe to hand to another thread;
		///     <see cref="HandleDecoded" /> is the other half. The wrapper is returned to the pool here.
		/// </summary>
		public List<Packet> DecodeBatch(McpeWrapper wrapper)
		{
			Volatile.Write(ref _lastIncomingTicks, Environment.TickCount64);

			var messages = new List<Packet>();

			{
				if (IgnoreIncoming)
				{
					wrapper.PutPool();
					return messages;
				}

				// Get bytes to process
				ReadOnlyMemory<byte> payload = wrapper.payload;

				// Decrypt bytes

				if (CryptoContext != null && CryptoContext.UseEncryption)
				{
					// This call copies the entire buffer, but what can we do? It is kind of compensated by not
					// creating a new buffer when parsing the packet (only a mem-slice)
					payload = CryptoUtils.Decrypt(payload, CryptoContext);
				}

				// Decompress bytes

				//var stream = new MemoryStreamReader(payload.Slice(0, payload.Length - 4)); // slice away adler
				//if (stream.ReadByte() != 0x78)
				//{
				//	if (Log.IsDebugEnabled) Log.Error($"Incorrect ZLib header. Expected 0x78 0x9C 0x{wrapper.Id:X2}\n{Packet.HexDump(wrapper.payload)}");
				//	if (Log.IsDebugEnabled) Log.Error($"Incorrect ZLib header. Decrypted 0x{wrapper.Id:X2}\n{Packet.HexDump(payload)}");
				//	throw new InvalidDataException("Incorrect ZLib header. Expected 0x78 0x9C");
				//}
				//stream.ReadByte();
				var stream = new MemoryStreamReader(payload);
				try
				{
					{
						using var s = new MemoryStream();
						if (CompressionEnabled)
						{
							int compressorId = stream.ReadByte();
							switch (compressorId)
							{
								case 0x00:
									using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress, false))
									{
										deflateStream.CopyTo(s);
									}
									break;
								case 0xff:
									stream.CopyTo(s);
									break;
								default:
									throw new InvalidDataException($"Unsupported compressor id 0x{compressorId:x2}");
							}
						}
						else
						{
							stream.CopyTo(s);
						}
						s.Position = 0;

						int count = 0;
						// Get actual packet out of bytes
						while (s.Position < s.Length)
						{
							count++;

							uint len = VarInt.ReadUInt32(s);
							long pos = s.Position;
							ReadOnlyMemory<byte> internalBuffer = s.GetBuffer().AsMemory((int) s.Position, (int) len);
							int id = VarInt.ReadInt32(s);

							// Frames are length-prefixed, so a packet can be skipped without decoding it.
							// The emulator fleet drops SubChunk responses here: decoding one materializes
							// a byte array per subchunk entry, which at fleet scale is the bulk of the
							// bot process's allocation, and a bot has no use for block data. The server
							// still does its full send-side work.
							if (DropPacketIds != null && DropPacketIds.Contains(id))
							{
								s.Position = pos + len;
								continue;
							}

							// Dumped BEFORE decoding, so a frame that throws is still on disk. Those are
							// the ones worth having: a packet we cannot parse is the one that changed.
							if (PacketDumpDir != null)
							{
								int seq = Interlocked.Increment(ref _packetDumpSeq);
								Directory.CreateDirectory(PacketDumpDir);
								File.WriteAllBytes(Path.Combine(PacketDumpDir, $"{seq:D4}-id{id}.bin"), internalBuffer.ToArray());
							}

							try
							{
								// Packet ids are varints and can exceed 255 in modern protocols; the factory
								// and UnknownPacket now carry the full id instead of truncating to a byte.
								Packet parsed = PacketFactory.Create(id, internalBuffer, "mcpe");
								if (parsed == null && Log.IsDebugEnabled) Log.Debug($"Unknown packet with id {id}");

								messages.Add(parsed ?? new UnknownPacket(id, internalBuffer));
							}
							catch (Exception e)
							{
								if (Log.IsDebugEnabled) Log.Warn($"Error parsing bedrock message #{count} id={id}\n{Packet.HexDump(internalBuffer)}", e);

								// Packets are length-framed, so realign and keep processing the rest
								// of the batch instead of dropping it. The frame itself is kept as an
								// UnknownPacket rather than discarded: a handler that forwards raw
								// frames (MiNET.Tunnel) must not lose exactly the packets whose shape
								// changed, which are the ones worth capturing.
								messages.Add(new UnknownPacket(id, internalBuffer));
							}

							s.Position = pos + len;
						}

						if (s.Length > s.Position) throw new Exception("Have more data");
					}
				}
				catch (Exception e)
				{
					if (Log.IsDebugEnabled) Log.Warn($"Error parsing bedrock message \n{Packet.HexDump(payload)}", e);
					throw;
				}

				foreach (Packet msg in messages)
				{
					msg.ReliabilityHeader = new ReliabilityHeader()
					{
						Reliability = wrapper.ReliabilityHeader.Reliability,
						ReliableMessageNumber = wrapper.ReliabilityHeader.ReliableMessageNumber,
						OrderingChannel = wrapper.ReliabilityHeader.OrderingChannel,
						OrderingIndex = wrapper.ReliabilityHeader.OrderingIndex,
					};
				}

				wrapper.PutPool();
			}

			return messages;
		}

		/// <summary>
		///     Game-logic handling of one packet <see cref="DecodeBatch" /> produced. Runs wherever
		///     the transport wants game code to run; the transport calls it straight after decoding
		///     on the session's own per-connection receive thread.
		/// </summary>
		public void HandleDecoded(Packet msg)
		{
			PacketTracing.TraceReceive(Log, msg);

			// The one seam both dispatch paths cross, so this measures the inline path and the queued
			// one alike. Timing is always taken (two timestamp reads); nothing is recorded unless the
			// handler breaches the threshold, so the fast case stays off the histogram machinery.
			// This is the enforcement arm of the dispatch contract - a verified handler running inline
			// sits ahead of that packet's own SACK, so a slow one delays the whole association.
			Type packetType = msg.GetType();
			long startedAt = Stopwatch.GetTimestamp();

			try
			{
				HandleCustomPacket(msg);
			}
			catch (Exception e)
			{
				Log.Warn($"Bedrock message handler error", e);
			}
			finally
			{
				EngineMetrics.RecordHandler(packetType, _session.Username, startedAt);
			}
		}

		private void HandleNonWrapper(Packet message)
		{
			Volatile.Write(ref _lastIncomingTicks, Environment.TickCount64);

			if (message is UnknownPacket unknownPacket)
			{
				if (Log.IsDebugEnabled) Log.Warn($"Received unknown packet 0x{unknownPacket.Id:X2}\n{Packet.HexDump(unknownPacket.Message)}");

				unknownPacket.PutPool();
			}
			else
			{
				Log.Error($"Unhandled packet: {message.GetType().Name} 0x{message.Id:X2} for user: {_session.Username}, IP {_session.GetClientEndPoint().Address}");
				if (Log.IsDebugEnabled) Log.Warn($"Unknown packet 0x{message.Id:X2}\n{Packet.HexDump(message.Bytes)}");
			}
		}

		/// <summary>The object whose HandleMcpe* methods ultimately run a packet (the login handler, then the player), for the direct-dispatch label lookup; null when the transport should always queue.</summary>
		protected virtual object HandlerTarget => null;

		/// <summary>Whether a plugin [PacketHandler] interceptor would run for this packet type; interceptors are reflection-invoked and invisible to verification, so they force the queue.</summary>
		protected virtual bool HasPluginInterceptor(Type packetType) => false;

		// Per concrete handler type: packet type -> "may dispatch inline". Static: the labels and
		// the handler type set are fixed after startup, so every session shares one cache.
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type HandlerType, Type PacketType), bool> InlineDispatchCache = new();

		/// <summary>
		///     Whether <paramref name="packet" /> may skip the dispatch queue and run its handler
		///     directly on the calling (transport) thread: the implementing handler method carries
		///     the startup scan's verified label and no plugin interceptor exists for the type.
		///     The caller still owns ordering: direct dispatch is only valid when nothing for this
		///     session is queued ahead.
		///     <para>
		///     The static label is the NECESSARY condition, never the sufficient one. It proves a
		///     handler is lock-free and touches no IO; it cannot prove the handler is fast, and a
		///     verified handler runs ahead of its own packet's SACK, so slow is as harmful here as
		///     blocking. Measured duration decides the rest: a type <see cref="EngineMetrics.IsDemoted" />
		///     has seen breach the handler threshold is refused the inline path from then on, whatever
		///     the scan said about it.
		///     </para>
		/// </summary>
		public bool CanDispatchInline(Packet packet)
		{
			object target = HandlerTarget;
			if (target == null) return false;

			// Ahead of the cache, not inside it: demotion happens while the process runs, and a cached
			// "true" from before the breach must not outlive it.
			if (EngineMetrics.IsDemoted(packet.GetType())) return false;

			(Type, Type) key = (target.GetType(), packet.GetType());
			if (InlineDispatchCache.TryGetValue(key, out bool cached)) return cached;

			// Generated convention: packet class McpeX is handled by HandleMcpeX.
			string methodName = "Handle" + key.Item2.Name;
			bool inline = HandlerVerification.IsVerified(key.Item1, methodName) && !HasPluginInterceptor(key.Item2);
			InlineDispatchCache[key] = inline;
			return inline;
		}

		public abstract Packet OnSendCustomPacket(Packet message);

		public abstract void HandleCustomPacket(Packet message);
	}
}
