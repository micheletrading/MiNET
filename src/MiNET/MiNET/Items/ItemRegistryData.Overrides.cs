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
using fNbt;

namespace MiNET.Items
{
	/// <summary>
	///     Hand-written additions and corrections to the generated <see cref="ItemRegistryData" />.
	///     Cloudburst's item_components data has no entry for the potion, so the generated blob is
	///     empty and the client falls back to its ~0.3s default use time. Vanilla drinks in 1.6s,
	///     which is the minecraft:use_duration component (a float, in seconds) the item_registry
	///     packet carries.
	/// </summary>
	public static partial class ItemRegistryData
	{
		public static void CreateOverrides(ItemRegistry registry)
		{
			var components = new NbtCompound("components")
			{
				new NbtFloat("minecraft:use_duration", 1.6f),
				new NbtString("minecraft:use_animation", "drink")
			};
			var root = new NbtCompound("") {components};
			string blob = Convert.ToBase64String(new NbtFile(root) {BigEndian = false, UseVarInt = true}.SaveToBuffer(NbtCompression.None));

			registry.Replace("minecraft:potion", 430, false, 2, blob);
		}
	}
}
