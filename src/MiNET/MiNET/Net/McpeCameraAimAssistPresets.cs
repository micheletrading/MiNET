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

namespace MiNET.Net
{
	public class AimAssistPriorityEntry
	{
		public string Id { get; set; }
		public int Priority { get; set; }
	}

	public class AimAssistCategory
	{
		public string Name { get; set; }
		public List<AimAssistPriorityEntry> EntityPriorities { get; set; } = new List<AimAssistPriorityEntry>();
		public List<AimAssistPriorityEntry> BlockPriorities { get; set; } = new List<AimAssistPriorityEntry>();
		public List<AimAssistPriorityEntry> BlockTags { get; set; } = new List<AimAssistPriorityEntry>();
		public List<AimAssistPriorityEntry> EntityTypeFamilies { get; set; } = new List<AimAssistPriorityEntry>();
		public int? EntityDefault { get; set; }
		public int? BlockDefault { get; set; }
	}

	public class AimAssistExclusionSettings
	{
		public List<string> Blocks { get; set; } = new List<string>();
		public List<string> Entities { get; set; } = new List<string>();
		public List<string> BlockTags { get; set; } = new List<string>();
		public List<string> EntityTypeFamilies { get; set; } = new List<string>();
	}

	public class AimAssistItemSetting
	{
		public string Id { get; set; }
		public string Category { get; set; }
	}

	public class AimAssistPreset
	{
		public string Id { get; set; }
		public AimAssistExclusionSettings ExclusionSettings { get; set; } = new AimAssistExclusionSettings();
		public List<string> TargetLiquids { get; set; } = new List<string>();
		public List<AimAssistItemSetting> ItemSettings { get; set; } = new List<AimAssistItemSetting>();
		public string DefaultItemSettings { get; set; }
		public string HandSettings { get; set; }
	}

	public partial class McpeCameraAimAssistPresets : Packet<McpeCameraAimAssistPresets>
	{
		public List<AimAssistCategory> Categories { get; set; } = new List<AimAssistCategory>();
		public List<AimAssistPreset> Presets { get; set; } = new List<AimAssistPreset>();
		public byte Operation { get; set; }

