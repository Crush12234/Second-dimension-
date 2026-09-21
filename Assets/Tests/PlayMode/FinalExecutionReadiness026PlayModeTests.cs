#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Release026;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FinalExecutionReadiness026PlayModeTests
    {
        [UnityTest]
        public IEnumerator ReadinessDashboardLoadsWithoutMutatingGameplay()
        {
            var go = new GameObject("Final Execution 026 Dashboard Test");
            var dashboard = go.AddComponent<FinalExecutionDashboard026>();
            yield return null;
            Assert.IsTrue(dashboard.IsReady, dashboard.Error);
            Object.Destroy(go);
        }
    }
}
#endif
