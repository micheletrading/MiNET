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
using MiNET.Utils;

// TODO: DELETE THIS FILE.
//
// Every class below models a block that no longer exists. Mojang split each of them into
// separate blocks, so minecraft:log became oak_log, spruce_log and the rest, minecraft:wool
// became the sixteen coloured wools, minecraft:stone_slab became stone_slab, sandstone_slab and
// so on. None of these names appear in the canonical palette any more, which is why the
// generator cannot emit them and why they are hand-written here.
//
// They only survive because live code still references them. Nothing else keeps them alive: the
// dead legacy-id entries in BlockFactory were already removed, along with the fourteen classes
// that had no callers at all.
//
// To retire one, rewrite its callers against the flattened blocks and delete it. The known work:
//
//   Sapling.cs        grows a trunk with `new Log { OldLogType = SaplingType }`, and its
//                     placement check lists Log, Log2, Leaves, Leaves2. Pick OakLog, BirchLog
//                     and friends by species instead.
//   Leaves.cs         `block is Log` for decay, same in Leaves2.cs. Becomes a check against
//   Leaves2.cs        the log family.
//   ItemShears.cs     `block is Wool` and `block is Leaves`.
//   World providers   CoolWorldProvider, ExperimentalWorldProvider and PlotWorldGenerator build
//                     terrain out of these aggregates.
//
// The block model is not right until this file is empty. Every class here is a state space the
// game does not have, and anything that resolves through one of them cannot round-trip to a
// real block state.
//
// Remaining, 34 classes:
// Carpet, Chain, Concrete, ConcretePowder, DoublePlant, DoubleStoneSlab, DoubleStoneSlab2,
//   DoubleStoneSlab3, DoubleStoneSlab4, DoubleWoodenSlab, Fence, Grass, Leaves, Leaves2, Log,
//   Log2, MonsterEgg, Planks, RedFlower, Sapling, ShulkerBox, Skull, StainedGlass,
//   StainedGlassPane, StainedHardenedClay, StoneSlab, StoneSlab2, StoneSlab3, StoneSlab4,
//   Stonebrick, Wood, WoodenSlab, Wool, YellowFlower

namespace MiNET.Blocks
{

    public partial class Carpet // 171 typeof=Carpet
    {
        public override string Name => "minecraft:carpet";

        [StateEnum("magenta","blue","silver","red","yellow","light_blue","white","lime","pink","green","purple","black","cyan","gray","orange","brown")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:carpet";
            record.Id = 171;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

	public class Chain : Block // chain, removed from the game by block flattening
    {
		public Chain() : base(541)
		{
			IsGenerated = true;
		}

        public override string Name => "minecraft:chain";

		[StateEnum("z", "y", "x")]
		public string PillarAxis { get; set; } = "y";

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch (state)
				{
					case BlockStateString s when s.Name == "pillar_axis":
						PillarAxis = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:chain";
			record.Id = 541;
			record.States.Add(new BlockStateString { Name = "pillar_axis", Value = PillarAxis });
			return record;
		} // method
	} // class

    public partial class Concrete // 236 typeof=Concrete
    {
        public override string Name => "minecraft:concrete";

        [StateEnum("green","orange","light_blue","black","red","yellow","blue","brown","lime","pink","gray","purple","magenta","cyan","white","silver")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:concrete";
            record.Id = 236;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class ConcretePowder // 237 typeof=ConcretePowder
    {
        public override string Name => "minecraft:concretePowder";

        [StateEnum("light_blue","gray","pink","red","silver","white","cyan","magenta","brown","lime","purple","orange","yellow","blue","black","green")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:concretePowder";
            record.Id = 237;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class DoublePlant // 175 typeof=DoublePlant
    {
        public override string Name => "minecraft:double_plant";

        [StateEnum("fern","syringa","sunflower","paeonia","rose","grass")]
        public string DoublePlantType { get; set; } = "sunflower";
        [StateBit] public bool UpperBlockBit { get; set; } = false;

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "double_plant_type":
                        DoublePlantType = s.Value;
                        break;
                    case BlockStateByte s when s.Name == "upper_block_bit":
                        UpperBlockBit = Convert.ToBoolean(s.Value);
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:double_plant";
            record.Id = 175;
            record.States.Add(new BlockStateString {Name = "double_plant_type", Value = DoublePlantType});
            record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
            return record;
        } // method
    } // class

    public partial class Fence // 85 typeof=Fence
    {
        public override string Name => "minecraft:fence";

        [StateEnum("jungle","spruce","birch","dark_oak","acacia","oak")]
        public string WoodType { get; set; } = "oak";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "wood_type":
                        WoodType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:fence";
            record.Id = 85;
            record.States.Add(new BlockStateString {Name = "wood_type", Value = WoodType});
            return record;
        } // method
    } // class

    public partial class Grass // 2 typeof=Grass
    {
        public override string Name => "minecraft:grass_block";


        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:grass_block";
            record.Id = 2;
            return record;
        } // method
    } // class

    public partial class Leaves // 18 typeof=Leaves
    {
        public override string Name => "minecraft:leaves";

        [StateEnum("birch","oak","spruce","jungle")]
        public string OldLeafType { get; set; } = "oak";
        [StateBit] public bool PersistentBit { get; set; } = false;
        [StateBit] public bool UpdateBit { get; set; } = false;

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "old_leaf_type":
                        OldLeafType = s.Value;
                        break;
                    case BlockStateByte s when s.Name == "persistent_bit":
                        PersistentBit = Convert.ToBoolean(s.Value);
                        break;
                    case BlockStateByte s when s.Name == "update_bit":
                        UpdateBit = Convert.ToBoolean(s.Value);
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:leaves";
            record.Id = 18;
            record.States.Add(new BlockStateString {Name = "old_leaf_type", Value = OldLeafType});
            record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
            record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
            return record;
        } // method
    } // class

    public partial class Leaves2 // 161 typeof=Leaves2
    {
        public override string Name => "minecraft:leaves2";

        [StateEnum("dark_oak","acacia")]
        public string NewLeafType { get; set; } = "acacia";
        [StateBit] public bool PersistentBit { get; set; } = false;
        [StateBit] public bool UpdateBit { get; set; } = false;

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "new_leaf_type":
                        NewLeafType = s.Value;
                        break;
                    case BlockStateByte s when s.Name == "persistent_bit":
                        PersistentBit = Convert.ToBoolean(s.Value);
                        break;
                    case BlockStateByte s when s.Name == "update_bit":
                        UpdateBit = Convert.ToBoolean(s.Value);
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:leaves2";
            record.Id = 161;
            record.States.Add(new BlockStateString {Name = "new_leaf_type", Value = NewLeafType});
            record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
            record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
            return record;
        } // method
    } // class

