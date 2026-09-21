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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroMasterRecruitIdentity096Tests
    {
        RecruitAutoGenerator010 _generator;
        M2CombatContent _combat;
        JArray _recipes;

        [OneTimeSetUp]
        public void Load096()
        {
            _generator = new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(HeroRosterAudit093.ContentRoot093));
            _combat = HeroRosterAudit093.LoadCombatContent093();
            _recipes = (JArray)JObject.Parse(File.ReadAllText(Path.Combine(
                HeroRosterAudit093.ContentRoot093, "RECRUIT_AUTOGEN_010", "DATA",
                "CLASS_WEAPON_BUILD_RECIPES_010.json")))["recipes"];
        }

        [TestCase(46, "CLASS_TACTICIAN", "WEAPON_FAMILY_ENGINEERING_TOOL", "TREE_CA002_WPN_ENGINEERING_N01", "TOOL")]
        [TestCase(179, "CLASS_SPELLBLADE", "WEAPON_FAMILY_SPEAR_POLEARM", "TREE_CA002_WPN_SPEAR_POLEARM_N01", "SPEAR")]
        public void AuthoredPrimarySignsThroughCoordinatorWithRealGearAndLegalArt096(
            int rosterId, string expectedClass, string family, string artId, string tag)
        {
            var hero = Hero096(rosterId);
            WithSave096(HeroRosterAudit093.CreateFixture093(hero), (path, before) =>
            {
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var view = coordinator.GuildCity017D.Applicants.Single();
                Assert.That(view.AuthoredRole096, Is.EqualTo(hero.Role));
                Assert.That(view.AuthoredWeaponStyle096, Is.EqualTo(hero.Weapon));
                Assert.That(view.EquipmentSummary, Is.EqualTo("Inventory upgrades or basic gear + armor"));
                var signing = coordinator.SignGuildCityApplicant017D(view.RecruitId);
                Assert.That(signing.Succeeded, Is.True, signing.Message);
                var assignment = coordinator.AssignRecruitToUnion(view.RecruitId, 0, 0);
                Assert.That(assignment.Succeeded, Is.True, assignment.Message);
                var saved = HeroRosterAudit093.Read093(path);
                var recruit = saved.Guild.Recruits.Single();
                Assert.That(recruit.AuthoredStableRecruitId, Is.EqualTo(hero.StableId));
                Assert.That(recruit.DisplayName, Is.EqualTo(hero.Name));
                Assert.That(recruit.ClassTendencyId, Is.EqualTo(expectedClass));
                Assert.That(_generator.Generate(recruit).FixedWeaponFamilyId, Is.EqualTo(family));
                Assert.That(recruit.Progression.LearnedArtIds, Does.Contain(artId));
                Assert.That(recruit.Equipment.Find(EquipmentSlotIds.MainHand).Item.EquipmentTags, Does.Contain(tag));
                Assert.That(recruit.Equipment.Find(EquipmentSlotIds.MainHand).Item.EquipmentTags, Does.Contain(family));
                Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
                Assert.That(recruit.Equipment.Assignments.Any(value =>
                    value.Item.EquipmentTags.Contains("SSS_SIGNATURE")), Is.False);
                Assert.That(saved.Guild.Inventory, Is.Empty);
                Assert.That(saved.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp - view.SigningCostTreasuryXp));

                var project = typeof(M2BattleCommandService).GetMethod("CreatePlayerUnions",
                    BindingFlags.NonPublic | BindingFlags.Static);
                var unions = (IReadOnlyList<BattleUnionState>)project.Invoke(null,
                    new object[] { saved, _combat, null, 10 });
                var union = unions.Single(value => value.Members.Any(member => member.MemberId == recruit.RecruitId));
                var member = union.Members.Single(value => value.MemberId == recruit.RecruitId);
                var candidates = _combat.Commands.Keys.SelectMany(command =>
                    M2BattleCommandService.CandidateArtsForVerification090(member, command, _combat,
                        union.CurrentAp, null, union.UnionId)).Distinct().ToArray();
                Assert.That(candidates, Does.Contain(artId), "Real learned primary Art must enter the lawful Forecast pool.");
                if (rosterId == 46)
                {
                    Assert.That(candidates, Does.Not.Contain("TREE_CA002_WPN_SWORD_N01"));
                    Assert.That(_combat.Arts.ContainsKey("TREE_FIREARMS"), Is.False,
                        "The adapter cannot pretend that the design-only firearm tree is implemented.");
                }

                var hash = CanonicalJson.Sha256Hex(saved);
                var reload = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                Assert.That(CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path)), Is.EqualTo(hash));
                var signedView = reload.GuildCity017D.Applicants.Single();
                Assert.That(signedView.IsSigned, Is.True);
                Assert.That(signedView.EquipmentSummary, Does.Contain("Basic"));
                Assert.That(signedView.EquipmentSummary, Does.Not.Contain("on recruit"));
                Assert.That(signedView.EquipmentSummary, Does.Not.Contain("No starter"));
                Assert.That(signedView.EquipmentSummary, Does.Contain("Travel Armor"));
            });
        }

        [Test]
        public void ExactOldCanonicalRecordsRegenerateTheirOriginalFamiliesAndArts096()
        {
            var path = Path.Combine(Application.dataPath, "Tests", "Fixtures", "HeroMasterLegacyRecords096.json");
            foreach (var row in JArray.Parse(File.ReadAllText(path)).Cast<JObject>())
            {
                var json = row.Value<string>("canonicalApplicantJson");
                var record = JsonConvert.DeserializeObject<OpeningRecruitRecord>(json);
                Assert.That(record.VariantFlags.Any(value => value.Contains("096")), Is.False);
                var generated = _generator.Generate(record);
                Assert.That(generated.FixedWeaponFamilyId, Is.EqualTo(row.Value<string>("expectedFamily")),
                    row.Value<string>("stableId") + ": old record replay");
                Assert.That(generated.StartingLearnedArtIds, Is.EquivalentTo(row["learnedArtIds"].Values<string>()));
                Assert.That(CanonicalJson.Serialize(record), Is.EqualTo(json), "Loading a legacy record must not add intent flags.");
            }
        }

        [Test]
        public void DariusExistingHybridRecipeAndExplicitSupportedAptitudesRemain096()
        {
            var darius = _generator.Generate(Record096(Hero096(175)));
            Assert.That(darius.FixedWeaponFamilyId, Is.EqualTo("WEAPON_FAMILY_HYBRID_RELIC_WEAPON"));
            Assert.That(darius.StartingLearnedArtIds, Does.Contain("TREE_CA002_WPN_HYBRID_RELIC_N01"));
            foreach (var id in new[] { 250, 299 })
            {
                var hero = Hero096(id);
                var record = Record096(hero);
                Assert.That(record.VariantFlags, Does.Not.Contain(HeroMasterPrimaryWeapon096.FirearmFallback),
                    "Existing gauntlet/tool interpretation is not the unsupported Sword fallback.");
            }
        }

        [TestCase(56)]
        [TestCase(222)]
        [TestCase(270)]
        [TestCase(273)]
        public void OtherAuthoredPolearmMagesKeepTheirExistingHybridRecipe096(int rosterId)
        {
            var hero = Hero096(rosterId);
            var record = Record096(hero);
            var generated = _generator.Generate(record);
            Assert.That(record.StartingClassId, Is.EqualTo("CLASS_MAGE"), hero.StableId);
            Assert.That(generated.FixedWeaponFamilyId,
                Is.EqualTo("WEAPON_FAMILY_HYBRID_RELIC_WEAPON"), hero.StableId);
            Assert.That(generated.StartingLearnedArtIds,
                Does.Contain("TREE_CA002_WPN_HYBRID_RELIC_N01"), hero.StableId);
            Assert.That(generated.DisplayName, Is.EqualTo(hero.Name));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 800f)]
        public void ActualRecruitmentBuilderSeparatesAuthoredStyleFromTrainingAndFits096(float width, float height)
        {
            WithSave096(HeroRosterAudit093.CreateFixture093(Hero096(46)), (path, before) =>
            {
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                // This deliberately minimal fixture has no frozen tutorial board.
                // Normal coordinator boot fills that metadata before UI exists.
                // Prove boot issued no reward, then measure rendering from the
                // exact initialized save/live state rather than the pre-boot fixture.
                var ready = HeroRosterAudit093.Read093(path);
                Assert.That(ready.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
                Assert.That(CanonicalJson.Serialize(ready.Guild.Recruits),
                    Is.EqualTo(CanonicalJson.Serialize(before.Guild.Recruits)));
                Assert.That(CanonicalJson.Serialize(ready.Guild.Inventory),
                    Is.EqualTo(CanonicalJson.Serialize(before.Guild.Inventory)));
                var readyHash = CanonicalJson.Sha256Hex(ready);
                var readySaveBytes = File.ReadAllBytes(path);
                var liveField = typeof(M1RuntimeCoordinator).GetField("_campaign",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(liveField, Is.Not.Null);
                Assert.That(CanonicalJson.Sha256Hex((CampaignState)liveField.GetValue(coordinator)),
                    Is.EqualTo(readyHash));
                var canvas = new GameObject("Recruit identity canvas096", typeof(RectTransform), typeof(Canvas));
                var owner = new GameObject("Recruit identity presenter096");
                try
                {
                    canvas.GetComponent<RectTransform>().sizeDelta =
                        M1FlowPresenter.ExpeditionCanvasSizeForVerification074(width, height);
                    var presenter = owner.AddComponent<M1FlowPresenter>();
                    presenter.enabled = false;
                    var state = coordinator.GuildCity017D;
                    typeof(M1FlowPresenter).GetMethod("BuildRecruitmentDecision074",
                        BindingFlags.NonPublic | BindingFlags.Instance).Invoke(presenter,
                        new object[] { canvas.transform, coordinator, state, state.Applicants.Single() });
                    typeof(M1FlowPresenter).GetMethod("BuildRecruitmentPortrait074",
                        BindingFlags.NonPublic | BindingFlags.Instance).Invoke(presenter,
                        new object[] { canvas.transform, state.Applicants.Single() });
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
                    var identity = canvas.GetComponentsInChildren<Text>().Single(value =>
                        value.name.StartsWith("Applicant Decision Identity 074", StringComparison.Ordinal));
                    Assert.That(identity.text, Does.Contain("ROLE  •  ARCANE ENGINEER"));
                    Assert.That(identity.text, Does.Contain("TRAINING  •  ENGINEERING"));
                    Assert.That(identity.text, Does.Contain("WEAPON STYLE  •  CRYSTAL CANNON"));
                    Assert.That(identity.text, Does.Contain("Inventory upgrades or basic gear + armor"));
                    Assert.That(identity.text, Does.Not.Contain("No starter gear"));
                    Assert.That(identity.text, Does.Not.Contain("WEAPON  •  SWORD"));
                    Assert.That(identity.preferredHeight, Is.LessThanOrEqualTo(identity.rectTransform.rect.height + 0.5f),
                        "The actual live decision builder must not clip its additional style line.");
                    Assert.That(canvas.GetComponentsInChildren<Text>().Any(value =>
                        value.text.Contains("NIXIE BRASSCOIL\nARCANE ENGINEER")), Is.True);
                    Assert.That(CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path)),
                        Is.EqualTo(readyHash), "Rendering cannot change the initialized campaign save.");
                    Assert.That(CanonicalJson.Sha256Hex((CampaignState)liveField.GetValue(coordinator)),
                        Is.EqualTo(readyHash), "Rendering cannot change canonical live state either.");
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(readySaveBytes),
                        "Read-only applicant rendering must not even rewrite an identical save envelope.");
                }
                finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(canvas); }
            });
        }

        [Test]
        public void AllAcceptedNewRecordsProduceExplicitPrimaryAuditIncludingUnmappedCases096()
        {
            var evidence = new List<object>();
            var physical = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TREE_POLEARM"] = "WEAPON_FAMILY_SPEAR_POLEARM",
                ["TREE_LONGBOW"] = "WEAPON_FAMILY_BOW",
                ["TREE_HEAVY_AXE"] = "WEAPON_FAMILY_AXE",
                ["TREE_HAMMER"] = "WEAPON_FAMILY_GREAT_WEAPON",
                ["TREE_MARTIAL"] = "WEAPON_FAMILY_GAUNTLET",
                ["TREE_BLADE"] = "WEAPON_FAMILY_SWORD"
            };
            var legacyClass = typeof(HeroMaster300CreatorRecruitProjection087).GetMethod(
                "CanonicalClassId", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var hero in HeroRosterAudit093.Catalog093.AcceptedHeroes.OrderBy(value => value.RosterId))
            {
                var record = Record096(hero);
                var previousJson = JObject.Parse(CanonicalJson.Serialize(record));
                previousJson["startingClassId"] = (string)legacyClass.Invoke(null, new object[] { hero });
                previousJson["variantFlags"] = new JArray(record.VariantFlags.Where(value => !value.Contains("096")));
                var previous = previousJson.ToObject<OpeningRecruitRecord>();
                var old = _generator.Generate(previous);
                var now = _generator.Generate(record);
                Assert.That(_recipes.Cast<JObject>().Any(recipe =>
                    recipe.Value<string>("classId") == now.StartingClassId &&
                    recipe.Value<string>("fixedWeaponFamilyId") == now.FixedWeaponFamilyId), Is.True, hero.StableId);
                Assert.That(now.StartingLearnedArtIds.All(_combat.Arts.ContainsKey), Is.True, hero.StableId);
                Assert.That(now.DisplayName, Is.EqualTo(hero.Name));
                Assert.That(now.BaseStats, Is.EquivalentTo(old.BaseStats), "No stat buffs: " + hero.StableId);
                var target = physical.TryGetValue(hero.ArtTree1, out var expected) ? expected :
                    hero.ArtTree1 == "TREE_FIREARMS" ? "WEAPON_FAMILY_ENGINEERING_TOOL" : "";
                var status = target.Length == 0 ? "AMBIGUOUS_PRIMARY_PRESERVED_REQUIRES_AUTHOR_REVIEW" :
                    now.FixedWeaponFamilyId == target ? "MATCHES_EXISTING_PHYSICAL_FAMILY" :
                    now.FixedWeaponFamilyId == "WEAPON_FAMILY_HYBRID_RELIC_WEAPON" &&
                    old.FixedWeaponFamilyId == now.FixedWeaponFamilyId ? "EXISTING_HYBRID_PRESERVED_NOT_PRIMARY_MATCH" :
                    "UNMAPPED_OR_UNSUPPORTED_REQUIRES_AUTHOR_REVIEW";
                evidence.Add(new { hero.StableId, hero.Name, hero.Role, hero.Weapon, hero.ArtTree1, hero.ArtTree2,
                    oldClass = old.StartingClassId, oldFamily = old.FixedWeaponFamilyId,
                    newClass = now.StartingClassId, newFamily = now.FixedWeaponFamilyId,
                    now.StartingLearnedArtIds, status });
            }
            Assert.That(evidence, Has.Count.EqualTo(251));
            var report = Path.Combine(Path.GetTempPath(), "HeroMasterPrimaryAudit096_" +
                Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(report, JsonConvert.SerializeObject(new {
                scope = "All251 accepted identity records; new-record canonical projection only. Unknown/ambiguous mappings are not counted as fixed; no natural acquisition or firearm skill claim.",
                rows = evidence }, Formatting.Indented));
            TestContext.WriteLine("PRIMARY096_AUDIT=" + report);
        }

        static HeroMaster300Hero087 Hero096(int id) =>
            HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == id);
        static OpeningRecruitRecord Record096(HeroMaster300Hero087 hero) =>
            hero.Rank == HeroMasterRank087.SS
                ? HeroMaster300CreatorRecruitProjection087.BuildOpeningRecord(hero)
                : HeroMaster300CreatorRecruitProjection087.BuildExpeditionApplicantRecord089(hero, "SKYHOME");
        static void WithSave096(CampaignState state, Action<string, CampaignState> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "sd_identity096_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { var path = Path.Combine(directory, "isolated.json"); HeroRosterAudit093.Write093(path, state); action(path, state); }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
