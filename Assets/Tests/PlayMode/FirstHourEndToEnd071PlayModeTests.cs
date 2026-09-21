using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SecondDimension.Gameplay.FirstHour071;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FirstHourEndToEnd071PlayModeTests
    {
        [UnityTest]
        public IEnumerator TitleToDoorInsideRouteSavesReloadsAndPreservesEveryGate()
        {
            var director = new FirstHourDirector071();
            var state = director.StartNewGuild();
            var phases = new List<FirstHourPhase071>();
            var battleIds = new List<string>();
            var savePath = Path.Combine(Application.temporaryCachePath,
                "first_hour_e2e_071.checkpoint");

            try
            {
                Assert.That(director.PhaseOf(state), Is.EqualTo(FirstHourPhase071.SkyhomeArrival));
                Assert.That(director.AvailableRoster(state), Has.Count.EqualTo(6));

                while (!director.IsComplete(state))
                {
                    var segmentResult = director.CurrentSegment(state);
                    Assert.That(segmentResult.IsSuccess, Is.True,
                        string.Join("\n", segmentResult.Errors));
                    var segment = segmentResult.Value;
                    phases.Add(segment.Phase);
                    if (segment.IsBattle)
                        battleIds.Add(segment.EncounterId);

                    var advance = director.Advance(state, segment.RequiredAction);
                    Assert.That(advance.IsSuccess, Is.True,
                        string.Join("\n", advance.Errors));
                    state = advance.Value;
                    File.WriteAllText(savePath, state.CompletedSegmentCount.ToString());
                    yield return null;
                }

                Assert.That(phases, Is.EqualTo(new[]
                {
                    FirstHourPhase071.SkyhomeArrival,
                    FirstHourPhase071.EmergencyCharter,
                    FirstHourPhase071.BellWithoutRope,
                    FirstHourPhase071.HallBreachBattle,
                    FirstHourPhase071.LanternRoadOrder,
                    FirstHourPhase071.ReadyTheGuild,
                    FirstHourPhase071.FormTheUnions,
                    FirstHourPhase071.WalkLanternRoad,
                    FirstHourPhase071.RoadsideAmbushBattle,
                    FirstHourPhase071.FindThePatrol,
                    FirstHourPhase071.RecoverTheWayglass,
                    FirstHourPhase071.GateEaterBattle,
                    FirstHourPhase071.ReturnToSkyhome,
                    FirstHourPhase071.ChapterTwoHook
                }));
                Assert.That(battleIds, Is.EqualTo(new[]
                {
                    "ENCOUNTER071_HALL_BREACH",
                    "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                    "ENCOUNTER071_GATE_EATER"
                }));
                Assert.That(director.AvailableRoster(state), Has.Count.EqualTo(20));
                Assert.That(director.LastCheckpointId(state),
                    Is.EqualTo("FH071_CP_14_CHAPTER_TWO_READY"));
                Assert.That(FirstHourDirector071.ChapterTwoTitle,
                    Is.EqualTo("Chapter 2: The Door Inside"));

                var persistedCount = int.Parse(File.ReadAllText(savePath));
                var reloaded = director.StartNewGuild();
                while (reloaded.CompletedSegmentCount < persistedCount)
                {
                    var segment = director.CurrentSegment(reloaded).Value;
                    reloaded = director.Advance(reloaded, segment.RequiredAction).Value;
                }

                Assert.That(director.IsComplete(reloaded), Is.True);
                Assert.That(director.PacingMarkerMinute(reloaded), Is.EqualTo(60),
                    "This is the authored pacing marker, not a measured play-session duration.");
                Assert.That(director.AvailableRoster(reloaded), Has.Count.EqualTo(20));
                Assert.That(director.LastCheckpointId(reloaded),
                    Is.EqualTo("FH071_CP_14_CHAPTER_TWO_READY"));
            }
            finally
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }
        }
    }
}
