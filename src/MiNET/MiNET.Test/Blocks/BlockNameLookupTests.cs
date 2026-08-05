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
	public class BlockNameLookupTests
	{
		// Asking which block sits at a runtime id without building one. A block with states holds
		// a runtime id per state, so code asking "is this a portal" has to compare the name: the
		// portal search would otherwise find the portals on one axis and walk past the other.

		[TestMethod]
		public void EveryStateOfABlockAnswersTheSameName()
		{
			var alongX = new Portal {PortalAxis = "x"};
			var alongZ = new Portal {PortalAxis = "z"};

			Assert.AreNotEqual(alongX.GetRuntimeId(), alongZ.GetRuntimeId(), "premise: the axis is a state, so it is a runtime id of its own");
			Assert.AreEqual("minecraft:portal", BlockFactory.GetBlockName(alongX.GetRuntimeId()));
			Assert.AreEqual("minecraft:portal", BlockFactory.GetBlockName(alongZ.GetRuntimeId()));
		}

		[TestMethod]
		public void AnUnknownRuntimeIdHasNoName()
		{
			Assert.IsNull(BlockFactory.GetBlockName(-1));
			Assert.IsNull(BlockFactory.GetBlockName(int.MaxValue));
		}
	}
}
