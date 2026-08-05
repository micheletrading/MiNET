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
	public class LightDiffusionTests
	{
		// What the skylight pass subtracts crossing a block: one for the step, plus whatever the
		// block itself filters. Both halves come from the block data, so a block Mojang adds is
		// lit correctly the day its data lands. The table this replaced named five numeric ids,
		// which covered oak through dark oak leaves and nothing added since, so cherry and
		// mangrove leaves let skylight through as if they were open air.

		[TestMethod]
		public void DiffusionIsOneStepPlusWhatTheBlockFilters()
		{
			Assert.AreEqual(1, BlockFactory.GetLightDiffusion(new Air().GetRuntimeId()));
			Assert.AreEqual(1, BlockFactory.GetLightDiffusion(new Glass().GetRuntimeId()));
			Assert.AreEqual(2, BlockFactory.GetLightDiffusion(new Water().GetRuntimeId()));
			Assert.AreEqual(3, BlockFactory.GetLightDiffusion(new FlowingWater().GetRuntimeId()));
			Assert.AreEqual(4, BlockFactory.GetLightDiffusion(new Ice().GetRuntimeId()));
		}

		[TestMethod]
		public void EveryLeafDiffusesTheSame()
		{
			Assert.AreEqual(2, BlockFactory.GetLightDiffusion(new OakLeaves().GetRuntimeId()));
			Assert.AreEqual(2, BlockFactory.GetLightDiffusion(new CherryLeaves().GetRuntimeId()));
			Assert.AreEqual(2, BlockFactory.GetLightDiffusion(new MangroveLeaves().GetRuntimeId()));
		}
	}
}
