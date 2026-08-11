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
	///     The top of the WebRTC stack, and this session's host object: owns one ICE session and
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

		// RFC 8831 6.2 recommends at least this budget per endpoint; this fixes it here rather than
		// exposing it as a parameter, matching SctpAssociation's own StreamCount constant one file over
		// (no per-caller configuration is needed).
		private const uint SctpArwndBudget = 256 * 1024;

		private readonly UdpMux _mux;
		private readonly RtcCertificate _certificate;
		private readonly IceSession _ice;
		private readonly bool _isAnswerer;
		private readonly string _localUfrag;
		private readonly string _localPassword;
		private readonly TaskCompletionSource<bool> _transportReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly DtlsDatagramHandler _feedDtls;

		private DtlsSession _dtls;
		private bool _dtlsIsClient;
		private int _remoteSctpPort = 5000;
		private SctpAssociation _association;
		private RtcChannelManager _channelManager;
		private Action _associationTick;
		private int _disposed;
		private volatile bool _transportWasUp;
		private int _transportClosedRaised;
		private int _noRemoteEndPointWarned;

		public string LocalUfrag => _localUfrag;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): the underlying association's current state, or <see langword="null" /> before <see cref="BuildSctpAssociation" /> has run. Exists for interop tests that need to prove a peer-initiated teardown actually reached <see cref="SctpState.Aborted" /> on this side, rather than assuming it did from the absence of an exception.</summary>
		internal SctpState? AssociationState => _association?.State;

		/// <summary>Test visibility only: how many inbound packets the underlying association has dropped and counted, for whatever reason - see <see cref="SctpAssociation.IgnoredPacketCount" />.</summary>
		internal long AssociationIgnoredPacketCount => _association?.IgnoredPacketCount ?? 0;

		/// <summary>Test visibility only (assembly's InternalsVisibleTo to MiNETTests): whether the underlying <see cref="DtlsSession" /> has closed, by either a local <see cref="DtlsSession.Dispose" /> or an inbound close_notify/fatal alert - see <see cref="DtlsSession.IsClosed" />. <see langword="false" /> before <see cref="CreateDtlsSession" /> has run. Exists for interop tests proving a peer's close_notify tore this side's DTLS session down per RFC 5246 7.2.1, independently of whatever the SCTP association above it observes (a dropped post-close datagram means it may never reach <see cref="SctpState.Aborted" /> at all).</summary>
		internal bool DtlsSessionClosed => _dtls?.IsClosed ?? false;

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
		///     Forwarded from <see cref="RtcChannelManager.OnDataChannel" /> once the SCTP association
		///     exists (see <see cref="BuildSctpAssociation" />): fires once per accepted inbound
		///     DATA_CHANNEL_OPEN, after the reply ACK has already gone out and the channel is already
		///     <see cref="RtcDataChannel.IsOpen" />. A subscription added before the association exists is
		///     not lost: it is wired to the manager's own event the moment <see cref="BuildSctpAssociation" />
		///     constructs it, exactly like <see cref="OnDecrypted" />'s own forwarding one property up.
		/// </summary>
		public event Action<RtcDataChannel> OnDataChannel;

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
			_remoteSctpPort = remote.SctpPort;
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
			_remoteSctpPort = remote.SctpPort;
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
		///     <para>
		///     Also builds the SCTP association and subscribes
		///     <see cref="_association" />'s <see cref="SctpAssociation.OnPacketReceived" /> to
		///     <see cref="DtlsSession.OnDecrypted" /> right here, synchronously, rather than waiting for
		///     the handshake to succeed first. See <see cref="BuildSctpAssociation" />'s own remarks for
		///     why this ordering closes, by construction, the gap a subscription made only after
		///     observing handshake success would leave open.
		///     </para>
		/// </summary>
		private void CreateDtlsSession(string remoteFingerprint, bool isServer)
		{
			if (_dtls != null) throw new InvalidOperationException("already negotiated");

			_dtlsIsClient = !isServer;

			var dtls = new DtlsSession(_certificate, remoteFingerprint, isServer, SendToWire);
			dtls.OnDecrypted += payload => OnDecrypted?.Invoke(payload);
			_dtls = dtls;

			BuildSctpAssociation();
		}

		/// <summary>
		///     <see cref="DtlsSession._handshakeDone" />
		///     flips to <see langword="true" /> inside <see cref="DtlsSession.DoHandshakeAsync" />, on
		///     whatever thread that method's own <see cref="Task.Run(Action)" /> continuation happens to
		///     resume on - a moment this class does not control the exact scheduling of. Subscribing
		///     <see cref="_association" /> to <see cref="DtlsSession.OnDecrypted" /> only after also
		///     observing that same handshake success would be a logically later point in program order,
		///     but not a HAPPENS-BEFORE guarantee against a datagram already queued on the mux
		///     receive thread: that thread could observe <see cref="DtlsSession._handshakeDone" />
		///     (a volatile field) true and drain-and-decrypt a queued datagram - delivering it only to
		///     <see cref="OnDecrypted" />'s pre-existing forwarding subscriber, never to
		///     <see cref="SctpAssociation.OnPacketReceived" /> - before such a delayed subscription had
		///     even been scheduled, let alone run. A dropped INIT self-heals via
		///     <see cref="SctpAssociation" />'s own T1 retransmit, but it would still be a silent drop on
		///     the receive path.
		///     <para>
		///     Instead: this method builds the association and subscribes here, from <see cref="CreateDtlsSession" />,
		///     called synchronously from <see cref="AcceptOffer" />/<see cref="AcceptAnswer" /> - both
		///     return to their own caller (their own last statement, for the offerer, is
		///     <see cref="IceSession.StartChecks" />, called strictly AFTER this returns) well before
		///     either side's ICE agent can possibly nominate a pair and trigger <see cref="OnIceNominated" />:
		///     the answerer is ICE-Lite and never sends a check of its own, only nominates in response to
		///     one carrying the offerer's credentials (set via <see cref="IceSession.SetRemoteCredentials" />,
		///     itself called before this in both methods, but that alone gives it something to verify, not
		///     anything to nominate on), and the offerer does not even start sending checks until its own
		///     <see cref="AcceptAnswer" /> calls <see cref="IceSession.StartChecks" />, sequenced after
		///     this method returns. So the very first moment <see cref="DoHandshakeAsync" /> could ever be
		///     scheduled - which is the earliest <see cref="DtlsSession._handshakeDone" /> could ever
		///     become true - is strictly after this subscription already exists. This is not a narrower
		///     window: it is provably no window at all, by the offer/answer protocol's own call ordering,
		///     not by a timing assumption. <see cref="RunHandshakeAsync" />'s own eager
		///     <see cref="SctpAssociation.Start" /> call (DTLS-client role) still only ever runs after a
		///     successful handshake, so it alone carries no risk here; <see cref="SctpAssociation.Start" />
		///     also has a second caller with no such guarantee
		///     (<see cref="RtcChannelManager.CreateChannel" />, which can race arbitrarily far ahead of the
		///     handshake), which is exactly the gap <see cref="SendSctpPacket" /> closes:
		///     <see cref="DtlsSession.SendApplicationData" /> still correctly throws before the handshake
		///     completes, so every send this association makes is routed through that one small wrapper
		///     instead of the delegate directly, rather than trusting every future caller of
		///     <see cref="SctpAssociation.Start" /> to independently know and honor this ordering.
		///     </para>
		/// </summary>
		private void BuildSctpAssociation()
		{
			_association = new SctpAssociation(_dtlsIsClient, (ushort) _remoteSctpPort, SctpArwndBudget, SendSctpPacket);
			_channelManager = new RtcChannelManager(_association, _dtlsIsClient);
			_channelManager.OnDataChannel += channel => OnDataChannel?.Invoke(channel);

			_dtls.OnDecrypted += _association.OnPacketReceived;

			// UdpMux.OnTick (UdpMux.cs) is a bare multicast with no per-subscriber isolation, and
			// HighPrecisionTimer's own catch around invoking it swallows silently - one association
			// throwing out of OnTick aborts the whole invocation list for that tick, so every OTHER peer
			// registered on the same mux misses its RTO/delayed-SACK/T3 timers too, with no log output at
			// all. Wrapped here, at the subscription site - the one place that already knows which
			// association a given tick belongs to, for a useful log message - rather than in UdpMux or
			// HighPrecisionTimer themselves. The wrapped delegate is kept so Dispose can unsubscribe the
			// SAME instance (unsubscribing _association.OnTick directly here would not remove it).
			_associationTick = () =>
			{
				try
				{
					_association.OnTick();
				}
				catch (Exception ex)
				{
					Log.Error("SctpAssociation.OnTick threw; this peer's tick is skipped, the mux keeps serving every other peer.", ex);
				}
			};
			_mux.OnTick += _associationTick;
		}

		/// <summary>
		///     <see cref="SctpAssociation" />'s <see cref="PacketSender" />: everything it ever sends,
		///     handshake or data plane, funnels through here to <see cref="DtlsSession.SendApplicationData" />.
		///     <see cref="RtcChannelManager.CreateChannel" />'s own demand-driven
		///     <see cref="SctpAssociation.Start" /> call (see that method's remarks) can run before this
		///     side's DTLS handshake has finished, which <see cref="DtlsSession.SendApplicationData" /> itself
		///     still correctly rejects by throwing. Swallowed and logged here rather than propagated: the
		///     caller is <see cref="SctpAssociation" />'s own internal send path, not application code with
		///     anything useful to do about a mid-handshake send failing, and <see cref="SctpAssociation" />'s
		///     own T1 retransmit (<see cref="SctpAssociation.OnTick" />) already retries a fixed interval
		///     later - by which point, in every scenario this stack's own tests or NetherNet's real topology
		///     produce, the handshake has finished. Still satisfies <see cref="PacketSender" />'s own leaf
		///     contract: this wrapper takes no lock of its own beyond what <see cref="DtlsSession.SendApplicationData" />
		///     already does, and never calls back into <see cref="_association" /> or blocks on anything.
		/// </summary>
		private void SendSctpPacket(ReadOnlySpan<byte> packet)
		{
			try
			{
				_dtls.SendApplicationData(packet);
			}
			catch (InvalidOperationException ex)
			{
				Log.Warn("Dropped an outgoing SCTP packet: DTLS handshake not done yet.", ex);
			}
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

		/// <summary>
		///     <see cref="_association" /> and <see cref="_channelManager" /> already exist by the time
		///     this runs (<see cref="BuildSctpAssociation" />, called synchronously from
		///     <see cref="CreateDtlsSession" /> well before this method's own trigger,
		///     <see cref="OnIceNominated" />, could ever fire - see that method's own remarks). All that is
		///     left here, on a successful handshake, is starting the SCTP handshake itself when this side
		///     is the DTLS client: RFC 8832 does not mandate which side sends the opening INIT, but
		///     NetherNet has the DTLS client do it (the same side RFC 5763 already makes responsible for
		///     starting the DTLS handshake as "active"), so this class follows the same convention.
		/// </summary>
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

			if (succeeded)
			{
				_transportWasUp = true;

				if (_dtlsIsClient)
				{
					try
					{
						_association.Start();
					}
					catch (Exception ex)
					{
						// Must not leave _transportReady uncompleted below: that would hang every
						// WaitForTransportAsync caller until its own timeout, for a bug in the layer
						// above an otherwise genuinely successful DTLS handshake - the same
						// "undiagnosable hang" class of failure DtlsSession.SendApplicationData's own
						// pre-handshake guard exists to avoid, one layer up.
						Log.Error($"SctpAssociation.Start() threw after a successful DTLS handshake ({(_isAnswerer ? "answerer" : "offerer")}).", ex);
					}
				}
			}

			_transportReady.TrySetResult(succeeded);
		}

		/// <summary>
		///     Creates a NetherNet data channel over the SCTP association <see cref="BuildSctpAssociation" />
		///     built. Throws <see cref="InvalidOperationException" /> rather than silently failing when
		///     called before that association even exists (no DTLS session negotiated yet - before
		///     <see cref="AcceptOffer" />/<see cref="AcceptAnswer" />, not before <see cref="WaitForTransportAsync" />
		///     resolves; see <see cref="BuildSctpAssociation" />'s own
		///     remarks): matches <see cref="DtlsSession.SendApplicationData" />'s own pre-handshake guard,
		///     the same "an undiagnosable silent drop is worse than a thrown exception" stance applied one
		///     layer up. Once the association exists, a call that races ahead of it reaching
		///     <see cref="SctpState.Established" /> - potentially well before
		///     <see cref="WaitForTransportAsync" /> even resolves - is not lost:
		///     <see cref="RtcChannelManager.CreateChannel" /> queues the DATA_CHANNEL_OPEN and sends it once
		///     established, and still returns a usable channel object immediately either way.
		/// </summary>
		public RtcDataChannel CreateDataChannel(string label, bool ordered = true, int maxRetransmits = -1)
		{
			if (_channelManager == null) throw new InvalidOperationException("RtcPeer.CreateDataChannel called before the transport is ready.");

			return _channelManager.CreateChannel(label, ordered, maxRetransmits);
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

		/// <summary>
		///     Tears down in dependency order: first unsubscribes <see cref="_association" /> from
		///     <see cref="DtlsSession.OnDecrypted" />, then aborts the association (a best-effort ABORT to
		///     the peer, then <see cref="SctpAssociation.Abort" />'s own <c>Teardown</c> releasing every
		///     outstanding send/receive lease - see that method's remarks for why this is safe to call even
		///     when the association never reached <see cref="SctpState.Established" />, or was never
		///     constructed at all) and unhooks it from <see cref="_mux" />'s tick, then runs the existing
		///     teardown (ICE event unhooks, ufrag removal, ICE and DTLS disposal).
		///     <para>
		///     Without the unsubscribe below,
		///     an in-flight <see cref="DtlsSession.OnDecrypted" /> delivery (already past its own delegate
		///     snapshot when this method runs - the "residue" this remarks paragraph exists to name
		///     honestly, see below) could still reach <see cref="SctpAssociation.OnPacketReceived" /> on an
		///     association this method is concurrently tearing down, and <see cref="SendApplicationData" />
		///     has no guard of its own to stop a resulting SACK/reply send from racing
		///     <see cref="DtlsSession.Dispose" />'s own close of the transport. The unsubscribe below runs
		///     FIRST, before <see cref="SctpAssociation.Abort" /> and before <see cref="_dtls" /> is
		///     disposed, to shrink the window as much as this class can: every delivery that has not yet
		///     had its delegate list read by <see cref="DtlsSession" /> at the instant this line runs will
		///     never reach the association at all.
		///     </para>
		///     <para>
		///     What is honestly still possible, unsubscribing here included: a .NET multicast event's
		///     invocation list is a snapshot taken at the moment <c>?.Invoke</c> is evaluated, so a
		///     delivery already past that point - already inside, or about to enter,
		///     <see cref="SctpAssociation.OnPacketReceived" /> on another thread - completes regardless of
		///     the <c>-=</c> below; this method does not, and cannot cheaply, barrier-wait for it. That
		///     residue is safe, not because it cannot happen: (a)
		///     <see cref="HandleCookieEcho" />/<see cref="HandleInit" />
		///     refuse to transition state or send once <see cref="SctpState.Aborted" />, which
		///     <see cref="SctpAssociation.Abort" /> below sets before this residual delivery could possibly
		///     finish processing a chunk that reached it after the abort; and (c)
		///     <see cref="DtlsSession.SendApplicationData" />'s own disposed guard means even a SACK this
		///     residual delivery tries to send through the now-<see cref="_dtls" />-owned
		///     <c>sendPacket</c> delegate is silently dropped, never touching a transport
		///     <see cref="DtlsSession.Dispose" /> may already be closing.
		///     </para>
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

			if (_association != null)
			{
				if (_dtls != null) _dtls.OnDecrypted -= _association.OnPacketReceived;

				_association.Abort();
				_mux.OnTick -= _associationTick;
			}

			_ice.OnNominated -= OnIceNominated;
			_ice.OnFailed -= OnIceFailed;
			_ice.OnDtlsDatagram -= _feedDtls;
			_mux.RemoveUfrag(_localUfrag);
			_ice.Dispose();
			_dtls?.Dispose();
		}
	}
}