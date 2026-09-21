using System;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRecoveredPoseProof099Tests
    {
        [TestCase("HERO_REC_035", 35)]
        public void RecoveredExactSeparateTexturesAreSelectedAndInspected099(string id, int roster)
        {
            Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(roster, id, 31, 48, true), Is.True);
            Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(roster, id, 169, 185, true), Is.False);
            Assert.That(HeroRecoveredSource100099.TryResolve099(id, false, out var idle, out var idleKey), Is.True);
            Assert.That(HeroRecoveredSource100099.TryResolve099(id, true, out var action, out var actionKey), Is.True);
            Assert.That(action.texture, Is.Not.SameAs(idle.texture));
            var host = new GameObject("Recovered real pose Image099", typeof(RectTransform), typeof(Image));
            Sprite fullSource = null;
            try
            {
                var image = host.GetComponent<Image>(); image.preserveAspect = true; image.sprite = idle;
                var before = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, id, "IDLE", false, false);
                Assert.That(before.expectedResource, Is.EqualTo(idleKey));
                Assert.That(before.exactCellBound && before.neighborCellExcluded, Is.True);
                Assert.That(before.campaignUnchanged, Is.False, "An Image-only test cannot certify save-state invariance.");
                image.sprite = action;
                Assert.Throws<InvalidOperationException>(() =>
                    HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, id, "IDLE", false, false));
                var during = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, id, "ACTION", true, false);
                Assert.That(during.expectedResource, Is.EqualTo(actionKey));
                Assert.That(during.exactCellBound && during.neighborCellExcluded, Is.True);
                Assert.Throws<InvalidOperationException>(() =>
                    HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, id, "ACTION", true, true),
                    "Editor-readable textures cannot claim Windows CPU-discard evidence.");
                fullSource = Sprite.Create(idle.texture, new Rect(0, 0, 768, 1024), Vector2.one * 0.5f);
                image.sprite = fullSource;
                Assert.Throws<InvalidOperationException>(() =>
                    HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, id, "IDLE", false, false),
                    "An arbitrary whole PNG must not bypass the reviewed source cell/framed sprite.");
                image.sprite = idle;
                var restored = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, id, "IDLE RESTORED", false, false);
                Assert.That(restored.spriteRect, Is.EqualTo(before.spriteRect));
                Assert.That(HeroRosterBuiltPlayerAudit093.CanKeepReviewedBattleOpen093(
                    new[] { "--sd-roster-audit-093", "--sd-roster-audit-keep-open" }, true, id, true), Is.True);
            }
            finally { if (fullSource != null) Object.DestroyImmediate(fullSource); Object.DestroyImmediate(host); }
        }

        [TestCase("HERO_REC_032", 32)]
        [TestCase("HERO_REC_039", 39)]
        [TestCase("HERO_REC_043", 43)]
        [TestCase("HERO_REC_048", 48)]
        [TestCase("HERO_REC_033", 33)]
        [TestCase("HERO_REC_034", 34)]
        [TestCase("HERO_REC_041", 41)]
        [TestCase("HERO_REC_047", 47)]
        [TestCase("HERO_REC_031", 31)]
        [TestCase("HERO_REC_040", 40)]
        [TestCase("HERO_REC_042", 42)]
        [TestCase("HERO_REC_045", 45)]
        [TestCase("HERO_REC_036", 36)]
        public void NewlyAuthoredOriginalDoesNotReapproveRejectedRecoveredPixels099(string id, int roster)
        {
            Assert.That(HeroRecoveredSource100099.TryResolve099(id, false, out _, out _), Is.False);
            Assert.That(HeroRecoveredSource100099.TryResolve099(id, true, out _, out _), Is.False);
            Assert.That(HeroRemasterAtlas093.TryResolve093(id, false, out _, out var idleKey), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(id, true, out _, out var actionKey), Is.True);
            Assert.That(idleKey, Is.EqualTo(HeroRemasterAtlas093.Root093 + id + "_PAIR_093#IDLE"));
            Assert.That(actionKey, Is.EqualTo(HeroRemasterAtlas093.Root093 + id + "_PAIR_093#ACTION"));
            Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(roster, id, 1, 300, true), Is.True);
        }

        [TestCase("HERO_REC_111", 111)]
        [TestCase("UNKNOWN", 33)]
        public void NonreviewedAndQuarantinedRemainExcluded099(string id, int roster)
        {
            Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(roster, id, 1, 300, true), Is.False);
            var host = new GameObject("Nonreviewed pose guard099", typeof(RectTransform), typeof(Image));
            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                    HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(host.GetComponent<Image>(), id, "IDLE", false, false));
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
