using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Tiny presentation-only standee breathing/parallax motion. It never owns
    /// battlefield position or combat timing, and reduced-motion mode disables it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2BattleIdleMotion : MonoBehaviour
    {
        private RectTransform _rect;
        private Vector2 _home;
        private float _phase;
        private float _amplitude;

        public void Configure(string stableIdentity, bool reducedMotion)
        {
            _rect = transform as RectTransform;
            _home = _rect == null ? Vector2.zero : _rect.anchoredPosition;
            _phase = (StableHash(stableIdentity) % 628u) / 100f;
            _amplitude = reducedMotion ? 0f : 3.5f;
            if (reducedMotion && _rect != null) _rect.localScale = Vector3.one;
        }

        private void Update()
        {
            if (_rect == null || _amplitude <= 0f) return;
            var wave = Mathf.Sin(Time.unscaledTime * 1.15f + _phase);
            _rect.anchoredPosition = _home + Vector2.up * (wave * _amplitude);
            _rect.localScale = Vector3.one * (1f + wave * 0.006f);
        }

        private static uint StableHash(string value)
        {
            var result = 2166136261u;
            value = value ?? string.Empty;
            for (var index = 0; index < value.Length; index++) result = (result ^ value[index]) * 16777619u;
            return result;
        }
    }
}
