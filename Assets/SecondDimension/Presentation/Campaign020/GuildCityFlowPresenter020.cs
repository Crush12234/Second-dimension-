using System;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
 public sealed partial class M1FlowPresenter
 {
   void EnterPlayableCampaignBattle020(Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator)
   {
     var result=coordinator?.EnterPlayableBattle020();
     _localStatus=result?.Message??"The playable campaign battle command returned no result.";
     _localStatusPositive=result!=null&&result.Succeeded;
     if(_localStatusPositive)Navigate(M1Screen.Battle);else BuildCurrentScreen();
   }

   void BuildGuildCityCampaignLegacy020(Transform body,Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,Campaign020.CampaignPlayablePresentationState020 state)
   {
     if(state==null||!state.IsAvailable){AddMessagePanel(body,"PLAYABLE CAMPAIGN OPERATIONS 020",state?.Error??"Campaign 020 authority unavailable.",RuntimeUi.Warning);return;}
     AddMessagePanel(body,"PLAYABLE OPERATION CONTROLLER","Campaign 020 turns every authored chapter into committed briefing, board, civic, diplomacy, fortress, certified-battle, and result steps. Existing battle rewards remain authoritative; operation receipts never duplicate equipment.",RuntimeUi.Accent);
     var campaign019=coordinator as Campaign019.ICampaignPresentationCoordinator019;
     var chapterReceiptReady=campaign019?.Campaign019!=null&&!string.IsNullOrWhiteSpace(campaign019.Campaign019.PendingReceiptId);
     if(string.IsNullOrWhiteSpace(state.ActiveOperationId))
     {
       AddMessagePanel(body,"NO ACTIVE OPERATION","Start an available chapter above. Campaign 020 will create its deterministic operation blueprint.",RuntimeUi.ButtonNormal);
     }
     else
     {
       AddMessagePanel(body,"ACTIVE • "+state.ActiveChapterId,state.Status+" • Step "+Math.Min(state.CurrentStepIndex+1,Math.Max(1,state.TotalSteps))+" / "+state.TotalSteps+" • "+state.WorldId+(state.ExistingBattleRewardReferenced?"\nExisting battle reward receipt linked — no duplicate item roll.":""),RuntimeUi.Accent);
       foreach(var step in state.Steps)
       {
         var panel=AddMessagePanel(body,step.Title,step.Status+" • "+step.Kind+(step.RequiresCertifiedBattle?" • CERTIFIED UNION BATTLE":"")+(step.ConsumesOperation?"":" • FREE/PRESENTATION")+"\n"+step.Description,step.Status=="CURRENT"?RuntimeUi.Accent:step.Status=="COMPLETED"?RuntimeUi.Positive:RuntimeUi.ButtonNormal);
         if(step.Status=="CURRENT"&&string.IsNullOrWhiteSpace(state.PendingStepReceiptId))
         {
           if(step.RequiresCertifiedBattle)
           {
             if(state.Status=="AwaitingBattle")RuntimeUi.AddButton(panel,"Commit battle step result 020","COMMIT CLAIMED BATTLE RESULT",()=>RunGuildCityCommand017D(coordinator.CommitPlayableBattleStepResult020),124f,RuntimeUi.Positive);
             else RuntimeUi.AddButton(panel,"Enter certified Union battle 020","ENTER CERTIFIED UNION BATTLE",()=>EnterPlayableCampaignBattle020(coordinator),124f,RuntimeUi.Accent);
           }
           else RuntimeUi.AddButton(panel,"Resolve operation step 020","RESOLVE COMMITTED STEP",()=>RunGuildCityCommand017D(()=>coordinator.CommitPlayableStep020("SUCCESS")),118f,RuntimeUi.Positive);
         }
       }
       if(!string.IsNullOrWhiteSpace(state.PendingStepReceiptId))RuntimeUi.AddButton(body,"Apply exact-once step receipt 020","APPLY STEP RESULT",()=>RunGuildCityCommand017D(coordinator.ApplyPlayableStep020),125f,RuntimeUi.Positive);
       if(state.Status=="ReadyToFinalize"&&!chapterReceiptReady)RuntimeUi.AddButton(body,"Finalize playable chapter 020","FINALIZE CHAPTER",()=>RunGuildCityCommand017D(coordinator.FinalizePlayableChapter020),128f,RuntimeUi.Accent);
       if(state.Status=="ReadyToFinalize"&&chapterReceiptReady)RuntimeUi.AddButton(body,"Apply chapter result and close operation 020","APPLY CHAPTER RESULT",()=>RunGuildCityCommand017D(coordinator.ApplyPlayableChapterResult020),132f,RuntimeUi.Positive);
     }
     AddMessagePanel(body,"WORLD CONTENT ACTIVATION",string.Join("\n",state.WorldSummaries.Select(w=>(w.GateUnlocked?"OPEN • ":"LOCKED • ")+w.DisplayName+" — enemies "+w.EnemyArchetypes+", recruit origins "+w.RecruitOrigins+", loot entries "+w.LootEntries+", repeatables "+w.RepeatableContracts+", crises "+w.CrisisOperations+" — "+w.StandingTier)),RuntimeUi.ButtonNormal);
     AddMessagePanel(body,"REPEATABLE OPERATIONS",string.Join("\n",state.Repeatables.Select(r=>(r.Unlocked?"OPEN • ":"LOCKED • ")+r.Title+" — "+r.OperationType+" — events "+r.EventCount+", battles "+r.BattleCount)),RuntimeUi.ButtonNormal);
   }
 }
}
