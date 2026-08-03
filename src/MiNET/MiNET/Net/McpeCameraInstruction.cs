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

using System.Collections.Generic;
using System.Numerics;

namespace MiNET.Net
{
	/// <summary>
	///     How a camera move is interpolated.
	///
	///     Written two different ways depending on where it appears: the set instruction sends the
	///     ordinal as a byte, while the field-of-view and spline instructions send the name as a
	///     string. So the order of this enum is wire-significant, and it is not the order
	///     minecraft-data lists (which has the quad family at 2..4); this is the order the client
	///     uses, with sine first.
	/// </summary>
	public enum CameraEaseType : byte
	{
		Linear = 0,
		Spring = 1,
		InSine = 2,
		OutSine = 3,
		InOutSine = 4,
		InQuad = 5,
		OutQuad = 6,
		InOutQuad = 7,
		InCubic = 8,
		OutCubic = 9,
		InOutCubic = 10,
		InQuart = 11,
		OutQuart = 12,
		InOutQuart = 13,
		InQuint = 14,
		OutQuint = 15,
		InOutQuint = 16,
		InExpo = 17,
		OutExpo = 18,
		InOutExpo = 19,
		InCirc = 20,
		OutCirc = 21,
		InOutCirc = 22,
		InBack = 23,
		OutBack = 24,
		InOutBack = 25,
		InElastic = 26,
		OutElastic = 27,
		InOutElastic = 28,
		InBounce = 29,
		OutBounce = 30,
		InOutBounce = 31
	}

	public static class CameraEaseTypes
	{
		private static readonly string[] Names =
		{
			"linear", "spring",
			"in_sine", "out_sine", "in_out_sine",
			"in_quad", "out_quad", "in_out_quad",
			"in_cubic", "out_cubic", "in_out_cubic",
			"in_quart", "out_quart", "in_out_quart",
			"in_quint", "out_quint", "in_out_quint",
			"in_expo", "out_expo", "in_out_expo",
			"in_circ", "out_circ", "in_out_circ",
			"in_back", "out_back", "in_out_back",
			"in_elastic", "out_elastic", "in_out_elastic",
			"in_bounce", "out_bounce", "in_out_bounce"
		};

		public static string ToName(this CameraEaseType type)
		{
			var index = (int) type;
			return index >= 0 && index < Names.Length ? Names[index] : Names[0];
		}

		public static CameraEaseType FromName(string name)
		{
			int index = System.Array.IndexOf(Names, name);
			return index < 0 ? CameraEaseType.Linear : (CameraEaseType) index;
		}
	}

	public enum CameraSplineType : byte
	{
		CatmullRom = 0,
		Linear = 1
	}

	public class CameraEase
	{
		public CameraEaseType Type { get; set; }
		public float Duration { get; set; }
	}

	/// <summary>
	///     Puts the player on a preset. <see cref="RuntimeId" /> indexes the preset registry the
	///     client was sent (McpeCameraPresets), which is why the registry has to arrive first.
	/// </summary>
	public class CameraSetInstruction
	{
		public int RuntimeId { get; set; }
		public CameraEase Ease { get; set; }
		public Vector3? Position { get; set; }

		/// <summary>Pitch then yaw, in degrees.</summary>
		public Vector2? Rotation { get; set; }

		public Vector3? Facing { get; set; }
		public Vector2? Offset { get; set; }
		public Vector3? EntityOffset { get; set; }
		public bool? Default { get; set; }

		/// <summary>Drops the preset's own starting position and rotation, so only what is set here applies.</summary>
		public bool RemoveIgnoreStartingValues { get; set; }
	}

	public class CameraFadeTime
	{
		public float FadeIn { get; set; }
		public float Hold { get; set; }
		public float FadeOut { get; set; }
	}

	/// <summary>
	///     Both halves are independently optional, so a fade can change only the colour or only the
	///     timing and inherit the rest.
	/// </summary>
	public class CameraFadeInstruction
	{
		public CameraFadeTime Time { get; set; }
		public Vector3? ColorRgb { get; set; }
	}

