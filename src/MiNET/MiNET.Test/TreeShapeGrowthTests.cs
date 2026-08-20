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
				int trunkY = wood == "mangrove" ? 5 : 1; // the mangrove trunk sits above its roots
				Assert.IsTrue(level.GetBlock(4, 4 + trunkY, 0) is LogBase, $"{wood} trunk must appear above the sapling");
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




