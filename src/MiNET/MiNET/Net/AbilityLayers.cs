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
using System.Collections.Generic;

namespace MiNET.Net
{
	// Shared by MCPE_ADD_PLAYER and MCPE_UPDATE_ABILITIES (protocol 1001 / 1.26.34).

	[Flags]
	public enum AbilitySet : uint
	{
		Build = 1u << 0,
		Mine = 1u << 1,
		DoorsAndSwitches = 1u << 2,
		OpenContainers = 1u << 3,
		AttackPlayers = 1u << 4,
		AttackMobs = 1u << 5,
		OperatorCommands = 1u << 6,
		Teleport = 1u << 7,
		Invulnerable = 1u << 8,
		Flying = 1u << 9,
		MayFly = 1u << 10,
		InstantBuild = 1u << 11,
		Lightning = 1u << 12,
		FlySpeed = 1u << 13,
		WalkSpeed = 1u << 14,
		Muted = 1u << 15,
		WorldBuilder = 1u << 16,
		NoClip = 1u << 17,
		PrivilegedBuilder = 1u << 18,
		VerticalFlySpeed = 1u << 19,
	}

	public enum AbilityLayerType : ushort
	{
		Cache = 0,
		Base = 1,
		Spectator = 2,
		Commands = 3,
		Editor = 4,
		LoadingScreen = 5,
	}

	public class AbilityLayer
	{
		public AbilityLayerType Type { get; set; }
		public AbilitySet Allowed { get; set; }
		public AbilitySet Enabled { get; set; }
		public float FlySpeed { get; set; }
		public float VerticalFlySpeed { get; set; }
		public float WalkSpeed { get; set; }
	}

	public abstract partial class Packet
	{
		public List<AbilityLayer> ReadAbilityLayers()
		{
			var layers = new List<AbilityLayer>();

			byte count = ReadByte();
			for (int i = 0; i < count; i++)
			{
				layers.Add(new AbilityLayer
				{
					Type = (AbilityLayerType) ReadUshort(),
					Allowed = (AbilitySet) ReadUint(),
					Enabled = (AbilitySet) ReadUint(),
					FlySpeed = ReadFloat(),
					VerticalFlySpeed = ReadFloat(),
					WalkSpeed = ReadFloat(),
				});
			}

			return layers;
		}

		public void Write(List<AbilityLayer> layers)
		{
			layers ??= new List<AbilityLayer>();

			Write((byte) layers.Count);
			foreach (var layer in layers)
			{
				Write((ushort) layer.Type);
				Write((uint) layer.Allowed);
				Write((uint) layer.Enabled);
				Write(layer.FlySpeed);
				Write(layer.VerticalFlySpeed);
				Write(layer.WalkSpeed);
			}
		}
	}
}
