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
using fNbt;
using Newtonsoft.Json;

namespace MiNET.Utils
{
	/// <summary>
	///     The palette is an ordered list and the order is its meaning: an entry's position is
	///     its runtime id. BlockPaletteData fills it in canonical order at startup.
	/// </summary>
	public class BlockPalette : List<BlockStateContainer>
	{
	}

	public class BlockStateContainer
	{
		public int Id { get; set; }
		public short Data { get; set; }
		public string Name { get; set; }
		public int RuntimeId { get; set; }
		public List<IBlockState> States { get; set; } = new List<IBlockState>();

		[JsonIgnore]
		public byte[] StatesCacheNbt { get; set; }
		public ItemPickInstance ItemInstance { get; set; }

		/// <summary>
		///     Name plus the set of states, without regard to the order they are listed in: two lists
		///     holding the same states are the same block, and a stored world writes them in whatever
		///     order the version that wrote it used.
		///     <para>
		///     Allocation-free on purpose. This runs for every palette entry of every section read off
		///     disk, and a state list is a handful of entries, so scanning beats building a set.
		///     </para>
		/// </summary>
		protected bool Equals(BlockStateContainer other)
		{
			if (Name != other.Name) return false;
			if (States.Count != other.States.Count) return false;

			for (int i = 0; i < States.Count; i++)
			{
				IBlockState state = States[i];
				bool found = false;
				for (int j = 0; j < other.States.Count; j++)
				{
					if (state.Equals(other.States[j]))
					{
						found = true;
						break;
					}
				}

				if (!found) return false;
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((BlockStateContainer) obj);
		}

		public override int GetHashCode()
		{
			// Mirrors Equals, which is why the states combine with XOR rather than in sequence: order
			// must not change the result. Hashing them in list order put two containers holding the
			// same states in different buckets, so whether a palette lookup found its block came down
			// to the order the world it came from happened to store them in.
			int hash = Name?.GetHashCode() ?? 0;

			foreach (IBlockState state in States)
			{
				hash ^= state.GetHashCode();
			}

			return hash;
		}

		public override string ToString()
		{
			return $"{nameof(Name)}: {Name}, {nameof(Id)}: {Id}, {nameof(Data)}: {Data}, {nameof(RuntimeId)}: {RuntimeId}, {nameof(States)} {{ {string.Join(';', States)} }}";
		}
	}

	public class ItemPickInstance
	{
		public short Id { get; set; } = -1;
		public short Metadata { get; set; } = -1;
		public bool WantNbt { get; set; } = false;
	}

	public interface IBlockState
	{
		public string Name { get; set; }
	}

	public class BlockStateInt : IBlockState
	{
		public int Type { get; } = 3;
		public string Name { get; set; }
		public int Value { get; set; }

		protected bool Equals(BlockStateInt other)
		{
			return Name == other.Name && Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((BlockStateInt) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(GetType().Name, Name, Value);
		}

		public override string ToString()
		{
			return $"{nameof(Name)}: {Name}, {nameof(Value)}: {Value}";
		}
	}

	public class BlockStateByte : IBlockState
	{
		public int Type { get; } = 1;
		public string Name { get; set; }
		public byte Value { get; set; }

		protected bool Equals(BlockStateByte other)
		{
			return Name == other.Name && Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((BlockStateByte) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(GetType().Name, Name, Value);
		}

		public override string ToString()
		{
			return $"{nameof(Name)}: {Name}, {nameof(Value)}: {Value}";
		}
	}

	public class BlockStateString : IBlockState
	{
		public int Type { get; } = 8;
		public string Name { get; set; }
		public string Value { get; set; }

		protected bool Equals(BlockStateString other)
		{
			return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((BlockStateString) obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(GetType().Name, Name, Value.ToLowerInvariant());
		}

		public override string ToString()
		{
			return $"{nameof(Name)}: {Name}, {nameof(Value)}: {Value}";
		}
	}
}