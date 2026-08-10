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
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class RtcSessionDescriptionTests
	{
		[TestMethod]
		public void RoundTrips()
		{
			var description = new RtcSessionDescription
			{
				SessionId = 12345,
				IceUfrag = "abcd",
				IcePassword = "0123456789abcdef012345",
				IceLite = true,
				FingerprintSha256 = "AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99",
				Setup = "passive",
				Candidates = {new IPEndPoint(IPAddress.Parse("192.168.1.5"), 19132)}
			};

			RtcSessionDescription parsed = RtcSessionDescription.Parse(description.ToSdp());
			Assert.AreEqual("abcd", parsed.IceUfrag);
			Assert.AreEqual(description.IcePassword, parsed.IcePassword);
			Assert.IsTrue(parsed.IceLite);
			Assert.AreEqual(description.FingerprintSha256, parsed.FingerprintSha256);
			Assert.AreEqual("passive", parsed.Setup);
			Assert.AreEqual(5000, parsed.SctpPort);
			Assert.AreEqual(1, parsed.Candidates.Count);
			Assert.AreEqual(19132, parsed.Candidates[0].Port);
		}

		[TestMethod]
		public async Task Parses_ASipSorceryOffer()
		{
			// SIPSorcery is the oracle: whatever RTCPeerConnection.createOffer emits must parse.
			var peer = new SIPSorcery.Net.RTCPeerConnection(new SIPSorcery.Net.RTCConfiguration());
			await peer.createDataChannel("test");
			var offer = peer.createOffer();
			RtcSessionDescription parsed = RtcSessionDescription.Parse(offer.sdp);
			Assert.IsFalse(string.IsNullOrEmpty(parsed.IceUfrag));
			Assert.IsFalse(string.IsNullOrEmpty(parsed.IcePassword));
			Assert.IsFalse(string.IsNullOrEmpty(parsed.FingerprintSha256));
			peer.close();
		}

		[TestMethod]
		public void MissingCredentials_Throw()
		{
			Assert.ThrowsExactly<FormatException>(() => RtcSessionDescription.Parse("v=0\r\ns=-\r\n"));
		}
	}
}