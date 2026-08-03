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
	///     A dead <see cref="Camel" />, same size and health. Passive itself: what makes it dangerous
	///     is the husk riding it, which does the pathfinding towards the player.
	/// </summary>
	public class CamelHusk : PassiveMob, IRideable
	{
		public Entity Rider { get; set; }

		public CamelHusk(Level level) : base(EntityType.CamelHusk, level)
		{
			Width = Length = 1.7;
			Height = 2.375;
			HealthManager.MaxHealth = 320;
			HealthManager.ResetHealth();
			Speed = 0.09f;
		}
	}
}
