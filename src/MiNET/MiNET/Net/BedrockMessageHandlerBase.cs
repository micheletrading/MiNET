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
using System.IO.Compression;
using System.Threading;
using log4net;
using MiNET.Net.RakNet;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.IO;

namespace MiNET.Net
{
	public abstract class BedrockMessageHandlerBase : ICustomMessageHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BedrockMessageHandlerBase));

		// Wire-content tracing: when MINET_PACKET_DUMP is set to a directory, every received
		// packet's raw payload is written there as <seq>-<name>.bin. Used to capture what BDS
		// sends our client vs what MiNET sends the same client, and diff the two byte-for-byte.
		private static readonly string PacketDumpDir = Environment.GetEnvironmentVariable("MINET_PACKET_DUMP");
		private static int _packetDumpSeq;

		private protected readonly RakSession _session;

		public CryptoContext CryptoContext { get; set; }

		// Compression is off until the NetworkSettings exchange completes, then every wrapper
		// payload carries a leading compressor id byte (0x00=zlib, 0x01=snappy, 0xff=none).
		public bool CompressionEnabled { get; set; }
		public ushort CompressionThreshold { get; set; } = 1;

		protected BedrockMessageHandlerBase(RakSession session)
		{
			_session = session;
		}

		public abstract void Connected();
		public abstract void Disconnect(string reason, bool sendDisconnect = true);

		public List<Packet> PrepareSend(List<Packet> packetsToSend)
		{
			var sendList = new List<Packet>();
			var sendInBatch = new List<Packet>();

			foreach (Packet packet in packetsToSend)
			{
				// We must send forced clear messages in single message batch because
				// we can't mix them with un-encrypted messages for obvious reasons.
				// If need be, we could put these in a batch of it's own, but too rare 
				// to bother.
				if (packet.ForceClear)
				{
					var wrapper = McpeWrapper.CreateObject();
					wrapper.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
					wrapper.ForceClear = true;
					wrapper.payload = Compression.CompressPacketsForWrapper(new List<Packet> {packet}, CompressionEnabled, CompressionThreshold);
					wrapper.Encode(); // prepare
					packet.PutPool();
					sendList.Add(wrapper);
					continue;
				}

				if (packet is McpeWrapper)
				{
					packet.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
					sendList.Add(packet);
					continue;
				}

				if (!packet.IsMcpe)
				{
					packet.ReliabilityHeader.Reliability = packet.ReliabilityHeader.Reliability != Reliability.Undefined ? packet.ReliabilityHeader.Reliability : Reliability.Reliable;
					sendList.Add(packet);
					continue;
				}

				packet.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;

				sendInBatch.Add(OnSendCustomPacket(packet));
			}

			if (sendInBatch.Count > 0)
			{
				var batch = McpeWrapper.CreateObject();
				batch.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;
				batch.payload = Compression.CompressPacketsForWrapper(sendInBatch, CompressionEnabled, CompressionThreshold);
				batch.Encode(); // prepare
				sendList.Add(batch);
			}

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
				var messages = new List<Packet>();

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
							try
							{
								//if (Log.IsDebugEnabled)
								//	Log.Debug($"0x{internalBuffer[0]:x2}\n{Packet.HexDump(internalBuffer)}");

								// Packet ids are varints and can exceed 255 in modern protocols; the factory
								// and UnknownPacket now carry the full id instead of truncating to a byte.
								Packet parsed = PacketFactory.Create(id, internalBuffer, "mcpe");
								if (parsed == null && Log.IsDebugEnabled) Log.Debug($"Unknown packet with id {id}");

								if (PacketDumpDir != null)
								{
									int seq = Interlocked.Increment(ref _packetDumpSeq);
									string name = parsed?.GetType().Name ?? $"Unknown_{id}";
									Directory.CreateDirectory(PacketDumpDir);
									File.WriteAllBytes(Path.Combine(PacketDumpDir, $"{seq:D4}-{name}.bin"), internalBuffer.ToArray());
								}

								messages.Add(parsed ?? new UnknownPacket(id, internalBuffer));
							}
							catch (Exception e)
							{
								if (Log.IsDebugEnabled) Log.Warn($"Error parsing bedrock message #{count} id={id}\n{Packet.HexDump(internalBuffer)}", e);
								// Packets are length-framed, so realign and keep processing the
								// rest of the batch instead of dropping it.
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
					// Temp fix for performance, take 1.
					//var interact = msg as McpeInteract;
					//if (interact?.actionId == 4 && interact.targetRuntimeEntityId == 0) continue;

					msg.ReliabilityHeader = new ReliabilityHeader()
					{
						Reliability = wrapper.ReliabilityHeader.Reliability,
						ReliableMessageNumber = wrapper.ReliabilityHeader.ReliableMessageNumber,
						OrderingChannel = wrapper.ReliabilityHeader.OrderingChannel,
						OrderingIndex = wrapper.ReliabilityHeader.OrderingIndex,
					};

					RakOfflineHandler.TraceReceive(Log, msg);
					try
					{
						HandleCustomPacket(msg);
					}
					catch (Exception e)
					{
						Log.Warn($"Bedrock message handler error", e);
					}
				}

				wrapper.PutPool();
			}
			else if (message is UnknownPacket unknownPacket)
			{
				if (Log.IsDebugEnabled) Log.Warn($"Received unknown packet 0x{unknownPacket.Id:X2}\n{Packet.HexDump(unknownPacket.Message)}");

				unknownPacket.PutPool();
			}
			else
			{
				Log.Error($"Unhandled packet: {message.GetType().Name} 0x{message.Id:X2} for user: {_session.Username}, IP {_session.EndPoint.Address}");
				if (Log.IsDebugEnabled) Log.Warn($"Unknown packet 0x{message.Id:X2}\n{Packet.HexDump(message.Bytes)}");
			}
		}

		public abstract Packet OnSendCustomPacket(Packet message);

		public abstract void HandleCustomPacket(Packet message);
	}
}
