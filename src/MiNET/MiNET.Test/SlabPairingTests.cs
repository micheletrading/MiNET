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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2026 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using MiNET.Items;
using MiNET.Utils;

namespace MiNET.Test
{
	[TestClass]
	public class SlabPairingTests
	{
		// Placing a slab into a matching slab makes the double slab, which is a separate block with
		// its own name. SlabBase derives that name from its own rather than carrying it, because the
		// pairing is a naming rule Mojang has kept twice over: X_slab gives X_double_slab, and copper
		// infixes instead, cut_copper_slab giving double_cut_copper_slab. A new wood or stone set
		// that pairs a third way would break stacking for that set alone, silently, and only for a
		// player who tried it.

		[TestMethod]
		public void EverySlab_PairsWithADoubleSlabThatExists()
		{
			var unpaired = new List<string>();

			foreach (BlockStateContainer state in BlockFactory.BlockPalette)
			{
				if (BlockFactory.GetBlockByName(state.Name) is not SlabBase slab) continue;
				if (unpaired.Contains(slab.Name)) continue;

				Block doubleSlab = BlockFactory.GetBlockByName(slab.DoubleSlabName);
				if (doubleSlab == null) unpaired.Add($"{slab.Name} -> {slab.DoubleSlabName} (no such block)");
			}

			Assert.AreEqual(0, unpaired.Count, $"slabs with no double slab:\n{string.Join("\n", unpaired)}");
		}

		[TestMethod]
		public void SlabFamily_CoversTheWholeCreativeSlabGroup()
		{
			// A slab that is not a SlabBase has no stacking, no half-height bounding box and ignores
			// which half it was placed in. Family membership is generated from the creative groups,
			// so the group is what the coverage has to be measured against, not the palette: the
			// palette also holds blocks that are in no group, which is deliberate and accepted.
			// petrified_oak_slab is unobtainable, and the poplar set is behind the Third Drop 2026
			// experiment. Asking the group instead of naming those two keeps the test honest when
			// either changes: graduate poplar and it joins the group, and this starts covering it.
			CreativeGroupData creative = InventoryUtils.CreativeGroups.Value;
			int groupIndex = creative.Groups.FindIndex(g => g.Name == "itemGroup.name.slab");
			Assert.AreNotEqual(-1, groupIndex, "the creative slab group is gone, which is what family membership is generated from");

			var members = creative.Entries
				.Where(entry => entry.GroupIndex == groupIndex)
				.Select(entry => ItemFactory.ItemRegistry.GetName((short) entry.NetworkId))
				.Where(name => name != null)
				.Distinct()
				.OrderBy(name => name)
				.ToList();

			Assert.AreNotEqual(0, members.Count, "the creative slab group resolved to nothing");

			var missing = members.Where(name => BlockFactory.GetBlockByName(name) is not SlabBase).ToList();

			Assert.AreEqual(0, missing.Count, $"slabs outside SlabBase:\n{string.Join("\n", missing)}");
		}
	}
}