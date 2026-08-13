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
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Vectors;

namespace MiNET.Test
{
	[TestClass]
	public class InventoryTransactionWireTests
	{
		// Every block a player places, breaks or uses arrives in this packet, so a byte out of
		// place here puts blocks somewhere other than where they were clicked. It is about to move
		// from a hand-written codec onto the Mojang schemas, and unlike the outbound packets this
		// one is read, not written, so both directions have to hold: the same transaction has to
		// encode to the same bytes, and those bytes have to decode back to the same values.

		[TestMethod]
		public void ItemUseTransaction_EncodesToTheSameBytesItAlwaysHas()
		{
			var packet = McpeInventoryTransaction.CreateObject();
			packet.legacyRequestId = 0;
			packet.transaction = BuildItemUse();

			byte[] encoded = packet.Encode();

			Assert.AreEqual(ExpectedItemUse, Convert.ToHexString(encoded).ToLowerInvariant());
		}

		[TestMethod]
		public void ItemUseTransaction_SurvivesARoundTrip()
		{
			var packet = McpeInventoryTransaction.CreateObject();
			packet.legacyRequestId = 0;
			packet.transaction = BuildItemUse();
			byte[] encoded = packet.Encode();

			var decoded = McpeInventoryTransaction.CreateObject();
			decoded.Decode(encoded);

			var source = (ItemUseInventoryTransaction) packet.transaction;
			var result = decoded.transaction as ItemUseInventoryTransaction;

			Assert.IsNotNull(result, "an item use transaction has to come back as one");
			Assert.AreEqual(packet.legacyRequestId, decoded.legacyRequestId);
			Assert.AreEqual(source.actionType, result.actionType);
			Assert.AreEqual(source.position, result.position);
			Assert.AreEqual(source.face, result.face);
			Assert.AreEqual(source.slot, result.slot);
			Assert.AreEqual(source.targetBlockId, result.targetBlockId);
			Assert.AreEqual(source.fromPosition, result.fromPosition);
			Assert.AreEqual(source.clickPosition, result.clickPosition);
		}

		// Captured from the hand-written codec before the conversion:
		//
		//   1e            packet id 0x1e
		//   00            request id 0, zigzag
		//   00            no changed slots
		//   01            presence byte before the transaction type, which IS on the wire here
		//   02            transaction type 2, item use
		//   01            presence byte before the transaction data
		//   00            no transaction records
		//   00            action 0, place
		//   01            trigger type 1, player input
		//   14 8201 27    position 10, 65, -20, three zigzag varints
		//   01            face 1
		//   06            slot 3, zigzag
		//   0000 0000 00 00 00 00      the carried item, air, eight bytes
		//   00002841 00008442 00009cc1 from position 10.5, 66.0, -19.5
		//   0000003f 0000803f 0000003f click position 0.5, 1.0, 0.5
		//   b015          block runtime id 2736
		//   00 00         client prediction, client cooldown state
		private const string ExpectedItemUse =
			"1e" + "00" + "00" + "01" + "02" + "01" + "00" +
			"00" + "01" + "14820127" + "01" + "06" +
			"0000000000000000" +
			"000028410000844200009cc1" +
			"0000003f0000803f0000003f" +
			"b015" + "0000";

		private static ItemUseInventoryTransaction BuildItemUse()
		{
			return new ItemUseInventoryTransaction
			{
				actionType = ItemUseInventoryTransaction.ItemUseActionType.Place,
				triggerType = ItemUseInventoryTransaction.ItemUseTriggerType.PlayerInput,
				position = new BlockCoordinates(10, 65, -20),
				face = 1,
				slot = 3,
				item = new ItemAir(),
				fromPosition = new Vector3(10.5f, 66.0f, -19.5f),
				clickPosition = new Vector3(0.5f, 1.0f, 0.5f),
				targetBlockId = 2736,
				actions = new List<InventoryAction>()
			};
		}

