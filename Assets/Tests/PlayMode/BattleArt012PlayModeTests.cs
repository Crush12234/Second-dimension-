using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Battle.ArtProduction012;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleArt012PlayModeTests
    {
        [UnityTest]
        public IEnumerator ShowcaseBuildsWithoutChangingAnyGameplayService()
        {
            GameObject root = new GameObject("Art012 PlayMode Test");
            root.AddComponent<ThursdayArtShowcase012>();
            yield return null;
            Assert.That(root.transform.childCount, Is.GreaterThanOrEqualTo(9));
            Object.Destroy(root);
            yield return null;
        }
    }
}
