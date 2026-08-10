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
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     Fingerprint pinning shared by both handshake roles: WebRTC never chains a DTLS certificate
	///     to a CA, so the only trust anchor is the SHA-256 fingerprint carried out of band in SDP.
	///     A mismatch is a hard handshake failure, not a warning.
	/// </summary>
	internal static class DtlsFingerprint
	{
		public static void Verify(Certificate presented, string expectedFingerprint)
		{
			if (presented == null || presented.IsEmpty)
			{
				throw new TlsFatalAlert(AlertDescription.bad_certificate);
			}

			byte[] leafDer = presented.GetCertificateAt(0).GetEncoded();
			string actualFingerprint = RtcCertificate.ComputeFingerprint(leafDer);

			if (!string.Equals(actualFingerprint, expectedFingerprint, StringComparison.OrdinalIgnoreCase))
			{
				throw new TlsFatalAlert(AlertDescription.bad_certificate);
			}
		}
	}

	/// <summary>
	///     Server side of the DTLS handshake used to secure the WebRTC data channel. There is no
	///     SRTP negotiation here (no protection profile extension, no key export): this stack
	///     terminates at the DTLS record layer and SCTP rides the plain application-data stream
	///     from there.
	/// </summary>
	internal sealed class DtlsHandshakeServer : DefaultTlsServer
	{
		// AbstractTlsPeer.GetHandshakeTimeoutMillis() defaults to 0, which BouncyCastle's internal
		// Timeout.ForWaitMillis treats as "never expires": an unreachable or protocol-incompatible
		// peer would retransmit forever with no exception ever thrown. Bound it.
		private const int HandshakeTimeoutMillis = 10000;

		private readonly RtcCertificate _localCertificate;
		private readonly string _expectedRemoteFingerprint;

		public DtlsHandshakeServer(RtcCertificate localCertificate, string expectedRemoteFingerprint)
			: base(new BcTlsCrypto())
		{
			_localCertificate = localCertificate;
			_expectedRemoteFingerprint = expectedRemoteFingerprint;
		}

		public override bool RequiresExtendedMasterSecret()
		{
			return true;
		}

		public override int GetHandshakeTimeoutMillis()
		{
			return HandshakeTimeoutMillis;
		}

		protected override ProtocolVersion[] GetSupportedVersions()
		{
			return ProtocolVersion.DTLSv12.Only();
		}

		protected override int[] GetSupportedCipherSuites()
		{
			return new[]
			{
				CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
				CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384
			};
		}

		public override CertificateRequest GetCertificateRequest()
		{
			var certificateTypes = new[] {ClientCertificateType.ecdsa_sign};

			IList<SignatureAndHashAlgorithm> serverSigAlgs = null;
			if (TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(m_context.ServerVersion))
			{
				serverSigAlgs = TlsUtilities.GetDefaultSupportedSignatureAlgorithms(m_context);
			}

			return new CertificateRequest(certificateTypes, serverSigAlgs, null);
		}

		public override void NotifyClientCertificate(Certificate clientCertificate)
		{
			DtlsFingerprint.Verify(clientCertificate, _expectedRemoteFingerprint);
		}

		protected override TlsCredentialedSigner GetECDsaSignerCredentials()
		{
			SignatureAndHashAlgorithm signatureAndHashAlgorithm = SelectSignatureAndHashAlgorithm(m_context.SecurityParameters.ClientSigAlgs);
			return new BcDefaultTlsCredentialedSigner(new TlsCryptoParameters(m_context), (BcTlsCrypto) m_context.Crypto, _localCertificate.PrivateKey, _localCertificate.Certificate, signatureAndHashAlgorithm);
		}

		internal static SignatureAndHashAlgorithm SelectSignatureAndHashAlgorithm(IList<SignatureAndHashAlgorithm> peerSigAlgs)
		{
			if (peerSigAlgs != null)
			{
				foreach (SignatureAndHashAlgorithm algorithm in peerSigAlgs)
				{
					if (algorithm.Signature == SignatureAlgorithm.ecdsa && algorithm.Hash == HashAlgorithm.sha256)
					{
						return algorithm;
					}
				}
			}

			throw new InvalidOperationException("DTLS peer does not support ECDSA with SHA-256.");
		}
	}

	/// <summary>
	///     Client side of the DTLS handshake, with the same absence of SRTP negotiation as
	///     <see cref="DtlsHandshakeServer" />.
	/// </summary>
	internal sealed class DtlsHandshakeClient : DefaultTlsClient
	{
		// See DtlsHandshakeServer.HandshakeTimeoutMillis: without this override BouncyCastle
		// never gives up on an unreachable or protocol-incompatible peer.
		private const int HandshakeTimeoutMillis = 10000;

		private readonly RtcCertificate _localCertificate;
		private readonly string _expectedRemoteFingerprint;

		public DtlsHandshakeClient(RtcCertificate localCertificate, string expectedRemoteFingerprint)
			: base(new BcTlsCrypto())
		{
			_localCertificate = localCertificate;
			_expectedRemoteFingerprint = expectedRemoteFingerprint;
		}

		public override bool RequiresExtendedMasterSecret()
		{
			return true;
		}

		public override int GetHandshakeTimeoutMillis()
		{
			return HandshakeTimeoutMillis;
		}

		protected override ProtocolVersion[] GetSupportedVersions()
		{
			return ProtocolVersion.DTLSv12.Only();
		}

		protected override int[] GetSupportedCipherSuites()
		{
			return new[]
			{
				CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
				CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384
			};
		}

		public override TlsAuthentication GetAuthentication()
		{
			return new ClientAuthentication(m_context, _localCertificate, _expectedRemoteFingerprint);
		}

		private sealed class ClientAuthentication : TlsAuthentication
		{
			private readonly TlsContext _context;
			private readonly RtcCertificate _localCertificate;
			private readonly string _expectedRemoteFingerprint;

			public ClientAuthentication(TlsContext context, RtcCertificate localCertificate, string expectedRemoteFingerprint)
			{
				_context = context;
				_localCertificate = localCertificate;
				_expectedRemoteFingerprint = expectedRemoteFingerprint;
			}

			public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
			{
				DtlsFingerprint.Verify(serverCertificate?.Certificate, _expectedRemoteFingerprint);
			}

			public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
			{
				SignatureAndHashAlgorithm signatureAndHashAlgorithm = DtlsHandshakeServer.SelectSignatureAndHashAlgorithm(_context.SecurityParameters.ClientSigAlgs);
				return new BcDefaultTlsCredentialedSigner(new TlsCryptoParameters(_context), (BcTlsCrypto) _context.Crypto, _localCertificate.PrivateKey, _localCertificate.Certificate, signatureAndHashAlgorithm);
			}
		}
	}
}