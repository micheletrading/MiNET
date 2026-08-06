# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

MiNET is a Minecraft: Bedrock Edition server written in C#, targeting .NET 10. The solution lives at `src/MiNET/MiNET.sln`; everything is under `src/MiNET/`. The current protocol target lives in `MCPE Protocol.xml` (currently protocol 1001, game 1.26.34).

Naming note: the edition is Minecraft: Bedrock Edition ("Bedrock"). The `Mcpe`/`MCPE` prefix all over the code and filenames (`McpeProtocolInfo`, `MCPE Protocol.xml`, the `Mcpe*` packet classes) is legacy from the Pocket Edition era and stays, it's baked into the codebase, but "Bedrock" is the edition in prose.

## An unhandled throw in a hot path stops the line

An NRE, or any unhandled exception on a send/receive/encode/decode/tick path, is a hard defect. Fix it the moment it is observed, before any other investigation continues. Never log-and-defer, never file it as a "known follow-up", never reason around it. In this codebase a throw inside the outgoing path is especially lethal: `Compression.CompressPacketsForWrapper` throwing on one bad packet silently kills the entire wrapper batch it rides in, so the client just stops receiving with no error, and it looks like a protocol/sequence bug when it is actually a crash. A swallowed batch or a dropped packet is not an acceptable state to build on. Fixed, or the work halts until it is. (Learned twice on the 2168 port: the `SetEntityData`/`AddEntity` NRE on null `PropertySyncData` was seen, noted, and walked past while join-sequence bisecting burned real client joins. Do not repeat that.)

## Never dismiss a BDS/MiNET difference on judgment

Every difference between what vanilla BDS puts on the wire and what MiNET puts on the wire is REPORTED, in full, every time. Do not filter, do not rank, do not call one "cosmetic", "benign", "expected", or "tolerated", and never silently drop one from a diff summary. We do not yet know which differences matter, and the evidence says the intuition is bad: the two confirmed client-killers so far were `xboxLiveBroadcastSetting=6` (an out-of-range enum that looked harmless) and a skin animation count written as le32 instead of varint (three bytes). Both would have been filed as unimportant by judgment.

The division of labour: the diff enumerates EVERY divergence with its field, offset, and both values; classifying one as acceptable is Niclas's call, not Claude's. Stating a hypothesis about a difference is fine ("this is probably world state") as long as the difference is still listed and still counted. Building a catalogue of which divergences are genuinely benign is a goal of this work, so each ruling gets recorded here or in the effort memory with the reason and the evidence. Until a difference has been explicitly ruled benign, it is an open defect.

## Commands

```bash
# Build the whole solution
dotnet build src/MiNET/MiNET.sln

# Run all tests (MSTest)
dotnet test src/MiNET/MiNET.Test/MiNETTests.csproj

# Run a single test
dotnet test src/MiNET/MiNET.Test/MiNETTests.csproj --filter "FullyQualifiedName~PacketTests"

# Run the server (reads server.conf from the working directory)
dotnet run --project src/MiNET/MiNET.Console
```

CI (`.github/workflows/dotnetcore.yml`) builds and packs only the core `MiNET` project and pushes it to NuGet on every push to master.

### Restarting the server (the ONLY procedure)

After every code change, this exact loop, nothing else:

1. Kill the old process: `powershell -Command "Get-Process MiNET.Console -ErrorAction SilentlyContinue | Stop-Process -Force"`. A running server locks MiNET.dll and the build silently fails or runs stale code.
2. Start with `dotnet run --project src/MiNET/MiNET.Console > temp_auto/minet-server.log 2>&1 &` from the repo root. `dotnet run` builds the project and its references itself; do NOT run a separate `dotnet build` first, and NEVER start the exe from `bin/` directly (that is how stale-binary runs happen).
3. Wait for readiness by polling the log for `Server open for business`, with a background until-loop, never fixed `sleep N` guesses. Startup takes ~20-40s (build + 2x ~1000-chunk pre-cache).

Logs, two different files:
- `temp_auto/minet-server.log` - stdout capture (console appender, TRACE and up).
- `src/MiNET/MiNET.Console/bin/Debug/net10.0/minetlog.log` - the rolling file appender; the only place VERBOSE lines (datagram/ACK traces) appear. Config in `src/MiNET/MiNET.Console/log4net.xml`; the active server config is `server.nicke.conf` next to the exe (this machine), not `server.conf`.

## Projects

