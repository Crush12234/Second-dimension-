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
    /// <summary>
    /// Player-facing Tower contract: one clear battle action and no room cards.
    /// Campaign022 remains the exact-once save authority behind the presentation.
    /// </summary>
    public sealed class TowerBattleOnly088Tests
    {
        [UnityTearDown]
        public IEnumerator TearDown088()
        {
            foreach (var root in UnityEngine.Object.FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(value => value != null && value.parent == null &&
                                     value.name.StartsWith("Tower Battle Only 088",
                                         StringComparison.Ordinal)).ToArray())
                UnityEngine.Object.Destroy(root.gameObject);
            yield return null;
        }

        [Test]
        public void LobbyOffersOneBattleAndNoCardRoute088()
        {
            var harness = CreateHarness088("Lobby");
            var state = LobbyState088();
            InvokeBuild088(harness.Presenter, harness.Body,
                new Coordinator088(state), state);

            var buttons = harness.Body.GetComponentsInChildren<Button>(true);
            var copy = VisibleText088(harness.Body);
            Assert.That(buttons, Has.Length.EqualTo(1));
            Assert.That(buttons[0].name, Is.EqualTo("Climb next Tower floor 084"));
            Assert.That(copy, Does.Contain("FIGHT FLOOR 4"));
            Assert.That(copy, Does.Contain("YOUR NEXT UNION BATTLE"));
            Assert.That(copy, Does.Contain("VICTORY REWARD  •  140 GUILD XP"));
            AssertNoCards088(harness.Body, copy);
        }

        [Test]
        public void LegacyBookendOffersPreparationWithoutCards088()
        {
            var harness = CreateHarness088("Prepare");
            var state = ActiveState088();
            InvokeBuild088(harness.Presenter, harness.Body,
                new Coordinator088(state), state);

            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            var copy = VisibleText088(harness.Body);
            Assert.That(button.name, Is.EqualTo("Prepare Tower battle only 088"));
            Assert.That(copy, Does.Contain("PREPARE THE FLOOR BATTLE"));
            AssertNoCards088(harness.Body, copy);
        }

        [Test]
        public void BattleReadyOffersFightWithoutCards088()
        {
            var harness = CreateHarness088("BattleReady");
            var state = ActiveState088();
            state.ActiveStepRequiresBattle = true;
            InvokeBuild088(harness.Presenter, harness.Body,
                new Coordinator088(state), state);

            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            var copy = VisibleText088(harness.Body);
            Assert.That(button.name, Is.EqualTo("Enter Tower battle 081"));
            Assert.That(copy, Does.Contain("FIGHT FLOOR 4"));
            AssertNoCards088(harness.Body, copy);
        }

        [Test]
        public void VictoryOffersBankWithoutRevealCard088()
        {
            var harness = CreateHarness088("Victory");
            var state = ActiveState088();
            state.ActiveAbyssStatus = "AwaitingBattle";
            state.TowerBattleResolved = true;
            state.TowerBattleWon = true;
            state.HasPendingAbyssBattleReceipt = true;
            InvokeBuild088(harness.Presenter, harness.Body,
                new Coordinator088(state), state);

            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            var copy = VisibleText088(harness.Body);
            Assert.That(button.name, Is.EqualTo("Bank Tower battle victory 088"));
            Assert.That(copy, Does.Contain("BANK VICTORY & UNLOCK NEXT FLOOR"));
            AssertNoCards088(harness.Body, copy);
        }

        [Test]
        public void DefeatOffersReturnWithoutCards088()
        {
            var harness = CreateHarness088("Defeat");
            var state = ActiveState088();
            state.ActiveAbyssStatus = "AwaitingBattle";
            state.TowerBattleResolved = true;
            state.TowerBattleWon = false;
            InvokeBuild088(harness.Presenter, harness.Body,
                new Coordinator088(state), state);

            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            var copy = VisibleText088(harness.Body);
            Assert.That(button.name, Is.EqualTo("Retreat from defeated Tower run 081"));
            Assert.That(copy, Does.Contain("RETURN TO GUILD"));
            AssertNoCards088(harness.Body, copy);
        }

        [TestCase("AwaitingBattle"), TestCase("ReadyToFinalize")]
        public void ActualBankButtonUsesOneAtomicBankAndNeverStartsNextFloor110(string status)
        {
            var harness = CreateHarness088("Atomic Bank");
            var state = ActiveState088();
            state.ActiveAbyssStatus = status;
            state.TowerBattleResolved = true;
            state.TowerBattleWon = true;
            var coordinator = new BankCoordinator110(state);
            InvokeBuild088(harness.Presenter, harness.Body, coordinator, state);
            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            Assert.That(button.name, Is.EqualTo(status == "ReadyToFinalize"
                ? "Prepare Tower battle only 088" : "Bank Tower battle victory 088"));
            button.onClick.Invoke();
            Assert.That(coordinator.BankCalls110, Is.EqualTo(1));
            Assert.That(coordinator.StepCalls110, Is.Zero);
            Assert.That(coordinator.BeginCalls110, Is.Zero);
            Assert.That(coordinator.EnterCalls110, Is.Zero);
        }

        [TestCase("idle", "Climb next Tower floor 084")]
        [TestCase("prepared", "Prepare Tower battle only 088")]
        [TestCase("ready", "Enter Tower battle 081")]
        [TestCase("committed", "Enter prepared Tower battle 081")]
        public void ActualManualTowerEntryButtonsUseOneAtomicStartWithoutStepReads110(string checkpoint, string buttonName)
        {
            var harness = CreateHarness088("Atomic Start " + checkpoint);
            var state = checkpoint == "idle" ? LobbyState088() : ActiveState088();
            state.ActiveStepRequiresBattle = checkpoint == "ready";
            if (checkpoint == "committed") state.ActiveAbyssStatus = "AwaitingBattle";
            var coordinator = new StartCoordinator110(state);
            var screen = typeof(M1FlowPresenter).GetField("_screen", BindingFlags.Instance | BindingFlags.NonPublic);
            screen.SetValue(harness.Presenter, M1Screen.GuildOperations);
            coordinator.BeforeReturn110 = () => Assert.That(screen.GetValue(harness.Presenter),
                Is.EqualTo(M1Screen.GuildOperations), "Battle must not become visible before the command reports its saved result.");
            InvokeBuild088(harness.Presenter, harness.Body, coordinator, state);
            var readsBeforeClick = coordinator.ProgressionReads110;
            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            Assert.That(button.name, Is.EqualTo(buttonName));
            button.onClick.Invoke();
            Assert.That(coordinator.StartCalls110, Is.EqualTo(1));
            Assert.That(coordinator.StepCalls110, Is.Zero);
            Assert.That(coordinator.BeginCalls110, Is.Zero);
            Assert.That(coordinator.EnterCalls110, Is.Zero);
            Assert.That(coordinator.ProgressionReads110, Is.EqualTo(readsBeforeClick),
                "The actual click must not re-read full Tower projections between authority commands.");
            Assert.That(screen.GetValue(harness.Presenter), Is.EqualTo(M1Screen.Battle));
        }

        [Test]
        public void FailedAtomicManualTowerEntryKeepsCurrentScreenAndAllowsRetry110()
        {
            var harness = CreateHarness088("Atomic Start Failure");
            var state = LobbyState088();
            var coordinator = new StartCoordinator110(state) { Succeed110 = false };
            var screen = typeof(M1FlowPresenter).GetField("_screen", BindingFlags.Instance | BindingFlags.NonPublic);
            screen.SetValue(harness.Presenter, M1Screen.GuildOperations);
            InvokeBuild088(harness.Presenter, harness.Body, coordinator, state);
            var button = harness.Body.GetComponentsInChildren<Button>(true).Single();
            button.onClick.Invoke();
            Assert.That(screen.GetValue(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(coordinator.StartCalls110, Is.EqualTo(1));
            coordinator.Succeed110 = true;
            button.onClick.Invoke();
            Assert.That(coordinator.StartCalls110, Is.EqualTo(2));
            Assert.That(screen.GetValue(harness.Presenter), Is.EqualTo(M1Screen.Battle));
            Assert.That(coordinator.BeginCalls110 + coordinator.StepCalls110 + coordinator.EnterCalls110, Is.Zero);
        }


        static void AssertNoCards088(Transform body, string copy)
        {
            var names = body.GetComponentsInChildren<Transform>(true)
                .Select(value => value.name).ToArray();
            Assert.That(names.Any(value => value.StartsWith(
                "Tower Track Card ", StringComparison.Ordinal)), Is.False);
            Assert.That(names, Does.Not.Contain("Board Adventure Guild Pawn 086"));
            Assert.That(names, Does.Not.Contain(
                "Board Adventure Next Face Down Physical Card 086"));
            Assert.That(copy, Does.Not.Contain("FACE DOWN"));
            Assert.That(copy, Does.Not.Contain("MOVE PAWN"));
            Assert.That(copy, Does.Not.Contain("FLIP"));
        }

        static CampaignProgressionPresentationState022 LobbyState088() =>
            new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                TowerFloorNumber = 4,
                TowerFloorDisplayName = "The Moon Stair",
                HighestClearedTowerFloor = 3,
                TowerRewardSummary = "140 GUILD XP",
                TowerArtResourcePath = TowerRunRules081.ArtResourcePath(4)
            };

        static CampaignProgressionPresentationState022 ActiveState088() =>
            new CampaignProgressionPresentationState022
            {
                IsAvailable = true,
                ActiveAbyssOperationId = "TOWER_BATTLE_ONLY_088",
                ActiveAbyssStatus = "Active",
                ActiveAbyssFloorId = "FLOOR_04",
                ActiveOperationDisplayName = "The Moon Stair",
                ActiveOperationKind = "GUARDIAN",
                ActiveStepIndex = 1,
                ActiveCompletedStepCount = 1,
                ActiveStepCount = 5,
                ActiveStepKind = "EVENT",
                ActiveStepTitle = "A sealed moon door",
                ActiveStepDescription = "The floor battle is being prepared.",
                TowerFloorNumber = 4,
                TowerFloorDisplayName = "The Moon Stair",
                HighestClearedTowerFloor = 3,
                TowerRewardSummary = "140 GUILD XP",
                TowerRunNumber = 1,
                TotalTowerClears = 3,
                TowerArtResourcePath = TowerRunRules081.ArtResourcePath(4)
            };

        static Harness088 CreateHarness088(string suffix)
        {
            var root = new GameObject("Tower Battle Only 088 " + suffix,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 800f);
            var presenterObject = new GameObject("Presenter", typeof(RectTransform));
            presenterObject.transform.SetParent(root.transform, false);
            var presenter = presenterObject.AddComponent<M1FlowPresenter>();
            return new Harness088(root, presenter,
                AddBody088(root.transform, "Tower Battle Only Body 088"));
        }

        static RectTransform AddBody088(Transform parent, string name)
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

        static void InvokeBuild088(M1FlowPresenter presenter, Transform body,
            ICampaignProgressionPresentationCoordinator022 coordinator,
            CampaignProgressionPresentationState022 state)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                "BuildGuildCityAbyss022",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(presenter, new object[] {body, coordinator, state});
        }

        static string VisibleText088(Transform body) => string.Join("\n",
            body.GetComponentsInChildren<Text>(true).Select(value => value.text));

        readonly struct Harness088
        {
            public Harness088(GameObject root, M1FlowPresenter presenter,
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

        sealed class StartCoordinator110 : Coordinator088, ITowerBattleStartCoordinator110
        {
            public StartCoordinator110(CampaignProgressionPresentationState022 state) : base(state) { }
            public int StartCalls110;
            public bool Succeed110 = true;
            public Action BeforeReturn110;
            public M1CommandResult StartTowerBattle110()
            {
                StartCalls110++;
                BeforeReturn110?.Invoke();
                return Succeed110 ? M1CommandResult.Success() : M1CommandResult.Failure("Synthetic save failure.");
            }
        }

        sealed class BankCoordinator110 : Coordinator088, ITowerVictoryBankCoordinator110
        {
            public BankCoordinator110(CampaignProgressionPresentationState022 state) : base(state) { }
            public int BankCalls110;
            public M1CommandResult BankTowerVictory110()
            {
                BankCalls110++;
                CampaignProgression022.ActiveAbyssOperationId = string.Empty;
                CampaignProgression022.ActiveStepRequiresBattle = false;
                return M1CommandResult.Success();
            }
        }

        class Coordinator088 : ICampaignProgressionPresentationCoordinator022
        {
            public Coordinator088(CampaignProgressionPresentationState022 state) =>
                _state110 = state;
            readonly CampaignProgressionPresentationState022 _state110;
            public int ProgressionReads110;
            public CampaignProgressionPresentationState022 CampaignProgression022
            {
                get { ProgressionReads110++; return _state110; }
            }
            public M1CommandResult RecordEquipmentUse022(string itemInstanceId, string trackId) => M1CommandResult.Success();
            public M1CommandResult EvolveWeapon022(string itemInstanceId, string recipeId) => M1CommandResult.Success();
            public M1CommandResult CertifyAdvancedClass022(string recruitId, string classId) => M1CommandResult.Success();
            public M1CommandResult BeginAbyssOperation022(string operationId) => M1CommandResult.Success();
            public int StepCalls110, BeginCalls110, EnterCalls110;
            public M1CommandResult BeginTowerRun081() { BeginCalls110++; return M1CommandResult.Success(); }
            public M1CommandResult AdvanceTowerRun081() { StepCalls110++; return M1CommandResult.Success(); }
            public M1CommandResult RetreatTowerRun081() => M1CommandResult.Success();
            public M1CommandResult RecoverInactiveLegacyTower084() => M1CommandResult.Success();
            public M1CommandResult EnterAbyssBattle022() { EnterCalls110++; return M1CommandResult.Success(); }
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
