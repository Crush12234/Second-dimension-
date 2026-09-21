using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>
    /// Focused contracts for the phone-simple quest presentation. These tests do
    /// not exercise combat or legacy expedition screens.
    /// </summary>
    public sealed class BoardQuestPhoneSimple081Tests
    {
        [Test]
        public void AutomaticDestinationIsStableAcrossReversedNodeOrder081()
        {
            var state = BranchingQuestState081();
            var firstOrder = new[]
            {
                Node081("N12"),
                Node081("UNLINKED"),
                Node081("N11")
            };
            var reversedOrder = firstOrder.Reverse().ToArray();

            var first = BoardQuestRules081.AutomaticDestination081(
                state,
                BoardView081(firstOrder));
            var expectedStableShuffle = new[] { "N11", "N12" }
                .OrderBy(nodeId => GuildCityExpeditionService017D.BoardRoomShuffleKey081(
                    state.Expedition.ExpeditionId,
                    state.Expedition.CurrentNodeId,
                    nodeId), StringComparer.Ordinal)
                .ThenBy(nodeId => nodeId, StringComparer.Ordinal)
                .First();
            state.Expedition.LinkedNodeIds = state.Expedition.LinkedNodeIds
                .Reverse()
                .ToArray();
            var reversed = BoardQuestRules081.AutomaticDestination081(
                state,
                BoardView081(reversedOrder));

            Assert.That(first, Is.Not.Null);
            Assert.That(reversed, Is.Not.Null);
            Assert.That(first.NodeId, Is.EqualTo(reversed.NodeId),
                "The automatic room must not change when projections enumerate nodes differently.");
            Assert.That(first.NodeId, Is.EqualTo(expectedStableShuffle),
                "Equal-priority rooms must retain the authoritative deterministic shuffle.");
            Assert.That(new[] { "N11", "N12" }, Does.Contain(first.NodeId));
            Assert.That(first.NodeId, Is.Not.EqualTo("UNLINKED"));
        }

        [Test]
        public void AutomaticDestinationPrefersStoryRoomOverOptionalElite081()
        {
            var state = BranchingQuestState081();
            state.Expedition.ExpeditionId = "EXP_PRIORITY_0";
            state.Expedition.CurrentNodeId = "N08";
            state.Expedition.CurrentNodeKind = "EVENT";
            state.Expedition.LinkedNodeIds = new[] { "N09", "N10" };
            var optionalElite = Node081("N09", "OPTIONAL_ELITE");
            var storyRoom = Node081("N10", "SKILL_CHECK");

            Assert.That(StringComparer.Ordinal.Compare(
                    GuildCityExpeditionService017D.BoardRoomShuffleKey081(
                        state.Expedition.ExpeditionId,
                        state.Expedition.CurrentNodeId,
                        optionalElite.NodeId),
                    GuildCityExpeditionService017D.BoardRoomShuffleKey081(
                        state.Expedition.ExpeditionId,
                        state.Expedition.CurrentNodeId,
                        storyRoom.NodeId)),
                Is.LessThan(0),
                "The fixture must prove priority wins even when shuffle alone picks the elite.");

            var selected = BoardQuestRules081.AutomaticDestination081(
                state,
                BoardView081(new[] { optionalElite, storyRoom }));

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.NodeId, Is.EqualTo("N10"),
                "Phone-simple movement must not force an optional elite detour when story can continue.");

            state.Expedition.LinkedNodeIds = new[] { "N09" };
            var optionalOnly = BoardQuestRules081.AutomaticDestination081(
                state,
                BoardView081(new[] { optionalElite, storyRoom }));
            Assert.That(optionalOnly, Is.Not.Null);
            Assert.That(optionalOnly.NodeId, Is.EqualTo("N09"),
                "Optional rooms remain valid when they are the only authored way forward.");
        }

        [TestCase("N01", "N02", "EVENT", "N04", "SKILL_CHECK",
            TestName = "AutomaticDestination_FirstFork_PrefersStoryEvent081")]
        [TestCase("N06", "N07", "CAMP", "N08", "EVENT",
            TestName = "AutomaticDestination_PostBattle_PrefersStoryCamp081")]
        [TestCase("N10", "N11", "SECONDARY_OBJECTIVE", "N12", "SAFE_ROUTE",
            TestName = "AutomaticDestination_FinalFork_PrefersStoryEvidence081")]
        public void AutomaticDestinationKeepsTheReadableStoryRoute081(
            string currentNodeId,
            string expectedNodeId,
            string expectedKind,
            string detourNodeId,
            string detourKind)
        {
            var state = BranchingQuestState081();
            state.Expedition.ExpeditionId = "EXP_PHONE_STORY_PRIORITY_081";
            state.Expedition.CurrentNodeId = currentNodeId;
            state.Expedition.LinkedNodeIds = new[] { detourNodeId, expectedNodeId };

            var selected = BoardQuestRules081.AutomaticDestination081(
                state,
                BoardView081(new[]
                {
                    Node081(detourNodeId, detourKind),
                    Node081(expectedNodeId, expectedKind)
                }));

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.NodeId, Is.EqualTo(expectedNodeId),
                "One-tap movement must keep mandatory story evidence ahead of a detour.");
        }

        [TestCase(true, 4, 3, true, TestName =
            "BestTeam_StrictlyBetter_IsUsed081")]
        [TestCase(true, 3, 3, false, TestName =
            "BestTeam_Tie_UsesFreeSoloRoll081")]
        [TestCase(true, 2, 3, false, TestName =
            "BestTeam_Worse_UsesFreeSoloRoll081")]
        [TestCase(false, 99, -99, false, TestName =
            "BestTeam_Unavailable_IsNeverUsed081")]
        public void BestTeamPolicyIsStrictAndAvailabilityGated081(
            bool canTeamUp,
            int teamModifier,
            int soloModifier,
            bool expected)
        {
            Assert.That(
                BoardQuestRules081.ShouldUseBestTeam081(
                    canTeamUp,
                    teamModifier,
                    soloModifier),
                Is.EqualTo(expected));
        }

        [Test]
        public void BranchingRoomRendersOneForwardFlipActionAndNoChoiceButtons081()
        {
            var presenterObject = new GameObject("Phone Simple Presenter Test 081");
            var root = new GameObject(
                "Phone Simple Action Root Test 081",
                typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var state = BranchingQuestState081();
                var view = BoardView081(new[] { Node081("N11"), Node081("N12") });
                view.ActionKind = ExpeditionBoardActionKind074.ChooseRoute;

                var builder = typeof(M1FlowPresenter).GetMethod(
                    "BuildBoardQuestActions081",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(builder, Is.Not.Null,
                    "The focused quest action presenter must remain available.");
                builder.Invoke(presenter, new object[]
                {
                    root.transform,
                    null,
                    state,
                    view
                });

                var labels = root.GetComponentsInChildren<Button>(true)
                    .Select(button => button.GetComponentInChildren<Text>(true)?.text ?? string.Empty)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Select(text => text.ToUpperInvariant())
                    .ToArray();
                var forward = labels.Where(text =>
                        text.Contains("MOVE FORWARD") &&
                        text.Contains("FLIP NEXT ROOM"))
                    .ToArray();

                Assert.That(forward, Has.Length.EqualTo(1),
                    "A normal room must offer one obvious forward action.");
                Assert.That(labels.Any(text => text.Contains("TEAM UP")), Is.False);
                Assert.That(labels.Any(text => text.Contains("MOVE FAST")), Is.False);
                Assert.That(labels.Any(text => Regex.IsMatch(
                    text,
                    @"\b(?:LEFT|RIGHT)\b",
                    RegexOptions.CultureInvariant)), Is.False,
                    "Authored graph directions must not become player decisions.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void BoardQuestUsesSavedDiceAndContainsNoChoiceButtonLiterals081()
        {
            var questSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "GuildCity017D",
                "M1FlowPresenter.BoardQuest081.cs"));
            var animationSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "M1FlowPresenter.BoardAdventureAnimation084.cs"));

            Assert.That(questSource, Does.Not.Contain("\"TEAM UP\\n"));
            Assert.That(questSource, Does.Not.Contain("\"MOVE FAST\\n"));
            Assert.That(questSource, Does.Not.Contain("? \"LEFT\" : \"RIGHT\""));
            Assert.That(questSource, Does.Not.Contain("UnityEngine.Random"));
            Assert.That(questSource, Does.Not.Contain("new System.Random"));

            Assert.That(Regex.IsMatch(
                questSource,
                @"BuildAuthoritativeDiceRoll084\s*\([\s\S]{0,500}?" +
                @"LastCheckDieOne[\s\S]{0,180}?LastCheckDieTwo[\s\S]{0,180}?" +
                @"LastCheckModifier[\s\S]{0,180}?LastCheckTotal",
                RegexOptions.CultureInvariant), Is.True,
                "The visible dice must receive the check values already stored by authority.");
            Assert.That(animationSource,
                Does.Contain("SetBoardDieValue084(first, settledOne)"));
            Assert.That(animationSource,
                Does.Contain("SetBoardDieValue084(second, settledTwo)"));
        }

        [Test]
        public void MissionCardRewardPreviewUsesReachablePathsAndAuthorityScale084()
        {
            var policy = new WorldGateRewardPolicy023
            {
                RepeatableConsecutiveRewardBasisPoints = new[] {10000, 5000, 0}
            };
            var runtime = WorldGateRuntimeState023.Default().With(
                repeatableStreakDefinitionId: "BOARD_REPEAT_084",
                repeatableStreakCount: 1);
            var multiplier = BoardAdventureRules084.RewardPermilleForNextRun084(
                "REPEATABLE", "BOARD_REPEAT_084", policy, runtime);
            Assert.That(multiplier, Is.EqualTo(5000));

            var board = new BoardDto023
            {
                startNodeId = "START",
                exitNodeId = "EXIT",
                nodes = new[]
                {
                    new NodeDto023
                    {
                        nodeId = "START",
                        nextNodeIds = new[] {"LEFT", "RIGHT"}
                    },
                    new NodeDto023
                    {
                        nodeId = "LEFT",
                        guildXp = 100,
                        hallXp = 20,
                        materialIds = new[] {"MATERIAL_LEFT"},
                        nextNodeIds = new[] {"EXIT"}
                    },
                    new NodeDto023
                    {
                        nodeId = "RIGHT",
                        guildXp = 200,
                        hallXp = 10,
                        materialIds = new[]
                        {
                            "MATERIAL_A", "MATERIAL_B", "MATERIAL_C", "MATERIAL_D"
                        },
                        nextNodeIds = new[] {"EXIT"}
                    },
                    new NodeDto023
                    {
                        nodeId = "EXIT",
                        guildXp = 50,
                        hallXp = 5,
                        nextNodeIds = Array.Empty<string>()
                    }
                }
            };

            Assert.That(
                AdventureBoardNarrativeProjection084.BoardRewardPreview(
                    board, multiplier),
                Is.EqualTo(
                    "+75–125 GUILD XP  •  +7–12 HALL XP  •  MATERIALS ×1–2"));
        }

        private static GuildCityPresentationState017D BranchingQuestState081() =>
            new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                Expedition = new GuildCityExpeditionView017D
                {
                    ExpeditionId = "EXP_PHONE_SIMPLE_081",
                    BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                    CurrentNodeId = "N10",
                    CurrentNodeKind = "FORK",
                    Status = "Active",
                    ResolutionComplete = true,
                    CanMove = true,
                    LinkedNodeIds = new[] { "N11", "N12" },
                    VisitedNodeIds = new[] { "N10" },
                    RevealedNodeIds = new[] { "N10", "N11", "N12" },
                    ObjectiveFlags = Array.Empty<string>()
                }
            };

        private static ExpeditionBoardView074 BoardView081(
            ExpeditionBoardNodeView074[] nodes) =>
            new ExpeditionBoardView074
            {
                BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                ActionKind = ExpeditionBoardActionKind074.ChooseRoute,
                Nodes = nodes ?? Array.Empty<ExpeditionBoardNodeView074>()
            };

        private static ExpeditionBoardNodeView074 Node081(
            string nodeId,
            string kind = "FORK") =>
            new ExpeditionBoardNodeView074
            {
                NodeId = nodeId,
                DisplayName = "Face-down room",
                Kind = kind,
                IsAvailable = true,
                IsRevealed = false
            };
    }
}
