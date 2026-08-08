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

using System.Numerics;
using log4net;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     A single slab stands in half a block, and a second one of the same kind placed into it
	///     becomes the double slab, which is a separate block. Which half it occupies is
	///     minecraft:vertical_half, generated onto this base since every member of the family carries
	///     it and nothing else.
	/// </summary>
	public abstract partial class SlabBase : Block
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(SlabBase));

		/// <summary>Whether this slab sits in the upper half of its block.</summary>
		public bool IsTopSlab => VerticalHalf == "top";

		/// <summary>
		///     The double slab this pairs with, by name: X_slab gives X_double_slab, and copper
		///     infixes instead, cut_copper_slab giving double_cut_copper_slab. Override for a name
		///     that follows neither.
		/// </summary>
		public virtual string DoubleSlabName =>
			Name.Contains("cut_copper")
				? Name.Replace("cut_copper", "double_cut_copper")
				: Name.Replace("_slab", "_double_slab");

		public override BoundingBox GetBoundingBox()
		{
			var bottom = (Vector3) Coordinates;

			if (IsTopSlab)
				bottom.Y += 0.5f;

			var top = bottom + new Vector3(1f, 0.5f, 1f);

			return new BoundingBox(bottom, top);
		}

		protected override bool CanPlace(Level world, Player player, BlockCoordinates blockCoordinates, BlockCoordinates targetCoordinates, BlockFace face)
		{
			return base.CanPlace(world, player, blockCoordinates, targetCoordinates, face) || world.GetBlock(blockCoordinates).Name == Name;
		}

		public override bool PlaceBlock(Level world, Player player, BlockCoordinates targetCoordinates, BlockFace face, Vector3 faceCoords)
		{
			var targetBlock = world.GetBlock(targetCoordinates);

			if (targetBlock != null && face == BlockFace.Up && faceCoords.Y == 0.5 && AreSameType(targetBlock))
			{
				// Replace with double block
				SetDoubleSlab(world, targetCoordinates);
				return true;
			}

			if (targetBlock != null && face == BlockFace.Down && faceCoords.Y == 0.5 && AreSameType(targetBlock))
			{
				// Replace with double block
				SetDoubleSlab(world, targetCoordinates);
				return true;
			}

			var existingBlock = world.GetBlock(Coordinates);
			if (existingBlock == null || !AreSameType(existingBlock))
			{
				if (face != BlockFace.Up && faceCoords.Y > 0.5 || (face == BlockFace.Down && faceCoords.Y == 0.0))
				{
					VerticalHalf = "top";
				}

				return false;
			}

			// Same material in existing block, make double slab
			// Create double slab, replace existing
			SetDoubleSlab(world, Coordinates);

			return true;
		}

		protected virtual bool AreSameType(Block obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != this.GetType()) return false;
			return true;
		}

		protected void SetDoubleSlab(Level world, BlockCoordinates coordinates)
		{
			Block slab = BlockFactory.GetBlockByName(DoubleSlabName);
			if (slab == null)
			{
				// An unpaired name leaves the single slab standing rather than throwing on placement.
				Log.Warn($"No double slab {DoubleSlabName} for {Name}");
				return;
			}

			slab.Coordinates = coordinates;
			slab.SetState(GetState().States);
			world.SetBlock(slab);
		}
	}
}