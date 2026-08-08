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
using System.Linq;
using fNbt;
using log4net;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;

namespace MiNET.Crafting
{
	/// <summary>
	///     Factory methods that turn a recipe ingredient/result described by registry string id (never a
	///     numeric wire id) into the <see cref="Item" /> a <see cref="Recipe" /> carries. This is the one
	///     place recipe data resolves names to runtime numbers - through <see cref="ItemFactory" /> and
	///     <see cref="BlockFactory" /> - so both the generated vanilla recipe set
	///     (<see cref="RecipeData" />) and hand-written/plugin recipes build ingredients and results the
	///     same way, and never bake a numeric id into source.
	/// </summary>
	public static class RecipeItems
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeItems));

		/// <summary>"Any variant" aux value on an ingredient, 0x7fff.</summary>
		private const short WildcardMetadata = 32767;

		/// <summary>A plain item ingredient (wire "int_id_meta" variant), resolved by registry name.</summary>
		public static Item Ingredient(string name, short meta = 0, int count = 1)
		{
			Item item = ItemFactory.GetItemByName(name, meta, count);
			item.IngredientDescriptor = new RecipeIngredientDescriptor {Type = 1, Name = name, Metadata = meta};
			return item;
		}

		/// <summary>An item-tag ingredient (matches any item carrying the tag, e.g. "minecraft:planks").</summary>
		public static Item Tag(string tag, int count = 1)
		{
			return new ItemAir
			{
				Count = (byte) count,
				IngredientDescriptor = new RecipeIngredientDescriptor {Type = 3, Text = tag}
			};
		}

		/// <summary>A molang-expression ingredient.</summary>
		public static Item Molang(string expr, byte version, int count = 1)
		{
			return new ItemAir
			{
				Count = (byte) count,
				IngredientDescriptor = new RecipeIngredientDescriptor {Type = 2, Text = expr, MolangVersion = version}
			};
		}

		/// <summary>A "string_id_meta" ingredient variant, deferred to a name the client resolves itself.</summary>
		public static Item Deferred(string name, short meta, int count = 1)
		{
			return new ItemAir
			{
				Count = (byte) count,
				IngredientDescriptor = new RecipeIngredientDescriptor {Type = 4, Text = name, Metadata = meta}
			};
		}

		/// <summary>
		///     A "complex_alias" ingredient, e.g. "minecraft:log" standing for any log. Vanilla does
		///     not use a complex-alias descriptor for these: it writes an ordinary item descriptor
		///     naming the alias, with wildcard metadata and a block runtime id of -1 (any state).
		/// </summary>
		public static Item Alias(string alias, int count = 1)
		{
			// Built directly rather than through ItemFactory: an alias names a pre-flattening
			// aggregate (minecraft:log, minecraft:wood and friends) that the palette no longer
			// holds, so resolving it would warn about a missing block state on every recipe that
			// uses one, for a block nothing here wants. Only the name reaches the wire.
			return new Item(alias, WildcardMetadata, count)
			{
				NetworkId = ItemFactory.GetNetworkIdByName(alias),
				NetworkMetadata = WildcardMetadata,
				RuntimeId = -1,
				IngredientDescriptor = new RecipeIngredientDescriptor {Type = 1, Name = alias, Metadata = WildcardMetadata}
			};
		}

		/// <summary>An empty slot: no ingredient at all.</summary>
		public static Item Empty()
		{
			return new ItemAir {Count = 0};
		}

		/// <summary>
		///     A recipe result item. The typed item comes from the registry name so server logic gets a real
		///     Item; NetworkId/NetworkMetadata pin the exact wire identity of that name (see WriteNetworkItemInstanceDescriptor).
		/// </summary>
		public static Item Result(string name, short meta = 0, int count = 1, string nbtB64 = null)
		{
			if (name == null || count == 0 || name == "minecraft:air") return new ItemAir {Count = 0};

			short networkId = ItemFactory.GetNetworkIdByName(name);
			if (networkId == 0) throw new FormatException($"Recipe result '{name}' is not in the item registry");

			Item item = ItemFactory.GetItemByName(name, meta, count);
			item.NetworkId = networkId;
			item.NetworkMetadata = meta;
			item.Count = (byte) count;

			// A plain result names no block state, so it carries no block runtime id. ItemBlock's
			// constructor sets one for any item that happens to have a block form, which is not
			// what this factory means; results that do want a state go through BlockResult.
			item.RuntimeId = 0;

			if (nbtB64 != null) item.ExtraData = (NbtCompound) JoinSequenceData.NbtFromBase64(nbtB64).NbtFile.RootTag;

			return item;
		}

		/// <summary>
		///     A recipe result item that places a block, e.g. a shulker box color result. The block-state
		///     reference becomes the block's network hash through the palette instead of a runtime number
		///     carried in source; a state name with no explicit <paramref name="states" /> means the block's
		///     default (first palette) state.
		/// </summary>
		public static Item BlockResult(string name, int count = 1, Dictionary<string, string> states = null, string nbtB64 = null, short meta = 0)
		{
			if (name == null || count == 0 || name == "minecraft:air") return new ItemAir {Count = 0};

			short networkId = ItemFactory.GetNetworkIdByName(name);
			if (networkId == 0) throw new FormatException($"Recipe result '{name}' is not in the item registry");

			Item item = ItemFactory.GetItemByName(name, meta, count);
			item.NetworkId = networkId;
			item.NetworkMetadata = meta;
			item.Count = (byte) count;
			item.RuntimeId = ResolveBlockStateHash(name, states);
			if (nbtB64 != null) item.ExtraData = (NbtCompound) JoinSequenceData.NbtFromBase64(nbtB64).NbtFile.RootTag;

			return item;
		}

		// Block-state reference -> whatever the protocol's single "block runtime id" field carries,
		// which BlockFactory decides. This used to hash unconditionally, which is only right in one
		// of the two modes.
		private static int ResolveBlockStateHash(string name, Dictionary<string, string> states)
		{
			if (states == null || states.Count == 0)
			{
				BlockStateContainer defaultState = BlockFactory.GetDefaultState(name);
				if (defaultState == null) Log.Warn($"Recipe result references unknown block '{name}'");
				return defaultState == null ? 0 : unchecked((int) BlockFactory.GetNetworkId(defaultState));
			}

			foreach (BlockStateContainer state in BlockFactory.BlockPalette)
			{
				if (!string.Equals(state.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
				if (state.States.Count != states.Count) continue;
				if (!state.States.All(s => states.TryGetValue(s.Name, out string value) && string.Equals(StateValue(s), value, StringComparison.OrdinalIgnoreCase))) continue;

				return unchecked((int) BlockFactory.GetNetworkId(state));
			}

			Log.Warn($"Recipe result references block state '{name}' that is not in the palette; falling back to its default state");
			BlockStateContainer fallback = BlockFactory.GetDefaultState(name);
			return fallback == null ? 0 : unchecked((int) BlockFactory.GetNetworkId(fallback));
		}

		private static string StateValue(IBlockState state)
		{
			return state switch
			{
				BlockStateByte b => b.Value.ToString(),
				BlockStateInt i => i.Value.ToString(),
				BlockStateString s => s.Value,
				_ => null
			};
		}
	}
}
