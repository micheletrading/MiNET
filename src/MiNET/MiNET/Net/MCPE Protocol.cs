#region LICENSE

// The contents of this file are subject to the Common Public Attribution// The contents of this file are subject to the Common Public Attribution
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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson. 
// All Rights Reserved.

#endregion

//
// WARNING: T4 GENERATED CODE - DO NOT EDIT
// 

using System;
using System.Net;
using System.Numerics;
using System.Threading;
using fNbt;
using MiNET.Utils; 
using MiNET.Utils.Skins;
using MiNET.Items;
using MiNET.Crafting;
using MiNET.Net.RakNet;
using little = MiNET.Utils.Int24; // friendly name
using LongString = System.String;
using MiNET.Utils.Metadata;
using MiNET.Utils.Vectors;
using MiNET.Utils.Nbt;

namespace MiNET.Net
{
	public class McpeProtocolInfo
	{
		public const int ProtocolVersion = 1001;
		public const string GameVersion = "1.26.34";
	}

	public interface IMcpeMessageHandler
	{
		void Disconnect(string reason, bool sendDisconnect = true);

		void HandleMcpeLogin(McpeLogin message);
		void HandleMcpeClientToServerHandshake(McpeClientToServerHandshake message);
		void HandleMcpeResourcePackClientResponse(McpeResourcePackClientResponse message);
		void HandleMcpeText(McpeText message);
		void HandleMcpeMoveEntity(McpeMoveEntity message);
		void HandleMcpeMovePlayer(McpeMovePlayer message);
		void HandleMcpeEntityEvent(McpeEntityEvent message);
		void HandleMcpeInventoryTransaction(McpeInventoryTransaction message);
		void HandleMcpeMobEquipment(McpeMobEquipment message);
		void HandleMcpeMobArmorEquipment(McpeMobArmorEquipment message);
		void HandleMcpeInteract(McpeInteract message);
		void HandleMcpeBlockPickRequest(McpeBlockPickRequest message);
		void HandleMcpeEntityPickRequest(McpeEntityPickRequest message);
		void HandleMcpePlayerAction(McpePlayerAction message);
		void HandleMcpeSetEntityData(McpeSetEntityData message);
		void HandleMcpeSetEntityMotion(McpeSetEntityMotion message);
		void HandleMcpeAnimate(McpeAnimate message);
		void HandleMcpeRespawn(McpeRespawn message);
		void HandleMcpeContainerClose(McpeContainerClose message);
		void HandleMcpePlayerHotbar(McpePlayerHotbar message);
		void HandleMcpeInventoryContent(McpeInventoryContent message);
		void HandleMcpeInventorySlot(McpeInventorySlot message);
		void HandleMcpeBlockEntityData(McpeBlockEntityData message);
		void HandleMcpeSetPlayerGameType(McpeSetPlayerGameType message);
		void HandleMcpeMapInfoRequest(McpeMapInfoRequest message);
		void HandleMcpeRequestChunkRadius(McpeRequestChunkRadius message);
		void HandleMcpeCommandRequest(McpeCommandRequest message);
		void HandleMcpeCommandBlockUpdate(McpeCommandBlockUpdate message);
		void HandleMcpeResourcePackChunkRequest(McpeResourcePackChunkRequest message);
		void HandleMcpePurchaseReceipt(McpePurchaseReceipt message);
		void HandleMcpePlayerSkin(McpePlayerSkin message);
		void HandleMcpeNpcRequest(McpeNpcRequest message);
		void HandleMcpePhotoTransfer(McpePhotoTransfer message);
		void HandleMcpeModalFormResponse(McpeModalFormResponse message);
		void HandleMcpeServerSettingsRequest(McpeServerSettingsRequest message);
		void HandleMcpeLabTable(McpeLabTable message);
		void HandleMcpeSetLocalPlayerAsInitialized(McpeSetLocalPlayerAsInitialized message);
		void HandleMcpeNetworkStackLatency(McpeNetworkStackLatency message);
		void HandleMcpeLevelSoundEvent(McpeLevelSoundEvent message);
		void HandleMcpeClientCacheStatus(McpeClientCacheStatus message);
		void HandleMcpeClientCacheBlobStatus(McpeClientCacheBlobStatus message);
		void HandleMcpeEmote(McpeEmote message);
		void HandleMcpeMultiplayerSettings(McpeMultiplayerSettings message);
		void HandleMcpeSettingsCommand(McpeSettingsCommand message);
		void HandleMcpeAnvilDamage(McpeAnvilDamage message);
		void HandleMcpePlayerAuthInput(McpePlayerAuthInput message);
		void HandleMcpeItemStackRequest(McpeItemStackRequest message);
		void HandleMcpeUpdatePlayerGameType(McpeUpdatePlayerGameType message);
		void HandleMcpeEmoteList(McpeEmoteList message);
		void HandleMcpePositionTrackingDbClientRequest(McpePositionTrackingDbClientRequest message);
		void HandleMcpeDebugInfo(McpeDebugInfo message);
		void HandleMcpePacketViolationWarning(McpePacketViolationWarning message);
		void HandleMcpeCreatePhoto(McpeCreatePhoto message);
		void HandleMcpeUpdateSubChunkBlocksPacket(McpeUpdateSubChunkBlocksPacket message);
		void HandleMcpeSubChunkRequestPacket(McpeSubChunkRequestPacket message);
		void HandleMcpeScriptMessage(McpeScriptMessage message);
		void HandleMcpeCodeBuilderSource(McpeCodeBuilderSource message);
		void HandleMcpeChangeMobProperty(McpeChangeMobProperty message);
		void HandleMcpeRequestAbility(McpeRequestAbility message);
		void HandleMcpeRequestPermissions(McpeRequestPermissions message);
		void HandleMcpeEditorNetwork(McpeEditorNetwork message);
		void HandleMcpeRequestNetworkSettings(McpeRequestNetworkSettings message);
		void HandleMcpeGameTestRequest(McpeGameTestRequest message);
		void HandleMcpePlayerToggleCrafterSlotRequest(McpePlayerToggleCrafterSlotRequest message);
		void HandleMcpeSetPlayerInventoryOptions(McpeSetPlayerInventoryOptions message);
		void HandleMcpeServerBoundLoadingScreen(McpeServerBoundLoadingScreen message);
		void HandleMcpeServerBoundDiagnostics(McpeServerBoundDiagnostics message);
		void HandleMcpeClientCameraAimAssist(McpeClientCameraAimAssist message);
		void HandleMcpeClientMovementPredictionSync(McpeClientMovementPredictionSync message);
		void HandleMcpeUpdateClientOptions(McpeUpdateClientOptions message);
		void HandleMcpeServerboundPackSettingChange(McpeServerboundPackSettingChange message);
		void HandleMcpeServerboundDataStore(McpeServerboundDataStore message);
		void HandleMcpeResourcePacksReadyForValidation(McpeResourcePacksReadyForValidation message);
		void HandleMcpePartyChanged(McpePartyChanged message);
		void HandleMcpeServerboundDataDrivenScreenClosed(McpeServerboundDataDrivenScreenClosed message);
		void HandleMcpePartyDestinationCookieResponse(McpePartyDestinationCookieResponse message);
	}

	public interface IMcpeClientMessageHandler
	{
		void HandleMcpePlayStatus(McpePlayStatus message);
		void HandleMcpeServerToClientHandshake(McpeServerToClientHandshake message);
		void HandleMcpeDisconnect(McpeDisconnect message);
		void HandleMcpeResourcePacksInfo(McpeResourcePacksInfo message);
		void HandleMcpeResourcePackStack(McpeResourcePackStack message);
		void HandleMcpeText(McpeText message);
		void HandleMcpeSetTime(McpeSetTime message);
		void HandleMcpeStartGame(McpeStartGame message);
		void HandleMcpeAddPlayer(McpeAddPlayer message);
		void HandleMcpeAddEntity(McpeAddEntity message);
		void HandleMcpeRemoveEntity(McpeRemoveEntity message);
		void HandleMcpeAddItemEntity(McpeAddItemEntity message);
		void HandleMcpeServerPlayerPostMovePosition(McpeServerPlayerPostMovePosition message);
		void HandleMcpeTakeItemEntity(McpeTakeItemEntity message);
		void HandleMcpeMoveEntity(McpeMoveEntity message);
		void HandleMcpeMovePlayer(McpeMovePlayer message);
		void HandleMcpeUpdateBlock(McpeUpdateBlock message);
		void HandleMcpeAddPainting(McpeAddPainting message);
		void HandleMcpeLevelEvent(McpeLevelEvent message);
		void HandleMcpeBlockEvent(McpeBlockEvent message);
		void HandleMcpeEntityEvent(McpeEntityEvent message);
		void HandleMcpeMobEffect(McpeMobEffect message);
		void HandleMcpeUpdateAttributes(McpeUpdateAttributes message);
		void HandleMcpeInventoryTransaction(McpeInventoryTransaction message);
		void HandleMcpeMobEquipment(McpeMobEquipment message);
		void HandleMcpeMobArmorEquipment(McpeMobArmorEquipment message);
		void HandleMcpeInteract(McpeInteract message);
		void HandleMcpeHurtArmor(McpeHurtArmor message);
		void HandleMcpeSetEntityData(McpeSetEntityData message);
		void HandleMcpeSetEntityMotion(McpeSetEntityMotion message);
		void HandleMcpeSetEntityLink(McpeSetEntityLink message);
		void HandleMcpeSetHealth(McpeSetHealth message);
		void HandleMcpeSetSpawnPosition(McpeSetSpawnPosition message);
		void HandleMcpeAnimate(McpeAnimate message);
		void HandleMcpeRespawn(McpeRespawn message);
		void HandleMcpeContainerOpen(McpeContainerOpen message);
		void HandleMcpeContainerClose(McpeContainerClose message);
		void HandleMcpePlayerHotbar(McpePlayerHotbar message);
		void HandleMcpeInventoryContent(McpeInventoryContent message);
		void HandleMcpeInventorySlot(McpeInventorySlot message);
		void HandleMcpeContainerSetData(McpeContainerSetData message);
		void HandleMcpeCraftingData(McpeCraftingData message);
		void HandleMcpeGuiDataPickItem(McpeGuiDataPickItem message);
		void HandleMcpeBlockEntityData(McpeBlockEntityData message);
		void HandleMcpeLevelChunk(McpeLevelChunk message);
		void HandleMcpeSetCommandsEnabled(McpeSetCommandsEnabled message);
		void HandleMcpeSetDifficulty(McpeSetDifficulty message);
		void HandleMcpeChangeDimension(McpeChangeDimension message);
		void HandleMcpeSetPlayerGameType(McpeSetPlayerGameType message);
		void HandleMcpePlayerList(McpePlayerList message);
		void HandleMcpeSimpleEvent(McpeSimpleEvent message);
		void HandleMcpeTelemetryEvent(McpeTelemetryEvent message);
		void HandleMcpeSpawnExperienceOrb(McpeSpawnExperienceOrb message);
		void HandleMcpeClientboundMapItemData(McpeClientboundMapItemData message);
		void HandleMcpeMapInfoRequest(McpeMapInfoRequest message);
		void HandleMcpeRequestChunkRadius(McpeRequestChunkRadius message);
		void HandleMcpeChunkRadiusUpdate(McpeChunkRadiusUpdate message);
		void HandleMcpeGameRulesChanged(McpeGameRulesChanged message);
		void HandleMcpeCamera(McpeCamera message);
		void HandleMcpeBossEvent(McpeBossEvent message);
		void HandleMcpeShowCredits(McpeShowCredits message);
		void HandleMcpeAvailableCommands(McpeAvailableCommands message);
		void HandleMcpeCommandOutput(McpeCommandOutput message);
		void HandleMcpeUpdateTrade(McpeUpdateTrade message);
		void HandleMcpeUpdateEquipment(McpeUpdateEquipment message);
		void HandleMcpeResourcePackDataInfo(McpeResourcePackDataInfo message);
		void HandleMcpeResourcePackChunkData(McpeResourcePackChunkData message);
		void HandleMcpeTransfer(McpeTransfer message);
		void HandleMcpePlaySound(McpePlaySound message);
		void HandleMcpeStopSound(McpeStopSound message);
		void HandleMcpeSetTitle(McpeSetTitle message);
		void HandleMcpeAddBehaviorTree(McpeAddBehaviorTree message);
		void HandleMcpeStructureBlockUpdate(McpeStructureBlockUpdate message);
		void HandleMcpeShowStoreOffer(McpeShowStoreOffer message);
		void HandleMcpePlayerSkin(McpePlayerSkin message);
		void HandleMcpeSubClientLogin(McpeSubClientLogin message);
		void HandleMcpeInitiateWebSocketConnection(McpeInitiateWebSocketConnection message);
		void HandleMcpeSetLastHurtBy(McpeSetLastHurtBy message);
		void HandleMcpeBookEdit(McpeBookEdit message);
		void HandleMcpeNpcRequest(McpeNpcRequest message);
		void HandleMcpeModalFormRequest(McpeModalFormRequest message);
		void HandleMcpeServerSettingsResponse(McpeServerSettingsResponse message);
		void HandleMcpeShowProfile(McpeShowProfile message);
		void HandleMcpeSetDefaultGameType(McpeSetDefaultGameType message);
		void HandleMcpeRemoveObjective(McpeRemoveObjective message);
		void HandleMcpeSetDisplayObjective(McpeSetDisplayObjective message);
		void HandleMcpeSetScore(McpeSetScore message);
		void HandleMcpeLabTable(McpeLabTable message);
		void HandleMcpeUpdateBlockSynced(McpeUpdateBlockSynced message);
		void HandleMcpeMoveEntityDelta(McpeMoveEntityDelta message);
		void HandleMcpeSetScoreboardIdentity(McpeSetScoreboardIdentity message);
		void HandleMcpeUpdateSoftEnum(McpeUpdateSoftEnum message);
		void HandleMcpeNetworkStackLatency(McpeNetworkStackLatency message);
		void HandleMcpeSpawnParticleEffect(McpeSpawnParticleEffect message);
		void HandleMcpeAvailableEntityIdentifiers(McpeAvailableEntityIdentifiers message);
		void HandleMcpeNetworkChunkPublisherUpdate(McpeNetworkChunkPublisherUpdate message);
		void HandleMcpeBiomeDefinitionList(McpeBiomeDefinitionList message);
		void HandleMcpeLevelSoundEvent(McpeLevelSoundEvent message);
		void HandleMcpeLevelEventGeneric(McpeLevelEventGeneric message);
		void HandleMcpeLecternUpdate(McpeLecternUpdate message);
		void HandleMcpeClientCacheStatus(McpeClientCacheStatus message);
		void HandleMcpeOnScreenTextureAnimation(McpeOnScreenTextureAnimation message);
		void HandleMcpeMapCreateLockedCopy(McpeMapCreateLockedCopy message);
		void HandleMcpeStructureTemplateDataExportRequest(McpeStructureTemplateDataExportRequest message);
		void HandleMcpeStructureTemplateDataExportResponse(McpeStructureTemplateDataExportResponse message);
		void HandleMcpeClientCacheMissResponse(McpeClientCacheMissResponse message);
		void HandleMcpeEducationSettings(McpeEducationSettings message);
		void HandleMcpeEmote(McpeEmote message);
		void HandleMcpeMultiplayerSettings(McpeMultiplayerSettings message);
		void HandleMcpeCompletedUsingItem(McpeCompletedUsingItem message);
		void HandleMcpeNetworkSettings(McpeNetworkSettings message);
		void HandleMcpeCreativeContent(McpeCreativeContent message);
		void HandleMcpePlayerEnchantOptions(McpePlayerEnchantOptions message);
		void HandleMcpeItemStackResponse(McpeItemStackResponse message);
		void HandleMcpePlayerArmorDamage(McpePlayerArmorDamage message);
		void HandleMcpeCodeBuilder(McpeCodeBuilder message);
		void HandleMcpePositionTrackingDbServerBroadcast(McpePositionTrackingDbServerBroadcast message);
		void HandleMcpeDebugInfo(McpeDebugInfo message);
		void HandleMcpeMotionPredictionHints(McpeMotionPredictionHints message);
		void HandleMcpeAnimateEntity(McpeAnimateEntity message);
		void HandleMcpePlayerFog(McpePlayerFog message);
		void HandleMcpeCorrectPlayerMovePrediction(McpeCorrectPlayerMovePrediction message);
		void HandleMcpeItemComponent(McpeItemComponent message);
		void HandleMcpeClientboundDebugRenderer(McpeClientboundDebugRenderer message);
		void HandleMcpeSyncEntityProperty(McpeSyncEntityProperty message);
		void HandleMcpeAddVolumeEntity(McpeAddVolumeEntity message);
		void HandleMcpeRemoveVolumeEntity(McpeRemoveVolumeEntity message);
		void HandleMcpeSimulationType(McpeSimulationType message);
		void HandleMcpeNpcDialogue(McpeNpcDialogue message);
		void HandleMcpeEduUriResource(McpeEduUriResource message);
		void HandleMcpeUpdateSubChunkBlocksPacket(McpeUpdateSubChunkBlocksPacket message);
		void HandleMcpeSubChunkPacket(McpeSubChunkPacket message);
		void HandleMcpePlayerStartItemCooldown(McpePlayerStartItemCooldown message);
		void HandleMcpeScriptMessage(McpeScriptMessage message);
		void HandleMcpeTickingAreasLoadStatus(McpeTickingAreasLoadStatus message);
		void HandleMcpeDimensionData(McpeDimensionData message);
		void HandleMcpeAgentActionEvent(McpeAgentActionEvent message);
		void HandleMcpeLessonProgress(McpeLessonProgress message);
		void HandleMcpeToastRequest(McpeToastRequest message);
		void HandleMcpeUpdateAbilities(McpeUpdateAbilities message);
		void HandleMcpeUpdateAdventureSettings(McpeUpdateAdventureSettings message);
		void HandleMcpeDeathInfo(McpeDeathInfo message);
		void HandleMcpeEditorNetwork(McpeEditorNetwork message);
		void HandleMcpeFeatureRegistry(McpeFeatureRegistry message);
		void HandleMcpeServerStats(McpeServerStats message);
		void HandleMcpeGameTestResults(McpeGameTestResults message);
		void HandleMcpeUpdateClientInputLocks(McpeUpdateClientInputLocks message);
		void HandleMcpeCameraPresets(McpeCameraPresets message);
		void HandleMcpeUnlockedRecipes(McpeUnlockedRecipes message);
		void HandleMcpeTrimData(McpeTrimData message);
		void HandleMcpeOpenSign(McpeOpenSign message);
		void HandleMcpeAgentAnimation(McpeAgentAnimation message);
		void HandleMcpeRefreshEntitlements(McpeRefreshEntitlements message);
		void HandleMcpeSetHud(McpeSetHud message);
		void HandleMcpeAwardAchievement(McpeAwardAchievement message);
		void HandleMcpeClientboundCloseForm(McpeClientboundCloseForm message);
		void HandleMcpeJigsawStructureData(McpeJigsawStructureData message);
		void HandleMcpeCurrentStructureFeature(McpeCurrentStructureFeature message);
		void HandleMcpeCameraAimAssist(McpeCameraAimAssist message);
		void HandleMcpeContainerRegistryCleanup(McpeContainerRegistryCleanup message);
		void HandleMcpeMovementEffect(McpeMovementEffect message);
		void HandleMcpeCameraAimAssistPresets(McpeCameraAimAssistPresets message);
		void HandleMcpePlayerVideoCapture(McpePlayerVideoCapture message);
		void HandleMcpePlayerUpdateEntityOverrides(McpePlayerUpdateEntityOverrides message);
		void HandleMcpeClientboundControlSchemeSet(McpeClientboundControlSchemeSet message);
		void HandleMcpePrimitiveShapes(McpePrimitiveShapes message);
		void HandleMcpePlayerLocation(McpePlayerLocation message);
		void HandleMcpeClientboundDataStore(McpeClientboundDataStore message);
		void HandleMcpeGraphicsOverrideParameter(McpeGraphicsOverrideParameter message);
		void HandleMcpeClientboundDataDrivenUiShowScreen(McpeClientboundDataDrivenUiShowScreen message);
		void HandleMcpeClientboundDataDrivenUiCloseScreen(McpeClientboundDataDrivenUiCloseScreen message);
		void HandleMcpeClientboundDataDrivenUiReload(McpeClientboundDataDrivenUiReload message);
		void HandleMcpeClientboundTextureShift(McpeClientboundTextureShift message);
		void HandleMcpeVoxelShapes(McpeVoxelShapes message);
		void HandleMcpeCameraSpline(McpeCameraSpline message);
		void HandleMcpeCameraAimAssistActorPriority(McpeCameraAimAssistActorPriority message);
		void HandleMcpeCameraInstruction(McpeCameraInstruction message);
		void HandleMcpeCameraShake(McpeCameraShake message);
		void HandleMcpeLocatorBar(McpeLocatorBar message);
		void HandleMcpeSyncWorldClocks(McpeSyncWorldClocks message);
		void HandleMcpeClientboundAttributeLayerSync(McpeClientboundAttributeLayerSync message);
		void HandleMcpeServerStoreInfo(McpeServerStoreInfo message);
		void HandleMcpeServerPresenceInfo(McpeServerPresenceInfo message);
		void HandleMcpeClientboundUpdateSoundData(McpeClientboundUpdateSoundData message);
		void HandleMcpeSendPartyDestinationCookie(McpeSendPartyDestinationCookie message);
		void HandleFtlCreatePlayer(FtlCreatePlayer message);
	}

	public class McpeClientMessageDispatcher
	{
		private IMcpeClientMessageHandler _messageHandler = null;

		public McpeClientMessageDispatcher(IMcpeClientMessageHandler messageHandler)
		{
			_messageHandler = messageHandler;
		}