	public class CameraTargetInstruction
	{
		public Vector3? Offset { get; set; }
		public long EntityUniqueId { get; set; }
	}

	public class CameraFovInstruction
	{
		public float FieldOfView { get; set; }
		public float EaseTime { get; set; }

		/// <summary>Sent as its name, not its ordinal, unlike the set instruction's ease.</summary>
		public CameraEaseType EaseType { get; set; }

		public bool Clear { get; set; }
	}

	public class CameraProgressKeyFrame
	{
		public float Value { get; set; }
		public float Time { get; set; }
		public CameraEaseType Ease { get; set; }
	}

	/// <summary>
	///     Where the camera looks at one point along a spline.
	///
	///     <see cref="Value" /> is euler degrees as pitch, yaw, roll, despite being a Vector3 where
	///     the set instruction uses a Vector2 for the same idea. It is not a look-at position and
	///     not a direction vector: a constant value holds one fixed heading for the whole path, and
	///     unit-length values produce roughly no rotation at all, both of which look like the field
	///     is being ignored.
	///
	///     Positive pitch looks UP, the opposite of the usual Minecraft convention. Yaw is
	///     atan2(dx, dz) in degrees plus 180 to face a target, and must be unwrapped across
	///     keyframes: atan2 wraps at 180, and the client interpolates straight through the
	///     discontinuity as a full extra revolution.
	/// </summary>
	public class CameraRotationKeyFrame
	{
		public Vector3 Value { get; set; }
		public float Time { get; set; }
		public CameraEaseType Ease { get; set; }
	}

	/// <summary>
	///     Flies the camera along a curve. Either the curve is given inline, or
	///     <see cref="SplineIdentifier" /> names one the client already holds and
	///     <see cref="LoadFromJson" /> is set.
	///
	///     The three lists are parallel: one curve point, one progress keyframe and one rotation
	///     keyframe per control point, which is the same model the Editor's camera tool authors.
	///     None of them may be empty. A curve with no progress or rotation keyframes hard-crashes
	///     the client the instant it arrives, with no packet violation and no disconnect reason.
	///
	///     Minimum control points are vanilla's: four for <see cref="CameraSplineType.CatmullRom" />,
	///     three for <see cref="CameraSplineType.Linear" />. The player must already be on the free
	///     preset; splines are documented as valid only there.
	/// </summary>
	public class CameraSplineInstruction
	{
		public float TotalTime { get; set; }
		public CameraSplineType Type { get; set; }
		public List<Vector3> Curve { get; set; } = new List<Vector3>();
		public List<CameraProgressKeyFrame> ProgressKeyFrames { get; set; } = new List<CameraProgressKeyFrame>();
		public List<CameraRotationKeyFrame> RotationOptions { get; set; } = new List<CameraRotationKeyFrame>();
		public string SplineIdentifier { get; set; } = string.Empty;
		public bool LoadFromJson { get; set; }
	}

