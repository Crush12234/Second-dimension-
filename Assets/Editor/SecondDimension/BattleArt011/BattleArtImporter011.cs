#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation;

namespace SecondDimension.Editor
{
    /// <summary>Applies consistent import settings to the Thursday 011 local art/audio library.</summary>
    public sealed class BattleArtAssetPostprocessor011 : AssetPostprocessor
    {
        private bool IsBattleArt011 => assetPath.Replace('\\', '/').Contains("/Battle011/", StringComparison.Ordinal);

        private void OnPreprocessTexture()
        {
            if (!IsBattleArt011) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = assetPath.Contains("/Icons/", StringComparison.Ordinal) ? 256 :
                assetPath.Contains("/VFX/", StringComparison.Ordinal) ? 1024 : 2048;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            if (assetPath.Contains("/Characters/ENEMY_", StringComparison.Ordinal))
                textureSettings.spritePivot = new Vector2(0.5f, 0.18f);
            else if (assetPath.Contains("/Characters/", StringComparison.Ordinal))
                textureSettings.spritePivot = new Vector2(0.5f, 0.05f);
            else
                textureSettings.spritePivot = new Vector2(0.5f, 0.5f);
            // These six authored player pose sets need their real alpha mesh for
            // shared visible-height fitting. Keep UI, effects, enemies and the
            // guildmaster's existing import policy exactly as authored.
            textureSettings.spriteMeshType = UsesAlliedSilhouette091(assetPath)
                ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            if (assetPath.Contains("/UI/", StringComparison.Ordinal))
                textureSettings.spriteBorder = new Vector4(28f, 28f, 28f, 28f);
            importer.SetTextureSettings(textureSettings);
        }

