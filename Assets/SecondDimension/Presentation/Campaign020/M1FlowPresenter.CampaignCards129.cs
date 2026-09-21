using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        RectTransform _campaignCardRoot129;
        string _campaignCardReceipt129 = string.Empty;
        string _skippedCampaignCardReceipt129 = string.Empty;
        string _campaignCardRetryReceipt129 = string.Empty;

        void BuildCampaignCardTable129(Transform fallback,
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            Campaign020.CampaignPlayablePresentationState020 state,
            Campaign020.CampaignStepView020 current, string chapterReceipt)
        {
            if (TryBuildCampaignDeckStep131(fallback, coordinator, state, current, chapterReceipt)) return;
            if (TryBuildCampaignInterruption132(coordinator, state)) return;
            if (_screenRoot == null) return;
            var body = BuildCampaignQuestShell131(state.OperationTitle,
                current?.Title ?? "Return to the Guild", state.WorldName,
                state.Status == "ReadyToFinalize" ? "JOURNEY COMPLETE" : "STORY MOMENT",
                ReturnToWalkableHall069);
            _campaignCardRoot129 = _campaignQuestRoot131;
            if (_campaignCardRoot129.GetComponent<WorldGateEventLifetime110>() == null)
                _campaignCardRoot129.gameObject.AddComponent<WorldGateEventLifetime110>();
            _campaignCardReceipt129 = !string.IsNullOrWhiteSpace(state.PendingStepReceiptId)
                ? state.PendingStepReceiptId : chapterReceipt ?? string.Empty;
            var table = _campaignCardRoot129;
            var progress = RuntimeUi.AddText(table, "Story Save Status 131", "", 24,
                TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold);
            SetAnchors074(progress.rectTransform, new Vector2(.04f,.805f), new Vector2(.60f,.87f));
            if (!string.IsNullOrWhiteSpace(_campaignCardReceipt129))
            {
                var capturedReceipt = _campaignCardReceipt129;
                var skipped = StringComparer.Ordinal.Equals(_skippedCampaignCardReceipt129, capturedReceipt);
                var retry = StringComparer.Ordinal.Equals(_campaignCardRetryReceipt129, capturedReceipt);
                var skip = RuntimeUi.AddButton(table.transform, "Story Card Skip 129",
                    retry ? "RETRY SAVE" : skipped || _reducedMotion ? "REVEALED" : "SKIP ANIMATION",
                    () => { if (retry) RetryCampaignCardSave129(coordinator, capturedReceipt); else SkipCampaignCardReveal129(coordinator, capturedReceipt); });
                SetAnchors074(skip.GetComponent<RectTransform>(), new Vector2(0.78f, 0.805f), new Vector2(0.96f, 0.865f));
                ConfigureResponsiveText062(skip.GetComponentInChildren<Text>(), 26, 34);
                skip.interactable = retry || (!skipped && !_reducedMotion);
                var reduced = _reducedMotion;
                try
                {
                    if (skipped) _reducedMotion = true;
                    BuildCampaignPrimaryAction084(body, coordinator, state, current, chapterReceipt);
                }
                finally { _reducedMotion = reduced; }
                var result = body.Cast<Transform>().Select(value => value.GetComponent<Image>())
                    .FirstOrDefault(value => value != null && value.GetComponent<Button>() == null);
                if (result != null)
                    ArrangeStoryCardFace129(result.rectTransform, current?.Kind ?? "RETURN");
                // An already-claimed external battle can still require its
                // existing explicit chapter-return action before a new receipt.
                else BuildRevealedStoryCard129(body, state, current, false);
            }
            else if (current != null && !current.RequiresCertifiedBattle && !current.IsWorldBoard && state.Status != "ReadyToFinalize")
                BuildSealedStoryCard129(body, coordinator, state);
            else
            {
                BuildCampaignPrimaryAction084(body, coordinator, state, current, chapterReceipt);
                BuildRevealedStoryCard129(body, state, current, true);
            }
            if (!string.IsNullOrWhiteSpace(_localStatus) && !_localStatusPositive)
            {
                progress.text = _localStatus;
                progress.color = RuntimeUi.Warning;
                ConfigureResponsiveText062(progress, 18, 24);
            }
            _campaignCardRoot129.SetAsLastSibling();
        }

        void BuildSealedStoryCard129(RectTransform body,
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            Campaign020.CampaignPlayablePresentationState020 state)
        {
            var current = state.Steps?.FirstOrDefault(value => value.Status == "CURRENT");
            var card = BuildCampaignQuestScene131(body, current?.Title ?? state.OperationTitle,
                current?.Description ?? "Continue the story.",
                BoardRoomIllustrationResource091(BoardAdventureVisualKind087(current?.Kind, null, true)));
            var expectedRoot = _campaignCardRoot129;
            var expectedOperation = state.ActiveOperationId;
            var expectedStep = state.CurrentStepIndex;
            RuntimeUi.AddButton(CampaignQuestActionsParent131(card), "Move forward campaign room 084", "CONTINUE",
                () =>
                {
                    var latest = coordinator?.CampaignPlayable020;
                    if (expectedRoot == null || expectedRoot != _campaignCardRoot129 || !expectedRoot.gameObject.activeInHierarchy ||
                        latest == null || latest.ActiveOperationId != expectedOperation || latest.CurrentStepIndex != expectedStep ||
                        !string.IsNullOrWhiteSpace(latest.PendingStepReceiptId)) return;
                    CommitAndRevealCampaignStep084(coordinator, false);
                }, 100f, RuntimeUi.Accent);
            FitCampaignQuestActions131(card);
        }

        void BuildRevealedStoryCard129(RectTransform body,
            Campaign020.CampaignPlayablePresentationState020 state,
            Campaign020.CampaignStepView020 current, bool showDescription)
        {
            var actions = body.GetComponentsInChildren<Button>(false);
            var face = BuildCampaignQuestScene131(body,
                state.Status == "ReadyToFinalize" ? "RETURN TO THE GUILD" : current?.Title ?? "STORY COMPLETE",
                showDescription ? current?.Description ?? "Your journey continues." : "Return to the Guild to continue your journey.",
                BoardRoomIllustrationResource091(BoardAdventureVisualKind087(current?.Kind ?? "RETURN", null, true)));
            foreach (var action in actions) action.transform.SetParent(CampaignQuestActionsParent131(face), false);
            FitCampaignQuestActions131(face);
        }

        void ArrangeStoryCardFace129(RectTransform face, string roomKind)
        {
            StyleCampaignQuestExistingScene131(face,
                BoardRoomIllustrationResource091(BoardAdventureVisualKind087(roomKind, null, true)));
            face.Find("Board Adventure Card Flip Stage 086")?.SetAsLastSibling();
        }

        void SkipCampaignCardReveal129(Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator, string expected)
        {
            if (_campaignCardRoot129 == null || !_campaignCardRoot129.gameObject.activeInHierarchy ||
                !StringComparer.Ordinal.Equals(_campaignCardReceipt129, expected) ||
                StringComparer.Ordinal.Equals(_skippedCampaignCardReceipt129, expected)) return;
            var step = coordinator?.CampaignPlayable020?.PendingStepReceiptId;
            var chapter = (coordinator as Campaign019.ICampaignPresentationCoordinator019)?.Campaign019?.PendingReceiptId;
            if (!StringComparer.Ordinal.Equals(step, expected) && !StringComparer.Ordinal.Equals(chapter, expected)) return;
            _skippedCampaignCardReceipt129 = expected;
            BuildCurrentScreen();
        }

        void BindCampaignCardRoutine129(string receipt, Coroutine routine, bool chapter)
        {
            if (_campaignCardRoot129 == null || routine == null || !StringComparer.Ordinal.Equals(_campaignCardReceipt129, receipt)) return;
            var owner = _campaignCardRoot129.GetComponent<WorldGateEventLifetime110>();
            owner.Detach110();
            owner.BindCancellation110(() =>
            {
                if (this != null) StopCoroutine(routine);
                if (chapter) _scheduledCampaignChapterReceipts084.Remove(receipt);
                else _scheduledCampaignStepReceipts084.Remove(receipt);
            });
        }

        void RetryCampaignCardSave129(Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator, string expected)
        {
            if (_campaignCardRoot129 == null || !_campaignCardRoot129.gameObject.activeInHierarchy ||
                !StringComparer.Ordinal.Equals(_campaignCardReceipt129, expected) ||
                !StringComparer.Ordinal.Equals(_campaignCardRetryReceipt129, expected)) return;
            var step = coordinator?.CampaignPlayable020?.PendingStepReceiptId;
            var chapter = (coordinator as Campaign019.ICampaignPresentationCoordinator019)?.Campaign019?.PendingReceiptId;
            if (!StringComparer.Ordinal.Equals(step, expected) && !StringComparer.Ordinal.Equals(chapter, expected)) return;
            _campaignCardRetryReceipt129 = string.Empty;
            BuildCurrentScreen();
        }

        void ClearCampaignCardScheduling129()
        {
            // OnDisable has stopped the presenter's iterators. The separate
            // canvas may still exist, so release its obsolete callback and keys.
            if (_campaignCardRoot129 != null)
            {
                if (_campaignCardRoot129.gameObject.activeInHierarchy &&
                    !string.IsNullOrWhiteSpace(_campaignCardReceipt129))
                    _towerManualRefreshOnEnable117 = true;
                _campaignCardRoot129.GetComponent<WorldGateEventLifetime110>()?.Detach110();
            }
            _scheduledCampaignStepReceipts084.Clear();
            _scheduledCampaignChapterReceipts084.Clear();
        }
        void DetachCampaignCardRoutine129(string receipt, bool chapter)
        {
            if (_campaignCardRoot129 != null && StringComparer.Ordinal.Equals(_campaignCardReceipt129, receipt))
                _campaignCardRoot129.GetComponent<WorldGateEventLifetime110>().Detach110();
            if (chapter) _scheduledCampaignChapterReceipts084.Remove(receipt);
            else _scheduledCampaignStepReceipts084.Remove(receipt);
        }
    }
}
