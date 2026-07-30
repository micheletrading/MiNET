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
using fNbt;
using MiNET.Utils;
using MiNET.Utils.Nbt;

namespace MiNET.Net
{
	/// <summary>
	///		Data models and Lazy embedded-resource loaders for the static join-sequence content
	///		exported from a decoded BDS 1.26.34 wire capture (src/MiNET/MiNET/Data/*.json). Each
	///		file mirrors the wire structure the corresponding packet's own AfterDecode already
	///		exposes; the Send* builders in Player.cs load these once and construct the packet
	///		through its typed fields (see MiNET/Player.cs, Send* methods for this data), never by
	///		replaying raw captured bytes (that's JoinCapture, which this replaces packet by packet).
	/// </summary>
	public static class JoinSequenceData
	{
		public static Nbt NbtFromBase64(string nbtB64)
		{
			byte[] bytes = Convert.FromBase64String(nbtB64);
			var nbtFile = new NbtFile {BigEndian = false, UseVarInt = true};
			nbtFile.LoadFromBuffer(bytes, 0, bytes.Length, NbtCompression.None);
			return new Nbt {NbtFile = nbtFile};
		}

		public class NbtDocumentFile
		{
			public string NbtB64 { get; set; }
		}

		public static readonly Lazy<NbtDocumentFile> JigsawStructureData = new Lazy<NbtDocumentFile>(() =>
			ResourceUtil.ReadResource<NbtDocumentFile>("jigsaw_structures.json", typeof(Player), "Data"));

		public static readonly Lazy<NbtDocumentFile> EntityIdentifiers = new Lazy<NbtDocumentFile>(() =>
			ResourceUtil.ReadResource<NbtDocumentFile>("entity_identifiers.json", typeof(Player), "Data"));

		public class EntityPropertyEntry
		{
			public string Name { get; set; }
			public string NbtB64 { get; set; }
		}

		public class EntityPropertiesFile
		{
			public List<EntityPropertyEntry> Entries { get; set; } = new List<EntityPropertyEntry>();
		}

		// Capture order (0012..0024) preserved: the join sequence sends one SyncEntityProperty
		// frame per entry, in this exact order.
		public static readonly Lazy<EntityPropertiesFile> EntityProperties = new Lazy<EntityPropertiesFile>(() =>
			ResourceUtil.ReadResource<EntityPropertiesFile>("entity_properties.json", typeof(Player), "Data"));

		public class PlayerFogFile
		{
			public List<string> Stack { get; set; } = new List<string>();
		}

		public static readonly Lazy<PlayerFogFile> PlayerFog = new Lazy<PlayerFogFile>(() =>
			ResourceUtil.ReadResource<PlayerFogFile>("player_fog.json", typeof(Player), "Data"));

		public class TrimDataFile
		{
			public List<TrimPattern> Patterns { get; set; } = new List<TrimPattern>();
			public List<TrimMaterial> Materials { get; set; } = new List<TrimMaterial>();
		}

		public static readonly Lazy<TrimDataFile> TrimData = new Lazy<TrimDataFile>(() =>
			ResourceUtil.ReadResource<TrimDataFile>("trim_data.json", typeof(Player), "Data"));

		public class BiomeDefinitionListFile
		{
			public List<BiomeDefinitionEntry> Definitions { get; set; } = new List<BiomeDefinitionEntry>();
			public List<string> Strings { get; set; } = new List<string>();
		}

		public static readonly Lazy<BiomeDefinitionListFile> BiomeDefinitions = new Lazy<BiomeDefinitionListFile>(() =>
			ResourceUtil.ReadResource<BiomeDefinitionListFile>("biome_definitions.json", typeof(Player), "Data"));

		public class VoxelShapesFile
		{
			public List<VoxelShape> Shapes { get; set; } = new List<VoxelShape>();
			public List<VoxelShapeNameEntry> NameMap { get; set; } = new List<VoxelShapeNameEntry>();
			public ushort CustomShapeCount { get; set; }
		}

