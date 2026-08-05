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

using MiNET.Worlds;

namespace MiNET.Entities.Passive
{
	/// <summary>
	///     The villager the game actually spawns. minecraft:villager is the pre-1.11 one, kept as a
	///     separate identity with no spawn egg; minecraft:villager_v2 carries professions, levels and
	///     the workstation trading. The "v2" is Mojang's name, not ours.
	/// </summary>
	public class VillagerV2 : PassiveMob, IAgeable
	{
		public int Profession { get; set; }
		public int TradeTier { get; set; }

		public VillagerV2(Level level) : base(EntityType.VillagerV2, level)
		{
			Width = Length = 0.6;
			Height = 1.9;
			HealthManager.MaxHealth = 200;
			HealthManager.ResetHealth();
			Speed = 0.5f;
		}
	}
}
