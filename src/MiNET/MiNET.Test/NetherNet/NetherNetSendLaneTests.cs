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
using MiNET.Net;
using MiNET.Net.NetherNet;

namespace MiNET.Test.NetherNet
{
	/// <summary>
	///     The drain-time upsert: loss is sender policy, declared per packet via
	///     <see cref="Packet.CoalesceKey" />, and the lane's drain collapses each key to its LAST
	///     queued packet in that packet's own position. What matters beyond the survivors: unkeyed
	///     packets are never touched (dropping reliable traffic is the one forbidden act), relative
	///     order is preserved, and a superseded packet goes back to the pool exactly once.
	/// </summary>
	[TestClass]
	public class NetherNetSendLaneTests
	{
		private static Packet Keyed(object key)
		{
			McpeSetTime packet = McpeSetTime.CreateObject();
			packet.CoalesceKey = key;
			return packet;
		}

		private static Packet Unkeyed()
		{
			return McpeSetTime.CreateObject();
		}

		[TestMethod]
		public void LastPerKeySurvives_InItsOwnPosition_UnkeyedUntouched()
		{
			var key = new object();
			Packet a = Keyed(key);
			Packet plain1 = Unkeyed();
			Packet b = Keyed(key);
			Packet plain2 = Unkeyed();
			Packet c = Keyed(key);

			var pending = new List<Packet> {a, plain1, b, plain2, c};
			NetherNetSession.CoalescePending(pending);

			CollectionAssert.AreEqual(new List<Packet> {plain1, plain2, c}, pending, "only the last keyed packet survives, in order, unkeyed untouched");

			plain1.PutPool();
			plain2.PutPool();
			c.PutPool();
		}

		[TestMethod]
		public void DistinctKeys_NeverSupersedeEachOther()
		{
			var keyA = new object();
			var keyB = new object();
			Packet a1 = Keyed(keyA);
			Packet b1 = Keyed(keyB);
			Packet a2 = Keyed(keyA);

			var pending = new List<Packet> {a1, b1, a2};
			NetherNetSession.CoalescePending(pending);

			CollectionAssert.AreEqual(new List<Packet> {b1, a2}, pending, "keyB's only packet survives keyA's supersede");

			b1.PutPool();
			a2.PutPool();
		}

		[TestMethod]
		public void NoKeys_ListUntouched()
		{
			Packet p1 = Unkeyed();
			Packet p2 = Unkeyed();

			var pending = new List<Packet> {p1, p2};
			NetherNetSession.CoalescePending(pending);

			CollectionAssert.AreEqual(new List<Packet> {p1, p2}, pending);

			p1.PutPool();
			p2.PutPool();
		}

		[TestMethod]
		public void PooledReuse_NeverInheritsAKey()
		{
			var key = new object();
			McpeSetTime packet = McpeSetTime.CreateObject();
			packet.CoalesceKey = key;
			packet.PutPool();

			McpeSetTime reused = McpeSetTime.CreateObject();
			Assert.IsNull(reused.CoalesceKey, "Reset must clear the key or a pooled reuse could be wrongly dropped");
			reused.PutPool();
		}
	}
}
