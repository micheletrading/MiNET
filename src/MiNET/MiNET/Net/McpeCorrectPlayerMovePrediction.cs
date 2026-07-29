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

namespace MiNET.Net
{
	public partial class McpeCorrectPlayerMovePrediction : Packet<McpeCorrectPlayerMovePrediction>
	{
		// Protocol 1001 tail after the rotation: optional vehicle angular velocity (present bool
		// + lf32, only used for prediction type 1/vehicle), on ground bool, then the client input
		// tick the correction applies to. Verified against minecraft-data 1.26.30
		// packet_correct_player_move_prediction and live BDS 1.26.34 frames (38 bytes on foot).
		public float? angularVelocity;
		public bool onGround;
		public long tick;

		partial void AfterDecode()
		{
			angularVelocity = ReadBool() ? ReadFloat() : null;
			onGround = ReadBool();
			tick = ReadUnsignedVarLong();
		}

		partial void AfterEncode()
		{
			Write(angularVelocity.HasValue);
			if (angularVelocity.HasValue)
			{
				Write(angularVelocity.Value);
			}
			Write(onGround);
			WriteUnsignedVarLong(tick);
		}
	}
}
