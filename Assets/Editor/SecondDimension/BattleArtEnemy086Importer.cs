#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    /// <summary>
    /// Keeps the original 086 enemy standees lossless in shape and ready for direct
    /// Resources Sprite loading. The rule is deliberately limited to this art pack.
    /// </summary>
    public sealed class BattleArtEnemy086Postprocessor : AssetPostprocessor
    {
        private const string EnemyArtRoot086 =
            "/Resources/SecondDimension/Art/Battle086/Enemies/";

        private void OnPreprocessTexture()
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            if (normalizedPath.IndexOf(
                    EnemyArtRoot086, StringComparison.Ordinal) < 0)
                return;

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
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0.06f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
#endif
