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
using System.Collections.Generic;
using fNbt;
using MiNET.Items;
using MiNET.Utils;

namespace MiNET
{
	// ReSharper disable RedundantArgumentDefaultValue
	// Creative tab groups captured from vanilla BDS 1.26.34 (Items/Data/creative_groups.json):
	// the group list (category, translation name, icon item name) and, per creative entry, the
	// index of the group it belongs to (aligned with CreativeInventoryItems order).
	public class CreativeGroupData
	{
		public List<CreativeGroupDef> Groups { get; set; }
		public List<CreativeEntryDef> Entries { get; set; }
		public List<int> EntryGroups { get; set; }
	}

	// A creative entry's exact wire identity from the vanilla capture. The catalog packet is
	// built from these verbatim; InventoryUtils.CreativeInventoryItems (same order, same
	// indexes) is only used server-side to resolve craft-creative requests into real items.
	public class CreativeEntryDef
	{
		public int GroupIndex { get; set; }
		public int NetworkId { get; set; }
		public short Metadata { get; set; }
		public int RuntimeId { get; set; }
		public string NbtB64 { get; set; }
	}

	public class CreativeGroupDef
	{
		public int Category { get; set; }
		public string Name { get; set; }
		public string Icon { get; set; }
		// The icon's exact wire identity from the vanilla capture; sent verbatim rather than
		// re-derived through the item factory (which cannot reconstruct it for every icon).
		public int IconNetworkId { get; set; }
		public short IconMetadata { get; set; }
		public int IconRuntimeId { get; set; }
		// Icon extra-data NBT (network little-endian varint bytes, base64), e.g. the enchanted
		// book group icon's stored enchantment. Null for plain icons.
		public string IconNbtB64 { get; set; }
	}

	public static class InventoryUtils
	{
		public static readonly Lazy<CreativeGroupData> CreativeGroups = new Lazy<CreativeGroupData>(() =>
			ResourceUtil.ReadResource<CreativeGroupData>("creative_groups.json", typeof(Items.Item), "Data"));

		public static CreativeItemStacks GetCreativeMetadataSlots()
		{
			CreativeItemStacks slotData = new CreativeItemStacks();
			for (int i = 0; i < CreativeInventoryItems.Count; i++)
			{
				slotData.Add(CreativeInventoryItems[i]);
			}

			return slotData;
		}
		

