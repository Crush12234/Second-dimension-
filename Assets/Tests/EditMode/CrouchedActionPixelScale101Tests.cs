using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>
    /// Regression for R173 native-reviewed crouches. Art remains byte-exact.
    /// Source pixels retain idle scale; action silhouettes deliberately become
    /// shorter rather than inflating the head, torso, hands or weapon.
    /// </summary>
    public sealed class CrouchedActionPixelScale101Tests
    {
        static readonly string[] Corrected101 = { "HERO_REC_105", "HERO_REC_221", "HERO_REC_257" };

        [TestCase("HERO_REC_105", 587, 981, 756, 863, 700,
            "2FAAA304199B7F14C2747B132574295C74C6D6120DB07E49B5578A174548A3B7")]
        [TestCase("HERO_REC_221", 417, 868, 502, 791, 768,
            "95CFEA5212C153C84CDE63911480AD4D06352A32D7955F92DE1B1070EAFAC118")]
        [TestCase("HERO_REC_257", 728, 974, 719, 837, 790,
            "45A6FC347F5C6C7DAF6189586D7BD29A6B3F63EE947C672FDEADC7A17D9CDBF2")]
        public void ReviewedOriginalPixelsKeepCrouchInsteadOfMagnification101(string identity,
            int idleWidth, int idleHeight, int actionWidth, int actionHeight, int split, string sourceSha)
        {
            var path = Path.Combine(Application.dataPath, "Resources", "SecondDimension", "Art",
                "Standees", "HeroRemaster093", identity + "_PAIR_093.png");
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""),
                    Is.EqualTo(sourceSha), "No raster edits or substituted source.");
            Assert.That(HeroRemasterAtlas093.UsesIdlePixelScale100(identity), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var idle, out var idleKey), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var action, out var actionKey), Is.True);
            Assert.That(idleKey, Is.EqualTo(HeroRemasterAtlas093.Root093 + identity + "_PAIR_093#IDLE"));
            Assert.That(actionKey, Is.EqualTo(HeroRemasterAtlas093.Root093 + identity + "_PAIR_093#ACTION"));
            Assert.That(action.texture, Is.SameAs(idle.texture));
            Assert.That(action, Is.Not.SameAs(idle));
            Assert.That(idle.rect.xMax, Is.LessThanOrEqualTo(split));
            Assert.That(action.rect.xMin, Is.GreaterThanOrEqualTo(split), "No neighboring pose bleed.");
            var idleAlpha = M1SilhouetteFraming091.VisibleRect091(idle);
            var actionAlpha = M1SilhouetteFraming091.VisibleRect091(action);
            Assert.That(idleAlpha.size, Is.EqualTo(new Vector2(idleWidth, idleHeight)));
            Assert.That(actionAlpha.size, Is.EqualTo(new Vector2(actionWidth, actionHeight)));
            const float requestedHeight = 420;
            var independentActionScale = requestedHeight / actionHeight;
            var expectedIdleScale = requestedHeight / idleHeight;
            Assert.That(independentActionScale, Is.GreaterThan(expectedIdleScale * 1.09f),
                "Control reproduces at least 9% old magnification before the exact-ID cap.");
            var actual = HeroRemasterAtlas093.CalibrateBattlePixelScale099(action,
                independentActionScale, requestedHeight, 2000);
            Assert.That(actual, Is.EqualTo(expectedIdleScale).Within(0.0001f));
            Assert.That(actionAlpha.height * actual / requestedHeight,
                Is.EqualTo((float)actionHeight / idleHeight).Within(0.0001f));
            Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(idle, independentActionScale,
                requestedHeight, 2000), Is.EqualTo(independentActionScale),
                "Idle never borrows action calibration.");
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var again, out _), Is.True);
            Assert.That(again, Is.SameAs(action), "No per-call reframing or texture duplication.");
        }

        [TestCase("HERO_REC_105", 420f, 1000f, 1200f)]
        [TestCase("HERO_REC_105", 230f, 130f, 380f)]
        [TestCase("HERO_REC_105", 420f, 90f, 100f)]
        [TestCase("HERO_REC_221", 420f, 1000f, 1200f)]
        [TestCase("HERO_REC_221", 230f, 130f, 380f)]
        [TestCase("HERO_REC_221", 420f, 90f, 100f)]
        [TestCase("HERO_REC_257", 420f, 1000f, 1200f)]
        [TestCase("HERO_REC_257", 230f, 130f, 380f)]
        [TestCase("HERO_REC_257", 420f, 90f, 100f)]
        public void ShippingRigIdleActionRoleAndReturnKeepPixelScaleAndGrounding101(
            string identity, float height, float idleWidth, float actionWidth) =>
            new ElaraBattleScale099Tests().ActualRigUsesIdlePixelScaleForExactReviewedAction100(
                identity, height, idleWidth, actionWidth);

        [TestCase("HERO_REC_105")]
        [TestCase("HERO_REC_221")]
        [TestCase("HERO_REC_257")]
        public void CopiedSameTextureCellCannotBorrowCachedActionAuthority101(string identity) =>
            new ElaraBattleScale099Tests().SameTextureCellCopyIsNotCalibrationAuthority099(identity);

        [TestCase("HERO_REC_105")]
        [TestCase("HERO_REC_221")]
        [TestCase("HERO_REC_257")]
        public void MissingExactCacheEntryFailsClosedAndRestoredEntryRetainsAuthority101(string identity)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var action, out _), Is.True);
            var field = typeof(HeroRemasterAtlas093).GetField("Pairs093", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            var cache = (IDictionary)field.GetValue(null);
            Assert.That(cache.Contains(identity), Is.True);
            var pair = cache[identity];
            var count = cache.Count;
            const float requested = 10;
            try
            {
                // Remove only this registry entry without destroying any resource or
                // other actor's cached sprites. A live but unregistered sprite is not
                // calibration authority, even when its texture/name/rect all match.
                cache.Remove(identity);
                Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(action, requested, 10, 10),
                    Is.EqualTo(requested));
                Assert.That(cache.Count, Is.EqualTo(count - 1), "Calibration must not load/create cache entries.");
            }
            finally { cache[identity] = pair; }
            Assert.That(cache.Count, Is.EqualTo(count));
            Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(action, requested, 10, 10),
                Is.LessThan(requested), "Restoring the actual cached action restores its cap.");
        }

        [TestCase("HERO_REC_105")]
        [TestCase("HERO_REC_221")]
        [TestCase("HERO_REC_257")]
        public void NarrowViewportRemainsAnUpperBoundNeverForcedUpscale101(string identity)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var idle, out _), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var action, out _), Is.True);
            var heightCap = 420f / M1SilhouetteFraming091.VisibleRect091(idle).height;
            var idleWidthCap = 90f / idle.rect.width;
            var expected = Mathf.Min(heightCap, idleWidthCap);
            Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(action, 10, 420, 90),
                Is.EqualTo(expected).Within(0.0001f));
            var narrowerAction = expected * 0.4f;
            Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(action, narrowerAction, 420, 90),
                Is.EqualTo(narrowerAction), "Fit is a cap, not a request to break an action viewport.");
        }

        [Test]
        public void OnlyTheThreeReviewedIdentitiesExtendExistingCapAndLookupDoesNotLoadArt101()
        {
            var expected = new HashSet<string>(StringComparer.Ordinal)
            { "HERO_REC_011", "HERO_REC_033", "HERO_REC_041", "HERO_REC_195" };
            foreach (var identity in Corrected101) Assert.That(expected.Add(identity), Is.True);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var splitField = typeof(HeroRemasterAtlas093).GetField("FrameSplits093", flags);
            var cacheField = typeof(HeroRemasterAtlas093).GetField("Pairs093", flags);
            Assert.That(splitField, Is.Not.Null); Assert.That(cacheField, Is.Not.Null);
            var entries = (IDictionary)splitField.GetValue(null);
            var cache = (IDictionary)cacheField.GetValue(null);
            var before = cache.Count; var enabled = 0;
            foreach (string identity in entries.Keys)
            {
                Assert.That(HeroRemasterAtlas093.UsesIdlePixelScale100(identity),
                    Is.EqualTo(expected.Contains(identity)), identity);
                if (HeroRemasterAtlas093.UsesIdlePixelScale100(identity)) enabled++;
            }
            Assert.That(enabled, Is.EqualTo(7));
            Assert.That(cache.Count, Is.EqualTo(before), "Identity lookup never loads the full roster.");
            foreach (var identity in new[] { null, "", "HERO_REC_105_SUFFIX", "hero_rec_105",
                "HERO_REC_221 ", "HERO_REC_257_ACTION", "ORC", "MINOTAUR", "BEASTKIN",
                "ENEMY_REC_021_VAR_03", "UNKNOWN" })
                Assert.That(HeroRemasterAtlas093.UsesIdlePixelScale100(identity), Is.False, identity);
        }

        [TestCase("HERO_REC_034")]
        [TestCase("HERO_REC_186")]
        [TestCase("HERO_REC_104")]
        [TestCase("HERO_REC_220")]
        [TestCase("HERO_REC_256")]
        public void ExistingAndNeighboringUnreviewedHeroesKeepPreviousShippingFit101(string identity) =>
            new ElaraBattleScale099Tests().OtherExactHeroesKeepOriginalPoseFit099(identity);
    }
}
