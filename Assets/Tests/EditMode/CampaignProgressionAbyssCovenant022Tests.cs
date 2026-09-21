#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;
namespace SecondDimension.Tests.EditMode
{
 public sealed class CampaignProgressionAbyssCovenant022Tests
 {
  CampaignRegistry022 _r;
  CampaignProgressionCommandService022 _service;
  M2CombatContent _combat;
  M2BattleCommandService _battle;
  [SetUp] public void Setup(){_r=CampaignRegistry022.LoadFromResources();_service=new CampaignProgressionCommandService022();_combat=M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT"));_battle=new M2BattleCommandService();}
  [Test] public void ContentCountsMatchAuthority(){Assert.AreEqual(12,_r.WeaponTracks.Count);Assert.AreEqual(72,_r.WeaponRecipes.Count);Assert.AreEqual(144,_r.ArmorRecipes.Count);Assert.AreEqual(18,_r.Classes.Count);Assert.AreEqual(30,_r.CertificationPaths.Count);Assert.AreEqual(10,_r.Floors.Count);Assert.AreEqual(40,_r.AbyssOperations.Count);Assert.AreEqual(30,_r.AbyssOperations.Values.Count(x=>x.kind!=CampaignProgressionCommandService022.EndlessBattleKind094));Assert.AreEqual(10,_r.AbyssOperations.Values.Count(x=>x.kind==CampaignProgressionCommandService022.EndlessBattleKind094));Assert.AreEqual(24,_r.ArtifactBases.Count);Assert.AreEqual(30,_r.Affixes.Count);Assert.AreEqual(12,_r.ArtifactPaths.Count);Assert.AreEqual(10,_r.Echoes.Count);Assert.AreEqual(8,_r.Covenants.Count);}
  [Test] public void CurrentSaveFormatIncludesProgressionState()=>Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion,9);
  [Test] public void EveryWeaponTrackReachesGodly()=>Assert.IsTrue(_r.WeaponTracks.Values.All(t=>_r.WeaponRecipes.Values.Count(x=>x.trackId==t.trackId)==6&&_r.WeaponRecipes.Values.Any(x=>x.trackId==t.trackId&&x.toTier=="GODLY"&&x.godlyRequiresStoryOrAbyssGate)));
  [Test] public void EveryCovenantRequiresVoluntaryAcceptance()=>Assert.IsTrue(_r.Covenants.Values.All(x=>x.voluntaryAcceptanceRequired&&!x.sentientOwnershipAllowed&&x.standardControl=="STORY_AND_COVENANT_FORECASTS"));
  [Test] public void SummonEchoesNeverExposeIndividualSelection()=>Assert.IsTrue(_r.Echoes.Values.All(x=>!x.directIndividualSelection));
  [Test] public void AbyssUsesExistingRewardAuthority()=>Assert.IsTrue(_r.AbyssOperations.Values.All(x=>x.exactOnceReceipts&&x.existingEquipmentRewardRemainsAuthoritative));
  [Test] public void AbyssOperationsEnforcePreviousFloorPositiveRewardsExactOnceAndUniqueMaterials()
  {
   foreach(var operation in _r.AbyssOperations.Values)
   {
    var floor=_r.Floors[operation.floorId];
    Assert.AreEqual(floor.floor>1,operation.requiresPreviousFloorClear,operation.operationId);
    Assert.Greater(operation.guildXp,0,operation.operationId);Assert.Greater(operation.hallXp,0,operation.operationId);Assert.GreaterOrEqual(operation.summonResonance,0,operation.operationId);
    Assert.IsTrue(operation.exactOnceReceipts,operation.operationId);Assert.IsTrue(operation.existingEquipmentRewardRemainsAuthoritative,operation.operationId);Assert.IsTrue(operation.steps.All(x=>x.exactOnce),operation.operationId);
    Assert.Greater(operation.rewardMaterialIds.Length,0,operation.operationId);Assert.AreEqual(operation.rewardMaterialIds.Length,operation.rewardMaterialIds.Distinct(StringComparer.Ordinal).Count(),operation.operationId);
   }
   var authored=_r.AbyssOperations["ABYSS_OP022_01_RECON"];var original=authored.rewardMaterialIds;
   try{authored.rewardMaterialIds=new[]{original[0],original[0]};Assert.Throws<InvalidOperationException>(()=>_r.ValidateOrThrow());}
   finally{authored.rewardMaterialIds=original;}
  }
  [Test] public void DefaultProgressionStateIsEmptyAndStable(){var s=CampaignProgressionState022.Default();Assert.AreEqual(0,s.SummonResonance);Assert.IsNull(s.ActiveAbyssOperation);Assert.AreEqual(0,s.InvocationArtifacts.Count);}
  [Test,Timeout(10000)]
  public void LongRunningTowerReceiptLedgerCopiesWithoutQuadraticGrowthAndSortsDeterministically()
  {
   var receiptIds=Enumerable.Range(0,50000)
    .Select(index=>"ABYSSREC022_"+(49999-index).ToString("D5"))
    .Concat(new[]{"ABYSSREC022_00042",string.Empty,"   "})
    .ToArray();
   var floor=new AbyssFloorProgressState022("ABYSS_FLOOR_SCALE_084",0,0,
    false,false,receiptIds);
   var state=CampaignProgressionState022.Default().With(
    appliedReceiptIds:receiptIds,
    abyssAuthorityBaseReceiptIds:receiptIds);

   Assert.AreEqual(50000,floor.AppliedReceiptIds.Count);
   Assert.AreEqual(50000,state.AppliedReceiptIds.Count);
   Assert.AreEqual(50000,state.AbyssAuthorityBaseReceiptIds.Count);
   Assert.AreEqual("ABYSSREC022_00000",state.AppliedReceiptIds[0]);
   Assert.AreEqual("ABYSSREC022_49999",state.AppliedReceiptIds[49999]);
   CollectionAssert.AreEqual(floor.AppliedReceiptIds,state.AppliedReceiptIds);
   CollectionAssert.AreEqual(state.AppliedReceiptIds,
    state.AbyssAuthorityBaseReceiptIds);
  }
  [Test] public void SaveFormatElevenLegacyFloorWithoutOptionalProofStillDeserializes(){var floor=JsonConvert.DeserializeObject<AbyssFloorProgressState022>("{\"FloorId\":\"ABYSS_FLOOR_001_MUD_TRENCHES\",\"ClearCount\":1,\"HighestJudgmentTier\":1,\"FirstClearApplied\":true,\"MarginResolved\":false,\"AppliedReceiptIds\":[]}");Assert.NotNull(floor);Assert.IsNull(floor.FirstClearProof);Assert.AreEqual(1,floor.ClearCount);Assert.AreEqual(11,SaveEnvelopeV1.CurrentFormatVersion);}

  [Test]
  public void InactiveLegacyTowerSaveMigratesOncePreservesProgressAndCanBeginAgainAfterReload()
  {
   var copiedSave=CreateAbyssBattleReadyCampaign();
   var source=Progression(copiedSave);
   var legacy=new CampaignProgressionState022(
    "CAMPAIGN_PROGRESSION_ABYSS_COVENANT_022_1.0",
    source.EquipmentEvolution,source.ClassCertifications,source.AbyssFloors,null,
    source.InvocationArtifacts,source.Covenants,source.AboveGroundChangeIds,
    source.AppliedReceiptIds,source.SummonResonance,"legacy_tower_copy_084",
    legacyAbyssOperationArchives:source.LegacyAbyssOperationArchives);
   var legacyCampaign=WithProgression(copiedSave,legacy);
   Assert.IsTrue(CampaignProgressionCommandService022
    .RequiresLegacyAbyssRecovery084(Progression(legacyCampaign)));

   var migrated=Require(_service.MigrateInactiveLegacyAbyssState084(
    legacyCampaign));
   var migratedState=Progression(migrated);
   Assert.AreEqual(CampaignProgressionState022.ContentVersion,
    migratedState.ContentAuthorityVersion);
   Assert.AreEqual(CanonicalJson.Serialize(source.AbyssFloors),
    CanonicalJson.Serialize(migratedState.AbyssFloors));
   Assert.AreEqual(CanonicalJson.Serialize(source.AppliedReceiptIds),
    CanonicalJson.Serialize(migratedState.AppliedReceiptIds));
   Assert.AreEqual(CanonicalJson.Serialize(source.AboveGroundChangeIds),
    CanonicalJson.Serialize(migratedState.AboveGroundChangeIds));
   Assert.AreEqual(source.SummonResonance,migratedState.SummonResonance);
   Assert.IsNotEmpty(migratedState.AbyssAuthorityMigrationReceiptId);
   Assert.IsNotEmpty(migratedState.AbyssAuthorityBaseHash);
   Assert.IsFalse(_service.MigrateInactiveLegacyAbyssState084(migrated).IsSuccess);

   var reloaded=JsonConvert.DeserializeObject<CampaignState>(
    JsonConvert.SerializeObject(migrated));
   var restarted=Require(_service.BeginAbyssOperation(reloaded,_r,
    "ABYSS_OP022_01_GUARDIAN"));
   Assert.AreEqual("ABYSS_OP022_01_GUARDIAN",
    Progression(restarted).ActiveAbyssOperation.OperationDefinitionId);
  }

