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
	// A plugin replacing a vanilla block's implementation is asked by name, and asked from
	// GetBlockByName, which every other lookup ends at. That is what makes the replacement hold
	// however the block was reached: an item placing it, a recipe naming it, a world load
	// resolving it from a runtime id.
	[TestClass, DoNotParallelize]
	public class CustomBlockFactoryTests
	{
		private class StoneReplacingFactory : ICustomBlockFactory
		{
			public Block GetBlockByName(string name)
			{
				return name == "minecraft:stone" ? new Dirt() : null;
			}
		}

		[TestInitialize]
		public void Hook()
		{
			BlockFactory.CustomBlockFactory = new StoneReplacingFactory();
		}

		[TestCleanup]
		public void Unhook()
		{
			BlockFactory.CustomBlockFactory = null;
		}

		[TestMethod]
		public void TheReplacementWinsOverTheVanillaClass()
		{
			Assert.AreEqual("minecraft:dirt", BlockFactory.GetBlockByName("minecraft:stone").Name);
		}

		[TestMethod]
		public void TheReplacementHoldsWhateverAskedForTheBlock()
		{
			Assert.AreEqual("minecraft:dirt", BlockFactory.GetBlockByRuntimeId(new Stone().GetRuntimeId()).Name);
			Assert.AreEqual("minecraft:dirt", BlockFactory.GetBlockById(1).Name);
		}

		[TestMethod]
		public void ANameThePluginLeavesAloneIsUntouched()
		{
			Assert.AreEqual("minecraft:cobblestone", BlockFactory.GetBlockByName("minecraft:cobblestone").Name);
		}

		[TestMethod]
		public void AnUnknownNameIsStillNull()
		{
			Assert.IsNull(BlockFactory.GetBlockByName("testplugin:nothing"));
		}
	}
}
