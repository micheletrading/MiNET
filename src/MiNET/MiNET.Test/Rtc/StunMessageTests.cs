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
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class StunMessageTests
	{
		[TestMethod]
		public void BindingRequest_RoundTrips()
		{
			var message = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = Enumerable.Range(1, 12).Select(i => (byte) i).ToArray(),
				Username = "srvUfrag:cliUfrag",
				Priority = 0x6e7f1eff,
				UseCandidate = true,
				IceControlling = 0x1122334455667788UL
			};

			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer);
			ReadOnlySpan<byte> bytes = buffer.Slice(0, written);
			Assert.IsTrue(StunMessage.LooksLikeStun(bytes));

			StunMessage parsed = StunMessage.Parse(bytes);
			Assert.AreEqual(StunMessageType.BindingRequest, parsed.Type);
			CollectionAssert.AreEqual(message.TransactionId, parsed.TransactionId);
			Assert.AreEqual("srvUfrag:cliUfrag", parsed.Username);
			Assert.AreEqual((uint) 0x6e7f1eff, parsed.Priority);
			Assert.IsTrue(parsed.UseCandidate);
			Assert.AreEqual(0x1122334455667788UL, parsed.IceControlling);
			Assert.IsNull(parsed.IceControlled);
		}

		[TestMethod]
		public void XorMappedAddress_RoundTrips_V4()
		{
			var message = new StunMessage
			{
				Type = StunMessageType.BindingSuccessResponse,
				TransactionId = new byte[12],
				XorMappedAddress = new IPEndPoint(IPAddress.Parse("192.168.10.230"), 54321)
			};

			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer);

			StunMessage parsed = StunMessage.Parse(buffer.Slice(0, written));
			Assert.AreEqual(message.XorMappedAddress, parsed.XorMappedAddress);
		}

		[TestMethod]
		public void Garbage_IsRejected()
		{
			Assert.IsFalse(StunMessage.LooksLikeStun(new byte[] {0x16, 0xfe, 0xfd, 0x00}));
			Assert.ThrowsExactly<FormatException>(() => StunMessage.Parse(new byte[24]));
		}
	}
}
