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
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
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

		// "log:x"/"log:z" carry the pillar axis of horizontal branch segments (BDS puts
		// pillar_axis on every log; y is the generated default and stays implicit, so only
		// x/z travel in the cell string). Parsed before the log/leaf split: the raw name
		// decides the class, the suffix the state.
		var parsed = cells.Select(c =>
		{
			string raw = c.Block;
			string axis = "y";
			if (raw.EndsWith("_log:x") || raw.EndsWith("_log:z"))
			{
				axis = raw.Substring(raw.Length - 1);
				raw = raw.Substring(0, raw.Length - 2);
			}
			return (c.X, c.Y, c.Z, Raw: raw, Axis: axis);
		}).ToList();

		foreach (var (dx, dy, dz, raw, axis) in parsed.Where(c => c.Raw.EndsWith("_log")))
		{
			Block b = BlockFactory.GetBlockByName("minecraft:" + raw);
			if (b == null) continue;
			b.Coordinates = origin + new BlockCoordinates(dx, dy, dz);
			if (b is LogBase log) log.PillarAxis = axis;
			level.SetBlock(b, true, false);
		}

		foreach (var (dx, dy, dz, block, _) in parsed.Where(c => !c.Raw.EndsWith("_log")))
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
				"mangrove_propagule:hanging" => "minecraft:mangrove_propagule",
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
			// Hanging mangrove propagules (the fruit) keep their hanging state.
			if (b is MangrovePropagule propagule && rawBlock == "mangrove_propagule:hanging")
			{
				propagule.Hanging = true;
			}
			level.SetBlock(b, true, false);
		}

		if (!coversSaplingCell)
		{
			level.SetAir(origin);
		}
	}
	}

	/// <summary>Empirical tree spec: the distributions fitted from the BDS captures (see
	/// minet-fit; data in Blocks/Data/tree-shape-specs.json).</summary>
	public sealed class TreeTypeSpec
	{
		public required int Samples { get; init; }
		public required bool BigFootprint { get; init; }
		public required int SaplingOffsetY { get; init; }
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required List<string> Blocks { get; init; }
		public required List<TreeTemplate> Templates { get; init; }
		public required Dictionary<int, List<TreeTemplate>> TemplatesByHeight { get; init; }
	}

	/// <summary>A captured tree's whole non-trunk structure (branches, canopy, roots), relative
	/// to the tree's own trunk top. Sampled as a unit and rotated, so every correlation between
	/// the fork, the canopy center and the silhouette is preserved.</summary>
	public sealed class TreeTemplate
	{
		public required int Weight { get; init; }
		public required List<(int X, int Y, int Z, int Block)> Cells { get; init; }
	}

	/// <summary>
	///     Data-driven generative tree model: the trunk height is drawn from the observed PMF,
	///     the trunk column is rebuilt, and the whole captured non-trunk structure (a sampled
	///     template) is placed on it with a random 4-fold rotation. Vines use the observed
	///     per-column model. The output cell vocabulary is the same as the literal generators,
	///     so placement is shared. Fitted on 2170 BDS-grown trees (26 mass-capture runs,
	///     2026-08-21); data in Blocks/Data/tree-shape-specs.json.
	/// </summary>
	public class EmpiricalTreeGenerator : TreeGeneratorBase
	{
		private readonly TreeTypeSpec _spec;

		public EmpiricalTreeGenerator(TreeTypeSpec spec)
		{
			_spec = spec;
		}

		protected virtual string Wood => "?";
		protected virtual bool CoversSaplingCell => true;

		public override bool Generate(Level level, BlockCoordinates origin)
		{
			if (origin.Y < 1 || origin.Y + 24 > 256) return false;
			if (!(level.GetBlock(origin.BlockDown()) is GrassBlock or Dirt or Farmland or Podzol)) return false;

			var shape = BuildShape(new Random());
			TreeShapePlacer.Place(level, origin, shape, CoversSaplingCell);
			return true;
		}

		private List<(int X, int Y, int Z, string Block)> BuildShape(Random random)
		{
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int offset = _spec.SaplingOffsetY;

			// 1. Trunk height from the PMF, restricted to heights with an EXACT template bucket.
			// A mismatched template (nearest-bucket fallback) shifts its low cells below the
			// surface: the underground trees. The clean-isolation dataset has no pollution, so
			// the whole height range stays drawable.
			int minWeight = 1;
			var heights = _spec.HeightPmf.Where(p => p.Weight >= minWeight).ToList();
			if (_spec.TemplatesByHeight.Count > 0)
			{
				var exact = heights.Where(p => _spec.TemplatesByHeight.ContainsKey(p.Height)).ToList();
				if (exact.Count > 0) heights = exact;
			}
			if (heights.Count == 0) heights = _spec.HeightPmf;
			int height = Math.Max(2, heights[DrawWeighted(random, heights.Select(h => (h, h.Weight)).ToList())].Height);
			int trunkTop = height - 1;

			// 2. Trunk footprint column(s), base at origin + SaplingOffsetY (BDS starts the
			// trunk at the sapling cell; the mangrove trunk sits one above the propagule).
			// The block under the sapling is converted to dirt the way BDS does (grass,
			// mycelium or moss become dirt; never a log).
			int footprint = _spec.BigFootprint ? 2 : 1;
			for (int y = 0; y < height; y++)
			for (int dx = 0; dx < footprint; dx++)
			for (int dz = 0; dz < footprint; dz++)
				cells.Add((dx, y + offset, dz, Wood + "_log"));
			for (int dx = 0; dx < footprint; dx++)
			for (int dz = 0; dz < footprint; dz++)
				cells.Add((dx, -1, dz, "dirt"));

			// 3. The whole captured non-trunk structure: sample a template and rotate it.
			// The template is drawn from the bucket matching the drawn H — the height draw above
			// guarantees an exact bucket, so no cross-height fallback (which would push the
			// template's low cells below the surface). Template cells are relative to the
			// template tree's trunk top; the generated trunk top is (H-1), so they land at
			// (H-1) + y. Cells below the world floor are dropped (the mangrove's deep roots and
			// low vine skirts legitimately sit BELOW the surface and are kept), and the trunk
			// span is never overwritten.
			List<TreeTemplate>? candidates = null;
			if (_spec.TemplatesByHeight.TryGetValue(height, out var byHeight) && byHeight.Count > 0)
				candidates = byHeight;
			if (candidates == null) candidates = _spec.Templates;
			if (candidates.Count > 0)
			{
				TreeTemplate template = candidates[DrawWeighted(random, candidates.Select(t => (t, t.Weight)).ToList())];
			int k = random.Next(4);
			foreach (var (x, y, z, blockIdx) in template.Cells)
			{
				// Below the world floor only: the mangrove's deep roots and low vine
				// skirts legitimately sit BELOW the surface (y<0) and must not be dropped.
				int wy = trunkTop + offset + y;
				if (wy < ChunkColumn.WorldMinY) continue;
				int wx = RotateX(x, z, k);
				int wz = RotateZ(x, z, k);
				// Defensive: never overwrite the trunk column (the fit excludes those cells).
				if (wx >= 0 && wx < footprint && wz >= 0 && wz < footprint && wy - offset >= 0 && wy - offset < height) continue;
				cells.Add((wx, wy, wz, RotateAxis(k, _spec.Blocks[blockIdx])));
			}
			}

			return cells;
		}

		private static int RotateX(int x, int z, int k) => k switch
		{
			1 => -z,
			2 => -x,
			3 => z,
			_ => x,
		};

		private static int RotateZ(int x, int z, int k) => k switch
		{
			1 => x,
			2 => -z,
			3 => -x,
			_ => z,
		};

		// A 90/270-degree rotation swaps the log's horizontal axis; y (the trunk axis) is
		// unchanged. "log:x"/"log:z" cells keep their suffix, everything else passes through.
		private static string RotateAxis(int k, string block)
		{
			if (k != 1 && k != 3) return block;
			if (block.EndsWith("_log:x")) return block.Substring(0, block.Length - 1) + "z";
			if (block.EndsWith("_log:z")) return block.Substring(0, block.Length - 1) + "x";
			return block;
		}

		private static int DrawWeighted<T>(Random random, List<(T Item, int Weight)> weighted)
		{
			int total = weighted.Sum(w => w.Weight);
			int roll = random.Next(total);
			for (int i = 0; i < weighted.Count; i++)
			{
				if (roll < weighted[i].Weight) return i;
				roll -= weighted[i].Weight;
			}
			return weighted.Count - 1;
		}
	}

	/// <summary>Spec loader: reads Blocks/Data/tree-shape-specs.json (generated by minet-fit from
	/// the BDS captures; regenerated whenever the capture dataset grows).</summary>
	public static class TreeShapeSpecs
	{
		private static readonly Lazy<Dictionary<string, TreeTypeSpec>> Specs = new(Load);

		public static TreeTypeSpec For(string wood)
		{
			return Specs.Value.TryGetValue(wood, out var spec) ? spec : throw new InvalidOperationException($"no tree spec for {wood}");
		}

		private static Dictionary<string, TreeTypeSpec> Load()
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(TreeShapeSpecs).Namespace + ".Data.tree-shape-specs.json")
				?? throw new InvalidOperationException("embedded tree-shape-specs.json missing");
			using var reader = new StreamReader(stream);
			var root = JsonNode.Parse(reader.ReadToEnd())!.AsObject();
			var result = new Dictionary<string, TreeTypeSpec>();
			foreach (var (wood, node) in root)
			{
				var spec = node!.AsObject();
				result[wood] = new TreeTypeSpec
				{
					Samples = spec["samples"]!.GetValue<int>(),
					BigFootprint = spec["bigFootprint"]!.GetValue<bool>(),
					// BDS starts the trunk AT the sapling position (rel 0); the block under
					// the sapling is converted to dirt (verified in the clean-50 captures:
					// grass at sapling-1 becomes dirt, never a log). The mangrove trunk rises
					// from the roots one above the propagule. (The old -1 reading put the
					// trunk one below the sapling: "trunk in the ground", user-visible.)
					SaplingOffsetY = spec["mangrove"]!.GetValue<bool>() ? 1 : 0,
					HeightPmf = spec["heightPmf"]!.AsObject()
						.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
						.ToList(),
					Blocks = spec["blocks"]!.AsArray().Select(b => b!.GetValue<string>()).ToList(),
					Templates = spec["templates"]!.AsArray()
						.Select(t => new TreeTemplate
						{
							Weight = t!["weight"]!.GetValue<int>(),
							Cells = t["cells"]!.AsArray()
								.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>(), c[3]!.GetValue<int>()))
								.ToList(),
						})
						.ToList(),
					TemplatesByHeight = spec["templatesByHeight"]!.AsObject()
						.ToDictionary(
							kv => int.Parse(kv.Key),
							kv => kv.Value!.AsArray()
								.Select(t => new TreeTemplate
								{
									Weight = t!["weight"]!.GetValue<int>(),
									Cells = t["cells"]!.AsArray()
										.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>(), c[3]!.GetValue<int>()))
										.ToList(),
								})
								.ToList()),
				};
			}
			return result;
		}
	}

	/// <summary>Oak: empirical spec (281 captured trees).</summary>
	public class ParametricOakTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricOakTreeGenerator() : base(TreeShapeSpecs.For("oak")) { }
		protected override string Wood => "oak";
	}

	/// <summary>Birch: empirical spec (283 captured trees).</summary>
	public class ParametricBirchTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricBirchTreeGenerator() : base(TreeShapeSpecs.For("birch")) { }
		protected override string Wood => "birch";
	}

	/// <summary>Spruce: empirical spec (281 captured trees).</summary>
	public class ParametricSpruceTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricSpruceTreeGenerator() : base(TreeShapeSpecs.For("spruce")) { }
		protected override string Wood => "spruce";
	}

	/// <summary>Jungle: empirical spec (279 captured trees).</summary>
	public class ParametricJungleTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricJungleTreeGenerator() : base(TreeShapeSpecs.For("jungle")) { }
		protected override string Wood => "jungle";
	}

	/// <summary>Acacia: empirical spec (279 captured trees).</summary>
	public class ParametricAcaciaTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricAcaciaTreeGenerator() : base(TreeShapeSpecs.For("acacia")) { }
		protected override string Wood => "acacia";
	}

	/// <summary>Cherry: empirical spec (248 captured trees).</summary>
	public class ParametricCherryTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricCherryTreeGenerator() : base(TreeShapeSpecs.For("cherry")) { }
		protected override string Wood => "cherry";
	}

	/// <summary>Dark oak: empirical spec (190 captured trees, 2x2 footprint).</summary>
	public class ParametricDarkOakTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricDarkOakTreeGenerator() : base(TreeShapeSpecs.For("dark_oak")) { }
		protected override string Wood => "dark_oak";
	}

	/// <summary>Pale oak: empirical spec (104 captured trees, 2x2 footprint).</summary>
	public class ParametricPaleOakTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricPaleOakTreeGenerator() : base(TreeShapeSpecs.For("pale_oak")) { }
		protected override string Wood => "pale_oak";
	}

	/// <summary>Mangrove: empirical spec (225 captured trees; roots, aerial chains, heavy vines).</summary>
	public class ParametricMangroveTreeGenerator : EmpiricalTreeGenerator
	{
		public ParametricMangroveTreeGenerator() : base(TreeShapeSpecs.For("mangrove")) { }
		protected override string Wood => "mangrove";
		protected override bool CoversSaplingCell => false;
	}
}
