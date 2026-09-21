using System;
using System.Collections.Generic;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// A derived view of an Art's persisted mastery. Art level is intentionally
    /// not saved: MasteryPoints remains the single campaign authority.
    /// </summary>
    public sealed class M2ArtMasteryLevelProgress088
    {
        internal M2ArtMasteryLevelProgress088(
            int masteryPoints,
            int level,
            int currentLevelThreshold,
            int nextLevelThreshold,
            int masteryIntoLevel,
            int masteryRequiredForNextLevel,
            int progressBasisPoints)
        {
            MasteryPoints = masteryPoints;
            Level = level;
            CurrentLevelThreshold = currentLevelThreshold;
            NextLevelThreshold = nextLevelThreshold;
            MasteryIntoLevel = masteryIntoLevel;
            MasteryRequiredForNextLevel = masteryRequiredForNextLevel;
            ProgressBasisPoints = progressBasisPoints;
        }

        public int MasteryPoints { get; }
        public int Level { get; }
        public int CurrentLevelThreshold { get; }
        public int NextLevelThreshold { get; }
        public int MasteryIntoLevel { get; }
        public int MasteryRequiredForNextLevel { get; }
        public int ProgressBasisPoints { get; }
        public bool IsMaximumLevel => Level >= M2ArtMasteryLevelPolicy088.MaximumLevel;
    }

    /// <summary>
    /// Pure Version 88 Art-level policy. Existing MasteryPoints deterministically
    /// produce levels 1-10, progress, and a bounded combat-power multiplier.
    /// </summary>
    public static class M2ArtMasteryLevelPolicy088
    {
        public const int MinimumLevel = 1;
        public const int MaximumLevel = 10;
        public const int BasePowerPermille = 1000;
        public const int PowerGainPerLevelPermille = 60;
        public const int MaximumPowerPermille = 1540;

        private static readonly int[] Thresholds =
        {
            0,
            100,
            260,
            520,
            900,
            1400,
            2000,
            2750,
            3650,
            4700
        };

        private static readonly IReadOnlyList<int> ReadOnlyThresholds =
            Array.AsReadOnly(Thresholds);

        public static IReadOnlyList<int> MasteryThresholds => ReadOnlyThresholds;

        public static int ThresholdForLevel(int level)
        {
            if (level < MinimumLevel || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            return Thresholds[level - 1];
        }

        public static int LevelForMasteryPoints(int masteryPoints)
        {
            var normalized = Math.Max(0, masteryPoints);
            for (var index = Thresholds.Length - 1; index >= 0; index--)
                if (normalized >= Thresholds[index]) return index + 1;
            return MinimumLevel;
        }

        public static M2ArtMasteryLevelProgress088 ProgressForMasteryPoints(
            int masteryPoints)
        {
            var normalized = Math.Max(0, masteryPoints);
            var level = LevelForMasteryPoints(normalized);
            var currentThreshold = Thresholds[level - 1];
            if (level >= MaximumLevel)
                return new M2ArtMasteryLevelProgress088(
                    normalized,
                    level,
                    currentThreshold,
                    0,
                    Math.Max(0, normalized - currentThreshold),
                    0,
                    10000);

            var nextThreshold = Thresholds[level];
            var required = nextThreshold - currentThreshold;
            var intoLevel = Math.Min(required, Math.Max(0, normalized - currentThreshold));
            var basisPoints = required <= 0
                ? 10000
                : (int)((long)intoLevel * 10000L / required);
            return new M2ArtMasteryLevelProgress088(
                normalized,
                level,
                currentThreshold,
                nextThreshold,
                intoLevel,
                required,
                Math.Max(0, Math.Min(10000, basisPoints)));
        }

        public static int PowerPermilleForMasteryPoints(int masteryPoints)
        {
            var level = LevelForMasteryPoints(masteryPoints);
            return BasePowerPermille +
                   (level - MinimumLevel) * PowerGainPerLevelPermille;
        }

        public static int ScaleMagnitude(int baseMagnitude, int masteryPoints) =>
            ScaleMagnitudeByPowerPermille(
                baseMagnitude,
                PowerPermilleForMasteryPoints(masteryPoints));

        public static int ScaleMagnitudeByPowerPermille(
            int baseMagnitude,
            int powerPermille)
        {
            if (baseMagnitude <= 0) return 0;
            var normalizedPower = Math.Max(1, powerPermille);
            var scaled = (long)baseMagnitude * normalizedPower /
                         BasePowerPermille;
            return scaled >= int.MaxValue ? int.MaxValue : (int)scaled;
        }
    }
}
