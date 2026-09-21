using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation.GuildCity017H
{
    public sealed class BuildingContributionView017H { public string BuildingId{get;set;} public string DisplayName{get;set;} public int Level{get;set;} public bool Staffed{get;set;} public string Summary{get;set;} public string CombatContribution{get;set;} public string XpContribution{get;set;} }
    public sealed class DefenseProfileView017H { public string ProfileId{get;set;} public string DisplayName{get;set;} public string Classification{get;set;} public string Summary{get;set;} public string RequiredStoryGate{get;set;} public bool Available{get;set;} public bool FutureLocked{get;set;} public int WaveCount{get;set;} public IReadOnlyList<string> LaneNames{get;set;}=Array.Empty<string>(); }
    public sealed class DefenseLaneView017H { public string LaneId{get;set;} public string DisplayName{get;set;} public string Summary{get;set;} public IReadOnlyList<string> AssignedUnionIds{get;set;}=Array.Empty<string>(); }
    public sealed class ActiveDefenseView017H { public string OperationId{get;set;} public string ProfileId{get;set;} public string DisplayName{get;set;} public string Status{get;set;} public int CurrentWave{get;set;} public int TotalWaves{get;set;} public string CurrentWaveName{get;set;} public bool CurrentWaveDecisiveBattle{get;set;} public int EnemyUnionCount{get;set;} public int CityIntegrity{get;set;} public int BarrierIntegrity{get;set;} public IReadOnlyList<DefenseLaneView017H> Lanes{get;set;}=Array.Empty<DefenseLaneView017H>(); }
    public sealed class CanonEventView017H { public string EventId{get;set;} public string DisplayName{get;set;} public string Classification{get;set;} public string Summary{get;set;} public string Status{get;set;} public string RequiredStoryGate{get;set;} public string DefenseProfileId{get;set;} public bool OutcomeLocked{get;set;} public bool Available{get;set;} }
    public sealed class GuildCityStrategicPresentationState017H
    {
        public bool IsAvailable{get;set;} public string Error{get;set;} public int DefenseMasteryXp{get;set;} public int DefensesWon{get;set;} public int DefensesLost{get;set;} public int TotalBuildingCount{get;set;} public int ActiveContributionBuildingCount{get;set;} public string AggregateCombatSummary{get;set;} public string AggregateXpSummary{get;set;} public IReadOnlyList<BuildingContributionView017H> Buildings{get;set;}=Array.Empty<BuildingContributionView017H>(); public IReadOnlyList<DefenseProfileView017H> DefenseProfiles{get;set;}=Array.Empty<DefenseProfileView017H>(); public ActiveDefenseView017H ActiveDefense{get;set;} public IReadOnlyList<CanonEventView017H> CanonEvents{get;set;}=Array.Empty<CanonEventView017H>(); public IReadOnlyList<string> StoryGates{get;set;}=Array.Empty<string>();
    }
    public interface IGuildCityStrategicPresentationCoordinator017H
    {
        GuildCityStrategicPresentationState017H GuildCityStrategic017H{get;}
        M1CommandResult StartGuildCityDefense017H(string profileId);
        M1CommandResult AssignGuildCityDefenseUnion017H(string laneId,string unionId);
        M1CommandResult CommitGuildCityDefenseWave017H();
        M1CommandResult StartCommittedGuildCityDefenseBattle017H();
        M1CommandResult FinalizeGuildCityDefense017H();
        M1CommandResult SynchronizeGuildCityCanonEvents017H();
        M1CommandResult ViewGuildCityCanonEvent017H(string eventId);
        M1CommandResult ResolveGuildCityCanonEvent017H(string eventId,string choiceId);
    }
}