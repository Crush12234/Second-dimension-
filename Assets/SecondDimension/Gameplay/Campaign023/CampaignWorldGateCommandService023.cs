using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.Campaign023
{
  public sealed class CampaignWorldGateCommandService023
  {
   public const int AuthorityChainEntryLimit084=65536;
  public Result<CampaignState> BeginOperation(CampaignState campaign,IWorldGateOperationsCatalog023 catalog,string definitionId,IReadOnlyList<string> alliedUnionIds,bool chapterBoardAuthorized)
  {
   // Compatibility overload: a caller-supplied boolean can never authorize a chapter WORLD_BOARD.
   return BeginOperation(campaign,catalog,definitionId,alliedUnionIds,(ICampaignPlayableCatalog020)null);
  }

  public Result<CampaignState> BeginOperation(CampaignState campaign,IWorldGateOperationsCatalog023 catalog,string definitionId,IReadOnlyList<string> alliedUnionIds,ICampaignPlayableCatalog020 campaignCatalog,HeroMaster300Catalog087 heroCatalog=null)
  {
    if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var runtime,out var error)||catalog==null)return Result<CampaignState>.Failure(error??"CAMPAIGN023_INPUT_REQUIRED");
    if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"CAMPAIGN"))return Result<CampaignState>.Failure("Resume the saved Campaign before starting another route.");
    if(!StringComparer.Ordinal.Equals(runtime.ContentAuthorityVersion,
           WorldGateRuntimeState023.ContentVersion))
     return Result<CampaignState>.Failure(
         "CAMPAIGN023_LEGACY_RECOVERY_REQUIRED");
    if(runtime.ActiveOperation==null)
     runtime=EnsureAuthorityChain084(campaign,city,runtime);
     if(!ValidateStoredCompletionProofs084(campaign,catalog,runtime))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_COMPLETION_LEDGER_INVALID");
    if(runtime.AuthorityEntries.Count>=AuthorityChainEntryLimit084)
     return Result<CampaignState>.Failure(
         "CAMPAIGN023_AUTHORITY_CHAIN_CAP_REACHED");
   if(runtime.ActiveOperation!=null||GuildCityExpeditionService017D.HasUnresolvedOperation(city)||city.PendingEncounter!=null||city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN023_OPERATION_ALREADY_ACTIVE");
   if(!catalog.TryGetBoard(definitionId,out var board))return Result<CampaignState>.Failure("CAMPAIGN023_BOARD_UNKNOWN");
   if(!BoardAdventureRules084.IsCompatible084(board,out var compatibilityError))return Result<CampaignState>.Failure("CAMPAIGN023_BOARD_UNSUPPORTED:"+compatibilityError);
     var chapter=StringComparer.Ordinal.Equals(board.OperationKind,"CHAPTER");
    var chapterAuthority=chapter&&HasCanonicalChapterWorldBoardAuthority(
        campaign,progress,playable,campaignCatalog,definitionId);
     if(chapter&&!chapterAuthority)
      return Result<CampaignState>.Failure("CAMPAIGN023_CHAPTER_WORLD_BOARD_STEP_REQUIRED");
      var unrelatedAdventure=chapterAuthority
          ?GuildCityExpeditionService017D.HasUnresolvedAdventureOutsideChapter084(
              campaign)
          :GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign);
      if(unrelatedAdventure)
       return Result<CampaignState>.Failure(
           "CAMPAIGN023_FINISH_ACTIVE_ADVENTURE_FIRST");
      if(playable.ActiveOperation!=null&&!chapterAuthority)
      return Result<CampaignState>.Failure("CAMPAIGN023_FINISH_STORY_QUEST_FIRST");
     if(StringComparer.Ordinal.Equals(board.OperationKind,"REPEATABLE")&&
        !playable.UnlockedRepeatableContractIds.Contains(definitionId))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_REPEATABLE_CONTRACT_NOT_UNLOCKED");
   if(!WorldUnlocked(progress,playable,runtime,board.WorldId))return Result<CampaignState>.Failure("CAMPAIGN023_WORLD_GATE_LOCKED");
   if(!StringComparer.Ordinal.Equals(runtime.CurrentWorldId,board.WorldId))return Result<CampaignState>.Failure("CAMPAIGN023_TRAVEL_TO_QUEST_WORLD_FIRST");
    var unions=ValidateUnions(campaign,alliedUnionIds);
    if(unions==null||unions.Count==0)
     return Result<CampaignState>.Failure("CAMPAIGN023_ALLIED_UNION_REQUIRED");
    if(unions.Count>board.MaximumAlliedUnions)
     return Result<CampaignState>.Failure("CAMPAIGN023_ALLIED_UNION_CAP_EXCEEDED");
     if(!TryAlliedRosterSnapshot084(campaign,unions,out var rosterIdentity,
            out var alliedRecruitIds))
      return Result<CampaignState>.Failure("CAMPAIGN023_ALLIED_UNION_ROSTER_INVALID");
   var standing=runtime.WorldStandings.FirstOrDefault(x=>x.WorldId==board.WorldId)??new WorldStandingState023(board.WorldId,0,25,0,"RECOVERABLE_FRACTURE");
   var seed=SemanticSeed.Derive(campaign.CampaignSeed,"WORLD_GATE_023",definitionId,(city.OperationOrdinal+1).ToString());var rewardPermille=RewardPermilleAtBegin084(board.OperationKind,definitionId,catalog.RewardPolicy,runtime);var authority=CanonicalJson.Sha256Hex(new{Seed=seed.ToString(),standing.Trust,standing.Tension,standing.CivilianSupport,AlliedUnionIds=unions,RewardPermilleAtBegin=rewardPermille,AlliedRosterIdentity=rosterIdentity,AlliedRecruitIds=alliedRecruitIds,PreviousAuthorityChainHash=runtime.AuthorityChainHash});var hash=CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,definitionId,city.OperationOrdinal,seed=seed.ToString(),authority});var opId="WGOP023_"+hash.Substring(0,24).ToUpperInvariant();var commitId="WGCONTRACT023_"+hash.Substring(0,20).ToUpperInvariant();var expeditionId="WGEXP023_"+hash.Substring(0,20).ToUpperInvariant();
    var deck=new ExpeditionDeckService089().Create(campaign.CampaignSeed,opId,
        board,campaign.Guild.Recruits,campaign.Guild.Inventory,heroCatalog,
        runtime.ExpeditionRecruitLeadIds089,
        campaign.Guild.Development.GuildLevel,
        SssTenV4CampaignAccessor090.Read(campaign).Progression);
   var operation=new WorldGateOperationState023(opId,definitionId,board.BoardId,board.OperationKind,board.WorldId,authority,board.StartNodeId,WorldGateOperationStatus023.Active,catalog.RewardPolicy.TravelSupplyReserveDefault,0,0,0,standing.Trust,standing.Tension,standing.CivilianSupport,unions,Array.Empty<string>(),Array.Empty<string>(),null,string.Empty,"world_gate_023_started",Array.Empty<WorldGateNodeReceipt023>(),standing.Trust,standing.Tension,standing.CivilianSupport,rewardPermille,rosterIdentity,alliedRecruitIds,runtime.AuthorityChainHash,deck);
   var contract=new ContractCommitState017D(commitId,definitionId,board.BoardId,authority,city.OperationOrdinal+1,false,false);
   var expedition=new ExpeditionState017D(expeditionId,commitId,board.BoardId,board.StartNodeId,ExpeditionStatus017D.Active,catalog.RewardPolicy.TravelSupplyReserveDefault,0,0,0,new[]{board.StartNodeId},new[]{board.StartNodeId},Array.Empty<string>(),Array.Empty<CommittedCheckState017D>(),Array.Empty<string>(),"world_gate_023_started");
   city=city.With(operationOrdinal:city.OperationOrdinal+1,activeContract:contract,replaceActiveContract:true,expedition:expedition,replaceExpedition:true,lastCheckpointId:"world_gate_023_started");
    runtime=UpdateStanding(runtime,board.WorldId,standing.Trust,standing.Tension,standing.CivilianSupport,catalog).With(activeOperation:operation,replaceActiveOperation:true,currentWorldId:board.WorldId,activeNodeLedger:Array.Empty<WorldGateNodeReceipt023>(),lastCheckpointId:"world_gate_023_started");
   return Success(campaign,city,strategic,progress,playable,runtime);
  }

  public Result<CampaignState> CommitNodeChoice(CampaignState campaign,IWorldGateOperationsCatalog023 catalog,string choiceId,string actorRecruitId,string assistantRecruitId,int modifier)
  {
   if(!TryActive(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var runtime,out var op,out var board,out var node,out var error))return Result<CampaignState>.Failure(error);
   if(op.PendingReceipt!=null)return Result<CampaignState>.Success(campaign);if(node.RequiresCertifiedBattle)return Result<CampaignState>.Failure("CAMPAIGN023_CERTIFIED_BATTLE_REQUIRED");
   if(!ExpeditionDeckService089.TryAuthorizedWorldGateModifier(op,node.NodeId,
          string.IsNullOrWhiteSpace(choiceId)
              ?((node.ChoiceIds?.Count??0)>0?node.ChoiceIds[0]:"CONTINUE")
              :choiceId,actorRecruitId,assistantRecruitId,out var deckModifier)||
      modifier!=deckModifier)
    return Result<CampaignState>.Failure("CAMPAIGN023_UNSUPPORTED_CHECK_MODIFIER");
    var choices=node.ChoiceIds??Array.Empty<string>();var choice=string.IsNullOrWhiteSpace(choiceId)?(choices.Count>0?choices[0]:"CONTINUE"):choiceId;if((choices.Count>0&&!choices.Contains(choice))||(choices.Count==0&&!StringComparer.Ordinal.Equals(choice,"CONTINUE")))return Result<CampaignState>.Failure("CAMPAIGN023_CHOICE_ILLEGAL");
   if(!BoardAdventureRules084.TryNextNode084(node,choice,out var next))return Result<CampaignState>.Failure("CAMPAIGN023_CHOICE_ROUTE_UNSUPPORTED");var die1=0;var die2=0;var effective=modifier;var outcome="SUCCESS";
    if(node.CheckDifficulty>0)
    {
     if(string.IsNullOrWhiteSpace(actorRecruitId)||!IsCommittedRecruit084(campaign,op,actorRecruitId))return Result<CampaignState>.Failure("CAMPAIGN023_CHECK_ACTOR_REQUIRED");if(!string.IsNullOrWhiteSpace(assistantRecruitId)&&(!IsCommittedRecruit084(campaign,op,assistantRecruitId)||StringComparer.Ordinal.Equals(actorRecruitId,assistantRecruitId)))return Result<CampaignState>.Failure("CAMPAIGN023_CHECK_ASSISTANT_NOT_COMMITTED");
     var seed=SemanticSeed.Derive(campaign.CampaignSeed,op.OperationId,node.NodeId,choice,actorRecruitId,assistantRecruitId??string.Empty);var rng=new Pcg32(seed.Seed,seed.Stream);die1=rng.NextInclusive(1,6);die2=rng.NextInclusive(1,6);if(!string.IsNullOrWhiteSpace(assistantRecruitId)&&!StringComparer.Ordinal.Equals(actorRecruitId,assistantRecruitId))effective=checked(effective+1);var total=die1+die2+effective;outcome=total>=node.CheckDifficulty+4?"EXCEPTIONAL":total>=node.CheckDifficulty+2?"FULL_SUCCESS":total>=node.CheckDifficulty?"SUCCESS_WITH_COST":total>=node.CheckDifficulty-2?"SETBACK":"SEVERE_SETBACK";
    }
    else
    {
     actorRecruitId=string.Empty;
     assistantRecruitId=string.Empty;
    }
   var decay=RewardPermille(op,catalog.RewardPolicy,runtime);var setback=outcome=="SETBACK"||outcome=="SEVERE_SETBACK";var severe=outcome=="SEVERE_SETBACK";
   int Scale(int value)=>Math.Max(0,(value*decay)/10000);
   var supply=node.SupplyDelta-(setback?1:0);var fatigue=node.FatigueDelta+(setback?1:0)+(severe?1:0);var trust=setback?Math.Min(0,node.TrustDelta):node.TrustDelta;var tension=node.TensionDelta+(setback?1:0);var support=setback?Math.Min(0,node.CivilianSupportDelta):node.CivilianSupportDelta;
   var hash=CanonicalJson.Sha256Hex(new{op.OperationId,node.NodeId,choice,next,outcome,die1,die2,effective,op.CanonicalSeedIdentity});var receipt=new WorldGateNodeReceipt023("WGREC023_"+hash.Substring(0,24).ToUpperInvariant(),op.OperationId,node.NodeId,choice,next,outcome,hash,supply,fatigue,node.UrgencyDelta,node.ThreatDelta,trust,tension,support,Scale(node.GuildXp),Scale(node.HallXp),ScaleMaterials084(node.MaterialIds,decay),actorRecruitId,assistantRecruitId,die1,die2,effective,0);
   op=op.With(pendingReceipt:receipt,replacePendingReceipt:true,lastCheckpointId:"world_gate_023_receipt_committed");runtime=runtime.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:"world_gate_023_receipt_committed");return Success(campaign,city,strategic,progress,playable,runtime);
  }

  public Result<CampaignState> ApplyNodeReceiptExactlyOnce(CampaignState campaign,IWorldGateOperationsCatalog023 catalog)
  {
   if(!TryActive(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var runtime,out var op,out var board,out var node,out var error))return Result<CampaignState>.Failure(error);var receipt=op.PendingReceipt;if(receipt==null)return Result<CampaignState>.Failure("CAMPAIGN023_PENDING_RECEIPT_REQUIRED");
   var fate=op.ExpeditionDeck089?.PendingReceipt?.Effect132;
   if(ExpeditionDeckService089.IsRouteFate132(fate)&&
      (!fate.IsRolled132||!new ExpeditionDeckService089().ValidateRouteFatePair132(op)))
    return Result<CampaignState>.Failure("EXPEDITION132_ROLL_ROUTE_FATE_BEFORE_APPLYING");
    if(op.AppliedReceiptIds.Contains(receipt.ReceiptId))
   {
    if(!StringComparer.Ordinal.Equals(receipt.OperationId,op.OperationId)||
       !op.CompletedNodeIds.Contains(receipt.NodeId))
     return Result<CampaignState>.Failure("CAMPAIGN023_STALE_APPLIED_RECEIPT_INVALID");
    op=op.With(pendingReceipt:null,replacePendingReceipt:true,
        lastCheckpointId:"world_gate_023_stale_receipt_cleared");
    runtime=runtime.With(activeOperation:op,replaceActiveOperation:true,
        lastCheckpointId:op.LastCheckpointId);
     return Success(campaign,city,strategic,progress,playable,runtime);
    }
     var globallyClaimed=campaign.Guild.Development.HasClaimedReward(
         receipt.ReceiptId);
     if(campaign.Guild.Development.HasAdventureAuthority(receipt.ReceiptId))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_GLOBAL_NODE_AUTHORITY_ALREADY_APPLIED");
     if(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count>=
        GuildDevelopmentState.AdventureAuthorityEntryLimit)
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_ADVENTURE_AUTHORITY_LEDGER_FULL");
    if(!ValidatePendingReceipt084(campaign,catalog,runtime,op,node,receipt,out var receiptError))
    return Result<CampaignState>.Failure(receiptError);
    var applied=new List<string>(op.AppliedReceiptIds){receipt.ReceiptId};applied.Sort(StringComparer.Ordinal);var proofs=new List<WorldGateNodeReceipt023>(op.AppliedReceipts){receipt};proofs.Sort((a,b)=>StringComparer.Ordinal.Compare(a.NodeId,b.NodeId));var completed=new List<string>(op.CompletedNodeIds);if(!completed.Contains(receipt.NodeId))completed.Add(receipt.NodeId);completed.Sort(StringComparer.Ordinal);
   var supplies=Math.Max(0,op.Supplies+receipt.SupplyDelta);var fatigue=Math.Max(0,op.Fatigue+receipt.FatigueDelta);var urgency=Math.Max(0,op.Urgency+receipt.UrgencyDelta);var threat=Math.Max(0,op.Threat+receipt.ThreatDelta);var trust=Math.Max(-100,Math.Min(100,op.Trust+receipt.TrustDelta));var tension=Math.Max(0,Math.Min(100,op.Tension+receipt.TensionDelta));var support=Math.Max(-100,Math.Min(100,op.CivilianSupport+receipt.CivilianSupportDelta));var isExit=StringComparer.Ordinal.Equals(node.Kind,"EXIT")||string.IsNullOrWhiteSpace(receipt.NextNodeId);var status=isExit?WorldGateOperationStatus023.ReadyToFinalize:WorldGateOperationStatus023.Active;var next=isExit?op.CurrentNodeId:receipt.NextNodeId;
    op=op.With(currentNodeId:next,status:status,supplies:supplies,fatigue:fatigue,urgency:urgency,threat:threat,trust:trust,tension:tension,civilianSupport:support,completedNodeIds:completed.AsReadOnly(),appliedReceiptIds:applied.AsReadOnly(),pendingReceipt:null,replacePendingReceipt:true,lastCheckpointId:isExit?"world_gate_023_ready_to_finalize":"world_gate_023_node_applied",appliedReceipts:proofs.AsReadOnly());
   var expedition=city.Expedition;var visited=new List<string>(expedition.VisitedNodeIds);if(!string.IsNullOrWhiteSpace(next)&&!visited.Contains(next))visited.Add(next);visited.Sort(StringComparer.Ordinal);var revealed=new List<string>(expedition.RevealedNodeIds);if(!string.IsNullOrWhiteSpace(next)&&!revealed.Contains(next))revealed.Add(next);revealed.Sort(StringComparer.Ordinal);var moves=new List<string>(expedition.CommittedMoveIds);if(!moves.Contains(receipt.ReceiptId))moves.Add(receipt.ReceiptId);moves.Sort(StringComparer.Ordinal);expedition=expedition.With(currentNodeId:next,status:isExit?ExpeditionStatus017D.Completed:ExpeditionStatus017D.Active,supplies:supplies,fatigue:fatigue,urgency:urgency,threat:threat,visitedNodeIds:visited.AsReadOnly(),revealedNodeIds:revealed.AsReadOnly(),committedMoveIds:moves.AsReadOnly(),lastCheckpointId:op.LastCheckpointId);
   var materials=globallyClaimed?city.Materials:
       MergeMaterials(city.Materials,receipt.MaterialIds);var memories=MergeMemory(city.RelationshipMemories,receipt,city.OperationOrdinal,node);city=city.With(materials:materials,relationshipMemories:memories,expedition:expedition,replaceExpedition:true,lastCheckpointId:op.LastCheckpointId);
    runtime=UpdateStanding(runtime,op.WorldId,trust,tension,support,catalog);runtime=runtime.With(activeOperation:op,replaceActiveOperation:true,activeNodeLedger:proofs.AsReadOnly(),lastCheckpointId:op.LastCheckpointId);
    var development=campaign.Guild.Development.RecordAdventureAuthority(
        receipt.ReceiptId);if(!globallyClaimed&&receipt.GuildXp>0&&receipt.HallXp>0)development=development.RecordBattleReward(receipt.ReceiptId,receipt.GuildXp,receipt.HallXp);var guild=new GuildState(campaign.Guild.GuildId,checked(campaign.Guild.TreasuryXp+(globallyClaimed?0:receipt.GuildXp)),campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development,city);var updated=campaign.With(guild,campaign.OpeningFlow);return Success(updated,city,strategic,progress,playable,runtime);
  }

  public Result<CampaignState> CommitEncounter(CampaignState campaign,IWorldGateOperationsCatalog023 catalog)
  {
    if(!TryActive(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var runtime,out var op,out var board,out var node,out var error))return Result<CampaignState>.Failure(error);if(!node.RequiresCertifiedBattle)return Result<CampaignState>.Failure("CAMPAIGN023_NODE_NOT_BATTLE");if(op.PendingReceipt!=null)return Result<CampaignState>.Failure("CAMPAIGN023_APPLY_NODE_RECEIPT_FIRST");
    if(city.PendingEncounter!=null)
    {
     var repeated=CreateEncounterRequest084(campaign,city,op,node,city.PendingEncounter.PreBattleStateHash);
     if(op.Status!=WorldGateOperationStatus023.AwaitingBattle||
         city.Expedition.Status!=ExpeditionStatus017D.AwaitingBattle||
         !SameEncounter084(city.PendingEncounter,repeated)||
         !campaign.Guild.Development.HasAdventureAuthority(
             GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(
                 repeated))||
         (city.PendingBattleReturn!=null&&
         (!StringComparer.Ordinal.Equals(city.PendingBattleReturn.LaunchRequestId,repeated.RequestId)||
          !StringComparer.Ordinal.Equals(city.PendingBattleReturn.BattleRunId,repeated.BattleId))))
      return Result<CampaignState>.Failure("CAMPAIGN023_EXISTING_ENCOUNTER_INVALID");
     return Result<CampaignState>.Success(campaign);
    }
    if(city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN023_EXISTING_BATTLE_RETURN_INVALID");
     if(op.Status!=WorldGateOperationStatus023.Active)return Result<CampaignState>.Failure("CAMPAIGN023_NODE_NOT_READY_FOR_BATTLE");
     var request=CreateEncounterRequest084(campaign,city,op,node,CanonicalJson.Sha256Hex(campaign),newCommit094:true);
     var requestAuthority=GuildCityBattleBridgeService017D
         .EncounterRequestAuthorityId084(request);
     var development=campaign.Guild.Development;
     if(development.HasAdventureAuthority(requestAuthority))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_ENCOUNTER_AUTHORITY_ALREADY_COMMITTED");
     if(!development.CanRecordAdventureAuthority(requestAuthority))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_ADVENTURE_AUTHORITY_LEDGER_FULL");
     development=development.RecordAdventureAuthority(requestAuthority);
     campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
         campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,
         development),campaign.OpeningFlow);
    op=op.With(status:WorldGateOperationStatus023.AwaitingBattle,lastCheckpointId:"world_gate_023_awaiting_battle");runtime=runtime.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:op.LastCheckpointId);city=city.With(expedition:city.Expedition.With(status:ExpeditionStatus017D.AwaitingBattle,lastCheckpointId:op.LastCheckpointId),replaceExpedition:true,pendingEncounter:request,replacePendingEncounter:true,lastCheckpointId:op.LastCheckpointId);return Success(campaign,city,strategic,progress,playable,runtime);
  }

  public Result<CampaignState> SynchronizeAfterClaimedBattle(CampaignState campaign,IWorldGateOperationsCatalog023 catalog)
  {
    if(!TryActive(campaign,catalog,out var city,out var strategic,out var progress,
       out var playable,out var runtime,out var op,out var board,out var node,
       out var error))return Result<CampaignState>.Failure(error);
    if(op==null||op.Status!=WorldGateOperationStatus023.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN023_AWAITING_BATTLE_REQUIRED");
   if(city.PendingEncounter!=null||city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN023_BATTLE_RETURN_NOT_APPLIED");
    if(campaign.Battle?.Reward==null||!campaign.Battle.Reward.Claimed)return Result<CampaignState>.Failure("CAMPAIGN023_CLAIM_EXISTING_REWARD_FIRST");
    var encounterHash=CanonicalJson.Sha256Hex(new
    {
     op.OperationId,node.NodeId,op.CanonicalSeedIdentity
    });
    var expectedBattleId="WORLD_GATE_BATTLE_023_"+
        encounterHash.Substring(0,18).ToUpperInvariant();
    if(!StringComparer.Ordinal.Equals(campaign.Battle.BattleId,expectedBattleId)||
       !M2BattleCommandService.HasValidFinalStateHash090(campaign.Battle)||
       !campaign.Guild.Development.HasClaimedReward(
           campaign.Battle.Reward.RewardId))
     return Result<CampaignState>.Failure("CAMPAIGN023_BATTLE_AUTHORITY_INVALID");
    if(op.PendingReceipt!=null)return Result<CampaignState>.Success(campaign);
     if(node==null||!node.RequiresCertifiedBattle)return Result<CampaignState>.Failure("CAMPAIGN023_BATTLE_NODE_REQUIRED");
    var replayRequest=CreateEncounterRequest084(campaign,city,op,node,
        "WORLD_GATE_023_BATTLE_RETURN_REPLAY");
    if(!GuildCityBattleBridgeService017D.TryCreateCanonicalBattleReturn084(
           campaign.Battle,replayRequest,city.OperationOrdinal,
           out var canonicalReturn,out _)||
       city.AppliedBattleReturnIds.Count(value=>StringComparer.Ordinal.Equals(
           value,canonicalReturn.ReceiptId))!=1)
     return Result<CampaignState>.Failure(
         "CAMPAIGN023_BATTLE_RETURN_AUTHORITY_INVALID");
    var battleReturnAuthority=GuildCityBattleBridgeService017D
        .BattleReturnApplyAuthorityId084(canonicalReturn);
    if(!campaign.Guild.Development.HasAdventureAuthority(
           battleReturnAuthority))
     return Result<CampaignState>.Failure(
         "CAMPAIGN023_BATTLE_RETURN_AUTHORITY_INVALID");
   var choice=(node.ChoiceIds??Array.Empty<string>()).FirstOrDefault()??"ENGAGE";
   if(!BoardAdventureRules084.TryNextNode084(node,choice,out var next))return Result<CampaignState>.Failure("CAMPAIGN023_CHOICE_ROUTE_UNSUPPORTED");
   var victory=campaign.Battle.Outcome==BattleOutcome.Victory;
   var hash=CanonicalJson.Sha256Hex(new
   {
    op.OperationId,
    node.NodeId,
    Choice=choice,
    Next=next,
    BattleOutcome=campaign.Battle.Outcome.ToString(),
     campaign.Battle.FinalStateHash,
     ExistingReward=campaign.Battle.Reward.RewardId,
     BattleReturnAuthority=battleReturnAuthority,
     op.CanonicalSeedIdentity
   });
   var rewardPermille=RewardPermille(op,catalog.RewardPolicy,runtime);
   int ScaleReward(int value)=>Math.Max(0,(value*rewardPermille)/10000);
   var receipt=new WorldGateNodeReceipt023(
       "WGREC023_"+hash.Substring(0,24).ToUpperInvariant(),
       op.OperationId,node.NodeId,choice,next,
       campaign.Battle.Outcome.ToString().ToUpperInvariant(),hash,
       node.SupplyDelta,
       victory?node.FatigueDelta:Math.Max(2,node.FatigueDelta+1),
       node.UrgencyDelta,
        victory?node.ThreatDelta:Math.Max(-1,node.ThreatDelta),
       victory?node.TrustDelta:Math.Min(0,node.TrustDelta),
       victory?node.TensionDelta:Math.Max(0,node.TensionDelta),
       victory?node.CivilianSupportDelta:Math.Min(0,node.CivilianSupportDelta),
       victory?ScaleReward(node.GuildXp):0,
       victory?ScaleReward(node.HallXp):0,
       victory?ScaleMaterials084(node.MaterialIds,rewardPermille):Array.Empty<string>(),
       string.Empty,string.Empty,0,0,0,0,battleReturnAuthority);
   op=op.With(status:WorldGateOperationStatus023.Active,
       pendingReceipt:receipt,replacePendingReceipt:true,
       existingBattleRewardReceiptId:campaign.Battle.Reward.RewardId,
       lastCheckpointId:"world_gate_023_battle_tile_committed");
    var expedition=city.Expedition.With(status:ExpeditionStatus017D.Active,
        supplies:op.Supplies,fatigue:op.Fatigue,urgency:op.Urgency,
        threat:op.Threat,lastCheckpointId:op.LastCheckpointId);
   city=city.With(expedition:expedition,replaceExpedition:true,lastCheckpointId:op.LastCheckpointId);
   runtime=runtime.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:op.LastCheckpointId);
   return Success(campaign,city,strategic,progress,playable,runtime);
  }

  public bool HasCanonicalPendingEncounter084(CampaignState campaign,IWorldGateOperationsCatalog023 catalog)
  {
   return TryActive(campaign,catalog,out var city,out _,out _,out _,out _,
              out var op,out _,out _,out _)&&
          op.Status==WorldGateOperationStatus023.AwaitingBattle&&
          city.PendingEncounter!=null;
  }

   public Result<CampaignState> FinalizeOperation(CampaignState campaign,IWorldGateOperationsCatalog023 catalog)
   {
     if(!TryActive(campaign,catalog,out var city,out var strategic,out var progress,
        out var playable,out var runtime,out var op,out var board,out var node,
        out var error))return Result<CampaignState>.Failure(error);
     if(op.Status!=WorldGateOperationStatus023.ReadyToFinalize)
      return Result<CampaignState>.Failure("CAMPAIGN023_OPERATION_NOT_READY");
     var completed=new List<string>(runtime.CompletedDefinitionIds);
     if(!completed.Contains(op.DefinitionId))completed.Add(op.DefinitionId);
     completed.Sort(StringComparer.Ordinal);
     var isRepeat=StringComparer.Ordinal.Equals(op.OperationKind,"REPEATABLE");
     var repeatRewardPermille=RewardPermille(op,catalog.RewardPolicy,runtime);
     var streak=isRepeat&&StringComparer.Ordinal.Equals(
         runtime.RepeatableStreakDefinitionId,op.DefinitionId)
         ?runtime.RepeatableStreakCount+1:isRepeat?1:0;
     var streakId=isRepeat?op.DefinitionId:string.Empty;
     var origins=new List<string>(runtime.UnlockedRecruitOriginIds);
     if(catalog.TryGetRecruitUnlock(op.WorldId,out var unlock)&&
        StandingIndex(TierFor(op.Trust,op.Tension,op.CivilianSupport,catalog,
             op.WorldId),catalog,op.WorldId)>=
        StandingIndex(unlock.MinimumStandingTier,catalog,op.WorldId))
      foreach(var id in unlock.OriginProfileIds)
       if(!origins.Contains(id))origins.Add(id);
     origins.Sort(StringComparer.Ordinal);
     var worldIds=new List<string>(runtime.UnlockedWorldIds);
     if(!worldIds.Contains(op.WorldId))worldIds.Add(op.WorldId);
     worldIds.Sort(StringComparer.Ordinal);
     var finalizationProof="world_gate_023_operation_finalized:"+op.OperationId;
     var completionProof=CreateCompletionProof084(
         campaign,board,op,CampaignRecoveryCommands150.CommittedWorldOrdinal(campaign),
         catalog.RewardPolicy.TravelSupplyReserveDefault);
     if(completionProof==null||
        !ValidateCompletionProof084(campaign,catalog,completionProof))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_COMPLETION_PROOF_INVALID");
     var retainedProofs=new List<WorldGateCompletionProof023>(
         runtime.CompletionProofs);
     if(StringComparer.Ordinal.Equals(op.OperationKind,"CHAPTER"))
     {
      var previousProof130=retainedProofs.SingleOrDefault(value=>
          StringComparer.Ordinal.Equals(value.DefinitionId,op.DefinitionId));
      if(previousProof130!=null)
      {
       // The full old proof remains in the immutable authority chain. This
       // bounded list is only the latest proof for each authored chapter.
       if((progress.Replay130==null&&CampaignRunRecoveryCommands151.ActiveRun(progress)?.HasCurrentRun!=true)||
          !CampaignReplayRules130.IsCurrentCycleProof(progress,completionProof)||
          CampaignReplayRules130.IsCurrentCycleProof(progress,previousProof130)||
          CampaignReplayRules130.CurrentCompleted(progress).Contains(op.DefinitionId))
        return Result<CampaignState>.Failure(
            "CAMPAIGN023_CHAPTER_COMPLETION_LEDGER_CONFLICT");
       retainedProofs.Remove(previousProof130);
      }
      else if(retainedProofs.Count>=82)
       return Result<CampaignState>.Failure(
           "CAMPAIGN023_CHAPTER_COMPLETION_LEDGER_CONFLICT");
      retainedProofs.Add(completionProof);
      retainedProofs.Sort((left,right)=>StringComparer.Ordinal.Compare(
          left.DefinitionId,right.DefinitionId));
     }
      var travelReward=isRepeat
         ?(catalog.RewardPolicy.TravelSupplyRewardPerCompletedOperation*
             repeatRewardPermille)/10000
          :catalog.RewardPolicy.TravelSupplyRewardPerCompletedOperation;
       var travelSuppliesAfter=runtime.TravelSupplies+travelReward;
       var authorityEntry=CreateAuthorityEntry084(runtime,completionProof,
           completed.AsReadOnly(),origins.AsReadOnly(),worldIds.AsReadOnly(),
           travelSuppliesAfter,streakId,streak);
       if(authorityEntry==null||runtime.AuthorityEntries.Count>=
              AuthorityChainEntryLimit084)
       return Result<CampaignState>.Failure(
           "CAMPAIGN023_AUTHORITY_CHAIN_CAP_REACHED");
      var authorityEntries=new List<WorldGateAuthorityEntry023>(
          runtime.AuthorityEntries){authorityEntry};
      runtime=runtime.With(activeOperation:null,replaceActiveOperation:true,
         currentWorldId:op.WorldId,unlockedWorldIds:worldIds.AsReadOnly(),
         completedDefinitionIds:completed.AsReadOnly(),
         unlockedRecruitOriginIds:origins.AsReadOnly(),
         repeatableStreakDefinitionId:streakId,repeatableStreakCount:streak,
          travelSupplies:travelSuppliesAfter,
          lastCheckpointId:finalizationProof,lastCompletionProof:completionProof,
          replaceLastCompletionProof:true,
          completionProofs:retainedProofs.AsReadOnly(),
          authorityEntries:authorityEntries.AsReadOnly(),
          authorityChainHash:authorityEntry.EntryHash,
          activeNodeLedger:Array.Empty<WorldGateNodeReceipt023>());
     var gates=new List<WorldGateProgress020>(playable.WorldGates);
     var existing=gates.FirstOrDefault(x=>x.WorldId==op.WorldId);
     if(existing!=null)
     {
      gates.Remove(existing);
      gates.Add(existing.With(unlocked:true,visitCount:existing.VisitCount+1,
          standingTier:TierFor(op.Trust,op.Tension,op.CivilianSupport,catalog,
              op.WorldId)));
     }
     else
      gates.Add(new WorldGateProgress020(op.WorldId,true,1,
          TierFor(op.Trust,op.Tension,op.CivilianSupport,catalog,op.WorldId),0));
     gates.Sort((a,b)=>StringComparer.Ordinal.Compare(a.WorldId,b.WorldId));
     playable=playable.With(worldGates:gates.AsReadOnly(),worldGate023:runtime,
         replaceWorldGate023:true,lastCheckpointId:runtime.LastCheckpointId);
     progress=progress.With(playable020:playable,replacePlayable020:true,
         lastCheckpointId:runtime.LastCheckpointId);
     strategic=strategic.With(campaign019:progress,replaceCampaign019:true,
         lastCheckpointId:runtime.LastCheckpointId);
     city=city.With(activeContract:null,replaceActiveContract:true,
         expedition:null,replaceExpedition:true,
         lastCheckpointId:runtime.LastCheckpointId);
     var finalized=campaign.With(
          campaign.Guild.WithGuildCity(city.With(strategic017H:strategic,
              replaceStrategic017H:true)),campaign.OpeningFlow);
     return SssTenV4ProgressionService090.RecordCampaignCompletion090(
          finalized,
          SssTenV4ProgressionService090.CampaignCompletionKey090(
              op.DefinitionId));
   }

   public Result<CampaignState> Travel(CampaignState campaign,IWorldGateOperationsCatalog023 catalog,string worldId,ICampaignPlayableCatalog020 campaignCatalog=null)
  {
    if(!TryContext(campaign,out var city,out var strategic,out var progress,
       out var playable,out var runtime,out var error)||catalog==null)
     return Result<CampaignState>.Failure(error??"CAMPAIGN023_INPUT_REQUIRED");
     if(!StringComparer.Ordinal.Equals(runtime.ContentAuthorityVersion,
            WorldGateRuntimeState023.ContentVersion))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_LEGACY_RECOVERY_REQUIRED");
    if(runtime.ActiveOperation==null)
     runtime=EnsureAuthorityChain084(campaign,city,runtime);
     if(!ValidateStoredCompletionProofs084(campaign,catalog,runtime))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_COMPLETION_LEDGER_INVALID");
     if(runtime.AuthorityEntries.Count>=AuthorityChainEntryLimit084)
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_AUTHORITY_CHAIN_CAP_REACHED");
     var chapterOperation=playable.ActiveOperation;
     var canonicalChapterTravel=chapterOperation!=null&&
         StringComparer.Ordinal.Equals(chapterOperation.WorldId,worldId)&&
         HasCanonicalChapterWorldBoardAuthority(campaign,progress,playable,
             campaignCatalog,chapterOperation.ChapterId);
     var unrelatedAdventure=canonicalChapterTravel
         ?GuildCityExpeditionService017D.HasUnresolvedAdventureOutsideChapter084(
             campaign)
         :GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign);
     if(unrelatedAdventure)
      return Result<CampaignState>.Failure("CAMPAIGN023_TRAVEL_BLOCKED_BY_OPERATION");
     if(StringComparer.Ordinal.Equals(worldId,runtime.CurrentWorldId))
      return Success(campaign,city,strategic,progress,playable,runtime);
     if(StringComparer.Ordinal.Equals(worldId,"SKYHOME"))
     {
      runtime=AppendTravelAuthority084(runtime,city.OperationOrdinal,
          "SKYHOME",0,"world_gate_023_returned_skyhome");
      if(runtime==null)return Result<CampaignState>.Failure(
          "CAMPAIGN023_AUTHORITY_CHAIN_CAP_REACHED");
       return Success(campaign,city,strategic,progress,playable,runtime);
    }
    if(!catalog.TryGetTravel(worldId,out var travel))
     return Result<CampaignState>.Failure("CAMPAIGN023_TRAVEL_ROUTE_UNKNOWN");
    if(!WorldUnlocked(progress,playable,runtime,worldId))
     return Result<CampaignState>.Failure("CAMPAIGN023_WORLD_GATE_LOCKED");
    if(runtime.TravelSupplies<travel.SupplyCost)
     return Result<CampaignState>.Failure("CAMPAIGN023_TRAVEL_SUPPLIES_REQUIRED");
     runtime=AppendTravelAuthority084(runtime,city.OperationOrdinal,worldId,
         travel.SupplyCost,"world_gate_023_travel_"+worldId);
     if(runtime==null)return Result<CampaignState>.Failure(
         "CAMPAIGN023_AUTHORITY_CHAIN_CAP_REACHED");
     return Success(campaign,city,strategic,progress,playable,runtime);
   }

   public Result<CampaignState> AbortUnverifiableLegacyOperation084(
        CampaignState campaign)
   {
    if(!TryContext(campaign,out var city,out var strategic,out var progress,
       out var playable,out var runtime,out var error))
     return Result<CampaignState>.Failure(error);
     if(!RequiresLegacyRecovery084(runtime))
      return Result<CampaignState>.Failure(
           "CAMPAIGN023_LEGACY_RECOVERY_NOT_REQUIRED");
      if(RequiresLegacyMigration084(runtime))
       return Result<CampaignState>.Failure(
           "CAMPAIGN023_LEGACY_NODE_LEDGER_MIGRATION_REQUIRED");
      var legacy=runtime.ActiveOperation;
      var baseOrdinal=city.OperationOrdinal;
      if(legacy!=null)
      {
       if(!HasCanonicalLegacyHandoff084(city,legacy))
        return Result<CampaignState>.Failure(
            "CAMPAIGN023_LEGACY_RECOVERY_HANDOFF_LINK_UNPROVEN");
       runtime=UpdateStanding(runtime,legacy.WorldId,legacy.InitialTrust,
           legacy.InitialTension,legacy.InitialCivilianSupport,null);
       baseOrdinal=Math.Max(0,city.OperationOrdinal-1);
       city=city.With(operationOrdinal:baseOrdinal,activeContract:null,
           replaceActiveContract:true,expedition:null,replaceExpedition:true,
           pendingEncounter:null,replacePendingEncounter:true,
           pendingBattleReturn:null,replacePendingBattleReturn:true,
           lastCheckpointId:"world_gate_023_legacy_run_recovered_no_reward");
      }
      else if(GuildCityExpeditionService017D.HasUnresolvedOperation(city)||
              city.PendingEncounter!=null||city.PendingBattleReturn!=null)
       return Result<CampaignState>.Failure(
           "CAMPAIGN023_LEGACY_RECOVERY_HANDOFF_LINK_UNPROVEN");
      runtime=CreateRecoveredLegacyRuntime084(campaign,runtime,baseOrdinal,
          "world_gate_023_legacy_run_recovered_no_reward");
      if(runtime==null)return Result<CampaignState>.Failure(
          "CAMPAIGN023_LEGACY_RECOVERY_STATE_INVALID");
    return Success(campaign,city,strategic,progress,playable,runtime);
   }

   public static bool HasCanonicalLegacyHandoff084(GuildCityState017D city,
       WorldGateOperationState023 operation)
   {
    if(!HasCanonicalLegacyExpeditionMirror104(city,operation))return false;
    var expedition=city.Expedition;
    var request=city.PendingEncounter;var battleReturn=city.PendingBattleReturn;
    if(request==null)return battleReturn==null;
    if(!StringComparer.Ordinal.Equals(request.ContractId,operation.DefinitionId)||
       !StringComparer.Ordinal.Equals(request.ExpeditionId,
           expedition.ExpeditionId)||
       !StringComparer.Ordinal.Equals(request.BoardId,operation.BoardId)||
       !StringComparer.Ordinal.Equals(request.NodeId,operation.CurrentNodeId)||
       !StringComparer.Ordinal.Equals(request.CanonicalSeedIdentity,
           operation.CanonicalSeedIdentity))return false;
    return battleReturn==null||
           (StringComparer.Ordinal.Equals(battleReturn.LaunchRequestId,
                request.RequestId)&&
            StringComparer.Ordinal.Equals(battleReturn.BattleRunId,
                request.BattleId));
   }

   // Shared identity only. Authored-node and optional-card encounters retain
   // their separate exact request validators; neither may relax this mirror.
   public static bool HasCanonicalLegacyExpeditionMirror104(GuildCityState017D city,
       WorldGateOperationState023 operation)
   {
    if(city==null||operation==null)return false;
    var contract=city.ActiveContract;var expedition=city.Expedition;
    if(contract==null||expedition==null||contract.Completed||contract.Failed||
       !StringComparer.Ordinal.Equals(contract.ContractId,operation.DefinitionId)||
       !StringComparer.Ordinal.Equals(contract.BoardId,operation.BoardId)||
       !StringComparer.Ordinal.Equals(contract.CanonicalSeedIdentity,
           operation.CanonicalSeedIdentity)||
       contract.AcceptedOperationOrdinal!=city.OperationOrdinal||
       !StringComparer.Ordinal.Equals(expedition.ContractCommitId,
           contract.CommitId)||
       !StringComparer.Ordinal.Equals(expedition.BoardId,operation.BoardId)||
       !StringComparer.Ordinal.Equals(expedition.CurrentNodeId,
           operation.CurrentNodeId))return false;
    return true;
   }

    public static bool RequiresLegacyRecovery084(WorldGateRuntimeState023 runtime)
    {
      if(RequiresLegacyMigration084(runtime))return true;
      if(runtime==null||!IsRecoverableLegacyRuntime084(runtime))return false;
     var operation=runtime.ActiveOperation;
     return operation==null||operation.RewardPermilleAtBegin<0||
            string.IsNullOrWhiteSpace(operation.AlliedRosterIdentity)||
            string.IsNullOrWhiteSpace(operation.PreviousAuthorityChainHash)||
            operation.AlliedRecruitIds.Count==0||
            operation.AppliedReceipts.Count!=operation.AppliedReceiptIds.Count||
            operation.AppliedReceipts.Count!=operation.CompletedNodeIds.Count;
    }

    public static bool RequiresLegacyMigration084(WorldGateRuntimeState023 runtime) =>
        runtime!=null&&StringComparer.Ordinal.Equals(runtime.ContentAuthorityVersion,
            "CAMPAIGN_WORLD_GATE_EXPEDITION_RUNTIME_023_2.2");

    public Result<CampaignState> MigrateLegacyNodeLedger084(CampaignState campaign,
        IWorldGateOperationsCatalog023 catalog)
    {
     if(!TryContext(campaign,out var city,out var strategic,out var progress,
        out var playable,out var runtime,out var error)||catalog==null)
      return Result<CampaignState>.Failure(error??"CAMPAIGN023_INPUT_REQUIRED");
     if(!RequiresLegacyMigration084(runtime))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_LEGACY_NODE_LEDGER_MIGRATION_NOT_REQUIRED");
     var legacyReceiptIds=runtime.CompletionProofs
         .Where(value=>value!=null)
         .SelectMany(value=>value.AppliedReceipts??
             Array.Empty<WorldGateNodeReceipt023>())
         .Concat(runtime.ActiveOperation?.AppliedReceipts??
             Array.Empty<WorldGateNodeReceipt023>())
         .Where(value=>value!=null).Select(value=>value.ReceiptId)
         .Distinct(StringComparer.Ordinal).OrderBy(value=>value,StringComparer.Ordinal)
         .ToArray();
     var development=campaign.Guild.Development;
     if(development.AppliedAdventureAuthorityIds.Count+legacyReceiptIds.Count(value=>
            !development.HasAdventureAuthority(value))>
        GuildDevelopmentState.AdventureAuthorityEntryLimit)
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_ADVENTURE_AUTHORITY_LEDGER_FULL");
     for(var index=0;index<legacyReceiptIds.Length;index++)
      development=development.RecordAdventureAuthority(legacyReceiptIds[index]);
     campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
         campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,
         development),campaign.OpeningFlow);
     var migrated=new WorldGateRuntimeState023(
         WorldGateRuntimeState023.ContentVersion,runtime.ActiveOperation,
         runtime.CurrentWorldId,runtime.UnlockedWorldIds,runtime.WorldStandings,
         runtime.CompletedDefinitionIds,runtime.UnlockedRecruitOriginIds,
         runtime.RepeatableStreakDefinitionId,runtime.RepeatableStreakCount,
         runtime.TravelSupplies,"world_gate_023_node_ledger_migrated",
         runtime.LastCompletionProof,runtime.CompletionProofs,
         runtime.AuthorityChainBaseHash,runtime.AuthorityChainBaseOperationOrdinal,
         runtime.AuthorityChainBaseStandings,runtime.AuthorityChainBaseUnlockedOrigins,
         runtime.AuthorityChainBaseUnlockedWorlds,
         runtime.AuthorityChainBaseCompletedDefinitions,runtime.AuthorityEntries,
         runtime.AuthorityChainHash,runtime.AuthorityChainBaseCurrentWorldId,
         runtime.AuthorityChainBaseTravelSupplies,
         runtime.AuthorityChainBaseRepeatableStreakDefinitionId,
         runtime.AuthorityChainBaseRepeatableStreakCount,
         runtime.AuthorityChainMigrationReceiptId,
         runtime.ActiveOperation?.AppliedReceipts??
             Array.Empty<WorldGateNodeReceipt023>(),runtime.LegacyOperationArchives,
         runtime.ExpeditionRecruitLeadIds089,
         runtime.ExpeditionDeckTutorialSeen089);
     if(!ValidateStoredCompletionProofs084(campaign,catalog,migrated))
      return Result<CampaignState>.Failure(
          "CAMPAIGN023_LEGACY_NODE_LEDGER_MIGRATION_INVALID");
     if(migrated.ActiveOperation!=null)
     {
      if(!catalog.TryGetBoard(migrated.ActiveOperation.DefinitionId,out var board)||
         !ValidateActiveOperation084(campaign,catalog,city,migrated,
             migrated.ActiveOperation,board))
       return Result<CampaignState>.Failure(
           "CAMPAIGN023_LEGACY_NODE_LEDGER_MIGRATION_INVALID");
     }
     return Success(campaign,city,strategic,progress,playable,migrated);
    }

    static bool IsRecoverableLegacyRuntime084(WorldGateRuntimeState023 runtime)
    {
     if(runtime==null||
        !(StringComparer.Ordinal.Equals(runtime.ContentAuthorityVersion,
             "CAMPAIGN_WORLD_GATE_EXPEDITION_RUNTIME_023_1.0")||
          StringComparer.Ordinal.Equals(runtime.ContentAuthorityVersion,
             "CAMPAIGN_WORLD_GATE_EXPEDITION_RUNTIME_023_2.1")))return false;
     return runtime.AuthorityEntries.Count==0&&
            string.IsNullOrWhiteSpace(runtime.AuthorityChainBaseHash)&&
            string.IsNullOrWhiteSpace(runtime.AuthorityChainHash)&&
            string.IsNullOrWhiteSpace(runtime.AuthorityChainMigrationReceiptId);
    }

    public static WorldGateRuntimeState023 CreateRecoveredLegacyRuntime084(
        CampaignState campaign,WorldGateRuntimeState023 source,int baseOrdinal,
        string checkpoint)
    {
     if(campaign==null||source==null||baseOrdinal<0||
        !IsRecoverableLegacyRuntime084(source))return null;
     var standings=source.WorldStandings;
     var origins=source.UnlockedRecruitOriginIds;
     var worlds=source.UnlockedWorldIds;
     var completed=source.CompletedDefinitionIds;
     var migrationReceipt=MigrationReceiptId084(campaign,baseOrdinal,standings,
         origins,worlds,completed,source.CurrentWorldId,source.TravelSupplies,
         source.RepeatableStreakDefinitionId,source.RepeatableStreakCount);
      var root=AuthorityChainRootHash084(campaign,baseOrdinal,standings,origins,
          worlds,completed,source.CurrentWorldId,source.TravelSupplies,
          source.RepeatableStreakDefinitionId,source.RepeatableStreakCount,
          migrationReceipt);
     return new WorldGateRuntimeState023(WorldGateRuntimeState023.ContentVersion,
         null,source.CurrentWorldId,worlds,standings,completed,origins,
         source.RepeatableStreakDefinitionId,source.RepeatableStreakCount,
         source.TravelSupplies,checkpoint,null,
         Array.Empty<WorldGateCompletionProof023>(),root,baseOrdinal,standings,
         origins,worlds,completed,Array.Empty<WorldGateAuthorityEntry023>(),root,
          source.CurrentWorldId,source.TravelSupplies,
          source.RepeatableStreakDefinitionId,source.RepeatableStreakCount,
          migrationReceipt,
          expeditionRecruitLeadIds089:source.ExpeditionRecruitLeadIds089,
          expeditionDeckTutorialSeen089:source.ExpeditionDeckTutorialSeen089);
    }

   static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,CampaignProgressState019 progress,CampaignPlayableState020 playable,WorldGateRuntimeState023 runtime){playable=playable.With(worldGate023:runtime,replaceWorldGate023:true,lastCheckpointId:runtime.LastCheckpointId);progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:runtime.LastCheckpointId);strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:runtime.LastCheckpointId);city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:runtime.LastCheckpointId);return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));}
   static WorldGateRuntimeState023 ReissueRuntime084(
       WorldGateRuntimeState023 source,WorldGateOperationState023 activeOperation,
       string checkpoint) => new WorldGateRuntimeState023(
           WorldGateRuntimeState023.ContentVersion,activeOperation,
           source.CurrentWorldId,source.UnlockedWorldIds,source.WorldStandings,
           source.CompletedDefinitionIds,source.UnlockedRecruitOriginIds,
            source.RepeatableStreakDefinitionId,source.RepeatableStreakCount,
            source.TravelSupplies,checkpoint,source.LastCompletionProof,
            source.CompletionProofs,source.AuthorityChainBaseHash,
            source.AuthorityChainBaseOperationOrdinal,
            source.AuthorityChainBaseStandings,
            source.AuthorityChainBaseUnlockedOrigins,
             source.AuthorityChainBaseUnlockedWorlds,
             source.AuthorityChainBaseCompletedDefinitions,
             source.AuthorityEntries,source.AuthorityChainHash,
             source.AuthorityChainBaseCurrentWorldId,
             source.AuthorityChainBaseTravelSupplies,
             source.AuthorityChainBaseRepeatableStreakDefinitionId,
             source.AuthorityChainBaseRepeatableStreakCount,
           source.AuthorityChainMigrationReceiptId,
           activeNodeLedger:source.ActiveNodeLedger,
           legacyOperationArchives:source.LegacyOperationArchives,
           expeditionRecruitLeadIds089:source.ExpeditionRecruitLeadIds089,
           expeditionDeckTutorialSeen089:source.ExpeditionDeckTutorialSeen089);
  static bool HasCanonicalChapterWorldBoardAuthority(CampaignState campaign,CampaignProgressState019 progress,CampaignPlayableState020 playable,ICampaignPlayableCatalog020 campaignCatalog,string definitionId)
  {
   if(campaign==null||progress==null||playable==null||campaignCatalog==null||!StringComparer.Ordinal.Equals(progress.ActiveChapterId,definitionId)||progress.PendingReceipt!=null)return false;
   var operation=playable.ActiveOperation;if(operation==null||operation.Status!=CampaignPlayableOperationStatus020.Active||operation.PendingReceipt!=null||!StringComparer.Ordinal.Equals(operation.ChapterId,definitionId))return false;
   if(!campaignCatalog.TryGetBlueprint(definitionId,out var blueprint)||blueprint==null)return false;
   blueprint=CampaignReplayBattle134.Effective(campaign,blueprint);
   if(!StringComparer.Ordinal.Equals(operation.BlueprintId,blueprint.BlueprintId)||!StringComparer.Ordinal.Equals(operation.WorldId,blueprint.WorldId)||operation.CurrentStepIndex<0||operation.CurrentStepIndex>=blueprint.Steps.Count)return false;
   var step=blueprint.Steps[operation.CurrentStepIndex];if(step==null||!StringComparer.Ordinal.Equals(step.Kind,"WORLD_BOARD")||operation.CompletedStepIds.Contains(step.StepId))return false;
     return CampaignPlayableCommandService020.HasCanonicalActiveOperation084(
         campaign,progress,operation,blueprint,campaignCatalog);
  }
  static bool TryContext(CampaignState c,out GuildCityState017D city,out GuildCityStrategicState017H strategic,out CampaignProgressState019 progress,out CampaignPlayableState020 playable,out WorldGateRuntimeState023 runtime,out string error){city=null;strategic=null;progress=null;playable=null;runtime=null;error="";if(c?.Guild?.GuildCity==null){error="CAMPAIGN023_CAMPAIGN_REQUIRED";return false;}city=c.Guild.GuildCity;strategic=city.Strategic017H??GuildCityStrategicState017H.Default();progress=strategic.Campaign019??CampaignProgressState019.Default();playable=progress.Playable020??CampaignPlayableState020.Default();runtime=playable.WorldGate023??WorldGateRuntimeState023.Default();return true;}
   public static bool ValidateActiveAuthority093(CampaignState campaign,
       IWorldGateOperationsCatalog023 catalog,out string error) =>
       TryActive(campaign,catalog,out _,out _,out _,out _,out _,out _,out _,out _,out error);

   static bool TryActive(CampaignState c,IWorldGateOperationsCatalog023 catalog,out GuildCityState017D city,out GuildCityStrategicState017H strategic,out CampaignProgressState019 progress,out CampaignPlayableState020 playable,out WorldGateRuntimeState023 runtime,out WorldGateOperationState023 op,out WorldGateBoardRule023 board,out WorldGateNodeRule023 node,out string error)
   {
    op=null;board=null;node=null;
     if(!TryContext(c,out city,out strategic,out progress,out playable,out runtime,out error)||catalog==null){error=error??"CAMPAIGN023_INPUT_REQUIRED";return false;}
      op=runtime.ActiveOperation;if(op==null){error="CAMPAIGN023_ACTIVE_OPERATION_REQUIRED";return false;}
      if(RequiresLegacyRecovery084(runtime))
      {error="CAMPAIGN023_LEGACY_OPERATION_RECOVERY_REQUIRED";return false;}
      if(!ValidateStoredCompletionProofs084(c,catalog,runtime))
      {error="CAMPAIGN023_COMPLETION_LEDGER_INVALID";return false;}
    if(!catalog.TryGetBoard(op.DefinitionId,out board)){error="CAMPAIGN023_BOARD_UNKNOWN";return false;}
    if(!BoardAdventureRules084.IsCompatible084(board,out var compatibilityError)){error="CAMPAIGN023_BOARD_UNSUPPORTED:"+compatibilityError;return false;}
     if(!ValidateActiveOperation084(c,catalog,city,runtime,op,board))
    {
     error="CAMPAIGN023_ACTIVE_OPERATION_AUTHORITY_INVALID";
     return false;
    }
     var currentNodeId=op.CurrentNodeId;
     node=board.Nodes.FirstOrDefault(x=>x.NodeId==currentNodeId);
    if(node==null){error="CAMPAIGN023_NODE_UNKNOWN";return false;}
    return true;
   }
    static bool ValidateActiveOperation084(CampaignState campaign,IWorldGateOperationsCatalog023 catalog,GuildCityState017D city,WorldGateRuntimeState023 runtime,WorldGateOperationState023 op,WorldGateBoardRule023 board)
   {
    if(campaign==null||city==null||runtime==null||op==null||board==null||
       city.OperationOrdinal<=0||
       !StringComparer.Ordinal.Equals(op.DefinitionId,board.DefinitionId)||
       !StringComparer.Ordinal.Equals(op.BoardId,board.BoardId)||
       !StringComparer.Ordinal.Equals(op.OperationKind,board.OperationKind)||
       !StringComparer.Ordinal.Equals(op.WorldId,board.WorldId)||
       !StringComparer.Ordinal.Equals(runtime.CurrentWorldId,op.WorldId))return false;
    var committedOrdinal=CampaignRecoveryCommands150.CommittedWorldOrdinal(campaign);
    var seed=SemanticSeed.Derive(campaign.CampaignSeed,"WORLD_GATE_023",
        op.DefinitionId,committedOrdinal.ToString());
    var definitionId=op.DefinitionId;
     var expectedRewardPermille=RewardPermilleAtBegin084(board.OperationKind,
         op.DefinitionId,catalog.RewardPolicy,runtime);
     if(!TryAlliedRosterSnapshot084(campaign,op.AlliedUnionIds,
            out var currentRosterIdentity,out var currentRecruitIds))return false;
     var expectedAuthority=CanonicalJson.Sha256Hex(new
     {
       Seed=seed.ToString(),Trust=op.InitialTrust,Tension=op.InitialTension,
       CivilianSupport=op.InitialCivilianSupport,AlliedUnionIds=op.AlliedUnionIds,
       RewardPermilleAtBegin=op.RewardPermilleAtBegin,
       AlliedRosterIdentity=op.AlliedRosterIdentity,
       AlliedRecruitIds=op.AlliedRecruitIds,
       PreviousAuthorityChainHash=op.PreviousAuthorityChainHash
      });
     var hash=CanonicalJson.Sha256Hex(new
     {
      campaign.CampaignGuid,definitionId,OperationOrdinal=committedOrdinal-1,
      seed=seed.ToString(),authority=expectedAuthority
     });
     var expectedOperationId="WGOP023_"+hash.Substring(0,24).ToUpperInvariant();
     var expectedCommitId="WGCONTRACT023_"+hash.Substring(0,20).ToUpperInvariant();
     var expectedExpeditionId="WGEXP023_"+hash.Substring(0,20).ToUpperInvariant();
     var contract=city.ActiveContract;var expedition=city.Expedition;
      if(!StringComparer.Ordinal.Equals(op.OperationId,expectedOperationId)||
         !StringComparer.Ordinal.Equals(op.CanonicalSeedIdentity,expectedAuthority)||
         !StringComparer.Ordinal.Equals(op.PreviousAuthorityChainHash,
             runtime.AuthorityChainHash)||
        op.RewardPermilleAtBegin!=expectedRewardPermille||
       contract==null||expedition==null||contract.Completed||contract.Failed||
       !StringComparer.Ordinal.Equals(contract.CommitId,expectedCommitId)||
       !StringComparer.Ordinal.Equals(contract.ContractId,op.DefinitionId)||
       !StringComparer.Ordinal.Equals(contract.BoardId,op.BoardId)||
       !StringComparer.Ordinal.Equals(contract.CanonicalSeedIdentity,op.CanonicalSeedIdentity)||
       contract.AcceptedOperationOrdinal!=committedOrdinal||
       !StringComparer.Ordinal.Equals(expedition.ExpeditionId,expectedExpeditionId)||
        !StringComparer.Ordinal.Equals(expedition.ContractCommitId,contract.CommitId)||
        !StringComparer.Ordinal.Equals(expedition.BoardId,op.BoardId)||
        !StringComparer.Ordinal.Equals(expedition.CurrentNodeId,op.CurrentNodeId)||
         op.CompletedNodeIds.Count!=op.AppliedReceiptIds.Count||
          op.CompletedNodeIds.Count!=op.AppliedReceipts.Count||
          !SameNodeLedger084(runtime.ActiveNodeLedger,op.AppliedReceipts)||
          !expedition.CommittedMoveIds.SequenceEqual(op.AppliedReceiptIds,StringComparer.Ordinal))return false;
     if(!MatchesExpeditionResources084(campaign,city,op,expedition))return false;
      var legalUnions=ValidateUnions(campaign,op.AlliedUnionIds);
       if(legalUnions.Count==0||legalUnions.Count>board.MaximumAlliedUnions||
          !legalUnions.SequenceEqual(op.AlliedUnionIds,StringComparer.Ordinal)||
          !StringComparer.Ordinal.Equals(op.AlliedRosterIdentity,
              currentRosterIdentity)||
          !op.AlliedRecruitIds.SequenceEqual(currentRecruitIds,
              StringComparer.Ordinal))return false;
    var expectedVisited=new HashSet<string>(op.CompletedNodeIds,StringComparer.Ordinal)
    {
     op.CurrentNodeId
    };
    if(!expedition.VisitedNodeIds.SequenceEqual(expectedVisited.OrderBy(x=>x,StringComparer.Ordinal),StringComparer.Ordinal)||
        !expedition.RevealedNodeIds.SequenceEqual(expectedVisited.OrderBy(x=>x,StringComparer.Ordinal),StringComparer.Ordinal)||
        !ValidateCompletedPath084(board,op)||
        !ValidateAppliedLedger084(campaign,catalog,runtime,op,board))return false;
    var current=board.Nodes.FirstOrDefault(value=>StringComparer.Ordinal.Equals(value.NodeId,op.CurrentNodeId));
    if(current==null)return false;
     if(op.Status!=WorldGateOperationStatus023.AwaitingBattle&&
        (city.PendingEncounter!=null||city.PendingBattleReturn!=null))return false;
     if(op.Status==WorldGateOperationStatus023.ReadyToFinalize)
      return op.PendingReceipt==null&&StringComparer.Ordinal.Equals(current.Kind,"EXIT")&&
             expedition.Status==ExpeditionStatus017D.Completed;
     if(op.Status==WorldGateOperationStatus023.AwaitingBattle)
     {
      if(op.PendingReceipt!=null||!current.RequiresCertifiedBattle)return false;
      if(city.PendingEncounter==null)
       return city.PendingBattleReturn==null&&
              (expedition.Status==ExpeditionStatus017D.Active||
               expedition.Status==ExpeditionStatus017D.Failed);
       var expected=CreateEncounterRequest084(campaign,city,op,current,
           city.PendingEncounter.PreBattleStateHash);
       if(!SameEncounter084(city.PendingEncounter,expected)||
          !campaign.Guild.Development.HasAdventureAuthority(
              GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(
                  expected))||
          expedition.Status!=ExpeditionStatus017D.AwaitingBattle)return false;
      return city.PendingBattleReturn==null||
             (StringComparer.Ordinal.Equals(city.PendingBattleReturn.LaunchRequestId,
                  expected.RequestId)&&
              StringComparer.Ordinal.Equals(city.PendingBattleReturn.BattleRunId,
                  expected.BattleId));
     }
    return op.Status==WorldGateOperationStatus023.Active&&
           expedition.Status==ExpeditionStatus017D.Active&&
           (op.PendingReceipt==null||StringComparer.Ordinal.Equals(op.PendingReceipt.NodeId,op.CurrentNodeId));
   }
    static bool ValidateCompletedPath084(WorldGateBoardRule023 board,WorldGateOperationState023 op)
   {
    var remaining=new HashSet<string>(op.CompletedNodeIds,StringComparer.Ordinal);
    var cursor=board.StartNodeId;
    for(var guard=0;guard<=board.Nodes.Count;guard++)
    {
     if(!remaining.Contains(cursor))
      return remaining.Count==0&&StringComparer.Ordinal.Equals(cursor,op.CurrentNodeId)&&
             op.Status!=WorldGateOperationStatus023.ReadyToFinalize;
     remaining.Remove(cursor);
     var current=board.Nodes.FirstOrDefault(value=>StringComparer.Ordinal.Equals(value.NodeId,cursor));
     if(current==null)return false;
     if(StringComparer.Ordinal.Equals(current.Kind,"EXIT")||current.NextNodeIds.Count==0)
      return remaining.Count==0&&StringComparer.Ordinal.Equals(cursor,op.CurrentNodeId)&&
             op.Status==WorldGateOperationStatus023.ReadyToFinalize;
     var candidates=current.NextNodeIds.Where(value=>remaining.Contains(value)||
         StringComparer.Ordinal.Equals(value,op.CurrentNodeId)).Distinct(StringComparer.Ordinal).ToArray();
     if(candidates.Length!=1)return false;
     cursor=candidates[0];
    }
     return false;
    }
    static WorldGateCompletionProof023 CreateCompletionProof084(
        CampaignState campaign,WorldGateBoardRule023 board,
        WorldGateOperationState023 operation,
        int committedOperationOrdinal,int initialSupplies)
    {
     if(board==null||operation==null)return null;
     var byNode=operation.AppliedReceipts.ToDictionary(
         value=>value.NodeId,value=>value,StringComparer.Ordinal);
     var ordered=new List<WorldGateNodeReceipt023>();
     var cursor=board.StartNodeId;
     for(var guard=0;guard<=board.Nodes.Count;guard++)
     {
      if(!byNode.TryGetValue(cursor,out var receipt))return null;
      ordered.Add(receipt);
      if(StringComparer.Ordinal.Equals(cursor,board.ExitNodeId))break;
      cursor=receipt.NextNodeId;
     }
     if(ordered.Count==0||
        !StringComparer.Ordinal.Equals(ordered[ordered.Count-1].NodeId,
            board.ExitNodeId)||ordered.Count!=operation.AppliedReceipts.Count)
      return null;
     var hasBattle=ordered.Any(value=>
         StringComparer.Ordinal.Equals(value.Outcome,"VICTORY")||
         StringComparer.Ordinal.Equals(value.Outcome,"DEFEAT"));
     var battle=hasBattle?campaign?.Battle:null;
     if(hasBattle&&(battle?.Reward==null||!battle.Reward.Claimed||
        !StringComparer.Ordinal.Equals(battle.Reward.RewardId,
            operation.ExistingBattleRewardReceiptId)))return null;
     var delegatedChecks=operation.ExpeditionDeck089?.AppliedReceipts
         .Where(value=>value.DelegatedToWorldGateCheck&&value.BaseCheckModifier!=0)
         .ToArray();
     if((delegatedChecks?.Length??0)==0)delegatedChecks=null;
     var draft=new WorldGateCompletionProof023(
         operation.OperationId,operation.DefinitionId,operation.BoardId,
         operation.WorldId,committedOperationOrdinal,operation.InitialTrust,
         operation.InitialTension,operation.InitialCivilianSupport,
         operation.AlliedUnionIds,operation.RewardPermilleAtBegin,
         operation.AlliedRosterIdentity,operation.CanonicalSeedIdentity,
         board.StartNodeId,board.ExitNodeId,operation.CompletedNodeIds,
         ordered.AsReadOnly(),initialSupplies,operation.Supplies,
         operation.Fatigue,operation.Urgency,operation.Threat,operation.Trust,
         operation.Tension,operation.CivilianSupport,
          operation.ExistingBattleRewardReceiptId,string.Empty,2,null,null,null,null,
           operation.AlliedRecruitIds,operation.PreviousAuthorityChainHash,
           operation.OptionalBattleCosts093,delegatedChecks);
     draft=new WorldGateCompletionProof023(
         draft.OperationId,draft.DefinitionId,draft.BoardId,draft.WorldId,
         draft.CommittedOperationOrdinal,draft.InitialTrust,draft.InitialTension,
         draft.InitialCivilianSupport,draft.AlliedUnionIds,
         draft.RewardPermilleAtBegin,draft.AlliedRosterIdentity,
         draft.CanonicalSeedIdentity,draft.StartNodeId,draft.ExitNodeId,
         draft.CompletedNodeIds,draft.AppliedReceipts,draft.InitialSupplies,
         draft.FinalSupplies,draft.FinalFatigue,draft.FinalUrgency,
         draft.FinalThreat,draft.FinalTrust,draft.FinalTension,
         draft.FinalCivilianSupport,draft.ExistingBattleRewardReceiptId,
          string.Empty,draft.ProofVersion,battle?.BattleId,battle?.FinalStateHash,
          battle?.Outcome.ToString(),battle?.Reward?.RewardId,
           draft.AlliedRecruitIds,draft.PreviousAuthorityChainHash,draft.OptionalBattleCosts093,draft.DelegatedCheckReceipts093);
     return new WorldGateCompletionProof023(
         draft.OperationId,draft.DefinitionId,draft.BoardId,draft.WorldId,
         draft.CommittedOperationOrdinal,draft.InitialTrust,draft.InitialTension,
         draft.InitialCivilianSupport,draft.AlliedUnionIds,
         draft.RewardPermilleAtBegin,draft.AlliedRosterIdentity,
         draft.CanonicalSeedIdentity,draft.StartNodeId,draft.ExitNodeId,
         draft.CompletedNodeIds,draft.AppliedReceipts,draft.InitialSupplies,
         draft.FinalSupplies,draft.FinalFatigue,draft.FinalUrgency,
         draft.FinalThreat,draft.FinalTrust,draft.FinalTension,
         draft.FinalCivilianSupport,draft.ExistingBattleRewardReceiptId,
          CompletionLedgerHash084(draft),draft.ProofVersion,draft.BattleId,
          draft.BattleFinalStateHash,draft.BattleOutcome,draft.BattleRewardId,
           draft.AlliedRecruitIds,draft.PreviousAuthorityChainHash,draft.OptionalBattleCosts093,draft.DelegatedCheckReceipts093);
    }

     public static bool ValidateCompletionProof084(
         CampaignState campaign,IWorldGateOperationsCatalog023 catalog,
         WorldGateCompletionProof023 proof)
     {
      return ValidateCompletionProof084(campaign,catalog,proof,true);
     }

     static bool ValidateCompletionProof084(
         CampaignState campaign,IWorldGateOperationsCatalog023 catalog,
         WorldGateCompletionProof023 proof,bool requireCurrentBattle)
     {
     if(campaign==null||catalog==null||proof==null||proof.ProofVersion!=2||
         string.IsNullOrWhiteSpace(proof.StartNodeId)||
         string.IsNullOrWhiteSpace(proof.ExitNodeId)||
          string.IsNullOrWhiteSpace(proof.PreviousAuthorityChainHash)||
         string.IsNullOrWhiteSpace(proof.CompletionLedgerHash)||
         proof.AlliedRecruitIds.Count==0||
         proof.AlliedRecruitIds.Distinct(StringComparer.Ordinal).Count()!=
             proof.AlliedRecruitIds.Count||
        proof.AppliedReceipts.Count==0||
        proof.AppliedReceipts.Count!=proof.CompletedNodeIds.Count||
        proof.AppliedReceipts.Select(value=>value.NodeId)
            .Distinct(StringComparer.Ordinal).Count()!=proof.AppliedReceipts.Count||
        proof.AppliedReceipts.Select(value=>value.ReceiptId)
            .Distinct(StringComparer.Ordinal).Count()!=proof.AppliedReceipts.Count)
      return false;
     if(!catalog.TryGetBoard(proof.DefinitionId,out var board)||board==null||
        !BoardAdventureRules084.IsCompatible084(board,out _)||
        !StringComparer.Ordinal.Equals(board.BoardId,proof.BoardId)||
        !StringComparer.Ordinal.Equals(board.WorldId,proof.WorldId)||
        !StringComparer.Ordinal.Equals(board.StartNodeId,proof.StartNodeId)||
        !StringComparer.Ordinal.Equals(board.ExitNodeId,proof.ExitNodeId)||
        proof.InitialSupplies!=catalog.RewardPolicy.TravelSupplyReserveDefault)
      return false;
     var seed=SemanticSeed.Derive(campaign.CampaignSeed,"WORLD_GATE_023",
         proof.DefinitionId,proof.CommittedOperationOrdinal.ToString());
     var authority=CanonicalJson.Sha256Hex(new
     {
      Seed=seed.ToString(),Trust=proof.InitialTrust,Tension=proof.InitialTension,
      CivilianSupport=proof.InitialCivilianSupport,
      AlliedUnionIds=proof.AlliedUnionIds,
       RewardPermilleAtBegin=proof.RewardPermilleAtBegin,
        AlliedRosterIdentity=proof.AlliedRosterIdentity,
        AlliedRecruitIds=proof.AlliedRecruitIds,
        PreviousAuthorityChainHash=proof.PreviousAuthorityChainHash
     });
     var hash=CanonicalJson.Sha256Hex(new
     {
      campaign.CampaignGuid,definitionId=proof.DefinitionId,
      OperationOrdinal=proof.CommittedOperationOrdinal-1,
      seed=seed.ToString(),authority
     });
     if(!StringComparer.Ordinal.Equals(proof.CanonicalSeedIdentity,authority)||
        !StringComparer.Ordinal.Equals(proof.OperationId,
            "WGOP023_"+hash.Substring(0,24).ToUpperInvariant())||
        !StringComparer.Ordinal.Equals(proof.CompletionLedgerHash,
            CompletionLedgerHash084(proof))||
        !proof.CompletedNodeIds.SequenceEqual(
            proof.AppliedReceipts.Select(value=>value.NodeId)
                .OrderBy(value=>value,StringComparer.Ordinal),
            StringComparer.Ordinal))return false;
     var operation=new WorldGateOperationState023(
         proof.OperationId,proof.DefinitionId,proof.BoardId,board.OperationKind,
         proof.WorldId,proof.CanonicalSeedIdentity,proof.ExitNodeId,
         WorldGateOperationStatus023.ReadyToFinalize,proof.FinalSupplies,
         proof.FinalFatigue,proof.FinalUrgency,proof.FinalThreat,
         proof.FinalTrust,proof.FinalTension,proof.FinalCivilianSupport,
         proof.AlliedUnionIds,proof.CompletedNodeIds,
         proof.AppliedReceipts.Select(value=>value.ReceiptId).ToArray(),null,
         proof.ExistingBattleRewardReceiptId,"completion_proof_084",
         proof.AppliedReceipts,proof.InitialTrust,proof.InitialTension,
          proof.InitialCivilianSupport,proof.RewardPermilleAtBegin,
           proof.AlliedRosterIdentity,proof.AlliedRecruitIds,
           proof.PreviousAuthorityChainHash,optionalBattleCosts093:proof.OptionalBattleCosts093);
     if(!WorldGateOptionalBattleCost093.Validate(campaign,operation,board,
            proof.OptionalBattleCosts093))return false;
     if(!ValidateDelegatedCheckProof093(campaign,proof))return false;
     var supplies=proof.InitialSupplies;
     var fatigue=0;var urgency=0;var threat=0;
     var trust=proof.InitialTrust;var tension=proof.InitialTension;
     var support=proof.InitialCivilianSupport;
     var cursor=proof.StartNodeId;var battleReceiptCount=0;
     for(var index=0;index<proof.AppliedReceipts.Count;index++)
     {
      var receipt=proof.AppliedReceipts[index];
      if(!WorldGateOptionalBattleCost093.ApplyAtNode(proof.OptionalBattleCosts093,
             cursor,ref supplies,ref fatigue,ref urgency))return false;
      var node=board.Nodes.FirstOrDefault(value=>StringComparer.Ordinal.Equals(
          value.NodeId,receipt?.NodeId));
       if(receipt==null||receipt.AppliedVersion!=0||
          !campaign.Guild.Development.HasAdventureAuthority(
              receipt.ReceiptId)||
          node==null||
         !StringComparer.Ordinal.Equals(receipt.OperationId,proof.OperationId)||
         !StringComparer.Ordinal.Equals(receipt.NodeId,cursor)||
         string.IsNullOrWhiteSpace(receipt.AuthoritativeHash)||
         receipt.AuthoritativeHash.Length<24||
         !StringComparer.Ordinal.Equals(receipt.ReceiptId,
             "WGREC023_"+receipt.AuthoritativeHash.Substring(0,24)
                 .ToUpperInvariant())||
         !(node.RequiresCertifiedBattle
              ?ValidateCompletedBattleReceipt084(campaign,proof,operation,node,receipt)
               :ValidatePendingReceipt084(campaign,catalog,
                   WorldGateRuntimeState023.Default(),operation,node,receipt,out _,
                   proof.DelegatedCheckReceipts093)))
       return false;
      var last=index==proof.AppliedReceipts.Count-1;
      if(last)
      {
       if(!StringComparer.Ordinal.Equals(receipt.NodeId,proof.ExitNodeId)||
          !string.IsNullOrWhiteSpace(receipt.NextNodeId))return false;
      }
      else
      {
       if(string.IsNullOrWhiteSpace(receipt.NextNodeId))return false;
       cursor=receipt.NextNodeId;
      }
      if(StringComparer.Ordinal.Equals(receipt.Outcome,"VICTORY")||
         StringComparer.Ordinal.Equals(receipt.Outcome,"DEFEAT"))
       battleReceiptCount++;
      supplies=Math.Max(0,supplies+receipt.SupplyDelta);
      fatigue=Math.Max(0,fatigue+receipt.FatigueDelta);
      urgency=Math.Max(0,urgency+receipt.UrgencyDelta);
      threat=Math.Max(0,threat+receipt.ThreatDelta);
      trust=Math.Max(-100,Math.Min(100,trust+receipt.TrustDelta));
      tension=Math.Max(0,Math.Min(100,tension+receipt.TensionDelta));
      support=Math.Max(-100,Math.Min(100,support+
          receipt.CivilianSupportDelta));
     }
      var hasCompleteBattleProof=!string.IsNullOrWhiteSpace(proof.BattleId)&&
          !string.IsNullOrWhiteSpace(proof.BattleFinalStateHash)&&
          !string.IsNullOrWhiteSpace(proof.BattleOutcome)&&
          !string.IsNullOrWhiteSpace(proof.BattleRewardId);
       var hasAnyBattleProof=!string.IsNullOrWhiteSpace(proof.BattleId)||
           !string.IsNullOrWhiteSpace(proof.BattleFinalStateHash)||
           !string.IsNullOrWhiteSpace(proof.BattleOutcome)||
           !string.IsNullOrWhiteSpace(proof.BattleRewardId);
       var battleAuthority=true;
        if(battleReceiptCount==1&&requireCurrentBattle)
        {
        var battle=campaign.Battle;
        battleAuthority=battle?.Reward!=null&&battle.Reward.Claimed&&
            StringComparer.Ordinal.Equals(battle.BattleId,proof.BattleId)&&
            StringComparer.Ordinal.Equals(battle.FinalStateHash,
                proof.BattleFinalStateHash)&&
            M2BattleCommandService.HasValidFinalStateHash090(battle)&&
            StringComparer.Ordinal.Equals(battle.Outcome.ToString(),
                proof.BattleOutcome)&&
            StringComparer.Ordinal.Equals(battle.Reward.RewardId,
                proof.BattleRewardId)&&
            campaign.Guild.Development.HasClaimedReward(
                proof.BattleRewardId);
        }
        else if(battleReceiptCount==1)
         battleAuthority=campaign.Guild.Development.HasClaimedReward(
             proof.BattleRewardId);
       return battleReceiptCount<=1&&
             (battleReceiptCount==0
                 ?string.IsNullOrWhiteSpace(proof.ExistingBattleRewardReceiptId)&&
                  !hasAnyBattleProof
                 :!string.IsNullOrWhiteSpace(proof.ExistingBattleRewardReceiptId)&&
                   hasCompleteBattleProof&&battleAuthority)&&
            supplies==proof.FinalSupplies&&fatigue==proof.FinalFatigue&&
            urgency==proof.FinalUrgency&&threat==proof.FinalThreat&&
            trust==proof.FinalTrust&&tension==proof.FinalTension&&
            support==proof.FinalCivilianSupport;
    }

    static bool ValidateCompletedBattleReceipt084(
        CampaignState campaign,WorldGateCompletionProof023 proof,
        WorldGateOperationState023 operation,
        WorldGateNodeRule023 node,WorldGateNodeReceipt023 receipt)
    {
     if(proof==null||operation==null||node==null||receipt==null||
        string.IsNullOrWhiteSpace(proof.BattleId)||
        string.IsNullOrWhiteSpace(proof.BattleFinalStateHash)||
        string.IsNullOrWhiteSpace(proof.BattleOutcome)||
        string.IsNullOrWhiteSpace(proof.BattleRewardId)||
         !StringComparer.Ordinal.Equals(proof.BattleRewardId,
             proof.ExistingBattleRewardReceiptId)||
         string.IsNullOrWhiteSpace(receipt.BattleReturnAuthorityId)||
         !receipt.BattleReturnAuthorityId.StartsWith(
             GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityPrefix084,
             StringComparison.Ordinal)||
         !campaign.Guild.Development.HasAdventureAuthority(
             receipt.BattleReturnAuthorityId)||
         !string.IsNullOrWhiteSpace(receipt.ActorRecruitId)||
        !string.IsNullOrWhiteSpace(receipt.AssistantRecruitId)||
        receipt.DieOne!=0||receipt.DieTwo!=0||receipt.Modifier!=0)
      return false;
     if(!Enum.TryParse(proof.BattleOutcome,false,out BattleOutcome outcome))
      return false;
     var encounterHash=CanonicalJson.Sha256Hex(new
     {
      operation.OperationId,node.NodeId,operation.CanonicalSeedIdentity
     });
     if(!StringComparer.Ordinal.Equals(proof.BattleId,
         "WORLD_GATE_BATTLE_023_"+
             encounterHash.Substring(0,18).ToUpperInvariant()))return false;
     if(!BoardAdventureRules084.TryNextNode084(node,receipt.ChoiceId,out var next)||
        !StringComparer.Ordinal.Equals(next,receipt.NextNodeId)||
        !(node.ChoiceIds??Array.Empty<string>()).Contains(receipt.ChoiceId))
      return false;
     var hash=CanonicalJson.Sha256Hex(new
     {
      operation.OperationId,node.NodeId,Choice=receipt.ChoiceId,Next=next,
      BattleOutcome=proof.BattleOutcome,
       FinalStateHash=proof.BattleFinalStateHash,
       ExistingReward=proof.BattleRewardId,
       BattleReturnAuthority=receipt.BattleReturnAuthorityId,
       operation.CanonicalSeedIdentity
     });
     var victory=outcome==BattleOutcome.Victory;
     int Scale(int value)=>Math.Max(0,
         (value*proof.RewardPermilleAtBegin)/10000);
     var expectedMaterials=victory
         ?ScaleMaterials084(node.MaterialIds,proof.RewardPermilleAtBegin)
         :Array.Empty<string>();
     return StringComparer.Ordinal.Equals(receipt.Outcome,
                outcome.ToString().ToUpperInvariant())&&
            StringComparer.Ordinal.Equals(receipt.AuthoritativeHash,hash)&&
            StringComparer.Ordinal.Equals(receipt.ReceiptId,
                "WGREC023_"+hash.Substring(0,24).ToUpperInvariant())&&
            receipt.SupplyDelta==node.SupplyDelta&&
            receipt.FatigueDelta==(victory?node.FatigueDelta:
                Math.Max(2,node.FatigueDelta+1))&&
            receipt.UrgencyDelta==node.UrgencyDelta&&
            receipt.ThreatDelta==(victory?node.ThreatDelta:
                Math.Max(-1,node.ThreatDelta))&&
            receipt.TrustDelta==(victory?node.TrustDelta:
                Math.Min(0,node.TrustDelta))&&
            receipt.TensionDelta==(victory?node.TensionDelta:
                Math.Max(0,node.TensionDelta))&&
            receipt.CivilianSupportDelta==(victory?node.CivilianSupportDelta:
                Math.Min(0,node.CivilianSupportDelta))&&
            receipt.GuildXp==(victory?Scale(node.GuildXp):0)&&
            receipt.HallXp==(victory?Scale(node.HallXp):0)&&
             SameIds084(receipt.MaterialIds,expectedMaterials);
    }

     static WorldGateRuntimeState023 EnsureAuthorityChain084(
         CampaignState campaign,GuildCityState017D city,
         WorldGateRuntimeState023 runtime)
     {
      if(runtime==null||!string.IsNullOrWhiteSpace(runtime.AuthorityChainHash))
       return runtime;
      if(!IsCanonicalUnchainedBaseline084(runtime))return runtime;
      var baseline=WorldGateRuntimeState023.Default();
       var root=AuthorityChainRootHash084(campaign,0,baseline.WorldStandings,
           baseline.UnlockedRecruitOriginIds,baseline.UnlockedWorldIds,
           baseline.CompletedDefinitionIds,baseline.CurrentWorldId,
           baseline.TravelSupplies,baseline.RepeatableStreakDefinitionId,
           baseline.RepeatableStreakCount,string.Empty);
      return new WorldGateRuntimeState023(
          WorldGateRuntimeState023.ContentVersion,runtime.ActiveOperation,
          runtime.CurrentWorldId,runtime.UnlockedWorldIds,runtime.WorldStandings,
         runtime.CompletedDefinitionIds,runtime.UnlockedRecruitOriginIds,
         runtime.RepeatableStreakDefinitionId,runtime.RepeatableStreakCount,
         runtime.TravelSupplies,runtime.LastCheckpointId,
         runtime.LastCompletionProof,runtime.CompletionProofs,root,
          0,baseline.WorldStandings,
           baseline.UnlockedRecruitOriginIds,baseline.UnlockedWorldIds,
           baseline.CompletedDefinitionIds,Array.Empty<WorldGateAuthorityEntry023>(),
           root,baseline.CurrentWorldId,baseline.TravelSupplies,
           baseline.RepeatableStreakDefinitionId,baseline.RepeatableStreakCount,
           string.Empty,
           expeditionRecruitLeadIds089:runtime.ExpeditionRecruitLeadIds089,
           expeditionDeckTutorialSeen089:runtime.ExpeditionDeckTutorialSeen089);
     }

     static bool IsCanonicalUnchainedBaseline084(
         WorldGateRuntimeState023 runtime)
     {
      var baseline=WorldGateRuntimeState023.Default();
      if(runtime==null||runtime.ActiveOperation!=null||
         runtime.LastCompletionProof!=null||runtime.CompletionProofs.Count!=0||
         runtime.AuthorityEntries.Count!=0||
         !string.IsNullOrWhiteSpace(runtime.AuthorityChainBaseHash)||
         !string.IsNullOrWhiteSpace(runtime.AuthorityChainHash)||
         runtime.AuthorityChainBaseOperationOrdinal!=0||
         runtime.AuthorityChainBaseStandings.Count!=0||
         runtime.AuthorityChainBaseUnlockedOrigins.Count!=0||
         runtime.AuthorityChainBaseUnlockedWorlds.Count!=0||
         runtime.AuthorityChainBaseCompletedDefinitions.Count!=0||
         !string.IsNullOrWhiteSpace(runtime.AuthorityChainBaseCurrentWorldId)||
         runtime.AuthorityChainBaseTravelSupplies!=0||
         !string.IsNullOrWhiteSpace(
             runtime.AuthorityChainBaseRepeatableStreakDefinitionId)||
         runtime.AuthorityChainBaseRepeatableStreakCount!=0||
         !string.IsNullOrWhiteSpace(runtime.AuthorityChainMigrationReceiptId)||
         !StringComparer.Ordinal.Equals(runtime.CurrentWorldId,
             baseline.CurrentWorldId)||
         runtime.TravelSupplies!=baseline.TravelSupplies||
         runtime.RepeatableStreakCount!=0||
         !string.IsNullOrWhiteSpace(runtime.RepeatableStreakDefinitionId)||
         !SameIds084(runtime.UnlockedWorldIds,baseline.UnlockedWorldIds)||
         !SameIds084(runtime.UnlockedRecruitOriginIds,
             baseline.UnlockedRecruitOriginIds)||
         !SameIds084(runtime.CompletedDefinitionIds,
             baseline.CompletedDefinitionIds)||
         runtime.WorldStandings.Count!=baseline.WorldStandings.Count)return false;
      for(var index=0;index<baseline.WorldStandings.Count;index++)
      {
       var expected=baseline.WorldStandings[index];
       var actual=runtime.WorldStandings[index];
       if(actual==null||!StringComparer.Ordinal.Equals(actual.WorldId,
              expected.WorldId)||actual.Trust!=expected.Trust||
          actual.Tension!=expected.Tension||
          actual.CivilianSupport!=expected.CivilianSupport||
          !StringComparer.Ordinal.Equals(actual.TierId,expected.TierId))return false;
      }
      return true;
     }

     static WorldGateAuthorityEntry023 CreateAuthorityEntry084(
         WorldGateRuntimeState023 runtime,WorldGateCompletionProof023 proof,
         IReadOnlyList<string> completedAfter,IReadOnlyList<string> originsAfter,
         IReadOnlyList<string> worldsAfter,int travelSuppliesAfter,
         string repeatableStreakDefinitionIdAfter,int repeatableStreakCountAfter)
    {
     if(runtime==null||proof==null||
        !StringComparer.Ordinal.Equals(proof.PreviousAuthorityChainHash,
            runtime.AuthorityChainHash))return null;
     WorldGateAuthorityEntry023 Build(string hash)=>
         new WorldGateAuthorityEntry023(runtime.AuthorityChainHash,hash,
             proof.CommittedOperationOrdinal,proof.OperationId,proof.DefinitionId,
             runtime.ActiveOperation?.OperationKind??"UNKNOWN",
             proof.WorldId,proof.CompletionLedgerHash,proof.InitialTrust,
             proof.InitialTension,proof.InitialCivilianSupport,proof.FinalTrust,
             proof.FinalTension,proof.FinalCivilianSupport,
             proof.AlliedRosterIdentity,proof.AlliedRecruitIds,
              runtime.UnlockedRecruitOriginIds,originsAfter,
              runtime.UnlockedWorldIds,worldsAfter,
              runtime.CompletedDefinitionIds,completedAfter,
              runtime.AuthorityEntries.Count+1,runtime.CurrentWorldId,
              proof.WorldId,runtime.TravelSupplies,travelSuppliesAfter,
              runtime.RepeatableStreakDefinitionId,runtime.RepeatableStreakCount,
              repeatableStreakDefinitionIdAfter,repeatableStreakCountAfter,proof);
     var draft=Build("PENDING_AUTHORITY_ENTRY_HASH_023");
      return Build(AuthorityEntryHash084(draft));
     }

     static WorldGateRuntimeState023 AppendTravelAuthority084(
         WorldGateRuntimeState023 runtime,int operationOrdinal,string destinationWorldId,
         int supplyCost,string checkpoint)
     {
      if(runtime==null||runtime.AuthorityEntries.Count>=
             AuthorityChainEntryLimit084||supplyCost<0||
         string.IsNullOrWhiteSpace(destinationWorldId)||
         runtime.TravelSupplies<supplyCost)return null;
      var standing=runtime.WorldStandings.FirstOrDefault(value=>
          StringComparer.Ordinal.Equals(value.WorldId,destinationWorldId))??
          new WorldStandingState023(destinationWorldId,0,25,0,
              "RECOVERABLE_FRACTURE");
      var sequence=runtime.AuthorityEntries.Count+1;
      var operationId=TravelOperationId084(runtime.AuthorityChainHash,sequence,
          runtime.CurrentWorldId,destinationWorldId,supplyCost);
      WorldGateAuthorityEntry023 Build(string hash)=>
          new WorldGateAuthorityEntry023(runtime.AuthorityChainHash,hash,
              Math.Max(1,operationOrdinal),operationId,
              "TRAVEL_"+destinationWorldId,"TRAVEL",destinationWorldId,
              "TRAVEL_COST_"+supplyCost,standing.Trust,standing.Tension,
              standing.CivilianSupport,standing.Trust,standing.Tension,
              standing.CivilianSupport,"NO_ROSTER",Array.Empty<string>(),
              runtime.UnlockedRecruitOriginIds,runtime.UnlockedRecruitOriginIds,
              runtime.UnlockedWorldIds,runtime.UnlockedWorldIds,
              runtime.CompletedDefinitionIds,runtime.CompletedDefinitionIds,
              sequence,runtime.CurrentWorldId,destinationWorldId,
              runtime.TravelSupplies,runtime.TravelSupplies-supplyCost,
              runtime.RepeatableStreakDefinitionId,runtime.RepeatableStreakCount,
              runtime.RepeatableStreakDefinitionId,runtime.RepeatableStreakCount,
              null);
      var draft=Build("PENDING_AUTHORITY_ENTRY_HASH_023");
      var entry=Build(AuthorityEntryHash084(draft));
      var entries=new List<WorldGateAuthorityEntry023>(runtime.AuthorityEntries)
          {entry};
      return runtime.With(currentWorldId:destinationWorldId,
          travelSupplies:runtime.TravelSupplies-supplyCost,
          lastCheckpointId:checkpoint,authorityEntries:entries.AsReadOnly(),
          authorityChainHash:entry.EntryHash);
     }

    static string AuthorityEntryHash084(WorldGateAuthorityEntry023 entry)=>
        CanonicalJson.Sha256Hex(new
        {
         entry.PreviousHash,entry.CommittedOperationOrdinal,entry.OperationId,
         entry.DefinitionId,entry.OperationKind,entry.WorldId,
         entry.CompletionLedgerHash,entry.InitialTrust,entry.InitialTension,
         entry.InitialCivilianSupport,entry.FinalTrust,entry.FinalTension,
         entry.FinalCivilianSupport,entry.AlliedRosterIdentity,
          entry.AlliedRecruitIds,entry.UnlockedOriginsBefore,
          entry.UnlockedOriginsAfter,entry.UnlockedWorldsBefore,
          entry.UnlockedWorldsAfter,entry.CompletedDefinitionsBefore,
          entry.CompletedDefinitionsAfter,entry.SequenceIndex,
          entry.CurrentWorldBefore,entry.CurrentWorldAfter,
          entry.TravelSuppliesBefore,entry.TravelSuppliesAfter,
          entry.RepeatableStreakDefinitionIdBefore,
          entry.RepeatableStreakCountBefore,
          entry.RepeatableStreakDefinitionIdAfter,
          entry.RepeatableStreakCountAfter,
          CompletionProofHash=entry.CompletionProof?.CompletionLedgerHash??string.Empty,
          entry.CompactedStandingsAfter
         });

    static string AuthorityChainRootHash084(CampaignState campaign,int ordinal,
         IReadOnlyList<WorldStandingState023> standings,
         IReadOnlyList<string> origins,IReadOnlyList<string> worlds,
         IReadOnlyList<string> completed,string currentWorldId,int travelSupplies,
         string repeatableStreakDefinitionId,int repeatableStreakCount,
         string migrationReceiptId)=>CanonicalJson.Sha256Hex(new
         {
          Authority="WORLD_GATE_AUTHORITY_CHAIN_023_2",
          campaign.CampaignGuid,campaign.CampaignSeed,
          BaseOperationOrdinal=ordinal,BaseStandings=standings,
          BaseUnlockedOrigins=origins,BaseUnlockedWorlds=worlds,
          BaseCompletedDefinitions=completed,BaseCurrentWorldId=currentWorldId,
          BaseTravelSupplies=travelSupplies,
          BaseRepeatableStreakDefinitionId=repeatableStreakDefinitionId,
          BaseRepeatableStreakCount=repeatableStreakCount,
         MigrationReceiptId=migrationReceiptId
         });

     static string MigrationReceiptId084(CampaignState campaign,int ordinal,
         IReadOnlyList<WorldStandingState023> standings,
         IReadOnlyList<string> origins,IReadOnlyList<string> worlds,
         IReadOnlyList<string> completed,string currentWorldId,int travelSupplies,
         string repeatableStreakDefinitionId,int repeatableStreakCount)=>
         "WGMIGRATE023_"+CanonicalJson.Sha256Hex(new
         {
          Authority="WORLD_GATE_LEGACY_RECOVERY_023_2.2",
          campaign.CampaignGuid,campaign.CampaignSeed,
          BaseOperationOrdinal=ordinal,BaseStandings=standings,
          BaseUnlockedOrigins=origins,BaseUnlockedWorlds=worlds,
          BaseCompletedDefinitions=completed,BaseCurrentWorldId=currentWorldId,
          BaseTravelSupplies=travelSupplies,
          BaseRepeatableStreakDefinitionId=repeatableStreakDefinitionId,
          BaseRepeatableStreakCount=repeatableStreakCount
         }).Substring(0,24).ToUpperInvariant();

     static bool ValidateAuthorityChain084(
         CampaignState campaign,IWorldGateOperationsCatalog023 catalog,
         WorldGateRuntimeState023 runtime)
     {
      if(campaign==null||catalog==null||runtime==null||
         string.IsNullOrWhiteSpace(runtime.AuthorityChainBaseHash)||
         string.IsNullOrWhiteSpace(runtime.AuthorityChainHash)||
         runtime.AuthorityEntries.Count>AuthorityChainEntryLimit084)return false;
       var baseline=WorldGateRuntimeState023.Default();
       var migrated=!string.IsNullOrWhiteSpace(
           runtime.AuthorityChainMigrationReceiptId);
       if(!migrated)
       {
        if(runtime.AuthorityChainBaseOperationOrdinal!=0||
           !StringComparer.Ordinal.Equals(
               runtime.AuthorityChainBaseCurrentWorldId,baseline.CurrentWorldId)||
           runtime.AuthorityChainBaseTravelSupplies!=baseline.TravelSupplies||
           !string.IsNullOrWhiteSpace(
               runtime.AuthorityChainBaseRepeatableStreakDefinitionId)||
           runtime.AuthorityChainBaseRepeatableStreakCount!=0||
           !SameIds084(runtime.AuthorityChainBaseUnlockedOrigins,
               baseline.UnlockedRecruitOriginIds)||
           !SameIds084(runtime.AuthorityChainBaseUnlockedWorlds,
               baseline.UnlockedWorldIds)||
           !SameIds084(runtime.AuthorityChainBaseCompletedDefinitions,
               baseline.CompletedDefinitionIds)||
           !SameStandings084(runtime.AuthorityChainBaseStandings,
               baseline.WorldStandings))return false;
       }
       else
       {
        if(runtime.AuthorityChainBaseOperationOrdinal<0||
           string.IsNullOrWhiteSpace(runtime.AuthorityChainBaseCurrentWorldId)||
           !runtime.AuthorityChainBaseUnlockedWorlds.Contains(
               runtime.AuthorityChainBaseCurrentWorldId)||
           (runtime.AuthorityChainBaseRepeatableStreakCount==0)!=
               string.IsNullOrWhiteSpace(
                   runtime.AuthorityChainBaseRepeatableStreakDefinitionId)||
           !StringComparer.Ordinal.Equals(runtime.AuthorityChainMigrationReceiptId,
               MigrationReceiptId084(campaign,
                   runtime.AuthorityChainBaseOperationOrdinal,
                   runtime.AuthorityChainBaseStandings,
                   runtime.AuthorityChainBaseUnlockedOrigins,
                   runtime.AuthorityChainBaseUnlockedWorlds,
                   runtime.AuthorityChainBaseCompletedDefinitions,
                   runtime.AuthorityChainBaseCurrentWorldId,
                   runtime.AuthorityChainBaseTravelSupplies,
                   runtime.AuthorityChainBaseRepeatableStreakDefinitionId,
                   runtime.AuthorityChainBaseRepeatableStreakCount)))return false;
        if(runtime.AuthorityChainBaseCompletedDefinitions.Any(id=>
               !catalog.TryGetBoard(id,out _)))return false;
        if(runtime.AuthorityChainBaseRepeatableStreakCount>0&&
           (!catalog.TryGetBoard(
                runtime.AuthorityChainBaseRepeatableStreakDefinitionId,
                out var streakBoard)||streakBoard==null||
            !StringComparer.Ordinal.Equals(streakBoard.OperationKind,
                "REPEATABLE")))return false;
        foreach(var baseStanding in runtime.AuthorityChainBaseStandings)
         if(baseStanding==null||
            !StringComparer.Ordinal.Equals(baseStanding.TierId,
                TierFor(baseStanding.Trust,baseStanding.Tension,
                    baseStanding.CivilianSupport,catalog,
                    baseStanding.WorldId)))return false;
       }
      if(!StringComparer.Ordinal.Equals(runtime.AuthorityChainBaseHash,
          AuthorityChainRootHash084(campaign,
             runtime.AuthorityChainBaseOperationOrdinal,
             runtime.AuthorityChainBaseStandings,
              runtime.AuthorityChainBaseUnlockedOrigins,
              runtime.AuthorityChainBaseUnlockedWorlds,
              runtime.AuthorityChainBaseCompletedDefinitions,
              runtime.AuthorityChainBaseCurrentWorldId,
              runtime.AuthorityChainBaseTravelSupplies,
              runtime.AuthorityChainBaseRepeatableStreakDefinitionId,
              runtime.AuthorityChainBaseRepeatableStreakCount,
              runtime.AuthorityChainMigrationReceiptId)))return false;
     var standings=new List<WorldStandingState023>(
         runtime.AuthorityChainBaseStandings);
     var origins=runtime.AuthorityChainBaseUnlockedOrigins.ToArray();
     var worlds=runtime.AuthorityChainBaseUnlockedWorlds.ToArray();
      var completed=runtime.AuthorityChainBaseCompletedDefinitions.ToArray();
      var previous=runtime.AuthorityChainBaseHash;
      var previousOrdinal=runtime.AuthorityChainBaseOperationOrdinal;
       var currentWorld=runtime.AuthorityChainBaseCurrentWorldId;
       var travelSupplies=runtime.AuthorityChainBaseTravelSupplies;
       var streakId=runtime.AuthorityChainBaseRepeatableStreakDefinitionId;
       var streakCount=runtime.AuthorityChainBaseRepeatableStreakCount;
      var sequence=0;
      foreach(var entry in runtime.AuthorityEntries)
      {
       var standing=standings.FirstOrDefault(value=>
           StringComparer.Ordinal.Equals(value.WorldId,entry.WorldId))??
           new WorldStandingState023(entry.WorldId,0,25,0,
               "RECOVERABLE_FRACTURE");
       if(!StringComparer.Ordinal.Equals(entry.PreviousHash,previous)||
          !StringComparer.Ordinal.Equals(entry.EntryHash,
              AuthorityEntryHash084(entry))||
          entry.SequenceIndex!=sequence+1||
          entry.CommittedOperationOrdinal<previousOrdinal||
          !StringComparer.Ordinal.Equals(entry.CurrentWorldBefore,currentWorld)||
          entry.TravelSuppliesBefore!=travelSupplies||
          !StringComparer.Ordinal.Equals(
              entry.RepeatableStreakDefinitionIdBefore,streakId)||
          entry.RepeatableStreakCountBefore!=streakCount||
          entry.InitialTrust!=standing.Trust||
          entry.InitialTension!=standing.Tension||
         entry.InitialCivilianSupport!=standing.CivilianSupport||
         !SameIds084(entry.UnlockedOriginsBefore,origins)||
          !SameIds084(entry.UnlockedWorldsBefore,worlds)||
          !SameIds084(entry.CompletedDefinitionsBefore,completed))return false;
       if(StringComparer.Ordinal.Equals(entry.OperationKind,"TRAVEL"))
       {
        var cost=0;
        if(!StringComparer.Ordinal.Equals(entry.WorldId,"SKYHOME"))
        {
         if(!catalog.TryGetTravel(entry.WorldId,out var travel))return false;
         cost=travel.SupplyCost;
        }
        var progress=campaign.Guild.GuildCity.Strategic017H?.Campaign019;
        var playable=progress?.Playable020;
        var unlocked=StringComparer.Ordinal.Equals(entry.WorldId,"SKYHOME")||
            worlds.Contains(entry.WorldId)||
            (progress?.UnlockedWorldIds?.Contains(entry.WorldId)??false)||
            (playable?.WorldGates?.Any(value=>value.Unlocked&&
                StringComparer.Ordinal.Equals(value.WorldId,entry.WorldId))??false);
        var expectedTravelId=TravelOperationId084(previous,entry.SequenceIndex,
            currentWorld,entry.WorldId,cost);
        if(!unlocked||entry.CompletionProof!=null||
           !StringComparer.Ordinal.Equals(entry.DefinitionId,
               "TRAVEL_"+entry.WorldId)||
           !StringComparer.Ordinal.Equals(entry.OperationId,expectedTravelId)||
           !StringComparer.Ordinal.Equals(entry.CompletionLedgerHash,
               "TRAVEL_COST_"+cost)||travelSupplies<cost||
           !StringComparer.Ordinal.Equals(entry.CurrentWorldAfter,entry.WorldId)||
           entry.TravelSuppliesAfter!=travelSupplies-cost||
           !StringComparer.Ordinal.Equals(
               entry.RepeatableStreakDefinitionIdAfter,streakId)||
           entry.RepeatableStreakCountAfter!=streakCount||
           entry.FinalTrust!=standing.Trust||
           entry.FinalTension!=standing.Tension||
           entry.FinalCivilianSupport!=standing.CivilianSupport||
           !SameIds084(entry.UnlockedOriginsAfter,origins)||
           !SameIds084(entry.UnlockedWorldsAfter,worlds)||
           !SameIds084(entry.CompletedDefinitionsAfter,completed))return false;
        currentWorld=entry.WorldId;
        travelSupplies-=cost;
       }
       else
       {
        var proof=entry.CompletionProof;
        if(proof==null||entry.CommittedOperationOrdinal<=previousOrdinal||
           !catalog.TryGetBoard(entry.DefinitionId,out var board)||board==null||
           !StringComparer.Ordinal.Equals(entry.OperationKind,board.OperationKind)||
           !StringComparer.Ordinal.Equals(entry.WorldId,board.WorldId)||
           !StringComparer.Ordinal.Equals(entry.OperationId,proof.OperationId)||
           !StringComparer.Ordinal.Equals(entry.CompletionLedgerHash,
               proof.CompletionLedgerHash)||
           !StringComparer.Ordinal.Equals(proof.PreviousAuthorityChainHash,
               previous)||
           !StringComparer.Ordinal.Equals(entry.AlliedRosterIdentity,
               proof.AlliedRosterIdentity)||
           !SameIds084(entry.AlliedRecruitIds,proof.AlliedRecruitIds)||
           !ValidateCompletionProof084(campaign,catalog,proof,false)||
           !StringComparer.Ordinal.Equals(currentWorld,entry.WorldId)||
           !StringComparer.Ordinal.Equals(entry.CurrentWorldAfter,entry.WorldId))
         return false;
        var expectedPermille=ExpectedRewardPermilleForStreak084(
            board.OperationKind,entry.DefinitionId,catalog.RewardPolicy,
            streakId,streakCount);
        var expectedTravelReward=StringComparer.Ordinal.Equals(
            board.OperationKind,"REPEATABLE")
            ?(catalog.RewardPolicy.TravelSupplyRewardPerCompletedOperation*
                expectedPermille)/10000
            :catalog.RewardPolicy.TravelSupplyRewardPerCompletedOperation;
        var nextStreakId=StringComparer.Ordinal.Equals(
            board.OperationKind,"REPEATABLE")?entry.DefinitionId:string.Empty;
        var nextStreakCount=StringComparer.Ordinal.Equals(
            board.OperationKind,"REPEATABLE")
            ?(StringComparer.Ordinal.Equals(streakId,entry.DefinitionId)
                ?streakCount+1:1):0;
        var nextCompleted=completed.Concat(new[]{entry.DefinitionId})
            .Distinct(StringComparer.Ordinal).OrderBy(value=>value,
                StringComparer.Ordinal).ToArray();
        var nextWorlds=worlds.Concat(new[]{entry.WorldId})
            .Distinct(StringComparer.Ordinal).OrderBy(value=>value,
                StringComparer.Ordinal).ToArray();
        var nextOrigins=new List<string>(origins);
        if(catalog.TryGetRecruitUnlock(entry.WorldId,out var unlock)&&
           StandingIndex(TierFor(entry.FinalTrust,entry.FinalTension,
                 entry.FinalCivilianSupport,catalog,entry.WorldId),catalog,
                 entry.WorldId)>=StandingIndex(unlock.MinimumStandingTier,catalog,
                 entry.WorldId))
         foreach(var origin in unlock.OriginProfileIds)
          if(!nextOrigins.Contains(origin))nextOrigins.Add(origin);
        nextOrigins.Sort(StringComparer.Ordinal);
        if(proof.RewardPermilleAtBegin!=expectedPermille||
           entry.TravelSuppliesAfter!=travelSupplies+expectedTravelReward||
           !StringComparer.Ordinal.Equals(
               entry.RepeatableStreakDefinitionIdAfter,nextStreakId)||
           entry.RepeatableStreakCountAfter!=nextStreakCount||
           !SameIds084(entry.UnlockedOriginsAfter,nextOrigins)||
           !SameIds084(entry.UnlockedWorldsAfter,nextWorlds)||
           !SameIds084(entry.CompletedDefinitionsAfter,nextCompleted)||
           entry.FinalTrust!=proof.FinalTrust||
           entry.FinalTension!=proof.FinalTension||
           entry.FinalCivilianSupport!=proof.FinalCivilianSupport)return false;
        standings.RemoveAll(value=>StringComparer.Ordinal.Equals(
            value.WorldId,entry.WorldId));
        standings.Add(new WorldStandingState023(entry.WorldId,entry.FinalTrust,
            entry.FinalTension,entry.FinalCivilianSupport,
            TierFor(entry.FinalTrust,entry.FinalTension,
                entry.FinalCivilianSupport,catalog,entry.WorldId)));
        origins=nextOrigins.ToArray();worlds=nextWorlds;completed=nextCompleted;
        travelSupplies+=expectedTravelReward;streakId=nextStreakId;
        streakCount=nextStreakCount;previousOrdinal=entry.CommittedOperationOrdinal;
       }
       previous=entry.EntryHash;
       sequence=entry.SequenceIndex;
      }
     if(!StringComparer.Ordinal.Equals(previous,runtime.AuthorityChainHash))
      return false;
     var active=runtime.ActiveOperation;
      if(active!=null)
      {
      var standing=standings.FirstOrDefault(value=>
          StringComparer.Ordinal.Equals(value.WorldId,active.WorldId))??
          new WorldStandingState023(active.WorldId,0,25,0,
              "RECOVERABLE_FRACTURE");
      if(!StringComparer.Ordinal.Equals(active.PreviousAuthorityChainHash,previous)||
         active.InitialTrust!=standing.Trust||
         active.InitialTension!=standing.Tension||
         active.InitialCivilianSupport!=standing.CivilianSupport)return false;
      standings.RemoveAll(value=>StringComparer.Ordinal.Equals(
          value.WorldId,active.WorldId));
      standings.Add(new WorldStandingState023(active.WorldId,active.Trust,
          active.Tension,active.CivilianSupport,
          TierFor(active.Trust,active.Tension,active.CivilianSupport,catalog,
              active.WorldId)));
      }
      if(active!=null&&active.PreviousAuthorityChainHash!=previous)return false;
     standings.Sort((left,right)=>StringComparer.Ordinal.Compare(
         left.WorldId,right.WorldId));
     var runtimeStandings=runtime.WorldStandings.OrderBy(value=>value.WorldId,
         StringComparer.Ordinal).ToArray();
     if(standings.Count!=runtimeStandings.Length)return false;
     for(var index=0;index<standings.Count;index++)
      if(!StringComparer.Ordinal.Equals(standings[index].WorldId,
             runtimeStandings[index].WorldId)||
         standings[index].Trust!=runtimeStandings[index].Trust||
         standings[index].Tension!=runtimeStandings[index].Tension||
         standings[index].CivilianSupport!=runtimeStandings[index].CivilianSupport||
         !StringComparer.Ordinal.Equals(standings[index].TierId,
             runtimeStandings[index].TierId))return false;
      return StringComparer.Ordinal.Equals(currentWorld,runtime.CurrentWorldId)&&
             travelSupplies==runtime.TravelSupplies&&
             StringComparer.Ordinal.Equals(streakId,
                 runtime.RepeatableStreakDefinitionId)&&
             streakCount==runtime.RepeatableStreakCount&&
             previousOrdinal<=campaign.Guild.GuildCity.OperationOrdinal&&
             SameIds084(origins,runtime.UnlockedRecruitOriginIds)&&
             SameIds084(worlds,runtime.UnlockedWorldIds)&&
             SameIds084(completed,runtime.CompletedDefinitionIds);
     }

     static int ExpectedRewardPermilleForStreak084(string operationKind,
         string definitionId,WorldGateRewardPolicy023 policy,string streakId,
         int streakCount)
     {
      if(!StringComparer.Ordinal.Equals(operationKind,"REPEATABLE"))return 10000;
      var basis=policy?.RepeatableConsecutiveRewardBasisPoints;
      if(basis==null||basis.Count==0)return 10000;
      var index=StringComparer.Ordinal.Equals(streakId,definitionId)
          ?streakCount:0;
      return Math.Max(0,basis[Math.Min(index,basis.Count-1)]);
     }

     static string TravelOperationId084(string previousHash,int sequence,
         string fromWorldId,string destinationWorldId,int supplyCost)=>
         "WGTRAVEL023_"+CanonicalJson.Sha256Hex(new
         {
          PreviousHash=previousHash,Sequence=sequence,From=fromWorldId,
          To=destinationWorldId,SupplyCost=supplyCost
         }).Substring(0,24).ToUpperInvariant();

    public static bool ValidateStoredCompletionProofs084(
        CampaignState campaign,IWorldGateOperationsCatalog023 catalog,
        WorldGateRuntimeState023 runtime)
    {
     var proofs=runtime?.CompletionProofs;
      return proofs!=null&&proofs.Count<=82&&
            proofs.Select(value=>value?.DefinitionId)
                .Distinct(StringComparer.Ordinal).Count()==proofs.Count&&
             proofs.Select(value=>value?.OperationId)
                 .Distinct(StringComparer.Ordinal).Count()==proofs.Count&&
             proofs.All(value=>value!=null&&
                 catalog.TryGetBoard(value.DefinitionId,out var board)&&board!=null&&
                  StringComparer.Ordinal.Equals(board.OperationKind,"CHAPTER")&&
                    ValidateCompletionProof084(campaign,catalog,value,false))&&
              LatestChapterProofProjection130(runtime)&&
              ValidateAuthorityChain084(campaign,catalog,runtime);
    }

    static bool LatestChapterProofProjection130(WorldGateRuntimeState023 runtime)
    {
     // One fresh index per validation; never cache mutable campaign state.
     var latest=new Dictionary<string,WorldGateAuthorityEntry023>(StringComparer.Ordinal);
     foreach(var entry in runtime.AuthorityEntries)
     {
      if(entry==null)return false;
      if(StringComparer.Ordinal.Equals(entry.OperationKind,"CHAPTER"))
       latest[entry.DefinitionId]=entry;
     }
     if(latest.Count!=runtime.CompletionProofs.Count)return false;
     return runtime.CompletionProofs.All(proof=>
         latest.TryGetValue(proof.DefinitionId,out var entry)&&
         StringComparer.Ordinal.Equals(entry.OperationId,proof.OperationId)&&
         StringComparer.Ordinal.Equals(entry.CompletionLedgerHash,proof.CompletionLedgerHash));
    }

    static bool ValidateDelegatedCheckProof093(CampaignState campaign,
        WorldGateCompletionProof023 proof)
    {
     var ids=new HashSet<string>(StringComparer.Ordinal);
     var nodes=new HashSet<string>(StringComparer.Ordinal);
     foreach(var card in proof.DelegatedCheckReceipts093??Array.Empty<ExpeditionCardReceipt089>())
     {
      if(card==null||!card.DelegatedToWorldGateCheck||card.RequiresCertifiedBattle||
         card.BaseCheckModifier==0||card.OperationId!=proof.OperationId||
         !ids.Add(card.ReceiptId)||!nodes.Add(card.NodeId)||
         !campaign.Guild.Development.HasAdventureAuthority(card.ReceiptId))return false;
      var route=proof.AppliedReceipts.FirstOrDefault(value=>value.NodeId==card.NodeId);
      if(!ExpeditionDeckService089.ValidateAppliedRouteFatePair132(card,route))return false;
      if(route==null||route.ChoiceId!=card.ChoiceId||
         route.ActorRecruitId!=card.ActorRecruitId||
         route.AssistantRecruitId!=card.AssistantRecruitId||
         route.DieOne!=card.DieOne||route.DieTwo!=card.DieTwo||
         route.Modifier!=card.EffectiveModifier||route.Outcome!=card.Outcome||
         !ExpeditionDeckService089.TryAuthorizedWorldGateModifier093(proof.OperationId,
             new[]{card},card.NodeId,card.ChoiceId,card.ActorRecruitId,
             card.AssistantRecruitId,out var modifier)||modifier!=card.BaseCheckModifier)
       return false;
     }
     return true;
    }

    static string CompletionLedgerHash084(WorldGateCompletionProof023 proof) =>
        (proof.DelegatedCheckReceipts093?.Count??0)>0
            ?CanonicalJson.Sha256Hex(new
            {
             RouteProofHash=LegacyCompletionLedgerHash093(proof),
             proof.OptionalBattleCosts093,proof.DelegatedCheckReceipts093
            })
            :proof.OptionalBattleCosts093==null||proof.OptionalBattleCosts093.Count==0
            ?LegacyCompletionLedgerHash093(proof)
            :CanonicalJson.Sha256Hex(new
            {
             RouteProofHash=LegacyCompletionLedgerHash093(proof),
             proof.OptionalBattleCosts093
            });

    static string LegacyCompletionLedgerHash093(WorldGateCompletionProof023 proof) =>
        CanonicalJson.Sha256Hex(new
        {
         proof.ProofVersion,proof.OperationId,proof.DefinitionId,proof.BoardId,
         proof.WorldId,proof.CommittedOperationOrdinal,proof.InitialTrust,
         proof.InitialTension,proof.InitialCivilianSupport,proof.AlliedUnionIds,
          proof.RewardPermilleAtBegin,proof.AlliedRosterIdentity,
           proof.AlliedRecruitIds,proof.PreviousAuthorityChainHash,
         proof.CanonicalSeedIdentity,proof.StartNodeId,proof.ExitNodeId,
         proof.CompletedNodeIds,proof.AppliedReceipts,proof.InitialSupplies,
         proof.FinalSupplies,proof.FinalFatigue,proof.FinalUrgency,
         proof.FinalThreat,proof.FinalTrust,proof.FinalTension,
         proof.FinalCivilianSupport,proof.ExistingBattleRewardReceiptId,
         proof.BattleId,proof.BattleFinalStateHash,proof.BattleOutcome,
         proof.BattleRewardId
        });
    static bool ValidateAppliedLedger084(CampaignState campaign,IWorldGateOperationsCatalog023 catalog,WorldGateRuntimeState023 runtime,WorldGateOperationState023 op,WorldGateBoardRule023 board)
    {
     if(!WorldGateOptionalBattleCost093.Validate(campaign,op,board,
            op.OptionalBattleCosts093))return false;
     if(op.AppliedReceipts.Count!=op.AppliedReceiptIds.Count)return false;
      if(op.AppliedReceipts.Select(value=>value.NodeId).Distinct(StringComparer.Ordinal)
             .Count()!=op.AppliedReceipts.Count)return false;
      var byNode=op.AppliedReceipts.ToDictionary(value=>value.NodeId,value=>value,
          StringComparer.Ordinal);
     if(byNode.Count!=op.AppliedReceipts.Count||
        !op.AppliedReceipts.Select(value=>value.ReceiptId).OrderBy(value=>value,
            StringComparer.Ordinal).SequenceEqual(op.AppliedReceiptIds,
                StringComparer.Ordinal))return false;
     var cursor=board.StartNodeId;
     var supplies=catalog.RewardPolicy.TravelSupplyReserveDefault;
     var fatigue=0;var urgency=0;var threat=0;var trust=op.InitialTrust;
     var tension=op.InitialTension;var support=op.InitialCivilianSupport;
     for(var guard=0;guard<op.CompletedNodeIds.Count;guard++)
     {
       if(!WorldGateOptionalBattleCost093.ApplyAtNode(op.OptionalBattleCosts093,
              cursor,ref supplies,ref fatigue,ref urgency))return false;
       if(!byNode.TryGetValue(cursor,out var receipt)||
          !campaign.Guild.Development.HasAdventureAuthority(receipt.ReceiptId))
        return false;
      var node=board.Nodes.FirstOrDefault(value=>
          StringComparer.Ordinal.Equals(value.NodeId,cursor));
      if(node==null||!ValidatePendingReceipt084(campaign,catalog,runtime,op,node,
             receipt,out _))return false;
      supplies=Math.Max(0,supplies+receipt.SupplyDelta);
      fatigue=Math.Max(0,fatigue+receipt.FatigueDelta);
      urgency=Math.Max(0,urgency+receipt.UrgencyDelta);
      threat=Math.Max(0,threat+receipt.ThreatDelta);
      trust=Math.Max(-100,Math.Min(100,trust+receipt.TrustDelta));
      tension=Math.Max(0,Math.Min(100,tension+receipt.TensionDelta));
      support=Math.Max(-100,Math.Min(100,support+
          receipt.CivilianSupportDelta));
      cursor=StringComparer.Ordinal.Equals(node.Kind,"EXIT")||
             string.IsNullOrWhiteSpace(receipt.NextNodeId)
          ?cursor:receipt.NextNodeId;
     }
      if(!op.CompletedNodeIds.Contains(cursor)&&
         !WorldGateOptionalBattleCost093.ApplyAtNode(op.OptionalBattleCosts093,
             cursor,ref supplies,ref fatigue,ref urgency))return false;
      var standings=runtime.WorldStandings.Where(value=>
          StringComparer.Ordinal.Equals(value.WorldId,op.WorldId)).ToArray();
      return standings.Length==1&&StringComparer.Ordinal.Equals(cursor,op.CurrentNodeId)&&
             supplies==op.Supplies&&fatigue==op.Fatigue&&urgency==op.Urgency&&
             threat==op.Threat&&trust==op.Trust&&tension==op.Tension&&
             support==op.CivilianSupport&&standings[0].Trust==op.Trust&&
             standings[0].Tension==op.Tension&&
             standings[0].CivilianSupport==op.CivilianSupport&&
             StringComparer.Ordinal.Equals(standings[0].TierId,
                 TierFor(op.Trust,op.Tension,op.CivilianSupport,catalog,op.WorldId));
    }
    static EncounterLaunchRequest017D CreateEncounterRequest084(CampaignState campaign,GuildCityState017D city,WorldGateOperationState023 op,WorldGateNodeRule023 node,string preBattleStateHash,bool newCommit094=false)
    {
     var route=new List<string>{"WORLD_GATE_OPERATION_023","WORLD_"+op.WorldId,
         "TRUST_"+op.Trust,"TENSION_"+op.Tension,"SUPPORT_"+op.CivilianSupport};
     if(op.Trust>=40)route.Add("LOCAL_SUPPORT");if(op.Tension>=60)route.Add("HIGH_TENSION");
     if(op.Supplies<=5)route.Add("LOW_SUPPLIES");
     // The contract ordinal belongs to this committed quest, including when
     // reconstructing a saved request after later campaign runs have begun.
     var replayProgress130=campaign.Guild.GuildCity.Strategic017H.Campaign019;
     var replayCycle130=StringComparer.Ordinal.Equals(op.OperationKind,"CHAPTER")
         ?CampaignReplayRules130.CycleForCommittedOrdinal(replayProgress130,
             city.ActiveContract.AcceptedOperationOrdinal):1;
     var committedRoute130=CampaignReplayThreat130.AppendFrozenRoutes132(route.AsReadOnly(),
         replayProgress130,replayCycle130);
     var hash=CanonicalJson.Sha256Hex(new{op.OperationId,node.NodeId,
         op.CanonicalSeedIdentity});
     var legacy094=new EncounterLaunchRequest017D("WG_ENCOUNTER023_"+
         hash.Substring(0,24).ToUpperInvariant(),op.DefinitionId,
         city.Expedition.ExpeditionId,op.BoardId,node.NodeId,
         string.IsNullOrWhiteSpace(node.SourceId)?node.NodeId:node.SourceId,
         "WORLD_GATE_BATTLE_023_"+hash.Substring(0,18).ToUpperInvariant(),
         node.Objective,CampaignReplayThreat130.MinimumUnionCount132(Math.Max(1,Math.Min(10,node.EnemyUnionCount)),committedRoute130),
         op.CanonicalSeedIdentity,op.AlliedUnionIds,Array.Empty<string>(),
         new[]{"OBJ023_"+node.NodeId},committedRoute130,op.Supplies,op.Fatigue,
         op.Urgency,"world_gate_023_return_"+node.NodeId,preBattleStateHash);
     return EnemyForceProfile094.ForRequest094(campaign,legacy094,
         CampaignReplayThreat130.ForceChapter132(EnemyForceProfile094.CampaignChapter094(campaign,op.DefinitionId),committedRoute130),newCommit094);
    }
    static bool SameEncounter084(EncounterLaunchRequest017D actual,EncounterLaunchRequest017D expected)=>
        actual!=null&&expected!=null&&
        StringComparer.Ordinal.Equals(CanonicalJson.Serialize(actual),CanonicalJson.Serialize(expected));
    static bool MatchesExpeditionResources084(CampaignState campaign,GuildCityState017D city,WorldGateOperationState023 op,ExpeditionState017D expedition)
    {
     if(expedition.Supplies==op.Supplies&&expedition.Fatigue==op.Fatigue&&
        expedition.Urgency==op.Urgency&&expedition.Threat==op.Threat)return true;
     if(op.Status!=WorldGateOperationStatus023.AwaitingBattle||
        city.PendingEncounter!=null||city.PendingBattleReturn!=null||
        campaign?.Battle?.Reward==null||!campaign.Battle.Reward.Claimed||
        !M2BattleCommandService.HasValidFinalStateHash090(campaign.Battle))return false;
     var encounterHash=CanonicalJson.Sha256Hex(new
     {
      op.OperationId,NodeId=op.CurrentNodeId,op.CanonicalSeedIdentity
     });
     var requestId="WG_ENCOUNTER023_"+
         encounterHash.Substring(0,24).ToUpperInvariant();
     var battleId="WORLD_GATE_BATTLE_023_"+
         encounterHash.Substring(0,18).ToUpperInvariant();
     if(!StringComparer.Ordinal.Equals(campaign.Battle.BattleId,battleId))return false;
     var returnHash=CanonicalJson.Sha256Hex(new
     {
      RequestId=requestId,
      BattleId=battleId,
      campaign.Battle.FinalStateHash,
      campaign.Battle.Outcome,
      RewardId=campaign.Battle.Reward.RewardId
     });
     var returnId="BATTLE_RETURN_"+returnHash.Substring(0,24).ToUpperInvariant();
     if(city.AppliedBattleReturnIds.Count(value=>
            StringComparer.Ordinal.Equals(value,returnId))!=1)return false;
     var expectedStatus=campaign.Battle.Outcome==BattleOutcome.Defeat
         ?ExpeditionStatus017D.Failed:ExpeditionStatus017D.Active;
     return expedition.Status==expectedStatus&&
            expedition.Supplies==Math.Max(0,op.Supplies-1)&&
            expedition.Fatigue==Math.Max(0,op.Fatigue+
                (campaign.Battle.Outcome==BattleOutcome.Victory?1:2))&&
            expedition.Urgency==Math.Max(0,op.Urgency-1)&&
            expedition.Threat==op.Threat;
    }
   static IReadOnlyList<string> ValidateUnions(CampaignState c,IReadOnlyList<string> ids){var r=new List<string>();if(ids!=null)for(var i=0;i<ids.Count;i++){var id=ids[i];if(string.IsNullOrWhiteSpace(id)||r.Contains(id))continue;if(c.Guild.Unions.Any(x=>x!=null&&x.MemberRecruitIds.Count>0&&StringComparer.Ordinal.Equals(x.UnionId,id)))r.Add(id);}r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
   static bool TryAlliedRosterSnapshot084(
       CampaignState campaign,IReadOnlyList<string> alliedUnionIds,
       out string identity,out IReadOnlyList<string> recruitIds)
   {
    identity=string.Empty;
    recruitIds=Array.Empty<string>();
    if(campaign?.Guild==null||alliedUnionIds==null||alliedUnionIds.Count==0)
     return false;
    var rows=new List<string>();
    var recruits=new List<string>();
    foreach(var unionId in alliedUnionIds.OrderBy(value=>value,StringComparer.Ordinal))
    {
     var matches=campaign.Guild.Unions.Where(value=>value!=null&&
         StringComparer.Ordinal.Equals(value.UnionId,unionId)).ToArray();
     if(matches.Length!=1)return false;
     var union=matches[0];
     var members=(union.MemberRecruitIds??Array.Empty<string>())
         .Where(value=>!string.IsNullOrWhiteSpace(value))
         .OrderBy(value=>value,StringComparer.Ordinal).ToArray();
     if(members.Length==0||members.Distinct(StringComparer.Ordinal).Count()!=members.Length||
        string.IsNullOrWhiteSpace(union.LeaderRecruitId)||
        !members.Contains(union.LeaderRecruitId,StringComparer.Ordinal))return false;
     for(var index=0;index<members.Length;index++)
      if(campaign.Guild.Recruits.Count(value=>value!=null&&
             StringComparer.Ordinal.Equals(value.RecruitId,members[index]))!=1)
       return false;
      else if(!recruits.Contains(members[index]))recruits.Add(members[index]);
     rows.Add(unionId+"|"+union.LeaderRecruitId+"|"+
         (union.FormationId??string.Empty)+"|"+(union.DoctrineId??string.Empty)+"|"+
         string.Join(",",members));
    }
    recruits.Sort(StringComparer.Ordinal);
    identity=CanonicalJson.Sha256Hex(rows);
    recruitIds=recruits.AsReadOnly();
    return !string.IsNullOrWhiteSpace(identity)&&recruitIds.Count>0;
   }
   static bool OwnsRecruit(CampaignState c,string id)=>c.Guild.Recruits.Any(x=>x!=null&&StringComparer.Ordinal.Equals(x.RecruitId,id));
   static bool IsCommittedRecruit084(CampaignState campaign,WorldGateOperationState023 operation,string recruitId)
   {
    return campaign?.Guild!=null&&operation!=null&&
           !string.IsNullOrWhiteSpace(recruitId)&&
           operation.AlliedRecruitIds.Contains(recruitId);
   }
  static bool ValidatePendingReceipt084(
      CampaignState campaign,
      IWorldGateOperationsCatalog023 catalog,
      WorldGateRuntimeState023 runtime,
      WorldGateOperationState023 op,
      WorldGateNodeRule023 node,
      WorldGateNodeReceipt023 receipt,
      out string error,
      IReadOnlyList<ExpeditionCardReceipt089> delegatedCheckReceipts093=null)
  {
   error="CAMPAIGN023_PENDING_RECEIPT_INVALID";
   if(receipt==null||receipt.AppliedVersion!=0||
      !StringComparer.Ordinal.Equals(receipt.OperationId,op.OperationId)||
      !StringComparer.Ordinal.Equals(receipt.NodeId,node.NodeId)||
      !IsCanonicalReceiptId084(receipt.ReceiptId))return false;
   var choices=node.ChoiceIds??Array.Empty<string>();
    if((choices.Count>0&&!choices.Contains(receipt.ChoiceId))||
       (choices.Count==0&&!StringComparer.Ordinal.Equals(receipt.ChoiceId,"CONTINUE")))return false;
   if(!BoardAdventureRules084.TryNextNode084(node,receipt.ChoiceId,out var next)||
      !StringComparer.Ordinal.Equals(next,receipt.NextNodeId))return false;

   string hash;
   int supply;
   int fatigue;
   int urgency;
   int threat;
   int trust;
   int tension;
   int support;
   int guildXp;
   int hallXp;
   IReadOnlyList<string> materials;
    if(node.RequiresCertifiedBattle)
    {
     if(campaign.Battle?.Reward==null||!campaign.Battle.Reward.Claimed||
       string.IsNullOrWhiteSpace(op.ExistingBattleRewardReceiptId)||
       !StringComparer.Ordinal.Equals(op.ExistingBattleRewardReceiptId,
           campaign.Battle.Reward.RewardId)||
       receipt.DieOne!=0||receipt.DieTwo!=0||receipt.Modifier!=0||
       !string.IsNullOrWhiteSpace(receipt.ActorRecruitId)||
         !string.IsNullOrWhiteSpace(receipt.AssistantRecruitId)||
         !StringComparer.Ordinal.Equals(receipt.Outcome,
             campaign.Battle.Outcome.ToString().ToUpperInvariant()))return false;
      if(string.IsNullOrWhiteSpace(receipt.BattleReturnAuthorityId)||
         !campaign.Guild.Development.HasAdventureAuthority(
             receipt.BattleReturnAuthorityId))return false;
     var encounterHash=CanonicalJson.Sha256Hex(new
     {
      op.OperationId,node.NodeId,op.CanonicalSeedIdentity
     });
     var expectedBattleId="WORLD_GATE_BATTLE_023_"+
         encounterHash.Substring(0,18).ToUpperInvariant();
     if(!StringComparer.Ordinal.Equals(campaign.Battle.BattleId,expectedBattleId)||
        !M2BattleCommandService.HasValidFinalStateHash090(campaign.Battle)||
        !campaign.Guild.Development.HasClaimedReward(
            campaign.Battle.Reward.RewardId))return false;
    hash=CanonicalJson.Sha256Hex(new
    {
     op.OperationId,
     node.NodeId,
     Choice=receipt.ChoiceId,
     Next=next,
     BattleOutcome=campaign.Battle.Outcome.ToString(),
     campaign.Battle.FinalStateHash,
     ExistingReward=campaign.Battle.Reward.RewardId,
      BattleReturnAuthority=receipt.BattleReturnAuthorityId,
      op.CanonicalSeedIdentity
    });
    var victory=campaign.Battle.Outcome==BattleOutcome.Victory;
    var battleDecay=RewardPermille(op,catalog.RewardPolicy,runtime);
    int ScaleBattle(int value)=>Math.Max(0,(value*battleDecay)/10000);
    supply=node.SupplyDelta;
    fatigue=victory?node.FatigueDelta:Math.Max(2,node.FatigueDelta+1);
    urgency=node.UrgencyDelta;
     threat=victory?node.ThreatDelta:Math.Max(-1,node.ThreatDelta);
    trust=victory?node.TrustDelta:Math.Min(0,node.TrustDelta);
    tension=victory?node.TensionDelta:Math.Max(0,node.TensionDelta);
    support=victory?node.CivilianSupportDelta:Math.Min(0,node.CivilianSupportDelta);
    guildXp=victory?ScaleBattle(node.GuildXp):0;
    hallXp=victory?ScaleBattle(node.HallXp):0;
    materials=victory?ScaleMaterials084(node.MaterialIds,battleDecay):Array.Empty<string>();
   }
    else
    {
      if(!string.IsNullOrWhiteSpace(receipt.BattleReturnAuthorityId))return false;
      if(node.CheckDifficulty<=0&&
        (!string.IsNullOrWhiteSpace(receipt.ActorRecruitId)||
         !string.IsNullOrWhiteSpace(receipt.AssistantRecruitId)||
         receipt.DieOne!=0||receipt.DieTwo!=0||receipt.Modifier!=0))return false;
     if(!string.IsNullOrWhiteSpace(receipt.ActorRecruitId)&&
        !IsCommittedRecruit084(campaign,op,receipt.ActorRecruitId))return false;
     if(!string.IsNullOrWhiteSpace(receipt.AssistantRecruitId)&&
        (!IsCommittedRecruit084(campaign,op,receipt.AssistantRecruitId)||
         StringComparer.Ordinal.Equals(receipt.ActorRecruitId,
             receipt.AssistantRecruitId)))return false;
    var outcome="SUCCESS";
    if(node.CheckDifficulty>0)
    {
    if(receipt.DieOne<1||receipt.DieOne>6||receipt.DieTwo<1||receipt.DieTwo>6||
       string.IsNullOrWhiteSpace(receipt.ActorRecruitId))return false;
    var checkSeed=SemanticSeed.Derive(
        campaign.CampaignSeed,op.OperationId,node.NodeId,receipt.ChoiceId,
        receipt.ActorRecruitId,receipt.AssistantRecruitId??string.Empty);
    var checkRng=new Pcg32(checkSeed.Seed,checkSeed.Stream);
    if(receipt.DieOne!=checkRng.NextInclusive(1,6)||
       receipt.DieTwo!=checkRng.NextInclusive(1,6))return false;
    int expectedBaseModifier;
    var modifierAuthorized=delegatedCheckReceipts093==null
        ?ExpeditionDeckService089.TryAuthorizedWorldGateModifier(op,node.NodeId,
            receipt.ChoiceId,receipt.ActorRecruitId,receipt.AssistantRecruitId,
            out expectedBaseModifier)
        :ExpeditionDeckService089.TryAuthorizedWorldGateModifier093(op.OperationId,
            delegatedCheckReceipts093,node.NodeId,receipt.ChoiceId,
            receipt.ActorRecruitId,receipt.AssistantRecruitId,out expectedBaseModifier);
    if(!modifierAuthorized)return false;
    var expectedModifier=expectedBaseModifier+
        (!string.IsNullOrWhiteSpace(receipt.AssistantRecruitId)&&
         !StringComparer.Ordinal.Equals(
             receipt.ActorRecruitId,receipt.AssistantRecruitId)?1:0);
    if(receipt.Modifier!=expectedModifier)return false;
     var total=receipt.DieOne+receipt.DieTwo+receipt.Modifier;
     outcome=total>=node.CheckDifficulty+4?"EXCEPTIONAL":
         total>=node.CheckDifficulty+2?"FULL_SUCCESS":
         total>=node.CheckDifficulty?"SUCCESS_WITH_COST":
         total>=node.CheckDifficulty-2?"SETBACK":"SEVERE_SETBACK";
    }
   else if(receipt.DieOne!=0||receipt.DieTwo!=0||receipt.Modifier!=0)return false;
    if(!StringComparer.Ordinal.Equals(receipt.Outcome,outcome))return false;
    var choice=receipt.ChoiceId;
    var die1=receipt.DieOne;
    var die2=receipt.DieTwo;
    var effective=receipt.Modifier;
    hash=CanonicalJson.Sha256Hex(new
    {
     op.OperationId,node.NodeId,choice,next,outcome,die1,die2,effective,
     op.CanonicalSeedIdentity
    });
    var decay=RewardPermille(op,catalog.RewardPolicy,runtime);
    var setback=outcome=="SETBACK"||outcome=="SEVERE_SETBACK";
    var severe=outcome=="SEVERE_SETBACK";
    int Scale(int value)=>Math.Max(0,(value*decay)/10000);
    supply=node.SupplyDelta-(setback?1:0);
    fatigue=node.FatigueDelta+(setback?1:0)+(severe?1:0);
    urgency=node.UrgencyDelta;
    threat=node.ThreatDelta;
    trust=setback?Math.Min(0,node.TrustDelta):node.TrustDelta;
    tension=node.TensionDelta+(setback?1:0);
    support=setback?Math.Min(0,node.CivilianSupportDelta):node.CivilianSupportDelta;
    guildXp=Scale(node.GuildXp);
    hallXp=Scale(node.HallXp);
    materials=ScaleMaterials084(node.MaterialIds,decay);
   }
   if(!StringComparer.Ordinal.Equals(receipt.AuthoritativeHash,hash)||
      !StringComparer.Ordinal.Equals(receipt.ReceiptId,
          "WGREC023_"+hash.Substring(0,24).ToUpperInvariant())||
      receipt.SupplyDelta!=supply||receipt.FatigueDelta!=fatigue||
      receipt.UrgencyDelta!=urgency||receipt.ThreatDelta!=threat||
      receipt.TrustDelta!=trust||receipt.TensionDelta!=tension||
      receipt.CivilianSupportDelta!=support||receipt.GuildXp!=guildXp||
      receipt.HallXp!=hallXp||!SameIds084(receipt.MaterialIds,materials))return false;
   error=string.Empty;
   return true;
  }
  static bool IsCanonicalReceiptId084(string value)
  {
   const string prefix="WGREC023_";
   if(string.IsNullOrWhiteSpace(value)||!value.StartsWith(prefix,StringComparison.Ordinal)||
      value.Length!=prefix.Length+24)return false;
   for(var index=prefix.Length;index<value.Length;index++)
    if(!((value[index]>='0'&&value[index]<='9')||
         (value[index]>='A'&&value[index]<='F')))return false;
   return true;
  }
   static bool SameIds084(IReadOnlyList<string> first,IReadOnlyList<string> second)
  {
   var left=(first??Array.Empty<string>()).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
   var right=(second??Array.Empty<string>()).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
   return left.SequenceEqual(right,StringComparer.Ordinal);
  }
  static bool SameStandings084(IReadOnlyList<WorldStandingState023> first,
      IReadOnlyList<WorldStandingState023> second)
  {
   var left=(first??Array.Empty<WorldStandingState023>()).OrderBy(
       value=>value.WorldId,StringComparer.Ordinal).ToArray();
   var right=(second??Array.Empty<WorldStandingState023>()).OrderBy(
       value=>value.WorldId,StringComparer.Ordinal).ToArray();
   if(left.Length!=right.Length)return false;
   for(var index=0;index<left.Length;index++)
    if(left[index]==null||right[index]==null||
       !StringComparer.Ordinal.Equals(left[index].WorldId,right[index].WorldId)||
       left[index].Trust!=right[index].Trust||
       left[index].Tension!=right[index].Tension||
       left[index].CivilianSupport!=right[index].CivilianSupport||
       !StringComparer.Ordinal.Equals(left[index].TierId,right[index].TierId))
     return false;
   return true;
  }
  static IReadOnlyList<string> ScaleMaterials084(IReadOnlyList<string> source,int rewardPermille)
  {
   if(source==null||source.Count==0||rewardPermille<=0)return Array.Empty<string>();
   var values=source.Where(value=>!string.IsNullOrWhiteSpace(value))
       .Distinct(StringComparer.Ordinal).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
   if(values.Length==0)return Array.Empty<string>();
   var count=BoardAdventureRules084.ScaledMaterialCount084(values.Length,rewardPermille);
   return values.Take(count).ToArray();
  }
   static int RewardPermille(WorldGateOperationState023 op,WorldGateRewardPolicy023 p,WorldGateRuntimeState023 r)=>op!=null&&op.RewardPermilleAtBegin>=0?op.RewardPermilleAtBegin:BoardAdventureRules084.RewardPermille084(op,p,r);
    static int RewardPermilleAtBegin084(string operationKind,string definitionId,WorldGateRewardPolicy023 policy,WorldGateRuntimeState023 runtime)
    =>BoardAdventureRules084.RewardPermilleForNextRun084(
     operationKind,definitionId,policy,runtime);
   static bool SameNodeLedger084(IReadOnlyList<WorldGateNodeReceipt023> first,
       IReadOnlyList<WorldGateNodeReceipt023> second)
   {
    var left=first??Array.Empty<WorldGateNodeReceipt023>();
    var right=second??Array.Empty<WorldGateNodeReceipt023>();
    return left.Count==right.Count&&StringComparer.Ordinal.Equals(
        CanonicalJson.Serialize(left),CanonicalJson.Serialize(right));
   }
  static bool WorldUnlocked(CampaignProgressState019 progress,CampaignPlayableState020 p,WorldGateRuntimeState023 r,string id)=>StringComparer.Ordinal.Equals(id,"SKYHOME")||(progress?.UnlockedWorldIds?.Contains(id)??false)||r.UnlockedWorldIds.Contains(id)||p.WorldGates.Any(x=>x.WorldId==id&&x.Unlocked);
  static IReadOnlyList<GuildMaterialState017D> MergeMaterials(IReadOnlyList<GuildMaterialState017D> existing,IReadOnlyList<string> ids){var r=new List<GuildMaterialState017D>(existing);if(ids!=null)for(var i=0;i<ids.Count;i++){var id=ids[i];if(string.IsNullOrWhiteSpace(id))continue;var x=r.FirstOrDefault(v=>v.MaterialId==id);if(x!=null){r.Remove(x);r.Add(x.WithAmount(x.Amount+1));}else r.Add(new GuildMaterialState017D(id,1));}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId));return r.AsReadOnly();}
  static IReadOnlyList<RelationshipMemoryState017D> MergeMemory(IReadOnlyList<RelationshipMemoryState017D> existing,WorldGateNodeReceipt023 receipt,int ordinal,WorldGateNodeRule023 node){var r=new List<RelationshipMemoryState017D>(existing);if(!string.IsNullOrWhiteSpace(receipt.ActorRecruitId)&&!string.IsNullOrWhiteSpace(receipt.AssistantRecruitId)&&!StringComparer.Ordinal.Equals(receipt.ActorRecruitId,receipt.AssistantRecruitId)){var id="REL_WG023_"+receipt.ReceiptId.Substring(Math.Max(0,receipt.ReceiptId.Length-20));if(!r.Any(x=>x.MemoryId==id))r.Add(new RelationshipMemoryState017D(id,receipt.ActorRecruitId,receipt.AssistantRecruitId,node.SourceId??node.NodeId,"They handled "+node.Title+" together without consuming a separate day.",ordinal,receipt.Outcome.Contains("SUCCESS")?2:1,"REL_SCENE_"+id,false));}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MemoryId,b.MemoryId));return r.AsReadOnly();}
   static WorldGateRuntimeState023 UpdateStanding(WorldGateRuntimeState023 r,string worldId,int trust,int tension,int support,IWorldGateOperationsCatalog023 c){var list=new List<WorldStandingState023>(r.WorldStandings.Where(x=>!StringComparer.Ordinal.Equals(x.WorldId,worldId)));list.Add(new WorldStandingState023(worldId,trust,tension,support,TierFor(trust,tension,support,c,worldId)));list.Sort((a,b)=>StringComparer.Ordinal.Compare(a.WorldId,b.WorldId));return r.With(worldStandings:list.AsReadOnly());}
   static string TierFor(int trust,int tension,int support,IWorldGateOperationsCatalog023 c,string worldId){if(c==null||!c.TryGetStanding(worldId,out var s)||s.TierIds.Count==0)return trust>=60&&tension<=25?"ALLIANCE_NETWORK":trust>=30&&tension<=50?"INDEPENDENT_COOPERATION":trust>=0?"RECOVERABLE_FRACTURE":"HOSTILE_STANDOFF";var index=trust>=70&&tension<=20?Math.Min(s.TierIds.Count-1,4):trust>=40&&tension<=40?Math.Min(s.TierIds.Count-1,3):trust>=10&&tension<=60?Math.Min(s.TierIds.Count-1,2):trust>=-20?Math.Min(s.TierIds.Count-1,1):0;return s.TierIds[index];}
  static int StandingIndex(string id,IWorldGateOperationsCatalog023 c,string world){if(!c.TryGetStanding(world,out var s))return 0;for(var i=0;i<s.TierIds.Count;i++)if(StringComparer.Ordinal.Equals(s.TierIds[i],id))return i;return 0;}
 }
}
