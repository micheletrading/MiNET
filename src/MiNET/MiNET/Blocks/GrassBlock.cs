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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MiNET.Items;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     The behaviour that used to live on the legacy id-2 <c>Grass</c> class. That class claimed
	///     the same name as this one, and BlockFactory always prefers the generated type, so none of
	///     it ever ran: grass never died in the dark, never spread, and bone meal did nothing.
	/// </summary>
	public partial class GrassBlock : Block
	{
		public GrassBlock()
		{
		}

		public override void DoPhysics(Level level)
		{
			if (level.GameMode == GameMode.Creative) return;

			if (level.GetSubtractedLight(Coordinates.BlockUp()) < 4)
			{
				Block dirt = BlockFactory.GetBlockByName("minecraft:dirt");
				dirt.Coordinates = Coordinates;
				level.SetBlock(dirt, true, false, false);
			}
		}

		public override void OnTick(Level level, bool isRandom)
		{
			if (level.GameMode == GameMode.Creative) return;
			if (!isRandom) return;

			var lightLevel = level.GetSubtractedLight(Coordinates.BlockUp());
			if (lightLevel < 4 /* && check opacity */)
			{
				Block dirt = BlockFactory.GetBlockByName("minecraft:dirt");
				dirt.Coordinates = Coordinates;
				level.SetBlock(dirt, true, false, false);
			}
			else
			{
				if (lightLevel >= 9)
				{
					Random random = new Random();
					for (int i = 0; i < 4; i++)
					{
						var coordinates = Coordinates + new BlockCoordinates(random.Next(3) - 1, random.Next(5) - 3, random.Next(3) - 1);
						if (level.GetBlock(coordinates) is Dirt)
						{
							Block nextUp = level.GetBlock(coordinates.BlockUp());
							if (nextUp.IsTransparent && (nextUp.BlockLight >= 4 || nextUp.SkyLight >= 4))
							{
								level.SetBlock(new GrassBlock {Coordinates = coordinates});
							}
						}
					}
				}
			}
		}

		public override bool Interact(Level level, Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoord)
		{
			var itemInHand = player.Inventory.GetItemInHand();
			if ((itemInHand is ItemBoneMeal || (itemInHand is ItemDye && itemInHand.Metadata == 15)))
			{
				// If bone meal is used on a grass block, 0-8(double) tall grass, 8-24 grass and 0-8 flowers form on the
				// targeted block and on randomly-selected adjacent grass blocks up to 7 blocks away (taxicab distance).
				// The flowers that appear are dependent on the biome, meaning that in order to obtain specific flowers,
				// the player must travel to biomes where the flowers are found naturally. See Flower Â§ Flower biomes
				// for more information.
				//TODO: Grow grass and flowers randomly
				int grassPlanted = 0;
				int flowersPlanted = 0;

				var rnd = new Random();
				for (int i = 0; i < 128; i++)
				{
					BlockCoordinates coord = blockCoordinates;
					bool shouldContinue = false;
					for (int j = 0; j < i / 16; j++)
					{
						coord += new BlockCoordinates(rnd.Next(3) - 1, (rnd.Next(3) - 1) * (rnd.Next(3) / 2), rnd.Next(3) - 1);
						if (!level.GetBlock(coord).IsSolid)
						{
							shouldContinue = true;
							break;
						}
					}
					if (shouldContinue) continue;

					if (!(level.GetBlock(coord) is GrassBlock)) continue;
					coord += BlockCoordinates.Up;
					Block growthBlock = level.GetBlock(coord);

					// minecraft:tallgrass carried a tall_grass_type state; it is now separate blocks,
					// short_grass and fern. minecraft:tall_grass is the two-block plant that used
					// to be double_plant, so it is deliberately not what grows here.
					if (growthBlock is ShortGrass or Fern)
					{
						if (grassPlanted >= 24) continue;

						if (growthBlock is ShortGrass)
						{
							if (rnd.Next(10) == 0)
							{
								Block block = BlockFactory.GetBlockByName("minecraft:tall_grass");
								if (block == null) continue;
								block.Coordinates = coord;
								level.SetBlock(block);
								grassPlanted++;
							}
						}
					}
					else if (growthBlock is Air)
					{
						if (rnd.Next(8) == 0)
						{
							if (flowersPlanted >= 8) continue;

							// Flattening made each flower its own block, so the species is a name rather
							// than a state on one red_flower or yellow_flower block.
							Block block = null;
							int biomeId = level.GetBiomeId(coord);
							switch (biomeId)
							{
								case 1: // plains
									block = BlockFactory.GetBlockByName(rnd.Next(2) == 0 ? "minecraft:poppy" : "minecraft:dandelion");
									break;
								default:
									break;
							}
							if (block != null)
							{
								block.Coordinates = coord;
								level.SetBlock(block);
							}

							flowersPlanted++;
						}
						else
						{
							if (grassPlanted >= 24) continue;

							Block block = rnd.Next(10) != 0 ? new ShortGrass() : new Fern();
							block.Coordinates = coord;
							level.SetBlock(block);
							grassPlanted++;
						}
					}
				}

				return true;
			}

			return false; // not handled
		}

		public override Item[] GetDrops(Item tool)
		{
			return new[] {ItemFactory.GetItemByName("minecraft:dirt")}; //Drop dirt block
		}
	}

	public class RandomWeighted<T>
	{
		private readonly List<RandomRange<T>> _items;
		private Random _random;

		public RandomWeighted(List<RandomRange<T>> items)
		{
			_items = items;
			_random = new Random();
		}

		public T Next()
		{
			int targetWeight = _random.Next(_items.Sum(i => i.Weight) + 1);
			int currentWeight = 0;
			foreach (RandomRange<T> range in _items)
			{
				currentWeight += range.Weight;

				if (targetWeight < currentWeight) return range.Item;
			}

			return default;
		}
	}

	public class RandomRange<T>
	{
		public T Item { get; }
		public int Weight { get; }

		public RandomRange(T item, int weight)
		{
			Item = item;
			Weight = weight;
		}
	}
}
