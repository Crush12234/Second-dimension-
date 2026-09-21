using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleReadProjection098Tests
    {
        string _directory;
        GameObject _owner, _host;
        M1FlowPresenter _flow;
        M2BattleExperienceController072 _controller;
        bool _flowConsumed;

        [SetUp] public void Setup098()
        {
            _directory = Path.Combine(Path.GetTempPath(), "sd_battle_read098_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }
        [TearDown] public void Cleanup098()
        {
            if (_owner != null) UnityEngine.Object.DestroyImmediate(_owner);
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [Test] public void ShippingNarrowReaderIsFreshExactBattleProjectionAndCannotMutateAuthority098()
        {
            var coordinator = Actual098(out var path);
            var bytes = File.ReadAllBytes(path);
            var expected = JsonConvert.SerializeObject(coordinator.State.Battle);
            var first = M2BattleViewAccess098.Read(coordinator);
            var second = M2BattleViewAccess098.Read(coordinator);
            Assert.That(JsonConvert.SerializeObject(first), Is.EqualTo(expected));
            Assert.That(JsonConvert.SerializeObject(second), Is.EqualTo(expected));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.PlayerUnions[0].Members[0], Is.Not.SameAs(first.PlayerUnions[0].Members[0]));
            first.PlayerUnions[0].Members[0].CurrentHp = 0;
            first.Forecasts[0].MemberActions[0].ArtId = "NOT_AN_AUTHORED_ART";
            Assert.That(JsonConvert.SerializeObject(M2BattleViewAccess098.Read(coordinator)), Is.EqualTo(expected));
            Assert.That(JsonConvert.SerializeObject(coordinator.State.Battle), Is.EqualTo(expected));
            Assert.That(coordinator.State, Is.Not.SameAs(coordinator.State), "Public State keeps fresh DTO semantics.");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes), "Projection is not a save/migration command.");
        }

        [Test] public void ActualAutoKeepsFullProjectionChoicesAndPersistsEveryExistingCommand098()
        {
            var coordinator = Actual098(out var path);
            var originalFixture = File.ReadAllBytes(Fixture098());
            var before = coordinator.State.Battle;
            var expected = before.PlayerUnions.Where(value => value.CanAct).ToDictionary(
                value => value.UnionId, value => M2BattleAutoOrders091.Choose(before, value.UnionId).ForecastId);
            var notified = 0;
            coordinator.Changed += () => notified++;
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure), Is.True, failure);
            Assert.That(notified, Is.EqualTo(expected.Count), "Every selection still persists and notifies; no batching.");
            var narrow = M2BattleViewAccess098.Read(coordinator);
            Assert.That(JsonConvert.SerializeObject(narrow), Is.EqualTo(JsonConvert.SerializeObject(coordinator.State.Battle)));
            foreach (var union in narrow.PlayerUnions.Where(value => value.CanAct))
                Assert.That(union.SelectedForecastId, Is.EqualTo(expected[union.UnionId]));
            Assert.That(narrow.Round, Is.EqualTo(before.Round), "Reading/selecting cannot resolve the round.");
            var saved = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(saved.IsSuccess, Is.True, string.Join(";", saved.Errors));
            Assert.That(saved.Value.CanonicalStateHash, Is.EqualTo(coordinator.State.CanonicalStateHash));
            Assert.That(saved.Value.CampaignState.Battle.Selections.Count, Is.EqualTo(expected.Count));
            var bytes = File.ReadAllBytes(path);
            Assert.That(coordinator.SelectForecast(narrow.PlayerUnions[0].UnionId, "UNCOMMITTED_098").Succeeded, Is.False);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            Assert.That(File.ReadAllBytes(Fixture098()), Is.EqualTo(originalFixture));
        }

        [Test] public void OptionalReaderAutoNeverRequestsUnrelatedWholeState098()
        {
            var coordinator = new Narrow098 { ThrowOnState = true };
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure), Is.True, failure);
            Assert.That(coordinator.StateReads, Is.Zero);
            Assert.That(coordinator.BattleReads, Is.GreaterThan(0));
            Assert.That(coordinator.SelectCalls, Is.EqualTo(1));
            Assert.That(coordinator.ConfirmCalls, Is.Zero);
        }

        [Test] public void AlternateCoordinatorRetainsFreshStateFallbackAndNullHandling098()
        {
            var coordinator = new Legacy098();
            Assert.That(M2BattleViewAccess098.Read(coordinator), Is.SameAs(coordinator.Battle));
            Assert.That(coordinator.StateReads, Is.EqualTo(1));
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure), Is.True, failure);
            Assert.That(coordinator.StateReads, Is.GreaterThan(1));
            Assert.That(coordinator.SelectCalls, Is.EqualTo(1));
            Assert.That(M2BattleViewAccess098.Read(null), Is.Null);
        }

        [Test] public void LiveControllerAndFlowChangedProjectAndRefreshOnlyOneOwner098()
        {
            var coordinator = Bind098();
            coordinator.Raise();
            Assert.That(coordinator.BattleReads, Is.EqualTo(1));
            Assert.That(coordinator.StateReads, Is.Zero);
            Assert.That(_flowConsumed, Is.True);
            Assert.That(_controller.OwnsCoordinatorNotifications098(coordinator), Is.True);
            Assert.That(Field098(_controller, "_pendingView"), Is.SameAs(coordinator.Battle));
        }

        [Test] public void DisabledControllerKeepsExactlyOneFlowFallback098()
        {
            var coordinator = Bind098();
            _controller.enabled = false;
            coordinator.Raise();
            Assert.That(_controller.OwnsCoordinatorNotifications098(coordinator), Is.False);
            Assert.That(_flowConsumed, Is.True);
            Assert.That(coordinator.BattleReads, Is.EqualTo(1), "Disabled .NET event receiver is gated; Flow fallback owns refresh.");
            Assert.That(coordinator.StateReads, Is.Zero);
        }

        [Test] public void HiddenControllerDoesNotConsumeOtherScreenNavigationOrProjectBattle098()
        {
            var coordinator = Bind098();
            _controller.SetVisible(false);
            coordinator.Raise();
            Assert.That(_flowConsumed, Is.False, "Actual Flow handler can continue its ordinary nonbattle screen branch.");
            Assert.That(coordinator.BattleReads, Is.Zero);
            Assert.That(coordinator.StateReads, Is.Zero);
        }

        [Test] public void HiddenAncestorRetainsExistingFlowFallbackAndPendingView098()
        {
            var coordinator = Bind098();
            _host.SetActive(false);
            coordinator.Raise();
            Assert.That(_controller.IsActive, Is.True, "The historical Flow gate is activeSelf, not hierarchy visibility.");
            Assert.That(_controller.OwnsCoordinatorNotifications098(coordinator), Is.False);
            Assert.That(_flowConsumed, Is.True, "Do not introduce normal-screen rebuild/re-entry beneath a hidden parent.");
            Assert.That(coordinator.BattleReads, Is.EqualTo(1));
            Assert.That(coordinator.StateReads, Is.Zero);
            Assert.That(Field098(_controller, "_pendingView"), Is.SameAs(coordinator.Battle));
        }

        [TestCase("_resolving")]
        [TestCase("_claiming")]
        public void ActiveOwnerKeepsPendingTerminalUpdatesDuringPlaybackAndClaim098(string field)
        {
            var coordinator = Bind098();
            Set098(_controller, field, true);
            coordinator.Battle.IsResolved = true;
            coordinator.Battle.Outcome = "Victory";
            coordinator.Raise();
            Assert.That(coordinator.BattleReads, Is.EqualTo(1));
            Assert.That(coordinator.StateReads, Is.Zero);
            Assert.That(_flowConsumed, Is.True);
            Assert.That(Field098(_controller, "_pendingView"), Is.SameAs(coordinator.Battle));
            Assert.That(coordinator.ClaimCalls, Is.Zero, "Notification never claims or skips staged results.");
        }

        [Test] public void ReinitializingRebindingAndCleanupHandlerManageExactlyOneSubscription098()
        {
            _owner = new GameObject("Subscription owner098");
            _host = new GameObject("Subscription host098", typeof(RectTransform));
            _controller = _owner.AddComponent<M2BattleExperienceController072>();
            var first = new Narrow098();
            var second = new Narrow098();
            _controller.Initialize(first, _host.GetComponent<RectTransform>());
            _controller.Initialize(first, _host.GetComponent<RectTransform>());
            Assert.That(first.Subscribers, Is.EqualTo(1));
            _controller.Initialize(second, _host.GetComponent<RectTransform>());
            Assert.That(first.Subscribers, Is.Zero);
            Assert.That(second.Subscribers, Is.EqualTo(1));
            Assert.That(_controller.OwnsCoordinatorNotifications098(first), Is.False);
            Assert.That(_controller.OwnsCoordinatorNotifications098(second), Is.True);
            // This synchronous EditMode fixture does not dispatch the runtime
            // MonoBehaviour lifecycle. Actual Unity destruction is covered by
            // BattleReadSubscription098PlayModeTests without manual invocation.
            var cleanup = typeof(M2BattleExperienceController072).GetMethod("OnDestroy",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(cleanup, Is.Not.Null);
            cleanup.Invoke(_controller, null);
            Assert.That(second.Subscribers, Is.Zero);
            Assert.That(_controller.OwnsCoordinatorNotifications098(second), Is.False);
            var reads = second.BattleReads;
            second.Raise();
            Assert.That(second.BattleReads, Is.EqualTo(reads), "A stale publisher cannot refresh after cleanup.");
            cleanup.Invoke(_controller, null);
            Assert.That(second.Subscribers, Is.Zero, "Cleanup remains safe when invoked twice.");
            UnityEngine.Object.DestroyImmediate(_controller);
        }

        Narrow098 Bind098()
        {
            _owner = new GameObject("Battle readers098");
            _host = new GameObject("Battle readers host098", typeof(RectTransform));
            _controller = _owner.AddComponent<M2BattleExperienceController072>();
            _flow = _owner.AddComponent<M1FlowPresenter>();
            var coordinator = new Narrow098 { ThrowOnState = true };
            Set098(_flow, "_coordinator", coordinator);
            Set098(_flow, "_battleExperience072", _controller);
            // Production subscribes Flow first; exercise that order explicitly.
            coordinator.Changed += () => _flowConsumed = (bool)typeof(M1FlowPresenter)
                .GetMethod("RefreshFirstHourBattleExperience072", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_flow, null);
            _controller.Initialize(coordinator, _host.GetComponent<RectTransform>());
            coordinator.BattleReads = coordinator.StateReads = 0;
            return coordinator;
        }
        M1RuntimeCoordinator Actual098(out string path)
        {
            path = Path.Combine(_directory, "ActualOldPending.json");
            File.Copy(Fixture098(), path);
            return new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
        }
        static string Fixture098() => Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", "R108_OldPendingFloor001.json");
        static object Field098(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        static void Set098(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        class Legacy098 : IM2PresentationCoordinator
        {
            Action _changed;
            public event Action Changed { add { _changed += value; } remove { _changed -= value; } }
            public int Subscribers => _changed?.GetInvocationList().Length ?? 0;
            public void Raise() => _changed?.Invoke();
            public int StateReads, SelectCalls, ConfirmCalls, ClaimCalls;
            public bool ThrowOnState;
            public M2BattleView Battle = new M2BattleView { BattleId = "BATTLE_READ098", Round = 1,
                Outcome = "In Progress", Objective = "Read committed battle",
                PlayerUnions = new[] { new M2BattleUnionView { UnionId = "ALLY", DisplayName = "Read ally", CanAct = true, CurrentAp = 10, MaximumAp = 10,
                    Members = new[] { new M2BattleMemberView { MemberId = "ACTOR", DisplayName = "Read actor", CurrentHp = 100, MaximumHp = 100, CurrentMp = 30, MaximumMp = 30 } } } },
                EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY", DisplayName = "Read enemy", CanAct = true,
                    Members = new[] { new M2BattleMemberView { MemberId = "FOE", DisplayName = "Read foe", CurrentHp = 100, MaximumHp = 100 } } } },
                Forecasts = new[] { new M2ForecastView { ForecastId = "FORECAST_READ098", UnionId = "ALLY", CommandId = "CMD_BALANCED", CommandName = "Attack",
                    TargetId = "ENEMY", TargetName = "Read enemy", MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "ACTOR", ArtId = "ART_BASIC_SABER_CUT", ArtName = "Saber Cut",
                        ActionKind = "Martial", TargetUnionId = "ENEMY", TargetMemberId = "FOE", PredictedHpDelta097 = -10 } } } } };
            public M1PresentationState State
            { get { StateReads++; if (ThrowOnState) throw new InvalidOperationException("Unrelated whole-state projection requested."); return new M1PresentationState { Battle = Battle }; } }
            public M1CommandResult SelectForecast(string unionId, string forecastId)
            {
                SelectCalls++;
                var forecast = Battle.Forecasts.Single(value => value.UnionId == unionId && value.ForecastId == forecastId);
                var union = Battle.PlayerUnions.Single(value => value.UnionId == unionId);
                union.SelectedForecastId = forecast.ForecastId; union.IsSelected = true; Battle.CanConfirmRound = true;
                Raise(); return M1CommandResult.Success();
            }
            public M1CommandResult ConfirmBattleRound() { ConfirmCalls++; throw new NotSupportedException(); }
            public M1CommandResult ClaimBattleRewards() { ClaimCalls++; throw new NotSupportedException(); }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => throw new NotSupportedException();
            public M1CommandResult SignRecruit(string id) => throw new NotSupportedException();
            public M1CommandResult EquipItem(string recruit, string slot, string item) => throw new NotSupportedException();
            public M1CommandResult UnequipItem(string recruit, string slot) => throw new NotSupportedException();
            public M1CommandResult SetEquipmentLock(string recruit, string slot, bool locked) => throw new NotSupportedException();
            public M1CommandResult CompleteEquipmentReview() => throw new NotSupportedException();
            public M1CommandResult AddUnion() => throw new NotSupportedException();
            public M1CommandResult RemoveUnion(int index) => throw new NotSupportedException();
            public M1CommandResult AssignRecruitToUnion(string recruit, int union, int slot) => throw new NotSupportedException();
            public M1CommandResult UnassignRecruitFromUnion(string recruit) => throw new NotSupportedException();
            public M1CommandResult SetUnionLeader(int union, string recruit) => throw new NotSupportedException();
            public M1CommandResult SetFormation(int union, string formation) => throw new NotSupportedException();
            public M1CommandResult SetDoctrine(int union, string doctrine) => throw new NotSupportedException();
            public M1CommandResult SaveAndReloadProof() => throw new NotSupportedException();
            public M1CommandResult StartTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult ReplayTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult RetryTutorialBattle() => throw new NotSupportedException();
        }
        sealed class Narrow098 : Legacy098, IM2BattleViewReader098
        {
            public int BattleReads;
            public M2BattleView ReadBattleView098() { BattleReads++; return Battle; }
        }
    }
}
