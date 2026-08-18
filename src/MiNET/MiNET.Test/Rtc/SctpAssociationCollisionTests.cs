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

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	/// <summary>
	///     <see cref="SctpAssociation.Start" /> is not locked to the DTLS-client-designated side,
	///     which makes two failure shapes reachable with no adversary involved, neither of which a
	///     real, independently-timed interop peer could exercise (both need precise control over
	///     packet ordering a real peer cannot give a test).
	///     Both are wired, our-vs-our associations driven by hand, one packet at a time, exactly like
	///     <see cref="SctpAssociationHandshakeTests" />'s own pair - no real transport, no timing
	///     dependency, fully deterministic.
	/// </summary>
	[TestClass]
	public class SctpAssociationCollisionTests
	{
		/// <summary>
		///     Delivers every packet either side has queued but not yet delivered to the other, repeating
		///     until neither side has anything left to deliver (a reply can itself queue a further reply,
		///     e.g. INIT-ACK provoking COOKIE-ECHO). Mirrors <see cref="SctpAssociationHandshakeTests" />'s
		///     synchronous, fully-wired pair, but as an explicit pump rather than an automatic callback
		///     chain: these tests need to pause mid-handshake and inject something between two specific
		///     deliveries, which an automatic wire (send this, and let it cascade) cannot do.
		/// </summary>
		private static void Pump(SctpAssociation a, List<byte[]> aOutbox, ref int aDelivered, SctpAssociation b, List<byte[]> bOutbox, ref int bDelivered)
		{
			bool progressed = true;
			while (progressed)
			{
				progressed = false;
				while (aDelivered < aOutbox.Count)
				{
					b.OnPacketReceived(aOutbox[aDelivered++]);
					progressed = true;
				}

				while (bDelivered < bOutbox.Count)
				{
					a.OnPacketReceived(bOutbox[bDelivered++]);
					progressed = true;
				}
			}
		}

		/// <summary>
		///     A initiates, B answers as a stateless responder
		///     (RFC 4960's own design - B commits nothing, <see cref="SctpAssociation.State" /> stays
		///     <see cref="SctpState.Closed" />) and B's INIT-ACK has not even reached A yet, let alone A's
		///     COOKIE-ECHO reaching B. B's own application now wants a channel
		///     (<see cref="RtcChannelManager.CreateChannel" />), which tries
		///     <see cref="SctpAssociation.Start" /> as a demand-driven fallback. Without the
		///     responder-in-flight suppression, nothing would distinguish this from a genuinely
		///     idle association: B would mint a fresh identity and send a competing INIT of its own, and
		///     A's still-perfectly-valid COOKIE-ECHO (answering B's ORIGINAL identity) would then be
		///     dropped by B's own <c>_isClient</c> gate on every retransmit - corrupting a handshake that
		///     had nothing wrong with it. Asserts the responder-in-flight suppression: no second INIT, the
		///     original handshake still completes, and B's queued channel still opens once it does (the
		///     channel manager's own pending-opens queue is what carries the create-channel intent through
		///     the suppressed <see cref="SctpAssociation.Start" /> call to actual completion).
		/// </summary>
		[TestMethod]
		public void ResponderInFlight_LocalDemandMidHandshake_SuppressesCompetingInit_HandshakeConverges_QueuedChannelOpens()
		{
			var aOutbox = new List<byte[]>();
			var bOutbox = new List<byte[]>();
			int aDelivered = 0;
			int bDelivered = 0;

			SctpAssociation a = null;
			SctpAssociation b = null;
			a = new SctpAssociation(isClient: true, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => aOutbox.Add(p.ToArray()));
			b = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => bOutbox.Add(p.ToArray()));

			var aChannels = new RtcChannelManager(a, isClient: true);
			var bChannels = new RtcChannelManager(b, isClient: false);

			RtcDataChannel aSideOfBChannel = null;
			aChannels.OnDataChannel += ch => aSideOfBChannel = ch;

			a.Start();
			Assert.AreEqual(1, aOutbox.Count, "expected exactly the opening INIT");

			// Deliver A's INIT to B: B answers as a stateless responder and commits nothing.
			b.OnPacketReceived(aOutbox[aDelivered++]);
			Assert.AreEqual(1, bOutbox.Count, "expected exactly B's INIT-ACK reply");
			Assert.AreEqual(SctpState.Closed, b.State, "B must still be stateless here - nothing commits until COOKIE-ECHO");

			// B's own application wants a channel now, before B's INIT-ACK (let alone A's COOKIE-ECHO)
			// has gone anywhere. This must not mint a competing INIT.
			RtcDataChannel bChannel = bChannels.CreateChannel("ResponderRace", ordered: true, maxRetransmits: -1);
			Assert.AreEqual(1, bOutbox.Count, "B must not send a second INIT while its own answered INIT-ACK may still be in flight");
			Assert.AreEqual(SctpState.Closed, b.State, "the suppressed Start must not have touched B's state either");

			// The original handshake, and the DCEP round trip local demand queued, must still converge.
			Pump(a, aOutbox, ref aDelivered, b, bOutbox, ref bDelivered);

			Assert.AreEqual(SctpState.Established, a.State);
			Assert.AreEqual(SctpState.Established, b.State);
			Assert.IsTrue(bChannel.IsOpen, "B's channel, queued before establishment, must open once the ORIGINAL (A-initiated) handshake completes");
			Assert.IsNotNull(aSideOfBChannel, "A must have seen B's channel open");
			Assert.AreEqual("ResponderRace", aSideOfBChannel.Label);
		}

		/// <summary>
		///     True RFC 4960 5.2.1 simultaneous-INIT collision: both sides decide to self-initiate before
		///     either has received anything from the other at all (no responder-in-flight hint is even in
		///     play here - see the other test in this file for that race). If each
		///     side's own <c>HandleInit</c> dropped the other's INIT outright (a self-initiated instance
		///     refusing to also answer one), neither side's opening chunk would ever be acknowledged and
		///     both would retry to exhaustion. Instead, <c>HandleInit</c> answers a colliding INIT with the
		///     responder's OWN EXISTING
		///     identity rather than a fresh one and accepts the resulting COOKIE-ECHO despite already being
		///     self-initiated, which is the convergent subset of 5.2.1 this stack implements (RFC
		///     4960 5.2.2-5.2.4's fuller duplicate-association tie-break is out of scope - see
		///     <see cref="SctpAssociation.HandleInit" />'s own remarks). Asserts both sides converge to
		///     <see cref="SctpState.Established" /> with no abort on either side.
		/// </summary>
		[TestMethod]
		public void SymmetricStart_BothSidesInitiateBeforeAnyPacketArrives_ConvergesToEstablished_NoAbort()
		{
			var aOutbox = new List<byte[]>();
			var bOutbox = new List<byte[]>();
			int aDelivered = 0;
			int bDelivered = 0;

			SctpAssociation a = null;
			SctpAssociation b = null;
			a = new SctpAssociation(isClient: true, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => aOutbox.Add(p.ToArray()));
			b = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => bOutbox.Add(p.ToArray()));

			string aAbortReason = null;
			string bAbortReason = null;
			a.OnAborted += reason => aAbortReason = reason;
			b.OnAborted += reason => bAbortReason = reason;

			// Both sides self-initiate before either has delivered anything to the other - the collision.
			a.Start();
			b.Start();
			Assert.AreEqual(1, aOutbox.Count);
			Assert.AreEqual(1, bOutbox.Count);

			Pump(a, aOutbox, ref aDelivered, b, bOutbox, ref bDelivered);

			Assert.AreEqual(SctpState.Established, a.State);
			Assert.AreEqual(SctpState.Established, b.State);
			Assert.IsNull(aAbortReason, "A must not have aborted");
			Assert.IsNull(bAbortReason, "B must not have aborted");
			Assert.AreEqual(0L, a.IgnoredPacketCount, "a clean collision has nothing for either side to drop");
			Assert.AreEqual(0L, b.IgnoredPacketCount, "a clean collision has nothing for either side to drop");
		}

		/// <summary>
		///     Regression: single-sided <see cref="SctpAssociation.Start" /> must still work in this
		///     direction - the DTLS-server-designated (<c>isClient: false</c>) side
		///     self-initiating on local demand while the peer never calls <see cref="SctpAssociation.Start" />
		///     at all, the shape that deadlocks against a peer that never self-initiates eagerly on its
		///     own. <see cref="SctpAssociationHandshakeTests.TwoAssociations_CompleteHandshake_AndAgreeOnVerificationTags" />
		///     already covers the original direction (the designated client self-initiates, the designated
		///     server stays purely passive); this is its mirror.
		/// </summary>
		[TestMethod]
		public void ServerRoleInstance_SelfInitiatesOnLocalDemand_WhenPeerNeverCallsStart_HandshakeCompletesAndChannelOpens()
		{
			var aOutbox = new List<byte[]>();
			var bOutbox = new List<byte[]>();
			int aDelivered = 0;
			int bDelivered = 0;

			SctpAssociation a = null;
			SctpAssociation b = null;
			a = new SctpAssociation(isClient: true, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => aOutbox.Add(p.ToArray())); // never calls Start() itself
			b = new SctpAssociation(isClient: false, sctpPort: 5000, arwndBudget: 131072, sendPacket: p => bOutbox.Add(p.ToArray()));

			var aChannels = new RtcChannelManager(a, isClient: true);
			var bChannels = new RtcChannelManager(b, isClient: false);

			RtcDataChannel bChannel = bChannels.CreateChannel("ServerSelfInitiates", ordered: true, maxRetransmits: -1);
			Assert.AreEqual(1, bOutbox.Count, "B must self-initiate since nobody else ever will");
			Assert.AreEqual(0, aOutbox.Count, "A never calls Start itself in this scenario");

			Pump(a, aOutbox, ref aDelivered, b, bOutbox, ref bDelivered);

			Assert.AreEqual(SctpState.Established, a.State);
			Assert.AreEqual(SctpState.Established, b.State);
			Assert.IsTrue(bChannel.IsOpen);
		}
	}
}