    public partial class Log // 17 typeof=Log
    {
        public override string Name => "minecraft:log";

        [StateEnum("spruce","birch","jungle","oak")]
        public string OldLogType { get; set; } = "oak";
        [StateEnum("y","x","z")]
        public string PillarAxis { get; set; } = "y";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "old_log_type":
                        OldLogType = s.Value;
                        break;
                    case BlockStateString s when s.Name == "pillar_axis":
                        PillarAxis = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:log";
            record.Id = 17;
            record.States.Add(new BlockStateString {Name = "old_log_type", Value = OldLogType});
            record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
            return record;
        } // method
    } // class

    public partial class Log2 // 162 typeof=Log2
    {
        public override string Name => "minecraft:log2";

        [StateEnum("dark_oak","acacia")]
        public string NewLogType { get; set; } = "acacia";
        [StateEnum("y","z","x")]
        public string PillarAxis { get; set; } = "y";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "new_log_type":
                        NewLogType = s.Value;
                        break;
                    case BlockStateString s when s.Name == "pillar_axis":
                        PillarAxis = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:log2";
            record.Id = 162;
            record.States.Add(new BlockStateString {Name = "new_log_type", Value = NewLogType});
            record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
            return record;
        } // method
    } // class

    public partial class MonsterEgg // 97 typeof=MonsterEgg
    {
        public override string Name => "minecraft:monster_egg";

        [StateEnum("stone","cobblestone","stone_brick","cracked_stone_brick","mossy_stone_brick","chiseled_stone_brick")]
        public string MonsterEggStoneType { get; set; } = "stone";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "monster_egg_stone_type":
                        MonsterEggStoneType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:monster_egg";
            record.Id = 97;
            record.States.Add(new BlockStateString {Name = "monster_egg_stone_type", Value = MonsterEggStoneType});
            return record;
        } // method
    } // class

    public partial class Planks // 5 typeof=Planks
    {
        public override string Name => "minecraft:planks";

        [StateEnum("birch","acacia","spruce","oak","jungle","dark_oak")]
        public string WoodType { get; set; } = "oak";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "wood_type":
                        WoodType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:planks";
            record.Id = 5;
            record.States.Add(new BlockStateString {Name = "wood_type", Value = WoodType});
            return record;
        } // method
    } // class

    public partial class RedFlower // 38 typeof=RedFlower
    {
        public override string Name => "minecraft:red_flower";

        [StateEnum("tulip_pink","houstonia","lily_of_the_valley","tulip_white","allium","tulip_red","poppy","cornflower","tulip_orange","oxeye","orchid")]
        public string FlowerType { get; set; } = "poppy";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "flower_type":
                        FlowerType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:red_flower";
            record.Id = 38;
            record.States.Add(new BlockStateString {Name = "flower_type", Value = FlowerType});
            return record;
        } // method
    } // class

    public partial class Sapling // 6 typeof=Sapling
    {
        public override string Name => "minecraft:sapling";

        [StateBit] public bool AgeBit { get; set; } = false;
        [StateEnum("jungle","oak","spruce","acacia","dark_oak","birch")]
        public string SaplingType { get; set; } = "oak";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateByte s when s.Name == "age_bit":
                        AgeBit = Convert.ToBoolean(s.Value);
                        break;
                    case BlockStateString s when s.Name == "sapling_type":
                        SaplingType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:sapling";
            record.Id = 6;
            record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
            record.States.Add(new BlockStateString {Name = "sapling_type", Value = SaplingType});
            return record;
        } // method
    } // class

    public partial class ShulkerBox // 218 typeof=ShulkerBox
    {
        public override string Name => "minecraft:shulker_box";

        [StateEnum("light_blue","pink","lime","orange","purple","brown","white","black","magenta","yellow","cyan","green","gray","blue","silver","red")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:shulker_box";
            record.Id = 218;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class Skull // 144 typeof=Skull
    {
        public override string Name => "minecraft:skull";

        [StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
        [StateBit] public bool NoDropBit { get; set; } = false;

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateInt s when s.Name == "facing_direction":
                        FacingDirection = s.Value;
                        break;
                    case BlockStateByte s when s.Name == "no_drop_bit":
                        NoDropBit = Convert.ToBoolean(s.Value);
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:skull";
            record.Id = 144;
            record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
            record.States.Add(new BlockStateByte {Name = "no_drop_bit", Value = Convert.ToByte(NoDropBit)});
            return record;
        } // method
    } // class

    public partial class StainedGlass // 241 typeof=StainedGlass
    {
        public override string Name => "minecraft:stained_glass";

        [StateEnum("brown","purple","light_blue","cyan","silver","black","pink","orange","white","green","magenta","gray","blue","lime","red","yellow")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:stained_glass";
            record.Id = 241;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class StainedGlassPane // 160 typeof=StainedGlassPane
    {
        public override string Name => "minecraft:stained_glass_pane";

        [StateEnum("black","lime","yellow","light_blue","white","purple","pink","red","magenta","orange","green","silver","gray","blue","cyan","brown")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:stained_glass_pane";
            record.Id = 160;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class StainedHardenedClay // 159 typeof=StainedHardenedClay
    {
        public override string Name => "minecraft:stained_hardened_clay";

        [StateEnum("pink","gray","lime","red","blue","cyan","green","light_blue","orange","black","yellow","magenta","brown","white","silver","purple")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:stained_hardened_clay";
            record.Id = 159;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class Stonebrick // 98 typeof=Stonebrick
    {
        public override string Name => "minecraft:stonebrick";

        [StateEnum("smooth","default","chiseled","cracked","mossy")]
        public string StoneBrickType { get; set; } = "default";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "stone_brick_type":
                        StoneBrickType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:stonebrick";
            record.Id = 98;
            record.States.Add(new BlockStateString {Name = "stone_brick_type", Value = StoneBrickType});
            return record;
        } // method
    } // class

	public class Wood : Block // wood, removed from the game by block flattening
    {
		public Wood() : base(467)
		{
			IsGenerated = true;
		}

        public override string Name => "minecraft:wood";

        [StateEnum("x","y","z")]
        public string PillarAxis { get; set; } = "y";
        [StateBit] public bool StrippedBit { get; set; } = false;
        [StateEnum("oak","dark_oak","acacia","jungle","birch","spruce")]
        public string WoodType { get; set; } = "oak";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "pillar_axis":
                        PillarAxis = s.Value;
                        break;
                    case BlockStateByte s when s.Name == "stripped_bit":
                        StrippedBit = Convert.ToBoolean(s.Value);
                        break;
                    case BlockStateString s when s.Name == "wood_type":
                        WoodType = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:wood";
            record.Id = 467;
            record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
            record.States.Add(new BlockStateByte {Name = "stripped_bit", Value = Convert.ToByte(StrippedBit)});
            record.States.Add(new BlockStateString {Name = "wood_type", Value = WoodType});
            return record;
        } // method
    } // class

    public partial class Wool // 35 typeof=Wool
    {
        public override string Name => "minecraft:wool";

        [StateEnum("light_blue","gray","orange","red","silver","green","pink","black","yellow","brown","blue","cyan","purple","white","lime","magenta")]
        public string Color { get; set; } = "white";

        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                    case BlockStateString s when s.Name == "color":
                        Color = s.Value;
                        break;
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:wool";
            record.Id = 35;
            record.States.Add(new BlockStateString {Name = "color", Value = Color});
            return record;
        } // method
    } // class

    public partial class YellowFlower // 37 typeof=YellowFlower
    {
        public override string Name => "minecraft:yellow_flower";


        public override void SetState(List<IBlockState> states)
        {
            foreach (var state in states)
            {
                switch(state)
                {
                } // switch
            } // foreach
        } // method

        public override BlockStateContainer GetState()
        {
            var record = new BlockStateContainer();
            record.Name = "minecraft:yellow_flower";
            record.Id = 37;
            return record;
        } // method
    } // class
}
