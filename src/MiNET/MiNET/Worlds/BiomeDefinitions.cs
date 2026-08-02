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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using MiNET.Net;
using Newtonsoft.Json;

namespace MiNET.Worlds
{
	/// <summary>
	///     A biome as the server knows it, not as the wire carries it. Server and plugin logic reads
	///     this: temperature and downfall drive weather and mob behaviour, the densities drive
	///     ambient particles, tags are what gameplay actually queries a biome by.
	///     The BiomeDefinitionList packet is one projection of this, and a lossy one. It has no room
	///     for the four particle densities, and it replaces names with offsets into a string table
	///     built per packet. Nothing here stores an index or a packed colour: those are transport
	///     details, produced at send time.
	/// </summary>
	public class BiomeDefinition
	{
		[JsonProperty("name")] public string Name { get; set; }

		[JsonProperty("temperature")] public float Temperature { get; set; }
		[JsonProperty("downfall")] public float Downfall { get; set; }
		[JsonProperty("rain")] public bool Rain { get; set; }

		/// <summary>Terrain shape. Depth is height relative to sea level, scale is vertical variation.</summary>
		[JsonProperty("depth")] public float Depth { get; set; }
		[JsonProperty("scale")] public float Scale { get; set; }

		/// <summary>0 to 1, how frozen the leaves look.</summary>
		[JsonProperty("foliageSnow")] public float FoliageSnow { get; set; }

		[JsonProperty("waterColor")] public BiomeColor WaterColor { get; set; } = new BiomeColor();

		/// <summary>Ambient particle densities. Nether biomes use these; everything else leaves them at zero.</summary>
		[JsonProperty("ashDensity")] public float AshDensity { get; set; }
		[JsonProperty("whiteAshDensity")] public float WhiteAshDensity { get; set; }
		[JsonProperty("redSporeDensity")] public float RedSporeDensity { get; set; }
		[JsonProperty("blueSporeDensity")] public float BlueSporeDensity { get; set; }

		/// <summary>What gameplay queries a biome by: "ocean", "monster", "overworld", "deep".</summary>
		[JsonProperty("tags")] public List<string> Tags { get; set; } = new List<string>();

		public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.Ordinal);
	}

	/// <summary>
	///     Kept as components rather than a packed int, because a packed ARGB in a data file is
	///     unreadable and the packing is a wire concern.
	/// </summary>
	public class BiomeColor
	{
		[JsonProperty("a")] public byte A { get; set; }
		[JsonProperty("r")] public byte R { get; set; }
		[JsonProperty("g")] public byte G { get; set; }
		[JsonProperty("b")] public byte B { get; set; }

		public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;
	}

	public class BiomeDefinitionFile
	{
		[JsonProperty("biomes")] public List<BiomeDefinition> Biomes { get; set; } = new List<BiomeDefinition>();
	}

	/// <summary>
	///     The biome registry, loaded from Data/biome_definitions.json.gz, which MiNET.BlockGen
	///     writes from the CloudburstMC data.
	///
	///     Not the same table as <see cref="BiomeUtils.Biomes" />, which is 71 hand-written entries
	///     carrying the numeric biome ids and the grass and foliage colours that appear in no data
	///     file. Two registries for one concept, neither derived from the other, differing by 17
	///     entries. Merging them is not attempted here.
	/// </summary>
	public static class BiomeDefinitions
	{
		private const string ResourceName = "biome_definitions.json.gz";

		private static readonly Lazy<List<BiomeDefinition>> Definitions = new Lazy<List<BiomeDefinition>>(Load);
		private static readonly Lazy<Dictionary<string, BiomeDefinition>> ByName = new Lazy<Dictionary<string, BiomeDefinition>>(
			() => Definitions.Value.ToDictionary(b => b.Name, StringComparer.OrdinalIgnoreCase));

		public static IReadOnlyList<BiomeDefinition> All => Definitions.Value;

		public static BiomeDefinition GetByName(string name)
		{
			return ByName.Value.TryGetValue(name, out BiomeDefinition biome) ? biome : null;
		}

		public static IEnumerable<BiomeDefinition> WithTag(string tag) => All.Where(b => b.HasTag(tag));

		private static List<BiomeDefinition> Load()
		{
			Assembly assembly = typeof(BiomeDefinitions).Assembly;
			string name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(ResourceName, StringComparison.Ordinal));
			if (name == null) throw new FileNotFoundException($"Embedded resource {ResourceName} not found");

			using Stream compressed = assembly.GetManifestResourceStream(name);
			using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
			using var text = new StreamReader(gzip);
			using var reader = new JsonTextReader(text);

			BiomeDefinitionFile file = new JsonSerializer().Deserialize<BiomeDefinitionFile>(reader)
				?? throw new FormatException($"{ResourceName} deserialized to nothing");

			return file.Biomes;
		}

		/// <summary>
		///     Projects the registry onto the packet, interning names and tags into the string table
		///     the wire format wants. First-seen order; the client only follows the indices we give it.
		/// </summary>
		public static McpeBiomeDefinitionList CreatePacket()
		{
			var strings = new List<string>();
			var indexOf = new Dictionary<string, short>(StringComparer.Ordinal);

			short Intern(string value)
			{
				if (indexOf.TryGetValue(value, out short existing)) return existing;

				var index = (short) strings.Count;
				strings.Add(value);
				indexOf[value] = index;
				return index;
			}

			McpeBiomeDefinitionList packet = McpeBiomeDefinitionList.CreateObject();

			foreach (BiomeDefinition biome in All)
			{
				packet.Definitions.Add(new BiomeDefinitionEntry
				{
					NameIndex = Intern(biome.Name),
					// The vendor data carries no numeric id and the wire field is uint16, so unset is
					// the only honest value. The real ids live in BiomeUtils.
					BiomeId = ushort.MaxValue,
					Temperature = biome.Temperature,
					Downfall = biome.Downfall,
					SnowFoliage = biome.FoliageSnow,
					Depth = biome.Depth,
					Scale = biome.Scale,
					MapWaterColour = biome.WaterColor.ToArgb(),
					Rain = biome.Rain,
					Tags = biome.Tags.Select(tag => (ushort) Intern(tag)).ToList()
				});
			}

			packet.Strings = strings;
			return packet;
		}
	}
}
