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

using System.Text;
using fNbt;
using log4net;
using MiNET.Blocks;
using MiNET.Crafting;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.BlockEntities
{
	/// <summary>
	///     The machine behind the five brewing stand slots: three bottles (0-2), the ingredient (3)
	///     and the blaze powder (4). The client places items and the server does the rest, the way
	///     BDS does it: a standing brew starts on its own the tick the slots form a valid mix, the
	///     ingredient is consumed up front, and 400 ticks (20s) later each bottle becomes its mix
	///     output. Any change to bottles, ingredient or fuel voids the standing brew without
	///     returning the ingredient, like vanilla.
	///     The client draws the progress bar and the fuel gauge from the ContainerSetData
	///     properties this sends: 0 brew time, 1 fuel amount, 2 fuel total.
	/// </summary>
	public class BrewingStandBlockEntity : ContainerBlockEntity
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BrewingStandBlockEntity));

		/// <summary>One brew, 20 seconds, the countdown BDS runs.</summary>
		private const short BrewDuration = 400;

		/// <summary>How many brews one blaze powder pays for.</summary>
		private const short FuelCapacity = 20;

		private static readonly int BlazePowderNetworkId = ItemFactory.GetNetworkIdByName("minecraft:blaze_powder");

		private string _slotFingerprint;
		private Item[] _results;
		private short _lastCookTime;
		private short _lastFuelAmount;
		private short _lastFuelTotal;

		public BrewingStandBlockEntity() : base("BrewingStand", 5,
			new NbtShort("CookTime", 0),
			new NbtShort("FuelAmount", 0),
			new NbtShort("FuelTotal", 0))
		{
			UpdatesOnTick = true;
		}

		private short CookTime
		{
			get => (short) (Compound["CookTime"]?.ShortValue ?? 0);
			set => Compound["CookTime"] = new NbtShort("CookTime", value);
		}

		private short FuelAmount
		{
			get => (short) (Compound["FuelAmount"]?.ShortValue ?? 0);
			set => Compound["FuelAmount"] = new NbtShort("FuelAmount", value);
		}

		private short FuelTotal
		{
			get => (short) (Compound["FuelTotal"]?.ShortValue ?? 0);
			set => Compound["FuelTotal"] = new NbtShort("FuelTotal", value);
		}

		public override void OnTick(Level level)
		{
			// The stand's chunk can be unloaded while this entity is still registered to tick.
			// Asking for anything in it would reload the column from disk on every tick, which
			// is a world stall for a machine that is not even in the loaded area. cacheOnly
			// answers from the cache, and a miss means the chunk is out and the brew can wait.
			ChunkColumn chunk = level.GetChunk(new ChunkCoordinates(Coordinates.X >> 4, Coordinates.Z >> 4), true);
			if (chunk == null) return;

			Inventory inventory = level.InventoryManager.GetInventory(Coordinates);
			if (inventory == null) return;

			string fingerprint = Fingerprint(inventory);
			if (fingerprint != _slotFingerprint)
			{
				// Bottles, ingredient or fuel changed: void the standing brew (the ingredient it
				// consumed is not coming back, the same as vanilla).
				_slotFingerprint = fingerprint;
				if (CookTime > 0)
				{
					CookTime = 0;
					_results = null;
				}
			}

			if (CookTime > 0)
			{
				CookTime--;
				if (CookTime <= 0)
				{
					ApplyResults(inventory);
					_results = null;
					BroadcastBrewed(level);
				}
			}
			else
			{
				TryStartBrew(inventory);
			}

			UpdateBottleBits(level, chunk, inventory);
			SendState(level, inventory);
		}

		/// <summary>
		///     Consumes the ingredient and one unit of fuel and starts the countdown, when at least one
		///     bottle mixes with the ingredient. Bottles the ingredient does nothing to are brewed
		///     unchanged, like BDS.
		/// </summary>
		private void TryStartBrew(Inventory inventory)
		{
			Item ingredient = inventory.Slots[3];
			if (ingredient.IsAir) return;

			var results = new Item[3];
			bool any = false;
			for (byte i = 0; i < 3; i++)
			{
				results[i] = FindMix(inventory.Slots[i], ingredient);
				if (results[i] != null) any = true;
			}

			if (!any) return;

			if (FuelAmount <= 0)
			{
				Item fuel = inventory.Slots[4];
				if (fuel.IsAir || fuel.NetworkId != BlazePowderNetworkId) return;
				inventory.DecreaseSlot(4);
				FuelAmount = FuelCapacity;
				FuelTotal = FuelCapacity;
			}

			inventory.DecreaseSlot(3);
			FuelAmount--;
			CookTime = BrewDuration;
			_results = results;

			// The ingredient and fuel just left the slots, which changes the fingerprint the
			// cancel-on-change guard below compares against on every tick. Sync it here, or the
			// very next tick reads the post-consume slots, thinks they changed, and voids the
			// brew it just started - consuming the ingredient and giving nothing back.
			_slotFingerprint = Fingerprint(inventory);

			if (Log.IsDebugEnabled) Log.Debug($"Brewing started at {Coordinates} with {ingredient}");
		}

		private void ApplyResults(Inventory inventory)
		{
			if (_results == null) return;

			for (byte i = 0; i < 3; i++)
			{
				Item result = _results[i];
				if (result == null) continue;
				inventory.SetSlot(null, i, result);
			}
		}

		/// <summary>
		///     The mix a bottle takes from the ingredient, or null when the ingredient does nothing to
		///     it. The type mixes are exact (potion id and meta, ingredient id and meta); the container
		///     mixes (gunpowder, dragon breath) apply to every bottle and keep its potion meta.
		/// </summary>
		internal static Item FindMix(Item bottle, Item ingredient)
		{
			if (bottle.IsAir || ingredient.IsAir) return null;

			foreach (PotionTypeRecipe recipe in RecipeManager.PotionTypeRecipes)
			{
				if (recipe.Input == bottle.NetworkId && recipe.InputMeta == bottle.Metadata
						&& recipe.Ingredient == ingredient.NetworkId && recipe.IngredientMeta == ingredient.Metadata)
				{
					return ItemFactory.GetItemByNetworkId(recipe.Output, (short) recipe.OutputMeta, 1);
				}
			}

			foreach (PotionContainerChangeRecipe recipe in RecipeManager.PotionContainerRecipes)
			{
				if (recipe.Input == bottle.NetworkId && recipe.Ingredient == ingredient.NetworkId)
				{
					return ItemFactory.GetItemByNetworkId(recipe.Output, bottle.Metadata, 1);
				}
			}

			return null;
		}

		/// <summary>The three slot bits the block carries, set from bottle presence.</summary>
		private void UpdateBottleBits(Level level, ChunkColumn chunk, Inventory inventory)
		{
			if (level.GetBlock(Coordinates, chunk) is not BrewingStand stand) return;

			bool a = !inventory.Slots[0].IsAir;
			bool b = !inventory.Slots[1].IsAir;
			bool c = !inventory.Slots[2].IsAir;
			if (stand.BrewingStandSlotABit == a && stand.BrewingStandSlotBBit == b && stand.BrewingStandSlotCBit == c) return;

			var updated = new BrewingStand
			{
				Coordinates = Coordinates,
				BrewingStandSlotABit = a,
				BrewingStandSlotBBit = b,
				BrewingStandSlotCBit = c
			};
			level.SetBlock(updated);
		}

		private void BroadcastBrewed(Level level)
		{
			// A sound event, not a level event: PotionBrewed (128) is a LevelSoundEventType value and
			// has no meaning as a level event id. The client plays "potion.brewed" from the name.
			McpeLevelSoundEvent levelEvent = McpeLevelSoundEvent.CreateObject();
			levelEvent.soundId = LevelSoundEventType.PotionBrewed.ToWireName();
			levelEvent.blockId = -1;
			levelEvent.position = Coordinates;
			level.RelayBroadcast(levelEvent);
		}

		/// <summary>Progress and fuel to the client's brewing screen, and the state to the chunk, only
		/// when a value changed. The furnace sends every tick, but the brewing screen stays open for
		/// the whole 20s of a brew and three packets a tick over the RTC lane is measurable on the
		/// world tick, so both the packets and the chunk write wait for a change.</summary>
		private void SendState(Level level, Inventory inventory)
		{
			if (CookTime == _lastCookTime && FuelAmount == _lastFuelAmount && FuelTotal == _lastFuelTotal) return;

			_lastCookTime = CookTime;
			_lastFuelAmount = FuelAmount;
			_lastFuelTotal = FuelTotal;

			level.SetBlockEntity(this, false);

			foreach (Player observer in inventory.Observers)
			{
				var brewTime = McpeContainerSetData.CreateObject();
				brewTime.windowId = inventory.WindowsId;
				brewTime.property = 0;
				brewTime.value = CookTime;
				observer.SendPacket(brewTime);

				var fuelAmount = McpeContainerSetData.CreateObject();
				fuelAmount.windowId = inventory.WindowsId;
				fuelAmount.property = 1;
				fuelAmount.value = FuelAmount;
				observer.SendPacket(fuelAmount);

				var fuelTotal = McpeContainerSetData.CreateObject();
				fuelTotal.windowId = inventory.WindowsId;
				fuelTotal.property = 2;
				fuelTotal.value = FuelTotal;
				observer.SendPacket(fuelTotal);
			}
		}

		private static string Fingerprint(Inventory inventory)
		{
			var sb = new StringBuilder();
			for (byte i = 0; i < 5; i++)
			{
				Item item = inventory.Slots[i];
				sb.Append(item.NetworkId).Append(':').Append(item.Metadata).Append(':').Append(item.Count).Append(';');
			}

			return sb.ToString();
		}
	}
}
