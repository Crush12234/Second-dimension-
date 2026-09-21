using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class WalkableGuildHall069PlayModeTests
    {
        [UnityTest]
        public IEnumerator HallProvidesMovementSixPhysicalDestinationsAndOneLiveObjective()
        {
            var host = new GameObject("Walkable Guild Hall 069 Test Host");
            var hall = host.AddComponent<WalkableGuildHall069>();
            var recruitmentVisits = 0;
            var quickPlayVisits = 0;
            var objective = new GuildHallObjective069(
                WalkableGuildHall069.RecruitmentDestinationId069,
                "Meet and recruit one permanent adventurer.");

            hall.Begin069(
                new GuildHallDestinationCallbacks069
                {
                    Recruitment = () => recruitmentVisits++,
                    QuickPlay = () => quickPlayVisits++
                },
                () => objective);
            yield return null;

            Assert.That(hall.IsActive069, Is.True);
            Assert.That(hall.UsesTrueWorldMovement069, Is.True);
            Assert.That(hall.HotspotCountForVerification069, Is.EqualTo(6));
            Assert.That(hall.CurrentObjectiveHotspotId069,
                Is.EqualTo(WalkableGuildHall069.RecruitmentDestinationId069));
            Assert.That(hall.WorldCameraForVerification069, Is.Not.Null);
            Assert.That(hall.WorldCameraForVerification069.orthographic, Is.True,
                "The Hall must keep the player and next destination on one readable plane.");
            Assert.That(hall.HudCanvasForVerification069, Is.Not.Null);
            Assert.That(hall.UsesFounderStandee069, Is.True,
                "The Hall must preserve the same authored Guildmaster identity established in the Market.");
            Assert.That(hall.FounderStandeeResourceKeyForVerification076,
                Is.EqualTo(M1VisualAssets.GuildmasterStandeeResourceKey076));
            Assert.That(hall.FounderPoseResourceRootForVerification076,
                Is.EqualTo(M1VisualAssets.GuildmasterPoseResourceRoot076));
            Assert.That(hall.ControlsTextForVerification076, Is.Not.Null);
            Assert.That(hall.ControlsTextForVerification076.fontSize,
                Is.GreaterThanOrEqualTo(22),
                "Hall movement and ACT bindings must remain legible at 1280x800.");
            if (!Application.isMobilePlatform)
                Assert.That(hall.DesktopControlsVisibleForVerification076, Is.True,
                    "Hall controls must remain visible instead of expiring while the player learns the space.");
            Assert.That(GameObject.Find("Guild Hall Route Strip 073"), Is.Not.Null);
            Assert.That(GameObject.Find("Guild Hall Gold Destination Pool 073"), Is.Not.Null);
            Assert.That(GameObject.Find("Readable Hall World Label 073"), Is.Not.Null,
                "The Hall must label the player and live destination in the world.");
            Assert.That(hall.HasQuickPlayForVerification070, Is.False,
                "Quick Play must not appear over the walkable Hall; practice is a physical destination.");
            Assert.That(hall.CurrentInteractionPromptForVerification071,
                Does.Contain("Meet and recruit one permanent adventurer"),
                "Mira's nearby line must follow the live objective instead of repeating opening-charter copy.");
            Assert.That(hall.UsesCrossPlatformWorldInput071, Is.True);
            Assert.That(hall.HasTouchControlsForVerification071, Is.True,
                "Mobile controls must be constructed inside the Hall safe area even when hidden on desktop.");
            if (!Application.isMobilePlatform)
                Assert.That(hall.WorldInputForVerification071.TouchControlsVisible071, Is.False,
                    "Desktop play must not boot with mobile touch controls covering the Hall.");
            var founderAnimator070 = hall.ControlledAvatar069
                .GetComponent<WorldCharacterAnimator070>();
            Assert.That(founderAnimator070, Is.Not.Null);
            Assert.That(founderAnimator070.UsesSpritePoses070, Is.True,
                "The controlled founder must use authored pose changes instead of a sliding standee.");
            hall.InvokeQuickPlayForVerification070();
            Assert.That(quickPlayVisits, Is.EqualTo(1));

            var before = hall.ControlledAvatar069.position;
            hall.ApplyMovementInput069(Vector2.right, false, 0.25f);
            Assert.That(hall.ControlledAvatar069.position.x, Is.GreaterThan(before.x));
            yield return null;
            var founderBillboard070 = GameObject.Find("Founder Camera-Facing Billboard Pivot 070");
            var founderVisual070 = GameObject.Find("Founder Vanguard Standee 069");
            Assert.That(founderBillboard070, Is.Not.Null);
            Assert.That(founderVisual070, Is.Not.Null);
            Assert.That(founderVisual070.transform.parent, Is.EqualTo(founderBillboard070.transform),
                "The gait animator must pose a child of the camera-facing pivot, never rotate the billboard itself.");
            Assert.That(Quaternion.Angle(
                    founderBillboard070.transform.rotation,
                    hall.WorldCameraForVerification069.transform.rotation),
                Is.LessThan(0.1f),
                "The founder standee must remain camera-facing after the motor turns its world root.");

            hall.SuspendInput069();
            var suspendedPosition = hall.ControlledAvatar069.position;
            hall.ApplyMovementInput069(Vector2.right, false, 0.25f);
            yield return null;
            Assert.That(hall.ControlledAvatar069.position.x,
                Is.EqualTo(suspendedPosition.x).Within(0.001f),
                "A modal must clear retained movement instead of sliding the avatar behind it.");
            Assert.That(hall.ControlledAvatar069.position.z,
                Is.EqualTo(suspendedPosition.z).Within(0.001f));
            hall.ResumeInput069();

            Assert.That(hall.TeleportToHotspotForVerification069(
                WalkableGuildHall069.RecruitmentDestinationId069), Is.True);
            Assert.That(hall.NearestInteractionIdForVerification069,
                Is.EqualTo(WalkableGuildHall069.RecruitmentDestinationId069));
            hall.ApplyInteractionInput069();
            Assert.That(recruitmentVisits, Is.EqualTo(1));

            objective = new GuildHallObjective069(
                WalkableGuildHall069.ContractDestinationId069,
                "Answer the rescue contract.");
            hall.Refresh069();
            Assert.That(hall.CurrentObjectiveHotspotId069,
                Is.EqualTo(WalkableGuildHall069.ContractDestinationId069));
            Assert.That(hall.TeleportToHotspotForVerification069(
                WalkableGuildHall069.GuideDestinationId069), Is.True);
            Assert.That(hall.CurrentInteractionPromptForVerification071,
                Does.Contain("Answer the rescue contract"));

            hall.Shutdown069();
            Object.Destroy(host);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<WalkableGuildHall069>(), Is.Null);
        }
    }
}
