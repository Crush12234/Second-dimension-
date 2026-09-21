using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        bool TryBuildCampaignSavedEvent131(
            Campaign023.ICampaignWorldGatePresentationCoordinator023 coordinator,
            Campaign023.CampaignWorldGatePresentationState023 state)
        {
            if (_screenRoot == null || state?.ActiveOperationKind != "CHAPTER" || state.CurrentNode == null ||
                string.IsNullOrWhiteSpace(state.PendingReceiptId)) return false;
            _worldGateEventBody110 = BuildCampaignQuestShell131(state.ActiveBoardTitle, state.BoardObjective,
                state.CurrentWorldName, "ENCOUNTER", ReturnToWalkableHall069);
            _worldGateEventRoot110 = _campaignQuestRoot131;
            if (_worldGateEventRoot110.GetComponent<WorldGateEventLifetime110>() == null)
                _worldGateEventRoot110.gameObject.AddComponent<WorldGateEventLifetime110>();
            _worldGateEventReceipt110 = state.PendingReceiptId;
            var receipt = state.PendingReceiptId;
            var skipped = StringComparer.Ordinal.Equals(_skippedWorldGateEventReceipt110, receipt);
            var skip = RuntimeUi.AddButton(_worldGateEventRoot110, "Committed Expedition Event Skip 110",
                skipped || _reducedMotion ? "SAVED RESULT" : "SKIP ANIMATION",
                () => SkipWorldGateEventReveal110(coordinator, receipt));
            SetAnchors074(skip.GetComponent<RectTransform>(), new Vector2(.78f,.805f), new Vector2(.96f,.865f));
            ConfigureAuthoredCompactText076(skip.GetComponentInChildren<UnityEngine.UI.Text>(), 18, 24);
            skip.interactable = !skipped && !_reducedMotion;
            var reduced = _reducedMotion;
            try
            {
                if (skipped) _reducedMotion = true;
                BuildWorldGatePrimaryAction084(_worldGateEventBody110, coordinator, state, state.CurrentNode);
            }
            finally { _reducedMotion = reduced; }
            return true;
        }
    }
}
