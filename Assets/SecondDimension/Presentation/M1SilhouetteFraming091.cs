using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>Frames existing cutout pixels; never recolors, replaces or redraws an identity.</summary>
    public static class M1SilhouetteFraming091
    {
        const byte AlphaThreshold091 = 16;
        const float PaddingFraction091 = 0.035f;
        sealed class CachedFrame091
        {
            public Sprite Source;
            public Sprite Framed;
        }
        static readonly Dictionary<int, CachedFrame091> Frames091 = new Dictionary<int, CachedFrame091>();
        sealed class CachedVisible091
        {
            public Sprite Sprite;
            public Rect Rect;
        }
        static readonly Dictionary<int, CachedVisible091> VisibleBounds091 = new Dictionary<int, CachedVisible091>();

        // Resources sprites live with their derived frames. Raw-file enemies do
        // not enter this cache: their one framed sprite remains owned by its lease.
        public static Sprite FrameResourceSprite091(Sprite source)
        {
            if (source == null || source.texture == null || source.packed ||
                source.name.EndsWith("_SILHOUETTE_FIT_091", StringComparison.Ordinal) ||
                source.name.EndsWith("_BOUNDS_FIT_089", StringComparison.Ordinal)) return source;
            var key = source.GetInstanceID();
            if (Frames091.TryGetValue(key, out var cached) && cached.Source == source && cached.Framed != null)
                return cached.Framed;
            var visible = VisibleRect091(source);
            var rect = PaddedRect091(visible, source.rect);
            var framed = source;
            if (rect.width > 1f && rect.height > 1f && rect != source.rect)
            {
                framed = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.035f),
                    source.pixelsPerUnit, 0u, SpriteMeshType.FullRect);
                framed.name = source.name + "_SILHOUETTE_FIT_091";
            }
            Frames091[key] = new CachedFrame091 { Source = source, Framed = framed };
            // A derived FullRect no longer carries the imported alpha mesh. Retain
            // the source silhouette coordinates for body height and foot fitting.
            VisibleBounds091[framed.GetInstanceID()] = new CachedVisible091 { Sprite = framed, Rect = visible };
            return framed;
        }

        public static Rect VisibleRect091(Sprite source)
        {
            if (source == null || source.texture == null) return Rect.zero;
            var key = source.GetInstanceID();
            if (VisibleBounds091.TryGetValue(key, out var cached) && cached.Sprite == source)
                return cached.Rect;
            var visible = MeasureVisibleRect091(source);
            VisibleBounds091[key] = new CachedVisible091 { Sprite = source, Rect = visible };
            return visible;
        }

        // For short-lived Resources providers that own their raw source sprites.
        // Retire only this exact source/derived pair; unrelated hero frames stay cached.
        public static void ReleaseResourceFrame091(Sprite source)
        {
            if (source == null) return;
            var key = source.GetInstanceID();
            if (Frames091.TryGetValue(key, out var cached) && cached.Source == source)
            {
                Frames091.Remove(key);
                if (cached.Framed != null)
                {
                    VisibleBounds091.Remove(cached.Framed.GetInstanceID());
                    if (cached.Framed != source)
                    {
                        if (Application.isPlaying) UnityEngine.Object.Destroy(cached.Framed);
                        else UnityEngine.Object.DestroyImmediate(cached.Framed);
                    }
                }
            }
            VisibleBounds091.Remove(key);
        }

        static Rect MeasureVisibleRect091(Sprite source)
        {
            if (source.texture.isReadable && !source.packed)
                return VisibleRect091(source.texture, source.rect);
            // Imported Tight sprites retain their silhouette mesh even when the
            // CPU texture is discarded. No GPU readback or import mutation needed.
            var vertices = source.vertices;
            if (vertices == null || vertices.Length < 3) return source.rect;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var vertex in vertices)
            {
                var pixel = vertex * source.pixelsPerUnit + source.pivot + source.rect.position;
                min = Vector2.Min(min, pixel);
                max = Vector2.Max(max, pixel);
            }
            return ClampRect091(Rect.MinMaxRect(min.x, min.y, max.x, max.y), source.rect);
        }

        // Called exactly once after raw PNG decode, before the loader discards CPU pixels.
        public static Rect VisibleRect091(Texture2D texture, Rect sourceRect)
        {
            if (texture == null || !texture.isReadable) return sourceRect;
            var pixels = texture.GetPixels32();
            var left = Mathf.Clamp(Mathf.FloorToInt(sourceRect.xMin), 0, texture.width - 1);
            var right = Mathf.Clamp(Mathf.CeilToInt(sourceRect.xMax), left + 1, texture.width);
            var bottom = Mathf.Clamp(Mathf.FloorToInt(sourceRect.yMin), 0, texture.height - 1);
            var top = Mathf.Clamp(Mathf.CeilToInt(sourceRect.yMax), bottom + 1, texture.height);
            var minX = right;
            var minY = top;
            var maxX = -1;
            var maxY = -1;
            for (var y = bottom; y < top; y++)
            for (var x = left; x < right; x++)
            {
                if (pixels[y * texture.width + x].a < AlphaThreshold091) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
            // An empty cutout must not produce an invalid sprite. A fully opaque
            // portrait naturally keeps its full rectangle rather than fake cropping.
            return maxX < minX || maxY < minY ? sourceRect :
                Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
        }

        public static Rect PaddedRect091(Rect visible, Rect source)
        {
            if (visible.width <= 1f || visible.height <= 1f) return source;
            var pad = Mathf.Max(2f, Mathf.Ceil(Mathf.Max(visible.width, visible.height) * PaddingFraction091));
            return ClampRect091(Rect.MinMaxRect(Mathf.Floor(visible.xMin - pad),
                Mathf.Floor(visible.yMin - pad), Mathf.Ceil(visible.xMax + pad),
                Mathf.Ceil(visible.yMax + pad)), source);
        }

        static Rect ClampRect091(Rect rect, Rect source) => Rect.MinMaxRect(
            Mathf.Clamp(rect.xMin, source.xMin, source.xMax),
            Mathf.Clamp(rect.yMin, source.yMin, source.yMax),
            Mathf.Clamp(rect.xMax, source.xMin, source.xMax),
            Mathf.Clamp(rect.yMax, source.yMin, source.yMax));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset091()
        {
            foreach (var frame in Frames091.Values)
                if (frame.Framed != null && frame.Framed != frame.Source)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(frame.Framed);
                    else UnityEngine.Object.DestroyImmediate(frame.Framed);
                }
            Frames091.Clear();
            VisibleBounds091.Clear();
        }
    }
}
