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

namespace MiNET.Net
{
	/// <summary>
	/// One entry of the biome definitions array. Only the flat, generally useful fields are
	/// modeled. The optional "chunk_generation" world-gen sync data (climate, surface materials,
	/// noise rules, etc.) is parsed and discarded on decode since MiNET does not model world-gen
	/// templates, and is never emitted on encode.
	/// </summary>
	public class BiomeDefinitionEntry
	{
		public short NameIndex { get; set; }
		public ushort BiomeId { get; set; }
		public float Temperature { get; set; }
		public float Downfall { get; set; }
		public float SnowFoliage { get; set; }
		public float Depth { get; set; }
		public float Scale { get; set; }
		public int MapWaterColour { get; set; }
		public bool Rain { get; set; }
		public List<ushort> Tags { get; set; }
	}

	public partial class McpeBiomeDefinitionList
	{
		public List<BiomeDefinitionEntry> Definitions { get; set; } = new List<BiomeDefinitionEntry>();
		public List<string> Strings { get; set; } = new List<string>();

		partial void AfterDecode()
		{
			uint definitionCount = ReadUnsignedVarInt();
			for (int i = 0; i < definitionCount; i++)
			{
				var entry = new BiomeDefinitionEntry
				{
					NameIndex = ReadShort(),
					BiomeId = ReadUshort(),
					Temperature = ReadFloat(),
					Downfall = ReadFloat(),
					SnowFoliage = ReadFloat(),
					Depth = ReadFloat(),
					Scale = ReadFloat(),
					MapWaterColour = ReadInt(),
					Rain = ReadBool(),
				};

				if (ReadBool()) // tags (optional)
				{
					uint tagCount = ReadUnsignedVarInt();
					entry.Tags = new List<ushort>((int) tagCount);
					for (int t = 0; t < tagCount; t++)
					{
						entry.Tags.Add(ReadUshort());
					}
				}

				if (ReadBool()) // chunk_generation (optional) - parsed and discarded
				{
					SkipBiomeChunkGeneration();
				}

				Definitions.Add(entry);
			}

			uint stringCount = ReadUnsignedVarInt();
			for (int i = 0; i < stringCount; i++)
			{
				Strings.Add(ReadString());
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Definitions.Count);
			foreach (var entry in Definitions)
			{
				Write(entry.NameIndex);
				Write(entry.BiomeId);
				Write(entry.Temperature);
				Write(entry.Downfall);
				Write(entry.SnowFoliage);
				Write(entry.Depth);
				Write(entry.Scale);
				Write(entry.MapWaterColour);
				Write(entry.Rain);

				if (entry.Tags != null)
				{
					Write(true);
					WriteUnsignedVarInt((uint) entry.Tags.Count);
					foreach (ushort tag in entry.Tags)
					{
						Write(tag);
					}
				}
				else
				{
					Write(false);
				}

				Write(false); // chunk_generation - MiNET never sends world-gen sync data
			}

			WriteUnsignedVarInt((uint) Strings.Count);
			foreach (string s in Strings)
			{
				Write(s);
			}
		}

		// The following "chunk_generation" sub-structures are only ever skipped (never observed
		// from vanilla in practice), so they read raw wire values without allocating model objects.

		private void SkipBiomeChunkGeneration()
		{
			if (ReadBool()) SkipBiomeClimate();
			if (ReadBool()) SkipArray(SkipBiomeConsolidatedFeature);
			if (ReadBool()) SkipBiomeMountainParameters();
			if (ReadBool()) SkipArray(SkipBiomeElementData);
			if (ReadBool()) SkipBiomeSurfaceMaterial();

			ReadBool(); // has_default_overworld_surface
			ReadBool(); // has_swamp_surface
			ReadBool(); // has_frozen_ocean_surface
			ReadBool(); // has_end_surface

			if (ReadBool()) SkipBiomeMesaSurface();
			if (ReadBool()) SkipBiomeCappedSurface();
			if (ReadBool()) SkipBiomeOverworldRules();
			if (ReadBool()) SkipBiomeMultiNoiseRules();
			if (ReadBool()) SkipArray(SkipBiomeConditionalTransformation);
			if (ReadBool()) SkipArray(SkipBiomeReplacementData);

			if (ReadBool()) ReadByte(); // village_type
		}

		private void SkipArray(System.Action skipOne)
		{
			uint count = ReadUnsignedVarInt();
			for (int i = 0; i < count; i++)
			{
				skipOne();
			}
		}

