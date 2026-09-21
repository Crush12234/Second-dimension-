using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroPoseHeightCalibration099Tests
    {
        [TestCase("HERO_REC_041", 922, 755)]
        [TestCase("HERO_REC_195", 841, 734)]
        public void NewNativeReviewedPairsUseTheirExactPoseRatio100(string identity, int idlePixels, int actionPixels)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var idle, out _), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var action, out _), Is.True);
            Assert.That(M1SilhouetteFraming091.VisibleRect091(idle).height, Is.EqualTo(idlePixels));
            Assert.That(M1SilhouetteFraming091.VisibleRect091(action).height, Is.EqualTo(actionPixels));
            Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099(identity, true, action),
                Is.EqualTo(0.28f * actionPixels / idlePixels).Within(0.000001f));
            Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099(identity, false, idle), Is.EqualTo(0.28f));
            Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099("HERO_REC_011", true, action), Is.EqualTo(0.28f));
        }

        [Test]
        public void ExactElaraActionUsesSameHeightRatioAsShippingRig099()
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093("HERO_REC_011", false, out var idle, out _), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093("HERO_REC_011", true, out var action, out _), Is.True);
            var idleHeight = M1SilhouetteFraming091.VisibleRect091(idle).height;
            var actionHeight = M1SilhouetteFraming091.VisibleRect091(action).height;
            Assert.That(idleHeight, Is.EqualTo(945));
            Assert.That(actionHeight, Is.EqualTo(720));
            var minimum = HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099("HERO_REC_011", true, action);
            Assert.That(minimum, Is.EqualTo(0.28f * actionHeight / idleHeight).Within(0.000001f));
            Assert.That(minimum, Is.EqualTo(0.21333333f).Within(0.000001f));
            Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099("HERO_REC_011", false, idle),
                Is.EqualTo(0.28f));
        }

        [TestCase("HERO_REC_016", true)]
        [TestCase("HERO_REC_183", true)]
        [TestCase("HERO_REC_031", true)]
        [TestCase("HERO_REC_011", false)]
        [TestCase("UNKNOWN", true)]
        [TestCase(null, true)]
        public void OtherIdentityOrIdleCannotBorrowElaraException099(string id, bool action)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093("HERO_REC_011", true, out var elaraAction, out _), Is.True);
            Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099(id, action, elaraAction),
                Is.EqualTo(0.28f));
        }

        [Test]
        public void SameTextureAndRectCannotForgeCalibratedPose099()
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093("HERO_REC_011", true, out var action, out _), Is.True);
            var forged = Sprite.Create(action.texture, action.rect, Vector2.one * 0.5f);
            try
            {
                Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099("HERO_REC_011", true, forged),
                    Is.EqualTo(0.28f));
                Assert.That(HeroRosterBuiltPlayerAudit093.MinimumReviewedPoseHeightFraction099("HERO_REC_011", true, null),
                    Is.EqualTo(0.28f));
            }
            finally { Object.DestroyImmediate(forged); }
        }
    }
}
