using System;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerEconomy159Tests
    {
        [TestCase(1,0L)] [TestCase(10,1800L)] [TestCase(81,16000L)] [TestCase(372,74200L)]
        public void ActualDepthHasAFixedRewardRate(int floor,long bonus)
        {
            Assert.That(TowerEconomy159.BonusBasisPoints159(floor),Is.EqualTo(bonus));
            Assert.That(TowerEconomy159.ApplyBonus159(10000,bonus),Is.EqualTo(10000+bonus));
        }
        [Test] public void OldCommitmentsAndUnboundNewMarkersCannotGainDepthRewards()
        {
            Assert.That(TowerEconomy159.ReadBonus159(Array.Empty<string>()),Is.Zero);
            Assert.That(TowerEconomy159.ReadBonus159(new[]{TowerScalingRules138.Modifier138}),Is.Zero);
            Assert.Throws<InvalidOperationException>(()=>TowerEconomy159.ReadBonus159(new[]{TowerEconomy159.Modifier159}));
        }
        [Test] public void IntegerLimitsNeverWrapDepthOrXp()
        {
            Assert.That(TowerEconomy159.BonusBasisPoints159(int.MaxValue),Is.EqualTo(429496729200L));
            Assert.That(TowerEconomy159.ApplyBonus159(long.MaxValue,0),Is.EqualTo(long.MaxValue));
            Assert.Throws<OverflowException>(()=>TowerEconomy159.ApplyBonus159(long.MaxValue,200));
        }
    }
}