- `MiNET` - the core server library and the NuGet package. Everything below refers to this project.
- `MiNET.Console` - console host that boots `MiNetServer`. Configured via `server.conf` (key=value, read through `MiNET.Utils.Config`).
- `MiNET.Client` - a Bedrock client/bot used to trace and reverse-engineer the protocol against a real server (e.g. vanilla BDS). `BedrockTraceHandler` dumps packets; this is the main tool when updating protocol versions. `MINET_TARGET=host:port` aims it somewhere other than the local BDS, `MINET_PACKET_DUMP=<dir>` writes every received frame as `<seq>-<name>.bin`, `MINET_BLOB_CACHE=1` makes it announce the client-side cache the way a real client does.
- `MiNET.Tunnel` - MITM proxy for capturing a real client's session against a real server: client -> tunnel -> BDS. Each leg handles only its own login and crypto and forwards every other frame verbatim, dumping both directions into one interleaved sequence (`MINET_TUNNEL_TARGET`, `MINET_TUNNEL_DUMP`). Point it at MiNET instead of BDS to capture our own output in the identical format for diffing.
- `MiNET.ServiceKiller` - load-test emulator that spawns many fake clients.
- `TestPlugin`, `MiNET.Plotter`, `MiNET.BuilderBase` - example/real plugins loaded at runtime (see plugin system below).
- `MiNETTests`, `MiNET.BuilderBase.Tests` - MSTest projects.

## Protocol layer (the important part)

Most work in this repo is keeping up with Mojang protocol changes. The network code in `MiNET/Net/` is largely generated:

- `MCPE Protocol.xml` is the source of truth: packet definitions, ids, fields, and the protocol/game version.
- `MCPE Protocol.tt` (T4 template) generates `MCPE Protocol.cs` from the XML; `MCPE Protocol Documentation.tt` generates the markdown protocol spec. Both outputs are committed. Regenerate after editing the XML, either via the Visual Studio design-time custom tool or the `dotnet-t4` CLI: `cd src/MiNET/MiNET/Net && t4 "MCPE Protocol.tt" -o "MCPE Protocol.cs"` (same for the documentation template).
- Never hand-edit `MCPE Protocol.cs`. To customize a packet beyond what the XML expresses, add a partial class in its own file (e.g. `Net/McpeAnimate.cs`): declare extra fields there and implement the `partial void AfterDecode()` / `AfterEncode()` hooks, which run after the generated fields. Fields that need conditional or custom encoding are left out of the XML entirely and handled in the partial.
- Machine-readable protocol references, in priority order for a specific target version:
  - PrismarineJS/minecraft-data (`data/bedrock/<version>/protocol.json`, ProtoDef) is version-exact, so it is the primary reference for whatever version the XML targets. It is community-maintained and does have occasional field errors.
  - Mojang's official docs repo (github.com/Mojang/bedrock-protocol-docs, JSON Schemas) is authoritative for field semantics and wire order (via `x-ordinal-index`), but it only publishes the single current protocol, which is usually AHEAD of the version we target. Use it to cross-check, and when the two disagree trust Mojang and note it.
  - Live BDS bytes are the ground-truth tiebreaker. Zero leftover bytes proves byte alignment, not field meaning.
- `McpeProtocolInfo.ProtocolVersion` / `GameVersion` (generated from XML attributes) gate client connections.

### Updating the protocol (the working order)

A protocol update is reverse engineering against a real vanilla BDS. Work it in this order; the early steps are not preamble, they are what makes the later evidence trustworthy.

**1. Get the reference server right.** Download the BDS build matching the target protocol and run it with the configuration we are comparing against. Note its `server.properties`: `view-distance` and `tick-distance` both show up on the wire (tick-distance is the join-burst publisher radius), and `online-mode=false` is what lets our client and the tunnel log into it.

**2. Get the client right.** Confirm the real Bedrock client is the target version. Its error screen reports the version and `RakNet:<protocol>`, which is the fastest way to be sure.

**3. Scout the data sources and refresh them.** Bump the CloudburstMC `Data` submodule pin, rerun `MiNET.BlockGen`, and check what it did and did not touch: each data folder's own `CLAUDE.md` says per file what is generated and what has a distinct source. Files with no generator (the pmmp legacy maps, the join-sequence captures) do not move on a protocol bump and go stale silently.

**4. Read Mojang's specifications and generate.** Bump the `ProtocolDocs` submodule and move packets from the XML to schema generation as Mojang completes them (see `MiNET.ProtocolGen`). The schemas are authoritative for field semantics and wire order via `x-ordinal-index`.

**5. Capture a real session through the tunnel, as early as possible.** `MiNET.Tunnel` sits between the real client and BDS, handles login on both legs locally, and forwards everything else verbatim while dumping both directions into one interleaved sequence. That capture is the ground truth every later step is measured against, so take it before guessing at packet shapes. Forwarding is raw bytes and unknown packet ids survive as `UnknownPacket`, so this does not need a finished protocol; what it does need is that a frame whose decode throws is still forwarded rather than dropped. Login and crypto are the only packets each leg handles itself, so a reshaped login handshake is the one thing that must be updated before the tunnel runs.

**6. Iterate in spawn-sequence order, one packet at a time, round-tripping each.** Priority comes from the captured order: fix what the client needs first, first. For each packet, decode the captured BDS frame, re-encode it, and require byte-identical output.

Then the ping-pong loop, which is where the remaining bugs live:

