using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Gameplay.Campaign022
{
    // Absolute floor curve. Only a new immutable begin binding may select it;
    // no player level, equipment, roster strength or previous outcome is read.
    public static class TowerScalingRules138
    {
        public const string Policy138 = "ENDLESS_FLOOR_138_PRESSURE_V1";
        public const string ModifierPrefix138 = "TOWER_SCALING138_";
        public const string Modifier138 = "TOWER_SCALING138_V1_H20_O20_A81_H30_O25";

        public static long HpBonusBasisPoints138(int floor) => Bonus138(floor, 500, 3000);
        public static long OffenseBonusBasisPoints138(int floor) => Bonus138(floor, 300, 2500);

        static long Bonus138(int floor, int openingStep, int endlessStep)
        {
            if (floor < 1) throw new ArgumentOutOfRangeException(nameof(floor));
            return checked(Math.Min((long)floor - 1, 9) * openingStep +
                Math.Min(Math.Max((long)floor - 10, 0), 71) * 2000 +
                Math.Max((long)floor - 81, 0) * endlessStep);
        }

        public static bool HasMarker138(IReadOnlyList<string> routes) =>
            (routes ?? Array.Empty<string>()).Any(tag => tag != null &&
                tag.StartsWith(ModifierPrefix138, StringComparison.Ordinal));

        public static int ReadFloor138(IReadOnlyList<string> routes)
        {
            var tags = (routes ?? Array.Empty<string>()).Where(tag => tag != null &&
                tag.StartsWith(ModifierPrefix138, StringComparison.Ordinal)).ToArray();
            if (tags.Length == 0) return 0;
            var floor = TowerThreatRules098.ReadFloor098(routes);
            var partyTags = routes.Where(tag => tag != null &&
                tag.StartsWith(TowerEnemyPartyRules137.ModifierPrefix137, StringComparison.Ordinal)).ToArray();
            if (tags.Length != 1 || tags[0] != Modifier138 || floor < 1 ||
                partyTags.Length != 1 || partyTags[0] != TowerEnemyPartyRules137.Modifier137 ||
                routes.Any(tag => tag != null && (tag.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal) ||
                    tag.StartsWith("CAMPAIGN_REPLAY", StringComparison.Ordinal))))
                throw new InvalidOperationException("TOWER138_SCALING_MODIFIER_INVALID");
            return floor;
        }

        public static bool ReadCommitted138(EncounterLaunchRequest017D request)
        {
            if (request == null || ReadFloor138(request.RouteModifiers) == 0) return false;
            if (!TowerEnemyPartyRules137.ReadCommitted137(request))
                throw new InvalidOperationException("TOWER138_FULL_PARTIES_REQUIRED");
            return true;
        }

        public static BattleUnionState Apply138(BattleUnionState union, int floor) =>
            TowerThreatRules098.ApplyBonuses098(union, floor,
                HpBonusBasisPoints138(floor), OffenseBonusBasisPoints138(floor));
    }
}
