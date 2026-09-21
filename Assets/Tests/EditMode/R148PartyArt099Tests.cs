using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    // New original artwork, not a claim that clipped source pixels were recovered.
    // Cases are added only after the exact bitmap and both frame bounds are reviewed.
    public sealed class R148PartyArt099Tests
    {
        public sealed class ReviewedPair099
        {
            public string Id, Name, Race, Role, Weapon, Sha;
            public int Split, GutterFrom, GutterThrough;
            public int IdleWidth, IdleHeight, IdleVisible, ActionWidth, ActionHeight, ActionVisible;
            public override string ToString() => Id;
        }

        static IEnumerable<ReviewedPair099> ReviewedPairs099()
        {
            yield return new ReviewedPair099
            {
                Id = "HERO_REC_051", Name = "Caldris Ashward", Race = "Human",
                Role = "Greatsword Vanguard", Weapon = "Greatsword", Split = 690,
                Sha = "DA3056B6656DFCEBC2EAF0B8F612C5FBF0014999E9A40BBEA98EEE5E8C5CD87E",
                GutterFrom = 611, GutterThrough = 766,
                IdleWidth = 536, IdleHeight = 844, IdleVisible = 191666,
                ActionWidth = 739, ActionHeight = 849, ActionVisible = 197807
            };
            yield return new ReviewedPair099
            {
                Id = "HERO_REC_071", Name = "Aldren Galeheart", Race = "Human",
                Role = "Blade Knight", Weapon = "Longsword", Split = 768,
                Sha = "22398179CABC490B7E0EADFB5CA2542B633BDA700CC74756569032157B82A81D",
                GutterFrom = 654, GutterThrough = 820,
                IdleWidth = 520, IdleHeight = 835, IdleVisible = 209774,
                ActionWidth = 654, ActionHeight = 800, ActionVisible = 183289
            };
            yield return new ReviewedPair099
            {
                Id = "HERO_REC_098", Name = "Aria Solspire", Race = "Human",
                Role = "Radiant Cleric", Weapon = "Sun staff", Split = 768,
                Sha = "BAAFA0FDC5EE9D514B09AE386577CF13C19D574DCC300C77DCDCC51394883663",
                GutterFrom = 620, GutterThrough = 837,
                IdleWidth = 467, IdleHeight = 923, IdleVisible = 237435,
                ActionWidth = 638, ActionHeight = 840, ActionVisible = 304466
            };
            yield return new ReviewedPair099
            {
                Id = "HERO_REC_192", Name = "Nerissa Darkhollow", Race = "Half-Elf",
                Role = "Shadow Dancer", Weapon = "Twin Blades", Split = 768,
                Sha = "C81967755E33AB43257643A57349EC78B67A76532CB9B8B1E863B6B799822C92",
                GutterFrom = 671, GutterThrough = 856,
                IdleWidth = 601, IdleHeight = 918, IdleVisible = 209712,
                ActionWidth = 673, ActionHeight = 847, ActionVisible = 220326
            };
            yield return new ReviewedPair099
            {
                Id = "HERO_REC_274", Name = "Poppy Ironpaw", Race = "Beastkin",
                Role = "Ironpaw Brawler", Weapon = "Mechanical Fists", Split = 768,
                Sha = "C684B1384A6D2C70FA2A35961283BB18A6D415FA1CE0D9BCF8C419880E69C4F6",
                GutterFrom = 652, GutterThrough = 885,
                IdleWidth = 483, IdleHeight = 967, IdleVisible = 217894,
                ActionWidth = 566, ActionHeight = 933, ActionVisible = 218625
            };
            yield return new ReviewedPair099
            {
                Id = "HERO_REC_276", Name = "Celia Sunclock", Race = "Human",
                Role = "Sunclock Cleric", Weapon = "Solar Staff", Split = 768,
                Sha = "624B78C514B241E7A933ECE55E0E665526F5ED3C7FD30245D21DDC7C544AA6A0",
                GutterFrom = 655, GutterThrough = 826,
                IdleWidth = 455, IdleHeight = 968, IdleVisible = 240232,
                ActionWidth = 589, ActionHeight = 889, ActionVisible = 259117
            };
        }

        [TestCaseSource(nameof(ReviewedPairs099))]
        public void ExactReviewedOriginalBindsBothProductionPosesAndPortrait099(ReviewedPair099 expected)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == expected.Id);
            Assert.That(hero.Name, Is.EqualTo(expected.Name));
            Assert.That(hero.Race, Is.EqualTo(expected.Race));
            Assert.That(hero.Role, Is.EqualTo(expected.Role));
            Assert.That(hero.Weapon, Is.EqualTo(expected.Weapon));
            var path = HeroRemasterAtlas093.Root093 + expected.Id + "_PAIR_093";
            var texture = Resources.Load<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, "Missing original artwork must not pass through a geometric fallback.");
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)))).Replace("-", ""),
                    Is.EqualTo(expected.Sha), "Only the inspected complete version may bind.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.That(importer.isReadable, Is.True);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(HeroRemasterAtlas093.TryGetFrameRects093(expected.Id, texture.width, texture.height,
                out var idleFrame, out var actionFrame), Is.True);
            Assert.That(idleFrame, Is.EqualTo(new Rect(0, 0, expected.Split, 1024)));
            Assert.That(actionFrame, Is.EqualTo(new Rect(expected.Split, 0, 1536 - expected.Split, 1024)));
            // Equal halves are permitted only where the individually reviewed gutter contains that split.
            Assert.That(expected.Split, Is.InRange(expected.GutterFrom, expected.GutterThrough));
            Assert.That(M1VisualAssets.TryResolveBattleStandee(expected.Id, expected.Id, hero.Race, expected.Id,
                out var idle, out var idleKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleActionPose(expected.Id, expected.Id, hero.Race, expected.Id,
                out var action, out var actionKey), Is.True);
            Assert.That(idle.texture, Is.SameAs(texture));
            Assert.That(action.texture, Is.SameAs(texture));
            Assert.That(idleKey, Is.EqualTo(path + "#IDLE"));
            Assert.That(actionKey, Is.EqualTo(path + "#ACTION"));
            Assert.That(idle.rect, Is.Not.EqualTo(action.rect));
            Assert.That(idle.rect.xMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(idle.rect.xMax, Is.LessThanOrEqualTo(expected.Split));
            Assert.That(action.rect.xMin, Is.GreaterThanOrEqualTo(expected.Split));
            Assert.That(action.rect.xMax, Is.LessThanOrEqualTo(1536));
            Assert.That(M1VisualAssets.TryResolvePortrait(expected.Id, expected.Id, hero.Race, expected.Id,
                out var portrait, out _), Is.True);
            Assert.That(portrait, Is.SameAs(idle));
            Assert.That(HeroRemasterAtlas093.TryResolve093(expected.Id, true, out var cached, out _), Is.True);
            Assert.That(cached, Is.SameAs(action));
            var row = HeroRosterAudit093.Census093().Single(value => value.stableId == expected.Id);
            HeroRosterAudit093.BindArt093(hero, row);
            Assert.That(row.artCategory, Is.EqualTo("ORIGINAL_EXACT_ID_TWO_POSE_REMASTER_ATLAS"));
            Assert.That(row.professionalArtReviewed || row.runtimeUiVerified, Is.False,
                "Source/binding tests do not certify a Windows visual acceptance run.");
        }

        [TestCaseSource(nameof(ReviewedPairs099))]
        public void BothReviewedCellsRetainCompleteInspectedAlphaBounds099(ReviewedPair099 expected)
        {
            var texture = Resources.Load<Texture2D>(HeroRemasterAtlas093.Root093 + expected.Id + "_PAIR_093");
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.isReadable, Is.True);
            Assert.That(texture.width, Is.EqualTo(1536));
            Assert.That(texture.height, Is.EqualTo(1024));
            var pixels = texture.GetPixels32();
            var gutterVisible = 0;
            for (var x = expected.GutterFrom; x <= expected.GutterThrough; x++)
            for (var y = 0; y < texture.height; y++)
                if (pixels[y * texture.width + x].a >= 16) gutterVisible++;
            Assert.That(gutterVisible, Is.Zero, "Inspected full-height split gutter.");
            Inspect099(pixels, texture.width, texture.height, 0, expected.Split,
                expected.IdleWidth, expected.IdleHeight, expected.IdleVisible);
            Inspect099(pixels, texture.width, texture.height, expected.Split, texture.width,
                expected.ActionWidth, expected.ActionHeight, expected.ActionVisible);
        }

        [TestCaseSource(nameof(ReviewedPairs099))]
        public void ExactRegistrationDoesNotBorrowForAnotherIdentity099(ReviewedPair099 expected)
        {
            Assert.That(HeroRemasterAtlas093.ContainsIdentity093(expected.Id), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(expected.Id + "_UNKNOWN", false, out _, out _), Is.False);
            foreach (var other in new[] { "HERO_REC_011", "HERO_REC_183", "SIGREC_ODELIA_FEN", "HUMAN", "HALF_ELF", "BEASTKIN" })
                if (HeroRemasterAtlas093.TryResolve093(other, false, out _, out var key))
                    Assert.That(key, Does.Not.Contain(expected.Id));
        }

        static void Inspect099(Color32[] pixels, int width, int height, int from, int through,
            int expectedWidth, int expectedHeight, int expectedVisible)
        {
            var minX = through; var maxX = -1; var minY = height; var maxY = -1; var visible = 0; var edges = 0;
            for (var y = 0; y < height; y++)
            for (var x = from; x < through; x++)
            {
                if (pixels[y * width + x].a < 16) continue;
                visible++; minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                if (x == from || x == through - 1 || y == 0 || y == height - 1) edges++;
            }
            Assert.That(visible, Is.EqualTo(expectedVisible));
            Assert.That(maxX - minX + 1, Is.EqualTo(expectedWidth));
            Assert.That(maxY - minY + 1, Is.EqualTo(expectedHeight));
            Assert.That(edges, Is.Zero, "Body, equipment and visible effects must not cross a frame boundary.");
        }
    }
}
