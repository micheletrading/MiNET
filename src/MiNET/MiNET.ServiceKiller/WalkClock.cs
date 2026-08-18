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
using System.Threading;
using System.Threading.Tasks;

namespace MiNET.ServiceKiller
{
	/// <summary>
	///     One clock for every walking bot, replacing a sleeping thread per bot. A thread per bot
	///     paces itself with Thread.Sleep, which at fleet scale is tens of thousands of scheduler
	///     wakes per second and dominates the whole process's CPU (measured: 97% of samples in
	///     Sleep, the protocol work at noise level). This clock is one thread on an absolute
	///     schedule: tick k fires at start + k*period, plain sleeps to the next boundary, no spin
	///     (bot pacing needs no sub-millisecond precision), and a late tick never shifts the grid.
	///     <para>
	///     Spread is structural, not stochastic: every walker carries its own cadence in ticks and
	///     a registration phase, so each tick steps only the walkers due on it and the aggregate
	///     packet stream stays level by construction. The old random per-step sleeps achieved that
	///     on average; the phases achieve it exactly, and reproducibly.
	///     </para>
	/// </summary>
	public sealed class WalkClock
	{
		public const int TickPeriodMillis = 5;

		private readonly object _sync = new object();
		private readonly List<BotWalker> _walkers = new List<BotWalker>();

		// Iterated lock-free by the tick loop; rebuilt under _sync on every add/remove. Walker
		// registration is rare (once per bot per run) so rebuild cost is irrelevant.
		private volatile BotWalker[] _snapshot = Array.Empty<BotWalker>();

		private Thread _thread;
		private volatile bool _running;
		private int _phaseSeed;

		public void Start()
		{
			if (_thread != null) return;

			_running = true;
			_thread = new Thread(RunLoop) {IsBackground = true, Name = "Walk_Clock"};
			_thread.Start();
		}

		public void Stop() => _running = false;

		/// <summary>
		///     Hands a walker to the clock. The first due tick is staggered by a running phase so
		///     walkers sharing a cadence spread themselves across its ticks instead of firing as one
		///     block, whatever order they spawned in.
		/// </summary>
		public void Register(BotWalker walker)
		{
			lock (_sync)
			{
				int phase = _phaseSeed++ % Math.Max(1, walker.IntervalTicks);
				walker.ScheduleFirst(CurrentTick + 1 + phase);
				_walkers.Add(walker);
				_snapshot = _walkers.ToArray();
			}
		}

		public long CurrentTick => Interlocked.Read(ref _tick);
		private long _tick;

		private void RunLoop()
		{
			var stopwatch = Stopwatch.StartNew();
			long k = 0;
			List<BotWalker> finished = null;

			while (_running)
			{
				long targetMillis = k * TickPeriodMillis;
				int sleep = (int) (targetMillis - stopwatch.ElapsedMilliseconds);
				if (sleep > 0) Thread.Sleep(sleep);

				BotWalker[] walkers = _snapshot;
				foreach (BotWalker walker in walkers)
				{
					if (!walker.IsDue(k)) continue;

					bool keep;
					try
					{
						keep = walker.Step(k);
					}
					catch (Exception e)
					{
						// One bot's throw must never stall the whole fleet's clock.
						Console.WriteLine($"Walker {walker.Name} threw and is dropped: {e.Message}");
						keep = false;
					}

					if (!keep) (finished ??= new List<BotWalker>()).Add(walker);
				}

				if (finished is {Count: > 0})
				{
					lock (_sync)
					{
						foreach (BotWalker walker in finished) _walkers.Remove(walker);
						_snapshot = _walkers.ToArray();
					}

					// Teardown (goodbye chat, StopClient) blocks on network and pool work; it never
					// runs on the clock thread.
					foreach (BotWalker walker in finished) Task.Run(walker.Finish);
					finished.Clear();
				}

				Interlocked.Exchange(ref _tick, k);
				k++;
			}
		}
	}
}
