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
using MiNET.Entities;
using MiNET.Entities.Hostile;
using MiNET.Utils.Metadata;
using MiNET.Worlds;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test
{
	/// <summary>
	///     Skeleton metadata wire shape vs the vanilla BDS capture. The bow draw is driven by
	///     TARGET_EID (metadata key 6, the client's query.has_target) plus the
	///     FACING_TARGET_TO_RANGE_ATTACK extended flag (bit 88, second flags long key 92), both
	///     set at target acquisition. The per-shot capture's bit 34 is MOVING (PMMP, ViaBedrock/
	///     Mojang docs, minecraft-data and Cloudburst all agree): vanilla skeletons strafe while
	///     shooting, so the bit follows the movement. It is NOT a bow-animation trigger. The
	///     old "bit 34 = JUMPING" claim confused PlayerAuthInputFlags with the entity flags, and
	///     the bit-34 regression (bubbles + flying on a real client) was caused by making
	///     Charged an explicit 34 in the enum, which shifted every later flag by +7.
	/// </summary>
	[TestClass]
	public class MobAnimationMetadataTests
	{
		// Vanilla BDS skeleton resting flags (capture): CAN_CLIMB(19), WALKER(22), BREATHING(35),
		// HAS_COLLISION(48), HAS_GRAVITY(49) = 0x3000800480000.
		private const long VanillaBaselineFlags =
			(1L << 19) | (1L << 22) | (1L << 35) | (1L << 48) | (1L << 49);

		[TestMethod]
		public void Skeleton_baseline_flags_match_vanilla()
		{
			Skeleton skeleton = new Skeleton(CreateFlatLevel());

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.AreEqual(VanillaBaselineFlags, flags,
				"an idle skeleton must carry exactly the vanilla baseline flag set");
		}

		[TestMethod]
		public void Skeleton_never_emits_the_angry_flag_when_targeting()
		{
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			Entity target = new Entity(EntityType.Player, skeleton.Level);
			target.EntityId = 12345;

			skeleton.SetTarget(target);

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.IsTrue((flags & (1L << (int) Entity.DataFlags.Angry)) == 0,
				"a vanilla skeleton never carries the angry flag, even while hostile");
		}

		[TestMethod]
		public void Skeleton_with_target_emits_target_eid_metadata()
		{
			// The client's query.has_target (which drives the bow draw) reads metadata key 6,
			// TARGET_EID - the runtime id of the mob's attack target.
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			Entity target = new Entity(EntityType.Player, skeleton.Level);
			target.EntityId = 12345;

			skeleton.SetTarget(target);

			MetadataLong targetEid = skeleton.GetMetadata()[6] as MetadataLong;
			Assert.IsNotNull(targetEid, "a targeting skeleton must emit TARGET_EID (key 6)");
			Assert.AreEqual(12345, targetEid.Value, "TARGET_EID must carry the target's runtime entity id");
		}

		[TestMethod]
		public void Skeleton_with_target_sets_range_attack_flag_in_extended_flags()
		{
			// The client's skeleton.attack controller plays the melee swing only when the mob has
			// a target AND FACING_TARGET_TO_RANGE_ATTACK (bit 88, second flags long key 92) is
			// unset - a bow user must carry it or the client plays the arm swing.
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			Entity target = new Entity(EntityType.Player, skeleton.Level);
			target.EntityId = 12345;

			skeleton.SetTarget(target);

			MetadataLong flags2 = skeleton.GetMetadata()[(int) Entity.MetadataFlags.EntityFlags2] as MetadataLong;
			Assert.IsNotNull(flags2, "the extended flags long (key 92) must be present");
			long bit = 1L << ((int) Entity.DataFlags.FacingTargetToRangeAttack - 64);
			Assert.IsTrue((flags2.Value & bit) != 0,
				"a ranged-attack skeleton must carry FACING_TARGET_TO_RANGE_ATTACK in the second flags long");
		}

		[TestMethod]
		public void Skeleton_without_target_clears_range_attack_flag()
		{
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			Entity target = new Entity(EntityType.Player, skeleton.Level);
			target.EntityId = 12345;
			skeleton.SetTarget(target);
			skeleton.SetTarget(null);

			MetadataLong flags2 = skeleton.GetMetadata()[(int) Entity.MetadataFlags.EntityFlags2] as MetadataLong;
			Assert.IsNotNull(flags2, "the extended flags long (key 92) must be present");
			long bit = 1L << ((int) Entity.DataFlags.FacingTargetToRangeAttack - 64);
			Assert.IsTrue((flags2.Value & bit) == 0,
				"a skeleton without a target must not carry the range-attack flag");
		}

		[TestMethod]
		public void Charging_skeleton_does_not_set_the_moving_flag()
		{
			// Bit 34 is MOVING (all four authorities agree), not a draw trigger: the vanilla
			// capture shows it toggling only because vanilla skeletons strafe while shooting.
			// It must come from actual movement (IsMoving), never from the charge path - the
			// original bit-34 regression (bubbles + flying) was caused by making Charged an
			// explicit 34 in the enum, which shifted every later flag by +7.
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			skeleton.IsCharged = true;

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.IsTrue((flags & (1L << 34)) == 0, "the charge path must never set the moving flag");
		}

		[TestMethod]
		public void Charged_flag_still_maps_to_its_positional_bit_when_set()
		{
			// The skeleton's bow path never sets it (vanilla toggles no flag around the shot),
			// but the property plumbing stays: charged creepers use the same bit.
			Skeleton skeleton = new Skeleton(CreateFlatLevel());
			skeleton.IsCharged = true;

			long flags = ReadFlags(skeleton.GetMetadata());
			Assert.IsTrue((flags & (1L << (int) Entity.DataFlags.Charged)) != 0,
				"the charged flag must still land on its positional bit");
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
