using System;
using System.Collections.Generic;
namespace SecondDimension.Gameplay.Creator028
{
 public class CreatorCodeRule028 { public string CodeId; public string CodeHash; public string Category; public string RewardId; public string DisplayLabel; public bool ExactOncePerCampaign; public bool Active; }
 public class CreatorResourceRule028 { public string ResourceId; public long TreasuryXp; public long HallXp; public int CreatorTokens; public IReadOnlyList<string> MaterialIds=Array.Empty<string>(); }
 public class CreatorInvitationRule028 { public string InvitationId; public string Kind; public string SignatureId; public string GenerationSeed; public string SourceChannel; public string WorldId; public string DisplayName; }
 public class CreatorWeaponRule028 { public string WeaponId; public string FamilyId; public string DisplayName; public string Description; public IReadOnlyList<string> ValidSlotIds=Array.Empty<string>(); public IReadOnlyList<string> EquipmentTags=Array.Empty<string>(); public string QualityId; public int ConditionBasisPoints; public bool ManualEquipOnly; }
 public class CreatorContentRule028 { public string ContentId; public string ContentType; public string DisplayName; public string Description; }
 public class CreatorRoomRule028 { public string RoomId; public string BoardDefinitionId; public string BoardId; public string WorldId; public string SourceRuntime; public string OperationKind; public string EntryNodeId; public string ReturnNodeId; public string RoomType; public string DisplayName; public string Description; public string RewardKind; public string RewardId; public string BonusKeyId; public bool OneVisitPerOperation; public bool CampaignRewardExactOnce; public bool DoesNotAdvanceOperationDay; public string ArtResourcePath; }
 public interface ICreatorContentCatalog028
 {
  bool TryGetCodeByHash(string hash,out CreatorCodeRule028 rule); bool TryGetResource(string id,out CreatorResourceRule028 rule); bool TryGetInvitation(string id,out CreatorInvitationRule028 rule); bool TryGetWeapon(string id,out CreatorWeaponRule028 rule); bool TryGetContent(string id,out CreatorContentRule028 rule); bool TryGetRoomForBoard(string boardDefinitionId,string boardId,out CreatorRoomRule028 rule); int CodeCount{get;} int RoomCount{get;} IReadOnlyList<CreatorRoomRule028> AllRooms{get;}
 }
}
