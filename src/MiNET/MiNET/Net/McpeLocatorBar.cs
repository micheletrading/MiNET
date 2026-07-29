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
using System.Numerics;
using MiNET.Utils;

namespace MiNET.Net
{
	public class WaypointWorldPosition
	{
		public Vector3 Position { get; set; }
		public int DimensionId { get; set; }
	}

	public class Waypoint
	{
		public uint UpdateFlags { get; set; }
		public bool? Visible { get; set; }
		public WaypointWorldPosition WorldPosition { get; set; }
		public string TexturePath { get; set; }
		public Vector2? IconSize { get; set; }
		public int? Color { get; set; }
		public bool? ClientPositionAuthority { get; set; }
		public long? ActorUniqueId { get; set; }
	}

	public class LocatorBarWaypoint
	{
		public UUID GroupHandle { get; set; }
		public Waypoint Waypoint { get; set; } = new Waypoint();
		public byte Action { get; set; }
	}

	public partial class McpeLocatorBar : Packet<McpeLocatorBar>
	{
		public List<LocatorBarWaypoint> Waypoints { get; set; } = new List<LocatorBarWaypoint>();

		private Waypoint ReadWaypoint()
		{
			var waypoint = new Waypoint {UpdateFlags = ReadUint()};

			if (ReadBool()) waypoint.Visible = ReadBool();
			if (ReadBool())
			{
				waypoint.WorldPosition = new WaypointWorldPosition
				{
					Position = ReadVector3(),
					DimensionId = ReadSignedVarInt(),
				};
			}
			if (ReadBool()) waypoint.TexturePath = ReadString();
			if (ReadBool()) waypoint.IconSize = ReadVector2();
			if (ReadBool()) waypoint.Color = ReadInt();
			if (ReadBool()) waypoint.ClientPositionAuthority = ReadBool();
			if (ReadBool()) waypoint.ActorUniqueId = ReadSignedVarLong();

			return waypoint;
		}

		private void WriteWaypoint(Waypoint waypoint)
		{
			Write(waypoint.UpdateFlags);

			Write(waypoint.Visible.HasValue);
			if (waypoint.Visible.HasValue) Write(waypoint.Visible.Value);

			Write(waypoint.WorldPosition != null);
			if (waypoint.WorldPosition != null)
			{
				Write(waypoint.WorldPosition.Position);
				WriteSignedVarInt(waypoint.WorldPosition.DimensionId);
			}

			Write(waypoint.TexturePath != null);
			if (waypoint.TexturePath != null) Write(waypoint.TexturePath);

			Write(waypoint.IconSize.HasValue);
			if (waypoint.IconSize.HasValue) Write(waypoint.IconSize.Value);

			Write(waypoint.Color.HasValue);
			if (waypoint.Color.HasValue) Write(waypoint.Color.Value);

			Write(waypoint.ClientPositionAuthority.HasValue);
			if (waypoint.ClientPositionAuthority.HasValue) Write(waypoint.ClientPositionAuthority.Value);

			Write(waypoint.ActorUniqueId.HasValue);
			if (waypoint.ActorUniqueId.HasValue) WriteSignedVarLong(waypoint.ActorUniqueId.Value);
		}

		partial void AfterDecode()
		{
			uint count = ReadUnsignedVarInt();
			for (int i = 0; i < count; i++)
			{
				Waypoints.Add(new LocatorBarWaypoint
				{
					GroupHandle = ReadUUID(),
					Waypoint = ReadWaypoint(),
					Action = ReadByte(),
				});
			}
		}

		partial void AfterEncode()
		{
			WriteUnsignedVarInt((uint) Waypoints.Count);
			foreach (var waypoint in Waypoints)
			{
				Write(waypoint.GroupHandle);
				WriteWaypoint(waypoint.Waypoint);
				Write(waypoint.Action);
			}
		}
	}
}
