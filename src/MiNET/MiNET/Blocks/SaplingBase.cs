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
using System.Numerics;
using MiNET.Items;
using MiNET.Particles;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	/// <summary>
	///     Behaviour shared by every sapling. It used to live on the legacy
	///     <c>minecraft:sapling</c> class, which carried the wood type as a <c>sapling_type</c> state.
	///     Flattening made the type the block identity, so it is read back off the name here rather
	///     than switched on.
	/// </summary>
	public abstract partial class SaplingBase : Block
	{
		protected SaplingBase(int id) : base(id)
		{
			FuelEfficiency = 5;
		}

		/// <summary>The wood type, from the block's own name: minecraft:birch_sapling gives birch,
		/// minecraft:mangrove_propagule gives mangrove (Bedrock's mangrove "sapling" id).</summary>
		protected string WoodType => Name.Replace("minecraft:", "").Replace("_sapling", "").Replace("_propagule", "");

		protected override bool CanPlace(Level world, Player player, BlockCoordinates blockCoordinates, BlockCoordinates targetCoordinates, BlockFace face)
		{
			if (base.CanPlace(world, player, blockCoordinates, targetCoordinates, face))
			{
				Block under = world.GetBlock(Coordinates.BlockDown());
				return under is Dirt || under is Podzol || under is GrassBlock;
			}

			return false;
		}

		public override bool Interact(Level level, Player player, BlockCoordinates blockCoordinates, BlockFace face, Vector3 faceCoord)
		{
			if (player.Inventory.GetItemInHand() is ItemBoneMeal || (player.Inventory.GetItemInHand() is ItemDye inHand && inHand.Metadata == 15))
			{
				var random = new Random();
				for (int i = 0; i < 3; i++)
				{
					var particle = new LegacyParticle((int) ParticleType.VillagerHappy, level)
					{
						Position = (Vector3) Coordinates
									+ (new Vector3(0.5f, 0.5f, 0.5f)
										+ new Vector3((float) (random.NextDouble() - 0.5D), (float) (random.NextDouble() - 0.5D), (float) (random.NextDouble() - 0.5D)))
					};
					particle.Spawn();
				}

				if (random.NextDouble() < 0.45)
				{
					OnTick(level, true);
					player.ConsumeItemInHand();
					return true;
				}
			}

			return false;
		}

		public override void OnTick(Level level, bool isRandom)
		{
			if (!isRandom) return;

			var lightLevel = level.GetSubtractedLight(Coordinates);
			if (lightLevel >= 9 && new Random().Next(7) == 0)
			{
				// The log and leaves come straight off the wood type.
				Block log = BlockFactory.GetBlockByName($"minecraft:{WoodType}_log");
				Block leaves = BlockFactory.GetBlockByName($"minecraft:{WoodType}_leaves");
				if (log == null || leaves == null) return;

				// Bedrock grows dark oak and pale oak ONLY from a 2x2 patch of four saplings
				// of the same type (verified against the BDS oracle 2026-08-20); the north-west
				// sapling of the patch performs the growth and consumes all four.
				if (WoodType is "dark_oak" or "pale_oak")
				{
					TryGrowPatchTree(level, log, leaves);
					return;
				}

				LiteralTreeGenerator generator = WoodType switch
				{
					"oak" => new OakTreeGenerator(),
					"birch" => new BirchTreeGenerator(),
					"spruce" => new SpruceTreeGenerator(),
					"jungle" => new JungleTreeGenerator(),
					"acacia" => new AcaciaTreeGenerator(),
					"cherry" => new CherryTreeGenerator(),
					"mangrove" => new MangroveTreeGenerator(),
					_ => null,
				};

				if (generator == null) return;

				// The trunk cell at (0,0,0) replaces the sapling; no SetAir first, so a failed
				// generate never leaves a sapling floating on a partial trunk.
				if (!generator.Generate(level, Coordinates))
				{
					level.SetBlock(this);
				}
			}
		}

		private void TryGrowPatchTree(Level level, Block log, Block leaves)
		{
			// Find the 2x2 patch this sapling belongs to: all four cells must hold the same
			// sapling type. Only the north-west sapling grows, and only when the other three
			// are present.
			foreach (BlockCoordinates corner in new[]
			         {
				         Coordinates + new BlockCoordinates(-1, 0, -1),
				         Coordinates,
				         Coordinates + new BlockCoordinates(0, 0, -1),
				         Coordinates + new BlockCoordinates(-1, 0, 0),
			         })
			{
				bool isNorthWest = corner == Coordinates;
				bool complete = true;
				for (int dx = 0; dx < 2 && complete; dx++)
				{
					for (int dz = 0; dz < 2 && complete; dz++)
					{
						Block block = level.GetBlock(corner + new BlockCoordinates(dx, 0, dz));
						if (!(block is SaplingBase sapling) || sapling.WoodType != WoodType)
						{
							complete = false;
						}
					}
				}

				if (!complete) continue;
				if (!isNorthWest) return;

				LiteralTreeGenerator generator = WoodType == "pale_oak" ? new PaleOakTreeGenerator() : new DarkOakTreeGenerator();
				// No SetAir of the four saplings afterwards: the 2x2 trunk's bottom layer
				// occupies exactly those cells, and clearing them left the trunk base hollow
				// ("the base looks cut off").
				generator.Generate(level, corner);
				return;
			}
		}
	}
}

