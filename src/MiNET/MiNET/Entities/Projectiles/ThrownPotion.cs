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

using System.Drawing;
using System.Numerics;
using MiNET.Effects;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Entities.Projectiles
{
	/// <summary>
	///     A thrown splash or lingering potion. Flies like the other projectiles, shows its
	///     potion colour through the PotionColor metadata flag, and on impact applies the
	///     potion's effect to every player in the splash radius. The lingering cloud is not
	///     modelled yet; a lingering potion behaves like a splash.
	/// </summary>
	public class ThrownPotion : Projectile
	{
		public short PotionMetadata { get; }

		/// <summary>Whether the impact leaves a lingering cloud rather than a one-off splash.</summary>
		private readonly bool _isLingering;

		public ThrownPotion(Player shooter, Level level, EntityType entityType, short metadata) : base(shooter, entityType, level, 0)
		{
			PotionMetadata = metadata;
			_isLingering = entityType == EntityType.LingeringPotion;

			Width = 0.25;
			Length = 0.25;
			Height = 0.25;

			Gravity = 0.05;
			Drag = 0.01;

			HealthManager.IsInvulnerable = true;
			DespawnOnImpact = true;
			BroadcastMovement = true;

			Effect[] effects = ItemPotion.GetEffects(metadata);
			if (effects.Length > 0)
			{
				Color c = effects[0].ParticleColor;
				PotionColor = (int) (0xff000000 | ((uint) c.R << 16) | ((uint) c.G << 8) | (uint) c.B);
			}
		}

		public override void DespawnEntity()
		{
			// The splash particle the client renders (tinted with the potion's colour) and the
			// glass sound everyone hears.
			var splash = McpeLevelEvent.CreateObject();
			splash.eventId = (int) LevelEventType.ParticlesPotionSplash;
			splash.position = KnownPosition.ToVector3();
			splash.data = PotionColor;
			Level.RelayBroadcast(splash);

			Level.BroadcastSound((BlockCoordinates) KnownPosition, LevelSoundEventType.Glass);

			if (_isLingering)
			{
				var cloud = new LingeringCloud(Level, PotionMetadata, KnownPosition);
				cloud.SpawnEntity();
			}
			else
			{
				ApplySplash();
			}

			base.DespawnEntity();
		}

		private void ApplySplash()
		{
			Effect[] effects = ItemPotion.GetEffects(PotionMetadata);
			if (effects.Length == 0) return;

			foreach (Player player in Level.GetSpawnedPlayers())
			{
				if (Vector3.Distance(player.KnownPosition.ToVector3(), KnownPosition.ToVector3()) > 4.5f) continue;

				// A fresh instance per player: an effect ticks its own duration down once applied.
				foreach (Effect effect in ItemPotion.GetEffects(PotionMetadata))
				{
					player.SetEffect(effect);
				}
			}
		}
	}
}
