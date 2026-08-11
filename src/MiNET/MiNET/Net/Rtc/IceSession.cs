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
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using log4net;

namespace MiNET.Net.Rtc
{
	public enum IceRole
	{
		ControlledLite,
		Controlling
	}

	/// <summary>
	///     Raised by <see cref="IceSession.OnDtls" /> for every datagram that demuxed as DTLS on
	///     the nominated pair. Task 5's DTLS record layer is the consumer.
	/// </summary>
	public delegate void DtlsDatagramHandler(ReadOnlySpan<byte> datagram, IPEndPoint from);

	/// <summary>
	///     RFC 8445 ICE, trimmed to the two roles this stack needs: a <see cref="IceRole.ControlledLite" />
	///     responder (our server, never sends checks) and an <see cref="IceRole.Controlling" /> dialer
	///     (our client, aggressive nomination on every check). Both roles verify MESSAGE-INTEGRITY
	///     before acting on anything and ignore, rather than throw on, a bad key or malformed
	///     attribute; hostile or merely mistimed input degrades to a dropped datagram.
	///     Every timed behaviour (retransmits, consent keepalives, the two failure timeouts) rides
	///     <see cref="UdpMux.OnTick" />; there are no timers or background threads of its own.
	/// </summary>
	public class IceSession : IMuxPeer, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(IceSession));

		private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		private const int UfragLength = 4;
		private const int PasswordLength = 24;

		// Host candidate priority, RFC 8445 formula: (type preference 126 << 24) | (local preference 65535 << 8) | (256 - component id).
		private const uint HostPriority = (126u << 24) | (65535u << 8) | 255u;

		private const long InitialRtoMillis = 50;
		private const long MaxRtoMillis = 3000;
		private const long GiveUpAfterMillis = 15000;
		private const long ConsentIntervalMillis = 2500;
		private const long ConsentTimeoutMillis = 30000;

		private readonly UdpMux _mux;
		private readonly IceRole _role;
		private readonly string _localUfrag;
		private readonly byte[] _localPasswordBytes;
		private readonly ulong _iceTiebreaker;
		private readonly object _gate = new();
		private readonly List<CandidateState> _candidates = new();

		// Every endpoint this session has ever asked the mux to route to it: candidates registered
		// via AddRemoteCandidate (Controlling), plus, for either role, the sender of any inbound
		// binding request the mux routed here (first contact auto-registers the ControlledLite
		// responder's own endpoints; a Controlling session's checked candidates duplicate what
		// _candidates already holds, but a HashSet makes that harmless). Dispose walks this to give
		// every one of them back to the mux via RemovePeer, so a torn-down session leaves no
		// _peers/_sendAddresses entries behind.
		private readonly HashSet<IPEndPoint> _knownEndpoints = new();

		private int _closedFlag;

		private string _remoteUfrag;
		private byte[] _remotePasswordBytes;
		private string _checkUsername;

		private bool _checksStarted;
		private long _checksStartedAtTicks;

		private CandidateState _nominatedCandidate;
		private long _lastConsentSentTicks;
		private long _lastSeenTicks;

		private int _nominatedFlag;
		private int _failedFlag;
		private long _integrityFailures;
		private long _consentChecksSent;

		/// <summary>
		///     Test visibility only (via the assembly's existing InternalsVisibleTo to MiNETTests): how
		///     many consent keepalives this session has actually sent on the wire. Used to prove
		///     <see cref="Dispose" /> stops an already-running keepalive cycle rather than merely one
		///     that never got the chance to start.
		/// </summary>
		internal long ConsentChecksSent => Interlocked.Read(ref _consentChecksSent);

		public IPEndPoint RemoteEndPoint { get; private set; }

		public event Action<IPEndPoint> OnNominated;
		public event Action OnFailed;
		public event DtlsDatagramHandler OnDtlsDatagram;

		public IceSession(UdpMux mux, IceRole role, string localUfrag, string localPassword)
		{
			_mux = mux;
			_role = role;
			_localUfrag = localUfrag;
			_localPasswordBytes = Encoding.UTF8.GetBytes(localPassword);
			_iceTiebreaker = RandomTiebreaker();

			_mux.OnTick += OnTick;
		}

		public static string NewUfrag()
		{
			return RandomNumberGenerator.GetString(Alphabet, UfragLength);
		}

		public static string NewPassword()
		{
			return RandomNumberGenerator.GetString(Alphabet, PasswordLength);
		}

		public void SetRemoteCredentials(string remoteUfrag, string remotePassword)
		{
			_remoteUfrag = remoteUfrag;
			_remotePasswordBytes = Encoding.UTF8.GetBytes(remotePassword);
		}

		/// <summary>
		///     Controlling only. Registers the candidate on the mux by endpoint before any check is
		///     sent to it: a binding SUCCESS carries no USERNAME, so it can only ever be routed back
		///     to us by endpoint, never by ufrag. Skipping this means every response from a candidate
		///     we have not yet nominated is dropped as unknown and nomination never happens.
		/// </summary>
		public void AddRemoteCandidate(IPEndPoint candidate)
		{
			if (_role != IceRole.Controlling) throw new InvalidOperationException("AddRemoteCandidate is only valid for the Controlling role.");

			// Register on the mux BEFORE the candidate becomes visible to the tick thread: a
			// binding SUCCESS carries no USERNAME, so it can only route back to us by endpoint.
			// Adding to _candidates first would let the tick thread send the very first check
			// against an endpoint the mux does not know yet, and its fast loopback response
			// would be dropped as unrouted.
			_mux.RegisterPeer(candidate, this);

			lock (_gate)
			{
				_candidates.Add(new CandidateState(candidate));
				_knownEndpoints.Add(candidate);
			}
		}

		public void StartChecks()
		{
			if (_role != IceRole.Controlling) throw new InvalidOperationException("StartChecks is only valid for the Controlling role.");
			if (_remoteUfrag == null || _remotePasswordBytes == null) throw new InvalidOperationException("SetRemoteCredentials must be called before StartChecks.");

			_checkUsername = _remoteUfrag + ":" + _localUfrag;
			_checksStartedAtTicks = Environment.TickCount64;
			_checksStarted = true;
		}

		/// <summary>
		///     Tears the session down: unsubscribes from <see cref="UdpMux.OnTick" /> (stopping every
		///     further retransmit, consent keepalive, and repeated <see cref="Fail" /> call this
		///     session would otherwise keep making forever) and gives every endpoint it ever registered
		///     back to the mux via <see cref="UdpMux.RemovePeer" />, so a disposed session leaves no
		///     live subscription, no outbound traffic, and no <c>_peers</c>/<c>_sendAddresses</c>
		///     entries behind. Idempotent; safe to call more than once.
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _closedFlag, 1, 0) != 0) return;

			_mux.OnTick -= OnTick;

			IPEndPoint[] endpoints;
			lock (_gate)
			{
				endpoints = new IPEndPoint[_knownEndpoints.Count];
				_knownEndpoints.CopyTo(endpoints);
			}

			foreach (IPEndPoint endpoint in endpoints)
			{
				_mux.RemovePeer(endpoint);
			}
		}

		public void OnStun(StunMessage message, ReadOnlySpan<byte> raw, IPEndPoint from)
		{
			switch (message.Type)
			{
				case StunMessageType.BindingRequest:
					HandleBindingRequest(message, raw, from);
					break;

				case StunMessageType.BindingSuccessResponse:
					if (_role == IceRole.Controlling) HandleBindingSuccess(message, raw, from);
					break;
			}
		}

		public void OnDtls(ReadOnlySpan<byte> datagram, IPEndPoint from)
		{
			OnDtlsDatagram?.Invoke(datagram, from);
		}

		/// <summary>
		///     Shared by both roles: a full ICE agent on the other end, or our own Controlling
		///     session answering a triggered check, always answers an inbound request
		///     with valid integrity, keyed with our LOCAL password since we are the recipient. A
		///     USE-CANDIDATE flag on the request nominates the sender's endpoint.
		/// </summary>
		private void HandleBindingRequest(StunMessage message, ReadOnlySpan<byte> raw, IPEndPoint from)
		{
			if (!StunMessage.VerifyIntegrity(raw, _localPasswordBytes))
			{
				Interlocked.Increment(ref _integrityFailures);
				Log.Debug("Ignoring binding request with invalid MESSAGE-INTEGRITY.");
				return;
			}

			lock (_gate)
			{
				_knownEndpoints.Add(from);
			}

			RefreshLastSeen();
			SendBindingSuccess(message.TransactionId, from);

			if (message.UseCandidate) Nominate(from);
		}

		/// <summary>
		///     Controlling only: a binding success validates (integrity keyed with the REMOTE
		///     password, since that request was signed for the responder) only when its transaction
		///     id matches a check we actually have outstanding, either the original nomination check
		///     or the current consent keepalive.
		/// </summary>
		private void HandleBindingSuccess(StunMessage message, ReadOnlySpan<byte> raw, IPEndPoint from)
		{
			if (!StunMessage.VerifyIntegrity(raw, _remotePasswordBytes))
			{
				Interlocked.Increment(ref _integrityFailures);
				Log.Debug("Ignoring binding success with invalid MESSAGE-INTEGRITY.");
				return;
			}

			CandidateState matched = null;
			lock (_gate)
			{
				if (_nominatedCandidate != null && _nominatedCandidate.EndPoint.Equals(from) && TransactionIdsEqual(_nominatedCandidate.TransactionId, message.TransactionId))
				{
					matched = _nominatedCandidate;
				}
				else
				{
					foreach (CandidateState candidate in _candidates)
					{
						if (candidate.EndPoint.Equals(from) && TransactionIdsEqual(candidate.TransactionId, message.TransactionId))
						{
							matched = candidate;
							break;
						}
					}
				}
			}

			if (matched == null) return;

			RefreshLastSeen();

			if (Volatile.Read(ref _nominatedFlag) == 0)
			{
				lock (_gate)
				{
					_nominatedCandidate = matched;
				}
				Nominate(matched.EndPoint);
			}
		}

		/// <summary>
		///     Subscribed directly to <see cref="UdpMux.OnTick" /> (<see cref="UdpMux" />'s own bare
		///     multicast, no per-subscriber isolation of its own), so a throw here would abort that tick's
		///     whole invocation list and starve every other peer registered on the same mux, not just this
		///     session - the same hazard <see cref="RtcPeer" /> guards its own association tick against at
		///     its own subscription site. Guarded here instead, since this class owns the subscription.
		/// </summary>
		private void OnTick()
		{
			try
			{
				TickCore();
			}
			catch (Exception ex)
			{
				Log.Error("IceSession tick threw; this tick is skipped, the mux keeps serving every other peer.", ex);
			}
		}

		private void TickCore()
		{
			// Closed: Dispose already unsubscribed this handler from mux.OnTick, so reaching this
			// point at all means a tick that was already in flight when Dispose ran; bail rather
			// than touch a session that may be mid-teardown. Failed: without this, a session stuck
			// past the consent timeout would call Fail() again on every single tick forever (it is
			// idempotent, but that is still unbounded wasted work); once failed there is nothing
			// left for a tick to usefully do either way.
			if (Volatile.Read(ref _closedFlag) == 1 || Volatile.Read(ref _failedFlag) == 1) return;

			long now = Environment.TickCount64;

			if (Volatile.Read(ref _nominatedFlag) == 1)
			{
				if (now - Interlocked.Read(ref _lastSeenTicks) >= ConsentTimeoutMillis)
				{
					Fail();
					return;
				}

				// _lastConsentSentTicks and candidate.TransactionId also cross to the receive
				// thread (read under _gate in HandleBindingSuccess), so both the read here and
				// the write inside SendConsentCheck happen under the same lock.
				lock (_gate)
				{
					if (_role == IceRole.Controlling && now - _lastConsentSentTicks >= ConsentIntervalMillis)
					{
						SendConsentCheck(now);
					}
				}

				return;
			}

			if (_role != IceRole.Controlling || !_checksStarted) return;

			if (now - _checksStartedAtTicks >= GiveUpAfterMillis)
			{
				Fail();
				return;
			}

			lock (_gate)
			{
				foreach (CandidateState candidate in _candidates)
				{
					if (now >= candidate.NextSendAtTicks) SendCheck(candidate, now);
				}
			}
		}

		private void SendCheck(CandidateState candidate, long now)
		{
			candidate.TransactionId ??= RandomNumberGenerator.GetBytes(12);

			var message = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = candidate.TransactionId,
				Username = _checkUsername,
				Priority = HostPriority,
				UseCandidate = true,
				IceControlling = _iceTiebreaker
			};

			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer, _remotePasswordBytes, true);
			_mux.Send(candidate.EndPoint, buffer.Slice(0, written));

			candidate.NextSendAtTicks = now + candidate.RtoMillis;
			candidate.RtoMillis = Math.Min(candidate.RtoMillis * 2, MaxRtoMillis);
		}

		private void SendConsentCheck(long now)
		{
			CandidateState candidate = _nominatedCandidate;
			if (candidate == null) return;

			candidate.TransactionId = RandomNumberGenerator.GetBytes(12);

			var message = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = candidate.TransactionId,
				Username = _checkUsername,
				Priority = HostPriority,
				UseCandidate = true,
				IceControlling = _iceTiebreaker
			};

			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer, _remotePasswordBytes, true);
			_mux.Send(candidate.EndPoint, buffer.Slice(0, written));

			_lastConsentSentTicks = now;
			Interlocked.Increment(ref _consentChecksSent);
		}

		private void SendBindingSuccess(byte[] transactionId, IPEndPoint to)
		{
			var message = new StunMessage
			{
				Type = StunMessageType.BindingSuccessResponse,
				TransactionId = transactionId,
				XorMappedAddress = to
			};

			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer, _localPasswordBytes, true);
			_mux.Send(to, buffer.Slice(0, written));
		}

		private void Nominate(IPEndPoint endpoint)
		{
			if (Interlocked.CompareExchange(ref _nominatedFlag, 1, 0) != 0) return;

			RemoteEndPoint = endpoint;
			lock (_gate)
			{
				_lastConsentSentTicks = Environment.TickCount64;
			}
			OnNominated?.Invoke(endpoint);
		}

		private void Fail()
		{
			if (Interlocked.CompareExchange(ref _failedFlag, 1, 0) != 0) return;

			OnFailed?.Invoke();
		}

		private void RefreshLastSeen()
		{
			Interlocked.Exchange(ref _lastSeenTicks, Environment.TickCount64);
		}

		private static bool TransactionIdsEqual(byte[] a, byte[] b)
		{
			return a != null && b != null && a.AsSpan().SequenceEqual(b);
		}

		private static ulong RandomTiebreaker()
		{
			Span<byte> bytes = stackalloc byte[8];
			RandomNumberGenerator.Fill(bytes);
			return BinaryPrimitives.ReadUInt64BigEndian(bytes);
		}

		/// <summary>
		///     Per-candidate retransmit state, walked on every tick. <see cref="TransactionId" /> is
		///     generated once for the first check and reused across retransmits so a late response
		///     to an earlier attempt still matches; it is replaced wholesale for each consent
		///     keepalive after nomination.
		/// </summary>
		private sealed class CandidateState
		{
			public readonly IPEndPoint EndPoint;
			public byte[] TransactionId;
			public long NextSendAtTicks;
			public long RtoMillis;

			public CandidateState(IPEndPoint endPoint)
			{
				EndPoint = endPoint;
				RtoMillis = InitialRtoMillis;
				NextSendAtTicks = Environment.TickCount64;
			}
		}
	}
}