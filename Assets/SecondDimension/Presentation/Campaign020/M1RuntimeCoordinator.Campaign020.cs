using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
 public sealed partial class M1RuntimeCoordinator : Campaign020.ICampaignPlayablePresentationCoordinator020
 {
   Campaign020.CampaignRegistry020 _campaignRegistry020;
   Campaign020.Campaign020RuleCatalogAdapter _campaignRules020;
   readonly CampaignPlayableCommandService020 _campaignCommands020=new CampaignPlayableCommandService020();

   Campaign020.CampaignRegistry020 Registry020()
   {
     if(_campaignRegistry020==null)
     {
       _campaignRegistry020=Campaign020.CampaignRegistry020.LoadFromResources();
       _campaignRules020=new Campaign020.Campaign020RuleCatalogAdapter(_campaignRegistry020);
     }
     return _campaignRegistry020;
   }

   public Campaign020.CampaignPlayablePresentationState020 CampaignPlayable020
   {
     get
     {
       try{return BuildCampaignPlayablePresentation020();}
       catch(Exception e){return new Campaign020.CampaignPlayablePresentationState020{IsAvailable=false,Error=e.Message};}
     }
   }

    public M1CommandResult StartPlayableChapter020(string chapterId)
    {
      Registry019(); Registry020();
      var boundary=ReleaseIdleTowerForGuildProgression107(_campaign);
      if(!boundary.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(boundary.Errors));
      var candidate=boundary.Value;
      var progress=candidate?.Guild?.GuildCity?.Strategic017H?.Campaign019;
      var existingChapter=progress?.ActiveOperation;
      var existingPlayable=progress?.Playable020?.ActiveOperation;
      if(existingChapter!=null&&existingPlayable==null&&
         StringComparer.Ordinal.Equals(existingChapter.ChapterId,chapterId)&&
         StringComparer.Ordinal.Equals(progress.ActiveChapterId,chapterId))
      {
        // Migration bridge for saves that committed the authored Campaign019
        // chapter before the playable-operation layer existed.
        return ApplyAndPersist(_campaignCommands020.BeginOperation(
          candidate,_campaignRules020,chapterId),true,
          "Existing story quest recovered into its playable board operation.");
      }
      var first=_campaignCommands019.StartChapter(candidate,_campaignRules019,chapterId,FirstCampaignUnionIds019(),ownerApprovedCandidateOrder:true);
     if(!first.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(first.Errors));
     var second=_campaignCommands020.BeginOperation(first.Value,_campaignRules020,chapterId);
     return ApplyAndPersist(second,true,"Playable campaign chapter and exact operation blueprint committed.");
   }

   public M1CommandResult CommitPlayableStep020(string outcome)
   {
     Registry020();
     return ApplyAndPersist(_campaignCommands020.CommitNonBattleStep(_campaign,_campaignRules020,outcome),true,"Operation step outcome committed before reveal.");
   }

   public M1CommandResult ApplyPlayableStep020()
   {
     Registry019(); Registry020();
     return ApplyAndPersist(_campaignCardFlow132.ApplyStoryAndPrepare(_campaign,_campaignRules020,_campaignRules019),true,"Story interruption resolved; the next authored boundary is saved.");
   }

   public M1CommandResult EnterPlayableBattle020()
   {
     Registry019(); Registry020();
     var candidate=_campaign;
     if(candidate?.Guild?.GuildCity?.PendingEncounter==null)
     {
       var encounter=_campaignCommands019.CommitCertifiedBattle(candidate,_campaignRules019);
       if(!encounter.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(encounter.Errors));
       candidate=encounter.Value;
     }
     var marked=_campaignCommands020.MarkBattleCommitted(candidate,_campaignRules020);
     if(!marked.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(marked.Errors));
     var saved=ApplyAndPersist(marked,false,"Playable campaign battle step committed.");
     if(!saved.Succeeded)return saved;
     return StartCommittedGuildCityBattle017D();
   }

   public M1CommandResult CommitPlayableBattleStepResult020()
   {
     Registry020();
     return ApplyAndPersist(_campaignCommands020.CommitBattleStepReceipt(_campaign,_campaignRules020),true,"Existing certified battle result linked to this operation step without a second item roll.");
   }

   public M1CommandResult FinalizePlayableChapter020()
   {
     Registry019(); Registry020();
     var view=CampaignPlayable020;
     if(!view.IsAvailable||string.IsNullOrWhiteSpace(view.ActiveOperationId))return M1CommandResult.Failure("CAMPAIGN020_ACTIVE_OPERATION_REQUIRED");
     var progress=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
     var blueprint=Registry020().Blueprints[view.ActiveChapterId];
     if(blueprint.requiresCertifiedBattle || CampaignReplayBattle134.Required(_campaign, view.ActiveChapterId))
     {
       if(progress?.PendingReceipt==null)return M1CommandResult.Failure("Claim the certified battle reward and commit its campaign return before finalizing.");
       return M1CommandResult.Success("Existing campaign battle receipt is ready. Apply it to close the playable operation.");
     }
     return ApplyAndPersist(_campaignCommands019.CommitNonCombatReceipt(_campaign,_campaignRules019,"SUCCESS"),true,"Noncombat campaign chapter receipt committed.");
   }

   public M1CommandResult ApplyPlayableChapterResult020()
   {
     Registry019(); Registry020();
     if(_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.ActiveOperation?.ChapterId==CampaignMissionMap132.FinaleChapterId)
     {
       Registry023();
       var restarted=new SecondDimension.Gameplay.Campaign019.CampaignReplayFinale132()
           .CompleteAndRestart(_campaign,_campaignRules019,_campaignRules020,_campaignRules023);
       if(!restarted.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(restarted.Errors));
       var next=PrepareCampaignDeck131("CH018_001",restarted.Value);
       return ApplyAndPersist(next,true,"Mission81 finale complete. A stronger campaign begins at mission1; all earned history is retained.");
     }
      var first=_campaignCommands019.ApplyReceiptExactlyOnce(_campaign,
          _campaignRules019,_campaignRules020);
     if(!first.IsSuccess)return M1CommandResult.Failure(FriendlyErrors(first.Errors));
     var chapterId=_campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.ActiveOperation?.ChapterId;
     var second=_campaignCommands020.CloseCompletedOperation(first.Value,_campaignRules020);
     if(second.IsSuccess && chapterId==CampaignMissionMap132.FinaleOpeningChapterId)
     {
       Registry023();
       second=PrepareCampaignDeck131(CampaignMissionMap132.FinaleChapterId,second.Value);
     }
      return ApplyAndPersist(second,true,"Campaign mission progress, world standing, and earned rewards saved exactly once.");
    }

    public M1CommandResult RecoverLegacyPlayableQuest020()
    {
      return ApplyAndPersist(
        _campaignCommands020.AbortUnverifiableLegacyOperation084(_campaign),true,
        "Old unfinished quest recovered safely. No reward or completed history changed.");
    }

    public M1CommandResult RecoverInsertedBattleBoundary020()
    {
      Registry020();
      return ApplyAndPersist(
        _campaignCommands020.RecoverInsertedBattleBoundary084(
          _campaign,_campaignRules020),true,
        "Quest progress restored. The required Union battle is ready; previous rewards and completed tiles are unchanged.");
    }

   Campaign020.CampaignPlayablePresentationState020 BuildCampaignPlayablePresentation020()
   {
     var r=Registry020();
     var p=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
     var playable=p?.Playable020??CampaignPlayableState020.Default();
     var op=playable.ActiveOperation;
      var battleInProgress=op?.Status==CampaignPlayableOperationStatus020.AwaitingBattle&&
       _campaign?.Battle?.Outcome==SecondDimension.Gameplay.M2.BattleOutcome.InProgress;
      var awaitingBattleReward=op?.Status==CampaignPlayableOperationStatus020.AwaitingBattle&&
       _campaign?.Battle?.Phase==SecondDimension.Gameplay.M2.BattlePhase.Resolved&&
       _campaign.Battle.Reward!=null&&!_campaign.Battle.Reward.Claimed;
      var state=new Campaign020.CampaignPlayablePresentationState020{IsAvailable=true,ActiveOperationId=op?.OperationId??string.Empty,ActiveChapterId=op?.ChapterId??string.Empty,WorldId=op?.WorldId??string.Empty,WorldName=op!=null&&r.Travel.TryGetValue(op.WorldId,out var activeTravel020)?activeTravel020.displayName:"Skyhome",Status=op?.Status.ToString()??"NONE",CurrentStepIndex=op?.CurrentStepIndex??0,PendingStepReceiptId=op?.PendingReceipt?.ReceiptId??string.Empty,PendingOutcome=op?.PendingReceipt?.Outcome??string.Empty,PendingReward=op?.PendingReceipt==null?string.Empty:"+"+op.PendingReceipt.GuildXp+" Guild XP  •  +"+op.PendingReceipt.HallXp+" Hall XP"+(op.PendingReceipt.Materials.Count>0?"  •  World materials ×"+op.PendingReceipt.Materials.Sum(value=>value.Amount):""),ExistingBattleRewardReferenced=!string.IsNullOrWhiteSpace(op?.ExistingBattleRewardReceiptId),BattleInProgress=battleInProgress,AwaitingBattleRewardClaim=awaitingBattleReward,CampaignEpilogueUnlocked=playable.CampaignEpilogueUnlocked,LegacyRecoveryRequired=CampaignPlayableCommandService020.RequiresLegacyRecovery084(playable),InsertedBattleBoundaryRecoveryRequired=_campaignCommands020.RequiresInsertedBattleBoundaryRecovery084(_campaign,_campaignRules020)};
     if(op!=null&&r.Blueprints.TryGetValue(op.ChapterId,out var b))
     {
       state.OperationTitle=b.title;
       _campaignRules020.TryGetBlueprint(op.ChapterId,out var authored134);
       var effective134=CampaignReplayBattle134.Effective(_campaign,authored134);
       state.TotalSteps=effective134.Steps.Count;
       var steps=new List<Campaign020.CampaignStepView020>();
       for(var i=0;i<effective134.Steps.Count;i++)
       {
         var rule=effective134.Steps[i];
         var description134=b.steps.FirstOrDefault(value=>value.stepId==rule.StepId)?.description??
             "Protect the Hall foundations against the returning threat. Win this Union battle to complete the mission.";
         steps.Add(new Campaign020.CampaignStepView020{StepId=rule.StepId,Kind=rule.Kind,TileType=CampaignAdventureRules084.TileType084(rule.Kind),ActionLabel=CampaignAdventureRules084.ActionLabel084(rule.Kind),RewardPreview=CampaignAdventureRules084.RewardPreview084(rule),Title=rule.Title,Description=description134,Status=op.CompletedStepIds.Contains(rule.StepId)?"COMPLETED":i==op.CurrentStepIndex?"CURRENT":i<op.CurrentStepIndex?"COMPLETED":"FACE_DOWN",RequiresCertifiedBattle=rule.RequiresCertifiedBattle,ConsumesOperation=rule.ConsumesOperation,IsWorldBoard=CampaignAdventureRules084.IsWorldBoardStep084(rule)});
       }
       state.Steps=steps.AsReadOnly();
     }
     var reps=new List<Campaign020.RepeatableContractView020>();
     foreach(var c in r.Repeatables.Values.OrderBy(x=>x.worldId).ThenBy(x=>x.contractId))reps.Add(new Campaign020.RepeatableContractView020{ContractId=c.contractId,WorldId=c.worldId,Title=c.title,OperationType=c.operationType,Unlocked=playable.UnlockedRepeatableContractIds.Contains(c.contractId),BattleCount=c.battleCount,EventCount=c.eventCount});
     state.Repeatables=reps.AsReadOnly();
     var summaries=new List<Campaign020.WorldContentSummary020>();
     foreach(var travel in r.Travel.Values.OrderBy(x=>x.displayName))
     {
       var gate=playable.WorldGates.FirstOrDefault(x=>x.WorldId==travel.worldId);
       summaries.Add(new Campaign020.WorldContentSummary020{WorldId=travel.worldId,DisplayName=travel.displayName,GateUnlocked=gate?.Unlocked??travel.worldId=="SKYHOME",EnemyArchetypes=r.EnemyPacks.TryGetValue(travel.worldId,out var ep)?ep.archetypes.Length:0,RecruitOrigins=r.RecruitPacks.TryGetValue(travel.worldId,out var rp)?rp.profiles.Length:0,LootEntries=r.LootProfiles.TryGetValue(travel.worldId,out var lp)?lp.entries.Length:0,RepeatableContracts=r.Repeatables.Values.Count(x=>x.worldId==travel.worldId),CrisisOperations=r.Crises.Values.Count(x=>x.worldId==travel.worldId),StandingTier=gate?.StandingTier??"LOCKED"});
     }
     state.WorldSummaries=summaries.AsReadOnly();
     return state;
   }
 }
}
