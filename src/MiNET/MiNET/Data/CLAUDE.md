# Data: where these files come from

Embedded resources read at runtime. One is generated, one is our own dataset with no live generator, and the rest are exports of a decoded BDS wire capture.

| File | Read by | Source |
|---|---|---|
| `biome_definitions.json.gz` | [BiomeDefinitions.cs](../Worlds/BiomeDefinitions.cs) | **GENERATED.** [BiomeGenerator.cs](../../MiNET.BlockGen/BiomeGenerator.cs) from the CloudburstMC submodule's `biome_definitions.json`. Rerun `dotnet run --project src/MiNET/MiNET.BlockGen`. Verified 2026-08-06: all 88 biomes match BDS 1.26.40 on the wire exactly, field for field, when compared by name. |
| `recipes.json.gz` | [RecipeLoader.cs](../Crafting/RecipeLoader.cs) | Our own dataset in our own schema ([RecipeDataFile.cs](../Crafting/RecipeDataFile.cs)): names, no numeric wire ids, ingredients carry a descriptor kind. **Not generated from CloudburstMC.** Its generator (`MiNET.Test/GenerateRecipesTests.cs`) only ever emitted C# *from* this file and was deleted in d7d116c9; the file itself is the source of truth and its upstream origin is outside this repo's history. See the warning below. |
| `jigsaw_structures.json` | [JoinSequenceData.cs:60](../Net/JoinSequenceData.cs#L60) | Exported from a decoded BDS **1.26.34** wire capture. |
| `entity_properties.json` | [JoinSequenceData.cs:76](../Net/JoinSequenceData.cs#L76) | Exported from a decoded BDS **1.26.34** wire capture. |
| `trim_data.json` | [JoinSequenceData.cs:85](../Net/JoinSequenceData.cs#L85) | Exported from a decoded BDS **1.26.34** wire capture. |
| `voxel_shapes.json` | [JoinSequenceData.cs:96](../Net/JoinSequenceData.cs#L96) | Exported from a decoded BDS **1.26.34** wire capture. |
| `camera_aim_assist_presets.json` | [JoinSequenceData.cs:106](../Net/JoinSequenceData.cs#L106) | Exported from a decoded BDS **1.26.34** wire capture. |

The capture-derived files are not replayed as raw bytes: the `Send*` builders in [Player.cs](../Player.cs) load them into typed models and construct the packet through its own fields.

## Known divergence: recipes.json.gz

Measured against a real BDS 1.26.40 CraftingData frame, both sides carry the same 3660 recipes. Two divergences were found and FIXED in code (not data), in `MiNET/Crafting/RecipeItems.cs`:

- `Result(...)` stamped every result with the block's default palette state, because `ItemFactory.GetItemByName` returns an `ItemBlock` whose constructor sets `RuntimeId`. Only results carrying an explicit `blockState` in the data (which take `BlockResult`) should have one.
- `Alias(...)` sent a complex-alias descriptor on an air stack. Vanilla sends an ordinary item descriptor naming the alias, with wildcard metadata and block runtime id -1.

The CloudburstMC submodule ships its own `recipes.json` with the same 3660 recipes, and it is **unused**. Generating ours from it was tried and reverted: its shape data is stale (the file declares `"version": 1001`), and the result reproduced BDS's ingredient order on 121 fewer shaped recipes than the committed file. Non-square recipes are where it shows; square ones flatten identically either way. Cloudburst also lacks per-recipe UUIDs and unlocking ingredient lists that this file carries.

MiNET's codec was never the problem: BDS's real 838 KB CraftingData frame decodes and re-encodes byte-identical through MiNET. A roundtrip test over captured BDS frames cannot see this class of bug, because it only exercises values BDS itself sends.

Still divergent: 130 `minecraft:furnace_*` recipes where BDS sends a Priority (90, 110, 280, 410) and both our file and Cloudburst's send 0.

## Rules

- The 1.26.34 capture exports are stale relative to the 1.26.40 target. Refreshing one means re-exporting from a current capture, not editing by hand.
- `biome_definitions.json.gz` is the only file here a BlockGen run refreshes. Rerunning the generator will not touch the others.
- Before blaming or "fixing" a file here, check this table for whether it is generated, captured, or ours.
