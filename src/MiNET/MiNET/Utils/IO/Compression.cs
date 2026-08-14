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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using log4net;
using MiNET.Net;

namespace MiNET.Utils.IO
{
	public class Compression
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Compression));

		/// <summary>
		///     Below or at this many payload bytes a batch ships uncompressed (compressor id 0xff,
		///     raw frames): compression is for payloads that need it, not a reflex. The cheap-looking
		///     alternative, a NoCompression deflate stream, still costs a native zlib handle per
		///     batch (finalizer pressure at packet rates) plus stored-block framing and a full copy.
		///     Advertised to the client in NetworkSettings, so the same rule holds in both
		///     directions.
		/// </summary>
		public const int CompressionThresholdBytes = 1;

		// Codec-input tracing: when MINET_BATCH_DUMP names a directory, every payload that actually
		// reaches the deflater (all three compression entry points, pre-compression, frames and
		// all) is written there as <seq>-<len>.bin. This is the exact byte stream a replacement
		// codec must handle, which the tunnel's post-compression per-frame dumps are not. Empty
		// counts as unset. Below-threshold batches ship raw and are deliberately not dumped.
		private static readonly string BatchDumpDir =
			Environment.GetEnvironmentVariable("MINET_BATCH_DUMP") is {Length: > 0} dir ? dir : null;
		private static int _batchDumpSeq;

		/// <summary><paramref name="lengthPrefix" /> non-negative means the live stream carried a varint length ahead of the payload (writeLen), and the dump must byte-match what the deflater consumed.</summary>
		private static void DumpBatch(ReadOnlySpan<byte> payload, int lengthPrefix = -1)
		{
			try
			{
				int seq = System.Threading.Interlocked.Increment(ref _batchDumpSeq);
				Directory.CreateDirectory(BatchDumpDir);
				using FileStream file = File.Create(Path.Combine(BatchDumpDir, $"{seq:D6}-{payload.Length}.bin"));
				if (lengthPrefix >= 0) WriteLength(file, lengthPrefix);
				file.Write(payload);
			}
			catch (Exception e)
			{
				Log.Warn($"Batch dump failed: {e.Message}");
			}
		}

		public static byte[] Compress(ReadOnlyMemory<byte> input, bool writeLen = false, CompressionLevel compressionLevel = CompressionLevel.Fastest)
		{
			using (MemoryStream stream = MiNetServer.MemoryStreamManager.GetStream())
			{
				using (var compressStream = new DeflateStream(stream, compressionLevel, true))
				{
					if (writeLen)
					{
						WriteLength(compressStream, input.Length);
					}

					compressStream.Write(input.Span);
				}

				byte[] bytes = stream.ToArray();
				return bytes;
			}
		}

		/// <summary>
		///     Wrapper-payload compression into a pooled stream the caller owns: attach it to the
		///     wrapper with <see cref="Packet.AttachLease" /> so it returns to the pool with the
		///     packet, and view the bytes via GetBuffer()/Length. Leading compressor-id byte
		///     included: 0x00 = zlib/deflate above <see cref="CompressionThresholdBytes" />,
		///     0xff = raw at or below it, no deflater involved.
		/// </summary>
		public static MemoryStream CompressIntoPooledStream(ReadOnlyMemory<byte> input, bool writeLen, CompressionLevel compressionLevel)
		{
			MemoryStream stream = MiNetServer.MemoryStreamManager.GetStream();

			if (input.Length <= CompressionThresholdBytes)
			{
				stream.WriteByte(0xff);
				if (writeLen) WriteLength(stream, input.Length);
				stream.Write(input.Span);
				return stream;
			}

			stream.WriteByte(0x00);
			using (var compressStream = new DeflateStream(stream, compressionLevel, true))
			{
				if (writeLen)
				{
					WriteLength(compressStream, input.Length);
				}

				compressStream.Write(input.Span);
			}

			if (BatchDumpDir != null) DumpBatch(input.Span, writeLen ? input.Length : -1);

			return stream;
		}

		/// <summary>
		///     Same wrapper-payload compression fed from a segment chain: deflate consumes
		///     sequentially, so a roster assembled as a <see cref="ReadOnlySequence{T}" /> of cached
		///     fragments compresses without ever existing as one contiguous buffer.
		/// </summary>
		public static MemoryStream CompressIntoPooledStream(ReadOnlySequence<byte> input, bool writeLen, CompressionLevel compressionLevel)
		{
			MemoryStream stream = MiNetServer.MemoryStreamManager.GetStream();

			if (input.Length <= CompressionThresholdBytes)
			{
				stream.WriteByte(0xff);
				if (writeLen) WriteLength(stream, (int) input.Length);
				foreach (ReadOnlyMemory<byte> segment in input)
				{
					stream.Write(segment.Span);
				}
				return stream;
			}

			stream.WriteByte(0x00);
			using (var compressStream = new DeflateStream(stream, compressionLevel, true))
			{
				if (writeLen)
				{
					WriteLength(compressStream, (int) input.Length);
				}

				foreach (ReadOnlyMemory<byte> segment in input)
				{
					compressStream.Write(segment.Span);
				}
			}

			if (BatchDumpDir != null)
			{
				// The sequence is consumed above but its segments stay valid until the caller drops
				// the roster reference; a contiguous copy here keeps the dump exact and simple.
				byte[] whole = input.ToArray();
				DumpBatch(whole, writeLen ? whole.Length : -1);
			}

			return stream;
		}

		// Packets packed with their lengths and nothing else: no deflate framing and no compressor
		// id byte. Both sides read a wrapper payload as plain bytes until the NetworkSettings
		// exchange completes, so the two packets that carry out that exchange, one each way, cannot
		// go through CompressPacketsForWrapper. Deflate at NoCompression is not a substitute: it
		// copies the bytes but still writes a five byte stored-block header a raw reader would eat.
		public static MemoryStream PackPacketsForWrapper(List<Packet> packets)
		{
			MemoryStream stream = MiNetServer.MemoryStreamManager.GetStream();
			foreach (Packet packet in packets)
			{
				ReadOnlyMemory<byte> bs = packet.EncodeAsMemory();
				if (bs.Length > 0)
				{
					BatchUtils.WriteLength(stream, bs.Length);
					stream.Write(bs.Span);
				}

				packet.PutPool();
			}

			return stream;
		}

		// Every caller sends this after the NetworkSettings exchange, where a wrapper payload leads
		// with a compressor id byte (0x00=zlib, 0x01=snappy, 0xff=none). The byte belongs here and
		// not at the call sites: whether to compress is a property of the batch, decided by the
		// size rule below, and a caller that got it wrong would produce a payload no client can
		// read rather than an error anyone could see.
		//
		// TODO: reserve one byte of headroom at the front of the returned buffer, and return the
		// payload offset alongside it. NetherNet has to put a segment header byte in front of this
		// payload and currently copies the entire batch on every send to do it, see
		// NetherNetSegments.ForEachSegment. With headroom it writes the header in place and sends
		// the same buffer. Every consumer reads the payload from offset zero today, so the offset
		// has to become part of this method's contract rather than something callers guess at.
		public static MemoryStream CompressPacketsForWrapper(List<Packet> packets, CompressionLevel compressionLevel = CompressionLevel.Fastest)
		{
			long length = 0;
			foreach (Packet packet in packets) length += packet.EncodeAsMemory().Length;

			MemoryStream stream = MiNetServer.MemoryStreamManager.GetStream();

			// At or below the threshold the batch ships raw under 0xff: no deflater exists at all,
			// which at input-packet rates is the difference between zero native handles and one
			// finalizable zlib handle per send.
			if (length <= CompressionThresholdBytes)
			{
				stream.WriteByte(0xff);
				foreach (Packet packet in packets)
				{
					ReadOnlyMemory<byte> bs = packet.EncodeAsMemory();
					if (bs.Length > 0)
					{
						BatchUtils.WriteLength(stream, bs.Length);
						stream.Write(bs.Span);
					}
					packet.PutPool();
				}

				return stream;
			}

			// Ahead of the deflate stream, so this costs one byte on the same pooled buffer.
			stream.WriteByte(0x00);

			// The frames are materialized before the deflater sees them, rather than each packet
			// being written into it. A batch is ~42 packets and every length prefix is a varint
			// written a byte at a time, so writing through costs the deflater roughly 125 calls
			// where this costs it one. Measured on captured batches that is 1.71x the time at
			// Fastest; at Optimal it is 1.17x the time and 2.5% of the output as well, from the
			// identical bytes, because the deflater sees the whole block at once. The frame buffer
			// is a rent from the same pool as the output, not an allocation.
			using MemoryStream frames = MiNetServer.MemoryStreamManager.GetStream();
			foreach (Packet packet in packets)
			{
				ReadOnlyMemory<byte> bs = packet.EncodeAsMemory();
				if (bs.Length > 0)
				{
					BatchUtils.WriteLength(frames, bs.Length);
					frames.Write(bs.Span);
				}
				packet.PutPool();
			}

			var payload = new ReadOnlySpan<byte>(frames.GetBuffer(), 0, (int) frames.Length);
			if (BatchDumpDir != null) DumpBatch(payload);

			using (var compressStream = new DeflateStream(stream, compressionLevel, true))
			{
				compressStream.Write(payload);
				compressStream.Flush();
			}

			return stream;
		}

		public static void WriteLength(Stream stream, int lenght)
		{
			VarInt.WriteUInt32(stream, (uint) lenght);
		}
	}
}