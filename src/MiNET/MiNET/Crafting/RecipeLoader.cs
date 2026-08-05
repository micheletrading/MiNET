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
using MiNET.Items;
using MiNET.Utils;
using Newtonsoft.Json;

namespace MiNET.Crafting
{
	/// <summary>
	///     Builds the vanilla recipe registry from Data/recipes.json.gz at startup.
	///
	///     This was generated C# for a while (RecipeData.generated.cs, 2.4 MB of source constructing
	///     the same objects). Generation earns its keep where it produces an API somebody writes
	///     against, the way block and item classes do. Nobody references a recipe symbol: plugins add
	///     recipes through RecipeManager and the RecipeItems factories. Measured, the generated path
	///     cost 911 ms against 74 ms to parse the JSON, because the real work in both is resolving
	///     every ingredient name through ItemFactory and BlockFactory, which generation never moved
	///     to build time. It bought a megabyte of IL and a 64 KB IL-limit workaround for nothing.
	///
	///     Shipped gzipped: 3.2 MB of very repetitive JSON compresses to 150 KB, and .NET does not
	///     compress embedded resources. Read as a stream, so neither the decompressed text nor a
	///     JObject tree is ever materialised.
	///
	///     recipes.json is ours, not a vendor file: names only, no numeric wire ids, with our own
	///     ingredient descriptor kinds.
	/// </summary>
	public static class RecipeLoader
	{
		private const string ResourceName = "recipes.json.gz";

		private const int Shapeless = 0;
		private const int Shaped = 1;
		private const int Furnace = 2;
		private const int FurnaceData = 3;
		private const int Multi = 4;
		private const int ShulkerBox = 5;
		private const int ShapelessChemistry = 6;
		private const int ShapedChemistry = 7;
		private const int SmithingTransform = 8;
		private const int SmithingTrim = 9;

		public static RecipeDataFile Load()
		{
			using Stream compressed = OpenResource();
			using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
			using var text = new StreamReader(gzip);
			using var reader = new JsonTextReader(text);

			return new JsonSerializer().Deserialize<RecipeDataFile>(reader)
				?? throw new FormatException($"{ResourceName} deserialized to nothing");
		}

		private static Stream OpenResource()
		{
			Assembly assembly = typeof(RecipeLoader).Assembly;
			string name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(ResourceName, StringComparison.Ordinal));
			if (name == null) throw new FileNotFoundException($"Embedded resource {ResourceName} not found");

			return assembly.GetManifestResourceStream(name);
		}

		/// <summary>The vanilla recipe set, in file order: registry network ids depend on load order.</summary>
		public static Recipes CreateVanillaRecipes(RecipeDataFile file)
		{
			var recipes = new Recipes();
			foreach (RecipeDef def in file.Recipes) recipes.Add(BuildRecipe(def));
			return recipes;
		}

		public static PotionTypeRecipe[] CreatePotionTypeRecipes(RecipeDataFile file)
		{
			return file.PotionTypeRecipes.Select(p => new PotionTypeRecipe
			{
				Input = ItemFactory.GetNetworkIdByName(p.Input),
				InputMeta = p.InputMeta,
				Ingredient = ItemFactory.GetNetworkIdByName(p.Ingredient),
				IngredientMeta = p.IngredientMeta,
				Output = ItemFactory.GetNetworkIdByName(p.Output),
				OutputMeta = p.OutputMeta
			}).ToArray();
		}

		public static PotionContainerChangeRecipe[] CreatePotionContainerRecipes(RecipeDataFile file)
		{
			return file.PotionContainerRecipes.Select(p => new PotionContainerChangeRecipe
			{
				Input = ItemFactory.GetNetworkIdByName(p.Input),
				Ingredient = ItemFactory.GetNetworkIdByName(p.Ingredient),
				Output = ItemFactory.GetNetworkIdByName(p.Output)
			}).ToArray();
		}

		public static MaterialReducerRecipe[] CreateMaterialReducerRecipes(RecipeDataFile file)
		{
			return file.MaterialReducerRecipes.Select(m => new MaterialReducerRecipe(
				ItemFactory.GetNetworkIdByName(m.Input),
				m.InputMeta,
				m.Outputs.Select(o => new MaterialReducerRecipe.MaterialReducerRecipeOutput(ItemFactory.GetNetworkIdByName(o.Name), o.Count)).ToArray())).ToArray();
		}