		private void SkipBiomeSurfaceMaterial()
		{
			ReadInt(); // top_block
			ReadInt(); // mid_block
			ReadInt(); // sea_floor_block
			ReadInt(); // foundation_block
			ReadInt(); // sea_block
			ReadInt(); // sea_floor_depth
		}

		private void SkipBiomeClimate()
		{
			ReadFloat(); // temperature
			ReadFloat(); // downfall
			ReadFloat(); // snow_accumulation_min
			ReadFloat(); // snow_accumulation_max
		}

		private void SkipBiomeMountainParameters()
		{
			ReadInt(); // steep_block
			ReadBool(); // north_slopes
			ReadBool(); // south_slopes
			ReadBool(); // west_slopes
			ReadBool(); // east_slopes
			ReadBool(); // top_slide_enabled
		}

		private void SkipBiomeMesaSurface()
		{
			ReadUint(); // clay_material
			ReadUint(); // hard_clay_material
			ReadBool(); // bryce_pillars
			ReadBool(); // has_forest
		}

		private void SkipBiomeCappedSurface()
		{
			SkipArray(() => ReadInt()); // floor_blocks
			SkipArray(() => ReadInt()); // ceiling_blocks
			if (ReadBool()) ReadUint(); // sea_block
			if (ReadBool()) ReadUint(); // foundation_block
			if (ReadBool()) ReadUint(); // beach_block
		}

		private void SkipBiomeWeight()
		{
			ReadShort(); // biome
			ReadUint(); // weight
		}

		private void SkipBiomeTemperatureWeight()
		{
			ReadSignedVarInt(); // temperature
			ReadUint(); // weight
		}

		private void SkipBiomeCoordinate()
		{
			ReadSignedVarInt(); // min_value_type
			ReadShort(); // min_value
			ReadSignedVarInt(); // max_value_type
			ReadShort(); // max_value
			ReadUint(); // grid_offset
			ReadUint(); // grid_step_size
			ReadSignedVarInt(); // distribution
		}

		private void SkipBiomeScatterParameter()
		{
			SkipArray(SkipBiomeCoordinate); // coordinates
			ReadSignedVarInt(); // evaluation_order
			ReadSignedVarInt(); // chance_percent_type
			ReadShort(); // chance_percent
			ReadInt(); // chance_numerator
			ReadInt(); // chance_denominator
			ReadSignedVarInt(); // iterations_type
			ReadShort(); // iterations
		}

		private void SkipBiomeConsolidatedFeature()
		{
			SkipBiomeScatterParameter(); // scatter
			ReadShort(); // feature
			ReadShort(); // identifier
			ReadShort(); // pass
			ReadBool(); // can_use_internal
		}

		private void SkipBiomeElementData()
		{
			ReadFloat(); // noise_frequency_scale
			ReadFloat(); // noise_lower_bound
			ReadFloat(); // noise_upper_bound
			ReadSignedVarInt(); // height_min_type
			ReadShort(); // height_min
			ReadSignedVarInt(); // height_max_type
			ReadShort(); // height_max
			SkipBiomeSurfaceMaterial(); // adjusted_materials
		}

		private void SkipBiomeConditionalTransformation()
		{
			SkipArray(SkipBiomeWeight); // weighted_biomes
			ReadShort(); // condition_json
			ReadUint(); // min_passing_neighbours
		}

		private void SkipBiomeReplacementData()
		{
			ReadShort(); // biome
			ReadShort(); // dimension
			SkipArray(() => ReadShort()); // target_biomes
			ReadFloat(); // amount
			ReadFloat(); // noise_frequency_scale
			ReadUint(); // replacement_index
		}

		private void SkipBiomeOverworldRules()
		{
			SkipArray(SkipBiomeWeight); // hills_transformations
			SkipArray(SkipBiomeWeight); // mutate_transformations
			SkipArray(SkipBiomeWeight); // river_transformations
			SkipArray(SkipBiomeWeight); // shore_transformations
			SkipArray(SkipBiomeConditionalTransformation); // pre_hills_edge_transformations
			SkipArray(SkipBiomeConditionalTransformation); // post_shore_edge_transformations
			SkipArray(SkipBiomeTemperatureWeight); // climate_transformations
		}

		private void SkipBiomeMultiNoiseRules()
		{
			ReadFloat(); // temperature
			ReadFloat(); // humidity
			ReadFloat(); // altitude
			ReadFloat(); // weirdness
			ReadFloat(); // weight
		}
	}
}
