using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class RecruitmentUnlockFlow088Tests
    {
        private RecruitmentContent _content;
        private RecruitmentContent _expandedContent;
        private GuildCityContent017D _cityContent;
        private GuildCityRecruitmentService017D _service;

        [SetUp]
        public void SetUp()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _content = RecruitmentContent.LoadFromDirectory(contentRoot);
            var signatures = JObject.Parse(File.ReadAllText(
                Path.Combine(contentRoot, "OPENING_SIGNATURE_RECRUITS.json")));
            AddTestWorldSignature(signatures, "SIG_W02_01", "SIGREC_EMBER_TEST_01");
            AddTestWorldSignature(signatures, "SIG_W02_02", "SIGREC_EMBER_TEST_02");
            _expandedContent = RecruitmentContent.FromJson(
                File.ReadAllText(Path.Combine(contentRoot, "OPENING_PROCEDURAL_TABLES.json")),
                signatures.ToString(Formatting.None),
                File.ReadAllText(Path.Combine(contentRoot, "RECRUITMENT_OFFICE_PROGRESSION.json")));
            _cityContent = GuildCityContent017D.LoadFromDirectory(
                Path.Combine(contentRoot, "GUILD_CITY_017D"));
            _service = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)));
        }

        [Test]
        public void LegacyCommittedFixtureDefaultRecurringBoardCannotProgressivelyForceUnearnedSignatures()
        {
            var campaign = WithDryStreak(CreateCampaign("SIG_W01_01"),
                DetailedApplicantBoardGenerator.MercyThresholdBoards);

            campaign = Require(LegacyApplicantBoardFixture124.Commit(campaign, _content, _cityContent));

            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .All(value => value.Kind == ApplicantKind.Procedural), Is.True,
                "The charter referral is already owned, so every remaining World 1 signature is unearned.");
        }

        [Test]
        public void LegacyCommittedFixtureWorldAndOriginUnlocksExpandEligibilityDeterministically()
        {
            var baseline = WithDryStreak(CreateCampaign("SIG_W01_01"),
                DetailedApplicantBoardGenerator.MercyThresholdBoards);
            var runtime = WorldGateRuntimeState023.Default().With(
                currentWorldId: "WORLD_EMBERCHAIN_02",
                unlockedWorldIds: new[] { "SKYHOME", "WORLD_EMBERCHAIN_02" },
                unlockedRecruitOriginIds: new[]
                {
                    "RECRUIT020_DEMON_01",
                    "FLAG_FIRE_CONTACT"
                });
            var expandedA = WithWorldGate(baseline, runtime);
            var expandedB = WithWorldGate(baseline, runtime);

            expandedA = Require(LegacyApplicantBoardFixture124.Commit(expandedA, _expandedContent, _cityContent));
            expandedB = Require(LegacyApplicantBoardFixture124.Commit(expandedB, _expandedContent, _cityContent));
            var defaultBoard = Require(LegacyApplicantBoardFixture124.Commit(
                baseline, _expandedContent, _cityContent)).Guild.GuildCity.RecruitmentBoard;
            var expandedBoard = expandedA.Guild.GuildCity.RecruitmentBoard;

            Assert.That(CanonicalJson.Serialize(expandedBoard),
                Is.EqualTo(CanonicalJson.Serialize(
                    expandedB.Guild.GuildCity.RecruitmentBoard)));
            Assert.That(expandedBoard.BoardId, Is.Not.EqualTo(defaultBoard.BoardId));
            Assert.That(expandedBoard.Applicants.Any(value =>
                    value.Kind == ApplicantKind.Signature &&
                    value.SignatureId.StartsWith("SIG_W02_", StringComparison.Ordinal)),
                Is.True,
                "The exact world unlock supplies WORLD_PACK_02_ACCESS and the origin ledger supplies its contact flag.");
        }

        [Test]
        public void LegacyCommittedFixtureAlreadyRecruitedSignatureStaysExcludedAfterWorldUnlock()
        {
            var campaign = WithDryStreak(
                CreateCampaign("SIG_W01_01", "SIG_W02_01"),
                DetailedApplicantBoardGenerator.MercyThresholdBoards);
            campaign = WithWorldGate(campaign, WorldGateRuntimeState023.Default().With(
                currentWorldId: "WORLD_EMBERCHAIN_02",
                unlockedWorldIds: new[] { "SKYHOME", "WORLD_EMBERCHAIN_02" },
                unlockedRecruitOriginIds: new[] { "FLAG_FIRE_CONTACT" }));

            campaign = Require(LegacyApplicantBoardFixture124.Commit(campaign, _expandedContent, _cityContent));
            var signatures = campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .Where(value => value.Kind == ApplicantKind.Signature)
                .ToArray();

            Assert.That(signatures, Is.Not.Empty);
            Assert.That(signatures.Any(value =>
                StringComparer.Ordinal.Equals(value.SignatureId, "SIG_W02_01")), Is.False);
        }

        [Test]
        public void LegacyCommittedFixtureMissingWorldGateRuntimeUsesSaveCompatibleSkyhomeDefaults()
        {
            var campaign = WithDryStreak(CreateCampaign("SIG_W01_01"),
                DetailedApplicantBoardGenerator.MercyThresholdBoards);
            var legacyToken = JObject.Parse(CanonicalJson.Serialize(campaign));
            var strategicToken = legacyToken["Guild"]?["GuildCity"]?["Strategic017H"] as JObject;
            var campaignToken = strategicToken?["Campaign019"] as JObject;
            var playable = campaignToken?["Playable020"] as JObject;
            Assert.That(playable, Is.Not.Null);
            playable.Remove("WorldGate023");
            var legacy = JsonConvert.DeserializeObject<CampaignState>(
                legacyToken.ToString(Formatting.None));

            var currentBoard = Require(LegacyApplicantBoardFixture124.Commit(
                campaign, _content, _cityContent)).Guild.GuildCity.RecruitmentBoard;
            var legacyBoard = Require(LegacyApplicantBoardFixture124.Commit(
                legacy, _content, _cityContent)).Guild.GuildCity.RecruitmentBoard;

            Assert.That(legacy.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023.CurrentWorldId, Is.EqualTo("SKYHOME"));
            Assert.That(CanonicalJson.Serialize(legacyBoard),
                Is.EqualTo(CanonicalJson.Serialize(currentBoard)));
        }

        [Test]
        public void ForcedSignatureMustMeetEligibilityAndSupportItsSourceChannel()
        {
            var generator = new DetailedApplicantBoardGenerator(_content);
            var ineligible = Request(
                new[] { "GUILD_CHARTER_SIGNED" },
                new ForcedApplicantSpec
                {
                    SlotIndex = 1,
                    SourceType = "SIGNATURE",
                    SignatureId = "SIG_W01_02",
                    SourceChannel = "GUILD_BOARD"
                });
            Assert.That(() => generator.Generate(ineligible),
                Throws.InvalidOperationException.With.Message.Contains("eligibility rules"));

            var wrongChannel = Request(
                new[] { "DUSK_ARCHIVE_TRUST_2", "FOUND_FALSE_OBJECTIVE" },
                new ForcedApplicantSpec
                {
                    SlotIndex = 1,
                    SourceType = "SIGNATURE",
                    SignatureId = "SIG_W01_10",
                    SourceChannel = "GUILD_BOARD"
                }, guildRank: 2);
            Assert.That(() => generator.Generate(wrongChannel),
                Throws.InvalidOperationException.With.Message.Contains("source channel"));
        }

        [Test]
        public void RolledSignatureAlwaysSupportsGuildBoardSource()
        {
            var board = new DetailedApplicantBoardGenerator(_content).Generate(
                Request(new[]
                {
                    "GUILD_CHARTER_SIGNED", "IRON_WALL_TRUST_1", "INFIRMARY_CONTACT",
                    "BRASSWORK_TRUST_1", "ASHGLASS_CHARTER_RECOGNIZED", "STORMROAD_ACCESS",
                    "STORMROAD_TRUST_2", "FREIGHT_YARD_OPEN", "DUSK_ARCHIVE_TRUST_2",
                    "FOUND_FALSE_OBJECTIVE"
                }, forced: null, guildRank: 2));

            Assert.That(board.Applicants.Any(value => value.SourceType == "SIGNATURE"), Is.True);
            Assert.That(board.Applicants.Any(value =>
                    StringComparer.Ordinal.Equals(value.SignatureId, "SIG_W01_10")),
                Is.False,
                "SIG_W01_10 is secret/targeted-only and cannot roll through GUILD_BOARD.");
        }

        private static ApplicantBoardRequest Request(
            IReadOnlyList<string> flags,
            ForcedApplicantSpec forced,
            int guildRank = 0)
        {
            return new ApplicantBoardRequest
            {
                CampaignSeed = 88001L,
                GuildDay = 1,
                RefreshIndex = 0,
                OfficeTier = 0,
                DryStreak = DetailedApplicantBoardGenerator.MercyThresholdBoards,
                UnlockedWorlds = new[] { "WORLD_GATE_01" },
                UnlockedRaces = new[]
                {
                    "HUMAN", "ORC", "GOBLIN", "DOG_TRIBE", "DARK_ELF", "DEMON_HERITAGE"
                },
                EligibilityFlags = flags,
                GuildRank = guildRank,
                RecruitedSignatureIds = Array.Empty<string>(),
                SeenSignatureIds = Array.Empty<string>(),
                DeclinedSignatureUntilDay = new Dictionary<string, int>(StringComparer.Ordinal),
                EventBonusBasisPoints = 0,
                ScoutSkill = 50,
                TargetedSearch = null,
                ForcedApplicants = forced == null
                    ? Array.Empty<ForcedApplicantSpec>()
                    : new[] { forced },
                BoardSizeOverride = 6
            };
        }

        private static CampaignState CreateCampaign(params string[] recruitedSignatureIds)
        {
            var recruits = new List<RecruitState>();
            for (var index = 0; index < 6; index++)
            {
                var signatureId = index < recruitedSignatureIds.Length
                    ? recruitedSignatureIds[index]
                    : string.Empty;
                recruits.Add(string.IsNullOrWhiteSpace(signatureId)
                    ? new RecruitState("R" + (index + 1), 100, 100, 20, 20)
                    : new RecruitState(
                        "OWNED_" + signatureId, 100, 100, 20, 20,
                        signatureId, RecruitOriginKind.Signature, signatureId,
                        "HUMAN", "WORLD_GATE_01", "CLASS_GUARDIAN", string.Empty,
                        600, RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                        EquipmentLoadoutState.Empty(), true));
            }
            var unions = new[]
            {
                new UnionState("U1", "First Union", UnionKind.Normal, recruits[0].RecruitId,
                    recruits.Take(3).Select(value => value.RecruitId).ToArray(),
                    "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Second Union", UnionKind.Normal, recruits[3].RecruitId,
                    recruits.Skip(3).Select(value => value.RecruitId).ToArray(),
                    "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000)
            };
            var guild = new GuildState("GUILD_RECRUITMENT_088", 5000,
                recruits.AsReadOnly(), unions);
            var flow = new OpeningFlowState(OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001", true, null, false, 439, 0,
                true, true, true, true, "complete");
            return new CampaignState(
                "00000000-0000-0000-0000-000000088001", 88001L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild,
                new NewGuildProfileState("Recruiter", SecondDimension.Core.GameMode.Standard,
                    TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false),
                flow);
        }

        private static CampaignState WithDryStreak(CampaignState campaign, int dryStreak)
        {
            var city = campaign.Guild.GuildCity.With(recruitmentDryStreak: dryStreak);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState WithWorldGate(
            CampaignState campaign,
            WorldGateRuntimeState023 runtime)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                worldGate023: runtime, replaceWorldGate023: true);
            progress = progress.With(playable020: playable, replacePlayable020: true);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static T Require<T>(SecondDimension.Core.Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void AddTestWorldSignature(
            JObject signatures,
            string signatureId,
            string stableRecruitId)
        {
            var records = (JArray)signatures["signatureRecruits"];
            var record = (JObject)records[0].DeepClone();
            record["signatureId"] = signatureId;
            record["stableRecruitId"] = stableRecruitId;
            record["home"]["worldId"] = "WORLD_EMBERCHAIN_02";
            record["recruitment"]["eligibilityFlags"] = new JArray(
                "WORLD_PACK_02_ACCESS", "FLAG_FIRE_CONTACT");
            records.Add(record);
        }
    }
}
