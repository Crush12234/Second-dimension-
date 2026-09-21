using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.RelicCode1000;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.SSSTenV4;
namespace SecondDimension.Presentation
{
 public sealed partial class M1RuntimeCoordinator
 {
  readonly CreatorAccessCommandService028 _creatorCommands028=new CreatorAccessCommandService028();
  readonly CreatorGiveawayCommandService10000 _creatorGiveawayCommands10000=new CreatorGiveawayCommandService10000();
  readonly RelicCodeCommandService1000 _relicCodeCommands1000=new RelicCodeCommandService1000();
  readonly SssTenV4CreatorCommand090 _sssTenV4Creator090=new SssTenV4CreatorCommand090();
  Creator028.CreatorRegistry028 _creatorRegistry028; Creator028.CreatorGiveawayRegistry10000 _creatorGiveawayRegistry10000;
  SpecialRelic001.SpecialRelicRegistry001 _specialRelicRegistry001; RelicCode1000.RelicCodeRegistry1000 _relicCodeRegistry1000;
  Creator028.HeroMaster300CreatorRegistry087 _heroMaster300CreatorRegistry087;
  bool _heroMaster300CreatorRegistryAttempted087;
  Creator028.CreatorRegistry028 CreatorRegistry028()=>_creatorRegistry028??(_creatorRegistry028=Creator028.CreatorRegistry028.Load());
  Creator028.CreatorGiveawayRegistry10000 CreatorGiveawayRegistry10000()=>_creatorGiveawayRegistry10000??(_creatorGiveawayRegistry10000=Creator028.CreatorGiveawayRegistry10000.Load());
  SpecialRelic001.SpecialRelicRegistry001 SpecialRelicRegistry001()=>_specialRelicRegistry001??(_specialRelicRegistry001=SpecialRelic001.SpecialRelicRegistry001.Load());
  RelicCode1000.RelicCodeRegistry1000 RelicCodeRegistry1000()=>_relicCodeRegistry1000??(_relicCodeRegistry1000=RelicCode1000.RelicCodeRegistry1000.Load());
  bool TryHeroMaster300CreatorRegistry087(out Creator028.HeroMaster300CreatorRegistry087 registry)
  {
   if(!_heroMaster300CreatorRegistryAttempted087)
   {
    _heroMaster300CreatorRegistryAttempted087=true;
    try{_heroMaster300CreatorRegistry087=Creator028.HeroMaster300CreatorRegistry087.Load();}
    catch{_heroMaster300CreatorRegistry087=null;}
   }
   registry=_heroMaster300CreatorRegistry087;
   return registry!=null;
  }
  public Creator028.CreatorAccessPresentationState028 CreatorAccess028=>BuildCreatorAccess028();
  Creator028.CreatorAccessPresentationState028 BuildCreatorAccess028(){try{var reg=CreatorRegistry028();var giveaway=CreatorGiveawayRegistry10000();var relicCodes=RelicCodeRegistry1000();var heroMasterCodeCount=TryHeroMaster300CreatorRegistry087(out var heroMaster)?heroMaster.Codes.CodeCount:0;var s=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();CreatorRoomRule028 room=null;var playable=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020;if(playable?.WorldGate023?.ActiveOperation!=null){var op=playable.WorldGate023.ActiveOperation;reg.TryGetRoomForBoard(op.DefinitionId,op.BoardId,out room);if(room!=null&&!StringComparer.Ordinal.Equals(room.EntryNodeId,op.CurrentNodeId))room=null;}else if(_campaign?.Guild?.GuildCity?.Expedition!=null){var ex=_campaign.Guild.GuildCity.Expedition;reg.TryGetRoomForBoard(_campaign.Guild.GuildCity.ActiveContract?.ContractId,ex.BoardId,out room);if(room!=null&&!StringComparer.Ordinal.Equals(room.EntryNodeId,ex.CurrentNodeId))room=null;}return new Creator028.CreatorAccessPresentationState028{IsAvailable=true,TotalCodes=reg.CodeCount+giveaway.CodeCount+relicCodes.CodeCount+heroMasterCodeCount+_sssTenV4Creator090.CodeCount,RedeemedCodes=s.RedeemedCodeIds.Count,CreatorTokens=s.CreatorTokens,UnlockedContent=s.UnlockedContentIds.Count,RoomKeys=s.UnlockedRoomKeyIds.Count,CompletedRooms=s.CompletedRoomIds.Count,ActiveRoomVisitId=s.ActiveRoomVisit?.VisitId??string.Empty,LastCheckpointId=s.LastCheckpointId,CurrentRoom=room==null?null:new Creator028.CreatorRoomView028{RoomId=room.RoomId,DisplayName=room.DisplayName,RoomType=room.RoomType,Description=room.Description,RewardKind=room.RewardKind,RewardId=room.RewardId,EntryNodeId=room.EntryNodeId,ArtResourcePath=room.ArtResourcePath,RequiredRoomKeyId=room.BonusKeyId,KeyUnlocked=string.IsNullOrWhiteSpace(room.BonusKeyId)||s.UnlockedRoomKeyIds.Contains(room.BonusKeyId)}};}catch(Exception e){return new Creator028.CreatorAccessPresentationState028{IsAvailable=false,Error=e.Message};}}

