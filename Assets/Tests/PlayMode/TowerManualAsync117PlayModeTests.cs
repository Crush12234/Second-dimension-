using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    // Presenter lifecycle/wiring coverage using a controllable fake authority.
    // Real campaign transactions and durable-save boundaries are tested separately.
    public sealed class TowerManualAsync117PlayModeTests
    {
        readonly List<GameObject> _hosts = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator Cleanup117()
        {
            foreach (var host in _hosts) if (host != null) UnityEngine.Object.Destroy(host);
            _hosts.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualBankButtonDispatchesOnceUpdatesPhaseAndStaysInTower117()
        { yield return ExerciseCompletion117(false); }

        [UnityTest]
        public IEnumerator ActualStartButtonDispatchesOnceUpdatesPhaseThenOpensBattle117()
        { yield return ExerciseCompletion117(true); }

        IEnumerator ExerciseCompletion117(bool startsBattle)
        {
            var owner = new ManualCoordinator117(startsBattle);
            var harness = Harness117(owner);
            yield return Frames117(4);
            var button = Find117(harness.Canvas, startsBattle ? "Climb next Tower floor 084" : "Bank Tower battle victory 088");
            Click117(button, true);
            var pending = owner.PendingTowerManualTransition117;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(owner.BankCalls, Is.EqualTo(startsBattle ? 0 : 1));
            Assert.That(owner.StartCalls, Is.EqualTo(startsBattle ? 1 : 0));
            AssertBusy117(harness.Presenter, true);
            Assert.That(button.IsInteractable(), Is.False);
            Assert.That(Screen117(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations), "Navigation waits for the saved result.");
            var reads = owner.Reads117;
            yield return Frames117(4);
            Assert.That(button.GetComponentInChildren<Text>().text,
                Does.Contain(startsBattle ? "PREPARING BATTLE" : "BANKING REWARDS"));
            Click117(button, false);
            Assert.That(owner.StartCalls + owner.BankCalls, Is.EqualTo(1), "A pending disabled control must not dispatch again.");
            owner.Saving = true;
            yield return Frames117(4);
            Assert.That(button.GetComponentInChildren<Text>().text, Does.Contain("SAVING"));
            Assert.That(owner.Reads117, Is.EqualTo(reads), "Pending frames may read task/phase only, not campaign/view projections.");
            owner.Complete117();
            yield return Frames117(4);
            Assert.That(pending.Result.Succeeded, Is.True);
            AssertBusy117(harness.Presenter, false);
            Assert.That(Screen117(harness.Presenter), Is.EqualTo(startsBattle ? M1Screen.Battle : M1Screen.GuildOperations));
            if (startsBattle)
            {
                Assert.That(Get117<Component>(harness.Presenter, "_battleExperience072"), Is.Not.Null,
                    "Start must actually open the existing battle presentation after the saved result.");
                Assert.That(Get117<RectTransform>(harness.Presenter, "_battleExperienceHost072").gameObject.activeInHierarchy, Is.True);
            }
            else
            {
                Assert.That(Get117<string>(harness.Presenter, "_guildCityTab017D"), Is.EqualTo("ABYSS"));
                Assert.That(Find117(harness.Canvas, "Climb next Tower floor 084").GetComponentInChildren<Text>().text, Does.Contain("FIGHT FLOOR 5"));
            }
            Assert.That(owner.SyncCalls, Is.Zero, "The shipping buttons must not fall back to synchronous or step commands.");
            Assert.That(owner.StartCalls + owner.BankCalls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisablingDuringSavingReleasesLocalFlagsWithoutAbandoningOwnerOrNavigating117()
        {
            var owner = new ManualCoordinator117(true);
            var harness = Harness117(owner);
            yield return Frames117(4);
            Click117(Find117(harness.Canvas, "Climb next Tower floor 084"), true);
            owner.Saving = true;
            var pending = owner.PendingTowerManualTransition117;
            harness.Host.SetActive(false);
            AssertBusy117(harness.Presenter, false);
            Assert.That(owner.CancelCalls, Is.EqualTo(1));
            Assert.That(owner.PendingTowerManualTransition117, Is.SameAs(pending));
            Assert.That(pending.IsCompleted, Is.False, "View detachment cannot abandon a save which already entered its durable phase.");
            owner.Complete117();
            yield return Frames117(4);
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(Screen117(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            harness.Host.SetActive(true);
            yield return Frames117(3);
            Assert.That(Screen117(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations), "The retired waiter must not navigate after reactivation.");
            AssertBusy117(harness.Presenter, false);
        }

        [UnityTest]
        public IEnumerator CoordinatorReplacementCannotReceiveOldCompletionOrStaleNavigation117()
        {
            var oldOwner = new ManualCoordinator117(true);
            var harness = Harness117(oldOwner);
            yield return Frames117(4);
            Click117(Find117(harness.Canvas, "Climb next Tower floor 084"), true);
            oldOwner.Saving = true;
            var pending = oldOwner.PendingTowerManualTransition117;
            var replacement = new ManualCoordinator117(true);
            harness.Presenter.Initialize(replacement);
            AssertBusy117(harness.Presenter, false);
            Assert.That(oldOwner.CancelCalls, Is.EqualTo(1));
            Assert.That(pending.IsCompleted, Is.False);
            yield return Frames117(4);
            var replacementReads = replacement.Reads117;
            oldOwner.Complete117();
            yield return Frames117(4);
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(Get117<IM1PresentationCoordinator>(harness.Presenter, "_coordinator"), Is.SameAs(replacement));
            Assert.That(Screen117(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(replacement.Reads117, Is.EqualTo(replacementReads), "Old Changed/completion must not rebuild the replacement view.");
            Assert.That(replacement.StartCalls + replacement.BankCalls, Is.Zero);
            Assert.That(Get117<string>(harness.Presenter, "_localStatus"), Is.Empty);
            AssertBusy117(harness.Presenter, false);
        }

        [UnityTest]
        public IEnumerator ReattachingPendingBankObservesExistingTaskWithoutRedispatchOrPerFrameProjection117()
        {
            var owner = new ManualCoordinator117(false);
            var harness = Harness117(owner);
            yield return Frames117(4);
            Click117(Find117(harness.Canvas, "Bank Tower battle victory 088"), true);
            owner.Saving = true;
            var pending = owner.PendingTowerManualTransition117;
            harness.Presenter.enabled = false;
            AssertBusy117(harness.Presenter, false);
            harness.Presenter.enabled = true;
            harness.Presenter.Initialize(owner);
            yield return Frames117(4);
            Assert.That(owner.PendingTowerManualTransition117, Is.SameAs(pending));
            Assert.That(owner.BankCalls, Is.EqualTo(1));
            Assert.That(harness.Canvas.GetComponentsInChildren<Text>(false).Any(text => text.text == "SAVING…"), Is.True);
            AssertBusy117(harness.Presenter, true);
            var reads = owner.Reads117;
            yield return Frames117(4);
            Assert.That(owner.Reads117, Is.EqualTo(reads));
            owner.Complete117();
            yield return Frames117(4);
            Assert.That(Screen117(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(Find117(harness.Canvas, "Climb next Tower floor 084").IsInteractable(), Is.True);
            Assert.That(owner.BankCalls, Is.EqualTo(1));
            AssertBusy117(harness.Presenter, false);
        }

        HarnessState117 Harness117(ManualCoordinator117 coordinator)
        {
            var host = new GameObject("Tower Manual Async 117 Test");
            _hosts.Add(host);
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.enabled = false;
            Set117(presenter, "_screen", M1Screen.GuildOperations);
            Set117(presenter, "_guildCityTab017D", "ABYSS");
            presenter.Initialize(coordinator);
            var canvas = Get117<Canvas>(presenter, "_canvas");
            canvas.sortingOrder = 30000;
            presenter.enabled = true;
            return new HarnessState117 { Host = host, Presenter = presenter, Canvas = canvas };
        }

        static IEnumerator Frames117(int count) { for (int i = 0; i < count; i++) yield return null; }
        static FieldInfo Field117(string name) => typeof(M1FlowPresenter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static T Get117<T>(M1FlowPresenter presenter, string name) => (T)Field117(name).GetValue(presenter);
        static void Set117(M1FlowPresenter presenter, string name, object value) => Field117(name).SetValue(presenter, value);
        static M1Screen Screen117(M1FlowPresenter presenter) => Get117<M1Screen>(presenter, "_screen");
        static void AssertBusy117(M1FlowPresenter presenter, bool busy)
        {
            Assert.That(Get117<bool>(presenter, "_towerRoomCommandRunning084"), Is.EqualTo(busy));
            Assert.That(Get117<bool>(presenter, "_suppressBoardAdventureCoordinatorRefresh084"), Is.EqualTo(busy));
            Assert.That(Get117<Coroutine>(presenter, "_towerManualWait117") != null, Is.EqualTo(busy));
        }
        static Button Find117(Canvas canvas, string name) => canvas.GetComponentsInChildren<Button>(true)
            .Last(button => button.name == name && button.gameObject.activeInHierarchy);
        static void Click117(Button button, bool interactable)
        {
            Assert.That(button.IsInteractable(), Is.EqualTo(interactable));
            Canvas.ForceUpdateCanvases();
            var events = EventSystem.current;
            Assert.That(events, Is.Not.Null);
            var canvas = button.GetComponentInParent<Canvas>();
            var rect = (RectTransform)button.transform;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0f, (float)Screen.width));
            Assert.That(point.y, Is.InRange(0f, (float)Screen.height));
            var pointer = new PointerEventData(events) { pointerId = -1, button = PointerEventData.InputButton.Left,
                position = point, pressPosition = point, eligibleForClick = true, clickCount = 1 };
            var hits = new List<RaycastResult>();
            events.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            var hit = hits[0].gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject), "Actual Tower control is intercepted by " + hit.name);
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(pointer.pointerPress, Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        sealed class HarnessState117 { public GameObject Host; public M1FlowPresenter Presenter; public Canvas Canvas; }

        sealed class ManualCoordinator117 : IM2PresentationCoordinator, IM2BattleViewReader098, ICampaignProgressionPresentationCoordinator022,
            IGuildCityPresentationCoordinator017D, ITowerManualAsyncTransition117, ITowerVictoryBankCoordinator110, ITowerBattleStartCoordinator110
        {
            readonly M1PresentationState _state = new M1PresentationState { HasCampaign = true, HasSave = true,
                GuildmasterName = "Async UI fixture", GuildXpRequiredForNextLevel = 100, OpeningUnionsLegal = true };
            readonly GuildCityPresentationState017D _city = new GuildCityPresentationState017D { IsAvailable = true,
                CampaignModeId = "Standard", TutorialDepthId = "Minimal", TextScalePercent = 100, ReducedMotion = true };
            CampaignProgressionPresentationState022 _tower;
            TaskCompletionSource<M1CommandResult> _completion;
            public ManualCoordinator117(bool start) { _tower = Tower117(start); }
            public event Action Changed;
            public int StateReads, CityReads, ProgressionReads, BattleReads, StartCalls, BankCalls, CancelCalls, SyncCalls;
            public int Reads117 => StateReads + CityReads + ProgressionReads + BattleReads;
            public bool Saving;
            public M1PresentationState State { get { StateReads++; return _state; } }
            public M2BattleView ReadBattleView098() { BattleReads++; return _state.Battle; }
            public GuildCityPresentationState017D GuildCity017D { get { CityReads++; return _city; } }
            public CampaignProgressionPresentationState022 CampaignProgression022 { get { ProgressionReads++; return _tower; } }
            public Task<M1CommandResult> PendingTowerManualTransition117 => _completion != null && !_completion.Task.IsCompleted ? _completion.Task : null;
            public bool PendingTowerManualStartsBattle117 { get; private set; }
            public bool TowerManualIsSaving117 => Saving;
            public Task<M1CommandResult> BankTowerVictoryAsync117() { BankCalls++; return Begin117(false); }
            public Task<M1CommandResult> StartTowerBattleAsync117() { StartCalls++; return Begin117(true); }
            Task<M1CommandResult> Begin117(bool start)
            {
                Assert.That(PendingTowerManualTransition117, Is.Null, "Presenter must reuse the pending owner task.");
                PendingTowerManualStartsBattle117 = start;
                _completion = new TaskCompletionSource<M1CommandResult>();
                return _completion.Task;
            }
            public void CancelTowerManualBeforeSave117()
            {
                if (PendingTowerManualTransition117 == null) return;
                CancelCalls++;
                if (!Saving) _completion.TrySetCanceled();
            }
            public void Complete117()
            {
                Assert.That(_completion, Is.Not.Null);
                _tower = Tower117(true); _tower.TowerFloorNumber = 5; _tower.HighestClearedTowerFloor = 4;
                if (PendingTowerManualStartsBattle117)
                    _state.Battle = new M2BattleView {
                        BattleId = "ABYSS_BATTLE022_FLOOR_05_ACTUAL098_5_TEST117", Outcome = "InProgress", Round = 1,
                        Objective = "Presenter navigation fixture",
                        EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY117", DisplayName = "Guardian", Members = new[] {
                            new M2BattleMemberView { MemberId = "FOE117", DisplayName = "Guardian", CurrentHp = 100, MaximumHp = 100 } } } } };
                Changed?.Invoke(); // same publication ordering: Changed precedes owner task completion
                _completion.SetResult(M1CommandResult.Success(PendingTowerManualStartsBattle117 ? "Battle saved." : "Tower victory banked."));
                Saving = false;
            }
            static CampaignProgressionPresentationState022 Tower117(bool start) => new CampaignProgressionPresentationState022 {
                IsAvailable = true, TowerFloorNumber = 4, HighestClearedTowerFloor = 3, TowerFloorDisplayName = "The Moon Stair",
                TowerRewardSummary = "140 GUILD XP", TowerArtResourcePath = TowerRunRules081.ArtResourcePath(4),
                ActiveAbyssOperationId = start ? "" : "TOWER_UI117", ActiveAbyssStatus = start ? "" : "AwaitingBattle",
                TowerBattleResolved = !start, TowerBattleWon = !start, HasPendingAbyssBattleReceipt = !start,
                ActiveOperationDisplayName = "The Moon Stair", ActiveAbyssFloorId = "FLOOR_04"
            };
            M1CommandResult Sync117() { SyncCalls++; throw new NotSupportedException("Unexpected synchronous authority call in async presenter test."); }
            public M1CommandResult StartTutorialBattle() => Sync117();
            public M1CommandResult SelectForecast(string union, string forecast) => Sync117();
            public M1CommandResult ConfirmBattleRound() => Sync117();
            public M1CommandResult ReplayTutorialBattle() => Sync117();
            public M1CommandResult RetryTutorialBattle() => Sync117();
            public M1CommandResult ClaimBattleRewards() => Sync117();
            public M1CommandResult BankTowerVictory110() => Sync117();
            public M1CommandResult StartTowerBattle110() => Sync117();
            public M1CommandResult BeginTowerRun081() => Sync117();
            public M1CommandResult AdvanceTowerRun081() => Sync117();
            public M1CommandResult EnterAbyssBattle022() => Sync117();
            public M1CommandResult RecordEquipmentUse022(string item, string track) => Sync117();
            public M1CommandResult EvolveWeapon022(string item, string recipe) => Sync117();
            public M1CommandResult CertifyAdvancedClass022(string recruit, string cls) => Sync117();
            public M1CommandResult BeginAbyssOperation022(string operation) => Sync117();
            public M1CommandResult RetreatTowerRun081() => Sync117();
            public M1CommandResult RecoverInactiveLegacyTower084() => Sync117();
            public M1CommandResult CommitAbyssStep022() => Sync117();
            public M1CommandResult ApplyAbyssStep022() => Sync117();
            public M1CommandResult CommitAbyssBattleResult022() => Sync117();
            public M1CommandResult FinalizeAbyssOperation022() => Sync117();
            public M1CommandResult FinalizeAbyssBattle022() => Sync117();
            public M1CommandResult CraftInvocationArtifact022(string id) => Sync117();
            public M1CommandResult CraftInvocationArtifact022(string id, IReadOnlyList<string> affixes) => Sync117();
            public M1CommandResult EvolveInvocationArtifact022(string item) => Sync117();
            public M1CommandResult InvokeEligibleEchoForecast022() => Sync117();
            public M1CommandResult InvokeAcceptedCovenantForecast022(string covenant) => Sync117();
            public M1CommandResult AdvanceCovenant022(string covenant) => Sync117();
            public M1CommandResult AcceptCovenant022(string covenant) => Sync117();
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Sync117();
            public M1CommandResult SignRecruit(string id) => Sync117();
            public M1CommandResult EquipItem(string recruit, string slot, string item) => Sync117();
            public M1CommandResult UnequipItem(string recruit, string slot) => Sync117();
            public M1CommandResult SetEquipmentLock(string recruit, string slot, bool locked) => Sync117();
            public M1CommandResult CompleteEquipmentReview() => Sync117();
            public M1CommandResult AddUnion() => Sync117();
            public M1CommandResult RemoveUnion(int index) => Sync117();
            public M1CommandResult AssignRecruitToUnion(string recruit, int union, int slot) => Sync117();
            public M1CommandResult UnassignRecruitFromUnion(string recruit) => Sync117();
            public M1CommandResult SetUnionLeader(int union, string recruit) => Sync117();
            public M1CommandResult SetFormation(int union, string id) => Sync117();
            public M1CommandResult SetDoctrine(int union, string id) => Sync117();
            public M1CommandResult SaveAndReloadProof() => Sync117();
            public M1CommandResult PlaceGuildCityBuilding017D(string plot, string building) => Sync117();
            public M1CommandResult UpgradeGuildCityBuilding017D(string plot) => Sync117();
            public M1CommandResult AssignGuildCityStaff017D(string plot, string recruit) => Sync117();
            public M1CommandResult RecallGuildCityStaff017D(string recruit) => Sync117();
            public M1CommandResult SetGuildCityAssignment017D(string recruit, string kind) => Sync117();
            public M1CommandResult ArchiveGuildCityMember017D(string recruit, bool confirmed) => Sync117();
            public M1CommandResult CommitGuildCityApplicantBoard017D() => Sync117();
            public M1CommandResult RefreshGuildCityApplicantBoard017D() => Sync117();
            public M1CommandResult SignGuildCityApplicant017D(string recruit) => Sync117();
            public M1CommandResult DeclineGuildCityApplicant017D(string recruit) => Sync117();
            public M1CommandResult AcceptGuildCityContract017D(string contract) => Sync117();
            public M1CommandResult StartGuildCityExpedition017D() => Sync117();
            public M1CommandResult MoveGuildCityExpedition017D(string node) => Sync117();
            public M1CommandResult ResolveGuildCityCheck017D(string evt, string actor, string assistant, int modifier) => Sync117();
            public M1CommandResult DiscoverGateworksMaintenancePassage066() => Sync117();
            public M1CommandResult CommitGuildCityEncounter017D(string encounter) => Sync117();
            public M1CommandResult StartCommittedGuildCityBattle017D() => Sync117();
            public M1CommandResult FinalizeGuildCityOperation017D() => Sync117();
            public M1CommandResult AddGuildCityRelationshipMemory017D(string first, string second, string source, string summary, int strength, string scene) => Sync117();
            public M1CommandResult ViewGuildCityRelationshipScene017D(string scene) => Sync117();
        }
    }
}
