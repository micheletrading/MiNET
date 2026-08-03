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
	public class HappyGhast : PassiveMob, IAgeable, IRideable
	{
		public Entity Rider { get; set; }

		/// <summary>
		///     Vanilla exposes this to the client as the actor property minecraft:can_move (see
		///     entity_properties.nbt): a harnessed happy ghast only flies while it is being steered.
		/// </summary>
		public bool CanMove { get; set; } = true;

		public HappyGhast(Level level) : base(EntityType.HappyGhast, level)
		{
			Width = Length = 4;
			Height = 4;
			HealthManager.MaxHealth = 200;
			HealthManager.ResetHealth();
			Speed = 0.3f;
			Gravity = 0;
		}
	}
}
