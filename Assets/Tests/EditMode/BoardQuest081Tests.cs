using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardQuest081Tests
    {
        private const string ReliefRoadContractId081 = "CONTRACT_RELIEF_ROAD";

        private GuildCityContent017D _content;
        private GuildCityExpeditionService017D _expeditions;

        [SetUp]
        public void SetUp()
        {
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT",
                "GUILD_CITY_017D"));
            _expeditions = new GuildCityExpeditionService017D();
        }

        [Test]
        public void FiveSpaceBoardUsesOnlyPlainPlayerFacingMilestones081()
        {
            var labels = Enumerable.Range(0, 5)
                .Select(BoardQuestRules081.SpaceLabel081)
                .ToArray();

            Assert.That(labels, Is.EqualTo(new[]
            {
                "LEAVE HOME",
                "FOLLOW THE TRAIL",
                "FACE THE DANGER",
                "SAVE THE PEOPLE",
                "BRING THEM HOME"
            }));
            Assert.That(BoardQuestRules081.SpaceLabel081(-1), Is.EqualTo("LEAVE HOME"));
            Assert.That(BoardQuestRules081.SpaceLabel081(99), Is.EqualTo("BRING THEM HOME"));
            var visibleCopy = string.Join("\n", labels);
            Assert.That(visibleCopy, Does.Not.Contain("NODE").IgnoreCase);
            Assert.That(visibleCopy, Does.Not.Contain("GRAPH").IgnoreCase);
            Assert.That(visibleCopy, Does.Not.Match(@"\bN\d{2}\b"));
        }

        [TestCase("EXCEPTIONAL", "PERFECT ROLL")]
        [TestCase("FULL_SUCCESS", "CLEAN SUCCESS")]
        [TestCase("SUCCESS_WITH_COST", "SUCCESS — WITH A COST")]
        [TestCase("SETBACK", "SETBACK — KEEP MOVING")]
        [TestCase("SEVERE_SETBACK", "HARD SETBACK — THE STORY CONTINUES")]
        public void DiceOutcomesUsePlainVisibleHeadings081(
            string outcome,
            string expectedHeading)
        {
            Assert.That(BoardQuestRules081.OutcomeHeading081(outcome),
                Is.EqualTo(expectedHeading));
        }

        [TestCase("EXCEPTIONAL", 12)]
        [TestCase("FULL_SUCCESS", 8)]
        [TestCase("SUCCESS_WITH_COST", 5)]
        [TestCase("SETBACK", 2)]
        [TestCase("SEVERE_SETBACK", 1)]
        public void VisibleGuildXpMatchesGameplayAuthority081(string outcome, int expectedXp)
        {
            Assert.That(BoardQuestRules081.GuildXpForOutcome081(outcome),
                Is.EqualTo(expectedXp));
            Assert.That(GuildCityExpeditionService017D.BoardGameCheckGuildXp081(outcome),
                Is.EqualTo(expectedXp));
        }

        [TestCase(GuildCityExpeditionService017D.LegacyFirstRescueBoardId069, true)]
        [TestCase(GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069, true)]
        [TestCase(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071, true)]
        [TestCase(GuildCityExpeditionService017D.SecondStoryBoardId076, true)]
        [TestCase(GuildCityExpeditionService017D.ReliefRoadBoardId081, true)]
        [TestCase("BOARD_FUTURE_UNRELATED_OPERATION", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void BoardQuestRewardsAreScopedToPresentedOpeningBoards081(
            string boardId,
            bool expected)
        {
            Assert.That(
                GuildCityExpeditionService017D.UsesBoardQuestRewards081(boardId),
                Is.EqualTo(expected));
        }

        [Test]
        public void QuestStartsAtGuildDoorBeforeTheFirstFaceDownRoom081()
        {
            var presenterObject = new GameObject("Guild Door Presenter Test 081");
            var readyRoot = new GameObject(
                "Guild Door Ready Root Test 081",
                typeof(RectTransform));
            var startedRoot = new GameObject(
                "Guild Door Started Root Test 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var ready = new GuildCityPresentationState017D
                {
                    IsAvailable = true,
                    HasActiveContract = true,
                    TreasuryXp = 9
                };
                var readyView = ExpeditionBoardProjection074.Build(ready);

                Assert.That(readyView.ActionKind,
                    Is.EqualTo(ExpeditionBoardActionKind074.BeginExpedition));
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestHeader081",
                    null,
                    readyRoot.transform,
                    ready,
                    readyView);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestPath081",
                    presenter,
                    readyRoot.transform,
                    ready,
                    readyView);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestActions081",
                    presenter,
                    readyRoot.transform,
                    null,
                    ready,
                    readyView);

                Assert.That(VisibleText081(
                        readyRoot.transform,
                        "Board Quest Spendable XP Resources 081"),
                    Is.EqualTo("XP TO SPEND  9\nQUEST READY"));
                Assert.That(VisibleText081(
                        readyRoot.transform,
                        "Board Quest Space Label 0 081"),
                    Is.EqualTo("◆  LEAVE HOME"));
                Assert.That(VisibleText081(
                        readyRoot.transform,
                        "Board Quest Space Label 1 081"),
                    Is.EqualTo("○  FOLLOW THE TRAIL"));
                Assert.That(readyRoot.GetComponentsInChildren<Text>(true).Count(value =>
                        value.gameObject.name.StartsWith(
                            "Board Quest Space Label ",
                            StringComparison.Ordinal)),
                    Is.EqualTo(5));
                Assert.That(readyRoot.GetComponentsInChildren<Button>(true)
                        .Select(value => value.GetComponentInChildren<Text>(true)?.text),
                    Contains.Item("START QUEST\nPLACE PAWN AT GUILD DOOR"));

                var started = new GuildCityPresentationState017D
                {
                    IsAvailable = true,
                    HasActiveContract = true,
                    Expedition = new GuildCityExpeditionView017D
                    {
                        ExpeditionId = "EXP_GUILD_DOOR_START_081",
                        BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                        CurrentNodeId = "N00",
                        CurrentNodeKind = "START",
                        Status = "Active",
                        Supplies = 12,
                        Fatigue = 0,
                        Urgency = 14,
                        ResolutionComplete = true,
                        CanMove = true,
                        LinkedNodeIds = new[] { "N01" },
                        VisitedNodeIds = new[] { "N00" },
                        RevealedNodeIds = new[] { "N00" },
                        ObjectiveFlags = Array.Empty<string>()
                    }
                };
                var startedView = ExpeditionBoardProjection074.Build(started);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestPath081",
                    presenter,
                    startedRoot.transform,
                    started,
                    startedView);

                Assert.That(VisibleText081(
                        startedRoot.transform,
                        "Board Quest Space Label 0 081"),
                    Is.EqualTo("◆  LEAVE HOME"));
                Assert.That(VisibleText081(
                        startedRoot.transform,
                        "Board Quest Space Label 1 081"),
                    Is.EqualTo("○  FOLLOW THE TRAIL"));
                Assert.That(startedRoot.GetComponentsInChildren<Text>(true).Count(value =>
                        value.gameObject.name.StartsWith(
                            "Board Quest Space Label ",
                            StringComparison.Ordinal)),
                    Is.EqualTo(5));

                var campaign = Require(_expeditions.AcceptContract(
                    CreateCampaign081(),
                    _content,
                    GuildCityExpeditionService017D.FirstStoryContractId066));
                campaign = Require(_expeditions.StartExpedition(campaign, _content));
                var authoritativeStart = campaign.Guild.GuildCity.Expedition;
                Assert.That(authoritativeStart.CurrentNodeId, Is.EqualTo("N00"));
                Assert.That(authoritativeStart.VisitedNodeIds,
                    Is.EquivalentTo(new[] { "N00" }));
                Assert.That(authoritativeStart.CommittedMoveIds, Is.Empty,
                    "Starting a quest places the pawn; it must not silently consume Room 1.");

                var firstRoom = Require(_expeditions.CommitMove(campaign, _content, "N01"))
                    .Guild.GuildCity.Expedition;
                Assert.That(firstRoom.CurrentNodeId, Is.EqualTo("N01"));
                Assert.That(firstRoom.VisitedNodeIds,
                    Is.EquivalentTo(new[] { "N00", "N01" }));
                Assert.That(firstRoom.CommittedMoveIds, Has.Count.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(startedRoot);
                UnityEngine.Object.DestroyImmediate(readyRoot);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [TestCase(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
            "THE BELL BENEATH SKYHOME")]
        [TestCase(GuildCityExpeditionService017D.SecondStoryBoardId076,
            "THE LINES NOT RETURNED")]
        [TestCase(GuildCityExpeditionService017D.ReliefRoadBoardId081,
            "KEEP THE RELIEF ROAD OPEN")]
        public void ReadyQuestUsesItsActualContractBoardGoalAndTitle081(
            string boardId,
            string expectedHeader)
        {
            var root = new GameObject("Ready Contract Header Test 081", typeof(RectTransform));
            try
            {
                const string objective = "Escort the people named in this contract and return safely.";
                var state = new GuildCityPresentationState017D
                {
                    IsAvailable = true,
                    HasActiveContract = true,
                    Contracts = new[]
                    {
                        new GuildCityContractView017D
                        {
                            ContractId = "CONTRACT_READY_HEADER_081",
                            BoardId = boardId,
                            DisplayName = expectedHeader,
                            PrimaryObjective = objective,
                            IsActive = true
                        }
                    }
                };
                var view = ExpeditionBoardProjection074.Build(state);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestHeader081",
                    null,
                    root.transform,
                    state,
                    view);

                Assert.That(view.BoardId, Is.EqualTo(boardId));
                Assert.That(view.StoryObjective, Is.EqualTo(objective));
                Assert.That(VisibleText081(root.transform, "Board Quest Chapter 081"),
                    Does.Contain(expectedHeader));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReliefRoadObjectivesStayOnTheMedicineConvoyInEveryBoardState081()
        {
            var pendingBattle = ReliefRoadPresentationState081();
            pendingBattle.HasPendingEncounter = true;
            pendingBattle.Expedition.CurrentNodeId = "N06";
            pendingBattle.Expedition.CurrentNodeKind = "ENCOUNTER";
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.Build(pendingBattle).StoryObjective,
                "medicine wagons",
                "win the road");

            var readyToFinish = ReliefRoadPresentationState081();
            readyToFinish.Expedition.CurrentNodeId = "N14";
            readyToFinish.Expedition.CurrentNodeKind = "EXIT";
            readyToFinish.Expedition.ResolutionComplete = true;
            readyToFinish.Expedition.CanFinalizeOperation = true;
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.Build(readyToFinish).StoryObjective,
                "convoy reached the outer wards",
                "bank the operation reward");

            var optionalNest = ReliefRoadPresentationState081();
            optionalNest.Expedition.CurrentNodeId = "N09";
            optionalNest.Expedition.CurrentNodeKind = "OPTIONAL_ELITE";
            optionalNest.Expedition.ResolutionComplete = true;
            optionalNest.Expedition.CanCommitEncounter = true;
            optionalNest.Expedition.CanMove = true;
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.Build(optionalNest).StoryObjective,
                "raider elite",
                "reopen the relief road");

            var readyToFight = ReliefRoadPresentationState081();
            readyToFight.Expedition.CurrentNodeId = "N06";
            readyToFight.Expedition.CurrentNodeKind = "ENCOUNTER";
            readyToFight.Expedition.ResolutionComplete = true;
            readyToFight.Expedition.CanCommitEncounter = true;
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.Build(readyToFight).StoryObjective,
                "Union battle",
                "medicine wagon");

            var unresolvedProblem = ReliefRoadPresentationState081();
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.Build(unresolvedProblem).StoryObjective,
                "medicine wagons",
                "outer wards");

            var nextRoom = ReliefRoadPresentationState081();
            nextRoom.Expedition.ResolutionComplete = true;
            nextRoom.Expedition.CanMove = true;
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.Build(nextRoom).StoryObjective,
                "Guild pawn",
                "face-down room",
                "relief convoy");
        }

        [Test]
        public void ReliefRoadCompanionCopyStaysWithTheConvoyBeforeAndDuringTheQuest081()
        {
            var prepared = new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                Contracts = new[]
                {
                    new GuildCityContractView017D
                    {
                        ContractId = ReliefRoadContractId081,
                        BoardId = GuildCityExpeditionService017D.ReliefRoadBoardId081,
                        DisplayName = "Keep the Relief Road Open",
                        IsActive = true
                    }
                }
            };
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.CompanionStoryBeat076(prepared, null),
                "outer wards",
                "convoy");

            var active = ReliefRoadPresentationState081();
            for (var index = 0; index < ExpeditionBoardProjection074.NodeCount074; index++)
            {
                active.Expedition.CurrentNodeId = "N" + index.ToString("00");
                var view = ExpeditionBoardProjection074.Build(active);
                var current = view.Nodes.Single(value => value.IsCurrent);
                AssertReliefRoadCopy081(
                    ExpeditionBoardProjection074.CompanionStoryBeat076(active, current));
            }

            active.Expedition.CurrentNodeId = "N04";
            var culvertView = ExpeditionBoardProjection074.Build(active);
            AssertReliefRoadCopy081(
                ExpeditionBoardProjection074.CompanionStoryBeat076(
                    active,
                    culvertView.Nodes.Single(value => value.IsCurrent)),
                "culvert",
                "convoy");
        }

        [Test]
        public void GenericRoomsAndForkOrderShuffleDeterministicallyPerExpedition081()
        {
            var expeditionIds = Enumerable.Range(0, 64)
                .Select(index => "EXP_ROOM_DECK_081_" + index.ToString("000"))
                .ToArray();
            var firstReveal = expeditionIds
                .Select(value => GuildCityExpeditionService017D.BoardRoomKind081(
                    value,
                    "N03",
                    "SCOUTING"))
                .ToArray();
            var reloadedReveal = expeditionIds
                .Select(value => GuildCityExpeditionService017D.BoardRoomKind081(
                    value,
                    "N03",
                    "SCOUTING"))
                .ToArray();
            var allowed = new HashSet<string>(new[]
            {
                "TREASURE", "SKILL", "BLESSING", "DISCOVERY"
            }, StringComparer.Ordinal);

            Assert.That(reloadedReveal, Is.EqualTo(firstReveal),
                "Reloading the same expedition must reveal the same room deck.");
            Assert.That(firstReveal.All(allowed.Contains), Is.True);
            Assert.That(firstReveal.Distinct(StringComparer.Ordinal).Count(),
                Is.GreaterThan(1),
                "The deterministic deck must still vary room types across expedition identities.");

            var destinations = new[] { "N09", "N10", "N11", "N12" };
            var firstOrder = destinations.OrderBy(value =>
                    GuildCityExpeditionService017D.BoardRoomShuffleKey081(
                        "EXP_ROUTE_SHUFFLE_081",
                        "N08",
                        value),
                    StringComparer.Ordinal)
                .ToArray();
            var reloadedOrder = destinations.OrderBy(value =>
                    GuildCityExpeditionService017D.BoardRoomShuffleKey081(
                        "EXP_ROUTE_SHUFFLE_081",
                        "N08",
                        value),
                    StringComparer.Ordinal)
                .ToArray();
            var keys = destinations.Select(value =>
                    GuildCityExpeditionService017D.BoardRoomShuffleKey081(
                        "EXP_ROUTE_SHUFFLE_081",
                        "N08",
                        value))
                .ToArray();

            Assert.That(reloadedOrder, Is.EqualTo(firstOrder));
            Assert.That(keys.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(destinations.Length));
        }

        [TestCase("ENCOUNTER")]
        [TestCase("OPTIONAL_ELITE")]
        public void AuthoredCombatRoomsNeverShuffleAwayTheirBattle081(string authoredKind)
        {
            for (var index = 0; index < 32; index++)
                Assert.That(GuildCityExpeditionService017D.BoardRoomKind081(
                        "EXP_FIXED_FIGHT_081_" + index,
                        "N06",
                        authoredKind),
                    Is.EqualTo("MONSTER"));
        }

        [Test]
        public void AuthoredSafeRouteEventIsAlwaysAVisibleFateRoom081()
        {
            var state = ChapterTwoEventPresentationState081(false);
            state.Expedition.CurrentNodeId = "N12";
            state.Expedition.CurrentNodeKind = "SAFE_ROUTE";
            state.Expedition.CurrentEventId =
                GuildCityExpeditionService017D.SecondStorySafeDescentEventId080;
            state.Expedition.LinkedNodeIds = new[] { "N13" };
            state.Expedition.VisitedNodeIds = new[] { "N00", "N10", "N12" };
            state.Expedition.RevealedNodeIds = new[] { "N00", "N10", "N12", "N13" };
            var view = ExpeditionBoardProjection074.Build(state);
            var current = view.Nodes.Single(value => value.IsCurrent);

            Assert.That(current.Kind, Is.EqualTo("SAFE_ROUTE"));
            Assert.That(BoardQuestRules081.RoomKind081(state, current), Is.EqualTo("FATE"));
            Assert.That(BoardQuestRules081.RoomTitle081(
                    BoardQuestRules081.RoomKind081(state, current)),
                Is.EqualTo("FATE ROOM"));

            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign081(),
                _content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            var source = campaign.Guild.GuildCity.Expedition;
            var astrolabeCheck = new CommittedCheckState017D(
                "CHECK_FATE_ROUTE_SOURCE_081",
                "N10",
                "EVENT_BROKEN_ASTROLABE",
                "R1",
                string.Empty,
                4,
                4,
                0,
                8,
                "SUCCESS_WITH_COST",
                "COMMITTED081_FATE_ROUTE_SOURCE");
            var stagedExpedition = source.With(
                currentNodeId: "N10",
                status: ExpeditionStatus017D.Active,
                supplies: 5,
                fatigue: 2,
                urgency: 7,
                visitedNodeIds: new[] { "N00", "N10" },
                revealedNodeIds: new[] { "N00", "N10", "N11", "N12" },
                committedChecks: new[] { astrolabeCheck },
                objectiveFlags: new[] { "EVENT_RESOLVED_EVENT_BROKEN_ASTROLABE" },
                lastCheckpointId: "fate_route_source_081");
            var stagedCity = campaign.Guild.GuildCity.With(
                expedition: stagedExpedition,
                replaceExpedition: true,
                lastCheckpointId: "fate_route_source_081");
            campaign = campaign.With(
                campaign.Guild.WithGuildCity(stagedCity),
                campaign.OpeningFlow);

            var moved = Require(_expeditions.CommitMove(campaign, _content, "N12"));
            var movedExpedition = moved.Guild.GuildCity.Expedition;
            var fateFlag = GuildCityExpeditionService017D.BoardRoomRewardFlag081(
                "N12",
                "FATE");
            Assert.That(movedExpedition.CurrentNodeId, Is.EqualTo("N12"));
            Assert.That(movedExpedition.ObjectiveFlags, Contains.Item(fateFlag));
            Assert.That(movedExpedition.ObjectiveFlags.Where(value =>
                    value.StartsWith("ROOM_REVEALED081_N12_", StringComparison.Ordinal)),
                Is.EqualTo(new[] { fateFlag }));
        }

        [Test]
        public void StoryClimaxRoomStillCommitsItsExactAuthoredEncounter081()
        {
            var state = BranchChoicePresentationState081();
            state.Expedition.CurrentNodeId = "N13";
            state.Expedition.CurrentNodeKind = "MAIN_OBJECTIVE";
            state.Expedition.CurrentEncounterId =
                GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071;
            state.Expedition.CanCommitEncounter = true;
            state.Expedition.CanMove = false;
            state.Expedition.LinkedNodeIds = new[] { "N14" };
            state.Expedition.ObjectiveFlags = new[]
            {
                GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071
            };
            var view = ExpeditionBoardProjection074.Build(state);
            var current = view.Nodes.Single(value => value.IsCurrent);

            Assert.That(BoardQuestRules081.RoomKind081(state, current), Is.EqualTo("STORY"));
            Assert.That(view.ActionKind, Is.EqualTo(ExpeditionBoardActionKind074.CommitEncounter));
            Assert.That(state.Expedition.CurrentEncounterId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071));
            Assert.That(ExpeditionBoardProjection074.StoryBattleName074(state),
                Is.EqualTo("GATE-EATER"));
        }

        [TestCase("TREASURE", 5, 6, 9, 0, 1, 0, 0)]
        [TestCase("SKILL", 4, 6, 9, 4, 0, 1, 0)]
        [TestCase("BLESSING", 5, 4, 9, 0, 0, 0, 1)]
        [TestCase("DISCOVERY", 4, 6, 8, 0, 0, 0, 0)]
        public void EnteringARevealedRoomCommitsItsChestSkillOrBuffReward081(
            string roomKind,
            int expectedSupplies,
            int expectedFatigue,
            int expectedPressure,
            int expectedGuildXp,
            int expectedTimber,
            int expectedSkillBonus,
            int expectedBlessingBonus)
        {
            var staged = CreateLegacyForkRoom081(roomKind);
            var destination = _content
                .Board(GuildCityExpeditionService017D.LegacyFirstRescueBoardId069)
                .Node("N01");
            Assert.That(GuildCityExpeditionService017D.BoardRoomKind081(
                    staged.Guild.GuildCity.Expedition.ExpeditionId,
                    destination.Id,
                    destination.Kind),
                Is.EqualTo(roomKind));
            var timberBefore = staged.Guild.GuildCity.Materials
                .Where(value => StringComparer.Ordinal.Equals(
                    value.MaterialId,
                    "MAT_SALVAGED_TIMBER"))
                .Sum(value => value.Amount);

            var moved = Require(_expeditions.CommitMove(staged, _content, "N01"));
            var expedition = moved.Guild.GuildCity.Expedition;
            var rewardFlag = GuildCityExpeditionService017D.BoardRoomRewardFlag081(
                "N01",
                roomKind);
            var moduleId001 = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                staged.Guild.GuildCity.Expedition.ExpeditionId,
                "N01",
                roomKind);
            var moduleFlag001 = BoardTowerEnhancementRules001.SelectedRoomFlag001(
                "N01",
                moduleId001);
            var timber = moved.Guild.GuildCity.Materials
                .Where(value => StringComparer.Ordinal.Equals(
                    value.MaterialId,
                    "MAT_SALVAGED_TIMBER"))
                .Sum(value => value.Amount);

            Assert.That(expedition.Supplies, Is.EqualTo(expectedSupplies));
            Assert.That(expedition.Fatigue, Is.EqualTo(expectedFatigue));
            Assert.That(expedition.Urgency, Is.EqualTo(expectedPressure));
            Assert.That(moved.Guild.TreasuryXp, Is.EqualTo(expectedGuildXp));
            Assert.That(timber - timberBefore, Is.EqualTo(expectedTimber));
            Assert.That(expedition.ObjectiveFlags, Contains.Item(rewardFlag));
            Assert.That(expedition.ObjectiveFlags, Contains.Item(moduleFlag001),
                "The enhancement room identity must commit in the same atomic move as its reveal.");
            Assert.That(expedition.ObjectiveFlags.Count(value =>
                    value.StartsWith(
                        BoardTowerEnhancementRules001.SelectedRoomFlagPrefix001,
                        StringComparison.Ordinal)),
                Is.EqualTo(1));
            Assert.That(GuildCityExpeditionService017D.BoardRoomSkillBonus081(
                    expedition.ObjectiveFlags),
                Is.EqualTo(expectedSkillBonus));
            Assert.That(GuildCityExpeditionService017D.BoardRoomBlessingBonus081(
                    expedition.ObjectiveFlags),
                Is.EqualTo(expectedBlessingBonus));
            Assert.That(BoardQuestRules081.RoomReward081(roomKind), Is.Not.Empty);
            if (StringComparer.Ordinal.Equals(roomKind, "DISCOVERY"))
                Assert.That(BoardQuestRules081.RoomReward081(roomKind),
                    Does.Contain("-1 PRESSURE"));
        }

        [TestCase("TREASURE", 5, 6, 9, 0, 1, 0, 0)]
        [TestCase("SKILL", 4, 6, 9, 4, 0, 1, 0)]
        [TestCase("BLESSING", 5, 4, 9, 0, 0, 0, 1)]
        [TestCase("DISCOVERY", 4, 6, 8, 0, 0, 0, 0)]
        public void ReliefRoadUsesTheSameAuthoritativeRoomRewards081(
            string roomKind,
            int expectedSupplies,
            int expectedFatigue,
            int expectedPressure,
            int expectedGuildXp,
            int expectedTimber,
            int expectedSkillBonus,
            int expectedBlessingBonus)
        {
            var staged = CreateReliefForkRoom081(roomKind);
            var timberBefore = staged.Guild.GuildCity.Materials
                .Where(value => StringComparer.Ordinal.Equals(
                    value.MaterialId,
                    "MAT_SALVAGED_TIMBER"))
                .Sum(value => value.Amount);

            var moved = Require(_expeditions.CommitMove(staged, _content, "N01"));
            var expedition = moved.Guild.GuildCity.Expedition;
            var rewardFlag = GuildCityExpeditionService017D.BoardRoomRewardFlag081(
                "N01",
                roomKind);
            var moduleId001 = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                staged.Guild.GuildCity.Expedition.ExpeditionId,
                "N01",
                roomKind);
            var moduleFlag001 = BoardTowerEnhancementRules001.SelectedRoomFlag001(
                "N01",
                moduleId001);
            var timber = moved.Guild.GuildCity.Materials
                .Where(value => StringComparer.Ordinal.Equals(
                    value.MaterialId,
                    "MAT_SALVAGED_TIMBER"))
                .Sum(value => value.Amount);

            Assert.That(expedition.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.ReliefRoadBoardId081));
            Assert.That(expedition.Supplies, Is.EqualTo(expectedSupplies));
            Assert.That(expedition.Fatigue, Is.EqualTo(expectedFatigue));
            Assert.That(expedition.Urgency, Is.EqualTo(expectedPressure));
            Assert.That(moved.Guild.TreasuryXp, Is.EqualTo(expectedGuildXp));
            Assert.That(timber - timberBefore, Is.EqualTo(expectedTimber));
            Assert.That(expedition.ObjectiveFlags, Contains.Item(rewardFlag));
            Assert.That(expedition.ObjectiveFlags, Contains.Item(moduleFlag001));
            Assert.That(GuildCityExpeditionService017D.BoardRoomSkillBonus081(
                    expedition.ObjectiveFlags),
                Is.EqualTo(expectedSkillBonus));
            Assert.That(GuildCityExpeditionService017D.BoardRoomBlessingBonus081(
                    expedition.ObjectiveFlags),
                Is.EqualTo(expectedBlessingBonus));
        }

        [Test]
        public void EventBackedCampKeepsCampfireRestIdentityExactlyOnce081()
        {
            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign081(),
                _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            var source = campaign.Guild.GuildCity.Expedition;
            var board = _content.Board(source.BoardId);
            var camp = board.Node("N07");
            var campfireFlag = GuildCityExpeditionService017D.BoardRoomRewardFlag081(
                "N07",
                "CAMPFIRE");
            var stagedExpedition = source.With(
                currentNodeId: "N06",
                status: ExpeditionStatus017D.Active,
                supplies: 8,
                fatigue: 5,
                urgency: 10,
                visitedNodeIds: new[] { "N00", "N01", "N06" },
                revealedNodeIds: new[] { "N00", "N01", "N06", "N07" },
                objectiveFlags: new[]
                {
                    GuildCityExpeditionService017D.EncounterClearedFlag("N06")
                },
                lastCheckpointId: "event_camp_rest_081_stage");
            var stagedCity = campaign.Guild.GuildCity.With(
                expedition: stagedExpedition,
                replaceExpedition: true,
                lastCheckpointId: "event_camp_rest_081_stage");
            var staged = campaign.With(
                campaign.Guild.WithGuildCity(stagedCity),
                campaign.OpeningFlow);

            Assert.That(camp.Kind, Is.EqualTo("CAMP"));
            Assert.That(camp.EventId, Is.Not.Empty);
            Assert.That(GuildCityExpeditionService017D.BoardRoomKind081(
                    stagedExpedition.ExpeditionId,
                    camp.Id,
                    camp.Kind),
                Is.EqualTo("CAMPFIRE"));
            Assert.That(BoardQuestRules081.RoomReward081("CAMPFIRE"),
                Does.Contain("-3 FATIGUE"));

            var moved = Require(_expeditions.CommitMove(staged, _content, "N07"));
            Assert.That(moved.Guild.GuildCity.Expedition.Fatigue, Is.EqualTo(2));
            Assert.That(moved.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item(campfireFlag));
            Assert.That(moved.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Not.Contain(
                    GuildCityExpeditionService017D.BoardRoomRewardFlag081("N07", "FATE")));

            var replayed = Require(_expeditions.CommitMove(staged, _content, "N07"));
            Assert.That(CanonicalJson.Sha256Hex(replayed),
                Is.EqualTo(CanonicalJson.Sha256Hex(moved)));
            var retryAfterSave = _expeditions.CommitMove(moved, _content, "N07");
            Assert.That(retryAfterSave.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(moved),
                Is.EqualTo(CanonicalJson.Sha256Hex(replayed)));
        }

        [Test]
        public void QuestSkillAndBlessingBonusesStackOnlyToTheirVisibleCaps081()
        {
            var flags = new[]
            {
                "ROOM_REVEALED081_N01_SKILL",
                "ROOM_REVEALED081_N02_TREASURE",
                "ROOM_REVEALED081_N03_SKILL",
                "ROOM_REVEALED081_N04_BLESSING",
                "ROOM_REVEALED081_N05_SKILL",
                "ROOM_REVEALED081_N06_SKILL",
                "ROOM_REVEALED081_N07_BLESSING",
                "ROOM_REVEALED081_N08_BLESSING"
            };

            Assert.That(GuildCityExpeditionService017D.BoardRoomSkillBonus081(flags),
                Is.EqualTo(3));
            Assert.That(GuildCityExpeditionService017D.BoardRoomSkillBonus081(
                    new[] { "ROOM_REVEALED081_N02_TREASURE" }),
                Is.Zero);
            Assert.That(GuildCityExpeditionService017D.BoardRoomBlessingBonus081(flags),
                Is.EqualTo(3));
            Assert.That(GuildCityExpeditionService017D.BoardRoomBlessingBonus081(
                    new[] { "ROOM_REVEALED081_N05_SKILL" }),
                Is.Zero);
        }

        [Test]
        public void SavedRoomMoveCannotGrantItsRewardTwice081()
        {
            foreach (var roomKind in new[] { "TREASURE", "SKILL", "BLESSING", "DISCOVERY" })
            foreach (var staged in new[]
            {
                CreateLegacyForkRoom081(roomKind),
                CreateReliefForkRoom081(roomKind)
            })
            {
                var committed = Require(_expeditions.CommitMove(staged, _content, "N01"));
                var deterministicReplay = Require(
                    _expeditions.CommitMove(staged, _content, "N01"));
                var committedHash = CanonicalJson.Sha256Hex(committed);
                var rewardFlag = GuildCityExpeditionService017D.BoardRoomRewardFlag081(
                    "N01",
                    roomKind);

                Assert.That(CanonicalJson.Sha256Hex(deterministicReplay),
                    Is.EqualTo(committedHash));
                Assert.That(committed.Guild.GuildCity.Expedition.CommittedMoveIds,
                    Has.Count.EqualTo(1));
                Assert.That(committed.Guild.GuildCity.Expedition.ObjectiveFlags.Count(value =>
                        StringComparer.Ordinal.Equals(value, rewardFlag)),
                    Is.EqualTo(1));

                var retryAfterSave = _expeditions.CommitMove(committed, _content, "N01");
                Assert.That(retryAfterSave.IsSuccess, Is.False,
                    "A saved pawn cannot enter and loot its current room again.");
                Assert.That(CanonicalJson.Sha256Hex(committed), Is.EqualTo(committedHash),
                    "A rejected retry must not mutate the saved reward state.");
            }
        }

        [Test]
        public void FaceDownForkUsesOneAutomaticMoveAndSealedPreview081()
        {
            var presenterObject = new GameObject("Board Quest Presenter Test 081");
            var panelObject = new GameObject(
                "Board Quest Face Down Test Panel 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var state = BranchChoicePresentationState081();
                var view = ExpeditionBoardProjection074.Build(state);
                var builder = typeof(M1FlowPresenter).GetMethod(
                    "BuildBoardQuestMoveChoices081",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(builder, Is.Not.Null);
                builder.Invoke(presenter, new object[]
                {
                    panelObject.transform,
                    null,
                    state,
                    view
                });

                var labels = panelObject.GetComponentsInChildren<Button>(true)
                    .Select(value => value.GetComponentInChildren<Text>(true)?.text)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
                Assert.That(labels, Is.EqualTo(new[]
                {
                    "MOVE FORWARD\nFLIP NEXT ROOM"
                }), "The phone-simple board must offer one obvious move, not a route graph.");

                var destination = BoardQuestRules081.AutomaticDestination081(state, view);
                Assert.That(destination, Is.Not.Null);
                var preview = panelObject.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name ==
                                     "Board Adventure Next Face Down Card Preview 086");
                Assert.That(preview.GetComponentsInChildren<Image>(true).Any(value =>
                        value.name == "Board Adventure Next Face Down Physical Card 086"),
                    Is.True, "The one move must be introduced by a physical sealed card.");
                Assert.That(VisibleText081(
                        preview,
                        "Board Adventure Next Card Seal 086"),
                    Is.EqualTo("◆\nSEALED"));
                Assert.That(VisibleText081(
                        preview,
                        "Board Adventure Next Card Preview Copy 086"),
                    Is.EqualTo(
                        "ROOM " + ExpeditionBoardProjection074.StepNumberForNode074(
                            destination.NodeId) +
                        "\nFACE DOWN\n\nTAP ONCE TO MOVE + FLIP"));

                var destinations = view.Nodes.Where(value =>
                        state.Expedition.LinkedNodeIds.Contains(value.NodeId))
                    .ToArray();
                foreach (var label in labels)
                foreach (var branchDestination in destinations)
                {
                    Assert.That(label, Does.Not.Contain(branchDestination.DisplayName).IgnoreCase);
                    Assert.That(label, Does.Not.Contain(branchDestination.NodeId).IgnoreCase);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(panelObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void FixedScreenBoardQuestCopyKeepsItsCompactLayoutContract081()
        {
            var presenterObject = new GameObject("Compact Board Quest Presenter 081");
            var root = new GameObject("Compact Board Quest Root 081", typeof(RectTransform));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 800f);
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var state = ChapterTwoEventPresentationState081(false);
                var view = ExpeditionBoardProjection074.Build(state);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestActions081",
                    presenter,
                    root.transform,
                    null,
                    state,
                    view);

                var rendered = root.GetComponentsInChildren<Text>(true);
                Assert.That(rendered, Is.Not.Empty);
                var decorativeCorners = rendered.Where(value =>
                        value.gameObject.name.StartsWith(
                            "Premium Ink Corner ",
                            StringComparison.Ordinal))
                    .ToArray();
                Assert.That(decorativeCorners, Has.Length.EqualTo(2));
                Assert.That(decorativeCorners.All(value =>
                        !value.raycastTarget && value.text == "⌁"),
                    Is.True,
                    "Decorative ink glyphs must remain inert and are not player copy.");
                var nonCompact = rendered.Except(decorativeCorners).Where(value =>
                        !value.gameObject.name.Contains("[Authored Compact 076]"))
                    .Select(value => value.gameObject.name)
                    .ToArray();
                Assert.That(nonCompact, Is.Empty,
                    "The fixed 1280×800 board must bypass the page-wide 32 px floor. " +
                    "Unmarked copy: " + string.Join(", ", nonCompact));
                var prompt = rendered.Single(value => value.gameObject.name.StartsWith(
                    "Board Quest Plain Prompt Copy 081",
                    StringComparison.Ordinal));
                Assert.That(prompt.resizeTextMinSize, Is.LessThanOrEqualTo(15));
                Assert.That(prompt.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void ChapterTwoRollCommitsVisibleDiceAndGuildXpExactlyOnce081()
        {
            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign081(),
                _content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            var expedition = campaign.Guild.GuildCity.Expedition;
            var board = _content.Board(expedition.BoardId);
            var currentEvent = _content.Event(board.Node(expedition.CurrentNodeId).EventId);

            Assert.That(expedition.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.SecondStoryBoardId076));
            Assert.That(GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                expedition.BoardId,
                currentEvent), Is.True);

            var xpBefore = campaign.Guild.TreasuryXp;
            var resolved = Require(_expeditions.ResolveCommittedCheck(
                campaign,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                string.Empty,
                GuildCityExpeditionService017D.SwiftApproachModifier076));
            var check = resolved.Guild.GuildCity.Expedition.CommittedChecks.Single();
            var expectedReward = BoardQuestRules081.GuildXpForOutcome081(check.Outcome);

            Assert.That(check.DieOne, Is.InRange(1, 6));
            Assert.That(check.DieTwo, Is.InRange(1, 6));
            Assert.That(check.Total,
                Is.EqualTo(check.DieOne + check.DieTwo + check.Modifier));
            Assert.That(check.CanonicalSeedIdentity,
                Does.Not.StartWith("AUTHORED079_"));
            Assert.That(resolved.Guild.TreasuryXp,
                Is.EqualTo(xpBefore + expectedReward));

            var committedHash = CanonicalJson.Sha256Hex(resolved);
            var replayed = Require(_expeditions.ResolveCommittedCheck(
                resolved,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                string.Empty,
                99));

            Assert.That(replayed.Guild.TreasuryXp, Is.EqualTo(resolved.Guild.TreasuryXp),
                "Reloading or pressing the resolved action again must not award Guild XP twice.");
            Assert.That(replayed.Guild.GuildCity.Expedition.CommittedChecks, Has.Count.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(replayed), Is.EqualTo(committedHash),
                "A committed board roll must replay as the exact same saved campaign.");
        }

        [Test]
        public void ReliefRoadRollCommitsGuildXpExactlyOnce081()
        {
            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign081(),
                _content,
                ReliefRoadContractId081));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            campaign = Require(_expeditions.CommitMove(campaign, _content, "N01"));
            campaign = Require(_expeditions.CommitMove(campaign, _content, "N04"));
            var beforeCheckXp = campaign.Guild.TreasuryXp;

            Assert.That(campaign.Guild.GuildCity.Expedition.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.ReliefRoadBoardId081));
            var resolved = Require(_expeditions.ResolveCommittedCheck(
                campaign,
                _content,
                "EVENT_BLOCKED_CULVERT",
                "R1",
                string.Empty,
                GuildCityExpeditionService017D.SwiftApproachModifier076));
            var check = resolved.Guild.GuildCity.Expedition.CommittedChecks.Single();
            var expectedReward = GuildCityExpeditionService017D
                .BoardGameCheckGuildXp081(check.Outcome);

            Assert.That(check.DieOne, Is.InRange(1, 6));
            Assert.That(check.DieTwo, Is.InRange(1, 6));
            Assert.That(resolved.Guild.TreasuryXp,
                Is.EqualTo(beforeCheckXp + expectedReward));

            var committedHash = CanonicalJson.Sha256Hex(resolved);
            var replayed = Require(_expeditions.ResolveCommittedCheck(
                resolved,
                _content,
                "EVENT_BLOCKED_CULVERT",
                "R1",
                string.Empty,
                99));
            Assert.That(replayed.Guild.TreasuryXp, Is.EqualTo(resolved.Guild.TreasuryXp));
            Assert.That(replayed.Guild.GuildCity.Expedition.CommittedChecks,
                Has.Count.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(replayed), Is.EqualTo(committedHash));
        }

        [Test]
        public void SuccessWithCostChargesOneSupplyAndOneFatigueExactlyOnce081()
        {
            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign081(),
                _content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            var before = campaign.Guild.GuildCity.Expedition;
            var probe = Require(_expeditions.ResolveCommittedCheck(
                campaign,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                string.Empty,
                0));
            var probeCheck = probe.Guild.GuildCity.Expedition.CommittedChecks.Single();
            var modifierForSeven = 7 - probeCheck.DieOne - probeCheck.DieTwo;

            var resolved = Require(_expeditions.ResolveCommittedCheck(
                campaign,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                string.Empty,
                modifierForSeven));
            var resolvedExpedition = resolved.Guild.GuildCity.Expedition;
            var resolvedCheck = resolvedExpedition.CommittedChecks.Single();

            Assert.That(resolvedCheck.DieOne, Is.EqualTo(probeCheck.DieOne));
            Assert.That(resolvedCheck.DieTwo, Is.EqualTo(probeCheck.DieTwo));
            Assert.That(resolvedCheck.Total, Is.EqualTo(7));
            Assert.That(resolvedCheck.Outcome, Is.EqualTo("SUCCESS_WITH_COST"));
            Assert.That(resolvedExpedition.Supplies, Is.EqualTo(before.Supplies - 1));
            Assert.That(resolvedExpedition.Fatigue, Is.EqualTo(before.Fatigue + 1));
            Assert.That(resolvedExpedition.Urgency, Is.EqualTo(before.Urgency));
            Assert.That(resolved.Guild.TreasuryXp,
                Is.EqualTo(campaign.Guild.TreasuryXp + 5));

            var committedHash = CanonicalJson.Sha256Hex(resolved);
            var replayed = Require(_expeditions.ResolveCommittedCheck(
                resolved,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                string.Empty,
                99));
            Assert.That(CanonicalJson.Sha256Hex(replayed), Is.EqualTo(committedHash));
            Assert.That(replayed.Guild.GuildCity.Expedition.Supplies,
                Is.EqualTo(resolvedExpedition.Supplies));
            Assert.That(replayed.Guild.GuildCity.Expedition.Fatigue,
                Is.EqualTo(resolvedExpedition.Fatigue));
            Assert.That(replayed.Guild.TreasuryXp, Is.EqualTo(resolved.Guild.TreasuryXp));
        }

        [Test]
        public void FailedQuestBoardOnlyOffersSafeRegroupAndNeverPromisesCompletionReward081()
        {
            var presenterObject = new GameObject("Failed Board Quest Presenter Test 081");
            var rootObject = new GameObject(
                "Failed Board Quest Root Test 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var state = FailedQuestPresentationState081();
                var view = ExpeditionBoardProjection074.Build(state);

                Assert.That(view.ActionKind,
                    Is.EqualTo(ExpeditionBoardActionKind074.FinalizeOperation));
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestHeader081",
                    null,
                    rootObject.transform,
                    state,
                    view);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestScene081",
                    presenter,
                    rootObject.transform,
                    state,
                    view);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestActions081",
                    presenter,
                    rootObject.transform,
                    null,
                    state,
                    view);

                var visibleCopy = VisibleCopy081(rootObject.transform);
                Assert.That(visibleCopy, Does.Contain("QUEST FAILED"));
                Assert.That(visibleCopy, Does.Contain("NO QUEST COMPLETION REWARD"));
                Assert.That(visibleCopy, Does.Contain("BATTLE GROWTH IS KEPT"));
                Assert.That(visibleCopy, Does.Contain("REGROUP & RETRY"));
                Assert.That(visibleCopy, Does.Not.Contain("MISSION COMPLETE").IgnoreCase);
                Assert.That(visibleCopy,
                    Does.Not.Contain("COLLECT THE FINAL GUILD REWARD").IgnoreCase);
                Assert.That(visibleCopy,
                    Does.Not.Contain("COLLECT THE MISSION REWARD").IgnoreCase);
                Assert.That(visibleCopy, Does.Not.Contain("QUEST COMPLETE").IgnoreCase);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void GateEaterReturnCopyTellsTheTruthAfterVictoryAndDefeat081()
        {
            var defeated = FailedQuestPresentationState081();
            var defeatedView = ExpeditionBoardProjection074.Build(defeated);
            var defeatedCurrent = defeatedView.Nodes.Single(value => value.IsCurrent);
            var defeatedCopy = ExpeditionBoardProjection074.CurrentMomentSummary076(
                defeated,
                defeatedCurrent);
            Assert.That(defeatedCopy, Does.Contain("broke the line").IgnoreCase);
            Assert.That(defeatedCopy, Does.Not.Contain("Defeat the Gate-Eater").IgnoreCase);

            var victorious = FailedQuestPresentationState081();
            victorious.Expedition.Status = "Active";
            victorious.Expedition.CanFinalizeOperation = false;
            victorious.Expedition.CanMove = true;
            victorious.Expedition.ObjectiveFlags = new[]
            {
                GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071,
                "ENCOUNTER_CLEARED_N13"
            };
            var victoriousView = ExpeditionBoardProjection074.Build(victorious);
            var victoriousCurrent = victoriousView.Nodes.Single(value => value.IsCurrent);
            var victoriousCopy = ExpeditionBoardProjection074.CurrentMomentSummary076(
                victorious,
                victoriousCurrent);
            Assert.That(victoriousCopy, Does.Contain("Gate-Eater is defeated").IgnoreCase);
            Assert.That(victoriousCopy, Does.Contain("marked road home").IgnoreCase);
            Assert.That(victoriousCopy, Does.Not.Contain("Defeat the Gate-Eater").IgnoreCase);
        }

        [Test]
        public void BoardQuestSceneShowsTheEventProblemThenItsAuthoredOutcome081()
        {
            var presenterObject = new GameObject("Story Board Quest Presenter Test 081");
            var unresolvedRoot = new GameObject(
                "Unresolved Story Board Quest Root Test 081",
                typeof(RectTransform));
            var resolvedRoot = new GameObject(
                "Resolved Story Board Quest Root Test 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var unresolved = ChapterTwoEventPresentationState081(false);
                var unresolvedView = ExpeditionBoardProjection074.Build(unresolved);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestScene081",
                    presenter,
                    unresolvedRoot.transform,
                    unresolved,
                    unresolvedView);
                var unresolvedSummary = VisibleText081(
                    unresolvedRoot.transform,
                    "Expedition Current Position Summary 074");

                Assert.That(unresolvedView.ActionKind,
                    Is.EqualTo(ExpeditionBoardActionKind074.ResolveCheck));
                Assert.That(unresolvedSummary,
                    Is.EqualTo(unresolved.Expedition.CurrentEventProblem));

                var resolved = ChapterTwoEventPresentationState081(true);
                var resolvedView = ExpeditionBoardProjection074.Build(resolved);
                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestScene081",
                    presenter,
                    resolvedRoot.transform,
                    resolved,
                    resolvedView);
                var resolvedSummary = VisibleText081(
                    resolvedRoot.transform,
                    "Expedition Current Position Summary 074");

                Assert.That(resolvedView.ActionKind,
                    Is.EqualTo(ExpeditionBoardActionKind074.ChooseRoute));
                Assert.That(resolvedSummary,
                    Does.StartWith(resolved.Expedition.CurrentEventOutcomeText));
                Assert.That(resolvedSummary, Does.Contain("CLEAN SUCCESS"));
                Assert.That(resolvedSummary, Does.Contain("TOTAL 10"));
                Assert.That(resolvedSummary,
                    Does.Not.Contain(resolved.Expedition.CurrentEventProblem));
                var resolvedCopy = VisibleCopy081(resolvedRoot.transform);
                Assert.That(resolvedCopy.Split(new[] { "+8 GUILD XP" },
                        StringSplitOptions.None).Length - 1,
                    Is.EqualTo(1),
                    "A committed Fate result must announce its exact XP reward once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(resolvedRoot);
                UnityEngine.Object.DestroyImmediate(unresolvedRoot);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void BoardQuestHeaderKeepsSpendableXpAndRoomProgressVisible081()
        {
            var rootObject = new GameObject(
                "Board Quest Resource Header Test 081",
                typeof(RectTransform));
            try
            {
                var state = ChapterTwoEventPresentationState081(false);
                state.TreasuryXp = 37;
                state.Expedition.Supplies = 4;
                state.Expedition.Fatigue = 6;
                var view = ExpeditionBoardProjection074.Build(state);

                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestHeader081",
                    null,
                    rootObject.transform,
                    state,
                    view);

                Assert.That(VisibleText081(
                        rootObject.transform,
                        "Board Quest Spendable XP Resources 081"),
                    Is.EqualTo(
                        "XP TO SPEND  37\nROOM  3 / 15"));
                var visible = VisibleCopy081(rootObject.transform);
                Assert.That(visible, Does.Not.Contain("SUPPLY"));
                Assert.That(visible, Does.Not.Contain("FATIGUE"));
                Assert.That(visible, Does.Not.Contain("PRESSURE"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void BoardQuestActionPanelHidesPositiveSavedCommandStatus081()
        {
            var presenterObject = new GameObject("Saved Board Quest Presenter Test 081");
            var rootObject = new GameObject(
                "Saved Board Quest Action Root Test 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                typeof(M1FlowPresenter).GetField(
                        "_localStatus",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(presenter, "Room flipped and committed.");
                typeof(M1FlowPresenter).GetField(
                        "_localStatusPositive",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(presenter, true);
                var state = BranchChoicePresentationState081();
                var view = ExpeditionBoardProjection074.Build(state);

                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestActions081",
                    presenter,
                    rootObject.transform,
                    null,
                    state,
                    view);

                Assert.That(rootObject.GetComponentsInChildren<Text>(true).Any(value =>
                        value != null && value.gameObject.name.StartsWith(
                            "Board Quest Saved Command Status 081",
                            StringComparison.Ordinal)),
                    Is.False,
                    "Successful move receipts stay out of the phone-simple action panel; " +
                    "only errors need persistent attention copy.");
                Assert.That(VisibleCopy081(rootObject.transform),
                    Does.Not.Contain("Room flipped and committed."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void BoardQuestShowsEffectiveModeModifierButKeepsSubmittedCommandModifier081()
        {
            var presenterObject = new GameObject("Board Quest Modifier Presenter Test 081");
            var rootObject = new GameObject(
                "Board Quest Modifier Root Test 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var state = ChapterTwoEventPresentationState081(false);
                state.CampaignModeId = "Relaxed";
                state.Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "R1",
                        RecruitName = "Alpha",
                        Kind = "Deployed"
                    },
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "R2",
                        RecruitName = "Zed",
                        Kind = "Deployed"
                    }
                };
                state.Expedition.ObjectiveFlags = Array.Empty<string>();
                var crewBuilder = typeof(M1FlowPresenter).GetMethod(
                    "BoardQuestCrewFor081",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(crewBuilder, Is.Not.Null);
                var crew = crewBuilder.Invoke(presenter, new object[] { state });
                Assert.That(crew, Is.Not.Null);
                Assert.That(BoardQuestCrewInt081(crew, "FastModifier"), Is.Zero,
                    "The downstream service adds Relaxed mode authority, so the submitted fast command stays +0.");
                Assert.That(BoardQuestCrewInt081(crew, "FastVisibleModifier"), Is.EqualTo(1));
                Assert.That(BoardQuestCrewInt081(crew, "TeamModifier"), Is.EqualTo(2),
                    "The submitted team command contains only the careful-approach bonus.");
                Assert.That(BoardQuestCrewInt081(crew, "TeamVisibleModifier"), Is.EqualTo(3));

                InvokeBoardQuestBuilder081(
                    "BuildBoardQuestDiceChoices081",
                    presenter,
                    rootObject.transform,
                    null,
                    state);
                var buttonLabels = rootObject.GetComponentsInChildren<Button>(true)
                    .Select(value => value.GetComponentInChildren<Text>(true)?.text)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
                Assert.That(buttonLabels, Is.EqualTo(new[]
                {
                    "ROLL 2D6\nAUTO-RESOLVE"
                }), "The phone-simple check must choose the best legal crew automatically.");
                var visible = VisibleCopy081(rootObject.transform);
                Assert.That(visible, Does.Contain(
                    "The game chose the best useful crew."));
                Assert.That(visible, Does.Contain(
                    "READY  •  ALPHA + ZED  •  BONUS +3"));
                Assert.That(visible, Does.Not.Contain("MOVE FAST"));
                Assert.That(visible, Does.Not.Contain("TEAM UP"));
                Assert.That(visible, Does.Not.Contain("10+ is a clean success"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        private static CampaignState CreateCampaign081()
        {
            var recruits = Enumerable.Range(1, 7)
                .Select(index => new RecruitState("R" + index, 100, 100, 20, 20))
                .ToArray();
            var unions = new[]
            {
                new UnionState(
                    "U1",
                    "First Union",
                    UnionKind.Normal,
                    "R1",
                    new[] { "R1", "R2", "R3" },
                    "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED",
                    30,
                    7000),
                new UnionState(
                    "U2",
                    "Second Union",
                    UnionKind.Normal,
                    "R4",
                    new[] { "R4", "R5", "R6" },
                    "FORMATION_SKIRMISH_LINE",
                    "DOCTRINE_BALANCED",
                    30,
                    7000)
            };
            var guild = new GuildState("GUILD_BOARD_QUEST_081", 0, recruits, unions);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                true,
                "complete");
            var profile = new NewGuildProfileState(
                "Board Quest Tester",
                SecondDimension.Core.GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            return new CampaignState(
                "00000000-0000-0000-0000-000000081081",
                81081,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                profile,
                flow);
        }

        private CampaignState CreateLegacyForkRoom081(string roomKind) =>
            CreateForkRoom081(
                roomKind,
                GuildCityExpeditionService017D.FirstStoryContractId066,
                GuildCityExpeditionService017D.LegacyFirstRescueBoardId069);

        private CampaignState CreateReliefForkRoom081(string roomKind) =>
            CreateForkRoom081(
                roomKind,
                ReliefRoadContractId081,
                GuildCityExpeditionService017D.ReliefRoadBoardId081);

        private CampaignState CreateForkRoom081(
            string roomKind,
            string contractId,
            string boardId)
        {
            var campaign = Require(_expeditions.AcceptContract(
                CreateCampaign081(),
                _content,
                contractId));
            campaign = Require(_expeditions.StartExpedition(campaign, _content));
            var source = campaign.Guild.GuildCity.Expedition;
            var expeditionId = FindExpeditionForRoomKind081(roomKind);
            var replacement = new ExpeditionState017D(
                expeditionId,
                source.ContractCommitId,
                boardId,
                "N00",
                ExpeditionStatus017D.Active,
                5,
                5,
                0,
                10,
                new[] { "N00" },
                new[] { "N00", "N01" },
                Array.Empty<string>(),
                Array.Empty<CommittedCheckState017D>(),
                Array.Empty<string>(),
                "board_room_081_test_stage");
            var city = campaign.Guild.GuildCity.With(
                expedition: replacement,
                replaceExpedition: true,
                lastCheckpointId: "board_room_081_test_stage");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static string FindExpeditionForRoomKind081(string roomKind)
        {
            for (var index = 0; index < 4096; index++)
            {
                var expeditionId = "EXP_ROOM_REWARD_081_" + roomKind + "_" +
                                   index.ToString("0000");
                if (StringComparer.Ordinal.Equals(
                        GuildCityExpeditionService017D.BoardRoomKind081(
                            expeditionId,
                            "N01",
                            "FORK"),
                        roomKind))
                    return expeditionId;
            }
            Assert.Fail("Could not find deterministic " + roomKind + " room fixture.");
            return string.Empty;
        }

        private static GuildCityPresentationState017D BranchChoicePresentationState081()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_FACE_DOWN_BUTTONS_081",
                    BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                    CurrentNodeId = "N10",
                    CurrentNodeKind = "SKILL_CHECK",
                    ResolutionComplete = true,
                    CanMove = true,
                    CanCommitEncounter = false,
                    LinkedNodeIds = new[] { "N11", "N12" },
                    VisitedNodeIds = new[] { "N10" },
                    RevealedNodeIds = new[] { "N10", "N11", "N12" },
                    ObjectiveFlags = Array.Empty<string>()
                }
            };
        }

        private static GuildCityPresentationState017D FailedQuestPresentationState081()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                TreasuryXp = 19,
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_FAILED_BOARD_QUEST_081",
                    BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                    CurrentNodeId = "N13",
                    CurrentNodeKind = "MAIN_OBJECTIVE",
                    CurrentEncounterId =
                        GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071,
                    Status = "Failed",
                    Supplies = 0,
                    Fatigue = 12,
                    Urgency = 2,
                    ResolutionComplete = true,
                    CanMove = false,
                    CanCommitEncounter = false,
                    CanFinalizeOperation = true,
                    LinkedNodeIds = new[] { "N14" },
                    VisitedNodeIds = new[] { "N00", "N01", "N13" },
                    RevealedNodeIds = new[] { "N00", "N01", "N13", "N14" },
                    ObjectiveFlags = new[]
                    {
                        GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071
                    }
                }
            };
        }

        private static GuildCityPresentationState017D ChapterTwoEventPresentationState081(
            bool resolved)
        {
            const string problem =
                "Fresh paint reverses Orra's seventh-return stroke; Sella's brass tag proves which line is true.";
            const string outcome =
                "The Guild proves the forgery and preserves Orra's true brass line.";
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                CampaignModeId = "Standard",
                TreasuryXp = 31,
                Assignments = new[]
                {
                    new GuildCityAssignmentView017D
                    {
                        RecruitId = "R1",
                        RecruitName = "Tala",
                        Kind = "Deployed"
                    }
                },
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_CHAPTER_TWO_STORY_COPY_081",
                    BoardId = GuildCityExpeditionService017D.SecondStoryBoardId076,
                    CurrentNodeId = "N02",
                    CurrentNodeKind = "EVENT",
                    CurrentEventId = "EVENT_WRONG_ROUTE_MARKS",
                    CurrentEventTitle = "Fresh Marks on an Old Wall",
                    CurrentEventProblem = problem,
                    CurrentEventOutcomeText = resolved ? outcome : string.Empty,
                    CurrentEventEligibleSkills = new[] { "Perception", "Lore" },
                    CurrentEventUsesCommitted2d6 = true,
                    Status = "Active",
                    Supplies = 5,
                    Fatigue = 2,
                    Urgency = 7,
                    RequiresResolution = true,
                    ResolutionComplete = resolved,
                    CanMove = resolved,
                    CanCommitEncounter = false,
                    HasCommittedCheckAtCurrentNode = resolved,
                    LastCheckActorRecruitId = resolved ? "R1" : string.Empty,
                    LastCheckAssistantRecruitId = string.Empty,
                    LastCheckDieOne = resolved ? 4 : 0,
                    LastCheckDieTwo = resolved ? 5 : 0,
                    LastCheckModifier = resolved ? 1 : 0,
                    LastCheckTotal = resolved ? 10 : 0,
                    LastCheckOutcome = resolved ? "FULL_SUCCESS" : string.Empty,
                    LinkedNodeIds = new[] { "N03" },
                    VisitedNodeIds = new[] { "N00", "N01", "N02" },
                    RevealedNodeIds = new[] { "N00", "N01", "N02", "N03" },
                    ObjectiveFlags = Array.Empty<string>()
                }
            };
        }

        private static GuildCityPresentationState017D ReliefRoadPresentationState081()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                CampaignModeId = "Standard",
                TreasuryXp = 24,
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_RELIEF_ROAD_STORY_COPY_081",
                    BoardId = GuildCityExpeditionService017D.ReliefRoadBoardId081,
                    CurrentNodeId = "N04",
                    CurrentNodeKind = "EVENT",
                    CurrentEventId = "EVENT_BLOCKED_CULVERT",
                    CurrentEventTitle = "The Blocked Culvert",
                    CurrentEventProblem =
                        "Road debris is flooding the convoy route before nightfall.",
                    Status = "Active",
                    Supplies = 6,
                    Fatigue = 2,
                    Urgency = 8,
                    RequiresResolution = true,
                    ResolutionComplete = false,
                    CanMove = false,
                    CanCommitEncounter = false,
                    CanFinalizeOperation = false,
                    LinkedNodeIds = new[] { "N05" },
                    VisitedNodeIds = new[] { "N00", "N01", "N04" },
                    RevealedNodeIds = new[] { "N00", "N01", "N04", "N05" },
                    ObjectiveFlags = Array.Empty<string>()
                }
            };
        }

        private static void AssertReliefRoadCopy081(
            string copy,
            params string[] requiredPhrases)
        {
            Assert.That(copy, Is.Not.Null.And.Not.Empty);
            foreach (var phrase in requiredPhrases ?? Array.Empty<string>())
                Assert.That(copy, Does.Contain(phrase).IgnoreCase);
            foreach (var firstChapterPhrase in new[]
            {
                "missing patrol",
                "Lantern Patrol",
                "Zorin",
                "Wayglass",
                "Gate-Eater",
                "survey crew",
                "unrecorded door",
                "brass line"
            })
                Assert.That(copy, Does.Not.Contain(firstChapterPhrase).IgnoreCase,
                    "Relief Road copy must stay on its medicine-convoy story.");
        }

        private static void InvokeBoardQuestBuilder081(
            string methodName,
            object instance,
            params object[] arguments)
        {
            var flags = BindingFlags.NonPublic |
                        (instance == null ? BindingFlags.Static : BindingFlags.Instance);
            var method = typeof(M1FlowPresenter).GetMethod(methodName, flags);
            Assert.That(method, Is.Not.Null, methodName + " must remain available to the board UI.");
            method.Invoke(instance, arguments);
        }

        private static string VisibleText081(Transform root, string objectNamePrefix)
        {
            var match = root.GetComponentsInChildren<Text>(true)
                .SingleOrDefault(value => value != null &&
                    value.gameObject.name.StartsWith(
                        objectNamePrefix,
                        StringComparison.Ordinal));
            Assert.That(match, Is.Not.Null,
                objectNamePrefix + " must be visibly rendered by the board UI.");
            return match.text;
        }

        private static string VisibleCopy081(Transform root) => string.Join(
            "\n",
            root.GetComponentsInChildren<Text>(true)
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.text))
                .Select(value => value.text));

        private static int BoardQuestCrewInt081(object crew, string fieldName)
        {
            var field = crew?.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName + " must remain part of the board command projection.");
            return (int)field.GetValue(crew);
        }

        private static CampaignState Require(
            SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
