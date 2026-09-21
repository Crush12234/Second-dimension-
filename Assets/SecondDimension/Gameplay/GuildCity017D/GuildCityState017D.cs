using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    [Serializable]
    public sealed class EncounterLaunchRequest017D
    {
        [JsonConstructor]
        public EncounterLaunchRequest017D(
            string requestId,
            string contractId,
            string expeditionId,
            string boardId,
            string nodeId,
            string encounterId,
            string battleId,
            string objective,
            int enemyUnionCount,
            string canonicalSeedIdentity,
            IReadOnlyList<string> alliedUnionIds,
            IReadOnlyList<string> reserveUnionIds,
            IReadOnlyList<string> objectiveIds,
            IReadOnlyList<string> routeModifiers,
            int supplies,
            int fatigue,
            int urgency,
            string returnCheckpointId,
            string preBattleStateHash)
        {
            RequestId = Require(requestId, nameof(requestId));
            ContractId = Require(contractId, nameof(contractId));
            ExpeditionId = Require(expeditionId, nameof(expeditionId));
            BoardId = Require(boardId, nameof(boardId));
            NodeId = Require(nodeId, nameof(nodeId));
            EncounterId = Require(encounterId, nameof(encounterId));
            BattleId = Require(battleId, nameof(battleId));
            Objective = string.IsNullOrWhiteSpace(objective) ? "Resolve the encounter." : objective;
            EnemyUnionCount = Math.Max(1, Math.Min(10, enemyUnionCount <= 0 ? 1 : enemyUnionCount));
            CanonicalSeedIdentity = Require(canonicalSeedIdentity, nameof(canonicalSeedIdentity));
            AlliedUnionIds = CopyUnique(alliedUnionIds);
            ReserveUnionIds = CopyUnique(reserveUnionIds);
            ObjectiveIds = CopyUnique(objectiveIds);
            RouteModifiers = CopyUnique(routeModifiers);
            if (supplies < 0 || fatigue < 0 || urgency < 0) throw new ArgumentOutOfRangeException(nameof(supplies));
            Supplies = supplies;
            Fatigue = fatigue;
            Urgency = urgency;
            ReturnCheckpointId = Require(returnCheckpointId, nameof(returnCheckpointId));
            PreBattleStateHash = Require(preBattleStateHash, nameof(preBattleStateHash));
        }

        public string RequestId { get; }
        public string ContractId { get; }
        public string ExpeditionId { get; }
        public string BoardId { get; }
        public string NodeId { get; }
        public string EncounterId { get; }
        public string BattleId { get; }
        public string Objective { get; }
        public int EnemyUnionCount { get; }
        public string CanonicalSeedIdentity { get; }
        public IReadOnlyList<string> AlliedUnionIds { get; }
        public IReadOnlyList<string> ReserveUnionIds { get; }
        public IReadOnlyList<string> ObjectiveIds { get; }
        public IReadOnlyList<string> RouteModifiers { get; }
        public int Supplies { get; }
        public int Fatigue { get; }
        public int Urgency { get; }
        public string ReturnCheckpointId { get; }
        public string PreBattleStateHash { get; }

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
    public sealed class BattleReturnReceipt017D
    {
        [JsonConstructor]
        public BattleReturnReceipt017D(
            string receiptId,
            string launchRequestId,
            string battleRunId,
            string outcome,
            string battleResultHash,
            int supplyConsumption,
            int fatigueDelta,
            int urgencyDelta,
            IReadOnlyList<string> objectiveFlags,
            IReadOnlyList<GuildMaterialState017D> materialRewards,
            string equipmentRewardReceiptId,
            IReadOnlyList<RelationshipMemoryState017D> relationshipMemories,
            int cityProjectContribution,
            string returnCheckpointId,
            bool applied)
        {
            ReceiptId = Require(receiptId, nameof(receiptId));
            LaunchRequestId = Require(launchRequestId, nameof(launchRequestId));
            BattleRunId = Require(battleRunId, nameof(battleRunId));
            Outcome = Require(outcome, nameof(outcome));
            BattleResultHash = Require(battleResultHash, nameof(battleResultHash));
            if (supplyConsumption < 0 || cityProjectContribution < 0) throw new ArgumentOutOfRangeException(nameof(supplyConsumption));
            SupplyConsumption = supplyConsumption;
            FatigueDelta = fatigueDelta;
            UrgencyDelta = urgencyDelta;
            ObjectiveFlags = CopyUnique(objectiveFlags);
            MaterialRewards = CopyMaterials(materialRewards);
            EquipmentRewardReceiptId = equipmentRewardReceiptId ?? string.Empty;
            RelationshipMemories = CopyMemories(relationshipMemories);
            CityProjectContribution = cityProjectContribution;
            ReturnCheckpointId = Require(returnCheckpointId, nameof(returnCheckpointId));
            Applied = applied;
        }

        public string ReceiptId { get; }
        public string LaunchRequestId { get; }
        public string BattleRunId { get; }
        public string Outcome { get; }
        public string BattleResultHash { get; }
        public int SupplyConsumption { get; }
        public int FatigueDelta { get; }
        public int UrgencyDelta { get; }
        public IReadOnlyList<string> ObjectiveFlags { get; }
        public IReadOnlyList<GuildMaterialState017D> MaterialRewards { get; }
        public string EquipmentRewardReceiptId { get; }
        public IReadOnlyList<RelationshipMemoryState017D> RelationshipMemories { get; }
        public int CityProjectContribution { get; }
        public string ReturnCheckpointId { get; }
        public bool Applied { get; }

        public BattleReturnReceipt017D WithApplied(bool applied) =>
            new BattleReturnReceipt017D(ReceiptId, LaunchRequestId, BattleRunId, Outcome, BattleResultHash,
                SupplyConsumption, FatigueDelta, UrgencyDelta, ObjectiveFlags, MaterialRewards,
                EquipmentRewardReceiptId, RelationshipMemories, CityProjectContribution,
                ReturnCheckpointId, applied);

        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> values)
        {
            var result = new List<string>();
            if (values != null) for (var i = 0; i < values.Count; i++) if (!string.IsNullOrWhiteSpace(values[i]) && !result.Contains(values[i])) result.Add(values[i]);
            result.Sort(StringComparer.Ordinal); return result.AsReadOnly();
        }
        private static IReadOnlyList<GuildMaterialState017D> CopyMaterials(IReadOnlyList<GuildMaterialState017D> values)
        {
            var result = new List<GuildMaterialState017D>();
            if (values != null) for (var i = 0; i < values.Count; i++) result.Add(values[i] ?? throw new ArgumentException("Material cannot be null.", nameof(values)));
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId)); return result.AsReadOnly();
        }
        private static IReadOnlyList<RelationshipMemoryState017D> CopyMemories(IReadOnlyList<RelationshipMemoryState017D> values)
        {
            var result = new List<RelationshipMemoryState017D>();
            if (values != null) for (var i = 0; i < values.Count; i++) result.Add(values[i] ?? throw new ArgumentException("Memory cannot be null.", nameof(values)));
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MemoryId,b.MemoryId)); return result.AsReadOnly();
        }
        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", parameter) : value;
    }

    [Serializable]
    public sealed class GuildCityState017D
    {
        public const string ContentVersion = "GUILD_CITY_IMPLEMENTATION_017D_1.0";

        [JsonConstructor]
        public GuildCityState017D(
            string contentVersion,
            int operationOrdinal,
            int charterBuildCredits,
            int civicTrust,
            IReadOnlyList<GuildMaterialState017D> materials,
            IReadOnlyList<GuildMemberAssignmentState017D> memberAssignments,
            IReadOnlyList<RelationshipMemoryState017D> relationshipMemories,
            IReadOnlyList<CityPlotState017D> cityPlots,
            ContractCommitState017D activeContract,
            ExpeditionState017D expedition,
            EncounterLaunchRequest017D pendingEncounter,
            BattleReturnReceipt017D pendingBattleReturn,
            IReadOnlyList<string> appliedBattleReturnIds,
            ApplicantBoardState recruitmentBoard,
            int recruitmentRefreshOrdinal,
            int recruitmentDryStreak,
            string lastCheckpointId,
            GuildCityStrategicState017H strategic017H = null)
        {
            ContentAuthorityVersion = string.IsNullOrWhiteSpace(contentVersion) ? ContentVersion : contentVersion;
            if (operationOrdinal < 0 || charterBuildCredits < 0 || civicTrust < 0)
                throw new ArgumentOutOfRangeException(nameof(operationOrdinal));
            OperationOrdinal = operationOrdinal;
            CharterBuildCredits = charterBuildCredits;
            CivicTrust = civicTrust;
            Materials = CopyMaterials(materials);
            MemberAssignments = CopyAssignments(memberAssignments);
            RelationshipMemories = CopyMemories(relationshipMemories);
            CityPlots = CopyPlots(cityPlots);
            ActiveContract = activeContract;
            Expedition = expedition;
            PendingEncounter = pendingEncounter;
            PendingBattleReturn = pendingBattleReturn;
            AppliedBattleReturnIds = CopyUnique(appliedBattleReturnIds);
            RecruitmentBoard = recruitmentBoard;
            if (recruitmentRefreshOrdinal < 0 || recruitmentDryStreak < 0)
                throw new ArgumentOutOfRangeException(nameof(recruitmentRefreshOrdinal));
            RecruitmentRefreshOrdinal = recruitmentRefreshOrdinal;
            RecruitmentDryStreak = recruitmentDryStreak;
            LastCheckpointId = lastCheckpointId ?? string.Empty;
            Strategic017H = strategic017H ?? GuildCityStrategicState017H.Default();
        }

        public string ContentAuthorityVersion { get; }
        public int OperationOrdinal { get; }
        public int CharterBuildCredits { get; }
        public int CivicTrust { get; }
        public IReadOnlyList<GuildMaterialState017D> Materials { get; }
        public IReadOnlyList<GuildMemberAssignmentState017D> MemberAssignments { get; }
        public IReadOnlyList<RelationshipMemoryState017D> RelationshipMemories { get; }
        public IReadOnlyList<CityPlotState017D> CityPlots { get; }
        public ContractCommitState017D ActiveContract { get; }
        public ExpeditionState017D Expedition { get; }
        public EncounterLaunchRequest017D PendingEncounter { get; }
        public BattleReturnReceipt017D PendingBattleReturn { get; }
        public IReadOnlyList<string> AppliedBattleReturnIds { get; }
        public ApplicantBoardState RecruitmentBoard { get; }
        public int RecruitmentRefreshOrdinal { get; }
        public int RecruitmentDryStreak { get; }
        public string LastCheckpointId { get; }
        public GuildCityStrategicState017H Strategic017H { get; }

        public GuildCityState017D With(
            int? operationOrdinal = null,
            int? charterBuildCredits = null,
            int? civicTrust = null,
            IReadOnlyList<GuildMaterialState017D> materials = null,
            IReadOnlyList<GuildMemberAssignmentState017D> memberAssignments = null,
            IReadOnlyList<RelationshipMemoryState017D> relationshipMemories = null,
            IReadOnlyList<CityPlotState017D> cityPlots = null,
            ContractCommitState017D activeContract = null,
            bool replaceActiveContract = false,
            ExpeditionState017D expedition = null,
            bool replaceExpedition = false,
            EncounterLaunchRequest017D pendingEncounter = null,
            bool replacePendingEncounter = false,
            BattleReturnReceipt017D pendingBattleReturn = null,
            bool replacePendingBattleReturn = false,
            IReadOnlyList<string> appliedBattleReturnIds = null,
            ApplicantBoardState recruitmentBoard = null,
            bool replaceRecruitmentBoard = false,
            int? recruitmentRefreshOrdinal = null,
            int? recruitmentDryStreak = null,
            string lastCheckpointId = null,
            GuildCityStrategicState017H strategic017H = null,
            bool replaceStrategic017H = false) =>
            new GuildCityState017D(
                ContentAuthorityVersion,
                operationOrdinal ?? OperationOrdinal,
                charterBuildCredits ?? CharterBuildCredits,
                civicTrust ?? CivicTrust,
                materials ?? Materials,
                memberAssignments ?? MemberAssignments,
                relationshipMemories ?? RelationshipMemories,
                cityPlots ?? CityPlots,
                replaceActiveContract ? activeContract : ActiveContract,
                replaceExpedition ? expedition : Expedition,
                replacePendingEncounter ? pendingEncounter : PendingEncounter,
                replacePendingBattleReturn ? pendingBattleReturn : PendingBattleReturn,
                appliedBattleReturnIds ?? AppliedBattleReturnIds,
                replaceRecruitmentBoard ? recruitmentBoard : RecruitmentBoard,
                recruitmentRefreshOrdinal ?? RecruitmentRefreshOrdinal,
                recruitmentDryStreak ?? RecruitmentDryStreak,
                lastCheckpointId ?? LastCheckpointId,
                replaceStrategic017H ? strategic017H : Strategic017H);

        public static GuildCityState017D Default(
            IReadOnlyList<RecruitState> recruits,
            IReadOnlyList<UnionState> unions = null)
        {
            var activeRecruitIds = new HashSet<string>(StringComparer.Ordinal);
            if (unions != null)
            {
                for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
                {
                    var union = unions[unionIndex];
                    if (union == null || union.Kind != UnionKind.Normal) continue;
                    for (var memberIndex = 0; memberIndex < union.MemberRecruitIds.Count; memberIndex++)
                        activeRecruitIds.Add(union.MemberRecruitIds[memberIndex]);
                }
            }
            var assignments = new List<GuildMemberAssignmentState017D>();
            if (recruits != null)
                for (var i = 0; i < recruits.Count; i++)
                    assignments.Add(new GuildMemberAssignmentState017D(recruits[i].RecruitId,
                        activeRecruitIds.Contains(recruits[i].RecruitId)
                            ? GuildMemberAssignmentKind017D.Active
                            : GuildMemberAssignmentKind017D.Reserve,
                        string.Empty, 0, 0, 0));
            var plots = GuildCityOpeningContent017D.CreateInitialPlotStates();
            return new GuildCityState017D(ContentVersion, 0, 1, 0,
                new[] { new GuildMaterialState017D("MAT_SALVAGED_TIMBER", 12), new GuildMaterialState017D("MAT_GATE_IRON", 8), new GuildMaterialState017D("MAT_GATEGLASS", 4) },
                assignments.AsReadOnly(), Array.Empty<RelationshipMemoryState017D>(), plots,
                null, null, null, null, Array.Empty<string>(), null, 0, 0,
                "guild_city_initialized", GuildCityStrategicState017H.Default());
        }

        private static IReadOnlyList<GuildMaterialState017D> CopyMaterials(IReadOnlyList<GuildMaterialState017D> source)
        {
            var result = new List<GuildMaterialState017D>();
            if (source != null) for (var i = 0; i < source.Count; i++) result.Add(source[i] ?? throw new ArgumentException("Material cannot be null.", nameof(source)));
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId)); return result.AsReadOnly();
        }
        private static IReadOnlyList<GuildMemberAssignmentState017D> CopyAssignments(IReadOnlyList<GuildMemberAssignmentState017D> source)
        {
            var result = new List<GuildMemberAssignmentState017D>();
            if (source != null) for (var i = 0; i < source.Count; i++) result.Add(source[i] ?? throw new ArgumentException("Assignment cannot be null.", nameof(source)));
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.RecruitId,b.RecruitId)); return result.AsReadOnly();
        }
        private static IReadOnlyList<RelationshipMemoryState017D> CopyMemories(IReadOnlyList<RelationshipMemoryState017D> source)
        {
            var result = new List<RelationshipMemoryState017D>();
            if (source != null) for (var i = 0; i < source.Count; i++) result.Add(source[i] ?? throw new ArgumentException("Memory cannot be null.", nameof(source)));
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MemoryId,b.MemoryId)); return result.AsReadOnly();
        }
        private static IReadOnlyList<CityPlotState017D> CopyPlots(IReadOnlyList<CityPlotState017D> source)
        {
            var result = new List<CityPlotState017D>();
            if (source != null) for (var i = 0; i < source.Count; i++) result.Add(source[i] ?? throw new ArgumentException("Plot cannot be null.", nameof(source)));
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.PlotId,b.PlotId)); return result.AsReadOnly();
        }
        private static IReadOnlyList<string> CopyUnique(IReadOnlyList<string> source)
        {
            var result = new List<string>();
            if (source != null) for (var i=0;i<source.Count;i++) if (!string.IsNullOrWhiteSpace(source[i]) && !result.Contains(source[i])) result.Add(source[i]);
            result.Sort(StringComparer.Ordinal); return result.AsReadOnly();
        }
    }
}
