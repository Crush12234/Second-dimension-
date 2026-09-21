using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2BattlePresentationPlannerTests
    {
        [Test]
        public void SameAuthoritativeEventsProduceIdenticalOrderedBeatDescriptors()
        {
            var events = RepresentativeEvents();
            var first = BattlePresentationPlanner.Plan(events).Select(value => value.StableDescriptor).ToArray();
            var second = BattlePresentationPlanner.Plan(events).Select(value => value.StableDescriptor).ToArray();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Length, Is.EqualTo(events.Count));
            Assert.That(first[1], Does.Contain("PLAYER_A_MEMBER_1"));
            Assert.That(first[1], Does.Contain("ENEMY_A_MEMBER_1"));
        }

        [Test]
        public void EveryRequiredTutorialCombatEventHasVisualOrExplicitNonvisualHandling()
        {
            var required = new[]
            {
                "BATTLE_START", "FORECAST_COMMITTED", "MARTIAL_HIT", "MYSTIC_HIT", "TACTICAL_HIT",
                "RESTORATION", "REVIVED", "CLEANSED", "STABILIZED", "GUARD", "INTERCEPTION",
                "RECOVERY", "AP_RECOVERY", "FORMATION_RECOVERY", "ALLY_SUPPORT", "ALLY_PROTECTED",
                "ENEMY_SUPPORT_FORECAST", "ENEMY_HIT", "DOWNED", "ART_GROWTH", "BREAKTHROUGH", "RETREAT",
                "POSITION_SHIFT", "BATTLE_RESULT"
            };
            var events = required.Select((eventType, index) => new M2BattleEventView
            {
                Sequence = index,
                Round = 1,
                EventType = eventType,
                Text = eventType,
                ActorMemberId = "ACTOR",
                TargetMemberId = "TARGET",
                ArtId = eventType == "MARTIAL_HIT" ? "ART_BASIC_SABER_CUT" : "ART_POWER_CUT"
            }).ToArray();

            var beats = BattlePresentationPlanner.Plan(events);
            Assert.That(beats.Count, Is.EqualTo(required.Length));
            Assert.That(beats.All(value => value.VisuallyStaged), Is.True);
            Assert.That(beats.All(value => value.Family != BattleBeatFamily.IntentionallyNonVisual), Is.True);
            Assert.That(beats.Any(value => value.Family == BattleBeatFamily.BasicMartial), Is.True);
            Assert.That(beats.Any(value => value.Family == BattleBeatFamily.Mystic), Is.True);
            Assert.That(beats.Any(value => value.Family == BattleBeatFamily.Restoration), Is.True);
            Assert.That(beats.Single(value => value.EventType == "REVIVED").Family,
                Is.EqualTo(BattleBeatFamily.Restoration));
            Assert.That(beats.Single(value => value.EventType == "ALLY_SUPPORT").Family,
                Is.EqualTo(BattleBeatFamily.Formation));
            Assert.That(beats.Single(value => value.EventType == "ALLY_PROTECTED").Family,
                Is.EqualTo(BattleBeatFamily.Guard));
            Assert.That(beats.Any(value => value.Family == BattleBeatFamily.Positioning), Is.True);
            Assert.That(beats.Any(value => value.Family == BattleBeatFamily.Breakthrough), Is.True);
        }

        [Test]
        public void MissingOptionalPresentationDefinitionUsesDocumentedNonvisualFallback()
        {
            var unknown = new M2BattleEventView
            {
                Sequence = 42,
                Round = 3,
                EventType = "OPTIONAL_FUTURE_PRESENTATION_EVENT",
                Text = "Authoritative truth remains readable in captions."
            };

            var beat = BattlePresentationPlanner.Plan(new[] { unknown }).Single();
            Assert.That(beat.Family, Is.EqualTo(BattleBeatFamily.IntentionallyNonVisual));
            Assert.That(beat.VisuallyStaged, Is.False);
            Assert.That(beat.Caption, Is.EqualTo(unknown.Text));
            Assert.That(beat.SourceSequence, Is.EqualTo(42));
        }

        [Test]
        public void CompleteRoundPlanningDoesNotDropEventsBeyondTheRecentLogWindow()
        {
            var events = Enumerable.Range(0, 24).Select(index => new M2BattleEventView
            {
                Sequence = index,
                Round = 2,
                EventType = index % 2 == 0 ? "MARTIAL_HIT" : "ART_GROWTH",
                Text = "Event " + index,
                ActorMemberId = "PLAYER_" + index,
                TargetMemberId = "ENEMY_" + index,
                ArtId = index % 2 == 0 ? "ART_BASIC_SABER_CUT" : "ART_POWER_CUT",
                Amount = index
            }).ToArray();

            var beats = BattlePresentationPlanner.Plan(events);
            Assert.That(beats.Count, Is.EqualTo(24));
            Assert.That(beats.Select(value => value.SourceSequence).ToArray(),
                Is.EqualTo(Enumerable.Range(0, 24).ToArray()));
            Assert.That(beats.Last().Caption, Is.EqualTo("Event 23"));
        }

        private static IReadOnlyList<M2BattleEventView> RepresentativeEvents() => new[]
        {
            new M2BattleEventView
            {
                Sequence = 0, Round = 1, EventType = "FORECAST_COMMITTED", Text = "Attack!",
                ActorUnionId = "PLAYER_A", TargetUnionId = "ENEMY_A"
            },
            new M2BattleEventView
            {
                Sequence = 1, Round = 1, EventType = "MARTIAL_HIT", Text = "Saber Cut.", Amount = 23,
                ActorUnionId = "PLAYER_A", ActorMemberId = "PLAYER_A_MEMBER_1",
                TargetUnionId = "ENEMY_A", TargetMemberId = "ENEMY_A_MEMBER_1",
                ArtId = "ART_BASIC_SABER_CUT"
            },
            new M2BattleEventView
            {
                Sequence = 2, Round = 1, EventType = "BREAKTHROUGH", Text = "Power Cut learned.",
                ActorUnionId = "PLAYER_A", ActorMemberId = "PLAYER_A_MEMBER_1", ArtId = "ART_POWER_CUT"
            }
        };
    }
}
