using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.M1;

namespace SecondDimension.Presentation
{
    public enum M1Screen
    {
        MainMenu,
        NewGuild,
        ApplicantBoard,
        RecruitDetail,
        Equipment,
        UnionBuilder,
        Complete,
        GuildOperations,
        Battle,
        BattleResults,
        FirstHourOpening,
        FirstHourGuildReady
    }

    public sealed class M1CommandResult
    {
        private M1CommandResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        public LootRewardView092 LootReward092 { get; set; }

        public static M1CommandResult Success(string message = "") => new M1CommandResult(true, message);
        public static M1CommandResult Failure(string message) => new M1CommandResult(false, message);
    }

    public sealed class LootRewardView092
    {
        public string ItemName { get; set; }
        public string ItemVisualId { get; set; }
        public string RarityId { get; set; }
        public string Summary { get; set; }
        public bool IsCommitted { get; set; }
        public bool WasAutoEquipped { get; set; }
        public string RecipientName { get; set; }
        public int PhysicalGain { get; set; }
        public int MysticGain { get; set; }
    }

    public sealed class M1NewGuildIntent
    {
        public string GuildmasterName { get; set; }
        public string ModeId { get; set; }
        public string TutorialDepthId { get; set; }
        public float TextScale { get; set; } = 1f;
        public bool HighContrast { get; set; }
        public bool ReducedMotion { get; set; }
    }

    public sealed class M1ModeView
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Summary { get; set; }
        public bool RequiresPermanentConsequenceConfirmation { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public sealed class M1ApplicantView
    {
        public string RecruitId { get; set; }
        public string RaceId { get; set; }
        public string VisualSeed { get; set; }
        public string PortraitAuthorityId { get; set; }
        public string DisplayName { get; set; }
        public string RaceAndWorld { get; set; }
        public string ObservedClass { get; set; }
        public string ClassSymbol { get; set; }
        public string GearSummary { get; set; }
        public string ScoutObservations { get; set; }
        public string LeadershipBand { get; set; }
        public string PersonalityClues { get; set; }
        public string SigningCost { get; set; }
        public string Ambition { get; set; }
        public bool IsSigned { get; set; }
    }

    public sealed class M1EquipmentChoiceView
    {
        // All fields are value types or immutable strings. Each caller receives
        // its own editable view even when display facts are reused locally.
        internal M1EquipmentChoiceView Copy110() => (M1EquipmentChoiceView)MemberwiseClone();
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public string VisualGlyph { get; set; }
        public string EquipmentVisualId { get; set; }
        public string QualityId { get; set; }
        public string RarityTierId { get; set; }
        public string RarityDisplayName { get; set; }
        public string DirectChange { get; set; }
        public string ForecastBehavior { get; set; }
        public int PhysicalAttackBonus { get; set; }
        public int MysticAttackBonus { get; set; }
        public bool IsEquipped { get; set; }
        public bool IsLegal { get; set; } = true;
        public string LegalityReason { get; set; }
    }

    public sealed class M1EquipmentSlotView
    {
        public string SlotId { get; set; }
        public string DisplayName { get; set; }
        public string EquippedItemId { get; set; }
        public string EquippedItemName { get; set; }
        public string VisualGlyph { get; set; }
        public string EquippedVisualId { get; set; }
        public string EquippedQualityId { get; set; }
        public string EquippedRarityTierId { get; set; }
        public string EquippedRarityDisplayName { get; set; }
        public int EquippedPhysicalAttackBonus { get; set; }
        public int EquippedMysticAttackBonus { get; set; }
        public bool IsLocked { get; set; }
        public bool IsLegal { get; set; }
        public IReadOnlyList<M1EquipmentChoiceView> Choices { get; set; } = Array.Empty<M1EquipmentChoiceView>();
    }

    public sealed class M1RecruitLoadoutView
    {
        public string RecruitId { get; set; }
        public string RaceId { get; set; }
        public string VisualSeed { get; set; }
        public string PortraitAuthorityId { get; set; }
        public string DisplayName { get; set; }
        public string ObservedClass { get; set; }
        public string ClassSymbol { get; set; }
        public int Level { get; set; } = 1;
        public long TotalPersonalXp { get; set; }
        public long XpIntoCurrentLevel { get; set; }
        public long XpRequiredForNextLevel { get; set; }
        public bool HasManualEquipAction { get; set; }
        public bool IsLegal { get; set; }
        public int MaximumHp { get; set; }
        public int MaximumMp { get; set; }
        public int MaximumHpBonus { get; set; }
        public int MaximumMpBonus { get; set; }
        public int StrengthIndex { get; set; }
        public int MagicIndex { get; set; }
        public int DefenseIndex { get; set; }
        public int AgilityIndex { get; set; }
        public int WillIndex { get; set; }
        public int StrengthBonus { get; set; }
        public int DefenseBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int MagicBonus { get; set; }
        public int WillBonus { get; set; }
        public int PhysicalAttack { get; set; }
        public int MysticAttack { get; set; }
        public string EquipmentCombatFamily { get; set; }
        public string ArtsAccessSummary { get; set; }
        public IReadOnlyList<string> LearnedArtIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<M1ArtMasteryView> ArtMastery { get; set; } = Array.Empty<M1ArtMasteryView>();
        public IReadOnlyList<M1EquipmentSlotView> Slots { get; set; } = Array.Empty<M1EquipmentSlotView>();
        public bool IsSssHero { get; set; }
        public string SssHeroId { get; set; }
        public string SssRole { get; set; }
        public int AscensionLevel { get; set; }
        public int AscensionCredits { get; set; }
        public int SssHuntFamiliesCompleted { get; set; }
        public string SssHuntRemainingDefeats { get; set; }
        public string SssSignatureWeaponName { get; set; }
        public string SssSignatureWeaponArtResourcePath { get; set; }
        public bool SssSignatureEffectReady { get; set; }
        public string SssSignatureReadiness { get; set; }
        public string SssIdleArtResourcePath { get; set; }
        public string SssAttackArtResourcePath { get; set; }
        public string SssPortraitArtResourcePath { get; set; }
    }

    public sealed class M1ArtMasteryView
    {
        public string ArtId { get; set; }
        public string DisplayName { get; set; }
        public string Discipline { get; set; }
        public int MeaningfulUses { get; set; }
        public int MasteryPoints { get; set; }
    }

    public sealed class M1FacilityProgressionView
    {
        public string FacilityId { get; set; }
        public string DisplayName { get; set; }
        public int Level { get; set; }
        public long TotalFacilityXp { get; set; }
    }

    public sealed class M1ChoiceView
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Summary { get; set; }
    }

