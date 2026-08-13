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
using System.Diagnostics;
using System.Numerics;
using MiNET.Client;
using MiNET.Net;
using MiNET.Utils.Vectors;

namespace MiNET.ServiceKiller
{
	/// <summary>
	///     One bot's walk as a step function the shared <see cref="WalkClock" /> drives, carrying
	///     the state the old per-bot thread kept in locals. The path is unchanged: walk home to the
	///     level spawn first (plugins persist positions, so bots rejoin scattered), then corkscrew
	///     between the spawn and a fixed height above it for the rest of the run. Step math is
	///     elapsed-time based, so the pace along the path is exactly vanilla walking speed whatever
	///     cadence the clock drives this walker at.
	/// </summary>
	public class BotWalker
	{
		private const float WalkSpeed = 4.317f; // vanilla walking, blocks per second
		private const float Height = 50f;
		private const double ClimbPerRevolution = 4.0; // a walkable ramp, ~12.5 turns to the top

		private enum WalkState
		{
			Approach,
			Helix
		}

		private readonly MiNetClient _client;
		private readonly Emulator _emulator;
		private readonly TimeSpan _timeToRun;
		private readonly bool _useNetherNet;
		private readonly Random _random;
		private readonly Stopwatch _runningTime = Stopwatch.StartNew();

		private WalkState _state = WalkState.Approach;
		private Vector3 _worldSpawn;
		private Vector3 _lastPosition;
		private readonly Vector2 _forward = new Vector2(0, 1);
		private long _tick = 1;
		private float _lastYaw;
		private TimeSpan _lastSent;
		private long _nextDueTick;

		// Helix parameters, computed on the transition out of Approach, where the anchor position
		// is finally known.
		private float _radius;
		private double _climbSlope;
		private double _stepPerAngle;
		private double _floorAngle;
		private double _topAngle;
		private double _startBearing;
		private float _axisX;
		private float _axisZ;
		private double _angle;
		private int _direction = 1;

		public string Name { get; }

		/// <summary>This walker's cadence in clock ticks, fixed per bot at construction from the configured range, so the fleet keeps its heterogeneous send rates.</summary>
		public int IntervalTicks { get; }

		public BotWalker(MiNetClient client, Emulator emulator, TimeSpan timeToRun, string name, int ranMin, int ranMax, bool useNetherNet, Random random)
		{
			_client = client;
			_emulator = emulator;
			_timeToRun = timeToRun;
			Name = name;
			_useNetherNet = useNetherNet;
			_random = random;

			int intervalMillis = ranMin == ranMax ? ranMin : random.Next(ranMin, ranMax);
			IntervalTicks = Math.Max(1, intervalMillis / WalkClock.TickPeriodMillis);

			PlayerLocation start = client.CurrentLocation;
			_lastPosition = new Vector3(start.X, start.Y, start.Z);
			_worldSpawn = client.WorldSpawn != default ? client.WorldSpawn : _lastPosition;
			_lastSent = _runningTime.Elapsed;
		}

		public void ScheduleFirst(long dueTick) => _nextDueTick = dueTick;

		public bool IsDue(long clockTick) => clockTick >= _nextDueTick;