		partial void AfterDecode()
		{
			uint categoryCount = ReadUnsignedVarInt();
			for (int i = 0; i < categoryCount; i++)
			{
				var category = new AimAssistCategory {Name = ReadString()};

				uint entityPriorityCount = ReadUnsignedVarInt();
				for (int j = 0; j < entityPriorityCount; j++)
				{
					category.EntityPriorities.Add(new AimAssistPriorityEntry {Id = ReadString(), Priority = ReadInt()});
				}

				uint blockPriorityCount = ReadUnsignedVarInt();
				for (int j = 0; j < blockPriorityCount; j++)
				{
					category.BlockPriorities.Add(new AimAssistPriorityEntry {Id = ReadString(), Priority = ReadInt()});
				}

				uint blockTagCount = ReadUnsignedVarInt();
				for (int j = 0; j < blockTagCount; j++)
				{
					category.BlockTags.Add(new AimAssistPriorityEntry {Id = ReadString(), Priority = ReadInt()});
				}

				uint entityTypeFamilyCount = ReadUnsignedVarInt();
				for (int j = 0; j < entityTypeFamilyCount; j++)
				{
					category.EntityTypeFamilies.Add(new AimAssistPriorityEntry {Id = ReadString(), Priority = ReadInt()});
				}

				if (ReadBool()) category.EntityDefault = ReadInt();
				if (ReadBool()) category.BlockDefault = ReadInt();

				Categories.Add(category);
			}

			uint presetCount = ReadUnsignedVarInt();
			for (int i = 0; i < presetCount; i++)
			{
				var preset = new AimAssistPreset {Id = ReadString()};

				uint blockCount = ReadUnsignedVarInt();
				for (int j = 0; j < blockCount; j++) preset.ExclusionSettings.Blocks.Add(ReadString());

				uint entityCount = ReadUnsignedVarInt();
				for (int j = 0; j < entityCount; j++) preset.ExclusionSettings.Entities.Add(ReadString());

				uint exclBlockTagCount = ReadUnsignedVarInt();
				for (int j = 0; j < exclBlockTagCount; j++) preset.ExclusionSettings.BlockTags.Add(ReadString());

				uint exclEntityTypeFamilyCount = ReadUnsignedVarInt();
				for (int j = 0; j < exclEntityTypeFamilyCount; j++) preset.ExclusionSettings.EntityTypeFamilies.Add(ReadString());

				uint targetLiquidCount = ReadUnsignedVarInt();
				for (int j = 0; j < targetLiquidCount; j++) preset.TargetLiquids.Add(ReadString());

				uint itemSettingCount = ReadUnsignedVarInt();
				for (int j = 0; j < itemSettingCount; j++)
				{
					preset.ItemSettings.Add(new AimAssistItemSetting {Id = ReadString(), Category = ReadString()});
				}

				if (ReadBool()) preset.DefaultItemSettings = ReadString();
				if (ReadBool()) preset.HandSettings = ReadString();

				Presets.Add(preset);
			}

			Operation = ReadByte();
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Categories.Count);
			foreach (var category in Categories)
			{
				Write(category.Name);

				WriteUnsignedVarInt((uint) category.EntityPriorities.Count);
				foreach (var entry in category.EntityPriorities)
				{
					Write(entry.Id);
					Write(entry.Priority);
				}

				WriteUnsignedVarInt((uint) category.BlockPriorities.Count);
				foreach (var entry in category.BlockPriorities)
				{
					Write(entry.Id);
					Write(entry.Priority);
				}

				WriteUnsignedVarInt((uint) category.BlockTags.Count);
				foreach (var tag in category.BlockTags)
				{
					Write(tag.Id);
					Write(tag.Priority);
				}

				WriteUnsignedVarInt((uint) category.EntityTypeFamilies.Count);
				foreach (var entry in category.EntityTypeFamilies)
				{
					Write(entry.Id);
					Write(entry.Priority);
				}

				Write(category.EntityDefault.HasValue);
				if (category.EntityDefault.HasValue) Write(category.EntityDefault.Value);

				Write(category.BlockDefault.HasValue);
				if (category.BlockDefault.HasValue) Write(category.BlockDefault.Value);
			}

			WriteUnsignedVarInt((uint) Presets.Count);
			foreach (var preset in Presets)
			{
				Write(preset.Id);

				WriteUnsignedVarInt((uint) preset.ExclusionSettings.Blocks.Count);
				foreach (var block in preset.ExclusionSettings.Blocks) Write(block);

				WriteUnsignedVarInt((uint) preset.ExclusionSettings.Entities.Count);
				foreach (var entity in preset.ExclusionSettings.Entities) Write(entity);

				WriteUnsignedVarInt((uint) preset.ExclusionSettings.BlockTags.Count);
				foreach (var tag in preset.ExclusionSettings.BlockTags) Write(tag);

				WriteUnsignedVarInt((uint) preset.ExclusionSettings.EntityTypeFamilies.Count);
				foreach (var family in preset.ExclusionSettings.EntityTypeFamilies) Write(family);

				WriteUnsignedVarInt((uint) preset.TargetLiquids.Count);
				foreach (var liquid in preset.TargetLiquids) Write(liquid);

				WriteUnsignedVarInt((uint) preset.ItemSettings.Count);
				foreach (var itemSetting in preset.ItemSettings)
				{
					Write(itemSetting.Id);
					Write(itemSetting.Category);
				}

				Write(preset.DefaultItemSettings != null);
				if (preset.DefaultItemSettings != null) Write(preset.DefaultItemSettings);

				Write(preset.HandSettings != null);
				if (preset.HandSettings != null) Write(preset.HandSettings);
			}

			Write(Operation);
		}
	}
}
