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
using System.Linq;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test
{
	/// <summary>
	///     The procedural generator family (plan §7.4): deterministic per seed, invariant across
	///     seeds, and wired into the sapling growth path behind the TreeGenerator config key.
	/// </summary>
	[TestClass, DoNotParallelize]
	public class ProceduralTreeGeneratorTests
	{
		[TestMethod]
		public void Birch_procedural_shapes_are_deterministic_per_seed()
		{
			Level a = CreateLevel();
			Level b = CreateLevel();
			var generator = new ProceduralBirchTreeGenerator {Seed = 424242};
			generator.Generate(a, new BlockCoordinates(4, 3, 0));
			generator.Generate(b, new BlockCoordinates(4, 3, 0));
			CollectionAssert.AreEqual(DumpCells(a, new BlockCoordinates(4, 3, 0)), DumpCells(b, new BlockCoordinates(4, 3, 0)));
		}

		[TestMethod]
		public void Birch_procedural_invariants_hold_across_seeds()
		{
			// One level, one tree per 16-block slot: the level+world setup is the dominant
			// cost of these seed loops (measured ~36x), so the seeds share a single level.
			var heights = new HashSet<int>();
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 100; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 16), 3, 0);
				var generator = new ProceduralBirchTreeGenerator {Seed = seed};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "birch_log").ToList();
				var leaves = cells.Where(c => c.Block == "birch_leaves").ToList();

				int trunkHeight = logs.Count;
				Assert.IsTrue(trunkHeight >= 5 && trunkHeight <= 7, $"seed {seed}: trunk height {trunkHeight} must be 5..7");
				heights.Add(trunkHeight);
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: must produce leaves");

				// The trunk base covers the sapling cell; every leaf sits within the 5x5 canopy
				// disc of the trunk and inside the world bounds. The support block below the
				// sapling is converted to dirt (BDS parity: grass under a grown tree becomes
				// dirt, never a log).
				Assert.AreEqual("birch_log", cells.Single(c => c.X == 0 && c.Y == 0 && c.Z == 0).Block, $"seed {seed}: trunk must cover the sapling cell");
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Dirt, $"seed {seed}: support block under the sapling must become dirt");
				foreach (var leaf in leaves)
				{
					Assert.IsTrue(Math.Abs(leaf.X) <= 2 && Math.Abs(leaf.Z) <= 2, $"seed {seed}: leaf {leaf} outside the 5x5 canopy");
					Assert.IsTrue(leaf.Y >= 1 && leaf.Y <= 8, $"seed {seed}: leaf {leaf} outside the canopy band");
				}

				// Connectivity: every leaf touches (26-neighborhood) a log or another leaf.
				var all = cells.Select(c => (c.X, c.Y, c.Z)).ToHashSet();
				foreach (var leaf in leaves)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((leaf.X + dx, leaf.Y + dy, leaf.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: leaf {leaf} is isolated");
				}
			}
			// The sampled heights must cover the observed range (PMF 5:15, 6:12, 7:27).
			Assert.IsTrue(heights.SetEquals(new HashSet<int> {5, 6, 7}), "100 seeds must hit all three birch heights");
		}

		[TestMethod]
		public void Birch_procedural_fixed_seed_shape_is_registered()
		{
			// Regression catalog (plan §3.2, §7.4): seed -> shape hash. If this constant
			// moves, the sampling chain changed and the catalog is re-registered DELIBERATELY.
			Level level = CreateLevel();
			var origin = new BlockCoordinates(4, 3, 0);
			var generator = new ProceduralBirchTreeGenerator {Seed = 1};
			Assert.IsTrue(GenerateAt(generator, level, ref origin));

			var cells = DumpCells(level, origin);
			Assert.AreEqual(60, cells.Count, "seed 1 birch cell count (dump excludes the dirt support block)");
			ulong hash = Fnv1a(cells);
			// Re-registered 2026-08-23 after the trunk-base fix (trunk at rel 0, dirt under)
			// and again 2026-08-23 when the dump started carrying the log pillar axis.
			Assert.AreEqual(0xC30D0BCB2772069FUL, hash, "seed 1 birch shape hash");
		}

		[TestMethod]
		public void Oak_procedural_invariants_hold_across_seeds()
		{
			var normalHeights = new HashSet<int>();
			var largeHeights = new HashSet<int>();
			bool sawVine = false;
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 150; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 24), 3, 0);
				var generator = new ProceduralOakTreeGenerator {Seed = seed};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "oak_log").ToList();
				var leaves = cells.Where(c => c.Block == "oak_leaves").ToList();
				var vines = cells.Where(c => c.Block == "vine").ToList();

				// Trunk = the (0,0) column only (the large variant adds branch logs).
				int trunkHeight = logs.Count(l => l.X == 0 && l.Z == 0);
				Assert.IsTrue(trunkHeight >= 4 && trunkHeight <= 13, $"seed {seed}: trunk height {trunkHeight} must be 4..13");
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: must produce leaves");
				Assert.AreEqual("oak_log", cells.Single(c => c.X == 0 && c.Y == 0 && c.Z == 0).Block, $"seed {seed}: trunk must cover the sapling cell");
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Dirt, $"seed {seed}: support block under the sapling must become dirt");

				// Variant classification: large = tall trunk or wide canopy; vine = has vines.
				bool isLarge = trunkHeight >= 8 || leaves.Any(l => Math.Abs(l.X) > 3 || Math.Abs(l.Z) > 3);
				if (isLarge) largeHeights.Add(trunkHeight);
				else normalHeights.Add(trunkHeight);

				// Canopy bounds: normal/vine 5x5 (r<=2, tolerate r3 noise), large r<=8.
				int maxR = leaves.Max(l => Math.Max(Math.Abs(l.X), Math.Abs(l.Z)));
				Assert.IsTrue(isLarge ? maxR <= 8 : maxR <= 3, $"seed {seed}: canopy radius {maxR} outside the variant bound");

				// Connectivity: every leaf touches (26-neighborhood) a log or another leaf.
				var all = cells.Select(c => (c.X, c.Y, c.Z)).ToHashSet();
				foreach (var leaf in leaves)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((leaf.X + dx, leaf.Y + dy, leaf.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: leaf {leaf} is isolated");
				}

				// Vines: only the vine variant carries them; always on a trunk face, bits > 0.
				if (vines.Count > 0) sawVine = true;
				foreach (var vine in vines)
				{
					Assert.IsTrue(Math.Abs(vine.X) + Math.Abs(vine.Z) == 1, $"seed {seed}: vine {vine} not on a trunk face");
				}
			}
			Assert.IsTrue(normalHeights.Count > 0, "must see normal oaks");
			Assert.IsTrue(sawVine, "150 seeds must include the vine variant (~7%)");
		}

		[TestMethod]
		public void Oak_procedural_fixed_seed_shape_is_registered()
		{
			Level level = CreateLevel();
			var origin = new BlockCoordinates(4, 3, 0);
			var generator = new ProceduralOakTreeGenerator {Seed = 1};
			Assert.IsTrue(GenerateAt(generator, level, ref origin));

			var cells = DumpCells(level, origin);
			ulong hash = Fnv1a(cells);
			// Re-registered 2026-08-23 when the dump started carrying the log pillar axis.
			Assert.AreEqual(0x9A3BF76094141F7DUL, hash, "seed 1 oak shape hash");
		}

		[TestMethod]
		public void Oak_sapling_grows_procedurally_when_configured()
		{
			var originalProvider = Config.Provider;
			try
			{
				Config.Provider = new TestConfigProvider(new Dictionary<string, string> {["TreeGenerator"] = "procedural"});
				Level level = CreateLevel();
				var sapling = (SaplingBase) BlockFactory.GetBlockByName("minecraft:oak_sapling");
				sapling.Coordinates = new BlockCoordinates(4, 3, 0);
				level.SetBlock(sapling);

				bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					sapling.OnTick(level, true);
					grew = level.GetBlock(4, 3, 0) is not SaplingBase;
				}
				Assert.IsTrue(grew, "oak sapling must grow with TreeGenerator=procedural");
				Assert.IsTrue(level.GetBlock(4, 3, 0) is LogBase, "trunk must cover the sapling cell");
				Assert.IsTrue(DumpCells(level, new BlockCoordinates(4, 3, 0)).Any(c => c.Block == "oak_leaves"), "must produce leaves");
			}
			finally
			{
				Config.Provider = originalProvider;
			}
		}

		[TestMethod]
		public void Spruce_procedural_invariants_hold_across_seeds()
		{
			// Lone-sapling behavior (the unforced generator, what the game does from one
			// sapling): NEVER a giant (the 2x2 trunk grows only from a complete patch), the
			// vine variant appears, heights 4-9.
			var normalHeights = new HashSet<int>();
			bool sawVine = false;
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 150; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 20), 3, 0);
				var generator = new ProceduralSpruceTreeGenerator {Seed = seed};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "spruce_log").ToList();
				var leaves = cells.Where(c => c.Block == "spruce_leaves").ToList();
				var vines = cells.Where(c => c.Block == "vine").ToList();

				int trunkHeight = logs.Count(l => l.X == 0 && l.Z == 0);
				bool big = logs.Any(l => l.X == 1 && l.Z == 0) && logs.Any(l => l.X == 0 && l.Z == 1);
				Assert.IsFalse(big, $"seed {seed}: a lone sapling must never grow the giant");
				Assert.IsTrue(trunkHeight >= 4 && trunkHeight <= 9, $"seed {seed}: lone trunk height {trunkHeight} must be 4..9");
				normalHeights.Add(trunkHeight);
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: must produce leaves");
				Assert.AreEqual("spruce_log", cells.Single(c => c.X == 0 && c.Y == 0 && c.Z == 0).Block, $"seed {seed}: trunk must cover the sapling cell");
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Dirt, $"seed {seed}: support block under the sapling must become dirt");

				// Connectivity: every leaf touches (26-neighborhood) a log or another leaf.
				var all = cells.Select(c => (c.X, c.Y, c.Z)).ToHashSet();
				foreach (var leaf in leaves)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((leaf.X + dx, leaf.Y + dy, leaf.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: leaf {leaf} is isolated");
				}

				// Vines: only the vine variant carries them; always on a trunk face.
				if (vines.Count > 0) sawVine = true;
				foreach (var vine in vines)
				{
					Assert.IsTrue(Math.Abs(vine.X) + Math.Abs(vine.Z) == 1, $"seed {seed}: vine {vine} not on a trunk face");
				}
			}
			Assert.IsTrue(normalHeights.Count > 0, "must see normal spruces");
			Assert.IsTrue(sawVine, "150 lone seeds must include the vine variant (~7%)");
		}

		[TestMethod]
		public void Spruce_forced_giant_invariants_hold_across_seeds()
		{
			// The patch path (ForceVariant="giant"): always a 2x2 trunk, heights 13-29.
			// The giants span rel -36..+36, so each seed gets an 80-block slot.
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 60; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 80), 3, 0);
				var generator = new ProceduralSpruceTreeGenerator {Seed = seed, ForceVariant = "giant"};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: giant generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "spruce_log").ToList();
				var leaves = cells.Where(c => c.Block == "spruce_leaves").ToList();

				int trunkHeight = logs.Count(l => l.X == 0 && l.Z == 0);
				Assert.IsTrue(logs.Any(l => l.X == 1 && l.Z == 0) && logs.Any(l => l.X == 0 && l.Z == 1), $"seed {seed}: giant must have a 2x2 trunk");
				Assert.IsTrue(trunkHeight >= 13 && trunkHeight <= 29, $"seed {seed}: giant trunk height {trunkHeight} must be 13..29");
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: giant must produce leaves");
				Assert.AreEqual("spruce_log", cells.Single(c => c.X == 0 && c.Y == 0 && c.Z == 0).Block, $"seed {seed}: trunk must cover the patch corner");
				// The giant spruce converts the ground to PODZOL (BDS behavior, fitted from
				// 295 captured giants): the footprint cells and the solid core are
				// deterministic (>= 99.5%), the fringe draws at its fitted occupancy.
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Podzol && level.GetBlock(origin.X + 1, 2, 1) is Podzol, $"seed {seed}: podzol under the footprint");
				Assert.IsTrue(level.GetBlock(origin.X - 3, 2, 1) is Podzol, $"seed {seed}: podzol core around the footprint (rel -3,-2)");

				var all = cells.Select(c => (c.X, c.Y, c.Z)).ToHashSet();
				foreach (var leaf in leaves)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((leaf.X + dx, leaf.Y + dy, leaf.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: giant leaf {leaf} is isolated");
				}
			}
		}

		[TestMethod]
		public void Spruce_2x2_patch_grows_the_giant()
		{
			var originalProvider = Config.Provider;
			try
			{
				Config.Provider = new TestConfigProvider(new Dictionary<string, string> {["TreeGenerator"] = "procedural"});
				Level level = CreateLevel();
				for (int dx = 0; dx < 2; dx++)
				{
					for (int dz = 0; dz < 2; dz++)
					{
						var sapling = (SaplingBase) BlockFactory.GetBlockByName("minecraft:spruce_sapling");
						sapling.Coordinates = new BlockCoordinates(4 + dx, 3, 4 + dz);
						level.SetBlock(sapling);
					}
				}

				var nw = (SaplingBase) level.GetBlock(4, 3, 4);
				bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					nw.OnTick(level, true);
					grew = level.GetBlock(4, 3, 4) is not SaplingBase;
				}
				Assert.IsTrue(grew, "spruce 2x2 patch must grow");
				// The giant: a 2x2 trunk reaching 13+ blocks.
				Assert.IsTrue(level.GetBlock(4, 4, 4) is LogBase, "trunk must appear at the NW patch corner");
				Assert.IsTrue(level.GetBlock(5, 4, 5) is LogBase, "2x2 trunk must cover the SE patch corner");
				int column = 0;
				for (int y = 3; y <= 40; y++)
				{
					if (level.GetBlock(4, y, 4) is LogBase) column++;
				}
				Assert.IsTrue(column >= 13, $"giant trunk column must reach 13+, got {column}");
			}
			finally
			{
				Config.Provider = originalProvider;
			}
		}

		[TestMethod]
		public void Spruce_blocked_patch_keeps_the_saplings_and_retries()
		{
			var originalProvider = Config.Provider;
			try
			{
				Config.Provider = new TestConfigProvider(new Dictionary<string, string> {["TreeGenerator"] = "procedural"});
				Level level = CreateLevel();
				for (int dx = 0; dx < 2; dx++)
				{
					for (int dz = 0; dz < 2; dz++)
					{
						var sapling = (SaplingBase) BlockFactory.GetBlockByName("minecraft:spruce_sapling");
						sapling.Coordinates = new BlockCoordinates(4 + dx, 3, 4 + dz);
						level.SetBlock(sapling);
					}
				}
				// A stone wall 2 blocks east of the patch blocks the giant's canopy cells.
				for (int y = 3; y <= 35; y++)
				for (int z = 4; z <= 6; z++)
				{
					level.SetBlock(new Stone {Coordinates = new BlockCoordinates(6, y, z)}, true, false);
				}

				var nw = (SaplingBase) level.GetBlock(4, 3, 4);
				for (int i = 0; i < 100; i++)
				{
					nw.OnTick(level, true);
				}
				// The patch is complete but the giant cannot grow: all four saplings stay.
				Assert.IsTrue(level.GetBlock(4, 3, 4) is SaplingBase, "blocked patch must keep the NW sapling");
				Assert.IsTrue(level.GetBlock(5, 3, 5) is SaplingBase, "blocked patch must keep the SE sapling");

				// Remove the wall: the same saplings retry and grow the giant.
				for (int y = 3; y <= 35; y++)
				for (int z = 4; z <= 6; z++)
				{
					level.SetAir(new BlockCoordinates(6, y, z));
				}
				bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					nw.OnTick(level, true);
					grew = level.GetBlock(4, 3, 4) is not SaplingBase;
				}
				Assert.IsTrue(grew, "the patch must grow the giant once the space is free");
				int column = 0;
				for (int y = 3; y <= 40; y++)
				{
					if (level.GetBlock(4, y, 4) is LogBase) column++;
				}
				Assert.IsTrue(column >= 13, $"the grown tree must be the giant (column {column})");
			}
			finally
			{
				Config.Provider = originalProvider;
			}
		}

		[TestMethod]
		public void Spruce_giant_podzol_matches_the_fitted_map()
		{
			// The giant spruce ground: the per-rel-cell occupancy map fitted from 295 BDS
			// captures (mean 79.4 cells, min 60, max 105; the core ~99.7%, the fringe
			// 10-50%). 120 generated giants reproduce the distribution (the core cells are
			// >= 99.5% deterministic, so a smaller sample keeps the assertions sharp while
			// keeping the test fast — the scan is the dominant cost).
			var counts = new List<int>();
			var perCell = new Dictionary<(int X, int Z), int>();
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 120; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 80), 3, 0);
				var generator = new ProceduralSpruceTreeGenerator {Seed = seed, ForceVariant = "giant"};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: giant generate must succeed");

				int n = 0;
				for (int dx = -8; dx <= 8; dx++)
				for (int dz = -8; dz <= 8; dz++)
				{
					if (level.GetBlock(origin + new BlockCoordinates(dx, -1, dz)) is Podzol)
					{
						n++;
						perCell[(dx, dz)] = perCell.GetValueOrDefault((dx, dz)) + 1;
					}
				}
				counts.Add(n);
			}

			double mean = counts.Average();
			Assert.IsTrue(mean >= 76 && mean <= 83, $"mean podzol cells per giant {mean:F1}, expected ~79.4");
			Assert.IsTrue(counts.Min() >= 50 && counts.Max() <= 115, $"count range {counts.Min()}..{counts.Max()}, captured was 60..105");

			// The solid core (>= 99.5% in the fitted map) must stay near-deterministic,
			// the fringe must vary: a sampled tree is never a clean disc, never a square.
			foreach (var (dx, dz) in new[] {(-3, -2), (-3, 3), (4, -2), (4, 3), (-2, -3), (0, 0), (2, 1), (3, -2)})
			{
				int n = perCell.GetValueOrDefault((dx, dz));
				Assert.IsTrue(n >= 115, $"core cell ({dx},{dz}) podzol in only {n}/120");
			}
			foreach (var (dx, dz) in new[] {(-6, -3), (6, -2), (5, 6), (-5, -5)})
			{
				int n = perCell.GetValueOrDefault((dx, dz));
				Assert.IsTrue(n <= 85, $"fringe cell ({dx},{dz}) podzol in {n}/120 (fitted <= 27%)");
			}
			// The footprint is always converted.
			Assert.AreEqual(120, perCell.GetValueOrDefault((0, 0)), "footprint NW cell must always be podzol");
		}

		[TestMethod]
		public void Jungle_procedural_invariants_hold_across_seeds()
		{
			// Lone-sapling behavior: never the mega (2x2), heights 4-10, vines possible.
			var heights = new HashSet<int>();
			bool sawVine = false;
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 150; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 24), 3, 0);
				var generator = new ProceduralJungleTreeGenerator {Seed = seed};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "jungle_log").ToList();
				var leaves = cells.Where(c => c.Block == "jungle_leaves").ToList();
				var vines = cells.Where(c => c.Block == "vine").ToList();

				int trunkHeight = logs.Count(l => l.X == 0 && l.Z == 0);
				bool big = logs.Any(l => l.X == 1 && l.Z == 0) && logs.Any(l => l.X == 0 && l.Z == 1);
				Assert.IsFalse(big, $"seed {seed}: a lone sapling must never grow the mega jungle");
				Assert.IsTrue(trunkHeight >= 4 && trunkHeight <= 10, $"seed {seed}: trunk height {trunkHeight} must be 4..10");
				heights.Add(trunkHeight);
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: must produce leaves");
				Assert.AreEqual("jungle_log", cells.Single(c => c.X == 0 && c.Y == 0 && c.Z == 0).Block, $"seed {seed}: trunk must cover the sapling cell");
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Dirt, $"seed {seed}: support block under the sapling must become dirt");

				// The flat canopy stays within r2 (no "wide" variant in the isolation data).
				int maxR = leaves.Max(l => Math.Max(Math.Abs(l.X), Math.Abs(l.Z)));
				Assert.IsTrue(maxR <= 2, $"seed {seed}: canopy radius {maxR} must be <= 2");

				var all = cells.Select(c => (c.X, c.Y, c.Z)).ToHashSet();
				foreach (var leaf in leaves)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((leaf.X + dx, leaf.Y + dy, leaf.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: leaf {leaf} is isolated");
				}

				if (vines.Count > 0) sawVine = true;
				foreach (var vine in vines)
				{
					Assert.IsTrue(Math.Abs(vine.X) + Math.Abs(vine.Z) == 1, $"seed {seed}: vine {vine} not on a trunk face");
				}
			}
			Assert.IsTrue(heights.Count > 0, "must see jungle trees");
			Assert.IsTrue(sawVine, "150 lone seeds must include the vine variant");
		}

		[TestMethod]
		public void Jungle_forced_giant_invariants_hold_across_seeds()
		{
			// The patch path (ForceVariant="giant"): always a 2x2 trunk, tall cone.
			// The megas span rel -25..+25, so each seed gets a 70-block slot.
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 30; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 70), 3, 0);
				var generator = new ProceduralJungleTreeGenerator {Seed = seed, ForceVariant = "giant"};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: giant generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "jungle_log").ToList();
				var leaves = cells.Where(c => c.Block == "jungle_leaves").ToList();

				int trunkHeight = logs.Count(l => l.X == 0 && l.Z == 0);
				Assert.IsTrue(logs.Any(l => l.X == 1 && l.Z == 0) && logs.Any(l => l.X == 0 && l.Z == 1), $"seed {seed}: giant must have a 2x2 trunk");
				Assert.IsTrue(trunkHeight >= 10, $"seed {seed}: giant trunk height {trunkHeight}");
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: giant must produce leaves");
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Dirt && level.GetBlock(origin.X + 1, 2, 1) is Dirt, $"seed {seed}: dirt under the whole footprint");
			}
		}

		[TestMethod]
		public void Jungle_2x2_patch_grows_the_mega()
		{
			var originalProvider = Config.Provider;
			try
			{
				Config.Provider = new TestConfigProvider(new Dictionary<string, string> {["TreeGenerator"] = "procedural"});
				Level level = CreateLevel();
				for (int dx = 0; dx < 2; dx++)
				{
					for (int dz = 0; dz < 2; dz++)
					{
						var sapling = (SaplingBase) BlockFactory.GetBlockByName("minecraft:jungle_sapling");
						sapling.Coordinates = new BlockCoordinates(4 + dx, 3, 4 + dz);
						level.SetBlock(sapling);
					}
				}

				var nw = (SaplingBase) level.GetBlock(4, 3, 4);
				bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					nw.OnTick(level, true);
					grew = level.GetBlock(4, 3, 4) is not SaplingBase;
				}
				Assert.IsTrue(grew, "jungle 2x2 patch must grow");
				Assert.IsTrue(level.GetBlock(4, 4, 4) is LogBase, "trunk must appear at the NW patch corner");
				Assert.IsTrue(level.GetBlock(5, 4, 5) is LogBase, "2x2 trunk must cover the SE patch corner");
				int column = 0;
				for (int y = 3; y <= 40; y++)
				{
					if (level.GetBlock(4, y, 4) is LogBase) column++;
				}
				Assert.IsTrue(column >= 10, $"mega trunk column must reach 10+, got {column}");
			}
			finally
			{
				Config.Provider = originalProvider;
			}
		}

		[TestMethod]
		public void Acacia_procedural_invariants_hold_across_seeds()
		{
			// The acacia (M5): a trunk column 1-7 plus cardinal chains forking from the
			// trunk top (diagonal / vertical), a flat canopy, dirt support. Everything
			// must connect to the trunk and nothing may float.
			var heights = new HashSet<int>();
			Level level = CreateLevel();
			for (ulong seed = 1; seed <= 150; seed++)
			{
				var origin = new BlockCoordinates(4 + (int) (seed * 20), 3, 0);
				var generator = new ProceduralAcaciaTreeGenerator {Seed = seed};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: generate must succeed on empty ground");

				var cells = DumpCells(level, origin);
				var logs = cells.Where(c => c.Block == "acacia_log").ToList();
				var leaves = cells.Where(c => c.Block == "acacia_leaves").ToList();

				int trunkHeight = logs.Count(l => l.X == 0 && l.Z == 0);
				Assert.IsTrue(trunkHeight >= 1 && trunkHeight <= 8, $"seed {seed}: trunk height {trunkHeight} must be 1..8");
				heights.Add(trunkHeight);
				Assert.IsTrue(leaves.Count > 0, $"seed {seed}: must produce leaves");
				Assert.AreEqual("acacia_log", cells.Single(c => c.X == 0 && c.Y == 0 && c.Z == 0).Block, $"seed {seed}: trunk must cover the sapling cell");
				Assert.IsTrue(level.GetBlock(origin.X, 2, 0) is Dirt, $"seed {seed}: support block under the sapling must become dirt");

				// Every log not on the trunk column must be within 26-adjacency of the
				// trunk or another branch log (no floating logs), and no chain cell may
				// hang below the trunk top by more than the fit allows (attachDy).
				var all = cells.Select(c => (c.X, c.Y, c.Z)).ToHashSet();
				var branchLogs = logs.Where(l => l.X != 0 || l.Z != 0).ToList();
				foreach (var b in branchLogs)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((b.X + dx, b.Y + dy, b.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: branch log {b} is isolated");
				}

				// Every leaf must touch the trunk or another kept cell (26-adjacency).
				foreach (var leaf in leaves)
				{
					bool connected = false;
					for (int dx = -1; dx <= 1 && !connected; dx++)
					for (int dy = -1; dy <= 1 && !connected; dy++)
					for (int dz = -1; dz <= 1 && !connected; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						if (all.Contains((leaf.X + dx, leaf.Y + dy, leaf.Z + dz))) connected = true;
					}
					Assert.IsTrue(connected, $"seed {seed}: leaf {leaf} is isolated");
				}
			}
			Assert.IsTrue(heights.Count >= 3, "must see several acacia trunk heights");
		}

		[TestMethod]
		public void Acacia_procedural_fixed_seed_shape_is_registered()
		{
			// Regression catalog (plan §3.2, §7.4): seed -> shape hash. If this constant
			// moves, the sampling chain changed and the catalog is re-registered DELIBERATELY.
			Level level = CreateLevel();
			var origin = new BlockCoordinates(4, 3, 0);
			var generator = new ProceduralAcaciaTreeGenerator {Seed = 1};
			Assert.IsTrue(generator.Generate(level, origin));

			var cells = DumpCells(level, origin);
			ulong hash = Fnv1a(cells);
			// Registered 2026-08-26 with the M5 heavy-data fit; re-registered the same day
			// when the canopy became PER-CHAIN chapel lobes (the classic acacia cappella).
			Assert.AreEqual(0x2D50F72CBC8C4DFCUL, hash, "seed 1 acacia shape hash");
		}

		[TestMethod]
		public void Birch_sapling_grows_procedurally_when_configured()
		{
			var originalProvider = Config.Provider;
			try
			{
				Config.Provider = new TestConfigProvider(new Dictionary<string, string> {["TreeGenerator"] = "procedural"});
				Level level = CreateLevel();
				var sapling = (SaplingBase) BlockFactory.GetBlockByName("minecraft:birch_sapling");
				sapling.Coordinates = new BlockCoordinates(4, 3, 0);
				level.SetBlock(sapling);

				bool grew = false;
				for (int i = 0; i < 200 && !grew; i++)
				{
					sapling.OnTick(level, true);
					grew = level.GetBlock(4, 3, 0) is not SaplingBase;
				}
				Assert.IsTrue(grew, "birch sapling must grow with TreeGenerator=procedural");
				Assert.IsTrue(level.GetBlock(4, 3, 0) is LogBase, "trunk must cover the sapling cell");
				Assert.IsTrue(DumpCells(level, new BlockCoordinates(4, 3, 0)).Any(c => c.Block == "birch_leaves"), "must produce leaves");
			}
			finally
			{
				Config.Provider = originalProvider;
			}
		}

		[TestMethod]
		public void Placer_sets_pillar_axis_on_log_cells()
		{
			// "log:x"/"log:z" cells carry the axis of horizontal branch segments (BDS puts
			// pillar_axis on every log; y is the default and stays implicit in the cell).
			Level level = CreateLevel();
			var origin = new BlockCoordinates(4, 3, 0);
			(int X, int Y, int Z, string Block)[] shape =
			{
				(0, 0, 0, "oak_log"),
				(1, 0, 0, "oak_log:x"),
				(0, 0, 1, "oak_log:z"),
				(0, 1, 0, "oak_leaves"),
			};
			TreeShapePlacer.Place(level, origin, shape, coversSaplingCell: true);

			Assert.AreEqual("y", ((LogBase) level.GetBlock(4, 3, 0)).PillarAxis, "trunk log stays y");
			Assert.AreEqual("x", ((LogBase) level.GetBlock(5, 3, 0)).PillarAxis, "east-west branch is x");
			Assert.AreEqual("z", ((LogBase) level.GetBlock(4, 3, 1)).PillarAxis, "north-south branch is z");
			Assert.IsTrue(level.GetBlock(4, 4, 0) is LeavesBase, "leaf cells still place");
		}

		[TestMethod]
		public void Oak_logs_carry_bds_pillar_axis_across_seeds()
		{
			// The fitted oak vocabulary has NO plain non-trunk log: every captured branch
			// cell carries x/z (normal/vine trunks are the plain column, large branches are
			// horizontal). So every generated oak must keep trunk logs at y and every
			// non-trunk log at x or z, and the 4-fold rotation must produce both axes.
			int branchLogs = 0;
			int sawX = 0;
			int sawZ = 0;
			for (ulong seed = 1; seed <= 300; seed++)
			{
				Level level = CreateLevel();
				var origin = new BlockCoordinates(4, 3, 0);
				var generator = new ProceduralOakTreeGenerator {Seed = seed};
				Assert.IsTrue(GenerateAt(generator, level, ref origin), $"seed {seed}: generate must succeed on empty ground");

				foreach (var c in DumpCells(level, origin).Where(c => c.Block == "oak_log"))
				{
					if (c.X == 0 && c.Z == 0)
					{
						Assert.AreEqual("y", c.Axis, $"seed {seed}: trunk log {c} must stay vertical");
					}
					else
					{
						Assert.IsTrue(c.Axis is "x" or "z", $"seed {seed}: branch log {c} must carry a horizontal axis");
						branchLogs++;
						if (c.Axis == "x") sawX++;
						else sawZ++;
					}
				}
			}
			Assert.IsTrue(branchLogs > 0, "300 seeds must include large oaks with branch logs");
			Assert.IsTrue(sawX > 0 && sawZ > 0, "rotations must produce both horizontal axes");
		}

		private static bool GenerateAt(ProceduralTreeGenerator generator, Level level, ref BlockCoordinates origin)
		{
			// The superflat's ~1% random lakes (deterministic per chunk) can occupy a seed's
			// slot and make the preflight reject it; bump the slot and retry (the seed's
			// shape is deterministic, so the retry places the same tree at the new slot).
			for (int attempt = 0; attempt < 8; attempt++)
			{
				if (generator.Generate(level, origin)) return true;
				origin = new BlockCoordinates(origin.X + 4, origin.Y, origin.Z);
			}
			return false;
		}

		private static ulong Fnv1a(List<(int X, int Y, int Z, string Block, string Axis)> cells)
		{
			ulong hash = 14695981039346656037UL;
			foreach (var c in cells.OrderBy(c => c))
			{
				// Logs carry their pillar axis (y/x/z); non-log cells have none. The axis is
				// part of the registered shape: a rotation or placement bug moves the hash.
				string key = $"{c.X}:{c.Y}:{c.Z}:{c.Block}" + (c.Axis == null ? ";" : $":{c.Axis};");
				foreach (byte v in System.Text.Encoding.ASCII.GetBytes(key))
				{
					hash ^= v;
					hash *= 1099511628211UL;
				}
			}
			return hash;
		}

		private static List<(int X, int Y, int Z, string Block, string Axis)> DumpCells(Level level, BlockCoordinates origin)
		{
			var cells = new List<(int X, int Y, int Z, string Block, string Axis)>();
			// The box must cover the giant spruce (trunk 13-29, canopy to rel 29) — a too-low
			// ceiling truncates the canopy and its edge cells look isolated (false positives).
			for (int dx = -6; dx <= 6; dx++)
			for (int dy = -2; dy <= 34; dy++)
			for (int dz = -6; dz <= 6; dz++)
			{
				Block b = level.GetBlock(origin + new BlockCoordinates(dx, dy, dz));
				if (b is Air or GrassBlock or Dirt or Podzol or Bedrock or Water or Lava or Flowing) continue;
				if (b is SaplingBase) continue;
				string axis = b is LogBase log ? log.PillarAxis : null;
				cells.Add((dx, dy, dz, b.Name.Replace("minecraft:", ""), axis));
			}
			return cells;
		}

		private sealed class TestConfigProvider : ConfigProvider
		{
			private readonly Dictionary<string, string> _values;

			public TestConfigProvider(Dictionary<string, string> values)
			{
				_values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
			}

			protected override void OnInitialize()
			{
			}

			public override string ReadString(string property)
			{
				return _values.TryGetValue(property, out string value) ? value : null;
			}
		}

		private static Level CreateLevel()
		{
			string dir = Path.Combine(Path.GetTempPath(), "minet-procedural-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);

			var provider = new AnvilWorldProvider(dir)
			{
				MissingChunkProvider = new SuperflatGenerator(Dimension.Overworld)
			};

			var level = new Level(new LevelManager(), "procedural-test", provider, new EntityManager(), GameMode.Survival, Difficulty.Normal, 4);
			level.Initialize();
			return level;
		}
	}
}

