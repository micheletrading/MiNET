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
using System.Security.Cryptography.X509Certificates;

namespace MiNET.Net.Rtc.FastDtls
{
	/// <summary>
	///     The WebRTC certificate model on BCL primitives: a self-signed ECDSA P-256 certificate
	///     generated per peer (<see cref="X509Certificates.CertificateRequest" />), identified by its
	///     SHA-256 fingerprint (the SDP <c>a=fingerprint</c> value) rather than any chain of trust.
	/// </summary>
	public sealed class DtlsCertificate : IDisposable
	{
		public ECDsa PrivateKey { get; }
		public byte[] Der { get; }
		public byte[] Fingerprint { get; }

		private DtlsCertificate(ECDsa privateKey, byte[] der)
		{
			PrivateKey = privateKey;
			Der = der;
			Fingerprint = SHA256.HashData(der);
		}

		public static DtlsCertificate Generate(string commonName = "WebRTC")
		{
			var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256);
			using X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
			return new DtlsCertificate(key, cert.Export(X509ContentType.Cert));
		}

		/// <summary>The peer's certificate public key as an ECDsa verifier; throws on a non-P-256-ECDSA certificate (profile violation).</summary>
		public static ECDsa ExtractPublicKey(ReadOnlySpan<byte> der)
		{
			using var cert = X509CertificateLoader.LoadCertificate(der.ToArray());
			ECDsa key = cert.GetECDsaPublicKey();
			if (key == null) throw new CryptographicException("Peer certificate does not carry an ECDSA public key.");
			return key;
		}

		/// <summary>Colon-separated uppercase hex, the SDP a=fingerprint wire form.</summary>
		public static string FormatFingerprint(ReadOnlySpan<byte> fingerprint)
		{
			string hex = Convert.ToHexString(fingerprint);
			string[] pairs = new string[hex.Length / 2];
			for (int i = 0; i < pairs.Length; i++) pairs[i] = hex.Substring(i * 2, 2);
			return string.Join(':', pairs);
		}

		public void Dispose() => PrivateKey.Dispose();
	}
}