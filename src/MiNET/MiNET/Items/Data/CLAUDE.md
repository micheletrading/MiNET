# Items/Data: where these files come from

Embedded resources (`MiNET.Items.Data.*`), read at runtime through `ResourceUtil.ReadResource<T>(name, typeof(Item), "Data")`. Only one of the three files is generated.

The item classes and registry are generated C# written next to this folder, not into it: `MiNET.BlockGen` produces `Items/ItemRegistryData.generated.cs` and `Items/ItemData.generated.cs` from the CloudburstMC `Data` submodule (`MiNET.BlockGen/Data`, pinned at 619483eb = v2168).

| File | Read by | Source |
|---|---|---|
| `creative_groups.json` | [InventoryUtils.cs:75](../../InventoryUtils.cs#L75) | **GENERATED.** [CreativeGenerator.cs](../../../MiNET.BlockGen/CreativeGenerator.cs), invoked from [MiNET.BlockGen/Program.cs:113](../../../MiNET.BlockGen/Program.cs#L113), reading `creative_items.json` / `creative_contents.dat` from the CloudburstMC submodule. Rerun `dotnet run --project src/MiNET/MiNET.BlockGen` to refresh. Do not hand-edit. (The comment at [InventoryUtils.cs:35](../../InventoryUtils.cs#L35) still says "captured from vanilla BDS 1.26.34"; that is stale, it is generated now.) |
| `item_id_map.json` | [BlockFactory.cs:390](../../Blocks/BlockFactory.cs#L390), [LegacyItemUpgrader.cs:91](../LegacyItemUpgrader.cs#L91) | Committed data, not generated. Last content change 735f451b ("Initial 1.18.10 support"). Upstream origin is outside this repo's history. A duplicate copy sits unused in [Blocks/Data/](../../Blocks/Data/item_id_map.json). |
| `r16_to_current_item_map.json` | [ItemFactory.cs:125](../ItemFactory.cs#L125), [ItemFactory.cs:147](../ItemFactory.cs#L147) | pmmp/BedrockData legacy mapping (r16 = 1.16 item names to current, plus the registry's renames in its `simple` section). Committed data, not generated, not a submodule. pmmp lags Mojang releases, so it ages behind the item registry. |

## Rules

- After changing the CloudburstMC submodule pin, rerun `MiNET.BlockGen`. That refreshes `creative_groups.json` and the generated .cs files. It does **not** touch the other two files here.
- `item_id_map.json` and `r16_to_current_item_map.json` have no generator, so a protocol bump leaves them behind with no error. Sourcing them is manual.
- Recipes are NOT here and are NOT generated from CloudburstMC: see [../../Data/CLAUDE.md](../../Data/CLAUDE.md).
