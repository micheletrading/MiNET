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
using System.Security.Cryptography;
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

		/// <summary>
		///     Pins the coupling <see cref="DtlsSession.MaxSendPayloadLength" />'s own comment names: the
		///     DTLS layer must accept anything SCTP can ever hand it, since every <see cref="SctpAssociation" />
		///     send path builds into an <see cref="SctpPacket.MaxSize" /> buffer. If this ever shrank below
		///     that, a legitimate full-size SCTP packet would start throwing out of
		///     <see cref="DtlsSession.SendApplicationData" /> instead of being sent.
		/// </summary>
		[TestMethod]
		public void MaxSendPayloadLength_IsAtLeastTheSctpPacketCeiling()
		{
			Assert.IsTrue(DtlsSession.MaxSendPayloadLength >= SctpPacket.MaxSize);
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

		/// <summary>
		///     Pins that the key block <see cref="CapturingTlsCrypto" /> captures out of the handshake
		///     is the actual material BouncyCastle used to protect a real record, not merely present.
		///     Both sides derive the same key block from the same master secret, so the two captures
		///     must agree field for field; and a manual AES-GCM decrypt of one wire datagram
		///     BouncyCastle itself encrypted, built entirely from the DTLS 1.2 record format (record
		///     header, explicit nonce, AAD per RFC 5246/RFC 5288) and the captured client write
		///     key/salt, must recover the exact plaintext that was sent.
		/// </summary>
		[TestMethod]
		public async Task Handshake_CapturesTheKeyBlock_AndItDecryptsARealBcRecord()
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

			CapturedDtlsKeys clientSideKeys = client.CapturedKeys;
			CapturedDtlsKeys serverSideKeys = server.CapturedKeys;
			Assert.IsNotNull(clientSideKeys, "expected the client-role handshake to have captured a key block");
			Assert.IsNotNull(serverSideKeys, "expected the server-role handshake to have captured a key block");

			Assert.IsTrue(clientSideKeys.ClientWriteKey.Length == 16 || clientSideKeys.ClientWriteKey.Length == 32, "key length must match one of the two negotiated AES-GCM suites");
			Assert.AreEqual(clientSideKeys.ClientWriteKey.Length, clientSideKeys.ServerWriteKey.Length);
			Assert.AreEqual(4, clientSideKeys.ClientWriteIv.Length);
			Assert.AreEqual(4, clientSideKeys.ServerWriteIv.Length);

			CollectionAssert.AreEqual(clientSideKeys.ClientWriteKey, serverSideKeys.ClientWriteKey, "both sides derive the same key block from the same master secret");
			CollectionAssert.AreEqual(clientSideKeys.ServerWriteKey, serverSideKeys.ServerWriteKey);
			CollectionAssert.AreEqual(clientSideKeys.ClientWriteIv, serverSideKeys.ClientWriteIv);
			CollectionAssert.AreEqual(clientSideKeys.ServerWriteIv, serverSideKeys.ServerWriteIv);
			Assert.AreEqual(clientSideKeys.CipherSuite, serverSideKeys.CipherSuite);

			byte[] plaintext = {10, 20, 30, 40, 50};
			client.SendApplicationData(plaintext);
			Assert.IsNotNull(lastClientToServer, "expected to have captured the wire datagram BouncyCastle encrypted");

			byte[] recovered = ManuallyDecryptOneAeadRecord(lastClientToServer, clientSideKeys.ClientWriteIv, clientSideKeys.ClientWriteKey);
			CollectionAssert.AreEqual(plaintext, recovered);

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     Decodes exactly one DTLS 1.2 AEAD record: header
		///     type(1)|version(2)|epoch(2)|sequence(6)|length(2) (RFC 6347 4.1), fragment
		///     explicit_nonce(8)|ciphertext|tag(16), GCM nonce = salt(4) + explicit_nonce(8), AAD =
		///     epoch(2)|sequence(6)|type(1)|version(2)|plaintext_length(2) (RFC 5246 6.2.3.3, RFC 5288).
		///     The explicit nonce is read off the wire, never reconstructed from the header: RFC 5288
		///     requires a receiver to use the nonce as sent, not to recompute it.
		/// </summary>
		private static byte[] ManuallyDecryptOneAeadRecord(byte[] record, byte[] writeIvSalt, byte[] writeKey)
		{
			byte contentType = record[0];
			Assert.AreEqual(0xFE, record[1], "DTLS 1.2 record version high byte");
			Assert.AreEqual(0xFD, record[2], "DTLS 1.2 record version low byte");

			int fragmentLength = (record[11] << 8) | record[12];
			ReadOnlySpan<byte> fragment = record.AsSpan(13, fragmentLength);
			ReadOnlySpan<byte> explicitNonce = fragment.Slice(0, 8);
			ReadOnlySpan<byte> ciphertext = fragment.Slice(8, fragment.Length - 8 - 16);
			ReadOnlySpan<byte> tag = fragment.Slice(fragment.Length - 16, 16);

			Span<byte> nonce = stackalloc byte[12];
			writeIvSalt.CopyTo(nonce);
			explicitNonce.CopyTo(nonce.Slice(4));

			Span<byte> aad = stackalloc byte[13];
			record.AsSpan(3, 8).CopyTo(aad); // epoch(2) + sequence(6), straight off the record header
			aad[8] = contentType;
			aad[9] = record[1];
			aad[10] = record[2];
			aad[11] = (byte) (ciphertext.Length >> 8);
			aad[12] = (byte) ciphertext.Length;

			byte[] plaintext = new byte[ciphertext.Length];
			using var aesGcm = new AesGcm(writeKey, 16);
			aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
			return plaintext;
		}

		/// <summary>
		///     <see cref="CapturedDtlsKeys" /> holds plaintext copies of both write keys and IVs for the
		///     session's whole lifetime; <see cref="DtlsSession.Dispose" /> zeroes them once
		///     <see cref="DtlsRecordCrypto" /> (which uses the two IV arrays as its live send/receive
		///     salts for as long as it runs) is itself disposed. The properties keep returning the same
		///     array instances afterward - only their contents change.
		/// </summary>
		[TestMethod]
		public async Task Dispose_ZeroesTheCapturedKeyMaterial()
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

			CapturedDtlsKeys keys = server.CapturedKeys;
			Assert.IsTrue(keys.ClientWriteKey.Any(b => b != 0), "sanity: expected real key material before dispose");
			Assert.IsTrue(keys.ServerWriteKey.Any(b => b != 0), "sanity: expected real key material before dispose");

			server.Dispose();

			CollectionAssert.AreEqual(new byte[keys.ClientWriteKey.Length], keys.ClientWriteKey, "expected the client write key to be zeroed on dispose");
			CollectionAssert.AreEqual(new byte[keys.ServerWriteKey.Length], keys.ServerWriteKey, "expected the server write key to be zeroed on dispose");
			CollectionAssert.AreEqual(new byte[keys.ClientWriteIv.Length], keys.ClientWriteIv, "expected the client write IV to be zeroed on dispose");
			CollectionAssert.AreEqual(new byte[keys.ServerWriteIv.Length], keys.ServerWriteIv, "expected the server write IV to be zeroed on dispose");

			client.Dispose();
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
		///     Replaying an already-seen ciphertext datagram
		///     makes BouncyCastle's DTLS anti-replay window discard it on the server, forcing
		///     DtlsRecordLayer.Receive to retry internally with nothing left queued. A waitMillis of 0
		///     (which BouncyCastle treats as "no deadline") would spin the caller's thread forever on
		///     that retry; FeedDatagram must return
		///     promptly instead, and the session must still carry legitimate application data afterward.
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
		///     Pins that the CancellationToken passed to
		///     DoHandshakeAsync unblocks a handshake already running inside BouncyCastle's blocking
		///     Accept/Connect, not only gating the start of the Task.Run. Against a peer that never
		///     answers, cancelling must resolve the handshake false well before the 10 s internal
		///     handshake timeout.
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
		///     Tearing the session down as soon as an application-data
		///     record signals an abort - calling Dispose synchronously from inside an
		///     <see cref="DtlsSession.OnDecrypted" /> subscriber, which is still further up the same
		///     call stack as FeedDatagram's drain loop - is a realistic pattern (a Monitor lock is
		///     reentrant on the owning thread, so the lock alone does not stop this). Without the
		///     deferred-return guard, DrainPending's `while`
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
		///     A caller racing an application send against
		///     <see cref="DtlsSession.Dispose" /> is a benign, expected race this class's teardown design
		///     tolerates (unlike the pre-handshake case above, a caller bug, which throws). Without a
		///     disposed guard, <see cref="DtlsSession.SendApplicationData" /> would call
		///     straight into a <see cref="Org.BouncyCastle.Tls.DtlsTransport" /> that
		///     <see cref="DtlsSession.Dispose" /> could be concurrently <c>Close()</c>-ing on another
		///     thread (<c>_sendGate</c> and <c>_gate</c> are disjoint locks). Must not throw,
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
		///     Pins concurrent <see cref="DtlsSession.FeedDatagram" /> safety: without the whole
		///     decide-copy-drain sequence running inside the lock, the direct-feed fast
		///     path's guard check, its copy into the shared staging buffer, and the length flag it sets
		///     would all run outside the lock, so two threads could both pass the empty-channel guard and
		///     race to write the one shared buffer; the loser's datagram would be silently dropped or,
		///     worse, the buffer's contents would be corrupted mid-copy. Two threads, synchronized with a
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

		/// <summary>
		///     Once the handshake is done, BouncyCastle never touches another byte of application data on
		///     either side. Both sessions exercise the native path at once, in both directions, well past
		///     the 64-wide replay window and the low sequence numbers BouncyCastle's own Finished flight
		///     used.
		/// </summary>
		[TestMethod]
		public async Task NativeRecordLayer_ExchangesOneThousandDatagramsEachWay_AllDeliveredIntact()
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

			var serverReceived = new List<byte[]>();
			var clientReceived = new List<byte[]>();
			server.OnDecrypted += payload => serverReceived.Add(payload.ToArray());
			client.OnDecrypted += payload => clientReceived.Add(payload.ToArray());

			const int count = 1000;
			for (int i = 0; i < count; i++)
			{
				client.SendApplicationData(new byte[] {(byte) i, (byte) (i >> 8), 0xAB});
			}
			for (int i = 0; i < count; i++)
			{
				server.SendApplicationData(new byte[] {(byte) i, (byte) (i >> 8), 0xCD});
			}

			Assert.AreEqual(count, serverReceived.Count, "expected every one of the 1000 client->server datagrams to be delivered");
			Assert.AreEqual(count, clientReceived.Count, "expected every one of the 1000 server->client datagrams to be delivered");
			for (int i = 0; i < count; i++)
			{
				CollectionAssert.AreEqual(new byte[] {(byte) i, (byte) (i >> 8), 0xAB}, serverReceived[i]);
				CollectionAssert.AreEqual(new byte[] {(byte) i, (byte) (i >> 8), 0xCD}, clientReceived[i]);
			}

			Assert.AreEqual(0L, server.RecordCrypto.DecryptFailures);
			Assert.AreEqual(0L, server.RecordCrypto.ReplayDrops);
			Assert.AreEqual(0L, client.RecordCrypto.DecryptFailures);
			Assert.AreEqual(0L, client.RecordCrypto.ReplayDrops);

			server.Dispose();
			client.Dispose();
		}

		/// <summary>Finds the last datagram in <paramref name="datagrams" /> whose first record declares epoch 0: for a live handshake capture, the peer's own ChangeCipherSpec, the shape a real final-flight retransmission carries at its front.</summary>
		private static byte[] FindLastEpochZeroDatagram(List<byte[]> datagrams)
		{
			return datagrams.Last(d => DtlsRecordCrypto.TryReadRecordHeader(d, out _, out int epoch, out _) && epoch == 0);
		}

		/// <summary>
		///     A server-role handshake ends with the server's own final flight (ChangeCipherSpec at epoch
		///     0, Finished at epoch 1) as the last thing it ever sends, so <see cref="DtlsSession.FinalFlightCacheCount" />
		///     must be non-empty once the handshake completes, and its contents must be exactly the tail
		///     of what the wire actually observed. Feeding the server a datagram whose first record
		///     declares epoch 0 (here, the client's own ChangeCipherSpec, the closest this harness's
		///     synchronous, one-record-per-datagram pump gets to a real peer retransmitting a lost final
		///     flight) then proves the resend itself: byte-identical to that same tail, on the same wire
		///     closure the live handshake used.
		/// </summary>
		[TestMethod]
		public async Task EpochZeroRecord_TriggersVerbatimResendOfTheCachedFinalFlight_MatchingWhatTheWireObserved()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			var serverToClientDatagrams = new List<byte[]>();
			var clientToServerDatagrams = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes =>
			{
				serverToClientDatagrams.Add(bytes.ToArray());
				client.FeedDatagram(bytes);
			});
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				clientToServerDatagrams.Add(bytes.ToArray());
				server.FeedDatagram(bytes);
			});

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			int cacheCount = server.FinalFlightCacheCount;
			Assert.IsTrue(cacheCount > 0, "expected a server-role handshake to end with a non-empty final-flight cache");
			Assert.IsTrue(cacheCount <= serverToClientDatagrams.Count);

			List<byte[]> expectedFinalFlight = serverToClientDatagrams.Skip(serverToClientDatagrams.Count - cacheCount).ToList();
			byte[] triggerDatagram = FindLastEpochZeroDatagram(clientToServerDatagrams);

			long resendsBefore = server.ResendsPerformed;
			int preTriggerCount = serverToClientDatagrams.Count;

			server.FeedDatagram(triggerDatagram);

			Assert.AreEqual(resendsBefore + 1, server.ResendsPerformed);
			List<byte[]> resent = serverToClientDatagrams.Skip(preTriggerCount).ToList();
			Assert.AreEqual(expectedFinalFlight.Count, resent.Count, "expected the resend to re-emit exactly the cached final flight, one datagram per cached entry");
			for (int i = 0; i < expectedFinalFlight.Count; i++)
			{
				CollectionAssert.AreEqual(expectedFinalFlight[i], resent[i], $"expected resent datagram {i} to be byte-identical to what the wire originally observed");
			}

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     Two epoch-0 triggers inside the same 1-second window must produce exactly one resend; only
		///     advancing the clock seam (<see cref="DtlsSession.ClockNowMillis" />) past that window
		///     re-arms it. Fully clock-driven, no real wall-clock wait.
		/// </summary>
		[TestMethod]
		public async Task EpochZeroRecord_RateLimitsResends_ToAtMostOnePerSecond_AdvancingTheClockReArms()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			var clientToServerDatagrams = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				clientToServerDatagrams.Add(bytes.ToArray());
				server.FeedDatagram(bytes);
			});

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(server.FinalFlightCacheCount > 0);

			byte[] triggerDatagram = FindLastEpochZeroDatagram(clientToServerDatagrams);

			long fakeNow = 1_000_000;
			server.ClockNowMillis = () => fakeNow;

			server.FeedDatagram(triggerDatagram);
			Assert.AreEqual(1L, server.ResendsPerformed, "expected the first trigger to resend: cache non-empty, no prior resend");

			server.FeedDatagram(triggerDatagram);
			Assert.AreEqual(1L, server.ResendsPerformed, "expected the second trigger inside the 1-second window to be rate-limited, not resent");
			Assert.AreEqual(1L, server.EpochZeroRecordsDropped);

			fakeNow += 1000; // exactly the boundary: "at least 1 second has passed" must re-arm here.
			server.FeedDatagram(triggerDatagram);
			Assert.AreEqual(2L, server.ResendsPerformed, "expected advancing the clock seam past the 1-second window to re-arm the resend");

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     RFC 5246 7.2.1's "any data received after a closure alert is ignored" has a counterpart
		///     here: an epoch-0 record must not cost the peer its other, legitimate records in the same
		///     datagram. A coalesced datagram - the exact shape a real retransmitted final flight has
		///     (ChangeCipherSpec at epoch 0 immediately followed by Finished at epoch 1) - must still
		///     deliver whatever valid epoch-1 record follows the epoch-0 one.
		/// </summary>
		[TestMethod]
		public async Task CoalescedDatagram_EpochZeroRecordFollowedByValidRecord_DeliversTheValidRecordNormally()
		{
			const byte ApplicationDataContentType = 23;

			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			var clientToServerDatagrams = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				clientToServerDatagrams.Add(bytes.ToArray());
				server.FeedDatagram(bytes);
			});

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			byte[] epochZeroRecord = FindLastEpochZeroDatagram(clientToServerDatagrams);

			using var forger = new DtlsRecordCrypto(client.CapturedKeys, isServer: false);
			forger.SetSendSequenceForTesting(1);
			byte[] appPayload = {9, 9, 9};
			Span<byte> appWire = stackalloc byte[appPayload.Length + DtlsRecordCrypto.RecordOverhead];
			int appLength = forger.EncryptRecord(ApplicationDataContentType, appPayload, appWire);
			Assert.AreNotEqual(-1, appLength);

			byte[] datagram = new byte[epochZeroRecord.Length + appLength];
			epochZeroRecord.CopyTo(datagram, 0);
			appWire.Slice(0, appLength).CopyTo(datagram.AsSpan(epochZeroRecord.Length));

			var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => received.TrySetResult(payload.ToArray());

			server.FeedDatagram(datagram);

			CollectionAssert.AreEqual(appPayload, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.AreEqual(1L, server.ResendsPerformed, "expected the epoch-0 record ahead of the valid one to still trigger its own resend");

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     An epoch-0 header (13 bytes) declaring a fragment length that consumes the rest of the
		///     datagram as garbage, with nothing valid before or after it, is the junk-prefix attack shape
		///     F1+F2 names: an unauthenticated header alone must never cost more than a rate-limited
		///     resend, never throw, and never allocate once the rate limit is active. A fixed fake clock
		///     (never advancing) makes every trigger past the first provably rate-limited, regardless of
		///     how fast or slow the machine running this test is.
		/// </summary>
		[TestMethod]
		public async Task JunkPrefixDatagram_EpochZeroHeaderThenGarbage_HandledFully_AtMostOneResend_ZeroAllocationOnTheDropPath()
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
			Assert.IsTrue(server.FinalFlightCacheCount > 0);

			const int garbageLength = 32;
			byte[] junkDatagram = new byte[13 + garbageLength];
			junkDatagram[0] = 22; // an arbitrary content type; the epoch-0 path never reads it
			junkDatagram[1] = 0xFE;
			junkDatagram[2] = 0xFD;
			// epoch (bytes 3-4) left at 0; sequence (bytes 5-10) left at 0, neither read by the epoch-0 path.
			junkDatagram[11] = (byte) (garbageLength >> 8);
			junkDatagram[12] = (byte) garbageLength;
			for (int i = 13; i < junkDatagram.Length; i++) junkDatagram[i] = 0xAA;

			long fakeNow = 5000;
			server.ClockNowMillis = () => fakeNow;

			server.FeedDatagram(junkDatagram);
			Assert.AreEqual(1L, server.ResendsPerformed, "expected the first-ever trigger to resend");

			// Warm this exact path once more before measuring, then measure allocation across many more:
			// every one of these is rate-limited (same fakeNow), so this is the pure drop-and-count path.
			server.FeedDatagram(junkDatagram);

			const int iterations = 500;
			long before = GC.GetTotalAllocatedBytes(precise: true);
			for (int i = 0; i < iterations; i++)
			{
				server.FeedDatagram(junkDatagram);
			}
			long after = GC.GetTotalAllocatedBytes(precise: true);

			Assert.AreEqual(0L, after - before, "expected zero heap allocation on the rate-limited drop path");
			Assert.AreEqual(1L, server.ResendsPerformed, "expected at most one resend total across every trigger in this test");

			// The session is still fully alive: a genuinely new record still delivers.
			var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => received.TrySetResult(payload.ToArray());
			client.SendApplicationData(new byte[] {1, 2, 3});
			CollectionAssert.AreEqual(new byte[] {1, 2, 3}, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     The role asymmetry F1+F2's design names: a client-role handshake's last handshake event is
		///     a receive (the server's own Finished flight), so <see cref="DtlsSession.FinalFlightCacheCount" />
		///     ends at zero, and empty is correct - a server that sent its final flight has, by
		///     definition, already received ours. An epoch-0 record reaching a session with an empty cache
		///     (the server's own ChangeCipherSpec, fed to the client here) is dropped and counted, never
		///     answered, and never throws.
		/// </summary>
		[TestMethod]
		public async Task EpochZeroRecord_ClientRoleWithEmptyCache_IsDroppedAndCounted_NoThrow()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			var serverToClientDatagrams = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes =>
			{
				serverToClientDatagrams.Add(bytes.ToArray());
				client.FeedDatagram(bytes);
			});
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			Assert.AreEqual(0, client.FinalFlightCacheCount, "expected a client-role handshake to end with an empty final-flight cache");

			byte[] serverEpochZeroDatagram = FindLastEpochZeroDatagram(serverToClientDatagrams);

			long droppedBefore = client.EpochZeroRecordsDropped;
			long resendsBefore = client.ResendsPerformed;

			client.FeedDatagram(serverEpochZeroDatagram);

			Assert.AreEqual(droppedBefore + 1, client.EpochZeroRecordsDropped);
			Assert.AreEqual(resendsBefore, client.ResendsPerformed, "expected no resend: the cache is empty");

			var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			client.OnDecrypted += payload => received.TrySetResult(payload.ToArray());
			server.SendApplicationData(new byte[] {7, 7, 7});
			CollectionAssert.AreEqual(new byte[] {7, 7, 7}, await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     An encrypted fatal alert is indistinguishable, on the wire, from a normal application-data
		///     record except for its content type; the receiving session must decrypt it, recognise it,
		///     and tear itself down exactly as if <see cref="DtlsSession.Dispose" /> had been called: no
		///     more sends reach the wire, and no more datagrams are delivered. The alert is forged with an
		///     independent <see cref="DtlsRecordCrypto" /> built from the real captured key block (the
		///     same cross-boundary pattern <see cref="DtlsRecordCryptoTests" /> uses), seeded well past
		///     anything the real session has sent so the server's replay window admits it.
		/// </summary>
		[TestMethod]
		public async Task EncryptedFatalAlert_TearsDownTheSession_SendsDropSilently_FeedDatagramBecomesNoOp()
		{
			const byte AlertContentType = 21;
			const byte AlertLevelFatal = 2;
			const byte AlertDescriptionUnexpectedMessage = 10;

			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			bool serverClosed = false;
			bool serverSentAfterClose = false;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes =>
			{
				if (serverClosed) serverSentAfterClose = true;
				client.FeedDatagram(bytes);
			});
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			// Sanity: the pipe works before teardown.
			var firstReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => firstReceived.TrySetResult(payload.ToArray());
			client.SendApplicationData(new byte[] {1, 2, 3});
			CollectionAssert.AreEqual(new byte[] {1, 2, 3}, await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5)));

			using (var forger = new DtlsRecordCrypto(client.CapturedKeys, isServer: false))
			{
				forger.SetSendSequenceForTesting(5000);
				Span<byte> wire = stackalloc byte[2 + DtlsRecordCrypto.RecordOverhead];
				int wireLength = forger.EncryptRecord(AlertContentType, new byte[] {AlertLevelFatal, AlertDescriptionUnexpectedMessage}, wire);
				Assert.AreNotEqual(-1, wireLength);
				server.FeedDatagram(wire.Slice(0, wireLength));
			}

			serverClosed = true;

			// Sends drop silently: must not throw, must never reach the wire.
			server.SendApplicationData(new byte[] {9, 9, 9});
			Assert.IsFalse(serverSentAfterClose, "SendApplicationData must not touch the wire at all once a fatal alert has torn the session down");

			// FeedDatagram becomes a no-op: a subsequent legitimate datagram is not delivered.
			bool deliveredAfterClose = false;
			server.OnDecrypted += _ => deliveredAfterClose = true;
			client.SendApplicationData(new byte[] {4, 5, 6});
			await Task.Delay(200);
			Assert.IsFalse(deliveredAfterClose);

			client.Dispose();
			server.Dispose();
		}

		/// <summary>
		///     RFC 5246 7.2.1: a close_notify ends the connection in both directions immediately, and the
		///     recipient sends its own close_notify back before closing. The response must ride the native
		///     record layer's live send sequence, not a stale one: proven here by sending one real message
		///     first (establishing a known baseline sequence) and asserting the response is exactly one
		///     past it, on the same session's own live counter.
		/// </summary>
		[TestMethod]
		public async Task EncryptedCloseNotify_ClosesTheSession_AndRespondsWithACloseNotifyAtTheLiveSequence()
		{
			const byte AlertContentType = 21;
			const byte AlertLevelWarning = 1;
			const byte AlertDescriptionCloseNotify = 0;

			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			byte[] lastServerToClient = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes =>
			{
				lastServerToClient = bytes.ToArray();
				client.FeedDatagram(bytes);
			});
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			// One real send first: the response's sequence is checked against this known live baseline,
			// not just "greater than zero".
			var firstReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			client.OnDecrypted += payload => firstReceived.TrySetResult(payload.ToArray());
			server.SendApplicationData(new byte[] {1, 2, 3});
			CollectionAssert.AreEqual(new byte[] {1, 2, 3}, await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.IsNotNull(lastServerToClient, "expected to have captured the sanity send's wire bytes");
			ulong sanitySequence = ReadSequence(lastServerToClient);

			// Seeded well below the server's own live sequence (already past 1000 after the sanity send
			// above): the replay window admits anything strictly ahead of the highest sequence seen so
			// far with no distance limit, so a low, distinct sequence here is enough to be accepted as a
			// genuine new record without needing to coordinate with the server's own counter.
			using (var forger = new DtlsRecordCrypto(client.CapturedKeys, isServer: false))
			{
				forger.SetSendSequenceForTesting(1);
				Span<byte> wire = stackalloc byte[2 + DtlsRecordCrypto.RecordOverhead];
				int wireLength = forger.EncryptRecord(AlertContentType, new byte[] {AlertLevelWarning, AlertDescriptionCloseNotify}, wire);
				Assert.AreNotEqual(-1, wireLength);
				server.FeedDatagram(wire.Slice(0, wireLength));
			}

			Assert.IsTrue(server.IsClosed, "expected the server session to close on receiving close_notify");

			byte responseContentType = lastServerToClient[0];
			Assert.AreEqual(AlertContentType, responseContentType, "expected the server's own close_notify response to be the last thing it sent");
			ulong responseSequence = ReadSequence(lastServerToClient);
			Assert.AreEqual(sanitySequence + 1, responseSequence, "expected the response to ride the native layer's live sequence, one past the last real send - never a stale low sequence");

			using var verifier = new DtlsRecordCrypto(client.CapturedKeys, isServer: false);
			Span<byte> plaintext = stackalloc byte[2];
			bool decrypted = verifier.TryDecryptRecord(lastServerToClient, plaintext, out byte contentType, out int length);
			Assert.IsTrue(decrypted, "expected the response to decrypt cleanly with the real key block");
			Assert.AreEqual(AlertContentType, contentType);
			Assert.AreEqual(2, length);
			Assert.AreEqual(AlertDescriptionCloseNotify, plaintext[1], "expected description close_notify(0) in the response body");

			client.Dispose();
			server.Dispose();
		}

		/// <summary>
		///     RFC 5246 7.2.1's "any data received after a closure alert is ignored" applies within one
		///     datagram, not just across datagrams: a close_notify record followed by an application-data
		///     record in the SAME datagram must stop the walk at the alert, never reaching the record
		///     after it.
		/// </summary>
		[TestMethod]
		public async Task CloseNotify_StopsProcessingTheRestOfTheDatagram_LaterRecordInTheSameDatagramNotDelivered()
		{
			const byte AlertContentType = 21;
			const byte ApplicationDataContentType = 23;
			const byte AlertLevelWarning = 1;
			const byte AlertDescriptionCloseNotify = 0;

			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			bool delivered = false;
			server.OnDecrypted += _ => delivered = true;

			using (var forger = new DtlsRecordCrypto(client.CapturedKeys, isServer: false))
			{
				forger.SetSendSequenceForTesting(1);
				Span<byte> alertWire = stackalloc byte[2 + DtlsRecordCrypto.RecordOverhead];
				int alertLength = forger.EncryptRecord(AlertContentType, new byte[] {AlertLevelWarning, AlertDescriptionCloseNotify}, alertWire);
				Assert.AreNotEqual(-1, alertLength);

				byte[] appPayload = {9, 9, 9};
				Span<byte> appWire = stackalloc byte[appPayload.Length + DtlsRecordCrypto.RecordOverhead];
				int appLength = forger.EncryptRecord(ApplicationDataContentType, appPayload, appWire);
				Assert.AreNotEqual(-1, appLength);

				byte[] datagram = new byte[alertLength + appLength];
				alertWire.Slice(0, alertLength).CopyTo(datagram);
				appWire.Slice(0, appLength).CopyTo(datagram.AsSpan(alertLength));

				server.FeedDatagram(datagram);
			}

			Assert.IsTrue(server.IsClosed, "expected the server session to close on the close_notify record");
			Assert.IsFalse(delivered, "expected the application-data record after the close_notify, in the same datagram, to never be delivered");

			client.Dispose();
			server.Dispose();
		}

		/// <summary>
		///     RFC 5246 7.2.1 requires nothing further on the wire once a close_notify has gone out: this
		///     asserts on every datagram the server sends across teardown, not just the last one, because
		///     two real leaks hide behind "the last datagram looked right" - BouncyCastle's own
		///     <c>_dtlsTransport.Close()</c> generates and sends a second alert of its own, on its stalled
		///     epoch-1 sequence, and it would otherwise be the true last datagram observed here.
		///     <see cref="DtlsSession.Dispose" /> emits exactly one close_notify, ours, at the native
		///     record layer's live sequence, and nothing else ever reaches the wire once it has.
		/// </summary>
		[TestMethod]
		public async Task Dispose_EmitsExactlyOneCloseNotify_NothingElseAfterIt()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			var sentByServer = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes =>
			{
				sentByServer.Add(bytes.ToArray());
				client.FeedDatagram(bytes);
			});
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			sentByServer.Clear(); // only datagrams from teardown onward matter here.
			server.Dispose();

			AssertExactlyOneLiveCloseNotify(sentByServer, client.CapturedKeys);

			client.Dispose();
		}

		/// <summary>
		///     The other order: a peer's close_notify already closed this side (sending our own
		///     response in the process, per RFC 5246 7.2.1) before <see cref="DtlsSession.Dispose" /> is
		///     ever called on it. Without a send-once guard, <see cref="DtlsSession.Dispose" /> would try
		///     to say goodbye a second time - <see cref="RequestClose" /> having already run inside the
		///     close_notify handler means a plain <see cref="DtlsSession._closed" /> check at the top of
		///     <see cref="DtlsSession.Dispose" /> could not tell the two cases apart, which is why the
		///     guard lives on the send itself, not on whether the session was already closed.
		/// </summary>
		[TestMethod]
		public async Task InboundCloseNotify_ThenDispose_EmitsExactlyOneCloseNotifyTotal()
		{
			const byte AlertContentType = 21;
			const byte AlertLevelWarning = 1;
			const byte AlertDescriptionCloseNotify = 0;

			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			var sentByServer = new List<byte[]>();
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes =>
			{
				sentByServer.Add(bytes.ToArray());
				client.FeedDatagram(bytes);
			});
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes));

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			sentByServer.Clear();

			using (var forger = new DtlsRecordCrypto(client.CapturedKeys, isServer: false))
			{
				forger.SetSendSequenceForTesting(1);
				Span<byte> wire = stackalloc byte[2 + DtlsRecordCrypto.RecordOverhead];
				int wireLength = forger.EncryptRecord(AlertContentType, new byte[] {AlertLevelWarning, AlertDescriptionCloseNotify}, wire);
				Assert.AreNotEqual(-1, wireLength);
				server.FeedDatagram(wire.Slice(0, wireLength));
			}

			Assert.IsTrue(server.IsClosed, "expected the inbound close_notify to have closed the server session already");

			// The leak this guards against: a caller disposing a session some other event already closed.
			server.Dispose();

			AssertExactlyOneLiveCloseNotify(sentByServer, client.CapturedKeys);

			client.Dispose();
		}

		/// <summary>
		///     Shared by the two close_notify-ordering tests above: every datagram captured must be
		///     exactly one record, a close_notify, riding the native layer's live sequence (never
		///     BouncyCastle's stale one), and there must be exactly one such datagram total.
		/// </summary>
		private static void AssertExactlyOneLiveCloseNotify(List<byte[]> sentDatagrams, CapturedDtlsKeys peerKeys)
		{
			const byte AlertContentType = 21;

			Assert.AreEqual(1, sentDatagrams.Count, "expected exactly one datagram total: our close_notify, and nothing else - not a second native attempt, not BouncyCastle's own stale-sequence alert");

			byte[] onlyDatagram = sentDatagrams[0];
			Assert.AreEqual(AlertContentType, onlyDatagram[0], "expected an alert record");

			ulong sequence = ReadSequence(onlyDatagram);
			Assert.IsTrue(sequence >= DtlsRecordCrypto.SendSequenceHandshakeHeadroom, $"expected the live native sequence ({DtlsRecordCrypto.SendSequenceHandshakeHeadroom}+), never BouncyCastle's stale one; got {sequence}");

			using var verifier = new DtlsRecordCrypto(peerKeys, isServer: false);
			Span<byte> plaintext = stackalloc byte[2];
			bool decrypted = verifier.TryDecryptRecord(onlyDatagram, plaintext, out byte contentType, out int length);
			Assert.IsTrue(decrypted, "expected the close_notify to decrypt cleanly with the real key block");
			Assert.AreEqual(AlertContentType, contentType);
			Assert.AreEqual(2, length);
			Assert.AreEqual(0, plaintext[1], "expected description close_notify(0)");
		}

		private static ulong ReadSequence(ReadOnlySpan<byte> record)
		{
			ReadOnlySpan<byte> sequence = record.Slice(5, 6);
			ulong value = 0;
			foreach (byte b in sequence) value = (value << 8) | b;
			return value;
		}

		/// <summary>
		///     A single corrupted byte on the wire must never take the session down or lose a delivery
		///     count: it is drop-and-count, exactly like <see cref="DtlsRecordCryptoTests" /> already
		///     proves for the record layer in isolation, but exercised through the full session so the
		///     dispatch in <see cref="DtlsSession.FeedDatagram" /> is what is actually under test, not
		///     just <see cref="DtlsRecordCrypto" /> on its own.
		/// </summary>
		[TestMethod]
		public async Task TamperedRecord_IsDroppedAndCounted_SessionStaysAlive_NextCleanRecordDelivers()
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			bool interceptOnly = false;
			byte[] intercepted = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				if (interceptOnly)
				{
					intercepted = bytes.ToArray();
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

			// Capture one clean record's wire bytes without delivering it, so its sequence has never
			// been seen by the server's replay window: tampering an already-delivered sequence would be
			// rejected as a replay, not a bad tag, and would prove nothing about DecryptFailures.
			interceptOnly = true;
			client.SendApplicationData(new byte[] {1, 1, 1});
			interceptOnly = false;
			Assert.IsNotNull(intercepted, "expected to have captured the wire bytes of an undelivered record");

			byte[] tampered = (byte[]) intercepted.Clone();
			tampered[tampered.Length - 1] ^= 0xFF; // last byte of the record is always the final tag byte

			long decryptFailuresBefore = server.RecordCrypto.DecryptFailures;
			int deliveredCount = 0;
			server.OnDecrypted += _ => deliveredCount++;

			server.FeedDatagram(tampered);

			Assert.AreEqual(0, deliveredCount, "a tampered record must never be delivered");
			Assert.AreEqual(decryptFailuresBefore + 1, server.RecordCrypto.DecryptFailures);

			// The replay window must not have advanced on the rejected tamper attempt: the clean
			// original, at the same sequence, still decrypts.
			var firstReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => firstReceived.TrySetResult(payload.ToArray());
			server.FeedDatagram(intercepted);
			CollectionAssert.AreEqual(new byte[] {1, 1, 1}, await firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(5)));

			// And the session is still fully alive for a genuinely new record afterward.
			var secondReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
			server.OnDecrypted += payload => secondReceived.TrySetResult(payload.ToArray());
			client.SendApplicationData(new byte[] {2, 2, 2});
			CollectionAssert.AreEqual(new byte[] {2, 2, 2}, await secondReceived.Task.WaitAsync(TimeSpan.FromSeconds(5)), "the session must still deliver a clean record after a rejected tamper attempt");

			server.Dispose();
			client.Dispose();
		}

		/// <summary>
		///     With BouncyCastle out of the post-handshake path entirely, this must allocate nothing on
		///     our side. Both sessions' native record layers, both directions, 10k datagrams each way,
		///     well past JIT/AesGcm warmup.
		/// </summary>
		[TestMethod]
		public async Task NativeRecordLayer_TenThousandDatagramsBothDirections_AllocatesNothingOnOurSide()
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

			server.OnDecrypted += _ => { };
			client.OnDecrypted += _ => { };

			byte[] payload = {1, 2, 3, 4, 5, 6, 7, 8};

			// Warmup: JIT and any AesGcm first-use setup happen here, not inside the measured bracket.
			for (int i = 0; i < 100; i++)
			{
				client.SendApplicationData(payload);
				server.SendApplicationData(payload);
			}

			long before = GC.GetTotalAllocatedBytes(precise: true);
			for (int i = 0; i < 10000; i++)
			{
				client.SendApplicationData(payload);
				server.SendApplicationData(payload);
			}
			long after = GC.GetTotalAllocatedBytes(precise: true);

			Assert.AreEqual(0L, after - before, "expected zero heap allocation across 10k datagrams each way, post-handshake; the BC-backed floor this replaces measured roughly 5000 bytes per datagram");

			server.Dispose();
			client.Dispose();
		}
	}
}