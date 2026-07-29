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
using System.Numerics;

namespace MiNET.Net
{
	public class CameraPresetAimAssist
	{
		public string PresetId { get; set; }

		// x-underlying-type uint8 (Mojang bedrock-protocol-docs Aim-Assist_Target_Mode, protocol 2169).
		// minecraft-data 1001 lists this as a li32 mapper instead; followed Mojang since it is the
		// semantic authority and the value space (angle/distance, 2 entries) fits comfortably in a byte.
		public byte? TargetMode { get; set; }
		public Vector2? Angle { get; set; }
		public float? Distance { get; set; }
	}

	public class CameraPreset
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
		public Vector2? HorizontalRotationLimit { get; set; }
		public Vector2? VerticalRotationLimit { get; set; }
		public bool? ContinueTargeting { get; set; }
		public float? TrackingRadius { get; set; }
		public Vector2? Offset { get; set; }
		public Vector3? EntityOffset { get; set; }
		public float? Radius { get; set; }
		public float? YawLimitMin { get; set; }
		public float? YawLimitMax { get; set; }
		public byte? AudioListener { get; set; }
		public bool? PlayerEffects { get; set; }
		public CameraPresetAimAssist AimAssist { get; set; }
		public byte? ControlScheme { get; set; }
	}

	public partial class McpeCameraPresets : Packet<McpeCameraPresets>
	{
		public List<CameraPreset> Presets { get; set; } = new List<CameraPreset>();

		partial void AfterDecode()
		{
			uint presetCount = ReadUnsignedVarInt();
			for (int i = 0; i < presetCount; i++)
			{
				var preset = new CameraPreset
				{
					Name = ReadString(),
					Parent = ReadString(),
				};

				if (ReadBool()) preset.PositionX = ReadFloat();
				if (ReadBool()) preset.PositionY = ReadFloat();
				if (ReadBool()) preset.PositionZ = ReadFloat();
				if (ReadBool()) preset.RotationX = ReadFloat();
				if (ReadBool()) preset.RotationY = ReadFloat();
				if (ReadBool()) preset.RotationSpeed = ReadFloat();
				if (ReadBool()) preset.SnapToTarget = ReadBool();
				if (ReadBool()) preset.HorizontalRotationLimit = ReadVector2();
				if (ReadBool()) preset.VerticalRotationLimit = ReadVector2();
				if (ReadBool()) preset.ContinueTargeting = ReadBool();
				if (ReadBool()) preset.TrackingRadius = ReadFloat();
				if (ReadBool()) preset.Offset = ReadVector2();
				if (ReadBool()) preset.EntityOffset = ReadVector3();
				if (ReadBool()) preset.Radius = ReadFloat();
				if (ReadBool()) preset.YawLimitMin = ReadFloat();
				if (ReadBool()) preset.YawLimitMax = ReadFloat();
				if (ReadBool()) preset.AudioListener = ReadByte();
				if (ReadBool()) preset.PlayerEffects = ReadBool();

				if (ReadBool())
				{
					var aimAssist = new CameraPresetAimAssist();
					if (ReadBool()) aimAssist.PresetId = ReadString();
					if (ReadBool()) aimAssist.TargetMode = ReadByte();
					if (ReadBool()) aimAssist.Angle = ReadVector2();
					if (ReadBool()) aimAssist.Distance = ReadFloat();
					preset.AimAssist = aimAssist;
				}

				if (ReadBool()) preset.ControlScheme = ReadByte();

				Presets.Add(preset);
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Presets.Count);
			foreach (var preset in Presets)
			{
				Write(preset.Name);
				Write(preset.Parent);

				Write(preset.PositionX.HasValue);
				if (preset.PositionX.HasValue) Write(preset.PositionX.Value);
				Write(preset.PositionY.HasValue);
				if (preset.PositionY.HasValue) Write(preset.PositionY.Value);
				Write(preset.PositionZ.HasValue);
				if (preset.PositionZ.HasValue) Write(preset.PositionZ.Value);
				Write(preset.RotationX.HasValue);
				if (preset.RotationX.HasValue) Write(preset.RotationX.Value);
				Write(preset.RotationY.HasValue);
				if (preset.RotationY.HasValue) Write(preset.RotationY.Value);
				Write(preset.RotationSpeed.HasValue);
				if (preset.RotationSpeed.HasValue) Write(preset.RotationSpeed.Value);
				Write(preset.SnapToTarget.HasValue);
				if (preset.SnapToTarget.HasValue) Write(preset.SnapToTarget.Value);
				Write(preset.HorizontalRotationLimit.HasValue);
				if (preset.HorizontalRotationLimit.HasValue) Write(preset.HorizontalRotationLimit.Value);
				Write(preset.VerticalRotationLimit.HasValue);
				if (preset.VerticalRotationLimit.HasValue) Write(preset.VerticalRotationLimit.Value);
				Write(preset.ContinueTargeting.HasValue);
				if (preset.ContinueTargeting.HasValue) Write(preset.ContinueTargeting.Value);
				Write(preset.TrackingRadius.HasValue);
				if (preset.TrackingRadius.HasValue) Write(preset.TrackingRadius.Value);
				Write(preset.Offset.HasValue);
				if (preset.Offset.HasValue) Write(preset.Offset.Value);
				Write(preset.EntityOffset.HasValue);
				if (preset.EntityOffset.HasValue) Write(preset.EntityOffset.Value);
				Write(preset.Radius.HasValue);
				if (preset.Radius.HasValue) Write(preset.Radius.Value);
				Write(preset.YawLimitMin.HasValue);
				if (preset.YawLimitMin.HasValue) Write(preset.YawLimitMin.Value);
				Write(preset.YawLimitMax.HasValue);
				if (preset.YawLimitMax.HasValue) Write(preset.YawLimitMax.Value);
				Write(preset.AudioListener.HasValue);
				if (preset.AudioListener.HasValue) Write(preset.AudioListener.Value);
				Write(preset.PlayerEffects.HasValue);
				if (preset.PlayerEffects.HasValue) Write(preset.PlayerEffects.Value);

				Write(preset.AimAssist != null);
				if (preset.AimAssist != null)
				{
					var aimAssist = preset.AimAssist;
					Write(aimAssist.PresetId != null);
					if (aimAssist.PresetId != null) Write(aimAssist.PresetId);
					Write(aimAssist.TargetMode.HasValue);
					if (aimAssist.TargetMode.HasValue) Write(aimAssist.TargetMode.Value);
					Write(aimAssist.Angle.HasValue);
					if (aimAssist.Angle.HasValue) Write(aimAssist.Angle.Value);
					Write(aimAssist.Distance.HasValue);
					if (aimAssist.Distance.HasValue) Write(aimAssist.Distance.Value);
				}

				Write(preset.ControlScheme.HasValue);
				if (preset.ControlScheme.HasValue) Write(preset.ControlScheme.Value);
			}
		}
	}
}
