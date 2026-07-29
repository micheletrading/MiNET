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
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;

namespace MiNET.Net
{
	/// <summary>
	///		Static join-sequence content captured from vanilla BDS 1.26.34 (Net/Data/JoinCapture,
	///		embedded resources named &lt;seq&gt;-&lt;PacketName&gt;.bin, wire trace via
	///		MINET_PACKET_DUMP). These carry data MiNET has no generator for yet: entity property
	///		definitions, biome definitions, camera presets, trim data, fog, etc. Every payload is
	///		decoded with MiNET's own parser and re-encoded with MiNET's own writer per send; the
	///		decode-&gt;encode roundtrip is verified byte-identical for every resource here, so what
	///		goes out is exactly the vanilla data, produced by our code.
	/// </summary>
	public static class JoinCapture
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(JoinCapture));

		private static readonly Lazy<List<(string Seq, string Name, byte[] Frame)>> _frames = new Lazy<List<(string Seq, string Name, byte[] Frame)>>(() =>
		{
			var result = new List<(string Seq, string Name, byte[] Frame)>();
			var assembly = Assembly.GetAssembly(typeof(JoinCapture));
			const string prefix = "MiNET.Net.Data.JoinCapture.";
			foreach (string resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix) && n.EndsWith(".bin")).OrderBy(n => n, StringComparer.Ordinal))
			{
				using var stream = assembly.GetManifestResourceStream(resource);
				using var ms = new MemoryStream();
				stream.CopyTo(ms);
				string baseName = resource.Substring(prefix.Length, resource.Length - prefix.Length - 4); // "0012-McpeSyncEntityProperty"
				int dash = baseName.IndexOf('-');
				result.Add((baseName.Substring(0, dash), baseName.Substring(dash + 1), ms.ToArray()));
			}
			Log.Info($"Loaded {result.Count} captured join packets");
			return result;
		});

		/// <summary>
		///		Creates fresh decoded packet instances for every captured frame matching the packet
		///		name (and optional sequence prefix), in capture order. A fresh decode per call keeps
		///		packet pooling correct.
		/// </summary>
		public static IEnumerable<Packet> CreatePackets(string packetName, string seq = null)
		{
			foreach ((string s, string name, byte[] frame) in _frames.Value)
			{
				if (name != packetName) continue;
				if (seq != null && s != seq) continue;

				int id = MiNET.Utils.VarInt.ReadInt32(new MemoryStream(frame));
				Packet packet = PacketFactory.Create(id, frame, "mcpe");
				if (packet == null)
				{
					Log.Error($"Captured join packet {s}-{name} no longer decodes; skipping");
					continue;
				}
				yield return packet;
			}
		}
	}
}
