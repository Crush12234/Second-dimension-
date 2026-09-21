using System;
using SecondDimension.Gameplay.Campaign023;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private RectTransform BuildQuestFate165(Transform parent,
            IQuestFateCoordinator165 authority, QuestFateView165 fate)
        {
            var wheel = fate.Kind == ExpeditionDeckService089.Wheel132;
            var curse = fate.Kind == ExpeditionDeckService089.CurseD20132;
            var accent = curse ? new Color(.93f, .32f, .57f) : RuntimeUi.Accent;
            var failureNotice = !_localStatusPositive && !string.IsNullOrWhiteSpace(_localStatus)
                ? "COULD NOT SAVE\n" + _localStatus + "\n\n" : string.Empty;
            var panel = RuntimeUi.AddPanel(parent, "Quest Fate Table165", new Color(.018f, .025f, .058f, .99f));
            var face = panel.rectTransform;
            SetAnchors074(face, new Vector2(.035f, .025f), new Vector2(.965f, .805f));
            var border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = accent;
            border.effectDistance = new Vector2(2f, -2f);

            var title = RuntimeUi.AddText(face, "Quest Fate Heading165", fate.Title.ToUpperInvariant(),
                42, TextAnchor.MiddleCenter, accent, FontStyle.Bold);
            SetAnchors074(title.rectTransform, new Vector2(.025f, .835f), new Vector2(.975f, .985f));
            var visual = RuntimeUi.AddStretchRect(face, "Quest Fate Visual165");
            SetAnchors074(visual, new Vector2(.02f, .04f), new Vector2(.49f, .83f));
            var result = RuntimeUi.AddText(face, "Quest Fate Result165",
                fate.Rolled ? "FATE IN MOTION…" : "A SEALED " + (wheel ? "FORTUNE" : curse ? "CURSE" : "BLESSING"),
                34, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            SetAnchors074(result.rectTransform, new Vector2(.51f, .66f), new Vector2(.975f, .825f));

            // The committed reward may be longer than the phone's landscape
            // panel. Give it a real scroll region; the action stays reachable.
            var viewport = RuntimeUi.AddPanel(face, "Quest Fate Reward View165", new Color(.009f, .016f, .035f, .6f));
            SetAnchors074(viewport.rectTransform, new Vector2(.51f, .28f), new Vector2(.975f, .65f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = RuntimeUi.AddStretchRect(viewport.transform, "Quest Fate Reward Content165");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            RuntimeUi.AddVerticalLayout(content, new RectOffset(12, 12, 8, 8), 4f, TextAnchor.UpperLeft);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var copy = RuntimeUi.AddText(content, "Quest Fate Reward165", fate.Rolled
                ? "The result appears when the motion settles."
                : wheel ? "Spin to discover XP, a supply cache, or new gear."
                : curse ? "Roll the D20 to reveal this quest's temporary check penalty."
                : fate.Lucky ? "LUCKY BLESSING\nThe higher of two D20 rolls is saved and shown. Roll to reveal your result."
                : "Roll the D20 to reveal a quest blessing. A natural 20 may grant a permanent hero boon.",
                30, TextAnchor.UpperLeft, RuntimeUi.Text);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 55f;

            var action = RuntimeUi.AddButton(face, "Quest Fate Action165", fate.Rolled ? "REVEALING…"
                : wheel ? "SPIN THE WHEEL" : "ROLL D20", () =>
                RunQuestFateCommand165(() => fate.Rolled
                    ? authority.CollectQuestFate165(fate.ReceiptId)
                    : authority.RollQuestFate165(fate.ReceiptId)), 110f, accent);
            SetAnchors074((RectTransform)action.transform, new Vector2(.51f, .04f), new Vector2(.975f, .235f));
            action.interactable = !fate.Rolled;

            if (!fate.Rolled)
            {
                var sealedFace = RuntimeUi.AddStretchRect(visual, "Quest Sealed Fate165");
                var graphic = sealedFace.gameObject.AddComponent<FatePolyhedronGraphic132>();
                graphic.Wheel132 = wheel;
                graphic.color = accent;
                graphic.raycastTarget = false;
                if (wheel)
                {
                    FateWheelLabels132.Attach132(graphic);
                    var pointer = RuntimeUi.AddText(visual, "Quest Wheel Pointer165", "▼", 62,
                        TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
                    SetAnchors074(pointer.rectTransform, new Vector2(.42f, .82f), new Vector2(.58f, 1f));
                    pointer.raycastTarget = false;
                }
                var mystery = RuntimeUi.AddText(visual, "Quest Sealed Fate Mark165", "?", 96,
                    TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
                SetAnchors074(mystery.rectTransform, Vector2.zero, Vector2.one);
                mystery.raycastTarget = false;
            }
            else
            {
                CommittedFateEffect132.Play132(visual, wheel, wheel ? fate.WheelSector : fate.D20,
                    accent, _reducedMotion, () =>
                    {
                        if (face == null || !face.gameObject.activeInHierarchy) return;
                        result.text = fate.Result;
                        result.color = curse ? RuntimeUi.Warning : RuntimeUi.Positive;
                        copy.text = failureNotice + fate.Reward;
                        scroll.verticalNormalizedPosition = 1f;
                        action.GetComponentInChildren<Text>().text = "COLLECT & CONTINUE";
                        action.interactable = true;
                    });
            }

            if (failureNotice.Length > 0 && !fate.Rolled)
                copy.text = failureNotice + copy.text;
            panel.gameObject.AddComponent<QuestFateReadability165>().Configure165(
                title, result, copy, action, viewport.rectTransform, visual);
            return face;
        }

        private void RunQuestFateCommand165(Func<M1CommandResult> command)
        {
            if (_boardQuestCommandRunning081) return;
            _boardQuestCommandRunning081 = true;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            M1CommandResult outcome;
            try { outcome = command(); }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _boardQuestCommandRunning081 = false;
            }
            _localStatusPositive = outcome?.Succeeded == true;
            _localStatus = _localStatusPositive ? string.Empty : outcome?.Message ?? "The event could not be saved. Try again.";
            if (_localStatusPositive)
            {
                _lastBoardQuestCard090 = null;
                _boardQuestCardAwaitingAcknowledgement090 = false;
                _selectedExpeditionDestination074 = null;
            }
            BuildCurrentScreen();
        }
    }

    // The authored page scale is applied after construction. Enforce physical
    // pixel readability after it and after a resize, including at 145% text.
    internal sealed class QuestFateReadability165 : MonoBehaviour
    {
        private Text _title, _result, _copy;
        private Button _action;
        private RectTransform _viewport;
        private Text[] _visualTexts;
        private int[] _visualFontSizes;

        public void Configure165(Text title, Text result, Text copy, Button action, RectTransform viewport,
            RectTransform visual)
        {
            _title = title; _result = result; _copy = copy; _action = action; _viewport = viewport;
            // Preserve the reusable die/wheel's authored mesh labels. The
            // page's 32-unit text floor is inappropriate for small die facets.
            _visualTexts = visual.GetComponentsInChildren<Text>(true);
            _visualFontSizes = new int[_visualTexts.Length];
            for (var index = 0; index < _visualTexts.Length; index++)
                _visualFontSizes[index] = _visualTexts[index].fontSize;
        }

        private void LateUpdate()
        {
            if (_title == null || _action == null) return;
            var scale = Mathf.Max(.01f, GetComponentInParent<Canvas>()?.scaleFactor ?? 1f);
            var compact = Screen.height <= 500 || Screen.width <= 900;
            Font165(_title, compact ? 18f : 24f, scale);
            Font165(_result, compact ? 14f : 19f, scale);
            Font165(_copy, compact ? 12f : 16f, scale);
            Font165(_action.GetComponentInChildren<Text>(), compact ? 13f : 17f, scale);
            for (var index = 0; index < _visualTexts.Length; index++)
                if (_visualTexts[index] != null)
                    _visualTexts[index].fontSize = _visualFontSizes[index];
            var element = _copy.GetComponent<LayoutElement>() ?? _copy.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 0f;
            element.flexibleHeight = 0f;
            element.preferredHeight = _copy.preferredHeight + 4f / scale;

            var button = (RectTransform)_action.transform;
            var minHeight = 44f / scale;
            var parent = (RectTransform)button.parent;
            var desiredHeight = Mathf.Max(minHeight, parent.rect.height * .195f);
            button.anchorMin = button.anchorMax = new Vector2(.7425f, .04f);
            button.pivot = new Vector2(.5f, 0f);
            button.anchoredPosition = Vector2.zero;
            button.sizeDelta = new Vector2(parent.rect.width * .465f, desiredHeight);
            var bottom = parent.rect.height * .04f + desiredHeight + 8f / scale;
            _viewport.offsetMin = new Vector2(0f, Mathf.Max(0f, bottom - parent.rect.height * .28f));
        }

        private static void Font165(Text text, float pixels, float scale)
        {
            if (text == null) return;
            text.resizeTextForBestFit = false;
            text.fontSize = Mathf.CeilToInt(pixels / scale);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
