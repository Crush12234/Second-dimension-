using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SecondDimension.Presentation
{
 public sealed partial class M1FlowPresenter
 {
  int _invocationAffixPage022;
  int _towerOperationPage084=-1;
  const float TowerRoomResultHold084=0.82f;
  readonly HashSet<string> _scheduledTowerReceiptApplies084=
   new HashSet<string>(StringComparer.Ordinal);
  bool _towerRoomCommandRunning084;
  System.Collections.Generic.IReadOnlyList<string> _selectedInvocationAffixes022=Array.Empty<string>();

  void BuildGuildCityProgression022(Transform body,Campaign022.ICampaignProgressionPresentationCoordinator022 c,Campaign022.CampaignProgressionPresentationState022 s)
  {
   if(s==null||!s.IsAvailable){AddMessagePanel(body,"FORGE",s?.Error??"The Forge is unavailable.",RuntimeUi.Warning);return;}
   BuildForge154(body);
  }
  void BuildGuildCityAbyss022(Transform body,Campaign022.ICampaignProgressionPresentationCoordinator022 c,Campaign022.CampaignProgressionPresentationState022 s)
  {
   if(DrawPendingTowerRestart130(body,c))return;
   if(DrawPendingTowerManual117(body,c))return;
   if(s==null||!s.IsAvailable){AddMessagePanel(body,"ENDLESS TOWER",s?.Error??"The Tower is unavailable.",RuntimeUi.Warning);return;}
   if(s.LegacyTowerRecoveryRequired)
   {
    if(s.LegacyTowerRecoveryRequiresSupport)
    {
     var held=AddMessagePanel(body,"OLDER TOWER RUN HELD SAFELY",
      "This Tower run began before the current saved-room proof. Its active room or battle handoff cannot be changed automatically without risking a reward. Keep this save and contact support; cleared floors and every banked reward remain untouched.",
      RuntimeUi.Warning);
     RuntimeUi.AddButton(held,"Return from held Tower run 084","RETURN TO THE GUILD HALL",()=>
     {
      _guildCityTab017D="HALL";
      BuildCurrentScreen();
     },118f,RuntimeUi.ButtonNormal);
     UseContentDrivenBoardPanelHeight084(held);
    }
    else
    {
     var recovery=AddMessagePanel(body,"OLDER TOWER RECORD READY TO RECOVER",
      "Update this inactive Tower record to the current saved-room proof. Cleared floors, Guild progress, inventory, recruits, and every banked reward remain exactly as saved.",
      RuntimeUi.Warning);
     RuntimeUi.AddButton(recovery,"Recover inactive legacy Tower 084",
      "RECOVER OLD TOWER RECORD — KEEP BANKED REWARDS",
      ()=>RunGuildCityCommand017D(c.RecoverInactiveLegacyTower084),
      132f,RuntimeUi.Warning);
     UseContentDrivenBoardPanelHeight084(recovery);
    }
    return;
   }

   if(!string.IsNullOrWhiteSpace(s.TowerAuthorityError094))
   {
    AddMessagePanel(body,"TOWER RECORD HELD SAFELY",s.TowerAuthorityError094,RuntimeUi.Warning);
    RuntimeUi.AddButton(body,"Return from invalid Tower record 094","RETURN TO GUILD HALL",()=>
    {
     _guildCityTab017D="HALL";
     BuildCurrentScreen();
    },118f,RuntimeUi.ButtonNormal);
    return;
   }

   var hasActiveRun=!string.IsNullOrWhiteSpace(s.ActiveAbyssOperationId);
   if(!hasActiveRun&&!string.IsNullOrWhiteSpace(s.TowerLastHeroRewardSummary094))
   {
    var savedReward=AddMessagePanel(body,
     "FLOOR "+s.TowerLastHeroRewardFloor094+" • HERO REWARD SAVED",
     s.TowerLastHeroRewardSummary094,
     s.TowerLastHeroRewardWon094?RuntimeUi.Positive:RuntimeUi.ButtonNormal);
    savedReward.gameObject.name="Tower Saved Hero Reward 094";
   }
   if(string.IsNullOrWhiteSpace(s.ActiveAbyssOperationId))
    RuntimeUi.AddButton(
     body,"Climb next Tower floor 084",
     "FIGHT FLOOR "+s.TowerFloorNumber+"\nYOUR NEXT UNION BATTLE",
     ()=>BeginTowerBattleOnly088(c),
     132f,RuntimeUi.Accent);
   else
    BuildTowerPrimaryAction084(body,c,s);

   var presentation110=_coordinator?.State;
   var guildXpCopy=_coordinator==null
    ?"— / —"
    :presentation110.GuildXpIntoCurrentLevel.ToString("N0")+" / "+
     Math.Max(1L,presentation110.GuildXpRequiredForNextLevel).ToString("N0");
    var current=AddMessagePanel(
     body,
     "FLOOR "+s.TowerFloorNumber+"  •  "+
      (s.TowerFloorDisplayName??"ENDLESS TOWER").ToUpperInvariant(),
     "BEST FLOOR  •  "+s.HighestClearedTowerFloor+
     "\nVICTORY REWARD  •  "+(string.IsNullOrWhiteSpace(s.TowerRewardSummary)
      ?"FLOOR CLEAR REWARD":s.TowerRewardSummary)+
     "\nGUILD XP  •  "+guildXpCopy,
     Campaign022.TowerRunRules081.FirstClimbHeadingColor083);
    current.gameObject.name="Tower Phone Simple Summary 084";

   if(!hasActiveRun)
   {
     if(!string.IsNullOrWhiteSpace(s.TowerArtResourcePath))
      BuildTowerFloorHero085(
       body,s,"Tower Current Floor Art 081",false);
     return;
   }

   var activeTile=AddMessagePanel(
    body,
    "TOWER BATTLE ACTIVE  •  "+
      SecondDimension.Gameplay.Campaign022.TowerAdventureRules084.PlayerOperationKind084(s.ActiveOperationKind),
    (s.ActiveOperationDisplayName??s.TowerOperationDisplayName??"Tower Run").ToUpperInvariant()+
    "\nFLOOR "+s.TowerFloorNumber+"  •  "+
    (s.TowerBattleInProgress?"BATTLE IN PROGRESS":
     s.TowerBattleRewardAwaitingClaim?"VICTORY REWARD READY":
     s.TowerBattleResolved?"BATTLE RESOLVED":"UNIONS READY"),
    RuntimeUi.Warning);
   activeTile.gameObject.name="Tower Battle Only Status 088";

   if(s.TowerBattleResolved&&!s.TowerBattleWon)
   {
    AddMessagePanel(body,"THE CLIMB ENDS HERE","Return safely, improve your roster, and retry this floor whenever you are ready. No earlier Tower reward is lost.",RuntimeUi.Warning);
    return;
   }

    if(!string.IsNullOrWhiteSpace(s.TowerArtResourcePath))
     BuildTowerFloorHero085(
       body,s,"Tower Active Floor Art 084",true);
  }

  void BuildTowerFloorHero085(
   Transform body,
   Campaign022.CampaignProgressionPresentationState022 state,
   string artName,
   bool activeRun)
  {
   var frame=RuntimeUi.AddPanel(
    body,
    activeRun?"Tower Active Floor Hero 085":"Tower Current Floor Hero 085",
    Color.white);
   RuntimeUi.SetLayout(
    frame.rectTransform,
    preferredHeight:activeRun?410f:570f,
    flexibleWidth:1f);
   frame.gameObject.AddComponent<Mask>().showMaskGraphic=true;

   var art=RuntimeUi.AddPanel(frame.transform,artName,Color.white);
   art.raycastTarget=false;
   art.sprite=GuildCity017E.GuildCityOpeningExperienceRegistry017E.Sprite(
    state.TowerArtResourcePath);
   art.type=Image.Type.Simple;
   art.preserveAspect=false;
   Stretch(art.rectTransform);
   if(art.sprite!=null)
   {
    var fitter=art.gameObject.AddComponent<AspectRatioFitter>();
    fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
    fitter.aspectRatio=art.sprite.rect.width/Mathf.Max(1f,art.sprite.rect.height);
   }

   var cinematicShade=RuntimeUi.AddPanel(
    frame.transform,
    "Tower Floor Cinematic Shade 085",
    new Color(0.005f,0.01f,0.025f,0.17f));
   Stretch(cinematicShade.rectTransform);
   cinematicShade.raycastTarget=false;

   var statusBand=RuntimeUi.AddPanel(
    frame.transform,
    "Tower Floor Status Band 085",
    new Color(0.008f,0.014f,0.028f,0.91f));
   statusBand.rectTransform.anchorMin=new Vector2(0f,0f);
   statusBand.rectTransform.anchorMax=new Vector2(1f,0.31f);
   statusBand.rectTransform.offsetMin=Vector2.zero;
   statusBand.rectTransform.offsetMax=Vector2.zero;
   statusBand.raycastTarget=false;

   var eyebrow=RuntimeUi.AddText(
    statusBand.transform,
    "Tower Floor Hero Eyebrow 085",
    activeRun?"UNION BATTLE  •  FLOOR IN PROGRESS":"NEXT CHALLENGE  •  READY TO FIGHT",
    22,
    TextAnchor.MiddleLeft,
    activeRun?RuntimeUi.Positive:RuntimeUi.Accent,
    FontStyle.Bold);
   eyebrow.rectTransform.anchorMin=new Vector2(0.025f,0.54f);
   eyebrow.rectTransform.anchorMax=new Vector2(0.68f,0.94f);
   eyebrow.rectTransform.offsetMin=Vector2.zero;
   eyebrow.rectTransform.offsetMax=Vector2.zero;
   eyebrow.raycastTarget=false;
   ConfigureResponsiveText062(eyebrow,16,24);

   var title=RuntimeUi.AddText(
    statusBand.transform,
    "Tower Floor Hero Title 085",
    "FLOOR "+state.TowerFloorNumber+"  •  "+
     (state.TowerFloorDisplayName??"ENDLESS TOWER").ToUpperInvariant(),
    34,
    TextAnchor.MiddleLeft,
    RuntimeUi.Text,
    FontStyle.Bold);
   title.rectTransform.anchorMin=new Vector2(0.025f,0.06f);
   title.rectTransform.anchorMax=new Vector2(0.72f,0.62f);
   title.rectTransform.offsetMin=Vector2.zero;
   title.rectTransform.offsetMax=Vector2.zero;
   title.raycastTarget=false;
   ConfigureResponsiveText062(title,20,36);

   var loop=RuntimeUi.AddText(
    statusBand.transform,
    "Tower Floor Loop Promise 085",
    "FIGHT  •  BANK LOOT  •  CLIMB AGAIN",
    21,
    TextAnchor.MiddleRight,
    RuntimeUi.MutedText,
    FontStyle.Bold);
   loop.rectTransform.anchorMin=new Vector2(0.60f,0.10f);
   loop.rectTransform.anchorMax=new Vector2(0.975f,0.90f);
   loop.rectTransform.offsetMin=Vector2.zero;
   loop.rectTransform.offsetMax=Vector2.zero;
   loop.raycastTarget=false;
   ConfigureResponsiveText062(loop,15,22);

   M1PremiumUi.StylePanel(frame,M1PremiumUi.Surface.WorldGlass);
  }

  void BuildTowerPrimaryAction084(
   Transform body,
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator,
   Campaign022.CampaignProgressionPresentationState022 state)
  {
   if(state.TowerBattleResolved&&!state.TowerBattleWon)
   {
    if(coordinator is Campaign022.ITowerRestartAsyncTransition130 restart130 && restart130.CanRestartTowerAfterPartyDefeat130)
     RuntimeUi.AddButton(body,"Restart Tower from Floor 1 after party defeat 130","RESTART FROM FLOOR 1",
      ()=>BeginTowerRestart130(restart130),132f,RuntimeUi.Accent);
    RuntimeUi.AddButton(body,"Retreat from defeated Tower run 081","RETURN TO GUILD",
     ()=>RunGuildCityCommand017D(coordinator.RetreatTowerRun081),132f,RuntimeUi.Positive);
    return;
   }
   if(StringComparer.Ordinal.Equals(state.ActiveAbyssStatus,"AwaitingBattle"))
   {
    if(state.TowerBattleInProgress)
     RuntimeUi.AddButton(body,"Resume Tower battle 081","RETURN TO BATTLE",
      ()=>Navigate(M1Screen.Battle),132f,RuntimeUi.Warning);
    else if(state.TowerBattleRewardAwaitingClaim)
     RuntimeUi.AddButton(body,"Open Tower battle results 084","OPEN BATTLE RESULTS & CLAIM REWARD",
      ()=>Navigate(M1Screen.Battle),132f,RuntimeUi.Positive);
    else if(state.HasPendingAbyssBattleReceipt||state.TowerBattleResolved)
     RuntimeUi.AddButton(body,"Bank Tower battle victory 088","BANK VICTORY & UNLOCK NEXT FLOOR",
      ()=>FinishTowerBattleOnly088(coordinator),132f,RuntimeUi.Positive);
    else
     RuntimeUi.AddButton(body,"Enter prepared Tower battle 081","ENTER BATTLE",
      ()=>EnterTowerBattle081(coordinator),132f,RuntimeUi.Warning);
    return;
   }
   if(state.ActiveStepRequiresBattle&&
      StringComparer.Ordinal.Equals(state.ActiveAbyssStatus,"Active"))
   {
    RuntimeUi.AddButton(body,"Enter Tower battle 081","FIGHT FLOOR "+state.TowerFloorNumber,
     ()=>EnterTowerBattle081(coordinator),132f,RuntimeUi.Warning);
    return;
   }
   RuntimeUi.AddButton(body,"Prepare Tower battle only 088",
    StringComparer.Ordinal.Equals(state.ActiveAbyssStatus,"ReadyToFinalize")
     ?"BANK VICTORY & RETURN":"PREPARE THE FLOOR BATTLE",
    ()=>ResumeTowerBattleOnly088(coordinator,
     !StringComparer.Ordinal.Equals(state.ActiveAbyssStatus,"ReadyToFinalize")),132f,
    StringComparer.Ordinal.Equals(state.ActiveAbyssStatus,"ReadyToFinalize")
     ?RuntimeUi.Positive:RuntimeUi.Accent);
  }

  void BeginTowerBattleOnly088(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
  {
   if(TryStartTowerBattle110(coordinator))return;
   if(_towerRoomCommandRunning084)return;
   _towerRoomCommandRunning084=true;
   _suppressBoardAdventureCoordinatorRefresh084=true;
   M1CommandResult result;
   try
   {
    var begun=coordinator?.BeginTowerRun081()??
     M1CommandResult.Failure("The Tower is unavailable. Open it again from the Guild Hall.");
    result=begun.Succeeded
     ?AdvanceTowerBattleOnlyTransitions088(coordinator)
     :begun;
   }
   finally
   {
    _suppressBoardAdventureCoordinatorRefresh084=false;
    _towerRoomCommandRunning084=false;
   }
   _localStatus=result?.Succeeded==true?string.Empty:
    result?.Message??"The Tower could not prepare the next battle.";
   _localStatusPositive=result?.Succeeded==true;
   var latest=coordinator?.CampaignProgression022;
   if(result?.Succeeded==true&&latest!=null&&
      latest.ActiveStepRequiresBattle&&
      StringComparer.Ordinal.Equals(latest.ActiveAbyssStatus,"Active"))
   {
    EnterTowerBattle081(coordinator);
    return;
   }
   BuildCurrentScreen();
  }

  void ResumeTowerBattleOnly088(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator,
   bool enterWhenReady=true)
  {
   if(!enterWhenReady&&TryBeginTowerManual117(coordinator,false))return;
   if(enterWhenReady&&TryStartTowerBattle110(coordinator))return;
   if(_towerRoomCommandRunning084)return;
   _towerRoomCommandRunning084=true;
   _suppressBoardAdventureCoordinatorRefresh084=true;
   M1CommandResult result;
   try
   {
    result=!enterWhenReady&&coordinator is Campaign022.ITowerVictoryBankCoordinator110 bank110
     ?bank110.BankTowerVictory110()
     :AdvanceTowerBattleOnlyTransitions088(coordinator);
   }
   finally
   {
    _suppressBoardAdventureCoordinatorRefresh084=false;
    _towerRoomCommandRunning084=false;
   }

   _localStatus=result?.Succeeded==true?string.Empty:
    result?.Message??"The Tower could not prepare the next battle.";
   _localStatusPositive=result?.Succeeded==true;
   var latest=coordinator?.CampaignProgression022;
   if(result?.Succeeded==true&&latest!=null&&
      latest.ActiveStepRequiresBattle&&
      StringComparer.Ordinal.Equals(latest.ActiveAbyssStatus,"Active")&&
      (enterWhenReady||!latest.TowerBattleResolved))
   {
    EnterTowerBattle081(coordinator);
    return;
   }
   BuildCurrentScreen();
  }

  void FinishTowerBattleOnly088(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
   =>ResumeTowerBattleOnly088(coordinator,false);

  static M1CommandResult AdvanceTowerBattleOnlyTransitions088(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
  {
   if(coordinator==null)return M1CommandResult.Failure("The Tower is unavailable.");
   for(var transition=0;transition<32;transition++)
   {
    var state=coordinator.CampaignProgression022;
    if(state==null||!state.IsAvailable)
     return M1CommandResult.Failure(state?.Error??"The Tower state is unavailable.");
    if(string.IsNullOrWhiteSpace(state.ActiveAbyssOperationId))
     return M1CommandResult.Success("Tower floor cleared and rewards banked.");
    if(state.TowerBattleInProgress||state.TowerBattleRewardAwaitingClaim)
     return M1CommandResult.Success("Tower battle is ready.");
    if(state.TowerBattleResolved&&!state.TowerBattleWon)
     return M1CommandResult.Failure("The climb ended. Return to the Guild and prepare for another attempt.");
    if(StringComparer.Ordinal.Equals(state.ActiveAbyssStatus,"Active")&&
       state.ActiveStepRequiresBattle)
     return M1CommandResult.Success("Tower battle prepared.");

    var advanced=coordinator.AdvanceTowerRun081();
    if(advanced==null||!advanced.Succeeded)
     return advanced??M1CommandResult.Failure("The Tower transition could not be saved.");
   }
   return M1CommandResult.Failure("The Tower safety limit stopped an unexpected route. No additional step was taken.");
  }

  void CommitAndRevealTowerRoom084(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
  {
   if(_towerRoomCommandRunning084)return;
   _towerRoomCommandRunning084=true;
   M1CommandResult result;
   _suppressBoardAdventureCoordinatorRefresh084=true;
   try
   {
    result=coordinator?.AdvanceTowerRun081()??
     M1CommandResult.Failure("The Tower is unavailable. Open the Tower again.");
   }
   finally
   {
    _suppressBoardAdventureCoordinatorRefresh084=false;
    _towerRoomCommandRunning084=false;
   }
   _localStatus=result.Succeeded?string.Empty:result.Message;
   _localStatusPositive=result.Succeeded;
   BuildCurrentScreen();
  }

  void ScheduleTowerReceiptApply084(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator,
   string receiptKey)
  {
   if(!Application.isPlaying||coordinator==null||
      string.IsNullOrWhiteSpace(receiptKey)||
      !_scheduledTowerReceiptApplies084.Add(receiptKey))return;
   StartCoroutine(ApplyTowerReceiptAfterReveal084(coordinator,receiptKey));
  }

  IEnumerator ApplyTowerReceiptAfterReveal084(
   Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator,
   string expectedReceiptKey)
  {
   try
   {
    var revealDuration=_reducedMotion?0f:BoardAdventureCardFlipDuration084;
    yield return new WaitForSecondsRealtime(revealDuration+TowerRoomResultHold084);

    var latest=coordinator?.CampaignProgression022;
    if(latest==null||!latest.HasPendingAbyssStepReceipt||
       !StringComparer.Ordinal.Equals(
        TowerPendingReceiptKey084(latest),expectedReceiptKey))yield break;

    M1CommandResult applied;
    _suppressBoardAdventureCoordinatorRefresh084=true;
    try
    {
     applied=coordinator.AdvanceTowerRun081();
    }
    finally
    {
     _suppressBoardAdventureCoordinatorRefresh084=false;
    }
    _localStatus=applied?.Succeeded==true?string.Empty:
     applied?.Message??"The saved Tower room could not be applied.";
    _localStatusPositive=applied?.Succeeded==true;
    BuildCurrentScreen();
   }
   finally
   {
    // A presenter disable, navigation change, or authority mismatch disposes this
    // iterator. Clear only the in-memory scheduling guard so the same persisted
    // receipt can resume if the player later returns to the Tower screen.
    _scheduledTowerReceiptApplies084.Remove(expectedReceiptKey);
   }
  }

  static string TowerPendingReceiptKey084(
   Campaign022.CampaignProgressionPresentationState022 state)
  {
   if(state==null)return string.Empty;
   return (state.ActiveAbyssOperationId??string.Empty)+"|"+
    (state.ActiveAbyssStatus??string.Empty)+"|"+
    state.ActiveStepIndex+"|"+state.ActiveCompletedStepCount+"|"+
    state.TowerRunNumber+"|"+state.TotalTowerClears+"|"+
    (state.PendingAbyssOutcome??string.Empty)+"|"+
     (state.PendingAbyssReward??string.Empty);
  }

  void BuildTowerResolvedReward086(
   Transform parent,
   Campaign022.CampaignProgressionPresentationState022 state,
   string receiptKey,
   string reward,
   bool positive)
  {
   var surface=RuntimeUi.AddPanel(
    parent,"Tower Resolved Reward Surface 086",
    positive?new Color(0.035f,0.14f,0.10f,0.96f):
     new Color(0.18f,0.075f,0.035f,0.96f));
   RuntimeUi.SetLayout(surface,preferredHeight:78f);
   M1PremiumUi.StylePanel(surface,
    positive?M1PremiumUi.Surface.Positive:M1PremiumUi.Surface.Warning);
   surface.raycastTarget=false;
   var rewardGroup=surface.gameObject.AddComponent<CanvasGroup>();
   BuildBoardAdventureRewardToken084(
    surface.transform,
    TowerResolvedRewardTokenKind086(state,reward),
    "TOWER_REWARD_086|"+receiptKey,
    positive,
    false,
    rewardGroup);
   var copy=RuntimeUi.AddText(
    surface.transform,"Tower Resolved Reward Copy 086",
    "REWARD SAVED  •  "+reward,
    19,TextAnchor.MiddleLeft,RuntimeUi.Text,FontStyle.Bold);
   copy.rectTransform.anchorMin=new Vector2(0f,0f);
   copy.rectTransform.anchorMax=new Vector2(1f,1f);
   copy.rectTransform.offsetMin=new Vector2(96f,8f);
   copy.rectTransform.offsetMax=new Vector2(-12f,-8f);
   ConfigureResponsiveText062(copy,13,20);
   copy.raycastTarget=false;
  }

  static string TowerResolvedRewardTokenKind086(
   Campaign022.CampaignProgressionPresentationState022 state,
   string reward)
  {
   var copy=reward??string.Empty;
   if(copy.IndexOf(" XP",StringComparison.OrdinalIgnoreCase)>=0||
      copy.IndexOf("MATERIAL",StringComparison.OrdinalIgnoreCase)>=0||
      copy.IndexOf("RESONANCE",StringComparison.OrdinalIgnoreCase)>=0||
      copy.IndexOf("CHEST",StringComparison.OrdinalIgnoreCase)>=0)
    return "TREASURE";
   return StringComparer.OrdinalIgnoreCase.Equals(state?.ActiveStepKind,"RESULTS")
    ?"RETURN":"CLUE";
  }

  void BuildTowerAdventureTrack084(
   Transform body,
   Campaign022.CampaignProgressionPresentationState022 state)
  {
   var track=RuntimeUi.AddPanel(body,"Tower Adventure Track 084",
    new Color(0.006f,0.016f,0.028f,0.96f));
   RuntimeUi.SetLayout(track,preferredHeight:124f);
   M1PremiumUi.StylePanel(track,M1PremiumUi.Surface.Iron);
   RuntimeUi.AddHorizontalLayout(track.transform,
    new RectOffset(10,10,8,8),7f,TextAnchor.MiddleCenter);
   var authored=(state.TowerTrackLabels??Array.Empty<string>()).Take(5).ToArray();
   var fallbacks=new[]{"ENTRY","ROOM 2","ROOM 3","ROOM 4","RETURN"};
   var labels=Enumerable.Range(0,5)
    .Select(index=>index<authored.Length&&!string.IsNullOrWhiteSpace(authored[index])
     ?authored[index]:fallbacks[index]).ToArray();
   var pawnIndex=Math.Max(0,Math.Min(4,state.TowerTrackPhase));
   var pawnTravelKey=(state.HasPendingAbyssStepReceipt
     ?TowerPendingReceiptKey084(state)
     :(state.ActiveAbyssOperationId??string.Empty)+"|"+
      state.ActiveStepIndex+"|"+state.TowerTrackPhase);
   for(var index=0;index<5;index++)
   {
    var complete=index<state.TowerTrackPhase;
    var current=index==state.TowerTrackPhase;
    var pawnHere=index==pawnIndex;
    var tile=RuntimeUi.AddPanel(track.transform,"Tower Track Card "+index+" 084",Color.white);
    RuntimeUi.SetLayout(tile,preferredHeight:98f,flexibleWidth:1f);
    M1PremiumUi.StylePanel(tile,
     current?M1PremiumUi.Surface.Warning:
     complete?M1PremiumUi.Surface.Positive:M1PremiumUi.Surface.WorldGlass);
    var copy=RuntimeUi.AddText(
     tile.transform,"Tower Pawn Tile Copy "+index+" 084",
     complete?"✓  "+labels[index]+"\nCLEARED":
     current?"YOUR PAWN\n"+labels[index]:
       "◇  FACE DOWN\n???",
     17,TextAnchor.MiddleCenter,
     current?RuntimeUi.Warning:complete?RuntimeUi.Positive:RuntimeUi.MutedText,
     FontStyle.Bold);
    Stretch(copy.rectTransform);
    copy.rectTransform.offsetMin=new Vector2(pawnHere?43f:6f,4f);
    copy.rectTransform.offsetMax=new Vector2(-6f,-4f);
    ConfigureResponsiveText062(copy,12,18);
    if(pawnHere)
    {
     BuildBoardAdventurePawn084(
      tile.transform,"TOWER_PAWN_086|"+pawnTravelKey+"|"+index,true);
     AnimateBoardAdventureTile084(tile.rectTransform,
      state.ActiveAbyssOperationId+"|C022|"+
      state.ActiveStepIndex+"|"+state.TowerTrackPhase+"|"+index);
    }
    }
  }

   void BuildTowerOperationSelection084(
    Transform body,
    Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator,
    Campaign022.CampaignProgressionPresentationState022 state)
   {
   var operations=(state.TowerOperationChoices??Array.Empty<Campaign022.AbyssOperationView084>())
    .OrderBy(value=>value.FloorNumber)
    .ThenBy(value=>value.Kind=="GUARDIAN"?0:value.Kind=="RECON"?1:2)
    .ToArray();
   if(state.TowerUsesNewFloorPolicy094||operations.Length==0)return;
   if(_towerOperationPage084<0)_towerOperationPage084=Math.Max(0,
    Math.Min(1,(Math.Max(1,state.TowerFloorNumber)-1)/5));
   _towerOperationPage084=Math.Max(0,Math.Min(1,_towerOperationPage084));
   var firstFloor=_towerOperationPage084*5+1;
   var lastFloor=firstFloor+4;
   var page=operations.Where(value=>
    value.FloorNumber>=firstFloor&&value.FloorNumber<=lastFloor).ToArray();
   AddMessagePanel(body,
    "CHOOSE A TOWER ROUTE  •  FLOORS "+firstFloor+"–"+lastFloor,
    "Every floor has three authored five-room boards. Guardian routes are first-clear only; Recon and Trial routes remain repeatable under the existing reward rules.",
    RuntimeUi.ButtonNormal);
   foreach(var floorGroup in page.GroupBy(value=>value.FloorNumber).OrderBy(value=>value.Key))
   {
    var floorPanel=AddMessagePanel(body,
     "FLOOR "+floorGroup.Key+"  •  "+floorGroup.First().FloorDisplayName.ToUpperInvariant(),
     "Pick a route, place the pawn, then flip one saved room at a time.",
     floorGroup.Any(value=>value.Available)?RuntimeUi.Accent:RuntimeUi.ButtonNormal);
    foreach(var operation in floorGroup)
    {
     var captured=operation;
     var route=AddMessagePanel(floorPanel,
      operation.PlayerKind+"  •  "+operation.DisplayName.ToUpperInvariant(),
      operation.StepCount+" FACE-DOWN ROOMS  •  "+
      (operation.RequiresBattle?"UNION BATTLE":"NONBATTLE ROUTE")+
      "\nREWARD  •  "+operation.RewardSummary+
      (operation.Available?string.Empty:"\nLOCKED  •  "+operation.LockedReason),
      operation.Available?RuntimeUi.ButtonNormal:RuntimeUi.MutedText);
     if(operation.Available)
      RuntimeUi.AddButton(route,"Begin authored Tower route "+operation.OperationId+" 084",
       "PLACE PAWN  •  START "+operation.PlayerKind,
       ()=>RunGuildCityCommand017D(
        ()=>coordinator.BeginAbyssOperation022(captured.OperationId)),
       112f,operation.FirstClearOnly?RuntimeUi.Warning:RuntimeUi.Positive);
     UseContentDrivenBoardPanelHeight084(route);
    }
    UseContentDrivenBoardPanelHeight084(floorPanel);
   }
   var nav=AddRow(body,"Tower Route Pages 084",8f,92f);
   RuntimeUi.AddButton(nav,"Previous Tower route page 084","FLOORS 1–5",()=>
   {
    _towerOperationPage084=0;
    BuildCurrentScreen();
   },86f,_towerOperationPage084==0?RuntimeUi.Positive:RuntimeUi.ButtonNormal);
   RuntimeUi.AddButton(nav,"Next Tower route page 084","FLOORS 6–10",()=>
   {
    _towerOperationPage084=1;
    BuildCurrentScreen();
   },86f,_towerOperationPage084==1?RuntimeUi.Positive:RuntimeUi.ButtonNormal);
  }

  bool TryStartTowerBattle110(Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
  {
   if(TryBeginTowerManual117(coordinator,true))return true;
   if(!(coordinator is Campaign022.ITowerBattleStartCoordinator110 start110))return false;
   if(_towerRoomCommandRunning084)return true;
   _towerRoomCommandRunning084=true;
   _suppressBoardAdventureCoordinatorRefresh084=true;
   M1CommandResult result;
   try { result=start110.StartTowerBattle110(); }
   finally
   {
    _suppressBoardAdventureCoordinatorRefresh084=false;
    _towerRoomCommandRunning084=false;
   }
   _localStatus=result?.Message??"The Tower battle could not begin.";
   _localStatusPositive=result?.Succeeded==true;
   if(_localStatusPositive)Navigate(M1Screen.Battle);else BuildCurrentScreen();
   return true;
  }

  void EnterTowerBattle081(Campaign022.ICampaignProgressionPresentationCoordinator022 coordinator)
  {
   if(TryStartTowerBattle110(coordinator))return;
   var result=coordinator?.EnterAbyssBattle022();
   _localStatus=result?.Message??"The Tower battle could not begin.";
   _localStatusPositive=result!=null&&result.Succeeded;
   if(_localStatusPositive)Navigate(M1Screen.Battle);else BuildCurrentScreen();
  }

  bool HasActiveTowerRun081()
  {
   var tower=_coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
   return !string.IsNullOrWhiteSpace(tower?.CampaignProgression022?.ActiveAbyssOperationId);
  }
  void BuildGuildCityCovenants022(Transform body,Campaign022.ICampaignProgressionPresentationCoordinator022 c,Campaign022.CampaignProgressionPresentationState022 s)
  {
   if(s==null||!s.IsAvailable){AddMessagePanel(body,"COVENANTS 022",s?.Error??"Covenant authority unavailable.",RuntimeUi.Warning);return;}
   AddMessagePanel(body,"INVOCATION & GREAT COVENANTS","Invocation Artifacts are owned equipment; sentient Great Covenants are never owned. Both operate through complete Union Forecasts with shared AP and personal MP. No gacha and no individual summon button.",RuntimeUi.Accent);
   AddMessagePanel(body,"SUMMON RESONANCE",s.SummonResonance.ToString(),RuntimeUi.Warning);
   AddMessagePanel(body,"GREAT COVENANT AUTHORITY",s.GreatCovenantGateEarned?"EARNED — canonical first-clear Aegis Gate receipt is linked.":"LOCKED — complete the first clear of the Aegis Gate through its terminal receipt.",s.GreatCovenantGateEarned?RuntimeUi.Positive:RuntimeUi.Warning);

   foreach(var artifact in s.Artifacts??Array.Empty<Campaign022.ArtifactView022>())
   {
    var artifactPanel=AddMessagePanel(body,artifact.DisplayName,"STAGE "+artifact.EvolutionStage+" • RESONANCE "+artifact.Resonance+" • "+artifact.EvolutionPathId+"\nAFFIXES: "+(artifact.AffixIds.Count==0?"NONE":string.Join(", ",artifact.AffixIds))+" • MANUAL EQUIP",RuntimeUi.ButtonNormal);
    RuntimeUi.AddButton(artifactPanel,"Evolve Invocation Artifact "+artifact.InstanceId,"EVOLVE IN PLACE",()=>RunGuildCityCommand017D(()=>c.EvolveInvocationArtifact022(artifact.InstanceId)),118f,RuntimeUi.Accent);
   }

   var affixes=(s.InvocationAffixes??Array.Empty<Campaign022.InvocationAffixView022>()).OrderBy(x=>x.AffixId,StringComparer.Ordinal).ToArray();
   var knownAffixIds=new System.Collections.Generic.HashSet<string>(affixes.Select(x=>x.AffixId),StringComparer.Ordinal);
   _selectedInvocationAffixes022=(_selectedInvocationAffixes022??Array.Empty<string>()).Where(knownAffixIds.Contains).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).Take(Campaign022.InvocationAffixSelectionRules022.MaximumSelected).ToArray();
   _invocationAffixPage022=Campaign022.InvocationAffixSelectionRules022.ClampPage(_invocationAffixPage022,affixes.Length);
   var pageCount=Campaign022.InvocationAffixSelectionRules022.PageCount(affixes.Length);
   AddMessagePanel(body,"INVOCATION AFFIXES — PAGE "+(_invocationAffixPage022+1)+"/"+pageCount,"Select zero, one, or two authored affixes. Exact stable IDs are committed; unknown, duplicate, or third affixes are refused.\nSELECTED: "+(_selectedInvocationAffixes022.Count==0?"NONE":string.Join(", ",_selectedInvocationAffixes022)),RuntimeUi.Accent);
   foreach(var affix in Campaign022.InvocationAffixSelectionRules022.Page(affixes,_invocationAffixPage022))
   {
    var selected=_selectedInvocationAffixes022.Contains(affix.AffixId);
    RuntimeUi.AddButton(body,"Invocation affix "+affix.AffixId,(selected?"✓ ":string.Empty)+affix.DisplayName.ToUpperInvariant()+" • "+affix.ForecastCategory+" • +"+affix.EffectPermille,()=>{_selectedInvocationAffixes022=Campaign022.InvocationAffixSelectionRules022.Toggle(_selectedInvocationAffixes022,affix.AffixId);BuildCurrentScreen();},118f,selected?RuntimeUi.Positive:RuntimeUi.ButtonNormal);
   }
   var pager=AddMessagePanel(body,"AFFIX CATALOG","All 30 authored affixes remain reachable in deterministic stable-ID order, eight per page.",RuntimeUi.ButtonNormal);
   RuntimeUi.AddButton(pager,"Previous Invocation affix page","PREVIOUS 8",()=>{_invocationAffixPage022=Campaign022.InvocationAffixSelectionRules022.ClampPage(_invocationAffixPage022-1,affixes.Length);BuildCurrentScreen();},108f,RuntimeUi.ButtonNormal);
   RuntimeUi.AddButton(pager,"Next Invocation affix page","NEXT 8",()=>{_invocationAffixPage022=Campaign022.InvocationAffixSelectionRules022.ClampPage(_invocationAffixPage022+1,affixes.Length);BuildCurrentScreen();},108f,RuntimeUi.ButtonNormal);
   RuntimeUi.AddButton(pager,"Clear Invocation affix selection","CLEAR SELECTION",()=>{_selectedInvocationAffixes022=Array.Empty<string>();BuildCurrentScreen();},108f,RuntimeUi.Warning);

   var selectedForCraft=_selectedInvocationAffixes022.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
   AddMessagePanel(body,"CRAFT AN INVOCATION ARTIFACT","All 24 authored bases are shown below. Crafting spends earned materials, creates one equipment instance in inventory, and never auto-equips it.",RuntimeUi.Accent);
   foreach(var artifactBase in (s.InvocationArtifactBases??Array.Empty<Campaign022.InvocationArtifactBaseView022>()).OrderBy(x=>x.BaseId,StringComparer.Ordinal))
   {
    var basePanel=AddMessagePanel(body,artifactBase.DisplayName,artifactBase.WeaponFamilyId+"\nCOST: "+string.Join(" • ",artifactBase.MaterialCosts)+"\nAFFIX IDS: "+(selectedForCraft.Length==0?"NONE":string.Join(", ",selectedForCraft)),RuntimeUi.ButtonNormal);
    RuntimeUi.AddButton(basePanel,"Craft Invocation Artifact "+artifactBase.BaseId,"CRAFT WITH SELECTED AFFIXES",()=>RunGuildCityCommand017D(()=>c.CraftInvocationArtifact022(artifactBase.BaseId,selectedForCraft)),118f,RuntimeUi.Positive);
   }

   foreach(var covenant in s.CovenantStates)
   {
    var covenantPanel=AddMessagePanel(body,covenant.DisplayName,covenant.Status+" • TRIAL "+covenant.TrialProgress+"/100 • STAGE RECEIPTS "+covenant.TrialReceiptCount+"/4 • TRUST "+covenant.Trust+(covenant.AcceptanceReceiptApplied?" • ACCEPTANCE RECEIPT SAVED":""),RuntimeUi.ButtonNormal);
    if(covenant.CanAdvanceTrial)RuntimeUi.AddButton(covenantPanel,"Advance covenant trial "+covenant.CovenantId,"COMPLETE NEXT 25-POINT TRIAL STAGE",()=>RunGuildCityCommand017D(()=>c.AdvanceCovenant022(covenant.CovenantId)),118f,RuntimeUi.Accent);
    if(covenant.CanAccept)RuntimeUi.AddButton(covenantPanel,"Accept covenant "+covenant.CovenantId,"VOLUNTARILY ACCEPT COVENANT",()=>RunGuildCityCommand017D(()=>c.AcceptCovenant022(covenant.CovenantId)),118f,RuntimeUi.Positive);
   }
  }
 }
}
