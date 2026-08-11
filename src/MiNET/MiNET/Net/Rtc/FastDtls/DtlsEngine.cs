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

namespace MiNET.Net.Rtc.FastDtls
{
	public sealed class DtlsHandshakeException : Exception
	{
		public DtlsHandshakeException(string message) : base(message)
		{
		}
	}

	/// <summary>The negotiated AES-128-GCM keying material, the handoff to the production record layer.</summary>
	public sealed class DtlsNegotiatedKeys
	{
		public byte[] ClientWriteKey { get; } = new byte[16];
		public byte[] ClientWriteSalt { get; } = new byte[4];
		public byte[] ServerWriteKey { get; } = new byte[16];
		public byte[] ServerWriteSalt { get; } = new byte[4];
	}

	/// <summary>
	///     A DTLS 1.2 handshake state machine on BCL crypto only, both roles, for the fixed WebRTC
	///     profile: TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256, P-256 ECDHE, mutual self-signed ECDSA
	///     certificates trusted by fingerprint, extended master secret, cookie exchange as server,
	///     no resumption or renegotiation. Datagrams in via <see cref="HandleDatagram" />, datagrams
	///     out via the transmit callback; the host owns the socket and the retransmission timer
	///     (<see cref="Retransmit" /> re-sends the current flight). On completion the engine exposes
	///     <see cref="Keys" /> and stops - application-data record protection is the production
	///     record layer's job.
	/// </summary>
	public sealed class DtlsEngine : IDisposable
	{
		private enum State
		{
			// client
			ClientStart,
			ClientAwaitServerFirst, // HelloVerifyRequest or ServerHello
			ClientAwaitCertificate,
			ClientAwaitServerKeyExchange,
			ClientAwaitCertificateRequest,
			ClientAwaitServerHelloDone,
			ClientAwaitChangeCipherSpec,
			ClientAwaitFinished,

			// server
			ServerAwaitClientHello,
			ServerAwaitCertificate,
			ServerAwaitClientKeyExchange,
			ServerAwaitCertificateVerify,
			ServerAwaitChangeCipherSpec,
			ServerAwaitFinished,

			Complete,
			Failed,
		}

		private const byte AlertUnexpectedMessage = 10;
		private const byte AlertHandshakeFailure = 40;
		private const byte AlertBadCertificate = 42;
		private const byte AlertDecryptError = 51;
		private const byte AlertDecodeError = 50;

		/// <summary>
		///     Outbound MTU probe ladder in UDP-payload bytes, walked downward on retransmission
		///     timeouts the same way the RakNet offline handshake does it: 1472 is the Ethernet-path
		///     ceiling, 1200 the RFC 8831 WebRTC-safe size, 576 the classic conservative floor. A rung
		///     is only abandoned when the timed-out flight actually contained a datagram larger than
		///     the next rung, so plain packet loss on small flights never shrinks the MTU.
		/// </summary>
		private static readonly int[] MtuLadder = { 1472, 1200, 576 };

		/// <summary>Tries per rung before stepping down, matching the retransmission cadence MiNET's RakNet layer uses (300ms timer, two tries per size).</summary>
		private const int TimeoutsPerRung = 2;

		private readonly bool _isClient;
		private readonly DtlsCertificate _certificate;
		private readonly Action<byte[]> _transmit;
		private readonly int[] _ladder;
		private int _ladderIndex;
		private int _timeoutsAtRung;
		private int _lastFlightMaxDatagram;
		private readonly byte[] _expectedPeerFingerprint;

		private State _state;
		private readonly Transcript _transcript = new Transcript();
		private readonly ECDiffieHellman _ecdhe = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

		private readonly byte[] _localRandom = new byte[32];
		private readonly byte[] _peerRandom = new byte[32];
		private byte[] _peerPoint;
		private byte[] _master;
		private bool _extendedMasterSecret;

		private ECDsa _peerPublicKey;
		public byte[] PeerCertificateDer { get; private set; }
		public byte[] PeerFingerprint { get; private set; }
		public DtlsNegotiatedKeys Keys { get; private set; }
		public bool IsComplete => _state == State.Complete;

		// handshake sequencing
		private ushort _nextSendSeq;
		private ushort _nextReceiveSeq;
		private ulong _sendSeqEpoch0;
		private ulong _sendSeqEpoch1;
		private bool _peerChangedCipherSpec;
		private RecordCipher _writeCipher;
		private RecordCipher _readCipher;

		// fragment reassembly for the one in-order message we are waiting for
		private byte[] _reassemblyBody;
		private byte[] _reassemblyMask;
		private int _reassemblyReceived;
		private HandshakeType _reassemblyType;

