using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public enum GuildMemberAssignmentKind017D
    {
        Active,
        Reserve,
        Staff,
        Training,
        Recovering,
        Injured,
        Deployed,
        PersonalQuest,
        Archived
    }

    public enum ExpeditionStatus017D
    {
        None,
        Active,
        AwaitingBattle,
        Completed,
        Failed,
        Extracted
    }

    [Serializable]
    public sealed class GuildMaterialState017D
    {
        [JsonConstructor]
        public GuildMaterialState017D(string materialId, int amount)
        {
            MaterialId = Require(materialId, nameof(materialId));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }

        public string MaterialId { get; }
        public int Amount { get; }

        public GuildMaterialState017D WithAmount(int amount) => new GuildMaterialState017D(MaterialId, amount);

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class GuildMemberAssignmentState017D
    {
        [JsonConstructor]
        public GuildMemberAssignmentState017D(
            string recruitId,
            GuildMemberAssignmentKind017D kind,
            string facilityId,
            int recoveryProgress,
            int trainingProgress,
            int dutyProgress)
        {
            RecruitId = Require(recruitId, nameof(recruitId));
            Kind = kind;
            FacilityId = facilityId ?? string.Empty;
            RecoveryProgress = NonNegative(recoveryProgress, nameof(recoveryProgress));
            TrainingProgress = NonNegative(trainingProgress, nameof(trainingProgress));
            DutyProgress = NonNegative(dutyProgress, nameof(dutyProgress));
            if (kind != GuildMemberAssignmentKind017D.Staff && FacilityId.Length > 0)
                throw new ArgumentException("Only staff assignments may reference a facility.", nameof(facilityId));
        }

        public string RecruitId { get; }
        public GuildMemberAssignmentKind017D Kind { get; }
        public string FacilityId { get; }
        public int RecoveryProgress { get; }
        public int TrainingProgress { get; }
        public int DutyProgress { get; }

        public GuildMemberAssignmentState017D With(
            GuildMemberAssignmentKind017D? kind = null,
            string facilityId = null,
            int? recoveryProgress = null,
            int? trainingProgress = null,
            int? dutyProgress = null) =>
            new GuildMemberAssignmentState017D(
                RecruitId,
                kind ?? Kind,
                facilityId ?? (kind.HasValue && kind.Value != GuildMemberAssignmentKind017D.Staff ? string.Empty : FacilityId),
                recoveryProgress ?? RecoveryProgress,
                trainingProgress ?? TrainingProgress,
                dutyProgress ?? DutyProgress);

        private static int NonNegative(int value, string parameter) =>
            value < 0 ? throw new ArgumentOutOfRangeException(parameter) : value;
        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    public static class GuildMemberDeploymentPolicy017D
    {
        public const string TrainingUnavailableError =
            "GC017D_TRAINING_MEMBER_UNAVAILABLE_FOR_DEPLOYMENT";

        public static bool IsDeployable(GuildMemberAssignmentKind017D kind) =>
            kind == GuildMemberAssignmentKind017D.Active ||
            kind == GuildMemberAssignmentKind017D.Reserve ||
            kind == GuildMemberAssignmentKind017D.Staff ||
            kind == GuildMemberAssignmentKind017D.Deployed;

        public static bool IsRecruitDeployable(GuildCityState017D city, string recruitId)
        {
            var assignment = FindAssignment(city, recruitId);
            return assignment == null || IsDeployable(assignment.Kind);
        }

        public static bool IsTraining(GuildCityState017D city, string recruitId)
        {
            var assignment = FindAssignment(city, recruitId);
            return assignment != null && assignment.Kind == GuildMemberAssignmentKind017D.Training;
        }

        private static GuildMemberAssignmentState017D FindAssignment(
            GuildCityState017D city,
            string recruitId)
        {
            if (city == null || string.IsNullOrWhiteSpace(recruitId)) return null;
            for (var index = 0; index < city.MemberAssignments.Count; index++)
            {
                var assignment = city.MemberAssignments[index];
                if (StringComparer.Ordinal.Equals(assignment.RecruitId, recruitId))
                    return assignment;
            }
            return null;
        }
    }

    [Serializable]
    public sealed class RelationshipMemoryState017D
    {
        [JsonConstructor]
        public RelationshipMemoryState017D(
            string memoryId,
            string firstRecruitId,
            string secondRecruitId,
            string sourceId,
            string summary,
            int operationOrdinal,
            int strength,
            string sceneId,
            bool viewed)
        {
            MemoryId = Require(memoryId, nameof(memoryId));
            FirstRecruitId = Require(firstRecruitId, nameof(firstRecruitId));
            SecondRecruitId = Require(secondRecruitId, nameof(secondRecruitId));
            if (StringComparer.Ordinal.Equals(FirstRecruitId, SecondRecruitId))
                throw new ArgumentException("A relationship memory requires two recruits.");
            SourceId = Require(sourceId, nameof(sourceId));
            Summary = summary ?? string.Empty;
            if (operationOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(operationOrdinal));
            if (strength <= 0) throw new ArgumentOutOfRangeException(nameof(strength));
            OperationOrdinal = operationOrdinal;
            Strength = strength;
            SceneId = sceneId ?? string.Empty;
            Viewed = viewed;
        }

        public string MemoryId { get; }
        public string FirstRecruitId { get; }
        public string SecondRecruitId { get; }
        public string SourceId { get; }
        public string Summary { get; }
        public int OperationOrdinal { get; }
        public int Strength { get; }
        public string SceneId { get; }
        public bool Viewed { get; }

        public RelationshipMemoryState017D WithViewed(bool viewed) =>
            new RelationshipMemoryState017D(MemoryId, FirstRecruitId, SecondRecruitId, SourceId, Summary,
                OperationOrdinal, Strength, SceneId, viewed);

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class CityPlotState017D
    {
        [JsonConstructor]
        public CityPlotState017D(
            string plotId,
            string districtId,
            string size,
            bool roadConnected,
            bool unlocked,
            string buildingId,
            int buildingLevel,
            int constructionProgress,
            IReadOnlyList<string> staffRecruitIds)
        {
            PlotId = Require(plotId, nameof(plotId));
            DistrictId = Require(districtId, nameof(districtId));
            Size = string.IsNullOrWhiteSpace(size) ? "STANDARD" : size;
            RoadConnected = roadConnected;
            Unlocked = unlocked;
            BuildingId = buildingId ?? string.Empty;
            if (buildingLevel < 0) throw new ArgumentOutOfRangeException(nameof(buildingLevel));
            if (constructionProgress < 0) throw new ArgumentOutOfRangeException(nameof(constructionProgress));
            if (BuildingId.Length == 0 && buildingLevel != 0)
                throw new ArgumentException("An empty plot cannot have a building level.", nameof(buildingLevel));
            BuildingLevel = buildingLevel;
            ConstructionProgress = constructionProgress;
            StaffRecruitIds = CopyUnique(staffRecruitIds);
        }

        public string PlotId { get; }
        public string DistrictId { get; }
        public string Size { get; }
        public bool RoadConnected { get; }
        public bool Unlocked { get; }
        public string BuildingId { get; }
        public int BuildingLevel { get; }
        public int ConstructionProgress { get; }
        public IReadOnlyList<string> StaffRecruitIds { get; }

        public CityPlotState017D With(
            bool? unlocked = null,
            string buildingId = null,
            int? buildingLevel = null,
            int? constructionProgress = null,
            IReadOnlyList<string> staffRecruitIds = null) =>
            new CityPlotState017D(
                PlotId, DistrictId, Size, RoadConnected, unlocked ?? Unlocked,
                buildingId ?? BuildingId, buildingLevel ?? BuildingLevel,
                constructionProgress ?? ConstructionProgress,
                staffRecruitIds ?? StaffRecruitIds);

        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    var value = Require(values[i], nameof(values));
                    if (!result.Contains(value)) result.Add(value);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class ContractCommitState017D
    {
        [JsonConstructor]
        public ContractCommitState017D(
            string commitId,
            string contractId,
            string boardId,
            string canonicalSeedIdentity,
            int acceptedOperationOrdinal,
            bool completed,
            bool failed)
        {
            CommitId = Require(commitId, nameof(commitId));
            ContractId = Require(contractId, nameof(contractId));
            BoardId = Require(boardId, nameof(boardId));
            CanonicalSeedIdentity = Require(canonicalSeedIdentity, nameof(canonicalSeedIdentity));
            if (acceptedOperationOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(acceptedOperationOrdinal));
            if (completed && failed) throw new ArgumentException("A contract cannot be both completed and failed.");
            AcceptedOperationOrdinal = acceptedOperationOrdinal;
            Completed = completed;
            Failed = failed;
        }

        public string CommitId { get; }
        public string ContractId { get; }
        public string BoardId { get; }
        public string CanonicalSeedIdentity { get; }
        public int AcceptedOperationOrdinal { get; }
        public bool Completed { get; }
        public bool Failed { get; }

        public ContractCommitState017D WithOutcome(bool completed, bool failed) =>
            new ContractCommitState017D(CommitId, ContractId, BoardId, CanonicalSeedIdentity,
                AcceptedOperationOrdinal, completed, failed);

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class CommittedCheckState017D
    {
        [JsonConstructor]
        public CommittedCheckState017D(
            string checkId,
            string nodeId,
            string eventId,
            string actorRecruitId,
            string assistantRecruitId,
            int dieOne,
            int dieTwo,
            int modifier,
            int total,
            string outcome,
            string canonicalSeedIdentity)
        {
            CheckId = Require(checkId, nameof(checkId));
            NodeId = Require(nodeId, nameof(nodeId));
            EventId = eventId ?? string.Empty;
            ActorRecruitId = Require(actorRecruitId, nameof(actorRecruitId));
            AssistantRecruitId = assistantRecruitId ?? string.Empty;
            var authoredOutcome = dieOne == 0 && dieTwo == 0;
            if (!authoredOutcome &&
                (dieOne < 1 || dieOne > 6 || dieTwo < 1 || dieTwo > 6))
                throw new ArgumentOutOfRangeException(nameof(dieOne));
            if (total != dieOne + dieTwo + modifier)
                throw new ArgumentException("Check total must equal dice plus modifier.", nameof(total));
            DieOne = dieOne;
            DieTwo = dieTwo;
            Modifier = modifier;
            Total = total;
            Outcome = outcome ?? string.Empty;
            CanonicalSeedIdentity = Require(canonicalSeedIdentity, nameof(canonicalSeedIdentity));
        }

        public string CheckId { get; }
        public string NodeId { get; }
        public string EventId { get; }
        public string ActorRecruitId { get; }
        public string AssistantRecruitId { get; }
        public int DieOne { get; }
        public int DieTwo { get; }
        public int Modifier { get; }
        public int Total { get; }
        public string Outcome { get; }
        public string CanonicalSeedIdentity { get; }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class ExpeditionState017D
    {
        [JsonConstructor]
        public ExpeditionState017D(
            string expeditionId,
            string contractCommitId,
            string boardId,
            string currentNodeId,
            ExpeditionStatus017D status,
            int supplies,
            int fatigue,
            int threat,
            int urgency,
            IReadOnlyList<string> visitedNodeIds,
            IReadOnlyList<string> revealedNodeIds,
            IReadOnlyList<string> committedMoveIds,
            IReadOnlyList<CommittedCheckState017D> committedChecks,
            IReadOnlyList<string> objectiveFlags,
            string lastCheckpointId,
            GuildQuestFateReceipt165 pendingQuestFate165 = null)
        {
            PendingQuestFate165 = pendingQuestFate165;
            ExpeditionId = Require(expeditionId, nameof(expeditionId));
            ContractCommitId = Require(contractCommitId, nameof(contractCommitId));
            BoardId = Require(boardId, nameof(boardId));
            CurrentNodeId = Require(currentNodeId, nameof(currentNodeId));
            Status = status;
            Supplies = NonNegative(supplies, nameof(supplies));
            Fatigue = NonNegative(fatigue, nameof(fatigue));
            Threat = NonNegative(threat, nameof(threat));
            Urgency = NonNegative(urgency, nameof(urgency));
            VisitedNodeIds = CopyUnique(visitedNodeIds);
            RevealedNodeIds = CopyUnique(revealedNodeIds);
            CommittedMoveIds = CopyUnique(committedMoveIds);
            CommittedChecks = CopyChecks(committedChecks);
            ObjectiveFlags = CopyUnique(objectiveFlags);
            LastCheckpointId = lastCheckpointId ?? string.Empty;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public GuildQuestFateReceipt165 PendingQuestFate165 { get; }
        public string ExpeditionId { get; }
        public string ContractCommitId { get; }
        public string BoardId { get; }
        public string CurrentNodeId { get; }
        public ExpeditionStatus017D Status { get; }
        public int Supplies { get; }
        public int Fatigue { get; }
        public int Threat { get; }
        public int Urgency { get; }
        public IReadOnlyList<string> VisitedNodeIds { get; }
        public IReadOnlyList<string> RevealedNodeIds { get; }
        public IReadOnlyList<string> CommittedMoveIds { get; }
        public IReadOnlyList<CommittedCheckState017D> CommittedChecks { get; }
        public IReadOnlyList<string> ObjectiveFlags { get; }
        public string LastCheckpointId { get; }

        public ExpeditionState017D With(
            string currentNodeId = null,
            ExpeditionStatus017D? status = null,
            int? supplies = null,
            int? fatigue = null,
            int? threat = null,
            int? urgency = null,
            IReadOnlyList<string> visitedNodeIds = null,
            IReadOnlyList<string> revealedNodeIds = null,
            IReadOnlyList<string> committedMoveIds = null,
            IReadOnlyList<CommittedCheckState017D> committedChecks = null,
            IReadOnlyList<string> objectiveFlags = null,
            string lastCheckpointId = null,
            GuildQuestFateReceipt165 pendingQuestFate165 = null,
            bool replacePendingQuestFate165 = false) =>
            new ExpeditionState017D(
                ExpeditionId, ContractCommitId, BoardId, currentNodeId ?? CurrentNodeId,
                status ?? Status, supplies ?? Supplies, fatigue ?? Fatigue, threat ?? Threat,
                urgency ?? Urgency, visitedNodeIds ?? VisitedNodeIds, revealedNodeIds ?? RevealedNodeIds,
                committedMoveIds ?? CommittedMoveIds, committedChecks ?? CommittedChecks,
                objectiveFlags ?? ObjectiveFlags, lastCheckpointId ?? LastCheckpointId,
                replacePendingQuestFate165 ? pendingQuestFate165 : PendingQuestFate165);

        private static int NonNegative(int value, string parameter) =>
            value < 0 ? throw new ArgumentOutOfRangeException(parameter) : value;
        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    var value = Require(values[i], nameof(values));
                    if (!result.Contains(value)) result.Add(value);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }
        private static IReadOnlyList<CommittedCheckState017D> CopyChecks(IReadOnlyList<CommittedCheckState017D> values)
        {
            var result = new List<CommittedCheckState017D>();
            if (values != null)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    var value = values[i] ?? throw new ArgumentException("Check cannot be null.", nameof(values));
                    for (var j = 0; j < result.Count; j++)
                        if (StringComparer.Ordinal.Equals(result[j].CheckId, value.CheckId))
                            throw new ArgumentException("Check IDs must be unique.", nameof(values));
                    result.Add(value);
                }
            }
            result.Sort((a,b) => StringComparer.Ordinal.Compare(a.CheckId,b.CheckId));
            return result.AsReadOnly();
        }
        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }
}
