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
	public partial class McpeContainerRegistryCleanup : Packet<McpeContainerRegistryCleanup>
	{
		// FullContainerName: container id (byte) + optional dynamic container id (bool + lu32).
		// Same shape as StackRequestSlotInfo's container id handling in Packet.cs.
		public List<(byte ContainerId, uint? DynamicId)> RemovedContainers { get; set; } = new List<(byte, uint?)>();

		partial void AfterDecode()
		{
			int count = (int) ReadUnsignedVarInt();
			RemovedContainers = new List<(byte, uint?)>(count);
			for (int i = 0; i < count; i++)
			{
				byte containerId = (byte) ReadByte();
				uint? dynamicId = ReadBool() ? (uint?) ReadUint() : null;
				RemovedContainers.Add((containerId, dynamicId));
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) RemovedContainers.Count);
			foreach (var container in RemovedContainers)
			{
				Write(container.ContainerId);
				Write(container.DynamicId.HasValue);
				if (container.DynamicId.HasValue) Write(container.DynamicId.Value);
			}
		}
	}
}
