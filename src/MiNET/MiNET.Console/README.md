# MiNET.Console

The console host that boots `MiNetServer`. It also doubles as the client for its own remote
console, so one binary is both the server and the tool you drive it with.

## Running the server

```bash
dotnet run --project src/MiNET/MiNET.Console
```

Configuration is read from `server.conf` in the working directory, as key=value through
`MiNET.Utils.Config`. A per-machine file such as `server.nicke.conf` takes its place when present.

Plugins are loaded at runtime from `PluginDirectory`, which means they are **not** project
references and `dotnet run` will not build them. After changing anything under `TestPlugin`,
`MiNET.Plotter` or `MiNET.BuilderBase`, build the solution first or you will run the old plugin
against new server code:

```bash
dotnet build src/MiNET/MiNET.sln
dotnet run --project src/MiNET/MiNET.Console --no-build
```

## Stopping it so the level saves

Pressing enter stops the server properly. A killed process never reaches `StopServer`, so every
change since the last save interval is lost.

A headless run has no stdin to type into, so `SIGINT` and `SIGTERM` stop it the same way, and so
does creating the file `temp_auto/stop-server` under the working directory. The server deletes the
file and shuts down cleanly.

## Remote console

Runs server commands from another shell or another machine, as a player that does not exist. Nobody
has to be connected, which is the point: a protocol change can be exercised without a client, and
without typing into the game.

### Configuration

```ini
RemoteConsole.Enabled=true
RemoteConsole.BindAddress=127.0.0.1
RemoteConsole.Port=19140
RemoteConsole.Secret=<64 hex characters>
```

`BindAddress` defaults to loopback. Set it to `0.0.0.0` to reach the server from another machine.

If `RemoteConsole.Secret` is empty the console refuses to start and logs a generated secret you can
paste into the config, so there is no way to leave an open command channel running by accident.

### Using it

```bash
# One command, exit code reflects success
MiNET.Console remote time set 6000

# Against another host
MiNET.Console remote --host 10.0.0.5 --port 19140 --secret <secret> players

# A session: one command per input line until end of input
printf 'time set 1000\ngamerule dodaylightcycle false\n' | MiNET.Console remote
```

The secret comes from `--secret` or the `MINET_REMOTE_SECRET` environment variable. Exit codes are
`0` success, `1` connection or transport failure, `2` no secret supplied, `3` rejected by the server.

Every accepted connection, every command and every rejection is logged server side.

### Authentication

The secret is never transmitted. On connect the server sends a fresh random 32-byte nonce, the
client answers with `HMAC-SHA256(secret, nonce)`, and the server recomputes it and compares in fixed
time. A captured exchange cannot be replayed against a later connection, because the nonce is new
every time.

There is no transport encryption, so command text and its output do travel in the clear. That is
fine on loopback or a trusted network; tunnel it over SSH if it is neither.

### Wire format

Every message is a little-endian `int32` byte count followed by that many bytes of UTF-8, capped at
1 MB so a bad peer cannot ask for an unbounded allocation.

```
server -> client   nonce, 64 hex characters
client -> server   HMAC-SHA256(secret, nonce), 64 hex characters
server -> client   "OK", or "DENIED" followed by close
client -> server   command line
server -> client   output
                   (last two repeat)
```

This is deliberately not Valve's Source RCON. The shape is the same, but RCON authenticates by
sending the password itself over an unencrypted socket, which is the part worth avoiding.

### What it cannot do

Commands run as `ConsolePlayer`, which has no network session. `Player.SendPacket` drops packets
when there is no handler, so a command whose whole purpose is to send one to its caller will report
success and do nothing. Nothing throws, but nothing happens either.

Commands that act on the world, on the server, or that broadcast to connected players all work
normally. `/r` is the useful example of the last kind: it takes the level from the caller and then
sends to everyone spawned, so it reaches real clients from the console just fine.
