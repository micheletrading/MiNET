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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using fNbt;
using log4net;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using Newtonsoft.Json;

namespace MiNET.Blocks
{
	public interface ICustomBlockFactory
	{
		Block GetBlockById(int blockId);
	}

	public class R12ToCurrentBlockMapEntry
	{
		public string StringId { get; set; }
		public short Meta { get; set; }
		public BlockStateContainer State { get; set; }

		public R12ToCurrentBlockMapEntry(string id, short meta, BlockStateContainer state)
		{
			StringId = id;
			Meta = meta;
			State = state;
		}
	}
	
	public static class BlockFactory
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(BlockFactory));

		public static ICustomBlockFactory CustomBlockFactory { get; set; }

		public static readonly byte[] TransparentBlocks = new byte[600];
		public static readonly byte[] LuminousBlocks = new byte[600];
		public static Dictionary<string, int> NameToId { get; private set; }

		/// <summary>
		///     Block identity by its Minecraft name, which is what the palette, the wire and items all
		///     use. The legacy numeric id predates flattening: one id covered every wood type, every
		///     colour, and the variant lived in a data value. Looking a block up by that id hands back
		///     the pre-flattening class, whose state does not exist in the palette any more, so the
		///     block cannot be written to the world. Anything that is not translating stored data
		///     should come through here.
		/// </summary>
		private static Dictionary<string, Type> NameToBlockType { get; set; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		public static BlockPalette BlockPalette { get; set; } = null;
		public static HashSet<BlockStateContainer> BlockStates { get; set; } = null;

		// runtime id -> FNV-1a 32 network hash of the block state. This is the order-independent
		// block id form used on the wire when StartGame.blockNetworkIdsAreHashes is true (the
		// vanilla BDS default since 1.19.80); hashing over {name, states} makes chunk block ids
		// immune to palette-order differences between server and client. minecraft:unknown is
		// hardcoded to -2 by the vanilla implementation.
		private static readonly Lazy<uint[]> _networkHashes = new Lazy<uint[]>(() =>
		{
			var hashes = new uint[BlockPalette.Count];
			for (int i = 0; i < BlockPalette.Count; i++)
			{
				hashes[i] = ComputeNetworkHash(BlockPalette[i]);
			}
			return hashes;
		});

		public static uint GetNetworkHash(int runtimeId)
		{
			return _networkHashes.Value[runtimeId];
		}

		private static readonly Lazy<HashSet<uint>> _validHashes = new Lazy<HashSet<uint>>(() => new HashSet<uint>(_networkHashes.Value));

		public static bool IsValidNetworkHash(uint hash) => _validHashes.Value.Contains(hash);

		// Network hash of a block name's default (first palette) state; 0 if the name is unknown.
		public static uint GetDefaultStateHash(string name)
		{
			if (string.IsNullOrEmpty(name)) return 0;
			if (!name.StartsWith("minecraft:")) name = "minecraft:" + name;
			return _defaultStateByName.Value.TryGetValue(name, out var state) ? GetNetworkHash(state.RuntimeId) : 0;
		}

		// FNV-1a 32 over the standard little-endian (non-varint) NBT of {name, states}, states
		// sorted alphabetically by name. Mirrors MiNET.Client NetworkBlockPalette.ComputeNetworkHash,
		// which is verified against live BDS 1.26.34 chunk data.
		// The serialized NBT document the network hash is computed over, exposed for tooling
		// (checksum-algorithm search against the BDS-known registry checksum).
		public static byte[] GetNetworkHashDocument(int runtimeId)
		{
			return SerializeHashDocument(BlockPalette[runtimeId]);
		}

		public static uint ComputeNetworkHash(BlockStateContainer container)
		{
			if (container.Name == "minecraft:unknown") return unchecked((uint) -2);

			byte[] bytes = SerializeHashDocument(container);

			uint hash = 0x811c9dc5;
			foreach (byte b in bytes)
			{
				hash ^= b;
				hash *= 0x01000193;
			}

			return hash;
		}

		private static byte[] SerializeHashDocument(BlockStateContainer container)
		{
			var statesCompound = new NbtCompound("states");
			foreach (IBlockState state in container.States.OrderBy(s => s.Name, StringComparer.Ordinal))
			{
				switch (state)
				{
					case BlockStateByte b:
						statesCompound.Add(new NbtByte(b.Name, b.Value));
						break;
					case BlockStateInt i:
						statesCompound.Add(new NbtInt(i.Name, i.Value));
						break;
					case BlockStateString s:
						statesCompound.Add(new NbtString(s.Name, s.Value));
						break;
				}
			}

			var root = new NbtCompound("")
			{
				new NbtString("name", container.Name),
				statesCompound
			};

			var file = new NbtFile
			{
				BigEndian = false,
				UseVarInt = false,
				RootTag = root
			};

			return file.SaveToBuffer(NbtCompression.None);
		}

		public static int[] LegacyToRuntimeId = new int[65536];

		static BlockFactory()
		{
			for (int i = 0; i < byte.MaxValue * 2; i++)
			{
				var block = GetBlockById(i);
				if (block != null)
				{
					if (block.IsTransparent)
					{
						TransparentBlocks[block.Id] = 1;
					}
					if (block.LightLevel > 0)
					{
						LuminousBlocks[block.Id] = (byte) block.LightLevel;
					}
				}
			}

			NameToId = BuildNameToId();
			NameToBlockType = BuildNameToBlockType();

			for (int i = 0; i < LegacyToRuntimeId.Length; ++i)
			{
				LegacyToRuntimeId[i] = -1;
			}

			var assembly = Assembly.GetAssembly(typeof(Block));

			lock (lockObj)
			{
				Dictionary<string, int> idMapping = new Dictionary<string, int>(ResourceUtil.ReadResource<Dictionary<string, int>>("block_id_map.json", typeof(Block), "Data"), StringComparer.OrdinalIgnoreCase);
				Dictionary<string, int> itemIdMapping = new Dictionary<string, int>(ResourceUtil.ReadResource<Dictionary<string, int>>("item_id_map.json", typeof(Item), "Data"), StringComparer.OrdinalIgnoreCase);

				// The palette is compiled in rather than parsed from NBT at startup. It is an
				// ordered list whose index IS the network id, and all of that is known when the
				// code is generated, so there is nothing to work out here. Regenerate with
				// MiNET.BlockGen after moving the pinned data submodule.
				BlockPalette = new BlockPalette();
				BlockPaletteData.Create(BlockPalette);

				List<R12ToCurrentBlockMapEntry> legacyStateMap = new List<R12ToCurrentBlockMapEntry>();
				using (var stream = assembly.GetManifestResourceStream(typeof(Block).Namespace + ".Data.r12_to_current_block_map.bin"))
				{
					while (stream.Position < stream.Length)
					{
						var length = VarInt.ReadUInt32(stream);
						byte[] bytes = new byte[length];
						stream.Read(bytes, 0, bytes.Length);

						string stringId = Encoding.UTF8.GetString(bytes);

						bytes = new byte[2];
						stream.Read(bytes, 0, bytes.Length);
						var meta = BitConverter.ToInt16(bytes);

						var compound = Packet.ReadNbtCompound(stream, true);

						legacyStateMap.Add(new R12ToCurrentBlockMapEntry(stringId, meta, GetBlockStateContainer(compound)));
					}
				}
				
				Dictionary<string, List<int>> idToStatesMap = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

				for (var index = 0; index < BlockPalette.Count; index++)
				{
					var state = BlockPalette[index];
					List<int> candidates;

					if (!idToStatesMap.TryGetValue(state.Name, out candidates))
						candidates = new List<int>();

					candidates.Add(index);

					idToStatesMap[state.Name] = candidates;
				}

				foreach (var pair in legacyStateMap)
				{
					if (!idMapping.TryGetValue(pair.StringId, out int id))
						continue;

					var data = pair.Meta;

					if (data > 15)
					{
						continue;
					}

					var mappedState = pair.State;
					var mappedName = pair.State.Name;

					if (!idToStatesMap.TryGetValue(mappedName, out var matching))
					{
						continue;
					}

					foreach (var match in matching)
					{
						var networkState = BlockPalette[match];

						var thisStates = new HashSet<IBlockState>(mappedState.States);
						var otherStates = new HashSet<IBlockState>(networkState.States);

						otherStates.IntersectWith(thisStates);

						if (otherStates.Count == thisStates.Count)
						{
							BlockPalette[match].Id = id;
							BlockPalette[match].Data = data;

							// Blocks whose item form has its own id (doors, beds, ...) must pick as the item, not the block
							BlockPalette[match].ItemInstance = new ItemPickInstance()
							{
								Id = (short) (itemIdMapping.TryGetValue(networkState.Name, out int pickItemId) ? pickItemId : id),
								Metadata = data,
								WantNbt = false
							};

							LegacyToRuntimeId[(id << 4) | (byte) data] = match;

							break;
						}
					}
				}

				// Blocks added after the R12 legacy map (chain, blackstone, candles, ...) never match above.
				// Fall back to name-based lookups so GetBlockById() and block picking still work for them.
				foreach (var state in BlockPalette)
				{
					if (state.Id != 0 || state.Name == "minecraft:air") continue;

					if (idMapping.TryGetValue(state.Name, out int legacyId))
					{
						state.Id = legacyId;
					}

					if (state.ItemInstance == null && itemIdMapping.TryGetValue(state.Name, out int itemId))
					{
						state.ItemInstance = new ItemPickInstance()
						{
							Id = (short) itemId,
							Metadata = 0,
							WantNbt = false
						};
					}
				}

				foreach(var record in BlockPalette)
				{
					var states = new List<NbtTag>();
					foreach (IBlockState state in record.States)
					{
						NbtTag stateTag = null;
						switch (state)
						{
							case BlockStateByte blockStateByte:
								stateTag = new NbtByte(state.Name, blockStateByte.Value);
								break;
							case BlockStateInt blockStateInt:
								stateTag = new NbtInt(state.Name, blockStateInt.Value);
								break;
							case BlockStateString blockStateString:
								stateTag = new NbtString(state.Name, blockStateString.Value);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(state));
						}
						states.Add(stateTag);
					}

					var nbt = new NbtFile()
					{
						BigEndian = false,
						UseVarInt = true,
						RootTag = new NbtCompound("states", states)
					};

					byte[] nbtBinary = nbt.SaveToBuffer(NbtCompression.None);

					record.StatesCacheNbt = nbtBinary;
				}
			}
			
			BlockStates = new HashSet<BlockStateContainer>(BlockPalette);
		}
		
		private static BlockStateContainer GetBlockStateContainer(NbtTag tag)
		{
			var record = new BlockStateContainer();

			string name = tag["name"].StringValue;
			record.Name = name;
			record.States = GetBlockStates(tag);

			return record;
		}

		private static List<IBlockState> GetBlockStates(NbtTag tag)
		{
			var result = new List<IBlockState>();

			var states = tag["states"];
			if (states != null && states is NbtCompound compound)
			{
				foreach (var stateEntry in compound)
				{
					switch (stateEntry)
					{
						case NbtInt nbtInt:
							result.Add(new BlockStateInt()
							{
								Name = nbtInt.Name,
								Value = nbtInt.Value
							});
							break;
						case NbtByte nbtByte:
							result.Add(new BlockStateByte()
							{
								Name = nbtByte.Name,
								Value = nbtByte.Value
							});
							break;
						case NbtString nbtString:
							result.Add(new BlockStateString()
							{
								Name = nbtString.Name,
								Value = nbtString.Value
							});
							break;
					}
				}
			}

			return result;
		}

		private static object lockObj = new object();

		/// <summary>
		///     Every Block subclass that can be constructed, keyed on the name it reports. Generated
		///     classes win over hand-written ones claiming the same name: the generated set is the
		///     one that matches the current palette.
		/// </summary>
		private static Dictionary<string, Type> BuildNameToBlockType()
		{
			var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

			foreach (Type type in Assembly.GetAssembly(typeof(Block)).GetTypes())
			{
				if (type.IsAbstract || !typeof(Block).IsAssignableFrom(type)) continue;
				if (type.GetConstructor(Type.EmptyTypes) == null) continue;

				Block block;
				try
				{
					block = (Block) Activator.CreateInstance(type);
				}
				catch (Exception e)
				{
					Log.Debug($"Could not construct block type {type.Name} for the name map", e);
					continue;
				}

				if (string.IsNullOrEmpty(block?.Name)) continue;

				if (map.ContainsKey(block.Name) && !block.IsGenerated) continue;

				map[block.Name] = type;
			}

			return map;
		}

		private static Dictionary<string, int> BuildNameToId()
		{
			//TODO: Refactor to use the Item.Name in hashed set instead.

			var nameToId = new Dictionary<string, int>();
			for (int idx = 0; idx < 1000; idx++)
			{
				Block block = GetBlockById(idx);
				string name = block.GetType().Name.ToLowerInvariant();

				if (name.Equals("block"))
				{
					//if (Log.IsDebugEnabled)
					//	Log.Debug($"Missing implementation for block ID={idx}");
					continue;
				}

				nameToId.Add(name, idx);
			}

			return nameToId;
		}

		public static int GetBlockIdByName(string blockName)
		{
			blockName = blockName.ToLowerInvariant().Replace("_", "").Replace("minecraft:", "");

			if (NameToId.ContainsKey(blockName))
			{
				return NameToId[blockName];
			}

			return 0;
		}

		// Palette-name resolution without legacy ids: the first palette state for a name is its
		// default state (canonical palette order). Covers every current block, including the
		// post-flattening ones that never had a legacy id.
		private static readonly Lazy<Dictionary<string, BlockStateContainer>> _defaultStateByName = new Lazy<Dictionary<string, BlockStateContainer>>(() =>
		{
			var result = new Dictionary<string, BlockStateContainer>(StringComparer.OrdinalIgnoreCase);
			foreach (BlockStateContainer state in BlockPalette)
			{
				result.TryAdd(state.Name, state);
			}
			return result;
		});

		// Typed classes for blocks that never had a legacy numeric id (everything the palette
		// gained after the 1.16 flattening: GetBlockById()'s switch stops at 559). These are
		// discovered by reflection instead of a hand-maintained id switch, keyed by the class
		// name the generator derives from the palette name (see GenerateBlocksTests.CodeName):
		// PascalCase with underscores removed, e.g. "minecraft:amethyst_block" -> "AmethystBlock".
		private static readonly Lazy<Dictionary<string, Type>> _typedBlockTypeByClassName = new Lazy<Dictionary<string, Type>>(() =>
		{
			var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
			foreach (Type t in typeof(Block).Assembly.GetTypes())
			{
				if (t == typeof(Block) || !typeof(Block).IsAssignableFrom(t) || t.IsAbstract) continue;
				if (t.GetConstructor(Type.EmptyTypes) == null) continue;
				map.TryAdd(t.Name, t);
			}
			return map;
		});

		private static string PaletteNameToClassName(string name)
		{
			string bare = name.Replace("minecraft:", "");
			var sb = new StringBuilder();
			bool upper = true;
			foreach (char c in bare)
			{
				if (c == '_')
				{
					upper = true;
					continue;
				}
				sb.Append(upper ? char.ToUpperInvariant(c) : c);
				upper = false;
			}
			return sb.ToString();
		}

		public static Block GetBlockByPaletteName(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			if (!name.StartsWith("minecraft:")) name = "minecraft:" + name;

			// Typed class when one exists (legacy-mapped blocks).
			Block typed = GetBlockByName(name);
			if (typed != null) return typed;

			if (!_defaultStateByName.Value.TryGetValue(name, out BlockStateContainer defaultState)) return null;

			// Typed class discovered by reflection, for blocks generated without a legacy id.
			if (_typedBlockTypeByClassName.Value.TryGetValue(PaletteNameToClassName(name), out Type blockType))
			{
				var typedBlock = (Block) Activator.CreateInstance(blockType);
				typedBlock.SetState(defaultState);
				return typedBlock;
			}

			var block = new Block(defaultState.Name, defaultState.Id);
			block.SetState(defaultState);
			return block;
		}

		/// <summary>
		///     The block for a Minecraft name, e.g. minecraft:oak_planks. This is the lookup to use:
		///     it returns the class whose state round-trips through the palette.
		/// </summary>
		public static Block GetBlockByName(string blockName)
		{
			if (string.IsNullOrEmpty(blockName)) return null;

			if (!blockName.Contains(':')) blockName = "minecraft:" + blockName;

			if (NameToBlockType.TryGetValue(blockName, out Type type)) return (Block) Activator.CreateInstance(type);

			// Older callers passed the C# type name with the underscores stripped.
			string legacyKey = blockName.ToLowerInvariant().Replace("_", "").Replace("minecraft:", "");
			if (NameToId.TryGetValue(legacyKey, out int legacyId)) return GetBlockById(legacyId);

			return null;
		}

		/// <summary>
		///     The block for a palette index, with its state applied. Goes by name, so the block that
		///     comes back can be asked for its state again and resolve to the same runtime id.
		/// </summary>
		public static Block GetBlockByRuntimeId(int runtimeId)
		{
			if (runtimeId < 0 || runtimeId >= BlockPalette.Count) return null;

			BlockStateContainer blockState = BlockPalette[runtimeId];

			Block block = GetBlockByName(blockState.Name);
			if (block == null)
			{
				Log.Warn($"No block class for palette name {blockState.Name} (runtime id {runtimeId})");
				return null;
			}

			block.SetState(blockState.States);
			return block;
		}

		public static Block GetBlockById(int blockId, byte metadata)
		{
			int runtimeId = (int) GetRuntimeId(blockId, metadata);
			if (runtimeId < 0 || runtimeId >= BlockPalette.Count) return null;
			BlockStateContainer blockState = BlockPalette[runtimeId];
			// By name, so the class round-trips through the palette. The legacy switch is the
			// fallback for palette names we have no class for, and never returns null.
			Block block = GetBlockByName(blockState.Name) ?? GetBlockById(blockState.Id);
			block.SetState(blockState.States);
			return block;
		}

		public static Block GetBlockById(int blockId)
		{
			Block block = null;

			if (CustomBlockFactory != null) block = CustomBlockFactory.GetBlockById(blockId);

			if (block != null) return block;

			block = blockId switch
			{
				0 => new Air(),
				1 => new Stone(),
				2 => new Grass(),
				3 => new Dirt(),
				4 => new Cobblestone(),
				5 => new Planks(),
				6 => new Sapling(),
				7 => new Bedrock(),
				8 => new FlowingWater(),
				9 => new Water(),
				10 => new FlowingLava(),
				11 => new Lava(),
				12 => new Sand(),
				13 => new Gravel(),
				14 => new GoldOre(),
				15 => new IronOre(),
				16 => new CoalOre(),
				17 => new Log(),
				18 => new Leaves(),
				19 => new Sponge(),
				20 => new Glass(),
				21 => new LapisOre(),
				22 => new LapisBlock(),
				23 => new Dispenser(),
				24 => new Sandstone(),
				25 => new Noteblock(),
				26 => new Bed(),
				27 => new GoldenRail(),
				28 => new DetectorRail(),
				29 => new StickyPiston(),
				30 => new Web(),
				31 => new TallGrass(),
				32 => new Deadbush(),
				33 => new Piston(),
				34 => new PistonArmCollision(),
				35 => new Wool(),
				36 => new Element0(),
				37 => new YellowFlower(),
				38 => new RedFlower(),
				39 => new BrownMushroom(),
				40 => new RedMushroom(),
				41 => new GoldBlock(),
				42 => new IronBlock(),
				43 => new DoubleStoneSlab(),
				44 => new StoneSlab(),
				45 => new BrickBlock(),
				46 => new Tnt(),
				47 => new Bookshelf(),
				48 => new MossyCobblestone(),
				49 => new Obsidian(),
				50 => new Torch(),
				51 => new Fire(),
				52 => new MobSpawner(),
				53 => new OakStairs(),
				54 => new Chest(),
				55 => new RedstoneWire(),
				56 => new DiamondOre(),
				57 => new DiamondBlock(),
				58 => new CraftingTable(),
				59 => new Wheat(),
				60 => new Farmland(),
				61 => new Furnace(),
				62 => new LitFurnace(),
				63 => new StandingSign(),
				64 => new WoodenDoor(),
				65 => new Ladder(),
				66 => new Rail(),
				67 => new StoneStairs(),
				68 => new WallSign(),
				69 => new Lever(),
				70 => new StonePressurePlate(),
				71 => new IronDoor(),
				72 => new WoodenPressurePlate(),
				73 => new RedstoneOre(),
				74 => new LitRedstoneOre(),
				75 => new UnlitRedstoneTorch(),
				76 => new RedstoneTorch(),
				77 => new StoneButton(),
				78 => new SnowLayer(),
				79 => new Ice(),
				80 => new Snow(),
				81 => new Cactus(),
				82 => new Clay(),
				83 => new Reeds(),
				84 => new Jukebox(),
				85 => new Fence(),
				86 => new Pumpkin(),
				87 => new Netherrack(),
				88 => new SoulSand(),
				89 => new Glowstone(),
				90 => new Portal(),
				91 => new LitPumpkin(),
				92 => new Cake(),
				93 => new UnpoweredRepeater(),
				94 => new PoweredRepeater(),
				95 => new InvisibleBedrock(),
				96 => new Trapdoor(),
				97 => new MonsterEgg(),
				98 => new Stonebrick(),
				99 => new BrownMushroomBlock(),
				100 => new RedMushroomBlock(),
				101 => new IronBars(),
				102 => new GlassPane(),
				103 => new MelonBlock(),
				104 => new PumpkinStem(),
				105 => new MelonStem(),
				106 => new Vine(),
				107 => new FenceGate(),
				108 => new BrickStairs(),
				109 => new StoneBrickStairs(),
				110 => new Mycelium(),
				111 => new Waterlily(),
				112 => new NetherBrick(),
				113 => new NetherBrickFence(),
				114 => new NetherBrickStairs(),
				115 => new NetherWart(),
				116 => new EnchantingTable(),
				117 => new BrewingStand(),
				118 => new Cauldron(),
				119 => new EndPortal(),
				120 => new EndPortalFrame(),
				121 => new EndStone(),
				122 => new DragonEgg(),
				123 => new RedstoneLamp(),
				124 => new LitRedstoneLamp(),
				125 => new Dropper(),
				126 => new ActivatorRail(),
				127 => new Cocoa(),
				128 => new SandstoneStairs(),
				129 => new EmeraldOre(),
				130 => new EnderChest(),
				131 => new TripwireHook(),
				132 => new TripWire(),
				133 => new EmeraldBlock(),
				134 => new SpruceStairs(),
				135 => new BirchStairs(),
				136 => new JungleStairs(),
				137 => new CommandBlock(),
				138 => new Beacon(),
				139 => new CobblestoneWall(),
				140 => new FlowerPot(),
				141 => new Carrots(),
				142 => new Potatoes(),
				143 => new WoodenButton(),
				144 => new Skull(),
				145 => new Anvil(),
				146 => new TrappedChest(),
				147 => new LightWeightedPressurePlate(),
				148 => new HeavyWeightedPressurePlate(),
				149 => new UnpoweredComparator(),
				150 => new PoweredComparator(),
				151 => new DaylightDetector(),
				152 => new RedstoneBlock(),
				153 => new QuartzOre(),
				154 => new Hopper(),
				155 => new QuartzBlock(),
				156 => new QuartzStairs(),
				157 => new DoubleWoodenSlab(),
				158 => new WoodenSlab(),
				159 => new StainedHardenedClay(),
				160 => new StainedGlassPane(),
				161 => new Leaves2(),
				162 => new Log2(),
				163 => new AcaciaStairs(),
				164 => new DarkOakStairs(),
				165 => new Slime(),
				167 => new IronTrapdoor(),
				168 => new Prismarine(),
				169 => new SeaLantern(),
				170 => new HayBlock(),
				171 => new Carpet(),
				172 => new HardenedClay(),
				173 => new CoalBlock(),
				174 => new PackedIce(),
				175 => new DoublePlant(),
				176 => new StandingBanner(),
				177 => new WallBanner(),
				178 => new DaylightDetectorInverted(),
				179 => new RedSandstone(),
				180 => new RedSandstoneStairs(),
				181 => new DoubleStoneSlab2(),
				182 => new StoneSlab2(),
				183 => new SpruceFenceGate(),
				184 => new BirchFenceGate(),
				185 => new JungleFenceGate(),
				186 => new DarkOakFenceGate(),
				187 => new AcaciaFenceGate(),
				188 => new RepeatingCommandBlock(),
				189 => new ChainCommandBlock(),
				190 => new HardGlassPane(),
				192 => new ChemicalHeat(),
				193 => new SpruceDoor(),
				194 => new BirchDoor(),
				195 => new JungleDoor(),
				196 => new AcaciaDoor(),
				197 => new DarkOakDoor(),
				198 => new GrassPath(),
				199 => new Frame(),
				200 => new ChorusFlower(),
				201 => new PurpurBlock(),
				203 => new PurpurStairs(),
				205 => new UndyedShulkerBox(),
				206 => new EndBricks(),
				207 => new FrostedIce(),
				208 => new EndRod(),
				209 => new EndGateway(),
				210 => new Allow(),
				211 => new Deny(),
				212 => new BorderBlock(),
				213 => new Magma(),
				214 => new NetherWartBlock(),
				215 => new RedNetherBrick(),
				216 => new BoneBlock(),
				217 => new StructureVoid(),
				218 => new ShulkerBox(),
				219 => new PurpleGlazedTerracotta(),
				220 => new WhiteGlazedTerracotta(),
				221 => new OrangeGlazedTerracotta(),
				222 => new MagentaGlazedTerracotta(),
				223 => new LightBlueGlazedTerracotta(),
				224 => new YellowGlazedTerracotta(),
				225 => new LimeGlazedTerracotta(),
				226 => new PinkGlazedTerracotta(),
				227 => new GrayGlazedTerracotta(),
				228 => new SilverGlazedTerracotta(),
				229 => new CyanGlazedTerracotta(),
				230 => new Chalkboard(),
				231 => new BlueGlazedTerracotta(),
				232 => new BrownGlazedTerracotta(),
				233 => new GreenGlazedTerracotta(),
				234 => new RedGlazedTerracotta(),
				235 => new BlackGlazedTerracotta(),
				236 => new Concrete(),
				237 => new ConcretePowder(),
				239 => new UnderwaterTorch(),
				240 => new ChorusPlant(),
				241 => new StainedGlass(),
				242 => new Camera(),
				243 => new Podzol(),
				244 => new Beetroot(),
				245 => new Stonecutter(),
				246 => new Glowingobsidian(),
				247 => new Netherreactor(),
				248 => new InfoUpdate(),
				249 => new InfoUpdate2(),
				250 => new MovingBlock(),
				251 => new Observer(),
				252 => new StructureBlock(),
				253 => new HardGlass(),
				255 => new Reserved6(),
				257 => new PrismarineStairs(),
				258 => new DarkPrismarineStairs(),
				259 => new PrismarineBricksStairs(),
				260 => new StrippedSpruceLog(),
				261 => new StrippedBirchLog(),
				262 => new StrippedJungleLog(),
				263 => new StrippedAcaciaLog(),
				264 => new StrippedDarkOakLog(),
				265 => new StrippedOakLog(),
				266 => new BlueIce(),
				267 => new Element1(),
				268 => new Element2(),
				269 => new Element3(),
				270 => new Element4(),
				271 => new Element5(),
				272 => new Element6(),
				273 => new Element7(),
				274 => new Element8(),
				275 => new Element9(),
				276 => new Element10(),
				277 => new Element11(),
				278 => new Element12(),
				279 => new Element13(),
				280 => new Element14(),
				281 => new Element15(),
				282 => new Element16(),
				283 => new Element17(),
				284 => new Element18(),
				285 => new Element19(),
				286 => new Element20(),
				287 => new Element21(),
				288 => new Element22(),
				289 => new Element23(),
				290 => new Element24(),
				291 => new Element25(),
				292 => new Element26(),
				293 => new Element27(),
				294 => new Element28(),
				295 => new Element29(),
				296 => new Element30(),
				297 => new Element31(),
				298 => new Element32(),
				299 => new Element33(),
				300 => new Element34(),
				301 => new Element35(),
				302 => new Element36(),
				303 => new Element37(),
				304 => new Element38(),
				305 => new Element39(),
				306 => new Element40(),
				307 => new Element41(),
				308 => new Element42(),
				309 => new Element43(),
				310 => new Element44(),
				311 => new Element45(),
				312 => new Element46(),
				313 => new Element47(),
				314 => new Element48(),
				315 => new Element49(),
				316 => new Element50(),
				317 => new Element51(),
				318 => new Element52(),
				319 => new Element53(),
				320 => new Element54(),
				321 => new Element55(),
				322 => new Element56(),
				323 => new Element57(),
				324 => new Element58(),
				325 => new Element59(),
				326 => new Element60(),
				327 => new Element61(),
				328 => new Element62(),
				329 => new Element63(),
				330 => new Element64(),
				331 => new Element65(),
				332 => new Element66(),
				333 => new Element67(),
				334 => new Element68(),
				335 => new Element69(),
				336 => new Element70(),
				337 => new Element71(),
				338 => new Element72(),
				339 => new Element73(),
				340 => new Element74(),
				341 => new Element75(),
				342 => new Element76(),
				343 => new Element77(),
				344 => new Element78(),
				345 => new Element79(),
				346 => new Element80(),
				347 => new Element81(),
				348 => new Element82(),
				349 => new Element83(),
				350 => new Element84(),
				351 => new Element85(),
				352 => new Element86(),
				353 => new Element87(),
				354 => new Element88(),
				355 => new Element89(),
				356 => new Element90(),
				357 => new Element91(),
				358 => new Element92(),
				359 => new Element93(),
				360 => new Element94(),
				361 => new Element95(),
				362 => new Element96(),
				363 => new Element97(),
				364 => new Element98(),
				365 => new Element99(),
				366 => new Element100(),
				367 => new Element101(),
				368 => new Element102(),
				369 => new Element103(),
				370 => new Element104(),
				371 => new Element105(),
				372 => new Element106(),
				373 => new Element107(),
				374 => new Element108(),
				375 => new Element109(),
				376 => new Element110(),
				377 => new Element111(),
				378 => new Element112(),
				379 => new Element113(),
				380 => new Element114(),
				381 => new Element115(),
				382 => new Element116(),
				383 => new Element117(),
				384 => new Element118(),
				385 => new Seagrass(),
				393 => new Kelp(),
				394 => new DriedKelpBlock(),
				395 => new AcaciaButton(),
				396 => new BirchButton(),
				397 => new DarkOakButton(),
				398 => new JungleButton(),
				399 => new SpruceButton(),
				400 => new AcaciaTrapdoor(),
				401 => new BirchTrapdoor(),
				402 => new DarkOakTrapdoor(),
				403 => new JungleTrapdoor(),
				404 => new SpruceTrapdoor(),
				405 => new AcaciaPressurePlate(),
				406 => new BirchPressurePlate(),
				407 => new DarkOakPressurePlate(),
				408 => new JunglePressurePlate(),
				409 => new SprucePressurePlate(),
				410 => new CarvedPumpkin(),
				411 => new SeaPickle(),
				412 => new Conduit(),
				414 => new TurtleEgg(),
				415 => new BubbleColumn(),
				416 => new Barrier(),
				417 => new StoneSlab3(),
				418 => new Bamboo(),
				419 => new BambooSapling(),
				420 => new Scaffolding(),
				421 => new StoneSlab4(),
				422 => new DoubleStoneSlab3(),
				423 => new DoubleStoneSlab4(),
				424 => new GraniteStairs(),
				425 => new DioriteStairs(),
				426 => new AndesiteStairs(),
				427 => new PolishedGraniteStairs(),
				428 => new PolishedDioriteStairs(),
				429 => new PolishedAndesiteStairs(),
				430 => new MossyStoneBrickStairs(),
				431 => new SmoothRedSandstoneStairs(),
				432 => new SmoothSandstoneStairs(),
				433 => new EndBrickStairs(),
				434 => new MossyCobblestoneStairs(),
				435 => new NormalStoneStairs(),
				436 => new SpruceStandingSign(),
				437 => new SpruceWallSign(),
				438 => new SmoothStone(),
				439 => new RedNetherBrickStairs(),
				440 => new SmoothQuartzStairs(),
				441 => new BirchStandingSign(),
				442 => new BirchWallSign(),
				443 => new JungleStandingSign(),
				444 => new JungleWallSign(),
				445 => new AcaciaStandingSign(),
				446 => new AcaciaWallSign(),
				447 => new DarkoakStandingSign(),
				448 => new DarkoakWallSign(),
				449 => new Lectern(),
				450 => new Grindstone(),
				451 => new BlastFurnace(),
				452 => new StonecutterBlock(),
				453 => new Smoker(),
				454 => new LitSmoker(),
				455 => new CartographyTable(),
				456 => new FletchingTable(),
				457 => new SmithingTable(),
				458 => new Barrel(),
				459 => new Loom(),
				461 => new Bell(),
				462 => new SweetBerryBush(),
				463 => new Lantern(),
				464 => new Campfire(),
				466 => new Jigsaw(),
				467 => new Wood(),
				468 => new Composter(),
				469 => new LitBlastFurnace(),
				471 => new WitherRose(),
				472 => new StickyPistonArmCollision(),
				473 => new BeeNest(),
				474 => new Beehive(),
				475 => new HoneyBlock(),
				476 => new HoneycombBlock(),
				477 => new Lodestone(),
				478 => new CrimsonRoots(),
				479 => new WarpedRoots(),
				480 => new CrimsonStem(),
				481 => new WarpedStem(),
				482 => new WarpedWartBlock(),
				483 => new CrimsonFungus(),
				484 => new WarpedFungus(),
				485 => new Shroomlight(),
				486 => new WeepingVines(),
				487 => new CrimsonNylium(),
				488 => new WarpedNylium(),
				489 => new Basalt(),
				490 => new PolishedBasalt(),
				491 => new SoulSoil(),
				492 => new SoulFire(),
				493 => new NetherSprouts(),
				494 => new Target(),
				495 => new StrippedCrimsonStem(),
				496 => new StrippedWarpedStem(),
				497 => new CrimsonPlanks(),
				498 => new WarpedPlanks(),
				499 => new CrimsonDoor(),
				500 => new WarpedDoor(),
				501 => new CrimsonTrapdoor(),
				502 => new WarpedTrapdoor(),
				505 => new CrimsonStandingSign(),
				506 => new WarpedStandingSign(),
				507 => new CrimsonWallSign(),
				508 => new WarpedWallSign(),
				509 => new CrimsonStairs(),
				510 => new WarpedStairs(),
				511 => new CrimsonFence(),
				512 => new WarpedFence(),
				513 => new CrimsonFenceGate(),
				514 => new WarpedFenceGate(),
				515 => new CrimsonButton(),
				516 => new WarpedButton(),
				517 => new CrimsonPressurePlate(),
				518 => new WarpedPressurePlate(),
				519 => new CrimsonSlab(),
				520 => new WarpedSlab(),
				521 => new CrimsonDoubleSlab(),
				522 => new WarpedDoubleSlab(),
				523 => new SoulTorch(),
				524 => new SoulLantern(),
				525 => new NetheriteBlock(),
				526 => new AncientDebris(),
				527 => new RespawnAnchor(),
				528 => new Blackstone(),
				529 => new PolishedBlackstoneBricks(),
				530 => new PolishedBlackstoneBrickStairs(),
				531 => new BlackstoneStairs(),
				532 => new BlackstoneWall(),
				533 => new PolishedBlackstoneBrickWall(),
				534 => new ChiseledPolishedBlackstone(),
				535 => new CrackedPolishedBlackstoneBricks(),
				536 => new GildedBlackstone(),
				537 => new BlackstoneSlab(),
				538 => new BlackstoneDoubleSlab(),
				539 => new PolishedBlackstoneBrickSlab(),
				540 => new PolishedBlackstoneBrickDoubleSlab(),
				541 => new Chain(),
				542 => new TwistingVines(),
				543 => new NetherGoldOre(),
				544 => new CryingObsidian(),
				545 => new SoulCampfire(),
				546 => new PolishedBlackstone(),
				547 => new PolishedBlackstoneStairs(),
				548 => new PolishedBlackstoneSlab(),
				549 => new PolishedBlackstoneDoubleSlab(),
				550 => new PolishedBlackstonePressurePlate(),
				551 => new PolishedBlackstoneButton(),
				552 => new PolishedBlackstoneWall(),
				553 => new WarpedHyphae(),
				554 => new CrimsonHyphae(),
				555 => new StrippedCrimsonHyphae(),
				556 => new StrippedWarpedHyphae(),
				557 => new ChiseledNetherBricks(),
				558 => new CrackedNetherBricks(),
				559 => new QuartzBricks(),
				_ => GetTypedBlockForLegacyId(blockId)
			};

			return block;
		}

		// Legacy id (block_id_map.json, negatives included) -> block name.
		private static readonly Lazy<Dictionary<int, string>> _blockNameByLegacyId = new Lazy<Dictionary<int, string>>(() =>
		{
			var idMap = ResourceUtil.ReadResource<Dictionary<string, int>>("block_id_map.json", typeof(Block), "Data");
			var map = new Dictionary<int, string>();
			foreach (KeyValuePair<string, int> kv in idMap) map.TryAdd(kv.Value, kv.Key);
			return map;
		});

		// Legacy ids past the generated switch resolve through the palette to a TYPED class:
		// legacy id -> name (block_id_map) -> palette default state -> generated class. A bare
		// Block is the last resort for ids with no palette mapping at all (see Block.GetState,
		// which warns loudly if such an instance is ever asked for state).
		private static Block GetTypedBlockForLegacyId(int blockId)
		{
			// The static initializer's warm-up loop (transparency/light tables) runs before the
			// palette fields are assigned; those calls only read id-based properties, so a bare
			// Block is correct there. Every call after initialization resolves typed.
			if (BlockPalette == null) return new Block(blockId);

			// Ids above 255 can be the folded encoding of a negative legacy id
			// (ItemFactory folds id < 0 to abs(id) + 255); block_id_map stores real negatives.
			if (!_blockNameByLegacyId.Value.TryGetValue(blockId, out string name) && blockId > 255)
			{
				_blockNameByLegacyId.Value.TryGetValue(-(blockId - 255), out name);
			}

			if (name != null
				&& _defaultStateByName.Value.TryGetValue(name, out BlockStateContainer defaultState)
				&& _typedBlockTypeByClassName.Value.TryGetValue(PaletteNameToClassName(name), out Type blockType))
			{
				var typedBlock = (Block) Activator.CreateInstance(blockType);
				typedBlock.SetState(defaultState);
				return typedBlock;
			}

			return new Block(blockId);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint GetRuntimeId(int blockId, byte metadata)
		{
			int idx = TryGetRuntimeId(blockId, metadata);
			if (idx != -1)
			{
				return (uint) idx;
			}

			//block found with bad metadata, try getting with zero
			int idx2 = TryGetRuntimeId(blockId, 0);
			if (idx2 != -1)
			{
				return (uint) idx2;
			}

			return (uint) TryGetRuntimeId(248, 0); //legacy id for info_update block (for unknown block)
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int TryGetRuntimeId(int blockId, byte metadata)
		{
			return LegacyToRuntimeId[(blockId << 4) | metadata];
		}
	}
}