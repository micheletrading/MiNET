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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Utils;

namespace MiNET.Test
{
	[TestClass]
	public class BlockRuntimeIdTests
	{
		// The generated GetRuntimeId computes a block's palette index arithmetically instead of
		// looking the state up, which is only correct while the palette stays a mixed-radix
		// encoding of the state values. That is a property of Mojang's data, not of our code, so
		// it has to be checked against the palette rather than assumed. A future Bedrock drop that
		// reorders states, drops a permutation, or stops emitting a full cross product breaks the
		// arithmetic silently: every block would still return an id, just the wrong one.

		[TestMethod]
		public void GeneratedRuntimeId_MatchesPaletteIndex_ForEveryState()
		{
			var mismatches = new List<string>();
			var unresolvable = new List<string>();

			for (int runtimeId = 0; runtimeId < BlockFactory.BlockPalette.Count; runtimeId++)
			{
				BlockStateContainer expected = BlockFactory.BlockPalette[runtimeId];

				Block block = BlockFactory.GetBlockByName(expected.Name);
				if (block == null)
				{
					if (unresolvable.Count < 20) unresolvable.Add(expected.Name);
					continue;
				}

				block.SetState(expected.States);

				int actual = block.GetRuntimeId();
				if (actual != runtimeId)
				{
					if (mismatches.Count < 20) mismatches.Add($"{expected.Name} [{Describe(expected)}] expected {runtimeId}, got {actual}");
				}
			}

			// Every palette name has a generated class, so name resolution is total. Callers depend
			// on that: without it they need a legacy id to fall back to, and the legacy id is the
			// thing that cannot express a post-flattening block.
			Assert.AreEqual(0, unresolvable.Count, $"palette names with no block class:\n{string.Join("\n", unresolvable.Distinct())}");
			Assert.AreEqual(0, mismatches.Count, $"{mismatches.Count} states resolve to the wrong runtime id:\n{string.Join("\n", mismatches)}");
		}

		// -1 is the contract for "this state has no id". SubChunk.SetBlock turns it into a refusal
		// to write and a loud error, so an out-of-domain value must not quietly produce a
		// neighbouring block's id, which is exactly what unguarded arithmetic would do.
		[TestMethod]
		public void GeneratedRuntimeId_ReturnsMinusOne_ForOutOfDomainState()
		{
			var candle = (BlueCandle) BlockFactory.GetBlockByName("minecraft:blue_candle");
			Assert.IsNotNull(candle);

			candle.Candles = 3;
			Assert.AreNotEqual(-1, candle.GetRuntimeId(), "the top of the domain is a valid state");

			candle.Candles = 4;
			Assert.AreEqual(-1, candle.GetRuntimeId(), "a value past the domain has no palette entry");

			var gate = (FenceGate) BlockFactory.GetBlockByName("minecraft:fence_gate");
			Assert.IsNotNull(gate);

			gate.CardinalDirection = "east";
			Assert.AreNotEqual(-1, gate.GetRuntimeId());

			gate.CardinalDirection = "upwards";
			Assert.AreEqual(-1, gate.GetRuntimeId(), "a value outside the enum has no palette entry");
		}

		// The arithmetic and the lookup have to agree, because classes that write GetState by hand
		// keep the lookup and everything else moved to arithmetic. If the two ever disagree, blocks
		// would land in different places depending on which half of the codebase produced them.
		[TestMethod]
		public void GeneratedRuntimeId_AgreesWithStateLookup()
		{
			foreach (string name in new[]
			{
				"minecraft:oak_planks", "minecraft:blue_candle", "minecraft:fence_gate",
				"minecraft:end_stone_brick_wall", "minecraft:furnace", "minecraft:air"
			})
			{
				foreach (BlockStateContainer state in BlockFactory.BlockPalette.Where(s => s.Name == name))
				{
					Block block = BlockFactory.GetBlockByName(name);
					block.SetState(state.States);

					BlockStateContainer current = block.GetState();
					Assert.IsTrue(BlockFactory.BlockStates.TryGetValue(current, out BlockStateContainer viaLookup),
						$"{name} [{Describe(state)}] rebuilt a state that is not in the palette");
					Assert.AreEqual(viaLookup.RuntimeId, block.GetRuntimeId(),
						$"{name} [{Describe(state)}] arithmetic and lookup disagree");
				}
			}
		}

		private static string Describe(BlockStateContainer state)
		{
			return string.Join(",", state.States.Select(s => $"{s.Name}={StateValue(s)}"));
		}

		private static object StateValue(IBlockState state)
		{
			return state switch
			{
				BlockStateByte b => b.Value,
				BlockStateInt i => i.Value,
				BlockStateString s => s.Value,
				_ => "?"
			};
		}
	}
}
