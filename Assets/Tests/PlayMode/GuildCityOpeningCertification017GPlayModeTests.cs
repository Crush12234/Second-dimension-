
using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017G;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class GuildCityOpeningCertification017GPlayModeTests
    {
        [UnityTest]
        public IEnumerator CertificationRegistryLoadsInPlayerContext()
        {
            var root = GuildCityOpeningCertificationRegistry017G.Load();
            Assert.NotNull(root);
            Assert.AreEqual("GUILD_CITY_OPENING_CERTIFICATION_017G_1.0", root.contentVersion);
            Assert.NotNull(GuildCityOpeningCertificationRegistry017G.Step("GUIDE_ENTER_BATTLE"));
            yield return null;
        }
    }
}
