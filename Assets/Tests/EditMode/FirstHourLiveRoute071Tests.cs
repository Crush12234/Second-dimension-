using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017E;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourLiveRoute071Tests
    {
        private GuildCityContent017D _content;
        private GuildCityExpeditionService017D _expeditions;

        [SetUp]
        public void SetUp()
        {
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT", "GUILD_CITY_017D"));
            _expeditions = new GuildCityExpeditionService017D();
        }

        [Test]
        public void ApplicantHookCodesBecomeNaturalInterviewCopy078()
        {
            var childApprentice =
                M1RuntimeCoordinator.RecurringApplicantPersonalHookForVerification078(
                    "HOOK_CHILD_APPRENTICE");
            var quest = M1RuntimeCoordinator.RecurringApplicantPersonalHookForVerification078(
                "QUEST_THE_THIRD_BELL");

            Assert.That(childApprentice, Is.EqualTo(
                "They asked the Guild to help protect a young apprentice they have taken responsibility for."));
            Assert.That(childApprentice, Does.Not.Contain("hook child"));
            Assert.That(quest, Is.EqualTo(
                "They hope the Guild will help them pursue the third bell."));
        }

        [Test]
        public void TenMemberFirstHourRouteRestoresCompleteBoardAroundThreeStoryBattles()
        {
            var board = _content.Board(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071);
            var directorBattleIds = new FirstHourDirector071().Timeline
                .Where(value => value.IsBattle)
                .Select(value => value.EncounterId)
                .ToArray();
            var routeBattleIds = new[] { "N01", "N06", "N13" }
                .Select(board.Node)
                .Select(value => value.EncounterId)
                .ToArray();

            Assert.That(board.StartNodeId, Is.EqualTo("N00"));
            Assert.That(board.ObjectiveNodeId, Is.EqualTo("N13"));
            Assert.That(board.ExitNodeId, Is.EqualTo("N14"));
            Assert.That(routeBattleIds, Is.EqualTo(directorBattleIds));
            Assert.That(routeBattleIds, Is.EqualTo(new[]
            {
                "ENCOUNTER071_HALL_BREACH",
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                "ENCOUNTER071_GATE_EATER"
            }));
            Assert.That(board.Nodes, Has.Length.EqualTo(15));
            Assert.That(board.Nodes.Count(value =>
                StringComparer.Ordinal.Equals(value.Kind, "OPTIONAL_ELITE")), Is.EqualTo(1));
            Assert.That(board.Node("N01").Links,
                Is.EqualTo(new[] { "N02" }),
                "Una's warning must be part of the rescue story rather than a skippable branch.");
            Assert.That(board.Node("N03").Links,
                Is.EqualTo(new[] { "N04" }),
                "Tazren's trail must lead into Quin's rescue before the ambush.");
            Assert.That(board.Node("N06").Links,
                Is.EqualTo(new[] { "N07" }),
                "The post-ambush camp must not be bypassed.");
            Assert.That(board.Node("N07").Links,
                Is.EqualTo(new[] { "N08" }),
                "The camp decision must carry into the Wayglass incident.");
            Assert.That(board.Node("N08").Links,
                Is.EqualTo(new[] { "N09", "N10" }),
                "Only the optional elite may be bypassed on the authored rescue route.");
            Assert.That(board.Node("N10").Links,
                Is.EqualTo(new[] { "N11", "N12" }));
            Assert.That(board.Node("N04").Kind, Is.EqualTo("SKILL_CHECK"));
            Assert.That(board.Node("N04").EventId, Is.EqualTo("EVENT_COLLAPSED_HANDRAIL"));
            Assert.That(board.Node("N04").Links, Is.EqualTo(new[] { "N05" }));
            Assert.That(board.Node("N07").Kind, Is.EqualTo("CAMP"));
            Assert.That(board.Node("N07").EventId,
                Is.EqualTo("EVENT_LANTERN_WATCH_CAMP"));
            var chapterOneCamp = _content.Event(board.Node("N07").EventId);
            Assert.That(chapterOneCamp.UsesCommitted2d6, Is.True);
            Assert.That(chapterOneCamp.Problem,
                Does.Contain("Maren").And.Contain("Jazzi").And.Contain("Zorin")
                    .And.Contain("Lantern Watch"));
            Assert.That(board.Node("N11").Kind, Is.EqualTo("SECONDARY_OBJECTIVE"));
            Assert.That(board.Node("N11").EventId, Is.EqualTo("EVENT_GATEGLASS_PULSE"));
        }

        [TestCase(7)]
        [TestCase(9)]
        [TestCase(10)]
        public void EveryAcceptedOpeningRosterSelectsTheCompleteFifteenNodeRoute(
            int recruitCount)
        {
            var accepted = Require(_expeditions.AcceptContract(
                CreateCampaign(recruitCount),
                _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            var boardId = accepted.Guild.GuildCity.ActiveContract.BoardId;
            var board = _content.Board(boardId);

            Assert.That(boardId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071));
            Assert.That(boardId,
                Is.Not.EqualTo(GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069));
            Assert.That(board.Nodes, Has.Length.EqualTo(15));
            Assert.That(board.ObjectiveNodeId, Is.EqualTo("N13"));
            Assert.That(board.Nodes.Count(value => !string.IsNullOrWhiteSpace(value.EncounterId)),
                Is.EqualTo(4),
                "The full route retains three required battles plus its optional elite encounter.");
            Assert.That(_content.Boards.Count, Is.EqualTo(3),
                "Runtime projections must not change the certified serialized board count.");
        }

        [Test]
        public void ResolvedSafeRouteWithNoSuppliesForcedMarchesIntoTheQuestOneClimax081()
        {
            var campaign = CreateResolvedFirstHourSafeRoute081(0, 9);

            var advanced = Require(_expeditions.CommitMove(campaign, _content, "N13"));
            var expedition = advanced.Guild.GuildCity.Expedition;

            Assert.That(expedition.CurrentNodeId, Is.EqualTo("N13"));
            Assert.That(expedition.Supplies, Is.Zero);
            Assert.That(expedition.Fatigue, Is.EqualTo(11),
                "The missing supply becomes one additional fatigue instead of stranding the story.");
            Assert.That(expedition.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.FirstHourForcedMarchFlag081));
            Assert.That(expedition.CommittedMoveIds, Has.Count.EqualTo(1),
                "The recovery move must remain an ordinary immutable board receipt.");
        }

        [Test]
        public void FundedQuestOneMoveDoesNotClaimAForcedMarch081()
        {
            var campaign = CreateResolvedFirstHourSafeRoute081(1, 9);

            var advanced = Require(_expeditions.CommitMove(campaign, _content, "N13"));
            var expedition = advanced.Guild.GuildCity.Expedition;

            Assert.That(expedition.Supplies, Is.Zero);
            Assert.That(expedition.Fatigue, Is.EqualTo(10));
            Assert.That(expedition.ObjectiveFlags,
                Does.Not.Contain(GuildCityExpeditionService017D.FirstHourForcedMarchFlag081));
        }

        [Test]
        public void ChapterTwoZeroSupplyCanStillEnterItsRescueClimax081()
        {
            var campaign = CreateActiveFirstStoryAtNode(7, "N12");
            campaign = WithExpeditionBoard(
                campaign,
                GuildCityExpeditionService017D.SecondStoryBoardId076);
            var source = campaign.Guild.GuildCity.Expedition;
            var check = new CommittedCheckState017D(
                "CHECK_CHAPTER_TWO081_SAFE_ROUTE",
                "N12",
                GuildCityExpeditionService017D.SecondStorySafeDescentEventId080,
                "R1",
                "R2",
                1,
                1,
                0,
                2,
                "FULL_SUCCESS",
                "COMMITTED081_CHAPTER_TWO_SAFE_ROUTE");
            var staged = source.With(
                supplies: 0,
                fatigue: 9,
                committedChecks: new[] { check },
                objectiveFlags: new[]
                {
                    "EVENT_RESOLVED_" +
                    GuildCityExpeditionService017D.SecondStorySafeDescentEventId080,
                    GuildCityExpeditionService017D.EventSuccessFlag076(
                        GuildCityExpeditionService017D.SecondStorySafeDescentEventId080)
                },
                lastCheckpointId: "chapter_two_081_supply_blocker_reproduction");
            var city = campaign.Guild.GuildCity.With(
                expedition: staged,
                replaceExpedition: true,
                lastCheckpointId: "chapter_two_081_supply_blocker_reproduction");
            campaign = campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);

            var advanced = Require(_expeditions.CommitMove(
                campaign,
                _content,
                "N13"));
            var expedition = advanced.Guild.GuildCity.Expedition;

            Assert.That(expedition.CurrentNodeId, Is.EqualTo("N13"));
            Assert.That(expedition.Supplies, Is.Zero);
            Assert.That(expedition.Fatigue, Is.EqualTo(11));
            Assert.That(expedition.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.StoryQuestForcedMarchFlag081));
            Assert.That(expedition.ObjectiveFlags,
                Does.Not.Contain(GuildCityExpeditionService017D.FirstHourForcedMarchFlag081));
        }

        [Test]
        public void ForcedMarchRecoveryDoesNotRelaxOtherExpeditionBoards081()
        {
            var campaign = CreateActiveFirstStoryAtNode(7, "N01");
            campaign = WithExpeditionBoard(
                campaign,
                GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069);
            var source = campaign.Guild.GuildCity.Expedition;
            var city = campaign.Guild.GuildCity.With(
                expedition: source.With(supplies: 0),
                replaceExpedition: true);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);

            var rejected = _expeditions.CommitMove(campaign, _content, "N04");

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain("GC017D_SUPPLIES_INSUFFICIENT"));
        }

        [Test]
        public void RetiredGuidedCorridorCannotReopenForAnyBoard()
        {
            Assert.That(M1FlowPresenter.IsGuidedFirstHourBoardForVerification076(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071), Is.False);
            Assert.That(M1FlowPresenter.IsGuidedFirstHourBoardForVerification076(
                GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069), Is.False);
            Assert.That(M1FlowPresenter.IsGuidedFirstHourBoardForVerification076(
                "BOARD_BELL_BENEATH_GATE"), Is.False);
            Assert.That(M1FlowPresenter.IsGuidedFirstHourBoardForVerification076(
                "BOARD_LINES_NOT_RETURNED"), Is.False);
            Assert.That(M1FlowPresenter.IsGuidedFirstHourBoardForVerification076(null), Is.False);
        }

        [Test]
        public void EachDirectorBattleBeatCommitsOneDistinctCertifiedLaunchRequest()
        {
            var expected = new[]
            {
                new { NodeId = "N01", EncounterId = "ENCOUNTER071_HALL_BREACH", EnemyUnions = 2,
                    Objective = "Protect the Guild Hall while Kael contains the breach." },
                new { NodeId = "N06", EncounterId = "ENCOUNTER071_LANTERN_ROAD_AMBUSH", EnemyUnions = 2,
                    Objective = "Break the Lantern Road ambush and reopen the way to the patrol." },
                new { NodeId = "N13", EncounterId = "ENCOUNTER071_GATE_EATER", EnemyUnions = 3,
                    Objective = "Defeat the Gate-Eater before it reaches Skyhome." }
            };
            var requestIds = new HashSet<string>(StringComparer.Ordinal);
            var battleIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var beat in expected)
            {
                var campaign = Require(_expeditions.AcceptContract(
                    CreateCampaign(GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071),
                    _content,
                    GuildCityExpeditionService017D.FirstStoryContractId066));
                campaign = Require(_expeditions.StartExpedition(campaign, _content));
                var stagedExpedition = campaign.Guild.GuildCity.Expedition.With(
                    currentNodeId: beat.NodeId,
                    status: ExpeditionStatus017D.Active,
                    visitedNodeIds: new[] { "N00", beat.NodeId },
                    revealedNodeIds: new[] { "N00", beat.NodeId },
                    objectiveFlags: Array.Empty<string>(),
                    lastCheckpointId: "first_hour_071_test_stage");
                var stagedCity = campaign.Guild.GuildCity.With(
                    expedition: stagedExpedition,
                    replaceExpedition: true,
                    pendingEncounter: null,
                    replacePendingEncounter: true,
                    pendingBattleReturn: null,
                    replacePendingBattleReturn: true,
                    lastCheckpointId: "first_hour_071_test_stage");
                campaign = campaign.With(campaign.Guild.WithGuildCity(stagedCity), campaign.OpeningFlow);

                if (StringComparer.Ordinal.Equals(beat.NodeId, "N13"))
                    campaign = Require(_expeditions.MarkFirstHourLanternPatrolRescued071(
                        campaign, _content));

                campaign = Require(_expeditions.CommitEncounter(campaign, _content, beat.EncounterId));
                var request = campaign.Guild.GuildCity.PendingEncounter;
                Assert.That(request, Is.Not.Null);
                Assert.That(request.NodeId, Is.EqualTo(beat.NodeId));
                Assert.That(request.EncounterId, Is.EqualTo(beat.EncounterId));
                Assert.That(request.Objective, Is.EqualTo(beat.Objective));
                Assert.That(request.EnemyUnionCount, Is.EqualTo(beat.EnemyUnions));
                Assert.That(requestIds.Add(request.RequestId), Is.True);
                Assert.That(battleIds.Add(request.BattleId), Is.True);

                var replay = Require(_expeditions.CommitEncounter(campaign, _content, beat.EncounterId));
                Assert.That(replay.Guild.GuildCity.PendingEncounter.RequestId,
                    Is.EqualTo(request.RequestId), "Re-entering a committed beat must not reroll it.");
            }
        }

        [Test]
        public void PatrolRescueRejectsWrongNodeAndWrongBoard()
        {
            var wrongNode = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N06");
            var wrongNodeResult = _expeditions.MarkFirstHourLanternPatrolRescued071(
                wrongNode, _content);
            Assert.That(wrongNodeResult.IsSuccess, Is.False);
            Assert.That(wrongNodeResult.Errors,
                Does.Contain("GC017D_FIRST_HOUR_PATROL_NODE_REQUIRED"));
            Assert.That(wrongNode.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Not.Contain(GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071));

            var wrongBoard = WithExpeditionBoard(
                CreateActiveFirstStoryAtNode(7, "N13"),
                GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069);
            var wrongBoardResult = _expeditions.MarkFirstHourLanternPatrolRescued071(
                wrongBoard, _content);
            Assert.That(wrongBoardResult.IsSuccess, Is.False);
            Assert.That(wrongBoardResult.Errors,
                Does.Contain("GC017D_FIRST_HOUR_PATROL_BOARD_REQUIRED"));
        }

        [Test]
        public void GateEaterIsBlockedUntilPatrolRescueAndRescueIsExactlyIdempotent()
        {
            var staged = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N13");

            var blocked = _expeditions.CommitEncounter(
                staged, _content, GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors,
                Does.Contain("GC017D_FIRST_HOUR_PATROL_RESCUE_REQUIRED"));
            Assert.That(staged.Guild.GuildCity.PendingEncounter, Is.Null);

            var rescued = Require(_expeditions.MarkFirstHourLanternPatrolRescued071(
                staged, _content));
            var replayed = Require(_expeditions.MarkFirstHourLanternPatrolRescued071(
                rescued, _content));
            Assert.That(CanonicalJson.Sha256Hex(replayed),
                Is.EqualTo(CanonicalJson.Sha256Hex(rescued)));
            Assert.That(rescued.Guild.GuildCity.Expedition.ObjectiveFlags.Count(value =>
                StringComparer.Ordinal.Equals(value,
                    GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071)),
                Is.EqualTo(1));

            var committed = Require(_expeditions.CommitEncounter(
                rescued, _content, GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071));
            Assert.That(committed.Guild.GuildCity.PendingEncounter, Is.Not.Null);
            Assert.That(committed.Guild.GuildCity.PendingEncounter.EncounterId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071));
        }

        [Test]
        public void ActiveRescueLocksManualUnionPlanCountButAllowsAuthorizedPatrolMaterialization084()
        {
            var commands = new M1CommandService();
            var staged = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N13");
            var rescued = Require(_expeditions.MarkFirstHourLanternPatrolRescued071(
                staged, _content));
            var roster = FirstHourRosterService071.LoadFromContentRoot(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT"));
            var rostered = Require(roster.EnsureLanternPatrol(rescued));
            var beforeManualCommands = CanonicalJson.Sha256Hex(rostered);

            var addRejected = commands.AddOpeningUnion(rostered);

            Assert.That(addRejected.IsSuccess, Is.False);
            Assert.That(addRejected.Errors,
                Does.Contain("M1_RETURN_BEFORE_REORGANIZING_UNIONS"));
            Assert.That(CanonicalJson.Sha256Hex(rostered), Is.EqualTo(beforeManualCommands));

            var emptyReinforcement = new UnionState(
                "UNION_OPENING_03",
                "Opening Union 3",
                UnionKind.Normal,
                null,
                Array.Empty<string>(),
                "FORMATION_SKIRMISH_LINE",
                "DOCTRINE_BALANCED",
                0,
                10_000);
            var removalGuild = rostered.Guild.With(
                rostered.Guild.TreasuryXp,
                rostered.Guild.Recruits,
                rostered.Guild.Unions.Concat(new[] { emptyReinforcement }).ToArray(),
                rostered.Guild.Inventory);
            var removalCandidate = rostered.With(removalGuild, rostered.OpeningFlow);

            var removeRejected = commands.RemoveOpeningUnion(removalCandidate, 2);

            Assert.That(removeRejected.IsSuccess, Is.False);
            Assert.That(removeRejected.Errors,
                Does.Contain("M1_RETURN_BEFORE_REORGANIZING_UNIONS"));

            var materialized = Require(
                commands.MaterializeFirstHourLanternPatrolUnions071(rostered));
            Assert.That(materialized.Guild.Unions.Select(value => value.MemberRecruitIds.Count),
                Is.EqualTo(new[] { 6, 6, 4 }));
            Assert.That(materialized.Guild.Unions.SelectMany(value => value.MemberRecruitIds)
                    .Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(16));
            var firstMaterializationHash = CanonicalJson.Sha256Hex(materialized);

            var replayed = Require(
                commands.MaterializeFirstHourLanternPatrolUnions071(materialized));

            Assert.That(CanonicalJson.Sha256Hex(replayed),
                Is.EqualTo(firstMaterializationHash),
                "The rescue-only Union transaction must replay without changing any state.");
        }

        [Test]
        public void AuthorizedPatrolMaterializationNormalizesLegacyFormationLine084()
        {
            var staged = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N13");
            var rescued = Require(_expeditions.MarkFirstHourLanternPatrolRescued071(
                staged, _content));
            var roster = FirstHourRosterService071.LoadFromContentRoot(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT"));
            var rostered = Require(roster.EnsureLanternPatrol(rescued));
            var legacyUnions = rostered.Guild.Unions.Select(value => new UnionState(
                value.UnionId,
                value.DisplayName,
                value.Kind,
                value.LeaderRecruitId,
                value.MemberRecruitIds,
                "FORMATION_LINE",
                value.DoctrineId,
                value.SharedAp,
                value.CohesionBasisPoints)).ToArray();
            var legacyGuild = rostered.Guild.With(
                rostered.Guild.TreasuryXp,
                rostered.Guild.Recruits,
                legacyUnions,
                rostered.Guild.Inventory);
            var legacy = rostered.With(legacyGuild, rostered.OpeningFlow);
            var commands = new M1CommandService();

            Assert.That(commands.ValidateGuildUnionPlans(legacy.Guild).IsSuccess, Is.False,
                "The retired formation must be repaired only at the supported command boundary.");

            var materialized = Require(
                commands.MaterializeFirstHourLanternPatrolUnions071(legacy));

            Assert.That(materialized.Guild.Unions.Select(value => value.MemberRecruitIds.Count),
                Is.EqualTo(new[] { 6, 6, 4 }));
            Assert.That(materialized.Guild.Unions.Select(value => value.FormationId),
                Is.EqualTo(new[]
                {
                    "FORMATION_SKIRMISH_LINE",
                    "FORMATION_SKIRMISH_LINE",
                    "FORMATION_SHIELD_WALL"
                }),
                "Legacy U1/U2 plans normalize to Skirmish Line; because those compatibility " +
                "IDs do not occupy UNION_OPENING_01, the new reinforcement plan receives " +
                "ordinal 1's deterministic Shield Wall default.");
            Assert.That(materialized.Guild.Unions.Select(value => value.FormationId),
                Does.Not.Contain("FORMATION_LINE"));
            Assert.That(materialized.Guild.Unions.All(value =>
                    OpeningUnionCatalog.IsFormation(value.FormationId)),
                Is.True);
            var firstMaterializationHash = CanonicalJson.Sha256Hex(materialized);
            var replayed = Require(
                commands.MaterializeFirstHourLanternPatrolUnions071(materialized));
            Assert.That(CanonicalJson.Sha256Hex(replayed),
                Is.EqualTo(firstMaterializationHash));
        }

        [Test]
        public void PatrolMaterializationRejectsAnAlreadyClearedGateEater084()
        {
            var staged = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N13");
            var rescued = Require(_expeditions.MarkFirstHourLanternPatrolRescued071(
                staged, _content));
            var roster = FirstHourRosterService071.LoadFromContentRoot(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT"));
            var rostered = Require(roster.EnsureLanternPatrol(rescued));
            var expedition = rostered.Guild.GuildCity.Expedition;
            var flags = expedition.ObjectiveFlags.Concat(new[]
            {
                GuildCityExpeditionService017D.EncounterClearedFlag("N13")
            }).ToArray();
            var clearedExpedition = expedition.With(objectiveFlags: flags);
            var clearedCity = rostered.Guild.GuildCity.With(
                expedition: clearedExpedition,
                replaceExpedition: true);
            var cleared = rostered.With(
                rostered.Guild.WithGuildCity(clearedCity),
                rostered.OpeningFlow);

            var rejected = new M1CommandService()
                .MaterializeFirstHourLanternPatrolUnions071(cleared);

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors,
                Does.Contain("GC017D_FIRST_HOUR_PATROL_RESCUE_TOO_LATE"));
        }

        [Test]
        public void RuntimeRescueSavesTwentyPlayableMembersBeforeGateEaterCommit()
        {
            var staged = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N13");
            var savePath = Path.Combine(Path.GetTempPath(),
                "sd071_patrol_rescue_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                var campaignField = typeof(M1RuntimeCoordinator).GetField(
                    "_campaign",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                Assert.That(campaignField, Is.Not.Null);
                campaignField.SetValue(coordinator, staged);
                Assert.That(coordinator.GuildCity017D.Expedition.CanCommitEncounter, Is.False,
                    "The boss read model must stay locked before the patrol rescue is saved.");

                var rescued = coordinator.RescueFirstHourLanternPatrol071();

                Assert.That(rescued.Succeeded, Is.True, rescued.Message);
                Assert.That(coordinator.GuildCity017D.TotalRecruitCount, Is.EqualTo(20));
                Assert.That(coordinator.GuildCity017D.Expedition.ObjectiveFlags,
                    Does.Contain(GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071));
                Assert.That(coordinator.GuildCity017D.Expedition.CanCommitEncounter, Is.True,
                    "The boss becomes available only after the saved patrol transaction completes.");
                var liveCampaign = (CampaignState)campaignField.GetValue(coordinator);
                Assert.That(liveCampaign.Guild.Unions, Has.Count.EqualTo(3),
                    "Under the six-member campaign rules, the ten rescued Lanterns fill the " +
                    "six open founder slots before one four-member reinforcement Union is added.");
                Assert.That(liveCampaign.Guild.Unions.All(value =>
                    value.MemberRecruitIds.Count <= NormalUnionPlanRules.MaximumMembersPerUnion),
                    Is.True);
                var assigned = new HashSet<string>(liveCampaign.Guild.Unions
                    .SelectMany(value => value.MemberRecruitIds), StringComparer.Ordinal);
                var patrolRecruitIds = liveCampaign.Guild.Recruits
                    .Where(value => FirstHourRosterService071.PatrolStableRecruitIds.Contains(
                        value.AuthoredStableRecruitId))
                    .Select(value => value.RecruitId)
                    .ToArray();
                Assert.That(patrolRecruitIds, Has.Length.EqualTo(10));
                Assert.That(patrolRecruitIds.All(assigned.Contains), Is.True,
                    "All ten rescued patrol members should be fieldable within the legal six-member Union plans.");

                var firstTransactionHash = CanonicalJson.Sha256Hex(liveCampaign);
                var replayedTransaction = coordinator.RescueFirstHourLanternPatrol071();
                Assert.That(replayedTransaction.Succeeded, Is.True, replayedTransaction.Message);
                liveCampaign = (CampaignState)campaignField.GetValue(coordinator);
                Assert.That(CanonicalJson.Sha256Hex(liveCampaign), Is.EqualTo(firstTransactionHash),
                    "Replaying the saved rescue must not duplicate recruits, flags, or Unions.");

                var persisted = new AtomicSaveStore().ReadWithRecovery(savePath);
                Assert.That(persisted.IsSuccess, Is.True, string.Join("\n", persisted.Errors));
                Assert.That(persisted.Value.CampaignState.Guild.Recruits, Has.Count.EqualTo(20));
                Assert.That(persisted.Value.CampaignState.Guild.GuildCity.Expedition.ObjectiveFlags,
                    Does.Contain(GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071));

                var committed = coordinator.CommitGuildCityEncounter017D(
                    GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071);
                Assert.That(committed.Succeeded, Is.True, committed.Message);
            }
            finally
            {
                DeleteSaveFamily(savePath);
            }
        }

        [Test]
        public void ThreeBattleFallbackNodesUseTheSameReadableStoryNames()
        {
            Assert.That(GuildCityOpeningExperienceRegistry017E.Node(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071, "N01").displayName,
                Is.EqualTo("The Hall Breach"));
            Assert.That(GuildCityOpeningExperienceRegistry017E.Node(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071, "N06").displayName,
                Is.EqualTo("Lantern Road Ambush"));
            Assert.That(GuildCityOpeningExperienceRegistry017E.Node(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071, "N13").displayName,
                Is.EqualTo("The Old Gatehouse"));
        }

        private static CampaignState CreateCampaign(int recruitCount)
        {
            var recruits = Enumerable.Range(1, recruitCount)
                .Select(index => new RecruitState("R" + index, 100, 100, 20, 20))
                .ToArray();
            var unions = new[]
            {
                new UnionState("U1", "First Union", UnionKind.Normal, "R1",
                    new[] { "R1", "R2", "R3" }, "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Second Union", UnionKind.Normal, "R4",
                    new[] { "R4", "R5", "R6" }, "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED", 30, 7000)
            };
            var guild = new GuildState("GUILD_FIRST_HOUR_071", 0, recruits, unions);
            var flow = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001",
                true, null, false, 439, 0, true, true, true, true, "complete");
            var profile = new NewGuildProfileState("Tester", SecondDimension.Core.GameMode.Standard,
                TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false);
            return new CampaignState("00000000-0000-0000-0000-000000071071", 71071,
                "1.0", ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }

        private CampaignState CreateActiveFirstStoryAtNode(int recruitCount, string nodeId)
        {
            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign(recruitCount),
                _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            var expedition = campaign.Guild.GuildCity.Expedition.With(
                currentNodeId: nodeId,
                status: ExpeditionStatus017D.Active,
                visitedNodeIds: new[] { "N00", nodeId },
                revealedNodeIds: new[] { "N00", nodeId },
                objectiveFlags: Array.Empty<string>(),
                lastCheckpointId: "first_hour_071_test_stage");
            var city = campaign.Guild.GuildCity.With(
                expedition: expedition,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true,
                lastCheckpointId: "first_hour_071_test_stage");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private CampaignState CreateResolvedFirstHourSafeRoute081(int supplies, int fatigue)
        {
            var campaign = CreateActiveFirstStoryAtNode(
                GuildCityExpeditionService017D.FirstHourThreeBattleRosterCount071,
                "N12");
            var source = campaign.Guild.GuildCity.Expedition;
            var check = new CommittedCheckState017D(
                "CHECK_FIRST_HOUR081_SAFE_ROUTE",
                "N12",
                GuildCityExpeditionService017D.FirstStorySafePassageEventId080,
                "R1",
                "R2",
                0,
                0,
                0,
                0,
                "FULL_SUCCESS",
                "AUTHORED080_FIRST_HOUR081_SAFE_ROUTE");
            var objectiveFlags = new[]
            {
                "EVENT_RESOLVED_" + GuildCityExpeditionService017D.FirstStorySafePassageEventId080,
                GuildCityExpeditionService017D.EventSuccessFlag076(
                    GuildCityExpeditionService017D.FirstStorySafePassageEventId080)
            };
            var staged = source.With(
                supplies: supplies,
                fatigue: fatigue,
                committedChecks: new[] { check },
                objectiveFlags: objectiveFlags,
                lastCheckpointId: "first_hour_081_supply_blocker_reproduction");
            var city = campaign.Guild.GuildCity.With(
                expedition: staged,
                replaceExpedition: true,
                lastCheckpointId: "first_hour_081_supply_blocker_reproduction");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState WithExpeditionBoard(
            CampaignState campaign,
            string boardId)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var replacement = new ExpeditionState017D(
                source.ExpeditionId,
                source.ContractCommitId,
                boardId,
                source.CurrentNodeId,
                source.Status,
                source.Supplies,
                source.Fatigue,
                source.Threat,
                source.Urgency,
                source.VisitedNodeIds,
                source.RevealedNodeIds,
                source.CommittedMoveIds,
                source.CommittedChecks,
                source.ObjectiveFlags,
                source.LastCheckpointId);
            var city = campaign.Guild.GuildCity.With(
                expedition: replacement,
                replaceExpedition: true);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static void DeleteSaveFamily(string savePath)
        {
            foreach (var path in new[] { savePath, savePath + ".bak", savePath + ".tmp" })
                if (File.Exists(path)) File.Delete(path);
        }

        private static CampaignState Require(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
