#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    /// <summary>
    /// Imports only the review-build HeroMaster sprites promoted under the stable
    /// HERO_REC_### naming contract. Dossiers use the standing sprite itself, while
    /// battle poses keep a low pivot for grounded actor-rig presentation.
    /// </summary>
    public sealed class HeroMasterSprite089Postprocessor : AssetPostprocessor
    {
        private const string BattleRoot089 =
            "/Resources/SecondDimension/Art/Battle/";
        private const string DossierRoot089 =
            "/Resources/SecondDimension/Art/Portraits/Recruits/";

        public override uint GetVersion() => 2u;

        private void OnPreprocessTexture()
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            if (!TryClassify089(normalizedPath, out var dossier089)) return;

            var importer089 = (TextureImporter)assetImporter;
            importer089.textureType = TextureImporterType.Sprite;
            importer089.spriteImportMode = SpriteImportMode.Single;
            importer089.alphaIsTransparency = true;
            // Presentation creates a cached alpha-bounds sprite at runtime so the
            // supplied source pixels stay byte-identical while padded figures fill
            // their card and actor frame. Readability is scoped to these 171 small
            // review assets and is required only for that one-time bounds scan.
            importer089.isReadable = true;
            importer089.mipmapEnabled = false;
            importer089.wrapMode = TextureWrapMode.Clamp;
            importer089.filterMode = FilterMode.Bilinear;
            importer089.textureCompression = TextureImporterCompression.CompressedHQ;
            importer089.crunchedCompression = false;
            importer089.npotScale = TextureImporterNPOTScale.None;
            importer089.maxTextureSize = 1024;
            importer089.spritePixelsPerUnit = 100f;

            var settings089 = new TextureImporterSettings();
            importer089.ReadTextureSettings(settings089);
            settings089.spriteAlignment = (int)SpriteAlignment.Custom;
            settings089.spritePivot = dossier089
                ? new Vector2(0.5f, 0.5f)
                : new Vector2(0.5f, 0.05f);
            settings089.spriteMeshType = SpriteMeshType.FullRect;
            importer089.SetTextureSettings(settings089);
        }

        private static bool TryClassify089(string normalizedPath089, out bool dossier089)
        {
            dossier089 = false;
            if (!normalizedPath089.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;

            var stem089 = Path.GetFileNameWithoutExtension(normalizedPath089);
            if (normalizedPath089.IndexOf(DossierRoot089, StringComparison.Ordinal) >= 0)
            {
                dossier089 = true;
                return IsStableHeroId089(stem089);
            }

            if (normalizedPath089.IndexOf(BattleRoot089, StringComparison.Ordinal) < 0)
                return false;
            if (stem089.StartsWith("STANDEE_", StringComparison.Ordinal))
                stem089 = stem089.Substring("STANDEE_".Length);
            else if (stem089.StartsWith("ACTION_", StringComparison.Ordinal))
                stem089 = stem089.Substring("ACTION_".Length);
            else
                return false;
            return IsStableHeroId089(stem089);
        }

        private static bool IsStableHeroId089(string value089)
        {
            return value089 != null &&
                   value089.Length == 12 &&
                   value089.StartsWith("HERO_REC_", StringComparison.Ordinal) &&
                   char.IsDigit(value089[9]) &&
                   char.IsDigit(value089[10]) &&
                   char.IsDigit(value089[11]);
        }
    }
}
#endif
