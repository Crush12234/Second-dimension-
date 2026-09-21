using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroMasterBattleBinding096Tests
    {
        M2CombatContent _combat;
        RecruitAutoGenerationCatalog010 _generated;
        RecruitAutoGenerator010 _generator;

        [OneTimeSetUp]
        public void Load096()
        {
            _combat = HeroRosterAudit093.LoadCombatContent093();
            _generated = RecruitAutoGenerationCatalog010.LoadFromContentRoot(HeroRosterAudit093.ContentRoot093);
            _generator = new RecruitAutoGenerator010(_generated);
        }

        static IEnumerable<TestCaseData> Accepted096() => HeroRosterAudit093.Catalog093.AcceptedHeroes
            .OrderBy(value => value.RosterId).Select(hero => new TestCaseData(hero.RosterId)
                .SetName("AcceptedHeroActuallyRetainsLearnedWeaponInBattle096_" + hero.StableId));

        [TestCaseSource(nameof(Accepted096))]
        public void EveryAcceptedOutfittedHeroRetainsItsLearnedWeaponRootAndLegalCandidate096(int rosterId)
        {
            var hero = Hero096(rosterId);
            var state = Signed096(hero);
            var recruit = state.Guild.Recruits.Single();
            var beforeHash = CanonicalJson.Sha256Hex(state);
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(recruit, out var profile), Is.True,
                "Only the exact accepted canonical record may enter generated battle authority.");
            var root = _combat.DeepProgression.Tree(profile.WeaponTreeId).RootNodeId;
            Assert.That(recruit.Progression.LearnedArtIds, Does.Contain(root));
            Assert.That(recruit.Progression.UnlockedTreeIds, Does.Contain(profile.WeaponTreeId));
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(recruit, _combat), Does.Contain(root));

            var union = Project096(state, _combat);
            var member = union.Members.Single();
            Assert.That(member.LearnedArtIds, Does.Contain(root), "The actual CreatePlayerUnions path must retain the weapon root.");
            var definition = _combat.Art(root);
            Assert.That(definition.IsForecastAction, Is.True, "Every assigned weapon root is an implemented active Art.");
            Assert.That(definition.RequiredEquipmentTags.Count == 0 ||
                definition.RequiredEquipmentTags.Any(member.EquipmentTags.Contains), Is.True, hero.StableId);
            Assert.That(member.CurrentMp, Is.GreaterThanOrEqualTo(definition.PersonalMpCost));
            Assert.That(union.CurrentAp, Is.GreaterThanOrEqualTo(definition.SharedApCost));
            Assert.That(Candidates096(union, _combat), Does.Contain(root),
                "A generic Basic/Assist fallback is not proof of the hero's actual learned weapon Art.");
            Assert.That(CanonicalJson.Sha256Hex(state), Is.EqualTo(beforeHash), "Projection grants or migrates nothing.");
        }

        [TestCase(46, "TREE_CA002_WPN_ENGINEERING_N01")]
        [TestCase(179, "TREE_CA002_WPN_SPEAR_POLEARM_N01")]
        public void ShippingCoordinatorOwnsTheTrustedBindingAfterActualPaidSigning096(int rosterId, string root)
        {
            var directory = Path.Combine(Path.GetTempPath(), "sd_live_binding096_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "isolated.json");
                HeroRosterAudit093.Write093(path, HeroRosterAudit093.CreateFixture093(Hero096(rosterId)));
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var applicant = coordinator.GuildCity017D.Applicants.Single();
                var result = coordinator.SignGuildCityApplicant017D(applicant.RecruitId);
                Assert.That(result.Succeeded, Is.True, result.Message);
                result = coordinator.AssignRecruitToUnion(applicant.RecruitId, 0, 0);
                Assert.That(result.Succeeded, Is.True, result.Message);
                var combatField = typeof(M1RuntimeCoordinator).GetField("_combatContent", BindingFlags.NonPublic | BindingFlags.Instance);
                var liveCombat = (M2CombatContent)combatField.GetValue(coordinator);
                Assert.That(liveCombat.HeroMasterGeneratedAuthority096, Is.Not.Null,
                    "Do not substitute a separately configured test-only content object.");
                var saved = HeroRosterAudit093.Read093(path);
                Assert.That(saved.Guild.TreasuryXp, Is.EqualTo(HeroRosterAudit093.FixtureTreasuryXp093 - applicant.SigningCostTreasuryXp));
                var liveProjectedUnion = Project096(saved, liveCombat);
                Assert.That(liveProjectedUnion.Members.Single().LearnedArtIds, Does.Contain(root));
                Assert.That(Candidates096(liveProjectedUnion, liveCombat), Does.Contain(root));
                // Coordinator boot legitimately normalizes this deliberately
                // minimal opening fixture. Prove its actual saved projection
                // above; only the isolated battle fixture below uses the existing
                // completed-opening test boundary. Never rewrite the live save.
                var encounterFixture = saved.With(saved.Guild,
                    HeroRosterAudit093.CreateFixture093(Hero096(rosterId)).OpeningFlow);
                var battle = HeroRosterAudit093.Require093(new M2BattleCommandService().StartEncounterBattle(
                    encounterFixture, liveCombat, "BINDING096_" + rosterId, "Isolated actual coordinator content binding", 1));
                var union = battle.Battle.PlayerUnions.Single();
                Assert.That(union.Members.Single().LearnedArtIds, Does.Contain(root));
                Assert.That(Candidates096(union, liveCombat), Does.Contain(root));
                Assert.That(battle.Battle.CommittedForecasts.SelectMany(value => value.MemberActions)
                    .Any(action => saved.Guild.Recruits.Single().Progression.LearnedArtIds.Contains(action.ArtId) &&
                        liveCombat.DeepProgression.TryNode(action.ArtId, out _)), Is.True,
                    "Actual Forecasts must use a learned deep Art, not only generic fallback. The retained weapon root is separately proven legal; an unused role Art may fairly win the first command.");
                var hash = CanonicalJson.Sha256Hex(saved);
                new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                Assert.That(CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path)), Is.EqualTo(hash));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test]
        public void UnconfiguredPureCombatContentDoesNotTrustAnAuthoredIdPrefix096()
        {
            var recruit = Signed096(Hero096(180)).Guild.Recruits.Single();
            var raw = M2CombatContent.LoadFromDirectory(HeroRosterAudit093.ContentRoot093);
            Assert.That(raw.HeroMasterGeneratedAuthority096, Is.Null);
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(recruit, raw)
                .Any(art => raw.DeepProgression.TryNode(art, out _)), Is.False);
        }

        [TestCase("unknown")]
        [TestCase("swapped")]
        [TestCase("quarantined")]
        [TestCase("known-fixed-signature")]
        [TestCase("signature")]
        [TestCase("protected")]
        [TestCase("canonical-name")]
        [TestCase("canonical-aptitude")]
        [TestCase("canonical-extra-flag")]
        [TestCase("outer-class")]
        [TestCase("outer-race")]
        public void UnknownForgedAndFixedSignatureAuthoritiesCannotBorrowGeneratedArts096(string mutation)
        {
            var recruit = Signed096(Hero096(180)).Guild.Recruits.Single();
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(recruit, out _), Is.True,
                "Warm the cache first; a modified outer identity must still be rejected.");
            var token = JObject.FromObject(recruit);
            switch (mutation)
            {
                case "unknown": token["AuthoredStableRecruitId"] = "HERO_REC_UNKNOWN096"; break;
                case "swapped": token["AuthoredStableRecruitId"] = "HERO_REC_046"; break;
                case "quarantined":
                    var quarantine = HeroRosterAudit093.Catalog093.QuarantinedHeroes.First();
                    Assert.That(HeroRosterAudit093.Catalog093.TryGetAcceptedHero(quarantine.StableId, out _), Is.False,
                        "Use an actually quarantined identity after the authored Veyra race repair.");
                    token["AuthoredStableRecruitId"] = quarantine.StableId;
                    break;
                case "known-fixed-signature": token["SignatureId"] = "SIG_W01_05"; break;
                case "signature": token["SignatureId"] = "SSS_FIXED_UNKNOWN096"; break;
                case "protected": token["AuthorityKind"] = (int)RecruitAuthorityKind.Founder; break;
                case "outer-class": token["ClassTendencyId"] = "CLASS_MAGE"; break;
                case "outer-race": token["RaceId"] = "GOBLIN"; break;
                default:
                    var record = JObject.Parse(recruit.CanonicalApplicantJson);
                    if (mutation == "canonical-name") record["displayName"] = "Not the accepted hero";
                    if (mutation == "canonical-aptitude") ((JObject)record["weaponAptitudes"])["BOW"] = 1000;
                    if (mutation == "canonical-extra-flag") ((JArray)record["variantFlags"]).Add("GRANT_ALL_TREES");
                    token["CanonicalApplicantJson"] = record.ToString(Formatting.None);
                    break;
            }
            var forged = token.ToObject<RecruitState>();
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(forged, out _), Is.False);
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(forged, _combat)
                .Any(art => _combat.DeepProgression.TryNode(art, out _)), Is.False);
            var member = Project096(Replace096(Signed096(Hero096(180)), forged), _combat).Members.Single();
            Assert.That(M2DeepArtRuntime070.TryGetNextLearning(forged, member,
                "TREE_CA002_WPN_HYBRID_RELIC_N01", 999, _combat, out _), Is.False);
        }

        [Test]
        public void LearnedUnlockedAssignedAndEquippedGatesStillApply096()
        {
            var state = Signed096(Hero096(180));
            var recruit = state.Guild.Recruits.Single();
            const string tree = "TREE_CA002_WPN_HYBRID_RELIC";
            const string root = tree + "_N01";
            const string echo = tree + "_N02";
            const string foreign = "TREE_CA002_WPN_BOW";
            var token = JObject.FromObject(recruit);
            var progress = (JObject)token["Progression"];
            ((JArray)progress["UnlockedTreeIds"]).Add(foreign);
            ((JArray)progress["LearnedArtIds"]).Add(foreign + "_N01");
            var altered = token.ToObject<RecruitState>();
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(altered, _combat), Does.Not.Contain(foreign + "_N01"));
            progress["UnlockedTreeIds"] = new JArray(recruit.Progression.UnlockedTreeIds.Where(value => value != tree));
            altered = token.ToObject<RecruitState>();
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(altered, _combat), Does.Not.Contain(root).And.Not.Contain(echo));
            token = JObject.FromObject(recruit);
            ((JObject)token["Progression"])["LearnedArtIds"] = new JArray(recruit.Progression.LearnedArtIds.Where(value => value != echo));
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(token.ToObject<RecruitState>(), _combat), Does.Not.Contain(echo));
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(recruit.WithEquipment(EquipmentLoadoutState.Empty()), _combat),
                Does.Not.Contain(root).And.Not.Contain(echo));

            var union = Project096(state, _combat);
            var member = union.Members.Single();
            Assert.That(M2DeepArtRuntime070.TryGetNextLearning(recruit, member, root, 1, _combat, out var next), Is.True,
                "The trusted Hero Master must use existing meaningful-use learning rather than stop at the unknown fixed plan.");
            Assert.That(next.TreeId, Is.EqualTo(tree));
            Assert.That(next.SourceArtId, Is.EqualTo(root));
            Assert.That(next.TargetArtId, Is.Not.EqualTo(root).And.Not.EqualTo(echo));
            Assert.That(_combat.Art(echo).PersonalMpCost, Is.Zero, "Echo Cast is authored AP-only; do not manufacture an MP price.");
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(recruit, out var generated), Is.True);
            Assert.That(generated.MysticTreeId, Is.Not.Empty);
            var adapter = new HeroMaster300DeepProgressionAdapter089(HeroRosterAudit093.Catalog093,
                _generator, _combat.DeepTreeProgression);
            // The isolated MP-gate fixture unlocks its assigned Mystic root using
            // the real existing progression authority, not an injected Art ID.
            // This is a legality test, not proof of natural acquisition pacing.
            var withMystic = recruit.WithProgression(adapter.UnlockEarnableTree089(
                recruit, recruit.Progression, generated.MysticTreeId));
            var mysticRoot = _combat.DeepProgression.Tree(generated.MysticTreeId).RootNodeId;
            var mysticUnion = Project096(Replace096(state, withMystic), _combat);
            Assert.That(Candidates096(mysticUnion, _combat), Does.Contain(mysticRoot));
            Assert.That(_combat.Art(mysticRoot).PersonalMpCost, Is.GreaterThan(0));
            var memberToken = JObject.FromObject(mysticUnion.Members.Single());
            memberToken["CurrentMp"] = 0;
            var dry = memberToken.ToObject<BattleMemberState>();
            Assert.That(_combat.Commands.Keys.SelectMany(command => M2BattleCommandService.CandidateArtsForVerification090(
                dry, command, _combat, mysticUnion.CurrentAp, null, mysticUnion.UnionId)), Does.Not.Contain(mysticRoot));
            Assert.That(_combat.Art(echo).SharedApCost, Is.GreaterThan(0));
            Assert.That(_combat.Commands.Keys.SelectMany(command => M2BattleCommandService.CandidateArtsForVerification090(
                member, command, _combat, 0, null, union.UnionId)), Does.Not.Contain(echo));
        }

        [Test]
        public void ExactRecordedPre096CanonicalRecordsKeepTheirOldFamilyAndBattleRoots096()
        {
            var rows = JArray.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
                "Tests", "Fixtures", "HeroMasterLegacyRecords096.json")));
            foreach (var row in rows.Cast<JObject>())
            {
                var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == row.Value<string>("stableId"));
                var state = Signed096(hero);
                var canonical = row.Value<string>("canonicalApplicantJson");
                var record = JsonConvert.DeserializeObject<OpeningRecruitRecord>(canonical);
                var token = JObject.FromObject(state.Guild.Recruits.Single());
                token["CanonicalApplicantJson"] = canonical;
                token["ClassTendencyId"] = record.StartingClassId;
                token["Progression"] = JObject.FromObject(RecruitProgressionState.Default());
                token["Equipment"] = JObject.FromObject(EquipmentLoadoutState.Empty());
                var legacy = new RecruitAutoGenerationSigningService010(new M1CommandService(), _generator)
                    .InitializeRecruit(token.ToObject<RecruitState>());
                Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(legacy, out var profile), Is.True, hero.StableId);
                Assert.That(profile.FixedWeaponFamilyId, Is.EqualTo(row.Value<string>("expectedFamily")), hero.StableId);
                Assert.That(legacy.Progression.LearnedArtIds, Is.EquivalentTo(row["learnedArtIds"].Values<string>()));
                var before = HeroRosterAudit093.CreateFixture093(hero);
                var equipped = new RecruitStarterEquipment094(_generated, _combat)
                    .ApplyToNewRecruits(before, Replace096(state, legacy));
                var ready = equipped.Guild.Recruits.Single();
                Assert.That(ready.CanonicalApplicantJson, Is.EqualTo(canonical));
                var root = _combat.DeepProgression.Tree(profile.WeaponTreeId).RootNodeId;
                var union = Project096(equipped, _combat);
                Assert.That(union.Members.Single().LearnedArtIds, Does.Contain(root), hero.StableId);
                Assert.That(Candidates096(union, _combat), Does.Contain(root), hero.StableId);
                var roundTrip = JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(equipped));
                Assert.That(CanonicalJson.Sha256Hex(roundTrip), Is.EqualTo(CanonicalJson.Sha256Hex(equipped)));
            }
        }

        [Test]
        public void GenuineLegacyVeyraKeepsHerFixedAuthorityDespiteHeroMasterQuarantine096()
        {
            var raw = M2CombatContent.LoadFromDirectory(HeroRosterAudit093.ContentRoot093);
            var plan = raw.DeepProgression.RecruitPlan("SIGREC_VEYRA_ASHGLASS");
            var veyra = new RecruitState(plan.StableRecruitId, 200, 200, 30, 30, plan.DisplayName,
                RecruitOriginKind.Signature, plan.SignatureRecruitId, "DEMON_HERITAGE", "WORLD_GATE_01",
                "CLASS_SPELLBLADE", "Leader", 7350, RecruitAuthorityKind.Normal,
                string.Empty, string.Empty, EquipmentLoadoutState.Empty(), true,
                string.Empty, plan.StableRecruitId, 70, 70, RecruitProgressionState.Default());
            Assert.That(HeroMasterGeneratedBattleAuthority096.ClaimsHeroMasterSource096(veyra), Is.False);
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(veyra, out _), Is.False);
            var expected = M2DeepArtRuntime070.BattleLearnedArts(veyra, raw);
            Assert.That(expected.Any(art => raw.DeepProgression.TryNode(art, out _)), Is.True);
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(veyra, _combat), Is.EquivalentTo(expected));
            var learned = new List<string>();
            var mastery = new List<BattleArtProgressState>();
            M2DeepArtRuntime070.MergeActiveLearnedArts(veyra, _combat, learned, mastery);
            Assert.That(learned, Is.EquivalentTo(expected));
        }

        [TestCase("{}", "[{},[],null,17,\"HERO_MASTER_300\"]")]
        [TestCase("[]", "[\"HERO_MASTER_300\",{}]")]
        [TestCase("\"HERO_MASTER_300_FAKE\"", "[{},[],null,17]")]
        public void MalformedTypedSourceFieldsStillRejectClaimedHeroMasterWithoutBorrowingVeyra096(
            string seedJson, string flagsJson)
        {
            var token = JObject.FromObject(Signed096(Hero096(180)).Guild.Recruits.Single());
            token["RecruitId"] = "SIGREC_VEYRA_ASHGLASS";
            token["AuthoredStableRecruitId"] = "SIGREC_VEYRA_ASHGLASS";
            var canonical = JObject.Parse(token.Value<string>("CanonicalApplicantJson"));
            canonical["generationSeed"] = JToken.Parse(seedJson);
            canonical["variantFlags"] = JToken.Parse(flagsJson);
            token["CanonicalApplicantJson"] = canonical.ToString(Formatting.None);
            var forged = token.ToObject<RecruitState>();
            Assert.That(HeroMasterGeneratedBattleAuthority096.ClaimsHeroMasterSource096(forged), Is.True,
                "A malformed adjacent field must neither throw nor hide the surviving Hero Master source marker.");
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(forged, out _), Is.False);
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(forged, _combat)
                .Any(art => _combat.DeepProgression.TryNode(art, out _)), Is.False);
        }

        static HeroMaster300Hero087 Hero096(int id) => HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == id);
        static CampaignState Signed096(HeroMaster300Hero087 hero) => HeroRosterAudit093.SignOutfitAndPlace093(
            HeroRosterAudit093.CreateFixture093(hero), hero, new HeroRosterAuditRow093());
        static CampaignState Replace096(CampaignState state, RecruitState recruit) => state.With(
            state.Guild.With(state.Guild.TreasuryXp, new[] { recruit }, state.Guild.Unions, state.Guild.Inventory), state.OpeningFlow);
        static BattleUnionState Project096(CampaignState state, M2CombatContent combat) =>
            ((IReadOnlyList<BattleUnionState>)typeof(M2BattleCommandService).GetMethod("CreatePlayerUnions",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { state, combat, null, 10 })).Single();
        static string[] Candidates096(BattleUnionState union, M2CombatContent combat) => combat.Commands.Keys.SelectMany(command =>
            M2BattleCommandService.CandidateArtsForVerification090(union.Members.Single(), command, combat,
                union.CurrentAp, null, union.UnionId)).Distinct().ToArray();
    }
}
