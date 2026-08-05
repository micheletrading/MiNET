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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson.
// All Rights Reserved.

#endregion

namespace MiNET.Net
{
	/// <summary>
	///     Which shape the body takes. Decided by the type, and a mismatch is a decode error, so it
	///     is derived rather than carried.
	/// </summary>
	public enum ChatCategory : byte
	{
		MessageOnly = 0,
		Authored = 1,
		WithParameters = 2
	}

	/// <summary>
	///     Wire order is needsTranslation, category, type, body, xuid, platformChatId, then an
	///     optional filtered message. Type is written here rather than as a generated field because
	///     two fields precede it.
	/// </summary>
	public partial class McpeText : Packet<McpeText>
	{
		public byte type;
		public bool needsTranslation; // = null
		public string source; // = null;
		public string message; // = null;
		public string xuid; // = null
		public string platformChatId; // = null
		public string[] parameters; // = null

		/// <summary>The message as the client's profanity filter would render it. Null means absent.</summary>
		public string filteredMessage;

		public static ChatCategory CategoryOf(ChatTypes type)
		{
			switch (type)
			{
				case ChatTypes.Chat:
				case ChatTypes.Whisper:
				case ChatTypes.Announcement:
					return ChatCategory.Authored;

				case ChatTypes.Translation:
				case ChatTypes.Popup:
				case ChatTypes.Jukeboxpopup:
					return ChatCategory.WithParameters;

				default:
					return ChatCategory.MessageOnly;
			}
		}

		partial void AfterEncode()
		{
			var chatType = (ChatTypes) type;

			Write(needsTranslation);
			Write((byte) CategoryOf(chatType));
			Write(type);

			switch (chatType)
			{
				case ChatTypes.Chat:
				case ChatTypes.Whisper:
				case ChatTypes.Announcement:
					Write(source ?? string.Empty);
					Write(message ?? string.Empty);
					break;

				case ChatTypes.Translation:
				case ChatTypes.Popup:
				case ChatTypes.Jukeboxpopup:
					Write(message ?? string.Empty);
					WriteUnsignedVarInt((uint) (parameters?.Length ?? 0));
					if (parameters != null)
					{
						foreach (string parameter in parameters) Write(parameter ?? string.Empty);
					}

					break;

				default:
					Write(message ?? string.Empty);
					break;
			}

			Write(xuid ?? string.Empty);
			Write(platformChatId ?? string.Empty);

			Write(filteredMessage != null);
			if (filteredMessage != null) Write(filteredMessage);
		}

		partial void AfterDecode()
		{
			needsTranslation = ReadBool();
			ReadByte(); // category: derivable from the type that follows
			type = ReadByte();

			switch ((ChatTypes) type)
			{
				case ChatTypes.Chat:
				case ChatTypes.Whisper:
				case ChatTypes.Announcement:
					source = ReadString();
					message = ReadString();
					break;

				case ChatTypes.Translation:
				case ChatTypes.Popup:
				case ChatTypes.Jukeboxpopup:
					message = ReadString();
					parameters = new string[ReadUnsignedVarInt()];
					for (int i = 0; i < parameters.Length; i++) parameters[i] = ReadString();
					break;

				default:
					message = ReadString();
					break;
			}

			xuid = ReadString();
			platformChatId = ReadString();

			if (ReadBool()) filteredMessage = ReadString();
		}
	}
}
