using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class RogarOriginal099Tests
    {
        const string Identity099 = "HERO_REC_183";
        const string SourceSha099 = "7EF7934AA83B0E09ED14D67F32391857FAB9550E9F9196FAFA27EC89C828FBB9";

        [Test]
        public void ExactRogarProfileAndReviewedOriginalBindBothProductionPoses099()
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == Identity099);
            Assert.That(hero.Name, Is.EqualTo("Rogar Earthborn"));
            Assert.That(hero.Race, Is.EqualTo("Dwarf"));
            Assert.That(hero.Role, Is.EqualTo("Mountain Aegis"));
            Assert.That(hero.Weapon, Is.EqualTo("Hammer & Shield"));
            // Reuse the complete existing production binding/import/cache test,
            // including exact original bytes, menu idle and battle/action cells.
            new HeroRemasterAtlas093Tests().ExactIdentityUsesInspectedUnequalFramesAndDistinctPoses093(
                Identity099, 752, SourceSha099);
        }

        [Test]
        public void BothCompleteFramesHaveClearEdgesAndComparableBodyScale099()
        {
            var texture = Resources.Load<Texture2D>(HeroRemasterAtlas093.Root093 + Identity099 + "_PAIR_093");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(1536));
            Assert.That(texture.height, Is.EqualTo(1024));
            Assert.That(texture.isReadable, Is.True);
            var pixels = texture.GetPixels32();
            var gutterVisible = 0;
            for (var x = 725; x <= 778; x++)
                for (var y = 0; y < texture.height; y++)
                    if (pixels[y * texture.width + x].a >= 16) gutterVisible++;
            Assert.That(gutterVisible, Is.Zero, "Inspected full-height split gutter.");
            var idle = Inspect099(pixels, texture.width, texture.height, 0, 752);
            var action = Inspect099(pixels, texture.width, texture.height, 752, 1536);
            Assert.That(idle.Visible, Is.EqualTo(357878));
            Assert.That(action.Visible, Is.EqualTo(335969));
            Assert.That(idle.Width, Is.EqualTo(717));
            Assert.That(idle.Height, Is.EqualTo(789));
            Assert.That(action.Width, Is.EqualTo(750));
            Assert.That(action.Height, Is.EqualTo(809));
            Assert.That((double)action.Height / idle.Height, Is.InRange(0.95, 1.05),
                "The action is another complete pose, not a disproportionately enlarged portrait.");
        }

        [Test]
        public void RegistrationDoesNotBorrowRogarForNeighborOrNerissa099()
        {
            Assert.That(HeroRemasterAtlas093.ContainsIdentity093(Identity099), Is.True);
            foreach (var other in new[] { "HERO_REC_182", "HERO_REC_184", "HERO_REC_192" })
                if (HeroRemasterAtlas093.TryResolve093(other, false, out var sprite, out var key))
                    Assert.That(key, Does.Not.Contain(Identity099));
            Assert.That(HeroRemasterAtlas093.TryResolve093("NOT_A_HERO_183", false, out _, out _), Is.False);
        }

        struct Bounds099 { public int Visible, Width, Height; }
        static Bounds099 Inspect099(Color32[] pixels, int width, int height, int start, int end)
        {
            var minX = end; var maxX = -1; var minY = height; var maxY = -1; var visible = 0; var edges = 0;
            for (var y = 0; y < height; y++)
            for (var x = start; x < end; x++)
            {
                if (pixels[y * width + x].a < 16) continue;
                if (x == start || x == end - 1 || y == 0 || y == height - 1) edges++;
                visible++; minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
            Assert.That(visible, Is.GreaterThan(100000));
            Assert.That(edges, Is.Zero, "No meaningful body, weapon or effect pixel may cross its frame boundary.");
            return new Bounds099 { Visible = visible, Width = maxX - minX + 1, Height = maxY - minY + 1 };
        }
    }
}
