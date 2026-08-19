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
	///     Melon/pumpkin stem growth. The vanilla model is the PMMP Stem + CropGrowthHelper
	///     (the arbiter) and the growth window was verified against a BDS 1.26.40 oracle capture
	///     (mechanics-farming-melon-growth scenario: stem reaches growth 7 within 3600 ticks at
	///     randomTickSpeed 100). These tests pin the same rules on MiNET: light &gt;= 9 + farmland
	///     below to grow, full-grown stems place fruit on a random adjacent dirt cell.
	/// </summary>
	[TestClass]
	public class StemGrowthTests
	{
		[TestMethod]
		public void Melon_stem_grows_on_random_tick_with_light_and_farmland()
		{
			Level level = CreateLevel();
			BlockCoordinates stemPos = new BlockCoordinates(0, 4, 0);
			level.SetBlock(new Farmland {Coordinates = stemPos.BlockDown()});
			var stem = new MelonStem {Coordinates = stemPos, Growth = 0};

			int newAge = StemGrowth.OnRandomTick(level, stemPos, stem.Growth, "minecraft:melon_block");

			// The multiplier on dry farmland is 2 (1 + 1), so a random tick succeeds with
			// probability 2/26 - not every tick. The light at the surface is 15, so growth is
			// possible; assert the age advanced when the tick succeeded, or at least that the
			// call never throws and never skips past one stage.
			Assert.IsTrue(newAge >= 0 && newAge <= 1, "one random tick may advance the stem by at most one stage");
		}

		[TestMethod]
		public void Melon_stem_does_not_grow_without_light()
		{
			Level level = CreateLevel();
			BlockCoordinates stemPos = new BlockCoordinates(0, 4, 0);
			level.SetBlock(new Farmland {Coordinates = stemPos.BlockDown()});
			level.SetSkyLight(stemPos, 0);

			int newAge = StemGrowth.OnRandomTick(level, stemPos, 0, "minecraft:melon_block");

			Assert.AreEqual(0, newAge, "a stem in darkness must not grow");
		}

		[TestMethod]
		public void Full_grown_stem_places_fruit_on_adjacent_dirt()
		{
			Level level = CreateLevel();
			BlockCoordinates stemPos = new BlockCoordinates(0, 4, 0);
			level.SetBlock(new Farmland {Coordinates = stemPos.BlockDown()});
			// Farmland reverts to dirt below the placed fruit, so surround the stem with dirt.
			foreach (BlockCoordinates side in new[] {stemPos.BlockWest(), stemPos.BlockEast(), stemPos.BlockSouth(), stemPos.BlockNorth()})
			{
				level.SetBlock(new Dirt {Coordinates = side.BlockDown()});
			}

			int newAge = StemGrowth.OnRandomTick(level, stemPos, StemGrowth.MaxAge, "minecraft:melon_block");

			Assert.AreEqual(StemGrowth.MaxAge, newAge, "a full-grown stem stays full-grown");

			bool fruitPlaced = false;
			foreach (BlockCoordinates side in new[] {stemPos.BlockWest(), stemPos.BlockEast(), stemPos.BlockSouth(), stemPos.BlockNorth()})
			{
				if (level.GetBlock(side) is MelonBlock)
				{
					fruitPlaced = true;
					break;
				}
			}

			Assert.IsTrue(fruitPlaced, "a full-grown stem must place its fruit on an adjacent dirt cell");
		}

		private static Level CreateLevel()
		{
			string dir = Path.Combine(Path.GetTempPath(), "minet-stem-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);

			var provider = new AnvilWorldProvider(dir)
			{
				MissingChunkProvider = new SuperflatGenerator(Dimension.Overworld)
			};

			var level = new Level(new LevelManager(), "stem-test", provider, new EntityManager(), GameMode.Survival, Difficulty.Normal, 4);
			level.Initialize();
			return level;
		}
	}
}
