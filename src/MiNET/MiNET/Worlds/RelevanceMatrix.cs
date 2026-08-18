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
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.Intrinsics;

namespace MiNET.Worlds
{
	/// <summary>
	///     Pairwise relevance bit matrix for entity broadcast culling. Each occupant of the level
	///     holds a stable slot; <see cref="Compute" /> rebuilds the full N x N bit matrix where row
	///     i has bit j set when slot j is within <see cref="Radius" /> blocks (2D, x/z) of slot i.
	///     Relevance is horizontal only: both editions cull entity visibility by horizontal
	///     distance, so a tall-world vertical axis is deliberately ignored.
	///
	///     The matrix is double buffered. XOR of the two buffers is the transition stream: a bit
	///     that turned on means "entity j entered slot i's view" (spawn j to i's client), a bit
	///     that turned off means it left (despawn). The diagonal is always clear; an entity is
	///     never relevant to itself.
	///
	///     Slots are stable across ticks (a free list recycles them) so the XOR delta is
	///     meaningful, and nothing in here is player-specific: any entity can occupy a row.
	///     Every array is an ArrayPool lease, so rented buffers can run longer than the logical
	///     size: all access goes through <see cref="_capacity" /> and <see cref="_words" />, never
	///     an array's own Length. Not thread safe; the level tick thread owns it.
	/// </summary>
	public class RelevanceMatrix : IDisposable
	{
		private int _capacity; // always a multiple of 64
		private int _words; // ulong words per matrix row, _capacity / 64

		private float[] _x;
		private float[] _z;

		private ulong[] _current;
		private ulong[] _previous;
		private ulong[] _liveMask; // bit per allocated slot; padding and freed slots stay 0

		private readonly Stack<int> _freeSlots = new Stack<int>();
		private int _highWater; // slots [0.._highWater) have been handed out at least once

		/// <summary>Relevance radius in blocks. Distance exactly on the radius is relevant.</summary>
		public float Radius { get; set; }

		/// <summary>Number of live slots.</summary>
		public int Count { get; private set; }

		/// <summary>Set bits in the current matrix, counted during <see cref="Compute" />. Directed, so a mutual pair counts twice.</summary>
		public long PairCount { get; private set; }

		public RelevanceMatrix(float radius, int initialCapacity = 64)
		{
			Radius = radius;
			_capacity = Math.Max(64, (initialCapacity + 63) & ~63);
			_words = _capacity / 64;
			_x = ArrayPool<float>.Shared.Rent(_capacity);
			_z = ArrayPool<float>.Shared.Rent(_capacity);
			_current = RentCleared(_capacity * _words);
			_previous = RentCleared(_capacity * _words);
			_liveMask = RentCleared(_words);
		}

		private static ulong[] RentCleared(int length)
		{
			ulong[] array = ArrayPool<ulong>.Shared.Rent(length);
			Array.Clear(array, 0, length);
			return array;
		}

		public int AllocateSlot(float x, float z)
		{
			ObjectDisposedException.ThrowIf(_current == null, this);

			int slot;
			if (_freeSlots.Count > 0)
			{
				slot = _freeSlots.Pop();
			}
			else
			{
				if (_highWater == _capacity) Grow();
				slot = _highWater++;
			}

			_liveMask[slot >> 6] |= 1UL << (slot & 63);
			_x[slot] = x;
			_z[slot] = z;
			Count++;
			return slot;
		}

		/// <summary>
		///     Frees a slot and scrubs its row and column from BOTH buffers, so a recycled slot can
		///     never inherit transitions from its previous occupant. The caller reads the row
		///     (<see cref="EnumerateRow" />) BEFORE freeing when it needs the final audience.
		/// </summary>
		public void FreeSlot(int slot)
		{
			if (!IsLive(slot)) throw new InvalidOperationException($"Slot {slot} is not live");

			_liveMask[slot >> 6] &= ~(1UL << (slot & 63));
			Count--;

			Array.Clear(_current, slot * _words, _words);
			Array.Clear(_previous, slot * _words, _words);
			int word = slot >> 6;
			ulong clear = ~(1UL << (slot & 63));
			for (int row = 0; row < _highWater; row++)
			{
				_current[row * _words + word] &= clear;
				_previous[row * _words + word] &= clear;
			}

			_freeSlots.Push(slot);
		}

