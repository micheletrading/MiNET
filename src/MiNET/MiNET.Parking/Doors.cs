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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using MiNET.Net.NetherNet;
using Newtonsoft.Json;

namespace MiNET.Parking
{
	/// <summary>
	///     One registered way in. Whoever holds the port sends players through it, and everyone who
	///     arrives on it goes back to the one destination it names.
	/// </summary>
	public class Door
	{
		/// <summary>The port on THIS server that was handed out. What a caller transfers players to.</summary>
		public int Port { get; set; }

		/// <summary>Where arrivals go back to.</summary>
		public string Address { get; set; }

		public int ReturnPort { get; set; }

		/// <summary>
		///     The registering account. An XUID when the server authenticates against Xbox Live,
		///     otherwise the name, which is only as trustworthy as the login was.
		/// </summary>
		public string OwnerId { get; set; }

		public string OwnerName { get; set; }

		/// <summary>Seconds until an arrival is sent back on its own. Zero or less leaves them here.</summary>
		public int AutoSeconds { get; set; }

		/// <summary>
		///     Who may come through, by NAME rather than by account: an arrival need not be Xbox
		///     authenticated, so a name is the only thing every visitor is guaranteed to have.
		///     <para>
		///         Empty means everyone, which is the default. Putting a single name in it turns the
		///         door private, so adding yourself is how you close it to everybody else.
		///     </para>
		/// </summary>
		public List<string> Allowed { get; set; } = new List<string>();

		/// <summary>Names refused regardless of <see cref="Allowed" />. A denial always wins.</summary>
		public List<string> Denied { get; set; } = new List<string>();

