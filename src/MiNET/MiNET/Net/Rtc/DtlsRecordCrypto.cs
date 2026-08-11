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
using System.Threading;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     Native DTLS 1.2 epoch-1 record protection: encode/decode, the 48-bit send sequence, and the
	///     RFC 6347 4.1.2.6 anti-replay window, all on <see cref="AesGcm" /> over spans with no
	///     BouncyCastle, socket, or lock anywhere in this class. Not thread-safe on its own:
	///     <see cref="EncryptRecord" /> and <see cref="TryDecryptRecord" /> both mutate instance state
	///     (the send sequence, the replay window) with no synchronization, so a caller with concurrent
	///     senders or receivers on the same instance must serialize every call itself
	///     (<see cref="DtlsSession" /> does this with its own send/receive gates).
	///     <para>
	///     Record layout (RFC 6347 4.1): <c>type(1) | version(2)=0xFEFD | epoch(2) | sequence(6) |
	///     length(2) | fragment</c>, big-endian throughout. The AEAD fragment (RFC 5288):
	///     <c>explicit_nonce(8) | ciphertext | tag(16)</c>, where the explicit nonce is exactly
	///     <c>epoch(2) | sequence(6)</c>. The GCM nonce is <c>write_IV_salt(4) | explicit_nonce(8)</c>
	///     (12 bytes); the AAD (RFC 5246 6.2.3.3, adapted for the DTLS sequence field) is
	///     <c>epoch(2) | sequence(6) | type(1) | version(2) | plaintext_length(2)</c> (13 bytes). A
	///     receiver uses the explicit nonce exactly as received and never reconstructs it from the header.
	///     </para>
	/// </summary>
	internal sealed class DtlsRecordCrypto : IDisposable
	{
		internal const int HeaderLength = 13;
		private const int ExplicitNonceLength = 8;
		private const int TagLength = 16;
		private const int SaltLength = 4;
		private const int NonceLength = SaltLength + ExplicitNonceLength;
		private const int AadLength = HeaderLength;
		private const ushort RecordVersion = 0xFEFD;
		private const int Epoch1 = 1;
		private const int ReplayWindowSize = 64;
		private const ulong MaxSequence = (1UL << 48) - 1;

		/// <summary>
		///     Every instance is built from a <see cref="CapturedDtlsKeys" />, which only exists after a
		///     completed BouncyCastle handshake, and that handshake has by definition already sent its
		///     Finished message (plus one retransmission per lost final flight) under epoch 1, consuming
		///     the low send sequence numbers before this class ever protects a byte. Starting fresh at 0
		///     would repeat one of those (epoch, sequence) pairs: a GCM nonce reused on the wire, and a
		///     guaranteed replay-window drop at the peer, which cannot distinguish an authentic collision
		///     from ordinary datagram loss. Defaulting the constructor to this headroom, rather than
		///     leaning on a caller to seed it correctly, means there is no sequence to get wrong: every
		///     instance is safe to send from the moment it exists.
		/// </summary>
		internal const ulong SendSequenceHandshakeHeadroom = 1000;

		/// <summary>Header(13) + explicit nonce(8) + tag(16): every byte an encrypted record carries beyond its plaintext.</summary>
		public const int RecordOverhead = HeaderLength + ExplicitNonceLength + TagLength;

		private readonly AesGcm _sendCipher;
		private readonly AesGcm _receiveCipher;
		private readonly byte[] _sendSalt;
		private readonly byte[] _receiveSalt;

		private ulong _sendSequence;
		private bool _hasReceivedAny;
		private ulong _highestReceivedSequence;
		private ulong _replayWindowMask;

		private long _replayDrops;
		private long _decryptFailures;
		private long _malformedRecords;

		public long ReplayDrops => Interlocked.Read(ref _replayDrops);
		public long DecryptFailures => Interlocked.Read(ref _decryptFailures);
		public long MalformedRecords => Interlocked.Read(ref _malformedRecords);

		/// <summary>
		///     Builds both directions' ciphers from one captured key block. <paramref name="isServer" />
		///     picks which of the two roles' key/salt pairs is ours to send with; the other role's pair
		///     is the peer's, used to receive, per the wire convention that a client always sends with
		///     <see cref="CapturedDtlsKeys.ClientWriteKey" />/<see cref="CapturedDtlsKeys.ClientWriteIv" />
		///     and a server always sends with the server pair. The send sequence starts at
		///     <see cref="SendSequenceHandshakeHeadroom" />, not 0; see that constant's remarks.
		/// </summary>
		public DtlsRecordCrypto(CapturedDtlsKeys keys, bool isServer)
		{
			byte[] sendKey = isServer ? keys.ServerWriteKey : keys.ClientWriteKey;
			byte[] receiveKey = isServer ? keys.ClientWriteKey : keys.ServerWriteKey;
			_sendSalt = isServer ? keys.ServerWriteIv : keys.ClientWriteIv;
			_receiveSalt = isServer ? keys.ClientWriteIv : keys.ServerWriteIv;
			_sendCipher = new AesGcm(sendKey, TagLength);
			_receiveCipher = new AesGcm(receiveKey, TagLength);
			_sendSequence = SendSequenceHandshakeHeadroom;
		}

		/// <summary>
		///     Writes one complete epoch-1 record for <paramref name="plaintext" /> at the next send
		///     sequence and returns its length. Returns -1, writing nothing, if
		///     <paramref name="destination" /> cannot hold <see cref="RecordOverhead" /> plus
		///     <paramref name="plaintext" />'s length, if the encoded fragment would exceed the 16-bit
		///     DTLS length field, or if the 48-bit sequence space (RFC 6347 4.1) is exhausted;
		///     renegotiation does not exist in this stack, so exhaustion ends the association rather than
		///     resetting the sequence. Not thread-safe: see the class remarks.
		/// </summary>
		public int EncryptRecord(byte contentType, ReadOnlySpan<byte> plaintext, Span<byte> destination)
		{
			// MaxSequence (2^48-1) itself is never sent: RFC 6347 4.1 caps the field at 48 bits, and
			// with no renegotiation in this stack there is no way to reset it once reached, so the
			// association tears down one value early rather than emitting a record whose sequence
			// could never be distinguished from "not yet wrapped" by a peer.
			if (_sendSequence >= MaxSequence) return -1;
			if (plaintext.Length > ushort.MaxValue - ExplicitNonceLength - TagLength) return -1;

			int recordLength = RecordOverhead + plaintext.Length;
			if (destination.Length < recordLength) return -1;

			ulong sequence = _sendSequence++;

			Span<byte> header = destination.Slice(0, HeaderLength);
			header[0] = contentType;
			header[1] = (byte) (RecordVersion >> 8);
			header[2] = (byte) (RecordVersion & 0xFF);
			BinaryPrimitives.WriteUInt16BigEndian(header.Slice(3, 2), Epoch1);
			WriteUInt48BigEndian(header.Slice(5, 6), sequence);
			ushort fragmentLength = (ushort) (ExplicitNonceLength + plaintext.Length + TagLength);
			BinaryPrimitives.WriteUInt16BigEndian(header.Slice(11, 2), fragmentLength);

			// The explicit nonce is epoch(2) | sequence(6): the same 8 bytes just written into the
			// header, never an independently chosen value (RFC 5288 requires a receiver to use it as
			// sent, which only holds meaning if a sender never diverges from the header field either).
			Span<byte> explicitNonce = destination.Slice(HeaderLength, ExplicitNonceLength);
			header.Slice(3, 8).CopyTo(explicitNonce);

			Span<byte> nonce = stackalloc byte[NonceLength];
			_sendSalt.CopyTo(nonce);
			explicitNonce.CopyTo(nonce.Slice(SaltLength));

			Span<byte> aad = stackalloc byte[AadLength];
			BuildAad(aad, header.Slice(3, 8), contentType, header[1], header[2], plaintext.Length);

			Span<byte> ciphertext = destination.Slice(HeaderLength + ExplicitNonceLength, plaintext.Length);
			Span<byte> tag = destination.Slice(HeaderLength + ExplicitNonceLength + plaintext.Length, TagLength);
			_sendCipher.Encrypt(nonce, plaintext, ciphertext, tag, aad);

			return recordLength;
		}

		/// <summary>
		///     Decrypts one already-framed epoch-1 record (header included) into
		///     <paramref name="destination" />. Every rejection is drop-and-count, never a throw: wrong
		///     version, an epoch other than 1, a replay-window reject (RFC 6347 4.1.2.6), a fragment
		///     shorter than <see cref="ExplicitNonceLength" /> + <see cref="TagLength" />, a
		///     <paramref name="destination" /> too small for the plaintext, or a tag failure
		///     (<see cref="AesGcm.Decrypt(ReadOnlySpan{byte},ReadOnlySpan{byte},ReadOnlySpan{byte},Span{byte},ReadOnlySpan{byte})" />
		///     throwing <see cref="AuthenticationTagMismatchException" />, caught here and nowhere else)
		///     all return <see langword="false" />. The replay window only advances after decryption has
		///     actually authenticated the record, so a forged sequence number can never poison it.
		/// </summary>
		public bool TryDecryptRecord(ReadOnlySpan<byte> record, Span<byte> destination, out byte contentType, out int length)
		{
			contentType = 0;
			length = 0;

			if (!TryReadRecordHeader(record, out contentType, out int epoch, out int fragmentLength))
			{
				Interlocked.Increment(ref _malformedRecords);
				return false;
			}

			ushort version = (ushort) ((record[1] << 8) | record[2]);
			if (version != RecordVersion || epoch != Epoch1 || fragmentLength < ExplicitNonceLength + TagLength)
			{
				Interlocked.Increment(ref _malformedRecords);
				return false;
			}

			int ciphertextLength = fragmentLength - ExplicitNonceLength - TagLength;
			if (destination.Length < ciphertextLength)
			{
				Interlocked.Increment(ref _malformedRecords);
				return false;
			}

			ulong sequence = ReadUInt48BigEndian(record.Slice(5, 6));
			if (!IsWithinReplayWindow(sequence))
			{
				Interlocked.Increment(ref _replayDrops);
				return false;
			}

			ReadOnlySpan<byte> fragment = record.Slice(HeaderLength, fragmentLength);
			ReadOnlySpan<byte> explicitNonce = fragment.Slice(0, ExplicitNonceLength);
			ReadOnlySpan<byte> ciphertext = fragment.Slice(ExplicitNonceLength, ciphertextLength);
			ReadOnlySpan<byte> tag = fragment.Slice(ExplicitNonceLength + ciphertextLength, TagLength);

			Span<byte> nonce = stackalloc byte[NonceLength];
			_receiveSalt.CopyTo(nonce);
			explicitNonce.CopyTo(nonce.Slice(SaltLength));

			Span<byte> aad = stackalloc byte[AadLength];
			BuildAad(aad, record.Slice(3, 8), contentType, record[1], record[2], ciphertextLength);

			Span<byte> plaintext = destination.Slice(0, ciphertextLength);
			try
			{
				_receiveCipher.Decrypt(nonce, ciphertext, tag, plaintext, aad);
			}
			catch (AuthenticationTagMismatchException)
			{
				Interlocked.Increment(ref _decryptFailures);
				return false;
			}

			AdvanceReplayWindow(sequence);
			length = ciphertextLength;
			return true;
		}

		/// <summary>
		///     The framing peek a caller walking a multi-record datagram uses to find each record's
		///     boundary without decrypting it: <see langword="false" /> if <paramref name="datagram" />
		///     is shorter than the fixed 13-byte header, or the declared fragment length runs past
		///     whatever remains of it. Does not itself validate version or epoch;
		///     <see cref="TryDecryptRecord" /> does that once it has a correctly-bounded record to look at.
		/// </summary>
		public static bool TryReadRecordHeader(ReadOnlySpan<byte> datagram, out byte contentType, out int epoch, out int fragmentLength)
		{
			contentType = 0;
			epoch = 0;
			fragmentLength = 0;

			if (datagram.Length < HeaderLength) return false;

			contentType = datagram[0];
			epoch = (datagram[3] << 8) | datagram[4];
			fragmentLength = (datagram[11] << 8) | datagram[12];

			return HeaderLength + fragmentLength <= datagram.Length;
		}

		/// <summary>RFC 5246 6.2.3.3's AEAD additional data, adapted for the DTLS sequence field: epoch(2) | sequence(6) | type(1) | version(2) | plaintext_length(2).</summary>
		private static void BuildAad(Span<byte> aad, ReadOnlySpan<byte> epochAndSequence, byte contentType, byte versionHigh, byte versionLow, int plaintextLength)
		{
			epochAndSequence.CopyTo(aad);
			aad[8] = contentType;
			aad[9] = versionHigh;
			aad[10] = versionLow;
			BinaryPrimitives.WriteUInt16BigEndian(aad.Slice(11, 2), (ushort) plaintextLength);
		}

		/// <summary>
		///     RFC 6347 4.1.2.6: a sequence at or right of the highest ever received is always admitted
		///     (the window itself only ever slides forward, in <see cref="AdvanceReplayWindow" />, and
		///     only after a successful decrypt); one 64 or more behind the highest is too old; one inside
		///     the 64-wide window is admitted unless its bit is already set, meaning it was already
		///     received.
		/// </summary>
		private bool IsWithinReplayWindow(ulong sequence)
		{
			if (!_hasReceivedAny || sequence > _highestReceivedSequence) return true;

			ulong behind = _highestReceivedSequence - sequence;
			if (behind >= ReplayWindowSize) return false;

			return (_replayWindowMask & (1UL << (int) behind)) == 0;
		}

		/// <summary>Called only after <see cref="TryDecryptRecord" /> has authenticated <paramref name="sequence" />; bit 0 of the mask always marks the current <see cref="_highestReceivedSequence" /> itself.</summary>
		private void AdvanceReplayWindow(ulong sequence)
		{
			if (!_hasReceivedAny)
			{
				_hasReceivedAny = true;
				_highestReceivedSequence = sequence;
				_replayWindowMask = 1UL;
				return;
			}

			if (sequence > _highestReceivedSequence)
			{
				ulong shift = sequence - _highestReceivedSequence;
				_replayWindowMask = shift >= ReplayWindowSize ? 0UL : _replayWindowMask << (int) shift;
				_replayWindowMask |= 1UL;
				_highestReceivedSequence = sequence;
			}
			else
			{
				_replayWindowMask |= 1UL << (int) (_highestReceivedSequence - sequence);
			}
		}

		private static void WriteUInt48BigEndian(Span<byte> destination, ulong value)
		{
			destination[0] = (byte) (value >> 40);
			destination[1] = (byte) (value >> 32);
			destination[2] = (byte) (value >> 24);
			destination[3] = (byte) (value >> 16);
			destination[4] = (byte) (value >> 8);
			destination[5] = (byte) value;
		}

		private static ulong ReadUInt48BigEndian(ReadOnlySpan<byte> source)
		{
			return ((ulong) source[0] << 40) | ((ulong) source[1] << 32) | ((ulong) source[2] << 24) |
				((ulong) source[3] << 16) | ((ulong) source[4] << 8) | source[5];
		}

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): seeds the send sequence directly so a test can reach the 2^48-1 exhaustion boundary without actually encrypting that many records first.</summary>
		internal void SetSendSequenceForTesting(ulong sequence)
		{
			_sendSequence = sequence;
		}

		public void Dispose()
		{
			_sendCipher.Dispose();
			_receiveCipher.Dispose();
		}
	}
}