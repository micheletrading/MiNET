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
using System.Reflection;
using System.Text;
using fNbt;
using log4net;
using MiNET.Utils;
using Newtonsoft.Json;

namespace MiNET.Blocks.Upgrade
{
	/// <summary>
	///     Everything needed to read a block out of an old world, in one place: an id and meta pair
	///     from before blocks had state NBT, a state written by any version since, or the two combined
	///     the way a pre-1.13 palette entry stores them.
	///     <para>
	///     The data is PocketMine-MP's, unmodified. See ATTRIBUTION.md in Blocks/Data/
	///     BlockStateUpgradeSchema for what it is and what it took to produce.
	///     </para>
	/// </summary>
	public static class BlockDataUpgrader
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BlockDataUpgrader));

		private static readonly Lazy<BlockStateUpgrader> Upgrader = new(() => new BlockStateUpgrader(BlockUpgradeSchemaLoader.Load()));
		private static readonly Lazy<Dictionary<string, Dictionary<int, BlockStateData>>> IdMetaTable = new(LoadIdMetaTable);
		private static readonly Lazy<Dictionary<int, string>> LegacyIdToName = new(LoadLegacyIdMap);

		/// <summary>The version every upgraded state comes out stamped with.</summary>
		public static uint CurrentVersion => Upgrader.Value.OutputVersion;

		/// <summary>
		///     A palette entry as it sits on disk. Before 1.13 that is a name and a numeric
		///     <c>val</c>; from 1.13 it is a name, a states compound and the version that wrote it.
		///     Both end up walking the same schema chain.
		/// </summary>
		public static bool TryUpgrade(NbtCompound tag, out string name, out List<IBlockState> states)
		{
			name = null;
			states = null;

			string storedName = tag["name"]?.StringValue;
			if (storedName == null) return false;

			BlockStateData data;
			if (tag["val"] != null)
			{
				if (!TryFromIdMeta(storedName, tag["val"].ShortValue, out data)) return false;
			}
			else
			{
				var stored = new List<IBlockState>();
				if (tag["states"] is NbtCompound compound) stored.AddRange(ReadStates(compound));

				data = BlockStateData.From(storedName, stored, (uint) (tag["version"]?.IntValue ?? 0));
			}

			data = Upgrader.Value.Upgrade(data);

			name = data.Name;
			states = data.ToStateList();
			return true;
		}

		/// <summary>
		///     A legacy string id and meta, which is what a pre-1.13 palette entry and a classic
		///     section both hold. The table answers with the state that pair meant in 1.12, and the
		///     chain carries it the rest of the way.
		/// </summary>
		public static bool TryFromIdMeta(string name, int meta, out BlockStateData data)
		{
			data = null;

			if (!IdMetaTable.Value.TryGetValue(name, out Dictionary<int, BlockStateData> byMeta)) return false;
			if (!byMeta.TryGetValue(meta, out BlockStateData found) && !byMeta.TryGetValue(0, out found)) return false;

			// The table is shared, so hand out a copy for the chain to work on.
			data = BlockStateData.From(found.Name, found.States.Values, found.Version);
			return true;
		}

		/// <summary>A numeric block id, which is how everything before 1.2.13 stored blocks.</summary>
		public static bool TryFromIdMeta(int legacyId, int meta, out BlockStateData data)
		{
			data = null;
			return LegacyIdToName.Value.TryGetValue(legacyId, out string name) && TryFromIdMeta(name, meta, out data);
		}

		/// <summary>
		///     A numeric id and meta all the way to a current block: the table says what the pair meant
		///     in 1.12, and the schema chain carries that state to now. This is what a classic section,
		///     which stores nothing but ids and meta nibbles, resolves every block through.
		/// </summary>
		public static bool TryUpgradeIdMeta(int legacyId, int meta, out string name, out List<IBlockState> states)
		{
			name = null;
			states = null;

			if (!TryFromIdMeta(legacyId, meta, out BlockStateData data)) return false;

			data = Upgrader.Value.Upgrade(data);
			name = data.Name;
			states = data.ToStateList();
			return true;
		}

		/// <summary>
		///     A legacy string id and meta all the way to a current block, which is what a block item
		///     stored as name plus damage needs before it can be an item name again.
		/// </summary>
		public static bool TryUpgradeIdMeta(string legacyName, int meta, out string name, out List<IBlockState> states)
		{
			name = null;
			states = null;

			if (!TryFromIdMeta(legacyName, meta, out BlockStateData data)) return false;

			data = Upgrader.Value.Upgrade(data);
			name = data.Name;
			states = data.ToStateList();
			return true;
		}

		public static bool TryUpgradeIdMeta(string legacyName, int meta, out string name)
		{
			return TryUpgradeIdMeta(legacyName, meta, out name, out _);
		}

		private static IEnumerable<IBlockState> ReadStates(NbtCompound compound)
		{
			foreach (NbtTag tag in compound)
			{
				switch (tag)
				{
					case NbtByte value:
						yield return new BlockStateByte {Name = value.Name, Value = value.Value};
						break;
					case NbtInt value:
						yield return new BlockStateInt {Name = value.Name, Value = value.Value};
						break;
					case NbtString value:
						yield return new BlockStateString {Name = value.Name, Value = value.Value};
						break;
				}
			}
		}

		/// <summary>
		///     <c>id_meta_to_nbt_1.12.0.bin</c>: for each string id, for each meta it had, the 1.12
		///     state that pair meant. The 1.9 file next to it is for servers upgrading TO 1.9, 1.10 or
		///     1.11, where 1.12 states would be wrong; upgrading to current uses this one for blocks
		///     and block items alike.
		///     <para>
		///     Serialization, in order: a varint count of ids, then per id a varint-prefixed name, a
		///     varint count of metas, and per meta a varint meta value followed by the state as
		///     little-endian NBT.
		///     </para>
		/// </summary>
		private static Dictionary<string, Dictionary<int, BlockStateData>> LoadIdMetaTable()
		{
			var table = new Dictionary<string, Dictionary<int, BlockStateData>>(StringComparer.Ordinal);

			using Stream stream = typeof(Block).Assembly.GetManifestResourceStream("MiNET.Blocks.Data.BlockStateUpgradeSchema.id_meta_to_nbt_1.12.0.bin");
			if (stream == null)
			{
				Log.Error("The 1.12 id and meta table is missing from the assembly; old worlds will not read.");
				return table;
			}

			var file = new NbtFile
			{
				BigEndian = false,
				UseVarInt = false
			};

			uint idCount = VarInt.ReadUInt32(stream);
			for (uint id = 0; id < idCount; id++)
			{
				uint nameLength = VarInt.ReadUInt32(stream);
				var nameBytes = new byte[nameLength];
				stream.ReadExactly(nameBytes);
				string name = Encoding.UTF8.GetString(nameBytes);

				var byMeta = new Dictionary<int, BlockStateData>();
				uint metaCount = VarInt.ReadUInt32(stream);
				for (uint meta = 0; meta < metaCount; meta++)
				{
					int metaValue = (int) VarInt.ReadUInt32(stream);
					file.LoadFromStream(stream, NbtCompression.None);

					var compound = (NbtCompound) file.RootTag;
					var states = new List<IBlockState>();
					if (compound["states"] is NbtCompound stateCompound) states.AddRange(ReadStates(stateCompound));

					byMeta[metaValue] = BlockStateData.From(compound["name"].StringValue, states, (uint) (compound["version"]?.IntValue ?? 0));
				}

				table[name] = byMeta;
			}

			Log.Debug($"Loaded the 1.12 id and meta table: {table.Count} block names");
			return table;
		}

		private static Dictionary<int, string> LoadLegacyIdMap()
		{
			var byId = new Dictionary<int, string>();

			using Stream stream = typeof(Block).Assembly.GetManifestResourceStream("MiNET.Blocks.Data.BlockStateUpgradeSchema.block_legacy_id_map.json");
			if (stream == null)
			{
				Log.Error("The legacy block id map is missing from the assembly; numeric block ids will not read.");
				return byId;
			}

			using var reader = new StreamReader(stream);
			var byName = JsonConvert.DeserializeObject<Dictionary<string, int>>(reader.ReadToEnd());
			foreach (KeyValuePair<string, int> entry in byName) byId.TryAdd(entry.Value, entry.Key);

			return byId;
		}
	}
}
