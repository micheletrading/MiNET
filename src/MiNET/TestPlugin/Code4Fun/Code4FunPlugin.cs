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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using MiNET;
using MiNET.Entities;
using MiNET.Net;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.Skins;
using MiNET.Utils.Vectors;

namespace TestPlugin.Code4Fun
{
	[Plugin(PluginName = "Code4Fun", Description = "Plugin with mostly fun stuff", PluginVersion = "1.0", Author = "MiNET Team")]
	public class Code4FunPlugin : Plugin
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(Code4FunPlugin));

		public const double CubeFilterFactor = 1.3;
		public const float ZTearFactor = 0.01f;
		public static int FakeIndex = 0;

		protected override void OnEnable()
		{
			Context.PluginManager.LoadCommands(new ScreenshotCommand());
			Context.PluginManager.LoadCommands(new VideoCommand());
		}

		[Command]
		public void Melt(Player player)
		{
			string pluginDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

			var skin = player.Skin;
			if (string.IsNullOrEmpty(skin.GeometryData))
			{
				string skinString = File.ReadAllText(Path.Combine(pluginDirectory, "geometry.json"));
				skin.GeometryData = skinString;
			}
			else
			{
				string fileName = $"{Path.GetTempPath()}Skin_{player.Username}_{skin.GeometryName}.txt";
				Log.Info($"Writing geometry to filename: {fileName}");
				File.WriteAllText(fileName, skin.GeometryData);
			}

			//GravityGeometryBehavior state = new GravityGeometryBehavior
			//{
			//	Uuid = player.ClientUuid,
			//	Level = player.Level,
			//	Skin = player.Skin,
			//	CurrentModel = Skin.Parse(skin.SkinGeometry),
			//	Position = player.KnownPosition,
			//	ResetOnEnd = true
			//};

			//var geometryTimer = new Timer(MeltTick, state, 0, 50);
			//state.Timer = geometryTimer;
		}

		/// <summary>
		///     The control for SpawnFake: a PlayerMob wearing the caller's own skin, unaltered, so
		///     the only thing under test is whether a PlayerMob spawns at all. The client already
		///     accepted this exact skin for the caller, so a drop here is the spawn sequence and a
		///     drop only in SpawnFake is the custom geometry.
		/// </summary>
		[Command]
		public void SpawnCopy(Player player)
		{
			PlayerLocation coordinates = player.KnownPosition;
			Vector3 direction = Vector3.Normalize(player.KnownPosition.GetHeadDirection()) * 1.5f;

			// Baseline is the identity MiNET.Client logs in with, which a real 1.26.40 client is
			// known to accept. One piece is swapped at a time from there; right now that piece is
			// the skin, taken from the caller. A clone, so the mob cannot mutate the live player's
			// skin, but otherwise byte-for-byte theirs, ids included.
			string name = $"{player.Username} (copy)";
			ClientData clientData = CryptoUtils.BuildBotClientData(name);
			var skin = (Skin) player.Skin.Clone();

			var copy = new PlayerMob(name, player.Level)
			{
				KnownPosition = new PlayerLocation(coordinates.X + direction.X, coordinates.Y, coordinates.Z + direction.Z, 0, 0),
				Skin = skin,
				PlayerInfo = new PlayerInfo
				{
					DeviceOS = clientData.DeviceOS,
					DeviceId = clientData.DeviceId,
					PlatformChatId = string.Empty,
				},
			};

			copy.SpawnEntity();
			Log.Warn($"Spawned skin copy of {player.Username}, despawning in {CopyLifetimeSeconds}s");

			Task.Delay(TimeSpan.FromSeconds(CopyLifetimeSeconds)).ContinueWith(_ =>
			{
				try
				{
					copy.DespawnEntity();
					Log.Warn("Despawned skin copy");
				}
				catch (Exception e)
				{
					Log.Error("Failed to despawn skin copy", e);
				}
			});
		}

		private const int CopyLifetimeSeconds = 20;

		/// <summary>Blows the stand-in apart: every cube takes an outward impulse and falls.</summary>
		[Command]
		public void SpawnExplode(Player player, string name)
		{
			SpawnExplode(player, name, GravityGeometryBehavior.ExplodeTicksPerFrame);
		}

		/// <summary>As above, with the frame interval in ticks. One tick is 50ms.</summary>
		[Command]
		public void SpawnExplode(Player player, string name, int ticksPerFrame)
		{
			PlayerMob fake = SpawnStandIn(player, name, out GeometryModel model);
			var state = new GravityGeometryBehavior(fake, model, GeometryEffect.Explode) {TicksPerFrame = ticksPerFrame};
			fake.Ticking += state.FakeMeltTicking;
			Log.Warn($"Explode at {ticksPerFrame} ticks per frame ({1000.0 / (ticksPerFrame * 50):F1} fps)");
		}

		/// <summary>Melts the stand-in: no impulse, just a slow uneven sag into a pile.</summary>
		[Command]
		public void SpawnMelt(Player player, string name)
		{
			SpawnMelt(player, name, GravityGeometryBehavior.MeltTicksPerFrame);
		}

		/// <summary>As above, with the frame interval in ticks. One tick is 50ms.</summary>
		[Command]
		public void SpawnMelt(Player player, string name, int ticksPerFrame)
		{
			PlayerMob fake = SpawnStandIn(player, name, out GeometryModel model);
			var state = new GravityGeometryBehavior(fake, model, GeometryEffect.Melt) {TicksPerFrame = ticksPerFrame};
			fake.Ticking += state.FakeMeltTicking;
			Log.Warn($"Melt at {ticksPerFrame} ticks per frame ({1000.0 / (ticksPerFrame * 50):F1} fps)");
		}

		/// <summary>Strikes the stand-in with lightning, flashing an arms-out pose on each hit.</summary>
		[Command]
		public void SpawnStrike(Player player, string name)
		{
			PlayerMob fake = SpawnStandIn(player, name, out GeometryModel model);
			var state = new LightningStrikeBehavior(fake, model) {Caller = player.KnownPosition.ToVector3()};
			fake.Ticking += state.StrikeTicking;
		}

		/// <summary>
		///     The stand-in every effect runs on: a PlayerMob wearing the bot's skin with the
		///     plugin's texture, spawned in front of the caller and despawned on a timer. The skin
		///     is what a real 1.26.40 client is known to accept, so an effect that drops the client
		///     is the effect's doing and not the spawn's.
		/// </summary>
		private PlayerMob SpawnStandIn(Player player, string name, out GeometryModel model)
		{
			PlayerLocation coordinates = player.KnownPosition;
			Vector3 direction = Vector3.Normalize(player.KnownPosition.GetHeadDirection()) * 1.5f;

			ClientData clientData = CryptoUtils.BuildBotClientData(name);
			Skin skin = clientData.ToSkin();

			string pluginDirectory = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
			skin.Data = Skin.GetTextureFromFile(Path.Combine(pluginDirectory, "IMG_0220.png"));


			// The effects look their model up by GeometryName, which a skin built from login data
			// does not carry; it is the same identifier the resource patch names.
			skin.GeometryName = skin.SkinResourcePatch.Geometry.Default;
			model = Skin.Parse(skin.GeometryData);

			var fake = new PlayerMob(name, player.Level)
			{
				KnownPosition = new PlayerLocation(coordinates.X + direction.X, coordinates.Y, coordinates.Z + direction.Z, 0, 0),
				Skin = skin,
				PlayerInfo = new PlayerInfo
				{
					DeviceOS = clientData.DeviceOS,
					DeviceId = clientData.DeviceId,
					PlatformChatId = string.Empty,
				},
			};

			fake.SpawnEntity();
			Log.Warn($"Spawned stand-in {name}, despawning in {CopyLifetimeSeconds}s");

			Task.Delay(TimeSpan.FromSeconds(CopyLifetimeSeconds)).ContinueWith(_ =>
			{
				try
				{
					// Backstop only: an effect that finishes despawns itself, and one that never
					// finishes still has to be cleaned up.
					if (!fake.IsSpawned) return;

					fake.DespawnEntity();
					Log.Warn("Despawned stand-in");
				}
				catch (Exception e)
				{
					Log.Error("Failed to despawn stand-in", e);
				}
			});

			return fake;
		}

		public enum GeometryEffect
		{
			/// <summary>Radial impulse from inside the figure, then gravity. A detonation.</summary>
			Explode,

			/// <summary>A tiny random downward drift per cube under gentle gravity, so it sags unevenly.</summary>
			Melt,
		}

		/// <summary>
		///     The cartoon lightning strike. No physics and no subdivision: the model is left whole
		///     and the effect is a scripted sequence of bolts, each one throwing the figure into an
		///     arms-out pose for a couple of ticks before it drops back. The pose is a geometry flag,
		///     so switching it means re-sending the skin under a fresh identifier, the same as the
		///     other effects.
		/// </summary>
		public class LightningStrikeBehavior
		{
			private static readonly ILog Log = LogManager.GetLogger(typeof(LightningStrikeBehavior));

			/// <summary>
			///     Ticks at which a bolt lands, from the original effect. The pose goes on with the
			///     bolt and comes off two ticks later, which is what gives the flicker.
			/// </summary>
			private static readonly int[] Flashes = {20, 40, 64, 80, 94, 110, 124, 140, 160, 192, 232, 256, 296, 344};

			/// <summary>Ticks the skeleton stays up after a bolt. Scaled with the schedule above.</summary>
			private const int PoseHoldTicks = 8;

			public PlayerMob Mob { get; }
			public GeometryModel CurrentModel { get; }

			private readonly GeometryModel _skeletonModel;

			// The humanoid in each document, held by reference. Looking it up by name would fail
			// once the identifier starts moving, and taking the first entry picks geometry.cape:
			// a single 10x16x1 box, which renders the player as a flat slab.
			private readonly Geometry _bodyGeometry;
			private readonly Geometry _skeletonGeometry;

			private int _tick;
			private int _bolts;
			private bool _posed;

			/// <summary>Diagnostic: where the caller stood when the effect started.</summary>
			public Vector3 Caller { get; set; }

			public LightningStrikeBehavior(PlayerMob mob, GeometryModel currentModel)
			{
				Mob = mob;
				CurrentModel = currentModel;
				_bodyGeometry = currentModel.FindGeometry(mob.Skin.GeometryName);

				// The x-ray core the bolt reveals. Subdivide with renderSkin off and renderSkeleton
				// on keeps only the interior cubes of each box, which is what the old effect showed
				// between flashes.
				_skeletonModel = Skin.Parse(Skin.ToJson(currentModel));
				_skeletonGeometry = _skeletonModel.CollapseToDerived(_skeletonModel.FindGeometry(mob.Skin.GeometryName));
				_skeletonGeometry.Subdivide(false, true, false, true);
				_skeletonModel.Geometry.Clear();
				_skeletonModel.Geometry.Add(_skeletonGeometry);
			}

			public void StrikeTicking(object sender, PlayerEventArgs playerEventArgs)
			{
				var mob = (PlayerMob) sender;
				if (CurrentModel == null) return;

				try
				{
					int tick = _tick++;

					if (tick > Flashes[Flashes.Length - 1] + PoseHoldTicks)
					{
						mob.Ticking -= StrikeTicking;
						Log.Warn("Strike sequence done. De-register tick.");
						return;
					}

					bool strike = Array.IndexOf(Flashes, tick) >= 0;
					bool release = Array.IndexOf(Flashes, tick - PoseHoldTicks) >= 0;

					if (strike) mob.Level.StrikeLightning(mob.KnownPosition.ToVector3());
					if (!strike && !release) return;

					SendPose(mob, strike);
				}
				catch (Exception e)
				{
					mob.Ticking -= StrikeTicking;
					Log.Error("Strike failed", e);
				}
			}

			private void SendPose(PlayerMob mob, bool skeleton)
			{
				if (_posed == skeleton) return;
				_posed = skeleton;

				Skin skin = mob.Skin;
				GeometryModel model = skeleton ? _skeletonModel : CurrentModel;
				Geometry geometry = skeleton ? _skeletonGeometry : _bodyGeometry;
				if (geometry?.Description == null) return;

				geometry.Description.Identifier = $"geometry.{DateTime.UtcNow.Ticks}.{mob.ClientUuid}";

				skin.SkinResourcePatch = new SkinResourcePatch {Geometry = new GeometryIdentifier {Default = geometry.Description.Identifier}};
				skin.GeometryName = geometry.Description.Identifier;
				skin.GeometryData = Skin.ToJson(model);

				string previousSkinId = skin.SkinId;
				skin.SkinId = $"{geometry.Description.Identifier}.Strike";
				skin.FullSkinId = skin.SkinId;

				var updateSkin = McpePlayerSkin.CreateObject();
				updateSkin.uuid = mob.ClientUuid;
				updateSkin.oldSkinName = previousSkinId;
				updateSkin.skinName = skin.SkinId;
				updateSkin.skin = skin;

				// Queued as an ordinary packet, not pre-wrapped: the bolt for this same flash is
				// already in the pending batch, and PrepareSend would emit a pre-encoded wrapper as
				// a separate payload, so the two would be applied by the client as two independent
				// updates. Sharing the batch makes the bolt and the pose one thing. The melt wants
				// the opposite, which is why it wraps each frame itself.
				mob.Level.RelayBroadcast(updateSkin);
			}
		}

		public class GravityGeometryBehavior
		{
			private static readonly ILog Log = LogManager.GetLogger(typeof(GravityGeometryBehavior));

			// Every constant below is per TICK, and each frame advances by TicksPerFrame of them.
			// They used to be per frame, which made the whole animation frame-rate dependent: the
			// same effect fell faster and landed sooner the more often it was sent.
			//
			// Cube coordinates are in model units, a sixteenth of a block, and a block is a metre,
			// so these two factors convert real units into the ones the geometry is written in.
			private const float TicksPerSecond = 20f;
			private const float ModelUnitsPerBlock = 16f;

			private static float MetresPerSecondSquared(float value) => value / (TicksPerSecond * TicksPerSecond) * ModelUnitsPerBlock;
			private static float MetresPerSecond(float value) => value / TicksPerSecond * ModelUnitsPerBlock;

			/// <summary>
			///     Earth. The blast used to run at 0.067 model units per tick squared, which works
			///     out at 1.67 m/s2 - lunar, near enough exactly - and that is why the debris floated.
			///     Vanilla Minecraft falls at roughly 0.08 blocks/tick2, about 32 m/s2, so this is
			///     lighter than the game but true to life.
			/// </summary>
			public static readonly float ExplodeGravity = MetresPerSecondSquared(9.81f);

			// Per frame, not per tick: the physics only advances on a tick that also sends, so no
			// state is ever computed and then skipped. This is the value the effect shipped with.
			public const float MeltGravity = 0.01f / 4f;

			/// <summary>Fraction of speed lost per tick. Compounded over the frame, not applied once.</summary>
			public const float Drag = 0.02f / 3f;

			/// <summary>
			///     Outward impulse on each cube. Was 5, which threw the pieces far enough that the
			///     figure read as scattering rather than bursting.
			/// </summary>
			/// <summary>
			///     Outward speed given to each cube. Under Earth gravity 2.7 m/s lifts a piece about
			///     37cm and lands the debris around a block out, which reads as a burst rather than
			///     a scatter. Halving the speed quarters the height, so this is a big cut.
			/// </summary>
			public static readonly float ExplodeForce = MetresPerSecond(2.7f);

			/// <summary>Degrees per tick, per axis, at the top of the random range.</summary>
			public const float MaxSpinPerTick = 18f / 3f;

			/// <summary>
			///     Ticks between frames. Physics and send are locked together: a tick that does not
			///     send does not advance anything either, so every state the animation passes
			///     through is transmitted, and the frame rate therefore also sets the speed. A blast
			///     wants to be over quickly, so it runs every tick; a melt is meant to ooze.
			/// </summary>
			// 100ms is what the original effects ran their timer at. One tick per frame is 20fps,
			// and the client showed nothing at all at that rate, so the ceiling is the client
			// rather than our frame cost. Settable per spawn so the rate can be swept without a
			// rebuild.
			public const int ExplodeTicksPerFrame = 2;

			public const int MeltTicksPerFrame = 4;

			public int TicksPerFrame { get; set; }

			/// <summary>Blast debris is dropped the moment it lands; melt debris stays and pools.</summary>
			private bool RemoveOnLanding => Effect != GeometryEffect.Melt;

			/// <summary>
			///     Only the blast eats its own cubes. A melt's ending is the puddle, so it keeps
			///     every piece: winking them out one by one undid the thing the effect is for.
			/// </summary>
			private bool Dissolves => Effect != GeometryEffect.Melt;

			/// <summary>
			///     Ticks to wait after the spawn before the first frame. The spawn and the first
			///     geometry swap used to be one tick apart, so the animation could start while the
			///     client was still resolving the entity and its skin.
			/// </summary>
			public const int StartDelayTicks = 20;

			/// <summary>
			///     Ticks over which the debris fades to nothing, measured from the moment it starts
			///     moving. The blast needs this because its cubes are all airborne until they land
			///     together, so the fraction that has settled stays at zero and then jumps: nothing
			///     to fade against. The melt settles progressively and fades on that instead,
			///     whichever reaches zero first.
			/// </summary>
			public const int ExplodeFadeTicks = 24;

			// The melt is not meant to vanish, it is meant to run down and pool, so its dissolve
			// only clears the puddle long after the shape has gone.
			public const int MeltFadeTicks = 400;

			/// <summary>
			///     Ticks between the first cube letting go and the last. Wax runs off the outside
			///     and from the top first, so a cube's delay comes from how high and how far out it
			///     sits: the head and the surface go early, the core and the feet last. Without this
			///     every cube released together and the whole figure sank as one piece.
			/// </summary>
			public const int MeltReleaseTicks = 50;

			/// <summary>How much of the delay is decided by height rather than by distance from the axis.</summary>
			public const float MeltHeightBias = 0.6f;

			/// <summary>
			///     Fraction of the remaining distance a pooling cube covers each tick. It eases into
			///     place rather than sliding on an impulse: an impulse has no idea where the pool
			///     should end, so it either stopped under the figure or flung the cubes into spokes.
			/// </summary>
			public const float MeltFlowPerTick = 0.025f;

			/// <summary>
			///     137.507 degrees in radians. Stepping the angle by this puts each piece in the
			///     largest remaining gap, which is why sunflowers use it and why it packs a disc more
			///     evenly than any random draw.
			/// </summary>
			private const double GoldenAngle = 2.39996322972865332;

			/// <summary>
			///     How far the pool's edge wanders from a true circle, as a fraction of its radius.
			///     Nothing melts into a perfect disc: a few low harmonics around the circle give it
			///     lobes and hollows while keeping the outline smooth.
			/// </summary>
			public const float MeltLobeDepth = 0.22f;

			/// <summary>
			///     What a cube flattens to when it pools, at the middle of the puddle and at its rim.
			///     Stamped rather than uniform: a piece landing in the middle keeps most of its
			///     height while one at the edge is pressed flat, so the pool ends up domed the way
			///     clay would be instead of an even sheet.
			/// </summary>
			public const float MeltCentreThickness = 0.85f;

			public const float MeltRimThickness = 0.4f;

			/// <summary>
			///     Width that keeps a cube's volume at a given height, rather than a separate
			///     constant. Flattening to a third and widening by 1.6 was losing a quarter of the
			///     material on contact: width squared times height has to come to one.
			/// </summary>
			private static float SpreadFor(float height) => (float) (1 / Math.Sqrt(height));

			/// <summary>
			///     Model height a MeltSpreadSpeed of one is calibrated against, in model units: 24 is
			///     a standard humanoid from foot to shoulder. A figure twice that tall spreads twice
			///     as fast and so covers proportionally more ground, instead of every model pooling
			///     into the same footprint.
			/// </summary>
			public const float ReferenceModelHeight = 24f;



			/// <summary>
			///     Bends the dissolve so it bites early instead of ramping evenly. Below 1 it is
			///     front-loaded: at a quarter of the way through, a square root is already half
			///     dissolved. The blast is over in half a second, so an even ramp had barely
			///     started before the debris landed.
			/// </summary>
			public const float DissolveCurve = 0.5f;

			private int FadeTicks => Effect == GeometryEffect.Melt ? MeltFadeTicks : ExplodeFadeTicks;

			private int _tick;
			private int _spawnDelay;

			/// <summary>The untouched texture, and a stable per-texel threshold that decides the order
			/// texels wink out in. Stable so the dissolve eats the same texels away rather than
			/// flickering a fresh random set every frame.</summary>
			private readonly Dictionary<Cube, float> _dissolveAt = new Dictionary<Cube, float>();

			/// <summary>Tick at which each cube starts to move. Melt only; the blast releases everything at once.</summary>
			private readonly Dictionary<Cube, float> _releaseAt = new Dictionary<Cube, float>();

			/// <summary>
			///     A cube that has landed and is still spreading: the size it landed at, where on the
			///     pool it is heading, and how far through it is. It arrives as a box and flattens
			///     from there rather than snapping flat on contact.
			/// </summary>
			private readonly Dictionary<Cube, (Vector3 Size, Vector3 Target, float Progress)> _spreading =
				new Dictionary<Cube, (Vector3, Vector3, float)>();

			/// <summary>Radius the pool covers: the model's whole volume laid out at squashed thickness.</summary>
			private float _poolRadius = 1f;

			/// <summary>Cubes in the model, so a landing piece knows how far through the filling it is.</summary>
			private int _meltCubeCount;

			/// <summary>Set once nothing is still falling, which freezes the spread where it stands.</summary>
			private bool _allLanded;

			/// <summary>Height of what is left of the model, in blocks, published as the collision box.</summary>
			private float _modelHeight;

			private bool _publishedMetadata;

			/// <summary>Harmonics that bend the pool's outline: how many lobes, how deep, and where they sit.</summary>
			private (int Lobes, float Depth, float Phase)[] _outline = Array.Empty<(int, float, float)>();

			private readonly Random _random = new Random();

			/// <summary>The model's height against a standard humanoid. Scales how far the pool spreads.</summary>
			private float _modelScale = 1f;


			public PlayerMob Mob { get; }
			public GeometryModel CurrentModel { get; private set; }
			public bool ResetOnEnd { get; set; }
			public GeometryEffect Effect { get; }

			private float Gravity => Effect == GeometryEffect.Melt ? MeltGravity : ExplodeGravity;

			public GravityGeometryBehavior(PlayerMob mob, GeometryModel currentModel, GeometryEffect effect = GeometryEffect.Explode)
			{
				Mob = mob;
				CurrentModel = currentModel;
				Effect = effect;

				TicksPerFrame = effect == GeometryEffect.Melt ? MeltTicksPerFrame : ExplodeTicksPerFrame;

				// The blast centre sits inside the mob, roughly chest height. It used to be the
				// world constant (0, 4, 10), so anywhere but the old demo's spawn every cube was
				// hundreds of blocks away, the 1/distance falloff fell under the 0.1 cutoff, and
				// they all kept zero velocity: the model came apart and then just stood there.
				_origin = mob.KnownPosition.ToVector3() + new Vector3(0, 1, 0);

				var geometry = CurrentModel.CollapseToDerived(CurrentModel.FindGeometry(mob.Skin.GeometryName));
				geometry.Subdivide(true, false);

				SetVelocity(geometry, new Random());

				// Each cube gets its own moment to vanish. Dissolving the texture instead was
				// limited by the atlas: 64x64 is 4096 texels shared between some 19,000 faces, so
				// clearing one texel took out every face that sampled it and the model came apart
				// in slabs. One threshold per cube is genuinely per-piece.
				var dissolveRandom = new Random();
				List<Cube> cubes = geometry.Bones.SelectMany(b => b.Cubes ?? new List<Cube>()).ToList();
				foreach (Cube cube in cubes)
				{
					_dissolveAt[cube] = (float) dissolveRandom.NextDouble();
				}

				if (Effect == GeometryEffect.Melt)
				{
					// Wax runs off the top and the outside first. A cube's delay comes from how high
					// it sits and how far it is from the model's axis, so the head and the surface
					// let go early while the core and the feet hold on. Everything releasing at once
					// is what made this read as the figure sinking rather than melting.
					float tallest = cubes.Max(c => c.Origin[1]);
					float widest = cubes.Max(Radius);
					_modelScale = tallest <= 0 ? 1f : tallest / ReferenceModelHeight;

					// Every cube keeps its volume and just gets thinner, so the ground it all has to
					// cover is the model's whole volume divided by that thickness. The pool's radius
					// follows from the material rather than from a number I picked, which is what
					// makes a bigger figure leave a bigger pool.
					float volume = cubes.Sum(c => c.Size[0] * c.Size[1] * c.Size[2]);
					float thickness = cubes.Average(c => c.Size[1]) * (MeltCentreThickness + MeltRimThickness) / 2;
					_poolRadius = (float) Math.Sqrt(volume / (Math.PI * Math.Max(thickness, 0.01f)));
					_meltCubeCount = cubes.Count;

					// Two or three slow waves around the circle, at random phases and split depths.
					// Low harmonics keep the edge smooth: it bulges and pinches rather than turning
					// ragged, which is what a puddle does.
					_outline = new[]
					{
						(2, MeltLobeDepth * 0.5f, (float) (_random.NextDouble() * Math.PI * 2)),
						(3, MeltLobeDepth * 0.3f, (float) (_random.NextDouble() * Math.PI * 2)),
						(5, MeltLobeDepth * 0.2f, (float) (_random.NextDouble() * Math.PI * 2)),
					};

				}

				CurrentModel.Geometry.Clear();
				CurrentModel.Geometry.Add(geometry);
			}

			private void SetVelocity(GeometryModel model, Random random)
			{
				foreach (var geometry in model.Geometry)
				{
					SetVelocity(geometry, random);
				}
			}


			private void SetVelocity(Geometry geometry, Random random1)
			{
				Random random = new Random();

				foreach (var bone in geometry.Bones)
				{
					SetVelocity(bone, random);
				}
			}

			private void SetVelocity(Bone bone, Random random)
			{
				if (bone.NeverRender) return;
				if (bone.Cubes == null || bone.Cubes.Count == 0) return;

				foreach (var cube in bone.Cubes)
				{
					SetVelocity(cube, random);
				}
			}

			private readonly Vector3 _origin;

			/// <summary>
			///     Builds and sends one frame. The debris fades by shrinking, not by going
			///     transparent: the client applies a texture sent in an McpePlayerSkin update (a
			///     probe painting it red proved that) but draws it opaque whatever the alpha
			///     channel says, so scaling the cubes toward nothing is what is left.
			/// </summary>
			private void SendFrame(PlayerMob mob, float alpha, long physicsMs = 0)
			{
				var buildTimer = Stopwatch.StartNew();

				Skin skin = mob.Skin;
				Geometry geometry = CurrentModel.FindGeometry(skin.GeometryName);
				geometry.Description.Identifier = $"geometry.{DateTime.UtcNow.Ticks}.{mob.ClientUuid}";
				skin.SkinResourcePatch = new SkinResourcePatch {Geometry = new GeometryIdentifier {Default = geometry.Description.Identifier}};

				CurrentModel.Geometry.Clear();
				CurrentModel.Geometry.Add(geometry);

				skin.GeometryName = geometry.Description.Identifier;
				skin.GeometryData = Skin.ToJson(CurrentModel);



				// The skin id has to move as well, not just the geometry identifier. The client
				// caches the whole skin by id and will not re-read a document for an id it already
				// knows, so every frame went out under the same id and it kept drawing the first one.
				string previousSkinId = skin.SkinId;
				skin.SkinId = $"{geometry.Description.Identifier}.Melt";
				skin.FullSkinId = skin.SkinId;

				var updateSkin = McpePlayerSkin.CreateObject();
				updateSkin.uuid = mob.ClientUuid;
				updateSkin.oldSkinName = previousSkinId;
				updateSkin.skinName = skin.SkinId;
				updateSkin.skin = skin;

				// Its own wrapper, the way chunks and player lists are sent. PrepareSend packs
				// everything queued between flushes into a single McpeWrapper, so two frames could
				// arrive in one payload and the client would only draw the second.
				byte[] encoded = updateSkin.Encode();
				mob.Level.RelayBroadcast(MiNET.Worlds.Level.CreateMcpeBatch(encoded));
				updateSkin.PutPool();

				if (_modelHeight > 0 && (!_publishedMetadata || Math.Abs(mob.Height - _modelHeight) > 0.01f))
				{
					mob.Height = _modelHeight;
					_publishedMetadata = true;

					var resize = McpeSetEntityData.CreateObject();
					resize.runtimeEntityId = mob.EntityId;
					resize.metadata = mob.GetMetadata();
					mob.Level.RelayBroadcast(resize);
				}

				Log.Warn($"{Effect} frame {_tick / TicksPerFrame}: physics {physicsMs}ms, build {buildTimer.ElapsedMilliseconds}ms, "
					+ $"{skin.GeometryData.Length} chars, {encoded.Length} bytes, alpha {alpha:F2}, height {mob.Height:F2}");
			}

			/// <summary>
			///     How far the pool reaches in a given direction. The harmonics are summed rather
			///     than multiplied so the outline stays smooth and the average radius is unchanged,
			///     which keeps the area - and so the thickness - about right.
			/// </summary>
			private float RadiusAt(double angle)
			{
				float wobble = 0f;
				foreach ((int lobes, float depth, float phase) in _outline)
				{
					wobble += depth * (float) Math.Sin(lobes * angle + phase);
				}

				return _poolRadius * (1f + wobble);
			}

			/// <summary>Distance from the model's vertical axis, in model units.</summary>
			private static float Radius(Cube cube)
			{
				return (float) Math.Sqrt(cube.Origin[0] * cube.Origin[0] + cube.Origin[2] * cube.Origin[2]);
			}

			private static float Wrap(float degrees)
			{
				degrees %= 360f;
				return degrees < 0 ? degrees + 360f : degrees;
			}

			private void SetVelocity(Cube cube, Random random)
			{
				if (Effect == GeometryEffect.Melt)
				{
					// What the melt originally seeded: a tiny random downward drift per cube. Every
					// cube starts at a slightly different rate, so the figure sags unevenly rather
					// than dropping as one piece. Gentle gravity does the rest.
					cube.Velocity = new Vector3(0, (float) (random.NextDouble() * -0.01 / 4), 0);
					return;
				}

				var pos = new Vector3(cube.Origin[0] / 16f, cube.Origin[1] / 16f, cube.Origin[2] / 16f) + Mob.KnownPosition;
				var dir = pos - _origin;
				float distance = dir.Length();

				distance = Math.Max(1, distance);
				distance = distance / (distance * distance);
				if (distance < 0.1) return;

				Vector3 force = new Vector3(distance, distance, distance) * ExplodeForce;
				cube.Velocity = Vector3.Reflect(dir.Normalize() * force, Vector3.UnitZ);

				// Debris tumbles. Rotation is a schema field on the cube and, with no pivot set,
				// turns it about its own centre; the rate is per frame, so a piece makes roughly
				// one turn a second at the blast's frame rate.
				cube.Rotation = new float[3];
				cube.AngularVelocity = new Vector3(
					(float) (random.NextDouble() - 0.5) * 2 * MaxSpinPerTick,
					(float) (random.NextDouble() - 0.5) * 2 * MaxSpinPerTick,
					(float) (random.NextDouble() - 0.5) * 2 * MaxSpinPerTick);
			}

			public void FakeMeltTicking(object sender, PlayerEventArgs playerEventArgs)
			{
				var mob = (PlayerMob) sender;

				//Log.Warn("Done. De-register tick.");
				//mob.Ticking -= FakeMeltTicking;
				//return;

				if (CurrentModel == null)
				{
					Log.Warn($"No current model set for mob.");
					return;
				}

				// Let the client finish the spawn before anything animates. Physics is held too, so
				// the figure stands still rather than starting mid-flight.
				if (_spawnDelay++ < StartDelayTicks) return;

				// One frame per TicksPerFrame, physics included. Advancing on ticks that do not
				// send would compute states the client never sees.
				if (++_tick % TicksPerFrame != 0) return;

				try
				{
					// This runs on the level tick thread, so anything slow here delays the whole
					// world tick, not just the animation. Physics and packet build are timed
					// separately: the first is arithmetic over a few thousand cubes, the second
					// serialises a ~215KB geometry document and compresses it, every frame.
					var physicsTimer = Stopwatch.StartNew();

					// Ticks advanced by this frame. Every constant is per tick, so the animation
					// covers the same ground in the same wall-clock time whatever the frame rate;
					// changing the rate changes only how smooth it looks. Drag is a fraction lost
					// per tick, so it compounds over the frame rather than being applied once.
					float dt = TicksPerFrame;
					float drag = (float) Math.Pow(1 - Drag, dt);

					// How far through the dissolve we are, front-loaded by the curve so it bites
					// immediately rather than building. Time-based, because a blast's cubes are all
					// airborne until they land together, so a settled count stays at zero.
					float dissolved = (float) Math.Pow(Math.Min(1f, _tick / (float) FadeTicks), DissolveCurve);

					bool stillMoving = false;
					int settled = 0;
					int total = 0;
					int falling = 0;
					float tallest = 0f;
					foreach (Geometry geometry in CurrentModel.Geometry)
					{
						foreach (Bone bone in geometry.Bones)
						{
							if (bone.NeverRender) continue;
							if (bone.Cubes == null || bone.Cubes.Count == 0) continue;

							// Backwards, because a cube whose moment has come is removed here.
							for (int i = bone.Cubes.Count - 1; i >= 0; i--)
							{
								Cube cube = bone.Cubes[i];

								if (Dissolves && _dissolveAt.TryGetValue(cube, out float dissolveAt) && dissolveAt < dissolved)
								{
									// Gone. Dropping it from the model also shortens the document, so
									// the payload falls away as the animation finishes.
									bone.Cubes.RemoveAt(i);
									_dissolveAt.Remove(cube);
									continue;
								}

								total++;
								tallest = Math.Max(tallest, cube.Origin[1] + cube.Size[1]);

								// Not its turn yet: it stands still rather than starting to sag.
								if (_releaseAt.TryGetValue(cube, out float releaseAt) && _tick < releaseAt)
								{
									stillMoving = true;
									continue;
								}

								if (cube.Origin[1] <= 0.05f && cube.Velocity.Y <= 0.01)
								{
									// A blast piece has finished its story when it touches down, so it
									// goes there and then. That is what keeps debris from stacking up
									// into a pile: time alone cannot guarantee it, since a cube with a
									// late threshold would sit on the ground waiting for it. A melt is
									// the opposite - the pool it leaves is the point - so that one
									// keeps its cubes and lets the clock take them.
									if (RemoveOnLanding)
									{
										bone.Cubes.RemoveAt(i);
										_dissolveAt.Remove(cube);
										continue;
									}

									cube.Origin[1] = 0f;
									cube.AngularVelocity = Vector3.Zero;
									settled++;

									// First contact: the piece flattens and pushes outward, which is
									// what turns a heap into a puddle. Later arrivals hit the
									// spreading layer and widen it further.
									cube.Velocity = Vector3.Zero;

									if (!_spreading.TryGetValue(cube, out var spread))
									{
										// Somewhere on the pool's disc, drawn so points land evenly
										// across its area rather than bunching in the middle: the
										// square root is what makes a uniform radius uniform in area.
										// A fresh angle also breaks up the spokes that pushing each
										// cube out along its own starting angle produced.
										// A sunflower spiral. Radius by arrival order, so the pool fills
										// from the middle out and its edge advances as more lands, and
										// the angle stepped by the golden angle so consecutive pieces
										// never line up. Random angles were the source of the holes:
										// scattering points at random over an area covers only about
										// 63% of it no matter how many you use, because they clump.
										int arrival = _spreading.Count;
										float filled = _meltCubeCount <= 0 ? 1f : (float) arrival / _meltCubeCount;
										double angle = arrival * GoldenAngle;
										float radius = RadiusAt(angle) * (float) Math.Sqrt(filled);

										spread = (
											new Vector3(cube.Size[0], cube.Size[1], cube.Size[2]),
											new Vector3((float) Math.Cos(angle) * radius, 0, (float) Math.Sin(angle) * radius),
											0f);
									}

									// The flow stops the moment the last piece touches down. Left
									// running, the pool kept creeping outward after there was nothing
									// more to feed it and pulled itself open in the middle, which is
									// where the holes came from.
									if (!_allLanded)
									{
										float step = 1f - (float) Math.Pow(1 - MeltFlowPerTick, dt);
										spread.Progress = Math.Min(1f, spread.Progress + step);
										_spreading[cube] = spread;

										cube.Origin[0] += (spread.Target.X - cube.Origin[0]) * step;
										cube.Origin[2] += (spread.Target.Z - cube.Origin[2]) * step;

										// Stamped by where it is heading: pieces bound for the middle
										// stay tall, pieces bound for the rim are pressed flat.
										double targetAngle = Math.Atan2(spread.Target.Z, spread.Target.X);
										float edge = RadiusAt(targetAngle);
										float where = edge <= 0
											? 0f
											: Math.Min(1f, (float) Math.Sqrt(spread.Target.X * spread.Target.X + spread.Target.Z * spread.Target.Z) / edge);
										float flattened = MeltCentreThickness + (MeltRimThickness - MeltCentreThickness) * where;

										// Eases from the box it landed as toward that thickness, with
										// the footprint derived at every step so it holds its volume
										// the whole way rather than only at the ends.
										float height = 1f + (flattened - 1f) * spread.Progress;

										cube.Size[1] = spread.Size.Y * height;
										cube.Size[0] = spread.Size.X * SpreadFor(height);
										cube.Size[2] = spread.Size.Z * SpreadFor(height);

										stillMoving = true;
									}

									continue;
								}

								if (cube.AngularVelocity != Vector3.Zero && cube.Rotation != null)
								{
									// Kept inside a turn so the numbers stay short: the whole model is
									// reserialised every frame and 12.34 costs less than 3612.34.
									cube.Rotation[0] = Wrap(cube.Rotation[0] + cube.AngularVelocity.X * dt);
									cube.Rotation[1] = Wrap(cube.Rotation[1] + cube.AngularVelocity.Y * dt);
									cube.Rotation[2] = Wrap(cube.Rotation[2] + cube.AngularVelocity.Z * dt);
								}

								stillMoving = true;
								falling++;

								float x = cube.Origin[0];
								float y = cube.Origin[1];
								float z = cube.Origin[2];

								cube.Origin = new[]
								{
									x + cube.Velocity.X * dt,
									Math.Max(0f, y + cube.Velocity.Y * dt),
									z + cube.Velocity.Z * dt,
								};
								cube.Velocity -= new Vector3(0, Gravity * dt, 0);
								cube.Velocity *= drag;
							}
						}
					}

					// The entity shrinks with the model. The nametag hangs off the collision box, so
					// without this it stays floating at head height over a puddle. Model units are
					// sixteenths of a block, and a little is kept so the box never reaches zero.
					_modelHeight = Math.Max(0.05f, tallest / ModelUnitsPerBlock);

					// Nothing left in the air, so the pool is as wide as it is going to get.
					if (falling == 0 && _spreading.Count > 0) _allLanded = true;

					// Fade as it falls, not after: alpha tracks the fraction of cubes already at
					// rest, so the debris is gone by the time the last piece lands instead of
					// forming a pile that then disappears. Self-scaling, so it holds for any
					// gravity or frame rate without a tuned duration.
					float settledAlpha = total == 0 ? 1f : 1f - settled / (float) total;
					float timeAlpha = 1f - _tick / (float) FadeTicks;
					float alpha = Math.Max(0f, Math.Min(settledAlpha, timeAlpha));

					if (!stillMoving)
					{
						Log.Warn("Done. De-register tick.");
						mob.Ticking -= FakeMeltTicking;

						// Reset?
						if (ResetOnEnd)
						{
							Skin skin = mob.Skin;

							var updateSkin = McpePlayerSkin.CreateObject();
							updateSkin.NoBatch = true;
							updateSkin.uuid = mob.ClientUuid;
							updateSkin.oldSkinName = mob.Skin.SkinId;
							updateSkin.skinName = mob.Skin.SkinId;
							updateSkin.skin = skin;
							mob.Level.RelayBroadcast(updateSkin);
						}

						// The blast has nothing left once the debris has gone, so it goes straight
						// away. The melt has just finished making a puddle, so it leaves it lying
						// there and lets the stand-in's own lifetime clear it up.
						if (Dissolves) mob.DespawnEntity();
					}
					else
					{
						SendFrame(mob, alpha, physicsTimer.ElapsedMilliseconds);
					}
				}
				catch (Exception e)
				{
					mob.Ticking -= FakeMeltTicking;
					Log.Error(e);
				}
			}
		}

		//private void StrikeTick(object state)
		//{
		//	if (!Monitor.TryEnter(state)) return;

		//	try
		//	{
		//		GravityGeometryBehavior signal = state as GravityGeometryBehavior;

		//		if (signal == null) return;
		//		if (signal.Timer == null) return;
		//		if (signal.CurrentModel == null) return;

		//		if (signal.Tick++ >= signal.MaxDuration)
		//		{
		//			Log.Warn($"Reached end of animation: {signal.Tick}");
		//			signal.Tick = 0;
		//			signal.Timer.Dispose();
		//			signal.Timer = null;

		//			// Reset?
		//			if (signal.ResetOnEnd)
		//			{
		//				Skin skin = signal.Skin;

		//				McpePlayerSkin updateSkin = McpePlayerSkin.CreateObject();
		//				updateSkin.NoBatch = true;
		//				updateSkin.uuid = signal.Uuid;
		//				updateSkin.skinId = skin.SkinId;
		//				updateSkin.skinData = skin.SkinData;
		//				updateSkin.capeData = skin.CapeData;
		//				updateSkin.geometryModel = skin.SkinGeometryName;
		//				updateSkin.geometryData = skin.SkinGeometry;
		//				signal.Level.RelayBroadcast(updateSkin);
		//			}

		//			return;
		//		}

		//		try
		//		{
		//			if (signal.Tick == 1)
		//			{
		//				var geometry = signal.CurrentModel.CollapseToDerived(signal.CurrentModel.FindGeometry(signal.Skin.SkinGeometryName));
		//				geometry.Subdivide(false, true, false, true);
		//				signal.CurrentModel.Clear();
		//				signal.CurrentModel.Add(geometry.Name, geometry);
		//			}

		//			int[] flashes = {50, 60, 100, 120, 135, 155, 170, 185, 209, 250, 300, 330, 380, 440};

		//			if (flashes.Contains((int) signal.Tick + 1))
		//			{
		//				signal.Level.StrikeLightning(signal.Position);
		//			}


		//			if (flashes.Contains((int) signal.Tick) || flashes.Contains((int) signal.Tick - 4))
		//			{
		//				string fullName = signal.CurrentModel.Keys.First(m => m.StartsWith(signal.Skin.SkinGeometryName));
		//				signal.CurrentModel[fullName].AnimationArmsOutFront = true;
		//				string skinString = Skin.ToJson(signal.CurrentModel);

		//				string newName = $"geometry.{DateTime.UtcNow.Ticks}.{signal.Uuid}";
		//				skinString = skinString.Replace(fullName, newName);

		//				Skin skin = signal.Skin;

		//				McpePlayerSkin updateSkin = McpePlayerSkin.CreateObject();
		//				updateSkin.NoBatch = true;
		//				updateSkin.uuid = signal.Uuid;
		//				updateSkin.skinId = skin.SkinId;
		//				updateSkin.skinData = skin.SkinData;
		//				updateSkin.capeData = skin.CapeData;
		//				updateSkin.geometryModel = newName;
		//				updateSkin.geometryData = skinString;
		//				signal.Level.RelayBroadcast(updateSkin);
		//			}

		//			if (flashes.Contains((int) signal.Tick - 2) || flashes.Contains((int) signal.Tick - 6))
		//			{
		//				Skin skin = signal.Skin;

		//				McpePlayerSkin updateSkin = McpePlayerSkin.CreateObject();
		//				updateSkin.NoBatch = true;
		//				updateSkin.uuid = signal.Uuid;
		//				updateSkin.skinId = skin.SkinId;
		//				updateSkin.skinData = skin.SkinData;
		//				updateSkin.capeData = skin.CapeData;
		//				updateSkin.geometryModel = skin.SkinGeometryName;
		//				updateSkin.geometryData = skin.SkinGeometry;
		//				signal.Level.RelayBroadcast(updateSkin);
		//			}
		//		}
		//		catch (Exception e)
		//		{
		//			Log.Error(e);
		//		}
		//	}
		//	finally
		//	{
		//		Monitor.Exit(state);
		//	}
		//}
	}
}