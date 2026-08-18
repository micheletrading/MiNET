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
using System.Linq;
using MiNET.Utils;

namespace MiNET.Blocks.Upgrade
{
	/// <summary>
	///     A block as it was written to disk: a name, its properties, and the Bedrock version that
	///     wrote it. Properties are keyed by name here rather than listed, because every upgrade rule
	///     addresses them by name.
	/// </summary>
	public sealed class BlockStateData
	{
		public string Name { get; set; }
		public Dictionary<string, IBlockState> States { get; set; } = new(StringComparer.Ordinal);
		public uint Version { get; set; }

		public static BlockStateData From(string name, IEnumerable<IBlockState> states, uint version)
		{
			var data = new BlockStateData {Name = name, Version = version};
			foreach (IBlockState state in states) data.States[state.Name] = state;

			return data;
		}

		public List<IBlockState> ToStateList()
		{
			return States.Values.ToList();
		}

		public override string ToString()
		{
			return $"{Name} {{{string.Join(", ", States.Values.Select(state => state.ToString()))}}} v{Version >> 24 & 0xff}.{Version >> 16 & 0xff}.{Version >> 8 & 0xff}.{Version & 0xff}";
		}
	}

	/// <summary>
	///     Walks a stored block through every schema newer than the version that wrote it, which is how
	///     a 2016 block becomes a block the current palette knows. The rules and their order come from
	///     PocketMine-MP; see ATTRIBUTION.md next to the data.
	///     <para>
	///     Mojang does not always raise the version when it breaks the format, so several schemas can
	///     share a version id. Where that happens all of them are applied, including to a state already
	///     stamped with that version, which is why the equal-version case is only skipped when a
	///     version has a single schema.
	///     </para>
	/// </summary>
	internal sealed class BlockStateUpgrader
	{
		private readonly SortedDictionary<uint, List<BlockUpgradeSchema>> _schemas = new();

		public uint OutputVersion { get; private set; }

		public BlockStateUpgrader(IEnumerable<BlockUpgradeSchema> schemas)
		{
			foreach (BlockUpgradeSchema schema in schemas)
			{
				if (!_schemas.TryGetValue(schema.VersionId, out List<BlockUpgradeSchema> list))
				{
					list = new List<BlockUpgradeSchema>();
					_schemas[schema.VersionId] = list;
				}

				list.Add(schema);
				if (schema.VersionId > OutputVersion) OutputVersion = schema.VersionId;
			}

			foreach (List<BlockUpgradeSchema> list in _schemas.Values) list.Sort((left, right) => left.SchemaId.CompareTo(right.SchemaId));
		}

		public BlockStateData Upgrade(BlockStateData state)
		{
			uint version = state.Version;

			foreach (KeyValuePair<uint, List<BlockUpgradeSchema>> step in _schemas)
			{
				if (version > step.Key || (step.Value.Count == 1 && version == step.Key)) continue;

				foreach (BlockUpgradeSchema schema in step.Value) Apply(schema, state);
			}

			state.Version = OutputVersion;
			return state;
		}

		private static void Apply(BlockUpgradeSchema schema, BlockStateData state)
		{
			// A state remap replaces the whole thing, so nothing else in this schema applies to it.
			if (ApplyStateRemap(schema, state)) return;

			string oldName = state.Name;

			if (schema.RenamedIds.TryGetValue(oldName, out string renamed)) state.Name = renamed;
			else if (schema.FlattenedProperties.TryGetValue(oldName, out BlockUpgradeSchema.FlattenRule flatten)) ApplyFlatten(flatten, state);

			// Everything below is indexed by the name and property names as they were before this
			// step, which is what makes the order of these four irrelevant.
			ApplyAddedProperties(schema, oldName, state);
			ApplyRemovedProperties(schema, oldName, state);
			ApplyRenamedProperties(schema, oldName, state);
			ApplyRemappedValues(schema, oldName, state);
		}

		private static bool ApplyStateRemap(BlockUpgradeSchema schema, BlockStateData state)
		{
			if (!schema.RemappedStates.TryGetValue(state.Name, out List<BlockUpgradeSchema.StateRemap> remaps)) return false;

			foreach (BlockUpgradeSchema.StateRemap remap in remaps)
			{
				// The old state is a filter, not an exact match, and the rules arrive most specific
				// first, so the first one that matches is the one that applies.
				if (remap.Old.Count > state.States.Count) continue;
				if (!remap.Old.All(required => state.States.TryGetValue(required.Name, out IBlockState held) && SameValue(held, required))) continue;

				string newName = remap.NewName;
				if (newName == null && remap.NewFlattenedName != null)
				{
					// The flatten rule here names the block; its state changes are the remap's job.
					var probe = new BlockStateData {Name = state.Name, States = new Dictionary<string, IBlockState>(state.States, StringComparer.Ordinal)};
					ApplyFlatten(remap.NewFlattenedName, probe);
					newName = probe.Name;
				}

				var replacement = new Dictionary<string, IBlockState>(StringComparer.Ordinal);
				foreach (IBlockState newState in remap.New) replacement[newState.Name] = newState;
				foreach (string copied in remap.CopiedState)
				{
					if (state.States.TryGetValue(copied, out IBlockState carried)) replacement[copied] = carried;
				}

				state.Name = newName ?? state.Name;
				state.States = replacement;
				return true;
			}

			return false;
		}

