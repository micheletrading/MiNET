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

using System;
using System.Collections.Generic;
using log4net;
using MiNET.Net;
using MiNET.Utils.Vectors;
using Container = MiNET.Net.FullContainerName.ContainerEnumName;

namespace MiNET
{
	/// <summary>What a player has open. One screen at a time, and there is always one: closing
	/// everything leaves <see cref="ScreenKind.Inventory" />.</summary>
	public enum ScreenKind
	{
		/// <summary>The player's own inventory. Cursor and the 2x2 grid, no block behind it.</summary>
		Inventory,

		/// <summary>Chest, shulker box, barrel. Storage that lives in the block entity.</summary>
		Container,

		Furnace,
		BlastFurnace,
		EnchantingTable,
		Anvil,
		Horse
	}

	/// <summary>Where a container name's items actually live.</summary>
	public enum SlotStore
	{
		/// <summary>The flat UI window (0x7c). Client-side scratch with no persistence: crafting
		/// grid, anvil and smithing inputs, enchanting, loom, grindstone, stonecutter, cartography,
		/// the cursor and the created output. Addressed by absolute index into that one window,
		/// which is why several container names share it.</summary>
		Ui,

		/// <summary>The player's own 45 slots, hotbar first.</summary>
		Main,

		/// <summary>Helmet, chest, leggings, boots.</summary>
		Armor,

		Offhand,

		/// <summary>The open screen's block inventory.</summary>
		Block,

		/// <summary>Named by the client but with nowhere to put the items. Addressing one is an
		/// error, never a silent drop.</summary>
		Unsupported
	}

	/// <summary>A container name resolved against the open screen: which store, and which index
	/// within it.</summary>
	public readonly struct SlotBinding
	{
		public SlotStore Store { get; }
		public int Index { get; }

		public SlotBinding(SlotStore store, int index)
		{
			Store = store;
			Index = index;
		}
	}

	public class Screen
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Screen));

		/// <summary>Container name to store. A single-slot container carries its index here, because
		/// the name alone fixes the slot and the client's slot number is then redundant. A
		/// multi-slot container has no index and passes the client's slot through.</summary>
		private static readonly Dictionary<Container, SlotBinding> Bindings = new Dictionary<Container, SlotBinding>
		{
			// The player's own storage. Present whatever screen is open.
			[Container.Combinedhotbarandinventorycontainer] = new SlotBinding(SlotStore.Main, -1),
			[Container.Hotbarcontainer] = new SlotBinding(SlotStore.Main, -1),
			[Container.Inventorycontainer] = new SlotBinding(SlotStore.Main, -1),
			[Container.Armorcontainer] = new SlotBinding(SlotStore.Armor, -1),
			[Container.Offhandcontainer] = new SlotBinding(SlotStore.Offhand, 0),

			// Block storage, addressed by absolute slot in the block entity's item list.
			[Container.Levelentitycontainer] = new SlotBinding(SlotStore.Block, -1),
			[Container.Shulkerboxcontainer] = new SlotBinding(SlotStore.Block, -1),
			[Container.Barrelcontainer] = new SlotBinding(SlotStore.Block, -1),

			// Furnace family. Each name is one slot, and the index is the block entity's own
			// ordering: 0 smelts, 1 burns, 2 holds the result.
			[Container.Furnaceingredientcontainer] = new SlotBinding(SlotStore.Block, 0),
			[Container.Blastfurnaceingredientcontainer] = new SlotBinding(SlotStore.Block, 0),
			[Container.Smokeringredientcontainer] = new SlotBinding(SlotStore.Block, 0),
			[Container.Furnacefuelcontainer] = new SlotBinding(SlotStore.Block, 1),
			[Container.Furnaceresultcontainer] = new SlotBinding(SlotStore.Block, 2),

			// Everything below is the flat UI window. None of it persists and none of it is shared,
			// so two players at one anvil never collide.
			[Container.Cursorcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Createdoutputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Craftinginputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Craftingoutputpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Anvilinputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Anvilmaterialcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Anvilresultpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Smithingtableinputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Smithingtablematerialcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Smithingtabletemplatecontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Smithingtableresultpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Enchantinginputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Enchantingmaterialcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Grindstoneinputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Grindstoneadditionalcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Grindstoneresultpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Stonecutterinputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Stonecutterresultpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Loominputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Loomdyecontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Loommaterialcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Loomresultpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Cartographyinputcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Cartographyadditionalcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Cartographyresultpreviewcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipeconstructioncontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipenaturecontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipeitemscontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipesearchcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipesearchbarcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipeequipmentcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipebookcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipefoodcontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipeblockscontainer] = new SlotBinding(SlotStore.Ui, -1),
			[Container.Recipefurnaceitemscontainer] = new SlotBinding(SlotStore.Ui, -1),

			// Screens whose contents have to be stored server-side, and are not. Listed so the
			// reason is "no storage yet" rather than "name unknown".
			[Container.Beaconpaymentcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Brewingstandinputcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Brewingstandfuelcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Brewingstandresultcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Horseequipcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Crafterlevelentitycontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Dynamiccontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Tradeingredient1container] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Tradeingredient2container] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Traderesultpreviewcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Trade2ingredient1container] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Trade2ingredient2container] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Trade2resultpreviewcontainer] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Compoundcreatorinput] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Compoundcreatoroutputpreview] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Elementconstructoroutputpreview] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Materialreducerinput] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Materialreduceroutput] = new SlotBinding(SlotStore.Unsupported, -1),
			[Container.Labtableinput] = new SlotBinding(SlotStore.Unsupported, -1)
		};

		public ScreenKind Kind { get; }

		/// <summary>The block the screen belongs to, or <see cref="BlockCoordinates.Zero" /> for a
		/// screen with no block (the player's own inventory, a horse).</summary>
		public BlockCoordinates Coordinates { get; }

		/// <summary>Storage behind the screen, shared with every other player looking at it. Null for
		/// a screen whose slots are all scratch.</summary>
		public IInventory Backing { get; }

		public Inventory BlockInventory => Backing as Inventory;

		public Screen(ScreenKind kind) : this(kind, BlockCoordinates.Zero, null)
		{
		}

		public Screen(ScreenKind kind, IInventory backing) : this(kind, BlockCoordinates.Zero, backing)
		{
		}

		public Screen(ScreenKind kind, BlockCoordinates coordinates, IInventory backing)
		{
			Kind = kind;
			Coordinates = coordinates;
			Backing = backing;
		}

		/// <summary>Resolves a container name and the slot the client sent into the store that holds
		/// the item. Throws rather than guessing: an unresolvable container gets an error response and
		/// a resync, where returning null would make the item vanish client-side.</summary>
		public SlotBinding Bind(Container container, int slot)
		{
			if (!Bindings.TryGetValue(container, out SlotBinding binding))
			{
				throw new InvalidOperationException($"Unknown container: {container}");
			}

			switch (binding.Store)
			{
				case SlotStore.Unsupported:
					throw new InvalidOperationException($"Container {container} has no server-side storage");
				case SlotStore.Block when BlockInventory == null:
					throw new InvalidOperationException($"Container {container} addressed with no block inventory open (screen is {Kind})");
			}

			if (binding.Index < 0) return new SlotBinding(binding.Store, slot);

			// A single-slot container is addressed by name, so the client's slot number is redundant
			// and ignored. Whether the client counts within the container or across the window is
			// unverified, and this is the line that settles it the first time one is clicked.
			if (slot != binding.Index && Log.IsDebugEnabled) Log.Debug($"Container {container} is fixed to slot {binding.Index}, client addressed it as {slot}");

			return binding;
		}
	}
}