		// Both captured from a real 1.26.42 client (protocol 2168) over the tunnel, one placing an
		// oak log onto a block (64 -> 63 in the slot), one right-clicking the air with it. The place
		// carries one legacy set slot and one container action, so it pins the whole layout: the
		// InventorySource optionals are DoubleOptional on the wire (two presence bytes each, the
		// outer always set) and the action items are the regular NetworkItemStackDescriptor.
		//
		//   1e            packet id 0x1e
		//   0f            request id -8, zigzag
		//   01 01         changed slots present, one entry
		//   1d 01 05      legacy set slot: container 29, one slot, slot 5
		//   01            presence byte before the transaction type
		//   02            transaction type 2, item use
		//   01            presence byte before the transaction data
		//   01            one transaction record
		//   00            source type 0, container inventory
		//   01 01 00      window id: outer presence, inner presence, value 0
		//   01 00         source flags: outer presence, inner absent
		//   05            slot 5
		//   1100 4000 00 00 950c 0a 00000000000000000000
		//                 old item: oak_log x64, runtime 2700, empty extra data
		//   1100 3f00 00 00 950c 0a 00000000000000000000
		//                 new item: oak_log x63
		//   00            action 0, place
		//   01            trigger type 1, player input
		//   030610        position -2, 3, 8, three zigzag varints
		//   01            face 1
		//   0a            slot 5, zigzag
		//   1100 4000 00 00 950c 0a 00000000000000000000
		//                 the held item, oak_log x64
		//   5fb49e3f 1fd7b340 8145dd40 from position 1.23987949, 5.62001, 6.91473436
		//   406e663e 0000803f 608d1c3f click position 0.225029945, 1.0, 0.6115322
		//   b65e          block runtime id 12086
		//   01 00         client prediction success, client cooldown off
		private const string ExpectedLivePlace =
			"1e" + "0f" + "0101" + "1d0105" + "01" + "02" + "01" + "01" +
			"00" + "010100" + "0100" + "05" +
			"110040000000950c0a" + "00000000000000000000" +
			"11003f000000950c0a" + "00000000000000000000" +
			"00" + "01" + "030610" + "01" + "0a" +
			"110040000000950c0a" + "00000000000000000000" +
			"5fb49e3f" + "1fd7b340" + "8145dd40" +
			"406e663e" + "0000803f" + "608d1c3f" +
			"b65e" + "0100";

		//   1e            packet id 0x1e
		//   00            request id 0, zigzag
		//   00            no changed slots
		//   01 02         presence byte, transaction type 2, item use
		//   01            presence byte before the transaction data
		//   00            no transaction records
		//   02            action 1, use (click air)
		//   00            trigger type 0, unknown
		//   000000        position 0, 0, 0
		//   ff            face 255, no block face
		//   0a            slot 5, zigzag
		//   1100 3f00 00 00 950c 0a 00000000000000000000 the held item, oak_log x63
		//   5fb49e3f 1fd7b340 8145dd40 from position 1.23987949, 5.62001, 6.91473436
		//   00000000 00000000 00000000 click position 0, 0, 0
		//   00            block runtime id 0
		//   00 00         client prediction, client cooldown state
		private const string ExpectedLiveUse =
			"1e" + "00" + "00" + "01" + "02" + "01" + "00" +
			"02" + "00" + "000000" + "ff" + "0a" +
			"11003f000000950c0a" + "00000000000000000000" +
			"5fb49e3f" + "1fd7b340" + "8145dd40" +
			"00000000" + "00000000" + "00000000" +
			"00" + "0000";

