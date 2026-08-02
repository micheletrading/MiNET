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
using System.Text.RegularExpressions;
using fNbt;
using Newtonsoft.Json;

namespace MiNET.BlockGen;

/// <summary>
///     Writes MiNET's block classes from the canonical palette.
///     Inputs are the palette data file and the listing of Blocks/*.cs. Nothing else. In
///     particular it does not reference MiNET: a generator that links against the code it emits
///     can only run while its own previous output compiles, which makes an empty or broken
///     generated file unrecoverable.
///     Two outputs, with one rule between them. A block with a hand-written .cs file never gets a
///     generated class. Every block gets a generated partial carrying its states. Nothing else
///     declares state members, so the halves cannot collide and no block can end up without a
///     GetState.
/// </summary>
public static class Program
{
	public static int Main(string[] args)
	{
		string repoRoot = args.Length > 0 ? args[0] : FindRepoRoot();
		string blocksDir = Path.Combine(repoRoot, "src", "MiNET", "MiNET", "Blocks");
		string itemsDir = Path.Combine(repoRoot, "src", "MiNET", "MiNET", "Items");
		string dataDir = Path.Combine(repoRoot, "src", "MiNET", "MiNET.BlockGen", "Data");
		string palettePath = Path.Combine(dataDir, "block_palette.nbt");
		string itemStatesPath = Path.Combine(dataDir, "runtime_item_states.json");
		string itemComponentsPath = Path.Combine(dataDir, "item_components.nbt");

		if (!Directory.Exists(blocksDir))
		{
			Console.Error.WriteLine($"Blocks directory not found: {blocksDir}");
			return 1;
		}

		foreach (string required in new[] {palettePath, itemStatesPath, itemComponentsPath})
		{
			if (File.Exists(required)) continue;
			Console.Error.WriteLine($"data file not found: {required}");
			Console.Error.WriteLine("The data is a git submodule. Run: git submodule update --init");
			return 1;
		}

		Console.WriteLine($"source: {dataDir}");
		Console.WriteLine($"        {DescribeSource(dataDir)}");

		List<BlockState> palette = ReadPalette(palettePath);
		Console.WriteLine($"palette: {palette.Count} states, {palette.Select(p => p.Name).Distinct().Count()} blocks");

		HashSet<string> handWritten = ReadHandWrittenClasses(blocksDir);
		HashSet<string> handImplemented = ReadHandImplementedStateClasses(blocksDir);
		Console.WriteLine($"hand-written classes: {handWritten.Count}, of which hand-implement GetState: {handImplemented.Count}");

		var byName = palette.GroupBy(p => p.Name).OrderBy(g => g.Key).ToList();

		int classes = WriteBlockDataClasses(Path.Combine(blocksDir, "BlockData.generated.cs"), byName, handWritten);
		Console.WriteLine($"BlockData.generated.cs: {classes} classes");

		int partials = WritePartialBlocks(Path.Combine(blocksDir, "PartialBlocks.cs"), byName, handImplemented);
		Console.WriteLine($"PartialBlocks.cs: {partials} partials");

		int entries = WriteBlockPalette(Path.Combine(blocksDir, "BlockPaletteData.generated.cs"), palette);
		Console.WriteLine($"BlockPaletteData.generated.cs: {entries} entries");

		List<ItemEntry> items = ReadItemRegistry(itemStatesPath, itemComponentsPath);
		Console.WriteLine($"item registry: {items.Count} items, {items.Count(i => i.ComponentBased)} component-based, {items.Count(i => i.ComponentNbt != null)} with components");

		int itemEntries = WriteItemRegistry(Path.Combine(itemsDir, "ItemRegistryData.generated.cs"), items);
		Console.WriteLine($"ItemRegistryData.generated.cs: {itemEntries} entries");

		var blockNames = new HashSet<string>(palette.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
		HashSet<string> handWrittenItems = ReadHandWrittenClasses(itemsDir, "ItemData.generated.cs", "ItemRegistryData.generated.cs");
		Console.WriteLine($"hand-written item classes: {handWrittenItems.Count}");

		int itemClasses = WriteItemDataClasses(Path.Combine(itemsDir, "ItemData.generated.cs"), items, blockNames, handWrittenItems,
			Path.Combine(dataDir, "item_mappings.json"));
		Console.WriteLine($"ItemData.generated.cs: {itemClasses} classes");

		// Not code: this one emits our own data file, because biomes are a table nobody writes
		// against by symbol. Their file stays here, ours ships.
		BiomeGenerator.Run(dataDir, Path.Combine(repoRoot, "src", "MiNET", "MiNET", "Data", "biome_definitions.json.gz"));

		return 0;
	}

	/// <summary>
	///     Writes MiNET/Items/ItemData.generated.cs: a typed <see cref="object" /> subclass for every
	///     registry identity that doesn't already have one.
	///     Three things are skipped. Block items, because a block's own generated class covers them.
	///     Names with a hand-written class in Items/. And names that are only a rename of something
	///     already written, since ItemFactory resolves the old class under the current name too.
	///     The class carries the registry string id and nothing else. The network id is not baked in:
	///     it changes every protocol version, and an identity that carries a stale number is worse
	///     than one that carries none.
	/// </summary>
	private static int WriteItemDataClasses(string path, List<ItemEntry> items, HashSet<string> blockNames, HashSet<string> handWritten, string mappingsPath)
	{
		// Renames, current name back to the old one the class was written under.
		var mappings = JsonConvert.DeserializeObject<ItemMappingsJson>(File.ReadAllText(mappingsPath));
		var renamedFrom = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> rename in mappings.Simple) renamedFrom[rename.Value] = rename.Key;

		var sb = new StringBuilder();
		WriteHeader(sb, "CloudburstMC/Data runtime_item_states.json");
		sb.AppendLine("namespace MiNET.Items");
		sb.AppendLine("{");

		var seen = new HashSet<string>();
		int count = 0;
		foreach (ItemEntry item in items.OrderBy(i => i.Name, StringComparer.Ordinal))
		{
			if (blockNames.Contains(BlockNameOf(item.Name))) continue;

			string className = "Item" + CodeName(item.Name.Replace("minecraft:", ""));
			if (handWritten.Contains(className)) continue;
			if (renamedFrom.TryGetValue(item.Name, out string oldName) && handWritten.Contains("Item" + CodeName(oldName.Replace("minecraft:", "")))) continue;
			if (!seen.Add(className)) continue;

			string baseClass = BaseClassFor(className);
			count++;
			sb.AppendLine();
			sb.AppendLine($"\tpublic partial class {className} : {baseClass} // {item.Name}");
			sb.AppendLine("\t{");
			sb.AppendLine($"\t\tpublic {className}() : base(\"{item.Name}\") {{ }}");
			sb.AppendLine("\t} // class");
		}

		sb.AppendLine("}");
		File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
		return count;
	}

