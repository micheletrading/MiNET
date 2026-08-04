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

namespace MiNET.Worlds
{
	/// <summary>
	///     The wire values of Bedrock's GameType. All seven exist on the protocol, and a packet
	///     carrying one of the four that used to be missing here would be cast into a GameMode
	///     matching nothing at all, which reads as "no game mode" to every comparison in the server.
	/// </summary>
	public enum GameMode
	{
		/// <summary>
		///     Players fight against the enviornment, mobs, and players
		///     with limited resources.
		/// </summary>
		Survival = 0,
		S = 0,

		/// <summary>
		///     Players are given unlimited resources, flying, and
		///     invulnerability.
		/// </summary>
		Creative = 1,
		C = 1,

		/// <summary>
		///     Similar to survival, with the exception that players may
		///     not place or remove blocks.
		/// </summary>
		Adventure = 2,

		/// <summary>
		///     The pre-1.16 spectator, which is survival without the ability to interact. Superseded
		///     by <see cref="Spectator" />, and kept because the value is still on the wire.
		/// </summary>
		SurvivalSpectator = 3,

		/// <summary>
		///     The pre-1.16 creative spectator. Superseded by <see cref="Spectator" />.
		/// </summary>
		CreativeSpectator = 4,

		/// <summary>
		///     Not a mode: the "use the level's game mode" sentinel. StartGame sends it as the
		///     player's mode, and the client acknowledges it verbatim, so it arrives at the server
		///     and has to be resolved rather than stored.
		/// </summary>
		Fallback = 5,

		/// <summary>
		///     Players move through blocks and cannot interact with the world at all.
		/// </summary>
		Spectator = 6
	}
}