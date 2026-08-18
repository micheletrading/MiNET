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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Test.Worlds
{
	/// <summary>
	///     Anvil load-path benchmarks against a real world on disk. The historical bar, from the
	///     2015 MinetAnvilTest (deleted at "NET Core has arrived", 3bc2878b), was 100 ticks = 10us
	///     per column re-read and sub-1ms region saves; the live server measures 3.4-5.2 MS per
	///     column through the full pre-warm path today. These exist to split that number apart.
	/// </summary>
	[TestClass]
	public class AnvilLoadPerfTests
	{
		private const string WorldPath = @"C:\Development\github\MiNET\worlds\Falcon's Rock v2.0.0";

		[TestMethod]
		[Ignore("Benchmark, not a test: loads a full region from disk with no assertions. Run manually when measuring anvil load performance.")]
		public void Anvil_region_sweep_perf_test()
		{
			var provider = new AnvilWorldProvider(WorldPath);
			provider.Initialize();

			// Region 0,0: the spawn region on this world. Fresh provider, so every column is a
			// real parse, not a cache hit.
			long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			var sw = Stopwatch.StartNew();

			int loaded = 0, missing = 0;
			var perColumn = new List<double>(1024);
			var one = new Stopwatch();
			for (int x = 0; x < 32; x++)
			{
				for (int z = 0; z < 32; z++)
				{
					one.Restart();
					ChunkColumn column = provider.GenerateChunkColumn(new ChunkCoordinates(x, z));
					one.Stop();
					perColumn.Add(one.Elapsed.TotalMilliseconds);
					if (column == null) missing++;
					else loaded++;
				}
			}

			sw.Stop();
			long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

			perColumn.Sort();
			Console.WriteLine($"Loaded {loaded} columns ({missing} missing) in {sw.ElapsedMilliseconds}ms");
			Console.WriteLine($"Per column: {sw.Elapsed.TotalMilliseconds / loaded:F3}ms avg, p50={perColumn[perColumn.Count / 2]:F3} p90={perColumn[(int) (perColumn.Count * 0.9)]:F3} max={perColumn[^1]:F3}");
			Console.WriteLine($"Allocated: {allocated / loaded / 1024.0:F1}KB/column, {allocated / 1024.0 / 1024.0:F1}MB total");
		}

		[TestMethod]
		[Ignore("Benchmark, not a test: loads a full region three times with pieces of the cell loop disabled. Run manually when attributing cell-loop cost.")]
		public void Anvil_cell_cost_distribution_perf_test()
		{
			// Same region swept under three configurations; the deltas attribute the sections
			// phase without per-cell timers. Two passes per configuration, minimum taken, because
			// run variance is bigger than the smallest share measured.
			double Run()
			{
				var provider = new AnvilWorldProvider(WorldPath);
				provider.Initialize();

				AnvilWorldProvider.ProfileLoad = true;
				AnvilWorldProvider.ProfileSectionTicks = 0;
				AnvilWorldProvider.ProfileColumns = 0;

				for (int x = 0; x < 32; x++)
				{
					for (int z = 0; z < 32; z++)
					{
						provider.GenerateChunkColumn(new ChunkCoordinates(x, z));
					}
				}

				AnvilWorldProvider.ProfileLoad = false;
				return AnvilWorldProvider.ProfileSectionTicks * (1_000_000.0 / Stopwatch.Frequency) / AnvilWorldProvider.ProfileColumns;
			}

			// Warm BlockFactory and table rows off the clock.
			var warm = new AnvilWorldProvider(WorldPath);
			warm.Initialize();
			Assert.IsNotNull(warm.GenerateChunkColumn(new ChunkCoordinates(26, 24)));

			double full = Math.Min(Run(), Run());

			AnvilWorldProvider.ProfileSkipLightWrites = true;
			double noLights = Math.Min(Run(), Run());

			AnvilWorldProvider.ProfileSkipBlockStores = true;
			double scanOnly = Math.Min(Run(), Run());

			AnvilWorldProvider.ProfileSkipLightWrites = false;
			AnvilWorldProvider.ProfileSkipBlockStores = false;

			Console.WriteLine($"Sections phase per column (min of 2 runs each):");
			Console.WriteLine($"  full loop:                 {full,8:F1}us");
			Console.WriteLine($"  without light writes:      {noLights,8:F1}us  -> lights cost {full - noLights:F1}us ({(full - noLights) / full:P1})");
			Console.WriteLine($"  scan+table only:           {scanOnly,8:F1}us  -> block stores cost {noLights - scanOnly:F1}us ({(noLights - scanOnly) / full:P1})");
			Console.WriteLine($"  residual (scan/init/nbt-lookups):      {scanOnly / full:P1}");
		}

		[TestMethod]
		[Ignore("Benchmark, not a test: loads a full region with phase accumulators on. Run manually when attributing anvil load cost.")]
		public void Anvil_phase_split_perf_test()
		{
			var provider = new AnvilWorldProvider(WorldPath);
			provider.Initialize();

			// Warm BlockFactory and the conversion table rows off the clock, so the phases
			// measure the steady state and not one-time process init.
			Assert.IsNotNull(provider.GenerateChunkColumn(new ChunkCoordinates(26, 24)));

			AnvilWorldProvider.ProfileLoad = true;
			AnvilWorldProvider.ProfileNbtTicks = 0;
			AnvilWorldProvider.ProfileSectionTicks = 0;
			AnvilWorldProvider.ProfileBlockEntityTicks = 0;
			AnvilWorldProvider.ProfileTailTicks = 0;
			AnvilWorldProvider.ProfileColumns = 0;
			try
			{
				for (int x = 0; x < 32; x++)
				{
					for (int z = 0; z < 32; z++)
					{
						provider.GenerateChunkColumn(new ChunkCoordinates(x, z));
					}
				}
			}
			finally
			{
				AnvilWorldProvider.ProfileLoad = false;
			}

			long columns = AnvilWorldProvider.ProfileColumns;
			double perTick = 1_000_000.0 / Stopwatch.Frequency; // us per tick
			double nbt = AnvilWorldProvider.ProfileNbtTicks * perTick / columns;
			double sections = AnvilWorldProvider.ProfileSectionTicks * perTick / columns;
			double blockEntities = AnvilWorldProvider.ProfileBlockEntityTicks * perTick / columns;
			double tail = AnvilWorldProvider.ProfileTailTicks * perTick / columns;
			double total = nbt + sections + blockEntities + tail;

			Console.WriteLine($"Phase split over {columns} columns ({total:F0}us total per column):");
			Console.WriteLine($"  nbt (open+inflate+tree):      {nbt,8:F1}us  {nbt / total,6:P1}");
			Console.WriteLine($"  sections (init+cell loop):    {sections,8:F1}us  {sections / total,6:P1}");
			Console.WriteLine($"  block entities:               {blockEntities,8:F1}us  {blockEntities / total,6:P1}");
			Console.WriteLine($"  tail (heights/lights/return): {tail,8:F1}us  {tail / total,6:P1}");
		}

		[TestMethod]
		[Ignore("Benchmark, not a test: decompresses one chunk's NBT in a loop with no assertions. Run manually when measuring anvil load performance.")]
		public void Anvil_nbt_only_perf_test()
		{
			// The same chunk the repeat test parses, but stopping after the NBT tree is built:
			// region file open, header, seek, ZLib inflate, fNbt tag tree. The difference between
			// this and the full parse is the per-block conversion loop.
			var coordinates = new ChunkCoordinates(26, 24);
			string filePath = System.IO.Path.Combine(WorldPath, "region", $"r.{coordinates.X >> 5}.{coordinates.Z >> 5}.mca");

			const int iterations = 200;
			long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			var sw = Stopwatch.StartNew();

			for (int i = 0; i < iterations; i++)
			{
				using var regionFile = System.IO.File.OpenRead(filePath);
				var header = new byte[8192];
				regionFile.Read(header, 0, 8192);

				int xi = coordinates.X % 32;
				int zi = coordinates.Z % 32;
				int tableOffset = (xi + zi * 32) * 4;
				int offset = ((header[tableOffset] << 16) | (header[tableOffset + 1] << 8) | header[tableOffset + 2]) << 12;

				regionFile.Seek(offset + 5, System.IO.SeekOrigin.Begin);
				var nbt = new fNbt.NbtFile();
				nbt.LoadFromStream(regionFile, fNbt.NbtCompression.ZLib);
			}

			sw.Stop();
			long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

			Console.WriteLine($"NBT-only x{iterations} in {sw.ElapsedMilliseconds}ms");
			Console.WriteLine($"Per load: {sw.Elapsed.TotalMicroseconds / iterations:F0}us, {allocated / iterations / 1024.0:F1}KB allocated");
		}

		[TestMethod]
		[Ignore("Benchmark, not a test: re-parses one chunk in a loop with no assertions. Run manually when measuring anvil parse cost.")]
		public void Anvil_single_column_repeat_perf_test()
		{
			var provider = new AnvilWorldProvider(WorldPath);
			provider.Initialize();

			// The spawn chunk. GetChunk is the raw parse path with no column cache in front, so
			// after the first iteration this measures pure parse + convert with a warm OS file
			// cache - the same shape as the 2015 test that asserted 10us here.
			var coordinates = new ChunkCoordinates(26, 24);
			ChunkColumn warmup = provider.GetChunk(coordinates, WorldPath, null);
			Assert.IsNotNull(warmup, "spawn chunk missing - wrong world path?");

			const int iterations = 200;
			long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
			var sw = Stopwatch.StartNew();

			for (int i = 0; i < iterations; i++)
			{
				provider.GetChunk(coordinates, WorldPath, null);
			}

			sw.Stop();
			long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

			Console.WriteLine($"Re-parsed 1 column x{iterations} in {sw.ElapsedMilliseconds}ms");
			Console.WriteLine($"Per parse: {sw.Elapsed.TotalMicroseconds / iterations:F0}us, {allocated / iterations / 1024.0:F1}KB allocated");
		}
	}
}
