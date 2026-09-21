#if LEGACY_TOWER_CARDS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class TowerPhoneSimple084Tests
    {
        [UnityTearDown]
        public IEnumerator TearDownTowerPhoneSimple084()
        {
            foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(value => value != null && value.parent == null &&
                                     value.name.StartsWith(
                                         "Tower Phone Simple 084",
                                         StringComparison.Ordinal))
                     .ToArray())
                UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
        }

        [Test]
        public void LobbyHasOneRecommendedClimbAndNoRouteCatalog084()
        {
            var harness = CreateHarness084("Lobby");
            var state = LobbyState084();
            var coordinator = new Coordinator084(state);

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            var buttons = harness.Body.GetComponentsInChildren<Button>(true);
            var copy = VisibleText084(harness.Body);
            Assert.That(buttons, Has.Length.EqualTo(1));
            Assert.That(buttons[0].name, Is.EqualTo("Climb next Tower floor 084"));
            Assert.That(copy, Does.Contain("CLIMB NEXT FLOOR"));
            Assert.That(copy, Does.Contain("FLOOR 4"));
            Assert.That(copy, Does.Contain("BEST FLOOR  •  3"));
            Assert.That(copy, Does.Contain("NEXT REWARD  •  140 GUILD XP"));
            Assert.That(copy, Does.Contain("GUILD XP  •"));
            Assert.That(copy, Does.Not.Contain("CHOOSE A TOWER ROUTE"));
            Assert.That(copy, Does.Not.Contain("PLACE PAWN"));
            Assert.That(copy, Does.Not.Contain("TOTAL CLEARS"));
            Assert.That(buttons.Any(value => value.name.StartsWith(
                "Begin authored Tower route ", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        public void NormalRoomHasOneMoveForwardActionAndFiveCards084()
        {
            var harness = CreateHarness084("Move");
            var state = ActiveState084();
            var coordinator = new Coordinator084(state);

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            var buttons = harness.Body.GetComponentsInChildren<Button>(true);
            Assert.That(buttons, Has.Length.EqualTo(1));
            Assert.That(buttons[0].name, Is.EqualTo("Move forward Tower room 084"));
            Assert.That(VisibleText084(harness.Body),
                Does.Contain("MOVE FORWARD / FLIP NEXT ROOM"));
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Count(value => value.name.StartsWith(
                        "Tower Track Card ", StringComparison.Ordinal)),
                Is.EqualTo(5));
            var pawn = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Board Adventure Guild Pawn 086");
            Assert.That(pawn.parent.name, Is.EqualTo("Tower Track Card 1 084"));
            Assert.That(pawn.sizeDelta.y, Is.LessThanOrEqualTo(62f),
                "The compact physical pawn must remain inside the 98px Tower tile at 1280×800.");
            var preview = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name ==
                                 "Board Adventure Next Face Down Card Preview 086");
            Assert.That(preview.GetComponent<LayoutElement>().preferredHeight,
                Is.EqualTo(132f));
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                                  "Board Adventure Next Face Down Physical Card 086"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name.IndexOf(
                        "Dice", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False, "Tower rooms have no saved dice values to present.");

            buttons[0].onClick.Invoke();
            Assert.That(coordinator.CommitCount, Is.EqualTo(1));
            Assert.That(coordinator.ApplyCount, Is.EqualTo(0));
            Assert.That(state.HasPendingAbyssStepReceipt, Is.True);
        }

        [UnityTest]
        public IEnumerator PendingRoomFlipsSavedCardWithoutSecondClick084()
        {
            var harness = CreateHarness084("PhysicalFlip");
            var state = PendingState084(false);
            var coordinator = new Coordinator084(state);

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Face Down Room Card Back 084"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Board Adventure Guild Pawn 086"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                                  "Board Adventure Reward Token 086 TREASURE"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Board Reward Chest Body 086"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Board Reward Chest Lid 086"),
                Is.True);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name.IndexOf(
                        "Dice", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False, "A Tower reveal must not fabricate a dice result.");
            Assert.That(harness.Body.GetComponentsInChildren<Button>(true), Is.Empty,
                "A saved Tower room must not expose a reveal, collect, or apply click.");
            var copy = VisibleText084(harness.Body);
            Assert.That(copy, Does.Contain("STRONG_SUCCESS"));
            Assert.That(copy, Does.Contain("+75 GUILD XP  •  MOONSTONE"));
            Assert.That(copy, Does.Not.Contain("REVEAL ROOM & MOVE PAWN"));
            Assert.That(copy, Does.Not.Contain("FLIP & SAVE"));

            UnityEngine.Object.Destroy(harness.Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PhysicalTowerSequenceMovesThenFlipsThenShowsSavedReward084()
        {
            var harness = CreateHarness084("PhysicalSequence");
            var state = PendingState084(false);
            var coordinator = new Coordinator084(state);

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);

            var stage = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Board Adventure Card Flip Stage 086");
            var rewardSurface = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Tower Resolved Reward Surface 086");
            var rewardGroup = rewardSurface.GetComponent<CanvasGroup>();
            Assert.That(stage.gameObject.activeSelf, Is.True);
            Assert.That(rewardGroup.alpha, Is.Zero,
                "The saved reward must stay hidden while the pawn and physical card move.");
            Assert.That(coordinator.ApplyCount, Is.Zero);

            yield return new WaitForSecondsRealtime(
                M1FlowPresenter.BoardAdventurePawnTravelDuration084 +
                M1FlowPresenter.BoardAdventureCardFlipDuration084 + 0.08f);

            Assert.That(stage.gameObject.activeSelf, Is.False,
                "The face-down card should have turned to its saved face.");
            Assert.That(rewardGroup.alpha, Is.GreaterThan(0f),
                "The reward surface should begin only after pawn travel and card flip.");
            Assert.That(coordinator.ApplyCount, Is.Zero,
                "Presentation must finish before the saved exact-once receipt auto-applies.");

            yield return new WaitForSecondsRealtime(
                M1FlowPresenter.BoardAdventureRewardRevealDuration084 + 0.12f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(state.HasPendingAbyssStepReceipt, Is.False);
        }

        [UnityTest]
        public IEnumerator SavedRoomAppliesExactlyOnceAfterReveal084()
        {
            var harness = CreateHarness084("ExactOnce");
            SetReducedMotion084(harness.Presenter);
            var state = PendingState084(false);
            var coordinator = new Coordinator084(state);

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
            Assert.That(coordinator.ApplyCount, Is.Zero);
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name == "Board Adventure Card Flip Stage 086"),
                Is.False, "Reduced motion should show the saved face immediately.");
            var reducedReward = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Tower Resolved Reward Surface 086");
            Assert.That(reducedReward.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            var reducedPawn = harness.Body.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name == "Board Adventure Guild Pawn 086");
            Assert.That(reducedPawn.localScale, Is.EqualTo(Vector3.one));
            Assert.That(reducedPawn.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));

            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(coordinator.AdvanceCount, Is.EqualTo(1));
            Assert.That(state.HasPendingAbyssStepReceipt, Is.False);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReturnTileBanksWithOneTapAndAutomaticApply084()
        {
            var harness = CreateHarness084("Return");
            SetReducedMotion084(harness.Presenter);
            var state = ActiveState084();
            state.ActiveAbyssStatus = "ReadyToFinalize";
            state.TowerTrackPhase = 5;
            var coordinator = new Coordinator084(state);

            InvokeBuild084(harness.Presenter, harness.Body, coordinator, state);
            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            Assert.That(button.name, Is.EqualTo("Bank Tower rewards and return 084"));
            Assert.That(VisibleText084(harness.Body),
                Does.Contain("BANK REWARDS & RETURN"));
            Assert.That(harness.Body.GetComponentsInChildren<RectTransform>(true)
                    .Any(value => value.name ==
                                  "Board Adventure Next Face Down Physical Card 086"),
                Is.True);
            button.onClick.Invoke();
            Assert.That(coordinator.CommitCount, Is.EqualTo(1));
            Assert.That(coordinator.ApplyCount, Is.Zero);

            var pendingBody = AddBody084(harness.Root.transform, "Return Pending Body 084");
            InvokeBuild084(harness.Presenter, pendingBody, coordinator, state);
            Assert.That(pendingBody.GetComponentsInChildren<Button>(true), Is.Empty);
            Assert.That(VisibleText084(pendingBody),
                Does.Contain("SAVED  •  BANKING REWARDS…"));

            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(coordinator.CommitCount, Is.EqualTo(1));
            Assert.That(coordinator.ApplyCount, Is.EqualTo(1));
            Assert.That(coordinator.AdvanceCount, Is.EqualTo(2));
            Assert.That(state.HasPendingAbyssStepReceipt, Is.False);
        }

        [Test]
        public void BattleRoomsRemainDeliberateFullStops084()
        {
            var activeHarness = CreateHarness084("BattleEntry");
            var activeState = ActiveState084();
            activeState.ActiveStepRequiresBattle = true;
            var activeCoordinator = new Coordinator084(activeState);
            InvokeBuild084(activeHarness.Presenter, activeHarness.Body,
                activeCoordinator, activeState);
            var entry = activeHarness.Body.GetComponentsInChildren<Button>(true).Single();
            Assert.That(entry.name, Is.EqualTo("Enter Tower battle 081"));
            Assert.That(activeCoordinator.AdvanceCount, Is.Zero);

            var receiptHarness = CreateHarness084("BattleReceipt");
            var receiptState = ActiveState084();
            receiptState.ActiveAbyssStatus = "AwaitingBattle";
            receiptState.HasPendingAbyssBattleReceipt = true;
            var receiptCoordinator = new Coordinator084(receiptState);
            InvokeBuild084(receiptHarness.Presenter, receiptHarness.Body,
                receiptCoordinator, receiptState);
            var reveal = receiptHarness.Body.GetComponentsInChildren<Button>(true).Single();
            Assert.That(reveal.name, Is.EqualTo("Reveal Tower battle tile 084"));
            Assert.That(VisibleText084(receiptHarness.Body),
                Does.Contain("REVEAL BATTLE RESULT & MOVE PAWN"));
            Assert.That(receiptCoordinator.AdvanceCount, Is.Zero,
                "Battle receipt application remains a deliberate boundary action.");
        }

        static CampaignProgressionPresentationState022 LobbyState084()
        {
            var operations = Enumerable.Range(1, 30)
                .Select(index => new AbyssOperationView084
                {
                    OperationId = "HIDDEN_ROUTE_" + index,
                    FloorNumber = ((index - 1) / 3) + 1,
                    FloorDisplayName = "Hidden floor",
                    DisplayName = "Hidden Guardian Recon Trial " + index,
                    Kind = index % 3 == 0 ? "TRIAL" : index % 3 == 1 ? "GUARDIAN" : "RECON",
                    PlayerKind = "HIDDEN ROUTE",
                    Available = true,
                    StepCount = 5,
                    RewardSummary = "Hidden route reward"
                }).ToArray();
            return new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                TowerFloorNumber = 4,
                TowerFloorDisplayName = "The Moon Stair",
                HighestClearedTowerFloor = 3,
                TowerRewardSummary = "140 GUILD XP",
                TowerOperationChoices = operations
            };
        }

        static CampaignProgressionPresentationState022 ActiveState084() =>
            new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                ActiveAbyssOperationId = "TOWER_PHONE_SIMPLE_084",
                ActiveAbyssStatus = "Active",
                ActiveAbyssFloorId = "FLOOR_04",
                ActiveOperationDisplayName = "The Moon Stair",
                ActiveOperationKind = "RECON",
                ActiveStepIndex = 1,
                ActiveCompletedStepCount = 1,
                ActiveStepCount = 5,
                ActiveStepKind = "EVENT",
                ActiveStepTitle = "A sealed moon door",
                ActiveStepDescription = "The next room waits face down.",
                TowerFloorNumber = 4,
                TowerFloorDisplayName = "The Moon Stair",
                HighestClearedTowerFloor = 3,
                TowerRewardSummary = "140 GUILD XP",
                TowerRunNumber = 1,
                TotalTowerClears = 3,
                TowerTrackPhase = 1,
                TowerTrackLabels = new[] {"ENTRY", "MOON DOOR"}
            };

        static CampaignProgressionPresentationState022 PendingState084(bool returning)
        {
            var state = ActiveState084();
            state.ActiveAbyssStatus = returning ? "ReadyToFinalize" : "Active";
            state.HasPendingAbyssStepReceipt = true;
            state.PendingAbyssOutcome = returning ? "FLOOR_CLEAR" : "STRONG_SUCCESS";
            state.PendingAbyssReward = returning
                ? "140 GUILD XP  •  FLOOR CHEST"
                : "+75 GUILD XP  •  MOONSTONE";
            state.TowerTrackPhase = returning ? 5 : 1;
            return state;
        }

        static Harness084 CreateHarness084(string suffix)
        {
            var root = new GameObject(
                "Tower Phone Simple 084 " + suffix,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 800f);

            var presenterObject = new GameObject("Presenter", typeof(RectTransform));
            presenterObject.transform.SetParent(root.transform, false);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();
            return new Harness084(root, presenter,
                AddBody084(root.transform, "Tower Phone Simple Body 084"));
        }

        static RectTransform AddBody084(Transform parent, string name)
        {
            var body = new GameObject(name, typeof(RectTransform),
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter))
                .GetComponent<RectTransform>();
            body.SetParent(parent, false);
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.sizeDelta = new Vector2(0f, 800f);
            var layout = body.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            body.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            return body;
        }

        static void InvokeBuild084(
            M1FlowPresenter presenter,
            Transform body,
            ICampaignProgressionPresentationCoordinator022 coordinator,
            CampaignProgressionPresentationState022 state)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                "BuildGuildCityAbyss022",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presenter, new object[] {body, coordinator, state});
        }

        static void SetReducedMotion084(M1FlowPresenter presenter) =>
            typeof(M1FlowPresenter).GetField("_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(presenter, true);

        static string VisibleText084(Transform body) => string.Join("\n",
            body.GetComponentsInChildren<Text>(true).Select(value => value.text));

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

        sealed class Coordinator084 : ICampaignProgressionPresentationCoordinator022
        {
            public Coordinator084(CampaignProgressionPresentationState022 state) =>
                CampaignProgression022 = state;

            public CampaignProgressionPresentationState022 CampaignProgression022 { get; }
            public int AdvanceCount { get; private set; }
            public int CommitCount { get; private set; }
            public int ApplyCount { get; private set; }

            public M1CommandResult AdvanceTowerRun081()
            {
                AdvanceCount++;
                if (CampaignProgression022.HasPendingAbyssStepReceipt)
                {
                    ApplyCount++;
                    CampaignProgression022.HasPendingAbyssStepReceipt = false;
                    CampaignProgression022.PendingAbyssOutcome = string.Empty;
                    CampaignProgression022.PendingAbyssReward = string.Empty;
                    if (CampaignProgression022.ActiveAbyssStatus == "ReadyToFinalize")
                        CampaignProgression022.ActiveAbyssOperationId = string.Empty;
                    else
                    {
                        CampaignProgression022.ActiveStepIndex++;
                        CampaignProgression022.ActiveCompletedStepCount++;
                        CampaignProgression022.TowerTrackPhase++;
                    }
                }
                else
                {
                    CommitCount++;
                    CampaignProgression022.HasPendingAbyssStepReceipt = true;
                    CampaignProgression022.PendingAbyssOutcome =
                        CampaignProgression022.ActiveAbyssStatus == "ReadyToFinalize"
                            ? "FLOOR_CLEAR"
                            : "STRONG_SUCCESS";
                    CampaignProgression022.PendingAbyssReward =
                        CampaignProgression022.ActiveAbyssStatus == "ReadyToFinalize"
                            ? "140 GUILD XP  •  FLOOR CHEST"
                            : "+75 GUILD XP  •  MOONSTONE";
                }
                return M1CommandResult.Success();
            }

            public M1CommandResult RecordEquipmentUse022(string itemInstanceId, string trackId) => M1CommandResult.Success();
            public M1CommandResult EvolveWeapon022(string itemInstanceId, string recipeId) => M1CommandResult.Success();
            public M1CommandResult CertifyAdvancedClass022(string recruitId, string classId) => M1CommandResult.Success();
            public M1CommandResult BeginAbyssOperation022(string operationId) => M1CommandResult.Success();
            public M1CommandResult BeginTowerRun081() => M1CommandResult.Success();
            public M1CommandResult RetreatTowerRun081() => M1CommandResult.Success();
            public M1CommandResult RecoverInactiveLegacyTower084() => M1CommandResult.Success();
            public M1CommandResult EnterAbyssBattle022() => M1CommandResult.Success();
            public M1CommandResult CommitAbyssStep022() => M1CommandResult.Success();
            public M1CommandResult ApplyAbyssStep022() => M1CommandResult.Success();
            public M1CommandResult CommitAbyssBattleResult022() => M1CommandResult.Success();
            public M1CommandResult FinalizeAbyssOperation022() => M1CommandResult.Success();
            public M1CommandResult FinalizeAbyssBattle022() => M1CommandResult.Success();
            public M1CommandResult CraftInvocationArtifact022(string baseId) => M1CommandResult.Success();
            public M1CommandResult CraftInvocationArtifact022(string baseId,
                IReadOnlyList<string> affixIds) => M1CommandResult.Success();
            public M1CommandResult EvolveInvocationArtifact022(string itemInstanceId) => M1CommandResult.Success();
            public M1CommandResult InvokeEligibleEchoForecast022() => M1CommandResult.Success();
            public M1CommandResult InvokeAcceptedCovenantForecast022(string covenantId) => M1CommandResult.Success();
            public M1CommandResult AdvanceCovenant022(string covenantId) => M1CommandResult.Success();
            public M1CommandResult AcceptCovenant022(string covenantId) => M1CommandResult.Success();
        }
    }
}
#endif
