using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    public enum BattleSide { Player, Enemy }
    public enum BattlePhase { ForecastSelection, Resolved }
    public enum BattleOutcome { InProgress, Victory, Defeat, Retreat }
    public enum EngagementState
    {
        Open = 0,
        Advancing = 1,
        Engaged = 2,
        Guarded = 3,
        Disengaging = 4,
        Broken = 5,
        Flanking = 6,
        RearPressure = 7,
        Intercepting = 8,
        Supporting = 9,
        Reinforcing = 10
    }
    public enum BattleActionKind { Martial, Mystic, Restoration, Guard, Recovery, Tactical }

    [Serializable]
    public sealed class BattleArtProgressState
    {
        [JsonConstructor]
        public BattleArtProgressState(
            string artId,
            string discipline,
            int meaningfulUses,
            int masteryPoints)
        {
            ArtId = string.IsNullOrWhiteSpace(artId)
                ? throw new ArgumentException("Art ID is required.", nameof(artId))
                : artId;
            Discipline = discipline ?? string.Empty;
            MeaningfulUses = Math.Max(0, meaningfulUses);
            MasteryPoints = Math.Max(0, masteryPoints);
        }

        public string ArtId { get; }
        public string Discipline { get; }
        public int MeaningfulUses { get; }
        public int MasteryPoints { get; }

        public BattleArtProgressState AddMeaningfulUse(int masteryGain) =>
            new BattleArtProgressState(ArtId, Discipline, MeaningfulUses + 1, MasteryPoints + Math.Max(0, masteryGain));
    }

    [Serializable]
    public sealed class BattleMemberState
    {
        [JsonConstructor]
        public BattleMemberState(
            string memberId,
            string displayName,
            string classId,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            int attack,
            int magicAttack,
            IReadOnlyList<string> equipmentTags,
            bool downed,
            bool stabilized,
            bool guarding,
            IReadOnlyList<string> learnedArtIds,
            int meaningfulUsePoints,
            int discoveryProgress,
            string breakthroughArtId,
            IReadOnlyList<BattleArtProgressState> artProgress = null,
            string equippedMainHandInstanceId = null,
            string enemyArtBaseId090 = null,
            string enemyArtVariantId090 = null,
            int visualVariantSeed090 = 0)
        {
            MemberId = Require(memberId, nameof(memberId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? MemberId : displayName;
            ClassId = classId ?? string.Empty;
            MaximumHp = Positive(maximumHp, nameof(maximumHp));
            CurrentHp = Range(currentHp, 0, MaximumHp, nameof(currentHp));
            MaximumMp = Range(maximumMp, 0, 9999, nameof(maximumMp));
            CurrentMp = Range(currentMp, 0, MaximumMp, nameof(currentMp));
            Attack = Positive(attack, nameof(attack));
            MagicAttack = Positive(magicAttack, nameof(magicAttack));
            EquipmentTags = CopyStrings(equipmentTags);
            Downed = downed || CurrentHp == 0;
            Stabilized = Downed && stabilized;
            Guarding = !Downed && guarding;
            LearnedArtIds = CopyStrings(learnedArtIds);
            MeaningfulUsePoints = Range(meaningfulUsePoints, 0, int.MaxValue, nameof(meaningfulUsePoints));
            DiscoveryProgress = Range(discoveryProgress, 0, 100, nameof(discoveryProgress));
            BreakthroughArtId = breakthroughArtId ?? string.Empty;
            ArtProgress = CopyArtProgress(artProgress);
            EquippedMainHandInstanceId = string.IsNullOrWhiteSpace(equippedMainHandInstanceId)
                ? null
                : equippedMainHandInstanceId;
            EnemyArtBaseId090 = NormalizeEnemyArtId090(enemyArtBaseId090);
            EnemyArtVariantId090 = NormalizeEnemyArtId090(enemyArtVariantId090);
            VisualVariantSeed090 = Math.Max(0, visualVariantSeed090);
        }

        public string MemberId { get; }
        public string DisplayName { get; }
        public string ClassId { get; }
        public int CurrentHp { get; }
        public int MaximumHp { get; }
        public int CurrentMp { get; }
        public int MaximumMp { get; }
        public int Attack { get; }
        public int MagicAttack { get; }
        public IReadOnlyList<string> EquipmentTags { get; }
        public bool Downed { get; }
        public bool Stabilized { get; }
        public bool Guarding { get; }
        public IReadOnlyList<string> LearnedArtIds { get; }
        public int MeaningfulUsePoints { get; }
        public int DiscoveryProgress { get; }
        public string BreakthroughArtId { get; }
        public IReadOnlyList<BattleArtProgressState> ArtProgress { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EquippedMainHandInstanceId { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EnemyArtBaseId090 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string EnemyArtVariantId090 { get; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int VisualVariantSeed090 { get; }

        public BattleMemberState With(
            int? currentHp = null,
            int? currentMp = null,
            bool? stabilized = null,
            bool? guarding = null,
            IReadOnlyList<string> learnedArtIds = null,
            int? meaningfulUsePoints = null,
            int? discoveryProgress = null,
            string breakthroughArtId = null,
            IReadOnlyList<BattleArtProgressState> artProgress = null) =>
            new BattleMemberState(
                MemberId, DisplayName, ClassId,
                currentHp ?? CurrentHp, MaximumHp,
                currentMp ?? CurrentMp, MaximumMp,
                Attack, MagicAttack, EquipmentTags,
                (currentHp ?? CurrentHp) == 0,
                stabilized ?? Stabilized,
                guarding ?? Guarding,
                learnedArtIds ?? LearnedArtIds,
                meaningfulUsePoints ?? MeaningfulUsePoints,
                discoveryProgress ?? DiscoveryProgress,
                breakthroughArtId ?? BreakthroughArtId,
                artProgress ?? ArtProgress,
                EquippedMainHandInstanceId,
                EnemyArtBaseId090,
                EnemyArtVariantId090,
                VisualVariantSeed090);

        public BattleMemberState WithEnemyArt090(
            string enemyArtBaseId090,
            string enemyArtVariantId090,
            int visualVariantSeed090) =>
            new BattleMemberState(
                MemberId, DisplayName, ClassId,
                CurrentHp, MaximumHp, CurrentMp, MaximumMp,
                Attack, MagicAttack, EquipmentTags, Downed, Stabilized, Guarding,
                LearnedArtIds, MeaningfulUsePoints, DiscoveryProgress,
                BreakthroughArtId, ArtProgress, EquippedMainHandInstanceId,
                enemyArtBaseId090, enemyArtVariantId090, visualVariantSeed090);

        private static string NormalizeEnemyArtId090(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        private static string Require(string value, string name) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", name) : value;
        private static int Positive(int value, string name) => value > 0 ? value : throw new ArgumentOutOfRangeException(name);
        private static int Range(int value, int low, int high, string name) =>
            value >= low && value <= high ? value : throw new ArgumentOutOfRangeException(name);
        internal static IReadOnlyList<string> CopyStrings(IReadOnlyList<string> source)
        {
            var copy = new List<string>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i] ?? string.Empty);
            return copy.AsReadOnly();
        }

        private static IReadOnlyList<BattleArtProgressState> CopyArtProgress(IReadOnlyList<BattleArtProgressState> source)
        {
            var copy = new List<BattleArtProgressState>();
            if (source != null)
            {
                for (var i = 0; i < source.Count; i++)
                {
                    var value = source[i] ?? throw new ArgumentException("Art progress cannot be null.", nameof(source));
                    for (var existing = 0; existing < copy.Count; existing++)
                        if (StringComparer.Ordinal.Equals(copy[existing].ArtId, value.ArtId))
                            throw new ArgumentException("Art progress IDs must be unique.", nameof(source));
                    copy.Add(value);
                }
            }
            copy.Sort((left, right) => StringComparer.Ordinal.Compare(left.ArtId, right.ArtId));
            return copy.AsReadOnly();
        }
    }

    [Serializable]
    public sealed class BattleUnionState
    {
        [JsonConstructor]
        public BattleUnionState(
            string unionId,
            string displayName,
            BattleSide side,
            string leaderMemberId,
            IReadOnlyList<BattleMemberState> members,
            string formationId,
            string formationName,
            bool formationMemberCountEligible,
            string formationInactiveReason,
            int currentAp,
            int maximumAp,
            int cohesion,
            int formationConditionBasisPoints,
            EngagementState engagement,
            bool guarding,
            bool retreated,
            int unionMeaningfulUsePoints)
        {
            UnionId = Require(unionId, nameof(unionId));
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? UnionId : displayName;
            Side = side;
            LeaderMemberId = Require(leaderMemberId, nameof(leaderMemberId));
            Members = CopyMembers(members);
            if (Members.Count == 0) throw new ArgumentException("Battle Union requires at least one member.", nameof(members));
            if (!ContainsMember(LeaderMemberId)) throw new ArgumentException("Leader must belong to the Union.", nameof(leaderMemberId));
            FormationId = formationId ?? string.Empty;
            FormationName = string.IsNullOrWhiteSpace(formationName) ? FormationId : formationName;
            FormationMemberCountEligible = formationMemberCountEligible;
            FormationInactiveReason = formationInactiveReason ?? string.Empty;
            MaximumAp = Range(maximumAp, 1, 999, nameof(maximumAp));
            CurrentAp = Range(currentAp, 0, MaximumAp, nameof(currentAp));
            Cohesion = Range(cohesion, 0, 100, nameof(cohesion));
            FormationConditionBasisPoints = Range(formationConditionBasisPoints, 0, 10000, nameof(formationConditionBasisPoints));
            Engagement = engagement;
            Guarding = guarding;
            Retreated = retreated;
            UnionMeaningfulUsePoints = Range(unionMeaningfulUsePoints, 0, int.MaxValue, nameof(unionMeaningfulUsePoints));
        }

        public string UnionId { get; }
        public string DisplayName { get; }
        public BattleSide Side { get; }
        public string LeaderMemberId { get; }
        public IReadOnlyList<BattleMemberState> Members { get; }
        public string FormationId { get; }
        public string FormationName { get; }
        public bool FormationMemberCountEligible { get; }
        public bool FormationBenefitActive =>
            FormationMemberCountEligible && FormationConditionBasisPoints > 0 && Cohesion > 0;
        public string FormationInactiveReason { get; }
        public int CurrentAp { get; }
        public int MaximumAp { get; }
        public int Cohesion { get; }
        public int FormationConditionBasisPoints { get; }
        public EngagementState Engagement { get; }
        public bool Guarding { get; }
        public bool Retreated { get; }
        public int UnionMeaningfulUsePoints { get; }
        public bool IsDefeated
        {
            get
            {
                for (var i = 0; i < Members.Count; i++) if (!Members[i].Downed) return false;
                return true;
            }
        }

        public BattleUnionState With(
            IReadOnlyList<BattleMemberState> members = null,
            int? currentAp = null,
            int? cohesion = null,
            int? formationConditionBasisPoints = null,
            EngagementState? engagement = null,
            bool? guarding = null,
            bool? retreated = null,
            int? unionMeaningfulUsePoints = null) =>
            new BattleUnionState(
                UnionId, DisplayName, Side, LeaderMemberId, members ?? Members,
                FormationId, FormationName, FormationMemberCountEligible, FormationInactiveReason,
                currentAp ?? CurrentAp, MaximumAp, cohesion ?? Cohesion,
                formationConditionBasisPoints ?? FormationConditionBasisPoints,
                engagement ?? Engagement, guarding ?? Guarding, retreated ?? Retreated,
                unionMeaningfulUsePoints ?? UnionMeaningfulUsePoints);

        public int FindMemberIndex(string memberId)
        {
            for (var i = 0; i < Members.Count; i++)
                if (StringComparer.Ordinal.Equals(Members[i].MemberId, memberId)) return i;
            return -1;
        }

        private bool ContainsMember(string memberId) => FindMemberIndex(memberId) >= 0;
        private static IReadOnlyList<BattleMemberState> CopyMembers(IReadOnlyList<BattleMemberState> source)
        {
            var copy = new List<BattleMemberState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i] ?? throw new ArgumentException("Member cannot be null.", nameof(source)));
            return copy.AsReadOnly();
        }
        private static string Require(string value, string name) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Stable ID is required.", name) : value;
        private static int Range(int value, int low, int high, string name) =>
            value >= low && value <= high ? value : throw new ArgumentOutOfRangeException(name);
    }

    [Serializable]
    public sealed class BattlePlannedActionState
    {
        [JsonConstructor]
        public BattlePlannedActionState(
            string actorMemberId, string actorName, string targetUnionId, string targetMemberId,
            string artId, string artName, BattleActionKind kind, int sharedApCost, int personalMpCost,
            int predictedHpDelta, int predictedCohesionDelta, int predictedFormationDelta,
            string prediction, bool meaningfulUse, bool breakthroughOpportunity, string animationTag,
            string discipline = "", int predictedGrowth = 0,
            string breakthroughTargetArtId = "", string breakthroughTargetArtName = "",
            BattleAreaActionPlan095 areaActionPlan095 = null)
        {
            ActorMemberId = actorMemberId ?? string.Empty;
            ActorName = actorName ?? string.Empty;
            TargetUnionId = targetUnionId ?? string.Empty;
            TargetMemberId = targetMemberId ?? string.Empty;
            ArtId = artId ?? string.Empty;
            ArtName = artName ?? string.Empty;
            Kind = kind;
            SharedApCost = Math.Max(0, sharedApCost);
            PersonalMpCost = Math.Max(0, personalMpCost);
            PredictedHpDelta = predictedHpDelta;
            PredictedCohesionDelta = predictedCohesionDelta;
            PredictedFormationDelta = predictedFormationDelta;
            Prediction = prediction ?? string.Empty;
            MeaningfulUse = meaningfulUse;
            BreakthroughOpportunity = breakthroughOpportunity;
            AnimationTag = animationTag ?? string.Empty;
            Discipline = discipline ?? string.Empty;
            PredictedGrowth = Math.Max(0, predictedGrowth);
            BreakthroughTargetArtId = breakthroughTargetArtId ?? string.Empty;
            BreakthroughTargetArtName = breakthroughTargetArtName ?? string.Empty;
            AreaActionPlan095 = areaActionPlan095;
        }

        public string ActorMemberId { get; }
        public string ActorName { get; }
        public string TargetUnionId { get; }
        public string TargetMemberId { get; }
        public string ArtId { get; }
        public string ArtName { get; }
        public BattleActionKind Kind { get; }
        public int SharedApCost { get; }
        public int PersonalMpCost { get; }
        public int PredictedHpDelta { get; }
        public int PredictedCohesionDelta { get; }
        public int PredictedFormationDelta { get; }
        public string Prediction { get; }
        public bool MeaningfulUse { get; }
        public bool BreakthroughOpportunity { get; }
        public string AnimationTag { get; }
        public string Discipline { get; }
        public int PredictedGrowth { get; }
        public string BreakthroughTargetArtId { get; }
        public string BreakthroughTargetArtName { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public BattleAreaActionPlan095 AreaActionPlan095 { get; }
    }

    [Serializable]
    public sealed class BattleForecastState
    {
        [JsonConstructor]
        public BattleForecastState(
            string forecastId, string unionId, string commandId, string commandName, string phrase,
            string tacticalIntent, string targetId, string targetName, IReadOnlyList<BattlePlannedActionState> memberActions,
            int sharedApCost, int apRecovery, int combinedMpCost, string expectedEffect, string risk,
            string learningOpportunity, string fallbackBehavior, string generationIdentity, string deterministicDebugEvidence)
        {
            ForecastId = forecastId ?? string.Empty;
            UnionId = unionId ?? string.Empty;
            CommandId = commandId ?? string.Empty;
            CommandName = commandName ?? string.Empty;
            Phrase = phrase ?? string.Empty;
            TacticalIntent = tacticalIntent ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            TargetName = targetName ?? string.Empty;
            MemberActions = CopyActions(memberActions);
            SharedApCost = Math.Max(0, sharedApCost);
            ApRecovery = Math.Max(0, apRecovery);
            CombinedMpCost = Math.Max(0, combinedMpCost);
            ExpectedEffect = expectedEffect ?? string.Empty;
            Risk = risk ?? string.Empty;
            LearningOpportunity = learningOpportunity ?? string.Empty;
            FallbackBehavior = fallbackBehavior ?? string.Empty;
            GenerationIdentity = generationIdentity ?? string.Empty;
            DeterministicDebugEvidence = deterministicDebugEvidence ?? string.Empty;
        }

        public string ForecastId { get; }
        public string UnionId { get; }
        public string CommandId { get; }
        public string CommandName { get; }
        public string Phrase { get; }
        public string TacticalIntent { get; }
        public string TargetId { get; }
        public string TargetName { get; }
        public IReadOnlyList<BattlePlannedActionState> MemberActions { get; }
        public int SharedApCost { get; }
        public int ApRecovery { get; }
        public int CombinedMpCost { get; }
        public string ExpectedEffect { get; }
        public string Risk { get; }
        public string LearningOpportunity { get; }
        public string FallbackBehavior { get; }
        public string GenerationIdentity { get; }
        public string DeterministicDebugEvidence { get; }

        private static IReadOnlyList<BattlePlannedActionState> CopyActions(IReadOnlyList<BattlePlannedActionState> source)
        {
            var copy = new List<BattlePlannedActionState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }

    [Serializable]
    public sealed class BattleForecastSelectionState
    {
        [JsonConstructor]
        public BattleForecastSelectionState(string unionId, string forecastId)
        {
            UnionId = unionId ?? string.Empty;
            ForecastId = forecastId ?? string.Empty;
        }
        public string UnionId { get; }
        public string ForecastId { get; }
    }

    [Serializable]
    public sealed class BattleEventState
    {
        [JsonConstructor]
        public BattleEventState(int sequence, int round, string eventType, BattleSide side, string unionId,
            string memberId, string artId, string text, int amount, string stateHash,
            string actorUnionId = "", string actorMemberId = "",
            string targetUnionId = "", string targetMemberId = "")
        {
            Sequence = sequence;
            Round = round;
            EventType = eventType ?? string.Empty;
            Side = side;
            UnionId = unionId ?? string.Empty;
            MemberId = memberId ?? string.Empty;
            ArtId = artId ?? string.Empty;
            Text = text ?? string.Empty;
            Amount = amount;
            StateHash = stateHash ?? string.Empty;
            ActorUnionId = actorUnionId ?? string.Empty;
            ActorMemberId = actorMemberId ?? string.Empty;
            TargetUnionId = targetUnionId ?? string.Empty;
            TargetMemberId = targetMemberId ?? string.Empty;
        }
        public int Sequence { get; }
        public int Round { get; }
        public string EventType { get; }
        public BattleSide Side { get; }
        public string UnionId { get; }
        public string MemberId { get; }
        public string ArtId { get; }
        public string Text { get; }
        public int Amount { get; }
        public string StateHash { get; }
        public string ActorUnionId { get; }
        public string ActorMemberId { get; }
        public string TargetUnionId { get; }
        public string TargetMemberId { get; }
    }

    [Serializable]
    public sealed class BattleRoundRecordState
    {
        [JsonConstructor]
        public BattleRoundRecordState(int round, string preRoundStateHash,
            IReadOnlyList<BattleForecastSelectionState> selections, IReadOnlyList<BattleEventState> events,
            string deterministicTraceHash, string postRoundStateHash)
        {
            Round = round;
            PreRoundStateHash = preRoundStateHash ?? string.Empty;
            Selections = CopySelections(selections);
            Events = CopyEvents(events);
            DeterministicTraceHash = deterministicTraceHash ?? string.Empty;
            PostRoundStateHash = postRoundStateHash ?? string.Empty;
        }
        public int Round { get; }
        public string PreRoundStateHash { get; }
        public IReadOnlyList<BattleForecastSelectionState> Selections { get; }
        public IReadOnlyList<BattleEventState> Events { get; }
        public string DeterministicTraceHash { get; }
        public string PostRoundStateHash { get; }
        internal static IReadOnlyList<BattleForecastSelectionState> CopySelections(IReadOnlyList<BattleForecastSelectionState> source)
        {
            var copy = new List<BattleForecastSelectionState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
        internal static IReadOnlyList<BattleEventState> CopyEvents(IReadOnlyList<BattleEventState> source)
        {
            var copy = new List<BattleEventState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }

    [Serializable]
    public sealed class BattleMemberRewardState
    {
        [JsonConstructor]
        public BattleMemberRewardState(
            string memberId,
            string displayName,
            long personalXp,
            int previousLevel,
            int projectedLevel,
            int maximumHpGain,
            int maximumMpGain,
            int strengthGain,
            int defenseGain,
            int agilityGain,
            int magicGain,
            int willGain)
        {
            MemberId = string.IsNullOrWhiteSpace(memberId)
                ? throw new ArgumentException("Reward member ID is required.", nameof(memberId))
                : memberId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? MemberId : displayName;
            if (personalXp <= 0) throw new ArgumentOutOfRangeException(nameof(personalXp));
            if (previousLevel < 1) throw new ArgumentOutOfRangeException(nameof(previousLevel));
            if (projectedLevel < previousLevel) throw new ArgumentOutOfRangeException(nameof(projectedLevel));
            PersonalXp = personalXp;
            PreviousLevel = previousLevel;
            ProjectedLevel = projectedLevel;
            MaximumHpGain = NonNegative(maximumHpGain, nameof(maximumHpGain));
            MaximumMpGain = NonNegative(maximumMpGain, nameof(maximumMpGain));
            StrengthGain = NonNegative(strengthGain, nameof(strengthGain));
            DefenseGain = NonNegative(defenseGain, nameof(defenseGain));
            AgilityGain = NonNegative(agilityGain, nameof(agilityGain));
            MagicGain = NonNegative(magicGain, nameof(magicGain));
            WillGain = NonNegative(willGain, nameof(willGain));
        }

        public string MemberId { get; }
        public string DisplayName { get; }
        public long PersonalXp { get; }
        public int PreviousLevel { get; }
        public int ProjectedLevel { get; }
        public int LevelsGained => ProjectedLevel - PreviousLevel;
        public int MaximumHpGain { get; }
        public int MaximumMpGain { get; }
        public int StrengthGain { get; }
        public int DefenseGain { get; }
        public int AgilityGain { get; }
        public int MagicGain { get; }
        public int WillGain { get; }

        private static int NonNegative(int value, string parameter) =>
            value < 0 ? throw new ArgumentOutOfRangeException(parameter) : value;
    }

    /// <summary>
    /// Immutable terminal reward receipt. Claimed is intentionally excluded from
    /// the battle authority hash; amounts, participant projections, and the optional
    /// deterministic equipment item are not. Null item omission preserves older save hashes.
    /// </summary>
    [Serializable]
    public sealed class BattleRewardState
    {
        [JsonConstructor]
        public BattleRewardState(
            string rewardId,
            string rewardRulesVersion,
            BattleOutcome outcome,
            long basePersonalXpPerMember,
            long baseGuildTreasuryXp,
            int enemyUnionMultiplierPermille,
            int outcomeMultiplierPermille,
            int personalXpModePercent,
            int treasuryXpModePercent,
            long guildTreasuryXpAward,
            long hallEnhancementXpAward,
            IReadOnlyList<BattleMemberRewardState> memberRewards,
            bool claimed,
            EquipmentItemState equipmentReward = null)
        {
            RewardId = string.IsNullOrWhiteSpace(rewardId)
                ? throw new ArgumentException("Reward ID is required.", nameof(rewardId))
                : rewardId;
            RewardRulesVersion = string.IsNullOrWhiteSpace(rewardRulesVersion)
                ? throw new ArgumentException("Reward rules version is required.", nameof(rewardRulesVersion))
                : rewardRulesVersion;
            if (outcome == BattleOutcome.InProgress) throw new ArgumentOutOfRangeException(nameof(outcome));
            if (basePersonalXpPerMember <= 0) throw new ArgumentOutOfRangeException(nameof(basePersonalXpPerMember));
            if (baseGuildTreasuryXp <= 0) throw new ArgumentOutOfRangeException(nameof(baseGuildTreasuryXp));
            if (enemyUnionMultiplierPermille <= 0) throw new ArgumentOutOfRangeException(nameof(enemyUnionMultiplierPermille));
            if (outcomeMultiplierPermille <= 0) throw new ArgumentOutOfRangeException(nameof(outcomeMultiplierPermille));
            if (personalXpModePercent < 0) throw new ArgumentOutOfRangeException(nameof(personalXpModePercent));
            if (treasuryXpModePercent < 0) throw new ArgumentOutOfRangeException(nameof(treasuryXpModePercent));
            if (guildTreasuryXpAward <= 0) throw new ArgumentOutOfRangeException(nameof(guildTreasuryXpAward));
            if (hallEnhancementXpAward <= 0) throw new ArgumentOutOfRangeException(nameof(hallEnhancementXpAward));
            Outcome = outcome;
            BasePersonalXpPerMember = basePersonalXpPerMember;
            BaseGuildTreasuryXp = baseGuildTreasuryXp;
            EnemyUnionMultiplierPermille = enemyUnionMultiplierPermille;
            OutcomeMultiplierPermille = outcomeMultiplierPermille;
            PersonalXpModePercent = personalXpModePercent;
            TreasuryXpModePercent = treasuryXpModePercent;
            GuildTreasuryXpAward = guildTreasuryXpAward;
            HallEnhancementXpAward = hallEnhancementXpAward;
            MemberRewards = CopyMemberRewards(memberRewards);
            if (MemberRewards.Count == 0) throw new ArgumentException("At least one member reward is required.", nameof(memberRewards));
            Claimed = claimed;
            EquipmentReward = equipmentReward;
        }

        public string RewardId { get; }
        public string RewardRulesVersion { get; }
        public BattleOutcome Outcome { get; }
        public long BasePersonalXpPerMember { get; }
        public long BaseGuildTreasuryXp { get; }
        public int EnemyUnionMultiplierPermille { get; }
        public int OutcomeMultiplierPermille { get; }
        public int PersonalXpModePercent { get; }
        public int TreasuryXpModePercent { get; }
        public long GuildTreasuryXpAward { get; }
        public long HallEnhancementXpAward { get; }
        public IReadOnlyList<BattleMemberRewardState> MemberRewards { get; }
        public bool Claimed { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public EquipmentItemState EquipmentReward { get; }

        public BattleRewardState WithClaimed(bool claimed) =>
            new BattleRewardState(
                RewardId,
                RewardRulesVersion,
                Outcome,
                BasePersonalXpPerMember,
                BaseGuildTreasuryXp,
                EnemyUnionMultiplierPermille,
                OutcomeMultiplierPermille,
                PersonalXpModePercent,
                TreasuryXpModePercent,
                GuildTreasuryXpAward,
                HallEnhancementXpAward,
                MemberRewards,
                claimed,
                EquipmentReward);

        private static IReadOnlyList<BattleMemberRewardState> CopyMemberRewards(
            IReadOnlyList<BattleMemberRewardState> source)
        {
            var result = new List<BattleMemberRewardState>();
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    var value = source[index] ??
                        throw new ArgumentException("Member reward cannot be null.", nameof(source));
                    for (var existing = 0; existing < result.Count; existing++)
                        if (StringComparer.Ordinal.Equals(result[existing].MemberId, value.MemberId))
                            throw new ArgumentException("Member reward IDs must be unique.", nameof(source));
                    result.Add(value);
                }
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.MemberId, right.MemberId));
            return result.AsReadOnly();
        }
    }

    [Serializable]
    public sealed class TownBattleBonuses159
    {
        [JsonConstructor]
        public TownBattleBonuses159(long treasuryBasisPoints, long personalBasisPoints)
        {
            if (treasuryBasisPoints < 0 || personalBasisPoints < 0)
                throw new ArgumentOutOfRangeException("Town battle bonuses cannot be negative.");
            TreasuryBasisPoints = treasuryBasisPoints;
            PersonalBasisPoints = personalBasisPoints;
        }
        public long TreasuryBasisPoints { get; }
        public long PersonalBasisPoints { get; }
    }

    [Serializable]
    public sealed class BattleState
    {
        [JsonConstructor]
        public BattleState(
            string battleId, string contentVersion, int round, BattlePhase phase, BattleOutcome outcome,
            string objective, IReadOnlyList<BattleUnionState> playerUnions, IReadOnlyList<BattleUnionState> enemyUnions,
            IReadOnlyList<BattleForecastState> committedForecasts, IReadOnlyList<BattleForecastSelectionState> selections,
            IReadOnlyList<BattleEventState> eventLog, IReadOnlyList<BattleRoundRecordState> roundRecords,
            string forecastStateBasisHash, string initialBattleStateHash, string finalStateHash,
            string tutorialBreakthroughMemberId, string tutorialBreakthroughArtId, bool tutorialBreakthroughOccurred,
            BattleRewardState reward = null,
            string initialIntegrityStateHash090 = null,
            string finalIntegrityStateHash090 = null,
            SssBattleRuntimeState090 sssBattleRuntime090 = null,
            TownBattleBonuses159 townBonuses159 = null,
            SecondDimension.Gameplay.TitanTrials160.TitanBossRuntime161 titanRuntime161 = null,
            SecondDimension.Gameplay.TitanHeroes161.TitanHeroBattleRuntime161 titanHeroes161 = null,
            M2BattlePolicy163 progression163 = null)
        {
            BattleId = string.IsNullOrWhiteSpace(battleId) ? throw new ArgumentException("Battle ID is required.", nameof(battleId)) : battleId;
            ContentVersion = contentVersion ?? string.Empty;
            Round = Math.Max(1, round);
            Phase = phase;
            Outcome = outcome;
            Objective = objective ?? string.Empty;
            PlayerUnions = CopyUnions(playerUnions);
            EnemyUnions = CopyUnions(enemyUnions);
            CommittedForecasts = CopyForecasts(committedForecasts);
            Selections = BattleRoundRecordState.CopySelections(selections);
            EventLog = BattleRoundRecordState.CopyEvents(eventLog);
            RoundRecords = CopyRounds(roundRecords);
            ForecastStateBasisHash = forecastStateBasisHash ?? string.Empty;
            InitialBattleStateHash = initialBattleStateHash ?? string.Empty;
            FinalStateHash = finalStateHash ?? string.Empty;
            TutorialBreakthroughMemberId = tutorialBreakthroughMemberId ?? string.Empty;
            TutorialBreakthroughArtId = tutorialBreakthroughArtId ?? string.Empty;
            TutorialBreakthroughOccurred = tutorialBreakthroughOccurred;
            Reward = reward;
            InitialIntegrityStateHash090 = string.IsNullOrWhiteSpace(initialIntegrityStateHash090)
                ? null
                : initialIntegrityStateHash090;
            FinalIntegrityStateHash090 = string.IsNullOrWhiteSpace(finalIntegrityStateHash090)
                ? null
                : finalIntegrityStateHash090;
            SssBattleRuntime090 = sssBattleRuntime090;
            TownBonuses159 = townBonuses159;
            TitanRuntime161 = titanRuntime161;
            TitanHeroes161 = titanHeroes161;
            Progression163 = progression163;
            if(TitanRuntime161!=null&&TitanRuntime161.Attempt.BattleId!=BattleId)
                throw new ArgumentException("TITAN161_BATTLE_OWNER_MISMATCH");
        }

        public string BattleId { get; }
        public string ContentVersion { get; }
        public int Round { get; }
        public BattlePhase Phase { get; }
        public BattleOutcome Outcome { get; }
        public string Objective { get; }
        public IReadOnlyList<BattleUnionState> PlayerUnions { get; }
        public IReadOnlyList<BattleUnionState> EnemyUnions { get; }
        public IReadOnlyList<BattleForecastState> CommittedForecasts { get; }
        public IReadOnlyList<BattleForecastSelectionState> Selections { get; }
        public IReadOnlyList<BattleEventState> EventLog { get; }
        public IReadOnlyList<BattleRoundRecordState> RoundRecords { get; }
        public string ForecastStateBasisHash { get; }
        public string InitialBattleStateHash { get; }
        public string FinalStateHash { get; }
        public string TutorialBreakthroughMemberId { get; }
        public string TutorialBreakthroughArtId { get; }
        public bool TutorialBreakthroughOccurred { get; }
        public BattleRewardState Reward { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string InitialIntegrityStateHash090 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string FinalIntegrityStateHash090 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SssBattleRuntimeState090 SssBattleRuntime090 { get; }
        // Missing on historical battles means no town bonus. Never infer a
        // historical reward rate from today's facility levels during settlement.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TownBattleBonuses159 TownBonuses159 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecondDimension.Gameplay.TitanTrials160.TitanBossRuntime161 TitanRuntime161 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecondDimension.Gameplay.TitanHeroes161.TitanHeroBattleRuntime161 TitanHeroes161 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public M2BattlePolicy163 Progression163 { get; }

        public BattleState With(
            int? round = null, BattlePhase? phase = null, BattleOutcome? outcome = null,
            IReadOnlyList<BattleUnionState> playerUnions = null, IReadOnlyList<BattleUnionState> enemyUnions = null,
            IReadOnlyList<BattleForecastState> committedForecasts = null,
            IReadOnlyList<BattleForecastSelectionState> selections = null,
            IReadOnlyList<BattleEventState> eventLog = null, IReadOnlyList<BattleRoundRecordState> roundRecords = null,
            string forecastStateBasisHash = null, string initialBattleStateHash = null, string finalStateHash = null,
            bool? tutorialBreakthroughOccurred = null,
            string initialIntegrityStateHash090 = null,
            string finalIntegrityStateHash090 = null,
            SssBattleRuntimeState090 sssBattleRuntime090 = null,
            SecondDimension.Gameplay.TitanTrials160.TitanBossRuntime161 titanRuntime161 = null,
            SecondDimension.Gameplay.TitanHeroes161.TitanHeroBattleRuntime161 titanHeroes161 = null) =>
            new BattleState(
                BattleId, ContentVersion, round ?? Round, phase ?? Phase, outcome ?? Outcome, Objective,
                playerUnions ?? PlayerUnions, enemyUnions ?? EnemyUnions,
                committedForecasts ?? CommittedForecasts, selections ?? Selections,
                eventLog ?? EventLog, roundRecords ?? RoundRecords,
                forecastStateBasisHash ?? ForecastStateBasisHash,
                initialBattleStateHash ?? InitialBattleStateHash,
                finalStateHash ?? FinalStateHash,
                TutorialBreakthroughMemberId, TutorialBreakthroughArtId,
                tutorialBreakthroughOccurred ?? TutorialBreakthroughOccurred,
                Reward,
                initialIntegrityStateHash090 ?? InitialIntegrityStateHash090,
                finalIntegrityStateHash090 ?? FinalIntegrityStateHash090,
                sssBattleRuntime090 ?? SssBattleRuntime090,
                TownBonuses159, titanRuntime161 ?? TitanRuntime161, titanHeroes161 ?? TitanHeroes161, Progression163);

        public BattleState WithReward(BattleRewardState reward) =>
            new BattleState(
                BattleId, ContentVersion, Round, Phase, Outcome, Objective,
                PlayerUnions, EnemyUnions, CommittedForecasts, Selections,
                EventLog, RoundRecords, ForecastStateBasisHash, InitialBattleStateHash, FinalStateHash,
                TutorialBreakthroughMemberId, TutorialBreakthroughArtId, TutorialBreakthroughOccurred,
                reward,
                InitialIntegrityStateHash090,
                FinalIntegrityStateHash090,
                SssBattleRuntime090,
                TownBonuses159, TitanRuntime161, TitanHeroes161, Progression163);

        private static IReadOnlyList<BattleUnionState> CopyUnions(IReadOnlyList<BattleUnionState> source)
        {
            var copy = new List<BattleUnionState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
        private static IReadOnlyList<BattleForecastState> CopyForecasts(IReadOnlyList<BattleForecastState> source)
        {
            var copy = new List<BattleForecastState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
        private static IReadOnlyList<BattleRoundRecordState> CopyRounds(IReadOnlyList<BattleRoundRecordState> source)
        {
            var copy = new List<BattleRoundRecordState>();
            if (source != null) for (var i = 0; i < source.Count; i++) copy.Add(source[i]);
            return copy.AsReadOnly();
        }
    }
}