	/// <summary>
	///     The block an item name refers to. Identical to the item name, except for the 17 surviving
	///     "minecraft:item.x" twins, whose block simply drops the "item." prefix.
	/// </summary>
	private static string BlockNameOf(string itemName)
	{
		return itemName.StartsWith("minecraft:item.", StringComparison.Ordinal) ? "minecraft:" + itemName.Substring("minecraft:item.".Length) : itemName;
	}

	private static string BaseClassFor(string className)
	{
		if (className.EndsWith("Axe", StringComparison.Ordinal)) return "ItemAxe";
		if (className.EndsWith("Shovel", StringComparison.Ordinal)) return "ItemShovel";
		if (className.EndsWith("Pickaxe", StringComparison.Ordinal)) return "ItemPickaxe";
		if (className.EndsWith("Hoe", StringComparison.Ordinal)) return "ItemHoe";
		if (className.EndsWith("Sword", StringComparison.Ordinal)) return "ItemSword";
		if (className.EndsWith("Helmet", StringComparison.Ordinal)) return "ArmorHelmetBase";
		if (className.EndsWith("Chestplate", StringComparison.Ordinal)) return "ArmorChestplateBase";
		if (className.EndsWith("Leggings", StringComparison.Ordinal)) return "ArmorLeggingsBase";
		if (className.EndsWith("Boots", StringComparison.Ordinal)) return "ArmorBootsBase";
		return "Item";
	}

