using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Presentation
{
 public sealed partial class M1RuntimeCoordinator : Campaign023.ICampaignWorldGatePresentationCoordinator023, Campaign023.IExpeditionDeckPresentationCoordinator089
 {
  Campaign023.CampaignRegistry023 _campaignRegistry023; Campaign023.Campaign023RuleCatalogAdapter _campaignRules023; readonly CampaignWorldGateCommandService023 _campaignCommands023=new CampaignWorldGateCommandService023(); readonly ExpeditionDeckCommandService089 _expeditionDeckCommands089=new ExpeditionDeckCommandService089();
  Campaign023.CampaignRegistry023 Registry023(){if(_campaignRegistry023==null){_campaignRegistry023=Campaign023.CampaignRegistry023.LoadFromResources();_campaignRules023=new Campaign023.Campaign023RuleCatalogAdapter(_campaignRegistry023);}return _campaignRegistry023;}
  public Campaign023.CampaignWorldGatePresentationState023 CampaignWorldGate023{get{try{return BuildWorldGate023();}catch(Exception e){return new Campaign023.CampaignWorldGatePresentationState023{IsAvailable=false,Error=e.Message};}}}
  public M1CommandResult BeginWorldGateOperation023(string definitionId)
  {
   Registry019();Registry020();Registry023();
   SecondDimension.Gameplay.Recruitment.HeroMaster300Catalog087 heroes=null;
   if(TryHeroMaster300CreatorRegistry087(out var heroRegistry))
    heroes=heroRegistry.Source;
   return ApplyAndPersist(_campaignCommands023.BeginOperation(_campaign,
       _campaignRules023,definitionId,FirstCampaignUnionIds019(),
       _campaignRules020,heroes),true,
       "World Gate board and shuffled Expedition Deck committed before reveal.");
  }
  public M1CommandResult CommitWorldGateChoice023(string choiceId)
  {
   Registry023();
   var crew=ActiveWorldGateCheckCrew084();
   var actor=crew.Count>0?crew[0]:string.Empty;
   var assistant=crew.Count>1?crew[1]:string.Empty;
   return ApplyAndPersist(_campaignCommands023.CommitNodeChoice(
       _campaign,_campaignRules023,choiceId,actor,assistant,0),true,
       "Board choice and deterministic check committed before reveal.");
  }
  public M1CommandResult CommitAutomaticWorldGateRoom023()
  {
   Registry023();
   var operation=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
       .Playable020?.WorldGate023?.ActiveOperation;
   if(operation==null)return M1CommandResult.Failure(
       "Start a quest board before moving forward.");
   if(!_campaignRules023.TryGetBoard(operation.DefinitionId,out var board))
    return M1CommandResult.Failure("The active quest board could not be loaded.");
   var node=board.Nodes.FirstOrDefault(value=>value!=null&&
       StringComparer.Ordinal.Equals(value.NodeId,operation.CurrentNodeId));
   if(node==null)return M1CommandResult.Failure(
       "The current quest room could not be loaded.");
   var choice=BoardAdventureRules084.AutomaticChoice084(
       operation.OperationId,node);
   if(string.IsNullOrWhiteSpace(choice))
    return M1CommandResult.Failure(node.RequiresCertifiedBattle
        ?"Complete this room in the Union battle before moving forward."
        :"The next quest room could not be selected.");
   var crew=ActiveWorldGateCheckCrew084();
   var actor=crew.Count>0?crew[0]:string.Empty;
   var assistant=crew.Count>1?crew[1]:string.Empty;
   return ApplyAndPersist(_campaignCommands023.CommitNodeChoice(
       _campaign,_campaignRules023,choice,actor,assistant,0),true,
       "The next board room and deterministic check were committed before reveal.");
  }
  public M1CommandResult CommitExpeditionRouteCard089(string cardId)
  {
   Registry023();
   var crew=ActiveWorldGateCheckCrew084();
   var actor=crew.Count>0?crew[0]:string.Empty;
   var assistant=crew.Count>1?crew[1]:string.Empty;
   return ApplyAndPersist(_expeditionDeckCommands089.CommitRouteCard(
       _campaign,_campaignRules023,cardId,actor,assistant),true,
       "Route card selected, dice committed, and card flipped.");
  }
  public M1CommandResult AcknowledgeExpeditionDeckTutorial089()=>
      ApplyAndPersist(_expeditionDeckCommands089.AcknowledgeTutorial(_campaign),
          true,"Expedition Deck tutorial saved.");
  public M1CommandResult EnterExpeditionCardBattle089()
  {
   if(!_expeditionDeckCommands089.HasPendingOptionalBattle089(_campaign))
    return M1CommandResult.Failure(
        "Choose and reveal an optional battle card first.");
   var request=_campaign?.Guild?.GuildCity?.PendingEncounter;
   if(request!=null&&_campaign.Battle!=null&&
      StringComparer.Ordinal.Equals(_campaign.Battle.BattleId,
          request.BattleId))
    return M1CommandResult.Success(
        "Returning to the saved optional Union battle.");
   return StartCommittedGuildCityBattle017D();
  }
  public M1CommandResult ApplyWorldGateReceipt023()
  {
   var before=_campaign;
   var deck=before?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.WorldGate023?.ActiveOperation?.ExpeditionDeck089;
   var receipt=deck?.PendingReceipt;
   var card=receipt==null?null:deck.CurrentRow.FirstOrDefault(value=>value.CardId==receipt.CardId);
   var item=card?.Category=="CHEST"&&receipt.MaterialIds.Count>0
       ?ExpeditionDeckService089.ChestEquipmentReward089(card.CardId,receipt.ReceiptId):null;
   Registry019(); Registry020(); Registry023();
   var applied=_expeditionDeckCommands089.ApplyWorldGateReceiptExactlyOnce(_campaign,_campaignRules023);
   if(applied.IsSuccess)
   {
    var active=applied.Value.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation;
    if(active?.OperationKind=="CHAPTER" && active.Status==WorldGateOperationStatus023.ReadyToFinalize)
     applied=_campaignCardFlow132.FinishBoardAndPrepare(applied.Value,_campaignRules020,_campaignRules019,_campaignRules023);
   }
   var result=ApplyAndPersist(applied,true,"World Gate room and card reward applied exactly once.");
   return WithChestReceipt092(result,before,_campaign,item);
  }
  public M1CommandResult EnterWorldGateBattle023(){Registry023();var committed=_campaignCommands023.CommitEncounter(_campaign,_campaignRules023);var saved=ApplyAndPersist(committed,false,"World Gate certified encounter committed.");if(!saved.Succeeded)return saved;return StartCommittedGuildCityBattle017D();}
  public M1CommandResult FinalizeWorldGateOperation023()
  {
   Registry019(); Registry020(); Registry023();
   var before=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.WorldGate023?.ActiveOperation;
   var result=before?.OperationKind=="CHAPTER"
       ?_campaignCardFlow132.FinishBoardAndPrepare(_campaign,_campaignRules020,_campaignRules019,_campaignRules023)
       :_campaignCommands023.FinalizeOperation(_campaign,_campaignRules023);
   return ApplyAndPersist(result,true,"Quest exploration complete; the authored story interruption is saved.");
  }

  public M1CommandResult TravelWorldGate023(string worldId){Registry020();Registry023();return ApplyAndPersist(_campaignCommands023.Travel(_campaign,_campaignRules023,worldId,_campaignRules020),true,"World Gate travel committed and saved.");}
  public M1CommandResult RecoverLegacyWorldGateQuest023()
  {
   Registry023();
   var runtime=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
       .Playable020?.WorldGate023;
   var result=CampaignWorldGateCommandService023.RequiresLegacyMigration084(runtime)
       ?_campaignCommands023.MigrateLegacyNodeLedger084(_campaign,
           _campaignRules023)
       :_campaignCommands023.AbortUnverifiableLegacyOperation084(_campaign);
   return ApplyAndPersist(result,true,
       "Old adventure authority migrated safely. Rewards and completed history were preserved.");
  }
   private bool HasActiveWorldGateEncounter023(CampaignState campaign)
   {
    Registry023();
    return _expeditionDeckCommands089.HasPendingOptionalBattle089(campaign)||
           _campaignCommands023.HasCanonicalPendingEncounter084(
               campaign,_campaignRules023);
   }
  private Result<CampaignState> SynchronizeClaimedWorldGateBattle023(
      CampaignState campaign)
  {
   Registry023();
   return _expeditionDeckCommands089
       .HasUnresolvedOptionalBattleReceipt089(campaign)
       ?_expeditionDeckCommands089.SynchronizeOptionalBattleAfterClaim089(
           campaign,_campaignRules023)
       :_campaignCommands023.SynchronizeAfterClaimedBattle(
           campaign,_campaignRules023);
  }
  IReadOnlyList<string> ActiveWorldGateCheckCrew084()
  {
   var result=new List<string>();
   var operation=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
       .Playable020?.WorldGate023?.ActiveOperation;
   var unions=_campaign?.Guild?.Unions;
   if(operation==null||unions==null)return result.AsReadOnly();
   foreach(var unionId in operation.AlliedUnionIds)
   {
    var union=unions.FirstOrDefault(value=>value!=null&&
        StringComparer.Ordinal.Equals(value.UnionId,unionId));
    if(union==null)continue;
    if(!string.IsNullOrWhiteSpace(union.LeaderRecruitId)&&
       !result.Contains(union.LeaderRecruitId))result.Add(union.LeaderRecruitId);
   }
   foreach(var unionId in operation.AlliedUnionIds)
   {
    var union=unions.FirstOrDefault(value=>value!=null&&
        StringComparer.Ordinal.Equals(value.UnionId,unionId));
    if(union==null)continue;
    foreach(var recruitId in union.MemberRecruitIds)
     if(!string.IsNullOrWhiteSpace(recruitId)&&!result.Contains(recruitId))
      result.Add(recruitId);
   }
   return result.AsReadOnly();
  }
  bool ChapterWorldBoardAuthorized023(string id){var p=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020;var op=p?.ActiveOperation;if(op==null||!StringComparer.Ordinal.Equals(op.ChapterId,id))return false;Registry020();return _campaignRules020.TryGetBlueprint(id,out var b)&&op.CurrentStepIndex<b.Steps.Count&&StringComparer.Ordinal.Equals(b.Steps[op.CurrentStepIndex].Kind,"WORLD_BOARD");}
  Campaign023.CampaignWorldGatePresentationState023 BuildWorldGate023()
  {
   var registry=Registry023();
   var progress=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
   var playable=progress?.Playable020??CampaignPlayableState020.Default();
   var runtime=playable.WorldGate023??WorldGateRuntimeState023.Default();
   var currentWorld=registry.Standing.TryGetValue(runtime.CurrentWorldId,out var currentStanding)
       ?currentStanding.displayName
       :"Skyhome";
   var operation=runtime.ActiveOperation;
   var state=new Campaign023.CampaignWorldGatePresentationState023
   {
    IsAvailable=true,
    CurrentWorldId=runtime.CurrentWorldId,
    CurrentWorldName=currentWorld,
    TravelSupplies=runtime.TravelSupplies,
    ActiveOperationId=operation?.OperationId??string.Empty,
    ActiveDefinitionId=operation?.DefinitionId??string.Empty,
    ActiveStatus=operation?.Status.ToString()??"NONE",
    Supplies=operation?.Supplies??0,
    Fatigue=operation?.Fatigue??0,
    Urgency=operation?.Urgency??0,
    Threat=operation?.Threat??0,
    CompletedNodes=operation?.CompletedNodeIds.Count??0,
    PendingReceiptId=operation?.PendingReceipt?.ReceiptId??string.Empty,
     ExistingRewardId=operation?.ExistingBattleRewardReceiptId??string.Empty,
     LegacyRecoveryRequired=CampaignWorldGateCommandService023
         .RequiresLegacyRecovery084(runtime)||
         ExpeditionDeckService089.RequiresDeckRecovery089(operation),
     UnlockedRecruitOriginIds=runtime.UnlockedRecruitOriginIds,
     ExpeditionRecruitLeadIds=runtime.ExpeditionRecruitLeadIds089,
     ExpeditionDeckTutorialSeen=runtime.ExpeditionDeckTutorialSeen089,
     ExpeditionDeckTutorialSteps=ExpeditionDeckService089.TutorialSteps,
     RouteCards=Array.Empty<Campaign023.ExpeditionRouteCardView089>(),
     DeckUnlockedCategoryIds=Array.Empty<string>(),
     DeckModifierLabels=Array.Empty<string>(),
     DeckRemainingCompositionLabels=Array.Empty<string>(),
     DeckDiscardCompositionLabels=Array.Empty<string>()
   };

   if(operation!=null&&registry.Boards.TryGetValue(operation.DefinitionId,out var activeBoard))
   {
    state.ActiveBoardTitle=BoardAdventureRules084.PlayerCopy084(
        activeBoard.title,activeBoard.definitionId,activeBoard.boardId);
    state.ActiveOperationKind=activeBoard.operationKind;
    state.TotalNodes=activeBoard.nodes?.Length??0;
    if(_campaignRules023.TryGetBoard(operation.DefinitionId,out var activeRuleBoard089))
     state.DeckMinimumChoiceRounds=Math.Max(
         ExpeditionDeckService089.MinimumQuestChoiceRounds089,
         ExpeditionDeckService089.MinimumChoiceRoundsForBoard089(
             activeRuleBoard089));
    Campaign019.ChapterNarrative019 chapterNarrative=null;
    if(StringComparer.Ordinal.Equals(activeBoard.operationKind,"CHAPTER"))
     Registry019().Chapters.TryGetValue(activeBoard.definitionId,out chapterNarrative);
    var activeWorldName=registry.Standing.TryGetValue(
        activeBoard.worldId,out var activeWorld)
        ?activeWorld.displayName
        :currentWorld;
    var authoredNodes=activeBoard.nodes??Array.Empty<Campaign023.NodeDto023>();
    var projectedRooms=new Dictionary<string,
        Campaign023.AdventureBoardNarrativeProjection084.RoomCopy084>(
            StringComparer.Ordinal);
    var projectedTitles=new Dictionary<string,string>(StringComparer.Ordinal);
    foreach(var authoredNode in authoredNodes)
    {
     if(authoredNode==null||string.IsNullOrWhiteSpace(authoredNode.nodeId))continue;
     var projected=Campaign023.AdventureBoardNarrativeProjection084.Project(
         activeBoard,authoredNode,chapterNarrative,activeWorldName);
     projectedRooms[authoredNode.nodeId]=projected;
     projectedTitles[authoredNode.nodeId]=projected.Title;
    }
    state.BoardObjective=Campaign023.AdventureBoardNarrativeProjection084
        .BoardObjective(activeBoard,chapterNarrative,activeWorldName);
    state.AdventureTiles=Campaign023.AdventureBoardTrackProjection084.Project(
        activeBoard,operation.CurrentNodeId,operation.CompletedNodeIds,
        operation.PendingReceipt?.NextNodeId,projectedTitles);
    var node=authoredNodes
        .FirstOrDefault(value=>value.nodeId==operation.CurrentNodeId);
    var activeRecruitContacts=PendingRecruitContactCount088(
        registry,activeBoard.worldId,runtime,
        StringComparer.Ordinal.Equals(activeBoard.operationKind,"CHAPTER"));
    if(node!=null)
    {
     var roomCopy=projectedRooms[node.nodeId];
     var ordered=BoardAdventureRules084.OrderedChoices084(
         operation.OperationId,node.nodeId,node.choiceIds??Array.Empty<string>());
     var choices=new List<Campaign023.ChoiceView023>();
     foreach(var choice in ordered)
      choices.Add(new Campaign023.ChoiceView023
      {
       ChoiceId=choice,
       Label=BoardAdventureRules084.ChoiceLabel084(choice),
       StableOrderKey=BoardAdventureRules084.RouteOrderKey084(
           operation.OperationId,node.nodeId,choice)
      });
     var rule=new WorldGateNodeRule023
     {
      NodeId=node.nodeId,
      Kind=node.kind,
      Title=node.title,
      Description=node.description,
      CheckDifficulty=node.checkDifficulty,
      SupplyDelta=node.supplyDelta,
      FatigueDelta=node.fatigueDelta,
      UrgencyDelta=node.urgencyDelta,
      ThreatDelta=node.threatDelta,
      TrustDelta=node.trustDelta,
      TensionDelta=node.tensionDelta,
      CivilianSupportDelta=node.civilianSupportDelta,
      GuildXp=node.guildXp,
      HallXp=node.hallXp,
      MaterialIds=node.materialIds??Array.Empty<string>(),
      RequiresCertifiedBattle=node.requiresCertifiedBattle,
      EnemyUnionCount=node.enemyUnionCount,
      Objective=node.objective,
      Optional=node.optional,
      SourceId=node.sourceId
     };
     state.CurrentNode=new Campaign023.NodeView023
     {
      NodeId=node.nodeId,
      Kind=node.kind,
      RoomKind=BoardAdventureRules084.RoomKind084(node.kind),
      RoomTitle=BoardAdventureRules084.RoomTitle084(node.kind),
      Title=roomCopy.Title,
      Description=roomCopy.Description,
      StoryFlavor=roomCopy.StoryFlavor,
      Objective=roomCopy.Objective,
      RewardPreview=BoardAdventureRules084.RewardPreview084(
          rule,
           operation.RewardPermilleAtBegin>=0
               ?operation.RewardPermilleAtBegin
               :BoardAdventureRules084.RewardPermille084(
                   operation,_campaignRules023.RewardPolicy,runtime)),
      Choices=ordered,
      ChoiceViews=choices.AsReadOnly(),
      Difficulty=node.checkDifficulty,
      RequiresBattle=node.requiresCertifiedBattle,
      IconResource="SecondDimension/Campaign023/Icons/NODE_"+node.kind+"_023"
     };
     if(activeRecruitContacts>0&&
        (StringComparer.OrdinalIgnoreCase.Equals(node.kind,"OBJECTIVE")||
         StringComparer.OrdinalIgnoreCase.Equals(node.kind,"EXIT")))
      state.CurrentNode.RewardPreview=AppendRecruitContactPreview088(
          state.CurrentNode.RewardPreview,activeRecruitContacts);
     var completedKinds=authoredNodes
         .Where(value=>operation.CompletedNodeIds.Contains(value.nodeId))
         .Select(value=>BoardAdventureRules084.RoomKind084(value.kind));
     state.AdventureTrackPhase=BoardAdventureRules084.MonotonicSemanticTrackPhase084(
         state.CurrentNode.RoomKind,state.ActiveStatus,completedKinds);
     var deck=operation.ExpeditionDeck089;
     if(deck!=null)
     {
      state.DeckDrawPileCount=deck.DrawPile.Count;
      state.DeckDiscardCount=deck.DiscardPile.Count;
      state.DeckBanishedCount=deck.BanishedCards.Count;
       state.DeckMomentum=deck.Momentum;
       state.DeckChoiceRound=deck.AppliedReceipts.Count+1;
       state.DeckProgressionTier=deck.ProgressionTier;
       state.DeckCardsPerNode=deck.CardsPerNodeAtCreation;
       state.DeckUnlockedCategoryIds=deck.UnlockedCategoryIds;
      state.DeckModifierLabels=deck.ModifierLabels;
      state.DeckRemainingCompositionLabels=ExpeditionDeckComposition089(
          deck.CurrentRow.Concat(deck.DrawPile));
      state.DeckDiscardCompositionLabels=ExpeditionDeckComposition089(
          deck.DiscardPile);
       var checkCrew089=ActiveWorldGateCheckCrew084();
       var checkActor089=checkCrew089.Count>0?checkCrew089[0]:string.Empty;
       var checkAssistant089=checkCrew089.Count>1?checkCrew089[1]:string.Empty;
       state.RouteCards=deck.CurrentRow.Select(card=>
       {
        var breakdown=ExpeditionDeckService089.CheckModifierBreakdown089(
            _campaign,operation,card,checkActor089,checkAssistant089);
        var effective=breakdown.EffectiveModifier;
        var chance=ExpeditionDeckService089.ChanceBasisPoints(
            card.ResolutionDifficulty,effective);
         var presentationCategory=ExpeditionPresentationCategory089(card);
         var presentationFaceKey=ExpeditionPresentationFaceKey089(card);
         var optionalBattle=ExpeditionDeckService089
             .IsOptionalBattleCard089(card);
         var enemyUnionCount094=optionalBattle
             ? SecondDimension.Gameplay.M2.EnemyForceProfile094.PreviewUnionCount094(
                 _campaign,operation.DefinitionId,card.EnemyUnionCount,card.CardId,
                 deck.PendingReceipt?.CardId,deck.PendingReceipt?.BattleId)
             : card.EnemyUnionCount;
         var freeRecruit094=SecondDimension.Gameplay.GuildCity017D
             .GuildCityRecruitmentService017D.IsFreeRecruitChanceCard094(card);
        return new Campaign023.ExpeditionRouteCardView089
       {
        CardId=card.CardId,
        Category=presentationCategory,
        VisualCategoryKey=presentationCategory,
        VisualResourcePath="SecondDimension/Art/Board086/CardFaces/CARD_FACE_"+
            presentationFaceKey+"_089",
        Title=freeRecruit094?"Recruit chance: "+card.RecruitName:card.Title,
       Description=freeRecruit094?"Win the dice check to earn "+card.RecruitName+
           " as a free quest reward. Claim after this adventure; no XP cost.":card.Description,
        RiskLabel=optionalBattle?"OPTIONAL BATTLE":
            card.TreasuryXpCost>0?"OPTIONAL PURCHASE":
            card.ResolutionDifficulty<=0?"CERTAIN":
            chance>=7500?"FAVORED":chance>=5000?"EVEN":"RISKY",
         Odds=optionalBattle?enemyUnionCount094+" ENEMY UNION"+
             (enemyUnionCount094==1?string.Empty:"S")+
             " • RETURN TO SAME ROOM":
             card.TreasuryXpCost>0?"BALANCE "+
             (_campaign?.Guild?.TreasuryXp??0)+" XP • COST "+
             card.TreasuryXpCost+" XP":
             card.ResolutionDifficulty<=0?"NO ROLL • CERTAIN":
              ExpeditionOdds089(card,breakdown,chance),
        OutcomePreview=freeRecruit094?"SUCCESS • FREE RECRUIT INVITATION  |  SETBACK • NO RECRUIT":ExpeditionOutcomePreview089(card),
        SuccessBasisPoints=chance,
        RewardPreview=freeRecruit094?card.RecruitName+" • EARNED RECRUIT • NO XP COST":
            optionalBattle?enemyUnionCount094+" enemy Union"+
            (enemyUnionCount094==1?string.Empty:"s")+
            " • battle loot + Art growth • route cards next":card.RewardPreview,
        RouteLabel=card.AdvancesRoute
            ? BoardAdventureRules084.ChoiceLabel084(card.ChoiceId)
            : "EVENT RESOLVES • ROUTE CARDS NEXT",
        RecruitStableId=card.RecruitStableId,
         RecruitName=card.RecruitName,
         RecruitRank=card.RecruitRank,
         LockedToBattle=node.requiresCertifiedBattle,
         IsEncounterRound=!card.AdvancesRoute,
         RequiresCertifiedBattle=optionalBattle,
         EnemyUnionCount=enemyUnionCount094,
         TreasuryXpCost=card.TreasuryXpCost,
         TreasuryXpBalance=_campaign?.Guild?.TreasuryXp??0,
         CanChoose=card.TreasuryXpCost<=0||
             (_campaign?.Guild?.TreasuryXp??0)>=card.TreasuryXpCost,
         LockedReason=card.TreasuryXpCost>0&&
             (_campaign?.Guild?.TreasuryXp??0)<card.TreasuryXpCost
             ?"NEED "+card.TreasuryXpCost+" XP • CURRENT BALANCE "+
              (_campaign?.Guild?.TreasuryXp??0)
             :string.Empty,
         PermanentHeroName=card.PermanentHeroName,
         PermanentHeroEffectKind=card.PermanentHeroEffectKind,
         PermanentHeroCheckModifier=card.PermanentHeroCheckModifier
        };
      }).ToArray();
     }
    }
    var receipt=operation.PendingReceipt;
    if(receipt!=null)
    {
      var receiptNode=authoredNodes.FirstOrDefault(value=>value!=null&&
          StringComparer.Ordinal.Equals(value.nodeId,receipt.NodeId));
      state.PendingHasCheck=receipt.DieOne>0&&receipt.DieTwo>0;
      state.PendingDieOne=receipt.DieOne;
      state.PendingDieTwo=receipt.DieTwo;
      state.PendingModifier=receipt.Modifier;
      state.PendingTotal=state.PendingHasCheck
          ?checked(receipt.DieOne+receipt.DieTwo+receipt.Modifier)
          :0;
      state.PendingDifficulty=receiptNode?.checkDifficulty??0;
      state.PendingChoiceLabel=BoardAdventureRules084.ChoiceLabel084(
          receipt.ChoiceId);
      state.PendingOutcomeTitle=BoardAdventureRules084.OutcomeTitle084(receipt.Outcome);
     state.PendingDice=receipt.DieOne>0&&receipt.DieTwo>0
         ?"DICE  "+receipt.DieOne+" + "+receipt.DieTwo+
          (receipt.Modifier==0?string.Empty:receipt.Modifier>0
              ?" + "+receipt.Modifier
              :" - "+Math.Abs(receipt.Modifier))
         :"STORY CHOICE COMMITTED";
      state.PendingReward=BoardAdventureRules084.ReceiptReward084(receipt);
      if(activeRecruitContacts>0&&receiptNode!=null&&
         (StringComparer.OrdinalIgnoreCase.Equals(receiptNode.kind,"OBJECTIVE")||
          StringComparer.OrdinalIgnoreCase.Equals(receiptNode.kind,"EXIT")))
       state.PendingReward=AppendRecruitContactPreview088(
           state.PendingReward,activeRecruitContacts);
     }

    // A saved encounter card has no World Gate receipt because it deliberately
    // leaves the authored route edge untouched. Project its own receipt through
    // the same reveal/dice/reward surface so retries remain visible and safe.
    var cardReceipt=operation.ExpeditionDeck089?.PendingReceipt;
    if(cardReceipt!=null)
    {
     if(receipt==null)state.PendingReceiptId=cardReceipt.ReceiptId;
     var card=operation.ExpeditionDeck089.CurrentRow.FirstOrDefault(value=>
         StringComparer.Ordinal.Equals(value.CardId,cardReceipt.CardId));
     state.PendingIsEncounterRound=card!=null&&!card.AdvancesRoute;
     state.PendingRequiresCertifiedBattle=
         cardReceipt.RequiresCertifiedBattle;
     state.PendingCardCategory=card==null?string.Empty:
         ExpeditionPresentationCategory089(card);
     state.PendingCardTitle=card?.Title??string.Empty;
     state.PendingCardVisualResourcePath=card==null?string.Empty:
         "SecondDimension/Art/Board086/CardFaces/CARD_FACE_"+
         ExpeditionPresentationFaceKey089(card)+"_089";
     if(state.PendingIsEncounterRound)
     {
      state.PendingReward=string.Empty;
      state.PendingChoiceLabel="EXPEDITION ENCOUNTER";
      state.PendingHasCheck=false;
     }
     if(cardReceipt.DieOne>0&&cardReceipt.DieTwo>0)
     {
      state.PendingHasCheck=true;
      state.PendingDieOne=cardReceipt.DieOne;
      state.PendingDieTwo=cardReceipt.DieTwo;
      state.PendingModifier=cardReceipt.EffectiveModifier;
      state.PendingTotal=cardReceipt.DieOne+cardReceipt.DieTwo+
          cardReceipt.EffectiveModifier;
      state.PendingDifficulty=cardReceipt.Difficulty;
      state.PendingDice="DICE  "+cardReceipt.DieOne+" + "+cardReceipt.DieTwo+
          (cardReceipt.EffectiveModifier==0?string.Empty:
           cardReceipt.EffectiveModifier>0?" + "+cardReceipt.EffectiveModifier:
           " - "+Math.Abs(cardReceipt.EffectiveModifier));
     }
      else if(state.PendingIsEncounterRound)
       state.PendingDice=state.PendingRequiresCertifiedBattle
           ?"CERTIFIED UNION BATTLE READY"
           :"ENCOUNTER CARD COMMITTED";
      state.PendingOutcomeTitle=BoardAdventureRules084.OutcomeTitle084(
          cardReceipt.Outcome);
      if(state.PendingRequiresCertifiedBattle)
       state.PendingOutcomeTitle="OPTIONAL BATTLE REVEALED";
     var cardRewards=new List<string>();
     if(cardReceipt.GuildXp>0)cardRewards.Add("+"+cardReceipt.GuildXp+
         " GUILD / HALL XP");
     if(card!=null&&StringComparer.Ordinal.Equals(card.Category,"CHEST")&&
        cardReceipt.MaterialIds.Count>0)
     {
      var chestItem=ExpeditionDeckService089.ChestEquipmentReward089(
          card.CardId,cardReceipt.ReceiptId);
      state.PendingChestHasReward092=true;
      state.PendingChestItemName092=chestItem.DisplayName;
      state.PendingChestItemVisualId092=EquipmentVisualId090(chestItem,chestItem.ValidSlotIds.FirstOrDefault());
      state.PendingChestRarityId092=chestItem.QualityId;
      state.PendingChestRewardCopy092=chestItem.DisplayName+" • READY TO COLLECT";
      cardRewards.Add("GEAR: "+chestItem.DisplayName+" • ready to collect");
     }
     if(cardReceipt.MaterialIds.Count>0)cardRewards.Add(
         cardReceipt.MaterialIds.Count+" CHEST MATERIAL");
     if(cardReceipt.MomentumDelta!=0)cardRewards.Add(
         (cardReceipt.MomentumDelta>0?"+":"")+
         cardReceipt.MomentumDelta+" ROUTE MOMENTUM");
      if(!string.IsNullOrWhiteSpace(cardReceipt.RecruitStableId))
       cardRewards.Add(SecondDimension.Gameplay.GuildCity017D.GuildCityRecruitmentService017D
           .IsFreeRecruitChanceCard094(card)?card.RecruitName+
               " • FREE RECRUIT • CLAIM AFTER THIS QUEST":"RECRUIT LEAD EARNED");
      if(card!=null&&(state.PendingRequiresCertifiedBattle||
         StringComparer.Ordinal.Equals(card.Category,"MERCHANT")||
         StringComparer.Ordinal.Equals(card.Category,"PERMANENT")))
       cardRewards.Add(card.RewardPreview);
     if(cardRewards.Count>0)state.PendingReward=
         (string.IsNullOrWhiteSpace(state.PendingReward)?string.Empty:
             state.PendingReward+"  •  ")+string.Join("  •  ",cardRewards);
     if(string.IsNullOrWhiteSpace(state.PendingReward))
      state.PendingReward="EXPEDITION PROGRESS";
    }
     WorldGateNodeReceipt023 lastApplied=null;
     if(operation.Status==WorldGateOperationStatus023.ReadyToFinalize)
      lastApplied=operation.AppliedReceipts.FirstOrDefault(value=>value!=null&&
          StringComparer.Ordinal.Equals(value.NodeId,operation.CurrentNodeId)&&
          string.IsNullOrWhiteSpace(value.NextNodeId));
     else
      lastApplied=operation.AppliedReceipts.FirstOrDefault(value=>value!=null&&
          StringComparer.Ordinal.Equals(value.NextNodeId,operation.CurrentNodeId));
     if(lastApplied!=null&&projectedRooms.TryGetValue(
         lastApplied.NodeId,out var appliedRoom))
     {
      state.HasLastAppliedRoom=true;
      state.LastAppliedRoomTitle=appliedRoom.Title;
      state.LastAppliedOutcomeTitle=BoardAdventureRules084.OutcomeTitle084(
          lastApplied.Outcome);
      state.LastAppliedReward=BoardAdventureRules084.ReceiptReward084(lastApplied);
      state.LastAppliedChoiceLabel=BoardAdventureRules084.ChoiceLabel084(
          lastApplied.ChoiceId);
     }
    }

   var standings=new List<Campaign023.StandingView023>();
   foreach(var world in registry.Standing.Values.OrderBy(value=>value.displayName))
   {
    var standing=runtime.WorldStandings.FirstOrDefault(value=>value.WorldId==world.worldId);
    var travelCost=registry.Travel.TryGetValue(world.worldId,out var travelRoute)
        ?travelRoute.supplyCost
        :0;
    standings.Add(new Campaign023.StandingView023
    {
     WorldId=world.worldId,
     DisplayName=world.displayName,
     Tier=standing?.TierId??"LOCKED",
     Trust=standing?.Trust??0,
     Tension=standing?.Tension??0,
     CivilianSupport=standing?.CivilianSupport??0,
     TravelSupplyCost=travelCost,
     CanAffordTravel=world.worldId=="SKYHOME"||runtime.TravelSupplies>=travelCost,
     Unlocked=world.worldId=="SKYHOME"||runtime.UnlockedWorldIds.Contains(world.worldId)||
              (progress?.UnlockedWorldIds?.Contains(world.worldId)??false)||
              playable.WorldGates.Any(value=>value.WorldId==world.worldId&&value.Unlocked)
    });
   }
   state.Standings=standings.AsReadOnly();

   var boards=new List<Campaign023.BoardView023>();
   foreach(var board in registry.Boards.Values
       .OrderBy(value=>value.operationKind=="CHAPTER"?0:value.operationKind=="REPEATABLE"?1:2)
       .ThenBy(value=>value.worldId)
       .ThenBy(value=>value.title))
   {
    var world=standings.FirstOrDefault(value=>value.WorldId==board.worldId);
    var worldOpen=world?.Unlocked??false;
    var isCurrentStory=board.operationKind=="CHAPTER"&&
                       ChapterWorldBoardAuthorized023(board.definitionId);
    var available=worldOpen&&runtime.ActiveOperation==null&&
                  StringComparer.Ordinal.Equals(runtime.CurrentWorldId,board.worldId);
    if(board.operationKind=="CHAPTER")available=available&&isCurrentStory;
    else if(board.operationKind=="REPEATABLE")
     available=available&&playable.UnlockedRepeatableContractIds.Contains(board.definitionId);
    Campaign019.ChapterNarrative019 boardNarrative=null;
    if(StringComparer.Ordinal.Equals(board.operationKind,"CHAPTER"))
     Registry019().Chapters.TryGetValue(board.definitionId,out boardNarrative);
    var boardWorldName=world?.DisplayName??"Unknown World";
    var nextRunRewardPermille=BoardAdventureRules084
        .RewardPermilleForNextRun084(board.operationKind,board.definitionId,
            _campaignRules023.RewardPolicy,runtime);
    boards.Add(new Campaign023.BoardView023
    {
     DefinitionId=board.definitionId,
     WorldId=board.worldId,
     WorldName=boardWorldName,
     Title=BoardAdventureRules084.PlayerCopy084(
         board.title,board.definitionId,board.boardId),
     Kind=board.operationKind,
     PlayerKind=board.operationKind=="CHAPTER"?"STORY QUEST":
                board.operationKind=="REPEATABLE"?"GUILD CONTRACT":"WORLD CRISIS",
     Objective=Campaign023.AdventureBoardNarrativeProjection084.BoardObjective(
         board,boardNarrative,boardWorldName),
     RewardPreview=Campaign023.AdventureBoardNarrativeProjection084
         .BoardRewardPreview(board,nextRunRewardPermille),
     NodeCount=board.nodes?.Length??0,
     TreasureCardCount=(board.nodes??Array.Empty<Campaign023.NodeDto023>())
         .Count(value=>value!=null&&StringComparer.OrdinalIgnoreCase.Equals(
             value.kind,"RESOURCE")),
     DiceCardCount=(board.nodes??Array.Empty<Campaign023.NodeDto023>())
         .Count(value=>value!=null&&value.checkDifficulty>0),
     StoryCardCount=(board.nodes??Array.Empty<Campaign023.NodeDto023>())
         .Count(value=>value!=null&&(
             StringComparer.OrdinalIgnoreCase.Equals(value.kind,"EVENT")||
             StringComparer.OrdinalIgnoreCase.Equals(value.kind,"DIPLOMACY")||
             StringComparer.OrdinalIgnoreCase.Equals(value.kind,"OBJECTIVE"))),
     BattleCardCount=(board.nodes??Array.Empty<Campaign023.NodeDto023>())
         .Count(value=>value?.requiresCertifiedBattle==true),
     RecruitContactCount=PendingRecruitContactCount088(
         registry,board.worldId,runtime,
         StringComparer.Ordinal.Equals(board.operationKind,"CHAPTER")),
     Available=available,
     UsesBattle=board.usesCertifiedBattle,
     IsCurrentStoryBoard=isCurrentStory
    });
   }
   state.Boards=boards.AsReadOnly();
    return state;
   }

  static string ExpeditionOutcomePreview089(
       ExpeditionRouteCardState089 card)
   {
   if(card==null)return "SUCCESS • advance the expedition";
    if(ExpeditionDeckService089.IsOptionalBattleCard089(card))
     return "ENTER UNION COMBAT • CLAIM BATTLE REWARD • RETURN TO THIS ROOM";
    if(card.TreasuryXpCost>0)
     return "BUY • ITEM ENTERS INVENTORY  |  LEAVE • CHOOSE ANOTHER CARD";
    if(!string.IsNullOrWhiteSpace(card.PermanentHeroEffectId))
     return "PERMANENT • APPLIES TO "+card.PermanentHeroName+
            " WHEN THIS CARD IS CLAIMED";
    if(card.ResolutionDifficulty<=0)
     return "SUCCESS • full listed reward  |  NO FAILURE ROLL";
    var momentumLoss=StringComparer.Ordinal.Equals(card.Category,"HAZARD")||
                     StringComparer.Ordinal.Equals(card.Category,"CHANCE")||
                     StringComparer.Ordinal.Equals(card.Category,"RECRUIT");
    return "SUCCESS • full listed reward  |  SETBACK • up to 2 XP, no item, material, or recruit"+
           (momentumLoss?", −1 momentum":string.Empty);
  }

  public static string ExpeditionPresentationCategory089(
      ExpeditionRouteCardState089 card)
  {
   if(card==null)return "STORY";
   return StringComparer.Ordinal.Equals(card.RecruitOfferKind,
              ExpeditionDeckService089.AscensionOfferKind089)
       ?"ASCENSION"
       :string.IsNullOrWhiteSpace(card.Category)?"STORY":card.Category;
  }

  public static string ExpeditionPresentationFaceKey089(
      ExpeditionRouteCardState089 card)
  {
   var category=ExpeditionPresentationCategory089(card);
   // The installed RECRUIT face remains the trusted Hero-card illustration;
   // ASCENSION gets its distinct purple/star frame without requesting a
   // nonexistent CARD_FACE_ASCENSION asset or ever producing a blank card.
   if(StringComparer.Ordinal.Equals(category,"ASCENSION"))return "RECRUIT";
   return card==null||string.IsNullOrWhiteSpace(card.VisualCategoryKey)
       ?category
       :card.VisualCategoryKey;
  }

  static string ExpeditionOdds089(
      ExpeditionRouteCardState089 card,
      ExpeditionCheckModifierBreakdown089 breakdown,
      int chanceBasisPoints)
  {
   string Signed089(int value)=>value>=0?"+"+value:value.ToString();
   return (chanceBasisPoints/100)+"% SUCCESS"+
          "  •  ROLL 2D6 VS "+card.ResolutionDifficulty+
          "  •  TOTAL BONUS "+Signed089(breakdown.EffectiveModifier)+
          (breakdown.TeamModifier>0?" (ALLY HELPING)":string.Empty);
  }

  static int PendingRecruitContactCount088(
   Campaign023.CampaignRegistry023 registry,
   string worldId,
   WorldGateRuntimeState023 runtime,
   bool storyQuest)
  {
   if(!storyQuest||registry==null||runtime==null||
      string.IsNullOrWhiteSpace(worldId)||
      !registry.RecruitUnlocks.TryGetValue(worldId,out var unlock))return 0;
   return (unlock.originProfileIds??Array.Empty<string>())
    .Where(value=>!string.IsNullOrWhiteSpace(value))
    .Distinct(StringComparer.Ordinal)
    .Count(value=>!runtime.UnlockedRecruitOriginIds.Contains(value));
  }

  static IReadOnlyList<string> ExpeditionDeckComposition089(
   IEnumerable<ExpeditionRouteCardState089> cards)
  {
   if(cards==null)return Array.Empty<string>();
   return cards.Where(value=>value!=null)
    .GroupBy(value=>value.Category??"STORY",StringComparer.Ordinal)
    .OrderBy(group=>group.Key,StringComparer.Ordinal)
    .Select(group=>group.Key+" ×"+group.Count())
    .ToArray();
  }

  static string AppendRecruitContactPreview088(string reward,int count)
  {
   if(count<=0)return reward??string.Empty;
   var prefix=string.IsNullOrWhiteSpace(reward)
    ?string.Empty
    :reward+"  •  ";
   return prefix+"RECRUIT NETWORK ×"+count+
    " CHECKED AT RETURN (WORLD STANDING APPLIES)";
  }
 }
 static class String023Extensions{public static bool HasValue(this string value)=>!string.IsNullOrWhiteSpace(value);}
}
