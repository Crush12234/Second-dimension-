using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Campaign023
{
    /// <summary>
    /// Presentation-only motion for the three Expedition route cards.  The
    /// authoritative card choice remains on the existing Button/command path;
    /// this component only gives the physical cards a readable entrance,
    /// hover lift, and press response.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    internal sealed class ExpeditionRouteCardMotion089 : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        const float EntranceDuration = 0.30f;
        const float ShuffleDuration089 = 0.34f;
        const float DealDuration089 = 0.32f;
        public const float ShuffleAndDealLead089 =
            ShuffleDuration089 + DealDuration089 + 0.10f;

        RectTransform _rect;
        float _bornAt;
        float _stagger;
        float _phase;
        bool _hovered;
        bool _pressed;
        bool _motionEnabled;
        bool _revealed;
        bool _homeCaptured;
        bool _dealSettled;
        int _cardIndex;
        Vector3 _homeLocalPosition;

        public void Configure089(int cardIndex, string stableCardId,
            bool motionEnabled)
        {
            _rect = GetComponent<RectTransform>();
            _bornAt = Time.unscaledTime;
            _cardIndex = Mathf.Clamp(cardIndex, 0, 2);
            _stagger = _cardIndex * 0.075f;
            _phase = Mathf.Abs((stableCardId ?? string.Empty).GetHashCode() % 997) /
                     997f * Mathf.PI * 2f;
            _motionEnabled = motionEnabled;
            _revealed = !motionEnabled;
            _homeCaptured = false;
            _dealSettled = !motionEnabled;
            if (!_motionEnabled)
            {
                _rect.localScale = Vector3.one;
                _rect.localRotation = Quaternion.identity;
            }
        }

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        void OnDisable()
        {
            if (_rect == null) return;
            if (_homeCaptured) _rect.localPosition = _homeLocalPosition;
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            if (!_motionEnabled || _rect == null) return;

            if (!_homeCaptured)
            {
                _homeLocalPosition = _rect.localPosition;
                _homeCaptured = true;
            }

            var age = Time.unscaledTime - _bornAt;
            if (!_revealed && !_dealSettled)
            {
                var parent = _rect.parent as RectTransform;
                var spread = Mathf.Max(170f,
                    (parent?.rect.width ?? 600f) * 0.285f);
                var centerOffset = (1 - _cardIndex) * spread;
                if (age < ShuffleDuration089)
                {
                    var shuffle = Mathf.Clamp01(age / ShuffleDuration089);
                    var cut = Mathf.Sin(shuffle * Mathf.PI * 4f +
                                       _cardIndex * 2.05f);
                    _rect.localPosition = _homeLocalPosition + new Vector3(
                        centerOffset + cut * 18f,
                        Mathf.Abs(cut) * 8f,
                        0f);
                    _rect.localScale = Vector3.one *
                        Mathf.Lerp(0.82f, 0.88f, shuffle);
                    _rect.localRotation = Quaternion.Euler(
                        0f, 0f, (_cardIndex - 1) * 7f + cut * 3.5f);
                    return;
                }

                var dealt = Mathf.Clamp01((age - ShuffleDuration089 - _stagger) /
                                          DealDuration089);
                var easedDeal = 1f - Mathf.Pow(1f - dealt, 3f);
                _rect.localPosition = _homeLocalPosition + new Vector3(
                    Mathf.Lerp(centerOffset, 0f, easedDeal),
                    Mathf.Sin(easedDeal * Mathf.PI) * 18f,
                    0f);
                _rect.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, easedDeal);
                _rect.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Lerp((_cardIndex - 1) * 7f, 0f, easedDeal));
                if (dealt >= 1f)
                {
                    _dealSettled = true;
                    _rect.localPosition = _homeLocalPosition;
                    _rect.localScale = Vector3.one;
                    _rect.localRotation = Quaternion.identity;
                }
                return;
            }

            if (!_revealed) return;

            var entrance = Mathf.Clamp01(
                (age - _stagger) / EntranceDuration);
            entrance = 1f - Mathf.Pow(1f - entrance, 3f);

            var idle = Mathf.Sin(Time.unscaledTime * 1.18f + _phase);
            var targetScale = _pressed ? 0.982f : _hovered ? 1.032f :
                1f + idle * 0.0035f;
            var displayedScale = Mathf.Lerp(0.94f, targetScale, entrance);
            var tilt = _hovered || _pressed ? 0f : idle * 0.42f;

            _rect.localScale = Vector3.Lerp(_rect.localScale,
                Vector3.one * displayedScale,
                1f - Mathf.Exp(-Time.unscaledDeltaTime * 13f));
            _rect.localRotation = Quaternion.Slerp(_rect.localRotation,
                Quaternion.Euler(0f, 0f, tilt),
                1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f));
        }

        public void MarkRevealed089()
        {
            _revealed = true;
            _dealSettled = true;
            if (_rect != null && _homeCaptured)
                _rect.localPosition = _homeLocalPosition;
            _bornAt = Time.unscaledTime;
        }

        public void SetHovered089(bool value)
        {
            _hovered = value;
            if (!value) _pressed = false;
        }

        public void SetPressed089(bool value)
        {
            _pressed = value;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHovered089(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered089(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SetPressed089(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed089(false);
        }
    }

    /// <summary>
    /// Keeps the shuffle/deal state legible while the three physical card backs
    /// gather, cut, and travel to their slots. It never owns card authority.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class ExpeditionShuffleDealStatus089 : MonoBehaviour
    {
        Text _label;
        bool _motionEnabled;
        string _readyCopy;

        public void Configure089(Text label, bool motionEnabled,
            string readyCopy)
        {
            _label = label;
            _motionEnabled = motionEnabled;
            _readyCopy = string.IsNullOrWhiteSpace(readyCopy)
                ? "CHOOSE ONE CARD" : readyCopy;
            if (!_motionEnabled && _label != null)
            {
                _label.text = "THREE CARDS READY  •  " + _readyCopy;
                _label.color = RuntimeUi.Positive;
            }
        }

        IEnumerator Start()
        {
            if (!_motionEnabled || _label == null) yield break;
            _label.text = "SHUFFLING THE EXPEDITION DECK…";
            yield return new WaitForSecondsRealtime(0.26f);
            if (_label == null) yield break;
            _label.text = "CUTTING THE DECK…";
            yield return new WaitForSecondsRealtime(0.22f);
            if (_label == null) yield break;
            _label.text = "DEALING THREE FACE-DOWN CARDS…";
            yield return new WaitForSecondsRealtime(0.38f);
            if (_label == null) yield break;
            _label.text = "THREE CARDS READY  •  " + _readyCopy;
            _label.color = RuntimeUi.Positive;
        }
    }

    /// <summary>Relays pointer feedback from the authoritative child Button.</summary>
    [DisallowMultipleComponent]
    internal sealed class ExpeditionRouteCardButtonRelay089 : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        ExpeditionRouteCardMotion089 _motion;

        public void Bind089(ExpeditionRouteCardMotion089 motion)
        {
            _motion = motion;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            _motion?.SetHovered089(true);

        public void OnPointerExit(PointerEventData eventData) =>
            _motion?.SetHovered089(false);

        public void OnPointerDown(PointerEventData eventData) =>
            _motion?.SetPressed089(true);

        public void OnPointerUp(PointerEventData eventData) =>
            _motion?.SetPressed089(false);
    }
}
