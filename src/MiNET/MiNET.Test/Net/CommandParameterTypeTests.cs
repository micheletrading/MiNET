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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net;

namespace MiNET.Test.Net
{
	[TestClass]
	public class CommandParameterTypeTests
	{
		// Command parameter type ids are Mojang's enum, and Mojang inserts into the middle of it, so
		// every value above 23 has moved at least once. A wrong id is not a decode failure the server
		// ever sees: the client just renders the argument as "unknown" and the parameter stops
		// offering completions. These are PMMP BedrockProtocol's values at protocol 1001.

		[TestMethod]
		public void TheTypesAProtocol1001ClientKnows()
		{
			var expected = new Dictionary<string, int>
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

			foreach (var pair in expected)
			{
				Assert.AreEqual(pair.Value, McpeAvailableCommands.GetParameterTypeId(pair.Key), pair.Key);
			}

			Assert.AreEqual(expected.Count, McpeAvailableCommands.ParameterTypeIds.Count, "a type was added or removed without updating this test");
		}

		// The two directions were separate switch statements, which is how they came to disagree.
		[TestMethod]
		public void EveryTypeSurvivesTheRoundTrip()
		{
			foreach (var pair in McpeAvailableCommands.ParameterTypeIds)
			{
				Assert.AreEqual(pair.Key, McpeAvailableCommands.GetParameterTypeName(pair.Value));
			}
		}

		[TestMethod]
		public void AnythingElseHasNoNameAndNoId()
		{
			Assert.AreEqual("unknown", McpeAvailableCommands.GetParameterTypeName(0));
			Assert.AreEqual("unknown", McpeAvailableCommands.GetParameterTypeName(44), "44 was string before the enum was renumbered");
			Assert.AreEqual(0, McpeAvailableCommands.GetParameterTypeId("command"), "no such type at 1001");
			Assert.AreEqual(0, McpeAvailableCommands.GetParameterTypeId(null));
		}
	}
}