		public bool HandlePacket(Packet message)
		{
			switch (message)
			{
				case McpePlayStatus msg:
					_messageHandler.HandleMcpePlayStatus(msg);
					break;
				case McpeServerToClientHandshake msg:
					_messageHandler.HandleMcpeServerToClientHandshake(msg);
					break;
				case McpeDisconnect msg:
					_messageHandler.HandleMcpeDisconnect(msg);
					break;
				case McpeResourcePacksInfo msg:
					_messageHandler.HandleMcpeResourcePacksInfo(msg);
					break;
				case McpeResourcePackStack msg:
					_messageHandler.HandleMcpeResourcePackStack(msg);
					break;
				case McpeText msg:
					_messageHandler.HandleMcpeText(msg);
					break;
				case McpeSetTime msg:
					_messageHandler.HandleMcpeSetTime(msg);
					break;
				case McpeStartGame msg:
					_messageHandler.HandleMcpeStartGame(msg);
					break;
				case McpeAddPlayer msg:
					_messageHandler.HandleMcpeAddPlayer(msg);
					break;
				case McpeAddEntity msg:
					_messageHandler.HandleMcpeAddEntity(msg);
					break;
				case McpeRemoveEntity msg:
					_messageHandler.HandleMcpeRemoveEntity(msg);
					break;
				case McpeAddItemEntity msg:
					_messageHandler.HandleMcpeAddItemEntity(msg);
					break;
				case McpeServerPlayerPostMovePosition msg:
					_messageHandler.HandleMcpeServerPlayerPostMovePosition(msg);
					break;
				case McpeTakeItemEntity msg:
					_messageHandler.HandleMcpeTakeItemEntity(msg);
					break;
				case McpeMoveEntity msg:
					_messageHandler.HandleMcpeMoveEntity(msg);
					break;
				case McpeMovePlayer msg:
					_messageHandler.HandleMcpeMovePlayer(msg);
					break;
				case McpeUpdateBlock msg:
					_messageHandler.HandleMcpeUpdateBlock(msg);
					break;
				case McpeAddPainting msg:
					_messageHandler.HandleMcpeAddPainting(msg);
					break;
				case McpeLevelEvent msg:
					_messageHandler.HandleMcpeLevelEvent(msg);
					break;
				case McpeBlockEvent msg:
					_messageHandler.HandleMcpeBlockEvent(msg);
					break;
				case McpeEntityEvent msg:
					_messageHandler.HandleMcpeEntityEvent(msg);
					break;
				case McpeMobEffect msg:
					_messageHandler.HandleMcpeMobEffect(msg);
					break;
				case McpeUpdateAttributes msg:
					_messageHandler.HandleMcpeUpdateAttributes(msg);
					break;
				case McpeInventoryTransaction msg:
					_messageHandler.HandleMcpeInventoryTransaction(msg);
					break;
				case McpeMobEquipment msg:
					_messageHandler.HandleMcpeMobEquipment(msg);
					break;
				case McpeMobArmorEquipment msg:
					_messageHandler.HandleMcpeMobArmorEquipment(msg);
					break;
				case McpeInteract msg:
					_messageHandler.HandleMcpeInteract(msg);
					break;
				case McpeHurtArmor msg:
					_messageHandler.HandleMcpeHurtArmor(msg);
					break;
				case McpeSetEntityData msg:
					_messageHandler.HandleMcpeSetEntityData(msg);
					break;
				case McpeSetEntityMotion msg:
					_messageHandler.HandleMcpeSetEntityMotion(msg);
					break;
				case McpeSetEntityLink msg:
					_messageHandler.HandleMcpeSetEntityLink(msg);
					break;
				case McpeSetHealth msg:
					_messageHandler.HandleMcpeSetHealth(msg);
					break;
				case McpeSetSpawnPosition msg:
					_messageHandler.HandleMcpeSetSpawnPosition(msg);
					break;
				case McpeAnimate msg:
					_messageHandler.HandleMcpeAnimate(msg);
					break;
				case McpeRespawn msg:
					_messageHandler.HandleMcpeRespawn(msg);
					break;
				case McpeContainerOpen msg:
					_messageHandler.HandleMcpeContainerOpen(msg);
					break;
				case McpeContainerClose msg:
					_messageHandler.HandleMcpeContainerClose(msg);
					break;
				case McpePlayerHotbar msg:
					_messageHandler.HandleMcpePlayerHotbar(msg);
					break;
				case McpeInventoryContent msg:
					_messageHandler.HandleMcpeInventoryContent(msg);
					break;
				case McpeInventorySlot msg:
					_messageHandler.HandleMcpeInventorySlot(msg);
					break;
				case McpeContainerSetData msg:
					_messageHandler.HandleMcpeContainerSetData(msg);
					break;
				case McpeCraftingData msg:
					_messageHandler.HandleMcpeCraftingData(msg);
					break;
				case McpeGuiDataPickItem msg:
					_messageHandler.HandleMcpeGuiDataPickItem(msg);
					break;
				case McpeBlockEntityData msg:
					_messageHandler.HandleMcpeBlockEntityData(msg);
					break;
				case McpeLevelChunk msg:
					_messageHandler.HandleMcpeLevelChunk(msg);
					break;
				case McpeSetCommandsEnabled msg:
					_messageHandler.HandleMcpeSetCommandsEnabled(msg);
					break;
				case McpeSetDifficulty msg:
					_messageHandler.HandleMcpeSetDifficulty(msg);
					break;
				case McpeChangeDimension msg:
					_messageHandler.HandleMcpeChangeDimension(msg);
					break;
				case McpeSetPlayerGameType msg:
					_messageHandler.HandleMcpeSetPlayerGameType(msg);
					break;
				case McpePlayerList msg:
					_messageHandler.HandleMcpePlayerList(msg);
					break;
				case McpeSimpleEvent msg:
					_messageHandler.HandleMcpeSimpleEvent(msg);
					break;
				case McpeTelemetryEvent msg:
					_messageHandler.HandleMcpeTelemetryEvent(msg);
					break;
				case McpeSpawnExperienceOrb msg:
					_messageHandler.HandleMcpeSpawnExperienceOrb(msg);
					break;
				case McpeClientboundMapItemData msg:
					_messageHandler.HandleMcpeClientboundMapItemData(msg);
					break;
				case McpeMapInfoRequest msg:
					_messageHandler.HandleMcpeMapInfoRequest(msg);
					break;
				case McpeRequestChunkRadius msg:
					_messageHandler.HandleMcpeRequestChunkRadius(msg);
					break;
				case McpeChunkRadiusUpdate msg:
					_messageHandler.HandleMcpeChunkRadiusUpdate(msg);
					break;
				case McpeGameRulesChanged msg:
					_messageHandler.HandleMcpeGameRulesChanged(msg);
					break;
				case McpeCamera msg:
					_messageHandler.HandleMcpeCamera(msg);
					break;
				case McpeBossEvent msg:
					_messageHandler.HandleMcpeBossEvent(msg);
					break;
				case McpeShowCredits msg:
					_messageHandler.HandleMcpeShowCredits(msg);
					break;
				case McpeAvailableCommands msg:
					_messageHandler.HandleMcpeAvailableCommands(msg);
					break;
				case McpeCommandOutput msg:
					_messageHandler.HandleMcpeCommandOutput(msg);
					break;
				case McpeUpdateTrade msg:
					_messageHandler.HandleMcpeUpdateTrade(msg);
					break;
				case McpeUpdateEquipment msg:
					_messageHandler.HandleMcpeUpdateEquipment(msg);
					break;
				case McpeResourcePackDataInfo msg:
					_messageHandler.HandleMcpeResourcePackDataInfo(msg);
					break;
				case McpeResourcePackChunkData msg:
					_messageHandler.HandleMcpeResourcePackChunkData(msg);
					break;
				case McpeTransfer msg:
					_messageHandler.HandleMcpeTransfer(msg);
					break;
				case McpePlaySound msg:
					_messageHandler.HandleMcpePlaySound(msg);
					break;
				case McpeStopSound msg:
					_messageHandler.HandleMcpeStopSound(msg);
					break;
				case McpeSetTitle msg:
					_messageHandler.HandleMcpeSetTitle(msg);
					break;
				case McpeAddBehaviorTree msg:
					_messageHandler.HandleMcpeAddBehaviorTree(msg);
					break;
				case McpeStructureBlockUpdate msg:
					_messageHandler.HandleMcpeStructureBlockUpdate(msg);
					break;
				case McpeShowStoreOffer msg:
					_messageHandler.HandleMcpeShowStoreOffer(msg);
					break;
				case McpePlayerSkin msg:
					_messageHandler.HandleMcpePlayerSkin(msg);
					break;
				case McpeSubClientLogin msg:
					_messageHandler.HandleMcpeSubClientLogin(msg);
					break;
				case McpeInitiateWebSocketConnection msg:
					_messageHandler.HandleMcpeInitiateWebSocketConnection(msg);
					break;
				case McpeSetLastHurtBy msg:
					_messageHandler.HandleMcpeSetLastHurtBy(msg);
					break;
				case McpeBookEdit msg:
					_messageHandler.HandleMcpeBookEdit(msg);
					break;
				case McpeNpcRequest msg:
					_messageHandler.HandleMcpeNpcRequest(msg);
					break;
				case McpeModalFormRequest msg:
					_messageHandler.HandleMcpeModalFormRequest(msg);
					break;
				case McpeServerSettingsResponse msg:
					_messageHandler.HandleMcpeServerSettingsResponse(msg);
					break;
				case McpeShowProfile msg:
					_messageHandler.HandleMcpeShowProfile(msg);
					break;
				case McpeSetDefaultGameType msg:
					_messageHandler.HandleMcpeSetDefaultGameType(msg);
					break;
				case McpeRemoveObjective msg:
					_messageHandler.HandleMcpeRemoveObjective(msg);
					break;
				case McpeSetDisplayObjective msg:
					_messageHandler.HandleMcpeSetDisplayObjective(msg);
					break;
				case McpeSetScore msg:
					_messageHandler.HandleMcpeSetScore(msg);
					break;
				case McpeLabTable msg:
					_messageHandler.HandleMcpeLabTable(msg);
					break;
				case McpeUpdateBlockSynced msg:
					_messageHandler.HandleMcpeUpdateBlockSynced(msg);
					break;
				case McpeMoveEntityDelta msg:
					_messageHandler.HandleMcpeMoveEntityDelta(msg);
					break;
				case McpeSetScoreboardIdentity msg:
					_messageHandler.HandleMcpeSetScoreboardIdentity(msg);
					break;
				case McpeUpdateSoftEnum msg:
					_messageHandler.HandleMcpeUpdateSoftEnum(msg);
					break;
				case McpeNetworkStackLatency msg:
					_messageHandler.HandleMcpeNetworkStackLatency(msg);
					break;
				case McpeSpawnParticleEffect msg:
					_messageHandler.HandleMcpeSpawnParticleEffect(msg);
					break;
				case McpeAvailableEntityIdentifiers msg:
					_messageHandler.HandleMcpeAvailableEntityIdentifiers(msg);
					break;
				case McpeNetworkChunkPublisherUpdate msg:
					_messageHandler.HandleMcpeNetworkChunkPublisherUpdate(msg);
					break;
				case McpeBiomeDefinitionList msg:
					_messageHandler.HandleMcpeBiomeDefinitionList(msg);
					break;
				case McpeLevelSoundEvent msg:
					_messageHandler.HandleMcpeLevelSoundEvent(msg);
					break;
				case McpeLevelEventGeneric msg:
					_messageHandler.HandleMcpeLevelEventGeneric(msg);
					break;
				case McpeLecternUpdate msg:
					_messageHandler.HandleMcpeLecternUpdate(msg);
					break;
				case McpeClientCacheStatus msg:
					_messageHandler.HandleMcpeClientCacheStatus(msg);
					break;
				case McpeOnScreenTextureAnimation msg:
					_messageHandler.HandleMcpeOnScreenTextureAnimation(msg);
					break;
				case McpeMapCreateLockedCopy msg:
					_messageHandler.HandleMcpeMapCreateLockedCopy(msg);
					break;
				case McpeStructureTemplateDataExportRequest msg:
					_messageHandler.HandleMcpeStructureTemplateDataExportRequest(msg);
					break;
				case McpeStructureTemplateDataExportResponse msg:
					_messageHandler.HandleMcpeStructureTemplateDataExportResponse(msg);
					break;
				case McpeClientCacheMissResponse msg:
					_messageHandler.HandleMcpeClientCacheMissResponse(msg);
					break;
				case McpeEducationSettings msg:
					_messageHandler.HandleMcpeEducationSettings(msg);
					break;
				case McpeEmote msg:
					_messageHandler.HandleMcpeEmote(msg);
					break;
				case McpeMultiplayerSettings msg:
					_messageHandler.HandleMcpeMultiplayerSettings(msg);
					break;
				case McpeCompletedUsingItem msg:
					_messageHandler.HandleMcpeCompletedUsingItem(msg);
					break;
				case McpeNetworkSettings msg:
					_messageHandler.HandleMcpeNetworkSettings(msg);
					break;
				case McpeCreativeContent msg:
					_messageHandler.HandleMcpeCreativeContent(msg);
					break;
				case McpePlayerEnchantOptions msg:
					_messageHandler.HandleMcpePlayerEnchantOptions(msg);
					break;
				case McpeItemStackResponse msg:
					_messageHandler.HandleMcpeItemStackResponse(msg);
					break;
				case McpePlayerArmorDamage msg:
					_messageHandler.HandleMcpePlayerArmorDamage(msg);
					break;
				case McpeCodeBuilder msg:
					_messageHandler.HandleMcpeCodeBuilder(msg);
					break;
				case McpePositionTrackingDbServerBroadcast msg:
					_messageHandler.HandleMcpePositionTrackingDbServerBroadcast(msg);
					break;
				case McpeDebugInfo msg:
					_messageHandler.HandleMcpeDebugInfo(msg);
					break;
				case McpeMotionPredictionHints msg:
					_messageHandler.HandleMcpeMotionPredictionHints(msg);
					break;
				case McpeAnimateEntity msg:
					_messageHandler.HandleMcpeAnimateEntity(msg);
					break;
				case McpePlayerFog msg:
					_messageHandler.HandleMcpePlayerFog(msg);
					break;
				case McpeCorrectPlayerMovePrediction msg:
					_messageHandler.HandleMcpeCorrectPlayerMovePrediction(msg);
					break;
				case McpeItemComponent msg:
					_messageHandler.HandleMcpeItemComponent(msg);
					break;
				case McpeClientboundDebugRenderer msg:
					_messageHandler.HandleMcpeClientboundDebugRenderer(msg);
					break;
				case McpeSyncEntityProperty msg:
					_messageHandler.HandleMcpeSyncEntityProperty(msg);
					break;
				case McpeAddVolumeEntity msg:
					_messageHandler.HandleMcpeAddVolumeEntity(msg);
					break;
				case McpeRemoveVolumeEntity msg:
					_messageHandler.HandleMcpeRemoveVolumeEntity(msg);
					break;
				case McpeSimulationType msg:
					_messageHandler.HandleMcpeSimulationType(msg);
					break;
				case McpeNpcDialogue msg:
					_messageHandler.HandleMcpeNpcDialogue(msg);
					break;
				case McpeEduUriResource msg:
					_messageHandler.HandleMcpeEduUriResource(msg);
					break;
				case McpeUpdateSubChunkBlocksPacket msg:
					_messageHandler.HandleMcpeUpdateSubChunkBlocksPacket(msg);
					break;
				case McpeSubChunkPacket msg:
					_messageHandler.HandleMcpeSubChunkPacket(msg);
					break;
				case McpePlayerStartItemCooldown msg:
					_messageHandler.HandleMcpePlayerStartItemCooldown(msg);
					break;
				case McpeScriptMessage msg:
					_messageHandler.HandleMcpeScriptMessage(msg);
					break;
				case McpeTickingAreasLoadStatus msg:
					_messageHandler.HandleMcpeTickingAreasLoadStatus(msg);
					break;
				case McpeDimensionData msg:
					_messageHandler.HandleMcpeDimensionData(msg);
					break;
				case McpeAgentActionEvent msg:
					_messageHandler.HandleMcpeAgentActionEvent(msg);
					break;
				case McpeLessonProgress msg:
					_messageHandler.HandleMcpeLessonProgress(msg);
					break;
				case McpeToastRequest msg:
					_messageHandler.HandleMcpeToastRequest(msg);
					break;
				case McpeUpdateAbilities msg:
					_messageHandler.HandleMcpeUpdateAbilities(msg);
					break;
				case McpeUpdateAdventureSettings msg:
					_messageHandler.HandleMcpeUpdateAdventureSettings(msg);
					break;
				case McpeDeathInfo msg:
					_messageHandler.HandleMcpeDeathInfo(msg);
					break;
				case McpeEditorNetwork msg:
					_messageHandler.HandleMcpeEditorNetwork(msg);
					break;
				case McpeFeatureRegistry msg:
					_messageHandler.HandleMcpeFeatureRegistry(msg);
					break;
				case McpeServerStats msg:
					_messageHandler.HandleMcpeServerStats(msg);
					break;
				case McpeGameTestResults msg:
					_messageHandler.HandleMcpeGameTestResults(msg);
					break;
				case McpeUpdateClientInputLocks msg:
					_messageHandler.HandleMcpeUpdateClientInputLocks(msg);
					break;
				case McpeCameraPresets msg:
					_messageHandler.HandleMcpeCameraPresets(msg);
					break;
				case McpeUnlockedRecipes msg:
					_messageHandler.HandleMcpeUnlockedRecipes(msg);
					break;
				case McpeTrimData msg:
					_messageHandler.HandleMcpeTrimData(msg);
					break;
				case McpeOpenSign msg:
					_messageHandler.HandleMcpeOpenSign(msg);
					break;
				case McpeAgentAnimation msg:
					_messageHandler.HandleMcpeAgentAnimation(msg);
					break;
				case McpeRefreshEntitlements msg:
					_messageHandler.HandleMcpeRefreshEntitlements(msg);
					break;
				case McpeSetHud msg:
					_messageHandler.HandleMcpeSetHud(msg);
					break;
				case McpeAwardAchievement msg:
					_messageHandler.HandleMcpeAwardAchievement(msg);
					break;
				case McpeClientboundCloseForm msg:
					_messageHandler.HandleMcpeClientboundCloseForm(msg);
					break;
				case McpeJigsawStructureData msg:
					_messageHandler.HandleMcpeJigsawStructureData(msg);
					break;
				case McpeCurrentStructureFeature msg:
					_messageHandler.HandleMcpeCurrentStructureFeature(msg);
					break;
				case McpeCameraAimAssist msg:
					_messageHandler.HandleMcpeCameraAimAssist(msg);
					break;
				case McpeContainerRegistryCleanup msg:
					_messageHandler.HandleMcpeContainerRegistryCleanup(msg);
					break;
				case McpeMovementEffect msg:
					_messageHandler.HandleMcpeMovementEffect(msg);
					break;
				case McpeCameraAimAssistPresets msg:
					_messageHandler.HandleMcpeCameraAimAssistPresets(msg);
					break;
				case McpePlayerVideoCapture msg:
					_messageHandler.HandleMcpePlayerVideoCapture(msg);
					break;
				case McpePlayerUpdateEntityOverrides msg:
					_messageHandler.HandleMcpePlayerUpdateEntityOverrides(msg);
					break;
				case McpeClientboundControlSchemeSet msg:
					_messageHandler.HandleMcpeClientboundControlSchemeSet(msg);
					break;
				case McpePrimitiveShapes msg:
					_messageHandler.HandleMcpePrimitiveShapes(msg);
					break;
				case McpePlayerLocation msg:
					_messageHandler.HandleMcpePlayerLocation(msg);
					break;
				case McpeClientboundDataStore msg:
					_messageHandler.HandleMcpeClientboundDataStore(msg);
					break;
				case McpeGraphicsOverrideParameter msg:
					_messageHandler.HandleMcpeGraphicsOverrideParameter(msg);
					break;
				case McpeClientboundDataDrivenUiShowScreen msg:
					_messageHandler.HandleMcpeClientboundDataDrivenUiShowScreen(msg);
					break;
				case McpeClientboundDataDrivenUiCloseScreen msg:
					_messageHandler.HandleMcpeClientboundDataDrivenUiCloseScreen(msg);
					break;
				case McpeClientboundDataDrivenUiReload msg:
					_messageHandler.HandleMcpeClientboundDataDrivenUiReload(msg);
					break;
				case McpeClientboundTextureShift msg:
					_messageHandler.HandleMcpeClientboundTextureShift(msg);
					break;
				case McpeVoxelShapes msg:
					_messageHandler.HandleMcpeVoxelShapes(msg);
					break;
				case McpeCameraSpline msg:
					_messageHandler.HandleMcpeCameraSpline(msg);
					break;
				case McpeCameraAimAssistActorPriority msg:
					_messageHandler.HandleMcpeCameraAimAssistActorPriority(msg);
					break;
				case McpeCameraInstruction msg:
					_messageHandler.HandleMcpeCameraInstruction(msg);
					break;
				case McpeCameraShake msg:
					_messageHandler.HandleMcpeCameraShake(msg);
					break;
				case McpeLocatorBar msg:
					_messageHandler.HandleMcpeLocatorBar(msg);
					break;
				case McpeSyncWorldClocks msg:
					_messageHandler.HandleMcpeSyncWorldClocks(msg);
					break;
				case McpeClientboundAttributeLayerSync msg:
					_messageHandler.HandleMcpeClientboundAttributeLayerSync(msg);
					break;
				case McpeServerStoreInfo msg:
					_messageHandler.HandleMcpeServerStoreInfo(msg);
					break;
				case McpeServerPresenceInfo msg:
					_messageHandler.HandleMcpeServerPresenceInfo(msg);
					break;
				case McpeClientboundUpdateSoundData msg:
					_messageHandler.HandleMcpeClientboundUpdateSoundData(msg);
					break;
				case McpeSendPartyDestinationCookie msg:
					_messageHandler.HandleMcpeSendPartyDestinationCookie(msg);
					break;
				case FtlCreatePlayer msg:
					_messageHandler.HandleFtlCreatePlayer(msg);
					break;
				default:
					return false;
			}

			return true;
		}
	}

	public class PacketFactory
	{
		public static ICustomPacketFactory CustomPacketFactory { get; set; } = null;

