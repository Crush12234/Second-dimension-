using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Creator028
{
    public enum CreatorRewardKind028 { Character, Resource, Weapon, Content, Chronicle, RoomKey }
    public enum CreatorRoomVisitStatus028 { Entered, RewardCommitted, Complete }

    [Serializable]
    public sealed class CreatorRoomVisitState028
    {
        [JsonConstructor]
        public CreatorRoomVisitState028(string visitId,string roomId,string operationId,string boardDefinitionId,string entryNodeId,string returnNodeId,CreatorRoomVisitStatus028 status,string pendingRewardReceiptId)
        { VisitId=Req(visitId,nameof(visitId));RoomId=Req(roomId,nameof(roomId));OperationId=Req(operationId,nameof(operationId));BoardDefinitionId=Req(boardDefinitionId,nameof(boardDefinitionId));EntryNodeId=Req(entryNodeId,nameof(entryNodeId));ReturnNodeId=Req(returnNodeId,nameof(returnNodeId));Status=status;PendingRewardReceiptId=pendingRewardReceiptId??string.Empty; }
        public string VisitId{get;} public string RoomId{get;} public string OperationId{get;} public string BoardDefinitionId{get;} public string EntryNodeId{get;} public string ReturnNodeId{get;} public CreatorRoomVisitStatus028 Status{get;} public string PendingRewardReceiptId{get;}
        public CreatorRoomVisitState028 With(CreatorRoomVisitStatus028? status=null,string pendingRewardReceiptId=null)=>new CreatorRoomVisitState028(VisitId,RoomId,OperationId,BoardDefinitionId,EntryNodeId,ReturnNodeId,status??Status,pendingRewardReceiptId??PendingRewardReceiptId);
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
    }

    [Serializable]
    public sealed class CreatorRewardReceipt028
    {
        [JsonConstructor]
        public CreatorRewardReceipt028(string receiptId,string sourceId,string rewardKind,string rewardId,string authoritativeHash,int appliedVersion)
        { ReceiptId=Req(receiptId,nameof(receiptId));SourceId=Req(sourceId,nameof(sourceId));RewardKind=Req(rewardKind,nameof(rewardKind));RewardId=Req(rewardId,nameof(rewardId));AuthoritativeHash=Req(authoritativeHash,nameof(authoritativeHash));if(appliedVersion<0)throw new ArgumentOutOfRangeException(nameof(appliedVersion));AppliedVersion=appliedVersion; }
        public string ReceiptId{get;} public string SourceId{get;} public string RewardKind{get;} public string RewardId{get;} public string AuthoritativeHash{get;} public int AppliedVersion{get;}
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
    }

    [Serializable]
    public sealed class CreatorGrowthAllocation10000
    {
        [JsonConstructor]
        public CreatorGrowthAllocation10000(
            string allocationId,
            string receiptId,
            string xpType,
            string targetId,
            long amount)
        {
            AllocationId=Req(allocationId,nameof(allocationId));
            ReceiptId=Req(receiptId,nameof(receiptId));
            XpType=Req(xpType,nameof(xpType));
            TargetId=Req(targetId,nameof(targetId));
            if(amount<=0)throw new ArgumentOutOfRangeException(nameof(amount));
            Amount=amount;
        }
        public string AllocationId{get;} public string ReceiptId{get;} public string XpType{get;}
        public string TargetId{get;} public long Amount{get;}
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
    }

    [Serializable]
    public sealed class CreatorRelicReceipt1000
    {
        [JsonConstructor]
        public CreatorRelicReceipt1000(
            string codeId,
            string bundleId,
            string resolvedRelicId,
            bool duplicateConverted,
            string materialId,
            int materialQuantity,
            string outcomeHash,
            string receiptId)
        {
            CodeId=Req(codeId,nameof(codeId));
            BundleId=Req(bundleId,nameof(bundleId));
            ResolvedRelicId=Req(resolvedRelicId,nameof(resolvedRelicId));
            DuplicateConverted=duplicateConverted;
            MaterialId=materialId??string.Empty;
            if(materialQuantity<0)throw new ArgumentOutOfRangeException(nameof(materialQuantity));
            MaterialQuantity=materialQuantity;
            OutcomeHash=Req(outcomeHash,nameof(outcomeHash));
            ReceiptId=Req(receiptId,nameof(receiptId));
            if(duplicateConverted!=(MaterialId.Length>0&&MaterialQuantity>0))
                throw new ArgumentException("Duplicate relic receipt material proof is inconsistent.");
        }
        public string CodeId{get;} public string BundleId{get;} public string ResolvedRelicId{get;}
        public bool DuplicateConverted{get;} public string MaterialId{get;} public int MaterialQuantity{get;}
        public string OutcomeHash{get;} public string ReceiptId{get;}
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
    }

    [Serializable]
    public sealed class CreatorAccessState028
    {
        public const string ContentVersion="FINAL_CREATOR_CODES_ROOMS_028_1.0";
        readonly bool _serializeGrowthAllocations10000;
        readonly bool _serializeRelicReceipts1000;
        [JsonConstructor]
        public CreatorAccessState028(string contentVersion,IReadOnlyList<string> redeemedCodeIds,IReadOnlyList<string> claimedInvitationIds,IReadOnlyList<string> unlockedContentIds,IReadOnlyList<string> unlockedRoomKeyIds,IReadOnlyList<string> discoveredRoomIds,IReadOnlyList<string> completedRoomIds,IReadOnlyList<string> appliedReceiptIds,int creatorTokens,CreatorRoomVisitState028 activeRoomVisit,string lastCheckpointId,IReadOnlyList<CreatorGrowthAllocation10000> growthAllocations10000=null,IReadOnlyList<CreatorRelicReceipt1000> relicReceipts1000=null)
        { ContentAuthorityVersion=string.IsNullOrWhiteSpace(contentVersion)?ContentVersion:contentVersion;RedeemedCodeIds=Copy(redeemedCodeIds);ClaimedInvitationIds=Copy(claimedInvitationIds);UnlockedContentIds=Copy(unlockedContentIds);UnlockedRoomKeyIds=Copy(unlockedRoomKeyIds);DiscoveredRoomIds=Copy(discoveredRoomIds);CompletedRoomIds=Copy(completedRoomIds);AppliedReceiptIds=Copy(appliedReceiptIds);if(creatorTokens<0)throw new ArgumentOutOfRangeException(nameof(creatorTokens));CreatorTokens=creatorTokens;ActiveRoomVisit=activeRoomVisit;LastCheckpointId=lastCheckpointId??string.Empty;_serializeGrowthAllocations10000=growthAllocations10000!=null;_serializeRelicReceipts1000=relicReceipts1000!=null;GrowthAllocations10000=CopyAllocations(growthAllocations10000);RelicReceipts1000=CopyRelicReceipts(relicReceipts1000); }
        public string ContentAuthorityVersion{get;} public IReadOnlyList<string> RedeemedCodeIds{get;} public IReadOnlyList<string> ClaimedInvitationIds{get;} public IReadOnlyList<string> UnlockedContentIds{get;} public IReadOnlyList<string> UnlockedRoomKeyIds{get;} public IReadOnlyList<string> DiscoveredRoomIds{get;} public IReadOnlyList<string> CompletedRoomIds{get;} public IReadOnlyList<string> AppliedReceiptIds{get;} public int CreatorTokens{get;} public CreatorRoomVisitState028 ActiveRoomVisit{get;} public string LastCheckpointId{get;} public IReadOnlyList<CreatorGrowthAllocation10000> GrowthAllocations10000{get;} public IReadOnlyList<CreatorRelicReceipt1000> RelicReceipts1000{get;}
        // Save-format 11 predates these two optional ledgers. Preserve whether an
        // empty ledger was present so the canonical re-hash accepts both older V11
        // saves (property absent) and newer V11 saves (empty property present).
        public bool ShouldSerializeGrowthAllocations10000()=>_serializeGrowthAllocations10000||GrowthAllocations10000.Count>0;
        public bool ShouldSerializeRelicReceipts1000()=>_serializeRelicReceipts1000||RelicReceipts1000.Count>0;
        public CreatorAccessState028 With(IReadOnlyList<string> redeemedCodeIds=null,IReadOnlyList<string> claimedInvitationIds=null,IReadOnlyList<string> unlockedContentIds=null,IReadOnlyList<string> unlockedRoomKeyIds=null,IReadOnlyList<string> discoveredRoomIds=null,IReadOnlyList<string> completedRoomIds=null,IReadOnlyList<string> appliedReceiptIds=null,int? creatorTokens=null,CreatorRoomVisitState028 activeRoomVisit=null,bool replaceActiveRoomVisit=false,string lastCheckpointId=null,IReadOnlyList<CreatorGrowthAllocation10000> growthAllocations10000=null,IReadOnlyList<CreatorRelicReceipt1000> relicReceipts1000=null)=>new CreatorAccessState028(ContentAuthorityVersion,redeemedCodeIds??RedeemedCodeIds,claimedInvitationIds??ClaimedInvitationIds,unlockedContentIds??UnlockedContentIds,unlockedRoomKeyIds??UnlockedRoomKeyIds,discoveredRoomIds??DiscoveredRoomIds,completedRoomIds??CompletedRoomIds,appliedReceiptIds??AppliedReceiptIds,creatorTokens??CreatorTokens,replaceActiveRoomVisit?activeRoomVisit:ActiveRoomVisit,lastCheckpointId??LastCheckpointId,growthAllocations10000??GrowthAllocations10000,relicReceipts1000??RelicReceipts1000);
        public static CreatorAccessState028 Default()=>new CreatorAccessState028(ContentVersion,Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),0,null,"creator028_initialized");
        static IReadOnlyList<string> Copy(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
        static IReadOnlyList<CreatorGrowthAllocation10000> CopyAllocations(IReadOnlyList<CreatorGrowthAllocation10000> values){var r=new List<CreatorGrowthAllocation10000>();if(values!=null)for(var i=0;i<values.Count;i++){var value=values[i]??throw new ArgumentException("Growth allocation cannot be null.",nameof(values));if(r.Exists(x=>StringComparer.Ordinal.Equals(x.AllocationId,value.AllocationId)))throw new ArgumentException("Growth allocation IDs must be unique.",nameof(values));r.Add(value);}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.AllocationId,b.AllocationId));return r.AsReadOnly();}
        static IReadOnlyList<CreatorRelicReceipt1000> CopyRelicReceipts(IReadOnlyList<CreatorRelicReceipt1000> values){var r=new List<CreatorRelicReceipt1000>();if(values!=null)for(var i=0;i<values.Count;i++){var value=values[i]??throw new ArgumentException("Relic receipt cannot be null.",nameof(values));if(r.Exists(x=>StringComparer.Ordinal.Equals(x.ReceiptId,value.ReceiptId)||StringComparer.Ordinal.Equals(x.CodeId,value.CodeId)))throw new ArgumentException("Relic receipt IDs and code IDs must be unique.",nameof(values));r.Add(value);}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.ReceiptId,b.ReceiptId));return r.AsReadOnly();}
    }

    public sealed class CreatorRecruitGrant028
    {
        public CreatorRecruitGrant028(RecruitState recruit,IReadOnlyList<EquipmentItemState> inventoryItems){Recruit=recruit??throw new ArgumentNullException(nameof(recruit));InventoryItems=inventoryItems??Array.Empty<EquipmentItemState>();}
        public RecruitState Recruit{get;} public IReadOnlyList<EquipmentItemState> InventoryItems{get;}
    }
}
