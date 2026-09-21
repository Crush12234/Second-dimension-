using System;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        bool TryBuildCampaignInterruption132(Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            Campaign020.CampaignPlayablePresentationState020 state)
        {
            var flow = coordinator as Campaign020.ICampaignCardFlowCoordinator132;
            var view = flow?.CampaignInterruption132;
            if (view == null || _screenRoot == null) return false;
            var body = BuildCampaignQuestShell131(state.OperationTitle, view.Title, state.WorldName,
                view.Kind == "BOSS" ? "STORY BATTLE" : "STORY MOMENT", ReturnToWalkableHall069);
            var root = _campaignQuestRoot131;
            _campaignCardRoot129 = root;
            _campaignCardReceipt129 = view.Identity ?? string.Empty;
            var life = root.gameObject.AddComponent<WorldGateEventLifetime110>();
            if (view.Kind == "RESUME")
            {
                // A pre132 save may have stopped before this authored receipt.
                // Resuming is explicit and writes only its existing step authority;
                // the actual interruption then uses the same saved receipt path.
                RuntimeUi.AddVerticalLayout(body, new RectOffset(40,40,30,30), 18f, TextAnchor.MiddleCenter);
                var title = RuntimeUi.AddText(body, "Saved Campaign Story Title 132", view.Title, 36,
                    TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
                RuntimeUi.SetLayout(title, preferredHeight: 90f);
                var copy = RuntimeUi.AddText(body, "Saved Campaign Story Description 132", view.Description, 27,
                    TextAnchor.MiddleCenter, RuntimeUi.Text);
                RuntimeUi.SetLayout(copy, preferredHeight: 180f);
                RuntimeUi.AddButton(body, "Resume Saved Story Interruption 132", "RESUME SAVED STORY",
                    () => RunGuildCityCommand017D(() => flow.ResumeCampaignInterruption132(view.OperationId, view.StepIndex)),
                    110f, RuntimeUi.Accent);
                return true;
            }
            if (StringComparer.Ordinal.Equals(_campaignCardRetryReceipt129, view.Identity))
            {
                AddMessagePanel(body, "STORY SAVE NEEDS RETRY", _localStatus, RuntimeUi.Warning);
                RuntimeUi.AddButton(body, "Retry Campaign Interruption 132", "RETRY SAVE",
                    () => CompleteCampaignInterruptionView132(flow, view, root, life), 110f, RuntimeUi.Accent);
                return true;
            }
            // Every interruption keeps its authored illustration and reading card.
            // Only ENTER BATTLE creates combat, before any battle shatter is shown.
            var scene = BuildCampaignQuestScene131(body, view.Title, view.Description,
                BoardRoomIllustrationResource091(view.Kind == "BOSS" ? "MONSTER" : "STORY"));
            var canceled = false;
            var continueButton = RuntimeUi.AddButton(CampaignQuestActionsParent131(scene),
                "Skip Committed Story Glass 132", view.Kind == "BOSS" ? "ENTER BATTLE" : "CONTINUE STORY",
                () =>
                {
                    if (!canceled && root != null && root.gameObject.activeInHierarchy && root == _campaignQuestRoot131)
                        CompleteCampaignInterruptionView132(flow, view, root, life);
                }, 110f, RuntimeUi.Accent);
            FitCampaignQuestActions131(scene);
            life.BindCancellation110(() => { canceled = true; if (continueButton != null) continueButton.interactable = false; });
            return true;
        }

        void CompleteCampaignInterruptionView132(Campaign020.ICampaignCardFlowCoordinator132 flow,
            Campaign020.CampaignInterruptionView132 expected, RectTransform root, WorldGateEventLifetime110 life)
        {
            if (_campaignRoomCommandRunning084 || root == null || root != _campaignQuestRoot131 || !root.gameObject.activeInHierarchy) return;
            var current = flow.CampaignInterruption132;
            if (current == null || current.Identity != expected.Identity) return;
            _campaignRoomCommandRunning084 = true;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            M1CommandResult result;
            try { result = flow.CompleteCampaignInterruption132(expected.Identity); }
            finally { _suppressBoardAdventureCoordinatorRefresh084 = false; _campaignRoomCommandRunning084 = false; }
            life.Detach110();
            _campaignCardRetryReceipt129 = result?.Succeeded == true ? string.Empty : expected.Identity;
            _localStatus = result?.Succeeded == true ? string.Empty : result?.Message ?? "The saved story interruption could not continue.";
            _localStatusPositive = result?.Succeeded == true;
            if (result?.Succeeded == true && expected.Kind == "BOSS")
            {
                Navigate(M1Screen.Battle);
                _battleExperience072?.BeginStoryBattleIntro132(expected.Identity, expected.Title);
            }
            else { _guildCityTab017D = "CAMPAIGN"; BuildCurrentScreen(); }
        }
    }
}
