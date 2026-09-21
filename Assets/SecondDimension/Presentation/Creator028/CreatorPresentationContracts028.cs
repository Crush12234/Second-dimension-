using System;
using System.Collections.Generic;
namespace SecondDimension.Presentation.Creator028
{
 public sealed class CreatorRewardTargetOption028 { public string TargetId; public string DisplayName; public string Detail; }
 public static class CreatorRewardTargetPaging10000
 {
  public const int PageSize = 8;

  public static IReadOnlyList<CreatorRewardTargetOption028> PreserveAll(
   IReadOnlyList<CreatorRewardTargetOption028> options)
  {
   if(options==null||options.Count==0)return Array.Empty<CreatorRewardTargetOption028>();
   var result=new List<CreatorRewardTargetOption028>(options.Count);
   for(var index=0;index<options.Count;index++)
    if(options[index]!=null)result.Add(options[index]);
   return result.AsReadOnly();
  }

  public static int PageCount(int optionCount) =>
   Math.Max(1,(Math.Max(0,optionCount)+PageSize-1)/PageSize);
 }
 public sealed class CreatorRewardPreview028
 {
  public bool IsValid; public bool AlreadyRedeemed; public string Error; public string CodeId;
  public string RewardBundleId; public string Label; public string Category; public string Rarity;
  public string RewardSummary; public string TargetMode; public string TargetPrompt;
  public IReadOnlyList<CreatorRewardTargetOption028> TargetOptions;
  public bool RequiresCreatorPowerConfirmation; public string CreatorPowerWarning;
 }
 public sealed class CreatorRoomView028 { public string RoomId; public string DisplayName; public string RoomType; public string Description; public string RewardKind; public string RewardId; public string EntryNodeId; public string ArtResourcePath; public string RequiredRoomKeyId; public bool KeyUnlocked; }
 public sealed class CreatorAccessPresentationState028 { public bool IsAvailable; public string Error; public int TotalCodes; public int RedeemedCodes; public int CreatorTokens; public int UnlockedContent; public int RoomKeys; public int CompletedRooms; public CreatorRoomView028 CurrentRoom; public string ActiveRoomVisitId; public string LastCheckpointId; }
 public interface ICreatorAccessPresentationCoordinator028 { CreatorAccessPresentationState028 CreatorAccess028{get;} CreatorRewardPreview028 PreviewCreatorCode028(string input); M1CommandResult RedeemCreatorCode028(string input); M1CommandResult RedeemCreatorCode028(string input,string targetId,bool creatorPowerConfirmed); M1CommandResult EnterCreatorRoom028(); M1CommandResult ResolveCreatorRoom028(); }
}
