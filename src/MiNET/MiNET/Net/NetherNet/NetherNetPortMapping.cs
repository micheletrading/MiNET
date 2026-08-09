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
using System.Net;
using System.Text.RegularExpressions;
using log4net;

namespace MiNET.Net.NetherNet
{
	/// <summary>
	///     Where gameplay UDP binds locally, and what address clients should be told to dial.
	///     <para>
	///         Signaling is TCP on the server port, but the gameplay path is a separate UDP socket
	///         that WebRTC allocates from the ephemeral range by default. Behind a router that is
	///         unusable: you cannot forward a port you cannot predict, and the candidate we advertise
	///         is the internal address, which no client outside the LAN can reach. So the range is
	///         pinned and the advertised candidates are rewritten to the mapped address.
	///     </para>
	///     <para>
	///         Configured with BDS's <c>server-udp-ports</c> syntax, deliberately, so a working BDS
	///         configuration can be copied across:
	///         <list type="bullet">
	///             <item><c>49152-49200</c> restricts local allocation to that range.</item>
	///             <item><c>19132-19232:32000-32100</c> binds locally to 32000-32100 and tells clients 19132-19232.</item>
	///             <item><c>203.0.113.10:19132-19232:32000-32100</c> also names the public address.</item>
	///         </list>
	///         Ranges paired by offset: internal start maps to external start, and so on.
	///     </para>
	/// </summary>
	public class NetherNetPortMapping
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(NetherNetPortMapping));

		// A range, "start" or "start-end". Deliberately not part of a single big pattern for the
		// whole entry: an address group permissive enough to accept a hostname also accepts
		// "19132-19232", so "19132-19232:32000-32100" parses as an address plus a bare range and
		// the mapping disappears without an error.
		private static readonly Regex Range = new(@"^(?<start>\d+)(?:-(?<end>\d+))?$", RegexOptions.Compiled);

		public sealed class Mapping
		{
			public int InternalStart { get; init; }
			public int InternalEnd { get; init; }
			public int ExternalStart { get; init; }
			public string Address { get; init; }

			public bool Contains(int port) => port >= InternalStart && port <= InternalEnd;

			/// <summary>Paired by offset, so the nth internal port is the nth external one.</summary>
			public int ToExternal(int port) => ExternalStart + (port - InternalStart);

			private string _resolved;
			private DateTime _resolvedAt;

			/// <summary>
			///     The address as an IP literal, since ICE candidates cannot carry a hostname.
			///     Re-resolved periodically rather than once at startup so a dynamic DNS name keeps
			///     working after the address behind it changes, which would otherwise leave every
			///     external client dialling an address that is no longer ours.
			/// </summary>
			public string ResolvedAddress
			{
				get
				{
					if (Address == null) return null;
					if (IPAddress.TryParse(Address, out _)) return Address;

					if (_resolved != null && DateTime.UtcNow - _resolvedAt < TimeSpan.FromMinutes(5)) return _resolved;

					try
					{
						IPAddress[] addresses = Dns.GetHostAddresses(Address);
						if (addresses.Length > 0)
						{
							_resolved = addresses[0].ToString();
							_resolvedAt = DateTime.UtcNow;
						}
					}
					catch (Exception e)
					{
						// Keep the last good answer rather than dropping the mapping: a DNS blip
						// should not silently make the server unreachable from outside.
						Log.Warn($"server-udp-ports: could not resolve \"{Address}\": {e.Message}");
					}

					return _resolved;
				}
			}
		}

		public List<Mapping> Mappings { get; } = new();

		/// <summary>The window gameplay sockets are allocated from, or null to let the OS choose.</summary>
		public int? RangeStart { get; private set; }

		public int? RangeEnd { get; private set; }

		public bool IsConfigured => Mappings.Count > 0 || RangeStart.HasValue;

		public static NetherNetPortMapping Parse(string configuration)
		{
			var mapping = new NetherNetPortMapping();
			if (string.IsNullOrWhiteSpace(configuration)) return mapping;

			foreach (string part in configuration.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				string rest = part;
				string address = null;

				// An IPv6 literal contains colons, so it is bracketed and taken off the front first.
				// Otherwise a leading segment is an address only when three segments are present,
				// which is what distinguishes "ip:external:internal" from "external:internal".
				if (rest.StartsWith('['))
				{
					int close = rest.IndexOf(']');
					if (close < 0 || close + 2 > rest.Length || rest[close + 1] != ':')
					{
						Log.Warn($"server-udp-ports: cannot parse \"{part}\", ignoring it");
						continue;
					}

					address = rest.Substring(1, close - 1);
					rest = rest.Substring(close + 2);
				}
				else if (rest.Count(c => c == ':') == 2)
				{
					int firstColon = rest.IndexOf(':');
					address = rest.Substring(0, firstColon);
					rest = rest.Substring(firstColon + 1);
				}

				string[] sides = rest.Split(':');
				if (sides.Length > 2 || !TryRange(sides[^1], out int internalStart, out int internalEnd))
				{
					Log.Warn($"server-udp-ports: cannot parse \"{part}\", ignoring it");
					continue;
				}

				if (sides.Length == 1)
				{
					if (address != null) Log.Warn($"server-udp-ports: \"{part}\" names an address but no external port range, ignoring the address");

					// Bare form: this is the local window, with nothing published.
					mapping.Widen(internalStart, internalEnd);
					continue;
				}

				if (!TryRange(sides[0], out int externalStart, out int externalEnd))
				{
					Log.Warn($"server-udp-ports: cannot parse \"{part}\", ignoring it");
					continue;
				}

				if (externalEnd - externalStart != internalEnd - internalStart)
				{
					Log.Warn($"server-udp-ports: \"{part}\" pairs {externalEnd - externalStart + 1} external ports with {internalEnd - internalStart + 1} internal ones, ignoring it");
					continue;
				}

				mapping.Widen(internalStart, internalEnd);
				mapping.Mappings.Add(new Mapping
				{
					InternalStart = internalStart,
					InternalEnd = internalEnd,
					ExternalStart = externalStart,
					Address = address
				});
			}

			if (mapping.IsConfigured)
			{
				Log.Info($"NetherNet gameplay UDP restricted to {mapping.RangeStart}-{mapping.RangeEnd}"
						+ (mapping.Mappings.Count > 0 ? $", advertising {mapping.Mappings.Count} mapping(s)" : ""));
			}

			return mapping;
		}

		/// <summary>
		///     A mapping with no address only changes the port, so it applies to either family. One
		///     that names an address applies only to candidates of the same family.
		/// </summary>
		private static bool FamilyMatches(string mappedAddress, string candidateAddress)
		{
			if (mappedAddress == null) return true;

			return IPAddress.TryParse(mappedAddress, out IPAddress mapped)
				&& IPAddress.TryParse(candidateAddress, out IPAddress candidate)
				&& mapped.AddressFamily == candidate.AddressFamily;
		}

		private static bool TryRange(string value, out int start, out int end)
		{
			start = end = 0;

			Match match = Range.Match(value);
			if (!match.Success) return false;

			start = int.Parse(match.Groups["start"].Value);
			end = match.Groups["end"].Success ? int.Parse(match.Groups["end"].Value) : start;

			return end >= start;
		}

		private void Widen(int start, int end)
		{
			RangeStart = RangeStart.HasValue ? Math.Min(RangeStart.Value, start) : start;
			RangeEnd = RangeEnd.HasValue ? Math.Max(RangeEnd.Value, end) : end;
		}

		/// <summary>
		///     Adds a server-reflexive candidate carrying the mapped address beside each host
		///     candidate it applies to, rather than replacing it.
		///     <para>
		///         Adding is the whole point. Replacing would advertise only the public address, and
		///         a client on the same LAN, or on this machine, can then reach us only if the router
		///         hairpins, which many do not. Offering both lets ICE decide: local peers succeed on
		///         the internal candidate, remote peers on the mapped one, and neither needs us to
		///         work out which kind of client we are talking to.
		///     </para>
		///     <para>
		///         Only host candidates are used as a base. Anything already reflexive or relayed
		///         describes an address as seen from outside, so translating it again would be wrong.
		///     </para>
		/// </summary>
		public string Apply(string sdp)
		{
			if (Mappings.Count == 0) return sdp;

			var lines = new List<string>();
			int added = 0;

			foreach (string raw in sdp.Split('\n'))
			{
				string line = raw.TrimEnd('\r');
				lines.Add(line);

				if (!line.StartsWith("a=candidate:", StringComparison.Ordinal)) continue;

				// a=candidate:<foundation> <component> <transport> <priority> <ip> <port> typ <type> ...
				string[] parts = line.Split(' ');
				if (parts.Length < 8 || !parts[7].Equals("host", StringComparison.OrdinalIgnoreCase)) continue;
				if (!int.TryParse(parts[5], out int port)) continue;

				// The address family has to match. A dual stack server offers an IPv4 and an IPv6
				// host candidate on the same port, so an IPv4 mapping must not be stamped onto the
				// IPv6 one, which would produce something no client can dial.
				Mapping match = Mappings.FirstOrDefault(m => m.Contains(port) && FamilyMatches(m.ResolvedAddress, parts[4]));
				if (match == null) continue;

				string address = match.ResolvedAddress ?? parts[4];
				int mapped = match.ToExternal(port);

				// A distinct foundation, because ICE treats candidates sharing one as the same base,
				// and raddr/rport naming the host candidate this was derived from, as RFC 8445 wants
				// for a reflexive candidate.
				var reflexive = new[]
				{
					$"a=candidate:{Math.Abs(HashCode.Combine(address, mapped))}",
					parts[1], parts[2], ReflexivePriority.ToString(), address, mapped.ToString(),
					"typ", "srflx", "raddr", parts[4], "rport", parts[5], "generation", "0"
				};

				lines.Add(string.Join(' ', reflexive));
				added++;
			}

			if (added > 0) Log.Debug($"NetherNet: added {added} mapped candidate(s) alongside the local ones");

			return string.Join("\r\n", lines);
		}

		// RFC 8445 priority: (2^24) * type preference + (2^8) * local preference + (256 - component).
		// Type preference 100 is the conventional value for server reflexive, below host's 126, so a
		// peer that can reach us directly prefers to.
		private const int ReflexivePriority = (100 << 24) + (65535 << 8) + 255;
	}
}
