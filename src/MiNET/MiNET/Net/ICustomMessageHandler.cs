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
using System.Collections.Generic;
using log4net;

namespace MiNET.Net
{
	public interface ICustomMessageHandler
	{
		void Connected();

		void Disconnect(string reason, bool sendDisconnect = true);

		void HandlePacket(Packet message);

		/// <summary>
		///     One complete batch payload as it came off the transport, starting at its compressor id
		///     byte. A VIEW onto memory the transport owns for the duration of this call and reuses
		///     afterwards, so an implementation that keeps any of it has to copy it here.
		///     <para>
		///         The payload rather than a wrapper packet, because the transport has nothing to wrap:
		///         building one would be an object per inbound batch carrying a field the callee reads
		///         immediately.
		///     </para>
		/// </summary>
		void HandlePayload(ReadOnlyMemory<byte> payload);

		Packet HandleOrderedSend(Packet packet);
		List<Packet> PrepareSend(List<Packet> packetsToSend);
	}

	public class DefaultMessageHandler : ICustomMessageHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DefaultMessageHandler));

		public void Connected()
		{
		}

		public void Disconnect(string reason, bool sendDisconnect = true)
		{
		}

		public void HandlePacket(Packet message)
		{
			Log.Warn($"Default custom message handler. Probably not what you want!");
		}

		public void HandlePayload(ReadOnlyMemory<byte> payload)
		{
			Log.Warn($"Default custom message handler. Probably not what you want!");
		}

		public Packet HandleOrderedSend(Packet packet)
		{
			Log.Warn($"Default custom message handler. Probably not what you want!");
			return packet;
		}

		public List<Packet> PrepareSend(List<Packet> packetsToSend)
		{
			return packetsToSend;
		}
	}
}