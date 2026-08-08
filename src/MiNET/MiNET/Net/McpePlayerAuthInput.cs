using System.Collections.Generic;
using System.Numerics;
using MiNET.Utils;

namespace MiNET.Net;

/// <summary>
///		Protocol 1001 (1.26) player input packet. Wire layout verified field-by-field against
///		minecraft-data 1001 packet_player_auth_input and PMMP PlayerAuthInputPacket::decodePayload.
///		Where the two disagree, PMMP (battle-tested against live clients) is followed and the
///		disagreement is noted inline.
/// </summary>
public partial class McpePlayerAuthInput : Packet<McpePlayerAuthInput>
{
	/// <summary>
	///		Pitch and Yaw hold the rotation that the player reports it has.
	/// </summary>
	public float Pitch;

	/// <summary>
	/// Pitch and Yaw hold the rotation that the player reports it has.
	/// </summary>
	public float Yaw;

	/// <summary>
	///		HeadYaw is the horizontal rotation of the head that the player reports it has.
	/// </summary>
	public float HeadYaw;

	/// <summary>
	///		 Position holds the position that the player reports it has.
	/// </summary>
	public Vector3 Position;

	/// <summary>
	///		MoveVector is a Vec2 that specifies the direction in which the player moved, as a combination of X/Z
	///		values which are created using the WASD/controller stick state.
	/// </summary>
	public Vector2 MoveVector;

	/// <summary>
	///		InputData is a combination of bit flags that together specify the way the player moved last tick.
	///		Flags 0-63; flag 64 (sneak_current_raw) is exposed separately as SneakCurrentRaw.
	/// </summary>
	public AuthInputFlags InputFlags;

	/// <summary>
	///		Flag 64 of the 65-flag input BitSet (sneak_current_raw), which does not fit in the 64-bit enum.
	/// </summary>
	public bool SneakCurrentRaw;

	/// <summary>
	///  InputMode specifies the way that the client inputs data to the screen.
	/// </summary>
	public PlayerInputMode InputMode;

	/// <summary>
	/// PlayMode specifies the way that the player is playing.
	/// </summary>
	public PlayerPlayMode PlayMode;

	/// <summary>
	///		InteractionModel is the interaction model the client is using (touch, crosshair, classic).
	/// </summary>
	public PlayerInteractionModel InteractionModel;

	/// <summary>
	///		InteractRotation is the rotation the player is looking at while interacting.
	/// </summary>
	public Vector2 InteractRotation;

	/// <summary>
	///		Tick is the client tick at which the packet was sent.
	/// </summary>
	public long Tick;

	/// <summary>
	///		Delta was the delta between the old and the new position.
	/// </summary>
	public Vector3 Delta;

	/// <summary>
	///		Single item stack request, present when InputFlags has PerformItemStackRequest.
	/// </summary>
	public ItemStackActionList ItemStackRequest;

	/// <summary>
	///		Block actions (start/abort/crack/predict/continue break etc), present when InputFlags
	///		has PerformBlockActions.
	/// </summary>
	public List<PlayerBlockAction> BlockActions;

	/// <summary>
	///		AnalogMoveVector is the analogue version of MoveVector (controller stick values).
	/// </summary>
	public Vector2 AnalogMoveVector;

	/// <summary>
	///		CameraOrientation is the direction in which the player's camera is facing.
	/// </summary>
	public Vector3 CameraOrientation;

	/// <summary>
	///		RawMoveVector is the raw (unfiltered) version of MoveVector.
	/// </summary>
	public Vector2 RawMoveVector;

	private const int NumberOfInputFlags = 65; // PMMP PlayerAuthInputFlags::NUMBER_OF_FLAGS

