using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only enemy threat language. The live Pass 03 roster keeps its
    /// immutable combat identity; this policy turns its authored rank/family into
    /// one of ten restrained colour grades so repeated Tower encounters remain
    /// readable at a glance without manufacturing a second enemy system.
    /// </summary>
    public sealed class M2EnemyThreatVisual089
    {
        internal M2EnemyThreatVisual089(
            int tier,
            string romanTier,
            string label,
            Color artworkTint,
            Color frameColor,
            Color plateColor)
        {
            Tier = tier;
            RomanTier = romanTier ?? string.Empty;
            Label = label ?? string.Empty;
            ArtworkTint = artworkTint;
            FrameColor = frameColor;
            PlateColor = plateColor;
        }

        public int Tier { get; }
        public string RomanTier { get; }
        public string Label { get; }
        public Color ArtworkTint { get; }
        public Color FrameColor { get; }
        public Color PlateColor { get; }
    }

    public static class M2EnemyThreatPalette089
    {
        public const int MinimumTier = 1;
        public const int MaximumTier = 10;
        public static M2EnemyThreatVisual089 Resolve(
            M2BattleMemberView member,
            bool enemy,
            bool certifiedBoss)
        {
            if (!enemy)
                return Visual(1);

            var identity = Normalize(member?.PortraitAuthorityId);
            if (identity.Length == 0) identity = Normalize(member?.MemberId);
            var tier = ResolveTier(
                identity,
                member?.MaximumHp ?? 0,
                certifiedBoss,
                member?.EnemyThreatTier089 ?? 0);
            return Visual(tier);
        }

        public static int ResolveTier(
            string identity,
            int maximumHp,
            bool certifiedBoss = false,
            int authoritativeDifficultyTier = 0)
        {
            var normalized = Normalize(identity);
            if (certifiedBoss ||
                Contains(normalized, "HINGE_EATER_COLOSSUS") ||
                Contains(normalized, "GATEHEART_WARDEN"))
                return MaximumTier;

            // Tower battle IDs are projected by the coordinator from the immutable
            // floor authority. Each opening floor owns one clear visual grade;
            // repeat runs retain tier X while their enemy-Union pressure can rise.
            if (authoritativeDifficultyTier >= MinimumTier &&
                authoritativeDifficultyTier <= MaximumTier)
                return authoritativeDifficultyTier;

            if (Contains(normalized, "CAPTAIN_RAVEL") ||
                Contains(normalized, "BRASSJAW_PACKLORD"))
                return 4;

            var tier = EndsWithRank(normalized, "03") ? 3
                : EndsWithRank(normalized, "02") ? 2
                : EndsWithRank(normalized, "01") ? 1
                : maximumHp >= 300 ? 5
                : maximumHp >= 220 ? 4
                : maximumHp >= 175 ? 3
                : maximumHp >= 100 ? 2
                : 1;

            // Heavy/support authorities are deliberately one colour grade above a
            // same-rank scout. This communicates their real HP/AP/support pressure.
            if (Contains(normalized, "GATEIRON_BRUTE") ||
                Contains(normalized, "ECHO_STALKER") ||
                Contains(normalized, "PULSE_SCRIBE") ||
                Contains(normalized, "ASH_MEDIC"))
                tier++;

            return Mathf.Clamp(tier, MinimumTier, MaximumTier);
        }

        public static int ResolveTowerTier(string battleId)
        {
            if (string.IsNullOrWhiteSpace(battleId)) return 0;
            const string marker = "ABYSS_BATTLE022_FLOOR_";
            var normalized = battleId.Trim().ToUpperInvariant();
            var start = normalized.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return 0;
            start += marker.Length;
            if (start + 2 > normalized.Length ||
                !int.TryParse(normalized.Substring(start, 2), out var floor) ||
                floor < 1 || floor > 10)
                return 0;
            return Mathf.Clamp(floor, MinimumTier, MaximumTier);
        }

        public static M2EnemyThreatVisual089 Visual(int tier)
        {
            switch (Mathf.Clamp(tier, MinimumTier, MaximumTier))
            {
                case 2:
                    return new M2EnemyThreatVisual089(
                        2, "II", "HARDENED",
                        new Color(0.83f, 1.00f, 0.87f, 1f),
                        new Color(0.30f, 0.94f, 0.55f, 0.84f),
                        new Color(0.025f, 0.13f, 0.075f, 0.84f));
                case 3:
                    return new M2EnemyThreatVisual089(
                        3, "III", "ELITE",
                        new Color(0.82f, 0.93f, 1.00f, 1f),
                        new Color(0.28f, 0.72f, 1.00f, 0.88f),
                        new Color(0.025f, 0.075f, 0.16f, 0.86f));
                case 4:
                    return new M2EnemyThreatVisual089(
                        4, "IV", "CHAMPION",
                        new Color(0.95f, 0.84f, 1.00f, 1f),
                        new Color(0.80f, 0.42f, 1.00f, 0.92f),
                        new Color(0.105f, 0.025f, 0.16f, 0.88f));
                case 5:
                    return new M2EnemyThreatVisual089(
                        5, "V", "MYTHIC",
                        new Color(1.00f, 0.90f, 0.66f, 1f),
                        new Color(1.00f, 0.70f, 0.20f, 0.96f),
                        new Color(0.18f, 0.075f, 0.018f, 0.92f));
                case 6:
                    return new M2EnemyThreatVisual089(
                        6, "VI", "ASCENDANT",
                        new Color(1.00f, 0.78f, 0.60f, 1f),
                        new Color(1.00f, 0.43f, 0.14f, 0.97f),
                        new Color(0.21f, 0.045f, 0.012f, 0.93f));
                case 7:
                    return new M2EnemyThreatVisual089(
                        7, "VII", "DREAD",
                        new Color(1.00f, 0.65f, 0.69f, 1f),
                        new Color(1.00f, 0.20f, 0.28f, 0.98f),
                        new Color(0.22f, 0.012f, 0.035f, 0.94f));
                case 8:
                    return new M2EnemyThreatVisual089(
                        8, "VIII", "ABYSSAL",
                        new Color(0.96f, 0.66f, 1.00f, 1f),
                        new Color(0.92f, 0.20f, 1.00f, 0.99f),
                        new Color(0.18f, 0.012f, 0.22f, 0.95f));
                case 9:
                    return new M2EnemyThreatVisual089(
                        9, "IX", "SOVEREIGN",
                        new Color(0.76f, 1.00f, 0.98f, 1f),
                        new Color(0.15f, 0.98f, 0.91f, 1.00f),
                        new Color(0.008f, 0.17f, 0.17f, 0.96f));
                case 10:
                    return new M2EnemyThreatVisual089(
                        10, "X", "APOCALYPTIC",
                        new Color(1.00f, 0.73f, 0.42f, 1f),
                        new Color(1.00f, 0.16f, 0.08f, 1.00f),
                        new Color(0.24f, 0.008f, 0.004f, 0.97f));
                default:
                    return new M2EnemyThreatVisual089(
                        1, "I", "SCOUT",
                        new Color(0.94f, 0.96f, 1.00f, 1f),
                        new Color(0.70f, 0.78f, 0.86f, 0.76f),
                        new Color(0.075f, 0.082f, 0.10f, 0.80f));
            }
        }

        private static bool EndsWithRank(string identity, string rank)
        {
            if (identity.EndsWith("_" + rank, StringComparison.Ordinal)) return true;
            var spawn = identity.IndexOf("_SPAWN070_", StringComparison.Ordinal);
            return spawn > 0 && identity.Substring(0, spawn)
                .EndsWith("_" + rank, StringComparison.Ordinal);
        }

        private static bool Contains(string identity, string token) =>
            identity.IndexOf(token, StringComparison.Ordinal) >= 0;

        private static string Normalize(string identity) =>
            string.IsNullOrWhiteSpace(identity)
                ? string.Empty
                : identity.Trim().ToUpperInvariant();
    }
}
