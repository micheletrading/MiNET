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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using log4net;
using Newtonsoft.Json;

namespace MiNET.Parking
{
	/// <summary>
	///     One account's personal key to the transfer API, issued in world by <c>/token</c>. What it
	///     opens is deliberately narrow: the owner's own name on the front entrance, and full power
	///     on doors the owner registered, nothing anywhere else. The name is captured at issuance,
	///     so a rename means reissuing the key.
	/// </summary>
	public class AccessToken
	{
		public string OwnerId { get; set; }

		public string OwnerName { get; set; }

		/// <summary>SHA-256 of the plaintext key, hex. The plaintext exists only in the chat message that delivered it.</summary>
		public string KeyHash { get; set; }

		public DateTime IssuedUtc { get; set; }

		/// <summary>
		///     Why this key may not do what was asked, or null when it may. No door means the front
		///     entrance, where a key moves its owner and nobody else; a door answers only to the
		///     account that registered it, and there the wildcard is included.
		/// </summary>
		public string RefusalFor(Door door, string targetName)
		{
			if (door == null)
			{
				return string.Equals(targetName, OwnerName, StringComparison.OrdinalIgnoreCase)
					? null
					: "Your key moves only your own name on the front entrance.";
			}

			return door.OwnerId == OwnerId ? null : "That door is not yours.";
		}
	}

	/// <summary>
	///     The keys, one per account: issuing is also revoking, because the new key replaces the old
	///     one in the same slot. Only hashes are persisted, so the file on disk can name every owner
	///     without being able to impersonate any of them.
	/// </summary>
	public class TokenRegistry
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(TokenRegistry));

		private readonly ConcurrentDictionary<string, AccessToken> _byOwner = new ConcurrentDictionary<string, AccessToken>();
		private readonly string _path;
		private readonly object _write = new object();

		public TokenRegistry(string path)
		{
			_path = path;
			Load();
		}

		/// <summary>A fresh key for this account, returned as the only plaintext copy that will ever exist. Whatever key the account held before stops working here and now.</summary>
		public string Issue(string ownerId, string ownerName)
		{
			string key = "park_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

			lock (_write)
			{
				_byOwner[ownerId] = new AccessToken
				{
					OwnerId = ownerId,
					OwnerName = ownerName,
					KeyHash = Hash(key),
					IssuedUtc = DateTime.UtcNow
				};
				Save();
			}

			return key;
		}

		/// <summary>The token a plaintext key unlocks, or null. Comparison is by hash; the registry never holds the plaintext to compare against.</summary>
		public AccessToken Find(string key)
		{
			if (string.IsNullOrEmpty(key)) return null;

			string hash = Hash(key);
			return _byOwner.Values.FirstOrDefault(token => token.KeyHash == hash);
		}

		private static string Hash(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

		private void Load()
		{
			if (!File.Exists(_path)) return;

			try
			{
				var stored = JsonConvert.DeserializeObject<List<AccessToken>>(File.ReadAllText(_path)) ?? new List<AccessToken>();
				foreach (AccessToken token in stored) _byOwner[token.OwnerId] = token;
			}
			catch (Exception e)
			{
				Log.Error($"Could not read tokens from {_path}, starting with none", e);
			}
		}

		private void Save()
		{
			try
			{
				File.WriteAllText(_path, JsonConvert.SerializeObject(_byOwner.Values.OrderBy(token => token.OwnerId), Formatting.Indented));
			}
			catch (Exception e)
			{
				// Losing the file costs the keys at next start, not this session. Reporting and
				// continuing beats failing a command the player watched succeed.
				Log.Error($"Could not write tokens to {_path}", e);
			}
		}
	}
}