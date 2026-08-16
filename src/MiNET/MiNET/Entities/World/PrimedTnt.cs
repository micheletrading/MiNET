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
using System.Numerics;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Metadata;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Entities.World
{
	public class PrimedTnt : Entity
	{
		public byte Fuse { get; set; }
		public bool Fire { get; set; }

		public PrimedTnt(Level level) : base(EntityType.PrimedTnt, level)
		{
			IsIgnited = true;
			NoAi = false;
			HasCollision = true;

			Gravity = 0.04;
			Drag = 0.02;
		}

		public override MetadataDictionary GetMetadata()
		{
			return new MetadataDictionary
			{
				[(int) MetadataFlags.EntityFlags] = new MetadataLong(GetDataValue()),
				[(int) MetadataFlags.DataFuseLength] = new MetadataInt(Fuse)
			};
		}

		public override void SpawnEntity()
		{
			// Vanilla TNT blasts set fire with ~1/3 chance per exposed block (see Explosion.SecondaryExplosion).
			Fire = true;

			base.SpawnEntity();

			// The fuse hiss, so the priming is audible (chain reactions prime through this path too).
			var fuse = McpeLevelEvent.CreateObject();
			fuse.eventId = (int) LevelEventType.SoundTNTFuse;
			fuse.position = KnownPosition;
			fuse.data = 0;
			Level.RelayBroadcast(fuse);
		}

		public override void OnTick(Entity[] entities)
		{
			Fuse--;

			if (Fuse == 0)
			{
				DespawnEntity();
				// Honor the tntExplodes game rule: the block still primes and hops, but never detonates.
				if (Level.TntExplodes) Explode();
			}
			else
			{
				// The vanilla hop: every 10 fuse ticks a planted TNT springs off the ground, and the
				// landing plants it again. A 0.15 hop has ~9 ticks of airtime with this gravity and
				// drag - shorter than the 10-tick cadence, so the TNT lands between hops and dances
				// every half second. Bigger hops (0.3) fly for ~16 ticks and skip every other hop,
				// which reads as floating; an ungated reset outpaces gravity and climbs.
				if (Fuse % 10 == 0 && _isOnGround)
				{
					Velocity = new Vector3((float) ((Level.Random.NextDouble() - 0.5) * 0.04), 0.15f, (float) ((Level.Random.NextDouble() - 0.5) * 0.04));
					_isOnGround = false;
				}

				PositionCheck();

				// Only broadcast movement when it changed: a planted TNT sits still for most of the
				// fuse, and the client should not be re-fed the same position every tick.
				if (LastSentPosition != null)
				{
					bool moved = LastSentPosition.X != KnownPosition.X
						|| LastSentPosition.Y != KnownPosition.Y
						|| LastSentPosition.Z != KnownPosition.Z
						|| LastSentPosition.Yaw != KnownPosition.Yaw
						|| LastSentPosition.Pitch != KnownPosition.Pitch
						|| LastSentPosition.HeadYaw != KnownPosition.HeadYaw
						|| _isOnGround != _lastSentOnGround;
					if (moved)
					{
						var move = McpeMoveEntityDelta.CreateObject();
						move.runtimeEntityId = EntityId;
						move.prevSentPosition = LastSentPosition;
						move.currentPosition = (PlayerLocation) KnownPosition.Clone();
						move.isOnGround = _isOnGround;
						if (move.SetFlags())
						{
							Level.RelayBroadcast(move);
							_lastSentOnGround = _isOnGround;
						}
					}
				}

				LastSentPosition = (PlayerLocation) KnownPosition.Clone();

				var entityData = McpeSetEntityData.CreateObject();
				entityData.runtimeEntityId = EntityId;
				entityData.metadata = GetMetadata();
				Level.RelayBroadcast(entityData);
			}
		}

		private bool _isOnGround;
		private bool _lastSentOnGround;

		private void PositionCheck()
		{
			if (_isOnGround)
			{
				// Planted: the horizontal slide dies out, no gravity while resting.
				Velocity = new Vector3(Velocity.X * 0.7f, 0, Velocity.Z * 0.7f);
				return;
			}

			if (KnownPosition.Y > -1)
			{
				Velocity -= new Vector3(0, (float) Gravity, 0);
				Velocity *= (float) (1.0f - Drag);
			}

			KnownPosition.X += (float) Velocity.X;
			KnownPosition.Y += (float) Velocity.Y;
			KnownPosition.Z += (float) Velocity.Z;

			if (Velocity.Y > 0) return; // still rising

			// Ground detection covering this tick's whole fall path, so a fast fall cannot tunnel
			// through the floor. The client renders the primed TNT centered on the entity position,
			// so it rests with its center in the middle of the block above the floor - the block it
			// was primed on, exactly where it spawned (floorTop + 0.5). Resting at floorTop is what
			// made it sink half a block into the ground.
			int distance = Math.Max(2, (int) Math.Ceiling(-Velocity.Y) + 1);
			BlockCoordinates check = new BlockCoordinates((int) Math.Floor(KnownPosition.X), (int) Math.Floor(KnownPosition.Y + 0.49f), (int) Math.Floor(KnownPosition.Z));
			for (int i = 0; i < distance; i++)
			{
				if (Level.GetBlock(check).IsSolid)
				{
					_isOnGround = true;
					KnownPosition.Y = check.Y + 1.5f;
					return;
				}
				check = check.BlockDown();
			}
		}

		private void Explode()
		{
			// Litteral "fire and forget"
			new Explosion(Level,
					new BlockCoordinates((int) Math.Floor(KnownPosition.X), (int) Math.Floor(KnownPosition.Y), (int) Math.Floor(KnownPosition.Z)), 4, Fire)
				.Explode();
		}
	}
}