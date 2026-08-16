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
using System.Globalization;
using System.Linq;
using MiNET.Utils.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MiNET.Test.Net
{
	/// <summary>
	///     A batch captured off the wire that the decoder rejected, with its length and CRC32c verified
	///     on arrival, so these bytes are exactly what the sender wrote. Either they are malformed, and
	///     the encoder built them wrong, or they are fine and the decoder reads them wrong. The frames
	///     are walked here with nothing else in the picture: no transport, no session, no threads.
	/// </summary>
	[TestClass]
	public class BatchFramingTests
	{
		// TheGrey004, uncompressed (0xff), 111 bytes, CRC verified by the receiver.
		private const string CapturedBatch =
			"ff 6d 90 01 e1 01 54 c0 64 37 2c 43 02 e1 ab c1 " +
			"cc 14 94 41 e7 2d 81 40 00 00 00 00 00 00 80 3f " +
			"64 37 2c 43 01 02 14 60 01 00 00 e1 01 54 c0 64 " +
			"37 2c 43 be 01 00 b8 d6 bc 00 88 37 3c 60 5a 44 " +
			"be 01 00 01 00 01 00 01 01 00 00 00 00 00 00 00 " +
			"00 01 00 00 00 00 00 00 00 80 3f 00 00 00 00 64 " +
			"37 2c 43 00 00 00 00 00 00 00 00 00 00 80 3f";

		private static byte[] Parse(string hex) =>
			hex.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(b => byte.Parse(b, NumberStyles.HexNumber))
				.ToArray();

		[TestMethod]
		public void Captured_batch_frames_land_exactly_on_its_end()
		{
			byte[] payload = Parse(CapturedBatch);
			Assert.AreEqual(111, payload.Length, "the capture is 111 bytes");
			Assert.AreEqual(0xff, payload[0], "0xff is the compressor id for an uncompressed batch");

			// Everything after the compressor id is length-prefixed frames, and they have to consume
			// the batch exactly. One byte over or under is what kills the session on the wire.
			var batch = new ReadOnlySpan<byte>(payload, 1, payload.Length - 1);

			int position = 0;
			int frame = 0;
			while (position < batch.Length)
			{
				frame++;

				int shift = 0;
				long length = 0;
				int varintStart = position;
				while (true)
				{
					Assert.IsTrue(position < batch.Length, $"frame {frame}: length varint starting at {varintStart} runs off the end of {batch.Length} bytes");

					byte b = batch[position++];
					length |= (long) (b & 0x7f) << shift;
					if ((b & 0x80) == 0) break;
					shift += 7;
				}

				Assert.IsTrue(position + length <= batch.Length,
					$"frame {frame} says {length} bytes at offset {position}, but the batch is {batch.Length}");

				position += (int) length;
			}

			Assert.AreEqual(batch.Length, position, "the frames must end exactly on the end of the batch");
			Assert.AreEqual(1, frame, "this capture holds one frame");
		}

	}
}
