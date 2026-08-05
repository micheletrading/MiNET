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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Worlds;

namespace MiNET.Test.Worlds
{
	[TestClass]
	public class SuperflatPresetTests
	{
		// The modern Java superflat preset: comma-separated layers, bottom first, each optionally
		// repeated with "count*", followed by an optional ";biome".
		//
		//   minecraft:bedrock,2*minecraft:dirt,minecraft:grass_block;minecraft:plains
		//
		// The pre-1.13 form led with a version number and trailed with a structures field
		// ("3;...;1;village"), which is why the old parser read layers out of the second field.

		[TestMethod]
		public void Parse_SingleLayer_IsThatBlock()
		{
			SuperflatPreset preset = SuperflatPreset.Parse("minecraft:bedrock");

			Assert.AreEqual(1, preset.Layers.Count);
			Assert.AreEqual("minecraft:bedrock", preset.Layers[0].Name);
		}

		[TestMethod]
		public void Parse_RepeatCount_ExpandsToThatManyLayers()
		{
			SuperflatPreset preset = SuperflatPreset.Parse("3*minecraft:dirt");

			Assert.AreEqual(3, preset.Layers.Count);
			CollectionAssert.AreEqual(
				new[] {"minecraft:dirt", "minecraft:dirt", "minecraft:dirt"},
				preset.Layers.Select(b => b.Name).ToArray());
		}

		// Order is the whole meaning of the list: PopulateChunk writes index 0 at y=0 and builds
		// upward, so a preset that parsed top-first would bury the surface.
		[TestMethod]
		public void Parse_LayersAreOrderedBottomFirst()
		{
			SuperflatPreset preset = SuperflatPreset.Parse("minecraft:bedrock,2*minecraft:dirt,minecraft:grass_block");

			CollectionAssert.AreEqual(
				new[] {"minecraft:bedrock", "minecraft:dirt", "minecraft:dirt", "minecraft:grass_block"},
				preset.Layers.Select(b => b.Name).ToArray());
		}

		// Java accepts a bare name and assumes the minecraft namespace.
		[TestMethod]
		public void Parse_BareName_ResolvesInTheMinecraftNamespace()
		{
			SuperflatPreset preset = SuperflatPreset.Parse("grass_block");

			Assert.AreEqual("minecraft:grass_block", preset.Layers[0].Name);
		}

		[TestMethod]
		public void Parse_BiomeSuffix_IsNotALayer()
		{
			SuperflatPreset preset = SuperflatPreset.Parse("minecraft:bedrock,minecraft:grass_block;minecraft:desert");

			Assert.AreEqual(2, preset.Layers.Count);
			Assert.AreEqual("minecraft:grass_block", preset.Layers[1].Name);
		}

		[TestMethod]
		public void Parse_BiomeSuffix_ResolvesToItsId()
		{
			Assert.AreEqual(2, SuperflatPreset.Parse("minecraft:sand;minecraft:desert").BiomeId);
			Assert.AreEqual(1, SuperflatPreset.Parse("minecraft:sand;plains").BiomeId);
		}

		[TestMethod]
		public void Parse_NoBiome_DefaultsToPlains()
		{
			Assert.AreEqual(1, SuperflatPreset.Parse("minecraft:bedrock").BiomeId);
		}

		// A typo in server.conf must name itself. The old parser threw a message built from an
		// already-split array, and an unknown block reached the world as a null layer.
		[TestMethod]
		public void Parse_UnknownBlock_ThrowsNamingIt()
		{
			var ex = Assert.ThrowsException<FormatException>(() => SuperflatPreset.Parse("minecraft:not_a_block"));

			StringAssert.Contains(ex.Message, "minecraft:not_a_block");
		}

		[TestMethod]
		public void Parse_UnknownBiome_ThrowsNamingIt()
		{
			var ex = Assert.ThrowsException<FormatException>(() => SuperflatPreset.Parse("minecraft:bedrock;minecraft:not_a_biome"));

			StringAssert.Contains(ex.Message, "minecraft:not_a_biome");
		}

		[TestMethod]
		public void Parse_RepeatCountThatIsNotANumber_ThrowsNamingTheLayer()
		{
			var ex = Assert.ThrowsException<FormatException>(() => SuperflatPreset.Parse("x*minecraft:dirt"));

			StringAssert.Contains(ex.Message, "x*minecraft:dirt");
		}

		// The pre-1.13 preset is not accepted, so a config carrying one fails loudly at startup
		// instead of reading the version number as a block name.
		[TestMethod]
		public void Parse_LegacyVersionedPreset_ThrowsRatherThanMisreading()
		{
			Assert.ThrowsException<FormatException>(
				() => SuperflatPreset.Parse("3;minecraft:bedrock,2*minecraft:dirt,minecraft:grass;1;village"));
		}

		[TestMethod]
		public void Parse_Empty_HasNoLayers()
		{
			Assert.AreEqual(0, SuperflatPreset.Parse("").Layers.Count);
		}
	}
}
