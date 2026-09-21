using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattlePlaybackSpeed091Tests
    {
        private GameObject _root;
        private GameObject _ownedEventSystem;
        private M2BattleExperienceController072 _controller;
        private float _timeScale;

        [SetUp]
        public void SetUp091()
        {
            _timeScale = Time.timeScale;
            _root = new GameObject("Battle Playback Speed Test 091", typeof(RectTransform));
            _controller = _root.AddComponent<M2BattleExperienceController072>();
        }

        [TearDown]
        public void TearDown091()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            if (_ownedEventSystem != null) UnityEngine.Object.DestroyImmediate(_ownedEventSystem);
            Assert.That(Time.timeScale, Is.EqualTo(_timeScale),
                "Playback controls must not change the global gameplay clock.");
        }

        [Test]
        public void DefaultCyclesOneTwoFourSixteenOneAndNotifiesOncePerClick091()
        {
            var notifications = new List<float>();
            _controller.PlaybackSpeedChanged091 = notifications.Add;
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(1f));
            for (var index = 0; index < 8; index++) _controller.CyclePlaybackSpeed091();
            Assert.That(notifications, Is.EqualTo(new[] { 2f, 4f, 16f, 1f, 2f, 4f, 16f, 1f }));
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(1f));
        }

        [TestCase(-10f, 0.25f)]
        [TestCase(0f, 0.25f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(10f, 10f)]
        [TestCase(20f, 16f)]
        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(float.NegativeInfinity, 1f)]
        public void SettingsAreFiniteAndClampedWithoutEmittingUserClicks091(float input, float expected)
        {
            var notifications = 0;
            _controller.PlaybackSpeedChanged091 = _ => notifications++;
            _controller.AnimationSpeed = input;
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(expected));
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void ActualRibbonButtonRemainsClickableWhileCommandsAreHiddenAndResolving091()
        {
            var ribbon = Child091("Battle Story Ribbon 072");
            var commands = Child091("Compact Union Command Layer 072");
            commands.gameObject.SetActive(false);
            SetField091("_resolving", true);
            var button = BuildButton091(ribbon);
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                _ownedEventSystem = new GameObject("Playback Test EventSystem 091", typeof(EventSystem));
                eventSystem = _ownedEventSystem.GetComponent<EventSystem>();
            }
            var pointer = new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left };
            var notifications = new List<float>();
            _controller.PlaybackSpeedChanged091 = notifications.Add;

            Assert.That(button.transform.parent, Is.EqualTo(ribbon));
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);

            Assert.That(_controller.IsResolving, Is.True, "Speed must not confirm, skip, or finish a round.");
            Assert.That(_controller.AnimationSpeed, Is.EqualTo(2f));
            Assert.That(notifications, Is.EqualTo(new[] { 2f }));
            Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("SPEED\n2×"));
            Assert.That(commands.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ExistingPreferenceAndNewClicksUpdateLabelAcrossViewRebuild091()
        {
            _controller.AnimationSpeed = 4f;
            var firstRibbon = Child091("First Ribbon");
            var first = BuildButton091(firstRibbon);
            Assert.That(first.GetComponentInChildren<Text>().text, Is.EqualTo("SPEED\n4×"));
            _controller.CyclePlaybackSpeed091();
            Assert.That(first.GetComponentInChildren<Text>().text, Is.EqualTo("SPEED\n16×"));
            UnityEngine.Object.DestroyImmediate(firstRibbon.gameObject);
            _controller.AnimationSpeed = 2f;
            var second = BuildButton091(Child091("Rebuilt Ribbon"));
            Assert.That(second.GetComponentInChildren<Text>().text, Is.EqualTo("SPEED\n2×"));
        }

        [Test]
        public void SequenceWaitReadsUpdatedSpeedOnEveryYieldAndSkipStillEndsIt091()
        {
            var samples = new List<float>();
            var skip = false;
            Func<float> provider = () =>
            {
                samples.Add(_controller.AnimationSpeed);
                return _controller.AnimationSpeed;
            };
            var wait = (IEnumerator)typeof(M2BattleSequenceDirector072)
                .GetMethod("Wait", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { float.MaxValue, provider, new Func<bool>(() => skip) });
            Assert.That(wait.MoveNext(), Is.True);
            _controller.CyclePlaybackSpeed091();
            Assert.That(wait.MoveNext(), Is.True);
            _controller.CyclePlaybackSpeed091();
            Assert.That(wait.MoveNext(), Is.True);
            skip = true;
            Assert.That(wait.MoveNext(), Is.False);
            Assert.That(samples, Is.EqualTo(new[] { 1f, 2f, 4f }),
                "The current beat must accelerate without restarting or rebuilding its sequence.");
        }

        [TestCase(1f, 0.8f)]
        [TestCase(4f, 0.2f)]
        [TestCase(16f, 0.05f)]
        public void AutoSchedulerWaitTracksSelectedPresentationSpeed108(float speed, float expected)
        {
            Assert.That(M2BattleExperienceController072.AutoSchedulerDelay108(speed),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void AutoFeedUsesAcceptedForecastAndActualResolvedTarget108()
        {
            var player = new M2BattleUnionView
            {
                UnionId = "UNION_A", DisplayName = "Vanguard", CanAct = true,
                SelectedForecastId = "FORECAST_A",
                Members = new[] { new M2BattleMemberView { MemberId = "HEALER", DisplayName = "Sella" } }
            };
            var enemy = new M2BattleUnionView
            {
                UnionId = "ENEMY_A", DisplayName = "Enemy Line",
                Members = new[] { new M2BattleMemberView { MemberId = "LOWEN", DisplayName = "Lowen" } }
            };
            var before = new M2BattleView
            {
                BattleId = "BATTLE108", Round = 7,
                PlayerUnions = new[] { player }, EnemyUnions = new[] { enemy },
                Forecasts = new[] { new M2ForecastView
                {
                    ForecastId = "FORECAST_A", UnionId = "UNION_A", CommandId = "CMD_RESTORE",
                    CommandName = "Restore the Line", TacticalIntent = "RESTORE",
                    ExpectedEffect = "Heal the lowest-HP ally.",
                    MemberActions = new[] { new M2PredictedActionView
                    {
                        ActorMemberId = "HEALER", ActorName = "Sella", ArtId = "ART_HEAL",
                        ArtName = "Healing Light", ActionKind = "Restoration",
                        TargetUnionId = "ENEMY_A", TargetMemberId = "PLANNED_TARGET",
                        TargetName = "Planned Target"
                    } }
                } }
            };
            var after = new M2BattleView
            {
                BattleId = "BATTLE108", Round = 8,
                PlayerUnions = new[] { player }, EnemyUnions = new[] { enemy },
                LastResolvedRoundEvents = new[] { new M2BattleEventView
                {
                    Sequence = 2, Round = 7, EventType = "RESTORATION",
                    Text = "Sella uses Healing Light on Lowen.", ActorUnionId = "UNION_A",
                    ActorMemberId = "HEALER", ArtId = "ART_HEAL",
                    TargetUnionId = "ENEMY_A", TargetMemberId = "LOWEN"
                } }
            };

            var text = M2BattleExperienceController072.BuildCommittedAutoDecisionBlocks108(before, after)["UNION_A"];
            Assert.That(text, Does.Contain("R7  •  Vanguard → Restore the Line"));
            Assert.That(text, Does.Contain("WHY  •  RESTORE  •  Heal the lowest-HP ally."));
            Assert.That(text, Does.Contain("Vanguard / Sella → Healing Light → Lowen"));
            Assert.That(text, Does.Contain("PLANNED  •  Vanguard / Sella → Healing Light → Planned Target"));
            Assert.That(text, Does.Contain("EXECUTED  •  Vanguard / Sella → Healing Light → Lowen"));
        }

        [Test]
        public void TowerRewardNoticeReportsOnlyCommittedRewardValues108()
        {
            var cleared = new SecondDimension.Presentation.Campaign022.CampaignProgressionPresentationState022
            {
                TowerGuildXpReward = 70, TowerHallXpReward = 55,
                TowerRewardMaterialIds108 = new[] { "MAT020_BUNNY_01", "MAT020_DOG_02" }
            };
            var saved = new SecondDimension.Presentation.Campaign022.CampaignProgressionPresentationState022
            {
                TowerLastHeroRewardFloor094 = 220,
                TowerLastHeroRewardSummary094 = "SSS HERO • Orinth — reward received."
            };
            var battle = new M2BattleRewardView
            {
                GuildTreasuryXpAward = 315, HallEnhancementXpAward = 157,
                EquipmentRewardDisplayName = "Moon Spear"
            };
            var text = M2BattleExperienceController072.BuildTowerRewardNotice108(220, cleared, battle, saved);
            Assert.That(text, Does.Contain("FLOOR 220 CLEARED"));
            Assert.That(text, Does.Contain("+315 BATTLE GUILD XP"));
            Assert.That(text, Does.Contain("+70 FLOOR GUILD XP"));
            Assert.That(text, Does.Contain("+4 MAT020_BUNNY_01"));
            Assert.That(text, Does.Contain("Moon Spear"));
            Assert.That(text, Does.Contain("Orinth"));
            Assert.That(text, Does.EndWith("REWARDS CLAIMED  •  PROGRESS SAVED"));
        }

        [Test]
        public void SpeedDoesNotChangeCommittedDamageOrGenerateCanceledActionBeats091()
        {
            var events = new[]
            {
                Event091(0, "MARTIAL_HIT", 120), Event091(1, "DOWNED", 0),
                Event091(2, "ACTION_CANCELED_DEAD_TARGET", 0),
                Event091(3, "VICTORY_SEQUENCE_STOP", 0), Event091(4, "BATTLE_RESULT", 0)
            };
            var original = BattlePresentationPlanner.Plan(events).Select(value => value.StableDescriptor).ToArray();
            _controller.AnimationSpeed = 4f;
            var accelerated = BattlePresentationPlanner.Plan(events);
            Assert.That(accelerated.Select(value => value.StableDescriptor), Is.EqualTo(original));
            Assert.That(events[0].Amount, Is.EqualTo(120));
            Assert.That(accelerated[2].VisuallyStaged, Is.False);
            Assert.That(accelerated[3].VisuallyStaged, Is.False);
            Assert.That(accelerated.Count(value => value.Family == BattleBeatFamily.Result), Is.EqualTo(1));
        }

        [Test]
        public void ShippingBindingRetainsPresenterPreferenceAndPassesLiveSpeedToDirector091()
        {
            var folder = Path.Combine(Application.dataPath, "SecondDimension", "Presentation", "Battle", "Experience");
            var binding = File.ReadAllText(Path.Combine(folder, "M1FlowPresenter.BattleExperience072.cs"));
            var controller = File.ReadAllText(Path.Combine(folder, "M2BattleExperienceController072.cs"));
            var sequence = File.ReadAllText(Path.Combine(folder, "M2BattleSequenceDirector072.cs"));
            Assert.That(binding, Does.Contain("PlaybackSpeedChanged091 = speed => _battleAnimationSpeed = speed"));
            Assert.That(binding, Does.Contain("M2BattleExperienceController072.MaximumPlaybackSpeed108"));
            Assert.That(controller, Does.Contain("BuildPlaybackSpeedControl091(_storyRibbon.transform)"));
            Assert.That(controller, Does.Contain("() => Mathf.Clamp(AnimationSpeed, 0.25f, MaximumPlaybackSpeed108)"));
            Assert.That(sequence, Does.Contain("Time.unscaledDeltaTime * multiplier"));
            Assert.That(binding + controller + sequence, Does.Not.Contain("Time.timeScale"));
        }

        private RectTransform Child091(string name)
        {
            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(_root.transform, false);
            return child;
        }

        private Button BuildButton091(Transform parent) => (Button)typeof(M2BattleExperienceController072)
            .GetMethod("BuildPlaybackSpeedControl091", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_controller, new object[] { parent });

        private void SetField091(string name, object value) => typeof(M2BattleExperienceController072)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_controller, value);

        private static M2BattleEventView Event091(int sequence, string eventType, int amount) => new M2BattleEventView
        {
            Sequence = sequence, Round = 1, EventType = eventType, Amount = amount,
            ActorUnionId = "PLAYER_A", ActorMemberId = "PLAYER_MEMBER_A",
            TargetUnionId = "ENEMY_A", TargetMemberId = "ENEMY_MEMBER_A", ArtId = "ART_BASIC_SABER_CUT"
        };
    }
}
