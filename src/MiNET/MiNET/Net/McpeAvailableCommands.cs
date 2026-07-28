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

	public partial class McpeAvailableCommands
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(McpeAvailableCommands));

		// Parameter enum_type discriminators (lu16 on the wire).
		private const ushort EnumTypeValid = 16;
		private const ushort EnumTypeEnum = 48;
		private const ushort EnumTypeSuffixed = 256;
		private const ushort EnumTypeSoftEnum = 1040;

		public CommandSet CommandSet { get; set; }

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

			var enumValues = new List<string>();
			uint valueCount = GuardCount(ReadUnsignedVarInt(), "enum value");
			for (int i = 0; i < valueCount; i++)
			{
				enumValues.Add(ReadString());
			}

			uint chainedValueCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand value");
			for (int i = 0; i < chainedValueCount; i++)
			{
				ReadString();
			}

			uint suffixCount = GuardCount(ReadUnsignedVarInt(), "suffix");
			for (int i = 0; i < suffixCount; i++)
			{
				ReadString();
			}

			uint enumCount = GuardCount(ReadUnsignedVarInt(), "enum");
			var enums = new EnumData[enumCount];
			for (int i = 0; i < enumCount; i++)
			{
				string enumName = ReadString();
				uint enumValueCount = GuardCount(ReadUnsignedVarInt(), "enum member");
				string[] values = new string[enumValueCount];
				for (int j = 0; j < enumValueCount; j++)
				{
					uint idx = ReadUint(); // always lu32 in current protocol
					values[j] = idx < enumValues.Count ? enumValues[(int) idx] : null;
				}

				enums[i] = new EnumData(enumName, values);
			}

			uint chainedSubcommandCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand");
			for (int i = 0; i < chainedSubcommandCount; i++)
			{
				ReadString(); // name
				uint valuesCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand entry");
				for (int j = 0; j < valuesCount; j++)
				{
					ReadUnsignedVarInt(); // index
					ReadUnsignedVarInt(); // value
				}
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

				uint offsetCount = GuardCount(ReadUnsignedVarInt(), "chained subcommand offset");
				for (int j = 0; j < offsetCount; j++)
				{
					ReadUint();
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

					ReadBool(); // is chaining

					uint parameterCount = GuardCount(ReadUnsignedVarInt(), "parameter");
					overload.Input.Parameters = new Parameter[parameterCount];
					for (int k = 0; k < parameterCount; k++)
					{
						string parameterName = ReadString();
						ushort valueType = ReadUshort();
						ushort enumType = ReadUshort();
						bool optional = ReadBool();
						ReadByte(); // options bitfield

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
				ReadString(); // name
				uint dynamicValueCount = GuardCount(ReadUnsignedVarInt(), "dynamic enum value");
				for (int j = 0; j < dynamicValueCount; j++)
				{
					ReadString();
				}
			}

			uint constraintCount = GuardCount(ReadUnsignedVarInt(), "enum constraint");
			for (int i = 0; i < constraintCount; i++)
			{
				ReadInt(); // value index
				ReadInt(); // enum index
				uint subCount = GuardCount(ReadUnsignedVarInt(), "constraint entry");
				for (int j = 0; j < subCount; j++)
				{
					ReadByte();
				}
			}
		}

		partial void AfterEncode()
		{
			try
			{
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

		private int GetParameterTypeId(string type)
		{
			return type switch
			{
				"unknown" => 0,
				"int" => 1,
				"float" => 3,
				"mixed" => 4,
				"wildcardint" => 5,
				"operator" => 6,
				"commandoperator" => 7,
				"target" => 8,
				"wildcardtarget" => 10,
				"filename" => 17,
				"integerrange" => 23,
				"equipmentslots" => 43,
				"string" => 44,
				"blockpos" => 52,
				"entitypos" => 53,
				"message" => 55,
				"rawtext" => 58,
				"json" => 62,
				"blockstates" => 71,
				"command" => 75,
				_ => 0
			};
		}

		private string GetParameterTypeName(int type)
		{
			return type switch
			{
				0 => "unknown",
				1 => "int",
				3 => "float",
				4 => "mixed",
				5 => "wildcardint",
				6 => "operator",
				7 => "commandoperator",
				8 => "target",
				10 => "wildcardtarget",
				17 => "filename",
				23 => "integerrange",
				43 => "equipmentslots",
				44 => "string",
				52 => "blockpos",
				53 => "entitypos",
				55 => "message",
				58 => "rawtext",
				62 => "json",
				71 => "blockstates",
				75 => "command",
				_ => "unknown"
			};
		}
	}
}
