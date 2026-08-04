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
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using fNbt;
using log4net;
using MiNET.Blocks;
using MiNET.LevelDB;
using MiNET.Utils;
using MiNET.Utils.IO;
using MiNET.Utils.Vectors;

namespace MiNET.Worlds
{
	public class LevelDbProvider : IWorldProvider, ICachingWorldProvider, ICloneable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(LevelDbProvider));

		private ConcurrentDictionary<ChunkCoordinates, ChunkColumn> _chunkCache = new ConcurrentDictionary<ChunkCoordinates, ChunkColumn>();
		public Database Db { get; private set; }

		public string BasePath { get; private set; }
		public LevelInfoBedrock LevelInfo { get; private set; }
		public bool IsCaching { get; } = true;
		public bool Locked { get; set; } = false;
		public IWorldGenerator MissingChunkProvider { get; set; }
		public Dimension Dimension { get; set; } = Dimension.Overworld;

		public LevelDbProvider(Database db = null)
		{
			Db = db;
		}
		
		public LevelDbProvider(string basePath)
		{
			BasePath = basePath;
		}

		public void Initialize()
		{
			BasePath ??= Config.GetProperty("LevelDBWorldFolder", "World").Trim();

			var directory = new DirectoryInfo(Path.Combine(BasePath, "db"));

			var levelFileName = Path.Combine(BasePath, "level.dat");
			Log.Debug($"Loading level.dat from {levelFileName}");
			if (File.Exists(levelFileName))
			{
				var file = new NbtFile
				{
					BigEndian = false,
					UseVarInt = false
				};

				using FileStream stream = File.OpenRead(levelFileName);
				stream.Seek(8, SeekOrigin.Begin);
				file.LoadFromStream(stream, NbtCompression.None);
				Log.Debug($"Level DAT\n{file.RootTag}");
				LevelInfo = file.RootTag.Deserialize<LevelInfoBedrock>();
			}
			else
			{
				Log.Warn($"No level.dat found at {levelFileName}. Creating empty.");
				LevelInfo = new LevelInfoBedrock();
			}

			// We must reuse the same DB for all providers (dimensions) in LevelDB.
			if (Db == null)
			{
				var db = new Database(directory, true);
				db.Open();
				Db = db;

				directory.Refresh(); // refresh create state if this dir didn't exist

				// Shutdown hook. Must use to flush in memory log of LevelDB.
				AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
				{
					SaveChunks();
					Log.Warn("Closing LevelDB");
					Db.Close();
				};
			}

			MissingChunkProvider?.Initialize(this);
		}

		public ChunkColumn GenerateChunkColumn(ChunkCoordinates chunkCoordinates, bool cacheOnly = false)
		{
			if (Locked || cacheOnly)
			{
				_chunkCache.TryGetValue(chunkCoordinates, out ChunkColumn chunk);
				return chunk;
			}

			if (_chunkCache.TryGetValue(chunkCoordinates, out ChunkColumn value))
			{
				if (value == null) _chunkCache.TryRemove(chunkCoordinates, out value);
				if (value != null) return value;
			}

			// Warning: The following code MAY execute the GetChunk 2 times for the same coordinate
			// if called in rapid succession. However, for the scenario of the provider, this is highly unlikely.
			return _chunkCache.GetOrAdd(chunkCoordinates, coordinates => GetChunk(coordinates, MissingChunkProvider));
		}

		// On-disk chunk format, read off a live BDS 1.26.34 world and cross-checked against
		// PocketMine-MP's LevelDB implementation. The pre-1.16.100 0x76 version key and the
		// pre-1.18 0x2d biome record are absent from current worlds; writing them makes Bedrock
		// run the chunk through its upgrade path, which rewrites block states.
		private const byte KeyHeightAndBiomes3D = 0x2b;
		private const byte KeyVersion = 0x2c;
		private const byte KeyHeightAndBiomes2D = 0x2d; // pre-1.18, read-only for old worlds
		private const byte KeySubChunk = 0x2f;
		private const byte KeyBlockEntity = 0x31;
		private const byte KeyFinalizedState = 0x36;
		private const byte KeyVersionLegacy = 0x76; // pre-1.16.100, read-only for old worlds

		private const byte ChunkVersion = 42;
		private const byte SubChunkVersion = 9;
		private const int FinalizedStateDone = 2;

		// ChunkColumn index 0 is world section -4, the bottom of the -64..320 range.
		private const int SubChunkIndexOffset = 4;

		public ChunkColumn GetChunk(ChunkCoordinates coordinates, IWorldGenerator generator)
		{
			var sw = Stopwatch.StartNew();
			sw.Stop();

			byte[] index = Combine(BitConverter.GetBytes(coordinates.X), BitConverter.GetBytes(coordinates.Z));
			if (Dimension == Dimension.Nether)
			{
				index = Combine(index, BitConverter.GetBytes(1));
			}

			sw.Start();
			// 1.16.100 onwards the version lives under 0x2c; older worlds still carry 0x76.
			byte[] version = Db.Get(Combine(index, KeyVersion)) ?? Db.Get(Combine(index, KeyVersionLegacy));
			sw.Stop();

			ChunkColumn chunkColumn = null;
			if (version != null && version.First() >= 10)
			{
				chunkColumn = new ChunkColumn
				{
					X = coordinates.X,
					Z = coordinates.Z
				};

				byte[] chunkDataKey = Combine(index, new byte[] {KeySubChunk, 0});
				for (int i = 0; i < ChunkColumn.WorldHeight / 16; i++)
				{
					// Section indices are signed since 1.18: -4 is the bottom, stored as 0xfc.
					chunkDataKey[^1] = unchecked((byte) (sbyte) (i - SubChunkIndexOffset));
					sw.Start();
					byte[] sectionBytes = Db.Get(chunkDataKey);
					sw.Stop();

					if (sectionBytes == null)
					{
						chunkColumn[i]?.PutPool();
						chunkColumn[i] = null;
						continue;
					}

					ParseSection(chunkColumn[i], sectionBytes);
				}

				// Biomes: 3D since 1.18, with the flat record kept for older worlds.
				sw.Start();
				byte[] biome3DBytes = Db.Get(Combine(index, KeyHeightAndBiomes3D));
				byte[] flatDataBytes = biome3DBytes == null ? Db.Get(Combine(index, KeyHeightAndBiomes2D)) : null;
				sw.Stop();
				if (biome3DBytes != null)
				{
					Buffer.BlockCopy(biome3DBytes.AsSpan().Slice(0, 512).ToArray(), 0, chunkColumn.height, 0, 512);
					ParseBiomes3D(chunkColumn, biome3DBytes.AsSpan().Slice(512).ToArray());
				}
				else if (flatDataBytes != null)
				{
					Buffer.BlockCopy(flatDataBytes.AsSpan().Slice(0, 512).ToArray(), 0, chunkColumn.height, 0, 512);
					chunkColumn.biomeId = flatDataBytes.AsSpan().Slice(512, 256).ToArray();
				}

				// Block entities
				sw.Start();
				byte[] blockEntityBytes = Db.Get(Combine(index, KeyBlockEntity));
				sw.Stop();

				//Log.Debug($"Read chunk from LevelDB {coordinates.X}, {coordinates.Z} in {sw.ElapsedMilliseconds} ms.");

				// A chunk with no block entities still carries this record, written as zero bytes
				// because MiNET.LevelDB throws on Delete.
				if (blockEntityBytes is {Length: > 0})
				{
					Memory<byte> data = blockEntityBytes.AsMemory();

					var file = new NbtFile
					{
						BigEndian = false,
						UseVarInt = false
					};
					int position = 0;
					while (position < data.Length)
					{
						position += (int) file.LoadFromStream(new MemoryStreamReader(data.Slice(position)), NbtCompression.None);

						NbtTag blockEntityTag = file.RootTag;
						int x = blockEntityTag["x"].IntValue;
						int y = blockEntityTag["y"].IntValue;
						int z = blockEntityTag["z"].IntValue;

						chunkColumn.SetBlockEntity(new BlockCoordinates(x, y, z), (NbtCompound) blockEntityTag);
					}
				}
			}

			if (chunkColumn == null)
			{
				if (version != null) Log.Error($"Expected other version, but got version={version.First()}");

				chunkColumn = generator?.GenerateChunkColumn(coordinates);
				chunkColumn?.RecalcHeight();
			}

			if (chunkColumn != null)
			{
				if (Dimension == Dimension.Overworld && Config.GetProperty("CalculateLights", false))
				{
					var blockAccess = new SkyLightBlockAccess(this, chunkColumn);
					new SkyLightCalculations().RecalcSkyLight(chunkColumn, blockAccess);
					//TODO: Block lights.
				}

				chunkColumn.IsDirty = false;
				//chunkColumn.NeedSave = isGenerated;
			}

			//Log.Debug($"Read chunk {coordinates.X}, {coordinates.Z} in {sw.ElapsedMilliseconds} ms. Was generated: {isGenerated}");

			return chunkColumn;
		}

		/// <summary>
		///     Projects the 3D biome record onto MiNET's per-column biome map by taking the bottom
		///     subchunk's palette. Only the first palette is read: the records above it are almost
		///     always the 0xff copy-previous marker, and MiNET has nowhere to put a vertical
		///     variation anyway.
		/// </summary>
		private static void ParseBiomes3D(ChunkColumn chunkColumn, byte[] data)
		{
			if (data.Length < 1) return;

			int flags = data[0];
			int bitsPerBlock = flags >> 1;
			if (bitsPerBlock == 127) return; // copy-previous with nothing before it

			int offset = 1;
			if (bitsPerBlock == 0)
			{
				if (data.Length < 5) return;
				byte biome = (byte) BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));
				Array.Fill(chunkColumn.biomeId, biome);
				return;
			}

			int biomesPerWord = (int) Math.Floor(32f / bitsPerBlock);
			int wordCount = (int) Math.Ceiling(4096f / biomesPerWord);
			int wordBytes = wordCount * 4;
			if (data.Length < offset + wordBytes + 4) return;

			int paletteCount = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + wordBytes, 4));
			int paletteOffset = offset + wordBytes + 4;
			if (paletteCount <= 0 || data.Length < paletteOffset + paletteCount * 4) return;

			var palette = new byte[paletteCount];
			for (int i = 0; i < paletteCount; i++)
			{
				palette[i] = (byte) BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(paletteOffset + i * 4, 4));
			}

			int mask = (1 << bitsPerBlock) - 1;
			int position = 0;
			for (int w = 0; w < wordCount && position < 4096; w++)
			{
				uint word = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + w * 4, 4));
				for (int slot = 0; slot < biomesPerWord && position < 4096; slot++, position++)
				{
					// Only y == 0 of each column is kept, matching the 2D model.
					if ((position & 0xf) != 0) continue;
					int paletteIndex = (int) ((word >> (slot * bitsPerBlock)) & mask);
					if (paletteIndex >= paletteCount) continue;
					int x = (position >> 8) & 0xf, z = (position >> 4) & 0xf;
					chunkColumn.biomeId[(x << 4) | z] = palette[paletteIndex];
				}
			}
		}

		internal void ParseSection(SubChunk section, ReadOnlyMemory<byte> data)
		{
			var reader = new MemoryStreamReader(data);

			int version = reader.ReadByte();
			if (version != 8 && version != 9) throw new Exception($"Unsupported subchunk version {version}");

			int storageSize = reader.ReadByte();
			// Version 9 carries its own signed section index; the caller already knows it.
			if (version >= 9) reader.ReadByte();
			for (int storage = 0; storage < storageSize; storage++)
			{
				bool isNotLoggedStorage = storage == 0;

				byte paletteAndFlag = (byte) reader.ReadByte();
				bool isRuntime = (paletteAndFlag & 1) != 0;
				if (isRuntime) throw new Exception("Can't use runtime for persistent storage.");
				int bitsPerBlock = paletteAndFlag >> 1;
				int blocksPerWord = (int) Math.Floor(32d / bitsPerBlock);
				int wordCount = (int) Math.Ceiling(4096d / blocksPerWord);

				long blockIndex = reader.Position;
				reader.Position += wordCount * 4;

				int paletteSize = reader.ReadInt32();
				List<int> palette = isNotLoggedStorage ? section.RuntimeIds : section.LoggedRuntimeIds;
				palette.Clear();
				for (int j = 0; j < paletteSize; j++)
				{
					var file = new NbtFile
					{
						BigEndian = false,
						UseVarInt = false
					};
					file.LoadFromStream(reader, NbtCompression.None);
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

					palette.Add(block.GetRuntimeId());
				}

				long nextStore = reader.Position;
				reader.Position = blockIndex;

				int position = 0;
				for (int wordIdx = 0; wordIdx < wordCount; wordIdx++)
				{
					uint word = reader.ReadUInt32();
					for (int block = 0; block < blocksPerWord; block++)
					{
						if (position >= 4096) continue; // padding bytes

						int state = (int) ((word >> ((position % blocksPerWord) * bitsPerBlock)) & ((1 << bitsPerBlock) - 1));
						int x = (position >> 8) & 0xF;
						int y = position & 0xF;
						int z = (position >> 4) & 0xF;
						if (state > palette.Count) Log.Error($"Got wrong state={state} from word. bitsPerBlock={bitsPerBlock}, blocksPerWord={blocksPerWord}, Word={word}");

						if (isNotLoggedStorage)
						{
							section.SetBlockIndex(x, y, z, (short) state);
						}
						else
						{
							section.SetLoggedBlockIndex(x, y, z, (byte) state);
						}
						position++;
					}
				}
				reader.Position = nextStore;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] Combine(byte[] first, byte[] second)
		{
			var ret = new byte[first.Length + second.Length];
			Buffer.BlockCopy(first, 0, ret, 0, first.Length);
			Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
			return ret;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] Combine(byte[] first, byte b)
		{
			var ret = new byte[first.Length + 1];
			Buffer.BlockCopy(first, 0, ret, 0, first.Length);
			ret[^1] = b;
			return ret;
		}


		public Vector3 GetSpawnPoint()
		{
			return new Vector3(LevelInfo.SpawnX, LevelInfo.SpawnY == short.MaxValue ? 0 : LevelInfo.SpawnY, LevelInfo.SpawnZ);
		}

		public string GetName()
		{
			return LevelInfo.LevelName;
		}

		public long GetTime()
		{
			return LevelInfo.Time;
		}

		public long GetDayTime()
		{
			return LevelInfo.Time;
		}

		public int SaveChunks()
		{
			if (!Config.GetProperty("Save.Enabled", false)) return 0;

			int count = 0;
			try
			{
				lock (_chunkCache)
				{
					if (Dimension == Dimension.Overworld) SaveLevelInfo(LevelInfo);

					foreach (ChunkColumn chunkColumn in _chunkCache.Values)
					{
						if (chunkColumn != null && chunkColumn.NeedSave)
						{
							SaveChunk(chunkColumn);
							count++;
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Error("saving chunks", e);
			}

			return count;
		}

		private void SaveLevelInfo(LevelInfoBedrock levelInfo)
		{
			levelInfo.LastPlayed = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			string levelFileName = Path.Combine(BasePath, "level.dat");
			Log.Debug($"Saving level.dat to {levelFileName}");

			NbtTag nbt = levelInfo.Serialize();

			var file = new NbtFile
			{
				BigEndian = false,
				UseVarInt = false,
			};
			file.RootTag = nbt;
			var bytes = file.SaveToBuffer(NbtCompression.None);

			using FileStream stream = File.Create(levelFileName);
			stream.Write(new ReadOnlySpan<byte>(new byte[] {0x08, 0, 0, 0}));
			stream.Write(BitConverter.GetBytes(bytes.Length));
			stream.Write(bytes);
			stream.Flush();
		}

		private void SaveChunk(ChunkColumn chunk)
		{
			byte[] index = Combine(BitConverter.GetBytes(chunk.X), BitConverter.GetBytes(chunk.Z));
			if (Dimension == Dimension.Nether)
			{
				index = Combine(index, BitConverter.GetBytes(1));
			}

			// Always rewrite the version: a stale value is what sends the chunk through Bedrock's
			// upgrade path on load, and that path rewrites block states.
			Db.Put(Combine(index, KeyVersion), new byte[] {ChunkVersion});

			var chunkDataKey = Combine(index, new byte[] {KeySubChunk, 0});
			for (int i = 0; i < ChunkColumn.WorldHeight / 16; i++)
			{
				int sectionY = i - SubChunkIndexOffset;
				chunkDataKey[^1] = unchecked((byte) (sbyte) sectionY);

				Db.Put(chunkDataKey, GetSectionBytes(chunk[i], sectionY));
			}

			Db.Put(Combine(index, KeyHeightAndBiomes3D), GetHeightAndBiome3DBytes(chunk));

			// Without this BDS treats the chunk as unpopulated and runs generation over it,
			// which would overwrite whatever we authored.
			var finalized = new byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(finalized, FinalizedStateDone);
			Db.Put(Combine(index, KeyFinalizedState), finalized);

			// Block entities: the 0x31 record is the concatenated NBT of every one in the chunk,
			// same encoding the reader expects. Without this a chest, sign or bookshelf loses
			// everything it holds on save, and Bedrock recomputes the block states that are
			// backed by the entity, so books_stored and its like reset to empty on load.
			if (chunk.BlockEntities.Count > 0)
			{
				using var blockEntityStream = new MemoryStream();
				foreach (NbtCompound blockEntity in chunk.BlockEntities.Values)
				{
					var file = new NbtFile(blockEntity)
					{
						BigEndian = false,
						UseVarInt = false
					};
					byte[] bytes = file.SaveToBuffer(NbtCompression.None);
					blockEntityStream.Write(bytes, 0, bytes.Length);
				}

				Db.Put(Combine(index, KeyBlockEntity), blockEntityStream.ToArray());
			}
			else
			{
				// An empty record rather than a delete: MiNET.LevelDB throws on Delete. The
				// reader stops at the end of the buffer, so zero bytes reads as no entities,
				// and a chunk that loses its last one does not keep the stale record.
				Db.Put(Combine(index, KeyBlockEntity), Array.Empty<byte>());
			}

			chunk.NeedSave = false;
		}

		private byte[] GetSectionBytes(SubChunk subChunk, int sectionY)
		{
			using var stream = new MemoryStream();
			Write(subChunk, stream, sectionY);

			return stream.ToArray();
		}

		/// <summary>
		///     Heightmap plus 3D biomes, the 0x2b record: 512 bytes of int16 heights followed by one
		///     paletted biome array per subchunk. Replaces the pre-1.18 0x2d record, which held a
		///     heightmap plus a flat 256-byte biome map and is absent from current worlds.
		///     MiNET models biomes per column, so every subchunk gets the same projection of it.
		/// </summary>
		private byte[] GetHeightAndBiome3DBytes(ChunkColumn chunk)
		{
			using var stream = new MemoryStream();

			byte[] heightBytes = new byte[512];
			Buffer.BlockCopy(chunk.height, 0, heightBytes, 0, 512);
			stream.Write(heightBytes);

			// Distinct biomes in this column decide the palette; a single-biome column needs no
			// index words at all, which is the common case and what BDS itself writes for one.
			var palette = new List<byte>();
			foreach (byte biome in chunk.biomeId)
			{
				if (!palette.Contains(biome)) palette.Add(biome);
			}
			if (palette.Count == 0) palette.Add(0);

			using var biomeSection = new MemoryStream();
			WriteBiomePalette(biomeSection, chunk, palette);
			byte[] first = biomeSection.ToArray();

			stream.Write(first);
			// Every remaining subchunk repeats it. 0xff is the copy-previous marker.
			for (int i = 1; i < ChunkColumn.WorldHeight / 16; i++) stream.WriteByte(0xff);

			return stream.ToArray();
		}

		private static void WriteBiomePalette(MemoryStream stream, ChunkColumn chunk, List<byte> palette)
		{
			if (palette.Count == 1)
			{
				// bitsPerBlock 0: no index words, and the palette count is implicit rather than written.
				stream.WriteByte(0 << 1);
				var single = new byte[4];
				BinaryPrimitives.WriteInt32LittleEndian(single, palette[0]);
				stream.Write(single);
				return;
			}

			int bitsPerBlock = 1;
			while ((1 << bitsPerBlock) < palette.Count) bitsPerBlock++;
			if (bitsPerBlock > 8) bitsPerBlock = 16;

			stream.WriteByte((byte) (bitsPerBlock << 1));

			int biomesPerWord = (int) Math.Floor(32f / bitsPerBlock);
			int wordCount = (int) Math.Ceiling(4096f / biomesPerWord);
			var words = new uint[wordCount];

			int position = 0;
			for (int w = 0; w < wordCount; w++)
			{
				uint word = 0;
				for (int slot = 0; slot < biomesPerWord && position < 4096; slot++, position++)
				{
					// Cell order matches the block storage: (x << 8) | (z << 4) | y.
					int x = (position >> 8) & 0xf, z = (position >> 4) & 0xf;
					uint paletteIndex = (uint) palette.IndexOf(chunk.biomeId[(x << 4) | z]);
					word |= paletteIndex << (bitsPerBlock * slot);
				}
				words[w] = word;
			}

			byte[] wordBytes = new byte[words.Length * 4];
			Buffer.BlockCopy(words, 0, wordBytes, 0, wordBytes.Length);
			stream.Write(wordBytes);

			var count = new byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(count, palette.Count);
			stream.Write(count);
			foreach (byte biome in palette)
			{
				var entry = new byte[4];
				BinaryPrimitives.WriteInt32LittleEndian(entry, biome);
				stream.Write(entry);
			}
		}

		public void Write(SubChunk subChunk, MemoryStream stream, int sectionY)
		{
			var startPos = stream.Position;

			stream.WriteByte(SubChunkVersion); // version

			long storePosition = stream.Position;
			int numberOfStores = 0;
			stream.WriteByte((byte) numberOfStores); // storage size

			// Version 9 onwards the record carries its own signed section index.
			stream.WriteByte(unchecked((byte) (sbyte) sectionY));

			if (WriteStore(stream, subChunk.Blocks, null, false, subChunk.RuntimeIds))
			{
				numberOfStores++;
				if (WriteStore(stream, null, subChunk.LoggedBlocks, false, subChunk.LoggedRuntimeIds))
				{
					numberOfStores++;
				}
			}

			stream.Position = storePosition;
			stream.WriteByte((byte) numberOfStores); // storage size
		}

		internal bool WriteStore(MemoryStream stream, short[] blocks, byte[] loggedBlocks, bool forceWrite, List<int> palette)
		{
			if (palette.Count == 0) return false;

			// log2(number of entries) => bits needed to store them
			int bitsPerBlock = (int) Math.Ceiling(Math.Log(palette.Count, 2));

			switch (bitsPerBlock)
			{
				case 0:
					if (!forceWrite && palette.Contains(0)) return false;
					bitsPerBlock = 1;
					break;
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
					//Paletted1 = 1,   // 32 blocks per word
					//Paletted2 = 2,   // 16 blocks per word
					//Paletted3 = 3,   // 10 blocks and 2 bits of padding per word
					//Paletted4 = 4,   // 8 blocks per word
					//Paletted5 = 5,   // 6 blocks and 2 bits of padding per word
					//Paletted6 = 6,   // 5 blocks and 2 bits of padding per word
					break;
				case 7:
				case 8:
					//Paletted8 = 8,  // 4 blocks per word
					bitsPerBlock = 8;
					break;
				case int i when i > 8:
					//Paletted16 = 16, // 2 blocks per word
					bitsPerBlock = 16;
					break;
				default:
					break;
			}

			stream.WriteByte((byte) ((bitsPerBlock << 1) | 0));

			int blocksPerWord = (int) Math.Floor(32f / bitsPerBlock); // Floor to remove padding bits
			int wordsPerChunk = (int) Math.Ceiling(4096f / blocksPerWord);

			uint[] indexes = new uint[wordsPerChunk];

			int position = 0;
			for (int w = 0; w < wordsPerChunk; w++)
			{
				uint word = 0;
				for (int block = 0; block < blocksPerWord; block++)
				{
					if (position >= 4096) continue;

					uint state;
					if (blocks != null)
					{
						state = (uint) blocks[position];
					}
					else
					{
						state = (uint) loggedBlocks[position];
					}
					word |= state << (bitsPerBlock * block);

					position++;
				}
				indexes[w] = word;
			}

			byte[] ba = new byte[indexes.Length * 4];
			Buffer.BlockCopy(indexes, 0, ba, 0, indexes.Length * 4);

			stream.Write(ba, 0, ba.Length);

			var count = new byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(count, palette.Count);

			stream.Write(count);
			foreach (int runtimeId in palette)
			{
				BlockStateContainer blockState = BlockFactory.BlockPalette[runtimeId];
				var file = new NbtFile
				{
					BigEndian = false,
					UseVarInt = false
				};
				file.RootTag = WriteBlockState(blockState);
				byte[] bytes = file.SaveToBuffer(NbtCompression.None);
				stream.Write(bytes);
			}

			return true;
		}


		public bool HaveNether()
		{
			return true;
		}

		public bool HaveTheEnd()
		{
			return false;
		}

		public ChunkColumn[] GetCachedChunks()
		{
			return _chunkCache.Values.Where(column => column != null).ToArray();
		}

		public void ClearCachedChunks()
		{
			_chunkCache.Clear();
		}

		public int UnloadChunks(Player[] players, ChunkCoordinates spawn, double maxViewDistance)
		{
			int removed = 0;

			lock (_chunkCache)
			{
				var coords = new List<ChunkCoordinates> {spawn};

				foreach (var player in players)
				{
					var chunkCoordinates = new ChunkCoordinates(player.KnownPosition);
					if (!coords.Contains(chunkCoordinates))
						coords.Add(chunkCoordinates);
				}

				Parallel.ForEach(_chunkCache, (chunkColumn) =>
				{
					bool keep = coords.Exists(c => c.DistanceTo(chunkColumn.Key) < maxViewDistance);
					if (!keep)
					{
						_chunkCache.TryRemove(chunkColumn.Key, out var waste);

						if (waste != null)
						{
							foreach (var chunk in waste)
							{
								chunk.PutPool();
							}
						}

						Interlocked.Increment(ref removed);
					}
				});
			}

			return removed;
		}

		public object Clone()
		{
			throw new NotImplementedException();
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
					NbtTagType.Byte => new BlockStateByte()
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

		private static NbtCompound WriteBlockState(BlockStateContainer container)
		{
			var tag = new NbtCompound("");

			tag.Add(new NbtString("name", container.Name));
			var nbtStates = new NbtCompound("states");

			foreach (IBlockState state in container.States)
			{
				switch (state)
				{
					case BlockStateByte value:
					{
						nbtStates.Add(new NbtByte(value.Name, value.Value));
						break;
					}
					case BlockStateInt value:
					{
						nbtStates.Add(new NbtInt(value.Name, value.Value));
						break;
					}
					case BlockStateString value:
					{
						nbtStates.Add(new NbtString(value.Name, value.Value));
						break;
					}
				}
			}

			tag.Add(nbtStates);
			// Generated from the palette data, not hardcoded: a state stored without the schema
			// stamp is treated as predating every upgrade schema and gets rewritten on load.
			tag.Add(new NbtInt("version", BlockPaletteData.BlockStateVersion));

			return tag;
		}
	}

	public static class NbtSerializationHelper
	{
		public static T Deserialize<T>(this NbtTag tag) where T : new()
		{
			var obj = new T();

			PropertyInfo[] properties = obj.GetType().GetProperties();
			//if(properties.Length != 12) throw new Exception($"{properties.Length}");
			foreach (PropertyInfo propertyInfo in properties)
			{
				//if (propertyInfo.PropertyType.IsValueType)
				{
					NbtTag nbtTag = tag[propertyInfo.Name];
					if (nbtTag == null)
					{
						nbtTag = tag[LowercaseFirst(propertyInfo.Name)];
					}

					if (nbtTag == null)
					{
						continue;
					}

					switch (nbtTag.TagType)
					{
						case NbtTagType.Unknown:
							break;
						case NbtTagType.End:
							break;
						case NbtTagType.Byte:
							if (propertyInfo.PropertyType == typeof(bool))
								propertyInfo.SetValue(obj, nbtTag.ByteValue == 1);
							else
								propertyInfo.SetValue(obj, nbtTag.ByteValue);
							break;
						case NbtTagType.Short:
							propertyInfo.SetValue(obj, nbtTag.ShortValue);
							break;
						case NbtTagType.Int:
							if (propertyInfo.PropertyType == typeof(bool))
								propertyInfo.SetValue(obj, nbtTag.IntValue == 1);
							else
								propertyInfo.SetValue(obj, nbtTag.IntValue);
							break;
						case NbtTagType.Long:
							propertyInfo.SetValue(obj, nbtTag.LongValue);
							break;
						case NbtTagType.Float:
							propertyInfo.SetValue(obj, nbtTag.FloatValue);
							break;
						case NbtTagType.Double:
							propertyInfo.SetValue(obj, nbtTag.DoubleValue);
							break;
						case NbtTagType.ByteArray:
							propertyInfo.SetValue(obj, nbtTag.ByteArrayValue);
							break;
						case NbtTagType.String:
							propertyInfo.SetValue(obj, nbtTag.StringValue);
							break;
						case NbtTagType.List:
							break;
						case NbtTagType.Compound:
							break;
						case NbtTagType.IntArray:
							propertyInfo.SetValue(obj, nbtTag.IntArrayValue);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			return obj;
		}

		private static string LowercaseFirst(string s)
		{
			// Check for empty string.
			if (string.IsNullOrEmpty(s))
			{
				return string.Empty;
			}
			// Return char and concat substring.
			return char.ToLower(s[0]) + s.Substring(1);
		}


		public static NbtTag Serialize<T>(this T obj, NbtTag tag = null) where T : new()
		{
			tag ??= new NbtCompound(string.Empty);

			if (obj == null) throw new NullReferenceException();

			PropertyInfo[] properties = obj.GetType().GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				var attribute = propertyInfo.GetCustomAttribute(typeof(JsonPropertyNameAttribute)) as JsonPropertyNameAttribute;
				string propertyName = attribute?.Name ?? propertyInfo.Name;
				NbtTag nbtTag = tag[propertyName] ?? tag[LowercaseFirst(propertyName)];

				if (nbtTag == null)
				{
					if (propertyInfo.PropertyType == typeof(bool))
					{
						nbtTag = new NbtByte(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(byte))
					{
						nbtTag = new NbtByte(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(short))
					{
						nbtTag = new NbtShort(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(int))
					{
						nbtTag = new NbtInt(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(long))
					{
						nbtTag = new NbtLong(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(float))
					{
						nbtTag = new NbtFloat(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(double))
					{
						nbtTag = new NbtDouble(propertyName);
					}
					else if (propertyInfo.PropertyType == typeof(string))
					{
						nbtTag = new NbtString(propertyName, "");
					}
					else
					{
						continue;
					}
				}

				//var mex = property.Body as MemberExpression;
				//var target = Expression.Lambda(mex.Expression).Compile().DynamicInvoke();

				switch (nbtTag.TagType)
				{
					case NbtTagType.Unknown:
						break;
					case NbtTagType.End:
						break;
					case NbtTagType.Byte:
						if (propertyInfo.PropertyType == typeof(bool))
							tag[nbtTag.Name] = new NbtByte(nbtTag.Name, (byte) ((bool) propertyInfo.GetValue(obj) ? 1 : 0));
						else
							tag[nbtTag.Name] = new NbtByte(nbtTag.Name, (byte) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.Short:
						tag[nbtTag.Name] = new NbtShort(nbtTag.Name, (short) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.Int:
						if (propertyInfo.PropertyType == typeof(bool))
							tag[nbtTag.Name] = new NbtInt(nbtTag.Name, (bool) propertyInfo.GetValue(obj) ? 1 : 0);
						else
							tag[nbtTag.Name] = new NbtInt(nbtTag.Name, (int) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.Long:
						tag[nbtTag.Name] = new NbtLong(nbtTag.Name, (long) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.Float:
						tag[nbtTag.Name] = new NbtFloat(nbtTag.Name, (float) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.Double:
						tag[nbtTag.Name] = new NbtDouble(nbtTag.Name, (double) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.ByteArray:
						tag[nbtTag.Name] = new NbtByteArray(nbtTag.Name, (byte[]) propertyInfo.GetValue(obj));
						break;
					case NbtTagType.String:
						tag[nbtTag.Name] = new NbtString(nbtTag.Name, (string) propertyInfo.GetValue(obj) ?? "");
						break;
					case NbtTagType.List:
						break;
					case NbtTagType.Compound:
						break;
					case NbtTagType.IntArray:
						tag[nbtTag.Name] = new NbtIntArray(nbtTag.Name, (int[]) propertyInfo.GetValue(obj));
						break;
				}
			}

			return tag;
		}
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	public class LevelInfoBedrock : ICloneable
	{
		public string BiomeOverride { get; set; }
		public byte CenterMapsToOrigin { get; set; }
		public byte ConfirmedPlatformLockedContent { get; set; }
		public int Difficulty { get; set; }
		public string FlatWorldLayers { get; set; }
		public byte ForceGameType { get; set; }
		public int GameType { get; set; }
		public int Generator { get; set; }
		public string InventoryVersion { get; set; }
		public byte LANBroadcast { get; set; }
		public byte LANBroadcastIntent { get; set; }
		public long LastPlayed { get; set; }
		public string LevelName { get; set; }
		public int LimitedWorldOriginX { get; set; }
		public int LimitedWorldOriginY { get; set; }

		public int LimitedWorldOriginZ { get; set; }

		//public  MinimumCompatibleClientVersion { get; set; }
		public byte MultiplayerGame { get; set; }
		public byte MultiplayerGameIntent { get; set; }
		public int NetherScale { get; set; }
		public int NetworkVersion { get; set; }
		public int Platform { get; set; }
		public int PlatformBroadcastIntent { get; set; }
		public long RandomSeed { get; set; }
		public byte SpawnV1Villagers { get; set; }
		public int SpawnX { get; set; }
		public int SpawnY { get; set; }
		public int SpawnZ { get; set; }
		public int StorageVersion { get; set; }
		public long Time { get; set; }

		public int XBLBroadcastIntent { get; set; }

		//[JsonPropertyName("abilities")] public  Abilities { get; set; }
		[JsonPropertyName("baseGameVersion")] public string BaseGameVersion { get; set; }

		[JsonPropertyName("bonusChestEnabled")]
		public byte BonusChestEnabled { get; set; }

		[JsonPropertyName("bonusChestSpawned")]
		public byte BonusChestSpawned { get; set; }

		[JsonPropertyName("commandblockoutput")]
		public byte Commandblockoutput { get; set; }

		[JsonPropertyName("commandblocksenabled")]
		public byte Commandblocksenabled { get; set; }

		[JsonPropertyName("commandsEnabled")] public byte CommandsEnabled { get; set; }
		[JsonPropertyName("currentTick")] public long CurrentTick { get; set; }
		[JsonPropertyName("dodaylightcycle")] public byte Dodaylightcycle { get; set; }
		[JsonPropertyName("doentitydrops")] public byte Doentitydrops { get; set; }
		[JsonPropertyName("dofiretick")] public byte Dofiretick { get; set; }

		[JsonPropertyName("doimmediaterespawn")]
		public byte Doimmediaterespawn { get; set; }

		[JsonPropertyName("doinsomnia")] public byte Doinsomnia { get; set; }
		[JsonPropertyName("domobloot")] public byte Domobloot { get; set; }
		[JsonPropertyName("domobspawning")] public byte Domobspawning { get; set; }
		[JsonPropertyName("dotiledrops")] public byte Dotiledrops { get; set; }
		[JsonPropertyName("doweathercycle")] public byte Doweathercycle { get; set; }
		[JsonPropertyName("drowningdamage")] public byte Drowningdamage { get; set; }
		[JsonPropertyName("eduOffer")] public int EduOffer { get; set; }

		[JsonPropertyName("educationFeaturesEnabled")]
		public byte EducationFeaturesEnabled { get; set; }

		[JsonPropertyName("experimentalgameplay")]
		public byte Experimentalgameplay { get; set; }

		[JsonPropertyName("falldamage")] public byte Falldamage { get; set; }
		[JsonPropertyName("firedamage")] public byte Firedamage { get; set; }

		[JsonPropertyName("functioncommandlimit")]
		public int Functioncommandlimit { get; set; }

		[JsonPropertyName("hasBeenLoadedInCreative")]
		public byte HasBeenLoadedInCreative { get; set; }

		[JsonPropertyName("hasLockedBehaviorPack")]
		public byte HasLockedBehaviorPack { get; set; }

		[JsonPropertyName("hasLockedResourcePack")]
		public byte HasLockedResourcePack { get; set; }

		[JsonPropertyName("immutableWorld")] public byte ImmutableWorld { get; set; }

		[JsonPropertyName("isFromLockedTemplate")]
		public byte IsFromLockedTemplate { get; set; }

		[JsonPropertyName("isFromWorldTemplate")]
		public byte IsFromWorldTemplate { get; set; }

		[JsonPropertyName("isSingleUseWorld")] public byte IsSingleUseWorld { get; set; }

		[JsonPropertyName("isWorldTemplateOptionLocked")]
		public byte IsWorldTemplateOptionLocked { get; set; }

		[JsonPropertyName("keepinventory")] public byte Keepinventory { get; set; }

		//[JsonPropertyName("lastOpenedWithVersion")] public  LastOpenedWithVersion { get; set; }
		[JsonPropertyName("lightningLevel")] public float LightningLevel { get; set; }
		[JsonPropertyName("lightningTime")] public int LightningTime { get; set; }

		[JsonPropertyName("limitedWorldDepth")]
		public int LimitedWorldDepth { get; set; }

		[JsonPropertyName("limitedWorldWidth")]
		public int LimitedWorldWidth { get; set; }

		[JsonPropertyName("maxcommandchainlength")]
		public int Maxcommandchainlength { get; set; }

		[JsonPropertyName("mobgriefing")] public byte Mobgriefing { get; set; }

		[JsonPropertyName("naturalregeneration")]
		public byte Naturalregeneration { get; set; }

		[JsonPropertyName("prid")] public string Prid { get; set; }
		[JsonPropertyName("pvp")] public byte Pvp { get; set; }
		[JsonPropertyName("rainLevel")] public float RainLevel { get; set; }
		[JsonPropertyName("rainTime")] public int RainTime { get; set; }
		[JsonPropertyName("randomtickspeed")] public int Randomtickspeed { get; set; }

		[JsonPropertyName("requiresCopiedPackRemovalCheck")]
		public byte RequiresCopiedPackRemovalCheck { get; set; }

		[JsonPropertyName("sendcommandfeedback")]
		public byte Sendcommandfeedback { get; set; }

		[JsonPropertyName("serverChunkTickRange")]
		public int ServerChunkTickRange { get; set; }

		[JsonPropertyName("showcoordinates")] public byte Showcoordinates { get; set; }

		[JsonPropertyName("showdeathmessages")]
		public byte Showdeathmessages { get; set; }

		[JsonPropertyName("showtags")] public byte Showtags { get; set; }
		[JsonPropertyName("spawnMobs")] public byte SpawnMobs { get; set; }
		[JsonPropertyName("spawnradius")] public int Spawnradius { get; set; }

		[JsonPropertyName("startWithMapEnabled")]
		public byte StartWithMapEnabled { get; set; }

		[JsonPropertyName("texturePacksRequired")]
		public byte TexturePacksRequired { get; set; }

		[JsonPropertyName("tntexplodes")] public byte Tntexplodes { get; set; }

		[JsonPropertyName("useMsaGamertagsOnly")]
		public byte UseMsaGamertagsOnly { get; set; }

		[JsonPropertyName("worldStartCount")] public long WorldStartCount { get; set; }

		public static LevelInfoBedrock FromNbt(NbtTag tag)
		{
			LevelInfoBedrock obj = tag.Deserialize<LevelInfoBedrock>();
			return obj;
		}

		public LevelInfoBedrock()
		{
		}

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}