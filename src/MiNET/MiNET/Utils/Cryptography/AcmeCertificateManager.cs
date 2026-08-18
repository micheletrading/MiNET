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
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace MiNET.Utils.Cryptography
{
	/// <summary>
	///     Owns one domain's TLS certificate for the signaling port: issues it from Let's Encrypt
	///     over ACME HTTP-01 through the listener's own challenge route, renews it 30 days before
	///     expiry, and persists the PEM pair plus the ACME account key in one directory. Before any
	///     order it preflights its own responder over plain HTTP, because Let's Encrypt rate-limits
	///     failed validations and a misrouted port 80 would burn that budget for nothing.
	/// </summary>
	public class AcmeCertificateManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(AcmeCertificateManager));

		private const string ChallengePrefix = "/.well-known/acme-challenge/";
		private const int RenewDaysBeforeExpiry = 30;

		private readonly string _domain;
		private readonly string _directory;
		private readonly string _contactEmail;
		private readonly bool _staging;

		/// <summary>Where the preflight probe dials the domain. Always 80 in production, HTTP-01's fixed port; a constructor override exists so a test can aim the probe at an ephemeral listener.</summary>
		private readonly int _probePort;

		/// <summary>Key authorizations by token: the live HTTP-01 challenges of an order in flight, plus the preflight probe's own token while it runs.</summary>
		private readonly ConcurrentDictionary<string, string> _challenges = new();

		private volatile SslStreamCertificateContext _context;
		private Timer _renewTimer;
		private int _ordering;

		/// <summary>The machine's default gateway addresses, enumerated once: a connection whose SOURCE is a gateway is hairpin traffic through the NAT router, meaning the client dialled the external address.</summary>
		private readonly IReadOnlyCollection<System.Net.IPAddress> _gateways;

		public AcmeCertificateManager(string domain, string directory, string contactEmail = null, bool staging = false, int probePort = 80, IEnumerable<System.Net.IPAddress> gatewayOverride = null)
		{
			if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentException("A domain is required", nameof(domain));
			if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A certificate directory is required", nameof(directory));

			_domain = domain;
			_directory = directory;
			_contactEmail = contactEmail;
			_staging = staging;
			_probePort = probePort;
			_gateways = gatewayOverride?.ToArray() ?? EnumerateGateways();
		}

		private static IReadOnlyCollection<System.Net.IPAddress> EnumerateGateways()
		{
			try
			{
				return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
					.Where(nic => nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
					.SelectMany(nic => nic.GetIPProperties().GatewayAddresses)
					.Select(gateway => gateway.Address)
					.Distinct()
					.ToArray();
			}
			catch (Exception e)
			{
				Log.Warn("Could not enumerate gateways; hairpin detection is off and only public sources count as external", e);
				return Array.Empty<System.Net.IPAddress>();
			}
		}

		// Domain-named, so the directory reads as what it holds and a second domain can share it.
		private string CertificatePath => Path.Combine(_directory, $"{_domain}.cert.pem");
		private string KeyPath => Path.Combine(_directory, $"{_domain}.key.pem");
		private string AccountKeyPath => Path.Combine(_directory, $"{_domain}.account.pem");

		/// <summary>
		///     The certificate context for a TLS offer, or null to refuse into the plaintext
		///     fallback. An SNI match serves outright; the interesting case is no SNI at all, which
		///     is every real Bedrock client (verified live: a join with the name typed in still
		///     offers a nameless ClientHello). Those are decided by the connection's SOURCE: a
		///     gateway address is hairpin through the NAT router and a public address is an external
		///     client, both meaning the external address was dialled, and both get the certificate;
		///     LAN and loopback sources keep the refusal that is known to work.
		/// </summary>
		public SslStreamCertificateContext GetCertificateContext(string sniHost, System.Net.IPAddress remoteAddress)
		{
			SslStreamCertificateContext context = _context;
			if (context == null) return null;

			if (sniHost != null) return _domain.Equals(sniHost, StringComparison.OrdinalIgnoreCase) ? context : null;

			return IsExternalPath(remoteAddress, _gateways) ? context : null;
		}

		/// <summary>External path = the client dialled the external address: its source is a default gateway (NAT hairpin) or a public address. Loopback and other private sources are not.</summary>
		internal static bool IsExternalPath(System.Net.IPAddress remoteAddress, IReadOnlyCollection<System.Net.IPAddress> gateways)
		{
			if (remoteAddress == null) return false;
			if (remoteAddress.IsIPv4MappedToIPv6) remoteAddress = remoteAddress.MapToIPv4();

			if (System.Net.IPAddress.IsLoopback(remoteAddress)) return false;
			if (gateways != null && gateways.Any(gateway => gateway.Equals(remoteAddress))) return true;

			return !IsPrivate(remoteAddress);
		}

		private static bool IsPrivate(System.Net.IPAddress address)
		{
			if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
			{
				byte[] bytes = address.GetAddressBytes();
				return bytes[0] == 10
					|| (bytes[0] == 172 && (bytes[1] & 0xF0) == 16)
					|| (bytes[0] == 192 && bytes[1] == 168)
					|| (bytes[0] == 169 && bytes[1] == 254);
			}

			// IPv6: link-local fe80::/10 and unique-local fc00::/7.
			return address.IsIPv6LinkLocal || (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
		}

		/// <summary>The key authorization for an ACME HTTP-01 token of an order in flight, or null for anything unknown.</summary>
		public string GetChallengeResponse(string token)
		{
			return token != null && _challenges.TryGetValue(token, out string keyAuthorization) ? keyAuthorization : null;
		}

		/// <summary>Loads whatever the directory holds, then checks hourly whether an issue or renewal is due. The cadence is retry pace, not renewal precision: the renewal window is 30 days wide.</summary>
		public void Start()
		{
			LoadCertificateFromDisk();
			_renewTimer = new Timer(_ => _ = EnsureCertificateAsync(), null, TimeSpan.Zero, TimeSpan.FromHours(1));
		}

		public void Stop()
		{
			_renewTimer?.Dispose();
			_renewTimer = null;
		}

		/// <summary>No certificate renews immediately; a held one renews inside the last 30 days of its life, so a failed attempt has a month of hourly retries before the cliff.</summary>
		internal static bool NeedsRenewal(X509Certificate2 certificate, DateTime now)
		{
			if (certificate == null) return true;

			return certificate.NotAfter - now < TimeSpan.FromDays(RenewDaysBeforeExpiry);
		}

		/// <summary>
		///     Proves the world can reach our own responder before any ACME order runs: registers a
		///     throwaway token, dials the domain over plain HTTP the way the CA's validators will,
		///     and requires our own value back. Anything else - refused, timed out, or answered by
		///     some other server that owns the port - fails the flight and skips the order.
		/// </summary>
		internal async Task<bool> PreflightAsync()
		{
			string token = "preflight-" + Guid.NewGuid().ToString("N");
			string expected = Guid.NewGuid().ToString("N");
			_challenges[token] = expected;

			try
			{
				using var http = new HttpClient {Timeout = TimeSpan.FromSeconds(10)};
				string url = $"http://{_domain}:{_probePort}{ChallengePrefix}{token}";
				string answer = await http.GetStringAsync(url);

				if (answer == expected) return true;

				Log.Warn($"ACME preflight for {_domain}: {url} answered, but not with our probe value; something else owns that port");
				return false;
			}
			catch (Exception e)
			{
				Log.Warn($"ACME preflight for {_domain} could not reach its own responder: {e.Message}");
				return false;
			}
			finally
			{
				_challenges.TryRemove(token, out _);
			}
		}

		/// <summary>The timer callback. Never throws: this runs unobserved, and a failed attempt's whole contract is "log it, try again next hour".</summary>
		private async Task EnsureCertificateAsync()
		{
			if (Interlocked.Exchange(ref _ordering, 1) != 0) return;
			try
			{
				if (!NeedsRenewal(_context?.TargetCertificate, DateTime.Now)) return;

				if (!await PreflightAsync())
				{
					Log.Warn($"Skipping the ACME order for {_domain}: the preflight probe did not round-trip, and a doomed validation would spend one of Let's Encrypt's rate-limited failures. Retrying next hour.");
					return;
				}

				await OrderCertificateAsync();
			}
			catch (Exception e)
			{
				Log.Error($"Issuing or renewing the certificate for {_domain} failed; retrying next hour", e);
			}
			finally
			{
				_ordering = 0;
			}
		}

		private static readonly Uri LetsEncrypt = new("https://acme-v02.api.letsencrypt.org/directory");
		private static readonly Uri LetsEncryptStaging = new("https://acme-staging-v02.api.letsencrypt.org/directory");

		private async Task OrderCertificateAsync()
		{
			using AcmeClient acme = await ConnectAccountAsync(_staging ? LetsEncryptStaging : LetsEncrypt);

			Log.Info($"Ordering a certificate for {_domain} from {(_staging ? "Let's Encrypt staging" : "Let's Encrypt")}");
			AcmeOrder order = await acme.CreateOrderAsync(_domain);
			(string challengeUrl, string token) = await acme.GetHttpChallengeAsync(order.AuthorizationUrls.First());

			_challenges[token] = acme.KeyAuthorization(token);
			try
			{
				await acme.TriggerChallengeAsync(challengeUrl);

				// The CA checks from several network vantage points and flips the challenge when
				// they agree; poll until it lands one way or the other.
				DateTime deadline = DateTime.UtcNow.AddSeconds(60);
				while (true)
				{
					(string status, string error) = await acme.GetChallengeStatusAsync(challengeUrl);
					if (status == "valid") break;
					if (status == "invalid") throw new IOException($"ACME validation for {_domain} failed: {error ?? "no detail given"}");
					if (DateTime.UtcNow > deadline) throw new TimeoutException($"ACME validation for {_domain} did not complete in time");
					await Task.Delay(1000);
				}

				using var certificateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
				string pemChain = await acme.FinalizeAsync(order, BuildCertificateRequest(_domain, certificateKey), TimeSpan.FromSeconds(60));

				System.IO.Directory.CreateDirectory(_directory);
				WriteAtomically(CertificatePath, pemChain);
				WriteAtomically(KeyPath, certificateKey.ExportPkcs8PrivateKeyPem());

				LoadCertificateFromDisk();
				Log.Info($"Certificate for {_domain} issued, valid to {_context?.TargetCertificate.NotAfter:yyyy-MM-dd}");
			}
			finally
			{
				_challenges.TryRemove(token, out _);
			}
		}

		/// <summary>The CSR the order is finalized with: the domain rides in the SAN, which is what the CA validates; the CN is tradition.</summary>
		internal static byte[] BuildCertificateRequest(string domain, ECDsa key)
		{
			var request = new CertificateRequest($"CN={domain}", key, HashAlgorithmName.SHA256);
			var san = new SubjectAlternativeNameBuilder();
			san.AddDnsName(domain);
			request.CertificateExtensions.Add(san.Build());

			return request.CreateSigningRequest();
		}

		/// <summary>
		///     The persisted account key is the ACME identity; losing it is harmless (a new account
		///     is free) but keeping it is what makes renewals count against one account's limits
		///     rather than many. ImportFromPem reads both this client's PKCS8 and the SEC1 the old
		///     Certes-based version wrote, so existing accounts carry over.
		/// </summary>
		private async Task<AcmeClient> ConnectAccountAsync(Uri directoryUri)
		{
			var accountKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
			if (File.Exists(AccountKeyPath))
			{
				accountKey.ImportFromPem(await File.ReadAllTextAsync(AccountKeyPath));
			}
			else
			{
				System.IO.Directory.CreateDirectory(_directory);
				WriteAtomically(AccountKeyPath, accountKey.ExportPkcs8PrivateKeyPem());
			}

			var acme = new AcmeClient(directoryUri, accountKey);
			try
			{
				await acme.InitializeAsync();
				await acme.EnsureAccountAsync(_contactEmail);
			}
			catch
			{
				acme.Dispose();
				throw;
			}

			return acme;
		}

		/// <summary>
		///     Loads cert.pem/key.pem into a served-ready context. The leaf is round-tripped through
		///     PFX because Windows' TLS stack cannot serve from an ephemeral private key; harmless
		///     elsewhere. cert.pem carries the full chain, and the intermediates ride along in the
		///     handshake so a client that cannot fetch them still builds a path to the root.
		/// </summary>
		internal void LoadCertificateFromDisk()
		{
			if (!File.Exists(CertificatePath) || !File.Exists(KeyPath)) return;

			try
			{
				X509Certificate2 leaf;
				using (X509Certificate2 ephemeral = X509Certificate2.CreateFromPemFile(CertificatePath, KeyPath))
				{
					leaf = X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null);
				}

				var chain = new X509Certificate2Collection();
				chain.ImportFromPemFile(CertificatePath);

				_context = SslStreamCertificateContext.Create(leaf, chain, offline: true);
				Log.Info($"Loaded certificate for {_domain}, valid to {leaf.NotAfter:yyyy-MM-dd}");
			}
			catch (Exception e)
			{
				Log.Error($"Could not load the certificate pair in {_directory}", e);
			}
		}

		/// <summary>Write-to-temp then rename, so a reader never sees a half-written PEM.</summary>
		private static void WriteAtomically(string path, string content)
		{
			string temp = path + ".tmp";
			File.WriteAllText(temp, content);
			File.Move(temp, path, true);
		}
	}
}