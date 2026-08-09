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
using log4net;
using MiNET.Utils;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

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

		// P-384, matching the curve BDS uses for the same purpose and the ES384 the assertion signs with.
		private const string Secp384R1 = "1.3.132.0.34";

		public AsymmetricCipherKeyPair Key { get; }

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

		private static AsymmetricCipherKeyPair Load(string path)
		{
			try
			{
				if (!File.Exists(path)) return null;

				using var reader = new PemReader(new StringReader(File.ReadAllText(path)));
				object read = reader.ReadObject();

				AsymmetricCipherKeyPair pair = read switch
				{
					AsymmetricCipherKeyPair keyPair => keyPair,
					// A PKCS#8 PRIVATE KEY block gives only the private half; the public point is
					// derivable, so a key written by OpenSSL in either form still loads.
					ECPrivateKeyParameters priv => new AsymmetricCipherKeyPair(
						new ECPublicKeyParameters(priv.AlgorithmName, priv.Parameters.G.Multiply(priv.D).Normalize(), priv.Parameters), priv),
					_ => null
				};

				if (pair == null)
				{
					Log.Warn($"{path} does not contain an EC private key, generating a new server identity");
					return null;
				}

				Log.Info($"NetherNet server identity loaded from {path}");
				return pair;
			}
			catch (Exception e)
			{
				// Refusing to start over an unreadable key would be worse, but silently replacing it
				// re-prompts every player, so this is deliberately loud.
				Log.Error($"Could not read the NetherNet server identity at {path}. A new one will be generated and every returning player will see a trust prompt.", e);
				return null;
			}
		}

		private static AsymmetricCipherKeyPair Create(string path)
		{
			var generator = new ECKeyPairGenerator("ECDSA");
			generator.Init(new ECKeyGenerationParameters(new DerObjectIdentifier(Secp384R1), new SecureRandom()));
			AsymmetricCipherKeyPair pair = generator.GenerateKeyPair();

			try
			{
				string directory = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

				using var writer = new StringWriter();
				new PemWriter(writer).WriteObject(PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private));
				File.WriteAllText(path, writer.ToString());

				Log.Info($"Generated a new NetherNet server identity at {path}. Keep this file: clients recognise this server by this key.");
			}
			catch (Exception e)
			{
				Log.Error($"Could not persist the NetherNet server identity to {path}. It will change on restart and re-prompt every player.", e);
			}

			return pair;
		}
	}
}