	/// <summary>
	///     One packet, nine independent verbs. The server sets whichever apply and leaves the rest
	///     null; each is preceded by a presence bool on the wire, so an instruction that does
	///     nothing is nine zero bytes. This is what the /camera command compiles into.
	///
	///     Sources, in the order they were trusted. pmmp/BedrockProtocol is on CURRENT_PROTOCOL
	///     1001, the same protocol number we target, and CloudburstMC/Protocol agrees with it
	///     throughout; between them they are the authority here. Mojang's published schema is
	///     wrong on three counts (it has the set instruction's default flag as a bare bool, and
	///     both the field-of-view and spline ease types as numbers), and minecraft-data is wrong on
	///     the fade nesting and on the ease ordering. Everything below was then confirmed against a
	///     real 1.26 client.
	///
	///     Things that are not obvious and cost time to find:
	///
	///     The fade is two independently optional halves, a time struct and a colour struct, so a
	///     fade can change only the timing or only the colour. Writing it flat is two bytes short
	///     and the client reads the first float's leading byte as the time presence flag.
	///
	///     The ease type is written two different ways. The set instruction sends the ordinal as a
	///     byte; the field-of-view and spline instructions send the name as a string. The ordinal
	///     order is not the one minecraft-data lists: sine comes before quad.
	///
	///     Entity ids here are little-endian int64, unlike most of the protocol, hence WriteLe.
	///     PMMP's own comment on that field is "why be consistent mojang ?????".
	///
	///     The set instruction's preset is an index into the registry the client was last sent by
	///     McpeCameraPresets, not a name, so the registry has to arrive first and indices shift if
	///     it is resent with a different order.
	/// </summary>
	public partial class McpeCameraInstruction : Packet<McpeCameraInstruction>
	{
		public CameraSetInstruction Set { get; set; }
		public bool? Clear { get; set; }
		public CameraFadeInstruction Fade { get; set; }
		public CameraTargetInstruction Target { get; set; }
		public bool? RemoveTarget { get; set; }
		public CameraFovInstruction Fov { get; set; }
		public CameraSplineInstruction Spline { get; set; }
		public long? AttachToEntity { get; set; }
		public bool? DetachFromEntity { get; set; }

		partial void AfterDecode()
		{
			if (ReadBool())
			{
				var set = new CameraSetInstruction {RuntimeId = ReadInt()};
				if (ReadBool()) set.Ease = new CameraEase {Type = (CameraEaseType) ReadByte(), Duration = ReadFloat()};
				if (ReadBool()) set.Position = ReadVector3();
				if (ReadBool()) set.Rotation = ReadVector2();
				if (ReadBool()) set.Facing = ReadVector3();
				if (ReadBool()) set.Offset = ReadVector2();
				if (ReadBool()) set.EntityOffset = ReadVector3();
				if (ReadBool()) set.Default = ReadBool();
				set.RemoveIgnoreStartingValues = ReadBool();
				Set = set;
			}

			if (ReadBool()) Clear = ReadBool();

			if (ReadBool())
			{
				var fade = new CameraFadeInstruction();
				if (ReadBool()) fade.Time = new CameraFadeTime {FadeIn = ReadFloat(), Hold = ReadFloat(), FadeOut = ReadFloat()};
				if (ReadBool()) fade.ColorRgb = ReadVector3();
				Fade = fade;
			}

			if (ReadBool())
			{
				var target = new CameraTargetInstruction();
				if (ReadBool()) target.Offset = ReadVector3();
				target.EntityUniqueId = ReadLeLong();
				Target = target;
			}

			if (ReadBool()) RemoveTarget = ReadBool();

			if (ReadBool())
			{
				Fov = new CameraFovInstruction
				{
					FieldOfView = ReadFloat(),
					EaseTime = ReadFloat(),
					EaseType = CameraEaseTypes.FromName(ReadString()),
					Clear = ReadBool()
				};
			}

			if (ReadBool())
			{
				var spline = new CameraSplineInstruction {TotalTime = ReadFloat(), Type = (CameraSplineType) ReadByte()};

				uint curveCount = ReadUnsignedVarInt();
				for (int i = 0; i < curveCount; i++) spline.Curve.Add(ReadVector3());

				uint progressCount = ReadUnsignedVarInt();
				for (int i = 0; i < progressCount; i++)
				{
					spline.ProgressKeyFrames.Add(new CameraProgressKeyFrame
					{
						Value = ReadFloat(),
						Time = ReadFloat(),
						Ease = CameraEaseTypes.FromName(ReadString())
					});
				}

				uint rotationCount = ReadUnsignedVarInt();
				for (int i = 0; i < rotationCount; i++)
				{
					spline.RotationOptions.Add(new CameraRotationKeyFrame
					{
						Value = ReadVector3(),
						Time = ReadFloat(),
						Ease = CameraEaseTypes.FromName(ReadString())
					});
				}

				spline.SplineIdentifier = ReadString();
				spline.LoadFromJson = ReadBool();

				Spline = spline;
			}

			if (ReadBool()) AttachToEntity = ReadLeLong();
			if (ReadBool()) DetachFromEntity = ReadBool();
		}

