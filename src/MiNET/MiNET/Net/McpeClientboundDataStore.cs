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
	// DynamicValue type tag (raw LE uint32, NOT a varint): 0=null,1=bool,2=long,3=double,4=string,5=list,6=map.
	public class DataStoreDynamicValue
	{
		public byte Type;
		public bool BoolValue;
		public long LongValue;
		public double DoubleValue;
		public string StringValue;
		public List<DataStoreDynamicValue> ListValue;
		public Dictionary<string, DataStoreDynamicValue> MapValue;
	}

	public class DataStoreOperationData
	{
		// DataStoreOperationType (UnsignedVarInt): 0=Update,1=Change,2=Removal.
		public uint OperationType;

		// Present for Update and Removal (Update also uses Property/Path); Change uses Name/Property only.
		public string Name;
		public string Property;
		public string Path;

		// Update only. ValueType (UnsignedVarInt): 0=double,1=bool,2=string.
		public byte ValueType;
		public double UpdateDoubleValue;
		public bool UpdateBoolValue;
		public string UpdateStringValue;
		public uint UpdateCount;
		public uint PathUpdateCount;

		// Change only.
		public DataStoreDynamicValue ChangeData;
	}

	public partial class McpeClientboundDataStore : Packet<McpeClientboundDataStore>
	{
		public List<DataStoreOperationData> Operations { get; set; } = new List<DataStoreOperationData>();

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Operations.Count);
			foreach (var op in Operations)
			{
				WriteUnsignedVarInt(op.OperationType);
				switch (op.OperationType)
				{
					case 0: // Update
						Write(op.Name ?? string.Empty);
						Write(op.Property ?? string.Empty);
						Write(op.Path ?? string.Empty);
						WriteUnsignedVarInt(op.ValueType);
						switch (op.ValueType)
						{
							case 0:
								Write(op.UpdateDoubleValue);
								break;
							case 1:
								Write(op.UpdateBoolValue);
								break;
							case 2:
								Write(op.UpdateStringValue ?? string.Empty);
								break;
						}
						Write(op.UpdateCount);
						Write(op.PathUpdateCount);
						break;
					case 1: // Change
						Write(op.Name ?? string.Empty);
						Write(op.Property ?? string.Empty);
						Write(op.UpdateCount);
						WriteDynamicValue(op.ChangeData);
						break;
					case 2: // Removal
						Write(op.Name ?? string.Empty);
						break;
				}
			}
		}

		partial void AfterDecode()
		{
			int count = (int) ReadUnsignedVarInt();
			Operations = new List<DataStoreOperationData>(count);
			for (int i = 0; i < count; i++)
			{
				var op = new DataStoreOperationData {OperationType = ReadUnsignedVarInt()};
				switch (op.OperationType)
				{
					case 0: // Update
						op.Name = ReadString();
						op.Property = ReadString();
						op.Path = ReadString();
						op.ValueType = (byte) ReadUnsignedVarInt();
						switch (op.ValueType)
						{
							case 0:
								op.UpdateDoubleValue = ReadDouble();
								break;
							case 1:
								op.UpdateBoolValue = ReadBool();
								break;
							case 2:
								op.UpdateStringValue = ReadString();
								break;
						}
						op.UpdateCount = ReadUint();
						op.PathUpdateCount = ReadUint();
						break;
					case 1: // Change
						op.Name = ReadString();
						op.Property = ReadString();
						op.UpdateCount = ReadUint();
						op.ChangeData = ReadDynamicValue();
						break;
					case 2: // Removal
						op.Name = ReadString();
						break;
				}

				Operations.Add(op);
			}
		}

		private void WriteDynamicValue(DataStoreDynamicValue value)
		{
			uint type = value?.Type ?? 0;
			Write(type);
			switch (type)
			{
				case 1:
					Write(value.BoolValue);
					break;
				case 2:
					WriteLe(value.LongValue);
					break;
				case 3:
					Write(value.DoubleValue);
					break;
				case 4:
					Write(value.StringValue ?? string.Empty);
					break;
				case 5:
					WriteUnsignedVarInt((uint) (value.ListValue?.Count ?? 0));
					if (value.ListValue != null)
					{
						foreach (var item in value.ListValue) WriteDynamicValue(item);
					}
					break;
				case 6:
					WriteUnsignedVarInt((uint) (value.MapValue?.Count ?? 0));
					if (value.MapValue != null)
					{
						foreach (var kvp in value.MapValue)
						{
							Write(kvp.Key);
							WriteDynamicValue(kvp.Value);
						}
					}
					break;
			}
		}

		private DataStoreDynamicValue ReadDynamicValue()
		{
			uint type = ReadUint();
			if (type == 0) return null;

			var value = new DataStoreDynamicValue {Type = (byte) type};
			switch (type)
			{
				case 1:
					value.BoolValue = ReadBool();
					break;
				case 2:
					value.LongValue = ReadLeLong();
					break;
				case 3:
					value.DoubleValue = ReadDouble();
					break;
				case 4:
					value.StringValue = ReadString();
					break;
				case 5:
				{
					int listCount = (int) ReadUnsignedVarInt();
					value.ListValue = new List<DataStoreDynamicValue>(listCount);
					for (int i = 0; i < listCount; i++) value.ListValue.Add(ReadDynamicValue());
					break;
				}
				case 6:
				{
					int mapCount = (int) ReadUnsignedVarInt();
					value.MapValue = new Dictionary<string, DataStoreDynamicValue>(mapCount);
					for (int i = 0; i < mapCount; i++)
					{
						string key = ReadString();
						value.MapValue[key] = ReadDynamicValue();
					}
					break;
				}
			}
			return value;
		}
	}
}
