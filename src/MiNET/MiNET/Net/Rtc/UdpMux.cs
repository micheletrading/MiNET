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
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MiNET.Utils.Diagnostics;
using MiNET.Utils.IO;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     One UDP socket serving every peer of this process, demultiplexed on the wire per
	///     RFC 7983: the first byte of a datagram is 0..3 for STUN, 20..63 for DTLS. A known
	///     remote endpoint routes straight to its registered <see cref="IMuxPeer" />; an unknown
	///     endpoint may only enter via a STUN binding request whose USERNAME attribute carries
	///     "localUfrag:remoteUfrag", resolved through <see cref="RegisterUfrag" />. Everything
	///     else, including DTLS from an endpoint ICE has not yet admitted, is dropped and
	///     counted in <see cref="DroppedDatagrams" />.
	/// </summary>
	public class UdpMux : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(UdpMux));

		// SIO_UDP_CONNRESET: stop an ICMP port-unreachable from a dead peer aborting the socket.
		private const int SioUdpConnReset = -1744830452;
		private const int SocketBufferSize = 1024 * 1024;

		private const int ReceiveBufferSize = 2048;
		private const int TickIntervalMs = 10;

		// A real client's NAT rebinding produces at most one, occasionally two, source endpoints for
		// the life of one ufrag, so this leaves generous headroom while still bounding how far a flood
		// that knows (or guesses) a live ufrag can grow _peers/_sendAddresses for that ufrag alone.
		internal const int MaxEndpointsPerUfrag = 8;

		// A defense-in-depth ceiling on first-contact admissions across every ufrag combined,
		// independent of the per-ufrag cap above; RemovePeer decrements this as sessions end, so a
		// long-lived server's churn never erodes the budget left for new joins.
		// Settable (NetherNetListener seeds it from Mux.MaxUnknownEndpointAdmissions in the config;
		// this layer stays config-free) because every joining client burns one admission per source
		// address its ICE checks arrive from, one per advertised server candidate, and only the
		// nominated one is returned on disconnect today. At three candidates the default budget
		// stalls new joins near 1365 concurrent sessions on a loopback fleet. Releasing the
		// non-nominated admissions at nomination is the real fix and is still owed.
		internal static int MaxUnknownEndpointAdmissions { get; set; } = 4096;

		private readonly Socket _socket;
		private readonly CancellationTokenSource _cancellation = new();
		private readonly ConcurrentDictionary<SocketAddress, PeerEntry> _peers = new();
		private readonly ConcurrentDictionary<string, Func<IPEndPoint, IMuxPeer>> _ufragResolvers = new();

		// Endpoints first contact has admitted for each ufrag so far, checked against
		// MaxEndpointsPerUfrag on every new endpoint; cleared by RemoveUfrag rather than decremented
		// per endpoint, since the only caller that ever removes individual peers (IceSession.Dispose)
		// tears the whole session, and its ufrag, down at the same time - no live session ever needs
		// its own count to shrink out from under it.
		private readonly ConcurrentDictionary<string, int> _ufragEndpointCounts = new();

		// Send-side mirror of the SocketAddress already computed for every registered peer, so
		// Send can hand the alloc-free SendTo(ReadOnlySpan<byte>, SocketFlags, SocketAddress)
		// overload a cached address instead of re-serializing the IPEndPoint on every call.
		private readonly ConcurrentDictionary<IPEndPoint, SocketAddress> _sendAddresses = new();

		// One tick thread for the whole process, however many muxes exist. HighPrecisionTimer is a
		// dedicated AboveNormal thread that busy-spins the last stretch of every period, priced for
		// there being ONE of it: a process running many muxes (a bot fleet, one mux per outgoing
		// connection) must never multiply it, fifty of them starve a 16-thread box outright. All
		// subscribed muxes' ticks run serially on this single thread.
		private static readonly object SharedTimerLock = new object();
		private static HighPrecisionTimer _sharedTimer;
		private static event Action SharedTick;
		private static int _sharedTimerSubscribers;

		private Action _sharedTickHandler;
		private long _droppedDatagrams;
		private long _dispatchFailures;
		private long _unreachableFamilyDrops;
		private int _unreachableFamilyWarned;
		private long _admittedEndpointCount;
		private long _admissionCapDrops;
		private int _started;
		private int _disposed;

		public IPEndPoint LocalEndPoint { get; }

		public long DroppedDatagrams => Interlocked.Read(ref _droppedDatagrams);

		/// <summary>
		///     Datagrams whose dispatch threw after routing (a peer callback, or a STUN parse
		///     failure other than the expected <see cref="FormatException" />). Counted and
		///     logged rather than allowed to unwind <see cref="ReceiveLoopAsync" />, which runs
		///     fire-and-forget and would otherwise go silently deaf for every peer on the mux.
		/// </summary>
		public long DispatchFailures => Interlocked.Read(ref _dispatchFailures);

		/// <summary>
		///     First-contact STUN binding requests dropped because <see cref="MaxEndpointsPerUfrag" />
		///     or <see cref="MaxUnknownEndpointAdmissions" /> was already spent - a flood that knows
		///     (or guesses) a live ufrag, counted separately from <see cref="DroppedDatagrams" />'s
		///     parse/route failures.
		/// </summary>
		public long AdmissionCapDrops => Interlocked.Read(ref _admissionCapDrops);

		/// <summary>
		///     Datagrams dropped because the destination is in an address family this socket cannot
		///     send to. See <see cref="CanSendTo" />.
		/// </summary>
		public long UnreachableFamilyDrops => Interlocked.Read(ref _unreachableFamilyDrops);

		public event Action OnTick;

		/// <summary>
		///     Whether this socket can address that peer at all. A v4 socket cannot send to a v6
		///     destination and the other way round; a dual-mode v6 socket reaches both, because a v4
		///     destination is sent as its mapped form. The peers we talk to advertise candidates in
		///     whichever families they hold, so a mixed list is normal and the unreachable half is
		///     simply not ours to use.
		/// </summary>
		public bool CanSendTo(IPAddress address)
		{
			if (address == null) return false;
			if (address.AddressFamily == _socket.AddressFamily) return true;

			return _socket.AddressFamily == AddressFamily.InterNetworkV6 && _socket.DualMode && address.AddressFamily == AddressFamily.InterNetwork;
		}

		/// <summary>
		///     Answers a non-STUN datagram from an unknown endpoint. Returns the reply to send, or
		///     null to fall through to the drop counter.
		/// </summary>
		public delegate byte[] OfflineDatagramHandler(ReadOnlySpan<byte> datagram, IPEndPoint from);

		/// <summary>
		///     Optional stateless answerer for non-STUN datagrams from unknown endpoints, consulted
		///     before they are dropped. This is where server-list discovery (the RakNet unconnected
		///     ping) attaches on a NetherNet-only server; leave null and every such datagram just
		///     drops, exactly as before. Replies are sent directly, no peer is created and nothing
		///     counts against the first-contact admission budget.
		/// </summary>
		public OfflineDatagramHandler OfflineResponder { get; set; }

		public UdpMux(IPEndPoint bindEndPoint)
		{
			_socket = new Socket(bindEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
			{
				ReceiveBufferSize = SocketBufferSize,
				SendBufferSize = SocketBufferSize
			};
			_socket.Bind(bindEndPoint);
			LocalEndPoint = (IPEndPoint) _socket.LocalEndPoint;

			if (OperatingSystem.IsWindows())
			{
				_socket.IOControl(SioUdpConnReset, new byte[] {0}, null);
			}
		}

		/// <summary>
		///     Not idempotent by design: a second call would spawn a second receive loop on the same
		///     socket and orphan the first tick timer, so it throws rather than allowing that, since a
		///     repeated call is a caller bug, not a runtime condition to tolerate.
		/// </summary>
		public void Start()
		{
			if (Interlocked.Exchange(ref _started, 1) != 0) throw new InvalidOperationException("UdpMux.Start already called.");

			_ = ReceiveLoopAsync();

			_sharedTickHandler = () => OnTick?.Invoke();
			lock (SharedTimerLock)
			{
				SharedTick += _sharedTickHandler;
				_sharedTimerSubscribers++;
				_sharedTimer ??= new HighPrecisionTimer(TickIntervalMs, _ => SharedTick?.Invoke());
			}
		}

		public void RegisterPeer(IPEndPoint remote, IMuxPeer peer)
		{
			SocketAddress address = remote.Serialize();
			_peers[address] = new PeerEntry(peer, remote, admittedByFirstContact: false);
			_sendAddresses[remote] = address;
		}

		/// <summary>
		///     Decrements <see cref="_admittedEndpointCount" /> only for an entry <see cref="HandleFirstContact" />
		///     admitted (<see cref="PeerEntry.AdmittedByFirstContact" />): an app-registered peer
		///     (<see cref="RegisterPeer" />) never counted against <see cref="MaxUnknownEndpointAdmissions" />
		///     in the first place, so removing one must not push the budget negative.
		/// </summary>
		public void RemovePeer(IPEndPoint remote)
		{
			if (_peers.TryRemove(remote.Serialize(), out PeerEntry entry) && entry.AdmittedByFirstContact)
			{
				Interlocked.Decrement(ref _admittedEndpointCount);
			}

			_sendAddresses.TryRemove(remote, out _);
		}

		public void RegisterUfrag(string localUfrag, Func<IPEndPoint, IMuxPeer> resolver)
		{
			_ufragResolvers[localUfrag] = resolver;
		}

		public void RemoveUfrag(string localUfrag)
		{
			_ufragResolvers.TryRemove(localUfrag, out _);
			_ufragEndpointCounts.TryRemove(localUfrag, out _);
		}

		public void Send(IPEndPoint to, ReadOnlySpan<byte> datagram)
		{
			// A destination this socket cannot address is a drop, never a throw. Every caller here is
			// a send or a tick path, and the family of a peer's candidate is not something they chose
			// or can do anything about: SendTo would raise WSAEAFNOSUPPORT, which unwinds an ICE tick
			// or an SCTP retransmit and takes the healthy work in it down as well.
			if (!CanSendTo(to?.Address))
			{
				Interlocked.Increment(ref _unreachableFamilyDrops);
				if (Interlocked.Exchange(ref _unreachableFamilyWarned, 1) == 0)
				{
					Log.Warn($"Dropping datagrams to {to}: this mux is bound {_socket.AddressFamily} and cannot address that family. Further drops are counted in UnreachableFamilyDrops.");
				}

				return;
			}

			// The counting point transport.datagrams.out names: one call here is one sendto, so this
			// number is directly comparable against the kernel's own UDP send counter. That comparison
			// is the calibration - if the two disagree on an otherwise idle box, this seam moved.
			TransportMetrics.DatagramOut(datagram.Length);

			// Known peer: the SocketAddress was already computed when it was registered, so the
			// alloc-free SendTo(SocketAddress) overload skips re-serializing the IPEndPoint here.
			if (_sendAddresses.TryGetValue(to, out SocketAddress address))
			{
				_socket.SendTo(datagram, SocketFlags.None, address);
				return;
			}

			// Unregistered target (e.g. a one-off send before the peer is known): pay the
			// serialization once for this call rather than growing the cache unbounded for
			// endpoints that may never be seen again.
			_socket.SendTo(datagram, SocketFlags.None, to.Serialize());
		}

		private async Task ReceiveLoopAsync()
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
			var address = new SocketAddress(_socket.AddressFamily);
			try
			{
				while (!_cancellation.IsCancellationRequested)
				{
					int received;
					try
					{
						received = await _socket.ReceiveFromAsync(buffer.AsMemory(), SocketFlags.None, address, _cancellation.Token);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (ObjectDisposedException)
					{
						break;
					}
					catch (SocketException)
					{
						// A dead peer's ICMP port-unreachable, or similar transient network noise.
						// The socket stays usable; drop this iteration and keep serving everyone else.
						continue;
					}

					TransportMetrics.DatagramIn(received);

					try
					{
						Dispatch(buffer.AsSpan(0, received), address);
					}
					catch (Exception e)
					{
						// A peer's OnStun/OnDtls (or anything else Dispatch reaches into) threw.
						// This loop is started fire-and-forget from Start(), so an unhandled
						// exception here would unwind it silently and deafen the mux for every
						// peer, not just the one that misbehaved. Count it, log it, keep serving.
						Interlocked.Increment(ref _dispatchFailures);
						TransportMetrics.Dropped(DropReason.Dispatch);
						Log.Warn("Unhandled exception dispatching a datagram; continuing the receive loop.", e);
					}
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		private void Dispatch(ReadOnlySpan<byte> data, SocketAddress from)
		{
			if (_peers.TryGetValue(from, out PeerEntry entry))
			{
				RouteToPeer(entry, data);
				return;
			}

			HandleFirstContact(data, from);
		}

		private void RouteToPeer(PeerEntry entry, ReadOnlySpan<byte> data)
		{
			if (data.Length == 0)
			{
				CountDropped();
				return;
			}

			byte first = data[0];
			if (first <= 3)
			{
				if (!TryParseStun(data, out StunMessage message))
				{
					CountDropped();
					return;
				}

				entry.Peer.OnStun(message, data, entry.EndPoint);
			}
			else if (first is >= 20 and <= 63)
			{
				entry.Peer.OnDtls(data, entry.EndPoint);
			}
			else
			{
				CountDropped();
			}
		}

		private void HandleFirstContact(ReadOnlySpan<byte> data, SocketAddress from)
		{
			// Only a STUN binding request can admit an endpoint we have never seen; ICE always
			// precedes DTLS, so unsolicited DTLS (or anything else) from an unknown endpoint is
			// dropped rather than routed anywhere. The offline responder gets one look first:
			// server-list discovery pings arrive exactly here, non-STUN and from strangers.
			if (data.Length == 0 || data[0] > 3 || !TryParseStun(data, out StunMessage message))
			{
				OfflineDatagramHandler responder = OfflineResponder;
				if (responder != null)
				{
					byte[] reply = responder(data, (IPEndPoint) LocalEndPoint.Create(from));
					if (reply != null)
					{
						// Counted here as well as in Send: this is a second sendto seam, and the
						// datagrams.out contract is one increment per sendto, whoever makes it.
						TransportMetrics.DatagramOut(reply.Length);
						_socket.SendTo(reply, SocketFlags.None, from);
						return;
					}
				}

				CountDropped();
				return;
			}

			if (message.Type != StunMessageType.BindingRequest || message.Username == null)
			{
				CountDropped();
				return;
			}

			int separator = message.Username.IndexOf(':');
			if (separator < 0)
			{
				CountDropped();
				return;
			}

			string ufrag = message.Username.Substring(0, separator);
			if (!_ufragResolvers.TryGetValue(ufrag, out Func<IPEndPoint, IMuxPeer> resolver))
			{
				CountDropped();
				return;
			}

			// Bounded admission, ahead of ever calling the resolver: an unauthenticated flood that
			// knows (or guesses) a live ufrag must not grow _peers/_sendAddresses without limit before
			// ICE integrity even has a chance to reject it (see the two constants' own remarks).
			int endpointsForUfrag = _ufragEndpointCounts.TryGetValue(ufrag, out int count) ? count : 0;
			if (endpointsForUfrag >= MaxEndpointsPerUfrag || Interlocked.Read(ref _admittedEndpointCount) >= MaxUnknownEndpointAdmissions)
			{
				long drops = Interlocked.Increment(ref _admissionCapDrops);
				TransportMetrics.Dropped(DropReason.Admission);

				// A saturated admission budget presents as a healthy server that new clients cannot
				// reach, so it must say so: loud on the first drop, then once per 1000 to survive a
				// flood without drowning the log.
				if (drops == 1 || drops % 1000 == 0)
				{
					Log.Warn($"First-contact admission dropped (total {drops}): ufrag has {endpointsForUfrag}/{MaxEndpointsPerUfrag} endpoints, {Interlocked.Read(ref _admittedEndpointCount)}/{MaxUnknownEndpointAdmissions} admissions in use.");
				}

				return;
			}

			var endPoint = (IPEndPoint) LocalEndPoint.Create(from);
			IMuxPeer peer = resolver(endPoint);
			if (peer == null)
			{
				CountDropped();
				return;
			}

			SocketAddress address = endPoint.Serialize();
			_peers[address] = new PeerEntry(peer, endPoint, admittedByFirstContact: true);
			_sendAddresses[endPoint] = address;
			_ufragEndpointCounts.AddOrUpdate(ufrag, 1, (_, existing) => existing + 1);
			Interlocked.Increment(ref _admittedEndpointCount);
			peer.OnStun(message, data, endPoint);
		}

		/// <summary>
		///     One drop, counted twice on purpose: <see cref="DroppedDatagrams" /> stays the property
		///     tests and the console read, and the meter gets the same event tagged with its reason so
		///     the loss bracketing (kernel counters, the BCL socket meter, then us) has a third layer to
		///     subtract.
		/// </summary>
		private void CountDropped()
		{
			Interlocked.Increment(ref _droppedDatagrams);
			TransportMetrics.Dropped(DropReason.Route);
		}

		private static bool TryParseStun(ReadOnlySpan<byte> data, out StunMessage message)
		{
			if (!StunMessage.LooksLikeStun(data))
			{
				message = null;
				return false;
			}

			try
			{
				message = StunMessage.Parse(data);
				return true;
			}
			catch (FormatException)
			{
				message = null;
				return false;
			}
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

			_cancellation.Cancel();

			if (_sharedTickHandler != null)
			{
				lock (SharedTimerLock)
				{
					SharedTick -= _sharedTickHandler;
					if (--_sharedTimerSubscribers == 0)
					{
						_sharedTimer?.Dispose();
						_sharedTimer = null;
					}
				}
			}

			_socket.Dispose();
			_cancellation.Dispose();
		}

		private readonly struct PeerEntry
		{
			public readonly IMuxPeer Peer;
			public readonly IPEndPoint EndPoint;

			/// <summary>Whether <see cref="HandleFirstContact" /> admitted this entry (counted against <see cref="MaxUnknownEndpointAdmissions" />), as opposed to an app-driven <see cref="RegisterPeer" /> call - see <see cref="RemovePeer" />'s own remarks for why this matters on the way out.</summary>
			public readonly bool AdmittedByFirstContact;

			public PeerEntry(IMuxPeer peer, IPEndPoint endPoint, bool admittedByFirstContact)
			{
				Peer = peer;
				EndPoint = endPoint;
				AdmittedByFirstContact = admittedByFirstContact;
			}
		}
	}
}