using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Gameplay.Campaign022
{
    // Composition only. The immutable floor begin binding selects this policy;
    // the existing actual-floor policy still supplies all stats and Union counts.
    public static class TowerEnemyPartyRules137
    {
        public const string Policy137 = "ENDLESS_FLOOR_137_PARTIES_V1";
        public const string ModifierPrefix137 = "TOWER_PARTY137_";
        public const string Modifier137 = "TOWER_PARTY137_V1_M06";
        public const string SpawnSuffix137 = "_TOWER_PARTY137_V1";
        public const int MemberCount137 = 6;

        public static bool HasMarker137(IReadOnlyList<string> modifiers) =>
            (modifiers ?? Array.Empty<string>()).Any(value => value != null &&
                value.StartsWith(ModifierPrefix137, StringComparison.Ordinal));

        public static bool ReadCommitted137(EncounterLaunchRequest017D request)
        {
            if (request == null) return false;
            // New stat markers must validate before the existing roster reveal;
            // old137/098 requests have no138 marker and remain unchanged.
            TowerScalingRules138.ReadFloor138(request.RouteModifiers);
            var markers = request.RouteModifiers.Where(value => value != null &&
                value.StartsWith(ModifierPrefix137, StringComparison.Ordinal)).ToArray();
            if (markers.Length == 0) return false;
            var floor = TowerThreatRules098.ReadFloor098(request.RouteModifiers);
            if (markers.Length != 1 || markers[0] != Modifier137 || floor < 1 ||
                request.EnemyUnionCount != TowerThreatRules098.EnemyUnionCount098(floor) ||
                request.RouteModifiers.Any(value => value != null &&
                    value.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)))
                throw new InvalidOperationException("TOWER137_COMPOSITION_MODIFIER_INVALID");
            return true;
        }

        public static void ValidateRoster137(EncounterLaunchRequest017D request, EncounterRoster070 roster)
        {
            if (!ReadCommitted137(request)) return;
            if (roster == null)
                throw new InvalidOperationException("TOWER137_COMMITTED_ROSTER_REQUIRED");
            if (roster.Unions.Count != request.EnemyUnionCount ||
                roster.CanonicalSeedIdentity != request.CanonicalSeedIdentity)
                throw new InvalidOperationException("TOWER137_ROSTER_IDENTITY_MISMATCH");
            if (roster.Unions.Any(union => union.Members.Count != MemberCount137))
                throw new InvalidOperationException("TOWER137_ROSTER_MEMBER_COUNT_INVALID");
            var members = roster.Unions.SelectMany(union => union.Members).ToArray();
            if (members.Select(member => member.MemberId).Distinct(StringComparer.Ordinal).Count() != members.Length ||
                members.Any(member => !member.MemberId.EndsWith(SpawnSuffix137, StringComparison.Ordinal)))
                throw new InvalidOperationException("TOWER137_SPAWN_IDENTITY_MISMATCH");
            if (members.Count(member => EnemyForceProfile094.IsNamedBossFamily094(member.FamilyId)) > 1)
                throw new InvalidOperationException("TOWER137_NAMED_BOSS_DUPLICATED");
        }
    }
}
