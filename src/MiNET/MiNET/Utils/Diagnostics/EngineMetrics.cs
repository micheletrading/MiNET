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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using log4net;

namespace MiNET.Utils.Diagnostics
{
	/// <summary>
	///     How far a join got. Ordered, and bounded, so it is legal as the <c>stage</c> tag on
	///     <c>join.abandoned</c>: "40% of abandoned joins died after packs" becomes a query rather than
	///     an investigation.
	/// </summary>
	public enum JoinStage
	{
		/// <summary>Nothing completed yet: the encryption handshake has not been answered.</summary>
		None,

		/// <summary>Encryption handshake answered, LoginSuccess and the pack list sent.</summary>
		Handshake,

		/// <summary>The client accepted the resource pack stack, which is what releases the join burst.</summary>
		Packs,

		/// <summary>The StartGame burst finished; chunk streaming is unblocked.</summary>
		Burst,

		/// <summary>Enough chunks published for the client to be initialized.</summary>
		Chunks,

		/// <summary>Spawned. A join that reaches here is not abandoned.</summary>
		Spawn
	}

	/// <summary>
	///     What a live level reports to the per-instance gauges. Implemented by the level itself and
	///     read only from a collector's scrape thread, so every member must be a cheap, non-blocking
	///     read of state the level already keeps - never a computation, and never anything that takes
	///     a lock the tick thread holds.
	/// </summary>
	public interface ILevelMetricsSource
	{
		/// <summary>The manager's stable id, not a per-creation identity: a reused slot must reuse its series rather than accumulate churn.</summary>
		string LevelId { get; }

		string LevelType { get; }

		string DimensionName { get; }

		int MetricPlayerCount { get; }

		int MetricEntityCount { get; }
	}