  public Creator028.CreatorRewardPreview028 PreviewCreatorCode028(string input)
  {
   try
   {
    if(string.IsNullOrWhiteSpace(input))return InvalidCreatorPreview028("Enter a bonus code first.");
    if(_sssTenV4Creator090.TryPreview(_campaign,input,out var sssPreview090))
    {
     return new Creator028.CreatorRewardPreview028
     {
      IsValid=true,
      AlreadyRedeemed=sssPreview090.AlreadyRedeemed,
      CodeId=sssPreview090.CodeId,
      RewardBundleId=sssPreview090.HeroId,
      Label=sssPreview090.RewardName,
      Category=sssPreview090.IsPrimaryRecruit?"CHARACTER":sssPreview090.IsAscensionCredit?"RESOURCE":"WEAPON",
      Rarity="SSS",
      RewardSummary=sssPreview090.Summary+(sssPreview090.CanRedeem?string.Empty:"\nLOCKED • Recruit "+sssPreview090.HeroName+" first; attempting now consumes nothing."),
      TargetMode=string.Empty,
      TargetPrompt=string.Empty,
      TargetOptions=Array.Empty<Creator028.CreatorRewardTargetOption028>()
     };
    }
    var relicCodes=RelicCodeRegistry1000();var relicHash=CreatorGiveawayCommandService10000.HashCode10000(input);
    if(relicCodes.TryGetCodeByHash(relicHash,out var relicCode))
    {
     if(!relicCodes.TryGetRewardBundle(relicCode.RewardBundleId,out var relicBundle))return InvalidCreatorPreview028("The relic reward attached to this code is unavailable.");
     var relics=SpecialRelicRegistry001();var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     SecondDimension.Gameplay.SpecialRelic001.SpecialRelicRule001 directRelic=null;
     if(relicBundle.IsDirect&&!relics.TryGetRelic(relicBundle.RelicId,out directRelic))return InvalidCreatorPreview028("The special relic attached to this code is unavailable.");
     var directIsP0=directRelic!=null&&relics.IsP0(directRelic.RelicId);var previewLabel=directRelic==null?"Committed Special Relic Cache":directRelic.Name+(directIsP0?" • ACTIVE P0":" • SEALED");var previewSummary=relicBundle.IsCache?"Special Relic cache • outcome commits exactly once on claim • manual Tool Relic equip • P0 outcomes are active; other outcomes remain sealed data-only until their combat adapter ships • duplicates convert to Resonance Thread.":directIsP0?"Active P0 Special Relic • stored in Guild inventory • manual Tool Relic equip only • owned duplicates convert to Resonance Thread.":"Sealed Special Relic • stored in Guild inventory • manual Tool Relic equip only • combat adapter is not active yet • owned duplicates convert to Resonance Thread.";
     return new Creator028.CreatorRewardPreview028{IsValid=true,AlreadyRedeemed=state.RedeemedCodeIds.Contains(relicCode.CodeId),CodeId=relicCode.CodeId,RewardBundleId=relicBundle.RewardBundleId,Label=previewLabel,Category=relicCode.Category,Rarity=directRelic?.Rarity??"CACHE",RewardSummary=previewSummary,TargetMode=string.Empty,TargetPrompt=string.Empty,TargetOptions=Array.Empty<Creator028.CreatorRewardTargetOption028>()};
    }
    var giveaway=CreatorGiveawayRegistry10000();
    var giveawayHash=CreatorGiveawayCommandService10000.HashCode10000(input);
    if(giveaway.TryGetCodeByHash(giveawayHash,out var code))
    {
     if(!giveaway.TryGetRewardBundle(code.RewardBundleId,out var bundle))return InvalidCreatorPreview028("The reward attached to this code is unavailable.");
     var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     var targetMode=CreatorGiveawayCommandService10000.RequiredTargetMode(bundle);
     var options=BuildCreatorTargetOptions10000(targetMode);
     return new Creator028.CreatorRewardPreview028
     {
      IsValid=true,AlreadyRedeemed=state.RedeemedCodeIds.Contains(code.CodeId),CodeId=code.CodeId,
      RewardBundleId=bundle.RewardBundleId,Label=bundle.Label,Category=bundle.Category,Rarity=bundle.Rarity,
      RewardSummary=CreatorRewardSummary10000(bundle),TargetMode=targetMode,
      TargetPrompt=CreatorTargetPrompt10000(targetMode),TargetOptions=options,
      RequiresCreatorPowerConfirmation=code.CreatorPowerFlag||bundle.CreatorPowerFlag,
      CreatorPowerWarning=(code.CreatorPowerFlag||bundle.CreatorPowerFlag)
       ?"CREATOR Ω POWER — This fully awakened weapon marks this campaign CREATOR_POWER_USED, makes the run noncanon/sandbox, and is not eligible for competitive play. It enters inventory and never auto-equips."
       :string.Empty
     };
    }
    if(TryHeroMaster300CreatorRegistry087(out var heroMaster)&&
       heroMaster.Codes.TryResolveInput(input,out var heroCode,out var hero))
    {
     var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     var rosterDuplicate=HeroMaster300CreatorRecruitProjection087.RosterContainsHero(_campaign?.Guild?.Recruits,hero);
     var alreadyRedeemed=state.RedeemedCodeIds.Contains(heroCode.CodeId)||
                         state.ClaimedInvitationIds.Contains(heroCode.RewardId)||
                         rosterDuplicate;
     return new Creator028.CreatorRewardPreview028
     {
      IsValid=true,
      AlreadyRedeemed=alreadyRedeemed,
      CodeId=heroCode.CodeId,
      RewardBundleId=heroCode.RewardId,
      Label=heroCode.DisplayLabel,
      Category=heroCode.Category,
      Rarity="SS",
      RewardSummary=HeroMaster300RewardSummary087(hero,rosterDuplicate),
      TargetMode=string.Empty,
      TargetPrompt=string.Empty,
      TargetOptions=Array.Empty<Creator028.CreatorRewardTargetOption028>()
     };
    }
    var legacy=CreatorRegistry028();var legacyHash=CreatorAccessCommandService028.HashNormalizedCode(input);
    if(legacy.TryGetCodeByHash(legacyHash,out var oldCode)&&oldCode.Active)
    {
     var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     return new Creator028.CreatorRewardPreview028{IsValid=true,AlreadyRedeemed=state.RedeemedCodeIds.Contains(oldCode.CodeId),CodeId=oldCode.CodeId,RewardBundleId=oldCode.RewardId,Label=oldCode.DisplayLabel,Category=oldCode.Category,Rarity="LEGACY",RewardSummary="Original Creator reward • applied through the certified 300-code catalog.",TargetMode=string.Empty,TargetPrompt=string.Empty,TargetOptions=Array.Empty<Creator028.CreatorRewardTargetOption028>()};
    }
    return InvalidCreatorPreview028("That bonus code was not recognized. Check the letters and numbers, then try again.");
   }
   catch(Exception e){return InvalidCreatorPreview028(e.Message);}
  }

