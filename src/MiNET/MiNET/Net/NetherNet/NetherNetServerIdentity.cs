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
using System.IO;
using System.Security.Cryptography;
using log4net;
using MiNET.Utils;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     The operator keypair a NetherNet server identifies itself with.
	///     <para>
	///         This is the value clients pin. Every player who has connected before recognises the
	///         server by this key alone, not by its address, so it must survive restarts: generating
	///         a fresh one makes every returning player see a first-use trust prompt again, as though
	///         they had never played here. That is why it is written to disk rather than kept in
	///         memory, and why BDS keeps the equivalent at <c>keys/server_identity_key.pem</c>.
	///     </para>
	///     <para>
	///         Sharing one key across a fleet makes those servers one trust unit, which is what lets
	///         a TransferPacket move a player between them without re-prompting.
	///     </para>
	/// </summary>
	public class NetherNetServerIdentity
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetherNetServerIdentity));

		public ECDsa Key { get; }

		/// <summary>Surfaced to the player as untrusted display text in the first-use prompt.</summary>
		public string Issuer { get; }

		public string Domain { get; }

		public NetherNetServerIdentity(string path = null, string issuer = null, string domain = null)
		{
			// Beside the executable, not the working directory. A bare relative path lands wherever
			// the process happened to be started from, which for "dotnet run" is the repository root,
			// and a private key does not belong in a source tree. This is also where the server's
			// other runtime files live and where BDS keeps the same file.
			path ??= Path.Combine(AppContext.BaseDirectory, "keys", "server_identity_key.pem");
			Issuer = issuer ?? Config.GetProperty("motd", "MiNET Server");
			Domain = domain ?? Config.GetProperty("NetherNetDomain", "minet.local");

			Key = Load(path) ?? Create(path);
		}

		private static ECDsa Load(string path)
		{
			try
			{
				if (!File.Exists(path)) return null;

				// Reads both PEM forms this file has ever been written in: PKCS#8 "PRIVATE KEY", which
				// is what we write, and SEC1 "EC PRIVATE KEY", which is what OpenSSL writes by default.
				// The public half is derived from the private one either way, so neither needs to be
				// stored.
				var key = ECDsa.Create();
				key.ImportFromPem(File.ReadAllText(path));

				Log.Info($"NetherNet server identity loaded from {path}");
				return key;
			}
			catch (Exception e)
			{
				// Refusing to start over an unreadable key would be worse, but silently replacing it
				// re-prompts every player, so this is deliberately loud.
				Log.Error($"Could not read the NetherNet server identity at {path}. A new one will be generated and every returning player will see a trust prompt.", e);
				return null;
			}
		}

		private static ECDsa Create(string path)
		{
			// P-384, matching the curve BDS uses for the same purpose and the ES384 the assertion
			// signs with.
			var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);

			try
			{
				string directory = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

				// PKCS#8, the same "PRIVATE KEY" block written before, so a key from either era loads.
				File.WriteAllText(path, key.ExportPkcs8PrivateKeyPem());

				Log.Info($"Generated a new NetherNet server identity at {path}. Keep this file: clients recognise this server by this key.");
			}
			catch (Exception e)
			{
				Log.Error($"Could not persist the NetherNet server identity to {path}. It will change on restart and re-prompt every player.", e);
			}

			return key;
		}
	}
}
