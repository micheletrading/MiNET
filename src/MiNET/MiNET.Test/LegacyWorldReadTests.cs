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
using System.Linq;
using fNbt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Test
{
	/// <summary>
	///     Reads real saved worlds, because the failure this guards against is silent. A world whose
	///     columns the provider rejects, or whose palette entries it cannot resolve, still opens,
	///     still answers every chunk request and throws nothing. It just hands back air, and the map
	///     is not there.
	///     <para>
	///     The set is the NetherGames arenas (CC BY 4.0, github.com/NetherGamesMC/assets), which hold
	///     both generations that matter: PocketMine wrote its columns as version 7 with pre-1.13
	///     palette entries, a name and a numeric val, while the Bedrock-written ones are version 40
	///     with a name and a states compound.
	///     </para>
	/// </summary>
	[TestClass]
	public class LegacyWorldReadTests
	{
		// Set MINET_LEGACY_WORLDS to a folder of Bedrock world folders to point these elsewhere.
		private const string DefaultWorldsRoot = @"C:\Development\Worlds\PlotterMaps";
		private const string PocketMineWorld = "SW-Space";
		private const string BedrockWorld = "SW-Aether";

		// The arenas sit within a few hundred blocks of origin, so this covers each one whole.
		private const int ScanRadius = 32;

		private static string _scratch;

		[ClassInitialize]
		public static void UseFreshCopies(TestContext context)
		{
			// Copies, because a running server holds its LevelDB folders open, and because opening a
			// world replays its write-ahead log, which writes to the folder.
			_scratch = Path.Combine(Path.GetTempPath(), "minet-legacy-worlds");
			if (Directory.Exists(_scratch)) Directory.Delete(_scratch, true);
		}

		[TestMethod]
		public void PocketMineWorld_ColumnsAreReadFromDisk()
		{
			LevelDbProvider provider = OpenWorld(PocketMineWorld, "-structure");

			List<ChunkColumn> columns = ReadAll(provider);

			Assert.IsTrue(columns.Count > 100, $"Expected the stored columns, got {columns.Count}. A version gate that rejects the column format leaves zero.");
			Assert.IsTrue(columns.Any(column => SectionCount(column) > 0), "Every column came back without a single section, so no section record was parsed.");
		}

		[TestMethod]
		public void PocketMineWorld_ColumnsHoldEveryBlockTheirPaletteNames()
		{
			AssertNothingWasLost(PocketMineWorld);
		}

		[TestMethod]
		public void BedrockWorld_ColumnsHoldEveryBlockTheirPaletteNames()
		{
			AssertNothingWasLost(BedrockWorld);
		}

		/// <summary>
		///     Counts the blocks a world stores against the blocks the provider hands back. The stored
		///     record is the oracle: every index in a section points at a palette entry, and an entry
		///     that is not minecraft:air is a block that must survive the read. Comparing the two counts
		///     says nothing about how the provider resolves a name, only that it resolved it, which is
		///     what a map being there or not comes down to.
		/// </summary>
		private static void AssertNothingWasLost(string world)
		{
			int stored = CountStoredBlocks(OpenWorld(world, "-stored"));
			(int read, int total) = CountBlocks(ReadAll(OpenWorld(world, "-read")));

			Console.WriteLine($"{world}: {stored} blocks stored, {read} read back, {total} block slots in stored sections");
			Assert.AreEqual(stored, read, $"{stored - read} of {stored} stored blocks came back as air.");
		}

		[TestMethod]
		public void ReadingAColumn_StaysWithinItsTimeBudget()
		{
			// A join reads a disc of columns, so per-column cost is what the player waits on. This is
			// a floor, not a benchmark: it catches a read that has become an order of magnitude
			// slower, which is what a fallback path or a per-column rescan looks like.
			LevelDbProvider warmup = OpenWorld(PocketMineWorld, "-warm");
			ReadAll(warmup);

			LevelDbProvider provider = OpenWorld(PocketMineWorld, "-timed");

			var stopwatch = Stopwatch.StartNew();
			List<ChunkColumn> columns = ReadAll(provider);
			stopwatch.Stop();

			double perColumn = stopwatch.Elapsed.TotalMilliseconds / Math.Max(1, columns.Count);
			Console.WriteLine($"{columns.Count} columns in {stopwatch.ElapsedMilliseconds}ms, {perColumn:F2}ms per column");

			Assert.IsTrue(perColumn < 10, $"{perColumn:F2}ms per column is far above what reading a stored column costs.");
		}

		private static double Percent(int part, int whole)
		{
			return 100.0 * part / Math.Max(1, whole);
		}

		private static LevelDbProvider OpenWorld(string name, string suffix = "")
		{
			string root = Environment.GetEnvironmentVariable("MINET_LEGACY_WORLDS") ?? DefaultWorldsRoot;
			string source = Path.Combine(root, name);
			if (!Directory.Exists(source)) Assert.Inconclusive($"No world at {source}. Set MINET_LEGACY_WORLDS to a folder of Bedrock worlds.");

			string target = Path.Combine(_scratch, name + suffix);
			if (!Directory.Exists(target)) CopyDirectory(source, target);

			var provider = new LevelDbProvider(target);
			provider.Initialize();
			return provider;
		}

		private static void CopyDirectory(string source, string target)
		{
			Directory.CreateDirectory(target);
			foreach (string file in Directory.EnumerateFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
			foreach (string directory in Directory.EnumerateDirectories(source)) CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
		}

		private static List<ChunkColumn> ReadAll(LevelDbProvider provider)
		{
			var columns = new List<ChunkColumn>();
			for (int x = -ScanRadius; x <= ScanRadius; x++)
			{
				for (int z = -ScanRadius; z <= ScanRadius; z++)
				{
					// No generator, so a column that is not on disk comes back null instead of being
					// filled in. This counts what was stored and nothing else.
					ChunkColumn column = provider.GetChunk(new ChunkCoordinates(x, z), null);
					if (column != null) columns.Add(column);
				}
			}

			return columns;
		}

		private static int SectionCount(ChunkColumn column)
		{
			int count = 0;
			for (int i = 0; i < ChunkColumn.WorldHeight / 16; i++)
			{
				if (column[i, false] != null) count++;
			}

			return count;
		}

		private static (int Solid, int Total) CountBlocks(List<ChunkColumn> columns)
		{
			int air = new Air().GetRuntimeId();
			int solid = 0, total = 0;

			foreach (ChunkColumn column in columns)
			{
				for (int i = 0; i < ChunkColumn.WorldHeight / 16; i++)
				{
					SubChunk section = column[i, false];
					if (section == null) continue;

					for (int x = 0; x < 16; x++)
					for (int y = 0; y < 16; y++)
					for (int z = 0; z < 16; z++)
					{
						total++;
						if (section.GetBlockRuntimeId(x, y, z) != air) solid++;
					}
				}
			}

			return (solid, total);
		}

		private static int CountStoredBlocks(LevelDbProvider provider)
		{
			const byte keyVersion = 0x2c;
			const byte keyVersionLegacy = 0x76;
			const byte keySubChunk = 0x2f;

			int stored = 0;
			for (int x = -ScanRadius; x <= ScanRadius; x++)
			{
				for (int z = -ScanRadius; z <= ScanRadius; z++)
				{
					byte[] index = BitConverter.GetBytes(x).Concat(BitConverter.GetBytes(z)).ToArray();
					if (provider.Db.Get(index.Append(keyVersion).ToArray()) == null && provider.Db.Get(index.Append(keyVersionLegacy).ToArray()) == null) continue;

					for (int section = -4; section <= 19; section++)
					{
						byte[] record = provider.Db.Get(index.Append(keySubChunk).Append(unchecked((byte) (sbyte) section)).ToArray());
						if (record != null) stored += CountNonAirInRecord(record);
					}
				}
			}

			return stored;
		}

		/// <summary>
		///     Reads the block layer of a stored section record without going through the provider: the
		///     palette flag says how many bits an index takes, the packed words hold one index per block,
		///     and the palette says which indices are air. Only the first storage is the block layer; a
		///     second one, where there is one, is what the block is waterlogged with.
		/// </summary>
		private static int CountNonAirInRecord(byte[] record)
		{
			var stream = new MemoryStream(record);
			int version = stream.ReadByte();
			if (version != 8 && version != 9) return 0;

			int storageCount = stream.ReadByte();
			if (storageCount == 0) return 0;
			if (version >= 9) stream.ReadByte();

			int bitsPerBlock = stream.ReadByte() >> 1;
			if (bitsPerBlock == 0) return 0;

			int blocksPerWord = 32 / bitsPerBlock;
			int wordCount = (int) Math.Ceiling(4096d / blocksPerWord);

			var words = new byte[wordCount * 4];
			stream.ReadExactly(words);

			var paletteSizeBytes = new byte[4];
			stream.ReadExactly(paletteSizeBytes);
			int paletteSize = BitConverter.ToInt32(paletteSizeBytes);

			var isAir = new bool[paletteSize];
			for (int entry = 0; entry < paletteSize; entry++)
			{
				var file = new NbtFile {BigEndian = false, UseVarInt = false};
				file.LoadFromStream(stream, NbtCompression.None);
				isAir[entry] = ((NbtCompound) file.RootTag)["name"]?.StringValue == "minecraft:air";
			}

			int mask = (1 << bitsPerBlock) - 1;
			int nonAir = 0;
			int position = 0;
			for (int word = 0; word < wordCount && position < 4096; word++)
			{
				uint value = BitConverter.ToUInt32(words, word * 4);
				for (int block = 0; block < blocksPerWord && position < 4096; block++, position++)
				{
					int paletteIndex = (int) ((value >> (bitsPerBlock * block)) & mask);
					if (paletteIndex < paletteSize && !isAir[paletteIndex]) nonAir++;
				}
			}

			return nonAir;
		}
	}
}
