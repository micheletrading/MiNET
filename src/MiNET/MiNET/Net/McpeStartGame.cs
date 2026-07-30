using System;
using System.Numerics;
using fNbt;
using log4net;
using MiNET.Utils;
using MiNET.Utils.Nbt;

namespace MiNET.Net
{
	public class SpawnSettings
	{
		public short BiomeType { get; set; }
		public string BiomeName { get; set; }
		public int Dimension { get; set; }
	}

	public class LevelSettings
	{
			public long seed; // = null;
			public SpawnSettings spawnSettings;

    		public int generator; // = null;
    		public int gamemode; // = null;
    		public bool hardcore; // = null;
    		public int difficulty; // = null;
    		public int x; // = null;
    		public int y; // = null;
    		public int z; // = null;
    		public bool hasAchievementsDisabled; // = null;
    		public int editorWorldType; // = null;
    		public bool createdInEditor; // = null;
    		public bool exportedFromEditor; // = null;
    		public int time; // = null; // day_cycle_stop_time
    		public int eduOffer; // = null;
    		public bool hasEduFeaturesEnabled; // = null;
    		public string eduProductUuid; // = null;
    		public float rainLevel; // = null;
    		public float lightningLevel; // = null;
    		public bool hasConfirmedPlatformLockedContent; // = null;
    		public bool isMultiplayer; // = null;
    		public bool broadcastToLan; // = null;
    		public int xboxLiveBroadcastMode; // = null;
    		public int platformBroadcastMode; // = null;
    		public bool enableCommands; // = null;
    		public bool isTexturepacksRequired; // = null;
    		public GameRules gamerules; // = null;
    		public Experiments experiments;
    		public bool bonusChest; // = null;
    		public bool mapEnabled; // = null;
    		public int permissionLevel; // = null;
    		public int serverChunkTickRange; // = null;
    		public bool hasLockedBehaviorPack; // = null;
    		public bool hasLockedResourcePack; // = null;
    		public bool isFromLockedWorldTemplate; // = null;
    		public bool useMsaGamertagsOnly; // = null;
    		public bool isFromWorldTemplate; // = null;
    		public bool isWorldTemplateOptionLocked; // = null;
    		public bool onlySpawnV1Villagers; // = null;
    		public bool personaDisabled; // = null;
    		public bool customSkinsDisabled; // = null;
    		public bool emoteChatMuted; // = null;
    		public string gameVersion; // = null;
    		public int limitedWorldWidth; // = null;
    		public int limitedWorldLength; // = null;
    		public bool isNewNether; // = null;
    		public EducationUriResource eduSharedUriResource = null;
    		public bool experimentalGameplayOverride; // = null;
    		public byte chatRestrictionLevel; // = null;
    		public bool disablePlayerInteractions; // = null;
    		public int serverEditorConnectionPolicy; // = null;
    		public bool allowAnonymousBlockDropsInEditorWorlds; // = null;

