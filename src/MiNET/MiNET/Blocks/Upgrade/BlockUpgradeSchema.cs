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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using MiNET.Utils;
using Newtonsoft.Json.Linq;

namespace MiNET.Blocks.Upgrade
{
	/// <summary>
	///     One version step of Bedrock's blockstate history, read from the JSON that PocketMine-MP
	///     generates out of BDS itself. See ATTRIBUTION.md next to the data.
	///     <para>
	///     Every rule but <see cref="RemappedStates" /> is indexed by the block name and property
	///     names as they were BEFORE this step, which is what lets them be applied in any order.
	///     </para>
	/// </summary>
	internal sealed class BlockUpgradeSchema
	{
		public int SchemaId { get; init; }
		public uint VersionId { get; init; }

		public Dictionary<string, string> RenamedIds { get; init; } = new();
		public Dictionary<string, Dictionary<string, string>> RenamedProperties { get; init; } = new();
		public Dictionary<string, Dictionary<string, IBlockState>> AddedProperties { get; init; } = new();
		public Dictionary<string, List<string>> RemovedProperties { get; init; } = new();
		public Dictionary<string, Dictionary<string, List<ValueRemap>>> RemappedPropertyValues { get; init; } = new();
		public Dictionary<string, FlattenRule> FlattenedProperties { get; init; } = new();
		public Dictionary<string, List<StateRemap>> RemappedStates { get; init; } = new();

		public sealed record ValueRemap(IBlockState Old, IBlockState New);

		/// <summary>
		///     A property that became part of the block name, which is how the flattening was done:
		///     <c>minecraft:wool</c> with <c>color=red</c> is <c>minecraft:red_wool</c>.
		/// </summary>
		public sealed record FlattenRule(string Prefix, string FlattenedProperty, string Suffix, string FlattenedPropertyType, Dictionary<string, string> ValueRemaps)
		{
			public string NameFor(IBlockState state)
			{
				string value = state switch
				{
					BlockStateString text => text.Value,
					BlockStateInt number => number.Value.ToString(),
					BlockStateByte number => number.Value.ToString(),
					_ => null
				};

				if (value == null) return null;
				if (ValueRemaps != null && ValueRemaps.TryGetValue(value, out string remapped)) value = remapped;

				return Prefix + value + Suffix;
			}
		}

		/// <summary>
		///     A whole state replaced by another, for changes a rename cannot express. <see cref="Old" />
		///     is a filter rather than an exact match, so the rules are kept in the order the file lists
		///     them, most specific first, and the first match wins.
		/// </summary>
		public sealed record StateRemap(List<IBlockState> Old, string NewName, FlattenRule NewFlattenedName, List<IBlockState> New, List<string> CopiedState);
	}

