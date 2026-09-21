using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SecondDimension.Presentation
{
 public sealed partial class M1FlowPresenter
 {
  public const string CreatorBonusRoomsToggleLabel084="OPTIONAL BONUS ROOMS";
  public const string CreatorClaimActionLabel084="CLAIM REWARD";
  string _creatorCodeInput028=string.Empty;
  Creator028.CreatorRewardPreview028 _creatorRewardPreview028;
  string _selectedCreatorTarget028=string.Empty;
  int _creatorTargetPage028;
  bool _creatorBonusRoomsOpen084;
  string _creatorClaimedEquipmentId087=string.Empty;
  string _creatorClaimedEquipmentName087=string.Empty;

  void BuildCreatorCodesRooms028(
   Transform body,
   Creator028.ICreatorAccessPresentationCoordinator028 coordinator,
   Creator028.CreatorAccessPresentationState028 state)
  {
   if(coordinator==null||state==null||!state.IsAvailable)
   {
    AddMessagePanel(body,"BONUS CODES",state?.Error??"Bonus codes are unavailable right now.",RuntimeUi.Warning);
    return;
   }

   AddMessagePanel(
    body,
    "REDEEM A BONUS CODE",
   "Enter a code to see its reward. Nothing is claimed until you choose any required target and press Claim Reward.",
   RuntimeUi.Accent);

   BuildCreatorEquipmentHandoff087(body);

   var row=AddRow(body,"Creator Code Input 028",12f,156f);
   var input=RuntimeUi.AddInputField(row,"Creator Code 028","ENTER BONUS CODE",96);
   input.text=_creatorCodeInput028;
   input.onValueChanged.AddListener(value=>
   {
    _creatorCodeInput028=value;
    _creatorRewardPreview028=null;
    _selectedCreatorTarget028=string.Empty;
   });
   var check=RuntimeUi.AddButton(
    row,
    "Preview Creator Code 10000",
    "CHECK CODE",
    ()=>
    {
     _creatorRewardPreview028=coordinator.PreviewCreatorCode028(_creatorCodeInput028);
     _selectedCreatorTarget028=string.Empty;
     _creatorTargetPage028=0;
     if(_creatorRewardPreview028==null)
     {
      _localStatus="That code could not be checked. Try again.";
      _localStatusPositive=false;
     }
     else
     {
      _localStatus=_creatorRewardPreview028.IsValid
       ?(_creatorRewardPreview028.AlreadyRedeemed
        ?"That code was already claimed in this campaign."
        :"Reward found. Review it below before claiming.")
       :_creatorRewardPreview028.Error;
      _localStatusPositive=_creatorRewardPreview028.IsValid&&!_creatorRewardPreview028.AlreadyRedeemed;
     }
     BuildCurrentScreen();
    },
    156f,
    RuntimeUi.Accent);
   RuntimeUi.SetLayout(check,preferredWidth:410f,flexibleWidth:0f);
   ConfigureResponsiveText062(check.GetComponentInChildren<Text>(),18,30);

   BuildCreatorClaimCount084(body,state.RedeemedCodes);
   BuildCreatorRewardPreview10000(body,coordinator);
   BuildCreatorBonusRoomsToggle084(body);
   if(_creatorBonusRoomsOpen084)BuildCreatorBonusRooms084(body,coordinator,state);
  }

  void BuildCreatorRewardPreview10000(
   Transform body,
   Creator028.ICreatorAccessPresentationCoordinator028 coordinator)
  {
   var preview=_creatorRewardPreview028;
   if(preview==null)return;
   if(!preview.IsValid)
   {
    AddMessagePanel(body,"CODE NOT READY",preview.Error??"Check the code and try again.",RuntimeUi.Warning);
    return;
   }

   var rewardColor=preview.RequiresCreatorPowerConfirmation?RuntimeUi.Warning:RuntimeUi.Positive;
   BuildCreatorRewardCard084(body,preview,rewardColor);
   if(preview.AlreadyRedeemed)
   {
    AddMessagePanel(
     body,
     "ALREADY CLAIMED",
     "This reward is already saved in this campaign. Nothing was added twice.",
     RuntimeUi.Warning);
    return;
   }

   var targets=preview.TargetOptions??Array.Empty<Creator028.CreatorRewardTargetOption028>();
   if(!string.IsNullOrWhiteSpace(preview.TargetMode))
   {
    AddMessagePanel(
     body,
     "CHOOSE THE TARGET",
     string.IsNullOrWhiteSpace(preview.TargetPrompt)
      ?"Choose who receives this reward."
      :preview.TargetPrompt,
     RuntimeUi.Accent);
    if(targets.Count==0)
    {
     AddMessagePanel(
      body,
      "NO ELIGIBLE TARGET YET",
      "Prepare an eligible member or Union, then return here. The code remains unclaimed.",
      RuntimeUi.Warning);
     return;
    }

    var pageSize=Creator028.CreatorRewardTargetPaging10000.PageSize;
    var pages=Creator028.CreatorRewardTargetPaging10000.PageCount(targets.Count);
    _creatorTargetPage028=Math.Max(0,Math.Min(pages-1,_creatorTargetPage028));
    var start=_creatorTargetPage028*pageSize;
    var end=Math.Min(targets.Count,start+pageSize);
    for(var index=start;index<end;index+=2)
    {
     var targetRow=AddRow(body,"Creator Target Row "+index,10f,146f);
     AddCreatorTargetButton10000(targetRow,targets[index]);
     if(index+1<end)AddCreatorTargetButton10000(targetRow,targets[index+1]);
    }

    if(pages>1)
    {
     var pager=AddRow(body,"Creator Target Pager",10f,132f);
     if(_creatorTargetPage028>0)
      RuntimeUi.AddButton(
       pager,
       "Previous Creator Targets",
       "◀ PREVIOUS",
       ()=>{_creatorTargetPage028--;BuildCurrentScreen();},
       132f,
       RuntimeUi.ButtonNormal);
     RuntimeUi.AddButton(
      pager,
      "Creator Target Page",
      "PAGE "+(_creatorTargetPage028+1)+" / "+pages,
      ()=>{},
      132f,
      RuntimeUi.ButtonNormal).interactable=false;
     if(_creatorTargetPage028+1<pages)
      RuntimeUi.AddButton(
       pager,
       "Next Creator Targets",
       "NEXT ▶",
       ()=>{_creatorTargetPage028++;BuildCurrentScreen();},
       132f,
       RuntimeUi.ButtonNormal);
    }

    var selected=targets.FirstOrDefault(value=>
     StringComparer.Ordinal.Equals(value.TargetId,_selectedCreatorTarget028));
    BuildCreatorCompactMessage084(
     body,
     "Creator Selected Target 084",
     selected==null?"TARGET NEEDED":"TARGET READY",
     selected==null
      ?"Choose one target above."
      :selected.DisplayName+"  •  "+selected.Detail,
     selected==null?RuntimeUi.Warning:RuntimeUi.Positive,
     150f);
   }

   if(preview.RequiresCreatorPowerConfirmation)
   {
    var warning=string.IsNullOrWhiteSpace(preview.CreatorPowerWarning)
     ?"This reward changes the campaign's Creator Power status."
     :preview.CreatorPowerWarning;
    AddMessagePanel(
     body,
     "IMPORTANT BEFORE CLAIMING",
     warning+" Pressing Claim Reward confirms that you understand.",
     RuntimeUi.Warning);
   }

   var canClaim=string.IsNullOrWhiteSpace(preview.TargetMode)||
                !string.IsNullOrWhiteSpace(_selectedCreatorTarget028);
   if(!canClaim)return;
   BuildCreatorClaimAction084(body,coordinator,preview);
  }

  private static void BuildCreatorClaimCount084(Transform body,int redeemedCodes)
  {
   var panel=RuntimeUi.AddPanel(
    body,
    "Creator Rewards Claimed 084",
    new Color(0.020f,0.038f,0.055f,0.96f));
   RuntimeUi.SetLayout(panel,preferredHeight:82f);
   M1PremiumUi.StylePanel(panel,M1PremiumUi.Surface.WorldGlass);
   var copy=RuntimeUi.AddText(
    panel.transform,
    "Creator Rewards Claimed Copy 084",
    "REWARDS CLAIMED "+Math.Max(0,redeemedCodes),
    24,
    TextAnchor.MiddleCenter,
    RuntimeUi.MutedText,
    FontStyle.Bold);
   Stretch(copy.rectTransform);
   copy.rectTransform.offsetMin=new Vector2(18f,6f);
   copy.rectTransform.offsetMax=new Vector2(-18f,-6f);
   ConfigureResponsiveText062(copy,16,24);
   copy.raycastTarget=false;
  }

  private static void BuildCreatorRewardCard084(
   Transform body,
   Creator028.CreatorRewardPreview028 preview,
   Color color)
  {
   var panel=RuntimeUi.AddPanel(
    body,
    "Creator Compact Reward Preview 084",
    new Color(0.025f,0.050f,0.065f,0.98f));
   RuntimeUi.SetLayout(panel,preferredHeight:214f);
   M1PremiumUi.StylePanel(
    panel,
    preview.RequiresCreatorPowerConfirmation
     ?M1PremiumUi.Surface.Warning
     :M1PremiumUi.Surface.Positive);
   RuntimeUi.AddVerticalLayout(
    panel.transform,
    new RectOffset(26,26,14,14),
    4f,
    TextAnchor.MiddleLeft);
   var heading=RuntimeUi.AddText(
    panel.transform,
    "Creator Reward Preview Heading 084",
    "REWARD PREVIEW",
    25,
    TextAnchor.MiddleLeft,
    color,
    FontStyle.Bold);
   RuntimeUi.SetLayout(heading,preferredHeight:38f);
   ConfigureResponsiveText062(heading,17,25);
   var label=RuntimeUi.AddText(
    panel.transform,
    "Creator Reward Preview Label 084",
    FriendlyCreatorRewardKind084(preview.Category)+"  •  "+
    (string.IsNullOrWhiteSpace(preview.Label)?"BONUS REWARD":preview.Label),
    28,
    TextAnchor.MiddleLeft,
    RuntimeUi.Text,
    FontStyle.Bold);
   RuntimeUi.SetLayout(label,preferredHeight:52f);
   ConfigureResponsiveText062(label,18,28);
   var summary=RuntimeUi.AddText(
    panel.transform,
    "Creator Reward Preview Summary 084",
    string.IsNullOrWhiteSpace(preview.RewardSummary)
     ?"This reward will be saved when claimed."
     :preview.RewardSummary,
    22,
    TextAnchor.UpperLeft,
    RuntimeUi.Text,
    FontStyle.Normal);
   RuntimeUi.SetLayout(summary,preferredHeight:86f);
   ConfigureResponsiveText062(summary,15,22);
   summary.verticalOverflow=VerticalWrapMode.Truncate;
  }

  private void BuildCreatorClaimAction084(
   Transform body,
   Creator028.ICreatorAccessPresentationCoordinator028 coordinator,
   Creator028.CreatorRewardPreview028 preview)
  {
   var panel=RuntimeUi.AddPanel(
    body,
    "Creator Claim Reward Panel 084",
    new Color(0.020f,0.045f,0.052f,0.98f));
   RuntimeUi.SetLayout(panel,preferredHeight:272f);
   M1PremiumUi.StylePanel(
    panel,
    preview.RequiresCreatorPowerConfirmation
     ?M1PremiumUi.Surface.Warning
     :M1PremiumUi.Surface.Positive);
   RuntimeUi.AddVerticalLayout(
    panel.transform,
    new RectOffset(24,24,14,14),
    8f,
    TextAnchor.MiddleCenter);
   var heading=RuntimeUi.AddText(
    panel.transform,
    "Creator Claim Reward Heading 084",
    "READY TO CLAIM",
    26,
    TextAnchor.MiddleCenter,
    preview.RequiresCreatorPowerConfirmation?RuntimeUi.Warning:RuntimeUi.Positive,
    FontStyle.Bold);
   RuntimeUi.SetLayout(heading,preferredHeight:38f);
   ConfigureResponsiveText062(heading,18,26);
   var copy=RuntimeUi.AddText(
    panel.transform,
    "Creator Claim Reward Copy 084",
    "The complete reward saves once. Equipment goes to Guild inventory and is never equipped automatically.",
    21,
    TextAnchor.MiddleCenter,
    RuntimeUi.Text,
    FontStyle.Normal);
   RuntimeUi.SetLayout(copy,preferredHeight:60f);
   ConfigureResponsiveText062(copy,15,21);
   var claim=RuntimeUi.AddButton(
    panel.transform,
    "Commit Creator Reward 10000",
    CreatorClaimActionLabel084,
    ()=>
    {
     var inventoryBefore087=CreatorProjectedEquipmentIds087(_coordinator?.State);
     var result=coordinator.RedeemCreatorCode028(
      _creatorCodeInput028,
      _selectedCreatorTarget028,
      preview.RequiresCreatorPowerConfirmation);
     _localStatus=result.Message;
     _localStatusPositive=result.Succeeded;
     if(result.Succeeded)
     {
      var claimedEquipment087=FindNewCreatorEquipment087(
       _coordinator?.State,
       inventoryBefore087);
      _creatorClaimedEquipmentId087=claimedEquipment087?.ItemId??string.Empty;
      _creatorClaimedEquipmentName087=claimedEquipment087?.DisplayName??string.Empty;
      _creatorCodeInput028=string.Empty;
      _creatorRewardPreview028=null;
      _selectedCreatorTarget028=string.Empty;
      _creatorTargetPage028=0;
     }
     BuildCurrentScreen();
    },
    132f,
    preview.RequiresCreatorPowerConfirmation?RuntimeUi.Warning:RuntimeUi.Positive);
   ConfigureResponsiveText062(claim.GetComponentInChildren<Text>(),19,30);
  }

  private void BuildCreatorEquipmentHandoff087(Transform body)
  {
   if(string.IsNullOrWhiteSpace(_creatorClaimedEquipmentId087))return;
   var panel=RuntimeUi.AddPanel(
    body,
    "Creator Equipment Handoff 087",
    new Color(0.018f,0.075f,0.085f,0.98f));
   RuntimeUi.SetLayout(panel,preferredHeight:250f);
   M1PremiumUi.StylePanel(panel,M1PremiumUi.Surface.Positive);
   RuntimeUi.AddVerticalLayout(
    panel.transform,
    new RectOffset(24,24,14,14),
    7f,
    TextAnchor.MiddleCenter);
   var heading=RuntimeUi.AddText(
    panel.transform,
    "Creator Equipment Handoff Heading 087",
    "WEAPON ADDED TO YOUR ARMORY",
    27,
    TextAnchor.MiddleCenter,
    RuntimeUi.Positive,
    FontStyle.Bold);
   RuntimeUi.SetLayout(heading,preferredHeight:40f);
   var copy=RuntimeUi.AddText(
    panel.transform,
    "Creator Equipment Handoff Copy 087",
    (string.IsNullOrWhiteSpace(_creatorClaimedEquipmentName087)
     ?"Your new equipment"
     :_creatorClaimedEquipmentName087)+
    " is saved. Choose its wielder and preview every combat-stat change before equipping.",
    21,
    TextAnchor.MiddleCenter,
    RuntimeUi.Text,
    FontStyle.Bold);
   RuntimeUi.SetLayout(copy,preferredHeight:72f);
   ConfigureResponsiveText062(copy,15,21);
   var itemId087=_creatorClaimedEquipmentId087;
   var choose=RuntimeUi.AddButton(
    panel.transform,
    "Choose Creator Equipment Wielder 087",
    "CHOOSE WHO EQUIPS IT",
    ()=>OpenCreatorRewardInventory087(itemId087),
    132f,
    RuntimeUi.Positive);
   ConfigureResponsiveText062(choose.GetComponentInChildren<Text>(),19,30);
  }

  private static HashSet<string> CreatorProjectedEquipmentIds087(
   M1PresentationState state)
  {
   var result=new HashSet<string>(StringComparer.Ordinal);
   foreach(var recruit in state?.Recruits??Array.Empty<M1RecruitLoadoutView>())
    foreach(var slot in recruit?.Slots??Array.Empty<M1EquipmentSlotView>())
     foreach(var choice in slot?.Choices??Array.Empty<M1EquipmentChoiceView>())
      if(choice!=null&&!string.IsNullOrWhiteSpace(choice.ItemId))result.Add(choice.ItemId);
   return result;
  }

  private static M1EquipmentChoiceView FindNewCreatorEquipment087(
   M1PresentationState state,
   HashSet<string> existingIds)
  {
   existingIds=existingIds??new HashSet<string>(StringComparer.Ordinal);
   return (state?.Recruits??Array.Empty<M1RecruitLoadoutView>())
    .Where(recruit=>recruit!=null)
    .SelectMany(recruit=>recruit.Slots??Array.Empty<M1EquipmentSlotView>())
    .Where(slot=>slot!=null)
    .SelectMany(slot=>slot.Choices??Array.Empty<M1EquipmentChoiceView>())
    .FirstOrDefault(choice=>choice!=null&&
     !string.IsNullOrWhiteSpace(choice.ItemId)&&
     !existingIds.Contains(choice.ItemId)&&
     choice.ItemId.StartsWith("CRITEM10000_",StringComparison.Ordinal));
  }

  private void BuildCreatorBonusRoomsToggle084(Transform body)
  {
   var row=AddRow(
    body,
    "Creator Optional Bonus Rooms Toggle Row 084",
    0f,
    132f);
   var layout=row.GetComponent<HorizontalLayoutGroup>();
   if(layout!=null)layout.childForceExpandWidth=false;
   var toggle=RuntimeUi.AddButton(
    row,
    "Creator Optional Bonus Rooms Toggle 084",
    _creatorBonusRoomsOpen084
     ?"HIDE OPTIONAL BONUS ROOMS"
     :CreatorBonusRoomsToggleLabel084,
    ()=>
    {
     _creatorBonusRoomsOpen084=!_creatorBonusRoomsOpen084;
     BuildCurrentScreen();
    },
    132f,
    RuntimeUi.ButtonNormal);
   RuntimeUi.SetLayout(toggle,preferredWidth:620f,flexibleWidth:0f);
   ConfigureResponsiveText062(toggle.GetComponentInChildren<Text>(),17,25);
  }

  private void BuildCreatorBonusRooms084(
   Transform body,
   Creator028.ICreatorAccessPresentationCoordinator028 coordinator,
   Creator028.CreatorAccessPresentationState028 state)
  {
   var room=state.CurrentRoom;
   if(!string.IsNullOrWhiteSpace(state.ActiveRoomVisitId))
   {
    BuildCreatorRoomCard084(
     body,
     room?.DisplayName??"BONUS ROOM IN PROGRESS",
     room?.Description??"Finish this optional room to receive its saved reward.",
     "READY",
     FriendlyCreatorRewardKind084(room?.RewardKind),
     "FINISH BONUS ROOM",
     ()=>RunGuildCityCommand017D(coordinator.ResolveCreatorRoom028));
    return;
   }

   if(room!=null)
   {
    BuildCreatorRoomCard084(
     body,
     string.IsNullOrWhiteSpace(room.DisplayName)?"OPTIONAL BONUS ROOM":room.DisplayName,
     string.IsNullOrWhiteSpace(room.Description)
      ?"A hidden bonus challenge waits here."
      :room.Description,
     room.KeyUnlocked?"READY":"LOCKED",
     FriendlyCreatorRewardKind084(room.RewardKind),
     room.KeyUnlocked?"ENTER BONUS ROOM":string.Empty,
     room.KeyUnlocked
      ?(Action)(()=>RunGuildCityCommand017D(coordinator.EnterCreatorRoom028))
      :null);
    return;
   }

   BuildCreatorCompactMessage084(
    body,
    "Creator No Bonus Room 084",
    "NO BONUS ROOM HERE",
    "Bonus rooms appear on certain mission spaces. Keep exploring and check again when one is discovered.",
    RuntimeUi.MutedText,
    168f);
  }

  private static void BuildCreatorRoomCard084(
   Transform body,
   string displayName,
   string description,
   string status,
   string rewardKind,
   string actionLabel,
   Action action)
  {
   var hasAction=action!=null&&!string.IsNullOrWhiteSpace(actionLabel);
   var panel=RuntimeUi.AddPanel(
    body,
    "Creator Optional Bonus Room Card 084",
    new Color(0.025f,0.048f,0.065f,0.98f));
   RuntimeUi.SetLayout(panel,preferredHeight:hasAction?348f:214f);
   M1PremiumUi.StylePanel(
    panel,
    StringComparer.Ordinal.Equals(status,"READY")
     ?M1PremiumUi.Surface.Positive
     :M1PremiumUi.Surface.Warning);
   RuntimeUi.AddVerticalLayout(
    panel.transform,
    new RectOffset(26,26,14,14),
    6f,
    TextAnchor.MiddleLeft);
   var title=RuntimeUi.AddText(
    panel.transform,
    "Creator Optional Bonus Room Name 084",
    displayName,
    28,
    TextAnchor.MiddleLeft,
    RuntimeUi.Text,
    FontStyle.Bold);
   RuntimeUi.SetLayout(title,preferredHeight:44f);
   ConfigureResponsiveText062(title,18,28);
   var stateCopy=RuntimeUi.AddText(
    panel.transform,
    "Creator Optional Bonus Room Status 084",
    status,
    23,
    TextAnchor.MiddleLeft,
    StringComparer.Ordinal.Equals(status,"READY")?RuntimeUi.Positive:RuntimeUi.Warning,
    FontStyle.Bold);
   RuntimeUi.SetLayout(stateCopy,preferredHeight:32f);
   ConfigureResponsiveText062(stateCopy,16,23);
   var descriptionCopy=RuntimeUi.AddText(
    panel.transform,
    "Creator Optional Bonus Room Description 084",
    description,
    21,
    TextAnchor.UpperLeft,
    RuntimeUi.Text,
    FontStyle.Normal);
   RuntimeUi.SetLayout(descriptionCopy,preferredHeight:64f);
   ConfigureResponsiveText062(descriptionCopy,15,21);
   descriptionCopy.verticalOverflow=VerticalWrapMode.Truncate;
   var reward=RuntimeUi.AddText(
    panel.transform,
    "Creator Optional Bonus Room Reward Kind 084",
    "REWARD  •  "+rewardKind,
    21,
    TextAnchor.MiddleLeft,
    RuntimeUi.Accent,
    FontStyle.Bold);
   RuntimeUi.SetLayout(reward,preferredHeight:34f);
   ConfigureResponsiveText062(reward,15,21);
   if(!hasAction)return;

   var button=RuntimeUi.AddButton(
    panel.transform,
    "Creator Optional Bonus Room Action 084",
    actionLabel,
    action,
    132f,
    RuntimeUi.Accent);
   ConfigureResponsiveText062(button.GetComponentInChildren<Text>(),18,28);
  }

  private static void BuildCreatorCompactMessage084(
   Transform body,
   string name,
   string heading,
   string message,
   Color color,
   float height)
  {
   var panel=RuntimeUi.AddPanel(
    body,
    name,
    new Color(0.022f,0.042f,0.058f,0.98f));
   RuntimeUi.SetLayout(panel,preferredHeight:height);
   M1PremiumUi.StylePanel(panel,M1PremiumUi.Surface.WorldGlass);
   RuntimeUi.AddVerticalLayout(
    panel.transform,
    new RectOffset(24,24,12,12),
    4f,
    TextAnchor.MiddleLeft);
   var title=RuntimeUi.AddText(
    panel.transform,
    name+" Heading",
    heading,
    23,
    TextAnchor.MiddleLeft,
    color,
    FontStyle.Bold);
   RuntimeUi.SetLayout(title,preferredHeight:36f);
   ConfigureResponsiveText062(title,16,23);
   var copy=RuntimeUi.AddText(
    panel.transform,
    name+" Copy",
    message,
    20,
    TextAnchor.UpperLeft,
    RuntimeUi.Text,
    FontStyle.Normal);
   RuntimeUi.SetLayout(copy,preferredHeight:Mathf.Max(64f,height-64f));
   ConfigureResponsiveText062(copy,14,20);
   copy.verticalOverflow=VerticalWrapMode.Truncate;
  }

  private static string FriendlyCreatorRewardKind084(string value)
  {
   switch((value??string.Empty).Trim().ToUpperInvariant())
   {
    case "CHARACTER":return "NEW GUILD MEMBER";
    case "RESOURCE":return "GUILD SUPPLIES";
    case "WEAPON":
    case "EQUIPMENT":return "EQUIPMENT";
    case "CONTENT":
    case "CHRONICLE":return "STORY BONUS";
    case "ROOM_KEY":return "BONUS ROOM ACCESS";
    case "XP":
    case "XP_VOUCHER":return "GROWTH XP";
    case "INVENTORY_ITEM":return "INVENTORY ITEM";
    case "GROWTH_BOOST_ITEM":return "GROWTH ITEM";
    case "EQUIPMENT_INSTANCE":return "EQUIPMENT";
    case "MULTI_GRANT":return "REWARD CACHE";
    case "":return "BONUS REWARD";
    default:
     var readable=value.Replace('_',' ').Trim().ToLowerInvariant();
     return string.IsNullOrWhiteSpace(readable)
      ?"BONUS REWARD"
      :CultureInfo.InvariantCulture.TextInfo.ToTitleCase(readable);
   }
  }

  void AddCreatorTargetButton10000(Transform row,Creator028.CreatorRewardTargetOption028 option)
  {
   var captured=option;
   var selected=StringComparer.Ordinal.Equals(_selectedCreatorTarget028,captured.TargetId);
   var button=RuntimeUi.AddButton(
    row,
    "Creator Target "+captured.TargetId,
    (selected?"✓ ":string.Empty)+captured.DisplayName+"\n"+captured.Detail,
    ()=>
    {
     _selectedCreatorTarget028=captured.TargetId;
     BuildCurrentScreen();
    },
    146f,
    selected?RuntimeUi.Positive:RuntimeUi.ButtonNormal);
   ConfigureResponsiveText062(button.GetComponentInChildren<Text>(),16,25);
  }
 }
}
