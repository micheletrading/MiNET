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
using log4net;
using MiNET.Blocks;
using MiNET.Utils;
using Newtonsoft.Json;

namespace MiNET.Items
{
	/// <summary>
	///     Turns a pre-flattening <c>(id, meta)</c> pair from an old save into a modern item.
	///     This is the only place legacy numeric item ids are understood, and it runs in one
	///     direction: a world read through it is written back in the modern form, so a save upgrades
	///     itself once and never comes back here.
	///     Two mappings do the work, both from the r16 era. "Complex" entries split one old id across
	///     several current identities by metadata, the way <c>banner_pattern:3</c> became
	///     <c>mojang_banner_pattern</c>; the metadata is consumed by the split and does not survive.
	///     "Simple" entries are plain renames and keep their metadata, since there it still means
	///     durability.
	///     Ids at or below 255 are block ids, not item ids: the two numbering spaces overlapped and
	///     the block one won for low values. That is how the old ItemFactory read them too.
	/// </summary>
	public static class LegacyItemUpgrader
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(LegacyItemUpgrader));

		private static readonly Lazy<Legacy> _legacy = new Lazy<Legacy>(Build);

		public static Item Upgrade(short id, short metadata = 0, int count = 1)
		{
			if (id == 0) return new ItemAir();

			Legacy legacy = _legacy.Value;

			// A metadata-split identity resolves to its own name and drops the metadata with it.
			if (legacy.Complex.TryGetValue((id, metadata), out string split)) return ItemFactory.GetItemByName(split, 0, count);

			if (legacy.ByItemId.TryGetValue(id, out string name)) return ItemFactory.GetItemByName(name, metadata, count);

			// Below 256 the number is a block id. Metadata selected the variant back then, which is
			// now a name of its own, so go through the palette to land on the right state.
			if (id <= 255)
			{
				Block block = BlockFactory.GetBlockById(id);
				if (block != null && block.GetType() != typeof(Block))
				{
					uint runtimeId = BlockFactory.GetRuntimeId(id, (byte) metadata);
					if (runtimeId < BlockFactory.BlockPalette.Count) block.SetState(BlockFactory.BlockPalette[(int) runtimeId]);
					return new ItemBlock(block) {Count = (byte) count};
				}
			}

			Log.Warn($"No modern item for legacy id {id} meta {metadata}; dropping the stack");
			return new ItemAir();
		}

		private sealed class Legacy
		{
			public Dictionary<short, string> ByItemId { get; init; }
			public Dictionary<(short Id, short Meta), string> Complex { get; init; }
		}

		private static Legacy Build()
		{
			// Old registry name -> old numeric id, and old name -> current name(s).
			var idsByOldName = ResourceUtil.ReadResource<Dictionary<string, short>>("item_id_map.json", typeof(Item), "Data");
			var renames = ResourceUtil.ReadResource<R16ToCurrentMap>("r16_to_current_item_map.json", typeof(Item), "Data");

			var byItemId = new Dictionary<short, string>();
			foreach (KeyValuePair<string, short> entry in idsByOldName)
			{
				string current = renames.Simple.TryGetValue(entry.Key, out string renamed) ? renamed : entry.Key;
				byItemId.TryAdd(entry.Value, current);
			}

			var complex = new Dictionary<(short, short), string>();
			foreach (KeyValuePair<string, Dictionary<string, string>> entry in renames.Complex)
			{
				if (!idsByOldName.TryGetValue(entry.Key, out short oldId)) continue;
				foreach (KeyValuePair<string, string> variant in entry.Value)
				{
					if (short.TryParse(variant.Key, out short meta)) complex.TryAdd((oldId, meta), variant.Value);
				}
			}

			Log.Debug($"Legacy item upgrade table: {byItemId.Count} ids, {complex.Count} metadata splits");
			return new Legacy {ByItemId = byItemId, Complex = complex};
		}
	}

	/// <summary>
	///     r16_to_current_item_map.json: "simple" is a one-to-one rename, "complex" splits one old
	///     identity across several current ones by metadata.
	/// </summary>
	internal class R16ToCurrentMap
	{
		[JsonProperty("complex")] public Dictionary<string, Dictionary<string, string>> Complex { get; set; } = new Dictionary<string, Dictionary<string, string>>();

		[JsonProperty("simple")] public Dictionary<string, string> Simple { get; set; } = new Dictionary<string, string>();
	}
}
