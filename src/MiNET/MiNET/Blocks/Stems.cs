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

using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Melon and pumpkin stems grow on random ticks: light &gt;= 9, farmland below, then one
	///     stage per successful tick, and a full-grown stem places its fruit on a random adjacent
	///     dirt cell and faces it. See <see cref="StemGrowth" /> for the vanilla model this
	///     mirrors (PMMP Stem + CropGrowthHelper; verified against the BDS oracle capture).
	/// </summary>
	public partial class MelonStem
	{
		protected override bool CanPlace(Level world, Player player, BlockCoordinates blockCoordinates, BlockCoordinates targetCoordinates, BlockFace face)
		{
			Block under = world.GetBlock(Coordinates.BlockDown());
			return under is Farmland;
		}

		public override void OnTick(Level level, bool isRandom)
		{
			if (!isRandom) return;

			int newAge = StemGrowth.OnRandomTick(level, Coordinates, Growth, "minecraft:melon_block");
			if (newAge != Growth)
			{
				Growth = newAge;
				level.SetBlock(this);
			}
		}
	}

	/// <summary>Pumpkin stems grow identically to melon stems; only the fruit differs.</summary>
	public partial class PumpkinStem
	{
		protected override bool CanPlace(Level world, Player player, BlockCoordinates blockCoordinates, BlockCoordinates targetCoordinates, BlockFace face)
		{
			Block under = world.GetBlock(Coordinates.BlockDown());
			return under is Farmland;
		}

		public override void OnTick(Level level, bool isRandom)
		{
			if (!isRandom) return;

			int newAge = StemGrowth.OnRandomTick(level, Coordinates, Growth, "minecraft:pumpkin");
			if (newAge != Growth)
			{
				Growth = newAge;
				level.SetBlock(this);
			}
		}
	}
}
