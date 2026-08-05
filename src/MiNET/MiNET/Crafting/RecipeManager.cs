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
using MiNET.Net;
using MiNET.Worlds;

namespace MiNET.Crafting
{
	/// <summary>
	///     The server's recipe registry: the single source of truth for what can be crafted, smelted,
	///     brewed and reduced. Recipes are domain data - the server's own crafting logic resolves them
	///     (see ItemStackInventoryManager) and plugins extend them through <see cref="Add" /> - and the
	///     CraftingData packet is only a projection of this registry onto the wire
	///     (Player.SendCraftingRecipes).
	///     <para>
	///         The vanilla 1.26.34 recipe set is generated code (<see cref="RecipeData" />, generated from
	///         Data/recipes.json by MiNET.Test's GenerateRecipesTests), which references every item by
	///         registry string id, never by a numeric wire id, resolved through <see cref="RecipeItems" />:
	///         string ids are durable identity, the numbers are per-version registry assignments resolved
	///         there. Recipe network ids are not part of that data either - they are the handles the client
	///         sends back in a CraftRecipe action, so the registry assigns them in load order starting at 1.
	///     </para>
	/// </summary>
	public class RecipeManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeManager));

		private static readonly object _sync = new object();

		private static Recipes _recipes;
		private static Dictionary<int, Recipe> _byNetworkId;
		private static int _nextNetworkId = 1;

		private static PotionTypeRecipe[] _potionTypeRecipes = new PotionTypeRecipe[0];
		private static PotionContainerChangeRecipe[] _potionContainerRecipes = new PotionContainerChangeRecipe[0];
		private static MaterialReducerRecipe[] _materialReducerRecipes = new MaterialReducerRecipe[0];

		private static McpeWrapper _craftingData;

		/// <summary>
		///     Every registered recipe, in registration order. Mutate through <see cref="Add" /> and
		///     <see cref="Remove(Recipe)" /> so network ids get assigned and the cached CraftingData batch
		///     is invalidated; adding to this list directly leaves the recipe unreachable by network id.
		/// </summary>
		public static Recipes Recipes
		{
			get
			{
				EnsureLoaded();
				return _recipes;
			}
		}

		/// <summary>Brewing-stand potion mixes (input potion + ingredient -> output potion).</summary>
		public static PotionTypeRecipe[] PotionTypeRecipes
		{
			get
			{
				EnsureLoaded();
				return _potionTypeRecipes;
			}
			set
			{
				EnsureLoaded();
				lock (_sync)
				{
					_potionTypeRecipes = value ?? new PotionTypeRecipe[0];
					InvalidateCraftingData();
				}
			}
		}

		/// <summary>Brewing-stand container mixes (bottle type changes, e.g. potion -> splash potion).</summary>
		public static PotionContainerChangeRecipe[] PotionContainerRecipes
		{
			get
			{
				EnsureLoaded();
				return _potionContainerRecipes;
			}
			set
			{
				EnsureLoaded();
				lock (_sync)
				{
					_potionContainerRecipes = value ?? new PotionContainerChangeRecipe[0];
					InvalidateCraftingData();
				}
			}
		}

		/// <summary>Education-Edition material reducer recipes.</summary>
		public static MaterialReducerRecipe[] MaterialReducerRecipes
		{
			get
			{
				EnsureLoaded();
				return _materialReducerRecipes;
			}
			set
			{
				EnsureLoaded();
				lock (_sync)
				{
					_materialReducerRecipes = value ?? new MaterialReducerRecipe[0];
					InvalidateCraftingData();
				}
			}
		}

		/// <summary>
		///     Registers a recipe and returns the network id assigned to it. Recipe types that carry no
		///     network id on the wire (furnace/smelting) still get one, so server-side lookups work for
		///     every recipe.
		/// </summary>
		public static int Add(Recipe recipe)
		{
			if (recipe == null) throw new ArgumentNullException(nameof(recipe));

			EnsureLoaded();
			lock (_sync)
			{
				return AddLocked(recipe);
			}
		}

		public static bool Remove(Recipe recipe)
		{
			if (recipe == null) return false;

			EnsureLoaded();
			lock (_sync)
			{
				if (!_recipes.Remove(recipe)) return false;

				_byNetworkId.Remove(recipe.NetworkId);
				InvalidateCraftingData();
				return true;
			}
		}

		public static bool Remove(int networkId)
		{
			EnsureLoaded();
			lock (_sync)
			{
				if (!_byNetworkId.TryGetValue(networkId, out Recipe recipe)) return false;

				_byNetworkId.Remove(networkId);
				_recipes.Remove(recipe);
				InvalidateCraftingData();
				return true;
			}
		}

		/// <summary>
		///     Resolves the network id a client sent in a CraftRecipe / CraftRecipeAuto action back to the
		///     registered recipe. False means the client asked for a recipe this server never published.
		/// </summary>
		public static bool TryGetByNetworkId(int networkId, out Recipe recipe)
		{
			EnsureLoaded();
			lock (_sync)
			{
				return _byNetworkId.TryGetValue(networkId, out recipe);
			}
		}

		public static int GetNetworkId(Recipe recipe)
		{
			return recipe?.NetworkId ?? 0;
		}

		/// <summary>
		///     The registry as a ready-to-send, permanently cached CraftingData batch. The bytes are the
		///     same for every player, so they are produced once and reused; any registry change drops the
		///     cache.
		/// </summary>
		public static McpeWrapper GetCraftingData()
		{
			EnsureLoaded();
			lock (_sync)
			{
				if (_craftingData != null) return _craftingData;

				McpeCraftingData craftingData = CreateCraftingDataPacket();
				McpeWrapper packet = Level.CreateMcpeBatch(craftingData.Encode());
				craftingData.PutPool();
				packet.MarkPermanent(true);
				_craftingData = packet;

				return _craftingData;
			}
		}

		/// <summary>
		///     A fresh CraftingData packet projecting the whole registry. The caller owns the packet
		///     (sending it pools it). The recipe list is a snapshot, so a plugin registering a recipe while
		///     a player is joining cannot break that player's encode.
		/// </summary>
		public static McpeCraftingData CreateCraftingDataPacket()
		{
			EnsureLoaded();

			McpeCraftingData craftingData = McpeCraftingData.CreateObject();
			lock (_sync)
			{
				var snapshot = new Recipes();
				snapshot.AddRange(_recipes);

				craftingData.recipes = snapshot;
				craftingData.potionTypeRecipes = _potionTypeRecipes;
				craftingData.potionContainerRecipes = _potionContainerRecipes;
				craftingData.materialReducerRecipes = _materialReducerRecipes;
				craftingData.isClean = true;
			}

			return craftingData;
		}

		private static int AddLocked(Recipe recipe)
		{
			recipe.NetworkId = _nextNetworkId++;
			_recipes.Add(recipe);
			_byNetworkId[recipe.NetworkId] = recipe;
			InvalidateCraftingData();

			return recipe.NetworkId;
		}

		private static void InvalidateCraftingData()
		{
			if (_craftingData == null) return;

			_craftingData.MarkPermanent(false);
			_craftingData.PutPool();
			_craftingData = null;
		}

		private static void EnsureLoaded()
		{
			if (_recipes != null) return;

			lock (_sync)
			{
				if (_recipes != null) return;

				_recipes = new Recipes();
				_byNetworkId = new Dictionary<int, Recipe>();

				RecipeDataFile file = RecipeLoader.Load();

				foreach (Recipe recipe in RecipeLoader.CreateVanillaRecipes(file))
				{
					AddLocked(recipe);
				}

				_potionTypeRecipes = RecipeLoader.CreatePotionTypeRecipes(file);
				_potionContainerRecipes = RecipeLoader.CreatePotionContainerRecipes(file);
				_materialReducerRecipes = RecipeLoader.CreateMaterialReducerRecipes(file);

				Log.Info($"Recipe registry loaded: {_recipes.Count} recipes, {_potionTypeRecipes.Length} potion mixes, {_potionContainerRecipes.Length} container mixes, {_materialReducerRecipes.Length} material reducers");
			}
		}

		// Wire recipe-type discriminators, see the "Recipe Types" enum in MCPE Protocol.xml. Public so
		// RecipeData (generated from Data/recipes.json) can set Shapeless/ShapedRecipe.RecipeType without
		// duplicating these values.
		public const int Shapeless = 0;
		public const int Shaped = 1;
		public const int Furnace = 2;
		public const int FurnaceData = 3;
		public const int Multi = 4;
		public const int ShulkerBox = 5;
		public const int ShapelessChemistry = 6;
		public const int ShapedChemistry = 7;
		public const int SmithingTransform = 8;
		public const int SmithingTrim = 9;
	}
}