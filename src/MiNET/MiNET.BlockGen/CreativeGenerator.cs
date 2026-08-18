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

using System.Text;
using fNbt;
using Newtonsoft.Json;

namespace MiNET.BlockGen;

/// <summary>
///     Writes MiNET/Items/Data/creative_groups.json from CloudburstMC/Data creative_items.json.
///     The creative catalog was long a hand-captured file, so it carried the network ids of
///     whatever BDS version it was grabbed from and drifted every time the item registry
///     renumbered. Cloudburst ships the catalog name-addressed, so it can be regenerated in the
///     same pass as the item registry: each entry's name resolves to the current network id, its
///     block state carries the runtime hash, and nothing is captured by hand.
///
///     The output schema matches MiNET.InventoryUtils.CreativeGroupData exactly (the runtime reads
///     it at startup, the same way the biome table is a generated data file rather than symbols).
/// </summary>
public static class CreativeGenerator
{
	// CloudburstMC CreativeItemCategory ordinals: ALL, CONSTRUCTION, NATURE, EQUIPMENT, ITEMS, ...
	private static readonly Dictionary<string, int> Categories = new(StringComparer.OrdinalIgnoreCase)
	{
		["all"] = 0,
		["construction"] = 1,
		["nature"] = 2,
		["equipment"] = 3,
		["items"] = 4,
		["item_command_only"] = 5,
	};

	public static void Run(string dataDir, string outputPath, IReadOnlyDictionary<string, short> networkIdByName)
	{
		string sourcePath = Path.Combine(dataDir, "creative_items.json");
		var source = JsonConvert.DeserializeObject<CreativeItemsJson>(File.ReadAllText(sourcePath));

		short NetworkId(string name)
		{
			if (networkIdByName.TryGetValue(name, out short id)) return id;
			throw new Exception($"creative item {name} is not in the item registry");
		}

		var output = new CreativeGroupDataJson();

		foreach (CreativeGroupJson group in source.Groups)
		{
			if (!Categories.TryGetValue(group.Category, out int category))
				throw new Exception($"unknown creative category '{group.Category}' for group {group.Name}");

			var icon = group.Icon;
			output.Groups.Add(new CreativeGroupDefJson
			{
				Category = category,
				Name = group.Name,
				Icon = icon?.Id,
				IconNetworkId = icon?.Id == null ? 0 : NetworkId(icon.Id),
				IconMetadata = 0,
				IconRuntimeId = BlockRuntimeId(icon?.BlockStateB64),
				IconNbtB64 = ReEncodeNbt(icon?.NbtB64),
			});
		}

		foreach (CreativeItemJson item in source.Items)
		{
			output.Entries.Add(new CreativeEntryDefJson
			{
				GroupIndex = item.GroupId,
				NetworkId = NetworkId(item.Id),
				Metadata = item.Damage,
				RuntimeId = BlockRuntimeId(item.BlockStateB64),
				NbtB64 = ReEncodeNbt(item.NbtB64),
			});
		}

		// Unused by the runtime but part of the schema; keep it truthful rather than empty.
		output.EntryGroups = source.Items.Select(i => i.GroupId).ToList();

		string json = JsonConvert.SerializeObject(output, Formatting.Indented, new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
		});
		File.WriteAllText(outputPath, json, new UTF8Encoding(true));
		Console.WriteLine($"creative_groups.json: {output.Groups.Count} groups, {output.Entries.Count} entries");
	}

	/// <summary>The block network hash lives inside the state NBT as "network_id"; 0 for a non-block item.</summary>
	private static int BlockRuntimeId(string blockStateB64)
	{
		if (string.IsNullOrEmpty(blockStateB64)) return 0;

		byte[] bytes = Convert.FromBase64String(blockStateB64);
		var file = new NbtFile {BigEndian = false, UseVarInt = false};
		file.LoadFromBuffer(bytes, 0, bytes.Length, NbtCompression.None);
		return ((NbtCompound) file.RootTag)["network_id"]?.IntValue ?? 0;
	}

	/// <summary>
	///     Cloudburst stores item extra-data NBT as fixed little-endian; the runtime loads it as
	///     network little-endian (varint lengths). Re-encode so the base64 the server replays is the
	///     form the item descriptor writer expects.
	/// </summary>
	private static string ReEncodeNbt(string fixedLeB64)
	{
		if (string.IsNullOrEmpty(fixedLeB64)) return null;

		byte[] bytes = Convert.FromBase64String(fixedLeB64);
		var read = new NbtFile {BigEndian = false, UseVarInt = false};
		read.LoadFromBuffer(bytes, 0, bytes.Length, NbtCompression.None);

		var root = (NbtCompound) read.RootTag;
		root.Name = "";
		byte[] reEncoded = new NbtFile(root) {BigEndian = false, UseVarInt = true}.SaveToBuffer(NbtCompression.None);
		return Convert.ToBase64String(reEncoded);
	}

	// CloudburstMC creative_items.json shape.
	private sealed class CreativeItemsJson
	{
		[JsonProperty("groups")] public List<CreativeGroupJson> Groups { get; set; }
		[JsonProperty("items")] public List<CreativeItemJson> Items { get; set; }
	}

	private sealed class CreativeGroupJson
	{
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("category")] public string Category { get; set; }
		[JsonProperty("icon")] public CreativeIconJson Icon { get; set; }
	}

	private sealed class CreativeIconJson
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("block_state_b64")] public string BlockStateB64 { get; set; }
		[JsonProperty("nbt_b64")] public string NbtB64 { get; set; }
	}

	private sealed class CreativeItemJson
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("groupId")] public int GroupId { get; set; }
		[JsonProperty("damage")] public short Damage { get; set; }
		[JsonProperty("block_state_b64")] public string BlockStateB64 { get; set; }
		[JsonProperty("nbt_b64")] public string NbtB64 { get; set; }
	}

	// MiNET.InventoryUtils.CreativeGroupData shape (the runtime reader).
	private sealed class CreativeGroupDataJson
	{
		public List<CreativeGroupDefJson> Groups { get; } = new();
		public List<CreativeEntryDefJson> Entries { get; } = new();
		public List<int> EntryGroups { get; set; }
	}

	private sealed class CreativeGroupDefJson
	{
		public int Category { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		public int IconNetworkId { get; set; }
		public short IconMetadata { get; set; }
		public int IconRuntimeId { get; set; }
		public string IconNbtB64 { get; set; }
	}

	private sealed class CreativeEntryDefJson
	{
		public int GroupIndex { get; set; }
		public int NetworkId { get; set; }
		public short Metadata { get; set; }
		public int RuntimeId { get; set; }
		public string NbtB64 { get; set; }
	}
}
