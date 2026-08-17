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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using MiNET.Items;
using MiNET.Worlds;

namespace MiNET.Blocks
{
	public partial class Fire : Block
	{
		public Fire()
		{
			IsReplaceable = true;
		}

		public override Item[] GetDrops(Item tool)
		{
			return new Item[0];
		}

		/// <summary>
		///     Fire dies like vanilla: within a few dozen seconds of being placed it burns out.
		///     The burn-out is scheduled at placement (the way Button schedules its unpress)
		///     rather than driven by random ticks, which visit a given cell only about once per
		///     thirteen seconds. Honours the doFireTick game rule: with it off, the scheduled
		///     tick no-ops and the fire lasts forever.
		/// </summary>
		public override void BlockAdded(Level level)
		{
			level.ScheduleBlockTick(this, level.Random.Next(400, 800)); // 20-40 seconds
		}

		public override void OnTick(Level level, bool isRandom)
		{
			if (isRandom || !level.DoFiretick) return;

			level.SetAir(Coordinates);
			level.BroadcastSound(Coordinates, LevelSoundEventType.ExtinguishFire);
		}
	}
}