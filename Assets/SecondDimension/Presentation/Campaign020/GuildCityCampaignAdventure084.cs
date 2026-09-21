using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        const float CampaignRoomResultHold084 = 0.82f;
        readonly HashSet<string> _scheduledCampaignStepReceipts084 =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _armedCampaignChapterReceipts084 =
            new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _scheduledCampaignChapterReceipts084 =
            new HashSet<string>(StringComparer.Ordinal);
        bool _campaignRoomCommandRunning084;

        void BuildGuildCityCampaign020(
            Transform body,
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            Campaign020.CampaignPlayablePresentationState020 state)
        {
            if (state == null || !state.IsAvailable)
            {
                AddMessagePanel(body, "STORY ADVENTURE",
                    state?.Error ?? "The story quest could not be opened.", RuntimeUi.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(state.ActiveOperationId))
            {
                return;
            }
            if (state.InsertedBattleBoundaryRecoveryRequired)
            {
                var recovery = AddMessagePanel(body,
                    "QUEST UPDATE READY",
                    "This saved quest reached its return-home tile before a required Union battle was added. " +
                    "Restore the battle tile and continue. Every prior reward and cleared route tile remains safe. " +
                    "The return-home result will reattach automatically after the battle.",
                    RuntimeUi.Warning);
                RuntimeUi.AddButton(recovery,
                    "Recover inserted campaign battle 084",
                    "RESTORE REQUIRED UNION BATTLE",
                    () => RunGuildCityCommand017D(
                        coordinator.RecoverInsertedBattleBoundary020),
                    132f, RuntimeUi.Warning);
                UseContentDrivenBoardPanelHeight084(recovery);
                return;
            }

            if (state.LegacyRecoveryRequired)
            {
                var recovery = AddMessagePanel(body,
                    "OLD UNFINISHED QUEST FOUND",
                    "This quest began before the new saved tile-proof rules. Recover old quest — " +
                    "no reward lost outside this unfinished run. Completed chapters, guild progress, " +
                    "recruits, inventory, and previous rewards stay exactly as saved.",
                    RuntimeUi.Warning);
                RuntimeUi.AddButton(recovery, "Recover legacy campaign quest 084",
                    "RECOVER OLD QUEST — NO REWARD FROM THIS RUN",
                    () => RunGuildCityCommand017D(
                        coordinator.RecoverLegacyPlayableQuest020),
                    132f, RuntimeUi.Warning);
                UseContentDrivenBoardPanelHeight084(recovery);
                return;
            }

            var current = (state.Steps ?? Array.Empty<Campaign020.CampaignStepView020>())
                .FirstOrDefault(value => value.Status == "CURRENT");
            var campaign019 = coordinator as Campaign019.ICampaignPresentationCoordinator019;
            var chapterReceiptId = campaign019?.Campaign019?.PendingReceiptId ??
                                   string.Empty;
            BuildCampaignCardTable129(body, coordinator, state, current, chapterReceiptId);
        }

        void BuildCampaignPrimaryAction084(
            Transform body,
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            Campaign020.CampaignPlayablePresentationState020 state,
            Campaign020.CampaignStepView020 current,
            string chapterReceiptId)
        {
            if (!string.IsNullOrWhiteSpace(state.PendingStepReceiptId))
            {
                var setback = IsCampaignSetback084(state.PendingOutcome);
                var result = AddMessagePanel(body,
                    (current?.Title ?? "A STORY MOMENT").ToUpperInvariant(),
                    (current?.Description ?? "The Guild continues its journey.") +
                    "\n\n" + (state.PendingReward ?? "Story progress"),
                    setback ? RuntimeUi.Warning : RuntimeUi.Positive);
                var resultCard = result as RectTransform ??
                                 result.GetComponent<RectTransform>();
                DecorateBoardAdventureRevealedCard084(
                    resultCard, current?.Kind);
                BuildBoardAdventureResolvedRewardStrip087(
                    result,
                    current?.Kind,
                    state.PendingReward,
                    "CAMPAIGN_STEP_REWARD_087|" + state.PendingStepReceiptId,
                    !setback,
                    false);
                var resolving = RuntimeUi.AddText(result,
                    "Campaign Automatic Step Apply 084",
                    "SAVED RESULT  •  CONTINUING THE STORY…",
                    18, TextAnchor.MiddleCenter, RuntimeUi.MutedText,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(resolving, preferredHeight: 30f);
                UseContentDrivenBoardPanelHeight084(result);
                AnimateBoardAdventureCardFlip084(
                    resultCard,
                    "CAMPAIGN_STEP_CARD_084|" + state.PendingStepReceiptId,
                    "ROOM " + Math.Min(Math.Max(1, state.TotalSteps),
                        Math.Max(1, state.CurrentStepIndex + 1)) + "  •  FACE DOWN", stageCueCopy: "TURNING THE STORY CARD");
                ScheduleCampaignStepReceiptApply084(
                    coordinator, state.PendingStepReceiptId);
                return;
            }

            if (state.Status == "ReadyToFinalize")
            {
                if (!string.IsNullOrWhiteSpace(chapterReceiptId) &&
                    (_armedCampaignChapterReceipts084.Contains(chapterReceiptId) ||
                     !state.ExistingBattleRewardReferenced))
                {
                    var result = AddMessagePanel(body,
                        "RETURN-HOME CARD FLIPPED",
                        "RESULT  •  CHAPTER COMPLETE\n" +
                        "REWARD  •  Guild progress, world standing, and new quests saved.",
                        RuntimeUi.Positive);
                    var resultCard = result as RectTransform ??
                                     result.GetComponent<RectTransform>();
                    DecorateBoardAdventureRevealedCard084(resultCard, "RETURN");
                    BuildBoardAdventureResolvedRewardStrip087(
                        result,
                        "RETURN",
                        "Guild progress, world standing, and new quests",
                        "CAMPAIGN_CHAPTER_REWARD_087|" + chapterReceiptId,
                        true,
                        false);
                    var resolving = RuntimeUi.AddText(result,
                        "Campaign Automatic Chapter Apply 084",
                        "SAVED  •  RETURNING TO THE GUILD…",
                        18, TextAnchor.MiddleCenter, RuntimeUi.MutedText,
                        FontStyle.Bold);
                    RuntimeUi.SetLayout(resolving, preferredHeight: 30f);
                    UseContentDrivenBoardPanelHeight084(result);
                    AnimateBoardAdventureCardFlip084(
                        resultCard,
                        "CAMPAIGN_CHAPTER_CARD_084|" + chapterReceiptId,
                        "RETURN HOME  •  FACE DOWN", stageCueCopy: "TURNING THE RETURN CARD");
                    ScheduleCampaignChapterReceiptApply084(
                        coordinator, chapterReceiptId);
                }
                else
                    RuntimeUi.AddButton(body, "Complete campaign chapter 084",
                        "RETURN TO GUILD\nCOMPLETE CHAPTER",
                        () => CommitCampaignChapterReturn084(coordinator),
                        132f, RuntimeUi.Positive);
                return;
            }
            if (current == null) return;

            if (current.IsWorldBoard)
            {
                RuntimeUi.AddButton(body, "Open campaign adventure board 084",
                    "OPEN EXPEDITION CARDS",
                    () =>
                    {
                        _guildCityTab017D = "WORLD GATE";
                        BuildCurrentScreen();
                    }, 132f, RuntimeUi.Accent);
                return;
            }
            if (current.RequiresCertifiedBattle)
            {
                if (state.Status == "AwaitingBattle")
                {
                    if (state.BattleInProgress)
                        RuntimeUi.AddButton(body, "Return to campaign battle 084",
                            "RETURN TO THE UNION BATTLE",
                            () => Navigate(M1Screen.Battle),
                            126f, RuntimeUi.Warning);
                    else if (state.AwaitingBattleRewardClaim)
                        RuntimeUi.AddButton(body, "Open campaign battle results 084",
                            "OPEN BATTLE RESULTS & CLAIM REWARD",
                            () => Navigate(M1Screen.Battle),
                            126f, RuntimeUi.Positive);
                    else
                        RuntimeUi.AddButton(body, "Commit campaign battle tile 084",
                            "COLLECT CLAIMED BATTLE RESULT",
                            () => CommitAndRevealCampaignStep084(
                                coordinator, true),
                            126f, RuntimeUi.Positive);
                }
                else
                    RuntimeUi.AddButton(body, "Enter campaign battle tile 084",
                        current.ActionLabel,
                        () => EnterPlayableCampaignBattle020(coordinator),
                        126f, RuntimeUi.Warning);
                return;
            }
            RuntimeUi.AddButton(body, "Move forward campaign room 084",
                "TURN STORY CARD",
                () => CommitAndRevealCampaignStep084(coordinator, false),
                126f, RuntimeUi.Accent);
        }

        void CommitAndRevealCampaignStep084(
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            bool claimedBattleResult)
        {
            if (_campaignRoomCommandRunning084) return;
            _campaignRoomCommandRunning084 = true;
            M1CommandResult result;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                result = coordinator == null
                    ? M1CommandResult.Failure("The story quest is unavailable.")
                    : claimedBattleResult
                        ? coordinator.CommitPlayableBattleStepResult020()
                        : coordinator.CommitPlayableStep020("SUCCESS");
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _campaignRoomCommandRunning084 = false;
            }
            _localStatus = result?.Succeeded == true
                ? string.Empty
                : result?.Message ?? "The next room could not be saved.";
            _localStatusPositive = result?.Succeeded == true;
            BuildCurrentScreen();
        }

        void ScheduleCampaignStepReceiptApply084(
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            string receiptId)
        {
            if (!Application.isPlaying || coordinator == null ||
                string.IsNullOrWhiteSpace(receiptId) ||
                StringComparer.Ordinal.Equals(_campaignCardRetryReceipt129, receiptId) ||
                !_scheduledCampaignStepReceipts084.Add(receiptId)) return;
            var routine = StartCoroutine(ApplyCampaignStepAfterReveal084(coordinator, receiptId));
            BindCampaignCardRoutine129(receiptId, routine, false);
        }

        IEnumerator ApplyCampaignStepAfterReveal084(
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            string expectedReceiptId)
        {
            yield return new WaitForSecondsRealtime(
                (_reducedMotion ? 0f : BoardAdventureCardFlipDuration084) +
                CampaignRoomResultHold084);
            var latest = coordinator?.CampaignPlayable020;
            if (latest == null || !StringComparer.Ordinal.Equals(
                    latest.PendingStepReceiptId, expectedReceiptId))
            {
                DetachCampaignCardRoutine129(expectedReceiptId, false);
                yield break;
            }
            DetachCampaignCardRoutine129(expectedReceiptId, false);

            M1CommandResult applied;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                applied = coordinator.ApplyPlayableStep020();
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
            }
            _campaignCardRetryReceipt129 = applied?.Succeeded == true ? string.Empty : expectedReceiptId;
            _localStatus = applied?.Succeeded == true
                ? string.Empty
                : applied?.Message ?? "The saved room result could not be applied.";
            _localStatusPositive = applied?.Succeeded == true;
            BuildCurrentScreen();
        }

        void CommitCampaignChapterReturn084(
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator)
        {
            if (_campaignRoomCommandRunning084) return;
            _campaignRoomCommandRunning084 = true;
            M1CommandResult result;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                result = coordinator?.FinalizePlayableChapter020() ??
                         M1CommandResult.Failure(
                             "The chapter return is unavailable.");
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _campaignRoomCommandRunning084 = false;
            }
            var campaign019 = coordinator as
                Campaign019.ICampaignPresentationCoordinator019;
            var receiptId = campaign019?.Campaign019?.PendingReceiptId ??
                            string.Empty;
            if (result?.Succeeded == true && !string.IsNullOrWhiteSpace(receiptId))
                _armedCampaignChapterReceipts084.Add(receiptId);
            _localStatus = result?.Succeeded == true &&
                           !string.IsNullOrWhiteSpace(receiptId)
                ? string.Empty
                : result?.Message ?? "The return-home result could not be saved.";
            _localStatusPositive = result?.Succeeded == true &&
                                   !string.IsNullOrWhiteSpace(receiptId);
            BuildCurrentScreen();
        }

        void ScheduleCampaignChapterReceiptApply084(
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            string receiptId)
        {
            if (!Application.isPlaying || coordinator == null ||
                string.IsNullOrWhiteSpace(receiptId) ||
                StringComparer.Ordinal.Equals(_campaignCardRetryReceipt129, receiptId) ||
                !_scheduledCampaignChapterReceipts084.Add(receiptId)) return;
            var routine = StartCoroutine(ApplyCampaignChapterAfterReveal084(coordinator, receiptId));
            BindCampaignCardRoutine129(receiptId, routine, true);
        }

        IEnumerator ApplyCampaignChapterAfterReveal084(
            Campaign020.ICampaignPlayablePresentationCoordinator020 coordinator,
            string expectedReceiptId)
        {
            yield return new WaitForSecondsRealtime(
                (_reducedMotion ? 0f : BoardAdventureCardFlipDuration084) +
                CampaignRoomResultHold084);
            var campaign019 = coordinator as
                Campaign019.ICampaignPresentationCoordinator019;
            if (!StringComparer.Ordinal.Equals(
                    campaign019?.Campaign019?.PendingReceiptId,
                    expectedReceiptId))
            {
                DetachCampaignCardRoutine129(expectedReceiptId, true);
                yield break;
            }
            DetachCampaignCardRoutine129(expectedReceiptId, true);

            M1CommandResult applied;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                applied = coordinator.ApplyPlayableChapterResult020();
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
            }
            _campaignCardRetryReceipt129 = applied?.Succeeded == true ? string.Empty : expectedReceiptId;
            _localStatus = applied?.Succeeded == true
                ? string.Empty
                : applied?.Message ?? "The chapter result could not be applied.";
            _localStatusPositive = applied?.Succeeded == true;
            BuildCurrentScreen();
        }

        static bool IsCampaignSetback084(string outcome) =>
            !string.IsNullOrWhiteSpace(outcome) &&
            (outcome.IndexOf("DEFEAT", StringComparison.OrdinalIgnoreCase) >= 0 ||
             outcome.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
             outcome.IndexOf("SETBACK", StringComparison.OrdinalIgnoreCase) >= 0);

        static string CampaignCurrentTask084(
            Campaign020.CampaignPlayablePresentationState020 state,
            Campaign020.CampaignStepView020 current)
        {
            if (state?.Status == "ReadyToFinalize")
                return "Return to the Guild and complete the chapter.";
            if (current?.RequiresCertifiedBattle == true)
                return "Enter the Union battle and claim its result.";
            if (current?.IsWorldBoard == true)
                return "Open the mission board and finish its rooms.";
            if (!string.IsNullOrWhiteSpace(state?.PendingStepReceiptId))
                return "Watch the room reveal and saved result.";
            return "Move forward and flip the next room.";
        }

        static string CompactCampaignCopy084(
            string value,
            int maximumLength) =>
            CompactCampaignCopy084(value, "Story progress", maximumLength);

        static string CompactCampaignCopy084(
            string value,
            string fallback,
            int maximumLength) =>
            CompactBoardQuestCopy081(
                string.IsNullOrWhiteSpace(value) ? fallback : value,
                maximumLength);

        void BuildCampaignAdventureTrack084(
            Transform body,
            Campaign020.CampaignPlayablePresentationState020 state)
        {
            var steps = (state.Steps ??
                         Array.Empty<Campaign020.CampaignStepView020>()).ToArray();
            var currentIndex = Array.FindIndex(steps,
                value => value.Status == "CURRENT");
            if (currentIndex < 0)
                currentIndex = Math.Max(0, Math.Min(steps.Length - 1,
                    state.CurrentStepIndex));
            const int visibleRooms = 5;
            var windowStart = Math.Max(0, Math.Min(currentIndex - 2,
                Math.Max(0, steps.Length - visibleRooms)));
            var visibleSteps = steps.Skip(windowStart).Take(visibleRooms).ToArray();

            var track = RuntimeUi.AddPanel(body, "Campaign Compact Progress Strip 084",
                new Color(0.006f, 0.016f, 0.028f, 0.96f));
            RuntimeUi.SetLayout(track, preferredHeight: 1f);
            M1PremiumUi.StylePanel(track, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(track.transform,
                new RectOffset(10, 10, 8, 10), 7f, TextAnchor.UpperCenter);
            var heading = RuntimeUi.AddText(track.transform,
                "Campaign Compact Progress Heading 084",
                "QUEST PATH  •  ROOM " +
                Math.Min(Math.Max(1, steps.Length), Math.Max(1, currentIndex + 1)) +
                " OF " + Math.Max(1, steps.Length) +
                "  •  ONE TAP = ONE ROOM",
                20, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 38f);
            ConfigureResponsiveText062(heading, 14, 21);
            var row = AddRow(track.transform,
                "Campaign Compact Progress Row 084", 8f, 106f);
            foreach (var step in visibleSteps)
            {
                var stepIndex = Array.IndexOf(steps, step);
                var current = step.Status == "CURRENT";
                var complete = step.Status == "COMPLETED";
                var currentRevealed = current &&
                    (!string.IsNullOrWhiteSpace(state.PendingStepReceiptId) ||
                     step.RequiresCertifiedBattle || step.IsWorldBoard ||
                     state.Status == "ReadyToFinalize");
                var panel = RuntimeUi.AddPanel(row,
                    "Campaign Compact Room " + (stepIndex + 1) + " 084", Color.white);
                RuntimeUi.SetLayout(panel, preferredHeight: 96f, flexibleWidth: 1f);
                M1PremiumUi.StylePanel(panel,
                    current ? M1PremiumUi.Surface.Warning :
                    complete ? M1PremiumUi.Surface.Positive : M1PremiumUi.Surface.WorldGlass);
                var text = RuntimeUi.AddText(panel.transform,
                    "Campaign Compact Room Copy " + (stepIndex + 1) + " 084",
                    complete ? "✓  ROOM " + (stepIndex + 1) + "\nCLEARED" :
                    current ? "●  YOUR PAWN\nROOM " + (stepIndex + 1) + "\n" +
                              (currentRevealed
                                  ? CompactCampaignCopy084(step.TileType, 28)
                                  : "FACE DOWN") :
                    "◇  ROOM " + (stepIndex + 1) + "\nFACE DOWN",
                    17, TextAnchor.MiddleCenter,
                    current ? RuntimeUi.Warning :
                    complete ? RuntimeUi.Positive : RuntimeUi.MutedText,
                    FontStyle.Bold);
                Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(6f, 4f);
                text.rectTransform.offsetMax = new Vector2(-6f, -4f);
                ConfigureAuthoredCompactText076(text, 12, 18);
            }
            for (var fillerIndex = visibleSteps.Length;
                 fillerIndex < visibleRooms; fillerIndex++)
            {
                var filler = RuntimeUi.AddPanel(row,
                    "Campaign Compact Empty Room " + fillerIndex + " 084",
                    Color.clear);
                RuntimeUi.SetLayout(filler, preferredHeight: 96f, flexibleWidth: 1f);
                filler.raycastTarget = false;
            }
            UseContentDrivenBoardPanelHeight084(track.transform);
        }
    }
}
