using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Campaign019;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class CampaignRuntime019PlayModeTests
    {
        [UnityTest]
        public IEnumerator RegistryAndCampaignResourcesLoadInPlayerContext()
        {
            var registry = CampaignRegistry019.LoadFromResources();
            Assert.NotNull(registry);
            Assert.AreEqual(82, registry.Chapters.Count);
            Assert.AreEqual(7, registry.Worlds.Count);
            yield return null;
        }
    }
}
