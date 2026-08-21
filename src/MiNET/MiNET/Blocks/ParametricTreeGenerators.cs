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
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Shared placement pipeline for tree shapes: trunk cells first, canopy after, every cell
	///     broadcast as UpdateBlock with applyPhysics=false (a leaf BlockUpdate would set
	///     update_bit and change the runtime id to one the client does not render). "vine:N"
	///     carries attachment bits; shapes that do not cover the sapling cell consume it.
	/// </summary>
	public static class TreeShapePlacer
	{
		public static void Place(Level level, BlockCoordinates origin, IEnumerable<(int X, int Y, int Z, string Block)> shape, bool coversSaplingCell)
		{
			var cells = shape.ToList();

			foreach (var (dx, dy, dz, block) in cells.Where(c => c.Block.EndsWith("_log")))
			{
				Block b = BlockFactory.GetBlockByName("minecraft:" + block);
				if (b == null) continue;
				b.Coordinates = origin + new BlockCoordinates(dx, dy, dz);
				level.SetBlock(b, true, false);
			}

			foreach (var (dx, dy, dz, block) in cells.Where(c => !c.Block.EndsWith("_log")))
			{
				string rawBlock = block;
				int vineBits = 0;
				if (rawBlock.StartsWith("vine:"))
				{
					int.TryParse(rawBlock.Substring(5), out vineBits);
					rawBlock = "vine";
				}
				string fullName = rawBlock switch
				{
					"vine" => "minecraft:vine",
					"moss_carpet" => "minecraft:moss_carpet",
					_ => "minecraft:" + rawBlock,
				};
				Block b = BlockFactory.GetBlockByName(fullName);
				if (b == null) continue;
				b.Coordinates = origin + new BlockCoordinates(dx, dy, dz);
				// Vines carry their attachment direction. A vine with 0 bits has no faces to
				// render; fall back to the "north" bit.
				if (b is Vine vine)
				{
					vine.VineDirectionBits = vineBits > 0 ? vineBits : 1;
				}
				level.SetBlock(b, true, false);
			}

			if (!coversSaplingCell)
			{
				level.SetAir(origin);
			}
		}
	}

	/// <summary>
	///     Parametric tree generator: shapes are computed from random parameters fitted to the
	///     BDS 1.26.40.8 oracle captures (43 trees across 5 parity runs, 2026-08-21; see
	///     K:\mcbe\go-tools\minet-fit). Height is drawn from the observed PMF, canopy layers
	///     use the observed per-layer radii, and canopy corners are pruned with probability 0.5
	///     exactly like BDS's own random draws. Vines and branch logs follow the per-type
	///     presence rates. The output is the same cell vocabulary the literal generators emit,
	///     so the placement pipeline is shared.
	/// </summary>
	public abstract class ParametricTreeGenerator : TreeGeneratorBase
	{
		protected abstract string Wood { get; }

		protected abstract List<(int X, int Y, int Z, string Block)> BuildShape(Random random);

		/// <summary>True when the shape covers the sapling cell with its trunk base.</summary>
		protected virtual bool CoversSaplingCell => true;

		public override bool Generate(Level level, BlockCoordinates origin)
		{
			if (origin.Y < 1 || origin.Y + 24 > 256) return false;
			if (!(level.GetBlock(origin.BlockDown()) is GrassBlock or Dirt or Farmland or Podzol)) return false;

			List<(int X, int Y, int Z, string Block)> shape = BuildShape(new Random());
			TreeShapePlacer.Place(level, origin, shape, CoversSaplingCell);
			return true;
		}

		/// <summary>Weighted height draw from the observed BDS PMF.</summary>
		protected static int DrawHeight(Random random, params (int Height, int Weight)[] pmf)
		{
			int total = pmf.Sum(p => p.Weight);
			int roll = random.Next(total);
			foreach (var (height, weight) in pmf)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return pmf[^1].Height;
		}

		/// <summary>True when the tree's trunk footprint is the 2x2 square (dark/pale oak).</summary>
		protected virtual bool IsBigFootprint => false;

		/// <summary>Full Chebyshev square of the given radius; every corner cell (|dx|==r==|dz|)
		/// is dropped with probability `prune`, matching BDS's per-corner random drops. Layers at
		/// or below `trunkTop` skip the trunk footprint (BDS keeps the top log visible); layers
		/// above the trunk top cover it (the leaf sits one above the top log, exactly like the
		/// captured shapes).</summary>
		protected void AddLeafLayer(List<(int X, int Y, int Z, string Block)> cells, Random random, int y, int radius, int trunkTop, double prune = 0.5, HashSet<(int Y, int X, int Z)>? exclude = null)
		{
			if (y < 0) return;
			bool atOrBelowTop = y <= trunkTop;
			for (int dx = -radius; dx <= radius; dx++)
			{
				for (int dz = -radius; dz <= radius; dz++)
				{
					bool corner = Math.Abs(dx) == radius && Math.Abs(dz) == radius;
					if (corner && random.NextDouble() < prune) continue;
					if (atOrBelowTop && ((!IsBigFootprint && dx == 0 && dz == 0) || (IsBigFootprint && dx >= 0 && dx <= 1 && dz >= 0 && dz <= 1))) continue;
					if (exclude != null && exclude.Contains((y, dx, dz))) continue;
					cells.Add((dx, y, dz, Wood + "_leaves"));
				}
			}
		}

		/// <summary>Vine curtain: a column of N vine cells below the given point, each carrying a
		/// random cardinal attachment bit (1|2|4|8), like the captured BDS vines.</summary>
		protected void AddVineColumn(List<(int X, int Y, int Z, string Block)> cells, Random random, int x, int y, int z, int length)
		{
			int[] bits = {1, 2, 4, 8, 1 | 2, 1 | 4, 2 | 4, 1 | 8, 4 | 8, 2 | 8};
			for (int i = 0; i < length; i++)
			{
				cells.Add((x, y - i, z, "vine:" + bits[random.Next(bits.Length)]));
			}
		}
	}

	/// <summary>Oak: trunk 4-6 (PMF 4:1, 5:3, 6:1), blob canopy of 4 layers (r2, r2, r1, r1) above
	/// the trunk top, corner pruning 0.5. Fitted from 5 BDS captures.</summary>
	public class ParametricOakTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "oak";

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (4, 1), (5, 3), (6, 1));
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++) cells.Add((0, y, 0, "oak_log"));

			int top = height - 1;
			AddLeafLayer(cells, random, top - 2, 2, top);
			AddLeafLayer(cells, random, top - 1, 2, top);
			AddLeafLayer(cells, random, top, 1, top);
			AddLeafLayer(cells, random, top + 1, 1, top);
			return cells;
		}
	}

	/// <summary>Birch: trunk 5-6 (PMF 5:1, 6:4), canopy of 5 layers (r2, r2, r2, r1, r1).</summary>
	public class ParametricBirchTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "birch";

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (5, 1), (6, 4));
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++) cells.Add((0, y, 0, "birch_log"));

			int top = height - 1;
			AddLeafLayer(cells, random, top - 3, 2, top);
			AddLeafLayer(cells, random, top - 2, 2, top);
			AddLeafLayer(cells, random, top - 1, 2, top);
			AddLeafLayer(cells, random, top, 1, top);
			AddLeafLayer(cells, random, top + 1, 1, top);
			return cells;
		}
	}

	/// <summary>Spruce: trunk 4-9 (PMF 4:1, 7:3, 9:1), cone of 10 layers (r2 at the bottom
	/// tapering to r1 at the top, the occasional r3 at mid-height), sparse vines.</summary>
	public class ParametricSpruceTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "spruce";

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (4, 1), (7, 3), (9, 1));
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++) cells.Add((0, y, 0, "spruce_log"));

			int top = height - 1;
			AddLeafLayer(cells, random, top - 6, 2, top);
			AddLeafLayer(cells, random, top - 5, 1, top);
			AddLeafLayer(cells, random, top - 4, random.NextDouble() < 0.3 ? 3 : 2, top);
			AddLeafLayer(cells, random, top - 3, 2, top);
			AddLeafLayer(cells, random, top - 2, 2, top);
			AddLeafLayer(cells, random, top - 1, 2, top);
			AddLeafLayer(cells, random, top, 1, top);
			AddLeafLayer(cells, random, top + 1, 1, top, 0.5);
			AddLeafLayer(cells, random, top + 2, 1, top, 0.5);
			AddLeafLayer(cells, random, top + 3, 1, top, 0.7);

			if (random.NextDouble() < 0.2)
			{
				AddVineColumn(cells, random, 1, top - 1, 1, random.Next(3, 9));
			}
			return cells;
		}
	}

	/// <summary>Jungle: trunk 5-10 (PMF 5:1, 7:2, 8:1, 10:1), flat canopy of 4 layers; the bottom
	/// layer is r4 in 40% of trees (the wide flat canopy), vines in 80%.</summary>
	public class ParametricJungleTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "jungle";

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (5, 1), (7, 2), (8, 1), (10, 1));
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++) cells.Add((0, y, 0, "jungle_log"));

			int top = height - 1;
			AddLeafLayer(cells, random, top - 2, random.NextDouble() < 0.4 ? 4 : 2, top);
			AddLeafLayer(cells, random, top - 1, 2, top);
			int topRadius = random.NextDouble() switch { < 0.2 => 3, < 0.4 => 2, _ => 1 };
			AddLeafLayer(cells, random, top, topRadius, top);
			AddLeafLayer(cells, random, top + 1, 1, top);

			if (random.NextDouble() < 0.8)
			{
				int count = random.Next(2, 5);
				for (int i = 0; i < count; i++)
				{
					AddVineColumn(cells, random, random.Next(-2, 3), top - random.Next(0, 2), random.Next(-2, 3), random.Next(3, 12));
				}
			}
			return cells;
		}
	}

	/// <summary>Acacia: trunk 3-5, forked top with 2-3 diagonal branch chains reaching 2-4 above
	/// the trunk, flat canopy (r3, r4, r3) on the fork.</summary>
	public class ParametricAcaciaTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "acacia";

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = 3 + random.Next(3);
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++) cells.Add((0, y, 0, "acacia_log"));

			int forkTop = height - 1;
			int branches = 2 + random.Next(2);
			var directions = new[] {(1, 0), (-1, 0), (0, 1), (0, -1)};
			foreach (var (dx, dz) in directions.OrderBy(_ => random.Next()).Take(branches))
			{
				int length = 2 + random.Next(3);
				for (int i = 1; i <= length; i++)
				{
					cells.Add((dx * i, height - 1 + i, dz * i, "acacia_log"));
				}
				forkTop = Math.Max(forkTop, height - 1 + length);
			}

			// The BDS canopy sits on the fork: 5 layers above the trunk top (captured deltas
			// 0..5 with radii 3-7; the middle two are the widest). The fork logs stay visible
			// through the canopy, so leaf layers skip the fork cells.
			int trunkTop = height - 1;
			var forkCells = cells.Where(c => c.Block == "acacia_log" && (c.X != 0 || c.Z != 0)).Select(c => (c.Y, c.X, c.Z)).ToHashSet();
			AddLeafLayer(cells, random, trunkTop + 1, 3, trunkTop, 0.4, forkCells);
			AddLeafLayer(cells, random, trunkTop + 2, 3, trunkTop, 0.4, forkCells);
			AddLeafLayer(cells, random, trunkTop + 3, 4, trunkTop, 0.4, forkCells);
			AddLeafLayer(cells, random, trunkTop + 4, 4, trunkTop, 0.4, forkCells);
			AddLeafLayer(cells, random, trunkTop + 5, 3, trunkTop, 0.5, forkCells);
			return cells;
		}
	}

	/// <summary>Cherry: trunk 4-7, 2-4 diagonal branch chains reaching 2-4 above the trunk top,
	/// blob canopy r3-5 spanning trunk top -2 .. +5.</summary>
	public class ParametricCherryTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "cherry";

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = 6 + random.Next(2);
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++) cells.Add((0, y, 0, "cherry_log"));

			int top = height - 1;
			int branchTop = top;
			int branches = 2 + random.Next(3);
			var directions = new[] {(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)};
			foreach (var (dx, dz) in directions.OrderBy(_ => random.Next()).Take(branches))
			{
				int length = 2 + random.Next(2);
				for (int i = 1; i <= length; i++)
				{
					cells.Add((dx * i, top - 1 + i, dz * i, "cherry_log"));
				}
				branchTop = Math.Max(branchTop, top - 1 + length);
			}

			for (int delta = -2; delta <= 5; delta++)
			{
				int radius = delta switch
				{
					-2 => 4 + random.Next(2),
					-1 => 5 + random.Next(2),
					0 => 5 + random.Next(2),
					1 => 4 + random.Next(2),
					2 => 4 + random.Next(2),
					3 => 4 + random.Next(2),
					4 => 4 + random.Next(2),
					_ => 3 + random.Next(2),
				};
				AddLeafLayer(cells, random, branchTop + delta - 2, radius, top, 0.4);
			}
			return cells;
		}
	}

	/// <summary>Dark oak: 2x2 trunk 7-8 (PMF 7:1, 8:4), 3-4 branch clusters from the top corners,
	/// flat canopy r4-r5 with r2 top layer.</summary>
	public class ParametricDarkOakTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "dark_oak";

		protected override bool IsBigFootprint => true;

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (7, 1), (8, 4));
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++)
			{
				for (int dx = 0; dx < 2; dx++)
				for (int dz = 0; dz < 2; dz++)
					cells.Add((dx, y, dz, "dark_oak_log"));
			}

			int top = height - 1;
			var corners = new[] {(0, 0), (1, 0), (0, 1), (1, 1)};
			foreach (var (cx, cz) in corners.OrderBy(_ => random.Next()))
			{
				if (random.NextDouble() < 0.75)
				{
					int length = 1 + random.Next(3);
					int outX = cx == 0 ? -1 : 1;
					int outZ = cz == 0 ? -1 : 1;
					for (int i = 1; i <= length; i++)
					{
						cells.Add((cx + outX * i, top - 3 + i, cz + outZ * i, "dark_oak_log"));
					}
				}
			}

			AddLeafLayer(cells, random, top - 2, 4, top);
			AddLeafLayer(cells, random, top - 1, 5, top);
			AddLeafLayer(cells, random, top, 5, top);
			AddLeafLayer(cells, random, top + 1, 4, top);
			AddLeafLayer(cells, random, top + 2, 2, top);
			return cells;
		}
	}

	/// <summary>Pale oak: 2x2 trunk 6-9 (PMF 6:1, 7:2, 9:1), flat canopy r5-r4-r2.</summary>
	public class ParametricPaleOakTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "pale_oak";

		protected override bool IsBigFootprint => true;

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (6, 1), (7, 2), (9, 1));
			var cells = new List<(int X, int Y, int Z, string Block)>();
			for (int y = 0; y < height; y++)
			{
				for (int dx = 0; dx < 2; dx++)
				for (int dz = 0; dz < 2; dz++)
					cells.Add((dx, y, dz, "pale_oak_log"));
			}

			int top = height - 1;
			AddLeafLayer(cells, random, top - 1, 5, top);
			AddLeafLayer(cells, random, top, 5, top);
			AddLeafLayer(cells, random, top + 1, 4, top);
			AddLeafLayer(cells, random, top + 2, 2, top);
			return cells;
		}
	}

	/// <summary>Mangrove: trunk 4-14 (PMF 4:1, 6:1, 10:1, 14:2), surface root ring of
	/// mangrove_roots at and below ground level, 2-3 aerial root chains (diagonal logs) up the
	/// trunk, wide canopy r5-7, heavy vine curtains, moss_carpet on the roots.</summary>
	public class ParametricMangroveTreeGenerator : ParametricTreeGenerator
	{
		protected override string Wood => "mangrove";

		protected override bool CoversSaplingCell => false;

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			int height = DrawHeight(random, (4, 1), (6, 1), (10, 1), (14, 2));
			var cells = new List<(int X, int Y, int Z, string Block)>();

			// Surface roots: ring around the propagule at and below ground level.
			for (int dx = -4; dx <= 4; dx++)
			{
				for (int dz = -4; dz <= 4; dz++)
				{
					int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));
					if (dist < 2 || dist > 4) continue;
					if (random.NextDouble() < 0.5) continue;
					cells.Add((dx, -1, dz, "mangrove_roots"));
					if (random.NextDouble() < 0.6) cells.Add((dx, 0, dz, "mangrove_roots"));
					if (random.NextDouble() < 0.15) cells.Add((dx, -1, dz, "moss_carpet"));
				}
			}

			// Trunk sits above the propagule (whose cell stays air after growth, like BDS).
			for (int y = 1; y <= height; y++) cells.Add((0, y, 0, "mangrove_log"));

			// Aerial root chains: diagonal logs climbing outward from the lower trunk, staying
			// within the trunk height (the captured small trees have no chains above the top).
			int chains = 3 + random.Next(3);
			var directions = new[] {(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)};
			foreach (var (dx, dz) in directions.OrderBy(_ => random.Next()).Take(chains))
			{
				int startY = 2 + random.Next(3);
				int length = Math.Min(3 + random.Next(4), Math.Max(1, height - startY + 2));
				for (int i = 0; i < length; i++)
				{
					cells.Add((dx * i, startY + i, dz * i, "mangrove_log"));
				}
			}

			int top = height;
			AddLeafLayer(cells, random, top - 3, 6, top);
			AddLeafLayer(cells, random, top - 2, 6, top);
			AddLeafLayer(cells, random, top - 1, 6, top);
			AddLeafLayer(cells, random, top, random.NextDouble() < 0.5 ? 7 : 5, top);

			// Vine curtains from the canopy edges, hanging most of the way to the ground like
			// the captured trees (90-650 vine cells per tree).
			foreach (var (vx, vy, vz) in cells.Where(c => c.Block == "mangrove_leaves" && (c.Y == top - 3 || c.Y == top - 2))
				         .Select(c => (c.X, c.Y, c.Z)).ToList())
			{
				if (Math.Abs(vx) < 4 && Math.Abs(vz) < 4) continue;
				if (random.NextDouble() < 0.4)
				{
					AddVineColumn(cells, random, vx, vy, vz, Math.Max(4, random.Next(vy - 8, vy)));
				}
			}
			return cells;
		}
	}
}
