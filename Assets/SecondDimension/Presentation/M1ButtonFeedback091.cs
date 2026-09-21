using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only feedback. Button still owns selection, interactability
    /// and its immediate onClick; this component never queues or consumes input.
    /// The root transform is deliberately untouched so card flips, layout and
    /// facility-card motion retain sole ownership of their transforms.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class M1ButtonFeedback091 : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        private Button _button;
        private Image _wash;
        private RectTransform _label;
        private Vector3 _labelHomeScale = Vector3.one;
        private bool _hovered;
        private bool _focused;
        private bool _pressed;
        private int _pressedPointer;
        private float _pulse;
        private float _washAlpha;

        public void Configure091(Button button, Sprite roundedSprite)
        {
            _button = button;
            var label = FindLabel091(button);
            if (_label != label)
            {
                RestoreLabel091();
                _label = label;
                _labelHomeScale = label != null ? label.localScale : Vector3.one;
            }

            if (_wash == null)
            {
                var washObject = new GameObject(
                    "Button Touch Light 091", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                washObject.transform.SetParent(button.transform, false);
                _wash = washObject.GetComponent<Image>();
                _wash.raycastTarget = false;
                _wash.color = new Color(0.78f, 0.90f, 1f, 0f);
                washObject.GetComponent<LayoutElement>().ignoreLayout = true;
                var rect = _wash.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(1f, 1f);
                rect.offsetMax = new Vector2(-1f, -1f);
            }
            _wash.sprite = roundedSprite;
            _wash.type = Image.Type.Sliced;
        }

        private bool CanRespond091 => _button != null && _button.IsActive() && _button.IsInteractable();

        internal static RectTransform FindLabel091(Button button)
        {
            if (button == null) return null;
            for (var index = 0; index < button.transform.childCount; index++)
            {
                var child = button.transform.GetChild(index);
                if ((child.name == "Label" || child.name.StartsWith("Label [", System.StringComparison.Ordinal)) &&
                    child.GetComponent<Text>() != null)
                    return child as RectTransform;
            }
            return null;
        }

        private void Update() => Tick091(Time.unscaledDeltaTime);

        private void Tick091(float dt)
        {
            if (!CanRespond091)
            {
                ResetVisuals091();
                return;
            }

            _pulse = Mathf.MoveTowards(_pulse, 0f, dt / 0.24f);
            var targetAlpha = _pressed ? 0.105f : (_hovered || _focused ? 0.045f : 0f);
            targetAlpha += Mathf.Sin(_pulse * Mathf.PI) * 0.075f;
            var response = 1f - Mathf.Exp(-(_pressed ? 30f : 16f) * dt);
            _washAlpha = Mathf.Lerp(_washAlpha, targetAlpha, response);
            if (_wash != null)
            {
                var color = _wash.color;
                if (Mathf.Abs(color.a - _washAlpha) > 0.0005f)
                {
                    color.a = _washAlpha;
                    _wash.color = color;
                }
            }

            if (_label != null)
            {
                var scale = _pressed ? 0.972f : 1f + Mathf.Sin(_pulse * Mathf.PI) * 0.014f;
                var target = _labelHomeScale * scale;
                if ((_label.localScale - target).sqrMagnitude > 0.000001f)
                    _label.localScale = Vector3.Lerp(_label.localScale, target, response);
                else if (_label.localScale != _labelHomeScale && !_pressed && _pulse <= 0f)
                    _label.localScale = _labelHomeScale;
            }
        }

        private void OnDisable() => ResetVisuals091();

        private void ResetVisuals091()
        {
            _hovered = false;
            _focused = false;
            _pressed = false;
            _pulse = 0f;
            _washAlpha = 0f;
            RestoreLabel091();
            if (_wash != null && _wash.color.a != 0f)
            {
                var color = _wash.color;
                color.a = 0f;
                _wash.color = color;
            }
        }

        private void RestoreLabel091()
        {
            if (_label != null && _label.localScale != _labelHomeScale)
                _label.localScale = _labelHomeScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanRespond091) _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanRespond091 || eventData.button != PointerEventData.InputButton.Left) return;
            _pressed = true;
            _pressedPointer = eventData.pointerId;
            _pulse = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed || eventData.pointerId != _pressedPointer) return;
            _pressed = false;
            if (CanRespond091) _pulse = 1f;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (CanRespond091) _focused = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            _pressed = false;
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (CanRespond091) _pulse = 1f;
        }
    }
}
