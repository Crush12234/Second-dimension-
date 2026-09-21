using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Read-only binding of accepted Hero Master records to their existing generated
    /// tree authority. An authored visual ID is not a fixed legacy recruit plan.
    /// No unknown named actor receives the procedural fallback merely by prefix.
    /// </summary>
    public sealed class HeroMasterGeneratedBattleAuthority096
    {
        readonly HeroMaster300Catalog087 _heroes;
        readonly DeepProgressionCatalog070 _deep;
        readonly HeroMaster300DeepProgressionAdapter089 _adapter;
        readonly Dictionary<string, GeneratedRecruitProfile010> _validated =
            new Dictionary<string, GeneratedRecruitProfile010>(StringComparer.Ordinal);

        public HeroMasterGeneratedBattleAuthority096(HeroMaster300Catalog087 heroes,
            RecruitAutoGenerationCatalog010 generated, DeepProgressionCatalog070 deep)
        {
            _heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
            _deep = deep ?? throw new ArgumentNullException(nameof(deep));
            _adapter = new HeroMaster300DeepProgressionAdapter089(heroes,
                new RecruitAutoGenerator010(generated ?? throw new ArgumentNullException(nameof(generated))),
                new RecruitTreeProgressionService070(deep));
        }

        public bool TryDescribe(RecruitState recruit, out GeneratedRecruitProfile010 profile)
        {
            profile = null;
            if (recruit == null || recruit.OriginKind != RecruitOriginKind.Procedural ||
                recruit.AuthorityKind != RecruitAuthorityKind.Normal ||
                !string.IsNullOrWhiteSpace(recruit.SignatureId) ||
                string.IsNullOrWhiteSpace(recruit.CanonicalApplicantJson) ||
                !_heroes.TryGetAcceptedHero(recruit.AuthoredStableRecruitId, out var hero)) return false;
            var expectedId = hero.Rank == HeroMasterRank087.SS
                ? HeroMaster300CreatorRecruitProjection087.RecruitIdFor(hero)
                : HeroMaster300CreatorRecruitProjection087.ExpeditionApplicantRecruitIdFor089(hero);
            if (recruit.RecruitId != expectedId || recruit.DisplayName != hero.Name) return false;
            var key = hero.StableId + "|" + recruit.RecruitId + "|" + recruit.CanonicalApplicantJson;
            // Main-thread projections and an isolated Tower worker may validate
            // the same cold record concurrently. Only memo access is serialized;
            // canonical validation and profile generation stay outside this lock.
            GeneratedRecruitProfile010 cached;
            lock (_validated) _validated.TryGetValue(key, out cached);
            if (cached != null)
            {
                if (cached.RaceId != recruit.RaceId || cached.StartingClassId != recruit.ClassTendencyId) return false;
                profile = cached;
                return true;
            }
            try
            {
                var token = JObject.Parse(recruit.CanonicalApplicantJson);
                var record = token.ToObject<OpeningRecruitRecord>();
                if (record == null || record.RecruitId != recruit.RecruitId ||
                    record.DisplayName != recruit.DisplayName || record.RaceId != recruit.RaceId ||
                    record.StartingClassId != recruit.ClassTendencyId) return false;
                var expected = hero.Rank == HeroMasterRank087.SS
                    ? HeroMaster300CreatorRecruitProjection087.BuildOpeningRecord(hero)
                    : HeroMaster300CreatorRecruitProjection087.BuildExpeditionApplicantRecord089(
                        hero, record.HomeCommunityId);
                var current = JObject.FromObject(expected);
                // Older valid saves retain their original canonical class/family.
                // Only the explicitly introduced 096 fields differ in this legacy
                // reconstruction; no arbitrary saved flag or aptitude is trusted.
                var legacy = (JObject)current.DeepClone();
                legacy["startingClassId"] = HeroMaster300CreatorRecruitProjection087.LegacyClassIdForValidation096(hero);
                legacy["variantFlags"] = new JArray(expected.VariantFlags.Where(flag =>
                    !flag.StartsWith(HeroMasterPrimaryWeapon096.PrimaryPrefix, StringComparison.Ordinal) &&
                    flag != HeroMasterPrimaryWeapon096.FirearmFallback));
                if (!JToken.DeepEquals(token, current) && !JToken.DeepEquals(token, legacy)) return false;
                profile = _adapter.DescribeValidatedProfile089(recruit);
                lock (_validated)
                {
                    if (_validated.TryGetValue(key, out cached)) profile = cached;
                    else _validated.Add(key, profile);
                }
                return true;
            }
            catch (Newtonsoft.Json.JsonException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (ArgumentException) { return false; }
            catch (KeyNotFoundException) { return false; }
        }

        // These markers only identify which source must be validated. They never
        // confer authority. In particular, a malformed Hero Master record cannot
        // borrow a legacy fixed plan merely by changing its authored stable ID.
        public static bool ClaimsHeroMasterSource096(RecruitState recruit)
        {
            if (recruit == null) return false;
            var recruitId = recruit.RecruitId ?? string.Empty;
            if (recruitId.StartsWith(HeroMaster300CreatorRecruitProjection087.RecruitIdPrefix, StringComparison.Ordinal) ||
                recruitId.StartsWith(HeroMaster300CreatorRecruitProjection087.ExpeditionApplicantRecruitIdPrefix, StringComparison.Ordinal)) return true;
            if (string.IsNullOrWhiteSpace(recruit.CanonicalApplicantJson)) return false;
            try
            {
                var record = JObject.Parse(recruit.CanonicalApplicantJson);
                var seed = record["generationSeed"];
                return seed?.Type == JTokenType.String &&
                    seed.Value<string>().StartsWith("HERO_MASTER_300_", StringComparison.Ordinal) ||
                    record["variantFlags"] is JArray flags && flags.Any(flag =>
                        flag.Type == JTokenType.String && flag.Value<string>() == "HERO_MASTER_300");
            }
            catch (Newtonsoft.Json.JsonException) { return false; }
        }

        public HashSet<string> ActiveTrees(RecruitState recruit, GeneratedRecruitProfile010 profile)
        {
            var assigned = _adapter.ValidateGeneratedProfile089(profile);
            return new HashSet<string>(assigned.Where(treeId =>
                recruit.Progression.UnlockedTreeIds.Contains(treeId) &&
                recruit.Progression.LearnedArtIds.Contains(_deep.Tree(treeId).RootNodeId)), StringComparer.Ordinal);
        }
    }
}
