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
using System.Linq;
using System.Numerics;
using fNbt;
using log4net;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Metadata;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Entities.Projectiles
{
	public class FireworksRocket : Projectile
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(FireworksRocket));

		public Item Fireworks { get; set; }
		public int Lifetime { get; set; }

		public FireworksRocket(Player shooter, Level level, Item fireworks, Random random = null) : base(shooter, EntityType.FireworksRocket, level, 0)
		{
			random = random ?? new Random();

			Fireworks = fireworks;
			Width = 0.25;
			Length = 0.25;
			Height = 0.25;

			Gravity = 0.0;
			Drag = 0.01;

			HealthManager.IsInvulnerable = true;

			HasCollision = true;
			IsAffectedByGravity = true;

			int flyTime = 1;
			try
			{
				if (Fireworks.ExtraData["Fireworks"]["Flight"] is NbtByte flight)
				{
					flyTime = flight.ByteValue;
				}
			}
			catch (Exception e)
			{
				Log.Debug(e);
			}

			Lifetime = 20 * flyTime + random.Next(5) + random.Next(7);
		}

		public override MetadataDictionary GetMetadata()
		{
			var metadata = base.GetMetadata();
			// The client renders the rocket (and the burst colors) from the firework item NBT under
			// key 16 (FIREWORK_ITEM). Without it the rocket spawns blank and never bursts. The item's
			// ExtraData is exactly the {Fireworks:{Explosions:[...],Flight:N}} compound vanilla sends.
			if (Fireworks?.ExtraData != null)
			{
				metadata[(int) MetadataFlags.FireworksType] = new MetadataNbt(Fireworks.ExtraData);
			}

			return metadata;
		}

		public override void SpawnEntity()
		{
			Velocity = Force = KnownPosition.GetDirection().Normalize() * 0.06055374f;
			KnownPosition.Yaw = (float) Velocity.GetYaw();
			KnownPosition.Pitch = (float) Velocity.GetPitch();

			var sound = McpeLevelSoundEvent.CreateObject();
			sound.soundId = LevelSoundEventType.Launch.ToWireName();
			sound.blockId = -1;
			sound.position = KnownPosition;
			Level.RelayBroadcast(sound);

			base.SpawnEntity();
		}

		public override void DespawnEntity()
		{
			// The burst: the client renders the firework colors from the FIREWORK_EXPLODE actor event.
			McpeEntityEvent entityEvent = McpeEntityEvent.CreateObject();
			entityEvent.runtimeEntityId = EntityId;
			entityEvent.eventId = 25;
			entityEvent.data = 0;
			Level.RelayBroadcast(entityEvent);

			base.DespawnEntity();

			Burst();
		}

		/// <summary>
		///     The blast itself: sound and damage, like vanilla. Damage scales with the number of
		///     explosions on the firework (force = count * 2 + 5) and falls off with distance
		///     (force * sqrt((5 - distance) / 5)) inside the 5-block radius, per pmmp's reference.
		/// </summary>
		private void Burst()
		{
			int explosionCount = 0;
			bool twinkle = false;
			try
			{
				if (Fireworks?.ExtraData?["Fireworks"] is NbtCompound fireworks
					&& fireworks["Explosions"] is NbtList explosions)
				{
					explosionCount = explosions.Count;
					foreach (NbtTag tag in explosions)
					{
						if (tag is NbtCompound explosion && explosion["FireworkFlicker"] is NbtByte flicker && flicker.ByteValue != 0)
						{
							twinkle = true;
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Debug(e);
			}

			var burst = McpeLevelSoundEvent.CreateObject();
			burst.soundId = LevelSoundEventType.Blast.ToWireName();
			burst.blockId = -1;
			burst.position = KnownPosition;
			Level.RelayBroadcast(burst);

			if (twinkle)
			{
				var twinkleSound = McpeLevelSoundEvent.CreateObject();
				twinkleSound.soundId = LevelSoundEventType.Twinkle.ToWireName();
				twinkleSound.blockId = -1;
				twinkleSound.position = KnownPosition;
				Level.RelayBroadcast(twinkleSound);
			}

			if (explosionCount == 0) return;

			float force = explosionCount * 2 + 5;
			const float radius = 5.0f;
			var center = KnownPosition.ToVector3();

			foreach (Entity entity in Level.Entities.Values.Concat(Level.GetSpawnedPlayers()).ToArray())
			{
				if (entity.HealthManager.IsInvulnerable) continue;

				float distance = Vector3.Distance(entity.KnownPosition.ToVector3(), center);
				if (distance > radius) continue;

				float damage = force * (float) Math.Sqrt((radius - distance) / radius);
				if (damage > 0)
				{
					entity.HealthManager.TakeHit(null, (int) Math.Ceiling(damage), DamageCause.EntityExplosion);
				}

				// The blast push, strongest at the centre.
				Vector3 offset = entity.KnownPosition.ToVector3() - center;
				Vector3 push = offset.LengthSquared() < 0.0001f ? Vector3.UnitY : Vector3.Normalize(offset);
				push *= (1.0f - distance / radius) * 2f;

				if (entity is Player)
				{
					var motion = McpeSetEntityMotion.CreateObject();
					motion.runtimeEntityId = entity.EntityId;
					motion.velocity = push;
					Level.RelayBroadcast(motion);
				}
				else
				{
					entity.Velocity += push;
				}
			}
		}

		public override void OnTick(Entity[] entities)
		{
			if (Lifetime-- < 0)
			{
				DespawnEntity();
			}
			else
			{
				base.OnTick(entities);
			}
		}
	}
}