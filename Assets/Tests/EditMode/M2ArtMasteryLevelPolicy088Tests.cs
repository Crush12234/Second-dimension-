using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2ArtMasteryLevelPolicy088Tests
    {
        [Test]
        public void TenMonotonicThresholdsMapExactlyAndCapAtLevelTen088()
        {
            var thresholds = M2ArtMasteryLevelPolicy088.MasteryThresholds.ToArray();

            Assert.That(thresholds, Is.EqualTo(new[]
            {
                0, 100, 260, 520, 900, 1400, 2000, 2750, 3650, 4700
            }));
            Assert.That(thresholds, Is.Ordered.Ascending);
            for (var level = M2ArtMasteryLevelPolicy088.MinimumLevel;
                 level <= M2ArtMasteryLevelPolicy088.MaximumLevel;
                 level++)
            {
                var threshold = M2ArtMasteryLevelPolicy088.ThresholdForLevel(level);
                Assert.That(
                    M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(threshold),
                    Is.EqualTo(level));
                if (level > M2ArtMasteryLevelPolicy088.MinimumLevel)
                    Assert.That(
                        M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(threshold - 1),
                        Is.EqualTo(level - 1));
            }

            Assert.That(
                M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(-1),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.MinimumLevel));
            Assert.That(
                M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(int.MaxValue),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.MaximumLevel));
            Assert.That(
                M2ArtMasteryLevelPolicy088.PowerPermilleForMasteryPoints(0),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.BasePowerPermille));
            Assert.That(
                M2ArtMasteryLevelPolicy088.PowerPermilleForMasteryPoints(int.MaxValue),
                Is.EqualTo(M2ArtMasteryLevelPolicy088.MaximumPowerPermille));
        }

        [Test]
        public void ProgressAndPowerAreDeterministicAtIntermediateAndMaximumLevels088()
        {
            var halfwayThroughLevelTwo =
                M2ArtMasteryLevelPolicy088.ProgressForMasteryPoints(180);

            Assert.That(halfwayThroughLevelTwo.Level, Is.EqualTo(2));
            Assert.That(halfwayThroughLevelTwo.CurrentLevelThreshold, Is.EqualTo(100));
            Assert.That(halfwayThroughLevelTwo.NextLevelThreshold, Is.EqualTo(260));
            Assert.That(halfwayThroughLevelTwo.MasteryIntoLevel, Is.EqualTo(80));
            Assert.That(halfwayThroughLevelTwo.MasteryRequiredForNextLevel, Is.EqualTo(160));
            Assert.That(halfwayThroughLevelTwo.ProgressBasisPoints, Is.EqualTo(5000));
            Assert.That(halfwayThroughLevelTwo.IsMaximumLevel, Is.False);

            var capped = M2ArtMasteryLevelPolicy088.ProgressForMasteryPoints(9999);
            Assert.That(capped.Level, Is.EqualTo(10));
            Assert.That(capped.NextLevelThreshold, Is.Zero);
            Assert.That(capped.MasteryRequiredForNextLevel, Is.Zero);
            Assert.That(capped.ProgressBasisPoints, Is.EqualTo(10000));
            Assert.That(capped.IsMaximumLevel, Is.True);

            Assert.That(M2ArtMasteryLevelPolicy088.ScaleMagnitude(100, 0), Is.EqualTo(100));
            Assert.That(M2ArtMasteryLevelPolicy088.ScaleMagnitude(100, 4700), Is.EqualTo(154));
        }
    }
}
