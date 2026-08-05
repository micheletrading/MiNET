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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Items;

namespace MiNET.Test.Items
{
	[TestClass]
	public class BlockItemIdentityTests
	{
		// These items name the block they place instead of carrying its pre-flattening id. A wrong
		// name resolves to null and the item places nothing, with no error, so the pairing is worth
		// pinning: an item whose block went missing is silent in every other way.

		[TestMethod]
		public void EachBlockItem_PlacesTheBlockItNames()
		{
			var expected = new Dictionary<ItemBlock, string>
			{
				[new ItemBed()] = "minecraft:bed",
				[new ItemCauldron()] = "minecraft:cauldron",
				[new ItemFrame()] = "minecraft:frame",
				[new ItemWheatSeeds()] = "minecraft:wheat",
				[new ItemBeetrootSeeds()] = "minecraft:beetroot"
			};

			foreach ((ItemBlock item, string blockName) in expected)
			{
				Assert.IsNotNull(item.Block, $"{item.GetType().Name} has no block, so it places nothing");
				Assert.AreEqual(blockName, item.Block.Name, $"{item.GetType().Name} places the wrong block");
			}
		}

		// A sign is two blocks and the face decides which, so both halves have to resolve.
		[TestMethod]
		public void SignVariants_ResolveBothHalves()
		{
			foreach (ItemBlock sign in new ItemBlock[]
			{
				new ItemSign(), new ItemAcaciaSign(), new ItemSpruceSign(), new ItemBirchSign(),
				new ItemJungleSign(), new ItemDarkoakSign(), new ItemCrimsonSign(), new ItemWarpedSign()
			})
			{
				string name = sign.GetType().Name;
				Assert.IsNotNull(MiNET.Blocks.BlockFactory.GetBlockByName(StandingNameOf(name)), $"{name} standing half missing");
				Assert.IsNotNull(MiNET.Blocks.BlockFactory.GetBlockByName(WallNameOf(name)), $"{name} wall half missing");
			}
		}

		private static string StandingNameOf(string itemClass) => SignBlockName(itemClass, "standing_sign");
		private static string WallNameOf(string itemClass) => SignBlockName(itemClass, "wall_sign");

		private static string SignBlockName(string itemClass, string suffix)
		{
			string wood = itemClass.Replace("Item", "").Replace("Sign", "").ToLowerInvariant();
			return wood.Length == 0 ? $"minecraft:{suffix}" : $"minecraft:{wood}_{suffix}";
		}
	}
}
