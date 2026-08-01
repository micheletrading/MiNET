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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MiNET
{
	// Wire shape is an ordered array (see PlayerAttributes/EntityAttributes ProtoDef), and vanilla
	// BDS can legitimately repeat the same attribute name more than once in it - a Dictionary
	// keyed by name silently collapses those repeats, losing data on decode and desyncing a
	// decode->encode round trip. List-backed to preserve order and duplicates; the string indexer
	// keeps the upsert-by-name call pattern used everywhere server code constructs an outbound set
	// from scratch (those never contain duplicates, so upsert semantics are unaffected).
	public class PlayerAttributes : List<PlayerAttribute>
	{
		public PlayerAttribute this[string name]
		{
			get => this.FirstOrDefault(a => a.Name == name);
			set
			{
				int index = FindIndex(a => a.Name == name);
				if (index >= 0) this[index] = value;
				else Add(value);
			}
		}
	}

	public class EntityAttributes : List<EntityAttribute>
	{
		public EntityAttribute this[string name]
		{
			get => this.FirstOrDefault(a => a.Name == name);
			set
			{
				int index = FindIndex(a => a.Name == name);
				if (index >= 0) this[index] = value;
				else Add(value);
			}
		}
	}

	public class EntityLink
	{
		public long FromEntityId { get; set; }
		public long ToEntityId { get; set; }
		public EntityLinkType Type { get; set; }
		public bool Immediate { get; set; }
		public bool CausedByRider { get; set; }

		public EntityLink(long fromEntityId, long toEntityId, EntityLinkType type, bool immediate, bool causedByRider)
		{
			FromEntityId = fromEntityId;
			ToEntityId = toEntityId;
			Type = type;
			Immediate = immediate;
			CausedByRider = causedByRider;
		}
		
		public enum EntityLinkType : byte
		{
			Remove = 0,
			Rider = 1,
			Passenger = 2
		}
	}
	
	public class EntityLinks : List<EntityLink>
	{
	}

	public class GameRules : HashSet<GameRule>
	{
	}

}