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

namespace MiNET.Net.Rtc.FastDtls
{
	internal enum HandshakeType : byte
	{
		HelloRequest = 0,
		ClientHello = 1,
		ServerHello = 2,
		HelloVerifyRequest = 3,
		Certificate = 11,
		ServerKeyExchange = 12,
		CertificateRequest = 13,
		ServerHelloDone = 14,
		CertificateVerify = 15,
		ClientKeyExchange = 16,
		Finished = 20,
	}

	/// <summary>
	///     Handshake message codecs for the fixed profile: DTLS 1.2,
	///     TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 (0xC02B), P-256 (named curve 23, uncompressed
	///     points), ecdsa_secp256r1_sha256 (0x0403), extended master secret, empty renegotiation_info.
	///     Every builder writes the message BODY (no 12-byte DTLS handshake header - the engine's
	///     flight writer owns headers, fragmentation, and message_seq).
	/// </summary>
	internal static class HandshakeMessages
	{
		public const ushort CipherSuite = 0xC02B;
		public const ushort NamedCurveP256 = 23;
		public const ushort SigAlgEcdsaSecp256r1Sha256 = 0x0403;

		private const ushort ExtRenegotiationInfo = 0xFF01;
		private const ushort ExtSupportedGroups = 0x000A;
		private const ushort ExtEcPointFormats = 0x000B;
		private const ushort ExtSignatureAlgorithms = 0x000D;
		private const ushort ExtUseSrtp = 0x000E;
		private const ushort ExtPadding = 0x0015; // RFC 7685, the ClientHello's MTU-probe ballast
		private const ushort ExtExtendedMasterSecret = 0x0017;

		/// <summary>SRTP_AES128_CM_HMAC_SHA1_80. Never used to derive a key here (SCTP rides plain application data), but RFC 8827 makes a spec-compliant WebRTC peer abort a handshake without RFC 5764 use_srtp, so the client always offers it and the server echoes whatever was offered.</summary>
		public const ushort SrtpDefaultProfile = 0x0001;

