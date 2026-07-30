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

// GENERATED CODE. DON'T EDIT BY HAND.

using System;
using System.Collections.Generic;
using MiNET.Utils;

namespace MiNET.Blocks
{

	public partial class AcaciaDoubleSlab : Block // minecraft:acacia_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public AcaciaDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:acacia_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class AcaciaFence : Block // minecraft:acacia_fence
	{

		public AcaciaFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:acacia_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class AcaciaHangingSign : Block // minecraft:acacia_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public AcaciaHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:acacia_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class AcaciaLeaves : Block // minecraft:acacia_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public AcaciaLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:acacia_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class AcaciaLog : Block // minecraft:acacia_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public AcaciaLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:acacia_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class AcaciaPlanks : Block // minecraft:acacia_planks
	{

		public AcaciaPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:acacia_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class AcaciaSapling : Block // minecraft:acacia_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public AcaciaSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:acacia_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class AcaciaShelf : Block // minecraft:acacia_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public AcaciaShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:acacia_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class AcaciaSlab : Block // minecraft:acacia_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public AcaciaSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:acacia_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class AcaciaWood : Block // minecraft:acacia_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public AcaciaWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:acacia_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class Allium : Block // minecraft:allium
	{

		public Allium() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:allium";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class AmethystBlock : Block // minecraft:amethyst_block
	{

		public AmethystBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:amethyst_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class AmethystCluster : Block // minecraft:amethyst_cluster
	{
		[StateEnum("down","up","north","south","west","east")]
		public string BlockFace { get; set; } = "down";

		public AmethystCluster() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:block_face":
						BlockFace = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:amethyst_cluster";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:block_face", Value = BlockFace});
			return record;
		} // method
	} // class

	public partial class Andesite : Block // minecraft:andesite
	{

