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
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes.Span));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes => server.FeedDatagram(bytes.Span));

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
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, true, bytes => client.FeedDatagram(bytes.Span));
			client = new DtlsSession(clientCert, imposter.FingerprintSha256, false, bytes => server.FeedDatagram(bytes.Span));

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
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes.Span));
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				lastClientToServer = bytes.ToArray();
				server.FeedDatagram(bytes.Span);
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
	}
}