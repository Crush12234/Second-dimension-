#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleReadLifecycle098PlayModeTests
    {
        string _directory;
        GameObject _owner;
        M1RuntimeCoordinator _coordinator;
        Action _observer;

        [UnityTearDown]
        public IEnumerator Cleanup098()
        {
            if (_coordinator != null && _observer != null) _coordinator.Changed -= _observer;
            if (_owner != null) UnityEngine.Object.Destroy(_owner);
            yield return null;
            if (!string.IsNullOrEmpty(_directory) && Directory.Exists(_directory))
                Directory.Delete(_directory, true); // This fixture's exact newly-created GUID directory only.
        }

        [UnityTest]
        public IEnumerator ActualTowerNotificationsRetainTerminalPlaybackClaimAndResultReentry098()
        {
            var source = Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", "R108_OldPendingFloor001.json");
            var original = File.ReadAllBytes(source);
            _directory = Path.Combine(Path.GetTempPath(), "sd_battle_lifecycle098_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var save = Path.Combine(_directory, "CopiedTower.json");
            File.Copy(source, save);
            _coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
            var opening = _coordinator.ReadBattleView098();
            Assert.That(opening.IsResolved, Is.False);
            Assert.That(opening.BattleId, Does.StartWith("ABYSS_BATTLE022_FLOOR_01_"));
            Assert.That(opening.EnemyUnions.Count, Is.EqualTo(1), "One existing small committed battle, not a new campaign run.");

            // A real rejected premature claim cannot notify, grant, or change the copied save.
            var beforeRejectedClaim = File.ReadAllBytes(save);
            var rejectedNotifications = 0;
            Action rejectedObserver = () => rejectedNotifications++;
            _coordinator.Changed += rejectedObserver;
            var rejectedClaim = _coordinator.ClaimBattleRewards();
            _coordinator.Changed -= rejectedObserver;
            Assert.That(rejectedClaim.Succeeded, Is.False);
            Assert.That(rejectedNotifications, Is.Zero);
            Assert.That(File.ReadAllBytes(save), Is.EqualTo(beforeRejectedClaim));

            _owner = new GameObject("Actual Tower notification lifecycle 098");
            var flow = _owner.AddComponent<M1FlowPresenter>();
            flow.Initialize(_coordinator);
            flow.EnterFirstHourBattleExperience072();
            yield return null;
            var controller = Field098<M2BattleExperienceController072>(flow, "_battleExperience072");
            controller.ReducedMotion = true;
            controller.AnimationSpeed = 4f;
            Assert.That(controller.OwnsCoordinatorNotifications098(_coordinator), Is.True);
            var terminalNotifications = 0;
            var claimingNotifications = 0;
            var completed = 0;
            controller.BattleCompleted += () => completed++;
            // Registered after actual Flow + controller subscriptions: inspect what their real callbacks retained.
            _observer = () =>
            {
                var current = _coordinator.ReadBattleView098();
                var pending = Field098<M2BattleView>(controller, "_pendingView");
                if (controller.IsResolving && current.IsResolved)
                {
                    terminalNotifications++;
                    Assert.That(pending.StateHash, Is.EqualTo(current.StateHash));
                    Assert.That(pending.IsResolved, Is.True);
                    Assert.That(Field098<RectTransform>(controller, "_resultLayer").gameObject.activeSelf, Is.False,
                        "Terminal authority must not reveal results over the ongoing round choreography.");
                }
                if (Field098<bool>(controller, "_claiming"))
                {
                    claimingNotifications++;
                    Assert.That(current.Reward.Claimed, Is.True);
                    Assert.That(pending.Reward.Claimed, Is.True);
                    Assert.That(pending.StateHash, Is.EqualTo(current.StateHash));
                    Assert.That(controller.IsActive, Is.True, "Synchronous claim notification precedes navigation.");
                    Assert.That(completed, Is.Zero);
                }
            };
            _coordinator.Changed += _observer;

            var deadline = Time.realtimeSinceStartup + 90f;
            var confirmedRounds = 0;
            while (!_coordinator.ReadBattleView098().IsResolved)
            {
                Assert.That(++confirmedRounds, Is.LessThanOrEqualTo(8), "No forced victory or unlimited test loop.");
                Assert.That(M2BattleAutoOrders091.SelectCompletePlan(_coordinator, out var failure), Is.True, failure);
                Assert.That(_coordinator.ReadBattleView098().CanConfirmRound, Is.True);
                // Invoke the actual UI handler: it resolves through the runtime coordinator and starts the real director.
                Invoke098(controller, "ConfirmRound");
                while (controller.IsResolving)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Supported 4x/reduced-motion playback exceeded 90 seconds.");
                    yield return null;
                }
            }

            var terminal = _coordinator.ReadBattleView098();
            Assert.That(terminal.Outcome, Is.EqualTo("Victory"), "A real defeat is not a passing claim/return test.");
            Assert.That(terminalNotifications, Is.EqualTo(1));
            Assert.That(terminal.Reward.Claimed, Is.False);
            Assert.That(Field098<RectTransform>(controller, "_resultLayer").gameObject.activeSelf, Is.True);
            Assert.That(Field098<M2BattleView>(controller, "_pendingView").StateHash, Is.EqualTo(terminal.StateHash));

            // Same handler installed on results Continue; this test covers notification/authority lifecycle,
            // not the independent results reveal/button animation timing.
            Invoke098(controller, "ClaimAndComplete");
            Assert.That(claimingNotifications, Is.EqualTo(1));
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(flow.IsFirstHourBattleExperienceActive072, Is.False);
            Assert.That(Field098<M1Screen>(flow, "_screen"), Is.EqualTo(M1Screen.GuildOperations));
            Assert.That(Field098<string>(flow, "_guildCityTab017D"), Is.EqualTo("ABYSS"));
            var claimed = new AtomicSaveStore().ReadWithRecovery(save);
            Assert.That(claimed.IsSuccess, Is.True, string.Join(";", claimed.Errors));
            Assert.That(claimed.Value.CampaignState.Guild.Development.ClaimedBattleRewardIds.Count(id => id == terminal.Reward.RewardId), Is.EqualTo(1));
            Assert.That(_coordinator.ReadBattleView098().Reward.Claimed, Is.True);
            var savedAfterClaim = File.ReadAllBytes(save);
            _coordinator.Changed -= _observer;
            _observer = null;
            yield return null; // Retire the old screen before testing explicit re-entry.

            flow.EnterFirstHourBattleExperience072();
            yield return null;
            var reopened = Field098<M2BattleExperienceController072>(flow, "_battleExperience072");
            Assert.That(reopened.OwnsCoordinatorNotifications098(_coordinator), Is.True);
            Assert.That(Field098<M2BattleView>(reopened, "_pendingView").Reward.Claimed, Is.True);
            Assert.That(Field098<RectTransform>(reopened, "_resultLayer").gameObject.activeSelf, Is.True);
            Assert.That(File.ReadAllBytes(save), Is.EqualTo(savedAfterClaim), "Reopening the retained result must not re-claim or replay the battle.");
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(original), "The archived earned-save fixture is never modified.");
            LogAssert.NoUnexpectedReceived();
        }

        static T Field098<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }
        static void Invoke098(object target, string name)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, null);
        }
    }
}
#endif
