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
	public partial class McpeModalFormResponse : Packet<McpeModalFormResponse>
	{
		// Both fields are wire-optional, each gated by its own leading bool.
		public string data; // present when the player submitted the form
		public byte? cancelReason; // present when the player cancelled it instead; 0 = closed, 1 = busy

		partial void AfterEncode()
		{
			bool hasData = data != null;
			Write(hasData);
			if (hasData) Write(data);

			bool hasCancelReason = cancelReason.HasValue;
			Write(hasCancelReason);
			if (hasCancelReason) Write(cancelReason.Value);
		}

		partial void AfterDecode()
		{
			if (ReadBool()) data = ReadString();
			if (ReadBool()) cancelReason = ReadByte();
		}
	}
}