		/// <summary>
		///     Advances the walk by one send. Returns false when this walker is done (duration
		///     elapsed, emulator stopping, or connection gone); the clock then unregisters it and
		///     runs <see cref="Finish" /> off the clock thread.
		/// </summary>
		public bool Step(long clockTick)
		{
			if (!_emulator.Running || _runningTime.Elapsed >= _timeToRun || !_client.IsConnected) return false;

			_nextDueTick = clockTick + IntervalTicks;

			switch (_state)
			{
				case WalkState.Approach:
				{
					Vector3 toSpawn = _worldSpawn - _lastPosition;
					float distance = toSpawn.Length();
					if (distance < 0.5f)
					{
						InitHelix();
						_state = WalkState.Helix;
						return true;
					}

					float step = Math.Min(distance, WalkSpeed * NextStepSeconds());
					SendStep(_lastPosition + toSpawn * (step / distance));
					return true;
				}

				case WalkState.Helix:
				{
					_angle += _direction * WalkSpeed * NextStepSeconds() / _stepPerAngle;

					// Ceiling reached: turn around and retrace. Floor reached: head back up. Only
					// the direction flips; the position is never clamped.
					if (_direction > 0 && _angle >= _topAngle) _direction = -1;
					else if (_direction < 0 && _angle <= _floorAngle) _direction = 1;

					float x = _axisX + (float) (_radius * Math.Cos(_angle + _startBearing));
					float z = _axisZ + (float) (_radius * Math.Sin(_angle + _startBearing));
					float y = (float) (_helixCenterY + _climbSlope * _angle);
					SendStep(new Vector3(x, y, z));
					return true;
				}
			}

			return false;
		}

		private float _helixCenterY;

		private void InitHelix()
		{
			PlayerLocation center = _client.CurrentLocation;
			_radius = _random.Next(5, 20);
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

		/// <summary>
		///     The step budget for this send. Capped so a stalled bot (receive flood, GC,
		///     scheduling) resumes walking from where it was instead of teleporting to where the
		///     clock says it should be.
		/// </summary>
		private float NextStepSeconds()
		{
			TimeSpan now = _runningTime.Elapsed;
			float dt = (float) (now - _lastSent).TotalSeconds;
			_lastSent = now;
			return Math.Min(dt, 0.2f);
		}

		/// <summary>Sends one movement step facing the direction of travel. The old thread's per-step sleep is gone; pacing is the clock's job now.</summary>
		private void SendStep(Vector3 position)
		{
			Vector3 posDelta = position - _lastPosition;
			float length = posDelta.Length();

			float yaw = _lastYaw;
			float pitch = 0f;
			if (length > 0.0001f)
			{
				yaw = (float) ((Math.Atan2(-posDelta.X, posDelta.Z).ToDegrees() + 360) % 360);
				pitch = (float) (-Math.Asin(posDelta.Y / length)).ToDegrees();
			}
			_lastYaw = yaw;

			var input = McpePlayerAuthInput.CreateObject();
			input.playerRotation = new Vector2(pitch, yaw);
			input.playerHeadRotation = yaw;
			input.position = position;
			input.moveVector = _forward;
			input.inputData = AuthInputFlags.BlockBreakingDelayEnabled | AuthInputFlags.WalkForwards;
			input.inputMode = McpePlayerAuthInput.InputMode.Mouse;
			input.playMode = McpePlayerAuthInput.ClientPlayMode.Normal;
			input.newInteractionModel = McpePlayerAuthInput.NewInteractionModel.Touch;
			input.interactRotation = new Vector2(pitch, yaw);
			input.clientTick = _tick++;
			input.posDelta = posDelta;
			input.analogMoveVector = _forward;
			input.rawMoveVector = _forward;
			input.cameraOrientation = new Vector3(0, yaw, 0);
			_client.SendPacket(input);

			_lastPosition = position;
			_client.CurrentLocation = new PlayerLocation(position, yaw, yaw, pitch);
		}

		/// <summary>Runs off the clock thread (it blocks on network teardown): the goodbye and the client stop, the tail of the old per-bot thread.</summary>
		public void Finish()
		{
			try
			{
				if (_client.IsConnected)
				{
					_client.SendChat("Shadow gov agent BREXITING!");
					// RakNet-level goodbye; on NetherNet closing the data channel is the disconnect,
					// which StopClient below does.
					if (!_useNetherNet) _client.SendDisconnectionNotification();
				}

				_client.StopClient();
				Console.WriteLine($"{_runningTime.ElapsedMilliseconds} Client stopped. {_client.IsConnected}, {_emulator.Running}");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}
