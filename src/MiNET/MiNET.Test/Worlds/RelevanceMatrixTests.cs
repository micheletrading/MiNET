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
using System.Linq;
using System.Runtime.Intrinsics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Worlds;

namespace MiNET.Test.Worlds
{
	/// <summary>
	///     The relevance matrix is the source of truth for who receives whose movement and
	///     spawn/despawn packets. A wrong bit is a player that never appears or never vanishes on
	///     someone's client, so the vector kernel is held to the scalar oracle bit for bit, and the
	///     transition stream is held to exact directed pairs.
	/// </summary>
	[TestClass]
	public class RelevanceMatrixTests
	{
		private static (RelevanceMatrix Matrix, int[] Slots) Build(float radius, (float X, float Z)[] positions, bool scalar)
		{
			var matrix = new RelevanceMatrix(radius);
			var slots = new int[positions.Length];
			for (int i = 0; i < positions.Length; i++)
			{
				slots[i] = matrix.AllocateSlot(positions[i].X, positions[i].Z);
			}
			matrix.Compute(scalar);
			return (matrix, slots);
		}

		private static void AssertMatricesEqual(RelevanceMatrix vector, RelevanceMatrix scalar, int[] slots)
		{
			Assert.AreEqual(scalar.PairCount, vector.PairCount, "pair count");
			foreach (int slot in slots)
			{
				var expected = scalar.EnumerateRow(slot).ToArray();
				var actual = vector.EnumerateRow(slot).ToArray();
				CollectionAssert.AreEqual(expected, actual, $"row {slot}");
				Assert.AreEqual(scalar.GetRowHash(slot), vector.GetRowHash(slot), $"row hash {slot}");
			}
		}

		[DataTestMethod]
		[DataRow(1)]
		[DataRow(63)]
		[DataRow(64)]
		[DataRow(65)]
		[DataRow(1000)]
		public void Vector_kernel_matches_scalar_oracle(int count)
		{
			if (!Vector256.IsHardwareAccelerated) Assert.Inconclusive("No Vector256 acceleration on this machine");

			// Random spread plus the hostile cases: exactly-on-radius, negative and huge
			// coordinates. Quantized to 0.25 so distances are exact in single precision and the
			// inclusive compare is deterministic.
			var rng = new Random(42);
			var positions = new (float X, float Z)[count];
			for (int i = 0; i < count; i++)
			{
				positions[i] = ((float) (Math.Floor(rng.NextDouble() * 3200 - 1600) * 0.25), (float) (Math.Floor(rng.NextDouble() * 3200 - 1600) * 0.25));
			}
			if (count >= 4)
			{
				positions[0] = (0, 0);
				positions[1] = (96, 0); // exactly on the radius, must be relevant
				positions[2] = (-1_000_000, -1_000_000);
				positions[3] = (1_000_000, 1_000_000);
			}

			var (vector, slots) = Build(96f, positions, scalar: false);
			var (scalar, _) = Build(96f, positions, scalar: true);

			AssertMatricesEqual(vector, scalar, slots);
			if (count >= 4)
			{
				CollectionAssert.Contains(vector.EnumerateRow(slots[0]).ToArray(), slots[1], "exactly-on-radius pair missing");
			}
		}

		[TestMethod]
		public void Distance_on_the_radius_is_relevant_and_one_step_past_is_not()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			int b = matrix.AllocateSlot(96f, 0);
			matrix.Compute();

			CollectionAssert.AreEqual(new[] { b }, matrix.EnumerateRow(a).ToArray());

			matrix.SetPosition(b, 96.25f, 0);
			matrix.Compute();
			Assert.AreEqual(0, matrix.EnumerateRow(a).Count());
		}

		[TestMethod]
		public void Diagonal_is_never_set()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(10, 10);
			matrix.Compute();