		private static Recipe BuildRecipe(RecipeDef def)
		{
			switch (def.Type)
			{
				case Shapeless:
				case ShulkerBox:
				case ShapelessChemistry:
				{
					var recipe = new ShapelessRecipe
					{
						RecipeType = def.Type,
						RecipeId = def.RecipeId,
						Id = new UUID(def.Uuid),
						Block = def.Block,
						Priority = def.Priority,
						Unlocking = BuildUnlocking(def.Unlocking)
					};
					foreach (IngredientDef i in def.Ingredients) recipe.Input.Add(BuildIngredient(i));
					foreach (ItemDef r in def.Results) recipe.Result.Add(BuildResult(r));
					return recipe;
				}
				case Shaped:
				case ShapedChemistry:
				{
					var recipe = new ShapedRecipe(def.Width, def.Height)
					{
						RecipeType = def.Type,
						RecipeId = def.RecipeId,
						Id = new UUID(def.Uuid),
						Block = def.Block,
						Priority = def.Priority,
						AssumeSymmetry = def.AssumeSymmetry,
						Unlocking = BuildUnlocking(def.Unlocking)
					};

					// Ingredients are stored flat in the order Write(Recipes) walks the grid, so they
					// map straight onto Input, Width * Height entries.
					for (int i = 0; i < def.Ingredients.Count && i < recipe.Input.Length; i++)
					{
						recipe.Input[i] = BuildIngredient(def.Ingredients[i]);
					}

					foreach (ItemDef r in def.Results) recipe.Result.Add(BuildResult(r));
					return recipe;
				}
				case Furnace:
				case FurnaceData:
					return new SmeltingRecipe
					{
						Block = def.Block,
						Input = RecipeItems.Ingredient(def.InputName, def.InputMeta, 1),
						Result = BuildResult(def.Results.FirstOrDefault())
					};
				case Multi:
					return new MultiRecipe {Id = new UUID(def.Uuid)};
				case SmithingTransform:
					return new SmithingTransformRecipe
					{
						RecipeId = def.RecipeId,
						Tag = def.Tag,
						Template = BuildIngredient(def.Template),
						Base = BuildIngredient(def.Base),
						Addition = BuildIngredient(def.Addition),
						Result = BuildResult(def.Results.FirstOrDefault())
					};
				case SmithingTrim:
					return new SmithingTrimRecipe
					{
						RecipeId = def.RecipeId,
						Block = def.Block,
						Template = BuildIngredient(def.Template),
						Input = BuildIngredient(def.Input),
						Addition = BuildIngredient(def.Addition)
					};
				default:
					throw new FormatException($"Recipe '{def.RecipeId}': unknown recipe type {def.Type}");
			}
		}

		private static UnlockingRequirement BuildUnlocking(UnlockingDef def)
		{
			if (def == null) return new UnlockingRequirement();

			if (def.Context != 0) return new UnlockingRequirement {Context = def.Context};

			return new UnlockingRequirement
			{
				Context = 0,
				Ingredients = (def.Ingredients ?? new List<IngredientDef>()).Select(BuildIngredient).ToList()
			};
		}

		private static Item BuildIngredient(IngredientDef def)
		{
			if (def == null) return RecipeItems.Empty();

			switch (def.Kind)
			{
				case "item": return RecipeItems.Ingredient(def.Name, def.Meta, def.Count);
				case "molang": return RecipeItems.Molang(def.Text, def.MolangVersion, def.Count);
				case "tag": return RecipeItems.Tag(def.Text, def.Count);
				case "deferred": return RecipeItems.Deferred(def.Name, def.Meta, def.Count);
				case "alias": return RecipeItems.Alias(def.Text, def.Count);
				case "empty": return RecipeItems.Empty();
				default: throw new FormatException($"Unknown recipe ingredient kind '{def.Kind}'");
			}
		}

		// Block-state results resolve their block hash through RecipeItems, so no numeric block or
		// runtime id ever appears in the data.
		private static Item BuildResult(ItemDef def)
		{
			if (def?.Name == null || def.Count == 0 || def.Name == "minecraft:air") return RecipeItems.Empty();

			if (def.BlockState != null)
			{
				return RecipeItems.BlockResult(def.BlockState.Name, def.Count, def.BlockState.States?.Count > 0 ? def.BlockState.States : null, def.NbtB64, (short) def.Meta);
			}

			return RecipeItems.Result(def.Name, (short) def.Meta, def.Count, def.NbtB64);
		}
	}
}
