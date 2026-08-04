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
	public class GraphicsParameterKeyframe
	{
		public float Time { get; set; }
		public Vector3 Value { get; set; }
	}

	public partial class McpeGraphicsOverrideParameter : Packet<McpeGraphicsOverrideParameter>
	{
		public List<GraphicsParameterKeyframe> Values { get; set; } = new List<GraphicsParameterKeyframe>();
		public float? FloatValue { get; set; }
		public Vector3? Vec3Value { get; set; }
		public string BiomeIdentifier { get; set; }
		public string PlayerIdentifier { get; set; }

		// GraphicsOverrideParameterType, raw byte (49 named values in PMMP 1.26.30; not modeled as an enum).
		public byte ParameterType { get; set; }
		public bool Reset { get; set; }

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Values.Count);
			foreach (var keyframe in Values)
			{
				Write(keyframe.Time);
				Write(keyframe.Value);
			}

			Write(FloatValue.HasValue);
			if (FloatValue.HasValue) Write(FloatValue.Value);

			Write(Vec3Value.HasValue);
			if (Vec3Value.HasValue) Write(Vec3Value.Value);

			Write(BiomeIdentifier ?? string.Empty);

			Write(PlayerIdentifier != null);
			if (PlayerIdentifier != null) Write(PlayerIdentifier);

			Write(ParameterType);
			Write(Reset);
		}

		partial void AfterDecode()
		{
			int count = (int) ReadUnsignedVarInt();
			Values = new List<GraphicsParameterKeyframe>(count);
			for (int i = 0; i < count; i++)
			{
				Values.Add(new GraphicsParameterKeyframe {Time = ReadFloat(), Value = ReadVector3()});
			}

			if (ReadBool()) FloatValue = ReadFloat();
			if (ReadBool()) Vec3Value = ReadVector3();
			BiomeIdentifier = ReadString();
			if (ReadBool()) PlayerIdentifier = ReadString();
			ParameterType = ReadByte();
			Reset = ReadBool();
		}
	}
}
