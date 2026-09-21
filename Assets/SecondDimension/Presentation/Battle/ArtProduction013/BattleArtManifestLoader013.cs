using System;
using SecondDimension.Presentation.Battle.ArtProduction011;
using PoseSetManifest011 = SecondDimension.Presentation.Battle.ArtProduction011.BattleArtManifest011;
using UnityEngine;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    public static class BattleArtManifestLoader013
    {
        public const string ResourcePath = "SecondDimension/Art/Generated013/battle_art_manifest_013";
        private static PoseSetManifest011 cache;

        public static PoseSetManifest011 Load()
        {
            if (cache != null) return cache;
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing " + ResourcePath);
            cache = JsonUtility.FromJson<PoseSetManifest011>(asset.text);
            if (cache == null || cache.characters == null || cache.vfx == null || cache.icons == null)
                throw new InvalidOperationException("Battle Art 013 JSON could not be parsed.");
            return cache;
        }
    }
}
