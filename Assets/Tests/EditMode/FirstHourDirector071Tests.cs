using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.FirstHour071;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourDirector071Tests
    {
        private FirstHourDirector071 _director;

        [SetUp]
        public void SetUp()
        {
            _director = new FirstHourDirector071();
        }

        [Test]
        public void AuthoredPacingMarkersCoverZeroToSixtyInOrder()
        {
            var state = _director.StartNewGuild();
            var reached = new List<FirstHourPhase071>();
            var checkpoints = new HashSet<string>(StringComparer.Ordinal)
            {
                _director.LastCheckpointId(state)
            };

            Assert.That(_director.PacingMarkerMinute(state), Is.Zero);
            for (var expectedIndex = 0; expectedIndex < _director.Timeline.Count; expectedIndex++)
            {
                var current = _director.CurrentSegment(state);
                Assert.That(current.IsSuccess, Is.True, string.Join("\n", current.Errors));
                var segment = current.Value;
                reached.Add(segment.Phase);
                Assert.That(segment, Is.SameAs(_director.Timeline[expectedIndex]));
                Assert.That(segment.StartMinute,
                    Is.EqualTo(expectedIndex == 0 ? 0 : _director.Timeline[expectedIndex - 1].EndMinute),
                    segment.SegmentId);

                var advanced = _director.Advance(state, segment.RequiredAction);
                Assert.That(advanced.IsSuccess, Is.True, string.Join("\n", advanced.Errors));
                state = advanced.Value;
                Assert.That(checkpoints.Add(_director.LastCheckpointId(state)), Is.True,
                    segment.CompletionCheckpointId);
            }

            Assert.That(reached, Has.Count.EqualTo(14));
            Assert.That(reached.Distinct().Count(), Is.EqualTo(reached.Count));
            Assert.That(reached[0], Is.EqualTo(FirstHourPhase071.SkyhomeArrival));
            Assert.That(reached[reached.Count - 1], Is.EqualTo(FirstHourPhase071.ChapterTwoHook));
            Assert.That(_director.Timeline[reached.Count - 1].EndMinute, Is.EqualTo(60));
            Assert.That(_director.PacingMarkerMinute(state), Is.EqualTo(60));
            Assert.That(_director.PhaseOf(state), Is.EqualTo(FirstHourPhase071.Complete));
            Assert.That(_director.IsComplete(state), Is.True);
            Assert.That(_director.LastCheckpointId(state), Is.EqualTo("FH071_CP_14_CHAPTER_TWO_READY"));
            Assert.That(FirstHourDirector071.ChapterTwoTitle, Is.EqualTo("Chapter 2: The Door Inside"));
        }

        [Test]
        public void EveryObjectiveActionAndDestinationUsesPlainPlayerFacingLanguage()
        {
            foreach (var segment in _director.Timeline)
            {
                AssertPlain(segment.Objective, segment.SegmentId + " objective");
                AssertPlain(segment.NextAction, segment.SegmentId + " next action");
                AssertPlain(segment.DestinationName, segment.SegmentId + " destination");
                Assert.That(segment.Objective.Length, Is.LessThanOrEqualTo(80), segment.SegmentId);
                Assert.That(segment.NextAction.Length, Is.LessThanOrEqualTo(80), segment.SegmentId);
                Assert.That(segment.Dialogue, Is.Not.Empty, segment.SegmentId);
            }
        }

        [Test]
        public void KaelAndAllSevenFoundersRemainProtectedAndNeverRecruitable()
        {
            var playableIds = new HashSet<string>(
                _director.PlayableRoster.Select(value => value.RecruitId), StringComparer.Ordinal);

            Assert.That(_director.ProtectedActors, Has.Count.EqualTo(8));
            Assert.That(_director.ProtectedActors.Count(value => value.CanonStatus == "FOUNDER"), Is.EqualTo(7));
            Assert.That(_director.ProtectedActors.Any(value => value.ActorId == "CANON_KAEL"), Is.True);
            Assert.That(_director.ProtectedActors.Any(value => value.ActorId == "CANON_KIRI_AETHERHEART"), Is.True);
            foreach (var actor in _director.ProtectedActors)
            {
                Assert.That(actor.CanJoinFirstHourRoster, Is.False, actor.ActorId);
                Assert.That(playableIds.Contains(actor.ActorId), Is.False, actor.ActorId);
            }
        }

        [Test]
        public void AvailabilityManifestContainsTwentyUniqueCanonicalPlayableCharacters()
        {
            Assert.That(_director.PlayableRoster, Has.Count.EqualTo(20));
            Assert.That(_director.PlayableRoster.Select(value => value.RecruitId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
            Assert.That(_director.PlayableRoster.Select(value => value.DisplayName)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(20));
            Assert.That(_director.PlayableRoster.All(value => value.IsPlayable), Is.True);
            Assert.That(_director.PlayableRoster.All(value =>
                value.RecruitId.StartsWith("SIGREC_", StringComparison.Ordinal) ||
                value.RecruitId.StartsWith("PROC_", StringComparison.Ordinal)), Is.True);
            Assert.That(_director.PlayableRoster.Count(value =>
                value.Wave == FirstHourRosterWave071.OpeningLead), Is.EqualTo(6));
            Assert.That(_director.PlayableRoster.Count(value =>
                value.Wave == FirstHourRosterWave071.SkyhomeCharter), Is.EqualTo(4));
            Assert.That(_director.PlayableRoster.Count(value =>
                value.Wave == FirstHourRosterWave071.LanternPatrol), Is.EqualTo(10));
            Assert.That(_director.PlayableRoster[0].RecruitId, Is.EqualTo("PROC_36344E2400DC98B6"));
            Assert.That(_director.PlayableRoster.Any(value =>
                value.RecruitId == "SIGREC_MAREN_HOLT"), Is.True);
            Assert.That(_director.PlayableRoster.All(value =>
                value.AvailableAtMinute >= 0 && value.AvailableAtMinute <= 50), Is.True);

            var state = _director.StartNewGuild();
            Assert.That(_director.AvailableRoster(state), Has.Count.EqualTo(6));
            while (_director.PacingMarkerMinute(state) < 5)
            {
                var segment = _director.CurrentSegment(state).Value;
                state = _director.Advance(state, segment.RequiredAction).Value;
            }
            Assert.That(_director.AvailableRoster(state), Has.Count.EqualTo(10));
            while (_director.PacingMarkerMinute(state) < 50)
            {
                var segment = _director.CurrentSegment(state).Value;
                state = _director.Advance(state, segment.RequiredAction).Value;
            }
            Assert.That(_director.AvailableRoster(state), Has.Count.EqualTo(20));
        }

        [Test]
        public void FirstHourContainsExactlyThreeOrderedBattlePhases()
        {
            var battles = _director.Timeline.Where(value => value.IsBattle).ToArray();

            Assert.That(battles, Has.Length.EqualTo(3));
            Assert.That(battles.Select(value => value.EncounterId)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
            Assert.That(battles.Select(value => value.Phase), Is.EqualTo(new[]
            {
                FirstHourPhase071.HallBreachBattle,
                FirstHourPhase071.RoadsideAmbushBattle,
                FirstHourPhase071.GateEaterBattle
            }));
            Assert.That(battles.Select(value => value.StartMinute), Is.EqualTo(new[] { 7, 34, 50 }));
            Assert.That(battles.Select(value => value.EndMinute), Is.EqualTo(new[] { 14, 40, 57 }));
            Assert.That(_director.Timeline.Where(value => !value.IsBattle)
                .All(value => string.IsNullOrEmpty(value.EncounterId)), Is.True);
        }

        [Test]
        public void StateTransitionsAreDeterministicAndRejectOutOfOrderActions()
        {
            var start = _director.StartNewGuild();
            var rejected = _director.Advance(start, FirstHourAction071.WinGateEaterBattle);

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(start.CompletedSegmentCount, Is.Zero);
            Assert.That(_director.PhaseOf(start), Is.EqualTo(FirstHourPhase071.SkyhomeArrival));

            var expectedAction = _director.CurrentSegment(start).Value.RequiredAction;
            var first = _director.Advance(start, expectedAction).Value;
            var replay = _director.Advance(start, expectedAction).Value;
            Assert.That(replay.CompletedSegmentCount, Is.EqualTo(first.CompletedSegmentCount));
            Assert.That(_director.PhaseOf(replay), Is.EqualTo(_director.PhaseOf(first)));
            Assert.That(_director.LastCheckpointId(replay), Is.EqualTo(_director.LastCheckpointId(first)));
        }

        private static void AssertPlain(string value, string context)
        {
            Assert.That(value, Is.Not.Null.And.Not.Empty, context);
            Assert.That(value, Does.Not.Contain("_"), context);
            Assert.That(value, Does.Not.Contain("FH071"), context);
            Assert.That(value, Does.Not.Contain("SIGREC"), context);
            Assert.That(value.Any(char.IsLower), Is.True, context);
        }
    }
}
