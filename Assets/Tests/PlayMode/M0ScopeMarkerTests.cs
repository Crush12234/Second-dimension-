using NUnit.Framework;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M0ScopeMarkerTests
    {
        [Test]
        public void M0HasNoGameplayAcceptanceClaim()
        {
            Assert.Pass("PlayMode gameplay begins at M1. M0 contains only the Boot validation proof.");
        }
    }
}

