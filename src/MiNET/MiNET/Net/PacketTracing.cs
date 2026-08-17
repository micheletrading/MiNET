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
using System.Text.RegularExpressions;
using log4net;
using MiNET.Utils;
using MiNET.Utils.Nbt;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MiNET.Net
{
	/// <summary>
	///     Per-packet trace logging, filtered by the TracePackets.* config keys: an include and
	///     exclude regex over the packet type name, and a verbosity (0 = one line, 1 = JSON,
	///     2 = hex dump, 3 = both), overridable per type with TracePackets.Verbosity.&lt;Type&gt;.
	/// </summary>
	public static class PacketTracing
	{
		/// <summary>
		///     Hex of an incoming frame, called from the decode loop while the bytes still exist. A
		///     packet does not keep what it was parsed from, so this is the only place the raw form
		///     can be traced, and the id is all that is known about it here.
		/// </summary>
		public static void TraceReceiveFrame(ILog log, int id, ReadOnlyMemory<byte> frame)
		{
			if (!log.IsTraceEnabled()) return;
			if (Config.GetProperty("TracePackets.Verbosity", 0) is not (2 or 3)) return;

			try
			{
				log.Verbose($"> Receive frame: {id} (0x{id:x2})\n{Packet.HexDump(frame)}");
			}
			catch (Exception e)
			{
				log.Error("Error when printing trace", e);
			}
		}

		public static void TraceReceive(ILog log, Packet message)
		{
			if (!log.IsTraceEnabled()) return;

			try
			{
				string typeName = message.GetType().Name;

				string includePattern = Config.GetProperty("TracePackets.Include", ".*");
				string excludePattern = Config.GetProperty("TracePackets.Exclude", null);
				int verbosity = Config.GetProperty("TracePackets.Verbosity", 0);
				verbosity = Config.GetProperty($"TracePackets.Verbosity.{typeName}", verbosity);

				if (!Regex.IsMatch(typeName, includePattern))
				{
					return;
				}

				if (!string.IsNullOrWhiteSpace(excludePattern) && Regex.IsMatch(typeName, excludePattern))
				{
					return;
				}

				if (verbosity == 0)
				{
					log.Trace($"> Receive: {message.Id} (0x{message.Id:x2}): {message.GetType().Name}");
				}
				else if (verbosity == 1 || verbosity == 3)
				{
					var jsonSerializerSettings = new JsonSerializerSettings
					{
						PreserveReferencesHandling = PreserveReferencesHandling.Arrays,
						TypeNameHandling = TypeNameHandling.Auto,
						Formatting = Formatting.Indented,
						ReferenceLoopHandling = ReferenceLoopHandling.Ignore
					};

					jsonSerializerSettings.Converters.Add(new StringEnumConverter());
					jsonSerializerSettings.Converters.Add(new NbtIntConverter());
					jsonSerializerSettings.Converters.Add(new NbtStringConverter());
					jsonSerializerSettings.Converters.Add(new IPAddressConverter());
					jsonSerializerSettings.Converters.Add(new IPEndPointConverter());

					string result = JsonConvert.SerializeObject(message, jsonSerializerSettings);
					log.Trace($"> Receive: {message.Id} (0x{message.Id:x2}): {message.GetType().Name}\n{result}");
				}
				else if (verbosity == 2 || verbosity == 3)
				{
					// No hex here: the frame this was parsed from is gone by now, and TraceReceiveFrame
					// has already logged it from the decode loop where it still existed.
					log.Verbose($"> Receive: {message.Id} (0x{message.Id:x2}): {message.GetType().Name}");
				}
			}
			catch (Exception e)
			{
				log.Error("Error when printing trace", e);
			}
		}

		public static void TraceSend(ILog log, Packet message)
		{
			if (!log.IsTraceEnabled()) return;

			try
			{
				string typeName = message.GetType().Name;

				string includePattern = Config.GetProperty("TracePackets.Include", ".*");
				string excludePattern = Config.GetProperty("TracePackets.Exclude", null);
				int verbosity = Config.GetProperty("TracePackets.Verbosity", 0);
				verbosity = Config.GetProperty($"TracePackets.Verbosity.{typeName}", verbosity);

				if (!Regex.IsMatch(typeName, includePattern))
				{
					return;
				}

				if (!string.IsNullOrWhiteSpace(excludePattern) && Regex.IsMatch(typeName, excludePattern))
				{
					return;
				}

				if (verbosity == 0)
				{
					log.Trace($"<    Send: {message.Id} (0x{message.Id:x2}): {message.GetType().Name}");
				}
				else if (verbosity == 1 || verbosity == 3)
				{
					var jsonSerializerSettings = new JsonSerializerSettings
					{
						PreserveReferencesHandling = PreserveReferencesHandling.Arrays,
						TypeNameHandling = TypeNameHandling.Auto,
						Formatting = Formatting.Indented,
						DefaultValueHandling = DefaultValueHandling.Include,
						ReferenceLoopHandling = ReferenceLoopHandling.Ignore
					};

					jsonSerializerSettings.Converters.Add(new StringEnumConverter());
					jsonSerializerSettings.Converters.Add(new NbtIntConverter());
					jsonSerializerSettings.Converters.Add(new NbtStringConverter());
					jsonSerializerSettings.Converters.Add(new IPAddressConverter());
					jsonSerializerSettings.Converters.Add(new IPEndPointConverter());

					string result = JsonConvert.SerializeObject(message, jsonSerializerSettings);
					log.Trace($"<    Send: {message.Id} (0x{message.Id:x2}): {message.GetType().Name}\n{result}");
				}
				else if (verbosity == 2 || verbosity == 3)
				{
					log.Verbose($"<    Send: {message.Id} (0x{message.Id:x2}): {message.GetType().Name}\n{Packet.HexDump(message.EncodeAsMemory())}");
				}
			}
			catch (Exception e)
			{
				log.Error("Error when printing trace", e);
			}
		}
	}
}
