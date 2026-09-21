using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class TowerAsyncLifecycle116Tests
    {
        GameObject _owner, _host;
        M2BattleExperienceController072 _controller;
        PendingCoordinator116 _coordinator;

        [SetUp]
        public void SetUp116()
        {
            _owner = new GameObject("Tower async owner116");
            _host = new GameObject("Tower async host116", typeof(RectTransform));
            _coordinator = new PendingCoordinator116();
            _controller = _owner.AddComponent<M2BattleExperienceController072>();
            _controller.Initialize(_coordinator, _host.GetComponent<RectTransform>());
            _controller.ContinueTowerAuto107 = () => throw new InvalidOperationException("Async dispatch used the synchronous fallback.");
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
            _controller.AnimationSpeed = 16f;
        }

        [TearDown]
        public void TearDown116()
        {
            if (_owner != null) UnityEngine.Object.DestroyImmediate(_owner);
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
        }

        void Start116()
        {
            _controller.SetAutoOrders091(true);
            _coordinator.ResolveVictory();
            _controller.Refresh(_coordinator.Battle);
            Assert.That(_coordinator.Dispatches, Is.EqualTo(1));
            Assert.That(_controller.IsResolving, Is.True);
        }

        [UnityTest]
        public IEnumerator PendingTransactionRendersWithoutProjectionPollingOrRepeatDispatch116()
        {
            Start116();
            var reads = _coordinator.Reads;
            var frames = Time.frameCount;
            for (var index = 0; index < 4; index++)
            {
                _controller.Refresh(_coordinator.Battle);
                yield return null;
            }
            Assert.That(Time.frameCount, Is.GreaterThan(frames));
            Assert.That(_coordinator.Reads, Is.EqualTo(reads), "Pending frames may only read task/phase, not campaign projections.");
            Assert.That(_coordinator.Dispatches, Is.EqualTo(1));
            Assert.That(Phase(), Is.EqualTo("BANKING REWARDS…"));
            Assert.That(_controller.AutoOrdersEnabled091, Is.True);
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(16f));
            _coordinator.TowerAutoIsSaving116 = true;
            yield return null;
            Assert.That(Phase(), Is.EqualTo("SAVING…"));
            Assert.That(_controller.OwnedResultsView078.OutcomeTitleText079, Is.Empty);
            _coordinator.CompleteSaved();
            yield return Settled116();
            Assert.That(_controller.AutoOrdersEnabled091, Is.True);
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(16f));
            Assert.That(_controller.LastCompletedTowerFloor108, Is.EqualTo(315));
            Assert.That(_coordinator.Dispatches, Is.EqualTo(1));
            _controller.SetAutoOrders091(false);
        }

        [UnityTest]
        public IEnumerator AutoOffDuringStoreRemainsBusyUntilCommittedNextFloor116()
        {
            Start116();
            _coordinator.TowerAutoIsSaving116 = true;
            _controller.SetAutoOrders091(false);
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
            for (var index = 0; index < 3; index++)
            {
                yield return null;
                Assert.That(_controller.IsResolving, Is.True, "QA may not read/reload the save until the coordinator task settles.");
            }
            Assert.That(Phase(), Is.EqualTo("SAVING…"));
            _coordinator.CompleteSaved();
            yield return Settled116();
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
            Assert.That(_controller.LastCompletedTowerFloor108, Is.EqualTo(315));
            Assert.That(_coordinator.Battle.BattleId, Does.Contain("ACTUAL098_316_"));
            Assert.That(_coordinator.Commits, Is.EqualTo(1));
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(16f));
        }

        [UnityTest]
        public IEnumerator HideAndReenterDuringChangedUsesOriginalVictoryAndCoordinatorBusyTruth116()
        {
            Start116();
            _coordinator.TowerAutoIsSaving116 = true;
            _controller.SetVisible(false);
            Assert.That(Field("_towerWait116").GetValue(_controller), Is.Null);
            Assert.That(_controller.IsResolving, Is.True, "Detached views still expose the actual coordinator transaction.");
            yield return null;
            _coordinator.BeforeTaskCompletion = () => Assert.That(_controller.EnterCurrentBattle(), Is.True);
            // Reentry sees already-published 316 while the owner still holds 315's
            // pending task. Its original receipt, not current Battle, identifies the clear.
            _coordinator.CompleteSaved();
            yield return Settled116();
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(_controller.LastCompletedTowerFloor108, Is.EqualTo(315));
            Assert.That((string)Field("_towerAutoFailure107").GetValue(_controller), Is.Empty);
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
            Assert.That(_coordinator.Commits, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HideBeforeStoreCancelsAndReentryRetainsUnclaimedVictory116()
        {
            Start116();
            _controller.SetVisible(false);
            yield return null;
            Assert.That(_coordinator.PendingTowerAutoTransition116, Is.Null);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(_coordinator.Commits, Is.Zero);
            Assert.That(_coordinator.Battle.Reward.Claimed, Is.False);
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
            Assert.That(_controller.OwnedResultsView078.OutcomeTitleText079, Is.Not.Empty);
            Assert.That(_coordinator.Dispatches, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyDuringStoreDoesNotAbandonOwnerOrRefreshReplacement116()
        {
            Start116();
            _coordinator.TowerAutoIsSaving116 = true;
            var pending = _coordinator.PendingTowerAutoTransition116;
            UnityEngine.Object.DestroyImmediate(_owner);
            _owner = null;
            Assert.That(pending.IsCompleted, Is.False);
            _coordinator.CompleteSaved();
            yield return null;
            Assert.That(pending.Result.Succeeded, Is.True);
            Assert.That(_coordinator.Commits, Is.EqualTo(1));
            Assert.That(_coordinator.PendingTowerAutoTransition116, Is.Null);
        }

        [UnityTest]
        public IEnumerator CoordinatorReplacementIgnoresPriorSaveCompletion116()
        {
            Start116();
            _coordinator.TowerAutoIsSaving116 = true;
            var replacement = new PendingCoordinator116();
            _controller.Initialize(replacement, _host.GetComponent<RectTransform>());
            _controller.SetAutoOrders091(false);
            _controller.EnterCurrentBattle();
            var phase = Phase();
            var reads = replacement.Reads;
            _coordinator.CompleteSaved();
            yield return null;
            yield return null;
            Assert.That(replacement.Reads, Is.EqualTo(reads));
            Assert.That(Phase(), Is.EqualTo(phase));
            Assert.That(_controller.LastCompletedTowerFloor108, Is.Zero);
            Assert.That(_controller.IsResolving, Is.False);
            Assert.That(replacement.Dispatches, Is.Zero);
        }

        IEnumerator Settled116()
        {
            for (var frame = 0; _controller.IsResolving && frame < 12; frame++) yield return null;
            Assert.That(_controller.IsResolving, Is.False);
        }
        string Phase() => ((Text)Field("_phaseText").GetValue(_controller)).text;
        static FieldInfo Field(string name) => typeof(M2BattleExperienceController072).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        // Scheduling/lifetime fixture only. The companion real-coordinator tests
        // use pinned R288 bytes and compare the worker with all synchronous authorities.
        sealed class PendingCoordinator116 : IM2PresentationCoordinator, IM2BattleViewReader098, ITowerAutoAsyncTransition116
        {
            public event Action Changed;
            public int Dispatches, Commits, Reads;
            public Action BeforeTaskCompletion;
            public M2BattleView Battle = Live(315);
            M2BattleView _victory;
            TaskCompletionSource<M1CommandResult> _completion;
            public bool TowerAutoIsSaving116 { get; set; }
            public Task<M1CommandResult> PendingTowerAutoTransition116 => _completion != null && !_completion.Task.IsCompleted ? _completion.Task : null;
            public M1PresentationState State { get { Reads++; return new M1PresentationState { Battle = Battle }; } }
            public M2BattleView ReadBattleView098() { Reads++; return Battle; }
            public M2BattleView ReadPendingTowerVictory116() => _victory;
            public void ResolveVictory()
            {
                Battle.IsResolved = true;
                Battle.Outcome = "Victory";
                Battle.Reward = new M2BattleRewardView { RewardId = "SAVED116", Outcome = "Victory", CanClaim = true, GuildTreasuryXpAward = 330 };
                _victory = Battle;
            }
            public Task<M1CommandResult> AdvanceTowerAutoAfterVictoryAsync116()
            {
                Dispatches++;
                _completion = new TaskCompletionSource<M1CommandResult>();
                return _completion.Task;
            }
            public void CancelTowerAutoBeforeSave116()
            {
                if (!TowerAutoIsSaving116) _completion?.TrySetResult(M1CommandResult.Failure("Canceled before saving116"));
            }
            public void CompleteSaved()
            {
                Assert.That(TowerAutoIsSaving116, Is.True);
                Assert.That(_completion.Task.IsCompleted, Is.False);
                Commits++;
                Battle = Live(316);
                Changed?.Invoke();
                BeforeTaskCompletion?.Invoke();
                _completion.SetResult(M1CommandResult.Success());
                TowerAutoIsSaving116 = false;
            }
            static M2BattleView Live(int floor) => new M2BattleView {
                BattleId = "ABYSS_BATTLE022_FLOOR_05_ACTUAL098_" + floor + "_TEST116", Outcome = "InProgress", Round = 1,
                EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY116", DisplayName = "Guardian", Members = new[] {
                    new M2BattleMemberView { MemberId = "FOE116", DisplayName = "Guardian", CurrentHp = 100, MaximumHp = 100 } } } } };
            M1CommandResult Unexpected() => M1CommandResult.Failure("Unexpected gameplay command116");
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected();
            public M1CommandResult SignRecruit(string recruitId) => Unexpected();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => Unexpected();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => Unexpected();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => Unexpected();
            public M1CommandResult CompleteEquipmentReview() => Unexpected();
            public M1CommandResult AddUnion() => Unexpected();
            public M1CommandResult RemoveUnion(int unionIndex) => Unexpected();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => Unexpected();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => Unexpected();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => Unexpected();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => Unexpected();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => Unexpected();
            public M1CommandResult SaveAndReloadProof() => Unexpected();
            public M1CommandResult StartTutorialBattle() => Unexpected();
            public M1CommandResult SelectForecast(string unionId, string forecastId) => Unexpected();
            public M1CommandResult ConfirmBattleRound() => Unexpected();
            public M1CommandResult ReplayTutorialBattle() => Unexpected();
            public M1CommandResult RetryTutorialBattle() => Unexpected();
            public M1CommandResult ClaimBattleRewards() => Unexpected();
        }
    }
}
