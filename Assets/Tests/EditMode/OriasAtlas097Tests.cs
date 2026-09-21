using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class OriasAtlas097Tests
    {
        [TestCase("HERO_REC_265", 756, 54, 12, 687, 1006, 771, 15, 761, 976)]
        [TestCase("HERO_REC_011", 634, 107, 58, 454, 945, 707, 57, 816, 720)]
        [TestCase("HERO_REC_016", 679, 113, 76, 424, 873, 822, 76, 705, 810)]
        public void ActualOriasAtlasRetainsBothExactAlphaFramesAfterWindowsCpuRelease097(
            string identity, int split, int idleX, int idleY, int idleWidth, int idleHeight,
            int actionX, int actionY, int actionWidth, int actionHeight)
        {
            var source = Resources.Load<Texture2D>(HeroRemasterAtlas093.Root093 + identity + "_PAIR_093");
            Assert.That(source, Is.Not.Null, "Missing candidate is an art blocker, not a fallback pass.");
            var texture = Object.Instantiate(source);
            texture.name = identity + " exact candidate Windows lifetime fixture097";
            HeroRemasterAtlas093.Pair093 pair = null;
            try
            {
                Assert.That(HeroRemasterAtlas093.TryGetFrameRects093(identity, texture.width, texture.height,
                    out var idleCell, out var actionCell), Is.True);
                Assert.That(texture.isReadable, Is.True);
                var sourcePixels = texture.GetPixels32();
                var gutterClear = true;
                for (var y = 0; y < texture.height; y++)
                for (var x = split - 2; x <= split + 2; x++)
                    gutterClear &= sourcePixels[y * texture.width + x].a < 16;
                Assert.That(gutterClear, Is.True,
                    "The inspected split must remain in the transparent gutter, not cut anatomy or FX.");
                var expectedIdle = new Rect(idleX, idleY, idleWidth, idleHeight);
                var expectedAction = new Rect(actionX, actionY, actionWidth, actionHeight);
                Assert.That(M1SilhouetteFraming091.VisibleRect091(texture, idleCell), Is.EqualTo(expectedIdle));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(texture, actionCell), Is.EqualTo(expectedAction));
                pair = HeroRemasterAtlas093.BuildPair093(texture, idleCell, actionCell, true);
                Assert.That(pair, Is.Not.Null, "Both real cells must be transparent, separate and edge-clear.");
                Assert.That(texture.isReadable, Is.False);
                Assert.That(M1SilhouetteFraming091.VisibleRect091(pair.Idle), Is.EqualTo(expectedIdle));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(pair.Action), Is.EqualTo(expectedAction));
                Assert.That(pair.Idle.texture, Is.SameAs(texture));
                Assert.That(pair.Action.texture, Is.SameAs(texture));
                Assert.That(pair.Idle.rect, Is.Not.EqualTo(pair.Action.rect));
                Assert.That(pair.Idle.rect.xMax, Is.LessThanOrEqualTo(split));
                Assert.That(pair.Action.rect.xMin, Is.GreaterThanOrEqualTo(split));
                if (identity == "HERO_REC_265")
                    Assert.That(expectedIdle.height / expectedAction.height, Is.InRange(0.95f, 1.05f),
                        "The original Orias body-scale baseline remains unchanged.");
                // Elara's upright idle spear and thrust action have deliberately
                // different alpha heights. Exact cells/grounding, not equal
                // silhouette height, are the relevant lifetime invariants.
                foreach (var pose in new[] { pair.Idle, pair.Action, pair.Idle })
                    Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(pose), Is.SameAs(pose),
                        "Pose switching must reuse framed geometry without another CPU scan/GPU readback.");
            }
            finally { HeroRemasterAtlas093.RetireSources093(pair); Object.DestroyImmediate(texture); }
        }
    }
}