	private sealed class ItemMappingsJson
	{
		[JsonProperty("simple")] public Dictionary<string, string> Simple { get; set; } = new Dictionary<string, string>();
	}

	// One item registry identity: the durable string id, this protocol version's network id, and
	// the component blob for the items that carry one. ComponentNbt is already serialized as
	// network NBT, which is exactly what the item_registry packet puts on the wire.
	private sealed record ItemEntry(string Name, short NetworkId, bool ComponentBased, int Version, byte[] ComponentNbt);

	/// <summary>
	///     Reads CloudburstMC/Data runtime_item_states.json and item_components.nbt into one list.
	///     Verified against a live BDS 1.26.34 item_registry capture on 2026-08-01: same 1933 names,
	///     same network ids, same component_based flags, same versions, and the 76 component trees
	///     re-serialize to byte-identical network NBT.
	///     Note that "component based" and "has components" are close to independent here. 73 items
	///     carry the flag, 76 carry components, and the sets only partly overlap (food carries
	///     components without the flag, music discs carry the flag without components). BDS reports
	///     it that way, so neither is derived from the other.
	/// </summary>
	private static List<ItemEntry> ReadItemRegistry(string statesPath, string componentsPath)
	{
		var states = JsonConvert.DeserializeObject<List<ItemStateJson>>(File.ReadAllText(statesPath));

		// Gzipped big-endian NBT, a root compound holding one compound per item name. An item with
		// no components is present with an empty compound.
		var file = new NbtFile {BigEndian = true, UseVarInt = false};
		file.LoadFromFile(componentsPath, NbtCompression.AutoDetect, null);
		var componentRoot = (NbtCompound) file.RootTag;

		var result = new List<ItemEntry>(states.Count);
		foreach (ItemStateJson state in states)
		{
			byte[] nbt = null;
			if (componentRoot[state.Name] is NbtCompound components && components.Count > 0)
			{
				// The tree is keyed by item name, so its root tag carries that name. The wire root is
				// unnamed; without this the client reads a name where it expects the payload.
				var root = (NbtCompound) components.Clone();
				root.Name = "";
				nbt = new NbtFile(root) {BigEndian = false, UseVarInt = true}.SaveToBuffer(NbtCompression.None);
			}

			result.Add(new ItemEntry(state.Name, state.Id, state.ComponentBased, state.Version, nbt));
		}

		return result;
	}

