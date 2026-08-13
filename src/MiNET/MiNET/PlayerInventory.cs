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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;

namespace MiNET
{
	public class PlayerInventory
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(PlayerInventory));

		public const int HotbarSize = 9;
		public const int InventorySize = HotbarSize + 36;
		public Player Player { get; }

		public List<Item> Slots { get; }

		public int InHandSlot { get; set; }
		public Item OffHand { get; set; } = new ItemAir();

		public CursorInventory UiInventory { get; set; } = new CursorInventory();

		// Armour
		public Item Boots { get; set; } = new ItemAir();
		public Item Leggings { get; set; } = new ItemAir();
		public Item Chest { get; set; } = new ItemAir();
		public Item Helmet { get; set; } = new ItemAir();

		// Server-side stack network ids, one per main-inventory slot, the PMMP model. The ids are
		// assigned incrementally, sent to the client in every content/slot push for the slot, and
		// echoed in every item-stack response that references the slot. The 1.26 client matches a
		// response against the stacks it knows by this id; Item.UniqueId (Environment.TickCount) is
		// not a net id the client ever registered, so it must never leak into a response.
		private readonly int[] _stackNetIds = new int[InventorySize];
		private int _nextStackNetId = 1;

		/// <summary>
		///     The stack network id the client knows for a main-inventory slot, assigning one on
		///     first use. Empty slots carry no id (0), matching vanilla: an empty stack has no
		///     stack net id on the wire.
		/// </summary>
		public virtual int GetStackNetId(int slot)
		{
			if (slot < 0 || slot >= _stackNetIds.Length) return 0;
			if (Slots[slot].IsAir) return 0;
			if (_stackNetIds[slot] == 0) _stackNetIds[slot] = _nextStackNetId++;
			return _stackNetIds[slot];
		}

		/// <summary>Hands the slot a fresh stack id after its content changed.</summary>
		public virtual int RefreshStackNetId(int slot)
		{
			if (slot < 0 || slot >= _stackNetIds.Length) return 0;
			_stackNetIds[slot] = Slots[slot].IsAir ? 0 : _nextStackNetId++;
			return _stackNetIds[slot];
		}


		public PlayerInventory(Player player)
		{
			Player = player;

			Slots = Enumerable.Repeat((Item) new ItemAir(), InventorySize).ToList();

			InHandSlot = 0;
		}

		public virtual Item GetItemInHand()
		{
			return Slots[InHandSlot] ?? new ItemAir();
		}

		public virtual void DamageItemInHand(ItemDamageReason reason, Entity target, Block block)
		{
			if (Player.GameMode != GameMode.Survival) return;

			var itemInHand = GetItemInHand();
			short metaBefore = itemInHand.Metadata;

			var unbreakingLevel = itemInHand.GetEnchantingLevel(EnchantingType.Unbreaking);
			if (unbreakingLevel > 0)
			{
				if (new Random().Next(1 + unbreakingLevel) != 0) return;
			}


			if (itemInHand.DamageItem(Player, reason, target, block))
			{
				Slots[InHandSlot] = new ItemAir();

				var sound = McpeLevelSoundEvent.CreateObject();
				sound.soundId = LevelSoundEventType.Break.ToString();
				sound.blockId = -1;
				sound.position = Player.KnownPosition;
				Player.Level.RelayBroadcast(sound);
			}

			Log.Debug($"DamageItemInHand reason={reason} item={itemInHand.Name} meta {metaBefore}->{itemInHand.Metadata} maxUses={itemInHand.GetMaxUses()}");
			SendSetSlot(InHandSlot);
		}

		public virtual void DamageArmor()
		{
			if (Player.GameMode != GameMode.Survival) return;

			Helmet = DamageArmorItem(Helmet);
			Chest = DamageArmorItem(Chest);
			Leggings = DamageArmorItem(Leggings);
			Boots = DamageArmorItem(Boots);
			Player.SendEquipmentForPlayer();
		}

		public virtual Item DamageArmorItem(Item item)
		{
			if (Player.GameMode != GameMode.Survival) return item;

			var unbreakingLevel = item.GetEnchantingLevel(EnchantingType.Unbreaking);
			if (unbreakingLevel > 0)
			{
				if (new Random().Next(1 + unbreakingLevel) != 0) return item;
			}

			item.Metadata++;

			if (item.Metadata >= item.GetMaxUses())
			{
				item = new ItemAir();

				var sound = McpeLevelSoundEvent.CreateObject();
				sound.soundId = LevelSoundEventType.Break.ToString();
				sound.blockId = -1;
				sound.position = Player.KnownPosition;
				Player.Level.RelayBroadcast(sound);
			}

			return item;
		}


		[Wired]
		public virtual void SetInventorySlot(int slot, Item item, bool forceReplace = false)
		{
			if (item == null || item.Count <= 0) item = new ItemAir();

			UpdateInventorySlot(slot, item, forceReplace);

			SendSetSlot(slot);
		}

		public virtual void UpdateInventorySlot(int slot, Item item, bool forceReplace = false)
		{
			var existing = Slots[slot];
			if (forceReplace || !existing.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
			{
				Slots[slot] = item;
				existing = item;
				RefreshStackNetId(slot);
			}

			existing.UniqueId = item.UniqueId;
			existing.Count = item.Count;
			existing.Metadata = item.Metadata;
			existing.ExtraData = item.ExtraData;
		}

		public ItemStacks GetSlots()
		{
			ItemStacks slotData = new ItemStacks();
			for (int i = 0; i < Slots.Count; i++)
			{
				if (Slots[i].Count == 0) Slots[i] = new ItemAir();
				slotData.Add(Slots[i]);
			}

			return slotData;
		}

		public ItemStacks GetUiSlots()
		{
			ItemStacks slotData = new ItemStacks();
			for (int i = 0; i < UiInventory.Slots.Count; i++)
			{
				if (UiInventory.Slots[i].Count == 0) UiInventory.Slots[i] = new ItemAir();
				slotData.Add(UiInventory.Slots[i]);
			}

			return slotData;
		}

		public ItemStacks GetOffHand()
		{
			return new ItemStacks
			{
				OffHand ?? new ItemAir(),
			};
		}

		public ItemStacks GetArmor()
		{
			return new ItemStacks
			{
				Helmet ?? new ItemAir(),
				Chest ?? new ItemAir(),
				Leggings ?? new ItemAir(),
				Boots ?? new ItemAir(),
			};
		}

		public virtual bool SetFirstEmptySlot(Item item, bool update)
		{
			for (int si = 0; si < Slots.Count; si++)
			{
				Item existingItem = Slots[si];

				// This needs to also take extradata into account when comparing.
				if (existingItem.Equals(item) && existingItem.Count < existingItem.MaxStackSize)
				{
					int take = Math.Min(item.Count, existingItem.MaxStackSize - existingItem.Count);
					existingItem.Count += (byte) take;
					item.Count -= (byte) take;
					if (update) SendSetSlot(si);

					if (item.Count <= 0)
					{
						return true;
					}
				}
			}

			for (int si = 0; si < Slots.Count; si++)
			{
				if (FirstEmptySlot(item, update, si)) return true;
			}

			return false;
		}

		private bool FirstEmptySlot(Item item, bool update, int si)
		{
			Item existingItem = Slots[si];

			if (existingItem is ItemAir || existingItem.IsAir)
			{
				Slots[si] = (Item) item.Clone();
				item.Count = 0;
				if (update) SendSetSlot(si);
				return true;
			}

			return false;
		}

		public bool AddItem(Item item, bool update)
		{
			for (int si = 0; si < Slots.Count; si++)
			{
				Item existingItem = Slots[si];

				if (existingItem is ItemAir || existingItem.IsAir)
				{
					Slots[si] = item;
					if (update) SendSetSlot(si);
					return true;
				}
			}

			return false;
		}


		public virtual void SetHeldItemSlot(int selectedHotbarSlot, bool sendToPlayer = true)
		{
			InHandSlot = selectedHotbarSlot;

			if (sendToPlayer)
			{
				var order = McpeMobEquipment.CreateObject();
				order.runtimeEntityId = EntityManager.EntityIdSelf;
				order.item = GetItemInHand();
				order.selectedSlot = (byte) InHandSlot;
				order.slot = (byte) (InHandSlot + HotbarSize);
				Player.SendPacket(order);
			}

			var broadcast = McpeMobEquipment.CreateObject();
			broadcast.runtimeEntityId = Player.EntityId;
			broadcast.item = GetItemInHand();
			broadcast.selectedSlot = (byte) InHandSlot;
			broadcast.slot = (byte) (InHandSlot + HotbarSize);
			Player.Level?.RelayBroadcast(Player, broadcast);
		}

		/// <summary>
		///     Empty the specified slot
		/// </summary>
		/// <param name="slot">The slot to empty.</param>
		public void ClearInventorySlot(byte slot)
		{
			SetInventorySlot(slot, new ItemAir());
		}

		public bool HasItem(Item item)
		{
			for (byte i = 0; i < Slots.Count; i++)
			{
				if (Slots[i].Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase) && Slots[i].Metadata == item.Metadata)
				{
					return true;
				}
			}

			return false;
		}

		public void RemoveItems(string name, byte count)
		{
			if (count <= 0) return;

			for (byte i = 0; i < Slots.Count; i++)
			{
				if (count <= 0) break;

				var slot = Slots[i];
				if (slot.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					if (Slots[i].Count >= count)
					{
						Slots[i].Count -= count;
						count = 0;
					}
					else
					{
						count -= Slots[i].Count;
						Slots[i].Count = 0;
					}

					if (slot.Count == 0)
					{
						Slots[i] = new ItemAir();
					}

					SendSetSlot(i);
				}
			}
		}

		public virtual void SendSetSlot(int slot)
		{
			var sendSlot = McpeInventorySlot.CreateObject();
			sendSlot.inventoryId = 0;
			sendSlot.slot = (uint) slot;
			// The stack net id must be the one the client knows for this slot (see the registry on
			// PlayerInventory); a random Item.UniqueId is an id the client never saw and rejects.
			sendSlot.item = (Item) Slots[slot].Clone();
			sendSlot.item.UniqueId = GetStackNetId(slot);
			Player.SendPacket(sendSlot);
		}

		public void Clear()
		{
			for (int i = 0; i < Slots.Count; ++i)
			{
				if (Slots[i] == null || !Slots[i].IsAir) Slots[i] = new ItemAir();
			}
			
			UiInventory.Clear();

			if (!OffHand.IsAir) OffHand = new ItemAir();

			if (!Helmet.IsAir) Helmet = new ItemAir();
			if (!Chest.IsAir) Chest = new ItemAir();
			if (!Leggings.IsAir) Leggings = new ItemAir();
			if (!Boots.IsAir) Boots = new ItemAir();

			Player.SendPlayerInventory();
		}
	}
}
