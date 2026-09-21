using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Gameplay.M2
{
    // Recorded late-campaign victories retained 83-90% HP. New V4 keeps
    // the first 25 quests unchanged and adds pressure as the full army grows.
    // All older committed versions and their historical stats stay frozen.
    public sealed partial class EnemyForceProfile094
    {
        public const string Prefix138 = "ENEMY_FORCE094_V4_";
        public const string SpawnMarker138 = "_FORCE094_V4_CH";
        private readonly bool _pressure138;
        private static readonly Regex PressureToken138 = new Regex(
            @"\AENEMY_FORCE094_V4_CH(\d{3})_U(\d{2})_M(\d{2})_S(\d{3})_H14_O12_L25_H6_O8\z",
            RegexOptions.CultureInvariant);

        private EnemyForceProfile094(EnemyForceProfile094 composition, int statChapter, bool pressure138)
            : this(composition.Chapter094, composition.MinimumUnions094, composition.MemberCount094,
                composition.UnionCount094, statChapter)
        { _pressure138 = pressure138; }

        public bool IsPressure138 => _pressure138;
        public int HpPercent138 => _pressure138 ? 100 + 14 * Math.Max(0, StatChapter134 - 10) + 6 * Math.Max(0, StatChapter134 - 25) : HpPercent135;
        public int OffensePercent138 => _pressure138 ? 100 + 12 * Math.Max(0, StatChapter134 - 10) + 8 * Math.Max(0, StatChapter134 - 25) : OffensePercent135;
        private string PressureTag138 => string.Format(CultureInfo.InvariantCulture,
            "ENEMY_FORCE094_V4_CH{0:000}_U{1:00}_M{2:00}_S{3:000}_H14_O12_L25_H6_O8",
            Chapter094, UnionCount094, MemberCount094, StatChapter134);

        public static EnemyForceProfile094 ForNewChapter138(int chapter, int authoredUnionCount, int statChapter = 0)
        {
            var composition = ForNewChapter134(chapter, authoredUnionCount, statChapter);
            return composition == null ? null : new EnemyForceProfile094(composition, composition.StatChapter134, true);
        }

        private static EnemyForceProfile094 ReadPressureTag138(string tag)
        {
            var match = PressureToken138.Match(tag ?? string.Empty);
            if (!match.Success) throw new InvalidOperationException("ENEMY_FORCE138_PROFILE_INVALID");
            var chapter = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var unions = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var members = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var statChapter = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
            var profile = ForNewChapter138(chapter, unions, statChapter);
            if (profile == null || profile.Tag094 != tag || profile.MemberCount094 != members)
                throw new InvalidOperationException("ENEMY_FORCE138_CARDINALITY_INVALID");
            return profile;
        }

        // The old implementation handles old routes verbatim. New requests use
        // their immutable S chapter, before the existing replay finale floor.
        public static IReadOnlyList<BattleUnionState> ApplyProgression138(
            IReadOnlyList<BattleUnionState> source, IReadOnlyList<string> routes)
        {
            var tags = (routes ?? Array.Empty<string>()).Where(value => value != null &&
                value.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)).ToArray();
            if (!tags.Any(value => value.StartsWith(Prefix138, StringComparison.Ordinal)))
                return ApplyProgression135(source, routes);
            if (tags.Length != 1) throw new InvalidOperationException("ENEMY_FORCE094_MULTIPLE_PROFILES");
            RejectTowerMix134(routes);
            var profile = ReadPressureTag138(tags[0]);
            if (source == null || source.Count != profile.UnionCount094 ||
                source.Any(union => union == null || union.Side != BattleSide.Enemy ||
                    union.Members.Count < profile.MemberCount094 || union.Members.Count > 6))
                throw new InvalidOperationException("ENEMY_FORCE138_ROSTER_REQUIRED");
            return Array.AsReadOnly(source.Select(union => union.With(members: Array.AsReadOnly(union.Members.Select(member =>
            {
                var hp = ScaleProgression134(member.MaximumHp, profile.HpPercent138, TowerThreatRules098.MaximumMemberHp098);
                var current = member.CurrentHp <= 0 ? 0 : Math.Max(1, (int)((long)member.CurrentHp * hp / member.MaximumHp));
                return new BattleMemberState(member.MemberId, member.DisplayName, member.ClassId, current, hp,
                    member.CurrentMp, member.MaximumMp,
                    ScaleProgression134(member.Attack, profile.OffensePercent138, TowerThreatRules098.MaximumMemberOffense098),
                    ScaleProgression134(member.MagicAttack, profile.OffensePercent138, TowerThreatRules098.MaximumMemberOffense098),
                    member.EquipmentTags, member.Downed, member.Stabilized, member.Guarding, member.LearnedArtIds,
                    member.MeaningfulUsePoints, member.DiscoveryProgress, member.BreakthroughArtId, member.ArtProgress,
                    member.EquippedMainHandInstanceId, member.EnemyArtBaseId090, member.EnemyArtVariantId090, member.VisualVariantSeed090);
            }).ToArray()))).ToArray());
        }
    }
}
