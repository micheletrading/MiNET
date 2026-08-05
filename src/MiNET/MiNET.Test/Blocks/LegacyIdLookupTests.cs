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
	public class LegacyIdLookupTests
	{
		// Asking for a block by legacy id is one question, so the answer cannot depend on whether
		// the caller passed a data value. The two overloads used to be separate implementations
		// that agreed only when the lookup succeeded, and disagreed on every way it can fail.

		[TestMethod]
		public void PassingNoDataValueIsTheSameAsPassingZero()
		{
			foreach (int blockId in new[] {1, 2, 5, 17, 35, 98})
			{
				Assert.AreEqual(BlockFactory.GetBlockById(blockId, 0).Name, BlockFactory.GetBlockById(blockId).Name, $"legacy id {blockId}");
			}
		}

		// The block a client shows for something it does not know. Unlike a bare Block it is a
		// real palette entry, so it has a runtime id and can be saved and sent.
		[TestMethod]
		public void AnIdTheMapDoesNotCoverIsTheUnknownBlock()
		{
			Assert.AreEqual("minecraft:info_update", BlockFactory.GetBlockById(4000).Name);
			Assert.AreEqual("minecraft:info_update", BlockFactory.GetBlockById(4000, 0).Name);
		}

		// (id << 4) walks off the end of the table long before int does.
		[TestMethod]
		public void AnIdBeyondTheTableIsTheUnknownBlock()
		{
			Assert.AreEqual("minecraft:info_update", BlockFactory.GetBlockById(70000).Name);
			Assert.AreEqual("minecraft:info_update", BlockFactory.GetBlockById(70000, 0).Name);
		}
	}
}
