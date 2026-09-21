using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Exact-identity, two-pose ORIGINAL art replacement. No race/family remapping,
    /// no source-image edits, no progression or combat authority changes.
    /// </summary>
    public static class HeroRemasterAtlas093
    {
        public const string Root093 = "SecondDimension/Art/Standees/HeroRemaster093/";
        static readonly Dictionary<string, Pair093> Pairs093 = new Dictionary<string, Pair093>(StringComparer.Ordinal);
        static readonly Dictionary<string, int> FrameSplits093 = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "HERO_REC_011", 634 },
            { "HERO_REC_012", 773 },
            { "HERO_REC_013", 755 },
            { "HERO_REC_014", 768 },
            { "HERO_REC_015", 803 },
            { "HERO_REC_016", 679 },
            { "HERO_REC_017", 773 },
            { "HERO_REC_018", 768 },
            { "HERO_REC_019", 752 },
            { "HERO_REC_020", 768 },
            { "HERO_REC_021", 768 },
            { "HERO_REC_022", 746 },
            { "HERO_REC_023", 768 },
            { "HERO_REC_024", 768 },
            { "HERO_REC_025", 697 },
            { "HERO_REC_026", 773 },
            { "HERO_REC_027", 768 },
            { "HERO_REC_028", 768 },
            { "HERO_REC_029", 704 },
            { "HERO_REC_030", 773 },
            { "HERO_REC_031", 754 },
            { "HERO_REC_032", 758 },
            { "HERO_REC_033", 768 },
            { "HERO_REC_034", 768 },
            { "HERO_REC_036", 768 },
            { "HERO_REC_037", 768 },
            { "HERO_REC_038", 781 },
            { "HERO_REC_039", 768 },
            { "HERO_REC_040", 768 },
            { "HERO_REC_041", 768 },
            { "HERO_REC_042", 768 },
            { "HERO_REC_043", 740 },
            { "HERO_REC_044", 805 },
            { "HERO_REC_045", 768 },
            { "HERO_REC_046", 741 },
            { "HERO_REC_047", 710 },
            { "HERO_REC_048", 768 },
            { "HERO_REC_049", 724 },
            { "HERO_REC_050", 768 },
            { "HERO_REC_051", 690 },
            { "HERO_REC_052", 768 },
            { "HERO_REC_053", 735 },
            { "HERO_REC_054", 768 },
            { "HERO_REC_055", 768 },
            { "HERO_REC_056", 715 },
            { "HERO_REC_057", 757 },
            { "HERO_REC_058", 768 },
            { "HERO_REC_059", 768 },
            { "HERO_REC_060", 768 },
            { "HERO_REC_061", 772 },
            { "HERO_REC_062", 718 },
            { "HERO_REC_063", 768 },
            { "HERO_REC_064", 768 },
            { "HERO_REC_065", 768 },
            { "HERO_REC_066", 764 },
            { "HERO_REC_067", 772 },
            { "HERO_REC_068", 771 },
            { "HERO_REC_069", 725 },
            { "HERO_REC_070", 768 },
            { "HERO_REC_071", 768 },
            { "HERO_REC_072", 768 },
            { "HERO_REC_073", 811 },
            { "HERO_REC_074", 752 },
            { "HERO_REC_075", 722 },
            { "HERO_REC_076", 768 },
            { "HERO_REC_077", 768 },
            { "HERO_REC_078", 768 },
            { "HERO_REC_079", 676 },
            { "HERO_REC_080", 773 },
            { "HERO_REC_081", 768 },
            { "HERO_REC_082", 768 },
            { "HERO_REC_083", 778 },
            { "HERO_REC_084", 768 },
            { "HERO_REC_085", 768 },
            { "HERO_REC_086", 768 },
            { "HERO_REC_087", 768 },
            { "HERO_REC_088", 768 },
            { "HERO_REC_089", 768 },
            { "HERO_REC_090", 768 },
            { "HERO_REC_091", 774 },
            { "HERO_REC_092", 768 },
            { "HERO_REC_093", 768 },
            { "HERO_REC_094", 772 },
            { "HERO_REC_095", 768 },
            { "HERO_REC_096", 838 },
            { "HERO_REC_097", 830 },
            { "HERO_REC_098", 768 },
            { "HERO_REC_099", 768 },
            { "HERO_REC_100", 786 },
            { "HERO_REC_101", 768 },
            { "HERO_REC_102", 768 },
            { "HERO_REC_103", 773 },
            { "HERO_REC_104", 768 },
            { "HERO_REC_105", 700 },
            { "HERO_REC_106", 692 },
            { "HERO_REC_107", 773 },
            { "HERO_REC_108", 768 },
            { "HERO_REC_109", 827 },
            { "HERO_REC_110", 768 },
            { "HERO_REC_169", 664 },
            { "HERO_REC_170", 672 },
            { "HERO_REC_171", 712 },
            { "HERO_REC_172", 647 },
            { "HERO_REC_173", 570 },
            { "HERO_REC_174", 705 },
            { "HERO_REC_175", 679 },
            { "HERO_REC_176", 622 },
            { "HERO_REC_177", 708 },
            { "HERO_REC_178", 705 },
            { "HERO_REC_179", 670 },
            { "HERO_REC_180", 670 },
            { "HERO_REC_181", 768 },
            { "HERO_REC_183", 752 },
            { "HERO_REC_185", 764 },
            { "HERO_REC_186", 768 },
            { "HERO_REC_187", 768 },
            { "HERO_REC_188", 700 },
            { "HERO_REC_189", 780 },
            { "HERO_REC_191", 768 },
            { "HERO_REC_192", 768 },
            { "HERO_REC_193", 768 },
            { "HERO_REC_195", 768 },
            { "HERO_REC_196", 768 },
            { "HERO_REC_197", 768 },
            { "HERO_REC_198", 779 },
            { "HERO_REC_219", 772 },
            { "HERO_REC_220", 768 },
            { "HERO_REC_221", 768 },
            { "HERO_REC_222", 768 },
            { "HERO_REC_223", 768 },
            { "HERO_REC_225", 764 },
            { "HERO_REC_228", 768 },
            { "HERO_REC_232", 768 },
            { "HERO_REC_233", 768 },
            { "HERO_REC_234", 768 },
            { "HERO_REC_236", 768 },
            { "HERO_REC_237", 750 },
            { "HERO_REC_238", 812 },
            { "HERO_REC_239", 768 },
            { "HERO_REC_240", 768 },
            { "HERO_REC_241", 768 },
            { "HERO_REC_245", 738 },
            { "HERO_REC_248", 768 },
            { "HERO_REC_249", 768 },
            { "HERO_REC_250", 768 },
            { "HERO_REC_253", 848 },
            { "HERO_REC_256", 768 },
            { "HERO_REC_257", 790 },
            { "HERO_REC_258", 768 },
            { "HERO_REC_259", 768 },
            { "HERO_REC_260", 768 },
            { "HERO_REC_261", 768 },
            { "HERO_REC_262", 773 },
            { "HERO_REC_263", 768 },
            { "HERO_REC_265", 756 },
            { "HERO_REC_266", 768 },
            { "HERO_REC_267", 768 },
            { "HERO_REC_268", 768 },
            { "HERO_REC_269", 768 },
            { "HERO_REC_270", 720 },
            { "HERO_REC_271", 768 },
            { "HERO_REC_272", 768 },
            { "HERO_REC_274", 768 },
            { "HERO_REC_275", 768 },
            { "HERO_REC_276", 768 },
            { "HERO_REC_277", 787 },
            { "HERO_REC_278", 768 },
            { "HERO_REC_279", 768 },
            { "HERO_REC_280", 774 },
            { "HERO_REC_281", 768 },
            { "HERO_REC_282", 768 },
            { "HERO_REC_284", 775 },
            { "HERO_REC_285", 768 },
            { "HERO_REC_286", 768 },
            { "HERO_REC_288", 768 },
            { "HERO_REC_289", 786 },
            { "HERO_REC_290", 768 },
            { "HERO_REC_292", 768 },
            { "HERO_REC_293", 768 },
            { "HERO_REC_294", 768 },
            { "HERO_REC_295", 768 },
            { "HERO_REC_296", 768 },
            { "HERO_REC_297", 768 },
            { "HERO_REC_298", 768 },
            { "HERO_REC_299", 768 },
            { "HERO_REC_300", 768 },
        };

        public sealed class Pair093
        {
            internal Sprite IdleSource, ActionSource;
            public Sprite Idle { get; internal set; }
            public Sprite Action { get; internal set; }
        }

        public static bool ContainsIdentity093(string identity) =>
            identity != null && FrameSplits093.ContainsKey(identity);

        // These exact original pairs have native-reviewed crouching/raised-weapon
        // poses that inflate the body under independent equal-height fitting.
        // Retain their own idle pixel scale, including the complete weapon.
        // No family/global resize; a cached action reference is the authority.
        static readonly string[] IdlePixelScaleIdentities100 =
            { "HERO_REC_011", "HERO_REC_033", "HERO_REC_041", "HERO_REC_105",
                "HERO_REC_195", "HERO_REC_221", "HERO_REC_257" };

        public static bool UsesIdlePixelScale100(string identity) =>
            Array.IndexOf(IdlePixelScaleIdentities100, identity) >= 0;

        public static float CalibrateBattlePixelScale099(Sprite sprite, float requestedScale,
            float visibleHeight, float maximumIdleWidth)
        {
            if (sprite == null) return requestedScale;
            foreach (var identity in IdlePixelScaleIdentities100)
            {
                if (!Pairs093.TryGetValue(identity, out var pair) || pair.Idle == null ||
                    !ReferenceEquals(sprite, pair.Action)) continue;
                var idleVisible = M1SilhouetteFraming091.VisibleRect091(pair.Idle);
                if (idleVisible.height <= 1f || pair.Idle.rect.width <= 1f) return requestedScale;
                var idleScale = Mathf.Min(visibleHeight / idleVisible.height,
                    maximumIdleWidth / pair.Idle.rect.width);
                return Mathf.Min(requestedScale, idleScale);
            }
            return requestedScale;
        }

        // Authored frame boundaries follow each atlas's inspected transparent
        // gutter. The generated poses are NOT equal-width cells.
        public static bool TryGetFrameRects093(string identity, int width, int height,
            out Rect idle, out Rect action)
        {
            idle = action = Rect.zero;
            if (!ContainsIdentity093(identity) || width != 1536 || height != 1024) return false;
            var split = FrameSplits093[identity];
            idle = new Rect(0, 0, split, height);
            action = new Rect(split, 0, width - split, height);
            return true;
        }

        public static bool TryResolve093(string identity, bool action, out Sprite sprite, out string key)
        {
            sprite = null; key = string.Empty;
            if (!ContainsIdentity093(identity)) return false;
            var path = Root093 + identity + "_PAIR_093";
            if (!Pairs093.TryGetValue(identity, out var pair) || pair.Idle == null || pair.Action == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                // Exactly these reviewed atlases are imported readable and
                // uncompressed. Both cells must be alpha-framed before Windows
                // releases that CPU copy; no GPU readback or per-frame scanning.
                if (texture == null || !texture.isReadable ||
                    !TryGetFrameRects093(identity, texture.width, texture.height, out var idle, out var actionRect))
                    return false;
#if UNITY_EDITOR
                const bool discardCpuPixels = false;
#else
                const bool discardCpuPixels = true;
#endif
                pair = BuildPair093(texture, idle, actionRect, discardCpuPixels);
                if (pair == null) return false;
                Pairs093[identity] = pair;
            }
            sprite = action ? pair.Action : pair.Idle;
            key = path + (action ? "#ACTION" : "#IDLE");
            return sprite != null;
        }

        public static Pair093 BuildPair093(Texture2D texture, Rect idleRect, Rect actionRect,
            bool discardCpuPixels)
        {
            if (texture == null || !texture.isReadable || texture.width < 8 || texture.height < 8 ||
                !IsFrameRect093(idleRect, texture) || !IsFrameRect093(actionRect, texture) ||
                idleRect.Overlaps(actionRect))
                return null;
            var pixels = texture.GetPixels32();
            if (!IsCutoutCell093(pixels, texture.width, idleRect) ||
                !IsCutoutCell093(pixels, texture.width, actionRect))
                return null;
            var pair = new Pair093();
            pair.IdleSource = Sprite.Create(texture, idleRect,
                new Vector2(0.5f, 0.035f), 100f, 0u, SpriteMeshType.FullRect);
            pair.ActionSource = Sprite.Create(texture, actionRect,
                new Vector2(0.5f, 0.035f), 100f, 0u, SpriteMeshType.FullRect);
            pair.IdleSource.name = texture.name + "_IDLE_093";
            pair.ActionSource.name = texture.name + "_ACTION_093";
            pair.Idle = M1SilhouetteFraming091.FrameResourceSprite091(pair.IdleSource);
            pair.Action = M1SilhouetteFraming091.FrameResourceSprite091(pair.ActionSource);
            if (discardCpuPixels) texture.Apply(false, true);
            return pair;
        }

        static bool IsFrameRect093(Rect rect, Texture2D texture) =>
            rect.width >= 4 && rect.height >= 4 && rect.xMin >= 0 && rect.yMin >= 0 &&
            rect.xMax <= texture.width && rect.yMax <= texture.height &&
            rect.x == Mathf.Floor(rect.x) && rect.y == Mathf.Floor(rect.y) &&
            rect.width == Mathf.Floor(rect.width) && rect.height == Mathf.Floor(rect.height);

        static bool IsCutoutCell093(Color32[] pixels, int width, Rect rect)
        {
            var fromX = (int)rect.xMin;
            var throughX = (int)rect.xMax;
            var fromY = (int)rect.yMin;
            var throughY = (int)rect.yMax;
            var visible = 0;
            var clear = 0;
            var edgeVisible = false;
            for (var y = fromY; y < throughY; y++)
            for (var x = fromX; x < throughX; x++)
            {
                var alpha = pixels[y * width + x].a;
                if (alpha >= 16)
                {
                    visible++;
                    // A pose crossing a cell boundary would be clipped or leak
                    // into the other pose. Treat that as unfinished art, not a
                    // license to crop away meaningful anatomy/equipment.
                    if (x == fromX || x == throughX - 1 || y == fromY || y == throughY - 1) edgeVisible = true;
                }
                else clear++;
            }
            return visible > 16 && clear > rect.width * rect.height / 20 && !edgeVisible;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset093()
        {
            // The shared silhouette cache owns derived frames; this helper owns
            // only the two raw cell sprites. Resources retains the source texture.
            foreach (var pair in Pairs093.Values) RetireSources093(pair);
            Pairs093.Clear();
        }

        public static void RetireSources093(Pair093 pair)
        {
            if (pair == null) return;
            foreach (var sprite in new[] { pair.IdleSource, pair.ActionSource })
            {
                if (sprite == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(sprite);
                else UnityEngine.Object.DestroyImmediate(sprite);
            }
        }
    }
}
