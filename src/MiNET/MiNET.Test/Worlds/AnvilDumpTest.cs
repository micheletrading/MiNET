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

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Test.Worlds
{
	/// <summary>
	///     Bisection tool, not a test: dumps every block runtime id of the spawn-area columns to a
	///     text file so two versions of the anvil loader can be diffed. Compiles against both the
	///     current tree and HEAD on purpose - keep it to public API only.
	/// </summary>
	[TestClass]
	public class AnvilDumpTest
	{
		[TestMethod]
		[Ignore("Bisection tool, not a test: prints the air runtime id from every derivation the anvil path uses.")]
		public void Probe_air_identities()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"BlockFactory.AirRuntimeId:                       {MiNET.Blocks.BlockFactory.AirRuntimeId}");
			sb.AppendLine($"GetBlockByName(minecraft:air).GetRuntimeId():    {MiNET.Blocks.BlockFactory.GetBlockByName("minecraft:air").GetRuntimeId()}");
			for (byte m = 0; m < 4; m++)
			{
				sb.AppendLine($"GetRuntimeId(0, {m}) via R12 legacy table:         {MiNET.Blocks.BlockFactory.GetRuntimeId(0, m)}");
			}
			sb.AppendLine($"GetRuntimeId(248, 0) info_update fallback:       {MiNET.Blocks.BlockFactory.GetRuntimeId(248, 0)}");

			File.WriteAllText(@"C:\Development\github\MiNET\temp_auto\air-identities.txt", sb.ToString());
		}

		[TestMethod]
		[Ignore("Bisection tool, not a test: dumps spawn-area column hashes for diffing two loader versions.")]
		public void Dump_spawn_columns()
		{
			var provider = new AnvilWorldProvider(@"C:\Development\github\MiNET\worlds\Falcon's Rock v2.0.0");
			provider.Initialize();

			var sb = new StringBuilder();
			for (int cx = 29; cx <= 33; cx++)
			{
				for (int cz = 45; cz <= 49; cz++)
				{
					ChunkColumn column = provider.GenerateChunkColumn(new ChunkCoordinates(cx, cz));
					if (column == null)
					{
						sb.AppendLine($"column {cx},{cz}: MISSING");
						continue;
					}

					ulong hash = 14695981039346656037UL;
					int nonAir = 0;
					for (int y = 0; y < 256; y++)
					{
						for (int x = 0; x < 16; x++)
						{
							for (int z = 0; z < 16; z++)
							{
								int id = column.GetBlockRuntimeId(x, y, z);
								hash = (hash ^ (uint) id) * 1099511628211UL;
								if (column.GetBlockId(x, y, z) != 0) nonAir++;
							}
						}
					}
					sb.AppendLine($"column {cx},{cz}: hash={hash:X16} nonAir={nonAir}");
				}
			}

			File.WriteAllText(@"C:\Development\github\MiNET\temp_auto\anvil-dump.txt", sb.ToString());
		}

		[TestMethod]
		[Ignore("Bisection tool, not a test: prints one column's block distribution to see what a location actually contains.")]
		public void Probe_spawn_column_content()
		{
			var provider = new AnvilWorldProvider(@"C:\Development\github\MiNET\worlds\Falcon's Rock v2.0.0");
			provider.Initialize();

			ChunkColumn column = provider.GenerateChunkColumn(new ChunkCoordinates(31, 47));
			var sb = new StringBuilder();

			// The vertical profile at the spawn block itself (506,762 -> local 10,2).
			for (int y = 100; y >= 0; y -= 1)
			{
				var block = column.GetBlockObject(10, y, 2);
				if (y % 10 == 0 || (y > 55 && y < 70)) sb.AppendLine($"y={y}: {block.GetType().Name}");
			}

			// Distribution over the whole column, by block type.
			var counts = new System.Collections.Generic.Dictionary<string, int>();
			for (int y = 0; y < 256; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					for (int z = 0; z < 16; z++)
					{
						string name = column.GetBlockObject(x, y, z).GetType().Name;
						counts[name] = counts.TryGetValue(name, out int n) ? n + 1 : 1;
					}
				}
			}
			sb.AppendLine("--- distribution ---");
			foreach (var pair in counts.OrderByDescending(p => p.Value)) sb.AppendLine($"{pair.Key}: {pair.Value}");

			File.WriteAllText(@"C:\Development\github\MiNET\temp_auto\anvil-probe.txt", sb.ToString());
		}

		/// <summary>
		///     The concurrency half of the bisection: loads the same region sequentially and under
		///     Parallel.ForEach with fresh providers and compares every column hash. A mismatch is
		///     a data race in the load path.
		/// </summary>
		[TestMethod]
		public void Parallel_load_matches_sequential()
		{
			const string worldPath = @"C:\Development\github\MiNET\worlds\Falcon's Rock v2.0.0";

			ulong HashColumn(ChunkColumn column)
			{
				ulong hash = 14695981039346656037UL;
				for (int y = 0; y < 256; y++)
				{
					for (int x = 0; x < 16; x++)
					{
						for (int z = 0; z < 16; z++)
						{
							hash = (hash ^ (uint) column.GetBlockRuntimeId(x, y, z)) * 1099511628211UL;
						}
					}
				}
				return hash;
			}

			var coordinates = new System.Collections.Generic.List<ChunkCoordinates>();
			for (int cx = 20; cx <= 43; cx++)
			{
				for (int cz = 36; cz <= 59; cz++)
				{
					coordinates.Add(new ChunkCoordinates(cx, cz));
				}
			}

			var sequential = new AnvilWorldProvider(worldPath);
			sequential.Initialize();
			var expected = new System.Collections.Generic.Dictionary<ChunkCoordinates, ulong>();
			foreach (ChunkCoordinates c in coordinates)
			{
				expected[c] = HashColumn(sequential.GenerateChunkColumn(c));
			}

			for (int round = 0; round < 3; round++)
			{
				var parallel = new AnvilWorldProvider(worldPath);
				parallel.Initialize();
				var mismatches = new System.Collections.Concurrent.ConcurrentBag<string>();

				System.Threading.Tasks.Parallel.ForEach(coordinates, c =>
				{
					ulong hash = HashColumn(parallel.GenerateChunkColumn(c));
					if (hash != expected[c]) mismatches.Add($"{c.X},{c.Z}");
				});

				Assert.AreEqual(0, mismatches.Count, $"round {round}: columns differing under parallel load: {string.Join(" ", mismatches)}");
			}
		}
	}
}
