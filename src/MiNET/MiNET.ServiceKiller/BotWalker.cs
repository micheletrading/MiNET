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
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using MiNET.Client;
using MiNET.Net;
using MiNET.Utils.Vectors;

namespace MiNET.ServiceKiller
{
	/// <summary>
	///     One bot's walk as a step function the shared <see cref="WalkClock" /> drives, carrying
	///     the state the old per-bot thread kept in locals. The path itself is a pluggable
	///     <see cref="IWalkPath" /> (--walker selects it per run); this class owns everything
	///     around the path: the clock cadence, the chunk verdict flushing and the sliding-window
	///     forgetting. Step math is elapsed-time based, so the pace along the path is exactly
	///     vanilla walking speed whatever cadence the clock drives this walker at.
	/// </summary>
	public class BotWalker
	{
		public const float WalkSpeed = 4.317f; // vanilla walking, blocks per second

		private readonly MiNetClient _client;
		private readonly Emulator _emulator;
		private readonly TimeSpan _timeToRun;
		private readonly IWalkPath _path;
		private readonly Stopwatch _runningTime = Stopwatch.StartNew();

		private Vector3 _lastPosition;
		private ChunkCoordinates _lastChunkPos;
		private readonly Vector2 _forward = new Vector2(0, 1);
		private long _tick = 1;
		private float _lastYaw;
		private TimeSpan _lastSent;
		private long _nextDueTick;

		public string Name { get; }

		/// <summary>This walker's cadence in clock ticks, fixed per bot at construction from the configured range, so the fleet keeps its heterogeneous send rates.</summary>
		public int IntervalTicks { get; }

		public BotWalker(MiNetClient client, Emulator emulator, TimeSpan timeToRun, string name, int ranMin, int ranMax, Random random, IWalkPath path)
		{
			_client = client;
			_emulator = emulator;
			_timeToRun = timeToRun;
			Name = name;
			_path = path;

			int intervalMillis = ranMin == ranMax ? ranMin : random.Next(ranMin, ranMax);
			IntervalTicks = Math.Max(1, intervalMillis / WalkClock.TickPeriodMillis);

			PlayerLocation start = client.CurrentLocation;
			_lastPosition = new Vector3(start.X, start.Y, start.Z);
			Vector3 worldSpawn = client.WorldSpawn != default ? client.WorldSpawn : _lastPosition;
			_path.Init(_lastPosition, worldSpawn, random);
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

			// The chunk answers ride the walk timer: one batched cache status and one batched
			// sub-chunk request per step, the way a real client flushes its verdicts per tick
			// instead of answering every LevelChunk the instant it lands.
			_client.FlushChunkResponses();

			// The client's sliding window follows the walk, which is what keeps it identical to
			// the server's prune.
			var chunkPos = new ChunkCoordinates((int) _lastPosition.X >> 4, (int) _lastPosition.Z >> 4);
			if (chunkPos != _lastChunkPos)
			{
				_lastChunkPos = chunkPos;
				_client.ForgetColumnsOutsideWindow(chunkPos);
			}

			SendStep(_path.Next(_lastPosition, NextStepSeconds()));
			return true;
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
					// Closing the data channel is the disconnect, which StopClient below does.
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