		public bool Admits(string username)
		{
			if (Denied.Any(name => string.Equals(name, username, StringComparison.OrdinalIgnoreCase))) return false;

			return Allowed.Count == 0 || Allowed.Any(name => string.Equals(name, username, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		///     What makes two registrations the same door. Everything the owner chose, so asking twice
		///     for the same thing returns the port they already have, and asking for anything different
		///     opens another. Ports are plentiful; there is no reason to ration them per person.
		/// </summary>
		public bool Matches(string ownerId, string address, int returnPort, int autoSeconds)
		{
			return OwnerId == ownerId
				&& string.Equals(Address, address, StringComparison.OrdinalIgnoreCase)
				&& ReturnPort == returnPort
				&& AutoSeconds == autoSeconds;
		}

		public override string ToString()
		{
			return $"{Port} -> {Address}:{ReturnPort}"
				+ (AutoSeconds > 0 ? $", auto after {AutoSeconds}s" : "")
				+ (Allowed.Count > 0 ? $", allowing {Allowed.Count}" : "")
				+ (Denied.Count > 0 ? $", denying {Denied.Count}" : "");
		}
	}

	/// <summary>
	///     The doors, and the ports they occupy. Registration is in world and therefore already
	///     authenticated, so the port itself is the only credential anything else needs: it is
	///     learned by being handed one, never by asking.
	/// </summary>
	public class DoorRegistry
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(DoorRegistry));

		private readonly ConcurrentDictionary<int, Door> _byPort = new ConcurrentDictionary<int, Door>();
		private readonly string _path;
		private readonly int _first;
		private readonly int _last;
		private readonly int _perOwner;
		private readonly object _allocation = new object();

		public DoorRegistry(string path, int first, int last, int perOwner)
		{
			_path = path;
			_first = first;
			_last = last;
			_perOwner = perOwner;
		}

		public Door ByPort(int port) => port != 0 && _byPort.TryGetValue(port, out Door door) ? door : null;

		public IReadOnlyCollection<Door> ByOwner(string ownerId)
		{
			return ownerId == null ? Array.Empty<Door>() : _byPort.Values.Where(door => door.OwnerId == ownerId).OrderBy(door => door.Port).ToArray();
		}

		public IReadOnlyCollection<Door> All => _byPort.Values.ToArray();

		/// <summary>
		///     Gives this owner a door for exactly this combination, or hands back the one they were
		///     already given for it. Different combinations are different doors, so one person can
		///     hold several, capped so nobody can drain the pool.
		///     <para>Null with a reason when the cap is reached or every port is taken.</para>
		/// </summary>
		public Door Register(string ownerId, string ownerName, string address, int returnPort, int autoSeconds, NetherNetListener listener, out string refusal)
		{
			refusal = null;

			lock (_allocation)
			{
				Door existing = _byPort.Values.FirstOrDefault(door => door.Matches(ownerId, address, returnPort, autoSeconds));
				if (existing != null) return existing;

				if (ByOwner(ownerId).Count >= _perOwner)
				{
					refusal = $"You already hold {_perOwner} doors, the limit. Release one first.";
					return null;
				}

				for (int port = _first; port <= _last; port++)
				{
					if (_byPort.ContainsKey(port)) continue;

					// The socket is opened BEFORE the door is recorded: a port the OS will not give
					// us must not become an entry that looks registered and answers nothing.
					if (!listener.AddSignalingPort(port)) continue;

					var door = new Door
					{
						Port = port,
						Address = address,
						ReturnPort = returnPort,
						OwnerId = ownerId,
						OwnerName = ownerName,
						AutoSeconds = autoSeconds
					};

					_byPort[port] = door;
					Save();

					return door;
				}

				refusal = "Every port is taken. Nothing to hand out.";
				return null;
			}
		}

		/// <summary>
		///     Releases one of this owner's doors, or all of them when no port is named. Only ever
		///     their own: a port number is not proof of anything on its own.
		/// </summary>
		/// <summary>
		///     Adds or removes a name on one of this owner's doors, and persists it. Returns false
		///     when they hold no such door, so a mistyped port cannot silently edit nothing.
		/// </summary>
		public bool Amend(string ownerId, int port, Action<Door> change)
		{
			lock (_allocation)
			{
				Door door = _byPort.Values.FirstOrDefault(candidate => candidate.Port == port && candidate.OwnerId == ownerId);
				if (door == null) return false;

				change(door);
				Save();

				return true;
			}
		}

		public int Release(string ownerId, int port, NetherNetListener listener)
		{
			lock (_allocation)
			{
				Door[] going = ByOwner(ownerId).Where(door => port <= 0 || door.Port == port).ToArray();

				foreach (Door door in going)
				{
					_byPort.TryRemove(door.Port, out _);
					listener.RemoveSignalingPort(door.Port);
				}

				if (going.Length > 0) Save();

				return going.Length;
			}
		}

		/// <summary>
		///     Reopens every stored door. Ports do not survive the process, only the registrations do,
		///     so this runs at startup and a door whose port is now taken by something else is dropped
		///     rather than silently pointing at a socket we do not own.
		/// </summary>
		public void Restore(NetherNetListener listener)
		{
			if (!File.Exists(_path)) return;

			try
			{
				var stored = JsonConvert.DeserializeObject<List<Door>>(File.ReadAllText(_path)) ?? new List<Door>();

				foreach (Door door in stored)
				{
					if (!listener.AddSignalingPort(door.Port))
					{
						Log.Warn($"Dropping door {door.Port} for {door.OwnerName}: the port could not be opened");
						continue;
					}

					_byPort[door.Port] = door;
				}

				Log.Info($"Restored {_byPort.Count} of {stored.Count} doors from {_path}");
			}
			catch (Exception e)
			{
				Log.Error($"Could not read doors from {_path}, starting with none", e);
			}
		}

		private void Save()
		{
			try
			{
				File.WriteAllText(_path, JsonConvert.SerializeObject(_byPort.Values.OrderBy(door => door.Port), Formatting.Indented));
			}
			catch (Exception e)
			{
				// Losing the file costs the registrations at next start, not this session. Reporting
				// and continuing beats failing a command the player watched succeed.
				Log.Error($"Could not write doors to {_path}", e);
			}
		}
	}
}
