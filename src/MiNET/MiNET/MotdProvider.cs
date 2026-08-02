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
using System.Net;
using MiNET.Net;
using MiNET.Net.RakNet;
using MiNET.Utils;

namespace MiNET
{
	public class MotdProvider
	{
		public string Motd { get; set; }

		public string SecondLine { get; set; }

		public int MaxNumberOfPlayers { get; set; }

		public int NumberOfPlayers { get; set; }

		public long ServerId { get; set; }

		public string GameMode { get; set; }

		public int PortV4 { get; set; }

		public int PortV6 { get; set; }

		public bool IsFreeToJoin { get; set; } = true;

		public bool IsInEditorMode { get; set; }

		/// <summary>
		///     The version the pong advertises: the protocol target's major and minor with the patch
		///     zeroed. The client checks this string as well as the protocol number before it will
		///     list a server, and it is stricter about it than the protocol. Tested against a 1.26.33
		///     client, all on protocol 1001: "1.26.34" and "1.26" are both hidden, "1.26.33" and
		///     "1.26.0" both show. So a patch we name that the client has not shipped is rejected,
		///     and zero is the wildcard.
		/// </summary>
		public string AdvertisedVersion { get; set; } = "1.26.33";

		public MotdProvider()
		{
			byte[] buffer = new byte[8];
			new Random().NextBytes(buffer);
			buffer[7] = 0;
			ServerId = BitConverter.ToInt64(buffer, 0);

			ServerId = Config.GetProperty("serverid", ServerId);
			Motd = Config.GetProperty("motd", "MiNET Server");
			SecondLine = Config.GetProperty("motd-2nd", "MiNET");
			GameMode = Config.GetProperty("gamemode", "Survival");

			// Overwritten with the real listening port once the server has resolved its endpoint.
			PortV4 = Config.GetProperty("port", 19132);
			PortV6 = PortV4 + 1;
		}

		private static string ZeroPatchVersion(string gameVersion)
		{
			string[] parts = gameVersion.Split('.');
			return parts.Length < 2 ? gameVersion : $"{parts[0]}.{parts[1]}.0";
		}

		public virtual string GetMotd(ConnectionInfo connectionInfo, IPEndPoint caller, bool eduMotd = false)
		{
			NumberOfPlayers = connectionInfo.NumberOfPlayers;
			MaxNumberOfPlayers = connectionInfo.MaxNumberOfPlayers;

			ulong serverId = (ulong) ServerId;

			var protocolVersion = McpeProtocolInfo.ProtocolVersion.ToString();
			var clientVersion = AdvertisedVersion;
			var edition = "MCPE";

			if (eduMotd)
			{
				protocolVersion = "291";
				clientVersion = "1.7.0";
				edition = "MCEE";
			}
			
			// Big brain Microjang moment here
			if (SecondLine == "")
				// As of 1.16.210, the sub-MOTD cannot be blank or Minecraft won't see the MOTD
				SecondLine = "MiNET";

			// The tail of the string is what the client needs to offer the server as joinable. The
			// field after the game mode is not a numeric game mode, it is whether the server is open
			// to join, and the client honours it. The last one is the editor-mode flag.
			string isFreeToJoin = IsFreeToJoin ? "1" : "0";
			string isInEditorMode = IsInEditorMode ? "1" : "0";

			// 2019-12-29 20:00:46,672 [DedicatedThreadPool-8631ff8f-0339-4a0d-83c7-222335bdb410_1] WARN  MiNET.Client.MiNetClient - MOTD: MCPE;gurunx;389;1.14.1;1;8;9586953286635751800;My World;Creative;1;53387;53388;
			return string.Format($"{edition};{Motd};{protocolVersion};{clientVersion};{NumberOfPlayers};{MaxNumberOfPlayers};{serverId};{SecondLine};{GameMode};{isFreeToJoin};{PortV4};{PortV6};{isInEditorMode};");
		}
	}
}