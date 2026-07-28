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

namespace MiNET.Net
{
	public partial class McpeSetEntityData : Packet<McpeSetEntityData>
	{
		// PropertySyncData (protocol 557+) followed by tick (748+). Property values are
		// currently parsed and discarded; MiNET does not use actor properties yet.
		public long tick;

		partial void AfterEncode()
		{
			WriteUnsignedVarInt(0); // int properties
			WriteUnsignedVarInt(0); // float properties
			WriteUnsignedVarLong(tick);
		}

		partial void AfterDecode()
		{
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

			tick = ReadUnsignedVarLong();
		}
	}
}
