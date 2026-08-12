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
using System.Buffers;
using System.IO;
using System.IO.Compression;
using MiNET.Net;
using MiNET.Net.RakNet;

namespace MiNET.Utils.IO
{
	public class BatchUtils
	{
		public static McpeWrapper CreateBatchPacket(CompressionLevel compressionLevel, params Packet[] packets)
		{
			using (MemoryStream stream = MiNetServer.MemoryStreamManager.GetStream())
			{
				foreach (Packet packet in packets)
				{
					ReadOnlyMemory<byte> bytes = packet.EncodeAsMemory();
					WriteLength(stream, bytes.Length);
					stream.Write(bytes.Span);
					packet.PutPool();
				}

				var buffer = new ReadOnlyMemory<byte>(stream.GetBuffer(), 0, (int) stream.Length);
				return CreateBatchPacket(buffer, compressionLevel, false);
			}
		}

		public static McpeWrapper CreateBatchPacket(ReadOnlyMemory<byte> input, CompressionLevel compressionLevel, bool writeLen)
		{
			var batch = McpeWrapper.CreateObject();
			batch.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;

			// Post-1.19.30 wrapper payloads carry a leading compressor-id byte, written by
			// CompressIntoPooledStream. This path runs only after compression is negotiated.
			batch.SetPayload(Compression.CompressIntoPooledStream(input, writeLen, input.Length > 1000 ? compressionLevel : CompressionLevel.NoCompression));

			batch.EncodeAsMemory(); // prepare
			return batch;
		}

		/// <summary>
		///     Batches a packet already assembled as a segment chain (the cached-roster path). The
		///     sequence is consumed here, inside the call, so the caller may drop it as soon as
		///     this returns.
		/// </summary>
		public static McpeWrapper CreateBatchPacket(ReadOnlySequence<byte> input, CompressionLevel compressionLevel, bool writeLen)
		{
			var batch = McpeWrapper.CreateObject();
			batch.ReliabilityHeader.Reliability = Reliability.ReliableOrdered;

			batch.SetPayload(Compression.CompressIntoPooledStream(input, writeLen, input.Length > 1000 ? compressionLevel : CompressionLevel.NoCompression));

			batch.EncodeAsMemory(); // prepare
			return batch;
		}

		public static void WriteLength(Stream stream, int length)
		{
			VarInt.WriteUInt32(stream, (uint) length);
		}
	}
}