	private sealed class ItemStateJson
	{
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("id")] public short Id { get; set; }
		[JsonProperty("version")] public int Version { get; set; }
		[JsonProperty("componentBased")] public bool ComponentBased { get; set; }
	}

	/// <summary>
	///     Emits the item registry as compiled code, the same way the block palette is emitted.
	///     An item's identity is its string id; the network id is only what this protocol version
	///     numbered it, so it is generated data rather than something the server works out.
	///     Component blobs are stored base64 and handed to the wire verbatim. They are already the
	///     exact bytes BDS sends, so nothing parses NBT to write the item_registry packet.
	///     Split into parts for the 64KB IL method body cap, as with the block palette.
	/// </summary>
	private static int WriteItemRegistry(string path, List<ItemEntry> items)
	{
		const int PerPart = 400;
		int parts = (items.Count + PerPart - 1) / PerPart;

		var sb = new StringBuilder();
		WriteHeader(sb, "CloudburstMC/Data runtime_item_states.json + item_components.nbt");
		sb.AppendLine("namespace MiNET.Items");
		sb.AppendLine("{");
		sb.AppendLine("\tpublic static partial class ItemRegistryData");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\t/// <summary>Fills the registry. Entry order is the order the item_registry packet sends.</summary>");
		sb.AppendLine("\t\tpublic static void Create(ItemRegistry registry)");
		sb.AppendLine("\t\t{");
		for (int part = 1; part <= parts; part++) sb.AppendLine($"\t\t\tCreateItems_Part{part}(registry);");
		sb.AppendLine("\t\t}");

		for (int part = 1; part <= parts; part++)
		{
			sb.AppendLine();
			sb.AppendLine($"\t\tprivate static void CreateItems_Part{part}(ItemRegistry registry)");
			sb.AppendLine("\t\t{");

			int from = (part - 1) * PerPart;
			int to = Math.Min(from + PerPart, items.Count);
			for (int i = from; i < to; i++)
			{
				ItemEntry item = items[i];
				string componentBased = item.ComponentBased ? "true" : "false";
				string nbt = item.ComponentNbt == null ? "null" : $"\"{Convert.ToBase64String(item.ComponentNbt)}\"";
				sb.AppendLine($"\t\t\tregistry.Add(\"{item.Name}\", {item.NetworkId}, {componentBased}, {item.Version}, {nbt});");
			}

			sb.AppendLine("\t\t}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine("}");
		File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
		return items.Count;
	}

	// A block's identity in the palette: its name, its legacy id if it still has one, and the
	// set of states for one permutation.
	private sealed record BlockState(string Name, int Id, int Version, List<(string Name, object Value)> States);

	/// <summary>
	///     Emits the block palette as compiled code instead of a data file parsed at startup.
	///     The palette is just an ordered list of name plus states, and the order is its meaning:
	///     a block's network id is its position. All of that is known when this runs, so there is
	///     nothing for the server to work out at runtime, and nothing to keep a second copy of the
	///     source data around for.
	///     Split into parts because a C# method body is capped at 64KB of IL, the same reason
	///     RecipeData is written this way.
	/// </summary>
	private static int WriteBlockPalette(string path, List<BlockState> palette)
	{
		const int PerPart = 1000;
		int parts = (palette.Count + PerPart - 1) / PerPart;

		var sb = new StringBuilder();
		WriteHeader(sb, "CloudburstMC/Data block_palette.nbt");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using MiNET.Utils;");
		sb.AppendLine();
		sb.AppendLine("namespace MiNET.Blocks");
		sb.AppendLine("{");
		// Every entry carries the same schema stamp, so it is one constant rather than a field on
		// 16913 objects. A state written to disk without it is treated as predating every upgrade
		// schema, and Bedrock runs it through the whole rename and remap chain on load.
		int[] versions = palette.Select(p => p.Version).Distinct().ToArray();
		if (versions.Length != 1)
		{
			throw new InvalidDataException($"expected one block state version, found {versions.Length}: {string.Join(", ", versions)}");
		}

		int version = versions[0];
		sb.AppendLine("\tpublic static partial class BlockPaletteData");
		sb.AppendLine("\t{");
		sb.AppendLine("\t\t/// <summary>");
		sb.AppendLine("\t\t///     Block state schema version, as published with the palette. Stamp this on every");
		sb.AppendLine("\t\t///     block state written to disk: without it Bedrock treats the state as predating");
		sb.AppendLine("\t\t///     every upgrade schema and rewrites it on load.");
		sb.AppendLine("\t\t/// </summary>");
		sb.AppendLine($"\t\tpublic const int BlockStateVersion = {version}; // {version >> 24 & 0xff}.{version >> 16 & 0xff}.{version >> 8 & 0xff}.{version & 0xff}");
		sb.AppendLine();
		sb.AppendLine("\t\t/// <summary>Fills the palette in canonical order. Index is the network id.</summary>");
		sb.AppendLine("\t\tpublic static void Create(BlockPalette palette)");
		sb.AppendLine("\t\t{");
		for (int part = 1; part <= parts; part++) sb.AppendLine($"\t\t\tCreatePalette_Part{part}(palette);");
		sb.AppendLine("\t\t}");

		for (int part = 1; part <= parts; part++)
		{
			sb.AppendLine();
			sb.AppendLine($"\t\tprivate static void CreatePalette_Part{part}(BlockPalette palette)");
			sb.AppendLine("\t\t{");

			int from = (part - 1) * PerPart;
			int to = Math.Min(from + PerPart, palette.Count);
			for (int i = from; i < to; i++)
			{
				BlockState state = palette[i];
				var states = state.States.Select(s => s.Value switch
				{
					byte b => $"new BlockStateByte {{Name = \"{s.Name}\", Value = {b}}}",
					int n => $"new BlockStateInt {{Name = \"{s.Name}\", Value = {n}}}",
					_ => $"new BlockStateString {{Name = \"{s.Name}\", Value = \"{s.Value}\"}}"
				});

				string statePart = state.States.Count == 0 ? "" : $", States = {{{string.Join(", ", states)}}}";
				sb.AppendLine($"\t\t\tpalette.Add(new BlockStateContainer {{RuntimeId = {i}, Id = {state.Id}, Name = \"{state.Name}\"{statePart}}});");
			}

			sb.AppendLine("\t\t}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine("}");
		File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
		return palette.Count;
	}

	/// <summary>
	///     What the generated code was actually built from. The data is a pinned submodule, so the
	///     commit is the answer, and printing it means a regeneration says on the tin which palette
	///     produced it rather than leaving it to be inferred later.
	/// </summary>
	private static string DescribeSource(string dataDir)
	{
		try
		{
			// Quoted: the format contains spaces, and unquoted git reads each word as its own
			// argument and fails.
			var info = new System.Diagnostics.ProcessStartInfo("git", "log -1 --date=short \"--format=%h  %ad  %s\"")
			{
				WorkingDirectory = dataDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			};
			using var process = System.Diagnostics.Process.Start(info);
			string line = process?.StandardOutput.ReadToEnd().Trim();
			process?.WaitForExit();
			return string.IsNullOrEmpty(line) ? "unknown revision" : line;
		}
		catch (Exception e)
		{
			return $"unknown revision ({e.GetType().Name})";
		}
	}

	/// <summary>
	///     Reads CloudburstMC/Data block_palette.nbt: gzipped, big endian, a root compound holding
	///     a "blocks" list. List order is the canonical palette order, and each entry also carries
	///     block_id, network_id and name_hash.
	///     The submodule is pinned to the commit matching the protocol we target. Their master runs
	///     ahead, and a newer palette has extra states that shift every index after them, which
	///     looks exactly like an ordering bug.
	/// </summary>
	private static List<BlockState> ReadPalette(string nbtPath)
	{
		var file = new NbtFile {BigEndian = true, UseVarInt = false};
		file.LoadFromFile(nbtPath, NbtCompression.AutoDetect, null);
		var root = (NbtCompound) file.RootTag;

		if (root["blocks"] is not NbtList blocks)
		{
			throw new InvalidDataException($"no 'blocks' list in {nbtPath}; root tags: {string.Join(",", root.Names)}");
		}

		var result = new List<BlockState>(blocks.Count);
		foreach (NbtTag tag in blocks)
		{
			var entry = (NbtCompound) tag;
			var states = new List<(string, object)>();
			if (entry["states"] is NbtCompound stateTag)
			{
				foreach (NbtTag state in stateTag)
				{
					object value = state.TagType switch
					{
						NbtTagType.Byte => state.ByteValue,
						NbtTagType.Int => state.IntValue,
						NbtTagType.String => state.StringValue,
						_ => null
					};
					if (value != null) states.Add((state.Name, value));
				}
			}

			// block_id is the legacy numeric id, or absent for a block that never had one.
			// version is the block state schema stamp, the same on every entry.
			result.Add(new BlockState(entry["name"].StringValue, entry["block_id"]?.IntValue ?? 0,
				entry["version"]?.IntValue ?? 0, states));
		}

		return result;
	}

	/// <summary>
	///     Class names declared by a hand-written file in Blocks/. Read off disk rather than by
	///     reflection: the previously generated classes are indistinguishable from hand-written
	///     ones once compiled, so asking the type system makes the generator skip everything.
	/// </summary>
	private static HashSet<string> ReadHandWrittenClasses(string blocksDir, params string[] generatedFiles)
	{
		var generated = new HashSet<string>(generatedFiles.Length > 0 ? generatedFiles : new[] {"PartialBlocks.cs", "BlockData.generated.cs"});
		var names = new HashSet<string>();
		foreach (string path in Directory.GetFiles(blocksDir, "*.cs"))
		{
			if (generated.Contains(Path.GetFileName(path))) continue;
			foreach (Match m in Regex.Matches(File.ReadAllText(path), @"public\s+(?:abstract\s+)?(?:partial\s+)?class\s+(\w+)"))
			{
				names.Add(m.Groups[1].Value);
			}
		}

		return names;
	}

	/// <summary>
	///     Classes that write their own GetState by hand. A generated partial would duplicate it,
	///     so those blocks are the one case where the states are not generated. Detected from the
	///     file text, since the compiled type cannot tell a hand-written override from a
	///     previously generated one.
	/// </summary>
	private static HashSet<string> ReadHandImplementedStateClasses(string blocksDir)
	{
		var generated = new HashSet<string> {"PartialBlocks.cs", "BlockData.generated.cs", "LegacyPartialBlocks.cs"};
		var names = new HashSet<string>();
		foreach (string path in Directory.GetFiles(blocksDir, "*.cs"))
		{
			if (generated.Contains(Path.GetFileName(path))) continue;
			string text = File.ReadAllText(path);
			if (!text.Contains("BlockStateContainer GetState()")) continue;
			foreach (Match m in Regex.Matches(text, @"public\s+(?:abstract\s+)?(?:partial\s+)?class\s+(\w+)"))
			{
				names.Add(m.Groups[1].Value);
			}
		}

		return names;
	}

	private static int WriteBlockDataClasses(string path, List<IGrouping<string, BlockState>> byName, HashSet<string> handWritten)
	{
		var sb = new StringBuilder();
		WriteHeader(sb, "CloudburstMC/Data block_palette.nbt");
		sb.AppendLine("namespace MiNET.Blocks");
		sb.AppendLine("{");

		int count = 0;
		foreach (IGrouping<string, BlockState> group in byName)
		{
			string className = CodeName(group.Key.Replace("minecraft:", ""));
			if (handWritten.Contains(className)) continue;

			BlockState first = group.First();
			count++;
			sb.AppendLine();
			sb.AppendLine($"\tpublic partial class {className} : Block // {group.Key}");
			sb.AppendLine("\t{");
			sb.AppendLine($"\t\tpublic {className}() : base({first.Id})");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tIsGenerated = true;");
			sb.AppendLine("\t\t}");
			sb.AppendLine("\t} // class");
		}

		sb.AppendLine("}");
		File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
		return count;
	}

	private static int WritePartialBlocks(string path, List<IGrouping<string, BlockState>> byName, HashSet<string> handImplemented)
	{
		var sb = new StringBuilder();
		WriteHeader(sb, "CloudburstMC/Data block_palette.nbt");
		sb.AppendLine("using System;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using MiNET.Utils;");
		sb.AppendLine();
		sb.AppendLine("namespace MiNET.Blocks");
		sb.AppendLine("{");

		int count = 0;
		foreach (IGrouping<string, BlockState> group in byName)
		{
			string className = CodeName(group.Key.Replace("minecraft:", ""));
			if (handImplemented.Contains(className)) continue;
			BlockState first = group.First();
			count++;

			// Every distinct value a state takes across this block's permutations, so a bit stays
			// a bool and a range keeps its real bounds.
			var valuesByState = new Dictionary<string, List<object>>();
			foreach (BlockState permutation in group)
			foreach ((string stateName, object value) in permutation.States)
			{
				if (!valuesByState.TryGetValue(stateName, out List<object> values)) valuesByState[stateName] = values = new List<object>();
				if (!values.Contains(value)) values.Add(value);
			}

			var bits = new HashSet<string>();
			sb.AppendLine();
			sb.AppendLine($"\tpublic partial class {className} // {group.Key}");
			sb.AppendLine("\t{");
			sb.AppendLine($"\t\tpublic override string Name => \"{group.Key}\";");
			sb.AppendLine();

			foreach ((string stateName, object defaultValue) in first.States)
			{
				string prop = CodeName(stateName.Replace("minecraft:", ""));
				List<object> values = valuesByState[stateName];
				switch (defaultValue)
				{
					case byte:
					{
						List<byte> bytes = values.Cast<byte>().OrderBy(v => v).ToList();
						if (bytes.Count <= 2 && bytes.Min() == 0 && bytes.Max() <= 1)
						{
							bits.Add(stateName);
							sb.AppendLine($"\t\t[StateBit] public bool {prop} {{ get; set; }} = {((byte) defaultValue == 1 ? "true" : "false")};");
						}
						else
						{
							sb.AppendLine($"\t\t[StateRange({bytes.Min()}, {bytes.Max()})] public byte {prop} {{ get; set; }} = {(byte) defaultValue};");
						}
						break;
					}
					case int:
					{
						List<int> ints = values.Cast<int>().OrderBy(v => v).ToList();
						sb.AppendLine($"\t\t[StateRange({ints.Min()}, {ints.Max()})] public int {prop} {{ get; set; }} = {(int) defaultValue};");
						break;
					}
					case string:
					{
						string enumValues = string.Join(",", values.Cast<string>().Select(v => $"\"{v}\""));
						sb.AppendLine($"\t\t[StateEnum({enumValues})] public string {prop} {{ get; set; }} = \"{(string) defaultValue}\";");
						break;
					}
				}
			}

			sb.AppendLine();
			sb.AppendLine("\t\tpublic override void SetState(List<IBlockState> states)");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tforeach (var state in states)");
			sb.AppendLine("\t\t\t{");
			sb.AppendLine("\t\t\t\tswitch (state)");
			sb.AppendLine("\t\t\t\t{");
			foreach ((string stateName, object value) in first.States)
			{
				string prop = CodeName(stateName.Replace("minecraft:", ""));
				string type = StateTypeName(value);
				string assign = bits.Contains(stateName) ? "Convert.ToBoolean(s.Value)" : "s.Value";
				sb.AppendLine($"\t\t\t\t\tcase {type} s when s.Name == \"{stateName}\":");
				sb.AppendLine($"\t\t\t\t\t\t{prop} = {assign};");
				sb.AppendLine("\t\t\t\t\t\tbreak;");
			}
			sb.AppendLine("\t\t\t\t} // switch");
			sb.AppendLine("\t\t\t} // foreach");
			sb.AppendLine("\t\t} // method");

			sb.AppendLine();
			sb.AppendLine("\t\tpublic override BlockStateContainer GetState()");
			sb.AppendLine("\t\t{");
			sb.AppendLine("\t\t\tvar record = new BlockStateContainer();");
			sb.AppendLine($"\t\t\trecord.Name = \"{group.Key}\";");
			sb.AppendLine($"\t\t\trecord.Id = {first.Id};");
			foreach ((string stateName, object value) in first.States)
			{
				string prop = CodeName(stateName.Replace("minecraft:", ""));
				string type = StateTypeName(value);
				string expr = bits.Contains(stateName) ? $"Convert.ToByte({prop})" : prop;
				sb.AppendLine($"\t\t\trecord.States.Add(new {type} {{Name = \"{stateName}\", Value = {expr}}});");
			}
			sb.AppendLine("\t\t\treturn record;");
			sb.AppendLine("\t\t} // method");
			sb.AppendLine("\t} // class");
		}

		sb.AppendLine("}");
		File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
		return count;
	}

	private static string StateTypeName(object value) => value switch
	{
		byte => "BlockStateByte",
		int => "BlockStateInt",
		_ => "BlockStateString"
	};

	private static void WriteHeader(StringBuilder sb, string source)
	{
		sb.AppendLine($"// GENERATED by MiNET.BlockGen from {source}.");
		sb.AppendLine("// Do not hand-edit. Run the tool again after updating the pinned data submodule.");
		sb.AppendLine();
	}

	private static string CodeName(string name)
	{
		var sb = new StringBuilder();
		bool upper = true;
		foreach (char c in name)
		{
			if (c == '_' || c == '.' || c == ':')
			{
				upper = true;
				continue;
			}
			sb.Append(upper ? char.ToUpperInvariant(c) : c);
			upper = false;
		}

		return sb.ToString();
	}

	private static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git"))) dir = dir.Parent;
		return dir?.FullName ?? Directory.GetCurrentDirectory();
	}
}
