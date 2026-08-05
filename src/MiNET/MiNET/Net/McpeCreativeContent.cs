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
using MiNET.Items;

namespace MiNET.Net
{
	public class CreativeItemGroup
	{
		public int Category { get; set; }
		public string Name { get; set; }
		public Item Icon { get; set; }
	}

	public class CreativeContentEntry
	{
		public int EntryId { get; set; }
		public Item Item { get; set; }
		public int GroupIndex { get; set; }
	}

	public partial class McpeCreativeContent
	{
		public List<CreativeItemGroup> Groups { get; set; } = new List<CreativeItemGroup>();
		public List<CreativeContentEntry> Entries { get; set; } = new List<CreativeContentEntry>();

		partial void AfterDecode()
		{
			uint groupCount = ReadUnsignedVarInt();
			for (int i = 0; i < groupCount; i++)
			{
				Groups.Add(new CreativeItemGroup
				{
					// One byte since 2168; was a raw le32.
					Category = ReadByte(),
					Name = ReadString(),
					Icon = ReadItemLegacy(),
				});
			}

			uint entryCount = ReadUnsignedVarInt();
			for (int i = 0; i < entryCount; i++)
			{
				Entries.Add(new CreativeContentEntry
				{
					EntryId = ReadVarInt(),
					Item = ReadItemLegacy(),
					GroupIndex = ReadVarInt(),
				});
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Groups.Count);
			foreach (var group in Groups)
			{
				// One byte since 2168; was a raw le32.
				Write((byte) group.Category);
				Write(group.Name);
				WriteItemLegacy(group.Icon);
			}

			WriteUnsignedVarInt((uint) Entries.Count);
			foreach (var entry in Entries)
			{
				WriteVarInt(entry.EntryId);
				WriteItemLegacy(entry.Item);
				WriteVarInt(entry.GroupIndex);
			}
		}
	}
}