		public void Write(Packet packet)
		{
			packet.Write(unchecked((ulong) seed));

			var s = spawnSettings ?? new SpawnSettings();
			packet.Write(s.BiomeType);
			packet.Write(s.BiomeName);
			packet.WriteSignedVarInt(s.Dimension);

			packet.WriteSignedVarInt(generator);
			packet.WriteSignedVarInt(gamemode);
			packet.Write(hardcore);
			packet.WriteSignedVarInt(difficulty);

			packet.WriteSignedVarInt(x);
			packet.WriteSignedVarInt(y);
			packet.WriteSignedVarInt(z);

			packet.Write(hasAchievementsDisabled);
			packet.WriteSignedVarInt(editorWorldType);
			packet.Write(createdInEditor);
			packet.Write(exportedFromEditor);
			packet.WriteSignedVarInt(time);
			packet.WriteSignedVarInt(eduOffer);
			packet.Write(hasEduFeaturesEnabled);
			packet.Write(eduProductUuid);
			packet.Write(rainLevel);
			packet.Write(lightningLevel);
			packet.Write(hasConfirmedPlatformLockedContent);
			packet.Write(isMultiplayer);
			packet.Write(broadcastToLan);
			packet.WriteVarInt(xboxLiveBroadcastMode);
			packet.WriteVarInt(platformBroadcastMode);
			packet.Write(enableCommands);
			packet.Write(isTexturepacksRequired);
			packet.Write(gamerules);
			packet.Write(experiments);
			packet.Write(false); // experiments_previously_used
			packet.Write(bonusChest);
			packet.Write(mapEnabled);
			packet.Write((byte) permissionLevel);
			packet.Write(serverChunkTickRange);
			packet.Write(hasLockedBehaviorPack);
			packet.Write(hasLockedResourcePack);
			packet.Write(isFromLockedWorldTemplate);
			packet.Write(useMsaGamertagsOnly);
			packet.Write(isFromWorldTemplate);
			packet.Write(isWorldTemplateOptionLocked);
			packet.Write(onlySpawnV1Villagers);
			packet.Write(personaDisabled);
			packet.Write(customSkinsDisabled);
			packet.Write(emoteChatMuted);
			packet.Write(gameVersion);
			packet.Write(limitedWorldWidth);
			packet.Write(limitedWorldLength);
			packet.Write(isNewNether);
			packet.Write(eduSharedUriResource ?? new EducationUriResource("", ""));
			packet.Write(experimentalGameplayOverride);
			packet.Write(chatRestrictionLevel);
			packet.Write(disablePlayerInteractions);
			packet.WriteSignedVarInt(serverEditorConnectionPolicy);
			packet.Write(allowAnonymousBlockDropsInEditorWorlds);
		}

		public void Read(Packet packet)
		{
			seed = unchecked((long) packet.ReadUlong());

			spawnSettings = new SpawnSettings();
			spawnSettings.BiomeType = packet.ReadShort();
			spawnSettings.BiomeName = packet.ReadString();
			spawnSettings.Dimension = packet.ReadSignedVarInt();

			generator = packet.ReadSignedVarInt();
			gamemode = packet.ReadSignedVarInt();
			hardcore = packet.ReadBool();
			difficulty = packet.ReadSignedVarInt();

			x = packet.ReadSignedVarInt();
			y = packet.ReadSignedVarInt();
			z = packet.ReadSignedVarInt();

			hasAchievementsDisabled = packet.ReadBool();
			editorWorldType = packet.ReadSignedVarInt();
			createdInEditor = packet.ReadBool();
			exportedFromEditor = packet.ReadBool();
			time = packet.ReadSignedVarInt();
			eduOffer = packet.ReadSignedVarInt();
			hasEduFeaturesEnabled = packet.ReadBool();
			eduProductUuid = packet.ReadString();
			rainLevel = packet.ReadFloat();
			lightningLevel = packet.ReadFloat();
			hasConfirmedPlatformLockedContent = packet.ReadBool();
			isMultiplayer = packet.ReadBool();
			broadcastToLan = packet.ReadBool();
			xboxLiveBroadcastMode = packet.ReadVarInt();
			platformBroadcastMode = packet.ReadVarInt();
			enableCommands = packet.ReadBool();
			isTexturepacksRequired = packet.ReadBool();
			gamerules = packet.ReadGameRules();
			experiments = packet.ReadExperiments();
			packet.ReadBool(); // experiments_previously_used
			bonusChest = packet.ReadBool();
			mapEnabled = packet.ReadBool();
			permissionLevel = packet.ReadByte();
			serverChunkTickRange = packet.ReadInt();
			hasLockedBehaviorPack = packet.ReadBool();
			hasLockedResourcePack = packet.ReadBool();
			isFromLockedWorldTemplate = packet.ReadBool();
			useMsaGamertagsOnly = packet.ReadBool();
			isFromWorldTemplate = packet.ReadBool();
			isWorldTemplateOptionLocked = packet.ReadBool();
			onlySpawnV1Villagers = packet.ReadBool();
			personaDisabled = packet.ReadBool();
			customSkinsDisabled = packet.ReadBool();
			emoteChatMuted = packet.ReadBool();
			gameVersion = packet.ReadString();
			limitedWorldWidth = packet.ReadInt();
			limitedWorldLength = packet.ReadInt();
			isNewNether = packet.ReadBool();
			eduSharedUriResource = packet.ReadEducationUriResource();
			experimentalGameplayOverride = packet.ReadBool();
			chatRestrictionLevel = packet.ReadByte();
			disablePlayerInteractions = packet.ReadBool();
			serverEditorConnectionPolicy = packet.ReadSignedVarInt();
			allowAnonymousBlockDropsInEditorWorlds = packet.ReadBool();
		}
	}