		// Nine optionals, each a presence bool then its payload. Mirrors
		// CameraInstructionPacket::encodePayload in pmmp/BedrockProtocol, whose CommonTypes
		// writeOptional is exactly this bool-then-payload pair.
		partial void AfterEncode()
		{
			// 0: set
			Write(Set != null);
			if (Set != null)
			{
				Write(Set.RuntimeId);

				Write(Set.Ease != null);
				if (Set.Ease != null)
				{
					Write((byte) Set.Ease.Type);
					Write(Set.Ease.Duration);
				}

				Write(Set.Position.HasValue);
				if (Set.Position.HasValue) Write(Set.Position.Value);
				Write(Set.Rotation.HasValue);
				if (Set.Rotation.HasValue) Write(Set.Rotation.Value);
				Write(Set.Facing.HasValue);
				if (Set.Facing.HasValue) Write(Set.Facing.Value);
				Write(Set.Offset.HasValue);
				if (Set.Offset.HasValue) Write(Set.Offset.Value);
				Write(Set.EntityOffset.HasValue);
				if (Set.EntityOffset.HasValue) Write(Set.EntityOffset.Value);
				Write(Set.Default.HasValue);
				if (Set.Default.HasValue) Write(Set.Default.Value);

				Write(Set.RemoveIgnoreStartingValues);
			}

			// 1: clear
			Write(Clear.HasValue);
			if (Clear.HasValue) Write(Clear.Value);

			// 2: fade
			Write(Fade != null);
			if (Fade != null)
			{
				Write(Fade.Time != null);
				if (Fade.Time != null)
				{
					Write(Fade.Time.FadeIn);
					Write(Fade.Time.Hold);
					Write(Fade.Time.FadeOut);
				}

				Write(Fade.ColorRgb.HasValue);
				if (Fade.ColorRgb.HasValue) Write(Fade.ColorRgb.Value);
			}

			// 3: target
			Write(Target != null);
			if (Target != null)
			{
				Write(Target.Offset.HasValue);
				if (Target.Offset.HasValue) Write(Target.Offset.Value);
				WriteLe(Target.EntityUniqueId);
			}

			// 4: remove target
			Write(RemoveTarget.HasValue);
			if (RemoveTarget.HasValue) Write(RemoveTarget.Value);

			// 5: field of view
			Write(Fov != null);
			if (Fov != null)
			{
				Write(Fov.FieldOfView);
				Write(Fov.EaseTime);
				Write(Fov.EaseType.ToName());
				Write(Fov.Clear);
			}

			// 6: spline
			Write(Spline != null);
			if (Spline != null)
			{
				Write(Spline.TotalTime);
				Write((byte) Spline.Type);

				WriteUnsignedVarInt((uint) Spline.Curve.Count);
				foreach (Vector3 point in Spline.Curve) Write(point);

				WriteUnsignedVarInt((uint) Spline.ProgressKeyFrames.Count);
				foreach (CameraProgressKeyFrame frame in Spline.ProgressKeyFrames)
				{
					Write(frame.Value);
					Write(frame.Time);
					Write(frame.Ease.ToName());
				}

				WriteUnsignedVarInt((uint) Spline.RotationOptions.Count);
				foreach (CameraRotationKeyFrame frame in Spline.RotationOptions)
				{
					Write(frame.Value);
					Write(frame.Time);
					Write(frame.Ease.ToName());
				}

				Write(Spline.SplineIdentifier ?? string.Empty);
				Write(Spline.LoadFromJson);
			}

			// 7: attach to entity
			Write(AttachToEntity.HasValue);
			if (AttachToEntity.HasValue) WriteLe(AttachToEntity.Value);

			// 8: detach from entity
			Write(DetachFromEntity.HasValue);
			if (DetachFromEntity.HasValue) Write(DetachFromEntity.Value);
		}
	}
}
