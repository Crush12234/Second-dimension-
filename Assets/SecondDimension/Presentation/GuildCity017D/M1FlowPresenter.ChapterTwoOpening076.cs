using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public enum ChapterTwoOpeningStage076
    {
        Unavailable,
        WayglassBriefing,
        RouteDecision,
        RouteCommitted
    }

    public sealed partial class M1FlowPresenter
    {
        public const string ChapterTwoOpeningRootName076 = "Chapter Two Wayglass Opening 076";
        public const string ChapterTwoArcTitle076 = "THE DOOR INSIDE";
        public const string ChapterTwoOperationTitle076 = "THE LINES NOT RETURNED";
        public const string ChapterTwoFreshMarksRouteTitle076 = "THE FRESH MARKS";
        public const string ChapterTwoBrokenBridgeRouteTitle076 = "THE BROKEN BRIDGE";
        public const string ChapterTwoKiriKeyArtResource076 =
            "SecondDimension/Art/Portraits/Recruits/CANON_KIRI_AETHERHEART";
        public const string ChapterTwoNorthRouteNodeId076 = "N02";
        public const string ChapterTwoUnderhallRouteNodeId076 = "N04";
        public const string ChapterTwoNorthRouteAction076 = "TRUST THE FRESH MARKS";
        public const string ChapterTwoUnderhallRouteAction076 = "CROSS THE SURVEY BRIDGE";
        public const string ChapterTwoThresholdAction078 = "DESCEND WITH KIRI";
        public const float ChapterTwoRouteCardNormalizedHeight076 = 0.325f;
        public const float ChapterTwoRouteButtonLocalHeight076 = 0.320f;
        public const float ChapterTwoKiriVerticalFocus076 = 0.91f;

        private static readonly Rect[] ChapterTwoDecisionRegions076 =
        {
            new Rect(0.025f, 0.855f, 0.950f, 0.120f),
            new Rect(0.035f, 0.155f, 0.505f, 0.395f),
            new Rect(0.565f, 0.510f, 0.410f, ChapterTwoRouteCardNormalizedHeight076),
            new Rect(0.565f, 0.165f, 0.410f, ChapterTwoRouteCardNormalizedHeight076),
            new Rect(0.025f, 0.035f, 0.190f, 0.105f),
            new Rect(0.225f, 0.035f, 0.315f, 0.105f),
            new Rect(0.565f, 0.035f, 0.410f, 0.105f)
        };

        private static readonly IReadOnlyList<Rect> ChapterTwoDecisionRegionsReadOnly076 =
            Array.AsReadOnly(ChapterTwoDecisionRegions076);

        private bool _returnToChapterTwoAfterUnionRepair076;

        public static IReadOnlyList<Rect> ChapterTwoDecisionRegionsForVerification076 =>
            ChapterTwoDecisionRegionsReadOnly076;

        public static float ChapterTwoRouteButtonHeightForVerification076(
            float screenWidth,
            float screenHeight)
        {
            var canvas = ExpeditionCanvasSizeForVerification074(screenWidth, screenHeight);
            var screenRootHeight = Mathf.Max(0f, canvas.y - 84f);
            return screenRootHeight * ChapterTwoRouteCardNormalizedHeight076 *
                   ChapterTwoRouteButtonLocalHeight076;
        }

        public static ChapterTwoOpeningStage076 ChapterTwoOpeningStageForVerification076(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            if (state == null ||
                !IsStoryContractActive065(state, SecondStoryContractId065))
                return ChapterTwoOpeningStage076.Unavailable;
            if (state.Expedition == null)
                return ChapterTwoOpeningStage076.WayglassBriefing;
            if (!StringComparer.Ordinal.Equals(
                    state.Expedition.BoardId,
                    GuildCityExpeditionService017D.SecondStoryBoardId076))
                return ChapterTwoOpeningStage076.RouteCommitted;
            if (StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeId, "N00"))
                return ChapterTwoOpeningStage076.WayglassBriefing;
            if (StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeId, "N01"))
                return ChapterTwoOpeningStage076.RouteDecision;
            return ChapterTwoOpeningStage076.RouteCommitted;
        }

        private static bool ShouldShowChapterTwoOpening076(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var stage = ChapterTwoOpeningStageForVerification076(state);
            return stage == ChapterTwoOpeningStage076.WayglassBriefing;
        }

        private bool NeedsChapterTwoUnionRepair076(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var stage = ChapterTwoOpeningStageForVerification076(state);
            var routeStillUncommitted =
                stage == ChapterTwoOpeningStage076.WayglassBriefing ||
                stage == ChapterTwoOpeningStage076.RouteDecision;
            return routeStillUncommitted &&
                   (_coordinator?.State?.OpeningUnionsLegal != true ||
                    (state?.NormalUnionCount ?? 0) <= 0);
        }

        private void OpenChapterTwoUnionRepair076()
        {
            _returnToExpeditionAfterUnionReview076 = false;
            _returnToChapterTwoAfterUnionRepair076 = true;
            _guildCityTab017D = "CHAPTER2";
            Navigate(M1Screen.UnionBuilder);
        }

        /// <summary>
        /// Authored Chapter 2 handoff. It replaces the old accept -> party ->
        /// generic-board chain with one story screen and one clear descent action.
        /// The threshold command commits through GuildCity authority and therefore
        /// survives reload before the ordinary operation board takes over.
        /// </summary>
        private void BuildChapterTwoOpening076(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activeContent = null;
            _activeScroll = null;

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                ChapterTwoOpeningRootName076,
                new Color(0.006f, 0.012f, 0.021f, 1f));
            Stretch(root.rectTransform);
            _activePage = root.rectTransform;

            BuildChapterTwoKeyArt076(root.transform);

            var worldShade = RuntimeUi.AddPanel(
                root.transform,
                "Chapter Two Readability Shade 076",
                new Color(0.002f, 0.007f, 0.014f, _highContrast ? 0.45f : 0.24f));
            Stretch(worldShade.rectTransform);
            worldShade.raycastTarget = false;

            BuildChapterTwoHeader076(root.transform, state);
            BuildChapterTwoStoryBrief076(root.transform, state);
            var unionPlansLegal076 = _coordinator?.State?.OpeningUnionsLegal == true &&
                                     (state?.NormalUnionCount ?? 0) > 0;
            var threshold078 = AddChapterTwoRouteCard076(
                root.transform,
                new Rect(0.565f, 0.165f, 0.410f, 0.670f),
                "THE WAYGLASS THRESHOLD",
                "Kiri will lead the descent. A frightened survey apprentice is waiting below with the first real evidence—and the first name of someone still missing.",
                "DIPLOMACY / MEDICINE",
                "FALSE MARKS",
                ChapterTwoThresholdAction078,
                () => EnterChapterTwoThreshold078(coordinator),
                RuntimeUi.Warning);
            threshold078.interactable = unionPlansLegal076;

            var hall = RuntimeUi.AddButton(
                root.transform,
                "Chapter Two Return To Hall 076",
                "←  GUILD HALL",
                () =>
                {
                    _guildCityTab017D = "HALL";
                    BuildCurrentScreen();
                },
                RuntimeUi.MinimumTouchPixels,
                new Color(0.025f, 0.050f, 0.064f, 0.97f));
            AnchorChapterTwo076(
                hall.GetComponent<RectTransform>(),
                ChapterTwoDecisionRegions076[4]);
            var hallLabel076 = hall.GetComponentInChildren<Text>();
            ConfigureAuthoredCompactText076(hallLabel076, 28, 34);
            hallLabel076.color = RuntimeUi.Text;
            hallLabel076.transform.SetAsLastSibling();

            var readyUnionCount076 = Math.Max(0, state?.NormalUnionCount ?? 0);
            var party = RuntimeUi.AddButton(
                root.transform,
                "Chapter Two Review Unions 076",
                unionPlansLegal076
                    ? "REVIEW " + readyUnionCount076 + " UNION PLAN" +
                      (readyUnionCount076 == 1 ? string.Empty : "S")
                    : "FIX UNION PLANS  •  " + readyUnionCount076 + " ACTIVE",
                () =>
                {
                    if (unionPlansLegal076)
                    {
                        _guildCityTab017D = "PARTY";
                        BuildCurrentScreen();
                    }
                    else
                    {
                        OpenChapterTwoUnionRepair076();
                    }
                },
                RuntimeUi.MinimumTouchPixels,
                unionPlansLegal076
                    ? new Color(0.025f, 0.050f, 0.064f, 0.97f)
                    : RuntimeUi.Warning);
            AnchorChapterTwo076(
                party.GetComponent<RectTransform>(),
                ChapterTwoDecisionRegions076[5]);
            ConfigureAuthoredCompactText076(party.GetComponentInChildren<Text>(), 28, 34);
            if (!unionPlansLegal076) ConfigureChapterTwoGoldButton076(party);

            var saveNotice = RuntimeUi.AddPanel(
                root.transform,
                "Chapter Two Save Consequence 076",
                new Color(0.018f, 0.047f, 0.056f, 0.97f));
            AnchorChapterTwo076(saveNotice.rectTransform, ChapterTwoDecisionRegions076[6]);
            M1PremiumUi.StylePanel(saveNotice, M1PremiumUi.Surface.WorldRibbon);
            var saveCopy = RuntimeUi.AddText(
                saveNotice.transform,
                "Chapter Two Save Consequence Copy 076",
                unionPlansLegal076
                    ? "OPERATION 2 STARTS AT THE THRESHOLD  •  THE APPRENTICE, EVIDENCE, AND EVERY ROOM RESULT SAVE"
                    : "UNIONS NOT READY  •  REVIEW PARTY BEFORE DESCENDING WITH KIRI",
                20,
                TextAnchor.MiddleCenter,
                unionPlansLegal076 ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            Stretch(saveCopy.rectTransform);
            saveCopy.rectTransform.offsetMin = new Vector2(14f, 4f);
            saveCopy.rectTransform.offsetMax = new Vector2(-14f, -4f);
            saveCopy.raycastTarget = false;
            ConfigureAuthoredCompactText076(saveCopy, 28, 34);

            if (unionPlansLegal076) threshold078.Select();
            else party.Select();
        }

        private static void BuildChapterTwoKeyArt076(Transform parent)
        {
            var viewport = RuntimeUi.AddPanel(
                parent,
                "Chapter Two Kiri Art Viewport 076",
                new Color(0.02f, 0.03f, 0.04f, 1f));
            AnchorChapterTwo076(viewport.rectTransform, new Rect(0f, 0f, 0.565f, 1f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var artwork = RuntimeUi.AddPanel(
                viewport.transform,
                "Chapter Two Kiri Wayglass Key Art 076",
                Color.white);
            Stretch(artwork.rectTransform);
            // EnvelopeParent is intentionally cinematic, but a centered crop cuts
            // Kiri's face at both production aspects. Bias the driven crop upward;
            // the lower legs may leave frame while her face and Wayglass remain.
            artwork.rectTransform.pivot =
                new Vector2(0.5f, ChapterTwoKiriVerticalFocus076);
            artwork.raycastTarget = false;
            artwork.sprite = ResolveVisualSliceSprite062(ChapterTwoKiriKeyArtResource076);
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = false;
            if (artwork.sprite == null) return;
            var fitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = artwork.sprite.rect.width /
                                 Mathf.Max(1f, artwork.sprite.rect.height);
        }

        private void BuildChapterTwoHeader076(
            Transform parent,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var header = RuntimeUi.AddPanel(
                parent,
                "Chapter Two Opening Header 076",
                new Color(0.015f, 0.033f, 0.045f, 0.97f));
            AnchorChapterTwo076(header.rectTransform, ChapterTwoDecisionRegions076[0]);
            M1PremiumUi.StylePanel(header, M1PremiumUi.Surface.WorldRibbon);

            var title = RuntimeUi.AddText(
                header.transform,
                "Chapter Two Opening Title 076",
                "CHAPTER 2  •  " + ChapterTwoArcTitle076,
                31,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorChapterTwo076(title.rectTransform, new Rect(0.025f, 0.58f, 0.64f, 0.34f));
            ConfigureAuthoredCompactText076(title, 32, 42);

            var subtitle = RuntimeUi.AddText(
                header.transform,
                "Chapter Two Opening Subtitle 076",
                "OPERATION " + Math.Max(2, (state?.OperationOrdinal ?? 1) + 1) +
                "  •  " + ChapterTwoOperationTitle076,
                19,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorChapterTwo076(subtitle.rectTransform, new Rect(0.025f, 0.06f, 0.70f, 0.48f));
            ConfigureAuthoredCompactText076(subtitle, 28, 34);

            var readyUnionCount076 = Math.Max(0, state?.NormalUnionCount ?? 0);
            var unionPlansLegal076 = _coordinator?.State?.OpeningUnionsLegal == true &&
                                     readyUnionCount076 > 0;
            var operation = RuntimeUi.AddText(
                header.transform,
                "Chapter Two Opening Operation Status 076",
                readyUnionCount076 +
                (unionPlansLegal076
                    ? readyUnionCount076 == 1 ? " UNION READY" : " UNIONS READY"
                    : readyUnionCount076 == 1 ? " UNION — FIX PARTY" : " UNIONS — FIX PARTY"),
                19,
                TextAnchor.MiddleRight,
                unionPlansLegal076 ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorChapterTwo076(operation.rectTransform, new Rect(0.74f, 0.14f, 0.235f, 0.72f));
            ConfigureAuthoredCompactText076(operation, 28, 34);
        }

        private void BuildChapterTwoStoryBrief076(
            Transform parent,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var brief = RuntimeUi.AddPanel(
                parent,
                "Chapter Two Story Brief 076",
                new Color(0.010f, 0.021f, 0.031f, 0.94f));
            AnchorChapterTwo076(brief.rectTransform, ChapterTwoDecisionRegions076[1]);
            M1PremiumUi.StylePanel(brief, M1PremiumUi.Surface.WorldPaper);

            var scene = RuntimeUi.AddText(
                brief.transform,
                "Chapter Two Scene Heading 076",
                "THE HALL TABLE  •  MOMENTS AFTER THE PATROL RETURNS",
                20,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorChapterTwo076(scene.rectTransform, new Rect(0.045f, 0.815f, 0.91f, 0.140f));
            ConfigureAuthoredCompactText076(scene, 28, 32);

            var quote = RuntimeUi.AddText(
                brief.transform,
                "Chapter Two Kiri Dialogue 076",
                "KIRI AETHERHEART\n“The Wayglass shows a road inside Skyhome. Something below is changing it.”",
                25,
                TextAnchor.UpperLeft,
                RuntimeUi.Warning,
                FontStyle.Bold);
            // Keep a clean one-percent gutter above the stakes and below the scene
            // heading while giving Kiri's two-line reveal measured 1280x800 headroom.
            AnchorChapterTwo076(quote.rectTransform, new Rect(0.045f, 0.575f, 0.91f, 0.230f));
            ConfigureAuthoredCompactText076(quote, 28, 34);
            quote.verticalOverflow = VerticalWrapMode.Truncate;

            var stakes = RuntimeUi.AddText(
                brief.transform,
                "Chapter Two Stakes 076",
                "SITUATION  •  THE WAYGLASS POINTS BENEATH YOUR GUILD HALL. One survey apprentice made it back to the threshold; the rest of the crew is silent.\nYOUR OBJECTIVE  •  Hear the apprentice, recover the crew and instruments, then follow evidence to the unrecorded door.",
                20,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Normal);
            AnchorChapterTwo076(stakes.rectTransform, new Rect(0.045f, 0.205f, 0.91f, 0.360f));
            ConfigureAuthoredCompactText076(stakes, 28, 32);
            stakes.verticalOverflow = VerticalWrapMode.Truncate;

            var command = RuntimeUi.AddPanel(
                brief.transform,
                "Chapter Two First Command 076",
                new Color(0.045f, 0.095f, 0.105f, 0.96f));
            AnchorChapterTwo076(command.rectTransform, new Rect(0.045f, 0.025f, 0.91f, 0.160f));
            M1PremiumUi.StylePanel(command, M1PremiumUi.Surface.WorldGlass);
            var commandText = RuntimeUi.AddText(
                command.transform,
                "Chapter Two First Command Copy 076",
                state?.Expedition == null
                    ? "FIRST ORDER  •  DESCEND WITH KIRI AND FIND OUT WHO IS STILL ALIVE"
                    : "WAYGLASS OPEN  •  MEET THE APPRENTICE BEFORE THE NEXT ROOM FLIPS",
                20,
                TextAnchor.MiddleCenter,
                RuntimeUi.Positive,
                FontStyle.Bold);
            Stretch(commandText.rectTransform);
            // Recover four logical pixels of vertical room while preserving the
            // inset, keeping the two-line first order above the 1280x800 fit threshold.
            commandText.rectTransform.offsetMin = new Vector2(14f, 3f);
            commandText.rectTransform.offsetMax = new Vector2(-14f, -3f);
            ConfigureAuthoredCompactText076(commandText, 28, 34);
        }

        private Button AddChapterTwoRouteCard076(
            Transform parent,
            Rect region,
            string heading,
            string copy,
            string firstTest,
            string riskLabel,
            string actionLabel,
            Action action,
            Color color)
        {
            var card = RuntimeUi.AddPanel(
                parent,
                "Chapter Two Route " + actionLabel + " 076",
                new Color(0.012f, 0.025f, 0.036f, 0.97f));
            AnchorChapterTwo076(card.rectTransform, region);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldRibbon);

            var title = RuntimeUi.AddText(
                card.transform,
                "Chapter Two Route Heading " + actionLabel + " 076",
                heading,
                21,
                TextAnchor.MiddleLeft,
                color,
                FontStyle.Bold);
            AnchorChapterTwo076(title.rectTransform, new Rect(0.045f, 0.785f, 0.91f, 0.165f));
            ConfigureAuthoredCompactText076(title, 28, 34);

            var story = RuntimeUi.AddText(
                card.transform,
                "Chapter Two Route Story " + actionLabel + " 076",
                copy,
                18,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Normal);
            AnchorChapterTwo076(story.rectTransform, new Rect(0.045f, 0.555f, 0.91f, 0.210f));
            ConfigureAuthoredCompactText076(story, 28, 32);
            story.verticalOverflow = VerticalWrapMode.Truncate;

            var test = RuntimeUi.AddText(
                card.transform,
                "Chapter Two Route Test " + actionLabel + " 076",
                "FIRST TEST  •  " + firstTest,
                30,
                TextAnchor.MiddleLeft,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            AnchorChapterTwo076(test.rectTransform, new Rect(0.045f, 0.365f, 0.585f, 0.170f));
            ConfigureAuthoredCompactText076(test, 28, 32);

            var risk = RuntimeUi.AddText(
                card.transform,
                "Chapter Two Route Risk " + actionLabel + " 076",
                "RISK  •  " + riskLabel,
                30,
                TextAnchor.MiddleRight,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            AnchorChapterTwo076(risk.rectTransform, new Rect(0.645f, 0.365f, 0.310f, 0.170f));
            ConfigureAuthoredCompactText076(risk, 28, 32);

            var button = RuntimeUi.AddButton(
                card.transform,
                "Chapter Two Route Action " + actionLabel + " 076",
                actionLabel + "  →",
                action,
                RuntimeUi.MinimumTouchPixels,
                color);
            AnchorChapterTwo076(
                button.GetComponent<RectTransform>(),
                new Rect(0.045f, 0.025f, 0.91f, ChapterTwoRouteButtonLocalHeight076));
            ConfigureAuthoredCompactText076(button.GetComponentInChildren<Text>(), 28, 34);
            if (color == RuntimeUi.Warning || color == RuntimeUi.Accent)
                ConfigureChapterTwoGoldButton076(button);
            return button;
        }

        private static void ConfigureChapterTwoGoldButton076(Button button)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.color = new Color(0.07f, 0.08f, 0.09f, 1f);
            var colors = button.colors;
            colors.normalColor = RuntimeUi.Warning;
            colors.highlightedColor = RuntimeUi.Accent;
            colors.selectedColor = RuntimeUi.Warning;
            colors.pressedColor = RuntimeUi.Accent;
            button.colors = colors;
        }

        private void EnterChapterTwoThreshold078(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if (coordinator == null)
            {
                _localStatusPositive = false;
                _localStatus = "The Guild record is unavailable. The descent was not started.";
                BuildCurrentScreen();
                return;
            }

            if (_coordinator?.State?.OpeningUnionsLegal != true ||
                (coordinator.GuildCity017D?.NormalUnionCount ?? 0) <= 0)
            {
                _localStatusPositive = false;
                _localStatus = "Union plans are not ready. Repair the party before descending with Kiri.";
                OpenChapterTwoUnionRepair076();
                return;
            }

            if (coordinator.GuildCity017D?.Expedition == null)
            {
                var begun = coordinator.StartGuildCityExpedition017D();
                if (begun == null || !begun.Succeeded)
                {
                    _localStatusPositive = false;
                    _localStatus = begun?.Message ??
                                   "The Wayglass threshold could not be opened. No progress was lost.";
                    BuildCurrentScreen();
                    return;
                }
            }

            _localStatusPositive = true;
            _localStatus = "Kiri leads the descent. Hear the apprentice, then flip the next room.";
            _guildCityTab017D = "EXPEDITION";
            BuildCurrentScreen();
        }

        private void CommitChapterTwoRoute076(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string destinationNodeId)
        {
            if (coordinator == null)
            {
                _localStatusPositive = false;
                _localStatus = "The Guild record is unavailable. No room result was changed.";
                BuildCurrentScreen();
                return;
            }

            if (_coordinator?.State?.OpeningUnionsLegal != true ||
                (coordinator.GuildCity017D?.NormalUnionCount ?? 0) <= 0)
            {
                _localStatusPositive = false;
                _localStatus = "Union plans are not ready. Repair the party before beginning the Wayglass descent.";
                OpenChapterTwoUnionRepair076();
                return;
            }

            var state = coordinator.GuildCity017D;
            if (state?.Expedition == null)
            {
                var begun = coordinator.StartGuildCityExpedition017D();
                if (begun == null || !begun.Succeeded)
                {
                    _localStatusPositive = false;
                    _localStatus = begun?.Message ??
                                   "The Wayglass descent could not be opened. No room result was changed.";
                    BuildCurrentScreen();
                    return;
                }
                state = coordinator.GuildCity017D;
            }

            // Release 075 saves can resume one step earlier at N00. Walk them to
            // the same authored decision without discarding the existing Guild.
            if (StringComparer.Ordinal.Equals(state?.Expedition?.CurrentNodeId, "N00"))
            {
                var threshold = coordinator.MoveGuildCityExpedition017D("N01");
                if (threshold == null || !threshold.Succeeded)
                {
                    _localStatusPositive = false;
                    _localStatus = threshold?.Message ??
                                   "Your Unions could not reach the Wayglass threshold. No route was chosen.";
                    BuildCurrentScreen();
                    return;
                }
                state = coordinator.GuildCity017D;
            }

            if (!StringComparer.Ordinal.Equals(state?.Expedition?.BoardId,
                    GuildCityExpeditionService017D.SecondStoryBoardId076) ||
                !StringComparer.Ordinal.Equals(state.Expedition.CurrentNodeId, "N01"))
            {
                _guildCityTab017D = "EXPEDITION";
                BuildCurrentScreen();
                return;
            }

            var moved = coordinator.MoveGuildCityExpedition017D(destinationNodeId);
            _localStatusPositive = moved != null && moved.Succeeded;
            _localStatus = _localStatusPositive
                ? (StringComparer.Ordinal.Equals(destinationNodeId, ChapterTwoNorthRouteNodeId076)
                    ? "Room saved: your Unions followed the fresh marks and found the first false symbol."
                    : "Room saved: your Unions crossed the broken survey bridge and secured the damaged span.")
                : moved?.Message ?? "The next room could not be saved. Try moving again.";
            if (_localStatusPositive) _guildCityTab017D = "EXPEDITION";
            BuildCurrentScreen();
        }

        private static Button FindButtonIn076(Transform root, string label)
        {
            if (root == null) return null;
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index++)
            {
                var text = buttons[index].GetComponentInChildren<Text>();
                if (text != null && text.text != null &&
                    text.text.StartsWith(label, StringComparison.Ordinal))
                    return buttons[index];
            }
            return null;
        }

        private static void AnchorChapterTwo076(RectTransform rect, Rect region)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(region.xMin, region.yMin);
            rect.anchorMax = new Vector2(region.xMax, region.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
