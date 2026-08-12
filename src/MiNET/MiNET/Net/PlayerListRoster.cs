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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using MiNET.Utils;

namespace MiNET.Net
{
	/// <summary>
	///     Content-addressed store for the serialized-skin section of a player-list Add record.
	///     Every player carrying the same skin shares one immutable byte array, so resident skin
	///     memory is the sum over distinct skins, not over players. Entries are refcounted by
	///     acquire/release and evicted when the last holder releases. The arrays are plain GC-owned
	///     memory, never pooled: a roster sequence borrowing one stays valid for as long as it is
	///     reachable, whatever the store does.
	/// </summary>
	public static class SerializedSkinStore
	{
		private static readonly ConcurrentDictionary<ulong, Entry> Store = new ConcurrentDictionary<ulong, Entry>();

		private sealed class Entry
		{
			public byte[] Bytes;
			public int RefCount;
		}

		public sealed class Handle
		{
			internal ulong Key;
			internal bool Shared;
			public byte[] Bytes;
		}

		public static int Count => Store.Count;

		public static Handle Acquire(byte[] serializedSkin)
		{
			ulong key = XxHash64.HashToUInt64(serializedSkin);
			while (true)
			{
				Entry entry = Store.GetOrAdd(key, _ => new Entry {Bytes = serializedSkin});
				lock (entry)
				{
					if (entry.RefCount < 0) continue; // lost a race with eviction, the dict slot is free again

					// A 64-bit collision is vanishingly unlikely but would put someone else's skin on
					// this player forever, so verify and fall back to an unshared handle instead.
					if (!entry.Bytes.AsSpan().SequenceEqual(serializedSkin))
					{
						return new Handle {Shared = false, Bytes = serializedSkin};
					}

					entry.RefCount++;
					return new Handle {Key = key, Shared = true, Bytes = entry.Bytes};
				}
			}
		}

		public static void Release(Handle handle)
		{
			if (handle == null || !handle.Shared) return;
			if (!Store.TryGetValue(handle.Key, out Entry entry)) return;

			lock (entry)
			{
				if (entry.RefCount <= 0) return;
				if (--entry.RefCount == 0)
				{
					entry.RefCount = -1; // poison so a racing Acquire retries against a fresh entry
					Store.TryRemove(handle.Key, out _);
				}
			}
		}
	}

	/// <summary>
	///     One player's Add record as immutable wire fragments: variant tag + fields before the
	///     skin, the shared skin body, and the fields after it. Concatenated they are the exact
	///     bytes the object-path record writer produces. Rebuilt from scratch on invalidation
	///     (rename, skin change); the old arrays stay valid for any roster still borrowing them.
	/// </summary>
	public sealed class PlayerListRecordSlices
	{
		public readonly byte[] Prefix;
		public readonly SerializedSkinStore.Handle Skin;
		public readonly byte[] Suffix;

		public PlayerListRecordSlices(byte[] prefix, SerializedSkinStore.Handle skin, byte[] suffix)
		{
			Prefix = prefix;
			Skin = skin;
			Suffix = suffix;
		}

		public static PlayerListRecordSlices Build(Player player)
		{
			(byte[] prefix, byte[] skinBytes, byte[] suffix) = McpePlayerList.EncodeAddRecordSlices(McpePlayerList.AddEntry(player));
			return new PlayerListRecordSlices(prefix, SerializedSkinStore.Acquire(skinBytes), suffix);
		}

		public void Release()
		{
			SerializedSkinStore.Release(Skin);
		}
	}

	/// <summary>
	///     Assembles the full-roster McpePlayerList a joiner receives as a zero-copy
	///     <see cref="ReadOnlySequence{T}" /> chaining every player's cached record slices, so the
	///     multi-megabyte contiguous encode a thousand-player roster used to require never exists.
	///     The consumer is the batch compressor, which reads segments sequentially. The sequence
	///     borrows the slices: consume it before dropping the reference, and rely on the store's
	///     refcounts plus GC ownership for safety against concurrent invalidation.
	/// </summary>
	public static class PlayerListRosterBuilder
	{
		public static ReadOnlySequence<byte> BuildAdded(IReadOnlyList<Player> players)
		{
			// Packet id + record count, the only bytes not served from a cache.
			using var headerStream = new MemoryStream(8);
			VarInt.WriteInt32(headerStream, 0x3f); // McpePlayerList
			VarInt.WriteUInt32(headerStream, (uint) players.Count);

			var first = new Segment(headerStream.ToArray(), null);
			Segment last = first;

			foreach (Player player in players)
			{
				PlayerListRecordSlices slices = player.GetOrBuildRosterSlices();
				last = new Segment(slices.Prefix, last);
				last = new Segment(slices.Skin.Bytes, last);
				last = new Segment(slices.Suffix, last);
			}

			return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
		}

		private sealed class Segment : ReadOnlySequenceSegment<byte>
		{
			public Segment(ReadOnlyMemory<byte> memory, Segment previous)
			{
				Memory = memory;
				if (previous != null)
				{
					previous.Next = this;
					RunningIndex = previous.RunningIndex + previous.Memory.Length;
				}
			}
		}
	}
}