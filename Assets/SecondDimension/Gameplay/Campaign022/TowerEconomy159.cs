using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.Gameplay.Campaign022
{
    // Selected only at a new floor begin. Historical floor bindings keep their
    // original rewards. This fixed curve never reads player power or wallet.
    public static class TowerEconomy159
    {
        public const string Policy159 = "ENDLESS_FLOOR_159_DEPTH_XP_V1";
        public const string Modifier159 = "TOWER_REWARD159_DEPTH_200BP";
        public const string Prefix159 = "TOWER_REWARD159_";
        public static long BonusBasisPoints159(int floor)
        {
            if (floor < 1) throw new ArgumentOutOfRangeException(nameof(floor));
            return checked(((long)floor - 1) * 200L);
        }
        public static bool HasMarker159(IReadOnlyList<string> tags) =>
            (tags ?? Array.Empty<string>()).Any(t => t != null && t.StartsWith(Prefix159, StringComparison.Ordinal));
        public static long ReadBonus159(IReadOnlyList<string> tags)
        {
            if (!HasMarker159(tags)) return 0;
            var markers = tags.Where(t => t != null && t.StartsWith(Prefix159, StringComparison.Ordinal)).ToArray();
            var floor = TowerScalingRules138.ReadFloor138(tags);
            if (markers.Length != 1 || markers[0] != Modifier159 || floor < 1)
                throw new InvalidOperationException("TOWER159_REWARD_BINDING_INVALID");
            return BonusBasisPoints159(floor);
        }
        public static long ApplyBonus159(long value, long basisPoints)
        {
            if (value < 0 || basisPoints < 0) throw new ArgumentOutOfRangeException();
            // Decimal avoids intermediate long overflow for otherwise legal sums.
            return checked((long)decimal.Floor((decimal)value * (10000m + basisPoints) / 10000m));
        }
    }
}
