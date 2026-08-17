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

Players may be on the server. Never `Stop-Process` it: a killed process never reaches `StopServer`, so
everything built since the last save interval is lost, and anyone connected is dropped with no
warning. Stop it through the remote console instead.

After every code change, this exact loop, nothing else:

1. `MiNET.Console remote restart`, always. It transfers everyone to
   `RemoteConsole.TransferAddress`, which is `yodamine.info`, the OVH box whose parking server is
   always up, then shuts down cleanly and saves the level. Players land on the box immediately and
   stay there, so how long this server is down does not matter. They are parked, not queued:
   nothing sends them back, `/back` on the parking server does.
2. Build the SOLUTION: `dotnet build src/MiNET/MiNET.sln`. Not the project. `dotnet run` builds only
   the console and its references, and the plugins (`TestPlugin`, `MiNET.Plotter`,
   `MiNET.BuilderBase`) are loaded at runtime from `PluginDirectory`, so they are NOT references and
   never get rebuilt that way. A plugin edit then appears to do nothing and looks like a bug in the
   thing you changed.
3. Start with `dotnet run --project src/MiNET/MiNET.Console --no-build > temp_auto/minet-server.log 2>&1 &`
   from the repo root. `--no-build` because step 2 already built; never start the exe from `bin/`
   directly.
4. Wait for readiness by polling the log for `Server open for business`, with a background
   until-loop, never fixed `sleep N` guesses.

`.claude/skills/restart-server/restart-minet.sh` is this loop in one script and prints the
downtime; pass `--build` to include step 2.

**There is no downtime budget any more**, because the transfer target is a different machine.
Restart with `--build`, run the tests, take as long as the work takes.

