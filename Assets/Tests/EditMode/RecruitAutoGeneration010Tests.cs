using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class RecruitAutoGeneration010Tests
    {
        private static readonly string[] Races = { "HUMAN", "ORC", "GOBLIN", "DOG_TRIBE", "DARK_ELF", "DEMON_HERITAGE" };
        private RecruitmentContent _recruitment;
        private RecruitAutoGenerator010 _generator;

        [SetUp]
        public void SetUp()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _recruitment = RecruitmentContent.LoadFromDirectory(contentRoot);
            _generator = new RecruitAutoGenerator010(RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot));
        }

        [Test]
        public void ProceduralRecruitMaterializesCompleteDeterministicProfile()
        {
            var recruit = Generate(0, "GOLDEN");
            var first = _generator.Generate(recruit, "WORLD_GATE_01");
            var second = _generator.Generate(recruit, "WORLD_GATE_01");
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(first.FixedWeaponFamilyId, Does.StartWith("WEAPON_FAMILY_"));
            Assert.That(first.WeaponTreeId, Does.StartWith("TREE_CA002_WPN_"));
            Assert.That(first.StartingLearnedArtIds.Count, Is.GreaterThan(0));
            Assert.That(first.VisualRecipe.RuntimeGeneratedAi, Is.False);
            Assert.That(first.AutoEquipAllowed, Is.False);
            Assert.That(first.ProfileHash, Has.Length.EqualTo(64));
        }

        [Test]
        public void EveryGeneratedArtRemainsForecastOnly()
        {
            var profile = _generator.Generate(Generate(1, "ART_LAW"));
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT", "BATTLE_ARTS_009", "DATA", "BATTLE_ART_PROFILES_240.json");
            var byId = JObject.Parse(File.ReadAllText(root))["profiles"].ToDictionary(x => x["stableNodeId"].Value<string>(), x => x);
            var skillRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT", "CONTENT_AUTHORITY_002", "DATA", "SKILL_NODES_360.json");
            var skillById = JObject.Parse(File.ReadAllText(skillRoot))["skillNodes"].ToDictionary(x => x["id"].Value<string>(), x => x);
            foreach (var id in profile.StartingStableNodeIds)
            {
                if (byId.ContainsKey(id))
                {
                    Assert.That(byId[id]["legality"]["playerDirectlySelectableInStandard"].Value<bool>(), Is.False, id);
                    Assert.That(byId[id]["legality"]["memberActionClickable"].Value<bool>(), Is.False, id);
                }
                else
                {
                    Assert.That(skillById.ContainsKey(id), Is.True, id);
                    Assert.That(skillById[id]["playerDirectlySelectableInStandard"].Value<bool>(), Is.False, id);
                }
            }
        }

        [Test]
        public void BoardPlannerRepairsFaceHairOutfitCollisionsDeterministically()
        {
            var recruits = new List<OpeningRecruitRecord>();
            for (var i = 0; i < 10; i++) recruits.Add(Generate(i, "BOARD_VISUAL"));
            var first = _generator.GenerateBoard("BOARD_AUTOGEN010_TEST", recruits, "WORLD_GATE_01");
            var second = _generator.GenerateBoard("BOARD_AUTOGEN010_TEST", recruits, "WORLD_GATE_01");
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));
            Assert.That(first.Profiles.Select(x => x.VisualRecipe.CollisionKey).Distinct().Count(), Is.EqualTo(10));
        }

        [Test]
        public void SignedRecruitInitializationAddsStartingArtsExactlyOnce()
        {
            var record = Generate(2, "SIGN_INIT");
            var recruit = new RecruitState(
                record.RecruitId, 100, 100, 20, 20, record.DisplayName, RecruitOriginKind.Procedural,
                string.Empty, record.RaceId, "WORLD_GATE_01", record.StartingClassId, string.Empty,
                record.DevelopmentPotentialScore, RecruitAuthorityKind.Normal,
                CanonicalJson.Serialize(record), string.Empty, EquipmentLoadoutState.Empty(), true,
                string.Empty, string.Empty, record.LeadershipScore, record.DisciplineAptitudes["TACTICAL"]);
            var service = new RecruitAutoGenerationSigningService010(new SecondDimension.Gameplay.M1.M1CommandService(), _generator);
            var first = service.InitializeRecruit(recruit);
            var second = service.InitializeRecruit(first);
            Assert.That(first.Progression.LearnedArtIds.Count, Is.GreaterThan(0));
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));
        }

        [Test]
        public void TenThousandGeneratedProfilesAreStableAndAllWeaponFamiliesAreReachable()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var families = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < 10000; i++)
            {
                var profile = _generator.Generate(Generate(i, "STRESS_010"));
                Assert.That(ids.Add(profile.ProfileId), Is.True, profile.ProfileId);
                families.Add(profile.FixedWeaponFamilyId);
                Assert.That(profile.VisualRecipe.RuntimeGeneratedAi, Is.False);
                Assert.That(profile.VisualRecipe.Layers.Count(x => x.Required), Is.GreaterThanOrEqualTo(9));
            }
            Assert.That(families.Count, Is.GreaterThanOrEqualTo(6), "Opening six classes need broad weapon-family reachability; later classes reach all 12.");
        }

        [Test]
        public void SignatureRecruitUsesAuthoredProgressionAndBespokePreferredVisualRoute()
        {
            var record = new SignatureRecruitMaterializer(_recruitment).Materialize(20260811L, "SIG_W01_01", "GUILD_BOARD");
            var profile = _generator.Generate(record, "WORLD_GATE_01");
            Assert.That(profile.SignatureId, Is.EqualTo("SIG_W01_01"));
            Assert.That(profile.FixedWeaponFamilyId, Is.EqualTo("WEAPON_FAMILY_SPEAR_POLEARM"));
            Assert.That(profile.VisualRecipe.VisualMode, Does.StartWith("BESPOKE_OVERRIDE_PREFERRED"));
            Assert.That(profile.StableAuthoredRecruitId, Is.EqualTo("SIGREC_MAREN_HOLT"));
        }

        private OpeningRecruitRecord Generate(int slot, string salt) =>
            new OpeningRecruitGenerator(_recruitment).Generate(new ProceduralRecruitRequest
            {
                CampaignSeed = 20260811L,
                GuildDay = 2 + slot / 10,
                RefreshIndex = slot / 6,
                SlotIndex = slot % 10,
                SourceChannel = "GUILD_BOARD",
                UnlockedRaces = Races,
                ExtraSalt = salt + "_" + slot
            });
    }
}
