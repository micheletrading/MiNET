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
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Behaviour shared by every stairs block. Which way it faces and whether it is upside down
	///     are minecraft:weirdo_direction and minecraft:upside_down_bit, generated onto this base
	///     because all 64 members of the family carry them and nothing else.
	///     <para>
	///         They used to be declared here by hand as virtual properties. The generator does not
	///         emit override on a state, so each member redeclared them and shadowed these: PlaceBlock
	///         wrote the base's copy and GetState read the member's, and no stair ever faced anywhere
	///         but north side up.
	///     </para>
	/// </summary>
	public abstract partial class StairsBase : Block
	{
		protected StairsBase()
		{
			FuelEfficiency = 15;
		}

		public override bool PlaceBlock(Level world, Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoords)
		{
			UpsideDownBit = (faceCoords.Y > 0.5 && face != BlockFace.Up) || face == BlockFace.Down;

			WeirdoDirection = player.GetProperDirection();

			world.SetBlock(this);
			return true;
		}
	}
}
	/// <summary>
	///     Stairs face the way they were placed and sit either side up. Both are states,
	///     minecraft:weirdo_direction and minecraft:upside_down_bit, generated onto this base since
	///     every member of the family carries them and nothing else.
	/// </summary>