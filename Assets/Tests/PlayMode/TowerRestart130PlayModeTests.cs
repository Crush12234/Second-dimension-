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
    // Actual shipping controls and EventSystem input; fake authority makes save
    // phase/failure/detachment reproducible. Real saves are covered in EditMode.
    public sealed class TowerRestart130PlayModeTests
    {
        readonly List<GameObject> _hosts = new List<GameObject>();
        const string Restart = "Restart Tower from Floor 1 after party defeat 130";
        [UnityTearDown] public IEnumerator Cleanup130()
        { foreach (var host in _hosts) if (host != null) UnityEngine.Object.Destroy(host); _hosts.Clear(); yield return null; }

        [UnityTest]
        public IEnumerator ShippingDefeatResultsContinuePreservesTowerThenRestartFailureAndRetryRemainUsable130()
        {
            var owner = new RestartCoordinator130(); owner.ShowDefeatResults130();
            var harness = Harness130(owner, M1Screen.BattleResults);
            yield return Frames130(4);
            Assert.That(Screen130(harness.Presenter), Is.EqualTo(M1Screen.Battle), "A real-pattern Tower ID must enter shipping 072 results, not the legacy fixture presenter.");
            Assert.That(harness.Presenter.IsFirstHourBattleExperienceActive072, Is.True);
            Button proceed = null; var deadline = Time.realtimeSinceStartup + 30f;
            while (proceed == null || !proceed.IsInteractable())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Shipping results did not reveal their usable Continue control.");
                proceed = harness.Canvas.GetComponentsInChildren<Button>(false).LastOrDefault(x => x.name == "Continue From Battle Results 072");
                yield return null;
            }
            Click130(proceed, true); yield return Frames130(4);
            Assert.That(Screen130(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(Get130<string>(harness.Presenter, "_guildCityTab017D"), Is.EqualTo("ABYSS"));
            Assert.That(owner.CampaignProgression022.ActiveAbyssOperationId, Is.EqualTo("TOWER_DEFEATED130"));
            Assert.That(owner.RestartCalls, Is.Zero); Assert.That(owner.SyncCalls, Is.Zero,
                "Defeat Continue must not claim, retreat, clear the active Tower owner or start a battle.");
            var restart = Find130(harness.Canvas, Restart); Click130(restart, true);
            Click130(restart, false); Assert.That(owner.RestartCalls, Is.EqualTo(1));
            owner.Complete130(false); yield return Frames130(4);
            Assert.That(harness.Canvas.GetComponentsInChildren<Text>(false).Any(x => x.text.Contains("fixture write failed")), Is.True);
            Click130(Find130(harness.Canvas, Restart), true); Assert.That(owner.RestartCalls, Is.EqualTo(2));
            Assert.That(harness.Canvas.GetComponentsInChildren<Text>(false).Any(x => x.text.Contains("fixture write failed")), Is.False,
                "Retry hides the previous failure while the new transaction runs.");
            owner.Saving = true; yield return Frames130(3); owner.Complete130(true); yield return Frames130(4);
            Assert.That(Find130(harness.Canvas, "Enter Tower battle 081").GetComponentInChildren<Text>().text, Does.Contain("FIGHT FLOOR 1"));
            Assert.That(owner.SyncCalls, Is.Zero); Assert.That(owner.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator ActualRestartButtonDispatchesOnceSavesThenShowsFloorOneWithoutStartingBattle130()
        {
            var owner = new RestartCoordinator130(); var harness = Harness130(owner);
            yield return Frames130(4);
            var button = Find130(harness.Canvas, Restart); Click130(button, true);
            var pending = owner.PendingTowerRestart130;
            Assert.That(pending, Is.Not.Null); Assert.That(owner.RestartCalls, Is.EqualTo(1));
            AssertBusy130(harness.Presenter, true);
            Assert.That(Find130(harness.Canvas, "Retreat from defeated Tower run 081").IsInteractable(), Is.False);
            var reads = owner.Reads130; yield return Frames130(4);
            Assert.That(button.GetComponentInChildren<Text>().text, Does.Contain("PREPARING FLOOR 1"));
            Click130(button, false); Assert.That(owner.RestartCalls, Is.EqualTo(1));
            owner.Saving = true; yield return Frames130(4);
            Assert.That(button.GetComponentInChildren<Text>().text, Does.Contain("SAVING"));
            Assert.That(owner.Reads130, Is.EqualTo(reads), "Pending frames read task/phase, not full campaign projections.");
            owner.Complete130(true); yield return Frames130(4);
            Assert.That(pending.Result.Succeeded, Is.True); AssertBusy130(harness.Presenter, false);
            Assert.That(Screen130(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(Get130<string>(harness.Presenter, "_guildCityTab017D"), Is.EqualTo("ABYSS"));
            Assert.That(Find130(harness.Canvas, "Enter Tower battle 081").GetComponentInChildren<Text>().text, Does.Contain("FIGHT FLOOR 1"));
            Assert.That(owner.SyncCalls, Is.Zero, "Restart must not enter battle, bank, Auto or retreat through an unrelated command.");
            Assert.That(owner.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator FailedRestartShowsRetryAndSurvivorRetreatNeverOffersRestart130()
        {
            var owner = new RestartCoordinator130(); var harness = Harness130(owner);
            yield return Frames130(4); Click130(Find130(harness.Canvas, Restart), true);
            owner.Complete130(false); yield return Frames130(4);
            AssertBusy130(harness.Presenter, false);
            Assert.That(harness.Canvas.GetComponentsInChildren<Text>(false).Any(x => x.text.Contains("fixture write failed")), Is.True);
            Click130(Find130(harness.Canvas, Restart), true); Assert.That(owner.RestartCalls, Is.EqualTo(2));
            owner.Saving = true; owner.Complete130(true); yield return Frames130(4);
            var survivors = new RestartCoordinator130 { Eligible = false };
            harness.Presenter.Initialize(survivors); yield return Frames130(4);
            Assert.That(harness.Canvas.GetComponentsInChildren<Button>(false).Any(x => x.name == Restart), Is.False);
            Assert.That(Find130(harness.Canvas, "Retreat from defeated Tower run 081").IsInteractable(), Is.True);
            Assert.That(survivors.RestartCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DisableAndReattachDuringSaveObservesOwnerWithoutRedispatch130()
        {
            var owner = new RestartCoordinator130(); var harness = Harness130(owner);
            yield return Frames130(4); Click130(Find130(harness.Canvas, Restart), true);
            owner.Saving = true; var pending = owner.PendingTowerRestart130;
            harness.Host.SetActive(false); AssertBusy130(harness.Presenter, false);
            Assert.That(pending.IsCompleted, Is.False); Assert.That(owner.CancelCalls, Is.EqualTo(1));
            harness.Host.SetActive(true); yield return Frames130(4);
            Assert.That(harness.Canvas.GetComponentsInChildren<Text>(false).Any(x => x.text == "SAVING…"), Is.True);
            Assert.That(owner.RestartCalls, Is.EqualTo(1)); AssertBusy130(harness.Presenter, true);
            var reads = owner.Reads130; yield return Frames130(3); Assert.That(owner.Reads130, Is.EqualTo(reads));
            harness.Host.SetActive(false); owner.Complete130(true); yield return Frames130(3);
            Assert.That(pending.Result.Succeeded, Is.True); AssertBusy130(harness.Presenter, false);
            harness.Host.SetActive(true); yield return Frames130(4);
            Assert.That(Screen130(harness.Presenter), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(Find130(harness.Canvas, "Enter Tower battle 081").IsInteractable(), Is.True);
            Assert.That(owner.SyncCalls, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReplacementAndDestroyedViewCannotReceiveOldSavedCompletion130()
        {
            var owner = new RestartCoordinator130(); var harness = Harness130(owner);
            yield return Frames130(4); Click130(Find130(harness.Canvas, Restart), true);
            owner.Saving = true; var pending = owner.PendingTowerRestart130;
            var replacement = new RestartCoordinator130(); harness.Presenter.Initialize(replacement);
            yield return Frames130(4); var reads = replacement.Reads130;
            owner.Complete130(true); yield return Frames130(4);
            Assert.That(pending.Result.Succeeded, Is.True); Assert.That(replacement.Reads130, Is.EqualTo(reads));
            Assert.That(Get130<string>(harness.Presenter, "_localStatus"), Is.Empty); AssertBusy130(harness.Presenter, false);
            Click130(Find130(harness.Canvas, Restart), true); replacement.Saving = true;
            pending = replacement.PendingTowerRestart130;
            UnityEngine.Object.Destroy(harness.Host); yield return null;
            Assert.That(pending.IsCompleted, Is.False); replacement.Complete130(true); yield return Frames130(3);
            Assert.That(pending.Result.Succeeded, Is.True); Assert.That(replacement.RestartCalls, Is.EqualTo(1));
        }

        HarnessState130 Harness130(RestartCoordinator130 coordinator, M1Screen screen = M1Screen.GuildOperations)
        {
            var host = new GameObject("Tower Manual Async 130 Test");
            _hosts.Add(host);
            var presenter = host.AddComponent<M1FlowPresenter>();
            presenter.enabled = false;
            Set130(presenter, "_screen", screen);
            Set130(presenter, "_guildCityTab017D", "ABYSS");
            presenter.Initialize(coordinator);
            var canvas = Get130<Canvas>(presenter, "_canvas");
            canvas.sortingOrder = 30000;
            presenter.enabled = true;
            return new HarnessState130 { Host = host, Presenter = presenter, Canvas = canvas };
        }

        static IEnumerator Frames130(int count) { for (int i = 0; i < count; i++) yield return null; }
        static FieldInfo Field130(string name) => typeof(M1FlowPresenter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static T Get130<T>(M1FlowPresenter presenter, string name) => (T)Field130(name).GetValue(presenter);
        static void Set130(M1FlowPresenter presenter, string name, object value) => Field130(name).SetValue(presenter, value);
        static M1Screen Screen130(M1FlowPresenter presenter) => Get130<M1Screen>(presenter, "_screen");
        static void AssertBusy130(M1FlowPresenter presenter, bool busy)
        {
            Assert.That(Get130<bool>(presenter, "_towerRoomCommandRunning084"), Is.EqualTo(busy));
            Assert.That(Get130<bool>(presenter, "_suppressBoardAdventureCoordinatorRefresh084"), Is.EqualTo(busy));
            Assert.That(Get130<Coroutine>(presenter, "_towerRestartWait130") != null, Is.EqualTo(busy));
        }
        static Button Find130(Canvas canvas, string name) => canvas.GetComponentsInChildren<Button>(true)
            .Last(button => button.name == name && button.gameObject.activeInHierarchy);
        static void Click130(Button button, bool interactable)
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

        sealed class HarnessState130 { public GameObject Host; public M1FlowPresenter Presenter; public Canvas Canvas; }

        sealed class RestartCoordinator130 : IM2PresentationCoordinator, IM2BattleViewReader098,
            ICampaignProgressionPresentationCoordinator022, IGuildCityPresentationCoordinator017D,
            ITowerRestartAsyncTransition130, ITowerVictoryBankCoordinator110, ITowerBattleStartCoordinator110
        {
            readonly M1PresentationState _state = new M1PresentationState { HasCampaign = true, HasSave = true,
                GuildmasterName = "Tower restart UI fixture", GuildXpRequiredForNextLevel = 100, OpeningUnionsLegal = true };
            readonly GuildCityPresentationState017D _city = new GuildCityPresentationState017D { IsAvailable = true,
                CampaignModeId = "Standard", TutorialDepthId = "Minimal", TextScalePercent = 100, ReducedMotion = true };
            CampaignProgressionPresentationState022 _tower = Tower130(false);
            TaskCompletionSource<M1CommandResult> _completion;
            public event Action Changed;
            public bool Saving, Eligible = true;
            public int RestartCalls, CancelCalls, SyncCalls, Reads130;
            public M1PresentationState State { get { Reads130++; return _state; } }
            public M2BattleView ReadBattleView098() { Reads130++; return _state.Battle; }
            public GuildCityPresentationState017D GuildCity017D { get { Reads130++; return _city; } }
            public CampaignProgressionPresentationState022 CampaignProgression022 { get { Reads130++; return _tower; } }
            public bool CanRestartTowerAfterPartyDefeat130 => Eligible;
            public Task<M1CommandResult> PendingTowerRestart130 => _completion != null && !_completion.Task.IsCompleted ? _completion.Task : null;
            public bool TowerRestartIsSaving130 => Saving;
            public Task<M1CommandResult> RestartTowerFromFloorOneAsync130()
            { Assert.That(PendingTowerRestart130, Is.Null); RestartCalls++; _completion = new TaskCompletionSource<M1CommandResult>(); return _completion.Task; }
            public void CancelTowerRestartBeforeSave130()
            { if (PendingTowerRestart130 == null) return; CancelCalls++; if (!Saving) _completion.TrySetCanceled(); }
            public void ShowDefeatResults130()
            {
                _state.Battle = new M2BattleView {
                    BattleId = "ABYSS_BATTLE022_FLOOR_10_ACTUAL098_10_TEST130", IsResolved = true,
                    Outcome = "Defeat", Round = 4, LastResolvedRound = 3, Objective = "Tower full-party-defeat presentation fixture",
                    PlayerUnions = new[] { new M2BattleUnionView { UnionId = "UNION130", DisplayName = "Fallen Union", Members = new[] {
                        new M2BattleMemberView { MemberId = "ACTOR130", DisplayName = "Fallen member", CurrentHp = 0, MaximumHp = 100, Downed = true } } } },
                    EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY130", DisplayName = "Tower guardian", Members = new[] {
                        new M2BattleMemberView { MemberId = "FOE130", DisplayName = "Tower guardian", CurrentHp = 100, MaximumHp = 100 } } } },
                    Reward = new M2BattleRewardView { RewardId = "FAKE_REWARD130", Outcome = "Defeat", Claimed = false }
                };
            }
            public void Complete130(bool success)
            {
                if (success) { _tower = Tower130(true); Eligible = false; }
                Changed?.Invoke(); // Shipping publication precedes owner task completion.
                _completion.SetResult(success ? M1CommandResult.Success("Floor 1 saved.") : M1CommandResult.Failure("fixture write failed; retry available"));
                Saving = false;
            }
            static CampaignProgressionPresentationState022 Tower130(bool restarted) => new CampaignProgressionPresentationState022 {
                IsAvailable = true, TowerFloorNumber = restarted ? 1 : 10, HighestClearedTowerFloor = 9,
                TowerFloorDisplayName = "Tower restart fixture", TowerRewardSummary = "25 GUILD XP",
                ActiveAbyssOperationId = restarted ? "TOWER_FRESH130" : "TOWER_DEFEATED130",
                ActiveAbyssStatus = restarted ? "Active" : "AwaitingBattle", TowerBattleResolved = !restarted,
                TowerBattleWon = false, ActiveStepRequiresBattle = restarted, ActiveOperationDisplayName = "Tower restart fixture", ActiveAbyssFloorId = restarted ? "FLOOR_01" : "FLOOR_10"
            };
            M1CommandResult Sync130() { SyncCalls++; throw new NotSupportedException("Unexpected synchronous authority call in async presenter test."); }
            public M1CommandResult StartTutorialBattle() => Sync130();
            public M1CommandResult SelectForecast(string union, string forecast) => Sync130();
            public M1CommandResult ConfirmBattleRound() => Sync130();
            public M1CommandResult ReplayTutorialBattle() => Sync130();
            public M1CommandResult RetryTutorialBattle() => Sync130();
            public M1CommandResult ClaimBattleRewards() => Sync130();
            public M1CommandResult BankTowerVictory110() => Sync130();
            public M1CommandResult StartTowerBattle110() => Sync130();
            public M1CommandResult BeginTowerRun081() => Sync130();
            public M1CommandResult AdvanceTowerRun081() => Sync130();
            public M1CommandResult EnterAbyssBattle022() => Sync130();
            public M1CommandResult RecordEquipmentUse022(string item, string track) => Sync130();
            public M1CommandResult EvolveWeapon022(string item, string recipe) => Sync130();
            public M1CommandResult CertifyAdvancedClass022(string recruit, string cls) => Sync130();
            public M1CommandResult BeginAbyssOperation022(string operation) => Sync130();
            public M1CommandResult RetreatTowerRun081() => Sync130();
            public M1CommandResult RecoverInactiveLegacyTower084() => Sync130();
            public M1CommandResult CommitAbyssStep022() => Sync130();
            public M1CommandResult ApplyAbyssStep022() => Sync130();
            public M1CommandResult CommitAbyssBattleResult022() => Sync130();
            public M1CommandResult FinalizeAbyssOperation022() => Sync130();
            public M1CommandResult FinalizeAbyssBattle022() => Sync130();
            public M1CommandResult CraftInvocationArtifact022(string id) => Sync130();
            public M1CommandResult CraftInvocationArtifact022(string id, IReadOnlyList<string> affixes) => Sync130();
            public M1CommandResult EvolveInvocationArtifact022(string item) => Sync130();
            public M1CommandResult InvokeEligibleEchoForecast022() => Sync130();
            public M1CommandResult InvokeAcceptedCovenantForecast022(string covenant) => Sync130();
            public M1CommandResult AdvanceCovenant022(string covenant) => Sync130();
            public M1CommandResult AcceptCovenant022(string covenant) => Sync130();
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Sync130();
            public M1CommandResult SignRecruit(string id) => Sync130();
            public M1CommandResult EquipItem(string recruit, string slot, string item) => Sync130();
            public M1CommandResult UnequipItem(string recruit, string slot) => Sync130();
            public M1CommandResult SetEquipmentLock(string recruit, string slot, bool locked) => Sync130();
            public M1CommandResult CompleteEquipmentReview() => Sync130();
            public M1CommandResult AddUnion() => Sync130();
            public M1CommandResult RemoveUnion(int index) => Sync130();
            public M1CommandResult AssignRecruitToUnion(string recruit, int union, int slot) => Sync130();
            public M1CommandResult UnassignRecruitFromUnion(string recruit) => Sync130();
            public M1CommandResult SetUnionLeader(int union, string recruit) => Sync130();
            public M1CommandResult SetFormation(int union, string id) => Sync130();
            public M1CommandResult SetDoctrine(int union, string id) => Sync130();
            public M1CommandResult SaveAndReloadProof() => Sync130();
            public M1CommandResult PlaceGuildCityBuilding017D(string plot, string building) => Sync130();
            public M1CommandResult UpgradeGuildCityBuilding017D(string plot) => Sync130();
            public M1CommandResult AssignGuildCityStaff017D(string plot, string recruit) => Sync130();
            public M1CommandResult RecallGuildCityStaff017D(string recruit) => Sync130();
            public M1CommandResult SetGuildCityAssignment017D(string recruit, string kind) => Sync130();
            public M1CommandResult ArchiveGuildCityMember017D(string recruit, bool confirmed) => Sync130();
            public M1CommandResult CommitGuildCityApplicantBoard017D() => Sync130();
            public M1CommandResult RefreshGuildCityApplicantBoard017D() => Sync130();
            public M1CommandResult SignGuildCityApplicant017D(string recruit) => Sync130();
            public M1CommandResult DeclineGuildCityApplicant017D(string recruit) => Sync130();
            public M1CommandResult AcceptGuildCityContract017D(string contract) => Sync130();
            public M1CommandResult StartGuildCityExpedition017D() => Sync130();
            public M1CommandResult MoveGuildCityExpedition017D(string node) => Sync130();
            public M1CommandResult ResolveGuildCityCheck017D(string evt, string actor, string assistant, int modifier) => Sync130();
            public M1CommandResult DiscoverGateworksMaintenancePassage066() => Sync130();
            public M1CommandResult CommitGuildCityEncounter017D(string encounter) => Sync130();
            public M1CommandResult StartCommittedGuildCityBattle017D() => Sync130();
            public M1CommandResult FinalizeGuildCityOperation017D() => Sync130();
            public M1CommandResult AddGuildCityRelationshipMemory017D(string first, string second, string source, string summary, int strength, string scene) => Sync130();
            public M1CommandResult ViewGuildCityRelationshipScene017D(string scene) => Sync130();
        }
    }
}
