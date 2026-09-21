using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2DefeatedTargetRetarget088PlayModeTests
    {
        [UnityTest]
        public IEnumerator AuthorityEventsReuseExistingVisibleBeatsWithoutPhantomPresentation088()
        {
            var authorityTypes = new[]
            {
                "TARGET_MEMBER_RETARGETED",
                "TARGET_UNION_RETARGETED",
                "ENEMY_UNION_DEFEATED",
                "ACTION_CANCELED_DEAD_TARGET",
                "VICTORY_SEQUENCE_STOP"
            };
            var events = authorityTypes.Select((eventType, index) => Event(index, eventType))
                .Concat(new[]
                {
                    Event(5, "POSITION_SHIFT"),
                    Event(6, "DOWNED"),
                    Event(7, "BATTLE_RESULT")
                }).ToArray();

            var beats = BattlePresentationPlanner.Plan(events);
            yield return null;

            foreach (var eventType in authorityTypes)
            {
                var beat = beats.Single(value => value.EventType == eventType);
                Assert.That(beat.Family, Is.EqualTo(BattleBeatFamily.IntentionallyNonVisual));
                Assert.That(beat.VisuallyStaged, Is.False);
                Assert.That(beat.VfxCue, Is.Empty);
                Assert.That(beat.AudioCue, Is.Empty,
                    eventType + " must not create a phantom or duplicate audiovisual reaction.");
            }

            var movement = beats.Single(value => value.EventType == "POSITION_SHIFT");
            Assert.That(movement.Family, Is.EqualTo(BattleBeatFamily.Positioning));
            Assert.That(movement.Camera, Is.EqualTo(BattleCameraShot.ActionTrack));
            Assert.That(movement.VisuallyStaged, Is.True);
            Assert.That(beats.Single(value => value.EventType == "DOWNED").VisuallyStaged, Is.True);
            Assert.That(beats.Single(value => value.EventType == "BATTLE_RESULT").Family,
                Is.EqualTo(BattleBeatFamily.Result));
        }

        private static M2BattleEventView Event(int sequence, string eventType) =>
            new M2BattleEventView
            {
                Sequence = sequence,
                Round = 1,
                EventType = eventType,
                Text = eventType,
                Side = "Player",
                ActorUnionId = "ALLY_A",
                ActorMemberId = "ALLY_MEMBER_A",
                TargetUnionId = "ENEMY_B",
                TargetMemberId = "ENEMY_MEMBER_B"
            };
    }
}
