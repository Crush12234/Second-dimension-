using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Recruitment.AutoGeneration010
{
    /// <summary>
    /// Presentation-only compositor for pre-authored transparent layers. Missing
    /// pieces are reported honestly; no network or runtime image generation occurs.
    /// </summary>
    public sealed class RecruitPortraitComposer010 : MonoBehaviour
    {
        [SerializeField] private RectTransform layerRoot;
        [SerializeField] private bool logMissingRequiredAssets = true;
        private readonly List<GameObject> _created = new List<GameObject>();

        public IReadOnlyList<string> Apply(RecruitVisualRecipe010 recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            Clear();
            var missing = new List<string>();
            var root = layerRoot == null ? (RectTransform)transform : layerRoot;
            for (var index = 0; index < recipe.Layers.Count; index++)
            {
                var layer = recipe.Layers[index];
                var sprite = Resources.Load<Sprite>(layer.ResourcePath);
                if (sprite == null)
                {
                    if (layer.Required)
                    {
                        missing.Add(layer.PieceId);
                        if (logMissingRequiredAssets) Debug.LogWarning("AUTOGEN010 missing required portrait piece: " + layer.PieceId);
                    }
                    continue;
                }
                var child = new GameObject(layer.Order.ToString("D2") + "_" + layer.PieceId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child.transform.SetParent(root, false);
                var rect = (RectTransform)child.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var image = child.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                _created.Add(child);
            }
            return missing.AsReadOnly();
        }

        public void Clear()
        {
            for (var index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] == null) continue;
                if (Application.isPlaying) Destroy(_created[index]); else DestroyImmediate(_created[index]);
            }
            _created.Clear();
        }
    }

    public sealed class RecruitBattlePuppetComposer010 : MonoBehaviour
    {
        [SerializeField] private Transform layerRoot;
        [SerializeField] private string sortingLayerName = "Default";
        private readonly List<GameObject> _created = new List<GameObject>();

        public IReadOnlyList<string> Apply(GeneratedRecruitProfile010 profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            Clear();
            var missing = new List<string>();
            var root = layerRoot == null ? transform : layerRoot;
            for (var index = 0; index < profile.VisualRecipe.Layers.Count; index++)
            {
                var layer = profile.VisualRecipe.Layers[index];
                var sprite = Resources.Load<Sprite>(layer.ResourcePath);
                if (sprite == null)
                {
                    if (layer.Required) missing.Add(layer.PieceId);
                    continue;
                }
                var child = new GameObject(layer.Order.ToString("D2") + "_" + layer.PieceId, typeof(SpriteRenderer));
                child.transform.SetParent(root, false);
                var renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = layer.Order;
                _created.Add(child);
            }
            return missing.AsReadOnly();
        }

        public void Clear()
        {
            for (var index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] == null) continue;
                if (Application.isPlaying) Destroy(_created[index]); else DestroyImmediate(_created[index]);
            }
            _created.Clear();
        }
    }
}
