using System;
using Newtonsoft.Json;
using SecondDimension.Core;

namespace SecondDimension.Gameplay.State
{
    [Serializable]
    public sealed class ModeRuleSnapshot
    {
        public ModeRuleSnapshot(GameMode mode, bool permanentDeathEnabled, bool departureEnabled)
            : this(
                mode,
                permanentDeathEnabled,
                departureEnabled,
                100,
                100,
                100,
                100,
                100,
                100,
                100,
                "standard",
                100,
                100,
                100,
                0,
                100,
                0,
                25,
                "partial",
                100,
                null,
                null,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false)
        {
        }

        public ModeRuleSnapshot(
            GameMode mode,
            bool permanentDeathEnabled,
            bool departureEnabled,
            int? personalXpPct,
            int? treasuryXpPct,
            int? artGrowthPct,
            int? unionGrowthPct,
            int? enemyHpPct,
            int? enemyDamagePct,
            int? enemyCohesionPct,
            string enemyAiTier,
            int? injuryPct,
            int? recoveryPct,
            int? recruitRefreshPct,
            int? signatureBonusPoints,
            int? lootPct,
            int? craftingAssist,
            int? relationshipDecayPct,
            string learningVisibility,
            int? sustainabilityStrictnessPct,
            long? startingTreasuryXp,
            string startingEquipmentQuality,
            bool storyAuthorityBypassEnabled,
            bool consentBypassEnabled,
            bool protectedActorBypassEnabled)
            : this(
                mode,
                permanentDeathEnabled,
                departureEnabled,
                personalXpPct,
                treasuryXpPct,
                artGrowthPct,
                unionGrowthPct,
                enemyHpPct,
                enemyDamagePct,
                enemyCohesionPct,
                enemyAiTier,
                injuryPct,
                recoveryPct,
                recruitRefreshPct,
                signatureBonusPoints,
                lootPct,
                craftingAssist,
                relationshipDecayPct,
                learningVisibility,
                sustainabilityStrictnessPct,
                startingTreasuryXp,
                startingEquipmentQuality,
                false,
                false,
                false,
                false,
                false,
                false,
                storyAuthorityBypassEnabled,
                consentBypassEnabled,
                protectedActorBypassEnabled)
        {
        }