		// the first ClientHello is only transcript-relevant when the server skips the cookie exchange
		private byte[] _bufferedFirstClientHello;
		private ushort _bufferedFirstClientHelloSeq;

		// server cookie: HMAC(secret, client_random), stateless per RFC 6347 4.2.1 intent
		private readonly byte[] _cookieSecret;

		// the current flight as MESSAGES, not datagrams: every (re)transmission rebuilds records at
		// the current rung with fresh record sequence numbers, which is what lets a retransmission
		// re-fragment smaller (RFC 6347 4.2.3) and stay clear of the peer's anti-replay window
		private readonly struct FlightEntry
		{
			public readonly bool IsChangeCipherSpec;
			public readonly HandshakeType Type;
			public readonly byte[] Body;
			public readonly ushort MessageSeq;
			public readonly bool Epoch1;

			public FlightEntry(bool isCcs, HandshakeType type, byte[] body, ushort messageSeq, bool epoch1)
			{
				IsChangeCipherSpec = isCcs; Type = type; Body = body; MessageSeq = messageSeq; Epoch1 = epoch1;
			}
		}

		private readonly List<FlightEntry> _flight = new List<FlightEntry>();
		private readonly byte[] _datagramBuffer;
		private int _datagramUsed;

		private int CurrentMtu => _ladder[_ladderIndex];

		// per-record overhead: 13-byte record header + 12-byte handshake header, plus the AEAD
		// explicit nonce and tag on epoch-1 records only; epoch 0 carries no margin so a padded
		// ClientHello can fill its datagram to exactly the rung under probe
		private int MaxFragmentEpoch0 => CurrentMtu - DtlsRecords.HeaderLength - 12;
		private int MaxFragmentEpoch1 => MaxFragmentEpoch0 - 8 - 16;

		/// <summary>The outbound MTU currently in effect; after completion, the discovered value the host hands to the SCTP layer.</summary>
		public int NegotiatedMtu => CurrentMtu;

		/// <summary>
		///     The next epoch-1 record sequence this engine's own transmissions will consume. After the
		///     handshake completes this is the exact seed for the production record layer: the Finished
		///     flight (and any retransmission of it) allocates from the same 48-bit space, same key, as
		///     application data, and two records under one key sharing a sequence number is an AES-GCM
		///     nonce reuse. The host serializes all access.
		/// </summary>
		public ulong NextEpoch1SendSequence => _sendSeqEpoch1;

		/// <summary>
		///     Forward-only: moves this engine's epoch-1 sequence to <paramref name="next" /> when that
		///     is ahead. The host calls it before a post-handshake <see cref="Retransmit" /> so the
		///     rebuilt flight allocates above everything the record layer has already sent.
		/// </summary>
		public void SeedEpoch1SendSequence(ulong next)
		{
			if (next > _sendSeqEpoch1) _sendSeqEpoch1 = next;
		}

		public DtlsEngine(bool isClient, DtlsCertificate certificate, Action<byte[]> transmit, int mtu = 1472, byte[] expectedPeerFingerprint = null)
		{
			_isClient = isClient;
			_certificate = certificate;
			_transmit = transmit;

			var ladder = new List<int> { mtu };
			foreach (int rung in MtuLadder)
			{
				if (rung < mtu) ladder.Add(rung);
			}
			_ladder = ladder.ToArray();
			if (MaxFragmentEpoch1 < 64) throw new ArgumentOutOfRangeException(nameof(mtu));
			_expectedPeerFingerprint = expectedPeerFingerprint;
			_datagramBuffer = new byte[mtu];

			RandomNumberGenerator.Fill(_localRandom);
			_cookieSecret = isClient ? null : RandomNumberGenerator.GetBytes(32);
			_state = isClient ? State.ClientStart : State.ServerAwaitClientHello;
		}

		/// <summary>Client: sends the first ClientHello. Server: no-op, it waits for one.</summary>
		public void Start()
		{
			if (!_isClient) return;
			if (_state != State.ClientStart) throw new InvalidOperationException("Already started.");

			// padded so the very first datagram is a full-size MTU probe, RakNet-style; the body is
			// then immutable (it may end up in the transcript), so ladder steps re-FRAGMENT it and the
			// first fragment's datagram stays the probe for the new rung
			BeginFlight();
			Span<byte> body = stackalloc byte[CurrentMtu];
			int n = HandshakeMessages.WriteClientHello(body, _localRandom, ReadOnlySpan<byte>.Empty, MaxFragmentEpoch0);
			_bufferedFirstClientHelloSeq = SendHandshakeMessage(HandshakeType.ClientHello, body.Slice(0, n), appendTranscript: false);
			_bufferedFirstClientHello = body.Slice(0, n).ToArray();
			EndFlight();
			_state = State.ClientAwaitServerFirst;
		}

