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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using log4net;
using Newtonsoft.Json;

namespace MiNET.Utils.Cryptography
{
	/// <summary>
	///     What has to survive between logins so a human is not asked for a device code every run.
	/// </summary>
	public class XboxSession
	{
		/// <summary>Microsoft account refresh token. Trades for a new access token without a prompt.</summary>
		public string RefreshToken { get; set; }

		/// <summary>
		///     Xbox device identifier. Bound to <see cref="AuthPrivateKey" />: Xbox records which proof
		///     key registered an id and refuses that id with any other key, so the two are one
		///     credential in two halves and must be replaced together.
		/// </summary>
		public string DeviceId { get; set; }

		/// <summary>PKCS#8, base64. The P-256 proof-of-possession key that signs Xbox requests.</summary>
		public string AuthPrivateKey { get; set; }

		/// <summary>
		///     PKCS#8, base64. The P-384 key the authorization service names in the token's cpk claim.
		///     Reusing it keeps us the same player, and losing it invalidates any token already issued.
		/// </summary>
		public string IdentityPrivateKey { get; set; }

		public DateTime SavedUtc { get; set; }
	}

	public interface IXboxSessionStore
	{
		XboxSession Load();
		void Save(XboxSession session);
		void Clear();
	}

	/// <summary>
	///     Stores the session in a file, encrypted with DPAPI at CurrentUser scope on Windows so only
	///     the signed-in user can read it.
	///     <para>
	///         There is no cross-platform equivalent in .NET. On other systems the file is written
	///         with owner-only permissions and a warning, because pretending otherwise would be worse
	///         than saying so. Anywhere the difference matters, supply your own
	///         <see cref="IXboxSessionStore" /> backed by the platform's secret store.
	///     </para>
	/// </summary>
	public class ProtectedFileSessionStore : IXboxSessionStore
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ProtectedFileSessionStore));

		private readonly string _path;

		public ProtectedFileSessionStore(string path = null)
		{
			_path = path ?? Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"MiNET", "xbox-session.dat");
		}

		public XboxSession Load()
		{
			try
			{
				if (!File.Exists(_path)) return null;

				byte[] raw = File.ReadAllBytes(_path);
				byte[] plain = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? ProtectedData.Unprotect(raw, null, DataProtectionScope.CurrentUser)
					: raw;

				return JsonConvert.DeserializeObject<XboxSession>(Encoding.UTF8.GetString(plain));
			}
			catch (Exception e)
			{
				// A session we cannot read is a session we acquire again, never a reason to fail.
				Log.Warn($"Could not read the saved Xbox session, signing in again: {e.Message}");
				return null;
			}
		}

		public void Save(XboxSession session)
		{
			session.SavedUtc = DateTime.UtcNow;

			Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

			byte[] plain = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(session));

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				File.WriteAllBytes(_path, ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser));
			}
			else
			{
				File.WriteAllBytes(_path, plain);
				try
				{
					File.SetUnixFileMode(_path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
				}
				catch (Exception e)
				{
					Log.Warn($"Could not restrict permissions on {_path}: {e.Message}");
				}

				Log.Warn($"Xbox session stored unencrypted at {_path}: DPAPI is Windows only. Supply an IXboxSessionStore to do better.");
			}
		}

		public void Clear()
		{
			if (File.Exists(_path)) File.Delete(_path);
		}
	}
}
