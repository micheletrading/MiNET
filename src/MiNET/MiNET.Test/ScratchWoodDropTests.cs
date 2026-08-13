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
using MiNET.Items;

namespace MiNET.Tests
{
	[TestClass]
	public class ScratchWoodDropTests
	{
		[TestMethod]
		public void OakLogDropResolves()
		{
			OakLog log = new OakLog();
			var drops = log.GetDrops(new ItemAir());
			Assert.IsNotNull(drops);
			Assert.AreEqual(1, drops.Length, "oak_log should drop exactly one item");
			Assert.IsFalse(drops[0].IsAir, "oak_log drop must not be air");
			Assert.AreEqual("minecraft:oak_log", drops[0].Name);

			Item byName = ItemFactory.GetItemByName("minecraft:oak_log");
			Assert.IsFalse(byName.IsAir, "GetItemByName(oak_log) must not be air");
			Assert.AreEqual("minecraft:oak_log", byName.Name);

			Block resolved = BlockFactory.GetBlockByName("minecraft:oak_log");
			Assert.IsNotNull(resolved, "GetBlockByName(oak_log) must resolve");
		}
	}
}
