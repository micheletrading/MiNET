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

using System.Collections.Generic;
using MiNET.Utils;

namespace MiNET.Net
{
	public partial class McpeAddEntity : Packet<McpeAddEntity>
	{
		// PropertySyncData (protocol 557+) between metadata and links. MiNET does not use actor
		// properties yet, but preserves them verbatim so a decode->encode round trip is byte-exact
		// instead of silently dropping real data BDS sends.
		public List<(uint index, int value)> intProperties = new List<(uint, int)>();
		public List<(uint index, float value)> floatProperties = new List<(uint, float)>();
		public EntityLinks links;

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) intProperties.Count);
			foreach (var (index, value) in intProperties)
			{
				WriteUnsignedVarInt(index);
				WriteSignedVarInt(value);
			}

			WriteUnsignedVarInt((uint) floatProperties.Count);
			foreach (var (index, value) in floatProperties)
			{
				WriteUnsignedVarInt(index);
				Write(value);
			}

			Write(links);
		}

		partial void AfterDecode()
		{
			// Fresh lists rather than appending to the field initializer's instance: packets are
			// pooled and reused (see ObjectPool/CreateObject), and ResetPacket has no partial hook
			// to clear these between checkouts.
			intProperties = new List<(uint, int)>();
			uint intPropertyCount = ReadUnsignedVarInt();
			for (uint i = 0; i < intPropertyCount; i++)
			{
				uint index = ReadUnsignedVarInt();
				int value = ReadSignedVarInt();
				intProperties.Add((index, value));
			}

			floatProperties = new List<(uint, float)>();
			uint floatPropertyCount = ReadUnsignedVarInt();
			for (uint i = 0; i < floatPropertyCount; i++)
			{
				uint index = ReadUnsignedVarInt();
				float value = ReadFloat();
				floatProperties.Add((index, value));
			}

			links = ReadEntityLinks();
		}
	}
}
