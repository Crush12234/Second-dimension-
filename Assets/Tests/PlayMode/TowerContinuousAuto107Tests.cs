using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class TowerContinuousAuto107Tests
    {
        private GameObject _owner;
        private GameObject _host;
        private M2BattleExperienceController072 _controller;
        private TowerCoordinator107 _coordinator;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Tower continuous Auto owner107");
            _host = new GameObject("Tower continuous Auto host107", typeof(RectTransform));
            _coordinator = new TowerCoordinator107();
            _controller = _owner.AddComponent<M2BattleExperienceController072>();
            _controller.Initialize(_coordinator, _host.GetComponent<RectTransform>());
            _controller.ContinueTowerAuto107 = () => (M1CommandResult)typeof(M1FlowPresenter)
                .GetMethod("PrepareNextTowerAutoBattle107", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { _coordinator });
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (_owner != null) UnityEngine.Object.DestroyImmediate(_owner);
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
        }

        [Test]
        public void ConsecutiveVictoriesBankEachFloorBeforeEnteringNextWithoutResults()
        {
            var completed = 0;
            _controller.BattleCompleted += () => completed++;
            _controller.SetAutoOrders091(true);
            for (var floor = 3; floor <= 4; floor++)
            {
                _coordinator.Resolve("Victory");
                _controller.Refresh(_coordinator.Battle);
                Assert.That(_coordinator.Floor, Is.EqualTo(floor + 1));
                Assert.That(_coordinator.Battle.IsResolved, Is.False);
                Assert.That(_controller.AutoOrdersEnabled091, Is.True);
                Assert.That(ResultLayer().gameObject.activeSelf, Is.False);
                Assert.That(_controller.OwnedResultsView078.OutcomeTitleText079, Is.Empty,
                    "The terminal reward view must never be populated during a successful Auto transition.");
                Assert.That((bool)Field("_claiming").GetValue(_controller), Is.False);
            }
            Assert.That(_coordinator.Calls, Is.EqualTo(new[] {
                "claim", "commit battle", "apply battle", "bank floor", "begin floor", "prepare battle", "enter battle",
                "claim", "commit battle", "apply battle", "bank floor", "begin floor", "prepare battle", "enter battle"
            }));
            Assert.That(_coordinator.ClaimedFloors, Is.EquivalentTo(new[] { 3, 4 }));
            Assert.That(_coordinator.BankedFloors, Is.EquivalentTo(new[] { 3, 4 }));
            Assert.That(completed, Is.Zero, "Tower Auto keeps the battle surface instead of returning to the Guild.");
        }

        [TestCase("off")]
        [TestCase("other route")]
        [TestCase("defeat")]
        public void ExplicitStopOtherRoutesAndDefeatNeverClaimOrAdvance(string condition)
        {
            _controller.SetAutoOrders091(true);
            if (condition == "off") _controller.SetAutoOrders091(false);
            if (condition == "other route") _controller.ContinueTowerAuto107 = null;
            _coordinator.Resolve(condition == "defeat" ? "Defeat" : "Victory");
            _controller.Refresh(_coordinator.Battle);
            Assert.That(_coordinator.Calls, Is.Empty);
            Assert.That(ResultLayer().gameObject.activeSelf, Is.True);
            if (condition != "other route") Assert.That(_controller.AutoOrdersEnabled091, Is.False);
        }

        [TestCase("claim")]
        [TestCase("commit battle")]
        [TestCase("bank floor")]
        [TestCase("begin floor")]
        [TestCase("prepare battle")]
        [TestCase("enter battle")]
        public void FailedSavedCommandStopsAtThatBoundaryAndShowsManualRecovery(string command)
        {
            _coordinator.FailCommand = command;
            _controller.SetAutoOrders091(true);
            _coordinator.Resolve("Victory");
            _controller.Refresh(_coordinator.Battle);
            Assert.That(_coordinator.Calls.Last(), Is.EqualTo(command));
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
            Assert.That(ResultLayer().gameObject.activeSelf, Is.True);
            Assert.That(_controller.OwnedResultsView078.NextObjectiveText079, Does.Contain("could not save " + command));
            Assert.That(ContinueButton().interactable, Is.True);
            var calls = _coordinator.Calls.Count;
            _controller.Refresh(_coordinator.Battle);
            Assert.That(_coordinator.Calls.Count, Is.EqualTo(calls), "A failed Auto transition cannot retry itself every frame.");
            Assert.That((bool)Field("_claiming").GetValue(_controller), Is.False);
        }

        [Test]
        public void TurningOffDuringClaimNotificationPreventsNextFloorCommands()
        {
            _controller.SetAutoOrders091(true);
            _coordinator.AfterClaim = () => _controller.SetAutoOrders091(false);
            _coordinator.Resolve("Victory");
            _controller.Refresh(_coordinator.Battle);
            Assert.That(_coordinator.Calls, Is.EqualTo(new[] { "claim" }));
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
            Assert.That(ResultLayer().gameObject.activeSelf, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavedTerminalResultShowsClaimFailureAndCanRetry(bool throwInstead)
        {
            var completed = 0;
            _controller.BattleCompleted += () => completed++;
            _coordinator.Resolve("Victory");
            _controller.Refresh(_coordinator.Battle);
            _coordinator.FailCommand = "claim";
            _coordinator.ThrowOnClaim = throwInstead;
            ContinueButton().onClick.Invoke();
            Assert.That(_controller.OwnedResultsView078.NextObjectiveText079, Does.Contain("could not save claim"));
            Assert.That(ContinueButton().interactable, Is.True);
            Assert.That(ResultLayer().gameObject.activeSelf, Is.True);
            Assert.That((bool)Field("_claiming").GetValue(_controller), Is.False);
            Assert.That(completed, Is.Zero);
            _coordinator.FailCommand = null;
            _coordinator.ThrowOnClaim = false;
            ContinueButton().onClick.Invoke();
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(_coordinator.ClaimedFloors, Is.EqualTo(new[] { 3 }));
            Assert.That(_controller.IsActive, Is.False);
        }

        [Test]
        public void AutoClaimExceptionStopsAndReleasesReentrancyGuard()
        {
            _coordinator.ThrowOnClaim = true;
            _controller.SetAutoOrders091(true);
            _coordinator.Resolve("Victory");
            Assert.DoesNotThrow(() => _controller.Refresh(_coordinator.Battle));
            Assert.That(_controller.AutoOrdersEnabled091, Is.False);
            Assert.That((bool)Field("_claiming").GetValue(_controller), Is.False);
            Assert.That(ContinueButton().interactable, Is.True);
            Assert.That(_coordinator.Calls, Is.EqualTo(new[] { "claim" }));
        }

        private RectTransform ResultLayer() => (RectTransform)Field("_resultLayer").GetValue(_controller);
        private Button ContinueButton() => ResultLayer().GetComponentsInChildren<Button>(true)
            .Single(value => value.name == "Continue From Battle Results 072");
        private static FieldInfo Field(string name) => typeof(M2BattleExperienceController072)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        // This fixture enforces independent persistence boundaries: a battle reward
        // must be claimed, then its floor banked, before the next floor may begin.
        private sealed class TowerCoordinator107 : IM2PresentationCoordinator,
            IM2BattleViewReader098, ICampaignProgressionPresentationCoordinator022
        {
            public event Action Changed;
            public readonly List<string> Calls = new List<string>();
            public readonly List<int> ClaimedFloors = new List<int>();
            public readonly List<int> BankedFloors = new List<int>();
            public string FailCommand;
            public bool ThrowOnClaim;
            public Action AfterClaim;
            public int Floor = 3;
            private int _step;
            public M2BattleView Battle = LiveBattle(3);
            public M1PresentationState State => new M1PresentationState { Battle = Battle };
            public M2BattleView ReadBattleView098() => Battle;
            public CampaignProgressionPresentationState022 CampaignProgression022 { get; private set; } = Active(3);

            public void Resolve(string outcome)
            {
                Battle.IsResolved = true;
                Battle.Outcome = outcome;
                Battle.Reward = new M2BattleRewardView { RewardId = "REWARD107_" + Floor, Outcome = outcome, CanClaim = true };
                CampaignProgression022.TowerBattleInProgress = false;
                CampaignProgression022.TowerBattleResolved = true;
                CampaignProgression022.TowerBattleWon = outcome == "Victory";
                CampaignProgression022.TowerBattleRewardAwaitingClaim = outcome == "Victory";
                _step = 0;
            }

            public M1CommandResult ClaimBattleRewards()
            {
                var result = Command("claim");
                if (ThrowOnClaim) throw new InvalidOperationException("could not save claim");
                if (!result.Succeeded) return result;
                if (!ClaimedFloors.Contains(Floor)) ClaimedFloors.Add(Floor);
                Battle.Reward.Claimed = true;
                CampaignProgression022.TowerBattleRewardAwaitingClaim = false;
                Changed?.Invoke();
                AfterClaim?.Invoke();
                return result;
            }

            public M1CommandResult AdvanceTowerRun081()
            {
                var command = _step == 0 ? "commit battle" : _step == 1 ? "apply battle" :
                    _step == 2 ? "bank floor" : "prepare battle";
                var result = Command(command);
                if (!result.Succeeded) return result;
                if (_step <= 2) Assert.That(ClaimedFloors, Does.Contain(Floor));
                if (_step == 0) CampaignProgression022.HasPendingAbyssBattleReceipt = true;
                else if (_step == 1) CampaignProgression022.ActiveAbyssStatus = "ReadyToFinalize";
                else if (_step == 2)
                {
                    Assert.That(BankedFloors.Contains(Floor), Is.False);
                    BankedFloors.Add(Floor);
                    CampaignProgression022 = new CampaignProgressionPresentationState022 { IsAvailable = true };
                }
                else CampaignProgression022.ActiveStepRequiresBattle = true;
                _step++;
                Changed?.Invoke();
                return result;
            }

            public M1CommandResult BeginTowerRun081()
            {
                var result = Command("begin floor");
                if (!result.Succeeded) return result;
                Assert.That(BankedFloors, Does.Contain(Floor));
                Assert.That(CampaignProgression022.ActiveAbyssOperationId, Is.Null.Or.Empty);
                Floor++;
                CampaignProgression022 = Active(Floor);
                CampaignProgression022.ActiveAbyssStatus = "Active";
                CampaignProgression022.TowerBattleInProgress = false;
                Changed?.Invoke();
                return result;
            }

            public M1CommandResult EnterAbyssBattle022()
            {
                var result = Command("enter battle");
                if (!result.Succeeded) return result;
                Assert.That(CampaignProgression022.ActiveStepRequiresBattle, Is.True);
                Battle = LiveBattle(Floor);
                CampaignProgression022.ActiveAbyssStatus = "AwaitingBattle";
                CampaignProgression022.TowerBattleInProgress = true;
                Changed?.Invoke();
                return result;
            }

            private M1CommandResult Command(string command)
            {
                Calls.Add(command);
                return FailCommand == command ? M1CommandResult.Failure("could not save " + command) : M1CommandResult.Success();
            }

            private static CampaignProgressionPresentationState022 Active(int floor) =>
                new CampaignProgressionPresentationState022 {
                    IsAvailable = true, ActiveAbyssOperationId = "TOWER107_" + floor,
                    ActiveAbyssStatus = "AwaitingBattle", TowerBattleInProgress = true, TowerFloorNumber = floor
                };

            private static M2BattleView LiveBattle(int floor) => new M2BattleView {
                BattleId = "ABYSS_BATTLE107_" + floor, Outcome = "InProgress", Round = 1,
                EnemyUnions = new[] { new M2BattleUnionView {
                    UnionId = "ENEMY107", DisplayName = "Tower Guardian", Members = new[] {
                        new M2BattleMemberView { MemberId = "FOE107", DisplayName = "Guardian", CurrentHp = 100, MaximumHp = 100 }
                    }
                } }
            };

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
            public M1CommandResult SelectForecast(string union, string forecast) => throw new NotSupportedException();
            public M1CommandResult ConfirmBattleRound() => throw new NotSupportedException();
            public M1CommandResult ReplayTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult RetryTutorialBattle() => throw new NotSupportedException();
            public M1CommandResult RecordEquipmentUse022(string item, string track) => throw new NotSupportedException();
            public M1CommandResult EvolveWeapon022(string item, string recipe) => throw new NotSupportedException();
            public M1CommandResult CertifyAdvancedClass022(string recruit, string id) => throw new NotSupportedException();
            public M1CommandResult BeginAbyssOperation022(string id) => throw new NotSupportedException();
            public M1CommandResult RetreatTowerRun081() => throw new NotSupportedException();
            public M1CommandResult RecoverInactiveLegacyTower084() => throw new NotSupportedException();
            public M1CommandResult CommitAbyssStep022() => throw new NotSupportedException();
            public M1CommandResult ApplyAbyssStep022() => throw new NotSupportedException();
            public M1CommandResult CommitAbyssBattleResult022() => throw new NotSupportedException();
            public M1CommandResult FinalizeAbyssOperation022() => throw new NotSupportedException();
            public M1CommandResult FinalizeAbyssBattle022() => throw new NotSupportedException();
            public M1CommandResult CraftInvocationArtifact022(string id) => throw new NotSupportedException();
            public M1CommandResult CraftInvocationArtifact022(string id, IReadOnlyList<string> affixes) => throw new NotSupportedException();
            public M1CommandResult EvolveInvocationArtifact022(string id) => throw new NotSupportedException();
            public M1CommandResult InvokeEligibleEchoForecast022() => throw new NotSupportedException();
            public M1CommandResult InvokeAcceptedCovenantForecast022(string id) => throw new NotSupportedException();
            public M1CommandResult AdvanceCovenant022(string id) => throw new NotSupportedException();
            public M1CommandResult AcceptCovenant022(string id) => throw new NotSupportedException();
        }
    }
}
