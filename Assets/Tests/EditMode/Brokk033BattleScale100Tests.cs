using System;
using System.IO;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class Brokk033BattleScale100Tests
    {
        const string Identity = "HERO_REC_033";
        const string SourceSha = "44E02BA541ADD47F51302EABDFAF678530045FC5955FB35F7B82BF7C7F6CA887";

        [TestCase(420f, 260f, 620f)]
        [TestCase(230f, 130f, 380f)]
        [TestCase(420f, 90f, 170f)]
        public void ShippingRigRetainsBrokkSourcePixelScaleAndGrounding100(
            float height, float idleWidth, float actionWidth)
        {
            Assert.That(HeroRemasterAtlas093.UsesIdlePixelScale100(Identity), Is.True);
            new ElaraBattleScale099Tests().ActualRigUsesIdlePixelScaleForExactReviewedAction100(
                Identity, height, idleWidth, actionWidth);
        }

        [Test]
        public void ExactUneditedBrokkAlpha774To723DoesNotInflateHisActionBody100()
        {
            var path = Path.Combine(Application.dataPath, "Resources", "SecondDimension", "Art",
                "Standees", "HeroRemaster093", Identity + "_PAIR_093.png");
            using (var sha = System.Security.Cryptography.SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""),
                    Is.EqualTo(SourceSha), "This is presentation calibration, not edited or substituted art.");
            Assert.That(HeroRemasterAtlas093.TryResolve093(Identity, false, out var idle, out var idleKey), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(Identity, true, out var action, out var actionKey), Is.True);
            Assert.That(idleKey, Is.EqualTo(HeroRemasterAtlas093.Root093 + Identity + "_PAIR_093#IDLE"));
            Assert.That(actionKey, Is.EqualTo(HeroRemasterAtlas093.Root093 + Identity + "_PAIR_093#ACTION"));
            Assert.That(idle.texture, Is.SameAs(action.texture));
            Assert.That(action, Is.Not.SameAs(idle));
            var idleAlpha = M1SilhouetteFraming091.VisibleRect091(idle);
            var actionAlpha = M1SilhouetteFraming091.VisibleRect091(action);
            Assert.That(idleAlpha.size, Is.EqualTo(new Vector2(655, 774)));
            Assert.That(actionAlpha.size, Is.EqualTo(new Vector2(743, 723)));
            var scale = HeroRemasterAtlas093.CalibrateBattlePixelScale099(action, 420f / 723f, 420, 1000);
            Assert.That(scale, Is.EqualTo(420f / 774f).Within(0.0001f));
            Assert.That(actionAlpha.height * scale / (idleAlpha.height * scale),
                Is.EqualTo(723f / 774f).Within(0.0001f),
                "Brokk may brace lower; keep his existing source pixel scale, not equal silhouette heights.");
            Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(idle, 0.25f, 420, 1000),
                Is.EqualTo(0.25f), "Idle is never an action calibration authority.");
        }

        [Test]
        public void CopiedSameTextureSpriteCannotBorrowBrokkCalibrationAuthority100() =>
            new ElaraBattleScale099Tests().SameTextureCellCopyIsNotCalibrationAuthority099(Identity);

        [Test]
        public void CalibrationRemainsExactAndOtherHeroesKeepTheirPriorFit100()
        {
            foreach (var identity in new[] { "HERO_REC_011", Identity, "HERO_REC_041", "HERO_REC_195" })
                Assert.That(HeroRemasterAtlas093.UsesIdlePixelScale100(identity), Is.True, identity);
            foreach (var identity in new[] { "HERO_REC_032", "HERO_REC_034", "HERO_REC_186",
                "HERO_REC_033_SUFFIX", "DWARF", "UNKNOWN", string.Empty, null })
                Assert.That(HeroRemasterAtlas093.UsesIdlePixelScale100(identity), Is.False, identity);
            new ElaraBattleScale099Tests().OtherExactHeroesKeepOriginalPoseFit099("HERO_REC_016");
        }
    }
}
