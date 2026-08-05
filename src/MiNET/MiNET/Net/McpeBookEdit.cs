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

namespace MiNET.Net
{
	public partial class McpeBookEdit : Packet<McpeBookEdit>
	{
		public const byte TypeReplacePage = 0;
		public const byte TypeAddPage = 1;
		public const byte TypeDeletePage = 2;
		public const byte TypeSwapPages = 3;
		public const byte TypeSignBook = 4;

		public int PageNumber;
		public int SecondaryPageNumber;
		public string Text;
		public string PhotoName;
		public string Title;
		public string Author;
		public string Xuid;

		partial void AfterDecode()
		{
			switch (type)
			{
				case TypeReplacePage:
				case TypeAddPage:
					PageNumber = ReadSignedVarInt();
					Text = ReadString();
					PhotoName = ReadString();
					break;
				case TypeDeletePage:
					PageNumber = ReadSignedVarInt();
					break;
				case TypeSwapPages:
					PageNumber = ReadSignedVarInt();
					SecondaryPageNumber = ReadSignedVarInt();
					break;
				case TypeSignBook:
					Title = ReadString();
					Author = ReadString();
					Xuid = ReadString();
					break;
			}
		}

		partial void AfterEncode()
		{
			switch (type)
			{
				case TypeReplacePage:
				case TypeAddPage:
					WriteSignedVarInt(PageNumber);
					Write(Text);
					Write(PhotoName);
					break;
				case TypeDeletePage:
					WriteSignedVarInt(PageNumber);
					break;
				case TypeSwapPages:
					WriteSignedVarInt(PageNumber);
					WriteSignedVarInt(SecondaryPageNumber);
					break;
				case TypeSignBook:
					Write(Title);
					Write(Author);
					Write(Xuid);
					break;
			}
		}
	}
}