		public bool IsLive(int slot)
		{
			return slot >= 0 && slot < _highWater && (_liveMask[slot >> 6] & (1UL << (slot & 63))) != 0;
		}

		public void SetPosition(int slot, float x, float z)
		{
			if (!IsLive(slot)) throw new InvalidOperationException($"Slot {slot} is not live");
			_x[slot] = x;
			_z[slot] = z;
		}

		public void Update(ReadOnlySpan<(int Slot, float X, float Z)> positions)
		{
			foreach ((int slot, float x, float z) in positions)
			{
				SetPosition(slot, x, z);
			}
		}

		/// <summary>Whether the entity slot is currently relevant to the viewer slot.</summary>
		public bool IsRelevant(int viewer, int entity)
		{
			return (_current[viewer * _words + (entity >> 6)] & (1UL << (entity & 63))) != 0;
		}

		/// <summary>
		///     Rotates the buffers (current becomes previous) and rebuilds the current matrix from
		///     the positions as they stand. Call once per tick, after updating positions.
		/// </summary>
		public void Compute()
		{
			Compute(!Vector256.IsHardwareAccelerated);
		}

		internal void Compute(bool forceScalar)
		{
			(_current, _previous) = (_previous, _current);
			Array.Clear(_current, 0, _capacity * _words);
			PairCount = 0;

			if (Count == 0) return;

			if (forceScalar) ComputeScalar();
			else ComputeVector();
		}

		private void ComputeVector()
		{
			float r2 = Radius * Radius;
			var radiusSq = Vector256.Create(r2);
			int span = (_highWater + 63) & ~63;

			for (int i = 0; i < _highWater; i++)
			{
				if (!IsLive(i)) continue;

				var xi = Vector256.Create(_x[i]);
				var zi = Vector256.Create(_z[i]);
				int rowBase = i * _words;

				for (int j = 0; j < span; j += 64)
				{
					ulong word = 0;
					for (int k = 0; k < 64; k += 8)
					{
						var dx = Vector256.LoadUnsafe(ref _x[j + k]) - xi;
						var dz = Vector256.LoadUnsafe(ref _z[j + k]) - zi;
						var d2 = dx * dx + dz * dz;
						uint mask = Vector256.LessThanOrEqual(d2, radiusSq).ExtractMostSignificantBits();
						word |= (ulong) mask << k;
					}

					word &= _liveMask[j >> 6]; // freed and padding slots carry stale positions; never a bit
					if ((j >> 6) == (i >> 6)) word &= ~(1UL << (i & 63)); // clear the diagonal
					_current[rowBase + (j >> 6)] = word;
					PairCount += (long) ulong.PopCount(word);
				}
			}
		}

		/// <summary>Scalar fallback and the test oracle for the vector kernel. Same single-precision math, same inclusive compare.</summary>
		private void ComputeScalar()
		{
			float r2 = Radius * Radius;

			for (int i = 0; i < _highWater; i++)
			{
				if (!IsLive(i)) continue;

				float xi = _x[i];
				float zi = _z[i];
				int rowBase = i * _words;

				for (int j = 0; j < _highWater; j++)
				{
					if (j == i || !IsLive(j)) continue;

					float dx = _x[j] - xi;
					float dz = _z[j] - zi;
					float d2 = dx * dx + dz * dz;
					if (d2 <= r2)
					{
						_current[rowBase + (j >> 6)] |= 1UL << (j & 63);
						PairCount++;
					}
				}
			}
		}

		/// <summary>
		///     The spawn/despawn event stream: every bit that changed since the previous
		///     <see cref="Compute" />, directed. Entered means entity became relevant to the viewer
		///     (spawn it on the viewer's client); not entered means it left (despawn).
		/// </summary>
		public IEnumerable<(int Viewer, int Entity, bool Entered)> EnumerateTransitions()
		{
			for (int row = 0; row < _highWater; row++)
			{
				int rowBase = row * _words;
				for (int w = 0; w < _words; w++)
				{
					ulong diff = _current[rowBase + w] ^ _previous[rowBase + w];
					while (diff != 0)
					{
						int bit = System.Numerics.BitOperations.TrailingZeroCount(diff);
						diff &= diff - 1;
						int entity = (w << 6) + bit;
						bool entered = (_current[rowBase + w] & (1UL << bit)) != 0;
						yield return (row, entity, entered);
					}
				}
			}
		}

