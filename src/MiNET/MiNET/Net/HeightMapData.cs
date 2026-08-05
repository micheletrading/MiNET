using System;

namespace MiNET.Net;

public class HeightMapData
{
	// The wire type this instance represents. Explicit rather than inferred from Heights, so a
	// decoded AllTooHigh/AllTooLow/AllCopied marker (which carries no height array at all) writes
	// back out exactly as read instead of being re-guessed from data that doesn't exist for it.
	public SubChunkPacketHeightMapType Type { get; }

	public short[] Heights { get; }

	public HeightMapData(short[] heights)
	{
		if (heights.Length != 256)
			throw new ArgumentException("Expected 256 data entries");

		Heights = heights;
		Type = SubChunkPacketHeightMapType.Data;
	}

	public HeightMapData(SubChunkPacketHeightMapType type)
	{
		if (type == SubChunkPacketHeightMapType.Data)
			throw new ArgumentException("Data requires a 256-entry heights array; use the other constructor.");

		Type = type;
	}

	public int GetHeight(int x, int z)
	{
		return Heights[((z & 0xf) << 4) | (x & 0xf)];
	}
}

public enum SubChunkPacketHeightMapType : byte
{
	NoData = 0,
	Data = 1,
	AllTooHigh = 2,
	AllTooLow = 3,
	AllCopied = 4
}

public enum SubChunkRequestResult : byte
{
	Success = 1,
	NoSuchChunk = 2,
	WrongDimension = 3,
	NullPlayer = 4, 
	YIndexOutOfBounds = 5,
	SuccessAllAir = 6
}