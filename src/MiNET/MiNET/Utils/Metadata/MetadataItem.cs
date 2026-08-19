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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using System.IO;
using MiNET.Items;
using MiNET.Utils;

namespace MiNET.Utils.Metadata
{
	/// <summary>
	///     Metadata slot entry (type 6): the item an entity is holding in use. The client plays the
	///     item-use animation (e.g. the skeleton's bow draw) off this property (index 48) plus the
	///     using-item flag; the flag alone does not animate.
	/// </summary>
	public class MetadataItem : MetadataEntry
	{
		public override byte Identifier
		{
			get { return 6; }
		}

		public override string FriendlyName
		{
			get { return "Item"; }
		}

		public Item Item { get; set; }

		public MetadataItem(Item item)
		{
			Item = item;
		}

		public override void FromStream(BinaryReader reader)
		{
			throw new System.NotSupportedException("MetadataItem decode is not implemented");
		}

		public override void WriteTo(BinaryWriter stream)
		{
			// Mirrors Packet.Write(Item, writeUniqueId: false): network id, count, metadata,
			// no stack id, block runtime id, zero-length extra data. The client decodes
			// inventory items with this exact encoding, and metadata slots use the same one.
			short networkId = Item == null || Item.IsAir ? (short) 0 : ItemFactory.GetNetworkIdByName(Item.Name);
			if (networkId == 0)
			{
				stream.Write((short) 0); // network_id
				stream.Write((ushort) 0); // count
				VarInt.WriteInt32(stream.BaseStream, 0); // metadata
				stream.Write(false); // has_stack_id
				VarInt.WriteInt32(stream.BaseStream, 0); // block_runtime_id
				VarInt.WriteInt32(stream.BaseStream, 0); // extra_data length
				return;
			}

			stream.Write(networkId); // network_id
			stream.Write((ushort) Item.Count); // count
			VarInt.WriteInt32(stream.BaseStream, Item.Metadata); // metadata
			stream.Write(false); // has_stack_id
			VarInt.WriteInt32(stream.BaseStream, Item.RuntimeId); // block_runtime_id
			VarInt.WriteInt32(stream.BaseStream, 0); // extra_data length
		}
	}
}
