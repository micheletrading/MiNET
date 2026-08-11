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
using System.Diagnostics;
using System.Security.Cryptography;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     The epoch-1 DTLS key block for one handshake, sliced per RFC 5246 6.3 (no MAC keys: both
	///     cipher suites this stack negotiates are AEAD). <see cref="ClientWriteIv" /> and
	///     <see cref="ServerWriteIv" /> are the 4-byte GCM salts, not full 12-byte nonces. Every
	///     property is get-only and set exactly once, in the constructor, so an instance cannot be
	///     mutated after <see cref="CapturingTlsCrypto.CreateCipher" /> hands it out.
	/// </summary>
	internal sealed class CapturedDtlsKeys
	{
		public byte[] ClientWriteKey { get; }
		public byte[] ServerWriteKey { get; }
		public byte[] ClientWriteIv { get; }
		public byte[] ServerWriteIv { get; }
		public int CipherSuite { get; }

		public CapturedDtlsKeys(byte[] clientWriteKey, byte[] serverWriteKey, byte[] clientWriteIv, byte[] serverWriteIv, int cipherSuite)
		{
			ClientWriteKey = clientWriteKey;
			ServerWriteKey = serverWriteKey;
			ClientWriteIv = clientWriteIv;
			ServerWriteIv = serverWriteIv;
			CipherSuite = cipherSuite;
		}

		/// <summary>
		///     Zeroes every key/IV array this instance owns. Called only from <see cref="DtlsSession.Dispose" />,
		///     after <see cref="DtlsRecordCrypto" /> has itself been disposed: that class holds the two IV
		///     arrays here as its live send/receive salts for as long as it runs (never a copy - see its
		///     constructor), so zeroing them any earlier would corrupt every record it is still able to
		///     encrypt or decrypt. The array instances themselves are unaffected and every property here
		///     keeps returning them; only their contents change.
		/// </summary>
		internal void Zero()
		{
			CryptographicOperations.ZeroMemory(ClientWriteKey);
			CryptographicOperations.ZeroMemory(ServerWriteKey);
			CryptographicOperations.ZeroMemory(ClientWriteIv);
			CryptographicOperations.ZeroMemory(ServerWriteIv);
		}
	}

	/// <summary>
	///     A BouncyCastle crypto provider that intercepts the negotiated key block on its way through
	///     the handshake so the native record layer can protect application data without BouncyCastle
	///     in the loop. <see cref="CreateCipher" /> is where BouncyCastle turns the negotiated master
	///     secret into the <see cref="TlsCipher" /> it uses for both directions; recomputing the same
	///     key block here with <see cref="TlsImplUtilities.CalculateKeyBlock" /> and slicing it into
	///     <see cref="Captured" /> changes nothing about the cipher BouncyCastle goes on to build, so
	///     the handshake's own Finished protection is untouched.
	/// </summary>
	internal sealed class CapturingTlsCrypto : BcTlsCrypto
	{
		public CapturedDtlsKeys Captured { get; private set; }

		public override TlsCipher CreateCipher(TlsCryptoParameters cryptoParams, int encryptionAlgorithm, int macAlgorithm)
		{
			if (Captured == null)
			{
				Captured = Capture(cryptoParams, encryptionAlgorithm);
			}
			else
			{
				// A single TlsCipher already covers both directions for the AEAD suites this stack
				// negotiates, so a second call for the same connection can only be a redundant
				// re-derivation of the identical key block, never a different suite mid-handshake.
				Debug.Assert(cryptoParams.SecurityParameters.CipherSuite == Captured.CipherSuite, "unexpected cipher suite change between CreateCipher calls on the same handshake");
			}

			return base.CreateCipher(cryptoParams, encryptionAlgorithm, macAlgorithm);
		}

		private static CapturedDtlsKeys Capture(TlsCryptoParameters cryptoParams, int encryptionAlgorithm)
		{
			int keyLength = encryptionAlgorithm switch
			{
				EncryptionAlgorithm.AES_128_GCM => 16,
				EncryptionAlgorithm.AES_256_GCM => 32,
				_ => throw new NotSupportedException($"DTLS encryption algorithm {encryptionAlgorithm} is not one of the two AES-GCM suites this stack negotiates.")
			};

			byte[] keyBlock = TlsImplUtilities.CalculateKeyBlock(cryptoParams, 2 * keyLength + 2 * 4);
			try
			{
				return new CapturedDtlsKeys(
					clientWriteKey: keyBlock.AsSpan(0, keyLength).ToArray(),
					serverWriteKey: keyBlock.AsSpan(keyLength, keyLength).ToArray(),
					clientWriteIv: keyBlock.AsSpan(2 * keyLength, 4).ToArray(),
					serverWriteIv: keyBlock.AsSpan(2 * keyLength + 4, 4).ToArray(),
					cipherSuite: cryptoParams.SecurityParameters.CipherSuite);
			}
			finally
			{
				// Every per-direction array above is its own copy (ToArray()); this intermediate is a
				// second copy of the same key material with nothing else ever reading it again, so it is
				// zeroed here rather than left for the GC to collect still holding plaintext key bytes.
				CryptographicOperations.ZeroMemory(keyBlock);
			}
		}
	}
}