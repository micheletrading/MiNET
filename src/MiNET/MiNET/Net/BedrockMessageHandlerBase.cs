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
using System.Buffers;
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

		// There is no compression state here on purpose. Every batch this class builds carries a
		// compressor id byte (0x00=zlib, 0xff=none, chosen by size), and every batch it reads has its
		// id read off the wire. The one exchange that predates the id byte, NetworkSettings one way
		// and RequestNetworkSettings the other, is pre-wrapped raw at its call site. A flag for it
		// only ever said what the bytes already say, and said it a batch too early.

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
				pending.SetPayload(Compression.CompressPacketsForWrapper(sendInBatch));
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
					wrapper.ForceClear = true;
					wrapper.SetPayload(Compression.CompressPacketsForWrapper(new List<Packet> {packet}));
					wrapper.EncodeAsMemory(); // prepare
					packet.PutPool();
					sendList.Add(wrapper);
					continue;
				}

				if (packet is McpeWrapper)
				{
					FlushBatch();

					sendList.Add(packet);
					continue;
				}

				if (!packet.IsMcpe)
				{
					FlushBatch();

					sendList.Add(packet);
					continue;
				}


				// Null is a plugin suppressing the packet, and it has to be dropped here rather than
				// batched: a null reaching Compression.CompressPacketsForWrapper throws, and a throw
				// there takes the whole wrapper with it, so one suppressed packet would silently
				// cost every packet riding beside it. The original is already pooled by
				// OnSendCustomPacket when it hands back anything other than what it was given.
				Packet outgoing = OnSendCustomPacket(packet);
				if (outgoing == null) continue;

				sendInBatch.Add(outgoing);
			}

			FlushBatch();

			return sendList;
		}

		public Packet HandleOrderedSend(Packet packet)
		{
			if (!packet.ForceClear && CryptoContext != null && CryptoContext.UseEncryption && packet is McpeWrapper wrapper)
			{
				var encryptedWrapper = McpeWrapper.CreateObject();
				encryptedWrapper.payload = CryptoUtils.Encrypt(wrapper.payload, CryptoContext);
				encryptedWrapper.Encode();

				return encryptedWrapper;
			}

			return packet;
		}

		/// <summary>
		///     The interface entry point, for a transport that hands over a batch payload and has no
		///     interest in how it is split. The NetherNet session does not use it: it drives
		///     <see cref="DecodeBatch" /> and <see cref="HandleDecoded" /> itself so it can dispatch a
		///     verified handler inline instead of queueing it.
		/// </summary>
		public void HandlePayload(ReadOnlyMemory<byte> payload)
		{
			foreach (Packet msg in DecodeBatch(payload))
			{
				HandleDecoded(msg);
			}
		}

		public void HandlePacket(Packet message)
		{
			if (message == null) throw new NullReferenceException();

			if (message is McpeWrapper wrapper)
			{
				try
				{
					foreach (Packet msg in DecodeBatch(wrapper.payload))
					{
						HandleDecoded(msg);
					}
				}
				finally
				{
					wrapper.PutPool();
				}

				return;
			}

			HandleNonWrapper(message);
		}

		/// <summary>
		///     The transport-thread half of batch handling: decrypt, decompress and decode a batch
		///     payload into packet objects, consuming it synchronously (a transport may hand in a
		///     borrowed view valid only for this call). Takes the payload rather than a wrapper: on
		///     receive there is nothing to wrap, and building one per batch only to read a field off
		///     it costs an object and a copy for nothing.
		///     <see cref="HandleDecoded" /> is the other half.
		/// </summary>
		public List<Packet> DecodeBatch(ReadOnlyMemory<byte> payload)
		{
			Volatile.Write(ref _lastIncomingTicks, Environment.TickCount64);

			var messages = new List<Packet>();

			// Rented here, handed back here, on every path. That is only sound because no packet
			// leaves this method pointing into it: anything that has to keep bytes copies them into
			// memory it owns while parsing. Nothing outside this method can hold it, so there is no
			// count to get wrong and no second place to look.
			byte[] decompressed = null;
			int decompressedLength = 0;

			try
			{
				if (IgnoreIncoming) return messages;

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
				// REFCT: this reader and the one over the batch below are the second largest allocation
				// on the inbound path, one small object each per batch. Both exist only because
				// DeflateStream and VarInt.ReadUInt32 take a Stream. A span-based varint read deletes the
				// batch one outright; a struct reader would delete both but stops being a Stream. Not a
				// shared instance: it carries a mutable Position.
				var stream = new MemoryStreamReader(payload);
				try
				{
					{
						// This buffer is GC-owned on purpose. The decoded packets slice it and outlive
						// this call, so a rented one would have to be handed back by whoever releases
						// the LAST packet of the batch, which is a hand-back spread over N objects and
						// no single place that can be shown to be correct. Handing a live buffer back
						// to a pool the transport also rents from corrupts a payload, not a packet.
						// The compressor id is on the wire, so read it instead of inferring it from
						// CompressionEnabled, which flips when we QUEUE NetworkSettings rather than when
						// the client starts prefixing. One batch on the wrong side of that eats the
						// first length varint as a compressor id, and it surfaces as the decompressor
						// complaining about an unsupported method on a batch nobody compressed.
						// There is no id byte at all until the exchange completes, so a first byte that
						// is none of the three is a payload starting straight at its first length varint.
						switch (payload.Length > 0 ? payload.Span[0] : -1)
						{
							case 0x00:
								stream.Position = 1;
								using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress, false))
								{
									decompressed = ReadAll(deflateStream, out decompressedLength);
								}
								break;
							case 0x01:
								throw new InvalidDataException("Snappy compressed batch, which is not implemented");
							case 0xff:
								stream.Position = 1;
								decompressed = ReadAll(stream, out decompressedLength);
								break;
							default:
								decompressed = ReadAll(stream, out decompressedLength);
								break;
						}

						var batch = new ReadOnlyMemory<byte>(decompressed, 0, decompressedLength);
						var s = new MemoryStreamReader(batch);

						int count = 0;
						// Get actual packet out of bytes
						while (s.Position < s.Length)
						{
							count++;

							uint len = VarInt.ReadUInt32(s);
							long pos = s.Position;

							if (pos + len > decompressedLength)
							{
								throw new InvalidDataException(
									$"Frame {count} says {len} bytes at offset {pos}, but the batch is {decompressedLength} bytes (payload was {payload.Length})");
							}

							ReadOnlyMemory<byte> internalBuffer = batch.Slice((int) s.Position, (int) len);
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

							// The raw frame is traced here for the same reason it is dumped here: this is
							// where it exists. A packet keeps no copy of the bytes it was parsed from.
							PacketTracing.TraceReceiveFrame(Log, id, internalBuffer);

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
					// At Error, not Debug: this kills the session, so the one line that says which
					// session and how big the batch was is worth having without turning tracing on.
					// The bytes themselves stay behind Debug; they are only readable in a quiet log.
					Log.Error($"Error parsing bedrock batch of {payload.Length} bytes for {_session.Username ?? "unknown"}", e);
					if (Log.IsDebugEnabled) Log.Debug($"The batch was\n{Packet.HexDump(payload)}");
					throw;
				}
			}
			finally
			{
				if (decompressed != null) ArrayPool<byte>.Shared.Return(decompressed);
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

				// Both dispatch paths end here, inline and queued alike, so this is the one place a
				// received packet can be returned. Nothing did until now: the wrapper went back to
				// the pool and the packets inside it went to the GC, which at 20Hz per player is
				// where the allocation rate comes from.
				//
				// The contract this assumes is that a handler is done with the packet when it
				// returns. One that wants to keep it has to take a copy, because the instance is
				// reused the moment this runs.
				msg.PutPool();
			}
		}

		/// <summary>
		///     Reads a stream to its end into a rented array, doubling as it goes. Deflate carries no
		///     uncompressed length, so the size cannot be known up front and the copy is unavoidable;
		///     what this avoids is the copy landing in a fresh MemoryStream every time. The array is
		///     the caller's to hand back, and is longer than the byte count reported.
		/// </summary>

		private static byte[] ReadAll(Stream source, out int length)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(4 * 1024);
			length = 0;

			while (true)
			{
				if (length == buffer.Length)
				{
					byte[] bigger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
					Buffer.BlockCopy(buffer, 0, bigger, 0, length);
					ArrayPool<byte>.Shared.Return(buffer);
					buffer = bigger;
				}

				int read = source.Read(buffer, length, buffer.Length - length);
				if (read == 0) return buffer;

				length += read;
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
