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
	///     Shaped like a slime and sized like one, but passive. It absorbs a block and takes on how
	///     that block behaves, which is what the archetype is.
	/// </summary>
	public class SulfurCube : PassiveMob
	{
		private byte _size = 2;

		/// <summary>Two only: 1 is the small cube, 2 the large one.</summary>
		public byte Size
		{
			get { return _size; }
			set
			{
				_size = value;
				Width = Height = Length = _size * 0.49;
				HealthManager.MaxHealth = _size * 40;
			}
		}

		/// <summary>
		///     Which block it absorbed, and so how it moves: bouncy, sticky, explosive, hot and the
		///     rest. Vanilla exposes this to the client as the actor property
		///     minecraft:sulfur_cube_archetype, a thirteen-value enum (see entity_properties.nbt).
		/// </summary>
		public int Archetype { get; set; }

		public SulfurCube(Level level, byte size = 2) : base(EntityType.SulfurCube, level)
		{
			Size = size;
			HealthManager.ResetHealth();
			Speed = 0.3f;
		}
	}
}
