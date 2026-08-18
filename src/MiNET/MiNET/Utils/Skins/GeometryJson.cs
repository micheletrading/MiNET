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
using System.Globalization;
using System.Text;

namespace MiNET.Utils.Skins
{
	/// <summary>
	///     Writes a GeometryModel straight to JSON instead of going through Newtonsoft. The animated
	///     effects reserialise a model of several thousand cubes once per frame on the level tick
	///     thread, where reflection-driven serialisation costs more than half the 50ms tick budget.
	///     Output is byte-identical to the Newtonsoft path, which GeometryJsonTests asserts against
	///     the real captured geometries.
	/// </summary>
	public static class GeometryJson
	{
		/// <summary>Model units are sixteenths of a block; two decimals is finer than the client draws.</summary>
		private const int Decimals = 2;

		/// <summary>
		///     Reused rather than allocated per call: a subdivided model runs to a few hundred
		///     kilobytes and this is called once per animation frame. Thread-static because the
		///     level tick thread is not the only caller.
		/// </summary>
		[ThreadStatic] private static StringBuilder _builder;

		public static string Write(GeometryModel model)
		{
			StringBuilder json = _builder ??= new StringBuilder(256 * 1024);
			json.Clear();

			json.Append("{\"format_version\":");
			WriteString(json, model.FormatVersion);
			json.Append(",\"minecraft:geometry\":[");

			for (int i = 0; i < model.Geometry.Count; i++)
			{
				if (i > 0) json.Append(',');
				WriteGeometry(json, model.Geometry[i]);
			}

			json.Append("]}");
			return json.ToString();
		}

		private static void WriteGeometry(StringBuilder json, Geometry geometry)
		{
			json.Append('{');
			bool first = true;

			if (geometry.Description != null)
			{
				Separate(json, ref first);
				json.Append("\"description\":");
				WriteDescription(json, geometry.Description);
			}

			if (geometry.Bones != null)
			{
				Separate(json, ref first);
				json.Append("\"bones\":[");
				for (int i = 0; i < geometry.Bones.Count; i++)
				{
					if (i > 0) json.Append(',');
					WriteBone(json, geometry.Bones[i]);
				}
				json.Append(']');
			}

			WriteOptionalString(json, ref first, "META_BoneType", geometry.BoneType);
			WriteOptionalString(json, ref first, "META_ModelVersion", geometry.ModelVersion);
			WriteOptionalString(json, ref first, "rigtype", geometry.RigType);
			WriteOptionalInt(json, ref first, "texturewidth", geometry.TextureWidth);
			WriteOptionalInt(json, ref first, "textureheight", geometry.TextureHeight);

			WriteOptionalBool(json, ref first, "animationArmsDown", geometry.AnimationArmsDown);
			WriteOptionalBool(json, ref first, "animationArmsOutFront", geometry.AnimationArmsOutFront);
			WriteOptionalBool(json, ref first, "animationStatueOfLibertyArms", geometry.AnimationStatueOfLibertyArms);
			WriteOptionalBool(json, ref first, "animationSingleArmAnimation", geometry.AnimationSingleArmAnimation);
			WriteOptionalBool(json, ref first, "animationStationaryLegs", geometry.AnimationStationaryLegs);
			WriteOptionalBool(json, ref first, "animationSingleLegAnimation", geometry.AnimationSingleLegAnimation);
			WriteOptionalBool(json, ref first, "animationNoHeadBob", geometry.AnimationNoHeadBob);
			WriteOptionalBool(json, ref first, "animationDontShowArmor", geometry.AnimationDontShowArmor);
			WriteOptionalBool(json, ref first, "animationUpsideDown", geometry.AnimationUpsideDown);
			WriteOptionalBool(json, ref first, "animationInvertedCrouch", geometry.AnimationInvertedCrouch);

			json.Append('}');
		}

		private static void WriteDescription(StringBuilder json, Description description)
		{
			json.Append('{');
			bool first = true;

			WriteOptionalString(json, ref first, "identifier", description.Identifier);
			WriteOptionalInt(json, ref first, "texture_height", description.TextureHeight);
			WriteOptionalInt(json, ref first, "texture_width", description.TextureWidth);
			WriteOptionalInt(json, ref first, "visible_bounds_height", description.VisibleBoundsHeight);
			WriteOptionalIntArray(json, ref first, "visible_bounds_offset", description.VisibleBoundsOffset);
			WriteOptionalInt(json, ref first, "visible_bounds_width", description.VisibleBoundsWidth);

			json.Append('}');
		}

