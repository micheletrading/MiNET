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
	// "Easing" is the easing_function enum (int32), not a string - confirmed against
	// Mojang's bedrock-protocol-docs CameraSplineProgressKeyFrame/RotationKeyFrame (protocol 2169).

	public class CameraProgressOption
	{
		public float Value { get; set; }
		public float Time { get; set; }
		public int? Easing { get; set; }
	}

	public class CameraRotationOption
	{
		public Vector3 Value { get; set; }
		public float Time { get; set; }
		public int? Easing { get; set; }
	}

	public class CameraSplineDefinition
	{
		public string Name { get; set; }
		public float TotalTime { get; set; }
		public string SplineType { get; set; }
		public List<Vector3> ControlPoints { get; set; } = new List<Vector3>();
		public List<CameraProgressOption> ProgressKeyFrames { get; set; } = new List<CameraProgressOption>();
		public List<CameraRotationOption> RotationKeyFrames { get; set; } = new List<CameraRotationOption>();
	}

	public partial class McpeCameraSpline : Packet<McpeCameraSpline>
	{
		public List<CameraSplineDefinition> Splines { get; set; } = new List<CameraSplineDefinition>();

		partial void AfterDecode()
		{
			uint splineCount = ReadUnsignedVarInt();
			for (int i = 0; i < splineCount; i++)
			{
				var spline = new CameraSplineDefinition
				{
					Name = ReadString(),
					TotalTime = ReadFloat(),
				};

				if (ReadBool()) spline.SplineType = ReadString();

				uint controlPointCount = ReadUnsignedVarInt();
				for (int j = 0; j < controlPointCount; j++) spline.ControlPoints.Add(ReadVector3());

				uint progressCount = ReadUnsignedVarInt();
				for (int j = 0; j < progressCount; j++)
				{
					var option = new CameraProgressOption {Value = ReadFloat(), Time = ReadFloat()};
					if (ReadBool()) option.Easing = ReadInt();
					spline.ProgressKeyFrames.Add(option);
				}

				uint rotationCount = ReadUnsignedVarInt();
				for (int j = 0; j < rotationCount; j++)
				{
					var option = new CameraRotationOption {Value = ReadVector3(), Time = ReadFloat()};
					if (ReadBool()) option.Easing = ReadInt();
					spline.RotationKeyFrames.Add(option);
				}

				Splines.Add(spline);
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Splines.Count);
			foreach (var spline in Splines)
			{
				Write(spline.Name);
				Write(spline.TotalTime);

				Write(spline.SplineType != null);
				if (spline.SplineType != null) Write(spline.SplineType);

				WriteUnsignedVarInt((uint) spline.ControlPoints.Count);
				foreach (var point in spline.ControlPoints) Write(point);

				WriteUnsignedVarInt((uint) spline.ProgressKeyFrames.Count);
				foreach (var option in spline.ProgressKeyFrames)
				{
					Write(option.Value);
					Write(option.Time);
					Write(option.Easing.HasValue);
					if (option.Easing.HasValue) Write(option.Easing.Value);
				}

				WriteUnsignedVarInt((uint) spline.RotationKeyFrames.Count);
				foreach (var option in spline.RotationKeyFrames)
				{
					Write(option.Value);
					Write(option.Time);
					Write(option.Easing.HasValue);
					if (option.Easing.HasValue) Write(option.Easing.Value);
				}
			}
		}
	}
}
