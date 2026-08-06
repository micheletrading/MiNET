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

namespace MiNET.Net
{
	public partial class McpeServerBoundDiagnostics : Packet<McpeServerBoundDiagnostics>
	{
		// Client performance telemetry, sent when the client's creator diagnostics setting is
		// enabled. The four trailing arrays are parsed for wire alignment and discarded; MiNET
		// has no use for them. Layout per minecraft-data 1001 packet_serverbound_diagnostics:
		//   memory_category_values: array { category: u8, bytes: lu64 }
		//   entity_diagnostics:     array { display_name: string, entity: string, duration_nanos: lu64, percent_of_total: u8 }
		//   system_diagnostics:     array { display_name: string, system_index: lu64, duration_nanos: lu64, percent_of_total: u8 }
		//   whisker_scopes:         array { label: string, indentation: string, total_high/mid/low_cost_ns: lu64 x3 }

		partial void AfterDecode()
		{
			uint memoryCategories = ReadUnsignedVarInt();
			for (int i = 0; i < memoryCategories; i++)
			{
				ReadByte(); // category
				ReadUlong(); // bytes
			}

			uint entityDiagnostics = ReadUnsignedVarInt();
			for (int i = 0; i < entityDiagnostics; i++)
			{
				ReadString(); // display name
				ReadString(); // entity
				ReadUlong(); // duration nanos
				ReadByte(); // percent of total
			}

			uint systemDiagnostics = ReadUnsignedVarInt();
			for (int i = 0; i < systemDiagnostics; i++)
			{
				ReadString(); // display name
				ReadUlong(); // system index
				ReadUlong(); // duration nanos
				ReadByte(); // percent of total
			}

			// Entity System category-to-index mappings, added at 2168.
			uint systemCategories = ReadUnsignedVarInt();
			for (int i = 0; i < systemCategories; i++)
			{
				ReadString(); // category name
				ReadUlong(); // system index
			}

			uint whiskerScopes = ReadUnsignedVarInt();
			for (int i = 0; i < whiskerScopes; i++)
			{
				ReadString(); // label
				ReadString(); // indentation
				ReadUlong(); // total high cost ns
				ReadUlong(); // total mid cost ns
				ReadUlong(); // total low cost ns
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt(0); // memory category values
			WriteUnsignedVarInt(0); // entity diagnostics
			WriteUnsignedVarInt(0); // system diagnostics
			WriteUnsignedVarInt(0); // system categories (2168)
			WriteUnsignedVarInt(0); // whisker scopes
		}
	}
}
