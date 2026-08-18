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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using MiNET.Blocks;
using MiNET.Blocks.Upgrade;
using MiNET.Utils;
using Newtonsoft.Json.Linq;

namespace MiNET.Items.Upgrade
{
	/// <summary>
	///     One version step of Bedrock's item history: names that changed, and metas that became names
	///     of their own. Data from pmmp/BedrockItemUpgradeSchema; see the ATTRIBUTION.md beside it.
	/// </summary>
	internal sealed class ItemUpgradeSchema
	{
		public int SchemaId { get; init; }
		// Case-insensitive because the stored names are not consistent about it: a world holds
		// minecraft:glazedTerracotta.silver where the schema keys the rule as
		// minecraft:glazedterracotta.silver, and both mean the same item.
		public Dictionary<string, string> RenamedIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, Dictionary<int, string>> RemappedMetas { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	///     Turns an item stack as some older Bedrock wrote it into one this server can hand a player and
	///     a current client can draw. Three shapes exist on disk and all of them turn up in real worlds:
	///     a numeric <c>id</c> with the variant in <c>Damage</c> (up to 1.5), a legacy <c>Name</c> with
	///     the variant still in <c>Damage</c> (1.6 through the flattening), and the current form where
	///     the variant is part of the name.
	/// </summary>
	public static class ItemDataUpgrader
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ItemDataUpgrader));

		private const string ResourcePrefix = "MiNET.Items.Data.ItemUpgradeSchema.";

		private static readonly Lazy<List<ItemUpgradeSchema>> Schemas = new(Load);
		private static readonly Lazy<Dictionary<int, string>> LegacyIdToName = new(LoadLegacyItemIds);
		private static readonly ConcurrentDictionary<(string Name, int Meta), (string Name, int Meta)> Resolved = new();

		/// <summary>
		///     A name and its metadata, walked through every schema in order. A meta remap wins over a
		///     rename and takes the metadata with it, because the meta became part of the identity.
		/// </summary>
		public static (string Name, int Meta) Upgrade(string name, int meta)
		{
			if (name == null) return (null, meta);

			if (Resolved.TryGetValue((name, meta), out (string Name, int Meta) cached)) return cached;

			(string Name, int Meta) result = Resolve(name, meta);
			Resolved.TryAdd((name, meta), result);

			return result;
		}

		/// <summary>A numeric item id, which is what everything up to Bedrock 1.5 stored.</summary>
		public static (string Name, int Meta) Upgrade(int legacyId, int meta)
		{
			// Ids at or below 255 are block ids: the two numbering spaces overlapped and the block one
			// won for low values, which is how the old ItemFactory read them too.
			if (legacyId > 0 && legacyId <= 255 && BlockDataUpgrader.TryUpgradeIdMeta(legacyId, meta, out string blockName, out _)) return Upgrade(blockName, 0);

			return LegacyIdToName.Value.TryGetValue(legacyId, out string name) ? Upgrade(name, meta) : (null, meta);
		}

		private static (string Name, int Meta) Resolve(string name, int meta)
		{
			string upgraded = name;
			int upgradedMeta = meta;

			foreach (ItemUpgradeSchema schema in Schemas.Value)
			{
				if (schema.RemappedMetas.TryGetValue(upgraded, out Dictionary<int, string> byMeta) && byMeta.TryGetValue(upgradedMeta, out string remapped))
				{
					upgraded = remapped;
					upgradedMeta = 0;
				}
				else if (schema.RenamedIds.TryGetValue(upgraded, out string renamed))
				{
					upgraded = renamed;
				}
			}

			// A block item still carrying its variant in the metadata: the block tables know what that
			// pair meant, and after the flattening the answer is a name of its own. Only taken when the
			// result is a name the item registry knows, so a plain item with durability keeps its meta.
			if (upgradedMeta != 0 && BlockDataUpgrader.TryUpgradeIdMeta(name, upgradedMeta, out string flattened) && flattened != upgraded && IsKnownItem(flattened))
			{
				return (flattened, 0);
			}

			return (upgraded, upgradedMeta);
		}

		internal static bool IsKnownItem(string name)
		{
			if (ItemFactory.ItemRegistry.Contains(name)) return true;

			Block block = BlockFactory.GetBlockByName(name);
			return block != null && block.GetType() != typeof(Block);
		}

		private static List<ItemUpgradeSchema> Load()
		{
			var schemas = new List<ItemUpgradeSchema>();
			Assembly assembly = typeof(Item).Assembly;

			foreach (string resource in assembly.GetManifestResourceNames()
				.Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
				.OrderBy(name => name, StringComparer.Ordinal))
			{
				string fileName = resource.Substring(ResourcePrefix.Length);
				if (!int.TryParse(fileName.Split('_')[0], out int schemaId)) continue;

				using Stream stream = assembly.GetManifestResourceStream(resource);
				using var reader = new StreamReader(stream);
				JObject json = JObject.Parse(reader.ReadToEnd());

				var renamed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				if (json["renamedIds"] is JObject renames)
				{
					foreach (JProperty property in renames.Properties()) renamed[property.Name] = property.Value.Value<string>();
				}

				var remapped = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
				if (json["remappedMetas"] is JObject metas)
				{
					foreach (JProperty item in metas.Properties())
					{
						var byMeta = new Dictionary<int, string>();
						foreach (JProperty value in ((JObject) item.Value).Properties())
						{
							if (int.TryParse(value.Name, out int meta)) byMeta[meta] = value.Value.Value<string>();
						}

						remapped[item.Name] = byMeta;
					}
				}

				schemas.Add(new ItemUpgradeSchema {SchemaId = schemaId, RenamedIds = renamed, RemappedMetas = remapped});
			}

			Log.Debug($"Loaded {schemas.Count} item upgrade schemas");
			return schemas;
		}

		private static Dictionary<int, string> LoadLegacyItemIds()
		{
			var byId = new Dictionary<int, string>();

			var byName = ResourceUtil.ReadResource<Dictionary<string, short>>("item_id_map.json", typeof(Item), "Data");
			foreach (KeyValuePair<string, short> entry in byName) byId.TryAdd(entry.Value, entry.Key);

			return byId;
		}
	}
}
