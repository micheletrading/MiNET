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
using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Threading;
using log4net;
using MiNET.Utils;

namespace MiNET.Worlds.BlobCache
{
	/// <summary>
	///     Content-addressed store of the chunk blobs handed out to clients, so a miss can be
	///     answered later in the session with the exact bytes the hash was taken over.
	///
	///     Server-wide because the addressing is content based: the same section produces the same
	///     hash in any level or dimension, and splitting the store per level would just fragment
	///     it. Nothing here needs invalidating either. Edit a block and the section produces a new
	///     hash and a new entry; the old one stops being referenced and ages out.
	/// </summary>
	public static class BlobStore
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BlobStore));

		private static readonly ConcurrentDictionary<ulong, byte[]> Blobs = new ConcurrentDictionary<ulong, byte[]>();

		private static long _bytesHeld;
		private static long _hits;
		private static long _misses;

		/// <summary>
		///     Above this the store stops accepting new blobs. Not an eviction policy: dropping a
		///     blob a client still references turns into a miss we cannot answer, which strands
		///     that chunk. Refusing to add is safe because a blob we never stored is simply one we
		///     never advertise a hash for.
		/// </summary>
		public static long MaxBytes { get; set; } = Config.GetProperty("BlobCache.MaxBytes", 256) * 1024L * 1024L;

		/// <summary>
		///     Whether we serve chunks as blobs at all. Off by default: the plain chunk path is the
		///     one every client can take, and this only pays back for players who return to a world
		///     they have already downloaded.
		/// </summary>
		public static bool Enabled { get; set; } = Config.GetProperty("BlobCache.Enabled", false);

		public static long BytesHeld => Interlocked.Read(ref _bytesHeld);
		public static int Count => Blobs.Count;
		public static long Hits => Interlocked.Read(ref _hits);
		public static long Misses => Interlocked.Read(ref _misses);

		/// <summary>
		///     XXHash64 with seed 0, the only hashing the client cache accepts.
		/// </summary>
		public static ulong ComputeHash(ReadOnlySpan<byte> blob)
		{
			return XxHash64.HashToUInt64(blob);
		}

		/// <summary>
		///     Stores the blob if it is new and returns its hash. Deduplication happens here and is
		///     the whole point: every all-air section, every solid stone section and every repeated
		///     terrain shape in the world collapses onto one entry.
		/// </summary>
		public static ulong Add(byte[] blob)
		{
			ulong hash = ComputeHash(blob);

			if (Blobs.ContainsKey(hash)) return hash;
			if (Interlocked.Read(ref _bytesHeld) + blob.Length > MaxBytes) return hash;

			if (Blobs.TryAdd(hash, blob)) Interlocked.Add(ref _bytesHeld, blob.Length);

			return hash;
		}

		public static bool TryGet(ulong hash, out byte[] blob)
		{
			bool found = Blobs.TryGetValue(hash, out blob);
			if (found) Interlocked.Increment(ref _hits);
			else Interlocked.Increment(ref _misses);
			return found;
		}

		public static void Clear()
		{
			Blobs.Clear();
			Interlocked.Exchange(ref _bytesHeld, 0);
			Log.Debug("Blob store cleared");
		}
	}
}
