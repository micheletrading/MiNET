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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Effects;
using MiNET.Items;

namespace MiNET.Test.Items
{
	[TestClass]
	public class PotionTests
	{
		// The factory resolves typed items by reflection, and that lookup requires a
		// parameterless constructor. Without one, "minecraft:potion" silently falls back to a
		// generic Item and drinking does nothing at all.
		[TestMethod]
		public void Potion_ResolvesToTypedItem()
		{
			Item item = ItemFactory.GetItemByName("minecraft:potion", 5);

			Assert.IsInstanceOfType(item, typeof(ItemPotion), "minecraft:potion did not resolve to ItemPotion, so the drink logic is unreachable");
			Assert.AreEqual(5, item.Metadata);
		}

		// Bedrock potion metadata runs 0..46 at this protocol; 0..4 are the effectless base
		// potions (water, mundane, long mundane, thick, awkward). Everything above must map to
		// at least one effect or the potion drinks as water.
		[TestMethod]
		public void EveryEffectPotionVariant_HasEffects()
		{
			for (short metadata = 0; metadata <= 46; metadata++)
			{
				Effect[] effects = ItemPotion.GetEffects(metadata);
				if (metadata <= 4)
				{
					Assert.AreEqual(0, effects.Length, $"base potion {metadata} should have no effect");
				}
				else
				{
					Assert.IsTrue(effects.Length > 0, $"potion metadata {metadata} maps to no effect");
				}
			}
		}

		// Turtle master is the one potion that applies two effects at once; a single-effect
		// table shape silently drops one of them.
		[TestMethod]
		public void TurtleMaster_GivesSlownessAndResistance()
		{
			Effect[] regular = ItemPotion.GetEffects(37);
			Assert.AreEqual(3, regular.OfType<Slowness>().Single().Level, "regular turtle master is Slowness IV");
			Assert.AreEqual(2, regular.OfType<Resistance>().Single().Level, "regular turtle master is Resistance III");
			Assert.IsTrue(regular.All(e => e.Duration == 400), "regular turtle master lasts 0:20");

			Effect[] strong = ItemPotion.GetEffects(39);
			Assert.AreEqual(5, strong.OfType<Slowness>().Single().Level, "strong turtle master is Slowness VI");
			Assert.AreEqual(3, strong.OfType<Resistance>().Single().Level, "strong turtle master is Resistance IV");
			Assert.IsTrue(strong.All(e => e.Duration == 400), "strong turtle master lasts 0:20");
		}

		[TestMethod]
		public void Decay_GivesWitherTwo()
		{
			Effect effect = ItemPotion.GetEffects(36).Single();
			Assert.IsInstanceOfType(effect, typeof(Wither));
			Assert.AreEqual(1, effect.Level, "decay is Wither II");
			Assert.AreEqual(800, effect.Duration, "decay lasts 0:40");
		}

		[TestMethod]
		public void SlowFalling_RegularAndLong()
		{
			Effect regular = ItemPotion.GetEffects(40).Single();
			Assert.IsInstanceOfType(regular, typeof(SlowFalling));
			Assert.AreEqual(1800, regular.Duration, "slow falling lasts 1:30");

			Effect extended = ItemPotion.GetEffects(41).Single();
			Assert.IsInstanceOfType(extended, typeof(SlowFalling));
			Assert.AreEqual(4800, extended.Duration, "long slow falling lasts 4:00");
		}

		[TestMethod]
		public void StrongSlowness_IsSlownessFour()
		{
			Effect effect = ItemPotion.GetEffects(42).Single();
			Assert.IsInstanceOfType(effect, typeof(Slowness));
			Assert.AreEqual(3, effect.Level, "strong slowness is Slowness IV");
			Assert.AreEqual(400, effect.Duration, "strong slowness lasts 0:20");
		}

		// The 1.21 Tricky Trials potions, metadata 43..46, each 3:00 at level I.
		[TestMethod]
		public void TrickyTrialsPotions_MapToTheirEffects()
		{
			Assert.IsInstanceOfType(ItemPotion.GetEffects(43).Single(), typeof(WindCharged));
			Assert.IsInstanceOfType(ItemPotion.GetEffects(44).Single(), typeof(Weaving));
			Assert.IsInstanceOfType(ItemPotion.GetEffects(45).Single(), typeof(Oozing));
			Assert.IsInstanceOfType(ItemPotion.GetEffects(46).Single(), typeof(Infested));

			for (short metadata = 43; metadata <= 46; metadata++)
			{
				Effect effect = ItemPotion.GetEffects(metadata).Single();
				Assert.AreEqual(3600, effect.Duration, $"potion {metadata} lasts 3:00");
				Assert.AreEqual(0, effect.Level, $"potion {metadata} is level I");
			}
		}

		// Regular slowness is 1:30, not 3:00; the old table had it doubled.
		[TestMethod]
		public void Slowness_RegularDuration()
		{
			Effect effect = ItemPotion.GetEffects(17).Single();
			Assert.IsInstanceOfType(effect, typeof(Slowness));
			Assert.AreEqual(1800, effect.Duration, "slowness lasts 1:30");
		}
	}
}
