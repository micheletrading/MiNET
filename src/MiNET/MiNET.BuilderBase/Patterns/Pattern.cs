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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using log4net;
using MiNET.Blocks;
using MiNET.Plugins;
using MiNET.Utils;
using MiNET.Utils.Vectors;

[assembly: InternalsVisibleTo("MiNET.BuilderBase.Tests")]

namespace MiNET.BuilderBase.Patterns
{
	public class Pattern : IParameterSerializer
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Pattern));

		/// <summary>
		///     One block the pattern can hand out, resolved when the pattern was read. Holding the
		///     block rather than a name and a list of states means a bad pattern fails once, while
		///     the player is typing it, instead of quietly placing the wrong thing per position.
		/// </summary>
		internal class BlockDataEntry
		{
			public Block Block { get; set; }
			public int Weight { get; set; } = 100;
			public int Accumulated { get; set; } = 100;
		}

		internal List<BlockDataEntry> BlockList { get; set; } = new List<BlockDataEntry>();
		private Random _random;
		public string OriginalPattern { get; private set; }

		// Used by command handler
		public Pattern()
		{
			_random = new Random();
		}

		public Pattern(Block block)
		{
			BlockList.Add(new BlockDataEntry {Block = block});
			OriginalPattern = block.Name;
			_random = new Random();
		}

		internal BlockDataEntry GetRandomBlock(Random random, List<BlockDataEntry> blockEntries)
		{
			var blocks = blockEntries.OrderBy(entry => entry.Accumulated).ToList();

			if (blocks.Count == 1) return blocks[0];

			double value = random.Next(blocks.Last().Accumulated + 1);

			Log.Debug($"Random value {value:F2}, length={blocks.Count}, high={blocks.Last().Accumulated}");

			return blocks.First(entry => value <= entry.Accumulated);
		}

		public Block Next(BlockCoordinates position)
		{
			BlockDataEntry blockEntry = GetRandomBlock(_random, BlockList);

			// A copy per position: the entry's block is the pattern's, and handing the same instance
			// out twice would have the second placement move the first.
			var block = (Block) blockEntry.Block.Clone();
			block.Coordinates = position;

			return block;
		}

		public virtual void Deserialize(Player player, string currentPattern)
		{
			// See documentation: https://worldedit.enginehub.org/en/latest/usage/general/patterns/

			if (currentPattern.StartsWith("x")) currentPattern = currentPattern.Remove(0, 1); // remove starting x

			OriginalPattern = currentPattern.Trim();

			var patternsEx = new Regex(@",(?![^\[]*])");
			foreach (string pattern in patternsEx.Split(currentPattern.Trim()))
			{
				BlockList.Add(ParseEntry(pattern.Trim()));
			}

			int acc = 0;
			foreach (var entry in BlockList.OrderBy(entry => entry.Weight))
			{
				acc += entry.Weight;
				entry.Accumulated = acc;
			}

			BlockList = BlockList.OrderBy(entry => entry.Accumulated).ToList();
		}

		// weight%name[state=value,state=value], the WorldEdit spelling. No id and no data value:
		// neither can address a block added since the flattening, and a name that is not a block is
		// an error rather than air, so the player hears about it once instead of after the fact.
		private static readonly Regex EntryEx = new Regex(@"^((?<weight>\d+)%)?(?<blockName>[a-zA-Z0-9_:]+)?(?<states>\[[^\]]*\])?$");

		private static BlockDataEntry ParseEntry(string pattern)
		{
			Match match = EntryEx.Match(pattern);
			if (!match.Success) throw new FormatException($"'{pattern}' is not a block pattern.");

			var entry = new BlockDataEntry();

			if (match.Groups["weight"].Success && int.TryParse(match.Groups["weight"].Value, out int weight)) entry.Weight = weight;

			string name = match.Groups["blockName"].Success ? match.Groups["blockName"].Value : "minecraft:air";

			entry.Block = BlockFactory.GetBlockByName(name);
			if (entry.Block == null) throw new FormatException($"'{name}' is not a block.");

			if (match.Groups["states"].Success)
			{
				BlockStates states = BlockStates.Parse(match.Groups["states"].Value);
				if (states != null && !states.TryApplyTo(entry.Block, out string error)) throw new FormatException(error);
			}

			return entry;
		}
	}
}