using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyArt700Runtime090Tests
    {
        private static readonly EnemyArt700Pose090[] AllPoses090 =
        {
            EnemyArt700Pose090.Idle,
            EnemyArt700Pose090.Attack,
            EnemyArt700Pose090.Portrait
        };

        [SetUp]
        public void SetUp090()
        {
            EnemyArt700Runtime090.ResetForTests090();
        }

        [TearDown]
        public void TearDown090()
        {
            EnemyArt700Runtime090.ResetForTests090();
        }

        [Test]
        public void CatalogAndExplicitSelectorsCoverAllSevenHundredVariants090()
        {
            var issues090 = EnemyArt700Runtime090.ValidateCatalog090(true);
            Assert.That(
                issues090,
                Is.Empty,
                string.Join(Environment.NewLine, issues090));
            Assert.That(
                EnemyArt700Runtime090.CatalogCount090,
                Is.EqualTo(EnemyArt700Runtime090.ExpectedCatalogCount090));

            var variants090 = EnemyArt700Runtime090.Catalog090.variants;
            Assert.That(
                variants090.Select(value090 => value090.variantId).Distinct().Count(),
                Is.EqualTo(700));
            Assert.That(
                variants090.Select(value090 => value090.baseEnemyId).Distinct().Count(),
                Is.EqualTo(70));

            foreach (var expected090 in variants090)
            {
                Assert.That(EnemyArt700Runtime090.TryResolveVariant090(
                    expected090.baseEnemyId,
                    expected090.variantId,
                    out var exact090,
                    out var exactError090), Is.True, exactError090);
                Assert.That(exact090, Is.SameAs(expected090), expected090.variantId);

                Assert.That(EnemyArt700Runtime090.TryResolveVariant090(
                    expected090.baseEnemyId,
                    expected090.variantIndex.ToString(CultureInfo.InvariantCulture),
                    out var indexed090,
                    out var indexedError090), Is.True, indexedError090);
                Assert.That(indexed090.variantId, Is.EqualTo(expected090.variantId));
            }
        }

        [Test]
        public void CacheRemainsBoundedAndEvictsLeastRecentSprites090()
        {
            EnemyArt700Runtime090.ConfigureCacheCapacity090(3);
            AssertLoad090(
                "ENEMY_REC_001",
                "ENEMY_REC_001_VAR_01",
                EnemyArt700Pose090.Idle);
            AssertLoad090(
                "ENEMY_REC_001",
                "ENEMY_REC_001_VAR_01",
                EnemyArt700Pose090.Attack);
            AssertLoad090(
                "ENEMY_REC_001",
                "ENEMY_REC_001_VAR_01",
                EnemyArt700Pose090.Portrait);
            AssertLoad090(
                "ENEMY_REC_002",
                "ENEMY_REC_002_VAR_01",
                EnemyArt700Pose090.Idle);

            var diagnostics090 = EnemyArt700Runtime090.GetDiagnostics090();
            Assert.That(diagnostics090.CacheCapacity, Is.EqualTo(3));
            Assert.That(diagnostics090.CachedSpriteCount, Is.EqualTo(3));
            Assert.That(
                diagnostics090.CachedSpriteCount,
                Is.LessThanOrEqualTo(EnemyArt700Runtime090.MaximumCacheCapacity090));
            Assert.That(diagnostics090.PngReadCount, Is.EqualTo(4));
            Assert.That(diagnostics090.EvictionCount, Is.EqualTo(1));
        }

        [Test]
        public void LeasePinsDisplayedSpriteAcrossEvictionAndSafeClearThenReclaimsIt090()
        {
            EnemyArt700Runtime090.ConfigureCacheCapacity090(3);
            Assert.That(EnemyArt700Runtime090.TryAcquireSprite090(
                "ENEMY_REC_001",
                "ENEMY_REC_001_VAR_01",
                EnemyArt700Pose090.Idle,
                out var displayed090,
                out var lease090,
                out var error090), Is.True, error090);

            AssertLoad090(
                "ENEMY_REC_002",
                "ENEMY_REC_002_VAR_01",
                EnemyArt700Pose090.Idle);
            AssertLoad090(
                "ENEMY_REC_003",
                "ENEMY_REC_003_VAR_01",
                EnemyArt700Pose090.Idle);
            AssertLoad090(
                "ENEMY_REC_004",
                "ENEMY_REC_004_VAR_01",
                EnemyArt700Pose090.Idle);

            var stressed090 = EnemyArt700Runtime090.GetDiagnostics090();
            Assert.That(stressed090.CachedSpriteCount, Is.EqualTo(3));
            Assert.That(stressed090.ResidentSpriteCount, Is.EqualTo(3));
            Assert.That(stressed090.PinnedSpriteCount, Is.EqualTo(1));
            Assert.That(stressed090.OutstandingLeaseCount, Is.EqualTo(1));
            Assert.That(displayed090, Is.Not.Null);
            Assert.That(displayed090.texture, Is.Not.Null);

            EnemyArt700Runtime090.ClearCache090();
            var safelyCleared090 = EnemyArt700Runtime090.GetDiagnostics090();
            Assert.That(safelyCleared090.CachedSpriteCount, Is.Zero);
            Assert.That(safelyCleared090.ResidentSpriteCount, Is.EqualTo(1));
            Assert.That(safelyCleared090.PinnedSpriteCount, Is.EqualTo(1));
            Assert.That(safelyCleared090.RetiredSpriteCount, Is.EqualTo(1));
            Assert.That(displayed090, Is.Not.Null);
            Assert.That(displayed090.texture, Is.Not.Null);

            lease090.Dispose();
            lease090.Dispose();
            var released090 = EnemyArt700Runtime090.GetDiagnostics090();
            Assert.That(released090.ResidentSpriteCount, Is.Zero);
            Assert.That(released090.PinnedSpriteCount, Is.Zero);
            Assert.That(released090.OutstandingLeaseCount, Is.Zero);
            Assert.That(released090.RetiredSpriteCount, Is.Zero);
            Assert.That(displayed090 == null, Is.True,
                "Unity's destroyed-object null operator must confirm the released sprite was reclaimed.");
        }

        [TestCase("ENEMY_REC_001", "ENEMY_REC_001_VAR_01")]
        [TestCase("ENEMY_REC_035", "ENEMY_REC_035_VAR_05")]
        [TestCase("ENEMY_REC_070", "ENEMY_REC_070_VAR_10")]
        public void PairedIdleAttackIdleReusesTheOriginalIdleSprite090(
            string baseEnemyId090,
            string variantId090)
        {
            EnemyArt700Runtime090.ConfigureCacheCapacity090(3);
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                baseEnemyId090,
                variantId090,
                EnemyArt700Pose090.Idle,
                out var firstIdle090,
                out var error090), Is.True, error090);
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                baseEnemyId090,
                variantId090,
                EnemyArt700Pose090.Attack,
                out var attack090,
                out error090), Is.True, error090);

            var beforeReturn090 = EnemyArt700Runtime090.GetDiagnostics090();
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                baseEnemyId090,
                variantId090,
                EnemyArt700Pose090.Idle,
                out var returnedIdle090,
                out error090), Is.True, error090);
            var afterReturn090 = EnemyArt700Runtime090.GetDiagnostics090();

            Assert.That(returnedIdle090, Is.SameAs(firstIdle090));
            Assert.That(attack090, Is.Not.SameAs(firstIdle090));
            Assert.That(
                afterReturn090.PngReadCount,
                Is.EqualTo(beforeReturn090.PngReadCount));
            Assert.That(
                afterReturn090.CacheHitCount,
                Is.EqualTo(beforeReturn090.CacheHitCount + 1));
            Assert.That(afterReturn090.CachedSpriteCount, Is.EqualTo(2));
        }

        [TestCase("ENEMY_REC_001", "ENEMY_REC_001_VAR_01")]
        [TestCase("ENEMY_REC_035", "ENEMY_REC_035_VAR_05")]
        [TestCase("ENEMY_REC_070", "ENEMY_REC_070_VAR_10")]
        public void RepresentativeRawSpritesPreservePixelsAndFrameVisibleSilhouettes091(
            string baseEnemyId090,
            string variantId090)
        {
            Assert.That(EnemyArt700Runtime090.TryLoadVariantSprites090(
                baseEnemyId090,
                variantId090,
                out var sprites090,
                out var error090), Is.True, error090);

            AssertBattlePoseGeometry090(sprites090.Idle, variantId090 + "/IDLE");
            AssertBattlePoseGeometry090(sprites090.Attack, variantId090 + "/ATTACK");
            Assert.That(sprites090.Portrait.texture.width, Is.EqualTo(256));
            Assert.That(sprites090.Portrait.texture.height, Is.EqualTo(256));
            Assert.That(sprites090.Portrait.rect.width, Is.EqualTo(256f));
            Assert.That(sprites090.Portrait.rect.height, Is.EqualTo(256f));
            Assert.That(sprites090.Portrait.pivot.x, Is.EqualTo(128f).Within(0.01f));
            Assert.That(sprites090.Portrait.pivot.y, Is.EqualTo(128f).Within(0.01f));
            Assert.That(sprites090.Idle.texture.isReadable, Is.False);
            Assert.That(sprites090.Attack.texture.isReadable, Is.False);
            Assert.That(sprites090.Portrait.texture.isReadable, Is.False);
        }

        [Test]
        public void MissingOrMismatchedExplicitIdsFailClosedWithoutPngReads090()
        {
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                "ENEMY_REC_999",
                "ENEMY_REC_999_VAR_01",
                EnemyArt700Pose090.Idle,
                out var missingBase090,
                out var missingBaseError090), Is.False);
            Assert.That(missingBase090, Is.Null);
            Assert.That(missingBaseError090, Is.Not.Empty);

            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                "ENEMY_REC_001",
                "ENEMY_REC_002_VAR_01",
                EnemyArt700Pose090.Idle,
                out var mismatch090,
                out var mismatchError090), Is.False);
            Assert.That(mismatch090, Is.Null);
            Assert.That(mismatchError090, Does.Contain("does not belong"));

            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                "ENEMY_REC_001",
                "ENEMY_REC_001_VAR_99",
                EnemyArt700Pose090.Idle,
                out var missingVariant090,
                out var missingVariantError090), Is.False);
            Assert.That(missingVariant090, Is.Null);
            Assert.That(missingVariantError090, Is.Not.Empty);

            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                "ENEMY_REC_001",
                "ENEMY_REC_001_VAR_01",
                "DANCE",
                out var missingPose090,
                out var missingPoseError090), Is.False);
            Assert.That(missingPose090, Is.Null);
            Assert.That(missingPoseError090, Does.Contain("IDLE"));
            Assert.That(
                EnemyArt700Runtime090.GetDiagnostics090().PngReadCount,
                Is.Zero);
        }

        [Test]
        public void EveryBaseHasAnExplicitBoundedPresentationScale090()
        {
            var baseIds090 = EnemyArt700Runtime090.Catalog090.variants
                .Select(value090 => value090.baseEnemyId)
                .Distinct()
                .OrderBy(value090 => value090, StringComparer.Ordinal)
                .ToArray();
            Assert.That(baseIds090.Length, Is.EqualTo(70));

            var classes090 = baseIds090
                .Select(EnemyArt700Runtime090.ScaleClass090)
                .Distinct()
                .ToArray();
            Assert.That(classes090, Does.Contain(EnemyArt700ScaleClass090.Small));
            Assert.That(classes090, Does.Contain(EnemyArt700ScaleClass090.Default));
            Assert.That(classes090, Does.Contain(EnemyArt700ScaleClass090.Large));
            foreach (var baseId090 in baseIds090)
                Assert.That(
                    EnemyArt700Runtime090.RelativeScale090(baseId090),
                    Is.InRange(
                        EnemyArt700Runtime090.SmallRelativeScale090,
                        EnemyArt700Runtime090.LargeRelativeScale090),
                    baseId090);
        }

        [Test]
        public void EveryTextureUsesScopedFullRectAlphaImportSettings090()
        {
            var paths090 = EnemyArt700Runtime090.Catalog090.variants
                .SelectMany(value090 => new[]
                {
                    value090.unityAssetPaths.idle,
                    value090.unityAssetPaths.attack,
                    value090.unityAssetPaths.portrait
                })
                .OrderBy(value090 => value090, StringComparer.Ordinal)
                .ToArray();
            Assert.That(paths090.Length, Is.EqualTo(2100));
            Assert.That(paths090.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(2100));

            foreach (var assetPath090 in paths090)
            {
                Assert.That(
                    assetPath090,
                    Does.StartWith("Assets/SecondDimension/EnemyArt700/Textures/"));
                var importer090 = AssetImporter.GetAtPath(assetPath090) as TextureImporter;
                Assert.That(importer090, Is.Not.Null, assetPath090);
                Assert.That(importer090.textureType,
                    Is.EqualTo(TextureImporterType.Sprite), assetPath090);
                Assert.That(importer090.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Single), assetPath090);
                Assert.That(importer090.alphaSource,
                    Is.EqualTo(TextureImporterAlphaSource.FromInput), assetPath090);
                Assert.That(importer090.alphaIsTransparency, Is.True, assetPath090);
                Assert.That(importer090.sRGBTexture, Is.True, assetPath090);
                Assert.That(importer090.isReadable, Is.False, assetPath090);
                Assert.That(importer090.mipmapEnabled, Is.False, assetPath090);
                Assert.That(importer090.streamingMipmaps, Is.False, assetPath090);
                Assert.That(importer090.wrapMode,
                    Is.EqualTo(TextureWrapMode.Clamp), assetPath090);
                Assert.That(importer090.filterMode,
                    Is.EqualTo(FilterMode.Bilinear), assetPath090);
                Assert.That(importer090.npotScale,
                    Is.EqualTo(TextureImporterNPOTScale.None), assetPath090);
                Assert.That(importer090.textureCompression,
                    Is.EqualTo(TextureImporterCompression.CompressedHQ), assetPath090);
                Assert.That(importer090.compressionQuality, Is.EqualTo(85), assetPath090);
                Assert.That(importer090.crunchedCompression, Is.False, assetPath090);
                Assert.That(importer090.maxTextureSize,
                    Is.GreaterThanOrEqualTo(1024), assetPath090);
                Assert.That(importer090.spritePixelsPerUnit,
                    Is.EqualTo(100f).Within(0.001f), assetPath090);

                var spriteSettings090 = new TextureImporterSettings();
                importer090.ReadTextureSettings(spriteSettings090);
                Assert.That(spriteSettings090.spriteMeshType,
                    Is.EqualTo(SpriteMeshType.FullRect), assetPath090);
                var portrait090 = assetPath090.EndsWith(
                    "_PORTRAIT.png",
                    StringComparison.OrdinalIgnoreCase);
                Assert.That(spriteSettings090.spriteAlignment,
                    Is.EqualTo((int)(portrait090
                        ? SpriteAlignment.Center
                        : SpriteAlignment.Custom)), assetPath090);
                Assert.That(spriteSettings090.spritePivot.x,
                    Is.EqualTo(0.5f).Within(0.0001f), assetPath090);
                Assert.That(spriteSettings090.spritePivot.y,
                    Is.EqualTo(portrait090 ? 0.5f : 100f / 1024f)
                        .Within(0.0001f), assetPath090);

                var windowsSettings090 = importer090.GetPlatformTextureSettings("Standalone");
                Assert.That(windowsSettings090.overridden, Is.True, assetPath090);
                Assert.That(windowsSettings090.maxTextureSize,
                    Is.GreaterThanOrEqualTo(1024), assetPath090);
                Assert.That(windowsSettings090.format,
                    Is.EqualTo(TextureImporterFormat.DXT5), assetPath090);
                Assert.That(windowsSettings090.textureCompression,
                    Is.EqualTo(TextureImporterCompression.CompressedHQ), assetPath090);
                Assert.That(windowsSettings090.compressionQuality,
                    Is.EqualTo(85), assetPath090);
                Assert.That(windowsSettings090.crunchedCompression, Is.False, assetPath090);
                Assert.That(windowsSettings090.allowsAlphaSplitting, Is.False, assetPath090);
            }
        }

        [Test]
        public void WindowsBuildPayloadGateAcceptsExactlyTwoDataFilesAndTwentyOneHundredPngs090()
        {
            var sourceRoot090 = Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "EnemyArt700");
            var copierType090 = ResolveBuildCopierType090();
            var expectedCount090 = (int)copierType090.GetField(
                "ExpectedRuntimeFileCount090",
                BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
            var actualCount090 = (int)InvokeBuildPayloadValidation090(
                copierType090,
                sourceRoot090);

            Assert.That(actualCount090, Is.EqualTo(expectedCount090));
            Assert.That(expectedCount090, Is.EqualTo(2102));
        }

        [Test]
        public void WindowsBuildPayloadGateRejectsAnIncompleteSourceRoot090()
        {
            var sourceRoot090 = Path.Combine(
                Path.GetTempPath(),
                "sd_enemy_art_700_incomplete_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sourceRoot090);
            try
            {
                var exception090 = Assert.Throws<TargetInvocationException>(() =>
                    InvokeBuildPayloadValidation090(
                        ResolveBuildCopierType090(),
                        sourceRoot090));
                Assert.That(exception090.InnerException, Is.Not.Null);
                Assert.That(
                    exception090.InnerException.GetType().Name,
                    Is.EqualTo("BuildFailedException"));
                Assert.That(
                    exception090.InnerException.Message,
                    Does.Contain("requires both catalog Data JSON files"));
            }
            finally
            {
                if (Directory.Exists(sourceRoot090)) Directory.Delete(sourceRoot090, true);
            }
        }

        [Test]
        public void Release090BuilderInventoriesAndPreflightsEnemyArt700090()
        {
            var builderPath090 = Path.Combine(
                Application.dataPath,
                "Editor",
                "SecondDimension",
                "Release072",
                "FirstHourGoldWindowsBuild.cs");
            var builderSource090 = File.ReadAllText(builderPath090);
            foreach (var requiredPath090 in new[]
                     {
                         "Assets/Editor/SecondDimension/EnemyArt700TexturePostprocessor090.cs",
                         "Assets/Editor/SecondDimension/EnemyArt700WindowsBuildCopy090.cs",
                         "Assets/SecondDimension/EnemyArt700/Data/BaseEnemyIndex_001_070.json",
                         "Assets/SecondDimension/EnemyArt700/Data/EnemyArtCatalog_001_070.json",
                         "Assets/SecondDimension/Gameplay/M2/BattleState.cs",
                         "Assets/SecondDimension/Gameplay/M2/EnemyArtIdentity090.cs",
                         "Assets/SecondDimension/Presentation/Battle/Experience/EnemyArt700Runtime090.cs",
                         "Assets/SecondDimension/Presentation/Battle/M2LastRemnantBattleStaging.cs",
                         "Assets/Tests/EditMode/EnemyArt700Runtime090Tests.cs",
                         "Assets/Tests/EditMode/EnemyArtIdentity090Tests.cs"
                     })
                Assert.That(builderSource090, Does.Contain(requiredPath090), requiredPath090);
            Assert.That(builderSource090, Does.Contain(
                "ValidateEnemyArt700PayloadOrThrow090(projectRoot);"));
            Assert.That(builderSource090, Does.Contain(
                "ValidateSourcePayload090(enemyArtRoot090)"));
            Assert.That(builderSource090, Does.Contain(
                ".ValidateCatalog090(verifyFiles090: true)"));

            var builderType090 = Type.GetType(
                "SecondDimension.Editor.Release072.FirstHourGoldWindowsBuild, Assembly-CSharp-Editor",
                throwOnError: false);
            Assert.That(builderType090, Is.Not.Null);
            var validate090 = builderType090.GetMethod(
                "ValidateEnemyArt700PayloadOrThrow090",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(validate090, Is.Not.Null);
            try
            {
                validate090.Invoke(
                    null,
                    new object[]
                    {
                        Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                    });
            }
            catch (TargetInvocationException exception090)
            {
                Assert.Fail(
                    "Release090 EnemyArt700 preflight rejected the current project:\n" +
                    (exception090.InnerException ?? exception090));
            }
        }

        [Test]
        [Explicit(
            "Decodes every one of the 2,100 PNGs. Run as the focused EnemyArt700 visual QA gate.")]
        [Category("EnemyArt700VisualQA")]
        public void EveryExplicitVariantLoadsEveryPoseWithinBoundedCache090()
        {
            EnemyArt700Runtime090.ConfigureCacheCapacity090(12);
            var variants090 = EnemyArt700Runtime090.Catalog090.variants
                .OrderBy(value090 => value090.baseEnemyId, StringComparer.Ordinal)
                .ThenBy(value090 => value090.variantIndex)
                .ThenBy(value090 => value090.variantId, StringComparer.Ordinal)
                .ToArray();

            var loadedPoseCount090 = 0;
            for (var variantIndex090 = 0;
                 variantIndex090 < variants090.Length;
                 variantIndex090++)
            {
                var variant090 = variants090[variantIndex090];
                for (var poseIndex090 = 0;
                     poseIndex090 < AllPoses090.Length;
                     poseIndex090++)
                {
                    var pose090 = AllPoses090[poseIndex090];
                    Assert.That(EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098(
                        variant090.baseEnemyId,
                        variant090.variantId,
                        pose090,
                        out var sprite090,
                        out var error090), Is.True,
                        variant090.variantId + "/" + pose090 + ": " + error090);
                    Assert.That(sprite090, Is.Not.Null);
                    Assert.That(sprite090.texture, Is.Not.Null);
                    Assert.That(
                        EnemyArt700Runtime090.GetDiagnostics090().CachedSpriteCount,
                        Is.LessThanOrEqualTo(12),
                        variant090.variantId + "/" + pose090);
                    loadedPoseCount090++;
                }

                if ((variantIndex090 + 1) % 25 == 0)
                    TestContext.Progress.WriteLine(
                        "EnemyArt700 decoded " + (variantIndex090 + 1) +
                        "/700 variants.");
            }

            var diagnostics090 = EnemyArt700Runtime090.GetDiagnostics090();
            Assert.That(loadedPoseCount090, Is.EqualTo(2100));
            Assert.That(diagnostics090.PngReadCount, Is.EqualTo(2100));
            Assert.That(diagnostics090.CacheHitCount, Is.Zero);
            Assert.That(diagnostics090.CachedSpriteCount, Is.EqualTo(12));
            Assert.That(
                diagnostics090.CachedSpriteCount,
                Is.LessThanOrEqualTo(
                    EnemyArt700Runtime090.MaximumCacheCapacity090));
            Assert.That(diagnostics090.EvictionCount, Is.EqualTo(2088));
        }

        private static Sprite AssertLoad090(
            string baseEnemyId090,
            string variantId090,
            EnemyArt700Pose090 pose090)
        {
            Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                baseEnemyId090,
                variantId090,
                pose090,
                out var sprite090,
                out var error090), Is.True, error090);
            Assert.That(sprite090, Is.Not.Null);
            return sprite090;
        }

        private static void AssertBattlePoseGeometry090(
            Sprite sprite090,
            string label090)
        {
            Assert.That(sprite090, Is.Not.Null, label090);
            Assert.That(sprite090.texture.width, Is.EqualTo(768), label090);
            Assert.That(sprite090.texture.height, Is.EqualTo(1024), label090);
            var pieces091 = label090.Split('/');
            var variant091 = EnemyArt700Runtime090.Catalog090.variants.Single(value => value.variantId == pieces091[0]);
            var pose091 = pieces091[1] == "ATTACK" ? EnemyArt700Pose090.Attack : EnemyArt700Pose090.Idle;
            var resolve091 = typeof(EnemyArt700Runtime090).GetMethod("TryResolvePngPath090",
                BindingFlags.NonPublic | BindingFlags.Static);
            var arguments091 = new object[] { variant091, pose091, null, null };
            Assert.That((bool)resolve091.Invoke(null, arguments091), Is.True);
            var original091 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(original091, File.ReadAllBytes((string)arguments091[2]), false), Is.True);
                var source091 = new Rect(0f, 0f, original091.width, original091.height);
                var visible091 = M1SilhouetteFraming091.VisibleRect091(original091, source091);
                var expected091 = M1SilhouetteFraming091.PaddedRect091(visible091, source091);
                Assert.That(sprite090.rect, Is.EqualTo(expected091), label090 + " exact visible-alpha framing");
                Assert.That(sprite090.rect.xMin, Is.LessThanOrEqualTo(visible091.xMin));
                Assert.That(sprite090.rect.yMin, Is.LessThanOrEqualTo(visible091.yMin));
                Assert.That(sprite090.rect.xMax, Is.GreaterThanOrEqualTo(visible091.xMax));
                Assert.That(sprite090.rect.yMax, Is.GreaterThanOrEqualTo(visible091.yMax));
                Assert.That(sprite090.pivot.x, Is.EqualTo(sprite090.rect.width * 0.5f).Within(0.01f));
                Assert.That(sprite090.pivot.y,
                    Is.EqualTo(expected091 == source091 ? 100f : sprite090.rect.height * 0.035f).Within(0.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(original091); }
        }

        private static Type ResolveBuildCopierType090()
        {
            var type090 = Type.GetType(
                "SecondDimension.Editor.EnemyArt700WindowsBuildCopy090, Assembly-CSharp-Editor",
                throwOnError: false);
            Assert.That(type090, Is.Not.Null);
            return type090;
        }

        private static object InvokeBuildPayloadValidation090(
            Type copierType090,
            string sourceRoot090)
        {
            var method090 = copierType090.GetMethod(
                "ValidateSourcePayload090",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method090, Is.Not.Null);
            return method090.Invoke(null, new object[] { sourceRoot090 });
        }
    }
}
