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
using System.Security.Cryptography;

namespace MiNET.Net.Rtc.FastDtls
{
	/// <summary>
	///     TLS 1.2 PRF (RFC 5246 5): P_SHA256 only, which is all the
	///     TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 profile ever needs. Also the derivation helpers
	///     built on it: master secret (extended, RFC 7627, and classic), key block, and Finished
	///     verify_data.
	/// </summary>
	internal static class Prf
	{
		/// <summary>P_SHA256(secret, label || seed) expanded to fill <paramref name="output" /> (RFC 5246 5).</summary>
		public static void Compute(ReadOnlySpan<byte> secret, string label, ReadOnlySpan<byte> seed, Span<byte> output)
		{
			Span<byte> labelSeed = stackalloc byte[label.Length + seed.Length];
			for (int i = 0; i < label.Length; i++) labelSeed[i] = (byte) label[i];
			seed.CopyTo(labelSeed.Slice(label.Length));

			// A(0) = labelSeed; A(i) = HMAC(secret, A(i-1)); output blocks = HMAC(secret, A(i) || labelSeed)
			Span<byte> a = stackalloc byte[32];
			Span<byte> block = stackalloc byte[32];
			Span<byte> hmacInput = stackalloc byte[32 + labelSeed.Length];

			HMACSHA256.HashData(secret, labelSeed, a);

			int written = 0;
			while (written < output.Length)
			{
				a.CopyTo(hmacInput);
				labelSeed.CopyTo(hmacInput.Slice(32));
				HMACSHA256.HashData(secret, hmacInput, block);

				int n = Math.Min(32, output.Length - written);
				block.Slice(0, n).CopyTo(output.Slice(written));
				written += n;

				HMACSHA256.HashData(secret, a, a); // A(i+1)
			}
		}

		/// <summary>RFC 7627 extended master secret: PRF(pre_master, "extended master secret", session_hash)[0..47].</summary>
		public static byte[] ExtendedMasterSecret(ReadOnlySpan<byte> preMaster, ReadOnlySpan<byte> sessionHash)
		{
			byte[] master = new byte[48];
			Compute(preMaster, "extended master secret", sessionHash, master);
			return master;
		}

		/// <summary>RFC 5246 8.1 classic master secret: PRF(pre_master, "master secret", client_random || server_random)[0..47].</summary>
		public static byte[] ClassicMasterSecret(ReadOnlySpan<byte> preMaster, ReadOnlySpan<byte> clientRandom, ReadOnlySpan<byte> serverRandom)
		{
			Span<byte> seed = stackalloc byte[64];
			clientRandom.CopyTo(seed);
			serverRandom.CopyTo(seed.Slice(32));
			byte[] master = new byte[48];
			Compute(preMaster, "master secret", seed, master);
			return master;
		}

		/// <summary>
		///     RFC 5246 6.3 key block for an AEAD suite (no MAC keys): client_write_key(16),
		///     server_write_key(16), client_write_IV(4), server_write_IV(4), seeded with
		///     server_random || client_random (note the reversed order vs the master secret seed).
		/// </summary>
		public static void KeyBlock(ReadOnlySpan<byte> master, ReadOnlySpan<byte> clientRandom, ReadOnlySpan<byte> serverRandom,
			Span<byte> clientKey, Span<byte> serverKey, Span<byte> clientSalt, Span<byte> serverSalt)
		{
			Span<byte> seed = stackalloc byte[64];
			serverRandom.CopyTo(seed);
			clientRandom.CopyTo(seed.Slice(32));

			Span<byte> block = stackalloc byte[16 + 16 + 4 + 4];
			Compute(master, "key expansion", seed, block);

			block.Slice(0, 16).CopyTo(clientKey);
			block.Slice(16, 16).CopyTo(serverKey);
			block.Slice(32, 4).CopyTo(clientSalt);
			block.Slice(36, 4).CopyTo(serverSalt);
		}

		/// <summary>RFC 5246 7.4.9 Finished verify_data: PRF(master, label, Hash(transcript))[0..11].</summary>
		public static void VerifyData(ReadOnlySpan<byte> master, bool client, ReadOnlySpan<byte> transcriptHash, Span<byte> output12)
		{
			Compute(master, client ? "client finished" : "server finished", transcriptHash, output12);
		}
	}

	/// <summary>
	///     The running SHA-256 over every handshake message body (header included, records excluded),
	///     RFC 5246 7.4.9 / RFC 6347 4.2.6: when a cookie exchange happens, the first ClientHello and
	///     the HelloVerifyRequest are NOT part of the transcript - the caller simply does not append
	///     them. Snapshots (session_hash for the extended master secret, the CertificateVerify point,
	///     each Finished point) clone the running state.
	/// </summary>
	internal sealed class Transcript : IDisposable
	{
		private IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

		public void Append(ReadOnlySpan<byte> handshakeMessage) => _hash.AppendData(handshakeMessage);

		/// <summary>Hash of everything appended so far, without disturbing the running state.</summary>
		public byte[] Snapshot()
		{
			// IncrementalHash has no clone, but GetCurrentHash (netcore3.0+) reads the running hash
			// without resetting it, so no separate copy of the appended data is needed to snapshot it.
			byte[] hash = new byte[32];
			_hash.TryGetCurrentHash(hash, out _);
			return hash;
		}

		public void Dispose() => _hash.Dispose();
	}
}