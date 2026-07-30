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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Crafting;
using MiNET.Items;
using MiNET.Net;

namespace MiNET.Test.Crafting
{
	[TestClass, DoNotParallelize]
	public class RecipeManagerTests
	{
		/// <summary>
		///     The registry is the source of truth for crafting, so a recipe the server publishes must be
		///     reachable by the network id the client will send back for it. A duplicate or missing id means
		///     some recipe can never be crafted.
		/// </summary>
		[TestMethod]
		public void EveryRegisteredRecipeIsResolvableByItsNetworkId()
		{
			Recipes recipes = RecipeManager.Recipes;

			Assert.IsTrue(recipes.Count > 0, "Recipe registry is empty; Data/recipes.json did not load");

			List<int> networkIds = recipes.Select(r => r.NetworkId).ToList();
			Assert.AreEqual(networkIds.Count, networkIds.Distinct().Count(), "Recipe network ids are not unique");
			CollectionAssert.DoesNotContain(networkIds, 0, "A registered recipe was left without a network id");

			foreach (Recipe recipe in recipes)
			{
				Assert.IsTrue(RecipeManager.TryGetByNetworkId(recipe.NetworkId, out Recipe resolved), $"Recipe {recipe.NetworkId} is not resolvable");
				Assert.AreSame(recipe, resolved);
			}
		}

		/// <summary>
		///     The CraftingData packet is only a projection of the registry, and the client has to be able to
		///     parse it. Encoding the registry and reading it back with MiNET's own reader has to consume
		///     every byte and produce the same recipes: a reader that stops short of the buffer, or a writer
		///     that emits a field the reader doesn't expect, breaks the client's join.
		/// </summary>
		[TestMethod]
		public void CraftingDataProjectionOfTheRegistryRoundtrips()
		{
			McpeCraftingData packet = RecipeManager.CreateCraftingDataPacket();
			int recipeCount = packet.recipes.Count;
			byte[] encoded = packet.Encode();
			packet.PutPool();

			var decoded = (McpeCraftingData) PacketFactory.Create(0x34, encoded, "mcpe");
			Assert.IsNotNull(decoded, "CraftingData built from the registry does not decode");

			Assert.AreEqual(recipeCount, decoded.recipes.Count);
			Assert.AreEqual(RecipeManager.PotionTypeRecipes.Length, decoded.potionTypeRecipes.Length);
			Assert.AreEqual(RecipeManager.PotionContainerRecipes.Length, decoded.potionContainerRecipes.Length);
			Assert.AreEqual(RecipeManager.MaterialReducerRecipes.Length, decoded.materialReducerRecipes.Length);

			// Re-encoding what was decoded is the zero-leftover proof: any byte the reader mis-sized shows
			// up as a difference here.
			byte[] reencoded = decoded.Encode();
			decoded.PutPool();
			CollectionAssert.AreEqual(encoded, reencoded, "Decoding our own CraftingData bytes does not reproduce them");
		}

		/// <summary>
		///     Recipes reference items by registry string id. A result that lost its wire identity would be
		///     sent to the client as air, so every result must resolve to a real item registry entry.
		/// </summary>
		[TestMethod]
		public void RecipeResultsResolveToItemRegistryEntries()
		{
			var unresolved = new List<string>();

			foreach (Recipe recipe in RecipeManager.Recipes)
			{
				IEnumerable<Item> results = recipe switch
				{
					ShapedRecipe shaped => shaped.Result,
					ShapelessRecipe shapeless => shapeless.Result,
					SmeltingRecipe smelting => new[] {smelting.Result},
					SmithingTransformRecipe transform => new[] {transform.Result},
					_ => new Item[0]
				};

				foreach (Item result in results.Where(r => r != null && r.Count > 0))
				{
					if (result.NetworkId <= 0 && result.NetworkId != -1) continue; // negative registry ids are block items
					if (ItemFactory.Itemstates.Any(s => s.Id == result.NetworkId)) continue;

					unresolved.Add($"{recipe.NetworkId}: network id {result.NetworkId}");
				}
			}

			Assert.AreEqual(0, unresolved.Count, "Unresolved recipe results: " + string.Join(", ", unresolved.Take(10)));
		}

		/// <summary>
		///     Tag ingredients ("any plank") are semantics, not an encoding shortcut. Flattening one to a
		///     single item would silently narrow the recipe, so the vanilla set must still carry tags.
		/// </summary>
		[TestMethod]
		public void TagIngredientsSurviveAsTags()
		{
			int tagIngredients = RecipeManager.Recipes
				.SelectMany(r => r switch
				{
					ShapedRecipe shaped => shaped.Input.AsEnumerable(),
					ShapelessRecipe shapeless => shapeless.Input,
					_ => new Item[0]
				})
				.Count(i => i?.IngredientDescriptor?.Type == 3);

			Assert.IsTrue(tagIngredients > 0, "No tag ingredients in the registry; they were flattened to plain items");
		}
	}
}