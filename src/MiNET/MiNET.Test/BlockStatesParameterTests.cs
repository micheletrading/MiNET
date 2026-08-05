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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Plugins;
using MiNET.Utils;

namespace MiNET.Test
{
	[TestClass]
	public class BlockStatesParameterTests
	{
		// The blockStates argument a player types on /setblock and /fill:
		// ["state"=value,"state"=value], names always quoted, values quoted for strings and bare for
		// booleans and numbers. A value's syntax is the only clue to which state type it is, so the
		// parse has to produce the right one or the states will not match a palette entry.

		[TestMethod]
		public void AQuotedValueIsAStringState()
		{
			BlockStates states = BlockStates.Parse("[\"pillar_axis\"=\"x\"]");

			var state = (BlockStateString) states.States[0];
			Assert.AreEqual("pillar_axis", state.Name);
			Assert.AreEqual("x", state.Value);
		}

		[TestMethod]
		public void ABareBooleanIsAByteState()
		{
			BlockStates states = BlockStates.Parse("[\"persistent_bit\"=true,\"update_bit\"=false]");

			Assert.AreEqual(1, ((BlockStateByte) states.States[0]).Value);
			Assert.AreEqual(0, ((BlockStateByte) states.States[1]).Value);
		}

		[TestMethod]
		public void ABareNumberIsAnIntState()
		{
			BlockStates states = BlockStates.Parse("[\"growth\"=3]");

			Assert.AreEqual(3, ((BlockStateInt) states.States[0]).Value);
		}

		[TestMethod]
		public void SeveralStatesInOneLiteral()
		{
			BlockStates states = BlockStates.Parse("[\"old_leaf_type\"=\"birch\",\"persistent_bit\"=true]");

			Assert.AreEqual(2, states.States.Count);
		}

		// Parsing is only worth anything if what comes out reaches a real palette entry.
		[TestMethod]
		public void TheParsedStatesLandOnTheBlockTheyName()
		{
			Block log = BlockFactory.GetBlockByName("minecraft:oak_log");

			Assert.IsTrue(BlockStates.Parse("[\"pillar_axis\"=\"x\"]").TryApplyTo(log, out string error), error);

			Block expected = BlockFactory.GetBlockByName("minecraft:oak_log");
			((OakLog) expected).PillarAxis = "x";

			Assert.AreEqual(expected.GetRuntimeId(), log.GetRuntimeId());
		}

		// SetState ignores what it does not recognise, so without the resolve step a typo places the
		// default block and says nothing.
		[TestMethod]
		public void AStateTheBlockDoesNotHaveIsRefused()
		{
			Block log = BlockFactory.GetBlockByName("minecraft:oak_log");

			Assert.IsFalse(BlockStates.Parse("[\"pilar_axis\"=\"x\"]").TryApplyTo(log, out string error));
			StringAssert.Contains(error, "pilar_axis");
		}

		[TestMethod]
		public void AValueTheStateCannotTakeIsRefused()
		{
			Block log = BlockFactory.GetBlockByName("minecraft:oak_log");

			Assert.IsFalse(BlockStates.Parse("[\"pillar_axis\"=\"sideways\"]").TryApplyTo(log, out string error));
			StringAssert.Contains(error, "pillar_axis");
		}

		[TestMethod]
		public void NothingUsableParsesToNothing()
		{
			Assert.IsNull(BlockStates.Parse(null));
			Assert.IsNull(BlockStates.Parse(""));
			Assert.AreEqual(0, BlockStates.Parse("[]").States.Count);
		}
	}
}
