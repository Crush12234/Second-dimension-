using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyArtRemaster098Tests
    {
        [SetUp] public void SetUp() => EnemyArt700Runtime090.ResetForTests090();
        [TearDown] public void TearDown() => EnemyArt700Runtime090.ResetForTests090();

        [Test]
        public void ExactReviewedAssetFramesItsOwnBodiesAndOnlyOverridesItsTenFamilyVariants098()
        {
            var entries = EnemyArt700Runtime090.Catalog090.variants;
            Assert.That(entries.Count(value => EnemyArtRemaster098.Handles098(value.baseEnemyId, value.variantId)), Is.EqualTo(10));
            var texture = Resources.Load<Texture2D>(EnemyArtRemaster098.ResourcePath098);
            Assert.That(texture, Is.Not.Null);
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture))))
                    .Replace("-", ""), Is.EqualTo("2EAD7A38FBAB4DF3D8C43232238B21919270AA6E9B497588CBE5A30E10FD91C0"));
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.That(importer.isReadable, Is.True);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Idle, out var idle, out var error), Is.True, error);
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Attack, out var action, out error), Is.True, error);
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Portrait, out var portrait, out error), Is.True, error);
            Assert.That(idle.texture, Is.SameAs(texture));
            Assert.That(action.texture, Is.SameAs(texture));
            Assert.That(portrait, Is.SameAs(idle));
            Assert.That(texture.isReadable, Is.False);
            Assert.That(M1SilhouetteFraming091.VisibleRect091(idle), Is.EqualTo(new Rect(98, 48, 522, 927)));
            Assert.That(M1SilhouetteFraming091.VisibleRect091(action), Is.EqualTo(new Rect(798, 75, 715, 748)));
            Assert.That(idle.rect.xMax, Is.LessThanOrEqualTo(709));
            Assert.That(action.rect.xMin, Is.GreaterThanOrEqualTo(709));
            Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(1));
            Assert.That(EnemyArt700Runtime090.GetDiagnostics090().PngReadCount, Is.Zero,
                "A Resources atlas load must not masquerade as three archived PNG reads.");
        }

        [Test]
        public void SharedPairSurvivesCacheClearUntilBothPoseOwnersRelease098()
        {
            Assert.That(EnemyArt700Runtime090.TryAcquireSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Idle, out var idle, out var idleLease, out var error), Is.True, error);
            Assert.That(EnemyArt700Runtime090.TryAcquireSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Attack, out var action, out var actionLease, out error), Is.True, error);
            try
            {
                EnemyArt700Runtime090.ClearCache090();
                Assert.That(idle != null && action != null && idle.texture != null, Is.True);
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.EqualTo(2));
                Assert.That(EnemyArt700Runtime090.GetDiagnostics090().OutstandingLeaseCount, Is.EqualTo(2));
                idleLease.Dispose(); idleLease.Dispose();
                Assert.That(action != null && action.texture != null && idle != null, Is.True,
                    "One pose's release cannot destroy the shared atlas or sibling sprite.");
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.EqualTo(1));
                actionLease.Dispose(); actionLease.Dispose();
                Assert.That(EnemyArtRemaster098.ResidentSpriteCount098, Is.Zero);
                Assert.That(EnemyArt700Runtime090.GetDiagnostics090().OutstandingLeaseCount, Is.Zero);
                Assert.That(idle == null && action == null, Is.True);
                Assert.That(EnemyArt700Runtime090.TryLoadSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                    EnemyArt700Pose090.Idle, out var reopened, out error), Is.True, error);
                Assert.That(reopened, Is.Not.Null);
                Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(2));
            }
            finally { idleLease?.Dispose(); actionLease?.Dispose(); }
        }

        [Test]
        public void PairCleanupKeepsUnrelatedAuthoredHeroFrameAliveAndCached098()
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093("HERO_REC_170", false, out var hero, out _), Is.True);
            var heroTexture = hero.texture;
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Idle, out _, out var error), Is.True, error);
            EnemyArt700Runtime090.ClearCache090();
            Assert.That(hero != null && heroTexture != null, Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093("HERO_REC_170", false, out var after, out _), Is.True);
            Assert.That(after, Is.SameAs(hero));
            Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(hero), Is.SameAs(hero));
        }

        [Test]
        public void ArchivedSourceQAStillLoadsOriginalPngAndOtherVariantsStayOnRawProvider098()
        {
            Assert.That(EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098("ENEMY_REC_021",
                "ENEMY_REC_021_VAR_03", EnemyArt700Pose090.Idle, out var archived, out var error), Is.True, error);
            Assert.That(archived.texture.width, Is.EqualTo(768));
            Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.Zero);
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090("ENEMY_REC_021", "ENEMY_REC_021_VAR_03",
                EnemyArt700Pose090.Idle, out var live, out error), Is.True, error);
            Assert.That(live.texture, Is.Not.SameAs(archived.texture));
            Assert.That(live.texture.width, Is.EqualTo(1536));
            foreach (var baseId in new[] { "ENEMY_REC_020", "ENEMY_REC_022" })
            {
                var variant = baseId + "_VAR_04";
                Assert.That(EnemyArt700Runtime090.TryLoadSprite090(baseId, variant,
                    EnemyArt700Pose090.Idle, out var ordinary, out error), Is.True, error);
                Assert.That(EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098(baseId, variant,
                    EnemyArt700Pose090.Idle, out var source, out error), Is.True, error);
                Assert.That(ordinary, Is.SameAs(source));
            }
            Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(1));
            Assert.That(EnemyArt700Runtime090.GetDiagnostics090().PngReadCount, Is.EqualTo(3));
        }
    }
}