		/// <summary>Re-sends the current flight, rebuilt at the current rung with fresh record sequence numbers.</summary>
		public void Retransmit()
		{
			TransmitFlight();
		}

		/// <summary>
		///     The host's retransmission timer entry (300ms cadence): after <see cref="TimeoutsPerRung" />
		///     tries at a rung whose flight held a datagram too big for a lower rung, steps the ladder
		///     down and re-fragments, RakNet-style. Small-flight loss just retransmits at the same size.
		/// </summary>
		public void OnTimeout()
		{
			if (_state == State.Failed) return;
			if (_state != State.Complete && ++_timeoutsAtRung >= TimeoutsPerRung)
			{
				_timeoutsAtRung = 0;
				// the biggest rung that would actually shrink the flight's biggest datagram; none
				// means the loss was not size-related (or the floor is reached), so same size again
				for (int i = _ladderIndex + 1; i < _ladder.Length; i++)
				{
					if (_ladder[i] < _lastFlightMaxDatagram)
					{
						_ladderIndex = i;
						break;
					}
				}
			}
			TransmitFlight();
		}

		public void HandleDatagram(ReadOnlySpan<byte> datagram)
		{
			if (_state == State.Failed) return;

			while (!datagram.IsEmpty)
			{
				if (!DtlsRecords.TryReadHeader(datagram, out ContentType type, out ushort epoch, out ulong seq48, out int payloadLength))
				{
					return; // truncated datagram tail: drop, DTLS records are droppable
				}
				ReadOnlySpan<byte> payload = datagram.Slice(DtlsRecords.HeaderLength, payloadLength);
				datagram = datagram.Slice(DtlsRecords.HeaderLength + payloadLength);

				if (epoch == 0)
				{
					DispatchPlaintext(type, payload);
				}
				else
				{
					if (_readCipher == null || !_peerChangedCipherSpec) continue; // epoch 1 before keys/CCS: drop
					byte[] plaintext = new byte[payloadLength];
					int n = _readCipher.Decrypt(epoch, seq48, type, payload, plaintext);
					if (n < 0) continue; // bad tag: drop the record, never the association
					DispatchPlaintext(type, plaintext.AsSpan(0, n));
				}
			}
		}

		private void DispatchPlaintext(ContentType type, ReadOnlySpan<byte> payload)
		{
			switch (type)
			{
				case ContentType.Handshake:
					ParseHandshakeFragments(payload);
					break;
				case ContentType.ChangeCipherSpec:
					OnChangeCipherSpec(payload);
					break;
				case ContentType.Alert:
					OnAlert(payload);
					break;
				case ContentType.ApplicationData:
					break; // post-handshake traffic is the production record layer's, not ours
				default:
					Abort(AlertDecodeError, $"Unknown record content type {(byte) type}.");
					break;
			}
		}

		private void ParseHandshakeFragments(ReadOnlySpan<byte> payload)
		{
			while (!payload.IsEmpty)
			{
				if (payload.Length < 12)
				{
					Abort(AlertDecodeError, "Truncated handshake header.");
					return;
				}
				var type = (HandshakeType) payload[0];
				int totalLength = HandshakeMessages.ReadUInt24(payload.Slice(1));
				ushort messageSeq = (ushort) ((payload[4] << 8) | payload[5]);
				int fragmentOffset = HandshakeMessages.ReadUInt24(payload.Slice(6));
				int fragmentLength = HandshakeMessages.ReadUInt24(payload.Slice(9));
				if (payload.Length < 12 + fragmentLength || fragmentOffset + fragmentLength > totalLength)
				{
					Abort(AlertDecodeError, "Malformed handshake fragment.");
					return;
				}
				OnHandshakeFragment(type, totalLength, messageSeq, fragmentOffset, payload.Slice(12, fragmentLength));
				payload = payload.Slice(12 + fragmentLength);
			}
		}

