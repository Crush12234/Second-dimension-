using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class PresenterRenderReads110Tests
    {
        GameObject _owner, _canvasObject;
        M1FlowPresenter _flow;
        RectTransform _root;

        [SetUp]
        public void Setup110()
        {
            Assert.That(Application.isPlaying, Is.True, "The actual terminal view owns a reveal coroutine.");
            _owner = new GameObject("Presenter render reads 110");
            _flow = _owner.AddComponent<M1FlowPresenter>();
            _flow.enabled = false;
            _canvasObject = new GameObject("Render test canvas 110", typeof(RectTransform), typeof(Canvas));
            _root = new GameObject("Render test root 110", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(_canvasObject.transform, false);
            Set110(_flow, "_canvas", _canvasObject.GetComponent<Canvas>());
            Set110(_flow, "_screenRoot", _root);
        }

        [TearDown]
        public void Cleanup110()
        {
            // This synchronous fixture owns its canvas and cleans it up before another test runs.
            if (_flow != null) Set110(_flow, "_canvas", null);
            if (_owner != null) UnityEngine.Object.DestroyImmediate(_owner);
            if (_canvasObject != null) UnityEngine.Object.DestroyImmediate(_canvasObject);
        }

        [Test]
        public void ActualPageHeaderUsesOneWholeStateReadAndNextRenderIsFresh110()
        {
            var coordinator = new CountingCoordinator110 { PublishedState = State110(41, 100, 77) };
            Set110(_flow, "_coordinator", coordinator);
            Invoke110(_flow, "CreatePage", "READ TEST", "CURRENT GUILD", null);
            Assert.That(coordinator.StateReads, Is.EqualTo(1), "The four badge values share one render-local snapshot.");
            Assert.That(Text110("Always Visible Spendable XP 073"), Is.EqualTo("GUILD XP 41 / 100\nXP TO SPEND 77"));

            // Publish a replacement DTO, as the coordinator does after a committed change.
            coordinator.PublishedState = State110(82, 200, 155);
            ClearRoot110();
            Invoke110(_flow, "CreatePage", "READ TEST", "UPDATED GUILD", null);
            Assert.That(coordinator.StateReads, Is.EqualTo(2), "A later render must request a new snapshot.");
            Assert.That(Text110("Always Visible Spendable XP 073"), Is.EqualTo("GUILD XP 82 / 200\nXP TO SPEND 155"));
            Assert.That(coordinator.CommandCalls, Is.Zero);
        }

        [Test]
        public void ActualTowerSummaryUsesOneWholeStateReadAndNextRenderIsFresh110()
        {
            var coordinator = new CountingCoordinator110 { PublishedState = State110(41, 100, 77) };
            Set110(_flow, "_coordinator", coordinator);
            var tower = new CampaignProgressionPresentationState022
            {
                IsAvailable = true, TowerFloorNumber = 302, HighestClearedTowerFloor = 301
            };
            // Inactive Tower presentation has no transition to perform; no command callback is invoked.
            Invoke110(_flow, "BuildGuildCityAbyss022", _root, null, tower);
            Assert.That(coordinator.StateReads, Is.EqualTo(1), "Both Guild XP operands share the same local snapshot.");
            Assert.That(PanelText110("Tower Phone Simple Summary 084"), Does.Contain("GUILD XP  •  41 / 100"));

            coordinator.PublishedState = State110(82, 200, 155);
            ClearRoot110();
            Invoke110(_flow, "BuildGuildCityAbyss022", _root, null, tower);
            Assert.That(coordinator.StateReads, Is.EqualTo(2));
            Assert.That(PanelText110("Tower Phone Simple Summary 084"), Does.Contain("GUILD XP  •  82 / 200"));
            Assert.That(coordinator.CommandCalls, Is.Zero);
        }

        [TestCase("BuildBattle", false)]
        [TestCase("BuildBattleResults", true)]
        public void ActualShippingBattleEntryUsesOnlyBattleReadsAndNextRenderIsFresh110(string builder, bool resolved)
        {
            var coordinator = new NarrowCoordinator110
            {
                ThrowOnState = true, Battle = Battle110("FIRST", resolved)
            };
            Set110(_flow, "_coordinator", coordinator);
            var readsPerEntry110 = resolved ? 3 : 4;
            if (!resolved) ResetControllerSelection110();
            Invoke110(_flow, builder);
            var controller = (M2BattleExperienceController072)Field110(_flow, "_battleExperience072");
            Assert.That(controller, Is.Not.Null, "Exercise the shipping diorama/results owner, not the legacy training UI.");
            Assert.That(coordinator.StateReads, Is.Zero);
            Assert.That(coordinator.BattleReads, Is.EqualTo(readsPerEntry110), "Active HUD initial controller selection also reads its forecast preview.");
            Assert.That(Field110(controller, "_pendingView"), Is.SameAs(coordinator.Battle));
            Assert.That(((RectTransform)Field110(controller, "_resultLayer")).gameObject.activeSelf, Is.EqualTo(resolved));

            var first = coordinator.Battle;
            coordinator.Battle = Battle110("SECOND", resolved);
            if (!resolved) ResetControllerSelection110();
            Invoke110(_flow, builder);
            Assert.That(coordinator.StateReads, Is.Zero);
            Assert.That(coordinator.BattleReads, Is.EqualTo(2 * readsPerEntry110), "The next entry rereads the coordinator, including the same focus precondition; no cross-render cache.");
            Assert.That(Field110(controller, "_pendingView"), Is.SameAs(coordinator.Battle).And.Not.SameAs(first));
            Assert.That(coordinator.CommandCalls, Is.Zero, "Presentation must not select, resolve, or claim anything.");
        }

        void ResetControllerSelection110()
        {
            // Initial HUD focus selects its forecast and reads PreviewForecast.
            // Make that input precondition identical on both renders, independent
            // of the runner scene or the previous render's retained selection.
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var owner = new GameObject("Render read controller focus 110", typeof(EventSystem));
                owner.transform.SetParent(_owner.transform, false);
                eventSystem = owner.GetComponent<EventSystem>();
            }
            eventSystem.SetSelectedGameObject(null);
        }

        void ClearRoot110()
        {
            // RuntimeUi normally defers child destruction to the next player frame.
            // Clear explicitly between synchronous test renders without changing the presenter.
            for (var index = _root.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(_root.GetChild(index).gameObject);
        }

        string Text110(string name) => _root.GetComponentsInChildren<Text>(true).Single(value => value.name == name).text;
        string PanelText110(string name) => string.Join("\n", _root.GetComponentsInChildren<Transform>(true)
            .Single(value => value.name == name).GetComponentsInChildren<Text>(true).Select(value => value.text));
        static object Field110(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        static void Set110(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        static object Invoke110(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        static M1PresentationState State110(long progress, long required, long treasury) => new M1PresentationState
        {
            HasCampaign = true, GuildXpIntoCurrentLevel = progress, GuildXpRequiredForNextLevel = required, TreasuryXp = treasury
        };

        // Synthetic read-only DTO fixture, following BattleReadProjection098Tests' one-Union shape.
        // It is never written to disk and provides no gameplay command implementation.
        static M2BattleView Battle110(string suffix, bool resolved) => new M2BattleView
        {
            BattleId = "BATTLE_RENDER_READ_110_" + suffix, Round = 1,
            IsResolved = resolved, Outcome = resolved ? "Victory" : "In Progress", Objective = "Read " + suffix,
            PlayerUnions = new[] { new M2BattleUnionView { UnionId = "ALLY", DisplayName = "Read ally", CanAct = true, CurrentAp = 10, MaximumAp = 10,
                Members = new[] { new M2BattleMemberView { MemberId = "ACTOR", DisplayName = "Read actor", CurrentHp = 100, MaximumHp = 100, CurrentMp = 30, MaximumMp = 30 } } } },
            EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY", DisplayName = "Read enemy", CanAct = true,
                Members = new[] { new M2BattleMemberView { MemberId = "FOE", DisplayName = "Read foe", CurrentHp = 100, MaximumHp = 100 } } } },
            Forecasts = new[] { new M2ForecastView { ForecastId = "FORECAST_RENDER_110", UnionId = "ALLY", CommandId = "CMD_BALANCED", CommandName = "Attack",
                TargetId = "ENEMY", TargetName = "Read enemy", MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "ACTOR", ArtId = "ART_BASIC_SABER_CUT", ArtName = "Saber Cut",
                    ActionKind = "Martial", TargetUnionId = "ENEMY", TargetMemberId = "FOE", PredictedHpDelta097 = -10 } } } },
            Reward = resolved ? new M2BattleRewardView { RewardId = "RENDER_REWARD_110_" + suffix, Outcome = "Victory", CanClaim = true } : null
        };

        class CountingCoordinator110 : IM2PresentationCoordinator
        {
            public event Action Changed { add { } remove { } }
            public int StateReads, CommandCalls;
            public bool ThrowOnState;
            public M1PresentationState PublishedState;
            public M1PresentationState State
            {
                get
                {
                    StateReads++;
                    if (ThrowOnState) throw new InvalidOperationException("Whole State was requested by battle-only presentation.");
                    return PublishedState;
                }
            }
            M1CommandResult Unexpected110() { CommandCalls++; throw new InvalidOperationException("Read-only rendering invoked a gameplay command."); }
            public M1CommandResult CreateGuild(M1NewGuildIntent intent) => Unexpected110();
            public M1CommandResult SignRecruit(string id) => Unexpected110();
            public M1CommandResult EquipItem(string recruit, string slot, string item) => Unexpected110();
            public M1CommandResult UnequipItem(string recruit, string slot) => Unexpected110();
            public M1CommandResult SetEquipmentLock(string recruit, string slot, bool locked) => Unexpected110();
            public M1CommandResult CompleteEquipmentReview() => Unexpected110();
            public M1CommandResult AddUnion() => Unexpected110();
            public M1CommandResult RemoveUnion(int index) => Unexpected110();
            public M1CommandResult AssignRecruitToUnion(string recruit, int union, int slot) => Unexpected110();
            public M1CommandResult UnassignRecruitFromUnion(string recruit) => Unexpected110();
            public M1CommandResult SetUnionLeader(int union, string recruit) => Unexpected110();
            public M1CommandResult SetFormation(int union, string formation) => Unexpected110();
            public M1CommandResult SetDoctrine(int union, string doctrine) => Unexpected110();
            public M1CommandResult SaveAndReloadProof() => Unexpected110();
            public M1CommandResult StartTutorialBattle() => Unexpected110();
            public M1CommandResult SelectForecast(string union, string forecast) => Unexpected110();
            public M1CommandResult ConfirmBattleRound() => Unexpected110();
            public M1CommandResult ReplayTutorialBattle() => Unexpected110();
            public M1CommandResult RetryTutorialBattle() => Unexpected110();
            public M1CommandResult ClaimBattleRewards() => Unexpected110();
        }

        sealed class NarrowCoordinator110 : CountingCoordinator110, IM2BattleViewReader098
        {
            public int BattleReads;
            public M2BattleView Battle;
            public M2BattleView ReadBattleView098() { BattleReads++; return Battle; }
        }
    }
}
