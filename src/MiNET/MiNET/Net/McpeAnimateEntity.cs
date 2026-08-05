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

using System;

namespace MiNET.Net
{
	/// <summary>
	///     The trailing runtime entity id list, which the generated code cannot express: a
	///     VarInt-count-prefixed array of UnsignedVarLong runtime entity ids, following the fields
	///     declared in the XML. Field order and types are from Mojang's AnimateEntityPacket schema
	///     (mRuntimeIds at ordinal 6).
	/// </summary>
	public partial class McpeAnimateEntity
	{
		public long[] runtimeEntityIds = Array.Empty<long>();

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) runtimeEntityIds.Length);
			foreach (long id in runtimeEntityIds) WriteUnsignedVarLong(id);
		}

		partial void AfterDecode()
		{
			var count = ReadUnsignedVarInt();
			runtimeEntityIds = new long[count];
			for (int i = 0; i < count; i++) runtimeEntityIds[i] = ReadUnsignedVarLong();
		}
	}
}
