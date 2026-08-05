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
using log4net;
using MiNET.Utils;
using MiNET.Utils.Vectors;

namespace MiNET.Net
{
	public partial class McpeMoveEntityDelta
	{
		// Convenience state for building deltas from entity positions; the wire shape lives in
		// the generated moveData struct (per-field presence optionals since 2168, replacing the
		// old 16-bit flags header).
		public PlayerLocation currentPosition; // = null;
		public PlayerLocation prevSentPosition; // = null;
		public bool isOnGround; // = null;

		public long runtimeEntityId
		{
			get => moveData?.runtimeEntityId ?? 0;
			set => (moveData ??= new MoveActorDeltaData()).runtimeEntityId = value;
		}

		partial void BeforeEncode()
		{
			if (currentPosition == null || prevSentPosition == null) return; // decoded or prebuilt moveData

			SetFlags();
		}

		/// <summary>
		///     Builds the wire fields from currentPosition/prevSentPosition and reports whether the
		///     delta carries any change worth sending, mirroring the old flags-header contract.
		/// </summary>
		public bool SetFlags()
		{
			if (currentPosition == null || prevSentPosition == null) return false;

			long id = runtimeEntityId;
			var d = new MoveActorDeltaData {runtimeEntityId = id};
			moveData = d;

			if (currentPosition.X != 0) d.newPositionX = currentPosition.X;
			if (currentPosition.Y != 0) d.newPositionY = currentPosition.Y;
			if (currentPosition.Z != 0) d.newPositionZ = currentPosition.Z;

			float k = 256f / 360f;
			if (prevSentPosition.Pitch != currentPosition.Pitch) d.rotationX = unchecked((sbyte) (byte) Math.Round(currentPosition.Pitch * k));
			if (prevSentPosition.Yaw != currentPosition.Yaw) d.rotationY = unchecked((sbyte) (byte) Math.Round(currentPosition.Yaw * k));
			if (prevSentPosition.HeadYaw != currentPosition.HeadYaw) d.rotationYHead = unchecked((sbyte) (byte) Math.Round(currentPosition.HeadYaw * k));

			d.isOnGround = isOnGround;

			return d.newPositionX != null || d.newPositionY != null || d.newPositionZ != null
				|| d.rotationX != null || d.rotationY != null || d.rotationYHead != null;
		}

		partial void AfterDecode()
		{
			isOnGround = moveData.isOnGround;
			currentPosition = new PlayerLocation(moveData.newPositionX ?? 0, moveData.newPositionY ?? 0, moveData.newPositionZ ?? 0);
			float k = 360f / 256f;
			if (moveData.rotationX != null) currentPosition.Pitch = moveData.rotationX.Value * k;
			if (moveData.rotationY != null) currentPosition.Yaw = moveData.rotationY.Value * k;
			if (moveData.rotationYHead != null) currentPosition.HeadYaw = moveData.rotationYHead.Value * k;
		}

		public PlayerLocation GetCurrentPosition(PlayerLocation previousPosition)
		{
			var pos = previousPosition;
			float k = 360f / 256f;
			pos.X = moveData.newPositionX ?? previousPosition.X;
			pos.Y = moveData.newPositionY ?? previousPosition.Y;
			pos.Z = moveData.newPositionZ ?? previousPosition.Z;
			pos.Pitch = moveData.rotationX != null ? -(moveData.rotationX.Value * k) : previousPosition.Pitch;
			pos.Yaw = moveData.rotationY != null ? -(moveData.rotationY.Value * k) : previousPosition.Yaw;
			pos.HeadYaw = moveData.rotationYHead != null ? -(moveData.rotationYHead.Value * k) : previousPosition.HeadYaw;
			return pos;
		}
	}
}