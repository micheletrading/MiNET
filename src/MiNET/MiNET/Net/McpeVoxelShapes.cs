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
	public class VoxelCells
	{
		public byte XSize { get; set; }
		public byte YSize { get; set; }
		public byte ZSize { get; set; }
		public List<byte> Storage { get; set; } = new List<byte>();
	}

	public class VoxelShape
	{
		// A single cell grid per shape (not an array) - confirmed against Mojang's
		// bedrock-protocol-docs VoxelShapes::SerializableVoxelShape (protocol 2169).
		public VoxelCells Cells { get; set; } = new VoxelCells();
		public List<float> XCoordinates { get; set; } = new List<float>();
		public List<float> YCoordinates { get; set; } = new List<float>();
		public List<float> ZCoordinates { get; set; } = new List<float>();
	}

	public class VoxelShapeNameEntry
	{
		public string Name { get; set; }
		public ushort Id { get; set; }
	}

	public partial class McpeVoxelShapes : Packet<McpeVoxelShapes>
	{
		public List<VoxelShape> Shapes { get; set; } = new List<VoxelShape>();
		public List<VoxelShapeNameEntry> NameMap { get; set; } = new List<VoxelShapeNameEntry>();
		public ushort CustomShapeCount { get; set; }

		partial void AfterDecode()
		{
			uint shapeCount = ReadUnsignedVarInt();
			for (int i = 0; i < shapeCount; i++)
			{
				var shape = new VoxelShape
				{
					Cells = new VoxelCells
					{
						XSize = ReadByte(),
						YSize = ReadByte(),
						ZSize = ReadByte(),
					}
				};

				uint storageCount = ReadUnsignedVarInt();
				for (int k = 0; k < storageCount; k++) shape.Cells.Storage.Add(ReadByte());

				uint xCount = ReadUnsignedVarInt();
				for (int j = 0; j < xCount; j++) shape.XCoordinates.Add(ReadFloat());

				uint yCount = ReadUnsignedVarInt();
				for (int j = 0; j < yCount; j++) shape.YCoordinates.Add(ReadFloat());

				uint zCount = ReadUnsignedVarInt();
				for (int j = 0; j < zCount; j++) shape.ZCoordinates.Add(ReadFloat());

				Shapes.Add(shape);
			}

			uint nameCount = ReadUnsignedVarInt();
			for (int i = 0; i < nameCount; i++)
			{
				NameMap.Add(new VoxelShapeNameEntry {Name = ReadString(), Id = ReadUshort()});
			}

			CustomShapeCount = ReadUshort();
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Shapes.Count);
			foreach (var shape in Shapes)
			{
				Write(shape.Cells.XSize);
				Write(shape.Cells.YSize);
				Write(shape.Cells.ZSize);

				WriteUnsignedVarInt((uint) shape.Cells.Storage.Count);
				foreach (var b in shape.Cells.Storage) Write(b);

				WriteUnsignedVarInt((uint) shape.XCoordinates.Count);
				foreach (var x in shape.XCoordinates) Write(x);

				WriteUnsignedVarInt((uint) shape.YCoordinates.Count);
				foreach (var y in shape.YCoordinates) Write(y);

				WriteUnsignedVarInt((uint) shape.ZCoordinates.Count);
				foreach (var z in shape.ZCoordinates) Write(z);
			}

			WriteUnsignedVarInt((uint) NameMap.Count);
			foreach (var entry in NameMap)
			{
				Write(entry.Name);
				Write(entry.Id);
			}

			Write(CustomShapeCount);
		}
	}
}
