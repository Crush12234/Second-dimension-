using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Versioned composition policy for NEW campaign encounter commitments.
    /// V1 fixes composition; later explicit versions also fix chapter stat growth.
    /// Old untagged requests use the unchanged Version70 roster path.
    /// </summary>
    public sealed partial class EnemyForceProfile094
    {
        public const string Prefix094 = "ENEMY_FORCE094_V1_";
        public const string SpawnMarker094 = "_FORCE094_V1_CH";
        static readonly Regex ChapterToken094 = new Regex(
            @"(?:^|[^A-Z0-9])CH018_(\d{3})(?:$|[^0-9])",
            RegexOptions.CultureInvariant);
        static readonly Regex TagToken094 = new Regex(
            @"^ENEMY_FORCE094_V1_CH(\d{3})_U(\d{2})_M(\d{2})$",
            RegexOptions.CultureInvariant);

        EnemyForceProfile094(int chapter, int minimumUnions, int memberCount, int unionCount)
        {
            Chapter094 = chapter;
            MinimumUnions094 = minimumUnions;
            MemberCount094 = memberCount;
            UnionCount094 = unionCount;
        }

        public int Chapter094 { get; }
        public int MinimumUnions094 { get; }
        public int MemberCount094 { get; }
        public int UnionCount094 { get; }
        public int ArtTier094 => Math.Max(1, Math.Min(10, (Chapter094 + 4) / 5));
        public string Tag094 => IsPressure138 ? PressureTag138 : IsPressure135 ? PressureTag135 : IsProgression134 ? ProgressionTag134 : Prefix094 + "CH" + Chapter094.ToString("000", CultureInfo.InvariantCulture)
            + "_U" + UnionCount094.ToString("00", CultureInfo.InvariantCulture)
            + "_M" + MemberCount094.ToString("00", CultureInfo.InvariantCulture);
        public string SpawnSuffix094 => (IsPressure138 ? SpawnMarker138 : IsPressure135 ? SpawnMarker135 : IsProgression134 ? SpawnMarker134 : SpawnMarker094) +
            Chapter094.ToString("000", CultureInfo.InvariantCulture);

        public static EnemyForceProfile094 ForChapter094(int chapter, int authoredUnionCount)
        {
            if (chapter <= 10) return null;
            if (chapter > 999) throw new ArgumentOutOfRangeException(nameof(chapter));
            var minimum = chapter <= 15 ? 3 : chapter <= 20 ? 4 :
                chapter <= 25 ? 6 : chapter <= 30 ? 8 : chapter <= 34 ? 9 : 10;
            var members = chapter <= 15 ? 3 : chapter <= 25 ? 4 :
                chapter <= 30 ? 5 : 6;
            return new EnemyForceProfile094(chapter, minimum, members,
                Math.Max(minimum, Math.Min(10, Math.Max(1, authoredUnionCount))));
        }

        public static int ChapterNumber094(string identity)
        {
            var match = ChapterToken094.Match(identity ?? string.Empty);
            return match.Success && int.TryParse(match.Groups[1].Value,
                NumberStyles.None, CultureInfo.InvariantCulture, out var chapter)
                ? chapter : 0;
        }

        public static int CampaignChapter094(CampaignState campaign, string operationIdentity)
        {
            var explicitChapter = ChapterNumber094(operationIdentity);
            if (explicitChapter > 0) return explicitChapter;
            var progress = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            var active = ChapterNumber094(progress?.ActiveOperation?.ChapterId);
            if (active > 0) return active;
            return (progress?.CompletedChapterIds ?? Array.Empty<string>())
                .Select(ChapterNumber094).DefaultIfEmpty(0).Max();
        }

        /// <summary>
        /// Verification only selects a new profile if that exact full request
        /// hash was recorded by the existing commitment authority. Untagged
        /// historical requests and receipts retain byte-identical construction.
        /// </summary>
        public static EncounterLaunchRequest017D ForRequest094(
            CampaignState campaign, EncounterLaunchRequest017D legacy,
            int chapter, bool newCommit)
        {
            if (legacy == null) throw new ArgumentNullException(nameof(legacy));
            var oldProfile = ForChapter094(chapter, legacy.EnemyUnionCount);
            if (oldProfile == null) return legacy;
            var actualChapter = ChapterNumber094(legacy.ContractId);
            var statChapter = actualChapter > 0 ? actualChapter : chapter;
            var currentProfile = ForNewChapter138(chapter, legacy.EnemyUnionCount, statChapter);
            if (newCommit) return currentProfile.Apply094(legacy);
            var previous = oldProfile.Apply094(legacy);
            // Reconstruct every frozen version; only the exact durable request
            // authority may select one. Old V1 mixed Tower routes stay unchanged.
            var tower = legacy.RouteModifiers.Any(value => value != null &&
                value.StartsWith("TOWER_", StringComparison.OrdinalIgnoreCase));
            var v2 = tower ? null : ForNewChapter134(chapter, legacy.EnemyUnionCount, statChapter).Apply094(legacy);
            var v3 = tower ? null : ForNewChapter135(chapter, legacy.EnemyUnionCount, statChapter).Apply094(legacy);
            var current = tower ? null : currentProfile.Apply094(legacy);
            var development = campaign?.Guild?.Development;
            var recorded = new[] { legacy, previous, v2, v3, current }.Where(value => value != null && development != null &&
                development.HasAdventureAuthority(GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(value))).ToArray();
            if (recorded.Length > 1)
                throw new InvalidOperationException("ENEMY_FORCE094_AMBIGUOUS_COMMITMENT");
            return recorded.Length == 1 ? recorded[0] : legacy;
        }

        /// <summary>Read-only reveal copy: a saved battle wins over today's ramp.</summary>
        public static int PreviewUnionCount094(CampaignState campaign, string operationIdentity,
            int authoredCount, string cardId, string pendingCardId, string pendingBattleId)
        {
            var authored = Math.Max(1, Math.Min(10, authoredCount));
            if (!string.IsNullOrEmpty(pendingCardId) &&
                StringComparer.Ordinal.Equals(cardId, pendingCardId))
            {
                var pending = campaign?.Guild?.GuildCity?.PendingEncounter;
                if (pending != null && StringComparer.Ordinal.Equals(pending.BattleId, pendingBattleId))
                    return pending.EnemyUnionCount;
                var battle = campaign?.Battle;
                if (battle != null && StringComparer.Ordinal.Equals(battle.BattleId, pendingBattleId))
                    return battle.EnemyUnions.Count;
                // Missing linkage is handled by the existing command verifier.
                // Never advertise an uncommitted upgraded force for an old receipt.
                return authored;
            }
            return ForNewChapter138(CampaignChapter094(campaign, operationIdentity), authored)
                ?.UnionCount094 ?? authored;
        }

        public EncounterLaunchRequest017D Apply094(EncounterLaunchRequest017D request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.RouteModifiers.Any(value => value.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)))
                throw new InvalidOperationException("ENEMY_FORCE094_ALREADY_TAGGED");
            if (IsProgression134) RejectTowerMix134(request.RouteModifiers);
            var route = new List<string>(request.RouteModifiers) { Tag094 };
            return new EncounterLaunchRequest017D(request.RequestId, request.ContractId,
                request.ExpeditionId, request.BoardId, request.NodeId, request.EncounterId,
                request.BattleId, request.Objective, UnionCount094,
                request.CanonicalSeedIdentity, request.AlliedUnionIds, request.ReserveUnionIds,
                request.ObjectiveIds, route.AsReadOnly(), request.Supplies, request.Fatigue,
                request.Urgency, request.ReturnCheckpointId, request.PreBattleStateHash);
        }

        public static EnemyForceProfile094 ReadCommitted094(EncounterLaunchRequest017D request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var tags = request.RouteModifiers.Where(value =>
                value.StartsWith("ENEMY_FORCE094_", StringComparison.Ordinal)).ToArray();
            if (tags.Length == 0) return null;
            if (tags.Length != 1) throw new InvalidOperationException("ENEMY_FORCE094_MULTIPLE_PROFILES");
            if (tags[0].StartsWith(Prefix138, StringComparison.Ordinal))
            {
                RejectTowerMix134(request.RouteModifiers);
                var modern = ReadPressureTag138(tags[0]);
                if (modern.UnionCount094 != request.EnemyUnionCount)
                    throw new InvalidOperationException("ENEMY_FORCE094_CARDINALITY_INVALID");
                return modern;
            }
            if (tags[0].StartsWith(Prefix135, StringComparison.Ordinal))
            {
                RejectTowerMix134(request.RouteModifiers);
                var modern = ReadPressureTag135(tags[0]);
                if (modern.UnionCount094 != request.EnemyUnionCount)
                    throw new InvalidOperationException("ENEMY_FORCE094_CARDINALITY_INVALID");
                return modern;
            }
            if (tags[0].StartsWith(Prefix134, StringComparison.Ordinal))
            {
                RejectTowerMix134(request.RouteModifiers);
                var modern = ReadProgressionTag134(tags[0]);
                if (modern.UnionCount094 != request.EnemyUnionCount)
                    throw new InvalidOperationException("ENEMY_FORCE094_CARDINALITY_INVALID");
                return modern;
            }
            var match = TagToken094.Match(tags[0]);
            if (!match.Success) throw new InvalidOperationException("ENEMY_FORCE094_PROFILE_INVALID");
            var chapter = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var unions = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            var members = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            var profile = ForChapter094(chapter, unions);
            if (profile == null || unions != request.EnemyUnionCount ||
                profile.UnionCount094 != unions || profile.MemberCount094 != members ||
                !StringComparer.Ordinal.Equals(profile.Tag094, tags[0]))
                throw new InvalidOperationException("ENEMY_FORCE094_CARDINALITY_INVALID");
            return profile;
        }

        /// <summary>
        /// A versioned request cannot silently enter the old 3-member constructor.
        /// This guards both the public bridge and the existing committed M2 entry.
        /// Legacy and non-Guild-City calls keep their original behavior.
        /// </summary>
        public static void ValidateRoster094(EncounterLaunchRequest017D request,
            EncounterRoster070 roster)
        {
            if (request == null) return;
            var profile = ReadCommitted094(request);
            if (profile == null) return;
            if (roster == null)
                throw new InvalidOperationException("ENEMY_FORCE094_COMMITTED_ROSTER_REQUIRED");
            if (roster.Unions.Count != profile.UnionCount094 ||
                !StringComparer.Ordinal.Equals(roster.CanonicalSeedIdentity, request.CanonicalSeedIdentity))
                throw new InvalidOperationException("ENEMY_FORCE094_ROSTER_IDENTITY_MISMATCH");
            if (roster.Unions.Any(union => union.Members.Count < profile.MemberCount094 ||
                    union.Members.Count > 6))
                throw new InvalidOperationException("ENEMY_FORCE094_ROSTER_MEMBER_COUNT_INVALID");
            var members = roster.Unions.SelectMany(union => union.Members).ToArray();
            if (members.Select(member => member.MemberId).Distinct(StringComparer.Ordinal).Count() != members.Length ||
                members.Any(member => !member.MemberId.EndsWith(profile.SpawnSuffix094, StringComparison.Ordinal)))
                throw new InvalidOperationException("ENEMY_FORCE094_SPAWN_IDENTITY_MISMATCH");
            if (members.Count(member => IsNamedBossFamily094(member.FamilyId)) > 1)
                throw new InvalidOperationException("ENEMY_FORCE094_NAMED_BOSS_DUPLICATED");
        }

        public static bool TryArtTierFromSpawn094(string memberId, out int tier)
        {
            tier = 0;
            var id = memberId ?? string.Empty;
            var marker = id.IndexOf(SpawnMarker138, StringComparison.Ordinal) >= 0 ? SpawnMarker138 :
                id.IndexOf(SpawnMarker135, StringComparison.Ordinal) >= 0 ? SpawnMarker135 :
                id.IndexOf(SpawnMarker134, StringComparison.Ordinal) >= 0 ? SpawnMarker134 : SpawnMarker094;
            var index = id.LastIndexOf(marker, StringComparison.Ordinal);
            if (index < 0 || index + marker.Length + 3 != id.Length) return false;
            var token = id.Substring(index + marker.Length);
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out var chapter) ||
                chapter <= 10 || chapter > 999) return false;
            tier = Math.Max(1, Math.Min(10, (chapter + 4) / 5));
            return true;
        }

        internal static bool IsNamedBossFamily094(string familyId) =>
            StringComparer.Ordinal.Equals(familyId, "ENEMY_FAMILY_HINGE_EATER_COLOSSUS") ||
            StringComparer.Ordinal.Equals(familyId, "ENEMY_FAMILY_CAPTAIN_RAVEL") ||
            StringComparer.Ordinal.Equals(familyId, "ENEMY_FAMILY_GATEHEART_WARDEN");
    }
}
