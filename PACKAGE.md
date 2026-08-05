# MiNET

A Minecraft: Bedrock Edition server written in C#, targeting .NET 10. This package is
the server library: the RakNet transport, the Bedrock protocol, worlds, entities, and
the plugin host.

Current target is protocol 1001, game version 1.26.34.

## Running a server

```csharp
using MiNET;

var server = new MiNetServer();
server.StartServer();

Console.WriteLine("MiNET running. Press <enter> to stop.");
Console.ReadLine();

server.StopServer();
```

Configuration is read from `server.conf` in the working directory as `key=value`
pairs, through `MiNET.Utils.Config`. Listening port, world provider, game mode, view
distance and the plugin directory all come from there.

## Worlds

World storage is pluggable through `IWorldProvider`. In the box:

- `LevelDbProvider` reads and writes the Bedrock LevelDB format.
- `AnvilWorldProvider` reads Java Anvil worlds, converting blocks on load.
- `SuperflatGenerator` and friends generate in memory, taking the same preset string
  as Java: `minecraft:bedrock,2*minecraft:dirt,minecraft:grass_block;minecraft:plains`.

## Plugins

A plugin is a class carrying `[Plugin]` or implementing `IPlugin`. Assemblies in the
configured plugin directory are scanned at startup.

```csharp
[Plugin(PluginName = "HelloWorld")]
public class HelloWorld : Plugin
{
    [Command(Description = "Says hello")]
    public string Hello(Player player)
    {
        return $"Hello {player.Username}";
    }

    [PacketHandler, Receive]
    public Packet OnMovement(McpeMovePlayer message, Player player)
    {
        return message;
    }
}
```

`[Command]` methods become server commands, with parameters typed as `Target`,
`BlockPos`, `BlockStates` and the rest so the client offers the right picker.
`[Authorize]` gates them by permission. `[PacketHandler]` intercepts packets in either
direction.

## Blocks

Blocks are addressed by name and by runtime id, not by the pre-flattening numeric id:

```csharp
Block log = BlockFactory.GetBlockByName("minecraft:oak_log");
((OakLog) log).PillarAxis = "x";

level.SetBlock(log);
```

## Links

- [Source and issues](https://github.com/NiclasOlofsson/MiNET)
- [Wiki](https://github.com/NiclasOlofsson/MiNET/wiki)
- [Generated Bedrock protocol specification](https://github.com/NiclasOlofsson/MiNET/blob/master/src/MiNET/MiNET/Net/MCPE%20Protocol%20Documentation.md)
- [Discord](https://discord.gg/xCNrhDd)

Licensed under the Common Public Attribution License 1.0. See
[LICENSE](https://github.com/NiclasOlofsson/MiNET/blob/master/LICENSE).