		public static Packet Create(int messageId, ReadOnlyMemory<byte> buffer, string ns)
		{
			Packet packet = CustomPacketFactory?.Create(messageId, buffer, ns);
			if (packet != null) return packet;

			if(ns == "raknet") 
			{
				switch (messageId)
				{
					case 0x00:
						return ConnectedPing.CreateObject().Decode(buffer);
					case 0x01:
						return UnconnectedPing.CreateObject().Decode(buffer);
					case 0x03:
						return ConnectedPong.CreateObject().Decode(buffer);
					case 0x04:
						return DetectLostConnections.CreateObject().Decode(buffer);
					case 0x1c:
						return UnconnectedPong.CreateObject().Decode(buffer);
					case 0x05:
						return OpenConnectionRequest1.CreateObject().Decode(buffer);
					case 0x06:
						return OpenConnectionReply1.CreateObject().Decode(buffer);
					case 0x07:
						return OpenConnectionRequest2.CreateObject().Decode(buffer);
					case 0x08:
						return OpenConnectionReply2.CreateObject().Decode(buffer);
					case 0x09:
						return ConnectionRequest.CreateObject().Decode(buffer);
					case 0x10:
						return ConnectionRequestAccepted.CreateObject().Decode(buffer);
					case 0x13:
						return NewIncomingConnection.CreateObject().Decode(buffer);
					case 0x14:
						return NoFreeIncomingConnections.CreateObject().Decode(buffer);
					case 0x15:
						return DisconnectionNotification.CreateObject().Decode(buffer);
					case 0x17:
						return ConnectionBanned.CreateObject().Decode(buffer);
					case 0x1A:
						return IpRecentlyConnected.CreateObject().Decode(buffer);
					case 0xfe:
						return McpeWrapper.CreateObject().Decode(buffer);
				}
			} else if(ns == "ftl") 
			{
				switch (messageId)
				{
					case 0x01:
						return FtlCreatePlayer.CreateObject().Decode(buffer);
				}
			} else {

				switch (messageId)
				{
					case 0x01:
						return McpeLogin.CreateObject().Decode(buffer);
					case 0x02:
						return McpePlayStatus.CreateObject().Decode(buffer);
					case 0x03:
						return McpeServerToClientHandshake.CreateObject().Decode(buffer);
					case 0x04:
						return McpeClientToServerHandshake.CreateObject().Decode(buffer);
					case 0x05:
						return McpeDisconnect.CreateObject().Decode(buffer);
					case 0x06:
						return McpeResourcePacksInfo.CreateObject().Decode(buffer);
					case 0x07:
						return McpeResourcePackStack.CreateObject().Decode(buffer);
					case 0x08:
						return McpeResourcePackClientResponse.CreateObject().Decode(buffer);
					case 0x09:
						return McpeText.CreateObject().Decode(buffer);
					case 0x0a:
						return McpeSetTime.CreateObject().Decode(buffer);
					case 0x0b:
						return McpeStartGame.CreateObject().Decode(buffer);
					case 0x0c:
						return McpeAddPlayer.CreateObject().Decode(buffer);
					case 0x0d:
						return McpeAddEntity.CreateObject().Decode(buffer);
					case 0x0e:
						return McpeRemoveEntity.CreateObject().Decode(buffer);
					case 0x0f:
						return McpeAddItemEntity.CreateObject().Decode(buffer);
					case 0x10:
						return McpeServerPlayerPostMovePosition.CreateObject().Decode(buffer);
					case 0x11:
						return McpeTakeItemEntity.CreateObject().Decode(buffer);
					case 0x12:
						return McpeMoveEntity.CreateObject().Decode(buffer);
					case 0x13:
						return McpeMovePlayer.CreateObject().Decode(buffer);
					case 0x15:
						return McpeUpdateBlock.CreateObject().Decode(buffer);
					case 0x16:
						return McpeAddPainting.CreateObject().Decode(buffer);
					case 0x19:
						return McpeLevelEvent.CreateObject().Decode(buffer);
					case 0x1a:
						return McpeBlockEvent.CreateObject().Decode(buffer);
					case 0x1b:
						return McpeEntityEvent.CreateObject().Decode(buffer);
					case 0x1c:
						return McpeMobEffect.CreateObject().Decode(buffer);
					case 0x1d:
						return McpeUpdateAttributes.CreateObject().Decode(buffer);
					case 0x1e:
						return McpeInventoryTransaction.CreateObject().Decode(buffer);
					case 0x1f:
						return McpeMobEquipment.CreateObject().Decode(buffer);
					case 0x20:
						return McpeMobArmorEquipment.CreateObject().Decode(buffer);
					case 0x21:
						return McpeInteract.CreateObject().Decode(buffer);
					case 0x22:
						return McpeBlockPickRequest.CreateObject().Decode(buffer);
					case 0x23:
						return McpeEntityPickRequest.CreateObject().Decode(buffer);
					case 0x24:
						return McpePlayerAction.CreateObject().Decode(buffer);
					case 0x26:
						return McpeHurtArmor.CreateObject().Decode(buffer);
					case 0x27:
						return McpeSetEntityData.CreateObject().Decode(buffer);
					case 0x28:
						return McpeSetEntityMotion.CreateObject().Decode(buffer);
					case 0x29:
						return McpeSetEntityLink.CreateObject().Decode(buffer);
					case 0x2a:
						return McpeSetHealth.CreateObject().Decode(buffer);
					case 0x2b:
						return McpeSetSpawnPosition.CreateObject().Decode(buffer);
					case 0x2c:
						return McpeAnimate.CreateObject().Decode(buffer);
					case 0x2d:
						return McpeRespawn.CreateObject().Decode(buffer);
					case 0x2e:
						return McpeContainerOpen.CreateObject().Decode(buffer);
					case 0x2f:
						return McpeContainerClose.CreateObject().Decode(buffer);
					case 0x30:
						return McpePlayerHotbar.CreateObject().Decode(buffer);
					case 0x31:
						return McpeInventoryContent.CreateObject().Decode(buffer);
					case 0x32:
						return McpeInventorySlot.CreateObject().Decode(buffer);
					case 0x33:
						return McpeContainerSetData.CreateObject().Decode(buffer);
					case 0x34:
						return McpeCraftingData.CreateObject().Decode(buffer);
					case 0x36:
						return McpeGuiDataPickItem.CreateObject().Decode(buffer);
					case 0x38:
						return McpeBlockEntityData.CreateObject().Decode(buffer);
					case 0x3a:
						return McpeLevelChunk.CreateObject().Decode(buffer);
					case 0x3b:
						return McpeSetCommandsEnabled.CreateObject().Decode(buffer);
					case 0x3c:
						return McpeSetDifficulty.CreateObject().Decode(buffer);
					case 0x3d:
						return McpeChangeDimension.CreateObject().Decode(buffer);
					case 0x3e:
						return McpeSetPlayerGameType.CreateObject().Decode(buffer);
					case 0x3f:
						return McpePlayerList.CreateObject().Decode(buffer);
					case 0x40:
						return McpeSimpleEvent.CreateObject().Decode(buffer);
					case 0x41:
						return McpeTelemetryEvent.CreateObject().Decode(buffer);
					case 0x42:
						return McpeSpawnExperienceOrb.CreateObject().Decode(buffer);
					case 0x43:
						return McpeClientboundMapItemData.CreateObject().Decode(buffer);
					case 0x44:
						return McpeMapInfoRequest.CreateObject().Decode(buffer);
					case 0x45:
						return McpeRequestChunkRadius.CreateObject().Decode(buffer);
					case 0x46:
						return McpeChunkRadiusUpdate.CreateObject().Decode(buffer);
					case 0x48:
						return McpeGameRulesChanged.CreateObject().Decode(buffer);
					case 0x49:
						return McpeCamera.CreateObject().Decode(buffer);
					case 0x4a:
						return McpeBossEvent.CreateObject().Decode(buffer);
					case 0x4b:
						return McpeShowCredits.CreateObject().Decode(buffer);
					case 0x4c:
						return McpeAvailableCommands.CreateObject().Decode(buffer);
					case 0x4d:
						return McpeCommandRequest.CreateObject().Decode(buffer);
					case 0x4e:
						return McpeCommandBlockUpdate.CreateObject().Decode(buffer);
					case 0x4f:
						return McpeCommandOutput.CreateObject().Decode(buffer);
					case 0x50:
						return McpeUpdateTrade.CreateObject().Decode(buffer);
					case 0x51:
						return McpeUpdateEquipment.CreateObject().Decode(buffer);
					case 0x52:
						return McpeResourcePackDataInfo.CreateObject().Decode(buffer);
					case 0x53:
						return McpeResourcePackChunkData.CreateObject().Decode(buffer);
					case 0x54:
						return McpeResourcePackChunkRequest.CreateObject().Decode(buffer);
					case 0x55:
						return McpeTransfer.CreateObject().Decode(buffer);
					case 0x56:
						return McpePlaySound.CreateObject().Decode(buffer);
					case 0x57:
						return McpeStopSound.CreateObject().Decode(buffer);
					case 0x58:
						return McpeSetTitle.CreateObject().Decode(buffer);
					case 0x59:
						return McpeAddBehaviorTree.CreateObject().Decode(buffer);
					case 0x5a:
						return McpeStructureBlockUpdate.CreateObject().Decode(buffer);
					case 0x5b:
						return McpeShowStoreOffer.CreateObject().Decode(buffer);
					case 0x5c:
						return McpePurchaseReceipt.CreateObject().Decode(buffer);
					case 0x5d:
						return McpePlayerSkin.CreateObject().Decode(buffer);
					case 0x5e:
						return McpeSubClientLogin.CreateObject().Decode(buffer);
					case 0x5f:
						return McpeInitiateWebSocketConnection.CreateObject().Decode(buffer);
					case 0x60:
						return McpeSetLastHurtBy.CreateObject().Decode(buffer);
					case 0x61:
						return McpeBookEdit.CreateObject().Decode(buffer);
					case 0x62:
						return McpeNpcRequest.CreateObject().Decode(buffer);
					case 0x63:
						return McpePhotoTransfer.CreateObject().Decode(buffer);
					case 0x64:
						return McpeModalFormRequest.CreateObject().Decode(buffer);
					case 0x65:
						return McpeModalFormResponse.CreateObject().Decode(buffer);
					case 0x66:
						return McpeServerSettingsRequest.CreateObject().Decode(buffer);
					case 0x67:
						return McpeServerSettingsResponse.CreateObject().Decode(buffer);
					case 0x68:
						return McpeShowProfile.CreateObject().Decode(buffer);
					case 0x69:
						return McpeSetDefaultGameType.CreateObject().Decode(buffer);
					case 0x6a:
						return McpeRemoveObjective.CreateObject().Decode(buffer);
					case 0x6b:
						return McpeSetDisplayObjective.CreateObject().Decode(buffer);
					case 0x6c:
						return McpeSetScore.CreateObject().Decode(buffer);
					case 0x6d:
						return McpeLabTable.CreateObject().Decode(buffer);
					case 0x6e:
						return McpeUpdateBlockSynced.CreateObject().Decode(buffer);
					case 0x6f:
						return McpeMoveEntityDelta.CreateObject().Decode(buffer);
					case 0x70:
						return McpeSetScoreboardIdentity.CreateObject().Decode(buffer);
					case 0x71:
						return McpeSetLocalPlayerAsInitialized.CreateObject().Decode(buffer);
					case 0x72:
						return McpeUpdateSoftEnum.CreateObject().Decode(buffer);
					case 0x73:
						return McpeNetworkStackLatency.CreateObject().Decode(buffer);
					case 0x76:
						return McpeSpawnParticleEffect.CreateObject().Decode(buffer);
					case 0x77:
						return McpeAvailableEntityIdentifiers.CreateObject().Decode(buffer);
					case 0x79:
						return McpeNetworkChunkPublisherUpdate.CreateObject().Decode(buffer);
					case 0x7a:
						return McpeBiomeDefinitionList.CreateObject().Decode(buffer);
					case 0x7b:
						return McpeLevelSoundEvent.CreateObject().Decode(buffer);
					case 0x7c:
						return McpeLevelEventGeneric.CreateObject().Decode(buffer);
					case 0x7d:
						return McpeLecternUpdate.CreateObject().Decode(buffer);
					case 0x81:
						return McpeClientCacheStatus.CreateObject().Decode(buffer);
					case 0x82:
						return McpeOnScreenTextureAnimation.CreateObject().Decode(buffer);
					case 0x83:
						return McpeMapCreateLockedCopy.CreateObject().Decode(buffer);
					case 0x84:
						return McpeStructureTemplateDataExportRequest.CreateObject().Decode(buffer);
					case 0x85:
						return McpeStructureTemplateDataExportResponse.CreateObject().Decode(buffer);
					case 0x87:
						return McpeClientCacheBlobStatus.CreateObject().Decode(buffer);
					case 0x88:
						return McpeClientCacheMissResponse.CreateObject().Decode(buffer);
					case 0x89:
						return McpeEducationSettings.CreateObject().Decode(buffer);
					case 0x8a:
						return McpeEmote.CreateObject().Decode(buffer);
					case 0x8b:
						return McpeMultiplayerSettings.CreateObject().Decode(buffer);
					case 0x8c:
						return McpeSettingsCommand.CreateObject().Decode(buffer);
					case 0x8d:
						return McpeAnvilDamage.CreateObject().Decode(buffer);
					case 0x8e:
						return McpeCompletedUsingItem.CreateObject().Decode(buffer);
					case 0x8f:
						return McpeNetworkSettings.CreateObject().Decode(buffer);
					case 0x90:
						return McpePlayerAuthInput.CreateObject().Decode(buffer);
					case 0x91:
						return McpeCreativeContent.CreateObject().Decode(buffer);
					case 0x92:
						return McpePlayerEnchantOptions.CreateObject().Decode(buffer);
					case 0x93:
						return McpeItemStackRequest.CreateObject().Decode(buffer);
					case 0x94:
						return McpeItemStackResponse.CreateObject().Decode(buffer);
					case 0x95:
						return McpePlayerArmorDamage.CreateObject().Decode(buffer);
					case 0x96:
						return McpeCodeBuilder.CreateObject().Decode(buffer);
					case 0x97:
						return McpeUpdatePlayerGameType.CreateObject().Decode(buffer);
					case 0x98:
						return McpeEmoteList.CreateObject().Decode(buffer);
					case 0x99:
						return McpePositionTrackingDbServerBroadcast.CreateObject().Decode(buffer);
					case 0x9a:
						return McpePositionTrackingDbClientRequest.CreateObject().Decode(buffer);
					case 0x9b:
						return McpeDebugInfo.CreateObject().Decode(buffer);
					case 0x9c:
						return McpePacketViolationWarning.CreateObject().Decode(buffer);
					case 0x9d:
						return McpeMotionPredictionHints.CreateObject().Decode(buffer);
					case 0x9e:
						return McpeAnimateEntity.CreateObject().Decode(buffer);
					case 0xa0:
						return McpePlayerFog.CreateObject().Decode(buffer);
					case 0xa1:
						return McpeCorrectPlayerMovePrediction.CreateObject().Decode(buffer);
					case 0xa2:
						return McpeItemComponent.CreateObject().Decode(buffer);
					case 0xa4:
						return McpeClientboundDebugRenderer.CreateObject().Decode(buffer);
					case 0xa5:
						return McpeSyncEntityProperty.CreateObject().Decode(buffer);
					case 0xa6:
						return McpeAddVolumeEntity.CreateObject().Decode(buffer);
					case 0xa7:
						return McpeRemoveVolumeEntity.CreateObject().Decode(buffer);
					case 0xa8:
						return McpeSimulationType.CreateObject().Decode(buffer);
					case 0xa9:
						return McpeNpcDialogue.CreateObject().Decode(buffer);
					case 0xaa:
						return McpeEduUriResource.CreateObject().Decode(buffer);
					case 0xab:
						return McpeCreatePhoto.CreateObject().Decode(buffer);
					case 0xac:
						return McpeUpdateSubChunkBlocksPacket.CreateObject().Decode(buffer);
					case 0xae:
						return McpeSubChunkPacket.CreateObject().Decode(buffer);
					case 0xaf:
						return McpeSubChunkRequestPacket.CreateObject().Decode(buffer);
					case 0xb0:
						return McpePlayerStartItemCooldown.CreateObject().Decode(buffer);
					case 0xb1:
						return McpeScriptMessage.CreateObject().Decode(buffer);
					case 0xb2:
						return McpeCodeBuilderSource.CreateObject().Decode(buffer);
					case 0xb3:
						return McpeTickingAreasLoadStatus.CreateObject().Decode(buffer);
					case 0xb4:
						return McpeDimensionData.CreateObject().Decode(buffer);
					case 0xb5:
						return McpeAgentActionEvent.CreateObject().Decode(buffer);
					case 0xb6:
						return McpeChangeMobProperty.CreateObject().Decode(buffer);
					case 0xb7:
						return McpeLessonProgress.CreateObject().Decode(buffer);
					case 0xb8:
						return McpeRequestAbility.CreateObject().Decode(buffer);
					case 0xb9:
						return McpeRequestPermissions.CreateObject().Decode(buffer);
					case 0xba:
						return McpeToastRequest.CreateObject().Decode(buffer);
					case 0xbb:
						return McpeUpdateAbilities.CreateObject().Decode(buffer);
					case 0xbc:
						return McpeUpdateAdventureSettings.CreateObject().Decode(buffer);
					case 0xbd:
						return McpeDeathInfo.CreateObject().Decode(buffer);
					case 0xbe:
						return McpeEditorNetwork.CreateObject().Decode(buffer);
					case 0xbf:
						return McpeFeatureRegistry.CreateObject().Decode(buffer);
					case 0xc0:
						return McpeServerStats.CreateObject().Decode(buffer);
					case 0xc1:
						return McpeRequestNetworkSettings.CreateObject().Decode(buffer);
					case 0xc2:
						return McpeGameTestRequest.CreateObject().Decode(buffer);
					case 0xc3:
						return McpeGameTestResults.CreateObject().Decode(buffer);
					case 0xc4:
						return McpeUpdateClientInputLocks.CreateObject().Decode(buffer);
					case 0xc6:
						return McpeCameraPresets.CreateObject().Decode(buffer);
					case 0xc7:
						return McpeUnlockedRecipes.CreateObject().Decode(buffer);
					case 0x12e:
						return McpeTrimData.CreateObject().Decode(buffer);
					case 0x12f:
						return McpeOpenSign.CreateObject().Decode(buffer);
					case 0x130:
						return McpeAgentAnimation.CreateObject().Decode(buffer);
					case 0x131:
						return McpeRefreshEntitlements.CreateObject().Decode(buffer);
					case 0x132:
						return McpePlayerToggleCrafterSlotRequest.CreateObject().Decode(buffer);
					case 0x133:
						return McpeSetPlayerInventoryOptions.CreateObject().Decode(buffer);
					case 0x134:
						return McpeSetHud.CreateObject().Decode(buffer);
					case 0x135:
						return McpeAwardAchievement.CreateObject().Decode(buffer);
					case 0x136:
						return McpeClientboundCloseForm.CreateObject().Decode(buffer);
					case 0x138:
						return McpeServerBoundLoadingScreen.CreateObject().Decode(buffer);
					case 0x139:
						return McpeJigsawStructureData.CreateObject().Decode(buffer);
					case 0x13a:
						return McpeCurrentStructureFeature.CreateObject().Decode(buffer);
					case 0x13b:
						return McpeServerBoundDiagnostics.CreateObject().Decode(buffer);
					case 0x13c:
						return McpeCameraAimAssist.CreateObject().Decode(buffer);
					case 0x13d:
						return McpeContainerRegistryCleanup.CreateObject().Decode(buffer);
					case 0x13e:
						return McpeMovementEffect.CreateObject().Decode(buffer);
					case 0x141:
						return McpeClientCameraAimAssist.CreateObject().Decode(buffer);
					case 0x140:
						return McpeCameraAimAssistPresets.CreateObject().Decode(buffer);
					case 0x142:
						return McpeClientMovementPredictionSync.CreateObject().Decode(buffer);
					case 0x143:
						return McpeUpdateClientOptions.CreateObject().Decode(buffer);
					case 0x144:
						return McpePlayerVideoCapture.CreateObject().Decode(buffer);
					case 0x145:
						return McpePlayerUpdateEntityOverrides.CreateObject().Decode(buffer);
					case 0x147:
						return McpeClientboundControlSchemeSet.CreateObject().Decode(buffer);
					case 0x148:
						return McpePrimitiveShapes.CreateObject().Decode(buffer);
					case 0x149:
						return McpeServerboundPackSettingChange.CreateObject().Decode(buffer);
					case 0x146:
						return McpePlayerLocation.CreateObject().Decode(buffer);
					case 0x14a:
						return McpeClientboundDataStore.CreateObject().Decode(buffer);
					case 0x14b:
						return McpeGraphicsOverrideParameter.CreateObject().Decode(buffer);
					case 0x14c:
						return McpeServerboundDataStore.CreateObject().Decode(buffer);
					case 0x14d:
						return McpeClientboundDataDrivenUiShowScreen.CreateObject().Decode(buffer);
					case 0x14e:
						return McpeClientboundDataDrivenUiCloseScreen.CreateObject().Decode(buffer);
					case 0x14f:
						return McpeClientboundDataDrivenUiReload.CreateObject().Decode(buffer);
					case 0x150:
						return McpeClientboundTextureShift.CreateObject().Decode(buffer);
					case 0x151:
						return McpeVoxelShapes.CreateObject().Decode(buffer);
					case 0x152:
						return McpeCameraSpline.CreateObject().Decode(buffer);
					case 0x153:
						return McpeCameraAimAssistActorPriority.CreateObject().Decode(buffer);
					case 0x154:
						return McpeResourcePacksReadyForValidation.CreateObject().Decode(buffer);
					case 0x12c:
						return McpeCameraInstruction.CreateObject().Decode(buffer);
					case 0x9f:
						return McpeCameraShake.CreateObject().Decode(buffer);
					case 0x155:
						return McpeLocatorBar.CreateObject().Decode(buffer);
					case 0x156:
						return McpePartyChanged.CreateObject().Decode(buffer);
					case 0x157:
						return McpeServerboundDataDrivenScreenClosed.CreateObject().Decode(buffer);
					case 0x158:
						return McpeSyncWorldClocks.CreateObject().Decode(buffer);
					case 0x159:
						return McpeClientboundAttributeLayerSync.CreateObject().Decode(buffer);
					case 0x15a:
						return McpeServerStoreInfo.CreateObject().Decode(buffer);
					case 0x15b:
						return McpeServerPresenceInfo.CreateObject().Decode(buffer);
					case 0x15c:
						return McpeClientboundUpdateSoundData.CreateObject().Decode(buffer);
					case 0x15d:
						return McpeSendPartyDestinationCookie.CreateObject().Decode(buffer);
					case 0x15e:
						return McpePartyDestinationCookieResponse.CreateObject().Decode(buffer);
				}
			}

			return null;
		}
	}

	public enum CommandPermission
	{
		Normal = 0,
		Operator = 1,
		Host = 2,
		Automation = 3,
		Admin = 4,
	}
	public enum PermissionLevel
	{
		Visitor = 0,
		Member = 1,
		Operator = 2,
		Custom = 3,
	}
	public enum ActionPermissions
	{
		BuildAndMine = 0x1,
		DoorsAndSwitches = 0x2,
		OpenContainers = 0x4,
		AttackPlayers = 0x8,
		AttackMobs = 0x10,
		Operator = 0x20,
		Teleport = 0x80,
		Default = (BuildAndMine | DoorsAndSwitches | OpenContainers | AttackPlayers | AttackMobs ),
		All = (BuildAndMine | DoorsAndSwitches | OpenContainers | AttackPlayers | AttackMobs | Operator | Teleport),
	}

	public partial class ConnectedPing : Packet<ConnectedPing>
	{

		public long sendpingtime; // = null;

		public ConnectedPing()
		{
			Id = 0x00;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(sendpingtime);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			sendpingtime = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			sendpingtime=default(long);
		}

	}

	public partial class UnconnectedPing : Packet<UnconnectedPing>
	{

		public long pingId; // = null;
		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public long guid; // = null;

		public UnconnectedPing()
		{
			Id = 0x01;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(pingId);
			Write(offlineMessageDataId);
			Write(guid);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			pingId = ReadLong();
			ReadBytes(offlineMessageDataId.Length);
			guid = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			pingId=default(long);
			guid=default(long);
		}

	}

	public partial class ConnectedPong : Packet<ConnectedPong>
	{

		public long sendpingtime; // = null;
		public long sendpongtime; // = null;

		public ConnectedPong()
		{
			Id = 0x03;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(sendpingtime);
			Write(sendpongtime);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			sendpingtime = ReadLong();
			sendpongtime = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			sendpingtime=default(long);
			sendpongtime=default(long);
		}

	}

	public partial class DetectLostConnections : Packet<DetectLostConnections>
	{


		public DetectLostConnections()
		{
			Id = 0x04;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class UnconnectedPong : Packet<UnconnectedPong>
	{

		public long pingId; // = null;
		public long serverId; // = null;
		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public string serverName; // = null;

		public UnconnectedPong()
		{
			Id = 0x1c;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(pingId);
			Write(serverId);
			Write(offlineMessageDataId);
			WriteFixedString(serverName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			pingId = ReadLong();
			serverId = ReadLong();
			ReadBytes(offlineMessageDataId.Length);
			serverName = ReadFixedString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			pingId=default(long);
			serverId=default(long);
			serverName=default(string);
		}

	}

	public partial class OpenConnectionRequest1 : Packet<OpenConnectionRequest1>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public byte raknetProtocolVersion; // = null;

		public OpenConnectionRequest1()
		{
			Id = 0x05;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);
			Write(raknetProtocolVersion);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);
			raknetProtocolVersion = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			raknetProtocolVersion=default(byte);
		}

	}

	public partial class OpenConnectionReply1 : Packet<OpenConnectionReply1>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public long serverGuid; // = null;
		public byte serverHasSecurity; // = null;
		public short mtuSize; // = null;