		private static void ApplyFlatten(BlockUpgradeSchema.FlattenRule flatten, BlockStateData state)
		{
			if (!state.States.TryGetValue(flatten.FlattenedProperty, out IBlockState value)) return;
			if (!TypeMatches(flatten.FlattenedPropertyType, value)) return;

			string name = flatten.NameFor(value);
			if (name == null) return;

			state.Name = name;
			state.States.Remove(flatten.FlattenedProperty);
		}

		private static void ApplyAddedProperties(BlockUpgradeSchema schema, string oldName, BlockStateData state)
		{
			if (!schema.AddedProperties.TryGetValue(oldName, out Dictionary<string, IBlockState> added)) return;

			foreach (KeyValuePair<string, IBlockState> property in added)
			{
				if (!state.States.ContainsKey(property.Key)) state.States[property.Key] = property.Value;
			}
		}

		private static void ApplyRemovedProperties(BlockUpgradeSchema schema, string oldName, BlockStateData state)
		{
			if (!schema.RemovedProperties.TryGetValue(oldName, out List<string> removed)) return;

			foreach (string property in removed) state.States.Remove(property);
		}

		private static void ApplyRenamedProperties(BlockUpgradeSchema schema, string oldName, BlockStateData state)
		{
			if (!schema.RenamedProperties.TryGetValue(oldName, out Dictionary<string, string> renames)) return;

			foreach (KeyValuePair<string, string> rename in renames)
			{
				if (!state.States.TryGetValue(rename.Key, out IBlockState value)) continue;

				state.States.Remove(rename.Key);

				// The value remap has to happen here rather than after: remaps are indexed by the old
				// property name, and after the rename there is nothing left to look it up by.
				state.States[rename.Value] = Rename(NewValueFor(schema, oldName, rename.Key, value), rename.Value);
			}
		}

		private static void ApplyRemappedValues(BlockUpgradeSchema schema, string oldName, BlockStateData state)
		{
			if (!schema.RemappedPropertyValues.TryGetValue(oldName, out Dictionary<string, List<BlockUpgradeSchema.ValueRemap>> remaps)) return;

			foreach (string property in remaps.Keys)
			{
				if (!state.States.TryGetValue(property, out IBlockState value)) continue;

				state.States[property] = Rename(NewValueFor(schema, oldName, property, value), property);
			}
		}

		private static IBlockState NewValueFor(BlockUpgradeSchema schema, string oldName, string property, IBlockState value)
		{
			if (!schema.RemappedPropertyValues.TryGetValue(oldName, out Dictionary<string, List<BlockUpgradeSchema.ValueRemap>> perProperty)) return value;
			if (!perProperty.TryGetValue(property, out List<BlockUpgradeSchema.ValueRemap> remaps)) return value;

			foreach (BlockUpgradeSchema.ValueRemap remap in remaps)
			{
				if (SameValue(value, remap.Old)) return remap.New;
			}

			return value;
		}

		/// <summary>Values are compared by type and value alone. What a property is called is the
		/// rule's business, not the value's, and the schema writes remap values under a placeholder
		/// name.</summary>
		private static bool SameValue(IBlockState left, IBlockState right)
		{
			return (left, right) switch
			{
				(BlockStateByte a, BlockStateByte b) => a.Value == b.Value,
				(BlockStateInt a, BlockStateInt b) => a.Value == b.Value,
				(BlockStateString a, BlockStateString b) => a.Value == b.Value,
				_ => false
			};
		}

		private static bool TypeMatches(string expected, IBlockState value)
		{
			return expected switch
			{
				"byte" => value is BlockStateByte,
				"int" => value is BlockStateInt,
				"string" => value is BlockStateString,
				_ => true // Older schemas do not state the type; anything the rule can stringify goes.
			};
		}

		private static IBlockState Rename(IBlockState state, string name)
		{
			if (state.Name == name) return state;

			return state switch
			{
				BlockStateByte number => new BlockStateByte {Name = name, Value = number.Value},
				BlockStateInt number => new BlockStateInt {Name = name, Value = number.Value},
				BlockStateString text => new BlockStateString {Name = name, Value = text.Value},
				_ => state
			};
		}
	}
}
