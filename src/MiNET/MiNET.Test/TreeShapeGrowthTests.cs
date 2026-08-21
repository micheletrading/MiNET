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
using System.IO;
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
				int trunkY = wood == "mangrove" ? 5 : 1; // the mangrove trunk sits above its roots
				Assert.IsTrue(level.GetBlock(4, 4 + trunkY, 0) is LogBase, $"{wood} trunk must appear above the sapling");

				int leafCount = 0;
				for (int dx = 0; dx < 9; dx++)
				for (int dy = 2; dy < 12; dy++)
				for (int dz = -8; dz < 9; dz++)
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
						Assert.IsTrue(vine.VineDirectionBits > 0, $"{wood} vine must carry direction bits");
				}
				Assert.IsTrue(leafCount > 0, $"{wood} must produce leaves (cell 4,9,0 = {level.GetBlock(4, 9, 0)?.Name ?? "null"})");
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






