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

using MiNET.Entities;
using MiNET.Entities.Projectiles;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Items
{
	/// <summary>A splash potion: thrown rather than drunk, applying its effect to everyone in the
	/// splash radius on impact.</summary>
	public class ItemSplashPotion : ItemPotion
	{
		public ItemSplashPotion() : this(0)
		{
		}

		public ItemSplashPotion(short metadata) : base("minecraft:splash_potion", metadata)
		{
		}

		public override void UseItem(Level world, Player player, BlockCoordinates blockCoordinates)
		{
			Throw(player, world, EntityType.ThrownSpashPotion);
		}

		internal static void Throw(Player player, Level world, EntityType entityType, short metadata)
		{
			var thrown = new ThrownPotion(player, world, entityType, metadata);
			thrown.KnownPosition = (PlayerLocation) player.KnownPosition.Clone();
			thrown.KnownPosition.Y += 1.62f;
			thrown.Velocity = thrown.KnownPosition.GetDirection().Normalize() * 1.5f;
			thrown.SpawnEntity();

			world.BroadcastSound((BlockCoordinates) player.KnownPosition, LevelSoundEventType.Throw);

			if (player.GameMode == GameMode.Survival || player.GameMode == GameMode.Adventure)
			{
				player.Inventory.ClearInventorySlot((byte) player.Inventory.InHandSlot);
				player.SendPlayerInventory();
			}
		}

		private void Throw(Player player, Level world, EntityType entityType)
		{
			Throw(player, world, entityType, Metadata);
		}
	}
}
