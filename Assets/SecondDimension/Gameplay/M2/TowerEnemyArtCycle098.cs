using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// New committed Tower098 presentation only. Numeric threat, member legality,
    /// rewards and Art selection do not come from these ten color variants.
    /// Historical Tower090/094 battle IDs are deliberately not interpreted here.
    /// </summary>
    public static class TowerEnemyArtCycle098
    {
        public const string Marker098 = "_ACTUAL098_";
        static readonly Regex ExactId098 = new Regex(
            @"\AABYSS_BATTLE022_FLOOR_(0[1-9]|10)_ACTUAL098_([1-9][0-9]{0,9})_([A-F0-9]{24})\z",
            RegexOptions.CultureInvariant);

        public static bool HasMarker098(string battleId) =>
            !string.IsNullOrEmpty(battleId) && battleId.IndexOf(Marker098, StringComparison.Ordinal) >= 0;

        public static bool TryResolve098(string battleId, out int actualFloor, out int template, out int variant)
        {
            actualFloor = template = variant = 0;
            if (string.IsNullOrEmpty(battleId)) return false;
            var match = ExactId098.Match(battleId);
            if (!match.Success ||
                !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var actual) ||
                actual < 1) return false;
            var templateValue = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            if (templateValue != (actual - 1) % 10 + 1) return false;
            var cycle = (actual - 1) / 10;
            actualFloor = actual;
            template = templateValue;
            variant = (templateValue - 1 + cycle % 10) % 10 + 1;
            return true;
        }
    }
}
