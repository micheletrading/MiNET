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
using System.Diagnostics.Metrics;

namespace MiNET.Utils.Diagnostics
{
	/// <summary>
	///     Which side of the transport asked for a lock, so <see cref="TransportMetrics.GateWaited" />
	///     says WHICH path is starving rather than only that one is. A leaf-frame profile names the
	///     blocked call; it cannot name the hold that blocked it, nor which caller pays for the wait.
	/// </summary>
	public enum GateCaller
	{
		Send,
		Receive,
		Tick
	}

	/// <summary>
	///     What triggered a resend. The two mean opposite things and want opposite fixes: a fast
	///     retransmit says the wire ORDER was wrong (the receiver saw a gap and duplicate-acked it),
	///     a timeout says the packet was too SLOW (the RTO expired before it was acknowledged).
	/// </summary>
	public enum RetransmitCause
	{
		/// <summary>T3-rtx: the retransmission timer expired (RFC 4960 6.3.3).</summary>
		Timeout,

		/// <summary>Fast retransmit: enough duplicate SACKs reported the same gap.</summary>
		Fast
	}

	/// <summary>
	///     How an acknowledgement reached the peer. Piggybacked is free, riding a packet that was
	///     leaving anyway; standalone costs its own datagram and, worse, means the ack waited for a
	///     timer because nothing was going out to carry it.
	/// </summary>
	public enum SackKind
	{
		/// <summary>Bundled into the first packet of a flush that had DATA to send.</summary>
		Piggybacked,

		/// <summary>Sent on its own, from the RFC 4960 6.2 cadence rule or the delayed-SACK fallback.</summary>
		Standalone
	}

	/// <summary>
	///     Why a datagram or chunk was thrown away. Bounded set: this is a metric tag, so it obeys the
	///     cardinality law (see the observability plan's domain taxonomy) and may only ever grow by
	///     adding a named member here.
	/// </summary>
	public enum DropReason
	{
		/// <summary>Unroutable at the mux: empty, unknown first byte, unparseable STUN, or no resolver for the ufrag.</summary>
		Route,

		/// <summary>First-contact admission budget spent (per-ufrag endpoints, or the process-wide cap).</summary>
		Admission,

		/// <summary>A peer callback threw while dispatching; the receive loop counted it and kept serving.</summary>
		Dispatch,

		/// <summary>An SCTP packet or chunk the association would not act on: bad checksum, wrong verification tag, malformed, or arriving after teardown.</summary>
		Ignored,

		/// <summary>Receive-buffer arwnd budget exhausted; the DATA chunk was not admitted.</summary>
		Budget,

		/// <summary>Too many outstanding gaps in the receive buffer; the DATA chunk was not admitted.</summary>
		GapCap,

		/// <summary>A partially reassembled fragment run was given back to reclaim receive-buffer budget (RFC 4960 6.2 renege).</summary>
		Renege
	}

	/// <summary>
	///     Process-wide transport instruments on the <c>MiNET.Net.Transport</c> meter.
	///     <para>
	///     Accumulation is <see cref="Counter{T}" />'s job, not ours. Its aggregator keeps one padded
	///     delta slot PER CORE, indexed by <c>Thread.GetCurrentProcessorId()</c>, so a counter written
	///     from every transport thread at datagram rate stays on lines those cores already own instead
	///     of dragging one shared line around the fabric. It also does nothing at all while no
	///     collector is attached. A hand-rolled field cannot match either property, and the first
	///     attempt at this file proved it: padded like the BCL's slots but with no striping, so all
	///     231,000 sends a second contended one line.
	///     </para>
	///     <para>
	///     Counters are MONOTONIC and nothing ever resets one. Rates are the READER's job: delta over
	///     measured elapsed. Gauges stay observable, reading live state on the collector's thread, and
	///     cost nothing on any hot path by construction.
	///     </para>
	///     <para>
	///     Counting points are normative and documented per instrument below. If a seam moves, its
	///     contract moves with it, because a counter whose meaning drifted is worse than no counter:
	///     the arithmetic that brackets packet loss across kernel, socket layer and this process only
	///     works while all three count the same events.
	///     </para>
	/// </summary>
	public static class TransportMetrics
	{
		public const string MeterName = "MiNET.Net.Transport";

		private static readonly Meter Meter = new(MeterName, "1.0.0");

		// Datagram seams. "Datagram" is one UDP payload, which for this transport is one DTLS record:
		// the counting point is the mux, so these are directly comparable against the kernel's own UDP
		// counters and against the BCL System.Net.Sockets meter. That bracket is what separates
		// "dropped before .NET" from "lost inside our code".
		private static readonly Counter<long> DatagramsIn = Meter.CreateCounter<long>(
			"transport.datagrams.in", "{datagram}", "UDP payloads entering the transport, counted at the mux receive loop, post-recvfrom and pre-dispatch.");

