using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private RectTransform _worldGateEventRoot110;
        private RectTransform _worldGateEventBody110;
        private string _worldGateEventReceipt110 = string.Empty;
        private string _skippedWorldGateEventReceipt110 = string.Empty;

        private void BuildFullScreenWorldGateEvent110(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            ConcealCommittedEffectCopy132(coordinator,state);
            if (TryBuildCampaignSavedEvent131(coordinator, state)) return;
            if (_screenRoot == null || state?.CurrentNode == null ||
                string.IsNullOrWhiteSpace(state.PendingReceiptId)) return;
            if (_activePage != null && _activePage != _screenRoot &&
                _activePage != _worldGateEventRoot110 && _activePage.IsChildOf(_screenRoot))
                _activePage.gameObject.SetActive(false);
            if (_worldGateEventRoot110 != null)
            {
                _worldGateEventRoot110.gameObject.SetActive(false);
                Destroy(_worldGateEventRoot110.gameObject);
            }
            var panel = RuntimeUi.AddPanel(_screenRoot, "Committed Expedition Event 110",
                new Color(0.008f, 0.016f, 0.026f, 1f));
            _worldGateEventRoot110 = panel.rectTransform;
            Stretch(_worldGateEventRoot110);
            panel.gameObject.AddComponent<WorldGateEventLifetime110>();
            _worldGateEventReceipt110 = state.PendingReceiptId;
            _activePage = _worldGateEventRoot110;
            _activeContent = null;
            _activeScroll = null;
            var title = RuntimeUi.AddText(panel.transform, "Committed Expedition Event Context 110",
                state.ActiveBoardTitle ?? "EXPEDITION", 38, TextAnchor.MiddleLeft,
                RuntimeUi.Text, FontStyle.Bold);
            SetAnchors074(title.rectTransform, new Vector2(0.025f, 0.92f), new Vector2(0.60f, 0.985f));
            var back = RuntimeUi.AddButton(panel.transform, "Committed Expedition Event Back 110",
                "BACK TO GUILD", ReturnToWalkableHall069);
            SetAnchors074(back.GetComponent<RectTransform>(), new Vector2(0.615f, 0.92f), new Vector2(0.785f, 0.985f));
            var receipt = state.PendingReceiptId;
            var skipped = StringComparer.Ordinal.Equals(_skippedWorldGateEventReceipt110, receipt);
            var skip = RuntimeUi.AddButton(panel.transform, "Committed Expedition Event Skip 110",
                skipped || _reducedMotion ? "SAVED RESULT" : "SKIP ANIMATION",
                () => SkipWorldGateEventReveal110(coordinator, receipt));
            SetAnchors074(skip.GetComponent<RectTransform>(), new Vector2(0.80f, 0.92f), new Vector2(0.975f, 0.985f));
            skip.interactable = !skipped && !_reducedMotion;
            foreach (var button in new[] { back, skip })
            {
                var text = button.GetComponentInChildren<Text>();
                ConfigureResponsiveText062(text, 28, 34);
                text.rectTransform.offsetMin = new Vector2(8f, 6f);
                text.rectTransform.offsetMax = new Vector2(-8f, -6f);
            }
            _worldGateEventBody110 = RuntimeUi.AddStretchRect(panel.transform, "Committed Expedition Event Body 110");
            SetAnchors074(_worldGateEventBody110, new Vector2(0.02f, 0.025f), new Vector2(0.98f, 0.905f));
            var reduced = _reducedMotion;
            try
            {
                // Existing reveal builders settle immediately in reduced-motion
                // mode. This temporary presentation choice never saves a setting.
                if (skipped) _reducedMotion = true;
                BuildWorldGatePrimaryAction084(_worldGateEventBody110, coordinator, state, state.CurrentNode);
            }
            finally { _reducedMotion = reduced; }
            var controls = panel.GetComponentsInChildren<Button>(false).Where(value => value.interactable).ToArray();
            for (var index = 0; index < controls.Length; index++)
            {
                var navigation = controls[index].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = navigation.selectOnUp = controls[(index + controls.Length - 1) % controls.Length];
                navigation.selectOnRight = navigation.selectOnDown = controls[(index + 1) % controls.Length];
                controls[index].navigation = navigation;
            }
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject((skip.interactable ? skip : back).gameObject);
            _worldGateEventRoot110.SetAsLastSibling();
        }

        private void SkipWorldGateEventReveal110(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator, string expectedReceipt)
        {
            if (_worldGateEventRoot110 == null || !_worldGateEventRoot110.gameObject.activeInHierarchy ||
                !StringComparer.Ordinal.Equals(_worldGateEventReceipt110, expectedReceipt) ||
                StringComparer.Ordinal.Equals(_skippedWorldGateEventReceipt110, expectedReceipt)) return;
            var latest = coordinator?.CampaignWorldGate023;
            if (latest == null || !StringComparer.Ordinal.Equals(latest.PendingReceiptId, expectedReceipt)) return;
            _skippedWorldGateEventReceipt110 = expectedReceipt;
            BuildFullScreenWorldGateEvent110(coordinator, latest);
        }

        private bool IsFullScreenWorldGateEventBody110(Transform body) =>
            body != null && _worldGateEventBody110 != null && body == _worldGateEventBody110;

        private void BindWorldGateEventReceiptRoutine110(string receipt, Coroutine routine)
        {
            if (_worldGateEventRoot110 == null || routine == null ||
                !StringComparer.Ordinal.Equals(_worldGateEventReceipt110, receipt)) return;
            var owner = _worldGateEventRoot110.GetComponent<WorldGateEventLifetime110>();
            // The presenter's OnDisable may already have stopped an older timer
            // without disabling its visual root. Do not remove the new schedule key.
            owner.Detach110();
            owner.BindCancellation110(() =>
            {
                if (this != null) StopCoroutine(routine);
                _scheduledWorldGateReceiptApplies084.Remove(receipt);
            });
        }

        private void DetachWorldGateEventReceiptRoutine110(string receipt)
        {
            if (_worldGateEventRoot110 != null && StringComparer.Ordinal.Equals(_worldGateEventReceipt110, receipt))
                _worldGateEventRoot110.GetComponent<WorldGateEventLifetime110>().Detach110();
        }

        private void ExpandWorldGateEventCard110(RectTransform card, bool chest)
        {
            if (card == null || !IsFullScreenWorldGateEventBody110(card.parent)) return;
            if (_campaignQuestRoot131 != null && card.IsChildOf(_campaignQuestRoot131))
                StyleCampaignQuestExistingScene131(card, null);
            else ExpandSavedEventCard110(card, chest);
        }

        private void ExpandSavedEventCard110(RectTransform card, bool chest)
        {
            if (card == null) return;
            var layout = card.GetComponent<VerticalLayoutGroup>();
            if (layout != null) layout.enabled = false;
            var fitter = card.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            RuntimeUi.SetLayout(card).ignoreLayout = true;
            Stretch(card);
            var reading = card.Find("Board Card Reading Column 091") as RectTransform;
            if (reading == null)
            {
                // Missing illustration must still leave a readable saved event.
                reading = RuntimeUi.AddStretchRect(card, "Board Card Reading Column 091");
                RuntimeUi.AddVerticalLayout(reading, new RectOffset(8, 8, 8, 8), 12f, TextAnchor.UpperLeft);
                foreach (var child in card.Cast<Transform>().ToArray())
                {
                    if (child == reading || child.GetComponent<LayoutElement>()?.ignoreLayout == true) continue;
                    child.SetParent(reading, false);
                }
            }
            var hasIllustration = card.Find("Board Card Illustration Window 091") != null;
            var viewport = RuntimeUi.AddPanel(card, "Committed Expedition Event Reading Viewport 110", Color.clear);
            SetAnchors074(viewport.rectTransform, new Vector2(hasIllustration ? 0.51f : 0.04f, 0.04f), new Vector2(0.975f, 0.96f));
            viewport.gameObject.AddComponent<RectMask2D>();
            reading.SetParent(viewport.transform, false);
            reading.anchorMin = new Vector2(0f, 1f);
            reading.anchorMax = Vector2.one;
            reading.pivot = new Vector2(0.5f, 1f);
            reading.anchoredPosition = Vector2.zero;
            reading.sizeDelta = Vector2.zero;
            var contentFitter = reading.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = reading;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            foreach (var child in reading.Cast<Transform>())
            {
                var text = child.GetComponent<Text>();
                if (text != null)
                {
                    var isTitle = StringComparer.Ordinal.Equals(text.name, "Title") ||
                        text.name.StartsWith("Title [", StringComparison.Ordinal);
                    text.resizeTextForBestFit = false;
                    text.fontSize = isTitle ? 44 : 32;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Overflow;
                    var element = RuntimeUi.SetLayout(text);
                    element.preferredHeight = -1f;
                    element.minHeight = isTitle ? 96f : 44f;
                    element.flexibleHeight = 0f;
                }
                var button = child.GetComponent<Button>();
                if (button != null)
                {
                    RuntimeUi.SetLayout(button, preferredHeight: 110f).minHeight = 110f;
                    ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 28, 34);
                }
            }
            var dice = card.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(value => value.name == "Authoritative Dice Roll 084");
            if (dice != null)
            {
                RuntimeUi.SetLayout(dice, preferredHeight: 280f).minHeight = 280f;
                foreach (var die in dice.GetComponentsInChildren<RectTransform>(true)
                    .Where(value => value.name == "First Authoritative Die 084" || value.name == "Second Authoritative Die 084"))
                {
                    die.sizeDelta = new Vector2(136f, 136f);
                    var element = RuntimeUi.SetLayout(die.parent, preferredWidth: 144f, preferredHeight: 144f);
                    element.minWidth = element.minHeight = 144f;
                    foreach (var pip in die.Cast<Transform>().Where(value => value.name.StartsWith("Die Pip ", StringComparison.Ordinal)))
                        ((RectTransform)pip).sizeDelta = new Vector2(30f, 30f);
                }
                foreach (var text in dice.GetComponentsInChildren<Text>(true))
                {
                    text.resizeTextForBestFit = false;
                    text.fontSize = text.name == "Authoritative Dice Motion Status 086" ? 28 : 40;
                }
            }
            if (chest)
            {
                var art = card.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(value => value.name == "Revealed Expedition Card Face 089");
                var aspect = art?.GetComponent<AspectRatioFitter>();
                if (aspect != null) aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
        }
    }
}
