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
using MiNET.Worlds;

namespace MiNET.Test.Blocks
{
	[TestClass]
	public class AirTests
	{
		// Air is the most asked question in the server, so it is asked of the runtime id, which is
		// the block's identity in the palette. The numeric block id answers it too, but only by
		// projecting the palette entry onto Bedrock's id map first, and that map is a lookup we do
		// not need to reach a comparison of two integers.

		[TestMethod]
		public void AirIsRecognisedByItsRuntimeId()
		{
			Assert.IsTrue(BlockFactory.IsAir(new Air().GetRuntimeId()));
			Assert.IsFalse(BlockFactory.IsAir(new Stone().GetRuntimeId()));
			Assert.IsFalse(BlockFactory.IsAir(BlockFactory.GetBlockByName("minecraft:cherry_planks").GetRuntimeId()));
		}

		// A sub chunk seeds its palette with air at index 0 so that unwritten space costs nothing.
		// Reading it back has to give air's runtime id and not the palette index, which belongs to
		// an unrelated block: the palette is ordered by a hash of the block name.
		[TestMethod]
		public void SpaceNeverWrittenToReadsAsAir()
		{
			var chunk = new ChunkColumn {X = 0, Z = 0};
			chunk.SetBlock(0, 70, 0, BlockFactory.GetBlockByName("minecraft:cherry_planks"));

			Assert.IsFalse(BlockFactory.IsAir(chunk.GetBlockRuntimeId(0, 70, 0)));
			Assert.IsTrue(BlockFactory.IsAir(chunk.GetBlockRuntimeId(0, 71, 0)));
		}
	}
}
