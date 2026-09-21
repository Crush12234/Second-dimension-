using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SssTenV4AuthorityBinding090Tests
    {
        private static readonly string[] ExpectedArts090 =
        {
            "SSS_RYLEN_STONEBOND_ART_01|Beast Mark|Combat|DamageOneEnemyUnion",
            "SSS_RYLEN_STONEBOND_ART_02|Pack Instinct|Beastcraft|BuffSourceUnion",
            "SSS_RYLEN_STONEBOND_ART_03|Wild Mend|Restoration|HealOneAlliedUnion",
            "SSS_RYLEN_STONEBOND_ART_04|Call the Tamed Union|Beastcraft|SpawnSixMonsterUnion",
            "SSS_ELYSIA_NIGHTCALL_ART_01|Echo Bolt|Arcane|DamageOneEnemyUnion",
            "SSS_ELYSIA_NIGHTCALL_ART_02|Spirit Veil|Warding|BarrierOneAlliedUnion",
            "SSS_ELYSIA_NIGHTCALL_ART_03|Pact Convergence|Conjuration|BuffOneAlliedGuestUnion",
            "SSS_ELYSIA_NIGHTCALL_ART_04|Manifest the Pact|Conjuration|SpawnSingleGuestUnion",
            "SSS_VAELIS_MANYFORM_ART_01|Primal Rend|Combat|DamageOneEnemyUnion",
            "SSS_VAELIS_MANYFORM_ART_02|Adaptive Hide|Warding|BuffSourceUnion",
            "SSS_VAELIS_MANYFORM_ART_03|Instinctive Surge|Instinct|BuffSourceUnion",
            "SSS_VAELIS_MANYFORM_ART_04|Union Metamorphosis|Metamorphosis|ReplaceSourceUnion",
            "SSS_NERIS_DAWNWELL_ART_01|Dawn Pulse|Restoration|HealOneAlliedUnion",
            "SSS_NERIS_DAWNWELL_ART_02|Cleansing Tide|Restoration|CleanseAllAlliedUnions",
            "SSS_NERIS_DAWNWELL_ART_03|Life Chorus|Restoration|HealAllAlliedUnions",
            "SSS_NERIS_DAWNWELL_ART_04|Mercy Beyond Measure|Restoration|HealAllAlliedUnions",
            "SSS_MYRIEN_STARFALL_ART_01|Astral Lance|Astral|DamageOneEnemyUnion",
            "SSS_MYRIEN_STARFALL_ART_02|Meteor Choir|Elemental|DamageAllEnemyUnions",
            "SSS_MYRIEN_STARFALL_ART_03|Void Comet|Astral|DamageOneEnemyUnion",
            "SSS_MYRIEN_STARFALL_ART_04|Astral Cataclysm|Astral|DamageAllEnemyUnions",
            "SSS_ASTERION_SUNWARD_ART_01|Sunward Strike|Combat|DamageOneEnemyUnion",
            "SSS_ASTERION_SUNWARD_ART_02|Rallying Call|Leadership|BuffSourceUnion",
            "SSS_ASTERION_SUNWARD_ART_03|Dawn Standard|Leadership|BuffAllAlliedUnions",
            "SSS_ASTERION_SUNWARD_ART_04|Banner of Ten Thousand Dawns|Leadership|BuffAllAlliedUnions",
            "SSS_SOLENNE_AEGIS_ART_01|Shield Arc|Combat|DamageOneEnemyUnion",
            "SSS_SOLENNE_AEGIS_ART_02|Aegis Ward|Warding|BarrierOneAlliedUnion",
            "SSS_SOLENNE_AEGIS_ART_03|Fortress Chorus|Warding|BarrierAllAlliedUnions",
            "SSS_SOLENNE_AEGIS_ART_04|Citadel Without End|Warding|BarrierAllAlliedUnions",
            "SSS_CAEDRAN_TEMPEST_ART_01|Storm Spear|Storm|DamageOneEnemyUnion",
            "SSS_CAEDRAN_TEMPEST_ART_02|Thunder Chorus|Storm|DamageAllEnemyUnions",
            "SSS_CAEDRAN_TEMPEST_ART_03|Ion Break|Storm|DamageOneEnemyUnion",
            "SSS_CAEDRAN_TEMPEST_ART_04|Storm Across Worlds|Storm|DamageAllEnemyUnions",
            "SSS_ISOLDE_ECLIPSERIFT_ART_01|Rift Needle|Void|DamageOneEnemyUnion",
            "SSS_ISOLDE_ECLIPSERIFT_ART_02|Umbral Collapse|Void|DamageAllEnemyUnions",
            "SSS_ISOLDE_ECLIPSERIFT_ART_03|Eclipse Seal|Hexcraft|DebuffOneEnemyUnion",
            "SSS_ISOLDE_ECLIPSERIFT_ART_04|Eclipse of Every Front|Void|DamageDebuffAllEnemyUnions",
            "SSS_ORINTH_WORLDSONG_ART_01|Resonant Note|Harmonics|DamageOneEnemyUnion",
            "SSS_ORINTH_WORLDSONG_ART_02|March of Worlds|Leadership|BuffAllAlliedUnions",
            "SSS_ORINTH_WORLDSONG_ART_03|Renewal Verse|Restoration|HealAllAlliedUnions",
            "SSS_ORINTH_WORLDSONG_ART_04|Concordance of Worlds|Harmonics|BuffAllAlliedUnions"
        };

        [Test]
        public void ExactFortyArtsAreLiveAndUseCertifiedForecastPools090()
        {
            var content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath,
                    "Authority", "CONTENT"));
            var authored = SssTenV4ArtRegistry090.Build();
            Assert.That(authored.Count, Is.EqualTo(40));
            Assert.That(content.Arts.Keys.Count(value =>
                value.StartsWith("SSS_", StringComparison.Ordinal)),
                Is.EqualTo(40));

            foreach (var row in ExpectedArts090)
            {
                var fields = row.Split('|');
                var art = content.Art(fields[0]);
                Assert.That(art.Name, Is.EqualTo(fields[1]), fields[0]);
                Assert.That(art.TreeId, Is.EqualTo(fields[2]), fields[0]);
                Assert.That(art.EffectTags, Does.Contain(fields[3]), fields[0]);
                Assert.That(art.RequiredEquipmentTags, Is.Empty, fields[0]);
                Assert.That(art.PlayerDirectlySelectableInStandard,
                    Is.False, fields[0]);
                Assert.That(art.AnimationTag,
                    Is.EqualTo(fields[0] + "_ANIM"), fields[0]);

                var member = ForecastMember090(art.Id);
                var commandId = ForecastCommand090(art.Discipline);
                var candidates = M2BattleCommandService
                    .CandidateArtsForVerification090(
                        member, commandId, content, 99);
                var gold = art.Id.EndsWith("_ART_04",
                    StringComparison.Ordinal);
                var ownedGuestOnly = StringComparer.Ordinal.Equals(
                    art.Id, "SSS_ELYSIA_NIGHTCALL_ART_03");
                Assert.That(art.NodeType,
                    Is.EqualTo(gold ? "SIGNATURE" : "ACTION"), fields[0]);
                Assert.That(art.IsForecastAction, Is.EqualTo(!gold), fields[0]);
                if (ownedGuestOnly)
                    Assert.That(art.EffectTags,
                        Does.Contain("SSS_V4_SCOPE_OWNED_GUEST_UNION"));
                Assert.That(candidates.Contains(art.Id),
                    Is.EqualTo(!gold && !ownedGuestOnly),
                    gold
                        ? fields[0] + " must remain Gold-adapter-only."
                        : ownedGuestOnly
                            ? fields[0] +
                              " must remain conditional until Elysia owns a living guest Union."
                        : fields[0] + " must enter its real M2 Forecast pool.");
            }
        }

        [Test]
        public void PreparedFamilyAuthorityEnforcesExactSlotsUnlocksAndRylenDuplicates090()
        {
            var campaign = PreparedFamilyCampaign090();
            CollectionAssert.AreEqual(
                Enumerable.Range(1, 70).Select(value =>
                    "ENEMY_REC_" + value.ToString("D3")),
                SssPreparedFamilyService090.AllFamilies.Select(value =>
                    value.FamilyId));

            var shortRylen = SssPreparedFamilyService090.Configure(
                campaign,
                "SSS_RYLEN_STONEBOND",
                Enumerable.Repeat("ENEMY_REC_001", 5));
            Assert.That(shortRylen.IsSuccess, Is.False);
            Assert.That(SssTenV4CampaignAccessor090.Read(campaign)
                .PreparedFamilies, Is.Empty,
                "Rejected preparation must not mutate the source snapshot.");

            campaign = Require090(SssPreparedFamilyService090.Configure(
                campaign,
                "SSS_RYLEN_STONEBOND",
                Enumerable.Repeat("ENEMY_REC_001", 6)));
            CollectionAssert.AreEqual(
                Enumerable.Repeat("ENEMY_REC_001", 6),
                SssPreparedFamilyService090.View(
                    campaign, "SSS_RYLEN_STONEBOND").PreparedFamilyIds,
                "All six Rylen slots are independent and duplicates remain legal.");

            campaign = Require090(SssPreparedFamilyService090.Configure(
                campaign,
                "SSS_ELYSIA_NIGHTCALL",
                new[] { "ENEMY_REC_001" }));
            campaign = Require090(SssPreparedFamilyService090.Configure(
                campaign,
                "SSS_VAELIS_MANYFORM",
                new[] { "ENEMY_REC_001" }));
            Assert.That(SssPreparedFamilyService090.View(
                campaign, "SSS_ELYSIA_NIGHTCALL").Complete, Is.True);
            Assert.That(SssPreparedFamilyService090.View(
                campaign, "SSS_VAELIS_MANYFORM").Complete, Is.True);

            Assert.That(SssPreparedFamilyService090.Configure(
                campaign,
                "SSS_ELYSIA_NIGHTCALL",
                new[] { "ENEMY_REC_002" }).IsSuccess, Is.False,
                "A family must be unlocked independently for the selected hero.");
            Assert.That(SssPreparedFamilyService090.Configure(
                campaign,
                "SSS_VAELIS_MANYFORM",
                new[] { "ENEMY_REC_001_V05" }).IsSuccess, Is.False,
                "Variant IDs may resolve kills but cannot enter frozen base-family slots.");
        }

        [Test]
        public void UnionMutationsUseCovenantLeadershipAtTheExistingBoundary090()
        {
            var campaign = LeadershipCampaign090();
            var commands = new M1CommandService();

            campaign = Require090(commands.SetFormation(
                campaign, 0, "FORMATION_WEDGE"));
            Assert.That(campaign.Guild.Unions[0].LeaderRecruitId,
                Is.EqualTo("SSS_RYLEN_STONEBOND"),
                "An ordinary Union mutation promotes the first SSS in authored formation order.");

            campaign = Require090(commands.SetUnionLeader(
                campaign, 0, "SSS_ELYSIA_NIGHTCALL"));
            Assert.That(campaign.Guild.Unions[0].LeaderRecruitId,
                Is.EqualTo("SSS_ELYSIA_NIGHTCALL"),
                "An explicitly selected valid SSS leader is preserved.");

            campaign = Require090(commands.SetFormation(
                campaign, 0, "FORMATION_SHIELD_WALL"));
            Assert.That(campaign.Guild.Unions[0].LeaderRecruitId,
                Is.EqualTo("SSS_ELYSIA_NIGHTCALL"),
                "Later Union edits preserve the existing valid SSS leader.");

            campaign = Require090(commands.SetUnionLeader(
                campaign, 0, "REGULAR_090_A"));
            Assert.That(campaign.Guild.Unions[0].LeaderRecruitId,
                Is.EqualTo("SSS_ELYSIA_NIGHTCALL"),
                "Selecting a non-SSS member cannot displace the first SSS leader.");
        }

        private static BattleMemberState ForecastMember090(string artId) =>
            new BattleMemberState(
                "FORECAST_MEMBER_090",
                "Forecast Member",
                "CLASS_TEST",
                100,
                100,
                999,
                999,
                10,
                10,
                Array.Empty<string>(),
                false,
                false,
                false,
                new[] { artId },
                0,
                0,
                string.Empty);

        private static string ForecastCommand090(string discipline)
        {
            switch (discipline)
            {
                case "Restoration": return "CMD_HEAL";
                case "Support": return "CMD_SUPPORT";
                case "Mystic": return "CMD_MYSTIC";
                default: return "CMD_ALL_OUT";
            }
        }

        private static CampaignState PreparedFamilyCampaign090()
        {
            var heroIds = SssHeroes.All.Take(3).ToArray();
            var progression = new SssSave();
            foreach (var heroId in heroIds)
            {
                progression.familyProgress.counters.Add(new CounterRow
                {
                    heroId = heroId,
                    baseFamilyId = "ENEMY_REC_001",
                    totalDefeats = "10"
                });
            }
            return new CampaignState(
                    "00000000-0000-0000-0000-000000090092",
                    90092L,
                    "SSS_AUTHORITY_TEST_090",
                    ModeRuleSnapshot.StandardDefaults(),
                    new GuildState(
                        "GUILD_SSS_AUTHORITY_090",
                        0,
                        heroIds.Select(value =>
                            SssTenV4Roster090.MaterializeGrant(value).Recruit)
                            .ToArray(),
                        Array.Empty<UnionState>()))
                .WithSssV4090(new SssTenV4State090(progression));
        }

        private static CampaignState LeadershipCampaign090()
        {
            var regularA = new RecruitState(
                "REGULAR_090_A", 100, 100, 20, 20);
            var regularB = new RecruitState(
                "REGULAR_090_B", 100, 100, 20, 20);
            var rylen = SssTenV4Roster090
                .MaterializeGrant("SSS_RYLEN_STONEBOND").Recruit;
            var elysia = SssTenV4Roster090
                .MaterializeGrant("SSS_ELYSIA_NIGHTCALL").Recruit;
            var unions = new[]
            {
                new UnionState(
                    "UNION_AUTHORITY_090_A",
                    "Covenant Test Union",
                    UnionKind.Normal,
                    regularA.RecruitId,
                    new[]
                    {
                        regularA.RecruitId,
                        rylen.RecruitId,
                        elysia.RecruitId
                    },
                    "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED",
                    18,
                    8500),
                new UnionState(
                    "UNION_AUTHORITY_090_B",
                    "Second Test Union",
                    UnionKind.Normal,
                    regularB.RecruitId,
                    new[] { regularB.RecruitId },
                    "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED",
                    18,
                    8500)
            };
            var profile = new NewGuildProfileState(
                "SSS Authority Tester",
                GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                ApplicantBoardState.FrozenTutorialSeedId,
                true,
                null,
                false,
                0,
                0,
                true,
                true,
                true,
                false,
                "sss_authority_ready");
            return new CampaignState(
                "00000000-0000-0000-0000-000000090093",
                90093L,
                "SSS_AUTHORITY_TEST_090",
                ModeRuleSnapshot.StandardDefaults(),
                new GuildState(
                    "GUILD_SSS_LEADERSHIP_090",
                    0,
                    new[] { regularA, regularB, rylen, elysia },
                    unions),
                profile,
                flow);
        }

        private static CampaignState Require090(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                result.IsSuccess
                    ? string.Empty
                    : string.Join(" | ", result.Errors));
            return result.Value;
        }
    }
}
