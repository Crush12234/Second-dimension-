#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    /// <summary>
    /// Keeps the 089 Expedition Deck card faces importable through Resources as
    /// crisp, single-sprite UI artwork. The rule is intentionally limited to the
    /// dedicated card-face folder so unrelated Board086 textures are untouched.
    /// </summary>
    public sealed class ExpeditionCardFace089Postprocessor : AssetPostprocessor
    {
        private const string CardFaceRoot089 =
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/";

        public override uint GetVersion() => 1u;

        private void OnPreprocessTexture()
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(CardFaceRoot089, StringComparison.Ordinal) ||
                !normalizedPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.compressionQuality = 85;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
#endif