1. Our client -> BDS: parse EVERY server->client packet with zero unknown ids, zero leftover bytes, zero decode exceptions, and record the packet ORDER. Do NOT skip or leniently swallow a packet you can't parse: an unrecognized or unparseable packet is the signal to stop and fix it, not move on. Unknown ids surface as `Unknown packet with id N` (decimal), decode failures as `Error parsing bedrock message ... id=N`, leftovers as `... Still have N` in `minetlog.log`.
2. Implement the fix (XML pdu + regenerate, or the schema generator).
3. Our client -> MiNET server: confirm MiNET emits the same packets, same order, same formats.
4. Real Bedrock client -> MiNET server: reach spawn.

### Diagnosing a strict client

The real client rejects with no useful diagnostic (`InitialConnection-90`). What works:

**Read the disconnect timing first.** Roughly 100ms after a batch means the client parsed something and rejected it. Tens of seconds means it is waiting for something that never arrived, or the human closed the window. These are opposite bugs, and guessing which one you have wastes every join that follows.

**Space the burst to name the packet.** Put a one second sleep between each send in `Player.Start()`. The disconnect then lands inside a named gap and identifies the packet outright, instead of being bisected over many joins. Confirm by disabling that packet and checking the failure mode changes from rejection to timeout.

**A round-trip test proves the codec, never the content.** Decoding a captured BDS frame and re-encoding it byte-identical only exercises values BDS itself sends. Every confirmed client-killer so far was invisible to it, because the bug was in content MiNET generates and vanilla never emits. To see those, capture MiNET's own output in the same format and diff the two.

**Compare by name, never by array position.** Two registries holding the same data in a different order produce thousands of false differences. Recipes, biomes, item registries and command sets are all ordered differently from vanilla.

**Beware fields that are not on the wire.** After decode they hold their C# defaults, so the same frame decoded twice can differ (`Item.UniqueId` defaults to `Environment.TickCount`). If a difference changes between runs, it is an artifact of the harness, not the server.

Stale-binary trap: building `MiNET.csproj` alone does NOT refresh `MiNET.Console/bin` or `MiNET.Client/bin`, so you end up running old code. Always build the SOLUTION (`dotnet build src/MiNET/MiNET.sln`) before running the server or client.

Generated code also defines `IMcpeMessageHandler` (server-side handling) and `IMcpeClientMessageHandler` (client-side). `Player` implements `IMcpeMessageHandler`; `MiNET.Client` handlers implement the client interface.

### Transport stack

`Net/RakNet/` is a full RakNet implementation: `RakConnection` (UDP socket + offline handshake via `RakOfflineHandler`), `RakSession` (reliability, ordering, ACK/NAK, datagram split/reassembly). Above that sits `BedrockMessageHandler` (`ICustomMessageHandler`): batching (`McpeWrapper`), compression, and encryption. Flow for an incoming connection:

`MiNetServer` -> `RakConnection` -> `RakSession` -> `BedrockMessageHandler` -> `LoginMessageHandler` (XBL auth, encryption handshake, spawns a `Player` via `PlayerFactory`) -> `Player.HandleMcpe*` methods.

Packets are pooled (`ObjectPool` in `Net/`); `Packet.CreateObject()`/`PutPool()` manage reuse. Be careful with packet lifetime when handling or forwarding them.

## World and game layer

- `LevelManager` owns `Level` instances; `Level` runs the game tick (block ticking, entities, players, time).
- Chunks: `ChunkColumn` -> `SubChunk` (16x16x16, paletted block storage).
- World storage is pluggable via `IWorldProvider`: `AnvilWorldProvider` (Java Anvil format), `LevelDbProvider` (Bedrock LevelDB format), plus in-memory generators (`SuperflatGenerator`, `CoolWorldProvider`, etc.).
- Blocks and items are data-driven from embedded resources in `Blocks/Data/` and `Items/Data/` (canonical block states NBT, id maps, legacy mappings). `BlockFactory`/`ItemFactory` resolve runtime ids.
- The block/item classes in `Blocks/` and `Items/` are themselves generated by the `[Ignore]`d "tests" in `MiNET.Test/GenerateBlocksTests.cs` and `GenerateMobsTests.cs`. When new game versions add blocks/items, update the data files and run those manually to emit new class code.

## Plugin system

`PluginManager` scans assemblies in `PluginDirectory` (from `server.conf`). A plugin is a class with `[Plugin]` and/or implementing `IPlugin` (`OnEnable(PluginContext)`). Extension points: `[PacketHandler]` methods to intercept packets, `[Command]` methods for chat commands (permissions via `[Authorize]`). `TestPlugin` is the reference example.

## Conventions

- `.editorconfig` is authoritative: tabs for indentation, C# with Allman braces, `_camelCase` instance fields, PascalCase constants. UTF-8 BOM, no final newline.
- Every source file carries the CPAL license header; keep it in new files.
- Code style follows the existing ReSharper setup; match surrounding code rather than reformatting.
