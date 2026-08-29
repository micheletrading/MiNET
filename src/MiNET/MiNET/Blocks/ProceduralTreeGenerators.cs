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
using System.Text.Json.Nodes;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     The shape-level PRNG (plan §3.2): one seeded instance per Generate call, threaded
	///     through every sampling step. xoshiro256** is used instead of System.Random so the
	///     sequence is a stable function of the seed across runtimes and versions: the
	///     fixed-seed regression catalog (seed → shape hash) must never move.
	/// </summary>
	public interface ITreeRng
	{
		int Next(int max);
		double NextDouble();
	}

	/// <summary>xoshiro256** seeded via SplitMix64; zero is a legal state.</summary>
	public sealed class TreeRng : ITreeRng
	{
		private ulong _s0, _s1, _s2, _s3;

		public TreeRng(ulong seed)
		{
			_s0 = SplitMix64(ref seed);
			_s1 = SplitMix64(ref seed);
			_s2 = SplitMix64(ref seed);
			_s3 = SplitMix64(ref seed);
		}

		private static ulong SplitMix64(ref ulong x)
		{
			x += 0x9E3779B97F4A7C15UL;
			ulong z = x;
			z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
			z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
			return z ^ (z >> 31);
		}

		private ulong NextU64()
		{
			ulong result = Rotl(_s1 * 5, 7) * 9;
			ulong t = _s1 << 17;
			_s2 ^= _s0;
			_s3 ^= _s1;
			_s1 ^= _s2;
			_s0 ^= _s3;
			_s2 ^= t;
			_s3 = Rotl(_s3, 45);
			return result;
		}

		private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

		public double NextDouble() => (NextU64() >> 11) * (1.0 / (1UL << 53));

		public int Next(int max) => max <= 0 ? 0 : (int) (NextDouble() * max);
	}

	/// <summary>
	///     The procedural generator family (plan §3-§5): build the complete cell list from a
	///     seeded RNG, preflight (bounds from the shape itself, atomic replaceability), then
	///     place via the shared TreeShapePlacer. On any preflight failure nothing is placed and
	///     the sapling survives (TryGrow re-seats it). Seed==0 draws a random seed (the default
	///     gameplay path); the harness and the regression tests seed explicitly.
	/// </summary>
	public abstract class ProceduralTreeGenerator : TreeGeneratorBase
	{
		public ulong Seed { get; set; }

		protected virtual bool CoversSaplingCell => true;

		protected abstract List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng);

		public sealed override bool Generate(Level level, BlockCoordinates origin)
		{
			if (origin.Y < ChunkColumn.WorldMinY + 1 || origin.Y + 24 > ChunkColumn.WorldMaxY) return false;
			if (!(level.GetBlock(origin.BlockDown()) is GrassBlock or Dirt or Farmland or Podzol)) return false;

			ulong seed = Seed != 0 ? Seed : (ulong) Random.Shared.Next() | ((ulong) (uint) Random.Shared.Next() << 32);
			var rng = new TreeRng(seed);
			var shape = BuildShape(rng);

			// Preflight: bounds from the shape's own cells, then replaceability of every
			// target cell (air, the sapling itself, other leaves, ground blocks for the trunk
			// base, and the soft vegetation BDS grows over). Atomic: reject before placing.
			int minY = shape.Min(c => c.Y) + origin.Y;
			int maxY = shape.Max(c => c.Y) + origin.Y;
			if (minY < ChunkColumn.WorldMinY || maxY > ChunkColumn.WorldMaxY) return false;

			foreach (var (dx, dy, dz, _) in shape)
			{
				Block b = level.GetBlock(origin + new BlockCoordinates(dx, dy, dz));
				if (b is Air or SaplingBase or LeavesBase or TallGrass or FlowerBase or Deadbush
				    or GrassBlock or Dirt or Farmland or Podzol) continue;
				return false;
			}

			TreeShapePlacer.Place(level, origin, shape, CoversSaplingCell);
			return true;
		}
	}

	/// <summary>Profile model (plan §4.5, fitted by minet-fit --procedural): per column height,
	/// per canopy layer (delta relative to the column top), the cells with their observed
	/// frequency (0..100). Generation draws ONE latent density per layer and emits a cell with
	/// probability weight/100 * latent; the weight>=80 core is deterministic. The height draw
	/// is restricted to heights with an exact profile bucket (a PMF entry without a bucket is
	/// a handful of trees that cannot be emitted).</summary>
	public sealed class ProfileModel
	{
		public required int SaplingOffsetY { get; init; }
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>> LayerProfiles { get; init; }

		public int SampleHeight(ITreeRng rng)
		{
			var drawable = HeightPmf.Where(p => LayerProfiles.ContainsKey(p.Height)).ToList();
			if (drawable.Count == 0) drawable = HeightPmf;
			int total = drawable.Sum(p => p.Weight);
			int roll = rng.Next(total);
			foreach (var (height, weight) in drawable)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return drawable[^1].Height;
		}
	}

	/// <summary>Vine variant profile (oak): per column height, per layer delta, the trunk-face
	/// vine cells (x, z, direction bits, frequency). The 62 captured vine oaks are perfectly
	/// regular: all four faces at y = 0..colH-4, the bits pointing at the trunk.</summary>
	public sealed class VineProfileModel
	{
		public required Dictionary<int, List<(int Delta, List<(int X, int Z, int Bits, int Weight)> Cells)>> VineProfiles { get; init; }
	}

	/// <summary>Oak variant family (plan §4.3): variant → column height → canopy → vines.
	/// Fitted from 903 captured oaks (clean-50 + 12 oak-heavy runs): normal ~86%, vine ~7%,
	/// large ~6.5%.</summary>
	public sealed class OakProceduralModel
	{
		public required int SaplingOffsetY { get; init; }
		public required List<(string Variant, int Weight)> Variants { get; init; }
		public required ProfileModel Normal { get; init; }
		public required ProfileModel? Vine { get; init; }
		public required VineProfileModel? VineProfile { get; init; }
		public required OakLargeGrammarModel? Large { get; init; }

		public string SampleVariant(ITreeRng rng)
		{
			int total = Variants.Sum(v => v.Weight);
			int roll = rng.Next(total);
			foreach (var (variant, weight) in Variants)
			{
				if (roll < weight) return variant;
				roll -= weight;
			}
			return Variants[^1].Variant;
		}
	}

	/// <summary>Spruce canopy model (plan §5.3, M3): the normal/vine canopy is a CORRELATED
	/// layered cone — the bottom ring starts at a sampled delta and the layers alternate
	/// ring/cross up to the top, the skirt replaces the bottom ring, then the cap above the
	/// trunk top. A per-layer profile averages the parity away; the grammar samples (start,
	/// layer count, skirt, cap) and emits the alternating pattern. The GIANT is a dense
	/// cone (layers at r 1-5, no alternation) and uses the shared profile machinery.</summary>
	public sealed class SpruceCanopyModel
	{
		public required bool BigFootprint { get; init; }
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required Dictionary<int, SpruceHeightGrammar>? Grammar { get; init; }
		public required Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>>? Profile { get; init; }
		public required Dictionary<int, GiantCanopyModel>? Giant { get; init; }
		public required VineProfileModel? VineProfile { get; init; }

		public int SampleHeight(ITreeRng rng)
		{
			var drawable = HeightPmf.Where(p => Grammar?.ContainsKey(p.Height) == true || Profile?.ContainsKey(p.Height) == true || Giant?.ContainsKey(p.Height) == true).ToList();
			if (drawable.Count == 0) drawable = HeightPmf;
			int total = drawable.Sum(p => p.Weight);
			int roll = rng.Next(total);
			foreach (var (height, weight) in drawable)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return drawable[^1].Height;
		}

		public List<(int X, int Y, int Z)> BuildCanopy(ITreeRng rng, int height, int trunkTop, int offset, List<(int X, int Y, int Z)> trunk)
		{
			if (Grammar != null && Grammar.TryGetValue(height, out var grammar))
			{
				return BuildGrammarCanopy(rng, grammar, trunkTop, offset, trunk);
			}
			if (Giant != null && Giant.TryGetValue(height, out var giant))
			{
				return BuildGiantCanopy(rng, giant, trunkTop, offset, trunk);
			}
			if (Profile != null && Profile.TryGetValue(height, out var layers))
			{
				return ProfileCanopy.Build(rng, layers, trunkTop, offset, trunk);
			}
			return new List<(int X, int Y, int Z)>();
		}

		private List<(int X, int Y, int Z)> BuildGiantCanopy(ITreeRng rng, GiantCanopyModel giant, int trunkTop, int offset, List<(int X, int Y, int Z)> trunk)
		{
			// The dense cone, re-anchored to the sampled bottom layer: the layers at
			// (start + layerOffset) with the fitted cell weights — the dense base cells
			// hit w>=80 and draw fully (the per-delta profile smeared them to mid weights).
			var placed = new List<(int X, int Y, int Z)>();
			int start = DrawWeighted(rng, giant.Starts);
			double latent = 0.6 + rng.NextDouble() * 0.8;
			foreach (var (layerOffset, cells) in giant.LayersByOffset)
			{
				if (start + layerOffset > giant.TopDelta) continue;
				// The top layer is always drawn: a canopy that ends below the trunk top
				// leaves the tip log exposed (user-observed: "non può esserci un tronco
				// che esce").
				bool isTop = start + layerOffset == giant.TopDelta;
				int wy = trunkTop + offset + start + layerOffset;
				foreach (var (dx, dz, weight) in cells)
				{
					if (isTop || weight >= 80 || rng.NextDouble() < Math.Min(1.0, weight / 100.0 * latent))
					{
						placed.Add((dx, wy, dz));
					}
				}
			}
			return ProfileCanopy.Connect(placed, trunk);
		}

		private List<(int X, int Y, int Z)> BuildGrammarCanopy(ITreeRng rng, SpruceHeightGrammar grammar, int trunkTop, int offset, List<(int X, int Y, int Z)> trunk)
		{
			int footprint = BigFootprint ? 2 : 1;
			var placed = new List<(int X, int Y, int Z)>();

			// Sample the start, then the observed TYPE SEQUENCE: the types per position
			// (r=ring, c=cross, S=skirt) — the count, the phases and the skirt positions all
			// come from the data.
			int start = DrawWeighted(rng, grammar.Starts);
			var sequences = grammar.Sequences[start];
			int total = sequences.Sum(s => s.Weight);
			int roll = rng.Next(total);
			string seq = sequences[0].Sequence;
			foreach (var (candidate, weight) in sequences)
			{
				if (roll < weight)
				{
					seq = candidate;
					break;
				}
				roll -= weight;
			}
			var types = seq.Split(',');
			double latent = 0.6 + rng.NextDouble() * 0.8;
			for (int t = 0; t < types.Length; t++)
			{
				int delta = start + t;
				int wy = trunkTop + offset + delta;
				string type = types[t] switch
				{
					"r" => "ring",
					"S" => "skirt",
					_ => "cross",
				};
				foreach (var (dx, dz) in CanonicalCells(type, footprint))
				{
					placed.Add((dx, wy, dz));
				}
				if (grammar.ResidualCells.TryGetValue(delta, out var byType) && byType.TryGetValue(type, out var cells))
				{
					foreach (var (dx, dz, weight) in cells)
					{
						if (IsCanonical(type, footprint, dx, dz)) continue;
						if (rng.NextDouble() < Math.Min(1.0, weight / 100.0 * latent))
						{
							placed.Add((dx, wy, dz));
						}
					}
				}
			}

			// The cap (the deltas above the trunk top) as ATOMIC patterns: the center alone
			// or the full 5-cell plus, sampled at their observed frequencies — the captured
			// tips are never partial pluses (the user-observed tip defect).
			foreach (var (delta, pattern) in grammar.CapPatterns)
			{
				int wy = trunkTop + offset + delta;
				int patternRoll = rng.Next(Math.Max(1, pattern.Center + pattern.Plus));
				if (patternRoll < pattern.Center)
				{
					placed.Add((0, wy, 0));
				}
				else if (patternRoll < pattern.Center + pattern.Plus)
				{
					placed.Add((0, wy, 0));
					placed.Add((-1, wy, 0));
					placed.Add((0, wy, -1));
					placed.Add((0, wy, 1));
					placed.Add((1, wy, 0));
				}
			}

			return ProfileCanopy.Connect(placed, trunk);
		}

		// The canonical layer shapes, geometric: the 7x7/5x5 annulus minus the corners and
		// the trunk footprint, or the 4-cell cross. The BDS rings are exactly
		// 5x5-minus-corners-minus-center (the center is the trunk column) — 20 cells; the
		// skirt 7x7-minus-corners-minus-center — 44.
		internal static IEnumerable<(int X, int Z)> CanonicalCells(string type, int footprint)
		{
			if (type == "cross")
			{
				yield return (-1, 0);
				yield return (0, -1);
				yield return (0, 1);
				yield return (1, 0);
				yield break;
			}
			int r = type == "skirt" ? 3 : 2;
			for (int dx = -r; dx <= r; dx++)
			for (int dz = -r; dz <= r; dz++)
			{
				if (Math.Abs(dx) == r && Math.Abs(dz) == r) continue;
				if (dx >= 0 && dx < footprint && dz >= 0 && dz < footprint) continue;
				yield return (dx, dz);
			}
		}

		internal static bool IsCanonical(string type, int footprint, int x, int z)
		{
			if (type == "cross") return (x == -1 && z == 0) || (x == 0 && z == -1) || (x == 0 && z == 1) || (x == 1 && z == 0);
			int r = type == "skirt" ? 3 : 2;
			if (Math.Abs(x) > r || Math.Abs(z) > r) return false;
			if (Math.Abs(x) == r && Math.Abs(z) == r) return false;
			if (x >= 0 && x < footprint && z >= 0 && z < footprint) return false;
			return true;
		}

		internal static string OtherType(string type) => type == "cross" ? "ring" : "cross";

		private static int DrawWeighted(ITreeRng rng, IReadOnlyList<(int Item, int Weight)> weighted)
		{
			int total = weighted.Sum(w => w.Weight);
			int roll = rng.Next(total);
			foreach (var (item, weight) in weighted)
			{
				if (roll < weight) return item;
				roll -= weight;
			}
			return weighted[^1].Item;
		}

		private static string DrawWeightedType(ITreeRng rng, IReadOnlyList<(string Type, int Weight)> weighted)
		{
			int total = weighted.Sum(w => w.Weight);
			int roll = rng.Next(total);
			foreach (var (item, weight) in weighted)
			{
				if (roll < weight) return item;
				roll -= weight;
			}
			return weighted[^1].Type;
		}
	}

	/// <summary>The giant spruce's dense cone, re-anchored to the sampled bottom layer: the
	/// start (the bottom leaf delta) PMF, the per-offset cell maps and the per-H canopy top
	/// (the max leaf delta) — the layers land at (start + offset) capped at the top, so the
	/// shallow starts' offsets cannot overshoot the observed canopy (measured: d4/d5 cells
	/// the captured tops never have).</summary>
	public sealed class GiantCanopyModel
	{
		public required List<(int Start, int Weight)> Starts { get; init; }
		public required int TopDelta { get; init; }
		public required Dictionary<int, List<(int X, int Z, int Weight)>> LayersByOffset { get; init; }
	}

	/// <summary>Per-height spruce grammar: the bottom-ring start deltas, the observed TYPE
	/// SEQUENCES per start (strings like "c,S,r,c,r,c" — the layer types from the bottom
	/// up, encoding the count, the types and the skirt positions; the strict alternation
	/// the old model assumed breaks in the captures), the cap PATTERNS per delta (the tips
	/// are atomic: the center alone or the full plus, never partial — the captured d1/d2/d3
	/// are [1] or [5]), and the residual cells per (delta, layer type).</summary>
	public sealed class SpruceHeightGrammar
	{
		public required List<(int Start, int Weight)> Starts { get; init; }
		public required Dictionary<int, List<(string Sequence, int Weight)>> Sequences { get; init; }
		public required Dictionary<int, (int Center, int Plus)> CapPatterns { get; init; }
		public required Dictionary<int, Dictionary<string, List<(int X, int Z, int Weight)>>> ResidualCells { get; init; }
	}

	/// <summary>Spruce variant family (plan §5.3, M3): normal (~59%), vine (~5%) and the giant
	/// (~36%, 2×2 trunk, grown only from a 2×2 sapling patch). All three use the canopy
	/// grammar (the giant with the 2×2 footprint). Fitted from 925 captured spruce
	/// (clean-50 + 12 spruce-heavy runs).</summary>
	public sealed class SpruceProceduralModel
	{
		public required int SaplingOffsetY { get; init; }
		public required List<(string Variant, int Weight)> Variants { get; init; }
		public required SpruceCanopyModel Normal { get; init; }
		public required SpruceCanopyModel? Vine { get; init; }
		public required SpruceCanopyModel? Giant { get; init; }
		// The giant spruce's ground conversion: the per-rel-cell podzol occupancy fitted from
		// the capture worlds (295 giants) — the ground under a giant is an IRREGULAR blob, not
		// a disc (the mean 79.4 cells, radius ~5-6, fringe 10-50%). Null for the jungle.
		public Dictionary<(int X, int Z), double>? PodzolCells { get; init; }

		public string SampleVariant(ITreeRng rng)
		{
			int total = Variants.Sum(v => v.Weight);
			int roll = rng.Next(total);
			foreach (var (variant, weight) in Variants)
			{
				if (roll < weight) return variant;
				roll -= weight;
			}
			return Variants[^1].Variant;
		}
	}

	/// <summary>Cherry / dark oak / pale oak (plan §5.6-5.8, M6): skeleton-first — the
	/// trunk column (cherry 1x1 colH 3-8; dark/pale 2x2 colH 6-9) plus MANY cardinal
	/// chains climbing outward, then the canopy blob anchored at the WHOLE-tree top
	/// (cherry: a big rounded blob r5-8; dark/pale: a flat canopy r5). The small and the
	/// big canopies ride the same per-wholeTop profile via the per-layer latent density.
	/// No templates.</summary>
	public sealed class CherryProceduralModel
	{
		public required int SaplingOffsetY { get; init; }
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required Dictionary<int, List<(int Count, int Weight)>> ArmCountPmf { get; init; }
		public required Dictionary<int, List<(int Axis, int AttachDx, int AttachDz, int AttachDy, string Steps, int Weight)>> ArmTuples { get; init; }
		public required Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>> BranchLayers { get; init; }
		public required Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>> Canopy { get; init; }
		public bool BigFootprint { get; init; }

		public int SampleHeight(ITreeRng rng)
		{
			// The canopy buckets are keyed by the WHOLE-tree top, not the column height,
			// so only the arm buckets gate the draw here.
			var drawable = HeightPmf.Where(p => ArmCountPmf.ContainsKey(p.Height) && ArmTuples.ContainsKey(p.Height)).ToList();
			if (drawable.Count == 0) drawable = HeightPmf;
			int total = drawable.Sum(p => p.Weight);
			int roll = rng.Next(total);
			foreach (var (height, weight) in drawable)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return drawable[^1].Height;
		}
	}

	/// <summary>Mangrove (plan §5.9, M7): the skeleton — the ROOTS (the horizontal ring at
	/// the trunk base + the vertical columns going DOWN 1-5 blocks, ~25 cells consistently;
	/// the global per-delta cell profile at the rel -5..0), the trunk column (colH 3-13,
	/// SaplingOffsetY +1: the roots start AT the propagule cell so the trunk sits one
	/// above), the AERIAL CHAINS (diagonal arms climbing outward, joint attach/endpoint
	/// tuples), and the skirt canopy (the dense leaf profile anchored at the whole-tree
	/// top). The vines and the hanging propagules are M8. No templates.</summary>
	public sealed class MangroveProceduralModel
	{
		public required int SaplingOffsetY { get; init; }
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required Dictionary<int, List<(int Count, int Weight)>> ChainCountPmf { get; init; }
		public required Dictionary<int, List<(int AttachDx, int AttachDz, int AttachDy, int EndDx, int EndDz, int EndDy, int Weight)>> ChainTuples { get; init; }
		public required List<(int Delta, List<(int X, int Z, int Weight)> Cells)> Roots { get; init; }
		public required Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>> Canopy { get; init; }

		public int SampleHeight(ITreeRng rng)
		{
			var drawable = HeightPmf.Where(p => ChainCountPmf.ContainsKey(p.Height) && ChainTuples.ContainsKey(p.Height)).ToList();
			if (drawable.Count == 0) drawable = HeightPmf;
			int total = drawable.Sum(p => p.Weight);
			int roll = rng.Next(total);
			foreach (var (height, weight) in drawable)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return drawable[^1].Height;
		}
	}

	/// <summary>Acacia (plan §5.5, M5): skeleton-first — a trunk column (colH 1-8, mode 4-5)
	/// plus CARDINAL CHAINS forking from the trunk top: diagonal chains (one cell per y
	/// step, elevation 1, the wiki's "diagonal trunk") and vertical chains (the straight
	/// continuation). The chains carry the pillar axis y like the trunk (183/183 captured
	/// branch logs). EACH CHAIN ENDPOINT carries its own small flat-topped canopy — the
	/// classic acacia "cappella" — with TWO sizes by branch depth (measured on the
	/// captures): the branches within 2 of the top carry the FULL chapel (r2 disc + top,
	/// ~40 cells), the deeper branches a smaller one (just the disc, ~26 cells). The
	/// 2-chain trees follow the captured patterns: 1+1 (a lower second chapel) 83%, 2+0
	/// (the V-fork, both ends at the top) 17%; never 3+ chains. No templates.</summary>
	public sealed class AcaciaProceduralModel
	{
		public required int SaplingOffsetY { get; init; }
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required Dictionary<int, List<(int Count, int Weight)>> ChainCountPmf { get; init; }
		public required Dictionary<int, List<(int AttachDx, int AttachDz, int AttachDy, int EndDx, int EndDz, int EndDy, int Weight)>> ChainTopTuples { get; init; }
		public required Dictionary<int, List<(int AttachDx, int AttachDz, int AttachDy, int EndDx, int EndDz, int EndDy, int Weight)>> ChainLowerTuples { get; init; }
		public required List<(int Delta, int Presence, List<(int X, int Z, int Weight)> Cells)> LobeTop { get; init; }
		public required List<(int Delta, int Presence, List<(int X, int Z, int Weight)> Cells)> LobeDeep { get; init; }

		public int SampleHeight(ITreeRng rng)
		{
			var drawable = HeightPmf.Where(p => ChainCountPmf.ContainsKey(p.Height) && ChainTopTuples.ContainsKey(p.Height)).ToList();
			if (drawable.Count == 0) drawable = HeightPmf;
			int total = drawable.Sum(p => p.Weight);
			int roll = rng.Next(total);
			foreach (var (height, weight) in drawable)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return drawable[^1].Height;
		}
	}

	/// <summary>Oak large/fancy variant grammar (plan §5.2, M2): a dense canopy blob from the
	/// per-H profile (the shared machinery — the blob is dense, the profile works) plus a
	/// BRANCH GRAMMAR: per H, the branch-count PMF and the observed per-branch parameter
	/// tuples (attachment delta, azimuth octant, length, elevation) with weights; the
	/// generator samples the joint tuple and emits the axis-aligned path. No templates.
	/// Fitted from 58 captured large oaks (clean-50 + 12 oak-heavy runs).</summary>
	public sealed class LargeOakHeightModel
	{
		public required List<(int Delta, List<(int X, int Z, int Weight)> Cells)> Canopy { get; init; }
		public required List<(int Count, int Weight)> BranchCountPmf { get; init; }
		public required List<(int Delta, int Azimuth, int Length, int Elevation, int Weight)> BranchPaths { get; init; }
	}

	public sealed class OakLargeGrammarModel
	{
		public required List<(int Height, int Weight)> HeightPmf { get; init; }
		public required Dictionary<int, LargeOakHeightModel> PerHeight { get; init; }

		public int SampleHeight(ITreeRng rng)
		{
			int total = HeightPmf.Sum(p => p.Weight);
			int roll = rng.Next(total);
			foreach (var (height, weight) in HeightPmf)
			{
				if (roll < weight) return height;
				roll -= weight;
			}
			return HeightPmf[^1].Height;
		}
	}

	/// <summary>Procedural model loader: Blocks/Data/procedural-tree-params.json (generated by
	/// minet-fit --procedural from the BDS captures). A wood absent from the file has no
	/// procedural model yet (its milestone decides the class); For() returns null.
	/// SetModel() overrides the embedded model at runtime — the CV harness feeds it the
	/// per-fold fit (plan §7.1).</summary>
	public static class ProceduralTreeParams
	{
		private static readonly Dictionary<string, object> Overrides = new();
		private static readonly Lazy<Dictionary<string, object>> Models = new(Load);

		public static object? For(string wood)
		{
			if (Overrides.TryGetValue(wood, out var overrideModel)) return overrideModel;
			return Models.Value.TryGetValue(wood, out var model) ? model : null;
		}

		public static void SetModel(string wood, string json)
		{
			Overrides[wood] = ParseWood(JsonNode.Parse(json)!.AsObject());
		}

		private static Dictionary<string, object> Load()
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(ProceduralTreeParams).Namespace + ".Data.procedural-tree-params.json");
			if (stream == null) return new Dictionary<string, object>();

			using var reader = new StreamReader(stream);
			var root = JsonNode.Parse(reader.ReadToEnd())!.AsObject();
			var result = new Dictionary<string, object>();
			foreach (var (wood, node) in root)
			{
				result[wood] = ParseWood(node!.AsObject());
			}
			return result;
		}

		private static object ParseWood(JsonObject spec)
		{
			if (spec.ContainsKey("lobeTop")) return ParseAcacia(spec);
			if (spec.ContainsKey("roots")) return ParseMangrove(spec);
			if (spec.ContainsKey("chains") && spec.ContainsKey("canopy")) return ParseCherry(spec);
			if (!spec.ContainsKey("variants")) return ParseProfile(spec);
			return spec.ContainsKey("large") ? ParseOak(spec) : ParseSpruce(spec);
		}

		private static ProfileModel ParseProfile(JsonObject spec)
		{
			return new ProfileModel
			{
				SaplingOffsetY = spec["saplingOffsetY"]!.GetValue<int>(),
				HeightPmf = spec["heightPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList(),
				LayerProfiles = ParseLayerProfiles(spec["layerProfiles"]!.AsObject()),
			};
		}

		private static Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>> ParseLayerProfiles(JsonObject profiles)
		{
			var result = new Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>>();
			foreach (var (heightKey, layers) in profiles)
			{
				var layersList = new List<(int Delta, List<(int X, int Z, int Weight)> Cells)>();
				foreach (var (deltaKey, cells) in layers!.AsObject())
				{
					var cellList = cells!.AsArray()
						.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
						.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
						.ToList();
					layersList.Add((int.Parse(deltaKey), cellList));
				}
				result[int.Parse(heightKey)] = layersList;
			}
			return result;
		}

		private static OakProceduralModel ParseOak(JsonObject spec)
		{
			var normal = ParseProfile(spec["normal"]!.AsObject());

			ProfileModel? vine = null;
			VineProfileModel? vineProfile = null;
			if (spec["vine"] is JsonObject vineSpec)
			{
				vine = ParseProfile(vineSpec);
				vineProfile = ParseVineProfile(vineSpec);
			}

			OakLargeGrammarModel? large = null;
			if (spec["large"] is JsonObject largeSpec)
			{
				var perHeight = new Dictionary<int, LargeOakHeightModel>();
				foreach (var (heightKey, heightSpec) in largeSpec["perHeight"]!.AsObject())
				{
					var canopy = new List<(int Delta, List<(int X, int Z, int Weight)> Cells)>();
					foreach (var (deltaKey, cells) in heightSpec!["canopy"]!.AsObject())
					{
						canopy.Add((int.Parse(deltaKey), cells!.AsArray()
							.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
							.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
							.ToList()));
					}
					perHeight[int.Parse(heightKey)] = new LargeOakHeightModel
					{
						Canopy = canopy,
						BranchCountPmf = heightSpec["branchCountPmf"]!.AsObject()
							.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
							.ToList(),
						BranchPaths = heightSpec["branchPaths"]!.AsArray()
							.Select(p => (p!["delta"]!.GetValue<int>(), p["azimuth"]!.GetValue<int>(), p["length"]!.GetValue<int>(), p["elevation"]!.GetValue<int>(), p["weight"]!.GetValue<int>()))
							.ToList(),
					};
				}
				large = new OakLargeGrammarModel
				{
					HeightPmf = largeSpec["heightPmf"]!.AsObject()
						.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
						.ToList(),
					PerHeight = perHeight,
				};
			}

			return new OakProceduralModel
			{
				SaplingOffsetY = spec["saplingOffsetY"]!.GetValue<int>(),
				Variants = spec["variants"]!.AsObject()
					.Select(kv => (kv.Key, kv.Value!.GetValue<int>()))
					.ToList(),
				Normal = normal,
				Vine = vine,
				VineProfile = vineProfile,
				Large = large,
			};
		}

		private static VineProfileModel? ParseVineProfile(JsonObject spec)
		{
			if (spec["vineProfiles"] is not JsonObject vineProfiles) return null;
			var parsed = new Dictionary<int, List<(int Delta, List<(int X, int Z, int Bits, int Weight)> Cells)>>();
			foreach (var (heightKey, layers) in vineProfiles)
			{
				var layersList = new List<(int Delta, List<(int X, int Z, int Bits, int Weight)> Cells)>();
				foreach (var (deltaKey, cells) in layers!.AsObject())
				{
					var cellList = cells!.AsArray()
						.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>(), c[3]!.GetValue<int>()))
						.Select(c => (X: c.Item1, Z: c.Item2, Bits: c.Item3, Weight: c.Item4))
						.ToList();
					layersList.Add((int.Parse(deltaKey), cellList));
				}
				parsed[int.Parse(heightKey)] = layersList;
			}
			return new VineProfileModel {VineProfiles = parsed};
		}

		private static SpruceCanopyModel ParseSpruceCanopy(JsonObject spec)
		{
			Dictionary<int, SpruceHeightGrammar>? grammar = null;
			Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>>? profile = null;
			Dictionary<int, GiantCanopyModel>? giant = null;

			static Dictionary<int, List<(int X, int Z, int Weight)>> ParseCellMap(JsonObject map)
			{
				var result = new Dictionary<int, List<(int X, int Z, int Weight)>>();
				foreach (var (deltaKey, cells) in map)
				{
					result[int.Parse(deltaKey)] = cells!.AsArray()
						.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
						.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
						.ToList();
				}
				return result;
			}

			static Dictionary<int, Dictionary<string, List<(int X, int Z, int Weight)>>> ParseResidualMap(JsonObject map)
			{
				var result = new Dictionary<int, Dictionary<string, List<(int X, int Z, int Weight)>>>();
				foreach (var (deltaKey, byType) in map)
				{
					result[int.Parse(deltaKey)] = byType!.AsObject().ToDictionary(
						t => t.Key,
						t => t.Value!.AsArray()
							.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
							.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
							.ToList());
				}
				return result;
			}

			if (spec["canopyByHeight"] is JsonObject canopyByHeight
			    && canopyByHeight.First().Value?["layersByOffset"] == null)
			{
				grammar = new Dictionary<int, SpruceHeightGrammar>();
				foreach (var (heightKey, node) in canopyByHeight)
				{
					grammar[int.Parse(heightKey)] = new SpruceHeightGrammar
					{
						Starts = node!["starts"]!.AsObject()
							.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
							.ToList(),
						Sequences = node["sequences"]!.AsObject()
							.ToDictionary(
								kv => int.Parse(kv.Key),
								kv => kv.Value!.AsObject()
									.Select(c => (c.Key, c.Value!.GetValue<int>()))
									.ToList()),
						CapPatterns = node["capCells"]!.AsObject()
							.ToDictionary(
								kv => int.Parse(kv.Key),
								kv => (Center: kv.Value!["1"]?.GetValue<int>() ?? 0, Plus: kv.Value["5"]?.GetValue<int>() ?? 0)),
						ResidualCells = node["residualCells"] is JsonObject residual
							? ParseResidualMap(residual)
							: new Dictionary<int, Dictionary<string, List<(int X, int Z, int Weight)>>>(),
					};
				}
			}
			else if (spec["layerProfiles"] is JsonObject layerProfiles)
			{
				profile = ParseLayerProfiles(layerProfiles);
			}
			else if (spec["canopyByHeight"] is JsonObject giantByHeight
			         && giantByHeight.First().Value?["layersByOffset"] != null)
			{
				giant = new Dictionary<int, GiantCanopyModel>();
				foreach (var (heightKey, node) in giantByHeight)
				{
					giant[int.Parse(heightKey)] = new GiantCanopyModel
					{
						Starts = node!["starts"]!.AsObject()
							.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
							.ToList(),
						TopDelta = node["topDelta"]?.GetValue<int>() ?? 0,
						LayersByOffset = node["layersByOffset"]!.AsObject()
							.ToDictionary(
								kv => int.Parse(kv.Key),
								kv => kv.Value!.AsArray()
									.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
									.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
									.ToList()),
					};
				}
			}

			return new SpruceCanopyModel
			{
				BigFootprint = spec["bigFootprint"]?.GetValue<bool>() ?? false,
				HeightPmf = spec["heightPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList(),
				Grammar = grammar,
				Profile = profile,
				Giant = giant,
				VineProfile = ParseVineProfile(spec),
			};
		}

		private static MangroveProceduralModel ParseMangrove(JsonObject spec)
		{
			var chains = new Dictionary<int, List<(int Count, int Weight)>>();
			var chainTuples = new Dictionary<int, List<(int AttachDx, int AttachDz, int AttachDy, int EndDx, int EndDz, int EndDy, int Weight)>>();
			foreach (var (heightKey, chainSpec) in spec["chains"]!.AsObject())
			{
				var obj = chainSpec!.AsObject();
				chains[int.Parse(heightKey)] = obj["countPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList();
				chainTuples[int.Parse(heightKey)] = obj["chainTuples"]!.AsArray()
					.Select(t => (t!["attachDx"]!.GetValue<int>(), t["attachDz"]!.GetValue<int>(), t["attachDy"]!.GetValue<int>(),
						t["endDx"]!.GetValue<int>(), t["endDz"]!.GetValue<int>(), t["endDy"]!.GetValue<int>(), t["weight"]!.GetValue<int>()))
					.ToList();
			}

			var roots = new List<(int Delta, List<(int X, int Z, int Weight)> Cells)>();
			if (spec["roots"] is JsonObject rootSpec)
			{
				foreach (var (deltaKey, cells) in rootSpec)
				{
					var cellList = cells!.AsArray()
						.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
						.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
						.ToList();
					roots.Add((int.Parse(deltaKey), cellList));
				}
			}

			return new MangroveProceduralModel
			{
				SaplingOffsetY = spec["saplingOffsetY"]!.GetValue<int>(),
				HeightPmf = spec["heightPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList(),
				ChainCountPmf = chains,
				ChainTuples = chainTuples,
				Roots = roots.OrderBy(r => r.Delta).ToList(),
				Canopy = ParseLayerProfiles(spec["canopy"]!.AsObject()),
			};
		}

		private static CherryProceduralModel ParseCherry(JsonObject spec)
		{
			var chains = new Dictionary<int, List<(int Count, int Weight)>>();
			var chainTuples = new Dictionary<int, List<(int Axis, int AttachDx, int AttachDz, int AttachDy, string Steps, int Weight)>>();
			var branchLayers = new Dictionary<int, List<(int Delta, List<(int X, int Z, int Weight)> Cells)>>();
			foreach (var (heightKey, chainSpec) in spec["chains"]!.AsObject())
			{
				var obj = chainSpec!.AsObject();
				if (obj["layers"] is JsonObject layersSpec)
				{
					var layersList = new List<(int Delta, List<(int X, int Z, int Weight)> Cells)>();
					foreach (var (deltaKey, cells) in layersSpec)
					{
						var cellList = cells!.AsArray()
							.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
							.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
							.ToList();
						layersList.Add((int.Parse(deltaKey), cellList));
					}
					branchLayers[int.Parse(heightKey)] = layersList.OrderBy(l => l.Delta).ToList();
				}
				else
				{
					chains[int.Parse(heightKey)] = obj["countPmf"]!.AsObject()
						.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
						.ToList();
					chainTuples[int.Parse(heightKey)] = obj["armTuples"]!.AsArray()
						.Select(t => (t!["axis"]!.GetValue<int>(), t["attachDx"]!.GetValue<int>(), t["attachDz"]!.GetValue<int>(),
							t["attachDy"]!.GetValue<int>(), t["steps"]!.GetValue<string>()!, t["weight"]!.GetValue<int>()))
						.ToList();
				}
			}

			return new CherryProceduralModel
			{
				SaplingOffsetY = spec["saplingOffsetY"]!.GetValue<int>(),
				HeightPmf = spec["heightPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList(),
				ArmCountPmf = chains,
				ArmTuples = chainTuples,
				BranchLayers = branchLayers,
				Canopy = ParseLayerProfiles(spec["canopy"]!.AsObject()),
				BigFootprint = spec["bigFootprint"]?.GetValue<bool>() ?? false,
			};
		}

		private static AcaciaProceduralModel ParseAcacia(JsonObject spec)
		{
			var chains = new Dictionary<int, List<(int Count, int Weight)>>();
			foreach (var (heightKey, chainSpec) in spec["chains"]!.AsObject())
			{
				var obj = chainSpec!.AsObject();
				chains[int.Parse(heightKey)] = obj["countPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList();
			}

			var chainTopTuples = new Dictionary<int, List<(int AttachDx, int AttachDz, int AttachDy, int EndDx, int EndDz, int EndDy, int Weight)>>();
			var chainLowerTuples = new Dictionary<int, List<(int AttachDx, int AttachDz, int AttachDy, int EndDx, int EndDz, int EndDy, int Weight)>>();
			foreach (var (heightKey, chainSpec) in spec["chains"]!.AsObject())
			{
				chainTopTuples[int.Parse(heightKey)] = chainSpec!["topTuples"]!.AsArray()
					.Select(t => (t!["attachDx"]!.GetValue<int>(), t["attachDz"]!.GetValue<int>(), t["attachDy"]!.GetValue<int>(),
						t["endDx"]!.GetValue<int>(), t["endDz"]!.GetValue<int>(), t["endDy"]!.GetValue<int>(), t["weight"]!.GetValue<int>()))
					.ToList();
				chainLowerTuples[int.Parse(heightKey)] = chainSpec!["lowerTuples"]!.AsArray()
					.Select(t => (t!["attachDx"]!.GetValue<int>(), t["attachDz"]!.GetValue<int>(), t["attachDy"]!.GetValue<int>(),
						t["endDx"]!.GetValue<int>(), t["endDz"]!.GetValue<int>(), t["endDy"]!.GetValue<int>(), t["weight"]!.GetValue<int>()))
					.ToList();
			}

			var canopy = new List<(int Delta, int Presence, List<(int X, int Z, int Weight)> Cells)>();
			if (spec["lobeTop"] is JsonObject lobeSpec)
			{
				foreach (var (deltaKey, layer) in lobeSpec)
				{
					var cellList = layer!["cells"]!.AsArray()
						.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
						.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
						.ToList();
					canopy.Add((int.Parse(deltaKey), layer["p"]?.GetValue<int>() ?? 100, cellList));
				}
			}
			var canopyDeep = new List<(int Delta, int Presence, List<(int X, int Z, int Weight)> Cells)>();
			if (spec["lobeDeep"] is JsonObject lobeDeepSpec)
			{
				foreach (var (deltaKey, layer) in lobeDeepSpec)
				{
					var cellList = layer!["cells"]!.AsArray()
						.Select(c => (c![0]!.GetValue<int>(), c[1]!.GetValue<int>(), c[2]!.GetValue<int>()))
						.Select(c => (X: c.Item1, Z: c.Item2, Weight: c.Item3))
						.ToList();
					canopyDeep.Add((int.Parse(deltaKey), layer["p"]?.GetValue<int>() ?? 100, cellList));
				}
			}

			return new AcaciaProceduralModel
			{
				SaplingOffsetY = spec["saplingOffsetY"]!.GetValue<int>(),
				HeightPmf = spec["heightPmf"]!.AsObject()
					.Select(kv => (int.Parse(kv.Key), kv.Value!.GetValue<int>()))
					.ToList(),
				ChainCountPmf = chains,
				ChainTopTuples = chainTopTuples,
				ChainLowerTuples = chainLowerTuples,
				LobeTop = canopy.OrderBy(l => l.Delta).ToList(),
				LobeDeep = canopyDeep.OrderBy(l => l.Delta).ToList(),
			};
		}

		private static SpruceProceduralModel ParseSpruce(JsonObject spec)
		{
			var normal = ParseSpruceCanopy(spec["normal"]!.AsObject());
			SpruceCanopyModel? vine = spec["vine"] is JsonObject vineSpec ? ParseSpruceCanopy(vineSpec) : null;
			SpruceCanopyModel? giant = spec["giant"] is JsonObject giantSpec ? ParseSpruceCanopy(giantSpec) : null;

			Dictionary<(int X, int Z), double>? podzol = null;
			if (spec["podzol"] is JsonArray podzolCells)
			{
				podzol = podzolCells
					.Select(c => ((c![0]!.GetValue<int>(), c[1]!.GetValue<int>()), c[2]!.GetValue<int>() / 100.0))
					.ToDictionary(p => p.Item1, p => p.Item2);
			}

			return new SpruceProceduralModel
			{
				SaplingOffsetY = spec["saplingOffsetY"]!.GetValue<int>(),
				Variants = spec["variants"]!.AsObject()
					.Select(kv => (kv.Key, kv.Value!.GetValue<int>()))
					.ToList(),
				Normal = normal,
				Vine = vine,
				Giant = giant,
				PodzolCells = podzol,
			};
		}
	}
	/// density per layer, emit cells with probability weight/100 * latent (weight>=80 core
	/// deterministic), then drop cells with no 26-neighbor in the trunk, their own layer or
	/// an adjacent layer — the canopy stays connected the way BDS's is.</summary>
	internal static class ProfileCanopy
	{
		public static List<(int X, int Y, int Z)> Build(ITreeRng rng, IReadOnlyList<(int Delta, List<(int X, int Z, int Weight)> Cells)> layers,
			int trunkTop, int offset, IReadOnlyCollection<(int X, int Y, int Z)> trunk)
		{
			var placed = new List<(int X, int Y, int Z)>();
			foreach (var (delta, profileCells) in layers)
			{
				double latent = 0.6 + rng.NextDouble() * 0.8;
				int wy = trunkTop + offset + delta;
				foreach (var (dx, dz, weight) in profileCells)
				{
					if (weight >= 80 || rng.NextDouble() < Math.Min(1.0, weight / 100.0 * latent))
					{
						placed.Add((dx, wy, dz));
					}
				}
			}
			return Connect(placed, trunk);
		}

		/// <summary>Connectivity (plan §4.5 step 4): a leaf stays iff its connected component
		/// (26-adjacency) touches the trunk. A "touches a surviving neighbor" rule can keep a
		/// mutually-adjacent pair with no path to the tree — the sparse giant-spruce skirt
		/// edges made exactly such pairs survive, isolated in the final canopy. Cells that
		/// fall INSIDE the trunk footprint are dropped too: the placer lays logs first and a
		/// leaf at a trunk position would overwrite the top log (measured: the giant's
		/// whole-tree height came out one short).</summary>
		public static List<(int X, int Y, int Z)> Connect(List<(int X, int Y, int Z)> placed, IReadOnlyCollection<(int X, int Y, int Z)> trunk)
		{
			var trunkSet = trunk.ToHashSet();
			var placedSet = placed.ToHashSet();
			var visited = new HashSet<(int X, int Y, int Z)>();
			var kept = new List<(int X, int Y, int Z)>();
			foreach (var start in placed)
			{
				if (trunkSet.Contains(start)) continue;
				if (!visited.Add(start)) continue;
				var component = new List<(int X, int Y, int Z)>();
				var queue = new Queue<(int X, int Y, int Z)>();
				queue.Enqueue(start);
				bool touchesTrunk = false;
				while (queue.Count > 0)
				{
					var cur = queue.Dequeue();
					component.Add(cur);
					for (int dx = -1; dx <= 1; dx++)
					for (int dy = -1; dy <= 1; dy++)
					for (int dz = -1; dz <= 1; dz++)
					{
						if (dx == 0 && dy == 0 && dz == 0) continue;
						var neighbor = (cur.X + dx, cur.Y + dy, cur.Z + dz);
						if (trunkSet.Contains(neighbor))
						{
							touchesTrunk = true;
						}
						else if (placedSet.Contains(neighbor) && visited.Add(neighbor))
						{
							queue.Enqueue(neighbor);
						}
					}
				}
			if (touchesTrunk) kept.AddRange(component);
			}
			return kept;
		}
	}

	/// <summary>
	///     Birch (plan §5.1): trunk height from the PMF (exact profile bucket), trunk column at
	///     rel 0..H-1 with the dirt conversion of the support block, canopy from the shared
	///     profile machinery. No branches, no vines.
	/// </summary>
	public class ProceduralBirchTreeGenerator : ProceduralTreeGenerator
	{
		private static readonly ProfileModel Model = (ProfileModel) ProceduralTreeParams.For("birch")
			?? throw new InvalidOperationException("no procedural birch model embedded");

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng)
		{
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int height = Model.SampleHeight(rng);
			int trunkTop = height - 1;

			// Trunk at rel 0..H-1 (BDS starts it at the sapling cell) and the dirt conversion
			// of the support block below (grass -> dirt in the captures, never a log).
			for (int y = 0; y < height; y++)
				cells.Add((0, y + Model.SaplingOffsetY, 0, "birch_log"));
			cells.Add((0, -1, 0, "dirt"));

			if (!Model.LayerProfiles.TryGetValue(height, out var layers)) return cells;

			var trunk = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();
			cells.AddRange(ProfileCanopy.Build(rng, layers, trunkTop, Model.SaplingOffsetY, trunk)
				.Select(c => (c.X, c.Y, c.Z, "birch_leaves")));
			return cells;
		}
	}

	/// <summary>
	///     Oak (plan §5.2): the variant family — normal (~86%) and vine (~7%) use the shared
	///     layer-profile machinery (the vine variant adds the deterministic trunk-face vines
	///     at the low layers); large (~6.5%) draws a whole-structure template per column
	///     height and rotates it 4-fold.
	/// </summary>
	public class ProceduralOakTreeGenerator : ProceduralTreeGenerator
	{
		private static readonly OakProceduralModel Model = (OakProceduralModel) ProceduralTreeParams.For("oak")
			?? throw new InvalidOperationException("no procedural oak model embedded");

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng)
		{
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int offset = Model.SaplingOffsetY;
			string variant = Model.SampleVariant(rng);

			if (variant == "large" && Model.Large != null)
			{
				BuildLarge(rng, cells, offset);
				return cells;
			}

			ProfileModel profile = variant == "vine" && Model.Vine != null ? Model.Vine : Model.Normal;
			int height = profile.SampleHeight(rng);
			int trunkTop = height - 1;

			// Trunk at rel 0..H-1 and the dirt conversion of the support block.
			for (int y = 0; y < height; y++)
				cells.Add((0, y + offset, 0, "oak_log"));
			cells.Add((0, -1, 0, "dirt"));

			if (profile.LayerProfiles.TryGetValue(height, out var layers))
			{
				var trunk = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();
				cells.AddRange(ProfileCanopy.Build(rng, layers, trunkTop, offset, trunk)
					.Select(c => (c.X, c.Y, c.Z, "oak_leaves")));
			}

			// Vine variant: trunk-face vines, deterministic pattern (all four faces at the low
			// layers, the direction bits pointing at the trunk).
			if (variant == "vine" && Model.VineProfile != null && Model.VineProfile.VineProfiles.TryGetValue(height, out var vineLayers))
			{
				foreach (var (delta, vineCells) in vineLayers)
				{
					int wy = trunkTop + offset + delta;
					foreach (var (dx, dz, bits, weight) in vineCells)
					{
						if (weight >= 50 || rng.NextDouble() < weight / 100.0)
							cells.Add((dx, wy, dz, $"vine:{bits}"));
					}
				}
			}
			return cells;
		}

		private void BuildLarge(ITreeRng rng, List<(int X, int Y, int Z, string Block)> cells, int offset)
		{
			var m = Model.Large!;
			int height = m.SampleHeight(rng);
			int trunkTop = height - 1;
			for (int y = 0; y < height; y++)
				cells.Add((0, y + offset, 0, "oak_log"));
			cells.Add((0, -1, 0, "dirt"));

			if (!m.PerHeight.TryGetValue(height, out var hm)) return;

			// The dense canopy blob (the shared profile machinery — the large canopy is a
			// dense blob, the profile works).
			var trunk = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();
			cells.AddRange(ProfileCanopy.Build(rng, hm.Canopy, trunkTop, offset, trunk)
				.Select(c => (c.X, c.Y, c.Z, "oak_leaves")));

			// The branch grammar: sample the count, then the joint (attachment delta,
			// azimuth octant, length, elevation) tuples; emit the axis-aligned paths.
			int count = DrawWeighted(rng, hm.BranchCountPmf.Select(p => (p.Count, p.Weight)).ToList());
			for (int i = 0; i < count; i++)
			{
				var paths = hm.BranchPaths;
				int total = paths.Sum(p => p.Weight);
				int roll = rng.Next(total);
				var (delta, azimuth, length, elevation, _) = paths[0];
				foreach (var p in paths)
				{
					if (roll < p.Weight)
					{
						(delta, azimuth, length, elevation, _) = p;
						break;
					}
					roll -= p.Weight;
				}

				// Octant -> axis-aligned step direction (the even octants are the axes; the
				// odd ones step diagonally, which the captured paths do at their starts).
				double angle = azimuth * Math.PI / 4;
				int dirX = (int) Math.Round(Math.Sin(angle));
				int dirZ = (int) Math.Round(Math.Cos(angle));
				if (dirX == 0 && dirZ == 0) dirZ = 1;
				int y0 = trunkTop + offset + delta;
				for (int t = 1; t <= length; t++)
				{
					int x = dirX * t;
					int z = dirZ * t;
					int y = y0 + (elevation * t + length / 2) / length;
					string block = Math.Abs(dirX) > 0 ? "oak_log:x" : "oak_log:z";
					cells.Add((x, y, z, block));
				}
			}
		}

		private static int DrawWeighted(ITreeRng rng, List<(int Item, int Weight)> weighted)
		{
			int total = weighted.Sum(w => w.Weight);
			int roll = rng.Next(total);
			foreach (var (item, weight) in weighted)
			{
				if (roll < weight) return item;
				roll -= weight;
			}
			return weighted[^1].Item;
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
	}

	/// <summary>Shared variant-family generator (spruce M3, jungle M4, ...): the canopy model
	/// (grammar or profile) per variant, the 1×1/2×2 trunk footprint by variant, the dirt
	/// conversion under the footprint, and the vine pass. The UNFORCED draw (a lone sapling)
	/// excludes the giant — it grows EXCLUSIVELY from a complete 2×2 patch (vanilla and the
	/// captures agree); the patch path forces it.</summary>
	public abstract class ProceduralVariantTreeGenerator : ProceduralTreeGenerator
	{
		protected abstract SpruceProceduralModel Model { get; }
		protected abstract string Wood { get; }

		/// <summary>Forces a variant: the 2×2 patch path always grows the giant (a patch is a
		/// giant by definition; the random variant draw would produce a 1×1 tree on a patch).</summary>
		public string? ForceVariant { get; set; }

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng)
		{
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int offset = Model.SaplingOffsetY;
			string variant = ForceVariant ?? SampleLoneVariant(rng);

			bool big = variant == "giant" && Model.Giant != null;
			SpruceCanopyModel canopy = variant switch
			{
				"vine" when Model.Vine != null => Model.Vine,
				"giant" when Model.Giant != null => Model.Giant,
				_ => Model.Normal,
			};
			int height = canopy.SampleHeight(rng);
			int trunkTop = height - 1;
			int footprint = big ? 2 : 1;

			// Trunk footprint x height, and the dirt conversion under every footprint cell.
			// The 2x2 giants get their per-column heights from BigColumnHeight: the SPRUCE
			// has the NW column ONE block taller than the other three (measured in the
			// captures: 295/295 giants at (0,0)+1, the BDS spire that carries the tip leaf);
			// the jungle mega keeps all four equal (unverified, so unchanged).
			if (big)
			{
				for (int dx = 0; dx < footprint; dx++)
				for (int dz = 0; dz < footprint; dz++)
				{
					int h = BigColumnHeight(dx, dz, height);
					for (int y = 0; y < h; y++)
						cells.Add((dx, y + offset, dz, Wood + "_log"));
				}
			}
			else
			{
				for (int y = 0; y < height; y++)
				for (int dx = 0; dx < footprint; dx++)
				for (int dz = 0; dz < footprint; dz++)
					cells.Add((dx, y + offset, dz, Wood + "_log"));
			}
			if (big)
			{
				AddGiantGround(rng, cells);
			}
			else
			{
				for (int dx = 0; dx < footprint; dx++)
				for (int dz = 0; dz < footprint; dz++)
					cells.Add((dx, -1, dz, "dirt"));
			}

			var trunk = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();
			cells.AddRange(canopy.BuildCanopy(rng, height, trunkTop, offset, trunk)
				.Select(c => (c.X, c.Y, c.Z, Wood + "_leaves")));

			// Vine variant: trunk-face vines from the fitted profile (the frequency map
			// carries the variance).
			if (variant == "vine" && canopy.VineProfile != null && canopy.VineProfile.VineProfiles.TryGetValue(height, out var vineLayers))
			{
				foreach (var (delta, vineCells) in vineLayers)
				{
					int wy = trunkTop + offset + delta;
					foreach (var (dx, dz, bits, weight) in vineCells)
					{
						if (weight >= 50 || rng.NextDouble() < weight / 100.0)
							cells.Add((dx, wy, dz, $"vine:{bits}"));
					}
				}
			}
			return cells;
		}

		/// <summary>The giant's ground conversion: dirt under the 2×2 footprint by default
		/// (the mega jungle: the wiki says "always generates with dirt under its trunk").
		/// The giant SPRUCE overrides it — BDS converts the ground to PODZOL in an irregular
		/// blob fitted from the captures (the map carries the per-cell occupancy).</summary>
		protected virtual void AddGiantGround(ITreeRng rng, List<(int X, int Y, int Z, string Block)> cells)
		{
			for (int dx = 0; dx < 2; dx++)
			for (int dz = 0; dz < 2; dz++)
				cells.Add((dx, -1, dz, "dirt"));
		}

		/// <summary>The per-column trunk height of a giant's 2×2 footprint. The base keeps
		/// all four columns at H; the giant SPRUCE raises the NW (0,0) column by one (the
		/// BDS spire, measured 295/295 in the captures).</summary>
		protected virtual int BigColumnHeight(int dx, int dz, int height) => height;

		private string SampleLoneVariant(ITreeRng rng)
		{
			// The giant is excluded: it grows only from a complete 2x2 patch (ForceVariant).
			// Renormalize the remaining variants (normal + vine).
			var drawable = Model.Variants.Where(v => v.Variant != "giant").ToList();
			int total = drawable.Sum(v => v.Weight);
			int roll = rng.Next(total);
			foreach (var (variant, weight) in drawable)
			{
				if (roll < weight) return variant;
				roll -= weight;
			}
			return drawable[^1].Variant;
		}
	}

	/// <summary>
	///     Spruce (plan §5.3, M3): the variant family — normal (~59%) and vine (~5%) grow a
	///     1×1 trunk; giant (~36%) grows a 2×2 trunk from a 2×2 sapling patch. The canopy is
	///     the correlated layered cone (the canopy grammar). Dirt under the whole footprint.
	/// </summary>
	public class ProceduralSpruceTreeGenerator : ProceduralVariantTreeGenerator
	{
		private static readonly SpruceProceduralModel ModelInstance = (SpruceProceduralModel) ProceduralTreeParams.For("spruce")
			?? throw new InvalidOperationException("no procedural spruce model embedded");

		protected override SpruceProceduralModel Model => ModelInstance;
		protected override string Wood => "spruce";

		protected override int BigColumnHeight(int dx, int dz, int height) => dx == 0 && dz == 0 ? height : height - 1;

		protected override void AddGiantGround(ITreeRng rng, List<(int X, int Y, int Z, string Block)> cells)
		{
			// BDS converts the ground under the giant spruce to PODZOL in an IRREGULAR blob,
			// not a disc: the per-rel-cell occupancy map fitted from 295 captured giants
			// (min 60 / max 105 / mean 79.4 cells; the solid core ~99.7%, the fringe
			// 10-50%). Each cell draws at its fitted probability, so the generated shape
			// carries the same per-tree variance as the captures.
			var map = Model.PodzolCells;
			if (map == null) return;
			foreach (var ((dx, dz), p) in map)
			{
				if (p >= 0.995 || rng.NextDouble() < p)
					cells.Add((dx, -1, dz, "podzol"));
			}
		}
	}

	/// <summary>
	///     Jungle (plan §5.4, M4): the variant family — normal and vine grow a 1×1 trunk with
	///     the birch-shaped flat canopy (dense profile); the giant (mega, 2×2 trunk) grows
	///     only from a 2×2 sapling patch. The canopy models are all profiles (the jungle is
	///     dense, no alternation grammar needed).
	/// </summary>
	public class ProceduralJungleTreeGenerator : ProceduralVariantTreeGenerator
	{
		private static readonly SpruceProceduralModel ModelInstance = (SpruceProceduralModel) ProceduralTreeParams.For("jungle")
			?? throw new InvalidOperationException("no procedural jungle model embedded");

		protected override SpruceProceduralModel Model => ModelInstance;
		protected override string Wood => "jungle";
	}

	/// <summary>
	///     Acacia (plan §5.5, M5): skeleton-first — the trunk column plus the cardinal chains
	///     forking from its top (the joint attachment/endpoint tuples, 1-cell-per-y diagonal
	///     paths, pillar axis y like the trunk), then the FLAT canopy profile anchored at the
	///     whole-tree top (shared profile machinery, per-layer latent density). The chain
	///     count per tree comes from the fit (the single-canopy vs the multi-canopy fork).
	/// </summary>
	public class ProceduralAcaciaTreeGenerator : ProceduralTreeGenerator
	{
		private static readonly AcaciaProceduralModel Model = (AcaciaProceduralModel) ProceduralTreeParams.For("acacia")
			?? throw new InvalidOperationException("no procedural acacia model embedded");

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng)
		{
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int height = Model.SampleHeight(rng);
			int offset = Model.SaplingOffsetY;
			int trunkTop = height - 1;

			// Trunk column at rel 0..H-1 (BDS starts it at the sapling cell) + the dirt
			// conversion of the support block below.
			for (int y = 0; y < height; y++)
				cells.Add((0, y + offset, 0, "acacia_log"));
			cells.Add((0, -1, 0, "dirt"));

			var trunk = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();
			var chainEnds = new List<(int X, int Y, int Z)>();

			// The cardinal chains forking from the trunk top: the joint (attachment,
			// endpoint) tuples sampled per chain, emitted as 1-cell-per-y paths.
			if (Model.ChainCountPmf.TryGetValue(height, out var countPmf))
			{
				int total = countPmf.Sum(p => p.Weight);
				int roll = rng.Next(total);
				int count = countPmf[^1].Count;
				foreach (var (c, w) in countPmf)
				{
					if (roll < w) { count = c; break; }
					roll -= w;
				}
				var topTuples = Model.ChainTopTuples.TryGetValue(height, out var tuplesAtHeight) ? tuplesAtHeight : null;
				if (topTuples != null && topTuples.Count > 0)
				{
					// The FIRST chain is the TOP chain (the captured trees' whole top is
					// the highest branch end) — sample among the tree-max-height tuples.
					int tupleTotal = topTuples.Sum(p => p.Weight);
					int firstEndDy = 0;
					for (int i = 0; i < count; i++)
					{
						int tRoll = rng.Next(tupleTotal);
						var (adx, adz, ady, edx, edz, edy, _) = topTuples[^1];
						foreach (var (tdx, tdz, tdy, tx, tz, ty, w) in topTuples)
						{
							if (tRoll < w) { adx = tdx; adz = tdz; ady = tdy; edx = tx; edz = tz; edy = ty; break; }
							tRoll -= w;
						}
						// The 2-chain patterns (captured): 17% are V-forks with BOTH chains
						// ending at the top (2+0); the rest have the second chain LOWER, at
						// the fitted depth below the top (1:14%, 2:22%, 3:26%, 4:23%, 5:14%
						// — the "1+1" double-chapel trees, whose lower chapel sits mostly
						// 3-5 blocks below the top).
						if (i >= 1)
						{
							if (rng.NextDouble() < 0.17)
							{
								// reuse the first chain's endpoint height (the V-fork)
							}
							else
							{
								int dRoll = rng.Next(100);
								int depth = dRoll < 14 ? 1 : dRoll < 36 ? 2 : dRoll < 62 ? 3 : dRoll < 85 ? 4 : 5;
								edy = firstEndDy - depth;
							}
						}
						else
						{
							firstEndDy = edy;
						}
						int ay = trunkTop + offset + ady;
						int ey = trunkTop + offset + edy;
						int ySpan = ey - ay;
						for (int y = ay; y <= ey; y++)
						{
							int x = adx, z = adz;
							if (ySpan > 0)
							{
								double t = (double) (y - ay) / ySpan;
								x = (int) Math.Round(adx + (edx - adx) * t);
								z = (int) Math.Round(adz + (edz - adz) * t);
							}
						cells.Add((x, y, z, "acacia_log"));
						trunk.Add((x, y, z));
					}
					chainEnds.Add((edx, ey, edz));
				}
				}
			}

			// The chapel lobes: EACH chain endpoint carries its own small flat-topped
			// canopy — the classic acacia "cappella". Two sizes by branch depth: the
			// branches within 2 of the top carry the FULL chapel, the deeper ones a smaller
			// disc-only chapel. A second, lower branch shows a second, lower chapel.
			if (Model.LobeTop.Count > 0)
			{
				var placed = new List<(int X, int Y, int Z)>();
				var logCells = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();
				int wholeTop = logCells.Max(c => c.Y);
				foreach (var (ex, ey, ez) in chainEnds)
				{
					var lobe = wholeTop - ey >= 3 ? Model.LobeDeep : Model.LobeTop;
					if (lobe.Count == 0) continue;
					foreach (var (delta, presence, profileCells) in lobe)
					{
						if (rng.Next(100) >= presence) continue;
						double latent = 0.7 + rng.NextDouble() * 0.6;
						int wy = ey + delta;
						// The short trees' deep layers can reach the GROUND (the rel -1
						// dirt cell) — the captured canopies never sit at the ground.
						if (wy < 0) continue;
						// The captured lobes are SOLID blobs — the 100%-presence layers
						// draw their dense core deterministically (>= 50) plus a strong
						// density boost (the top disc runs ~45 cells in the captures vs
						// ~37 at the plain weights), the sparse underskirt layers draw
						// nearly full when present.
						double boost = presence >= 100 ? 1.5 : 1.6;
						foreach (var (dx, dz, weight) in profileCells)
						{
							if (weight >= 50 && presence >= 100 || rng.NextDouble() < Math.Min(1.0, weight / 100.0 * latent * boost))
								placed.Add((ex + dx, wy, ez + dz));
						}
					}
				}
				cells.AddRange(ProfileCanopy.Connect(placed, logCells).Select(c => (c.X, c.Y, c.Z, "acacia_leaves")));
			}
			return cells;
		}
	}

	/// <summary>
	///     Cherry / dark oak / pale oak (plan §5.6-5.8, M6): the trunk column (1x1 for the
	///     cherry, 2x2 for the dark/pale) plus the cardinal chains climbing outward (joint
	///     attachment/endpoint tuples), then the canopy blob anchored at the whole-tree
	///     top (the shared profile machinery with the per-layer latent density).
	/// </summary>
	public abstract class ProceduralChainBlobTreeGenerator : ProceduralTreeGenerator
	{
		protected abstract string Wood { get; }

		protected sealed override List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng)
		{
			var model = (CherryProceduralModel) ProceduralTreeParams.For(Wood);
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int height = model.SampleHeight(rng);
			int offset = model.SaplingOffsetY;
			int footprint = model.BigFootprint ? 2 : 1;

			// Trunk footprint x height at rel 0..H-1 (BDS starts it at the sapling cell)
			// + the dirt conversion of the support blocks below.
			for (int dx = 0; dx < footprint; dx++)
			for (int dz = 0; dz < footprint; dz++)
			{
				for (int y = 0; y < height; y++)
					cells.Add((dx, y + offset, dz, Wood + "_log"));
				cells.Add((dx, -1, dz, "dirt"));
			}

			var trunk = cells.Where(c => c.Block.EndsWith("_log")).Select(c => (c.X, c.Y, c.Z)).ToList();

			// The BRANCHES: the cherry's stepped arms (the joint axis/attachment/step
			// sequence tuples) or the dark/pale's short horizontal corner chains (the
			// per-(colH, delta) cell profile outside the 2x2 footprint).
			if (model.BigFootprint && model.BranchLayers.TryGetValue(height, out var branchLayers))
			{
				int trunkTop = height - 1 + offset;
				foreach (var (delta, branchCells) in branchLayers)
				{
					int wy = trunkTop + delta;
					foreach (var (dx, dz, weight) in branchCells)
					{
						if (weight >= 80 || rng.NextDouble() < weight / 100.0)
							cells.Add((dx, wy, dz, Wood + "_log"));
					}
				}
			}
			else if (model.ArmCountPmf.TryGetValue(height, out var countPmf))
			{
				int total = countPmf.Sum(p => p.Weight);
				int roll = rng.Next(total);
				int count = countPmf[^1].Count;
				foreach (var (c, w) in countPmf)
				{
					if (roll < w) { count = c; break; }
					roll -= w;
				}
				var tuples = model.ArmTuples.TryGetValue(height, out var tuplesAtHeight) ? tuplesAtHeight : null;
				if (tuples != null && tuples.Count > 0)
				{
					int tupleTotal = tuples.Sum(p => p.Weight);
					for (int i = 0; i < count; i++)
					{
						int tRoll = rng.Next(tupleTotal);
						var (axis, adx, adz, ady, steps, _) = tuples[^1];
						foreach (var (ta, tdx, tdz, tdy, ts, w) in tuples)
						{
							if (tRoll < w) { axis = ta; adx = tdx; adz = tdz; ady = tdy; steps = ts; break; }
							tRoll -= w;
						}
						int ax = axis == 1 || axis == -1 ? axis : 0;
						int az = axis == 2 || axis == -2 ? axis / 2 : 0;
						int x = adx, y = height - 1 + offset + ady, z = adz;
						cells.Add((x, y, z, Wood + "_log"));
						trunk.Add((x, y, z));
						foreach (char step in steps)
						{
							switch (step)
							{
								case 'D':
									x += ax;
									z += az;
									y += 1;
									break;
								case 'V':
									y += 1;
									break;
								case 'H':
									x += ax;
									z += az;
									break;
								default:
									y += 1; // the rare in-plane steps fall back to vertical
									break;
							}
							cells.Add((x, y, z, Wood + "_log"));
							trunk.Add((x, y, z));
						}
					}
				}
			}

			// The canopy blob anchored at the whole-tree top (the chains' tops included)
			// AND the branch centroid (the captured canopy sits off-center — the arms
			// reach one way). The canopy buckets are keyed by the WHOLE-tree top.
			int wholeTop = cells.Where(c => c.Block.EndsWith("_log")).Max(c => c.Y);
			if (model.Canopy.TryGetValue(wholeTop, out var layers))
			{
				var branchSet = cells.Where(c => c.Block.EndsWith("_log") && (c.X < 0 || c.X > 1 || c.Z < 0 || c.Z > 1)).Select(c => (c.X, c.Z)).ToList();
				double centerX = branchSet.Count > 0 ? branchSet.Average(c => c.X) : 0;
				double centerZ = branchSet.Count > 0 ? branchSet.Average(c => c.Z) : 0;
				var placed = new List<(int X, int Y, int Z)>();
				foreach (var (delta, profileCells) in layers)
				{
					double latent = 0.6 + rng.NextDouble() * 0.8;
					int wy = wholeTop + delta;
					foreach (var (dx, dz, weight) in profileCells)
					{
						if (weight >= 80 || rng.NextDouble() < Math.Min(1.0, weight / 100.0 * latent))
							placed.Add(((int) Math.Round(centerX + dx), wy, (int) Math.Round(centerZ + dz)));
					}
				}
				cells.AddRange(ProfileCanopy.Connect(placed, trunk).Select(c => (c.X, c.Y, c.Z, Wood + "_leaves")));
			}
			return cells;
		}
	}

	/// <summary>
	///     Cherry (plan §5.6, M6): 1x1 trunk, big rounded canopy blob.
	/// </summary>
	public class ProceduralCherryTreeGenerator : ProceduralChainBlobTreeGenerator
	{
		protected override string Wood => "cherry";
	}

	/// <summary>
	///     Dark oak (plan §5.7, M6): 2x2 trunk, corner chains, flat canopy.
	/// </summary>
	public class ProceduralDarkOakTreeGenerator : ProceduralChainBlobTreeGenerator
	{
		protected override string Wood => "dark_oak";
	}

	/// <summary>
	///     Pale oak (plan §5.8, M6): 2x2 trunk, corner chains, flat canopy.
	/// </summary>
	public class ProceduralPaleOakTreeGenerator : ProceduralChainBlobTreeGenerator
	{
		protected override string Wood => "pale_oak";
	}

	/// <summary>
	///     Mangrove (plan §5.9, M7): the roots (the ring + the columns at the rel -5..0,
	///     SaplingOffsetY +1 — the roots start AT the propagule cell), the trunk column,
	///     the aerial chains (joint attach/endpoint tuples), and the skirt canopy anchored
	///     at the whole-tree top. The vines and the hanging propagules are M8.
	/// </summary>
	public class ProceduralMangroveTreeGenerator : ProceduralTreeGenerator
	{
		private static readonly MangroveProceduralModel Model = (MangroveProceduralModel) ProceduralTreeParams.For("mangrove")
			?? throw new InvalidOperationException("no procedural mangrove model embedded");

		protected override List<(int X, int Y, int Z, string Block)> BuildShape(ITreeRng rng)
		{
			var cells = new List<(int X, int Y, int Z, string Block)>();
			int height = Model.SampleHeight(rng);
			int offset = Model.SaplingOffsetY;
			int trunkTop = height - 1 + offset;

			// The roots: the global per-delta profile at the rel -5..0 (the dataset's
			// baseCell-relative deltas shifted by the offset, so the ring sits AT the
			// trunk base and the columns run down from the propagule level). The deep
			// cells below the rel -2 are skipped: the flat test worlds put the bedrock
			// three below the surface (real worlds have dirt there; the captured roots
			// reach the rel -5).
			foreach (var (delta, rootCells) in Model.Roots)
			{
				int wy = delta + offset;
				if (wy < -2) continue;
				foreach (var (dx, dz, weight) in rootCells)
				{
					if (weight >= 80 || rng.NextDouble() < weight / 100.0)
						cells.Add((dx, wy, dz, "mangrove_roots"));
				}
			}

			// The trunk column at the rel offset..H-1+offset (the roots start at the
			// propagule cell below it).
			for (int y = 0; y < height; y++)
				cells.Add((0, y + offset, 0, "mangrove_log"));

			var trunk = cells.Where(c => c.Block.EndsWith("_log") || c.Block.EndsWith("_roots")).Select(c => (c.X, c.Y, c.Z)).ToList();

			// The aerial chains: the joint (attachment, endpoint) tuples sampled per
			// chain, 1-cell-per-y diagonal paths.
			if (Model.ChainCountPmf.TryGetValue(height, out var countPmf))
			{
				int total = countPmf.Sum(p => p.Weight);
				int roll = rng.Next(total);
				int count = countPmf[^1].Count;
				foreach (var (c, w) in countPmf)
				{
					if (roll < w) { count = c; break; }
					roll -= w;
				}
				var tuples = Model.ChainTuples.TryGetValue(height, out var tuplesAtHeight) ? tuplesAtHeight : null;
				if (tuples != null && tuples.Count > 0)
				{
					int tupleTotal = tuples.Sum(p => p.Weight);
					for (int i = 0; i < count; i++)
					{
						int tRoll = rng.Next(tupleTotal);
						var (adx, adz, ady, edx, edz, edy, _) = tuples[^1];
						foreach (var (tdx, tdz, tdy, tx, tz, ty, w) in tuples)
						{
							if (tRoll < w) { adx = tdx; adz = tdz; ady = tdy; edx = tx; edz = tz; edy = ty; break; }
							tRoll -= w;
						}
						int ay = trunkTop + ady;
						int ey = trunkTop + edy;
						int ySpan = ey - ay;
						for (int y = ay; y <= ey; y++)
						{
							int x = adx, z = adz;
							if (ySpan > 0)
							{
								double t = (double) (y - ay) / ySpan;
								x = (int) Math.Round(adx + (edx - adx) * t);
								z = (int) Math.Round(adz + (edz - adz) * t);
							}
							cells.Add((x, y, z, "mangrove_log"));
							trunk.Add((x, y, z));
						}
					}
				}
			}

			// The skirt canopy anchored at the whole-tree top (the chains' tops included).
			// The short trees' deep skirt layers can reach the GROUND — skip below the
			// propagule cell (the captured canopies never sit in the ground). A wholeTop
			// without its own bucket falls back to the nearest taller one.
			int wholeTop = cells.Where(c => c.Block.EndsWith("_log")).Max(c => c.Y);
			if (!Model.Canopy.TryGetValue(wholeTop, out var layers))
			{
				int fallback = Model.Canopy.Keys.Where(k => k >= wholeTop).DefaultIfEmpty(Model.Canopy.Keys.Max()).Min();
				layers = Model.Canopy[fallback];
			}
			{
				var placed = new List<(int X, int Y, int Z)>();
				foreach (var (delta, profileCells) in layers)
				{
					double latent = 0.6 + rng.NextDouble() * 0.8;
					int wy = wholeTop + delta;
					if (wy < 0) continue;
					foreach (var (dx, dz, weight) in profileCells)
					{
						if (weight >= 80 || rng.NextDouble() < Math.Min(1.0, weight / 100.0 * latent))
							placed.Add((dx, wy, dz));
					}
				}
				cells.AddRange(ProfileCanopy.Connect(placed, trunk).Select(c => (c.X, c.Y, c.Z, "mangrove_leaves")));
			}
			return cells;
		}
	}
}





