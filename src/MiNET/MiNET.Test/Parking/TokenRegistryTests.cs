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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Parking;

namespace MiNET.Test.Parking
{
	/// <summary>
	///     Personal access tokens for the parking transfer API: one per account, regenerating
	///     invalidates the old one, only the hash is persisted, and the scope rules are exactly
	///     what makes handing keys out safe: your own name on the front entrance, full power on
	///     your own doors, nothing anywhere else.
	/// </summary>
	[TestClass]
	public class TokenRegistryTests
	{
		private static string NewTempFile() => Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-tokens.json");

		[TestMethod]
		public void IssuedKey_FindsItsOwner()
		{
			var registry = new TokenRegistry(NewTempFile());

			string key = registry.Issue("xuid:1", "Gurun");
			AccessToken token = registry.Find(key);

			Assert.IsNotNull(token);
			Assert.AreEqual("xuid:1", token.OwnerId);
			Assert.AreEqual("Gurun", token.OwnerName);
		}

		[TestMethod]
		public void Reissue_InvalidatesTheOldKey()
		{
			var registry = new TokenRegistry(NewTempFile());

			string first = registry.Issue("xuid:1", "Gurun");
			string second = registry.Issue("xuid:1", "Gurun");

			Assert.AreNotEqual(first, second, "every issue is a fresh key");
			Assert.IsNull(registry.Find(first), "the old key must stop working the moment a new one exists");
			Assert.IsNotNull(registry.Find(second));
		}

		[TestMethod]
		public void UnknownOrEmptyKeys_FindNothing()
		{
			var registry = new TokenRegistry(NewTempFile());
			registry.Issue("xuid:1", "Gurun");

			Assert.IsNull(registry.Find("park_0000000000000000000000000000000000000000000000000000000000000000"));
			Assert.IsNull(registry.Find(""));
			Assert.IsNull(registry.Find(null));
		}

		[TestMethod]
		public void Keys_SurviveARestart_ButOnlyAsHashes()
		{
			string path = NewTempFile();
			string key = new TokenRegistry(path).Issue("xuid:1", "Gurun");

			var reloaded = new TokenRegistry(path);

			Assert.IsNotNull(reloaded.Find(key), "the registry persists across restarts");
			Assert.IsFalse(File.ReadAllText(path).Contains(key), "the plaintext key must never touch the disk");
		}

		[TestMethod]
		public void FrontEntrance_MovesOnlyTheOwnerByName()
		{
			var token = new AccessToken {OwnerId = "xuid:1", OwnerName = "Gurun"};

			Assert.IsNull(token.RefusalFor(door: null, targetName: "Gurun"), "your own name is allowed");
			Assert.IsNull(token.RefusalFor(door: null, targetName: "GURUN"), "names compare case-insensitively");
			Assert.IsNotNull(token.RefusalFor(door: null, targetName: "SomebodyElse"));
			Assert.IsNotNull(token.RefusalFor(door: null, targetName: "*"), "the wildcard is never yours on the front entrance");
		}

		[TestMethod]
		public void Doors_AnswerOnlyToTheirOwner()
		{
			var token = new AccessToken {OwnerId = "xuid:1", OwnerName = "Gurun"};
			var mine = new Door {Port = 19507, OwnerId = "xuid:1"};
			var theirs = new Door {Port = 19508, OwnerId = "xuid:2"};

			Assert.IsNull(token.RefusalFor(mine, "AnyName"));
			Assert.IsNull(token.RefusalFor(mine, "*"), "full power on your own door, wildcard included");
			Assert.IsNotNull(token.RefusalFor(theirs, "Gurun"), "even your own name is off limits through a door you do not own");
		}
	}
}