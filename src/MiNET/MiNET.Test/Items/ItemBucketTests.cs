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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using MiNET.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test.Items
{
	/// <summary>
	///     Legacy bucket metadata (8 water, 10 lava) must resolve to the flattened identities
	///     (minecraft:water_bucket / minecraft:lava_bucket) so /give shows the right item on the
	///     wire, and the result must stay a typed ItemBucket so pouring works.
	/// </summary>
	[TestClass]
	public class ItemBucketTests
	{
		[TestMethod]
		public void Give_lava_bucket_resolves_to_flattened_identity()
		{
			Item item = ItemFactory.GetItemByName("minecraft:bucket", 10, 1);

			Assert.IsInstanceOfType(item, typeof(ItemLavaBucket));
			Assert.AreEqual("minecraft:lava_bucket", item.Name);
			Assert.AreEqual(10, item.Metadata);
			Assert.AreEqual(366, item.NetworkId);
		}

		[TestMethod]
		public void Give_water_bucket_resolves_to_flattened_identity()
		{
			Item item = ItemFactory.GetItemByName("minecraft:bucket", 8, 1);

			Assert.IsInstanceOfType(item, typeof(ItemWaterBucket));
			Assert.AreEqual("minecraft:water_bucket", item.Name);
			Assert.AreEqual(365, item.NetworkId);
		}

		[TestMethod]
		public void Empty_bucket_stays_plain_bucket()
		{
			Item item = ItemFactory.GetItemByName("minecraft:bucket", 0, 1);

			Assert.IsInstanceOfType(item, typeof(ItemBucket));
			Assert.AreEqual("minecraft:bucket", item.Name);
			Assert.AreEqual(363, item.NetworkId);
		}
	}
}
