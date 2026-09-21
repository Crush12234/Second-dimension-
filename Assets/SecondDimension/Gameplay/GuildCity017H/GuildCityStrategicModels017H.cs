using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.Campaign019;

namespace SecondDimension.Gameplay.GuildCity017H
{
    public enum CityDefenseStatus017H { Planning, Active, AwaitingBattle, Completed, Failed }
    public enum CanonEventStatus017H { Locked, Available, Viewed, Resolved }

    [Serializable]
    public sealed class DefenseLaneAssignmentState017H
    {
        [JsonConstructor]
        public DefenseLaneAssignmentState017H(string laneId, IReadOnlyList<string> unionIds)
        {
            LaneId = Require(laneId, nameof(laneId));
            UnionIds = CopyUnique(unionIds);
        }
        public string LaneId { get; }
        public IReadOnlyList<string> UnionIds { get; }
        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null) for (var i=0;i<values.Count;i++)
                if (!string.IsNullOrWhiteSpace(values[i]) && !result.Contains(values[i])) result.Add(values[i]);
            result.Sort(StringComparer.Ordinal); return result.AsReadOnly();
        }
        private static string Require(string value,string parameter)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",parameter):value;
    }

    [Serializable]
    public sealed class CityDefenseOperationState017H
    {
        [JsonConstructor]
        public CityDefenseOperationState017H(string operationId,string profileId,int currentWaveIndex,
            CityDefenseStatus017H status,int cityIntegrity,int barrierIntegrity,
            IReadOnlyList<DefenseLaneAssignmentState017H> laneAssignments,
            IReadOnlyList<string> resolvedWaveIds,string pendingWaveId,string lastCheckpointId)
        {
            OperationId=Require(operationId,nameof(operationId)); ProfileId=Require(profileId,nameof(profileId));
            if(currentWaveIndex<0||cityIntegrity<0||barrierIntegrity<0)throw new ArgumentOutOfRangeException(nameof(currentWaveIndex));
            CurrentWaveIndex=currentWaveIndex;Status=status;CityIntegrity=cityIntegrity;BarrierIntegrity=barrierIntegrity;
            LaneAssignments=CopyAssignments(laneAssignments);ResolvedWaveIds=CopyUnique(resolvedWaveIds);
            PendingWaveId=pendingWaveId??string.Empty;LastCheckpointId=lastCheckpointId??string.Empty;
        }
        public string OperationId{get;} public string ProfileId{get;} public int CurrentWaveIndex{get;}
        public CityDefenseStatus017H Status{get;} public int CityIntegrity{get;} public int BarrierIntegrity{get;}
        public IReadOnlyList<DefenseLaneAssignmentState017H> LaneAssignments{get;}
        public IReadOnlyList<string> ResolvedWaveIds{get;} public string PendingWaveId{get;} public string LastCheckpointId{get;}
        public CityDefenseOperationState017H With(int? currentWaveIndex=null,CityDefenseStatus017H? status=null,int? cityIntegrity=null,int? barrierIntegrity=null,
            IReadOnlyList<DefenseLaneAssignmentState017H> laneAssignments=null,IReadOnlyList<string> resolvedWaveIds=null,string pendingWaveId=null,string lastCheckpointId=null)=>
            new CityDefenseOperationState017H(OperationId,ProfileId,currentWaveIndex??CurrentWaveIndex,status??Status,cityIntegrity??CityIntegrity,
                barrierIntegrity??BarrierIntegrity,laneAssignments??LaneAssignments,resolvedWaveIds??ResolvedWaveIds,pendingWaveId??PendingWaveId,lastCheckpointId??LastCheckpointId);
        private static IReadOnlyList<DefenseLaneAssignmentState017H> CopyAssignments(IReadOnlyList<DefenseLaneAssignmentState017H> values){var result=new List<DefenseLaneAssignmentState017H>();if(values!=null)for(var i=0;i<values.Count;i++)result.Add(values[i]??throw new ArgumentException("Lane assignment cannot be null.",nameof(values)));result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.LaneId,b.LaneId));return result.AsReadOnly();}
        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var result=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!result.Contains(values[i]))result.Add(values[i]);result.Sort(StringComparer.Ordinal);return result.AsReadOnly();}
        private static string Require(string value,string parameter)=>string.IsNullOrWhiteSpace(value)?throw new ArgumentException("Stable ID is required.",parameter):value;
    }

    [Serializable]
    public sealed class CanonEventState017H
    {
        [JsonConstructor]
        public CanonEventState017H(string eventId,CanonEventStatus017H status,string resolutionId,int resolvedOperationOrdinal)
        {EventId=string.IsNullOrWhiteSpace(eventId)?throw new ArgumentException("Event ID required.",nameof(eventId)):eventId;Status=status;ResolutionId=resolutionId??string.Empty;if(resolvedOperationOrdinal<0)throw new ArgumentOutOfRangeException(nameof(resolvedOperationOrdinal));ResolvedOperationOrdinal=resolvedOperationOrdinal;}
        public string EventId{get;} public CanonEventStatus017H Status{get;} public string ResolutionId{get;} public int ResolvedOperationOrdinal{get;}
        public CanonEventState017H With(CanonEventStatus017H? status=null,string resolutionId=null,int? resolvedOperationOrdinal=null)=>new CanonEventState017H(EventId,status??Status,resolutionId??ResolutionId,resolvedOperationOrdinal??ResolvedOperationOrdinal);
    }

    [Serializable]
    public sealed class GuildCityStrategicState017H
    {
        public const string ContentVersion="GUILD_CITY_DEFENSE_CANON_017H_1.0";
        [JsonConstructor]
        public GuildCityStrategicState017H(string contentVersion,CityDefenseOperationState017H activeDefense,
            IReadOnlyList<CanonEventState017H> canonEvents,IReadOnlyList<string> storyGates,
            IReadOnlyList<string> appliedStrategicReceiptIds,int defenseMasteryXp,int totalDefensesWon,int totalDefensesLost,string lastCheckpointId,CampaignProgressState019 campaign019=null)
        {
            ContentAuthorityVersion=string.IsNullOrWhiteSpace(contentVersion)?ContentVersion:contentVersion;ActiveDefense=activeDefense;
            CanonEvents=CopyEvents(canonEvents);StoryGates=CopyUnique(storyGates);AppliedStrategicReceiptIds=CopyUnique(appliedStrategicReceiptIds);
            if(defenseMasteryXp<0||totalDefensesWon<0||totalDefensesLost<0)throw new ArgumentOutOfRangeException(nameof(defenseMasteryXp));
            DefenseMasteryXp=defenseMasteryXp;TotalDefensesWon=totalDefensesWon;TotalDefensesLost=totalDefensesLost;LastCheckpointId=lastCheckpointId??string.Empty;Campaign019=campaign019??CampaignProgressState019.Default();
        }
        public string ContentAuthorityVersion{get;} public CityDefenseOperationState017H ActiveDefense{get;}
        public IReadOnlyList<CanonEventState017H> CanonEvents{get;} public IReadOnlyList<string> StoryGates{get;}
        public IReadOnlyList<string> AppliedStrategicReceiptIds{get;} public int DefenseMasteryXp{get;} public int TotalDefensesWon{get;} public int TotalDefensesLost{get;} public string LastCheckpointId{get;} public CampaignProgressState019 Campaign019{get;}
        public GuildCityStrategicState017H With(CityDefenseOperationState017H activeDefense=null,bool replaceActiveDefense=false,
            IReadOnlyList<CanonEventState017H> canonEvents=null,IReadOnlyList<string> storyGates=null,IReadOnlyList<string> appliedStrategicReceiptIds=null,
            int? defenseMasteryXp=null,int? totalDefensesWon=null,int? totalDefensesLost=null,string lastCheckpointId=null,CampaignProgressState019 campaign019=null,bool replaceCampaign019=false)=>
            new GuildCityStrategicState017H(ContentAuthorityVersion,replaceActiveDefense?activeDefense:ActiveDefense,canonEvents??CanonEvents,storyGates??StoryGates,
                appliedStrategicReceiptIds??AppliedStrategicReceiptIds,defenseMasteryXp??DefenseMasteryXp,totalDefensesWon??TotalDefensesWon,totalDefensesLost??TotalDefensesLost,lastCheckpointId??LastCheckpointId,replaceCampaign019?campaign019:Campaign019);
        public static GuildCityStrategicState017H Default()=>new GuildCityStrategicState017H(ContentVersion,null,Array.Empty<CanonEventState017H>(),new[]{"STORY_GATE_OPENING_GUILD"},Array.Empty<string>(),0,0,0,"strategic_017h_initialized",CampaignProgressState019.Default());
        private static IReadOnlyList<CanonEventState017H> CopyEvents(IReadOnlyList<CanonEventState017H> values){var result=new List<CanonEventState017H>();if(values!=null)for(var i=0;i<values.Count;i++)result.Add(values[i]??throw new ArgumentException("Canon event state cannot be null.",nameof(values)));result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.EventId,b.EventId));return result.AsReadOnly();}
        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values){var result=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!result.Contains(values[i]))result.Add(values[i]);result.Sort(StringComparer.Ordinal);return result.AsReadOnly();}
    }
}