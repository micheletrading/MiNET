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
using System.Threading;
using fNbt;
using log4net;
using MiNET.Blocks;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Client
{
	public class ClientUtils
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ClientUtils));

		private static int _waterOffsetY = 0;
		private static string _basePath = @"D:\Temp\MCPEWorldStore";

		private static object _chunkRead = new object();

		public static ChunkColumn DecodeChunkColumn(int subChunkCount, byte[] buffer, BlockPalette bedrockPalette = null, HashSet<BlockStateContainer> internalBlockPallet = null, bool blockNetworkIdsAreHashes = false)
		{
			//lock (_chunkRead)
			{
				var stream = new MemoryStream(buffer);
				{
					var defStream = new BinaryReader(stream);

					if (subChunkCount < 1)
					{
						Log.Warn("Nothing to read");
						return null;
					}

					//if (Log.IsTraceEnabled()) Log.Trace($"Reading {subChunkCount} sections");

					var chunkColumn = new ChunkColumn(false);

					for (int chunkIndex = 0; chunkIndex < subChunkCount; chunkIndex++)
					{
						int version = stream.ReadByte();
						int storageSize = stream.ReadByte();

						// Version 9 adds a signed y-index byte (world height is -64..320
						// since 1.18, so indexes run -4..19). Older ChunkColumn only has
						// 16 sections; sections outside 0..15 are parsed but not stored.
						int yIndex = chunkIndex;
						if (version >= 9)
						{
							yIndex = (sbyte) stream.ReadByte();
						}

						SubChunk subChunk = yIndex >= 0 && yIndex < 16 ? chunkColumn[yIndex] : null;

						for (int storageIndex = 0; storageIndex < storageSize; storageIndex++)
						{
							int flags = stream.ReadByte();
							bool isRuntime = (flags & 1) != 0;
							int bitsPerBlock = flags >> 1;

							// bitsPerBlock 0 is the single-value storage: no data words at
							// all, just a one-entry palette that fills the whole section.
							int blocksPerWord = 0;
							int wordsPerChunk = 0;
							if (bitsPerBlock > 0)
							{
								blocksPerWord = (int) Math.Floor(32f / bitsPerBlock);
								wordsPerChunk = (int) Math.Ceiling(4096f / blocksPerWord);
							}
							if (Log.IsTraceEnabled())
								Log.Trace($"New section {chunkIndex}, " +
										$"version={version}, " +
										$"storageSize={storageSize}, " +
										$"storageIndex={storageIndex}, " +
										$"bitsPerBlock={bitsPerBlock}, " +
										$"isRuntime={isRuntime}, " +
										$"noBlocksPerWord={blocksPerWord}, " +
										$"wordCount={wordsPerChunk}, " +
										$"");

							long jumpPos = stream.Position;
							stream.Seek(wordsPerChunk * 4, SeekOrigin.Current);

							int paletteCount = VarInt.ReadSInt32(stream);
							var palette = new int[paletteCount];
							for (int j = 0; j < paletteCount; j++)
							{
								if (!isRuntime)
								{
									var file = new NbtFile
									{
										BigEndian = false,
										UseVarInt = true
									};
									file.LoadFromStream(stream, NbtCompression.None);
									var tag = (NbtCompound) file.RootTag;

									Block block = BlockFactory.GetBlockByName(tag["name"].StringValue);
									if (block != null && block.GetType() != typeof(Block) && !(block is Air))
									{
										List<IBlockState> blockState = ReadBlockState(tag);
										block.SetState(blockState);
									}
									else
									{
										block = new Air();
									}

									palette[j] = block.GetRuntimeId();
								}
								else
								{
									int runtimeId = VarInt.ReadSInt32(stream);
									if (blockNetworkIdsAreHashes)
									{
										palette[j] = ResolveHashedBlockId(unchecked((uint) runtimeId));
									}
									else
									{
										if (bedrockPalette == null || internalBlockPallet == null) continue;

										palette[j] = GetServerRuntimeId(bedrockPalette, internalBlockPallet, runtimeId);
									}
								}
							}

							if (bitsPerBlock == 0)
							{
								// Single-value: fill the whole section with palette[0].
								if (subChunk != null && palette.Length > 0 && palette[0] >= 0)
								{
									for (int pos = 0; pos < 4096; pos++)
									{
										int x = (pos >> 8) & 0xF;
										int y = pos & 0xF;
										int z = (pos >> 4) & 0xF;
										if (storageIndex == 0) subChunk.SetBlockByRuntimeId(x, y, z, palette[0]);
										else subChunk.SetLoggedBlockByRuntimeId(x, y, z, palette[0]);
									}
								}
								continue;
							}

							long afterPos = stream.Position;
							stream.Position = jumpPos;
							int position = 0;
							for (int w = 0; w < wordsPerChunk; w++)
							{
								uint word = defStream.ReadUInt32();
								for (int block = 0; block < blocksPerWord; block++)
								{
									if (position >= 4096)
										continue;

									uint state = (uint) ((word >> ((position % blocksPerWord) * bitsPerBlock)) & ((1 << bitsPerBlock) - 1));

									int x = (position >> 8) & 0xF;
									int y = position & 0xF;
									int z = (position >> 4) & 0xF;

									if (state < palette.Length)
									{
										int runtimeId = palette[state];

										if (subChunk != null && runtimeId >= 0)
										{
											if (storageIndex == 0)
											{
												subChunk.SetBlockByRuntimeId(x, y, z, (int) runtimeId);
											}
											else
											{
												subChunk.SetLoggedBlockByRuntimeId(x, y, z, (int) runtimeId);
											}
										}
									}

									position++;
								}
							}
							stream.Position = afterPos;
						}
					}

					// TODO: The tail is 3D biome palettes per section since 1.18, then
					// border blocks and block entities. Not parsed yet; blocks above are
					// the useful part for tracing.
					if (Log.IsDebugEnabled) Log.Debug($"Skipping chunk tail: {stream.Length - stream.Position} bytes (biomes, border blocks, block entities)");
					return chunkColumn;

#pragma warning disable CS0162
					if (stream.Read(chunkColumn.biomeId, 0, 256) != 256) return chunkColumn;
					//Log.Debug($"biomeId:\n{Package.HexDump(chunk.biomeId)}");

					if (stream.Position >= stream.Length - 1) return chunkColumn;

					int borderBlock = VarInt.ReadSInt32(stream);
					if (borderBlock != 0)
					{
						Log.Warn($"??? Got borderblock with value {borderBlock}.");

						int len = (int) (stream.Length - stream.Position);
						var bytes = new byte[len];
						stream.Read(bytes, 0, len);
						Log.Warn($"Data to read for border blocks\n{Packet.HexDump(new ReadOnlyMemory<byte>(bytes))}");

						//byte[] buf = new byte[borderBlock];
						//int len = stream.Read(buf, 0, borderBlock);
						//Log.Warn($"??? Got borderblock {borderBlock}. Read {len} bytes");
						//Log.Debug($"{Packet.HexDump(buf)}");
						//for (int i = 0; i < borderBlock; i++)
						//{
						//	int x = (buf[i] & 0xf0) >> 4;
						//	int z = buf[i] & 0x0f;
						//	Log.Debug($"x={x}, z={z}");
						//}
					}

					if (stream.Position < stream.Length - 1)
					{
						while (stream.Position < stream.Length)
						{
							NbtFile file = new NbtFile()
							{
								BigEndian = false,
								UseVarInt = true
							};

							file.LoadFromStream(stream, NbtCompression.None);
							var blockEntityTag = file.RootTag;
							if (blockEntityTag.Name != "alex")
							{
								int x = blockEntityTag["x"].IntValue;
								int y = blockEntityTag["y"].IntValue;
								int z = blockEntityTag["z"].IntValue;

								chunkColumn.SetBlockEntity(new BlockCoordinates(x, y, z), (NbtCompound) file.RootTag);

								if (Log.IsTraceEnabled()) Log.Trace($"Blockentity:\n{file.RootTag}");
							}
						}
					}

					if (stream.Position < stream.Length - 1)
					{
						int len = (int) (stream.Length - stream.Position);
						var bytes = new byte[len];
						stream.Read(bytes, 0, len);
						Log.Warn($"Still have data to read\n{Packet.HexDump(new ReadOnlyMemory<byte>(bytes))}");
					}

					return chunkColumn;
#pragma warning restore CS0162
				}
			}
		}

		private static readonly System.Collections.Concurrent.ConcurrentDictionary<uint, int> HashToInternalRuntimeId = new System.Collections.Concurrent.ConcurrentDictionary<uint, int>();
		private static long _hashResolveExact;
		private static long _hashResolveDefault;
		private static long _hashResolveUnknown;
		private static int _chunksLogged;

		/// <summary>
		///     Maps a network block state hash to an internal (current embedded palette)
		///     runtime id. Falls back to the block's default state when the exact state
		///     does not exist internally, and -1 when the hash or block is unknown.
		/// </summary>
		private static int ResolveHashedBlockId(uint hash)
		{
			int resolved = HashToInternalRuntimeId.GetOrAdd(hash, h =>
			{
				if (!NetworkBlockPalette.HashToEntry.TryGetValue(h, out NetworkBlockPalette.Entry entry))
				{
					Interlocked.Increment(ref _hashResolveUnknown);
					return -1;
				}

				var container = new BlockStateContainer
				{
					Name = entry.Name,
					States = new List<IBlockState>(entry.States)
				};

				if (BlockFactory.BlockStates.TryGetValue(container, out BlockStateContainer match))
				{
					Interlocked.Increment(ref _hashResolveExact);
					return match.RuntimeId;
				}

				Block block = BlockFactory.GetBlockByName(entry.Name);
				if (block != null && !(block is Air))
				{
					Interlocked.Increment(ref _hashResolveDefault);
					return block.GetRuntimeId();
				}

				Interlocked.Increment(ref _hashResolveUnknown);
				return -1;
			});

			if (_chunksLogged < 3 && Interlocked.Increment(ref _chunksLogged) <= 3)
			{
				Log.Warn($"Hash palette resolution so far: exact={_hashResolveExact}, default-state={_hashResolveDefault}, unknown={_hashResolveUnknown}");
			}

			return resolved;
		}

		/// <summary>
		///     Fully decodes storage 0 of a subchunk payload into a 4096-entry grid of raw
		///     palette values (positional ids when the server runs non-hash mode). Cell index
		///     is the wire order (x &lt;&lt; 8) | (z &lt;&lt; 4) | y. Returns null on persisted
		///     (NBT) palettes or parse failure. Used to read BDS's canonical positional ids
		///     off the wire for the block-order extraction pipeline.
		/// </summary>
		public static int[] DecodeSubChunkGrid(byte[] data)
		{
			if (data == null || data.Length == 0) return null;

			try
			{
				var stream = new MemoryStream(data);

				int version = stream.ReadByte();
				int storageSize = stream.ReadByte();
				if (version >= 9) stream.ReadByte(); // y index
				if (storageSize < 1) return null;

				// Storage 0 only: the block layer (storage 1 is the liquid layer).
				int flags = stream.ReadByte();
				bool isRuntime = (flags & 1) != 0;
				int bitsPerBlock = flags >> 1;
				if (!isRuntime) return null;

				var indices = new int[4096];
				if (bitsPerBlock > 0)
				{
					int blocksPerWord = (int) Math.Floor(32f / bitsPerBlock);
					int wordCount = (int) Math.Ceiling(4096f / blocksPerWord);
					int mask = (1 << bitsPerBlock) - 1;
					var wordBytes = new byte[4];
					int cell = 0;
					for (int w = 0; w < wordCount; w++)
					{
						if (stream.Read(wordBytes, 0, 4) != 4) return null;
						uint word = BitConverter.ToUInt32(wordBytes, 0);
						for (int j = 0; j < blocksPerWord && cell < 4096; j++, cell++)
						{
							indices[cell] = (int) ((word >> (j * bitsPerBlock)) & mask);
						}
					}
				}

				int paletteCount = VarInt.ReadSInt32(stream);
				var palette = new int[paletteCount];
				for (int j = 0; j < paletteCount; j++) palette[j] = VarInt.ReadSInt32(stream);

				var grid = new int[4096];
				for (int i = 0; i < 4096; i++)
				{
					int idx = indices[i];
					if (idx < 0 || idx >= paletteCount) return null;
					grid[i] = palette[idx];
				}

				return grid;
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled) Log.Warn("Decoding subchunk grid", e);
				return null;
			}
		}

		/// <summary>
		///     Parses the serialized subchunk payload from a SubChunkPacket entry: version
		///     header, block storages, then block entities. Parse-only for now; feeds the
		///     hash resolution statistics.
		/// </summary>
		public static bool TryParseSubChunkPayload(byte[] data, bool blockNetworkIdsAreHashes)
		{
			if (data == null || data.Length == 0) return false;

			try
			{
				var stream = new MemoryStream(data);
				var defStream = new BinaryReader(stream);

				int version = stream.ReadByte();
				int storageSize = stream.ReadByte();
				if (version >= 9) stream.ReadByte(); // y index

				for (int storageIndex = 0; storageIndex < storageSize; storageIndex++)
				{
					int flags = stream.ReadByte();
					bool isRuntime = (flags & 1) != 0;
					int bitsPerBlock = flags >> 1;

					if (bitsPerBlock > 0)
					{
						int blocksPerWord = (int) Math.Floor(32f / bitsPerBlock);
						int wordsPerChunk = (int) Math.Ceiling(4096f / blocksPerWord);
						stream.Seek(wordsPerChunk * 4, SeekOrigin.Current);
					}

					int paletteCount = VarInt.ReadSInt32(stream);
					for (int j = 0; j < paletteCount; j++)
					{
						if (!isRuntime)
						{
							var file = new NbtFile {BigEndian = false, UseVarInt = true};
							file.LoadFromStream(stream, NbtCompression.None);
						}
						else
						{
							int value = VarInt.ReadSInt32(stream);
							if (blockNetworkIdsAreHashes) ResolveHashedBlockId(unchecked((uint) value));
						}
					}
				}

				// Trailing block entities, varint nbt until end.
				while (stream.Position < stream.Length)
				{
					var file = new NbtFile {BigEndian = false, UseVarInt = true};
					file.LoadFromStream(stream, NbtCompression.None);
				}

				return true;
			}
			catch (Exception e)
			{
				if (Log.IsDebugEnabled) Log.Warn("Parsing subchunk payload", e);
				return false;
			}
		}

		private static List<IBlockState> ReadBlockState(NbtCompound tag)
		{
			//Log.Debug($"Palette nbt:\n{tag}");

			var states = new List<IBlockState>();
			var nbtStates = (NbtCompound) tag["states"];
			foreach (NbtTag stateTag in nbtStates)
			{
				IBlockState state = stateTag.TagType switch
				{
					NbtTagType.Byte => (IBlockState) new BlockStateByte()
					{
						Name = stateTag.Name,
						Value = stateTag.ByteValue
					},
					NbtTagType.Int => new BlockStateInt()
					{
						Name = stateTag.Name,
						Value = stateTag.IntValue
					},
					NbtTagType.String => new BlockStateString()
					{
						Name = stateTag.Name,
						Value = stateTag.StringValue
					},
					_ => throw new ArgumentOutOfRangeException()
				};
				states.Add(state);
			}

			return states;
		}

		private static int GetServerRuntimeId(BlockPalette bedrockPalette, HashSet<BlockStateContainer> internalBlockPallet, int runtimeId)
		{
			if (runtimeId < 0 || runtimeId >= bedrockPalette.Count) Log.Error($"RuntimeId = {runtimeId}");

			var record = bedrockPalette[runtimeId];

			if (!internalBlockPallet.TryGetValue(record, out BlockStateContainer internalRecord))
			{
				Log.Error($"Did not find {record.Id}");
				return 0; // air
			}

			return internalRecord.RuntimeId;
		}

		private static void SetNibble4(byte[] arr, int index, byte value)
		{
			if (index % 2 == 0)
			{
				arr[index / 2] = (byte) ((value & 0x0F) | arr[index / 2]);
			}
			else
			{
				arr[index / 2] = (byte) (((value << 4) & 0xF0) | arr[index / 2]);
			}
		}

		public static void SaveLevel(LevelInfo level)
		{
			if (!Directory.Exists(_basePath))
				Directory.CreateDirectory(_basePath);

			NbtFile file = new NbtFile();
			NbtTag dataTag = file.RootTag["Data"] = new NbtCompound("Data");
			level.SaveToNbt(dataTag);
			file.SaveToFile(Path.Combine(_basePath, "level.dat"), NbtCompression.GZip);
		}

		public static void SaveChunkToAnvil(ChunkColumn chunk)
		{
			lock (_basePath)
			{
				AnvilWorldProvider.SaveChunk(chunk, _basePath);
			}
		}

		//private static NbtFile CreateNbtFromChunkColumn(ChunkColumn chunk)
		//{
		//	var nbt = new NbtFile();

		//	var levelTag = new NbtCompound("Level");
		//	var rootTag = (NbtCompound) nbt.RootTag;
		//	rootTag.Add(levelTag);

		//	levelTag.Add(new NbtInt("xPos", chunk.x));
		//	levelTag.Add(new NbtInt("zPos", chunk.z));
		//	levelTag.Add(new NbtByteArray("Biomes", chunk.biomeId));

		//	NbtList sectionsTag = new NbtList("Sections");
		//	levelTag.Add(sectionsTag);

		//	for (int i = 0; i < 8; i++)
		//	{
		//		NbtCompound sectionTag = new NbtCompound();
		//		sectionsTag.Add(sectionTag);
		//		sectionTag.Add(new NbtByte("Y", (byte) i));
		//		int sy = i * 16;

		//		byte[] blocks = new byte[4096];
		//		byte[] data = new byte[2048];
		//		byte[] blockLight = new byte[2048];
		//		byte[] skyLight = new byte[2048];

		//		for (int x = 0; x < 16; x++)
		//		{
		//			for (int z = 0; z < 16; z++)
		//			{
		//				for (int y = 0; y < 16; y++)
		//				{
		//					int yi = sy + y;
		//					if (yi < 0 || yi >= 256) continue; // ?

		//					int anvilIndex = (y + _waterOffsetY) * 16 * 16 + z * 16 + x;
		//					int blockId = chunk.GetBlockId(x, yi, z);

		//					// PE to Anvil friendly converstion
		//					if (blockId == 5) blockId = 125;
		//					else if (blockId == 158) blockId = 126;
		//					else if (blockId == 50) blockId = 75;
		//					else if (blockId == 50) blockId = 76;
		//					else if (blockId == 89) blockId = 123;
		//					else if (blockId == 89) blockId = 124;
		//					else if (blockId == 73) blockId = 152;

		//					blocks[anvilIndex] = (byte) blockId;
		//					SetNibble4(data, anvilIndex, chunk.GetMetadata(x, yi, z));
		//					SetNibble4(blockLight, anvilIndex, chunk.GetBlocklight(x, yi, z));
		//					SetNibble4(skyLight, anvilIndex, chunk.GetSkylight(x, yi, z));
		//				}
		//			}
		//		}

		//		sectionTag.Add(new NbtByteArray("Blocks", blocks));
		//		sectionTag.Add(new NbtByteArray("Data", data));
		//		sectionTag.Add(new NbtByteArray("BlockLight", blockLight));
		//		sectionTag.Add(new NbtByteArray("SkyLight", skyLight));
		//	}

		//	levelTag.Add(new NbtList("Entities", NbtTagType.Compound));
		//	levelTag.Add(new NbtList("TileEntities", NbtTagType.Compound));
		//	levelTag.Add(new NbtList("TileTicks", NbtTagType.Compound));

		//	return nbt;
		//}
	}
}
