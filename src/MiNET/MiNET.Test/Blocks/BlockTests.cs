using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Items;
using MiNET.Utils;

namespace MiNET.Blocks.Tests
{
	/// <summary>
	///     Picking a block has to produce the item that places that same block back. These used to
	///     assert against hardcoded 1.18.30 palette indices, which stopped pointing at the blocks
	///     they named the moment the palette moved; they now look the state up by name so they keep
	///     testing the behaviour rather than a snapshot of the numbering.
	/// </summary>
	[TestClass]
	public class BlockTests
	{
		[TestMethod]
		public void PickedBlockGivesItemOfTheSameIdentity()
		{
			AssertPickYieldsItem("minecraft:iron_chain");
			AssertPickYieldsItem("minecraft:dark_oak_door");
			AssertPickYieldsItem("minecraft:stone");
			AssertPickYieldsItem("minecraft:oak_stairs");
			AssertPickYieldsItem("minecraft:tall_grass");
		}

		private static void AssertPickYieldsItem(string blockName)
		{
			BlockStateContainer state = BlockFactory.BlockPalette.First(b => b.Name == blockName);
			Block block = BlockFactory.GetBlockByName(blockName);
			Assert.IsNotNull(block, $"no block class for {blockName}");
			block.SetState(state.States);

			Item item = block.GetItem();
			Assert.IsNotNull(item, $"picking {blockName} produced no item");
			Assert.AreEqual(blockName, item.Name, "picked item is a different identity from the block");
			Assert.AreNotEqual(0, item.NetworkId, $"{blockName} resolved to no registry entry, so it cannot go on the wire");
		}

		/// <summary>
		///     A block's runtime id is its index in the palette, so setting a block's states and asking
		///     for the id has to land back on the very state that was set. Asserting a literal index
		///     only tests that the palette has not changed.
		/// </summary>
		[TestMethod]
		public void RuntimeIdRoundTripsThroughThePalette()
		{
			var block = new TallGrass {UpperBlockBit = true};

			int runtimeId = block.GetRuntimeId();
			Assert.AreNotEqual(-1, runtimeId, "state is not in the palette");

			BlockStateContainer state = BlockFactory.BlockPalette[runtimeId];
			Assert.AreEqual("minecraft:tall_grass", state.Name);
			Assert.AreEqual(1, state.States.OfType<BlockStateByte>().Single(s => s.Name == "upper_block_bit").Value);
		}
	}
}
