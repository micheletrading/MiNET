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

using fNbt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.BlockEntities.Upgrade;

namespace MiNET.Test
{
	/// <summary>
	///     The block entity's own fields, as opposed to what it contains. A current client reads the
	///     current shape and nothing else, so a tile left in an older one is not half right: the sign
	///     is blank, the spawner is empty, the pot has nothing in it. Every case here is taken from a
	///     world in the regression corpus rather than invented.
	/// </summary>
	[TestClass]
	public class LegacyBlockEntityUpgradeTests
	{
		[TestMethod]
		public void SignText_MovesIntoTheFrontFace()
		{
			// 1.2 through 1.19.80 stored one face flat. Signs gained a back in 1.19.80 and the text
			// went into FrontText with it.
			var sign = new NbtCompound("Sign")
			{
				new NbtString("id", "Sign"),
				new NbtString("Text", "hello\nthere"),
				new NbtInt("SignTextColor", -16777216),
				new NbtByte("IgnoreLighting", 1),
				new NbtByte("TextIgnoreLegacyBugResolved", 1)
			};

			BlockEntityUpgrader.Upgrade(sign);

			Assert.IsNull(sign["Text"], "the flat text is still there");
			Assert.AreEqual("hello\nthere", ((NbtCompound) sign["FrontText"])["Text"].StringValue);
			Assert.AreEqual(1, ((NbtCompound) sign["FrontText"])["IgnoreLighting"].ByteValue);
			Assert.IsNotNull(sign["BackText"]);
			Assert.AreEqual(0, sign["IsWaxed"].ByteValue);
		}

		[TestMethod]
		public void SignLines_BecomeOneBlockOfText()
		{
			// Before 1.2 each line was its own tag, and trailing empty lines are not text.
			var sign = new NbtCompound("Sign")
			{
				new NbtString("id", "Sign"),
				new NbtString("Text1", "first"),
				new NbtString("Text2", "second"),
				new NbtString("Text3", ""),
				new NbtString("Text4", "")
			};

			BlockEntityUpgrader.Upgrade(sign);

			Assert.AreEqual("first\nsecond", ((NbtCompound) sign["FrontText"])["Text"].StringValue);
		}

		[TestMethod]
		public void SignGlow_IsNotReadUntilTheLightingBugWasResolved()
		{
			// IgnoreLighting only means glowing text once the flag beside it says so. Reading it
			// unconditionally lights up every old sign.
			var sign = new NbtCompound("Sign")
			{
				new NbtString("id", "Sign"),
				new NbtString("Text", "plain"),
				new NbtByte("IgnoreLighting", 1)
			};

			BlockEntityUpgrader.Upgrade(sign);

			Assert.AreEqual(0, ((NbtCompound) sign["FrontText"])["IgnoreLighting"].ByteValue);
		}

		[TestMethod]
		public void SpawnerEntityId_BecomesAnIdentifier()
		{
			// A bare legacy type id, as 1.6 wrote it.
			var spawner = new NbtCompound("MobSpawner")
			{
				new NbtString("id", "MobSpawner"),
				new NbtInt("EntityId", 32)
			};

			BlockEntityUpgrader.Upgrade(spawner);

			Assert.IsNull(spawner["EntityId"]);
			Assert.AreEqual("minecraft:zombie", spawner["EntityIdentifier"].StringValue);
		}

		[TestMethod]
		public void PackedSpawnerEntityId_IsReadFromItsLowByte()
		{
			// 1.2 and 1.6 worlds hold the id packed into a larger int. 0x110b22 is the skeleton, and
			// every packed value in the corpus decodes this way and matches the mobs the newer worlds
			// name outright.
			var spawner = new NbtCompound("MobSpawner")
			{
				new NbtString("id", "MobSpawner"),
				new NbtInt("EntityId", 0x110b22)
			};

			BlockEntityUpgrader.Upgrade(spawner);

			Assert.AreEqual("minecraft:skeleton", spawner["EntityIdentifier"].StringValue);
		}

		[TestMethod]
		public void ExistingIdentifier_IsLeftAlone()
		{
			var spawner = new NbtCompound("MobSpawner")
			{
				new NbtString("id", "MobSpawner"),
				new NbtString("EntityIdentifier", "minecraft:cave_spider")
			};

			BlockEntityUpgrader.Upgrade(spawner);

			Assert.AreEqual("minecraft:cave_spider", spawner["EntityIdentifier"].StringValue);
		}

		[TestMethod]
		public void FurnaceExperience_MovesToTheWiderField()
		{
			// StoredXP outgrew its short in 1.16.100 and became StoredXPInt.
			var furnace = new NbtCompound("Furnace")
			{
				new NbtString("id", "Furnace"),
				new NbtShort("StoredXP", 12)
			};

			BlockEntityUpgrader.Upgrade(furnace);

			Assert.IsNull(furnace["StoredXP"]);
			Assert.AreEqual(12, furnace["StoredXPInt"].IntValue);
		}

		[TestMethod]
		public void PottedPlant_IsUpgradedLikeAnyOtherBlock()
		{
			// A flower pot keeps what is planted in it as a stored blockstate, and before 1.13 that
			// is a name and a numeric val. Left alone, the pot holds a block the palette lost.
			var pot = new NbtCompound("FlowerPot")
			{
				new NbtString("id", "FlowerPot"),
				new NbtCompound("PlantBlock")
				{
					new NbtString("name", "minecraft:yellow_flower"),
					new NbtShort("val", 0)
				}
			};

			BlockEntityUpgrader.Upgrade(pot);

			var plant = (NbtCompound) pot["PlantBlock"];
			Assert.AreEqual("minecraft:dandelion", plant["name"].StringValue);
			Assert.IsNull(plant["val"], "the legacy value is still there");
			Assert.IsNotNull(plant["states"]);
		}
	}
}
