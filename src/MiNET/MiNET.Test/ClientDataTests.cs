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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Utils.Skins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiNET.Test
{
	/// <summary>
	///     The login token is where a player's appearance enters the server, and everything the
	///     server fails to read there is invisible: it does not error, it just arrives at the other
	///     players as an identity with pieces missing. These pin the parts that were being dropped.
	/// </summary>
	[TestClass]
	public class ClientDataTests
	{
		/// <summary>
		///     A Character Creator skin IS its pieces and tints. Relaying persona = true without them
		///     hands the receiving client an identity it cannot assemble. This was silently the case
		///     for as long as the login was read as a dynamic JObject, because a property nobody names
		///     reads back null exactly like one that isn't there.
		/// </summary>
		[TestMethod]
		public void PersonaIdentitySurvivesTheLoginToken()
		{
			string json = $@"{{
				""SkinId"": ""persona-abc-3"",
				""SkinImageWidth"": 256,
				""SkinImageHeight"": 256,
				""SkinData"": ""{B64("pixels")}"",
				""SkinResourcePatch"": ""{B64(@"{""geometry"":{""default"":""geometry.persona""}}")}"",
				""SkinGeometryData"": ""{B64("{}")}"",
				""SkinGeometryDataEngineVersion"": ""{B64("1.14.0")}"",
				""ArmSize"": ""slim"",
				""SkinColor"": ""#b37b62"",
				""PersonaSkin"": true,
				""PersonaPieces"": [
					{{ ""PieceId"": ""p1"", ""PieceType"": ""persona_skeleton"", ""PackId"": ""pack1"", ""IsDefault"": true, ""ProductId"": """" }},
					{{ ""PieceId"": ""p2"", ""PieceType"": ""persona_mouth"", ""PackId"": ""pack2"", ""IsDefault"": false, ""ProductId"": ""prod"" }}
				],
				""PieceTintColors"": [
					{{ ""PieceType"": ""persona_mouth"", ""Colors"": [""#0"", ""#774235""] }}
				]
			}}";

			Skin skin = JsonConvert.DeserializeObject<ClientData>(json).ToSkin();

			Assert.IsTrue(skin.IsPersonaSkin);
			Assert.AreEqual(2, skin.PersonaPieces.Count, "persona pieces were dropped, so the skin cannot be assembled");
			Assert.AreEqual("persona_skeleton", skin.PersonaPieces[0].PieceType);
			Assert.AreEqual("pack1", skin.PersonaPieces[0].PackId);

			// The token spells this "IsDefault" and the model spells it "IsDefaultPiece"; a rename
			// like that is exactly what an untyped read gets wrong without saying so.
			Assert.IsTrue(skin.PersonaPieces[0].IsDefaultPiece);
			Assert.IsFalse(skin.PersonaPieces[1].IsDefaultPiece);

			Assert.AreEqual(1, skin.SkinPieces.Count, "piece tint colours were dropped");
			Assert.AreEqual(2, skin.SkinPieces[0].Colors.Count);

			Assert.AreEqual("slim", skin.ArmSize);
			Assert.AreEqual("#b37b62", skin.SkinColor);
			Assert.AreEqual("1.14.0", skin.GeometryDataVersion, "base64 of the engine version, decoded");
		}

		/// <summary>
		///     Mojang's SerializedSkin schema constrains GeometryData to "verify that the string can be
		///     parsed as valid JSON". Our own bot sent an empty string there, and a real client handed
		///     that skin quit within 40ms of receiving it.
		/// </summary>
		[TestMethod]
		public void OurOwnLoginCarriesGeometryTheClientCanParse()
		{
			Skin skin = BuildOurClientData().ToSkin();

			Assert.IsFalse(string.IsNullOrEmpty(skin.GeometryData), "empty geometry is not valid JSON and crashes the receiving client");
			JToken.Parse(skin.GeometryData);

			Assert.IsFalse(string.IsNullOrEmpty(skin.ResourcePatch), "a skin with no resource patch has no model to reference");
			JToken.Parse(skin.ResourcePatch);

			Assert.AreEqual(64 * 64 * 4, skin.Data.Length, "64x64 RGBA");
			Assert.AreEqual("wide", skin.ArmSize);
		}

		/// <summary>
		///     The tripwire. A field the client sends and this server does not model lands in
		///     Unmapped rather than vanishing, so the next version's additions announce themselves.
		/// </summary>
		[TestMethod]
		public void FieldsTheModelDoesNotKnowAreKeptNotDropped()
		{
			var data = JsonConvert.DeserializeObject<ClientData>(@"{ ""SkinId"": ""x"", ""SomeFutureField"": 42 }");

			Assert.AreEqual("x", data.SkinId);
			Assert.AreEqual(1, data.Unmapped.Count);
			Assert.IsTrue(data.Unmapped.ContainsKey("SomeFutureField"));
		}

		/// <summary>Everything our own bot sends must be understood by our own parser.</summary>
		[TestMethod]
		public void OurOwnLoginHasNothingTheServerCannotRead()
		{
			Assert.AreEqual(0, BuildOurClientData().Unmapped.Count);
		}

		private static ClientData BuildOurClientData()
		{
			string json = JsonConvert.SerializeObject(new ClientData
			{
				SkinId = "test.Custom",
				SkinData = Convert.ToBase64String(new byte[64 * 64 * 4]),
				SkinImageWidth = 64,
				SkinImageHeight = 64,
				SkinResourcePatch = B64(@"{""geometry"":{""default"":""geometry.humanoid.custom""}}"),
				SkinGeometryData = B64("{}"),
				SkinGeometryDataEngineVersion = B64("1.14.0"),
				ArmSize = "wide",
				SkinColor = "#5a5a5a"
			});

			return JsonConvert.DeserializeObject<ClientData>(json);
		}

		private static string B64(string text)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
		}
	}
}