  [Test]
  public void WeaponEvolutionReplayIsIdempotentWhileFirstTimeTrackAndTierRemainValidated()
  {
   const string itemId="WEAPON_EVOLUTION_022_TEST";
   const string recipeId="WRECIPE022_01_01";
   var ready=CreateWeaponEvolutionCampaign(itemId,"WTRACK022_01","TRAINING",Array.Empty<string>());
   var first=Require(_service.EvolveWeapon(ready,_r,itemId,recipeId));
   var firstHash=CanonicalJson.Sha256Hex(first);
   var replayResult=_service.EvolveWeapon(first,_r,itemId,recipeId);

   Assert.IsTrue(replayResult.IsSuccess,string.Join("\n",replayResult.Errors));
   var replay=replayResult.Value;
   Assert.AreEqual(firstHash,CanonicalJson.Sha256Hex(replay));
   var evolved=Progression(replay).EquipmentEvolution.Single(x=>x.ItemInstanceId==itemId);
   Assert.AreEqual("COMMON",evolved.TierId);
   Assert.AreEqual(1,evolved.AppliedRecipeIds.Count(x=>x==recipeId));
   Assert.AreEqual(1,WorldMaterial(replay,"MAT020_SKYHOME_02"));
   Assert.AreEqual(1,WorldMaterial(replay,"MAT020_BEAST_02"));

   var wrongTrack=_service.EvolveWeapon(
    CreateWeaponEvolutionCampaign(itemId,"WTRACK022_02","TRAINING",Array.Empty<string>()),
    _r,itemId,recipeId);
   Assert.IsFalse(wrongTrack.IsSuccess);
   CollectionAssert.Contains(wrongTrack.Errors,"CAMPAIGN022_RECIPE_TIER_ILLEGAL");

   var wrongTier=_service.EvolveWeapon(
    CreateWeaponEvolutionCampaign(itemId,"WTRACK022_01","COMMON",Array.Empty<string>()),
    _r,itemId,recipeId);
   Assert.IsFalse(wrongTier.IsSuccess);
   CollectionAssert.Contains(wrongTier.Errors,"CAMPAIGN022_RECIPE_TIER_ILLEGAL");
  }

  [Test]
  public void NonBattleAbyssCompletionCreatesCanonicalTerminalReceiptAndSurvivesSaveRoundTrip()
  {
   var ready=CompleteNonBattleOperation("ABYSS_OP022_01_RECON");
   var first=Require(_service.CommitAbyssOperationCompletion(ready,_r));
   var second=Require(_service.CommitAbyssOperationCompletion(CompleteNonBattleOperation("ABYSS_OP022_01_RECON"),_r));
   var receipt=Progression(first).ActiveAbyssOperation.PendingReceipt;
   Assert.NotNull(receipt);
   StringAssert.StartsWith("ABYSSREC022_",receipt.ReceiptId);
   Assert.AreEqual("ABYSS_OP022_01_RECON",receipt.SourceId);
   Assert.AreEqual("OPERATION_COMPLETE",receipt.Outcome);
   Assert.AreEqual(1,receipt.AppliedVersion);
   Assert.AreEqual(_r.AbyssOperations[receipt.SourceId].guildXp,receipt.GuildXp);
   Assert.AreEqual(_r.AbyssOperations[receipt.SourceId].hallXp,receipt.HallXp);
   CollectionAssert.AreEquivalent(_r.AbyssOperations[receipt.SourceId].rewardMaterialIds,receipt.MaterialIds);
   Assert.AreEqual(receipt.ReceiptId,Progression(second).ActiveAbyssOperation.PendingReceipt.ReceiptId);
   Assert.AreEqual(receipt.AuthoritativeHash,Progression(second).ActiveAbyssOperation.PendingReceipt.AuthoritativeHash);

   var reloaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(first));
   Assert.NotNull(reloaded);
   var applied=Require(_service.ApplyAbyssOperationCompletion(reloaded,_r));
   Assert.IsNull(Progression(applied).ActiveAbyssOperation);
  }

  [Test]
  public void NonBattleAbyssCompletionAwardsAuthoredRewardsAndCityChangeExactlyOnce()
  {
   var committed=Require(_service.CommitAbyssOperationCompletion(CompleteNonBattleOperation("ABYSS_OP022_01_RECON"),_r));
   var receiptId=Progression(committed).ActiveAbyssOperation.PendingReceipt.ReceiptId;
   var beforeTreasury=committed.Guild.TreasuryXp;
   var beforeLifetime=committed.Guild.Development.LifetimeTreasuryXpEarned;
   var beforeHall=committed.Guild.Development.HallEnhancementXp;
   var beforeFloorClear=Progression(committed).AbyssFloors.Single(x=>
    x.FloorId==_r.AbyssOperations["ABYSS_OP022_01_RECON"].floorId).ClearCount;
   var beforeMaterials=committed.Guild.GuildCity.Strategic017H.Campaign019
    .Playable020.WorldMaterials.ToDictionary(value=>value.MaterialId,
     value=>value.Amount,StringComparer.Ordinal);
   var first=Require(_service.ApplyAbyssOperationCompletion(committed,_r));
   var repeatedFromSameCommit=Require(_service.ApplyAbyssOperationCompletion(committed,_r));
   var state=Progression(first);
   var op=_r.AbyssOperations["ABYSS_OP022_01_RECON"];
   var floor=_r.Floors[op.floorId];

   Assert.IsNull(state.ActiveAbyssOperation);
   Assert.AreEqual(beforeFloorClear+1,
    state.AbyssFloors.Single(x=>x.FloorId==op.floorId).ClearCount);
   Assert.AreEqual(1,state.AbyssFloors.Single(x=>x.FloorId==op.floorId).AppliedReceiptIds.Count(x=>x==receiptId));
   Assert.AreEqual(1,state.AppliedReceiptIds.Count(x=>x==receiptId));
   CollectionAssert.Contains(state.AboveGroundChangeIds,floor.firstClearAboveGroundChangeId);
   Assert.AreEqual(op.guildXp,first.Guild.TreasuryXp-beforeTreasury);
   Assert.AreEqual(op.guildXp,
    first.Guild.Development.LifetimeTreasuryXpEarned-beforeLifetime);
   Assert.AreEqual(op.hallXp,
    first.Guild.Development.HallEnhancementXp-beforeHall);
   Assert.AreEqual(1,first.Guild.Development.ClaimedBattleRewardIds.Count(x=>x==receiptId));
   foreach(var materialId in op.rewardMaterialIds)
    Assert.AreEqual(4,WorldMaterial(first,materialId)-
     (beforeMaterials.TryGetValue(materialId,out var amount)?amount:0));
   Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(repeatedFromSameCommit));

