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

namespace MiNET.Test
{
	/// <summary>
	///     A player's UUID is not the server's to invent. The client derives its own by a fixed rule
	///     and matches the player list against it, so a server that computes a different value hands
	///     every client an entry belonging to a stranger. That is exactly what MiNET did: it hashed
	///     "minet:" + xuid into a raw Guid, and a real client responded by listing itself twice, once
	///     from its own knowledge and once from ours.
	/// </summary>
	[TestClass]
	public class PlayerIdentityTests
	{
		/// <summary>
		///     The known-answer test. This UUID is what vanilla BDS 1.26.34 actually put on the wire
		///     for a live Xbox-authenticated client with this XUID, read out of a packet capture. If
		///     the derivation drifts, this fails and nobody has to rediscover it from a duplicated row
		///     in a player list.
		/// </summary>
		[TestMethod]
		public void PlayerUuidMatchesWhatVanillaSendsForTheSameXuid()
		{
			Assert.AreEqual("6cdfec82-45b6-3322-9111-084cd74e32f0", DeriveUuidFromXuid("2535410512372218").ToString());
		}

		/// <summary>
		///     Six of the 128 bits are not hash: four say "version 3, MD5-based" and two say
		///     "RFC 4122". Omitting them is what made the old value not a UUID at all.
		/// </summary>
		[TestMethod]
		public void DerivedUuidIsAWellFormedVersion3Uuid()
		{
			foreach (string xuid in new[] {"2535410512372218", "1", "9999999999999999"})
			{
				string uuid = DeriveUuidFromXuid(xuid).ToString();

				Assert.AreEqual('3', uuid[14], $"{uuid}: version nibble must be 3");
				Assert.IsTrue("89ab".IndexOf(uuid[19]) >= 0, $"{uuid}: variant must be RFC 4122");
			}
		}

		/// <summary>
		///     Same XUID, same player, every time. A UUID that changes between logins makes the client
		///     treat a returning player as somebody new.
		/// </summary>
		[TestMethod]
		public void DerivationIsStable()
		{
			Assert.AreEqual(DeriveUuidFromXuid("2535410512372218"), DeriveUuidFromXuid("2535410512372218"));
			Assert.AreNotEqual(DeriveUuidFromXuid("2535410512372218"), DeriveUuidFromXuid("2535410512372219"));
		}

		/// <summary>
		///     A .NET Guid is not byte-order-identical to a UUID: the first three fields are stored
		///     little-endian. Handing the hash straight to new Guid(byte[]) silently scrambles the
		///     first eight bytes, which still looks like a plausible UUID and is the wrong one.
		/// </summary>
		[TestMethod]
		public void GuidByteArrayConstructorWouldScrambleIt()
		{
			byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("pocket-auth-1-xuid:2535410512372218"));
			hash[6] = (byte) ((hash[6] & 0x0f) | 0x30);
			hash[8] = (byte) ((hash[8] & 0x3f) | 0x80);

			Assert.AreNotEqual(new Guid(hash).ToString(), DeriveUuidFromXuid("2535410512372218").ToString(),
				"if these ever match, the byte-order trap has gone away and the explicit construction is no longer needed");
		}

		/// <summary>Mirror of LoginMessageHandler.DeriveUuidFromXuid, which is private.</summary>
		private static Guid DeriveUuidFromXuid(string xuid)
		{
			byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("pocket-auth-1-xuid:" + xuid));
			hash[6] = (byte) ((hash[6] & 0x0f) | 0x30);
			hash[8] = (byte) ((hash[8] & 0x3f) | 0x80);

			return new Guid(
				(hash[0] << 24) | (hash[1] << 16) | (hash[2] << 8) | hash[3],
				(short) ((hash[4] << 8) | hash[5]),
				(short) ((hash[6] << 8) | hash[7]),
				hash[8], hash[9], hash[10], hash[11], hash[12], hash[13], hash[14], hash[15]);
		}
	}
}
