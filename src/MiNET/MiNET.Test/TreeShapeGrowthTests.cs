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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test
{
	/// <summary>
	///     The literal BDS-captured tree generators must grow every wood type from its sapling.
	/// </summary>
	[TestClass]
	public class TreeShapeGrowthTests
	{
		[TestMethod]
		public void Every_wood_type_grows_from_its_sapling()
		{
			foreach (string wood in new[] {"oak", "birch", "spruce", "jungle", "acacia", "cherry", "mangrove"})
			{
				Level level = CreateLevel();
				var sapling = (SaplingBase) BlockFactory.GetBlockByName($"minecraft:{wood}_sapling");
				if (sapling == null) sapling = (SaplingBase) BlockFactory.GetBlockByName($"minecraft:{wood}_propagule");
				sapling.Coordinates = new BlockCoordinates(4, 3, 0);
				level.SetBlock(sapling);

	bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					sapling.OnTick(level, true);
					grew = level.GetBlock(4, 3, 0) is not SaplingBase;
				}

				Assert.IsTrue(grew, $"{wood} sapling must grow");
				// The trunk base covers the sapling cell; the mangrove's propagule cell is air
				// after growth in BDS (its trunk starts above the roots).
				if (wood == "mangrove")
					Assert.IsTrue(level.GetBlock(4, 3, 0) is Air, $"{wood} propagule cell must be consumed");
				else
					Assert.IsTrue(!(level.GetBlock(4, 3, 0) is Air), $"{wood} base must cover the sapling cell");
				// The mangrove trunk starts 4 above the propagule in the literal shapes (roots
				// fill 1..3); the parametric family starts at 1 but always reaches 4 (min height
				// 4), so rel 4 is the single cell both families guarantee as log.
				int trunkY = wood == "mangrove" ? 4 : 1;
				Assert.IsTrue(level.GetBlock(4, 4 + trunkY, 0) is LogBase, $"{wood} trunk must appear above the sapling");

				int leafCount = 0;
				for (int dx = -12; dx < 13; dx++)
				for (int dy = 2; dy < 16; dy++)
				for (int dz = -12; dz < 13; dz++)
				{
					Block b = level.GetBlock(dx, dy, dz);
					if (b is LeavesBase)
					{
						leafCount++;
						int rt = b.GetRuntimeId();
						Block back = BlockFactory.GetBlockByRuntimeId(rt);
						Assert.IsTrue(back is LeavesBase, $"{wood} leaf runtime {rt} must round-trip to a leaf, got {back?.Name ?? "null"}");
					}
					if (b is Vine vine)
					{
						Assert.IsTrue(vine.VineDirectionBits > 0, $"{wood} vine must carry direction bits");
						int vrt = vine.GetRuntimeId();
						Block vback = BlockFactory.GetBlockByRuntimeId(vrt);
						Assert.IsTrue(vback is Vine, $"{wood} vine runtime {vrt} must round-trip to a vine, got {vback?.Name ?? "null"}");
					}
				}
				Assert.IsTrue(leafCount > 0, $"{wood} must produce leaves");
			}
		}

		[TestMethod]
		public void Parametric_trees_fall_back_to_literals_when_disabled()
		{
			var originalProvider = Config.Provider;
			try
			{
				Config.Provider = new TestConfigProvider(new Dictionary<string, string> {["ParametricTrees"] = "false"});
				for (int i = 0; i < 12; i++)
				{
					Level level = CreateLevel();
					var sapling = (SaplingBase) BlockFactory.GetBlockByName("minecraft:oak_sapling");
					sapling.Coordinates = new BlockCoordinates(4, 3, 0);
					level.SetBlock(sapling);

					bool grew = false;
					for (int t = 0; t < 200 && !grew; t++)
					{
						sapling.OnTick(level, true);
						grew = level.GetBlock(4, 3, 0) is not SaplingBase;
					}

					Assert.IsTrue(grew, "oak sapling must grow with ParametricTrees=false");
					Console.WriteLine($"DEBUG sapling.Coordinates={sapling.Coordinates} levelBlock={level.GetBlock(4, 3, 0)?.Name}");

					var grown = new HashSet<(int X, int Y, int Z, string Block)>();
					for (int dx = -8; dx <= 8; dx++)
					for (int dy = -2; dy <= 20; dy++)
					for (int dz = -8; dz <= 8; dz++)
					{
						Block b = level.GetBlock(4 + dx, 3 + dy, dz);
						if (b is Air or GrassBlock or Dirt or Bedrock) continue;
						if (b is SaplingBase) continue;
						grown.Add((dx, dy, dz, Normalize(b.Name)));
					}

					// With the flag off the growth must be byte-identical to one of the four
					// captured literal variants. Variants list both the top log and the leaf that
					// overwrites it; collapse to final state (logs first, then leaves) like the
					// placer does.
					bool matchesVariant = new OakTreeGenerator().Variants.Any(v =>
					{
						var final = new Dictionary<(int X, int Y, int Z), string>();
						foreach (var c in v)
							if (c.Block.EndsWith("_log")) final[(c.X, c.Y, c.Z)] = Normalize(c.Block);
						foreach (var c in v)
							if (!c.Block.EndsWith("_log")) final[(c.X, c.Y, c.Z)] = Normalize(c.Block);
						return final.Select(kv => (kv.Key.X, kv.Key.Y, kv.Key.Z, kv.Value)).ToHashSet().SetEquals(grown);
					});
					Assert.IsTrue(matchesVariant, $"grown shape must be one of the literal variants, got {grown.Count} cells: " + string.Join(" | ", new OakTreeGenerator().Variants.Select((v, vi) =>
					{
						var final = new Dictionary<(int X, int Y, int Z), string>();
						foreach (var c in v)
							if (c.Block.EndsWith("_log")) final[(c.X, c.Y, c.Z)] = Normalize(c.Block);
						foreach (var c in v)
							if (!c.Block.EndsWith("_log")) final[(c.X, c.Y, c.Z)] = Normalize(c.Block);
						var vSet = final.Select(kv => (kv.Key.X, kv.Key.Y, kv.Key.Z, kv.Value)).ToHashSet();
						var missing = vSet.Except(grown).OrderBy(c => c).ToList();
						var extra = grown.Except(vSet).OrderBy(c => c).ToList();
						return $"v{vi}: missing[{string.Join(",", missing)}] extra[{string.Join(",", extra)}]";
					})));
				}
			}
			finally
			{
				Config.Provider = originalProvider;
			}
		}

		[TestMethod]
		public void Dark_and_pale_oak_grow_only_from_a_2x2_patch()
		{
			foreach (string wood in new[] {"dark_oak", "pale_oak"})
			{
				Level level = CreateLevel();
				for (int dx = 0; dx < 2; dx++)
				{
					for (int dz = 0; dz < 2; dz++)
					{
						var sapling = (SaplingBase) BlockFactory.GetBlockByName($"minecraft:{wood}_sapling");
						sapling.Coordinates = new BlockCoordinates(4 + dx, 3, 4 + dz);
						level.SetBlock(sapling);
					}
				}

				var nw = (SaplingBase) level.GetBlock(4, 3, 4);
				bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					nw.OnTick(level, true);
					grew = level.GetBlock(4, 3, 4) is not SaplingBase;
				}

				Assert.IsTrue(grew, $"{wood} 2x2 patch must grow");
				Assert.IsTrue(level.GetBlock(4, 4, 4) is LogBase, $"{wood} trunk must appear above the patch corner");
				Assert.IsTrue(level.GetBlock(4, 4, 4).Name == $"minecraft:{wood}_log",
					$"{wood} trunk must be {wood} log, got {level.GetBlock(4, 4, 4).Name}");
			}
		}

		private static string Normalize(string blockName)
		{
			string name = blockName.Replace("minecraft:", "");
			int colon = name.IndexOf(':');
			return colon >= 0 ? name.Substring(0, colon) : name;
		}

		private sealed class TestConfigProvider : ConfigProvider
		{
			private readonly Dictionary<string, string> _values;

			public TestConfigProvider(Dictionary<string, string> values)
			{
				_values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
			}

			protected override void OnInitialize()
			{
			}

			public override string ReadString(string property)
			{
				// Config lowercases the property name before asking; the DefaultConfigProvider
				// lowercases keys on load, so lookups must be case-insensitive.
				return _values.TryGetValue(property, out string value) ? value : null;
			}
		}

		private static Level CreateLevel()
		{
			string dir = Path.Combine(Path.GetTempPath(), "minet-tree-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);

			var provider = new AnvilWorldProvider(dir)
			{
				MissingChunkProvider = new SuperflatGenerator(Dimension.Overworld)
			};

			var level = new Level(new LevelManager(), "tree-test", provider, new EntityManager(), GameMode.Survival, Difficulty.Normal, 4);
			level.Initialize();
			return level;
		}
	}
}






