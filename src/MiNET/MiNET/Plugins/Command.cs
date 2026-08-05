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

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using MiNET.Blocks;
using MiNET.Entities;
using MiNET.Utils;
using MiNET.Utils.Vectors;
using Newtonsoft.Json;

namespace MiNET.Plugins
{
	public class CommandSet : Dictionary<string, Command>
	{
	}

	public class Command
	{
		[JsonIgnore] public string Name { get; set; }

		public Version[] Versions { get; set; }
	}

	public class Version
	{
		[JsonProperty(propertyName: "version")]
		public int CommandVersion { get; set; }

		public string[] Aliases { get; set; }
		public string Description { get; set; }
		public string Permission { get; set; }
		public int CommandPermission { get; set; }
		public string ErrorMessage { get; set; }
		public bool RequiresTellPerms { get; set; }
		public bool RequiresChatPerms { get; set; }
		public bool OutputToSpeech { get; set; }

		[JsonProperty(propertyName: "requires_edu")]
		public bool RequiresEdu { get; set; }

		[JsonProperty(propertyName: "allows_indirect_exec")]
		public bool AllowsIndirectExec { get; set; }

		[JsonProperty(propertyName: "is_hidden")]
		public bool IsHidden { get; set; }

		public Dictionary<string, Overload> Overloads { get; set; }
	}


	public class Overload
	{
		[JsonIgnore] public MethodInfo Method { get; set; }

		[JsonIgnore] public string Description { get; set; }

		public Input Input { get; set; }
		public Parser Parser { get; set; }
	}

	public class Input
	{
		public Parameter[] Parameters { get; set; }
	}

	public class Output
	{
		[JsonProperty(propertyName: "format_strings")]
		public FormatString[] FormatStrings { get; set; }

		public Parameter[] Parameters { get; set; }
	}

	public class FormatString
	{
		public string Color { get; set; }
		public string Format { get; set; }

		[JsonProperty(propertyName: "params_to_use")]
		public string[] ParamsToUse { get; set; }

		[JsonProperty(propertyName: "should_show")]
		public FormatRule ShouldShow { get; set; }
	}

	public class FormatRule
	{
		[JsonProperty(propertyName: "not_empty")]
		public string[] NotEmpty { get; set; }

		[JsonProperty(propertyName: "is_true")]
		public string[] IsTrue { get; set; }
	}

	public class Parser
	{
		public string Tokens { get; set; }
	}

	public class Parameter
	{
		public string Name { get; set; }
		public string Type { get; set; }

		[JsonProperty(propertyName: "enum_type")]
		public string EnumType { get; set; }

		[JsonProperty(propertyName: "enum_values")]
		public string[] EnumValues { get; set; }

		public bool Optional { get; set; }

		[JsonProperty(propertyName: "target_data")]
		public TargetData TargetData { get; set; }
	}

	public class TargetData
	{
		[JsonProperty(propertyName: "players_only")]
		public bool PlayersOnly { get; set; }

		[JsonProperty(propertyName: "main_target")]
		public bool MainTarget { get; set; }

		[JsonProperty(propertyName: "allow_dead_players")]
		public bool AllowDeadPlayers { get; set; }
	}


	public class BlockPos
	{
		public int X { get; set; }
		public bool XRelative { get; set; }

		public int Y { get; set; }
		public bool YRelative { get; set; }

		public int Z { get; set; }
		public bool ZRelative { get; set; }

		public override string ToString()
		{
			return $"{nameof(X)}: {X}, {nameof(XRelative)}: {XRelative}, {nameof(Y)}: {Y}, {nameof(YRelative)}: {YRelative}, {nameof(Z)}: {Z}, {nameof(ZRelative)}: {ZRelative}";
		}

		public static BlockPos Parse(string x, string y, string z)
		{
			var position = new BlockPos();

			x = RelValue.StripRelative(x, out bool xRelative);
			position.XRelative = xRelative;
			int.TryParse(x, out int parsedX);
			position.X = parsedX;

			y = RelValue.StripRelative(y, out bool yRelative);
			position.YRelative = yRelative;
			int.TryParse(y, out int parsedY);
			position.Y = parsedY;

			z = RelValue.StripRelative(z, out bool zRelative);
			position.ZRelative = zRelative;
			int.TryParse(z, out int parsedZ);
			position.Z = parsedZ;

			return position;
		}

