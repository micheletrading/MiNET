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

using MiNET.Net.Rtc.FastDtls;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     A self-signed ECDSA P-256 identity for one DTLS endpoint. WebRTC never validates the
	///     certificate chain against a CA; the SDP offer/answer carries <see cref="FingerprintSha256" />
	///     out of band (over the already-authenticated signalling channel) and the DTLS handshake is
	///     trusted only when the peer's presented leaf hashes to that pinned value. A thin wrapper over
	///     <see cref="FastDtls.DtlsCertificate" />. Ownership stays with whoever calls
	///     <see cref="CreateSelfSigned" />: a <see cref="DtlsSession" /> built from this instance reads
	///     it but never disposes it, since one certificate is shared across every
	///     <see cref="DtlsSession" /> a server negotiates - the normal WebRTC shape - and disposing it
	///     from inside any one of those sessions would break every other peer still using it.
	/// </summary>
	public class RtcCertificate
	{
		internal DtlsCertificate DtlsCertificate { get; }
		public string FingerprintSha256 { get; }

		private RtcCertificate(DtlsCertificate certificate, string fingerprintSha256)
		{
			DtlsCertificate = certificate;
			FingerprintSha256 = fingerprintSha256;
		}

		public static RtcCertificate CreateSelfSigned()
		{
			DtlsCertificate certificate = DtlsCertificate.Generate();
			string fingerprint = DtlsCertificate.FormatFingerprint(certificate.Fingerprint);
			return new RtcCertificate(certificate, fingerprint);
		}
	}
}