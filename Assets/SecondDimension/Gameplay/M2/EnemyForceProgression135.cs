using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Gameplay.M2
{
    // Candidate following the earned sixty-hero campaign run: more survivors
    // can act, and their hits grow with the committed chapter. V1/V2 are frozen.
    public sealed partial class EnemyForceProfile094
    {
        public const string Prefix135 = "ENEMY_FORCE094_V3_";
        public const string SpawnMarker135 = "_FORCE094_V3_CH";
        private readonly bool _pressure135;
        private static readonly Regex PressureToken135 = new Regex(
            @"\AENEMY_FORCE094_V3_CH(\d{3})_U(\d{2})_M(\d{2})_S(\d{3})_H14_O12\z",
            RegexOptions.CultureInvariant);

        private EnemyForceProfile094(EnemyForceProfile094 composition, int statChapter)
            : this(composition.Chapter094, composition.MinimumUnions094, composition.MemberCount094,
                composition.UnionCount094, statChapter)
        { _pressure135 = true; }

        public bool IsPressure135 => _pressure135;
        public int HpPercent135 => _pressure135 ? 100 + 14 * Math.Max(0, StatChapter134 - 10) : HpPercent134;
        public int OffensePercent135 => _pressure135 ? 100 + 12 * Math.Max(0, StatChapter134 - 10) : OffensePercent134;
        private string PressureTag135 => string.Format(CultureInfo.InvariantCulture,
            "ENEMY_FORCE094_V3_CH{0:000}_U{1:00}_M{2:00}_S{3:000}_H14_O12",
            Chapter094, UnionCount094, MemberCount094, StatChapter134);

        public static EnemyForceProfile094 ForNewChapter135(int chapter, int authoredUnionCount, int statChapter = 0)
        {
            var composition = ForNewChapter134(chapter, authoredUnionCount, statChapter);
            return composition == null ? null : new EnemyForceProfile094(composition, composition.StatChapter134);
        }

        private static EnemyForceProfile094 ReadPressureTag135(string tag)
        {
            var match = PressureToken135.Match(tag ?? string.Empty);
            if (!match.Success) throw new InvalidOperationException("ENEMY_FORCE135_PROFILE_INVALID");
            var chapter = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var unions = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var members = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var statChapter = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            var profile = ForNewChapter135(chapter, unions, statChapter);
            if (profile == null || profile.Tag094 != tag || profile.MemberCount094 != members)
                throw new InvalidOperationException("ENEMY_FORCE135_CARDINALITY_INVALID");
            return profile;
        }

        // The old implementation handles old routes verbatim. New requests use
        // their immutable S chapter, before the existing replay finale floor.
        public static IReadOnlyList<BattleUnionState> ApplyProgression135(
            IReadOnlyList<BattleUnionState> source, IReadOnlyList<string> routes)
        {
            var tags = (routes ?? Array.Empty<string>()).Where(value => value != null &&
                value.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)).ToArray();
            if (!tags.Any(value => value.StartsWith(Prefix135, StringComparison.Ordinal)))
                return ApplyProgression134(source, routes);
            if (tags.Length != 1) throw new InvalidOperationException("ENEMY_FORCE094_MULTIPLE_PROFILES");
            RejectTowerMix134(routes);
            var profile = ReadPressureTag135(tags[0]);
            if (source == null || source.Count != profile.UnionCount094 ||
                source.Any(union => union == null || union.Side != BattleSide.Enemy ||
                    union.Members.Count < profile.MemberCount094 || union.Members.Count > 6))
                throw new InvalidOperationException("ENEMY_FORCE135_ROSTER_REQUIRED");
            return Array.AsReadOnly(source.Select(union => union.With(members: Array.AsReadOnly(union.Members.Select(member =>
            {
                var hp = ScaleProgression134(member.MaximumHp, profile.HpPercent135, TowerThreatRules098.MaximumMemberHp098);
                var current = member.CurrentHp <= 0 ? 0 : Math.Max(1, (int)((long)member.CurrentHp * hp / member.MaximumHp));
                return new BattleMemberState(member.MemberId, member.DisplayName, member.ClassId, current, hp,
                    member.CurrentMp, member.MaximumMp,
                    ScaleProgression134(member.Attack, profile.OffensePercent135, TowerThreatRules098.MaximumMemberOffense098),
                    ScaleProgression134(member.MagicAttack, profile.OffensePercent135, TowerThreatRules098.MaximumMemberOffense098),
                    member.EquipmentTags, member.Downed, member.Stabilized, member.Guarding, member.LearnedArtIds,
                    member.MeaningfulUsePoints, member.DiscoveryProgress, member.BreakthroughArtId, member.ArtProgress,
                    member.EquippedMainHandInstanceId, member.EnemyArtBaseId090, member.EnemyArtVariantId090, member.VisualVariantSeed090);
            }).ToArray()))).ToArray());
        }
    }
}
