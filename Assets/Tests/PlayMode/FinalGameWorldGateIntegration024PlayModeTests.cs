#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Release024;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FinalGameWorldGateIntegration024PlayModeTests
    {
        [UnityTest]
        public IEnumerator IntegrationResourcesLoadInPlayerContext()
        {
            yield return null;
            var registry = FullGameIntegrationRegistry024.LoadFromResources();
            var health = FullGameIntegrationHealthService024.BuildSnapshot();
            Assert.AreEqual(36, registry.SmokeCases.Count);
            Assert.IsTrue(health.IsReady, health.Error);
            Assert.AreEqual(130, health.WorldGateBoards);
            Assert.AreEqual(1372, health.WorldGateNodes);
        }

        [UnityTest]
        public IEnumerator IntegrationDashboardInitializesWithoutMutatingGameplay()
        {
            var go = new GameObject("Final Integration 024 Test Dashboard");
            var dashboard = go.AddComponent<FullGameIntegrationDashboard024>();
            yield return null;
            Assert.IsTrue(dashboard.IsReady, dashboard.Error);
            Object.Destroy(go);
        }
    }
}
#endif