		// GENERATED CODE. DON'T EDIT BY HAND
		// Exported by MiNET.Client WriteInventoryToFile from a vanilla BDS 1.26.34 session.
		// Entry ids on the wire are positional (index + 1); see SendCreativeInventory and
		// ItemStackInventoryManager.ProcessCraftCreativeAction, which must agree.
		public static List<Item> CreativeInventoryItems = new List<Item>()
		{
			new Item(5, 0, 1){ RuntimeId=1921718966, NetworkId=5, ExtraData = null }, /*minecraft:planks*/
			new Item(-739, 0, 1){ RuntimeId=1613885864, NetworkId=-739, ExtraData = null }, /**/
			new Item(-740, 0, 1){ RuntimeId=-1864524097, NetworkId=-740, ExtraData = null }, /**/
			new Item(-741, 0, 1){ RuntimeId=1113608855, NetworkId=-741, ExtraData = null }, /**/
			new Item(-742, 0, 1){ RuntimeId=-997433644, NetworkId=-742, ExtraData = null }, /**/
			new Item(-743, 0, 1){ RuntimeId=494521430, NetworkId=-743, ExtraData = null }, /**/
			new Item(-486, 0, 1){ RuntimeId=647292747, NetworkId=-486, ExtraData = null }, /**/
			new Item(-537, 0, 1){ RuntimeId=1754553875, NetworkId=-537, ExtraData = null }, /**/
			new Item(-996, 0, 1){ RuntimeId=-353874854, NetworkId=-996, ExtraData = null }, /**/
			new Item(-510, 0, 1){ RuntimeId=-1843072030, NetworkId=-510, ExtraData = null }, /**/
			new Item(-509, 0, 1){ RuntimeId=832568857, NetworkId=-509, ExtraData = null }, /**/
			new Item(-242, 0, 1){ RuntimeId=1967379138, NetworkId=-242, ExtraData = null }, /*minecraft:crimson_planks*/
			new Item(-243, 0, 1){ RuntimeId=1862290605, NetworkId=-243, ExtraData = null }, /*minecraft:warped_planks*/
			new Item(139, 0, 1){ RuntimeId=-261790837, NetworkId=139, ExtraData = null }, /*minecraft:cobblestone_wall*/
			new Item(-971, 0, 1){ RuntimeId=-1139720589, NetworkId=-971, ExtraData = null }, /**/
			new Item(-972, 0, 1){ RuntimeId=-174009265, NetworkId=-972, ExtraData = null }, /**/
			new Item(-973, 0, 1){ RuntimeId=-326321257, NetworkId=-973, ExtraData = null }, /**/
			new Item(-974, 0, 1){ RuntimeId=1863785289, NetworkId=-974, ExtraData = null }, /**/
			new Item(-975, 0, 1){ RuntimeId=-1907247074, NetworkId=-975, ExtraData = null }, /**/
			new Item(-982, 0, 1){ RuntimeId=-1610171762, NetworkId=-982, ExtraData = null }, /**/
			new Item(-977, 0, 1){ RuntimeId=-1966931272, NetworkId=-977, ExtraData = null }, /**/
			new Item(-978, 0, 1){ RuntimeId=-811512368, NetworkId=-978, ExtraData = null }, /**/
			new Item(-976, 0, 1){ RuntimeId=421914268, NetworkId=-976, ExtraData = null }, /**/
			new Item(-979, 0, 1){ RuntimeId=210714624, NetworkId=-979, ExtraData = null }, /**/
			new Item(-983, 0, 1){ RuntimeId=-1785092964, NetworkId=-983, ExtraData = null }, /**/
			new Item(-980, 0, 1){ RuntimeId=1646651022, NetworkId=-980, ExtraData = null }, /**/
			new Item(-981, 0, 1){ RuntimeId=-2043861218, NetworkId=-981, ExtraData = null }, /**/
			new Item(-277, 0, 1){ RuntimeId=453304006, NetworkId=-277, ExtraData = null }, /*minecraft:blackstone_wall*/
			new Item(-297, 0, 1){ RuntimeId=1068714754, NetworkId=-297, ExtraData = null }, /*minecraft:polished_blackstone_wall*/
			new Item(-278, 0, 1){ RuntimeId=-950547642, NetworkId=-278, ExtraData = null }, /*minecraft:polished_blackstone_brick_wall*/
			new Item(-382, 0, 1){ RuntimeId=-973431742, NetworkId=-382, ExtraData = null }, /**/
			new Item(-390, 0, 1){ RuntimeId=948683754, NetworkId=-390, ExtraData = null }, /**/
			new Item(-386, 0, 1){ RuntimeId=1666773026, NetworkId=-386, ExtraData = null }, /**/
			new Item(-394, 0, 1){ RuntimeId=465722050, NetworkId=-394, ExtraData = null }, /**/
			new Item(-747, 0, 1){ RuntimeId=-1951528903, NetworkId=-747, ExtraData = null }, /**/
			new Item(-758, 0, 1){ RuntimeId=-1789937515, NetworkId=-758, ExtraData = null }, /**/
			new Item(-752, 0, 1){ RuntimeId=-1971962955, NetworkId=-752, ExtraData = null }, /**/
			new Item(-481, 0, 1){ RuntimeId=1700199711, NetworkId=-481, ExtraData = null }, /**/
			new Item(-1017, 0, 1){ RuntimeId=2021612440, NetworkId=-1017, ExtraData = null }, /**/
			new Item(-1113, 0, 1){ RuntimeId=172535828, NetworkId=-1113, ExtraData = null }, /**/
			new Item(-1118, 0, 1){ RuntimeId=1617640592, NetworkId=-1118, ExtraData = null }, /**/
			new Item(-1123, 0, 1){ RuntimeId=-42976936, NetworkId=-1123, ExtraData = null }, /**/
			new Item(-1096, 0, 1){ RuntimeId=1535325837, NetworkId=-1096, ExtraData = null }, /**/
			new Item(-1101, 0, 1){ RuntimeId=1247777617, NetworkId=-1101, ExtraData = null }, /**/
			new Item(-1106, 0, 1){ RuntimeId=-28240235, NetworkId=-1106, ExtraData = null }, /**/
			new Item(85, 0, 1){ RuntimeId=1997655867, NetworkId=85, ExtraData = null }, /*minecraft:fence*/
			new Item(-579, 0, 1){ RuntimeId=1246777405, NetworkId=-579, ExtraData = null }, /**/
			new Item(-576, 0, 1){ RuntimeId=-652925544, NetworkId=-576, ExtraData = null }, /**/
			new Item(-578, 0, 1){ RuntimeId=425194876, NetworkId=-578, ExtraData = null }, /**/
			new Item(-575, 0, 1){ RuntimeId=-1128708395, NetworkId=-575, ExtraData = null }, /**/
			new Item(-577, 0, 1){ RuntimeId=329256923, NetworkId=-577, ExtraData = null }, /**/
			new Item(-491, 0, 1){ RuntimeId=-769833176, NetworkId=-491, ExtraData = null }, /**/
			new Item(-532, 0, 1){ RuntimeId=46432752, NetworkId=-532, ExtraData = null }, /**/
			new Item(-991, 0, 1){ RuntimeId=60668747, NetworkId=-991, ExtraData = null }, /**/
			new Item(-515, 0, 1){ RuntimeId=1462261651, NetworkId=-515, ExtraData = null }, /**/
			new Item(113, 0, 1){ RuntimeId=1580000185, NetworkId=113, ExtraData = null }, /*minecraft:nether_brick_fence*/
			new Item(-256, 0, 1){ RuntimeId=-830654345, NetworkId=-256, ExtraData = null }, /*minecraft:crimson_fence*/
			new Item(-257, 0, 1){ RuntimeId=790125098, NetworkId=-257, ExtraData = null }, /*minecraft:warped_fence*/
			new Item(107, 0, 1){ RuntimeId=-1873042880, NetworkId=107, ExtraData = null }, /*minecraft:fence_gate*/
			new Item(183, 0, 1){ RuntimeId=1689239332, NetworkId=183, ExtraData = null }, /*minecraft:spruce_fence_gate*/
			new Item(184, 0, 1){ RuntimeId=1330811813, NetworkId=184, ExtraData = null }, /*minecraft:birch_fence_gate*/
			new Item(185, 0, 1){ RuntimeId=513477517, NetworkId=185, ExtraData = null }, /*minecraft:jungle_fence_gate*/
			new Item(187, 0, 1){ RuntimeId=1912037792, NetworkId=187, ExtraData = null }, /*minecraft:acacia_fence_gate*/
			new Item(186, 0, 1){ RuntimeId=2003036498, NetworkId=186, ExtraData = null }, /*minecraft:dark_oak_fence_gate*/
			new Item(-492, 0, 1){ RuntimeId=-4390855, NetworkId=-492, ExtraData = null }, /**/
			new Item(-533, 0, 1){ RuntimeId=1143272025, NetworkId=-533, ExtraData = null }, /**/
			new Item(-992, 0, 1){ RuntimeId=-419240562, NetworkId=-992, ExtraData = null }, /**/
			new Item(-516, 0, 1){ RuntimeId=1881155758, NetworkId=-516, ExtraData = null }, /**/
			new Item(-258, 0, 1){ RuntimeId=619549542, NetworkId=-258, ExtraData = null }, /*minecraft:crimson_fence_gate*/
			new Item(-259, 0, 1){ RuntimeId=-341062853, NetworkId=-259, ExtraData = null }, /*minecraft:warped_fence_gate*/
			new Item(-180, 0, 1){ RuntimeId=189800222, NetworkId=-180, ExtraData = null }, /*minecraft:normal_stone_stairs*/
			new Item(67, 0, 1){ RuntimeId=-474021321, NetworkId=67, ExtraData = null }, /*minecraft:stone_stairs*/
			new Item(-179, 0, 1){ RuntimeId=430841682, NetworkId=-179, ExtraData = null }, /*minecraft:mossy_cobblestone_stairs*/
			new Item(53, 0, 1){ RuntimeId=-1054044407, NetworkId=53, ExtraData = null }, /*minecraft:oak_stairs*/
			new Item(134, 0, 1){ RuntimeId=925629167, NetworkId=134, ExtraData = null }, /*minecraft:spruce_stairs*/
			new Item(135, 0, 1){ RuntimeId=1888784220, NetworkId=135, ExtraData = null }, /*minecraft:birch_stairs*/
			new Item(136, 0, 1){ RuntimeId=2023214964, NetworkId=136, ExtraData = null }, /*minecraft:jungle_stairs*/
			new Item(163, 0, 1){ RuntimeId=-99866693, NetworkId=163, ExtraData = null }, /*minecraft:acacia_stairs*/
			new Item(164, 0, 1){ RuntimeId=-1604288215, NetworkId=164, ExtraData = null }, /*minecraft:dark_oak_stairs*/
			new Item(-488, 0, 1){ RuntimeId=189821736, NetworkId=-488, ExtraData = null }, /**/
			new Item(-541, 0, 1){ RuntimeId=-173161904, NetworkId=-541, ExtraData = null }, /**/
			new Item(-1000, 0, 1){ RuntimeId=1122325581, NetworkId=-1000, ExtraData = null }, /**/
			new Item(-512, 0, 1){ RuntimeId=907951829, NetworkId=-512, ExtraData = null }, /**/
			new Item(-523, 0, 1){ RuntimeId=-1899527197, NetworkId=-523, ExtraData = null }, /**/
			new Item(109, 0, 1){ RuntimeId=36844083, NetworkId=109, ExtraData = null }, /*minecraft:stone_brick_stairs*/
			new Item(-175, 0, 1){ RuntimeId=-1019587837, NetworkId=-175, ExtraData = null }, /*minecraft:mossy_stone_brick_stairs*/
			new Item(128, 0, 1){ RuntimeId=-120717419, NetworkId=128, ExtraData = null }, /*minecraft:sandstone_stairs*/
			new Item(-177, 0, 1){ RuntimeId=-240771151, NetworkId=-177, ExtraData = null }, /*minecraft:smooth_sandstone_stairs*/
			new Item(180, 0, 1){ RuntimeId=-546470611, NetworkId=180, ExtraData = null }, /*minecraft:red_sandstone_stairs*/
			new Item(-176, 0, 1){ RuntimeId=1060425153, NetworkId=-176, ExtraData = null }, /*minecraft:smooth_red_sandstone_stairs*/
			new Item(-169, 0, 1){ RuntimeId=-704559298, NetworkId=-169, ExtraData = null }, /*minecraft:granite_stairs*/
			new Item(-172, 0, 1){ RuntimeId=-601464526, NetworkId=-172, ExtraData = null }, /*minecraft:polished_granite_stairs*/
			new Item(-170, 0, 1){ RuntimeId=94842602, NetworkId=-170, ExtraData = null }, /*minecraft:diorite_stairs*/
			new Item(-173, 0, 1){ RuntimeId=-1599939218, NetworkId=-173, ExtraData = null }, /*minecraft:polished_diorite_stairs*/
			new Item(-171, 0, 1){ RuntimeId=85088168, NetworkId=-171, ExtraData = null }, /*minecraft:andesite_stairs*/
			new Item(-174, 0, 1){ RuntimeId=1370408852, NetworkId=-174, ExtraData = null }, /*minecraft:polished_andesite_stairs*/
			new Item(108, 0, 1){ RuntimeId=-1618506537, NetworkId=108, ExtraData = null }, /*minecraft:brick_stairs*/
			new Item(114, 0, 1){ RuntimeId=1913555715, NetworkId=114, ExtraData = null }, /*minecraft:nether_brick_stairs*/
			new Item(-184, 0, 1){ RuntimeId=-1274256697, NetworkId=-184, ExtraData = null }, /*minecraft:red_nether_brick_stairs*/
			new Item(-178, 0, 1){ RuntimeId=1520561357, NetworkId=-178, ExtraData = null }, /*minecraft:end_brick_stairs*/
			new Item(156, 0, 1){ RuntimeId=2067288422, NetworkId=156, ExtraData = null }, /*minecraft:quartz_stairs*/
			new Item(-185, 0, 1){ RuntimeId=-921646898, NetworkId=-185, ExtraData = null }, /*minecraft:smooth_quartz_stairs*/
			new Item(203, 0, 1){ RuntimeId=1656552147, NetworkId=203, ExtraData = null }, /*minecraft:purpur_stairs*/
			new Item(-2, 0, 1){ RuntimeId=2019504837, NetworkId=-2, ExtraData = null }, /*minecraft:prismarine_stairs*/
			new Item(-3, 0, 1){ RuntimeId=-1028342827, NetworkId=-3, ExtraData = null }, /*minecraft:dark_prismarine_stairs*/
			new Item(-4, 0, 1){ RuntimeId=-1228929695, NetworkId=-4, ExtraData = null }, /*minecraft:prismarine_bricks_stairs*/
			new Item(-254, 0, 1){ RuntimeId=-801869387, NetworkId=-254, ExtraData = null }, /*minecraft:crimson_stairs*/
			new Item(-255, 0, 1){ RuntimeId=1804931902, NetworkId=-255, ExtraData = null }, /*minecraft:warped_stairs*/
			new Item(-276, 0, 1){ RuntimeId=-427231771, NetworkId=-276, ExtraData = null }, /*minecraft:blackstone_stairs*/
			new Item(-292, 0, 1){ RuntimeId=-1292159207, NetworkId=-292, ExtraData = null }, /*minecraft:polished_blackstone_stairs*/
			new Item(-275, 0, 1){ RuntimeId=1955497857, NetworkId=-275, ExtraData = null }, /*minecraft:polished_blackstone_brick_stairs*/
			new Item(-381, 0, 1){ RuntimeId=-2110181483, NetworkId=-381, ExtraData = null }, /**/
			new Item(-389, 0, 1){ RuntimeId=-1954541935, NetworkId=-389, ExtraData = null }, /**/
			new Item(-385, 0, 1){ RuntimeId=-654748527, NetworkId=-385, ExtraData = null }, /**/
			new Item(-393, 0, 1){ RuntimeId=-1035950027, NetworkId=-393, ExtraData = null }, /**/
			new Item(-746, 0, 1){ RuntimeId=1915927544, NetworkId=-746, ExtraData = null }, /**/
			new Item(-751, 0, 1){ RuntimeId=-535512436, NetworkId=-751, ExtraData = null }, /**/
			new Item(-757, 0, 1){ RuntimeId=-1960589488, NetworkId=-757, ExtraData = null }, /**/
			new Item(-480, 0, 1){ RuntimeId=-857869146, NetworkId=-480, ExtraData = null }, /**/
			new Item(-354, 0, 1){ RuntimeId=-278926471, NetworkId=-354, ExtraData = null }, /**/
			new Item(-355, 0, 1){ RuntimeId=-579014752, NetworkId=-355, ExtraData = null }, /**/
			new Item(-356, 0, 1){ RuntimeId=-28976151, NetworkId=-356, ExtraData = null }, /**/
			new Item(-357, 0, 1){ RuntimeId=-1648158567, NetworkId=-357, ExtraData = null }, /**/
			new Item(-358, 0, 1){ RuntimeId=-1465349479, NetworkId=-358, ExtraData = null }, /**/
			new Item(-359, 0, 1){ RuntimeId=1324615712, NetworkId=-359, ExtraData = null }, /**/
			new Item(-360, 0, 1){ RuntimeId=1528655993, NetworkId=-360, ExtraData = null }, /**/
			new Item(-448, 0, 1){ RuntimeId=-822934175, NetworkId=-448, ExtraData = null }, /**/
			new Item(-1016, 0, 1){ RuntimeId=234418979, NetworkId=-1016, ExtraData = null }, /**/
			new Item(-1112, 0, 1){ RuntimeId=1526717471, NetworkId=-1112, ExtraData = null }, /**/
			new Item(-1117, 0, 1){ RuntimeId=-495344765, NetworkId=-1117, ExtraData = null }, /**/
			new Item(-1122, 0, 1){ RuntimeId=-1315792945, NetworkId=-1122, ExtraData = null }, /**/
			new Item(-1095, 0, 1){ RuntimeId=-305938344, NetworkId=-1095, ExtraData = null }, /**/
			new Item(-1100, 0, 1){ RuntimeId=1265606948, NetworkId=-1100, ExtraData = null }, /**/
			new Item(-1105, 0, 1){ RuntimeId=1606647052, NetworkId=-1105, ExtraData = null }, /**/
			new Item(324, 0, 1){ RuntimeId=0, NetworkId=391, ExtraData = null }, /*minecraft:wooden_door*/
			new Item(427, 0, 1){ RuntimeId=0, NetworkId=592, ExtraData = null }, /*minecraft:spruce_door*/
			new Item(428, 0, 1){ RuntimeId=0, NetworkId=593, ExtraData = null }, /*minecraft:birch_door*/
			new Item(429, 0, 1){ RuntimeId=0, NetworkId=594, ExtraData = null }, /*minecraft:jungle_door*/
			new Item(430, 0, 1){ RuntimeId=0, NetworkId=595, ExtraData = null }, /*minecraft:acacia_door*/
			new Item(431, 0, 1){ RuntimeId=0, NetworkId=596, ExtraData = null }, /*minecraft:dark_oak_door*/
			new Item(675, 0, 1){ RuntimeId=0, NetworkId=675, ExtraData = null }, /**/
			new Item(-531, 0, 1){ RuntimeId=0, NetworkId=-531, ExtraData = null }, /**/
			new Item(-990, 0, 1){ RuntimeId=0, NetworkId=-990, ExtraData = null }, /**/
			new Item(-517, 0, 1){ RuntimeId=0, NetworkId=-517, ExtraData = null }, /**/
			new Item(330, 0, 1){ RuntimeId=0, NetworkId=404, ExtraData = null }, /*minecraft:iron_door*/
			new Item(755, 0, 1){ RuntimeId=0, NetworkId=659, ExtraData = null }, /*minecraft:crimson_door*/
			new Item(756, 0, 1){ RuntimeId=0, NetworkId=660, ExtraData = null }, /*minecraft:warped_door*/
			new Item(-784, 0, 1){ RuntimeId=0, NetworkId=-784, ExtraData = null }, /**/
			new Item(-785, 0, 1){ RuntimeId=0, NetworkId=-785, ExtraData = null }, /**/
			new Item(-786, 0, 1){ RuntimeId=0, NetworkId=-786, ExtraData = null }, /**/
			new Item(-787, 0, 1){ RuntimeId=0, NetworkId=-787, ExtraData = null }, /**/
			new Item(-788, 0, 1){ RuntimeId=0, NetworkId=-788, ExtraData = null }, /**/
			new Item(-789, 0, 1){ RuntimeId=0, NetworkId=-789, ExtraData = null }, /**/
			new Item(-790, 0, 1){ RuntimeId=0, NetworkId=-790, ExtraData = null }, /**/
			new Item(-791, 0, 1){ RuntimeId=0, NetworkId=-791, ExtraData = null }, /**/
			new Item(96, 0, 1){ RuntimeId=-1877593911, NetworkId=96, ExtraData = null }, /*minecraft:trapdoor*/
			new Item(-149, 0, 1){ RuntimeId=-1378553103, NetworkId=-149, ExtraData = null }, /*minecraft:spruce_trapdoor*/
			new Item(-146, 0, 1){ RuntimeId=2122552696, NetworkId=-146, ExtraData = null }, /*minecraft:birch_trapdoor*/
			new Item(-148, 0, 1){ RuntimeId=-1703102900, NetworkId=-148, ExtraData = null }, /*minecraft:jungle_trapdoor*/
			new Item(-145, 0, 1){ RuntimeId=1341187041, NetworkId=-145, ExtraData = null }, /*minecraft:acacia_trapdoor*/
			new Item(-147, 0, 1){ RuntimeId=1330091683, NetworkId=-147, ExtraData = null }, /*minecraft:dark_oak_trapdoor*/
			new Item(-496, 0, 1){ RuntimeId=-991535216, NetworkId=-496, ExtraData = null }, /**/
			new Item(-543, 0, 1){ RuntimeId=-663208992, NetworkId=-543, ExtraData = null }, /**/
			new Item(-1002, 0, 1){ RuntimeId=1314457687, NetworkId=-1002, ExtraData = null }, /**/
			new Item(-520, 0, 1){ RuntimeId=1061149627, NetworkId=-520, ExtraData = null }, /**/
			new Item(167, 0, 1){ RuntimeId=2136558063, NetworkId=167, ExtraData = null }, /*minecraft:iron_trapdoor*/
			new Item(-246, 0, 1){ RuntimeId=365333515, NetworkId=-246, ExtraData = null }, /*minecraft:crimson_trapdoor*/
			new Item(-247, 0, 1){ RuntimeId=385782130, NetworkId=-247, ExtraData = null }, /*minecraft:warped_trapdoor*/
			new Item(-792, 0, 1){ RuntimeId=826714304, NetworkId=-792, ExtraData = null }, /**/
			new Item(-793, 0, 1){ RuntimeId=637942835, NetworkId=-793, ExtraData = null }, /**/
			new Item(-794, 0, 1){ RuntimeId=575336420, NetworkId=-794, ExtraData = null }, /**/
			new Item(-795, 0, 1){ RuntimeId=-1630510344, NetworkId=-795, ExtraData = null }, /**/
			new Item(-796, 0, 1){ RuntimeId=-479807600, NetworkId=-796, ExtraData = null }, /**/
			new Item(-797, 0, 1){ RuntimeId=432104899, NetworkId=-797, ExtraData = null }, /**/
			new Item(-798, 0, 1){ RuntimeId=-1823598236, NetworkId=-798, ExtraData = null }, /**/
			new Item(-799, 0, 1){ RuntimeId=801440824, NetworkId=-799, ExtraData = null }, /**/
			new Item(101, 0, 1){ RuntimeId=-1844999203, NetworkId=101, ExtraData = null }, /*minecraft:iron_bars*/
			new Item(-1066, 0, 1){ RuntimeId=-1215417744, NetworkId=-1066, ExtraData = null }, /**/
			new Item(-1067, 0, 1){ RuntimeId=519234921, NetworkId=-1067, ExtraData = null }, /**/
			new Item(-1068, 0, 1){ RuntimeId=1928613356, NetworkId=-1068, ExtraData = null }, /**/
			new Item(-1069, 0, 1){ RuntimeId=-2043766760, NetworkId=-1069, ExtraData = null }, /**/
			new Item(-1070, 0, 1){ RuntimeId=1831984032, NetworkId=-1070, ExtraData = null }, /**/
			new Item(-1071, 0, 1){ RuntimeId=1767308409, NetworkId=-1071, ExtraData = null }, /**/
			new Item(-1072, 0, 1){ RuntimeId=-144774100, NetworkId=-1072, ExtraData = null }, /**/
			new Item(-1073, 0, 1){ RuntimeId=35199432, NetworkId=-1073, ExtraData = null }, /**/
			new Item(20, 0, 1){ RuntimeId=927668178, NetworkId=20, ExtraData = null }, /*minecraft:glass*/
			new Item(241, 0, 1){ RuntimeId=-736463769, NetworkId=241, ExtraData = null }, /*minecraft:stained_glass*/
			new Item(-680, 0, 1){ RuntimeId=646116939, NetworkId=-680, ExtraData = null }, /**/
			new Item(-679, 0, 1){ RuntimeId=-1252645318, NetworkId=-679, ExtraData = null }, /**/
			new Item(-687, 0, 1){ RuntimeId=165342613, NetworkId=-687, ExtraData = null }, /**/
			new Item(-684, 0, 1){ RuntimeId=-447755188, NetworkId=-684, ExtraData = null }, /**/
			new Item(-686, 0, 1){ RuntimeId=1491455451, NetworkId=-686, ExtraData = null }, /**/
			new Item(-673, 0, 1){ RuntimeId=-2052812709, NetworkId=-673, ExtraData = null }, /**/
			new Item(-676, 0, 1){ RuntimeId=1576194193, NetworkId=-676, ExtraData = null }, /**/
			new Item(-677, 0, 1){ RuntimeId=-1779214532, NetworkId=-677, ExtraData = null }, /**/
			new Item(-685, 0, 1){ RuntimeId=-1004990747, NetworkId=-685, ExtraData = null }, /**/
			new Item(-681, 0, 1){ RuntimeId=2073359842, NetworkId=-681, ExtraData = null }, /**/
			new Item(-675, 0, 1){ RuntimeId=1387983062, NetworkId=-675, ExtraData = null }, /**/
			new Item(-683, 0, 1){ RuntimeId=1552706627, NetworkId=-683, ExtraData = null }, /**/
			new Item(-682, 0, 1){ RuntimeId=-971383025, NetworkId=-682, ExtraData = null }, /**/
			new Item(-674, 0, 1){ RuntimeId=437625455, NetworkId=-674, ExtraData = null }, /**/
			new Item(-678, 0, 1){ RuntimeId=665732263, NetworkId=-678, ExtraData = null }, /**/
			new Item(-334, 0, 1){ RuntimeId=2136584036, NetworkId=-334, ExtraData = null }, /**/
			new Item(102, 0, 1){ RuntimeId=1848427078, NetworkId=102, ExtraData = null }, /*minecraft:glass_pane*/
			new Item(160, 0, 1){ RuntimeId=922495761, NetworkId=160, ExtraData = null }, /*minecraft:stained_glass_pane*/
			new Item(-650, 0, 1){ RuntimeId=1450826485, NetworkId=-650, ExtraData = null }, /**/
			new Item(-649, 0, 1){ RuntimeId=-410078822, NetworkId=-649, ExtraData = null }, /**/
			new Item(-657, 0, 1){ RuntimeId=-2063925701, NetworkId=-657, ExtraData = null }, /**/
			new Item(-654, 0, 1){ RuntimeId=1278213580, NetworkId=-654, ExtraData = null }, /**/
			new Item(-656, 0, 1){ RuntimeId=-433812479, NetworkId=-656, ExtraData = null }, /**/
			new Item(-643, 0, 1){ RuntimeId=-1179189331, NetworkId=-643, ExtraData = null }, /**/
			new Item(-646, 0, 1){ RuntimeId=-1157348137, NetworkId=-646, ExtraData = null }, /**/
			new Item(-647, 0, 1){ RuntimeId=-2098753440, NetworkId=-647, ExtraData = null }, /**/
			new Item(-655, 0, 1){ RuntimeId=445359987, NetworkId=-655, ExtraData = null }, /**/
			new Item(-651, 0, 1){ RuntimeId=2035589458, NetworkId=-651, ExtraData = null }, /**/
			new Item(-645, 0, 1){ RuntimeId=834202646, NetworkId=-645, ExtraData = null }, /**/
			new Item(-653, 0, 1){ RuntimeId=1347150037, NetworkId=-653, ExtraData = null }, /**/
			new Item(-652, 0, 1){ RuntimeId=-1177374323, NetworkId=-652, ExtraData = null }, /**/
			new Item(-644, 0, 1){ RuntimeId=-325995691, NetworkId=-644, ExtraData = null }, /**/
			new Item(-648, 0, 1){ RuntimeId=-948560095, NetworkId=-648, ExtraData = null }, /**/
			new Item(65, 0, 1){ RuntimeId=1540239144, NetworkId=65, ExtraData = null }, /*minecraft:ladder*/
			new Item(-165, 0, 1){ RuntimeId=-1796842225, NetworkId=-165, ExtraData = null }, /*minecraft:scaffolding*/
			new Item(45, 0, 1){ RuntimeId=1184293289, NetworkId=45, ExtraData = null }, /*minecraft:brick_block*/
			new Item(44, 0, 1){ RuntimeId=98367658, NetworkId=44, ExtraData = null }, /*minecraft:stone_slab*/
			new Item(-899, 0, 1){ RuntimeId=1172986891, NetworkId=-899, ExtraData = null }, /**/
			new Item(-873, 0, 1){ RuntimeId=-165143741, NetworkId=-873, ExtraData = null }, /**/
			new Item(-888, 0, 1){ RuntimeId=102958971, NetworkId=-888, ExtraData = null }, /**/
			new Item(158, 0, 1){ RuntimeId=-1547796892, NetworkId=158, ExtraData = null }, /*minecraft:wooden_slab*/
			new Item(-804, 0, 1){ RuntimeId=1636743342, NetworkId=-804, ExtraData = null }, /**/
			new Item(-805, 0, 1){ RuntimeId=-220706847, NetworkId=-805, ExtraData = null }, /**/
			new Item(-806, 0, 1){ RuntimeId=-2088068891, NetworkId=-806, ExtraData = null }, /**/
			new Item(-807, 0, 1){ RuntimeId=1549458206, NetworkId=-807, ExtraData = null }, /**/
			new Item(-808, 0, 1){ RuntimeId=-231420852, NetworkId=-808, ExtraData = null }, /**/
			new Item(-489, 0, 1){ RuntimeId=1917708593, NetworkId=-489, ExtraData = null }, /**/
			new Item(-539, 0, 1){ RuntimeId=1701598681, NetworkId=-539, ExtraData = null }, /**/
			new Item(-998, 0, 1){ RuntimeId=653744680, NetworkId=-998, ExtraData = null }, /**/
			new Item(-513, 0, 1){ RuntimeId=-1212928172, NetworkId=-513, ExtraData = null }, /**/
			new Item(-524, 0, 1){ RuntimeId=1394399714, NetworkId=-524, ExtraData = null }, /**/
			new Item(-875, 0, 1){ RuntimeId=1523363922, NetworkId=-875, ExtraData = null }, /**/
			new Item(-166, 0, 1){ RuntimeId=-1903915334, NetworkId=-166, ExtraData = null }, /*minecraft:stone_slab4*/
			new Item(-872, 0, 1){ RuntimeId=-1692991724, NetworkId=-872, ExtraData = null }, /**/
			new Item(-900, 0, 1){ RuntimeId=-2031145349, NetworkId=-900, ExtraData = null }, /**/
			new Item(-889, 0, 1){ RuntimeId=-37707180, NetworkId=-889, ExtraData = null }, /**/
			new Item(182, 0, 1){ RuntimeId=1823406212, NetworkId=182, ExtraData = null }, /*minecraft:stone_slab2*/
			new Item(-901, 0, 1){ RuntimeId=-502587217, NetworkId=-901, ExtraData = null }, /**/
			new Item(-891, 0, 1){ RuntimeId=163341908, NetworkId=-891, ExtraData = null }, /**/
			new Item(-896, 0, 1){ RuntimeId=-814413689, NetworkId=-896, ExtraData = null }, /**/
			new Item(-897, 0, 1){ RuntimeId=1472925995, NetworkId=-897, ExtraData = null }, /**/
			new Item(-894, 0, 1){ RuntimeId=-624980193, NetworkId=-894, ExtraData = null }, /**/
			new Item(-895, 0, 1){ RuntimeId=-1944243117, NetworkId=-895, ExtraData = null }, /**/
			new Item(-893, 0, 1){ RuntimeId=1480285677, NetworkId=-893, ExtraData = null }, /**/
			new Item(-892, 0, 1){ RuntimeId=1169142593, NetworkId=-892, ExtraData = null }, /**/
			new Item(-874, 0, 1){ RuntimeId=1638596166, NetworkId=-874, ExtraData = null }, /**/
			new Item(-877, 0, 1){ RuntimeId=-707662934, NetworkId=-877, ExtraData = null }, /**/
			new Item(-890, 0, 1){ RuntimeId=1891256390, NetworkId=-890, ExtraData = null }, /**/
			new Item(-162, 0, 1){ RuntimeId=-7922556, NetworkId=-162, ExtraData = null }, /*minecraft:stone_slab3*/
			new Item(-876, 0, 1){ RuntimeId=1412851871, NetworkId=-876, ExtraData = null }, /**/
			new Item(-898, 0, 1){ RuntimeId=-1677974445, NetworkId=-898, ExtraData = null }, /**/
			new Item(-884, 0, 1){ RuntimeId=-434485178, NetworkId=-884, ExtraData = null }, /**/
			new Item(-885, 0, 1){ RuntimeId=476501012, NetworkId=-885, ExtraData = null }, /**/
			new Item(-886, 0, 1){ RuntimeId=1463596084, NetworkId=-886, ExtraData = null }, /**/
			new Item(-887, 0, 1){ RuntimeId=1405120576, NetworkId=-887, ExtraData = null }, /**/
			new Item(-264, 0, 1){ RuntimeId=1615941900, NetworkId=-264, ExtraData = null }, /*minecraft:crimson_slab*/
			new Item(-265, 0, 1){ RuntimeId=-726324521, NetworkId=-265, ExtraData = null }, /*minecraft:warped_slab*/
			new Item(-282, 0, 1){ RuntimeId=1007596908, NetworkId=-282, ExtraData = null }, /*minecraft:blackstone_slab*/
			new Item(-293, 0, 1){ RuntimeId=1919809176, NetworkId=-293, ExtraData = null }, /*minecraft:polished_blackstone_slab*/
			new Item(-284, 0, 1){ RuntimeId=-564160788, NetworkId=-284, ExtraData = null }, /*minecraft:polished_blackstone_brick_slab*/
			new Item(-380, 0, 1){ RuntimeId=-108723496, NetworkId=-380, ExtraData = null }, /**/
			new Item(-384, 0, 1){ RuntimeId=808632, NetworkId=-384, ExtraData = null }, /**/
			new Item(-388, 0, 1){ RuntimeId=1118934320, NetworkId=-388, ExtraData = null }, /**/
			new Item(-392, 0, 1){ RuntimeId=1781254744, NetworkId=-392, ExtraData = null }, /**/
			new Item(-744, 0, 1){ RuntimeId=-1102029347, NetworkId=-744, ExtraData = null }, /**/
			new Item(-749, 0, 1){ RuntimeId=1844300041, NetworkId=-749, ExtraData = null }, /**/
			new Item(-755, 0, 1){ RuntimeId=390504169, NetworkId=-755, ExtraData = null }, /**/
			new Item(-478, 0, 1){ RuntimeId=870812055, NetworkId=-478, ExtraData = null }, /**/
			new Item(-361, 0, 1){ RuntimeId=2077943524, NetworkId=-361, ExtraData = null }, /**/
			new Item(-362, 0, 1){ RuntimeId=794380717, NetworkId=-362, ExtraData = null }, /**/
			new Item(-363, 0, 1){ RuntimeId=-492790496, NetworkId=-363, ExtraData = null }, /**/
			new Item(-364, 0, 1){ RuntimeId=407487948, NetworkId=-364, ExtraData = null }, /**/
			new Item(-365, 0, 1){ RuntimeId=817643860, NetworkId=-365, ExtraData = null }, /**/
			new Item(-366, 0, 1){ RuntimeId=1318859037, NetworkId=-366, ExtraData = null }, /**/
			new Item(-367, 0, 1){ RuntimeId=-1759566432, NetworkId=-367, ExtraData = null }, /**/
			new Item(-449, 0, 1){ RuntimeId=1453091980, NetworkId=-449, ExtraData = null }, /**/
			new Item(-1014, 0, 1){ RuntimeId=45026994, NetworkId=-1014, ExtraData = null }, /**/
			new Item(-1110, 0, 1){ RuntimeId=-1483570994, NetworkId=-1110, ExtraData = null }, /**/
			new Item(-1115, 0, 1){ RuntimeId=-1995175302, NetworkId=-1115, ExtraData = null }, /**/
			new Item(-1120, 0, 1){ RuntimeId=-1701827726, NetworkId=-1120, ExtraData = null }, /**/
			new Item(-1093, 0, 1){ RuntimeId=1483012209, NetworkId=-1093, ExtraData = null }, /**/
			new Item(-1098, 0, 1){ RuntimeId=-1732558811, NetworkId=-1098, ExtraData = null }, /**/
			new Item(-1103, 0, 1){ RuntimeId=1559349481, NetworkId=-1103, ExtraData = null }, /**/
			new Item(98, 0, 1){ RuntimeId=1972208509, NetworkId=98, ExtraData = null }, /*minecraft:stonebrick*/
			new Item(-868, 0, 1){ RuntimeId=-1341221187, NetworkId=-868, ExtraData = null }, /**/
			new Item(-869, 0, 1){ RuntimeId=194928973, NetworkId=-869, ExtraData = null }, /**/
			new Item(-870, 0, 1){ RuntimeId=1117720546, NetworkId=-870, ExtraData = null }, /**/
			new Item(-183, 0, 1){ RuntimeId=555751865, NetworkId=-183, ExtraData = null }, /*minecraft:smooth_stone*/
			new Item(206, 0, 1){ RuntimeId=-1590117313, NetworkId=206, ExtraData = null }, /*minecraft:end_bricks*/
			new Item(-274, 0, 1){ RuntimeId=-547680429, NetworkId=-274, ExtraData = null }, /*minecraft:polished_blackstone_bricks*/
			new Item(-280, 0, 1){ RuntimeId=1481921079, NetworkId=-280, ExtraData = null }, /*minecraft:cracked_polished_blackstone_bricks*/
			new Item(-281, 0, 1){ RuntimeId=1394655156, NetworkId=-281, ExtraData = null }, /*minecraft:gilded_blackstone*/
			new Item(-279, 0, 1){ RuntimeId=-157993866, NetworkId=-279, ExtraData = null }, /*minecraft:chiseled_polished_blackstone*/
			new Item(-387, 0, 1){ RuntimeId=385861263, NetworkId=-387, ExtraData = null }, /**/
			new Item(-409, 0, 1){ RuntimeId=976881179, NetworkId=-409, ExtraData = null }, /**/
			new Item(-391, 0, 1){ RuntimeId=-814686073, NetworkId=-391, ExtraData = null }, /**/
			new Item(-754, 0, 1){ RuntimeId=-1048049174, NetworkId=-754, ExtraData = null }, /**/
			new Item(-410, 0, 1){ RuntimeId=339494639, NetworkId=-410, ExtraData = null }, /**/
			new Item(-395, 0, 1){ RuntimeId=1092855370, NetworkId=-395, ExtraData = null }, /**/
			new Item(-753, 0, 1){ RuntimeId=-1566362421, NetworkId=-753, ExtraData = null }, /**/
			new Item(-759, 0, 1){ RuntimeId=50098331, NetworkId=-759, ExtraData = null }, /**/
			new Item(-1119, 0, 1){ RuntimeId=258074389, NetworkId=-1119, ExtraData = null }, /**/
			new Item(-1124, 0, 1){ RuntimeId=-515449382, NetworkId=-1124, ExtraData = null }, /**/
			new Item(-1102, 0, 1){ RuntimeId=2099693770, NetworkId=-1102, ExtraData = null }, /**/
			new Item(-1107, 0, 1){ RuntimeId=2040895447, NetworkId=-1107, ExtraData = null }, /**/
			new Item(4, 0, 1){ RuntimeId=1741778478, NetworkId=4, ExtraData = null }, /*minecraft:cobblestone*/
			new Item(48, 0, 1){ RuntimeId=-735275266, NetworkId=48, ExtraData = null }, /*minecraft:mossy_cobblestone*/
			new Item(-379, 0, 1){ RuntimeId=-119993289, NetworkId=-379, ExtraData = null }, /**/
			new Item(24, 0, 1){ RuntimeId=-1540286469, NetworkId=24, ExtraData = null }, /*minecraft:sandstone*/
			new Item(-944, 0, 1){ RuntimeId=506739042, NetworkId=-944, ExtraData = null }, /**/
			new Item(-945, 0, 1){ RuntimeId=297338022, NetworkId=-945, ExtraData = null }, /**/
			new Item(-946, 0, 1){ RuntimeId=-734176589, NetworkId=-946, ExtraData = null }, /**/
			new Item(179, 0, 1){ RuntimeId=1502365015, NetworkId=179, ExtraData = null }, /*minecraft:red_sandstone*/
			new Item(-956, 0, 1){ RuntimeId=1326268410, NetworkId=-956, ExtraData = null }, /**/
			new Item(-957, 0, 1){ RuntimeId=1019854814, NetworkId=-957, ExtraData = null }, /**/
			new Item(-958, 0, 1){ RuntimeId=1329798895, NetworkId=-958, ExtraData = null }, /**/
			new Item(173, 0, 1){ RuntimeId=2126838376, NetworkId=173, ExtraData = null }, /*minecraft:coal_block*/
			new Item(-139, 0, 1){ RuntimeId=-282458096, NetworkId=-139, ExtraData = null }, /*minecraft:dried_kelp_block*/
			new Item(-340, 0, 1){ RuntimeId=-593390456, NetworkId=-340, ExtraData = null }, /**/
			new Item(-341, 0, 1){ RuntimeId=600140015, NetworkId=-341, ExtraData = null }, /**/
			new Item(-342, 0, 1){ RuntimeId=1229534000, NetworkId=-342, ExtraData = null }, /**/
			new Item(-343, 0, 1){ RuntimeId=-1242444440, NetworkId=-343, ExtraData = null }, /**/
			new Item(-344, 0, 1){ RuntimeId=-36704888, NetworkId=-344, ExtraData = null }, /**/
			new Item(-345, 0, 1){ RuntimeId=1496465303, NetworkId=-345, ExtraData = null }, /**/
			new Item(-346, 0, 1){ RuntimeId=-38144880, NetworkId=-346, ExtraData = null }, /**/
			new Item(-446, 0, 1){ RuntimeId=-1317255528, NetworkId=-446, ExtraData = null }, /**/
			new Item(-768, 0, 1){ RuntimeId=596285486, NetworkId=-768, ExtraData = null }, /**/
			new Item(-769, 0, 1){ RuntimeId=-1263973663, NetworkId=-769, ExtraData = null }, /**/
			new Item(-770, 0, 1){ RuntimeId=1315604322, NetworkId=-770, ExtraData = null }, /**/
			new Item(-771, 0, 1){ RuntimeId=1903310502, NetworkId=-771, ExtraData = null }, /**/
			new Item(-772, 0, 1){ RuntimeId=1619208318, NetworkId=-772, ExtraData = null }, /**/
			new Item(-773, 0, 1){ RuntimeId=1125768945, NetworkId=-773, ExtraData = null }, /**/
			new Item(-774, 0, 1){ RuntimeId=-1251980246, NetworkId=-774, ExtraData = null }, /**/
			new Item(-775, 0, 1){ RuntimeId=1235641318, NetworkId=-775, ExtraData = null }, /**/
			new Item(-347, 0, 1){ RuntimeId=1478169831, NetworkId=-347, ExtraData = null }, /**/
			new Item(-348, 0, 1){ RuntimeId=1152328132, NetworkId=-348, ExtraData = null }, /**/
			new Item(-349, 0, 1){ RuntimeId=-1172813825, NetworkId=-349, ExtraData = null }, /**/
			new Item(-350, 0, 1){ RuntimeId=-1144556273, NetworkId=-350, ExtraData = null }, /**/
			new Item(-351, 0, 1){ RuntimeId=2099034927, NetworkId=-351, ExtraData = null }, /**/
			new Item(-352, 0, 1){ RuntimeId=1193195900, NetworkId=-352, ExtraData = null }, /**/
			new Item(-353, 0, 1){ RuntimeId=1426268431, NetworkId=-353, ExtraData = null }, /**/
			new Item(-447, 0, 1){ RuntimeId=36290879, NetworkId=-447, ExtraData = null }, /**/
			new Item(-760, 0, 1){ RuntimeId=-1178423945, NetworkId=-760, ExtraData = null }, /**/
			new Item(-761, 0, 1){ RuntimeId=1071398502, NetworkId=-761, ExtraData = null }, /**/
			new Item(-762, 0, 1){ RuntimeId=2100169659, NetworkId=-762, ExtraData = null }, /**/
			new Item(-763, 0, 1){ RuntimeId=797802271, NetworkId=-763, ExtraData = null }, /**/
			new Item(-764, 0, 1){ RuntimeId=2075396615, NetworkId=-764, ExtraData = null }, /**/
			new Item(-765, 0, 1){ RuntimeId=549128054, NetworkId=-765, ExtraData = null }, /**/
			new Item(-766, 0, 1){ RuntimeId=-1157960737, NetworkId=-766, ExtraData = null }, /**/
			new Item(-767, 0, 1){ RuntimeId=-488767429, NetworkId=-767, ExtraData = null }, /**/
			new Item(-776, 0, 1){ RuntimeId=40859174, NetworkId=-776, ExtraData = null }, /**/
			new Item(-777, 0, 1){ RuntimeId=-2033617845, NetworkId=-777, ExtraData = null }, /**/
			new Item(-778, 0, 1){ RuntimeId=-1261143798, NetworkId=-778, ExtraData = null }, /**/
			new Item(-779, 0, 1){ RuntimeId=16042046, NetworkId=-779, ExtraData = null }, /**/
			new Item(-780, 0, 1){ RuntimeId=-2045350410, NetworkId=-780, ExtraData = null }, /**/
			new Item(-781, 0, 1){ RuntimeId=1508731, NetworkId=-781, ExtraData = null }, /**/
			new Item(-782, 0, 1){ RuntimeId=-1163775926, NetworkId=-782, ExtraData = null }, /**/
			new Item(-783, 0, 1){ RuntimeId=1752871630, NetworkId=-783, ExtraData = null }, /**/
			new Item(42, 0, 1){ RuntimeId=-939070369, NetworkId=42, ExtraData = null }, /*minecraft:iron_block*/
			new Item(41, 0, 1){ RuntimeId=1549804739, NetworkId=41, ExtraData = null }, /*minecraft:gold_block*/
			new Item(133, 0, 1){ RuntimeId=770369380, NetworkId=133, ExtraData = null }, /*minecraft:emerald_block*/
			new Item(57, 0, 1){ RuntimeId=1460042000, NetworkId=57, ExtraData = null }, /*minecraft:diamond_block*/
			new Item(22, 0, 1){ RuntimeId=78929077, NetworkId=22, ExtraData = null }, /*minecraft:lapis_block*/
			new Item(-452, 0, 1){ RuntimeId=1063959733, NetworkId=-452, ExtraData = null }, /**/
			new Item(-451, 0, 1){ RuntimeId=1077242782, NetworkId=-451, ExtraData = null }, /**/
			new Item(-453, 0, 1){ RuntimeId=-2126961618, NetworkId=-453, ExtraData = null }, /**/
			new Item(155, 0, 1){ RuntimeId=1808046669, NetworkId=155, ExtraData = null }, /*minecraft:quartz_block*/
			new Item(-304, 0, 1){ RuntimeId=-1961826404, NetworkId=-304, ExtraData = null }, /*minecraft:quartz_bricks*/
			new Item(-954, 0, 1){ RuntimeId=-882554691, NetworkId=-954, ExtraData = null }, /**/
			new Item(-953, 0, 1){ RuntimeId=-841862564, NetworkId=-953, ExtraData = null }, /**/
			new Item(-955, 0, 1){ RuntimeId=906320597, NetworkId=-955, ExtraData = null }, /**/
			new Item(168, 0, 1){ RuntimeId=-1243235801, NetworkId=168, ExtraData = null }, /*minecraft:prismarine*/
			new Item(-948, 0, 1){ RuntimeId=-1669203825, NetworkId=-948, ExtraData = null }, /**/
			new Item(-947, 0, 1){ RuntimeId=-1915731833, NetworkId=-947, ExtraData = null }, /**/
			new Item(165, 0, 1){ RuntimeId=-858454146, NetworkId=165, ExtraData = null }, /*minecraft:slime*/
			new Item(-220, 0, 1){ RuntimeId=1517479843, NetworkId=-220, ExtraData = null }, /*minecraft:honey_block*/
			new Item(-221, 0, 1){ RuntimeId=2128784556, NetworkId=-221, ExtraData = null }, /*minecraft:honeycomb_block*/
			new Item(-1021, 0, 1){ RuntimeId=330851185, NetworkId=-1021, ExtraData = null }, /**/
			new Item(170, 0, 1){ RuntimeId=1514836139, NetworkId=170, ExtraData = null }, /*minecraft:hay_block*/
			new Item(216, 0, 1){ RuntimeId=279386582, NetworkId=216, ExtraData = null }, /*minecraft:bone_block*/
			new Item(-1013, 0, 1){ RuntimeId=2086713373, NetworkId=-1013, ExtraData = null }, /**/
			new Item(-1020, 0, 1){ RuntimeId=383663638, NetworkId=-1020, ExtraData = null }, /**/
			new Item(112, 0, 1){ RuntimeId=1523459785, NetworkId=112, ExtraData = null }, /*minecraft:nether_brick*/
			new Item(215, 0, 1){ RuntimeId=408777833, NetworkId=215, ExtraData = null }, /*minecraft:red_nether_brick*/
			new Item(-302, 0, 1){ RuntimeId=-1818276676, NetworkId=-302, ExtraData = null }, /*minecraft:chiseled_nether_bricks*/
			new Item(-303, 0, 1){ RuntimeId=1014633093, NetworkId=-303, ExtraData = null }, /*minecraft:cracked_nether_bricks*/
			new Item(-270, 0, 1){ RuntimeId=-1382353780, NetworkId=-270, ExtraData = null }, /*minecraft:netherite_block*/
			new Item(-222, 0, 1){ RuntimeId=-1945640889, NetworkId=-222, ExtraData = null }, /*minecraft:lodestone*/
			new Item(35, 0, 1){ RuntimeId=-1869155698, NetworkId=35, ExtraData = null }, /*minecraft:wool*/
			new Item(-552, 0, 1){ RuntimeId=-2130077302, NetworkId=-552, ExtraData = null }, /**/
			new Item(-553, 0, 1){ RuntimeId=2029530197, NetworkId=-553, ExtraData = null }, /**/
			new Item(-554, 0, 1){ RuntimeId=1558473812, NetworkId=-554, ExtraData = null }, /**/
			new Item(-555, 0, 1){ RuntimeId=-52932765, NetworkId=-555, ExtraData = null }, /**/
			new Item(-556, 0, 1){ RuntimeId=1885700022, NetworkId=-556, ExtraData = null }, /**/
			new Item(-557, 0, 1){ RuntimeId=-1011696902, NetworkId=-557, ExtraData = null }, /**/
			new Item(-558, 0, 1){ RuntimeId=1549901968, NetworkId=-558, ExtraData = null }, /**/
			new Item(-559, 0, 1){ RuntimeId=1776245615, NetworkId=-559, ExtraData = null }, /**/
			new Item(-560, 0, 1){ RuntimeId=-258297172, NetworkId=-560, ExtraData = null }, /**/
			new Item(-561, 0, 1){ RuntimeId=-1207885395, NetworkId=-561, ExtraData = null }, /**/
			new Item(-562, 0, 1){ RuntimeId=-401430339, NetworkId=-562, ExtraData = null }, /**/
			new Item(-563, 0, 1){ RuntimeId=1416924150, NetworkId=-563, ExtraData = null }, /**/
			new Item(-564, 0, 1){ RuntimeId=890413206, NetworkId=-564, ExtraData = null }, /**/
			new Item(-565, 0, 1){ RuntimeId=882906254, NetworkId=-565, ExtraData = null }, /**/
			new Item(-566, 0, 1){ RuntimeId=-1438952986, NetworkId=-566, ExtraData = null }, /**/
			new Item(171, 0, 1){ RuntimeId=1460445510, NetworkId=171, ExtraData = null }, /*minecraft:carpet*/
			new Item(-604, 0, 1){ RuntimeId=1344176194, NetworkId=-604, ExtraData = null }, /**/
			new Item(-603, 0, 1){ RuntimeId=1534603537, NetworkId=-603, ExtraData = null }, /**/
			new Item(-611, 0, 1){ RuntimeId=-765532584, NetworkId=-611, ExtraData = null }, /**/
			new Item(-608, 0, 1){ RuntimeId=-367598733, NetworkId=-608, ExtraData = null }, /**/
			new Item(-610, 0, 1){ RuntimeId=-896564178, NetworkId=-610, ExtraData = null }, /**/
			new Item(-597, 0, 1){ RuntimeId=1314859442, NetworkId=-597, ExtraData = null }, /**/
			new Item(-600, 0, 1){ RuntimeId=-2068865284, NetworkId=-600, ExtraData = null }, /**/
			new Item(-601, 0, 1){ RuntimeId=-472719281, NetworkId=-601, ExtraData = null }, /**/
			new Item(-609, 0, 1){ RuntimeId=-268292072, NetworkId=-609, ExtraData = null }, /**/
			new Item(-605, 0, 1){ RuntimeId=778656297, NetworkId=-605, ExtraData = null }, /**/
			new Item(-599, 0, 1){ RuntimeId=-1953704051, NetworkId=-599, ExtraData = null }, /**/
			new Item(-607, 0, 1){ RuntimeId=1037929218, NetworkId=-607, ExtraData = null }, /**/
			new Item(-606, 0, 1){ RuntimeId=1671964394, NetworkId=-606, ExtraData = null }, /**/
			new Item(-598, 0, 1){ RuntimeId=413981434, NetworkId=-598, ExtraData = null }, /**/
			new Item(-602, 0, 1){ RuntimeId=257454254, NetworkId=-602, ExtraData = null }, /**/
			new Item(237, 0, 1){ RuntimeId=-1067198061, NetworkId=237, ExtraData = null }, /*minecraft:concretePowder*/
			new Item(-716, 0, 1){ RuntimeId=601213063, NetworkId=-716, ExtraData = null }, /**/
			new Item(-715, 0, 1){ RuntimeId=-679294390, NetworkId=-715, ExtraData = null }, /**/
			new Item(-723, 0, 1){ RuntimeId=-1056205883, NetworkId=-723, ExtraData = null }, /**/
			new Item(-720, 0, 1){ RuntimeId=-1406482624, NetworkId=-720, ExtraData = null }, /**/
			new Item(-722, 0, 1){ RuntimeId=409760431, NetworkId=-722, ExtraData = null }, /**/
			new Item(-709, 0, 1){ RuntimeId=85829319, NetworkId=-709, ExtraData = null }, /**/
			new Item(-712, 0, 1){ RuntimeId=-41091047, NetworkId=-712, ExtraData = null }, /**/
			new Item(-713, 0, 1){ RuntimeId=-803737640, NetworkId=-713, ExtraData = null }, /**/
			new Item(-721, 0, 1){ RuntimeId=326898957, NetworkId=-721, ExtraData = null }, /**/
			new Item(-717, 0, 1){ RuntimeId=-1141225062, NetworkId=-717, ExtraData = null }, /**/
			new Item(-711, 0, 1){ RuntimeId=1850893626, NetworkId=-711, ExtraData = null }, /**/
			new Item(-719, 0, 1){ RuntimeId=1173845579, NetworkId=-719, ExtraData = null }, /**/
			new Item(-718, 0, 1){ RuntimeId=593594903, NetworkId=-718, ExtraData = null }, /**/
			new Item(-710, 0, 1){ RuntimeId=1135729023, NetworkId=-710, ExtraData = null }, /**/
			new Item(-714, 0, 1){ RuntimeId=-1088265285, NetworkId=-714, ExtraData = null }, /**/
			new Item(236, 0, 1){ RuntimeId=1075364060, NetworkId=236, ExtraData = null }, /*minecraft:concrete*/
			new Item(-635, 0, 1){ RuntimeId=919423496, NetworkId=-635, ExtraData = null }, /**/
			new Item(-634, 0, 1){ RuntimeId=-267259613, NetworkId=-634, ExtraData = null }, /**/
			new Item(-642, 0, 1){ RuntimeId=227928282, NetworkId=-642, ExtraData = null }, /**/
			new Item(-639, 0, 1){ RuntimeId=229769057, NetworkId=-639, ExtraData = null }, /**/
			new Item(-641, 0, 1){ RuntimeId=-1614335316, NetworkId=-641, ExtraData = null }, /**/
			new Item(-628, 0, 1){ RuntimeId=1060622528, NetworkId=-628, ExtraData = null }, /**/
			new Item(-631, 0, 1){ RuntimeId=-591746106, NetworkId=-631, ExtraData = null }, /**/
			new Item(-632, 0, 1){ RuntimeId=-1464409123, NetworkId=-632, ExtraData = null }, /**/
			new Item(-640, 0, 1){ RuntimeId=1381373594, NetworkId=-640, ExtraData = null }, /**/
			new Item(-636, 0, 1){ RuntimeId=-57242853, NetworkId=-636, ExtraData = null }, /**/
			new Item(-630, 0, 1){ RuntimeId=-765002677, NetworkId=-630, ExtraData = null }, /**/
			new Item(-638, 0, 1){ RuntimeId=-850019252, NetworkId=-638, ExtraData = null }, /**/
			new Item(-637, 0, 1){ RuntimeId=-1744598484, NetworkId=-637, ExtraData = null }, /**/
			new Item(-629, 0, 1){ RuntimeId=-1651250724, NetworkId=-629, ExtraData = null }, /**/
			new Item(-633, 0, 1){ RuntimeId=728288428, NetworkId=-633, ExtraData = null }, /**/
			new Item(172, 0, 1){ RuntimeId=-336983999, NetworkId=172, ExtraData = null }, /*minecraft:hardened_clay*/
			new Item(159, 0, 1){ RuntimeId=1051498658, NetworkId=159, ExtraData = null }, /*minecraft:stained_hardened_clay*/
			new Item(-731, 0, 1){ RuntimeId=-411014530, NetworkId=-731, ExtraData = null }, /**/
			new Item(-730, 0, 1){ RuntimeId=1561153741, NetworkId=-730, ExtraData = null }, /**/
			new Item(-738, 0, 1){ RuntimeId=-1088692924, NetworkId=-738, ExtraData = null }, /**/
			new Item(-735, 0, 1){ RuntimeId=-1924625601, NetworkId=-735, ExtraData = null }, /**/
			new Item(-737, 0, 1){ RuntimeId=-238693134, NetworkId=-737, ExtraData = null }, /**/
			new Item(-724, 0, 1){ RuntimeId=-1868141930, NetworkId=-724, ExtraData = null }, /**/
			new Item(-727, 0, 1){ RuntimeId=-1394028696, NetworkId=-727, ExtraData = null }, /**/
			new Item(-728, 0, 1){ RuntimeId=-933333861, NetworkId=-728, ExtraData = null }, /**/
			new Item(-736, 0, 1){ RuntimeId=1197969468, NetworkId=-736, ExtraData = null }, /**/
			new Item(-732, 0, 1){ RuntimeId=973836165, NetworkId=-732, ExtraData = null }, /**/
			new Item(-726, 0, 1){ RuntimeId=1272302161, NetworkId=-726, ExtraData = null }, /**/
			new Item(-734, 0, 1){ RuntimeId=-1839896994, NetworkId=-734, ExtraData = null }, /**/
			new Item(-733, 0, 1){ RuntimeId=-1757162362, NetworkId=-733, ExtraData = null }, /**/
			new Item(-725, 0, 1){ RuntimeId=-1449045282, NetworkId=-725, ExtraData = null }, /**/
			new Item(-729, 0, 1){ RuntimeId=-2038737358, NetworkId=-729, ExtraData = null }, /**/
			new Item(220, 0, 1){ RuntimeId=16603170, NetworkId=220, ExtraData = null }, /*minecraft:white_glazed_terracotta*/
			new Item(228, 0, 1){ RuntimeId=-1850412551, NetworkId=228, ExtraData = null }, /*minecraft:silver_glazed_terracotta*/
			new Item(227, 0, 1){ RuntimeId=-2050534635, NetworkId=227, ExtraData = null }, /*minecraft:gray_glazed_terracotta*/
			new Item(235, 0, 1){ RuntimeId=-199522344, NetworkId=235, ExtraData = null }, /*minecraft:black_glazed_terracotta*/
			new Item(232, 0, 1){ RuntimeId=-1037882073, NetworkId=232, ExtraData = null }, /*minecraft:brown_glazed_terracotta*/
			new Item(234, 0, 1){ RuntimeId=556073414, NetworkId=234, ExtraData = null }, /*minecraft:red_glazed_terracotta*/
			new Item(221, 0, 1){ RuntimeId=904773342, NetworkId=221, ExtraData = null }, /*minecraft:orange_glazed_terracotta*/
			new Item(224, 0, 1){ RuntimeId=-362985564, NetworkId=224, ExtraData = null }, /*minecraft:yellow_glazed_terracotta*/
			new Item(225, 0, 1){ RuntimeId=379907727, NetworkId=225, ExtraData = null }, /*minecraft:lime_glazed_terracotta*/
			new Item(233, 0, 1){ RuntimeId=-1837802080, NetworkId=233, ExtraData = null }, /*minecraft:green_glazed_terracotta*/
			new Item(229, 0, 1){ RuntimeId=769879805, NetworkId=229, ExtraData = null }, /*minecraft:cyan_glazed_terracotta*/
			new Item(223, 0, 1){ RuntimeId=-976483655, NetworkId=223, ExtraData = null }, /*minecraft:light_blue_glazed_terracotta*/
			new Item(231, 0, 1){ RuntimeId=-2106754178, NetworkId=231, ExtraData = null }, /*minecraft:blue_glazed_terracotta*/
			new Item(219, 0, 1){ RuntimeId=1313381298, NetworkId=219, ExtraData = null }, /*minecraft:purple_glazed_terracotta*/
			new Item(222, 0, 1){ RuntimeId=802520842, NetworkId=222, ExtraData = null }, /*minecraft:magenta_glazed_terracotta*/
			new Item(226, 0, 1){ RuntimeId=-156512822, NetworkId=226, ExtraData = null }, /*minecraft:pink_glazed_terracotta*/
			new Item(201, 0, 1){ RuntimeId=-2047108768, NetworkId=201, ExtraData = null }, /*minecraft:purpur_block*/
			new Item(-951, 0, 1){ RuntimeId=518574878, NetworkId=-951, ExtraData = null }, /**/
			new Item(-477, 0, 1){ RuntimeId=-224235564, NetworkId=-477, ExtraData = null }, /**/
			new Item(-475, 0, 1){ RuntimeId=-2093611356, NetworkId=-475, ExtraData = null }, /**/
			new Item(214, 0, 1){ RuntimeId=581531271, NetworkId=214, ExtraData = null }, /*minecraft:nether_wart_block*/
			new Item(-227, 0, 1){ RuntimeId=1820365362, NetworkId=-227, ExtraData = null }, /*minecraft:warped_wart_block*/
			new Item(-230, 0, 1){ RuntimeId=-1953927892, NetworkId=-230, ExtraData = null }, /*minecraft:shroomlight*/
			new Item(-232, 0, 1){ RuntimeId=240216761, NetworkId=-232, ExtraData = null }, /*minecraft:crimson_nylium*/
			new Item(-233, 0, 1){ RuntimeId=-1538785618, NetworkId=-233, ExtraData = null }, /*minecraft:warped_nylium*/
			new Item(87, 0, 1){ RuntimeId=-2144106048, NetworkId=87, ExtraData = null }, /*minecraft:netherrack*/
			new Item(-236, 0, 1){ RuntimeId=601701031, NetworkId=-236, ExtraData = null }, /*minecraft:soul_soil*/
			new Item(2, 0, 1){ RuntimeId=-567203660, NetworkId=2, ExtraData = null }, /*minecraft:grass_block*/
			new Item(243, 0, 1){ RuntimeId=1711067891, NetworkId=243, ExtraData = null }, /*minecraft:podzol*/
			new Item(110, 0, 1){ RuntimeId=1576129324, NetworkId=110, ExtraData = null }, /*minecraft:mycelium*/
			new Item(198, 0, 1){ RuntimeId=1942424059, NetworkId=198, ExtraData = null }, /*minecraft:grass_path*/
			new Item(3, 0, 1){ RuntimeId=-2108756090, NetworkId=3, ExtraData = null }, /*minecraft:dirt*/
			new Item(-962, 0, 1){ RuntimeId=1884368513, NetworkId=-962, ExtraData = null }, /**/
			new Item(-318, 0, 1){ RuntimeId=985900240, NetworkId=-318, ExtraData = null }, /**/
			new Item(60, 0, 1){ RuntimeId=360492383, NetworkId=60, ExtraData = null }, /*minecraft:farmland*/
			new Item(-473, 0, 1){ RuntimeId=1234506994, NetworkId=-473, ExtraData = null }, /**/
			new Item(82, 0, 1){ RuntimeId=666874214, NetworkId=82, ExtraData = null }, /*minecraft:clay*/
			new Item(15, 0, 1){ RuntimeId=2032622302, NetworkId=15, ExtraData = null }, /*minecraft:iron_ore*/
			new Item(14, 0, 1){ RuntimeId=2144082742, NetworkId=14, ExtraData = null }, /*minecraft:gold_ore*/
			new Item(56, 0, 1){ RuntimeId=1419890941, NetworkId=56, ExtraData = null }, /*minecraft:diamond_ore*/
			new Item(21, 0, 1){ RuntimeId=-1364705592, NetworkId=21, ExtraData = null }, /*minecraft:lapis_ore*/
			new Item(73, 0, 1){ RuntimeId=180213920, NetworkId=73, ExtraData = null }, /*minecraft:redstone_ore*/
			new Item(16, 0, 1){ RuntimeId=685383673, NetworkId=16, ExtraData = null }, /*minecraft:coal_ore*/
			new Item(-311, 0, 1){ RuntimeId=-1478483411, NetworkId=-311, ExtraData = null }, /**/
			new Item(129, 0, 1){ RuntimeId=170572189, NetworkId=129, ExtraData = null }, /*minecraft:emerald_ore*/
			new Item(153, 0, 1){ RuntimeId=-205965389, NetworkId=153, ExtraData = null }, /*minecraft:quartz_ore*/
			new Item(-288, 0, 1){ RuntimeId=950287826, NetworkId=-288, ExtraData = null }, /*minecraft:nether_gold_ore*/
			new Item(-271, 0, 1){ RuntimeId=274932653, NetworkId=-271, ExtraData = null }, /*minecraft:ancient_debris*/
			new Item(-401, 0, 1){ RuntimeId=1047475280, NetworkId=-401, ExtraData = null }, /**/
			new Item(-402, 0, 1){ RuntimeId=360452980, NetworkId=-402, ExtraData = null }, /**/
			new Item(-405, 0, 1){ RuntimeId=811072115, NetworkId=-405, ExtraData = null }, /**/
			new Item(-400, 0, 1){ RuntimeId=-925097910, NetworkId=-400, ExtraData = null }, /**/
			new Item(-403, 0, 1){ RuntimeId=299238750, NetworkId=-403, ExtraData = null }, /**/
			new Item(-407, 0, 1){ RuntimeId=-1417960745, NetworkId=-407, ExtraData = null }, /**/
			new Item(-406, 0, 1){ RuntimeId=680219455, NetworkId=-406, ExtraData = null }, /**/
			new Item(-408, 0, 1){ RuntimeId=-2115277505, NetworkId=-408, ExtraData = null }, /**/
			new Item(1, 0, 1){ RuntimeId=-2144268767, NetworkId=1, ExtraData = null }, /*minecraft:stone*/
			new Item(-590, 0, 1){ RuntimeId=-909691658, NetworkId=-590, ExtraData = null }, /**/
			new Item(-592, 0, 1){ RuntimeId=-1756489686, NetworkId=-592, ExtraData = null }, /**/
			new Item(-594, 0, 1){ RuntimeId=1683032592, NetworkId=-594, ExtraData = null }, /**/
			new Item(-273, 0, 1){ RuntimeId=-2017701205, NetworkId=-273, ExtraData = null }, /*minecraft:blackstone*/
			new Item(-378, 0, 1){ RuntimeId=994207970, NetworkId=-378, ExtraData = null }, /**/
			new Item(-333, 0, 1){ RuntimeId=-194428816, NetworkId=-333, ExtraData = null }, /**/
			new Item(-234, 0, 1){ RuntimeId=1581894931, NetworkId=-234, ExtraData = null }, /*minecraft:basalt*/
			new Item(-591, 0, 1){ RuntimeId=118998978, NetworkId=-591, ExtraData = null }, /**/
			new Item(-593, 0, 1){ RuntimeId=1362605798, NetworkId=-593, ExtraData = null }, /**/
			new Item(-595, 0, 1){ RuntimeId=200169620, NetworkId=-595, ExtraData = null }, /**/
			new Item(-291, 0, 1){ RuntimeId=-1467081089, NetworkId=-291, ExtraData = null }, /*minecraft:polished_blackstone*/
			new Item(-383, 0, 1){ RuntimeId=-1052582365, NetworkId=-383, ExtraData = null }, /**/
			new Item(-748, 0, 1){ RuntimeId=1802723292, NetworkId=-748, ExtraData = null }, /**/
			new Item(-235, 0, 1){ RuntimeId=2090385247, NetworkId=-235, ExtraData = null }, /*minecraft:polished_basalt*/
			new Item(-377, 0, 1){ RuntimeId=368734180, NetworkId=-377, ExtraData = null }, /**/
			new Item(-1109, 0, 1){ RuntimeId=-1113577883, NetworkId=-1109, ExtraData = null }, /**/
			new Item(-1114, 0, 1){ RuntimeId=-1440159439, NetworkId=-1114, ExtraData = null }, /**/
			new Item(-1092, 0, 1){ RuntimeId=761630168, NetworkId=-1092, ExtraData = null }, /**/
			new Item(-1097, 0, 1){ RuntimeId=-937432156, NetworkId=-1097, ExtraData = null }, /**/
			new Item(-1108, 0, 1){ RuntimeId=-660266133, NetworkId=-1108, ExtraData = null }, /**/
			new Item(13, 0, 1){ RuntimeId=1529044762, NetworkId=13, ExtraData = null }, /*minecraft:gravel*/
			new Item(12, 0, 1){ RuntimeId=138639715, NetworkId=12, ExtraData = null }, /*minecraft:sand*/
			new Item(-949, 0, 1){ RuntimeId=-66861741, NetworkId=-949, ExtraData = null }, /**/
			new Item(81, 0, 1){ RuntimeId=269582903, NetworkId=81, ExtraData = null }, /*minecraft:cactus*/
			new Item(17, 0, 1){ RuntimeId=825916963, NetworkId=17, ExtraData = null }, /*minecraft:log*/
			new Item(-10, 0, 1){ RuntimeId=1798777496, NetworkId=-10, ExtraData = null }, /*minecraft:stripped_oak_log*/
			new Item(-569, 0, 1){ RuntimeId=507450469, NetworkId=-569, ExtraData = null }, /**/
			new Item(-5, 0, 1){ RuntimeId=-1220513712, NetworkId=-5, ExtraData = null }, /*minecraft:stripped_spruce_log*/
			new Item(-570, 0, 1){ RuntimeId=1423805714, NetworkId=-570, ExtraData = null }, /**/
			new Item(-6, 0, 1){ RuntimeId=-768079651, NetworkId=-6, ExtraData = null }, /*minecraft:stripped_birch_log*/
			new Item(-571, 0, 1){ RuntimeId=1059442202, NetworkId=-571, ExtraData = null }, /**/
			new Item(-7, 0, 1){ RuntimeId=516789807, NetworkId=-7, ExtraData = null }, /*minecraft:stripped_jungle_log*/
			new Item(162, 0, 1){ RuntimeId=-1264119183, NetworkId=162, ExtraData = null }, /*minecraft:log2*/
			new Item(-8, 0, 1){ RuntimeId=1367858272, NetworkId=-8, ExtraData = null }, /*minecraft:stripped_acacia_log*/
			new Item(-572, 0, 1){ RuntimeId=-1226044173, NetworkId=-572, ExtraData = null }, /**/
			new Item(-9, 0, 1){ RuntimeId=1920611122, NetworkId=-9, ExtraData = null }, /*minecraft:stripped_dark_oak_log*/
			new Item(-484, 0, 1){ RuntimeId=-1984330898, NetworkId=-484, ExtraData = null }, /**/
			new Item(-485, 0, 1){ RuntimeId=-1247849989, NetworkId=-485, ExtraData = null }, /**/
			new Item(-536, 0, 1){ RuntimeId=-992628298, NetworkId=-536, ExtraData = null }, /**/
			new Item(-535, 0, 1){ RuntimeId=629722723, NetworkId=-535, ExtraData = null }, /**/
			new Item(-995, 0, 1){ RuntimeId=1544644811, NetworkId=-995, ExtraData = null }, /**/
			new Item(-994, 0, 1){ RuntimeId=1627143190, NetworkId=-994, ExtraData = null }, /**/
			new Item(-225, 0, 1){ RuntimeId=1025569707, NetworkId=-225, ExtraData = null }, /*minecraft:crimson_stem*/
			new Item(-240, 0, 1){ RuntimeId=850053242, NetworkId=-240, ExtraData = null }, /*minecraft:stripped_crimson_stem*/
			new Item(-226, 0, 1){ RuntimeId=867349882, NetworkId=-226, ExtraData = null }, /*minecraft:warped_stem*/
			new Item(-241, 0, 1){ RuntimeId=74387233, NetworkId=-241, ExtraData = null }, /*minecraft:stripped_warped_stem*/
			new Item(-212, 0, 1){ RuntimeId=1622499771, NetworkId=-212, ExtraData = null }, /*minecraft:wood*/
			new Item(-819, 0, 1){ RuntimeId=2055607698, NetworkId=-819, ExtraData = null }, /**/
			new Item(-814, 0, 1){ RuntimeId=200372841, NetworkId=-814, ExtraData = null }, /**/
			new Item(-820, 0, 1){ RuntimeId=1957875066, NetworkId=-820, ExtraData = null }, /**/
			new Item(-815, 0, 1){ RuntimeId=1439623604, NetworkId=-815, ExtraData = null }, /**/
			new Item(-821, 0, 1){ RuntimeId=155416677, NetworkId=-821, ExtraData = null }, /**/
			new Item(-816, 0, 1){ RuntimeId=764042440, NetworkId=-816, ExtraData = null }, /**/
			new Item(-822, 0, 1){ RuntimeId=490498551, NetworkId=-822, ExtraData = null }, /**/
			new Item(-817, 0, 1){ RuntimeId=1923429817, NetworkId=-817, ExtraData = null }, /**/
			new Item(-823, 0, 1){ RuntimeId=-1047830858, NetworkId=-823, ExtraData = null }, /**/
			new Item(-818, 0, 1){ RuntimeId=1020570755, NetworkId=-818, ExtraData = null }, /**/
			new Item(-824, 0, 1){ RuntimeId=1427006944, NetworkId=-824, ExtraData = null }, /**/
			new Item(-497, 0, 1){ RuntimeId=480159908, NetworkId=-497, ExtraData = null }, /**/
			new Item(-498, 0, 1){ RuntimeId=1014956299, NetworkId=-498, ExtraData = null }, /**/
			new Item(-546, 0, 1){ RuntimeId=-1040123092, NetworkId=-546, ExtraData = null }, /**/
			new Item(-545, 0, 1){ RuntimeId=-1248487805, NetworkId=-545, ExtraData = null }, /**/
			new Item(-1005, 0, 1){ RuntimeId=-887214257, NetworkId=-1005, ExtraData = null }, /**/
			new Item(-1004, 0, 1){ RuntimeId=-140707320, NetworkId=-1004, ExtraData = null }, /**/
			new Item(-299, 0, 1){ RuntimeId=-1380369927, NetworkId=-299, ExtraData = null }, /*minecraft:crimson_hyphae*/
			new Item(-300, 0, 1){ RuntimeId=1851001188, NetworkId=-300, ExtraData = null }, /*minecraft:stripped_crimson_hyphae*/
			new Item(-298, 0, 1){ RuntimeId=16963668, NetworkId=-298, ExtraData = null }, /*minecraft:warped_hyphae*/
			new Item(-301, 0, 1){ RuntimeId=-1008489317, NetworkId=-301, ExtraData = null }, /*minecraft:stripped_warped_hyphae*/
			new Item(-527, 0, 1){ RuntimeId=2109882402, NetworkId=-527, ExtraData = null }, /**/
			new Item(-528, 0, 1){ RuntimeId=-781413973, NetworkId=-528, ExtraData = null }, /**/
			new Item(18, 0, 1){ RuntimeId=2110714365, NetworkId=18, ExtraData = null }, /*minecraft:leaves*/
			new Item(-800, 0, 1){ RuntimeId=1404704691, NetworkId=-800, ExtraData = null }, /**/
			new Item(-801, 0, 1){ RuntimeId=1740500456, NetworkId=-801, ExtraData = null }, /**/
			new Item(-802, 0, 1){ RuntimeId=-190454656, NetworkId=-802, ExtraData = null }, /**/
			new Item(161, 0, 1){ RuntimeId=1857362751, NetworkId=161, ExtraData = null }, /*minecraft:leaves2*/
			new Item(-803, 0, 1){ RuntimeId=-1231978339, NetworkId=-803, ExtraData = null }, /**/
			new Item(-472, 0, 1){ RuntimeId=-1626787340, NetworkId=-472, ExtraData = null }, /**/
			new Item(-548, 0, 1){ RuntimeId=1895459964, NetworkId=-548, ExtraData = null }, /**/
			new Item(-1007, 0, 1){ RuntimeId=129912273, NetworkId=-1007, ExtraData = null }, /**/
			new Item(-324, 0, 1){ RuntimeId=1157564365, NetworkId=-324, ExtraData = null }, /**/
			new Item(-325, 0, 1){ RuntimeId=-226272453, NetworkId=-325, ExtraData = null }, /**/
			new Item(6, 0, 1){ RuntimeId=-125670117, NetworkId=6, ExtraData = null }, /*minecraft:sapling*/
			new Item(-825, 0, 1){ RuntimeId=1772161361, NetworkId=-825, ExtraData = null }, /**/
			new Item(-826, 0, 1){ RuntimeId=1998475830, NetworkId=-826, ExtraData = null }, /**/
			new Item(-827, 0, 1){ RuntimeId=1950376030, NetworkId=-827, ExtraData = null }, /**/
			new Item(-828, 0, 1){ RuntimeId=720729405, NetworkId=-828, ExtraData = null }, /**/
			new Item(-829, 0, 1){ RuntimeId=-2029460797, NetworkId=-829, ExtraData = null }, /**/
			new Item(-474, 0, 1){ RuntimeId=-1765922558, NetworkId=-474, ExtraData = null }, /**/
			new Item(-547, 0, 1){ RuntimeId=1571067594, NetworkId=-547, ExtraData = null }, /**/
			new Item(-1006, 0, 1){ RuntimeId=1828678323, NetworkId=-1006, ExtraData = null }, /**/
			new Item(-218, 0, 1){ RuntimeId=277575049, NetworkId=-218, ExtraData = null }, /*minecraft:bee_nest*/
			new Item(295, 0, 1){ RuntimeId=0, NetworkId=320, ExtraData = null }, /*minecraft:wheat_seeds*/
			new Item(361, 0, 1){ RuntimeId=0, NetworkId=321, ExtraData = null }, /*minecraft:pumpkin_seeds*/
			new Item(362, 0, 1){ RuntimeId=0, NetworkId=322, ExtraData = null }, /*minecraft:melon_seeds*/
			new Item(458, 0, 1){ RuntimeId=0, NetworkId=324, ExtraData = null }, /*minecraft:beetroot_seeds*/
			new Item(325, 0, 1){ RuntimeId=0, NetworkId=325, ExtraData = null }, /*minecraft:bucket*/
			new Item(326, 0, 1){ RuntimeId=0, NetworkId=326, ExtraData = null }, /**/
			new Item(296, 0, 1){ RuntimeId=0, NetworkId=366, ExtraData = null }, /*minecraft:wheat*/
			new Item(457, 0, 1){ RuntimeId=0, NetworkId=314, ExtraData = null }, /*minecraft:beetroot*/
			new Item(392, 0, 1){ RuntimeId=0, NetworkId=309, ExtraData = null }, /*minecraft:potato*/
			new Item(394, 0, 1){ RuntimeId=0, NetworkId=311, ExtraData = null }, /*minecraft:poisonous_potato*/
			new Item(391, 0, 1){ RuntimeId=0, NetworkId=308, ExtraData = null }, /*minecraft:carrot*/
			new Item(396, 0, 1){ RuntimeId=0, NetworkId=312, ExtraData = null }, /*minecraft:golden_carrot*/
			new Item(260, 0, 1){ RuntimeId=0, NetworkId=285, ExtraData = null }, /*minecraft:apple*/
			new Item(322, 0, 1){ RuntimeId=0, NetworkId=287, ExtraData = null }, /*minecraft:golden_apple*/
			new Item(466, 0, 1){ RuntimeId=0, NetworkId=288, ExtraData = null }, /*minecraft:enchanted_golden_apple*/
			new Item(103, 0, 1){ RuntimeId=-890578421, NetworkId=103, ExtraData = null }, /*minecraft:melon_block*/
			new Item(360, 0, 1){ RuntimeId=0, NetworkId=301, ExtraData = null }, /*minecraft:melon_slice*/
			new Item(382, 0, 1){ RuntimeId=0, NetworkId=467, ExtraData = null }, /*minecraft:glistering_melon_slice*/
			new Item(477, 0, 1){ RuntimeId=0, NetworkId=316, ExtraData = null }, /*minecraft:sweet_berries*/
			new Item(845, 0, 1){ RuntimeId=0, NetworkId=845, ExtraData = null }, /**/
			new Item(86, 0, 1){ RuntimeId=-1324410811, NetworkId=86, ExtraData = null }, /*minecraft:pumpkin*/
			new Item(-155, 0, 1){ RuntimeId=-446180516, NetworkId=-155, ExtraData = null }, /*minecraft:carved_pumpkin*/
			new Item(91, 0, 1){ RuntimeId=-535508085, NetworkId=91, ExtraData = null }, /*minecraft:lit_pumpkin*/
			new Item(736, 0, 1){ RuntimeId=0, NetworkId=632, ExtraData = null }, /*minecraft:honeycomb*/
			new Item(-1022, 0, 1){ RuntimeId=-1007963883, NetworkId=-1022, ExtraData = null }, /**/
			new Item(-848, 0, 1){ RuntimeId=-1146418422, NetworkId=-848, ExtraData = null }, /**/
			new Item(-865, 0, 1){ RuntimeId=-1206782254, NetworkId=-865, ExtraData = null }, /**/
			new Item(31, 0, 1){ RuntimeId=-1467063515, NetworkId=31, ExtraData = null }, /*minecraft:tallgrass*/
			new Item(-864, 0, 1){ RuntimeId=-119440047, NetworkId=-864, ExtraData = null }, /**/
			new Item(-1028, 0, 1){ RuntimeId=1208577191, NetworkId=-1028, ExtraData = null }, /**/
			new Item(-1029, 0, 1){ RuntimeId=935230755, NetworkId=-1029, ExtraData = null }, /**/
			new Item(-1023, 0, 1){ RuntimeId=-1333695061, NetworkId=-1023, ExtraData = null }, /**/
			new Item(760, 0, 1){ RuntimeId=0, NetworkId=663, ExtraData = null }, /*minecraft:nether_sprouts*/
			new Item(-583, 0, 1){ RuntimeId=341687485, NetworkId=-583, ExtraData = null }, /**/
			new Item(-581, 0, 1){ RuntimeId=-2043793234, NetworkId=-581, ExtraData = null }, /**/
			new Item(-582, 0, 1){ RuntimeId=760973449, NetworkId=-582, ExtraData = null }, /**/
			new Item(-131, 0, 1){ RuntimeId=-103777197, NetworkId=-131, ExtraData = null }, /*minecraft:coral*/
			new Item(-584, 0, 1){ RuntimeId=2087327870, NetworkId=-584, ExtraData = null }, /**/
			new Item(-588, 0, 1){ RuntimeId=1592597203, NetworkId=-588, ExtraData = null }, /**/
			new Item(-586, 0, 1){ RuntimeId=-1573442780, NetworkId=-586, ExtraData = null }, /**/
			new Item(-587, 0, 1){ RuntimeId=205368427, NetworkId=-587, ExtraData = null }, /**/
			new Item(-585, 0, 1){ RuntimeId=684972525, NetworkId=-585, ExtraData = null }, /**/
			new Item(-589, 0, 1){ RuntimeId=-419885808, NetworkId=-589, ExtraData = null }, /**/
			new Item(-842, 0, 1){ RuntimeId=307997716, NetworkId=-842, ExtraData = null }, /**/
			new Item(-840, 0, 1){ RuntimeId=-594785445, NetworkId=-840, ExtraData = null }, /**/
			new Item(-841, 0, 1){ RuntimeId=363691816, NetworkId=-841, ExtraData = null }, /**/
			new Item(-133, 0, 1){ RuntimeId=1658024058, NetworkId=-133, ExtraData = null }, /*minecraft:coral_fan*/
			new Item(-843, 0, 1){ RuntimeId=923220603, NetworkId=-843, ExtraData = null }, /**/
			new Item(-846, 0, 1){ RuntimeId=194525402, NetworkId=-846, ExtraData = null }, /**/
			new Item(-844, 0, 1){ RuntimeId=1659995561, NetworkId=-844, ExtraData = null }, /**/
			new Item(-845, 0, 1){ RuntimeId=297061818, NetworkId=-845, ExtraData = null }, /**/
			new Item(-134, 0, 1){ RuntimeId=379921572, NetworkId=-134, ExtraData = null }, /*minecraft:coral_fan_dead*/
			new Item(-847, 0, 1){ RuntimeId=-434411723, NetworkId=-847, ExtraData = null }, /**/
			new Item(-223, 0, 1){ RuntimeId=-678394037, NetworkId=-223, ExtraData = null }, /*minecraft:crimson_roots*/
			new Item(-224, 0, 1){ RuntimeId=1880023250, NetworkId=-224, ExtraData = null }, /*minecraft:warped_roots*/
			new Item(37, 0, 1){ RuntimeId=-1911823592, NetworkId=37, ExtraData = null }, /*minecraft:yellow_flower*/
			new Item(-1091, 0, 1){ RuntimeId=-758812583, NetworkId=-1091, ExtraData = null }, /**/
			new Item(38, 0, 1){ RuntimeId=-876992014, NetworkId=38, ExtraData = null }, /*minecraft:red_flower*/
			new Item(-830, 0, 1){ RuntimeId=1273766654, NetworkId=-830, ExtraData = null }, /**/
			new Item(-831, 0, 1){ RuntimeId=-1115536577, NetworkId=-831, ExtraData = null }, /**/
			new Item(-832, 0, 1){ RuntimeId=-1627158336, NetworkId=-832, ExtraData = null }, /**/
			new Item(-833, 0, 1){ RuntimeId=-543276538, NetworkId=-833, ExtraData = null }, /**/
			new Item(-834, 0, 1){ RuntimeId=-1011475722, NetworkId=-834, ExtraData = null }, /**/
			new Item(-835, 0, 1){ RuntimeId=-1380429918, NetworkId=-835, ExtraData = null }, /**/
			new Item(-836, 0, 1){ RuntimeId=-1791018526, NetworkId=-836, ExtraData = null }, /**/
			new Item(-837, 0, 1){ RuntimeId=2004595907, NetworkId=-837, ExtraData = null }, /**/
			new Item(-838, 0, 1){ RuntimeId=-1061464456, NetworkId=-838, ExtraData = null }, /**/
			new Item(-839, 0, 1){ RuntimeId=2096774481, NetworkId=-839, ExtraData = null }, /**/
			new Item(175, 0, 1){ RuntimeId=713651213, NetworkId=175, ExtraData = null }, /*minecraft:double_plant*/
			new Item(-863, 0, 1){ RuntimeId=346976229, NetworkId=-863, ExtraData = null }, /**/
			new Item(-866, 0, 1){ RuntimeId=-1883287610, NetworkId=-866, ExtraData = null }, /**/
			new Item(-867, 0, 1){ RuntimeId=1203451821, NetworkId=-867, ExtraData = null }, /**/
			new Item(-612, 0, 1){ RuntimeId=-1782684825, NetworkId=-612, ExtraData = null }, /**/
			new Item(-549, 0, 1){ RuntimeId=450783541, NetworkId=-549, ExtraData = null }, /**/
			new Item(-1024, 0, 1){ RuntimeId=-390925247, NetworkId=-1024, ExtraData = null }, /**/
			new Item(-216, 0, 1){ RuntimeId=1070281985, NetworkId=-216, ExtraData = null }, /*minecraft:wither_rose*/
			new Item(-568, 0, 1){ RuntimeId=-133267933, NetworkId=-568, ExtraData = null }, /**/
			new Item(-1030, 0, 1){ RuntimeId=1885884239, NetworkId=-1030, ExtraData = null }, /**/
			new Item(-1019, 0, 1){ RuntimeId=2018094267, NetworkId=-1019, ExtraData = null }, /**/
			new Item(-1018, 0, 1){ RuntimeId=-2043716611, NetworkId=-1018, ExtraData = null }, /**/
			new Item(351, 19, 1){ RuntimeId=0, NetworkId=442, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 7, 1){ RuntimeId=0, NetworkId=434, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 8, 1){ RuntimeId=0, NetworkId=435, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 16, 1){ RuntimeId=0, NetworkId=427, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 17, 1){ RuntimeId=0, NetworkId=430, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 1, 1){ RuntimeId=0, NetworkId=428, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 14, 1){ RuntimeId=0, NetworkId=441, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 11, 1){ RuntimeId=0, NetworkId=438, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 10, 1){ RuntimeId=0, NetworkId=437, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 2, 1){ RuntimeId=0, NetworkId=429, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 6, 1){ RuntimeId=0, NetworkId=433, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 12, 1){ RuntimeId=0, NetworkId=439, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 18, 1){ RuntimeId=0, NetworkId=431, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 5, 1){ RuntimeId=0, NetworkId=432, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 13, 1){ RuntimeId=0, NetworkId=440, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 9, 1){ RuntimeId=0, NetworkId=436, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 0, 1){ RuntimeId=0, NetworkId=445, ExtraData = null }, /*minecraft:dye*/
			new Item(543, 0, 1){ RuntimeId=0, NetworkId=543, ExtraData = null }, /**/
			new Item(351, 3, 1){ RuntimeId=0, NetworkId=444, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 4, 1){ RuntimeId=0, NetworkId=446, ExtraData = null }, /*minecraft:dye*/
			new Item(351, 15, 1){ RuntimeId=0, NetworkId=443, ExtraData = null }, /*minecraft:dye*/
			new Item(106, 0, 1){ RuntimeId=1530630292, NetworkId=106, ExtraData = null }, /*minecraft:vine*/
			new Item(-231, 0, 1){ RuntimeId=-1988873156, NetworkId=-231, ExtraData = null }, /*minecraft:weeping_vines*/
			new Item(-287, 0, 1){ RuntimeId=555042534, NetworkId=-287, ExtraData = null }, /*minecraft:twisting_vines*/
			new Item(111, 0, 1){ RuntimeId=-1615946013, NetworkId=111, ExtraData = null }, /*minecraft:waterlily*/
			new Item(-130, 0, 1){ RuntimeId=274823543, NetworkId=-130, ExtraData = null }, /*minecraft:seagrass*/
			new Item(335, 0, 1){ RuntimeId=0, NetworkId=414, ExtraData = null }, /*minecraft:kelp*/
			new Item(32, 0, 1){ RuntimeId=-85591723, NetworkId=32, ExtraData = null }, /*minecraft:deadbush*/
			new Item(-163, 0, 1){ RuntimeId=1993764742, NetworkId=-163, ExtraData = null }, /*minecraft:bamboo*/
			new Item(80, 0, 1){ RuntimeId=-2027434956, NetworkId=80, ExtraData = null }, /*minecraft:snow*/
			new Item(79, 0, 1){ RuntimeId=107547877, NetworkId=79, ExtraData = null }, /*minecraft:ice*/
			new Item(174, 0, 1){ RuntimeId=445316843, NetworkId=174, ExtraData = null }, /*minecraft:packed_ice*/
			new Item(-11, 0, 1){ RuntimeId=-234816571, NetworkId=-11, ExtraData = null }, /*minecraft:blue_ice*/
			new Item(78, 0, 1){ RuntimeId=-595350462, NetworkId=78, ExtraData = null }, /*minecraft:snow_layer*/
			new Item(-308, 0, 1){ RuntimeId=1659726445, NetworkId=-308, ExtraData = null }, /**/
			new Item(-317, 0, 1){ RuntimeId=1289380550, NetworkId=-317, ExtraData = null }, /**/
			new Item(-1026, 0, 1){ RuntimeId=-2092212228, NetworkId=-1026, ExtraData = null }, /**/
			new Item(-335, 0, 1){ RuntimeId=1136091496, NetworkId=-335, ExtraData = null }, /**/
			new Item(-320, 0, 1){ RuntimeId=-551148041, NetworkId=-320, ExtraData = null }, /**/
			new Item(-1010, 0, 1){ RuntimeId=-1083982928, NetworkId=-1010, ExtraData = null }, /**/
			new Item(-1009, 0, 1){ RuntimeId=924256979, NetworkId=-1009, ExtraData = null }, /**/
			new Item(-1011, 0, 1){ RuntimeId=1318033419, NetworkId=-1011, ExtraData = null }, /**/
			new Item(-319, 0, 1){ RuntimeId=-1345461932, NetworkId=-319, ExtraData = null }, /**/
			new Item(-482, 0, 1){ RuntimeId=503319956, NetworkId=-482, ExtraData = null }, /**/
			new Item(-483, 0, 1){ RuntimeId=-476298465, NetworkId=-483, ExtraData = null }, /**/
			new Item(-323, 0, 1){ RuntimeId=-1078914111, NetworkId=-323, ExtraData = null }, /**/
			new Item(-336, 0, 1){ RuntimeId=1070937763, NetworkId=-336, ExtraData = null }, /**/
			new Item(-321, 0, 1){ RuntimeId=481848731, NetworkId=-321, ExtraData = null }, /**/
			new Item(-1025, 0, 1){ RuntimeId=329484357, NetworkId=-1025, ExtraData = null }, /**/
			new Item(-337, 0, 1){ RuntimeId=-731457553, NetworkId=-337, ExtraData = null }, /**/
			new Item(-338, 0, 1){ RuntimeId=356354615, NetworkId=-338, ExtraData = null }, /**/
			new Item(-411, 0, 1){ RuntimeId=534169277, NetworkId=-411, ExtraData = null }, /**/
			new Item(-327, 0, 1){ RuntimeId=-831947792, NetworkId=-327, ExtraData = null }, /**/
			new Item(-328, 0, 1){ RuntimeId=-1062772016, NetworkId=-328, ExtraData = null }, /**/
			new Item(-329, 0, 1){ RuntimeId=-960283518, NetworkId=-329, ExtraData = null }, /**/
			new Item(-330, 0, 1){ RuntimeId=-406581079, NetworkId=-330, ExtraData = null }, /**/
			new Item(-331, 0, 1){ RuntimeId=553552024, NetworkId=-331, ExtraData = null }, /**/
			new Item(-332, 0, 1){ RuntimeId=-1262046363, NetworkId=-332, ExtraData = null }, /**/
			new Item(-326, 0, 1){ RuntimeId=1135757605, NetworkId=-326, ExtraData = null }, /**/
			new Item(-1125, 0, 1){ RuntimeId=-1334289858, NetworkId=-1125, ExtraData = null }, /**/
			new Item(365, 0, 1){ RuntimeId=0, NetworkId=304, ExtraData = null }, /*minecraft:chicken*/
			new Item(319, 0, 1){ RuntimeId=0, NetworkId=291, ExtraData = null }, /*minecraft:porkchop*/
			new Item(363, 0, 1){ RuntimeId=0, NetworkId=302, ExtraData = null }, /*minecraft:beef*/
			new Item(423, 0, 1){ RuntimeId=0, NetworkId=589, ExtraData = null }, /*minecraft:mutton*/
			new Item(411, 0, 1){ RuntimeId=0, NetworkId=317, ExtraData = null }, /*minecraft:rabbit*/
			new Item(349, 0, 1){ RuntimeId=0, NetworkId=293, ExtraData = null }, /*minecraft:cod*/
			new Item(460, 0, 1){ RuntimeId=0, NetworkId=294, ExtraData = null }, /*minecraft:salmon*/
			new Item(461, 0, 1){ RuntimeId=0, NetworkId=295, ExtraData = null }, /*minecraft:tropical_fish*/
			new Item(462, 0, 1){ RuntimeId=0, NetworkId=296, ExtraData = null }, /*minecraft:pufferfish*/
			new Item(39, 0, 1){ RuntimeId=1548623150, NetworkId=39, ExtraData = null }, /*minecraft:brown_mushroom*/
			new Item(40, 0, 1){ RuntimeId=-1992436181, NetworkId=40, ExtraData = null }, /*minecraft:red_mushroom*/
			new Item(-228, 0, 1){ RuntimeId=-783457521, NetworkId=-228, ExtraData = null }, /*minecraft:crimson_fungus*/
			new Item(-229, 0, 1){ RuntimeId=-1422933532, NetworkId=-229, ExtraData = null }, /*minecraft:warped_fungus*/
			new Item(99, 0, 1){ RuntimeId=203547508, NetworkId=99, ExtraData = null }, /*minecraft:brown_mushroom_block*/
			new Item(100, 0, 1){ RuntimeId=-1161199409, NetworkId=100, ExtraData = null }, /*minecraft:red_mushroom_block*/
			new Item(-1008, 0, 1){ RuntimeId=-19901229, NetworkId=-1008, ExtraData = null }, /**/
			new Item(344, 0, 1){ RuntimeId=0, NetworkId=422, ExtraData = null }, /*minecraft:egg*/
			new Item(754, 0, 1){ RuntimeId=0, NetworkId=754, ExtraData = null }, /*minecraft:warped_sign*/
			new Item(753, 0, 1){ RuntimeId=0, NetworkId=753, ExtraData = null }, /*minecraft:crimson_sign*/
			new Item(338, 0, 1){ RuntimeId=0, NetworkId=417, ExtraData = null }, /*minecraft:item.reeds*/
			new Item(353, 0, 1){ RuntimeId=0, NetworkId=448, ExtraData = null }, /*minecraft:sugar*/
			new Item(367, 0, 1){ RuntimeId=0, NetworkId=306, ExtraData = null }, /*minecraft:rotten_flesh*/
			new Item(352, 0, 1){ RuntimeId=0, NetworkId=447, ExtraData = null }, /*minecraft:bone*/
			new Item(30, 0, 1){ RuntimeId=955936010, NetworkId=30, ExtraData = null }, /*minecraft:web*/
			new Item(375, 0, 1){ RuntimeId=0, NetworkId=307, ExtraData = null }, /*minecraft:spider_eye*/
			new Item(52, 0, 1){ RuntimeId=-1710007245, NetworkId=52, ExtraData = null }, /*minecraft:mob_spawner*/
			new Item(-315, 0, 1){ RuntimeId=-964618794, NetworkId=-315, ExtraData = null }, /**/
			new Item(-314, 0, 1){ RuntimeId=-1073810453, NetworkId=-314, ExtraData = null }, /**/
			new Item(-1012, 0, 1){ RuntimeId=-809380911, NetworkId=-1012, ExtraData = null }, /**/
			new Item(120, 0, 1){ RuntimeId=-59303845, NetworkId=120, ExtraData = null }, /*minecraft:end_portal_frame*/
			new Item(97, 0, 1){ RuntimeId=-1306003547, NetworkId=97, ExtraData = null }, /*minecraft:monster_egg*/
			new Item(-858, 0, 1){ RuntimeId=1889697702, NetworkId=-858, ExtraData = null }, /**/
			new Item(-859, 0, 1){ RuntimeId=1188684497, NetworkId=-859, ExtraData = null }, /**/
			new Item(-860, 0, 1){ RuntimeId=-67942651, NetworkId=-860, ExtraData = null }, /**/
			new Item(-861, 0, 1){ RuntimeId=1839492969, NetworkId=-861, ExtraData = null }, /**/
			new Item(-862, 0, 1){ RuntimeId=656057126, NetworkId=-862, ExtraData = null }, /**/
			new Item(-454, 0, 1){ RuntimeId=1860943670, NetworkId=-454, ExtraData = null }, /**/
			new Item(122, 0, 1){ RuntimeId=-1856226336, NetworkId=122, ExtraData = null }, /*minecraft:dragon_egg*/
			new Item(-159, 0, 1){ RuntimeId=567624840, NetworkId=-159, ExtraData = null }, /*minecraft:turtle_egg*/
			new Item(-596, 0, 1){ RuntimeId=-637589777, NetworkId=-596, ExtraData = null }, /**/
			new Item(-1027, 0, 1){ RuntimeId=-1564075629, NetworkId=-1027, ExtraData = null }, /**/
			new Item(-468, 0, 1){ RuntimeId=-1650734011, NetworkId=-468, ExtraData = null }, /**/
			new Item(-469, 0, 1){ RuntimeId=1080727706, NetworkId=-469, ExtraData = null }, /**/
			new Item(-470, 0, 1){ RuntimeId=-1318615796, NetworkId=-470, ExtraData = null }, /**/
			new Item(-471, 0, 1){ RuntimeId=-1659923475, NetworkId=-471, ExtraData = null }, /**/
			new Item(383, 10, 1){ RuntimeId=0, NetworkId=468, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 11, 1){ RuntimeId=0, NetworkId=469, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 12, 1){ RuntimeId=0, NetworkId=470, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 13, 1){ RuntimeId=0, NetworkId=471, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(697, 0, 1){ RuntimeId=0, NetworkId=697, ExtraData = null }, /**/
			new Item(383, 24, 1){ RuntimeId=0, NetworkId=499, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 23, 1){ RuntimeId=0, NetworkId=491, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 25, 1){ RuntimeId=0, NetworkId=500, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 75, 1){ RuntimeId=0, NetworkId=522, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 30, 1){ RuntimeId=0, NetworkId=512, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 14, 1){ RuntimeId=0, NetworkId=472, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(745, 0, 1){ RuntimeId=0, NetworkId=745, ExtraData = null }, /*minecraft:netherite_pickaxe*/
			new Item(383, 19, 1){ RuntimeId=0, NetworkId=486, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 122, 1){ RuntimeId=0, NetworkId=528, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 121, 1){ RuntimeId=0, NetworkId=524, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 128, 1){ RuntimeId=0, NetworkId=537, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 29, 1){ RuntimeId=0, NetworkId=507, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 22, 1){ RuntimeId=0, NetworkId=484, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 113, 1){ RuntimeId=0, NetworkId=523, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 28, 1){ RuntimeId=0, NetworkId=506, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 18, 1){ RuntimeId=0, NetworkId=492, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 130, 1){ RuntimeId=0, NetworkId=536, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 112, 1){ RuntimeId=0, NetworkId=514, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 31, 1){ RuntimeId=0, NetworkId=518, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 132, 1){ RuntimeId=0, NetworkId=670, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 129, 1){ RuntimeId=0, NetworkId=538, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(784, 0, 1){ RuntimeId=0, NetworkId=784, ExtraData = null }, /**/
			new Item(383, 108, 1){ RuntimeId=0, NetworkId=515, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 109, 1){ RuntimeId=0, NetworkId=516, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 17, 1){ RuntimeId=0, NetworkId=483, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 133, 1){ RuntimeId=0, NetworkId=671, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 111, 1){ RuntimeId=0, NetworkId=513, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 74, 1){ RuntimeId=0, NetworkId=519, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 134, 1){ RuntimeId=0, NetworkId=673, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 16, 1){ RuntimeId=0, NetworkId=473, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(534, 0, 1){ RuntimeId=0, NetworkId=534, ExtraData = null }, /**/
			new Item(772, 0, 1){ RuntimeId=0, NetworkId=772, ExtraData = null }, /**/
			new Item(539, 0, 1){ RuntimeId=0, NetworkId=539, ExtraData = null }, /**/
			new Item(540, 0, 1){ RuntimeId=0, NetworkId=540, ExtraData = null }, /**/
			new Item(690, 0, 1){ RuntimeId=0, NetworkId=690, ExtraData = null }, /**/
			new Item(383, 115, 1){ RuntimeId=0, NetworkId=482, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 118, 1){ RuntimeId=0, NetworkId=526, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(496, 0, 1){ RuntimeId=0, NetworkId=496, ExtraData = null }, /**/
			new Item(698, 0, 1){ RuntimeId=0, NetworkId=698, ExtraData = null }, /**/
			new Item(383, 110, 1){ RuntimeId=0, NetworkId=517, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 47, 1){ RuntimeId=0, NetworkId=497, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(786, 0, 1){ RuntimeId=0, NetworkId=786, ExtraData = null }, /**/
			new Item(383, 34, 1){ RuntimeId=0, NetworkId=477, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 26, 1){ RuntimeId=0, NetworkId=501, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 46, 1){ RuntimeId=0, NetworkId=495, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 32, 1){ RuntimeId=0, NetworkId=480, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 27, 1){ RuntimeId=0, NetworkId=502, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(785, 0, 1){ RuntimeId=0, NetworkId=785, ExtraData = null }, /**/
			new Item(383, 116, 1){ RuntimeId=0, NetworkId=511, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 40, 1){ RuntimeId=0, NetworkId=490, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 35, 1){ RuntimeId=0, NetworkId=479, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(535, 0, 1){ RuntimeId=0, NetworkId=535, ExtraData = null }, /**/
			new Item(751, 0, 1){ RuntimeId=0, NetworkId=751, ExtraData = null }, /*minecraft:netherite_boots*/
			new Item(383, 33, 1){ RuntimeId=0, NetworkId=474, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 50, 1){ RuntimeId=0, NetworkId=505, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 49, 1){ RuntimeId=0, NetworkId=494, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 58, 1){ RuntimeId=0, NetworkId=520, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 39, 1){ RuntimeId=0, NetworkId=476, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 37, 1){ RuntimeId=0, NetworkId=478, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(793, 0, 1){ RuntimeId=0, NetworkId=793, ExtraData = null }, /**/
			new Item(674, 0, 1){ RuntimeId=0, NetworkId=674, ExtraData = null }, /**/
			new Item(383, 45, 1){ RuntimeId=0, NetworkId=485, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 104, 1){ RuntimeId=0, NetworkId=509, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 114, 1){ RuntimeId=0, NetworkId=525, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 59, 1){ RuntimeId=0, NetworkId=527, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 105, 1){ RuntimeId=0, NetworkId=510, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 57, 1){ RuntimeId=0, NetworkId=508, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 43, 1){ RuntimeId=0, NetworkId=489, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 41, 1){ RuntimeId=0, NetworkId=487, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(755, 0, 1){ RuntimeId=0, NetworkId=755, ExtraData = null }, /*minecraft:crimson_door*/
			new Item(383, 124, 1){ RuntimeId=0, NetworkId=530, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 42, 1){ RuntimeId=0, NetworkId=488, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 123, 1){ RuntimeId=0, NetworkId=531, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 127, 1){ RuntimeId=0, NetworkId=533, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 125, 1){ RuntimeId=0, NetworkId=529, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 48, 1){ RuntimeId=0, NetworkId=498, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 126, 1){ RuntimeId=0, NetworkId=532, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 36, 1){ RuntimeId=0, NetworkId=481, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 38, 1){ RuntimeId=0, NetworkId=475, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 55, 1){ RuntimeId=0, NetworkId=493, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(383, 54, 1){ RuntimeId=0, NetworkId=503, ExtraData = null }, /*minecraft:spawn_egg*/
			new Item(49, 0, 1){ RuntimeId=-1513117254, NetworkId=49, ExtraData = null }, /*minecraft:obsidian*/
			new Item(-289, 0, 1){ RuntimeId=1581112104, NetworkId=-289, ExtraData = null }, /*minecraft:crying_obsidian*/
			new Item(7, 0, 1){ RuntimeId=-173245189, NetworkId=7, ExtraData = null }, /*minecraft:bedrock*/
			new Item(88, 0, 1){ RuntimeId=-1289874924, NetworkId=88, ExtraData = null }, /*minecraft:soul_sand*/
			new Item(213, 0, 1){ RuntimeId=1719727561, NetworkId=213, ExtraData = null }, /*minecraft:magma*/
			new Item(372, 0, 1){ RuntimeId=0, NetworkId=323, ExtraData = null }, /*minecraft:nether_wart*/
			new Item(121, 0, 1){ RuntimeId=-1253866153, NetworkId=121, ExtraData = null }, /*minecraft:end_stone*/
			new Item(200, 0, 1){ RuntimeId=1448507239, NetworkId=200, ExtraData = null }, /*minecraft:chorus_flower*/
			new Item(240, 0, 1){ RuntimeId=-1554584051, NetworkId=240, ExtraData = null }, /*minecraft:chorus_plant*/
			new Item(432, 0, 1){ RuntimeId=0, NetworkId=597, ExtraData = null }, /*minecraft:chorus_fruit*/
			new Item(433, 0, 1){ RuntimeId=0, NetworkId=598, ExtraData = null }, /*minecraft:popped_chorus_fruit*/
			new Item(19, 0, 1){ RuntimeId=-94662439, NetworkId=19, ExtraData = null }, /*minecraft:sponge*/
			new Item(-984, 0, 1){ RuntimeId=-1025025254, NetworkId=-984, ExtraData = null }, /**/
			new Item(-132, 0, 1){ RuntimeId=-1398918129, NetworkId=-132, ExtraData = null }, /*minecraft:coral_block*/
			new Item(-849, 0, 1){ RuntimeId=-1099594858, NetworkId=-849, ExtraData = null }, /**/
			new Item(-850, 0, 1){ RuntimeId=-1564187623, NetworkId=-850, ExtraData = null }, /**/
			new Item(-851, 0, 1){ RuntimeId=2070863273, NetworkId=-851, ExtraData = null }, /**/
			new Item(-852, 0, 1){ RuntimeId=480717650, NetworkId=-852, ExtraData = null }, /**/
			new Item(-853, 0, 1){ RuntimeId=-1507319459, NetworkId=-853, ExtraData = null }, /**/
			new Item(-854, 0, 1){ RuntimeId=-1709117648, NetworkId=-854, ExtraData = null }, /**/
			new Item(-855, 0, 1){ RuntimeId=1385671143, NetworkId=-855, ExtraData = null }, /**/
			new Item(-856, 0, 1){ RuntimeId=-819992741, NetworkId=-856, ExtraData = null }, /**/
			new Item(-857, 0, 1){ RuntimeId=-1169945792, NetworkId=-857, ExtraData = null }, /**/
			new Item(-458, 0, 1){ RuntimeId=1041212874, NetworkId=-458, ExtraData = null }, /**/
			new Item(-459, 0, 1){ RuntimeId=1467842196, NetworkId=-459, ExtraData = null }, /**/
			new Item(-460, 0, 1){ RuntimeId=-5792464, NetworkId=-460, ExtraData = null }, /**/
			new Item(-461, 0, 1){ RuntimeId=879274693, NetworkId=-461, ExtraData = null }, /**/
			new Item(-307, 0, 1){ RuntimeId=-145164616, NetworkId=-307, ExtraData = null }, /**/
			new Item(-580, 0, 1){ RuntimeId=1098378432, NetworkId=-580, ExtraData = null }, /**/
			new Item(-466, 0, 1){ RuntimeId=1769900828, NetworkId=-466, ExtraData = null }, /**/
			new Item(298, 0, 1){ RuntimeId=0, NetworkId=367, ExtraData = null }, /*minecraft:leather_helmet*/
			new Item(778, 0, 1){ RuntimeId=0, NetworkId=778, ExtraData = null }, /**/
			new Item(302, 0, 1){ RuntimeId=0, NetworkId=371, ExtraData = null }, /*minecraft:chainmail_helmet*/
			new Item(306, 0, 1){ RuntimeId=0, NetworkId=375, ExtraData = null }, /*minecraft:iron_helmet*/
			new Item(314, 0, 1){ RuntimeId=0, NetworkId=383, ExtraData = null }, /*minecraft:golden_helmet*/
			new Item(310, 0, 1){ RuntimeId=0, NetworkId=379, ExtraData = null }, /*minecraft:diamond_helmet*/
			new Item(748, 0, 1){ RuntimeId=0, NetworkId=652, ExtraData = null }, /*minecraft:netherite_helmet*/
			new Item(299, 0, 1){ RuntimeId=0, NetworkId=368, ExtraData = null }, /*minecraft:leather_chestplate*/
			new Item(779, 0, 1){ RuntimeId=0, NetworkId=779, ExtraData = null }, /**/
			new Item(303, 0, 1){ RuntimeId=0, NetworkId=372, ExtraData = null }, /*minecraft:chainmail_chestplate*/
			new Item(307, 0, 1){ RuntimeId=0, NetworkId=376, ExtraData = null }, /*minecraft:iron_chestplate*/
			new Item(315, 0, 1){ RuntimeId=0, NetworkId=384, ExtraData = null }, /*minecraft:golden_chestplate*/
			new Item(311, 0, 1){ RuntimeId=0, NetworkId=380, ExtraData = null }, /*minecraft:diamond_chestplate*/
			new Item(749, 0, 1){ RuntimeId=0, NetworkId=653, ExtraData = null }, /*minecraft:netherite_chestplate*/
			new Item(300, 0, 1){ RuntimeId=0, NetworkId=369, ExtraData = null }, /*minecraft:leather_leggings*/
			new Item(780, 0, 1){ RuntimeId=0, NetworkId=780, ExtraData = null }, /**/
			new Item(304, 0, 1){ RuntimeId=0, NetworkId=373, ExtraData = null }, /*minecraft:chainmail_leggings*/
			new Item(308, 0, 1){ RuntimeId=0, NetworkId=377, ExtraData = null }, /*minecraft:iron_leggings*/
			new Item(316, 0, 1){ RuntimeId=0, NetworkId=385, ExtraData = null }, /*minecraft:golden_leggings*/
			new Item(312, 0, 1){ RuntimeId=0, NetworkId=381, ExtraData = null }, /*minecraft:diamond_leggings*/
			new Item(750, 0, 1){ RuntimeId=0, NetworkId=654, ExtraData = null }, /*minecraft:netherite_leggings*/
			new Item(301, 0, 1){ RuntimeId=0, NetworkId=370, ExtraData = null }, /*minecraft:leather_boots*/
			new Item(781, 0, 1){ RuntimeId=0, NetworkId=781, ExtraData = null }, /**/
			new Item(305, 0, 1){ RuntimeId=0, NetworkId=374, ExtraData = null }, /*minecraft:chainmail_boots*/
			new Item(309, 0, 1){ RuntimeId=0, NetworkId=378, ExtraData = null }, /*minecraft:iron_boots*/
			new Item(317, 0, 1){ RuntimeId=0, NetworkId=386, ExtraData = null }, /*minecraft:golden_boots*/
			new Item(313, 0, 1){ RuntimeId=0, NetworkId=382, ExtraData = null }, /*minecraft:diamond_boots*/
			new Item(751, 0, 1){ RuntimeId=0, NetworkId=655, ExtraData = null }, /*minecraft:netherite_boots*/
			new Item(268, 0, 1){ RuntimeId=0, NetworkId=339, ExtraData = null }, /*minecraft:wooden_sword*/
			new Item(272, 0, 1){ RuntimeId=0, NetworkId=343, ExtraData = null }, /*minecraft:stone_sword*/
			new Item(773, 0, 1){ RuntimeId=0, NetworkId=773, ExtraData = null }, /**/
			new Item(267, 0, 1){ RuntimeId=0, NetworkId=338, ExtraData = null }, /*minecraft:iron_sword*/
			new Item(283, 0, 1){ RuntimeId=0, NetworkId=354, ExtraData = null }, /*minecraft:golden_sword*/
			new Item(276, 0, 1){ RuntimeId=0, NetworkId=347, ExtraData = null }, /*minecraft:diamond_sword*/
			new Item(743, 0, 1){ RuntimeId=0, NetworkId=646, ExtraData = null }, /*minecraft:netherite_sword*/
			new Item(263, 0, 1){ RuntimeId=0, NetworkId=263, ExtraData = null }, /*minecraft:coal*/
			new Item(262, 0, 1){ RuntimeId=0, NetworkId=262, ExtraData = null }, /*minecraft:arrow*/
			new Item(257, 0, 1){ RuntimeId=0, NetworkId=257, ExtraData = null }, /*minecraft:iron_pickaxe*/
			new Item(260, 0, 1){ RuntimeId=0, NetworkId=260, ExtraData = null }, /*minecraft:apple*/
			new Item(259, 0, 1){ RuntimeId=0, NetworkId=259, ExtraData = null }, /*minecraft:flint_and_steel*/
			new Item(258, 0, 1){ RuntimeId=0, NetworkId=258, ExtraData = null }, /*minecraft:iron_axe*/
			new Item(261, 0, 1){ RuntimeId=0, NetworkId=261, ExtraData = null }, /*minecraft:bow*/
			new Item(271, 0, 1){ RuntimeId=0, NetworkId=342, ExtraData = null }, /*minecraft:wooden_axe*/
			new Item(275, 0, 1){ RuntimeId=0, NetworkId=346, ExtraData = null }, /*minecraft:stone_axe*/
			new Item(776, 0, 1){ RuntimeId=0, NetworkId=776, ExtraData = null }, /**/
			new Item(258, 0, 1){ RuntimeId=0, NetworkId=329, ExtraData = null }, /*minecraft:iron_axe*/
			new Item(286, 0, 1){ RuntimeId=0, NetworkId=357, ExtraData = null }, /*minecraft:golden_axe*/
			new Item(279, 0, 1){ RuntimeId=0, NetworkId=350, ExtraData = null }, /*minecraft:diamond_axe*/
			new Item(746, 0, 1){ RuntimeId=0, NetworkId=649, ExtraData = null }, /*minecraft:netherite_axe*/
			new Item(270, 0, 1){ RuntimeId=0, NetworkId=341, ExtraData = null }, /*minecraft:wooden_pickaxe*/
			new Item(274, 0, 1){ RuntimeId=0, NetworkId=345, ExtraData = null }, /*minecraft:stone_pickaxe*/
			new Item(775, 0, 1){ RuntimeId=0, NetworkId=775, ExtraData = null }, /**/
			new Item(257, 0, 1){ RuntimeId=0, NetworkId=328, ExtraData = null }, /*minecraft:iron_pickaxe*/
			new Item(285, 0, 1){ RuntimeId=0, NetworkId=356, ExtraData = null }, /*minecraft:golden_pickaxe*/
			new Item(278, 0, 1){ RuntimeId=0, NetworkId=349, ExtraData = null }, /*minecraft:diamond_pickaxe*/
			new Item(745, 0, 1){ RuntimeId=0, NetworkId=648, ExtraData = null }, /*minecraft:netherite_pickaxe*/
			new Item(269, 0, 1){ RuntimeId=0, NetworkId=340, ExtraData = null }, /*minecraft:wooden_shovel*/
			new Item(273, 0, 1){ RuntimeId=0, NetworkId=344, ExtraData = null }, /*minecraft:stone_shovel*/
			new Item(774, 0, 1){ RuntimeId=0, NetworkId=774, ExtraData = null }, /**/
			new Item(256, 0, 1){ RuntimeId=0, NetworkId=327, ExtraData = null }, /*minecraft:iron_shovel*/
			new Item(284, 0, 1){ RuntimeId=0, NetworkId=355, ExtraData = null }, /*minecraft:golden_shovel*/
			new Item(277, 0, 1){ RuntimeId=0, NetworkId=348, ExtraData = null }, /*minecraft:diamond_shovel*/
			new Item(744, 0, 1){ RuntimeId=0, NetworkId=647, ExtraData = null }, /*minecraft:netherite_shovel*/
			new Item(290, 0, 1){ RuntimeId=0, NetworkId=361, ExtraData = null }, /*minecraft:wooden_hoe*/
			new Item(291, 0, 1){ RuntimeId=0, NetworkId=362, ExtraData = null }, /*minecraft:stone_hoe*/
			new Item(777, 0, 1){ RuntimeId=0, NetworkId=777, ExtraData = null }, /**/
			new Item(292, 0, 1){ RuntimeId=0, NetworkId=363, ExtraData = null }, /*minecraft:iron_hoe*/
			new Item(294, 0, 1){ RuntimeId=0, NetworkId=365, ExtraData = null }, /*minecraft:golden_hoe*/
			new Item(293, 0, 1){ RuntimeId=0, NetworkId=364, ExtraData = null }, /*minecraft:diamond_hoe*/
			new Item(747, 0, 1){ RuntimeId=0, NetworkId=650, ExtraData = null }, /*minecraft:netherite_hoe*/
			new Item(261, 0, 1){ RuntimeId=0, NetworkId=331, ExtraData = null }, /*minecraft:bow*/
			new Item(471, 0, 1){ RuntimeId=0, NetworkId=614, ExtraData = null }, /*minecraft:crossbow*/
			new Item(351, 0, 1){ RuntimeId=0, NetworkId=351, ExtraData = null }, /*minecraft:dye*/
			new Item(455, 0, 1){ RuntimeId=0, NetworkId=585, ExtraData = null }, /*minecraft:trident*/
			new Item(262, 0, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 6, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 7, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 8, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 9, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 10, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 11, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 12, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 13, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 14, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 15, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 16, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 17, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 18, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 19, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 20, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 21, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 22, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 23, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 24, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 25, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 26, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 27, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 28, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 29, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 30, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 31, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 32, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 33, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 34, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 35, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 36, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 37, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 38, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 39, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 40, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 41, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 42, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 43, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 44, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 45, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 46, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(262, 47, 1){ RuntimeId=0, NetworkId=332, ExtraData = null }, /*minecraft:arrow*/
			new Item(513, 0, 1){ RuntimeId=0, NetworkId=387, ExtraData = null }, /*minecraft:shield*/
			new Item(366, 0, 1){ RuntimeId=0, NetworkId=305, ExtraData = null }, /*minecraft:cooked_chicken*/
			new Item(320, 0, 1){ RuntimeId=0, NetworkId=292, ExtraData = null }, /*minecraft:cooked_porkchop*/
			new Item(364, 0, 1){ RuntimeId=0, NetworkId=303, ExtraData = null }, /*minecraft:cooked_beef*/
			new Item(424, 0, 1){ RuntimeId=0, NetworkId=590, ExtraData = null }, /*minecraft:cooked_mutton*/
			new Item(412, 0, 1){ RuntimeId=0, NetworkId=318, ExtraData = null }, /*minecraft:cooked_rabbit*/
			new Item(350, 0, 1){ RuntimeId=0, NetworkId=297, ExtraData = null }, /*minecraft:cooked_cod*/
			new Item(463, 0, 1){ RuntimeId=0, NetworkId=298, ExtraData = null }, /*minecraft:cooked_salmon*/
			new Item(297, 0, 1){ RuntimeId=0, NetworkId=290, ExtraData = null }, /*minecraft:bread*/
			new Item(282, 0, 1){ RuntimeId=0, NetworkId=289, ExtraData = null }, /*minecraft:mushroom_stew*/
			new Item(459, 0, 1){ RuntimeId=0, NetworkId=315, ExtraData = null }, /*minecraft:beetroot_soup*/
			new Item(413, 0, 1){ RuntimeId=0, NetworkId=319, ExtraData = null }, /*minecraft:rabbit_stew*/
			new Item(734, 0, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 1, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 2, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 3, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 4, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 5, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 6, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 7, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 8, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 9, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 10, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 11, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(734, 12, 1){ RuntimeId=0, NetworkId=631, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(393, 0, 1){ RuntimeId=0, NetworkId=310, ExtraData = null }, /*minecraft:baked_potato*/
			new Item(357, 0, 1){ RuntimeId=0, NetworkId=300, ExtraData = null }, /*minecraft:cookie*/
			new Item(400, 0, 1){ RuntimeId=0, NetworkId=313, ExtraData = null }, /*minecraft:pumpkin_pie*/
			new Item(354, 0, 1){ RuntimeId=0, NetworkId=449, ExtraData = null }, /*minecraft:cake*/
			new Item(464, 0, 1){ RuntimeId=0, NetworkId=299, ExtraData = null }, /*minecraft:dried_kelp*/
			new Item(346, 0, 1){ RuntimeId=0, NetworkId=424, ExtraData = null }, /*minecraft:fishing_rod*/
			new Item(398, 0, 1){ RuntimeId=0, NetworkId=556, ExtraData = null }, /*minecraft:carrot_on_a_stick*/
			new Item(757, 0, 1){ RuntimeId=0, NetworkId=661, ExtraData = null }, /*minecraft:warped_fungus_on_a_stick*/
			new Item(332, 0, 1){ RuntimeId=0, NetworkId=406, ExtraData = null }, /*minecraft:snowball*/
			new Item(284, 0, 1){ RuntimeId=0, NetworkId=284, ExtraData = null }, /*minecraft:golden_shovel*/
			new Item(359, 0, 1){ RuntimeId=0, NetworkId=453, ExtraData = null }, /*minecraft:shears*/
			new Item(259, 0, 1){ RuntimeId=0, NetworkId=330, ExtraData = null }, /*minecraft:flint_and_steel*/
			new Item(420, 0, 1){ RuntimeId=0, NetworkId=586, ExtraData = null }, /*minecraft:lead*/
			new Item(347, 0, 1){ RuntimeId=0, NetworkId=425, ExtraData = null }, /*minecraft:clock*/
			new Item(345, 0, 1){ RuntimeId=0, NetworkId=423, ExtraData = null }, /*minecraft:compass*/
			new Item(688, 0, 1){ RuntimeId=0, NetworkId=688, ExtraData = null }, /**/
			new Item(669, 0, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 1, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 2, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 3, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 4, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 5, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 6, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(669, 7, 1){ RuntimeId=0, NetworkId=669, ExtraData = null }, /**/
			new Item(395, 0, 1){ RuntimeId=0, NetworkId=555, ExtraData = null }, /*minecraft:empty_map*/
			new Item(395, 2, 1){ RuntimeId=0, NetworkId=555, ExtraData = null }, /*minecraft:empty_map*/
			new Item(329, 0, 1){ RuntimeId=0, NetworkId=403, ExtraData = null }, /*minecraft:saddle*/
			new Item(770, 0, 1){ RuntimeId=0, NetworkId=770, ExtraData = null }, /**/
			new Item(763, 0, 1){ RuntimeId=0, NetworkId=763, ExtraData = null }, /**/
			new Item(760, 0, 1){ RuntimeId=0, NetworkId=760, ExtraData = null }, /*minecraft:nether_sprouts*/
			new Item(756, 0, 1){ RuntimeId=0, NetworkId=756, ExtraData = null }, /*minecraft:warped_door*/
			new Item(758, 0, 1){ RuntimeId=0, NetworkId=758, ExtraData = null }, /*minecraft:chain*/
			new Item(769, 0, 1){ RuntimeId=0, NetworkId=769, ExtraData = null }, /**/
			new Item(766, 0, 1){ RuntimeId=0, NetworkId=766, ExtraData = null }, /**/
			new Item(771, 0, 1){ RuntimeId=0, NetworkId=771, ExtraData = null }, /**/
			new Item(764, 0, 1){ RuntimeId=0, NetworkId=764, ExtraData = null }, /**/
			new Item(761, 0, 1){ RuntimeId=0, NetworkId=761, ExtraData = null }, /**/
			new Item(759, 0, 1){ RuntimeId=0, NetworkId=759, ExtraData = null }, /*minecraft:music_disc_pigstep*/
			new Item(762, 0, 1){ RuntimeId=0, NetworkId=762, ExtraData = null }, /**/
			new Item(757, 0, 1){ RuntimeId=0, NetworkId=757, ExtraData = null }, /*minecraft:warped_fungus_on_a_stick*/
			new Item(768, 0, 1){ RuntimeId=0, NetworkId=768, ExtraData = null }, /**/
			new Item(765, 0, 1){ RuntimeId=0, NetworkId=765, ExtraData = null }, /**/
			new Item(767, 0, 1){ RuntimeId=0, NetworkId=767, ExtraData = null }, /**/
			new Item(267, 0, 1){ RuntimeId=0, NetworkId=267, ExtraData = null }, /*minecraft:iron_sword*/
			new Item(279, 0, 1){ RuntimeId=0, NetworkId=279, ExtraData = null }, /*minecraft:diamond_axe*/
			new Item(272, 0, 1){ RuntimeId=0, NetworkId=272, ExtraData = null }, /*minecraft:stone_sword*/
			new Item(269, 0, 1){ RuntimeId=0, NetworkId=269, ExtraData = null }, /*minecraft:wooden_shovel*/
			new Item(264, 0, 1){ RuntimeId=0, NetworkId=264, ExtraData = null }, /*minecraft:diamond*/
			new Item(266, 0, 1){ RuntimeId=0, NetworkId=266, ExtraData = null }, /*minecraft:gold_ingot*/
			new Item(278, 0, 1){ RuntimeId=0, NetworkId=278, ExtraData = null }, /*minecraft:diamond_pickaxe*/
			new Item(275, 0, 1){ RuntimeId=0, NetworkId=275, ExtraData = null }, /*minecraft:stone_axe*/
			new Item(280, 0, 1){ RuntimeId=0, NetworkId=280, ExtraData = null }, /*minecraft:stick*/
			new Item(273, 0, 1){ RuntimeId=0, NetworkId=273, ExtraData = null }, /*minecraft:stone_shovel*/
			new Item(270, 0, 1){ RuntimeId=0, NetworkId=270, ExtraData = null }, /*minecraft:wooden_pickaxe*/
			new Item(268, 0, 1){ RuntimeId=0, NetworkId=268, ExtraData = null }, /*minecraft:wooden_sword*/
			new Item(271, 0, 1){ RuntimeId=0, NetworkId=271, ExtraData = null }, /*minecraft:wooden_axe*/
			new Item(265, 0, 1){ RuntimeId=0, NetworkId=265, ExtraData = null }, /*minecraft:iron_ingot*/
			new Item(277, 0, 1){ RuntimeId=0, NetworkId=277, ExtraData = null }, /*minecraft:diamond_shovel*/
			new Item(274, 0, 1){ RuntimeId=0, NetworkId=274, ExtraData = null }, /*minecraft:stone_pickaxe*/
			new Item(276, 0, 1){ RuntimeId=0, NetworkId=276, ExtraData = null }, /*minecraft:diamond_sword*/
			new Item(416, 0, 1){ RuntimeId=0, NetworkId=569, ExtraData = null }, /*minecraft:leather_horse_armor*/
			new Item(783, 0, 1){ RuntimeId=0, NetworkId=783, ExtraData = null }, /**/
			new Item(417, 0, 1){ RuntimeId=0, NetworkId=570, ExtraData = null }, /*minecraft:iron_horse_armor*/
			new Item(418, 0, 1){ RuntimeId=0, NetworkId=571, ExtraData = null }, /*minecraft:golden_horse_armor*/
			new Item(419, 0, 1){ RuntimeId=0, NetworkId=572, ExtraData = null }, /*minecraft:diamond_horse_armor*/
			new Item(792, 0, 1){ RuntimeId=0, NetworkId=792, ExtraData = null }, /**/
			new Item(747, 0, 1){ RuntimeId=0, NetworkId=747, ExtraData = null }, /*minecraft:netherite_hoe*/
			new Item(787, 0, 1){ RuntimeId=0, NetworkId=787, ExtraData = null }, /**/
			new Item(788, 0, 1){ RuntimeId=0, NetworkId=788, ExtraData = null }, /**/
			new Item(789, 0, 1){ RuntimeId=0, NetworkId=789, ExtraData = null }, /**/
			new Item(790, 0, 1){ RuntimeId=0, NetworkId=790, ExtraData = null }, /**/
			new Item(791, 0, 1){ RuntimeId=0, NetworkId=791, ExtraData = null }, /**/
			new Item(469, 0, 1){ RuntimeId=0, NetworkId=612, ExtraData = null }, /*minecraft:turtle_helmet*/
			new Item(444, 0, 1){ RuntimeId=0, NetworkId=603, ExtraData = null }, /*minecraft:elytra*/
			new Item(450, 0, 1){ RuntimeId=0, NetworkId=607, ExtraData = null }, /*minecraft:totem_of_undying*/
			new Item(374, 0, 1){ RuntimeId=0, NetworkId=460, ExtraData = null }, /*minecraft:glass_bottle*/
			new Item(384, 0, 1){ RuntimeId=0, NetworkId=548, ExtraData = null }, /*minecraft:experience_bottle*/
			new Item(373, 0, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 1, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 2, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 3, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 4, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 5, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 6, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 7, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 8, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 9, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 10, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 11, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 12, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 13, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 14, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 15, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 16, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 17, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 18, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 42, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 19, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 20, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 21, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 22, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 23, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 24, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 25, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 26, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 27, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 28, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 29, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 30, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 31, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 32, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 33, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 34, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 35, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 36, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 37, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 38, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 39, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 40, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 41, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 43, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 44, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 45, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(373, 46, 1){ RuntimeId=0, NetworkId=459, ExtraData = null }, /*minecraft:potion*/
			new Item(438, 0, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 1, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 2, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 3, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 4, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 5, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 6, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 7, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 8, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 9, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 10, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 11, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 12, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 13, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 14, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 15, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 16, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 17, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 18, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 42, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 19, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 20, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 21, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 22, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 23, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 24, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 25, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 26, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 27, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 28, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 29, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 30, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 31, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 32, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 33, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 34, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 35, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 36, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 37, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 38, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 39, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 40, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 41, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 43, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 44, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 45, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(438, 46, 1){ RuntimeId=0, NetworkId=600, ExtraData = null }, /*minecraft:splash_potion*/
			new Item(441, 0, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 1, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 2, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 3, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 4, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 5, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 6, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 7, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 8, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 9, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 10, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 11, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 12, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 13, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 14, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 15, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 16, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 17, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 18, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 42, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 19, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 20, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 21, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 22, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 23, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 24, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 25, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 26, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 27, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 28, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 29, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 30, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 31, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 32, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 33, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 34, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 35, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 36, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 37, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 38, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 39, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 40, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 41, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 43, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 44, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 45, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(441, 46, 1){ RuntimeId=0, NetworkId=601, ExtraData = null }, /*minecraft:lingering_potion*/
			new Item(634, 0, 1){ RuntimeId=0, NetworkId=634, ExtraData = null }, /**/
			new Item(634, 1, 1){ RuntimeId=0, NetworkId=634, ExtraData = null }, /**/
			new Item(634, 2, 1){ RuntimeId=0, NetworkId=634, ExtraData = null }, /**/
			new Item(634, 3, 1){ RuntimeId=0, NetworkId=634, ExtraData = null }, /**/
			new Item(634, 4, 1){ RuntimeId=0, NetworkId=634, ExtraData = null }, /**/
			new Item(667, 0, 1){ RuntimeId=0, NetworkId=667, ExtraData = null }, /**/
			new Item(722, 0, 1){ RuntimeId=0, NetworkId=722, ExtraData = null }, /**/
			new Item(280, 0, 1){ RuntimeId=0, NetworkId=352, ExtraData = null }, /*minecraft:stick*/
			new Item(355, 0, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 8, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 7, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 15, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 12, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 14, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 1, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 4, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 5, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 13, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 9, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 3, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 11, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 10, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 2, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(355, 6, 1){ RuntimeId=0, NetworkId=450, ExtraData = null }, /*minecraft:bed*/
			new Item(50, 0, 1){ RuntimeId=-1188029192, NetworkId=50, ExtraData = null }, /*minecraft:torch*/
			new Item(-268, 0, 1){ RuntimeId=189951137, NetworkId=-268, ExtraData = null }, /*minecraft:soul_torch*/
			new Item(-1082, 0, 1){ RuntimeId=192591789, NetworkId=-1082, ExtraData = null }, /**/
			new Item(-156, 0, 1){ RuntimeId=845428104, NetworkId=-156, ExtraData = null }, /*minecraft:sea_pickle*/
			new Item(-208, 0, 1){ RuntimeId=-217346716, NetworkId=-208, ExtraData = null }, /*minecraft:lantern*/
			new Item(-269, 0, 1){ RuntimeId=-952113049, NetworkId=-269, ExtraData = null }, /*minecraft:soul_lantern*/
			new Item(-1083, 0, 1){ RuntimeId=639828303, NetworkId=-1083, ExtraData = null }, /**/
			new Item(-1084, 0, 1){ RuntimeId=-2132343142, NetworkId=-1084, ExtraData = null }, /**/
			new Item(-1085, 0, 1){ RuntimeId=-1239845425, NetworkId=-1085, ExtraData = null }, /**/
			new Item(-1086, 0, 1){ RuntimeId=2084297703, NetworkId=-1086, ExtraData = null }, /**/
			new Item(-1087, 0, 1){ RuntimeId=-908847977, NetworkId=-1087, ExtraData = null }, /**/
			new Item(-1088, 0, 1){ RuntimeId=1432913586, NetworkId=-1088, ExtraData = null }, /**/
			new Item(-1089, 0, 1){ RuntimeId=-2051646321, NetworkId=-1089, ExtraData = null }, /**/
			new Item(-1090, 0, 1){ RuntimeId=1918408151, NetworkId=-1090, ExtraData = null }, /**/
			new Item(-412, 0, 1){ RuntimeId=221249915, NetworkId=-412, ExtraData = null }, /**/
			new Item(-413, 0, 1){ RuntimeId=1754953123, NetworkId=-413, ExtraData = null }, /**/
			new Item(-414, 0, 1){ RuntimeId=910120863, NetworkId=-414, ExtraData = null }, /**/
			new Item(-415, 0, 1){ RuntimeId=-1853025801, NetworkId=-415, ExtraData = null }, /**/
			new Item(-416, 0, 1){ RuntimeId=-227381798, NetworkId=-416, ExtraData = null }, /**/
			new Item(-417, 0, 1){ RuntimeId=-1455870595, NetworkId=-417, ExtraData = null }, /**/
			new Item(-418, 0, 1){ RuntimeId=-382860000, NetworkId=-418, ExtraData = null }, /**/
			new Item(-419, 0, 1){ RuntimeId=-462981925, NetworkId=-419, ExtraData = null }, /**/
			new Item(-420, 0, 1){ RuntimeId=147970782, NetworkId=-420, ExtraData = null }, /**/
			new Item(-421, 0, 1){ RuntimeId=2060361839, NetworkId=-421, ExtraData = null }, /**/
			new Item(-422, 0, 1){ RuntimeId=957383270, NetworkId=-422, ExtraData = null }, /**/
			new Item(-423, 0, 1){ RuntimeId=-2121454425, NetworkId=-423, ExtraData = null }, /**/
			new Item(-424, 0, 1){ RuntimeId=1088625327, NetworkId=-424, ExtraData = null }, /**/
			new Item(-425, 0, 1){ RuntimeId=1980081316, NetworkId=-425, ExtraData = null }, /**/
			new Item(-426, 0, 1){ RuntimeId=1000092441, NetworkId=-426, ExtraData = null }, /**/
			new Item(-427, 0, 1){ RuntimeId=114257755, NetworkId=-427, ExtraData = null }, /**/
			new Item(-428, 0, 1){ RuntimeId=-509686055, NetworkId=-428, ExtraData = null }, /**/
			new Item(58, 0, 1){ RuntimeId=1752181952, NetworkId=58, ExtraData = null }, /*minecraft:crafting_table*/
			new Item(-200, 0, 1){ RuntimeId=863215907, NetworkId=-200, ExtraData = null }, /*minecraft:cartography_table*/
			new Item(-201, 0, 1){ RuntimeId=1247520413, NetworkId=-201, ExtraData = null }, /*minecraft:fletching_table*/
			new Item(-202, 0, 1){ RuntimeId=-855547043, NetworkId=-202, ExtraData = null }, /*minecraft:smithing_table*/
			new Item(-219, 0, 1){ RuntimeId=450333055, NetworkId=-219, ExtraData = null }, /*minecraft:beehive*/
			new Item(-529, 0, 1){ RuntimeId=-2140609367, NetworkId=-529, ExtraData = null }, /**/
			new Item(-573, 0, 1){ RuntimeId=623036, NetworkId=-573, ExtraData = null }, /**/
			new Item(720, 0, 1){ RuntimeId=0, NetworkId=630, ExtraData = null }, /*minecraft:campfire*/
			new Item(801, 0, 1){ RuntimeId=0, NetworkId=664, ExtraData = null }, /*minecraft:soul_campfire*/
			new Item(61, 0, 1){ RuntimeId=-831469991, NetworkId=61, ExtraData = null }, /*minecraft:furnace*/
			new Item(-196, 0, 1){ RuntimeId=2142573020, NetworkId=-196, ExtraData = null }, /*minecraft:blast_furnace*/
			new Item(-198, 0, 1){ RuntimeId=-859788187, NetworkId=-198, ExtraData = null }, /*minecraft:smoker*/
			new Item(-272, 0, 1){ RuntimeId=1763447706, NetworkId=-272, ExtraData = null }, /*minecraft:respawn_anchor*/
			new Item(379, 0, 1){ RuntimeId=0, NetworkId=464, ExtraData = null }, /*minecraft:brewing_stand*/
			new Item(145, 0, 1){ RuntimeId=-1882358615, NetworkId=145, ExtraData = null }, /*minecraft:anvil*/
			new Item(-959, 0, 1){ RuntimeId=-1801922743, NetworkId=-959, ExtraData = null }, /**/
			new Item(-960, 0, 1){ RuntimeId=-467540639, NetworkId=-960, ExtraData = null }, /**/
			new Item(-195, 0, 1){ RuntimeId=1003295416, NetworkId=-195, ExtraData = null }, /*minecraft:grindstone*/
			new Item(116, 0, 1){ RuntimeId=1230080101, NetworkId=116, ExtraData = null }, /*minecraft:enchanting_table*/
			new Item(47, 0, 1){ RuntimeId=-1933568489, NetworkId=47, ExtraData = null }, /*minecraft:bookshelf*/
			new Item(-526, 0, 1){ RuntimeId=-1833473142, NetworkId=-526, ExtraData = null }, /**/
			new Item(-1047, 0, 1){ RuntimeId=-413905954, NetworkId=-1047, ExtraData = null }, /**/
			new Item(-1048, 0, 1){ RuntimeId=400564380, NetworkId=-1048, ExtraData = null }, /**/
			new Item(-1049, 0, 1){ RuntimeId=-25467123, NetworkId=-1049, ExtraData = null }, /**/
			new Item(-1050, 0, 1){ RuntimeId=-1734164151, NetworkId=-1050, ExtraData = null }, /**/
			new Item(-1051, 0, 1){ RuntimeId=447055332, NetworkId=-1051, ExtraData = null }, /**/
			new Item(-1052, 0, 1){ RuntimeId=814593406, NetworkId=-1052, ExtraData = null }, /**/
			new Item(-1053, 0, 1){ RuntimeId=272314173, NetworkId=-1053, ExtraData = null }, /**/
			new Item(-1054, 0, 1){ RuntimeId=394363157, NetworkId=-1054, ExtraData = null }, /**/
			new Item(-1055, 0, 1){ RuntimeId=1395370190, NetworkId=-1055, ExtraData = null }, /**/
			new Item(-1056, 0, 1){ RuntimeId=1680282726, NetworkId=-1056, ExtraData = null }, /**/
			new Item(-1057, 0, 1){ RuntimeId=1385924850, NetworkId=-1057, ExtraData = null }, /**/
			new Item(-1058, 0, 1){ RuntimeId=-1442565541, NetworkId=-1058, ExtraData = null }, /**/
			new Item(-194, 0, 1){ RuntimeId=-940563080, NetworkId=-194, ExtraData = null }, /*minecraft:lectern*/
			new Item(380, 0, 1){ RuntimeId=0, NetworkId=465, ExtraData = null }, /*minecraft:cauldron*/
			new Item(-213, 0, 1){ RuntimeId=787090290, NetworkId=-213, ExtraData = null }, /*minecraft:composter*/
			new Item(54, 0, 1){ RuntimeId=-1132117234, NetworkId=54, ExtraData = null }, /*minecraft:chest*/
			new Item(146, 0, 1){ RuntimeId=828777955, NetworkId=146, ExtraData = null }, /*minecraft:trapped_chest*/
			new Item(130, 0, 1){ RuntimeId=1239582919, NetworkId=130, ExtraData = null }, /*minecraft:ender_chest*/
			new Item(-1031, 0, 1){ RuntimeId=-327497521, NetworkId=-1031, ExtraData = null }, /**/
			new Item(-1032, 0, 1){ RuntimeId=987437688, NetworkId=-1032, ExtraData = null }, /**/
			new Item(-1033, 0, 1){ RuntimeId=-710286221, NetworkId=-1033, ExtraData = null }, /**/
			new Item(-1034, 0, 1){ RuntimeId=-1538055545, NetworkId=-1034, ExtraData = null }, /**/
			new Item(-1035, 0, 1){ RuntimeId=-1874727137, NetworkId=-1035, ExtraData = null }, /**/
			new Item(-1036, 0, 1){ RuntimeId=-500711928, NetworkId=-1036, ExtraData = null }, /**/
			new Item(-1037, 0, 1){ RuntimeId=998383803, NetworkId=-1037, ExtraData = null }, /**/
			new Item(-1038, 0, 1){ RuntimeId=1084588487, NetworkId=-1038, ExtraData = null }, /**/
			new Item(-203, 0, 1){ RuntimeId=198111737, NetworkId=-203, ExtraData = null }, /*minecraft:barrel*/
			new Item(205, 0, 1){ RuntimeId=-1647957089, NetworkId=205, ExtraData = null }, /*minecraft:undyed_shulker_box*/
			new Item(218, 0, 1){ RuntimeId=-367808114, NetworkId=218, ExtraData = null }, /*minecraft:shulker_box*/
			new Item(-620, 0, 1){ RuntimeId=-191146878, NetworkId=-620, ExtraData = null }, /**/
			new Item(-619, 0, 1){ RuntimeId=382425271, NetworkId=-619, ExtraData = null }, /**/
			new Item(-627, 0, 1){ RuntimeId=887979100, NetworkId=-627, ExtraData = null }, /**/
			new Item(-624, 0, 1){ RuntimeId=1634303081, NetworkId=-624, ExtraData = null }, /**/
			new Item(-626, 0, 1){ RuntimeId=670752730, NetworkId=-626, ExtraData = null }, /**/
			new Item(-613, 0, 1){ RuntimeId=-1730998678, NetworkId=-613, ExtraData = null }, /**/
			new Item(-616, 0, 1){ RuntimeId=642416912, NetworkId=-616, ExtraData = null }, /**/
			new Item(-617, 0, 1){ RuntimeId=-1431668663, NetworkId=-617, ExtraData = null }, /**/
			new Item(-625, 0, 1){ RuntimeId=-2052548404, NetworkId=-625, ExtraData = null }, /**/
			new Item(-621, 0, 1){ RuntimeId=-2074576185, NetworkId=-621, ExtraData = null }, /**/
			new Item(-615, 0, 1){ RuntimeId=885329687, NetworkId=-615, ExtraData = null }, /**/
			new Item(-623, 0, 1){ RuntimeId=1525866702, NetworkId=-623, ExtraData = null }, /**/
			new Item(-622, 0, 1){ RuntimeId=1625601618, NetworkId=-622, ExtraData = null }, /**/
			new Item(-614, 0, 1){ RuntimeId=1407699698, NetworkId=-614, ExtraData = null }, /**/
			new Item(-618, 0, 1){ RuntimeId=-479916018, NetworkId=-618, ExtraData = null }, /**/
			new Item(425, 0, 1){ RuntimeId=0, NetworkId=591, ExtraData = null }, /*minecraft:armor_stand*/
			new Item(-1039, 0, 1){ RuntimeId=-1021733596, NetworkId=-1039, ExtraData = null }, /**/
			new Item(-1040, 0, 1){ RuntimeId=1150593401, NetworkId=-1040, ExtraData = null }, /**/
			new Item(-1041, 0, 1){ RuntimeId=-1533250616, NetworkId=-1041, ExtraData = null }, /**/
			new Item(-1042, 0, 1){ RuntimeId=2018082108, NetworkId=-1042, ExtraData = null }, /**/
			new Item(-1043, 0, 1){ RuntimeId=262768116, NetworkId=-1043, ExtraData = null }, /**/
			new Item(-1044, 0, 1){ RuntimeId=1181619721, NetworkId=-1044, ExtraData = null }, /**/
			new Item(-1045, 0, 1){ RuntimeId=-1394923576, NetworkId=-1045, ExtraData = null }, /**/
			new Item(-1046, 0, 1){ RuntimeId=-1313165940, NetworkId=-1046, ExtraData = null }, /**/
			new Item(25, 0, 1){ RuntimeId=166024317, NetworkId=25, ExtraData = null }, /*minecraft:noteblock*/
			new Item(84, 0, 1){ RuntimeId=1605519270, NetworkId=84, ExtraData = null }, /*minecraft:jukebox*/
			new Item(500, 0, 1){ RuntimeId=0, NetworkId=573, ExtraData = null }, /*minecraft:music_disc_13*/
			new Item(501, 0, 1){ RuntimeId=0, NetworkId=574, ExtraData = null }, /*minecraft:music_disc_cat*/
			new Item(502, 0, 1){ RuntimeId=0, NetworkId=575, ExtraData = null }, /*minecraft:music_disc_blocks*/
			new Item(503, 0, 1){ RuntimeId=0, NetworkId=576, ExtraData = null }, /*minecraft:music_disc_chirp*/
			new Item(504, 0, 1){ RuntimeId=0, NetworkId=577, ExtraData = null }, /*minecraft:music_disc_far*/
			new Item(505, 0, 1){ RuntimeId=0, NetworkId=578, ExtraData = null }, /*minecraft:music_disc_mall*/
			new Item(506, 0, 1){ RuntimeId=0, NetworkId=579, ExtraData = null }, /*minecraft:music_disc_mellohi*/
			new Item(507, 0, 1){ RuntimeId=0, NetworkId=580, ExtraData = null }, /*minecraft:music_disc_stal*/
			new Item(508, 0, 1){ RuntimeId=0, NetworkId=581, ExtraData = null }, /*minecraft:music_disc_strad*/
			new Item(509, 0, 1){ RuntimeId=0, NetworkId=582, ExtraData = null }, /*minecraft:music_disc_ward*/
			new Item(510, 0, 1){ RuntimeId=0, NetworkId=583, ExtraData = null }, /*minecraft:music_disc_11*/
			new Item(511, 0, 1){ RuntimeId=0, NetworkId=584, ExtraData = null }, /*minecraft:music_disc_wait*/
			new Item(668, 0, 1){ RuntimeId=0, NetworkId=668, ExtraData = null }, /**/
			new Item(678, 0, 1){ RuntimeId=0, NetworkId=678, ExtraData = null }, /**/
			new Item(759, 0, 1){ RuntimeId=0, NetworkId=662, ExtraData = null }, /*minecraft:music_disc_pigstep*/
			new Item(742, 0, 1){ RuntimeId=0, NetworkId=742, ExtraData = null }, /*minecraft:netherite_ingot*/
			new Item(828, 0, 1){ RuntimeId=0, NetworkId=828, ExtraData = null }, /**/
			new Item(829, 0, 1){ RuntimeId=0, NetworkId=829, ExtraData = null }, /**/
			new Item(830, 0, 1){ RuntimeId=0, NetworkId=830, ExtraData = null }, /**/
			new Item(831, 0, 1){ RuntimeId=0, NetworkId=831, ExtraData = null }, /**/
			new Item(832, 0, 1){ RuntimeId=0, NetworkId=832, ExtraData = null }, /**/
			new Item(833, 0, 1){ RuntimeId=0, NetworkId=833, ExtraData = null }, /**/
			new Item(679, 0, 1){ RuntimeId=0, NetworkId=679, ExtraData = null }, /**/
			new Item(348, 0, 1){ RuntimeId=0, NetworkId=426, ExtraData = null }, /*minecraft:glowstone_dust*/
			new Item(89, 0, 1){ RuntimeId=-2040923292, NetworkId=89, ExtraData = null }, /*minecraft:glowstone*/
			new Item(123, 0, 1){ RuntimeId=670839919, NetworkId=123, ExtraData = null }, /*minecraft:redstone_lamp*/
			new Item(169, 0, 1){ RuntimeId=-925502508, NetworkId=169, ExtraData = null }, /*minecraft:seaLantern*/
			new Item(323, 0, 1){ RuntimeId=0, NetworkId=390, ExtraData = null }, /*minecraft:oak_sign*/
			new Item(472, 0, 1){ RuntimeId=0, NetworkId=615, ExtraData = null }, /*minecraft:spruce_sign*/
			new Item(473, 0, 1){ RuntimeId=0, NetworkId=616, ExtraData = null }, /*minecraft:birch_sign*/
			new Item(474, 0, 1){ RuntimeId=0, NetworkId=617, ExtraData = null }, /*minecraft:jungle_sign*/
			new Item(475, 0, 1){ RuntimeId=0, NetworkId=618, ExtraData = null }, /*minecraft:acacia_sign*/
			new Item(476, 0, 1){ RuntimeId=0, NetworkId=619, ExtraData = null }, /*minecraft:dark_oak_sign*/
			new Item(676, 0, 1){ RuntimeId=0, NetworkId=676, ExtraData = null }, /**/
			new Item(693, 0, 1){ RuntimeId=0, NetworkId=693, ExtraData = null }, /**/
			new Item(750, 0, 1){ RuntimeId=0, NetworkId=750, ExtraData = null }, /*minecraft:netherite_leggings*/
			new Item(694, 0, 1){ RuntimeId=0, NetworkId=694, ExtraData = null }, /**/
			new Item(753, 0, 1){ RuntimeId=0, NetworkId=657, ExtraData = null }, /*minecraft:crimson_sign*/
			new Item(754, 0, 1){ RuntimeId=0, NetworkId=658, ExtraData = null }, /*minecraft:warped_sign*/
			new Item(-500, 0, 1){ RuntimeId=0, NetworkId=-500, ExtraData = null }, /**/
			new Item(-501, 0, 1){ RuntimeId=0, NetworkId=-501, ExtraData = null }, /**/
			new Item(-502, 0, 1){ RuntimeId=0, NetworkId=-502, ExtraData = null }, /**/
			new Item(-503, 0, 1){ RuntimeId=0, NetworkId=-503, ExtraData = null }, /**/
			new Item(-504, 0, 1){ RuntimeId=0, NetworkId=-504, ExtraData = null }, /**/
			new Item(-505, 0, 1){ RuntimeId=0, NetworkId=-505, ExtraData = null }, /**/
			new Item(-508, 0, 1){ RuntimeId=0, NetworkId=-508, ExtraData = null }, /**/
			new Item(-534, 0, 1){ RuntimeId=0, NetworkId=-534, ExtraData = null }, /**/
			new Item(-993, 0, 1){ RuntimeId=0, NetworkId=-993, ExtraData = null }, /**/
			new Item(-522, 0, 1){ RuntimeId=0, NetworkId=-522, ExtraData = null }, /**/
			new Item(-506, 0, 1){ RuntimeId=0, NetworkId=-506, ExtraData = null }, /**/
			new Item(-507, 0, 1){ RuntimeId=0, NetworkId=-507, ExtraData = null }, /**/
			new Item(321, 0, 1){ RuntimeId=0, NetworkId=389, ExtraData = null }, /*minecraft:painting*/
			new Item(389, 0, 1){ RuntimeId=0, NetworkId=553, ExtraData = null }, /*minecraft:frame*/
			new Item(665, 0, 1){ RuntimeId=0, NetworkId=665, ExtraData = null }, /**/
			new Item(737, 0, 1){ RuntimeId=0, NetworkId=633, ExtraData = null }, /*minecraft:honey_bottle*/
			new Item(390, 0, 1){ RuntimeId=0, NetworkId=554, ExtraData = null }, /*minecraft:flower_pot*/
			new Item(281, 0, 1){ RuntimeId=0, NetworkId=353, ExtraData = null }, /*minecraft:bowl*/
			new Item(325, 0, 1){ RuntimeId=0, NetworkId=392, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 1, 1){ RuntimeId=0, NetworkId=393, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 8, 1){ RuntimeId=0, NetworkId=394, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 10, 1){ RuntimeId=0, NetworkId=395, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 2, 1){ RuntimeId=0, NetworkId=396, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 3, 1){ RuntimeId=0, NetworkId=397, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 4, 1){ RuntimeId=0, NetworkId=398, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 5, 1){ RuntimeId=0, NetworkId=399, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 11, 1){ RuntimeId=0, NetworkId=400, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 12, 1){ RuntimeId=0, NetworkId=401, ExtraData = null }, /*minecraft:bucket*/
			new Item(325, 13, 1){ RuntimeId=0, NetworkId=672, ExtraData = null }, /*minecraft:bucket*/
			new Item(794, 0, 1){ RuntimeId=0, NetworkId=794, ExtraData = null }, /**/
			new Item(-967, 0, 1){ RuntimeId=-1474830235, NetworkId=-967, ExtraData = null }, /**/
			new Item(-966, 0, 1){ RuntimeId=235396048, NetworkId=-966, ExtraData = null }, /**/
			new Item(-968, 0, 1){ RuntimeId=1989274695, NetworkId=-968, ExtraData = null }, /**/
			new Item(-969, 0, 1){ RuntimeId=-1226950401, NetworkId=-969, ExtraData = null }, /**/
			new Item(144, 0, 1){ RuntimeId=1872108285, NetworkId=144, ExtraData = null }, /*minecraft:skull*/
			new Item(-965, 0, 1){ RuntimeId=-98863466, NetworkId=-965, ExtraData = null }, /**/
			new Item(-970, 0, 1){ RuntimeId=1460883779, NetworkId=-970, ExtraData = null }, /**/
			new Item(-312, 0, 1){ RuntimeId=1504023697, NetworkId=-312, ExtraData = null }, /**/
			new Item(-1059, 0, 1){ RuntimeId=-1490803732, NetworkId=-1059, ExtraData = null }, /**/
			new Item(-1060, 0, 1){ RuntimeId=-271629079, NetworkId=-1060, ExtraData = null }, /**/
			new Item(-1061, 0, 1){ RuntimeId=1704554945, NetworkId=-1061, ExtraData = null }, /**/
			new Item(-1062, 0, 1){ RuntimeId=1910125425, NetworkId=-1062, ExtraData = null }, /**/
			new Item(-1063, 0, 1){ RuntimeId=-1056375380, NetworkId=-1063, ExtraData = null }, /**/
			new Item(-1064, 0, 1){ RuntimeId=-1524063783, NetworkId=-1064, ExtraData = null }, /**/
			new Item(-1065, 0, 1){ RuntimeId=-1343192615, NetworkId=-1065, ExtraData = null }, /**/
			new Item(138, 0, 1){ RuntimeId=561914719, NetworkId=138, ExtraData = null }, /*minecraft:beacon*/
			new Item(-206, 0, 1){ RuntimeId=-1475096837, NetworkId=-206, ExtraData = null }, /*minecraft:bell*/
			new Item(-157, 0, 1){ RuntimeId=1729458390, NetworkId=-157, ExtraData = null }, /*minecraft:conduit*/
			new Item(-197, 0, 1){ RuntimeId=-1945031079, NetworkId=-197, ExtraData = null }, /*minecraft:stonecutter_block*/
			new Item(263, 0, 1){ RuntimeId=0, NetworkId=333, ExtraData = null }, /*minecraft:coal*/
			new Item(263, 1, 1){ RuntimeId=0, NetworkId=334, ExtraData = null }, /*minecraft:coal*/
			new Item(264, 0, 1){ RuntimeId=0, NetworkId=335, ExtraData = null }, /*minecraft:diamond*/
			new Item(452, 0, 1){ RuntimeId=0, NetworkId=608, ExtraData = null }, /*minecraft:iron_nugget*/
			new Item(545, 0, 1){ RuntimeId=0, NetworkId=545, ExtraData = null }, /**/
			new Item(546, 0, 1){ RuntimeId=0, NetworkId=546, ExtraData = null }, /**/
			new Item(782, 0, 1){ RuntimeId=0, NetworkId=782, ExtraData = null }, /**/
			new Item(547, 0, 1){ RuntimeId=0, NetworkId=547, ExtraData = null }, /**/
			new Item(544, 0, 1){ RuntimeId=0, NetworkId=544, ExtraData = null }, /**/
			new Item(265, 0, 1){ RuntimeId=0, NetworkId=336, ExtraData = null }, /*minecraft:iron_ingot*/
			new Item(752, 0, 1){ RuntimeId=0, NetworkId=656, ExtraData = null }, /*minecraft:netherite_scrap*/
			new Item(742, 0, 1){ RuntimeId=0, NetworkId=651, ExtraData = null }, /*minecraft:netherite_ingot*/
			new Item(371, 0, 1){ RuntimeId=0, NetworkId=458, ExtraData = null }, /*minecraft:gold_nugget*/
			new Item(266, 0, 1){ RuntimeId=0, NetworkId=337, ExtraData = null }, /*minecraft:gold_ingot*/
			new Item(388, 0, 1){ RuntimeId=0, NetworkId=552, ExtraData = null }, /*minecraft:emerald*/
			new Item(406, 0, 1){ RuntimeId=0, NetworkId=563, ExtraData = null }, /*minecraft:quartz*/
			new Item(337, 0, 1){ RuntimeId=0, NetworkId=416, ExtraData = null }, /*minecraft:clay_ball*/
			new Item(336, 0, 1){ RuntimeId=0, NetworkId=415, ExtraData = null }, /*minecraft:brick*/
			new Item(405, 0, 1){ RuntimeId=0, NetworkId=562, ExtraData = null }, /*minecraft:netherbrick*/
			new Item(752, 0, 1){ RuntimeId=0, NetworkId=752, ExtraData = null }, /*minecraft:netherite_scrap*/
			new Item(409, 0, 1){ RuntimeId=0, NetworkId=604, ExtraData = null }, /*minecraft:prismarine_shard*/
			new Item(666, 0, 1){ RuntimeId=0, NetworkId=666, ExtraData = null }, /**/
			new Item(422, 0, 1){ RuntimeId=0, NetworkId=588, ExtraData = null }, /*minecraft:prismarine_crystals*/
			new Item(465, 0, 1){ RuntimeId=0, NetworkId=609, ExtraData = null }, /*minecraft:nautilus_shell*/
			new Item(467, 0, 1){ RuntimeId=0, NetworkId=610, ExtraData = null }, /*minecraft:heart_of_the_sea*/
			new Item(611, 0, 1){ RuntimeId=0, NetworkId=611, ExtraData = null }, /**/
			new Item(746, 0, 1){ RuntimeId=0, NetworkId=746, ExtraData = null }, /*minecraft:netherite_axe*/
			new Item(470, 0, 1){ RuntimeId=0, NetworkId=613, ExtraData = null }, /*minecraft:phantom_membrane*/
			new Item(287, 0, 1){ RuntimeId=0, NetworkId=358, ExtraData = null }, /*minecraft:string*/
			new Item(288, 0, 1){ RuntimeId=0, NetworkId=359, ExtraData = null }, /*minecraft:feather*/
			new Item(318, 0, 1){ RuntimeId=0, NetworkId=388, ExtraData = null }, /*minecraft:flint*/
			new Item(289, 0, 1){ RuntimeId=0, NetworkId=360, ExtraData = null }, /*minecraft:gunpowder*/
			new Item(334, 0, 1){ RuntimeId=0, NetworkId=413, ExtraData = null }, /*minecraft:leather*/
			new Item(415, 0, 1){ RuntimeId=0, NetworkId=568, ExtraData = null }, /*minecraft:rabbit_hide*/
			new Item(414, 0, 1){ RuntimeId=0, NetworkId=567, ExtraData = null }, /*minecraft:rabbit_foot*/
			new Item(385, 0, 1){ RuntimeId=0, NetworkId=549, ExtraData = null }, /*minecraft:fire_charge*/
			new Item(369, 0, 1){ RuntimeId=0, NetworkId=455, ExtraData = null }, /*minecraft:blaze_rod*/
			new Item(281, 0, 1){ RuntimeId=0, NetworkId=281, ExtraData = null }, /*minecraft:bowl*/
			new Item(-316, 0, 1){ RuntimeId=-1125181862, NetworkId=-316, ExtraData = null }, /**/
			new Item(377, 0, 1){ RuntimeId=0, NetworkId=462, ExtraData = null }, /*minecraft:blaze_powder*/
			new Item(378, 0, 1){ RuntimeId=0, NetworkId=463, ExtraData = null }, /*minecraft:magma_cream*/
			new Item(376, 0, 1){ RuntimeId=0, NetworkId=461, ExtraData = null }, /*minecraft:fermented_spider_eye*/
			new Item(689, 0, 1){ RuntimeId=0, NetworkId=689, ExtraData = null }, /**/
			new Item(437, 0, 1){ RuntimeId=0, NetworkId=599, ExtraData = null }, /*minecraft:dragon_breath*/
			new Item(445, 0, 1){ RuntimeId=0, NetworkId=605, ExtraData = null }, /*minecraft:shulker_shell*/
			new Item(370, 0, 1){ RuntimeId=0, NetworkId=457, ExtraData = null }, /*minecraft:ghast_tear*/
			new Item(341, 0, 1){ RuntimeId=0, NetworkId=420, ExtraData = null }, /*minecraft:slime_ball*/
			new Item(368, 0, 1){ RuntimeId=0, NetworkId=454, ExtraData = null }, /*minecraft:ender_pearl*/
			new Item(381, 0, 1){ RuntimeId=0, NetworkId=466, ExtraData = null }, /*minecraft:ender_eye*/
			new Item(399, 0, 1){ RuntimeId=0, NetworkId=557, ExtraData = null }, /*minecraft:nether_star*/
			new Item(208, 0, 1){ RuntimeId=617576502, NetworkId=208, ExtraData = null }, /*minecraft:end_rod*/
			new Item(426, 0, 1){ RuntimeId=0, NetworkId=844, ExtraData = null }, /*minecraft:end_crystal*/
			new Item(339, 0, 1){ RuntimeId=0, NetworkId=418, ExtraData = null }, /*minecraft:paper*/
			new Item(340, 0, 1){ RuntimeId=0, NetworkId=419, ExtraData = null }, /*minecraft:book*/
			new Item(386, 0, 1){ RuntimeId=0, NetworkId=550, ExtraData = null }, /*minecraft:writable_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 0), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 0), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 0), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 0), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 1), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 1), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 1), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 1), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 2), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 2), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 2), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 2), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 3), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 3), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 3), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 3), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 4), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 4), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 4), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 4), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 5), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 5), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 5), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 6), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 6), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 6), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 7), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 7), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 7), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 8), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 9), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 9), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 9), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 9), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 9), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 10), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 10), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 10), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 10), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 10), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 11), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 11), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 11), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 11), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 11), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 12), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 12), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 13), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 13), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 14), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 14), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 14), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 15), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 15), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 15), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 15), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 15), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 16), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 17), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 17), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 17), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 18), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 18), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 18), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 19), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 19), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 19), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 19), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 19), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 20), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 20), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 21), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 22), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 23), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 23), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 23), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 24), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 24), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 24), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 25), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 25), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 26), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 27), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 28), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 29), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 29), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 29), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 29), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 29), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 30), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 30), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 30), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 31), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 31), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 31), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 32), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 33), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 34), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 34), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 34), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 34), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 35), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 35), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 35), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 36), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 36), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 36), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 37), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 37), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 37), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 38), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 38), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 38), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 39), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 39), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 39), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 39), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 39), new NbtShort("lvl", 5) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 40), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 40), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 40), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 40), new NbtShort("lvl", 4) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 41), new NbtShort("lvl", 1) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 41), new NbtShort("lvl", 2) } } } }, /*minecraft:enchanted_book*/
			new Item(403, 0, 1){ RuntimeId=0, NetworkId=560, ExtraData = new NbtCompound { new NbtList("ench", (NbtTagType)10) { new NbtCompound { new NbtShort("id", 41), new NbtShort("lvl", 3) } } } }, /*minecraft:enchanted_book*/
			new Item(333, 0, 1){ RuntimeId=0, NetworkId=407, ExtraData = null }, /*minecraft:boat*/
			new Item(333, 1, 1){ RuntimeId=0, NetworkId=410, ExtraData = null }, /*minecraft:boat*/
			new Item(333, 2, 1){ RuntimeId=0, NetworkId=408, ExtraData = null }, /*minecraft:boat*/
			new Item(333, 3, 1){ RuntimeId=0, NetworkId=409, ExtraData = null }, /*minecraft:boat*/
			new Item(333, 4, 1){ RuntimeId=0, NetworkId=411, ExtraData = null }, /*minecraft:boat*/
			new Item(333, 5, 1){ RuntimeId=0, NetworkId=412, ExtraData = null }, /*minecraft:boat*/
			new Item(677, 0, 1){ RuntimeId=0, NetworkId=677, ExtraData = null }, /**/
			new Item(691, 0, 1){ RuntimeId=0, NetworkId=691, ExtraData = null }, /**/
			new Item(748, 0, 1){ RuntimeId=0, NetworkId=748, ExtraData = null }, /*minecraft:netherite_helmet*/
			new Item(695, 0, 1){ RuntimeId=0, NetworkId=695, ExtraData = null }, /**/
			new Item(680, 0, 1){ RuntimeId=0, NetworkId=680, ExtraData = null }, /**/
			new Item(683, 0, 1){ RuntimeId=0, NetworkId=683, ExtraData = null }, /**/
			new Item(681, 0, 1){ RuntimeId=0, NetworkId=681, ExtraData = null }, /**/
			new Item(682, 0, 1){ RuntimeId=0, NetworkId=682, ExtraData = null }, /**/
			new Item(684, 0, 1){ RuntimeId=0, NetworkId=684, ExtraData = null }, /**/
			new Item(685, 0, 1){ RuntimeId=0, NetworkId=685, ExtraData = null }, /**/
			new Item(686, 0, 1){ RuntimeId=0, NetworkId=686, ExtraData = null }, /**/
			new Item(692, 0, 1){ RuntimeId=0, NetworkId=692, ExtraData = null }, /**/
			new Item(749, 0, 1){ RuntimeId=0, NetworkId=749, ExtraData = null }, /*minecraft:netherite_chestplate*/
			new Item(696, 0, 1){ RuntimeId=0, NetworkId=696, ExtraData = null }, /**/
			new Item(66, 0, 1){ RuntimeId=-1734456706, NetworkId=66, ExtraData = null }, /*minecraft:rail*/
			new Item(27, 0, 1){ RuntimeId=791742332, NetworkId=27, ExtraData = null }, /*minecraft:golden_rail*/
			new Item(28, 0, 1){ RuntimeId=-1997120683, NetworkId=28, ExtraData = null }, /*minecraft:detector_rail*/
			new Item(126, 0, 1){ RuntimeId=-1692076187, NetworkId=126, ExtraData = null }, /*minecraft:activator_rail*/
			new Item(328, 0, 1){ RuntimeId=0, NetworkId=402, ExtraData = null }, /*minecraft:minecart*/
			new Item(342, 0, 1){ RuntimeId=0, NetworkId=421, ExtraData = null }, /*minecraft:chest_minecart*/
			new Item(408, 0, 1){ RuntimeId=0, NetworkId=565, ExtraData = null }, /*minecraft:hopper_minecart*/
			new Item(407, 0, 1){ RuntimeId=0, NetworkId=564, ExtraData = null }, /*minecraft:tnt_minecart*/
			new Item(331, 0, 1){ RuntimeId=0, NetworkId=405, ExtraData = null }, /*minecraft:redstone*/
			new Item(152, 0, 1){ RuntimeId=512666773, NetworkId=152, ExtraData = null }, /*minecraft:redstone_block*/
			new Item(76, 0, 1){ RuntimeId=-923042632, NetworkId=76, ExtraData = null }, /*minecraft:redstone_torch*/
			new Item(69, 0, 1){ RuntimeId=-1194959088, NetworkId=69, ExtraData = null }, /*minecraft:lever*/
			new Item(143, 0, 1){ RuntimeId=414831508, NetworkId=143, ExtraData = null }, /*minecraft:wooden_button*/
			new Item(-144, 0, 1){ RuntimeId=-1367256860, NetworkId=-144, ExtraData = null }, /*minecraft:spruce_button*/
			new Item(-141, 0, 1){ RuntimeId=719300965, NetworkId=-141, ExtraData = null }, /*minecraft:birch_button*/
			new Item(-143, 0, 1){ RuntimeId=-1769374467, NetworkId=-143, ExtraData = null }, /*minecraft:jungle_button*/
			new Item(-140, 0, 1){ RuntimeId=152215376, NetworkId=-140, ExtraData = null }, /*minecraft:acacia_button*/
			new Item(-142, 0, 1){ RuntimeId=1644326878, NetworkId=-142, ExtraData = null }, /*minecraft:dark_oak_button*/
			new Item(-487, 0, 1){ RuntimeId=219759617, NetworkId=-487, ExtraData = null }, /**/
			new Item(-530, 0, 1){ RuntimeId=-802498999, NetworkId=-530, ExtraData = null }, /**/
			new Item(-989, 0, 1){ RuntimeId=-1662398766, NetworkId=-989, ExtraData = null }, /**/
			new Item(-511, 0, 1){ RuntimeId=1057945498, NetworkId=-511, ExtraData = null }, /**/
			new Item(77, 0, 1){ RuntimeId=761806540, NetworkId=77, ExtraData = null }, /*minecraft:stone_button*/
			new Item(-260, 0, 1){ RuntimeId=-915440134, NetworkId=-260, ExtraData = null }, /*minecraft:crimson_button*/
			new Item(-261, 0, 1){ RuntimeId=897130215, NetworkId=-261, ExtraData = null }, /*minecraft:warped_button*/
			new Item(-296, 0, 1){ RuntimeId=-415378418, NetworkId=-296, ExtraData = null }, /*minecraft:polished_blackstone_button*/
			new Item(131, 0, 1){ RuntimeId=-1991050318, NetworkId=131, ExtraData = null }, /*minecraft:tripwire_hook*/
			new Item(72, 0, 1){ RuntimeId=-600018124, NetworkId=72, ExtraData = null }, /*minecraft:wooden_pressure_plate*/
			new Item(-154, 0, 1){ RuntimeId=218827956, NetworkId=-154, ExtraData = null }, /*minecraft:spruce_pressure_plate*/
			new Item(-151, 0, 1){ RuntimeId=-323141345, NetworkId=-151, ExtraData = null }, /*minecraft:birch_pressure_plate*/
			new Item(-153, 0, 1){ RuntimeId=537218999, NetworkId=-153, ExtraData = null }, /*minecraft:jungle_pressure_plate*/
			new Item(-150, 0, 1){ RuntimeId=1596185984, NetworkId=-150, ExtraData = null }, /*minecraft:acacia_pressure_plate*/
			new Item(-152, 0, 1){ RuntimeId=-1396926422, NetworkId=-152, ExtraData = null }, /*minecraft:dark_oak_pressure_plate*/
			new Item(-490, 0, 1){ RuntimeId=-914160197, NetworkId=-490, ExtraData = null }, /**/
			new Item(-538, 0, 1){ RuntimeId=-1107667981, NetworkId=-538, ExtraData = null }, /**/
			new Item(-997, 0, 1){ RuntimeId=-1196137202, NetworkId=-997, ExtraData = null }, /**/
			new Item(-514, 0, 1){ RuntimeId=1985903750, NetworkId=-514, ExtraData = null }, /**/
			new Item(-262, 0, 1){ RuntimeId=190797670, NetworkId=-262, ExtraData = null }, /*minecraft:crimson_pressure_plate*/
			new Item(-263, 0, 1){ RuntimeId=-2019536491, NetworkId=-263, ExtraData = null }, /*minecraft:warped_pressure_plate*/
			new Item(70, 0, 1){ RuntimeId=-1046659956, NetworkId=70, ExtraData = null }, /*minecraft:stone_pressure_plate*/
			new Item(147, 0, 1){ RuntimeId=-1904165458, NetworkId=147, ExtraData = null }, /*minecraft:light_weighted_pressure_plate*/
			new Item(148, 0, 1){ RuntimeId=-1186327273, NetworkId=148, ExtraData = null }, /*minecraft:heavy_weighted_pressure_plate*/
			new Item(-295, 0, 1){ RuntimeId=1903053274, NetworkId=-295, ExtraData = null }, /*minecraft:polished_blackstone_pressure_plate*/
			new Item(251, 0, 1){ RuntimeId=-428259264, NetworkId=251, ExtraData = null }, /*minecraft:observer*/
			new Item(151, 0, 1){ RuntimeId=2103062190, NetworkId=151, ExtraData = null }, /*minecraft:daylight_detector*/
			new Item(356, 0, 1){ RuntimeId=0, NetworkId=451, ExtraData = null }, /*minecraft:repeater*/
			new Item(404, 0, 1){ RuntimeId=0, NetworkId=561, ExtraData = null }, /*minecraft:comparator*/
			new Item(410, 0, 1){ RuntimeId=0, NetworkId=566, ExtraData = null }, /*minecraft:hopper*/
			new Item(125, 0, 1){ RuntimeId=-1277280225, NetworkId=125, ExtraData = null }, /*minecraft:dropper*/
			new Item(23, 0, 1){ RuntimeId=1036524128, NetworkId=23, ExtraData = null }, /*minecraft:dispenser*/
			new Item(-313, 0, 1){ RuntimeId=-340046385, NetworkId=-313, ExtraData = null }, /**/
			new Item(33, 0, 1){ RuntimeId=1961230256, NetworkId=33, ExtraData = null }, /*minecraft:piston*/
			new Item(29, 0, 1){ RuntimeId=-1993329665, NetworkId=29, ExtraData = null }, /*minecraft:sticky_piston*/
			new Item(46, 0, 1){ RuntimeId=622850821, NetworkId=46, ExtraData = null }, /*minecraft:tnt*/
			new Item(421, 0, 1){ RuntimeId=0, NetworkId=587, ExtraData = null }, /*minecraft:name_tag*/
			new Item(-204, 0, 1){ RuntimeId=-674110593, NetworkId=-204, ExtraData = null }, /*minecraft:loom*/
			new Item(446, 0, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 8, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 7, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 15, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 12, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 14, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 1, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 4, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 5, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 13, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 9, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 3, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 11, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 10, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 2, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 6, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 0) } }, /*minecraft:banner*/
			new Item(446, 15, 1){ RuntimeId=0, NetworkId=606, ExtraData = new NbtCompound { new NbtInt("Type", 1) } }, /*minecraft:banner*/
			new Item(434, 0, 1){ RuntimeId=0, NetworkId=621, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 1, 1){ RuntimeId=0, NetworkId=622, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 2, 1){ RuntimeId=0, NetworkId=620, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 3, 1){ RuntimeId=0, NetworkId=623, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 4, 1){ RuntimeId=0, NetworkId=624, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 5, 1){ RuntimeId=0, NetworkId=625, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 6, 1){ RuntimeId=0, NetworkId=626, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(434, 7, 1){ RuntimeId=0, NetworkId=627, ExtraData = null }, /*minecraft:banner_pattern*/
			new Item(628, 0, 1){ RuntimeId=0, NetworkId=628, ExtraData = null }, /**/
			new Item(629, 0, 1){ RuntimeId=0, NetworkId=629, ExtraData = null }, /**/
			new Item(699, 0, 1){ RuntimeId=0, NetworkId=699, ExtraData = null }, /**/
			new Item(700, 0, 1){ RuntimeId=0, NetworkId=700, ExtraData = null }, /**/
			new Item(701, 0, 1){ RuntimeId=0, NetworkId=701, ExtraData = null }, /**/
			new Item(702, 0, 1){ RuntimeId=0, NetworkId=702, ExtraData = null }, /**/
			new Item(703, 0, 1){ RuntimeId=0, NetworkId=703, ExtraData = null }, /**/
			new Item(704, 0, 1){ RuntimeId=0, NetworkId=704, ExtraData = null }, /**/
			new Item(705, 0, 1){ RuntimeId=0, NetworkId=705, ExtraData = null }, /**/
			new Item(706, 0, 1){ RuntimeId=0, NetworkId=706, ExtraData = null }, /**/
			new Item(707, 0, 1){ RuntimeId=0, NetworkId=707, ExtraData = null }, /**/
			new Item(708, 0, 1){ RuntimeId=0, NetworkId=708, ExtraData = null }, /**/
			new Item(709, 0, 1){ RuntimeId=0, NetworkId=709, ExtraData = null }, /**/
			new Item(710, 0, 1){ RuntimeId=0, NetworkId=710, ExtraData = null }, /**/
			new Item(711, 0, 1){ RuntimeId=0, NetworkId=711, ExtraData = null }, /**/
			new Item(712, 0, 1){ RuntimeId=0, NetworkId=712, ExtraData = null }, /**/
			new Item(713, 0, 1){ RuntimeId=0, NetworkId=713, ExtraData = null }, /**/
			new Item(714, 0, 1){ RuntimeId=0, NetworkId=714, ExtraData = null }, /**/
			new Item(715, 0, 1){ RuntimeId=0, NetworkId=715, ExtraData = null }, /**/
			new Item(716, 0, 1){ RuntimeId=0, NetworkId=716, ExtraData = null }, /**/
			new Item(717, 0, 1){ RuntimeId=0, NetworkId=717, ExtraData = null }, /**/
			new Item(718, 0, 1){ RuntimeId=0, NetworkId=718, ExtraData = null }, /**/
			new Item(719, 0, 1){ RuntimeId=0, NetworkId=719, ExtraData = null }, /**/
			new Item(720, 0, 1){ RuntimeId=0, NetworkId=720, ExtraData = null }, /*minecraft:campfire*/
			new Item(721, 0, 1){ RuntimeId=0, NetworkId=721, ExtraData = null }, /**/
			new Item(723, 0, 1){ RuntimeId=0, NetworkId=723, ExtraData = null }, /**/
			new Item(724, 0, 1){ RuntimeId=0, NetworkId=724, ExtraData = null }, /**/
			new Item(730, 0, 1){ RuntimeId=0, NetworkId=730, ExtraData = null }, /**/
			new Item(727, 0, 1){ RuntimeId=0, NetworkId=727, ExtraData = null }, /**/
			new Item(726, 0, 1){ RuntimeId=0, NetworkId=726, ExtraData = null }, /**/
			new Item(725, 0, 1){ RuntimeId=0, NetworkId=725, ExtraData = null }, /**/
			new Item(736, 0, 1){ RuntimeId=0, NetworkId=736, ExtraData = null }, /*minecraft:honeycomb*/
			new Item(738, 0, 1){ RuntimeId=0, NetworkId=738, ExtraData = null }, /**/
			new Item(737, 0, 1){ RuntimeId=0, NetworkId=737, ExtraData = null }, /*minecraft:honey_bottle*/
			new Item(739, 0, 1){ RuntimeId=0, NetworkId=739, ExtraData = null }, /**/
			new Item(728, 0, 1){ RuntimeId=0, NetworkId=728, ExtraData = null }, /**/
			new Item(735, 0, 1){ RuntimeId=0, NetworkId=735, ExtraData = null }, /**/
			new Item(731, 0, 1){ RuntimeId=0, NetworkId=731, ExtraData = null }, /**/
			new Item(732, 0, 1){ RuntimeId=0, NetworkId=732, ExtraData = null }, /**/
			new Item(733, 0, 1){ RuntimeId=0, NetworkId=733, ExtraData = null }, /**/
			new Item(729, 0, 1){ RuntimeId=0, NetworkId=729, ExtraData = null }, /**/
			new Item(734, 0, 1){ RuntimeId=0, NetworkId=734, ExtraData = null }, /*minecraft:suspicious_stew*/
			new Item(740, 0, 1){ RuntimeId=0, NetworkId=740, ExtraData = null }, /**/
			new Item(741, 0, 1){ RuntimeId=0, NetworkId=741, ExtraData = null }, /*minecraft:lodestone_compass*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)0), new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{0}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{8}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{7}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{15}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{12}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{14}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{1}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{4}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{5}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{13}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{9}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{3}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{11}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{10}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{2}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(401, 0, 1){ RuntimeId=0, NetworkId=558, ExtraData = new NbtCompound { new NbtCompound("Fireworks") { new NbtList("Explosions", (NbtTagType)10) { new NbtCompound { new NbtByteArray("FireworkColor", new byte[1]{6}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) } }, new NbtByte("Flight", 1) } } }, /*minecraft:firework_rocket*/
			new Item(402, 0, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{0}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -14869215) } }, /*minecraft:firework_star*/
			new Item(402, 8, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{8}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -12103854) } }, /*minecraft:firework_star*/
			new Item(402, 7, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{7}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -6447721) } }, /*minecraft:firework_star*/
			new Item(402, 15, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{15}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -986896) } }, /*minecraft:firework_star*/
			new Item(402, 12, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{12}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -12930086) } }, /*minecraft:firework_star*/
			new Item(402, 14, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{14}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -425955) } }, /*minecraft:firework_star*/
			new Item(402, 1, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{1}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -5231066) } }, /*minecraft:firework_star*/
			new Item(402, 4, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{4}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -12827478) } }, /*minecraft:firework_star*/
			new Item(402, 5, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{5}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -7785800) } }, /*minecraft:firework_star*/
			new Item(402, 13, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{13}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -3715395) } }, /*minecraft:firework_star*/
			new Item(402, 9, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{9}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -816214) } }, /*minecraft:firework_star*/
			new Item(402, 3, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{3}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -8170446) } }, /*minecraft:firework_star*/
			new Item(402, 11, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{11}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -75715) } }, /*minecraft:firework_star*/
			new Item(402, 10, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{10}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -8337633) } }, /*minecraft:firework_star*/
			new Item(402, 2, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{2}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -10585066) } }, /*minecraft:firework_star*/
			new Item(402, 6, 1){ RuntimeId=0, NetworkId=559, ExtraData = new NbtCompound { new NbtCompound("FireworksItem") { new NbtByteArray("FireworkColor", new byte[1]{6}), new NbtByteArray("FireworkFade", new byte[0]{}), new NbtByte("FireworkFlicker", 0), new NbtByte("FireworkTrail", 0), new NbtByte("FireworkType", 0) }, new NbtInt("customColor", -15295332) } }, /*minecraft:firework_star*/
			new Item(-286, 0, 1){ RuntimeId=-464697614, NetworkId=-286, ExtraData = null }, /*minecraft:chain*/
			new Item(-1074, 0, 1){ RuntimeId=1502169779, NetworkId=-1074, ExtraData = null }, /**/
			new Item(-1075, 0, 1){ RuntimeId=1618350242, NetworkId=-1075, ExtraData = null }, /**/
			new Item(-1076, 0, 1){ RuntimeId=-1321339769, NetworkId=-1076, ExtraData = null }, /**/
			new Item(-1077, 0, 1){ RuntimeId=935170363, NetworkId=-1077, ExtraData = null }, /**/
			new Item(-1078, 0, 1){ RuntimeId=1732436259, NetworkId=-1078, ExtraData = null }, /**/
			new Item(-1079, 0, 1){ RuntimeId=-1199104174, NetworkId=-1079, ExtraData = null }, /**/
			new Item(-1080, 0, 1){ RuntimeId=2043267039, NetworkId=-1080, ExtraData = null }, /**/
			new Item(-1081, 0, 1){ RuntimeId=1286321659, NetworkId=-1081, ExtraData = null }, /**/
			new Item(-239, 0, 1){ RuntimeId=-842110736, NetworkId=-239, ExtraData = null }, /*minecraft:target*/
			new Item(-551, 0, 1){ RuntimeId=340115056, NetworkId=-551, ExtraData = null }, /**/
			new Item(283, 0, 1){ RuntimeId=0, NetworkId=283, ExtraData = null }, /*minecraft:golden_sword*/
			new Item(282, 0, 1){ RuntimeId=0, NetworkId=282, ExtraData = null }, /*minecraft:mushroom_stew*/
		};
	}
}