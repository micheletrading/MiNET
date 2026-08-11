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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;
using Org.BouncyCastle.Tls;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class DtlsRecordCryptoTests
	{
		private const byte ApplicationData = 23;

		/// <summary>Fixed, deterministic key material: <see cref="DtlsRecordCrypto" /> is pure spans in/out and does not care where its keys came from, so most of this class exercises it against synthetic vectors rather than paying for a real handshake.</summary>
		private static CapturedDtlsKeys CreateTestKeys(int keyLength = 16)
		{
			var clientKey = new byte[keyLength];
			var serverKey = new byte[keyLength];
			var clientIv = new byte[4];
			var serverIv = new byte[4];
			for (int i = 0; i < keyLength; i++)
			{
				clientKey[i] = (byte) (i + 1);
				serverKey[i] = (byte) (i + 101);
			}
			for (int i = 0; i < 4; i++)
			{
				clientIv[i] = (byte) (i + 201);
				serverIv[i] = (byte) (i + 211);
			}
			return new CapturedDtlsKeys(clientKey, serverKey, clientIv, serverIv, cipherSuite: 0);
		}

		[TestMethod]
		public void EncryptRecord_ThenTryDecryptRecord_RoundTripsAcrossRoles_SequenceIncrementsPerRecord()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			Span<byte> wire = stackalloc byte[5 + DtlsRecordCrypto.RecordOverhead];
			Span<byte> plaintext = stackalloc byte[5];
			for (int i = 0; i < 5; i++)
			{
				byte[] payload = {(byte) i, 1, 2, 3, 4};
				int wireLength = client.EncryptRecord(ApplicationData, payload, wire);

				Assert.AreNotEqual(-1, wireLength);
				Assert.AreEqual((ulong) i, ReadSequence(wire));

				bool ok = server.TryDecryptRecord(wire.Slice(0, wireLength), plaintext, out byte contentType, out int length);

				Assert.IsTrue(ok, $"expected record {i} to decrypt");
				Assert.AreEqual(ApplicationData, contentType);
				Assert.AreEqual(payload.Length, length);
				CollectionAssert.AreEqual(payload, plaintext.Slice(0, length).ToArray());
			}
		}

		[TestMethod]
		public void EncryptRecord_DestinationTooSmall_ReturnsMinusOne_WithoutAdvancingSequence()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);

			byte[] payload = {1, 2, 3, 4};
			Span<byte> tooSmall = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead - 1];
			Assert.AreEqual(-1, client.EncryptRecord(ApplicationData, payload, tooSmall));

			// The sequence must not have moved: a record encrypted right after must still be sequence 0.
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int wireLength = client.EncryptRecord(ApplicationData, payload, wire);
			Assert.AreNotEqual(-1, wireLength);
			Assert.AreEqual(0UL, ReadSequence(wire));
		}

		[TestMethod]
		public void TryDecryptRecord_DestinationTooSmall_ReturnsFalse_MalformedRecordsCounted()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			byte[] payload = {1, 2, 3, 4, 5, 6, 7, 8};
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int wireLength = client.EncryptRecord(ApplicationData, payload, wire);

			Span<byte> tooSmall = stackalloc byte[payload.Length - 1];
			long before = server.MalformedRecords;
			bool ok = server.TryDecryptRecord(wire.Slice(0, wireLength), tooSmall, out _, out _);

			Assert.IsFalse(ok);
			Assert.AreEqual(before + 1, server.MalformedRecords);
		}

		/// <summary>
		///     The strong cross-check: a real BouncyCastle handshake (Task 1's wired-pair pump,
		///     <see cref="DtlsSession" />) hands out a real captured key block, then bytes cross the
		///     BouncyCastle/native boundary in both directions. Direction one feeds a
		///     <see cref="DtlsRecordCrypto" />-encrypted record straight into the peer's still-alive
		///     BouncyCastle <c>DtlsTransport</c> via <see cref="DtlsSession.FeedDatagram" />; direction
		///     two decrypts a real BouncyCastle-encrypted wire datagram with <see cref="DtlsRecordCrypto" />.
		///     Run once under each of the two cipher suites this stack negotiates, the AES-256 run forcing
		///     the suite by narrowing what both handshake roles offer (the sanctioned test-only knob on
		///     <see cref="DtlsHandshakeServer" />/<see cref="DtlsHandshakeClient" />), so both the 16-byte
		///     and 32-byte key paths are proven against real BouncyCastle output, not just against our own
		///     encoder.
		/// </summary>
		[TestMethod]
		public async Task Interop_Aes128Gcm_BothDirections()
		{
			await RunInteropCrossCheck(null);
		}

		[TestMethod]
		public async Task Interop_Aes256Gcm_BothDirections()
		{
			int[] aes256Only = {CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384};
			await RunInteropCrossCheck(aes256Only);
		}

		private static async Task RunInteropCrossCheck(int[] cipherSuites)
		{
			var serverCert = RtcCertificate.CreateSelfSigned();
			var clientCert = RtcCertificate.CreateSelfSigned();

			DtlsSession server = null, client = null;
			byte[] lastClientToServer = null;
			server = new DtlsSession(serverCert, clientCert.FingerprintSha256, isServer: true, bytes => client.FeedDatagram(bytes), cipherSuites);
			client = new DtlsSession(clientCert, serverCert.FingerprintSha256, isServer: false, bytes =>
			{
				lastClientToServer = bytes.ToArray();
				server.FeedDatagram(bytes);
			}, cipherSuites);

			Task<bool> serverDone = server.DoHandshakeAsync(CancellationToken.None);
			Task<bool> clientDone = client.DoHandshakeAsync(CancellationToken.None);
			Assert.IsTrue(await clientDone.WaitAsync(TimeSpan.FromSeconds(15)));
			Assert.IsTrue(await serverDone.WaitAsync(TimeSpan.FromSeconds(15)));

			CapturedDtlsKeys clientKeys = client.CapturedKeys;
			CapturedDtlsKeys serverKeys = server.CapturedKeys;
			if (cipherSuites != null)
			{
				Assert.AreEqual(cipherSuites[0], clientKeys.CipherSuite, "expected the forced suite to have actually been negotiated");
			}

			// Direction 1: DtlsRecordCrypto encrypts (client role), BouncyCastle's still-alive transport
			// on the server side decrypts it via the session's ordinary FeedDatagram path. BouncyCastle
			// sends the handshake's own Finished message under epoch 1 too (post-CCS), so the server's
			// epoch-1 receive window has already advanced past a few low sequence numbers by the time
			// the handshake completes; a synthetic instance starting fresh at sequence 0 would collide
			// with one BouncyCastle already consumed and be silently dropped as a replay/bad MAC.
			// Seeding well clear of anything the handshake itself could plausibly have used sidesteps
			// that without needing to parse BouncyCastle's actual Finished sequence off the wire.
			using (var ourClientCrypto = new DtlsRecordCrypto(clientKeys, isServer: false))
			{
				ourClientCrypto.SetSendSequenceForTesting(1000);
				byte[] payload1 = {10, 20, 30, 40, 50};
				Span<byte> wire1 = stackalloc byte[payload1.Length + DtlsRecordCrypto.RecordOverhead];
				int wire1Length = ourClientCrypto.EncryptRecord(ApplicationData, payload1, wire1);
				Assert.AreNotEqual(-1, wire1Length);

				var received1 = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
				server.OnDecrypted += p => received1.TrySetResult(p.ToArray());
				server.FeedDatagram(wire1.Slice(0, wire1Length));
				CollectionAssert.AreEqual(payload1, await received1.Task.WaitAsync(TimeSpan.FromSeconds(5)));
			}

			// Direction 2: BouncyCastle encrypts (client role, via the session's ordinary
			// SendApplicationData), DtlsRecordCrypto decrypts the captured wire bytes independently.
			byte[] payload2 = {60, 70, 80, 90};
			client.SendApplicationData(payload2);
			Assert.IsNotNull(lastClientToServer, "expected to have captured the wire datagram BouncyCastle encrypted");

			using var ourServerCrypto = new DtlsRecordCrypto(serverKeys, isServer: true);
			Span<byte> plaintext2 = stackalloc byte[payload2.Length];
			bool ok = ourServerCrypto.TryDecryptRecord(lastClientToServer, plaintext2, out byte contentType, out int length2);

			Assert.IsTrue(ok, "expected DtlsRecordCrypto to decrypt a genuine BouncyCastle-produced record");
			Assert.AreEqual(ApplicationData, contentType);
			CollectionAssert.AreEqual(payload2, plaintext2.Slice(0, length2).ToArray());

			server.Dispose();
			client.Dispose();
		}

		private static void AssertTamperRejectedThenCleanRecordStillDecrypts(int tamperByteIndex)
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			byte[] payload = {1, 2, 3, 4, 5, 6, 7, 8};
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int wireLength = client.EncryptRecord(ApplicationData, payload, wire);
			byte[] clean = wire.Slice(0, wireLength).ToArray();

			byte[] tampered = (byte[]) clean.Clone();
			tampered[tamperByteIndex] ^= 0xFF;

			Span<byte> plaintext = stackalloc byte[payload.Length];
			long before = server.DecryptFailures;
			bool tamperedOk = server.TryDecryptRecord(tampered, plaintext, out _, out _);

			Assert.IsFalse(tamperedOk, $"expected tampering byte {tamperByteIndex} to fail authentication");
			Assert.AreEqual(before + 1, server.DecryptFailures);

			// The replay window must not have advanced: the same sequence, presented clean, still decrypts.
			bool cleanOk = server.TryDecryptRecord(clean, plaintext, out byte contentType, out int length);
			Assert.IsTrue(cleanOk, "a clean record at the same sequence must still decrypt after a rejected tamper attempt");
			Assert.AreEqual(ApplicationData, contentType);
			CollectionAssert.AreEqual(payload, plaintext.Slice(0, length).ToArray());
		}

		[TestMethod]
		public void TryDecryptRecord_TamperedCiphertextByte_IsRejected_WindowNotAdvanced()
		{
			// First byte of the ciphertext: header(13) + explicit nonce(8).
			AssertTamperRejectedThenCleanRecordStillDecrypts(13 + 8);
		}

		[TestMethod]
		public void TryDecryptRecord_TamperedTagByte_IsRejected_WindowNotAdvanced()
		{
			// Last byte of the record is always the final tag byte.
			byte[] payload = {1, 2, 3, 4, 5, 6, 7, 8};
			int lastByteIndex = payload.Length + DtlsRecordCrypto.RecordOverhead - 1;
			AssertTamperRejectedThenCleanRecordStillDecrypts(lastByteIndex);
		}

		[TestMethod]
		public void TryDecryptRecord_TamperedAadCoveredHeaderByte_IsRejected_WindowNotAdvanced()
		{
			// Byte 0 of the header is the content type, which is part of the AAD.
			AssertTamperRejectedThenCleanRecordStillDecrypts(0);
		}

		[TestMethod]
		public void TryDecryptRecord_DuplicateRecord_SecondIsRejected_ReplayDropsCounted()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			byte[] payload = {1, 2, 3, 4};
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int wireLength = client.EncryptRecord(ApplicationData, payload, wire);
			byte[] record = wire.Slice(0, wireLength).ToArray();

			Span<byte> plaintext = stackalloc byte[payload.Length];
			Assert.IsTrue(server.TryDecryptRecord(record, plaintext, out _, out _));

			long before = server.ReplayDrops;
			bool replayed = server.TryDecryptRecord(record, plaintext, out _, out _);

			Assert.IsFalse(replayed);
			Assert.AreEqual(before + 1, server.ReplayDrops);
		}

		[TestMethod]
		public void TryDecryptRecord_OutOfOrderWithinWindow_IsAdmitted()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			const int count = 5;
			var records = new byte[count][];
			Span<byte> wire = stackalloc byte[4 + DtlsRecordCrypto.RecordOverhead];
			for (int i = 0; i < count; i++)
			{
				byte[] payload = {(byte) i, 1, 2, 3};
				int wireLength = client.EncryptRecord(ApplicationData, payload, wire);
				records[i] = wire.Slice(0, wireLength).ToArray();
			}

			int[] deliveryOrder = {4, 2, 0, 3, 1};
			Span<byte> plaintext = stackalloc byte[4];
			foreach (int sequence in deliveryOrder)
			{
				bool ok = server.TryDecryptRecord(records[sequence], plaintext, out _, out _);
				Assert.IsTrue(ok, $"expected out-of-order sequence {sequence} to be admitted within the replay window");
			}
		}

		[TestMethod]
		public void TryDecryptRecord_SequenceSixtyFourOrMoreBehindHighest_Rejects_ReplayDropsCounted()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			byte[] payload = {1, 2, 3, 4};
			Span<byte> firstWire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int firstLength = client.EncryptRecord(ApplicationData, payload, firstWire);
			byte[] sequenceZero = firstWire.Slice(0, firstLength).ToArray();

			byte[] farRecord = null;
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			for (int i = 1; i <= 70; i++)
			{
				int wireLength = client.EncryptRecord(ApplicationData, payload, wire);
				if (i == 70) farRecord = wire.Slice(0, wireLength).ToArray();
			}

			Span<byte> plaintext = stackalloc byte[payload.Length];
			Assert.IsTrue(server.TryDecryptRecord(farRecord, plaintext, out _, out _), "expected sequence 70 to establish the window's high water mark");

			long before = server.ReplayDrops;
			bool ok = server.TryDecryptRecord(sequenceZero, plaintext, out _, out _);

			Assert.IsFalse(ok, "sequence 0 is 70 behind the highest received (70), which is >= the 64-wide window");
			Assert.AreEqual(before + 1, server.ReplayDrops);
		}

		[TestMethod]
		public void TryReadRecordHeader_ValidRecord_ReturnsFieldsAndTrue()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);

			byte[] payload = {1, 2, 3};
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int wireLength = client.EncryptRecord(ApplicationData, payload, wire);

			bool ok = DtlsRecordCrypto.TryReadRecordHeader(wire.Slice(0, wireLength), out byte contentType, out int epoch, out int fragmentLength);

			Assert.IsTrue(ok);
			Assert.AreEqual(ApplicationData, contentType);
			Assert.AreEqual(1, epoch);
			Assert.AreEqual(wireLength - 13, fragmentLength);
		}

		[TestMethod]
		public void TryReadRecordHeader_TooShortForHeader_ReturnsFalse()
		{
			Span<byte> tooShort = stackalloc byte[12];
			bool ok = DtlsRecordCrypto.TryReadRecordHeader(tooShort, out _, out _, out _);
			Assert.IsFalse(ok);
		}

		[TestMethod]
		public void TryDecryptRecord_MalformedInputs_NeverThrow_MalformedRecordsCounted()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);

			byte[] payload = {1, 2, 3, 4, 5, 6, 7, 8};
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			int wireLength = client.EncryptRecord(ApplicationData, payload, wire);
			byte[] validRecord = wire.Slice(0, wireLength).ToArray();

			Span<byte> plaintext = stackalloc byte[payload.Length];

			// Every truncation length of a valid record must be rejected without throwing.
			for (int truncateTo = 0; truncateTo < validRecord.Length; truncateTo++)
			{
				using var server = new DtlsRecordCrypto(keys, isServer: true);
				long before = server.MalformedRecords;
				bool ok = server.TryDecryptRecord(validRecord.AsSpan(0, truncateTo), plaintext, out _, out _);

				Assert.IsFalse(ok, $"expected a record truncated to {truncateTo} bytes to be rejected");
				Assert.AreEqual(before + 1, server.MalformedRecords, $"expected MalformedRecords to be counted for a {truncateTo}-byte truncation");
			}

			// Epoch 2 instead of 1.
			{
				using var server = new DtlsRecordCrypto(keys, isServer: true);
				byte[] wrongEpoch = (byte[]) validRecord.Clone();
				wrongEpoch[4] = 2;
				long before = server.MalformedRecords;
				Assert.IsFalse(server.TryDecryptRecord(wrongEpoch, plaintext, out _, out _));
				Assert.AreEqual(before + 1, server.MalformedRecords);
			}

			// Wrong record version.
			{
				using var server = new DtlsRecordCrypto(keys, isServer: true);
				byte[] wrongVersion = (byte[]) validRecord.Clone();
				wrongVersion[1] = 0x03;
				wrongVersion[2] = 0x03;
				long before = server.MalformedRecords;
				Assert.IsFalse(server.TryDecryptRecord(wrongVersion, plaintext, out _, out _));
				Assert.AreEqual(before + 1, server.MalformedRecords);
			}

			// Declared length past the end of the buffer, header otherwise intact.
			{
				using var server = new DtlsRecordCrypto(keys, isServer: true);
				byte[] lengthPastEnd = (byte[]) validRecord.Clone();
				lengthPastEnd[11] = 0xFF;
				lengthPastEnd[12] = 0xFF;
				long before = server.MalformedRecords;
				Assert.IsFalse(server.TryDecryptRecord(lengthPastEnd, plaintext, out _, out _));
				Assert.AreEqual(before + 1, server.MalformedRecords);
			}
		}

		[TestMethod]
		public void EncryptRecord_AtSequenceExhaustion_ReturnsMinusOne()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			client.SetSendSequenceForTesting((1UL << 48) - 1);

			byte[] payload = {1, 2, 3};
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];

			Assert.AreEqual(-1, client.EncryptRecord(ApplicationData, payload, wire));
			// Exhaustion is permanent: a second attempt must still fail, never wrap back to 0.
			Assert.AreEqual(-1, client.EncryptRecord(ApplicationData, payload, wire));
		}

		[TestMethod]
		public void EncryptThenDecrypt_TenThousandRecords_AllocatesNothingAfterWarmup()
		{
			CapturedDtlsKeys keys = CreateTestKeys();
			using var client = new DtlsRecordCrypto(keys, isServer: false);
			using var server = new DtlsRecordCrypto(keys, isServer: true);

			Span<byte> payload = stackalloc byte[8];
			for (int i = 0; i < payload.Length; i++) payload[i] = (byte) i;
			Span<byte> wire = stackalloc byte[payload.Length + DtlsRecordCrypto.RecordOverhead];
			Span<byte> plaintext = stackalloc byte[payload.Length];

			// Warmup: JIT and any AesGcm first-use setup happen here, not inside the measured bracket.
			for (int i = 0; i < 100; i++)
			{
				int len = client.EncryptRecord(ApplicationData, payload, wire);
				server.TryDecryptRecord(wire.Slice(0, len), plaintext, out _, out _);
			}

			bool allOk = true;
			long before = GC.GetTotalAllocatedBytes(precise: true);
			for (int i = 0; i < 10000; i++)
			{
				int len = client.EncryptRecord(ApplicationData, payload, wire);
				allOk &= server.TryDecryptRecord(wire.Slice(0, len), plaintext, out _, out _);
			}
			long after = GC.GetTotalAllocatedBytes(precise: true);

			Assert.IsTrue(allOk, "expected every one of the 10k records to decrypt successfully");
			Assert.AreEqual(0, after - before, "expected zero heap allocation across 10k encrypt+decrypt cycles");
		}

		private static ulong ReadSequence(ReadOnlySpan<byte> record)
		{
			ReadOnlySpan<byte> sequence = record.Slice(5, 6);
			ulong value = 0;
			foreach (byte b in sequence) value = (value << 8) | b;
			return value;
		}
	}
}