		public static readonly Lazy<VoxelShapesFile> VoxelShapes = new Lazy<VoxelShapesFile>(() =>
			ResourceUtil.ReadResource<VoxelShapesFile>("voxel_shapes.json", typeof(Player), "Data"));

		public class CameraAimAssistPresetsFile
		{
			public List<AimAssistCategory> Categories { get; set; } = new List<AimAssistCategory>();
			public List<AimAssistPreset> Presets { get; set; } = new List<AimAssistPreset>();
			public byte Operation { get; set; }
		}

		public static readonly Lazy<CameraAimAssistPresetsFile> CameraAimAssistPresets = new Lazy<CameraAimAssistPresetsFile>(() =>
			ResourceUtil.ReadResource<CameraAimAssistPresetsFile>("camera_aim_assist_presets.json", typeof(Player), "Data"));

		// Camera presets/splines carry Vector2/Vector3-valued fields, which System.Numerics does
		// not expose as JSON-serializable properties (they are public fields), so the data files
		// use a plain x/y/z DTO for those and the Send* builder converts it to the packet's own
		// Vector2/Vector3 fields.
		public class Vec2Dto
		{
			public float X { get; set; }
			public float Y { get; set; }

			public Vector2 ToVector2()
			{
				return new Vector2(X, Y);
			}
		}

		public class Vec3Dto
		{
			public float X { get; set; }
			public float Y { get; set; }
			public float Z { get; set; }

			public Vector3 ToVector3()
			{
				return new Vector3(X, Y, Z);
			}
		}

		public class CameraPresetAimAssistDto
		{
			public string PresetId { get; set; }
			public byte? TargetMode { get; set; }
			public Vec2Dto Angle { get; set; }
			public float? Distance { get; set; }
		}

		public class CameraPresetDto
		{
			public string Name { get; set; }
			public string Parent { get; set; }
			public float? PositionX { get; set; }
			public float? PositionY { get; set; }
			public float? PositionZ { get; set; }
			public float? RotationX { get; set; }
			public float? RotationY { get; set; }
			public float? RotationSpeed { get; set; }
			public bool? SnapToTarget { get; set; }
			public Vec2Dto HorizontalRotationLimit { get; set; }
			public Vec2Dto VerticalRotationLimit { get; set; }
			public bool? ContinueTargeting { get; set; }
			public float? TrackingRadius { get; set; }
			public Vec2Dto Offset { get; set; }
			public Vec3Dto EntityOffset { get; set; }
			public float? Radius { get; set; }
			public float? YawLimitMin { get; set; }
			public float? YawLimitMax { get; set; }
			public byte? AudioListener { get; set; }
			public bool? PlayerEffects { get; set; }
			public CameraPresetAimAssistDto AimAssist { get; set; }
			public byte? ControlScheme { get; set; }
		}

		public class CameraPresetsFile
		{
			public List<CameraPresetDto> Presets { get; set; } = new List<CameraPresetDto>();
		}

		public static readonly Lazy<CameraPresetsFile> CameraPresets = new Lazy<CameraPresetsFile>(() =>
			ResourceUtil.ReadResource<CameraPresetsFile>("camera_presets.json", typeof(Player), "Data"));

		public class CameraRotationOptionDto
		{
			public Vec3Dto Value { get; set; }
			public float Time { get; set; }
			public int? Easing { get; set; }
		}

		public class CameraSplineDto
		{
			public string Name { get; set; }
			public float TotalTime { get; set; }
			public string SplineType { get; set; }
			public List<Vec3Dto> ControlPoints { get; set; } = new List<Vec3Dto>();
			public List<CameraProgressOption> ProgressKeyFrames { get; set; } = new List<CameraProgressOption>();
			public List<CameraRotationOptionDto> RotationKeyFrames { get; set; } = new List<CameraRotationOptionDto>();
		}

		public class CameraSplineFile
		{
			public List<CameraSplineDto> Splines { get; set; } = new List<CameraSplineDto>();
		}

		public static readonly Lazy<CameraSplineFile> CameraSpline = new Lazy<CameraSplineFile>(() =>
			ResourceUtil.ReadResource<CameraSplineFile>("camera_spline.json", typeof(Player), "Data"));
	}
}
