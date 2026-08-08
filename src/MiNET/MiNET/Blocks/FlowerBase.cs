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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using MiNET.Items;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Behaviour shared by every flower. It used to live on the legacy
	///     <c>minecraft:red_flower</c> and <c>minecraft:yellow_flower</c> classes, which held the
	///     species as a state; flattening made each flower its own block. Both carried exactly this
	///     code, so the split cost nothing but the duplication.
	/// </summary>
	public abstract partial class FlowerBase : Block
	{
		protected FlowerBase(int id) : base(id)
		{
		}

		protected override bool CanPlace(Level world, Player player, BlockCoordinates blockCoordinates, BlockCoordinates targetCoordinates, BlockFace face)
		{
			if (base.CanPlace(world, player, blockCoordinates, targetCoordinates, face))
			{
				Block under = world.GetBlock(Coordinates.BlockDown());
				return under is GrassBlock || under is Dirt;
			}

			return false;
		}

		/// <summary>
		///     Two-block plants (sunflower, lilac, tall grass) drop only from their top half, or the
		///     pair would yield twice. Single flowers carry no upper_block_bit and are unaffected.
		/// </summary>
		public override Item[] GetDrops(Item tool)
		{
			// upper_block_bit is not shared across the flower family: single flowers have no states
			// at all, so it lives on the two-block variants and is read dynamically here.
			BlockStateContainer state = GetState();
			if (state != null)
			{
				foreach (IBlockState entry in state.States)
				{
					if (entry is BlockStateByte b && b.Name == "upper_block_bit") return b.Value != 0 ? base.GetDrops(tool) : new Item[0];
				}
			}

			return base.GetDrops(tool);
		}

		/// <summary>A flower breaks when whatever it stands on goes away.</summary>
		public override void BlockUpdate(Level level, BlockCoordinates blockCoordinates)
		{
			if (Coordinates.BlockDown() == blockCoordinates)
			{
				level.SetAir(Coordinates);
				UpdateBlocks(level);
			}
		}
	}
}