        [JsonConstructor]
        public ModeRuleSnapshot(
            GameMode mode,
            bool permanentDeathEnabled,
            bool departureEnabled,
            int? personalXpPct,
            int? treasuryXpPct,
            int? artGrowthPct,
            int? unionGrowthPct,
            int? enemyHpPct,
            int? enemyDamagePct,
            int? enemyCohesionPct,
            string enemyAiTier,
            int? injuryPct,
            int? recoveryPct,
            int? recruitRefreshPct,
            int? signatureBonusPoints,
            int? lootPct,
            int? craftingAssist,
            int? relationshipDecayPct,
            string learningVisibility,
            int? sustainabilityStrictnessPct,
            long? startingTreasuryXp,
            string startingEquipmentQuality,
            bool showAllLearningConditions,
            bool noTrivialEnemyDiminishingReturns,
            bool freeUnionReorganization,
            bool allowBothArtBranches,
            bool instantConstruction,
            bool freeConstruction,
            bool storyAuthorityBypassEnabled,
            bool consentBypassEnabled,
            bool protectedActorBypassEnabled)
        {
            if (storyAuthorityBypassEnabled || consentBypassEnabled || protectedActorBypassEnabled)
            {
                throw new ArgumentException("A game mode cannot bypass story, consent, or protected-actor authority.");
            }

            Mode = mode;
            PermanentDeathEnabled = permanentDeathEnabled;
            DepartureEnabled = departureEnabled;
            PersonalXpPct = RequireNonNegative(personalXpPct ?? 100, nameof(personalXpPct));
            TreasuryXpPct = RequireNonNegative(treasuryXpPct ?? 100, nameof(treasuryXpPct));
            ArtGrowthPct = RequireNonNegative(artGrowthPct ?? 100, nameof(artGrowthPct));
            UnionGrowthPct = RequireNonNegative(unionGrowthPct ?? 100, nameof(unionGrowthPct));
            EnemyHpPct = RequireNonNegative(enemyHpPct ?? 100, nameof(enemyHpPct));
            EnemyDamagePct = RequireNonNegative(enemyDamagePct ?? 100, nameof(enemyDamagePct));
            EnemyCohesionPct = RequireNonNegative(enemyCohesionPct ?? 100, nameof(enemyCohesionPct));
            EnemyAiTier = RequireText(enemyAiTier ?? "standard", nameof(enemyAiTier));
            InjuryPct = RequireNonNegative(injuryPct ?? 100, nameof(injuryPct));
            RecoveryPct = RequireNonNegative(recoveryPct ?? 100, nameof(recoveryPct));
            RecruitRefreshPct = RequireNonNegative(recruitRefreshPct ?? 100, nameof(recruitRefreshPct));
            SignatureBonusPoints = RequireNonNegative(signatureBonusPoints ?? 0, nameof(signatureBonusPoints));
            LootPct = RequireNonNegative(lootPct ?? 100, nameof(lootPct));
            CraftingAssist = RequireNonNegative(craftingAssist ?? 0, nameof(craftingAssist));
            RelationshipDecayPct = RequireNonNegative(relationshipDecayPct ?? 25, nameof(relationshipDecayPct));
            LearningVisibility = RequireText(learningVisibility ?? "partial", nameof(learningVisibility));
            SustainabilityStrictnessPct = RequireNonNegative(
                sustainabilityStrictnessPct ?? 100,
                nameof(sustainabilityStrictnessPct));
            if (startingTreasuryXp.HasValue && startingTreasuryXp.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingTreasuryXp));
            }
            StartingTreasuryXp = startingTreasuryXp;
            StartingEquipmentQuality = startingEquipmentQuality;
            ShowAllLearningConditions = showAllLearningConditions;
            NoTrivialEnemyDiminishingReturns = noTrivialEnemyDiminishingReturns;
            FreeUnionReorganization = freeUnionReorganization;
            AllowBothArtBranches = allowBothArtBranches;
            InstantConstruction = instantConstruction;
            FreeConstruction = freeConstruction;
            StoryAuthorityBypassEnabled = false;
            ConsentBypassEnabled = false;
            ProtectedActorBypassEnabled = false;
        }

        public GameMode Mode { get; }
        public bool PermanentDeathEnabled { get; }
        public bool DepartureEnabled { get; }
        public int PersonalXpPct { get; }
        public int TreasuryXpPct { get; }
        public int ArtGrowthPct { get; }
        public int UnionGrowthPct { get; }
        public int EnemyHpPct { get; }
        public int EnemyDamagePct { get; }
        public int EnemyCohesionPct { get; }
        public string EnemyAiTier { get; }
        public int InjuryPct { get; }
        public int RecoveryPct { get; }
        public int RecruitRefreshPct { get; }
        public int SignatureBonusPoints { get; }
        public int LootPct { get; }
        public int CraftingAssist { get; }
        public int RelationshipDecayPct { get; }
        public string LearningVisibility { get; }
        public int SustainabilityStrictnessPct { get; }
        public long? StartingTreasuryXp { get; }
        public string StartingEquipmentQuality { get; }
        public bool ShowAllLearningConditions { get; }
        public bool NoTrivialEnemyDiminishingReturns { get; }
        public bool FreeUnionReorganization { get; }
        public bool AllowBothArtBranches { get; }
        public bool InstantConstruction { get; }
        public bool FreeConstruction { get; }

        // These explicit, persisted locks make it impossible for OP Start or a future
        // custom preset to silently acquire canon/consent privileges.
        public bool StoryAuthorityBypassEnabled { get; }
        public bool ConsentBypassEnabled { get; }
        public bool ProtectedActorBypassEnabled { get; }

        public static ModeRuleSnapshot StandardDefaults() =>
            new ModeRuleSnapshot(GameMode.Standard, permanentDeathEnabled: false, departureEnabled: false);

        private static int RequireNonNegative(int value, string parameter)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(parameter);
            return value;
        }

        private static string RequireText(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Rule text is required.", parameter)
                : value;
    }
}
