using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class WorldInput071Tests
    {
        [Test]
        public void MovementSelectionPreservesLegacyKeyboardAndLetsActiveTouchTakeControl()
        {
            var keyboardOnly = WorldInput071.SelectMovement071(
                new Vector2(1f, 1f),
                new Vector2(-0.6f, 0f),
                Vector2.zero,
                out var keyboardSource);
            Assert.That(keyboardSource, Is.EqualTo(WorldInputSource071.Keyboard));
            Assert.That(keyboardOnly.magnitude, Is.EqualTo(1f).Within(0.001f),
                "Diagonal WASD must remain normalized rather than moving faster.");

            var touch = WorldInput071.SelectMovement071(
                Vector2.right,
                Vector2.left,
                new Vector2(0.2f, 1.4f),
                out var touchSource);
            Assert.That(touchSource, Is.EqualTo(WorldInputSource071.Touch));
            Assert.That(touch.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(touch.y, Is.GreaterThan(0.95f),
                "An active finger must not fight a connected controller's idle drift.");

            var belowDeadZone = WorldInput071.SelectMovement071(
                Vector2.zero,
                new Vector2(-0.72f, 0.18f),
                new Vector2(0.02f, 0.01f),
                out var controllerSource);
            Assert.That(controllerSource, Is.EqualTo(WorldInputSource071.Controller));
            Assert.That(belowDeadZone.x, Is.LessThan(-0.7f));
        }

        [Test]
        public void TouchRollAndActAreOneFrameEdgesWhileStickMovementIsHeld()
        {
            var host = new GameObject("World Input 071 Touch Edge Test");
            try
            {
                var input = host.AddComponent<WorldInput071>();
                input.SetTouchMovementForVerification071(new Vector2(0.25f, 0.94f));
                input.QueueTouchInteractForVerification071();
                input.QueueTouchDodgeForVerification071();

                var first = input.ReadTouchFrameForVerification071();
                var second = input.ReadTouchFrameForVerification071();

                Assert.That(first.Source071, Is.EqualTo(WorldInputSource071.Touch));
                Assert.That(first.InteractPressed071, Is.True);
                Assert.That(first.DodgePressed071, Is.True);
                Assert.That(first.RunHeld071, Is.True);
                Assert.That(first.Movement071.sqrMagnitude, Is.GreaterThan(0.8f));
                Assert.That(second.InteractPressed071, Is.False);
                Assert.That(second.DodgePressed071, Is.False);
                Assert.That(second.Movement071, Is.EqualTo(first.Movement071),
                    "The stick remains held until its pointer is released, while actions are pulses.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MobileHudBuildsNotchSafeStickAndLargeActionTargets()
        {
            var canvas = new GameObject("World Input 071 Canvas", typeof(RectTransform));
            var safe = new GameObject("World Input 071 Safe Area", typeof(RectTransform));
            safe.transform.SetParent(canvas.transform, false);
            var host = new GameObject("World Input 071 Host");
            try
            {
                var safeRect = safe.GetComponent<RectTransform>();
                safeRect.anchorMin = Vector2.zero;
                safeRect.anchorMax = Vector2.one;
                safeRect.offsetMin = Vector2.zero;
                safeRect.offsetMax = Vector2.zero;
                var input = host.AddComponent<WorldInput071>();
                input.BuildTouchControls071(safeRect, true, true);
                input.SetTouchControlsVisibleForVerification071(true);

                Assert.That(input.TouchControlsBuilt071, Is.True);
                Assert.That(input.TouchControlsVisible071, Is.True);
                Assert.That(input.TouchRootForVerification071.parent, Is.EqualTo(safeRect),
                    "Every touch control must remain inside the Safe Area used for notches and rounded screens.");
                var stick = input.TouchRootForVerification071
                    .GetComponentInChildren<WorldVirtualStick071>(true);
                Assert.That(stick, Is.Not.Null);
                Assert.That(((RectTransform)stick.transform).sizeDelta.x,
                    Is.GreaterThanOrEqualTo(300f));
                var buttons = input.TouchRootForVerification071
                    .GetComponentsInChildren<WorldTouchButton071>(true);
                Assert.That(buttons.Length, Is.EqualTo(4));
                foreach (var button in buttons)
                {
                    var size = ((RectTransform)button.transform).sizeDelta;
                    Assert.That(size.x, Is.GreaterThanOrEqualTo(WorldInput071.MinimumTouchTarget071));
                    Assert.That(size.y, Is.GreaterThanOrEqualTo(WorldInput071.MinimumTouchTarget071));
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
