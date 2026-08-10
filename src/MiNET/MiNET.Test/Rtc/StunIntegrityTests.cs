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
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.Rtc;

namespace MiNET.Test.Rtc
{
	[TestClass]
	public class StunIntegrityTests
	{
		private static readonly byte[] Password = Encoding.UTF8.GetBytes("sVGnvBw3HFmoBW177Fnwzc");

		[TestMethod]
		public void Integrity_RoundTrips_AndVerifies()
		{
			var message = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = RandomNumberGenerator.GetBytes(12),
				Username = "left:right",
				Priority = 100
			};

			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer, Password, addFingerprint: true);
			ReadOnlySpan<byte> bytes = buffer.Slice(0, written);

			Assert.IsTrue(StunMessage.VerifyIntegrity(bytes, Password));
			byte[] wrongKey = Encoding.UTF8.GetBytes("wrong");
			Assert.IsFalse(StunMessage.VerifyIntegrity(bytes, wrongKey));

			StunMessage parsed = StunMessage.Parse(bytes);
			Assert.AreEqual("left:right", parsed.Username);
		}

		[TestMethod]
		public void Tampering_BreaksIntegrity()
		{
			var message = new StunMessage {Type = StunMessageType.BindingRequest, TransactionId = new byte[12], Username = "a:b"};
			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer, Password, addFingerprint: false);
			buffer[StunMessage.HeaderSize + 4] ^= 0xff; // first byte of the first attribute's value (USERNAME)
			Assert.IsFalse(StunMessage.VerifyIntegrity(buffer.Slice(0, written), Password));
		}

		[TestMethod]
		public void Fingerprint_IsIndependent_OfIntegrity()
		{
			var message = new StunMessage {Type = StunMessageType.BindingRequest, TransactionId = RandomNumberGenerator.GetBytes(12), Username = "a:b"};
			Span<byte> buffer = stackalloc byte[StunMessage.MaxSize];
			int written = message.WriteTo(buffer, ReadOnlySpan<byte>.Empty, addFingerprint: true);
			byte[] bytes = buffer.Slice(0, written).ToArray();

			SIPSorcery.Net.STUNMessage theirs = SIPSorcery.Net.STUNMessage.ParseSTUNMessage(bytes, bytes.Length);
			Assert.IsTrue(theirs.isFingerprintValid, "SIPSorcery rejected our FINGERPRINT-only message");

			Assert.IsFalse(StunMessage.VerifyIntegrity(bytes, Password), "message has no MESSAGE-INTEGRITY attribute to verify");
		}

		// The oracle: SIPSorcery must accept what we emit, and we must accept what SIPSorcery emits.
		[TestMethod]
		public void SipSorcery_Accepts_OurBytes_AndViceVersa()
		{
			var ours = new StunMessage
			{
				Type = StunMessageType.BindingRequest,
				TransactionId = RandomNumberGenerator.GetBytes(12),
				Username = "srv:cli",
				Priority = 0x7effffff,
				IceControlling = 42
			};
			byte[] ourBytes = new byte[StunMessage.MaxSize];
			int written = ours.WriteTo(ourBytes, Password, addFingerprint: true);
			Array.Resize(ref ourBytes, written);

			SIPSorcery.Net.STUNMessage theirs = SIPSorcery.Net.STUNMessage.ParseSTUNMessage(ourBytes, ourBytes.Length);
			Assert.AreEqual(SIPSorcery.Net.STUNMessageTypesEnum.BindingRequest, theirs.Header.MessageType);
			Assert.IsTrue(theirs.isFingerprintValid, "SIPSorcery rejected our FINGERPRINT");
			Assert.IsTrue(theirs.CheckIntegrity(Password), "SIPSorcery rejected our MESSAGE-INTEGRITY");

			var theirMessage = new SIPSorcery.Net.STUNMessage(SIPSorcery.Net.STUNMessageTypesEnum.BindingRequest);
			theirMessage.AddUsernameAttribute("srv:cli");
			byte[] theirBytes = theirMessage.ToByteBufferStringKey(Encoding.UTF8.GetString(Password), true);
			Assert.IsTrue(StunMessage.LooksLikeStun(theirBytes));
			Assert.IsTrue(StunMessage.VerifyIntegrity(theirBytes, Password), "we rejected SIPSorcery's MESSAGE-INTEGRITY");
			StunMessage parsed = StunMessage.Parse(theirBytes);
			Assert.AreEqual("srv:cli", parsed.Username);
		}
	}
}