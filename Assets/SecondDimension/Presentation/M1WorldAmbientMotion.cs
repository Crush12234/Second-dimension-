using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only camera breathing for noncombat locations. It never reads or
    /// writes campaign state and is disabled by the reduced-motion preference.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class M1WorldAmbientMotion : MonoBehaviour
    {
        private RectTransform _rect;
        private Vector2 _restingPosition;
        private bool _captured;
        private bool _reducedMotion;
        private float _phase;

        public void Configure(bool reducedMotion, string identity)
        {
            _reducedMotion = reducedMotion;
            var stablePhase = 0;
            foreach (var character in identity ?? string.Empty)
            {
                stablePhase = (stablePhase * 31 + character) % 997;
            }
            _phase = stablePhase / 997f * Mathf.PI * 2f;
            CaptureRestingPosition();
            ApplyImmediateState();
        }

        private void Awake()
        {
            _rect = transform as RectTransform;
            CaptureRestingPosition();
        }

        private void OnEnable()
        {
            CaptureRestingPosition();
            ApplyImmediateState();
        }

        private void LateUpdate()
        {
            if (_rect == null) return;
            if (_reducedMotion)
            {
                ApplyImmediateState();
                return;
            }

            var time = Time.unscaledTime;
            var drift = new Vector2(
                Mathf.Sin(time * 0.075f + _phase) * 7f,
                Mathf.Cos(time * 0.058f + _phase * 0.7f) * 4f);
            _rect.anchoredPosition = _restingPosition + drift;
            _rect.localScale = Vector3.one * (1.018f + Mathf.Sin(time * 0.043f + _phase) * 0.002f);
        }

        private void CaptureRestingPosition()
        {
            if (_captured) return;
            _rect = transform as RectTransform;
            if (_rect == null) return;
            _restingPosition = _rect.anchoredPosition;
            _captured = true;
        }

        private void ApplyImmediateState()
        {
            if (_rect == null) return;
            _rect.anchoredPosition = _restingPosition;
            _rect.localScale = _reducedMotion ? Vector3.one : Vector3.one * 1.018f;
        }
    }
}
