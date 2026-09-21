using System;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>Three authored chest poses; this component never creates, equips, or claims rewards.</summary>
    [DisallowMultipleComponent]
    public sealed class BoardChestReveal092 : MonoBehaviour
    {
        public const string AtlasResource092 = "SecondDimension/Art/Rewards/Chest092/CHEST_OPENING_ATLAS_092";
        public const float OpeningDuration092 = 1.25f;
        public const float LootRevealStart092 = 0.94f;
        private static readonly Sprite[] Frames092 = new Sprite[3];
        private static Sprite _glowSprite092;
        private Image _closed;
        private Image _half;
        private Image _open;
        private Image _glow;
        private Image _loot;
        private Image[] _sparkles;
        private CanvasGroup[] _rewardCopy;
        private Text _heading;
        private string _openedHeading;
        private Action _settled;
        private Func<bool> _diceReady132;
        private float _elapsed;
        private float _leadIn;
        private bool _configured;
        private bool _playing;
        private bool _captureHeld092;
        private bool _skipCaptureDelta092;
        private bool _skipInitialDelta100;
        public int FrameIndex092 { get; private set; }
        public bool IsLootRevealed092 { get; private set; }
        public bool IsPlaying092 => _playing;

        // Release smoke only: latch the naturally reached pose while PNG encoding
        // blocks the render thread. This never seeks a pose or pauses game time.
        internal void HoldCaptureClock092(bool held)
        {
            _captureHeld092 = held;
            if (!held) _skipCaptureDelta092 = true;
        }

        public static Sprite Frame092(int index)
        {
            index = Mathf.Clamp(index, 0, 2);
            if (Frames092[index] != null) return Frames092[index];
            var texture = Resources.Load<Texture2D>(AtlasResource092);
            if (texture == null || texture.width < 3 || texture.height < 1) return null;
            var left = Mathf.RoundToInt(index * texture.width / 3f);
            var right = Mathf.RoundToInt((index + 1) * texture.width / 3f);
            Frames092[index] = Sprite.Create(texture, new Rect(left, 0, right - left, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Frames092[index].name = "Chest " + new[] { "Closed", "Half Open", "Open" }[index] + " 092";
            return Frames092[index];
        }

        public void Configure092(Image art, Sprite earnedItemArt, CanvasGroup[] rewardCopy,
            Text heading, string openedHeading, bool animate, float leadIn, Action settled,
            Func<bool> diceReady132 = null)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));
            _closed = art;
            _rewardCopy = rewardCopy ?? Array.Empty<CanvasGroup>();
            _heading = heading;
            _openedHeading = openedHeading ?? "CHEST OPENED  •  LOOT REVEALED";
            _leadIn = Mathf.Max(0f, leadIn);
            _settled = settled;
            _diceReady132 = diceReady132;
            _elapsed = 0f;
            _skipInitialDelta100 = true;
            _closed.sprite = Frame092(0);
            _closed.type = Image.Type.Simple;
            _closed.preserveAspect = true;
            _closed.raycastTarget = false;
            var aspect = art.GetComponent<AspectRatioFitter>();
            if (aspect == null) aspect = art.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = _closed.sprite == null ? 1f :
                _closed.sprite.rect.width / _closed.sprite.rect.height;
            _glow = AddImage092(art.transform, "Chest Confined Glow 092", GlowSprite092());
            SetRect092(_glow.rectTransform, new Vector2(0.21f, 0.24f), new Vector2(0.80f, 0.80f));
            _glow.transform.SetAsFirstSibling();
            _half = AddImage092(art.transform, "Chest Half Open Pose 092", Frame092(1));
            _open = AddImage092(art.transform, "Chest Open Pose 092", Frame092(2));
            if (earnedItemArt != null)
            {
                _loot = AddImage092(art.transform, "Chest Earned Item Art 092", earnedItemArt);
                SetRect092(_loot.rectTransform, new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.78f));
            }
            _sparkles = new Image[10];
            for (var index = 0; index < _sparkles.Length; index++)
            {
                _sparkles[index] = AddImage092(art.transform, "Chest Sparkle " + index + " 092", GlowSprite092());
                var rect = _sparkles[index].rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.52f);
                rect.sizeDelta = Vector2.one * (index % 3 == 0 ? 10f : 6f);
            }
            _configured = true;
            _playing = animate && Application.isPlaying && _closed.sprite != null && _half.sprite != null && _open.sprite != null;
            if (_playing) Present092(0f);
            else Settle092();
        }

        private void Update()
            => AdvancePresentationClock100(Time.unscaledDeltaTime);

        // The first delta can include synchronous resource/UI creation before
        // this chest existed. Later stalls must not skip whole opening poses.
        // Only the cosmetic clock is capped; timeScale and reward authorities
        // are untouched. Under 30 FPS the reveal takes longer instead of jumping.
        private void AdvancePresentationClock100(float delta)
        {
            if (!_playing || _captureHeld092 || (_diceReady132 != null && !_diceReady132())) return;
            if (_skipInitialDelta100 || _skipCaptureDelta092)
            {
                _skipInitialDelta100 = false;
                _skipCaptureDelta092 = false;
                return;
            }
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta <= 0f) return;
            _elapsed += Mathf.Min(delta, 1f / 30f);
            var time = Mathf.Max(0f, _elapsed - _leadIn);
            Present092(time);
            if (time >= OpeningDuration092) Settle092();
        }

        private void Present092(float time)
        {
            var half = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.40f, time));
            var open = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.86f, time));
            var loot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LootRevealStart092, OpeningDuration092, time));
            SetAlpha092(_closed, 1f - half);
            SetAlpha092(_half, half * (1f - open));
            SetAlpha092(_open, open);
            FrameIndex092 = time < 0.40f ? 0 : time < 0.86f ? 1 : 2;
            IsLootRevealed092 = time >= LootRevealStart092;
            if (_heading != null) _heading.text = time < 0.22f
                ? "TREASURE CHEST  •  SEALED"
                : time < 0.86f ? "THE CHEST IS OPENING…" : _openedHeading;
            foreach (var group in _rewardCopy)
                if (group != null) group.alpha = loot;
            if (_loot != null)
            {
                SetAlpha092(_loot, loot);
                _loot.rectTransform.anchoredPosition = Vector2.up * Mathf.Lerp(-16f, 9f, loot);
                _loot.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f, loot);
            }
            if (_glow != null) _glow.color = new Color(1f, 0.72f, 0.18f, half * 0.24f);
            if (_sparkles == null) return;
            var burst = Mathf.Clamp01((time - 0.64f) / 0.60f);
            for (var index = 0; index < _sparkles.Length; index++)
            {
                var angle = (20f + index * 15f) * Mathf.Deg2Rad;
                var distance = Mathf.Lerp(12f, 62f + index % 3 * 9f, burst);
                _sparkles[index].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                _sparkles[index].color = new Color(1f, 0.85f, 0.37f,
                    time < 0.64f ? 0f : Mathf.Sin(burst * Mathf.PI) * 0.7f);
            }
        }

        private void Settle092()
        {
            if (!_configured) return;
            Present092(OpeningDuration092);
            _playing = false;
            FrameIndex092 = 2;
            IsLootRevealed092 = true;
            var settled = _settled;
            _settled = null;
            settled?.Invoke();
        }

        private void OnDisable() => Settle092();

        private static Image AddImage092(Transform parent, string name, Sprite sprite)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            SetRect092(image.rectTransform, Vector2.zero, Vector2.one);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void SetRect092(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void SetAlpha092(Image image, float alpha)
        {
            if (image != null) image.color = new Color(1f, 1f, 1f, alpha);
        }

        private static Sprite GlowSprite092()
        {
            if (_glowSprite092 != null) return _glowSprite092;
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = "Chest Soft Sparkle 092", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[1024];
            for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
            {
                var radius = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - radius), 2f));
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            _glowSprite092 = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
            return _glowSprite092;
        }
    }
}
