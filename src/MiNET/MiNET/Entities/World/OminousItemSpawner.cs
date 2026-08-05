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

using MiNET.Items;
using MiNET.Worlds;

namespace MiNET.Entities.World
{
	/// <summary>
	///     What an ominous trial spawner drops its reward through: it hangs in the air holding one
	///     item, then releases it. Not a pickup itself, so it is not an <see cref="ItemEntity" />.
	/// </summary>
	public class OminousItemSpawner : Entity
	{
		public Item Item { get; set; }

		/// <summary>Ticks left before the item is released.</summary>
		public int SpawnDelay { get; set; }

		public OminousItemSpawner(Level level) : base(EntityType.OminousItemSpawner, level)
		{
			Width = Length = Height = 0.25;
			NoAi = true;
			HealthManager.IsInvulnerable = true;
		}
	}
}