		public Andesite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:andesite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class AndesiteDoubleSlab : Block // minecraft:andesite_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public AndesiteDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:andesite_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class AndesiteSlab : Block // minecraft:andesite_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public AndesiteSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:andesite_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class AndesiteWall : Block // minecraft:andesite_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public AndesiteWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:andesite_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class Azalea : Block // minecraft:azalea
	{

		public Azalea() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:azalea";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class AzaleaLeaves : Block // minecraft:azalea_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public AzaleaLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:azalea_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class AzaleaLeavesFlowered : Block // minecraft:azalea_leaves_flowered
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public AzaleaLeavesFlowered() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:azalea_leaves_flowered";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class AzureBluet : Block // minecraft:azure_bluet
	{

		public AzureBluet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:azure_bluet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BambooBlock : Block // minecraft:bamboo_block
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public BambooBlock() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:bamboo_block";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class BambooButton : Block // minecraft:bamboo_button
	{
		[StateBit] public bool ButtonPressedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public BambooButton() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "button_pressed_bit":
						ButtonPressedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_button";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "button_pressed_bit", Value = Convert.ToByte(ButtonPressedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class BambooDoor : Block // minecraft:bamboo_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public BambooDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:bamboo_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class BambooDoubleSlab : Block // minecraft:bamboo_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BambooDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BambooFence : Block // minecraft:bamboo_fence
	{

		public BambooFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:bamboo_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BambooFenceGate : Block // minecraft:bamboo_fence_gate
	{
		[StateBit] public bool InWallBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;

		public BambooFenceGate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "in_wall_bit":
						InWallBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_fence_gate";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "in_wall_bit", Value = Convert.ToByte(InWallBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			return record;
		} // method
	} // class

	public partial class BambooHangingSign : Block // minecraft:bamboo_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public BambooHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class BambooMosaic : Block // minecraft:bamboo_mosaic
	{

		public BambooMosaic() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:bamboo_mosaic";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BambooMosaicDoubleSlab : Block // minecraft:bamboo_mosaic_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BambooMosaicDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_mosaic_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BambooMosaicSlab : Block // minecraft:bamboo_mosaic_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BambooMosaicSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_mosaic_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BambooMosaicStairs : Block // minecraft:bamboo_mosaic_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public BambooMosaicStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_mosaic_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class BambooPlanks : Block // minecraft:bamboo_planks
	{

		public BambooPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:bamboo_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BambooPressurePlate : Block // minecraft:bamboo_pressure_plate
	{
		[StateRange(0, 15)] public int RedstoneSignal { get; set; } = 0;

		public BambooPressurePlate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "redstone_signal":
						RedstoneSignal = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_pressure_plate";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "redstone_signal", Value = RedstoneSignal});
			return record;
		} // method
	} // class

	public partial class BambooShelf : Block // minecraft:bamboo_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public BambooShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class BambooSlab : Block // minecraft:bamboo_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BambooSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BambooStairs : Block // minecraft:bamboo_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public BambooStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class BambooStandingSign : Block // minecraft:bamboo_standing_sign
	{
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;

		public BambooStandingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_standing_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			return record;
		} // method
	} // class

	public partial class BambooTrapdoor : Block // minecraft:bamboo_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public BambooTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class BambooWallSign : Block // minecraft:bamboo_wall_sign
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public BambooWallSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bamboo_wall_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class BigDripleaf : Block // minecraft:big_dripleaf
	{
		[StateBit] public bool BigDripleafHead { get; set; } = false;
		[StateEnum("none","unstable","partial_tilt","full_tilt")]
		public string BigDripleafTilt { get; set; } = "none";
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public BigDripleaf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "big_dripleaf_head":
						BigDripleafHead = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "big_dripleaf_tilt":
						BigDripleafTilt = s.Value;
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:big_dripleaf";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "big_dripleaf_head", Value = Convert.ToByte(BigDripleafHead)});
			record.States.Add(new BlockStateString {Name = "big_dripleaf_tilt", Value = BigDripleafTilt});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class BirchDoubleSlab : Block // minecraft:birch_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BirchDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:birch_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BirchFence : Block // minecraft:birch_fence
	{

		public BirchFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:birch_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BirchHangingSign : Block // minecraft:birch_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public BirchHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:birch_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class BirchLeaves : Block // minecraft:birch_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public BirchLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:birch_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class BirchLog : Block // minecraft:birch_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public BirchLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:birch_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class BirchPlanks : Block // minecraft:birch_planks
	{

		public BirchPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:birch_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BirchSapling : Block // minecraft:birch_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public BirchSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:birch_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class BirchShelf : Block // minecraft:birch_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public BirchShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:birch_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class BirchSlab : Block // minecraft:birch_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BirchSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:birch_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BirchWood : Block // minecraft:birch_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public BirchWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:birch_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class BlackCandle : Block // minecraft:black_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public BlackCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:black_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class BlackCandleCake : Block // minecraft:black_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public BlackCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:black_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class BlackCarpet : Block // minecraft:black_carpet
	{

		public BlackCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackConcrete : Block // minecraft:black_concrete
	{

		public BlackConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackConcretePowder : Block // minecraft:black_concrete_powder
	{

		public BlackConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackShulkerBox : Block // minecraft:black_shulker_box
	{

		public BlackShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackStainedGlass : Block // minecraft:black_stained_glass
	{

		public BlackStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackStainedGlassPane : Block // minecraft:black_stained_glass_pane
	{

		public BlackStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackTerracotta : Block // minecraft:black_terracotta
	{

		public BlackTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlackWool : Block // minecraft:black_wool
	{

		public BlackWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:black_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueCandle : Block // minecraft:blue_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public BlueCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:blue_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class BlueCandleCake : Block // minecraft:blue_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public BlueCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:blue_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class BlueCarpet : Block // minecraft:blue_carpet
	{

		public BlueCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueConcrete : Block // minecraft:blue_concrete
	{

		public BlueConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueConcretePowder : Block // minecraft:blue_concrete_powder
	{

		public BlueConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueOrchid : Block // minecraft:blue_orchid
	{

		public BlueOrchid() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_orchid";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueShulkerBox : Block // minecraft:blue_shulker_box
	{

		public BlueShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueStainedGlass : Block // minecraft:blue_stained_glass
	{

		public BlueStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueStainedGlassPane : Block // minecraft:blue_stained_glass_pane
	{

		public BlueStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueTerracotta : Block // minecraft:blue_terracotta
	{

		public BlueTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BlueWool : Block // minecraft:blue_wool
	{

		public BlueWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:blue_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrainCoral : Block // minecraft:brain_coral
	{

		public BrainCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brain_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrainCoralBlock : Block // minecraft:brain_coral_block
	{

		public BrainCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brain_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrainCoralFan : Block // minecraft:brain_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public BrainCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brain_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class BrainCoralWallFan : Block // minecraft:brain_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public BrainCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brain_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class BrickDoubleSlab : Block // minecraft:brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BrickSlab : Block // minecraft:brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public BrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class BrickWall : Block // minecraft:brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public BrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class BrownCandle : Block // minecraft:brown_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public BrownCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brown_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class BrownCandleCake : Block // minecraft:brown_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public BrownCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:brown_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class BrownCarpet : Block // minecraft:brown_carpet
	{

		public BrownCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownConcrete : Block // minecraft:brown_concrete
	{

		public BrownConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownConcretePowder : Block // minecraft:brown_concrete_powder
	{

		public BrownConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownShulkerBox : Block // minecraft:brown_shulker_box
	{

		public BrownShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownStainedGlass : Block // minecraft:brown_stained_glass
	{

		public BrownStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownStainedGlassPane : Block // minecraft:brown_stained_glass_pane
	{

		public BrownStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownTerracotta : Block // minecraft:brown_terracotta
	{

		public BrownTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BrownWool : Block // minecraft:brown_wool
	{

		public BrownWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:brown_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BubbleCoral : Block // minecraft:bubble_coral
	{

		public BubbleCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:bubble_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BubbleCoralBlock : Block // minecraft:bubble_coral_block
	{

		public BubbleCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:bubble_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class BubbleCoralFan : Block // minecraft:bubble_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public BubbleCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bubble_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class BubbleCoralWallFan : Block // minecraft:bubble_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public BubbleCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:bubble_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class BuddingAmethyst : Block // minecraft:budding_amethyst
	{

		public BuddingAmethyst() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:budding_amethyst";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Bush : Block // minecraft:bush
	{

		public Bush() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:bush";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CactusFlower : Block // minecraft:cactus_flower
	{

		public CactusFlower() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cactus_flower";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Calcite : Block // minecraft:calcite
	{

		public Calcite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:calcite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CalibratedSculkSensor : Block // minecraft:calibrated_sculk_sensor
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateRange(0, 2)] public int SculkSensorPhase { get; set; } = 0;

		public CalibratedSculkSensor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "sculk_sensor_phase":
						SculkSensorPhase = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:calibrated_sculk_sensor";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateInt {Name = "sculk_sensor_phase", Value = SculkSensorPhase});
			return record;
		} // method
	} // class

	public partial class Candle : Block // minecraft:candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public Candle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class CandleCake : Block // minecraft:candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public CandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class CaveVines : Block // minecraft:cave_vines
	{
		[StateRange(0, 25)] public int GrowingPlantAge { get; set; } = 0;

		public CaveVines() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growing_plant_age":
						GrowingPlantAge = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cave_vines";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growing_plant_age", Value = GrowingPlantAge});
			return record;
		} // method
	} // class

	public partial class CaveVinesBodyWithBerries : Block // minecraft:cave_vines_body_with_berries
	{
		[StateRange(0, 25)] public int GrowingPlantAge { get; set; } = 0;

		public CaveVinesBodyWithBerries() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growing_plant_age":
						GrowingPlantAge = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cave_vines_body_with_berries";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growing_plant_age", Value = GrowingPlantAge});
			return record;
		} // method
	} // class

	public partial class CaveVinesHeadWithBerries : Block // minecraft:cave_vines_head_with_berries
	{
		[StateRange(0, 25)] public int GrowingPlantAge { get; set; } = 0;

		public CaveVinesHeadWithBerries() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growing_plant_age":
						GrowingPlantAge = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cave_vines_head_with_berries";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growing_plant_age", Value = GrowingPlantAge});
			return record;
		} // method
	} // class

	public partial class CherryButton : Block // minecraft:cherry_button
	{
		[StateBit] public bool ButtonPressedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public CherryButton() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "button_pressed_bit":
						ButtonPressedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_button";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "button_pressed_bit", Value = Convert.ToByte(ButtonPressedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class CherryDoor : Block // minecraft:cherry_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public CherryDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:cherry_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class CherryDoubleSlab : Block // minecraft:cherry_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CherryDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CherryFence : Block // minecraft:cherry_fence
	{

		public CherryFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cherry_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CherryFenceGate : Block // minecraft:cherry_fence_gate
	{
		[StateBit] public bool InWallBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;

		public CherryFenceGate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "in_wall_bit":
						InWallBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_fence_gate";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "in_wall_bit", Value = Convert.ToByte(InWallBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			return record;
		} // method
	} // class

	public partial class CherryHangingSign : Block // minecraft:cherry_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public CherryHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class CherryLeaves : Block // minecraft:cherry_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public CherryLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:cherry_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class CherryLog : Block // minecraft:cherry_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public CherryLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:cherry_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class CherryPlanks : Block // minecraft:cherry_planks
	{

		public CherryPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cherry_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CherryPressurePlate : Block // minecraft:cherry_pressure_plate
	{
		[StateRange(0, 15)] public int RedstoneSignal { get; set; } = 0;

		public CherryPressurePlate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "redstone_signal":
						RedstoneSignal = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_pressure_plate";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "redstone_signal", Value = RedstoneSignal});
			return record;
		} // method
	} // class

	public partial class CherrySapling : Block // minecraft:cherry_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public CherrySapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class CherryShelf : Block // minecraft:cherry_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public CherryShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class CherrySlab : Block // minecraft:cherry_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CherrySlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CherryStairs : Block // minecraft:cherry_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public CherryStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class CherryStandingSign : Block // minecraft:cherry_standing_sign
	{
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;

		public CherryStandingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_standing_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			return record;
		} // method
	} // class

	public partial class CherryTrapdoor : Block // minecraft:cherry_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public CherryTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class CherryWallSign : Block // minecraft:cherry_wall_sign
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public CherryWallSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cherry_wall_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class CherryWood : Block // minecraft:cherry_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public CherryWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:cherry_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class ChippedAnvil : Block // minecraft:chipped_anvil
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public ChippedAnvil() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:chipped_anvil";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class ChiseledBookshelf : Block // minecraft:chiseled_bookshelf
	{
		[StateRange(0, 63)] public int BooksStored { get; set; } = 0;
		[StateRange(0, 3)] public int Direction { get; set; } = 0;

		public ChiseledBookshelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "books_stored":
						BooksStored = s.Value;
						break;
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:chiseled_bookshelf";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "books_stored", Value = BooksStored});
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			return record;
		} // method
	} // class

	public partial class ChiseledCinnabar : Block // minecraft:chiseled_cinnabar
	{

		public ChiseledCinnabar() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_cinnabar";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledCopper : Block // minecraft:chiseled_copper
	{

		public ChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledDeepslate : Block // minecraft:chiseled_deepslate
	{

		public ChiseledDeepslate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_deepslate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledQuartzBlock : Block // minecraft:chiseled_quartz_block
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public ChiseledQuartzBlock() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:chiseled_quartz_block";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class ChiseledRedSandstone : Block // minecraft:chiseled_red_sandstone
	{

		public ChiseledRedSandstone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_red_sandstone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledResinBricks : Block // minecraft:chiseled_resin_bricks
	{

		public ChiseledResinBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_resin_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledSandstone : Block // minecraft:chiseled_sandstone
	{

		public ChiseledSandstone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_sandstone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledStoneBricks : Block // minecraft:chiseled_stone_bricks
	{

		public ChiseledStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledSulfur : Block // minecraft:chiseled_sulfur
	{

		public ChiseledSulfur() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_sulfur";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledTuff : Block // minecraft:chiseled_tuff
	{

		public ChiseledTuff() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_tuff";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ChiseledTuffBricks : Block // minecraft:chiseled_tuff_bricks
	{

		public ChiseledTuffBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:chiseled_tuff_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Cinnabar : Block // minecraft:cinnabar
	{

		public Cinnabar() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cinnabar";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CinnabarBrickDoubleSlab : Block // minecraft:cinnabar_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CinnabarBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CinnabarBrickSlab : Block // minecraft:cinnabar_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CinnabarBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CinnabarBrickStairs : Block // minecraft:cinnabar_brick_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public CinnabarBrickStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_brick_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class CinnabarBrickWall : Block // minecraft:cinnabar_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public CinnabarBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class CinnabarBricks : Block // minecraft:cinnabar_bricks
	{

		public CinnabarBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cinnabar_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CinnabarDoubleSlab : Block // minecraft:cinnabar_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CinnabarDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CinnabarSlab : Block // minecraft:cinnabar_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CinnabarSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CinnabarStairs : Block // minecraft:cinnabar_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public CinnabarStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class CinnabarWall : Block // minecraft:cinnabar_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public CinnabarWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cinnabar_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class ClientRequestPlaceholderBlock : Block // minecraft:client_request_placeholder_block
	{

		public ClientRequestPlaceholderBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:client_request_placeholder_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ClosedEyeblossom : Block // minecraft:closed_eyeblossom
	{

		public ClosedEyeblossom() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:closed_eyeblossom";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CoarseDirt : Block // minecraft:coarse_dirt
	{

		public CoarseDirt() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:coarse_dirt";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CobbledDeepslate : Block // minecraft:cobbled_deepslate
	{

		public CobbledDeepslate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cobbled_deepslate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CobbledDeepslateDoubleSlab : Block // minecraft:cobbled_deepslate_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CobbledDeepslateDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cobbled_deepslate_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CobbledDeepslateSlab : Block // minecraft:cobbled_deepslate_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CobbledDeepslateSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cobbled_deepslate_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CobbledDeepslateStairs : Block // minecraft:cobbled_deepslate_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public CobbledDeepslateStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cobbled_deepslate_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class CobbledDeepslateWall : Block // minecraft:cobbled_deepslate_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public CobbledDeepslateWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cobbled_deepslate_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class CobblestoneDoubleSlab : Block // minecraft:cobblestone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CobblestoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cobblestone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CobblestoneSlab : Block // minecraft:cobblestone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CobblestoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cobblestone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class ColoredTorchBlue : Block // minecraft:colored_torch_blue
	{
		[StateEnum("unknown","west","east","north","south","top")]
		public string TorchFacingDirection { get; set; } = "unknown";

		public ColoredTorchBlue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "torch_facing_direction":
						TorchFacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:colored_torch_blue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "torch_facing_direction", Value = TorchFacingDirection});
			return record;
		} // method
	} // class

	public partial class ColoredTorchGreen : Block // minecraft:colored_torch_green
	{
		[StateEnum("unknown","west","east","north","south","top")]
		public string TorchFacingDirection { get; set; } = "unknown";

		public ColoredTorchGreen() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "torch_facing_direction":
						TorchFacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:colored_torch_green";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "torch_facing_direction", Value = TorchFacingDirection});
			return record;
		} // method
	} // class

	public partial class ColoredTorchPurple : Block // minecraft:colored_torch_purple
	{
		[StateEnum("unknown","west","east","north","south","top")]
		public string TorchFacingDirection { get; set; } = "unknown";

		public ColoredTorchPurple() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "torch_facing_direction":
						TorchFacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:colored_torch_purple";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "torch_facing_direction", Value = TorchFacingDirection});
			return record;
		} // method
	} // class

	public partial class ColoredTorchRed : Block // minecraft:colored_torch_red
	{
		[StateEnum("unknown","west","east","north","south","top")]
		public string TorchFacingDirection { get; set; } = "unknown";

		public ColoredTorchRed() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "torch_facing_direction":
						TorchFacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:colored_torch_red";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "torch_facing_direction", Value = TorchFacingDirection});
			return record;
		} // method
	} // class

	public partial class CompoundCreator : Block // minecraft:compound_creator
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;

		public CompoundCreator() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:compound_creator";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			return record;
		} // method
	} // class

	public partial class CopperBars : Block // minecraft:copper_bars
	{

		public CopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CopperBlock : Block // minecraft:copper_block
	{

		public CopperBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:copper_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CopperBulb : Block // minecraft:copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public CopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class CopperChain : Block // minecraft:copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public CopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class CopperChest : Block // minecraft:copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public CopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class CopperDoor : Block // minecraft:copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public CopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class CopperGolemStatue : Block // minecraft:copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public CopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class CopperGrate : Block // minecraft:copper_grate
	{

		public CopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CopperLantern : Block // minecraft:copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public CopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class CopperOre : Block // minecraft:copper_ore
	{

		public CopperOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:copper_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CopperTorch : Block // minecraft:copper_torch
	{
		[StateEnum("unknown","west","east","north","south","top")]
		public string TorchFacingDirection { get; set; } = "unknown";

		public CopperTorch() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "torch_facing_direction":
						TorchFacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:copper_torch";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "torch_facing_direction", Value = TorchFacingDirection});
			return record;
		} // method
	} // class

	public partial class CopperTrapdoor : Block // minecraft:copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public CopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class Cornflower : Block // minecraft:cornflower
	{

		public Cornflower() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cornflower";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CrackedDeepslateBricks : Block // minecraft:cracked_deepslate_bricks
	{

		public CrackedDeepslateBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cracked_deepslate_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CrackedDeepslateTiles : Block // minecraft:cracked_deepslate_tiles
	{

		public CrackedDeepslateTiles() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cracked_deepslate_tiles";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CrackedStoneBricks : Block // minecraft:cracked_stone_bricks
	{

		public CrackedStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cracked_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Crafter : Block // minecraft:crafter
	{
		[StateBit] public bool Crafting { get; set; } = false;
		[StateEnum("down_east","down_north","down_south","down_west","up_east","up_north","up_south","up_west","west_up","east_up","north_up","south_up")]
		public string Orientation { get; set; } = "down_east";
		[StateBit] public bool TriggeredBit { get; set; } = false;

		public Crafter() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "crafting":
						Crafting = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "orientation":
						Orientation = s.Value;
						break;
					case BlockStateByte s when s.Name == "triggered_bit":
						TriggeredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:crafter";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "crafting", Value = Convert.ToByte(Crafting)});
			record.States.Add(new BlockStateString {Name = "orientation", Value = Orientation});
			record.States.Add(new BlockStateByte {Name = "triggered_bit", Value = Convert.ToByte(TriggeredBit)});
			return record;
		} // method
	} // class

	public partial class CreakingHeart : Block // minecraft:creaking_heart
	{
		[StateEnum("uprooted","dormant","awake")]
		public string CreakingHeartState { get; set; } = "uprooted";
		[StateBit] public bool Natural { get; set; } = false;
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public CreakingHeart() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "creaking_heart_state":
						CreakingHeartState = s.Value;
						break;
					case BlockStateByte s when s.Name == "natural":
						Natural = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:creaking_heart";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "creaking_heart_state", Value = CreakingHeartState});
			record.States.Add(new BlockStateByte {Name = "natural", Value = Convert.ToByte(Natural)});
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class CreeperHead : Block // minecraft:creeper_head
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public CreeperHead() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:creeper_head";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class CrimsonHangingSign : Block // minecraft:crimson_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public CrimsonHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:crimson_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class CrimsonShelf : Block // minecraft:crimson_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public CrimsonShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:crimson_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class CutCopper : Block // minecraft:cut_copper
	{

		public CutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CutCopperSlab : Block // minecraft:cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CutCopperStairs : Block // minecraft:cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public CutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class CutRedSandstone : Block // minecraft:cut_red_sandstone
	{

		public CutRedSandstone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cut_red_sandstone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CutRedSandstoneDoubleSlab : Block // minecraft:cut_red_sandstone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CutRedSandstoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cut_red_sandstone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CutRedSandstoneSlab : Block // minecraft:cut_red_sandstone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CutRedSandstoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cut_red_sandstone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CutSandstone : Block // minecraft:cut_sandstone
	{

		public CutSandstone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cut_sandstone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CutSandstoneDoubleSlab : Block // minecraft:cut_sandstone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CutSandstoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cut_sandstone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CutSandstoneSlab : Block // minecraft:cut_sandstone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public CutSandstoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cut_sandstone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class CyanCandle : Block // minecraft:cyan_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public CyanCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cyan_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class CyanCandleCake : Block // minecraft:cyan_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public CyanCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:cyan_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class CyanCarpet : Block // minecraft:cyan_carpet
	{

		public CyanCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanConcrete : Block // minecraft:cyan_concrete
	{

		public CyanConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanConcretePowder : Block // minecraft:cyan_concrete_powder
	{

		public CyanConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanShulkerBox : Block // minecraft:cyan_shulker_box
	{

		public CyanShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanStainedGlass : Block // minecraft:cyan_stained_glass
	{

		public CyanStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanStainedGlassPane : Block // minecraft:cyan_stained_glass_pane
	{

		public CyanStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanTerracotta : Block // minecraft:cyan_terracotta
	{

		public CyanTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class CyanWool : Block // minecraft:cyan_wool
	{

		public CyanWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:cyan_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DamagedAnvil : Block // minecraft:damaged_anvil
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public DamagedAnvil() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:damaged_anvil";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class Dandelion : Block // minecraft:dandelion
	{

		public Dandelion() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dandelion";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DarkOakDoubleSlab : Block // minecraft:dark_oak_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DarkOakDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_oak_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DarkOakFence : Block // minecraft:dark_oak_fence
	{

		public DarkOakFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dark_oak_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DarkOakHangingSign : Block // minecraft:dark_oak_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public DarkOakHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_oak_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class DarkOakLeaves : Block // minecraft:dark_oak_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public DarkOakLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:dark_oak_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class DarkOakLog : Block // minecraft:dark_oak_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public DarkOakLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:dark_oak_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class DarkOakPlanks : Block // minecraft:dark_oak_planks
	{

		public DarkOakPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dark_oak_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DarkOakSapling : Block // minecraft:dark_oak_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public DarkOakSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_oak_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class DarkOakShelf : Block // minecraft:dark_oak_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public DarkOakShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_oak_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class DarkOakSlab : Block // minecraft:dark_oak_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DarkOakSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_oak_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DarkOakWood : Block // minecraft:dark_oak_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public DarkOakWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:dark_oak_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class DarkPrismarine : Block // minecraft:dark_prismarine
	{

		public DarkPrismarine() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dark_prismarine";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DarkPrismarineDoubleSlab : Block // minecraft:dark_prismarine_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DarkPrismarineDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_prismarine_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DarkPrismarineSlab : Block // minecraft:dark_prismarine_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DarkPrismarineSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dark_prismarine_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DeadBrainCoral : Block // minecraft:dead_brain_coral
	{

		public DeadBrainCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_brain_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadBrainCoralBlock : Block // minecraft:dead_brain_coral_block
	{

		public DeadBrainCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_brain_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadBrainCoralFan : Block // minecraft:dead_brain_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public DeadBrainCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_brain_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class DeadBrainCoralWallFan : Block // minecraft:dead_brain_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public DeadBrainCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_brain_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class DeadBubbleCoral : Block // minecraft:dead_bubble_coral
	{

		public DeadBubbleCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_bubble_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadBubbleCoralBlock : Block // minecraft:dead_bubble_coral_block
	{

		public DeadBubbleCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_bubble_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadBubbleCoralFan : Block // minecraft:dead_bubble_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public DeadBubbleCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_bubble_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class DeadBubbleCoralWallFan : Block // minecraft:dead_bubble_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public DeadBubbleCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_bubble_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class DeadFireCoral : Block // minecraft:dead_fire_coral
	{

		public DeadFireCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_fire_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadFireCoralBlock : Block // minecraft:dead_fire_coral_block
	{

		public DeadFireCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_fire_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadFireCoralFan : Block // minecraft:dead_fire_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public DeadFireCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_fire_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class DeadFireCoralWallFan : Block // minecraft:dead_fire_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public DeadFireCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_fire_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class DeadHornCoral : Block // minecraft:dead_horn_coral
	{

		public DeadHornCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_horn_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadHornCoralBlock : Block // minecraft:dead_horn_coral_block
	{

		public DeadHornCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_horn_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadHornCoralFan : Block // minecraft:dead_horn_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public DeadHornCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_horn_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class DeadHornCoralWallFan : Block // minecraft:dead_horn_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public DeadHornCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_horn_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class DeadTubeCoral : Block // minecraft:dead_tube_coral
	{

		public DeadTubeCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_tube_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadTubeCoralBlock : Block // minecraft:dead_tube_coral_block
	{

		public DeadTubeCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dead_tube_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeadTubeCoralFan : Block // minecraft:dead_tube_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public DeadTubeCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_tube_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class DeadTubeCoralWallFan : Block // minecraft:dead_tube_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public DeadTubeCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dead_tube_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class DecoratedPot : Block // minecraft:decorated_pot
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;

		public DecoratedPot() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:decorated_pot";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			return record;
		} // method
	} // class

	public partial class Deepslate : Block // minecraft:deepslate
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public Deepslate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:deepslate";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class DeepslateBrickDoubleSlab : Block // minecraft:deepslate_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DeepslateBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DeepslateBrickSlab : Block // minecraft:deepslate_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DeepslateBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DeepslateBrickStairs : Block // minecraft:deepslate_brick_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public DeepslateBrickStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_brick_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class DeepslateBrickWall : Block // minecraft:deepslate_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public DeepslateBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class DeepslateBricks : Block // minecraft:deepslate_bricks
	{

		public DeepslateBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateCoalOre : Block // minecraft:deepslate_coal_ore
	{

		public DeepslateCoalOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_coal_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateCopperOre : Block // minecraft:deepslate_copper_ore
	{

		public DeepslateCopperOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_copper_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateDiamondOre : Block // minecraft:deepslate_diamond_ore
	{

		public DeepslateDiamondOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_diamond_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateEmeraldOre : Block // minecraft:deepslate_emerald_ore
	{

		public DeepslateEmeraldOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_emerald_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateGoldOre : Block // minecraft:deepslate_gold_ore
	{

		public DeepslateGoldOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_gold_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateIronOre : Block // minecraft:deepslate_iron_ore
	{

		public DeepslateIronOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_iron_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateLapisOre : Block // minecraft:deepslate_lapis_ore
	{

		public DeepslateLapisOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_lapis_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateRedstoneOre : Block // minecraft:deepslate_redstone_ore
	{

		public DeepslateRedstoneOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_redstone_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeepslateTileDoubleSlab : Block // minecraft:deepslate_tile_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DeepslateTileDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_tile_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DeepslateTileSlab : Block // minecraft:deepslate_tile_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DeepslateTileSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_tile_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DeepslateTileStairs : Block // minecraft:deepslate_tile_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public DeepslateTileStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_tile_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class DeepslateTileWall : Block // minecraft:deepslate_tile_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public DeepslateTileWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deepslate_tile_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class DeepslateTiles : Block // minecraft:deepslate_tiles
	{

		public DeepslateTiles() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:deepslate_tiles";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DeprecatedAnvil : Block // minecraft:deprecated_anvil
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public DeprecatedAnvil() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:deprecated_anvil";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class DeprecatedPurpurBlock1 : Block // minecraft:deprecated_purpur_block_1
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public DeprecatedPurpurBlock1() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:deprecated_purpur_block_1";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class DeprecatedPurpurBlock2 : Block // minecraft:deprecated_purpur_block_2
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public DeprecatedPurpurBlock2() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:deprecated_purpur_block_2";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class Diorite : Block // minecraft:diorite
	{

		public Diorite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:diorite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DioriteDoubleSlab : Block // minecraft:diorite_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DioriteDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:diorite_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DioriteSlab : Block // minecraft:diorite_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DioriteSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:diorite_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DioriteWall : Block // minecraft:diorite_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public DioriteWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:diorite_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class DirtWithRoots : Block // minecraft:dirt_with_roots
	{

		public DirtWithRoots() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dirt_with_roots";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class DoubleCutCopperSlab : Block // minecraft:double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public DoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class DragonHead : Block // minecraft:dragon_head
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public DragonHead() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dragon_head";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class DriedGhast : Block // minecraft:dried_ghast
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateRange(0, 3)] public int RehydrationLevel { get; set; } = 0;

		public DriedGhast() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "rehydration_level":
						RehydrationLevel = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:dried_ghast";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateInt {Name = "rehydration_level", Value = RehydrationLevel});
			return record;
		} // method
	} // class

	public partial class DripstoneBlock : Block // minecraft:dripstone_block
	{

		public DripstoneBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:dripstone_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ElementConstructor : Block // minecraft:element_constructor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;

		public ElementConstructor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:element_constructor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			return record;
		} // method
	} // class

	public partial class EndStoneBrickDoubleSlab : Block // minecraft:end_stone_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public EndStoneBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:end_stone_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class EndStoneBrickSlab : Block // minecraft:end_stone_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public EndStoneBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:end_stone_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class EndStoneBrickWall : Block // minecraft:end_stone_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public EndStoneBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:end_stone_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class ExposedChiseledCopper : Block // minecraft:exposed_chiseled_copper
	{

		public ExposedChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:exposed_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ExposedCopper : Block // minecraft:exposed_copper
	{

		public ExposedCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:exposed_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ExposedCopperBars : Block // minecraft:exposed_copper_bars
	{

		public ExposedCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:exposed_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ExposedCopperBulb : Block // minecraft:exposed_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public ExposedCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class ExposedCopperChain : Block // minecraft:exposed_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public ExposedCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:exposed_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class ExposedCopperChest : Block // minecraft:exposed_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public ExposedCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class ExposedCopperDoor : Block // minecraft:exposed_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public ExposedCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:exposed_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class ExposedCopperGolemStatue : Block // minecraft:exposed_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public ExposedCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class ExposedCopperGrate : Block // minecraft:exposed_copper_grate
	{

		public ExposedCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:exposed_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ExposedCopperLantern : Block // minecraft:exposed_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public ExposedCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class ExposedCopperTrapdoor : Block // minecraft:exposed_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public ExposedCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class ExposedCutCopper : Block // minecraft:exposed_cut_copper
	{

		public ExposedCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:exposed_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ExposedCutCopperSlab : Block // minecraft:exposed_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public ExposedCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class ExposedCutCopperStairs : Block // minecraft:exposed_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public ExposedCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class ExposedDoubleCutCopperSlab : Block // minecraft:exposed_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public ExposedDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class ExposedLightningRod : Block // minecraft:exposed_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public ExposedLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:exposed_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class Fern : Block // minecraft:fern
	{

		public Fern() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:fern";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class FireCoral : Block // minecraft:fire_coral
	{

		public FireCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:fire_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class FireCoralBlock : Block // minecraft:fire_coral_block
	{

		public FireCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:fire_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class FireCoralFan : Block // minecraft:fire_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public FireCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:fire_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class FireCoralWallFan : Block // minecraft:fire_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public FireCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:fire_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class FireflyBush : Block // minecraft:firefly_bush
	{

		public FireflyBush() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:firefly_bush";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class FloweringAzalea : Block // minecraft:flowering_azalea
	{

		public FloweringAzalea() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:flowering_azalea";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class FrogSpawn : Block // minecraft:frog_spawn
	{

		public FrogSpawn() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:frog_spawn";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GlowFrame : Block // minecraft:glow_frame
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool ItemFrameMapBit { get; set; } = false;
		[StateBit] public bool ItemFramePhotoBit { get; set; } = false;

		public GlowFrame() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "item_frame_map_bit":
						ItemFrameMapBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "item_frame_photo_bit":
						ItemFramePhotoBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:glow_frame";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "item_frame_map_bit", Value = Convert.ToByte(ItemFrameMapBit)});
			record.States.Add(new BlockStateByte {Name = "item_frame_photo_bit", Value = Convert.ToByte(ItemFramePhotoBit)});
			return record;
		} // method
	} // class

	public partial class GlowLichen : Block // minecraft:glow_lichen
	{
		[StateRange(0, 63)] public int MultiFaceDirectionBits { get; set; } = 0;

		public GlowLichen() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "multi_face_direction_bits":
						MultiFaceDirectionBits = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:glow_lichen";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "multi_face_direction_bits", Value = MultiFaceDirectionBits});
			return record;
		} // method
	} // class

	public partial class GoldenDandelion : Block // minecraft:golden_dandelion
	{

		public GoldenDandelion() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:golden_dandelion";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Granite : Block // minecraft:granite
	{

		public Granite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:granite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GraniteDoubleSlab : Block // minecraft:granite_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public GraniteDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:granite_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class GraniteSlab : Block // minecraft:granite_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public GraniteSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:granite_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class GraniteWall : Block // minecraft:granite_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public GraniteWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:granite_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class GrassBlock : Block // minecraft:grass_block
	{

		public GrassBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayCandle : Block // minecraft:gray_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public GrayCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:gray_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class GrayCandleCake : Block // minecraft:gray_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public GrayCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:gray_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class GrayCarpet : Block // minecraft:gray_carpet
	{

		public GrayCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayConcrete : Block // minecraft:gray_concrete
	{

		public GrayConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayConcretePowder : Block // minecraft:gray_concrete_powder
	{

		public GrayConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayShulkerBox : Block // minecraft:gray_shulker_box
	{

		public GrayShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayStainedGlass : Block // minecraft:gray_stained_glass
	{

		public GrayStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayStainedGlassPane : Block // minecraft:gray_stained_glass_pane
	{

		public GrayStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayTerracotta : Block // minecraft:gray_terracotta
	{

		public GrayTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GrayWool : Block // minecraft:gray_wool
	{

		public GrayWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:gray_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenCandle : Block // minecraft:green_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public GreenCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:green_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class GreenCandleCake : Block // minecraft:green_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public GreenCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:green_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class GreenCarpet : Block // minecraft:green_carpet
	{

		public GreenCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenConcrete : Block // minecraft:green_concrete
	{

		public GreenConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenConcretePowder : Block // minecraft:green_concrete_powder
	{

		public GreenConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenShulkerBox : Block // minecraft:green_shulker_box
	{

		public GreenShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenStainedGlass : Block // minecraft:green_stained_glass
	{

		public GreenStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenStainedGlassPane : Block // minecraft:green_stained_glass_pane
	{

		public GreenStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenTerracotta : Block // minecraft:green_terracotta
	{

		public GreenTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class GreenWool : Block // minecraft:green_wool
	{

		public GreenWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:green_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HangingRoots : Block // minecraft:hanging_roots
	{

		public HangingRoots() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hanging_roots";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardBlackStainedGlass : Block // minecraft:hard_black_stained_glass
	{

		public HardBlackStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_black_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardBlackStainedGlassPane : Block // minecraft:hard_black_stained_glass_pane
	{

		public HardBlackStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_black_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardBlueStainedGlass : Block // minecraft:hard_blue_stained_glass
	{

		public HardBlueStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_blue_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardBlueStainedGlassPane : Block // minecraft:hard_blue_stained_glass_pane
	{

		public HardBlueStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_blue_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardBrownStainedGlass : Block // minecraft:hard_brown_stained_glass
	{

		public HardBrownStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_brown_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardBrownStainedGlassPane : Block // minecraft:hard_brown_stained_glass_pane
	{

		public HardBrownStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_brown_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardCyanStainedGlass : Block // minecraft:hard_cyan_stained_glass
	{

		public HardCyanStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_cyan_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardCyanStainedGlassPane : Block // minecraft:hard_cyan_stained_glass_pane
	{

		public HardCyanStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_cyan_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardGrayStainedGlass : Block // minecraft:hard_gray_stained_glass
	{

		public HardGrayStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_gray_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardGrayStainedGlassPane : Block // minecraft:hard_gray_stained_glass_pane
	{

		public HardGrayStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_gray_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardGreenStainedGlass : Block // minecraft:hard_green_stained_glass
	{

		public HardGreenStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_green_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardGreenStainedGlassPane : Block // minecraft:hard_green_stained_glass_pane
	{

		public HardGreenStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_green_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardLightBlueStainedGlass : Block // minecraft:hard_light_blue_stained_glass
	{

		public HardLightBlueStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_light_blue_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardLightBlueStainedGlassPane : Block // minecraft:hard_light_blue_stained_glass_pane
	{

		public HardLightBlueStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_light_blue_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardLightGrayStainedGlass : Block // minecraft:hard_light_gray_stained_glass
	{

		public HardLightGrayStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_light_gray_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardLightGrayStainedGlassPane : Block // minecraft:hard_light_gray_stained_glass_pane
	{

		public HardLightGrayStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_light_gray_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardLimeStainedGlass : Block // minecraft:hard_lime_stained_glass
	{

		public HardLimeStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_lime_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardLimeStainedGlassPane : Block // minecraft:hard_lime_stained_glass_pane
	{

		public HardLimeStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_lime_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardMagentaStainedGlass : Block // minecraft:hard_magenta_stained_glass
	{

		public HardMagentaStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_magenta_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardMagentaStainedGlassPane : Block // minecraft:hard_magenta_stained_glass_pane
	{

		public HardMagentaStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_magenta_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardOrangeStainedGlass : Block // minecraft:hard_orange_stained_glass
	{

		public HardOrangeStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_orange_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardOrangeStainedGlassPane : Block // minecraft:hard_orange_stained_glass_pane
	{

		public HardOrangeStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_orange_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardPinkStainedGlass : Block // minecraft:hard_pink_stained_glass
	{

		public HardPinkStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_pink_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardPinkStainedGlassPane : Block // minecraft:hard_pink_stained_glass_pane
	{

		public HardPinkStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_pink_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardPurpleStainedGlass : Block // minecraft:hard_purple_stained_glass
	{

		public HardPurpleStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_purple_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardPurpleStainedGlassPane : Block // minecraft:hard_purple_stained_glass_pane
	{

		public HardPurpleStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_purple_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardRedStainedGlass : Block // minecraft:hard_red_stained_glass
	{

		public HardRedStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_red_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardRedStainedGlassPane : Block // minecraft:hard_red_stained_glass_pane
	{

		public HardRedStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_red_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardWhiteStainedGlass : Block // minecraft:hard_white_stained_glass
	{

		public HardWhiteStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_white_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardWhiteStainedGlassPane : Block // minecraft:hard_white_stained_glass_pane
	{

		public HardWhiteStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_white_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardYellowStainedGlass : Block // minecraft:hard_yellow_stained_glass
	{

		public HardYellowStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_yellow_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HardYellowStainedGlassPane : Block // minecraft:hard_yellow_stained_glass_pane
	{

		public HardYellowStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:hard_yellow_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HeavyCore : Block // minecraft:heavy_core
	{

		public HeavyCore() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:heavy_core";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HornCoral : Block // minecraft:horn_coral
	{

		public HornCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:horn_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HornCoralBlock : Block // minecraft:horn_coral_block
	{

		public HornCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:horn_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class HornCoralFan : Block // minecraft:horn_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public HornCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:horn_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class HornCoralWallFan : Block // minecraft:horn_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public HornCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:horn_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class InfestedChiseledStoneBricks : Block // minecraft:infested_chiseled_stone_bricks
	{

		public InfestedChiseledStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:infested_chiseled_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class InfestedCobblestone : Block // minecraft:infested_cobblestone
	{

		public InfestedCobblestone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:infested_cobblestone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class InfestedCrackedStoneBricks : Block // minecraft:infested_cracked_stone_bricks
	{

		public InfestedCrackedStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:infested_cracked_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class InfestedDeepslate : Block // minecraft:infested_deepslate
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public InfestedDeepslate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:infested_deepslate";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class InfestedMossyStoneBricks : Block // minecraft:infested_mossy_stone_bricks
	{

		public InfestedMossyStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:infested_mossy_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class InfestedStone : Block // minecraft:infested_stone
	{

		public InfestedStone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:infested_stone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class InfestedStoneBricks : Block // minecraft:infested_stone_bricks
	{

		public InfestedStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:infested_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class IronChain : Block // minecraft:iron_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public IronChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:iron_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class JungleDoubleSlab : Block // minecraft:jungle_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public JungleDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:jungle_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class JungleFence : Block // minecraft:jungle_fence
	{

		public JungleFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:jungle_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class JungleHangingSign : Block // minecraft:jungle_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public JungleHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:jungle_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class JungleLeaves : Block // minecraft:jungle_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public JungleLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:jungle_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class JungleLog : Block // minecraft:jungle_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public JungleLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:jungle_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class JunglePlanks : Block // minecraft:jungle_planks
	{

		public JunglePlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:jungle_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class JungleSapling : Block // minecraft:jungle_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public JungleSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:jungle_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class JungleShelf : Block // minecraft:jungle_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public JungleShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:jungle_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class JungleSlab : Block // minecraft:jungle_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public JungleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:jungle_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class JungleWood : Block // minecraft:jungle_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public JungleWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:jungle_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class LabTable : Block // minecraft:lab_table
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;

		public LabTable() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:lab_table";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			return record;
		} // method
	} // class

	public partial class LargeAmethystBud : Block // minecraft:large_amethyst_bud
	{
		[StateEnum("down","up","north","south","west","east")]
		public string BlockFace { get; set; } = "down";

		public LargeAmethystBud() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:block_face":
						BlockFace = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:large_amethyst_bud";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:block_face", Value = BlockFace});
			return record;
		} // method
	} // class

	public partial class LargeFern : Block // minecraft:large_fern
	{
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public LargeFern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upper_block_bit":
						UpperBlockBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:large_fern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class LeafLitter : Block // minecraft:leaf_litter
	{
		[StateRange(0, 7)] public int Growth { get; set; } = 0;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public LeafLitter() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growth":
						Growth = s.Value;
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:leaf_litter";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growth", Value = Growth});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class LightBlock0 : Block // minecraft:light_block_0
	{

		public LightBlock0() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_0";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock1 : Block // minecraft:light_block_1
	{

		public LightBlock1() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_1";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock10 : Block // minecraft:light_block_10
	{

		public LightBlock10() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_10";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock11 : Block // minecraft:light_block_11
	{

		public LightBlock11() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_11";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock12 : Block // minecraft:light_block_12
	{

		public LightBlock12() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_12";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock13 : Block // minecraft:light_block_13
	{

		public LightBlock13() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_13";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock14 : Block // minecraft:light_block_14
	{

		public LightBlock14() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_14";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock15 : Block // minecraft:light_block_15
	{

		public LightBlock15() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_15";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock2 : Block // minecraft:light_block_2
	{

		public LightBlock2() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_2";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock3 : Block // minecraft:light_block_3
	{

		public LightBlock3() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_3";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock4 : Block // minecraft:light_block_4
	{

		public LightBlock4() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_4";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock5 : Block // minecraft:light_block_5
	{

		public LightBlock5() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_5";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock6 : Block // minecraft:light_block_6
	{

		public LightBlock6() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_6";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock7 : Block // minecraft:light_block_7
	{

		public LightBlock7() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_7";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock8 : Block // minecraft:light_block_8
	{

		public LightBlock8() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_8";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlock9 : Block // minecraft:light_block_9
	{

		public LightBlock9() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_block_9";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueCandle : Block // minecraft:light_blue_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public LightBlueCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:light_blue_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class LightBlueCandleCake : Block // minecraft:light_blue_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public LightBlueCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:light_blue_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class LightBlueCarpet : Block // minecraft:light_blue_carpet
	{

		public LightBlueCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueConcrete : Block // minecraft:light_blue_concrete
	{

		public LightBlueConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueConcretePowder : Block // minecraft:light_blue_concrete_powder
	{

		public LightBlueConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueShulkerBox : Block // minecraft:light_blue_shulker_box
	{

		public LightBlueShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueStainedGlass : Block // minecraft:light_blue_stained_glass
	{

		public LightBlueStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueStainedGlassPane : Block // minecraft:light_blue_stained_glass_pane
	{

		public LightBlueStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueTerracotta : Block // minecraft:light_blue_terracotta
	{

		public LightBlueTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightBlueWool : Block // minecraft:light_blue_wool
	{

		public LightBlueWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_blue_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayCandle : Block // minecraft:light_gray_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public LightGrayCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:light_gray_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class LightGrayCandleCake : Block // minecraft:light_gray_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public LightGrayCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:light_gray_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class LightGrayCarpet : Block // minecraft:light_gray_carpet
	{

		public LightGrayCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayConcrete : Block // minecraft:light_gray_concrete
	{

		public LightGrayConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayConcretePowder : Block // minecraft:light_gray_concrete_powder
	{

		public LightGrayConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayShulkerBox : Block // minecraft:light_gray_shulker_box
	{

		public LightGrayShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayStainedGlass : Block // minecraft:light_gray_stained_glass
	{

		public LightGrayStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayStainedGlassPane : Block // minecraft:light_gray_stained_glass_pane
	{

		public LightGrayStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayTerracotta : Block // minecraft:light_gray_terracotta
	{

		public LightGrayTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightGrayWool : Block // minecraft:light_gray_wool
	{

		public LightGrayWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:light_gray_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LightningRod : Block // minecraft:lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public LightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class Lilac : Block // minecraft:lilac
	{
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public Lilac() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upper_block_bit":
						UpperBlockBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:lilac";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class LilyOfTheValley : Block // minecraft:lily_of_the_valley
	{

		public LilyOfTheValley() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lily_of_the_valley";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeCandle : Block // minecraft:lime_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public LimeCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:lime_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class LimeCandleCake : Block // minecraft:lime_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public LimeCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:lime_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class LimeCarpet : Block // minecraft:lime_carpet
	{

		public LimeCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeConcrete : Block // minecraft:lime_concrete
	{

		public LimeConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeConcretePowder : Block // minecraft:lime_concrete_powder
	{

		public LimeConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeShulkerBox : Block // minecraft:lime_shulker_box
	{

		public LimeShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeStainedGlass : Block // minecraft:lime_stained_glass
	{

		public LimeStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeStainedGlassPane : Block // minecraft:lime_stained_glass_pane
	{

		public LimeStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeTerracotta : Block // minecraft:lime_terracotta
	{

		public LimeTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LimeWool : Block // minecraft:lime_wool
	{

		public LimeWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lime_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class LitDeepslateRedstoneOre : Block // minecraft:lit_deepslate_redstone_ore
	{

		public LitDeepslateRedstoneOre() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:lit_deepslate_redstone_ore";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaCandle : Block // minecraft:magenta_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public MagentaCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:magenta_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class MagentaCandleCake : Block // minecraft:magenta_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public MagentaCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:magenta_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class MagentaCarpet : Block // minecraft:magenta_carpet
	{

		public MagentaCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaConcrete : Block // minecraft:magenta_concrete
	{

		public MagentaConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaConcretePowder : Block // minecraft:magenta_concrete_powder
	{

		public MagentaConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaShulkerBox : Block // minecraft:magenta_shulker_box
	{

		public MagentaShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaStainedGlass : Block // minecraft:magenta_stained_glass
	{

		public MagentaStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaStainedGlassPane : Block // minecraft:magenta_stained_glass_pane
	{

		public MagentaStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaTerracotta : Block // minecraft:magenta_terracotta
	{

		public MagentaTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MagentaWool : Block // minecraft:magenta_wool
	{

		public MagentaWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:magenta_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MangroveButton : Block // minecraft:mangrove_button
	{
		[StateBit] public bool ButtonPressedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public MangroveButton() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "button_pressed_bit":
						ButtonPressedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_button";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "button_pressed_bit", Value = Convert.ToByte(ButtonPressedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class MangroveDoor : Block // minecraft:mangrove_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public MangroveDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:mangrove_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class MangroveDoubleSlab : Block // minecraft:mangrove_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MangroveDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MangroveFence : Block // minecraft:mangrove_fence
	{

		public MangroveFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:mangrove_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MangroveFenceGate : Block // minecraft:mangrove_fence_gate
	{
		[StateBit] public bool InWallBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;

		public MangroveFenceGate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "in_wall_bit":
						InWallBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_fence_gate";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "in_wall_bit", Value = Convert.ToByte(InWallBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			return record;
		} // method
	} // class

	public partial class MangroveHangingSign : Block // minecraft:mangrove_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public MangroveHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class MangroveLeaves : Block // minecraft:mangrove_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public MangroveLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:mangrove_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class MangroveLog : Block // minecraft:mangrove_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public MangroveLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:mangrove_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class MangrovePlanks : Block // minecraft:mangrove_planks
	{

		public MangrovePlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:mangrove_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MangrovePressurePlate : Block // minecraft:mangrove_pressure_plate
	{
		[StateRange(0, 15)] public int RedstoneSignal { get; set; } = 0;

		public MangrovePressurePlate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "redstone_signal":
						RedstoneSignal = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_pressure_plate";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "redstone_signal", Value = RedstoneSignal});
			return record;
		} // method
	} // class

	public partial class MangrovePropagule : Block // minecraft:mangrove_propagule
	{
		[StateBit] public bool Hanging { get; set; } = false;
		[StateRange(0, 4)] public int PropaguleStage { get; set; } = 0;

		public MangrovePropagule() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "propagule_stage":
						PropaguleStage = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_propagule";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			record.States.Add(new BlockStateInt {Name = "propagule_stage", Value = PropaguleStage});
			return record;
		} // method
	} // class

	public partial class MangroveRoots : Block // minecraft:mangrove_roots
	{

		public MangroveRoots() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:mangrove_roots";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MangroveShelf : Block // minecraft:mangrove_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public MangroveShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class MangroveSlab : Block // minecraft:mangrove_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MangroveSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MangroveStairs : Block // minecraft:mangrove_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public MangroveStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class MangroveStandingSign : Block // minecraft:mangrove_standing_sign
	{
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;

		public MangroveStandingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_standing_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			return record;
		} // method
	} // class

	public partial class MangroveTrapdoor : Block // minecraft:mangrove_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public MangroveTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class MangroveWallSign : Block // minecraft:mangrove_wall_sign
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public MangroveWallSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mangrove_wall_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class MangroveWood : Block // minecraft:mangrove_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public MangroveWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:mangrove_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class MaterialReducer : Block // minecraft:material_reducer
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;

		public MaterialReducer() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:material_reducer";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			return record;
		} // method
	} // class

	public partial class MediumAmethystBud : Block // minecraft:medium_amethyst_bud
	{
		[StateEnum("down","up","north","south","west","east")]
		public string BlockFace { get; set; } = "down";

		public MediumAmethystBud() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:block_face":
						BlockFace = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:medium_amethyst_bud";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:block_face", Value = BlockFace});
			return record;
		} // method
	} // class

	public partial class MossBlock : Block // minecraft:moss_block
	{

		public MossBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:moss_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MossCarpet : Block // minecraft:moss_carpet
	{

		public MossCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:moss_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MossyCobblestoneDoubleSlab : Block // minecraft:mossy_cobblestone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MossyCobblestoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mossy_cobblestone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MossyCobblestoneSlab : Block // minecraft:mossy_cobblestone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MossyCobblestoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mossy_cobblestone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MossyCobblestoneWall : Block // minecraft:mossy_cobblestone_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public MossyCobblestoneWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mossy_cobblestone_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class MossyStoneBrickDoubleSlab : Block // minecraft:mossy_stone_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MossyStoneBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mossy_stone_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MossyStoneBrickSlab : Block // minecraft:mossy_stone_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MossyStoneBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mossy_stone_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MossyStoneBrickWall : Block // minecraft:mossy_stone_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public MossyStoneBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mossy_stone_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class MossyStoneBricks : Block // minecraft:mossy_stone_bricks
	{

		public MossyStoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:mossy_stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Mud : Block // minecraft:mud
	{

		public Mud() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:mud";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MudBrickDoubleSlab : Block // minecraft:mud_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MudBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mud_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MudBrickSlab : Block // minecraft:mud_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public MudBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mud_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class MudBrickStairs : Block // minecraft:mud_brick_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public MudBrickStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mud_brick_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class MudBrickWall : Block // minecraft:mud_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public MudBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mud_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class MudBricks : Block // minecraft:mud_bricks
	{

		public MudBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:mud_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class MuddyMangroveRoots : Block // minecraft:muddy_mangrove_roots
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public MuddyMangroveRoots() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:muddy_mangrove_roots";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class MushroomStem : Block // minecraft:mushroom_stem
	{
		[StateRange(0, 15)] public int HugeMushroomBits { get; set; } = 0;

		public MushroomStem() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "huge_mushroom_bits":
						HugeMushroomBits = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:mushroom_stem";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "huge_mushroom_bits", Value = HugeMushroomBits});
			return record;
		} // method
	} // class

	public partial class NetherBrickDoubleSlab : Block // minecraft:nether_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public NetherBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:nether_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class NetherBrickSlab : Block // minecraft:nether_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public NetherBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:nether_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class NetherBrickWall : Block // minecraft:nether_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public NetherBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:nether_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class NormalStoneDoubleSlab : Block // minecraft:normal_stone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public NormalStoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:normal_stone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class NormalStoneSlab : Block // minecraft:normal_stone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public NormalStoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:normal_stone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class OakDoubleSlab : Block // minecraft:oak_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public OakDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oak_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class OakFence : Block // minecraft:oak_fence
	{

		public OakFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oak_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OakHangingSign : Block // minecraft:oak_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public OakHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oak_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class OakLeaves : Block // minecraft:oak_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public OakLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:oak_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class OakLog : Block // minecraft:oak_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public OakLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:oak_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class OakPlanks : Block // minecraft:oak_planks
	{

		public OakPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oak_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OakSapling : Block // minecraft:oak_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public OakSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oak_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class OakShelf : Block // minecraft:oak_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public OakShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oak_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class OakSlab : Block // minecraft:oak_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public OakSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oak_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class OakWood : Block // minecraft:oak_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public OakWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:oak_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class OchreFroglight : Block // minecraft:ochre_froglight
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public OchreFroglight() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:ochre_froglight";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class OpenEyeblossom : Block // minecraft:open_eyeblossom
	{

		public OpenEyeblossom() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:open_eyeblossom";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeCandle : Block // minecraft:orange_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public OrangeCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:orange_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class OrangeCandleCake : Block // minecraft:orange_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public OrangeCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:orange_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class OrangeCarpet : Block // minecraft:orange_carpet
	{

		public OrangeCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeConcrete : Block // minecraft:orange_concrete
	{

		public OrangeConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeConcretePowder : Block // minecraft:orange_concrete_powder
	{

		public OrangeConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeShulkerBox : Block // minecraft:orange_shulker_box
	{

		public OrangeShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeStainedGlass : Block // minecraft:orange_stained_glass
	{

		public OrangeStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeStainedGlassPane : Block // minecraft:orange_stained_glass_pane
	{

		public OrangeStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeTerracotta : Block // minecraft:orange_terracotta
	{

		public OrangeTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeTulip : Block // minecraft:orange_tulip
	{

		public OrangeTulip() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_tulip";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OrangeWool : Block // minecraft:orange_wool
	{

		public OrangeWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:orange_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxeyeDaisy : Block // minecraft:oxeye_daisy
	{

		public OxeyeDaisy() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oxeye_daisy";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxidizedChiseledCopper : Block // minecraft:oxidized_chiseled_copper
	{

		public OxidizedChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oxidized_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxidizedCopper : Block // minecraft:oxidized_copper
	{

		public OxidizedCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oxidized_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxidizedCopperBars : Block // minecraft:oxidized_copper_bars
	{

		public OxidizedCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oxidized_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxidizedCopperBulb : Block // minecraft:oxidized_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public OxidizedCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class OxidizedCopperChain : Block // minecraft:oxidized_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public OxidizedCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:oxidized_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class OxidizedCopperChest : Block // minecraft:oxidized_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public OxidizedCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class OxidizedCopperDoor : Block // minecraft:oxidized_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public OxidizedCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:oxidized_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class OxidizedCopperGolemStatue : Block // minecraft:oxidized_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public OxidizedCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class OxidizedCopperGrate : Block // minecraft:oxidized_copper_grate
	{

		public OxidizedCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oxidized_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxidizedCopperLantern : Block // minecraft:oxidized_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public OxidizedCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class OxidizedCopperTrapdoor : Block // minecraft:oxidized_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public OxidizedCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class OxidizedCutCopper : Block // minecraft:oxidized_cut_copper
	{

		public OxidizedCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:oxidized_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class OxidizedCutCopperSlab : Block // minecraft:oxidized_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public OxidizedCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class OxidizedCutCopperStairs : Block // minecraft:oxidized_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public OxidizedCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class OxidizedDoubleCutCopperSlab : Block // minecraft:oxidized_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public OxidizedDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class OxidizedLightningRod : Block // minecraft:oxidized_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public OxidizedLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:oxidized_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class PackedMud : Block // minecraft:packed_mud
	{

		public PackedMud() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:packed_mud";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PaleHangingMoss : Block // minecraft:pale_hanging_moss
	{
		[StateBit] public bool Tip { get; set; } = false;

		public PaleHangingMoss() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "tip":
						Tip = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_hanging_moss";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "tip", Value = Convert.ToByte(Tip)});
			return record;
		} // method
	} // class

	public partial class PaleMossBlock : Block // minecraft:pale_moss_block
	{

		public PaleMossBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pale_moss_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PaleMossCarpet : Block // minecraft:pale_moss_carpet
	{
		[StateEnum("none","short","tall")]
		public string PaleMossCarpetSideEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string PaleMossCarpetSideNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string PaleMossCarpetSideSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string PaleMossCarpetSideWest { get; set; } = "none";
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public PaleMossCarpet() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "pale_moss_carpet_side_east":
						PaleMossCarpetSideEast = s.Value;
						break;
					case BlockStateString s when s.Name == "pale_moss_carpet_side_north":
						PaleMossCarpetSideNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "pale_moss_carpet_side_south":
						PaleMossCarpetSideSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "pale_moss_carpet_side_west":
						PaleMossCarpetSideWest = s.Value;
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
			record.Name = "minecraft:pale_moss_carpet";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pale_moss_carpet_side_east", Value = PaleMossCarpetSideEast});
			record.States.Add(new BlockStateString {Name = "pale_moss_carpet_side_north", Value = PaleMossCarpetSideNorth});
			record.States.Add(new BlockStateString {Name = "pale_moss_carpet_side_south", Value = PaleMossCarpetSideSouth});
			record.States.Add(new BlockStateString {Name = "pale_moss_carpet_side_west", Value = PaleMossCarpetSideWest});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class PaleOakButton : Block // minecraft:pale_oak_button
	{
		[StateBit] public bool ButtonPressedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public PaleOakButton() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "button_pressed_bit":
						ButtonPressedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_button";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "button_pressed_bit", Value = Convert.ToByte(ButtonPressedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class PaleOakDoor : Block // minecraft:pale_oak_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public PaleOakDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:pale_oak_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class PaleOakDoubleSlab : Block // minecraft:pale_oak_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PaleOakDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PaleOakFence : Block // minecraft:pale_oak_fence
	{

		public PaleOakFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pale_oak_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PaleOakFenceGate : Block // minecraft:pale_oak_fence_gate
	{
		[StateBit] public bool InWallBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;

		public PaleOakFenceGate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "in_wall_bit":
						InWallBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_fence_gate";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "in_wall_bit", Value = Convert.ToByte(InWallBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			return record;
		} // method
	} // class

	public partial class PaleOakHangingSign : Block // minecraft:pale_oak_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public PaleOakHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class PaleOakLeaves : Block // minecraft:pale_oak_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public PaleOakLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:pale_oak_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class PaleOakLog : Block // minecraft:pale_oak_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public PaleOakLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:pale_oak_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class PaleOakPlanks : Block // minecraft:pale_oak_planks
	{

		public PaleOakPlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pale_oak_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PaleOakPressurePlate : Block // minecraft:pale_oak_pressure_plate
	{
		[StateRange(0, 15)] public int RedstoneSignal { get; set; } = 0;

		public PaleOakPressurePlate() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "redstone_signal":
						RedstoneSignal = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_pressure_plate";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "redstone_signal", Value = RedstoneSignal});
			return record;
		} // method
	} // class

	public partial class PaleOakSapling : Block // minecraft:pale_oak_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public PaleOakSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class PaleOakShelf : Block // minecraft:pale_oak_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public PaleOakShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class PaleOakSlab : Block // minecraft:pale_oak_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PaleOakSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PaleOakStairs : Block // minecraft:pale_oak_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public PaleOakStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class PaleOakStandingSign : Block // minecraft:pale_oak_standing_sign
	{
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;

		public PaleOakStandingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_standing_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			return record;
		} // method
	} // class

	public partial class PaleOakTrapdoor : Block // minecraft:pale_oak_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public PaleOakTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class PaleOakWallSign : Block // minecraft:pale_oak_wall_sign
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public PaleOakWallSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pale_oak_wall_sign";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class PaleOakWood : Block // minecraft:pale_oak_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public PaleOakWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:pale_oak_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class PearlescentFroglight : Block // minecraft:pearlescent_froglight
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public PearlescentFroglight() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:pearlescent_froglight";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class Peony : Block // minecraft:peony
	{
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public Peony() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upper_block_bit":
						UpperBlockBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:peony";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class PetrifiedOakDoubleSlab : Block // minecraft:petrified_oak_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PetrifiedOakDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:petrified_oak_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PetrifiedOakSlab : Block // minecraft:petrified_oak_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PetrifiedOakSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:petrified_oak_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PiglinHead : Block // minecraft:piglin_head
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public PiglinHead() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:piglin_head";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class PinkCandle : Block // minecraft:pink_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public PinkCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pink_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class PinkCandleCake : Block // minecraft:pink_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public PinkCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pink_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class PinkCarpet : Block // minecraft:pink_carpet
	{

		public PinkCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkConcrete : Block // minecraft:pink_concrete
	{

		public PinkConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkConcretePowder : Block // minecraft:pink_concrete_powder
	{

		public PinkConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkPetals : Block // minecraft:pink_petals
	{
		[StateRange(0, 7)] public int Growth { get; set; } = 0;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public PinkPetals() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growth":
						Growth = s.Value;
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pink_petals";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growth", Value = Growth});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class PinkShulkerBox : Block // minecraft:pink_shulker_box
	{

		public PinkShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkStainedGlass : Block // minecraft:pink_stained_glass
	{

		public PinkStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkStainedGlassPane : Block // minecraft:pink_stained_glass_pane
	{

		public PinkStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkTerracotta : Block // minecraft:pink_terracotta
	{

		public PinkTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkTulip : Block // minecraft:pink_tulip
	{

		public PinkTulip() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_tulip";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PinkWool : Block // minecraft:pink_wool
	{

		public PinkWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:pink_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PitcherCrop : Block // minecraft:pitcher_crop
	{
		[StateRange(0, 7)] public int Growth { get; set; } = 0;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public PitcherCrop() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growth":
						Growth = s.Value;
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
			record.Name = "minecraft:pitcher_crop";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growth", Value = Growth});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class PitcherPlant : Block // minecraft:pitcher_plant
	{
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public PitcherPlant() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upper_block_bit":
						UpperBlockBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pitcher_plant";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class PlayerHead : Block // minecraft:player_head
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public PlayerHead() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:player_head";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class PointedDripstone : Block // minecraft:pointed_dripstone
	{
		[StateEnum("tip","frustum","middle","base","merge")]
		public string DripstoneThickness { get; set; } = "tip";
		[StateBit] public bool Hanging { get; set; } = false;

		public PointedDripstone() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "dripstone_thickness":
						DripstoneThickness = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:pointed_dripstone";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "dripstone_thickness", Value = DripstoneThickness});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class PolishedAndesite : Block // minecraft:polished_andesite
	{

		public PolishedAndesite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_andesite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedAndesiteDoubleSlab : Block // minecraft:polished_andesite_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedAndesiteDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_andesite_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedAndesiteSlab : Block // minecraft:polished_andesite_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedAndesiteSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_andesite_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedCinnabar : Block // minecraft:polished_cinnabar
	{

		public PolishedCinnabar() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_cinnabar";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedCinnabarDoubleSlab : Block // minecraft:polished_cinnabar_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedCinnabarDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_cinnabar_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedCinnabarSlab : Block // minecraft:polished_cinnabar_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedCinnabarSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_cinnabar_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedCinnabarStairs : Block // minecraft:polished_cinnabar_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public PolishedCinnabarStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_cinnabar_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class PolishedCinnabarWall : Block // minecraft:polished_cinnabar_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public PolishedCinnabarWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_cinnabar_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class PolishedDeepslate : Block // minecraft:polished_deepslate
	{

		public PolishedDeepslate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_deepslate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedDeepslateDoubleSlab : Block // minecraft:polished_deepslate_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedDeepslateDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_deepslate_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedDeepslateSlab : Block // minecraft:polished_deepslate_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedDeepslateSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_deepslate_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedDeepslateStairs : Block // minecraft:polished_deepslate_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public PolishedDeepslateStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_deepslate_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class PolishedDeepslateWall : Block // minecraft:polished_deepslate_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public PolishedDeepslateWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_deepslate_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class PolishedDiorite : Block // minecraft:polished_diorite
	{

		public PolishedDiorite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_diorite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedDioriteDoubleSlab : Block // minecraft:polished_diorite_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedDioriteDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_diorite_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedDioriteSlab : Block // minecraft:polished_diorite_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedDioriteSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_diorite_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedGranite : Block // minecraft:polished_granite
	{

		public PolishedGranite() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_granite";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedGraniteDoubleSlab : Block // minecraft:polished_granite_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedGraniteDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_granite_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedGraniteSlab : Block // minecraft:polished_granite_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedGraniteSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_granite_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedSulfur : Block // minecraft:polished_sulfur
	{

		public PolishedSulfur() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_sulfur";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedSulfurDoubleSlab : Block // minecraft:polished_sulfur_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedSulfurDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_sulfur_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedSulfurSlab : Block // minecraft:polished_sulfur_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedSulfurSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_sulfur_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedSulfurStairs : Block // minecraft:polished_sulfur_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public PolishedSulfurStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_sulfur_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class PolishedSulfurWall : Block // minecraft:polished_sulfur_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public PolishedSulfurWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_sulfur_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class PolishedTuff : Block // minecraft:polished_tuff
	{

		public PolishedTuff() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:polished_tuff";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PolishedTuffDoubleSlab : Block // minecraft:polished_tuff_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedTuffDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_tuff_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedTuffSlab : Block // minecraft:polished_tuff_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PolishedTuffSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_tuff_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PolishedTuffStairs : Block // minecraft:polished_tuff_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public PolishedTuffStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_tuff_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class PolishedTuffWall : Block // minecraft:polished_tuff_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public PolishedTuffWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:polished_tuff_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class Poppy : Block // minecraft:poppy
	{

		public Poppy() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:poppy";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PotentSulfur : Block // minecraft:potent_sulfur
	{
		[StateEnum("dry","wet","dormant","erupting","continuous")]
		public string PotentSulfurState { get; set; } = "dry";

		public PotentSulfur() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "potent_sulfur_state":
						PotentSulfurState = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:potent_sulfur";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "potent_sulfur_state", Value = PotentSulfurState});
			return record;
		} // method
	} // class

	public partial class PowderSnow : Block // minecraft:powder_snow
	{

		public PowderSnow() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:powder_snow";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PrismarineBrickDoubleSlab : Block // minecraft:prismarine_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PrismarineBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:prismarine_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PrismarineBrickSlab : Block // minecraft:prismarine_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PrismarineBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:prismarine_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PrismarineBricks : Block // minecraft:prismarine_bricks
	{

		public PrismarineBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:prismarine_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PrismarineDoubleSlab : Block // minecraft:prismarine_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PrismarineDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:prismarine_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PrismarineSlab : Block // minecraft:prismarine_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PrismarineSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:prismarine_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PrismarineWall : Block // minecraft:prismarine_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public PrismarineWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:prismarine_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class PurpleCandle : Block // minecraft:purple_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public PurpleCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:purple_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class PurpleCandleCake : Block // minecraft:purple_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public PurpleCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:purple_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class PurpleCarpet : Block // minecraft:purple_carpet
	{

		public PurpleCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleConcrete : Block // minecraft:purple_concrete
	{

		public PurpleConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleConcretePowder : Block // minecraft:purple_concrete_powder
	{

		public PurpleConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleShulkerBox : Block // minecraft:purple_shulker_box
	{

		public PurpleShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleStainedGlass : Block // minecraft:purple_stained_glass
	{

		public PurpleStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleStainedGlassPane : Block // minecraft:purple_stained_glass_pane
	{

		public PurpleStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleTerracotta : Block // minecraft:purple_terracotta
	{

		public PurpleTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpleWool : Block // minecraft:purple_wool
	{

		public PurpleWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:purple_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class PurpurDoubleSlab : Block // minecraft:purpur_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PurpurDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:purpur_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class PurpurPillar : Block // minecraft:purpur_pillar
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public PurpurPillar() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:purpur_pillar";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class PurpurSlab : Block // minecraft:purpur_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public PurpurSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:purpur_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class QuartzDoubleSlab : Block // minecraft:quartz_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public QuartzDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:quartz_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class QuartzPillar : Block // minecraft:quartz_pillar
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public QuartzPillar() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:quartz_pillar";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class QuartzSlab : Block // minecraft:quartz_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public QuartzSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:quartz_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class RawCopperBlock : Block // minecraft:raw_copper_block
	{

		public RawCopperBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:raw_copper_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RawGoldBlock : Block // minecraft:raw_gold_block
	{

		public RawGoldBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:raw_gold_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RawIronBlock : Block // minecraft:raw_iron_block
	{

		public RawIronBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:raw_iron_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedCandle : Block // minecraft:red_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public RedCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class RedCandleCake : Block // minecraft:red_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public RedCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class RedCarpet : Block // minecraft:red_carpet
	{

		public RedCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedConcrete : Block // minecraft:red_concrete
	{

		public RedConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedConcretePowder : Block // minecraft:red_concrete_powder
	{

		public RedConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedNetherBrickDoubleSlab : Block // minecraft:red_nether_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public RedNetherBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_nether_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class RedNetherBrickSlab : Block // minecraft:red_nether_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public RedNetherBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_nether_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class RedNetherBrickWall : Block // minecraft:red_nether_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public RedNetherBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_nether_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class RedSand : Block // minecraft:red_sand
	{

		public RedSand() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_sand";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedSandstoneDoubleSlab : Block // minecraft:red_sandstone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public RedSandstoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_sandstone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class RedSandstoneSlab : Block // minecraft:red_sandstone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public RedSandstoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_sandstone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class RedSandstoneWall : Block // minecraft:red_sandstone_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public RedSandstoneWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:red_sandstone_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class RedShulkerBox : Block // minecraft:red_shulker_box
	{

		public RedShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedStainedGlass : Block // minecraft:red_stained_glass
	{

		public RedStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedStainedGlassPane : Block // minecraft:red_stained_glass_pane
	{

		public RedStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedTerracotta : Block // minecraft:red_terracotta
	{

		public RedTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedTulip : Block // minecraft:red_tulip
	{

		public RedTulip() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_tulip";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class RedWool : Block // minecraft:red_wool
	{

		public RedWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:red_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ReinforcedDeepslate : Block // minecraft:reinforced_deepslate
	{

		public ReinforcedDeepslate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:reinforced_deepslate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ResinBlock : Block // minecraft:resin_block
	{

		public ResinBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:resin_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ResinBrickDoubleSlab : Block // minecraft:resin_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public ResinBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:resin_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class ResinBrickSlab : Block // minecraft:resin_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public ResinBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:resin_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class ResinBrickStairs : Block // minecraft:resin_brick_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public ResinBrickStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:resin_brick_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class ResinBrickWall : Block // minecraft:resin_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public ResinBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:resin_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class ResinBricks : Block // minecraft:resin_bricks
	{

		public ResinBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:resin_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ResinClump : Block // minecraft:resin_clump
	{
		[StateRange(0, 63)] public int MultiFaceDirectionBits { get; set; } = 0;

		public ResinClump() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "multi_face_direction_bits":
						MultiFaceDirectionBits = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:resin_clump";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "multi_face_direction_bits", Value = MultiFaceDirectionBits});
			return record;
		} // method
	} // class

	public partial class RoseBush : Block // minecraft:rose_bush
	{
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public RoseBush() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upper_block_bit":
						UpperBlockBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:rose_bush";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class SandstoneDoubleSlab : Block // minecraft:sandstone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SandstoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sandstone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SandstoneSlab : Block // minecraft:sandstone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SandstoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sandstone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SandstoneWall : Block // minecraft:sandstone_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public SandstoneWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sandstone_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class Sculk : Block // minecraft:sculk
	{

		public Sculk() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:sculk";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SculkCatalyst : Block // minecraft:sculk_catalyst
	{
		[StateBit] public bool Bloom { get; set; } = false;

		public SculkCatalyst() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "bloom":
						Bloom = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sculk_catalyst";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "bloom", Value = Convert.ToByte(Bloom)});
			return record;
		} // method
	} // class

	public partial class SculkSensor : Block // minecraft:sculk_sensor
	{
		[StateRange(0, 2)] public int SculkSensorPhase { get; set; } = 0;

		public SculkSensor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "sculk_sensor_phase":
						SculkSensorPhase = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sculk_sensor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "sculk_sensor_phase", Value = SculkSensorPhase});
			return record;
		} // method
	} // class

	public partial class SculkShrieker : Block // minecraft:sculk_shrieker
	{
		[StateBit] public bool Active { get; set; } = false;
		[StateBit] public bool CanSummon { get; set; } = false;

		public SculkShrieker() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "active":
						Active = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "can_summon":
						CanSummon = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sculk_shrieker";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "active", Value = Convert.ToByte(Active)});
			record.States.Add(new BlockStateByte {Name = "can_summon", Value = Convert.ToByte(CanSummon)});
			return record;
		} // method
	} // class

	public partial class SculkVein : Block // minecraft:sculk_vein
	{
		[StateRange(0, 63)] public int MultiFaceDirectionBits { get; set; } = 0;

		public SculkVein() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "multi_face_direction_bits":
						MultiFaceDirectionBits = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sculk_vein";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "multi_face_direction_bits", Value = MultiFaceDirectionBits});
			return record;
		} // method
	} // class

	public partial class ShortDryGrass : Block // minecraft:short_dry_grass
	{

		public ShortDryGrass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:short_dry_grass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ShortGrass : Block // minecraft:short_grass
	{

		public ShortGrass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:short_grass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SkeletonSkull : Block // minecraft:skeleton_skull
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public SkeletonSkull() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:skeleton_skull";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class SmallAmethystBud : Block // minecraft:small_amethyst_bud
	{
		[StateEnum("down","up","north","south","west","east")]
		public string BlockFace { get; set; } = "down";

		public SmallAmethystBud() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:block_face":
						BlockFace = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:small_amethyst_bud";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:block_face", Value = BlockFace});
			return record;
		} // method
	} // class

	public partial class SmallDripleafBlock : Block // minecraft:small_dripleaf_block
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public SmallDripleafBlock() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
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
			record.Name = "minecraft:small_dripleaf_block";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class SmoothBasalt : Block // minecraft:smooth_basalt
	{

		public SmoothBasalt() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:smooth_basalt";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SmoothQuartz : Block // minecraft:smooth_quartz
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public SmoothQuartz() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:smooth_quartz";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class SmoothQuartzDoubleSlab : Block // minecraft:smooth_quartz_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothQuartzDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_quartz_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothQuartzSlab : Block // minecraft:smooth_quartz_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothQuartzSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_quartz_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothRedSandstone : Block // minecraft:smooth_red_sandstone
	{

		public SmoothRedSandstone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:smooth_red_sandstone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SmoothRedSandstoneDoubleSlab : Block // minecraft:smooth_red_sandstone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothRedSandstoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_red_sandstone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothRedSandstoneSlab : Block // minecraft:smooth_red_sandstone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothRedSandstoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_red_sandstone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothSandstone : Block // minecraft:smooth_sandstone
	{

		public SmoothSandstone() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:smooth_sandstone";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SmoothSandstoneDoubleSlab : Block // minecraft:smooth_sandstone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothSandstoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_sandstone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothSandstoneSlab : Block // minecraft:smooth_sandstone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothSandstoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_sandstone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothStoneDoubleSlab : Block // minecraft:smooth_stone_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothStoneDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_stone_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SmoothStoneSlab : Block // minecraft:smooth_stone_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SmoothStoneSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:smooth_stone_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SnifferEgg : Block // minecraft:sniffer_egg
	{
		[StateEnum("no_cracks","cracked","max_cracked")]
		public string CrackedState { get; set; } = "no_cracks";

		public SnifferEgg() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "cracked_state":
						CrackedState = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sniffer_egg";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "cracked_state", Value = CrackedState});
			return record;
		} // method
	} // class

	public partial class SporeBlossom : Block // minecraft:spore_blossom
	{

		public SporeBlossom() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:spore_blossom";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SpruceDoubleSlab : Block // minecraft:spruce_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SpruceDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:spruce_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SpruceFence : Block // minecraft:spruce_fence
	{

		public SpruceFence() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:spruce_fence";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SpruceHangingSign : Block // minecraft:spruce_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public SpruceHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:spruce_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class SpruceLeaves : Block // minecraft:spruce_leaves
	{
		[StateBit] public bool PersistentBit { get; set; } = false;
		[StateBit] public bool UpdateBit { get; set; } = false;

		public SpruceLeaves() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
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
			record.Name = "minecraft:spruce_leaves";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "persistent_bit", Value = Convert.ToByte(PersistentBit)});
			record.States.Add(new BlockStateByte {Name = "update_bit", Value = Convert.ToByte(UpdateBit)});
			return record;
		} // method
	} // class

	public partial class SpruceLog : Block // minecraft:spruce_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public SpruceLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:spruce_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class SprucePlanks : Block // minecraft:spruce_planks
	{

		public SprucePlanks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:spruce_planks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SpruceSapling : Block // minecraft:spruce_sapling
	{
		[StateBit] public bool AgeBit { get; set; } = false;

		public SpruceSapling() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "age_bit":
						AgeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:spruce_sapling";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "age_bit", Value = Convert.ToByte(AgeBit)});
			return record;
		} // method
	} // class

	public partial class SpruceShelf : Block // minecraft:spruce_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public SpruceShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:spruce_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class SpruceSlab : Block // minecraft:spruce_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SpruceSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:spruce_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SpruceWood : Block // minecraft:spruce_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public SpruceWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:spruce_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StoneBrickDoubleSlab : Block // minecraft:stone_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public StoneBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:stone_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class StoneBrickSlab : Block // minecraft:stone_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public StoneBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:stone_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class StoneBrickWall : Block // minecraft:stone_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public StoneBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:stone_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class StoneBricks : Block // minecraft:stone_bricks
	{

		public StoneBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:stone_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class StrippedAcaciaWood : Block // minecraft:stripped_acacia_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedAcaciaWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_acacia_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedBambooBlock : Block // minecraft:stripped_bamboo_block
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedBambooBlock() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_bamboo_block";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedBirchWood : Block // minecraft:stripped_birch_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedBirchWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_birch_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedCherryLog : Block // minecraft:stripped_cherry_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedCherryLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_cherry_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedCherryWood : Block // minecraft:stripped_cherry_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedCherryWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_cherry_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedDarkOakWood : Block // minecraft:stripped_dark_oak_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedDarkOakWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_dark_oak_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedJungleWood : Block // minecraft:stripped_jungle_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedJungleWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_jungle_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedMangroveLog : Block // minecraft:stripped_mangrove_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedMangroveLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_mangrove_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedMangroveWood : Block // minecraft:stripped_mangrove_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedMangroveWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_mangrove_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedOakWood : Block // minecraft:stripped_oak_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedOakWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_oak_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedPaleOakLog : Block // minecraft:stripped_pale_oak_log
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedPaleOakLog() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_pale_oak_log";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedPaleOakWood : Block // minecraft:stripped_pale_oak_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedPaleOakWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_pale_oak_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class StrippedSpruceWood : Block // minecraft:stripped_spruce_wood
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public StrippedSpruceWood() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:stripped_spruce_wood";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class Sulfur : Block // minecraft:sulfur
	{

		public Sulfur() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:sulfur";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SulfurBrickDoubleSlab : Block // minecraft:sulfur_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SulfurBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SulfurBrickSlab : Block // minecraft:sulfur_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SulfurBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SulfurBrickStairs : Block // minecraft:sulfur_brick_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public SulfurBrickStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_brick_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class SulfurBrickWall : Block // minecraft:sulfur_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public SulfurBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class SulfurBricks : Block // minecraft:sulfur_bricks
	{

		public SulfurBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:sulfur_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class SulfurDoubleSlab : Block // minecraft:sulfur_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SulfurDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SulfurSlab : Block // minecraft:sulfur_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public SulfurSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class SulfurSpike : Block // minecraft:sulfur_spike
	{
		[StateEnum("tip","frustum","middle","base","merge")]
		public string DripstoneThickness { get; set; } = "tip";
		[StateBit] public bool Hanging { get; set; } = false;

		public SulfurSpike() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "dripstone_thickness":
						DripstoneThickness = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_spike";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "dripstone_thickness", Value = DripstoneThickness});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class SulfurStairs : Block // minecraft:sulfur_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public SulfurStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class SulfurWall : Block // minecraft:sulfur_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public SulfurWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sulfur_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class Sunflower : Block // minecraft:sunflower
	{
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public Sunflower() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upper_block_bit":
						UpperBlockBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:sunflower";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class SuspiciousGravel : Block // minecraft:suspicious_gravel
	{
		[StateRange(0, 3)] public int BrushedProgress { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public SuspiciousGravel() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "brushed_progress":
						BrushedProgress = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:suspicious_gravel";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "brushed_progress", Value = BrushedProgress});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class SuspiciousSand : Block // minecraft:suspicious_sand
	{
		[StateRange(0, 3)] public int BrushedProgress { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public SuspiciousSand() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "brushed_progress":
						BrushedProgress = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:suspicious_sand";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "brushed_progress", Value = BrushedProgress});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class TallDryGrass : Block // minecraft:tall_dry_grass
	{

		public TallDryGrass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:tall_dry_grass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class TintedGlass : Block // minecraft:tinted_glass
	{

		public TintedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:tinted_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Torchflower : Block // minecraft:torchflower
	{

		public Torchflower() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:torchflower";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class TorchflowerCrop : Block // minecraft:torchflower_crop
	{
		[StateRange(0, 7)] public int Growth { get; set; } = 0;

		public TorchflowerCrop() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growth":
						Growth = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:torchflower_crop";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growth", Value = Growth});
			return record;
		} // method
	} // class

	public partial class TrialSpawner : Block // minecraft:trial_spawner
	{
		[StateBit] public bool Ominous { get; set; } = false;
		[StateRange(0, 5)] public int TrialSpawnerState { get; set; } = 0;

		public TrialSpawner() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "ominous":
						Ominous = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "trial_spawner_state":
						TrialSpawnerState = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:trial_spawner";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "ominous", Value = Convert.ToByte(Ominous)});
			record.States.Add(new BlockStateInt {Name = "trial_spawner_state", Value = TrialSpawnerState});
			return record;
		} // method
	} // class

	public partial class TubeCoral : Block // minecraft:tube_coral
	{

		public TubeCoral() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:tube_coral";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class TubeCoralBlock : Block // minecraft:tube_coral_block
	{

		public TubeCoralBlock() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:tube_coral_block";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class TubeCoralFan : Block // minecraft:tube_coral_fan
	{
		[StateRange(0, 1)] public int CoralFanDirection { get; set; } = 0;

		public TubeCoralFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_fan_direction":
						CoralFanDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tube_coral_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_fan_direction", Value = CoralFanDirection});
			return record;
		} // method
	} // class

	public partial class TubeCoralWallFan : Block // minecraft:tube_coral_wall_fan
	{
		[StateRange(0, 3)] public int CoralDirection { get; set; } = 0;

		public TubeCoralWallFan() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "coral_direction":
						CoralDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tube_coral_wall_fan";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "coral_direction", Value = CoralDirection});
			return record;
		} // method
	} // class

	public partial class Tuff : Block // minecraft:tuff
	{

		public Tuff() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:tuff";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class TuffBrickDoubleSlab : Block // minecraft:tuff_brick_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public TuffBrickDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_brick_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class TuffBrickSlab : Block // minecraft:tuff_brick_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public TuffBrickSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_brick_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class TuffBrickStairs : Block // minecraft:tuff_brick_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public TuffBrickStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_brick_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class TuffBrickWall : Block // minecraft:tuff_brick_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public TuffBrickWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_brick_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class TuffBricks : Block // minecraft:tuff_bricks
	{

		public TuffBricks() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:tuff_bricks";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class TuffDoubleSlab : Block // minecraft:tuff_double_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public TuffDoubleSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_double_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class TuffSlab : Block // minecraft:tuff_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public TuffSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class TuffStairs : Block // minecraft:tuff_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public TuffStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class TuffWall : Block // minecraft:tuff_wall
	{
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeEast { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeNorth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeSouth { get; set; } = "none";
		[StateEnum("none","short","tall")]
		public string WallConnectionTypeWest { get; set; } = "none";
		[StateBit] public bool WallPostBit { get; set; } = false;

		public TuffWall() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "wall_connection_type_east":
						WallConnectionTypeEast = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_north":
						WallConnectionTypeNorth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_south":
						WallConnectionTypeSouth = s.Value;
						break;
					case BlockStateString s when s.Name == "wall_connection_type_west":
						WallConnectionTypeWest = s.Value;
						break;
					case BlockStateByte s when s.Name == "wall_post_bit":
						WallPostBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:tuff_wall";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "wall_connection_type_east", Value = WallConnectionTypeEast});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_north", Value = WallConnectionTypeNorth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_south", Value = WallConnectionTypeSouth});
			record.States.Add(new BlockStateString {Name = "wall_connection_type_west", Value = WallConnectionTypeWest});
			record.States.Add(new BlockStateByte {Name = "wall_post_bit", Value = Convert.ToByte(WallPostBit)});
			return record;
		} // method
	} // class

	public partial class UnderwaterTnt : Block // minecraft:underwater_tnt
	{
		[StateBit] public bool ExplodeBit { get; set; } = false;

		public UnderwaterTnt() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "explode_bit":
						ExplodeBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:underwater_tnt";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "explode_bit", Value = Convert.ToByte(ExplodeBit)});
			return record;
		} // method
	} // class

	public partial class Unknown : Block // minecraft:unknown
	{

		public Unknown() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:unknown";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Vault : Block // minecraft:vault
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool Ominous { get; set; } = false;
		[StateEnum("inactive","active","unlocking","ejecting")]
		public string VaultState { get; set; } = "inactive";

		public Vault() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "ominous":
						Ominous = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "vault_state":
						VaultState = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:vault";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "ominous", Value = Convert.ToByte(Ominous)});
			record.States.Add(new BlockStateString {Name = "vault_state", Value = VaultState});
			return record;
		} // method
	} // class

	public partial class VerdantFroglight : Block // minecraft:verdant_froglight
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public VerdantFroglight() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:verdant_froglight";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class WarpedHangingSign : Block // minecraft:warped_hanging_sign
	{
		[StateBit] public bool AttachedBit { get; set; } = false;
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateRange(0, 15)] public int GroundSignDirection { get; set; } = 0;
		[StateBit] public bool Hanging { get; set; } = false;

		public WarpedHangingSign() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "attached_bit":
						AttachedBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateInt s when s.Name == "ground_sign_direction":
						GroundSignDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:warped_hanging_sign";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "attached_bit", Value = Convert.ToByte(AttachedBit)});
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateInt {Name = "ground_sign_direction", Value = GroundSignDirection});
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class WarpedShelf : Block // minecraft:warped_shelf
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool PoweredBit { get; set; } = false;
		[StateRange(0, 3)] public int PoweredShelfType { get; set; } = 0;

		public WarpedShelf() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "powered_shelf_type":
						PoweredShelfType = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:warped_shelf";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			record.States.Add(new BlockStateInt {Name = "powered_shelf_type", Value = PoweredShelfType});
			return record;
		} // method
	} // class

	public partial class WaxedChiseledCopper : Block // minecraft:waxed_chiseled_copper
	{

		public WaxedChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedCopper : Block // minecraft:waxed_copper
	{

		public WaxedCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedCopperBars : Block // minecraft:waxed_copper_bars
	{

		public WaxedCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedCopperBulb : Block // minecraft:waxed_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedCopperChain : Block // minecraft:waxed_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public WaxedCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:waxed_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class WaxedCopperChest : Block // minecraft:waxed_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedCopperDoor : Block // minecraft:waxed_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public WaxedCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:waxed_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class WaxedCopperGolemStatue : Block // minecraft:waxed_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedCopperGrate : Block // minecraft:waxed_copper_grate
	{

		public WaxedCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedCopperLantern : Block // minecraft:waxed_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public WaxedCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class WaxedCopperTrapdoor : Block // minecraft:waxed_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public WaxedCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class WaxedCutCopper : Block // minecraft:waxed_cut_copper
	{

		public WaxedCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedCutCopperSlab : Block // minecraft:waxed_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedCutCopperStairs : Block // minecraft:waxed_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public WaxedCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class WaxedDoubleCutCopperSlab : Block // minecraft:waxed_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedExposedChiseledCopper : Block // minecraft:waxed_exposed_chiseled_copper
	{

		public WaxedExposedChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_exposed_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopper : Block // minecraft:waxed_exposed_copper
	{

		public WaxedExposedCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_exposed_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperBars : Block // minecraft:waxed_exposed_copper_bars
	{

		public WaxedExposedCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_exposed_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperBulb : Block // minecraft:waxed_exposed_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedExposedCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperChain : Block // minecraft:waxed_exposed_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public WaxedExposedCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:waxed_exposed_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperChest : Block // minecraft:waxed_exposed_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedExposedCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperDoor : Block // minecraft:waxed_exposed_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public WaxedExposedCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:waxed_exposed_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperGolemStatue : Block // minecraft:waxed_exposed_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedExposedCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperGrate : Block // minecraft:waxed_exposed_copper_grate
	{

		public WaxedExposedCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_exposed_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperLantern : Block // minecraft:waxed_exposed_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public WaxedExposedCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCopperTrapdoor : Block // minecraft:waxed_exposed_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public WaxedExposedCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCutCopper : Block // minecraft:waxed_exposed_cut_copper
	{

		public WaxedExposedCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_exposed_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedExposedCutCopperSlab : Block // minecraft:waxed_exposed_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedExposedCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedExposedCutCopperStairs : Block // minecraft:waxed_exposed_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public WaxedExposedCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class WaxedExposedDoubleCutCopperSlab : Block // minecraft:waxed_exposed_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedExposedDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedExposedLightningRod : Block // minecraft:waxed_exposed_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedExposedLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_exposed_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedLightningRod : Block // minecraft:waxed_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedChiseledCopper : Block // minecraft:waxed_oxidized_chiseled_copper
	{

		public WaxedOxidizedChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_oxidized_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopper : Block // minecraft:waxed_oxidized_copper
	{

		public WaxedOxidizedCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_oxidized_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperBars : Block // minecraft:waxed_oxidized_copper_bars
	{

		public WaxedOxidizedCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_oxidized_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperBulb : Block // minecraft:waxed_oxidized_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedOxidizedCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperChain : Block // minecraft:waxed_oxidized_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public WaxedOxidizedCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:waxed_oxidized_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperChest : Block // minecraft:waxed_oxidized_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedOxidizedCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperDoor : Block // minecraft:waxed_oxidized_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public WaxedOxidizedCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:waxed_oxidized_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperGolemStatue : Block // minecraft:waxed_oxidized_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedOxidizedCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperGrate : Block // minecraft:waxed_oxidized_copper_grate
	{

		public WaxedOxidizedCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_oxidized_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperLantern : Block // minecraft:waxed_oxidized_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public WaxedOxidizedCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCopperTrapdoor : Block // minecraft:waxed_oxidized_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public WaxedOxidizedCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCutCopper : Block // minecraft:waxed_oxidized_cut_copper
	{

		public WaxedOxidizedCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_oxidized_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCutCopperSlab : Block // minecraft:waxed_oxidized_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedOxidizedCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedCutCopperStairs : Block // minecraft:waxed_oxidized_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public WaxedOxidizedCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedDoubleCutCopperSlab : Block // minecraft:waxed_oxidized_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedOxidizedDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedOxidizedLightningRod : Block // minecraft:waxed_oxidized_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedOxidizedLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_oxidized_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredChiseledCopper : Block // minecraft:waxed_weathered_chiseled_copper
	{

		public WaxedWeatheredChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_weathered_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopper : Block // minecraft:waxed_weathered_copper
	{

		public WaxedWeatheredCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_weathered_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperBars : Block // minecraft:waxed_weathered_copper_bars
	{

		public WaxedWeatheredCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_weathered_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperBulb : Block // minecraft:waxed_weathered_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedWeatheredCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperChain : Block // minecraft:waxed_weathered_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public WaxedWeatheredCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:waxed_weathered_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperChest : Block // minecraft:waxed_weathered_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedWeatheredCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperDoor : Block // minecraft:waxed_weathered_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public WaxedWeatheredCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:waxed_weathered_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperGolemStatue : Block // minecraft:waxed_weathered_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WaxedWeatheredCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperGrate : Block // minecraft:waxed_weathered_copper_grate
	{

		public WaxedWeatheredCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_weathered_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperLantern : Block // minecraft:waxed_weathered_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public WaxedWeatheredCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCopperTrapdoor : Block // minecraft:waxed_weathered_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public WaxedWeatheredCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCutCopper : Block // minecraft:waxed_weathered_cut_copper
	{

		public WaxedWeatheredCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:waxed_weathered_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCutCopperSlab : Block // minecraft:waxed_weathered_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedWeatheredCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredCutCopperStairs : Block // minecraft:waxed_weathered_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public WaxedWeatheredCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredDoubleCutCopperSlab : Block // minecraft:waxed_weathered_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WaxedWeatheredDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WaxedWeatheredLightningRod : Block // minecraft:waxed_weathered_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WaxedWeatheredLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:waxed_weathered_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WeatheredChiseledCopper : Block // minecraft:weathered_chiseled_copper
	{

		public WeatheredChiseledCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:weathered_chiseled_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WeatheredCopper : Block // minecraft:weathered_copper
	{

		public WeatheredCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:weathered_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WeatheredCopperBars : Block // minecraft:weathered_copper_bars
	{

		public WeatheredCopperBars() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:weathered_copper_bars";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WeatheredCopperBulb : Block // minecraft:weathered_copper_bulb
	{
		[StateBit] public bool Lit { get; set; } = false;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WeatheredCopperBulb() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_copper_bulb";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WeatheredCopperChain : Block // minecraft:weathered_copper_chain
	{
		[StateEnum("y","x","z")]
		public string PillarAxis { get; set; } = "y";

		public WeatheredCopperChain() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
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
			record.Name = "minecraft:weathered_copper_chain";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "pillar_axis", Value = PillarAxis});
			return record;
		} // method
	} // class

	public partial class WeatheredCopperChest : Block // minecraft:weathered_copper_chest
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WeatheredCopperChest() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_copper_chest";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WeatheredCopperDoor : Block // minecraft:weathered_copper_door
	{
		[StateBit] public bool DoorHingeBit { get; set; } = false;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpperBlockBit { get; set; } = false;

		public WeatheredCopperDoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "door_hinge_bit":
						DoorHingeBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
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
			record.Name = "minecraft:weathered_copper_door";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "door_hinge_bit", Value = Convert.ToByte(DoorHingeBit)});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upper_block_bit", Value = Convert.ToByte(UpperBlockBit)});
			return record;
		} // method
	} // class

	public partial class WeatheredCopperGolemStatue : Block // minecraft:weathered_copper_golem_statue
	{
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public WeatheredCopperGolemStatue() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_copper_golem_statue";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WeatheredCopperGrate : Block // minecraft:weathered_copper_grate
	{

		public WeatheredCopperGrate() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:weathered_copper_grate";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WeatheredCopperLantern : Block // minecraft:weathered_copper_lantern
	{
		[StateBit] public bool Hanging { get; set; } = false;

		public WeatheredCopperLantern() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "hanging":
						Hanging = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_copper_lantern";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "hanging", Value = Convert.ToByte(Hanging)});
			return record;
		} // method
	} // class

	public partial class WeatheredCopperTrapdoor : Block // minecraft:weathered_copper_trapdoor
	{
		[StateRange(0, 3)] public int Direction { get; set; } = 0;
		[StateBit] public bool OpenBit { get; set; } = false;
		[StateBit] public bool UpsideDownBit { get; set; } = false;

		public WeatheredCopperTrapdoor() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "direction":
						Direction = s.Value;
						break;
					case BlockStateByte s when s.Name == "open_bit":
						OpenBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_copper_trapdoor";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "direction", Value = Direction});
			record.States.Add(new BlockStateByte {Name = "open_bit", Value = Convert.ToByte(OpenBit)});
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			return record;
		} // method
	} // class

	public partial class WeatheredCutCopper : Block // minecraft:weathered_cut_copper
	{

		public WeatheredCutCopper() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:weathered_cut_copper";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WeatheredCutCopperSlab : Block // minecraft:weathered_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WeatheredCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WeatheredCutCopperStairs : Block // minecraft:weathered_cut_copper_stairs
	{
		[StateBit] public bool UpsideDownBit { get; set; } = false;
		[StateRange(0, 3)] public int WeirdoDirection { get; set; } = 0;

		public WeatheredCutCopperStairs() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "upside_down_bit":
						UpsideDownBit = Convert.ToBoolean(s.Value);
						break;
					case BlockStateInt s when s.Name == "weirdo_direction":
						WeirdoDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_cut_copper_stairs";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "upside_down_bit", Value = Convert.ToByte(UpsideDownBit)});
			record.States.Add(new BlockStateInt {Name = "weirdo_direction", Value = WeirdoDirection});
			return record;
		} // method
	} // class

	public partial class WeatheredDoubleCutCopperSlab : Block // minecraft:weathered_double_cut_copper_slab
	{
		[StateEnum("bottom","top")]
		public string VerticalHalf { get; set; } = "bottom";

		public WeatheredDoubleCutCopperSlab() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateString s when s.Name == "minecraft:vertical_half":
						VerticalHalf = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_double_cut_copper_slab";
			record.Id = 0;
			record.States.Add(new BlockStateString {Name = "minecraft:vertical_half", Value = VerticalHalf});
			return record;
		} // method
	} // class

	public partial class WeatheredLightningRod : Block // minecraft:weathered_lightning_rod
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;
		[StateBit] public bool PoweredBit { get; set; } = false;

		public WeatheredLightningRod() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
					case BlockStateByte s when s.Name == "powered_bit":
						PoweredBit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:weathered_lightning_rod";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			record.States.Add(new BlockStateByte {Name = "powered_bit", Value = Convert.ToByte(PoweredBit)});
			return record;
		} // method
	} // class

	public partial class WetSponge : Block // minecraft:wet_sponge
	{

		public WetSponge() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:wet_sponge";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteCandle : Block // minecraft:white_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public WhiteCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:white_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class WhiteCandleCake : Block // minecraft:white_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public WhiteCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:white_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class WhiteCarpet : Block // minecraft:white_carpet
	{

		public WhiteCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteConcrete : Block // minecraft:white_concrete
	{

		public WhiteConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteConcretePowder : Block // minecraft:white_concrete_powder
	{

		public WhiteConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteShulkerBox : Block // minecraft:white_shulker_box
	{

		public WhiteShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteStainedGlass : Block // minecraft:white_stained_glass
	{

		public WhiteStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteStainedGlassPane : Block // minecraft:white_stained_glass_pane
	{

		public WhiteStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteTerracotta : Block // minecraft:white_terracotta
	{

		public WhiteTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteTulip : Block // minecraft:white_tulip
	{

		public WhiteTulip() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_tulip";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class WhiteWool : Block // minecraft:white_wool
	{

		public WhiteWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:white_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class Wildflowers : Block // minecraft:wildflowers
	{
		[StateRange(0, 7)] public int Growth { get; set; } = 0;
		[StateEnum("south","west","north","east")]
		public string CardinalDirection { get; set; } = "south";

		public Wildflowers() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "growth":
						Growth = s.Value;
						break;
					case BlockStateString s when s.Name == "minecraft:cardinal_direction":
						CardinalDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:wildflowers";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "growth", Value = Growth});
			record.States.Add(new BlockStateString {Name = "minecraft:cardinal_direction", Value = CardinalDirection});
			return record;
		} // method
	} // class

	public partial class WitherSkeletonSkull : Block // minecraft:wither_skeleton_skull
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public WitherSkeletonSkull() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:wither_skeleton_skull";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class

	public partial class YellowCandle : Block // minecraft:yellow_candle
	{
		[StateRange(0, 3)] public int Candles { get; set; } = 0;
		[StateBit] public bool Lit { get; set; } = false;

		public YellowCandle() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "candles":
						Candles = s.Value;
						break;
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:yellow_candle";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "candles", Value = Candles});
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class YellowCandleCake : Block // minecraft:yellow_candle_cake
	{
		[StateBit] public bool Lit { get; set; } = false;

		public YellowCandleCake() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateByte s when s.Name == "lit":
						Lit = Convert.ToBoolean(s.Value);
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:yellow_candle_cake";
			record.Id = 0;
			record.States.Add(new BlockStateByte {Name = "lit", Value = Convert.ToByte(Lit)});
			return record;
		} // method
	} // class

	public partial class YellowCarpet : Block // minecraft:yellow_carpet
	{

		public YellowCarpet() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_carpet";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowConcrete : Block // minecraft:yellow_concrete
	{

		public YellowConcrete() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_concrete";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowConcretePowder : Block // minecraft:yellow_concrete_powder
	{

		public YellowConcretePowder() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_concrete_powder";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowShulkerBox : Block // minecraft:yellow_shulker_box
	{

		public YellowShulkerBox() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_shulker_box";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowStainedGlass : Block // minecraft:yellow_stained_glass
	{

		public YellowStainedGlass() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_stained_glass";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowStainedGlassPane : Block // minecraft:yellow_stained_glass_pane
	{

		public YellowStainedGlassPane() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_stained_glass_pane";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowTerracotta : Block // minecraft:yellow_terracotta
	{

		public YellowTerracotta() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_terracotta";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class YellowWool : Block // minecraft:yellow_wool
	{

		public YellowWool() : base(0)
		{
			IsGenerated = true;
		}

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
			record.Name = "minecraft:yellow_wool";
			record.Id = 0;
			return record;
		} // method
	} // class

	public partial class ZombieHead : Block // minecraft:zombie_head
	{
		[StateRange(0, 5)] public int FacingDirection { get; set; } = 0;

		public ZombieHead() : base(0)
		{
			IsGenerated = true;
		}

		public override void SetState(List<IBlockState> states)
		{
			foreach (var state in states)
			{
				switch(state)
				{
					case BlockStateInt s when s.Name == "facing_direction":
						FacingDirection = s.Value;
						break;
				} // switch
			} // foreach
		} // method

		public override BlockStateContainer GetState()
		{
			var record = new BlockStateContainer();
			record.Name = "minecraft:zombie_head";
			record.Id = 0;
			record.States.Add(new BlockStateInt {Name = "facing_direction", Value = FacingDirection});
			return record;
		} // method
	} // class
}
