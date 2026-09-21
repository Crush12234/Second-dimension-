using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.FirstHour072;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FirstHourWorldStage076PlayModeTests
    {
        [Test]
        public void GuidedFieldBindsDistinctShippingPlatesToStoryLandmarks076()
        {
            var resources076 = new[]
            {
                OuterGateworksExploration066.BackdropResourceForBeatForVerification076(
                    "HallBreach", "N01"),
                OuterGateworksExploration066.BackdropResourceForBeatForVerification076(
                    "LanternRoad", "N04"),
                OuterGateworksExploration066.BackdropResourceForBeatForVerification076(
                    "LanternRoad", "N06"),
                OuterGateworksExploration066.BackdropResourceForBeatForVerification076(
                    "PatrolRescue", "N13"),
                OuterGateworksExploration066.BackdropResourceForBeatForVerification076(
                    "Gatehouse", "N13"),
                OuterGateworksExploration066.BackdropResourceForBeatForVerification076(
                    "ReturnRoad", "N14")
            };

            Assert.That(resources076.Distinct().Count(), Is.EqualTo(5),
                "Hall, open road, ambush/patrol, Gatehouse, and Skyhome return must not reuse one repeating screen.");
            foreach (var resource076 in resources076.Distinct())
                Assert.That(Resources.Load<Texture2D>(resource076), Is.Not.Null,
                    "Every field variation must bind an existing shipping texture: " + resource076);
        }

        [UnityTest]
        public IEnumerator AuthoredBackdropCoversSupportedResolutionsAndMovesInParallax076()
        {
            foreach (var aspect076 in new[] { 1280f / 800f, 1920f / 1080f })
            {
                var root076 = new GameObject(
                    "First Hour Stage Aspect " + aspect076.ToString("0.000") + " 076");
                var cameraRoot076 = new GameObject("First Hour Stage Camera 076");
                cameraRoot076.transform.SetParent(root076.transform, false);
                var camera076 = cameraRoot076.AddComponent<Camera>();
                camera076.enabled = false;
                camera076.aspect = aspect076;
                var focus076 = new GameObject("First Hour Stage Focus 076").transform;
                focus076.SetParent(root076.transform, false);
                focus076.position = new Vector3(0f, 0.20f, -0.90f);
                var stage076 = root076.AddComponent<FirstHourWorldStage072>();

                stage076.Configure072(
                    camera076,
                    focus076,
                    29,
                    OuterGateworksExploration066.LanternRoadBackdropResource076,
                    OuterGateworksExploration066.LanternAmbushBackdropResource076,
                    new Vector2(-6.4f, 6.4f),
                    new Vector2(-2.0f, 2.25f),
                    new Vector3(0f, 5.65f, -12.6f),
                    new Vector3(0f, 2.35f, 0.65f),
                    4.78f,
                    1.10f,
                    1.35f,
                    0.24f);
                yield return null;

                Assert.That(stage076.HasWorldAnchoredBackdrop072, Is.True);
                Assert.That(stage076.CoversViewportAtAspect072(aspect076), Is.True,
                    "The authored plate must cover the full field at " + aspect076.ToString("0.000") + " aspect.");
                Assert.That(stage076.UsesBackdropParallax072, Is.True);

                var cameraStart076 = camera076.transform.position;
                var backdropStart076 = stage076.Backdrop072.position;
                focus076.position = new Vector3(6.0f, 0.20f, -0.90f);
                for (var frame076 = 0; frame076 < 180; frame076++)
                    stage076.TickCamera072(1f / 60f);

                var cameraTravel076 = Vector3.Distance(
                    cameraStart076,
                    camera076.transform.position);
                var backdropTravel076 = Vector3.Distance(
                    backdropStart076,
                    stage076.Backdrop072.position);
                Assert.That(cameraTravel076, Is.GreaterThan(0.5f));
                Assert.That(backdropTravel076, Is.GreaterThan(0.05f),
                    "The skyline must drift independently instead of reading as a static repeated card.");
                Assert.That(backdropTravel076, Is.LessThan(cameraTravel076 * 0.5f),
                    "The distant plate should move more slowly than the foreground party.");

                UnityEngine.Object.Destroy(root076);
                yield return null;
            }
        }
    }
}
