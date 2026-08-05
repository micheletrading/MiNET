using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using log4net;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.BuilderBase.Commands
{
	public class SelectionCommands
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (SelectionCommands));

		[Command(Description = "Set selection position 1")]
		public void Pos1(Player player, BlockPos coordinates = null)
		{
			var pos = Convert(coordinates, new BlockCoordinates(player.KnownPosition));

			RegionSelector selector = RegionSelector.GetSelector(player);
			selector.SelectPrimary(pos);

			player.SendMessage($"First position set to {pos}");
		}

		[Command(Description = "Set selection position 2")]
		public void Pos2(Player player, BlockPos coordinates = null)
		{
			var pos = Convert(coordinates, new BlockCoordinates(player.KnownPosition));

			RegionSelector selector = RegionSelector.GetSelector(player);
			selector.SelectSecondary(pos);

			player.SendMessage($"Second position set to {pos}");
		}

		[Command(Description = "Set position 1 to targeted block")]
		public void Hpos1(Player player)
		{
			var target = new EditHelper(player.Level, player).GetBlockInLineOfSight(player.Level, player.KnownPosition);
			if (target == null)
			{
				player.SendMessage("No block in range");
			}
			else
			{
				var pos = target.Coordinates;

				RegionSelector selector = RegionSelector.GetSelector(player);
				selector.SelectPrimary(pos);

				player.SendMessage($"First position set to {pos}");
			}
		}

		[Command(Description = "Set position 2 to targeted block")]
		public void Hpos2(Player player)
		{
			var target = new EditHelper(player.Level, player).GetBlockInLineOfSight(player.Level, player.KnownPosition);
			if (target == null)
			{
				player.SendMessage("No block in range");
			}
			else
			{
				var pos = target.Coordinates;

				RegionSelector selector = RegionSelector.GetSelector(player);
				selector.SelectSecondary(pos);

				player.SendMessage($"Second position set to {pos}");
			}
		}

		[Command(Description = "Set the selection to your current chunk")]
		public void Chunk(Player player)
		{
			BlockCoordinates blockCoordinates = (BlockCoordinates) player.KnownPosition;
			ChunkCoordinates chunk = new ChunkCoordinates(blockCoordinates.X >> 4, blockCoordinates.Z >> 4);

			RegionSelector selector = RegionSelector.GetSelector(player);
			selector.Select(new BlockCoordinates(chunk.X << 4, 0, chunk.Z << 4), new BlockCoordinates(16 + (chunk.X << 4), 255, 16 + (chunk.Z << 4)));
		}

		[Command(Description = "Expand the selection area")]
		public void Expand(Player player, string direction)
		{
			SelectionDirection dir;
			if (Enum.TryParse(direction, true, out dir))
			{
				if (dir != SelectionDirection.Vertical)
				{
					player.SendMessage($"You can only use vertical for unspecificed amout");
					return;
				}

				var selector = RegionSelector.GetSelector(player);
				selector.Select(new BlockCoordinates(selector.Position1.X, 0, selector.Position1.Z), new BlockCoordinates(selector.Position2.X, 255, selector.Position2.Z));
			}
		}

		[Command(Description = "Expand the selection area")]
		public void Expand(Player player, int amount, string direction = "me")
		{
			Expand(player, amount, 0, direction);
		}

		[Command(Description = "Expand the selection area")]
		public void Expand(Player player, int amount, int reverseAmount, string direction = "me")
		{
			SelectionDirection dir;
			if (Enum.TryParse(direction, true, out dir))
			{
				Expand(player, amount, reverseAmount, dir);
			}
			else
			{
				player.SendMessage($"Invalid value for direction <{direction}>");
			}
		}

		private void Expand(Player player, int amount, int reverseAmount = 0, SelectionDirection direction = SelectionDirection.Me)
		{
			if (amount < 0 || reverseAmount < 0)
			{
				player.SendMessage($"The amount need to a positive value. Try reversing the direction instead");
				return;
			}

			var selector = RegionSelector.GetSelector(player);

			var position1 = selector.Position1;
			var position2 = selector.Position2;

			if (direction == SelectionDirection.Me)
			{
				int playerDirection = Entity.DirectionByRotationFlat(player.KnownPosition.HeadYaw);
				switch (playerDirection)
				{
					case 1:
						direction = SelectionDirection.West;
						break;
					case 2:
						direction = SelectionDirection.North;
						break;
					case 3:
						direction = SelectionDirection.East;
						break;
					case 0:
						direction = SelectionDirection.South;
						break;
				}
			}

			switch (direction)
			{
				case SelectionDirection.Up:
					if (position1.Y > position2.Y)
					{
						selector.SelectPrimary(position1 + (Level.Up*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.Down*reverseAmount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.Up*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.Down*reverseAmount));
					}
					break;
				case SelectionDirection.Down:
					if (position1.Y < position2.Y)
					{
						selector.SelectPrimary(position1 + (Level.Down*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.Up*reverseAmount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.Down*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.Up*reverseAmount));
					}
					break;
				case SelectionDirection.West:
					if (position1.Z > position2.Z)
					{
						selector.SelectPrimary(position1 + (Level.South*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.North*reverseAmount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.South*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.North*reverseAmount));
					}
					break;
				case SelectionDirection.East:
					if (position1.Z < position2.Z)
					{
						selector.SelectPrimary(position1 + (Level.North*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.South*reverseAmount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.North*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.South*reverseAmount));
					}
					break;
				case SelectionDirection.South:
					if (position1.X > position2.X)
					{
						selector.SelectPrimary(position1 + (Level.East*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.West*reverseAmount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.East*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.West*reverseAmount));
					}
					break;
				case SelectionDirection.North:
					if (position1.X < position2.X)
					{
						selector.SelectPrimary(position1 + (Level.West*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.East*reverseAmount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.West*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.East*reverseAmount));
					}
					break;
				case SelectionDirection.Vertical:
					Expand(player, "vertical");
					break;
			}
		}

		[Command(Description = "Contract the selection area")]
		public void Contract(Player player, int amount, string direction = "me")
		{
			Contract(player, amount, 0, direction);
		}

		[Command(Description = "Contract the selection area")]
		public void Contract(Player player, int amount, int reverseAmount, string direction = "me")
		{
			SelectionDirection dir;
			if (Enum.TryParse(direction, true, out dir))
			{
				Contract(player, amount, reverseAmount, dir);
			}
			else
			{
				player.SendMessage($"Invalid value for direction <{direction}>");
			}
		}

		private void Contract(Player player, int amount, int reverseAmount = 0, SelectionDirection direction = SelectionDirection.Me)
		{
			if (amount < 0 || reverseAmount < 0)
			{
				player.SendMessage($"The amount need to a positive value. Try reversing the direction instead");
				return;
			}

			var selector = RegionSelector.GetSelector(player);

			var position1 = selector.Position1;
			var position2 = selector.Position2;

			if (direction == SelectionDirection.Me)
			{
				int playerDirection = Entity.DirectionByRotationFlat(player.KnownPosition.HeadYaw);
				switch (playerDirection)
				{
					case 1:
						direction = SelectionDirection.West;
						break;
					case 2:
						direction = SelectionDirection.North;
						break;
					case 3:
						direction = SelectionDirection.East;
						break;
					case 0:
						direction = SelectionDirection.South;
						break;
				}
			}

			switch (direction)
			{
				case SelectionDirection.Up:
					if (position1.Y < position2.Y)
					{
						selector.SelectPrimary(position1 + (Level.Up*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.Down*amount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.Up*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.Down*amount));
					}
					break;
				case SelectionDirection.Down:
					if (position1.Y > position2.Y)
					{
						selector.SelectPrimary(position1 + (Level.Down*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.Up*amount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.Down*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.Up*amount));
					}
					break;
				case SelectionDirection.West:
					if (position1.Z < position2.Z)
					{
						selector.SelectPrimary(position1 + (Level.South*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.North*amount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.South*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.North*amount));
					}
					break;
				case SelectionDirection.East:
					if (position1.Z > position2.Z)
					{
						selector.SelectPrimary(position1 + (Level.North*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.South*amount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.North*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.South*amount));
					}
					break;
				case SelectionDirection.South:
					if (position1.X < position2.X)
					{
						selector.SelectPrimary(position1 + (Level.East*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.West*amount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.East*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.West*amount));
					}
					break;
				case SelectionDirection.North:
					if (position1.X > position2.X)
					{
						selector.SelectPrimary(position1 + (Level.West*amount));
						if (reverseAmount != 0) selector.SelectSecondary(position2 + (Level.East*amount));
					}
					else
					{
						selector.SelectSecondary(position2 + (Level.West*amount));
						if (reverseAmount != 0) selector.SelectPrimary(position1 + (Level.East*amount));
					}
					break;
			}
		}

		[Command(Description = "Shift the selection area")]
		public void Shift(Player player, int amount, string direction = "me")
		{
			if (amount < 0)
			{
				player.SendMessage($"The amount need to a positive value. Try reversing the direction instead");
				return;
			}

			Expand(player, amount, direction);
			Contract(player, amount, direction);
		}

		[Command(Description = "Outset the selection area")]
		public void Outset(Player player, int amount, string hv = "hv")
		{
			if (hv.Contains("v"))
				Expand(player, amount, amount, SelectionDirection.Up);
			if (hv.Contains("h"))
				Expand(player, amount, amount, SelectionDirection.North);
			if (hv.Contains("h"))
				Expand(player, amount, amount, SelectionDirection.West);
		}

		[Command(Description = "Inset the selection area")]
		public void Inset(Player player, int amount, string hv = "hv")
		{
			if (hv.Contains("v"))
				Contract(player, amount, amount, SelectionDirection.Up);
			if (hv.Contains("h"))
				Contract(player, amount, amount, SelectionDirection.North);
			if (hv.Contains("h"))
				Contract(player, amount, amount, SelectionDirection.West);
		}

		[Command(Description = "Get information about the selection")]
		public void Size(Player player)
		{
			var selector = RegionSelector.GetSelector(player);

			BlockCoordinates max = selector.GetMax();
			BlockCoordinates min = selector.GetMin();
			var size = max - min;
			player.SendMessage($"Cuboid dimensions (max - min): {size}\n" +
			                   $"# of blocks: {size.X*size.Y*size.Z}");
		}

		// Naming states counts only that exact block; a bare name counts it in any state. That is the
		// same choice the separateByData flag used to offer, asked in the block's own terms.
		[Command(Description = "Counts the number of a certain type of block")]
		public string Count(Player player, BlockTypeEnum tileName, BlockStates blockStates = null)
		{
			Block wanted = BlockFactory.GetBlockByName(tileName?.Value);
			if (wanted == null) return $"There is no block called {tileName?.Value}.";

			bool exact = blockStates != null;
			if (exact && !blockStates.TryApplyTo(wanted, out string error)) return error;

			int runtimeId = wanted.GetRuntimeId();
			var selector = RegionSelector.GetSelector(player);

			int count = selector.GetSelectedBlocks().Count(coord =>
			{
				Block block = player.Level.GetBlock(coord);
				return exact ? block.GetRuntimeId() == runtimeId : block.Name == wanted.Name;
			});

			return $"Counted: {count}";
		}

		[Command(Description = "Get the distribution of blocks in the selection")]
		public void Distribution(Player player, bool separateStates = false)
		{
			var selector = RegionSelector.GetSelector(player);

			var selection = selector.GetSelectedBlocks().Select(coord => player.Level.GetBlock(coord)).ToArray();

			Dictionary<string, int> dist = new Dictionary<string, int>();
			foreach (var block in selection)
			{
				string key = separateStates ? $"{block.Name} #{block.GetRuntimeId()}" : block.Name;

				if (dist.ContainsKey(key)) dist[key] = dist[key] + 1;
				else dist.Add(key, 1);
			}

			player.SendMessage($"# total blocks {selection.Length}");

			foreach (var kvp in dist.OrderByDescending(kvp => kvp.Value))
			{
				double pct = ((float) kvp.Value)/selection.Length*100f;
				player.SendMessage($"{kvp.Value}, ({pct :F3}%), {kvp.Key}");
			}
		}

		[Command(Description = "Choose a region selector", Aliases = new[] {"sel", "desel", "deselect"})]
		public void Select(Player player)
		{
			RegionSelector selector = RegionSelector.GetSelector(player);
			selector.Select(new BlockCoordinates(), new BlockCoordinates());
		}

		[Command(Description = "Toggle particle display of current selection", Aliases = new[] {"showsel", "ss", "/ss"})]
		public void ShowSelection(Player player)
		{
			RegionSelector selector = RegionSelector.GetSelector(player);
			selector.ShowSelection = !selector.ShowSelection;
			string toggleMessage = selector.ShowSelection ? "show selections" : "hide selections";

			player.SendMessage($"Set to {toggleMessage}");
		}


		[MethodImpl(MethodImplOptions.NoInlining)]
		public string GetCurrentMethod()
		{
			StackTrace st = new StackTrace();
			StackFrame sf = st.GetFrame(1);

			return sf.GetMethod().Name;
		}

		private BlockCoordinates Convert(BlockPos blockPos, BlockCoordinates playerPos)
		{
			if (blockPos == null) return playerPos;

			BlockCoordinates result = new BlockCoordinates();

			result.X = blockPos.XRelative ? playerPos.X + blockPos.X : blockPos.X;
			result.Y = blockPos.YRelative ? playerPos.Y + blockPos.Y : blockPos.Y;
			result.Z = blockPos.ZRelative ? playerPos.Z + blockPos.Z : blockPos.Z;

			return result;
		}
	}

	public enum SelectionDirection
	{
		Up,
		Down,
		North,
		West,
		South,
		East,
		Me,
		Vertical
	}
}