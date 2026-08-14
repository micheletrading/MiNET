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

			Effect e = GetPotionEffect(Metadata);

			if (e != null)
			{
				Log.Debug($"Consume applies {e}");
				player.SetEffect(e);
			}
			else
			{
				Log.Debug($"Consume found no effect for metadata {Metadata}");
			}
		}

		/// <summary>
		///     The effect a potion of the given metadata carries, or null for water and the mundane
		///     types. Shared by the drink and the thrown potion, so a splash applies the same
		///     effect it would have given as a drink.
		/// </summary>
		internal static Effect GetPotionEffect(short metadata)
		{
			Effect e = null;
			switch (metadata)
			{
				case 5:
					e = new NightVision
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 6:
					e = new NightVision
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 7:
					e = new Invisibility
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 8:
					e = new Invisibility
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 9:
					e = new JumpBoost
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 10:
					e = new JumpBoost
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 11:
					e = new JumpBoost
					{
						Duration = 1800,
						Level = 1
					};
					break;
				case 12:
					e = new FireResistance
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 13:
					e = new FireResistance
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 14:
					e = new Speed
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 15:
					e = new Speed
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 16:
					e = new Speed
					{
						Duration = 1800,
						Level = 1
					};
					break;
				case 17:
					e = new Slowness
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 18:
					e = new Slowness
					{
						Duration = 4800,
						Level = 0
					};
					break;
				case 19:
					e = new WaterBreathing
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 20:
					e = new WaterBreathing
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 21:
					e = new InstantHealth
					{
						Duration = 0,
						Level = 0
					};
					break;
				case 22:
					e = new InstantHealth
					{
						Duration = 0,
						Level = 1
					};
					break;
				case 23:
					e = new InstantDamage
					{
						Duration = 0,
						Level = 0
					};
					break;
				case 24:
					e = new InstantDamage
					{
						Duration = 0,
						Level = 1
					};
					break;
				case 25:
					e = new Poison
					{
						Duration = 900,
						Level = 0
					};
					break;
				case 26:
					e = new Poison
					{
						Duration = 2400,
						Level = 0
					};
					break;
				case 27:
					e = new Poison
					{
						Duration = 440,
						Level = 1
					};
					break;
				case 28:
					e = new Regeneration
					{
						Duration = 900,
						Level = 0
					};
					break;
				case 29:
					e = new Regeneration
					{
						Duration = 2400,
						Level = 0
					};
					break;
				case 30:
					e = new Regeneration
					{
						Duration = 440,
						Level = 1
					};
					break;
				case 31:
					e = new Strength
					{
						Duration = 3600,
						Level = 0
					};
					break;
				case 32:
					e = new Strength
					{
						Duration = 9600,
						Level = 0
					};
					break;
				case 33:
					e = new Strength
					{
						Duration = 1800,
						Level = 1
					};
					break;
				case 34:
					e = new Weakness
					{
						Duration = 1800,
						Level = 0
					};
					break;
				case 35:
					e = new Weakness
					{
						Duration = 4800,
						Level = 0
					};
					break;
			}

			return e;
		}
	}
}