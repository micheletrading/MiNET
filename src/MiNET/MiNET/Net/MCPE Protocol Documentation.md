
**WARNING: T4 GENERATED MARKUP - DO NOT EDIT**

Read more about packets and this specification on the [Protocol Wiki](https://github.com/NiclasOlofsson/MiNET/wiki//ref-protocol)

## ALL PACKETS

| ID  | ID (hex) | ID (dec) | 
|:--- |:---------|---------:| 
| Login | 0x01 | 1 |   
| Play Status | 0x02 | 2 |   
| Server To Client Handshake | 0x03 | 3 |   
| Client To Server Handshake | 0x04 | 4 |   
| Disconnect | 0x05 | 5 |   
| Resource Packs Info | 0x06 | 6 |   
| Resource Pack Stack | 0x07 | 7 |   
| Resource Pack Client Response | 0x08 | 8 |   
| Text | 0x09 | 9 |   
| Set Time | 0x0a | 10 |   
| Start Game | 0x0b | 11 |   
| Add Player | 0x0c | 12 |   
| Add Entity | 0x0d | 13 |   
| Remove Entity | 0x0e | 14 |   
| Add Item Entity | 0x0f | 15 |   
| Take Item Entity | 0x11 | 17 |   
| Move Entity | 0x12 | 18 |   
| Camera Instruction | 0x12c | 18 |   
| Trim Data | 0x12e | 18 |   
| Move Player | 0x13 | 19 |   
| Set Player Inventory Options | 0x133 | 19 |   
| Server Bound Loading Screen | 0x138 | 19 |   
| Jigsaw Structure Data | 0x139 | 19 |   
| Current Structure Feature | 0x13a | 19 |   
| Server Bound Diagnostics | 0x13b | 19 |   
| Rider Jump | 0x14 | 20 |   
| Camera Aim Assist Presets | 0x140 | 20 |   
| Client Camera Aim Assist | 0x141 | 20 |   
| Player Location | 0x146 | 20 |   
| Update Block | 0x15 | 21 |   
| Voxel Shapes | 0x151 | 21 |   
| Camera Spline | 0x152 | 21 |   
| Locator Bar | 0x155 | 21 |   
| Sync World Clocks | 0x158 | 21 |   
| Add Painting | 0x16 | 22 |   
| Tick Sync | 0x17 | 23 |   
| Level Sound Event Old | 0x18 | 24 |   
| Level Event | 0x19 | 25 |   
| Block Event | 0x1a | 26 |   
| Entity Event | 0x1b | 27 |   
| Mob Effect | 0x1c | 28 |   
| Update Attributes | 0x1d | 29 |   
| Inventory Transaction | 0x1e | 30 |   
| Mob Equipment | 0x1f | 31 |   
| Mob Armor Equipment | 0x20 | 32 |   
| Interact | 0x21 | 33 |   
| Block Pick Request | 0x22 | 34 |   
| Entity Pick Request | 0x23 | 35 |   
| Player Action | 0x24 | 36 |   
| Hurt Armor | 0x26 | 38 |   
| Set Entity Data | 0x27 | 39 |   
| Set Entity Motion | 0x28 | 40 |   
| Set Entity Link | 0x29 | 41 |   
| Set Health | 0x2a | 42 |   
| Set Spawn Position | 0x2b | 43 |   
| Animate | 0x2c | 44 |   
| Respawn | 0x2d | 45 |   
| Container Open | 0x2e | 46 |   
| Container Close | 0x2f | 47 |   
| Player Hotbar | 0x30 | 48 |   
| Inventory Content | 0x31 | 49 |   
| Inventory Slot | 0x32 | 50 |   
| Container Set Data | 0x33 | 51 |   
| Crafting Data | 0x34 | 52 |   
| Crafting Event | 0x35 | 53 |   
| Gui Data Pick Item | 0x36 | 54 |   
| Block Entity Data | 0x38 | 56 |   
| Level Chunk | 0x3a | 58 |   
| Set Commands Enabled | 0x3b | 59 |   
| Set Difficulty | 0x3c | 60 |   
| Change Dimension | 0x3d | 61 |   
| Set Player Game Type | 0x3e | 62 |   
| Player List | 0x3f | 63 |   
| Simple Event | 0x40 | 64 |   
| Telemetry Event | 0x41 | 65 |   
| Spawn Experience Orb | 0x42 | 66 |   
| Clientbound Map Item Data  | 0x43 | 67 |   
| Map Info Request | 0x44 | 68 |   
| Request Chunk Radius | 0x45 | 69 |   
| Chunk Radius Update | 0x46 | 70 |   
| Game Rules Changed | 0x48 | 72 |   
| Camera | 0x49 | 73 |   
| Boss Event | 0x4a | 74 |   
| Show Credits | 0x4b | 75 |   
| Available Commands | 0x4c | 76 |   
| Command Request | 0x4d | 77 |   
| Command Block Update | 0x4e | 78 |   
| Command Output | 0x4f | 79 |   
| Update Trade | 0x50 | 80 |   
| Update Equipment | 0x51 | 81 |   
| Resource Pack Data Info | 0x52 | 82 |   
| Resource Pack Chunk Data | 0x53 | 83 |   
| Resource Pack Chunk Request | 0x54 | 84 |   
| Transfer | 0x55 | 85 |   
| Play Sound | 0x56 | 86 |   
| Stop Sound | 0x57 | 87 |   
| Set Title | 0x58 | 88 |   
| Add Behavior Tree | 0x59 | 89 |   
| Structure Block Update | 0x5a | 90 |   
| Show Store Offer | 0x5b | 91 |   
| Purchase Receipt | 0x5c | 92 |   
| Player Skin | 0x5d | 93 |   
| Sub Client Login | 0x5e | 94 |   
| Initiate Web Socket Connection | 0x5f | 95 |   
| Set Last Hurt By | 0x60 | 96 |   
| Book Edit | 0x61 | 97 |   
| Npc Request | 0x62 | 98 |   
| Photo Transfer | 0x63 | 99 |   
| Modal Form Request | 0x64 | 100 |   
| Modal Form Response | 0x65 | 101 |   
| Server Settings Request | 0x66 | 102 |   
| Server Settings Response | 0x67 | 103 |   
| Show Profile | 0x68 | 104 |   
| Set Default Game Type | 0x69 | 105 |   
| Remove Objective | 0x6a | 106 |   
| Set Display Objective | 0x6b | 107 |   
| Set Score | 0x6c | 108 |   
| Lab Table | 0x6d | 109 |   
| Update Block Synced | 0x6e | 110 |   
| Move Entity Delta | 0x6f | 111 |   
| Set Scoreboard Identity | 0x70 | 112 |   
| Set Local Player As Initialized | 0x71 | 113 |   
| Update Soft Enum | 0x72 | 114 |   
| Network Stack Latency | 0x73 | 115 |   
| Script Custom Event | 0x75 | 117 |   
| Spawn Particle Effect | 0x76 | 118 |   
| Available Entity Identifiers | 0x77 | 119 |   
| Level Sound Event V2 | 0x78 | 120 |   
| Network Chunk Publisher Update | 0x79 | 121 |   
| Biome Definition List | 0x7a | 122 |   
| Level Sound Event | 0x7b | 123 |   
| Level Event Generic | 0x7c | 124 |   
| Lectern Update | 0x7d | 125 |   
| Video Stream Connect | 0x7e | 126 |   
| Client Cache Status | 0x81 | 129 |   
| On Screen Texture Animation | 0x82 | 130 |   
| Map Create Locked Copy | 0x83 | 131 |   
| Structure Template Data Export Request | 0x84 | 132 |   
| Structure Template Data Export Response | 0x85 | 133 |   
| Update Block Properties | 0x86 | 134 |   
| Client Cache Blob Status | 0x87 | 135 |   
| Client Cache Miss Response | 0x88 | 136 |   
| Network Settings | 0x8f | 143 |   
| Player Auth Input | 0x90 | 144 |   
| Creative Content | 0x91 | 145 |   
| Player Enchant Options | 0x92 | 146 |   
| Item Stack Request | 0x93 | 147 |   
| Item Stack Response | 0x94 | 148 |   
| Update Player Game Type | 0x97 | 151 |   
| Emote List | 0x98 | 152 |   
| Packet Violation Warning | 0x9c | 156 |   
| Camera Shake | 0x9f | 159 |   
| Player Fog | 0xa0 | 160 |   
| Correct Player Move Prediction | 0xa1 | 161 |   
| Item Component | 0xa2 | 162 |   
| Filter Text Packet | 0xa3 | 163 |   
| Sync Entity Property | 0xa5 | 165 |   
| Update Sub Chunk Blocks Packet | 0xac | 172 |   
| Sub Chunk Packet | 0xae | 174 |   
| Sub Chunk Request Packet | 0xaf | 175 |   
| Dimension Data | 0xb4 | 180 |   
| Update Abilities | 0xbb | 187 |   
| Update Adventure Settings | 0xbc | 188 |   
| Request Network Settings | 0xc1 | 193 |   
| Camera Presets | 0xc6 | 198 |   


## Data types

| Data type | 
|:--- |
| BlockCoordinates [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-BlockCoordinates) |
| bool [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-bool) |
| byte [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-byte) |
| byte[] [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-byte[]) |
| ByteArray [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ByteArray) |
| CommandOriginData [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-CommandOriginData) |
| DimensionDefinitions [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-DimensionDefinitions) |
| EnchantOptions [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-EnchantOptions) |
| EntityAttributes [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-EntityAttributes) |
| Experiments [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Experiments) |
| FixedString [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-FixedString) |
| float [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-float) |
| int [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-int) |
| IPEndPoint [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-IPEndPoint) |
| IPEndPoint[] [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-IPEndPoint[]) |
| Item [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Item) |
| ItemComponentList [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ItemComponentList) |
| ItemInstance [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ItemInstance) |
| ItemStackRequests [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ItemStackRequests) |
| ItemStackResponses [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ItemStackResponses) |
| ItemStacks [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ItemStacks) |
| long [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-long) |
| MapInfo [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-MapInfo) |
| MaterialReducerRecipe[] [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-MaterialReducerRecipe[]) |
| MetadataDictionary [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-MetadataDictionary) |
| Nbt [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Nbt) |
| NbtBody [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-NbtBody) |
| OFFLINE_MESSAGE_DATA_ID [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-OFFLINE_MESSAGE_DATA_ID) |
| PlayerAttributes [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-PlayerAttributes) |
| PlayerLocation [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-PlayerLocation) |
| PlayerRecords [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-PlayerRecords) |
| PotionContainerChangeRecipe[] [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-PotionContainerChangeRecipe[]) |
| PotionTypeRecipe[] [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-PotionTypeRecipe[]) |
| Recipes [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Recipes) |
| ResourcePackIds [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ResourcePackIds) |
| ResourcePackIdVersions [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ResourcePackIdVersions) |
| ScoreboardIdentityEntries [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ScoreboardIdentityEntries) |
| ScoreEntries [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ScoreEntries) |
| short [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-short) |
| SignedVarInt [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-SignedVarInt) |
| SignedVarLong [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-SignedVarLong) |
| Skin [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Skin) |
| string [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-string) |
| StructureSettings [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-StructureSettings) |
| TexturePackInfos [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-TexturePackInfos) |
| Transaction [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Transaction) |
| uint [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-uint) |
| ulong [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ulong) |
| UnsignedVarInt [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-UnsignedVarInt) |
| UnsignedVarLong [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-UnsignedVarLong) |
| UpdateSubChunkBlocksPacketEntry[] [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-UpdateSubChunkBlocksPacketEntry[]) |
| ushort [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-ushort) |
| UUID [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-UUID) |
| VarInt [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-VarInt) |
| Vector3 [(wiki)](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Type-Vector3) |

## Constants
	OFFLINE_MESSAGE_DATA_ID
	byte[]
	{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }

## Packets

### Login (0x01)
Wiki: [Login](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Login)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Protocol Version | int |  |
|Payload | ByteArray |  |
-----------------------------------------------------------------------
### Play Status (0x02)
Wiki: [Play Status](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayStatus)

**Sent from server:** true  
**Sent from client:** false



#### Play Status constants

| Name | Value |
|:-----|:-----|
|Login Success | 0 |
|Login Failed Client | 1 |
|Login Failed Server | 2 |
|Player Spawn | 3 |
|Login Failed Invalid Tenant | 4 |
|Login Failed Vanilla Edu | 5 |
|Login Failed Edu Vanilla | 6 |
|Login Failed Server Full | 7 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Status | int |  |
-----------------------------------------------------------------------
### Server To Client Handshake (0x03)
Wiki: [Server To Client Handshake](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ServerToClientHandshake)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Token | string |  |
-----------------------------------------------------------------------
### Client To Server Handshake (0x04)
Wiki: [Client To Server Handshake](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ClientToServerHandshake)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Disconnect (0x05)
Wiki: [Disconnect](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Disconnect)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Reason | SignedVarInt |  |
|Hide disconnect reason | bool |  |
-----------------------------------------------------------------------
### Resource Packs Info (0x06)
Wiki: [Resource Packs Info](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ResourcePacksInfo)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Must accept | bool |  |
|Has addon packs | bool |  |
|Has scripts | bool |  |
|Disable vibrant visuals | bool |  |
|World template id | UUID |  |
|World template version | string |  |
|TexturePacks | TexturePackInfos |  |
-----------------------------------------------------------------------
### Resource Pack Stack (0x07)
Wiki: [Resource Pack Stack](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ResourcePackStack)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Must accept | bool |  |
|ResourcePackIdVersions | ResourcePackIdVersions |  |
|Game Version | string |  |
|Experiments | Experiments |  |
|Experiments Previously Toggled | bool |  |
|Has editor packs | bool |  |
-----------------------------------------------------------------------
### Resource Pack Client Response (0x08)
Wiki: [Resource Pack Client Response](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ResourcePackClientResponse)

**Sent from server:** false  
**Sent from client:** true



#### Response Status constants

| Name | Value |
|:-----|:-----|
|Refused | 1 |
|Send Packs | 2 |
|Have All Packs | 3 |
|Completed | 4 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Response status | byte |  |
|ResourcePackIds | ResourcePackIds |  |
-----------------------------------------------------------------------
### Text (0x09)
Wiki: [Text](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Text)

**Sent from server:** true  
**Sent from client:** true



#### Chat Types constants

| Name | Value |
|:-----|:-----|
|Raw | 0 |
|Chat | 1 |
|Translation | 2 |
|Popup | 3 |
|Jukeboxpopup | 4 |
|Tip | 5 |
|System | 6 |
|Whisper | 7 |
|Announcement | 8 |
|Jsonwhisper | 9 |
|Json | 10 |
|Jsonannouncement | 11 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Set Time (0x0a)
Wiki: [Set Time](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetTime)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Time | SignedVarInt |  |
-----------------------------------------------------------------------
### Start Game (0x0b)
Wiki: [Start Game](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-StartGame)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Add Player (0x0c)
Wiki: [Add Player](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AddPlayer)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|UUID | UUID |  |
|Username | string |  |
|Runtime Entity ID | UnsignedVarLong |  |
|Platform Chat ID | string |  |
|X | float |  |
|Y | float |  |
|Z | float |  |
|Speed X | float |  |
|Speed Y | float |  |
|Speed Z | float |  |
|Pitch | float |  |
|Yaw | float |  |
|Head Yaw | float |  |
-----------------------------------------------------------------------
### Add Entity (0x0d)
Wiki: [Add Entity](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AddEntity)

**Sent from server:** true  
**Sent from client:** false


TODO: Links
count short
loop
link[0] long
link[1] long
link[2] byte
TODO: Modifiers
count int
name string
val1 float
val2 float



#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entity ID Self | SignedVarLong |  |
|Runtime Entity ID | UnsignedVarLong |  |
|Entity Type | string |  |
|X | float |  |
|Y | float |  |
|Z | float |  |
|Speed X | float |  |
|Speed Y | float |  |
|Speed Z | float |  |
|Pitch | float |  |
|Yaw | float |  |
|Head Yaw | float |  |
|Body Yaw | float |  |
|Attributes | EntityAttributes |  |
|Metadata | MetadataDictionary |  |
-----------------------------------------------------------------------
### Remove Entity (0x0e)
Wiki: [Remove Entity](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-RemoveEntity)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entity ID Self | SignedVarLong |  |
-----------------------------------------------------------------------
### Add Item Entity (0x0f)
Wiki: [Add Item Entity](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AddItemEntity)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entity ID Self | SignedVarLong |  |
|Runtime Entity ID | UnsignedVarLong |  |
|Item | ItemInstance |  |
|X | float |  |
|Y | float |  |
|Z | float |  |
|Speed X | float |  |
|Speed Y | float |  |
|Speed Z | float |  |
|Metadata | MetadataDictionary |  |
|Is From Fishing | bool |  |
-----------------------------------------------------------------------
### Take Item Entity (0x11)
Wiki: [Take Item Entity](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-TakeItemEntity)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Target | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Move Entity (0x12)
Wiki: [Move Entity](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MoveEntity)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Flags | byte |  |
|Position | PlayerLocation |  |
-----------------------------------------------------------------------
### Move Player (0x13)
Wiki: [Move Player](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MovePlayer)

**Sent from server:** true  
**Sent from client:** true



#### Mode constants

| Name | Value |
|:-----|:-----|
|Normal | 0 |
|Reset | 1 |
|Teleport | 2 |
|Rotation | 3 |

#### Teleportcause constants

| Name | Value |
|:-----|:-----|
|Unknown | 0 |
|Projectile | 1 |
|Chorus Fruit | 2 |
|Command | 3 |
|Behavior | 4 |
|Count | 5 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|X | float |  |
|Y | float |  |
|Z | float |  |
|Pitch | float |  |
|Yaw | float |  |
|Head Yaw | float |  |
|Mode | byte |  |
|On Ground | bool |  |
|Other Runtime Entity ID | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Rider Jump (0x14)
Wiki: [Rider Jump](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-RiderJump)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Unknown | SignedVarInt |  |
-----------------------------------------------------------------------
### Update Block (0x15)
Wiki: [Update Block](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateBlock)

**Sent from server:** true  
**Sent from client:** false



#### Flags constants

| Name | Value |
|:-----|:-----|
|None | 0 |
|Neighbors | 1 |
|Network | 2 |
|Nographic | 4 |
|Priority | 8 |
|All | (Neighbors | Network) |
|All Priority | (All | Priority) |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Coordinates | BlockCoordinates |  |
|Block Runtime ID | UnsignedVarInt |  |
|Block Priority | UnsignedVarInt |  |
|Storage | UnsignedVarInt |  |
-----------------------------------------------------------------------
### Add Painting (0x16)
Wiki: [Add Painting](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AddPainting)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entity ID Self | SignedVarLong |  |
|Runtime Entity ID | UnsignedVarLong |  |
|Coordinates | Vector3 |  |
|Direction | SignedVarInt |  |
|Title | string |  |
-----------------------------------------------------------------------
### Tick Sync (0x17)
Wiki: [Tick Sync](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-TickSync)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Request Time | long |  |
|Response Time | long |  |
-----------------------------------------------------------------------
### Level Sound Event Old (0x18)
Wiki: [Level Sound Event Old](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LevelSoundEventOld)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Sound ID | byte |  |
|Position | Vector3 |  |
|Block Id | SignedVarInt |  |
|Entity Type | SignedVarInt |  |
|Is baby mob | bool |  |
|Is global | bool |  |
-----------------------------------------------------------------------
### Level Event (0x19)
Wiki: [Level Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LevelEvent)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Event ID | SignedVarInt |  |
|Position | Vector3 |  |
|Data | SignedVarInt |  |
-----------------------------------------------------------------------
### Block Event (0x1a)
Wiki: [Block Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-BlockEvent)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Coordinates | BlockCoordinates |  |
|Case 1 | SignedVarInt |  |
|Case 2 | SignedVarInt |  |
-----------------------------------------------------------------------
### Entity Event (0x1b)
Wiki: [Entity Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-EntityEvent)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Event ID | byte |  |
|Data | SignedVarInt |  |
-----------------------------------------------------------------------
### Mob Effect (0x1c)
Wiki: [Mob Effect](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MobEffect)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Event ID | byte |  |
|Effect ID | SignedVarInt |  |
|Amplifier | SignedVarInt |  |
|Particles | bool |  |
|Duration | SignedVarInt |  |
|Tick | UnsignedVarLong |  |
|Ambient | bool |  |
-----------------------------------------------------------------------
### Update Attributes (0x1d)
Wiki: [Update Attributes](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateAttributes)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Attributes | PlayerAttributes |  |
|Tick | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Inventory Transaction (0x1e)
Wiki: [Inventory Transaction](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-InventoryTransaction)

**Sent from server:** true  
**Sent from client:** true



#### Transaction Type constants

| Name | Value |
|:-----|:-----|
|Normal | 0 |
|Inventory Mismatch | 1 |
|Item Use | 2 |
|Item Use On Entity | 3 |
|Item Release | 4 |

#### Inventory Source Type constants

| Name | Value |
|:-----|:-----|
|Container | 0 |
|Global | 1 |
|World Interaction | 2 |
|Creative | 3 |
|Crafting | 100 |
|Unspecified | 99999 |

#### Crafting Action constants

| Name | Value |
|:-----|:-----|
|Craft Add Ingredient | -2 |
|Craft Remove Ingredient | -3 |
|Craft Result | -4 |
|Craft Use Ingredient | -5 |
|Anvil Input | -10 |
|Anvil Material | -11 |
|Anvil Result | -12 |
|Anvil Output | -13 |
|Enchant Item | -15 |
|Enchant Lapis | -16 |
|Enchant Result | -17 |
|Drop | -100 |

#### Item Release Action constants

| Name | Value |
|:-----|:-----|
|Release | 0 |
|Use | 1 |

#### Item Use Action constants

| Name | Value |
|:-----|:-----|
|Place, Clickblock | 0 |
|Use, Clickair | 1 |
|Destroy | 2 |

#### Item Use On Entity Action constants

| Name | Value |
|:-----|:-----|
|Interact | 0 |
|Attack | 1 |
|Item Interact | 2 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Transaction | Transaction |  |
-----------------------------------------------------------------------
### Mob Equipment (0x1f)
Wiki: [Mob Equipment](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MobEquipment)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Item | Item |  |
|Slot | byte |  |
|Selected Slot | byte |  |
|Windows Id | byte |  |
-----------------------------------------------------------------------
### Mob Armor Equipment (0x20)
Wiki: [Mob Armor Equipment](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MobArmorEquipment)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Helmet | Item |  |
|Chestplate | Item |  |
|Leggings | Item |  |
|Boots | Item |  |
|Body | Item |  |
-----------------------------------------------------------------------
### Interact (0x21)
Wiki: [Interact](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Interact)

**Sent from server:** true  
**Sent from client:** true



#### Actions constants

| Name | Value |
|:-----|:-----|
|Right Click | 1 |
|Left Click | 2 |
|Leave Vehicle | 3 |
|Mouse Over | 4 |
|Open Npc | 5 |
|Open Inventory | 6 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Action ID | byte |  |
|Target Runtime Entity ID | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Block Pick Request (0x22)
Wiki: [Block Pick Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-BlockPickRequest)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|X | SignedVarInt |  |
|Y | SignedVarInt |  |
|Z | SignedVarInt |  |
|Add User Data | bool |  |
|Selected Slot | byte |  |
-----------------------------------------------------------------------
### Entity Pick Request (0x23)
Wiki: [Entity Pick Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-EntityPickRequest)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | ulong |  |
|Selected Slot | byte |  |
|Add User Data | bool |  |
-----------------------------------------------------------------------
### Player Action (0x24)
Wiki: [Player Action](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerAction)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Action ID | SignedVarInt |  |
|Coordinates | BlockCoordinates |  |
|Result Coordinates | BlockCoordinates |  |
|Face | SignedVarInt |  |
-----------------------------------------------------------------------
### Hurt Armor (0x26)
Wiki: [Hurt Armor](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-HurtArmor)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Cause | SignedVarInt |  |
|Health | SignedVarInt |  |
|Armor slot flags | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Set Entity Data (0x27)
Wiki: [Set Entity Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetEntityData)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Metadata | MetadataDictionary |  |
-----------------------------------------------------------------------
### Set Entity Motion (0x28)
Wiki: [Set Entity Motion](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetEntityMotion)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Velocity | Vector3 |  |
-----------------------------------------------------------------------
### Set Entity Link (0x29)
Wiki: [Set Entity Link](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetEntityLink)

**Sent from server:** true  
**Sent from client:** false



#### Link Actions constants

| Name | Value |
|:-----|:-----|
|Remove | 0 |
|Ride | 1 |
|Passenger | 2 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Ridden ID | SignedVarLong |  |
|Rider ID | SignedVarLong |  |
|Link Type | byte |  |
|Immediate | bool |  |
|Rider Initiated | bool |  |
|Vehicle Angular Velocity | float |  |
-----------------------------------------------------------------------
### Set Health (0x2a)
Wiki: [Set Health](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetHealth)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Health | SignedVarInt |  |
-----------------------------------------------------------------------
### Set Spawn Position (0x2b)
Wiki: [Set Spawn Position](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetSpawnPosition)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Spawn Type | SignedVarInt |  |
|Coordinates | BlockCoordinates |  |
|Dimension | SignedVarInt |  |
|Unknown coordinates | BlockCoordinates |  |
-----------------------------------------------------------------------
### Animate (0x2c)
Wiki: [Animate](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Animate)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Action ID | byte |  |
|Runtime Entity ID | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Respawn (0x2d)
Wiki: [Respawn](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Respawn)

**Sent from server:** true  
**Sent from client:** true



#### Respawn State constants

| Name | Value |
|:-----|:-----|
|Search | 0 |
|Ready | 1 |
|Client Ready | 2 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|X | float |  |
|Y | float |  |
|Z | float |  |
|State | byte |  |
|Runtime Entity ID | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Container Open (0x2e)
Wiki: [Container Open](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ContainerOpen)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Window ID | byte |  |
|Type | byte |  |
|Coordinates | BlockCoordinates |  |
|Actor Unique ID | SignedVarLong |  |
-----------------------------------------------------------------------
### Container Close (0x2f)
Wiki: [Container Close](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ContainerClose)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Window ID | byte |  |
|Window Type | byte |  |
|Server | bool |  |
-----------------------------------------------------------------------
### Player Hotbar (0x30)
Wiki: [Player Hotbar](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerHotbar)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Selected Slot | UnsignedVarInt |  |
|Window ID | byte |  |
|Select Slot  | bool |  |
-----------------------------------------------------------------------
### Inventory Content (0x31)
Wiki: [Inventory Content](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-InventoryContent)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Inventory Id | UnsignedVarInt |  |
|Input | ItemStacks |  |
-----------------------------------------------------------------------
### Inventory Slot (0x32)
Wiki: [Inventory Slot](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-InventorySlot)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Inventory Id | UnsignedVarInt |  |
|Slot | UnsignedVarInt |  |
-----------------------------------------------------------------------
### Container Set Data (0x33)
Wiki: [Container Set Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ContainerSetData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Window ID | byte |  |
|Property | SignedVarInt |  |
|Value | SignedVarInt |  |
-----------------------------------------------------------------------
### Crafting Data (0x34)
Wiki: [Crafting Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CraftingData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Recipes | Recipes |  |
|Potion type recipes | PotionTypeRecipe[] |  |
|potion container recipes | PotionContainerChangeRecipe[] |  |
|Material reducer recipes | MaterialReducerRecipe[] |  |
|Is Clean | bool |  |
-----------------------------------------------------------------------
### Crafting Event (0x35)
Wiki: [Crafting Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CraftingEvent)

**Sent from server:** true  
**Sent from client:** true



#### Recipe Types constants

| Name | Value |
|:-----|:-----|
|Shapeless | 0 |
|Shaped | 1 |
|Furnace | 2 |
|Furnace Data | 3 |
|Multi | 4 |
|Shulker Box | 5 |
|Chemistry Shapeless | 6 |
|Chemistry Shaped | 7 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Window ID | byte |  |
|Recipe Type | SignedVarInt |  |
|Recipe ID | UUID |  |
|Input | ItemStacks |  |
|Result | ItemStacks |  |
-----------------------------------------------------------------------
### Gui Data Pick Item (0x36)
Wiki: [Gui Data Pick Item](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-GuiDataPickItem)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Item Name | string |  |
|Item Effects | string |  |
|Hotbar Slot | int |  |
-----------------------------------------------------------------------
### Block Entity Data (0x38)
Wiki: [Block Entity Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-BlockEntityData)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Coordinates | BlockCoordinates |  |
|NamedTag | Nbt |  |
-----------------------------------------------------------------------
### Level Chunk (0x3a)
Wiki: [Level Chunk](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LevelChunk)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Chunk X | SignedVarInt |  |
|Chunk Z | SignedVarInt |  |
|Dimension | SignedVarInt |  |
-----------------------------------------------------------------------
### Set Commands Enabled (0x3b)
Wiki: [Set Commands Enabled](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetCommandsEnabled)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Enabled | bool |  |
-----------------------------------------------------------------------
### Set Difficulty (0x3c)
Wiki: [Set Difficulty](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetDifficulty)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Difficulty | UnsignedVarInt |  |
-----------------------------------------------------------------------
### Change Dimension (0x3d)
Wiki: [Change Dimension](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ChangeDimension)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Dimension | SignedVarInt |  |
|Position | Vector3 |  |
|Respawn | bool |  |
-----------------------------------------------------------------------
### Set Player Game Type (0x3e)
Wiki: [Set Player Game Type](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetPlayerGameType)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Gamemode | SignedVarInt |  |
-----------------------------------------------------------------------
### Player List (0x3f)
Wiki: [Player List](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerList)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Records | PlayerRecords |  |
-----------------------------------------------------------------------
### Simple Event (0x40)
Wiki: [Simple Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SimpleEvent)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Event Type | ushort |  |
-----------------------------------------------------------------------
### Telemetry Event (0x41)
Wiki: [Telemetry Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-TelemetryEvent)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Event data | SignedVarInt |  |
|Event type | byte |  |
|Aux Data | byte[] | 0, true |
-----------------------------------------------------------------------
### Spawn Experience Orb (0x42)
Wiki: [Spawn Experience Orb](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SpawnExperienceOrb)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Position | Vector3 |  |
|Count | SignedVarInt |  |
-----------------------------------------------------------------------
### Clientbound Map Item Data  (0x43)
Wiki: [Clientbound Map Item Data ](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ClientboundMapItemData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|MapInfo | MapInfo |  |
-----------------------------------------------------------------------
### Map Info Request (0x44)
Wiki: [Map Info Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MapInfoRequest)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Map ID | SignedVarLong |  |
-----------------------------------------------------------------------
### Request Chunk Radius (0x45)
Wiki: [Request Chunk Radius](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-RequestChunkRadius)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Chunk Radius | SignedVarInt |  |
|Max Radius | byte |  |
-----------------------------------------------------------------------
### Chunk Radius Update (0x46)
Wiki: [Chunk Radius Update](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ChunkRadiusUpdate)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Chunk Radius | SignedVarInt |  |
-----------------------------------------------------------------------
### Game Rules Changed (0x48)
Wiki: [Game Rules Changed](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-GameRulesChanged)

**Sent from server:** true  
**Sent from client:** false

 0x47 ItemFrameDropItem removed from the protocol at 662 (Feb 2024); frame drops arrive via the block-attack path 


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Camera (0x49)
Wiki: [Camera](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Camera)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Unknown1 | SignedVarLong |  |
|Unknown2 | SignedVarLong |  |
-----------------------------------------------------------------------
### Boss Event (0x4a)
Wiki: [Boss Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-BossEvent)

**Sent from server:** true  
**Sent from client:** false



#### Type constants

| Name | Value |
|:-----|:-----|
|Add Boss | 0 |
|Add Player | 1 |
|Remove Boss | 2 |
|Remove Player | 3 |
|Update Progress | 4 |
|Update Name | 5 |
|Update Options | 6 |
|Update Style | 7 |
|Query | 8 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Boss Entity ID | SignedVarLong |  |
|Player ID | SignedVarLong |  |
|Event Type | byte |  |
|Title | string |  |
|Filtered Title | string |  |
|Health Percent | float |  |
|Color | byte |  |
|Overlay | byte |  |
-----------------------------------------------------------------------
### Show Credits (0x4b)
Wiki: [Show Credits](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ShowCredits)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Status | SignedVarInt |  |
-----------------------------------------------------------------------
### Available Commands (0x4c)
Wiki: [Available Commands](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AvailableCommands)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Command Request (0x4d)
Wiki: [Command Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CommandRequest)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Command | string |  |
|Origin | CommandOriginData |  |
|Is Internal | bool |  |
|Version | string |  |
-----------------------------------------------------------------------
### Command Block Update (0x4e)
Wiki: [Command Block Update](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CommandBlockUpdate)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Is Block | bool |  |
-----------------------------------------------------------------------
### Command Output (0x4f)
Wiki: [Command Output](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CommandOutput)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Update Trade (0x50)
Wiki: [Update Trade](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateTrade)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Window ID | byte |  |
|Window Type | byte |  |
|Size | SignedVarInt |  |
|Trade Tier | SignedVarInt |  |
|Trader Entity ID | SignedVarLong |  |
|Player Entity ID | SignedVarLong |  |
|Display Name | string |  |
|Use New Trade Screen | bool |  |
|Using Economy Trade | bool |  |
|NamedTag | Nbt |  |
-----------------------------------------------------------------------
### Update Equipment (0x51)
Wiki: [Update Equipment](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateEquipment)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Window ID | byte |  |
|Window Type | byte |  |
|Size | SignedVarInt |  |
|Entity ID | SignedVarLong |  |
|NamedTag | Nbt |  |
-----------------------------------------------------------------------
### Resource Pack Data Info (0x52)
Wiki: [Resource Pack Data Info](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ResourcePackDataInfo)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Package ID | string |  |
|Max Chunk Size | uint |  |
|Chunk Count | uint |  |
|Compressed Package Size | ulong |  |
|Hash | ByteArray |  |
|Is Premium | bool |  |
|Pack Type | byte |  |
-----------------------------------------------------------------------
### Resource Pack Chunk Data (0x53)
Wiki: [Resource Pack Chunk Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ResourcePackChunkData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Package ID | string |  |
|Chunk Index | uint |  |
|Progress | ulong |  |
|Payload | ByteArray |  |
-----------------------------------------------------------------------
### Resource Pack Chunk Request (0x54)
Wiki: [Resource Pack Chunk Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ResourcePackChunkRequest)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Package ID | string |  |
|Chunk Index | uint |  |
-----------------------------------------------------------------------
### Transfer (0x55)
Wiki: [Transfer](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-Transfer)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Server Address | string |  |
|Port | ushort |  |
|Reload World | bool |  |
-----------------------------------------------------------------------
### Play Sound (0x56)
Wiki: [Play Sound](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlaySound)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Name | string |  |
|Coordinates | BlockCoordinates |  |
|Volume | float |  |
|Pitch | float |  |
-----------------------------------------------------------------------
### Stop Sound (0x57)
Wiki: [Stop Sound](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-StopSound)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Name | string |  |
|Stop All | bool |  |
|Stop Music Legacy | bool |  |
-----------------------------------------------------------------------
### Set Title (0x58)
Wiki: [Set Title](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetTitle)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Type | SignedVarInt |  |
|Text | string |  |
|Fade In Time | SignedVarInt |  |
|Stay Time | SignedVarInt |  |
|Fade Out Time | SignedVarInt |  |
|Xuid | string |  |
|Platform Online Id | string |  |
|Filtered Title Text | string |  |
-----------------------------------------------------------------------
### Add Behavior Tree (0x59)
Wiki: [Add Behavior Tree](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AddBehaviorTree)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|BehaviorTree | string |  |
-----------------------------------------------------------------------
### Structure Block Update (0x5a)
Wiki: [Structure Block Update](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-StructureBlockUpdate)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Show Store Offer (0x5b)
Wiki: [Show Store Offer](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ShowStoreOffer)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Offer Id | UUID |  |
|Redirect Type | byte |  |
-----------------------------------------------------------------------
### Purchase Receipt (0x5c)
Wiki: [Purchase Receipt](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PurchaseReceipt)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Player Skin (0x5d)
Wiki: [Player Skin](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerSkin)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|UUID | UUID |  |
|Skin | Skin |  |
|Skin Name | string |  |
|Old Skin Name | string |  |
|is Verified | bool |  |
-----------------------------------------------------------------------
### Sub Client Login (0x5e)
Wiki: [Sub Client Login](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SubClientLogin)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Connection Request | ByteArray |  |
-----------------------------------------------------------------------
### Initiate Web Socket Connection (0x5f)
Wiki: [Initiate Web Socket Connection](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-InitiateWebSocketConnection)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Server | string |  |
-----------------------------------------------------------------------
### Set Last Hurt By (0x60)
Wiki: [Set Last Hurt By](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetLastHurtBy)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Unknown | VarInt |  |
-----------------------------------------------------------------------
### Book Edit (0x61)
Wiki: [Book Edit](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-BookEdit)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Inventory Slot | SignedVarInt |  |
|Type | UnsignedVarInt |  |
-----------------------------------------------------------------------
### Npc Request (0x62)
Wiki: [Npc Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-NpcRequest)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Unknown0 | byte |  |
|Unknown1 | string |  |
|Unknown2 | byte |  |
|Scene Name | string |  |
-----------------------------------------------------------------------
### Photo Transfer (0x63)
Wiki: [Photo Transfer](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PhotoTransfer)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|File name | string |  |
|Image data | string |  |
|Unknown2 | string |  |
|Type | byte |  |
|Source Type | byte |  |
|Owner Unique ID | long |  |
|New Photo Name | string |  |
-----------------------------------------------------------------------
### Modal Form Request (0x64)
Wiki: [Modal Form Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ModalFormRequest)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Form Id | UnsignedVarInt |  |
|Data | string |  |
-----------------------------------------------------------------------
### Modal Form Response (0x65)
Wiki: [Modal Form Response](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ModalFormResponse)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Form Id | UnsignedVarInt |  |
-----------------------------------------------------------------------
### Server Settings Request (0x66)
Wiki: [Server Settings Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ServerSettingsRequest)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Server Settings Response (0x67)
Wiki: [Server Settings Response](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ServerSettingsResponse)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Form Id | UnsignedVarInt |  |
|Data | string |  |
-----------------------------------------------------------------------
### Show Profile (0x68)
Wiki: [Show Profile](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ShowProfile)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|XUID | string |  |
-----------------------------------------------------------------------
### Set Default Game Type (0x69)
Wiki: [Set Default Game Type](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetDefaultGameType)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Gamemode | SignedVarInt |  |
-----------------------------------------------------------------------
### Remove Objective (0x6a)
Wiki: [Remove Objective](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-RemoveObjective)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Objective Name | string |  |
-----------------------------------------------------------------------
### Set Display Objective (0x6b)
Wiki: [Set Display Objective](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetDisplayObjective)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Display Slot | string |  |
|Objective Name | string |  |
|Display Name | string |  |
|Criteria Name | string |  |
|Sort Order | SignedVarInt |  |
-----------------------------------------------------------------------
### Set Score (0x6c)
Wiki: [Set Score](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetScore)

**Sent from server:** true  
**Sent from client:** false



#### Types constants

| Name | Value |
|:-----|:-----|
|Change | 0 |
|Remove | 1 |

#### Change Types constants

| Name | Value |
|:-----|:-----|
|Player | 1 |
|Entity | 2 |
|Fake Player | 3 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entries | ScoreEntries |  |
-----------------------------------------------------------------------
### Lab Table (0x6d)
Wiki: [Lab Table](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LabTable)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Useless Byte | byte |  |
|Lab Table X | SignedVarInt |  |
|Lab Table Y | SignedVarInt |  |
|Lab Table Z | SignedVarInt |  |
|Reaction Type | byte |  |
-----------------------------------------------------------------------
### Update Block Synced (0x6e)
Wiki: [Update Block Synced](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateBlockSynced)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Coordinates | BlockCoordinates |  |
|Block Runtime ID | UnsignedVarInt |  |
|Block Priority | UnsignedVarInt |  |
|Data Layer ID | UnsignedVarInt |  |
|Unknown0 | UnsignedVarLong |  |
|Unknown1 | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Move Entity Delta (0x6f)
Wiki: [Move Entity Delta](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MoveEntityDelta)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
|Flags | ushort |  |
-----------------------------------------------------------------------
### Set Scoreboard Identity (0x70)
Wiki: [Set Scoreboard Identity](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetScoreboardIdentity)

**Sent from server:** true  
**Sent from client:** false



#### Operations constants

| Name | Value |
|:-----|:-----|
|Register Identity | 0 |
|Clear Identity | 1 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entries | ScoreboardIdentityEntries |  |
-----------------------------------------------------------------------
### Set Local Player As Initialized (0x71)
Wiki: [Set Local Player As Initialized](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetLocalPlayerAsInitialized)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Update Soft Enum (0x72)
Wiki: [Update Soft Enum](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateSoftEnum)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Network Stack Latency (0x73)
Wiki: [Network Stack Latency](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-NetworkStackLatency)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Timestamp | ulong |  |
|Unknown Flag | byte |  |
-----------------------------------------------------------------------
### Script Custom Event (0x75)
Wiki: [Script Custom Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ScriptCustomEvent)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Event Name | string |  |
|Event Data | string |  |
-----------------------------------------------------------------------
### Spawn Particle Effect (0x76)
Wiki: [Spawn Particle Effect](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SpawnParticleEffect)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Dimension ID | byte |  |
|Entity ID | SignedVarLong |  |
|Position | Vector3 |  |
|Particle name | string |  |
-----------------------------------------------------------------------
### Available Entity Identifiers (0x77)
Wiki: [Available Entity Identifiers](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-AvailableEntityIdentifiers)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|NamedTag | Nbt |  |
-----------------------------------------------------------------------
### Level Sound Event V2 (0x78)
Wiki: [Level Sound Event V2](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LevelSoundEventV2)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Sound ID | byte |  |
|Position | Vector3 |  |
|Block Id | SignedVarInt |  |
|Entity Type | string |  |
|Is baby mob | bool |  |
|Is global | bool |  |
-----------------------------------------------------------------------
### Network Chunk Publisher Update (0x79)
Wiki: [Network Chunk Publisher Update](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-NetworkChunkPublisherUpdate)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Coordinates | BlockCoordinates |  |
|Radius | UnsignedVarInt |  |
-----------------------------------------------------------------------
### Biome Definition List (0x7a)
Wiki: [Biome Definition List](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-BiomeDefinitionList)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Level Sound Event (0x7b)
Wiki: [Level Sound Event](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LevelSoundEvent)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Sound ID | string |  |
|Position | Vector3 |  |
|Block Id | SignedVarInt |  |
|Entity Type | string |  |
|Is baby mob | bool |  |
|Is global | bool |  |
-----------------------------------------------------------------------
### Level Event Generic (0x7c)
Wiki: [Level Event Generic](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LevelEventGeneric)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Event ID | SignedVarInt |  |
|Event Data | NbtBody |  |
-----------------------------------------------------------------------
### Lectern Update (0x7d)
Wiki: [Lectern Update](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LecternUpdate)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Page | byte |  |
|Total Pages | byte |  |
|Block Position | BlockCoordinates |  |
-----------------------------------------------------------------------
### Video Stream Connect (0x7e)
Wiki: [Video Stream Connect](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-VideoStreamConnect)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Server URI | string |  |
|Frame Send Frequency | float |  |
|Action | byte |  |
|Resolution X | int |  |
|Resolution Y | int |  |
-----------------------------------------------------------------------
### Client Cache Status (0x81)
Wiki: [Client Cache Status](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ClientCacheStatus)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Enabled | bool |  |
-----------------------------------------------------------------------
### On Screen Texture Animation (0x82)
Wiki: [On Screen Texture Animation](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-OnScreenTextureAnimation)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Effect Id | uint |  |
-----------------------------------------------------------------------
### Map Create Locked Copy (0x83)
Wiki: [Map Create Locked Copy](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-MapCreateLockedCopy)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Original Map Id | SignedVarLong |  |
|New Map Id | SignedVarLong |  |
-----------------------------------------------------------------------
### Structure Template Data Export Request (0x84)
Wiki: [Structure Template Data Export Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-StructureTemplateDataExportRequest)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Name | string |  |
|Position | BlockCoordinates |  |
|Settings | StructureSettings |  |
|Request Type | byte |  |
-----------------------------------------------------------------------
### Structure Template Data Export Response (0x85)
Wiki: [Structure Template Data Export Response](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-StructureTemplateDataExportResponse)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Name | string |  |
-----------------------------------------------------------------------
### Update Block Properties (0x86)
Wiki: [Update Block Properties](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateBlockProperties)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|NamedTag | Nbt |  |
-----------------------------------------------------------------------
### Client Cache Blob Status (0x87)
Wiki: [Client Cache Blob Status](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ClientCacheBlobStatus)

**Sent from server:** false  
**Sent from client:** true

 The client sends this to report which blobs it already holds, so the handler belongs on
     the server. It was declared the other way round, which put the handler on the client and
     left the server with no way to receive it at all. 


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Client Cache Miss Response (0x88)
Wiki: [Client Cache Miss Response](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ClientCacheMissResponse)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Network Settings (0x8f)
Wiki: [Network Settings](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-NetworkSettings)

**Sent from server:** true  
**Sent from client:** false



#### Compressionalgorithm constants

| Name | Value |
|:-----|:-----|
|Zlib | 0 |
|Snappy | 1 |
|None | 65535 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Compression threshold | ushort |  |
|Compression algorithm | ushort |  |
|Client throttle enabled | bool |  |
|Client throttle threshold | byte |  |
|Client throttle scalar | float |  |
-----------------------------------------------------------------------
### Player Auth Input (0x90)
Wiki: [Player Auth Input](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerAuthInput)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Creative Content (0x91)
Wiki: [Creative Content](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CreativeContent)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Player Enchant Options (0x92)
Wiki: [Player Enchant Options](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerEnchantOptions)

**Sent from server:** true  
**Sent from client:** false


public const PLAYER_AUTH_INPUT_PACKET = 0x90;
public const PLAYER_ARMOR_DAMAGE_PACKET = 0x95;
public const CODE_BUILDER_PACKET = 0x96;
public const UPDATE_PLAYER_GAME_TYPE_PACKET = 0x97;
public const EMOTE_LIST_PACKET = 0x98;
public const POSITION_TRACKING_D_B_SERVER_BROADCAST_PACKET = 0x99;
public const POSITION_TRACKING_D_B_CLIENT_REQUEST_PACKET = 0x9a;
public const DEBUG_INFO_PACKET = 0x9b;



#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Enchant options | EnchantOptions |  |
-----------------------------------------------------------------------
### Item Stack Request (0x93)
Wiki: [Item Stack Request](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ItemStackRequest)

**Sent from server:** false  
**Sent from client:** true



#### Action Type constants

| Name | Value |
|:-----|:-----|
|Take | 0 |
|Place | 1 |
|Swap | 2 |
|Drop | 3 |
|Destroy | 4 |
|Consume | 5 |
|Create | 6 |
|Place Into Bundle | 7 |
|Take From Bundle | 8 |
|Lab Table Combine | 9 |
|Beacon Payment | 10 |
|Mine Block | 11 |
|Craft Recipe | 12 |
|Craft Recipe Auto | 13 |
|Craft Creative | 14 |
|Craft Recipe Optional | 15 |
|Craft Grindstone | 16 |
|Craft Loom | 17 |
|Craft Not Implemented Deprecated | 18 |
|Craft Results Deprecated | 19 |


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Requests | ItemStackRequests |  |
-----------------------------------------------------------------------
### Item Stack Response (0x94)
Wiki: [Item Stack Response](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ItemStackResponse)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Responses | ItemStackResponses |  |
-----------------------------------------------------------------------
### Update Player Game Type (0x97)
Wiki: [Update Player Game Type](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdatePlayerGameType)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Player Game Type | SignedVarInt |  |
|Target Player Unique ID | SignedVarLong |  |
|Tick | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Emote List (0x98)
Wiki: [Emote List](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-EmoteList)

**Sent from server:** false  
**Sent from client:** true

 Sent by the client right after login, listing the emotes it owns so the server can
     validate an Emote packet later. The piece id list is a UUID array, which the generator has
     no type for, so it lives in the partial (Net/McpeEmoteList.cs). 


#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Runtime Entity ID | UnsignedVarLong |  |
-----------------------------------------------------------------------
### Packet Violation Warning (0x9c)
Wiki: [Packet Violation Warning](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PacketViolationWarning)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Violation Type | SignedVarInt |  |
|Severity | SignedVarInt |  |
|Packet Id | SignedVarInt |  |
|Reason | string |  |
-----------------------------------------------------------------------
### Player Fog (0xa0)
Wiki: [Player Fog](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerFog)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Correct Player Move Prediction (0xa1)
Wiki: [Correct Player Move Prediction](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CorrectPlayerMovePrediction)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Prediction Type | byte |  |
|Position | Vector3 |  |
|Delta | Vector3 |  |
|Rotation Pitch | float |  |
|Rotation Yaw | float |  |
-----------------------------------------------------------------------
### Item Component (0xa2)
Wiki: [Item Component](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ItemComponent)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entries | ItemComponentList |  |
-----------------------------------------------------------------------
### Filter Text Packet (0xa3)
Wiki: [Filter Text Packet](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-FilterTextPacket)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Text | string |  |
|From server | bool |  |
-----------------------------------------------------------------------
### Sync Entity Property (0xa5)
Wiki: [Sync Entity Property](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SyncEntityProperty)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|NamedTag | Nbt |  |
-----------------------------------------------------------------------
### Update Sub Chunk Blocks Packet (0xac)
Wiki: [Update Sub Chunk Blocks Packet](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateSubChunkBlocksPacket)

**Sent from server:** true  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Subchunk coordinates | BlockCoordinates |  |
|Layer zero updates | UpdateSubChunkBlocksPacketEntry[] |  |
|Layer one updates | UpdateSubChunkBlocksPacketEntry[] |  |
-----------------------------------------------------------------------
### Sub Chunk Packet (0xae)
Wiki: [Sub Chunk Packet](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SubChunkPacket)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Cache enabled | bool |  |
|Dimension | SignedVarInt |  |
|Origin X | SignedVarInt |  |
|Origin Y | SignedVarInt |  |
|Origin Z | SignedVarInt |  |
-----------------------------------------------------------------------
### Sub Chunk Request Packet (0xaf)
Wiki: [Sub Chunk Request Packet](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SubChunkRequestPacket)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Dimension Data (0xb4)
Wiki: [Dimension Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-DimensionData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Definitions | DimensionDefinitions |  |
-----------------------------------------------------------------------
### Update Abilities (0xbb)
Wiki: [Update Abilities](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateAbilities)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Entity Unique ID | long |  |
|Permission Level | byte |  |
|Command Permission | byte |  |
-----------------------------------------------------------------------
### Update Adventure Settings (0xbc)
Wiki: [Update Adventure Settings](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-UpdateAdventureSettings)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|No PvM | bool |  |
|No MvP | bool |  |
|Immutable World | bool |  |
|Show Name Tags | bool |  |
|Auto Jump | bool |  |
-----------------------------------------------------------------------
### Request Network Settings (0xc1)
Wiki: [Request Network Settings](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-RequestNetworkSettings)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Protocol Version | int |  |
-----------------------------------------------------------------------
### Camera Presets (0xc6)
Wiki: [Camera Presets](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CameraPresets)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Trim Data (0x12e)
Wiki: [Trim Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-TrimData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Set Player Inventory Options (0x133)
Wiki: [Set Player Inventory Options](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SetPlayerInventoryOptions)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Left Tab | SignedVarInt |  |
|Right Tab | SignedVarInt |  |
|Filtering | bool |  |
|Layout | SignedVarInt |  |
|Crafting Layout | SignedVarInt |  |
-----------------------------------------------------------------------
### Server Bound Loading Screen (0x138)
Wiki: [Server Bound Loading Screen](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ServerBoundLoadingScreen)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Type | SignedVarInt |  |
-----------------------------------------------------------------------
### Jigsaw Structure Data (0x139)
Wiki: [Jigsaw Structure Data](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-JigsawStructureData)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Structure Data | Nbt |  |
-----------------------------------------------------------------------
### Current Structure Feature (0x13a)
Wiki: [Current Structure Feature](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CurrentStructureFeature)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Current Feature | string |  |
-----------------------------------------------------------------------
### Server Bound Diagnostics (0x13b)
Wiki: [Server Bound Diagnostics](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ServerBoundDiagnostics)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Average Frames Per Second | float |  |
|Average Server Sim Tick Time | float |  |
|Average Client Sim Tick Time | float |  |
|Average Begin Frame Time | float |  |
|Average Input Time | float |  |
|Average Render Time | float |  |
|Average End Frame Time | float |  |
|Average Remainder Time Percent | float |  |
|Average Unaccounted Time Percent | float |  |
-----------------------------------------------------------------------
### Client Camera Aim Assist (0x141)
Wiki: [Client Camera Aim Assist](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-ClientCameraAimAssist)

**Sent from server:** false  
**Sent from client:** true




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Preset ID | string |  |
|Action | byte |  |
|Allow Aim Assist | bool |  |
-----------------------------------------------------------------------
### Camera Aim Assist Presets (0x140)
Wiki: [Camera Aim Assist Presets](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CameraAimAssistPresets)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Player Location (0x146)
Wiki: [Player Location](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-PlayerLocation)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Type | int |  |
|Entity Unique ID | SignedVarLong |  |
-----------------------------------------------------------------------
### Voxel Shapes (0x151)
Wiki: [Voxel Shapes](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-VoxelShapes)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Camera Spline (0x152)
Wiki: [Camera Spline](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CameraSpline)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Camera Instruction (0x12c)
Wiki: [Camera Instruction](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CameraInstruction)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Camera Shake (0x9f)
Wiki: [Camera Shake](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-CameraShake)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Intensity | float |  |
|Duration | float |  |
|Type | byte |  |
|Action | byte |  |
-----------------------------------------------------------------------
### Locator Bar (0x155)
Wiki: [Locator Bar](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-LocatorBar)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
-----------------------------------------------------------------------
### Sync World Clocks (0x158)
Wiki: [Sync World Clocks](https://github.com/NiclasOlofsson/MiNET/wiki//Protocol-SyncWorldClocks)

**Sent from server:** true  
**Sent from client:** false




#### Fields

| Name | Type | Size |
|:-----|:-----|:-----|
|Payload Type | UnsignedVarInt |  |
-----------------------------------------------------------------------


