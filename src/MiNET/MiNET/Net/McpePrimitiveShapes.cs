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
	// PrimitiveShapeType -> payload type (PMMP PrimitiveShapeType::getPayloadType):
	// NONE=0, LINE=4, BOX=3, SPHERE=5, CIRCLE=5, TEXT=2, ARROW=1, CYLINDER=6, PYRAMID=7,
	// ELLIPSOID=8, CONE=9.
	public class PrimitiveShapeData
	{
		public long NetworkId { get; set; }
		public byte? Type { get; set; }
		public Vector3? Location { get; set; }
		public float? Scale { get; set; }
		public Vector3? Rotation { get; set; }
		public float? TotalTimeLeft { get; set; }
		public float? MaximumRenderDistance { get; set; }
		public uint? Color { get; set; }
		public int? DimensionId { get; set; }
		public long? AttachedToEntityId { get; set; }
		public int PayloadType { get; set; }

		// Arrow payload (1)
		public Vector3? ArrowLineEndLocation { get; set; }
		public float? ArrowHeadLength { get; set; }
		public float? ArrowHeadRadius { get; set; }
		public byte? ArrowSegments { get; set; }

		// Text payload (2)
		public string Text { get; set; }
		public bool TextUseRotation { get; set; }
		public uint? TextBackgroundColor { get; set; }
		public bool TextDepthTest { get; set; }
		public bool TextShowBackface { get; set; }
		public bool TextShowTextBackface { get; set; }

		// Box payload (3)
		public Vector3 BoxBound { get; set; }

		// Line payload (4)
		public Vector3 LineEndLocation { get; set; }

		// Circle/sphere payload (5)
		public byte CircleOrSphereSegments { get; set; }

		// Cylinder payload (6)
		public Vector2 CylinderRadiusX { get; set; }
		public Vector2 CylinderRadiusZ { get; set; }
		public float CylinderHeight { get; set; }
		public byte CylinderSegments { get; set; }

		// Pyramid payload (7)
		public float PyramidWidth { get; set; }
		public float? PyramidDepth { get; set; }
		public float PyramidHeight { get; set; }

		// Ellipsoid payload (8)
		public Vector3 EllipsoidRadii { get; set; }
		public byte EllipsoidSegmentsPerAxis { get; set; }

		// Cone payload (9)
		public Vector2 ConeRadii { get; set; }
		public float ConeHeight { get; set; }
		public byte ConeSegments { get; set; }
	}

	public partial class McpePrimitiveShapes : Packet<McpePrimitiveShapes>
	{
		public List<PrimitiveShapeData> Shapes { get; set; } = new List<PrimitiveShapeData>();

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Shapes.Count);
			foreach (var s in Shapes)
			{
				WriteUnsignedVarLong(s.NetworkId);

				Write(s.Type.HasValue);
				if (s.Type.HasValue) Write(s.Type.Value);

				Write(s.Location.HasValue);
				if (s.Location.HasValue) Write(s.Location.Value);

				Write(s.Scale.HasValue);
				if (s.Scale.HasValue) Write(s.Scale.Value);

				Write(s.Rotation.HasValue);
				if (s.Rotation.HasValue) Write(s.Rotation.Value);

				Write(s.TotalTimeLeft.HasValue);
				if (s.TotalTimeLeft.HasValue) Write(s.TotalTimeLeft.Value);

				Write(s.MaximumRenderDistance.HasValue);
				if (s.MaximumRenderDistance.HasValue) Write(s.MaximumRenderDistance.Value);

				Write(s.Color.HasValue);
				if (s.Color.HasValue) Write(s.Color.Value);

				Write(s.DimensionId.HasValue);
				if (s.DimensionId.HasValue) WriteSignedVarInt(s.DimensionId.Value);

				Write(s.AttachedToEntityId.HasValue);
				if (s.AttachedToEntityId.HasValue) WriteUnsignedVarLong(s.AttachedToEntityId.Value);

				WriteUnsignedVarInt((uint) s.PayloadType);
				switch (s.PayloadType)
				{
					case 1:
						Write(s.ArrowLineEndLocation.HasValue);
						if (s.ArrowLineEndLocation.HasValue) Write(s.ArrowLineEndLocation.Value);
						Write(s.ArrowHeadLength.HasValue);
						if (s.ArrowHeadLength.HasValue) Write(s.ArrowHeadLength.Value);
						Write(s.ArrowHeadRadius.HasValue);
						if (s.ArrowHeadRadius.HasValue) Write(s.ArrowHeadRadius.Value);
						Write(s.ArrowSegments.HasValue);
						if (s.ArrowSegments.HasValue) Write(s.ArrowSegments.Value);
						break;
					case 2:
						Write(s.Text ?? string.Empty);
						Write(s.TextUseRotation);
						Write(s.TextBackgroundColor.HasValue);
						if (s.TextBackgroundColor.HasValue) Write(s.TextBackgroundColor.Value);
						Write(s.TextDepthTest);
						Write(s.TextShowBackface);
						Write(s.TextShowTextBackface);
						break;
					case 3:
						Write(s.BoxBound);
						break;
					case 4:
						Write(s.LineEndLocation);
						break;
					case 5:
						Write(s.CircleOrSphereSegments);
						break;
					case 6:
						Write(s.CylinderRadiusX);
						Write(s.CylinderRadiusZ);
						Write(s.CylinderHeight);
						Write(s.CylinderSegments);
						break;
					case 7:
						Write(s.PyramidWidth);
						Write(s.PyramidDepth.HasValue);
						if (s.PyramidDepth.HasValue) Write(s.PyramidDepth.Value);
						Write(s.PyramidHeight);
						break;
					case 8:
						Write(s.EllipsoidRadii);
						Write(s.EllipsoidSegmentsPerAxis);
						break;
					case 9:
						Write(s.ConeRadii);
						Write(s.ConeHeight);
						Write(s.ConeSegments);
						break;
				}
			}
		}

		partial void AfterDecode()
		{
			int count = (int) ReadUnsignedVarInt();
			Shapes = new List<PrimitiveShapeData>(count);
			for (int i = 0; i < count; i++)
			{
				var s = new PrimitiveShapeData {NetworkId = ReadUnsignedVarLong()};

				if (ReadBool()) s.Type = ReadByte();
				if (ReadBool()) s.Location = ReadVector3();
				if (ReadBool()) s.Scale = ReadFloat();
				if (ReadBool()) s.Rotation = ReadVector3();
				if (ReadBool()) s.TotalTimeLeft = ReadFloat();
				if (ReadBool()) s.MaximumRenderDistance = ReadFloat();
				if (ReadBool()) s.Color = ReadUint();
				if (ReadBool()) s.DimensionId = ReadSignedVarInt();
				if (ReadBool()) s.AttachedToEntityId = ReadUnsignedVarLong();

				s.PayloadType = (int) ReadUnsignedVarInt();
				switch (s.PayloadType)
				{
					case 1:
						if (ReadBool()) s.ArrowLineEndLocation = ReadVector3();
						if (ReadBool()) s.ArrowHeadLength = ReadFloat();
						if (ReadBool()) s.ArrowHeadRadius = ReadFloat();
						if (ReadBool()) s.ArrowSegments = ReadByte();
						break;
					case 2:
						s.Text = ReadString();
						s.TextUseRotation = ReadBool();
						if (ReadBool()) s.TextBackgroundColor = ReadUint();
						s.TextDepthTest = ReadBool();
						s.TextShowBackface = ReadBool();
						s.TextShowTextBackface = ReadBool();
						break;
					case 3:
						s.BoxBound = ReadVector3();
						break;
					case 4:
						s.LineEndLocation = ReadVector3();
						break;
					case 5:
						s.CircleOrSphereSegments = ReadByte();
						break;
					case 6:
						s.CylinderRadiusX = ReadVector2();
						s.CylinderRadiusZ = ReadVector2();
						s.CylinderHeight = ReadFloat();
						s.CylinderSegments = ReadByte();
						break;
					case 7:
						s.PyramidWidth = ReadFloat();
						if (ReadBool()) s.PyramidDepth = ReadFloat();
						s.PyramidHeight = ReadFloat();
						break;
					case 8:
						s.EllipsoidRadii = ReadVector3();
						s.EllipsoidSegmentsPerAxis = ReadByte();
						break;
					case 9:
						s.ConeRadii = ReadVector2();
						s.ConeHeight = ReadFloat();
						s.ConeSegments = ReadByte();
						break;
				}

				Shapes.Add(s);
			}
		}
	}
}
