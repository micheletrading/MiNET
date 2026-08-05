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

namespace MiNET.Test.Blocks
{
	[TestClass]
	public class LiquidPairTests
	{
		// A liquid is two blocks: the flowing one and the still one, and the physics moves a block
		// between them. That pairing used to be arithmetic on the legacy id, flowing + 1 for still
		// and still - 1 for flowing, which only held while water was 8/9 and lava 10/11. The pair
		// is a property of the block, so each side names its counterpart.

		[TestMethod]
		public void FlowingWater_SettlesToWater()
		{
			Assert.AreEqual("minecraft:water", new FlowingWater().StillCounterpart().Name);
		}

		[TestMethod]
		public void Water_FlowsAsFlowingWater()
		{
			Assert.AreEqual("minecraft:flowing_water", new Water().FlowingCounterpart().Name);
		}

		[TestMethod]
		public void FlowingLava_SettlesToLava()
		{
			Assert.AreEqual("minecraft:lava", new FlowingLava().StillCounterpart().Name);
		}

		[TestMethod]
		public void Lava_FlowsAsFlowingLava()
		{
			Assert.AreEqual("minecraft:flowing_lava", new Lava().FlowingCounterpart().Name);
		}

		// The counterpart has to be the right half of the pair, or the physics loops: a still block
		// that resolves to another still block never settles.
		[TestMethod]
		public void Counterparts_AreTheOppositeKind()
		{
			Assert.IsInstanceOfType(new FlowingWater().StillCounterpart(), typeof(Stationary));
			Assert.IsInstanceOfType(new Water().FlowingCounterpart(), typeof(Flowing));
			Assert.IsInstanceOfType(new FlowingLava().StillCounterpart(), typeof(Stationary));
			Assert.IsInstanceOfType(new Lava().FlowingCounterpart(), typeof(Flowing));
		}
	}
}
