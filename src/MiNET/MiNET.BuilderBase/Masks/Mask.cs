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
using System.Text.RegularExpressions;
using log4net;
using MiNET.Blocks;
using MiNET.Plugins;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using MiNET.Worlds;

namespace MiNET.BuilderBase.Masks
{
	public class Mask : IParameterSerializer
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (Mask));

		/// <summary>
		///     A block the mask matches. A bare name matches the block whatever state it is in, which
		///     is what "oak_log" should mean; naming states pins it to that one runtime id. Those are
		///     the two things the metadata flag used to stand for.
		/// </summary>
		private class BlockDataEntry
		{
			public string Name { get; set; }
			public int RuntimeId { get; set; } = AnyState;
		}

		private const int AnyState = -1;

		private class MaskEntry
		{
			public bool AboveOnly { get; set; }
			public bool BelowOnly { get; set; }
			public bool Inverted { get; set; }

			public List<BlockDataEntry> BlockList = new List<BlockDataEntry>();
		}

		public Level Level { get; set; }
		public string OriginalMask { get; set; }

		MaskEntry[] _masks = new MaskEntry[0];

		// Used by command handler
		public Mask()
		{
		}

		public Mask(Level level, List<Block> blocks, bool anyState)
		{
			Level = level;

			MaskEntry entry = new MaskEntry();

			foreach (var block in blocks)
			{
				entry.BlockList.Add(new BlockDataEntry {Name = block.Name, RuntimeId = anyState ? AnyState : block.GetRuntimeId()});
			}

			_masks = new[] {entry};
		}

		public virtual bool Test(BlockCoordinates coordinates)
		{
			foreach (var mask in _masks)
			{
				if (!Test(coordinates, mask)) return false;
			}

			return true;
		}

		private bool Test(BlockCoordinates coordinates, MaskEntry mask)
		{
			if (Level == null) return true;

			if (mask.AboveOnly)
			{
				coordinates += BlockCoordinates.Down;
			}
			else if (mask.BelowOnly)
			{
				coordinates += BlockCoordinates.Up;
			}

			Block block = Level.GetBlock(coordinates);

			var matches = mask.BlockList.Exists(entry => entry.RuntimeId == AnyState
				? entry.Name == block.Name
				: entry.RuntimeId == block.GetRuntimeId());

			if (mask.Inverted)
			{
				return !matches;
			}

			return matches;
		}

		public virtual void Deserialize(Player player, string input)
		{
			Level = player.Level;

			// air,oak_log,oak_log[pillar_axis=x]
			// A bare name matches any state of that block; naming states matches only those.

			// TODO: #existing, #region

			if (input.StartsWith("x")) input = input.Remove(0, 1); // remove starting x

			OriginalMask = input;

			string[] inputs = input.Split(' ');

			_masks = new MaskEntry[inputs.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
				MaskEntry entry = new MaskEntry();
				_masks[i] = entry;

				string currentPattern = inputs[i];

				if (currentPattern.StartsWith(">")) // Only place above certain blocks
				{
					entry.AboveOnly = true;
					currentPattern = currentPattern.Remove(0, 1); // remove starting x
				}
				else if (currentPattern.StartsWith("<")) // Only place below certain blocks
				{
					entry.BelowOnly = true;
					currentPattern = currentPattern.Remove(0, 1); // remove starting x
				}
				else if (currentPattern.StartsWith("!")) // Only place if NOT certain blocks
				{
					entry.Inverted = true;
					currentPattern = currentPattern.Remove(0, 1); // remove starting x
				}

				// A comma inside [..] separates states, not blocks.
				foreach (string pattern in Regex.Split(currentPattern, @",(?![^\[]*])"))
				{
					entry.BlockList.Add(ParseEntry(pattern.Trim()));
				}
			}
		}

		private static readonly Regex EntryEx = new Regex(@"^(?<blockName>[a-zA-Z0-9_:]+)(?<states>\[[^\]]*\])?$");

		private static BlockDataEntry ParseEntry(string pattern)
		{
			Match match = EntryEx.Match(pattern);
			if (!match.Success) throw new FormatException($"'{pattern}' is not a block.");

			string name = match.Groups["blockName"].Value;

			Block block = BlockFactory.GetBlockByName(name);
			if (block == null) throw new FormatException($"'{name}' is not a block.");

			var entry = new BlockDataEntry {Name = block.Name};

			if (match.Groups["states"].Success)
			{
				BlockStates states = BlockStates.Parse(match.Groups["states"].Value);
				if (states != null && !states.TryApplyTo(block, out string error)) throw new FormatException(error);

				entry.RuntimeId = block.GetRuntimeId();
			}

			return entry;
		}
	}
}