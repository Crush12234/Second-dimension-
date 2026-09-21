using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Campaign023
{
    /// <summary>Fits the existing three-card row below its live page context.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class ExpeditionRouteViewport110 : MonoBehaviour
    {
        RectTransform _row;
        RectTransform _body;
        ScrollRect _scroll;
        HorizontalLayoutGroup _layout;
        ExpeditionCardChoice091 _choice;
        float _minimumRowHeight156E = 1f;
        readonly Vector3[] _corners = new Vector3[4];

        public void Configure110(RectTransform row, ScrollRect scroll, float minimumRowHeight156E = 1f)
        {
            _row = row;
            _body = row == null ? null : row.parent as RectTransform;
            _scroll = scroll;
            _layout = row == null ? null : row.GetComponent<HorizontalLayoutGroup>();
            _choice = row == null ? null : row.GetComponent<ExpeditionCardChoice091>();
            _minimumRowHeight156E = Mathf.Max(1f, minimumRowHeight156E);
        }

        void LateUpdate()
        {
            if (_row == null || _body == null || _scroll == null ||
                _scroll.viewport == null || _scroll.content != _body ||
                (_choice != null && _choice.Phase091 == ExpeditionCardChoice091.ChoicePhase091.Revealing)) return;
            _scroll.viewport.GetWorldCorners(_corners);
            var height = Mathf.Abs((_body.InverseTransformPoint(_corners[1]) -
                _body.InverseTransformPoint(_corners[0])).y);
            if (height < 1f) return;
            var bodyLayout = _body.GetComponent<VerticalLayoutGroup>();
            var reserved = bodyLayout == null ? 16f :
                bodyLayout.padding.vertical + bodyLayout.spacing;
            for (var index = 0; index < _row.GetSiblingIndex(); index++)
            {
                var child = _body.GetChild(index) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy ||
                    child.GetComponent<LayoutElement>()?.ignoreLayout == true) continue;
                reserved += child.rect.height + (bodyLayout == null ? 0f : bodyLayout.spacing);
            }
            // Keep room for the existing hover lift and card tilt at the edges.
            // Never collapse disclosure behind a zero-height mask just to keep
            // the actions on screen. The existing outer ScrollRect can reach the
            // taller card, including its unchanged buy/decline controls.
            var fitted = Mathf.Clamp(height - reserved - 24f,
                _minimumRowHeight156E, Mathf.Max(830f, _minimumRowHeight156E));
            var element = _row.GetComponent<LayoutElement>();
            if (element == null || Mathf.Abs(element.preferredHeight - fitted) < 0.5f) return;
            element.minHeight = _minimumRowHeight156E;
            element.preferredHeight = fitted;
            foreach (RectTransform wrapper in _row)
            {
                var wrapperLayout = wrapper.GetComponent<LayoutElement>();
                if (wrapperLayout == null) continue;
                wrapperLayout.minHeight = _minimumRowHeight156E;
                wrapperLayout.preferredHeight = fitted;
                // The reveal owns horizontal motion after it disables the row
                // layout. A later viewport resize changes only vertical bounds.
                if (_layout != null && !_layout.enabled)
                    wrapper.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, fitted);
            }
            // Only changed geometry needs a layout pass; stable frames do no rebuild.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_body);
        }
    }
}
