#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Release030;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FinalSupersessionPipeline030PlayModeTests
    {
        [UnityTest]
        public IEnumerator ActiveReleaseSnapshotBuildsInPlayerContext()
        {
            yield return null;
            var snapshot = ReleaseSupersessionReadinessService030.BuildSnapshot();
            Assert.IsTrue(snapshot.IsReady, snapshot.Error);
            Assert.AreEqual(11, snapshot.SaveFormatVersion);
        }
    }
}
#endif
