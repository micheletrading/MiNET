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

using System.Text;
using MiNET.Net;
using MiNET.Worlds;

namespace MiNET.Console
{
	/// <summary>
	///     A player with no network session, so commands can run with nobody connected. The Player
	///     constructor is already null-safe (it sets IsConnected from the endpoint) and SendPacket
	///     drops packets when there is no handler, so this is safe to hand to any command. What it
	///     cannot do is talk to a client: a command whose whole purpose is to send a packet will
	///     appear to succeed and do nothing.
	/// </summary>
	public class ConsolePlayer : Player
	{
		private readonly StringBuilder _output = new StringBuilder();

		public ConsolePlayer(Level level) : base(null, null)
		{
			Level = level;
			Username = "CONSOLE";
			CommandPermission = CommandPermission.Admin;
			PermissionLevel = PermissionLevel.Operator;
		}

		/// <summary>
		///     Commands report through SendMessage, which normally broadcasts to the level. Capturing
		///     it here is what gives the caller something to print instead.
		/// </summary>
		public override void SendMessage(string text, MessageType type = MessageType.Chat, Player sender = null, bool needsTranslation = false, string[] parameters = null)
		{
			lock (_output)
			{
				_output.AppendLine(text);
			}
		}

		public string TakeOutput()
		{
			lock (_output)
			{
				string text = _output.ToString();
				_output.Clear();
				return text;
			}
		}
	}
}
