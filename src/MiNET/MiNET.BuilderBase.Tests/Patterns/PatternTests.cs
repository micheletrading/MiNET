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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Utils.Vectors;

namespace MiNET.BuilderBase.Patterns.Tests
{
	// A pattern names blocks the way the game names them now: minecraft:oak_log[pillar_axis=x].
	// The id and data value it used to take cannot address a modern block, and the two tests that
	// used them were already failing because stone_slab and log stopped being block names.
	//
	// The name and the states are resolved while the pattern is being read, so a typo is refused
	// while the player is typing rather than quietly placing something else a thousand times.

	[TestClass]
	public class PatternTests
	{
		[TestMethod]
		public void An_empty_pattern_is_air()
		{
			var pattern = new Pattern();
			pattern.Deserialize(null, "");

			Assert.AreEqual(1, pattern.BlockList.Count);
			Assert.AreEqual("minecraft:air", pattern.BlockList[0].Block.Name);
		}

		[TestMethod]
		public void A_name_resolves_with_or_without_the_namespace()
		{
			var pattern = new Pattern();
			pattern.Deserialize(null, "stone,minecraft:dirt");

			Assert.AreEqual(2, pattern.BlockList.Count);
			Assert.AreEqual("minecraft:stone", pattern.BlockList[0].Block.Name);
			Assert.AreEqual("minecraft:dirt", pattern.BlockList[1].Block.Name);
		}

		[TestMethod]
		public void Weights_accumulate_in_order()
		{
			var pattern = new Pattern();
			pattern.Deserialize(null, "1%stone,10%dirt");

			Assert.AreEqual(2, pattern.BlockList.Count);

			Assert.AreEqual("minecraft:stone", pattern.BlockList[0].Block.Name);
			Assert.AreEqual(1, pattern.BlockList[0].Weight);
			Assert.AreEqual(1, pattern.BlockList[0].Accumulated);

			Assert.AreEqual("minecraft:dirt", pattern.BlockList[1].Block.Name);
			Assert.AreEqual(10, pattern.BlockList[1].Weight);
			Assert.AreEqual(11, pattern.BlockList[1].Accumulated);
		}

		[TestMethod]
		public void States_are_applied_to_the_block_they_name()
		{
			var pattern = new Pattern();
			pattern.Deserialize(null, "oak_log[pillar_axis=x]");

			var expected = new OakLog {PillarAxis = "x"};
			Assert.AreEqual(expected.GetRuntimeId(), pattern.BlockList[0].Block.GetRuntimeId());
		}

		[TestMethod]
		public void The_same_block_on_different_states_stays_separate()
		{
			var pattern = new Pattern();
			pattern.Deserialize(null, "oak_log[pillar_axis=x],oak_log[pillar_axis=y],oak_log[pillar_axis=z]");

			Assert.AreEqual(3, pattern.BlockList.Count);
			Assert.AreEqual("x", ((OakLog) pattern.BlockList[0].Block).PillarAxis);
			Assert.AreEqual("y", ((OakLog) pattern.BlockList[1].Block).PillarAxis);
			Assert.AreEqual("z", ((OakLog) pattern.BlockList[2].Block).PillarAxis);
		}

		// Every placement is its own block, or moving one would move all of them.
		[TestMethod]
		public void Next_hands_out_a_block_per_position()
		{
			var pattern = new Pattern();
			pattern.Deserialize(null, "stone");

			Block first = pattern.Next(new BlockCoordinates(1, 2, 3));
			Block second = pattern.Next(new BlockCoordinates(4, 5, 6));

			Assert.AreNotSame(first, second);
			Assert.AreEqual(new BlockCoordinates(1, 2, 3), first.Coordinates);
			Assert.AreEqual(new BlockCoordinates(4, 5, 6), second.Coordinates);
		}

		[TestMethod]
		public void A_name_that_is_not_a_block_is_refused()
		{
			var pattern = new Pattern();

			var thrown = Assert.ThrowsExactly<FormatException>(() => pattern.Deserialize(null, "stone,nosuchblock"));
			StringAssert.Contains(thrown.Message, "nosuchblock");
		}

		// The id and data value are retired, so they read as a block name and fail like one.
		[TestMethod]
		public void A_legacy_id_and_data_value_is_refused()
		{
			var pattern = new Pattern();

			Assert.ThrowsExactly<FormatException>(() => pattern.Deserialize(null, "35:14"));
		}

		[TestMethod]
		public void A_state_the_block_does_not_have_is_refused()
		{
			var pattern = new Pattern();

			var thrown = Assert.ThrowsExactly<FormatException>(() => pattern.Deserialize(null, "oak_log[pilar_axis=x]"));
			StringAssert.Contains(thrown.Message, "pilar_axis");
		}
	}
}
