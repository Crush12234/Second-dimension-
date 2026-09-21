using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Gameplay.PeopleBonds026;

namespace SecondDimension.Gameplay.Campaign020
{
    public enum CampaignPlayableOperationStatus020 { Active, AwaitingBattle, ReadyToFinalize, Complete }

    [Serializable]
    public sealed class CampaignStepReceipt020
    {
        [JsonConstructor]
        public CampaignStepReceipt020(string receiptId,string operationId,string stepId,string outcome,string authoritativeHash,string existingEquipmentRewardReceiptId,int guildXp,int hallXp,IReadOnlyList<WorldMaterialAmount020> materials,int appliedVersion,string externalAuthorityProofId=null,string externalAuthorityProofHash=null)
        {
            ReceiptId=Require(receiptId,nameof(receiptId));OperationId=Require(operationId,nameof(operationId));StepId=Require(stepId,nameof(stepId));Outcome=Require(outcome,nameof(outcome));AuthoritativeHash=Require(authoritativeHash,nameof(authoritativeHash));ExistingEquipmentRewardReceiptId=existingEquipmentRewardReceiptId??string.Empty;if(guildXp<0||hallXp<0||appliedVersion<0)throw new ArgumentOutOfRangeException(nameof(guildXp));GuildXp=guildXp;HallXp=hallXp;Materials=CopyMaterials(materials);AppliedVersion=appliedVersion;ExternalAuthorityProofId=externalAuthorityProofId??string.Empty;ExternalAuthorityProofHash=externalAuthorityProofHash??string.Empty;
        }
        public string ReceiptId{get;} public string OperationId{get;} public string StepId{get;} public string Outcome{get;} public string AuthoritativeHash{get;} public string ExistingEquipmentRewardReceiptId{get;} public int GuildXp{get;} public int HallXp{get;} public IReadOnlyList<WorldMaterialAmount020> Materials{get;} public int AppliedVersion{get;} public string ExternalAuthorityProofId{get;} public string ExternalAuthorityProofHash{get;}
        static string Require(string value,string param)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",param):value;
        static IReadOnlyList<WorldMaterialAmount020> CopyMaterials(IReadOnlyList<WorldMaterialAmount020> values){var r=new List<WorldMaterialAmount020>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("Material cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId));return r.AsReadOnly();}
    }

    [Serializable]
    public sealed class CampaignPlayableOperationState020
    {
        [JsonConstructor]
        public CampaignPlayableOperationState020(string operationId,string blueprintId,string chapterId,string worldId,string canonicalSeedIdentity,int currentStepIndex,CampaignPlayableOperationStatus020 status,IReadOnlyList<string> completedStepIds,IReadOnlyList<string> appliedReceiptIds,CampaignStepReceipt020 pendingReceipt,string existingBattleRewardReceiptId,string lastCheckpointId,IReadOnlyList<CampaignStepReceipt020> appliedReceipts=null)
        {
            OperationId=Require(operationId,nameof(operationId));BlueprintId=Require(blueprintId,nameof(blueprintId));ChapterId=Require(chapterId,nameof(chapterId));WorldId=Require(worldId,nameof(worldId));CanonicalSeedIdentity=Require(canonicalSeedIdentity,nameof(canonicalSeedIdentity));if(currentStepIndex<0)throw new ArgumentOutOfRangeException(nameof(currentStepIndex));CurrentStepIndex=currentStepIndex;Status=status;CompletedStepIds=CopyUnique(completedStepIds);AppliedReceiptIds=CopyUnique(appliedReceiptIds);PendingReceipt=pendingReceipt;ExistingBattleRewardReceiptId=existingBattleRewardReceiptId??string.Empty;LastCheckpointId=lastCheckpointId??string.Empty;AppliedReceipts=CopyReceipts(appliedReceipts);
        }
        public string OperationId{get;} public string BlueprintId{get;} public string ChapterId{get;} public string WorldId{get;} public string CanonicalSeedIdentity{get;} public int CurrentStepIndex{get;} public CampaignPlayableOperationStatus020 Status{get;} public IReadOnlyList<string> CompletedStepIds{get;} public IReadOnlyList<string> AppliedReceiptIds{get;} public CampaignStepReceipt020 PendingReceipt{get;} public string ExistingBattleRewardReceiptId{get;} public string LastCheckpointId{get;} public IReadOnlyList<CampaignStepReceipt020> AppliedReceipts{get;}
        public CampaignPlayableOperationState020 With(int? currentStepIndex=null,CampaignPlayableOperationStatus020? status=null,IReadOnlyList<string> completedStepIds=null,IReadOnlyList<string> appliedReceiptIds=null,CampaignStepReceipt020 pendingReceipt=null,bool replacePendingReceipt=false,string existingBattleRewardReceiptId=null,string lastCheckpointId=null,IReadOnlyList<CampaignStepReceipt020> appliedReceipts=null)=>new CampaignPlayableOperationState020(OperationId,BlueprintId,ChapterId,WorldId,CanonicalSeedIdentity,currentStepIndex??CurrentStepIndex,status??Status,completedStepIds??CompletedStepIds,appliedReceiptIds??AppliedReceiptIds,replacePendingReceipt?pendingReceipt:PendingReceipt,existingBattleRewardReceiptId??ExistingBattleRewardReceiptId,lastCheckpointId??LastCheckpointId,appliedReceipts??AppliedReceipts);
        static string Require(string value,string param)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",param):value;
        static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
        static IReadOnlyList<CampaignStepReceipt020> CopyReceipts(IReadOnlyList<CampaignStepReceipt020> values){var r=new List<CampaignStepReceipt020>();if(values!=null)for(var i=0;i<values.Count;i++){var value=values[i];if(value==null||r.Exists(existing=>StringComparer.Ordinal.Equals(existing.ReceiptId,value.ReceiptId)))continue;r.Add(value);}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.StepId,b.StepId));return r.AsReadOnly();}
    }

