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
using System.Threading;
using log4net;
using MiNET.Utils;

namespace MiNET.Net
{
	/// <summary>
	///     Server-wide connection statistics: the player counts the MOTD and discovery answer from,
	///     refreshed once a second and surfaced in the log or the console title.
	/// </summary>
	public class ConnectionInfo
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ConnectionInfo));

		public int NumberOfPlayers { get; set; }
		public int MaxNumberOfPlayers { get; set; }
		public int MaxNumberOfConcurrentConnects { get; set; }

		/// <summary>How many sessions are inside the login sequence right now (Player increments on handshake, decrements when the sequence completes).</summary>
		public int ConnectionsInConnectPhase = 0;

		public Timer ThroughPut { get; set; }

		/// <param name="playerCount">
		///     Live session count, supplied by the transport that holds the sessions.
		/// </param>
		public ConnectionInfo(Func<int> playerCount = null)
		{
			if (!Log.IsInfoEnabled) return;

			ThroughPut = new Timer(state =>
			{
				if (playerCount != null) NumberOfPlayers = playerCount();

				var message = $"Players {NumberOfPlayers}";

				if (Config.GetProperty("ServerInfoInTitle", false))
				{
					Console.Title = message;
				}
				else
				{
					Log.Info(message);
				}
			}, null, 1000, 1000);
		}

		internal void Stop()
		{
			ThroughPut?.Change(Timeout.Infinite, Timeout.Infinite);
			ThroughPut?.Dispose();
			ThroughPut = null;
		}
	}
}
