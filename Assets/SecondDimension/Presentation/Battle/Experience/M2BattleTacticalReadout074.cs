using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only tactical readouts for the focused-Union battle experience.
    /// These values summarize the authoritative view; they never feed battle rules.
    /// </summary>
    public static class M2BattleTacticalReadout074
    {
        public static float AllyMoraleShare(M2BattleView battle)
        {
            var allyStrength = SideStrength(battle?.PlayerUnions);
            var enemyStrength = SideStrength(battle?.EnemyUnions);
            var total = allyStrength + enemyStrength;
            return total <= 0f ? 0.5f : Mathf.Clamp(allyStrength / total, 0.04f, 0.96f);
        }

        public static int CombatChain(M2BattleView battle)
        {
            var events = battle?.LastResolvedRoundEvents;
            if (events == null || events.Count == 0) events = battle?.RecentEvents;
            if (events == null) return 0;

            var chain = 0;
            for (var index = 0; index < events.Count; index++)
            {
                var type = events[index]?.EventType ?? string.Empty;
                if (Contains(type, "HIT") || Contains(type, "DAMAGE") ||
                    Contains(type, "INTERCEPTION") || Contains(type, "BREAKTHROUGH"))
                    chain++;
            }
            return chain;
        }

        public static string EngagementLabel(M2BattleUnionView ally, M2BattleUnionView enemy)
        {
            var allyState = ally?.Engagement ?? string.Empty;
            var enemyState = enemy?.Engagement ?? string.Empty;

            if (Contains(allyState, "FLANK") || Contains(allyState, "REAR")) return "FLANK";
            if (Contains(allyState, "INTERCEPT") || Contains(enemyState, "INTERCEPT"))
                return "INTERFERENCE";
            if (Contains(enemyState, "BROKEN")) return "BREAKTHROUGH";
            if (Contains(allyState, "BROKEN")) return "UNION BROKEN";
            if (Contains(allyState, "ENGAGED") || Contains(enemyState, "ENGAGED") ||
                Contains(allyState, "GUARDED") || Contains(enemyState, "GUARDED"))
                return "DEADLOCK";
            if (Contains(allyState, "SUPPORT") || Contains(allyState, "REINFORC")) return "SUPPORT";
            if (Contains(allyState, "DISENGAG")) return "DISENGAGE";
            return "APPROACH";
        }

        public static string UnionSummary(M2BattleUnionView union, string fallback)
        {
            if (union == null) return fallback ?? string.Empty;
            var currentHp = CurrentHp(union);
            var maximumHp = MaximumHp(union);
            var living = LivingMembers(union);
            var total = union.Members?.Count ?? 0;
            return FriendlyName(union.DisplayName, fallback) + "  •  HP " +
                   currentHp.ToString(CultureInfo.InvariantCulture) + "/" +
                   maximumHp.ToString(CultureInfo.InvariantCulture) + "  •  AP " +
                   Math.Max(0, union.CurrentAp).ToString(CultureInfo.InvariantCulture) + "/" +
                   Math.Max(0, union.MaximumAp).ToString(CultureInfo.InvariantCulture) + "  •  " +
                   living.ToString(CultureInfo.InvariantCulture) + "/" +
                   total.ToString(CultureInfo.InvariantCulture) + " UP";
        }

        public static int CurrentHp(M2BattleUnionView union)
        {
            var value = 0;
            if (union?.Members == null) return value;
            for (var index = 0; index < union.Members.Count; index++)
                if (union.Members[index] != null) value += Math.Max(0, union.Members[index].CurrentHp);
            return value;
        }

        public static int MaximumHp(M2BattleUnionView union)
        {
            var value = 0;
            if (union?.Members == null) return value;
            for (var index = 0; index < union.Members.Count; index++)
                if (union.Members[index] != null) value += Math.Max(0, union.Members[index].MaximumHp);
            return value;
        }

        public static int LivingMembers(M2BattleUnionView union)
        {
            var value = 0;
            if (union?.Members == null) return value;
            for (var index = 0; index < union.Members.Count; index++)
                if (union.Members[index] != null && !union.Members[index].Downed) value++;
            return value;
        }

        public static string FriendlyName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback ?? string.Empty;
            var result = value.Trim();
            if (Guid.TryParse(result, out _)) return fallback ?? string.Empty;
            if (result.IndexOf('_') < 0) return result;
            var prefixes = new[] { "PLAYER_UNION_", "ENEMY_UNION_", "UNION_", "ENEMY_" };
            for (var index = 0; index < prefixes.Length; index++)
            {
                if (!result.StartsWith(prefixes[index], StringComparison.OrdinalIgnoreCase)) continue;
                result = result.Substring(prefixes[index].Length);
                break;
            }
            result = result.Replace('_', ' ').Trim();
            return string.IsNullOrWhiteSpace(result)
                ? fallback ?? string.Empty
                : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(result.ToLowerInvariant());
        }

        private static float SideStrength(IReadOnlyList<M2BattleUnionView> unions)
        {
            if (unions == null) return 0f;
            var currentHp = 0;
            var maximumHp = 0;
            var living = 0;
            var members = 0;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                currentHp += CurrentHp(union);
                maximumHp += MaximumHp(union);
                living += LivingMembers(union);
                members += union?.Members?.Count ?? 0;
            }
            if (maximumHp <= 0 || members <= 0) return 0f;
            var vitality = currentHp / (float)maximumHp;
            var presence = living / (float)members;
            // Maximum side HP keeps a larger army tactically meaningful, while
            // vitality and living formations make the rail react immediately.
            return maximumHp * (0.20f + vitality * 0.60f + presence * 0.20f);
        }

        private static bool Contains(string value, string token) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