	partial void AfterDecode()
	{
		Pitch = ReadFloat();
		Yaw = ReadFloat();
		Position = ReadVector3();
		MoveVector = ReadVector2();
		HeadYaw = ReadFloat();

		// input_data: since 2168 a list of pressed-flag ordinals (zigzag varints) behind an
		// always-true marker bool, replacing the 65-flag varint BitSet. Ordinal 64 is
		// sneak_current_raw, which never fit the 64-bit enum.
		ReadBool(); // marker, vanilla writes true
		uint flagCount = ReadUnsignedVarInt();
		InputFlags = 0;
		SneakCurrentRaw = false;
		for (int i = 0; i < flagCount; i++)
		{
			int ordinal = ReadSignedVarInt();
			if (ordinal == 64) SneakCurrentRaw = true;
			else if (ordinal >= 0 && ordinal < 64) InputFlags |= (AuthInputFlags) (long) (1UL << ordinal);
		}

		InputMode = (PlayerInputMode) ReadUnsignedVarInt();
		PlayMode = (PlayerPlayMode) ReadUnsignedVarInt();
		// PMMP reads this as unsigned varint; minecraft-data 1001 claims zigzag32. The values
		// are 0-2 so both are a single byte on the wire; following PMMP.
		InteractionModel = (PlayerInteractionModel) ReadUnsignedVarInt();
		InteractRotation = ReadVector2();
		Tick = ReadUnsignedVarLong();
		Delta = ReadVector3();

		// Conditional bodies. Since 2168 each travels as a double-bool optional (an always-true
		// outer marker, then the actual presence bool), in this order: item interaction, item
		// stack request, block actions, vehicle rotation, predicted vehicle. Vehicle rotation
		// and the predicted vehicle id are separate optionals now.
		ReadBool(); // marker
		if (ReadBool())
		{
			ReadItemInteractionData();
		}

		ReadBool(); // marker
		if (ReadBool())
		{
			ItemStackRequest = ReadItemStackRequest();
		}

		ReadBool(); // marker
		if (ReadBool())
		{
			int count = (int) ReadUnsignedVarInt(); // unsigned since 2168, was zigzag
			BlockActions = new List<PlayerBlockAction>(count);
			for (int i = 0; i < count; i++)
			{
				var blockAction = new PlayerBlockAction {ActionType = ReadSignedVarInt()};
				switch (blockAction.ActionType)
				{
					case 0: // start_break
					case 1: // abort_break
					case 18: // crack_break
					case 26: // predict_break
					case 27: // continue_break
						// Signed block position (x/y/z all zigzag; PMMP getSignedBlockPosition,
						// unlike the y-unsigned ReadBlockCoordinates) + face.
						blockAction.X = ReadSignedVarInt();
						blockAction.Y = ReadSignedVarInt();
						blockAction.Z = ReadSignedVarInt();
						blockAction.Face = ReadSignedVarInt();
						break;
				}
				BlockActions.Add(blockAction);
			}
		}

		ReadBool(); // marker
		if (ReadBool())
		{
			ReadVector2(); // vehicle rotation
		}

		ReadBool(); // marker
		if (ReadBool())
		{
			ReadSignedVarLong(); // predicted vehicle actor unique id
		}

		AnalogMoveVector = ReadVector2();
		CameraOrientation = ReadVector3();
		RawMoveVector = ReadVector2();
	}

	// ItemInteractionData: legacy request id (+ changed-slots hack when non-zero), the
	// auth-input flavour of inventory actions (items use the zigzag wrapper encoding, unlike
	// the li16 descriptors in the standalone McpeInventoryTransaction), then the use-item
	// transaction body. Parsed for wire alignment; values are not retained yet. Verified vs
	// PMMP ItemInteractionData::read -> TransactionData::decodeAuthInput ->
	// UseItemTransactionData::decodeData.
	private void ReadItemInteractionData()
	{
		int legacyRequestId = ReadSignedVarInt();
		if (legacyRequestId != 0)
		{
			uint containerCount = ReadUnsignedVarInt();
			for (int i = 0; i < containerCount; i++)
			{
				ReadByte(); // container id
				uint slotCount = ReadUnsignedVarInt();
				for (int j = 0; j < slotCount; j++) ReadByte(); // changed slot
			}
		}

		uint actionCount = ReadUnsignedVarInt();
		for (int i = 0; i < actionCount; i++)
		{
			uint sourceType = ReadUnsignedVarInt();
			switch (sourceType)
			{
				case 0: // container
				case 99999: // TODO/craft
					ReadSignedVarInt(); // window id
					break;
				case 2: // world interaction
					ReadUnsignedVarInt(); // flags
					break;
				case 1: // global
				case 3: // creative
					break;
			}
			ReadUnsignedVarInt(); // slot
			ReadItemStackWrapper(); // old item (zigzag wrapper; PMMP readAuthInput getItemStackWrapper)
			ReadItemStackWrapper(); // new item
		}

		// Use-item body (PMMP UseItemTransactionData::decodeData).
		ReadSignedVarInt(); // action type
		ReadByte(); // trigger type
		ReadBlockCoordinates(); // block position (x zigzag, y unsigned varint, z zigzag)
		ReadByte(); // face
		ReadSignedVarInt(); // hotbar slot
		ReadItem(); // held item (li16 descriptor; PMMP getNetworkItemStackDescriptor - NOT the zigzag wrapper minecraft-data claims)
		ReadVector3(); // player position
		ReadVector3(); // click position
		ReadUnsignedVarInt(); // block runtime id
		ReadByte(); // client prediction
		ReadByte(); // client cooldown state
	}

