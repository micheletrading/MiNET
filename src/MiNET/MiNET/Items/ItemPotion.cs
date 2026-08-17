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

using log4net;
using MiNET.Effects;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Items
{
	public class ItemPotion : Item
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ItemPotion));

		/// <summary>
		///     Parameterless so the typed-item factory can build the class by reflection (it skips
		///     classes without one and falls back to a generic Item, which is why a potion in hand
		///     behaved like a plain item). The factory sets Metadata right after construction.
		/// </summary>
		public ItemPotion() : this(0)
		{
		}

		/// <summary>Shared by the thrown variants, which are the same item with another name.</summary>
		protected ItemPotion(string name, short metadata) : base(name, metadata)
		{
			// Potions are unstackable: the item declares no max_stack_size component, which the
			// client reads as one per slot, and a bigger stack is an item the client refuses to
			// touch. The default Item.MaxStackSize of 64 is wrong for every potion kind.
			MaxStackSize = 1;
		}

		public ItemPotion(short metadata) : this("minecraft:potion", metadata)
		{
		}

		private bool _isUsing;

		public override void UseItem(Level world, Player player, BlockCoordinates blockCoordinates)
		{
			// The client finishes a drink with EntityEvent 57, not a second Use press, and its own
			// MobEquipment can replace this instance mid-drink, so the in-use state lives on the
			// player. Every press starts a fresh drink client-side; only the event completes one.
			if (player.ItemInUse != null) return;

			_isUsing = true;
			player.ItemInUse = this;
		}

		public override void CompleteUse(Player player)
		{
			Log.Debug($"CompleteUse {this} gamemode={player.GameMode} handSlot={player.Inventory.InHandSlot}");
			Consume(player);
			_isUsing = false;
			player.ItemInUse = null;
		}

		public override void Release(Level world, Player player, BlockCoordinates blockCoordinates)
		{
			if (player.ItemInUse == this) player.ItemInUse = null;
			_isUsing = false;
		}


		public virtual void Consume(Player player)
		{
			if (player.GameMode == GameMode.Survival || player.GameMode == GameMode.Adventure)
			{
				player.Inventory.ClearInventorySlot((byte) player.Inventory.InHandSlot);
				player.Inventory.SetFirstEmptySlot(ItemFactory.GetItemByName("minecraft:glass_bottle"), true);
				// The per-slot updates above are not applied by the 1.26 client to its own
				// inventory (the ItemStackNetManager owns it), so the drink would complete
				// server-side while the potion stays in the client's hand. A full content sync
				// is the path the client always honours.
				player.SendPlayerInventory();
			}

			Effect[] effects = GetEffects(Metadata);
			if (effects.Length == 0)
			{
				Log.Debug($"Consume found no effect for metadata {Metadata}");
				return;
			}

			foreach (Effect effect in effects)
			{
				Log.Debug($"Consume applies {effect}");
				player.SetEffect(effect);
			}
		}

		// The Bedrock potion table for this protocol, metadata 0..46. 0..4 are the effectless
		// base potions (water, mundane, long mundane, thick, awkward). Durations are ticks.
		// Fresh instances per call: an effect ticks its own duration down once applied.
		public static Effect[] GetEffects(short metadata)
		{
			return metadata switch
			{
				5 => new Effect[] {new NightVision {Duration = 3600, Level = 0}},
				6 => new Effect[] {new NightVision {Duration = 9600, Level = 0}},
				7 => new Effect[] {new Invisibility {Duration = 3600, Level = 0}},
				8 => new Effect[] {new Invisibility {Duration = 9600, Level = 0}},
				9 => new Effect[] {new JumpBoost {Duration = 3600, Level = 0}},
				10 => new Effect[] {new JumpBoost {Duration = 9600, Level = 0}},
				11 => new Effect[] {new JumpBoost {Duration = 1800, Level = 1}},
				12 => new Effect[] {new FireResistance {Duration = 3600, Level = 0}},
				13 => new Effect[] {new FireResistance {Duration = 9600, Level = 0}},
				14 => new Effect[] {new Speed {Duration = 3600, Level = 0}},
				15 => new Effect[] {new Speed {Duration = 9600, Level = 0}},
				16 => new Effect[] {new Speed {Duration = 1800, Level = 1}},
				17 => new Effect[] {new Slowness {Duration = 1800, Level = 0}},
				18 => new Effect[] {new Slowness {Duration = 4800, Level = 0}},
				19 => new Effect[] {new WaterBreathing {Duration = 3600, Level = 0}},
				20 => new Effect[] {new WaterBreathing {Duration = 9600, Level = 0}},
				21 => new Effect[] {new InstantHealth {Duration = 0, Level = 0}},
				22 => new Effect[] {new InstantHealth {Duration = 0, Level = 1}},
				23 => new Effect[] {new InstantDamage {Duration = 0, Level = 0}},
				24 => new Effect[] {new InstantDamage {Duration = 0, Level = 1}},
				25 => new Effect[] {new Poison {Duration = 900, Level = 0}},
				26 => new Effect[] {new Poison {Duration = 2400, Level = 0}},
				27 => new Effect[] {new Poison {Duration = 440, Level = 1}},
				28 => new Effect[] {new Regeneration {Duration = 900, Level = 0}},
				29 => new Effect[] {new Regeneration {Duration = 2400, Level = 0}},
				30 => new Effect[] {new Regeneration {Duration = 440, Level = 1}},
				31 => new Effect[] {new Strength {Duration = 3600, Level = 0}},
				32 => new Effect[] {new Strength {Duration = 9600, Level = 0}},
				33 => new Effect[] {new Strength {Duration = 1800, Level = 1}},
				34 => new Effect[] {new Weakness {Duration = 1800, Level = 0}},
				35 => new Effect[] {new Weakness {Duration = 4800, Level = 0}},
				36 => new Effect[] {new Wither {Duration = 800, Level = 1}},
				37 => new Effect[] {new Slowness {Duration = 400, Level = 3}, new Resistance {Duration = 400, Level = 2}},
				38 => new Effect[] {new Slowness {Duration = 800, Level = 3}, new Resistance {Duration = 800, Level = 2}},
				39 => new Effect[] {new Slowness {Duration = 400, Level = 5}, new Resistance {Duration = 400, Level = 3}},
				40 => new Effect[] {new SlowFalling {Duration = 1800, Level = 0}},
				41 => new Effect[] {new SlowFalling {Duration = 4800, Level = 0}},
				42 => new Effect[] {new Slowness {Duration = 400, Level = 3}},
				43 => new Effect[] {new WindCharged {Duration = 3600, Level = 0}},
				44 => new Effect[] {new Weaving {Duration = 3600, Level = 0}},
				45 => new Effect[] {new Oozing {Duration = 3600, Level = 0}},
				46 => new Effect[] {new Infested {Duration = 3600, Level = 0}},
				_ => new Effect[0]
			};
		}
	}
}
