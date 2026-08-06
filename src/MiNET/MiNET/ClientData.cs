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
using System.Collections.Generic;
using System.Text;
using log4net;
using MiNET.Utils.Skins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiNET
{
	/// <summary>
	///     The "client data" JWT a client signs into its login: who is connecting, on what, and what
	///     they look like. Modelled rather than read as a dynamic JObject, because a dynamic read
	///     cannot tell a field that isn't there from one nobody thought to ask for. The old code
	///     asked for 29 of the 45 fields a 1.26 client sends, and the 16 it never named included
	///     every part of a Character Creator identity, which is why persona skins arrived at other
	///     players with the pieces stripped off.
	///     <see cref="Unmapped" /> keeps that from happening again: anything the client sends that
	///     this class does not claim lands there and gets logged once, so a field added by a future
	///     version announces itself instead of being silently dropped.
	/// </summary>
	public class ClientData
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ClientData));

		// Identity and session
		[JsonProperty] public long ClientRandomId { get; set; }
		[JsonProperty] public string SelfSignedId { get; set; }
		[JsonProperty] public string ServerAddress { get; set; }
		[JsonProperty] public string ThirdPartyName { get; set; }
		[JsonProperty] public bool ThirdPartyNameOnly { get; set; }
		[JsonProperty] public string PlatformOnlineId { get; set; }
		[JsonProperty] public string PlatformOfflineId { get; set; }
		[JsonProperty] public string PlayFabId { get; set; }
		[JsonProperty] public string TenantId { get; set; }

		// Device and client capabilities
		[JsonProperty] public string DeviceId { get; set; }
		[JsonProperty] public string DeviceModel { get; set; }
		[JsonProperty] public int DeviceOS { get; set; }
		[JsonProperty] public int PlatformType { get; set; }
		[JsonProperty] public string GameVersion { get; set; }
		[JsonProperty] public string LanguageCode { get; set; }
		[JsonProperty] public int CurrentInputMode { get; set; }
		[JsonProperty] public int DefaultInputMode { get; set; }
		[JsonProperty] public int GuiScale { get; set; }
		[JsonProperty] public int UIProfile { get; set; }
		[JsonProperty] public int GraphicsMode { get; set; }
		[JsonProperty] public int MaxViewDistance { get; set; }
		[JsonProperty] public int MemoryTier { get; set; }
		[JsonProperty] public bool CompatibleWithClientSideChunkGen { get; set; }
		[JsonProperty] public bool FilterProfanity { get; set; }
		[JsonProperty] public bool ClientIsEditorCapable { get; set; }
		[JsonProperty] public int ClientEditorConnectionIntent { get; set; }

		// Skin. The blobs are base64; SkinResourcePatch and SkinGeometryData are base64 of JSON,
		// and SkinGeometryDataEngineVersion is base64 of a version string such as "1.14.0".
		[JsonProperty] public string SkinId { get; set; }
		[JsonProperty] public string SkinData { get; set; }
		[JsonProperty] public int SkinImageWidth { get; set; }
		[JsonProperty] public int SkinImageHeight { get; set; }
		[JsonProperty] public string SkinResourcePatch { get; set; }
		[JsonProperty] public string SkinGeometryData { get; set; }
		[JsonProperty] public string SkinGeometryDataEngineVersion { get; set; }
		[JsonProperty] public string SkinAnimationData { get; set; }
		[JsonProperty] public string SkinColor { get; set; }
		[JsonProperty] public string ArmSize { get; set; }

		/// <summary>
		///     The skin's cache key, which SerializedSkin has carried on the wire since 2168. The
		///     client sends it at login and expects it back when the server relays the skin, so
		///     dropping it means relaying a persona identity with an empty hash.
		/// </summary>
		[JsonProperty] public string ProfileHash { get; set; }
		[JsonProperty] public bool PremiumSkin { get; set; }
		[JsonProperty] public bool PersonaSkin { get; set; }
		[JsonProperty] public bool OverrideSkin { get; set; }
		[JsonProperty] public bool TrustedSkin { get; set; }
		[JsonProperty] public List<ClientAnimatedImage> AnimatedImageData { get; set; } = new List<ClientAnimatedImage>();
		[JsonProperty] public List<ClientPersonaPiece> PersonaPieces { get; set; } = new List<ClientPersonaPiece>();
		[JsonProperty] public List<ClientPieceTintColor> PieceTintColors { get; set; } = new List<ClientPieceTintColor>();

		// Cape
		[JsonProperty] public string CapeId { get; set; }
		[JsonProperty] public string CapeData { get; set; }
		[JsonProperty] public int CapeImageWidth { get; set; }
		[JsonProperty] public int CapeImageHeight { get; set; }
		[JsonProperty] public bool CapeOnClassicSkin { get; set; }

		/// <summary>Everything the client sent that this class does not model. Should stay empty.</summary>
		[JsonExtensionData]
		public IDictionary<string, JToken> Unmapped { get; set; } = new Dictionary<string, JToken>();

		public static ClientData FromJson(string json)
		{
			var data = JsonConvert.DeserializeObject<ClientData>(json);

			if (data.Unmapped.Count > 0)
			{
				Log.Warn($"Client sent {data.Unmapped.Count} field(s) this server does not model: {string.Join(", ", data.Unmapped.Keys)}");
			}

			return data;
		}

		/// <summary>
		///     The appearance as the rest of the server and every other client will see it. This is the
		///     only place the login form is turned into the wire form, so a field cannot be understood
		///     here and lost on the way out.
		/// </summary>
		public Skin ToSkin()
		{
			var skin = new Skin
			{
				SkinId = SkinId,
				PlayFabId = PlayFabId,
				ResourcePatch = DecodeText(SkinResourcePatch),
				Width = SkinImageWidth,
				Height = SkinImageHeight,
				Data = DecodeBytes(SkinData),
				GeometryData = DecodeText(SkinGeometryData),
				GeometryDataVersion = DecodeText(SkinGeometryDataEngineVersion),
				AnimationData = SkinAnimationData,
				ArmSize = ArmSize,
				SkinColor = SkinColor,
				ProfileHash = ProfileHash ?? "",
				IsPremiumSkin = PremiumSkin,
				IsPersonaSkin = PersonaSkin,
				OverrideAppearance = OverrideSkin,

				// The server vouches for skins it relays: the trusted flag is what the client's
				// "only allow trusted skins" setting checks, so without it every custom skin renders
				// as a default persona for players with that setting on (PMMP marks relayed skins
				// the same way).
				IsVerified = true,

				Cape = new Cape
				{
					Id = CapeId,
					Data = DecodeBytes(CapeData),
					ImageWidth = CapeImageWidth,
					ImageHeight = CapeImageHeight,
					OnClassicSkin = CapeOnClassicSkin
				}
			};

			foreach (ClientAnimatedImage animation in AnimatedImageData)
			{
				skin.Animations.Add(new Animation
				{
					Image = DecodeBytes(animation.Image),
					ImageWidth = animation.ImageWidth,
					ImageHeight = animation.ImageHeight,
					FrameCount = animation.Frames,
					Expression = animation.AnimationExpression,
					Type = animation.Type
				});
			}

			// A Character Creator skin IS its pieces. Relaying persona=true without them leaves the
			// receiving client an identity it has no way to assemble.
			foreach (ClientPersonaPiece piece in PersonaPieces)
			{
				skin.PersonaPieces.Add(new PersonaPiece
				{
					PieceId = piece.PieceId,
					PieceType = piece.PieceType,
					PackId = piece.PackId,
					IsDefaultPiece = piece.IsDefault,
					ProductId = piece.ProductId
				});
			}

			foreach (ClientPieceTintColor tint in PieceTintColors)
			{
				skin.SkinPieces.Add(new SkinPiece {PieceType = tint.PieceType, Colors = tint.Colors});
			}

			return skin;
		}

		private static byte[] DecodeBytes(string base64)
		{
			return string.IsNullOrEmpty(base64) ? Array.Empty<byte>() : Convert.FromBase64String(base64);
		}

		private static string DecodeText(string base64)
		{
			return string.IsNullOrEmpty(base64) ? string.Empty : Encoding.UTF8.GetString(Convert.FromBase64String(base64));
		}
	}

	public class ClientAnimatedImage
	{
		[JsonProperty] public string Image { get; set; }
		[JsonProperty] public int ImageWidth { get; set; }
		[JsonProperty] public int ImageHeight { get; set; }

		/// <summary>Fractional on the wire and in the token, not a whole number of frames.</summary>
		[JsonProperty] public float Frames { get; set; }

		[JsonProperty] public int Type { get; set; }
		[JsonProperty] public int AnimationExpression { get; set; }
	}

	public class ClientPersonaPiece
	{
		[JsonProperty] public string PieceId { get; set; }
		[JsonProperty] public string PieceType { get; set; }
		[JsonProperty] public string PackId { get; set; }
		[JsonProperty] public string ProductId { get; set; }

		/// <summary>Named "IsDefault" in the token and "is_default_piece" on the wire.</summary>
		[JsonProperty] public bool IsDefault { get; set; }
	}

	public class ClientPieceTintColor
	{
		[JsonProperty] public string PieceType { get; set; }
		[JsonProperty] public List<string> Colors { get; set; } = new List<string>();
	}
}
