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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Utils.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using SicStream;

namespace MiNET.Test
{
	/// <summary>
	///     The session cipher is the one part of the stack no test could see, and the one where a
	///     silent change is fatal: the client does not report a decryption failure, it simply stops
	///     making sense of the stream and drops the connection with a generic error. Everything here
	///     is built on BouncyCastle internals through a hand-written <see cref="StreamingSicBlockCipher" />
	///     wrapper, so a BouncyCastle upgrade can change behaviour without changing a signature.
	///     These tests pin the wire contract itself, not the implementation, so they stay meaningful
	///     across any crypto library swap.
	/// </summary>
	[TestClass]
	public class PacketEncryptionTests
	{
		// Any fixed 32 bytes stands in for the ECDH shared secret. The value is irrelevant; that it
		// is the same on both sides, and stable across runs, is the whole point.
		private static readonly byte[] Secret = Enumerable.Range(0, 32).Select(i => (byte) (i * 7 + 3)).ToArray();

		/// <summary>
		///     Mirrors LoginMessageHandler exactly: AES in counter mode, keyed with the shared secret,
		///     IV of the first twelve secret bytes followed by 00 00 00 02. If that construction ever
		///     drifts from the handler, these tests are testing something the server does not do.
		/// </summary>
		private static IBufferedCipher MakeCipher(bool forEncryption)
		{
			byte[] iv = Secret.Take(12).Concat(new byte[] {0, 0, 0, 2}).ToArray();
			var cipher = new StreamingSicBlockCipher(new SicBlockCipher(new AesEngine()));
			cipher.Init(forEncryption, new ParametersWithIV(new KeyParameter(Secret), iv));
			return cipher;
		}

		private static CryptoContext NewContext()
		{
			return new CryptoContext
			{
				Key = Secret,
				Encryptor = MakeCipher(true),
				Decryptor = MakeCipher(false),
				UseEncryption = true
			};
		}

		/// <summary>
		///     The baseline: what one side encrypts, the other side reads back. Two contexts, because
		///     a real session has an encryptor here and a decryptor there, each with its own keystream
		///     position.
		/// </summary>
		[TestMethod]
		public void WhatTheServerEncryptsTheClientReadsBack()
		{
			CryptoContext server = NewContext();
			CryptoContext client = NewContext();

			byte[] payload = Encoding.UTF8.GetBytes("McpeText: the quick brown fox");

			byte[] encrypted = CryptoUtils.Encrypt(payload, server);
			ReadOnlyMemory<byte> decrypted = CryptoUtils.Decrypt(encrypted, client);

			CollectionAssert.AreEqual(payload, decrypted.ToArray());
		}

		/// <summary>
		///     Every frame carries eight trailing bytes the peer uses to decide the stream is still
		///     intact, and they are the first eight bytes of SHA-256 over the send counter as a
		///     little-endian int64, then the plaintext, then the raw shared secret. This is the field
		///     order a real client recomputes; get it wrong in any of the three parts and the client
		///     rejects the frame rather than telling you why.
		/// </summary>
		[TestMethod]
		public void ChecksumIsTruncatedSha256OverCounterThenPayloadThenKey()
		{
			CryptoContext server = NewContext();
			byte[] payload = Encoding.UTF8.GetBytes("checksum me");

			byte[] encrypted = CryptoUtils.Encrypt(payload, server);

			// Decrypt the whole frame, checksum included, rather than through CryptoUtils.Decrypt,
			// which deliberately strips the trailing eight bytes.
			byte[] full = MakeCipher(false).ProcessBytes(encrypted);
			byte[] actualChecksum = full.Skip(payload.Length).Take(8).ToArray();

			// SendCounter starts at -1 and is pre-incremented, so the first frame of a session is 0.
			byte[] expected = SHA256.HashData(BitConverter.GetBytes(0L).Concat(payload).Concat(Secret).ToArray()).Take(8).ToArray();

			CollectionAssert.AreEqual(expected, actualChecksum);
		}

		/// <summary>
		///     The counter is what stops an observer replaying a captured frame, and it is inside the
		///     hash rather than on the wire. So the same payload sent twice has to produce different
		///     bytes. If a change ever made the counter constant, everything would still round-trip
		///     and the protection would be gone silently, which is why this asserts on the ciphertext
		///     rather than on the counter field.
		/// </summary>
		[TestMethod]
		public void TheSamePayloadTwiceDoesNotProduceTheSameBytes()
		{
			CryptoContext server = NewContext();
			byte[] payload = Encoding.UTF8.GetBytes("identical");

			byte[] first = CryptoUtils.Encrypt(payload, server);
			byte[] second = CryptoUtils.Encrypt(payload, server);

			Assert.AreEqual(1L, server.SendCounter, "counter should have advanced once per frame");
			CollectionAssert.AreNotEqual(first, second);
		}

		/// <summary>
		///     Counter mode is a stream cipher: the keystream runs continuously across frames, so the
		///     decryptor only produces the right bytes if it has consumed every earlier frame in
		///     order. This is why a dropped or reordered frame kills a session rather than corrupting
		///     one packet, and why swapping the cipher for a per-frame mode would look fine in a
		///     single round-trip test and then fail on the second packet of a real join.
		/// </summary>
		[TestMethod]
		public void KeystreamRunsAcrossFramesSoOrderMatters()
		{
			CryptoContext server = NewContext();
			byte[] one = Encoding.UTF8.GetBytes("frame one");
			byte[] two = Encoding.UTF8.GetBytes("frame two");

			byte[] encryptedOne = CryptoUtils.Encrypt(one, server);
			byte[] encryptedTwo = CryptoUtils.Encrypt(two, server);

			CryptoContext inOrder = NewContext();
			CollectionAssert.AreEqual(one, CryptoUtils.Decrypt(encryptedOne, inOrder).ToArray());
			CollectionAssert.AreEqual(two, CryptoUtils.Decrypt(encryptedTwo, inOrder).ToArray());

			// Same frames, wrong order, fresh keystream: the second frame must not decode as itself.
			CryptoContext outOfOrder = NewContext();
			CollectionAssert.AreNotEqual(two, CryptoUtils.Decrypt(encryptedTwo, outOfOrder).ToArray());
		}

		/// <summary>
		///     The frame grows by exactly the checksum and nothing else. A block cipher with padding
		///     would round the length up instead, and the peer reads the payload length from the
		///     wrapper, so any padding would be read as packet data.
		/// </summary>
		[TestMethod]
		public void EncryptionAddsEightBytesAndNoPadding()
		{
			CryptoContext server = NewContext();

			foreach (int length in new[] {1, 15, 16, 17, 1024})
			{
				byte[] payload = Enumerable.Repeat((byte) 0xAB, length).ToArray();
				Assert.AreEqual(length + 8, CryptoUtils.Encrypt(payload, server).Length, $"payload of {length} bytes");
			}
		}
	}
}
