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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Utils;

namespace MiNET.Test
{
	[TestClass]
	public class BlockHashTests
	{
		// Published Bedrock block-permutation network-hash constants (from Mojang's scripting
		// ecosystem / the community protocol Discord). These pin our production hash,
		// BlockFactory.ComputeNetworkHash (FNV-1a 32 over the little-endian NBT of {name, states}
		// with states sorted by name), to the exact values the real client computes. The test does
		// not depend on any palette we hold, so it fails only if the hashing itself regresses:
		// wrong FNV constants, wrong NBT byte layout, or a broken state sort.

		[TestMethod]
		public void ComputeNetworkHash_Dirt_NoStates()
		{
			var dirt = new BlockStateContainer { Name = "minecraft:dirt", States = new List<IBlockState>() };
			Assert.AreEqual(unchecked((uint) -2108756090), BlockFactory.ComputeNetworkHash(dirt));
		}

		[TestMethod]
		public void ComputeNetworkHash_Bedrock_InfiniburnBitFalse()
		{
			var bedrock = new BlockStateContainer
			{
				Name = "minecraft:bedrock",
				States = new List<IBlockState> { new BlockStateByte { Name = "infiniburn_bit", Value = 0 } }
			};
			Assert.AreEqual(unchecked((uint) -173245189), BlockFactory.ComputeNetworkHash(bedrock));
		}
	}
}