		private static void WriteBone(StringBuilder json, Bone bone)
		{
			json.Append('{');
			bool first = true;

			WriteOptionalString(json, ref first, "name", bone.Name);
			WriteOptionalString(json, ref first, "META_BoneType", bone.BoneType);
			WriteOptionalString(json, ref first, "material", bone.Material);
			WriteOptionalString(json, ref first, "parent", bone.Parent);
			WriteOptionalFloatArray(json, ref first, "pivot", bone.Pivot);
			WriteOptionalFloatArray(json, ref first, "pos", bone.Pos);
			WriteOptionalFloatArray(json, ref first, "rotation", bone.Rotation);

			if (bone.Cubes != null)
			{
				Separate(json, ref first);
				json.Append("\"cubes\":[");
				for (int i = 0; i < bone.Cubes.Count; i++)
				{
					if (i > 0) json.Append(',');
					WriteCube(json, bone.Cubes[i]);
				}
				json.Append(']');
			}

			WriteOptionalBool(json, ref first, "neverRender", bone.NeverRender);
			WriteOptionalBool(json, ref first, "reset", bone.Reset);
			WriteOptionalBool(json, ref first, "mirror", bone.Mirror);

			if (bone.Locators?.LeadHold != null)
			{
				Separate(json, ref first);
				json.Append("\"locators\":{\"lead_hold\":");
				WriteFloatArray(json, bone.Locators.LeadHold);
				json.Append('}');
			}

			json.Append('}');
		}

		private static void WriteCube(StringBuilder json, Cube cube)
		{
			json.Append('{');
			bool first = true;

			WriteOptionalFloatArray(json, ref first, "origin", cube.Origin);
			WriteOptionalFloatArray(json, ref first, "size", cube.Size);
			WriteOptionalFloatArray(json, ref first, "rotation", cube.Rotation);
			WriteOptionalFloatArray(json, ref first, "pivot", cube.Pivot);
			WriteOptionalFloatArray(json, ref first, "uv", cube.Uv);
			WriteOptionalFloat(json, ref first, "inflate", cube.Inflate);
			WriteOptionalBool(json, ref first, "mirror", cube.Mirror);

			json.Append('}');
		}

		private static void Separate(StringBuilder json, ref bool first)
		{
			if (!first) json.Append(',');
			first = false;
		}

		private static void WriteOptionalString(StringBuilder json, ref bool first, string name, string value)
		{
			if (value == null) return;
			Separate(json, ref first);
			json.Append('"').Append(name).Append("\":");
			WriteString(json, value);
		}

		private static void WriteOptionalInt(StringBuilder json, ref bool first, string name, int value)
		{
			if (value == 0) return;
			Separate(json, ref first);
			json.Append('"').Append(name).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
		}

		private static void WriteOptionalBool(StringBuilder json, ref bool first, string name, bool value)
		{
			if (!value) return;
			Separate(json, ref first);
			json.Append('"').Append(name).Append("\":true");
		}

		private static void WriteOptionalFloat(StringBuilder json, ref bool first, string name, float value)
		{
			if (value == 0f) return;
			Separate(json, ref first);
			json.Append('"').Append(name).Append("\":");
			WriteFloat(json, value);
		}

		private static void WriteOptionalFloatArray(StringBuilder json, ref bool first, string name, float[] values)
		{
			if (values == null) return;
			Separate(json, ref first);
			json.Append('"').Append(name).Append("\":");
			WriteFloatArray(json, values);
		}

		private static void WriteOptionalIntArray(StringBuilder json, ref bool first, string name, int[] values)
		{
			if (values == null) return;
			Separate(json, ref first);
			json.Append('"').Append(name).Append("\":[");
			for (int i = 0; i < values.Length; i++)
			{
				if (i > 0) json.Append(',');
				json.Append(values[i].ToString(CultureInfo.InvariantCulture));
			}
			json.Append(']');
		}

		private static void WriteFloatArray(StringBuilder json, IReadOnlyList<float> values)
		{
			json.Append('[');
			for (int i = 0; i < values.Count; i++)
			{
				if (i > 0) json.Append(',');
				WriteFloat(json, values[i]);
			}
			json.Append(']');
		}

		/// <summary>
		///     Two decimals means every value is a hundredth, so it can be written from an integer
		///     without going near general float formatting. A model of a few thousand cubes carries
		///     tens of thousands of numbers per frame, and ToString("R") allocates a string for each.
		/// </summary>
		private static void WriteFloat(StringBuilder json, float value)
		{
			int hundredths = (int) Math.Round(value * 100f);

			if (hundredths < 0)
			{
				json.Append('-');
				hundredths = -hundredths;
			}

			int whole = hundredths / 100;
			int fraction = hundredths % 100;

			json.Append(whole);
			if (fraction == 0) return;

			json.Append('.');
			if (fraction % 10 == 0)
			{
				json.Append((char) ('0' + fraction / 10));
				return;
			}

			json.Append((char) ('0' + fraction / 10)).Append((char) ('0' + fraction % 10));
		}

		private static void WriteString(StringBuilder json, string value)
		{
			json.Append('"');
			foreach (char c in value)
			{
				switch (c)
				{
					case '"': json.Append("\\\""); break;
					case '\\': json.Append("\\\\"); break;
					case '\n': json.Append("\\n"); break;
					case '\r': json.Append("\\r"); break;
					case '\t': json.Append("\\t"); break;
					default:
						if (c < ' ') json.Append("\\u").Append(((int) c).ToString("x4", CultureInfo.InvariantCulture));
						else json.Append(c);
						break;
				}
			}
			json.Append('"');
		}
	}
}
