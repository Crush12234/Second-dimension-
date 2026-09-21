using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour071;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class WalkableSkyhomeArrival071PlayModeTests
    {
        [UnityTest]
        public IEnumerator ArrivalGivesControlBeforeCharterAndReachesHallThroughOneWorldAction()
        {
            var host071 = new GameObject("Walkable Skyhome Arrival 071 Test Host");
            var arrival071 = host071.AddComponent<WalkableSkyhomeArrival071>();
            var reachedHall071 = 0;
            var returned071 = 0;
            arrival071.Begin071(() => reachedHall071++, () => returned071++);

            // The player can act on the same rendered frame that the runtime-built
            // Market opens. With automatic physics transform syncing disabled, this
            // step guards the authored floor/boundaries being published before input.
            var sameFramePosition071 = arrival071.ControlledAvatar071.position;
            var sameFrameDistance071 = arrival071.CurrentObjectiveDistanceForVerification076;
            arrival071.ApplyMovementForVerification071(
                arrival071.CurrentObjectiveDirectionForVerification076,
                true,
                false,
                0.05f);
            Assert.That(arrival071.ControlledAvatar071.position.x,
                Is.GreaterThan(sameFramePosition071.x),
                "The Market must accept movement before its first rendered-frame physics sync.");
            Assert.That(arrival071.CurrentObjectiveDistanceForVerification076,
                Is.LessThan(sameFrameDistance071));
            yield return null;

            Assert.That(arrival071.IsActive071, Is.True);
            Assert.That(arrival071.UsesAuthoredSkyhomeArt071, Is.True);
            Assert.That(arrival071.HasMarenCompanion071, Is.True);
            Assert.That(arrival071.UsesCrossPlatformWorldInput071, Is.True);
            Assert.That(arrival071.HasTouchControls071, Is.True);
            Assert.That(arrival071.WorldCamera071, Is.Not.Null);
            Assert.That(arrival071.WorldCamera071.orthographic, Is.True,
                "The Market must keep the authored plate edge-to-edge without exposing a tilted void.");
            Assert.That(arrival071.HudCanvas071, Is.Not.Null);
            Assert.That(arrival071.UsesAuthoredMarenPortrait071, Is.True);
            Assert.That(arrival071.UsesAuthoredGuildmasterStandee076, Is.True,
                "The controlled Guildmaster must use the dedicated authored identity, not a recruit's standee.");
            Assert.That(arrival071.GuildmasterStandeeResourceKeyForVerification076,
                Is.EqualTo(M1VisualAssets.GuildmasterStandeeResourceKey076));
            Assert.That(arrival071.GuildmasterPoseResourceRootForVerification076,
                Is.EqualTo(M1VisualAssets.GuildmasterPoseResourceRoot076));
            Assert.That(arrival071.StoryDialogueCard071, Is.Not.Null);
            Assert.That(arrival071.StoryDialogueCard071.anchorMin.x, Is.GreaterThanOrEqualTo(0.025f));
            Assert.That(arrival071.StoryDialogueCard071.anchorMax.x, Is.LessThanOrEqualTo(0.975f));
            Assert.That(arrival071.StoryDialogueCard071.anchorMin.y, Is.GreaterThanOrEqualTo(0.18f));
            Assert.That(
                arrival071.StoryDialogueCard071.anchorMax.y -
                arrival071.StoryDialogueCard071.anchorMin.y,
                Is.GreaterThanOrEqualTo(0.19f),
                "The opening story strip needs enough vertical space for readable dialogue and progress.");
            Assert.That(arrival071.StoryDialogueCard071.IsChildOf(
                arrival071.HudCanvas071.transform.Find("Safe Area")), Is.True);
            Assert.That(arrival071.StorySpeaker071, Is.EqualTo("MAREN HOLT  •  LANTERN GUIDE"));
            Assert.That(arrival071.CurrentStoryBeat071, Is.EqualTo(1));
            Assert.That(arrival071.StoryProgress071, Does.Contain("1 OF 3"));
            Assert.That(arrival071.StoryBody071, Does.Contain("sealed bell"));
            Assert.That(arrival071.StoryProgressTextForVerification076.fontSize,
                Is.GreaterThanOrEqualTo(20),
                "The story beat counter must remain legible in the 1280x800 studio capture.");
            Assert.That(arrival071.DesktopControlsTextForVerification076.fontSize,
                Is.GreaterThanOrEqualTo(24));
            if (!Application.isMobilePlatform)
            {
                Assert.That(arrival071.DesktopControlsPanelForVerification076.gameObject.activeSelf,
                    Is.True);
                arrival071.DesktopControlsPanelForVerification076.gameObject.SetActive(false);
                arrival071.RefreshControlLegendForVerification076();
                Assert.That(arrival071.DesktopControlsPanelForVerification076.gameObject.activeSelf,
                    Is.True,
                    "Desktop movement and action bindings must remain persistent instead of expiring on a timer.");
            }
            Assert.That(GameObject.Find("Guild Hall Destination Billboard 073"), Is.Not.Null);
            Assert.That(GameObject.Find("Skyhome Arrival Route Strip 073"), Is.Not.Null);
            Assert.That(GameObject.Find("Readable World Label 073"), Is.Not.Null,
                "The controlled avatar and destination need large world labels.");
            var guildmasterAnimator071 = arrival071.ControlledAvatar071
                .GetComponent<WorldCharacterAnimator070>();
            var guildmasterMotor071 = arrival071.ControlledAvatar071
                .GetComponent<WorldCharacterMotor070>();
            var maren071 = GameObject.Find("Maren Holt Walking Companion 071");
            var marenAnimator071 = maren071 != null
                ? maren071.GetComponent<WorldCharacterAnimator070>()
                : null;
            Assert.That(guildmasterAnimator071, Is.Not.Null);
            Assert.That(guildmasterMotor071, Is.Not.Null);
            Assert.That(marenAnimator071, Is.Not.Null);
            var guildmasterStandee071 = GameObject.Find("Guildmaster Standee 071")
                ?.GetComponent<SpriteRenderer>()?.sprite;
            var marenStandee071 = GameObject.Find("Maren Holt Companion Standee 071")
                ?.GetComponent<SpriteRenderer>()?.sprite;
            Assert.That(guildmasterStandee071, Is.Not.Null);
            Assert.That(marenStandee071, Is.Not.Null);
            Assert.That(guildmasterStandee071.texture, Is.Not.SameAs(marenStandee071.texture),
                "YOU and Maren must remain visually distinct before the player meets the recruit roster.");
            Assert.That(guildmasterStandee071.texture, Is.Not.SameAs(marenStandee071.texture),
                "The controlled Guildmaster must not borrow Maren's or another recruit's painted source.");
            Assert.That(guildmasterAnimator071.UsesSpritePoses070, Is.True);
            Assert.That(marenAnimator071.UsesSpritePoses070, Is.True,
                "The controlled Guildmaster and Maren must use authored pose changes instead of sliding standees.");
            Assert.That(arrival071.CurrentObjectivePositionForVerification076.x,
                Is.GreaterThan(arrival071.ControlledAvatar071.position.x));
            Assert.That(arrival071.CurrentObjectiveDistanceForVerification076,
                Is.GreaterThan(arrival071.CurrentObjectiveInteractionRadiusForVerification076));
            Assert.That(arrival071.CurrentObjectiveDirectionForVerification076.sqrMagnitude,
                Is.EqualTo(1f).Within(0.001f));

            var before071 = arrival071.ControlledAvatar071.position;
            arrival071.ApplyMovementForVerification071(
                arrival071.CurrentObjectiveDirectionForVerification076,
                true,
                false,
                0.25f);
            Assert.That(arrival071.ControlledAvatar071.position.x, Is.GreaterThan(before071.x));
            Assert.That(reachedHall071, Is.Zero);
            Assert.That(returned071, Is.Zero);

            for (var storyStep071 = 0;
                 storyStep071 < 160 && arrival071.CurrentStoryBeat071 < 2;
                 storyStep071++)
                arrival071.ApplyMovementForVerification071(
                    arrival071.CurrentObjectiveDirectionForVerification076,
                    true,
                    false,
                    0.05f);
            Assert.That(arrival071.CurrentStoryBeat071, Is.EqualTo(2));
            Assert.That(arrival071.StoryProgress071, Does.Contain("2 OF 3"));
            Assert.That(arrival071.StoryBody071, Does.Contain("Our guild protects"));
            var afterMidpoint071 = arrival071.ControlledAvatar071.position.x;
            arrival071.ApplyMovementForVerification071(
                arrival071.CurrentObjectiveDirectionForVerification076,
                true,
                false,
                0.10f);
            Assert.That(arrival071.ControlledAvatar071.position.x, Is.GreaterThan(afterMidpoint071),
                "Dialogue progression must not lock player-controlled walking.");

            var largestMotorStep071 = 0f;
            // Keep a generous deterministic step ceiling for full-suite runs,
            // where the CharacterController can begin with different velocity
            // carry-in after domain/scene churn. Per-step displacement remains
            // independently capped below, so this cannot conceal a teleport.
            for (var step071 = 0;
                 step071 < 320 &&
                 !arrival071.IsWithinCurrentObjectiveInteractionRangeForVerification076;
                 step071++)
            {
                var previous071 = arrival071.ControlledAvatar071.position;
                arrival071.ApplyMovementForVerification071(
                    arrival071.CurrentObjectiveDirectionForVerification076,
                    true,
                    false,
                    0.05f);
                var displacement071 = arrival071.ControlledAvatar071.position - previous071;
                displacement071.y = 0f;
                largestMotorStep071 = Mathf.Max(largestMotorStep071, displacement071.magnitude);
            }
            var finalPosition071 = arrival071.ControlledAvatar071.position;
            var finalDistance071 = arrival071.CurrentObjectiveDistanceForVerification076;
            var finalCollisionFlags071 = guildmasterMotor071.LastCollisionFlags070;
            Assert.That(arrival071.IsWithinCurrentObjectiveInteractionRangeForVerification076,
                Is.True,
                "The real Market motor must reach the Hall interaction radius from spawn without a teleport. " +
                "Final position=" + finalPosition071.ToString("F3") +
                ", distance=" + finalDistance071.ToString("F3") +
                ", radius=" + arrival071.CurrentObjectiveInteractionRadiusForVerification076.ToString("F3") +
                ", collision flags=" + finalCollisionFlags071 + ".");
            Assert.That(arrival071.CurrentObjectiveDistanceForVerification076,
                Is.LessThanOrEqualTo(arrival071.CurrentObjectiveInteractionRadiusForVerification076));
            Assert.That(largestMotorStep071, Is.LessThan(0.55f),
                "Certification movement must remain frame-sized rather than hiding a teleport.");
            Assert.That(arrival071.CurrentStoryBeat071, Is.EqualTo(3));
            Assert.That(arrival071.StoryProgress071, Does.Contain("3 OF 3"));
            Assert.That(arrival071.StoryBody071, Does.Contain("emergency charter"));
            arrival071.ApplyInteractionForVerification071();
            Assert.That(reachedHall071, Is.EqualTo(1));

            arrival071.Shutdown071();
            Object.Destroy(host071);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<WalkableSkyhomeArrival071>(), Is.Null);
        }
    }
}
