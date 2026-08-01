using MiNET.Items;

namespace MiNET.BuilderBase
{
	/// <summary>
	///     The builder tools are ordinary items with extra behaviour, so they are recognised by the
	///     registry name of the item they stand in for: an iron shovel is the brush, an iron axe the
	///     distance wand, a compass the teleport tool.
	/// </summary>
	public class BuilderBaseItemFactory : ICustomItemFactory
	{
		public Item GetItem(string name, short metadata, int count)
		{
			return name switch
			{
				"minecraft:iron_shovel" => new Tools.BrushTool(),
				"minecraft:iron_axe" => new Tools.DistanceWand(),
				"minecraft:compass" => new Tools.TeleportTool(),
				_ => null
			};
		}
	}
}
