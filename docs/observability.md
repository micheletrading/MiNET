# Observability

MiNET instruments itself with `System.Diagnostics.Metrics` (always on, near-zero cost) and
`EventSource` (off unless something attaches). Nothing renders in-process: consumption is
`dotnet-counters`, `dotnet-trace` and `dotnet-monitor`.

## Watching a running server

```bash
dotnet-counters monitor -n MiNET.Console \
  --counters System.Net.Sockets,System.Runtime,MiNET.Net.Transport,MiNET.Engine
```

One invocation covers all three loss layers plus the runtime, which is the point: the numbers only
mean anything next to each other.

Install the tools once:

```bash
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace
```

## The meters

`MiNET.Net.Transport` counts the wire. `MiNET.Engine` counts the game loop.

Every counter is monotonic and nothing ever resets one. Rates are the reader's job: delta over
measured elapsed. `dotnet-counters` does this itself; a hand calculation must use the real interval
between two reads, never the interval it asked for.

### Transport

| Instrument | What it counts |
|---|---|
| transport.datagrams.in / .out | one UDP payload at the mux, one per recvfrom / sendto |
| transport.bytes.in / .out | wire bytes at the same seams, excluding UDP and IP headers |
| transport.messages.in / .out | one complete game packet crossing the handler seam |
| transport.retransmits | SCTP resends (T3-rtx or fast retransmit) |
| transport.drops | tagged `reason`: route, admission, dispatch, ignored, budget, gapcap, renege |
| transport.sessions.active | live sessions |
| transport.sessions.duration | session lifetime, recorded at close |
| transport.queue.send / .dispatch | packets waiting, summed across sessions |
| transport.sctp.flush.count / .flush.packets | flushes, and packets per flush |
| transport.sctp.gate.held | microseconds the association gate was held across a flush |
| transport.sctp.gate.waited | microseconds spent waiting for it, tagged `caller`: send, receive, tick |

`messages.out` divided by `datagrams.out` is the fragmentation ratio: how many datagrams the
transport spends per thing the game asked it to deliver. A number far above 1 means batches are
crossing the SCTP fragment threshold.

### Engine

| Instrument | What it counts |
|---|---|
| tick.duration | MSPT, per level tick, tagged levelType and dimension |
| tick.lag | how late the tick STARTED against its 50ms schedule |
| tick.overruns | ticks whose body exceeded 50ms |
| level.players / level.entities | per named level, tagged `level` |
| level.tick.duration | MSPT for one named level |
| broadcast.count / .movers / .bytes / .build | the movement broadcast |
| join.duration | player object created to spawned |
| join.abandoned | joins that never spawned, tagged `stage` |
| handlers.slow | handler invocations at or over the threshold, tagged `packet` |
| world.chunk.load / .encode, world.save.duration | the tick-stall suspects |

A tick that is slow and a tick that is LATE are opposite faults. `tick.duration` high means the
body is doing too much work. `tick.lag` high with `tick.duration` low means the timer is not
getting the thread, which is a scheduling or contention problem elsewhere in the process.

## Reading the gate instruments

These exist because a profiler cannot answer the question. A sampled leaf frame shows a thread
parked in `Monitor.Enter`; it cannot show how long the holder held the lock, nor which path paid
for the wait. `System.Runtime`'s `monitor.lock_contentions` gives a process-wide count with no
attribution at all.

`gate.held` read as a rate is a fraction of a core-second: 1,000,000 microseconds per second of
elapsed time means one full core is spent inside that lock. `gate.waited` tagged by caller says
which path is starving, and the three answers mean different things:

- `caller=receive` dominating: inbound packets are queueing behind the send path. SACKs go out
  late, the peer's window stays closed, throughput falls.
- `caller=send` dominating: the game cannot hand work to the transport fast enough. Broadcasts
  back up and `transport.queue.send` grows with it.
- `caller=tick` dominating: retransmit and SACK timers are firing late, which shows as
  retransmits climbing for no network reason.

Waiting costs nothing to measure when nothing is waiting: the instrument reads a clock only after
its uncontended fast path has already failed, so it cannot inflate the contention it measures.

## Bracketing packet loss

Three layers count the same events at three different places. Subtract to locate a loss.

1. Kernel: `netstat -s -p UDP`. Receive-buffer overflow (SO_RCVBUF) appears ONLY here.
2. Socket layer: the BCL's built-in `System.Net.Sockets` meter, datagrams and bytes both ways.
3. Us: `transport.datagrams.in` and `transport.drops`.

Layer 1 minus layer 2 is what was dropped before .NET ever saw it. Layer 2 minus layer 3 is what
was lost inside our code.

Calibration, worth doing once after any change to the mux: with nothing else on the box,
`transport.datagrams.out` must agree with the OS `\UDPv4\Datagrams Sent/sec`. If it does not, a
counting point moved and the arithmetic above is no longer valid.

## Turning on the forensic tier

`MiNET-Engine` carries what the cardinality law bars from metric tags: individual players,
individual joins, individual slow handlers.

```bash
dotnet-trace collect -n MiNET.Console --providers MiNET-Engine
```

Events:

- `JoinStage(username, stage, elapsedMillis)` once per stage. In arrival order these ARE the join
  waterfall; each carries elapsed-since-join-start, so the expensive stage is the one whose elapsed
  jumped from its predecessor's.
- `JoinAbandoned(username, lastStage, elapsedMillis)` for a join that never spawned.
- `SlowHandler(username, packetType, millis)` for every `handlers.slow` increment.

## Thresholds

`EngineMetrics.SlowHandlerThresholdMillis` defaults to 1ms. Below it a handler records nothing at
all: the timing is always taken (two timestamp reads) but the counter and the event only fire on a
breach. `handlers.slow` should read zero. It is the enforcement arm of the dispatch contract, since
a handler labelled verified runs inline on the transport thread, ahead of its own packet's SACK.

Raise the threshold to cut noise from a known-slow handler; never disable the measurement, or the
next violator arrives silently.

## Measuring under load

NEVER run a load test with the log root above INFO. At TRACE the two appenders take roughly 29,000
events a second, each a string format and a write behind an appender lock. That blocks threads
while burning almost no CPU, so the box reads as idle while the world tick starves. Every number
measured that way describes the logging harness, not the server.

## What is not measured

Stated so nobody reads a gap as a zero:

- `world.chunk.load` includes cache hits. `IWorldProvider.GenerateChunkColumn` does not distinguish
  a hit, a disk load and a generation, so the split is not available at that seam. Read it at p99,
  where the real work is.
- `transport.rtt` has a recording method but no caller yet: it needs a per-interval sampler over
  live associations.
- Plugin handler and command timing (the `MiNET.Plugins` meter in the plan) is not built.
