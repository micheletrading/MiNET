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
using System.Numerics;
using MiNET.Net;
using MiNET.Sounds;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Entities.World
{
	/// <summary>
	///     An experience orb. Sits where it spawned and is picked up by the first player who
	///     walks close enough, playing the pickup sound and adding the value to their experience.
	/// </summary>
	public class ExperienceOrb : Entity
	{
		private const float PickupDistance = 1.5f;
		private const float AttractionDistance = 4.0f;

		private int _value;

		public ExperienceOrb(Level level, int value) : base(EntityType.ExperienceOrb, level)
		{
			_value = value;
			NoAi = true;
			HealthManager.IsInvulnerable = true;
		}

		public override void OnTick(Entity[] entities)
		{
			Player nearest = null;
			float nearestDistance = float.MaxValue;
			foreach (Player player in Level.GetSpawnedPlayers())
			{
				float distance = Vector3.Distance(player.KnownPosition.ToVector3(), KnownPosition.ToVector3());
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearest = player;
				}
			}

			if (nearest == null) return;

			if (nearestDistance <= PickupDistance)
			{
				nearest.ExperienceManager.AddExperience(_value);
				new ExperienceOrbSound(KnownPosition.ToVector3()).Spawn(Level);
				DespawnEntity();
				return;
			}

			// Drift toward the nearest player, the way vanilla orbs do.
			if (nearestDistance <= AttractionDistance)
			{
				Vector3 direction = Vector3.Normalize(nearest.KnownPosition.ToVector3() - KnownPosition.ToVector3());
				KnownPosition += direction * 0.15f;
			}
		}
	}
}
