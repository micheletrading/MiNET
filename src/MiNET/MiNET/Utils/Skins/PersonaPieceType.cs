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

namespace MiNET.Utils.Skins
{
	/// <summary>
	///     persona::PieceType as the wire carries it. The client's ClientData names these
	///     "persona_&lt;name&gt;" and a tint entry names them bare ("eyes"), but on the wire a persona
	///     piece carries the numeric value.
	///     Mojang's persona__PieceType schema omits the member at 0, so every name in it sits one
	///     place lower than the value BDS sends (verified across all eight pieces of a live persona
	///     skin: skeleton=1, body=2, skin=3, bottom=4, feet=5, top=7, eyes=13, capes=25).
	/// </summary>
	public enum PersonaPieceType
	{
		Unknown = 0,
		Skeleton = 1,
		Body = 2,
		Skin = 3,
		Bottom = 4,
		Feet = 5,
		Dress = 6,
		Top = 7,
		HighPants = 8,
		Hands = 9,
		Outerwear = 10,
		FacialHair = 11,
		Mouth = 12,
		Eyes = 13,
		Hair = 14,
		Hood = 15,
		Back = 16,
		FaceAccessory = 17,
		Head = 18,
		Legs = 19,
		LeftLeg = 20,
		RightLeg = 21,
		Arms = 22,
		LeftArm = 23,
		RightArm = 24,
		Capes = 25,
		ClassicSkin = 26,
		Emote = 27
	}

	public static class PersonaPieceTypes
	{
		/// <summary>"persona_bottom" or "bottom" to the wire value; unknown names become Unknown.</summary>
		public static PersonaPieceType Parse(string name)
		{
			if (string.IsNullOrEmpty(name)) return PersonaPieceType.Unknown;

			string bare = name.StartsWith("persona_", StringComparison.OrdinalIgnoreCase) ? name.Substring("persona_".Length) : name;
			bare = bare.Replace("_", "");

			return Enum.TryParse(bare, true, out PersonaPieceType type) ? type : PersonaPieceType.Unknown;
		}

		/// <summary>The name a persona piece carries in ClientData, e.g. "persona_bottom".</summary>
		public static string ToClientDataName(PersonaPieceType type) => "persona_" + type.ToString().ToLowerInvariant();

		/// <summary>The bare name a tint entry carries, e.g. "eyes".</summary>
		public static string ToTintName(PersonaPieceType type) => type.ToString().ToLowerInvariant();
	}
}