		private void OnHandshakeFragment(HandshakeType type, int totalLength, ushort messageSeq, int fragmentOffset, ReadOnlySpan<byte> fragment)
		{
			if (_state == State.Complete || _state == State.Failed) return;
			if (messageSeq != _nextReceiveSeq) return; // old = retransmit duplicate; future = peer will retransmit after our flight repeats

			if (_reassemblyBody == null)
			{
				if (totalLength > 1 << 20)
				{
					Abort(AlertDecodeError, "Oversized handshake message.");
					return;
				}
				_reassemblyBody = new byte[totalLength];
				_reassemblyMask = new byte[totalLength];
				_reassemblyReceived = 0;
				_reassemblyType = type;
			}
			else if (_reassemblyBody.Length != totalLength || _reassemblyType != type)
			{
				Abort(AlertDecodeError, "Inconsistent handshake fragments.");
				return;
			}

			fragment.CopyTo(_reassemblyBody.AsSpan(fragmentOffset));
			for (int i = 0; i < fragment.Length; i++)
			{
				if (_reassemblyMask[fragmentOffset + i] == 0)
				{
					_reassemblyMask[fragmentOffset + i] = 1;
					_reassemblyReceived++;
				}
			}

			if (_reassemblyReceived < totalLength) return;

			byte[] body = _reassemblyBody;
			_reassemblyBody = null;
			_reassemblyMask = null;
			_nextReceiveSeq++;
			ProcessMessage(type, messageSeq, body);
		}

		private void ProcessMessage(HandshakeType type, ushort messageSeq, byte[] body)
		{
			switch (type)
			{
				case HandshakeType.HelloVerifyRequest when _state == State.ClientAwaitServerFirst:
					OnHelloVerifyRequest(body);
					break;
				case HandshakeType.ServerHello when _state == State.ClientAwaitServerFirst:
					OnServerHello(messageSeq, body);
					break;
				case HandshakeType.Certificate when _state == State.ClientAwaitCertificate:
					OnCertificate(messageSeq, body);
					_state = State.ClientAwaitServerKeyExchange;
					break;
				case HandshakeType.ServerKeyExchange when _state == State.ClientAwaitServerKeyExchange:
					OnServerKeyExchange(messageSeq, body);
					break;
				case HandshakeType.CertificateRequest when _state == State.ClientAwaitCertificateRequest:
					AppendTranscript(type, messageSeq, body);
					_state = State.ClientAwaitServerHelloDone;
					break;
				case HandshakeType.ServerHelloDone when _state == State.ClientAwaitServerHelloDone:
					AppendTranscript(type, messageSeq, body);
					SendClientSecondFlight();
					break;
				case HandshakeType.Finished when _state == State.ClientAwaitFinished:
					OnFinished(messageSeq, body, peerIsClient: false);
					_state = State.Complete;
					break;

				case HandshakeType.ClientHello when _state == State.ServerAwaitClientHello:
					OnClientHello(messageSeq, body);
					break;
				case HandshakeType.Certificate when _state == State.ServerAwaitCertificate:
					OnCertificate(messageSeq, body);
					_state = State.ServerAwaitClientKeyExchange;
					break;
				case HandshakeType.ClientKeyExchange when _state == State.ServerAwaitClientKeyExchange:
					OnClientKeyExchange(messageSeq, body);
					break;
				case HandshakeType.CertificateVerify when _state == State.ServerAwaitCertificateVerify:
					OnCertificateVerify(messageSeq, body);
					break;
				case HandshakeType.Finished when _state == State.ServerAwaitFinished:
					OnFinished(messageSeq, body, peerIsClient: true);
					SendServerFinalFlight();
					break;

				default:
					Abort(AlertUnexpectedMessage, $"Unexpected {type} in state {_state}.");
					break;
			}
		}

		// ---- client side ----

		private void OnHelloVerifyRequest(ReadOnlySpan<byte> body)
		{
			if (!HandshakeMessages.TryParseHelloVerifyRequest(body, out ReadOnlySpan<byte> cookie))
			{
				Abort(AlertDecodeError, "Malformed HelloVerifyRequest.");
				return;
			}
			// RFC 6347 4.2.6: the initial ClientHello and this HelloVerifyRequest stay out of the transcript
			_bufferedFirstClientHello = null;

			BeginFlight();
			Span<byte> hello = stackalloc byte[CurrentMtu];
			// same random per RFC 6347 4.2.2; padded to keep probing the current rung
			int n = HandshakeMessages.WriteClientHello(hello, _localRandom, cookie, MaxFragmentEpoch0);
			SendHandshakeMessage(HandshakeType.ClientHello, hello.Slice(0, n));
			EndFlight();
		}

