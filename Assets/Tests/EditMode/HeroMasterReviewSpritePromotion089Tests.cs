using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroMasterReviewSpritePromotion089Tests
    {
        private const string ManifestResource089 =
            "SecondDimension/HeroMaster300/Data/HERO_300_REVIEW_SPRITE_PROMOTION_089";
        private const string PromotionStatus089 =
            "PROMOTED_USER_SUPPLIED_REVIEW_BUILD_PENDING_USER_RIGHTS_CONFIRMATION";

        private static readonly string[] ExpectedHeroIds089 =
        {
            "HERO_REC_131", "HERO_REC_132", "HERO_REC_133", "HERO_REC_134", "HERO_REC_135",
            "HERO_REC_136", "HERO_REC_137", "HERO_REC_138", "HERO_REC_139",
            "HERO_REC_149", "HERO_REC_150",
            "HERO_REC_151", "HERO_REC_152", "HERO_REC_153", "HERO_REC_154", "HERO_REC_155",
            "HERO_REC_156", "HERO_REC_157", "HERO_REC_158", "HERO_REC_159", "HERO_REC_160",
            "HERO_REC_161", "HERO_REC_162", "HERO_REC_163", "HERO_REC_164", "HERO_REC_165",
            "HERO_REC_166", "HERO_REC_167", "HERO_REC_168", "HERO_REC_182", "HERO_REC_184",
            "HERO_REC_190", "HERO_REC_194", "HERO_REC_219", "HERO_REC_224", "HERO_REC_226",
            "HERO_REC_227", "HERO_REC_229", "HERO_REC_230", "HERO_REC_231", "HERO_REC_235",
            "HERO_REC_237", "HERO_REC_242", "HERO_REC_243", "HERO_REC_244", "HERO_REC_246",
            "HERO_REC_247", "HERO_REC_251", "HERO_REC_252", "HERO_REC_254", "HERO_REC_255",
            "HERO_REC_263", "HERO_REC_264", "HERO_REC_273", "HERO_REC_283", "HERO_REC_287",
            "HERO_REC_291"
        };

        private static readonly string[] AcceptedSignatureHeroIds089 =
        {
            "SIGREC_MAREN_HOLT",
            "SIGREC_BRAKKA_EMBERWALL",
            "SIGREC_ODELIA_FEN",
            "SIGREC_TOVVI_COPPERSPARK",
            "SIGREC_RUSK_FENRUNNER",
            "SIGREC_TALA_STORMROAD",
            "SIGREC_ORREN_CLAY",
            "SIGREC_BESSA_BRASSWHISTLE",
            "SIGREC_VAELIS_NOCT"
        };

        [Test]
        public void Manifest089_RecordsExactlyTheHashVerifiedReviewPromotion()
        {
            Assert.That(ExpectedHeroIds089.Length, Is.EqualTo(57));
            Assert.That(ExpectedHeroIds089.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(57));

            var manifestAsset089 = Resources.Load<TextAsset>(ManifestResource089);
            Assert.That(manifestAsset089, Is.Not.Null);
            var manifest089 = JsonUtility.FromJson<PromotionManifest089>(manifestAsset089.text);
            Assert.That(manifest089, Is.Not.Null);
            Assert.That(manifest089.source_archive_sha256,
                Is.EqualTo("18B20C3F0BEF43888DB9774124E08F5BC4E708E68F8E8C76CF59B3AD28264612"));
            Assert.That(manifest089.rights_status, Is.EqualTo("PENDING_USER_RIGHTS_CONFIRMATION"));
            Assert.That(manifest089.promoted_hero_count, Is.EqualTo(57));
            Assert.That(manifest089.promoted_distinct_source_sprite_count, Is.EqualTo(114));
            Assert.That(manifest089.promoted_destination_asset_count, Is.EqualTo(171));
            Assert.That(manifest089.accepted_catalog_hero_count, Is.EqualTo(250));
            Assert.That(manifest089.preexisting_exact_unique_sprite_set_count, Is.EqualTo(9));
            Assert.That(manifest089.exact_unique_sprite_set_count, Is.EqualTo(66));
            Assert.That(manifest089.deterministic_sprite_fallback_count, Is.EqualTo(184));
            Assert.That(manifest089.destination_assets, Has.Length.EqualTo(171));

            var expected089 = new HashSet<string>(ExpectedHeroIds089, StringComparer.Ordinal);
            var grouped089 = manifest089.destination_assets
                .GroupBy(value089 => value089.stable_id, StringComparer.Ordinal)
                .ToDictionary(value089 => value089.Key, value089 => value089.ToArray(), StringComparer.Ordinal);
            Assert.That(grouped089.Keys, Is.EquivalentTo(expected089));

            foreach (var heroId089 in ExpectedHeroIds089)
            {
                Assert.That(grouped089[heroId089], Has.Length.EqualTo(3), heroId089);
                Assert.That(grouped089[heroId089].Select(value089 => value089.destination_role),
                    Is.EquivalentTo(new[] { "standing", "action", "sprite_dossier" }), heroId089);
            }

            foreach (var asset089 in manifest089.destination_assets)
            {
                Assert.That(asset089.status, Is.EqualTo(PromotionStatus089), asset089.destination_path);
                Assert.That(expected089.Contains(asset089.stable_id), Is.True, asset089.stable_id);
                Assert.That(File.Exists(asset089.destination_path), Is.True, asset089.destination_path);
                Assert.That(Sha256Hex089(asset089.destination_path),
                    Is.EqualTo(asset089.destination_sha256), asset089.destination_path);
                Assert.That(asset089.destination_sha256,
                    Is.EqualTo(asset089.source_sha256), asset089.destination_path);
            }
        }

        [Test]
        public void AllAcceptedHeroes089_ResolveSpriteFormUiAndBattleCoverage_WithoutMislabelingFallbacks()
        {
            var catalogSource089 = Resources.Load<TextAsset>(
                "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300");
            Assert.That(catalogSource089, Is.Not.Null);
            var catalog089 = HeroMaster300Catalog087.FromJson(catalogSource089.text);
            Assert.That(catalog089.AcceptedHeroes, Has.Count.EqualTo(250));
            Assert.That(catalog089.QuarantinedHeroes, Has.Count.EqualTo(50));

            var exactUniqueCount089 = 0;
            var fallbackCount089 = 0;
            var fallbackStandeeFingerprints089 = new HashSet<ulong>();
            var fallbackActionFingerprints089 = new HashSet<ulong>();
            foreach (var hero089 in catalog089.AcceptedHeroes)
            {
                Assert.That(M1VisualAssets.TryResolvePortrait(
                    "RUNTIME_" + hero089.StableId,
                    hero089.StableId,
                    hero089.Race,
                    hero089.StableId,
                    hero089.Role,
                    out var dossier089,
                    out var dossierKey089), Is.True, hero089.StableId);
                Assert.That(M1VisualAssets.TryResolveBattleStandee(
                    "RUNTIME_" + hero089.StableId,
                    hero089.StableId,
                    hero089.Race,
                    hero089.StableId,
                    out var standee089,
                    out var standeeKey089), Is.True, hero089.StableId);
                Assert.That(M1VisualAssets.TryResolveBattleActionPose(
                    "RUNTIME_" + hero089.StableId,
                    hero089.StableId,
                    hero089.Race,
                    hero089.StableId,
                    out var action089,
                    out var actionKey089), Is.True, hero089.StableId);

                Assert.That(dossier089, Is.Not.Null, hero089.StableId);
                Assert.That(standee089, Is.Not.Null, hero089.StableId);
                Assert.That(action089, Is.Not.Null, hero089.StableId);
                Assert.That(dossier089.texture, Is.Not.Null, hero089.StableId);
                Assert.That(standee089.texture, Is.Not.Null, hero089.StableId);
                Assert.That(action089.texture, Is.Not.Null, hero089.StableId);
                Assert.That(ReferenceEquals(dossier089.texture, standee089.texture), Is.True,
                    hero089.StableId + " dossier must use the full-body standing texture");
                Assert.That(dossier089.rect, Is.EqualTo(standee089.rect),
                    hero089.StableId + " dossier must preserve the complete standing frame");
                AssertPhoneVisibleSprite089(standee089, hero089.StableId + " standing");
                AssertPhoneVisibleSprite089(action089, hero089.StableId + " action");

                var dossierFallback089 = M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(dossierKey089);
                var standeeFallback089 = M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(standeeKey089);
                var actionFallback089 = M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(actionKey089);
                Assert.That(standeeFallback089, Is.EqualTo(dossierFallback089), hero089.StableId);
                Assert.That(actionFallback089, Is.EqualTo(dossierFallback089), hero089.StableId);

                if (dossierFallback089)
                {
                    fallbackCount089++;
                    StringAssert.Contains(hero089.StableId, dossierKey089);
                    StringAssert.Contains(hero089.StableId, standeeKey089);
                    StringAssert.Contains(hero089.StableId, actionKey089);
                    StringAssert.StartsWith(M1VisualAssets.HeroMasterSpriteFallbackRoot089, dossierKey089);
                    StringAssert.StartsWith(M1VisualAssets.HeroMasterSpriteFallbackRoot089, standeeKey089);
                    StringAssert.StartsWith(M1VisualAssets.HeroMasterSpriteFallbackRoot089, actionKey089);
                    Assert.That(standee089.texture.isReadable, Is.True, hero089.StableId);
                    Assert.That(action089.texture.isReadable, Is.True, hero089.StableId);
                    var standingFingerprint089 = PixelFingerprint089(standee089);
                    var actionFingerprint089 = PixelFingerprint089(action089);
                    Assert.That(fallbackStandeeFingerprints089.Add(standingFingerprint089), Is.True,
                        hero089.StableId + " repeated another generated standing sprite");
                    Assert.That(fallbackActionFingerprints089.Add(actionFingerprint089), Is.True,
                        hero089.StableId + " repeated another generated action sprite");
                    Assert.That(actionFingerprint089, Is.Not.EqualTo(standingFingerprint089),
                        hero089.StableId + " action must be visibly different from idle");
                }
                else
                {
                    exactUniqueCount089++;
                    StringAssert.Contains(hero089.StableId, dossierKey089);
                    StringAssert.Contains(hero089.StableId, standeeKey089);
                    StringAssert.Contains(hero089.StableId, actionKey089);
                    Assert.That(dossierKey089, Is.EqualTo(standeeKey089),
                        hero089.StableId + " exact dossier must use its full-body standee");
                }
            }

            // 57 same-identity source pairs from the supplied archive plus nine
            // accepted signature heroes that already had exact UI/stand/action art,
            // plus these independently reviewed original exact-ID 093 atlases, not family substitutions.
            var reviewedRemasterIds093 = new[]
            {
                "HERO_REC_012", "HERO_REC_015", "HERO_REC_019", "HERO_REC_022", "HERO_REC_025", "HERO_REC_029",
                "HERO_REC_046", "HERO_REC_056", "HERO_REC_066", "HERO_REC_069", "HERO_REC_079", "HERO_REC_106",
                "HERO_REC_169", "HERO_REC_170", "HERO_REC_171", "HERO_REC_172", "HERO_REC_173", "HERO_REC_174",
                "HERO_REC_175", "HERO_REC_176", "HERO_REC_177", "HERO_REC_178", "HERO_REC_179", "HERO_REC_180"
            };
            Assert.That(exactUniqueCount089, Is.EqualTo(66 + reviewedRemasterIds093.Length));
            Assert.That(fallbackCount089, Is.EqualTo(184 - reviewedRemasterIds093.Length));
            Assert.That(exactUniqueCount089 + fallbackCount089, Is.EqualTo(250));
            Assert.That(fallbackStandeeFingerprints089, Has.Count.EqualTo(184 - reviewedRemasterIds093.Length));
            Assert.That(fallbackActionFingerprints089, Has.Count.EqualTo(184 - reviewedRemasterIds093.Length));
        }

        [Test]
        public void AcceptedSignatureDossiers089_UseFullBodyStandeeInsteadOfBustPortrait()
        {
            Assert.That(AcceptedSignatureHeroIds089, Has.Length.EqualTo(9));
            foreach (var heroId089 in AcceptedSignatureHeroIds089)
            {
                Assert.That(M1VisualAssets.TryResolvePortrait(
                    "RUNTIME_" + heroId089,
                    "VISUAL_" + heroId089,
                    "HUMAN",
                    heroId089,
                    out var dossier089,
                    out var dossierKey089), Is.True, heroId089);

                Assert.That(dossier089, Is.Not.Null, heroId089);
                Assert.That(dossierKey089,
                    Is.EqualTo(M1VisualAssets.BattleRoot + "/STANDEE_" + heroId089), heroId089);
                Assert.That(dossier089.rect.height, Is.GreaterThan(dossier089.rect.width), heroId089);
                Assert.That(dossierKey089, Does.Not.StartWith(M1VisualAssets.PortraitRoot), heroId089);
            }
        }

        [Test]
        public void EveryPromotedHero089_ResolvesThroughExistingStableVisualAuthority()
        {
            foreach (var heroId089 in ExpectedHeroIds089)
            {
                var standeeKey089 = M1VisualAssets.BattleRoot + "/STANDEE_" + heroId089;
                var actionKey089 = M1VisualAssets.BattleRoot + "/ACTION_" + heroId089;

                Assert.That(M1VisualAssets.TryResolvePortrait(
                    "RUNTIME_" + heroId089,
                    "VISUAL_" + heroId089,
                    "HUMAN",
                    heroId089,
                    out var portrait089,
                    out var resolvedPortraitKey089), Is.True, heroId089);
                Assert.That(M1VisualAssets.TryResolveBattleStandee(
                    "RUNTIME_" + heroId089,
                    "VISUAL_" + heroId089,
                    "HUMAN",
                    heroId089,
                    out var standee089,
                    out var resolvedStandeeKey089), Is.True, heroId089);
                Assert.That(M1VisualAssets.TryResolveBattleActionPose(
                    "RUNTIME_" + heroId089,
                    "VISUAL_" + heroId089,
                    "HUMAN",
                    heroId089,
                    out var action089,
                    out var resolvedActionKey089), Is.True, heroId089);

                Assert.That(portrait089, Is.Not.Null, heroId089);
                Assert.That(standee089, Is.Not.Null, heroId089);
                Assert.That(action089, Is.Not.Null, heroId089);
                if (heroId089 == "HERO_REC_287")
                {
                    // Freya's old source was a torso fragment. The complete 091
                    // body replaces that runtime frame, not its archived provenance.
                    standeeKey089 = "SecondDimension/Art/Standees/HeroRemaster091/HERO_REC_287_IDLE_091";
                    actionKey089 = standeeKey089;
                    Assert.That(standee089.texture.height, Is.EqualTo(1536));
                    Assert.That(action089, Is.SameAs(standee089));
                }
                else
                {
                    StringAssert.EndsWith("_BOUNDS_FIT_089", portrait089.name, heroId089);
                    StringAssert.EndsWith("_BOUNDS_FIT_089", standee089.name, heroId089);
                    StringAssert.EndsWith("_BOUNDS_FIT_089", action089.name, heroId089);
                }
                Assert.That(resolvedPortraitKey089, Is.EqualTo(standeeKey089), heroId089);
                Assert.That(resolvedStandeeKey089, Is.EqualTo(standeeKey089), heroId089);
                Assert.That(resolvedActionKey089, Is.EqualTo(actionKey089), heroId089);
                Assert.That(ReferenceEquals(portrait089, standee089), Is.True, heroId089);
                Assert.That(portrait089.texture.width, Is.EqualTo(standee089.texture.width), heroId089);
                Assert.That(portrait089.texture.height, Is.EqualTo(standee089.texture.height), heroId089);
            }
        }

        [Test]
        public void EveryPromotedHero089_UsesScopedSingleSpriteImportSettings()
        {
            foreach (var heroId089 in ExpectedHeroIds089)
            {
                AssertImporter089(
                    "Assets/Resources/SecondDimension/Art/Battle/STANDEE_" + heroId089 + ".png",
                    new Vector2(0.5f, 0.05f));
                AssertImporter089(
                    "Assets/Resources/SecondDimension/Art/Battle/ACTION_" + heroId089 + ".png",
                    new Vector2(0.5f, 0.05f));
                AssertImporter089(
                    "Assets/Resources/SecondDimension/Art/Portraits/Recruits/" + heroId089 + ".png",
                    new Vector2(0.5f, 0.5f));
            }
        }

        private static void AssertImporter089(string assetPath089, Vector2 expectedPivot089)
        {
            var importer089 = AssetImporter.GetAtPath(assetPath089) as TextureImporter;
            Assert.That(importer089, Is.Not.Null, assetPath089);
            Assert.That(importer089.textureType, Is.EqualTo(TextureImporterType.Sprite), assetPath089);
            Assert.That(importer089.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), assetPath089);
            Assert.That(importer089.alphaIsTransparency, Is.True, assetPath089);
            Assert.That(importer089.isReadable, Is.True, assetPath089);
            Assert.That(importer089.mipmapEnabled, Is.False, assetPath089);
            Assert.That(importer089.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), assetPath089);
            Assert.That(importer089.filterMode, Is.EqualTo(FilterMode.Bilinear), assetPath089);
            Assert.That(importer089.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), assetPath089);
            Assert.That(importer089.spritePixelsPerUnit, Is.EqualTo(100f), assetPath089);

            var settings089 = new TextureImporterSettings();
            importer089.ReadTextureSettings(settings089);
            Assert.That(settings089.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Custom), assetPath089);
            Assert.That(settings089.spritePivot.x, Is.EqualTo(expectedPivot089.x).Within(0.001f), assetPath089);
            Assert.That(settings089.spritePivot.y, Is.EqualTo(expectedPivot089.y).Within(0.001f), assetPath089);
            Assert.That(settings089.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), assetPath089);
        }

        private static string Sha256Hex089(string path089)
        {
            using (var stream089 = File.OpenRead(path089))
            using (var sha089 = SHA256.Create())
            {
                return BitConverter.ToString(sha089.ComputeHash(stream089)).Replace("-", string.Empty);
            }
        }

        private static void AssertPhoneVisibleSprite089(Sprite sprite089, string scope089)
        {
            Assert.That(sprite089, Is.Not.Null, scope089);
            Assert.That(sprite089.rect.width, Is.GreaterThanOrEqualTo(48f), scope089);
            Assert.That(sprite089.rect.height, Is.GreaterThanOrEqualTo(64f), scope089);
            if (!sprite089.texture.isReadable) return;

            var pixels089 = sprite089.texture.GetPixels32();
            var textureWidth089 = sprite089.texture.width;
            var minX089 = Mathf.CeilToInt(sprite089.rect.xMin);
            var minY089 = Mathf.CeilToInt(sprite089.rect.yMin);
            var maxX089 = Mathf.FloorToInt(sprite089.rect.xMax) - 1;
            var maxY089 = Mathf.FloorToInt(sprite089.rect.yMax) - 1;
            var visibleMinX089 = maxX089;
            var visibleMinY089 = maxY089;
            var visibleMaxX089 = -1;
            var visibleMaxY089 = -1;
            var visiblePixelCount089 = 0;
            for (var y089 = minY089; y089 <= maxY089; y089++)
            for (var x089 = minX089; x089 <= maxX089; x089++)
            {
                if (pixels089[y089 * textureWidth089 + x089].a < 16) continue;
                visiblePixelCount089++;
                visibleMinX089 = Math.Min(visibleMinX089, x089);
                visibleMinY089 = Math.Min(visibleMinY089, y089);
                visibleMaxX089 = Math.Max(visibleMaxX089, x089);
                visibleMaxY089 = Math.Max(visibleMaxY089, y089);
            }

            var framePixelCount089 = Math.Max(1,
                (maxX089 - minX089 + 1) * (maxY089 - minY089 + 1));
            Assert.That(visiblePixelCount089 / (float)framePixelCount089,
                Is.GreaterThanOrEqualTo(0.025f), scope089 + " is effectively transparent");
            Assert.That((visibleMaxX089 - visibleMinX089 + 1) / sprite089.rect.width,
                Is.GreaterThanOrEqualTo(0.35f), scope089 + " is horizontally cropped to invisibility");
            Assert.That((visibleMaxY089 - visibleMinY089 + 1) / sprite089.rect.height,
                Is.GreaterThanOrEqualTo(0.55f), scope089 + " is vertically cropped to invisibility");
        }

        private static ulong PixelFingerprint089(Sprite sprite089)
        {
            unchecked
            {
                var fingerprint089 = 1469598103934665603UL;
                foreach (var pixel089 in sprite089.texture.GetPixels32())
                {
                    fingerprint089 = (fingerprint089 ^ pixel089.r) * 1099511628211UL;
                    fingerprint089 = (fingerprint089 ^ pixel089.g) * 1099511628211UL;
                    fingerprint089 = (fingerprint089 ^ pixel089.b) * 1099511628211UL;
                    fingerprint089 = (fingerprint089 ^ pixel089.a) * 1099511628211UL;
                }
                return fingerprint089;
            }
        }

        [Serializable]
        private sealed class PromotionManifest089
        {
            public string source_archive_sha256;
            public string rights_status;
            public int promoted_hero_count;
            public int promoted_distinct_source_sprite_count;
            public int promoted_destination_asset_count;
            public int accepted_catalog_hero_count;
            public int preexisting_exact_unique_sprite_set_count;
            public int exact_unique_sprite_set_count;
            public int deterministic_sprite_fallback_count;
            public PromotionAsset089[] destination_assets;
        }

        [Serializable]
        private sealed class PromotionAsset089
        {
            public string stable_id;
            public string destination_role;
            public string source_sha256;
            public string destination_path;
            public string destination_sha256;
            public string status;
        }
    }
}
