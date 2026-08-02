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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using fNbt;
using log4net;
using MiNET.Blocks;
using MiNET.Utils;

namespace MiNET.Client
{
	/// <summary>
	///     The full network block palette for the target game version, keyed by the FNV-1a 32
	///     network hash of each state. Chunk palettes reference block states by these hashes when
	///     the server has block_network_ids_are_hashes set in StartGame.
	///     Taken straight from BlockFactory, which compiles the same palette in as generated code.
	///     This used to parse its own 5.5MB copy of the minecraft-data dump at startup to rebuild a
	///     table the referenced assembly already had in memory.
	/// </summary>
	public static class NetworkBlockPalette
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetworkBlockPalette));

		public class Entry
		{
			public int Index { get; set; }
			public string Name { get; set; }
			public List<IBlockState> States { get; set; }
		}

		private static readonly object LoadLock = new object();
		private static Dictionary<uint, Entry> _hashToEntry;

		public static Dictionary<uint, Entry> HashToEntry
		{
			get
			{
				if (_hashToEntry != null) return _hashToEntry;
				lock (LoadLock)
				{
					if (_hashToEntry == null) _hashToEntry = Load();
				}
				return _hashToEntry;
			}
		}

		private static Dictionary<uint, Entry> Load()
		{
			var result = new Dictionary<uint, Entry>();

			int index = 0;
			foreach (BlockStateContainer container in BlockFactory.BlockPalette)
			{
				uint hash = ComputeNetworkHash(container.Name, container.States);
				result[hash] = new Entry {Index = index, Name = container.Name, States = container.States};
				index++;
			}

			Log.Warn($"Loaded network block palette: {index} states, {result.Count} unique hashes");
			return result;
		}

		/// <summary>
		///     The client-side registry checksum, computed from the palette exactly as the real
		///     client is presumed to: verified against BDS's StartGame blockPaletteChecksum on
		///     every connection (see BedrockTraceHandler.HandleMcpeStartGame). The ALGORITHM IS
		///     UNVERIFIED: no candidate has reproduced BDS's value yet, so a MISMATCH log is the
		///     expected state until the real computation is identified. Current candidate:
		///     FNV-1a 64 over the concatenated per-state hash documents in palette order.
		/// </summary>
		public static ulong ComputeRegistryChecksum()
		{
			ulong hash = 14695981039346656037;
			foreach (Entry entry in HashToEntry.Values.OrderBy(e => e.Index))
			{
				foreach (byte b in SerializeHashDocument(entry.Name, entry.States))
				{
					hash ^= b;
					hash = unchecked(hash * 1099511628211);
				}
			}
			return hash;
		}

		/// <summary>
		///     FNV-1a 32 over the standard little-endian NBT of {name, states}, states
		///     sorted alphabetically. minecraft:unknown is hardcoded to -2.
		/// </summary>
		public static uint ComputeNetworkHash(string name, List<IBlockState> states)
		{
			if (name == "minecraft:unknown") return unchecked((uint) -2);

			byte[] bytes = SerializeHashDocument(name, states);

			uint hash = 0x811c9dc5;
			foreach (byte b in bytes)
			{
				hash ^= b;
				hash *= 0x01000193;
			}

			return hash;
		}

		private static byte[] SerializeHashDocument(string name, List<IBlockState> states)
		{
			var statesCompound = new NbtCompound("states");
			foreach (IBlockState state in states.OrderBy(s => s.Name, StringComparer.Ordinal))
			{
				switch (state)
				{
					case BlockStateByte b:
						statesCompound.Add(new NbtByte(b.Name, b.Value));
						break;
					case BlockStateInt i:
						statesCompound.Add(new NbtInt(i.Name, i.Value));
						break;
					case BlockStateString s:
						statesCompound.Add(new NbtString(s.Name, s.Value));
						break;
				}
			}

			var root = new NbtCompound("")
			{
				new NbtString("name", name),
				statesCompound
			};

			var file = new NbtFile
			{
				BigEndian = false,
				UseVarInt = false,
				RootTag = root
			};

			return file.SaveToBuffer(NbtCompression.None);
		}
	}
}
