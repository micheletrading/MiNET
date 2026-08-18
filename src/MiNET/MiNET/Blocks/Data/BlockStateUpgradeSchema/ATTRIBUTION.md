# With thanks to Dylan T. and PocketMine-MP

Everything in this folder, and everything in `Items/Data/ItemUpgradeSchema/`, is the work of
[Dylan T. (dktapps)](https://github.com/dktapps) and the PocketMine-MP project. MiNET copies it
verbatim. We wrote none of it, and without it this server could not open a world older than the
version it was built for.

## What he solved

Bedrock does not upgrade terrain or inventories until a chunk is actually loaded in a game session.
A world that has been played since 2016 therefore still holds 2016 chunks, in the 2016 format, next
to chunks rewritten last week. Any server that is not Mojang's has to understand every blockstate
Bedrock has ever written, and there have been many backwards-incompatible changes.

The usual answer is code: a pile of hand-written per-version conversions, read out of a decompiled
game binary, in whatever language the project happens to be written in. Dylan took a different
route and made it data. The schemas here are generated from BDS itself, using
[pmmp/bds-mod-mapping](https://github.com/pmmp/bds-mod-mapping), which feeds old block palettes
into the real game server and records what it turns each state into. A generator then reduces those
mappings to the compact rules in these files, which are verified against
[pmmp/BedrockBlockPaletteArchive](https://github.com/pmmp/BedrockBlockPaletteArchive). Where Mojang
themselves got an upgrade wrong, the schema is corrected by hand and the correction is written down.

Two consequences follow, and they are why this folder exists at all. The data is at least as
accurate as Bedrock's own behaviour, because it was produced by asking Bedrock. And it belongs to no
language: PHP wrote it, C# reads it, and Java reads it too. A project like ours gets a decade of
Bedrock's format history for the price of a JSON parser.

## What it took to maintain

Thirty-four block schemas, twenty-seven item schemas, the id and meta tables, and the legacy id map,
maintained file by file as Mojang shipped version after version, including the ones where Mojang
changed the format without bumping the version. The block schema repository shows 90 of its
contributions under his name, the item one 19. The notes in its README are the kind only someone who
has been burned writes down: that `remappedStates` must win over every other rule, that its
`oldState` is a filter rather than an exact match and must be applied most-specific first, that
naming a schema after the previous schema's version instead of the palette it actually applies to
will quietly produce wrong results.

He also caught something we would have shipped wrong. Block items began carrying blockstate NBT at
1.9, before chunk blocks did at 1.12, which is why `id_meta_to_nbt/` holds two files rather than
one. For a server upgrading to current, `1.12.0.bin` covers blocks and block items alike;
`1.9.0.bin` is what you need if you are targeting 1.9, 1.10 or 1.11, where the 1.12 states would be
wrong. Both are kept here so the distinction stays visible rather than being rediscovered later.

On top of that he sent us his own regression corpus: 82 saved worlds written by Bedrock versions
from 0.16.1 through 1.18.12, the set he tests PocketMine-MP against. It contains eighteen distinct
chunk versions, sections in the classic pre-palette format, and two worlds that hold more than one
generation at once. It is the difference between believing our upgrade path works and knowing.

## Files, and where they came from

| File | Source |
|---|---|
| `NNNN_<from>_to_<to>.json` (34) | [pmmp/BedrockBlockUpgradeSchema](https://github.com/pmmp/BedrockBlockUpgradeSchema), `nbt_upgrade_schema/` |
| `id_meta_to_nbt_1.9.0.bin`, `id_meta_to_nbt_1.12.0.bin` | same repository, `id_meta_to_nbt/` |
| `block_legacy_id_map.json` | same repository, root |
| `../../../Items/Data/ItemUpgradeSchema/*.json` (27) | [pmmp/BedrockItemUpgradeSchema](https://github.com/pmmp/BedrockItemUpgradeSchema), `id_meta_upgrade_schema/` |

Both repositories are released under CC0 1.0, which asks for nothing at all: no attribution, no
notice, no conditions. This file is here because the work deserves saying out loud, not because a
licence requires it.

Keep these files byte-identical to upstream. When Mojang ships a version that needs a new schema,
take the new file from the repository above rather than editing anything here, so that what we ship
stays exactly what PocketMine ships and a bug in ours can never be a bug we introduced.
