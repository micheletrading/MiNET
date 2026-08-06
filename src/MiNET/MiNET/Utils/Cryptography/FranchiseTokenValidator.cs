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
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using Jose;
using log4net;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace MiNET.Utils.Cryptography
{
	/// <summary>
	///     Verifies the multiplayer token a modern client logs in with.
	///
	///     Since protocol 944 the certificate chain is empty and identity travels in a single JWT
	///     issued by Mojang's franchise authorization service. Decoding it without checking the
	///     signature, which is what the login path does by default, means the gamertag and XUID are
	///     whatever the client typed: identity is spoofable by anyone who can format JSON.
	///
	///     This checks the signature against the issuer's published keys, then the issuer, audience
	///     and expiry. It does NOT by itself prove the connection owns the identity: see
	///     <see cref="ClientPublicKey" />, which the caller must compare against the key the
	///     encryption handshake actually used.
	/// </summary>
	public static class FranchiseTokenValidator
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(FranchiseTokenValidator));

		public const string Issuer = "https://authorization.franchise.minecraft-services.net/";
		public const string Audience = "api://auth-minecraft-services/multiplayer";

		private const string JwksUri = "https://authorization.franchise.minecraft-services.net/.well-known/keys";

		private static readonly HttpClient Http = new HttpClient {Timeout = TimeSpan.FromSeconds(10)};
		private static readonly object KeySync = new object();
		private static Dictionary<string, RSA> _keys = new Dictionary<string, RSA>(StringComparer.Ordinal);

		// Keys rotate. An unknown kid triggers one refetch, rate limited so a client sending
		// nonsense kids cannot turn logins into a request flood against Mojang.
		private static DateTime _lastFetch = DateTime.MinValue;
		private static readonly TimeSpan MinimumRefetchInterval = TimeSpan.FromMinutes(5);

		/// <summary>Tolerance on the expiry and not-before checks, for clock skew between us and the issuer.</summary>
		private static readonly TimeSpan ClockSkewLeeway = TimeSpan.FromMinutes(2);

		public class Identity
		{
			public string Xuid { get; init; }
			public string DisplayName { get; init; }

			/// <summary>
			///     The client's public key as the token asserts it (the "cpk" claim). The caller must
			///     check that the encryption handshake used THIS key; without that, a token captured
			///     from someone else's login replays fine.
			/// </summary>
			public string ClientPublicKey { get; init; }
		}

		/// <summary>
		///     Verifies signature, issuer, audience and expiry. Returns null and logs the reason when
		///     the token is not a genuine, current franchise token.
		/// </summary>
		public static Identity Validate(string token)
		{
			if (string.IsNullOrEmpty(token)) return null;

			try
			{
				IDictionary<string, object> headers = JWT.Headers(token);
				if (!headers.TryGetValue("kid", out object kidValue) || kidValue is not string kid)
				{
					Log.Warn("Login token rejected: no kid in the header, so no key can be selected for it");
					return null;
				}

				RSA key = ResolveKey(kid);
				if (key == null)
				{
					Log.Warn($"Login token rejected: signing key {kid} is not published by {Issuer}");
					return null;
				}

				// Throws when the signature does not verify; jose-jwt checks the algorithm too, so a
				// token claiming "none" or a symmetric algorithm cannot slip past.
				string payload = JWT.Decode(token, key, JwsAlgorithm.RS256);
				var claims = JObject.Parse(payload);

				string issuer = (string) claims["iss"];
				if (!Issuer.Equals(issuer, StringComparison.Ordinal))
				{
					Log.Warn($"Login token rejected: issued by '{issuer}', not {Issuer}");
					return null;
				}

				string audience = (string) claims["aud"];
				if (!Audience.Equals(audience, StringComparison.Ordinal))
				{
					Log.Warn($"Login token rejected: audience '{audience}' is not {Audience}");
					return null;
				}

				// The token is an entry ticket, not a heartbeat: it is checked once, and the session
				// stays authenticated afterwards by the encryption keyed from the cpk it names. A
				// four hour lifetime is therefore not a session limit.
				// The leeway is for clock skew. Without it a server running slightly fast refuses
				// genuine logins near the boundary, and the player only sees a generic kick.
				var exp = (long?) claims["exp"];
				if (exp == null || DateTimeOffset.FromUnixTimeSeconds(exp.Value) + ClockSkewLeeway <= DateTimeOffset.UtcNow)
				{
					Log.Warn($"Login token rejected: expired at {(exp == null ? "<no exp>" : DateTimeOffset.FromUnixTimeSeconds(exp.Value).ToString("u"))}");
					return null;
				}

				var nbf = (long?) claims["nbf"];
				if (nbf != null && DateTimeOffset.FromUnixTimeSeconds(nbf.Value) - ClockSkewLeeway > DateTimeOffset.UtcNow)
				{
					Log.Warn($"Login token rejected: not valid until {DateTimeOffset.FromUnixTimeSeconds(nbf.Value):u}");
					return null;
				}

				return new Identity
				{
					Xuid = (string) claims["xid"],
					DisplayName = (string) claims["xname"],
					ClientPublicKey = (string) claims["cpk"]
				};
			}
			catch (Exception e)
			{
				Log.Warn($"Login token rejected: {e.GetType().Name}: {e.Message}");
				return null;
			}
		}

		/// <summary>
		///     Checks that the client-data (skin) document was signed by the key the verified token
		///     names in cpk. Without this a verified identity could carry a client-data blob it did
		///     not sign: an attacker who captured someone's token could present it with their own
		///     appearance and device claims attached.
		/// </summary>
		public static bool VerifyClientData(string clientDataJwt, string clientPublicKey)
		{
			if (string.IsNullOrEmpty(clientDataJwt) || string.IsNullOrEmpty(clientPublicKey)) return false;

			try
			{
				var bouncyKey = (ECPublicKeyParameters) PublicKeyFactory.CreateKey(Convert.FromBase64String(clientPublicKey));
				var parameters = new ECParameters
				{
					Curve = ECCurve.NamedCurves.nistP384,
					Q =
					{
						X = bouncyKey.Q.AffineXCoord.GetEncoded(),
						Y = bouncyKey.Q.AffineYCoord.GetEncoded()
					}
				};
				parameters.Validate();

				// Throws unless the signature verifies under exactly this key and algorithm.
				JWT.Decode(clientDataJwt, ECDsa.Create(parameters), JwsAlgorithm.ES384);
				return true;
			}
			catch (Exception e)
			{
				Log.Warn($"Client data rejected: not signed by the key the token names ({e.GetType().Name}: {e.Message})");
				return false;
			}
		}

		private static RSA ResolveKey(string kid)
		{
			lock (KeySync)
			{
				if (_keys.TryGetValue(kid, out RSA cached)) return cached;
				if (DateTime.UtcNow - _lastFetch < MinimumRefetchInterval) return null;
			}

			Dictionary<string, RSA> fetched = FetchKeys();
			if (fetched == null) return null;

			lock (KeySync)
			{
				_keys = fetched;
				_lastFetch = DateTime.UtcNow;
				return _keys.TryGetValue(kid, out RSA key) ? key : null;
			}
		}

		private static Dictionary<string, RSA> FetchKeys()
		{
			try
			{
				string json = Http.GetStringAsync(JwksUri).GetAwaiter().GetResult();
				var keys = new Dictionary<string, RSA>(StringComparer.Ordinal);

				foreach (JToken jwk in JObject.Parse(json)["keys"] ?? new JArray())
				{
					string kid = (string) jwk["kid"];
					if (string.IsNullOrEmpty(kid) || (string) jwk["kty"] != "RSA") continue;

					var rsa = RSA.Create();
					rsa.ImportParameters(new RSAParameters
					{
						Modulus = Base64Url.Decode((string) jwk["n"]),
						Exponent = Base64Url.Decode((string) jwk["e"])
					});
					keys[kid] = rsa;
				}

				Log.Info($"Fetched {keys.Count} signing key(s) from {JwksUri}");
				return keys;
			}
			catch (Exception e)
			{
				// A failed fetch must not authenticate anyone: the caller sees no key and rejects.
				Log.Warn($"Could not fetch the login signing keys from {JwksUri}: {e.Message}");
				return null;
			}
		}
	}
}
