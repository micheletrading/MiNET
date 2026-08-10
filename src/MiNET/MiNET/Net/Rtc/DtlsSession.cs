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
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using log4net;
using Org.BouncyCastle.Tls;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     One DTLS 1.2 association securing a WebRTC data channel, pinned to a peer fingerprint
	///     exchanged out of band over SDP rather than a certificate chain. There is no SRTP key
	///     export: this class blocks a thread only for the handshake (<see cref="DoHandshakeAsync" />,
	///     via <see cref="Task.Run" />) and is entirely receive-driven afterwards, with no dedicated
	///     receive thread for the life of the session. <see cref="FeedDatagram" /> both queues the raw
	///     datagram for BouncyCastle's <see cref="DatagramTransport" /> and, once the handshake is
	///     done, immediately pumps it through <see cref="DtlsTransport.Receive(Span{byte}, int)" /> and
	///     drains any records it bundled via <see cref="DtlsTransport.ReceivePending(Span{byte}, DtlsRecordCallback)" />,
	///     all inline on the caller's thread. No background thread ever exists after the handshake.
	/// </summary>
	public sealed class DtlsSession : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DtlsSession));

		// 1500 (typical path MTU) - 20 (IPv4) - 8 (UDP) = 1472. The scratch buffer is sized above
		// that to accommodate BouncyCastle bundling more than one plaintext record per receive.
		private const int WireLimit = 1472;
		private const int ScratchBufferSize = 4096;

		public delegate void DecryptedHandler(ReadOnlySpan<byte> payload);

		public event DecryptedHandler OnDecrypted;

		private readonly RtcCertificate _localCertificate;
		private readonly string _expectedRemoteFingerprint;
		private readonly bool _isServer;
		private readonly Action<ReadOnlyMemory<byte>> _sendToWire;
		private readonly Channel<(byte[] Leased, int Length)> _inbound = Channel.CreateUnbounded<(byte[] Leased, int Length)>();
		private readonly byte[] _receiveScratch = ArrayPool<byte>.Shared.Rent(ScratchBufferSize);
		private readonly DatagramTransportAdapter _transportAdapter;

		private DtlsTransport _dtlsTransport;
		private volatile bool _handshakeDone;
		private int _disposed;

		public DtlsSession(RtcCertificate localCertificate, string expectedRemoteFingerprint, bool isServer, Action<ReadOnlyMemory<byte>> sendToWire)
		{
			_localCertificate = localCertificate;
			_expectedRemoteFingerprint = expectedRemoteFingerprint;
			_isServer = isServer;
			_sendToWire = sendToWire;
			_transportAdapter = new DatagramTransportAdapter(this);
		}

		/// <summary>
		///     Feeds one raw datagram demuxed as DTLS by <see cref="IceSession.OnDtlsDatagram" />.
		///     Before the handshake completes this only queues the datagram for the blocking
		///     <see cref="Task.Run" /> handshake thread to pick up. Afterwards it also drains it
		///     immediately, inline, on the calling thread.
		/// </summary>
		public void FeedDatagram(ReadOnlySpan<byte> datagram)
		{
			if (Volatile.Read(ref _disposed) != 0) return;

			byte[] leased = ArrayPool<byte>.Shared.Rent(datagram.Length);
			datagram.CopyTo(leased);

			if (!_inbound.Writer.TryWrite((leased, datagram.Length)))
			{
				ArrayPool<byte>.Shared.Return(leased);
				return;
			}

			if (_handshakeDone) DrainPending();
		}

		/// <summary>
		///     Runs BouncyCastle's blocking handshake protocol on a pool thread; this is the one
		///     place this session ever blocks a thread. Resolves false, rather than throwing, on any
		///     handshake failure, including a fingerprint mismatch raised from the server or client
		///     certificate callbacks in <see cref="DtlsHandshakeServer" />/<see cref="DtlsHandshakeClient" />.
		/// </summary>
		public async Task<bool> DoHandshakeAsync(CancellationToken cancellationToken)
		{
			try
			{
				_dtlsTransport = await Task.Run(RunHandshake, cancellationToken).ConfigureAwait(false);
				_handshakeDone = true;
				return true;
			}
			catch (Exception ex)
			{
				Log.Debug($"DTLS handshake failed ({(_isServer ? "server" : "client")}).", ex);
				return false;
			}
		}

		private DtlsTransport RunHandshake()
		{
			if (_isServer)
			{
				var server = new DtlsHandshakeServer(_localCertificate, _expectedRemoteFingerprint);
				return new DtlsServerProtocol().Accept(server, _transportAdapter);
			}

			var client = new DtlsHandshakeClient(_localCertificate, _expectedRemoteFingerprint);
			return new DtlsClientProtocol().Connect(client, _transportAdapter);
		}

		public void SendApplicationData(ReadOnlySpan<byte> payload)
		{
			_dtlsTransport?.Send(payload);
		}

		/// <summary>
		///     One receive-plus-drain cycle per queued raw datagram: <see cref="DtlsTransport.Receive" />
		///     pulls the next queued datagram (non-blocking here, since it is already sitting in
		///     <see cref="_inbound" />) and decodes its first record; <see cref="DtlsTransport.ReceivePending" />
		///     then drains any further records BouncyCastle bundled from that same datagram without
		///     touching the queue again. The outer loop repeats for any datagram left queued from a
		///     previous call, so the session self-heals rather than falling behind.
		/// </summary>
		private void DrainPending()
		{
			DtlsTransport transport = _dtlsTransport;
			if (transport == null) return;

			// BouncyCastle's own Timeout.ForWaitMillis treats 0 the same way
			// AbstractTlsPeer.GetHandshakeTimeoutMillis' default 0 is treated: "no deadline", not
			// "don't wait". Receive(buf, 0) on a queue that is genuinely empty therefore never
			// returns -1, it spins retrying forever. The guard below only ever calls it when
			// _inbound.Reader.Count confirms a raw datagram is actually queued, so it always finds
			// data on the first attempt; ReceivePending (which never touches the queue, only
			// BouncyCastle's own already-parsed record buffer) is what may legitimately return
			// nothing, hence the plain non-blocking drain there.
			do
			{
				int n = transport.Receive(_receiveScratch.AsSpan(), 0);
				while (n > 0)
				{
					OnDecrypted?.Invoke(_receiveScratch.AsSpan(0, n));
					n = transport.ReceivePending(_receiveScratch.AsSpan(), null);
				}
			} while (_inbound.Reader.Count > 0);
		}

		/// <summary>
		///     BouncyCastle calls this with waitMillis == 0 exactly once per <see cref="FeedDatagram" />
		///     drain, to collect the datagram it just queued without ever waiting; that path skips the
		///     <see cref="CancellationTokenSource" /> entirely so steady-state receive costs no timer.
		///     A positive waitMillis only happens on the blocking <see cref="Task.Run" /> handshake
		///     thread, waiting out BouncyCastle's retransmission schedule.
		/// </summary>
		private int ReceiveFromQueue(Span<byte> buffer, int waitMillis)
		{
			if (waitMillis <= 0) return TryDeliverOne(buffer);

			bool canRead;
			using (var cts = new CancellationTokenSource(waitMillis))
			{
				try
				{
					canRead = _inbound.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();
				}
				catch (OperationCanceledException)
				{
					return -1;
				}
			}

			return canRead ? TryDeliverOne(buffer) : -1;
		}

		private int TryDeliverOne(Span<byte> buffer)
		{
			if (!_inbound.Reader.TryRead(out (byte[] Leased, int Length) item)) return -1;

			try
			{
				int copyLength = Math.Min(item.Length, buffer.Length);
				item.Leased.AsSpan(0, copyLength).CopyTo(buffer);
				return copyLength;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(item.Leased);
			}
		}

		private void SendToWire(ReadOnlySpan<byte> buffer)
		{
			byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
			try
			{
				buffer.CopyTo(rented);
				_sendToWire(rented.AsMemory(0, buffer.Length));
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(rented);
			}
		}

		/// <summary>
		///     Completes the inbound channel so a handshake blocked in <see cref="ReceiveFromQueue" />
		///     unblocks with -1 rather than hanging, closes the BouncyCastle transport if the
		///     handshake reached that far, and returns every still-queued lease to the pool.
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

			_inbound.Writer.TryComplete();

			_dtlsTransport?.Close();

			while (_inbound.Reader.TryRead(out (byte[] Leased, int Length) item))
			{
				ArrayPool<byte>.Shared.Return(item.Leased);
			}

			ArrayPool<byte>.Shared.Return(_receiveScratch);
		}

		/// <summary>
		///     BouncyCastle's <see cref="DatagramTransport" /> as seen from this session: reads come
		///     from <see cref="_inbound" />, writes go straight to <see cref="_sendToWire" />. Kept as
		///     a nested adapter rather than having <see cref="DtlsSession" /> implement the interface
		///     directly so the BouncyCastle-facing surface (byte[] AND Span overloads of every member)
		///     does not leak onto the session's own public API.
		/// </summary>
		private sealed class DatagramTransportAdapter : DatagramTransport
		{
			private readonly DtlsSession _session;

			public DatagramTransportAdapter(DtlsSession session)
			{
				_session = session;
			}

			public int GetReceiveLimit()
			{
				return WireLimit;
			}

			public int GetSendLimit()
			{
				return WireLimit;
			}

			public int Receive(byte[] buf, int off, int len, int waitMillis)
			{
				return _session.ReceiveFromQueue(buf.AsSpan(off, len), waitMillis);
			}

			public int Receive(Span<byte> buffer, int waitMillis)
			{
				return _session.ReceiveFromQueue(buffer, waitMillis);
			}

			public void Send(byte[] buf, int off, int len)
			{
				_session.SendToWire(buf.AsSpan(off, len));
			}

			public void Send(ReadOnlySpan<byte> buffer)
			{
				_session.SendToWire(buffer);
			}

			public void Close()
			{
			}
		}
	}
}