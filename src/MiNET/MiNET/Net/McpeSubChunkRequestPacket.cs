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

namespace MiNET.Net
{
	public partial class McpeSubChunkRequestPacket : Packet<McpeSubChunkRequestPacket>
	{
		public int dimension;

		// Absolute subchunk coordinates the offsets are relative to.
		public int originX;
		public int originY;
		public int originZ;

		public List<SubChunkPosOffset> offsets = new List<SubChunkPosOffset>();

		partial void AfterEncode()
		{
			WriteSignedVarInt(dimension);

			WriteUnsignedVarInt((uint) offsets.Count);
			foreach (SubChunkPosOffset offset in offsets)
			{
				Write((byte) offset.subchunkOffsetX);
				Write((byte) offset.subchunkOffsetY);
				Write((byte) offset.subchunkOffsetZ);
			}

			// Origin is a little-endian int32 triple, unlike the response's varints.
			Write(originX);
			Write(originY);
			Write(originZ);
		}

		partial void AfterDecode()
		{
			dimension = ReadSignedVarInt();

			uint count = ReadUnsignedVarInt();
			offsets = new List<SubChunkPosOffset>((int) count);
			for (uint i = 0; i < count; i++)
			{
				offsets.Add(new SubChunkPosOffset
				{
					subchunkOffsetX = (sbyte) ReadByte(),
					subchunkOffsetY = (sbyte) ReadByte(),
					subchunkOffsetZ = (sbyte) ReadByte()
				});
			}

			originX = ReadInt();
			originY = ReadInt();
			originZ = ReadInt();
		}
	}
}
