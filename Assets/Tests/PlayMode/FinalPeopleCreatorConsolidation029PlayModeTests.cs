using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Release029;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FinalPeopleCreatorConsolidation029PlayModeTests
    {
        [UnityTest] public IEnumerator PeopleCreatorResourcesLoadInPlayerContext()
        {
            var snapshot=PeopleCreatorReadinessService029.BuildSnapshot();Assert.That(snapshot.IsReady,Is.True,snapshot.Error);Assert.That(snapshot.Manifest.saveFormatVersion,Is.EqualTo(11));yield return null;
        }
    }
}
