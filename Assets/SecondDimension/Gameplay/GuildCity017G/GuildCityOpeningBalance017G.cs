
using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017G
{
    public sealed class GuildCityModeProfile017G
    {
        public GuildCityModeProfile017G(
            GameMode mode,
            string displayName,
            int startingSupplyBonus,
            int checkModifier,
            int fatigueCostDelta,
            int urgencyCostDelta,
            int enemyUnionDelta,
            int guildRewardBasisPoints,
            int hallRewardBasisPoints)
        {
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (guildRewardBasisPoints <= 0 || hallRewardBasisPoints <= 0)
                throw new ArgumentOutOfRangeException(nameof(guildRewardBasisPoints));
            Mode = mode;
            DisplayName = displayName;
            StartingSupplyBonus = startingSupplyBonus;
            CheckModifier = checkModifier;
            FatigueCostDelta = fatigueCostDelta;
            UrgencyCostDelta = urgencyCostDelta;
            EnemyUnionDelta = enemyUnionDelta;
            GuildRewardBasisPoints = guildRewardBasisPoints;
            HallRewardBasisPoints = hallRewardBasisPoints;
        }

        public GameMode Mode { get; }
        public string DisplayName { get; }
        public int StartingSupplyBonus { get; }
        public int CheckModifier { get; }
        public int FatigueCostDelta { get; }
        public int UrgencyCostDelta { get; }
        public int EnemyUnionDelta { get; }
        public int GuildRewardBasisPoints { get; }
        public int HallRewardBasisPoints { get; }

        public int AdjustFatigueCost(int baseCost) => Math.Max(0, checked(baseCost + FatigueCostDelta));
        public int AdjustUrgencyCost(int baseCost) => Math.Max(0, checked(baseCost + UrgencyCostDelta));
        public int AdjustEnemyUnionCount(int baseCount) => Math.Max(1, Math.Min(10, checked(baseCount + EnemyUnionDelta)));
        public long AdjustGuildReward(long baseReward) => ScalePositive(baseReward, GuildRewardBasisPoints);
        public long AdjustHallReward(long baseReward) => ScalePositive(baseReward, HallRewardBasisPoints);

        private static long ScalePositive(long value, int basisPoints)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            return Math.Max(1, checked((value * basisPoints + 9_999L) / 10_000L));
        }
    }

    public static class GuildCityOpeningBalance017G
    {
        private static readonly GuildCityModeProfile017G[] Profiles =
        {
            new GuildCityModeProfile017G(GameMode.Relaxed, "Relaxed", 3, 1, -1, -1, -1, 11_000, 11_000),
            new GuildCityModeProfile017G(GameMode.Standard, "Standard", 0, 0, 0, 0, 0, 10_000, 10_000),
            new GuildCityModeProfile017G(GameMode.Iron, "Iron", -1, 0, 1, 1, 1, 11_500, 11_500),
            new GuildCityModeProfile017G(GameMode.Custom, "Custom", 0, 0, 0, 0, 0, 10_000, 10_000),
            new GuildCityModeProfile017G(GameMode.OverpoweredStart, "Overpowered Start", 5, 2, -1, -1, -1, 12_500, 12_500)
        };

        public static IReadOnlyList<GuildCityModeProfile017G> All => Array.AsReadOnly(Profiles);

        public static GuildCityModeProfile017G For(CampaignState campaign) =>
            For(campaign?.Profile?.Mode ?? GameMode.Standard);

        public static GuildCityModeProfile017G For(GameMode mode)
        {
            for (var index = 0; index < Profiles.Length; index++)
                if (Profiles[index].Mode == mode) return Profiles[index];
            return Profiles[1];
        }
    }
}
