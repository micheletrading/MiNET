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

using System.Numerics;
using MiNET.Utils.Vectors;

namespace MiNET.Net
{
	/// <summary>
	///     The StructureSettings struct carried by MCPE_STRUCTURE_TEMPLATE_DATA_EXPORT_REQUEST.
	///     Field order verified against PMMP CommonTypes::getStructureSettings/putStructureSettings
	///     and minecraft-data's StructureBlockSettings type.
	/// </summary>
	public class StructureSettings
	{
		public string PaletteName { get; set; }
		public bool IgnoreEntities { get; set; }
		public bool IgnoreBlocks { get; set; }
		public bool AllowNonTickingChunks { get; set; }
		public BlockCoordinates Size { get; set; }
		public BlockCoordinates Offset { get; set; }
		public long LastEditingPlayerUniqueId { get; set; }
		public byte Rotation { get; set; }
		public byte Mirror { get; set; }
		public byte AnimationMode { get; set; }
		public float AnimationSeconds { get; set; }
		public float IntegrityValue { get; set; }
		public uint IntegritySeed { get; set; }
		public Vector3 Pivot { get; set; }
	}
}
