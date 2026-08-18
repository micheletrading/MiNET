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
using fNbt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.BlockEntities.Upgrade;
using MiNET.Items.Upgrade;

namespace MiNET.Test
{
	/// <summary>
	///     What a chest saved years ago hands back today. A stack that fails to upgrade does not throw
	///     and does not disappear: it becomes an item with no registry entry, which goes out to the
	///     client as an empty slot, so the loss is silent in exactly the way an unread block was.
	/// </summary>
	[TestClass]
	public class LegacyItemUpgradeTests
	{
		[TestMethod]
		public void NumericIdAndMetadata_BecomeTheItemThatPairMeant()
		{
			// Up to Bedrock 1.5 a stack was a numeric id with the variant in Damage. Ids at or below
			// 255 are block ids, which is the overlap that makes this worth testing rather than
			// assuming.
			Assert.AreEqual("minecraft:yellow_stained_glass", ItemDataUpgrader.Upgrade(241, 4).Name);
			Assert.AreEqual("minecraft:hay_block", ItemDataUpgrader.Upgrade(170, 0).Name);
			Assert.AreEqual("minecraft:apple", ItemDataUpgrader.Upgrade(260, 0).Name);
		}

		[TestMethod]
		public void LegacyNameAndMetadata_BecomeTheFlattenedName()
		{
			// 1.6 through the flattening: the name is the family and Damage picks the member.
			Assert.AreEqual(("minecraft:yellow_stained_glass", 0), ItemDataUpgrader.Upgrade("minecraft:stained_glass", 4));
			Assert.AreEqual(("minecraft:dark_oak_fence", 0), ItemDataUpgrader.Upgrade("minecraft:fence", 5));
		}

		[TestMethod]
		public void RenamedItem_IsFoundWhateverTheStoredNameIsCasedLike()
		{
			// Worlds hold minecraft:glazedTerracotta.silver where the schema keys the rule in lower
			// case. Matching case-sensitively leaves the item unresolved and the slot empty.
			Assert.AreEqual("minecraft:silver_glazed_terracotta", ItemDataUpgrader.Upgrade("minecraft:glazedTerracotta.silver", 0).Name);
		}

		[TestMethod]
		public void DurabilityIsNotAVariant()
		{
			// A worn tool keeps its metadata: it is damage, not an identity, and treating it as one
			// would turn every used pickaxe into something else.
			(string Name, int Meta) upgraded = ItemDataUpgrader.Upgrade("minecraft:diamond_pickaxe", 37);

			Assert.AreEqual("minecraft:diamond_pickaxe", upgraded.Name);
			Assert.AreEqual(37, upgraded.Meta);
		}

		[TestMethod]
		public void BlockCompound_DecidesWhatABlockItemIs()
		{
			// Between 1.9 and the flattening the variant moved into the stack's Block compound while
			// Damage stayed zero, so reading the name and Damage alone yields the default variant:
			// every colour of glass in a chest would come back white.
			var stack = new NbtCompound
			{
				new NbtByte("Count", 64),
				new NbtShort("Damage", 0),
				new NbtString("Name", "minecraft:stained_glass"),
				new NbtCompound("Block")
				{
					new NbtString("name", "minecraft:stained_glass"),
					new NbtCompound("states") {new NbtString("color", "magenta")},
					new NbtInt("version", 17432626)
				}
			};

			BlockEntityUpgrader.Upgrade(new NbtCompound("block_entity") {new NbtList("Items", new List<NbtTag> {stack})});

			Assert.AreEqual("minecraft:magenta_stained_glass", stack["Name"].StringValue);
			Assert.AreEqual(0, stack["Damage"].ShortValue);
		}

		[TestMethod]
		public void UpgradedStacks_AreItemsTheServerCanHandOut()
		{
			// The end of the line: an upgraded name has to resolve to a registry entry, because an
			// item without a network id is sent as an empty slot.
			foreach (string name in new[] {"minecraft:yellow_stained_glass", "minecraft:silver_glazed_terracotta", "minecraft:dark_oak_fence", "minecraft:hay_block"})
			{
				Item item = ItemFactory.GetItemByName(name);

				Assert.IsNotNull(item, name);
				Assert.AreNotEqual(0, item.NetworkId, $"{name} has no network id, so it would go out as an empty slot");
			}
		}

		[TestMethod]
		public void LegacyStackNbt_IsRewrittenIntoTheCurrentShape()
		{
			// The numeric form as 1.5 wrote it. What leaves here rides inline with the chunk, so it
			// has to be the shape a current client reads, not the shape it was stored in.
			var stack = new NbtCompound
			{
				new NbtByte("Count", 64),
				new NbtShort("Damage", 4),
				new NbtByte("Slot", 0),
				new NbtShort("id", 241)
			};

			BlockEntityUpgrader.Upgrade(new NbtCompound("block_entity") {new NbtList("Items", new List<NbtTag> {stack})});

			Assert.IsNull(stack["id"], "the legacy numeric id is still there");
			Assert.AreEqual("minecraft:yellow_stained_glass", stack["Name"].StringValue);
			Assert.AreEqual(64, stack["Count"].ByteValue);
			Assert.AreEqual(0, stack["Slot"].ByteValue);
		}
	}
}
