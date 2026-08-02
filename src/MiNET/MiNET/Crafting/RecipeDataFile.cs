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
using Newtonsoft.Json;

namespace MiNET.Crafting
{
	// The shape of Data/recipes.json. Ours, not a vendor schema: every reference is a name, no
	// numeric wire or runtime ids, and ingredients carry their descriptor kind so the loader can
	// pick the right RecipeItems factory. Shared with the generator in MiNET.BlockGen that writes
	// the file, which is why the property names are pinned with JsonProperty rather than left to
	// casing convention.
	public class RecipeDataFile
	{
		[JsonProperty("isClean")] public bool IsClean { get; set; } = true;
		[JsonProperty("recipes")] public List<RecipeDef> Recipes { get; set; } = new List<RecipeDef>();
		[JsonProperty("potionTypeRecipes")] public List<PotionTypeDef> PotionTypeRecipes { get; set; } = new List<PotionTypeDef>();
		[JsonProperty("potionContainerRecipes")] public List<PotionContainerDef> PotionContainerRecipes { get; set; } = new List<PotionContainerDef>();
		[JsonProperty("materialReducerRecipes")] public List<MaterialReducerDef> MaterialReducerRecipes { get; set; } = new List<MaterialReducerDef>();
	}

	public class RecipeDef
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

	public class IngredientDef
	{
		[JsonProperty("kind")] public string Kind { get; set; } = "item";
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("text")] public string Text { get; set; }
		[JsonProperty("meta")] public short Meta { get; set; }
		[JsonProperty("count")] public int Count { get; set; } = 1;
		[JsonProperty("molangVersion")] public byte MolangVersion { get; set; }
	}

	public class ItemDef
	{
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("meta")] public int Meta { get; set; }
		[JsonProperty("count")] public int Count { get; set; } = 1;
		[JsonProperty("blockState")] public BlockStateDef BlockState { get; set; }
		[JsonProperty("nbtB64")] public string NbtB64 { get; set; }
	}

	public class BlockStateDef
	{
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("states")] public Dictionary<string, string> States { get; set; }
	}

	public class UnlockingDef
	{
		[JsonProperty("context")] public byte Context { get; set; } = 1;
		[JsonProperty("ingredients")] public List<IngredientDef> Ingredients { get; set; }
	}

	public class PotionTypeDef
	{
		[JsonProperty("input")] public string Input { get; set; }
		[JsonProperty("inputMeta")] public int InputMeta { get; set; }
		[JsonProperty("ingredient")] public string Ingredient { get; set; }
		[JsonProperty("ingredientMeta")] public int IngredientMeta { get; set; }
		[JsonProperty("output")] public string Output { get; set; }
		[JsonProperty("outputMeta")] public int OutputMeta { get; set; }
	}

	public class PotionContainerDef
	{
		[JsonProperty("input")] public string Input { get; set; }
		[JsonProperty("ingredient")] public string Ingredient { get; set; }
		[JsonProperty("output")] public string Output { get; set; }
	}

	public class MaterialReducerDef
	{
		[JsonProperty("input")] public string Input { get; set; }
		[JsonProperty("inputMeta")] public int InputMeta { get; set; }
		[JsonProperty("outputs")] public List<MaterialReducerOutputDef> Outputs { get; set; } = new List<MaterialReducerOutputDef>();
	}

	public class MaterialReducerOutputDef
	{
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("count")] public int Count { get; set; }
	}
}
