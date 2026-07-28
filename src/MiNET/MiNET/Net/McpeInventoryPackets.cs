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

using MiNET.Items;

namespace MiNET.Net
{
	// Container addressing since 712/729: a slot-type byte plus an optional dynamic
	// container id, and a storage item for dynamic containers (bundles).

	public partial class McpeInventoryContent : Packet<McpeInventoryContent>
	{
		public byte containerId;
		public bool hasDynamicContainerId;
		public uint dynamicContainerId;
		public Item storageItem;

		partial void AfterEncode()
		{
			Write(containerId);
			Write(hasDynamicContainerId);
			if (hasDynamicContainerId) Write(dynamicContainerId);
			Write(storageItem ?? new ItemAir());
		}

		partial void AfterDecode()
		{
			containerId = ReadByte();
			hasDynamicContainerId = ReadBool();
			if (hasDynamicContainerId) dynamicContainerId = ReadUint();
			storageItem = ReadItem();
		}
	}

	public partial class McpeInventorySlot : Packet<McpeInventorySlot>
	{
		public bool hasContainer;
		public byte containerId;
		public bool hasDynamicContainerId;
		public uint dynamicContainerId;
		public bool hasStorageItem;
		public Item storageItem;
		public Item item;

		partial void AfterEncode()
		{
			Write(hasContainer);
			if (hasContainer)
			{
				Write(containerId);
				Write(hasDynamicContainerId);
				if (hasDynamicContainerId) Write(dynamicContainerId);
			}
			Write(hasStorageItem);
			if (hasStorageItem) Write(storageItem ?? new ItemAir());
			Write(item ?? new ItemAir());
		}

		partial void AfterDecode()
		{
			hasContainer = ReadBool();
			if (hasContainer)
			{
				containerId = ReadByte();
				hasDynamicContainerId = ReadBool();
				if (hasDynamicContainerId) dynamicContainerId = ReadUint();
			}
			hasStorageItem = ReadBool();
			if (hasStorageItem) storageItem = ReadItem();
			item = ReadItem();
		}
	}

	public partial class McpeGameRulesChanged : Packet<McpeGameRulesChanged>
	{
		public GameRules rules;

		partial void AfterEncode()
		{
			if (rules == null)
			{
				WriteUnsignedVarInt(0);
				return;
			}

			WriteUnsignedVarInt((uint) rules.Count);
			foreach (GameRule rule in rules)
			{
				Write(rule.Name.ToLower());
				Write(rule.IsPlayerModifiable);
				switch (rule)
				{
					case GameRule<bool> boolRule:
						WriteUnsignedVarInt(1);
						Write(boolRule.Value);
						break;
					case GameRule<int> intRule:
						WriteUnsignedVarInt(2);
						Write(intRule.Value); // li32, unlike StartGame's varint
						break;
					case GameRule<float> floatRule:
						WriteUnsignedVarInt(3);
						Write(floatRule.Value);
						break;
				}
			}
		}

		partial void AfterDecode()
		{
			rules = new GameRules();
			uint count = ReadUnsignedVarInt();
			for (uint i = 0; i < count; i++)
			{
				string name = ReadString();
				bool editable = ReadBool();
				uint type = ReadUnsignedVarInt();
				switch (type)
				{
					case 1:
						rules.Add(new GameRule<bool>(name, ReadBool()) {IsPlayerModifiable = editable});
						break;
					case 2:
						rules.Add(new GameRule<int>(name, ReadInt()) {IsPlayerModifiable = editable}); // li32
						break;
					case 3:
						rules.Add(new GameRule<float>(name, ReadFloat()) {IsPlayerModifiable = editable});
						break;
				}
			}
		}
	}
}
