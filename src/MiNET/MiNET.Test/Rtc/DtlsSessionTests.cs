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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class DtlsSessionTests
	{
		[TestMethod]
		public void Fingerprint_IsStable_AndFormatted()
		{
			var certificate = RtcCertificate.CreateSelfSigned();
			StringAssert.Matches(certificate.FingerprintSha256, new Regex("^([0-9A-F]{2}:){31}[0-9A-F]{2}$"));
		}

		[TestMethod]
		public async Task Handshake_Completes_AndCarriesApplicationData()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			// Loopback wiring without ICE or sockets: each session's sendToWire feeds the other.
			DtlsSession server = null, client = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => received.TrySetResult(payload.ToArray());
			client.SendApplicationData(new byte[] {1, 2, 3, 4});
			CollectionAssert.AreEqual(new byte[] {1, 2, 3, 4}, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
		}

		[TestMethod]
		public async Task WrongFingerprint_FailsTheHandshake()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();
			var imposter = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, imposter.FingerprintSha256, false, bytes => server.FeedDatagram(bytes));

			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsFalse(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
		}

		/// <summary>
		///     Regression for Finding 1 (drain livelock): replaying an already-seen ciphertext datagram
		///     makes BouncyCastle's DTLS anti-replay window discard it on the server, forcing
		///     DtlsRecordLayer.Receive to retry internally with nothing left queued. Before the fix
		///     (DrainPending handing BouncyCastle a waitMillis of 0, which BouncyCastle treats as "no
		///     deadline") that retry spun the caller's thread forever. FeedDatagram must now return
		///     promptly, and the session must still carry legitimate application data afterward.
		/// </summary>
		[TestMethod]
		public async Task ReplayedRecord_IsDiscarded_WithoutLivelock_AndSessionKeepsWorking()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			byte[] lastClientToServer = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				lastClientToServer = bytes.ToArray();
				server.FeedDatagram(bytes);
			});

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			var firstReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => firstReceived.TrySetResult(payload.ToArray());
			client.SendApplicationData(new byte[] {9, 9, 9});
			CollectionAssert.AreEqual(new byte[] {9, 9, 9}, await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.IsNotNull(lastClientToServer, "expected to have captured the wire datagram carrying the application data");

			byte[] replay = lastClientToServer;
			await Task.Run(() => server.FeedDatagram(replay)).WaitAsync(TimeSpan.FromSeconds(2));

			var secondReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => secondReceived.TrySetResult(payload.ToArray());
			client.SendApplicationData(new byte[] {4, 5, 6});
			CollectionAssert.AreEqual(new byte[] {4, 5, 6}, await secondReceived.Task.WaitAsync(TimeSpan.FromSeconds(5)));

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     Regression for Finding 2 (decorative cancellation): the CancellationToken passed to
		///     DoHandshakeAsync used to only gate starting the Task.Run, doing nothing once BouncyCastle's
		///     blocking Accept/Connect was already running. Against a peer that never answers, cancelling
		///     must now resolve the handshake false well before the 10 s internal handshake timeout.
		/// </summary>
		[TestMethod]
		public async Task Cancelling_TheHandshake_ResolvesFalse_WellBeforeTheHandshakeTimeout()
		{
			var clientCert = RtcCertificate.CreateSelfSigned();
			var serverCert = RtcCertificate.CreateSelfSigned();

			// Nobody on the other end: sendToWire goes nowhere, so Connect() just keeps retransmitting
			// ClientHello until either cancellation or its own 10 s handshake timeout.
			using var client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, _ => { });

			using var cts = new CancellationTokenSource();
			Task<bool> handshake = client.DoHandshakeAsync(cts.Token);
			cts.CancelAfter(TimeSpan.FromMilliseconds(300));

			Assert.IsFalse(await handshake.WaitAsync(TimeSpan.FromSeconds(3)));
		}

		/// <summary>
		///     Regression for round-2 Item 1 (reentrant Dispose defeats the gate invariant): the
		///     realistic stage-2 pattern is tearing the session down as soon as an application-data
		///     record signals an abort, i.e. calling Dispose synchronously from inside an
		///     <see cref="DtlsSession.OnDecrypted" /> subscriber, which is still further up the same
		///     call stack as FeedDatagram's drain loop (a Monitor lock is reentrant on the owning
		///     thread, so the lock alone does not stop this). Before the fix, DrainPending's `while`
		///     loop would call ReceivePending against the scratch buffer after Dispose had already
		///     returned it to the pool.
		/// </summary>
		[TestMethod]
		public async Task DisposeFromWithinOnDecrypted_DoesNotCorruptTheScratchBuffer()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload =>
			{
				received.TrySetResult(payload.ToArray());
				server.Dispose(); // Reentrant: FeedDatagram is still on the stack, holding _gate.
			};

			// Must not throw: this exercises the exact reentrant path.
			client.SendApplicationData(new byte[] {7, 7, 7});
			CollectionAssert.AreEqual(new byte[] {7, 7, 7}, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));

			// Pool sanity, best effort: renting immediately after must succeed cleanly. This cannot
			// prove an unrelated consumer elsewhere in the process was not corrupted, but a double
			// return or a still-in-use buffer handed back early is exactly the kind of bug that tends
			// to surface as a failure on the very next rent from the same shared pool.
			byte[] probe = ArrayPool<byte>.Shared.Rent(4096);
			ArrayPool<byte>.Shared.Return(probe);

			// The session no longer delivers: FeedDatagram on a disposed session is a no-op.
			bool deliveredAfterDispose = false;
			server.OnDecrypted += _ => deliveredAfterDispose = true;
			client.SendApplicationData(new byte[] {8, 8, 8});
			await Task.Delay(200);
			Assert.IsFalse(deliveredAfterDispose);

			client.Dispose();
		}

		/// <summary>
		///     Fix round, Critical finding 1(c): a caller racing an application send against
		///     <see cref="DtlsSession.Dispose" /> is a benign, expected race this class's teardown design
		///     tolerates (unlike the pre-handshake case above, a caller bug, which throws) - but before
		///     this fix, <see cref="DtlsSession.SendApplicationData" /> had no disposed guard at all, so it
		///     called straight into a <see cref="Org.BouncyCastle.Tls.DtlsTransport" /> that
		///     <see cref="DtlsSession.Dispose" /> could be concurrently <c>Close()</c>-ing on another
		///     thread (<c>_sendGate</c> and <c>_gate</c> were, and remain, disjoint locks). Must not throw,
		///     must not touch the transport, once disposed.
		/// </summary>
		[TestMethod]
		public async Task SendApplicationData_AfterDispose_IsSilentlyDropped_DoesNotThrow()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			bool clientDisposed = false;
			bool clientSentAnythingAfterDispose = false;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				if (clientDisposed) clientSentAnythingAfterDispose = true;
				server.FeedDatagram(bytes);
			});

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			client.Dispose();
			clientDisposed = true;

			// Must not throw (silently dropped, exactly like the already-covered "no delivery after
			// Dispose" case above, just exercised directly against the disposed side's own send path
			// rather than the peer's receive path) AND must never reach the wire: a guard that only
			// swallowed an exception from a still-attempted send would not be enough, since that send is
			// the exact thing racing DtlsSession.Dispose's own _dtlsTransport.Close() on another thread in
			// the real bug this fixes.
			client.SendApplicationData(new byte[] {1, 2, 3});

			Assert.IsFalse(clientSentAnythingAfterDispose, "SendApplicationData must not touch the transport at all once disposed");

			server.Dispose();
		}

		/// <summary>
		///     Regression for round 5 (concurrent FeedDatagram): before the fix, the direct-feed fast
		///     path's guard check, its copy into the shared staging buffer, and the length flag it set
		///     all ran outside the lock, so two threads could both pass the empty-channel guard and race
		///     to write the one shared buffer; the loser's datagram would be silently dropped or, worse,
		///     the buffer's contents would be corrupted mid-copy. Two threads, synchronized with a
		///     <see cref="Barrier" /> to maximise actual overlap, each feed a disjoint half of a batch of
		///     distinct, never-before-delivered wire datagrams (captured up front rather than replayed,
		///     since a genuine replay is deliberately discarded by BouncyCastle's anti-replay window,
		///     which is a different code path already covered by <see cref="ReplayedRecord_IsDiscarded_WithoutLivelock_AndSessionKeepsWorking" />).
		///     Every one of them must arrive exactly once, none dropped, none corrupted.
		/// </summary>
		[TestMethod]
		public async Task ConcurrentFeedDatagram_DeliversEveryDatagram_NoneDroppedOrCorrupted()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			bool captureOnly = false;
			var captured = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				if (captureOnly)
				{
					captured.Add(bytes.ToArray());
				}
				else
				{
					server.FeedDatagram(bytes);
				}
			});

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			// Produce 2 * perThread distinct, valid, never-yet-delivered wire datagrams: each carries
			// its own DTLS sequence number, so BouncyCastle's anti-replay window accepts all of them
			// regardless of the order the two threads below happen to feed them in.
			const int perThread = 25;
			var payloads = new List<byte[]>();
			captureOnly = true;
			for (int i = 0; i < perThread * 2; i++)
			{
				byte[] payload = {(byte) i, (byte) (i >> 8), 0xAA};
				payloads.Add(payload);
				client.SendApplicationData(payload);
			}
			captureOnly = false;
			Assert.AreEqual(perThread * 2, captured.Count, "expected to have captured one wire datagram per SendApplicationData call");

			var receivedPayloads = new ConcurrentBag<byte[]>();
			server.OnDecrypted += payload => receivedPayloads.Add(payload.ToArray());

			using var barrier = new Barrier(2);

			void Feed(int startIndex)
			{
				barrier.SignalAndWait();
				for (int i = 0; i < perThread; i++)
				{
					server.FeedDatagram(captured[startIndex + i]);
				}
			}

			Task t1 = Task.Run(() => Feed(0));
			Task t2 = Task.Run(() => Feed(perThread));
			await Task.WhenAll(t1, t2).WaitAsync(TimeSpan.FromSeconds(10));

			var deadline = DateTime.UtcNow.AddSeconds(5);
			while (receivedPayloads.Count < perThread * 2 && DateTime.UtcNow < deadline)
			{
				await Task.Delay(10);
			}

			Assert.AreEqual(perThread * 2, receivedPayloads.Count, "expected every concurrently-fed datagram to be decrypted exactly once; a drop or a corrupted copy would show up as a count mismatch here");

			List<string> expected = payloads.Select(Convert.ToHexString).OrderBy(s => s).ToList();
			List<string> actual = receivedPayloads.Select(Convert.ToHexString).OrderBy(s => s).ToList();
			CollectionAssert.AreEqual(expected, actual);

			server.Dispose();
			client.Dispose();
		}
	}
}