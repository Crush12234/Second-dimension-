using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2ArtLevelPresentation089Tests
    {
        [Test]
        public void EveryLevelIncreasesEffectAndMotionWithoutExceedingTheLevelTenCap()
        {
            var previousEffect = 0f;
            var previousMotion = 0f;
            for (var level = 1; level <= 10; level++)
            {
                var effect = M2ArtLevelPresentation089.EffectScale(level);
                var motion = M2ArtLevelPresentation089.MotionScale(level);
                Assert.That(effect, Is.GreaterThan(previousEffect));
                Assert.That(motion, Is.GreaterThan(previousMotion));
                previousEffect = effect;
                previousMotion = motion;
            }

            Assert.That(M2ArtLevelPresentation089.EffectScale(999),
                Is.EqualTo(M2ArtLevelPresentation089.EffectScale(10)));
            Assert.That(M2ArtLevelPresentation089.MotionScale(999),
                Is.EqualTo(M2ArtLevelPresentation089.MotionScale(10)));
        }

        [TestCase(1, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 2)]
        [TestCase(9, 2)]
        [TestCase(10, 3)]
        public void MilestonesAddDistinctImpactPulsePasses(int level, int expected)
        {
            Assert.That(M2ArtLevelPresentation089.AccentPulseCount(level), Is.EqualTo(expected));
        }

        [Test]
        public void CommittedForecastIsTheOnlyLevelSource()
        {
            var view = new M2BattleView
            {
                Forecasts = new[]
                {
                    new M2ForecastView
                    {
                        MemberActions = new[]
                        {
                            new M2PredictedActionView
                            {
                                ActorMemberId = "HERO_A",
                                ArtId = "ART_PIERCING_SHOT",
                                ArtLevel = 7
                            }
                        }
                    }
                }
            };

            Assert.That(M2ArtLevelPresentation089.ResolveCommittedLevel(
                view, "HERO_A", "ART_PIERCING_SHOT"), Is.EqualTo(7));
            Assert.That(M2ArtLevelPresentation089.ResolveCommittedLevel(
                view, "HERO_B", "ART_PIERCING_SHOT"), Is.EqualTo(1));
            Assert.That(M2ArtLevelPresentation089.ResolveCommittedLevel(
                view, "HERO_A", "ART_OTHER"), Is.EqualTo(1));
        }

        [Test]
        public void CaptionMakesMilestoneStrengthReadable()
        {
            Assert.That(M2ArtLevelPresentation089.CaptionSuffix(1), Is.Empty);
            Assert.That(M2ArtLevelPresentation089.CaptionSuffix(4), Does.Contain("REFINED"));
            Assert.That(M2ArtLevelPresentation089.CaptionSuffix(7), Does.Contain("SURGING"));
            Assert.That(M2ArtLevelPresentation089.CaptionSuffix(10), Does.Contain("MASTERED"));
        }
    }
}
