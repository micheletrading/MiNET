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
using MiNET.Utils.Vectors;

namespace MiNET.Net
{
	public partial class McpeNetworkChunkPublisherUpdate
	{
		/// <summary>
		///     "Server Built Chunks List": the trailing field the XML pdu never modeled. The count is a
		///     raw le32 ("No size compression" in the schema), each entry a ChunkPos (two zigzag
		///     varints). MiNET does not track server-built chunks, so it sends the list it has: empty
		///     unless a caller fills this.
		/// </summary>
		public List<ChunkCoordinates> serverBuiltChunks;

		partial void AfterEncode()
		{
			Write(serverBuiltChunks?.Count ?? 0);
			if (serverBuiltChunks != null)
			{
				foreach (ChunkCoordinates chunk in serverBuiltChunks)
				{
					WriteSignedVarInt(chunk.X);
					WriteSignedVarInt(chunk.Z);
				}
			}
		}

		partial void AfterDecode()
		{
			int count = ReadInt();
			serverBuiltChunks = new List<ChunkCoordinates>(count);
			for (int i = 0; i < count; i++)
			{
				serverBuiltChunks.Add(new ChunkCoordinates(ReadSignedVarInt(), ReadSignedVarInt()));
			}
		}

		public override void Reset()
		{
			serverBuiltChunks = null;
			base.Reset();
		}
	}
}
