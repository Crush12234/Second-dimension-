#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    /// <summary>
    /// Imports only the EnemyArt700 texture drop as single sprites. The source pack
    /// is also read directly at runtime; these settings keep Editor previews useful
    /// without changing or rescaling the supplied PNG files.
    /// </summary>
    public sealed class EnemyArt700TexturePostprocessor090 : AssetPostprocessor
    {
        public const string TextureRoot090 =
            "Assets/SecondDimension/EnemyArt700/Textures/";

        public override uint GetVersion() => 2u;

        private void OnPreprocessTexture()
        {
            var normalizedPath090 = assetPath.Replace('\\', '/');
            if (!normalizedPath090.StartsWith(TextureRoot090, StringComparison.Ordinal) ||
                !normalizedPath090.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;

            var importer090 = (TextureImporter)assetImporter;
            importer090.textureType = TextureImporterType.Sprite;
            importer090.spriteImportMode = SpriteImportMode.Single;
            importer090.alphaSource = TextureImporterAlphaSource.FromInput;
            importer090.alphaIsTransparency = true;
            importer090.sRGBTexture = true;
            importer090.isReadable = false;
            importer090.mipmapEnabled = false;
            importer090.streamingMipmaps = false;
            importer090.wrapMode = TextureWrapMode.Clamp;
            importer090.filterMode = FilterMode.Bilinear;
            importer090.npotScale = TextureImporterNPOTScale.None;
            importer090.textureCompression = TextureImporterCompression.CompressedHQ;
            importer090.compressionQuality = 85;
            importer090.crunchedCompression = false;
            importer090.maxTextureSize = Math.Max(2048, importer090.maxTextureSize);
            importer090.spritePixelsPerUnit = 100f;

            var spriteSettings090 = new TextureImporterSettings();
            importer090.ReadTextureSettings(spriteSettings090);
            var portrait090 = normalizedPath090.EndsWith(
                "_PORTRAIT.png",
                StringComparison.OrdinalIgnoreCase);
            spriteSettings090.spriteAlignment = portrait090
                ? (int)SpriteAlignment.Center
                : (int)SpriteAlignment.Custom;
            spriteSettings090.spritePivot = portrait090
                ? new Vector2(0.5f, 0.5f)
                : new Vector2(0.5f, 100f / 1024f);
            spriteSettings090.spriteMeshType = SpriteMeshType.FullRect;
            importer090.SetTextureSettings(spriteSettings090);

            var windowsSettings090 = importer090.GetPlatformTextureSettings("Standalone");
            windowsSettings090.name = "Standalone";
            windowsSettings090.overridden = true;
            windowsSettings090.maxTextureSize = Math.Max(2048, windowsSettings090.maxTextureSize);
            windowsSettings090.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            windowsSettings090.format = TextureImporterFormat.DXT5;
            windowsSettings090.textureCompression = TextureImporterCompression.CompressedHQ;
            windowsSettings090.compressionQuality = 85;
            windowsSettings090.crunchedCompression = false;
            windowsSettings090.allowsAlphaSplitting = false;
            importer090.SetPlatformTextureSettings(windowsSettings090);
        }
    }
}
#endif
