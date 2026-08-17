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
using MiNET.Utils.Metadata;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Entities.Projectiles
{
	/// <summary>
	///     The cloud a lingering potion leaves on impact. Sits where it landed, shows the potion's
	///     colour, and re-applies the effect to every player inside its radius while it lasts
	///     (30 seconds, vanilla's own lifetime).
	/// </summary>
	public class LingeringCloud : Entity
	{
		private const float Radius = 3.0f;
		private const int Duration = 600; // 30 seconds

		private readonly short _potionMetadata;
		private int _ticksLeft = Duration;

		public LingeringCloud(Level level, short potionMetadata, PlayerLocation position) : base(EntityType.AreaEffectCloud, level)
		{
			_potionMetadata = potionMetadata;

			KnownPosition = (PlayerLocation) position.Clone();
			HealthManager.IsInvulnerable = true;
			NoAi = true;

			Effect[] effects = ItemPotion.GetEffects(potionMetadata);
			if (effects.Length > 0)
			{
				Color c = effects[0].ParticleColor;
				PotionColor = (int) (0xff000000 | ((uint) c.R << 16) | ((uint) c.G << 8) | (uint) c.B);
			}
		}

		public override MetadataDictionary GetMetadata()
		{
			MetadataDictionary metadata = base.GetMetadata();
			// Flag 58 is the area effect cloud's radius (the generic name in MetadataFlags is
			// the rider one; the value is per-entity-type and the cloud reads a float here).
			metadata[58] = new MetadataFloat(Radius);
			return metadata;
		}

		public override void OnTick(Entity[] entities)
		{
			if (--_ticksLeft <= 0)
			{
				DespawnEntity();
				return;
			}

			if (_ticksLeft % 20 != 0) return;

			foreach (Player player in Level.GetSpawnedPlayers())
			{
				if (Vector3.Distance(player.KnownPosition.ToVector3(), KnownPosition.ToVector3()) > Radius) continue;

				// Fresh instance per player: an effect ticks its own duration down once applied,
				// and re-applying refreshes it the way vanilla's cloud does.
				foreach (Effect effect in ItemPotion.GetEffects(_potionMetadata))
				{
					player.SetEffect(effect);
				}
			}
		}
	}
}