    public sealed class M1UnionView
    {
        public int Index { get; set; }
        public string UnionId { get; set; }
        public string DisplayName { get; set; }
        public IReadOnlyList<string> MemberRecruitIds { get; set; } = Array.Empty<string>();
        public string LeaderRecruitId { get; set; }
        public string FormationId { get; set; }
        public string DoctrineId { get; set; }
        public int SharedAp { get; set; }
        public int CombinedCurrentHp { get; set; }
        public int CombinedMaximumHp { get; set; }
        public int CombinedMp { get; set; }
        public int CombinedMaximumMp { get; set; }
        public int CombinedAttack { get; set; }
        public int CombinedMagicAttack { get; set; }
        public int CombinedDefense { get; set; }
        public int CombinedAgility { get; set; }
        public int CombinedWill { get; set; }
        public int BaseCohesionBasisPoints { get; set; }
        public int FormationCohesionRuleBasisPoints { get; set; }
        public int CohesionBasisPoints { get; set; }
        public bool IsLegal { get; set; }
        public string LegalitySummary { get; set; }
        public string ExpectedTendencies { get; set; }
    }

    public sealed class M1PresentationState
    {
        public bool HasCampaign { get; set; }
        public bool HasSave { get; set; }
        public M1Screen ResumeScreen { get; set; } = M1Screen.NewGuild;
        public string GuildmasterName { get; set; }
        public string SelectedModeId { get; set; } = "Standard";
        public int GuildLevel { get; set; } = 1;
        public long LifetimeGuildXp { get; set; }
        public long GuildXpIntoCurrentLevel { get; set; }
        public long GuildXpRequiredForNextLevel { get; set; }
        public long TreasuryXp { get; set; }
        public int HallStageIndex { get; set; }
        public string HallStageId { get; set; }
        public string HallStageName { get; set; }
        public long HallEnhancementXp { get; set; }
        public IReadOnlyList<M1FacilityProgressionView> Facilities { get; set; } = Array.Empty<M1FacilityProgressionView>();
        public IReadOnlyList<M1ModeView> Modes { get; set; } = Array.Empty<M1ModeView>();
        public IReadOnlyList<M1ApplicantView> Applicants { get; set; } = Array.Empty<M1ApplicantView>();
        public IReadOnlyList<M1RecruitLoadoutView> Recruits { get; set; } = Array.Empty<M1RecruitLoadoutView>();
        public IReadOnlyList<M1UnionView> Unions { get; set; } = Array.Empty<M1UnionView>();
        public IReadOnlyList<M1ChoiceView> Formations { get; set; } = Array.Empty<M1ChoiceView>();
        public IReadOnlyList<M1ChoiceView> Doctrines { get; set; } = Array.Empty<M1ChoiceView>();
        public bool AllSixSigned { get; set; }
        public bool OpeningEquipmentLegal { get; set; }
        public bool OpeningUnionsLegal { get; set; }
        public bool TwoUnionsLegal { get; set; }
        public int UsedUnionCount { get; set; }
        public int MaximumUnionPlanCount { get; set; } = NormalUnionPlanRules.MaximumPlanCount;
        public bool SaveReloadVerified { get; set; }
        public string CanonicalStateHash { get; set; }
        public string StatusMessage { get; set; }
        /// <summary>
        /// Non-blocking details for a save that could not be restored at startup.
        /// The title keeps this behind an explicit details action so a stale local
        /// save never replaces the player's first story instruction with an error.
        /// </summary>
        public string SaveRecoveryDiagnostic { get; set; }
        public M2BattleView Battle { get; set; }
    }

