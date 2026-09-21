using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.BoardTower001;
using SecondDimension.Presentation.Campaign022;

namespace SecondDimension.Presentation
{
 public sealed partial class M1RuntimeCoordinator :
  ICampaignProgressionPresentationCoordinator022,
  ITowerAutoTransitionCoordinator108,
  ITowerVictoryBankCoordinator110
 {
  CampaignRegistry022 _campaignRegistry022; readonly CampaignProgressionCommandService022 _campaignCommands022=new CampaignProgressionCommandService022(); readonly CovenantBattleInvocationService022 _covenantBattle022=new CovenantBattleInvocationService022();
  CampaignRegistry022 Registry022(){if(_campaignRegistry022==null)_campaignRegistry022=CampaignRegistry022.LoadFromResources();return _campaignRegistry022;}
  public CampaignProgressionPresentationState022 CampaignProgression022{get{try{return BuildProgression022();}catch(Exception e){return new CampaignProgressionPresentationState022{IsAvailable=false,Error=e.Message};}}}
  public static void ApplyOptionalTowerRoomModule001(
   CampaignProgressionPresentationState022 coreState,
   string committedRunId,
   int floorNumber,
   Func<BoardTowerEnhancementCatalog001> catalogLoader=null)
   =>ApplyOptionalTowerRoomModule001(
    coreState,committedRunId,floorNumber,Array.Empty<string>(),catalogLoader);

  public static void ApplyOptionalTowerRoomModule001(
   CampaignProgressionPresentationState022 coreState,
   string committedRunId,
   int floorNumber,
   IReadOnlyList<string> completedStepIds,
   Func<BoardTowerEnhancementCatalog001> catalogLoader=null)
  {
   if(coreState==null)return;
   coreState.TowerRoomModuleId001=string.Empty;
   coreState.TowerRoomModuleDisplayName001=string.Empty;
   coreState.TowerRoomModuleNarrative001=string.Empty;
   coreState.TowerRoomModuleRevealedStepNumber001=0;
   if(string.IsNullOrWhiteSpace(committedRunId)||
      completedStepIds==null||completedStepIds.Count==0)return;
   try
   {
    var catalog=(catalogLoader??BoardTowerEnhancementCatalog001.LoadFromResources)();
    var room=TowerRevealedRoomProjection001.Project(
     catalog,committedRunId,floorNumber,completedStepIds);
    coreState.TowerRoomModuleId001=room?.RoomId??string.Empty;
    coreState.TowerRoomModuleDisplayName001=room?.Title??string.Empty;
    coreState.TowerRoomModuleNarrative001=room?.Flavor??string.Empty;
    coreState.TowerRoomModuleRevealedStepNumber001=room?.StepNumber??0;
   }
   catch(Exception)
   {
    // This pack is optional decoration until its numeric adapters are complete.
    // Invalid optional data must never make the authoritative Tower unavailable.
   }
  }
  public M1CommandResult RecordEquipmentUse022(string itemInstanceId,string trackId)=>ApplyAndPersist(_campaignCommands022.RecordEquipmentMeaningfulUse(_campaign,Registry022(),itemInstanceId,trackId,25,"Meaningful operation use"),true,"Equipment history and mastery recorded.");
  public M1CommandResult EvolveWeapon022(string itemInstanceId,string recipeId)=>ApplyAndPersist(_campaignCommands022.EvolveWeapon(_campaign,Registry022(),itemInstanceId,recipeId),true,"Equipment evolution applied without replacing the owned instance.");
  public M1CommandResult CertifyAdvancedClass022(string recruitId,string classId){var p=Registry022().Classes[classId];return ApplyAndPersist(_campaignCommands022.CertifyAdvancedClass(_campaign,Registry022(),recruitId,classId,p.primaryProficiencyRequired,p.secondaryProficiencyRequired,p.primaryUnionRankRequired,p.secondaryUnionRankRequired,p.meaningfulPrimaryActionsRequired,p.meaningfulSecondaryActionsRequired),true,"Advanced class certification recorded.");}
  public M1CommandResult BeginAbyssOperation022(string operationId)=>ApplyAndPersist(_campaignCommands022.BeginAbyssOperation(_campaign,Registry022(),operationId),true,"Endless Abyss operation committed.");
  public M1CommandResult BeginTowerRun081()
  {
   var registry=Registry022();
   var progression=ProgressionState081(_campaign);
   if(progression.ActiveAbyssOperation!=null)return M1CommandResult.Failure("Finish or leave the current Tower run first.");
   var begun=_campaignCommands022.BeginTowerFloor094(_campaign,registry);
   if(!begun.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(begun.Errors));
   return ApplyAndPersist(begun,true,
    "Tower floor committed. Preparing the certified Union battle.");
  }

  public M1CommandResult AdvanceTowerRun081()
  {
   using var timing110=BeginTowerTiming110("manual-tower-step");
   var registry=Registry022();
   var candidate=_campaign;
   var active=ProgressionState081(candidate).ActiveAbyssOperation;
   if(active==null)return M1CommandResult.Failure("No Tower run is active.");
   if(active.Status==AbyssOperationStatus022.AwaitingBattle)
   {
    if(active.PendingReceipt==null)
    {
     var committed=_campaignCommands022.CommitAbyssBattleResult(candidate,registry);
     if(!committed.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(committed.Errors));
     return ApplyAndPersist(committed,true,
      "The Tower battle result is committed and saved.");
    }
    var applied=_campaignCommands022.ApplyAbyssBattleAndFinalize(candidate,registry);
    if(!applied.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(applied.Errors));
    return ApplyAndPersist(applied,true,
     "The Tower victory is applied. Preparing the floor-clear reward.");
   }
   if(active.Status==AbyssOperationStatus022.ReadyToFinalize)
   {
    if(active.PendingReceipt==null)
     return ApplyAndPersist(
      _campaignCommands022.CommitAbyssOperationCompletion(candidate,registry),true,
      "The Tower floor-clear reward is committed and saved.");
    var completed=_campaignCommands022.ApplyAbyssOperationCompletion(candidate,registry,_guildCityRecruitment);
    return ApplyAndPersist(completed,true,completed.IsSuccess
     ?TowerCompletionCopy094(candidate,completed.Value,registry)
     :"The Tower floor reward could not be saved. Your committed victory is retained.");
   }
   if(active.Status!=AbyssOperationStatus022.Active)
    return M1CommandResult.Failure("The Tower run is not ready to move.");
   if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var operation))
    return M1CommandResult.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
   if(active.CurrentStepIndex<0||active.CurrentStepIndex>=operation.steps.Length)
    return M1CommandResult.Failure("CAMPAIGN022_ABYSS_STEP_RANGE");
   var step=operation.steps[active.CurrentStepIndex];
   if(step.requiresBattle)
    return M1CommandResult.Failure("This Tower floor is ready for its Union battle.");
   if(active.PendingReceipt==null)
    return ApplyAndPersist(
     _campaignCommands022.CommitAbyssStep(candidate,registry,"SUCCESS"),true,
     "Tower battle preparation committed and saved.");
   return ApplyAndPersist(
    _campaignCommands022.ApplyAbyssStep(candidate,registry),true,
    "Tower battle preparation applied.");
  }

  public M1CommandResult BankTowerVictory110()
  {
   if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
   using var timing110=BeginTowerTiming110("manual-bank-transaction");
   var source=_campaign;
   if(ProgressionState081(source).ActiveAbyssOperation==null)
    return M1CommandResult.Failure("No Tower floor is waiting to be banked.");
   if(source.Battle==null||source.Battle.Phase!=BattlePhase.Resolved||
      source.Battle.Outcome!=BattleOutcome.Victory)
    return M1CommandResult.Failure("Finish the Tower battle before banking its victory.");
   if(source.Battle.Reward?.Claimed!=true)
    return M1CommandResult.Failure("Claim the battle reward before banking this Tower floor.");

   // Reuse the exact Auto authorities on one isolated candidate. Manual Bank
   // stops after completion; only the separate Fight action may begin a floor.
   // ApplyAndPersist retains normal recruit outfitting and full save validation.
   var registry=Registry022();
   var banked=AdvanceTowerToBattleBoundary108(source,registry);
   timing110?.Mark("bank-floor-authorities");
   if(!banked.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(banked.Errors));
   if(ProgressionState081(banked.Value).ActiveAbyssOperation!=null)
    return M1CommandResult.Failure("The current Tower floor still needs its battle. No reward was banked.");
   return ApplyAndPersist(banked,true,TowerCompletionCopy094(source,banked.Value,registry));
  }

  public M1CommandResult AdvanceTowerAutoAfterVictory108()
  {
   if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
   using var timing110=BeginTowerTiming110("auto-transition");
   var registry=Registry022();
   var claimed=TryBuildClaimedBattleRewards108(
    _campaign,out var candidate,out _);
   timing110?.Mark("claim-battle-and-return");
   if(!claimed.Succeeded)return claimed;

   var banked=AdvanceTowerToBattleBoundary108(candidate,registry);
   timing110?.Mark("bank-floor-authorities");
   if(!banked.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(banked.Errors));
   candidate=banked.Value;
   if(ProgressionState081(candidate).ActiveAbyssOperation!=null)
    return M1CommandResult.Failure("The current Tower floor still needs attention. Auto is paused.");

   // Recruit initialization belongs to the completed-floor transaction. Once
   // the next battle is created, its roster/equipment snapshot is immutable.
   // Keep the normal in-combat equipment guard and finish arrivals before it.
   try
   {
    if(_starterEquipment094!=null)
     candidate=_starterEquipment094.ApplyToNewRecruits(_campaign,candidate);
    timing110?.Mark("outfit-milestone-arrivals");
   }
   catch(Exception exception)
   {
    UnityEngine.Debug.LogError("TOWER_REWARD_RECRUIT_INITIALIZATION_FAILED110\n"+exception);
    return M1CommandResult.Failure(
     "The Tower reward could not be prepared. Your saved victory is ready to retry. "+exception.Message);
   }

   var rewardDisplay110=PrepareTowerRewardDisplay110(_campaign,candidate,registry);
   timing110?.Mark("prepare-actual-reward-display");
   var completedBattleId110=_campaign.Battle.BattleId;
   var begun=_campaignCommands022.BeginTowerFloor094(candidate,registry);
   timing110?.Mark("begin-next-floor-authority");
   if(!begun.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(begun.Errors));
   var prepared=AdvanceTowerToBattleBoundary108(begun.Value,registry);
   timing110?.Mark("prepare-next-floor-authorities");
   if(!prepared.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(prepared.Errors));
   candidate=prepared.Value;
   var active=ProgressionState081(candidate).ActiveAbyssOperation;
   if(active==null||active.Status!=AbyssOperationStatus022.Active)
    return M1CommandResult.Failure("The next Tower floor is not ready for battle. Auto is paused.");

   var encounter=_campaignCommands022.CommitAbyssBattleEncounter(candidate,registry);
   timing110?.Mark("commit-next-encounter");
   if(!encounter.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(encounter.Errors));
   var started=_guildCityBattleBridge.StartCertifiedEncounter(
    encounter.Value,_battleCommands,_combatContent,_encounterRosterResolver070);
   timing110?.Mark("initialize-next-battle");
   var saved110=ApplyAndPersist(started,true,
    "Tower rewards banked and the next floor battle started.");
   if(saved110.Succeeded)PublishTowerRewardDisplay110(completedBattleId110,rewardDisplay110);
   return saved110;
  }

  private Result<CampaignState> AdvanceTowerToBattleBoundary108(
   CampaignState source,ICampaignRegistry022 registry)
  {
   using var timing110=BeginTowerTiming110("tower-boundary-authorities");
   var candidate=source;
   for(var transition=0;transition<32;transition++)
   {
    var active=ProgressionState081(candidate).ActiveAbyssOperation;
    if(active==null)return Result<CampaignState>.Success(candidate);
    if(active.Status==AbyssOperationStatus022.AwaitingBattle)
    {
     var moved=active.PendingReceipt==null
      ?_campaignCommands022.CommitAbyssBattleResult(candidate,registry)
      :_campaignCommands022.ApplyAbyssBattleAndFinalize(candidate,registry);
     timing110?.Mark(active.PendingReceipt==null?"commit-battle-result":"apply-battle-result");
     if(!moved.IsSuccess)return moved;
     candidate=moved.Value;
     continue;
    }
    if(active.Status==AbyssOperationStatus022.ReadyToFinalize)
    {
     var moved=active.PendingReceipt==null
      ?_campaignCommands022.CommitAbyssOperationCompletion(candidate,registry)
      :_campaignCommands022.ApplyAbyssOperationCompletion(candidate,registry,_guildCityRecruitment);
     timing110?.Mark(active.PendingReceipt==null?"commit-floor-completion":"apply-floor-completion-and-hero");
     if(!moved.IsSuccess)return moved;
     candidate=moved.Value;
     continue;
    }
    if(active.Status!=AbyssOperationStatus022.Active)
     return Result<CampaignState>.Failure("The Tower run is not ready to move.");
    if(!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId,out var operation))
     return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
    if(active.CurrentStepIndex<0||active.CurrentStepIndex>=operation.steps.Length)
     return Result<CampaignState>.Failure("CAMPAIGN022_ABYSS_STEP_RANGE");
    if(operation.steps[active.CurrentStepIndex].requiresBattle)
     return Result<CampaignState>.Success(candidate);
    var advanced=active.PendingReceipt==null
     ?_campaignCommands022.CommitAbyssStep(candidate,registry,"SUCCESS")
     :_campaignCommands022.ApplyAbyssStep(candidate,registry);
    timing110?.Mark(active.PendingReceipt==null?"commit-nonbattle-step":"apply-nonbattle-step");
    if(!advanced.IsSuccess)return advanced;
    candidate=advanced.Value;
   }
   return Result<CampaignState>.Failure(
    "The Tower safety limit stopped an unexpected route. No additional step was taken.");
  }

  public M1CommandResult RetreatTowerRun081()
  {
   var registry=Registry022();
   var active=ProgressionState081(_campaign).ActiveAbyssOperation;
   if(active==null)return M1CommandResult.Failure("No Tower run is active.");
   var battleMatchesTower=_campaignCommands022.HasMatchingActiveAbyssBattle(_campaign,registry);
   if(_campaign?.Battle?.Outcome==BattleOutcome.InProgress&&battleMatchesTower)
    return M1CommandResult.Failure("Finish the active Tower battle before leaving the floor.");
   var candidate=_campaign;
   var defeatedBattle=battleMatchesTower&&
    candidate?.Battle!=null&&candidate.Battle.Phase==BattlePhase.Resolved&&
    candidate.Battle.Outcome!=BattleOutcome.Victory;
   if(defeatedBattle&&candidate.Battle.Reward?.Claimed!=true)
   {
    var claimed=_battleCommands.ClaimBattleRewards(candidate);
    if(!claimed.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(claimed.Errors));
    candidate=claimed.Value;
    var growth=new BattleEquipmentGrowthBridge022().ApplyClaimedBattleGrowth(candidate,registry);
    if(!growth.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(growth.Errors));
    candidate=growth.Value;
    var loot=ApplyVersion70WeaponLoot070(
     candidate,
     _campaign.Guild.GuildCity?.PendingEncounter,
     _campaign.Battle);
    if(!loot.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(loot.Errors));
    candidate=loot.Value;
   }
   return ApplyAndPersist(
    _campaignCommands022.RetreatAbyssOperation(candidate,registry),true,
    defeatedBattle
     ?"The climb ended, defeat rewards were saved, and your Unions returned safely. Retry this floor when you are stronger."
     :"Your Unions returned safely. Keep every reward already earned and retry when you are stronger.");
  }

  public M1CommandResult RecoverInactiveLegacyTower084()=>ApplyAndPersist(
   _campaignCommands022.MigrateInactiveLegacyAbyssState084(_campaign),true,
   "The older Tower record was recovered safely. Cleared floors and banked rewards remain exactly as saved.");

  internal Result<CampaignState> ReleaseIdleTowerForGuildProgression107(
   CampaignState campaign)
  {
   var active=ProgressionState081(campaign).ActiveAbyssOperation;
   if(active==null||string.IsNullOrWhiteSpace(active.OperationInstanceId)||
      !active.OperationInstanceId.StartsWith(
       CampaignProgressionCommandService022.TowerOperationPrefix094,
       StringComparison.Ordinal))
    return Result<CampaignState>.Success(campaign);

   var city=campaign?.Guild?.GuildCity;
   var idleBeforeBattle=active.Status==AbyssOperationStatus022.Active&&
    active.PendingReceipt==null&&city?.PendingEncounter==null&&
    city.PendingBattleReturn==null;
   if(!idleBeforeBattle)
    return Result<CampaignState>.Success(campaign);

   // A Guild route change parks the exact floor; it does not author a retreat.
   return SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.Switch(campaign,
    SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.Campaign);
  }

  public M1CommandResult EnterAbyssBattle022(){var committed=_campaignCommands022.CommitAbyssBattleEncounter(_campaign,Registry022());if(!committed.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(committed.Errors));var saved=ApplyAndPersist(committed,true,"Authored Abyss encounter committed before reveal.");if(!saved.Succeeded)return saved;return StartCommittedGuildCityBattle017D();}
  public M1CommandResult CommitAbyssStep022()=>ApplyAndPersist(_campaignCommands022.CommitAbyssStep(_campaign,Registry022(),"SUCCESS"),true,"Abyss step committed before reveal.");
  public M1CommandResult ApplyAbyssStep022()=>ApplyAndPersist(_campaignCommands022.ApplyAbyssStep(_campaign,Registry022()),true,"Abyss step applied exactly once.");
  public M1CommandResult CommitAbyssBattleResult022()=>ApplyAndPersist(_campaignCommands022.CommitAbyssBattleResult(_campaign,Registry022()),true,"Existing certified battle result linked to the Abyss operation.");
  public M1CommandResult FinalizeAbyssOperation022()
  {
   var registry=Registry022();
   var committed=_campaignCommands022.CommitAbyssOperationCompletion(_campaign,registry);
   if(!committed.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(committed.Errors));
   var completed=_campaignCommands022.ApplyAbyssOperationCompletion(committed.Value,registry,_guildCityRecruitment);
   return ApplyAndPersist(completed,true,completed.IsSuccess
    ?TowerCompletionCopy094(committed.Value,completed.Value,registry)
    :"The Tower floor reward could not be saved. Your committed victory is retained.");
  }
  public M1CommandResult FinalizeAbyssBattle022()=>ApplyAndPersist(_campaignCommands022.ApplyAbyssBattleAndFinalize(_campaign,Registry022()),true,"Claimed Abyss battle receipt applied to its authored step; remaining operation steps are still required.");
  public M1CommandResult CraftInvocationArtifact022(string baseId)=>CraftInvocationArtifact022(baseId,Array.Empty<string>());
  public M1CommandResult CraftInvocationArtifact022(string baseId,IReadOnlyList<string> affixIds)=>ApplyAndPersist(_campaignCommands022.CraftInvocationArtifact(_campaign,Registry022(),baseId,affixIds),true,"Invocation Artifact crafted with the exact selected affixes into Guild inventory; manual equip remains required.");
  public M1CommandResult EvolveInvocationArtifact022(string itemInstanceId)=>ApplyAndPersist(_campaignCommands022.EvolveInvocationArtifact(_campaign,Registry022(),itemInstanceId),true,"Invocation Artifact evolved in place under its authored resonance path.");
  public M1CommandResult InvokeEligibleEchoForecast022()=>ApplyAndPersist(_campaignCommands022.InvokeEligibleEchoForecast(_campaign,Registry022(),_battleCommands,_combatContent),true,"Eligible Echo invoked through the selected complete Union Forecast; shared AP, personal MP, and the exact-once receipt were saved.");
  public M1CommandResult InvokeAcceptedCovenantForecast022(string covenantId)=>ApplyAndPersist(_covenantBattle022.InvokeAcceptedCovenantForecast(_campaign,Registry022(),_campaignCommands022,_battleCommands,_combatContent,covenantId),true,"Accepted Great Covenant answered through the selected complete Union Forecast; its positive battle effect and exact-once receipt were saved.");
  public M1CommandResult AdvanceCovenant022(string covenantId)=>ApplyAndPersist(_campaignCommands022.AdvanceCovenantTrial(_campaign,Registry022(),covenantId),true,"One receipt-bound voluntary covenant trial stage recorded.");
  public M1CommandResult AcceptCovenant022(string covenantId)=>ApplyAndPersist(_campaignCommands022.AcceptCovenant(_campaign,Registry022(),covenantId),true,"Great Covenant accepted voluntarily after the completed trial.");


  static string TowerHeroRewardPreviewCopy094(int actualFloor,bool newPolicy,int legacyClearOrdinal)
  {
   if(!newPolicy)return CampaignProgressionCommandService022.IsTowerMajorRecruitMilestone089(legacyClearOrdinal)
    ?"  •  LEGACY REWARD: S-RANK RECRUIT LEAD":string.Empty;
   if(!TowerHeroRewardRules094.IsRewardFloor(actualFloor))return string.Empty;
   return TowerHeroRewardRules094.IsGuaranteedFloor(actualFloor)
    ?"  •  GUARANTEED "+TowerHeroRewardRules094.TierForFloor(actualFloor)+" HERO"
    :"  •  "+(TowerHeroRewardRules094.BonusChanceBasisPoints/100f).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+
     "% "+TowerHeroRewardRules094.TierForFloor(actualFloor)+" HERO CHANCE";
  }

  static string TowerSavedHeroRewardCopy094(TowerHeroRewardResult094 reward)
  {
   if(reward==null)return string.Empty;
   return reward.WinsHero
    ?reward.Tier+" HERO • "+reward.HeroDisplayName+" — reward received. New heroes join Reserve; duplicates strengthen your existing hero."
    :"No bonus hero this time. Your XP and loot are safely banked; the next milestone is ahead.";
  }

  string TowerCompletionCopy094(CampaignState before,CampaignState completed,ICampaignRegistry022 registry)
  {
   var active=ProgressionState081(before).ActiveAbyssOperation;
   if(active?.OperationInstanceId.StartsWith(CampaignProgressionCommandService022.TowerOperationPrefix094,StringComparison.Ordinal)==true)
   {
    if(_guildCityRecruitment!=null)
    {
     var reward=_guildCityRecruitment.DescribeLatestTowerHeroReward094(completed,registry);
     if(reward.IsSuccess&&reward.Value!=null)
      return "Tower floor "+reward.Value.ActualFloor+" cleared. "+TowerSavedHeroRewardCopy094(reward.Value);
    }
    return "Tower floor cleared. Rewards are banked and the next floor is unlocked.";
   }
   return CampaignProgressionCommandService022.IsTowerMajorRecruitMilestone089(
    ProgressionState081(completed).AbyssFloors.Sum(floor=>floor.ClearCount))
     ?"Tower floor cleared. Your legacy S-rank recruit lead is waiting at Recruitment."
     :"Tower floor cleared. Rewards are banked and the next floor is unlocked.";
  }

  static CampaignProgressionState022 ProgressionState081(CampaignState campaign)
   =>campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022??CampaignProgressionState022.Default();

  static int HighestClearedTowerFloor081(CampaignProgressionState022 progression,ICampaignRegistry022 registry)
   =>(progression?.AbyssFloors??Array.Empty<AbyssFloorProgressState022>())
    .Where(x=>x.ClearCount>0&&registry.Floors.ContainsKey(x.FloorId))
    .Select(x=>registry.Floors[x.FloorId].floor)
    .DefaultIfEmpty(0)
    .Max();

  CampaignProgressionPresentationState022 BuildProgression022()
  {
   using var timing110=BeginTowerTiming110("read-tower-progression");
   var r=Registry022();
   var strategic=_campaign?.Guild?.GuildCity?.Strategic017H;
   var s=strategic?.Campaign019?.Playable020?.Progression022??CampaignProgressionState022.Default();
   var templateHighest=HighestClearedTowerFloor081(s,r);
   var totalClears=s.AbyssFloors.Sum(x=>Math.Max(0,x.ClearCount));
   var storyGates=strategic?.StoryGates??Array.Empty<string>();
   var active=s.ActiveAbyssOperation;
   var legacyTowerRecoveryRequired=CampaignProgressionCommandService022
    .RequiresLegacyAbyssRecovery084(s);
   var legacyTowerRecoveryRequiresSupport=legacyTowerRecoveryRequired&&
    (active!=null||_campaign?.Guild?.GuildCity?.PendingEncounter!=null||
     _campaign?.Guild?.GuildCity?.PendingBattleReturn!=null);
   var authority110=ReadTowerAuthorityProjection110(r,legacyTowerRecoveryRequired);
   var greatCovenantGateEarned=authority110.GreatCovenantGateEarned;
   var actualFloors=authority110.Floors;
   var towerAuthorityError=!legacyTowerRecoveryRequired&&!actualFloors.IsSuccess
    ?"Your Tower record needs attention. No floor or reward was changed. "+FriendlyErrors(actualFloors.Errors)
    :string.Empty;
   var highest=actualFloors.IsSuccess?actualFloors.Value.HighestActualFloor:templateHighest;
   var usesNewFloorPolicy=active==null||actualFloors.IsSuccess&&actualFloors.Value.ActiveUsesNewPolicy;
   AbyssOperationDto022 activeDefinition=null;
   if(active!=null)r.AbyssOperations.TryGetValue(active.OperationDefinitionId,out activeDefinition);
   var activeStep=activeDefinition!=null&&active.CurrentStepIndex>=0&&active.CurrentStepIndex<activeDefinition.steps.Length
    ?activeDefinition.steps[active.CurrentStepIndex]
    :null;
   var activeStepRequiresBattle=activeStep?.requiresBattle==true;
   var pendingAbyssReceipt=active?.PendingReceipt;
   var hasPendingAbyssBattleReceipt=active?.PendingReceipt?.ReceiptId.StartsWith("ABYSSREC022_",StringComparison.Ordinal)==true&&StringComparer.Ordinal.Equals(active.PendingReceipt.Outcome,"VICTORY")&&active.PendingReceipt.AppliedVersion==0;
   var templateFloorNumber=actualFloors.IsSuccess?actualFloors.Value.ContentTemplateFloor:
    active!=null&&r.Floors.TryGetValue(active.FloorId,out var activeFloor)
     ?activeFloor.floor:TowerRunRules081.NextFloorNumber(templateHighest);
   var plannedFloorNumber=actualFloors.IsSuccess
    ?active!=null?actualFloors.Value.ActiveActualFloor:actualFloors.Value.NextActualFloor
    :templateFloorNumber;
   var plannedTemplate=r.Floors.Values.FirstOrDefault(floor=>floor.floor==templateFloorNumber);
   var templateFirstClear=plannedTemplate!=null&&!s.AbyssFloors.Any(floor=>
    floor.FloorId==plannedTemplate.floorId&&floor.ClearCount>0);
   var plannedOperationId=active?.OperationDefinitionId??
    CampaignProgressionCommandService022.TowerOperationDefinitionId094(templateFloorNumber,templateFirstClear);
   r.AbyssOperations.TryGetValue(plannedOperationId,out var plannedOperation);
   var towerOperation=activeDefinition??plannedOperation;
   var towerFloorId=active?.FloorId??towerOperation?.floorId??string.Empty;
   r.Floors.TryGetValue(towerFloorId,out var towerFloor);
   var currentFloorProgress=s.AbyssFloors.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.FloorId,towerFloorId));
   var floorTen=r.Floors.Values.FirstOrDefault(x=>x.floor==TowerRunRules081.OpeningFloorCount);
   var floorTenClearCount=floorTen==null?0:s.AbyssFloors.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.FloorId,floorTen.floorId))?.ClearCount??0;
   var battle=_campaign?.Battle;
   var towerBattleInProgress=battle!=null&&battle.Outcome==BattleOutcome.InProgress;
   var towerBattleResolved=battle!=null&&battle.Phase==BattlePhase.Resolved&&
    (battle.Outcome!=BattleOutcome.Victory||battle.Reward?.Claimed==true);
   var towerBattleWon=towerBattleResolved&&battle.Outcome==BattleOutcome.Victory;
   var materialCount=towerOperation?.rewardMaterialIds?.Length??0;
   var battleMatchesTower=authority110.BattleMatchesTower;
   var towerBattleRewardAwaitingClaim=battleMatchesTower&&battle!=null&&
    battle.Phase==BattlePhase.Resolved&&battle.Outcome==BattleOutcome.Victory&&
    battle.Reward!=null&&!battle.Reward.Claimed;
   // Existing encounters preview their saved force. Only a new098 run uses
   // actual-floor growth; legacy094 runs retain their original template budget.
   var expectedTowerEnemyUnions098=CampaignProgressionCommandService022.TowerEnemyUnionCount081(
    templateFloorNumber,currentFloorProgress?.ClearCount??0);
   if(active==null||actualFloors.IsSuccess&&CampaignProgressionCommandService022.UsesActualThreatPolicy098(_campaign,active))
    expectedTowerEnemyUnions098=TowerThreatRules098.EnemyUnionCount098(Math.Max(1,plannedFloorNumber));
   if(battleMatchesTower&&battle!=null)expectedTowerEnemyUnions098=battle.EnemyUnions.Count;
   TowerHeroRewardResult094 lastHeroReward=null;
   if(!legacyTowerRecoveryRequired&&actualFloors.IsSuccess&&_guildCityRecruitment!=null)
   {
    var savedReward=authority110.LastHeroReward;
    if(savedReward.IsSuccess)lastHeroReward=savedReward.Value;
    else towerAuthorityError="Your saved Tower reward needs attention. Nothing was rerolled or removed. "+FriendlyErrors(savedReward.Errors);
   }
   var v=new CampaignProgressionPresentationState022
   {
    IsAvailable=true,
    WeaponTracks=r.WeaponTracks.Count,
    WeaponRecipes=r.WeaponRecipes.Count,
    ArmorRecipes=r.ArmorRecipes.Count,
    AdvancedClasses=r.Classes.Count,
    CertificationPaths=r.CertificationPaths.Count,
    AbyssFloors=r.Floors.Count,
    AbyssOperations=r.AbyssOperations.Count,
    ArtifactBases=r.ArtifactBases.Count,
    Covenants=r.Covenants.Count,
    SummonResonance=s.SummonResonance,
    EchoInvocationReceiptCount=s.AppliedReceiptIds.Count(x=>x.StartsWith("ECHOREC022_",StringComparison.Ordinal)),
    GreatCovenantGateEarned=greatCovenantGateEarned,
    ActiveAbyssOperationId=active?.OperationInstanceId??string.Empty,
    ActiveAbyssFloorId=active?.FloorId??string.Empty,
    ActiveAbyssStatus=active?.Status.ToString()??"NONE",
    LegacyTowerRecoveryRequired=legacyTowerRecoveryRequired,
    LegacyTowerRecoveryRequiresSupport=legacyTowerRecoveryRequiresSupport,
    ActiveStepIndex=active?.CurrentStepIndex??0,
    ActiveStepCount=activeDefinition?.steps?.Length??0,
    ActiveCompletedStepCount=active?.CompletedStepIds.Count??0,
    ActiveOperationDisplayName=activeDefinition?.displayName??string.Empty,
    ActiveOperationKind=activeDefinition?.kind??string.Empty,
    ActiveStepKind=activeStep?.kind??string.Empty,
    ActiveStepTitle=activeStep?.title??string.Empty,
    ActiveStepDescription=TowerAdventureRules084.TileDescription084(
     activeStep?.kind,activeStepRequiresBattle,activeDefinition?.kind),
    ActiveStepRequiresBattle=activeStepRequiresBattle,
    HasPendingAbyssStepReceipt=pendingAbyssReceipt!=null,
    HasPendingAbyssBattleReceipt=hasPendingAbyssBattleReceipt,
    PendingAbyssOutcome=pendingAbyssReceipt?.Outcome??string.Empty,
    PendingAbyssReward=TowerAdventureRules084.ReceiptReward084(pendingAbyssReceipt),
    TowerTrackPhase=TowerAdventureRules084.TrackPhase084(
     active?.CurrentStepIndex??0,active?.Status??AbyssOperationStatus022.Active),
    TowerTrackLabels=(activeDefinition?.steps??Array.Empty<AbyssStepDto022>())
     .Select(step=>TowerAdventureRules084.TileLabel084(
      step.kind,step.requiresBattle)).ToArray(),
    TowerBattleInProgress=battleMatchesTower&&towerBattleInProgress,
    TowerBattleRewardAwaitingClaim=towerBattleRewardAwaitingClaim,
    TowerBattleResolved=battleMatchesTower&&towerBattleResolved,
    TowerBattleWon=battleMatchesTower&&towerBattleWon,
    HighestClearedTowerFloor=highest,
    TotalTowerClears=totalClears,
    TowerFloorNumber=plannedFloorNumber,
    TowerContentTemplateFloor094=templateFloorNumber,
    TowerUsesNewFloorPolicy094=usesNewFloorPolicy,
    TowerAuthorityError094=towerAuthorityError,
    TowerLastHeroRewardFloor094=lastHeroReward?.ActualFloor??0,
    TowerLastHeroRewardWon094=lastHeroReward?.WinsHero??false,
    TowerLastHeroRewardSummary094=TowerSavedHeroRewardCopy094(lastHeroReward),
    TowerRunNumber=usesNewFloorPolicy?((Math.Max(1,plannedFloorNumber)-1)/TowerRunRules081.OpeningFloorCount)+1:
     TowerRunRules081.RunNumber(templateHighest,floorTenClearCount),
    TowerExpectedEnemyUnions=expectedTowerEnemyUnions098,
    TowerGuildXpReward=towerOperation?.guildXp??0,
    TowerHallXpReward=towerOperation?.hallXp??0,
    TowerRewardMaterialIds108=(towerOperation?.rewardMaterialIds??Array.Empty<string>()).ToArray(),
    TowerFloorId=towerFloorId,
    TowerFloorDisplayName=towerFloor?.displayName??"Unknown Floor",
    TowerOperationId=plannedOperationId,
    TowerOperationDisplayName=towerOperation?.displayName??"Tower Battle",
    TowerRewardSummary="+"+(towerOperation?.guildXp??0)+" GUILD XP  •  +"+(towerOperation?.hallXp??0)+" HALL XP  •  +"+(materialCount*4)+" MATERIALS (4 EACH)"+
     TowerHeroRewardPreviewCopy094(plannedFloorNumber,usesNewFloorPolicy,totalClears+1),
    TowerArtResourcePath=TowerRunRules081.ArtResourcePath(templateFloorNumber)
   };
   ApplyOptionalTowerRoomModule001(
    v,
    active?.OperationInstanceId??string.Empty,
    templateFloorNumber,
    active?.CompletedStepIds??Array.Empty<string>());
   var ownedEquipment=OwnedEquipmentForPresentation022(_campaign);var equippedIds=new HashSet<string>((_campaign?.Guild?.Recruits??Array.Empty<RecruitState>()).SelectMany(x=>x.Equipment?.Assignments??Array.Empty<EquipmentSlotAssignmentState>()).Where(x=>x?.Item!=null).Select(x=>x.Item.InstanceId),StringComparer.Ordinal);
   v.Equipment=ownedEquipment.Select(item=>{var evolution=s.EquipmentEvolution.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ItemInstanceId,item.InstanceId));var track=evolution?.TrackId??r.WeaponTracks.Values.Where(x=>item.EquipmentTags.Contains(x.weaponFamilyId)).OrderBy(x=>x.trackId,StringComparer.Ordinal).Select(x=>x.trackId).FirstOrDefault()??string.Empty;return new ProgressionItemView022{ItemInstanceId=item.InstanceId,DisplayName=item.DisplayName,TrackId=track,TierId=evolution?.TierId??"TRAINING",MeaningfulUses=evolution?.MeaningfulUses??0,MasteryPoints=evolution?.MasteryPoints??0,IsEquipped=equippedIds.Contains(item.InstanceId)};}).Where(x=>!string.IsNullOrWhiteSpace(x.TrackId)).OrderBy(x=>x.ItemInstanceId,StringComparer.Ordinal).ToArray();
   v.ClassStates=s.ClassCertifications.Select(x=>new ClassCertificationView022{RecruitId=x.RecruitId,ActiveClassId=x.ActiveAdvancedClassId,CertifiedCount=x.CertifiedClassIds.Count}).ToArray();
   v.FloorStates=r.Floors.Values.OrderBy(x=>x.floor).Select(f=>
   {
    var p=s.AbyssFloors.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.FloorId,f.floorId));
    var clears=p?.ClearCount??0;
    var current=f.floor==templateFloorNumber;
    return new AbyssFloorView022
    {
     FloorId=f.floorId,
     DisplayName=f.displayName,
     FloorNumber=f.floor,
     ClearCount=clears,
     IsUnlocked=f.floor<=Math.Min(TowerRunRules081.OpeningFloorCount,templateHighest+1),
     IsCurrentGoal=current,
     Status=clears>0?"CLEARED ×"+clears:(current?"NEXT FIGHT":"LOCKED"),
     AboveGroundChangeId=f.firstClearAboveGroundChangeId
    };
   }).ToArray();
   v.TowerOperationChoices=r.AbyssOperations.Values
    .OrderBy(operation=>r.Floors[operation.floorId].floor)
    .ThenBy(operation=>operation.kind=="GUARDIAN"?0:operation.kind=="RECON"?1:2)
    .ThenBy(operation=>operation.operationId,StringComparer.Ordinal)
    .Select(operation=>
    {
     var floor=r.Floors[operation.floorId];
     var floorProgress=s.AbyssFloors.FirstOrDefault(value=>
      StringComparer.Ordinal.Equals(value.FloorId,floor.floorId));
     var clearCount=floorProgress?.ClearCount??0;
     var previousCleared=floor.floor<=1||r.Floors.Values
      .Where(value=>value.floor==floor.floor-1)
      .Any(previous=>s.AbyssFloors.Any(value=>
       StringComparer.Ordinal.Equals(value.FloorId,previous.floorId)&&
       value.ClearCount>0&&value.FirstClearApplied));
     var available=active==null&&string.IsNullOrEmpty(towerAuthorityError)&&
      (!usesNewFloorPolicy||operation.operationId==plannedOperationId)&&TowerAdventureRules084.IsAvailable084(
      operation,floor.floor,clearCount,previousCleared,
      floorProgress?.FirstClearApplied==true);
     var materialCountForOperation=operation.rewardMaterialIds?.Length??0;
     return new Campaign022.AbyssOperationView084
     {
      OperationId=operation.operationId,
      FloorId=operation.floorId,
      FloorNumber=floor.floor,
      FloorDisplayName=floor.displayName,
      DisplayName=operation.displayName,
      Kind=operation.kind,
      PlayerKind=TowerAdventureRules084.PlayerOperationKind084(operation.kind),
      StepCount=operation.steps?.Length??0,
      RequiresBattle=(operation.steps??Array.Empty<AbyssStepDto022>())
       .Any(step=>step.requiresBattle),
      FirstClearOnly=operation.firstClearOnly,
      Available=available,
      LockedReason=TowerAdventureRules084.LockedReason084(
       operation,clearCount,previousCleared,
       floorProgress?.FirstClearApplied==true),
      RewardSummary="+"+operation.guildXp+" GUILD XP  •  +"+
       operation.hallXp+" HALL XP  •  +"+
       (materialCountForOperation*4)+" MATERIALS"
     };
    }).ToArray();
   v.InvocationArtifactBases=r.ArtifactBases.Values.OrderBy(x=>x.baseId,StringComparer.Ordinal).Select(x=>new InvocationArtifactBaseView022{BaseId=x.baseId,DisplayName=x.displayName,WeaponFamilyId=x.weaponFamilyId,MaterialCosts=(x.materialCosts??Array.Empty<MaterialCostDto022>()).Select(cost=>cost.materialId+" ×"+cost.amount).ToArray()}).ToArray();
   v.InvocationAffixes=r.Affixes.Values.OrderBy(x=>x.affixId,StringComparer.Ordinal).Select(x=>new InvocationAffixView022{AffixId=x.affixId,DisplayName=x.displayName,ForecastCategory=x.forecastCategory,EffectPermille=x.effectPermille}).ToArray();
   v.Artifacts=s.InvocationArtifacts.Select(x=>new ArtifactView022{InstanceId=x.InstanceId,DisplayName=r.ArtifactBases.TryGetValue(x.BaseId,out var b)?b.displayName:x.BaseId,EvolutionStage=x.EvolutionStage,Resonance=x.Resonance,EvolutionPathId=x.EvolutionPathId,AffixIds=x.AffixIds}).ToArray();
   v.CovenantStates=r.Covenants.Values.OrderBy(x=>x.displayName).Select(c=>{var p=s.Covenants.FirstOrDefault(x=>x.CovenantId==c.covenantId);var eligible=storyGates.Contains(c.requiredStoryGate)&&highest>=c.minimumAbyssFloor&&(!StringComparer.Ordinal.Equals(c.requiredStoryGate,CampaignProgressionCommandService022.GreatCovenantsStoryGate)||greatCovenantGateEarned);var status=p?.Status??(eligible?CovenantStatus022.TrialAvailable:CovenantStatus022.Locked);var closed=status==CovenantStatus022.Accepted||status==CovenantStatus022.Refused;return new CovenantView022{CovenantId=c.covenantId,DisplayName=c.displayName,Role=c.role,Status=status.ToString(),TrialProgress=p?.TrialProgress??0,Trust=p?.Trust??0,TrialReceiptCount=p?.AppliedReceiptIds.Count(x=>x.StartsWith("COVTRIALREC022_",StringComparison.Ordinal))??0,CanAdvanceTrial=eligible&&!closed&&(p?.TrialProgress??0)<100,CanAccept=eligible&&status==CovenantStatus022.TrialActive&&(p?.TrialProgress??0)>=100,AcceptanceReceiptApplied=p?.AppliedReceiptIds.Any(x=>x.StartsWith("COVACCEPTREC022_",StringComparison.Ordinal))??false};}).ToArray();return v;
  }

  public static IReadOnlyList<EquipmentItemState> OwnedEquipmentForPresentation022(CampaignState campaign)
  {
   var byId=new Dictionary<string,EquipmentItemState>(StringComparer.Ordinal);var guild=campaign?.Guild;if(guild==null)return Array.Empty<EquipmentItemState>();
   for(var index=0;index<guild.Inventory.Count;index++){var item=guild.Inventory[index];if(item!=null&&!byId.ContainsKey(item.InstanceId))byId.Add(item.InstanceId,item);}
   for(var recruitIndex=0;recruitIndex<guild.Recruits.Count;recruitIndex++){var assignments=guild.Recruits[recruitIndex]?.Equipment?.Assignments;if(assignments==null)continue;for(var assignmentIndex=0;assignmentIndex<assignments.Count;assignmentIndex++){var item=assignments[assignmentIndex]?.Item;if(item!=null&&!byId.ContainsKey(item.InstanceId))byId.Add(item.InstanceId,item);}}
   return byId.Values.OrderBy(x=>x.InstanceId,StringComparer.Ordinal).ToArray();
  }
 }
}