			Assert.AreEqual(0, matrix.EnumerateRow(a).Count());
			Assert.AreEqual(0, matrix.PairCount);
		}

		[TestMethod]
		public void Boundary_crossing_emits_exactly_one_symmetric_transition_pair()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			int b = matrix.AllocateSlot(200, 0);
			matrix.Compute();
			matrix.EnumerateTransitions().ToArray(); // drain the initial state

			// B walks into range: exactly two directed entered events, one per viewer.
			matrix.SetPosition(b, 50, 0);
			matrix.Compute();
			var entered = matrix.EnumerateTransitions().ToArray();
			CollectionAssert.AreEquivalent(new[] { (a, b, true), (b, a, true) }, entered);

			// And back out: the same pair, left.
			matrix.SetPosition(b, 200, 0);
			matrix.Compute();
			var left = matrix.EnumerateTransitions().ToArray();
			CollectionAssert.AreEquivalent(new[] { (a, b, false), (b, a, false) }, left);
		}

		[TestMethod]
		public void First_compute_emits_entered_for_every_pair_in_range()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			int b = matrix.AllocateSlot(10, 0);
			int c = matrix.AllocateSlot(500, 0);
			matrix.Compute();

			var transitions = matrix.EnumerateTransitions().ToArray();
			CollectionAssert.AreEquivalent(new[] { (a, b, true), (b, a, true) }, transitions);
			Assert.AreEqual(0, matrix.EnumerateRow(c).Count());
		}

		[TestMethod]
		public void Unmoved_players_emit_no_transitions()
		{
			var matrix = new RelevanceMatrix(96f);
			matrix.AllocateSlot(0, 0);
			matrix.AllocateSlot(10, 0);
			matrix.Compute();
			matrix.EnumerateTransitions().ToArray();

			matrix.Compute();
			Assert.AreEqual(0, matrix.EnumerateTransitions().Count());
		}

		[TestMethod]
		public void Recycled_slot_carries_no_ghost_transitions()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			int b = matrix.AllocateSlot(10, 0); // in range of a
			matrix.Compute();
			matrix.EnumerateTransitions().ToArray();

			matrix.FreeSlot(b);
			int c = matrix.AllocateSlot(10, 0); // recycles b's slot, same position
			Assert.AreEqual(b, c, "free list should recycle the slot");
			matrix.Compute();

			// The new occupant must announce itself as entered even though the old occupant
			// stood on the same spot: the scrub on free has to reach both buffers.
			var transitions = matrix.EnumerateTransitions().ToArray();
			CollectionAssert.AreEquivalent(new[] { (a, c, true), (c, a, true) }, transitions);
		}

		[TestMethod]
		public void Freed_slot_leaves_no_bits_in_any_row()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			int b = matrix.AllocateSlot(10, 0);
			matrix.Compute();
			Assert.AreEqual(2, matrix.PairCount);

			matrix.FreeSlot(b);
			Assert.AreEqual(0, matrix.EnumerateRow(a).Count());
			Assert.IsFalse(matrix.IsLive(b));

			matrix.Compute();
			Assert.AreEqual(0, matrix.PairCount);
			Assert.AreEqual(0, matrix.EnumerateTransitions().Count());
		}

		[TestMethod]
		public void Identical_rows_hash_identically_and_different_rows_do_not()
		{
			// Two viewers outside each other's radius watching the same target have identical
			// rows (the diagonal exclusion means mutually visible viewers never can): those must
			// group. A viewer seeing nothing must not.
			var matrix = new RelevanceMatrix(10f);
			int v1 = matrix.AllocateSlot(8, 0);
			int v2 = matrix.AllocateSlot(-8, 0); // 16 apart: v1 and v2 do not see each other
			int target = matrix.AllocateSlot(0, 0); // but both see the target
			int loner = matrix.AllocateSlot(5000, 5000);
			matrix.Compute();

			CollectionAssert.AreEqual(new[] { target }, matrix.EnumerateRow(v1).ToArray());
			CollectionAssert.AreEqual(new[] { target }, matrix.EnumerateRow(v2).ToArray());
			Assert.AreEqual(matrix.GetRowHash(v1), matrix.GetRowHash(v2), "same visible set must group");
			Assert.AreNotEqual(matrix.GetRowHash(v1), matrix.GetRowHash(loner), "different visible sets must not group");
		}

		[TestMethod]
		public void Row_plus_self_hash_unifies_a_mutually_visible_cluster()
		{
			// Plain row hashes differ inside a cluster (each row is missing its owner), which
			// would cost one compression per member. Row-plus-self collapses the cluster to one
			// broadcast group; that economy is what the hash exists for.
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			int b = matrix.AllocateSlot(5, 0);
			int c = matrix.AllocateSlot(0, 5);
			int loner = matrix.AllocateSlot(5000, 5000);
			matrix.Compute();

			Assert.AreNotEqual(matrix.GetRowHash(a), matrix.GetRowHash(b));
			Assert.AreEqual(matrix.GetRowHashWithSelf(a), matrix.GetRowHashWithSelf(b));
			Assert.AreEqual(matrix.GetRowHashWithSelf(b), matrix.GetRowHashWithSelf(c));
			Assert.AreNotEqual(matrix.GetRowHashWithSelf(a), matrix.GetRowHashWithSelf(loner));
		}

		[TestMethod]
		public void Growth_past_initial_capacity_preserves_rows_and_transitions()
		{
			var matrix = new RelevanceMatrix(96f, 64);
			var slots = new List<int>();
			for (int i = 0; i < 200; i++)
			{
				slots.Add(matrix.AllocateSlot(i * 10, 0)); // chain: each sees ~19 neighbours
			}
			matrix.Compute();
			matrix.EnumerateTransitions().ToArray();

			var scalar = new RelevanceMatrix(96f, 64);
			for (int i = 0; i < 200; i++)
			{
				scalar.AllocateSlot(i * 10, 0);
			}
			scalar.Compute(forceScalar: true);

			AssertMatricesEqual(matrix, scalar, slots.ToArray());

			// And the delta survives the grow: move one player, only its pairs transition.
			matrix.SetPosition(slots[0], 100_000, 0);
			matrix.Compute();
			var transitions = matrix.EnumerateTransitions().ToArray();
			Assert.IsTrue(transitions.All(t => t.Viewer == slots[0] || t.Entity == slots[0]));
			Assert.IsTrue(transitions.Length > 0);
			Assert.IsTrue(transitions.All(t => !t.Entered));
		}

		[TestMethod]
		public void Freeing_a_dead_slot_throws()
		{
			var matrix = new RelevanceMatrix(96f);
			int a = matrix.AllocateSlot(0, 0);
			matrix.FreeSlot(a);
			Assert.ThrowsExactly<InvalidOperationException>(() => matrix.FreeSlot(a));
			Assert.ThrowsExactly<InvalidOperationException>(() => matrix.SetPosition(a, 1, 1));
		}
	}
}