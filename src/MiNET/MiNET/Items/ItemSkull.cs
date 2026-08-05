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

using System;
using System.Collections.Generic;
using System.Numerics;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Items
{
	public class ItemSkull : Item
	{
		public ItemSkull(short metadata) : base("minecraft:skull", metadata)
		{
			MaxStackSize = 1;
		}

		/// <summary>
		///     The skull metadata used to pick which head; each is its own block now. The item is still
		///     one identity carrying the type in its metadata, so this is the one place that splits.
		/// </summary>
		private static string SkullBlockName(short metadata)
		{
			return metadata switch
			{
				0 => "minecraft:skeleton_skull",
				1 => "minecraft:wither_skeleton_skull",
				2 => "minecraft:zombie_head",
				3 => "minecraft:player_head",
				4 => "minecraft:creeper_head",
				5 => "minecraft:dragon_head",
				6 => "minecraft:piglin_head",
				_ => "minecraft:skeleton_skull"
			};
		}

		public override void PlaceBlock(Level world, Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoords)
		{
			if (face == BlockFace.Down) return; // Doesn't work, ignore if that happen.

			var coor = GetNewCoordinatesFromFace(blockCoordinates, face);

			Block skull = BlockFactory.GetBlockByName(SkullBlockName(Metadata));
			if (skull == null) return;

			skull.Coordinates = coor;

			// facing_direction by state, not by property: the six heads are separate blocks with no
			// common type, so there is nothing to cast to. 1 is on the floor, where the rotation
			// lives in the block entity instead.
			skull.SetState(new List<IBlockState>
			{
				new BlockStateInt {Name = "facing_direction", Value = face == BlockFace.Up ? 1 : (int) face}
			});

			world.SetBlock(skull);

			// Then we create and set the sign block entity that has all the intersting data

			var skullBlockEntity = new SkullBlockEntity
			{
				Coordinates = coor,
				Rotation = (byte) ((int) (Math.Floor(((player.KnownPosition.Yaw)) * 16 / 360) + 0.5) & 0x0f),
				SkullType = (byte) Metadata
			};


			world.SetBlockEntity(skullBlockEntity);

			if (player.GameMode == GameMode.Survival)
			{
				var itemInHand = player.Inventory.GetItemInHand();
				itemInHand.Count--;
				player.Inventory.SetInventorySlot(player.Inventory.InHandSlot, itemInHand);
			}
		}
	}
}