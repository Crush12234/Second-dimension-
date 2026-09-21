using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Release030;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class OwnerReviewSmokeAdapter030Tests
    {
        [Test]
        public void CommandLineAdapterIsInertWithoutFlagAndRejectsDataFolderEvidence()
        {
            var persistent = Path.Combine(Path.GetTempPath(), "sd030_persistent");
            var data = Path.Combine(Path.GetTempPath(), "SecondDimension_Data");
            Assert.That(OwnerReviewSmokeOptions030.TryParse(Array.Empty<string>(), persistent, data,
                out var inactive, out var inactiveError), Is.False);
            Assert.That(inactive, Is.Null);
            Assert.That(inactiveError, Is.Empty);

            var insideData = Path.Combine(data, "Evidence");
            var arguments = new[]
            {
                OwnerReviewSmokeOptions030.InitialFlag,
                OwnerReviewSmokeOptions030.EvidenceDirectoryFlag + insideData
            };
            Assert.That(OwnerReviewSmokeOptions030.TryParse(arguments, persistent, data,
                out var rejected, out var error), Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(error, Does.Contain("outside the player Data folder"));
        }

        [Test]
        public void ExactFlagsProduceStableExternalReportPathsWithoutSerializingCreatorPlaintext()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd030_options_" + Guid.NewGuid().ToString("N"));
            var evidence = Path.Combine(root, "Evidence");
            var save = Path.Combine(root, "owner_review_030.json");
            const string secret = "DO-NOT-SERIALIZE-THIS-CODE";
            var arguments = new[]
            {
                OwnerReviewSmokeOptions030.InitialFlag,
                OwnerReviewSmokeOptions030.EvidenceDirectoryFlag + evidence,
                OwnerReviewSmokeOptions030.SavePathFlag + save,
                OwnerReviewSmokeOptions030.CreatorCodeFlag + secret
            };
            try
            {
                Assert.That(OwnerReviewSmokeOptions030.TryParse(arguments, root,
                    Path.Combine(root, "Game_Data"), out var options, out var error), Is.True, error);
                Assert.That(options.ReportPath, Is.EqualTo(Path.Combine(evidence, "built_player_smoke_030.json")));
                Assert.That(options.ProgressPath, Is.EqualTo(Path.Combine(evidence, "built_player_smoke_030.progress.json")));
                var report = new OwnerReviewSmokeReport030
                {
                    runKind = "INITIAL",
                    runStatus = "PASS",
                    reportPath = options.ReportPath,
                    savePath = options.SavePath
                };
                report.gates.Add(new OwnerReviewGateEvidence030
                    { id = "creator", stage = "Creator", status = "PASS", detail = "Ledger advanced." });
                OwnerReviewSmokeEvidenceWriter030.Write(options.ReportPath, report);
                var json = File.ReadAllText(options.ReportPath);
                Assert.That(json, Does.Not.Contain(secret));
                Assert.That(OwnerReviewSmokeEvidenceWriter030.Read(options.ReportPath).gates.Single().status,
                    Is.EqualTo("PASS"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void CreatorCodeUsesInheritedEnvironmentFallbackWithoutEnteringArgumentsOrEvidence()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd030_env_code_" + Guid.NewGuid().ToString("N"));
            var evidence = Path.Combine(root, "Evidence");
            var save = Path.Combine(root, "owner_review_030.json");
            const string inheritedSecret = "INHERITED-OWNER-CODE-DO-NOT-EMIT";
            const string explicitOverride = "EXPLICIT-OWNER-CODE-OVERRIDE";
            var previous = Environment.GetEnvironmentVariable(
                OwnerReviewSmokeOptions030.CreatorCodeEnvironmentVariable);
            var arguments = new[]
            {
                OwnerReviewSmokeOptions030.InitialFlag,
                OwnerReviewSmokeOptions030.EvidenceDirectoryFlag + evidence,
                OwnerReviewSmokeOptions030.SavePathFlag + save
            };
            try
            {
                Environment.SetEnvironmentVariable(
                    OwnerReviewSmokeOptions030.CreatorCodeEnvironmentVariable, inheritedSecret);
                Assert.That(arguments.Any(value => value.Contains(inheritedSecret)), Is.False);
                Assert.That(OwnerReviewSmokeOptions030.TryParse(arguments, root,
                    Path.Combine(root, "Game_Data"), out var options, out var error), Is.True, error);
                Assert.That(options.CreatorCode, Is.EqualTo(inheritedSecret));
                Assert.That(JsonConvert.SerializeObject(options), Does.Not.Contain(inheritedSecret));

                var report = new OwnerReviewSmokeReport030
                {
                    runKind = "INITIAL",
                    runStatus = "PASS",
                    reportPath = options.ReportPath,
                    savePath = options.SavePath
                };
                OwnerReviewSmokeEvidenceWriter030.Write(options.ReportPath, report);
                Assert.That(File.ReadAllText(options.ReportPath), Does.Not.Contain(inheritedSecret));

                var overrideArguments = arguments.Concat(new[]
                    { OwnerReviewSmokeOptions030.CreatorCodeFlag + explicitOverride }).ToArray();
                Assert.That(OwnerReviewSmokeOptions030.TryParse(overrideArguments, root,
                    Path.Combine(root, "Game_Data"), out var overridden, out error), Is.True, error);
                Assert.That(overridden.CreatorCode, Is.EqualTo(explicitOverride));

                var relaunchArguments = new[]
                {
                    OwnerReviewSmokeOptions030.RelaunchFlag,
                    OwnerReviewSmokeOptions030.EvidenceDirectoryFlag + evidence,
                    OwnerReviewSmokeOptions030.SavePathFlag + save
                };
                Assert.That(OwnerReviewSmokeOptions030.TryParse(relaunchArguments, root,
                    Path.Combine(root, "Game_Data"), out var relaunch, out error), Is.True, error);
                Assert.That(relaunch.CreatorCode, Is.Empty,
                    "The inherited INITIAL secret must not be retained by the relaunch pass.");
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    OwnerReviewSmokeOptions030.CreatorCodeEnvironmentVariable, previous);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void RequiredGateSetIsFixedUniqueAndFailsClosedForMissingOrDuplicateEvidence()
        {
            var required = OwnerReviewSmokeWorkflow030.RequiredGateIdsFor030(
                OwnerReviewSmokeRunKind030.Initial);
            Assert.That(required.Count, Is.EqualTo(43));
            Assert.That(required.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(required.Count));

            var report = new OwnerReviewSmokeReport030
            {
                gates = required.Select(value => new OwnerReviewGateEvidence030
                    { id = value, stage = "Test", status = "PASS", detail = "Observed." }).ToList()
            };
            Assert.That(OwnerReviewSmokeWorkflow030.EvaluateChecklist030(
                report, OwnerReviewSmokeRunKind030.Initial), Is.True);
            Assert.That(report.requiredGateCount, Is.EqualTo(43));
            Assert.That(report.passedRequiredGateCount, Is.EqualTo(43));

            report.gates.RemoveAt(0);
            Assert.That(OwnerReviewSmokeWorkflow030.EvaluateChecklist030(
                report, OwnerReviewSmokeRunKind030.Initial), Is.False);
            Assert.That(report.missingRequiredGateIds, Does.Contain(required[0]));

            report.gates.Add(new OwnerReviewGateEvidence030
                { id = required[0], stage = "Test", status = "PASS", detail = "Observed." });
            report.gates.Add(new OwnerReviewGateEvidence030
                { id = required[0], stage = "Test", status = "PASS", detail = "Duplicate." });
            Assert.That(OwnerReviewSmokeWorkflow030.EvaluateChecklist030(
                report, OwnerReviewSmokeRunKind030.Initial), Is.False);
            Assert.That(report.duplicateRequiredGateIds, Does.Contain(required[0]));
        }

        [Test]
        public void OwnerReviewOpeningUsesVisibleRelease071CharterBeforeFirstStoryContract()
        {
            var save = Path.Combine(Path.GetTempPath(),
                "sd030_owner_recruit_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var coordinator = new M1RuntimeCoordinator(ContentRoot(), save);
                Require(coordinator.CreateGuild(new M1NewGuildIntent
                {
                    GuildmasterName = "Owner Recruit Order Test",
                    ModeId = "Standard",
                    TutorialDepthId = "Full Tutorial"
                }));
                var founderIds = coordinator.State.Applicants
                    .Select(value => value.RecruitId)
                    .ToArray();
                Assert.That(founderIds.Length, Is.EqualTo(6));
                foreach (var recruitId in founderIds) Require(coordinator.SignRecruit(recruitId));
                Require(coordinator.CompleteEquipmentReview());
                for (var index = 0; index < 3; index++)
                    Require(coordinator.AssignRecruitToUnion(founderIds[index], 0, index));
                for (var index = 3; index < 6; index++)
                    Require(coordinator.AssignRecruitToUnion(founderIds[index], 1, index - 3));
                for (var unionIndex = 0; unionIndex < 2; unionIndex++)
                {
                    Require(coordinator.SetFormation(
                        unionIndex, coordinator.State.Formations[unionIndex].Id));
                    Require(coordinator.SetDoctrine(
                        unionIndex, coordinator.State.Doctrines[unionIndex].Id));
                }
                Require(coordinator.SaveAndReloadProof());

                var city = coordinator.GuildCity017D;
                Assert.That(city.TotalRecruitCount, Is.EqualTo(10),
                    "Release 071 visibly charters four named Skyhome companions after the six founders.");
                var charterNames = new HashSet<string>(new FirstHourDirector071().PlayableRoster
                    .Where(value => value.Wave == FirstHourRosterWave071.SkyhomeCharter)
                    .Select(value => value.DisplayName), StringComparer.Ordinal);
                var charterRecruitIds = new HashSet<string>(coordinator.State.Recruits
                    .Where(value => charterNames.Contains(value.DisplayName))
                    .Select(value => value.RecruitId), StringComparer.Ordinal);
                Assert.That(charterRecruitIds.Count, Is.EqualTo(4));
                Assert.That(city.Assignments.Count(value =>
                    charterRecruitIds.Contains(value.RecruitId) &&
                    StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Reserve")), Is.EqualTo(4));
                Assert.That(coordinator.State.AllSixSigned, Is.True);
                Assert.That(coordinator.State.TwoUnionsLegal, Is.True);
                Assert.That(coordinator.State.Unions.Count(value => value.MemberRecruitIds.Count > 0),
                    Is.EqualTo(2));
                Assert.That(coordinator.State.Unions
                    .Where(value => value.MemberRecruitIds.Count > 0)
                    .All(value => value.IsLegal), Is.True);

                var people = coordinator.PeopleRuntime029;
                var unionMemberRecruitIds = new HashSet<string>(
                    people.Unions.SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
                Assert.That(people.Quests.FirstOrDefault(value =>
                    unionMemberRecruitIds.Contains(value.RecruitId)), Is.Not.Null,
                    "The owner smoke must select an authored quest for a deployed Union member.");

                var treasuryBefore = city.TreasuryXp;
                Require(coordinator.AcceptGuildCityContract017D(
                    GuildCityExpeditionService017D.FirstStoryContractId066));
                Assert.That(coordinator.GuildCity017D.TreasuryXp, Is.EqualTo(treasuryBefore));
                Assert.That(coordinator.GuildCity017D.Contracts.Single(value =>
                    StringComparer.Ordinal.Equals(value.ContractId,
                        GuildCityExpeditionService017D.FirstStoryContractId066)).IsActive, Is.True);
            }
            finally
            {
                DeleteSaveFamily(save);
            }
        }

        [Test]
        public void AbyssSmokeEvidenceRequiresCanonicalFinalizeAndExactAuthoredDeltas()
        {
            var registry = SecondDimension.Presentation.Campaign022.CampaignRegistry022.LoadFromResources();
            var service = new CampaignProgressionCommandService022();
            var guardianReady = AdvanceAbyssOperationToReady030(
                CreateCompleteCampaign(), service, registry,
                "ABYSS_OP022_01_GUARDIAN", "OWNER_REVIEW_ABYSS_GUARDIAN_030");
            var guardianCommitted = RequireCampaign(
                service.CommitAbyssOperationCompletion(guardianReady, registry));
            var guardianCleared = RequireCampaign(
                service.ApplyAbyssOperationCompletion(guardianCommitted, registry));
            var before = AdvanceAbyssOperationToReady030(
                guardianCleared, service, registry,
                "ABYSS_OP022_01_RECON", "OWNER_REVIEW_ABYSS_RECON_030");
            var committed = RequireCampaign(service.CommitAbyssOperationCompletion(before, registry));
            var after = RequireCampaign(service.ApplyAbyssOperationCompletion(committed, registry));
            Assert.That(OwnerReviewSmokeWorkflow030.EvaluateAbyssFinalizationEvidence030(
                before, after, out var detail), Is.True, detail);
            Assert.That(OwnerReviewSmokeWorkflow030.EvaluateAbyssFinalizationEvidence030(
                after, after, out _), Is.False,
                "An already-finalized state must not be relabeled as a newly exercised Abyss clear.");
        }

        private static CampaignState AdvanceAbyssOperationToReady030(
            CampaignState campaign,
            CampaignProgressionCommandService022 service,
            SecondDimension.Presentation.Campaign022.CampaignRegistry022 registry,
            string operationId,
            string rewardIdPrefix)
        {
            campaign = RequireCampaign(service.BeginAbyssOperation(
                campaign, registry, operationId));
            var operation = registry.AbyssOperations[operationId];
            for (var index = 0; index < operation.steps.Length; index++)
            {
                if (operation.steps[index].requiresBattle)
                {
                    campaign = RequireCampaign(
                        service.CommitAbyssBattleEncounter(campaign, registry));
                    var request = campaign.Guild.GuildCity.PendingEncounter;
                    campaign = WithClaimedAbyssBattle030(
                        campaign, rewardIdPrefix + "_" + index, request);
                    campaign = RequireCampaign(
                        service.CommitAbyssBattleReturn(campaign, registry));
                    campaign = RequireCampaign(
                        service.ApplyAbyssBattleReturnExactlyOnce(campaign, registry));
                    campaign = RequireCampaign(
                        service.CommitAbyssBattleResult(campaign, registry));
                    campaign = RequireCampaign(
                        service.ApplyAbyssBattleAndFinalize(campaign, registry));
                }
                else
                {
                    campaign = RequireCampaign(
                        service.CommitAbyssStep(campaign, registry, "SUCCESS"));
                    campaign = RequireCampaign(
                        service.ApplyAbyssStep(campaign, registry));
                }
            }
            return campaign;
        }

        [Test]
        public void SignedOpeningIdentityProjectsToAuthoredPersonalQuestWithoutDuplicatingAuthority()
        {
            var save = Path.Combine(Path.GetTempPath(), "sd030_alias_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var coordinator = new M1RuntimeCoordinator(ContentRoot(), save);
                Require(coordinator.CreateGuild(new M1NewGuildIntent
                {
                    GuildmasterName = "Alias Test",
                    ModeId = "Standard",
                    TutorialDepthId = "Full Tutorial"
                }));
                foreach (var recruitId in coordinator.State.Applicants.Select(value => value.RecruitId).ToArray())
                    Require(coordinator.SignRecruit(recruitId));
                var actualIds = new HashSet<string>(coordinator.State.Recruits.Select(value => value.RecruitId),
                    StringComparer.Ordinal);
                var people = coordinator.PeopleRuntime029;
                Assert.That(people.IsAvailable, Is.True, people.Error);
                Assert.That(people.Quests, Is.Not.Empty);
                Assert.That(people.Quests.All(value => actualIds.Contains(value.RecruitId)), Is.True);
                Assert.That(people.Quests.All(value => value.RecruitId.StartsWith("SIGI_", StringComparison.Ordinal)),
                    Is.True);

                var quest = people.Quests.First();
                Require(coordinator.StartPersonalQuest029(quest.BoardId));
                var restored = new M1RuntimeCoordinator(ContentRoot(), save);
                var restoredQuest = restored.PeopleRuntime029.Quests.Single(value =>
                    StringComparer.Ordinal.Equals(value.BoardId, quest.BoardId));
                Assert.That(restoredQuest.Started, Is.True);
                Assert.That(restoredQuest.RecruitId, Is.EqualTo(quest.RecruitId));
            }
            finally
            {
                DeleteSaveFamily(save);
            }
        }

        [Test]
        public void FrozenOpeningRosterExecutesEquipmentLegalCmdMysticWithoutLockedThirdSchool076()
        {
            var save = Path.Combine(Path.GetTempPath(), "sd030_mystic_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var coordinator = new M1RuntimeCoordinator(ContentRoot(), save);
                Require(coordinator.CreateGuild(new M1NewGuildIntent
                {
                    GuildmasterName = "Mystic Coverage Test",
                    ModeId = "Standard",
                    TutorialDepthId = "Full Tutorial"
                }));
                var recruitIds = coordinator.State.Applicants.Select(value => value.RecruitId).ToArray();
                Assert.That(recruitIds.Length, Is.EqualTo(6));
                foreach (var recruitId in recruitIds) Require(coordinator.SignRecruit(recruitId));

                var signed = coordinator.State.Recruits;
                var gara = signed.Single(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, "PROC_36344E2400DC98B6"));
                var odelia = signed.Single(value =>
                    StringComparer.Ordinal.Equals(value.DisplayName, "Odelia Fen"));
                Assert.That(gara.LearnedArtIds, Does.Contain("ART_RUNE_EDGE"));
                Assert.That(odelia.LearnedArtIds, Does.Contain("ART_FROST_THREAD"));
                Assert.That(gara.MaximumMp, Is.GreaterThanOrEqualTo(7));
                Assert.That(odelia.MaximumMp, Is.GreaterThanOrEqualTo(4));

                Require(coordinator.CompleteEquipmentReview());
                for (var index = 0; index < 3; index++)
                    Require(coordinator.AssignRecruitToUnion(recruitIds[index], 0, index));
                for (var index = 3; index < 6; index++)
                    Require(coordinator.AssignRecruitToUnion(recruitIds[index], 1, index - 3));
                for (var unionIndex = 0; unionIndex < 2; unionIndex++)
                {
                    Require(coordinator.SetFormation(unionIndex, coordinator.State.Formations[unionIndex].Id));
                    Require(coordinator.SetDoctrine(unionIndex, coordinator.State.Doctrines[unionIndex].Id));
                }
                Require(coordinator.SaveAndReloadProof());
                Require(coordinator.StartTutorialBattle());

                var mysticForecasts = coordinator.State.Battle.Forecasts
                    .Where(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_MYSTIC"))
                    .ToArray();
                Assert.That(mysticForecasts.Select(value => value.UnionId).Distinct(StringComparer.Ordinal).Count(),
                    Is.EqualTo(2), "Each frozen opening Union must expose CMD_MYSTIC.");
                var predictedMysticActions = mysticForecasts
                    .SelectMany(value => value.MemberActions)
                    .Where(value => StringComparer.OrdinalIgnoreCase.Equals(value.Discipline, "Mystic"))
                    .ToArray();
                Assert.That(predictedMysticActions.Select(value => value.ArtId).Distinct(StringComparer.Ordinal),
                    Is.EquivalentTo(new[]
                    {
                        "ART_RUNE_EDGE",
                        "TREE_CA002_WPN_STAFF_N01"
                    }),
                    "The opening Forecast must use the two equipment-legal starting trees, not grant a third Mystic school early.");
                Assert.That(predictedMysticActions.Select(value => value.ArtId),
                    Does.Not.Contain("TREE_CA002_MYS_FROST_N01"));
                Assert.That(predictedMysticActions.Select(value => ProfileSchool030(value.ArtId))
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.Ordinal),
                    Is.EquivalentTo(new[] { "AETHER" }));

                foreach (var forecast in mysticForecasts) Require(coordinator.SelectForecast(
                    forecast.UnionId, forecast.ForecastId));
                Require(coordinator.ConfirmBattleRound());
                var executedMysticActions = coordinator.State.Battle.LastResolvedRoundEvents
                    .Where(value => StringComparer.Ordinal.Equals(value.EventType, "MYSTIC_HIT"))
                    .ToArray();
                Assert.That(executedMysticActions.Select(value => value.ArtId).Distinct(StringComparer.Ordinal),
                    Is.EquivalentTo(new[]
                    {
                        "ART_RUNE_EDGE",
                        "TREE_CA002_WPN_STAFF_N01"
                    }));
                Assert.That(executedMysticActions.Select(value => value.ArtId),
                    Does.Not.Contain("TREE_CA002_MYS_FROST_N01"));
                Assert.That(executedMysticActions.Select(value => ProfileSchool030(value.ArtId))
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.Ordinal),
                    Is.EquivalentTo(new[] { "AETHER" }));
            }
            finally
            {
                DeleteSaveFamily(save);
            }
        }

        [Test]
        public void TerminalResolvedExpeditionAllowsDefenseButUnfinalizedExpeditionStillBlocks()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var strategic = GuildCityStrategicContent017H.LoadFromDirectory(
                Path.Combine(contentRoot, "GUILD_CITY_017H"));
            var service = new GuildCityDefenseService017H();

            var terminal = WithGuildOperation(CreateCompleteCampaign(), ExpeditionStatus017D.Completed,
                contractResolved: true);
            var allowed = service.StartDefense(terminal, strategic, "DEF_PROFILE_FIRST_BELL_DRILL");
            Assert.That(allowed.IsSuccess, Is.True, string.Join("\n", allowed.Errors));

            var unfinalized = WithGuildOperation(CreateCompleteCampaign(), ExpeditionStatus017D.Completed,
                contractResolved: false);
            var blocked = service.StartDefense(unfinalized, strategic, "DEF_PROFILE_FIRST_BELL_DRILL");
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors, Contains.Item("GC017H_OPERATION_ALREADY_ACTIVE"));
        }

        [Test]
        public void TerminalResolvedExpeditionAllowsWorldGateButActiveExpeditionStillBlocks()
        {
            var registry = CampaignRegistry023.LoadFromResources();
            var catalog = new Campaign023RuleCatalogAdapter(registry);
            var service = new CampaignWorldGateCommandService023();

            var terminal = WithGuildOperation(CreateCompleteCampaign(), ExpeditionStatus017D.Completed,
                contractResolved: true);
            var allowed = service.BeginOperation(terminal, catalog, "CRISIS020_SKYHOME_01", new[] { "U1" }, false);
            Assert.That(allowed.IsSuccess, Is.True, string.Join("\n", allowed.Errors));
            Assert.That(allowed.Value.Guild.GuildCity.Expedition.BoardId, Is.EqualTo("BOARD023_CRISIS020_SKYHOME_01"));

            var active = WithGuildOperation(CreateCompleteCampaign(), ExpeditionStatus017D.Active,
                contractResolved: false);
            var blocked = service.BeginOperation(active, catalog, "CRISIS020_SKYHOME_01", new[] { "U1" }, false);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors, Contains.Item("CAMPAIGN023_OPERATION_ALREADY_ACTIVE"));
        }

        private static CampaignState WithGuildOperation(CampaignState campaign, ExpeditionStatus017D status,
            bool contractResolved)
        {
            var city = campaign.Guild.GuildCity;
            var contract = new ContractCommitState017D("COMMIT_TEST_030", "CONTRACT_TEST_030",
                "BOARD_TEST_030", "SEED_TEST_030", 0, contractResolved, false);
            var expedition = new ExpeditionState017D("EXPEDITION_TEST_030", contract.CommitId,
                contract.BoardId, "NODE_TEST_030", status, 10, 0, 0, 0,
                new[] { "NODE_TEST_030" }, new[] { "NODE_TEST_030" }, Array.Empty<string>(),
                Array.Empty<CommittedCheckState017D>(), Array.Empty<string>(), "operation_test_030");
            city = city.With(activeContract: contract, replaceActiveContract: true,
                expedition: expedition, replaceExpedition: true, lastCheckpointId: "operation_test_030");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState CreateCompleteCampaign()
        {
            var recruits = new[]
            {
                new RecruitState("R1", 100, 100, 20, 20), new RecruitState("R2", 100, 100, 20, 20),
                new RecruitState("R3", 100, 100, 20, 20), new RecruitState("R4", 100, 100, 20, 20),
                new RecruitState("R5", 100, 100, 20, 20), new RecruitState("R6", 100, 100, 20, 20)
            };
            var unions = new[]
            {
                new UnionState("U1", "First Union", UnionKind.Normal, "R1", new[] { "R1", "R2", "R3" },
                    "FORMATION_LINE", "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Second Union", UnionKind.Normal, "R4", new[] { "R4", "R5", "R6" },
                    "FORMATION_LINE", "DOCTRINE_BALANCED", 30, 7000)
            };
            var guild = new GuildState("GUILD_TEST_030", 1000, recruits, unions);
            var flow = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null,
                false, 439, 0, true, true, true, true, "complete");
            var profile = new NewGuildProfileState("Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            return new CampaignState("00000000-0000-0000-0000-000000000030", 30030, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }

        private static CampaignState WithClaimedAbyssBattle030(
            CampaignState campaign, string rewardId,
            EncounterLaunchRequest017D request)
        {
            var memberReward = new BattleMemberRewardState(
                "OWNER_REVIEW_ABYSS_MEMBER_030", "Owner Review Guardian",
                1, 1, 1, 0, 0, 0, 0, 0, 0, 0);
            var reward = new BattleRewardState(rewardId,
                "OWNER_REVIEW_ABYSS_REWARD_RULE_030", BattleOutcome.Victory,
                1, 1, 1000, 1000, 100, 100, 7, 5,
                new[] { memberReward }, true);
            var battle = new BattleState(request.BattleId,
                "OWNER_REVIEW_ABYSS_030", 1, BattlePhase.Resolved,
                BattleOutcome.Victory, request.Objective,
                Array.Empty<BattleUnionState>(), Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(),
                Array.Empty<BattleRoundRecordState>(),
                "OWNER_REVIEW_ABYSS_FORECAST_030",
                "OWNER_REVIEW_ABYSS_INITIAL_030", string.Empty,
                string.Empty, string.Empty, false, reward);
            battle = battle.With(finalStateHash:
                M2BattleCommandService.AuthoritativeStateHash(battle));
            var development = campaign.Guild.Development.RecordBattleReward(
                rewardId, reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp + reward.GuildTreasuryXpAward,
                campaign.Guild.Recruits, campaign.Guild.Unions,
                campaign.Guild.Inventory, development);
            return campaign.With(guild, campaign.OpeningFlow).WithBattle(battle);
        }

        private static string ContentRoot()
        {
            return Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        }

        private static string ProfileSchool030(string artId)
        {
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            var profile = (manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>()).FirstOrDefault(value =>
                value != null && StringComparer.OrdinalIgnoreCase.Equals(value.artId, artId));
            if (profile == null)
            {
                var alias = (manifest.legacyAliases ?? Array.Empty<BattleArtAlias011>()).FirstOrDefault(value =>
                    value != null && StringComparer.OrdinalIgnoreCase.Equals(value.aliasArtId, artId));
                if (alias != null)
                    profile = (manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>()).FirstOrDefault(value =>
                        value != null && StringComparer.OrdinalIgnoreCase.Equals(value.artId, alias.runtimeArtId));
            }
            Assert.That(profile, Is.Not.Null, "BattleArt profile missing for " + artId);
            return profile.schoolId;
        }

        private static void Require(M1CommandResult result)
        {
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        private static CampaignState RequireCampaign(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void DeleteSaveFamily(string path)
        {
            foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(candidate)) File.Delete(candidate);
        }
    }
}
