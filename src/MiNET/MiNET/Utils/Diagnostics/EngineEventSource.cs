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

using System.Diagnostics.Tracing;

namespace MiNET.Utils.Diagnostics
{
	/// <summary>
	///     Tier two: the forensic record, off unless something is attached with dotnet-trace.
	///     <para>
	///     This is where the identities the cardinality law bars from metric tags live. A player, a
	///     session, a single join: unbounded as a metric series, and exactly what an investigation
	///     needs. Every call site guards on <see cref="EventSource.IsEnabled()" /> first, which is a
	///     field read when nothing is listening.
	///     </para>
	///     <para>
	///     Attach with:
	///     <c>dotnet-trace collect -n MiNET.Console --providers MiNET-Engine</c>
	///     </para>
	/// </summary>
	[EventSource(Name = "MiNET-Engine")]
	public sealed class EngineEventSource : EventSource
	{
		public static readonly EngineEventSource Log = new();

		private EngineEventSource()
		{
		}

		/// <summary>
		///     One join stage finished. Read in arrival order these events ARE the join waterfall, with
		///     no activity-id plumbing needed: each carries its own elapsed-since-join-start, so a stage
		///     that took the time is identified by subtracting its predecessor.
		/// </summary>
		[Event(1, Level = EventLevel.Informational, Message = "Join stage {1} for {0} at {2}ms")]
		public void JoinStage(string username, string stage, double elapsedMillis)
		{
			WriteEvent(1, username, stage, elapsedMillis);
		}

		/// <summary>A join that never reached spawn, with the last stage it did complete.</summary>
		[Event(2, Level = EventLevel.Warning, Message = "Join abandoned by {0} after stage {1} at {2}ms")]
		public void JoinAbandoned(string username, string lastStage, double elapsedMillis)
		{
			WriteEvent(2, username, lastStage, elapsedMillis);
		}

		/// <summary>
		///     A handler that ran long enough to matter. The enforcement arm of the dispatch contract:
		///     the matching counter should read zero, and when it does not, this names who broke it.
		/// </summary>
		[Event(3, Level = EventLevel.Warning, Message = "Slow handler {1} took {2}ms for {0}")]
		public void SlowHandler(string username, string packetType, double millis)
		{
			WriteEvent(3, username, packetType, millis);
		}
	}
}