		private static readonly Counter<long> DatagramsOut = Meter.CreateCounter<long>(
			"transport.datagrams.out", "{datagram}", "UDP payloads handed to the socket send, counted at the mux send, one per sendto call.");

		private static readonly Counter<long> BytesIn = Meter.CreateCounter<long>(
			"transport.bytes.in", "By", "Wire bytes at the same seam as transport.datagrams.in, excluding UDP and IP headers.");

		private static readonly Counter<long> BytesOut = Meter.CreateCounter<long>(
			"transport.bytes.out", "By", "Wire bytes at the same seam as transport.datagrams.out, excluding UDP and IP headers.");

		// Message seams. A message is one complete game packet, so messages against datagrams is the
		// fragmentation ratio: how many datagrams the transport spends per thing the game asked it to
		// deliver.
		private static readonly Counter<long> MessagesIn = Meter.CreateCounter<long>(
			"transport.messages.in", "{message}", "Complete game packets crossing the handler seam, post-reassembly and post-ordering.");

		private static readonly Counter<long> MessagesOut = Meter.CreateCounter<long>(
			"transport.messages.out", "{message}", "Game packets accepted for send, counted pre-fragmentation.");

		private static readonly Counter<long> RetransmitCounter = Meter.CreateCounter<long>(
			"transport.retransmits", "{datagram}", "Reliability-layer resends, tagged by cause. See RetransmitCause: fast means the wire order was wrong, timeout means the packet was too slow.");

		private static readonly Counter<long> DropCounter = Meter.CreateCounter<long>(
			"transport.drops", "{datagram}", "Datagrams and chunks thrown away, by reason. See DropReason for each reason's counting point.");

		// Lock instruments. gate.held is what a profiler cannot see: a leaf frame shows the thread
		// blocked on Monitor.Enter, never how long the holder held it or what it was doing. Read as a
		// rate, gate.held is the fraction of a core-second spent inside the lock, and gate.waited
		// tagged by caller says which path pays for that.
		private static readonly Counter<long> FlushCounter = Meter.CreateCounter<long>(
			"transport.sctp.flush.count", "{flush}", "SctpAssociation.Flush calls that produced at least one packet.");

		private static readonly Counter<long> FlushPackets = Meter.CreateCounter<long>(
			"transport.sctp.flush.packets", "{packet}", "Packets emitted by flushes, so packets-per-flush is derivable against transport.sctp.flush.count.");

		private static readonly Counter<long> GateHeldMicros = Meter.CreateCounter<long>(
			"transport.sctp.gate.held", "us", "Microseconds the association gate was held across a flush, summed.");

		private static readonly Counter<long> GateWaitedMicros = Meter.CreateCounter<long>(
			"transport.sctp.gate.waited", "us", "Microseconds spent waiting to enter the association gate, summed, by which path was waiting.");

		private static readonly Counter<long> SendMicros = Meter.CreateCounter<long>(
			"transport.sctp.send.duration", "us", "Microseconds spent in the send callback, summed. Measured at 83% of the gate hold before the send was moved out of the gate.");

		private static readonly Counter<long> SackCounter = Meter.CreateCounter<long>(
			"transport.sctp.sacks", "{sack}", "Acknowledgements sent, by how they travelled. A high standalone share means acks are waiting on a timer instead of riding outgoing data.");

		private static readonly Histogram<double> SackDelay = Meter.CreateHistogram<double>(
			"transport.sctp.sack.delay", "ms", "How long an acknowledgement sat armed before it went out. This is latency added to the peer's send window, and so to how fast it can ask for the next thing.");

		private static readonly Histogram<double> SessionDuration = Meter.CreateHistogram<double>(
			"transport.sessions.duration", "s", "Session lifetime, recorded once at close.");

		private static readonly Histogram<double> Rtt = Meter.CreateHistogram<double>(
			"transport.rtt", "ms", "Smoothed RTT per session (RFC 6298 estimator), sampled per interval.");

		// Built once so no counting path ever allocates a tag or formats a string.
		private static readonly KeyValuePair<string, object>[] DropTags = BuildTags<DropReason>("reason");
		private static readonly KeyValuePair<string, object>[] GateCallerTags = BuildTags<GateCaller>("caller");
		private static readonly KeyValuePair<string, object>[] RetransmitCauseTags = BuildTags<RetransmitCause>("cause");
		private static readonly KeyValuePair<string, object>[] SackKindTags = BuildTags<SackKind>("kind");

