using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Gameplay.M2
{
    // Candidate first-cycle curve. V1 remains immutable; only newly committed
    // V2 requests use these counts and stats. This is not a rubber-band rule.
    public sealed partial class EnemyForceProfile094
    {
        public const string Prefix134 = "ENEMY_FORCE094_V2_";
        public const string SpawnMarker134 = "_FORCE094_V2_CH";
        private readonly bool _progression134;
        private readonly int _statChapter134;
        private static readonly Regex ProgressionToken134 = new Regex(
            @"\AENEMY_FORCE094_V2_CH(\d{3})_U(\d{2})_M(\d{2})_S(\d{3})_H06_O04\z",
            RegexOptions.CultureInvariant);

        private EnemyForceProfile094(int chapter, int minimum, int members, int unions, int statChapter)
            : this(chapter, minimum, members, unions)
        { _progression134 = true; _statChapter134 = statChapter; }

        public bool IsProgression134 => _progression134;
        public int StatChapter134 => _progression134 ? _statChapter134 : Chapter094;
        public int HpPercent134 => _progression134 ? 100 + 6 * Math.Max(0, StatChapter134 - 10) : 100;
        public int OffensePercent134 => _progression134 ? 100 + 4 * Math.Max(0, StatChapter134 - 10) : 100;
        private string ProgressionTag134 => string.Format(CultureInfo.InvariantCulture,
            "ENEMY_FORCE094_V2_CH{0:000}_U{1:00}_M{2:00}_S{3:000}_H06_O04",
            Chapter094, UnionCount094, MemberCount094, StatChapter134);

        public static EnemyForceProfile094 ForNewChapter134(int chapter, int authoredUnionCount, int statChapter = 0)
        {
            if (chapter <= 10) return null;
            if (chapter > 999) throw new ArgumentOutOfRangeException(nameof(chapter));
            if (statChapter == 0) statChapter = chapter;
            if (statChapter < 1 || statChapter > 999) throw new ArgumentOutOfRangeException(nameof(statChapter));
            var minimum = Math.Min(10, 3 + (chapter - 11) / 2);
            var members = chapter <= 15 ? 3 : chapter <= 20 ? 4 : chapter <= 24 ? 5 : 6;
            return new EnemyForceProfile094(chapter, minimum, members,
                Math.Max(minimum, Math.Min(10, Math.Max(1, authoredUnionCount))), statChapter);
        }

        private static EnemyForceProfile094 ReadProgressionTag134(string tag)
        {
            var match = ProgressionToken134.Match(tag ?? string.Empty);
            if (!match.Success) throw new InvalidOperationException("ENEMY_FORCE134_PROFILE_INVALID");
            var chapter = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var unions = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var members = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var statChapter = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            var profile = ForNewChapter134(chapter, unions, statChapter);
            if (profile == null || profile.Tag094 != tag || profile.MemberCount094 != members)
                throw new InvalidOperationException("ENEMY_FORCE134_CARDINALITY_INVALID");
            return profile;
        }

        private static void RejectTowerMix134(IReadOnlyList<string> routes)
        {
            if ((routes ?? Array.Empty<string>()).Any(value => value != null &&
                value.StartsWith("TOWER_", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("ENEMY_FORCE134_TOWER_MIX_INVALID");
        }

        // Apply before the existing replay transform: its saved finale floor is
        // the final minimum, and scaling never reads mutable campaign progress.
        public static IReadOnlyList<BattleUnionState> ApplyProgression134(
            IReadOnlyList<BattleUnionState> source, IReadOnlyList<string> routes)
        {
            var tags = (routes ?? Array.Empty<string>()).Where(value => value != null &&
                value.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)).ToArray();
            if (!tags.Any(value => value.StartsWith(Prefix134, StringComparison.Ordinal))) return source;
            if (tags.Length != 1) throw new InvalidOperationException("ENEMY_FORCE094_MULTIPLE_PROFILES");
            RejectTowerMix134(routes);
            var profile = ReadProgressionTag134(tags[0]);
            if (source == null || source.Count != profile.UnionCount094 ||
                source.Any(union => union == null || union.Side != BattleSide.Enemy ||
                    union.Members.Count < profile.MemberCount094 || union.Members.Count > 6))
                throw new InvalidOperationException("ENEMY_FORCE134_ROSTER_REQUIRED");
            return Array.AsReadOnly(source.Select(union => union.With(members: Array.AsReadOnly(union.Members.Select(member =>
            {
                var hp = ScaleProgression134(member.MaximumHp, profile.HpPercent134, TowerThreatRules098.MaximumMemberHp098);
                var current = member.CurrentHp <= 0 ? 0 : Math.Max(1, (int)((long)member.CurrentHp * hp / member.MaximumHp));
                return new BattleMemberState(member.MemberId, member.DisplayName, member.ClassId, current, hp,
                    member.CurrentMp, member.MaximumMp,
                    ScaleProgression134(member.Attack, profile.OffensePercent134, TowerThreatRules098.MaximumMemberOffense098),
                    ScaleProgression134(member.MagicAttack, profile.OffensePercent134, TowerThreatRules098.MaximumMemberOffense098),
                    member.EquipmentTags, member.Downed, member.Stabilized, member.Guarding, member.LearnedArtIds,
                    member.MeaningfulUsePoints, member.DiscoveryProgress, member.BreakthroughArtId, member.ArtProgress,
                    member.EquippedMainHandInstanceId, member.EnemyArtBaseId090, member.EnemyArtVariantId090, member.VisualVariantSeed090);
            }).ToArray()))).ToArray());
        }

        private static int ScaleProgression134(int value, int percent, int ceiling) =>
            value >= ceiling ? value : TowerThreatRules098.ScaleStat098(value, (long)(percent - 100) * 100L, ceiling);
    }
}