	public partial class McpeStartGame : Packet<McpeStartGame>
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(McpeStartGame));

		public long entityIdSelf; // = null;
		public long runtimeEntityId; // = null;
		public int playerGamemode; // = null;
		public Vector3 spawn; // = null;
		public Vector2 rotation; // = null;

		public string levelId; // = null;
		public string worldName; // = null;
		public string premiumWorldTemplateId; // = null;
		public bool isTrial; // = null;
		public int movementRewindHistorySize; // = null; // rewind_history_size
		public bool enableNewBlockBreakSystem; // = null; // server_authoritative_block_breaking
		public long currentTick; // = null;
		public int enchantmentSeed; // = null;
		public BlockPalette blockPalette; // = null;
		public string multiplayerCorrelationId; // = null;
		public bool enableNewInventorySystem; // = null; // server_authoritative_inventory
		public string serverVersion; // = null; // engine
		public Nbt propertyData; // = null;
		public ulong blockPaletteChecksum;
		public UUID worldTemplateId; // = null;
		public bool clientSideGeneration; // = null;
		public bool blockNetworkIdsAreHashes; // = null;
		public bool serverControlledSound; // = null;
		public bool isChatLogging; // = null;
		public bool hasServerJoinInfo; // = null;
		public string serverIdentifier; // = null;
		public string scenarioIdentifier; // = null;
		public string worldIdentifier; // = null;
		public string ownerIdentifier; // = null;

		public LevelSettings levelSettings = new LevelSettings();

		partial void AfterEncode()
		{
			WriteSignedVarLong(entityIdSelf);
			WriteUnsignedVarLong(runtimeEntityId);
			WriteSignedVarInt(playerGamemode);
			Write(spawn);
			Write(rotation);

			LevelSettings s = levelSettings ?? new LevelSettings();
			s.Write(this);

			Write(levelId);
			Write(worldName);
			Write(premiumWorldTemplateId);
			Write(isTrial);

			WriteSignedVarInt(movementRewindHistorySize);
			Write(enableNewBlockBreakSystem);

			Write(unchecked((ulong) currentTick));
			WriteSignedVarInt(enchantmentSeed);

			Write(blockPalette);

			Write(multiplayerCorrelationId);
			Write(enableNewInventorySystem);
			Write(serverVersion);

			var pd = propertyData ?? new Nbt {NbtFile = new NbtFile(new NbtCompound("")) {BigEndian = false, UseVarInt = true}};
			pd.NbtFile.UseVarInt = true;
			Write(pd);

			Write(blockPaletteChecksum);
			Write(worldTemplateId ?? new UUID(new byte[16]));
			Write(clientSideGeneration);
			Write(blockNetworkIdsAreHashes);
			Write(serverControlledSound);
			Write(isChatLogging);

			Write(hasServerJoinInfo);
			if (hasServerJoinInfo)
			{
				WriteServerJoinInfo();
			}

			Write(serverIdentifier);
			Write(scenarioIdentifier);
			Write(worldIdentifier);
			Write(ownerIdentifier);
		}

		partial void AfterDecode()
		{
			entityIdSelf = ReadSignedVarLong();
			runtimeEntityId = ReadUnsignedVarLong();
			playerGamemode = ReadSignedVarInt();
			spawn = ReadVector3();
			rotation = ReadVector2();

			levelSettings = new LevelSettings();
			levelSettings.Read(this);

			levelId = ReadString();
			worldName = ReadString();
			premiumWorldTemplateId = ReadString();
			isTrial = ReadBool();

			movementRewindHistorySize = ReadSignedVarInt();
			enableNewBlockBreakSystem = ReadBool();

			currentTick = unchecked((long) ReadUlong());
			enchantmentSeed = ReadSignedVarInt();

			try
			{
				blockPalette = ReadBlockPalette();
			}
			catch (Exception ex)
			{
				Log.Warn($"Failed to read complete blockpallete", ex);
				return;
			}

			multiplayerCorrelationId = ReadString();
			enableNewInventorySystem = ReadBool();
			serverVersion = ReadString();
			propertyData = ReadNbt();
			blockPaletteChecksum = ReadUlong();
			worldTemplateId = ReadUUID();
			clientSideGeneration = ReadBool();
			blockNetworkIdsAreHashes = ReadBool();
			serverControlledSound = ReadBool();
			isChatLogging = ReadBool();

			hasServerJoinInfo = ReadBool();
			if (hasServerJoinInfo)
			{
				ReadServerJoinInfo();
			}

			serverIdentifier = ReadString();
			scenarioIdentifier = ReadString();
			worldIdentifier = ReadString();
			ownerIdentifier = ReadString();
		}

		/// <summary>
		///     Reads the (currently unused by MiNET) server_join_info container so the remainder of the
		///     packet stays in sync. Vanilla BDS 1.26.34 DOES send it with all three optional
		///     sub-blocks absent (verified live 2026-07-30); the sub-block content is Realms
		///     gathering features.
		/// </summary>
		private void ReadServerJoinInfo()
		{
			bool hasGatheringInfo = ReadBool();
			if (hasGatheringInfo)
			{
				ReadUUID(); // experience_id
				ReadString(); // experience_name
				ReadUUID(); // experience_world_id
				ReadString(); // experience_world_name
				ReadString(); // creator_id
				ReadUUID(); // target_id
				ReadString(); // scenario_id
				ReadString(); // server_id
			}

			if (ReadBool()) // store_entry_point_info
			{
				ReadString(); // store_id
				ReadString(); // store_name
			}

			if (ReadBool()) // presence_info
			{
				if (ReadBool()) ReadString(); // experience_name
				if (ReadBool()) ReadString(); // world_name
				if (ReadBool()) ReadString(); // rich_presence_id
			}
		}

		private void WriteServerJoinInfo()
		{
			Write(false); // has_gathering_info
			Write(false); // store_entry_point_info present
			Write(false); // presence_info present
		}

		/// <inheritdoc />
		public override void Reset()
		{
			entityIdSelf=default(long);
			runtimeEntityId=default(long);
			playerGamemode=default(int);
			spawn=default(Vector3);
			rotation=default(Vector2);
			levelSettings = default;
			levelId=default(string);
			worldName=default(string);
			premiumWorldTemplateId=default(string);
			isTrial=default(bool);
			movementRewindHistorySize=default(int);
			enableNewBlockBreakSystem=default(bool);
			currentTick=default(long);
			enchantmentSeed=default(int);
			blockPalette=default(BlockPalette);
			multiplayerCorrelationId=default(string);
			enableNewInventorySystem=default(bool);
			serverVersion=default(string);
			propertyData=default(Nbt);
			blockPaletteChecksum=default(ulong);
			worldTemplateId=default(UUID);
			clientSideGeneration=default(bool);
			blockNetworkIdsAreHashes=default(bool);
			serverControlledSound=default(bool);
			isChatLogging=default(bool);
			hasServerJoinInfo=default(bool);
			serverIdentifier=default(string);
			scenarioIdentifier=default(string);
			worldIdentifier=default(string);
			ownerIdentifier=default(string);

			base.Reset();
		}
	}
}
