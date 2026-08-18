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

namespace MiNET.ServiceKiller
{
	/// <summary>
	///     A bot's path through the world, pluggable per run (--walker) so different tests get
	///     different movement shapes without touching the walker machinery: the shared clock,
	///     chunk verdict flushing and sliding-window forgetting live in <see cref="BotWalker" />
	///     whatever the path. Implementations are pure position generators: given where the bot
	///     stands and how many seconds this step covers, produce the next position at vanilla
	///     walking pace.
	/// </summary>
	public interface IWalkPath
	{
		/// <summary>Called once when the walk starts. start is where the bot stands, worldSpawn the level spawn, random the bot's own rng.</summary>
		void Init(Vector3 start, Vector3 worldSpawn, Random random);

		/// <summary>The next position, a step of stepSeconds at walking speed from current.</summary>
		Vector3 Next(Vector3 current, float stepSeconds);
	}

	/// <summary>
	///     The original fleet path: walk home to the level spawn first (plugins persist positions,
	///     so bots rejoin scattered), then corkscrew between the spawn and a fixed height above it
	///     for the rest of the run. Everything stays inside a 15-60 block circle at the spawn, so
	///     the whole fleet shares one dense cluster: the movement-broadcast worst case, and the
	///     shape every historical run was measured with.
	/// </summary>
	public class HelixPath : IWalkPath
	{
		private const float Height = 50f;
		private const double ClimbPerRevolution = 4.0; // a walkable ramp, ~12.5 turns to the top

		private Vector3 _worldSpawn;
		private Random _random;
		private bool _helixStarted;

		private float _radius;
		private double _climbSlope;
		private double _stepPerAngle;
		private double _floorAngle;
		private double _topAngle;
		private double _startBearing;
		private float _axisX;
		private float _axisZ;
		private float _helixCenterY;
		private double _angle;
		private int _direction = 1;

		public void Init(Vector3 start, Vector3 worldSpawn, Random random)
		{
			_worldSpawn = worldSpawn;
			_random = random;
		}

		public Vector3 Next(Vector3 current, float stepSeconds)
		{
			if (!_helixStarted)
			{
				Vector3 toSpawn = _worldSpawn - current;
				float distance = toSpawn.Length();
				if (distance >= 0.5f)
				{
					float step = Math.Min(distance, BotWalker.WalkSpeed * stepSeconds);
					return current + toSpawn * (step / distance);
				}

				InitHelix(current);
				_helixStarted = true;
			}

			_angle += _direction * BotWalker.WalkSpeed * stepSeconds / _stepPerAngle;

			// Ceiling reached: turn around and retrace. Floor reached: head back up. Only
			// the direction flips; the position is never clamped.
			if (_direction > 0 && _angle >= _topAngle) _direction = -1;
			else if (_direction < 0 && _angle <= _floorAngle) _direction = 1;

			float x = _axisX + (float) (_radius * Math.Cos(_angle + _startBearing));
			float z = _axisZ + (float) (_radius * Math.Sin(_angle + _startBearing));
			float y = (float) (_helixCenterY + _climbSlope * _angle);
			return new Vector3(x, y, z);
		}

		private void InitHelix(Vector3 center)
		{
			// Wide enough that a revolution crosses chunk boundaries for real: the sliding
			// window's rim delivery is the thing under load, and a circle that fits inside one
			// chunk never exercises it.
			_radius = _random.Next(15, 60);
			_climbSlope = ClimbPerRevolution / (2 * Math.PI); // dy/dAngle, constant
			// Constant because the ramp is linear: |dPos/dAngle| = sqrt(radius^2 + slope^2).
			_stepPerAngle = Math.Sqrt(_radius * _radius + _climbSlope * _climbSlope);

			// The band is absolute, anchored on the level spawn: floor at the spawn itself,
			// ceiling at Height above it. The approach ends within half a block of the spawn,
			// so these angles only square up the turnarounds.
			_floorAngle = (_worldSpawn.Y - center.Y) / _climbSlope;
			_topAngle = (_worldSpawn.Y + Height - center.Y) / _climbSlope;

			// The helix axis sits one radius to the side, so the path's first position (angle 0)
			// is exactly where the bot stands. Without this the first input teleports the bot
			// sideways onto the circle, which reads as a jump the moment it starts moving.
			// The side is a random bearing per bot: with a fixed one, every circle leaves the
			// spawn in the same direction and the fleet braids into a single visible rope.
			_startBearing = _random.NextDouble() * 2 * Math.PI;
			_axisX = center.X - _radius * (float) Math.Cos(_startBearing);
			_axisZ = center.Z - _radius * (float) Math.Sin(_startBearing);
			_helixCenterY = center.Y;

			_angle = 0.0;
			_direction = 1;
		}
	}

	/// <summary>
	///     Waypoint wandering over the map: pick a target inside the bounds disc around the level
	///     spawn, walk straight to it, pick the next. Spreads the fleet over real terrain, which
	///     keeps the sliding window's rim delivery hot for the whole run and gives relevance
	///     culling its intended population shape: small visible sets, paths that cross.
	///     A fraction of the targets come from a small set of HUBS every bot derives from the same
	///     fixed seed, so many routes converge on the same spots: crowds form and dissolve there,
	///     which is exactly the dense/sparse mix the transition stream has to survive.
	///     Targets keep the spawn height; bots do not track terrain, and the server does not
	///     validate movement in these runs.
	/// </summary>
	public class WaypointPath : IWalkPath
	{
		private const int HubSeed = 0x574b5054; // fixed on purpose: every bot computes the SAME hubs
		private const int HubCount = 6;
		private const double HubChance = 0.3;

		private readonly float _bounds;
		private Vector3 _worldSpawn;
		private Random _random;
		private Vector3[] _hubs;
		private Vector3 _target;
		private bool _hasTarget;

		public WaypointPath(float bounds)
		{
			_bounds = bounds;
		}

		public void Init(Vector3 start, Vector3 worldSpawn, Random random)
		{
			_worldSpawn = worldSpawn;
			_random = random;

			var hubRandom = new Random(HubSeed);
			_hubs = new Vector3[HubCount];
			for (int i = 0; i < HubCount; i++)
			{
				_hubs[i] = RandomPoint(hubRandom);
			}
		}

		public Vector3 Next(Vector3 current, float stepSeconds)
		{
			if (!_hasTarget || Vector3.Distance(current, _target) < 0.5f)
			{
				_target = _random.NextDouble() < HubChance ? _hubs[_random.Next(HubCount)] : RandomPoint(_random);
				_hasTarget = true;
			}

			Vector3 to = _target - current;
			float distance = to.Length();
			if (distance < 0.0001f) return current;

			float step = Math.Min(distance, BotWalker.WalkSpeed * stepSeconds);
			return current + to * (step / distance);
		}

		/// <summary>Uniform over the bounds disc (sqrt keeps density even; plain r would pile the points at the center).</summary>
		private Vector3 RandomPoint(Random random)
		{
			double angle = random.NextDouble() * 2 * Math.PI;
			double radius = _bounds * Math.Sqrt(random.NextDouble());
			return new Vector3(
				_worldSpawn.X + (float) (radius * Math.Cos(angle)),
				_worldSpawn.Y,
				_worldSpawn.Z + (float) (radius * Math.Sin(angle)));
		}
	}
}