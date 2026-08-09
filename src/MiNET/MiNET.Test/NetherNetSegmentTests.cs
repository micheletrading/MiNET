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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Net.NetherNet;

namespace MiNET.Test
{
	/// <summary>
	///     NetherNet's framing is a single byte, which makes it easy to get subtly wrong and
	///     impossible to notice until a chunk batch is large enough to split. The header counts
	///     segments still to come, so it runs downwards and ends at zero, and a reader that assumed
	///     it counted upwards would work perfectly for every message that fits in one piece.
	/// </summary>
	[TestClass]
	public class NetherNetSegmentTests
	{
		private static byte[] Payload(int length) => Enumerable.Range(0, length).Select(i => (byte) (i % 251)).ToArray();

		/// <summary>Feeds every segment in order and returns the completed message.</summary>
		private static byte[] Reassemble(NetherNetSegmentReassembler reassembler, IEnumerable<byte[]> segments)
		{
			byte[] result = null;
			foreach (byte[] segment in segments)
			{
				if (reassembler.TryAccept(segment, out ReadOnlyMemory<byte> message)) result = message.ToArray();
			}

			return result;
		}

		/// <summary>
		///     The overwhelmingly common case. Anything that fits goes out as one message behind a
		///     zero, and the zero means "complete" rather than "first of one".
		/// </summary>
		[TestMethod]
		public void AMessageThatFitsIsOneSegmentBehindAZeroByte()
		{
			byte[] payload = Payload(100);

			List<byte[]> segments = NetherNetSegments.Split(payload, 1024);

			Assert.AreEqual(1, segments.Count);
			Assert.AreEqual(0, segments[0][0]);
			CollectionAssert.AreEqual(payload, segments[0].Skip(1).ToArray());
		}

		/// <summary>
		///     The header counts down, so the first segment on the wire carries the highest value and
		///     the last carries zero. This is the detail the format hinges on.
		/// </summary>
		[TestMethod]
		public void HeadersCountDownAndEndAtZero()
		{
			// 10 usable bytes per segment, 25 bytes of payload, so three segments.
			List<byte[]> segments = NetherNetSegments.Split(Payload(25), 11);

			CollectionAssert.AreEqual(new byte[] {2, 1, 0}, segments.Select(s => s[0]).ToArray());
		}

		/// <summary>
		///     Split then reassemble has to give back exactly what went in, at sizes either side of a
		///     segment boundary so an off-by-one in the last segment cannot hide.
		/// </summary>
		[TestMethod]
		public void SplitThenReassembleRoundTripsAtEverySizeAroundTheBoundary()
		{
			const int maxMessageSize = 64;

			foreach (int length in new[] {0, 1, 62, 63, 64, 126, 127, 1000})
			{
				byte[] payload = Payload(length);
				byte[] result = Reassemble(new NetherNetSegmentReassembler(), NetherNetSegments.Split(payload, maxMessageSize));

				CollectionAssert.AreEqual(payload, result, $"payload of {length} bytes");
			}
		}

		/// <summary>
		///     An unfragmented message must be handed back as a view onto the caller's own buffer, not
		///     a copy of it. This is the whole point of the receive path: skipping the header byte is
		///     an offset, not a memcpy of the entire batch. Proven by mutating the source afterwards,
		///     because a copy would not notice.
		/// </summary>
		[TestMethod]
		public void AnUnfragmentedMessageIsAViewNotACopy()
		{
			byte[] framed = NetherNetSegments.Split(Payload(64), 1024).Single();
			var reassembler = new NetherNetSegmentReassembler();

			Assert.IsTrue(reassembler.TryAccept(framed, out ReadOnlyMemory<byte> message));

			framed[1] = 0xEE;

			Assert.AreEqual(0xEE, message.Span[0], "message should alias the caller's buffer, not copy it");
		}

		/// <summary>
		///     Nothing is delivered until the final segment lands. A reader that returned early would
		///     hand a truncated packet to the decoder, which is a far worse failure than waiting.
		/// </summary>
		[TestMethod]
		public void NothingIsDeliveredUntilTheFinalSegment()
		{
			List<byte[]> segments = NetherNetSegments.Split(Payload(25), 11);
			var reassembler = new NetherNetSegmentReassembler();

			Assert.IsFalse(reassembler.TryAccept(segments[0], out _));
			Assert.IsFalse(reassembler.TryAccept(segments[1], out _));
			Assert.IsTrue(reassembler.TryAccept(segments[2], out _));
		}

		/// <summary>
		///     The pooled buffer is kept rented between messages so a fragmenting peer does not re-rent
		///     on every one, which means "is a message in progress" cannot be inferred from the buffer
		///     existing. Get that wrong and the first unfragmented message after a fragmented one is
		///     treated as a continuation, which is silent corruption rather than an error.
		/// </summary>
		[TestMethod]
		public void AnUnfragmentedMessageAfterAFragmentedOneIsStillUnfragmented()
		{
			var reassembler = new NetherNetSegmentReassembler();

			byte[] first = Payload(25);
			CollectionAssert.AreEqual(first, Reassemble(reassembler, NetherNetSegments.Split(first, 11)));

			byte[] second = Payload(8);
			CollectionAssert.AreEqual(second, Reassemble(reassembler, NetherNetSegments.Split(second, 1024)));
		}

		/// <summary>
		///     The countdown is the only integrity check the format has. SCTP is ordered and reliable,
		///     so a gap cannot happen on a healthy channel, which is exactly why it must be treated as
		///     a broken session rather than quietly patched over.
		/// </summary>
		[TestMethod]
		public void AGapInTheCountdownIsAnError()
		{
			List<byte[]> segments = NetherNetSegments.Split(Payload(25), 11);
			var reassembler = new NetherNetSegmentReassembler();

			reassembler.TryAccept(segments[0], out _);

			Assert.ThrowsExactly<IOException>(() => reassembler.TryAccept(segments[2], out _));
		}

		/// <summary>
		///     After a broken message the reassembler must not carry the half-built buffer into the
		///     next one, or a single bad frame corrupts every packet that follows it.
		/// </summary>
		[TestMethod]
		public void AFailedMessageDoesNotPoisonTheNextOne()
		{
			List<byte[]> broken = NetherNetSegments.Split(Payload(25), 11);
			var reassembler = new NetherNetSegmentReassembler();

			reassembler.TryAccept(broken[0], out _);
			Assert.ThrowsExactly<IOException>(() => reassembler.TryAccept(broken[2], out _));

			byte[] good = Payload(40);
			CollectionAssert.AreEqual(good, Reassemble(reassembler, NetherNetSegments.Split(good, 11)));
		}

		/// <summary>
		///     An empty data channel message has no header at all, so it cannot be interpreted. Real
		///     clients do not send these, which is precisely why it must not be silently accepted.
		/// </summary>
		[TestMethod]
		public void AnEmptyMessageIsAnError()
		{
			var reassembler = new NetherNetSegmentReassembler();

			Assert.ThrowsExactly<IOException>(() => reassembler.TryAccept(Array.Empty<byte>(), out _));
		}
	}
}