		/// <summary>Where this points, with anything written as ~ taken from the given origin.</summary>
		public BlockCoordinates ToCoordinates(BlockCoordinates origin)
		{
			return new BlockCoordinates(
				XRelative ? origin.X + X : X,
				YRelative ? origin.Y + Y : Y,
				ZRelative ? origin.Z + Z : Z);
		}
	}

	/// <summary>
	///     The blockStates argument of /setblock and /fill, written ["state"=value,"state"=value].
	///     A state name is always quoted; a value is quoted when it is a string and bare when it is a
	///     boolean or a number, and that is the only thing saying which kind of state it is. What the
	///     player leaves out keeps the block's default, so a partial literal is a whole answer.
	/// </summary>
	public class BlockStates
	{
		public List<IBlockState> States { get; } = new List<IBlockState>();

		public static BlockStates Parse(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			string body = value.Trim().TrimStart('[').TrimEnd(']');

			var states = new BlockStates();
			if (string.IsNullOrWhiteSpace(body)) return states;

			foreach (string pair in SplitPairs(body))
			{
				int equals = pair.IndexOf('=');
				if (equals < 0) continue;

				string name = pair.Substring(0, equals).Trim().Trim('"');
				string raw = pair.Substring(equals + 1).Trim();
				if (name.Length == 0) continue;

				states.States.Add(ToState(name, raw));
			}

			return states;
		}

		/// <summary>
		///     Applies these states to the block and says whether they were real. SetState ignores a
		///     state name the block does not have and a value outside its range, so without this a
		///     typo silently places the default block: the player asked for something and got
		///     something else with nothing said. The resolve half of the parse, and it needs the
		///     block for the same reason a target needs the level.
		/// </summary>
		public bool TryApplyTo(Block block, out string error)
		{
			error = null;
			if (block == null) return false;

			BlockStateContainer defaults = block.GetState();
			if (defaults == null)
			{
				error = $"{block.Name} has no states";
				return States.Count == 0;
			}

			foreach (IBlockState state in States)
			{
				if (!defaults.States.Exists(known => known.Name == state.Name))
				{
					error = $"{block.Name} has no state called {state.Name}";
					return false;
				}
			}

			block.SetState(States);

			// SetState takes a value without judging it, so asking the block what it now holds
			// proves nothing. The palette is what decides: a state value or combination that is not
			// in it is not a block, and would be sent as an id the client cannot resolve.
			if (BlockFactory.BlockStates.Contains(block.GetState())) return true;

			error = $"{block.Name} has no state {ToString()}";
			return false;
		}

		private static IBlockState ToState(string name, string raw)
		{
			if (raw.StartsWith("\"")) return new BlockStateString {Name = name, Value = raw.Trim('"')};
			if (bool.TryParse(raw, out bool flag)) return new BlockStateByte {Name = name, Value = (byte) (flag ? 1 : 0)};
			if (int.TryParse(raw, out int number)) return new BlockStateInt {Name = name, Value = number};

			return new BlockStateString {Name = name, Value = raw};
		}

		// A comma inside a quoted value is part of the value, not a separator.
		private static IEnumerable<string> SplitPairs(string body)
		{
			bool inQuotes = false;
			int start = 0;

			for (int i = 0; i < body.Length; i++)
			{
				if (body[i] == '"') inQuotes = !inQuotes;
				else if (body[i] == ',' && !inQuotes)
				{
					yield return body.Substring(start, i - start);
					start = i + 1;
				}
			}

			yield return body.Substring(start);
		}

