using System;
using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class MovementFoundation070Tests
    {
        [Test]
        public void MotorTuningContainsUnsafeAuthorityOrInspectorValues()
        {
            var unsafeTuning = new WorldCharacterMotorTuning070
            {
                WalkSpeed = -10f,
                RunSpeed = -20f,
                Acceleration = 0f,
                Braking = -5f,
                Gravity = 18f,
                GroundStickVelocity = 4f,
                DodgeDuration = 0f,
                DodgeExitSpeedMultiplier = 4f,
                InputDeadZone = 2f
            };

            var safe = unsafeTuning.SanitizedCopy070();

            Assert.That(safe.WalkSpeed, Is.GreaterThan(0f));
            Assert.That(safe.RunSpeed, Is.GreaterThanOrEqualTo(safe.WalkSpeed));
            Assert.That(safe.Acceleration, Is.GreaterThan(0f));
            Assert.That(safe.Braking, Is.GreaterThan(0f));
            Assert.That(safe.Gravity, Is.LessThan(0f));
            Assert.That(safe.GroundStickVelocity, Is.LessThan(0f));
            Assert.That(safe.DodgeDuration, Is.GreaterThan(0f));
            Assert.That(safe.DodgeExitSpeedMultiplier, Is.InRange(0.1f, 1f));
            Assert.That(safe.InputDeadZone, Is.InRange(0f, 0.5f));
        }

        [Test]
        public void MotorTuningReplacesNaNAndInfinityWithFiniteDefaults()
        {
            var unsafeTuning = new WorldCharacterMotorTuning070
            {
                WalkSpeed = float.NaN,
                RunSpeed = float.PositiveInfinity,
                Acceleration = float.NegativeInfinity,
                Braking = float.NaN,
                TurnSpeedDegrees = float.PositiveInfinity,
                TurnAnticipationSeconds = float.NaN,
                StartAnticipationSeconds = float.NegativeInfinity,
                StopSettleSeconds = float.PositiveInfinity,
                ReverseTurnAngle = float.NaN,
                InputDeadZone = float.PositiveInfinity,
                Gravity = float.NaN,
                GroundStickVelocity = float.PositiveInfinity,
                DodgeSpeed = float.NegativeInfinity,
                DodgeExitSpeedMultiplier = float.NaN,
                DodgeDuration = float.PositiveInfinity,
                DodgeCooldown = float.NegativeInfinity,
                InteractionLockSeconds = float.NaN
            };

            var safe = unsafeTuning.SanitizedCopy070();
            var values = new[]
            {
                safe.WalkSpeed, safe.RunSpeed, safe.Acceleration, safe.Braking,
                safe.TurnSpeedDegrees, safe.TurnAnticipationSeconds,
                safe.StartAnticipationSeconds, safe.StopSettleSeconds,
                safe.ReverseTurnAngle, safe.InputDeadZone, safe.Gravity,
                safe.GroundStickVelocity, safe.DodgeSpeed,
                safe.DodgeExitSpeedMultiplier, safe.DodgeDuration,
                safe.DodgeCooldown, safe.InteractionLockSeconds
            };
            foreach (var value in values)
                Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False);
            Assert.That(safe.WalkSpeed, Is.EqualTo(4.6f));
            Assert.That(safe.RunSpeed, Is.EqualTo(7.2f));
            Assert.That(safe.Gravity, Is.EqualTo(-24f));
        }

        [Test]
        public void ExpeditionBuildsStableDataDrivenStationsAndFindsTheNearestAvailableMarker()
        {
            var spaceObject = new GameObject("Expedition Space 070 EditMode Test");
            var avatarObject = new GameObject("Expedition Avatar 070 EditMode Test");
            try
            {
                spaceObject.transform.position = new Vector3(10f, 0f, -2f);
                avatarObject.transform.position = new Vector3(13.1f, 0f, -2f);
                var space = spaceObject.AddComponent<ExpeditionSpace070>();
                space.Configure070(new[]
                {
                    new ExpeditionStationDefinition070(
                        " arrival gate ",
                        "Arrival Gate",
                        "Review the route",
                        ExpeditionStationKind070.Arrival,
                        Vector3.zero,
                        1.5f),
                    new ExpeditionStationDefinition070(
                        "field objective",
                        "Trapped Caravan",
                        "Help the caravan",
                        ExpeditionStationKind070.Objective,
                        new Vector3(3f, 0f, 0f),
                        2f),
                    new ExpeditionStationDefinition070(
                        "hidden cache",
                        "Hidden Cache",
                        "Search the cache",
                        ExpeditionStationKind070.Secret,
                        new Vector3(3.2f, 0f, 0f),
                        2f,
                        false)
                }, avatarObject.transform, false);

                Assert.That(space.StationCount070, Is.EqualTo(3));
                Assert.That(space.TryGetStation070("ARRIVAL_GATE", out var arrival), Is.True);
                Assert.That(arrival.DisplayName070, Is.EqualTo("Arrival Gate"));
                Assert.That(space.TryGetNearestStation070(avatarObject.transform.position, true, out var nearest),
                    Is.True);
                Assert.That(nearest.StationId070, Is.EqualTo("FIELD_OBJECTIVE"),
                    "The disabled secret marker must not steal proximity focus.");

                Assert.That(space.SetObjective070("field objective"), Is.True);
                Assert.That(space.ObjectiveStationId070, Is.EqualTo("FIELD_OBJECTIVE"));
                Assert.That(space.TryGetObjectiveDirection070(out var direction, out var distance), Is.True);
                Assert.That(direction.x, Is.LessThan(0f));
                Assert.That(distance, Is.LessThan(0.2f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(spaceObject);
                UnityEngine.Object.DestroyImmediate(avatarObject);
            }
        }

        [Test]
        public void ExpeditionRejectsDuplicateNormalizedStationIds()
        {
            var host = new GameObject("Duplicate Station Test 070");
            try
            {
                var space = host.AddComponent<ExpeditionSpace070>();
                Assert.Throws<ArgumentException>(() => space.Configure070(new[]
                {
                    new ExpeditionStationDefinition070(
                        "creator room",
                        "Creator Room",
                        "Enter",
                        ExpeditionStationKind070.CreatorRoom,
                        Vector3.zero),
                    new ExpeditionStationDefinition070(
                        "CREATOR-ROOM",
                        "Duplicate",
                        "Enter",
                        ExpeditionStationKind070.CreatorRoom,
                        Vector3.right)
                }, null, false));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
