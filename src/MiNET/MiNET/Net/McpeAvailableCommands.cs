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
using System.IO;
using System.Linq;
using log4net;
using MiNET.Plugins;
using Version = MiNET.Plugins.Version;

namespace MiNET.Net
{
	public class EnumData
	{
		public string Name { get; set; }
		public string[] Values { get; set; }
		public EnumData(string name, string[] values)
		{
			Name = name;
			Values = values;
		}
	}

	/// <summary>
	///     The full wire structure of MCPE_AVAILABLE_COMMANDS, captured field-for-field on decode so a
	///     decoded packet can be re-encoded byte-identical. MiNET's own command-authoring path (building
	///     a packet from <see cref="CommandSet" />, e.g. Player.SendAvailableCommands) doesn't populate
	///     this - see McpeAvailableCommands.AfterEncode, which falls back to deriving the wire structure
	///     from CommandSet when Raw is null.
	/// </summary>
	public class RawCommandData
	{
		public List<string> EnumValues { get; set; } = new List<string>();
		public List<string> ChainedSubcommandValues { get; set; } = new List<string>();
		public List<string> Suffixes { get; set; } = new List<string>();
		public List<RawEnumData> Enums { get; set; } = new List<RawEnumData>();
		public List<RawChainedSubcommand> ChainedSubcommands { get; set; } = new List<RawChainedSubcommand>();
		public List<RawCommandEntry> Commands { get; set; } = new List<RawCommandEntry>();
		public List<RawDynamicEnum> DynamicEnums { get; set; } = new List<RawDynamicEnum>();
		public List<RawConstraint> Constraints { get; set; } = new List<RawConstraint>();
	}

	/// <summary>An "enums" table entry: a name plus raw indices into RawCommandData.EnumValues (kept as indices, not resolved strings, so an out-of-range or duplicate index round-trips exactly).</summary>
	public class RawEnumData
	{
		public string Name { get; set; }
		public List<uint> ValueIndices { get; set; } = new List<uint>();
	}

	public class RawChainedSubcommand
	{
		public string Name { get; set; }
		public List<(uint Index, uint Value)> Entries { get; set; } = new List<(uint, uint)>();
	}

