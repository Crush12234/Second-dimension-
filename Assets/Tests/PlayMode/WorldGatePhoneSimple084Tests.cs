using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class WorldGatePhoneSimple084Tests
    {
        [UnityTearDown]
        public IEnumerator TearDownWorldGatePhoneSimple084()
        {
            foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(value => value != null && value.parent == null &&
                                     value.name.StartsWith(
                                         "World Gate Phone Simple 084",
                                         StringComparison.Ordinal))
                     .ToArray())
                UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WorldGateOffersOneForwardActionAndKeepsRoomFaceDown084()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                var harness = CreateHarness084(resolution, "Forward");
                var state = ActiveState084();
                var coordinator = new Coordinator084(state);
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                Canvas.ForceUpdateCanvases();

                var buttons = harness.Body.GetComponentsInChildren<Button>(true);
                Assert.That(buttons, Has.Length.EqualTo(1));
                Assert.That(buttons[0].name,
                    Is.EqualTo("Move forward World Gate room 084"));
                Assert.That(VisibleText084(harness.Body),
                    Does.Contain("MOVE FORWARD  •  FLIP NEXT ROOM"));
                Assert.That(VisibleText084(harness.Body),
                    Does.Not.Contain("SECRET UNFLIPPED ROOM"));
                Assert.That(VisibleText084(harness.Body),
                    Does.Not.Contain("SECRET ROOM DESCRIPTION"));
                Assert.That(buttons.Any(value => value.name.StartsWith(
                    "Flip World Gate route ", StringComparison.Ordinal)), Is.False);

                buttons[0].onClick.Invoke();
                Assert.That(coordinator.AutomaticCommitCount, Is.EqualTo(1));
                Assert.That(coordinator.LegacyChoiceCommitCount, Is.EqualTo(0));

                UnityEngine.Object.Destroy(harness.Root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator PendingRoomShowsPhysicalFlipAndAuthoritativeDiceWithoutCollectButton084()
        {
            var harness = CreateHarness084(new Vector2(1280f, 800f), "Reveal");
            var state = PendingState084();
            var coordinator = new Coordinator084(state);
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Face Down Room Card Back 084"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "First Authoritative Die 084"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Second Authoritative Die 084"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                        "Board Adventure Revealed Card Type Ribbon 087 FATE"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                        "Board Adventure Resolved Reward Surface 087 FATE"),
                Is.True);
            Assert.That(VisibleText084(harness.Body), Does.Contain("SAVED CHECK  •  TARGET 8"));
            Assert.That(VisibleText084(harness.Body), Does.Contain("+3  =  12"));
            Assert.That(VisibleText084(harness.Body), Does.Contain("+40 GUILD XP"));
            Assert.That(harness.Body.GetComponentsInChildren<Button>(true), Is.Empty,
                "A revealed room applies itself; there is no second collect/apply click.");

            UnityEngine.Object.Destroy(harness.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingReceiptAppliesOnceAfterSavedDiceSettle084()
        {
            var harness = CreateHarness084(new Vector2(1280f, 800f), "ExactOnce");
            typeof(M1FlowPresenter).GetField("_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(harness.Presenter, true);
            var state = PendingState084();
            var coordinator = new Coordinator084(state);
            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            Assert.That(ActivePipCount084(harness.Body, "First Authoritative Die 084"),
                Is.EqualTo(4));
            Assert.That(ActivePipCount084(harness.Body, "Second Authoritative Die 084"),
                Is.EqualTo(5));
            Assert.That(coordinator.ApplyCount, Is.EqualTo(0));

            // The approved reduced-motion reveal holds for0.35s. Measure the
            // actual callback so a slow rendered frame cannot make an early
            // intermediate sample fail after the legitimate callback ran.
            var deadline127 = Time.realtimeSinceStartup + 3f;
            while (coordinator.ApplyCount == 0 && Time.realtimeSinceStartup < deadline127)
                yield return null;
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(coordinator.AppliedAt127 - coordinator.CreatedAt127,
                Is.GreaterThanOrEqualTo(0.35f),
                "The saved dice must be shown before the single reward callback.");
            Assert.That(state.PendingReceiptId, Is.Empty);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));

            UnityEngine.Object.Destroy(harness.Root);
        }

        [UnityTest]
        public IEnumerator BattleAndFinalRoomRemainDeliberateFullStops084()
        {
            var battleHarness = CreateHarness084(
                new Vector2(1280f, 800f), "BattleStop");
            var battleState = ActiveState084();
            battleState.CurrentNode.RequiresBattle = true;
            var battleCoordinator = new Coordinator084(battleState);
            InvokeBuild084(battleHarness.Presenter, battleHarness.Body,
                battleCoordinator, battleState);
            var battleButtons = battleHarness.Body.GetComponentsInChildren<Button>(true);
            Assert.That(battleButtons, Has.Length.EqualTo(1));
            Assert.That(battleButtons[0].name,
                Is.EqualTo("Flip monster battle tile 084"));
            Assert.That(battleButtons.Any(value =>
                value.name == "Move forward World Gate room 084"), Is.False);
            Assert.That(battleCoordinator.AutomaticCommitCount, Is.EqualTo(0));
            UnityEngine.Object.Destroy(battleHarness.Root);
            yield return null;

            var finalHarness = CreateHarness084(
                new Vector2(1280f, 800f), "FinalStop");
            var finalState = ActiveState084();
            finalState.ActiveStatus = "ReadyToFinalize";
            var finalCoordinator = new Coordinator084(finalState);
            InvokeBuild084(finalHarness.Presenter, finalHarness.Body,
                finalCoordinator, finalState);
            var finalButtons = finalHarness.Body.GetComponentsInChildren<Button>(true);
            Assert.That(finalButtons, Has.Length.EqualTo(1));
            Assert.That(finalButtons[0].name,
                Is.EqualTo("Return from completed quest board 084"));
            Assert.That(finalButtons.Any(value =>
                value.name == "Move forward World Gate room 084"), Is.False);
            Assert.That(finalCoordinator.AutomaticCommitCount, Is.EqualTo(0));
            UnityEngine.Object.Destroy(finalHarness.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissionShelfKeepsOneRecommendedCardWithGoalAndReward084()
        {
            foreach (var resolution in new[]
                     {
                         new Vector2(1280f, 800f),
                         new Vector2(1920f, 1080f)
                     })
            {
                var harness = CreateHarness084(resolution, "Shelf");
                var state = new CampaignWorldGatePresentationState023
                {
                    IsAvailable = true,
                    CurrentWorldId = "SKYHOME",
                    CurrentWorldName = "Skyhome",
                    Boards = new[]
                    {
                        new BoardView023
                        {
                            DefinitionId = "QUEST_TECHNICAL_ID_084",
                            WorldId = "SKYHOME",
                            WorldName = "Skyhome",
                            Title = "The Lantern Road",
                            PlayerKind = "STORY QUEST",
                            Objective = "Ring the old bell and guide the caravan home.",
                            RewardPreview = "+90–120 GUILD XP  •  MATERIALS ×1–2",
                            NodeCount = 5,
                            Available = true,
                            UsesBattle = true,
                            IsCurrentStoryBoard = true
                        },
                        new BoardView023
                        {
                            DefinitionId = "HIDDEN_QUEST_TECHNICAL_ID_084",
                            WorldId = "SKYHOME",
                            WorldName = "Skyhome",
                            Title = "Hidden Extra Contract",
                            PlayerKind = "GUILD CONTRACT",
                            Objective = "THIS EXTRA OBJECTIVE STARTS HIDDEN",
                            RewardPreview = "+999 GUILD XP",
                            NodeCount = 4,
                            Available = true
                        }
                    }
                };
                var coordinator = new Coordinator084(state);
                InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
                LayoutRebuilder.ForceRebuildLayoutImmediate(harness.Body);
                Canvas.ForceUpdateCanvases();

                var visible = VisibleText084(harness.Body);
                Assert.That(visible, Does.Contain("GOAL  •  Ring the old bell and guide the caravan home."));
                Assert.That(visible, Does.Contain("SUCCESS REWARD  •  +90–120 GUILD XP"));
                Assert.That(visible, Does.Contain("MATERIALS ×1–2"));
                Assert.That(visible, Does.Not.Contain("QUEST_TECHNICAL_ID_084"));
                Assert.That(visible, Does.Not.Contain("THIS EXTRA OBJECTIVE STARTS HIDDEN"));
                Assert.That(visible, Does.Not.Contain("+999 GUILD XP"));
                Assert.That(harness.Body.GetComponentsInChildren<Button>(true)
                        .Count(value => value.name.StartsWith(
                            "Begin adventure board ", StringComparison.Ordinal)),
                    Is.EqualTo(1), "Only the recommended quest card is expanded by default.");
                var start = harness.Body.GetComponentsInChildren<Button>(true)
                    .Single(value => value.name.StartsWith(
                        "Begin adventure board ", StringComparison.Ordinal));
                Assert.That(start.GetComponentInChildren<Text>().text,
                    Is.EqualTo("START STORY QUEST"));
                Assert.That(start.navigation.mode, Is.Not.EqualTo(Navigation.Mode.None));

                UnityEngine.Object.Destroy(harness.Root);
                yield return null;
            }
        }

        static CampaignWorldGatePresentationState023 ActiveState084() =>
            new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                CurrentWorldId = "SKYHOME",
                CurrentWorldName = "Skyhome",
                ActiveOperationId = "WG_PHONE_SIMPLE_084",
                ActiveBoardTitle = "The Lantern Road",
                ActiveOperationKind = "CHAPTER",
                ActiveStatus = "Active",
                ExpeditionDeckTutorialSeen = true,
                BoardObjective = "Reach the old bell and bring everyone home.",
                Supplies = 4,
                CompletedNodes = 1,
                TotalNodes = 4,
                CurrentNode = new NodeView023
                {
                    NodeId = "ROOM_02",
                    RoomKind = "EVENT",
                    RoomTitle = "EVENT ROOM",
                    Title = "SECRET UNFLIPPED ROOM",
                    Description = "SECRET ROOM DESCRIPTION",
                    StoryFlavor = "SECRET STORY COPY",
                    Objective = "SECRET ROOM OBJECTIVE",
                    RewardPreview = "+40 GUILD XP",
                    ChoiceViews = new[]
                    {
                        new ChoiceView023 {ChoiceId = "LEFT", Label = "Left road"},
                        new ChoiceView023 {ChoiceId = "RIGHT", Label = "Right road"},
                        new ChoiceView023 {ChoiceId = "RISK", Label = "Risky road"}
                    }
                },
                AdventureTiles = new[]
                {
                    new AdventureTrackTileView023
                    {
                        NodeId = "ROOM_01", AuthoredOrder = 0, SpaceNumber = 1,
                        BranchCount = 1,
                        State = AdventureBoardTrackProjection084.ClearedState,
                        RevealedLabel = "Guild Door"
                    },
                    new AdventureTrackTileView023
                    {
                        NodeId = "ROOM_02", AuthoredOrder = 1, SpaceNumber = 2,
                        BranchCount = 1,
                        State = AdventureBoardTrackProjection084.CurrentState,
                        RevealedLabel = "SECRET UNFLIPPED ROOM"
                    },
                    new AdventureTrackTileView023
                    {
                        NodeId = "ROOM_03", AuthoredOrder = 2, SpaceNumber = 3,
                        BranchCount = 1,
                        State = AdventureBoardTrackProjection084.FaceDownState
                    }
                }
            };

        static CampaignWorldGatePresentationState023 PendingState084()
        {
            var state = ActiveState084();
            state.PendingReceiptId = "RECEIPT_PHONE_SIMPLE_084";
            state.PendingOutcomeTitle = "STRONG SUCCESS";
            state.PendingReward = "+40 GUILD XP  •  HERB BUNDLE";
            state.PendingChoiceLabel = "Lantern road";
            state.PendingHasCheck = true;
            state.PendingDieOne = 4;
            state.PendingDieTwo = 5;
            state.PendingModifier = 3;
            state.PendingTotal = 12;
            state.PendingDifficulty = 8;
            return state;
        }

        static Harness084 CreateHarness084(Vector2 resolution, string suffix)
        {
            var root = new GameObject(
                "World Gate Phone Simple 084 " + suffix,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = resolution;

            var presenterObject = new GameObject("Presenter", typeof(RectTransform));
            presenterObject.transform.SetParent(root.transform, false);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();

            var body = new GameObject(
                    "World Gate Phone Simple Body 084",
                    typeof(RectTransform), typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter))
                .GetComponent<RectTransform>();
            body.SetParent(root.transform, false);
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.sizeDelta = new Vector2(0f, resolution.y);
            var layout = body.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            body.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            return new Harness084(root, presenter, body);
        }

        static void InvokeBuild084(
            M1FlowPresenter presenter,
            Transform body,
            ICampaignWorldGatePresentationCoordinator023 coordinator,
            CampaignWorldGatePresentationState023 state)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                "BuildGuildCityWorldGate023",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presenter, new object[] {body, coordinator, state});
        }

        static string VisibleText084(Transform body) => string.Join("\n",
            body.GetComponentsInChildren<Text>(true).Select(value => value.text));

        static int ActivePipCount084(Transform body, string dieName)
        {
            var die = body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == dieName);
            return die.GetComponentsInChildren<Transform>(true).Count(value =>
                value != die && value.name.StartsWith("Die Pip ",
                    StringComparison.Ordinal) && value.gameObject.activeSelf);
        }

        readonly struct Harness084
        {
            public Harness084(GameObject root, M1FlowPresenter presenter,
                RectTransform body)
            {
                Root = root;
                Presenter = presenter;
                Body = body;
            }

            public GameObject Root { get; }
            public M1FlowPresenter Presenter { get; }
            public RectTransform Body { get; }
        }

        sealed class Coordinator084 : ICampaignWorldGatePresentationCoordinator023
        {
            public Coordinator084(CampaignWorldGatePresentationState023 state) =>
                CampaignWorldGate023 = state;

            public CampaignWorldGatePresentationState023 CampaignWorldGate023 { get; }
            public int AutomaticCommitCount { get; private set; }
            public int LegacyChoiceCommitCount { get; private set; }
            public int ApplyCount { get; private set; }
            public float CreatedAt127 { get; } = Time.realtimeSinceStartup;
            public float AppliedAt127 { get; private set; }

            public M1CommandResult BeginWorldGateOperation023(string definitionId) =>
                M1CommandResult.Success();

            public M1CommandResult CommitWorldGateChoice023(string choiceId)
            {
                LegacyChoiceCommitCount++;
                return M1CommandResult.Success();
            }

            public M1CommandResult CommitAutomaticWorldGateRoom023()
            {
                AutomaticCommitCount++;
                return M1CommandResult.Success();
            }

            public M1CommandResult ApplyWorldGateReceipt023()
            {
                ApplyCount++;
                AppliedAt127 = Time.realtimeSinceStartup;
                CampaignWorldGate023.PendingReceiptId = string.Empty;
                return M1CommandResult.Success();
            }

            public M1CommandResult EnterWorldGateBattle023() =>
                M1CommandResult.Success();
            public M1CommandResult FinalizeWorldGateOperation023() =>
                M1CommandResult.Success();
            public M1CommandResult TravelWorldGate023(string worldId) =>
                M1CommandResult.Success();
            public M1CommandResult RecoverLegacyWorldGateQuest023() =>
                M1CommandResult.Success();
        }
    }
}
