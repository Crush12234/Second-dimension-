using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    internal sealed class UnionAddTouch165 : MonoBehaviour
    {
        private void LateUpdate()
        {
            var rect = (RectTransform)transform;
            var parent = (RectTransform)rect.parent;
            var scale = Mathf.Max(.01f, GetComponentInParent<Canvas>()?.scaleFactor ?? 1f);
            rect.anchorMin = rect.anchorMax = new Vector2(.79f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(Mathf.Max(parent.rect.width * .12f, 88f / scale),
                Mathf.Max(parent.rect.height * .79f, 44f / scale));
            var label = GetComponentInChildren<Text>();
            if (label == null) return;
            label.resizeTextForBestFit = false;
            label.fontSize = Mathf.CeilToInt(12f / scale);
            label.rectTransform.offsetMin = new Vector2(2f / scale, 2f / scale);
            label.rectTransform.offsetMax = new Vector2(-2f / scale, -2f / scale);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
