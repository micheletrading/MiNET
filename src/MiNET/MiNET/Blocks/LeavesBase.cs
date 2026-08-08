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
using MiNET.Items;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Decay behaviour shared by every leaves block. It used to live on the legacy
	///     <c>minecraft:leaves</c> and <c>minecraft:leaves2</c> classes, which held the wood type as
	///     an <c>old_leaf_type</c> state. Flattening turned that one block into thirteen, so the type
	///     is now the class itself and the behaviour belongs on a base they all share.
	///     <para>
	///         The generator picks this base for any block whose class name ends in Leaves, so a new
	///         wood type gets the behaviour without anyone remembering to wire it up.
	///     </para>
	/// </summary>
	public abstract partial class LeavesBase : Block
	{
		/// <summary>How far a leaf may be from a log before it decays.</summary>
		private const int MaxLogDistance = 4;

		protected LeavesBase(int id) : base(id)
		{
		}

		/// <summary>
		///     The sapling this drops. Derived from the block's own name, since post-flattening the
		///     two share a wood prefix. Override where that does not hold, as for mangrove, which
		///     drops a propagule rather than a sapling.
		/// </summary>
		public virtual string SaplingName => Name.Replace("_leaves", "_sapling");

		/// <summary>Oak and dark oak are the only leaves that drop apples.</summary>
		protected virtual bool DropsApples => false;

		public override void BlockUpdate(Level level, BlockCoordinates blockCoordinates)
		{
			// No decay
			if (PersistentBit) return;
			if (UpdateBit) return;

			UpdateBit = true;

			level.SetBlock(this, false, false, false);
		}

		public override void OnTick(Level level, bool isRandom)
		{
			if (PersistentBit) return;
			if (!UpdateBit) return;

			if (FindLog(level, Coordinates, new List<BlockCoordinates>(), 0))
			{
				UpdateBit = false;
				level.SetBlock(this, false, false, false);
				return;
			}

			var drops = GetDrops(null);
			BreakBlock(level, BlockFace.None, drops.Length == 0);
			foreach (var drop in drops)
			{
				level.DropItem(Coordinates, drop);
			}
		}

		public override Item[] GetDrops(Item tool)
		{
			var rnd = new Random();

			if (DropsApples && rnd.Next(200) == 0)
			{
				return new[] {ItemFactory.GetItemByName("minecraft:apple", 0, 1)};
			}

			if (rnd.Next(20) == 0)
			{
				Item sapling = ItemFactory.GetItemByName(SaplingName, 0, 1);
				if (sapling != null) return new[] {sapling};
			}

			return new Item[0];
		}

		/// <summary>
		///     Walks outward looking for a log within <see cref="MaxLogDistance" />, through leaves of
		///     this same kind only. Mixed species do not sustain each other, which is what the old
		///     old_leaf_type comparison expressed and what the type check expresses now.
		/// </summary>
		private bool FindLog(Level level, BlockCoordinates coord, List<BlockCoordinates> visited, int distance)
		{
			if (visited.Contains(coord)) return false;

			Block block = level.GetBlock(coord);

			if (block is LogBase) return true;

			visited.Add(coord);

			if (distance >= MaxLogDistance) return false;

			if (block?.GetType() != GetType()) return false;

			return FindLog(level, coord.BlockDown(), visited, distance + 1)
					|| FindLog(level, coord.BlockWest(), visited, distance + 1)
					|| FindLog(level, coord.BlockEast(), visited, distance + 1)
					|| FindLog(level, coord.BlockSouth(), visited, distance + 1)
					|| FindLog(level, coord.BlockNorth(), visited, distance + 1)
					|| FindLog(level, coord.BlockUp(), visited, distance + 1);
		}
	}
}
