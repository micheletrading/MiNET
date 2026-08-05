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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using MiNET.Blocks;

namespace MiNET.Worlds
{
	/// <summary>
	///     A superflat world written the way modern Minecraft writes it: the layers bottom first,
	///     each optionally repeated, then an optional biome.
	///     <code>minecraft:bedrock,2*minecraft:dirt,minecraft:grass_block;minecraft:plains</code>
	/// </summary>
	public class SuperflatPreset
	{
		private const byte PlainsBiomeId = 1;

		public List<Block> Layers { get; } = new List<Block>();
		public byte BiomeId { get; private set; } = PlainsBiomeId;

		public static SuperflatPreset Parse(string preset)
		{
			var result = new SuperflatPreset();
			if (string.IsNullOrWhiteSpace(preset)) return result;

			string[] parts = preset.Split(';');
			if (parts.Length > 2)
			{
				throw new FormatException($"Superflat preset '{preset}' has {parts.Length} sections. "
					+ "Expected layers, and optionally a biome: minecraft:bedrock,2*minecraft:dirt,minecraft:grass_block;minecraft:plains");
			}

			foreach (string layer in parts[0].Split(',', StringSplitOptions.RemoveEmptyEntries))
			{
				AddLayers(result.Layers, layer.Trim());
			}

			if (parts.Length == 2) result.BiomeId = ResolveBiome(parts[1].Trim());

			return result;
		}

		private static void AddLayers(List<Block> layers, string layer)
		{
			int count = 1;
			string name = layer;

			int star = layer.IndexOf('*');
			if (star >= 0)
			{
				if (!int.TryParse(layer[..star], out count) || count < 0)
				{
					throw new FormatException($"Superflat layer '{layer}' has no valid repeat count before the '*'.");
				}

				name = layer[(star + 1)..];
			}

			Block block = BlockFactory.GetBlockByName(name)
				?? throw new FormatException($"Superflat layer '{layer}' names a block that does not exist: '{name}'.");

			for (int i = 0; i < count; i++)
			{
				layers.Add(block);
			}
		}

		private static byte ResolveBiome(string name)
		{
			string bare = name.StartsWith("minecraft:", StringComparison.OrdinalIgnoreCase) ? name["minecraft:".Length..] : name;

			Biome biome = BiomeUtils.Biomes.FirstOrDefault(b => string.Equals(b.DefinitionName, bare, StringComparison.OrdinalIgnoreCase))
				?? throw new FormatException($"Superflat preset names a biome that does not exist: '{name}'.");

			return (byte) biome.Id;
		}
	}
}
