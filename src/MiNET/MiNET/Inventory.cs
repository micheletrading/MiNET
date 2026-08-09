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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using fNbt;
using log4net;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET
{
	public interface IInventory
	{
	}

	public class Inventory : IInventory
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Inventory));

		public event Action<Player, Inventory, byte, Item> InventoryChange;

		public int Id { get; set; }
		public byte Type { get; set; }
		public ItemStacks Slots { get; set; }
		public short Size { get; set; }
		public BlockCoordinates Coordinates { get; set; }
		public BlockEntity BlockEntity { get; set; }
		public byte WindowsId { get; set; }

		/// <summary>The level the block entity belongs to, so a slot change reaches the chunk. Set by
		/// <see cref="InventoryManager" />; an inventory built without one keeps its items in memory
		/// only.</summary>
		public Level Level { get; set; }

		public Inventory(int id, BlockEntity blockEntity, short inventorySize, NbtList slots)
		{
			Id = id;
			BlockEntity = blockEntity;
			Size = inventorySize;
			Coordinates = BlockEntity.Coordinates;

			Slots = new ItemStacks();
			for (byte i = 0; i < Size; i++)
			{
				Slots.Add(new ItemAir());
			}

			for (byte i = 0; i < (slots?.Count ?? 0); i++)
			{
				var nbtItem = (NbtCompound) slots[i];

				byte slotIdx = nbtItem["Slot"]?.ByteValue ?? i;

				// An empty slot is written without a name: the enchanting table and the furnace both
				// seed their item list with placeholder entries carrying a numeric id of 0 and no
				// Name, so reading the name unconditionally threw on every enchanting table opened.
				byte count = nbtItem["Count"]?.ByteValue ?? 0;
				string name = nbtItem["Name"]?.StringValue;
				if (count == 0 || string.IsNullOrEmpty(name)) continue;

				Item item = ItemFactory.GetItemByName(name, nbtItem["Damage"]?.ShortValue ?? 0, count);
				Log.Debug($"Chest item {slotIdx}: {item}");
				Slots[slotIdx] = item;
			}
		}

		// A block inventory is shared: every player looking at the chest, and the level tick driving
		// the furnace, reach the same slots. Every read-modify-write goes under this.
		private readonly object _slotSync = new object();

		public void SetSlot(Player player, byte slot, Item itemStack)
		{
			lock (_slotSync)
			{
				Slots[slot] = itemStack;

				NbtCompound compound = BlockEntity.GetCompound();
				compound["Items"] = GetSlots();

				// The chunk keeps a CLONE of the compound, so writing the items into the block entity
				// alone changes nothing that is ever saved: the chest emptied itself on restart. Hand
				// it back to the level, which re-clones it and marks the chunk dirty. No broadcast, the
				// observers below get the one slot rather than the whole tag.
				Level?.SetBlockEntity(BlockEntity, false);

				OnInventoryChange(player, slot, itemStack);
			}
		}

		public Item GetSlot(byte slot)
		{
			lock (_slotSync)
			{
				return Slots[slot];
			}
		}

		public void DecreaseSlot(byte slot)
		{
			lock (_slotSync)
			{
				var slotData = Slots[slot];
				if (slotData is ItemAir) return;

				slotData.Count--;

				if (slotData.Count <= 0)
				{
					slotData = new ItemAir();
				}

				SetSlot(null, slot, slotData);
			}
		}

		public void IncreaseSlot(byte slot, string itemName, short metadata)
		{
			lock (_slotSync)
			{
				Item slotData = Slots[slot];
				if (slotData is ItemAir)
				{
					slotData = ItemFactory.GetItemByName(itemName, metadata, 1);
				}
				else
				{
					slotData.Count++;
				}

				SetSlot(null, slot, slotData);
			}
		}

		public bool IsOpen()
		{
			return InventoryChange != null;
		}


		private NbtList GetSlots()
		{
			NbtList slots = new NbtList("Items");
			for (byte i = 0; i < Size; i++)
			{
				var slot = Slots[i];
				slots.Add(new NbtCompound
				{
					new NbtByte("Count", slot.Count),
					new NbtByte("Slot", i),
					new NbtString("Name", slot.Name),
					new NbtShort("Damage", slot.Metadata),
				});
			}

			return slots;
		}

		protected virtual void OnInventoryChange(Player player, byte slot, Item itemStack)
		{
			InventoryChange?.Invoke(player, this, slot, itemStack);
		}


		// The players with this inventory open, so a slot change goes only to them.

		private readonly ConcurrentDictionary<Player, byte> _observers = new ConcurrentDictionary<Player, byte>();

		public ICollection<Player> Observers => _observers.Keys;

		public void AddObserver(Player player)
		{
			_observers.TryAdd(player, 0);
		}

		public void RemoveObserver(Player player)
		{
			_observers.TryRemove(player, out _);
		}
	}
}