  public M1CommandResult RedeemCreatorCode028(string input)=>RedeemCreatorCode028(input,string.Empty,false);

  public M1CommandResult RedeemCreatorCode028(string input,string targetId,bool creatorPowerConfirmed)
  {
   try
   {
    if(_sssTenV4Creator090.TryPreview(_campaign,input,out var sssPreview090))
    {
     var result=_sssTenV4Creator090.Redeem(_campaign,input);
     if(result.IsSuccess)result=SssAutomaticRewards107.ApplyReady(result.Value);
     var rewarded107=result.IsSuccess?SssTenV4Roster090.FindOwned(result.Value.Guild.Recruits,sssPreview090.HeroId):null;
     var signatureEquipped107=rewarded107!=null&&rewarded107.Equipment.Assignments.Any(x=>x.Item.DefinitionId==SssTenV4Roster090.Get(sssPreview090.HeroId).WeaponItemId);
     var message=sssPreview090.IsPrimaryRecruit
      ?"SSS hero recruited: "+sssPreview090.HeroName+". A0 • Reserve • no equipment auto-equipped."
      :sssPreview090.IsAscensionCredit
       ?"Ascension received for "+sssPreview090.HeroName+". A"+(rewarded107?.Progression.AscensionLevel??0)+"/10. "+
        (result.IsSuccess&&SssTenV4Inventory090.AscensionCreditCount(result.Value,sssPreview090.HeroId)>0?"Unspent credits apply automatically after battle, up to A10.":"Rank applied automatically; Arts and mastery preserved.")
       :(signatureEquipped107?"Signature weapon equipped on ":"Signature weapon received for ")+sssPreview090.HeroName+". "+
        (signatureEquipped107?"Omega power and ":"Choose Equip or Auto Equip in Inventory to use it. Once equipped, Omega power and ")+
        (SssBattleIntegration090.HasSignatureWeaponEffectHandler090(sssPreview090.HeroId)
         ?"its equipped-only battle effect is active."
         :"its bespoke effect adapter remains pending.");
     return ApplyCreator028(result,message);
    }
    var relicCodes=RelicCodeRegistry1000();var relicHash=CreatorGiveawayCommandService10000.HashCode10000(input);
    if(relicCodes.TryGetCodeByHash(relicHash,out var relicCode))
    {
     var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     if(state.RedeemedCodeIds.Contains(relicCode.CodeId))return M1CommandResult.Failure("That relic code was already claimed in this campaign.");
     if(!relicCodes.TryGetRewardBundle(relicCode.RewardBundleId,out var relicBundle))return M1CommandResult.Failure("The relic reward attached to this code is unavailable.");
     var relics=SpecialRelicRegistry001();
     if(!RelicCodeCommandService1000.TryResolveOutcome(_campaign,relicCode,relicBundle,relics,out var resolvedRelic,out var outcomeHash,out var outcomeError))return M1CommandResult.Failure(outcomeError);
     var owned=CountOwnedRelicDefinition1000(resolvedRelic.RelicId);var duplicateQuantity=RelicDuplicateQuantity1000(resolvedRelic.Rarity);
     var message=owned==1?"Duplicate relic converted: "+resolvedRelic.Name+" → "+duplicateQuantity+" Resonance Thread. Saved exactly once.":relics.IsP0(resolvedRelic.RelicId)?"Active P0 relic claimed: "+resolvedRelic.Name+". Stored in Guild inventory for manual Tool Relic equip; nothing was auto-equipped.":"Sealed relic claimed: "+resolvedRelic.Name+". Stored in Guild inventory for manual Tool Relic equip; its combat adapter is not active yet.";
     if(relicBundle.IsCache)message="Cache outcome committed: "+message;
     return ApplyCreator028(_relicCodeCommands1000.RedeemCode(_campaign,relicCodes,relics,input,_campaignCommands022,Registry022()),message);
    }
    var giveaway=CreatorGiveawayRegistry10000();var giveawayHash=CreatorGiveawayCommandService10000.HashCode10000(input);
    if(giveaway.TryGetCodeByHash(giveawayHash,out var giveawayCode))
    {
     var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     if(state.RedeemedCodeIds.Contains(giveawayCode.CodeId))return M1CommandResult.Failure("That bonus code was already claimed in this campaign.");
     if(!giveaway.TryGetRewardBundle(giveawayCode.RewardBundleId,out var bundle))return M1CommandResult.Failure("The reward attached to this code is unavailable.");
     var targetMode=CreatorGiveawayCommandService10000.RequiredTargetMode(bundle);
     if(!string.IsNullOrWhiteSpace(targetMode)&&string.IsNullOrWhiteSpace(targetId))return M1CommandResult.Failure(CreatorTargetPrompt10000(targetMode));
     if((giveawayCode.CreatorPowerFlag||bundle.CreatorPowerFlag)&&!creatorPowerConfirmed)return M1CommandResult.Failure("Read and accept the Creator Ω warning before claiming this reward.");
     return ApplyCreator028(_creatorGiveawayCommands10000.RedeemCode(_campaign,giveaway,input,targetId,creatorPowerConfirmed),"Bonus claimed: "+bundle.Label+". Saved exactly once.");
    }
    if(TryHeroMaster300CreatorRegistry087(out var heroMaster)&&
       heroMaster.Codes.TryResolveInput(input,out var heroCode,out var hero))
    {
     var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();
     if(state.RedeemedCodeIds.Contains(heroCode.CodeId)||state.ClaimedInvitationIds.Contains(heroCode.RewardId))
      return M1CommandResult.Failure("That SS hero code was already claimed in this campaign.");
     if(HeroMaster300CreatorRecruitProjection087.RosterContainsHero(_campaign?.Guild?.Recruits,hero))
      return M1CommandResult.Failure(hero.Name+" is already in this Guild roster. No duplicate was added.");
     var grant=MaterializeHeroMaster300Recruit087(hero);
     return ApplyCreator028(
      _creatorCommands028.RedeemCode(_campaign,heroMaster.Codes,input,grant),
      "SS hero recruited: "+hero.Name+". Sent to Reserve with no equipment auto-equipped. Saved exactly once.");
    }
    return RedeemLegacyCreatorCode028(input);
   }
   catch(Exception e){return M1CommandResult.Failure(e.Message);}
  }

