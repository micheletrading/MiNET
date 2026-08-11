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
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace MiNET.Net.Rtc.FastDtls
{
	internal enum ContentType : byte
	{
		ChangeCipherSpec = 20,
		Alert = 21,
		Handshake = 22,
		ApplicationData = 23,
	}

	/// <summary>
	///     DTLS record encoding for the handshake module: epoch-0 plaintext both ways, and epoch-1
	///     AES-128-GCM for the Finished flight (RFC 5246 6.2.3.3 AEAD record protection with DTLS's
	///     epoch||seq nonce/AAD shape, RFC 6347 4.1). Application-data records after the handshake are
	///     the production record layer's job, not this codec's - the engine hands the negotiated keys
	///     over and stops.
	/// </summary>
	internal static class DtlsRecords
	{
		public const int HeaderLength = 13;
		public static readonly byte[] Dtls12 = { 254, 253 };
		public static readonly byte[] Dtls10 = { 254, 255 }; // legal on first-flight plaintext records

		public static int WriteHeader(Span<byte> destination, ContentType type, ushort epoch, ulong seq48, int payloadLength)
		{
			destination[0] = (byte) type;
			destination[1] = 254;
			destination[2] = 253;
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(3), epoch);
			destination[5] = (byte) (seq48 >> 40);
			destination[6] = (byte) (seq48 >> 32);
			destination[7] = (byte) (seq48 >> 24);
			destination[8] = (byte) (seq48 >> 16);
			destination[9] = (byte) (seq48 >> 8);
			destination[10] = (byte) seq48;
			BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(11), (ushort) payloadLength);
			return HeaderLength;
		}

		public static bool TryReadHeader(ReadOnlySpan<byte> datagram, out ContentType type, out ushort epoch, out ulong seq48, out int payloadLength)
		{
			type = default; epoch = 0; seq48 = 0; payloadLength = 0;
			if (datagram.Length < HeaderLength) return false;

			type = (ContentType) datagram[0];
			// version bytes accepted as-is: {254,255} on first flight, {254,253} after - not load-bearing
			epoch = BinaryPrimitives.ReadUInt16BigEndian(datagram.Slice(3));
			seq48 = ((ulong) datagram[5] << 40) | ((ulong) datagram[6] << 32) | ((ulong) datagram[7] << 24)
				| ((ulong) datagram[8] << 16) | ((ulong) datagram[9] << 8) | datagram[10];
			payloadLength = BinaryPrimitives.ReadUInt16BigEndian(datagram.Slice(11));
			return datagram.Length >= HeaderLength + payloadLength;
		}
	}

	/// <summary>
	///     One direction of epoch-1 record protection during the handshake tail: AES-128-GCM with the
	///     4-byte implicit salt + 8-byte explicit (epoch||seq) nonce, the explicit part sent on the
	///     wire ahead of the ciphertext, AAD = epoch||seq || type || version || plaintext-length
	///     (RFC 5288 3 as profiled for DTLS).
	/// </summary>
	internal sealed class RecordCipher : IDisposable
	{
		private readonly AesGcm _aes;
		private readonly byte[] _salt = new byte[4];

		public RecordCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> salt)
		{
			_aes = new AesGcm(key, 16);
			salt.CopyTo(_salt);
		}

		/// <summary>Output layout: explicitNonce(8) || ciphertext || tag(16). Returns bytes written.</summary>
		public int Encrypt(ushort epoch, ulong seq48, ContentType type, ReadOnlySpan<byte> plaintext, Span<byte> output)
		{
			Span<byte> epochSeq = stackalloc byte[8];
			BinaryPrimitives.WriteUInt16BigEndian(epochSeq, epoch);
			epochSeq[2] = (byte) (seq48 >> 40);
			epochSeq[3] = (byte) (seq48 >> 32);
			epochSeq[4] = (byte) (seq48 >> 24);
			epochSeq[5] = (byte) (seq48 >> 16);
			epochSeq[6] = (byte) (seq48 >> 8);
			epochSeq[7] = (byte) seq48;

			Span<byte> nonce = stackalloc byte[12];
			_salt.CopyTo(nonce);
			epochSeq.CopyTo(nonce.Slice(4));

			Span<byte> aad = stackalloc byte[13];
			epochSeq.CopyTo(aad);
			aad[8] = (byte) type;
			aad[9] = 254;
			aad[10] = 253;
			BinaryPrimitives.WriteUInt16BigEndian(aad.Slice(11), (ushort) plaintext.Length);

			epochSeq.CopyTo(output); // explicit nonce on the wire
			_aes.Encrypt(nonce, plaintext, output.Slice(8, plaintext.Length), output.Slice(8 + plaintext.Length, 16), aad);
			return 8 + plaintext.Length + 16;
		}

		/// <summary>Input layout: explicitNonce(8) || ciphertext || tag(16). Returns plaintext length, or -1 on tag failure/short input.</summary>
		public int Decrypt(ushort epoch, ulong seq48, ContentType type, ReadOnlySpan<byte> recordPayload, Span<byte> plaintextOut)
		{
			if (recordPayload.Length < 8 + 16) return -1;
			int plaintextLength = recordPayload.Length - 8 - 16;

			Span<byte> epochSeq = stackalloc byte[8];
			BinaryPrimitives.WriteUInt16BigEndian(epochSeq, epoch);
			epochSeq[2] = (byte) (seq48 >> 40);
			epochSeq[3] = (byte) (seq48 >> 32);
			epochSeq[4] = (byte) (seq48 >> 24);
			epochSeq[5] = (byte) (seq48 >> 16);
			epochSeq[6] = (byte) (seq48 >> 8);
			epochSeq[7] = (byte) seq48;

			Span<byte> nonce = stackalloc byte[12];
			_salt.CopyTo(nonce);
			recordPayload.Slice(0, 8).CopyTo(nonce.Slice(4)); // sender's explicit nonce, verbatim

			Span<byte> aad = stackalloc byte[13];
			epochSeq.CopyTo(aad);
			aad[8] = (byte) type;
			aad[9] = 254;
			aad[10] = 253;
			BinaryPrimitives.WriteUInt16BigEndian(aad.Slice(11), (ushort) plaintextLength);

			try
			{
				_aes.Decrypt(nonce, recordPayload.Slice(8, plaintextLength), recordPayload.Slice(8 + plaintextLength, 16), plaintextOut.Slice(0, plaintextLength), aad);
				return plaintextLength;
			}
			catch (AuthenticationTagMismatchException)
			{
				return -1;
			}
		}

		public void Dispose() => _aes.Dispose();
	}
}