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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.NetherNet;

namespace MiNET.Test
{
	/// <summary>
	///     Port mapping is the difference between a server anyone can join and one that only works on
	///     the LAN, and every way of getting it wrong fails the same silent way: the client receives
	///     an answer, dials an address that goes nowhere, and times out with nothing logged anywhere.
	///     So the arithmetic is pinned here rather than discovered from outside a router.
	/// </summary>
	[TestClass]
	public class NetherNetPortMappingTests
	{
		private static string Sdp(params int[] ports)
		{
			string sdp = "v=0\r\ns=-\r\n";
			foreach (int port in ports) sdp += $"a=candidate:1 1 udp 2130706431 192.168.1.10 {port} typ host generation 0\r\n";
			return sdp + "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n";
		}

		/// <summary>
		///     The bare form pins where gameplay binds without claiming anything about the outside,
		///     which is what a directly reachable server with a firewall wants.
		/// </summary>
		[TestMethod]
		public void ABareRangeRestrictsBindingAndAdvertisesNothing()
		{
			var mapping = NetherNetPortMapping.Parse("49152-49200");

			Assert.AreEqual(49152, mapping.RangeStart);
			Assert.AreEqual(49200, mapping.RangeEnd);
			Assert.AreEqual(0, mapping.Mappings.Count);

			// Nothing to translate, so the SDP must come back untouched.
			string sdp = Sdp(49160);
			Assert.AreEqual(sdp, mapping.Apply(sdp));
		}

		/// <summary>
		///     Ranges pair by offset, not by position in the string. The nth internal port must map to
		///     the nth external one or players land on each other's ports.
		/// </summary>
		[TestMethod]
		public void RangesPairByOffset()
		{
			var mapping = NetherNetPortMapping.Parse("19132-19232:32000-32100");

			Assert.AreEqual(32000, mapping.RangeStart);
			Assert.AreEqual(32100, mapping.RangeEnd);

			Assert.IsTrue(mapping.Apply(Sdp(32000)).Contains(" 19132 typ srflx"), "first internal port maps to first external");
			Assert.IsTrue(mapping.Apply(Sdp(32050)).Contains(" 19182 typ srflx"), "offset is preserved through the range");
			Assert.IsTrue(mapping.Apply(Sdp(32100)).Contains(" 19232 typ srflx"), "last internal port maps to last external");
		}

		/// <summary>
		///     With a public address given, the candidate has to carry it instead of the internal one,
		///     which is the whole point behind NAT: the internal address is unroutable to the client.
		/// </summary>
		[TestMethod]
		public void APublicAddressReplacesTheInternalOne()
		{
			var mapping = NetherNetPortMapping.Parse("203.0.113.10:19132-19232:32000-32100");

			string result = mapping.Apply(Sdp(32005));

			Assert.IsTrue(result.Contains("203.0.113.10 19137 typ srflx"), $"expected a reflexive candidate with the public address, got: {result}");
			Assert.IsTrue(result.Contains("192.168.1.10 32005 typ host"), "the local candidate must survive so LAN peers can still connect");
		}

		/// <summary>
		///     A port outside every mapping is left alone. Rewriting it would hand the client an
		///     address that resolves to some unrelated forwarded port.
		/// </summary>
		[TestMethod]
		public void PortsOutsideTheMappingAreUntouched()
		{
			var mapping = NetherNetPortMapping.Parse("19132-19232:32000-32100");

			Assert.IsTrue(mapping.Apply(Sdp(40000)).Contains("192.168.1.10 40000 typ host"));
		}

		/// <summary>
		///     Only host candidates describe a local address. A reflexive or relayed candidate already
		///     says how the outside sees us, so translating it again would break a working path.
		/// </summary>
		[TestMethod]
		public void OnlyHostCandidatesAreTranslated()
		{
			var mapping = NetherNetPortMapping.Parse("19132-19232:32000-32100");
			string sdp = "a=candidate:1 1 udp 2130706431 192.168.1.10 32000 typ srflx generation 0\r\nm=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n";

			Assert.IsTrue(mapping.Apply(sdp).Contains("32000 typ srflx"), "a reflexive candidate must not be rewritten");
		}

		/// <summary>
		///     Mismatched range lengths have no correct interpretation, so the entry is refused rather
		///     than guessed at. Accepting it would silently map some players to ports nobody forwarded.
		/// </summary>
		[TestMethod]
		public void MismatchedRangeLengthsAreRejected()
		{
			var mapping = NetherNetPortMapping.Parse("19132-19232:32000-32010");

			Assert.AreEqual(0, mapping.Mappings.Count);
		}

		/// <summary>Several entries on one line, as BDS documents.</summary>
		[TestMethod]
		public void MultipleEntriesAreAccepted()
		{
			var mapping = NetherNetPortMapping.Parse("19132-19140:32000-32008, 19200:32100");

			Assert.AreEqual(2, mapping.Mappings.Count);
			Assert.AreEqual(32000, mapping.RangeStart);
			Assert.AreEqual(32100, mapping.RangeEnd);
			Assert.IsTrue(mapping.Apply(Sdp(32100)).Contains(" 19200 typ srflx"));
		}

		/// <summary>
		///     A dual stack server offers an IPv4 and an IPv6 host candidate on the same port. An
		///     IPv4 mapping must not rewrite the IPv6 one, which would produce a candidate that is
		///     neither address and that no client can dial.
		/// </summary>
		[TestMethod]
		public void AnIPv4MappingLeavesIPv6CandidatesAlone()
		{
			var mapping = NetherNetPortMapping.Parse("78.82.183.209:30000-30099:30000-30099");

			string sdp = "a=candidate:1 1 udp 2130706431 192.168.1.10 30005 typ host generation 0\r\n"
						+ "a=candidate:2 1 udp 2130706431 fdcd:6929:62d2::1 30005 typ host generation 0\r\n"
						+ "m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n";

			string rewritten = mapping.Apply(sdp);

			Assert.IsTrue(rewritten.Contains("78.82.183.209 30005 typ srflx"), "the IPv4 candidate should gain a mapped sibling");
			Assert.IsFalse(rewritten.Contains("78.82.183.209 30005 typ srflx raddr fdcd"), "the IPv6 candidate must not be given an IPv4 address");
		}

		/// <summary>An empty or absent setting must leave the OS to choose, not restrict anything.</summary>
		[TestMethod]
		public void NoConfigurationMeansNoRestriction()
		{
			var mapping = NetherNetPortMapping.Parse("");

			Assert.IsFalse(mapping.IsConfigured);
			Assert.IsNull(mapping.RangeStart);
		}
	}
}