	/// <summary>
	///     Engine instruments on the <c>MiNET.Engine</c> meter: the tick heartbeat and the movement
	///     broadcast that dominates outbound traffic.
	///     <para>
	///     Tags obey the cardinality law (see the observability plan's domain taxonomy): a level's TYPE
	///     and dimension are bounded and appear here; a level's identity and a player's identity are
	///     not, and belong to per-instance curated instruments and to events respectively.
	///     </para>
	///     <para>
	///     Everything here records at tick rate (20/sec per level) or slower, so per-event histogram
	///     cost is irrelevant. Nothing on a packet-rate path records into this meter.
	///     </para>
	/// </summary>
	public static class EngineMetrics
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(EngineMetrics));

		public const string MeterName = "MiNET.Engine";

		private static readonly Meter Meter = new(MeterName, "1.0.0");

		private static readonly Histogram<double> TickDurationHistogram = Meter.CreateHistogram<double>(
			"tick.duration", "ms", "Time spent inside one level tick body. This is MSPT, with percentiles.");

		// A histogram rather than the gauge the plan first named: there are many levels, so one
		// process-wide last-value would answer for whichever level wrote last. Percentiles over the
		// same tags as tick.duration answer the question the gauge was for, and answer it per type.
		private static readonly Histogram<double> TickLagHistogram = Meter.CreateHistogram<double>(
			"tick.lag", "ms", "Scheduled-vs-actual tick start drift, so a late timer is separable from a slow tick body.");

		private static readonly Counter<long> TickOverruns = Meter.CreateCounter<long>(
			"tick.overruns", "{tick}", "Ticks whose body exceeded the 50ms budget.");

		private static readonly Counter<long> BroadcastCount = Meter.CreateCounter<long>(
			"broadcast.count", "{broadcast}", "Movement broadcasts that actually sent. The rate is the real broadcast frequency, which is not the tick rate once ticks slip.");

		private static readonly Histogram<int> BroadcastMovers = Meter.CreateHistogram<int>(
			"broadcast.movers", "{player}", "Move records in one broadcast batch.");

		private static readonly Histogram<int> BroadcastBytes = Meter.CreateHistogram<int>(
			"broadcast.bytes", "By", "Compressed payload size of one broadcast batch, which is what decides its fragment count and so the datagram rate.");

		private static readonly Histogram<double> BroadcastBuild = Meter.CreateHistogram<double>(
			"broadcast.build", "us", "Time to build and compress one broadcast batch, spent on the tick thread.");

		// The one place level IDENTITY is allowed to be a tag. Hundreds of levels at scrape rate is an
		// affordable series budget; players are not, which is why they are only ever the population of
		// an aggregate. See the plan's domain taxonomy for the cardinality law this implements.
		private static readonly ConcurrentDictionary<ILevelMetricsSource, byte> LiveLevels = new();

		private static readonly Histogram<double> LevelTickDuration = Meter.CreateHistogram<double>(
			"level.tick.duration", "ms", "Tick body duration for ONE named level, alongside the type-aggregated tick.duration.");

		public static void RegisterLevel(ILevelMetricsSource level) => LiveLevels[level] = 0;

		public static void UnregisterLevel(ILevelMetricsSource level) => LiveLevels.TryRemove(level, out _);

		public static void RecordLevelTick(double millis, in TagList tags) => LevelTickDuration.Record(millis, tags);

		private static IEnumerable<Measurement<int>> ObservePlayers()
		{
			foreach (ILevelMetricsSource level in LiveLevels.Keys) yield return new Measurement<int>(level.MetricPlayerCount, LevelTags(level));
		}

		private static IEnumerable<Measurement<int>> ObserveEntities()
		{
			foreach (ILevelMetricsSource level in LiveLevels.Keys) yield return new Measurement<int>(level.MetricEntityCount, LevelTags(level));
		}

		private static TagList LevelTags(ILevelMetricsSource level) => new()
		{
			{"level", level.LevelId},
			{"levelType", level.LevelType},
			{"dimension", level.DimensionName}
		};

		static EngineMetrics()
		{
			Meter.CreateObservableGauge("level.players", ObservePlayers, "{player}", "Players in each live level.");
			Meter.CreateObservableGauge("level.entities", ObserveEntities, "{entity}", "Entities in each live level.");
		}

		public static void RecordTick(double millis, in TagList tags)
		{
			TickDurationHistogram.Record(millis, tags);
			if (millis >= 50) TickOverruns.Add(1, tags);
		}

		public static void RecordTickLag(double millis, in TagList tags) => TickLagHistogram.Record(millis, tags);

		private static readonly Histogram<double> JoinDuration = Meter.CreateHistogram<double>(
			"join.duration", "ms", "Time from the player object existing to spawned.");

		private static readonly Counter<long> JoinAbandoned = Meter.CreateCounter<long>(
			"join.abandoned", "{join}", "Joins that never reached spawn, tagged with the last stage they did complete.");

		private static readonly Counter<long> SlowHandlers = Meter.CreateCounter<long>(
			"handlers.slow", "{handler}", "Handler invocations at or over the slow threshold, by packet type. Should read zero.");

		/// <summary>
		///     Handlers below this run entirely unrecorded: the timing is always measured (two timestamp
		///     reads), but nothing is emitted unless it breaches. That is what keeps a packet-rate path
		///     off the histogram machinery while still catching every violator.
		/// </summary>
		public static double SlowHandlerThresholdMillis { get; set; } = 1;

		/// <summary><paramref name="startedAt" /> is a <see cref="Stopwatch.GetTimestamp" /> reading from when the player object was created.</summary>
		public static void RecordJoinCompleted(long startedAt) => JoinDuration.Record(ElapsedMillis(startedAt));

		public static void RecordJoinAbandoned(JoinStage lastStage, string username, long startedAt)
		{
			JoinAbandoned.Add(1, new KeyValuePair<string, object>("stage", lastStage.ToString().ToLowerInvariant()));
			if (EngineEventSource.Log.IsEnabled()) EngineEventSource.Log.JoinAbandoned(username, lastStage.ToString(), ElapsedMillis(startedAt));
		}

		public static void RecordJoinStage(JoinStage stage, string username, long startedAt)
		{
			if (EngineEventSource.Log.IsEnabled()) EngineEventSource.Log.JoinStage(username, stage.ToString(), ElapsedMillis(startedAt));
		}

		/// <summary>How many threshold breaches demote a packet type off the inline path. Not one: a first dispatch pays JIT and cold caches, and demoting on that would punish every handler once.</summary>
		public static int DemoteAfterBreaches { get; set; } = 3;

		private static readonly ConcurrentDictionary<Type, int> HandlerBreaches = new();
		private static readonly ConcurrentDictionary<Type, byte> DemotedPackets = new();

		/// <summary>
		///     Whether this packet type has been measured too slow to keep running inline on the
		///     transport receive thread. Read on the dispatch path, so it is a lock-free lookup on a
		///     dictionary that is empty in the healthy case.
		/// </summary>
		public static bool IsDemoted(Type packetType) => !DemotedPackets.IsEmpty && DemotedPackets.ContainsKey(packetType);

		/// <summary>
		///     Called after every handler invocation with the timestamp taken before it. Returns without
		///     recording anything at all below the threshold, which is the overwhelmingly common case.
		///     <para>
		///     Past the threshold it also counts toward demotion. This is the half of the inline-dispatch
		///     contract the startup scan cannot enforce: the scan proves a handler is lock-free, never
		///     that it is FAST, and every attempt to express "fast" as a call-graph rule reduces to
		///     somebody guessing which calls are expensive. Measuring removes the guess - a handler that
		///     is genuinely slow demotes itself the first second it runs under load, and one that merely
		///     looked expensive keeps its inline path.
		///     </para>
		/// </summary>
		public static void RecordHandler(Type packetType, string username, long startedAt)
		{
			double millis = ElapsedMillis(startedAt);
			if (millis < SlowHandlerThresholdMillis) return;

			string name = packetType.Name;
			SlowHandlers.Add(1, new KeyValuePair<string, object>("packet", name));
			if (EngineEventSource.Log.IsEnabled()) EngineEventSource.Log.SlowHandler(username, name, millis);

			if (DemotedPackets.ContainsKey(packetType)) return;

			int breaches = HandlerBreaches.AddOrUpdate(packetType, 1, (_, n) => n + 1);
			if (breaches < DemoteAfterBreaches) return;

			// TryAdd, so the log line is printed by exactly one racing thread.
			if (DemotedPackets.TryAdd(packetType, 0))
			{
				Log.Warn($"Demoted {name} off inline dispatch: {breaches} handler invocations at or over {SlowHandlerThresholdMillis}ms (last {millis:F2}ms). It keeps working, on the dispatch queue, off the transport receive thread.");
			}
		}

		// The chunk pipeline, which is two halves with opposite failure modes. The server PUSHES a
		// skeleton per column; the client then PULLS blocks per section. A column whose skeleton never
		// arrives is invisible rather than late, because the client never learns it exists and so never
		// asks - so "we never pushed it" and "we pushed it and the pull failed" need telling apart, and
		// nothing could tell them apart before these.
		private static readonly Counter<long> SkeletonsSent = Meter.CreateCounter<long>(
			"chunk.skeletons.sent", "{column}", "Skeleton LevelChunk columns pushed to clients. Each is its own pre-compressed wrapper and so its own transport message.");

		private static readonly Counter<long> ChunkPassDropped = Meter.CreateCounter<long>(
			"chunk.pass.dropped", "{pass}", "Chunk streaming passes abandoned because one was already running (the TryEnter miss). A dropped pass is not retried until the player moves far enough to trigger another.");

		private static readonly Histogram<int> ChunkPassColumns = Meter.CreateHistogram<int>(
			"chunk.pass.columns", "{column}", "Columns pushed by one streaming pass.");

		private static readonly Counter<long> SubChunkResults = Meter.CreateCounter<long>(
			"chunk.subchunk.results", "{section}", "Sub-chunk request outcomes by result: success, successallair, levelchunkdoesntexist, indexoutofbounds, wrongdimension.");

		private static readonly Histogram<int> SubChunkResponseBytes = Meter.CreateHistogram<int>(
			"chunk.subchunk.bytes", "By", "Serialized size of one sub-chunk section handed to the client, before batching and compression.");

		private static readonly Histogram<double> ChunkRequestLatency = Meter.CreateHistogram<double>(
			"chunk.request.latency", "ms", "From pushing a column's skeleton to the client asking for its first sub-chunk. This is the client's own turnaround, the half of the pipeline we do not control and could not previously see.");

		private static readonly Counter<long> BlobCacheHashes = Meter.CreateCounter<long>(
			"chunk.blobcache.hashes", "{hash}", "Client blob-cache reports by status: hit (the client already held the blob), miss (it asked for the bytes), unresolved (a missed hash no longer in the store, which strands the chunk). Cache hit ratio = hit / (hit + miss).");

		private static readonly Counter<long> SubChunkReRequests = Meter.CreateCounter<long>(
			"chunk.subchunk.rerequests", "{section}", "Sub-chunk requests for a section this player was already served. The client only re-asks after a fresh skeleton re-marks the column, so each one is a section the client evicted and came back for.");

		private static readonly Histogram<int> ChunkNeverRequested = Meter.CreateHistogram<int>(
			"chunk.never.requested", "{column}", "Columns whose skeleton was pushed and which the client has still not asked a single sub-chunk for. A hole in the world looks exactly like this.");

		private static readonly Counter<long> ChunkAbandonedCounter = Meter.CreateCounter<long>(
			"chunk.abandoned", "{column}", "Columns pushed, still inside the player's radius, and left unrequested for longer than that client's own turnaround distribution allows. Not queued work: work nobody is coming back for.");

		public static void ChunkAbandoned(long columns) => ChunkAbandonedCounter.Add(columns);

		public static void RecordChunkRequestLatency(double millis) => ChunkRequestLatency.Record(millis);

		public static void ChunkNeverAsked(int columns) => ChunkNeverRequested.Record(columns);

		public static void SkeletonSent() => SkeletonsSent.Add(1);

		public static void ChunkPassSkipped() => ChunkPassDropped.Add(1);

		public static void ChunkPassCompleted(int columns) => ChunkPassColumns.Record(columns);

		public static void SubChunkResult(string result) => SubChunkResults.Add(1, new KeyValuePair<string, object>("result", result));

		public static void SubChunkBytes(int bytes) => SubChunkResponseBytes.Record(bytes);

		public static void BlobCacheReport(string status, int count)
		{
			if (count > 0) BlobCacheHashes.Add(count, new KeyValuePair<string, object>("status", status));
		}

		public static void SubChunkReRequested() => SubChunkReRequests.Add(1);

		// The tick-stall suspects. All low-rate, so a histogram per event costs nothing worth counting,
		// and a regression in any of them shows up here before it shows up as a tick overrun.
		private static readonly Histogram<double> ChunkLoad = Meter.CreateHistogram<double>(
			"world.chunk.load", "ms", "One chunk column obtained from the world provider. Cache HITS are included and sit near zero, because IWorldProvider does not separate a hit, a disk load and a generation - so read this at p99, where the real work is.");

		private static readonly Histogram<double> ChunkEncode = Meter.CreateHistogram<double>(
			"world.chunk.encode", "ms", "One chunk column encoded and compressed into its wrapper. Cache misses only: a cached batch never reaches this.");

		private static readonly Histogram<double> SaveDuration = Meter.CreateHistogram<double>(
			"world.save.duration", "ms", "One SaveChunks call, which runs on the tick thread and is the classic source of a periodic stall.");

		public static void RecordChunkLoad(long startedAt, in TagList tags) => ChunkLoad.Record(ElapsedMillis(startedAt), tags);

		public static void RecordChunkEncode(long startedAt) => ChunkEncode.Record(ElapsedMillis(startedAt));

		public static void RecordSave(long startedAt, in TagList tags) => SaveDuration.Record(ElapsedMillis(startedAt), tags);

		private static double ElapsedMillis(long since) => (Stopwatch.GetTimestamp() - since) * 1000d / Stopwatch.Frequency;

		/// <summary>
		///     One call per broadcast that actually sent, carrying everything about it: how many movers
		///     it carried, how large it compressed to, and how long the tick thread spent building it.
		///     <paramref name="startedAt" /> is a <see cref="Stopwatch.GetTimestamp" /> reading from
		///     before the batch was built.
		/// </summary>
		public static void RecordBroadcast(int movers, int bytes, long startedAt, in TagList tags)
		{
			BroadcastCount.Add(1, tags);
			BroadcastMovers.Record(movers, tags);
			BroadcastBytes.Record(bytes, tags);
			BroadcastBuild.Record((Stopwatch.GetTimestamp() - startedAt) * 1_000_000d / Stopwatch.Frequency, tags);
		}
	}
}