		/// <summary>
		///     <paramref name="padToLength" /> inflates the hello body to that size with an RFC 7685
		///     padding extension so the ClientHello datagram doubles as an MTU probe, the DTLS analog of
		///     RakNet padding OpenConnectionRequest1 to the size it is testing. -1 sends it unpadded.
		/// </summary>
		public static int WriteClientHello(Span<byte> b, ReadOnlySpan<byte> random32, ReadOnlySpan<byte> cookie, int padToLength = -1)
		{
			int n = 0;
			b[n++] = 254; b[n++] = 253; // client_version DTLS 1.2
			random32.CopyTo(b.Slice(n)); n += 32;
			b[n++] = 0; // session_id: empty
			b[n++] = (byte) cookie.Length; // DTLS cookie
			cookie.CopyTo(b.Slice(n)); n += cookie.Length;

			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), 2); n += 2; // cipher_suites length
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), CipherSuite); n += 2;
			b[n++] = 1; b[n++] = 0; // compression: null only

			int extLenAt = n; n += 2;
			n += WriteExtension(b.Slice(n), ExtRenegotiationInfo, new byte[] { 0 });
			n += WriteExtension(b.Slice(n), ExtSupportedGroups, new byte[] { 0, 2, 0, (byte) NamedCurveP256 });
			n += WriteExtension(b.Slice(n), ExtEcPointFormats, new byte[] { 1, 0 });
			n += WriteExtension(b.Slice(n), ExtSignatureAlgorithms, new byte[] { 0, 2, 0x04, 0x03 });
			n += WriteExtension(b.Slice(n), ExtUseSrtp, new byte[] { 0, 2, (byte) (SrtpDefaultProfile >> 8), (byte) SrtpDefaultProfile, 0 });
			n += WriteExtension(b.Slice(n), ExtExtendedMasterSecret, ReadOnlySpan<byte>.Empty);
			if (padToLength - n >= 4)
			{
				int zeros = padToLength - n - 4;
				BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), ExtPadding);
				BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n + 2), (ushort) zeros);
				b.Slice(n + 4, zeros).Clear();
				n = padToLength;
			}
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(extLenAt), (ushort) (n - extLenAt - 2));
			return n;
		}

		public static int WriteServerHello(Span<byte> b, ReadOnlySpan<byte> random32, bool extendedMasterSecret, int srtpProfile = -1)
		{
			int n = 0;
			b[n++] = 254; b[n++] = 253;
			random32.CopyTo(b.Slice(n)); n += 32;
			b[n++] = 0; // session_id: empty (no resumption in this profile)
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), CipherSuite); n += 2;
			b[n++] = 0; // compression null

			int extLenAt = n; n += 2;
			n += WriteExtension(b.Slice(n), ExtRenegotiationInfo, new byte[] { 0 });
			n += WriteExtension(b.Slice(n), ExtEcPointFormats, new byte[] { 1, 0 });
			if (srtpProfile >= 0) n += WriteExtension(b.Slice(n), ExtUseSrtp, new byte[] { 0, 2, (byte) (srtpProfile >> 8), (byte) srtpProfile, 0 });
			if (extendedMasterSecret) n += WriteExtension(b.Slice(n), ExtExtendedMasterSecret, ReadOnlySpan<byte>.Empty);
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(extLenAt), (ushort) (n - extLenAt - 2));
			return n;
		}

		public static int WriteHelloVerifyRequest(Span<byte> b, ReadOnlySpan<byte> cookie)
		{
			int n = 0;
			b[n++] = 254; b[n++] = 255; // RFC 6347 4.2.1: HVR version is DTLS 1.0
			b[n++] = (byte) cookie.Length;
			cookie.CopyTo(b.Slice(n)); n += cookie.Length;
			return n;
		}

		public static int WriteCertificate(Span<byte> b, ReadOnlySpan<byte> certDer)
		{
			int n = 0;
			WriteUInt24(b.Slice(n), certDer.Length + 3); n += 3; // certificate_list length
			WriteUInt24(b.Slice(n), certDer.Length); n += 3;
			certDer.CopyTo(b.Slice(n)); n += certDer.Length;
			return n;
		}

		/// <summary>ECDHE params + signature. <paramref name="signature" /> is the DER ECDSA signature over client_random || server_random || params.</summary>
		public static int WriteServerKeyExchange(Span<byte> b, ReadOnlySpan<byte> publicPoint65, ReadOnlySpan<byte> signature)
		{
			int n = WriteEcdheParams(b, publicPoint65);
			b[n++] = 0x04; b[n++] = 0x03; // SignatureAndHashAlgorithm: sha256, ecdsa
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), (ushort) signature.Length); n += 2;
			signature.CopyTo(b.Slice(n)); n += signature.Length;
			return n;
		}

		/// <summary>The signed-over portion of ServerKeyExchange (curve_type, named_curve, point) - shared by the builder above and both signature paths.</summary>
		public static int WriteEcdheParams(Span<byte> b, ReadOnlySpan<byte> publicPoint65)
		{
			int n = 0;
			b[n++] = 3; // curve_type: named_curve
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), NamedCurveP256); n += 2;
			b[n++] = (byte) publicPoint65.Length;
			publicPoint65.CopyTo(b.Slice(n)); n += publicPoint65.Length;
			return n;
		}

		public static int WriteCertificateRequest(Span<byte> b)
		{
			int n = 0;
			b[n++] = 1; b[n++] = 64; // certificate_types: ecdsa_sign
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), 2); n += 2; // supported_signature_algorithms length
			b[n++] = 0x04; b[n++] = 0x03;
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), 0); n += 2; // certificate_authorities: empty
			return n;
		}

		public static int WriteClientKeyExchange(Span<byte> b, ReadOnlySpan<byte> publicPoint65)
		{
			int n = 0;
			b[n++] = (byte) publicPoint65.Length;
			publicPoint65.CopyTo(b.Slice(n)); n += publicPoint65.Length;
			return n;
		}

		public static int WriteCertificateVerify(Span<byte> b, ReadOnlySpan<byte> signature)
		{
			int n = 0;
			b[n++] = 0x04; b[n++] = 0x03;
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(n), (ushort) signature.Length); n += 2;
			signature.CopyTo(b.Slice(n)); n += signature.Length;
			return n;
		}

		// ---- parsers (each returns false on any structural violation; the engine aborts the handshake) ----

		public readonly ref struct ParsedClientHello
		{
			public readonly ReadOnlySpan<byte> Random;
			public readonly ReadOnlySpan<byte> Cookie;
			public readonly bool OffersCipherSuite;
			public readonly bool OffersExtendedMasterSecret;
			public readonly int SrtpProfile; // first offered use_srtp profile, -1 when the extension is absent

			public ParsedClientHello(ReadOnlySpan<byte> random, ReadOnlySpan<byte> cookie, bool offersSuite, bool offersEms, int srtpProfile)
			{
				Random = random; Cookie = cookie; OffersCipherSuite = offersSuite; OffersExtendedMasterSecret = offersEms; SrtpProfile = srtpProfile;
			}
		}

		public static bool TryParseClientHello(ReadOnlySpan<byte> b, out ParsedClientHello parsed)
		{
			parsed = default;
			int n = 0;
			if (b.Length < 2 + 32 + 1) return false;
			n += 2; // client_version: not load-bearing (cookie exchange pins the real version)
			ReadOnlySpan<byte> random = b.Slice(n, 32); n += 32;
			int sessionIdLength = b[n++]; n += sessionIdLength;
			if (b.Length < n + 1) return false;
			int cookieLength = b[n++];
			if (b.Length < n + cookieLength) return false;
			ReadOnlySpan<byte> cookie = b.Slice(n, cookieLength); n += cookieLength;

			if (b.Length < n + 2) return false;
			int suitesLength = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n)); n += 2;
			if (b.Length < n + suitesLength || (suitesLength & 1) != 0) return false;
			bool offersSuite = false;
			for (int i = 0; i < suitesLength; i += 2)
			{
				if (BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n + i)) == CipherSuite) offersSuite = true;
			}
			n += suitesLength;

			if (b.Length < n + 1) return false;
			int compLength = b[n++]; n += compLength;

			bool offersEms = false;
			int srtpProfile = -1;
			if (b.Length >= n + 2)
			{
				int extTotal = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n)); n += 2;
				if (b.Length < n + extTotal) return false;
				int end = n + extTotal;
				while (n + 4 <= end)
				{
					ushort extType = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n));
					int extLength = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n + 2));
					n += 4;
					if (n + extLength > end) return false;
					if (extType == ExtExtendedMasterSecret) offersEms = true;
					if (extType == ExtUseSrtp && extLength >= 4 && BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n)) >= 2)
					{
						srtpProfile = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n + 2));
					}
					n += extLength;
				}
			}

			parsed = new ParsedClientHello(random, cookie, offersSuite, offersEms, srtpProfile);
			return true;
		}

		public static bool TryParseServerHello(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> random, out ushort cipherSuite, out bool extendedMasterSecret)
		{
			random = default; cipherSuite = 0; extendedMasterSecret = false;
			int n = 0;
			if (b.Length < 2 + 32 + 1) return false;
			n += 2;
			random = b.Slice(n, 32); n += 32;
			int sessionIdLength = b[n++]; n += sessionIdLength;
			if (b.Length < n + 3) return false;
			cipherSuite = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n)); n += 2;
			n += 1; // compression

			if (b.Length >= n + 2)
			{
				int extTotal = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n)); n += 2;
				if (b.Length < n + extTotal) return false;
				int end = n + extTotal;
				while (n + 4 <= end)
				{
					ushort extType = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n));
					int extLength = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n + 2));
					n += 4;
					if (n + extLength > end) return false;
					if (extType == ExtExtendedMasterSecret) extendedMasterSecret = true;
					n += extLength;
				}
			}

			return true;
		}

		public static bool TryParseHelloVerifyRequest(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> cookie)
		{
			cookie = default;
			if (b.Length < 3) return false;
			int cookieLength = b[2];
			if (b.Length < 3 + cookieLength) return false;
			cookie = b.Slice(3, cookieLength);
			return true;
		}

		/// <summary>First certificate of the chain only - the WebRTC trust model never walks a chain, it fingerprints the leaf.</summary>
		public static bool TryParseCertificate(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> leafDer)
		{
			leafDer = default;
			if (b.Length < 6) return false;
			int listLength = ReadUInt24(b);
			if (listLength < 3 || b.Length < 3 + listLength) return false;
			int leafLength = ReadUInt24(b.Slice(3));
			if (b.Length < 6 + leafLength) return false;
			leafDer = b.Slice(6, leafLength);
			return true;
		}

		public static bool TryParseServerKeyExchange(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> signedParams, out ReadOnlySpan<byte> publicPoint, out ReadOnlySpan<byte> signature)
		{
			signedParams = default; publicPoint = default; signature = default;
			if (b.Length < 4) return false;
			if (b[0] != 3) return false; // named_curve
			if (BinaryPrimitives.ReadUInt16BigEndian(b.Slice(1)) != NamedCurveP256) return false;
			int pointLength = b[3];
			if (b.Length < 4 + pointLength + 2 + 2) return false;
			publicPoint = b.Slice(4, pointLength);
			signedParams = b.Slice(0, 4 + pointLength);

			int n = 4 + pointLength;
			if (b[n] != 0x04 || b[n + 1] != 0x03) return false; // sha256/ecdsa only
			n += 2;
			int sigLength = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(n)); n += 2;
			if (b.Length < n + sigLength) return false;
			signature = b.Slice(n, sigLength);
			return true;
		}

		public static bool TryParseClientKeyExchange(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> publicPoint)
		{
			publicPoint = default;
			if (b.Length < 1) return false;
			int pointLength = b[0];
			if (b.Length < 1 + pointLength) return false;
			publicPoint = b.Slice(1, pointLength);
			return true;
		}

		public static bool TryParseCertificateVerify(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> signature)
		{
			signature = default;
			if (b.Length < 4) return false;
			if (b[0] != 0x04 || b[1] != 0x03) return false;
			int sigLength = BinaryPrimitives.ReadUInt16BigEndian(b.Slice(2));
			if (b.Length < 4 + sigLength) return false;
			signature = b.Slice(4, sigLength);
			return true;
		}

		private static int WriteExtension(Span<byte> b, ushort type, ReadOnlySpan<byte> body)
		{
			BinaryPrimitives.WriteUInt16BigEndian(b, type);
			BinaryPrimitives.WriteUInt16BigEndian(b.Slice(2), (ushort) body.Length);
			body.CopyTo(b.Slice(4));
			return 4 + body.Length;
		}

		public static void WriteUInt24(Span<byte> b, int value)
		{
			b[0] = (byte) (value >> 16);
			b[1] = (byte) (value >> 8);
			b[2] = (byte) value;
		}

		public static int ReadUInt24(ReadOnlySpan<byte> b) => (b[0] << 16) | (b[1] << 8) | b[2];
	}
}