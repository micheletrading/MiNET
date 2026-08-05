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
using MiNET.Items;
using MiNET.Utils;
using MiNET.Utils.Metadata;

namespace MiNET.Net
{
	public partial class McpeAddPlayer : Packet<McpeAddPlayer>
	{
		// Held item uses the getItemStackWrapper ("Item", zigzag) shape, not the li16 inventory
		// descriptor - read/written via ReadItemInstance/WriteItemInstance. Held item, gamemode and
		// metadata live here (not in the XML) because the item needs the wrapper reader and all three
		// sit before the entity-properties tail.
		public Item item;
		public int gamemode;
		public MetadataDictionary metadata;

		// Entity properties (protocol 1001+), between metadata and unique id. Property values are
		// parsed and discarded; MiNET does not use actor properties yet (same approach as McpeAddEntity).
		public long uniqueId;
		public byte permissionLevel;
		public byte commandPermission;
		public List<AbilityLayer> abilities = new List<AbilityLayer>();
		public EntityLinks links;
		public string deviceId;
		public int deviceOs;

		partial void AfterEncode()
		{
			WriteItemInstance(item);
			WriteSignedVarInt(gamemode);
			Write(metadata);

			WriteUnsignedVarInt(0); // int properties
			WriteUnsignedVarInt(0); // float properties

			WriteLe(uniqueId);
			Write(permissionLevel);
			Write(commandPermission);
			Write(abilities);
			Write(links);
			Write(deviceId);
			Write(deviceOs);
		}

		partial void AfterDecode()
		{
			item = ReadItemInstance();
			gamemode = ReadSignedVarInt();
			metadata = ReadMetadataDictionary();

			uint intProperties = ReadUnsignedVarInt();
			for (uint i = 0; i < intProperties; i++)
			{
				ReadUnsignedVarInt(); // property index
				ReadSignedVarInt(); // value
			}

			uint floatProperties = ReadUnsignedVarInt();
			for (uint i = 0; i < floatProperties; i++)
			{
				ReadUnsignedVarInt(); // property index
				ReadFloat(); // value
			}

			uniqueId = ReadLongLe();
			permissionLevel = ReadByte();
			commandPermission = ReadByte();
			abilities = ReadAbilityLayers();
			links = ReadEntityLinks();
			deviceId = ReadString();
			deviceOs = ReadInt();
		}
	}
}
