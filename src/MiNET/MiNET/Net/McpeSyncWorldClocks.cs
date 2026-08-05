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
	// payload_type: 0=sync_state, 1=initialize_registry, 2=add_time_marker, 3=remove_time_marker

	public class SyncWorldClockStateData
	{
		public long ClockId { get; set; }
		public int Time { get; set; }
		public bool Paused { get; set; }
	}

	public class TimeMarkerData
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public int Time { get; set; }

		// Bool-prefixed optional, plain li32 (no varint compression) when present. Verified
		// against a live BDS capture: sunrise/night/noon/midnight/day/sunset markers all
		// decode with Period=24000 (one Minecraft day), zero leftover bytes. Mojang's
		// bedrock-protocol-docs schema (protocol 2169) lists Period as "required" but that
		// does not reflect the wire's presence flag.
		public int? Period { get; set; }
	}

	public class WorldClockData
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public int Time { get; set; }
		public bool Paused { get; set; }
		public List<TimeMarkerData> TimeMarkers { get; set; } = new List<TimeMarkerData>();
	}

	public partial class McpeSyncWorldClocks : Packet<McpeSyncWorldClocks>
	{
		public List<SyncWorldClockStateData> SyncStates { get; set; } = new List<SyncWorldClockStateData>();
		public List<WorldClockData> Clocks { get; set; } = new List<WorldClockData>();
		public long AddClockId { get; set; }
		public List<TimeMarkerData> AddTimeMarkers { get; set; } = new List<TimeMarkerData>();
		public long RemoveClockId { get; set; }
		public List<long> RemoveTimeMarkerIds { get; set; } = new List<long>();

		private TimeMarkerData ReadTimeMarker()
		{
			var marker = new TimeMarkerData
			{
				Id = ReadUnsignedVarLong(),
				Name = ReadString(),
				Time = ReadSignedVarInt(),
			};

			if (ReadBool()) marker.Period = ReadInt();

			return marker;
		}

		private void WriteTimeMarker(TimeMarkerData marker)
		{
			WriteUnsignedVarLong(marker.Id);
			Write(marker.Name);
			WriteSignedVarInt(marker.Time);
			Write(marker.Period.HasValue);
			if (marker.Period.HasValue) Write(marker.Period.Value);
		}

		partial void AfterDecode()
		{
			switch (payloadType)
			{
				case 0: // sync_state
				{
					uint count = ReadUnsignedVarInt();
					for (int i = 0; i < count; i++)
					{
						SyncStates.Add(new SyncWorldClockStateData
						{
							ClockId = ReadUnsignedVarLong(),
							Time = ReadSignedVarInt(),
							Paused = ReadBool(),
						});
					}
					break;
				}
				case 1: // initialize_registry
				{
					uint count = ReadUnsignedVarInt();
					for (int i = 0; i < count; i++)
					{
						var clock = new WorldClockData
						{
							Id = ReadUnsignedVarLong(),
							Name = ReadString(),
							Time = ReadSignedVarInt(),
							Paused = ReadBool(),
						};

						uint markerCount = ReadUnsignedVarInt();
						for (int j = 0; j < markerCount; j++) clock.TimeMarkers.Add(ReadTimeMarker());

						Clocks.Add(clock);
					}
					break;
				}
				case 2: // add_time_marker
				{
					AddClockId = ReadUnsignedVarLong();
					uint count = ReadUnsignedVarInt();
					for (int i = 0; i < count; i++) AddTimeMarkers.Add(ReadTimeMarker());
					break;
				}
				case 3: // remove_time_marker
				{
					RemoveClockId = ReadUnsignedVarLong();
					uint count = ReadUnsignedVarInt();
					for (int i = 0; i < count; i++) RemoveTimeMarkerIds.Add(ReadUnsignedVarLong());
					break;
				}
			}
		}

		partial void AfterEncode()
		{
			switch (payloadType)
			{
				case 0: // sync_state
					WriteUnsignedVarInt((uint) SyncStates.Count);
					foreach (var state in SyncStates)
					{
						WriteUnsignedVarLong(state.ClockId);
						WriteSignedVarInt(state.Time);
						Write(state.Paused);
					}
					break;
				case 1: // initialize_registry
					WriteUnsignedVarInt((uint) Clocks.Count);
					foreach (var clock in Clocks)
					{
						WriteUnsignedVarLong(clock.Id);
						Write(clock.Name);
						WriteSignedVarInt(clock.Time);
						Write(clock.Paused);

						WriteUnsignedVarInt((uint) clock.TimeMarkers.Count);
						foreach (var marker in clock.TimeMarkers) WriteTimeMarker(marker);
					}
					break;
				case 2: // add_time_marker
					WriteUnsignedVarLong(AddClockId);
					WriteUnsignedVarInt((uint) AddTimeMarkers.Count);
					foreach (var marker in AddTimeMarkers) WriteTimeMarker(marker);
					break;
				case 3: // remove_time_marker
					WriteUnsignedVarLong(RemoveClockId);
					WriteUnsignedVarInt((uint) RemoveTimeMarkerIds.Count);
					foreach (var id in RemoveTimeMarkerIds) WriteUnsignedVarLong(id);
					break;
			}
		}
	}
}
