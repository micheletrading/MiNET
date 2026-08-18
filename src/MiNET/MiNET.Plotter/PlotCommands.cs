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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2017 Niclas Olofsson. 
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using MiNET.Net;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.Plotter
{
	public class PlotCommands
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(PlotCommands));

		private readonly PlotManager _plotManager;

		public PlotCommands(PlotManager plotManager)
		{
			_plotManager = plotManager;
		}

		/// <summary>
		///     Plot coordinates cover every world, so without this guard a plot command run in a map
		///     world claims, retitles or resets ground that has no plot grid under it at all.
		/// </summary>
		private static bool HasNoPlots(Player player)
		{
			return !PlotterLevelManager.IsPlotWorld(player.Level);
		}

		private const string NoPlotsHere = "There are no plots in this world.";

		[Command(Name = "plot auto")]
		public string PlotAuto(Player player)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			BlockCoordinates coords = (BlockCoordinates) player.KnownPosition;

			int x = 0, y = 0, d = 1, m = 1;
			int i = 0;
			while (i++ < 1000)
			{
				while (2*x*d < m)
				{
					var current = new PlotCoordinates(x, y);
					if (_plotManager.TryClaim(current, player, out var plot))
					{
						return ClaimPlot(player, current);
					}

					x = x + d;
				}

				while (2*y*d < m)
				{
					var current = new PlotCoordinates(x, y);
					if (_plotManager.TryClaim(current, player, out var plot))
					{
						return ClaimPlot(player, current);
					}

					y = y + d;
				}

				d = -1*d;
				m = m + 1;
			}

			return "Not able to claim plot at this position.";
		}

		private string ClaimPlot(Player player, PlotCoordinates coords)
		{
			var bbox = PlotManager.GetBoundingBoxForPlot(coords);
			var center = bbox.Max - (bbox.Max - bbox.Min)/2;
			int height = player.Level.GetHeight(center);

			player.Teleport(new PlayerLocation(center.X, height + 3, center.Z));

			return $"Claimed plot {coords.X},{coords.Z}";
		}

		[Command(Name = "plot claim")]
		public string PlotClaim(Player player)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (coords == null) return "Not able to claim plot at this position.";

			Plot plot;
			if (!_plotManager.TryClaim(coords, player, out plot)) return "Not able to claim plot at this position.";

			return $"Claimed plot {plot.Coordinates.X},{plot.Coordinates.Z} at {coords}";
		}

		[Command(Name = "plot setowner")]
		public string PlotSetOwner(Player player, string username)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (coords == null) return "Not able to set owner for this plot.";
			if (!_plotManager.HasClaim(coords, player)) return "Not able to set owner for this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "Not able to set owner for this plot.";

			var plotPlayer = _plotManager.GetPlotPlayer(username);
			if (plotPlayer == null)
			{
				var newOwnerPlayer = player.Level.GetSpawnedPlayers().FirstOrDefault(p => p.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
				if (newOwnerPlayer == null)
				{
					return $"Found no player with the name {username}";
				}

				plotPlayer = _plotManager.GetOrAddPlotPlayer(newOwnerPlayer);
			}

			plot.Owner = plotPlayer.Xuid;
			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to set owner for this plot.";
			}

			return $"Set new owner to {username}";
		}

		[Command(Name = "plot add")]
		public string PlotAddPlayer(Player player, string username)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (coords == null) return "Not able to add player for this plot.";
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			var plotPlayer = _plotManager.GetPlotPlayer(username);
			if (plotPlayer == null)
			{
				var newOwnerPlayer = player.Level.GetSpawnedPlayers().FirstOrDefault(p => p.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
				if (newOwnerPlayer == null)
				{
					return $"Found no player with the name {username}";
				}

				plotPlayer = _plotManager.GetOrAddPlotPlayer(newOwnerPlayer);
			}

			List<UUID> builders = new List<UUID>(plot.AllowedPlayers);
			if (builders.Contains(plotPlayer.Xuid)) return "Player already added to this plot.";

			builders.Add(plotPlayer.Xuid);
			plot.AllowedPlayers = builders.ToArray();

			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to update this plot.";
			}

			return $"Added player {username} to plot {plot.Coordinates}";
		}

		[Command(Name = "plot remove")]
		public string PlotRemovePlayer(Player player, string username)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (coords == null) return "No plot found.";
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			var plotPlayer = _plotManager.GetPlotPlayer(username);
			if (plotPlayer == null)
			{
				var newOwnerPlayer = player.Level.GetSpawnedPlayers().FirstOrDefault(p => p.Username.Equals(username, StringComparison.InvariantCultureIgnoreCase));
				if (newOwnerPlayer == null)
				{
					return $"Found no player with the name {username}";
				}

				plotPlayer = _plotManager.GetOrAddPlotPlayer(newOwnerPlayer);
			}

			List<UUID> builders = new List<UUID>(plot.AllowedPlayers);
			if (!builders.Contains(plotPlayer.Xuid)) return "Player does not exist on this plot.";

			builders.Remove(plotPlayer.Xuid);
			plot.AllowedPlayers = builders.ToArray();

			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to update this plot.";
			}

			return $"Removed player {username} from plot {plot.Coordinates}";
		}

		[Command(Name = "plot sethome")]
		public string PlotSetHome(Player player)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (coords == null) return "Not able to set home plot at this position.";
			if (!_plotManager.HasClaim(coords, player)) return "Not able to set home plot at this position.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "Not able to set home plot at this position.";

			PlotPlayer plotPlayer = _plotManager.GetOrAddPlotPlayer(player);
			plotPlayer.Home = player.KnownPosition;
			_plotManager.UpdatePlotPlayer(plotPlayer);

			return $"Set home to plot {plot.Coordinates.X},{plot.Coordinates.Z}";
		}

		[Command(Name = "plot home")]
		public string PlotHome(Player player)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotPlayer plotPlayer = _plotManager.GetOrAddPlotPlayer(player);
			if (plotPlayer == null) return "Sorry, you don't exist.";

			PlotCoordinates coords = (PlotCoordinates) plotPlayer.Home;
			if (coords == null) return "Sorry, you are homeless.";
			if (!_plotManager.HasClaim(coords, player)) return "Sorry, you don't own your home. Did the bank claim it?";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "Sorry, we lost your home. Ask an admin to find it again!";

			int height = player.Level.GetHeight((BlockCoordinates) plotPlayer.Home);
			if (plotPlayer.Home.Y < height) plotPlayer.Home.Y = height + 2;
			player.Teleport(plotPlayer.Home);

			return $"Moved to your home plot {plot.Coordinates.X},{plot.Coordinates.Z}";
		}

		[Command(Name = "plot settitle")]
		public string PlotSetTitle(Player player, string title = null)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			plot.Title = title;

			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to update this plot.";
			}

			return $"Set title on plot {plot.Coordinates} to {title}";
		}

		[Command(Name = "plot setdescription")]
		public string PlotSetDescription(Player player, string description = null)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			plot.Description = description;

			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to update this plot.";
			}

			return $"Set description on plot {plot.Coordinates} to {description}";
		}

		[Command(Name = "plot settime")]
		public string PlotSetTime(Player player, int time = 5000)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			plot.Time = time;

			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to update this plot.";
			}

			player.SendSetTime(plot.Time);

			return $"Set time on plot {plot.Coordinates} to {time}";
		}

		[Command(Name = "plot setdownfall")]
		public string PlotSetDownfall(Player player, int downfall = 0)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			plot.Downfall = downfall;

			if (!_plotManager.UpdatePlot(plot))
			{
				return "Not able to update this plot.";
			}

			player.SendSetDownfall(plot.Downfall);

			return $"Set downfall on plot {plot.Coordinates} to {downfall}";
		}

		[Command(Name = "plot setbiome")]
		public string PlotSetBiome(Player player, int biomeId = 1)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (!_plotManager.HasClaim(coords, player)) return "You don't own this plot.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "No plot found.";

			var bbox = PlotManager.GetBoundingBoxForPlot(plot.Coordinates);

			PlotWorldGenerator.SetBiome(player.Level, bbox, (byte) biomeId);

			Task.Run(() =>
			{
				player.CleanCache();
				player.ForcedSendChunks(() => { player.SendMessage($"Resent chunks."); });
			});

			return $"Set biome on plot {plot.Coordinates} to {biomeId}";
		}

		[Command(Name = "plot visit")]
		public string PlotVisit(Player player, string username)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotPlayer plotPlayer = _plotManager.GetPlotPlayer(username);
			if (plotPlayer == null) return "Sorry, that user is homeless.";

			PlotCoordinates coords = (PlotCoordinates) plotPlayer.Home;
			if (coords == null) return "Sorry, player is homeless.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "Sorry, we lost his home. Maybe ask an admin to find it again!";

			int height = player.Level.GetHeight((BlockCoordinates) plotPlayer.Home);
			if (plotPlayer.Home.Y < height) plotPlayer.Home.Y = height + 2;
			player.Teleport(plotPlayer.Home);

			return $"Moved you to plot {plot.Coordinates.X},{plot.Coordinates.Z}. Home of {username}";
		}

		[Command(Name = "plot visit")]
		public string PlotVisit(Player player, int x, int z)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = new PlotCoordinates(x, z);

			if (x == 0 || z == 0) return $"No plot at this location {coords.X},{coords.Z}.";

			var bbox = PlotManager.GetBoundingBoxForPlot(coords);
			var center = bbox.Max - (bbox.Max - bbox.Min)/2;
			int height = player.Level.GetHeight(center);

			player.Teleport(new PlayerLocation(center.X, height + 3, center.Z));

			return $"Moved you to plot {coords.X},{coords.Z}.";
		}

		[Command(Name = "plot clear")]
		public string PlotClear(Player player)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (coords == null) return "Not able to reset plot at this position.";

			if (!_plotManager.HasClaim(coords, player)) return "Not able to reset plot at this position.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot)) return "Not able to reset plot at this position.";

			PlotWorldGenerator.ResetBlocks(player.Level, PlotManager.GetBoundingBoxForPlot(plot.Coordinates));

			return $"Reset plot {plot.Coordinates.X},{plot.Coordinates.Z}.";
		}

		[Command(Name = "plot delete")]
		[Authorize(Permission = 4)]
		public string PlotDelete(Player player, bool force = false)
		{
			if (HasNoPlots(player)) return NoPlotsHere;

			PlotCoordinates coords = (PlotCoordinates) player.KnownPosition;
			if (!force && !_plotManager.HasClaim(coords, player)) return "Not able to reset plot at this position.";
			if (!_plotManager.TryGetPlot(coords, out Plot plot) && !force) return "Not able to delete plot at this position.";
			if (plot != null && !_plotManager.Delete(plot)) return "Not able to delete plot at this position.";

			PlotWorldGenerator.ResetBlocks(player.Level, PlotManager.GetBoundingBoxForPlot(coords), true);

			return $"Deleted plot {coords.X},{coords.Z}.";
		}

		[Command(Name = "plot resend")]
		[Authorize(Permission = 4)]
		public void PlotResendChunks(Player player)
		{
			Task.Run(() =>
			{
				player.CleanCache();
				player.ForcedSendChunks(() => { player.SendMessage($"Resent chunks."); });
			});
		}

		// World management.
		//
		// A world here is its folder and nothing else: the plot world in LevelDBWorldFolder, and one
		// world per subfolder of WorldsRoot, opened on first visit by PlotterLevelManager. Nothing
		// registers them anywhere, so listing means reading the folders and asking the level manager
		// which of them it currently holds.

		[Command(Name = "plot worlds", Description = "Lists the worlds under WorldsRoot, marking loaded ones.")]
		public string Worlds(Player player, string filter = null)
		{
			LevelManager levelManager = player.Level.LevelManager;

			string[] loaded;
			lock (levelManager.Levels)
			{
				loaded = levelManager.Levels.Select(level => level.LevelId).ToArray();
			}

			string worldsRoot = Config.GetProperty("WorldsRoot", "").Trim();
			IEnumerable<string> available = worldsRoot.Length == 0 || !Directory.Exists(worldsRoot)
				? Array.Empty<string>()
				: Directory.EnumerateDirectories(worldsRoot).Select(Path.GetFileName);

			if (!string.IsNullOrEmpty(filter))
			{
				available = available.Where(name => name.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
			}

			string[] names = available.OrderBy(name => name, StringComparer.InvariantCultureIgnoreCase).ToArray();
			if (names.Length == 0) return worldsRoot.Length == 0 ? "No WorldsRoot configured." : "No worlds found.";

			var message = new List<string> {$"{names.Length} world(s), you are in {player.Level.LevelId}:"};
			foreach (string name in names)
			{
				bool isLoaded = loaded.Any(id => id.Equals(name, StringComparison.InvariantCultureIgnoreCase));
				message.Add(isLoaded ? $"{ChatColors.Green}{name}{ChatColors.White} (loaded)" : name);
			}

			return string.Join("\n", message);
		}

		[Command(Name = "plot world", Description = "Teleports you to a world. Pass create to make one that does not exist.")]
		public string World(Player player, string name, bool create = false)
		{
			if (player.Level.LevelId.Equals(name, StringComparison.InvariantCultureIgnoreCase))
			{
				player.Teleport(player.SpawnPosition);
				return $"Already in {player.Level.LevelId}, moved you to its spawn.";
			}

			LevelManager levelManager = player.Level.LevelManager;

			bool exists;
			lock (levelManager.Levels)
			{
				exists = levelManager.Levels.Any(level => level.LevelId.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			}

			// A name with no folder is a new world, which is right for the plot server and wrong for a
			// typo while browsing maps. So creating one is asked for, and a misspelt map name says so
			// instead of quietly leaving an empty world behind.
			if (!exists && !create)
			{
				string worldsRoot = Config.GetProperty("WorldsRoot", "").Trim();
				if (worldsRoot.Length > 0 && !Directory.Exists(Path.Combine(worldsRoot, name)))
				{
					return $"No world named {name}. Use /plot worlds to list them, or /plot world {name} true to create it.";
				}
			}

			// Opened before the move rather than inside SpawnLevel's level function: that function runs
			// after the player has already left the old world, so a database that will not open strands
			// them mid-transfer instead of answering the command. Reading a map's level.dat and tables
			// is quick, and pre-warming its chunks runs on its own thread afterwards.
			Level nextLevel;
			try
			{
				lock (levelManager.Levels)
				{
					nextLevel = levelManager.GetLevel(player, name);
				}
			}
			catch (Exception e)
			{
				Log.Error($"Could not open world {name}", e);
				return $"Could not open {name}: {e.Message}";
			}

			player.SpawnLevel(nextLevel, nextLevel.SpawnPoint, false);

			return $"Moving you to {name}.";
		}
	}
}