That holds only while `RemoteConsole.TransferAddress` points off-box. A client transferred to an
address on THIS machine retries for about 22 seconds against a listener that is down, then gives
up with `InitialConnection-13` and is lost. So it must be `yodamine.info` (the box), never
`yodamine.com` (this machine) and never `127.0.0.1` (the player's own machine). The address is
resolved by the CLIENT, so it has to be a name the client can reach.

For reference, since the script prints it: a plain restart is ~6s, and with `--build` it has
measured 19s, 40s and 48s. The 48s was a one-file change in `MiNET.Console` alone, so "small edit"
predicts nothing.

Falling back without the remote console: create `temp_auto/stop-server`, which the host watches and
which stops it the same way pressing enter does. `SIGINT`/`SIGTERM` work too.

Logs, two different files:
- `temp_auto/minet-server.log` - stdout capture (console appender, TRACE and up).
- `src/MiNET/MiNET.Console/bin/Debug/net10.0/minetlog.log` - the rolling file appender; the only place VERBOSE lines (datagram/ACK traces) appear. Config in `src/MiNET/MiNET.Console/log4net.xml`; the active server config is `server.nicke.conf` next to the exe (this machine), not `server.conf`.

### Running the bot fleet (MiNET.ServiceKiller)

This exact invocation, from the repo root, every time:

```bash
src/MiNET/MiNET.ServiceKiller/bin/Debug/net10.0/MiNET.ServiceKiller.exe \
  --number-of-bots 1000 --duration-of-connection 900 \
  --processor-affinity 65520 --auto true \
  > temp_auto/fleet.log 2>&1
```

The standing CPU split on this box (Ryzen AI 7 350, 4x Zen 5 + 4x Zen 5c): the server owns
the two fast physical cores (`ProcessorAffinity=15` in server.nicke.conf, logicals 0-3) and
the fleet owns everything else (`--processor-affinity 65520`). Production-shaped runs use
the Release build of both sides (`dotnet build -c Release`, run the exe from bin/Release);
measured 2026-08-17 at radius 12: 400 walking players cost ~1.4 of the two server cores,
the fitted two-core ceiling is ~800-850, and the join burst saturates before occupancy does
(400 simultaneous radius-32 arrivals lost 6 bots to spawn starvation while 400 residents
ran clean at 1.92 cores).

Only `--number-of-bots` and `--duration-of-connection` ever change. Every other knob (batch
size 5, chunk radius 5, send interval 40-100ms, concurrent spawn on) stays default, because
the runs are compared against each other and a changed knob silently invalidates the
comparison: a spawn-batch and send-interval change once produced a "huge difference" that was
the knob, not the code. If a cadence itself is what is being measured, that is a deliberate
experiment, said out loud, not folded into a normal run.

- The built exe directly, never `dotnet run` (a run wrapper cannot be killed cleanly; `taskkill /F /T` the exe).
- `--auto true` skips the "Press Enter" prompt, which is what makes it work detached.
- `--name-offset N` when a second fleet process runs alongside, so bot names cannot collide.
- Long runs go to the background and the log gets read; never watch a stream.

Count the result BOTH ways, they are opposite failures:

```bash
grep -c "spawned, emulating" temp_auto/fleet.log        # made it into the world
grep -c "connected but never spawned" temp_auto/fleet.log  # transport up, join never finished
```

**N launched means N spawned and staying.** 987 of 1000 is not a pass, it is a defect that
halts the line until the cause is known (see the 85-bot loss: overlapping sweeps starved
logins). Never report a run without both counts read.

Server side is already configured for this in `server.nicke.conf`: `MaxNumberOfPlayers=5000`
and `InactivityTimeout=60000` (a thousand bots starve threads long enough that healthy
sessions look silent on the default 8.5s and get swept).

Bots mimic a real client's chunk manners (set in `EmulatorClient`): cold blob cache per bot
(every join pays full price, which is what a load test should stress), only the top 4
sections of each column requested (`RequestTopSections`, the surface band at the column's
own limit), and cache verdicts plus sub-chunk requests batched on the walk timer
(`BotWalker.FlushChunkResponses`) instead of answered per packet. The flags live on
`MiNetClient` and default off, so the protocol tooling keeps its immediate, exhaustive
behaviour.

**NEVER measure anything with the log root above INFO.** At TRACE/VERBOSE the two appenders
take ~29000 events a second, each a string format and a write behind an appender lock, and
per-datagram hex dumps on top. That blocks threads while burning almost no CPU, so the box
reads as idle while the world tick starves: 4ms of tick work arriving 72-118ms apart. Every
number measured that way is the logging harness, not the server. `log4net.xml` root is INFO;
raise it only for a deliberate trace session and never for a load run.

## Projects

- `MiNET` - the core server library and the NuGet package. Everything below refers to this project.
- `MiNET.Console` - console host that boots `MiNetServer`. Configured via `server.conf` (key=value, read through `MiNET.Utils.Config`).
- `MiNET.Client` - a Bedrock client/bot used to trace and reverse-engineer the protocol against a real server (e.g. vanilla BDS). `BedrockTraceHandler` dumps packets; this is the main tool when updating protocol versions. `MINET_TARGET=host:port` aims it somewhere other than the local BDS, `MINET_PACKET_DUMP=<dir>` writes every received frame as `<seq>-<name>.bin`, `MINET_BLOB_CACHE=1` makes it announce the client-side cache the way a real client does.
- `MiNET.ServiceKiller` - load-test emulator that spawns many fake clients. See "Running the bot fleet" below; never invent an invocation for it.
- `TestPlugin`, `MiNET.Plotter`, `MiNET.BuilderBase` - example/real plugins loaded at runtime (see plugin system below).
- `MiNETTests`, `MiNET.BuilderBase.Tests` - MSTest projects.

## Protocol layer (the important part)

Most work in this repo is keeping up with Mojang protocol changes. The network code in `MiNET/Net/` is largely generated:

- `MCPE Protocol.xml` is the source of truth: packet definitions, ids, fields, and the protocol/game version.
- `MCPE Protocol.tt` (T4 template) generates `MCPE Protocol.cs` from the XML; `MCPE Protocol Documentation.tt` generates the markdown protocol spec. Both outputs are committed. Regenerate after editing the XML, either via the Visual Studio design-time custom tool or the `dotnet-t4` CLI: `cd src/MiNET/MiNET/Net && t4 "MCPE Protocol.tt" -o "MCPE Protocol.cs"` (same for the documentation template).
- Never hand-edit `MCPE Protocol.cs`. To customize a packet beyond what the XML expresses, add a partial class in its own file (e.g. `Net/McpeAnimate.cs`): declare extra fields there and implement the `partial void AfterDecode()` / `AfterEncode()` hooks, which run after the generated fields. Fields that need conditional or custom encoding are left out of the XML entirely and handled in the partial.
- Reference trust order. Working implementations are the authorities on the wire format; a spec merely describes and is never exercised against a real client, so a spec-vs-implementation disagreement is a spec bug to note, not a code change to make. In order:
  1. Live BDS bytes / tunnel captures are ground truth. Zero leftover bytes proves byte alignment, not field meaning.
  2. CloudburstMC (Data + Protocol) is the primary source, both halves: Data feeds MiNET.BlockGen, Protocol is the reference implementation, and they are the fastest movers on a protocol bump.
  3. PMMP (pmmp/BedrockProtocol) is the arbiter. When any two sources disagree, PMMP usually holds the accurate value; PMMP and minecraft-data disagreeing with EACH OTHER is the warning signal to dig deeper.
  4. PrismarineJS/minecraft-data (`data/bedrock/<version>/protocol.json`, ProtoDef) is version-exact for whatever the XML targets, with occasional field errors.
  5. RaphiMC/ViaBedrock is the aggregate cross-check when its target protocol matches ours (`.../protocol/data/ProtocolConstants.java` states it): `.../data/enums/bedrock/generated/` holds ~200 enums generated from Mojang's protocol docs, `src/main/resources/assets/viabedrock/data/` holds curated game data (effects.json, block_traits.json, potion metadata in `custom/item_mappings.json`), aggregating Geyser, PMMP, CloudburstMC and Mojang (see its `Data Asset Sources.md`). The generated enums are faithful to Mojang's docs; the hand-maintained `custom/` files can lag (its potion table was missing Bedrock metas 43-46).
  6. Mojang's official docs repo (github.com/Mojang/bedrock-protocol-docs, JSON Schemas) is for field semantics, naming and wire order (`x-ordinal-index`) only. It publishes just the current protocol (usually AHEAD of our target), and a slot marked "reserved" can be live on the wire: the spec hides deprecated-but-working ids (Scale=38 and the entity bounding-box ids are real; PMMP confirms them against Mojang's "RESERVED_038").
  7. minecraft.wiki is the source for game-mechanics VALUES, not wire format: potion tables, effect durations and levels, per-edition data values. It is what ViaBedrock itself cites for effects.json. Use it whenever the question is "what does this potion/effect/mechanic do", and prefer it over guessing from an implementation's constant names.
  - Corollary from the ParticleType work: a derived reconstruction (replaying another repo's per-version diffs, a homegrown extraction script) is NOT a source. When your derivation of source A disagrees with sources B and C directly read, suspect the derivation first. Settle disputed ids by two-of-three agreement among direct reads, PMMP as tiebreak.
- `McpeProtocolInfo.ProtocolVersion` / `GameVersion` (generated from XML attributes) gate client connections.

### Updating the protocol (the working order)

A protocol update is reverse engineering against a real vanilla BDS. Work it in this order; the early steps are not preamble, they are what makes the later evidence trustworthy.

**1. Get the reference server right.** Download the BDS build matching the target protocol and run it with the configuration we are comparing against. In `server.properties`:

- `block-network-ids-are-hashes=false`, ALWAYS, when testing. Both schemes are legal (the server declares which one it uses in StartGame and the client honours it), but with hashes off both sides speak palette indices, which is what the CloudburstMC data we generate from gives us. Leave it on and every block id in a capture is a hash, comparable to nothing we hold.
- `online-mode=false` is what lets our client and the tunnel log in.
- `view-distance` and `tick-distance` both show up on the wire; tick-distance is the join-burst publisher radius.

Captures are only comparable to each other when the reference server's configuration matches, so record it alongside the capture.

**2. Get the client right.** Confirm the real Bedrock client is the target version. Its error screen reports the version and `RakNet:<protocol>`, which is the fastest way to be sure.

**3. Scout the data sources and refresh them.** Bump the CloudburstMC `Data` submodule pin, rerun `MiNET.BlockGen`, and check what it did and did not touch: each data folder's own `CLAUDE.md` says per file what is generated and what has a distinct source. Files with no generator (the pmmp legacy maps, the join-sequence captures) do not move on a protocol bump and go stale silently.

**4. Read Mojang's specifications and generate.** Start the target BDS once with a `test_config.json` of `{"generate_documentation":true}`, which writes `docs/json_schemas/protocol` and exits, then point `MiNET.ProtocolGen` at that folder (second argument, or `MINET_SCHEMA_DIR`) and move packets from the XML to schema generation as Mojang completes them. The schemas are authoritative for field semantics and wire order via `x-ordinal-index`. They are not committed, and they must come from the BDS whose version matches the XML: github.com/Mojang/bedrock-protocol-docs publishes only the current protocol, normally a version ahead of ours, and its changelogs and guides are still worth reading there.

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

RakNet is gone; NetherNet (WebRTC) is the only transport. `Net/Rtc/` is the WebRTC stack, self-contained and BouncyCastle-free: `UdpMux` (one socket, STUN/DTLS/SCTP demux), `IceSession`, `DtlsSession` (handshake in `Net/Rtc/FastDtls/`, record crypto in `DtlsRecordCrypto`), `SctpAssociation` (reliability, ordering, SACK, fragmentation), `RtcPeer` tying them together. `Net/NetherNet/` is the Bedrock layer on top: `NetherNetListener`/`NetherNetClient` (signaling and session setup), `NetherNetSession` (the `INetworkHandler`: send lane, dispatch, coalescing). Above that sits `BedrockMessageHandler` (`ICustomMessageHandler`): batching (`McpeWrapper`) and compression. There is no Bedrock-layer encryption: DTLS already covers the transport (`IsTransportEncrypted`), so the login handshake skips the session cipher entirely. Flow for an incoming connection:

`NetherNetListener` -> `RtcPeer` -> `NetherNetSession` -> `BedrockMessageHandler` -> `LoginMessageHandler` (XBL auth, spawns a `Player` via `PlayerFactory`) -> `Player.HandleMcpe*` methods.

Packets are pooled (`ObjectPool` in `Net/`); `Packet.CreateObject()`/`PutPool()` manage reuse. Be careful with packet lifetime when handling or forwarding them.

## World and game layer

- `LevelManager` owns `Level` instances; `Level` runs the game tick (block ticking, entities, players, time).
- Chunks: `ChunkColumn` -> `SubChunk` (16x16x16, paletted block storage).
- World storage is pluggable via `IWorldProvider`: `AnvilWorldProvider` (Java Anvil format), `LevelDbProvider` (Bedrock LevelDB format), plus in-memory generators (`SuperflatGenerator`, `CoolWorldProvider`, etc.).
- Blocks and items are data-driven from embedded resources in `Blocks/Data/` and `Items/Data/` (canonical block states NBT, id maps, legacy mappings). `BlockFactory`/`ItemFactory` resolve runtime ids.
- The block/item classes in `Blocks/` and `Items/` are themselves generated by the `[Ignore]`d "tests" in `MiNET.Test/GenerateBlocksTests.cs` and `GenerateMobsTests.cs`. When new game versions add blocks/items, update the data files and run those manually to emit new class code.

## Chunk delivery and the client blob cache

Three delivery modes exist, chosen per LevelChunk packet, and the client handles all of them:

1. **Legacy push**: the LevelChunk carries every subchunk serialized inline. Simplest, most bytes.
2. **Cached push**: `cacheEnabled=true`, the LevelChunk carries only blob hashes (one xxHash64 per subchunk, plus the biome blob's hash as the LAST entry; `subChunkCount` excludes the biome blob, so the hash list has count+1 entries). Only the border-blocks/block-entity tail stays inline. Server-driven: the client never requests anything, it just reports cache status.
3. **SubChunk Request System (pull)**: the LevelChunk is a skeleton (biome blob hash only), and the client requests sections itself via `SubChunkRequest`, answered with `SubChunkPacket`. Client-driven. Introduced for 1.18's 384-tall worlds because per-column push scaled with world height; pull scales with what is visible.

MiNET implements mode 3 with the blob cache layered on both halves, matching vanilla 1.26.40: [ChunkColumn.CreateSkeletonChunk](src/MiNET/MiNET/Worlds/ChunkColumn.cs) sends the skeleton, [Player.HandleMcpeSubChunkRequestPacket](src/MiNET/MiNET/Player.cs) answers section requests with cache-enabled entries, [Player.HandleMcpeClientCacheBlobStatus](src/MiNET/MiNET/Player.cs) serves misses from the content-addressed `BlobStore` ([Worlds/BlobCache/](src/MiNET/MiNET/Worlds/BlobCache/)). Block entities travel inline beside the blob id, never inside the blob (settled against a real client; blobbed block entities leave chests invisible). At protocol 2168 request mode is signaled by an explicit bool-prefixed optional `clientRequestSubchunkLimit` field, not the old -1/-2 `subChunkCount` sentinels other implementations still use.

The cache round trip and its hard rules:

- Flow: LevelChunk/SubChunkPacket announces hashes -> client answers `ClientCacheBlobStatus` (two uint64 arrays, MISS and ACK) -> server answers misses with `ClientCacheMissResponse` (hash+payload pairs). Even a cold client gets all chunk data through the miss path once caching is on; the announce packets never carry section data.
- The client batches ACK/MISS sets and flushes roughly per tick; one status packet carries at most 4095 ids.
- Validation (client disconnects on violation): a miss response containing a blob id the client did not report as MISS, or a payload whose xxHash64 (unsigned 8-byte little endian) does not match its id. So never push unsolicited blobs and never answer the same miss twice.
- Server obligation is a refcount: hold every announced payload until each announced client has acked or been served the miss. Vanilla clients run 1-8 concurrent chunk transactions depending on connection quality.

Push vs pull trade-off: cached push wins on a warm cache (metadata only, one status round trip, no request leg) but degenerates on a cold cache, because announcing a column's hashes obligates the client to materialize the whole column, deepslate included, and terrain blobs dedup badly (air collapses, real sections are near-unique). Pull sends only what the client asks for. Mojang's SubChunk doc states the client requests ALL subchunks within "approximately four LevelChunks" of its position unconditionally (the client ticking area) and everything else progressively by visibility. Inside that radius push therefore loses nothing, which makes a hybrid attractive: cached-push full-hash LevelChunks within ~4 chunks of spawn/teleport, skeletons beyond. Per-packet mode choice makes this protocol-legal. VERIFIED 2026-08-17: a real 1.26.44 client (protocol 2168) accepts full cached-push LevelChunks from MiNET (`ChunkCachedPush=true` in server.conf), renders the world, sends zero SubChunkRequests, and its cache from pull-mode sessions hits under push announcements (130k hits / 9.5k misses on the first push join, 12,853 columns), because both modes serve identical version-9 section bytes. It remains a deliberate divergence from what BDS sends (BDS uses request mode) and is catalogued as such. Known interaction: AdaptChunkRadius's signal (player standing in a never-requested column) never arms in push mode, so adaptive view distance is silently disabled under this flag and `_skeletonSentAt` never drains. Cached push is not a dead client path: PMMP-family servers still use it at current protocol.

RULING: reason about chunk delivery as a READONLY world, like most game servers. Column
versions exist in the code but never move here; the sent-set is pure membership in the
player's current disc, and the only re-push is prune/re-entry at the rim as the player
moves. Do not bring content-change invalidation into chunk-flow reasoning.

THE SLIDING WINDOW (the model, exactly; do not deviate from it):

**The window.** A player's column state is one sliding window: the disc around their
current position with their chunk radius. That window is the whole of what the client
"knows" as columns. It slides with movement: columns crossing the trailing edge are
forgotten completely, columns entering the leading edge are new, full stop. There is no
memory of columns outside the window, no history, no versions (readonly world), nothing.

**The symmetry.** The server keeps the identical window per player: the sent-set pruned to
the same disc, same centre, same radius. The two must match exactly, because the entire
delta protocol rests on it: a column entering the window gets a fresh skeleton from the
server precisely because the server forgot it too, and a fresh skeleton always means "new
column in your window", so the client always runs the full dance for it: SubChunkRequest,
hash announcements, verdicts. If the windows ever disagree, either the client waits for
chunks that never come (server thinks it has them) or gets chunks it ignores. Match, or
the protocol breaks.

**The one persistent thing.** The only cache the client has is the blob cache:
content-addressed payload hashes, and nothing else. It is completely independent of the
window, survives movement, rejoins, other servers. This is what makes the sliding window
affordable: walking back over old ground re-runs the full structural dance, but every hash
verifies as a hit, so the re-dance is metadata and round trips, never terrain bytes.
Structure is windowed and cheap to rebuild; payloads are cached forever and never re-sent.
That division is the whole design.

**The publisher packet.** NetworkChunkPublisherUpdate is exactly its name: "this is the
area I will publish chunks for". It is the intake filter guarding the window's coherence
against in-flight packets: when the window slides, chunks from the old window position are
still in the pipe, and anything arriving outside the declared area is discarded on
receive, no processing, no CPU. It does not prune, does not evict, does not touch what is
held. ChunkRadiusUpdate sets the window's size; the publisher heartbeat re-declares its
position as it moves.

**Why the 600k-rerequest fleet run failed.** Not because the sliding window is expensive:
because publisher-eviction code shrank the window to radius 4 on every pass, thrashing the
outer ring in and out of existence. A correctly sliding window re-dances only the genuine
leading rim, and on revisited ground the dance is all hits.

The bots carry exactly this: windowed KnownColumns, forgotten on movement with the
server's exact disc; persistent KnownBlobs, never forgotten; the publisher as a cheap
discard gate; and a full dance for every fresh skeleton.

Measured client behaviour (real 1.26.44 client, 2026-08-17), the facts join tuning must respect:

- The client reconciles announced hashes at a fixed budget of ~290 per tick (~5,800/s), flushing exactly one ClientCacheBlobStatus per tick. Full-horizon render time is announced-hash-count / 5,800 and nothing server-side changes it; a fully warm cache pays the same verification time and only skips the downloads (a radius-64 push join: 98k verdicts, 100% hits, zero payload bytes, ~23s to full render).
- The client's "I am in the world" edge is ServerBoundLoadingScreen (close) and SetLocalPlayerAsInitialized, sent in the same millisecond. Blob statuses trickle in BEFORE that edge, while the loading screen is still up, so gating on statuses releases too early.
- Intake outranks processing on the client: chunks flooded before its spawn work completes starve the spawn behind the whole backlog. The join shape that works is a small complete spawn block first (its own direction-blind generate pass over the join-burst radius, prune: false), spawn, then the sweep.
- Block size: groups of 4095 hashes (one status packet's worth) work; tick-sized 250-hash groups made joins WORSE - per-wrapper decompress/parse overhead dominates the client's intake, so fewer-but-bigger blocks win. Group size 1 (the pre-list-form shape) is predicted worst and untested.
- Vanilla's publisher is a heartbeat, not a declaration: the BDS capture (temp_auto/tunnel) re-stamps NetworkChunkPublisherUpdate alongside every chunk batch, radius 4 throughout the join burst, then answers ChunkRadiusUpdate after the client's loading-screen packet and re-stamps at the granted radius forever after. MiNET sending only start-of-pass publishers is a catalogued divergence.

The client-side cache itself (the undocumented half):

- Content-addressed storage keyed only by hash: no per-server namespace, and Tomcc's design gist confirms blobs are reused across sessions "or even previous sessions in different servers".
- On the current GDK Windows client it is a plain LevelDB database at `%LocalAppData%\Temp\Minecraft Bedrock\minecraftpe\blob_cache` (observed 2026-08-17: ~80MB, file numbering in the 6600s, so long-lived and genuinely persistent). The old UWP path (`Packages\Microsoft.MinecraftUWP...\LocalCache\minecraftpe\blob_cache`) exists but is empty; online guides pointing there are stale.
- Living under `Temp` means Windows disk cleanup can wipe it at any time, so persistence is best-effort by construction. Capacity and eviction policy are documented nowhere; the DB is inspectable with the same LevelDB code `LevelDbProvider` uses (copy the folder with the client closed), and retention is measurable black-box from our side via `BlobStore` hit/miss metrics across rejoins.

Sources: [LevelChunkPacket](https://github.com/Mojang/bedrock-protocol-docs/blob/main/docs/LevelChunkPacket.html), [ClientCacheBlobStatusPacket](https://github.com/Mojang/bedrock-protocol-docs/blob/main/docs/ClientCacheBlobStatusPacket.html), [ClientCacheMissResponsePacket](https://github.com/Mojang/bedrock-protocol-docs/blob/main/docs/ClientCacheMissResponsePacket.html), [ClientCacheStatusPacket](https://github.com/Mojang/bedrock-protocol-docs/blob/main/docs/ClientCacheStatusPacket.html), [ClientCacheMissResponsePacketValidation.md](https://github.com/Mojang/bedrock-protocol-docs/blob/main/additional_docs/ClientCacheMissResponsePacketValidation.md), [SubChunk Request System v1.18.10.md](https://github.com/Mojang/bedrock-protocol-docs/blob/main/additional_docs/SubChunk%20Request%20System%20v1.18.10.md), [Tomcc's client cache design gist](https://gist.github.com/Tomcc/4be79d3eafcd158c5059abd4ab2e8d35) (the only first-party description of client behaviour), [minecraft.wiki Bedrock cache files](https://minecraft.wiki/w/Bedrock_Edition_cache_files) (marks blob_cache "more information needed"). Neighborhood: [JustTalDevelops' "Exploiting the Blob Cache"](https://gist.github.com/JustTalDevelops/1abfdae7ab7618af2ec82f709ffa93bb) is the attack that forced Mojang's validation rules; cross-server CAS is exactly why unsolicited-blob injection mattered.

## Plugin system

`PluginManager` scans assemblies in `PluginDirectory` (from `server.conf`). A plugin is a class with `[Plugin]` and/or implementing `IPlugin` (`OnEnable(PluginContext)`). Extension points: `[PacketHandler]` methods to intercept packets, `[Command]` methods for chat commands (permissions via `[Authorize]`). `TestPlugin` is the reference example.

## Conventions

- `.editorconfig` is authoritative: tabs for indentation, C# with Allman braces, `_camelCase` instance fields, PascalCase constants. UTF-8 BOM, no final newline.
- Every source file carries the CPAL license header; keep it in new files.
- Code style follows the existing ReSharper setup; match surrounding code rather than reformatting.