	/// <summary>
	///     Loads every schema shipped in Blocks/Data/BlockStateUpgradeSchema, ordered by the id in its
	///     file name, which is the order they must be applied in.
	/// </summary>
	internal static class BlockUpgradeSchemaLoader
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BlockUpgradeSchemaLoader));

		private const string ResourcePrefix = "MiNET.Blocks.Data.BlockStateUpgradeSchema.";

		public static List<BlockUpgradeSchema> Load()
		{
			var schemas = new List<BlockUpgradeSchema>();
			Assembly assembly = typeof(Block).Assembly;

			foreach (string resource in assembly.GetManifestResourceNames()
				.Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
				.OrderBy(name => name, StringComparer.Ordinal))
			{
				string fileName = resource.Substring(ResourcePrefix.Length);
				if (!int.TryParse(fileName.Split('_')[0], out int schemaId)) continue;

				using Stream stream = assembly.GetManifestResourceStream(resource);
				using var reader = new StreamReader(stream);
				schemas.Add(Parse(JObject.Parse(reader.ReadToEnd()), schemaId));
			}

			Log.Debug($"Loaded {schemas.Count} block state upgrade schemas, {schemas.FirstOrDefault()?.SchemaId} to {schemas.LastOrDefault()?.SchemaId}");
			return schemas;
		}

		private static BlockUpgradeSchema Parse(JObject json, int schemaId)
		{
			uint versionId = (uint) (((json.Value<int?>("maxVersionMajor") ?? 0) << 24)
				| ((json.Value<int?>("maxVersionMinor") ?? 0) << 16)
				| ((json.Value<int?>("maxVersionPatch") ?? 0) << 8)
				| (json.Value<int?>("maxVersionRevision") ?? 0));

			return new BlockUpgradeSchema
			{
				SchemaId = schemaId,
				VersionId = versionId,
				RenamedIds = ReadStringMap(json["renamedIds"]),
				RenamedProperties = ReadPerBlock(json["renamedProperties"], ReadStringMap),
				RemovedProperties = ReadPerBlock(json["removedProperties"], token => token.Select(name => name.Value<string>()).ToList()),
				AddedProperties = ReadPerBlock(json["addedProperties"], token => ReadStates((JObject) token).ToDictionary(state => state.Name)),
				FlattenedProperties = ReadPerBlock(json["flattenedProperties"], token => ReadFlattenRule((JObject) token)),
				RemappedPropertyValues = ReadValueRemaps(json),
				RemappedStates = ReadPerBlock(json["remappedStates"], token => token.Select(remap => ReadStateRemap((JObject) remap)).ToList())
			};
		}

		private static Dictionary<string, string> ReadStringMap(JToken token)
		{
			var map = new Dictionary<string, string>(StringComparer.Ordinal);
			if (token is not JObject json) return map;

			foreach (JProperty property in json.Properties()) map[property.Name] = property.Value.Value<string>();

			return map;
		}

		private static Dictionary<string, T> ReadPerBlock<T>(JToken token, Func<JToken, T> read)
		{
			var map = new Dictionary<string, T>(StringComparer.Ordinal);
			if (token is not JObject json) return map;

			foreach (JProperty property in json.Properties()) map[property.Name] = read(property.Value);

			return map;
		}

		/// <summary>
		///     The value remaps sit behind one level of indirection: a block's property names a shared
		///     entry in remappedPropertyValuesIndex, so identical remaps are written once.
		/// </summary>
		private static Dictionary<string, Dictionary<string, List<BlockUpgradeSchema.ValueRemap>>> ReadValueRemaps(JObject json)
		{
			var index = new Dictionary<string, List<BlockUpgradeSchema.ValueRemap>>(StringComparer.Ordinal);
			if (json["remappedPropertyValuesIndex"] is JObject indexJson)
			{
				foreach (JProperty property in indexJson.Properties())
				{
					var remaps = new List<BlockUpgradeSchema.ValueRemap>();
					foreach (JToken remap in property.Value)
					{
						IBlockState old = ReadState("value", (JObject) remap["old"]);
						IBlockState updated = ReadState("value", (JObject) remap["new"]);
						if (old != null && updated != null) remaps.Add(new BlockUpgradeSchema.ValueRemap(old, updated));
					}

					index[property.Name] = remaps;
				}
			}

			var result = new Dictionary<string, Dictionary<string, List<BlockUpgradeSchema.ValueRemap>>>(StringComparer.Ordinal);
			if (json["remappedPropertyValues"] is not JObject values) return result;

			foreach (JProperty block in values.Properties())
			{
				var perProperty = new Dictionary<string, List<BlockUpgradeSchema.ValueRemap>>(StringComparer.Ordinal);
				foreach (JProperty property in ((JObject) block.Value).Properties())
				{
					if (index.TryGetValue(property.Value.Value<string>(), out List<BlockUpgradeSchema.ValueRemap> remaps)) perProperty[property.Name] = remaps;
				}

				result[block.Name] = perProperty;
			}

			return result;
		}

		private static BlockUpgradeSchema.FlattenRule ReadFlattenRule(JObject json)
		{
			return new BlockUpgradeSchema.FlattenRule(
				json.Value<string>("prefix") ?? string.Empty,
				json.Value<string>("flattenedProperty"),
				json.Value<string>("suffix") ?? string.Empty,
				json.Value<string>("flattenedPropertyType"),
				json["flattenedValueRemaps"] is JObject remaps ? ReadStringMap(remaps) : null);
		}

		private static BlockUpgradeSchema.StateRemap ReadStateRemap(JObject json)
		{
			return new BlockUpgradeSchema.StateRemap(
				json["oldState"] is JObject oldState ? ReadStates(oldState) : new List<IBlockState>(),
				json.Value<string>("newName"),
				json["newFlattenedName"] is JObject flattened ? ReadFlattenRule(flattened) : null,
				json["newState"] is JObject newState ? ReadStates(newState) : new List<IBlockState>(),
				json["copiedState"]?.Select(name => name.Value<string>()).ToList() ?? new List<string>());
		}

		private static List<IBlockState> ReadStates(JObject json)
		{
			var states = new List<IBlockState>();
			foreach (JProperty property in json.Properties())
			{
				IBlockState state = ReadState(property.Name, (JObject) property.Value);
				if (state != null) states.Add(state);
			}

			return states;
		}

		/// <summary>
		///     A property value is written as its NBT type: <c>{"byte": 1}</c>, <c>{"int": 3}</c> or
		///     <c>{"string": "red"}</c>. The type matters, because the palette distinguishes a byte
		///     property from an int one.
		/// </summary>
		private static IBlockState ReadState(string name, JObject json)
		{
			if (json == null) return null;

			if (json["byte"] != null) return new BlockStateByte {Name = name, Value = (byte) json.Value<int>("byte")};
			if (json["int"] != null) return new BlockStateInt {Name = name, Value = json.Value<int>("int")};
			if (json["string"] != null) return new BlockStateString {Name = name, Value = json.Value<string>("string")};

			Log.Warn($"Block state property {name} has no known value type: {json}");
			return null;
		}
	}
}
