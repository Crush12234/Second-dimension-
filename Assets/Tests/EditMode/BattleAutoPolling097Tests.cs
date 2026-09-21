using System;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleAutoPolling097Tests
    {
        private GameObject _host;
        private GameObject _parent;
        private M2BattleExperienceController072 _controller;
        private CountingCoordinator097 _coordinator;

        [SetUp]
        public void Setup097()
        {
            _parent = new GameObject("Polling Parent 097", typeof(RectTransform));
            _host = new GameObject("Polling Controller 097", typeof(RectTransform));
            _host.transform.SetParent(_parent.transform, false);
            _controller = _host.AddComponent<M2BattleExperienceController072>();
            _coordinator = new CountingCoordinator097();
            Set097("_coordinator", _coordinator);
            Set097("_root", _host.GetComponent<RectTransform>());
            Set097("_autoOrders091", true);
            Set097("_autoBattleId091", "POLL097");
            Set097("_nextAutoOrderAt091", -1f);
        }

        [TearDown]
        public void Cleanup097()
        {
            if (_parent != null) UnityEngine.Object.DestroyImmediate(_parent);
        }

        [TestCase("_resolving")]
        [TestCase("_claiming")]
        [TestCase("_autoSelecting091")]
        public void BlockedPlaybackFramesPerformZeroStateProjections097(string flag)
        {
            Set097(flag, true);
            for (var frame = 0; frame < 120; frame++) Update097();
            Assert.That(_coordinator.StateReads097, Is.Zero);
            Assert.That(_coordinator.SelectCalls097, Is.Zero);
            Assert.That(_controller.AutoOrdersEnabled091, Is.True,
                "A blocked animation frame does not cancel committed playback or future Auto.");
        }

        [Test]
        public void AutoDelayPerformsZeroStateProjections097()
        {
            Set097("_nextAutoOrderAt091", Time.unscaledTime + 60f);
            for (var frame = 0; frame < 120; frame++) Update097();
            Assert.That(_coordinator.StateReads097, Is.Zero);
            Assert.That(_coordinator.SelectCalls097, Is.Zero);
        }

        [TestCase("root_missing")]
        [TestCase("inactive_self")]
        [TestCase("inactive_parent")]
        [TestCase("coordinator_missing")]
        public void InactiveControllerTurnsOffWithoutProjecting097(string condition)
        {
            if (condition == "root_missing") Set097("_root", null);
            else if (condition == "inactive_self") _host.SetActive(false);
            else if (condition == "inactive_parent") _parent.SetActive(false);
            else Set097("_coordinator", null);
            Update097();
            Assert.That(_coordinator.StateReads097, Is.Zero);
            Assert.That(_coordinator.SelectCalls097, Is.Zero);
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
        }

        [Test]
        public void TurningAutoOffNeverQueriesCoordinator097()
        {
            _coordinator.ThrowOnState097 = true;
            Assert.DoesNotThrow(() => _controller.SetAutoOrders091(false));
            Assert.That(_coordinator.StateReads097, Is.Zero);
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
        }

        [TestCase("terminal")]
        [TestCase("other_battle")]
        [TestCase("no_opponents")]
        public void ReadyInputStillValidatesFreshBattleAndStops097(string condition)
        {
            if (condition == "terminal") _coordinator.View097.Battle.IsResolved = true;
            else if (condition == "other_battle") _coordinator.View097.Battle.BattleId = "REPLACED097";
            else _coordinator.View097.Battle.EnemyUnions[0].Members[0].CurrentHp = 0;
            Update097();
            Assert.That(_coordinator.StateReads097, Is.EqualTo(1));
            Assert.That(_coordinator.SelectCalls097, Is.Zero);
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
        }

        [Test]
        public void RejectedSelectionReleasesFlagAndPausesWithoutConfirming097()
        {
            Update097();
            Assert.That(_coordinator.SelectCalls097, Is.EqualTo(1));
            Assert.That(_coordinator.ConfirmCalls097, Is.Zero);
            Assert.That((bool)Field097("_autoSelecting091").GetValue(_controller), Is.False);
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
        }

        [Test]
        public void SelectionExceptionAlwaysReleasesLocalReentrancyFlag097()
        {
            _coordinator.ThrowOnSelect097 = true;
            var error = Assert.Throws<TargetInvocationException>(() => Update097());
            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(_coordinator.SelectCalls097, Is.EqualTo(1));
            Assert.That((bool)Field097("_autoSelecting091").GetValue(_controller), Is.False);
            Assert.That(_coordinator.ConfirmCalls097, Is.Zero);
        }

        private void Update097() => typeof(M2BattleExperienceController072)
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_controller, null);
        private void Set097(string name, object value) => Field097(name).SetValue(_controller, value);
        private static FieldInfo Field097(string name) => typeof(M2BattleExperienceController072)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private sealed class CountingCoordinator097 : IM2PresentationCoordinator
        {
            public event Action Changed { add { } remove { } }
            public int StateReads097;
            public int SelectCalls097;
            public int ConfirmCalls097;
            public bool ThrowOnState097;
            public bool ThrowOnSelect097;
            public M1PresentationState View097 = new M1PresentationState
            {
                Battle = new M2BattleView
                {
                    BattleId = "POLL097", Round = 1,
                    PlayerUnions = new[] { new M2BattleUnionView
                    {
                        UnionId = "ALLY", CanAct = true, CurrentAp = 10,
                        Members = new[] { new M2BattleMemberView { MemberId = "ACTOR", CurrentHp = 100, MaximumHp = 100, CurrentMp = 30 } }
                    } },
                    EnemyUnions = new[] { new M2BattleUnionView
                    {
                        UnionId = "ENEMY", CanAct = true,
                        Members = new[] { new M2BattleMemberView { MemberId = "FOE", CurrentHp = 100, MaximumHp = 100 } }
                    } },
                    Forecasts = new[] { new M2ForecastView
                    {
                        ForecastId = "FORECAST097", UnionId = "ALLY", CommandId = "CMD_BALANCED",
                        MemberActions = new[] { new M2PredictedActionView
                        {
                            ActorMemberId = "ACTOR", ActionKind = "Martial", TargetUnionId = "ENEMY", TargetMemberId = "FOE"
                        } }
                    } }
                }
            };
            public M1PresentationState State
            {
                get
                {
                    StateReads097++;
                    if (ThrowOnState097) throw new InvalidOperationException("Unexpected expensive state query.");
                    return View097;
                }
            }
            public M1CommandResult SelectForecast(string unionId, string forecastId)
            {
                SelectCalls097++;
                if (ThrowOnSelect097) throw new InvalidOperationException("Synchronous selection failure for flag cleanup.");
                return M1CommandResult.Failure("Counting fixture does not commit gameplay.");
            }
            public M1CommandResult ConfirmBattleRound() { ConfirmCalls097++; throw new NotSupportedException(); }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => throw new NotSupportedException();
            public M1CommandResult SignRecruit(string recruitId) => throw new NotSupportedException();
            public M1CommandResult EquipItem(string recruitId, string slotId, string itemId) => throw new NotSupportedException();
            public M1CommandResult UnequipItem(string recruitId, string slotId) => throw new NotSupportedException();
            public M1CommandResult SetEquipmentLock(string recruitId, string slotId, bool locked) => throw new NotSupportedException();
            public M1CommandResult CompleteEquipmentReview() => throw new NotSupportedException();
            public M1CommandResult AddUnion() => throw new NotSupportedException();
            public M1CommandResult RemoveUnion(int unionIndex) => throw new NotSupportedException();
            public M1CommandResult AssignRecruitToUnion(string recruitId, int unionIndex, int slotIndex) => throw new NotSupportedException();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId) => throw new NotSupportedException();
            public M1CommandResult SetUnionLeader(int unionIndex, string recruitId) => throw new NotSupportedException();
            public M1CommandResult SetFormation(int unionIndex, string formationId) => throw new NotSupportedException();
            public M1CommandResult SetDoctrine(int unionIndex, string doctrineId) => throw new NotSupportedException();
            public M1CommandResult SaveAndReloadProof() => throw new NotSupportedException();
            public M1CommandResult StartTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult ReplayTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult RetryTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult ClaimBattleRewards() => throw new NotSupportedException();
        }
    }
}