		[TestMethod]
		public void ItemUsePlace_FromLiveClient2168_DecodesPositionAndItems()
		{
			var packet = McpeInventoryTransaction.CreateObject();
			packet.Decode(Convert.FromHexString(ExpectedLivePlace));

			Assert.AreEqual(-8, packet.legacyRequestId);
			Assert.AreEqual(1, packet.legacySetItemSlots.Count);
			Assert.AreEqual(LegacySetSlot.ContainerEnumName.Furnaceresultcontainer, packet.legacySetItemSlots[0].containerEnum);
			CollectionAssert.AreEqual(new byte[] {5}, packet.legacySetItemSlots[0].slots);

			var txn = packet.transaction as ItemUseInventoryTransaction;
			Assert.IsNotNull(txn, "a place has to come back as an item use transaction");

			Assert.AreEqual(1, txn.actions.Count);
			InventoryAction action = txn.actions[0];
			Assert.AreEqual(InventorySource.InventorySourceType.ContainerInventory, action.source.sourceType);
			Assert.AreEqual((sbyte) 0, action.source.containerId);
			Assert.IsNull(action.source.bitFlags);
			Assert.AreEqual(5u, action.slot);
			Assert.AreEqual("minecraft:oak_log", action.fromItem.Name);
			Assert.AreEqual((byte) 64, action.fromItem.Count);
			Assert.AreEqual("minecraft:oak_log", action.toItem.Name);
			Assert.AreEqual((byte) 63, action.toItem.Count);

			Assert.AreEqual(ItemUseInventoryTransaction.ItemUseActionType.Place, txn.actionType);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUseTriggerType.PlayerInput, txn.triggerType);
			Assert.AreEqual(new BlockCoordinates(-2, 3, 8), txn.position);
			Assert.AreEqual((byte) 1, txn.face);
			Assert.AreEqual(5, txn.slot);
			Assert.AreEqual("minecraft:oak_log", txn.item.Name);
			Assert.AreEqual((byte) 64, txn.item.Count);
			Assert.AreEqual(new Vector3(1.23987949f, 5.62001f, 6.91473436f), txn.fromPosition);
			Assert.AreEqual(new Vector3(0.225029945f, 1.0f, 0.6115322f), txn.clickPosition);
			Assert.AreEqual(12086u, txn.targetBlockId);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUsePredictedResult.Success, txn.clientInteractPrediction);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUseClientCooldownState.Off, txn.clientCooldownState);
		}

		[TestMethod]
		public void ItemUseUse_FromLiveClient2168_DecodesPositionAndItems()
		{
			var packet = McpeInventoryTransaction.CreateObject();
			packet.Decode(Convert.FromHexString(ExpectedLiveUse));

			var txn = packet.transaction as ItemUseInventoryTransaction;
			Assert.IsNotNull(txn, "a use has to come back as an item use transaction");

			Assert.AreEqual(0, txn.actions.Count);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUseActionType.Use, txn.actionType);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUseTriggerType.Unknown, txn.triggerType);
			Assert.AreEqual(new BlockCoordinates(0, 0, 0), txn.position);
			Assert.AreEqual((byte) 255, txn.face);
			Assert.AreEqual(5, txn.slot);
			Assert.AreEqual("minecraft:oak_log", txn.item.Name);
			Assert.AreEqual((byte) 63, txn.item.Count);
			Assert.AreEqual(new Vector3(1.23987949f, 5.62001f, 6.91473436f), txn.fromPosition);
			Assert.AreEqual(Vector3.Zero, txn.clickPosition);
			Assert.AreEqual(0u, txn.targetBlockId);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUsePredictedResult.Failure, txn.clientInteractPrediction);
			Assert.AreEqual(ItemUseInventoryTransaction.ItemUseClientCooldownState.Off, txn.clientCooldownState);
		}

		[TestMethod]
		public void ItemUsePlace_FromLiveClient2168_RoundTripsByteIdentical()
		{
			var packet = McpeInventoryTransaction.CreateObject();
			packet.Decode(Convert.FromHexString(ExpectedLivePlace));

			byte[] encoded = packet.Encode();

			Assert.AreEqual(ExpectedLivePlace, Convert.ToHexString(encoded).ToLowerInvariant());
		}
	}
}
