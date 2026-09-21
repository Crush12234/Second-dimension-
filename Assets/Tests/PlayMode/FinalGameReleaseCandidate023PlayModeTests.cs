#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Release023;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FinalGameReleaseCandidate023PlayModeTests
    {
        [UnityTest] public IEnumerator ReleaseResourcesAndSnapshotLoad()
        {
            yield return null;
            var registry=FinalReleaseRegistry023.LoadFromResources();
            var snapshot=FinalReleaseReadinessService023.BuildSnapshot();
            Assert.AreEqual(30,registry.SmokeCases.Count);
            Assert.IsTrue(snapshot.IsAvailable,snapshot.Error);
        }

        [UnityTest] public IEnumerator SmokeDashboardInitializesWithoutMutatingGameplay()
        {
            var go=new GameObject("Release Candidate 023 Test Dashboard");
            var dashboard=go.AddComponent<FinalGameSmokeDashboard023>();
            yield return null;
            Assert.IsTrue(dashboard.IsReady,dashboard.Error);
            Object.Destroy(go);
        }
    }
}
#endif
