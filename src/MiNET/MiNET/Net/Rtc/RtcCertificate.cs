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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.X509;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     A self-signed ECDSA P-256 identity for one DTLS endpoint. WebRTC never validates the
	///     certificate chain against a CA; the SDP offer/answer carries <see cref="FingerprintSha256" />
	///     out of band (over the already-authenticated signalling channel) and the DTLS handshake is
	///     trusted only when the peer's presented leaf hashes to that pinned value.
	/// </summary>
	public class RtcCertificate
	{
		public Certificate Certificate { get; }
		public AsymmetricKeyParameter PrivateKey { get; }
		public string FingerprintSha256 { get; }

		private RtcCertificate(Certificate certificate, AsymmetricKeyParameter privateKey, string fingerprintSha256)
		{
			Certificate = certificate;
			PrivateKey = privateKey;
			FingerprintSha256 = fingerprintSha256;
		}

		public static RtcCertificate CreateSelfSigned()
		{
			var random = new SecureRandom(new CryptoApiRandomGenerator());

			X9ECParameters curve = ECNamedCurveTable.GetByName("secp256r1");
			var domainParameters = new ECDomainParameters(curve);

			var keyPairGenerator = new ECKeyPairGenerator("ECDSA");
			keyPairGenerator.Init(new ECKeyGenerationParameters(domainParameters, random));
			AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();

			string commonName = "MiNET-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
			var subjectDn = new X509Name(new List<DerObjectIdentifier> {X509Name.CN}, new Dictionary<DerObjectIdentifier, string> {{X509Name.CN, commonName}});

			var generator = new X509V3CertificateGenerator();
			generator.SetIssuerDN(subjectDn);
			generator.SetSubjectDN(subjectDn);
			generator.SetPublicKey(keyPair.Public);
			generator.SetNotBefore(DateTime.UtcNow.AddDays(-1));
			generator.SetNotAfter(DateTime.UtcNow.AddYears(1));

			byte[] serialBytes = new byte[16];
			random.NextBytes(serialBytes);
			serialBytes[0] &= 0x7F; // keep the serial positive
			generator.SetSerialNumber(new BigInteger(1, serialBytes));

			ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256withECDSA", keyPair.Private, random);
			X509Certificate x509Certificate = generator.Generate(signatureFactory);

			var crypto = new BcTlsCrypto(random);
			Org.BouncyCastle.Tls.Crypto.TlsCertificate tlsCertificate = crypto.CreateCertificate(x509Certificate.GetEncoded());
			var certificate = new Certificate(null, new[] {new CertificateEntry(tlsCertificate, null)});

			string fingerprint = ComputeFingerprint(x509Certificate.GetEncoded());

			return new RtcCertificate(certificate, keyPair.Private, fingerprint);
		}

		/// <summary>
		///     SHA-256 over the leaf's DER encoding, formatted the way SDP (and this class's own
		///     callers comparing two fingerprints) expect: uppercase hex pairs colon-joined.
		/// </summary>
		public static string ComputeFingerprint(byte[] derEncodedCertificate)
		{
			byte[] hash = SHA256.HashData(derEncodedCertificate);

			var builder = new StringBuilder(hash.Length * 3 - 1);
			for (int i = 0; i < hash.Length; i++)
			{
				if (i > 0) builder.Append(':');
				builder.Append(hash[i].ToString("X2"));
			}

			return builder.ToString();
		}
	}
}