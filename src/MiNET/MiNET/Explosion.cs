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
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Entities.World;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET
{
	public class Explosion
	{
		private const int Ray = 16;
		private readonly IDictionary<BlockCoordinates, Block> _afectedBlocks = new Dictionary<BlockCoordinates, Block>();
		private readonly float _size;
		private readonly Level _world;
		private BlockCoordinates _centerCoordinates;
		private bool CoordsSet = false;
		private bool Fire = false;

		/// <summary>
		///     Use this for Explosion an explosion only!
		/// </summary>
		/// <param name="world"></param>
		/// <param name="centerCoordinates"></param>
		/// <param name="size"></param>
		/// <param name="fire"></param>
		public Explosion(Level world, BlockCoordinates centerCoordinates, float size, bool fire = false)
		{
			_size = size;
			_centerCoordinates = centerCoordinates;
			_world = world;
			CoordsSet = true;
			Fire = fire;
		}

		/// <summary>
		///     Only use this for SpawnTNT!
		/// </summary>
		public Explosion()
		{
			CoordsSet = false;
		}

		public bool Explode()
		{
			if (PrimaryExplosion())
			{
				return SecondaryExplosion();
			}

			return false;
		}

		private bool PrimaryExplosion()
		{
			if (!CoordsSet) throw new Exception("Please intiate using Explosion(Level, coordinates, size)");
			if (_size < 0.1) return false;

			for (int i = 0; i < Ray; i++)
			{
				for (int j = 0; j < Ray; j++)
				{
					for (int k = 0; k < Ray; k++)
					{
						if (i == 0 || i == Ray - 1 || j == 0 || j == Ray - 1 || k == 0 || k == Ray - 1)
						{
							double x = i / (Ray - 1.0F) * 2.0F - 1.0F;
							double y = j / (Ray - 1.0F) * 2.0F - 1.0F;
							double z = k / (Ray - 1.0F) * 2.0F - 1.0F;
							double dist = Math.Sqrt(x * x + y * y + z * z);

							x /= dist;
							y /= dist;
							z /= dist;
							float blastForce1 = (float) (_size * (0.7F + _world.Random.NextDouble() * 0.6F));

							double cX = _centerCoordinates.X;
							double cY = _centerCoordinates.Y;
							double cZ = _centerCoordinates.Z;

							//for (float blastForce2 = 0.3F; blastForce1 > 0.0F; blastForce1 -= blastForce2*0.75F)
							for (float blastForce2 = 0.3F; blastForce1 > 0.0F; blastForce1 -= 0.225f)
							{
								var bx = (int) Math.Floor(cX);
								var by = (int) Math.Floor(cY);
								var bz = (int) Math.Floor(cZ);
								Block block = _world.GetBlock(bx, by, bz);

								if (!(block is Air))
								{
									float blastForce3 = block.BlastResistance / 5f;
									blastForce1 -= (blastForce3 + 0.3F) * 0.3f;
								}

								if (blastForce1 > 0.0F)
								{
									if (!_afectedBlocks.ContainsKey(block.Coordinates) && !(block is Air)) _afectedBlocks.Add(block.Coordinates, block);
								}

								cX += x * blastForce2;
								cY += y * blastForce2;
								cZ += z * blastForce2;
							}
						}
					}
				}
			}

			//_size *= 2.0F;
			return true;
		}

		private bool SecondaryExplosion()
		{
			// The client-side shockwave: the huge-explosion level event, then the individual
			// block-break debris particles and block updates below.
			var explodeParticle = McpeLevelEvent.CreateObject();
			explodeParticle.eventId = (int) LevelEventType.ParticleHugeExplosion; // 2026, the TNT blast
			explodeParticle.position = new Vector3(_centerCoordinates.X + 0.5f, _centerCoordinates.Y + 0.5f, _centerCoordinates.Z + 0.5f);
			_world.RelayBroadcast(explodeParticle);

			// The boom. LevelSoundEvent Explode, at the explosion centre.
			_world.BroadcastSound(_centerCoordinates, LevelSoundEventType.Explode);

			foreach (Block block in _afectedBlocks.Values)
			{
				Block block1 = block;

				// Debris: one destroy-block particle per broken block, carrying the block's
				// runtime id so the client renders the right shards.
				var debris = McpeLevelEvent.CreateObject();
				debris.eventId = (int) LevelEventType.ParticleDestroy;
				debris.position = block1.Coordinates;
				debris.data = block1.GetRuntimeId();
				_world.RelayBroadcast(debris);

				_world.SetAir(block1.Coordinates);
				if (block is Tnt)
				{
					new Task(() => SpawnTNT(block1.Coordinates, _world)).Start();
				}
			}

			DamageEntities();

			// Set stuff on fire
			if (Fire)
			{
				Random random = new Random();
				foreach (BlockCoordinates coord in _afectedBlocks.Keys)
				{
					var block = _world.GetBlock(coord.X, coord.Y, coord.Z);
					if (block is Air)
					{
						var blockDown = _world.GetBlock(coord.X, coord.Y - 1, coord.Z);
						if (!(blockDown is Air) && random.Next(3) == 0)
						{
							_world.SetBlock(new Fire {Coordinates = block.Coordinates});
						}
					}
				}
			}

			return true;
		}

		/// <summary>
		///     Explosion damage: every entity in range takes the full force scaled by its distance
		///     from the centre, like vanilla's (1 - distance / radius) falloff. Survivors are blasted
		///     away from the centre: mobs get server-side velocity, players get the motion packet.
		/// </summary>
		private void DamageEntities()
		{
			float radius = _size * 2;
			var center = new Vector3(_centerCoordinates.X + 0.5f, _centerCoordinates.Y + 0.5f, _centerCoordinates.Z + 0.5f);

			foreach (Entity entity in _world.Entities.Values.Concat(_world.GetSpawnedPlayers()).ToArray())
			{
				if (entity.HealthManager.IsInvulnerable) continue;

				Vector3 offset = entity.KnownPosition.ToVector3() - center;
				float distance = offset.Length();
				if (distance > radius) continue;

				float damage = (float) Math.Ceiling((1.0 - distance / radius) * 6.0 * _size);
				if (damage > 0)
				{
					entity.HealthManager.TakeHit(null, (int) damage, DamageCause.BlockExplosion);
				}

				// Blast knockback, strongest at the centre.
				float force = (1.0f - distance / radius) * 4f;
				Vector3 blast = offset.LengthSquared() < 0.0001f ? Vector3.UnitY * force : Vector3.Normalize(offset) * force;

				if (entity is Player)
				{
					var motion = McpeSetEntityMotion.CreateObject();
					motion.runtimeEntityId = entity.EntityId;
					motion.velocity = blast;
					_world.RelayBroadcast(motion);
				}
				else
				{
					entity.Velocity += blast;
				}
			}
		}

		private void SpawnTNT(BlockCoordinates blockCoordinates, Level world)
		{
			var rand = new Random();
			new PrimedTnt(world)
			{
				KnownPosition = new PlayerLocation
				{
					X = blockCoordinates.X + 0.5f,
					Y = blockCoordinates.Y + 0.5f,
					Z = blockCoordinates.Z + 0.5f,
				},
				Fuse = (byte) (rand.Next(0, 20) + 10)
			}.SpawnEntity();
		}
	}
}