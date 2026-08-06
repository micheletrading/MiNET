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
using System.IO;
using System.Threading;
using log4net;
using MiNET.Net;

namespace MiNET.Tunnel
{
	/// <summary>
	///     Writes every frame that passes through the tunnel to disk as
	///     &lt;seq&gt;-&lt;direction&gt;-&lt;name&gt;.bin, one shared sequence for both directions so the
	///     interleaved order of the session is preserved.
	/// </summary>
	public class TunnelDump
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TunnelDump));

		private readonly string _directory;
		private int _seq;

		public TunnelDump(string username)
		{
			string baseDir = Environment.GetEnvironmentVariable("MINET_TUNNEL_DUMP") ?? Path.Combine("temp_auto", "tunnel");
			_directory = Path.Combine(baseDir, $"{DateTime.Now:yyyyMMdd-HHmmss}-{Sanitize(username)}");
			Directory.CreateDirectory(_directory);
			Log.Warn($"Tunnel: dumping frames to {Path.GetFullPath(_directory)}");
		}

		public static ReadOnlyMemory<byte> FrameOf(Packet message)
		{
			// Packet.Decode keeps the raw frame (id varint included) in Bytes. UnknownPacket is
			// never decoded, its buffer lives in Message instead.
			return message is UnknownPacket unknown ? unknown.Message : message.Bytes;
		}

		public void Write(string direction, Packet message, ReadOnlyMemory<byte> frame)
		{
			int seq = Interlocked.Increment(ref _seq);
			string name = message is UnknownPacket ? $"Unknown_{message.Id}" : message.GetType().Name;
			File.WriteAllBytes(Path.Combine(_directory, $"{seq:D4}-{direction}-{name}.bin"), frame.ToArray());
		}

		private static string Sanitize(string name)
		{
			if (string.IsNullOrEmpty(name)) return "unknown";
			foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
			return name;
		}
	}
}
