using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Boot;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class HeroArtContact118PlayModeTests
    {
        [UnityTest]
        public IEnumerator CameraProbeReadsActualVisibleSpritePixelsAndDistinguishesCopies118()
        {
            var first = Texture118(Color.red);
            var copy = Texture118(Color.red);
            var different = Texture118(Color.blue);
            var a = Sprite.Create(first, new Rect(0, 0, 12, 18), new Vector2(.5f, .5f));
            var b = Sprite.Create(copy, new Rect(0, 0, 12, 18), new Vector2(.5f, .5f));
            var c = Sprite.Create(different, new Rect(0, 0, 12, 18), new Vector2(.5f, .5f));
            // Production textures need not retain CPU pixels for this camera path.
            first.Apply(false, true); copy.Apply(false, true); different.Apply(false, true);
            using (var surface = new HeroArtContactSheet118.RenderSurface118(160, 240, 30))
            {
                yield return null;
                var empty = surface.Probe118(null);
                var red = surface.Probe118(a);
                var duplicate = surface.Probe118(b);
                var blue = surface.Probe118(c);
                Assert.That(empty.VisibleProbePixels, Is.Zero);
                Assert.That(red.RenderStatus, Is.EqualTo("RENDERED_VISIBLE"));
                Assert.That(red.VisibleProbePixels, Is.GreaterThan(100));
                Assert.That(red.VisibleProbePixels, Is.LessThan(160 * 240));
                Assert.That(duplicate.PixelSha256, Is.EqualTo(red.PixelSha256), "Separate source files with identical pixels remain a reused pose.");
                Assert.That(blue.PixelSha256, Is.Not.EqualTo(red.PixelSha256));
                Assert.That(first.isReadable, Is.False, "QA must not alter production texture readability.");
                Assert.That(a.texture, Is.SameAs(first));
                Assert.That(a.rect, Is.EqualTo(new Rect(0, 0, 12, 18)));
            }
            Object.Destroy(a); Object.Destroy(b); Object.Destroy(c);
            Object.Destroy(first); Object.Destroy(copy); Object.Destroy(different);
            yield return null;
        }

        static Texture2D Texture118(Color color)
        {
            var texture = new Texture2D(12, 18, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color32[12 * 18];
            for (int y = 3; y < 15; y++)
            for (int x = 3; x < 9; x++) pixels[y * 12 + x] = color;
            texture.SetPixels32(pixels); texture.Apply(false, false);
            return texture;
        }
    }
}
