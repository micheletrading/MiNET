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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using log4net;

namespace MiNET.Worlds
{
	/// <summary>
	///     What happens to a player who dies while the level is in hardcore mode.
	/// </summary>
	public enum HardcoreDeathPolicy
	{
		/// <summary>The player is permanently banned (persisted per identity) and cannot rejoin.</summary>
		Ban,

		/// <summary>The player respawns as a spectator and can watch but not interact.</summary>
		Spectator,

		/// <summary>Vanilla behaviour: inventory drops, the player respawns normally.</summary>
		Drop
	}

	/// <summary>
	///     Server-side hardcore rules. Hardcore is a difficulty, not a gamemode: the client never
	///     sees a new GameMode value, only the isHardcore StartGame flag, and everything else here
	///     is enforced server-side.
	/// </summary>
	public static class HardcoreManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HardcoreManager));

		private static readonly object Sync = new object();
		private static readonly string BanFilePath = Path.Combine(Environment.CurrentDirectory, "hardcore-bans.json");
		private static readonly HashSet<string> Bans = LoadBans();

		public static bool IsHardcore(Level level)
		{
			return level.Difficulty == Difficulty.Hardcore;
		}

		public static bool IsBanned(string key)
		{
			lock (Sync)
			{
				return Bans.Contains(key);
			}
		}

		public static void Ban(string key)
		{
			lock (Sync)
			{
				if (Bans.Add(key)) SaveBans();
			}
		}

		private static HashSet<string> LoadBans()
		{
			try
			{
				if (!File.Exists(BanFilePath)) return new HashSet<string>();
				List<string> keys = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(BanFilePath));
				return keys == null ? new HashSet<string>() : new HashSet<string>(keys);
			}
			catch (Exception e)
			{
				Log.Warn($"Could not read hardcore ban list from {BanFilePath}", e);
				return new HashSet<string>();
			}
		}

		private static void SaveBans()
		{
			try
			{
				File.WriteAllText(BanFilePath, JsonSerializer.Serialize(new List<string>(Bans)));
			}
			catch (Exception e)
			{
				Log.Warn($"Could not write hardcore ban list to {BanFilePath}", e);
			}
		}
	}
}
