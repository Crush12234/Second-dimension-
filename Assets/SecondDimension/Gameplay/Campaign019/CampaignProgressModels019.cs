using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.Campaign020;

namespace SecondDimension.Gameplay.Campaign019
{
    [Serializable]
    public sealed class CampaignWorldStanding019
    {
        [JsonConstructor]
        public CampaignWorldStanding019(string worldId,int trust,int tension,int civilianSupport,IReadOnlyList<string> resolvedDiplomacyIds)
        {
            WorldId=Require(worldId,nameof(worldId)); Trust=Clamp(trust,-100,100); Tension=Clamp(tension,0,100); CivilianSupport=Clamp(civilianSupport,-100,100); ResolvedDiplomacyIds=CopyUnique(resolvedDiplomacyIds);
        }
        public string WorldId{get;} public int Trust{get;} public int Tension{get;} public int CivilianSupport{get;} public IReadOnlyList<string> ResolvedDiplomacyIds{get;}
        public CampaignWorldStanding019 With(int? trust=null,int? tension=null,int? civilianSupport=null,IReadOnlyList<string> resolvedDiplomacyIds=null)=>new CampaignWorldStanding019(WorldId,trust??Trust,tension??Tension,civilianSupport??CivilianSupport,resolvedDiplomacyIds??ResolvedDiplomacyIds);
        static int Clamp(int v,int min,int max)=>Math.Max(min,Math.Min(max,v));
        static string Require(string value,string param)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",param):value;
        static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
    }

    [Serializable]
    public sealed class CampaignOperationCommit019
    {
        [JsonConstructor]
        public CampaignOperationCommit019(string requestId,string chapterId,string arcId,string worldId,string mapId,string siegeId,string canonicalSeedIdentity,IReadOnlyList<string> alliedUnionIds,IReadOnlyList<string> objectiveIds,string returnCheckpointId,string preOperationStateHash,bool battleCommitted)
        {
            RequestId=Require(requestId,nameof(requestId));ChapterId=Require(chapterId,nameof(chapterId));ArcId=Require(arcId,nameof(arcId));WorldId=worldId??string.Empty;MapId=mapId??string.Empty;SiegeId=siegeId??string.Empty;CanonicalSeedIdentity=Require(canonicalSeedIdentity,nameof(canonicalSeedIdentity));AlliedUnionIds=CopyUnique(alliedUnionIds);ObjectiveIds=CopyUnique(objectiveIds);ReturnCheckpointId=Require(returnCheckpointId,nameof(returnCheckpointId));PreOperationStateHash=Require(preOperationStateHash,nameof(preOperationStateHash));BattleCommitted=battleCommitted;
        }
        public string RequestId{get;} public string ChapterId{get;} public string ArcId{get;} public string WorldId{get;} public string MapId{get;} public string SiegeId{get;} public string CanonicalSeedIdentity{get;} public IReadOnlyList<string> AlliedUnionIds{get;} public IReadOnlyList<string> ObjectiveIds{get;} public string ReturnCheckpointId{get;} public string PreOperationStateHash{get;} public bool BattleCommitted{get;}
        public CampaignOperationCommit019 With(bool? battleCommitted=null)=>new CampaignOperationCommit019(RequestId,ChapterId,ArcId,WorldId,MapId,SiegeId,CanonicalSeedIdentity,AlliedUnionIds,ObjectiveIds,ReturnCheckpointId,PreOperationStateHash,battleCommitted??BattleCommitted);
        static string Require(string value,string param)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",param):value;
        static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
    }

    [Serializable]
    public sealed class CampaignOperationReceipt019
    {
        [JsonConstructor]
        public CampaignOperationReceipt019(string receiptId,string requestId,string chapterId,string outcome,string authoritativeResultHash,string existingEquipmentRewardReceiptId,int guildXp,int hallXp,int materials,IReadOnlyList<string> relationshipMemoryIds,IReadOnlyList<string> cityUnlockIds,IReadOnlyList<string> storyGateIds,string returnCheckpointId,int appliedVersion)
        {
            ReceiptId=Require(receiptId,nameof(receiptId));RequestId=Require(requestId,nameof(requestId));ChapterId=Require(chapterId,nameof(chapterId));Outcome=Require(outcome,nameof(outcome));AuthoritativeResultHash=Require(authoritativeResultHash,nameof(authoritativeResultHash));ExistingEquipmentRewardReceiptId=existingEquipmentRewardReceiptId??string.Empty;if(guildXp<0||hallXp<0||materials<0||appliedVersion<0)throw new ArgumentOutOfRangeException(nameof(guildXp));GuildXp=guildXp;HallXp=hallXp;Materials=materials;RelationshipMemoryIds=CopyUnique(relationshipMemoryIds);CityUnlockIds=CopyUnique(cityUnlockIds);StoryGateIds=CopyUnique(storyGateIds);ReturnCheckpointId=Require(returnCheckpointId,nameof(returnCheckpointId));AppliedVersion=appliedVersion;
        }
        public string ReceiptId{get;} public string RequestId{get;} public string ChapterId{get;} public string Outcome{get;} public string AuthoritativeResultHash{get;} public string ExistingEquipmentRewardReceiptId{get;} public int GuildXp{get;} public int HallXp{get;} public int Materials{get;} public IReadOnlyList<string> RelationshipMemoryIds{get;} public IReadOnlyList<string> CityUnlockIds{get;} public IReadOnlyList<string> StoryGateIds{get;} public string ReturnCheckpointId{get;} public int AppliedVersion{get;}
        static string Require(string value,string param)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",param):value;
        static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
    }

