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
		string palettePath = Path.Combine(repoRoot, "src", "MiNET", "MiNET.BlockGen", "Data", "block_palette.nbt");

		if (!Directory.Exists(blocksDir))
		{
			Console.Error.WriteLine($"Blocks directory not found: {blocksDir}");
			return 1;
		}

		if (!File.Exists(palettePath))
		{
			Console.Error.WriteLine($"palette not found: {palettePath}");
			Console.Error.WriteLine("The data is a git submodule. Run: git submodule update --init");
			return 1;
		}

		Console.WriteLine($"source: {palettePath}");
		Console.WriteLine($"        {DescribeSource(Path.GetDirectoryName(palettePath))}");

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

		return 0;
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
		WriteHeader(sb);
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
	private static HashSet<string> ReadHandWrittenClasses(string blocksDir)
	{
		var generated = new HashSet<string> {"PartialBlocks.cs", "BlockData.generated.cs"};
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
		WriteHeader(sb);
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
		WriteHeader(sb);
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

	private static void WriteHeader(StringBuilder sb)
	{
		sb.AppendLine("// GENERATED by MiNET.BlockGen from Blocks/Data/canonical_block_states.nbt.");
		sb.AppendLine("// Do not hand-edit. Run the tool again after updating the palette data.");
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