  M1CommandResult RedeemLegacyCreatorCode028(string input){var reg=CreatorRegistry028();var hash=CreatorAccessCommandService028.HashNormalizedCode(input);if(string.IsNullOrWhiteSpace(hash))return M1CommandResult.Failure("Enter a bonus code first.");if(!reg.TryGetCodeByHash(hash,out var code)||!code.Active)return M1CommandResult.Failure("That bonus code was not recognized. Check the letters and numbers, then try again.");var state=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028??CreatorAccessState028.Default();if(state.RedeemedCodeIds.Contains(code.CodeId))return M1CommandResult.Failure("That bonus code was already claimed in this campaign.");CreatorRecruitGrant028 grant=null;if(StringComparer.OrdinalIgnoreCase.Equals(code.Category,"CHARACTER")){if(!reg.TryGetInvitation(code.RewardId,out var invite))return M1CommandResult.Failure("The adventurer invitation attached to this code is unavailable.");grant=MaterializeCreatorRecruit028(invite);}return ApplyCreator028(_creatorCommands028.RedeemCode(_campaign,reg,input,grant),"Bonus reward claimed and saved.");}

  int CountOwnedRelicDefinition1000(string definitionId){if(_campaign?.Guild==null)return 0;var count=_campaign.Guild.Inventory.Count(value=>StringComparer.Ordinal.Equals(value.DefinitionId,definitionId));foreach(var recruit in _campaign.Guild.Recruits)count+=recruit.Equipment.Assignments.Count(value=>StringComparer.Ordinal.Equals(value.Item.DefinitionId,definitionId));return count;}
  static int RelicDuplicateQuantity1000(string rarity){switch(rarity){case "Rare":return 1;case "Epic":return 2;case "Legendary":return 3;case "Mythic":return 4;default:return 0;}}

