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

using System;
using System.IO;
using MiNET.Entities.Hostile;
using MiNET.Utils.Metadata;
using MiNET.Worlds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test
{
	/// <summary>
	///     Charged flag regression. NOTE (2026-08-18): a vanilla BDS capture toggles flag bit 34
	///     around each skeleton arrow, but emitting bit 34 on MiNET made the real client render
	///     bubbles and knock the target flying - the bit is JUMPING in the client's flag map, not
	///     the bow animation. Bit 34 is therefore NOT used; the charged flag stays at its
	///     positional bit (27) and the bow animation question is still open (the client likely
	///     animates from its own local AI with NoAi=false).
	/// </summary>
	[TestClass]
	public class MobAnimationMetadataTests
	{
		[TestMethod]
		public void Charging_skeleton_sets_the_charged_flag_at_its_positional_bit()
		{
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			skeleton.IsCharged = true;

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.IsTrue((flags & (1L << (int) MiNET.Entities.Entity.DataFlags.Charged)) != 0,
				"charging skeleton must set the charged flag bit");
		}

		[TestMethod]
		public void Idle_skeleton_clears_the_charged_flag_bit()
		{
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			skeleton.IsCharged = false;

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.IsTrue((flags & (1L << (int) MiNET.Entities.Entity.DataFlags.Charged)) == 0,
				"an idle skeleton must not carry the charged bit");
		}

		[TestMethod]
		public void Charging_skeleton_never_emits_flag_bit_34()
		{
			// Bit 34 is JUMPING in the client's flag map (PMMP EntityMetadataFlags) - it made the
			// real client render bubbles on the skeleton and launch the target; it must never be
			// set for the bow draw.
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			skeleton.IsCharged = true;

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.IsTrue((flags & (1L << 34)) == 0, "flag bit 34 (jumping) must never be emitted");
		}

		private static long ReadFlags(MetadataDictionary metadata)
		{
			MetadataLong flags = metadata[0] as MetadataLong;
			Assert.IsNotNull(flags, "metadata key 0 must be the flags long");
			return flags.Value;
		}

		private static Level CreateFlatLevel()
		{
			string dir = Path.Combine(Path.GetTempPath(), "minet-metadata-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);

			var provider = new AnvilWorldProvider(dir)
			{
				MissingChunkProvider = new SuperflatGenerator(Dimension.Overworld)
			};

			var level = new Level(new LevelManager(), "metadata-test", provider, new EntityManager(), GameMode.Survival, Difficulty.Normal, 4);
			level.Initialize();
			return level;
		}
	}
}
