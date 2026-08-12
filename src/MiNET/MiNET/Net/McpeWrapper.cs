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

using System;
using System.IO;

namespace MiNET.Net
{
	public partial class McpeWrapper : Packet<McpeWrapper>
	{
		public ReadOnlyMemory<byte> payload; // = null;

		/// <summary>
		///     Sets the payload as a view over a pooled stream and takes ownership of the stream,
		///     which returns to the pool when this wrapper does.
		/// </summary>
		public void SetPayload(MemoryStream pooledStream)
		{
			payload = pooledStream.GetBuffer().AsMemory(0, (int) pooledStream.Length);
			AttachLease(pooledStream);
		}

		public override void MaterializePooledState()
		{
			// The payload may be a view over an attached lease; own it before base disposes them.
			payload = payload.ToArray();
			base.MaterializePooledState();
		}


		partial void AfterEncode()
		{
			Write(payload);
		}

		partial void AfterDecode()
		{
			payload = ReadReadOnlyMemory(0, true);
		}
	}
}