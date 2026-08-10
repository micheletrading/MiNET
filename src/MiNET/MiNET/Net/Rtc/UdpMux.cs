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
		// SIO_UDP_CONNRESET: stop an ICMP port-unreachable from a dead peer aborting the socket.
		private const int SioUdpConnReset = -1744830452;
		private const int SocketBufferSize = 1024 * 1024;
		private const int ReceiveBufferSize = 2048;
		private const int TickIntervalMs = 10;

		private readonly Socket _socket;
		private readonly CancellationTokenSource _cancellation = new();
		private readonly ConcurrentDictionary<SocketAddress, PeerEntry> _peers = new();
		private readonly ConcurrentDictionary<string, Func<IPEndPoint, IMuxPeer>> _ufragResolvers = new();

		private HighPrecisionTimer _timer;
		private long _droppedDatagrams;
		private bool _disposed;

		public IPEndPoint LocalEndPoint { get; }

		public long DroppedDatagrams => Interlocked.Read(ref _droppedDatagrams);

		public event Action OnTick;

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

		public void Start()
		{
			_ = ReceiveLoopAsync();
			_timer = new HighPrecisionTimer(TickIntervalMs, _ => OnTick?.Invoke());
		}

		public void RegisterPeer(IPEndPoint remote, IMuxPeer peer)
		{
			_peers[remote.Serialize()] = new PeerEntry(peer, remote);
		}

		public void RemovePeer(IPEndPoint remote)
		{
			_peers.TryRemove(remote.Serialize(), out _);
		}

		public void RegisterUfrag(string localUfrag, Func<IPEndPoint, IMuxPeer> resolver)
		{
			_ufragResolvers[localUfrag] = resolver;
		}

		public void RemoveUfrag(string localUfrag)
		{
			_ufragResolvers.TryRemove(localUfrag, out _);
		}

		public void Send(IPEndPoint to, ReadOnlySpan<byte> datagram)
		{
			_socket.SendTo(datagram, SocketFlags.None, to);
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

					Dispatch(buffer.AsSpan(0, received), address);
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
			// dropped rather than routed anywhere.
			if (data.Length == 0 || data[0] > 3 || !TryParseStun(data, out StunMessage message))
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			if (message.Type != StunMessageType.BindingRequest || message.Username == null)
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			int separator = message.Username.IndexOf(':');
			if (separator < 0 || !_ufragResolvers.TryGetValue(message.Username.Substring(0, separator), out Func<IPEndPoint, IMuxPeer> resolver))
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			var endPoint = (IPEndPoint) LocalEndPoint.Create(from);
			IMuxPeer peer = resolver(endPoint);
			if (peer == null)
			{
				Interlocked.Increment(ref _droppedDatagrams);
				return;
			}

			_peers[endPoint.Serialize()] = new PeerEntry(peer, endPoint);
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
			if (_disposed) return;
			_disposed = true;

			_cancellation.Cancel();
			_timer?.Dispose();
			_socket.Dispose();
			_cancellation.Dispose();
		}

		private readonly struct PeerEntry
		{
			public readonly IMuxPeer Peer;
			public readonly IPEndPoint EndPoint;

			public PeerEntry(IMuxPeer peer, IPEndPoint endPoint)
			{
				Peer = peer;
				EndPoint = endPoint;
			}
		}
	}
}