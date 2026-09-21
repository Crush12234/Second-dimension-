using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Gameplay.Campaign022
{
    /// <summary>
    /// Actual-floor tuning for newly committed098 Tower runs only. The ten art/
    /// encounter templates do not choose numerical strength. Ceilings protect
    /// existing integer combat/percentage paths; they are not an infinity claim.
    /// </summary>
    public static class TowerThreatRules098
    {
        public const string Policy098 = "ENDLESS_FLOOR_098_V1";
        public const string ModifierPrefix098 = "TOWER_ACTUAL_FLOOR098_";
        public const string BattleMarker098 = "_ACTUAL098_";
        public const int MaximumMemberHp098 = 1000000;
        public const int MaximumMemberOffense098 = 10000;

        public static long HpBonusBasisPoints098(int actualFloor) =>
            checked(((long)RequireFloor098(actualFloor) - 1L) * 500L);
        public static long OffenseBonusBasisPoints098(int actualFloor) =>
            checked(((long)RequireFloor098(actualFloor) - 1L) * 300L);
        public static int EnemyUnionCount098(int actualFloor) =>
            CampaignProgressionCommandService022.TowerEnemyUnionCount081(RequireFloor098(actualFloor), 0);
        public static string Modifier098(int actualFloor) => ModifierPrefix098 +
            RequireFloor098(actualFloor).ToString(CultureInfo.InvariantCulture);

        // Unknown/multiple/malformed reserved markers cannot silently downgrade.
        public static int ReadFloor098(IReadOnlyList<string> modifiers)
        {
            var values = (modifiers ?? Array.Empty<string>()).Where(value => value != null &&
                value.StartsWith(ModifierPrefix098, StringComparison.Ordinal)).Take(2).ToArray();
            if (values.Length == 0) return 0;
            if (values.Length != 1 || !int.TryParse(values[0].Substring(ModifierPrefix098.Length),
                NumberStyles.None, CultureInfo.InvariantCulture, out var floor) || floor < 1 ||
                values[0] != Modifier098(floor) || modifiers.Any(value => value != null &&
                    value.StartsWith("TOWER_THREAT_TIER_", StringComparison.Ordinal)))
                throw new InvalidOperationException("TOWER098_THREAT_MODIFIER_INVALID");
            return floor;
        }

        public static string BattleId098(int actualFloor, string requestHashSuffix)
        {
            RequireFloor098(actualFloor);
            if (requestHashSuffix == null || requestHashSuffix.Length != 24 ||
                requestHashSuffix.Any(value => !(value >= '0' && value <= '9') && !(value >= 'A' && value <= 'F')))
                throw new InvalidOperationException("TOWER098_REQUEST_HASH_SUFFIX_INVALID");
            var template = CampaignProgressionCommandService022.TowerContentTemplate094(actualFloor);
            return "ABYSS_BATTLE022_FLOOR_" + template.ToString("00", CultureInfo.InvariantCulture) +
                BattleMarker098 + actualFloor.ToString(CultureInfo.InvariantCulture) + "_" + requestHashSuffix;
        }

        public static int ScaleStat098(int value, long bonusBasisPoints, int ceiling)
        {
            if (value < 0 || bonusBasisPoints < 0 || ceiling < 1)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (value == 0) return 0;
            // Compare before multiplying; even int.MaxValue floor/base inputs
            // cannot overflow a long or wrap to weaker opposition.
            if (bonusBasisPoints > long.MaxValue - 10000L) return ceiling;
            var multiplier = 10000L + bonusBasisPoints;
            var cappedNumerator = (long)ceiling * 10000L;
            if (value >= ceiling || multiplier > cappedNumerator / value) return ceiling;
            return (int)Math.Min(ceiling, ((long)value * multiplier + 9999L) / 10000L);
        }

        public static BattleUnionState Apply098(BattleUnionState union, int actualFloor)
        {
            if (union == null) throw new ArgumentNullException(nameof(union));
            return ApplyBonuses098(union, actualFloor,
                HpBonusBasisPoints098(actualFloor), OffenseBonusBasisPoints098(actualFloor));
        }

        internal static BattleUnionState ApplyBonuses098(BattleUnionState union, int actualFloor, long hp, long offense)
        {
            if (union == null) throw new ArgumentNullException(nameof(union));
            var members = new List<BattleMemberState>(union.Members.Count);
            foreach (var member in union.Members)
            {
                var maximumHp = ScaleStat098(member.MaximumHp, hp, MaximumMemberHp098);
                var currentHp = member.CurrentHp <= 0 ? 0 : Math.Max(1,
                    (int)Math.Min(maximumHp, (long)member.CurrentHp * maximumHp / member.MaximumHp));
                members.Add(new BattleMemberState(member.MemberId, member.DisplayName, member.ClassId,
                    currentHp, maximumHp, member.CurrentMp, member.MaximumMp,
                    ScaleStat098(member.Attack, offense, MaximumMemberOffense098),
                    ScaleStat098(member.MagicAttack, offense, MaximumMemberOffense098),
                    member.EquipmentTags, member.Downed, member.Stabilized, member.Guarding,
                    member.LearnedArtIds, member.MeaningfulUsePoints, member.DiscoveryProgress,
                    member.BreakthroughArtId, member.ArtProgress, member.EquippedMainHandInstanceId,
                    member.EnemyArtBaseId090, member.EnemyArtVariantId090, member.VisualVariantSeed090));
            }
            // Keep the old first-ten-floor AP/Cohesion budget, never unbounded
            // action frequency. Beyond10 only vitality/offense continue growing.
            var tier = CampaignProgressionCommandService022.TowerThreatTier089(actualFloor);
            var apBonus = (tier - 1) / 3;
            var maximumAp = (int)Math.Min(999L, (long)union.MaximumAp + apBonus);
            var currentAp = (int)Math.Min(maximumAp, (long)union.CurrentAp + apBonus);
            return new BattleUnionState(union.UnionId, union.DisplayName, union.Side, union.LeaderMemberId,
                members.AsReadOnly(), union.FormationId, union.FormationName, union.FormationMemberCountEligible,
                union.FormationInactiveReason, currentAp, maximumAp,
                (int)Math.Min(100L, (long)union.Cohesion + tier - 1L), union.FormationConditionBasisPoints,
                union.Engagement, union.Guarding, union.Retreated, union.UnionMeaningfulUsePoints);
        }

        private static int RequireFloor098(int floor) => floor >= 1 ? floor :
            throw new ArgumentOutOfRangeException(nameof(floor));
    }
}
