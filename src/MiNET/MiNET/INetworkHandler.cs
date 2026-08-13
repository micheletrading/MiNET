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

using System.Net;
using MiNET.Net;

namespace MiNET
{
	public interface INetworkHandler
	{
		// Session identity, set during login and used for logging. On the interface rather than the
		// transport because every transport has to answer "who is this" the same way.
		string Username { get; set; }

		// True when the transport already encrypts and authenticates everything above it, as
		// NetherNet does through DTLS. Bedrock's own session cipher is then redundant and must be
		// skipped: the peer is not expecting a second layer and will not find one.
		bool IsTransportEncrypted { get; }

		// Which transport carried this session, for logging. Worth naming explicitly: with two
		// transports live, "which one did this player arrive on" is otherwise only inferable from
		// side effects such as whether encryption was negotiated.
		string TransportName { get; }

		// The layer above the transport: batching, compression and encryption. Both transports own
		// one, and callers need to reach it without knowing which transport they are on.
		Net.ICustomMessageHandler CustomMessageHandler { get; set; }

		void Close();

		// Ending a session with a reason the player sees is every transport's job, and the login
		// handler rejects connections before any transport-specific object exists to do it.
		void Disconnect(string reason, bool sendDisconnect = true);

		void SendPacket(Packet packet);
		void SendDirectPacket(Packet packet);
		IPEndPoint GetClientEndPoint();
		long GetNetworkNetworkIdentifier();
	}
}