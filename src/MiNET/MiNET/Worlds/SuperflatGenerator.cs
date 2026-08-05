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

using System;
using System.Collections.Generic;
using System.Numerics;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Utils.Vectors;

namespace MiNET.Worlds
{
	public class SuperflatGenerator : IWorldGenerator
	{
		public string Seed { get; set; }
		public SuperflatPreset Preset { get; private set; } = new SuperflatPreset();
		public Dimension Dimension { get; set; }

		public SuperflatGenerator(Dimension dimension)
		{
			Dimension = dimension;
			switch (dimension)
			{
				case Dimension.Overworld:
					Seed = Config.GetProperty("superflat.overworld", "minecraft:bedrock,2*minecraft:dirt,minecraft:grass_block;minecraft:plains");
					break;
				case Dimension.Nether:
					Seed = Config.GetProperty("superflat.nether", "minecraft:bedrock,2*minecraft:netherrack,3*minecraft:lava,2*minecraft:netherrack,20*minecraft:air,minecraft:bedrock;minecraft:hell");
					break;
				case Dimension.TheEnd:
					Seed = Config.GetProperty("superflat.theend", "40*minecraft:air,minecraft:bedrock,7*minecraft:end_stone;minecraft:the_end");
					break;
			}
		}

		public void Initialize(IWorldProvider worldProvider)
		{
			Preset = SuperflatPreset.Parse(Seed);
		}

		public ChunkColumn GenerateChunkColumn(ChunkCoordinates chunkCoordinates)
		{
			var chunk = new ChunkColumn();
			chunk.X = chunkCoordinates.X;
			chunk.Z = chunkCoordinates.Z;

			PopulateChunk(chunk);

			var random = new Random((chunk.X * 397) ^ chunk.Z);
			if (random.NextDouble() > 0.99)
			{
				GenerateLake(random, chunk, Dimension == Dimension.Overworld ? new Water() : Dimension == Dimension.Nether ? (Block) new Lava() : new Air());
			}
			else if (random.NextDouble() > 0.97)
			{
				GenerateGlowStone(random, chunk);
			}

			return chunk;
		}

		private void GenerateGlowStone(Random random, ChunkColumn chunk)
		{
			if (Dimension != Dimension.Nether) return;

			int h = FindGroundLevel();

			if (h < 0) return;

			Vector2 center = new Vector2(7, 8);

			for (int x = 0; x < 16; x++)
			{
				for (int z = 0; z < 16; z++)
				{
					Vector2 v = new Vector2(x, z);
					if (random.Next((int) Vector2.DistanceSquared(center, v)) < 1)
					{
						chunk.SetBlock(x, Preset.Layers.Count - 2, z, new Glowstone());
						if (random.NextDouble() > 0.85)
						{
							chunk.SetBlock(x, Preset.Layers.Count - 3, z, new Glowstone());
							if (random.NextDouble() > 0.50)
							{
								chunk.SetBlock(x, Preset.Layers.Count - 4, z, new Glowstone());
							}
						}
					}
				}
			}
		}

		private void GenerateLake(Random random, ChunkColumn chunk, Block block)
		{
			int h = FindGroundLevel();

			if (h < 0) return;

			Vector2 center = new Vector2(7, 8);

			for (int x = 0; x < 16; x++)
			{
				for (int z = 0; z < 16; z++)
				{
					Vector2 v = new Vector2(x, z);
					if (random.Next((int) Vector2.DistanceSquared(center, v)) < 4)
					{
						if (Dimension == Dimension.Overworld)
						{
							chunk.SetBlock(x, h, z, block);
						}
						else if (Dimension == Dimension.Nether)
						{
							chunk.SetBlock(x, h, z, block);

							if (random.Next(30) == 0)
							{
								for (int i = h; i < Preset.Layers.Count - 1; i++)
								{
									chunk.SetBlock(x, i, z, block);
								}
							}
						}
						else if (Dimension == Dimension.TheEnd)
						{
							for (int i = 0; i < Preset.Layers.Count; i++)
							{
								chunk.SetBlock(x, i, z, new Air());
							}
						}
					}
					else if (Dimension == Dimension.TheEnd && random.Next((int) Vector2.DistanceSquared(center, v)) < 15)
					{
						chunk.SetBlock(x, h, z, new Air());
					}
				}
			}
		}

		private int FindGroundLevel()
		{
			int h = 0;
			bool foundSolid = false;
			foreach (var block in Preset.Layers)
			{
				if (foundSolid && block is Air) return h - 1;

				if (block.IsSolid) foundSolid = true;

				h++;
			}

			return foundSolid ? h - 1 : -1;
		}

		public void PopulateChunk(ChunkColumn chunk)
		{
			List<Block> layers = Preset.Layers;

			for (int x = 0; x < 16; x++)
			{
				for (int z = 0; z < 16; z++)
				{
					int h = 0;

					foreach (Block layer in layers)
					{
						chunk.SetBlock(x, h, z, layer);
						h++;
					}

					chunk.SetHeight(x, z, (short) h);
					for (int i = h + Dimension == Dimension.Overworld ? 1 : 0; i >= 0; i--)
					{
						chunk.SetSkyLight(x, i, z, 0);
					}

					// need to take care of skylight for non overworld to make it 0.

					chunk.SetBiome(x, z, Preset.BiomeId);
				}
			}
		}

	}
}