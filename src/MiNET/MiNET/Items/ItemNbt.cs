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

namespace MiNET.Items
{
	/// <summary>
	///     Items as the save format stores them, inside a block entity or an entity's inventory.
	///     Modern Bedrock writes <c>Name</c> (the registry string id), <c>Damage</c> and <c>Count</c>.
	///     Old saves wrote <c>id</c> as a short instead, from the pre-flattening numbering. Reading
	///     both is the whole point of this class: the legacy form is upgraded on the way in and never
	///     written back out, so a world converts itself the first time it is saved.
	///     One place, used by every block entity, so the six of them cannot disagree about the schema
	///     again (four of them used to read and write the legacy form while two used the modern one).
	/// </summary>
	public static class ItemNbt
	{
		public static Item Read(NbtCompound compound)
		{
			if (compound == null) return new ItemAir();

			short damage = compound["Damage"]?.ShortValue ?? 0;
			int count = compound["Count"]?.ByteValue ?? 1;

			Item item;
			if (compound["Name"] is NbtString name)
			{
				item = ItemFactory.GetItemByName(name.StringValue, damage, count);
			}
			else if (compound["id"] is NbtShort legacyId)
			{
				item = LegacyItemUpgrader.Upgrade(legacyId.Value, damage, count);
			}
			else
			{
				return new ItemAir();
			}

			if (compound["tag"] is NbtCompound tag) item.ExtraData = tag;

			return item;
		}

		/// <summary>Always the modern form. Nothing writes the legacy <c>id</c> short any more.</summary>
		public static NbtCompound Write(Item item, string tagName = null)
		{
			var compound = new NbtCompound(tagName ?? string.Empty)
			{
				new NbtString("Name", item?.Name ?? "minecraft:air"),
				new NbtShort("Damage", item?.Metadata ?? 0),
				new NbtByte("Count", item?.Count ?? 0)
			};

			if (item?.ExtraData != null)
			{
				var tag = (NbtCompound) item.ExtraData.Clone();
				tag.Name = "tag";
				compound.Add(tag);
			}

			return compound;
		}
	}
}
