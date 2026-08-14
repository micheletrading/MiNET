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

		// UDP Segmentation Offload (ws2ipdef.h): with UDP_SEND_MSG_SIZE set, one SendTo of a buffer
		// holding several back-to-back segments leaves as one datagram per segment, so a run of
		// same-size fragments to one peer costs one syscall instead of one each. Windows 10 1709+,
		// and verified honoured on loopback (a 10000 byte send arrives as 8x1200 plus a 400 tail).
		// Every segment must be exactly SegmentSize except the last, so only uniform full-size
		// fragments may share a send; a short control packet (SACK, heartbeat) ends the run.
		private const int IpprotoUdp = 17;
		private const int UdpSendMsgSize = 2;

		/// <summary>
		///     Segment size for <see cref="UdpSendMsgSize" />: exactly one full wire datagram, a
		///     max-size SCTP packet plus the DTLS record it is wrapped in. Derived, not literal,
		///     because it MUST NOT be smaller than a single datagram we emit - the kernel would then
		///     split that datagram, and each piece would be an invalid DTLS record. At this value a
		///     lone send is never segmented, and only a deliberate concatenation of N full-size
		///     datagrams leaves as N.
		/// </summary>
		private const int SendSegmentSize = SctpPacket.MaxSize + DtlsRecordCrypto.RecordOverhead;
		private const int ReceiveBufferSize = 2048;
		private const int TickIntervalMs = 10;

		// A real client's NAT rebinding produces at most one, occasionally two, source endpoints for
		// the life of one ufrag, so this leaves generous headroom while still bounding how far a flood
		// that knows (or guesses) a live ufrag can grow _peers/_sendAddresses for that ufrag alone.
		internal const int MaxEndpointsPerUfrag = 8;

		// A defense-in-depth ceiling on first-contact admissions across every ufrag combined,
		// independent of the per-ufrag cap above; RemovePeer decrements this as sessions end, so a
		// long-lived server's churn never erodes the budget left for new joins.
		internal const int MaxUnknownEndpointAdmissions = 4096;

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

		public event Action OnTick;

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

			// Enables segmentation for sends larger than SendSegmentSize; sends at or below it are
			// unchanged, so this is inert until a caller actually hands over a multi-segment buffer.
			// Best effort: an older Windows build, or a platform without the option, just refuses it
			// and every send stays one datagram.
			try
			{
				_socket.SetRawSocketOption(IpprotoUdp, UdpSendMsgSize, BitConverter.GetBytes(SendSegmentSize));
				SegmentedSendEnabled = true;
			}
			catch (SocketException e)
			{
				Log.Warn($"UDP_SEND_MSG_SIZE not available, sends stay one datagram each: {e.SocketErrorCode}");
			}
		}

		/// <summary>Whether the socket accepted <see cref="UdpSendMsgSize" />, so a buffer of several <see cref="SendSegmentSize" /> segments may be handed to one <see cref="Send" />.</summary>
		public bool SegmentedSendEnabled { get; private set; }

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
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			byte first = data[0];
			if (first <= 3)
			{
				if (!TryParseStun(data, out StunMessage message))
				{
					Interlocked.Increment(ref _droppedDatagrams);
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
				Interlocked.Increment(ref _droppedDatagrams);
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
						_socket.SendTo(reply, SocketFlags.None, from);
						return;
					}
				}

				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			if (message.Type != StunMessageType.BindingRequest || message.Username == null)
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			int separator = message.Username.IndexOf(':');
			if (separator < 0)
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			string ufrag = message.Username.Substring(0, separator);
			if (!_ufragResolvers.TryGetValue(ufrag, out Func<IPEndPoint, IMuxPeer> resolver))
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			// Bounded admission, ahead of ever calling the resolver: an unauthenticated flood that
			// knows (or guesses) a live ufrag must not grow _peers/_sendAddresses without limit before
			// ICE integrity even has a chance to reject it (see the two constants' own remarks).
			int endpointsForUfrag = _ufragEndpointCounts.TryGetValue(ufrag, out int count) ? count : 0;
			if (endpointsForUfrag >= MaxEndpointsPerUfrag || Interlocked.Read(ref _admittedEndpointCount) >= MaxUnknownEndpointAdmissions)
			{
				long drops = Interlocked.Increment(ref _admissionCapDrops);

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
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			SocketAddress address = endPoint.Serialize();
			_peers[address] = new PeerEntry(peer, endPoint, admittedByFirstContact: true);
			_sendAddresses[endPoint] = address;
			_ufragEndpointCounts.AddOrUpdate(ufrag, 1, (_, existing) => existing + 1);
			Interlocked.Increment(ref _admittedEndpointCount);
			peer.OnStun(message, data, endPoint);
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