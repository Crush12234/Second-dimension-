using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2BattleAudio021Tests
    {
        private static readonly string[] RuntimeBattleCueIds =
        {
            "AUDIO_BATTLE",
            "SFX_COMMAND_SELECT",
            "SFX_COMMAND_CONFIRM",
            "SFX_WEAPON_IMPACT",
            "SFX_MYSTIC",
            "SFX_RESTORATION",
            "SFX_GUARD",
            "SFX_GUARD_IMPACT",
            "SFX_RECOVERY",
            "SFX_FORMATION",
            "SFX_WHOOSH",
            "SFX_DOWNED",
            "SFX_LEARNING",
            "SFX_BREAKTHROUGH",
            "AUDIO_RETREAT",
            "AUDIO_VICTORY"
        };

        [Test]
        public void EveryRuntimeBattleCueSynthesizesARealFiniteSignal()
        {
            foreach (var cueId in RuntimeBattleCueIds)
            {
                var samples = M2BattleAudioDirector.SynthesizeCueSamples(cueId);
                Assert.That(samples.Length, Is.GreaterThan(1000), cueId);
                Assert.That(samples.All(value => !float.IsNaN(value) && !float.IsInfinity(value)), Is.True, cueId);
                Assert.That(samples.Max(value => Math.Abs(value)), Is.GreaterThan(0.05f),
                    cueId + " synthesized silence.");
                Assert.That(samples.Any(value => value > 0f), Is.True, cueId);
                Assert.That(samples.Any(value => value < 0f), Is.True, cueId);
            }
        }

        [Test]
        public void CueSynthesisIsDeterministicAndBlankCueIsRejected()
        {
            var first = M2BattleAudioDirector.SynthesizeCueSamples("SFX_WEAPON_IMPACT");
            var second = M2BattleAudioDirector.SynthesizeCueSamples("SFX_WEAPON_IMPACT");

            Assert.That(second, Is.EqualTo(first));
            Assert.That(M2BattleAudioDirector.SynthesizeCueSamples(string.Empty), Is.Empty);
            Assert.That(M2BattleAudioDirector.SynthesizeCueSamples("   "), Is.Empty);
        }
    }
}
