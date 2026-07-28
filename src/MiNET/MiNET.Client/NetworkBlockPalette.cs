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
using MiNET.Utils;
using Newtonsoft.Json.Linq;

namespace MiNET.Client
{
	/// <summary>
	///     The full network block palette for the target game version, loaded from the
	///     PrismarineJS/minecraft-data dump, with the FNV-1a 32 network hash per state.
	///     Chunk palettes reference block states by these hashes when the server has
	///     block_network_ids_are_hashes set in StartGame.
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

			var assembly = Assembly.GetAssembly(typeof(NetworkBlockPalette));
			using Stream stream = assembly.GetManifestResourceStream("MiNET.Client.Data.blockStates-1.26.30.json");
			using var reader = new StreamReader(stream);
			var entries = JArray.Parse(reader.ReadToEnd());

			int index = 0;
			foreach (JToken token in entries)
			{
				string name = "minecraft:" + (string) token["name"];
				var states = new List<IBlockState>();
				foreach (JProperty state in ((JObject) token["states"]).Properties())
				{
					string type = (string) state.Value["type"];
					switch (type)
					{
						case "byte":
							states.Add(new BlockStateByte {Name = state.Name, Value = (byte) (int) state.Value["value"]});
							break;
						case "int":
							states.Add(new BlockStateInt {Name = state.Name, Value = (int) state.Value["value"]});
							break;
						case "string":
							states.Add(new BlockStateString {Name = state.Name, Value = (string) state.Value["value"]});
							break;
						default:
							throw new InvalidDataException($"Unknown state type {type} on {name}");
					}
				}

				uint hash = ComputeNetworkHash(name, states);
				result[hash] = new Entry {Index = index, Name = name, States = states};
				index++;
			}

			Log.Warn($"Loaded network block palette: {index} states, {result.Count} unique hashes");
			return result;
		}

		/// <summary>
		///     FNV-1a 32 over the standard little-endian NBT of {name, states}, states
		///     sorted alphabetically. minecraft:unknown is hardcoded to -2.
		/// </summary>
		public static uint ComputeNetworkHash(string name, List<IBlockState> states)
		{
			if (name == "minecraft:unknown") return unchecked((uint) -2);

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

			byte[] bytes = file.SaveToBuffer(NbtCompression.None);

			uint hash = 0x811c9dc5;
			foreach (byte b in bytes)
			{
				hash ^= b;
				hash *= 0x01000193;
			}

			return hash;
		}
	}
}
