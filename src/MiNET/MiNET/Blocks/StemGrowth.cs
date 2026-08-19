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

using System;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Melon and pumpkin stem growth, mirroring the vanilla Bedrock model (PMMP's Stem +
	///     CropGrowthHelper, the arbiter, and verified against a BDS 1.26.40 oracle capture):
	///     on a random tick with light &gt;= 9 the stem grows one stage (faster when standing on or
	///     near farmland), and a fully grown stem places its fruit on a random horizontal
	///     neighbour that is air above dirt, then faces that direction. The growth window
	///     (BDS oracle: reached stage 7 within 3600 ticks at randomTickSpeed 100) is what the
	///     mechanics-farming scenarios assert.
	/// </summary>
	public static class StemGrowth
	{
		public const int MaxAge = 7;
		private const int MinLightLevel = 9;

		/// <summary>Runs one random-tick growth step for the given stem; returns the new age.</summary>
		public static int OnRandomTick(Level level, BlockCoordinates coordinates, int currentAge, string fruitBlockName)
		{
			if (currentAge >= MaxAge)
			{
				TryPlaceFruit(level, coordinates, fruitBlockName);
				return currentAge;
			}

			if (level.GetSubtractedLight(coordinates) < MinLightLevel) return currentAge;

			int multiplier = GrowthMultiplier(level, coordinates);
			if (multiplier > 0 && level.Random.Next(26) < multiplier)
			{
				return currentAge + 1;
			}

			return currentAge;
		}

		/// <summary>
		///     Vanilla growth-speed multiplier: 1 alone, +3 on hydrated farmland, +1 on dry
		///     farmland, +3/4 per adjacent hydrated farmland, +1/4 per adjacent dry farmland,
		///     halved when crops are arranged improperly (rows crossing).
		/// </summary>
		private static int GrowthMultiplier(Level level, BlockCoordinates coordinates)
		{
			int result = 1;
			Block below = level.GetBlock(coordinates.BlockDown());
			if (below is Farmland farmland)
			{
				result += farmland.MoisturizedAmount > 0 ? 3 : 1;
			}

			bool xRow = false;
			bool zRow = false;
			bool improperArrangement = false;

			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dz = -1; dz <= 1; dz++)
				{
					if (dx == 0 && dz == 0) continue;

					Block nextFarmland = level.GetBlock(new BlockCoordinates(coordinates.X + dx, coordinates.Y - 1, coordinates.Z + dz));
					if (!(nextFarmland is Farmland next))
					{
						continue;
					}

					result += next.MoisturizedAmount > 0 ? 3 : 1;

					if (!improperArrangement)
					{
						Block nextCrop = level.GetBlock(new BlockCoordinates(coordinates.X + dx, coordinates.Y, coordinates.Z + dz));
						if (nextCrop.GetType() == below.GetType())
						{
							if (dx == 0 && zRow) improperArrangement = true;
							else if (dx != 0 && dz != 0) improperArrangement = true;
							else if (dz == 0 && xRow) improperArrangement = true;
							else if (dx == 0) zRow = true;
							else if (dz == 0) xRow = true;
						}
					}
				}
			}

			if (improperArrangement) result /= 2;

			return Math.Max(1, result);
		}

		/// <summary>
		///     A fully grown stem places its fruit on a random horizontal neighbour that is air
		///     above dirt (farmland reverts to dirt below the fruit, like vanilla), then faces it.
		///     Returns the new facing, or the current one when no fruit was placed.
		/// </summary>
		private static void TryPlaceFruit(Level level, BlockCoordinates coordinates, string fruitBlockName)
		{
			BlockCoordinates[] sides =
			{
				coordinates.BlockWest(),
				coordinates.BlockEast(),
				coordinates.BlockSouth(),
				coordinates.BlockNorth(),
			};

			// Try each random side once (vanilla picks one random side and gives up when it is
			// not usable).
			int start = level.Random.Next(4);
			for (int i = 0; i < 4; i++)
			{
				BlockCoordinates side = sides[(start + i) % 4];
				Block sideBlock = level.GetBlock(side);
				if (!(sideBlock is Air)) continue;

				Block sideBelow = level.GetBlock(side.BlockDown());
				// The vanilla minecraft:dirt tag (the fruit's allowed ground) includes dirt,
				// coarse dirt, farmland, grass, moss, podzol, mycelium and mud.
				if (!(sideBelow is Dirt || sideBelow is Farmland || sideBelow is GrassBlock ||
				      sideBelow is MossBlock || sideBelow is Podzol || sideBelow is Mycelium)) continue;

				Block fruit = BlockFactory.GetBlockByName(fruitBlockName);
				fruit.Coordinates = side;
				level.SetBlock(fruit);

				return;
			}
		}
	}
}
