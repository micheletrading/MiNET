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
using System.Numerics;
using Newtonsoft.Json;

namespace MiNET.Utils.Skins
{
	public enum Face
	{
		None,
		Inside,
		Top,
		Bottom,
		Right,
		Front,
		Left,
		Back,
	}

	public class Cube : ICloneable
	{
		public float[] Origin { get; set; } = new float[3];
		public float[] Size { get; set; } = new float[3];
		public float[] Uv { get; set; } = new float[3];
		public float Inflate { get; set; }
		public bool Mirror { get; set; }

		/// <summary>
		///     Degrees, x then y then z. Null when the cube is unrotated, so it stays off the wire.
		///     With <see cref="Pivot" /> unset the cube turns about its own centre, which is what the
		///     geometry 1.12.0 schema specifies and what tumbling debris wants.
		/// </summary>
		public float[] Rotation { get; set; }

		/// <summary>Point the cube rotates around, in model space. Its own centre when null.</summary>
		public float[] Pivot { get; set; }

		[JsonIgnore] public Vector3 Velocity { get; set; } = Vector3.Zero;

		/// <summary>Degrees per frame, applied to <see cref="Rotation" />. Server-side only.</summary>
		[JsonIgnore] public Vector3 AngularVelocity { get; set; } = Vector3.Zero;

		[JsonIgnore] public Face Face { get; set; } = Face.None;

		public object Clone()
		{
			var cube = (Cube) MemberwiseClone();

			cube.Origin = (float[]) Origin?.Clone();
			cube.Size = (float[]) Size?.Clone();
			cube.Uv = (float[]) Uv?.Clone();
			cube.Rotation = (float[]) Rotation?.Clone();
			cube.Pivot = (float[]) Pivot?.Clone();

			return cube;
		}
	}
}