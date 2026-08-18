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
using System.Linq;
using fNbt;
using log4net;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET
{
	public class InventoryManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(InventoryManager));

		private static byte _inventoryId = 2;

		private readonly Level _level;
		private Dictionary<BlockCoordinates, Inventory> _cache = new Dictionary<BlockCoordinates, Inventory>();


		public InventoryManager(Level level)
		{
			_level = level;
		}

		public virtual Inventory GetInventory(int inventoryId)
		{
			lock (_cache)
			{
				return _cache.Values.FirstOrDefault(inventory => inventory.Id == inventoryId);
			}
		}

		public virtual Inventory GetInventory(BlockCoordinates inventoryCoord)
		{
			lock (_cache)
			{
				if (_cache.ContainsKey(inventoryCoord))
				{
					Inventory cachedInventory = _cache[inventoryCoord];
					if (cachedInventory != null) return cachedInventory;
				}

				BlockEntity blockEntity = _level.GetBlockEntity(inventoryCoord);
				if (blockEntity == null)
				{
					Log.Warn($"Found no block entity");
					Block inventoryBlock = _level.GetBlock(inventoryCoord);
					switch (inventoryBlock)
					{
						case Chest _:
							blockEntity = new ChestBlockEntity();
							break;
						case var b when b.Name.EndsWith("shulker_box"):
							blockEntity = new ShulkerBoxBlockEntity();
							break;
					}
				}

				if (blockEntity == null)
				{
					if (Log.IsDebugEnabled) Log.Debug($"No blockentity found at {inventoryCoord}");
					return null;
				}

				NbtCompound comp = blockEntity.GetCompound();
				if (Log.IsDebugEnabled) Log.Warn($"Found block entity at {inventoryCoord}\n{comp}");


				Inventory inventory;
				switch (blockEntity)
				{
					case ChestBlockEntity _:
					case ShulkerBoxBlockEntity _:
						inventory = new Inventory(GetInventoryId(), blockEntity, 27, (NbtList) comp["Items"])
						{
							Type = 0,
							WindowsId = 10,
						};
						break;
					case EnchantingTableBlockEntity _:
						inventory = new Inventory(GetInventoryId(), blockEntity, 2, (NbtList) comp["Items"])
						{
							Type = 3,
							WindowsId = 12,
						};
						break;
					case FurnaceBlockEntity furnaceBlockEntity:
					{
						inventory = new Inventory(GetInventoryId(), furnaceBlockEntity, 3, (NbtList) comp["Items"])
						{
							Type = 2,
							WindowsId = 11,
						};

						furnaceBlockEntity.Inventory = inventory;
						break;
					}
					case BlastFurnaceBlockEntity furnaceBlockEntity:
					{
						inventory = new Inventory(GetInventoryId(), furnaceBlockEntity, 3, (NbtList) comp["Items"])
						{
							Type = 27,
							WindowsId = 13,
						};

						furnaceBlockEntity.Inventory = inventory;
						break;
					}
					// Storage without the machine: the slots hold items and nothing ticks them. Every
					// kind keeps its own window id, because the id is a property of the shared
					// inventory rather than of the player looking at it, and two kinds answering to
					// one id is a close for the wrong window waiting to happen.
					case ContainerBlockEntity container:
					{
						(ContainerType type, byte windowId) = container switch
						{
							BarrelBlockEntity => (ContainerType.Container, (byte) 14),
							SmokerBlockEntity => (ContainerType.Smoker, (byte) 15),
							BrewingStandBlockEntity => (ContainerType.BrewingStand, (byte) 16),
							HopperBlockEntity => (ContainerType.Hopper, (byte) 17),
							DispenserBlockEntity => (ContainerType.Dispenser, (byte) 18),
							DropperBlockEntity => (ContainerType.Dropper, (byte) 19),
							CrafterBlockEntity => (ContainerType.Crafter, (byte) 20),
							_ => (ContainerType.Container, (byte) 21)
						};

						inventory = new Inventory(GetInventoryId(), container, (short) container.SlotCount, (NbtList) comp["Items"])
						{
							Type = (byte) type,
							WindowsId = windowId,
						};
						break;
					}
					default:
					{
						if (Log.IsDebugEnabled) Log.Warn($"Block entity did not have a matching inventory {blockEntity}");
						return null;
					}
				}

				inventory.Level = _level;

				_cache[inventoryCoord] = inventory;

				return inventory;
			}
		}

		/// <summary>Drops the inventory held for these coordinates, and closes it for anyone still
		/// looking at it. Called when the block entity goes away: the cache is keyed by position, so a
		/// chest broken and rebuilt in the same spot would otherwise open holding the old chest's items
		/// and write them into a block entity that is no longer in the chunk.</summary>
		public virtual void RemoveInventory(BlockCoordinates inventoryCoord)
		{
			Inventory inventory;
			lock (_cache)
			{
				if (!_cache.Remove(inventoryCoord, out inventory)) return;
			}

			foreach (Player observer in inventory.Observers)
			{
				observer.HandleMcpeContainerClose(null);
			}
		}

		private byte GetInventoryId()
		{
			lock (_cache)
			{
				_inventoryId++;
				if (_inventoryId == 0x78)
					_inventoryId++;
				if (_inventoryId == 0x79)
					_inventoryId++;

				return _inventoryId;
			}
		}
	}
}