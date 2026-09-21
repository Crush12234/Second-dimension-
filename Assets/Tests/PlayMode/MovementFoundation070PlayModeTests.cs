using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class MovementFoundation070PlayModeTests
    {
        [UnityTest]
        public IEnumerator VerificationIntervalSubstepsFullDurationAndStopClearsQueuedEdges()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Movement Substep Floor 070 Test";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            var avatar = CreateAvatar070("Movement Substep Avatar 070 Test");
            var motor = avatar.GetComponent<WorldCharacterMotor070>();
            var interactions = 0;
            motor.InteractionRequested070 += () => interactions++;
            yield return null;

            var before = avatar.transform.position;
            motor.Simulate070(Vector2.right, false, false, false, 0.5f);
            Assert.That(Vector3.Distance(before, avatar.transform.position), Is.GreaterThan(1f),
                "A host verification interval must be consumed in safe substeps rather than truncated to one frame.");

            motor.SetInput070(Vector2.zero, true, true, true);
            motor.StopImmediately070();
            motor.Tick070(0.05f);
            Assert.That(motor.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Idle));
            Assert.That(interactions, Is.Zero,
                "Opening a modal must clear queued dodge and interaction edges before the next motor tick.");

            Object.Destroy(avatar);
            Object.Destroy(floor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MotorAcceleratesTurnsDodgesStaysGroundedAndStopsAtCollision()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Movement Floor 070 Test";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Movement Wall 070 Test";
            wall.transform.position = new Vector3(3f, 1f, 0f);
            wall.transform.localScale = new Vector3(0.5f, 2f, 4f);

            var avatar = CreateAvatar070("Collision Safe Avatar 070 Test");
            var motor = avatar.GetComponent<WorldCharacterMotor070>();

            // This test performs the complete verification interval inside one
            // rendered frame. The project deliberately disables automatic
            // transform syncing, so publish the newly positioned floor, wall,
            // and controller to the physics scene before CharacterController.Move.
            // Runtime scenes receive this sync from Unity's fixed-step loop.
            Physics.SyncTransforms();
            yield return null;

            motor.Simulate070(Vector2.right, false, false, false, 0.05f);
            var firstSpeed = motor.PlanarSpeed070;
            Assert.That(motor.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Starting));
            for (var index = 0; index < 8; index++)
                motor.Simulate070(Vector2.right, false, false, false, 0.05f);

            Assert.That(motor.PlanarSpeed070, Is.GreaterThan(firstSpeed),
                "Movement must accelerate instead of jumping to full speed.");
            Assert.That(motor.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Walking));

            for (var index = 0; index < 80; index++)
                motor.Simulate070(Vector2.right, false, false, false, 0.05f);
            Assert.That(avatar.transform.position.x, Is.LessThan(2.55f),
                "CharacterController.Move must stop the avatar at the wall.");
            Assert.That(motor.IsGrounded070, Is.True);
            Assert.That((motor.LastCollisionFlags070 & CollisionFlags.Sides) != 0, Is.True);

            motor.Simulate070(Vector2.left, false, false, false, 0.05f);
            Assert.That(motor.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Turning));

            motor.StopImmediately070();
            motor.Simulate070(Vector2.left, false, true, false, 0.05f);
            Assert.That(motor.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Dodging));
            Assert.That(motor.DodgeNormalizedTime070, Is.GreaterThan(0f));

            Object.Destroy(avatar);
            Object.Destroy(wall);
            Object.Destroy(floor);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimatorExposesReadableStartWalkRunStopTurnDodgeAndInteractionPoses()
        {
            var avatar = CreateAvatar070("Animated Avatar 070 Test");
            var motor = avatar.GetComponent<WorldCharacterMotor070>();
            var visualObject = new GameObject("Authored Visual Root 070 Test");
            visualObject.transform.SetParent(avatar.transform, false);
            var leftFoot = new GameObject("Left Foot 070 Test").transform;
            leftFoot.SetParent(visualObject.transform, false);
            leftFoot.localPosition = new Vector3(-0.2f, 0f, 0f);
            var rightFoot = new GameObject("Right Foot 070 Test").transform;
            rightFoot.SetParent(visualObject.transform, false);
            rightFoot.localPosition = new Vector3(0.2f, 0f, 0f);
            var motionAnimator = avatar.AddComponent<WorldCharacterAnimator070>();
            motionAnimator.Configure070(
                motor,
                visualObject.transform,
                null,
                leftFoot,
                rightFoot,
                false,
                true);
            yield return null;

            motor.Simulate070(Vector2.up, false, false, false, 0.05f);
            motionAnimator.Tick070(0.05f);
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Starting));
            Assert.That(visualObject.transform.localRotation, Is.Not.EqualTo(Quaternion.identity));

            for (var index = 0; index < 6; index++)
            {
                motor.Simulate070(Vector2.up, false, false, false, 0.05f);
                motionAnimator.Tick070(0.05f);
            }
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Walking));
            Assert.That(motionAnimator.HasVisibleGait070, Is.True);
            Assert.That(leftFoot.localPosition, Is.Not.EqualTo(rightFoot.localPosition));

            motor.Simulate070(Vector2.up, true, false, false, 0.05f);
            motionAnimator.Tick070(0.05f);
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Running));

            motor.Simulate070(Vector2.down, false, false, false, 0.05f);
            motionAnimator.Tick070(0.05f);
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Turning));

            motor.Simulate070(Vector2.zero, false, false, false, 0.10f);
            motionAnimator.Tick070(0.10f);
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Stopping));

            motor.StopImmediately070();
            motor.Simulate070(Vector2.right, false, true, false, 0.05f);
            motionAnimator.Tick070(0.05f);
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Dodging));
            Assert.That(visualObject.transform.localScale.y, Is.LessThan(visualObject.transform.localScale.x));

            for (var index = 0; index < 8; index++)
                motor.Simulate070(Vector2.zero, false, false, false, 0.05f);
            motor.Simulate070(Vector2.zero, false, false, true, 0.05f);
            motionAnimator.Tick070(0.05f);
            Assert.That(motionAnimator.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Interacting));

            Object.Destroy(avatar);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpeditionInteractionUsesPhysicalProximityAndMotorInteractionEvent()
        {
            var avatar = CreateAvatar070("Expedition Avatar 070 Test");
            avatar.transform.position = new Vector3(5f, 0f, 0f);
            var motor = avatar.GetComponent<WorldCharacterMotor070>();
            var spaceObject = new GameObject("Continuous Expedition Space 070 Test");
            var space = spaceObject.AddComponent<ExpeditionSpace070>();
            space.Configure070(new[]
            {
                new ExpeditionStationDefinition070(
                    "arrival",
                    "Arrival",
                    "Review route",
                    ExpeditionStationKind070.Arrival,
                    Vector3.zero,
                    1f),
                new ExpeditionStationDefinition070(
                    "creator console",
                    "Creator Console",
                    "Enter creator code",
                    ExpeditionStationKind070.CodeConsole,
                    new Vector3(5f, 0f, 0f),
                    1.5f),
                new ExpeditionStationDefinition070(
                    "battle approach",
                    "Gnawer Approach",
                    "Confront the patrol",
                    ExpeditionStationKind070.Encounter,
                    new Vector3(10f, 0f, 0f),
                    2f)
            }, avatar.transform, false);
            space.BindMotor070(motor);
            var visits = 0;
            var visitedId = string.Empty;
            space.RegisterInteraction070("creator console", marker =>
            {
                visits++;
                visitedId = marker.StationId070;
            });
            Assert.That(space.SetObjective070("battle approach"), Is.True);

            // The avatar position is changed after its CharacterController is
            // created. With automatic transform syncing disabled, a rendered
            // frame can let the motor move from the controller's stale native
            // x=0 pose. Publish the test-created controller and station triggers
            // before yielding so Update and the deterministic motor seam both
            // measure the authored x=5 position.
            Physics.SyncTransforms();
            yield return null;

            motor.Simulate070(Vector2.zero, false, false, true, 0.02f);
            Assert.That(visits, Is.EqualTo(1));
            Assert.That(visitedId, Is.EqualTo("CREATOR_CONSOLE"));
            Assert.That(motor.MotionState070, Is.EqualTo(WorldCharacterMotionState070.Interacting));
            Assert.That(space.TryGetObjectiveDirection070(out var direction, out var distance), Is.True);
            Assert.That(direction.x, Is.GreaterThan(0.9f));
            Assert.That(distance, Is.EqualTo(5f).Within(0.05f));

            Object.Destroy(spaceObject);
            Object.Destroy(avatar);
            yield return null;
        }

        private static GameObject CreateAvatar070(string objectName)
        {
            var avatar = new GameObject(objectName);
            var controller = avatar.AddComponent<CharacterController>();
            controller.radius = 0.35f;
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.25f;
            controller.skinWidth = 0.04f;
            var motor = avatar.AddComponent<WorldCharacterMotor070>();
            motor.Configure070(new WorldCharacterMotorTuning070(), false);
            return avatar;
        }
    }
}
