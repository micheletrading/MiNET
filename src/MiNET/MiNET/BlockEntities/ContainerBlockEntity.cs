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

using System.Collections.Generic;
using fNbt;
using MiNET.Items;

namespace MiNET.BlockEntities
{
	/// <summary>A block entity that is nothing but a fixed number of item slots: the storage half of a
	/// container, with none of the machine behind it. A smoker holds three items here without smelting
	/// anything, a brewing stand holds five without brewing. The rules go in a subclass, or in a plugin,
	/// without the slots having to be written again.</summary>
	public class ContainerBlockEntity : BlockEntity
	{
		protected NbtCompound Compound { get; set; }

		/// <summary>How many slots the screen addresses. The item list is seeded to this length so a
		/// freshly placed container answers a write to its last slot.</summary>
		public int SlotCount { get; }

		/// <summary>The fields this kind carries besides its items, at their resting values. Nothing
		/// here is driven yet: a smoker's cook timer stays at zero because nothing cooks. They are
		/// written anyway so the tag has the shape vanilla gives it, which is what the client and any
		/// other server reading the world expect to find.</summary>
		private readonly NbtTag[] _stateFields;

		public ContainerBlockEntity(string id, int slotCount, params NbtTag[] stateFields) : base(id)
		{
			SlotCount = slotCount;
			_stateFields = stateFields ?? new NbtTag[0];

			Compound = new NbtCompound(string.Empty)
			{
				new NbtString("id", Id),
				// On every block entity vanilla writes, without exception.
				new NbtInt("BlockEntityVersion", 0),
				NewItems(),
				new NbtInt("x", Coordinates.X),
				new NbtInt("y", Coordinates.Y),
				new NbtInt("z", Coordinates.Z)
			};

			// Cloned, because a tag belongs to one compound: the array is the template, not the
			// instance, and the same entity kind is built more than once.
			foreach (NbtTag field in _stateFields) Compound.Add((NbtTag) field.Clone());
		}

		private NbtList NewItems()
		{
			var items = new NbtList("Items");
			for (byte i = 0; i < SlotCount; i++)
			{
				items.Add(new NbtCompound
				{
					new NbtByte("Slot", i),
					new NbtString("Name", string.Empty),
					new NbtShort("Damage", 0),
					new NbtByte("Count", 0)
				});
			}

			return items;
		}

		public override NbtCompound GetCompound()
		{
			Compound["x"] = new NbtInt("x", Coordinates.X);
			Compound["y"] = new NbtInt("y", Coordinates.Y);
			Compound["z"] = new NbtInt("z", Coordinates.Z);

			return Compound;
		}

		public override void SetCompound(NbtCompound compound)
		{
			Compound = compound;

			if (Compound["Items"] == null) Compound["Items"] = NewItems();
			if (Compound["BlockEntityVersion"] == null) Compound["BlockEntityVersion"] = new NbtInt("BlockEntityVersion", 0);

			// A world written before these fields existed, ours or anyone's, is filled in on the way
			// through rather than saved back missing them.
			foreach (NbtTag field in _stateFields)
			{
				if (Compound[field.Name] == null) Compound[field.Name] = (NbtTag) field.Clone();
			}
		}

		public override List<Item> GetDrops()
		{
			var slots = new List<Item>();

			if (Compound["Items"] is not NbtList items) return slots;

			for (byte i = 0; i < items.Count; i++)
			{
				slots.Add(ItemNbt.Read((NbtCompound) items[i]));
			}

			return slots;
		}
	}

	public class BarrelBlockEntity : ContainerBlockEntity
	{
		public BarrelBlockEntity() : base("Barrel", 27, new NbtByte("Findable", 0))
		{
		}
	}

	/// <summary>Ingredient, fuel, result, in the furnace's own order. No smelting: the slots hold what
	/// is put in them, and the four timers below stay where they started.</summary>
	public class SmokerBlockEntity : ContainerBlockEntity
	{
		public SmokerBlockEntity() : base("Smoker", 3,
			new NbtShort("BurnDuration", 0),
			new NbtShort("BurnTime", 0),
			new NbtShort("CookTime", 0),
			new NbtInt("StoredXPInt", 0))
		{
		}
	}

	public class HopperBlockEntity : ContainerBlockEntity
	{
		public HopperBlockEntity() : base("Hopper", 5, new NbtInt("TransferCooldown", 0))
		{
		}
	}

	public class DispenserBlockEntity : ContainerBlockEntity
	{
		public DispenserBlockEntity() : base("Dispenser", 9)
		{
		}
	}

	public class DropperBlockEntity : ContainerBlockEntity
	{
		public DropperBlockEntity() : base("Dropper", 9)
		{
		}
	}

	/// <summary>The disabled slots are a bitmask the crafter screen sets, one bit per grid slot. It is
	/// written so the tag matches vanilla, and nothing reads it back yet.</summary>
	public class CrafterBlockEntity : ContainerBlockEntity
	{
		public CrafterBlockEntity() : base("Crafter", 9, new NbtShort("disabled_slots", 0))
		{
		}
	}
}
