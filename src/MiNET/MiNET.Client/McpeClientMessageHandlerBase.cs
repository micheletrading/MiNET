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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2020 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using Jose;
using log4net;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.Cryptography;
using MiNET.Utils.Vectors;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace MiNET.Client
{
	public abstract class McpeClientMessageHandlerBase : IMcpeClientMessageHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(McpeClientMessageHandlerBase));

		public MiNetClient Client { get; }

		public McpeClientMessageHandlerBase(MiNetClient client)
		{
			Client = client;
		}

		public virtual void HandleMcpePlayStatus(McpePlayStatus message)
		{
			Client.PlayerStatus = (McpePlayStatus.PlayStatus) message.status;

			if (Client.PlayerStatus == McpePlayStatus.PlayStatus.PlayerSpawn)
			{
				// Spawn tail exactly like a real 1.26 client (captured live): close the loading
				// screen, then announce spawn completion. Servers gate spawn-complete logic
				// (mob spawning, etc.) on set_local_player_as_initialized.
				var loadingScreen = McpeServerBoundLoadingScreen.CreateObject();
				loadingScreen.type = 2;
				loadingScreen.loadingScreenId = null;
				Client.SendPacket(loadingScreen);

				var initialized = McpeSetLocalPlayerAsInitialized.CreateObject();
				initialized.runtimeEntityId = Client.EntityId;
				Client.SendPacket(initialized);

				Client.HasSpawned = true;
				//if (Client.IsEmulator)
				{
					Client.PlayerStatusChangedWaitHandle.Set();
					//Client.SendMcpeMovePlayer();
				}
			}
		}

		public virtual void HandleMcpeServerToClientHandshake(McpeServerToClientHandshake message)
		{
			string token = message.token;
			if (Log.IsDebugEnabled) Log.Debug($"JWT:\n{token}");

			IDictionary<string, dynamic> headers = JWT.Headers(token);
			string x5u = headers["x5u"];

			if (Log.IsDebugEnabled) Log.Debug($"JWT payload:\n{JWT.Payload(token)}");

			var remotePublicKey = (ECPublicKeyParameters) PublicKeyFactory.CreateKey(x5u.DecodeBase64());

			var signParam = new ECParameters
			{
				Curve = ECCurve.NamedCurves.nistP384,
				Q =
				{
					X = remotePublicKey.Q.AffineXCoord.GetEncoded(),
					Y = remotePublicKey.Q.AffineYCoord.GetEncoded()
				},
			};
			signParam.Validate();

			var signKey = ECDsa.Create(signParam);

			try
			{
				var data = JWT.Decode<HandshakeData>(token, signKey);

				Client.InitiateEncryption(Base64Url.Decode(x5u), Base64Url.Decode(data.salt));
			}
			catch (Exception e)
			{
				Log.Error(token, e);
				throw;
			}
		}

		public virtual void HandleMcpeDisconnect(McpeDisconnect message)
		{
			Client.StopClient();
		}

		public virtual void HandleMcpeResourcePacksInfo(McpeResourcePacksInfo message)
		{
			//McpeResourcePackClientResponse response = new McpeResourcePackClientResponse();
			//response.responseStatus = 3;
			//SendPackage(response);

			if (message.resourcePacks.Count != 0)
			{
				var downloadingPacks = new List<string>();

				foreach (PackInfoData packInfo in message.resourcePacks)
				{
					downloadingPacks.Add(packInfo.packIdVersion.packUuid.ToString());
				}

				var response = new McpeResourcePackClientResponse();
				response.response = new ResourcePackClientResponseDownloading {downloadingPacks = downloadingPacks};
				Client.SendPacket(response);
			}
			else
			{
				var response = new McpeResourcePackClientResponse();
				response.response = new ResourcePackClientResponseDownloadingFinished();
				Client.SendPacket(response);
			}
		}

		public virtual void HandleMcpeResourcePackStack(McpeResourcePackStack message)
		{
			//if (message.resourcepackidversions.Count != 0)
			//{
			//	McpeResourcePackClientResponse response = new McpeResourcePackClientResponse();
			//	response.responseStatus = 2;
			//	response.resourcepackidversions = message.resourcepackidversions;
			//	SendPackage(response);
			//}
			//else
			{
				var response = new McpeResourcePackClientResponse();
				response.response = new ResourcePackClientResponseResourcePackStackFinished();
				Client.SendPacket(response);
			}
		}

		public virtual void HandleMcpeText(McpeText message)
		{
		}

		public virtual void HandleMcpeSetTime(McpeSetTime message)
		{
		}

		public virtual void HandleMcpeStartGame(McpeStartGame message)
		{
			var client = Client;
			client.EntityId = message.runtimeEntityId;
			client.NetworkEntityId = message.entityIdSelf;
			client.SpawnPoint = new PlayerLocation(message.position.X, message.position.Y, message.position.Z);
			client.CurrentLocation = new PlayerLocation(client.SpawnPoint, message.rotation.X, message.rotation.X, message.rotation.Y);

			client.LevelInfo.LevelName = message.levelName;
			client.LevelInfo.Version = 19133;
			client.LevelInfo.GameType = message.settings.gameType;

			var packet = McpeRequestChunkRadius.CreateObject();
			client.ChunkRadius = 5;
			packet.chunkRadius = client.ChunkRadius;
			packet.maxRadius = 32;

			if (Client.IsEmulator)
			{
				Client.HasSpawned = true;
				Client.PlayerStatusChangedWaitHandle.Set();
				Client.SendMcpeMovePlayer();
			}

			client.SendPacket(packet);

			// A real client opens its loading screen right after requesting the chunk radius
			// (captured live); the matching type 2 close is sent on PlayStatus(3).
			var loadingScreen = McpeServerBoundLoadingScreen.CreateObject();
			loadingScreen.type = 1;
			loadingScreen.loadingScreenId = null;
			client.SendPacket(loadingScreen);
		}

		public virtual void HandleMcpeAddPlayer(McpeAddPlayer message)
		{
		}

		public virtual void HandleMcpeAddEntity(McpeAddEntity message)
		{
		}

		public virtual void HandleMcpeRemoveEntity(McpeRemoveEntity message)
		{
		}

		public virtual void HandleMcpeAddItemEntity(McpeAddItemEntity message)
		{
		}

		public virtual void HandleMcpeTakeItemEntity(McpeTakeItemEntity message)
		{
		}

		public virtual void HandleMcpeServerPlayerPostMovePosition(McpeServerPlayerPostMovePosition message)
		{
		}

		public virtual void HandleMcpeMoveEntity(McpeMoveEntity message)
		{
		}

		public virtual void HandleMcpeMovePlayer(McpeMovePlayer message)
		{
			if (message.runtimeEntityId != Client.EntityId) return;

			Client.CurrentLocation = new PlayerLocation(message.position.X, message.position.Y, message.position.Z);

			//Client.LevelInfo.SpawnX = (int) message.position.X;
			//Client.LevelInfo.SpawnY = (int) message.position.Y;
			//Client.LevelInfo.SpawnZ = (int) message.position.Z;

			//Client.SendMcpeMovePlayer();
		}

		public virtual void HandleMcpeUpdateBlock(McpeUpdateBlock message)
		{
		}

		public virtual void HandleMcpeAddPainting(McpeAddPainting message)
		{
		}

		public virtual void HandleMcpeLevelEvent(McpeLevelEvent message)
		{
		}

		public virtual void HandleMcpeBlockEvent(McpeBlockEvent message)
		{
		}

		public virtual void HandleMcpeEntityEvent(McpeEntityEvent message)
		{
		}

		public virtual void HandleMcpeMobEffect(McpeMobEffect message)
		{
		}

		public virtual void HandleMcpeUpdateAttributes(McpeUpdateAttributes message)
		{
		}

		public virtual void HandleMcpeInventoryTransaction(McpeInventoryTransaction message)
		{
		}

		public virtual void HandleMcpeMobEquipment(McpeMobEquipment message)
		{
		}

		public virtual void HandleMcpeMobArmorEquipment(McpeMobArmorEquipment message)
		{
		}

		public virtual void HandleMcpeInteract(McpeInteract message)
		{
		}

		public virtual void HandleMcpeHurtArmor(McpeHurtArmor message)
		{
		}

		public virtual void HandleMcpeSetEntityData(McpeSetEntityData message)
		{
		}

		public virtual void HandleMcpeSetEntityMotion(McpeSetEntityMotion message)
		{
		}

		public virtual void HandleMcpeSetEntityLink(McpeSetEntityLink message)
		{
		}

		public virtual void HandleMcpeSetHealth(McpeSetHealth message)
		{
		}

		public virtual void HandleMcpeSetSpawnPosition(McpeSetSpawnPosition message)
		{
			Client.SpawnPoint = new Vector3(message.coordinates.X, message.coordinates.Y, message.coordinates.Z);
			Client.LevelInfo.SpawnX = (int) Client.SpawnPoint.X;
			Client.LevelInfo.SpawnY = (int) Client.SpawnPoint.Y;
			Client.LevelInfo.SpawnZ = (int) Client.SpawnPoint.Z;
		}

		public virtual void HandleMcpeAnimate(McpeAnimate message)
		{
		}

		public virtual void HandleMcpeRespawn(McpeRespawn message)
		{
			Client.CurrentLocation = new PlayerLocation(message.x, message.y, message.z);
		}

		public virtual void HandleMcpeContainerOpen(McpeContainerOpen message)
		{
		}

		public virtual void HandleMcpeContainerClose(McpeContainerClose message)
		{
		}

		public virtual void HandleMcpePlayerHotbar(McpePlayerHotbar message)
		{
		}

		public virtual void HandleMcpeInventoryContent(McpeInventoryContent message)
		{
		}

		public virtual void HandleMcpeInventorySlot(McpeInventorySlot message)
		{
		}

		public virtual void HandleMcpeContainerSetData(McpeContainerSetData message)
		{
		}

		public virtual void HandleMcpeCraftingData(McpeCraftingData message)
		{
		}

		public virtual void HandleMcpeGuiDataPickItem(McpeGuiDataPickItem message)
		{
		}


		public virtual void HandleMcpeBlockEntityData(McpeBlockEntityData message)
		{
		}

		public virtual void HandleMcpeLevelChunk(McpeLevelChunk message)
		{
		}

		public virtual void HandleMcpeSetCommandsEnabled(McpeSetCommandsEnabled message)
		{
		}

		public virtual void HandleMcpeSetDifficulty(McpeSetDifficulty message)
		{
		}

		public virtual void HandleMcpeChangeDimension(McpeChangeDimension message)
		{
			Thread.Sleep(3000);

			var action = McpePlayerAction.CreateObject();
			action.runtimeEntityId = Client.EntityId;
			action.actionId = (int) PlayerAction.DimensionChangeAck;
			Client.SendPacket(action);
		}

		public virtual void HandleMcpeSetPlayerGameType(McpeSetPlayerGameType message)
		{
		}

		public virtual void HandleMcpePlayerList(McpePlayerList message)
		{
		}

		public virtual void HandleMcpeSimpleEvent(McpeSimpleEvent message)
		{
		}

		public virtual void HandleMcpeTelemetryEvent(McpeTelemetryEvent message)
		{
		}

		public virtual void HandleMcpeSpawnExperienceOrb(McpeSpawnExperienceOrb message)
		{
		}

		public virtual void HandleMcpeClientboundMapItemData(McpeClientboundMapItemData message)
		{
		}

		public virtual void HandleMcpeMapInfoRequest(McpeMapInfoRequest message)
		{
		}

		public virtual void HandleMcpeRequestChunkRadius(McpeRequestChunkRadius message)
		{
		}

		public virtual void HandleMcpeChunkRadiusUpdate(McpeChunkRadiusUpdate message)
		{
		}

		public virtual void HandleMcpeGameRulesChanged(McpeGameRulesChanged message)
		{
		}

		public virtual void HandleMcpeCamera(McpeCamera message)
		{
		}

		public virtual void HandleMcpeBossEvent(McpeBossEvent message)
		{
		}

		public virtual void HandleMcpeShowCredits(McpeShowCredits message)
		{
		}

		public virtual void HandleMcpeAvailableCommands(McpeAvailableCommands message)
		{
		}

		public virtual void HandleMcpeCommandOutput(McpeCommandOutput message)
		{
		}

		public virtual void HandleMcpeUpdateTrade(McpeUpdateTrade message)
		{
		}

		public virtual void HandleMcpeUpdateEquipment(McpeUpdateEquipment message)
		{
		}

		private Dictionary<string, uint> _resourcePackDataInfos = new Dictionary<string, uint>();

		public virtual void HandleMcpeResourcePackDataInfo(McpeResourcePackDataInfo message)
		{
			var request = new McpeResourcePackChunkRequest();
			request.packageId = message.packageId;
			request.chunkIndex = 0;
			Client.SendPacket(request);
			_resourcePackDataInfos.Add(message.packageId, message.chunkCount);
		}

		public virtual void HandleMcpeResourcePackChunkData(McpeResourcePackChunkData message)
		{
			if (message.chunkIndex + 1 < _resourcePackDataInfos[message.packageId])
			{
				var request = new McpeResourcePackChunkRequest();
				request.packageId = message.packageId;
				request.chunkIndex = message.chunkIndex + 1;
				Client.SendPacket(request);
			}
			else
			{
				_resourcePackDataInfos.Remove(message.packageId);
			}

			if (_resourcePackDataInfos.Count == 0)
			{
				var response = new McpeResourcePackClientResponse();
				response.response = new ResourcePackClientResponseDownloadingFinished();
				Client.SendPacket(response);
			}
		}

		public virtual void HandleMcpeTransfer(McpeTransfer message)
		{
		}

		public virtual void HandleMcpePlaySound(McpePlaySound message)
		{
		}

		public virtual void HandleMcpeStopSound(McpeStopSound message)
		{
		}

		public virtual void HandleMcpeSetTitle(McpeSetTitle message)
		{
		}

		public virtual void HandleMcpeAddBehaviorTree(McpeAddBehaviorTree message)
		{
		}

		public virtual void HandleMcpeStructureBlockUpdate(McpeStructureBlockUpdate message)
		{
		}

		public virtual void HandleMcpeShowStoreOffer(McpeShowStoreOffer message)
		{
		}

		public virtual void HandleMcpePlayerSkin(McpePlayerSkin message)
		{
		}

		public virtual void HandleMcpeSubClientLogin(McpeSubClientLogin message)
		{
		}

		public virtual void HandleMcpeInitiateWebSocketConnection(McpeInitiateWebSocketConnection message)
		{
		}

		public virtual void HandleMcpeSetLastHurtBy(McpeSetLastHurtBy message)
		{
		}

		public virtual void HandleMcpeBookEdit(McpeBookEdit message)
		{
		}

		public virtual void HandleMcpeNpcRequest(McpeNpcRequest message)
		{
		}

		public virtual void HandleMcpeModalFormRequest(McpeModalFormRequest message)
		{
		}

		public virtual void HandleMcpeServerSettingsResponse(McpeServerSettingsResponse message)
		{
		}

		public virtual void HandleMcpeShowProfile(McpeShowProfile message)
		{
		}

		public virtual void HandleMcpeSetDefaultGameType(McpeSetDefaultGameType message)
		{
		}

		public virtual void HandleMcpeRemoveObjective(McpeRemoveObjective message)
		{
		}

		public virtual void HandleMcpeSetDisplayObjective(McpeSetDisplayObjective message)
		{
		}

		public virtual void HandleMcpeSetScore(McpeSetScore message)
		{
		}

		public virtual void HandleMcpeLabTable(McpeLabTable message)
		{
		}

		public virtual void HandleMcpeUpdateBlockSynced(McpeUpdateBlockSynced message)
		{
		}

		public virtual void HandleMcpeMoveEntityDelta(McpeMoveEntityDelta message)
		{
		}

		public virtual void HandleMcpeSetScoreboardIdentity(McpeSetScoreboardIdentity message)
		{
		}

		public virtual void HandleMcpeUpdateSoftEnum(McpeUpdateSoftEnum message)
		{
		}

		public virtual void HandleMcpeNetworkStackLatency(McpeNetworkStackLatency message)
		{
			var packet = McpeNetworkStackLatency.CreateObject();
			packet.timestamp = message.timestamp;
			packet.unknownFlag = 0;

			Client.SendPacket(packet);
		}

		public virtual void HandleMcpeSpawnParticleEffect(McpeSpawnParticleEffect message)
		{
		}

		public virtual void HandleMcpeAvailableEntityIdentifiers(McpeAvailableEntityIdentifiers message)
		{
		}

		public virtual void HandleMcpeNetworkChunkPublisherUpdate(McpeNetworkChunkPublisherUpdate message)
		{
		}

		public virtual void HandleMcpeBiomeDefinitionList(McpeBiomeDefinitionList message)
		{
		}

		public virtual void HandleMcpeLevelSoundEvent(McpeLevelSoundEvent message)
		{
		}

		public virtual void HandleMcpeLevelEventGeneric(McpeLevelEventGeneric message)
		{
		}

		public virtual void HandleMcpeLecternUpdate(McpeLecternUpdate message)
		{
		}

		public virtual void HandleMcpeClientCacheStatus(McpeClientCacheStatus message)
		{
		}

		public virtual void HandleMcpeOnScreenTextureAnimation(McpeOnScreenTextureAnimation message)
		{
		}

		public virtual void HandleMcpeMapCreateLockedCopy(McpeMapCreateLockedCopy message)
		{
		}

		public virtual void HandleMcpeStructureTemplateDataExportRequest(McpeStructureTemplateDataExportRequest message)
		{
		}

		public virtual void HandleMcpeStructureTemplateDataExportResponse(McpeStructureTemplateDataExportResponse message)
		{
		}

		public virtual void HandleMcpeClientCacheBlobStatus(McpeClientCacheBlobStatus message)
		{
		}

		public virtual void HandleMcpeClientCacheMissResponse(McpeClientCacheMissResponse message)
		{
		}

		public virtual void HandleMcpeEducationSettings(McpeEducationSettings message)
		{
		}

		public virtual void HandleMcpeEmote(McpeEmote message)
		{
		}

		public virtual void HandleMcpeMultiplayerSettings(McpeMultiplayerSettings message)
		{
		}

		public virtual void HandleMcpeCompletedUsingItem(McpeCompletedUsingItem message)
		{
		}

		public virtual void HandleMcpeNetworkSettings(McpeNetworkSettings message)
		{
			var handler = (BedrockMessageHandlerBase) Client.Session.CustomMessageHandler;
			handler.CompressionEnabled = true;
			handler.CompressionThreshold = message.compressionThreshold;

			if (message.compressionAlgorithm == 1) Log.Warn("Server negotiated snappy compression, which is not implemented. Expect failures.");

			Client.SendLogin(Client.Username);
		}

		public virtual void HandleMcpeCreativeContent(McpeCreativeContent message)
		{
		}

		public void HandleMcpePlayerEnchantOptions(McpePlayerEnchantOptions message)
		{
		}

		public virtual void HandleMcpeItemStackResponse(McpeItemStackResponse message)
		{
		}

		public virtual void HandleMcpePlayerArmorDamage(McpePlayerArmorDamage message)
		{
		}

		public virtual void HandleMcpeCodeBuilder(McpeCodeBuilder message)
		{
		}

		public virtual void HandleMcpePositionTrackingDbServerBroadcast(McpePositionTrackingDbServerBroadcast message)
		{
		}

		public virtual void HandleMcpeDebugInfo(McpeDebugInfo message)
		{
		}

		public virtual void HandleMcpeMotionPredictionHints(McpeMotionPredictionHints message)
		{
		}

		public virtual void HandleMcpeAnimateEntity(McpeAnimateEntity message)
		{
		}

		/// <inheritdoc />
		public virtual void HandleMcpeCorrectPlayerMovePrediction(McpeCorrectPlayerMovePrediction message)
		{
		}

		/// <inheritdoc />
		public virtual void HandleMcpeItemComponent(McpeItemComponent message)
		{

		}

		/// <inheritdoc />
		public virtual void HandleMcpeUpdateSubChunkBlocksPacket(McpeUpdateSubChunkBlocksPacket message)
		{

		}

		/// <inheritdoc />
		public virtual void HandleMcpeSubChunkPacket(McpeSubChunkPacket message)
		{
			
		}

		/// <inheritdoc />
		public void HandleMcpeDimensionData(McpeDimensionData message)
		{

		}

		public virtual void HandleMcpePlayerStartItemCooldown(McpePlayerStartItemCooldown message)
		{
		}

		public virtual void HandleMcpeScriptMessage(McpeScriptMessage message)
		{
		}

		public virtual void HandleMcpeTickingAreasLoadStatus(McpeTickingAreasLoadStatus message)
		{
		}

		public virtual void HandleMcpeAgentActionEvent(McpeAgentActionEvent message)
		{
		}

		public virtual void HandleMcpeLessonProgress(McpeLessonProgress message)
		{
		}

		public virtual void HandleMcpeToastRequest(McpeToastRequest message)
		{
		}

		public virtual void HandleMcpeDeathInfo(McpeDeathInfo message)
		{
		}

		public virtual void HandleMcpeEditorNetwork(McpeEditorNetwork message)
		{
		}

		public virtual void HandleMcpeFeatureRegistry(McpeFeatureRegistry message)
		{
		}

		public virtual void HandleMcpeServerStats(McpeServerStats message)
		{
		}

		public virtual void HandleFtlCreatePlayer(FtlCreatePlayer message)
		{
		}

		public virtual void HandleMcpeTrimData(McpeTrimData message)
		{
		}

		public virtual void HandleMcpeJigsawStructureData(McpeJigsawStructureData message)
		{
		}

		public virtual void HandleMcpeCurrentStructureFeature(McpeCurrentStructureFeature message)
		{
		}

		public virtual void HandleMcpeSetHud(McpeSetHud message)
		{
		}

		public virtual void HandleMcpeAwardAchievement(McpeAwardAchievement message)
		{
		}

		public virtual void HandleMcpeClientboundCloseForm(McpeClientboundCloseForm message)
		{
		}

		public virtual void HandleMcpeCameraAimAssist(McpeCameraAimAssist message)
		{
		}

		public virtual void HandleMcpeContainerRegistryCleanup(McpeContainerRegistryCleanup message)
		{
		}

		public virtual void HandleMcpeMovementEffect(McpeMovementEffect message)
		{
		}

		public virtual void HandleMcpeCameraAimAssistPresets(McpeCameraAimAssistPresets message)
		{
		}

		public virtual void HandleMcpeCameraAimAssistActorPriority(McpeCameraAimAssistActorPriority message)
		{
		}

		public virtual void HandleMcpeClientboundAttributeLayerSync(McpeClientboundAttributeLayerSync message)
		{
		}

		public virtual void HandleMcpeServerStoreInfo(McpeServerStoreInfo message)
		{
		}

		public virtual void HandleMcpeServerPresenceInfo(McpeServerPresenceInfo message)
		{
		}

		public virtual void HandleMcpePlayerVideoCapture(McpePlayerVideoCapture message)
		{
		}

		public virtual void HandleMcpePlayerUpdateEntityOverrides(McpePlayerUpdateEntityOverrides message)
		{
		}

		public virtual void HandleMcpeClientboundControlSchemeSet(McpeClientboundControlSchemeSet message)
		{
		}

		public virtual void HandleMcpePrimitiveShapes(McpePrimitiveShapes message)
		{
		}

		public virtual void HandleMcpeClientboundDataStore(McpeClientboundDataStore message)
		{
		}

		public virtual void HandleMcpeGraphicsOverrideParameter(McpeGraphicsOverrideParameter message)
		{
		}

		public virtual void HandleMcpeClientboundDataDrivenUiShowScreen(McpeClientboundDataDrivenUiShowScreen message)
		{
		}

		public virtual void HandleMcpeClientboundDataDrivenUiCloseScreen(McpeClientboundDataDrivenUiCloseScreen message)
		{
		}

		public virtual void HandleMcpeClientboundDataDrivenUiReload(McpeClientboundDataDrivenUiReload message)
		{
		}

		public virtual void HandleMcpeClientboundTextureShift(McpeClientboundTextureShift message)
		{
		}

		public virtual void HandleMcpeVoxelShapes(McpeVoxelShapes message)
		{
		}

		public virtual void HandleMcpeCameraSpline(McpeCameraSpline message)
		{
		}

		public virtual void HandleMcpeClientboundUpdateSoundData(McpeClientboundUpdateSoundData message)
		{
		}

		public virtual void HandleMcpeSendPartyDestinationCookie(McpeSendPartyDestinationCookie message)
		{
		}

		public virtual void HandleMcpeSyncWorldClocks(McpeSyncWorldClocks message)
		{
			foreach (var clock in message.Clocks)
			{
				Log.Warn($"WorldClock registry: id={clock.Id} name={clock.Name} time={clock.Time} paused={clock.Paused} markers={clock.TimeMarkers.Count}");
			}

			foreach (var state in message.SyncStates)
			{
				Log.Warn($"WorldClock state: id={state.ClockId} time={state.Time} paused={state.Paused}");
			}
		}

		public virtual void HandleMcpePlayerFog(McpePlayerFog message)
		{
		}

		public virtual void HandleMcpeSyncEntityProperty(McpeSyncEntityProperty message)
		{
		}

		public virtual void HandleMcpeClientboundDebugRenderer(McpeClientboundDebugRenderer message)
		{
		}

		public virtual void HandleMcpeAddVolumeEntity(McpeAddVolumeEntity message)
		{
		}

		public virtual void HandleMcpeRemoveVolumeEntity(McpeRemoveVolumeEntity message)
		{
		}

		public virtual void HandleMcpeSimulationType(McpeSimulationType message)
		{
		}

		public virtual void HandleMcpeNpcDialogue(McpeNpcDialogue message)
		{
		}

		public virtual void HandleMcpeEduUriResource(McpeEduUriResource message)
		{
		}

		public virtual void HandleMcpeUpdateAbilities(McpeUpdateAbilities message)
		{
			Client.UserPermission = (CommandPermission) message.commandPermission;
		}

		public virtual void HandleMcpeUpdateAdventureSettings(McpeUpdateAdventureSettings message)
		{
		}

		public virtual void HandleMcpeCameraPresets(McpeCameraPresets message)
		{
		}

		public virtual void HandleMcpeCameraInstruction(McpeCameraInstruction message)
		{
		}

		public virtual void HandleMcpeCameraShake(McpeCameraShake message)
		{
		}

		public virtual void HandleMcpePlayerLocation(McpePlayerLocation message)
		{
		}

		public virtual void HandleMcpeLocatorBar(McpeLocatorBar message)
		{
		}

		public virtual void HandleMcpeGameTestResults(McpeGameTestResults message)
		{
		}

		public virtual void HandleMcpeUpdateClientInputLocks(McpeUpdateClientInputLocks message)
		{
		}

		public virtual void HandleMcpeUnlockedRecipes(McpeUnlockedRecipes message)
		{
		}

		public virtual void HandleMcpeOpenSign(McpeOpenSign message)
		{
		}

		public virtual void HandleMcpeAgentAnimation(McpeAgentAnimation message)
		{
		}

		public virtual void HandleMcpeRefreshEntitlements(McpeRefreshEntitlements message)
		{
		}
	}

	public class DefaultMessageHandler : McpeClientMessageHandlerBase
	{
		public DefaultMessageHandler(MiNetClient client) : base(client)
		{
		}
	}
}

