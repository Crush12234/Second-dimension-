using System;
using SecondDimension.Core;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M1
{
    /// <summary>
    /// Frozen PASS10 game-mode presets. Values are copied into CampaignState so
    /// later content changes never alter an existing campaign.
    /// </summary>
    public static class ModeCatalog
    {
        public static ModeRuleSnapshot GetPreset(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Relaxed:
                    return Create(
                        mode, false, false, 150, 150, 150, 150, 85, 85, 90,
                        "forgiving", 70, 150, 125, 2, 115, 10, 0, "full", 80, null, null);

                case GameMode.Standard:
                    return Create(
                        mode, false, false, 100, 100, 100, 100, 100, 100, 100,
                        "standard", 100, 100, 100, 0, 100, 0, 0, "partial", 100, null, null);

                case GameMode.Iron:
                    return Create(
                        mode, false, false, 100, 100, 100, 100, 115, 115, 115,
                        "assertive", 160, 65, 100, 0, 105, 0, 0, "partial", 115, null, null);

                case GameMode.OverpoweredStart:
                    return Create(
                        mode, false, false, 500, 500, 500, 500, 85, 85, 90,
                        "standard", 60, 200, 150, 4, 150, 15, 0, "full", 80, 12000, "reinforced");

                case GameMode.Custom:
                    throw new ArgumentException(
                        "Custom mode requires an explicitly validated ModeRuleSnapshot; it has no implicit preset.",
                        nameof(mode));

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static bool RequiresVisibleConsequenceConfirmation(GameMode mode) => mode == GameMode.Iron;

        private static ModeRuleSnapshot Create(
            GameMode mode,
            bool death,
            bool departure,
            int personalXpPct,
            int treasuryXpPct,
            int artGrowthPct,
            int unionGrowthPct,
            int enemyHpPct,
            int enemyDamagePct,
            int enemyCohesionPct,
            string enemyAiTier,
            int injuryPct,
            int recoveryPct,
            int recruitRefreshPct,
            int signatureBonusPoints,
            int lootPct,
            int craftingAssist,
            int relationshipDecayPct,
            string learningVisibility,
            int sustainabilityStrictnessPct,
            long? startingTreasuryXp,
            string startingEquipmentQuality)
        {
            return new ModeRuleSnapshot(
                mode,
                death,
                departure,
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
                showAllLearningConditions: false,
                noTrivialEnemyDiminishingReturns: false,
                freeUnionReorganization: false,
                allowBothArtBranches: false,
                instantConstruction: false,
                freeConstruction: false,
                storyAuthorityBypassEnabled: false,
                consentBypassEnabled: false,
                protectedActorBypassEnabled: false);
        }
    }
}
