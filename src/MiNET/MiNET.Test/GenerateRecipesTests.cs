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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MiNET.Test
{
	/// <summary>
	///     Generates MiNET/Crafting/RecipeData.generated.cs from MiNET/Data/recipes.json. Run manually
	///     (un-Ignore, run GenerateRecipes, re-Ignore) after editing recipes.json, e.g. when updating the
	///     protocol version's vanilla recipe set. See RecipeManager for how the generated code is consumed.
	/// </summary>
	[TestClass
	, Ignore("Manual code generation")
	]
	public class GenerateRecipesTests
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(GenerateRecipesTests));

		// Keeps each CreateVanillaRecipes_PartN method well under the 64KB IL method-size limit.
		private const int RecipesPerPart = 30;

		[TestMethod]
		public void GenerateRecipes()
		{
			string repoRoot = FindRepoRoot();
			string jsonPath = Path.Combine(repoRoot, "src", "MiNET", "MiNET", "Data", "recipes.json");
			string outputPath = Path.Combine(repoRoot, "src", "MiNET", "MiNET", "Crafting", "RecipeData.generated.cs");

			Log.Info($"Reading {jsonPath}");
			RecipeDataFile file = JsonConvert.DeserializeObject<RecipeDataFile>(File.ReadAllText(jsonPath));

			using (FileStream stream = File.Create(outputPath))
			using (var writer = new IndentedTextWriter(new StreamWriter(stream, new UTF8Encoding(true)), "\t"))
			{
				WriteHeader(writer);

				writer.WriteLine("namespace MiNET.Crafting");
				writer.WriteLine("{");
				writer.Indent++;

				writer.WriteLine("public static partial class RecipeData");
				writer.WriteLine("{");
				writer.Indent++;

				List<string> partMethodNames = WriteVanillaRecipeParts(writer, file.Recipes);
				WriteCreateVanillaRecipes(writer, partMethodNames);

				writer.WriteLineNoTabs("");
				WritePotionTypeRecipes(writer, file.PotionTypeRecipes);

				writer.WriteLineNoTabs("");
				WritePotionContainerRecipes(writer, file.PotionContainerRecipes);

				writer.WriteLineNoTabs("");
				WriteMaterialReducerRecipes(writer, file.MaterialReducerRecipes);

				writer.Indent--;
				writer.WriteLine("}");

				writer.Indent--;
				writer.WriteLine("}");

				writer.Flush();
			}

			Log.Info($"Generated {file.Recipes.Count} recipes, {file.PotionTypeRecipes.Count} potion mixes, {file.PotionContainerRecipes.Count} container mixes, {file.MaterialReducerRecipes.Count} material reducers to\n{outputPath}");
		}

		private static string FindRepoRoot()
		{
			DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "src", "MiNET", "MiNET.sln")))
			{
				dir = dir.Parent;
			}

			if (dir == null) throw new DirectoryNotFoundException("Could not locate MiNET.sln above " + AppDomain.CurrentDomain.BaseDirectory);

			return dir.FullName;
		}

		private static void WriteHeader(IndentedTextWriter writer)
		{
			writer.WriteLine("#region LICENSE");
			writer.WriteLineNoTabs("");
			writer.WriteLine("// The contents of this file are subject to the Common Public Attribution");
			writer.WriteLine("// License Version 1.0. (the \"License\"); you may not use this file except in");
			writer.WriteLine("// compliance with the License. You may obtain a copy of the License at");
			writer.WriteLine("// https://github.com/NiclasOlofsson/MiNET/blob/master/LICENSE.");
			writer.WriteLine("// The License is based on the Mozilla Public License Version 1.1, but Sections 14");
			writer.WriteLine("// and 15 have been added to cover use of software over a computer network and");
			writer.WriteLine("// provide for limited attribution for the Original Developer. In addition, Exhibit A has");
			writer.WriteLine("// been modified to be consistent with Exhibit B.");
			writer.WriteLine("//");
			writer.WriteLine("// Software distributed under the License is distributed on an \"AS IS\" basis,");
			writer.WriteLine("// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for");
			writer.WriteLine("// the specific language governing rights and limitations under the License.");
			writer.WriteLine("//");
			writer.WriteLine("// The Original Code is MiNET.");
			writer.WriteLine("//");
			writer.WriteLine("// The Original Developer is the Initial Developer.  The Initial Developer of");
			writer.WriteLine("// the Original Code is Niclas Olofsson.");
			writer.WriteLine("//");
			writer.WriteLine("// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2026 Niclas Olofsson.");
			writer.WriteLine("// All Rights Reserved.");
			writer.WriteLineNoTabs("");
			writer.WriteLine("#endregion");
			writer.WriteLineNoTabs("");
			writer.WriteLine("// GENERATED CODE. DON'T EDIT BY HAND.");
			writer.WriteLine("// Regenerate with GenerateRecipesTests.GenerateRecipes after updating Data/recipes.json.");
			writer.WriteLineNoTabs("");
			writer.WriteLine("using System.Collections.Generic;");
			writer.WriteLine("using MiNET.Items;");
			writer.WriteLine("using MiNET.Utils;");
			writer.WriteLineNoTabs("");
		}

		private static List<string> WriteVanillaRecipeParts(IndentedTextWriter writer, List<RecipeDef> recipeDefs)
		{
			var partMethodNames = new List<string>();

			int partIndex = 0;
			for (int i = 0; i < recipeDefs.Count; i += RecipesPerPart)
			{
				partIndex++;
				string methodName = $"CreateVanillaRecipes_Part{partIndex}";
				partMethodNames.Add(methodName);

				writer.WriteLine($"private static void {methodName}(Recipes recipes)");
				writer.WriteLine("{");
				writer.Indent++;

				foreach (RecipeDef def in recipeDefs.Skip(i).Take(RecipesPerPart))
				{
					writer.WriteLine($"recipes.Add({EmitRecipe(def)});");
				}

				writer.Indent--;
				writer.WriteLine("}");
				writer.WriteLineNoTabs("");
			}

			return partMethodNames;
		}

		private static void WriteCreateVanillaRecipes(IndentedTextWriter writer, List<string> partMethodNames)
		{
			writer.WriteLine("/// <summary>The vanilla recipe set, in recipes.json order (registry network ids depend on load order).</summary>");
			writer.WriteLine("public static Recipes CreateVanillaRecipes()");
			writer.WriteLine("{");
			writer.Indent++;
			writer.WriteLine("var recipes = new Recipes();");
			foreach (string methodName in partMethodNames)
			{
				writer.WriteLine($"{methodName}(recipes);");
			}
			writer.WriteLine("return recipes;");
			writer.Indent--;
			writer.WriteLine("}");
		}

		private static void WritePotionTypeRecipes(IndentedTextWriter writer, List<PotionTypeDef> defs)
		{
			writer.WriteLine("public static PotionTypeRecipe[] CreatePotionTypeRecipes()");
			writer.WriteLine("{");
			writer.Indent++;
			writer.WriteLine("return new PotionTypeRecipe[]");
			writer.WriteLine("{");
			writer.Indent++;
			foreach (PotionTypeDef p in defs)
			{
				writer.WriteLine($"new PotionTypeRecipe {{Input = ItemFactory.GetNetworkIdByName({Str(p.Input)}), InputMeta = {p.InputMeta}, Ingredient = ItemFactory.GetNetworkIdByName({Str(p.Ingredient)}), IngredientMeta = {p.IngredientMeta}, Output = ItemFactory.GetNetworkIdByName({Str(p.Output)}), OutputMeta = {p.OutputMeta}}},");
			}
			writer.Indent--;
			writer.WriteLine("};");
			writer.Indent--;
			writer.WriteLine("}");
		}

		private static void WritePotionContainerRecipes(IndentedTextWriter writer, List<PotionContainerDef> defs)
		{
			writer.WriteLine("public static PotionContainerChangeRecipe[] CreatePotionContainerRecipes()");
			writer.WriteLine("{");
			writer.Indent++;
			writer.WriteLine("return new PotionContainerChangeRecipe[]");
			writer.WriteLine("{");
			writer.Indent++;
			foreach (PotionContainerDef p in defs)
			{
				writer.WriteLine($"new PotionContainerChangeRecipe {{Input = ItemFactory.GetNetworkIdByName({Str(p.Input)}), Ingredient = ItemFactory.GetNetworkIdByName({Str(p.Ingredient)}), Output = ItemFactory.GetNetworkIdByName({Str(p.Output)})}},");
			}
			writer.Indent--;
			writer.WriteLine("};");
			writer.Indent--;
			writer.WriteLine("}");
		}

		private static void WriteMaterialReducerRecipes(IndentedTextWriter writer, List<MaterialReducerDef> defs)
		{
			writer.WriteLine("public static MaterialReducerRecipe[] CreateMaterialReducerRecipes()");
			writer.WriteLine("{");
			writer.Indent++;
			writer.WriteLine("return new MaterialReducerRecipe[]");
			writer.WriteLine("{");
			writer.Indent++;
			foreach (MaterialReducerDef m in defs)
			{
				string outputs = string.Join(", ", m.Outputs.Select(o => $"new MaterialReducerRecipe.MaterialReducerRecipeOutput(ItemFactory.GetNetworkIdByName({Str(o.Name)}), {o.Count})"));
				writer.WriteLine($"new MaterialReducerRecipe(ItemFactory.GetNetworkIdByName({Str(m.Input)}), {m.InputMeta}, {outputs}),");
			}
			writer.Indent--;
			writer.WriteLine("};");
			writer.Indent--;
			writer.WriteLine("}");
		}

		// --- Per-recipe emission -------------------------------------------------------------------

		private static string EmitRecipe(RecipeDef def)
		{
			switch (def.Type)
			{
				case Shapeless:
				case ShulkerBox:
				case ShapelessChemistry:
				{
					var sb = new StringBuilder();
					sb.Append("new ShapelessRecipe {");
					sb.Append($"RecipeType = {def.Type}, ");
					sb.Append($"RecipeId = {Str(def.RecipeId)}, ");
					sb.Append($"Id = {EmitUuid(def.Uuid)}, ");
					sb.Append($"Block = {Str(def.Block)}, ");
					sb.Append($"Priority = {def.Priority}, ");
					sb.Append($"Unlocking = {EmitUnlocking(def.Unlocking)}, ");
					sb.Append($"Input = {{{string.Join(", ", def.Ingredients.Select(EmitIngredient))}}}, ");
					sb.Append($"Result = {{{string.Join(", ", def.Results.Select(EmitResult))}}}");
					sb.Append("}");
					return sb.ToString();
				}
				case Shaped:
				case ShapedChemistry:
				{
					var sb = new StringBuilder();
					sb.Append($"new ShapedRecipe({def.Width}, {def.Height}) {{");
					sb.Append($"RecipeType = {def.Type}, ");
					sb.Append($"RecipeId = {Str(def.RecipeId)}, ");
					sb.Append($"Id = {EmitUuid(def.Uuid)}, ");
					sb.Append($"Block = {Str(def.Block)}, ");
					sb.Append($"Priority = {def.Priority}, ");
					sb.Append($"AssumeSymmetry = {Bool(def.AssumeSymmetry)}, ");
					sb.Append($"Unlocking = {EmitUnlocking(def.Unlocking)}, ");
					// Ingredients are stored flat in the order Write(Recipes) walks the grid, so they map
					// straight onto the Input array (Width * Height entries, verified 1:1 at generation time).
					sb.Append($"Input = new[] {{{string.Join(", ", def.Ingredients.Select(EmitIngredient))}}}, ");
					sb.Append($"Result = {{{string.Join(", ", def.Results.Select(EmitResult))}}}");
					sb.Append("}");
					return sb.ToString();
				}
				case Furnace:
				case FurnaceData:
				{
					return $"new SmeltingRecipe {{Block = {Str(def.Block)}, Input = RecipeItems.Ingredient({Str(def.InputName)}, {def.InputMeta}, 1), Result = {EmitResult(def.Results.FirstOrDefault())}}}";
				}
				case Multi:
				{
					return $"new MultiRecipe {{Id = {EmitUuid(def.Uuid)}}}";
				}
				case SmithingTransform:
				{
					return $"new SmithingTransformRecipe {{RecipeId = {Str(def.RecipeId)}, Tag = {Str(def.Tag)}, Template = {EmitIngredient(def.Template)}, Base = {EmitIngredient(def.Base)}, Addition = {EmitIngredient(def.Addition)}, Result = {EmitResult(def.Results.FirstOrDefault())}}}";
				}
				case SmithingTrim:
				{
					return $"new SmithingTrimRecipe {{RecipeId = {Str(def.RecipeId)}, Block = {Str(def.Block)}, Template = {EmitIngredient(def.Template)}, Input = {EmitIngredient(def.Input)}, Addition = {EmitIngredient(def.Addition)}}}";
				}
				default:
					throw new FormatException($"Recipe '{def.RecipeId}': unknown recipe type {def.Type}");
			}
		}

		private static string EmitUnlocking(UnlockingDef def)
		{
			if (def == null) return "new UnlockingRequirement()";

			if (def.Context == 0)
			{
				List<IngredientDef> ingredients = def.Ingredients ?? new List<IngredientDef>();
				return $"new UnlockingRequirement {{Context = 0, Ingredients = new List<Item> {{{string.Join(", ", ingredients.Select(EmitIngredient))}}}}}";
			}

			return $"new UnlockingRequirement {{Context = {def.Context}}}";
		}

		// An ingredient is an Item so the crafting logic can match it against real inventory stacks; the
		// mapping from JSON "kind" to the RecipeItems factory that produces it lives here, once, so both
		// vanilla and hand-written/plugin recipes go through the same RecipeItems API.
		private static string EmitIngredient(IngredientDef def)
		{
			if (def == null) return "RecipeItems.Empty()";

			switch (def.Kind)
			{
				case "item":
					return $"RecipeItems.Ingredient({Str(def.Name)}, {def.Meta}, {def.Count})";
				case "molang":
					return $"RecipeItems.Molang({Str(def.Text)}, {def.MolangVersion}, {def.Count})";
				case "tag":
					return $"RecipeItems.Tag({Str(def.Text)}, {def.Count})";
				case "deferred":
					return $"RecipeItems.Deferred({Str(def.Name)}, {def.Meta}, {def.Count})";
				case "alias":
					return $"RecipeItems.Alias({Str(def.Text)}, {def.Count})";
				case "empty":
					return "RecipeItems.Empty()";
				default:
					throw new FormatException($"Unknown recipe ingredient kind '{def.Kind}'");
			}
		}

		// A result stack; block-state results resolve their block hash at runtime through RecipeItems, so
		// the generated code never carries a numeric block/runtime id, only names.
		private static string EmitResult(ItemDef def)
		{
			if (def?.Name == null || def.Count == 0 || def.Name == "minecraft:air") return "RecipeItems.Empty()";

			if (def.BlockState != null)
			{
				string meta = def.Meta != 0 ? $", meta: {def.Meta}" : "";
				return $"RecipeItems.BlockResult({Str(def.BlockState.Name)}, {def.Count}, {EmitStates(def.BlockState.States)}, {Str(def.NbtB64)}{meta})";
			}

			return $"RecipeItems.Result({Str(def.Name)}, {def.Meta}, {def.Count}, {Str(def.NbtB64)})";
		}

		private static string EmitStates(Dictionary<string, string> states)
		{
			if (states == null || states.Count == 0) return "null";

			return $"new Dictionary<string, string> {{{string.Join(", ", states.Select(kv => $"[{Str(kv.Key)}] = {Str(kv.Value)}"))}}}";
		}

		private static string EmitUuid(string uuid)
		{
			return $"new UUID({Str(uuid)})";
		}

		private static string Str(string value)
		{
			if (value == null) return "null";

			return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}

		private static string Bool(bool value)
		{
			return value ? "true" : "false";
		}

		// Wire recipe-type discriminators, see RecipeManager / the "Recipe Types" enum in MCPE Protocol.xml.
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

		// --- Shape of Data/recipes.json (generator input only; the runtime never parses this) ---------

		private class RecipeDataFile
		{
			[JsonProperty("isClean")] public bool IsClean { get; set; } = true;
			[JsonProperty("recipes")] public List<RecipeDef> Recipes { get; set; } = new List<RecipeDef>();
			[JsonProperty("potionTypeRecipes")] public List<PotionTypeDef> PotionTypeRecipes { get; set; } = new List<PotionTypeDef>();
			[JsonProperty("potionContainerRecipes")] public List<PotionContainerDef> PotionContainerRecipes { get; set; } = new List<PotionContainerDef>();
			[JsonProperty("materialReducerRecipes")] public List<MaterialReducerDef> MaterialReducerRecipes { get; set; } = new List<MaterialReducerDef>();
		}

		private class RecipeDef
		{
			[JsonProperty("type")] public int Type { get; set; }
			[JsonProperty("recipeId")] public string RecipeId { get; set; }
			[JsonProperty("uuid")] public string Uuid { get; set; } = "00000000-0000-0000-0000-000000000000";
			[JsonProperty("block")] public string Block { get; set; }
			[JsonProperty("priority")] public int Priority { get; set; }
			[JsonProperty("width")] public int Width { get; set; }
			[JsonProperty("height")] public int Height { get; set; }
			[JsonProperty("assumeSymmetry")] public bool AssumeSymmetry { get; set; }
			[JsonProperty("ingredients")] public List<IngredientDef> Ingredients { get; set; } = new List<IngredientDef>();
			[JsonProperty("results")] public List<ItemDef> Results { get; set; } = new List<ItemDef>();
			[JsonProperty("unlocking")] public UnlockingDef Unlocking { get; set; }

			// Furnace/smelting (types 2 and 3)
			[JsonProperty("inputName")] public string InputName { get; set; }
			[JsonProperty("inputMeta")] public short InputMeta { get; set; }

			// Smithing table (types 8 and 9)
			[JsonProperty("tag")] public string Tag { get; set; }
			[JsonProperty("template")] public IngredientDef Template { get; set; }
			[JsonProperty("base")] public IngredientDef Base { get; set; }
			[JsonProperty("addition")] public IngredientDef Addition { get; set; }
			[JsonProperty("input")] public IngredientDef Input { get; set; }
		}

		private class IngredientDef
		{
			[JsonProperty("kind")] public string Kind { get; set; } = "item";
			[JsonProperty("name")] public string Name { get; set; }
			[JsonProperty("text")] public string Text { get; set; }
			[JsonProperty("meta")] public short Meta { get; set; }
			[JsonProperty("count")] public int Count { get; set; } = 1;
			[JsonProperty("molangVersion")] public byte MolangVersion { get; set; }
		}

		private class ItemDef
		{
			[JsonProperty("name")] public string Name { get; set; }
			[JsonProperty("meta")] public int Meta { get; set; }
			[JsonProperty("count")] public int Count { get; set; } = 1;
			[JsonProperty("blockState")] public BlockStateDef BlockState { get; set; }
			[JsonProperty("nbtB64")] public string NbtB64 { get; set; }
		}

		private class BlockStateDef
		{
			[JsonProperty("name")] public string Name { get; set; }
			[JsonProperty("states")] public Dictionary<string, string> States { get; set; }
		}

		private class UnlockingDef
		{
			[JsonProperty("context")] public byte Context { get; set; } = 1;
			[JsonProperty("ingredients")] public List<IngredientDef> Ingredients { get; set; }
		}

		private class PotionTypeDef
		{
			[JsonProperty("input")] public string Input { get; set; }
			[JsonProperty("inputMeta")] public int InputMeta { get; set; }
			[JsonProperty("ingredient")] public string Ingredient { get; set; }
			[JsonProperty("ingredientMeta")] public int IngredientMeta { get; set; }
			[JsonProperty("output")] public string Output { get; set; }
			[JsonProperty("outputMeta")] public int OutputMeta { get; set; }
		}

		private class PotionContainerDef
		{
			[JsonProperty("input")] public string Input { get; set; }
			[JsonProperty("ingredient")] public string Ingredient { get; set; }
			[JsonProperty("output")] public string Output { get; set; }
		}

		private class MaterialReducerDef
		{
			[JsonProperty("input")] public string Input { get; set; }
			[JsonProperty("inputMeta")] public int InputMeta { get; set; }
			[JsonProperty("outputs")] public List<MaterialReducerOutputDef> Outputs { get; set; } = new List<MaterialReducerOutputDef>();
		}

		private class MaterialReducerOutputDef
		{
			[JsonProperty("name")] public string Name { get; set; }
			[JsonProperty("count")] public int Count { get; set; }
		}
	}
}
