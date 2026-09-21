using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    /// <summary>Presentation-only bridge from stable VFX IDs to the existing pooled uGUI VFX layer.</summary>
    public static class M2BattleVfxSpriteAdapter013
    {
        public static Image Spawn(
            M2BattleVfxPool pool,
            string vfxAssetId,
            Vector2 anchor,
            Vector2 normalizedSize,
            float duration,
            float rotationDegrees = 0f)
        {
            if (pool == null || !BattleArtVisualLookup013.TryLoadVfxSprite(vfxAssetId, out Sprite sprite))
                return null;
            Image image = pool.SpawnPanel("Art013 " + vfxAssetId, Color.white);
            if (image == null) return null;
            image.sprite = sprite;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            Vector2 half = normalizedSize * 0.5f;
            rect.anchorMin = anchor - half;
            rect.anchorMax = anchor + half;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            pool.Release(image, Mathf.Max(0.01f, duration));
            return image;
        }
    }
}