    [Serializable]
    public sealed class CampaignPlayableBeginGrant020
    {
        [JsonConstructor]
        public CampaignPlayableBeginGrant020(string operationId,string blueprintId,
            string chapterId,string worldId,string canonicalSeedIdentity,
            int campaignProgressAtBegin,string authorityHash)
        {
            OperationId=Require(operationId,nameof(operationId));
            BlueprintId=Require(blueprintId,nameof(blueprintId));
            ChapterId=Require(chapterId,nameof(chapterId));
            WorldId=Require(worldId,nameof(worldId));
            CanonicalSeedIdentity=Require(canonicalSeedIdentity,
                nameof(canonicalSeedIdentity));
            if(campaignProgressAtBegin<0)throw new ArgumentOutOfRangeException(
                nameof(campaignProgressAtBegin));
            CampaignProgressAtBegin=campaignProgressAtBegin;
            AuthorityHash=Require(authorityHash,nameof(authorityHash));
        }
        public string OperationId{get;} public string BlueprintId{get;}
        public string ChapterId{get;} public string WorldId{get;}
        public string CanonicalSeedIdentity{get;}
        public int CampaignProgressAtBegin{get;} public string AuthorityHash{get;}
        static string Require(string value,string param)=>
            string.IsNullOrWhiteSpace(value)?throw new ArgumentException(
                "Stable ID is required.",param):value;
    }

    [Serializable]
    public sealed class WorldMaterialAmount020
    {
        [JsonConstructor] public WorldMaterialAmount020(string materialId,int amount){MaterialId=string.IsNullOrWhiteSpace(materialId)?throw new ArgumentException("Material ID required.",nameof(materialId)):materialId;if(amount<0)throw new ArgumentOutOfRangeException(nameof(amount));Amount=amount;}
        public string MaterialId{get;} public int Amount{get;} public WorldMaterialAmount020 WithAmount(int value)=>new WorldMaterialAmount020(MaterialId,value);
    }