	public class RawCommandEntry
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public ushort Flags { get; set; }
		public string PermissionLevel { get; set; }
		public int AliasEnumIndex { get; set; }
		public List<uint> ChainedSubcommandOffsets { get; set; } = new List<uint>();
		public List<RawOverload> Overloads { get; set; } = new List<RawOverload>();
	}

	public class RawOverload
	{
		public bool IsChaining { get; set; }
		public List<RawParameter> Parameters { get; set; } = new List<RawParameter>();
	}

	public class RawParameter
	{
		public string Name { get; set; }
		public ushort ValueType { get; set; }
		public ushort EnumType { get; set; }
		public bool Optional { get; set; }
		public byte OptionsBitfield { get; set; }
	}

	public class RawDynamicEnum
	{
		public string Name { get; set; }
		public List<string> Values { get; set; } = new List<string>();
	}

	public class RawConstraint
	{
		public int ValueIndex { get; set; }
		public int EnumIndex { get; set; }
		public List<byte> SubConstraints { get; set; } = new List<byte>();
	}

	public partial class McpeAvailableCommands
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(McpeAvailableCommands));

		// Parameter enum_type discriminators (lu16 on the wire).
		private const ushort EnumTypeValid = 16;
		private const ushort EnumTypeEnum = 48;
		private const ushort EnumTypeSuffixed = 256;
		private const ushort EnumTypeSoftEnum = 1040;

		public CommandSet CommandSet { get; set; }

		/// <summary>Full wire structure, populated on decode. Null for packets MiNET builds itself from <see cref="CommandSet" /> - see AfterEncode.</summary>
		public RawCommandData Raw { get; set; }

		// A misread earlier in the stream shows up as an absurd count; fail loudly
		// instead of spinning for seconds and stalling the session thread.
		private static uint GuardCount(uint count, string what)
		{
			if (count > 1_000_000) throw new InvalidDataException($"Unreasonable {what} count: {count}");
			return count;
		}

		partial void AfterDecode()
		{
			CommandSet = new CommandSet();
			var raw = new RawCommandData();
			Raw = raw;

			var enumValues = new List<string>();
			uint valueCount = GuardCount(ReadUnsignedVarInt(), "enum value");
			for (int i = 0; i < valueCount; i++)
			{
				string value = ReadString();
				enumValues.Add(value);
				raw.EnumValues.Add(value);
			}

			uint chainedValueCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand value");
			for (int i = 0; i < chainedValueCount; i++)
			{
				raw.ChainedSubcommandValues.Add(ReadString());
			}

			uint suffixCount = GuardCount(ReadUnsignedVarInt(), "suffix");
			for (int i = 0; i < suffixCount; i++)
			{
				raw.Suffixes.Add(ReadString());
			}

			uint enumCount = GuardCount(ReadUnsignedVarInt(), "enum");
			var enums = new EnumData[enumCount];
			for (int i = 0; i < enumCount; i++)
			{
				string enumName = ReadString();
				uint enumValueCount = GuardCount(ReadUnsignedVarInt(), "enum member");
				string[] values = new string[enumValueCount];
				var rawEnum = new RawEnumData {Name = enumName};
				for (int j = 0; j < enumValueCount; j++)
				{
					uint idx = ReadUint(); // always lu32 in current protocol
					values[j] = idx < enumValues.Count ? enumValues[(int) idx] : null;
					rawEnum.ValueIndices.Add(idx);
				}

				enums[i] = new EnumData(enumName, values);
				raw.Enums.Add(rawEnum);
			}

			uint chainedSubcommandCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand");
			for (int i = 0; i < chainedSubcommandCount; i++)
			{
				var rawChained = new RawChainedSubcommand {Name = ReadString()};
				uint valuesCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand entry");
				for (int j = 0; j < valuesCount; j++)
				{
					uint index = ReadUnsignedVarInt();
					uint value = ReadUnsignedVarInt();
					rawChained.Entries.Add((index, value));
				}
				raw.ChainedSubcommands.Add(rawChained);
			}

			uint commandCount = GuardCount(ReadUnsignedVarInt(), "command");
			for (int i = 0; i < commandCount; i++)
			{
				var command = new Command();
				command.Versions = new Version[1];

				string commandName = ReadString();
				string description = ReadString();
				ushort flags = ReadUshort();
				string permissionLevel = ReadString();
				int aliasEnumIndex = ReadInt();

				var rawCommand = new RawCommandEntry
				{
					Name = commandName,
					Description = description,
					Flags = flags,
					PermissionLevel = permissionLevel,
					AliasEnumIndex = aliasEnumIndex
				};
				raw.Commands.Add(rawCommand);

				uint offsetCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand offset");
				for (int j = 0; j < offsetCount; j++)
				{
					rawCommand.ChainedSubcommandOffsets.Add(ReadUint());
				}

				command.Name = commandName;

				var version = new Version();
				version.Description = description;
				version.Overloads = new Dictionary<string, Overload>();

				uint overloadCount = GuardCount(ReadUnsignedVarInt(), "overload");
				for (int j = 0; j < overloadCount; j++)
				{
					var overload = new Overload();
					overload.Input = new Input();

					bool isChaining = ReadBool();
					var rawOverload = new RawOverload {IsChaining = isChaining};
					rawCommand.Overloads.Add(rawOverload);

					uint parameterCount = GuardCount(ReadUnsignedVarInt(), "parameter");
					overload.Input.Parameters = new Parameter[parameterCount];
					for (int k = 0; k < parameterCount; k++)
					{
						string parameterName = ReadString();
						ushort valueType = ReadUshort();
						ushort enumType = ReadUshort();
						bool optional = ReadBool();
						byte optionsBitfield = ReadByte();

						rawOverload.Parameters.Add(new RawParameter
						{
							Name = parameterName,
							ValueType = valueType,
							EnumType = enumType,
							Optional = optional,
							OptionsBitfield = optionsBitfield
						});

						var parameter = new Parameter()
						{
							Name = parameterName,
							Optional = optional,
							Type = GetParameterTypeName(valueType)
						};

						if (enumType == EnumTypeEnum && valueType < enums.Length)
						{
							EnumData paramEnum = enums[valueType];
							parameter.EnumValues = paramEnum.Values;
							parameter.EnumType = paramEnum.Name;
							parameter.Type = "stringenum";
						}
						else if (enumType == EnumTypeSoftEnum)
						{
							parameter.Type = "softenum";
						}

						overload.Input.Parameters[k] = parameter;
					}

					version.Overloads.Add(j.ToString(), overload);
				}

				command.Versions[0] = version;
				if (!CommandSet.ContainsKey(commandName)) CommandSet.Add(commandName, command);
			}

			uint dynamicEnumCount = GuardCount(ReadUnsignedVarInt(), "dynamic enum");
			for (int i = 0; i < dynamicEnumCount; i++)
			{
				var rawDynamicEnum = new RawDynamicEnum {Name = ReadString()};
				uint dynamicValueCount = GuardCount(ReadUnsignedVarInt(), "dynamic enum value");
				for (int j = 0; j < dynamicValueCount; j++)
				{
					rawDynamicEnum.Values.Add(ReadString());
				}
				raw.DynamicEnums.Add(rawDynamicEnum);
			}

			uint constraintCount = GuardCount(ReadUnsignedVarInt(), "enum constraint");
			for (int i = 0; i < constraintCount; i++)
			{
				var rawConstraint = new RawConstraint
				{
					ValueIndex = ReadInt(),
					EnumIndex = ReadInt()
				};
				uint subCount = GuardCount(ReadUnsignedVarInt(), "constraint entry");
				for (int j = 0; j < subCount; j++)
				{
					rawConstraint.SubConstraints.Add(ReadByte());
				}
				raw.Constraints.Add(rawConstraint);
			}
		}

		partial void AfterEncode()
		{
			try
			{
				if (Raw != null)
				{
					WriteRaw(Raw);
					return;
				}

				if (CommandSet == null || CommandSet.Count == 0)
				{
					Log.Warn("No commands to send");
					WriteUnsignedVarInt(0); // enum values
					WriteUnsignedVarInt(0); // chained subcommand values
					WriteUnsignedVarInt(0); // suffixes
					WriteUnsignedVarInt(0); // enums
					WriteUnsignedVarInt(0); // chained subcommands
					WriteUnsignedVarInt(0); // commands
					WriteUnsignedVarInt(0); // dynamic enums
					WriteUnsignedVarInt(0); // constraints
					return;
				}

				var commands = CommandSet;

				var stringList = new List<string>();
				foreach (Command command in commands.Values)
				{
					var aliases = command.Versions[0].Aliases.Concat(new[] {command.Name}).ToArray();
					foreach (string alias in aliases)
					{
						if (!stringList.Contains(alias)) stringList.Add(alias);
					}

					foreach (Overload overload in command.Versions[0].Overloads.Values)
					{
						Parameter[] parameters = overload.Input.Parameters;
						if (parameters == null) continue;
						foreach (Parameter parameter in parameters)
						{
							if (parameter.Type != "stringenum" || parameter.EnumValues == null) continue;
							foreach (string enumValue in parameter.EnumValues)
							{
								if (!stringList.Contains(enumValue)) stringList.Add(enumValue);
							}
						}
					}
				}

				WriteUnsignedVarInt((uint) stringList.Count); // enum values
				foreach (string s in stringList)
				{
					Write(s);
				}

				WriteUnsignedVarInt(0); // chained subcommand values
				WriteUnsignedVarInt(0); // suffixes

				var enumList = new List<string>();
				foreach (Command command in commands.Values)
				{
					if (command.Versions[0].Aliases.Length > 0)
					{
						string aliasEnum = command.Name + "CommandAliases";
						if (!enumList.Contains(aliasEnum)) enumList.Add(aliasEnum);
					}

					foreach (Overload overload in command.Versions[0].Overloads.Values)
					{
						Parameter[] parameters = overload.Input.Parameters;
						if (parameters == null) continue;
						foreach (Parameter parameter in parameters)
						{
							if (parameter.Type != "stringenum" || parameter.EnumValues == null) continue;
							if (!enumList.Contains(parameter.EnumType)) enumList.Add(parameter.EnumType);
						}
					}
				}

				WriteUnsignedVarInt((uint) enumList.Count); // enums
				var writtenEnumList = new List<string>();
				foreach (Command command in commands.Values)
				{
					if (command.Versions[0].Aliases.Length > 0)
					{
						var aliases = command.Versions[0].Aliases.Concat(new[] {command.Name}).ToArray();
						string aliasEnum = command.Name + "CommandAliases";
						if (!enumList.Contains(aliasEnum)) continue;
						if (writtenEnumList.Contains(aliasEnum)) continue;
						writtenEnumList.Add(aliasEnum);

						Write(aliasEnum);
						WriteUnsignedVarInt((uint) aliases.Length);
						foreach (string enumValue in aliases)
						{
							int idx = stringList.IndexOf(enumValue);
							if (idx < 0) Log.Error($"Expected enum value: {enumValue} in string list, but didn't find it.");
							Write((uint) idx); // always lu32 in current protocol
						}
					}

					foreach (Overload overload in command.Versions[0].Overloads.Values)
					{
						Parameter[] parameters = overload.Input.Parameters;
						if (parameters == null) continue;
						foreach (Parameter parameter in parameters)
						{
							if (parameter.Type != "stringenum" || parameter.EnumValues == null) continue;
							if (!enumList.Contains(parameter.EnumType)) continue;
							if (writtenEnumList.Contains(parameter.EnumType)) continue;
							writtenEnumList.Add(parameter.EnumType);

							Write(parameter.EnumType);
							WriteUnsignedVarInt((uint) parameter.EnumValues.Length);
							foreach (string enumValue in parameter.EnumValues)
							{
								int idx = stringList.IndexOf(enumValue);
								if (idx < 0) Log.Error($"Expected enum value: {enumValue} in string list, but didn't find it.");
								Write((uint) idx);
							}
						}
					}
				}

				WriteUnsignedVarInt(0); // chained subcommands

				WriteUnsignedVarInt((uint) commands.Count);
				foreach (Command command in commands.Values)
				{
					Write(command.Name);
					Write(command.Versions[0].Description);
					Write((ushort) 0); // flags
					Write(GetPermissionLevelName((CommandPermission) command.Versions[0].CommandPermission));

					if (command.Versions[0].Aliases.Length > 0)
					{
						string aliasEnum = command.Name + "CommandAliases";
						Write(enumList.IndexOf(aliasEnum));
					}
					else
					{
						Write(-1); // alias enum index
					}

					WriteUnsignedVarInt(0); // chained subcommand offsets

					var overloads = command.Versions[0].Overloads;
					WriteUnsignedVarInt((uint) overloads.Count);
					foreach (Overload overload in overloads.Values)
					{
						Write(false); // is chaining

						Parameter[] parameters = overload.Input.Parameters;
						if (parameters == null)
						{
							WriteUnsignedVarInt(0);
							continue;
						}

						WriteUnsignedVarInt((uint) parameters.Length);
						foreach (Parameter parameter in parameters)
						{
							Write(parameter.Name);
							if (parameter.Type == "stringenum" && parameter.EnumValues != null)
							{
								Write((ushort) enumList.IndexOf(parameter.EnumType));
								Write(EnumTypeEnum);
							}
							else if (parameter.Type == "softenum" && parameter.EnumValues != null)
							{
								Write((ushort) 0);
								Write(EnumTypeSoftEnum);
							}
							else
							{
								Write((ushort) GetParameterTypeId(parameter.Type));
								Write(EnumTypeValid);
							}

							Write(parameter.Optional);
							Write((byte) 0); // options bitfield
						}
					}
				}

				WriteUnsignedVarInt(1); // dynamic (soft) enums
				Write("CmdSoftEnumValues");
				WriteUnsignedVarInt(0);

				WriteUnsignedVarInt(0); // constraints
			}
			catch (Exception e)
			{
				Log.Error("Sending commands", e);
				//throw;
			}
		}

		// Mirrors AfterDecode field-for-field, in read order, from the raw structure captured there -
		// used for packets that came off the wire, so they re-encode byte-identical. See AfterEncode's
		// CommandSet-based path below for packets MiNET builds itself.
		private void WriteRaw(RawCommandData raw)
		{
			WriteUnsignedVarInt((uint) raw.EnumValues.Count);
			foreach (string value in raw.EnumValues)
			{
				Write(value);
			}

			WriteUnsignedVarInt((uint) raw.ChainedSubcommandValues.Count);
			foreach (string value in raw.ChainedSubcommandValues)
			{
				Write(value);
			}

			WriteUnsignedVarInt((uint) raw.Suffixes.Count);
			foreach (string value in raw.Suffixes)
			{
				Write(value);
			}

			WriteUnsignedVarInt((uint) raw.Enums.Count);
			foreach (RawEnumData rawEnum in raw.Enums)
			{
				Write(rawEnum.Name);
				WriteUnsignedVarInt((uint) rawEnum.ValueIndices.Count);
				foreach (uint idx in rawEnum.ValueIndices)
				{
					Write(idx); // always lu32 in current protocol
				}
			}

			WriteUnsignedVarInt((uint) raw.ChainedSubcommands.Count);
			foreach (RawChainedSubcommand chained in raw.ChainedSubcommands)
			{
				Write(chained.Name);
				WriteUnsignedVarInt((uint) chained.Entries.Count);
				foreach ((uint index, uint value) in chained.Entries)
				{
					WriteUnsignedVarInt(index);
					WriteUnsignedVarInt(value);
				}
			}

			WriteUnsignedVarInt((uint) raw.Commands.Count);
			foreach (RawCommandEntry command in raw.Commands)
			{
				Write(command.Name);
				Write(command.Description);
				Write(command.Flags);
				Write(command.PermissionLevel);
				Write(command.AliasEnumIndex);

				WriteUnsignedVarInt((uint) command.ChainedSubcommandOffsets.Count);
				foreach (uint offset in command.ChainedSubcommandOffsets)
				{
					Write(offset);
				}

				WriteUnsignedVarInt((uint) command.Overloads.Count);
				foreach (RawOverload overload in command.Overloads)
				{
					Write(overload.IsChaining);

					WriteUnsignedVarInt((uint) overload.Parameters.Count);
					foreach (RawParameter parameter in overload.Parameters)
					{
						Write(parameter.Name);
						Write(parameter.ValueType);
						Write(parameter.EnumType);
						Write(parameter.Optional);
						Write(parameter.OptionsBitfield);
					}
				}
			}

			WriteUnsignedVarInt((uint) raw.DynamicEnums.Count);
			foreach (RawDynamicEnum dynamicEnum in raw.DynamicEnums)
			{
				Write(dynamicEnum.Name);
				WriteUnsignedVarInt((uint) dynamicEnum.Values.Count);
				foreach (string value in dynamicEnum.Values)
				{
					Write(value);
				}
			}

			WriteUnsignedVarInt((uint) raw.Constraints.Count);
			foreach (RawConstraint constraint in raw.Constraints)
			{
				Write(constraint.ValueIndex);
				Write(constraint.EnumIndex);
				WriteUnsignedVarInt((uint) constraint.SubConstraints.Count);
				foreach (byte b in constraint.SubConstraints)
				{
					Write(b);
				}
			}
		}

		private static string GetPermissionLevelName(CommandPermission permission)
		{
			return permission switch
			{
				CommandPermission.Normal => "Any",
				CommandPermission.Operator => "GameDirectors",
				CommandPermission.Host => "Host",
				CommandPermission.Automation => "Admin",
				CommandPermission.Admin => "Owner",
				_ => "Any"
			};
		}

		/// <summary>
		///     What a command parameter's type is called on the wire. Mojang inserts types into the
		///     middle of this enum, so everything above 23 has been renumbered at least once since
		///     these were written: a stale id reaches the client as a type it cannot name and the
		///     client renders the whole argument as "unknown", which is what every string parameter
		///     did before this was corrected. Values are PMMP BedrockProtocol's at protocol 1001,
		///     CommandParameterTypes.php.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, int> ParameterTypeIds = new Dictionary<string, int>
		{
			["int"] = 1,
			["float"] = 3,
			["mixed"] = 4,
			["wildcardint"] = 5,
			["operator"] = 6,
			["commandoperator"] = 7,
			["target"] = 8,
			["wildcardtarget"] = 10,
			["filename"] = 17,
			["integerrange"] = 23,
			["equipmentslots"] = 47,
			["string"] = 56,
			["blockpos"] = 64,
			["entitypos"] = 65,
			["message"] = 68,
			["rawtext"] = 70,
			["json"] = 74,
			["blockstates"] = 84,
			["timemarker"] = 86,
			["codebuilderargs"] = 88
		};

		// One table read both ways, because two switches over the same values is how the encode and
		// decode sides came to disagree.
		private static readonly IReadOnlyDictionary<int, string> ParameterTypeNames = ParameterTypeIds.ToDictionary(pair => pair.Value, pair => pair.Key);

		/// <summary>The wire id for a parameter type name, 0 for a name with no type.</summary>
		public static int GetParameterTypeId(string type)
		{
			return type != null && ParameterTypeIds.TryGetValue(type, out int id) ? id : 0;
		}

		/// <summary>The parameter type name for a wire id, "unknown" for an id we have no name for.</summary>
		public static string GetParameterTypeName(int type)
		{
			return ParameterTypeNames.TryGetValue(type, out string name) ? name : "unknown";
		}
	}
}