   var duplicate=_service.ApplyAbyssOperationCompletion(first,_r);
   Assert.IsFalse(duplicate.IsSuccess);
   CollectionAssert.Contains(duplicate.Errors,"CAMPAIGN022_ABYSS_READY_TO_FINALIZE_REQUIRED");
  }

  [Test]
  public void NonBattleAbyssCompletionRejectsIncompleteOperation()
  {
   var prerequisite=EnsureFloorClearedForOperation084(
    CreateAbyssBattleReadyCampaign(),"ABYSS_OP022_01_RECON");
   var beforeFloors=CanonicalJson.Serialize(Progression(prerequisite).AbyssFloors);
   var started=Require(_service.BeginAbyssOperation(prerequisite,_r,
    "ABYSS_OP022_01_RECON"));
   var rejected=_service.CommitAbyssOperationCompletion(started,_r);
   Assert.IsFalse(rejected.IsSuccess);
   CollectionAssert.Contains(rejected.Errors,"CAMPAIGN022_ABYSS_READY_TO_FINALIZE_REQUIRED");
   Assert.AreEqual(beforeFloors,
    CanonicalJson.Serialize(Progression(started).AbyssFloors));
  }

  [Test]
  public void RetreatAfterAppliedTileReloadsAndStartsANewCanonicalRun()
  {
   const string operationId="ABYSS_OP022_01_GUARDIAN";
   var campaign=Require(_service.BeginAbyssOperation(
    CreateAbyssBattleReadyCampaign(),_r,operationId));
   var firstRun=Progression(campaign).ActiveAbyssOperation;
   campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
   campaign=Require(_service.ApplyAbyssStep(campaign,_r));
   var firstReceipt=Progression(campaign).ActiveAbyssOperation
    .AppliedStepProofs.Single().Receipt.ReceiptId;
   var firstOrdinal=campaign.Guild.GuildCity.OperationOrdinal;

   var retreated=Require(_service.RetreatAbyssOperation(campaign,_r));
   Assert.IsNull(Progression(retreated).ActiveAbyssOperation);
   Assert.AreEqual(1,Progression(retreated).AppliedReceiptIds
    .Count(value=>value==firstReceipt));
   Assert.IsTrue(retreated.Guild.Development
    .HasAdventureAuthority(firstReceipt));

   var reloaded=JsonConvert.DeserializeObject<CampaignState>(
    JsonConvert.SerializeObject(retreated));
   var restarted=Require(_service.BeginAbyssOperation(reloaded,_r,operationId));
   var secondRun=Progression(restarted).ActiveAbyssOperation;
   Assert.AreNotEqual(firstRun.OperationInstanceId,secondRun.OperationInstanceId);
   Assert.AreEqual(0,secondRun.CurrentStepIndex);
   Assert.AreEqual(firstOrdinal+1,restarted.Guild.GuildCity.OperationOrdinal);
   restarted=Require(_service.CommitAbyssStep(restarted,_r,"SUCCESS"));
   restarted=Require(_service.ApplyAbyssStep(restarted,_r));
   Assert.AreEqual(1,Progression(restarted).ActiveAbyssOperation.CurrentStepIndex);
   Assert.AreEqual(2,Progression(restarted).AppliedReceiptIds.Count);
  }

  [Test]
  public void BattleFinalizerRejectsOrdinaryNonBattleStepReceipt()
  {
   var prerequisite=EnsureFloorClearedForOperation084(
    CreateAbyssBattleReadyCampaign(),"ABYSS_OP022_01_RECON");
   var campaign=Require(_service.BeginAbyssOperation(prerequisite,_r,
    "ABYSS_OP022_01_RECON"));
   var stepCount=_r.AbyssOperations["ABYSS_OP022_01_RECON"].steps.Length;
   for(var i=0;i<stepCount-1;i++)
   {
    campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
    campaign=Require(_service.ApplyAbyssStep(campaign,_r));
   }
   campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
   StringAssert.StartsWith("PROGREC022_",Progression(campaign).ActiveAbyssOperation.PendingReceipt.ReceiptId);

   var beforeRejected=CanonicalJson.Sha256Hex(campaign);
   var rejected=_service.ApplyAbyssBattleAndFinalize(campaign,_r);
   Assert.IsFalse(rejected.IsSuccess);
   CollectionAssert.Contains(rejected.Errors,"CAMPAIGN022_ABYSS_AWAITING_BATTLE_REQUIRED");
   var rejectedAgain=_service.ApplyAbyssBattleAndFinalize(campaign,_r);
   Assert.IsFalse(rejectedAgain.IsSuccess);
   Assert.AreEqual(CanonicalJson.Serialize(rejected.Errors),
    CanonicalJson.Serialize(rejectedAgain.Errors));
   Assert.AreEqual(beforeRejected,CanonicalJson.Sha256Hex(campaign));
  }

  [Test]
  public void BattleFinalizerRejectsProgReceiptEvenInForgedBattleContext()
  {
   const string operationId="ABYSS_OP022_01_GUARDIAN";
   var campaign=Require(_service.BeginAbyssOperation(
    CreateAbyssBattleReadyCampaign(),_r,operationId));
   var operation=_r.AbyssOperations[operationId];
   while(!operation.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle)
   {
    campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
    campaign=Require(_service.ApplyAbyssStep(campaign,_r));
   }
   var state=Progression(campaign);
   var active=state.ActiveAbyssOperation;
   var battleStep=operation.steps[active.CurrentStepIndex];
   var forged=new ProgressionReceipt022("PROGREC022_FORGED_BATTLE_CONTEXT",battleStep.stepId,"SUCCESS","FORGED_HASH",0,0,0,Array.Empty<string>(),0);
   active=active.With(status:AbyssOperationStatus022.AwaitingBattle,pendingReceipt:forged,replacePendingReceipt:true,existingBattleRewardReceiptId:"FORGED_REWARD");
   campaign=WithProgression(campaign,state.With(activeAbyssOperation:active,replaceActiveAbyssOperation:true,lastCheckpointId:"forged_battle_context"));

   var rejected=_service.ApplyAbyssBattleAndFinalize(campaign,_r);
   Assert.IsFalse(rejected.IsSuccess);
   CollectionAssert.Contains(rejected.Errors,"CAMPAIGN022_ABYSS_BATTLE_RECEIPT_INVALID");
   Assert.AreEqual(0,Progression(campaign).AbyssFloors.Count);
  }

  [Test]
  public void VerifiedGuardianBattleAdvancesOneStepAndTerminalCompletionAwardsExactlyOnce()
  {
   const string operationId="ABYSS_OP022_01_GUARDIAN";
   const string existingRewardId="BATTLE_REWARD_GUARDIAN_022_TEST";
   var operation=_r.AbyssOperations[operationId];
   var campaign=Require(_service.BeginAbyssOperation(CreateAbyssBattleReadyCampaign(),_r,operationId));
   while(!operation.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle)
   {
    campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
    campaign=Require(_service.ApplyAbyssStep(campaign,_r));
   }
   var battleIndex=Progression(campaign).ActiveAbyssOperation.CurrentStepIndex;
   var battleStep=operation.steps[battleIndex];
   campaign=Require(_service.CommitAbyssBattleEncounter(campaign,_r));
   var encounter=campaign.Guild.GuildCity.PendingEncounter;
   Assert.NotNull(encounter);
   campaign=WithClaimedBattle(campaign,existingRewardId,encounter);
   var returnCommitted=Require(_service.CommitAbyssBattleReturn(campaign,_r));
   var returnCommittedRetry=Require(_service.CommitAbyssBattleReturn(campaign,_r));
   Assert.AreEqual(CanonicalJson.Serialize(returnCommitted),CanonicalJson.Serialize(returnCommittedRetry));
   Assert.IsTrue(_service.HasActiveAbyssEncounter(returnCommitted,_r));
   var returnReceiptId=returnCommitted.Guild.GuildCity.PendingBattleReturn.ReceiptId;
   var returned=Require(_service.ApplyAbyssBattleReturnExactlyOnce(returnCommitted,_r));
   var returnedFromSameCommit=Require(_service.ApplyAbyssBattleReturnExactlyOnce(returnCommitted,_r));
   var returnedReplay=Require(_service.ApplyAbyssBattleReturnExactlyOnce(returned,_r));
   Assert.AreEqual(CanonicalJson.Serialize(returned),CanonicalJson.Serialize(returnedFromSameCommit));
   Assert.AreEqual(CanonicalJson.Serialize(returned),CanonicalJson.Serialize(returnedReplay));
   Assert.IsNull(returned.Guild.GuildCity.PendingEncounter);
   Assert.IsNull(returned.Guild.GuildCity.PendingBattleReturn);
   Assert.AreEqual(1,returned.Guild.GuildCity.AppliedBattleReturnIds.Count(x=>x==returnReceiptId));

   var battleCommitted=Require(_service.CommitAbyssBattleResult(returned,_r));
   var battleReceiptId=Progression(battleCommitted).ActiveAbyssOperation.PendingReceipt.ReceiptId;
   var afterBattleStep=Require(_service.ApplyAbyssBattleAndFinalize(battleCommitted,_r));
   var afterBattle=Progression(afterBattleStep).ActiveAbyssOperation;
   Assert.AreEqual(battleIndex+1,afterBattle.CurrentStepIndex);
   Assert.AreEqual(AbyssOperationStatus022.Active,afterBattle.Status);
   Assert.IsNull(afterBattle.PendingReceipt);
   CollectionAssert.Contains(afterBattle.CompletedStepIds,battleStep.stepId);
   Assert.AreEqual(1,Progression(afterBattleStep).AppliedReceiptIds.Count(x=>x==battleReceiptId));
   Assert.AreEqual(0,Progression(afterBattleStep).AbyssFloors.Count);
   Assert.AreEqual(0,Progression(afterBattleStep).AboveGroundChangeIds.Count);
   Assert.AreEqual(0,afterBattleStep.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldMaterials.Count);
   Assert.AreEqual(7,afterBattleStep.Guild.TreasuryXp);

   var premature=_service.CommitAbyssOperationCompletion(afterBattleStep,_r);
   Assert.IsFalse(premature.IsSuccess);
   CollectionAssert.Contains(premature.Errors,"CAMPAIGN022_ABYSS_READY_TO_FINALIZE_REQUIRED");
   while(Progression(afterBattleStep).ActiveAbyssOperation.CurrentStepIndex<operation.steps.Length)
   {
    afterBattleStep=Require(_service.CommitAbyssStep(afterBattleStep,_r,"SUCCESS"));
    afterBattleStep=Require(_service.ApplyAbyssStep(afterBattleStep,_r));
   }
   var ready=Progression(afterBattleStep).ActiveAbyssOperation;
   Assert.AreEqual(AbyssOperationStatus022.ReadyToFinalize,ready.Status);
   CollectionAssert.AreEquivalent(operation.steps.Select(x=>x.stepId).ToArray(),ready.CompletedStepIds);

   var terminalCommitted=Require(_service.CommitAbyssOperationCompletion(afterBattleStep,_r));
   var terminalReceipt=Progression(terminalCommitted).ActiveAbyssOperation.PendingReceipt;
   StringAssert.StartsWith("ABYSSREC022_",terminalReceipt.ReceiptId);
   Assert.AreNotEqual(battleReceiptId,terminalReceipt.ReceiptId);
   Assert.AreEqual("OPERATION_COMPLETE",terminalReceipt.Outcome);
   var finalized=Require(_service.ApplyAbyssOperationCompletion(terminalCommitted,_r));
   var repeatedFromSameTerminal=Require(_service.ApplyAbyssOperationCompletion(terminalCommitted,_r));
   var finalState=Progression(finalized);
   var floorState=finalState.AbyssFloors.Single(x=>x.FloorId==operation.floorId);
   Assert.IsNull(finalState.ActiveAbyssOperation);
   Assert.AreEqual(1,floorState.ClearCount);
   CollectionAssert.AreEquivalent(new[]{terminalReceipt.ReceiptId},floorState.AppliedReceiptIds);
   Assert.AreEqual(1,finalState.AppliedReceiptIds.Count(x=>x==battleReceiptId));
   Assert.AreEqual(1,finalState.AppliedReceiptIds.Count(x=>x==terminalReceipt.ReceiptId));
   Assert.AreEqual(7+operation.guildXp,finalized.Guild.TreasuryXp);
   Assert.AreEqual(5+operation.hallXp,finalized.Guild.Development.HallEnhancementXp);
   foreach(var materialId in operation.rewardMaterialIds)Assert.AreEqual(4,WorldMaterial(finalized,materialId));
   Assert.AreEqual(CanonicalJson.Serialize(finalized),CanonicalJson.Serialize(repeatedFromSameTerminal));
  }

  [Test]
  public void AbyssEncounterCommitIsDeterministicAndStartsThroughExistingM2Authority()
  {
   var campaign=AdvanceToAbyssBattleStep(CreateAbyssBattleReadyCampaign(),"ABYSS_OP022_01_GUARDIAN");
   var first=Require(_service.CommitAbyssBattleEncounter(campaign,_r));
   var sameInput=Require(_service.CommitAbyssBattleEncounter(campaign,_r));
   var committedRetry=Require(_service.CommitAbyssBattleEncounter(first,_r));
   var request=first.Guild.GuildCity.PendingEncounter;

   Assert.NotNull(typeof(ICampaignProgressionPresentationCoordinator022).GetMethod("EnterAbyssBattle022"));
   Assert.NotNull(request);
   StringAssert.StartsWith("ABYSS_ENCOUNTER022_",request.RequestId);
   StringAssert.StartsWith("ABYSS_BATTLE022_",request.BattleId);
   Assert.AreEqual(1,request.EnemyUnionCount);
   Assert.That(request.AlliedUnionIds.Count,Is.InRange(1,10));
    Assert.AreEqual(AbyssOperationStatus022.AwaitingBattle,Progression(first).ActiveAbyssOperation.Status);
    Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(sameInput));
    Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(committedRetry));
    Assert.IsTrue(first.Guild.Development.HasAdventureAuthority(
     GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request)));

    var mutatedRequest=CopyEncounter(request,
     objective:request.Objective+" (mutated saved request)");
    var mutatedCampaign=WithCity(first,first.Guild.GuildCity.With(
     pendingEncounter:mutatedRequest,replacePendingEncounter:true,
     lastCheckpointId:"mutated_saved_tower_request_084"));
    var mutatedStart=new GuildCityBattleBridgeService017D()
     .StartCertifiedEncounter(mutatedCampaign,_battle,_combat);
    Assert.IsFalse(mutatedStart.IsSuccess);
    CollectionAssert.Contains(mutatedStart.Errors,
     "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED");
    Assert.IsNull(mutatedCampaign.Battle);

    var freeFormBypass=_battle.StartEncounterBattle(first,_combat,
    request.BattleId,request.Objective+" (weakened direct call)",
    request.EnemyUnionCount,Array.Empty<string>(),request.AlliedUnionIds);
   Assert.IsFalse(freeFormBypass.IsSuccess);
   CollectionAssert.Contains(freeFormBypass.Errors,
    "M2_PENDING_ENCOUNTER_REQUIRES_COMMITTED_BRIDGE");
   Assert.IsNull(first.Battle);

   var started=Require(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(first,_battle,_combat));
   Assert.AreEqual(request.BattleId,started.Battle.BattleId);
   Assert.AreEqual(request.EnemyUnionCount,started.Battle.EnemyUnions.Count);
   CollectionAssert.AreEquivalent(request.AlliedUnionIds,started.Battle.PlayerUnions.Select(x=>x.UnionId).ToArray());
   var activeRetry=Require(_service.CommitAbyssBattleEncounter(started,_r));
   Assert.AreEqual(CanonicalJson.Serialize(started),CanonicalJson.Serialize(activeRetry));
  }

  [Test]
  public void AllThirtyTowerRoutesExecuteEverySavedTileAndFinalizeExactlyOnce()
  {
   var campaign=CreateAbyssBattleReadyCampaign();
   var executed=new List<string>();
   var battleRoutes=0;var nonBattleRoutes=0;var executedSteps=0;
   for(var floorNumber=1;floorNumber<=10;floorNumber++)
   {
    var floor=_r.Floors.Values.Single(value=>value.floor==floorNumber);
    var ordered=_r.AbyssOperations.Values.Where(value=>
        StringComparer.Ordinal.Equals(value.floorId,floor.floorId) &&
        value.kind != CampaignProgressionCommandService022.EndlessBattleKind094)
       .OrderBy(value=>value.kind=="GUARDIAN"?0:value.kind=="RECON"?1:2)
       .ThenBy(value=>value.operationId,StringComparer.Ordinal).ToArray();
    Assert.AreEqual(3,ordered.Length,floor.floorId);
    foreach(var operation in ordered)
    {
     var beforeTreasury=campaign.Guild.TreasuryXp;
     var beforeHall=campaign.Guild.Development.HallEnhancementXp;
     var beforeMaterials=Progression(campaign).ActiveAbyssOperation==null
         ?campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020
             .WorldMaterials.ToDictionary(value=>value.MaterialId,
                 value=>value.Amount,StringComparer.Ordinal)
         :throw new AssertionException("A prior Tower route remained active.");
     campaign=Require(_service.BeginAbyssOperation(campaign,_r,
         operation.operationId));
     var hasBattle=operation.steps.Any(value=>value.requiresBattle);
     if(hasBattle)battleRoutes++;else nonBattleRoutes++;
     for(var stepIndex=0;stepIndex<operation.steps.Length;stepIndex++)
     {
      var step=operation.steps[stepIndex];
      Assert.AreEqual(stepIndex,
          Progression(campaign).ActiveAbyssOperation.CurrentStepIndex,
          operation.operationId+":"+step.stepId);
      if(step.requiresBattle)
      {
       campaign=Require(_service.CommitAbyssBattleEncounter(campaign,_r));
       var request=campaign.Guild.GuildCity.PendingEncounter;
       Assert.NotNull(request,operation.operationId);
       campaign=WithClaimedBattle(campaign,
           "BATTLE_REWARD_ABYSS_MATRIX_084_"+operation.operationId,request);
       var returnCommitted=Require(_service.CommitAbyssBattleReturn(campaign,_r));
       var returnReloaded=JsonConvert.DeserializeObject<CampaignState>(
           JsonConvert.SerializeObject(returnCommitted));
       Assert.NotNull(returnReloaded,operation.operationId);
       campaign=Require(_service.ApplyAbyssBattleReturnExactlyOnce(
           returnReloaded,_r));
       campaign=Require(_service.CommitAbyssBattleResult(campaign,_r));
       campaign=Require(_service.ApplyAbyssBattleAndFinalize(campaign,_r));
      }
      else
      {
       var committed=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
       var reloaded=JsonConvert.DeserializeObject<CampaignState>(
           JsonConvert.SerializeObject(committed));
       Assert.NotNull(reloaded,operation.operationId);
       campaign=Require(_service.ApplyAbyssStep(reloaded,_r));
      }
      executedSteps++;
     }
     Assert.AreEqual(AbyssOperationStatus022.ReadyToFinalize,
         Progression(campaign).ActiveAbyssOperation.Status,operation.operationId);
     var terminal=Require(_service.CommitAbyssOperationCompletion(campaign,_r));
     var terminalReloaded=JsonConvert.DeserializeObject<CampaignState>(
         JsonConvert.SerializeObject(terminal));
     Assert.NotNull(terminalReloaded,operation.operationId);
     var finalized=Require(_service.ApplyAbyssOperationCompletion(
         terminalReloaded,_r));
     var deterministicReplay=Require(_service.ApplyAbyssOperationCompletion(
         terminalReloaded,_r));
     Assert.AreEqual(CanonicalJson.Serialize(finalized),
         CanonicalJson.Serialize(deterministicReplay),operation.operationId);
     Assert.IsNull(Progression(finalized).ActiveAbyssOperation,
         operation.operationId);
     Assert.AreEqual(beforeTreasury+operation.guildXp+(hasBattle?7:0),
         finalized.Guild.TreasuryXp,operation.operationId);
     Assert.AreEqual(beforeHall+operation.hallXp+(hasBattle?5:0),
         finalized.Guild.Development.HallEnhancementXp,operation.operationId);
     foreach(var materialId in operation.rewardMaterialIds)
     {
      beforeMaterials.TryGetValue(materialId,out var beforeAmount);
      Assert.AreEqual(beforeAmount+4,WorldMaterial(finalized,materialId),
          operation.operationId+":"+materialId);
     }
     var duplicate=_service.ApplyAbyssOperationCompletion(finalized,_r);
     Assert.IsFalse(duplicate.IsSuccess,operation.operationId);
     campaign=finalized;
     executed.Add(operation.operationId);
    }
   }
   Assert.AreEqual(30,executed.Count);
   Assert.AreEqual(30,executed.Distinct(StringComparer.Ordinal).Count());
   Assert.AreEqual(150,executedSteps);
   Assert.AreEqual(15,battleRoutes);
   Assert.AreEqual(15,nonBattleRoutes);
  }

  [Test]
  public void AbyssEncounterRejectsForeignBattleAndFloorTwoTrialUsesAuthoredStepFallback()
  {
   var baseCampaign=CreateAbyssBattleReadyCampaign();
   var unrelatedBattle=Require(_battle.StartTutorialBattle(baseCampaign,_combat)).Battle;
   var guardian=AdvanceToAbyssBattleStep(baseCampaign,"ABYSS_OP022_01_GUARDIAN");
   var foreignRejected=_service.CommitAbyssBattleEncounter(
    guardian.WithBattle(unrelatedBattle),_r);
   Assert.IsFalse(foreignRejected.IsSuccess);
   CollectionAssert.Contains(foreignRejected.Errors,"CAMPAIGN022_ACTIVE_BATTLE_MUST_FINISH");

   var floorOneId=_r.Floors.Values.Single(value=>value.floor==1).floorId;
   var floorOne=CompleteGuardianFirstClear084(
    CreateAbyssBattleReadyCampaign(),floorOneId);
   var trialReady=EnsureFloorClearedForOperation084(
    floorOne,"ABYSS_OP022_02_TRIAL");
   var trial=AdvanceToAbyssBattleStep(
    trialReady,"ABYSS_OP022_02_TRIAL");
   var committed=Require(_service.CommitAbyssBattleEncounter(trial,_r));
   var request=committed.Guild.GuildCity.PendingEncounter;
   var step=_r.AbyssOperations["ABYSS_OP022_02_TRIAL"].steps[Progression(trial).ActiveAbyssOperation.CurrentStepIndex];
   Assert.IsTrue(string.IsNullOrWhiteSpace(step.bossId));
   Assert.AreEqual(step.stepId,request.EncounterId);
   Assert.AreEqual(step.stepId,request.NodeId);
   CollectionAssert.Contains(request.RouteModifiers,"TRIAL");
  }

  [Test]
  public void AbyssBattleTransportRejectsTamperedEncounterBattleAndReturnReceipt()
  {
   const string rewardId="BATTLE_REWARD_ABYSS_TAMPER_022_TEST";
   var atBattle=AdvanceToAbyssBattleStep(CreateAbyssBattleReadyCampaign(),"ABYSS_OP022_01_GUARDIAN");
   var committed=Require(_service.CommitAbyssBattleEncounter(atBattle,_r));
   var request=committed.Guild.GuildCity.PendingEncounter;
   var tamperedRequest=CopyEncounter(request,requestId:request.RequestId+"_FORGED");
   var tamperedCampaign=WithCity(committed,committed.Guild.GuildCity.With(pendingEncounter:tamperedRequest,replacePendingEncounter:true,lastCheckpointId:"forged_abyss_encounter"));
   Assert.IsFalse(_service.HasActiveAbyssEncounter(tamperedCampaign,_r));
   var requestRejected=_service.CommitAbyssBattleEncounter(tamperedCampaign,_r);
   Assert.IsFalse(requestRejected.IsSuccess);
   CollectionAssert.Contains(requestRejected.Errors,"CAMPAIGN022_ABYSS_ENCOUNTER_INVALID");

   var wrongBattle=WithClaimedBattle(committed,rewardId,request,request.BattleId+"_FORGED");
   var battleRejected=_service.CommitAbyssBattleReturn(wrongBattle,_r);
   Assert.IsFalse(battleRejected.IsSuccess);
   CollectionAssert.Contains(battleRejected.Errors,"CAMPAIGN022_ABYSS_BATTLE_ID_INVALID");

    var claimed=WithClaimedBattle(committed,rewardId,request);
    var sharedBridge=new GuildCityBattleBridgeService017D();
    var genericReturn=Require(sharedBridge.CommitBattleReturn(claimed));
    var genericReceipt=genericReturn.Guild.GuildCity.PendingBattleReturn;
    var forgedGenericReceipt=CopyBattleReturn(genericReceipt,
     materialRewards:new[]{new GuildMaterialState017D("MAT_GATE_IRON",999999)});
    var forgedGeneric=WithCity(genericReturn,genericReturn.Guild.GuildCity.With(
     pendingBattleReturn:forgedGenericReceipt,replacePendingBattleReturn:true,
     lastCheckpointId:"forged_tower_side_reward_084"));
    var forgedGenericHash=CanonicalJson.Sha256Hex(forgedGeneric);
    var genericRejected=sharedBridge.ApplyBattleReturnExactlyOnce(forgedGeneric);
    Assert.IsFalse(genericRejected.IsSuccess);
    CollectionAssert.Contains(genericRejected.Errors,
     "GC017D_BATTLE_RETURN_RECEIPT_MISMATCH");
    Assert.AreEqual(forgedGenericHash,CanonicalJson.Sha256Hex(forgedGeneric));

    var premature=_service.CommitAbyssBattleResult(claimed,_r);
   Assert.IsFalse(premature.IsSuccess);
   CollectionAssert.Contains(premature.Errors,"CAMPAIGN022_APPLY_EXISTING_BATTLE_RETURN_FIRST");
   var returnCommitted=Require(_service.CommitAbyssBattleReturn(claimed,_r));
   var receipt=returnCommitted.Guild.GuildCity.PendingBattleReturn;
   var forgedReceipt=CopyBattleReturn(receipt,receipt.ReceiptId+"_FORGED");
   var forgedReturnCampaign=WithCity(returnCommitted,returnCommitted.Guild.GuildCity.With(pendingBattleReturn:forgedReceipt,replacePendingBattleReturn:true,lastCheckpointId:"forged_abyss_return"));
    var returnRejected=_service.ApplyAbyssBattleReturnExactlyOnce(forgedReturnCampaign,_r);
    Assert.IsFalse(returnRejected.IsSuccess);
    CollectionAssert.Contains(returnRejected.Errors,"CAMPAIGN022_ABYSS_BATTLE_RETURN_INVALID");
    Assert.NotNull(forgedReturnCampaign.Guild.GuildCity.PendingEncounter);
    var returned=Require(_service.ApplyAbyssBattleReturnExactlyOnce(
     returnCommitted,_r));
    var returnAuthorityId=GuildCityBattleBridgeService017D
     .BattleReturnApplyAuthorityId084(receipt);
    Assert.IsTrue(returned.Guild.Development.HasAdventureAuthority(
     returnAuthorityId));
    var unanchored=WithoutAdventureAuthority084(returned,returnAuthorityId);
    var unanchoredHash=CanonicalJson.Sha256Hex(unanchored);
    Assert.IsFalse(_service.ApplyAbyssBattleReturnExactlyOnce(
     unanchored,_r).IsSuccess);
    Assert.IsFalse(_service.CommitAbyssBattleResult(unanchored,_r).IsSuccess);
    Assert.AreEqual(unanchoredHash,CanonicalJson.Sha256Hex(unanchored));
   }

  [Test]
  public void EchoInvocationRequiresClearedFloorAndManuallyEquippedInvocationArtifact()
  {
   var noFloor=SelectEchoForecast(CreateEchoCampaign(false,true),"CMD_GUARD",out _);
   var floorRejected=_service.InvokeEligibleEchoForecast(noFloor,_r,_battle,_combat);
   Assert.IsFalse(floorRejected.IsSuccess);
   CollectionAssert.Contains(floorRejected.Errors,"CAMPAIGN022_ECHO_ABYSS_FLOOR_REQUIRED");

   var unequipped=SelectEchoForecast(CreateEchoCampaign(true,false),"CMD_GUARD",out _);
   var equipRejected=_service.InvokeEligibleEchoForecast(unequipped,_r,_battle,_combat);
   Assert.IsFalse(equipRejected.IsSuccess);
   CollectionAssert.Contains(equipRejected.Errors,"CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");
   Assert.AreEqual(0,Progression(unequipped).SummonResonance);
   Assert.AreEqual(0,Progression(unequipped).AppliedReceiptIds.Count(x=>x.StartsWith("ECHOREC022_",StringComparison.Ordinal)));
  }

  [Test]
  public void EchoInvocationUsesOnlySelectedWholeForecastAndDeterministicCategory()
  {
   var campaign=SelectEchoForecast(CreateEchoCampaign(true,true),"CMD_ALL_OUT",out _);
   var rejected=_service.InvokeEligibleEchoForecast(campaign,_r,_battle,_combat);
   Assert.IsFalse(rejected.IsSuccess);
   CollectionAssert.Contains(rejected.Errors,"CAMPAIGN022_ECHO_FORECAST_CATEGORY_REQUIRED");

   var publicCommand=typeof(CampaignProgressionCommandService022).GetMethod("InvokeEligibleEchoForecast");
   Assert.NotNull(publicCommand);
   Assert.IsFalse(publicCommand.GetParameters().Any(x=>x.ParameterType==typeof(string)),"The public command must not expose an Echo, member, or Art selection ID.");
   Assert.IsTrue(_r.Echoes.Values.All(x=>!x.directIndividualSelection));
  }

  [Test]
  public void EligibleEchoChargesForecastResourcesEmitsBattleEvidenceAndPersistsExactlyOnce()
  {
   var selected=SelectEchoForecast(CreateEchoCampaign(true,true),"CMD_GUARD",out var baseForecast);
   var unionBefore=selected.Battle.PlayerUnions.Single();
   var actorBefore=unionBefore.Members.Single();
   var echo=_r.Echoes["SUMMON_ECHO022_01"];

   var first=Require(_service.InvokeEligibleEchoForecast(selected,_r,_battle,_combat));
   var deterministicRetry=Require(_service.InvokeEligibleEchoForecast(selected,_r,_battle,_combat));
   Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(deterministicRetry));
   var state=Progression(first);
   var receipt=state.AppliedReceiptIds.Single(x=>x.StartsWith("ECHOREC022_",StringComparison.Ordinal));
   var artifact=state.InvocationArtifacts.Single(x=>x.InstanceId=="INVOCATION022_ECHO_TEST");
   var events=first.Battle.EventLog.Where(x=>x.EventType=="SUMMON_ECHO_INVOKED"&&x.ArtId==echo.echoId).ToArray();
   var unionAfter=first.Battle.PlayerUnions.Single(x=>x.UnionId==unionBefore.UnionId);
   var actorAfter=unionAfter.Members.Single(x=>x.MemberId==actorBefore.MemberId);

   Assert.AreEqual(1,events.Length);
   StringAssert.Contains(receipt,events[0].Text);
   Assert.AreEqual(echo.sharedApCost,events[0].Amount);
   Assert.AreEqual(Math.Min(unionBefore.MaximumAp,unionBefore.CurrentAp-baseForecast.SharedApCost-echo.sharedApCost+3),unionAfter.CurrentAp);
   Assert.AreEqual(actorBefore.CurrentMp-baseForecast.MemberActions.Single().PersonalMpCost-echo.personalMpCost,actorAfter.CurrentMp);
   Assert.AreEqual(1,state.SummonResonance);
   Assert.AreEqual(1,artifact.Resonance);
   Assert.AreEqual(1,state.AppliedReceiptIds.Count(x=>x==receipt));
   StringAssert.StartsWith("campaign022_echo_invoked:"+echo.echoId+":",state.LastCheckpointId);
   Assert.AreEqual(1,first.Battle.RoundRecords.Single().Selections.Count);
   Assert.AreEqual(1,first.Battle.RoundRecords.Single().Events.Count(x=>x.EventType=="SUMMON_ECHO_INVOKED"));

   var reloaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(first));
   Assert.NotNull(reloaded);
   Assert.AreEqual(1,Progression(reloaded).SummonResonance);
   Assert.AreEqual(1,Progression(reloaded).InvocationArtifacts.Single(x=>x.InstanceId==artifact.InstanceId).Resonance);
   Assert.AreEqual(1,Progression(reloaded).AppliedReceiptIds.Count(x=>x==receipt));
  }

  [Test]
  public void ForgedEchoForecastMarkerCannotEmitOrPersistEchoProof()
  {
   var selected=SelectEchoForecast(CreateEchoCampaign(true,true),"CMD_GUARD",out var forecast);
   var forged=new BattleForecastState(forecast.ForecastId,forecast.UnionId,forecast.CommandId,forecast.CommandName,forecast.Phrase,forecast.TacticalIntent,forecast.TargetId,forecast.TargetName,forecast.MemberActions,forecast.SharedApCost,forecast.ApRecovery,forecast.CombinedMpCost,forecast.ExpectedEffect,forecast.Risk,forecast.LearningOpportunity+" · ECHO_INVOCATION022=FORGED|FORGED|FORGED|FORGED|FORGED|2|4|FORGED|ECHOREC022_FORGED",forecast.FallbackBehavior,forecast.GenerationIdentity,forecast.DeterministicDebugEvidence);
   var forecasts=selected.Battle.CommittedForecasts.Select(x=>x.ForecastId==forged.ForecastId?forged:x).ToArray();
   selected=selected.WithBattle(selected.Battle.With(committedForecasts:forecasts));

   var resolved=Require(_battle.ConfirmRound(selected,_combat));
   Assert.IsFalse(resolved.Battle.EventLog.Any(x=>x.EventType=="SUMMON_ECHO_INVOKED"));
   Assert.AreEqual(0,Progression(resolved).AppliedReceiptIds.Count(x=>x.StartsWith("ECHOREC022_",StringComparison.Ordinal)));
   Assert.AreEqual(0,Progression(resolved).SummonResonance);
  }

  [Test]
  public void CovenantTrialAndAcceptanceRejectPrematureOrNoncanonicalStoryAndFloorState()
  {
   const string covenantId="COVENANT022_AEGIS_FIRST_WALL";
   var opening=CampaignFactory.CreateM0Proof(22022);
   var storyRejected=_service.AdvanceCovenantTrial(opening,_r,covenantId);
   Assert.IsFalse(storyRejected.IsSuccess);
   CollectionAssert.Contains(storyRejected.Errors,"CAMPAIGN022_COVENANT_STORY_GATE_REQUIRED");
   var acceptStoryRejected=_service.AcceptCovenant(opening,_r,covenantId);
   Assert.IsFalse(acceptStoryRejected.IsSuccess);
   CollectionAssert.Contains(acceptStoryRejected.Errors,"CAMPAIGN022_COVENANT_STORY_GATE_REQUIRED");

   var forgedGate=WithStoryGate(opening,CampaignProgressionCommandService022.GreatCovenantsStoryGate);
   var floorRejected=_service.AdvanceCovenantTrial(forgedGate,_r,covenantId);
   Assert.IsFalse(floorRejected.IsSuccess);
   CollectionAssert.Contains(floorRejected.Errors,"CAMPAIGN022_COVENANT_ABYSS_RECOGNITION_REQUIRED");

   var forgedFloor=new AbyssFloorProgressState022("ABYSS_FLOOR_010_AEGIS_GATE",1,1,true,false,Array.Empty<string>());
   var forgedAuthority=WithProgression(forgedGate,Progression(forgedGate).With(abyssFloors:new[]{forgedFloor},lastCheckpointId:"forged_covenant_gate"));
   Assert.IsFalse(_service.HasEarnedGreatCovenantGate(forgedAuthority,_r));
   var authorityRejected=_service.AdvanceCovenantTrial(forgedAuthority,_r,covenantId);
   Assert.IsFalse(authorityRejected.IsSuccess);
   CollectionAssert.Contains(authorityRejected.Errors,"CAMPAIGN022_COVENANT_STORY_GATE_AUTHORITY_REQUIRED");
  }

  [Test]
  public void CanonicalFirstClearOfAegisGateGrantsGreatCovenantStoryGateExactlyOnce()
  {
   var terminal=PrepareFloorTenTerminalReceipt();
   var receipt=Progression(terminal).ActiveAbyssOperation.PendingReceipt;
   Assert.NotNull(receipt);
   Assert.IsFalse(terminal.Guild.GuildCity.Strategic017H.StoryGates.Contains(CampaignProgressionCommandService022.GreatCovenantsStoryGate));
   Assert.IsFalse(terminal.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds.Contains(receipt.ReceiptId));

   var first=Require(_service.ApplyAbyssOperationCompletion(terminal,_r));
   var deterministicRepeat=Require(_service.ApplyAbyssOperationCompletion(terminal,_r));
   var strategic=first.Guild.GuildCity.Strategic017H;
   var floorState=Progression(first).AbyssFloors.Single(x=>x.FloorId=="ABYSS_FLOOR_010_AEGIS_GATE");var proof=floorState.FirstClearProof;
   Assert.NotNull(proof);Assert.AreEqual(receipt.ReceiptId,proof.CompletionReceipt.ReceiptId);Assert.AreEqual(_r.AbyssOperations[proof.OperationDefinitionId].steps.Length,proof.StepReceipts.Count);CollectionAssert.AreEqual(proof.CompletedStepIds,proof.StepReceipts.Select(x=>x.StepId).ToArray());
   Assert.IsTrue(_service.HasEarnedGreatCovenantGate(first,_r));
   Assert.AreEqual(1,strategic.StoryGates.Count(x=>x==CampaignProgressionCommandService022.GreatCovenantsStoryGate));
   Assert.AreEqual(1,strategic.AppliedStrategicReceiptIds.Count(x=>x==receipt.ReceiptId));
   Assert.AreEqual(1,Progression(first).AppliedReceiptIds.Count(x=>x==receipt.ReceiptId));
   Assert.AreEqual(1,Progression(first).AbyssFloors.Single(x=>x.FloorId=="ABYSS_FLOOR_010_AEGIS_GATE").AppliedReceiptIds.Count(x=>x==receipt.ReceiptId));
   Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(deterministicRepeat));
   var reloaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(first));Assert.NotNull(reloaded);Assert.IsTrue(_service.HasEarnedGreatCovenantGate(reloaded,_r));

   var secondTerminal=PrepareNonBattleTerminalReceipt(first,"ABYSS_OP022_10_RECON");var secondReceipt=Progression(secondTerminal).ActiveAbyssOperation.PendingReceipt;var secondClear=Require(_service.ApplyAbyssOperationCompletion(secondTerminal,_r));
   Assert.AreNotEqual(receipt.ReceiptId,secondReceipt.ReceiptId);
   Assert.AreEqual(1,secondClear.Guild.GuildCity.Strategic017H.StoryGates.Count(x=>x==CampaignProgressionCommandService022.GreatCovenantsStoryGate));
   Assert.AreEqual(1,secondClear.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds.Count(x=>x==receipt.ReceiptId));
   Assert.IsFalse(secondClear.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds.Contains(secondReceipt.ReceiptId));
  }

  [Test]
  public void GreatCovenantGateRejectsPrefixOnlyMissingLedgerAndTamperedDurableProof()
  {
   var earned=EarnGreatCovenantGate();var state=Progression(earned);var floor=state.AbyssFloors.Single(x=>x.FloorId=="ABYSS_FLOOR_010_AEGIS_GATE");var proof=floor.FirstClearProof;var receiptId=proof.CompletionReceipt.ReceiptId;
   var tamperedProof=new AbyssFirstClearProof022(proof.CampaignGuid,proof.CampaignSeed,proof.AppliedReceiptCountAtBegin,proof.OperationInstanceId,proof.OperationDefinitionId,proof.FloorId,proof.CanonicalSeedIdentity+"_TAMPERED",proof.CompletedStepIds,proof.StepReceipts,proof.ExistingBattleRewardReceiptId,proof.CompletionReceipt);
   var tamperedFloor=floor.With(firstClearProof:tamperedProof);var tamperedFloors=state.AbyssFloors.Select(x=>x.FloorId==floor.FloorId?tamperedFloor:x).ToArray();var tampered=WithProgression(earned,state.With(abyssFloors:tamperedFloors,lastCheckpointId:"tampered_gate_proof"));
   Assert.IsFalse(_service.HasEarnedGreatCovenantGate(tampered,_r));

   var city=earned.Guild.GuildCity;var strategic=city.Strategic017H;var missingStrategic=strategic.With(appliedStrategicReceiptIds:strategic.AppliedStrategicReceiptIds.Where(x=>x!=receiptId).ToArray(),lastCheckpointId:"missing_gate_ledger");var missingLedger=WithCity(earned,city.With(strategic017H:missingStrategic,replaceStrategic017H:true,lastCheckpointId:"missing_gate_ledger"));
   Assert.IsFalse(_service.HasEarnedGreatCovenantGate(missingLedger,_r));

   var forgedFloor=new AbyssFloorProgressState022("ABYSS_FLOOR_010_AEGIS_GATE",1,1,true,false,new[]{"ABYSSREC022_PREFIX_ONLY_FORGED"});var prefixOnly=WithProgression(WithStoryGate(CampaignFactory.CreateM0Proof(22100),CampaignProgressionCommandService022.GreatCovenantsStoryGate),CampaignProgressionState022.Default().With(abyssFloors:new[]{forgedFloor},aboveGroundChangeIds:new[]{"CITY_CHANGE_AEGIS_COVENANT_DESK"},appliedReceiptIds:new[]{"ABYSSREC022_PREFIX_ONLY_FORGED"},lastCheckpointId:"prefix_only_forgery"));
   Assert.IsFalse(_service.HasEarnedGreatCovenantGate(prefixOnly,_r));
  }

  [Test]
  public void FourDistinctCovenantTrialStageReceiptsReachOneHundredWithoutImplicitAcceptance()
  {
   const string covenantId="COVENANT022_AEGIS_FIRST_WALL";
   var campaign=EarnGreatCovenantGate();
   for(var stage=1;stage<=4;stage++)
   {
    var before=campaign;
    campaign=Require(_service.AdvanceCovenantTrial(before,_r,covenantId));
    var retry=Require(_service.AdvanceCovenantTrial(before,_r,covenantId));
    Assert.AreEqual(CanonicalJson.Serialize(campaign),CanonicalJson.Serialize(retry));
    var current=Progression(campaign).Covenants.Single(x=>x.CovenantId==covenantId);
    Assert.AreEqual(stage*25,current.TrialProgress);
    Assert.AreEqual(CovenantStatus022.TrialActive,current.Status);
    Assert.AreEqual(stage,current.AppliedReceiptIds.Count(x=>x.StartsWith("COVTRIALREC022_",StringComparison.Ordinal)));
    Assert.AreEqual(stage,Progression(campaign).AppliedReceiptIds.Count(x=>x.StartsWith("COVTRIALREC022_",StringComparison.Ordinal)));
   }
   var ready=Progression(campaign).Covenants.Single(x=>x.CovenantId==covenantId);
   Assert.AreEqual(100,ready.TrialProgress);
   Assert.AreEqual(24,ready.Trust);
   Assert.AreNotEqual(CovenantStatus022.Accepted,ready.Status);
   var trialReceipts=ready.AppliedReceiptIds.Where(x=>x.StartsWith("COVTRIALREC022_",StringComparison.Ordinal)).ToArray();
   Assert.AreEqual(trialReceipts.Length,trialReceipts.Distinct().Count());
   var capped=Require(_service.AdvanceCovenantTrial(campaign,_r,covenantId));
   Assert.AreEqual(CanonicalJson.Serialize(campaign),CanonicalJson.Serialize(capped));
  }

  [Test]
  public void ExplicitVoluntaryCovenantAcceptancePersistsExactlyOnce()
  {
   const string covenantId="COVENANT022_AEGIS_FIRST_WALL";
   Assert.NotNull(typeof(ICampaignProgressionPresentationCoordinator022).GetMethod("AcceptCovenant022"));
   var ready=ReadyCovenantTrial(covenantId);
   var first=Require(_service.AcceptCovenant(ready,_r,covenantId));
   var sameInputRetry=Require(_service.AcceptCovenant(ready,_r,covenantId));
   var appliedRetry=Require(_service.AcceptCovenant(first,_r,covenantId));
   var current=Progression(first).Covenants.Single(x=>x.CovenantId==covenantId);
   var receipt=current.AppliedReceiptIds.Single(x=>x.StartsWith("COVACCEPTREC022_",StringComparison.Ordinal));
   Assert.AreEqual(CovenantStatus022.Accepted,current.Status);
   Assert.AreEqual(100,current.TrialProgress);
   Assert.AreEqual(1,current.AppliedReceiptIds.Count(x=>x==receipt));
   Assert.AreEqual(1,Progression(first).AppliedReceiptIds.Count(x=>x==receipt));
   Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(sameInputRetry));
   Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(appliedRetry));
   var reloaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(first));
   Assert.NotNull(reloaded);
   Assert.AreEqual(CovenantStatus022.Accepted,Progression(reloaded).Covenants.Single(x=>x.CovenantId==covenantId).Status);
   Assert.AreEqual(1,Progression(reloaded).AppliedReceiptIds.Count(x=>x==receipt));
  }

  [Test]
  public void CovenantAcceptanceRejectsPreReadyTamperedNonVoluntaryAndRefusedState()
  {
   const string covenantId="COVENANT022_AEGIS_FIRST_WALL";
   var eligible=EarnGreatCovenantGate();var preReady=eligible;
   for(var i=0;i<3;i++)preReady=Require(_service.AdvanceCovenantTrial(preReady,_r,covenantId));
   var premature=_service.AcceptCovenant(preReady,_r,covenantId);
   Assert.IsFalse(premature.IsSuccess);
   CollectionAssert.Contains(premature.Errors,"CAMPAIGN022_COVENANT_TRIAL_NOT_READY");

   var tamperedProgress=new CovenantProgressState022(covenantId,CovenantStatus022.TrialActive,100,24,Array.Empty<string>());
   var tampered=WithProgression(eligible,Progression(eligible).With(covenants:new[]{tamperedProgress},lastCheckpointId:"forged_covenant_trial"));
   var tamperedRejected=_service.AcceptCovenant(tampered,_r,covenantId);
   Assert.IsFalse(tamperedRejected.IsSuccess);
   CollectionAssert.Contains(tamperedRejected.Errors,"CAMPAIGN022_COVENANT_TRIAL_STATE_INVALID");

   var canonicalReady=ReadyCovenantTrial(covenantId);var canonicalState=Progression(canonicalReady);var canonicalCovenant=canonicalState.Covenants.Single(x=>x.CovenantId==covenantId);var duplicate=WithProgression(canonicalReady,canonicalState.With(covenants:new[]{canonicalCovenant,canonicalCovenant},lastCheckpointId:"duplicate_covenant_authority"));
   Assert.IsFalse(_service.AcceptCovenant(duplicate,_r,covenantId).IsSuccess);
   var missingTrialReceipt=canonicalCovenant.AppliedReceiptIds.First(x=>x.StartsWith("COVTRIALREC022_",StringComparison.Ordinal));var missingGlobal=WithProgression(canonicalReady,canonicalState.With(appliedReceiptIds:canonicalState.AppliedReceiptIds.Where(x=>x!=missingTrialReceipt).ToArray(),lastCheckpointId:"missing_global_trial_receipt"));var missingGlobalRejected=_service.AcceptCovenant(missingGlobal,_r,covenantId);
   Assert.IsFalse(missingGlobalRejected.IsSuccess);CollectionAssert.Contains(missingGlobalRejected.Errors,"CAMPAIGN022_COVENANT_TRIAL_STATE_INVALID");

   var ready=ReadyCovenantTrial(covenantId);var definition=_r.Covenants[covenantId];definition.voluntaryAcceptanceRequired=false;
   try
   {
    var lawRejected=_service.AcceptCovenant(ready,_r,covenantId);
    Assert.IsFalse(lawRejected.IsSuccess);
    CollectionAssert.Contains(lawRejected.Errors,"CAMPAIGN022_COVENANT_AUTHORITY_LAW_INVALID");
   }
   finally{definition.voluntaryAcceptanceRequired=true;}

   var readyState=Progression(ready);var refusedCurrent=readyState.Covenants.Single(x=>x.CovenantId==covenantId).With(status:CovenantStatus022.Refused);var refused=WithProgression(ready,readyState.With(covenants:new[]{refusedCurrent},lastCheckpointId:"covenant_refused"));
   var refusalPreserved=_service.AcceptCovenant(refused,_r,covenantId);
   Assert.IsFalse(refusalPreserved.IsSuccess);
   CollectionAssert.Contains(refusalPreserved.Errors,"CAMPAIGN022_COVENANT_REFUSAL_PRESERVED");
  }

  CampaignState PrepareFloorTenTerminalReceipt()
  {
   var campaign=CreateAbyssBattleReadyCampaign();
   for(var floor=1;floor<=9;floor++)
   {
    var floorId=_r.Floors.Values.Single(value=>value.floor==floor).floorId;
    campaign=CompleteGuardianFirstClear084(campaign,floorId);
   }
   var floorTenId=_r.Floors.Values.Single(value=>value.floor==10).floorId;
   var guardian=_r.AbyssOperations.Values.Single(value=>
    value.firstClearOnly&&value.floorId==floorTenId);
   campaign=AdvanceAbyssOperationToReady084(campaign,guardian.operationId);
   var terminal=Require(_service.CommitAbyssOperationCompletion(campaign,_r));
   Assert.IsFalse(terminal.Guild.GuildCity.Strategic017H.StoryGates
    .Contains(CampaignProgressionCommandService022.GreatCovenantsStoryGate));
   return terminal;
  }

  CampaignState PrepareNonBattleTerminalReceipt(CampaignState campaign,string operationId)
  {
   campaign=EnsureFloorClearedForOperation084(campaign,operationId);
   campaign=Require(_service.BeginAbyssOperation(campaign,_r,operationId));var operation=_r.AbyssOperations[operationId];Assert.IsFalse(operation.steps.Any(x=>x.requiresBattle));for(var i=0;i<operation.steps.Length;i++){campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));campaign=Require(_service.ApplyAbyssStep(campaign,_r));}return Require(_service.CommitAbyssOperationCompletion(campaign,_r));
  }

  CampaignState EarnGreatCovenantGate()=>Require(_service.ApplyAbyssOperationCompletion(PrepareFloorTenTerminalReceipt(),_r));

  CampaignState ReadyCovenantTrial(string covenantId)
  {
   var campaign=EarnGreatCovenantGate();
   for(var i=0;i<4;i++)campaign=Require(_service.AdvanceCovenantTrial(campaign,_r,covenantId));
   return campaign;
  }

  CampaignState CompleteNonBattleOperation(string operationId)
  {
   var campaign=EnsureFloorClearedForOperation084(
    CreateAbyssBattleReadyCampaign(),operationId);
   campaign=Require(_service.BeginAbyssOperation(campaign,_r,operationId));
   var operation=_r.AbyssOperations[operationId];
   Assert.IsFalse(operation.steps.Any(x=>x.requiresBattle));
   for(var i=0;i<operation.steps.Length;i++)
   {
    campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
    campaign=Require(_service.ApplyAbyssStep(campaign,_r));
   }
   Assert.AreEqual(AbyssOperationStatus022.ReadyToFinalize,Progression(campaign).ActiveAbyssOperation.Status);
   return campaign;
  }

  CampaignState EnsureFloorClearedForOperation084(
   CampaignState campaign,string operationId)
  {
   var operation=_r.AbyssOperations[operationId];
   var state=campaign.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020
    ?.Progression022??CampaignProgressionState022.Default();
   if(operation.firstClearOnly||state.AbyssFloors.Any(value=>
      value.FloorId==operation.floorId&&value.ClearCount>0&&
      value.FirstClearApplied))return campaign;
   return CompleteGuardianFirstClear084(campaign,operation.floorId);
  }

  CampaignState CompleteGuardianFirstClear084(
   CampaignState campaign,string floorId)
  {
   var guardian=_r.AbyssOperations.Values.Single(value=>
    value.firstClearOnly&&value.floorId==floorId);
   campaign=AdvanceAbyssOperationToReady084(campaign,guardian.operationId);
   campaign=Require(_service.CommitAbyssOperationCompletion(campaign,_r));
   return Require(_service.ApplyAbyssOperationCompletion(campaign,_r));
  }

  CampaignState AdvanceAbyssOperationToReady084(
   CampaignState campaign,string operationId)
  {
   campaign=Require(_service.BeginAbyssOperation(campaign,_r,operationId));
   var operation=_r.AbyssOperations[operationId];
   while(Progression(campaign).ActiveAbyssOperation.CurrentStepIndex<
         operation.steps.Length)
   {
    var step=operation.steps[
     Progression(campaign).ActiveAbyssOperation.CurrentStepIndex];
    if(step.requiresBattle)
    {
     campaign=Require(_service.CommitAbyssBattleEncounter(campaign,_r));
     var encounter=campaign.Guild.GuildCity.PendingEncounter;
     var rewardId="BATTLE_REWARD_PREREQUISITE_022_"+
      operationId+"_"+campaign.Guild.GuildCity.OperationOrdinal;
     campaign=WithClaimedBattle(campaign,rewardId,encounter);
     campaign=Require(_service.CommitAbyssBattleReturn(campaign,_r));
     campaign=Require(_service.ApplyAbyssBattleReturnExactlyOnce(campaign,_r));
     campaign=Require(_service.CommitAbyssBattleResult(campaign,_r));
     campaign=Require(_service.ApplyAbyssBattleAndFinalize(campaign,_r));
    }
    else
    {
     campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
     campaign=Require(_service.ApplyAbyssStep(campaign,_r));
    }
   }
   Assert.AreEqual(AbyssOperationStatus022.ReadyToFinalize,
    Progression(campaign).ActiveAbyssOperation.Status);
   return campaign;
  }

  CampaignState AdvanceToAbyssBattleStep(CampaignState campaign,string operationId)
  {
   campaign=Require(_service.BeginAbyssOperation(campaign,_r,operationId));var operation=_r.AbyssOperations[operationId];
   while(Progression(campaign).ActiveAbyssOperation.CurrentStepIndex<operation.steps.Length&&!operation.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle)
   {
    campaign=Require(_service.CommitAbyssStep(campaign,_r,"SUCCESS"));
    campaign=Require(_service.ApplyAbyssStep(campaign,_r));
   }
   Assert.Less(Progression(campaign).ActiveAbyssOperation.CurrentStepIndex,operation.steps.Length);
   Assert.IsTrue(operation.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle);
   return campaign;
  }

  CampaignState CreateAbyssBattleReadyCampaign()
  {
   const string recruitId="RECRUIT_ABYSS_022_TEST";const string unionId="UNION_ABYSS_022_TEST";
   var weapon=new EquipmentItemState("ABYSS_WEAPON_022_TEST","ABYSS_SWORD_022_TEST","Abyss Test Sword",new[]{EquipmentSlotIds.MainHand},new[]{"SWORD","WEAPON"},"QUALITY_STANDARD",10000,false);
   var recruit=new RecruitState(recruitId,150,150,40,40,"Abyss Tester",RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME","CLASS_TEND_GUARDIAN","Observed",7000,RecruitAuthorityKind.Normal,string.Empty,string.Empty,new EquipmentLoadoutState(new[]{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,weapon)}),true,string.Empty,string.Empty,60,60);
   var union=new UnionState(unionId,"Abyss Test Union",UnionKind.Normal,recruitId,new[]{recruitId},"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",18,8500);
   var guild=new GuildState("GUILD_ABYSS_022_TEST",0,new[]{recruit},new[]{union},Array.Empty<EquipmentItemState>());
   var profile=new NewGuildProfileState("Abyss Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);
   var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,false,"abyss_battle_test_ready");
   var campaign=new CampaignState("00000000-0000-0000-0000-000000000223",22023,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);
   return WithProgression(campaign,CampaignProgressionState022.Default());
  }

  CampaignState CreateWeaponEvolutionCampaign(
   string itemId,string trackId,string tierId,IReadOnlyList<string> appliedRecipeIds)
  {
   var campaign=CampaignFactory.CreateM0Proof(22024);
   var evolution=new EquipmentEvolutionState022(
    itemId,trackId,tierId,5,120,appliedRecipeIds,"EQUSE022_WEAPON_EVOLUTION_TEST");
   campaign=WithProgression(campaign,CampaignProgressionState022.Default().With(
    equipmentEvolution:new[]{evolution},lastCheckpointId:"weapon_evolution_test_ready"));
   return WithWorldMaterials(campaign,new[]
   {
    new WorldMaterialAmount020("MAT020_SKYHOME_02",4),
    new WorldMaterialAmount020("MAT020_BEAST_02",2)
   });
  }

  CampaignState SelectEchoForecast(CampaignState campaign,string commandId,out BattleForecastState forecast)
  {
   campaign=Require(_battle.StartTutorialBattle(campaign,_combat));
   var union=campaign.Battle.PlayerUnions.Single();
   forecast=campaign.Battle.CommittedForecasts.Single(x=>x.UnionId==union.UnionId&&x.CommandId==commandId);
   return Require(_battle.SelectForecast(campaign,union.UnionId,forecast.ForecastId));
  }

  CampaignState CreateEchoCampaign(bool clearedFloor,bool equipArtifact)
  {
   const string recruitId="RECRUIT_ECHO_022_TEST";const string unionId="UNION_ECHO_022_TEST";const string artifactId="INVOCATION022_ECHO_TEST";const string baseId="INVOCATION_BASE022_02_01";
   var weapon=new EquipmentItemState("ECHO_WEAPON_022_TEST","ECHO_SHIELD_022_TEST","Echo Test Shield",new[]{EquipmentSlotIds.MainHand},new[]{"SHIELD","SWORD","WEAPON"},"QUALITY_STANDARD",10000,false);
   var tool=new EquipmentItemState(artifactId,baseId,"Great Weapons Echo Relic",new[]{EquipmentSlotIds.ToolRelic},new[]{"INVOCATION_ARTIFACT","WEAPON_FAMILY_GREAT_WEAPON"},"UNCOMMON",10000,false);
   var assignments=new List<EquipmentSlotAssignmentState>{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,weapon)};
   if(equipArtifact)assignments.Add(new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic,tool));
   var recruit=new RecruitState(recruitId,150,150,40,40,"Echo Tester",RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME","CLASS_TEND_GUARDIAN","Observed",7000,RecruitAuthorityKind.Normal,string.Empty,string.Empty,new EquipmentLoadoutState(assignments.AsReadOnly()),true,string.Empty,string.Empty,60,60);
   var union=new UnionState(unionId,"Echo Test Union",UnionKind.Normal,recruitId,new[]{recruitId},"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",18,8500);
   var inventory=equipArtifact?Array.Empty<EquipmentItemState>():new[]{tool};
   var guild=new GuildState("GUILD_ECHO_022_TEST",0,new[]{recruit},new[]{union},inventory);
   var profile=new NewGuildProfileState("Echo Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);
   var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,false,"echo_test_ready");
   var campaign=new CampaignState("00000000-0000-0000-0000-000000000222",22022,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);
   var floors=clearedFloor?new[]{new AbyssFloorProgressState022("ABYSS_FLOOR_001_MUD_TRENCHES",1,1,true,false,Array.Empty<string>())}:Array.Empty<AbyssFloorProgressState022>();
   var artifact=new InvocationArtifactState022(artifactId,baseId,Array.Empty<string>(),"INVOCATION_PATH022_02",0,0,false);
   return WithProgression(campaign,CampaignProgressionState022.Default().With(abyssFloors:floors,invocationArtifacts:new[]{artifact},lastCheckpointId:"echo_test_ready"));
  }

  static CampaignProgressionState022 Progression(CampaignState campaign)=>campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
  static int WorldMaterial(CampaignState campaign,string materialId)=>campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldMaterials.Single(x=>x.MaterialId==materialId).Amount;
  static CampaignState WithCity(CampaignState campaign,GuildCityState017D city)=>campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
  static CampaignState WithoutAdventureAuthority084(
   CampaignState campaign,string authorityId)
  {
   var source=campaign.Guild.Development;
   var development=new GuildDevelopmentState(source.HallStageIndex,
    source.HallStageId,source.HallEnhancementXp,
    source.LifetimeTreasuryXpEarned,source.Facilities,
    source.ClaimedBattleRewardIds,source.AppliedAdventureAuthorityIds
     .Where(value=>!StringComparer.Ordinal.Equals(value,authorityId)).ToArray());
   return campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
    campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,
    development),campaign.OpeningFlow);
  }
  static CampaignState WithStoryGate(CampaignState campaign,string storyGate)
  {
   var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var gates=new List<string>(strategic.StoryGates);if(!gates.Contains(storyGate))gates.Add(storyGate);strategic=strategic.With(storyGates:gates.AsReadOnly(),lastCheckpointId:"test_story_gate");return WithCity(campaign,city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:"test_story_gate"));
  }
  static EncounterLaunchRequest017D CopyEncounter(EncounterLaunchRequest017D value,
   string requestId=null,string objective=null)
  {
   return new EncounterLaunchRequest017D(requestId??value.RequestId,
    value.ContractId,value.ExpeditionId,value.BoardId,value.NodeId,
    value.EncounterId,value.BattleId,objective??value.Objective,
    value.EnemyUnionCount,value.CanonicalSeedIdentity,value.AlliedUnionIds,
    value.ReserveUnionIds,value.ObjectiveIds,value.RouteModifiers,value.Supplies,
    value.Fatigue,value.Urgency,value.ReturnCheckpointId,value.PreBattleStateHash);
  }
  static BattleReturnReceipt017D CopyBattleReturn(BattleReturnReceipt017D value,
   string receiptId=null,IReadOnlyList<GuildMaterialState017D> materialRewards=null)
  {
   return new BattleReturnReceipt017D(receiptId??value.ReceiptId,
    value.LaunchRequestId,value.BattleRunId,value.Outcome,value.BattleResultHash,
    value.SupplyConsumption,value.FatigueDelta,value.UrgencyDelta,
    value.ObjectiveFlags,materialRewards??value.MaterialRewards,
    value.EquipmentRewardReceiptId,value.RelationshipMemories,
    value.CityProjectContribution,value.ReturnCheckpointId,value.Applied);
  }
  static CampaignState WithClaimedBattle(CampaignState campaign,string rewardId,EncounterLaunchRequest017D request,string battleIdOverride=null)
  {
   var memberReward=new BattleMemberRewardState("GUARDIAN_MEMBER_022_TEST","Guardian Tester",1,1,1,0,0,0,0,0,0,0);
   var reward=new BattleRewardState(rewardId,"ABYSS_TEST_REWARD_RULES",BattleOutcome.Victory,1,1,1000,1000,100,100,7,5,new[]{memberReward},true);
   var battle=new BattleState(battleIdOverride??request.BattleId,"ABYSS_TEST",1,BattlePhase.Resolved,BattleOutcome.Victory,request.Objective,Array.Empty<BattleUnionState>(),Array.Empty<BattleUnionState>(),Array.Empty<BattleForecastState>(),Array.Empty<BattleForecastSelectionState>(),Array.Empty<BattleEventState>(),Array.Empty<BattleRoundRecordState>(),"ABYSS_FORECAST_HASH_022_TEST","ABYSS_INITIAL_HASH_022_TEST",string.Empty,string.Empty,string.Empty,false,reward);
   battle=battle.With(finalStateHash:M2BattleCommandService.AuthoritativeStateHash(battle));
   var development=campaign.Guild.Development.RecordBattleReward(rewardId,reward.GuildTreasuryXpAward,reward.HallEnhancementXpAward);
   var guild=campaign.Guild.With(campaign.Guild.TreasuryXp+reward.GuildTreasuryXpAward,campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development);
   return campaign.With(guild,campaign.OpeningFlow).WithBattle(battle);
  }
  static CampaignState WithProgression(CampaignState campaign,CampaignProgressionState022 state)
  {
   var city=campaign.Guild.GuildCity;
   var strategic=city.Strategic017H;
   var progress=strategic.Campaign019;
   var playable=progress.Playable020.With(progression022:state,replaceProgression022:true,lastCheckpointId:state.LastCheckpointId);
   progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:state.LastCheckpointId);
   strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:state.LastCheckpointId);
   city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:state.LastCheckpointId);
   return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
  }
  static CampaignState WithWorldMaterials(CampaignState campaign,IReadOnlyList<WorldMaterialAmount020> materials)
  {
   var city=campaign.Guild.GuildCity;
   var strategic=city.Strategic017H;
   var progress=strategic.Campaign019;
   var playable=progress.Playable020.With(worldMaterials:materials,lastCheckpointId:"weapon_evolution_materials_ready");
   progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:playable.LastCheckpointId);
   strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:progress.LastCheckpointId);
   city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:strategic.LastCheckpointId);
   return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
  }
  static CampaignState Require(Result<CampaignState> result){Assert.IsTrue(result.IsSuccess,string.Join("\n",result.Errors));return result.Value;}
 }
}
#endif
