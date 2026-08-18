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
		protected SaplingBase()
		{
			FuelEfficiency = 5;
		}

		/// <summary>The wood type, from the block's own name: minecraft:birch_sapling gives birch.</summary>
		protected string WoodType => Name.Replace("minecraft:", "").Replace("_sapling", "");

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
			if (player.Inventory.GetItemInHand() is ItemDye inHand && inHand.Metadata == 15)
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
				// The log and leaves come straight off the wood type. Only oak and birch ever had a
				// working generator; the rest were commented out on the legacy class and are left
				// that way rather than quietly gaining trees they never grew.
				Block log = BlockFactory.GetBlockByName($"minecraft:{WoodType}_log");
				Block leaves = BlockFactory.GetBlockByName($"minecraft:{WoodType}_leaves");
				if (log == null || leaves == null) return;

				SmallTreeGenerator generator = WoodType switch
				{
					"oak" => new SmallTreeGenerator(log, leaves, 4),
					"birch" => new SmallTreeGenerator(log, leaves, 5),
					_ => null,
				};

				if (generator == null) return;

				level.SetAir(Coordinates);

				if (!generator.Generate(level, Coordinates))
				{
					level.SetBlock(this);
				}
			}
		}
	}
}
