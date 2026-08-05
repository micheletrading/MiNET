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

using System.Numerics;
using log4net;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Items
{
	public class ItemBucket : Item
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ItemBucket));

		public ItemBucket(short metadata) : base("minecraft:bucket", metadata)
		{
			MaxStackSize = 1;
			FuelEfficiency = (short) (Metadata == 10 ? 1000 : 0);
		}

		public override void PlaceBlock(Level world, Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoords)
		{
			// A bucket's contents are still carried as the liquid's pre-flattening id: 8 water,
			// 10 lava. Only those two can be poured, which is also what stops a crafted metadata
			// from placing an arbitrary block.
			string liquid = Metadata switch
			{
				8 => "minecraft:flowing_water",
				10 => "minecraft:flowing_lava",
				_ => null
			};

			if (liquid != null)
			{
				var itemBlock = new ItemBlock(BlockFactory.GetBlockByName(liquid));
				itemBlock.PlaceBlock(world, player, blockCoordinates, face, faceCoords);
			}
			else if (Metadata == 0) // Empty bucket
			{
				// Pick up water/lava
				var block = world.GetBlock(blockCoordinates);
				switch (block)
				{
					case Stationary fluid:
					{
						if (fluid.LiquidDepth == 0) // Only source blocks
						{
							world.SetAir(blockCoordinates);
						}
						break;
					}
					case Flowing fluid:
					{
						if (fluid.LiquidDepth == 0) // Only source blocks
						{
							world.SetAir(blockCoordinates);
						}
						break;
					}
				}
			}

			FuelEfficiency = (short) (Metadata == 10 ? 1000 : 0);
		}
	}
}