	partial void AfterEncode()
	{
		Write(Pitch);
		Write(Yaw);
		Write(Position);
		Write(MoveVector);
		Write(HeadYaw);

		// Encode supports the scalar fields plus the block-actions body (the emulator breaks
		// blocks the way a real 2168 client does, inside auth input); the other conditional
		// bodies (item interaction, stack request, vehicle) are not written, so their flags are
		// stripped to keep the packet self-consistent.
		// Since 2168 the flags travel as a list of pressed ordinals (zigzag varints) behind an
		// always-true marker bool, and each conditional body is a double-bool optional.
		bool hasBlockActions = BlockActions is {Count: > 0};
		var flags = InputFlags & ~(AuthInputFlags.PerformItemInteraction | AuthInputFlags.PerformItemStackRequest | AuthInputFlags.ClientPredictedVehicle);
		if (hasBlockActions) flags |= AuthInputFlags.PerformBlockActions;
		else flags &= ~AuthInputFlags.PerformBlockActions;

		Write(true); // marker
		ulong low = (ulong) (long) flags;
		var ordinals = new List<int>();
		for (int i = 0; i < 64; i++)
		{
			if ((low >> i & 1UL) != 0) ordinals.Add(i);
		}
		if (SneakCurrentRaw) ordinals.Add(64);
		WriteUnsignedVarInt((uint) ordinals.Count);
		foreach (int ordinal in ordinals)
		{
			WriteSignedVarInt(ordinal);
		}

		WriteUnsignedVarInt((uint) InputMode);
		WriteUnsignedVarInt((uint) PlayMode);
		WriteUnsignedVarInt((uint) InteractionModel);
		Write(InteractRotation);
		WriteUnsignedVarLong(Tick);
		Write(Delta);

		Write(true); // marker: item interaction
		Write(false);
		Write(true); // marker: item stack request
		Write(false);

		Write(true); // marker: block actions
		Write(hasBlockActions);
		if (hasBlockActions)
		{
			WriteUnsignedVarInt((uint) BlockActions.Count);
			foreach (PlayerBlockAction action in BlockActions)
			{
				WriteSignedVarInt(action.ActionType);
				switch (action.ActionType)
				{
					case 0: // start_break
					case 1: // abort_break
					case 18: // crack_break
					case 26: // predict_break
					case 27: // continue_break
						WriteSignedVarInt(action.X);
						WriteSignedVarInt(action.Y);
						WriteSignedVarInt(action.Z);
						WriteSignedVarInt(action.Face);
						break;
				}
			}
		}

		Write(true); // marker: vehicle rotation
		Write(false);
		Write(true); // marker: predicted vehicle
		Write(false);

		Write(AnalogMoveVector);
		Write(CameraOrientation);
		Write(RawMoveVector);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		Pitch = Yaw = HeadYaw = 0f;
		MoveVector = Vector2.Zero;
		Position = Vector3.Zero;
		InputFlags = 0;
		SneakCurrentRaw = false;
		InputMode = PlayerInputMode.Mouse;
		PlayMode = PlayerPlayMode.Normal;
		InteractionModel = PlayerInteractionModel.Touch;
		InteractRotation = Vector2.Zero;
		Tick = 0;
		Delta = Vector3.Zero;
		ItemStackRequest = null;
		BlockActions = null;
		AnalogMoveVector = Vector2.Zero;
		CameraOrientation = Vector3.Zero;
		RawMoveVector = Vector2.Zero;
	}

	public enum PlayerPlayMode
	{
		Normal = 0,
		Teaser = 1,
		Screen = 2,
		Viewer = 3,
		VR = 4,
		Placement = 5,
		LivingRoom = 6,
		ExitLevel = 7,
		ExitLevelLivingRoom = 8
	}

	public enum PlayerInputMode
	{
		Mouse = 1,
		Touch = 2,
		GamePad = 3,
		MotionController = 4
	}

	public enum PlayerInteractionModel
	{
		Touch = 0,
		Crosshair = 1,
		Classic = 2
	}

	public class PlayerBlockAction
	{
		public int ActionType;
		public int X;
		public int Y;
		public int Z;
		public int Face;
	}
}
