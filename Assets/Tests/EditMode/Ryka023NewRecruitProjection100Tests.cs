using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public sealed class Ryka023NewRecruitProjection100Tests
    {
        const string FocusRoot = "TREE_CA002_WPN_FOCUS_N01";
        const string FocusSecond = "TREE_CA002_WPN_FOCUS_N02";
        const string CatalogHash = "E2E186E217AA5B5D15A8C36366B2A3C192BD2FD71DB932040AC732D52A367845";
        RecruitAutoGenerationCatalog010 _generated;
        RecruitAutoGenerator010 _generator;
        M2CombatContent _combat;

        [OneTimeSetUp]
        public void Load100()
        {
            _generated = RecruitAutoGenerationCatalog010.LoadFromContentRoot(HeroRosterAudit093.ContentRoot093);
            _generator = new RecruitAutoGenerator010(_generated);
            _combat = HeroRosterAudit093.LoadCombatContent093();
        }

        [Test]
        public void ExactAuthoredRykaProjectsMageWithoutChangingCatalogStatsCostOrAliases100()
        {
            var hero = Hero100();
            Assert.That(hero.Role, Is.EqualTo("Elementalist"));
            Assert.That(hero.Weapon, Is.EqualTo("Flame focus"));
            Assert.That(hero.ArtTree1, Is.EqualTo("TREE_BLADE"));
            Assert.That(hero.ArtTree2, Is.EqualTo("TREE_COMBAT_MASTERY"));
            Assert.That(FileHash100(Path.Combine(Application.dataPath, "Resources", "SecondDimension",
                "HeroMaster300", "Data", "HERO_MASTER_001_300.json")), Is.EqualTo(CatalogHash),
                "This projection fix must not edit the 300 authored rows.");
            var current = HeroMaster300CreatorRecruitProjection087.BuildExpeditionApplicantRecord089(hero, "SKYHOME");
            var old = JObject.Parse(Legacy100().CanonicalApplicantJson).ToObject<OpeningRecruitRecord>();
            Assert.That(current.StartingClassId, Is.EqualTo("CLASS_MAGE"));
            Assert.That(old.StartingClassId, Is.EqualTo("CLASS_WARRIOR"));
            var previous = JObject.Parse(CanonicalJson.Serialize(old));
            var now = JObject.Parse(CanonicalJson.Serialize(current));
            now["startingClassId"] = previous["startingClassId"];
            Assert.That(JToken.DeepEquals(now, previous), Is.True,
                "Only new startingClassId changes; stats, price, aptitude, identity and raw applicant fields stay intact.");
            var profile = _generator.Generate(current);
            Assert.That(profile.FixedWeaponFamilyId, Is.EqualTo("WEAPON_FAMILY_FOCUS"));
            Assert.That(profile.StartingLearnedArtIds, Does.Contain(FocusRoot).And.Contain(FocusSecond));
            Assert.That(profile.BaseStats, Is.EquivalentTo(_generator.Generate(old).BaseStats));
            Assert.That(profile.MysticTreeId, Is.Empty,
                "Do not silently invent a Flame-tree unlock or raise the existing aptitude threshold.");
        }

        [Test]
        public void ActualCoordinatorSignsOutfitsSavesReloadsAndResolvesLegalMysticForecast100()
        {
            // Explicit isolated lead + fixture wallet. Signing, debit, equipment,
            // Union placement and saved player projection remain real authorities.
            var fixture = HeroRosterAudit093.CreateFixture093(Hero100());
            WithSave100(fixture, (path, before) =>
            {
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var applicant = coordinator.GuildCity017D.Applicants.Single();
                var result = coordinator.SignGuildCityApplicant017D(applicant.RecruitId);
                Assert.That(result.Succeeded, Is.True, result.Message);
                result = coordinator.AssignRecruitToUnion(applicant.RecruitId, 0, 0);
                Assert.That(result.Succeeded, Is.True, result.Message);
                var saved = HeroRosterAudit093.Read093(path);
                var recruit = saved.Guild.Recruits.Single();
                Assert.That(recruit.AuthoredStableRecruitId, Is.EqualTo("HERO_REC_023"));
                Assert.That(recruit.DisplayName, Is.EqualTo("Ryka Flameborn"));
                Assert.That(recruit.ClassTendencyId, Is.EqualTo("CLASS_MAGE"));
                Assert.That(saved.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp - applicant.SigningCostTreasuryXp));
                Assert.That(recruit.Progression.LearnedArtIds, Does.Contain(FocusRoot).And.Contain(FocusSecond));
                var tags = recruit.Equipment.Find(EquipmentSlotIds.MainHand).Item.EquipmentTags;
                Assert.That(tags, Does.Contain("FOCUS_TOOL").And.Contain("WAND").And.Contain("WEAPON_FAMILY_FOCUS"));
                Assert.That(tags, Does.Not.Contain("SWORD").And.Not.Contain("SSS_SIGNATURE"));
                Assert.That(recruit.Equipment.Find(EquipmentSlotIds.BodyArmor).Item, Is.Not.Null);
                var liveCombat = (M2CombatContent)typeof(M1RuntimeCoordinator).GetField("_combatContent",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(coordinator);
                Assert.That(liveCombat.HeroMasterGeneratedAuthority096.TryDescribe(recruit, out var generated), Is.True);
                Assert.That(generated.FixedWeaponFamilyId, Is.EqualTo("WEAPON_FAMILY_FOCUS"));
                var projected = Project100(saved, liveCombat);
                var member = projected.Members.Single();
                Assert.That(member.LearnedArtIds, Does.Contain(FocusRoot).And.Contain(FocusSecond));
                Assert.That(member.LearnedArtIds, Does.Contain("ART_BASIC_RUNE_BOLT").And.Contain("ART_EMBER_BOLT"),
                    "These established class/equipment starter Arts belong to the actual battle member, not a UI-only label.");
                Assert.That(member.LearnedArtIds, Does.Not.Contain("ART_BASIC_SABER_CUT"));
                var mysticCandidates = Candidates100(member, projected, liveCombat, projected.CurrentAp);
                Assert.That(mysticCandidates, Does.Contain("ART_BASIC_RUNE_BOLT").And.Contain("ART_EMBER_BOLT"));
                Assert.That(Candidates100(member, projected, liveCombat, 1), Does.Not.Contain("ART_EMBER_BOLT"));
                var dryToken = JObject.FromObject(member);
                dryToken["CurrentMp"] = 0;
                Assert.That(Candidates100(dryToken.ToObject<BattleMemberState>(), projected, liveCombat, projected.CurrentAp),
                    Does.Not.Contain("ART_BASIC_RUNE_BOLT").And.Not.Contain("ART_EMBER_BOLT"));
                var unchanged = CanonicalJson.Sha256Hex(saved);
                new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                Assert.That(CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path)), Is.EqualTo(unchanged));

                // Coordinator boot may normalize the minimal tutorial boundary.
                // Restore only the pre-existing explicit completed-opening TEST
                // boundary in memory; do not overwrite the live saved campaign.
                var battleFixture = saved.With(saved.Guild, fixture.OpeningFlow);
                var commands = new M2BattleCommandService();
                var battle = HeroRosterAudit093.Require093(commands.StartEncounterBattle(battleFixture, liveCombat,
                    "BATTLE_RYKA023_PROJECTION100", "Isolated exact Ryka legal casting regression", 1));
                var union = battle.Battle.PlayerUnions.Single();
                var forecast = battle.Battle.CommittedForecasts.Single(value =>
                    value.UnionId == union.UnionId && value.CommandId == "CMD_MYSTIC");
                var action = forecast.MemberActions.Single(value => value.ActorMemberId == recruit.RecruitId);
                Assert.That(action.Discipline, Is.EqualTo("Mystic"));
                Assert.That(union.Members.Single().LearnedArtIds, Does.Contain(action.ArtId));
                var mpBefore = union.Members.Single().CurrentMp;
                var selected = HeroRosterAudit093.Require093(commands.SelectForecast(battle, union.UnionId, forecast.ForecastId));
                var resolved = HeroRosterAudit093.Require093(commands.ConfirmRound(selected, liveCombat));
                Assert.That(resolved.Battle.RoundRecords, Has.Count.EqualTo(battle.Battle.RoundRecords.Count + 1));
                Assert.That(resolved.Battle.RoundRecords.Last().Events.Any(value =>
                    value.EventType == "MYSTIC_HIT" && value.ArtId == action.ArtId &&
                    value.ActorMemberId == recruit.RecruitId), Is.True,
                    "One selected shipping Mystic Forecast must really resolve; no forced victory or injected action.");
                Assert.That(resolved.Battle.PlayerUnions.Single().Members.Single().CurrentMp,
                    Is.EqualTo(mpBefore - action.PersonalMpCost));
                Assert.That(CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path)), Is.EqualTo(unchanged),
                    "The isolated combat probe must not write its result over the saved coordinator evidence.");
            });
        }

        [Test]
        public void ExactPreFixOwnedWarriorKeepsCanonicalRecipeAllArtsAndEquipmentAcrossReload100()
        {
            var legacy = Legacy100();
            var exactRecord = CanonicalJson.Sha256Hex(legacy);
            Assert.That(legacy.ClassTendencyId, Is.EqualTo("CLASS_WARRIOR"));
            Assert.That(legacy.Equipment.Find(EquipmentSlotIds.MainHand).Item.DefinitionId,
                Is.EqualTo("BASIC_094_WEAPON_FAMILY_SWORD"));
            Assert.That(legacy.Progression.LearnedArtIds, Is.EquivalentTo(new[] {
                "TREE_CA002_ROLE_DUELIST_N01", "TREE_CA002_WPN_SWORD_N01", "TREE_CA002_WPN_SWORD_N02" }));
            // Rehouse the exact old saved recruit in an isolated already-signed
            // campaign fixture. No regeneration of its Arts, gear or canonical JSON.
            var signed = HeroRosterAudit093.SignOutfitAndPlace093(HeroRosterAudit093.CreateFixture093(Hero100()),
                Hero100(), new HeroRosterAuditRow093());
            var oldState = signed.With(signed.Guild.With(signed.Guild.TreasuryXp, new[] { legacy },
                signed.Guild.Unions, signed.Guild.Inventory), signed.OpeningFlow);
            var noNewArrivals = new RecruitStarterEquipment094(_generated, _combat).ApplyToNewRecruits(oldState, oldState);
            Assert.That(CanonicalJson.Sha256Hex(noNewArrivals.Guild.Recruits.Single()), Is.EqualTo(exactRecord));
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(legacy, out var oldProfile), Is.True,
                "Legacy canonical Warrior reconstruction must remain trusted after new Mage applicants are introduced.");
            Assert.That(oldProfile.FixedWeaponFamilyId, Is.EqualTo("WEAPON_FAMILY_SWORD"));
            Assert.That(oldProfile.StartingLearnedArtIds, Is.EquivalentTo(legacy.Progression.LearnedArtIds));
            var oldUnion = Project100(oldState, _combat);
            Assert.That(oldUnion.Members.Single().LearnedArtIds,
                Is.SupersetOf(legacy.Progression.LearnedArtIds));
            Assert.That(oldUnion.Members.Single().LearnedArtIds, Does.Contain("ART_BASIC_SABER_CUT"));
            Assert.That(oldUnion.Members.Single().LearnedArtIds, Does.Not.Contain("ART_EMBER_BOLT"));
            WithSave100(oldState, (path, _) =>
            {
                for (var reload = 0; reload < 2; reload++)
                {
                    new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                    var reopened = HeroRosterAudit093.Read093(path).Guild.Recruits.Single();
                    Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(exactRecord),
                        "Do not auto-migrate an owned hero, overwrite learned Arts, exchange the sword, or rewrite canonical identity.");
                }
            });
        }

        [TestCase("StableId", "HERO_REC_999")]
        [TestCase("StableId", "HERO_REC_024")]
        [TestCase("Role", "elementalist")]
        [TestCase("Role", "Warrior")]
        [TestCase("Weapon", "Sword")]
        [TestCase("Weapon", "Flame focus ")]
        [TestCase("ArtTree1", "TREE_FIREARMS")]
        [TestCase("ArtTree2", "TREE_RESTORATION")]
        public void ExactGuardRejectsUnknownBorrowedOrChangedAuthoredIdentity100(string field, string replacement)
        {
            var altered = Clone100(Hero100(), field, replacement);
            Assert.That(HeroMasterPrimaryWeapon096.ClassForNewRecord(altered, "CLASS_WARRIOR"),
                Is.EqualTo("CLASS_WARRIOR"));
        }

        [Test]
        public void NullAndNonWarriorInputsCannotBeReclassifiedAndUnknownBattleIdentityIsRejected100()
        {
            Assert.That(HeroMasterPrimaryWeapon096.ClassForNewRecord(null, "CLASS_WARRIOR"), Is.EqualTo("CLASS_WARRIOR"));
            Assert.That(HeroMasterPrimaryWeapon096.ClassForNewRecord(Hero100(), "CLASS_PRIEST"), Is.EqualTo("CLASS_PRIEST"));
            var signed = HeroRosterAudit093.SignOutfitAndPlace093(HeroRosterAudit093.CreateFixture093(Hero100()),
                Hero100(), new HeroRosterAuditRow093());
            var token = JObject.FromObject(signed.Guild.Recruits.Single());
            token["AuthoredStableRecruitId"] = "HERO_REC_999";
            var unknown = token.ToObject<RecruitState>();
            Assert.That(_combat.HeroMasterGeneratedAuthority096.TryDescribe(unknown, out _), Is.False);
            Assert.That(M2DeepArtRuntime070.BattleLearnedArts(unknown, _combat)
                .Any(id => _combat.DeepProgression.TryNode(id, out _)), Is.False,
                "A forged stable ID cannot borrow Ryka's generated tree authority.");
        }

        [Test]
        public void EveryOtherAcceptedHeroKeepsExistingClassBridgeIncludingSsAndThalion100()
        {
            var previousClass = typeof(HeroMaster300CreatorRecruitProjection087).GetMethod(
                "CanonicalClassId", BindingFlags.NonPublic | BindingFlags.Static);
            var checkedCount = 0;
            foreach (var hero in HeroRosterAudit093.Catalog093.AcceptedHeroes.Where(value => value.StableId != "HERO_REC_023"))
            {
                var old = (string)previousClass.Invoke(null, new object[] { hero });
                var expected = hero.StableId == "HERO_REC_179" ? "CLASS_SPELLBLADE" : old;
                Assert.That(HeroMasterPrimaryWeapon096.ClassForNewRecord(hero, old), Is.EqualTo(expected), hero.StableId);
                checkedCount++;
            }
            Assert.That(checkedCount, Is.EqualTo(249));
        }

        static HeroMaster300Hero087 Hero100() => HeroRosterAudit093.Catalog093.AcceptedHeroes
            .Single(value => value.StableId == "HERO_REC_023");
        static RecruitState Legacy100() => JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
            "Tests", "Fixtures", "Ryka023LegacyRecruit100.fixture.json")))["recruit"].ToObject<RecruitState>();
        static BattleUnionState Project100(CampaignState state, M2CombatContent combat) =>
            ((IReadOnlyList<BattleUnionState>)typeof(M2BattleCommandService).GetMethod("CreatePlayerUnions",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { state, combat, null, 10 })).Single();
        static string[] Candidates100(BattleMemberState member, BattleUnionState union, M2CombatContent combat, int ap) =>
            M2BattleCommandService.CandidateArtsForVerification090(member, "CMD_MYSTIC", combat,
                ap, null, union.UnionId).ToArray();
        static HeroMaster300Hero087 Clone100(HeroMaster300Hero087 hero, string field, object replacement)
        {
            // Test-only constructor reflection deliberately supplies untrusted
            // identity variants. Production callers use catalog-accepted records.
            var constructor = typeof(HeroMaster300Hero087).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
            var args = constructor.GetParameters().Select(parameter =>
                string.Equals(parameter.Name, field, StringComparison.OrdinalIgnoreCase) ? replacement :
                typeof(HeroMaster300Hero087).GetProperty(parameter.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(hero)).ToArray();
            return (HeroMaster300Hero087)constructor.Invoke(args);
        }
        static string FileHash100(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }
        static void WithSave100(CampaignState state, Action<string, CampaignState> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "sd_ryka100_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "isolated.json");
                HeroRosterAudit093.Write093(path, state);
                action(path, state);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
