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

using MiNET.Utils.Nbt;

namespace MiNET.Net
{
	public partial class McpeStructureTemplateDataExportResponse : Packet<McpeStructureTemplateDataExportResponse>
	{
		// Protocol 1001: after Name comes a success bool, then the structure's NBT compound
		// (only present when success is true), then a response type byte. The success bool
		// gates the NBT so it can't be a plain XML field; handled here to keep wire order.
		// Verified against PMMP StructureTemplateDataResponsePacket and minecraft-data
		// packet_structure_template_data_export_response.
		public Nbt nbtData;
		public byte responseType;

		partial void AfterDecode()
		{
			bool success = ReadBool();
			if (success)
			{
				nbtData = ReadNbt();
			}
			responseType = ReadByte();
		}

		partial void AfterEncode()
		{
			Write(nbtData != null);
			if (nbtData != null)
			{
				Write(nbtData);
			}
			Write(responseType);
		}
	}
}
