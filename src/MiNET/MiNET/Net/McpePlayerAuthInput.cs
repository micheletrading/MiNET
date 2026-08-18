using System.Collections.Generic;

namespace MiNET.Net;

/// <summary>The one codec the schema cannot describe. Everything else about this packet is generated
/// from PlayerAuthInputPacket.json.</summary>
public partial class McpePlayerAuthInput : Packet<McpePlayerAuthInput>
{
	// PMMP PlayerAuthInputFlags::NUMBER_OF_FLAGS. The 65th does not fit a 64-bit mask, which is why
	// AuthInputFlags carries it as a member of its own rather than a bit.
	private const int NumberOfInputFlags = 65;

	/// <summary>The pressed flags: a count, then one zigzag varint per pressed ordinal. The schema
	/// calls this an array of enum values, which is the same information, and says nothing about how
	/// it travels.</summary>
	private void WriteAuthInputFlags(AuthInputFlags flags)
	{
		var ordinals = new List<int>();
		ulong bits = (ulong) (long) flags;
		for (int i = 0; i < NumberOfInputFlags; i++)
		{
			if ((bits >> i & 1UL) != 0) ordinals.Add(i);
		}

		WriteUnsignedVarInt((uint) ordinals.Count);
		foreach (int ordinal in ordinals)
		{
			WriteSignedVarInt(ordinal);
		}
	}

	private AuthInputFlags ReadAuthInputFlags()
	{
		AuthInputFlags flags = 0;

		uint count = ReadUnsignedVarInt();
		for (int i = 0; i < count; i++)
		{
			int ordinal = ReadSignedVarInt();
			if (ordinal >= 0 && ordinal < NumberOfInputFlags) flags |= (AuthInputFlags) (long) (1UL << ordinal);
		}

		return flags;
	}
}
