using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Gameplay.Campaign019
{
    /// <summary>A copied policy read from one committed encounter, never from today's campaign cycle.</summary>
    public sealed class CampaignReplayThreatProfile130
    {
        internal CampaignReplayThreatProfile130(int cycle, int growthPercent, long hp=0, long attack=0, long magic=0, int unions=0)
        {
            Cycle = cycle;
            GrowthPercent = growthPercent; MinimumTotalHp132=hp; MinimumTotalAttack132=attack; MinimumTotalMagic132=magic; MinimumEnemyUnions132=unions;
        }

        public int Cycle { get; }
        public int GrowthPercent { get; }
        public long MinimumTotalHp132{get;} public long MinimumTotalAttack132{get;} public long MinimumTotalMagic132{get;}
        public int MinimumEnemyUnions132{get;}
    }

    /// <summary>
    /// Pure encounter tuning. Callers still authenticate the committed request;
    /// a syntactically valid route tag is not proof of campaign completion.
    /// </summary>
    public static class CampaignReplayThreat130
    {
        public const int DefaultGrowthPercent130 = 25;
        public const int MaximumMemberHp130 = TowerThreatRules098.MaximumMemberHp098;
        public const int MaximumMemberOffense130 = TowerThreatRules098.MaximumMemberOffense098;
        public const string ReservedPrefix130 = "CAMPAIGN_REPLAY130";
        static readonly Regex CommittedTag130 = new Regex(
            @"\ACAMPAIGN_REPLAY130_V1_C([1-9][0-9]*)_P(25|50|100)\z",
            RegexOptions.CultureInvariant);

        static readonly Regex FinaleTag132 = new Regex(
            @"\ACAMPAIGN_REPLAY130_V2_C([1-9][0-9]*)_P(25|50|100)_H([1-9][0-9]*)_A([1-9][0-9]*)_M([1-9][0-9]*)_U([1-9][0-9]*)\z",
            RegexOptions.CultureInvariant);
        public static string FormatFinaleTag132(int cycle,int growth,CampaignFinaleProof132 floor)
        {
            if(floor==null)throw new ArgumentNullException(nameof(floor));
            RequirePolicy130(cycle,growth);if(cycle<2)throw InvalidTag130();
            return FormatProfile132(new CampaignReplayThreatProfile130(cycle,growth,floor.MinimumTotalHp,
                floor.MinimumTotalAttack,floor.MinimumTotalMagic,floor.MinimumEnemyUnions));
        }
        static string FormatProfile132(CampaignReplayThreatProfile130 profile)=>
            profile.MinimumEnemyUnions132==0?FormatTag130(profile.Cycle,profile.GrowthPercent):
            string.Format(CultureInfo.InvariantCulture,"CAMPAIGN_REPLAY130_V2_C{0}_P{1}_H{2}_A{3}_M{4}_U{5}",
                profile.Cycle,profile.GrowthPercent,profile.MinimumTotalHp132,profile.MinimumTotalAttack132,
                profile.MinimumTotalMagic132,profile.MinimumEnemyUnions132);
        public static IReadOnlyList<string> AppendProfile132(IReadOnlyList<string> existing,CampaignReplayThreatProfile130 profile)
        {
            if(profile==null)return existing??Array.Empty<string>();
            var tag=FormatProfile132(profile);var old=ParseCommittedRoutes(existing);
            if(old!=null){if(FormatProfile132(old)!=tag)throw InvalidTag130();return existing;}
            if(HasTowerTag130(existing))throw InvalidTag130();
            var values=new List<string>(existing??Array.Empty<string>()){tag};return values.AsReadOnly();
        }
        public static IReadOnlyList<string> AppendFrozenRoutes132(IReadOnlyList<string> existing,CampaignProgressState019 progress,int cycle)
        {
            var tag=CampaignReplayRules130.FrozenObjectiveTagForCycle132(progress,cycle);
            return AppendProfile132(existing,tag==null?null:ParseCommittedRoutes(new[]{tag}));
        }
        public static int MinimumUnionCount132(int authored,IReadOnlyList<string> routes)=>
            Math.Max(authored,ParseCommittedRoutes(routes)?.MinimumEnemyUnions132??0);
        // Reuse the existing authored late-campaign force shape:10 Unions with
        //6 members, so early encounters cannot dilute the prior finale's force.
        public static int ForceChapter132(int authored,IReadOnlyList<string> routes)=>
            (ParseCommittedRoutes(routes)?.MinimumEnemyUnions132??0)>0?Math.Max(82,authored):authored;


        public static string FormatTag130(int cycle, int growthPercent)
        {
            RequirePolicy130(cycle, growthPercent);
            return cycle == 1 ? null : ReservedPrefix130 + "_V1_C" +
                cycle.ToString(CultureInfo.InvariantCulture) + "_P" +
                growthPercent.ToString(CultureInfo.InvariantCulture);
        }

        // A matching single frozen tag is idempotent. A caller cannot overwrite
        // an old committed policy with the latest cycle or combine two policies.
        public static IReadOnlyList<string> AppendToRoutes(
            IReadOnlyList<string> existing, int cycle, int growthPercent)
        {
            var requested = FormatTag130(cycle, growthPercent);
            var frozen = ParseCommittedRoutes(existing);
            if (frozen != null)
            {
                if (cycle != frozen.Cycle || growthPercent != frozen.GrowthPercent)
                    throw InvalidTag130();
                return existing;
            }
            if (requested == null) return existing ?? Array.Empty<string>();
            if (HasTowerTag130(existing)) throw InvalidTag130();
            var result = new List<string>(existing ?? Array.Empty<string>()) { requested };
            return result.AsReadOnly();
        }

        public static CampaignReplayThreatProfile130 ParseCommittedRoutes(IReadOnlyList<string> routes)
        {
            string tag = null;
            if (routes != null)
                for (var index = 0; index < routes.Count; index++)
                {
                    var value = routes[index];
                    if (value == null || !value.TrimStart().StartsWith(
                        ReservedPrefix130, StringComparison.OrdinalIgnoreCase)) continue;
                    if (tag != null) throw InvalidTag130();
                    tag = value;
                }
            if (tag == null) return null;
            // Canonical tags fit well below this bound, including int.MaxValue.
            if (tag.Length > 160 || HasTowerTag130(routes)) throw InvalidTag130();
            var modern=FinaleTag132.Match(tag);
            if(modern.Success)
            {
                var values=new long[6];
                for(var i=0;i<values.Length;i++)
                    if(!long.TryParse(modern.Groups[i+1].Value,NumberStyles.None,CultureInfo.InvariantCulture,out values[i]))throw InvalidTag130();
                if(values[0]<2||values[0]>int.MaxValue||values[2]>60L*MaximumMemberHp130||
                    values[3]>60L*MaximumMemberOffense130||values[4]>60L*MaximumMemberOffense130||values[5]>10)
                    throw InvalidTag130();
                return new CampaignReplayThreatProfile130((int)values[0],(int)values[1],values[2],values[3],values[4],(int)values[5]);
            }
            var match = CommittedTag130.Match(tag);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var cycle) || cycle < 2 ||
                !int.TryParse(match.Groups[2].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var growth)) throw InvalidTag130();
            return new CampaignReplayThreatProfile130(cycle, growth);
        }

        public static BattleUnionState ApplyToEnemyUnion(
            BattleUnionState union, CampaignReplayThreatProfile130 profile)
        {
            if (union == null) throw new ArgumentNullException(nameof(union));
            if (profile == null) return union;
            RequirePolicy130(profile.Cycle, profile.GrowthPercent);
            if (union.Side != BattleSide.Enemy)
                throw new InvalidOperationException("CAMPAIGN_REPLAY130_ENEMY_UNION_REQUIRED");
            var members = new List<BattleMemberState>(union.Members.Count);
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                var maximumHp = Scale130(member.MaximumHp, profile, MaximumMemberHp130);
                // Retain the existing injury fraction and never revive a downed
                // member. The long product is bounded by int.MaxValue squared.
                var currentHp = member.CurrentHp == 0 ? 0 : Math.Max(1,
                    (int)((long)member.CurrentHp * maximumHp / member.MaximumHp));
                members.Add(new BattleMemberState(member.MemberId, member.DisplayName, member.ClassId,
                    currentHp, maximumHp, member.CurrentMp, member.MaximumMp,
                    Scale130(member.Attack, profile, MaximumMemberOffense130),
                    Scale130(member.MagicAttack, profile, MaximumMemberOffense130),
                    member.EquipmentTags, member.Downed, member.Stabilized, member.Guarding,
                    member.LearnedArtIds, member.MeaningfulUsePoints, member.DiscoveryProgress,
                    member.BreakthroughArtId, member.ArtProgress, member.EquippedMainHandInstanceId,
                    member.EnemyArtBaseId090, member.EnemyArtVariantId090, member.VisualVariantSeed090));
            }
            // With(members) carries every Union field: AP, cohesion, formation,
            // engagement, leader, ordering and counts receive no replay bonus.
            return union.With(members: members.AsReadOnly());
        }

        public static IReadOnlyList<BattleUnionState> ApplyToEnemyUnions132(IReadOnlyList<BattleUnionState> source,CampaignReplayThreatProfile130 profile)
        {
            if(profile==null)return source;
            var result=source.Select(union=>ApplyToEnemyUnion(union,profile)).ToArray();
            if(profile.MinimumEnemyUnions132==0)return Array.AsReadOnly(result);
            if(result.Length<profile.MinimumEnemyUnions132)throw new InvalidOperationException("CAMPAIGN132_FROZEN_ENEMY_FORCE_REQUIRED");
            var count=result.Sum(union=>union.Members.Count);if(count==0)throw InvalidTag130();
            if(profile.MinimumTotalHp132>(long)count*MaximumMemberHp130||
                profile.MinimumTotalAttack132>(long)count*MaximumMemberOffense130||
                profile.MinimumTotalMagic132>(long)count*MaximumMemberOffense130)
                throw new InvalidOperationException("CAMPAIGN132_FROZEN_FORCE_CAPACITY_REQUIRED");
            var hp=(int)Math.Min(MaximumMemberHp130,(profile.MinimumTotalHp132+count-1)/count);
            var attack=(int)Math.Min(MaximumMemberOffense130,(profile.MinimumTotalAttack132+count-1)/count);
            var magic=(int)Math.Min(MaximumMemberOffense130,(profile.MinimumTotalMagic132+count-1)/count);
            for(var i=0;i<result.Length;i++)
            {
                var members=result[i].Members.Select(member=>
                {
                    var maximumHp=Math.Max(member.MaximumHp,hp);
                    var currentHp=member.CurrentHp==0?0:Math.Max(1,(int)((long)member.CurrentHp*maximumHp/member.MaximumHp));
                    return new BattleMemberState(member.MemberId,member.DisplayName,member.ClassId,currentHp,maximumHp,
                        member.CurrentMp,member.MaximumMp,Math.Max(member.Attack,attack),Math.Max(member.MagicAttack,magic),
                        member.EquipmentTags,member.Downed,member.Stabilized,member.Guarding,member.LearnedArtIds,
                        member.MeaningfulUsePoints,member.DiscoveryProgress,member.BreakthroughArtId,member.ArtProgress,
                        member.EquippedMainHandInstanceId,member.EnemyArtBaseId090,member.EnemyArtVariantId090,member.VisualVariantSeed090);
                }).ToArray();
                result[i]=result[i].With(members:Array.AsReadOnly(members));
            }
            return Array.AsReadOnly(result);
        }

        static int Scale130(int value, CampaignReplayThreatProfile130 profile, int ceiling)
        {
            // Previously authored values above a ceiling remain unchanged. The
            // cap limits added growth; it never silently weakens existing data.
            if (value >= ceiling) return value;
            var result = value;
            for (var completed = 1; completed < profile.Cycle && result < ceiling; completed++)
                result = (int)Math.Min(ceiling,
                    ((long)result * (100L + profile.GrowthPercent) + 99L) / 100L);
            // Even int.MaxValue cycles need only the few iterations to saturate.
            return result;
        }

        static bool HasTowerTag130(IReadOnlyList<string> routes)
        {
            if (routes != null)
                for (var index = 0; index < routes.Count; index++)
                    if (routes[index] != null && routes[index].TrimStart().StartsWith(
                        "TOWER_", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static void RequirePolicy130(int cycle, int growthPercent)
        {
            if (cycle < 1) throw new ArgumentOutOfRangeException(nameof(cycle));
            if (growthPercent != 25 && growthPercent != 50 && growthPercent != 100)
                throw new ArgumentOutOfRangeException(nameof(growthPercent));
        }

        static InvalidOperationException InvalidTag130() =>
            new InvalidOperationException("CAMPAIGN_REPLAY130_THREAT_MODIFIER_INVALID");
    }
}
