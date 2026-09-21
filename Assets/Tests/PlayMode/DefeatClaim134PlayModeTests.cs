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
    // Real rendered results buttons; controllable authority isolates failure,
    // re-entrant notification, retry and the deliberate Tower restart boundary.
    public sealed class DefeatClaim134PlayModeTests
    {
        GameObject _host;
        M2BattleExperienceController072 _controller;
        DefeatOwner134 _owner;
        int _completed;

        [UnityTearDown] public IEnumerator Cleanup134()
        { if (_host != null) UnityEngine.Object.Destroy(_host); yield return null; }

        [UnityTest]
        public IEnumerator ObjectiveDefeatSaveFailureRetainsResultAndRetryClaimsBeforeLeaving134()
        {
            Create134(false, true);
            yield return Ready134();
            _owner.FailClaim = true;
            Click134();
            Assert.That(_owner.Claims, Is.EqualTo(1));
            Assert.That(_owner.Battle.Reward.Claimed, Is.False);
            Assert.That(_controller.IsActive, Is.True);
            Assert.That(_completed, Is.Zero);
            Assert.That(_controller.OwnedResultsView078.NextObjectiveText079, Does.Contain("isolated save rejected"));
            Assert.That(Continue134().IsInteractable(), Is.True);
            _owner.FailClaim = false;
            // A synchronous Changed observer attempts a duplicate callback while
            // the real claim is still executing. It must not claim or navigate.
            _owner.DuringClaim = () => InvokeClaim134();
            Click134();
            Assert.That(_owner.Claims, Is.EqualTo(2));
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_owner.Battle.Reward.Claimed, Is.True);
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
            InvokeClaim134();
            Assert.That(_owner.Claims, Is.EqualTo(2), "Hidden stale result callbacks cannot repeat completion.");
            Assert.That(_completed, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CampaignFullPartyDefeatClaimsBeforeReturning134()
        {
            Create134(false, false);
            yield return Ready134(); Click134();
            Assert.That(_owner.Claims, Is.EqualTo(1));
            Assert.That(_owner.Commits, Is.EqualTo(1));
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
        }

        [UnityTest]
        public IEnumerator EligibleTowerWipeKeepsOriginalDefeatForDeliberateRestart134()
        {
            Create134(true, false);
            yield return Ready134(); Click134();
            Assert.That(_owner.Claims, Is.Zero, "Restart or Retreat, not generic battle return, owns this claim.");
            Assert.That(_owner.Battle.Reward.Claimed, Is.False);
            Assert.That(_owner.CanRestartTowerAfterPartyDefeat130, Is.True);
            Assert.That(_completed, Is.EqualTo(1));
            Assert.That(_controller.IsActive, Is.False);
        }

        void Create134(bool towerWipe, bool survivor)
        {
            _completed = 0;
            _host = new GameObject("Defeat claim results 134");
            _owner = new DefeatOwner134(towerWipe, survivor);
            _controller = _host.AddComponent<M2BattleExperienceController072>();
            _controller.Initialize(_owner);
            Field134<Canvas>("_ownedCanvas").sortingOrder = 30000;
            _controller.BattleCompleted += () => {
                Assert.That(_owner.Battle.Reward.Claimed || _owner.CanRestartTowerAfterPartyDefeat130, Is.True);
                _completed++;
            };
            _controller.ReducedMotion = true;
            Assert.That(_controller.EnterCurrentBattle(), Is.True);
        }
        IEnumerator Ready134()
        {
            var deadline = Time.realtimeSinceStartup + 8f;
            while (!Continue134().IsInteractable() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Continue134().IsInteractable(), Is.True);
            yield return null;
        }
        T Field134<T>(string name) => (T)typeof(M2BattleExperienceController072)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_controller);
        Button Continue134() => Field134<RectTransform>("_resultLayer").GetComponentsInChildren<Button>(true)
            .Single(button => button.name == "Continue From Battle Results 072");
        void InvokeClaim134() => typeof(M2BattleExperienceController072)
            .GetMethod("ClaimAndComplete", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_controller, null);
        void Click134()
        {
            var button = Continue134(); Assert.That(button.IsInteractable(), Is.True);
            Canvas.ForceUpdateCanvases();
            var events = EventSystem.current; Assert.That(events, Is.Not.Null);
            var canvas = button.GetComponentInParent<Canvas>(); var rect = (RectTransform)button.transform;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(events) { pointerId = -1, button = PointerEventData.InputButton.Left,
                position = point, pressPosition = point, eligibleForClick = true, clickCount = 1 };
            var hits = new List<RaycastResult>(); events.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            var hit = hits[0].gameObject;
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit), Is.EqualTo(button.gameObject));
            pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
            pointer.pointerPress = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(pointer.pointerPress, pointer, ExecuteEvents.pointerClickHandler);
        }

        sealed class DefeatOwner134 : IM2PresentationCoordinator, IM2BattleViewReader098, ITowerRestartAsyncTransition130
        {
            readonly bool _towerWipe;
            public int Claims, Commits;
            public bool FailClaim;
            public Action DuringClaim;
            public event Action Changed;
            public M2BattleView Battle;
            public DefeatOwner134(bool towerWipe, bool survivor)
            {
                _towerWipe = towerWipe;
                Battle = new M2BattleView {
                    BattleId = towerWipe ? "ABYSS_BATTLE022_FLOOR_05_TEST134" : "OBJECTIVE_DEFEAT134",
                    Outcome = "Defeat", IsResolved = true, Round = 4, LastResolvedRound = 4,
                    Objective = "Stop the breach before its deadline",
                    Reward = new M2BattleRewardView { RewardId = "DEFEAT_REWARD134", Outcome = "Defeat", CanClaim = true },
                    PlayerUnions = new[] { new M2BattleUnionView { UnionId = "PLAYER134", DisplayName = "Company", Members = new[] {
                        new M2BattleMemberView { MemberId = "ALLY134", DisplayName = "Surviving adventurer", MaximumHp = 100,
                            CurrentHp = survivor ? 80 : 0, Downed = !survivor } } } },
                    EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY134", DisplayName = "Guardian", Members = new[] {
                        new M2BattleMemberView { MemberId = "FOE134", DisplayName = "Guardian", MaximumHp = 300, CurrentHp = 208 } } } }
                };
            }
            public M1PresentationState State => new M1PresentationState { HasCampaign = true, Battle = Battle };
            public M2BattleView ReadBattleView098() => Battle;
            public bool CanRestartTowerAfterPartyDefeat130 => _towerWipe;
            public Task<M1CommandResult> PendingTowerRestart130 => null;
            public bool TowerRestartIsSaving130 => false;
            public void CancelTowerRestartBeforeSave130() { }
            public Task<M1CommandResult> RestartTowerFromFloorOneAsync130() => throw new NotSupportedException();
            public M1CommandResult ClaimBattleRewards()
            {
                Claims++;
                if (FailClaim) return M1CommandResult.Failure("The isolated save rejected the claim.");
                if (!Battle.Reward.Claimed) { Commits++; Battle.Reward.Claimed = true; Battle.Reward.CanClaim = false; }
                Changed?.Invoke(); DuringClaim?.Invoke();
                return M1CommandResult.Success("Defeat reward and return saved.");
            }
            static M1CommandResult Unexpected134() => throw new NotSupportedException();
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected134();
            public M1CommandResult SignRecruit(string recruit) => Unexpected134();
            public M1CommandResult EquipItem(string recruit, string slot, string item) => Unexpected134();
            public M1CommandResult UnequipItem(string recruit, string slot) => Unexpected134();
            public M1CommandResult SetEquipmentLock(string recruit, string slot, bool locked) => Unexpected134();
            public M1CommandResult CompleteEquipmentReview() => Unexpected134();
            public M1CommandResult AddUnion() => Unexpected134();
            public M1CommandResult RemoveUnion(int index) => Unexpected134();
            public M1CommandResult AssignRecruitToUnion(string recruit, int union, int slot) => Unexpected134();
            public M1CommandResult UnassignRecruitFromUnion(string recruit) => Unexpected134();
            public M1CommandResult SetUnionLeader(int union, string recruit) => Unexpected134();
            public M1CommandResult SetFormation(int union, string formation) => Unexpected134();
            public M1CommandResult SetDoctrine(int union, string doctrine) => Unexpected134();
            public M1CommandResult SaveAndReloadProof() => Unexpected134();
            public M1CommandResult StartTutorialBattle() => Unexpected134();
            public M1CommandResult SelectForecast(string union, string forecast) => Unexpected134();
            public M1CommandResult ConfirmBattleRound() => Unexpected134();
            public M1CommandResult ReplayTutorialBattle() => Unexpected134();
            public M1CommandResult RetryTutorialBattle() => Unexpected134();
        }
    }
}
