using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Keeps a full-screen RectTransform inside the platform-reported safe area.
    /// Presentation only: it never affects authoritative game state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _lastSafeArea;
        private int _lastWidth;
        private int _lastHeight;

        private void OnEnable()
        {
            Apply(force: true);
        }

        private void Update()
        {
            Apply(force: false);
        }

        private void Apply(bool force)
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;

            var safeArea = Screen.safeArea;
            if (!force && safeArea == _lastSafeArea && Screen.width == _lastWidth && Screen.height == _lastHeight)
            {
                return;
            }

            _lastSafeArea = safeArea;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
