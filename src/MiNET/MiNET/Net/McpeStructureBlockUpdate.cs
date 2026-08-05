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

using System.Numerics;
using MiNET.Utils.Vectors;

namespace MiNET.Net
{
	// Protocol 1001: no XML-expressible nested StructureSettings type, so the whole payload is
	// hand-rolled here. Verified against PMMP StructureBlockUpdatePacket / StructureEditorData /
	// StructureSettings and minecraft-data packet_structure_block_update.
	public partial class McpeStructureBlockUpdate : Packet<McpeStructureBlockUpdate>
	{
		public BlockCoordinates blockPosition;
		public string structureName;
		public string filteredStructureName;
		public string dataField;
		public bool includePlayers;
		public bool showBoundingBox;
		public int structureBlockType;

		// StructureSettings
		public string paletteName;
		public bool ignoreEntities;
		public bool ignoreBlocks;
		public bool allowNonTickingChunks;
		public BlockCoordinates size;
		public BlockCoordinates offset;
		public long lastEditingPlayerUniqueId;
		public byte rotation;
		public byte mirror;
		public byte animationMode;
		public float animationDuration;
		public float integrity;
		public uint integritySeed;
		public Vector3 pivot;

		public int redstoneSaveMode;
		public bool isPowered;
		public bool waterlogged;

		partial void AfterEncode()
		{
			Write(blockPosition);
			Write(structureName);
			Write(filteredStructureName);
			Write(dataField);
			Write(includePlayers);
			Write(showBoundingBox);
			WriteSignedVarInt(structureBlockType);

			Write(paletteName);
			Write(ignoreEntities);
			Write(ignoreBlocks);
			Write(allowNonTickingChunks);
			Write(size);
			Write(offset);
			WriteSignedVarLong(lastEditingPlayerUniqueId);
			Write(rotation);
			Write(mirror);
			Write(animationMode);
			Write(animationDuration);
			Write(integrity);
			Write(integritySeed);
			Write(pivot);

			WriteSignedVarInt(redstoneSaveMode);
			Write(isPowered);
			Write(waterlogged);
		}

		partial void AfterDecode()
		{
			blockPosition = ReadBlockCoordinates();
			structureName = ReadString();
			filteredStructureName = ReadString();
			dataField = ReadString();
			includePlayers = ReadBool();
			showBoundingBox = ReadBool();
			structureBlockType = ReadSignedVarInt();

			paletteName = ReadString();
			ignoreEntities = ReadBool();
			ignoreBlocks = ReadBool();
			allowNonTickingChunks = ReadBool();
			size = ReadBlockCoordinates();
			offset = ReadBlockCoordinates();
			lastEditingPlayerUniqueId = ReadSignedVarLong();
			rotation = ReadByte();
			mirror = ReadByte();
			animationMode = ReadByte();
			animationDuration = ReadFloat();
			integrity = ReadFloat();
			integritySeed = ReadUint();
			pivot = ReadVector3();

			redstoneSaveMode = ReadSignedVarInt();
			isPowered = ReadBool();
			waterlogged = ReadBool();
		}

		public override void Reset()
		{
			blockPosition = default;
			structureName = default;
			filteredStructureName = default;
			dataField = default;
			includePlayers = default;
			showBoundingBox = default;
			structureBlockType = default;

			paletteName = default;
			ignoreEntities = default;
			ignoreBlocks = default;
			allowNonTickingChunks = default;
			size = default;
			offset = default;
			lastEditingPlayerUniqueId = default;
			rotation = default;
			mirror = default;
			animationMode = default;
			animationDuration = default;
			integrity = default;
			integritySeed = default;
			pivot = default;

			redstoneSaveMode = default;
			isPowered = default;
			waterlogged = default;

			base.Reset();
		}
	}
}