		/// <summary>
		///     Supplies the live session count for <c>transport.sessions.active</c>. Set once by whatever
		///     holds the sessions; left null the gauge simply reports zero rather than inventing a number.
		/// </summary>
		public static Func<int> SessionCountProvider { get; set; }

		/// <summary>
		///     Total packets waiting in every session's send lane, summed. A session is never a tag (the
		///     cardinality law), so backpressure is visible as one number here and per-session only
		///     through a trace.
		/// </summary>
		public static Func<long> SendQueueDepthProvider { get; set; }

		/// <summary>Total decoded packets accepted but not yet handled, summed the same way.</summary>
		public static Func<long> DispatchQueueDepthProvider { get; set; }

		static TransportMetrics()
		{
			Meter.CreateObservableGauge("transport.sessions.active", () => SessionCountProvider?.Invoke() ?? 0, "{session}",
				"Live transport sessions.");

			Meter.CreateObservableGauge("transport.queue.send", () => SendQueueDepthProvider?.Invoke() ?? 0, "{packet}",
				"Packets waiting in every session's send lane, summed. Backpressure made visible.");

			Meter.CreateObservableGauge("transport.queue.dispatch", () => DispatchQueueDepthProvider?.Invoke() ?? 0, "{packet}",
				"Decoded packets accepted but not yet handled, summed across sessions. Nonzero means handlers are behind arrivals.");
		}

		public static void DatagramIn(int bytes)
		{
			DatagramsIn.Add(1);
			BytesIn.Add(bytes);
		}

		public static void DatagramOut(int bytes)
		{
			DatagramsOut.Add(1);
			BytesOut.Add(bytes);
		}

		public static void MessageIn() => MessagesIn.Add(1);

		public static void MessageOut() => MessagesOut.Add(1);

		public static void MessageOut(int count) => MessagesOut.Add(count);

		public static void Retransmits(RetransmitCause cause, long count) => RetransmitCounter.Add(count, RetransmitCauseTags[(int) cause]);

		public static void Dropped(DropReason reason) => DropCounter.Add(1, DropTags[(int) reason]);

		public static void Dropped(DropReason reason, long count) => DropCounter.Add(count, DropTags[(int) reason]);

		public static void FlushSent(int packets)
		{
			FlushCounter.Add(1);
			FlushPackets.Add(packets);
		}

		/// <summary>
		///     <paramref name="startedAt" /> is a <see cref="Stopwatch.GetTimestamp" /> reading taken as
		///     the gate was entered. Two timestamp reads per flush, against a hold measured in hundreds
		///     of microseconds.
		/// </summary>
		public static void GateHeld(long startedAt) => GateHeldMicros.Add(ElapsedMicros(startedAt));

		/// <summary>
		///     <paramref name="requestedAt" /> is a <see cref="Stopwatch.GetTimestamp" /> reading taken
		///     immediately BEFORE the lock statement; call this as the first thing inside it, so the
		///     difference is exactly the blocked time and nothing else.
		/// </summary>
		public static void GateWaited(GateCaller caller, long requestedAt) => GateWaitedMicros.Add(ElapsedMicros(requestedAt), GateCallerTags[(int) caller]);

		/// <summary><paramref name="stopwatchTicks" /> is a raw <see cref="Stopwatch" /> tick total for one batch's sends, converted once here rather than per packet.</summary>
		public static void SendDuration(long stopwatchTicks) => SendMicros.Add(stopwatchTicks * 1_000_000 / Stopwatch.Frequency);

		/// <summary><paramref name="armedForMillis" /> is how long the SACK sat pending; negative means it was never armed and went out on the spot.</summary>
		public static void SackSent(SackKind kind, double armedForMillis)
		{
			SackCounter.Add(1, SackKindTags[(int) kind]);
			if (armedForMillis >= 0) SackDelay.Record(armedForMillis);
		}

		public static void SessionClosed(double seconds) => SessionDuration.Record(seconds);

		public static void RecordRtt(double millis) => Rtt.Record(millis);

		private static long ElapsedMicros(long since) => (Stopwatch.GetTimestamp() - since) * 1_000_000 / Stopwatch.Frequency;

		private static KeyValuePair<string, object>[] BuildTags<T>(string name) where T : struct, Enum
		{
			T[] values = Enum.GetValues<T>();
			var tags = new KeyValuePair<string, object>[values.Length];

			// Lowercased because tag values are a wire-facing vocabulary, not C# identifiers.
			for (int i = 0; i < values.Length; i++) tags[i] = new KeyValuePair<string, object>(name, values[i].ToString().ToLowerInvariant());

			return tags;
		}
	}
}
