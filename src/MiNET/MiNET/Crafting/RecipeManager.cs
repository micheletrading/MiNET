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
using MiNET.Net.RakNet;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json;

namespace MiNET.Crafting
{
	/// <summary>
	///     The server's recipe registry: the single source of truth for what can be crafted, smelted,
	///     brewed and reduced. Recipes are domain data - the server's own crafting logic resolves them
	///     (see ItemStackInventoryManager) and plugins extend them through <see cref="Add" /> - and the
	///     CraftingData packet is only a projection of this registry onto the wire
	///     (Player.SendCraftingRecipes).
	///     <para>
	///         The vanilla 1.26.34 recipe set is loaded from the embedded Data/recipes.json, which
	///         references every item by registry string id, never by a numeric wire id: string ids are
	///         durable identity, the numbers are per-version registry assignments resolved here. Recipe
	///         network ids are not in the file either - they are the handles the client sends back in a
	///         CraftRecipe action, so the registry assigns them in load order starting at 1.
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

				try
				{
					Load(ResourceUtil.ReadResource<RecipeDataFile>("recipes.json", typeof(Player), "Data"));
				}
				catch (Exception e)
				{
					Log.Error("Could not load the recipe registry from Data/recipes.json", e);
				}
			}
		}

		private static void Load(RecipeDataFile file)
		{
			int skipped = 0;
			foreach (RecipeDef def in file.Recipes)
			{
				Recipe recipe = BuildRecipe(def, ref skipped);
				if (recipe == null) continue;

				AddLocked(recipe);
			}

			_potionTypeRecipes = file.PotionTypeRecipes
				.Select(p => new PotionTypeRecipe
				{
					Input = ItemFactory.GetNetworkIdByName(p.Input),
					InputMeta = p.InputMeta,
					Ingredient = ItemFactory.GetNetworkIdByName(p.Ingredient),
					IngredientMeta = p.IngredientMeta,
					Output = ItemFactory.GetNetworkIdByName(p.Output),
					OutputMeta = p.OutputMeta
				})
				.ToArray();

			_potionContainerRecipes = file.PotionContainerRecipes
				.Select(p => new PotionContainerChangeRecipe
				{
					Input = ItemFactory.GetNetworkIdByName(p.Input),
					Ingredient = ItemFactory.GetNetworkIdByName(p.Ingredient),
					Output = ItemFactory.GetNetworkIdByName(p.Output)
				})
				.ToArray();

			_materialReducerRecipes = file.MaterialReducerRecipes
				.Select(m => new MaterialReducerRecipe(
					ItemFactory.GetNetworkIdByName(m.Input),
					m.InputMeta,
					m.Outputs.Select(o => new MaterialReducerRecipe.MaterialReducerRecipeOutput(ItemFactory.GetNetworkIdByName(o.Name), o.Count)).ToArray()))
				.ToArray();

			Log.Info($"Recipe registry loaded: {_recipes.Count} recipes, {_potionTypeRecipes.Length} potion mixes, {_potionContainerRecipes.Length} container mixes, {_materialReducerRecipes.Length} material reducers"
				+ (skipped > 0 ? $", {skipped} skipped" : ""));
		}

		private static Recipe BuildRecipe(RecipeDef def, ref int skipped)
		{
			try
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
						foreach (IngredientDef ingredient in def.Ingredients) recipe.Input.Add(BuildIngredient(ingredient));
						foreach (ItemDef result in def.Results) recipe.Result.Add(BuildItem(result));
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
						// Ingredients are stored in the order the reader produced them, which is the order
						// Write(Recipes) walks the grid, so the flat list maps straight onto Input.
						for (int i = 0; i < recipe.Input.Length && i < def.Ingredients.Count; i++)
						{
							recipe.Input[i] = BuildIngredient(def.Ingredients[i]);
						}
						foreach (ItemDef result in def.Results) recipe.Result.Add(BuildItem(result));
						return recipe;
					}
					case Furnace:
					case FurnaceData:
						return new SmeltingRecipe
						{
							Block = def.Block,
							Input = BuildIngredient(new IngredientDef {Kind = "item", Name = def.InputName, Meta = def.InputMeta, Count = 1}),
							Result = BuildItem(def.Results.FirstOrDefault())
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
							Result = BuildItem(def.Results.FirstOrDefault())
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
						Log.Warn($"Skipping recipe '{def.RecipeId}': unknown recipe type {def.Type}");
						skipped++;
						return null;
				}
			}
			catch (Exception e)
			{
				Log.Error($"Skipping malformed recipe '{def.RecipeId}' (type {def.Type})", e);
				skipped++;
				return null;
			}
		}

		private static UnlockingRequirement BuildUnlocking(UnlockingDef def)
		{
			if (def == null) return new UnlockingRequirement();

			var requirement = new UnlockingRequirement {Context = def.Context};
			if (def.Context == 0)
			{
				requirement.Ingredients = (def.Ingredients ?? new List<IngredientDef>()).Select(BuildIngredient).ToList();
			}

			return requirement;
		}

		// An ingredient is an Item so the crafting logic can match it against real inventory stacks, plus
		// a descriptor saying which wire variant it is. The "item" kind resolves the name through
		// ItemFactory so the ingredient is a real typed item; the descriptor keeps the name so the wire id
		// is resolved from the item registry rather than from Item.Id (a different id plane).
		private static Item BuildIngredient(IngredientDef def)
		{
			if (def == null) return new ItemAir {Count = 0};

			switch (def.Kind)
			{
				case "item":
				{
					Item item = ItemFactory.GetItem(def.Name, def.Meta, def.Count);
					item.IngredientDescriptor = new RecipeIngredientDescriptor {Type = 1, Name = def.Name, Metadata = def.Meta};
					return item;
				}
				case "molang":
					return new ItemAir
					{
						Count = (byte) def.Count,
						IngredientDescriptor = new RecipeIngredientDescriptor {Type = 2, Text = def.Text, MolangVersion = def.MolangVersion}
					};
				case "tag":
					return new ItemAir
					{
						Count = (byte) def.Count,
						IngredientDescriptor = new RecipeIngredientDescriptor {Type = 3, Text = def.Text}
					};
				case "deferred":
					return new ItemAir
					{
						Count = (byte) def.Count,
						IngredientDescriptor = new RecipeIngredientDescriptor {Type = 4, Text = def.Name, Metadata = def.Meta}
					};
				case "alias":
					return new ItemAir
					{
						Count = (byte) def.Count,
						IngredientDescriptor = new RecipeIngredientDescriptor {Type = 5, Text = def.Text}
					};
				case "empty":
					return new ItemAir {Count = (byte) def.Count};
				default:
					throw new FormatException($"Unknown recipe ingredient kind '{def.Kind}'");
			}
		}

		// A result stack. The typed item comes from the registry name so server logic gets a real Item;
		// NetworkId/NetworkMetadata pin the exact wire identity of that name (see WriteItemLegacy), and the
		// block-state reference becomes the block's network hash through the palette instead of carrying a
		// runtime number in the data file.
		private static Item BuildItem(ItemDef def)
		{
			if (def?.Name == null || def.Count == 0 || def.Name == "minecraft:air") return new ItemAir {Count = 0};

			short networkId = ItemFactory.GetNetworkIdByName(def.Name);
			if (networkId == 0) throw new FormatException($"Recipe result '{def.Name}' is not in the item registry");

			Item item = ItemFactory.GetItem(def.Name, (short) def.Meta, def.Count);
			item.NetworkId = networkId;
			item.NetworkMetadata = def.Meta;
			item.Count = (byte) def.Count;
			item.RuntimeId = ResolveBlockStateHash(def.BlockState);
			if (def.NbtB64 != null) item.ExtraData = (NbtCompound) JoinSequenceData.NbtFromBase64(def.NbtB64).NbtFile.RootTag;

			return item;
		}

		// Block-state reference -> the block's FNV-1a network hash, which is what an item stack's "block
		// runtime id" field carries while StartGame.blockNetworkIdsAreHashes is set. A name with no
		// explicit states means the block's default (first palette) state.
		private static int ResolveBlockStateHash(BlockStateDef def)
		{
			if (def?.Name == null) return 0;

			if (def.States == null || def.States.Count == 0)
			{
				uint defaultHash = BlockFactory.GetDefaultStateHash(def.Name);
				if (defaultHash == 0) Log.Warn($"Recipe result references unknown block '{def.Name}'");
				return unchecked((int) defaultHash);
			}

			foreach (BlockStateContainer state in BlockFactory.BlockPalette)
			{
				if (!string.Equals(state.Name, def.Name, StringComparison.OrdinalIgnoreCase)) continue;
				if (state.States.Count != def.States.Count) continue;
				if (!state.States.All(s => def.States.TryGetValue(s.Name, out string value) && string.Equals(StateValue(s), value, StringComparison.OrdinalIgnoreCase))) continue;

				return unchecked((int) BlockFactory.GetNetworkHash(state.RuntimeId));
			}

			Log.Warn($"Recipe result references block state '{def.Name}' that is not in the palette; falling back to its default state");
			return unchecked((int) BlockFactory.GetDefaultStateHash(def.Name));
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

		// Wire recipe-type discriminators, see the "Recipe Types" enum in MCPE Protocol.xml.
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

		/// <summary>Shape of the embedded Data/recipes.json. See <see cref="RecipeManager" /> for what is, and is not, in it.</summary>
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

		/// <summary>
		///     One ingredient slot. "kind" is the wire descriptor variant, and it is semantics rather than
		///     an encoding detail: a tag ingredient ("minecraft:planks") matches any item carrying the tag,
		///     so it stays a tag and is never flattened to a single item.
		/// </summary>
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

			/// <summary>Explicit state values, present only when the state is not the block's palette default.</summary>
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
}