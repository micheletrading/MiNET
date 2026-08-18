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
using MiNET.Utils.Vectors;
using MiNET.Worlds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test.Worlds
{
	/// <summary>
	///     Liquid flow regression: a placed liquid source (stationary OR flowing, the way a lava
	///     bucket pours) must convert to its flowing half and spread to neighbouring cells. Fires
	///     the scheduled tick directly, the same call the world tick makes for BlockWithTicks.
	/// </summary>
	[TestClass]
	public class FlowingLavaTests
	{
		private static Level CreateFlatLevel()
		{
			string dir = Path.Combine(Path.GetTempPath(), "minet-flow-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);

			var provider = new AnvilWorldProvider(dir)
			{
				MissingChunkProvider = new SuperflatGenerator(Dimension.Overworld)
			};

			var level = new Level(new LevelManager(), "flow-test", provider, new EntityManager(), GameMode.Survival, Difficulty.Normal, 4);
			level.Initialize();
			return level;
		}

		[TestMethod]
		public void Stationary_lava_source_spreads_to_neighbouring_cells()
		{
			Level level = CreateFlatLevel();
			var source = BlockFactory.GetBlockByName("minecraft:lava");
			source.Coordinates = new BlockCoordinates(5, 4, 0);
			level.SetBlock(source);

			for (int i = 0; i < 3; i++)
			{
				level.GetBlock(new BlockCoordinates(5, 4, 0)).OnTick(level, false);
			}

			Assert.IsInstanceOfType(level.GetBlock(new BlockCoordinates(5, 4, 1)), typeof(FlowingLava), "neighbour must become flowing lava");
		}

		[TestMethod]
		public void Flowing_lava_source_bucket_style_spreads_to_neighbouring_cells()
		{
			// A lava bucket pours minecraft:flowing_lava with LiquidDepth 0 (ItemBucket metadata 10).
			Level level = CreateFlatLevel();
			var source = BlockFactory.GetBlockByName("minecraft:flowing_lava");
			source.Coordinates = new BlockCoordinates(5, 4, 0);
			level.SetBlock(source);

			for (int i = 0; i < 3; i++)
			{
				level.GetBlock(new BlockCoordinates(5, 4, 0)).OnTick(level, false);
			}

			Assert.IsInstanceOfType(level.GetBlock(new BlockCoordinates(5, 4, 1)), typeof(FlowingLava), "neighbour must become flowing lava");
		}
	}
}
