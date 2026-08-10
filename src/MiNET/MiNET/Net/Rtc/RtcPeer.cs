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
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace MiNET.Net.Rtc
{
	/// <summary>
	///     The stage-1 top of the WebRTC stack, and stage 2's host object: owns one ICE session and
	///     lazily one DTLS session, and speaks the offer/answer exchange (a minimal subset of JSEP)
	///     needed to stand them up against a real peer. Two factories fix the role for the session's
	///     whole life, matching NetherNet's own client/server split: <see cref="CreateAnswerer" /> is
	///     ICE-Lite and stays the DTLS server (it answers "passive" per RFC 5763, which obliges the
	///     offering side to take "active"); <see cref="CreateOfferer" /> is the controlling ICE agent
	///     and offers "actpass", so its own DTLS role is not known until the answer names a concrete
	///     side. The <see cref="DtlsSession" /> is therefore created lazily, at
	///     <see cref="AcceptOffer" />/<see cref="AcceptAnswer" /> time, because that is the first
	///     moment both the remote fingerprint and (for the offerer) the resolved DTLS role exist.
	///     The DTLS handshake itself starts automatically once ICE nominates a pair
	///     (<see cref="IceSession.OnNominated" />): datagrams that arrive on the nominated pair before
	///     the handshake task is scheduled are still captured, because <see cref="IceSession.OnDtlsDatagram" />
	///     is wired straight to <see cref="DtlsSession.FeedDatagram" />, which queues unconditionally.
	/// </summary>
	public class RtcPeer : IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(RtcPeer));

		private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(15);

		private readonly UdpMux _mux;
		private readonly RtcCertificate _certificate;
		private readonly IceSession _ice;
		private readonly bool _isAnswerer;
		private readonly string _localUfrag;
		private readonly string _localPassword;
		private readonly TaskCompletionSource<bool> _transportReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly DtlsDatagramHandler _feedDtls;

		private DtlsSession _dtls;
		private int _disposed;
		private volatile bool _transportWasUp;
		private int _transportClosedRaised;
		private int _noRemoteEndPointWarned;

		public string LocalUfrag => _localUfrag;

		/// <summary>
		///     Off by default; exists only for same-machine test topologies where the peer's ICE
		///     candidate gathering advertises a real interface address (its own loopback filtering
		///     excludes 127.0.0.1) that this side cannot reach because its own <see cref="UdpMux" /> is
		///     itself bound to loopback, a combination that only ever arises when both peers of a test
		///     run on one machine. When set, <see cref="AcceptAnswer" /> remaps a same-address-family
		///     remote candidate to this side's own loopback address, keeping its port, and drops a
		///     candidate of a different address family outright (unreachable either way). Never set
		///     this for a real deployment: a production bind address is never a loopback address, so
		///     the remap would never trigger anyway, but leaving it off keeps protocol code free of
		///     test-topology behaviour by default.
		/// </summary>
		public bool RemapCandidatesForSameMachine { get; set; }

		public event DtlsSession.DecryptedHandler OnDecrypted;

		/// <summary>
		///     Raised at most once, when the transport is lost AFTER a handshake that already
		///     succeeded: the 30 s ICE consent-freshness timeout firing <see cref="IceSession.OnFailed" />
		///     is the only source today. It is not raised for a handshake that never came up in the
		///     first place, that failure is already observed through <see cref="WaitForTransportAsync" />
		///     resolving <see langword="false" />. Stage 3 tears down the game session on this event.
		/// </summary>
		public event Action OnTransportClosed;

		private RtcPeer(UdpMux mux, RtcCertificate certificate, IceSession ice, bool isAnswerer, string localUfrag, string localPassword)
		{
			_mux = mux;
			_certificate = certificate;
			_ice = ice;
			_isAnswerer = isAnswerer;
			_localUfrag = localUfrag;
			_localPassword = localPassword;
			_feedDtls = (datagram, from) => _dtls?.FeedDatagram(datagram);

			_ice.OnDtlsDatagram += _feedDtls;
			_ice.OnNominated += OnIceNominated;
			_ice.OnFailed += OnIceFailed;
		}

		/// <summary>
		///     Server role: ICE-Lite (never sends checks, only answers them) and the DTLS server for
		///     the life of the session, matching how a NetherNet host behaves.
		/// </summary>
		public static RtcPeer CreateAnswerer(UdpMux mux, RtcCertificate certificate)
		{
			string ufrag = IceSession.NewUfrag();
			string password = IceSession.NewPassword();
			var ice = new IceSession(mux, IceRole.ControlledLite, ufrag, password);
			var peer = new RtcPeer(mux, certificate, ice, true, ufrag, password);
			mux.RegisterUfrag(ufrag, _ => ice);
			return peer;
		}

		/// <summary>
		///     Client role: the controlling ICE agent, matching how a NetherNet client behaves.
		/// </summary>
		public static RtcPeer CreateOfferer(UdpMux mux, RtcCertificate certificate)
		{
			string ufrag = IceSession.NewUfrag();
			string password = IceSession.NewPassword();
			var ice = new IceSession(mux, IceRole.Controlling, ufrag, password);
			var peer = new RtcPeer(mux, certificate, ice, false, ufrag, password);
			mux.RegisterUfrag(ufrag, _ => ice);
			return peer;
		}

		/// <summary>
		///     Offerer only. Advertises <c>actpass</c> per RFC 5763: the DTLS role stays undecided
		///     until <see cref="AcceptAnswer" /> reads the concrete role the answer names.
		/// </summary>
		public string CreateOffer()
		{
			var description = new RtcSessionDescription
			{
				SessionId = NewSessionId(),
				IceUfrag = _localUfrag,
				IcePassword = _localPassword,
				IceLite = false,
				FingerprintSha256 = _certificate.FingerprintSha256,
				Setup = "actpass",
				Candidates = {_mux.LocalEndPoint}
			};
			return description.ToSdp();
		}

		/// <summary>
		///     Answerer only. Wires the remote credentials, creates the DTLS session as the server
		///     (the answer's <c>passive</c> obliges the offering side to take <c>active</c>, i.e. the
		///     DTLS client), and returns the answer SDP. The answerer is ICE-Lite and never sends
		///     checks, so no remote candidate is added here: it waits for the offering side's checks
		///     to arrive instead.
		/// </summary>
		public string AcceptOffer(string offerSdp)
		{
			RtcSessionDescription remote = RtcSessionDescription.Parse(offerSdp);
			_ice.SetRemoteCredentials(remote.IceUfrag, remote.IcePassword);
			CreateDtlsSession(remote.FingerprintSha256, true);

			var answer = new RtcSessionDescription
			{
				SessionId = NewSessionId(),
				IceUfrag = _localUfrag,
				IcePassword = _localPassword,
				IceLite = true,
				FingerprintSha256 = _certificate.FingerprintSha256,
				Setup = "passive",
				Candidates = {_mux.LocalEndPoint}
			};
			return answer.ToSdp();
		}

		/// <summary>
		///     Offerer only. Wires the remote credentials and candidates, resolves this side's DTLS
		///     role from the answer's concrete <c>setup</c> (their <c>active</c> means they are the
		///     DTLS client, so this session is the server; their <c>passive</c> is the reverse), then
		///     starts ICE checks.
		/// </summary>
		public void AcceptAnswer(string answerSdp)
		{
			RtcSessionDescription remote = RtcSessionDescription.Parse(answerSdp);
			_ice.SetRemoteCredentials(remote.IceUfrag, remote.IcePassword);
			foreach (IPEndPoint candidate in remote.Candidates)
			{
				IPEndPoint reachable = RemapCandidatesForSameMachine ? MakeReachable(candidate) : candidate;
				if (reachable != null) _ice.AddRemoteCandidate(reachable);
			}

			bool isServer = remote.Setup switch
			{
				"active" => true,
				"passive" => false,
				_ => throw new FormatException($"Answer setup must be 'active' or 'passive', was '{remote.Setup}'.")
			};
			CreateDtlsSession(remote.FingerprintSha256, isServer);

			_ice.StartChecks();
		}

		/// <summary>
		///     Only ever called when <see cref="RemapCandidatesForSameMachine" /> is set. A socket
		///     bound to a loopback address can never reach a non-loopback destination: the OS rejects
		///     the send outright, since 127.0.0.1 is not a valid source address for a packet leaving
		///     the loopback interface. Remapping a same-family remote candidate's address to our own
		///     loopback address, keeping its port, reaches the peer anyway: an endpoint listening on a
		///     wildcard bind (the common default) still accepts traffic addressed to 127.0.0.1 on that
		///     same port, exactly as proven by the reverse direction (the answerer role above), where
		///     such a peer dials our loopback address successfully on its own with no remapping needed.
		///     A candidate of a different address family than this side's own bind (e.g. an IPv6
		///     candidate while this mux is bound IPv4) can never be reached either way and is dropped
		///     outright.
		/// </summary>
		private IPEndPoint MakeReachable(IPEndPoint candidate)
		{
			IPAddress localAddress = _mux.LocalEndPoint.Address;
			if (candidate.AddressFamily != localAddress.AddressFamily) return null;

			return IPAddress.IsLoopback(localAddress) ? new IPEndPoint(localAddress, candidate.Port) : candidate;
		}

		/// <summary>
		///     Throws if a DTLS session already exists: a second <see cref="AcceptOffer" />/
		///     <see cref="AcceptAnswer" /> call would otherwise overwrite <see cref="_dtls" /> without
		///     disposing the previous one, leaking a live BouncyCastle session and its pooled buffers.
		///     Renegotiation is out of scope for this stage.
		/// </summary>
		private void CreateDtlsSession(string remoteFingerprint, bool isServer)
		{
			if (_dtls != null) throw new InvalidOperationException("already negotiated");

			var dtls = new DtlsSession(_certificate, remoteFingerprint, isServer, SendToWire);
			dtls.OnDecrypted += payload => OnDecrypted?.Invoke(payload);
			_dtls = dtls;
		}

		/// <summary>
		///     Guards against a null <see cref="IceSession.RemoteEndPoint" />: BouncyCastle can call
		///     back into this before ICE has nominated a pair (e.g. a DTLS retransmit scheduled off a
		///     timer), and <see cref="UdpMux.Send" /> would otherwise pass <see langword="null" /> into
		///     <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}.TryGetValue" />,
		///     which throws. Dropped datagrams are logged once, not per datagram, to avoid flooding the
		///     log on a hot outgoing path.
		/// </summary>
		private void SendToWire(ReadOnlySpan<byte> datagram)
		{
			IPEndPoint remote = _ice.RemoteEndPoint;
			if (remote == null)
			{
				if (Interlocked.Exchange(ref _noRemoteEndPointWarned, 1) == 0)
				{
					Log.Warn("Dropped outgoing datagram: no nominated remote endpoint yet.");
				}

				return;
			}

			_mux.Send(remote, datagram);
		}

		private void OnIceNominated(IPEndPoint endpoint)
		{
			_ = RunHandshakeAsync();
		}

		private async Task RunHandshakeAsync()
		{
			bool succeeded = false;
			try
			{
				using var cts = new CancellationTokenSource(HandshakeTimeout);
				succeeded = await _dtls.DoHandshakeAsync(cts.Token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warn($"DTLS handshake threw ({(_isAnswerer ? "answerer" : "offerer")}).", ex);
			}

			if (succeeded) _transportWasUp = true;
			_transportReady.TrySetResult(succeeded);
		}

		private void OnIceFailed()
		{
			_transportReady.TrySetResult(false);

			if (_transportWasUp && Interlocked.Exchange(ref _transportClosedRaised, 1) == 0)
			{
				OnTransportClosed?.Invoke();
			}
		}

		/// <summary>
		///     Resolves once ICE has nominated a pair and the DTLS handshake that nomination kicked
		///     off has finished, or <paramref name="timeout" /> elapses first, or ICE itself fails
		///     before ever nominating.
		/// </summary>
		public async Task<bool> WaitForTransportAsync(TimeSpan timeout)
		{
			using var cts = new CancellationTokenSource();
			Task delay = Task.Delay(timeout, cts.Token);
			Task completed = await Task.WhenAny(_transportReady.Task, delay).ConfigureAwait(false);
			if (completed != _transportReady.Task) return false;

			cts.Cancel();
			return await _transportReady.Task.ConfigureAwait(false);
		}

		public void SendApplicationData(ReadOnlySpan<byte> payload)
		{
			_dtls?.SendApplicationData(payload);
		}

		private static ulong NewSessionId()
		{
			Span<byte> bytes = stackalloc byte[8];
			RandomNumberGenerator.Fill(bytes);
			return BinaryPrimitives.ReadUInt64BigEndian(bytes);
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

			_ice.OnNominated -= OnIceNominated;
			_ice.OnFailed -= OnIceFailed;
			_ice.OnDtlsDatagram -= _feedDtls;
			_mux.RemoveUfrag(_localUfrag);
			_ice.Dispose();
			_dtls?.Dispose();
		}
	}
}