		/// <summary>Slots currently relevant to the given viewer slot.</summary>
		public IEnumerable<int> EnumerateRow(int slot)
		{
			if (!IsLive(slot)) yield break;

			int rowBase = slot * _words;
			for (int w = 0; w < _words; w++)
			{
				ulong word = _current[rowBase + w];
				while (word != 0)
				{
					int bit = System.Numerics.BitOperations.TrailingZeroCount(word);
					word &= word - 1;
					yield return (w << 6) + bit;
				}
			}
		}

		/// <summary>
		///     FNV-1a over the current row words. Two viewers with identical hashes see the same
		///     entity set, so their broadcast batches can share one compression pass.
		/// </summary>
		public ulong GetRowHash(int slot)
		{
			int rowBase = slot * _words;
			ulong hash = 14695981039346656037UL;
			for (int w = 0; w < _words; w++)
			{
				hash = (hash ^ _current[rowBase + w]) * 1099511628211UL;
			}
			return hash;
		}

		/// <summary>
		///     <see cref="GetRowHash" /> with the viewer's own bit OR'd in. The diagonal is clear
		///     in the matrix, so plain row hashes differ for every member of a mutually visible
		///     cluster (each row is missing its owner) and a tight cluster of N would degenerate
		///     into N one-member broadcast groups. Hashed over row-plus-self, the whole cluster
		///     shares one hash, one batch and one compression, at the price of each member
		///     receiving its own movement echo, which is exactly what the legacy all-to-all
		///     broadcast always did.
		/// </summary>
		public ulong GetRowHashWithSelf(int slot)
		{
			int rowBase = slot * _words;
			int selfWord = slot >> 6;
			ulong selfBit = 1UL << (slot & 63);
			ulong hash = 14695981039346656037UL;
			for (int w = 0; w < _words; w++)
			{
				ulong word = _current[rowBase + w];
				if (w == selfWord) word |= selfBit;
				hash = (hash ^ word) * 1099511628211UL;
			}
			return hash;
		}

		private void Grow()
		{
			int newCapacity = _capacity * 2;
			int newWords = newCapacity / 64;

			float[] newX = ArrayPool<float>.Shared.Rent(newCapacity);
			float[] newZ = ArrayPool<float>.Shared.Rent(newCapacity);
			Array.Copy(_x, newX, _highWater);
			Array.Copy(_z, newZ, _highWater);

			ulong[] newLiveMask = RentCleared(newWords);
			Array.Copy(_liveMask, newLiveMask, _words);

			ulong[] newCurrent = RentCleared(newCapacity * newWords);
			ulong[] newPrevious = RentCleared(newCapacity * newWords);
			for (int row = 0; row < _highWater; row++)
			{
				Array.Copy(_current, row * _words, newCurrent, row * newWords, _words);
				Array.Copy(_previous, row * _words, newPrevious, row * newWords, _words);
			}

			ArrayPool<float>.Shared.Return(_x);
			ArrayPool<float>.Shared.Return(_z);
			ArrayPool<ulong>.Shared.Return(_liveMask);
			ArrayPool<ulong>.Shared.Return(_current);
			ArrayPool<ulong>.Shared.Return(_previous);

			_x = newX;
			_z = newZ;
			_liveMask = newLiveMask;
			_current = newCurrent;
			_previous = newPrevious;
			_capacity = newCapacity;
			_words = newWords;
		}

		public void Dispose()
		{
			if (_current == null) return;

			ArrayPool<float>.Shared.Return(_x);
			ArrayPool<float>.Shared.Return(_z);
			ArrayPool<ulong>.Shared.Return(_liveMask);
			ArrayPool<ulong>.Shared.Return(_current);
			ArrayPool<ulong>.Shared.Return(_previous);
			_x = null;
			_z = null;
			_liveMask = null;
			_current = null;
			_previous = null;
			Count = 0;
			_highWater = 0;
			_freeSlots.Clear();
			GC.SuppressFinalize(this);
		}
	}
}