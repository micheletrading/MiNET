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

namespace MiNET.Test
{
	[TestClass]
	public class LegacyBlockMappingTests
	{
		// Reading a pre-flattening world (Java Anvil via AnvilWorldProvider.Convert, or an old
		// Bedrock world) ends at LegacyToRuntimeId, which is built by joining
		// r12_to_current_block_map.bin against the current palette. That join is silent when it
		// fails: an unmatched pair leaves -1 and the block loads as minecraft:info_update, so a
		// whole world reads as the unknown-block placeholder with nothing logged.
		//
		// It fails whenever the map file is older than the palette, because Mojang renames blocks
		// (planks -> oak_planks) and drops state properties (dirt lost dirt_type). The map must be
		// refreshed from pmmp/BedrockData for the version the palette targets whenever the palette
		// moves. These are the blocks any real map is made of, so they are the canary.

		[TestMethod]
		public void LegacyIdAndMetadata_ResolveToTheirModernBlock()
		{
			var expected = new Dictionary<(int Id, byte Metadata), string>
			{
				[(1, 0)] = "minecraft:stone",
				[(2, 0)] = "minecraft:grass_block",
				[(3, 0)] = "minecraft:dirt",
				[(4, 0)] = "minecraft:cobblestone",
				[(7, 0)] = "minecraft:bedrock",
				[(12, 0)] = "minecraft:sand",
				[(17, 0)] = "minecraft:oak_log",
				[(18, 0)] = "minecraft:oak_leaves",
				[(98, 0)] = "minecraft:stone_bricks",

				// Metadata carried the variant before the flattening, so these prove the join
				// resolves states and not just the id.
				[(5, 0)] = "minecraft:oak_planks",
				[(5, 1)] = "minecraft:spruce_planks",
				[(35, 14)] = "minecraft:red_wool"
			};

			foreach (((int id, byte metadata), string name) in expected)
			{
				uint runtimeId = BlockFactory.GetRuntimeId(id, metadata);
				Assert.IsTrue(runtimeId < BlockFactory.BlockPalette.Count, $"legacy ({id},{metadata}) resolved outside the palette");
				Assert.AreEqual(name, BlockFactory.BlockPalette[(int) runtimeId].Name, $"legacy ({id},{metadata}) resolved to the wrong block");
			}
		}

		// A wholesale regression (wrong file, failed join, empty resource) shows up as the table
		// collapsing rather than as a handful of wrong blocks, and the pairs above would not
		// necessarily catch it. The stale 1.18.30 map filled 1995 entries; the matching one fills
		// 3179. The floor is deliberately loose: it is a cliff detector, not a pin.
		[TestMethod]
		public void LegacyMappingTable_IsPopulated()
		{
			int populated = 0;
			foreach (int runtimeId in BlockFactory.LegacyToRuntimeId)
			{
				if (runtimeId != -1) populated++;
			}

			Assert.IsTrue(populated > 3000, $"only {populated} legacy id/metadata pairs map to a block; r12_to_current_block_map.bin is probably older than the palette");
		}
	}
}
