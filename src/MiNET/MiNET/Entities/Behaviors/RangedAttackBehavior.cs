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
using MiNET.Entities.Hostile;
using MiNET.Entities.Projectiles;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Entities.Behaviors
{
	/// <summary>
	///     Ranged attack for mobs with a bow: turns to face the target (the client renders the
	///     skeleton looking at the player it is shooting) and looses an arrow every N ticks.
	///     The arrow flies the direct line from the shooter's eye to the target's chest with a
	///     small arc. No charge/draw metadata is broadcast around the shot: the client animates
	///     the bow from its own local AI once it knows the target (TARGET_EID, key 6) and the
	///     range-attack extended flag (bit 88, key 92) are set.
	/// </summary>
	public class RangedAttackBehavior : BehaviorBase
	{
		private readonly Skeleton _entity;
		private int _cooldown;

		public RangedAttackBehavior(Skeleton entity)
		{
			_entity = entity;
		}

		public override bool ShouldStart()
		{
			return _entity.Target != null && _entity.DistanceTo(_entity.Target) < 16;
		}

		public override bool CanContinue()
		{
			return ShouldStart();
		}

		public override void OnTick(Entity[] entities)
		{
			Entity target = _entity.Target;
			if (target == null || target.HealthManager.IsDead)
			{
				return;
			}

			float distance = (float) _entity.DistanceTo(target);
			if (distance > 16 || !_entity.CanSee(target))
			{
				return;
			}

			// Face the target, same convention as LookAtPlayerBehavior (yaw = atan2(-dx, dz)).
			var dx = target.KnownPosition.X - _entity.KnownPosition.X;
			var dz = target.KnownPosition.Z - _entity.KnownPosition.Z;
			float yaw = (float) (Math.Atan2(-dx, dz) * 180.0 / Math.PI);
			_entity.KnownPosition.Yaw = yaw;
			_entity.KnownPosition.HeadYaw = yaw;
			_entity.EntityDirection = yaw;

			// Aim the head at the target's chest, like vanilla skeletons do.
			double bDiff = Math.Sqrt((dx * dx) + (dz * dz));
			double dy = (_entity.KnownPosition.Y + _entity.Height) - (target.KnownPosition.Y + 1.2);
			_entity.KnownPosition.Pitch = (float) (Math.Atan(dy / (bDiff)) * 180.0 / Math.PI);

			_entity.BroadcastMove(true);

			if (_cooldown > 0)
			{
				_cooldown--;
				return;
			}

			var eye = _entity.KnownPosition.ToVector3() + new Vector3(0, 1.6f, 0);
			var aim = target.KnownPosition.ToVector3() + new Vector3(0, 1.2f, 0) - eye;
			aim = Vector3.Normalize(aim) * 1.8f;
			aim.Y += 0.1f; // shallow arc over the flight

			var arrow = new Arrow(_entity, _entity.Level, 4)
			{
				KnownPosition = new PlayerLocation(eye, 0, 0, 0),
				Velocity = aim,
				// Client-side the arrow flies from its motion; without this flag no
				// SetEntityMotion/MoveEntityDelta ever leaves the server and nothing renders.
				BroadcastMovement = true,
			};
			arrow.KnownPosition.Yaw = (float) arrow.Velocity.GetYaw();
			arrow.KnownPosition.Pitch = (float) arrow.Velocity.GetPitch();
			arrow.SpawnEntity();

			_cooldown = 20; // one arrow per second, vanilla cadence
		}
	}
}