		public OpenConnectionReply1()
		{
			Id = 0x06;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);
			Write(serverGuid);
			Write(serverHasSecurity);
			WriteBe(mtuSize);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);
			serverGuid = ReadLong();
			serverHasSecurity = ReadByte();
			mtuSize = ReadShortBe();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverGuid=default(long);
			serverHasSecurity=default(byte);
			mtuSize=default(short);
		}

	}

	public partial class OpenConnectionRequest2 : Packet<OpenConnectionRequest2>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public IPEndPoint remoteBindingAddress; // = null;
		public short mtuSize; // = null;
		public long clientGuid; // = null;

		public OpenConnectionRequest2()
		{
			Id = 0x07;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);
			Write(remoteBindingAddress);
			WriteBe(mtuSize);
			Write(clientGuid);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);
			remoteBindingAddress = ReadIPEndPoint();
			mtuSize = ReadShortBe();
			clientGuid = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			remoteBindingAddress=default(IPEndPoint);
			mtuSize=default(short);
			clientGuid=default(long);
		}

	}

	public partial class OpenConnectionReply2 : Packet<OpenConnectionReply2>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public long serverGuid; // = null;
		public IPEndPoint clientEndpoint; // = null;
		public short mtuSize; // = null;
		public byte[] doSecurityAndHandshake; // = null;

		public OpenConnectionReply2()
		{
			Id = 0x08;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);
			Write(serverGuid);
			Write(clientEndpoint);
			WriteBe(mtuSize);
			Write(doSecurityAndHandshake);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);
			serverGuid = ReadLong();
			clientEndpoint = ReadIPEndPoint();
			mtuSize = ReadShortBe();
			doSecurityAndHandshake = ReadBytes(0, true);

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverGuid=default(long);
			clientEndpoint=default(IPEndPoint);
			mtuSize=default(short);
			doSecurityAndHandshake=default(byte[]);
		}

	}

	public partial class ConnectionRequest : Packet<ConnectionRequest>
	{

		public long clientGuid; // = null;
		public long timestamp; // = null;
		public byte doSecurity; // = null;

		public ConnectionRequest()
		{
			Id = 0x09;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(clientGuid);
			Write(timestamp);
			Write(doSecurity);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			clientGuid = ReadLong();
			timestamp = ReadLong();
			doSecurity = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			clientGuid=default(long);
			timestamp=default(long);
			doSecurity=default(byte);
		}

	}

	public partial class ConnectionRequestAccepted : Packet<ConnectionRequestAccepted>
	{

		public IPEndPoint systemAddress; // = null;
		public short systemIndex; // = null;
		public IPEndPoint[] systemAddresses; // = null;
		public long incomingTimestamp; // = null;
		public long serverTimestamp; // = null;

		public ConnectionRequestAccepted()
		{
			Id = 0x10;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(systemAddress);
			WriteBe(systemIndex);
			Write(systemAddresses);
			Write(incomingTimestamp);
			Write(serverTimestamp);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			systemAddress = ReadIPEndPoint();
			systemIndex = ReadShortBe();
			systemAddresses = ReadIPEndPoints(20);
			incomingTimestamp = ReadLong();
			serverTimestamp = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			systemAddress=default(IPEndPoint);
			systemIndex=default(short);
			systemAddresses=default(IPEndPoint[]);
			incomingTimestamp=default(long);
			serverTimestamp=default(long);
		}

	}

	public partial class NewIncomingConnection : Packet<NewIncomingConnection>
	{

		public IPEndPoint clientendpoint; // = null;
		public IPEndPoint[] systemAddresses; // = null;
		public long incomingTimestamp; // = null;
		public long serverTimestamp; // = null;

		public NewIncomingConnection()
		{
			Id = 0x13;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(clientendpoint);
			Write(systemAddresses);
			Write(incomingTimestamp);
			Write(serverTimestamp);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			clientendpoint = ReadIPEndPoint();
			systemAddresses = ReadIPEndPoints(20);
			incomingTimestamp = ReadLong();
			serverTimestamp = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			clientendpoint=default(IPEndPoint);
			systemAddresses=default(IPEndPoint[]);
			incomingTimestamp=default(long);
			serverTimestamp=default(long);
		}

	}

	public partial class NoFreeIncomingConnections : Packet<NoFreeIncomingConnections>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public long serverGuid; // = null;

		public NoFreeIncomingConnections()
		{
			Id = 0x14;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);
			Write(serverGuid);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);
			serverGuid = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverGuid=default(long);
		}

	}

	public partial class DisconnectionNotification : Packet<DisconnectionNotification>
	{


		public DisconnectionNotification()
		{
			Id = 0x15;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class ConnectionBanned : Packet<ConnectionBanned>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };
		public long serverGuid; // = null;

		public ConnectionBanned()
		{
			Id = 0x17;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);
			Write(serverGuid);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);
			serverGuid = ReadLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverGuid=default(long);
		}

	}

	public partial class IpRecentlyConnected : Packet<IpRecentlyConnected>
	{

		public readonly byte[] offlineMessageDataId = new byte[]{ 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 }; // = { 0x00, 0xff, 0xff, 0x00, 0xfe, 0xfe, 0xfe, 0xfe, 0xfd, 0xfd, 0xfd, 0xfd, 0x12, 0x34, 0x56, 0x78 };

		public IpRecentlyConnected()
		{
			Id = 0x1a;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offlineMessageDataId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			ReadBytes(offlineMessageDataId.Length);

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeLogin : Packet<McpeLogin>
	{

		public int protocolVersion; // = null;
		public byte[] payload; // = null;

		public McpeLogin()
		{
			Id = 0x01;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteBe(protocolVersion);
			WriteByteArray(payload);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			protocolVersion = ReadIntBe();
			payload = ReadByteArray();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			protocolVersion=default(int);
			payload=default(byte[]);
		}

	}

	public partial class McpePlayStatus : Packet<McpePlayStatus>
	{
		public enum PlayStatus
		{
			LoginSuccess = 0,
			LoginFailedClient = 1,
			LoginFailedServer = 2,
			PlayerSpawn = 3,
			LoginFailedInvalidTenant = 4,
			LoginFailedVanillaEdu = 5,
			LoginFailedEduVanilla = 6,
			LoginFailedServerFull = 7,
			LoginFailedEditorVanillaMismatch = 8,
			LoginFailedVanillaEditorMismatch = 9,
		}

		public int status; // = null;

		public McpePlayStatus()
		{
			Id = 0x02;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteBe(status);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			status = ReadIntBe();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			status=default(int);
		}

	}

	public partial class McpeServerToClientHandshake : Packet<McpeServerToClientHandshake>
	{

		public string token; // = null;

		public McpeServerToClientHandshake()
		{
			Id = 0x03;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(token);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			token = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			token=default(string);
		}

	}

	public partial class McpeClientToServerHandshake : Packet<McpeClientToServerHandshake>
	{


		public McpeClientToServerHandshake()
		{
			Id = 0x04;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeDisconnect : Packet<McpeDisconnect>
	{

		public int reason; // = null;
		public bool hideDisconnectReason; // = null;

		public McpeDisconnect()
		{
			Id = 0x05;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(reason);
			Write(hideDisconnectReason);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			reason = ReadSignedVarInt();
			hideDisconnectReason = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			reason=default(int);
			hideDisconnectReason=default(bool);
		}

	}

	public partial class McpeResourcePacksInfo : Packet<McpeResourcePacksInfo>
	{

		public bool mustAccept; // = null;
		public bool hasAddonPacks; // = null;
		public bool hasScripts; // = null;
		public bool disableVibrantVisuals; // = null;
		public UUID worldTemplateId; // = null;
		public string worldTemplateVersion; // = null;
		public TexturePackInfos texturepacks; // = null;

		public McpeResourcePacksInfo()
		{
			Id = 0x06;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(mustAccept);
			Write(hasAddonPacks);
			Write(hasScripts);
			Write(disableVibrantVisuals);
			Write(worldTemplateId);
			Write(worldTemplateVersion);
			Write(texturepacks);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			mustAccept = ReadBool();
			hasAddonPacks = ReadBool();
			hasScripts = ReadBool();
			disableVibrantVisuals = ReadBool();
			worldTemplateId = ReadUUID();
			worldTemplateVersion = ReadString();
			texturepacks = ReadTexturePackInfos();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			mustAccept=default(bool);
			hasAddonPacks=default(bool);
			hasScripts=default(bool);
			disableVibrantVisuals=default(bool);
			worldTemplateId=default(UUID);
			worldTemplateVersion=default(string);
			texturepacks=default(TexturePackInfos);
		}

	}

	public partial class McpeResourcePackStack : Packet<McpeResourcePackStack>
	{

		public bool mustAccept; // = null;
		public ResourcePackIdVersions resourcepackidversions; // = null;
		public string gameVersion; // = null;
		public Experiments experiments; // = null;
		public bool experimentsPreviouslyToggled; // = null;
		public bool hasEditorPacks; // = null;

		public McpeResourcePackStack()
		{
			Id = 0x07;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(mustAccept);
			Write(resourcepackidversions);
			Write(gameVersion);
			Write(experiments);
			Write(experimentsPreviouslyToggled);
			Write(hasEditorPacks);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			mustAccept = ReadBool();
			resourcepackidversions = ReadResourcePackIdVersions();
			gameVersion = ReadString();
			experiments = ReadExperiments();
			experimentsPreviouslyToggled = ReadBool();
			hasEditorPacks = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			mustAccept=default(bool);
			resourcepackidversions=default(ResourcePackIdVersions);
			gameVersion=default(string);
			experiments=default(Experiments);
			experimentsPreviouslyToggled=default(bool);
			hasEditorPacks=default(bool);
		}

	}

	public partial class McpeResourcePackClientResponse : Packet<McpeResourcePackClientResponse>
	{
		public enum ResponseStatus
		{
			None = 0,
			Refused = 1,
			SendPacks = 2,
			HaveAllPacks = 3,
			Completed = 4,
		}

		public byte responseStatus; // = null;
		public ResourcePackIds resourcepackids; // = null;

		public McpeResourcePackClientResponse()
		{
			Id = 0x08;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(responseStatus);
			Write(resourcepackids);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			responseStatus = ReadByte();
			resourcepackids = ReadResourcePackIds();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			responseStatus=default(byte);
			resourcepackids=default(ResourcePackIds);
		}

	}

	public partial class McpeText : Packet<McpeText>
	{
		public enum ChatTypes
		{
			Raw = 0,
			Chat = 1,
			Translation = 2,
			Popup = 3,
			Jukeboxpopup = 4,
			Tip = 5,
			System = 6,
			Whisper = 7,
			Announcement = 8,
			Jsonwhisper = 9,
			Json = 10,
			Jsonannouncement = 11,
		}


		public McpeText()
		{
			Id = 0x09;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeSetTime : Packet<McpeSetTime>
	{

		public int time; // = null;

		public McpeSetTime()
		{
			Id = 0x0a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(time);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			time = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			time=default(int);
		}

	}

	public partial class McpeStartGame : Packet<McpeStartGame>
	{


		public McpeStartGame()
		{
			Id = 0x0b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeAddPlayer : Packet<McpeAddPlayer>
	{

		public UUID uuid; // = null;
		public string username; // = null;
		public long runtimeEntityId; // = null;
		public string platformChatId; // = null;
		public float x; // = null;
		public float y; // = null;
		public float z; // = null;
		public float speedX; // = null;
		public float speedY; // = null;
		public float speedZ; // = null;
		public float pitch; // = null;
		public float yaw; // = null;
		public float headYaw; // = null;

		public McpeAddPlayer()
		{
			Id = 0x0c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(uuid);
			Write(username);
			WriteUnsignedVarLong(runtimeEntityId);
			Write(platformChatId);
			Write(x);
			Write(y);
			Write(z);
			Write(speedX);
			Write(speedY);
			Write(speedZ);
			Write(pitch);
			Write(yaw);
			Write(headYaw);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			uuid = ReadUUID();
			username = ReadString();
			runtimeEntityId = ReadUnsignedVarLong();
			platformChatId = ReadString();
			x = ReadFloat();
			y = ReadFloat();
			z = ReadFloat();
			speedX = ReadFloat();
			speedY = ReadFloat();
			speedZ = ReadFloat();
			pitch = ReadFloat();
			yaw = ReadFloat();
			headYaw = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			uuid=default(UUID);
			username=default(string);
			runtimeEntityId=default(long);
			platformChatId=default(string);
			x=default(float);
			y=default(float);
			z=default(float);
			speedX=default(float);
			speedY=default(float);
			speedZ=default(float);
			pitch=default(float);
			yaw=default(float);
			headYaw=default(float);
		}

	}

	public partial class McpeAddEntity : Packet<McpeAddEntity>
	{

		public long entityIdSelf; // = null;
		public long runtimeEntityId; // = null;
		public string entityType; // = null;
		public float x; // = null;
		public float y; // = null;
		public float z; // = null;
		public float speedX; // = null;
		public float speedY; // = null;
		public float speedZ; // = null;
		public float pitch; // = null;
		public float yaw; // = null;
		public float headYaw; // = null;
		public float bodyYaw; // = null;
		public EntityAttributes attributes; // = null;
		public MetadataDictionary metadata; // = null;

		public McpeAddEntity()
		{
			Id = 0x0d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(entityIdSelf);
			WriteUnsignedVarLong(runtimeEntityId);
			Write(entityType);
			Write(x);
			Write(y);
			Write(z);
			Write(speedX);
			Write(speedY);
			Write(speedZ);
			Write(pitch);
			Write(yaw);
			Write(headYaw);
			Write(bodyYaw);
			Write(attributes);
			Write(metadata);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityIdSelf = ReadSignedVarLong();
			runtimeEntityId = ReadUnsignedVarLong();
			entityType = ReadString();
			x = ReadFloat();
			y = ReadFloat();
			z = ReadFloat();
			speedX = ReadFloat();
			speedY = ReadFloat();
			speedZ = ReadFloat();
			pitch = ReadFloat();
			yaw = ReadFloat();
			headYaw = ReadFloat();
			bodyYaw = ReadFloat();
			attributes = ReadEntityAttributes();
			metadata = ReadMetadataDictionary();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityIdSelf=default(long);
			runtimeEntityId=default(long);
			entityType=default(string);
			x=default(float);
			y=default(float);
			z=default(float);
			speedX=default(float);
			speedY=default(float);
			speedZ=default(float);
			pitch=default(float);
			yaw=default(float);
			headYaw=default(float);
			bodyYaw=default(float);
			attributes=default(EntityAttributes);
			metadata=default(MetadataDictionary);
		}

	}

	public partial class McpeRemoveEntity : Packet<McpeRemoveEntity>
	{

		public long entityIdSelf; // = null;

		public McpeRemoveEntity()
		{
			Id = 0x0e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(entityIdSelf);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityIdSelf = ReadSignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityIdSelf=default(long);
		}

	}

	public partial class McpeAddItemEntity : Packet<McpeAddItemEntity>
	{

		public long entityIdSelf; // = null;
		public long runtimeEntityId; // = null;
		public Item item; // = null;
		public float x; // = null;
		public float y; // = null;
		public float z; // = null;
		public float speedX; // = null;
		public float speedY; // = null;
		public float speedZ; // = null;
		public MetadataDictionary metadata; // = null;
		public bool isFromFishing; // = null;

		public McpeAddItemEntity()
		{
			Id = 0x0f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(entityIdSelf);
			WriteUnsignedVarLong(runtimeEntityId);
			WriteItemInstance(item);
			Write(x);
			Write(y);
			Write(z);
			Write(speedX);
			Write(speedY);
			Write(speedZ);
			Write(metadata);
			Write(isFromFishing);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityIdSelf = ReadSignedVarLong();
			runtimeEntityId = ReadUnsignedVarLong();
			item = ReadItemInstance();
			x = ReadFloat();
			y = ReadFloat();
			z = ReadFloat();
			speedX = ReadFloat();
			speedY = ReadFloat();
			speedZ = ReadFloat();
			metadata = ReadMetadataDictionary();
			isFromFishing = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityIdSelf=default(long);
			runtimeEntityId=default(long);
			item=default(Item);
			x=default(float);
			y=default(float);
			z=default(float);
			speedX=default(float);
			speedY=default(float);
			speedZ=default(float);
			metadata=default(MetadataDictionary);
			isFromFishing=default(bool);
		}

	}

	public partial class McpeServerPlayerPostMovePosition : Packet<McpeServerPlayerPostMovePosition>
	{

		public Vector3 position; // = null;

		public McpeServerPlayerPostMovePosition()
		{
			Id = 0x10;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(position);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			position = ReadVector3();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			position=default(Vector3);
		}

	}

	public partial class McpeTakeItemEntity : Packet<McpeTakeItemEntity>
	{

		public long runtimeEntityId; // = null;
		public long target; // = null;

		public McpeTakeItemEntity()
		{
			Id = 0x11;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			WriteUnsignedVarLong(target);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			target = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			target=default(long);
		}

	}

	public partial class McpeMoveEntity : Packet<McpeMoveEntity>
	{

		public long runtimeEntityId; // = null;
		public byte flags; // = null;
		public PlayerLocation position; // = null;

		public McpeMoveEntity()
		{
			Id = 0x12;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(flags);
			Write(position);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			flags = ReadByte();
			position = ReadPlayerLocation();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			flags=default(byte);
			position=default(PlayerLocation);
		}

	}

	public partial class McpeMovePlayer : Packet<McpeMovePlayer>
	{
		public enum Mode
		{
			Normal = 0,
			Reset = 1,
			Teleport = 2,
			Rotation = 3,
		}
		public enum Teleportcause
		{
			Unknown = 0,
			Projectile = 1,
			ChorusFruit = 2,
			Command = 3,
			Behavior = 4,
			Count = 5,
		}

		public long runtimeEntityId; // = null;
		public float x; // = null;
		public float y; // = null;
		public float z; // = null;
		public float pitch; // = null;
		public float yaw; // = null;
		public float headYaw; // = null;
		public byte mode; // = null;
		public bool onGround; // = null;
		public long otherRuntimeEntityId; // = null;

		public McpeMovePlayer()
		{
			Id = 0x13;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(x);
			Write(y);
			Write(z);
			Write(pitch);
			Write(yaw);
			Write(headYaw);
			Write(mode);
			Write(onGround);
			WriteUnsignedVarLong(otherRuntimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			x = ReadFloat();
			y = ReadFloat();
			z = ReadFloat();
			pitch = ReadFloat();
			yaw = ReadFloat();
			headYaw = ReadFloat();
			mode = ReadByte();
			onGround = ReadBool();
			otherRuntimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			x=default(float);
			y=default(float);
			z=default(float);
			pitch=default(float);
			yaw=default(float);
			headYaw=default(float);
			mode=default(byte);
			onGround=default(bool);
			otherRuntimeEntityId=default(long);
		}

	}

	public partial class McpeUpdateBlock : Packet<McpeUpdateBlock>
	{
		public enum Flags
		{
			None = 0,
			Neighbors = 1,
			Network = 2,
			Nographic = 4,
			Priority = 8,
			All = (Neighbors | Network),
			AllPriority = (All | Priority),
		}

		public BlockCoordinates coordinates; // = null;
		public uint blockRuntimeId; // = null;
		public uint blockPriority; // = null;
		public uint storage; // = null;

		public McpeUpdateBlock()
		{
			Id = 0x15;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(coordinates);
			WriteUnsignedVarInt(blockRuntimeId);
			WriteUnsignedVarInt(blockPriority);
			WriteUnsignedVarInt(storage);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			coordinates = ReadBlockCoordinates();
			blockRuntimeId = ReadUnsignedVarInt();
			blockPriority = ReadUnsignedVarInt();
			storage = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			coordinates=default(BlockCoordinates);
			blockRuntimeId=default(uint);
			blockPriority=default(uint);
			storage=default(uint);
		}

	}

	public partial class McpeAddPainting : Packet<McpeAddPainting>
	{

		public long entityIdSelf; // = null;
		public long runtimeEntityId; // = null;
		public Vector3 coordinates; // = null;
		public int direction; // = null;
		public string title; // = null;

		public McpeAddPainting()
		{
			Id = 0x16;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(entityIdSelf);
			WriteUnsignedVarLong(runtimeEntityId);
			Write(coordinates);
			WriteSignedVarInt(direction);
			Write(title);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityIdSelf = ReadSignedVarLong();
			runtimeEntityId = ReadUnsignedVarLong();
			coordinates = ReadVector3();
			direction = ReadSignedVarInt();
			title = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityIdSelf=default(long);
			runtimeEntityId=default(long);
			coordinates=default(Vector3);
			direction=default(int);
			title=default(string);
		}

	}

	public partial class McpeLevelEvent : Packet<McpeLevelEvent>
	{

		public int eventId; // = null;
		public Vector3 position; // = null;
		public int data; // = null;

		public McpeLevelEvent()
		{
			Id = 0x19;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(eventId);
			Write(position);
			WriteSignedVarInt(data);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			eventId = ReadSignedVarInt();
			position = ReadVector3();
			data = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			eventId=default(int);
			position=default(Vector3);
			data=default(int);
		}

	}

	public partial class McpeBlockEvent : Packet<McpeBlockEvent>
	{

		public BlockCoordinates coordinates; // = null;
		public int case1; // = null;
		public int case2; // = null;

		public McpeBlockEvent()
		{
			Id = 0x1a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(coordinates);
			WriteSignedVarInt(case1);
			WriteSignedVarInt(case2);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			coordinates = ReadBlockCoordinates();
			case1 = ReadSignedVarInt();
			case2 = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			coordinates=default(BlockCoordinates);
			case1=default(int);
			case2=default(int);
		}

	}

	public partial class McpeEntityEvent : Packet<McpeEntityEvent>
	{

		public long runtimeEntityId; // = null;
		public byte eventId; // = null;
		public int data; // = null;

		public McpeEntityEvent()
		{
			Id = 0x1b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(eventId);
			WriteSignedVarInt(data);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			eventId = ReadByte();
			data = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			eventId=default(byte);
			data=default(int);
		}

	}

	public partial class McpeMobEffect : Packet<McpeMobEffect>
	{

		public long runtimeEntityId; // = null;
		public byte eventId; // = null;
		public int effectId; // = null;
		public int amplifier; // = null;
		public bool particles; // = null;
		public int duration; // = null;
		public long tick; // = null;
		public bool ambient; // = null;

		public McpeMobEffect()
		{
			Id = 0x1c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(eventId);
			WriteSignedVarInt(effectId);
			WriteSignedVarInt(amplifier);
			Write(particles);
			WriteSignedVarInt(duration);
			WriteUnsignedVarLong(tick);
			Write(ambient);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			eventId = ReadByte();
			effectId = ReadSignedVarInt();
			amplifier = ReadSignedVarInt();
			particles = ReadBool();
			duration = ReadSignedVarInt();
			tick = ReadUnsignedVarLong();
			ambient = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			eventId=default(byte);
			effectId=default(int);
			amplifier=default(int);
			particles=default(bool);
			duration=default(int);
			tick=default(long);
			ambient=default(bool);
		}

	}

	public partial class McpeUpdateAttributes : Packet<McpeUpdateAttributes>
	{

		public long runtimeEntityId; // = null;
		public PlayerAttributes attributes; // = null;
		public long tick; // = null;

		public McpeUpdateAttributes()
		{
			Id = 0x1d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(attributes);
			WriteUnsignedVarLong(tick);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			attributes = ReadPlayerAttributes();
			tick = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			attributes=default(PlayerAttributes);
			tick=default(long);
		}

	}

	public partial class McpeInventoryTransaction : Packet<McpeInventoryTransaction>
	{
		public enum TransactionType
		{
			Normal = 0,
			InventoryMismatch = 1,
			ItemUse = 2,
			ItemUseOnEntity = 3,
			ItemRelease = 4,
		}
		public enum InventorySourceType
		{
			Container = 0,
			Global = 1,
			WorldInteraction = 2,
			Creative = 3,
			Crafting = 100,
			Unspecified = 99999,
		}
		public enum CraftingAction
		{
			CraftAddIngredient = -2,
			CraftRemoveIngredient = -3,
			CraftResult = -4,
			CraftUseIngredient = -5,
			AnvilInput = -10,
			AnvilMaterial = -11,
			AnvilResult = -12,
			AnvilOutput = -13,
			EnchantItem = -15,
			EnchantLapis = -16,
			EnchantResult = -17,
			Drop = -100,
		}
		public enum ItemReleaseAction
		{
			Release = 0,
			Use = 1,
		}
		public enum ItemUseAction
		{
			Place,Clickblock = 0,
			Use,Clickair = 1,
			Destroy = 2,
		}
		public enum ItemUseOnEntityAction
		{
			Interact = 0,
			Attack = 1,
			ItemInteract = 2,
		}

		public Transaction transaction; // = null;

		public McpeInventoryTransaction()
		{
			Id = 0x1e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(transaction);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			transaction = ReadTransaction();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			transaction=default(Transaction);
		}

	}

	public partial class McpeMobEquipment : Packet<McpeMobEquipment>
	{

		public long runtimeEntityId; // = null;
		public Item item; // = null;
		public byte slot; // = null;
		public byte selectedSlot; // = null;
		public byte windowsId; // = null;

		public McpeMobEquipment()
		{
			Id = 0x1f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(item);
			Write(slot);
			Write(selectedSlot);
			Write(windowsId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			item = ReadItem();
			slot = ReadByte();
			selectedSlot = ReadByte();
			windowsId = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			item=default(Item);
			slot=default(byte);
			selectedSlot=default(byte);
			windowsId=default(byte);
		}

	}

	public partial class McpeMobArmorEquipment : Packet<McpeMobArmorEquipment>
	{

		public long runtimeEntityId; // = null;
		public Item helmet; // = null;
		public Item chestplate; // = null;
		public Item leggings; // = null;
		public Item boots; // = null;
		public Item body; // = null;

		public McpeMobArmorEquipment()
		{
			Id = 0x20;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(helmet);
			Write(chestplate);
			Write(leggings);
			Write(boots);
			Write(body);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			helmet = ReadItem();
			chestplate = ReadItem();
			leggings = ReadItem();
			boots = ReadItem();
			body = ReadItem();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			helmet=default(Item);
			chestplate=default(Item);
			leggings=default(Item);
			boots=default(Item);
			body=default(Item);
		}

	}

	public partial class McpeInteract : Packet<McpeInteract>
	{
		public enum Actions
		{
			RightClick = 1,
			LeftClick = 2,
			LeaveVehicle = 3,
			MouseOver = 4,
			OpenNpc = 5,
			OpenInventory = 6,
		}

		public byte actionId; // = null;
		public long targetRuntimeEntityId; // = null;

		public McpeInteract()
		{
			Id = 0x21;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(actionId);
			WriteUnsignedVarLong(targetRuntimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			actionId = ReadByte();
			targetRuntimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			actionId=default(byte);
			targetRuntimeEntityId=default(long);
		}

	}

	public partial class McpeBlockPickRequest : Packet<McpeBlockPickRequest>
	{

		public int x; // = null;
		public int y; // = null;
		public int z; // = null;
		public bool addUserData; // = null;
		public byte selectedSlot; // = null;

		public McpeBlockPickRequest()
		{
			Id = 0x22;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(x);
			WriteSignedVarInt(y);
			WriteSignedVarInt(z);
			Write(addUserData);
			Write(selectedSlot);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			x = ReadSignedVarInt();
			y = ReadSignedVarInt();
			z = ReadSignedVarInt();
			addUserData = ReadBool();
			selectedSlot = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			x=default(int);
			y=default(int);
			z=default(int);
			addUserData=default(bool);
			selectedSlot=default(byte);
		}

	}

	public partial class McpeEntityPickRequest : Packet<McpeEntityPickRequest>
	{

		public ulong runtimeEntityId; // = null;
		public byte selectedSlot; // = null;
		public bool addUserData; // = null;

		public McpeEntityPickRequest()
		{
			Id = 0x23;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(runtimeEntityId);
			Write(selectedSlot);
			Write(addUserData);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUlong();
			selectedSlot = ReadByte();
			addUserData = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(ulong);
			selectedSlot=default(byte);
			addUserData=default(bool);
		}

	}

	public partial class McpePlayerAction : Packet<McpePlayerAction>
	{

		public long runtimeEntityId; // = null;
		public int actionId; // = null;
		public BlockCoordinates coordinates; // = null;
		public BlockCoordinates resultCoordinates; // = null;
		public int face; // = null;

		public McpePlayerAction()
		{
			Id = 0x24;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			WriteSignedVarInt(actionId);
			Write(coordinates);
			Write(resultCoordinates);
			WriteSignedVarInt(face);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			actionId = ReadSignedVarInt();
			coordinates = ReadBlockCoordinates();
			resultCoordinates = ReadBlockCoordinates();
			face = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			actionId=default(int);
			coordinates=default(BlockCoordinates);
			resultCoordinates=default(BlockCoordinates);
			face=default(int);
		}

	}

	public partial class McpeHurtArmor : Packet<McpeHurtArmor>
	{

		public int cause; // = null;
		public int health; // = null;
		public long armorSlotFlags; // = null;

		public McpeHurtArmor()
		{
			Id = 0x26;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(cause);
			WriteSignedVarInt(health);
			WriteUnsignedVarLong(armorSlotFlags);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			cause = ReadSignedVarInt();
			health = ReadSignedVarInt();
			armorSlotFlags = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			cause=default(int);
			health=default(int);
			armorSlotFlags=default(long);
		}

	}

	public partial class McpeSetEntityData : Packet<McpeSetEntityData>
	{

		public long runtimeEntityId; // = null;
		public MetadataDictionary metadata; // = null;

		public McpeSetEntityData()
		{
			Id = 0x27;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(metadata);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			metadata = ReadMetadataDictionary();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			metadata=default(MetadataDictionary);
		}

	}

	public partial class McpeSetEntityMotion : Packet<McpeSetEntityMotion>
	{

		public long runtimeEntityId; // = null;
		public Vector3 velocity; // = null;

		public McpeSetEntityMotion()
		{
			Id = 0x28;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(velocity);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			velocity = ReadVector3();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			velocity=default(Vector3);
		}

	}

	public partial class McpeSetEntityLink : Packet<McpeSetEntityLink>
	{
		public enum LinkActions
		{
			Remove = 0,
			Ride = 1,
			Passenger = 2,
		}

		public long riddenId; // = null;
		public long riderId; // = null;
		public byte linkType; // = null;
		public bool immediate; // = null;
		public bool riderInitiated; // = null;
		public float vehicleAngularVelocity; // = null;

		public McpeSetEntityLink()
		{
			Id = 0x29;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(riddenId);
			WriteSignedVarLong(riderId);
			Write(linkType);
			Write(immediate);
			Write(riderInitiated);
			Write(vehicleAngularVelocity);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			riddenId = ReadSignedVarLong();
			riderId = ReadSignedVarLong();
			linkType = ReadByte();
			immediate = ReadBool();
			riderInitiated = ReadBool();
			vehicleAngularVelocity = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			riddenId=default(long);
			riderId=default(long);
			linkType=default(byte);
			immediate=default(bool);
			riderInitiated=default(bool);
			vehicleAngularVelocity=default(float);
		}

	}

	public partial class McpeSetHealth : Packet<McpeSetHealth>
	{

		public int health; // = null;

		public McpeSetHealth()
		{
			Id = 0x2a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(health);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			health = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			health=default(int);
		}

	}

	public partial class McpeSetSpawnPosition : Packet<McpeSetSpawnPosition>
	{

		public int spawnType; // = null;
		public BlockCoordinates coordinates; // = null;
		public int dimension; // = null;
		public BlockCoordinates unknownCoordinates; // = null;

		public McpeSetSpawnPosition()
		{
			Id = 0x2b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(spawnType);
			Write(coordinates);
			WriteSignedVarInt(dimension);
			Write(unknownCoordinates);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			spawnType = ReadSignedVarInt();
			coordinates = ReadBlockCoordinates();
			dimension = ReadSignedVarInt();
			unknownCoordinates = ReadBlockCoordinates();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			spawnType=default(int);
			coordinates=default(BlockCoordinates);
			dimension=default(int);
			unknownCoordinates=default(BlockCoordinates);
		}

	}

	public partial class McpeAnimate : Packet<McpeAnimate>
	{

		public byte actionId; // = null;
		public long runtimeEntityId; // = null;

		public McpeAnimate()
		{
			Id = 0x2c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(actionId);
			WriteUnsignedVarLong(runtimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			actionId = ReadByte();
			runtimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			actionId=default(byte);
			runtimeEntityId=default(long);
		}

	}

	public partial class McpeRespawn : Packet<McpeRespawn>
	{
		public enum RespawnState
		{
			Search = 0,
			Ready = 1,
			ClientReady = 2,
		}

		public float x; // = null;
		public float y; // = null;
		public float z; // = null;
		public byte state; // = null;
		public long runtimeEntityId; // = null;

		public McpeRespawn()
		{
			Id = 0x2d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(x);
			Write(y);
			Write(z);
			Write(state);
			WriteUnsignedVarLong(runtimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			x = ReadFloat();
			y = ReadFloat();
			z = ReadFloat();
			state = ReadByte();
			runtimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			x=default(float);
			y=default(float);
			z=default(float);
			state=default(byte);
			runtimeEntityId=default(long);
		}

	}

	public partial class McpeContainerOpen : Packet<McpeContainerOpen>
	{

		public byte windowId; // = null;
		public byte type; // = null;
		public BlockCoordinates coordinates; // = null;
		public long actorUniqueId; // = null;

		public McpeContainerOpen()
		{
			Id = 0x2e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(windowId);
			Write(type);
			Write(coordinates);
			WriteSignedVarLong(actorUniqueId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			windowId = ReadByte();
			type = ReadByte();
			coordinates = ReadBlockCoordinates();
			actorUniqueId = ReadSignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			windowId=default(byte);
			type=default(byte);
			coordinates=default(BlockCoordinates);
			actorUniqueId=default(long);
		}

	}

	public partial class McpeContainerClose : Packet<McpeContainerClose>
	{

		public byte windowId; // = null;
		public byte windowType; // = null;
		public bool server; // = null;

		public McpeContainerClose()
		{
			Id = 0x2f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(windowId);
			Write(windowType);
			Write(server);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			windowId = ReadByte();
			windowType = ReadByte();
			server = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			windowId=default(byte);
			windowType=default(byte);
			server=default(bool);
		}

	}

	public partial class McpePlayerHotbar : Packet<McpePlayerHotbar>
	{

		public uint selectedSlot; // = null;
		public byte windowId; // = null;
		public bool selectSlot; // = null;

		public McpePlayerHotbar()
		{
			Id = 0x30;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(selectedSlot);
			Write(windowId);
			Write(selectSlot);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			selectedSlot = ReadUnsignedVarInt();
			windowId = ReadByte();
			selectSlot = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			selectedSlot=default(uint);
			windowId=default(byte);
			selectSlot=default(bool);
		}

	}

	public partial class McpeInventoryContent : Packet<McpeInventoryContent>
	{

		public uint inventoryId; // = null;
		public ItemStacks input; // = null;

		public McpeInventoryContent()
		{
			Id = 0x31;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(inventoryId);
			Write(input);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			inventoryId = ReadUnsignedVarInt();
			input = ReadItemStacks();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			inventoryId=default(uint);
			input=default(ItemStacks);
		}

	}

	public partial class McpeInventorySlot : Packet<McpeInventorySlot>
	{

		public uint inventoryId; // = null;
		public uint slot; // = null;

		public McpeInventorySlot()
		{
			Id = 0x32;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(inventoryId);
			WriteUnsignedVarInt(slot);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			inventoryId = ReadUnsignedVarInt();
			slot = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			inventoryId=default(uint);
			slot=default(uint);
		}

	}

	public partial class McpeContainerSetData : Packet<McpeContainerSetData>
	{

		public byte windowId; // = null;
		public int property; // = null;
		public int value; // = null;

		public McpeContainerSetData()
		{
			Id = 0x33;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(windowId);
			WriteSignedVarInt(property);
			WriteSignedVarInt(value);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			windowId = ReadByte();
			property = ReadSignedVarInt();
			value = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			windowId=default(byte);
			property=default(int);
			value=default(int);
		}

	}

	public partial class McpeCraftingData : Packet<McpeCraftingData>
	{

		public Recipes recipes; // = null;
		public PotionTypeRecipe[] potionTypeRecipes; // = null;
		public PotionContainerChangeRecipe[] potionContainerRecipes; // = null;
		public MaterialReducerRecipe[] materialReducerRecipes; // = null;
		public bool isClean; // = null;

		public McpeCraftingData()
		{
			Id = 0x34;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(recipes);
			Write(potionTypeRecipes);
			Write(potionContainerRecipes);
			Write(materialReducerRecipes);
			Write(isClean);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			recipes = ReadRecipes();
			potionTypeRecipes = ReadPotionTypeRecipes();
			potionContainerRecipes = ReadPotionContainerChangeRecipes();
			materialReducerRecipes = ReadMaterialReducerRecipes();
			isClean = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			recipes=default(Recipes);
			potionTypeRecipes=default(PotionTypeRecipe[]);
			potionContainerRecipes=default(PotionContainerChangeRecipe[]);
			materialReducerRecipes=default(MaterialReducerRecipe[]);
			isClean=default(bool);
		}

	}

	public partial class McpeGuiDataPickItem : Packet<McpeGuiDataPickItem>
	{

		public string itemName; // = null;
		public string itemEffects; // = null;
		public int hotbarSlot; // = null;

		public McpeGuiDataPickItem()
		{
			Id = 0x36;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(itemName);
			Write(itemEffects);
			Write(hotbarSlot);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			itemName = ReadString();
			itemEffects = ReadString();
			hotbarSlot = ReadInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			itemName=default(string);
			itemEffects=default(string);
			hotbarSlot=default(int);
		}

	}

	public partial class McpeBlockEntityData : Packet<McpeBlockEntityData>
	{

		public BlockCoordinates coordinates; // = null;
		public Nbt namedtag; // = null;

		public McpeBlockEntityData()
		{
			Id = 0x38;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(coordinates);
			Write(namedtag);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			coordinates = ReadBlockCoordinates();
			namedtag = ReadNbt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			coordinates=default(BlockCoordinates);
			namedtag=default(Nbt);
		}

	}

	public partial class McpeLevelChunk : Packet<McpeLevelChunk>
	{

		public int chunkX; // = null;
		public int chunkZ; // = null;
		public int dimension; // = null;

		public McpeLevelChunk()
		{
			Id = 0x3a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(chunkX);
			WriteSignedVarInt(chunkZ);
			WriteSignedVarInt(dimension);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			chunkX = ReadSignedVarInt();
			chunkZ = ReadSignedVarInt();
			dimension = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			chunkX=default(int);
			chunkZ=default(int);
			dimension=default(int);
		}

	}

	public partial class McpeSetCommandsEnabled : Packet<McpeSetCommandsEnabled>
	{

		public bool enabled; // = null;

		public McpeSetCommandsEnabled()
		{
			Id = 0x3b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(enabled);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			enabled = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			enabled=default(bool);
		}

	}

	public partial class McpeSetDifficulty : Packet<McpeSetDifficulty>
	{

		public uint difficulty; // = null;

		public McpeSetDifficulty()
		{
			Id = 0x3c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(difficulty);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			difficulty = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			difficulty=default(uint);
		}

	}

	public partial class McpeChangeDimension : Packet<McpeChangeDimension>
	{

		public int dimension; // = null;
		public Vector3 position; // = null;
		public bool respawn; // = null;

		public McpeChangeDimension()
		{
			Id = 0x3d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(dimension);
			Write(position);
			Write(respawn);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			dimension = ReadSignedVarInt();
			position = ReadVector3();
			respawn = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			dimension=default(int);
			position=default(Vector3);
			respawn=default(bool);
		}

	}

	public partial class McpeSetPlayerGameType : Packet<McpeSetPlayerGameType>
	{

		public int gamemode; // = null;

		public McpeSetPlayerGameType()
		{
			Id = 0x3e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(gamemode);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			gamemode = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			gamemode=default(int);
		}

	}

	public partial class McpePlayerList : Packet<McpePlayerList>
	{

		public PlayerRecords records; // = null;

		public McpePlayerList()
		{
			Id = 0x3f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(records);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			records = ReadPlayerRecords();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			records=default(PlayerRecords);
		}

	}

	public partial class McpeSimpleEvent : Packet<McpeSimpleEvent>
	{

		public ushort eventType; // = null;

		public McpeSimpleEvent()
		{
			Id = 0x40;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(eventType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			eventType = ReadUshort();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			eventType=default(ushort);
		}

	}

	public partial class McpeTelemetryEvent : Packet<McpeTelemetryEvent>
	{

		public long runtimeEntityId; // = null;
		public int eventData; // = null;
		public byte eventType; // = null;
		public byte[] auxData; // = null;

		public McpeTelemetryEvent()
		{
			Id = 0x41;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			WriteSignedVarInt(eventData);
			Write(eventType);
			Write(auxData);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			eventData = ReadSignedVarInt();
			eventType = ReadByte();
			auxData = ReadBytes(0, true);

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			eventData=default(int);
			eventType=default(byte);
			auxData=default(byte[]);
		}

	}

	public partial class McpeSpawnExperienceOrb : Packet<McpeSpawnExperienceOrb>
	{

		public Vector3 position; // = null;
		public int count; // = null;

		public McpeSpawnExperienceOrb()
		{
			Id = 0x42;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(position);
			WriteSignedVarInt(count);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			position = ReadVector3();
			count = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			position=default(Vector3);
			count=default(int);
		}

	}

	public partial class McpeClientboundMapItemData : Packet<McpeClientboundMapItemData>
	{

		public MapInfo mapinfo; // = null;

		public McpeClientboundMapItemData()
		{
			Id = 0x43;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(mapinfo);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			mapinfo = ReadMapInfo();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			mapinfo=default(MapInfo);
		}

	}

	public partial class McpeMapInfoRequest : Packet<McpeMapInfoRequest>
	{

		public long mapId; // = null;

		public McpeMapInfoRequest()
		{
			Id = 0x44;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(mapId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			mapId = ReadSignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			mapId=default(long);
		}

	}

	public partial class McpeRequestChunkRadius : Packet<McpeRequestChunkRadius>
	{

		public int chunkRadius; // = null;
		public byte maxRadius; // = null;

		public McpeRequestChunkRadius()
		{
			Id = 0x45;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(chunkRadius);
			Write(maxRadius);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			chunkRadius = ReadSignedVarInt();
			maxRadius = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			chunkRadius=default(int);
			maxRadius=default(byte);
		}

	}

	public partial class McpeChunkRadiusUpdate : Packet<McpeChunkRadiusUpdate>
	{

		public int chunkRadius; // = null;

		public McpeChunkRadiusUpdate()
		{
			Id = 0x46;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(chunkRadius);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			chunkRadius = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			chunkRadius=default(int);
		}

	}

	public partial class McpeGameRulesChanged : Packet<McpeGameRulesChanged>
	{


		public McpeGameRulesChanged()
		{
			Id = 0x48;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCamera : Packet<McpeCamera>
	{

		public long unknown1; // = null;
		public long unknown2; // = null;

		public McpeCamera()
		{
			Id = 0x49;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(unknown1);
			WriteSignedVarLong(unknown2);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			unknown1 = ReadSignedVarLong();
			unknown2 = ReadSignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			unknown1=default(long);
			unknown2=default(long);
		}

	}

	public partial class McpeBossEvent : Packet<McpeBossEvent>
	{
		public enum Type
		{
			AddBoss = 0,
			AddPlayer = 1,
			RemoveBoss = 2,
			RemovePlayer = 3,
			UpdateProgress = 4,
			UpdateName = 5,
			UpdateOptions = 6,
			UpdateStyle = 7,
			Query = 8,
		}

		public long bossEntityId; // = null;
		public long playerId; // = null;
		public byte eventType; // = null;
		public string title; // = null;
		public string filteredTitle; // = null;
		public float healthPercent; // = null;
		public byte color; // = null;
		public byte overlay; // = null;

		public McpeBossEvent()
		{
			Id = 0x4a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(bossEntityId);
			WriteSignedVarLong(playerId);
			Write(eventType);
			Write(title);
			Write(filteredTitle);
			Write(healthPercent);
			Write(color);
			Write(overlay);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			bossEntityId = ReadSignedVarLong();
			playerId = ReadSignedVarLong();
			eventType = ReadByte();
			title = ReadString();
			filteredTitle = ReadString();
			healthPercent = ReadFloat();
			color = ReadByte();
			overlay = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			bossEntityId=default(long);
			playerId=default(long);
			eventType=default(byte);
			title=default(string);
			filteredTitle=default(string);
			healthPercent=default(float);
			color=default(byte);
			overlay=default(byte);
		}

	}

	public partial class McpeShowCredits : Packet<McpeShowCredits>
	{

		public long runtimeEntityId; // = null;
		public int status; // = null;

		public McpeShowCredits()
		{
			Id = 0x4b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			WriteSignedVarInt(status);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			status = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			status=default(int);
		}

	}

	public partial class McpeAvailableCommands : Packet<McpeAvailableCommands>
	{


		public McpeAvailableCommands()
		{
			Id = 0x4c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCommandRequest : Packet<McpeCommandRequest>
	{

		public string command; // = null;
		public CommandOriginData origin; // = null;
		public bool isInternal; // = null;
		public string version; // = null;

		public McpeCommandRequest()
		{
			Id = 0x4d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(command);
			Write(origin);
			Write(isInternal);
			Write(version);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			command = ReadString();
			origin = ReadCommandOriginData();
			isInternal = ReadBool();
			version = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			command=default(string);
			origin=default(CommandOriginData);
			isInternal=default(bool);
			version=default(string);
		}

	}

	public partial class McpeCommandBlockUpdate : Packet<McpeCommandBlockUpdate>
	{

		public bool isBlock; // = null;

		public McpeCommandBlockUpdate()
		{
			Id = 0x4e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(isBlock);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			isBlock = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			isBlock=default(bool);
		}

	}

	public partial class McpeCommandOutput : Packet<McpeCommandOutput>
	{


		public McpeCommandOutput()
		{
			Id = 0x4f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeUpdateTrade : Packet<McpeUpdateTrade>
	{

		public byte windowId; // = null;
		public byte windowType; // = null;
		public int size; // = null;
		public int tradeTier; // = null;
		public long traderEntityId; // = null;
		public long playerEntityId; // = null;
		public string displayName; // = null;
		public bool useNewTradeScreen; // = null;
		public bool usingEconomyTrade; // = null;
		public Nbt namedtag; // = null;

		public McpeUpdateTrade()
		{
			Id = 0x50;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(windowId);
			Write(windowType);
			WriteSignedVarInt(size);
			WriteSignedVarInt(tradeTier);
			WriteSignedVarLong(traderEntityId);
			WriteSignedVarLong(playerEntityId);
			Write(displayName);
			Write(useNewTradeScreen);
			Write(usingEconomyTrade);
			Write(namedtag);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			windowId = ReadByte();
			windowType = ReadByte();
			size = ReadSignedVarInt();
			tradeTier = ReadSignedVarInt();
			traderEntityId = ReadSignedVarLong();
			playerEntityId = ReadSignedVarLong();
			displayName = ReadString();
			useNewTradeScreen = ReadBool();
			usingEconomyTrade = ReadBool();
			namedtag = ReadNbt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			windowId=default(byte);
			windowType=default(byte);
			size=default(int);
			tradeTier=default(int);
			traderEntityId=default(long);
			playerEntityId=default(long);
			displayName=default(string);
			useNewTradeScreen=default(bool);
			usingEconomyTrade=default(bool);
			namedtag=default(Nbt);
		}

	}

	public partial class McpeUpdateEquipment : Packet<McpeUpdateEquipment>
	{

		public byte windowId; // = null;
		public byte windowType; // = null;
		public int size; // = null;
		public long entityId; // = null;
		public Nbt namedtag; // = null;

		public McpeUpdateEquipment()
		{
			Id = 0x51;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(windowId);
			Write(windowType);
			WriteSignedVarInt(size);
			WriteSignedVarLong(entityId);
			Write(namedtag);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			windowId = ReadByte();
			windowType = ReadByte();
			size = ReadSignedVarInt();
			entityId = ReadSignedVarLong();
			namedtag = ReadNbt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			windowId=default(byte);
			windowType=default(byte);
			size=default(int);
			entityId=default(long);
			namedtag=default(Nbt);
		}

	}

	public partial class McpeResourcePackDataInfo : Packet<McpeResourcePackDataInfo>
	{

		public string packageId; // = null;
		public uint maxChunkSize; // = null;
		public uint chunkCount; // = null;
		public ulong compressedPackageSize; // = null;
		public byte[] hash; // = null;
		public bool isPremium; // = null;
		public byte packType; // = null;

		public McpeResourcePackDataInfo()
		{
			Id = 0x52;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(packageId);
			Write(maxChunkSize);
			Write(chunkCount);
			Write(compressedPackageSize);
			WriteByteArray(hash);
			Write(isPremium);
			Write(packType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			packageId = ReadString();
			maxChunkSize = ReadUint();
			chunkCount = ReadUint();
			compressedPackageSize = ReadUlong();
			hash = ReadByteArray();
			isPremium = ReadBool();
			packType = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			packageId=default(string);
			maxChunkSize=default(uint);
			chunkCount=default(uint);
			compressedPackageSize=default(ulong);
			hash=default(byte[]);
			isPremium=default(bool);
			packType=default(byte);
		}

	}

	public partial class McpeResourcePackChunkData : Packet<McpeResourcePackChunkData>
	{

		public string packageId; // = null;
		public uint chunkIndex; // = null;
		public ulong progress; // = null;
		public byte[] payload; // = null;

		public McpeResourcePackChunkData()
		{
			Id = 0x53;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(packageId);
			Write(chunkIndex);
			Write(progress);
			WriteByteArray(payload);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			packageId = ReadString();
			chunkIndex = ReadUint();
			progress = ReadUlong();
			payload = ReadByteArray();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			packageId=default(string);
			chunkIndex=default(uint);
			progress=default(ulong);
			payload=default(byte[]);
		}

	}

	public partial class McpeResourcePackChunkRequest : Packet<McpeResourcePackChunkRequest>
	{

		public string packageId; // = null;
		public uint chunkIndex; // = null;

		public McpeResourcePackChunkRequest()
		{
			Id = 0x54;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(packageId);
			Write(chunkIndex);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			packageId = ReadString();
			chunkIndex = ReadUint();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			packageId=default(string);
			chunkIndex=default(uint);
		}

	}

	public partial class McpeTransfer : Packet<McpeTransfer>
	{

		public string serverAddress; // = null;
		public ushort port; // = null;
		public bool reloadWorld; // = null;

		public McpeTransfer()
		{
			Id = 0x55;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(serverAddress);
			Write(port);
			Write(reloadWorld);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			serverAddress = ReadString();
			port = ReadUshort();
			reloadWorld = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverAddress=default(string);
			port=default(ushort);
			reloadWorld=default(bool);
		}

	}

	public partial class McpePlaySound : Packet<McpePlaySound>
	{

		public string name; // = null;
		public BlockCoordinates coordinates; // = null;
		public float volume; // = null;
		public float pitch; // = null;

		public McpePlaySound()
		{
			Id = 0x56;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(name);
			Write(coordinates);
			Write(volume);
			Write(pitch);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			name = ReadString();
			coordinates = ReadBlockCoordinates();
			volume = ReadFloat();
			pitch = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			name=default(string);
			coordinates=default(BlockCoordinates);
			volume=default(float);
			pitch=default(float);
		}

	}

	public partial class McpeStopSound : Packet<McpeStopSound>
	{

		public string name; // = null;
		public bool stopAll; // = null;
		public bool stopMusicLegacy; // = null;

		public McpeStopSound()
		{
			Id = 0x57;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(name);
			Write(stopAll);
			Write(stopMusicLegacy);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			name = ReadString();
			stopAll = ReadBool();
			stopMusicLegacy = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			name=default(string);
			stopAll=default(bool);
			stopMusicLegacy=default(bool);
		}

	}

	public partial class McpeSetTitle : Packet<McpeSetTitle>
	{

		public int type; // = null;
		public string text; // = null;
		public int fadeInTime; // = null;
		public int stayTime; // = null;
		public int fadeOutTime; // = null;
		public string xuid; // = null;
		public string platformOnlineId; // = null;
		public string filteredTitleText; // = null;

		public McpeSetTitle()
		{
			Id = 0x58;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(type);
			Write(text);
			WriteSignedVarInt(fadeInTime);
			WriteSignedVarInt(stayTime);
			WriteSignedVarInt(fadeOutTime);
			Write(xuid);
			Write(platformOnlineId);
			Write(filteredTitleText);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			type = ReadSignedVarInt();
			text = ReadString();
			fadeInTime = ReadSignedVarInt();
			stayTime = ReadSignedVarInt();
			fadeOutTime = ReadSignedVarInt();
			xuid = ReadString();
			platformOnlineId = ReadString();
			filteredTitleText = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			type=default(int);
			text=default(string);
			fadeInTime=default(int);
			stayTime=default(int);
			fadeOutTime=default(int);
			xuid=default(string);
			platformOnlineId=default(string);
			filteredTitleText=default(string);
		}

	}

	public partial class McpeAddBehaviorTree : Packet<McpeAddBehaviorTree>
	{

		public string behaviortree; // = null;

		public McpeAddBehaviorTree()
		{
			Id = 0x59;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(behaviortree);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			behaviortree = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			behaviortree=default(string);
		}

	}

	public partial class McpeStructureBlockUpdate : Packet<McpeStructureBlockUpdate>
	{


		public McpeStructureBlockUpdate()
		{
			Id = 0x5a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeShowStoreOffer : Packet<McpeShowStoreOffer>
	{

		public UUID offerId; // = null;
		public byte redirectType; // = null;

		public McpeShowStoreOffer()
		{
			Id = 0x5b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(offerId);
			Write(redirectType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			offerId = ReadUUID();
			redirectType = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			offerId=default(UUID);
			redirectType=default(byte);
		}

	}

	public partial class McpePurchaseReceipt : Packet<McpePurchaseReceipt>
	{


		public McpePurchaseReceipt()
		{
			Id = 0x5c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpePlayerSkin : Packet<McpePlayerSkin>
	{

		public UUID uuid; // = null;
		public Skin skin; // = null;
		public string skinName; // = null;
		public string oldSkinName; // = null;
		public bool isVerified; // = null;

		public McpePlayerSkin()
		{
			Id = 0x5d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(uuid);
			Write(skin);
			Write(skinName);
			Write(oldSkinName);
			Write(isVerified);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			uuid = ReadUUID();
			skin = ReadSkin();
			skinName = ReadString();
			oldSkinName = ReadString();
			isVerified = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			uuid=default(UUID);
			skin=default(Skin);
			skinName=default(string);
			oldSkinName=default(string);
			isVerified=default(bool);
		}

	}

	public partial class McpeSubClientLogin : Packet<McpeSubClientLogin>
	{

		public byte[] connectionRequest; // = null;

		public McpeSubClientLogin()
		{
			Id = 0x5e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteByteArray(connectionRequest);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			connectionRequest = ReadByteArray();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			connectionRequest=default(byte[]);
		}

	}

	public partial class McpeInitiateWebSocketConnection : Packet<McpeInitiateWebSocketConnection>
	{

		public string server; // = null;

		public McpeInitiateWebSocketConnection()
		{
			Id = 0x5f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(server);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			server = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			server=default(string);
		}

	}

	public partial class McpeSetLastHurtBy : Packet<McpeSetLastHurtBy>
	{

		public int unknown; // = null;

		public McpeSetLastHurtBy()
		{
			Id = 0x60;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteVarInt(unknown);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			unknown = ReadVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			unknown=default(int);
		}

	}

	public partial class McpeBookEdit : Packet<McpeBookEdit>
	{

		public int inventorySlot; // = null;
		public uint type; // = null;

		public McpeBookEdit()
		{
			Id = 0x61;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(inventorySlot);
			WriteUnsignedVarInt(type);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			inventorySlot = ReadSignedVarInt();
			type = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			inventorySlot=default(int);
			type=default(uint);
		}

	}

	public partial class McpeNpcRequest : Packet<McpeNpcRequest>
	{

		public long runtimeEntityId; // = null;
		public byte unknown0; // = null;
		public string unknown1; // = null;
		public byte unknown2; // = null;
		public string sceneName; // = null;

		public McpeNpcRequest()
		{
			Id = 0x62;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(unknown0);
			Write(unknown1);
			Write(unknown2);
			Write(sceneName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			unknown0 = ReadByte();
			unknown1 = ReadString();
			unknown2 = ReadByte();
			sceneName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			unknown0=default(byte);
			unknown1=default(string);
			unknown2=default(byte);
			sceneName=default(string);
		}

	}

	public partial class McpePhotoTransfer : Packet<McpePhotoTransfer>
	{

		public string fileName; // = null;
		public string imageData; // = null;
		public string unknown2; // = null;
		public byte type; // = null;
		public byte sourceType; // = null;
		public long ownerUniqueId; // = null;
		public string newPhotoName; // = null;

		public McpePhotoTransfer()
		{
			Id = 0x63;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(fileName);
			Write(imageData);
			Write(unknown2);
			Write(type);
			Write(sourceType);
			WriteLe(ownerUniqueId);
			Write(newPhotoName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			fileName = ReadString();
			imageData = ReadString();
			unknown2 = ReadString();
			type = ReadByte();
			sourceType = ReadByte();
			ownerUniqueId = ReadLongLe();
			newPhotoName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			fileName=default(string);
			imageData=default(string);
			unknown2=default(string);
			type=default(byte);
			sourceType=default(byte);
			ownerUniqueId=default(long);
			newPhotoName=default(string);
		}

	}

	public partial class McpeModalFormRequest : Packet<McpeModalFormRequest>
	{

		public uint formId; // = null;
		public string data; // = null;

		public McpeModalFormRequest()
		{
			Id = 0x64;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(formId);
			Write(data);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			formId = ReadUnsignedVarInt();
			data = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			formId=default(uint);
			data=default(string);
		}

	}

	public partial class McpeModalFormResponse : Packet<McpeModalFormResponse>
	{

		public uint formId; // = null;

		public McpeModalFormResponse()
		{
			Id = 0x65;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(formId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			formId = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			formId=default(uint);
		}

	}

	public partial class McpeServerSettingsRequest : Packet<McpeServerSettingsRequest>
	{


		public McpeServerSettingsRequest()
		{
			Id = 0x66;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeServerSettingsResponse : Packet<McpeServerSettingsResponse>
	{

		public uint formId; // = null;
		public string data; // = null;

		public McpeServerSettingsResponse()
		{
			Id = 0x67;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(formId);
			Write(data);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			formId = ReadUnsignedVarInt();
			data = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			formId=default(uint);
			data=default(string);
		}

	}

	public partial class McpeShowProfile : Packet<McpeShowProfile>
	{

		public string xuid; // = null;

		public McpeShowProfile()
		{
			Id = 0x68;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(xuid);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			xuid = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			xuid=default(string);
		}

	}

	public partial class McpeSetDefaultGameType : Packet<McpeSetDefaultGameType>
	{

		public int gamemode; // = null;

		public McpeSetDefaultGameType()
		{
			Id = 0x69;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(gamemode);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			gamemode = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			gamemode=default(int);
		}

	}

	public partial class McpeRemoveObjective : Packet<McpeRemoveObjective>
	{

		public string objectiveName; // = null;

		public McpeRemoveObjective()
		{
			Id = 0x6a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(objectiveName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			objectiveName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			objectiveName=default(string);
		}

	}

	public partial class McpeSetDisplayObjective : Packet<McpeSetDisplayObjective>
	{

		public string displaySlot; // = null;
		public string objectiveName; // = null;
		public string displayName; // = null;
		public string criteriaName; // = null;
		public int sortOrder; // = null;

		public McpeSetDisplayObjective()
		{
			Id = 0x6b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(displaySlot);
			Write(objectiveName);
			Write(displayName);
			Write(criteriaName);
			WriteSignedVarInt(sortOrder);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			displaySlot = ReadString();
			objectiveName = ReadString();
			displayName = ReadString();
			criteriaName = ReadString();
			sortOrder = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			displaySlot=default(string);
			objectiveName=default(string);
			displayName=default(string);
			criteriaName=default(string);
			sortOrder=default(int);
		}

	}

	public partial class McpeSetScore : Packet<McpeSetScore>
	{
		public enum Types
		{
			Change = 0,
			Remove = 1,
		}
		public enum ChangeTypes
		{
			Player = 1,
			Entity = 2,
			FakePlayer = 3,
		}

		public ScoreEntries entries; // = null;

		public McpeSetScore()
		{
			Id = 0x6c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(entries);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entries = ReadScoreEntries();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entries=default(ScoreEntries);
		}

	}

	public partial class McpeLabTable : Packet<McpeLabTable>
	{

		public byte uselessByte; // = null;
		public int labTableX; // = null;
		public int labTableY; // = null;
		public int labTableZ; // = null;
		public byte reactionType; // = null;

		public McpeLabTable()
		{
			Id = 0x6d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(uselessByte);
			WriteSignedVarInt(labTableX);
			WriteSignedVarInt(labTableY);
			WriteSignedVarInt(labTableZ);
			Write(reactionType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			uselessByte = ReadByte();
			labTableX = ReadSignedVarInt();
			labTableY = ReadSignedVarInt();
			labTableZ = ReadSignedVarInt();
			reactionType = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			uselessByte=default(byte);
			labTableX=default(int);
			labTableY=default(int);
			labTableZ=default(int);
			reactionType=default(byte);
		}

	}

	public partial class McpeUpdateBlockSynced : Packet<McpeUpdateBlockSynced>
	{

		public BlockCoordinates coordinates; // = null;
		public uint blockRuntimeId; // = null;
		public uint blockPriority; // = null;
		public uint dataLayerId; // = null;
		public long unknown0; // = null;
		public long unknown1; // = null;

		public McpeUpdateBlockSynced()
		{
			Id = 0x6e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(coordinates);
			WriteUnsignedVarInt(blockRuntimeId);
			WriteUnsignedVarInt(blockPriority);
			WriteUnsignedVarInt(dataLayerId);
			WriteUnsignedVarLong(unknown0);
			WriteUnsignedVarLong(unknown1);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			coordinates = ReadBlockCoordinates();
			blockRuntimeId = ReadUnsignedVarInt();
			blockPriority = ReadUnsignedVarInt();
			dataLayerId = ReadUnsignedVarInt();
			unknown0 = ReadUnsignedVarLong();
			unknown1 = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			coordinates=default(BlockCoordinates);
			blockRuntimeId=default(uint);
			blockPriority=default(uint);
			dataLayerId=default(uint);
			unknown0=default(long);
			unknown1=default(long);
		}

	}

	public partial class McpeMoveEntityDelta : Packet<McpeMoveEntityDelta>
	{

		public long runtimeEntityId; // = null;
		public ushort flags; // = null;

		public McpeMoveEntityDelta()
		{
			Id = 0x6f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(flags);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			flags = ReadUshort();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			flags=default(ushort);
		}

	}

	public partial class McpeSetScoreboardIdentity : Packet<McpeSetScoreboardIdentity>
	{
		public enum Operations
		{
			RegisterIdentity = 0,
			ClearIdentity = 1,
		}

		public ScoreboardIdentityEntries entries; // = null;

		public McpeSetScoreboardIdentity()
		{
			Id = 0x70;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(entries);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entries = ReadScoreboardIdentityEntries();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entries=default(ScoreboardIdentityEntries);
		}

	}

	public partial class McpeSetLocalPlayerAsInitialized : Packet<McpeSetLocalPlayerAsInitialized>
	{

		public long runtimeEntityId; // = null;

		public McpeSetLocalPlayerAsInitialized()
		{
			Id = 0x71;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
		}

	}

	public partial class McpeUpdateSoftEnum : Packet<McpeUpdateSoftEnum>
	{


		public McpeUpdateSoftEnum()
		{
			Id = 0x72;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeNetworkStackLatency : Packet<McpeNetworkStackLatency>
	{

		public ulong timestamp; // = null;
		public byte unknownFlag; // = null;

		public McpeNetworkStackLatency()
		{
			Id = 0x73;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(timestamp);
			Write(unknownFlag);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			timestamp = ReadUlong();
			unknownFlag = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			timestamp=default(ulong);
			unknownFlag=default(byte);
		}

	}

	public partial class McpeSpawnParticleEffect : Packet<McpeSpawnParticleEffect>
	{

		public byte dimensionId; // = null;
		public long entityId; // = null;
		public Vector3 position; // = null;
		public string particleName; // = null;

		public McpeSpawnParticleEffect()
		{
			Id = 0x76;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(dimensionId);
			WriteSignedVarLong(entityId);
			Write(position);
			Write(particleName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			dimensionId = ReadByte();
			entityId = ReadSignedVarLong();
			position = ReadVector3();
			particleName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			dimensionId=default(byte);
			entityId=default(long);
			position=default(Vector3);
			particleName=default(string);
		}

	}

	public partial class McpeAvailableEntityIdentifiers : Packet<McpeAvailableEntityIdentifiers>
	{

		public Nbt namedtag; // = null;

		public McpeAvailableEntityIdentifiers()
		{
			Id = 0x77;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(namedtag);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			namedtag = ReadNbt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			namedtag=default(Nbt);
		}

	}

	public partial class McpeNetworkChunkPublisherUpdate : Packet<McpeNetworkChunkPublisherUpdate>
	{

		public BlockCoordinates coordinates; // = null;
		public uint radius; // = null;

		public McpeNetworkChunkPublisherUpdate()
		{
			Id = 0x79;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(coordinates);
			WriteUnsignedVarInt(radius);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			coordinates = ReadBlockCoordinates();
			radius = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			coordinates=default(BlockCoordinates);
			radius=default(uint);
		}

	}

	public partial class McpeBiomeDefinitionList : Packet<McpeBiomeDefinitionList>
	{


		public McpeBiomeDefinitionList()
		{
			Id = 0x7a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeLevelSoundEvent : Packet<McpeLevelSoundEvent>
	{

		public string soundId; // = null;
		public Vector3 position; // = null;
		public int blockId; // = null;
		public string entityType; // = null;
		public bool isBabyMob; // = null;
		public bool isGlobal; // = null;

		public McpeLevelSoundEvent()
		{
			Id = 0x7b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(soundId);
			Write(position);
			WriteSignedVarInt(blockId);
			Write(entityType);
			Write(isBabyMob);
			Write(isGlobal);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			soundId = ReadString();
			position = ReadVector3();
			blockId = ReadSignedVarInt();
			entityType = ReadString();
			isBabyMob = ReadBool();
			isGlobal = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			soundId=default(string);
			position=default(Vector3);
			blockId=default(int);
			entityType=default(string);
			isBabyMob=default(bool);
			isGlobal=default(bool);
		}

	}

	public partial class McpeLevelEventGeneric : Packet<McpeLevelEventGeneric>
	{

		public int eventId; // = null;
		public NbtCompound eventData; // = null;

		public McpeLevelEventGeneric()
		{
			Id = 0x7c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(eventId);
			WriteNbtBody(eventData);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			eventId = ReadSignedVarInt();
			eventData = ReadNbtBody();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			eventId=default(int);
			eventData=default(NbtCompound);
		}

	}

	public partial class McpeLecternUpdate : Packet<McpeLecternUpdate>
	{

		public byte page; // = null;
		public byte totalPages; // = null;
		public BlockCoordinates blockPosition; // = null;

		public McpeLecternUpdate()
		{
			Id = 0x7d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(page);
			Write(totalPages);
			Write(blockPosition);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			page = ReadByte();
			totalPages = ReadByte();
			blockPosition = ReadBlockCoordinates();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			page=default(byte);
			totalPages=default(byte);
			blockPosition=default(BlockCoordinates);
		}

	}

	public partial class McpeClientCacheStatus : Packet<McpeClientCacheStatus>
	{

		public bool enabled; // = null;

		public McpeClientCacheStatus()
		{
			Id = 0x81;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(enabled);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			enabled = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			enabled=default(bool);
		}

	}

	public partial class McpeOnScreenTextureAnimation : Packet<McpeOnScreenTextureAnimation>
	{

		public uint effectId; // = null;

		public McpeOnScreenTextureAnimation()
		{
			Id = 0x82;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(effectId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			effectId = ReadUint();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			effectId=default(uint);
		}

	}

	public partial class McpeMapCreateLockedCopy : Packet<McpeMapCreateLockedCopy>
	{

		public long originalMapId; // = null;
		public long newMapId; // = null;

		public McpeMapCreateLockedCopy()
		{
			Id = 0x83;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(originalMapId);
			WriteSignedVarLong(newMapId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			originalMapId = ReadSignedVarLong();
			newMapId = ReadSignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			originalMapId=default(long);
			newMapId=default(long);
		}

	}

	public partial class McpeStructureTemplateDataExportRequest : Packet<McpeStructureTemplateDataExportRequest>
	{

		public string name; // = null;
		public BlockCoordinates position; // = null;
		public StructureSettings settings; // = null;
		public byte requestType; // = null;

		public McpeStructureTemplateDataExportRequest()
		{
			Id = 0x84;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(name);
			Write(position);
			Write(settings);
			Write(requestType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			name = ReadString();
			position = ReadBlockCoordinates();
			settings = ReadStructureSettings();
			requestType = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			name=default(string);
			position=default(BlockCoordinates);
			settings=default(StructureSettings);
			requestType=default(byte);
		}

	}

	public partial class McpeStructureTemplateDataExportResponse : Packet<McpeStructureTemplateDataExportResponse>
	{

		public string name; // = null;

		public McpeStructureTemplateDataExportResponse()
		{
			Id = 0x85;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(name);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			name = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			name=default(string);
		}

	}

	public partial class McpeClientCacheBlobStatus : Packet<McpeClientCacheBlobStatus>
	{


		public McpeClientCacheBlobStatus()
		{
			Id = 0x87;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeClientCacheMissResponse : Packet<McpeClientCacheMissResponse>
	{


		public McpeClientCacheMissResponse()
		{
			Id = 0x88;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeEducationSettings : Packet<McpeEducationSettings>
	{

		public string codeBuilderDefaultUri; // = null;
		public string codeBuilderTitle; // = null;
		public bool canResizeCodeBuilder; // = null;
		public bool disableLegacyTitleBar; // = null;
		public string postProcessFilter; // = null;
		public string screenshotBorderResourcePath; // = null;

		public McpeEducationSettings()
		{
			Id = 0x89;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(codeBuilderDefaultUri);
			Write(codeBuilderTitle);
			Write(canResizeCodeBuilder);
			Write(disableLegacyTitleBar);
			Write(postProcessFilter);
			Write(screenshotBorderResourcePath);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			codeBuilderDefaultUri = ReadString();
			codeBuilderTitle = ReadString();
			canResizeCodeBuilder = ReadBool();
			disableLegacyTitleBar = ReadBool();
			postProcessFilter = ReadString();
			screenshotBorderResourcePath = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			codeBuilderDefaultUri=default(string);
			codeBuilderTitle=default(string);
			canResizeCodeBuilder=default(bool);
			disableLegacyTitleBar=default(bool);
			postProcessFilter=default(string);
			screenshotBorderResourcePath=default(string);
		}

	}

	public partial class McpeEmote : Packet<McpeEmote>
	{

		public long runtimeEntityId; // = null;
		public string emoteId; // = null;
		public uint emoteLengthTicks; // = null;
		public string xboxUserId; // = null;
		public string platformChatId; // = null;
		public byte flags; // = null;

		public McpeEmote()
		{
			Id = 0x8a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(emoteId);
			WriteUnsignedVarInt(emoteLengthTicks);
			Write(xboxUserId);
			Write(platformChatId);
			Write(flags);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			emoteId = ReadString();
			emoteLengthTicks = ReadUnsignedVarInt();
			xboxUserId = ReadString();
			platformChatId = ReadString();
			flags = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			emoteId=default(string);
			emoteLengthTicks=default(uint);
			xboxUserId=default(string);
			platformChatId=default(string);
			flags=default(byte);
		}

	}

	public partial class McpeMultiplayerSettings : Packet<McpeMultiplayerSettings>
	{

		public int action; // = null;

		public McpeMultiplayerSettings()
		{
			Id = 0x8b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(action);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			action = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			action=default(int);
		}

	}

	public partial class McpeSettingsCommand : Packet<McpeSettingsCommand>
	{

		public string command; // = null;
		public bool suppressOutput; // = null;

		public McpeSettingsCommand()
		{
			Id = 0x8c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(command);
			Write(suppressOutput);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			command = ReadString();
			suppressOutput = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			command=default(string);
			suppressOutput=default(bool);
		}

	}

	public partial class McpeAnvilDamage : Packet<McpeAnvilDamage>
	{

		public byte damageAmount; // = null;
		public BlockCoordinates blockPosition; // = null;

		public McpeAnvilDamage()
		{
			Id = 0x8d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(damageAmount);
			Write(blockPosition);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			damageAmount = ReadByte();
			blockPosition = ReadBlockCoordinates();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			damageAmount=default(byte);
			blockPosition=default(BlockCoordinates);
		}

	}

	public partial class McpeCompletedUsingItem : Packet<McpeCompletedUsingItem>
	{

		public short itemId; // = null;
		public int action; // = null;

		public McpeCompletedUsingItem()
		{
			Id = 0x8e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(itemId);
			Write(action);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			itemId = ReadShort();
			action = ReadInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			itemId=default(short);
			action=default(int);
		}

	}

	public partial class McpeNetworkSettings : Packet<McpeNetworkSettings>
	{
		public enum Compressionalgorithm
		{
			Zlib = 0,
			Snappy = 1,
			None = 65535,
		}

		public ushort compressionThreshold; // = null;
		public ushort compressionAlgorithm; // = null;
		public bool clientThrottleEnabled; // = null;
		public byte clientThrottleThreshold; // = null;
		public float clientThrottleScalar; // = null;

		public McpeNetworkSettings()
		{
			Id = 0x8f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(compressionThreshold);
			Write(compressionAlgorithm);
			Write(clientThrottleEnabled);
			Write(clientThrottleThreshold);
			Write(clientThrottleScalar);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			compressionThreshold = ReadUshort();
			compressionAlgorithm = ReadUshort();
			clientThrottleEnabled = ReadBool();
			clientThrottleThreshold = ReadByte();
			clientThrottleScalar = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			compressionThreshold=default(ushort);
			compressionAlgorithm=default(ushort);
			clientThrottleEnabled=default(bool);
			clientThrottleThreshold=default(byte);
			clientThrottleScalar=default(float);
		}

	}

	public partial class McpePlayerAuthInput : Packet<McpePlayerAuthInput>
	{


		public McpePlayerAuthInput()
		{
			Id = 0x90;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCreativeContent : Packet<McpeCreativeContent>
	{


		public McpeCreativeContent()
		{
			Id = 0x91;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpePlayerEnchantOptions : Packet<McpePlayerEnchantOptions>
	{

		public EnchantOptions enchantOptions; // = null;

		public McpePlayerEnchantOptions()
		{
			Id = 0x92;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(enchantOptions);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			enchantOptions = ReadEnchantOptions();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			enchantOptions=default(EnchantOptions);
		}

	}

	public partial class McpeItemStackRequest : Packet<McpeItemStackRequest>
	{
		public enum ActionType
		{
			Take = 0,
			Place = 1,
			Swap = 2,
			Drop = 3,
			Destroy = 4,
			Consume = 5,
			Create = 6,
			PlaceIntoBundle = 7,
			TakeFromBundle = 8,
			LabTableCombine = 9,
			BeaconPayment = 10,
			MineBlock = 11,
			CraftRecipe = 12,
			CraftRecipeAuto = 13,
			CraftCreative = 14,
			CraftRecipeOptional = 15,
			CraftGrindstone = 16,
			CraftLoom = 17,
			CraftNotImplementedDeprecated = 18,
			CraftResultsDeprecated = 19,
		}

		public ItemStackRequests requests; // = null;

		public McpeItemStackRequest()
		{
			Id = 0x93;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(requests);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			requests = ReadItemStackRequests();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			requests=default(ItemStackRequests);
		}

	}

	public partial class McpeItemStackResponse : Packet<McpeItemStackResponse>
	{

		public ItemStackResponses responses; // = null;

		public McpeItemStackResponse()
		{
			Id = 0x94;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(responses);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			responses = ReadItemStackResponses();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			responses=default(ItemStackResponses);
		}

	}

	public partial class McpePlayerArmorDamage : Packet<McpePlayerArmorDamage>
	{


		public McpePlayerArmorDamage()
		{
			Id = 0x95;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCodeBuilder : Packet<McpeCodeBuilder>
	{

		public string url; // = null;
		public bool openCodeBuilder; // = null;

		public McpeCodeBuilder()
		{
			Id = 0x96;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(url);
			Write(openCodeBuilder);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			url = ReadString();
			openCodeBuilder = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			url=default(string);
			openCodeBuilder=default(bool);
		}

	}

	public partial class McpeUpdatePlayerGameType : Packet<McpeUpdatePlayerGameType>
	{

		public int playerGameType; // = null;
		public long targetPlayerUniqueId; // = null;
		public long tick; // = null;

		public McpeUpdatePlayerGameType()
		{
			Id = 0x97;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(playerGameType);
			WriteSignedVarLong(targetPlayerUniqueId);
			WriteUnsignedVarLong(tick);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			playerGameType = ReadSignedVarInt();
			targetPlayerUniqueId = ReadSignedVarLong();
			tick = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			playerGameType=default(int);
			targetPlayerUniqueId=default(long);
			tick=default(long);
		}

	}

	public partial class McpeEmoteList : Packet<McpeEmoteList>
	{

		public long runtimeEntityId; // = null;

		public McpeEmoteList()
		{
			Id = 0x98;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
		}

	}

	public partial class McpePositionTrackingDbServerBroadcast : Packet<McpePositionTrackingDbServerBroadcast>
	{

		public byte action; // = null;
		public int trackingId; // = null;
		public NbtCompound nbt; // = null;

		public McpePositionTrackingDbServerBroadcast()
		{
			Id = 0x99;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(action);
			WriteSignedVarInt(trackingId);
			WriteNbtBody(nbt);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			action = ReadByte();
			trackingId = ReadSignedVarInt();
			nbt = ReadNbtBody();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			action=default(byte);
			trackingId=default(int);
			nbt=default(NbtCompound);
		}

	}

	public partial class McpePositionTrackingDbClientRequest : Packet<McpePositionTrackingDbClientRequest>
	{

		public byte action; // = null;
		public int trackingId; // = null;

		public McpePositionTrackingDbClientRequest()
		{
			Id = 0x9a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(action);
			WriteSignedVarInt(trackingId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			action = ReadByte();
			trackingId = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			action=default(byte);
			trackingId=default(int);
		}

	}

	public partial class McpeDebugInfo : Packet<McpeDebugInfo>
	{

		public long actorUniqueId; // = null;
		public string data; // = null;

		public McpeDebugInfo()
		{
			Id = 0x9b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(actorUniqueId);
			Write(data);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			actorUniqueId = ReadSignedVarLong();
			data = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			actorUniqueId=default(long);
			data=default(string);
		}

	}

	public partial class McpePacketViolationWarning : Packet<McpePacketViolationWarning>
	{

		public int violationType; // = null;
		public int severity; // = null;
		public int packetId; // = null;
		public string reason; // = null;

		public McpePacketViolationWarning()
		{
			Id = 0x9c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(violationType);
			WriteSignedVarInt(severity);
			WriteSignedVarInt(packetId);
			Write(reason);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			violationType = ReadSignedVarInt();
			severity = ReadSignedVarInt();
			packetId = ReadSignedVarInt();
			reason = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			violationType=default(int);
			severity=default(int);
			packetId=default(int);
			reason=default(string);
		}

	}

	public partial class McpeMotionPredictionHints : Packet<McpeMotionPredictionHints>
	{

		public long runtimeEntityId; // = null;
		public Vector3 motion; // = null;
		public bool onGround; // = null;

		public McpeMotionPredictionHints()
		{
			Id = 0x9d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			Write(motion);
			Write(onGround);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			motion = ReadVector3();
			onGround = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			motion=default(Vector3);
			onGround=default(bool);
		}

	}

	public partial class McpeAnimateEntity : Packet<McpeAnimateEntity>
	{

		public string animation; // = null;
		public string nextState; // = null;
		public string stopExpression; // = null;
		public int stopExpressionVersion; // = null;
		public string controller; // = null;
		public float blendOutTime; // = null;

		public McpeAnimateEntity()
		{
			Id = 0x9e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(animation);
			Write(nextState);
			Write(stopExpression);
			Write(stopExpressionVersion);
			Write(controller);
			Write(blendOutTime);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			animation = ReadString();
			nextState = ReadString();
			stopExpression = ReadString();
			stopExpressionVersion = ReadInt();
			controller = ReadString();
			blendOutTime = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			animation=default(string);
			nextState=default(string);
			stopExpression=default(string);
			stopExpressionVersion=default(int);
			controller=default(string);
			blendOutTime=default(float);
		}

	}

	public partial class McpePlayerFog : Packet<McpePlayerFog>
	{


		public McpePlayerFog()
		{
			Id = 0xa0;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCorrectPlayerMovePrediction : Packet<McpeCorrectPlayerMovePrediction>
	{

		public byte predictionType; // = null;
		public Vector3 position; // = null;
		public Vector3 delta; // = null;
		public float rotationPitch; // = null;
		public float rotationYaw; // = null;

		public McpeCorrectPlayerMovePrediction()
		{
			Id = 0xa1;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(predictionType);
			Write(position);
			Write(delta);
			Write(rotationPitch);
			Write(rotationYaw);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			predictionType = ReadByte();
			position = ReadVector3();
			delta = ReadVector3();
			rotationPitch = ReadFloat();
			rotationYaw = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			predictionType=default(byte);
			position=default(Vector3);
			delta=default(Vector3);
			rotationPitch=default(float);
			rotationYaw=default(float);
		}

	}

	public partial class McpeItemComponent : Packet<McpeItemComponent>
	{

		public ItemComponentList entries; // = null;

		public McpeItemComponent()
		{
			Id = 0xa2;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(entries);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entries = ReadItemComponentList();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entries=default(ItemComponentList);
		}

	}

	public partial class McpeClientboundDebugRenderer : Packet<McpeClientboundDebugRenderer>
	{

		public string type; // = null;

		public McpeClientboundDebugRenderer()
		{
			Id = 0xa4;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(type);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			type = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			type=default(string);
		}

	}

	public partial class McpeSyncEntityProperty : Packet<McpeSyncEntityProperty>
	{

		public Nbt namedtag; // = null;

		public McpeSyncEntityProperty()
		{
			Id = 0xa5;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(namedtag);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			namedtag = ReadNbt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			namedtag=default(Nbt);
		}

	}

	public partial class McpeAddVolumeEntity : Packet<McpeAddVolumeEntity>
	{

		public uint entityNetworkId; // = null;
		public Nbt data; // = null;
		public string jsonIdentifier; // = null;
		public string instanceName; // = null;
		public BlockCoordinates minBounds; // = null;
		public BlockCoordinates maxBounds; // = null;
		public int dimension; // = null;
		public string engineVersion; // = null;

		public McpeAddVolumeEntity()
		{
			Id = 0xa6;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(entityNetworkId);
			Write(data);
			Write(jsonIdentifier);
			Write(instanceName);
			Write(minBounds);
			Write(maxBounds);
			WriteSignedVarInt(dimension);
			Write(engineVersion);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityNetworkId = ReadUnsignedVarInt();
			data = ReadNbt();
			jsonIdentifier = ReadString();
			instanceName = ReadString();
			minBounds = ReadBlockCoordinates();
			maxBounds = ReadBlockCoordinates();
			dimension = ReadSignedVarInt();
			engineVersion = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityNetworkId=default(uint);
			data=default(Nbt);
			jsonIdentifier=default(string);
			instanceName=default(string);
			minBounds=default(BlockCoordinates);
			maxBounds=default(BlockCoordinates);
			dimension=default(int);
			engineVersion=default(string);
		}

	}

	public partial class McpeRemoveVolumeEntity : Packet<McpeRemoveVolumeEntity>
	{

		public uint entityNetworkId; // = null;
		public int dimension; // = null;

		public McpeRemoveVolumeEntity()
		{
			Id = 0xa7;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(entityNetworkId);
			WriteSignedVarInt(dimension);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityNetworkId = ReadUnsignedVarInt();
			dimension = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityNetworkId=default(uint);
			dimension=default(int);
		}

	}

	public partial class McpeSimulationType : Packet<McpeSimulationType>
	{

		public byte simulationType; // = null;

		public McpeSimulationType()
		{
			Id = 0xa8;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(simulationType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			simulationType = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			simulationType=default(byte);
		}

	}

	public partial class McpeNpcDialogue : Packet<McpeNpcDialogue>
	{

		public long npcUniqueId; // = null;
		public int actionType; // = null;
		public string dialogue; // = null;
		public string sceneName; // = null;
		public string npcName; // = null;
		public string actionJson; // = null;

		public McpeNpcDialogue()
		{
			Id = 0xa9;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteLe(npcUniqueId);
			WriteSignedVarInt(actionType);
			Write(dialogue);
			Write(sceneName);
			Write(npcName);
			Write(actionJson);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			npcUniqueId = ReadLongLe();
			actionType = ReadSignedVarInt();
			dialogue = ReadString();
			sceneName = ReadString();
			npcName = ReadString();
			actionJson = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			npcUniqueId=default(long);
			actionType=default(int);
			dialogue=default(string);
			sceneName=default(string);
			npcName=default(string);
			actionJson=default(string);
		}

	}

	public partial class McpeEduUriResource : Packet<McpeEduUriResource>
	{

		public string buttonName; // = null;
		public string linkUri; // = null;

		public McpeEduUriResource()
		{
			Id = 0xaa;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(buttonName);
			Write(linkUri);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			buttonName = ReadString();
			linkUri = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			buttonName=default(string);
			linkUri=default(string);
		}

	}

	public partial class McpeCreatePhoto : Packet<McpeCreatePhoto>
	{

		public long entityUniqueId; // = null;
		public string photoName; // = null;
		public string photoItemName; // = null;

		public McpeCreatePhoto()
		{
			Id = 0xab;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteLe(entityUniqueId);
			Write(photoName);
			Write(photoItemName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityUniqueId = ReadLongLe();
			photoName = ReadString();
			photoItemName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityUniqueId=default(long);
			photoName=default(string);
			photoItemName=default(string);
		}

	}

	public partial class McpeUpdateSubChunkBlocksPacket : Packet<McpeUpdateSubChunkBlocksPacket>
	{

		public BlockCoordinates subchunkCoordinates; // = null;
		public UpdateSubChunkBlocksPacketEntry[] layerZeroUpdates; // = null;
		public UpdateSubChunkBlocksPacketEntry[] layerOneUpdates; // = null;

		public McpeUpdateSubChunkBlocksPacket()
		{
			Id = 0xac;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(subchunkCoordinates);
			Write(layerZeroUpdates);
			Write(layerOneUpdates);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			subchunkCoordinates = ReadBlockCoordinates();
			layerZeroUpdates = ReadUpdateSubChunkBlocksPacketEntrys();
			layerOneUpdates = ReadUpdateSubChunkBlocksPacketEntrys();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			subchunkCoordinates=default(BlockCoordinates);
			layerZeroUpdates=default(UpdateSubChunkBlocksPacketEntry[]);
			layerOneUpdates=default(UpdateSubChunkBlocksPacketEntry[]);
		}

	}

	public partial class McpeSubChunkPacket : Packet<McpeSubChunkPacket>
	{

		public bool cacheEnabled; // = null;
		public int dimension; // = null;
		public int originX; // = null;
		public int originY; // = null;
		public int originZ; // = null;

		public McpeSubChunkPacket()
		{
			Id = 0xae;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(cacheEnabled);
			WriteSignedVarInt(dimension);
			WriteSignedVarInt(originX);
			WriteSignedVarInt(originY);
			WriteSignedVarInt(originZ);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			cacheEnabled = ReadBool();
			dimension = ReadSignedVarInt();
			originX = ReadSignedVarInt();
			originY = ReadSignedVarInt();
			originZ = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			cacheEnabled=default(bool);
			dimension=default(int);
			originX=default(int);
			originY=default(int);
			originZ=default(int);
		}

	}

	public partial class McpeSubChunkRequestPacket : Packet<McpeSubChunkRequestPacket>
	{


		public McpeSubChunkRequestPacket()
		{
			Id = 0xaf;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpePlayerStartItemCooldown : Packet<McpePlayerStartItemCooldown>
	{

		public string itemCategory; // = null;
		public int cooldownTicks; // = null;

		public McpePlayerStartItemCooldown()
		{
			Id = 0xb0;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(itemCategory);
			WriteSignedVarInt(cooldownTicks);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			itemCategory = ReadString();
			cooldownTicks = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			itemCategory=default(string);
			cooldownTicks=default(int);
		}

	}

	public partial class McpeScriptMessage : Packet<McpeScriptMessage>
	{

		public string messageId; // = null;
		public string messageValue; // = null;

		public McpeScriptMessage()
		{
			Id = 0xb1;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(messageId);
			Write(messageValue);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			messageId = ReadString();
			messageValue = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			messageId=default(string);
			messageValue=default(string);
		}

	}

	public partial class McpeCodeBuilderSource : Packet<McpeCodeBuilderSource>
	{

		public byte operation; // = null;
		public byte category; // = null;
		public byte codeStatus; // = null;

		public McpeCodeBuilderSource()
		{
			Id = 0xb2;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(operation);
			Write(category);
			Write(codeStatus);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			operation = ReadByte();
			category = ReadByte();
			codeStatus = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			operation=default(byte);
			category=default(byte);
			codeStatus=default(byte);
		}

	}

	public partial class McpeTickingAreasLoadStatus : Packet<McpeTickingAreasLoadStatus>
	{

		public bool waitingForPreload; // = null;

		public McpeTickingAreasLoadStatus()
		{
			Id = 0xb3;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(waitingForPreload);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			waitingForPreload = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			waitingForPreload=default(bool);
		}

	}

	public partial class McpeDimensionData : Packet<McpeDimensionData>
	{

		public DimensionDefinitions definitions; // = null;

		public McpeDimensionData()
		{
			Id = 0xb4;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(definitions);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			definitions = ReadDimensionDefinitions();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			definitions=default(DimensionDefinitions);
		}

	}

	public partial class McpeAgentActionEvent : Packet<McpeAgentActionEvent>
	{

		public string requestId; // = null;
		public int action; // = null;
		public string responseJson; // = null;

		public McpeAgentActionEvent()
		{
			Id = 0xb5;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(requestId);
			Write(action);
			Write(responseJson);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			requestId = ReadString();
			action = ReadInt();
			responseJson = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			requestId=default(string);
			action=default(int);
			responseJson=default(string);
		}

	}

	public partial class McpeChangeMobProperty : Packet<McpeChangeMobProperty>
	{

		public long actorUniqueId; // = null;
		public string propertyName; // = null;
		public bool boolValue; // = null;
		public string stringValue; // = null;
		public int intValue; // = null;
		public float floatValue; // = null;

		public McpeChangeMobProperty()
		{
			Id = 0xb6;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarLong(actorUniqueId);
			Write(propertyName);
			Write(boolValue);
			Write(stringValue);
			WriteSignedVarInt(intValue);
			Write(floatValue);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			actorUniqueId = ReadSignedVarLong();
			propertyName = ReadString();
			boolValue = ReadBool();
			stringValue = ReadString();
			intValue = ReadSignedVarInt();
			floatValue = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			actorUniqueId=default(long);
			propertyName=default(string);
			boolValue=default(bool);
			stringValue=default(string);
			intValue=default(int);
			floatValue=default(float);
		}

	}

	public partial class McpeLessonProgress : Packet<McpeLessonProgress>
	{

		public int action; // = null;
		public int score; // = null;
		public string activityId; // = null;

		public McpeLessonProgress()
		{
			Id = 0xb7;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(action);
			WriteSignedVarInt(score);
			Write(activityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			action = ReadSignedVarInt();
			score = ReadSignedVarInt();
			activityId = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			action=default(int);
			score=default(int);
			activityId=default(string);
		}

	}

	public partial class McpeRequestAbility : Packet<McpeRequestAbility>
	{

		public int abilityId; // = null;
		public byte valueType; // = null;
		public bool boolValue; // = null;
		public float floatValue; // = null;

		public McpeRequestAbility()
		{
			Id = 0xb8;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(abilityId);
			Write(valueType);
			Write(boolValue);
			Write(floatValue);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			abilityId = ReadSignedVarInt();
			valueType = ReadByte();
			boolValue = ReadBool();
			floatValue = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			abilityId=default(int);
			valueType=default(byte);
			boolValue=default(bool);
			floatValue=default(float);
		}

	}

	public partial class McpeRequestPermissions : Packet<McpeRequestPermissions>
	{

		public long targetActorUniqueId; // = null;
		public int playerPermission; // = null;
		public ushort customFlags; // = null;

		public McpeRequestPermissions()
		{
			Id = 0xb9;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteLe(targetActorUniqueId);
			WriteSignedVarInt(playerPermission);
			Write(customFlags);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			targetActorUniqueId = ReadLongLe();
			playerPermission = ReadSignedVarInt();
			customFlags = ReadUshort();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			targetActorUniqueId=default(long);
			playerPermission=default(int);
			customFlags=default(ushort);
		}

	}

	public partial class McpeToastRequest : Packet<McpeToastRequest>
	{

		public string title; // = null;
		public string content; // = null;

		public McpeToastRequest()
		{
			Id = 0xba;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(title);
			Write(content);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			title = ReadString();
			content = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			title=default(string);
			content=default(string);
		}

	}

	public partial class McpeUpdateAbilities : Packet<McpeUpdateAbilities>
	{

		public long entityUniqueId; // = null;
		public byte permissionLevel; // = null;
		public byte commandPermission; // = null;

		public McpeUpdateAbilities()
		{
			Id = 0xbb;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteLe(entityUniqueId);
			Write(permissionLevel);
			Write(commandPermission);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			entityUniqueId = ReadLongLe();
			permissionLevel = ReadByte();
			commandPermission = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			entityUniqueId=default(long);
			permissionLevel=default(byte);
			commandPermission=default(byte);
		}

	}

	public partial class McpeUpdateAdventureSettings : Packet<McpeUpdateAdventureSettings>
	{

		public bool noPvm; // = null;
		public bool noMvp; // = null;
		public bool immutableWorld; // = null;
		public bool showNameTags; // = null;
		public bool autoJump; // = null;

		public McpeUpdateAdventureSettings()
		{
			Id = 0xbc;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(noPvm);
			Write(noMvp);
			Write(immutableWorld);
			Write(showNameTags);
			Write(autoJump);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			noPvm = ReadBool();
			noMvp = ReadBool();
			immutableWorld = ReadBool();
			showNameTags = ReadBool();
			autoJump = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			noPvm=default(bool);
			noMvp=default(bool);
			immutableWorld=default(bool);
			showNameTags=default(bool);
			autoJump=default(bool);
		}

	}

	public partial class McpeDeathInfo : Packet<McpeDeathInfo>
	{

		public string cause; // = null;

		public McpeDeathInfo()
		{
			Id = 0xbd;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(cause);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			cause = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			cause=default(string);
		}

	}

	public partial class McpeEditorNetwork : Packet<McpeEditorNetwork>
	{

		public bool routeToManager; // = null;
		public NbtCompound payload; // = null;

		public McpeEditorNetwork()
		{
			Id = 0xbe;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(routeToManager);
			WriteNbtBody(payload);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			routeToManager = ReadBool();
			payload = ReadNbtBody();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			routeToManager=default(bool);
			payload=default(NbtCompound);
		}

	}

	public partial class McpeFeatureRegistry : Packet<McpeFeatureRegistry>
	{


		public McpeFeatureRegistry()
		{
			Id = 0xbf;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeServerStats : Packet<McpeServerStats>
	{

		public float serverTime; // = null;
		public float networkTime; // = null;

		public McpeServerStats()
		{
			Id = 0xc0;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(serverTime);
			Write(networkTime);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			serverTime = ReadFloat();
			networkTime = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverTime=default(float);
			networkTime=default(float);
		}

	}

	public partial class McpeRequestNetworkSettings : Packet<McpeRequestNetworkSettings>
	{

		public int protocolVersion; // = null;

		public McpeRequestNetworkSettings()
		{
			Id = 0xc1;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteBe(protocolVersion);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			protocolVersion = ReadIntBe();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			protocolVersion=default(int);
		}

	}

	public partial class McpeGameTestRequest : Packet<McpeGameTestRequest>
	{

		public int maxTestsPerBatch; // = null;
		public int repeatCount; // = null;
		public byte rotation; // = null;
		public bool stopOnFailure; // = null;
		public BlockCoordinates testPosition; // = null;
		public int testsPerRow; // = null;
		public string testName; // = null;

		public McpeGameTestRequest()
		{
			Id = 0xc2;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(maxTestsPerBatch);
			WriteSignedVarInt(repeatCount);
			Write(rotation);
			Write(stopOnFailure);
			Write(testPosition);
			WriteSignedVarInt(testsPerRow);
			Write(testName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			maxTestsPerBatch = ReadSignedVarInt();
			repeatCount = ReadSignedVarInt();
			rotation = ReadByte();
			stopOnFailure = ReadBool();
			testPosition = ReadBlockCoordinates();
			testsPerRow = ReadSignedVarInt();
			testName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			maxTestsPerBatch=default(int);
			repeatCount=default(int);
			rotation=default(byte);
			stopOnFailure=default(bool);
			testPosition=default(BlockCoordinates);
			testsPerRow=default(int);
			testName=default(string);
		}

	}

	public partial class McpeGameTestResults : Packet<McpeGameTestResults>
	{

		public bool success; // = null;
		public string error; // = null;
		public string testName; // = null;

		public McpeGameTestResults()
		{
			Id = 0xc3;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(success);
			Write(error);
			Write(testName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			success = ReadBool();
			error = ReadString();
			testName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			success=default(bool);
			error=default(string);
			testName=default(string);
		}

	}

	public partial class McpeUpdateClientInputLocks : Packet<McpeUpdateClientInputLocks>
	{

		public uint flags; // = null;

		public McpeUpdateClientInputLocks()
		{
			Id = 0xc4;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(flags);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			flags = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			flags=default(uint);
		}

	}

	public partial class McpeCameraPresets : Packet<McpeCameraPresets>
	{


		public McpeCameraPresets()
		{
			Id = 0xc6;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeUnlockedRecipes : Packet<McpeUnlockedRecipes>
	{

		public uint type; // = null;

		public McpeUnlockedRecipes()
		{
			Id = 0xc7;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(type);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			type = ReadUint();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			type=default(uint);
		}

	}

	public partial class McpeTrimData : Packet<McpeTrimData>
	{


		public McpeTrimData()
		{
			Id = 0x12e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeOpenSign : Packet<McpeOpenSign>
	{

		public BlockCoordinates blockPosition; // = null;
		public bool front; // = null;

		public McpeOpenSign()
		{
			Id = 0x12f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(blockPosition);
			Write(front);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			blockPosition = ReadBlockCoordinates();
			front = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			blockPosition=default(BlockCoordinates);
			front=default(bool);
		}

	}

	public partial class McpeAgentAnimation : Packet<McpeAgentAnimation>
	{

		public byte animationType; // = null;
		public long runtimeEntityId; // = null;

		public McpeAgentAnimation()
		{
			Id = 0x130;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(animationType);
			WriteUnsignedVarLong(runtimeEntityId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			animationType = ReadByte();
			runtimeEntityId = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			animationType=default(byte);
			runtimeEntityId=default(long);
		}

	}

	public partial class McpeRefreshEntitlements : Packet<McpeRefreshEntitlements>
	{


		public McpeRefreshEntitlements()
		{
			Id = 0x131;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpePlayerToggleCrafterSlotRequest : Packet<McpePlayerToggleCrafterSlotRequest>
	{

		public int posX; // = null;
		public int posY; // = null;
		public int posZ; // = null;
		public byte slotIndex; // = null;
		public bool isDisabled; // = null;

		public McpePlayerToggleCrafterSlotRequest()
		{
			Id = 0x132;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(posX);
			Write(posY);
			Write(posZ);
			Write(slotIndex);
			Write(isDisabled);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			posX = ReadInt();
			posY = ReadInt();
			posZ = ReadInt();
			slotIndex = ReadByte();
			isDisabled = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			posX=default(int);
			posY=default(int);
			posZ=default(int);
			slotIndex=default(byte);
			isDisabled=default(bool);
		}

	}

	public partial class McpeSetPlayerInventoryOptions : Packet<McpeSetPlayerInventoryOptions>
	{

		public int leftTab; // = null;
		public int rightTab; // = null;
		public bool filtering; // = null;
		public int layout; // = null;
		public int craftingLayout; // = null;

		public McpeSetPlayerInventoryOptions()
		{
			Id = 0x133;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(leftTab);
			WriteSignedVarInt(rightTab);
			Write(filtering);
			WriteSignedVarInt(layout);
			WriteSignedVarInt(craftingLayout);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			leftTab = ReadSignedVarInt();
			rightTab = ReadSignedVarInt();
			filtering = ReadBool();
			layout = ReadSignedVarInt();
			craftingLayout = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			leftTab=default(int);
			rightTab=default(int);
			filtering=default(bool);
			layout=default(int);
			craftingLayout=default(int);
		}

	}

	public partial class McpeSetHud : Packet<McpeSetHud>
	{


		public McpeSetHud()
		{
			Id = 0x134;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeAwardAchievement : Packet<McpeAwardAchievement>
	{

		public int achievementId; // = null;

		public McpeAwardAchievement()
		{
			Id = 0x135;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(achievementId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			achievementId = ReadInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			achievementId=default(int);
		}

	}

	public partial class McpeClientboundCloseForm : Packet<McpeClientboundCloseForm>
	{


		public McpeClientboundCloseForm()
		{
			Id = 0x136;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeServerBoundLoadingScreen : Packet<McpeServerBoundLoadingScreen>
	{

		public int type; // = null;

		public McpeServerBoundLoadingScreen()
		{
			Id = 0x138;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteSignedVarInt(type);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			type = ReadSignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			type=default(int);
		}

	}

	public partial class McpeJigsawStructureData : Packet<McpeJigsawStructureData>
	{

		public Nbt structureData; // = null;

		public McpeJigsawStructureData()
		{
			Id = 0x139;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(structureData);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			structureData = ReadNbt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			structureData=default(Nbt);
		}

	}

	public partial class McpeCurrentStructureFeature : Packet<McpeCurrentStructureFeature>
	{

		public string currentFeature; // = null;

		public McpeCurrentStructureFeature()
		{
			Id = 0x13a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(currentFeature);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			currentFeature = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			currentFeature=default(string);
		}

	}

	public partial class McpeServerBoundDiagnostics : Packet<McpeServerBoundDiagnostics>
	{

		public float averageFramesPerSecond; // = null;
		public float averageServerSimTickTime; // = null;
		public float averageClientSimTickTime; // = null;
		public float averageBeginFrameTime; // = null;
		public float averageInputTime; // = null;
		public float averageRenderTime; // = null;
		public float averageEndFrameTime; // = null;
		public float averageRemainderTimePercent; // = null;
		public float averageUnaccountedTimePercent; // = null;

		public McpeServerBoundDiagnostics()
		{
			Id = 0x13b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(averageFramesPerSecond);
			Write(averageServerSimTickTime);
			Write(averageClientSimTickTime);
			Write(averageBeginFrameTime);
			Write(averageInputTime);
			Write(averageRenderTime);
			Write(averageEndFrameTime);
			Write(averageRemainderTimePercent);
			Write(averageUnaccountedTimePercent);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			averageFramesPerSecond = ReadFloat();
			averageServerSimTickTime = ReadFloat();
			averageClientSimTickTime = ReadFloat();
			averageBeginFrameTime = ReadFloat();
			averageInputTime = ReadFloat();
			averageRenderTime = ReadFloat();
			averageEndFrameTime = ReadFloat();
			averageRemainderTimePercent = ReadFloat();
			averageUnaccountedTimePercent = ReadFloat();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			averageFramesPerSecond=default(float);
			averageServerSimTickTime=default(float);
			averageClientSimTickTime=default(float);
			averageBeginFrameTime=default(float);
			averageInputTime=default(float);
			averageRenderTime=default(float);
			averageEndFrameTime=default(float);
			averageRemainderTimePercent=default(float);
			averageUnaccountedTimePercent=default(float);
		}

	}

	public partial class McpeCameraAimAssist : Packet<McpeCameraAimAssist>
	{

		public string presetId; // = null;
		public Vector2 viewAngle; // = null;
		public float distance; // = null;
		public byte targetMode; // = null;
		public byte actionType; // = null;
		public bool showDebugRender; // = null;

		public McpeCameraAimAssist()
		{
			Id = 0x13c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(presetId);
			Write(viewAngle);
			Write(distance);
			Write(targetMode);
			Write(actionType);
			Write(showDebugRender);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			presetId = ReadString();
			viewAngle = ReadVector2();
			distance = ReadFloat();
			targetMode = ReadByte();
			actionType = ReadByte();
			showDebugRender = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			presetId=default(string);
			viewAngle=default(Vector2);
			distance=default(float);
			targetMode=default(byte);
			actionType=default(byte);
			showDebugRender=default(bool);
		}

	}

	public partial class McpeContainerRegistryCleanup : Packet<McpeContainerRegistryCleanup>
	{


		public McpeContainerRegistryCleanup()
		{
			Id = 0x13d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeMovementEffect : Packet<McpeMovementEffect>
	{

		public long runtimeEntityId; // = null;
		public uint effectType; // = null;
		public uint duration; // = null;
		public long tick; // = null;

		public McpeMovementEffect()
		{
			Id = 0x13e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(runtimeEntityId);
			WriteUnsignedVarInt(effectType);
			WriteUnsignedVarInt(duration);
			WriteUnsignedVarLong(tick);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			runtimeEntityId = ReadUnsignedVarLong();
			effectType = ReadUnsignedVarInt();
			duration = ReadUnsignedVarInt();
			tick = ReadUnsignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			runtimeEntityId=default(long);
			effectType=default(uint);
			duration=default(uint);
			tick=default(long);
		}

	}

	public partial class McpeClientCameraAimAssist : Packet<McpeClientCameraAimAssist>
	{

		public string presetId; // = null;
		public byte action; // = null;
		public bool allowAimAssist; // = null;

		public McpeClientCameraAimAssist()
		{
			Id = 0x141;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(presetId);
			Write(action);
			Write(allowAimAssist);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			presetId = ReadString();
			action = ReadByte();
			allowAimAssist = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			presetId=default(string);
			action=default(byte);
			allowAimAssist=default(bool);
		}

	}

	public partial class McpeCameraAimAssistPresets : Packet<McpeCameraAimAssistPresets>
	{


		public McpeCameraAimAssistPresets()
		{
			Id = 0x140;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeClientMovementPredictionSync : Packet<McpeClientMovementPredictionSync>
	{

		public float scale; // = null;
		public float width; // = null;
		public float height; // = null;
		public float movementSpeed; // = null;
		public float underwaterMovementSpeed; // = null;
		public float lavaMovementSpeed; // = null;
		public float jumpStrength; // = null;
		public float health; // = null;
		public float hunger; // = null;
		public float frictionModifier; // = null;
		public float bounciness; // = null;
		public float airDragModifier; // = null;
		public long actorUniqueId; // = null;
		public bool actorFlyingState; // = null;

		public McpeClientMovementPredictionSync()
		{
			Id = 0x142;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(scale);
			Write(width);
			Write(height);
			Write(movementSpeed);
			Write(underwaterMovementSpeed);
			Write(lavaMovementSpeed);
			Write(jumpStrength);
			Write(health);
			Write(hunger);
			Write(frictionModifier);
			Write(bounciness);
			Write(airDragModifier);
			WriteSignedVarLong(actorUniqueId);
			Write(actorFlyingState);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			scale = ReadFloat();
			width = ReadFloat();
			height = ReadFloat();
			movementSpeed = ReadFloat();
			underwaterMovementSpeed = ReadFloat();
			lavaMovementSpeed = ReadFloat();
			jumpStrength = ReadFloat();
			health = ReadFloat();
			hunger = ReadFloat();
			frictionModifier = ReadFloat();
			bounciness = ReadFloat();
			airDragModifier = ReadFloat();
			actorUniqueId = ReadSignedVarLong();
			actorFlyingState = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			scale=default(float);
			width=default(float);
			height=default(float);
			movementSpeed=default(float);
			underwaterMovementSpeed=default(float);
			lavaMovementSpeed=default(float);
			jumpStrength=default(float);
			health=default(float);
			hunger=default(float);
			frictionModifier=default(float);
			bounciness=default(float);
			airDragModifier=default(float);
			actorUniqueId=default(long);
			actorFlyingState=default(bool);
		}

	}

	public partial class McpeUpdateClientOptions : Packet<McpeUpdateClientOptions>
	{


		public McpeUpdateClientOptions()
		{
			Id = 0x143;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpePlayerVideoCapture : Packet<McpePlayerVideoCapture>
	{

		public bool recording; // = null;

		public McpePlayerVideoCapture()
		{
			Id = 0x144;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(recording);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			recording = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			recording=default(bool);
		}

	}

	public partial class McpePlayerUpdateEntityOverrides : Packet<McpePlayerUpdateEntityOverrides>
	{

		public long actorRuntimeId; // = null;
		public uint propertyIndex; // = null;
		public byte updateType; // = null;

		public McpePlayerUpdateEntityOverrides()
		{
			Id = 0x145;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarLong(actorRuntimeId);
			WriteUnsignedVarInt(propertyIndex);
			Write(updateType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			actorRuntimeId = ReadUnsignedVarLong();
			propertyIndex = ReadUnsignedVarInt();
			updateType = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			actorRuntimeId=default(long);
			propertyIndex=default(uint);
			updateType=default(byte);
		}

	}

	public partial class McpeClientboundControlSchemeSet : Packet<McpeClientboundControlSchemeSet>
	{

		public byte controlScheme; // = null;

		public McpeClientboundControlSchemeSet()
		{
			Id = 0x147;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(controlScheme);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			controlScheme = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			controlScheme=default(byte);
		}

	}

	public partial class McpePrimitiveShapes : Packet<McpePrimitiveShapes>
	{


		public McpePrimitiveShapes()
		{
			Id = 0x148;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeServerboundPackSettingChange : Packet<McpeServerboundPackSettingChange>
	{

		public UUID packId; // = null;
		public string name; // = null;
		public uint typeId; // = null;

		public McpeServerboundPackSettingChange()
		{
			Id = 0x149;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(packId);
			Write(name);
			WriteUnsignedVarInt(typeId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			packId = ReadUUID();
			name = ReadString();
			typeId = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			packId=default(UUID);
			name=default(string);
			typeId=default(uint);
		}

	}

	public partial class McpePlayerLocation : Packet<McpePlayerLocation>
	{

		public int type; // = null;
		public long entityUniqueId; // = null;

		public McpePlayerLocation()
		{
			Id = 0x146;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(type);
			WriteSignedVarLong(entityUniqueId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			type = ReadInt();
			entityUniqueId = ReadSignedVarLong();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			type=default(int);
			entityUniqueId=default(long);
		}

	}

	public partial class McpeClientboundDataStore : Packet<McpeClientboundDataStore>
	{


		public McpeClientboundDataStore()
		{
			Id = 0x14a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeGraphicsOverrideParameter : Packet<McpeGraphicsOverrideParameter>
	{


		public McpeGraphicsOverrideParameter()
		{
			Id = 0x14b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeServerboundDataStore : Packet<McpeServerboundDataStore>
	{

		public string name; // = null;
		public string property; // = null;
		public string path; // = null;

		public McpeServerboundDataStore()
		{
			Id = 0x14c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(name);
			Write(property);
			Write(path);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			name = ReadString();
			property = ReadString();
			path = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			name=default(string);
			property=default(string);
			path=default(string);
		}

	}

	public partial class McpeClientboundDataDrivenUiShowScreen : Packet<McpeClientboundDataDrivenUiShowScreen>
	{

		public string screenId; // = null;
		public uint formId; // = null;

		public McpeClientboundDataDrivenUiShowScreen()
		{
			Id = 0x14d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(screenId);
			Write(formId);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			screenId = ReadString();
			formId = ReadUint();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			screenId=default(string);
			formId=default(uint);
		}

	}

	public partial class McpeClientboundDataDrivenUiCloseScreen : Packet<McpeClientboundDataDrivenUiCloseScreen>
	{


		public McpeClientboundDataDrivenUiCloseScreen()
		{
			Id = 0x14e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeClientboundDataDrivenUiReload : Packet<McpeClientboundDataDrivenUiReload>
	{


		public McpeClientboundDataDrivenUiReload()
		{
			Id = 0x14f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeClientboundTextureShift : Packet<McpeClientboundTextureShift>
	{

		public byte actionId; // = null;
		public string collectionName; // = null;
		public string fromStep; // = null;
		public string toStep; // = null;

		public McpeClientboundTextureShift()
		{
			Id = 0x150;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(actionId);
			Write(collectionName);
			Write(fromStep);
			Write(toStep);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			actionId = ReadByte();
			collectionName = ReadString();
			fromStep = ReadString();
			toStep = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			actionId=default(byte);
			collectionName=default(string);
			fromStep=default(string);
			toStep=default(string);
		}

	}

	public partial class McpeVoxelShapes : Packet<McpeVoxelShapes>
	{


		public McpeVoxelShapes()
		{
			Id = 0x151;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCameraSpline : Packet<McpeCameraSpline>
	{


		public McpeCameraSpline()
		{
			Id = 0x152;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCameraAimAssistActorPriority : Packet<McpeCameraAimAssistActorPriority>
	{


		public McpeCameraAimAssistActorPriority()
		{
			Id = 0x153;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeResourcePacksReadyForValidation : Packet<McpeResourcePacksReadyForValidation>
	{


		public McpeResourcePacksReadyForValidation()
		{
			Id = 0x154;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCameraInstruction : Packet<McpeCameraInstruction>
	{


		public McpeCameraInstruction()
		{
			Id = 0x12c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeCameraShake : Packet<McpeCameraShake>
	{

		public float intensity; // = null;
		public float duration; // = null;
		public byte type; // = null;
		public byte action; // = null;

		public McpeCameraShake()
		{
			Id = 0x9f;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(intensity);
			Write(duration);
			Write(type);
			Write(action);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			intensity = ReadFloat();
			duration = ReadFloat();
			type = ReadByte();
			action = ReadByte();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			intensity=default(float);
			duration=default(float);
			type=default(byte);
			action=default(byte);
		}

	}

	public partial class McpeLocatorBar : Packet<McpeLocatorBar>
	{


		public McpeLocatorBar()
		{
			Id = 0x155;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpePartyChanged : Packet<McpePartyChanged>
	{

		public string partyId; // = null;
		public bool partyLeader; // = null;

		public McpePartyChanged()
		{
			Id = 0x156;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(partyId);
			Write(partyLeader);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			partyId = ReadString();
			partyLeader = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			partyId=default(string);
			partyLeader=default(bool);
		}

	}

	public partial class McpeServerboundDataDrivenScreenClosed : Packet<McpeServerboundDataDrivenScreenClosed>
	{

		public uint formId; // = null;
		public string closeReason; // = null;

		public McpeServerboundDataDrivenScreenClosed()
		{
			Id = 0x157;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(formId);
			Write(closeReason);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			formId = ReadUint();
			closeReason = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			formId=default(uint);
			closeReason=default(string);
		}

	}

	public partial class McpeSyncWorldClocks : Packet<McpeSyncWorldClocks>
	{

		public uint payloadType; // = null;

		public McpeSyncWorldClocks()
		{
			Id = 0x158;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(payloadType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			payloadType = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			payloadType=default(uint);
		}

	}

	public partial class McpeClientboundAttributeLayerSync : Packet<McpeClientboundAttributeLayerSync>
	{

		public uint payloadType; // = null;

		public McpeClientboundAttributeLayerSync()
		{
			Id = 0x159;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			WriteUnsignedVarInt(payloadType);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			payloadType = ReadUnsignedVarInt();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			payloadType=default(uint);
		}

	}

	public partial class McpeServerStoreInfo : Packet<McpeServerStoreInfo>
	{


		public McpeServerStoreInfo()
		{
			Id = 0x15a;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeServerPresenceInfo : Packet<McpeServerPresenceInfo>
	{


		public McpeServerPresenceInfo()
		{
			Id = 0x15b;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class McpeClientboundUpdateSoundData : Packet<McpeClientboundUpdateSoundData>
	{

		public ulong serverSoundHandle; // = null;
		public string soundEvent; // = null;

		public McpeClientboundUpdateSoundData()
		{
			Id = 0x15c;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(serverSoundHandle);
			Write(soundEvent);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			serverSoundHandle = ReadUlong();
			soundEvent = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			serverSoundHandle=default(ulong);
			soundEvent=default(string);
		}

	}

	public partial class McpeSendPartyDestinationCookie : Packet<McpeSendPartyDestinationCookie>
	{

		public string cookie; // = null;
		public string intent; // = null;
		public string destinationName; // = null;

		public McpeSendPartyDestinationCookie()
		{
			Id = 0x15d;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(cookie);
			Write(intent);
			Write(destinationName);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			cookie = ReadString();
			intent = ReadString();
			destinationName = ReadString();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			cookie=default(string);
			intent=default(string);
			destinationName=default(string);
		}

	}

	public partial class McpePartyDestinationCookieResponse : Packet<McpePartyDestinationCookieResponse>
	{

		public string cookie; // = null;
		public bool accepted; // = null;

		public McpePartyDestinationCookieResponse()
		{
			Id = 0x15e;
			IsMcpe = true;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(cookie);
			Write(accepted);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			cookie = ReadString();
			accepted = ReadBool();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			cookie=default(string);
			accepted=default(bool);
		}

	}

	public partial class McpeWrapper : Packet<McpeWrapper>
	{


		public McpeWrapper()
		{
			Id = 0xfe;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();


			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();


			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

		}

	}

	public partial class FtlCreatePlayer : Packet<FtlCreatePlayer>
	{

		public string username; // = null;
		public UUID clientuuid; // = null;
		public string serverAddress; // = null;
		public long clientId; // = null;
		public Skin skin; // = null;

		public FtlCreatePlayer()
		{
			Id = 0x01;
			IsMcpe = false;
		}

		protected override void EncodePacket()
		{
			base.EncodePacket();

			BeforeEncode();

			Write(username);
			Write(clientuuid);
			Write(serverAddress);
			Write(clientId);
			Write(skin);

			AfterEncode();
		}

		partial void BeforeEncode();
		partial void AfterEncode();

		protected override void DecodePacket()
		{
			base.DecodePacket();

			BeforeDecode();

			username = ReadString();
			clientuuid = ReadUUID();
			serverAddress = ReadString();
			clientId = ReadLong();
			skin = ReadSkin();

			AfterDecode();
		}

		partial void BeforeDecode();
		partial void AfterDecode();

		protected override void ResetPacket()
		{
			base.ResetPacket();

			username=default(string);
			clientuuid=default(UUID);
			serverAddress=default(string);
			clientId=default(long);
			skin=default(Skin);
		}

	}

}