		private void OnServerHello(ushort messageSeq, ReadOnlySpan<byte> body)
		{
			if (_bufferedFirstClientHello != null)
			{
				// no cookie exchange happened, so the first ClientHello IS in the transcript
				AppendTranscript(HandshakeType.ClientHello, _bufferedFirstClientHelloSeq, _bufferedFirstClientHello);
				_bufferedFirstClientHello = null;
			}
			AppendTranscript(HandshakeType.ServerHello, messageSeq, body);

			if (!HandshakeMessages.TryParseServerHello(body, out ReadOnlySpan<byte> random, out ushort suite, out bool ems))
			{
				Abort(AlertDecodeError, "Malformed ServerHello.");
				return;
			}
			if (suite != HandshakeMessages.CipherSuite)
			{
				Abort(AlertHandshakeFailure, $"Server chose cipher suite 0x{suite:X4}, not 0xC02B.");
				return;
			}
			random.CopyTo(_peerRandom);
			_extendedMasterSecret = ems;
			_state = State.ClientAwaitCertificate;
		}

		private void OnServerKeyExchange(ushort messageSeq, ReadOnlySpan<byte> body)
		{
			AppendTranscript(HandshakeType.ServerKeyExchange, messageSeq, body);
			if (!HandshakeMessages.TryParseServerKeyExchange(body, out ReadOnlySpan<byte> signedParams, out ReadOnlySpan<byte> point, out ReadOnlySpan<byte> signature))
			{
				Abort(AlertDecodeError, "Malformed ServerKeyExchange.");
				return;
			}

			// signature covers client_random || server_random || params (RFC 4492 5.4)
			Span<byte> signed = stackalloc byte[64 + signedParams.Length];
			ClientRandom.CopyTo(signed);
			ServerRandom.CopyTo(signed.Slice(32));
			signedParams.CopyTo(signed.Slice(64));
			Span<byte> hash = stackalloc byte[32];
			SHA256.HashData(signed, hash);
			if (!_peerPublicKey.VerifyHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence))
			{
				Abort(AlertDecryptError, "ServerKeyExchange signature verification failed.");
				return;
			}

			_peerPoint = point.ToArray();
			_state = State.ClientAwaitCertificateRequest;
		}

		private void SendClientSecondFlight()
		{
			BeginFlight();

			Span<byte> buffer = stackalloc byte[1024];
			int n = HandshakeMessages.WriteCertificate(buffer, _certificate.Der);
			SendHandshakeMessage(HandshakeType.Certificate, buffer.Slice(0, n));

			byte[] localPoint = ExportPublicPoint(_ecdhe);
			n = HandshakeMessages.WriteClientKeyExchange(buffer, localPoint);
			SendHandshakeMessage(HandshakeType.ClientKeyExchange, buffer.Slice(0, n));

			DeriveMasterAndKeys();

			// CertificateVerify signs the transcript through ClientKeyExchange (RFC 5246 7.4.8)
			byte[] signature = _certificate.PrivateKey.SignHash(_transcript.Snapshot(), DSASignatureFormat.Rfc3279DerSequence);
			n = HandshakeMessages.WriteCertificateVerify(buffer, signature);
			SendHandshakeMessage(HandshakeType.CertificateVerify, buffer.Slice(0, n));

			SendChangeCipherSpecAndFinished(client: true);
			EndFlight();
			_state = State.ClientAwaitChangeCipherSpec;
		}

		// ---- server side ----

		private void OnClientHello(ushort messageSeq, ReadOnlySpan<byte> body)
		{
			if (!HandshakeMessages.TryParseClientHello(body, out HandshakeMessages.ParsedClientHello hello))
			{
				Abort(AlertDecodeError, "Malformed ClientHello.");
				return;
			}
			if (!hello.OffersCipherSuite)
			{
				Abort(AlertHandshakeFailure, "Client does not offer TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256.");
				return;
			}

			Span<byte> expectedCookie = stackalloc byte[20];
			ComputeCookie(hello.Random, expectedCookie);
			if (!CryptographicOperations.FixedTimeEquals(hello.Cookie, expectedCookie))
			{
				// no or stale cookie: demand one; neither this hello nor the HVR enters the transcript
				BeginFlight();
				Span<byte> hvr = stackalloc byte[32];
				int n = HandshakeMessages.WriteHelloVerifyRequest(hvr, expectedCookie);
				SendHandshakeMessage(HandshakeType.HelloVerifyRequest, hvr.Slice(0, n), appendTranscript: false);
				EndFlight();
				return; // state stays ServerAwaitClientHello
			}

			hello.Random.CopyTo(_peerRandom);
			_extendedMasterSecret = hello.OffersExtendedMasterSecret;
			AppendTranscript(HandshakeType.ClientHello, messageSeq, body);

			BeginFlight();
			Span<byte> buffer = stackalloc byte[1024];
			int written = HandshakeMessages.WriteServerHello(buffer, _localRandom, _extendedMasterSecret, hello.SrtpProfile);
			SendHandshakeMessage(HandshakeType.ServerHello, buffer.Slice(0, written));

			written = HandshakeMessages.WriteCertificate(buffer, _certificate.Der);
			SendHandshakeMessage(HandshakeType.Certificate, buffer.Slice(0, written));

			byte[] localPoint = ExportPublicPoint(_ecdhe);
			Span<byte> signedParams = stackalloc byte[4 + localPoint.Length];
			HandshakeMessages.WriteEcdheParams(signedParams, localPoint);
			Span<byte> signed = stackalloc byte[64 + signedParams.Length];
			ClientRandom.CopyTo(signed);
			ServerRandom.CopyTo(signed.Slice(32));
			signedParams.CopyTo(signed.Slice(64));
			Span<byte> hash = stackalloc byte[32];
			SHA256.HashData(signed, hash);
			byte[] signature = _certificate.PrivateKey.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);
			written = HandshakeMessages.WriteServerKeyExchange(buffer, localPoint, signature);
			SendHandshakeMessage(HandshakeType.ServerKeyExchange, buffer.Slice(0, written));