        private static bool UsesAlliedSilhouette091(string path)
        {
            const string root = "Assets/Resources/SecondDimension/Art/Battle011/Characters/";
            var normalized = (path ?? string.Empty).Replace('\\', '/');
            if (!normalized.StartsWith(root, StringComparison.Ordinal) ||
                !normalized.EndsWith(".png", StringComparison.Ordinal)) return false;
            var relative = normalized.Substring(root.Length);
            var separator = relative.IndexOf('/');
            if (separator < 0 || !relative.Substring(separator + 1).StartsWith("POSE_", StringComparison.Ordinal))
                return false;
            switch (relative.Substring(0, separator))
            {
                case "SIGREC_MAREN_HOLT":
                case "SIGREC_ODELIA_FEN":
                case "PROC_36344E2400DC98B6":
                case "PROC_F85A4CAA747BC8C6":
                case "PROC_5B14E7816E55FFB5":
                case "PROC_748DD03A23E1FEB0":
                    return true;
                default:
                    return false;
            }
        }

        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            if (!UsesAlliedSilhouette091(assetPath) || texture == null || sprites == null) return;
            // Unity's automatic Tight hull includes alpha-1 glow and floor noise.
            // Derive framing geometry from actual visible pixels at import time;
            // the non-readable player texture remains unchanged in the build.
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(assetPath), false)) return;
                var visible = M1SilhouetteFraming091.VisibleRect091(decoded,
                    new Rect(0f, 0f, decoded.width, decoded.height));
                var pixelScale = new Vector2(texture.width / (float)decoded.width,
                    texture.height / (float)decoded.height);
                visible = new Rect(Vector2.Scale(visible.position, pixelScale),
                    Vector2.Scale(visible.size, pixelScale));
                foreach (var sprite in sprites)
                {
                    if (sprite == null) continue;
                    var rect = sprite.rect;
                    var min = Vector2.Max(visible.min, rect.min);
                    var max = Vector2.Min(visible.max, rect.max);
                    if (max.x <= min.x || max.y <= min.y) continue;
                    // OverrideGeometry takes sprite-rect pixel coordinates. Unity
                    // applies pivot and pixels-per-unit when exposing vertices.
                    var origin = rect.position;
                    sprite.OverrideGeometry(new[]
                    {
                        new Vector2(min.x, min.y) - origin,
                        new Vector2(min.x, max.y) - origin,
                        new Vector2(max.x, max.y) - origin,
                        new Vector2(max.x, min.y) - origin
                    }, new ushort[] { 0, 1, 2, 0, 2, 3 });
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }

        private void OnPreprocessAudio()
        {
            if (!IsBattleArt011) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.quality = 0.85f;
            importer.defaultSampleSettings = settings;
        }
    }

    public static class BattleArtValidationMenu011
    {
        [MenuItem("Second Dimension/Battle Art 011/Validate Thursday Package", false, 5100)]
        public static void Validate()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var issues = BattleArtRuntimeRegistry011.ValidateRuntime().ToList();
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            foreach (var character in manifest.characters ?? Array.Empty<BattleArtCharacter011>())
            {
                foreach (var pose in character.poses ?? Array.Empty<BattleArtPose011>())
                    if (Resources.Load<Sprite>(pose.resourcePath) == null)
                        issues.Add("MISSING_SPRITE " + character.memberId + " " + pose.poseId + " " + pose.resourcePath);
            }
            foreach (var vfx in manifest.vfx ?? Array.Empty<BattleArtVfx011>())
                if (Resources.Load<Sprite>(vfx.resourcePath) == null) issues.Add("MISSING_VFX " + vfx.vfxId);
            foreach (var cue in manifest.audio ?? Array.Empty<BattleArtAudio011>())
                if (Resources.Load<AudioClip>(cue.resourcePath) == null) issues.Add("MISSING_AUDIO " + cue.cueId);
            foreach (var profile in manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>())
            {
                ValidateProfileResource(profile.artId, "START_AUDIO", profile.startAudioResourcePath, true, issues);
                ValidateProfileResource(profile.artId, "IMPACT_AUDIO", profile.impactAudioResourcePath, true, issues);
                ValidateProfileResource(profile.artId, "TRAIL", profile.trailResourcePath, false, issues);
                ValidateProfileResource(profile.artId, "PROJECTILE", profile.projectileResourcePath, false, issues);
                ValidateProfileResource(profile.artId, "FIELD", profile.fieldResourcePath, false, issues);
                ValidateProfileResource(profile.artId, "IMPACT_VFX", profile.impactResourcePath, false, issues);
            }
            foreach (var asset in manifest.uiAssets ?? Array.Empty<BattleArtUiAsset011>())
                if (Resources.Load<Sprite>(asset.resourcePath) == null) issues.Add("MISSING_UI_ASSET " + asset.assetId);

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            var reportDir = Path.Combine(projectRoot, "BuildEvidence");
            Directory.CreateDirectory(reportDir);
            var reportPath = Path.Combine(reportDir, "BATTLE_ART_011_VALIDATION.txt");
            File.WriteAllLines(reportPath, new[]
            {
                "SECOND DIMENSION — BATTLE ART 011 VALIDATION",
                "Content version: " + manifest.contentVersion,
                "Characters: " + manifest.characterCount,
                "VFX: " + manifest.vfxCount,
                "Audio: " + manifest.audioCount,
                "Art profiles: " + manifest.runtimeArtProfileCount,
                "Legacy aliases: " + manifest.legacyAliasCount,
                "Result: " + (issues.Count == 0 ? "PASS" : "FAIL"),
                "",
            }.Concat(issues).ToArray());
            AssetDatabase.Refresh();
            if (issues.Count == 0) Debug.Log("Battle Art 011 validation PASS. Report: " + reportPath);
            else Debug.LogError("Battle Art 011 validation FAIL with " + issues.Count + " issue(s). Report: " + reportPath);
        }

        public static void ValidateFromCommandLine()
        {
            Validate();
            BattleArtRuntimeRegistry011.ReloadForTests();
            var issues = BattleArtRuntimeRegistry011.ValidateRuntime().ToList();
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            foreach (var character in manifest.characters ?? Array.Empty<BattleArtCharacter011>())
            {
                foreach (var pose in character.poses ?? Array.Empty<BattleArtPose011>())
                    if (Resources.Load<Sprite>(pose.resourcePath) == null)
                        issues.Add("MISSING_SPRITE " + character.memberId + " " + pose.poseId);
            }
            foreach (var vfx in manifest.vfx ?? Array.Empty<BattleArtVfx011>())
                if (Resources.Load<Sprite>(vfx.resourcePath) == null) issues.Add("MISSING_VFX " + vfx.vfxId);
            foreach (var cue in manifest.audio ?? Array.Empty<BattleArtAudio011>())
                if (Resources.Load<AudioClip>(cue.resourcePath) == null) issues.Add("MISSING_AUDIO " + cue.cueId);
            foreach (var profile in manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>())
            {
                ValidateProfileResource(profile.artId, "START_AUDIO", profile.startAudioResourcePath, true, issues);
                ValidateProfileResource(profile.artId, "IMPACT_AUDIO", profile.impactAudioResourcePath, true, issues);
                ValidateProfileResource(profile.artId, "TRAIL", profile.trailResourcePath, false, issues);
                ValidateProfileResource(profile.artId, "PROJECTILE", profile.projectileResourcePath, false, issues);
                ValidateProfileResource(profile.artId, "FIELD", profile.fieldResourcePath, false, issues);
                ValidateProfileResource(profile.artId, "IMPACT_VFX", profile.impactResourcePath, false, issues);
            }
            foreach (var asset in manifest.uiAssets ?? Array.Empty<BattleArtUiAsset011>())
                if (Resources.Load<Sprite>(asset.resourcePath) == null) issues.Add("MISSING_UI_ASSET " + asset.assetId);
            EditorApplication.Exit(issues.Count == 0 ? 0 : 1);
        }

        private static void ValidateProfileResource(
            string artId,
            string label,
            string resourcePath,
            bool audio,
            System.Collections.Generic.ICollection<string> issues)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return;
            var loaded = audio
                ? (UnityEngine.Object)Resources.Load<AudioClip>(resourcePath)
                : Resources.Load<Sprite>(resourcePath);
            if (loaded == null) issues.Add("MISSING_PROFILE_" + label + " " + artId + " " + resourcePath);
        }

        [MenuItem("Second Dimension/Battle Art 011/Reimport All Art and Audio", false, 5101)]
        public static void ReimportAll()
        {
            var guids = AssetDatabase.FindAssets(string.Empty, new[]
            {
                "Assets/Resources/SecondDimension/Art/Battle011",
                "Assets/Resources/SecondDimension/Audio/Battle011"
            });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("Battle Art 011 reimport completed for " + guids.Length + " assets.");
        }
    }
}
#endif
