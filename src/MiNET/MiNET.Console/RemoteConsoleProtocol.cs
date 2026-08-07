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
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiNET.Console
{
	/// <summary>
	///     Wire format shared by both ends of the remote console.
	///     <para>
	///         Every message is a little-endian int32 byte count followed by that many bytes of UTF-8.
	///         A connection opens with the server sending a fresh random nonce and the client replying
	///         with HMAC-SHA256 of that nonce under the shared secret. The secret itself never travels,
	///         and because the nonce is new for every connection a captured exchange cannot be replayed.
	///     </para>
	/// </summary>
	public static class RemoteConsoleProtocol
	{
		/// <summary>Bounds the allocation a frame header can ask for, so a bad peer cannot exhaust memory.</summary>
		public const int MaxFrameLength = 1024 * 1024;

		public const int NonceLength = 32;

		public const string Accepted = "OK";
		public const string Denied = "DENIED";

		public static async Task WriteFrameAsync(Stream stream, string text, CancellationToken cancellation)
		{
			byte[] payload = Encoding.UTF8.GetBytes(text);
			byte[] header = new byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

			await stream.WriteAsync(header, cancellation);
			await stream.WriteAsync(payload, cancellation);
			await stream.FlushAsync(cancellation);
		}

		/// <returns>The frame text, or null when the peer closed cleanly.</returns>
		public static async Task<string> ReadFrameAsync(Stream stream, CancellationToken cancellation)
		{
			byte[] header = new byte[4];
			if (!await ReadExactlyAsync(stream, header, cancellation)) return null;

			int length = BinaryPrimitives.ReadInt32LittleEndian(header);
			if (length < 0 || length > MaxFrameLength) throw new IOException($"Frame length {length} out of range");
			if (length == 0) return string.Empty;

			byte[] payload = new byte[length];
			if (!await ReadExactlyAsync(stream, payload, cancellation)) return null;

			return Encoding.UTF8.GetString(payload);
		}

		private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellation)
		{
			int read = 0;
			while (read < buffer.Length)
			{
				int count = await stream.ReadAsync(buffer.AsMemory(read), cancellation);
				if (count == 0) return false;
				read += count;
			}

			return true;
		}

		public static string CreateNonce()
		{
			return Convert.ToHexString(RandomNumberGenerator.GetBytes(NonceLength));
		}

		public static string Answer(string secret, string nonce)
		{
			using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
			return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(nonce)));
		}

		/// <summary>Compared in fixed time so a wrong answer leaks nothing through how long it took to reject.</summary>
		public static bool AnswerIsValid(string secret, string nonce, string answer)
		{
			if (string.IsNullOrEmpty(answer)) return false;

			byte[] expected = Encoding.UTF8.GetBytes(Answer(secret, nonce));
			byte[] actual = Encoding.UTF8.GetBytes(answer);

			return CryptographicOperations.FixedTimeEquals(expected, actual);
		}
	}
}
