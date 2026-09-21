using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    public static class ArtPresentationBindingLoader013
    {
        public const string ResourcePath = "SecondDimension/Art/Generated013/art_presentation_bindings_013";
        private static ArtPresentationBindingManifest013 cache;
        private static Dictionary<string, ArtPresentationBinding013> byId;

        public static ArtPresentationBindingManifest013 Load()
        {
            if (cache != null) return cache;
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing " + ResourcePath);
            cache = JsonUtility.FromJson<ArtPresentationBindingManifest013>(asset.text);
            if (cache == null || cache.bindings == null)
                throw new InvalidOperationException("Art Presentation 013 JSON could not be parsed.");
            byId = new Dictionary<string, ArtPresentationBinding013>(StringComparer.Ordinal);
            foreach (ArtPresentationBinding013 binding in cache.bindings)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.stableArtId)) continue;
                if (byId.ContainsKey(binding.stableArtId))
                    throw new InvalidOperationException("Duplicate Art binding: " + binding.stableArtId);
                byId.Add(binding.stableArtId, binding);
            }
            return cache;
        }

        public static bool TryGet(string stableArtId, out ArtPresentationBinding013 binding)
        {
            Load();
            return byId.TryGetValue(stableArtId ?? string.Empty, out binding);
        }

        public static ArtPresentationBinding013 Get(string stableArtId)
        {
            if (!TryGet(stableArtId, out ArtPresentationBinding013 binding))
                throw new KeyNotFoundException("No Art Presentation 013 binding for " + stableArtId);
            return binding;
        }
    }
}
