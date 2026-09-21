using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    // Actual results controls with a controllable authority task. Companion
    // EditMode tests cover the real transaction and durable campaign changes.
    public sealed class TowerClaimAsync120PlayModeTests
    {
        GameObject _host;
        M2BattleExperienceController072 _controller;
        ClaimCoordinator120 _owner;
        int _completed;

        [UnityTearDown]
        public IEnumerator Cleanup120()
        {
            if (_host != null) UnityEngine.Object.Destroy(_host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualTowerContinueClaimsOnceUpdatesVisiblePhaseAndCompletesOnce120()
        {
            Create120(true);
            yield return Ready120();
            var button = Continue120();
            Click120(button, true);
            var pending = _owner.PendingTowerClaim120;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1));
            Assert.That(_owner.SyncClaims, Is.Zero);
            Assert.That(_owner.AutoDispatches + _owner.ManualDispatches, Is.Zero);
            Assert.That(_controller.IsResolving, Is.True);
            Assert.That(button.IsInteractable(), Is.False);
            Assert.That(_completed, Is.Zero);
            var reads = _owner.Reads;
            yield return Frames120(4);
            Assert.That(Phase120(), Does.Contain("CLAIMING REWARDS"));
            Click120(button, false);
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1));
            _owner.Saving = true;
            yield return Frames120(4);
            Assert.That(Phase120(), Does.Contain("SAVING"));
            Assert.That(_owner.Reads, Is.EqualTo(reads), "Waiting reads task/phase only, not battle or campaign projections.");
            _owner.CompleteSaved120();
            yield return Frames120(4);
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_completed, Is.EqualTo(1));
            yield return Frames120(4);
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1));
            Assert.That(_owner.SyncClaims, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HidingDuringStoreReleasesWaiterButOwnerSavesWithoutStaleCompletion120()
        {
            Create120(true);
            yield return Ready120();
            Click120(Continue120(), true);
            _owner.Saving = true;
            var pending = _owner.PendingTowerClaim120;
            _controller.SetVisible(false);
            Assert.That(Claiming120(), Is.False);
            Assert.That(_owner.CancelCalls, Is.EqualTo(1));
            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(_owner.PendingTowerClaim120, Is.SameAs(pending));
            Assert.That(_controller.IsResolving, Is.True, "Busy state must retain the actual owner save truth after hiding.");
            _owner.CompleteSaved120();
            yield return Frames120(4);
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_completed, Is.Zero, "The detached waiter cannot navigate through BattleCompleted.");
            Assert.That(_owner.Commits, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyingDuringStoreLeavesOwnerTaskAliveAndNeverCallsRetiredView120()
        {
            Create120(true);
            yield return Ready120();
            Click120(Continue120(), true);
            _owner.Saving = true;
            var pending = _owner.PendingTowerClaim120;
            UnityEngine.Object.Destroy(_host);
            yield return null;
            Assert.That(pending.IsCompleted, Is.False);
            Assert.That(_owner.CancelCalls, Is.GreaterThanOrEqualTo(1), "Disable/destroy cancellation is safe and idempotent during the owned save.");
            _owner.CompleteSaved120();
            yield return Frames120(3);
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_completed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReplacingCoordinatorDuringStoreIgnoresPriorClaimCompletion120()
        {
            Create120(true);
            yield return Ready120();
            Click120(Continue120(), true);
            _owner.Saving = true;
            var pending = _owner.PendingTowerClaim120;
            var replacement = new ClaimCoordinator120(false);
            _controller.Initialize(replacement);
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
            yield return Ready120();
            var reads = replacement.Reads;
            var label = Label120(Continue120());
            Assert.That(Claiming120(), Is.False);
            Assert.That(_owner.CancelCalls, Is.EqualTo(1));
            _owner.CompleteSaved120();
            yield return Frames120(4);
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(replacement.Reads, Is.EqualTo(reads));
            Assert.That(replacement.AsyncClaims + replacement.SyncClaims, Is.Zero);
            Assert.That(Label120(Continue120()), Is.EqualTo(label));
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_completed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReenteringPendingClaimReusesOwnerTaskAndCompletesOnce120()
        {
            Create120(true);
            yield return Ready120();
            Click120(Continue120(), true);
            _owner.Saving = true;
            var pending = _owner.PendingTowerClaim120;
            _controller.SetVisible(false);
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
            yield return Frames120(4);
            Assert.That(_owner.PendingTowerClaim120, Is.SameAs(pending));
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1));
            Assert.That(Phase120(), Does.Contain("SAVING"));
            Assert.That(Continue120().IsInteractable(), Is.False);
            Assert.That(_controller.IsResolving, Is.True);
            var reads = _owner.Reads;
            yield return Frames120(4);
            Assert.That(_owner.Reads, Is.EqualTo(reads));
            _owner.CompleteSaved120();
            yield return Frames120(4);
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FreshControllerAttachesPendingClaimShowsFailureAndSupportsVisibleRetry120()
        {
            var owner = new ClaimCoordinator120(true);
            var pending = owner.ClaimTowerBattleVictoryAsync120();
            Create120(owner); // No earlier controller or previously shown results exist.
            Assert.That(_owner.PendingTowerClaim120, Is.SameAs(pending));
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1));
            var reads = _owner.Reads;
            var deadline = Time.realtimeSinceStartup + 8f;
            while (!ObjectiveVisible120() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Field120<RectTransform>("_resultLayer").gameObject.activeInHierarchy, Is.True);
            Assert.That(Phase120(), Does.Contain("CLAIMING REWARDS"));
            Assert.That(Continue120().IsInteractable(), Is.False);
            _owner.Saving = true;
            yield return Frames120(4);
            Assert.That(Phase120(), Does.Contain("SAVING"));
            Assert.That(_owner.Reads, Is.EqualTo(reads), "A fresh attachment renders the saved result once, then polls task/phase only.");
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1), "Attaching must reuse the pending owner task.");
            _owner.Fail120("The fresh-view claim could not be saved.");
            yield return Ready120();
            Assert.That(pending.Result.Succeeded, Is.False);
            Assert.That(Phase120(), Does.Contain("fresh-view claim could not be saved"));
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_completed, Is.Zero);
            Click120(Continue120(), true);
            Assert.That(_owner.AsyncClaims, Is.EqualTo(2));
            Assert.That(_owner.PendingTowerClaim120, Is.Not.SameAs(pending));
            _owner.Saving = true;
            _owner.CompleteSaved120();
            yield return Frames120(4);
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_owner.SyncClaims + _owner.AutoDispatches + _owner.ManualDispatches, Is.Zero);
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_controller.IsResolving, Is.False);
        }

        [UnityTest]
        public IEnumerator FailedTowerClaimKeepsResultsUsableAndExplicitRetryCompletes120()
        {
            Create120(true);
            yield return Ready120();
            Click120(Continue120(), true);
            var first = _owner.PendingTowerClaim120;
            _owner.Fail120("The isolated store rejected this claim.");
            yield return Frames120(4);
            Assert.That(first.Result.Succeeded, Is.False);
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(Claiming120(), Is.False);
            Assert.That(Phase120(), Does.Contain("isolated store rejected"));
            Assert.That(Continue120().IsInteractable(), Is.True);
            Assert.That(_owner.Commits, Is.Zero);
            Assert.That(_completed, Is.Zero);
            yield return Frames120(4);
            Assert.That(_owner.AsyncClaims, Is.EqualTo(1), "A failed claim must not retry itself.");
            Click120(Continue120(), true);
            Assert.That(_owner.PendingTowerClaim120, Is.Not.SameAs(first));
            Assert.That(_owner.AsyncClaims, Is.EqualTo(2));
            _owner.Saving = true;
            _owner.CompleteSaved120();
            yield return Frames120(4);
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_owner.SyncClaims, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AutoVictoryRetainsAuto116DispatchWithoutClaimOrManualDispatch120()
        {
            Create120(true);
            _owner.SetLive120();
            _controller.Refresh(_owner.Battle);
            _controller.ContinueTowerAuto107 = () => throw new InvalidOperationException("Unexpected synchronous Auto fallback.");
            _controller.SetAutoOrders091(true);
            _owner.Battle = ClaimCoordinator120.Victory120(true, false);
            _controller.Refresh(_owner.Battle);
            Assert.That(_owner.AutoDispatches, Is.EqualTo(1));
            Assert.That(_owner.AsyncClaims + _owner.SyncClaims + _owner.ManualDispatches, Is.Zero);
            Assert.That(_owner.PendingTowerClaim120, Is.Null);
            Assert.That(Field120<Coroutine>("_towerClaimWait120"), Is.Null);
            Assert.That(_controller.IsResolving, Is.True);
            var reads = _owner.Reads;
            yield return Frames120(4);
            Assert.That(_owner.Reads, Is.EqualTo(reads));
            Assert.That(_owner.AutoDispatches, Is.EqualTo(1));
            Assert.That(_owner.AsyncClaims + _owner.ManualDispatches, Is.Zero);
            _controller.SetVisible(false);
            yield return Frames120(2);
            Assert.That(_owner.PendingTowerAutoTransition116, Is.Null, "Existing Auto cancellation still owns its own pending task.");
            Assert.That(_completed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator NonTowerContinuePreservesOriginalSynchronousClaimRoute120()
        {
            Create120(false);
            yield return Ready120();
            Click120(Continue120(), true);
            yield return Frames120(3);
            Assert.That(_owner.SyncClaims, Is.EqualTo(1));
            Assert.That(_owner.AsyncClaims, Is.Zero);
            Assert.That(_owner.PendingTowerClaim120, Is.Null);
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
            Assert.That(_controller.IsResolving, Is.False);
        }

        void Create120(bool tower) => Create120(new ClaimCoordinator120(tower));

        void Create120(ClaimCoordinator120 owner)
        {
            _completed = 0;
            _host = new GameObject("Tower Claim Async 120 Test");
            _owner = owner;
            _controller = _host.AddComponent<M2BattleExperienceController072>();
            _controller.Initialize(_owner);
            Field120<Canvas>("_ownedCanvas").sortingOrder = 30000;
            _controller.BattleCompleted += () => _completed++;
            _controller.ReducedMotion = true;
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
        }

        IEnumerator Ready120()
        {
            var deadline = Time.realtimeSinceStartup + 8f;
            while (!Continue120().IsInteractable() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Continue120().IsInteractable(), Is.True, "Wait for the actual staged results reveal, without bypassing its input gate.");
            yield return null;
        }
        static IEnumerator Frames120(int count) { for (var i = 0; i < count; i++) yield return null; }
        T Field120<T>(string name) => (T)typeof(M2BattleExperienceController072)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller);
        bool Claiming120() => Field120<bool>("_claiming");
        Text Objective120() => (Text)typeof(M2BattleResultsView072)
            .GetField("_nextObjective", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller.OwnedResultsView078);
        bool ObjectiveVisible120()
        {
            var objective = Objective120();
            return objective.gameObject.activeInHierarchy && objective.color.a > 0.95f &&
                objective.GetComponentsInParent<CanvasGroup>().All(group => group.alpha > 0.95f);
        }
        string Phase120()
        {
            var results = _controller.OwnedResultsView078;
            var objective = Objective120();
            Assert.That(objective.gameObject.activeInHierarchy, Is.True);
            Assert.That(objective.color.a, Is.GreaterThan(0.95f));
            foreach (var group in objective.GetComponentsInParent<CanvasGroup>())
                Assert.That(group.alpha, Is.GreaterThan(0.95f), "Pending text must be visible through the real results reveal groups.");
            return results.NextObjectiveText079;
        }
        Button Continue120() => Field120<RectTransform>("_resultLayer").GetComponentsInChildren<Button>(true)
            .Single(button => button.name == "Continue From Battle Results 072");
        static string Label120(Button button) => button.GetComponentInChildren<Text>(true).text;
        static void Click120(Button button, bool interactable)
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
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject),
                "The actual results control is intercepted by " + hit.name);
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(pointer.pointerPress, Is.EqualTo(button.gameObject));
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        sealed class ClaimCoordinator120 : IM2PresentationCoordinator, IM2BattleViewReader098, ITowerClaimAsyncTransition120,
            ITowerAutoAsyncTransition116, ITowerManualAsyncTransition117
        {
            readonly bool _tower;
            TaskCompletionSource<M1CommandResult> _completion;
            TaskCompletionSource<M1CommandResult> _autoCompletion;
            public ClaimCoordinator120(bool tower) { _tower = tower; Battle = Victory120(tower, false); }
            public event Action Changed;
            public M2BattleView Battle;
            public int Reads, AsyncClaims, SyncClaims, Commits, CancelCalls, AutoDispatches, ManualDispatches;
            public bool Saving;
            public M1PresentationState State { get { Reads++; return new M1PresentationState { HasCampaign = true, Battle = Battle }; } }
            public M2BattleView ReadBattleView098() { Reads++; return Battle; }
            public bool TowerVictoryAwaitingClaim120 => _tower && Battle.IsResolved && Battle.Reward != null && !Battle.Reward.Claimed;
            public Task<M1CommandResult> PendingTowerClaim120 => _completion != null && !_completion.Task.IsCompleted ? _completion.Task : null;
            public bool TowerClaimIsSaving120 => Saving;
            public Task<M1CommandResult> PendingTowerAutoTransition116 => _autoCompletion != null && !_autoCompletion.Task.IsCompleted ? _autoCompletion.Task : null;
            public bool TowerAutoIsSaving116 => false;
            public M2BattleView ReadPendingTowerVictory116() => Battle;
            public Task<M1CommandResult> AdvanceTowerAutoAfterVictoryAsync116()
            {
                AutoDispatches++;
                _autoCompletion = new TaskCompletionSource<M1CommandResult>();
                return _autoCompletion.Task;
            }
            public void CancelTowerAutoBeforeSave116() { _autoCompletion?.TrySetCanceled(); }
            public Task<M1CommandResult> PendingTowerManualTransition117 => null;
            public bool PendingTowerManualStartsBattle117 => false;
            public bool TowerManualIsSaving117 => false;
            public void CancelTowerManualBeforeSave117() { }
            public Task<M1CommandResult> BankTowerVictoryAsync117() { ManualDispatches++; throw new NotSupportedException(); }
            public Task<M1CommandResult> StartTowerBattleAsync117() { ManualDispatches++; throw new NotSupportedException(); }
            public void SetLive120()
            {
                Battle = Victory120(_tower, false);
                Battle.IsResolved = false;
                Battle.Outcome = "InProgress";
                Battle.Reward = null;
                Battle.EnemyUnions[0].Members[0].CurrentHp = 100;
            }
            public Task<M1CommandResult> ClaimTowerBattleVictoryAsync120()
            {
                Assert.That(_tower, Is.True, "Only an authoritative Tower routing hint may select the async claim.");
                Assert.That(PendingTowerClaim120, Is.Null);
                AsyncClaims++;
                _completion = new TaskCompletionSource<M1CommandResult>();
                return _completion.Task;
            }
            public void CancelTowerClaimBeforeSave120()
            {
                if (PendingTowerClaim120 == null) return;
                CancelCalls++;
                if (!Saving) _completion.TrySetCanceled();
            }
            public void CompleteSaved120()
            {
                Assert.That(Saving, Is.True);
                Assert.That(PendingTowerClaim120, Is.Not.Null);
                Commits++;
                Battle = Victory120(_tower, true);
                Changed?.Invoke(); // publish saved state before completing the owner task
                _completion.SetResult(M1CommandResult.Success("Reward saved."));
                Saving = false;
            }
            public void Fail120(string message)
            {
                Assert.That(PendingTowerClaim120, Is.Not.Null);
                _completion.SetResult(M1CommandResult.Failure(message));
                Saving = false;
            }
            public M1CommandResult ClaimBattleRewards()
            {
                SyncClaims++;
                Assert.That(_tower, Is.False, "Tower Continue must not use the synchronous fallback.");
                Commits++;
                Battle = Victory120(false, true);
                Changed?.Invoke();
                return M1CommandResult.Success("Campaign reward saved.");
            }
            public static M2BattleView Victory120(bool tower, bool claimed) => new M2BattleView {
                BattleId = tower ? "ABYSS_BATTLE022_FLOOR_05_ACTUAL098_315_TEST120" : "STORY_CLAIM_FIXTURE120",
                Outcome = "Victory", IsResolved = true, Round = 2, LastResolvedRound = 1,
                Objective = "Claim lifecycle fixture",
                Reward = new M2BattleRewardView { RewardId = "REWARD120", Outcome = "Victory", Claimed = claimed,
                    CanClaim = !claimed, GuildTreasuryXpAward = 330 },
                EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY120", DisplayName = "Guardian", Members = new[] {
                    new M2BattleMemberView { MemberId = "FOE120", DisplayName = "Guardian", CurrentHp = 0, MaximumHp = 100 } } } } };
            static M1CommandResult Unexpected120() => throw new NotSupportedException("Unexpected authority command in Claim lifecycle fixture.");
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected120();
            public M1CommandResult SignRecruit(string recruit) => Unexpected120();
            public M1CommandResult EquipItem(string recruit, string slot, string item) => Unexpected120();
            public M1CommandResult UnequipItem(string recruit, string slot) => Unexpected120();
            public M1CommandResult SetEquipmentLock(string recruit, string slot, bool locked) => Unexpected120();
            public M1CommandResult CompleteEquipmentReview() => Unexpected120();
            public M1CommandResult AddUnion() => Unexpected120();
            public M1CommandResult RemoveUnion(int index) => Unexpected120();
            public M1CommandResult AssignRecruitToUnion(string recruit, int union, int slot) => Unexpected120();
            public M1CommandResult UnassignRecruitFromUnion(string recruit) => Unexpected120();
            public M1CommandResult SetUnionLeader(int union, string recruit) => Unexpected120();
            public M1CommandResult SetFormation(int union, string formation) => Unexpected120();
            public M1CommandResult SetDoctrine(int union, string doctrine) => Unexpected120();
            public M1CommandResult SaveAndReloadProof() => Unexpected120();
            public M1CommandResult StartTutorialBattle() => Unexpected120();
            public M1CommandResult SelectForecast(string union, string forecast) => Unexpected120();
            public M1CommandResult ConfirmBattleRound() => Unexpected120();
            public M1CommandResult ReplayTutorialBattle() => Unexpected120();
            public M1CommandResult RetryTutorialBattle() => Unexpected120();
        }
    }
}
