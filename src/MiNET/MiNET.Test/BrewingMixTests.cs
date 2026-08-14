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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2026 Niclas Olofsson.
// All Rights Reserved.

#endregion

using fNbt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.BlockEntities;
using MiNET.Items;

namespace MiNET.Test
{
	[TestClass]
	public class BrewingMixTests
	{
		// The brew a standing bottle takes from an ingredient. The registry is the embedded vanilla
		// recipe set, so the metas asserted here are the legacy potion ids the client also uses:
		// 0 water, 4 awkward, 5 night vision, 31 strength.

		[TestMethod]
		public void WaterBottlePlusNetherWart_BecomesAwkward()
		{
			Item result = BrewingStandBlockEntity.FindMix(Potion(0), Item("minecraft:nether_wart"));

			Assert.IsNotNull(result);
			Assert.AreEqual("minecraft:potion", result.Name);
			Assert.AreEqual((short) 4, result.Metadata);
		}

		[TestMethod]
		public void AwkwardPlusBlazePowder_BecomesStrength()
		{
			Item result = BrewingStandBlockEntity.FindMix(Potion(4), Item("minecraft:blaze_powder"));

			Assert.IsNotNull(result);
			Assert.AreEqual("minecraft:potion", result.Name);
			Assert.AreEqual((short) 31, result.Metadata);
		}

		[TestMethod]
		public void AwkwardPlusGoldenCarrot_BecomesNightVision()
		{
			Item result = BrewingStandBlockEntity.FindMix(Potion(4), Item("minecraft:golden_carrot"));

			Assert.IsNotNull(result);
			Assert.AreEqual("minecraft:potion", result.Name);
			Assert.AreEqual((short) 5, result.Metadata);
		}

		[TestMethod]
		public void Gunpowder_TurnsAnyPotionSplash_KeepingTheMeta()
		{
			Item result = BrewingStandBlockEntity.FindMix(Potion(5), Item("minecraft:gunpowder"));

			Assert.IsNotNull(result);
			Assert.AreEqual("minecraft:splash_potion", result.Name);
			Assert.AreEqual((short) 5, result.Metadata);
		}

		[TestMethod]
		public void Gunpowder_TurnsWaterSplash()
		{
			Item result = BrewingStandBlockEntity.FindMix(Potion(0), Item("minecraft:gunpowder"));

			Assert.IsNotNull(result);
			Assert.AreEqual("minecraft:splash_potion", result.Name);
			Assert.AreEqual((short) 0, result.Metadata);
		}

		[TestMethod]
		public void DragonBreath_TurnsSplashLingering_KeepingTheMeta()
		{
			Item result = BrewingStandBlockEntity.FindMix(Item("minecraft:splash_potion", 5), Item("minecraft:dragon_breath"));

			Assert.IsNotNull(result);
			Assert.AreEqual("minecraft:lingering_potion", result.Name);
			Assert.AreEqual((short) 5, result.Metadata);
		}

		[TestMethod]
		public void AnIngredientWithNoMix_ReturnsNull()
		{
			Assert.IsNull(BrewingStandBlockEntity.FindMix(Potion(4), Item("minecraft:stick")));
		}

		[TestMethod]
		public void AnEmptyBottle_ReturnsNull()
		{
			Assert.IsNull(BrewingStandBlockEntity.FindMix(new ItemAir(), Item("minecraft:nether_wart")));
		}

		[TestMethod]
		public void AnEmptyIngredient_ReturnsNull()
		{
			Assert.IsNull(BrewingStandBlockEntity.FindMix(Potion(4), new ItemAir()));
		}

		[TestMethod]
		public void TheFactoryBuildsTypedPotions()
		{
			// The drink flow lives in ItemPotion; a generic Item here means the factory's
			// parameterless-constructor requirement silently degraded the potion.
			Assert.IsInstanceOfType(ItemFactory.GetItemByName("minecraft:potion", 5, 1), typeof(ItemPotion));
			Assert.IsInstanceOfType(ItemFactory.GetItemByName("minecraft:potion", 0, 1), typeof(ItemPotion));
			Assert.AreEqual(1, ItemFactory.GetItemByName("minecraft:potion", 5, 1).MaxStackSize);
		}

		[TestMethod]
		public void TheFactoryBuildsTypedThrownPotions()
		{
			Assert.IsInstanceOfType(ItemFactory.GetItemByName("minecraft:splash_potion", 0, 1), typeof(ItemSplashPotion));
			Assert.IsInstanceOfType(ItemFactory.GetItemByName("minecraft:lingering_potion", 0, 1), typeof(ItemLingeringPotion));
		}

		[TestMethod]
		public void ThePotionRegistryEntryCarriesTheUseDuration()
		{
			// The client drinks for as long as the item's use_duration component says; without it
			// the drink is over in a fraction of the vanilla 1.6s.
			Assert.IsTrue(ItemFactory.ItemRegistry.TryGetByName("minecraft:potion", out ItemRegistryEntry entry));
			Assert.IsNotNull(entry.ComponentNbt);

			var file = new NbtFile {BigEndian = false, UseVarInt = true};
			file.LoadFromBuffer(entry.ComponentNbt, 0, entry.ComponentNbt.Length, NbtCompression.None);
			var components = (NbtCompound) file.RootTag["components"];
			Assert.AreEqual(1.6f, components["minecraft:use_duration"].FloatValue, 0.001f);
		}

		private static Item Potion(short meta)
		{
			return Item("minecraft:potion", meta);
		}

		private static Item Item(string name, short meta = 0)
		{
			return ItemFactory.GetItemByName(name, meta, 1);
		}
	}
}