    /// <summary>
    /// Presentation-facing adapter. An implementation wraps pure Gameplay/Save services;
    /// the UI never mutates authoritative records directly.
    /// </summary>
    public interface IM1PresentationCoordinator
    {
        event Action Changed;

        M1PresentationState State { get; }

        M1CommandResult CreateGuild(M1NewGuildIntent intent);
        M1CommandResult SignRecruit(string recruitId);
        M1CommandResult EquipItem(string recruitId, string slotId, string itemId);
        M1CommandResult UnequipItem(string recruitId, string slotId);
        M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked);
        M1CommandResult CompleteEquipmentReview();
        M1CommandResult AddUnion();
        M1CommandResult RemoveUnion(int unionIndex);
        M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex);
        M1CommandResult UnassignRecruitFromUnion(string recruitId);
        M1CommandResult SetUnionLeader(int unionIndex, string recruitId);
        M1CommandResult SetFormation(int unionIndex, string formationId);
        M1CommandResult SetDoctrine(int unionIndex, string doctrineId);
        M1CommandResult SaveAndReloadProof();
    }

    /// <summary>
    /// Optional capability exposed by the shipping coordinator. Keeping this
    /// separate preserves compatibility with narrow test and tool coordinators.
    /// </summary>
    public interface ISuggestedUnionPresentationCoordinator087
    {
        M1CommandResult ApplySuggestedRoleUnions087();
    }

    public sealed class M2BattleView
    {
        public string BattleId { get; set; }
        public int Round { get; set; }
        public string Outcome { get; set; }
        public string Objective { get; set; }
        public IReadOnlyList<M2BattleUnionView> PlayerUnions { get; set; } = Array.Empty<M2BattleUnionView>();
        public IReadOnlyList<M2BattleUnionView> EnemyUnions { get; set; } = Array.Empty<M2BattleUnionView>();
        public IReadOnlyList<M2ForecastView> Forecasts { get; set; } = Array.Empty<M2ForecastView>();
        public IReadOnlyList<M2BattleEventView> Events { get; set; } = Array.Empty<M2BattleEventView>();
        public IReadOnlyList<M2BattleEventView> RecentEvents { get; set; } = Array.Empty<M2BattleEventView>();
        public int LastResolvedRound { get; set; }
        public IReadOnlyList<M2BattleEventView> LastResolvedRoundEvents { get; set; } = Array.Empty<M2BattleEventView>();
        public bool CanConfirmRound { get; set; }
        public bool IsResolved { get; set; }
        public bool TutorialBreakthroughOccurred { get; set; }
        public string TutorialBreakthroughSummary { get; set; }
        public M2BattleRewardView Reward { get; set; }
        public string StateHash { get; set; }
        public string FinalStateHash { get; set; }
    }

    public sealed class M2BattleRewardView
    {
        public string TitanRewardSummary161 { get; set; }
        public string TitanRewardHeroId161 { get; set; }
        public string RewardId { get; set; }
        public string RewardRulesVersion { get; set; }
        public string Outcome { get; set; }
        public bool Claimed { get; set; }
        public bool CanClaim { get; set; }
        public long BasePersonalXpPerMember { get; set; }
        public long BaseGuildTreasuryXp { get; set; }
        public int EnemyUnionMultiplierPermille { get; set; }
        public int OutcomeMultiplierPermille { get; set; }
        public int PersonalXpModePercent { get; set; }
        public int TreasuryXpModePercent { get; set; }
        public long GuildTreasuryXpAward { get; set; }
        public long HallEnhancementXpAward { get; set; }
        public long GuildXpBefore { get; set; }
        public long GuildXpAfter { get; set; }
        public int GuildPreviousLevel { get; set; }
        public int GuildProjectedLevel { get; set; }
        public int GuildLevelsGained { get; set; }
        public string EquipmentRewardInstanceId { get; set; }
        public string EquipmentRewardDefinitionId { get; set; }
        public string EquipmentRewardDisplayName { get; set; }
        public string EquipmentRewardQualityId { get; set; }
        public IReadOnlyList<string> EquipmentRewardValidSlotIds { get; set; } = Array.Empty<string>();
        public IReadOnlyList<M2BattleMemberRewardView> MemberRewards { get; set; } = Array.Empty<M2BattleMemberRewardView>();
    }

    public sealed class M2BattleMemberRewardView
    {
        public string MemberId { get; set; }
        public string DisplayName { get; set; }
        public long PersonalXp { get; set; }
        public int PreviousLevel { get; set; }
        public int ProjectedLevel { get; set; }
        public int LevelsGained { get; set; }
        public int MaximumHpGain { get; set; }
        public int MaximumMpGain { get; set; }
        public int StrengthGain { get; set; }
        public int DefenseGain { get; set; }
        public int AgilityGain { get; set; }
        public int MagicGain { get; set; }
        public int WillGain { get; set; }
    }

    public sealed class M2BattleUnionView
    {
        public string UnionId { get; set; }
        public string DisplayName { get; set; }
        public string Side { get; set; }
        public string LeaderMemberId { get; set; }
        public string Formation { get; set; }
        public bool FormationBenefitActive { get; set; }
        public string FormationStatus { get; set; }
        public int CurrentAp { get; set; }
        public int MaximumAp { get; set; }
        public int Cohesion { get; set; }
        public int FormationConditionPercent { get; set; }
        public string Engagement { get; set; }
        public bool CanAct { get; set; }
        public bool IsSelected { get; set; }
        public string SelectedForecastId { get; set; }
        public int UnionDisciplinePoints { get; set; }
        public IReadOnlyList<M2BattleMemberView> Members { get; set; } = Array.Empty<M2BattleMemberView>();
    }

    /// <summary>
    /// Read-only projection of the next authority-backed Art/technique gate. These
    /// values describe progression already present in battle state and content; the
    /// presentation never awards points or unlocks the target.
    /// </summary>
    public sealed class M2BattleSkillProgressView
    {
        public string SkillId { get; set; }
        public string DisplayName { get; set; }
        public string ProgressKind { get; set; }
        public int CurrentPoints { get; set; }
        public int RequiredPoints { get; set; }
        public int RequiredLevel { get; set; }
        public bool LevelGateMet { get; set; }
    }

    public sealed class M2BattleMemberView
    {
        public string MemberId { get; set; }
        public string DisplayName { get; set; }
        public string ClassName { get; set; }
        public string ClassSymbol { get; set; }
        public string RaceId { get; set; }
        public string VisualSeed { get; set; }
        public string PortraitAuthorityId { get; set; }
        public string EnemyArtBaseId090 { get; set; }
        public string EnemyArtVariantId090 { get; set; }
        public int EnemyThreatTier089 { get; set; }
        public int CurrentHp { get; set; }
        public int MaximumHp { get; set; }
        public int CurrentMp { get; set; }
        public int MaximumMp { get; set; }
        public bool Downed { get; set; }
        public bool Stabilized { get; set; }
        public int MeaningfulUsePoints { get; set; }
        public string ArtGrowthSummary { get; set; }
        public M2BattleSkillProgressView NextSkillProgress { get; set; }
        public IReadOnlyList<string> EquipmentTags { get; set; } = Array.Empty<string>();
        public string EquipmentVisualFamily { get; set; }
        public string EquipmentAnimatorSet { get; set; }
    }

    public sealed class M2ForecastView
    {
        public string ForecastId { get; set; }
        public string UnionId { get; set; }
        public string CommandId { get; set; }
        public string CommandName { get; set; }
        public string Phrase { get; set; }
        public string TacticalIntent { get; set; }
        public string TargetId { get; set; }
        public string TargetName { get; set; }
        public int SharedApCost { get; set; }
        public int ApRecovery { get; set; }
        public int CombinedMpCost { get; set; }
        public string ExpectedEffect { get; set; }
        public string Risk { get; set; }
        public string LearningOpportunity { get; set; }
        public string FallbackBehavior { get; set; }
        public bool IsSelected { get; set; }
        public IReadOnlyList<M2PredictedActionView> MemberActions { get; set; } = Array.Empty<M2PredictedActionView>();
    }

    public sealed class M2PredictedDamageRecipient097
    {
        public string UnionId { get; set; }
        public string MemberId { get; set; }
        public int PredictedHpLoss { get; set; }
    }

    public sealed class M2PredictedActionView
    {
        public string ActorMemberId { get; set; }
        public string ActorName { get; set; }
        public string ArtId { get; set; }
        public string ArtName { get; set; }
        public string Discipline { get; set; }
        public string ActionKind { get; set; }
        public string TargetUnionId { get; set; }
        public string TargetMemberId { get; set; }
        public string TargetName { get; set; }
        public int SharedApCost { get; set; }
        public int PersonalMpCost { get; set; }
        public int PredictedGrowth { get; set; }
        // Read-only committed numeric preview; Auto never parses display text or
        // recalculates Art damage. This DTO is not part of canonical save state.
        public int PredictedHpDelta097 { get; set; }
        public IReadOnlyList<M2PredictedDamageRecipient097> DamageRecipients097 { get; set; } = Array.Empty<M2PredictedDamageRecipient097>();
        public int AreaTargetCount095 { get; set; }
        public string[] AreaUnionIds095 { get; set; } = Array.Empty<string>();
        public int AreaPredictedHpLoss095 { get; set; }
        public int ArtLevel { get; set; }
        public int ArtLevelProgressBasisPoints { get; set; }
        public string ArtPowerCue { get; set; }
        public string Prediction { get; set; }
        // Read-only Art authority, used to prioritize complete rescue Forecasts.
        public bool IsRevival091 { get; set; }
        public bool BreakthroughOpportunity { get; set; }
        public string BreakthroughTargetArtName { get; set; }
        public string AnimationTag { get; set; }
    }

    public sealed class M2BattleEventView
    {
        public int Sequence { get; set; }
        public int Round { get; set; }
        public string EventType { get; set; }
        public string Text { get; set; }
        public string Side { get; set; }
        public string UnionId { get; set; }
        public string MemberId { get; set; }
        public string ArtId { get; set; }
        public int Amount { get; set; }
        public string ActorUnionId { get; set; }
        public string ActorMemberId { get; set; }
        public string TargetUnionId { get; set; }
        public string TargetMemberId { get; set; }
    }

    /// <summary>
    /// M2 extends the approved M1 adapter with high-level complete-Forecast commands.
    /// It intentionally exposes no operation for selecting a member's exact Art.
    /// </summary>
    public interface IM2PresentationCoordinator : IM1PresentationCoordinator
    {
        M1CommandResult StartTutorialBattle();
        M1CommandResult SelectForecast(string unionId, string forecastId);
        M1CommandResult ConfirmBattleRound();
        M1CommandResult ReplayTutorialBattle();
        M1CommandResult RetryTutorialBattle();
        M1CommandResult ClaimBattleRewards();
    }

    public sealed class M2AutoForecastSelection108
    {
        public string UnionId { get; set; }
        public string ForecastId { get; set; }
    }

    /// <summary>
    /// Commits one complete Auto round plan atomically. Every Forecast is still
    /// revalidated by the authoritative battle service before the single save.
    /// </summary>
    public interface IM2BattleAutoPlanCoordinator108
    {
        M1CommandResult SelectAutoForecastPlan108(
            IReadOnlyList<M2AutoForecastSelection108> selections);
    }

    public interface IM2BattleAutoRoundCoordinator108
    {
        M1CommandResult ResolveAutoForecastPlan108(
            IReadOnlyList<M2AutoForecastSelection108> selections);
    }
}
