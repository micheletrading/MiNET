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
using MiNET;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Worlds;

namespace MiNET.WorldSwitcher
{
	/// <summary>
	///     Loads additional Bedrock worlds (each a folder holding db/ + level.dat) and lets
	///     players switch between them at runtime with /world &lt;name&gt;. Used to explore the
	///     BDS tree-capture worlds live: the levels are created lazily on first switch, the
	///     default level keeps the name "default", and the switch reuses the existing
	///     Player.SpawnLevel machinery (level change + respawn + forced chunk send).
	/// </summary>
	[Plugin(PluginName = "WorldSwitcher", Description = "Switch between preloaded Bedrock worlds at runtime", PluginVersion = "1.0", Author = "MiNET")]
	public class WorldSwitcherPlugin : Plugin
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(WorldSwitcherPlugin));

		private readonly object _sync = new();
		private readonly Dictionary<string, Level> _levels = new(StringComparer.OrdinalIgnoreCase);
		private string _folder = "TreeWorlds";

		protected override void OnEnable()
		{
			string folder = Config.GetProperty("WorldSwitcher.Folder", "TreeWorlds").Trim();
			// The server runs via `dotnet run` from the repo root, so the CWD is not the console
			// bin; the deployment copy lives next to the entry assembly. Register the union of
			// both (someone may have dropped extra worlds in either), and resolve level creation
			// against the folder holding the most worlds.
			var candidates = new List<string> {Path.Combine(Directory.GetCurrentDirectory(), folder)};
			string entry = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? ".", folder);
			if (!candidates.Contains(entry)) candidates.Add(entry);

			foreach (string candidate in candidates.Where(Directory.Exists))
			{
				foreach (string dir in Directory.GetDirectories(candidate))
				{
					lock (_sync)
					{
						_levels[Path.GetFileName(dir)] = null; // created lazily on first switch
					}
				}
			}

			_folder = candidates.Where(Directory.Exists)
				.OrderByDescending(c => Directory.GetDirectories(c).Length)
				.FirstOrDefault() ?? candidates[0];

			Log.Info($"WorldSwitcher: {_levels.Count} worlds registered, primary folder '{_folder}'");
		}

		[Command(Description = "Switch world: /world, /world list, /world <name>, /world default")]
		public string World(Player player, params string[] args)
		{
			if (args.Length == 0 || args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
			{
				lock (_sync)
				{
					var names = _levels.Keys.OrderBy(n => n).ToList();
					return $"Worlds ({names.Count}): " + string.Join(", ", names) + " â€” /world <name> to switch";
				}
			}

			string name = args[0];
			Level target;
			if (name.Equals("default", StringComparison.OrdinalIgnoreCase))
			{
				target = Context.LevelManager.Levels.FirstOrDefault();
				if (target == null) return "No default level";
			}
			else
			{
				lock (_sync)
				{
					if (!_levels.TryGetValue(name, out target))
						return $"Unknown world '{name}'. /world to list.";
					if (target == null)
					{
						string folder = Path.Combine(_folder, name);
						var provider = new LevelDbProvider(folder)
						{
							MissingChunkProvider = Context.LevelManager.Generator,
						};
						Log.Info($"WorldSwitcher: creating level '{name}' from {folder}");
						target = Context.LevelManager.CreateLevel(name, provider);
						_levels[name] = target;
					}
				}
			}

			if (player.Level == target) return $"Already in '{name}'";

			Log.Info($"WorldSwitcher: {player.Username} switching to '{name}'");
			player.SpawnLevel(target, target.SpawnPoint);
			return $"Switched to '{name}'";
		}
	}
}
