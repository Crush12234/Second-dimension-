using System;
using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleReadSubscription098PlayModeTests
    {
        [UnityTest]
        public IEnumerator ActualUnityDestroyUnsubscribesReboundControllerAndStopsStaleReads098()
        {
            var owner = new GameObject("Live subscription owner098");
            var host = new GameObject("Live subscription host098", typeof(RectTransform));
            var first = new Coordinator098();
            var second = new Coordinator098();
            try
            {
                Assert.That(Application.isPlaying, Is.True, "Destruction callback delivery requires PlayMode coverage.");
                var controller = owner.AddComponent<M2BattleExperienceController072>();
                controller.Initialize(first, host.GetComponent<RectTransform>());
                controller.Initialize(first, host.GetComponent<RectTransform>());
                yield return null;
                Assert.That(controller.isActiveAndEnabled, Is.True);
                Assert.That(first.Subscribers, Is.EqualTo(1));
                Assert.That(controller.OwnsCoordinatorNotifications098(first), Is.True);
                var firstReads = first.BattleReads;
                first.Raise();
                Assert.That(first.BattleReads, Is.EqualTo(firstReads + 1), "Positive control: the active subscription really projects.");

                controller.Initialize(second, host.GetComponent<RectTransform>());
                Assert.That(first.Subscribers, Is.Zero);
                Assert.That(second.Subscribers, Is.EqualTo(1));
                Assert.That(controller.OwnsCoordinatorNotifications098(first), Is.False);
                Assert.That(controller.OwnsCoordinatorNotifications098(second), Is.True);
                firstReads = first.BattleReads;
                first.Raise();
                Assert.That(first.BattleReads, Is.EqualTo(firstReads));
                var secondReads = second.BattleReads;
                second.Raise();
                Assert.That(second.BattleReads, Is.EqualTo(secondReads + 1));

                // No SendMessage/reflection cleanup: Unity must deliver OnDestroy.
                UnityEngine.Object.Destroy(controller);
                yield return null;
                Assert.That(controller == null, Is.True);
                Assert.That(first.Subscribers, Is.Zero);
                Assert.That(second.Subscribers, Is.Zero, "Destroyed controllers must release the live event publisher.");
                firstReads = first.BattleReads;
                secondReads = second.BattleReads;
                Assert.DoesNotThrow(() => { first.Raise(); second.Raise(); });
                Assert.That(first.BattleReads, Is.EqualTo(firstReads));
                Assert.That(second.BattleReads, Is.EqualTo(secondReads), "No projection from a stale publisher after actual destruction.");
                Assert.That(first.StateReads + second.StateReads, Is.Zero);
            }
            finally
            {
                if (owner != null) UnityEngine.Object.Destroy(owner);
                if (host != null) UnityEngine.Object.Destroy(host);
            }
            yield return null;
        }

        sealed class Coordinator098 : IM2PresentationCoordinator, IM2BattleViewReader098
        {
            Action _changed;
            public event Action Changed { add { _changed += value; } remove { _changed -= value; } }
            public int Subscribers => _changed?.GetInvocationList().Length ?? 0;
            public int BattleReads, StateReads;
            public void Raise() => _changed?.Invoke();
            // A battle is intentionally unnecessary: subscription lifecycle is
            // independent of commands, simulation, assets and reward claims.
            public M2BattleView ReadBattleView098() { BattleReads++; return null; }
            public M1PresentationState State { get { StateReads++; throw new NotSupportedException(); } }
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
            public M1CommandResult ClaimBattleRewards() => throw new NotSupportedException();
        }
    }
}