    [Serializable]
    public sealed class WorldGateProgress020
    {
        [JsonConstructor] public WorldGateProgress020(string worldId,bool unlocked,int visitCount,string standingTier,int localTimeCounter){WorldId=string.IsNullOrWhiteSpace(worldId)?throw new ArgumentException("World ID required.",nameof(worldId)):worldId;if(visitCount<0||localTimeCounter<0)throw new ArgumentOutOfRangeException(nameof(visitCount));Unlocked=unlocked;VisitCount=visitCount;StandingTier=standingTier??"RECOVERABLE_FRACTURE";LocalTimeCounter=localTimeCounter;}
        public string WorldId{get;} public bool Unlocked{get;} public int VisitCount{get;} public string StandingTier{get;} public int LocalTimeCounter{get;}
        public WorldGateProgress020 With(bool? unlocked=null,int? visitCount=null,string standingTier=null,int? localTimeCounter=null)=>new WorldGateProgress020(WorldId,unlocked??Unlocked,visitCount??VisitCount,standingTier??StandingTier,localTimeCounter??LocalTimeCounter);
    }

    [Serializable]
    public sealed class CampaignPlayableState020
    {
        public const string ContentVersion="CAMPAIGN_PLAYABLE_OPERATIONS_020_2.1";
        [JsonConstructor]
        public CampaignPlayableState020(string contentVersion,CampaignPlayableOperationState020 activeOperation,IReadOnlyList<string> unlockedRepeatableContractIds,IReadOnlyList<string> completedRepeatableInstanceIds,IReadOnlyList<WorldGateProgress020> worldGates,IReadOnlyList<WorldMaterialAmount020> worldMaterials,bool campaignEpilogueUnlocked,string lastCheckpointId,CampaignProgressionState022 progression022=null,WorldGateRuntimeState023 worldGate023=null,CreatorAccessState028 creatorAccess028=null,RecruitChronicleState025 recruitChronicles025=null,PeopleBondState026 peopleBonds026=null,CampaignPlayableBeginGrant020 activeOperationGrant=null,IReadOnlyList<CampaignStepReceipt020> activeStepLedger=null)
        {
            ContentAuthorityVersion=string.IsNullOrWhiteSpace(contentVersion)?ContentVersion:contentVersion;ActiveOperation=activeOperation;UnlockedRepeatableContractIds=CopyUnique(unlockedRepeatableContractIds);CompletedRepeatableInstanceIds=CopyUnique(completedRepeatableInstanceIds);WorldGates=CopyGates(worldGates);WorldMaterials=CopyMaterials(worldMaterials);CampaignEpilogueUnlocked=campaignEpilogueUnlocked;LastCheckpointId=lastCheckpointId??string.Empty;Progression022=progression022??CampaignProgressionState022.Default();WorldGate023=worldGate023??WorldGateRuntimeState023.Default();CreatorAccess028=creatorAccess028??CreatorAccessState028.Default();RecruitChronicles025=recruitChronicles025??RecruitChronicleState025.Default();PeopleBonds026=peopleBonds026??PeopleBondState026.Default();ActiveOperationGrant=activeOperationGrant;ActiveStepLedger=CopyReceipts(activeStepLedger);
        }
        public string ContentAuthorityVersion{get;} public CampaignPlayableOperationState020 ActiveOperation{get;} public IReadOnlyList<string> UnlockedRepeatableContractIds{get;} public IReadOnlyList<string> CompletedRepeatableInstanceIds{get;} public IReadOnlyList<WorldGateProgress020> WorldGates{get;} public IReadOnlyList<WorldMaterialAmount020> WorldMaterials{get;} public bool CampaignEpilogueUnlocked{get;} public string LastCheckpointId{get;} public CampaignProgressionState022 Progression022{get;} public WorldGateRuntimeState023 WorldGate023{get;} public CreatorAccessState028 CreatorAccess028{get;} public RecruitChronicleState025 RecruitChronicles025{get;} public PeopleBondState026 PeopleBonds026{get;} public CampaignPlayableBeginGrant020 ActiveOperationGrant{get;} public IReadOnlyList<CampaignStepReceipt020> ActiveStepLedger{get;}
        public CampaignPlayableState020 With(CampaignPlayableOperationState020 activeOperation=null,bool replaceActiveOperation=false,IReadOnlyList<string> unlockedRepeatableContractIds=null,IReadOnlyList<string> completedRepeatableInstanceIds=null,IReadOnlyList<WorldGateProgress020> worldGates=null,IReadOnlyList<WorldMaterialAmount020> worldMaterials=null,bool? campaignEpilogueUnlocked=null,string lastCheckpointId=null,CampaignProgressionState022 progression022=null,bool replaceProgression022=false,WorldGateRuntimeState023 worldGate023=null,bool replaceWorldGate023=false,CreatorAccessState028 creatorAccess028=null,bool replaceCreatorAccess028=false,RecruitChronicleState025 recruitChronicles025=null,bool replaceRecruitChronicles025=false,PeopleBondState026 peopleBonds026=null,bool replacePeopleBonds026=false,CampaignPlayableBeginGrant020 activeOperationGrant=null,bool replaceActiveOperationGrant=false,IReadOnlyList<CampaignStepReceipt020> activeStepLedger=null)=>new CampaignPlayableState020(ContentAuthorityVersion,replaceActiveOperation?activeOperation:ActiveOperation,unlockedRepeatableContractIds??UnlockedRepeatableContractIds,completedRepeatableInstanceIds??CompletedRepeatableInstanceIds,worldGates??WorldGates,worldMaterials??WorldMaterials,campaignEpilogueUnlocked??CampaignEpilogueUnlocked,lastCheckpointId??LastCheckpointId,replaceProgression022?progression022:Progression022,replaceWorldGate023?worldGate023:WorldGate023,replaceCreatorAccess028?creatorAccess028:CreatorAccess028,replaceRecruitChronicles025?recruitChronicles025:RecruitChronicles025,replacePeopleBonds026?peopleBonds026:PeopleBonds026,replaceActiveOperationGrant?activeOperationGrant:ActiveOperationGrant,activeStepLedger??ActiveStepLedger);
        public static CampaignPlayableState020 Default()=>new CampaignPlayableState020(ContentVersion,null,Array.Empty<string>(),Array.Empty<string>(),new[]{new WorldGateProgress020("SKYHOME",true,0,"ALLIANCE_NETWORK",0)},Array.Empty<WorldMaterialAmount020>(),false,"campaign020_initialized",CampaignProgressionState022.Default(),WorldGateRuntimeState023.Default(),CreatorAccessState028.Default(),RecruitChronicleState025.Default(),PeopleBondState026.Default(),null,Array.Empty<CampaignStepReceipt020>());
        static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
        static IReadOnlyList<WorldGateProgress020> CopyGates(IReadOnlyList<WorldGateProgress020> values){var r=new List<WorldGateProgress020>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("World gate cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.WorldId,b.WorldId));return r.AsReadOnly();}
        static IReadOnlyList<WorldMaterialAmount020> CopyMaterials(IReadOnlyList<WorldMaterialAmount020> values){var r=new List<WorldMaterialAmount020>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("Material cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId));return r.AsReadOnly();}
        static IReadOnlyList<CampaignStepReceipt020> CopyReceipts(IReadOnlyList<CampaignStepReceipt020> values){var r=new List<CampaignStepReceipt020>();if(values!=null)for(var i=0;i<values.Count;i++){var value=values[i];if(value==null||r.Exists(existing=>StringComparer.Ordinal.Equals(existing.ReceiptId,value.ReceiptId)))continue;r.Add(value);}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.StepId,b.StepId));return r.AsReadOnly();}
    }
}
