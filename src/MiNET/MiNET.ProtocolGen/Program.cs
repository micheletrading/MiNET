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

using System.Xml;

namespace MiNET.ProtocolGen;

/// <summary>
///     Writes MiNET's protocol registry (handler interfaces, dispatcher, packet factory) and the
///     Cereal packet classes. Two generated paths feed the same compiled result: the T4 template
///     still emits the packet classes that live in MCPE Protocol.xml, while this tool emits the
///     packets Mojang has migrated to Cereal serialization, generated from the schema submodule
///     (MiNET.BlockGen/ProtocolDocs/json). The registry covers both sides, so the compiled shapes
///     are identical to what the single T4 path produced before. Over time packets leave the XML
///     for the roster, until the T4 path is empty and this tool owns the whole protocol.
///     Same rule as MiNET.BlockGen: no reference to MiNET itself, so the tool can always run even
///     when its own previous output does not compile.
/// </summary>
public static class Program
{
	public static int Main(string[] args)
	{
		string repoRoot = args.Length > 0 ? args[0] : FindRepoRoot();
		string netDir = Path.Combine(repoRoot, "src", "MiNET", "MiNET", "Net");
		string xmlPath = Path.Combine(netDir, "MCPE Protocol.xml");
		string schemaDir = Path.Combine(repoRoot, "src", "MiNET", "MiNET.BlockGen", "ProtocolDocs", "json");
		string genDir = Path.Combine(repoRoot, "src", "MiNET", "MiNET.ProtocolGen");

		if (!File.Exists(xmlPath))
		{
			Console.Error.WriteLine($"Protocol XML not found: {xmlPath}");
			return 1;
		}
		if (!Directory.Exists(schemaDir))
		{
			Console.Error.WriteLine($"Schema directory not found (submodule not initialized?): {schemaDir}");
			return 1;
		}

		var doc = new XmlDocument();
		doc.Load(xmlPath);
		var xmlPdus = XmlPdu.LoadAll(doc);

		var roster = Roster.Load(Path.Combine(genDir, "roster.json"));
		var overrides = Overrides.Load(Path.Combine(genDir, "overrides.json"));
		var schemas = new SchemaRepo(schemaDir);

		var packets = new List<CerealPacket>();
		var structs = new Dictionary<string, CerealStruct>();
		foreach (RosterEntry entry in roster.Packets)
		{
			packets.Add(CerealPacket.Resolve(entry, schemas, overrides, structs));
		}

		string registryPath = Path.Combine(netDir, "MCPE Protocol Registry.cs");
		RegistryEmitter.Emit(registryPath, xmlPdus, roster.Packets);
		Console.WriteLine($"MCPE Protocol Registry.cs: {xmlPdus.Count} XML pdus + {roster.Packets.Count} Cereal packets");

		string cerealPath = Path.Combine(netDir, "MCPE Protocol Cereal.cs");
		CerealEmitter.Emit(cerealPath, packets, structs);
		Console.WriteLine($"MCPE Protocol Cereal.cs: {packets.Count} packets, {packets.Sum(p => p.Fields.Count)} fields, {structs.Count} structs");

		return 0;
	}

	private static string FindRepoRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git"))) dir = dir.Parent;
		return dir?.FullName ?? Directory.GetCurrentDirectory();
	}
}

/// <summary>
///     Name conversion, ported verbatim from the T4 template's CodeName/CodeTypeName so both
///     generated paths produce identical identifiers for the same wire names.
/// </summary>
public static class CodeNames
{
	public static string CodeTypeName(string name)
	{
		if (name.StartsWith("ID_")) name = name.Substring(3);
		return CodeName(name, true);
	}

	public static string CodeName(string name, bool firstUpper = false)
	{
		name = name.ToLowerInvariant();

		string result = string.Empty;
		bool upperCase = firstUpper;

		for (int i = 0; i < name.Length; i++)
		{
			if (name[i] == ' ' || name[i] == '_')
			{
				upperCase = true;
			}
			else
			{
				if ((i == 0 && firstUpper) || upperCase)
				{
					result += name[i].ToString().ToUpperInvariant();
					upperCase = false;
				}
				else
				{
					result += name[i];
				}
			}
		}

		result = result.Replace(@"[]", "s");
		return result;
	}
}
