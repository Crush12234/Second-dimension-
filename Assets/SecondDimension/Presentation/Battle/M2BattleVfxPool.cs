using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Small reusable uGUI effect pool for the vertical slice. Gameplay never reads
    /// these objects; pooling only limits presentation allocation during long rounds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2BattleVfxPool : MonoBehaviour
    {
        private readonly Stack<Image> _images = new Stack<Image>();
        private readonly Stack<Text> _texts = new Stack<Text>();
        private Transform _effectRoot;

        public void Configure(Transform effectRoot) => _effectRoot = effectRoot;

        public Image SpawnPanel(string objectName, Color color)
        {
            if (_effectRoot == null) return null;
            var image = _images.Count > 0 ? _images.Pop() : RuntimeUi.AddPanel(_effectRoot, objectName, color);
            image.gameObject.name = objectName;
            image.transform.SetParent(_effectRoot, false);
            image.color = color;
            image.sprite = null;
            image.raycastTarget = false;
            image.gameObject.SetActive(true);
            return image;
        }

        public Text SpawnText(string objectName, string value, Color color, int fontSize)
        {
            if (_effectRoot == null) return null;
            var text = _texts.Count > 0
                ? _texts.Pop()
                : RuntimeUi.AddText(_effectRoot, objectName, value, fontSize,
                    TextAnchor.MiddleCenter, color, FontStyle.Bold);
            text.gameObject.name = objectName;
            text.transform.SetParent(_effectRoot, false);
            text.text = value;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.gameObject.SetActive(true);
            return text;
        }

        public void Release(Image image, float delay) =>
            StartCoroutine(ReleaseImageAfter(image, Mathf.Max(0f, delay)));

        public void Release(Text text, float delay) =>
            StartCoroutine(ReleaseTextAfter(text, Mathf.Max(0f, delay)));

        private IEnumerator ReleaseImageAfter(Image image, float delay)
        {
            var elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (image == null) yield break;
            image.gameObject.SetActive(false);
            _images.Push(image);
        }

        private IEnumerator ReleaseTextAfter(Text text, float delay)
        {
            var elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (text == null) yield break;
            text.gameObject.SetActive(false);
            _texts.Push(text);
        }
    }
}
