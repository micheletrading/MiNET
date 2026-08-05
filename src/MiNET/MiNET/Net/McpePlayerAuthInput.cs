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

		// input_data: 65-flag BitSet encoded varint-style, 7 bits per byte plus a continuation
		// bit, bounded by ceil(65/7) = 10 bytes. NOT a plain varlong (minecraft-data: varint128;
		// PMMP: BitSet::read). Reading it as varlong throws once flags >= bit 63 appear.
		ulong low = 0;
		ulong high = 0;
		int shift = 0;
		for (int i = 0; i < NumberOfInputFlags; i += 7)
		{
			byte b = ReadByte();
			ulong bits = (ulong) (b & 0x7f);
			if (shift < 64)
			{
				low |= bits << shift;
				if (shift > 57) high |= bits >> (64 - shift);
			}
			else
			{
				high |= bits << (shift - 64);
			}
			shift += 7;
			if ((b & 0x80) == 0) break;
		}
		InputFlags = (AuthInputFlags) (long) low;
		SneakCurrentRaw = (high & 1) != 0;

		InputMode = (PlayerInputMode) ReadUnsignedVarInt();
		PlayMode = (PlayerPlayMode) ReadUnsignedVarInt();
		// PMMP reads this as unsigned varint; minecraft-data 1001 claims zigzag32. The values
		// are 0-2 so both are a single byte on the wire; following PMMP.
		InteractionModel = (PlayerInteractionModel) ReadUnsignedVarInt();
		InteractRotation = ReadVector2();
		Tick = ReadUnsignedVarLong();
		Delta = ReadVector3();

		// Conditional bodies. Order per PMMP decodePayload: item interaction, item stack
		// request, block actions, predicted vehicle. (minecraft-data lists vehicle before
		// block actions; PMMP is followed.)
		if ((InputFlags & AuthInputFlags.PerformItemInteraction) != 0)
		{
			ReadItemInteractionData();
		}

		if ((InputFlags & AuthInputFlags.PerformItemStackRequest) != 0)
		{
			ItemStackRequest = ReadItemStackRequest();
		}

		if ((InputFlags & AuthInputFlags.PerformBlockActions) != 0)
		{
			int count = ReadSignedVarInt();
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

		if ((InputFlags & AuthInputFlags.ClientPredictedVehicle) != 0)
		{
			ReadVector2(); // vehicle rotation
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
			ReadItemInstance(); // old item (zigzag wrapper; PMMP readAuthInput getItemStackWrapper)
			ReadItemInstance(); // new item
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

		// Encode supports the scalar fields only; the conditional bodies (item interaction,
		// stack request, block actions, vehicle) are not written, so their flags are stripped
		// to keep the packet self-consistent. MiNET only encodes this packet from the test
		// client, which sends plain movement.
		ulong low = (ulong) (long) (InputFlags & ~(AuthInputFlags.PerformItemInteraction | AuthInputFlags.PerformItemStackRequest | AuthInputFlags.PerformBlockActions | AuthInputFlags.ClientPredictedVehicle));
		ulong high = SneakCurrentRaw ? 1UL : 0UL;
		for (int i = 0; i < NumberOfInputFlags; i += 7)
		{
			int shift = i;
			byte bits = shift < 64
				? (byte) ((low >> shift | (shift > 57 ? high << (64 - shift) : 0)) & 0x7f)
				: (byte) ((high >> (shift - 64)) & 0x7f);
			bool more = i + 7 < NumberOfInputFlags && (shift + 7 < 64 ? (low >> (shift + 7)) != 0 || high != 0 : (high >> (shift + 7 - 64)) != 0);
			Write((byte) (bits | (more ? 0x80 : 0)));
			if (!more) break;
		}

		WriteUnsignedVarInt((uint) InputMode);
		WriteUnsignedVarInt((uint) PlayMode);
		WriteUnsignedVarInt((uint) InteractionModel);
		Write(InteractRotation);
		WriteUnsignedVarLong(Tick);
		Write(Delta);
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
