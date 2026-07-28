using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiNET.Items;
using MiNET.Utils;

namespace MiNET.Blocks.Tests
{
	[TestClass()]
	public class BlockTests
	{
		[TestMethod()]
		public void GetItemFromBlockStateTest()
		{
			// Picked block minecraft:chain from blockstate 7063 (1.18.30 palette)
			int runtimeId = 7063;

			BlockStateContainer blocStateFromPick = BlockFactory.BlockPalette[runtimeId];
			var block = BlockFactory.GetBlockById(blocStateFromPick.Id) as Chain;
			Assert.IsNotNull(block);
			block.SetState(blocStateFromPick.States);

			Item item = block.GetItem();

			Assert.AreEqual("minecraft:chain", item.Name);
			Assert.AreEqual(758, item.Id);
			Assert.AreEqual(0, item.Metadata);
		}

		[TestMethod()]
		public void GetDoorItemFromBlockStateTest()
		{
			// Picked block minecraft:dark_oak_door from blockstate 5667 (1.18.30 palette)
			int runtimeId = 5667;

			BlockStateContainer blocStateFromPick = BlockFactory.BlockPalette[runtimeId];
			var block = BlockFactory.GetBlockById(blocStateFromPick.Id) as DarkOakDoor;
			Assert.IsNotNull(block);
			block.SetState(blocStateFromPick.States);

			ItemBlock item = block.GetItem() as ItemBlock;
			Assert.IsNotNull(item, "Found no item");
			Assert.IsNotNull(item.Block);
			Assert.AreEqual("minecraft:dark_oak_door", item.Name);
			Assert.AreEqual(431, item.Id);
			Assert.AreEqual(0, item.Metadata);
		}

		[TestMethod()]
		public void GetRuntimeIdFromBlockStateTest()
		{
			var block = new DoublePlant();
			block.DoublePlantType = "grass";
			block.UpperBlockBit = true;

			Assert.AreEqual(5410, block.GetRuntimeId());
		}
	}
}