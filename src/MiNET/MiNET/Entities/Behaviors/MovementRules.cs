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
using MiNET.Blocks;

namespace MiNET.Entities.Behaviors
{
	/// <summary>
	///     What stops a moving entity. This is movement's own opinion, not a property of the block:
	///     the block data says whether something is a full solid cube, and says nothing about whether
	///     a mob can walk through it. Solidity alone answers wrong, because a fence and a closed door
	///     are both IsSolid false and both stop a mob.
	///     A bit per runtime id, resolved once from the palette. Pathfinding is the only caller that
	///     asks this thousands of times per operation and the only one that needs a block's state at
	///     that volume, which is why the answer is precomputed here rather than tested per call.
	/// </summary>
	public static class MovementRules
	{
		private static readonly Lazy<ulong[]> _blocking = new Lazy<ulong[]>(BuildBlockingMask);

		/// <summary>Whether a block at this runtime id stops a moving entity.</summary>
		public static bool Blocks(int runtimeId)
		{
			ulong[] mask = _blocking.Value;
			int word = runtimeId >> 6;
			return runtimeId >= 0 && word < mask.Length && (mask[word] & (1UL << (runtimeId & 63))) != 0;
		}

		private static ulong[] BuildBlockingMask()
		{
			int count = BlockFactory.BlockPalette.Count;
			ulong[] mask = new ulong[(count + 63) / 64];

			// Builds a block per palette entry, which is the one place that is the right thing to do:
			// once at startup, to answer a question the block data cannot, so that nothing has to
			// build one again per query.
			for (int runtimeId = 0; runtimeId < count; runtimeId++)
			{
				if (IsBlocking(BlockFactory.GetBlockByRuntimeId(runtimeId))) mask[runtimeId >> 6] |= 1UL << (runtimeId & 63);
			}

			return mask;
		}

		private static bool IsBlocking(Block block)
		{
			if (block == null) return false;

			// Doors carry their open state in the palette, so open and closed are separate runtime ids
			// and answer differently with no state check left at the call site.
			if (block is DoorBase door) return !door.OpenBit;

			// Post-flattening a fence is a separate block per wood type, and none of them is a full
			// cube, so solidity does not cover them.
			if (block.Name.EndsWith("_fence") || block.Name == "minecraft:nether_brick_fence") return true;

			return block.IsSolid;
		}
	}
}
