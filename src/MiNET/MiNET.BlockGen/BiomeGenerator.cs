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

using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiNET.BlockGen;

/// <summary>
///     Writes MiNET/Data/biome_definitions.json.gz from the CloudburstMC biome data.
///
///     Their file never ships and never reaches the runtime. This reads it and emits our own
///     structure: a flat list keyed by name, colours as components rather than a packed int, and
///     the four ambient densities the wire format has no room for but server logic wants.
///
///     chunkGenData is not carried yet. It is 3.8MB of the 9.3MB source, eighteen nested
///     structures of surface builders, climate and feature lists, and nothing in MiNET reads it.
/// </summary>
public static class BiomeGenerator
{
	public static int Run(string dataDir, string outputPath)
	{
		string sourcePath = Path.Combine(dataDir, "biome_definitions.json");
		if (!File.Exists(sourcePath))
		{
			Console.Error.WriteLine($"data file not found: {sourcePath}");
			return 0;
		}

		var source = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(File.ReadAllText(sourcePath));

		var biomes = new List<object>();
		foreach (KeyValuePair<string, JObject> entry in source.OrderBy(e => e.Key, StringComparer.Ordinal))
		{
			JObject b = entry.Value;
			JToken colour = b["mapWaterColor"];

			biomes.Add(new
			{
				name = entry.Key,
				temperature = b.Value<float>("temperature"),
				downfall = b.Value<float>("downfall"),
				rain = b.Value<bool>("rain"),
				depth = b.Value<float>("depth"),
				scale = b.Value<float>("scale"),
				foliageSnow = b.Value<float>("foliageSnow"),
				waterColor = new
				{
					a = colour.Value<byte>("a"),
					r = colour.Value<byte>("r"),
					g = colour.Value<byte>("g"),
					b = colour.Value<byte>("b")
				},
				ashDensity = b.Value<float>("ashDensity"),
				whiteAshDensity = b.Value<float>("whiteAshDensity"),
				redSporeDensity = b.Value<float>("redSporeDensity"),
				blueSporeDensity = b.Value<float>("blueSporeDensity"),
				tags = b["tags"].Select(t => t.Value<string>()).OrderBy(t => t, StringComparer.Ordinal).ToList()
			});
		}

		string json = JsonConvert.SerializeObject(new {biomes}, Formatting.Indented);

		using (var file = File.Create(outputPath))
		using (var gzip = new GZipStream(file, CompressionLevel.SmallestSize))
		using (var writer = new StreamWriter(gzip))
		{
			writer.Write(json);
		}

		Console.WriteLine($"biome_definitions.json.gz: {biomes.Count} biomes, {json.Length} bytes of JSON, {new FileInfo(outputPath).Length} compressed");
		return biomes.Count;
	}
}