		public override string ToString()
		{
			return $"[{string.Join(",", States.ConvertAll(state => $"\"{state.Name}\"={state}"))}]";
		}
	}

	public class EntityPos
	{
		public double X { get; set; }
		public bool XRelative { get; set; }

		public double Y { get; set; }
		public bool YRelative { get; set; }

		public double Z { get; set; }
		public bool ZRelative { get; set; }

		public static EntityPos Parse(string x, string y, string z)
		{
			RelValue parsedX = RelValue.Parse(x);
			RelValue parsedY = RelValue.Parse(y);
			RelValue parsedZ = RelValue.Parse(z);

			return new EntityPos
			{
				X = parsedX.Value,
				XRelative = parsedX.Relative,
				Y = parsedY.Value,
				YRelative = parsedY.Relative,
				Z = parsedZ.Value,
				ZRelative = parsedZ.Relative
			};
		}

		public override string ToString()
		{
			return $"{nameof(X)}: {X}, {nameof(XRelative)}: {XRelative}, {nameof(Y)}: {Y}, {nameof(YRelative)}: {YRelative}, {nameof(Z)}: {Z}, {nameof(ZRelative)}: {ZRelative}";
		}
	}

	public class RelValue
	{
		public double Value { get; set; }
		public bool Relative { get; set; }

		public static RelValue Parse(string value)
		{
			var result = new RelValue();

			value = StripRelative(value, out bool relative);
			result.Relative = relative;

			double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out double parsed);
			result.Value = parsed;

			return result;
		}

		/// <summary>
		///     Takes the ~ off a coordinate and says whether it was there. Shared by everything that
		///     reads a position, because ~ means the same thing in all of them.
		/// </summary>
		public static string StripRelative(string value, out bool relative)
		{
			relative = value != null && value.StartsWith("~");

			return relative ? value.Substring(1) : value;
		}

		public override string ToString()
		{
			return $"{nameof(Value)}: {Value}, {nameof(Relative)}: {Relative}";
		}
	}

	public class Target
	{
		public class Rule
		{
			public bool Inverted { get; set; }
			public string Name { get; set; }
			public string Value { get; set; }
		}

		public Rule[] Rules { get; set; }
		public string Selector { get; set; }

		public Player[] Players { get; set; }
		public Entity[] Entities { get; set; }

		/// <summary>
		///     Reads a selector, @a[r=10] or a bare player name. This is the text half only; turning
		///     the selector into actual players or entities needs the level and lives in
		///     PluginManager.FillTargets.
		/// </summary>
		public static Target Parse(string source)
		{
			Target target = new Target();
			if (!source.StartsWith("@"))
			{
				target.Selector = "closestPlayer";
				target.Rules = new[]
				{
					new Rule
					{
						Name = "name",
						Value = source
					}
				};
			}
			else
			{
				var matches = Regex.Matches(source, @"^(?<selector>@[aeprs])(\[((?<args>(c|dx|dy|dz|l|lm|m|name|r|rm|rx|rxm|rym|type|x|y|z)=.*?)(,*?))*\])*$");
				var selector = matches[0].Groups["selector"].Captures[0].Value;
				switch (selector)
				{
					case "@a":
						selector = "allPlayers";
						break;
					case "@e":
						selector = "allEntities";
						break;
					case "@p":
						selector = "closestPlayer";
						break;
					case "@r":
						selector = "randomPlayer";
						break;
					case "@s":
						selector = "yourself";
						break;
				}
				target.Selector = selector;
				List<Rule> rules = new List<Rule>();
				foreach (Capture arg in matches[0].Groups["args"].Captures)
				{
					string[] split = arg.Value.Split('=');
					string name = split[0];
					string value = split[1];

					Rule rule = new Rule();
					rule.Name = name;
					if (value.StartsWith("!"))
					{
						rule.Inverted = true;
						rule.Value = value.Substring(1);
					}
					else
					{
						rule.Value = value;
					}

					rules.Add(rule);
				}


				if (rules.Count != 0) target.Rules = rules.ToArray();
			}

			return target;
		}

		public override string ToString()
		{
			string body = string.Empty;

			if (Players != null)
			{
				var names = new List<string>();
				foreach (var p in Players)
				{
					names.Add(p.Username);
				}
				body = string.Join(", ", names);
			}

			return body;
		}
	}

	public abstract class SoftEnumBase
	{
	}

	public class TestSoftEnum : SoftEnumBase
	{

	}

	public abstract class EnumBase
	{
		public string Value { get; set; }
	}

	// enchantmentType
	public class EnchantmentTypeEnum : EnumBase
	{
	}

	// dimension
	public class DimensionEnum : EnumBase
	{
	}

	// itemType
	public class ItemTypeEnum : EnumBase
	{
	}

	// commandName
	public class CommandNameEnum : EnumBase
	{
	}

	// entityType
	public class EntityTypeEnum : EnumBase
	{
	}

	// blockType
	public class BlockTypeEnum : EnumBase
	{
	}

	public class EffectEnum : EnumBase
	{
	}

	public class EnchantEnum : EnumBase
	{
	}

	public class FeatureEnum : EnumBase
	{
	}


	//"rules": [
	//    {
	//    "inverted": false,
	//    "name": "name",
	//    "value": "gurunx"
	//	}
	//],
	//"selector": "nearestPlayer"
}