			written = HandshakeMessages.WriteCertificateRequest(buffer);
			SendHandshakeMessage(HandshakeType.CertificateRequest, buffer.Slice(0, written));

			SendHandshakeMessage(HandshakeType.ServerHelloDone, ReadOnlySpan<byte>.Empty);
			EndFlight();
			_state = State.ServerAwaitCertificate;
		}

		private void OnClientKeyExchange(ushort messageSeq, ReadOnlySpan<byte> body)
		{
			AppendTranscript(HandshakeType.ClientKeyExchange, messageSeq, body);
			if (!HandshakeMessages.TryParseClientKeyExchange(body, out ReadOnlySpan<byte> point))
			{
				Abort(AlertDecodeError, "Malformed ClientKeyExchange.");
				return;
			}
			_peerPoint = point.ToArray();
			DeriveMasterAndKeys();
			_state = State.ServerAwaitCertificateVerify;
		}

		private void OnCertificateVerify(ushort messageSeq, ReadOnlySpan<byte> body)
		{
			// the signature covers the transcript BEFORE this message
			byte[] transcriptHash = _transcript.Snapshot();
			if (!HandshakeMessages.TryParseCertificateVerify(body, out ReadOnlySpan<byte> signature))
			{
				Abort(AlertDecodeError, "Malformed CertificateVerify.");
				return;
			}
			if (!_peerPublicKey.VerifyHash(transcriptHash, signature, DSASignatureFormat.Rfc3279DerSequence))
			{
				Abort(AlertDecryptError, "CertificateVerify signature verification failed.");
				return;
			}
			AppendTranscript(HandshakeType.CertificateVerify, messageSeq, body);
			_state = State.ServerAwaitChangeCipherSpec;
		}

		private void SendServerFinalFlight()
		{
			BeginFlight();
			SendChangeCipherSpecAndFinished(client: false);
			EndFlight();
			_state = State.Complete;
		}

		// ---- shared ----

		private void OnCertificate(ushort messageSeq, ReadOnlySpan<byte> body)
		{
			AppendTranscript(HandshakeType.Certificate, messageSeq, body);
			if (!HandshakeMessages.TryParseCertificate(body, out ReadOnlySpan<byte> leafDer) || leafDer.IsEmpty)
			{
				Abort(AlertBadCertificate, "Malformed or empty Certificate message.");
				return;
			}

			PeerCertificateDer = leafDer.ToArray();
			PeerFingerprint = SHA256.HashData(PeerCertificateDer);
			if (_expectedPeerFingerprint != null && !CryptographicOperations.FixedTimeEquals(PeerFingerprint, _expectedPeerFingerprint))
			{
				Abort(AlertBadCertificate, "Peer certificate fingerprint does not match the expected value.");
				return;
			}

			try
			{
				_peerPublicKey = DtlsCertificate.ExtractPublicKey(PeerCertificateDer);
			}
			catch (Exception e)
			{
				Abort(AlertBadCertificate, $"Peer certificate rejected: {e.Message}");
			}
		}

		private void OnChangeCipherSpec(ReadOnlySpan<byte> payload)
		{
			if (payload.Length != 1 || payload[0] != 1)
			{
				Abort(AlertDecodeError, "Malformed ChangeCipherSpec.");
				return;
			}
			if (_state == State.ClientAwaitChangeCipherSpec)
			{
				_peerChangedCipherSpec = true;
				_state = State.ClientAwaitFinished;
			}
			else if (_state == State.ServerAwaitChangeCipherSpec)
			{
				_peerChangedCipherSpec = true;
				_state = State.ServerAwaitFinished;
			}
			// duplicate CCS from a retransmitted flight: ignore
		}

		private void OnFinished(ushort messageSeq, ReadOnlySpan<byte> body, bool peerIsClient)
		{
			if (body.Length != 12)
			{
				Abort(AlertDecodeError, "Malformed Finished.");
				return;
			}
			Span<byte> expected = stackalloc byte[12];
			Prf.VerifyData(_master, peerIsClient, _transcript.Snapshot(), expected);
			if (!CryptographicOperations.FixedTimeEquals(body, expected))
			{
				Abort(AlertDecryptError, "Finished verify_data mismatch.");
				return;
			}
			AppendTranscript(HandshakeType.Finished, messageSeq, body);
		}

		private void SendChangeCipherSpecAndFinished(bool client)
		{
			_flight.Add(new FlightEntry(true, default, null, 0, false));
			Span<byte> verifyData = stackalloc byte[12];
			Prf.VerifyData(_master, client, _transcript.Snapshot(), verifyData);
			SendHandshakeMessage(HandshakeType.Finished, verifyData, epoch1: true);
		}

		private void DeriveMasterAndKeys()
		{
			byte[] preMaster = DeriveSharedSecret(_peerPoint);
			_master = _extendedMasterSecret
				? Prf.ExtendedMasterSecret(preMaster, _transcript.Snapshot()) // session_hash: transcript through ClientKeyExchange
				: Prf.ClassicMasterSecret(preMaster, ClientRandom, ServerRandom);
			CryptographicOperations.ZeroMemory(preMaster);

			Keys = new DtlsNegotiatedKeys();
			Prf.KeyBlock(_master, ClientRandom, ServerRandom, Keys.ClientWriteKey, Keys.ServerWriteKey, Keys.ClientWriteSalt, Keys.ServerWriteSalt);
			_writeCipher = _isClient
				? new RecordCipher(Keys.ClientWriteKey, Keys.ClientWriteSalt)
				: new RecordCipher(Keys.ServerWriteKey, Keys.ServerWriteSalt);
			_readCipher = _isClient
				? new RecordCipher(Keys.ServerWriteKey, Keys.ServerWriteSalt)
				: new RecordCipher(Keys.ClientWriteKey, Keys.ClientWriteSalt);
		}

		private ReadOnlySpan<byte> ClientRandom => _isClient ? _localRandom : _peerRandom;
		private ReadOnlySpan<byte> ServerRandom => _isClient ? _peerRandom : _localRandom;

		private void ComputeCookie(ReadOnlySpan<byte> clientRandom, Span<byte> cookie20)
		{
			Span<byte> mac = stackalloc byte[32];
			HMACSHA256.HashData(_cookieSecret, clientRandom, mac);
			mac.Slice(0, 20).CopyTo(cookie20);
		}

		private static byte[] ExportPublicPoint(ECDiffieHellman ecdhe)
		{
			ECParameters p = ecdhe.ExportParameters(false);
			byte[] point = new byte[65];
			point[0] = 0x04;
			p.Q.X.CopyTo(point.AsSpan(1 + (32 - p.Q.X.Length)));
			p.Q.Y.CopyTo(point.AsSpan(33 + (32 - p.Q.Y.Length)));
			return point;
		}

		private byte[] DeriveSharedSecret(byte[] peerPoint)
		{
			if (peerPoint == null || peerPoint.Length != 65 || peerPoint[0] != 0x04)
			{
				Abort(AlertDecodeError, "Peer ECDHE public point is not a 65-byte uncompressed P-256 point.");
			}
			var parameters = new ECParameters
			{
				Curve = ECCurve.NamedCurves.nistP256,
				Q = new ECPoint { X = peerPoint.AsSpan(1, 32).ToArray(), Y = peerPoint.AsSpan(33, 32).ToArray() },
			};
			using ECDiffieHellman peer = ECDiffieHellman.Create(parameters); // throws on an off-curve point
			return _ecdhe.DeriveRawSecretAgreement(peer.PublicKey);
		}

		// ---- flight assembly ----

		private void BeginFlight()
		{
			_flight.Clear();
		}

		private void EndFlight()
		{
			TransmitFlight();
		}

		private ushort SendHandshakeMessage(HandshakeType type, ReadOnlySpan<byte> body, bool appendTranscript = true, bool epoch1 = false)
		{
			ushort messageSeq = _nextSendSeq++;
			if (appendTranscript) AppendTranscript(type, messageSeq, body);
			_flight.Add(new FlightEntry(false, type, body.ToArray(), messageSeq, epoch1));
			return messageSeq;
		}

		private void TransmitFlight()
		{
			_datagramUsed = 0;
			_lastFlightMaxDatagram = 0;
			Span<byte> fragment = stackalloc byte[12 + MaxFragmentEpoch0];

			foreach (FlightEntry entry in _flight)
			{
				if (entry.IsChangeCipherSpec)
				{
					QueueRecord(ContentType.ChangeCipherSpec, new byte[] { 1 }, epoch1: false);
					continue;
				}

				int maxFragment = entry.Epoch1 ? MaxFragmentEpoch1 : MaxFragmentEpoch0;
				int offset = 0;
				do
				{
					int fragmentLength = Math.Min(maxFragment, entry.Body.Length - offset);
					WriteHandshakeHeader(fragment, entry.Type, entry.Body.Length, entry.MessageSeq, offset, fragmentLength);
					entry.Body.AsSpan(offset, fragmentLength).CopyTo(fragment.Slice(12));
					QueueRecord(ContentType.Handshake, fragment.Slice(0, 12 + fragmentLength), entry.Epoch1);
					offset += fragmentLength;
				} while (offset < entry.Body.Length);
			}

			FlushDatagram();
		}

		private void QueueRecord(ContentType type, ReadOnlySpan<byte> plaintext, bool epoch1)
		{
			int wireLength = epoch1 ? plaintext.Length + 8 + 16 : plaintext.Length;
			if (_datagramUsed > 0 && _datagramUsed + DtlsRecords.HeaderLength + wireLength > CurrentMtu) FlushDatagram();

			Span<byte> b = _datagramBuffer.AsSpan(_datagramUsed);
			if (!epoch1)
			{
				DtlsRecords.WriteHeader(b, type, 0, _sendSeqEpoch0++, wireLength);
				plaintext.CopyTo(b.Slice(DtlsRecords.HeaderLength));
			}
			else
			{
				ulong seq = _sendSeqEpoch1++;
				DtlsRecords.WriteHeader(b, type, 1, seq, wireLength);
				_writeCipher.Encrypt(1, seq, type, plaintext, b.Slice(DtlsRecords.HeaderLength));
			}
			_datagramUsed += DtlsRecords.HeaderLength + wireLength;
		}

		private void FlushDatagram()
		{
			if (_datagramUsed == 0) return;
			byte[] datagram = _datagramBuffer.AsSpan(0, _datagramUsed).ToArray();
			_datagramUsed = 0;
			if (datagram.Length > _lastFlightMaxDatagram) _lastFlightMaxDatagram = datagram.Length;
			_transmit(datagram);
		}

		private void AppendTranscript(HandshakeType type, ushort messageSeq, ReadOnlySpan<byte> body)
		{
			// RFC 6347 4.2.6: the transcript sees each message in single-fragment form
			Span<byte> header = stackalloc byte[12];
			WriteHandshakeHeader(header, type, body.Length, messageSeq, 0, body.Length);
			_transcript.Append(header);
			_transcript.Append(body);
		}

		private static void WriteHandshakeHeader(Span<byte> b, HandshakeType type, int totalLength, ushort messageSeq, int fragmentOffset, int fragmentLength)
		{
			b[0] = (byte) type;
			HandshakeMessages.WriteUInt24(b.Slice(1), totalLength);
			b[4] = (byte) (messageSeq >> 8);
			b[5] = (byte) messageSeq;
			HandshakeMessages.WriteUInt24(b.Slice(6), fragmentOffset);
			HandshakeMessages.WriteUInt24(b.Slice(9), fragmentLength);
		}

		private void OnAlert(ReadOnlySpan<byte> payload)
		{
			if (payload.Length < 2) return;
			if (payload[0] == 2) // fatal
			{
				_state = State.Failed;
				throw new DtlsHandshakeException($"Peer sent fatal alert {payload[1]}.");
			}
		}

		private void Abort(byte description, string message)
		{
			_state = State.Failed;
			byte[] alert = new byte[DtlsRecords.HeaderLength + 2];
			DtlsRecords.WriteHeader(alert, ContentType.Alert, 0, _sendSeqEpoch0++, 2);
			alert[DtlsRecords.HeaderLength] = 2; // fatal
			alert[DtlsRecords.HeaderLength + 1] = description;
			try
			{
				_transmit(alert);
			}
			catch
			{
				// the transport may already be gone; the exception below is the real signal
			}
			throw new DtlsHandshakeException(message);
		}

		public void Dispose()
		{
			_transcript.Dispose();
			_ecdhe.Dispose();
			_peerPublicKey?.Dispose();
			_writeCipher?.Dispose();
			_readCipher?.Dispose();
			if (_master != null) CryptographicOperations.ZeroMemory(_master);
		}
	}
}