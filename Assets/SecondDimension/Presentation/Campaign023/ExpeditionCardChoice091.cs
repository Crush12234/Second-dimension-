using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Campaign023
{
    /// <summary>
    /// A blind, three-card presentation. Dealing and turning a card never call
    /// gameplay; only the revealed card's explicit action submits its command.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpeditionCardChoice091 : MonoBehaviour
    {
        public enum ChoicePhase091 { Dealing, AwaitingChoice, Revealing, AwaitingAction, Submitted }
        public const float RevealDuration091 = 0.68f;
        const float DealDuration091 = ExpeditionRouteCardMotion089.ShuffleAndDealLead089 + 0.20f;

        sealed class Entry
        {
            public RectTransform Wrapper;
            public RectTransform Face;
            public GameObject Back;
            public Button Pick;
            public Button Action;
            public Button Return;
            public ExpeditionRouteCardMotion089 Motion;
            public CanvasGroup Group;
            public bool CanCommit;
            public System.Action Commit;
            public Vector3 Home;
            public bool Seen;
            public string ActionLabel;
        }

        readonly List<Entry> _cards = new List<Entry>(3);
        bool _animate;
        bool _automaticTick;
        float _elapsed;
        Text _status;
        HorizontalLayoutGroup _layout;
        int _selected = -1;
        public ChoicePhase091 Phase091 { get; private set; }
        public int SelectedIndex091 => _selected;

        public void Configure091(bool animate, Text status, bool automaticTick = true)
        {
            _animate = animate;
            _automaticTick = automaticTick;
            _status = status;
            _layout = GetComponent<HorizontalLayoutGroup>();
            _elapsed = 0f;
            Phase091 = ChoicePhase091.Dealing;
        }

        public void Register091(RectTransform wrapper, RectTransform face,
            Button action, bool canCommit, Action commit, Sprite cardBack,
            bool optionalPurchase)
        {
            if (_cards.Count == 3)
                throw new InvalidOperationException("A blind draft contains exactly three cards.");
            var index = _cards.Count;
            // Unity's missing-component sentinel is not CLR null in the
            // Editor; null-coalescing can retain it instead of adding a group.
            var canvasGroup = wrapper.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = wrapper.gameObject.AddComponent<CanvasGroup>();
            var entry = new Entry
            {
                Wrapper = wrapper,
                Face = face,
                Action = action,
                CanCommit = canCommit,
                Commit = commit,
                ActionLabel = action.GetComponentInChildren<Text>(true)?.text,
                Motion = wrapper.GetComponent<ExpeditionRouteCardMotion089>(),
                Group = canvasGroup
            };
            _cards.Add(entry);
            face.gameObject.SetActive(false);
            action.interactable = false;
            action.onClick.RemoveAllListeners();
            action.onClick.AddListener(() =>
            {
                if (Phase091 == ChoicePhase091.AwaitingChoice) Choose091(index);
                else Confirm091(index);
            });

            var back = RuntimeUi.AddPanel(wrapper,
                "Blind Quest Card Back " + index + " 091", Color.white);
            Stretch091(back.rectTransform);
            back.rectTransform.offsetMin = new Vector2(6f, 6f);
            back.rectTransform.offsetMax = new Vector2(-6f, -6f);
            back.sprite = cardBack;
            back.type = Image.Type.Simple;
            back.preserveAspect = true;
            back.raycastTarget = true;
            // The authored magic back is portrait art. Its visible viewport and
            // the pick hit area share this rect, so neither stretches across a
            // landscape slot. The revealed event face may expand for legibility.
            var backAspect = back.gameObject.AddComponent<AspectRatioFitter>();
            backAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            backAspect.aspectRatio = cardBack != null && cardBack.rect.height > 0f
                ? cardBack.rect.width / cardBack.rect.height : 2f / 3f;
            var shadow = back.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
            shadow.effectDistance = new Vector2(0f, -9f);
            entry.Back = back.gameObject;
            entry.Pick = back.gameObject.AddComponent<Button>();
            entry.Pick.targetGraphic = back;
            entry.Pick.transition = Selectable.Transition.ColorTint;
            var pickColors = entry.Pick.colors;
            pickColors.normalColor = Color.white;
            pickColors.disabledColor = Color.white;
            pickColors.highlightedColor = new Color(1f, 0.97f, 0.90f, 1f);
            pickColors.pressedColor = new Color(0.86f, 0.90f, 0.97f, 1f);
            pickColors.fadeDuration = 0.10f;
            entry.Pick.colors = pickColors;
            entry.Pick.interactable = false;
            entry.Pick.onClick.AddListener(() => Choose091(index));
            var relay = back.gameObject.AddComponent<ExpeditionRouteCardButtonRelay089>();
            relay.Bind089(entry.Motion);

            var prompt = RuntimeUi.AddText(back.transform,
                "Blind Quest Card Pick Prompt " + index + " 091",
                "PICK THIS CARD", 23, TextAnchor.MiddleCenter,
                new Color(1f, 0.91f, 0.67f), FontStyle.Bold);
            prompt.rectTransform.anchorMin = new Vector2(0.10f, 0.05f);
            prompt.rectTransform.anchorMax = new Vector2(0.90f, 0.14f);
            prompt.rectTransform.offsetMin = Vector2.zero;
            prompt.rectTransform.offsetMax = Vector2.zero;
            prompt.resizeTextForBestFit = true;
            prompt.resizeTextMinSize = 14;
            prompt.resizeTextMaxSize = 23;
            prompt.raycastTarget = false;

            // A blind reveal cannot authorize an undisclosed XP purchase.
            // The existing action remains explicit; declining leaves the saved
            // row unchanged because the deck has no separate skip authority.
            if (!canCommit || optionalPurchase)
            {
                var returnButton = RuntimeUi.AddButton(face,
                    "Return Revealed Quest Card " + index + " 091",
                    canCommit ? "NOT NOW • RETURN TO CARDS" : "CHOOSE ANOTHER CARD",
                    ReturnToCards091, 42f, RuntimeUi.ButtonNormal);
                returnButton.interactable = false;
                var returnLabel = returnButton.GetComponentInChildren<Text>(true);
                if (returnLabel != null)
                {
                    returnLabel.resizeTextForBestFit = true;
                    returnLabel.resizeTextMinSize = 14;
                    returnLabel.resizeTextMaxSize = 18;
                }
                entry.Return = returnButton;
            }
            if (_cards.Count == 3 && !_animate) SettleDeal091();
        }

        void Update()
        {
            if (_automaticTick) Tick091(Time.unscaledDeltaTime);
        }

        public void Tick091(float deltaTime)
        {
            if (_cards.Count != 3 || deltaTime < 0f) return;
            if (Phase091 == ChoicePhase091.Dealing)
            {
                _elapsed += deltaTime;
                if (!_animate || _elapsed >= DealDuration091) SettleDeal091();
                return;
            }
            if (Phase091 != ChoicePhase091.Revealing) return;
            _elapsed += deltaTime;
            var t = _animate ? Mathf.Clamp01(_elapsed / RevealDuration091) : 1f;
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                if (index == _selected)
                {
                    card.Wrapper.localPosition = Vector3.Lerp(card.Home,
                        _cards[1].Home, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 20f;
                    // The physical card, not its UI container, turns edge-on.
                    var width = Mathf.Max(0.025f, Mathf.Abs(Mathf.Cos(t * Mathf.PI)));
                    card.Wrapper.localScale = new Vector3(width, 1f, 1f);
                    if (t >= 0.5f)
                    {
                        card.Seen = true;
                        card.Back.SetActive(false);
                        card.Face.gameObject.SetActive(true);
                    }
                }
                else
                {
                    var center = _cards[1].Home;
                    card.Wrapper.localPosition = Vector3.Lerp(card.Home,
                        center + Vector3.up * 95f, eased);
                    card.Wrapper.localScale = Vector3.one * Mathf.Lerp(1f, 0.42f, eased);
                    card.Wrapper.localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Lerp(0f, (index - 1) * 12f, eased));
                    card.Group.alpha = 1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.25f, 0.95f, t));
                }
            }
            if (t < 1f) return;
            for (var index = 0; index < _cards.Count; index++)
                if (index != _selected)
                {
                    _cards[index].Back.SetActive(false);
                    _cards[index].Face.gameObject.SetActive(false);
                }
            var selected = _cards[_selected];
            selected.Wrapper.localPosition = _cards[1].Home;
            selected.Wrapper.localScale = Vector3.one;
            selected.Action.interactable = selected.CanCommit;
            if (selected.Return != null) selected.Return.interactable = true;
            Phase091 = ChoicePhase091.AwaitingAction;
            if (_status != null)
            {
                _status.text = selected.CanCommit
                    ? "YOUR CARD • CHOOSE ITS ACTION BELOW"
                    : "THIS OPPORTUNITY IS LOCKED • CHOOSE ANOTHER CARD";
                _status.color = selected.CanCommit ? RuntimeUi.Accent : RuntimeUi.Warning;
            }
            var focus = selected.CanCommit ? selected.Action : selected.Return;
            if (focus != null && EventSystem.current != null) focus.Select();
        }

        void SettleDeal091()
        {
            Phase091 = ChoicePhase091.AwaitingChoice;
            foreach (var card in _cards)
            {
                card.Pick.interactable = !card.Seen;
                if (card.Seen) card.Action.interactable = true;
                card.Motion?.MarkRevealed089();
            }
            for (var index = 0; index < _cards.Count; index++)
            {
                var target = _cards[index].Seen ? _cards[index].Action : _cards[index].Pick;
                var left = _cards[(index + 2) % 3];
                var right = _cards[(index + 1) % 3];
                var navigation = target.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = left.Seen ? left.Action : left.Pick;
                navigation.selectOnRight = right.Seen ? right.Action : right.Pick;
                target.navigation = navigation;
            }
            if (_status != null)
            {
                _status.text = "PICK 1 OF 3 • WHAT WILL YOU DISCOVER?";
                _status.color = RuntimeUi.Accent;
            }
            if (EventSystem.current != null)
                (_cards[0].Seen ? _cards[0].Action : _cards[0].Pick).Select();
        }

        public bool Choose091(int index)
        {
            if (Phase091 != ChoicePhase091.AwaitingChoice || index < 0 || index >= _cards.Count)
                return false;
            _selected = index;
            _elapsed = 0f;
            Phase091 = ChoicePhase091.Revealing;
            if (_layout != null) _layout.enabled = false;
            foreach (var card in _cards)
            {
                card.Pick.interactable = false;
                card.Action.interactable = false;
                if (card.Motion != null) card.Motion.enabled = false;
                card.Home = card.Wrapper.localPosition;
            }
            var actionLabel = _cards[index].Action.GetComponentInChildren<Text>(true);
            if (actionLabel != null) actionLabel.text = _cards[index].ActionLabel;
            if (_status != null) _status.text = "TURNING YOUR CARD • THE OTHER TWO RETURN UNSEEN";
            if (!_animate) Tick091(0f);
            return true;
        }

        public bool Confirm091(int index)
        {
            if (Phase091 != ChoicePhase091.AwaitingAction || index != _selected ||
                !_cards[index].CanCommit) return false;
            var card = _cards[index];
            Phase091 = ChoicePhase091.Submitted;
            card.Action.interactable = false;
            if (card.Return != null) card.Return.interactable = false;
            card.Commit?.Invoke();
            return true;
        }

        public void ReturnToCards091()
        {
            if (Phase091 != ChoicePhase091.AwaitingAction || _selected < 0 ||
                _cards[_selected].Return == null) return;
            foreach (var card in _cards)
            {
                card.Face.gameObject.SetActive(card.Seen);
                card.Back.SetActive(!card.Seen);
                card.Wrapper.localPosition = card.Home;
                card.Wrapper.localScale = Vector3.one;
                card.Wrapper.localRotation = Quaternion.identity;
                card.Group.alpha = 1f;
                card.Action.interactable = false;
                if (card.Seen)
                {
                    var label = card.Action.GetComponentInChildren<Text>(true);
                    if (label != null) label.text = "VIEW THIS CARD";
                }
                if (card.Return != null) card.Return.interactable = false;
                if (card.Motion != null) card.Motion.enabled = true;
            }
            _selected = -1;
            if (_layout != null) _layout.enabled = true;
            SettleDeal091();
        }

        static void Stretch091(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
