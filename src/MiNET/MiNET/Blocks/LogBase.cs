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
using MiNET.Items;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Behaviour shared by every log, wood, stem and hyphae block. It used to live on the legacy
	///     <c>minecraft:log</c> and <c>minecraft:log2</c> classes, which carried the wood type as an
	///     <c>old_log_type</c> state; flattening made the type the block identity instead.
	///     <para>
	///         Leaves decay is defined by proximity to one of these, so this base is also what lets
	///         <see cref="LeavesBase" /> ask the question by type rather than by name.
	///     </para>
	/// </summary>
	public abstract partial class LogBase : Block
	{
		protected LogBase()
		{
			FuelEfficiency = 15;
		}

		/// <summary>
		///     Orients the log to the face it was placed against. pillar_axis is declared on the
		///     generated part of each log, so the base writes it through the state container rather
		///     than as a property, which would only shadow the one that actually serialises.
		/// </summary>
		public override bool PlaceBlock(Level world, Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoords)
		{
			PillarAxis = ItemBlock.GetPillarAxisFromFace(face).ToString().ToLowerInvariant();
			return false;
		}

		/// <summary>Smelting any log gives charcoal.</summary>
		public override Item GetSmelt()
		{
			return ItemFactory.GetItemByName("minecraft:coal", 1);
		}
	}
}
