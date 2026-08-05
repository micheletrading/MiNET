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
	public class TrimPattern
	{
		public string ItemName { get; set; }
		public string Pattern { get; set; }
	}

	public class TrimMaterial
	{
		public string Material { get; set; }
		public string Color { get; set; }
		public string ItemName { get; set; }
	}

	public partial class McpeTrimData : Packet<McpeTrimData>
	{
		public List<TrimPattern> Patterns { get; set; } = new List<TrimPattern>();
		public List<TrimMaterial> Materials { get; set; } = new List<TrimMaterial>();

		partial void AfterDecode()
		{
			uint patternCount = ReadUnsignedVarInt();
			for (int i = 0; i < patternCount; i++)
			{
				Patterns.Add(new TrimPattern
				{
					ItemName = ReadString(),
					Pattern = ReadString(),
				});
			}

			uint materialCount = ReadUnsignedVarInt();
			for (int i = 0; i < materialCount; i++)
			{
				Materials.Add(new TrimMaterial
				{
					Material = ReadString(),
					Color = ReadString(),
					ItemName = ReadString(),
				});
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Patterns.Count);
			foreach (var pattern in Patterns)
			{
				Write(pattern.ItemName);
				Write(pattern.Pattern);
			}

			WriteUnsignedVarInt((uint) Materials.Count);
			foreach (var material in Materials)
			{
				Write(material.Material);
				Write(material.Color);
				Write(material.ItemName);
			}
		}
	}
}
