using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Release025;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FinalImplementationHardening025PlayModeTests
    {
        [UnityTest]
        public IEnumerator DashboardLoadsAndReportsReady()
        {
            var root = new GameObject("Final Implementation Hardening 025 Test");
            var dashboard = root.AddComponent<ImplementationHardeningDashboard025>();
            yield return null;
            Assert.That(dashboard.IsReady, Is.True, dashboard.Error);
            Object.Destroy(root);
        }

        [Test]
        public void RuntimeReadinessUsesBuiltInFontAndFullWorldGateCoverage()
        {
            var snapshot = ImplementationReadinessService025.BuildSnapshot();
            Assert.That(snapshot.IsReady, Is.True, snapshot.Error);
            Assert.That(snapshot.BuiltInFontReady, Is.True);
            Assert.That(snapshot.EmbeddedUiFontAbsent, Is.True);
            Assert.That(snapshot.EmbeddedDisplayFontAbsent, Is.True);
            Assert.That(snapshot.WorldGateBoards, Is.EqualTo(130));
            Assert.That(snapshot.WorldGateNodes, Is.EqualTo(1372));
        }
    }
}
