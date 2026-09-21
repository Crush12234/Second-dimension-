using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Only native-reviewed HERO_REC_035 remains bound after R146 source QA.
    /// Eleven original candidates are deferred, not certified. PNGs are byte-identical to
    /// the verified original package. These are manually reviewed sprite cells,
    /// not generic alpha-component cropping or an identity/progression authority.
    /// Called only after remasters and existing authored sprite paths fail.
    /// Rights status: PENDING_COMMERCIAL_RIGHTS_REVIEW (see packaged provenance).
    /// </summary>
    public static class HeroRecoveredSource100099
    {
        public const string Root099 = "SecondDimension/Art/Standees/HeroRecovered100099/";
        static readonly Dictionary<string, int[]> Cells099 =
            new Dictionary<string, int[]>(StringComparer.Ordinal)
        {
            { "HERO_REC_035", new[] { 0, 604, 0, 768 } },
        };
        static readonly Dictionary<string, Pair099> Pairs099 =
            new Dictionary<string, Pair099>(StringComparer.Ordinal);

        public sealed class Pair099
        {
            internal Sprite IdleSource, ActionSource;
            public Sprite Idle { get; internal set; }
            public Sprite Action { get; internal set; }
        }

        public static bool ContainsIdentity099(string identity) =>
            identity != null && Cells099.ContainsKey(identity);

        public static bool TryGetSourceRects099(string identity, int width, int height,
            out Rect idle, out Rect action)
        {
            idle = action = Rect.zero;
            if (width != 768 || height != 1024 || !ContainsIdentity099(identity)) return false;
            var cells = Cells099[identity];
            idle = new Rect(cells[0], 0, cells[1], height);
            action = new Rect(cells[2], 0, cells[3], height);
            return true;
        }

        public static bool TryResolve099(string identity, bool action, out Sprite sprite, out string key)
        {
            sprite = null; key = string.Empty;
            if (!ContainsIdentity099(identity)) return false;
            if (!Pairs099.TryGetValue(identity, out var pair) || pair.Idle == null || pair.Action == null)
            {
                var idleTexture = Resources.Load<Texture2D>(Root099 + identity + "_IDLE_099");
                var actionTexture = Resources.Load<Texture2D>(Root099 + identity + "_ACTION_099");
                if (idleTexture == null || actionTexture == null ||
                    !TryGetSourceRects099(identity, idleTexture.width, idleTexture.height,
                        out var idleRect, out var actionRect) ||
                    actionTexture.width != 768 || actionTexture.height != 1024) return false;
#if UNITY_EDITOR
                const bool discardCpuPixels = false;
#else
                const bool discardCpuPixels = true;
#endif
                pair = BuildPair099(idleTexture, actionTexture, idleRect, actionRect, discardCpuPixels);
                if (pair == null) return false;
                Pairs099[identity] = pair;
            }
            sprite = action ? pair.Action : pair.Idle;
            key = Root099 + identity + (action ? "_ACTION_099" : "_IDLE_099");
            return sprite != null;
        }

        public static Pair099 BuildPair099(Texture2D idleTexture, Texture2D actionTexture,
            Rect idleRect, Rect actionRect, bool discardCpuPixels)
        {
            if (idleTexture == null || actionTexture == null || idleTexture == actionTexture ||
                !ValidCell099(idleTexture, idleRect) || !ValidCell099(actionTexture, actionRect)) return null;
            var pair = new Pair099();
            pair.IdleSource = Sprite.Create(idleTexture, idleRect,
                new Vector2(0.5f, 0.035f), 100f, 0u, SpriteMeshType.FullRect);
            pair.ActionSource = Sprite.Create(actionTexture, actionRect,
                new Vector2(0.5f, 0.035f), 100f, 0u, SpriteMeshType.FullRect);
            pair.IdleSource.name = idleTexture.name + "_SOURCE_099";
            pair.ActionSource.name = actionTexture.name + "_SOURCE_099";
            // Cache exact visible bounds for BOTH poses before CPU release.
            pair.Idle = M1SilhouetteFraming091.FrameResourceSprite091(pair.IdleSource);
            pair.Action = M1SilhouetteFraming091.FrameResourceSprite091(pair.ActionSource);
            if (discardCpuPixels)
            {
                idleTexture.Apply(false, true);
                actionTexture.Apply(false, true);
            }
            return pair;
        }

        static bool ValidCell099(Texture2D texture, Rect rect)
        {
            if (!texture.isReadable || texture.width != 768 || texture.height != 1024 ||
                rect.x < 0 || rect.y < 0 || rect.width < 4 || rect.height < 4 ||
                rect.xMax > texture.width || rect.yMax > texture.height ||
                rect.x != Mathf.Floor(rect.x) || rect.y != Mathf.Floor(rect.y) ||
                rect.width != Mathf.Floor(rect.width) || rect.height != Mathf.Floor(rect.height)) return false;
            var pixels = texture.GetPixels32();
            int visible = 0, clear = 0;
            for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
            for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
            {
                if (pixels[y * texture.width + x].a < 16) { clear++; continue; }
                visible++;
                if (x == (int)rect.xMin || x == (int)rect.xMax - 1 ||
                    y == (int)rect.yMin || y == (int)rect.yMax - 1) return false;
            }
            return visible > 16 && clear > rect.width * rect.height / 20;
        }

        public static void RetirePair099(Pair099 pair)
        {
            if (pair == null) return;
            foreach (var source in new[] { pair.IdleSource, pair.ActionSource })
            {
                if (source == null) continue;
                M1SilhouetteFraming091.ReleaseResourceFrame091(source);
                if (Application.isPlaying) UnityEngine.Object.Destroy(source);
                else UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset099()
        {
            foreach (var pair in Pairs099.Values) RetirePair099(pair);
            Pairs099.Clear();
        }
    }
}
