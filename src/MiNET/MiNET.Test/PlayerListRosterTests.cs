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
using System.Buffers;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.Skins;

namespace MiNET.Test
{
	/// <summary>
	///     The cached-slice roster path replaces the object-path serializer for the full roster a
	///     joiner receives, so its one non-negotiable property is byte identity with that
	///     serializer: any divergence is a wire regression a real client would reject. The skin
	///     store's refcounting is what makes slice sharing safe, so over- and under-release are
	///     pinned too.
	/// </summary>
	[TestClass]
	public class PlayerListRosterTests
	{
		private static Player MakePlayer(string name, byte skinFill)
		{
			var player = new Player(null, null)
			{
				ClientUuid = new UUID(Guid.NewGuid().ToByteArray()),
				EntityId = Math.Abs(name.GetHashCode()) % 100000,
				Username = name,
				DisplayName = name,
				NameTag = name,
				PlayerInfo = new PlayerInfo {DeviceOS = 7, PlatformChatId = string.Empty},
				Skin = MakeSkin(skinFill)
			};
			return player;
		}

		private static Skin MakeSkin(byte fill)
		{
			// The same complete skin PlayerMob builds: every field the wire writer touches is set.
			var data = new byte[8192];
			data.AsSpan().Fill(fill);
			return new Skin
			{
				SkinId = $"roster-test-{fill}.Custom",
				SkinResourcePatch = new SkinResourcePatch {Geometry = new GeometryIdentifier {Default = "geometry.humanoid.custom"}},
				Slim = false,
				ArmSize = "wide",
				SkinColor = "#0",
				GeometryDataVersion = "0.0.0",
				AnimationData = string.Empty,
				GeometryData = CryptoUtils.DefaultPlayerGeometry,
				IsVerified = true,
				Height = 32,
				Width = 64,
				Data = data
			};
		}

		[TestMethod]
		public void SequenceRoster_MatchesObjectPathEncode_ByteForByte()
		{
			Player[] players =
			{
				MakePlayer("RosterAlice", 0x11),
				MakePlayer("RosterBob", 0x22),
				MakePlayer("RosterCarol", 0x11) // shares Alice's skin bytes through the store
			};

			try
			{
				var packet = new McpePlayerList
				{
					records = McpePlayerList.Added(players)
				};
				byte[] expected = packet.Encode();

				ReadOnlySequence<byte> sequence = PlayerListRosterBuilder.BuildAdded(players);
				byte[] actual = sequence.ToArray();

				CollectionAssert.AreEqual(expected, actual);
			}
			finally
			{
				foreach (Player player in players) player.InvalidateRosterSlices();
			}
		}

		[TestMethod]
		public void SkinStore_SharesIdenticalSkins_AndEvictsOnLastRelease()
		{
			byte[] skinA1 = Encoding.ASCII.GetBytes("roster-skin-shared-" + Guid.NewGuid());
			byte[] skinA2 = (byte[]) skinA1.Clone();

			SerializedSkinStore.Handle first = SerializedSkinStore.Acquire(skinA1);
			SerializedSkinStore.Handle second = SerializedSkinStore.Acquire(skinA2);

			Assert.AreSame(first.Bytes, second.Bytes, "identical skin bytes must resolve to one shared array");

			SerializedSkinStore.Release(first);
			SerializedSkinStore.Handle third = SerializedSkinStore.Acquire((byte[]) skinA1.Clone());
			Assert.AreSame(second.Bytes, third.Bytes, "entry must survive while a holder remains");

			SerializedSkinStore.Release(second);
			SerializedSkinStore.Release(third);

			// After the last release the entry is evicted: a fresh acquire stores the new array
			// instead of resolving to the old one.
			byte[] skinA3 = (byte[]) skinA1.Clone();
			SerializedSkinStore.Handle fourth = SerializedSkinStore.Acquire(skinA3);
			Assert.AreSame(skinA3, fourth.Bytes, "an evicted entry must not resurrect the released array");
			SerializedSkinStore.Release(fourth);
		}

		[TestMethod]
		public void InvalidateRosterSlices_RebuildsWithChangedIdentity()
		{
			Player player = MakePlayer("RosterRename", 0x33);
			try
			{
				byte[] before = player.GetOrBuildRosterSlices().Prefix;

				player.DisplayName = "RosterRenamed";
				player.InvalidateRosterSlices();
				byte[] after = player.GetOrBuildRosterSlices().Prefix;

				Assert.IsFalse(before.SequenceEqual(after), "prefix must re-encode after a rename");
				Assert.IsTrue(Encoding.UTF8.GetString(after).Contains("RosterRenamed"));
			}
			finally
			{
				player.InvalidateRosterSlices();
			}
		}
	}
}