    [Serializable]
    public sealed class CampaignProgressState019
    {
        public const string ContentVersion="CAMPAIGN_RUNTIME_CONSOLIDATION_019_1.0";
        [JsonConstructor]
        public CampaignProgressState019(string contentVersion,string activeArcId,string activeChapterId,CampaignOperationCommit019 activeOperation,CampaignOperationReceipt019 pendingReceipt,IReadOnlyList<string> completedChapterIds,IReadOnlyList<string> unlockedArcIds,IReadOnlyList<string> unlockedWorldIds,IReadOnlyList<string> discoveredMapIds,IReadOnlyList<string> appliedReceiptIds,IReadOnlyList<CampaignWorldStanding019> worldStandings,IReadOnlyList<string> worldTimeCounters,int campaignProgress,string lastCheckpointId,CampaignPlayableState020 playable020=null,CampaignReplayState130 replay130=null,CampaignRunRecovery151 runRecovery151=null)
        {
            ContentAuthorityVersion=string.IsNullOrWhiteSpace(contentVersion)?ContentVersion:contentVersion;ActiveArcId=activeArcId??string.Empty;ActiveChapterId=activeChapterId??string.Empty;ActiveOperation=activeOperation;PendingReceipt=pendingReceipt;CompletedChapterIds=CopyUnique(completedChapterIds);UnlockedArcIds=CopyUnique(unlockedArcIds);UnlockedWorldIds=CopyUnique(unlockedWorldIds);DiscoveredMapIds=CopyUnique(discoveredMapIds);AppliedReceiptIds=CopyUnique(appliedReceiptIds);WorldStandings=CopyStandings(worldStandings);WorldTimeCounters=CopyUnique(worldTimeCounters);if(campaignProgress<0)throw new ArgumentOutOfRangeException(nameof(campaignProgress));CampaignProgress=campaignProgress;LastCheckpointId=lastCheckpointId??string.Empty;Playable020=playable020??CampaignPlayableState020.Default();Replay130=replay130;RunRecovery151=runRecovery151;
        }
        public string ContentAuthorityVersion{get;} public string ActiveArcId{get;} public string ActiveChapterId{get;} public CampaignOperationCommit019 ActiveOperation{get;} public CampaignOperationReceipt019 PendingReceipt{get;} public IReadOnlyList<string> CompletedChapterIds{get;} public IReadOnlyList<string> UnlockedArcIds{get;} public IReadOnlyList<string> UnlockedWorldIds{get;} public IReadOnlyList<string> DiscoveredMapIds{get;} public IReadOnlyList<string> AppliedReceiptIds{get;} public IReadOnlyList<CampaignWorldStanding019> WorldStandings{get;} public IReadOnlyList<string> WorldTimeCounters{get;} public int CampaignProgress{get;} public string LastCheckpointId{get;} public CampaignPlayableState020 Playable020{get;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public CampaignReplayState130 Replay130{get;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public CampaignRunRecovery151 RunRecovery151{get;}
        public CampaignProgressState019 With(string activeArcId=null,string activeChapterId=null,CampaignOperationCommit019 activeOperation=null,bool replaceActiveOperation=false,CampaignOperationReceipt019 pendingReceipt=null,bool replacePendingReceipt=false,IReadOnlyList<string> completedChapterIds=null,IReadOnlyList<string> unlockedArcIds=null,IReadOnlyList<string> unlockedWorldIds=null,IReadOnlyList<string> discoveredMapIds=null,IReadOnlyList<string> appliedReceiptIds=null,IReadOnlyList<CampaignWorldStanding019> worldStandings=null,IReadOnlyList<string> worldTimeCounters=null,int? campaignProgress=null,string lastCheckpointId=null,CampaignPlayableState020 playable020=null,bool replacePlayable020=false,CampaignReplayState130 replay130=null,bool replaceReplay130=false,CampaignRunRecovery151 runRecovery151=null,bool replaceRunRecovery151=false)=>new CampaignProgressState019(ContentAuthorityVersion,activeArcId??ActiveArcId,activeChapterId??ActiveChapterId,replaceActiveOperation?activeOperation:ActiveOperation,replacePendingReceipt?pendingReceipt:PendingReceipt,completedChapterIds??CompletedChapterIds,unlockedArcIds??UnlockedArcIds,unlockedWorldIds??UnlockedWorldIds,discoveredMapIds??DiscoveredMapIds,appliedReceiptIds??AppliedReceiptIds,worldStandings??WorldStandings,worldTimeCounters??WorldTimeCounters,campaignProgress??CampaignProgress,lastCheckpointId??LastCheckpointId,replacePlayable020?playable020:Playable020,replaceReplay130?replay130:Replay130,replaceRunRecovery151?runRecovery151:RunRecovery151);
        public static CampaignProgressState019 Default()=>new CampaignProgressState019(ContentVersion,"ARC018_FIRST_GATE_ECHOES","",null,null,Array.Empty<string>(),new[]{"ARC018_FIRST_GATE_ECHOES"},Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<CampaignWorldStanding019>(),Array.Empty<string>(),0,"campaign_019_initialized",CampaignPlayableState020.Default());
        static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
        static IReadOnlyList<CampaignWorldStanding019> CopyStandings(IReadOnlyList<CampaignWorldStanding019> values){var r=new List<CampaignWorldStanding019>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("World standing cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.WorldId,b.WorldId));return r.AsReadOnly();}
    }
}
