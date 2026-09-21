using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Recruitment;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroMaster300Catalog087Tests
    {
        [Test]
        public void ValidCatalogProjectsBASToExistingApplicantFlowAndKeepsSsCodeOnly087()
        {
            var root = BuildValidCatalog();

            var catalog = HeroMaster300Catalog087.FromJson(root.ToString(Formatting.None));

            Assert.That(catalog.SourceRecordCount, Is.EqualTo(300));
            Assert.That(catalog.AcceptedHeroes.Count, Is.EqualTo(300));
            Assert.That(catalog.QuarantinedHeroes, Is.Empty);
            Assert.That(catalog.NormalApplicantCandidates.Count, Is.EqualTo(270));
            Assert.That(catalog.NormalApplicantCandidates.All(hero =>
                hero.Rank != HeroMasterRank087.SS), Is.True);
            Assert.That(catalog.IsNormalApplicantEligible(1), Is.True);
            Assert.That(catalog.IsNormalApplicantEligible(271), Is.False);

            Assert.That(catalog.TryFindSsByCode(
                "  sd-ss-01-hero_271  ", out var ssHero), Is.True);
            Assert.That(ssHero.RosterId, Is.EqualTo(271));
            Assert.That(catalog.TryFindSsByCode("SDG-SS-271-0000010F", out _), Is.False,
                "The all-rank generation code is not an SS redemption code.");
        }

        [Test]
        public void PlaceholderAndInstructionResidueAreQuarantinedAndCannotEnterLookups087()
        {
            var root = BuildValidCatalog();
            var heroes = (JArray)root["heroes"];
            ((JObject)heroes[110])["weapon"] = "TBD";
            ((JObject)heroes[270])["race"] = "AUDIT_LIVE_REGISTRY";

            var catalog = HeroMaster300Catalog087.FromJson(root.ToString(Formatting.None));

            Assert.That(catalog.AcceptedHeroes.Count, Is.EqualTo(298));
            Assert.That(catalog.QuarantinedHeroes.Select(value => value.RosterId),
                Is.EqualTo(new[] { 111, 271 }));
            Assert.That(catalog.NormalApplicantCandidates.Count, Is.EqualTo(269));
            Assert.That(catalog.IsNormalApplicantEligible(111), Is.False);
            Assert.That(catalog.TryGetAcceptedHero(271, out _), Is.False);
            Assert.That(catalog.TryFindSsByCode("SD-SS-01-HERO_271", out _), Is.False,
                "A quarantined SS hero must not be redeemable.");
            Assert.That(catalog.TryGetQuarantine(111, out var missingWeapon), Is.True);
            Assert.That(missingWeapon.Reasons, Has.Some.Contains("weapon"));
            Assert.That(catalog.TryGetQuarantine(271, out var malformedRace), Is.True);
            Assert.That(malformedRace.Reasons, Has.Some.Contains("race"));
        }

        [Test]
        public void DuplicateOrMismatchedIdentityAndCodeAuthorityFailsClosed087()
        {
            var duplicateId = BuildValidCatalog();
            ((JObject)((JArray)duplicateId["heroes"])[1])["roster_id"] = 1;
            Assert.That(
                () => HeroMaster300Catalog087.FromJson(duplicateId.ToString(Formatting.None)),
                Throws.TypeOf<InvalidDataException>());

            var mismatchedCode = BuildValidCatalog();
            ((JObject)((JArray)mismatchedCode["heroes"])[0])["generation_code"] =
                "SDG-A-001-00000001";
            Assert.That(
                () => HeroMaster300Catalog087.FromJson(mismatchedCode.ToString(Formatting.None)),
                Throws.TypeOf<InvalidDataException>());

            var duplicateSsCode = BuildValidCatalog();
            var ssHeroes = (JArray)duplicateSsCode["heroes"];
            ((JObject)ssHeroes[271])["ss_generation_code"] = "SD-SS-01-HERO_271";
            Assert.That(
                () => HeroMaster300Catalog087.FromJson(duplicateSsCode.ToString(Formatting.None)),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void CountAndRankDistributionMustRemainTheDeclaredThreeHundredHeroAuthority087()
        {
            var wrongCount = BuildValidCatalog();
            wrongCount["count"] = 299;
            Assert.That(
                () => HeroMaster300Catalog087.FromJson(wrongCount.ToString(Formatting.None)),
                Throws.TypeOf<InvalidDataException>());

            var wrongDeclaredRanks = BuildValidCatalog();
            ((JObject)wrongDeclaredRanks["rank_distribution"])["SS"] = 29;
            Assert.That(
                () => HeroMaster300Catalog087.FromJson(wrongDeclaredRanks.ToString(Formatting.None)),
                Throws.TypeOf<InvalidDataException>());

            var wrongActualRanks = BuildValidCatalog();
            var first = (JObject)((JArray)wrongActualRanks["heroes"])[0];
            first["rank"] = "A";
            first["generation_code"] = "SDG-A-001-00000001";
            Assert.That(
                () => HeroMaster300Catalog087.FromJson(wrongActualRanks.ToString(Formatting.None)),
                Throws.TypeOf<InvalidDataException>());
        }

        private static JObject BuildValidCatalog()
        {
            var heroes = new JArray();
            var ssOrdinal = 0;
            for (var rosterId = 1; rosterId <= 300; rosterId++)
            {
                var rank = rosterId <= 90
                    ? "B"
                    : rosterId <= 180
                        ? "A"
                        : rosterId <= 270
                            ? "S"
                            : "SS";
                if (rank == "SS") ssOrdinal++;

                heroes.Add(new JObject
                {
                    ["roster_id"] = rosterId,
                    ["stable_id"] = "HERO_REC_" + rosterId.ToString("D3"),
                    ["name"] = "Hero " + rosterId,
                    ["race"] = "Human",
                    ["role"] = "Guardian",
                    ["weapon"] = "Spear and shield",
                    ["source_package"] = "HERO_MASTER_TEST_SOURCE.zip",
                    ["rank"] = rank,
                    ["game_entity_id"] = "SIG_HERO_" + rosterId.ToString("D3"),
                    ["generation_code"] = "SDG-" + rank + "-"
                                          + rosterId.ToString("D3") + "-"
                                          + rosterId.ToString("X8"),
                    ["art_tree_1"] = "TREE_POLEARM",
                    ["art_tree_2"] = "TREE_GUARD_PROTECT",
                    ["HP"] = 200,
                    ["AP"] = 20,
                    ["STR"] = 20,
                    ["DEF"] = 20,
                    ["AGI"] = 20,
                    ["MAG"] = 20,
                    ["RES"] = 20,
                    ["stat_budget"] = 100,
                    ["recruit_cost_xp"] = rank == "B" ? 1200
                        : rank == "A" ? 3500
                        : rank == "S" ? 9000
                        : 30000,
                    ["ss_generation_code"] = rank == "SS"
                        ? "SD-SS-" + ssOrdinal.ToString("D2") + "-HERO_" + rosterId
                        : string.Empty,
                    ["final_art_id"] = rank == "SS"
                        ? "FINAL_" + rosterId.ToString("D3") + "_TEST"
                        : string.Empty,
                    ["final_art_name"] = rank == "SS" ? "Final Test" : string.Empty,
                    ["final_art_type"] = rank == "SS" ? "GUARD" : string.Empty,
                    ["final_art_effect"] = rank == "SS" ? "Test effect." : string.Empty,
                    ["final_art_unlock"] = rank == "SS" ? "Test mastery trial." : string.Empty
                });
            }

            return new JObject
            {
                ["count"] = 300,
                ["rank_distribution"] = new JObject
                {
                    ["B"] = 90,
                    ["A"] = 90,
                    ["S"] = 90,
                    ["SS"] = 30
                },
                ["heroes"] = heroes
            };
        }
    }
}