  Creator028.CreatorRewardPreview028 InvalidCreatorPreview028(string error)=>new Creator028.CreatorRewardPreview028{IsValid=false,Error=error??"That bonus code is unavailable.",TargetOptions=Array.Empty<Creator028.CreatorRewardTargetOption028>()};

  IReadOnlyList<Creator028.CreatorRewardTargetOption028> BuildCreatorTargetOptions10000(string mode)
  {
   if(string.IsNullOrWhiteSpace(mode)||_campaign?.Guild==null)return Array.Empty<Creator028.CreatorRewardTargetOption028>();
   var options=new List<Creator028.CreatorRewardTargetOption028>();var recruits=EligibleCreatorRecruits10000();
   if(StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER"))
   {
    foreach(var recruit in recruits)options.Add(new Creator028.CreatorRewardTargetOption028{TargetId=recruit.RecruitId,DisplayName=recruit.DisplayName,Detail="LEVEL "+recruit.Progression.Level+" • "+FriendlyCreatorToken10000(recruit.ClassTendencyId)});
   }
   else if(StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_UNION"))
   {
    foreach(var union in _campaign.Guild.Unions.Where(value=>value.Kind==SecondDimension.Gameplay.State.UnionKind.Normal).OrderBy(value=>value.DisplayName,StringComparer.Ordinal))options.Add(new Creator028.CreatorRewardTargetOption028{TargetId=union.UnionId,DisplayName=union.DisplayName,Detail=union.MemberRecruitIds.Count+" MEMBERS • COHESION "+(union.CohesionBasisPoints/100)+"%"});
   }
   else if(StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_RELATIONSHIP"))
   {
    var seen=new HashSet<string>(StringComparer.Ordinal);var byId=recruits.ToDictionary(value=>value.RecruitId,value=>value,StringComparer.Ordinal);
    foreach(var union in _campaign.Guild.Unions.Where(value=>value.Kind==SecondDimension.Gameplay.State.UnionKind.Normal).OrderBy(value=>value.UnionId,StringComparer.Ordinal))
     for(var first=0;first<union.MemberRecruitIds.Count;first++)for(var second=first+1;second<union.MemberRecruitIds.Count;second++)
     {
      var a=union.MemberRecruitIds[first];var b=union.MemberRecruitIds[second];if(!byId.ContainsKey(a)||!byId.ContainsKey(b))continue;
      var key=StringComparer.Ordinal.Compare(a,b)<=0?a+CreatorGiveawayCommandService10000.TargetSeparator+b:b+CreatorGiveawayCommandService10000.TargetSeparator+a;if(!seen.Add(key))continue;
      options.Add(new Creator028.CreatorRewardTargetOption028{TargetId=key,DisplayName=byId[a].DisplayName+" + "+byId[b].DisplayName,Detail=union.DisplayName+" • relationship growth"});
     }
   }
   else if(StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_ART_TREE")||StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER_AND_FAMILY"))
   {
    foreach(var recruit in recruits)AddCreatorArtTargets10000(options,recruit,mode);
   }
   return Creator028.CreatorRewardTargetPaging10000.PreserveAll(options);
  }

  IReadOnlyList<SecondDimension.Gameplay.State.RecruitState> EligibleCreatorRecruits10000()
  {
   var priority=new Dictionary<string,int>(StringComparer.Ordinal);var rank=0;
   foreach(var union in _campaign.Guild.Unions.Where(value=>value.Kind==SecondDimension.Gameplay.State.UnionKind.Normal).OrderBy(value=>value.UnionId,StringComparer.Ordinal))foreach(var id in union.MemberRecruitIds)if(!priority.ContainsKey(id))priority[id]=rank++;
   return _campaign.Guild.Recruits.Where(value=>value.AuthorityKind==SecondDimension.Gameplay.State.RecruitAuthorityKind.Normal)
    .OrderBy(value=>priority.TryGetValue(value.RecruitId,out var order)?order:int.MaxValue).ThenBy(value=>value.DisplayName,StringComparer.Ordinal).ToList().AsReadOnly();
  }

  void AddCreatorArtTargets10000(List<Creator028.CreatorRewardTargetOption028> options,SecondDimension.Gameplay.State.RecruitState recruit,string mode)
  {
   var learned=recruit.Progression.LearnedArtIds??Array.Empty<string>();if(learned.Count==0)return;
   if(_deepProgressionCatalog070==null)
   {
    var art=learned[0];var target=StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER_AND_FAMILY")?recruit.RecruitId+CreatorGiveawayCommandService10000.TargetSeparator+"WEAPON_FAMILY"+CreatorGiveawayCommandService10000.TargetSeparator+art:recruit.RecruitId+CreatorGiveawayCommandService10000.TargetSeparator+art;options.Add(new Creator028.CreatorRewardTargetOption028{TargetId=target,DisplayName=recruit.DisplayName,Detail=FriendlyCreatorToken10000(art)});return;
   }
   try
   {
    var authorityId=!string.IsNullOrWhiteSpace(recruit.SignatureId)?recruit.SignatureId:recruit.AuthoredStableRecruitId;var plan=_deepProgressionCatalog070.RecruitPlan(authorityId);
    var byTree=new Dictionary<string,string>(StringComparer.Ordinal);
    foreach(var artId in learned){if(_deepProgressionCatalog070.TryNode(artId,out var node)&&plan.ContainsTree(node.TreeId)&&!byTree.ContainsKey(node.TreeId))byTree[node.TreeId]=artId;}
    foreach(var pair in byTree.OrderBy(value=>_deepProgressionCatalog070.Tree(value.Key).DisplayName,StringComparer.Ordinal))
    {
     var tree=_deepProgressionCatalog070.Tree(pair.Key);
     if(StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER_AND_FAMILY")&&
        (tree.Category??string.Empty).IndexOf("WEAPON",StringComparison.OrdinalIgnoreCase)<0&&
        !StringComparer.Ordinal.Equals(tree.TreeId,plan.Slot(RecruitTreeSlotKind070.Weapon).TreeId))continue;
     var target=StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER_AND_FAMILY")
      ?recruit.RecruitId+CreatorGiveawayCommandService10000.TargetSeparator+tree.WeaponFamilyId+CreatorGiveawayCommandService10000.TargetSeparator+pair.Value
      :recruit.RecruitId+CreatorGiveawayCommandService10000.TargetSeparator+pair.Value;
     options.Add(new Creator028.CreatorRewardTargetOption028{TargetId=target,DisplayName=recruit.DisplayName,Detail=tree.DisplayName+(StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER_AND_FAMILY")?" • weapon proficiency":" • Art mastery")});
    }
   }
   catch
   {
    var art=learned[0];var target=StringComparer.Ordinal.Equals(mode,"PLAYER_SELECT_CHARACTER_AND_FAMILY")?recruit.RecruitId+CreatorGiveawayCommandService10000.TargetSeparator+"WEAPON_FAMILY"+CreatorGiveawayCommandService10000.TargetSeparator+art:recruit.RecruitId+CreatorGiveawayCommandService10000.TargetSeparator+art;options.Add(new Creator028.CreatorRewardTargetOption028{TargetId=target,DisplayName=recruit.DisplayName,Detail=FriendlyCreatorToken10000(art)});
   }
  }

  static string CreatorTargetPrompt10000(string mode)
  {
   switch(mode){case "PLAYER_SELECT_CHARACTER":return "Choose the adventurer who receives this Personal XP.";case "PLAYER_SELECT_ART_TREE":return "Choose an adventurer and one learned Art path.";case "PLAYER_SELECT_CHARACTER_AND_FAMILY":return "Choose an adventurer and their weapon path.";case "PLAYER_SELECT_UNION":return "Choose the Union that receives this discipline training.";case "PLAYER_SELECT_RELATIONSHIP":return "Choose two active companions whose bond will grow.";default:return string.Empty;}
  }

  static string CreatorRewardSummary10000(CreatorGiveawayRewardBundle10000 bundle)
  {
   if(bundle==null)return string.Empty;
   switch(bundle.GrantType){case "XP_VOUCHER":return bundle.AmountOrQuantity.ToString("N0")+" "+FriendlyCreatorToken10000(bundle.PrimaryId)+" • applied to your chosen target";case "INVENTORY_ITEM":return bundle.AmountOrQuantity+" × "+bundle.Label+" • stored in the Expedition Kit";case "GROWTH_BOOST_ITEM":return bundle.Label+" • stored for manual use";case "EQUIPMENT_INSTANCE":return bundle.Label+" • deterministic Guild inventory item • never auto-equipped";case "MULTI_GRANT":var payload=JObject.Parse(bundle.PayloadJson);var count=(payload["grants"] as JArray)?.Count??0;return count+"-part cache • all rewards commit together or none do";default:return bundle.Label;}
  }

  static string FriendlyCreatorToken10000(string value)
  {
   if(string.IsNullOrWhiteSpace(value))return "Growth";var text=value.Replace("WF01_",string.Empty).Replace("WF02_",string.Empty).Replace("WF03_",string.Empty).Replace("WF04_",string.Empty).Replace("WF05_",string.Empty).Replace("WF06_",string.Empty).Replace("WF07_",string.Empty).Replace("WF08_",string.Empty).Replace("WF09_",string.Empty).Replace("WF10_",string.Empty).Replace("WF11_",string.Empty).Replace("WF12_",string.Empty).Replace('_',' ').ToLowerInvariant();return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);
  }
  static string HeroMaster300RewardSummary087(HeroMaster300Hero087 hero,bool rosterDuplicate)
  {
   if(hero==null)return "SS hero data is unavailable.";
   var status=rosterDuplicate?"Already present in this Guild roster.":"Joins Reserve permanently • no equipment is granted or auto-equipped.";
   return hero.Race+" • "+hero.Role+" • "+hero.Weapon+
    "\nHP "+hero.Hp+"  AP "+hero.Ap+"  STR "+hero.Strength+"  DEF "+hero.Defense+
    "  AGI "+hero.Agility+"  MAG "+hero.Magic+"  RES "+hero.Resistance+
    "\nART PATHS  "+FriendlyCreatorToken10000(hero.ArtTree1)+" / "+FriendlyCreatorToken10000(hero.ArtTree2)+
    " • FINAL ART  "+hero.FinalArtName+" • "+status;
  }
  public M1CommandResult EnterCreatorRoom028()=>ApplyCreator028(_creatorCommands028.EnterCurrentCreatorRoom(_campaign,CreatorRegistry028()),"Creator Room entered without advancing the operation.");
  public M1CommandResult ResolveCreatorRoom028(){try{var reg=CreatorRegistry028();var s=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.CreatorAccess028;CreatorRecruitGrant028 grant=null;if(s?.ActiveRoomVisit!=null){var room=reg.AllRooms.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.RoomId,s.ActiveRoomVisit.RoomId));if(room!=null&&StringComparer.OrdinalIgnoreCase.Equals(room.RewardKind,"CHARACTER")&&reg.TryGetInvitation(room.RewardId,out var invite))grant=MaterializeCreatorRecruit028(invite);}return ApplyCreator028(_creatorCommands028.ResolveActiveCreatorRoom(_campaign,reg,grant),"Creator Room reward applied exactly once.");}catch(Exception e){return M1CommandResult.Failure(e.Message);}}
  CreatorRecruitGrant028 MaterializeHeroMaster300Recruit087(HeroMaster300Hero087 hero)
  {
   var baseGrant=HeroMaster300CreatorRecruitProjection087.FromHero(hero);
   var catalog=RecruitAutoGenerationCatalog010.LoadFromContentRoot(ResolveRecruitmentContentRoot());
   var initialized=new RecruitAutoGenerationSigningService010(_commands,new RecruitAutoGenerator010(catalog)).InitializeRecruit(baseGrant.Recruit);
   return new CreatorRecruitGrant028(initialized,Array.Empty<SecondDimension.Gameplay.State.EquipmentItemState>());
  }
  CreatorRecruitGrant028 MaterializeCreatorRecruit028(CreatorInvitationRule028 invite){if(_recruitmentContent==null)throw new InvalidOperationException("Recruitment authority unavailable.");OpeningRecruitRecord record;if(StringComparer.Ordinal.Equals(invite.Kind,"SIGNATURE"))record=new SignatureRecruitMaterializer(_recruitmentContent).Materialize(invite.GenerationSeed.Length>0?invite.GenerationSeed:invite.InvitationId,invite.SignatureId,invite.SourceChannel);else record=new OpeningRecruitGenerator(_recruitmentContent).Generate(new ProceduralRecruitRequest{CampaignSeed=invite.GenerationSeed,GuildDay=1,RefreshIndex=0,SlotIndex=0,SourceChannel=invite.SourceChannel,UnlockedRaces=new[]{"HUMAN","ORC","GOBLIN","DOG_TRIBE","DARK_ELF","DEMON_HERITAGE"},ExtraSalt=invite.InvitationId});var baseGrant=CreatorRecruitProjection028.FromRecord(record,invite.WorldId);var catalog=RecruitAutoGenerationCatalog010.LoadFromContentRoot(ResolveRecruitmentContentRoot());var initialized=new RecruitAutoGenerationSigningService010(_commands,new RecruitAutoGenerator010(catalog)).InitializeRecruit(baseGrant.Recruit);return new CreatorRecruitGrant028(initialized,baseGrant.InventoryItems);}
  M1CommandResult ApplyCreator028(SecondDimension.Core.Result<SecondDimension.Gameplay.State.CampaignState> result,string ok)=>ApplyAndPersist(result,true,ok);
 }
}
