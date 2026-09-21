using System;
using System.Collections.Generic;
using SecondDimension.Presentation.Battle.ArtProduction011;
using PoseSetManifest011 = SecondDimension.Presentation.Battle.ArtProduction011.BattleArtManifest011;
using UnityEngine;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    public static class BattleArtVisualLookup013
    {
        private static Dictionary<string, VfxAsset011> vfxById;
        private static Dictionary<string, IconAsset011> iconById;

        public static bool TryLoadVfxSprite(string assetId, out Sprite sprite)
        {
            EnsureIndexes();
            sprite = null;
            if (!vfxById.TryGetValue(assetId ?? string.Empty, out VfxAsset011 entry)) return false;
            sprite = Resources.Load<Sprite>(entry.resourcesPath);
            return sprite != null;
        }

        public static bool TryLoadIconSprite(string assetId, out Sprite sprite)
        {
            EnsureIndexes();
            sprite = null;
            if (!iconById.TryGetValue(assetId ?? string.Empty, out IconAsset011 entry)) return false;
            sprite = Resources.Load<Sprite>(entry.resourcesPath);
            return sprite != null;
        }

        public static string VfxResourcePath(string assetId)
        {
            EnsureIndexes();
            return vfxById.TryGetValue(assetId ?? string.Empty, out VfxAsset011 entry)
                ? entry.resourcesPath
                : string.Empty;
        }

        private static void EnsureIndexes()
        {
            if (vfxById != null) return;
            vfxById = new Dictionary<string, VfxAsset011>(StringComparer.Ordinal);
            iconById = new Dictionary<string, IconAsset011>(StringComparer.Ordinal);
            PoseSetManifest011 manifest = BattleArtManifestLoader013.Load();
            foreach (VfxAsset011 value in manifest.vfx)
                if (value != null && !string.IsNullOrWhiteSpace(value.assetId)) vfxById[value.assetId] = value;
            foreach (IconAsset011 value in manifest.icons)
                if (value != null && !string.IsNullOrWhiteSpace(value.assetId)) iconById[value.assetId] = value;
        }
    }
}
