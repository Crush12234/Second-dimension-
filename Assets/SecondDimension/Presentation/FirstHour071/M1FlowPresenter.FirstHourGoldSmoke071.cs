using System;
using System.Linq;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private static bool FirstHourGoldSmokeRequested071() =>
            Environment.GetCommandLineArgs().Any(value =>
                StringComparer.Ordinal.Equals(value, "--sd-first-hour-gold-smoke"));

        /// <summary>
        /// Explicit built-player evidence seam. It is unavailable during normal play
        /// and presents the same shipping screens/components used by player input.
        /// </summary>
        public void ShowFirstHourGoldSmokeScreen071(M1Screen screen)
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _screen = screen;
            if (screen == M1Screen.GuildOperations)
            {
                _guildCityTab017D = "HALL";
                _guildCityMoreOpen060 = false;
            }
            BuildCurrentScreen();
        }

        public void ShowFirstHourGoldMarket071()
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _firstHourCharterRoute071 = true;
            _screen = M1Screen.FirstHourOpening;
            BuildCurrentScreen();
        }

        public void ShowFirstHourGoldExpedition071()
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "EXPEDITION";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        public void ShowFirstHourGoldGuildTab071(string tab)
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = string.IsNullOrWhiteSpace(tab) ? "HALL" : tab.Trim().ToUpperInvariant();
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        /// <summary>
        /// Command-line-gated evidence seam that selects one exact applicant and
        /// renders the unchanged shipping Recruitment Desk.
        /// </summary>
        public void ShowFirstHourGoldRecruitmentApplicant089(string recruitId)
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "APPLICANTS";
            _guildCityMoreOpen060 = false;
            _guildApplicantSelectedId066 = recruitId ?? string.Empty;
            BuildCurrentScreen();
        }

        /// <summary>
        /// Built-player evidence seam for the shipping C023 board presenter. The
        /// supplied state is a read-only projection of authored catalog content;
        /// normal play can never enter through this command-line-gated path.
        /// </summary>
        public void ShowFirstHourGoldAdventureBoard084(
            ICampaignWorldGatePresentationCoordinator023 coordinator,
            CampaignWorldGatePresentationState023 state)
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            if (state == null) throw new ArgumentNullException(nameof(state));
            CloseOuterGateworks066();
            CloseVersion69Experiences069();
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "WORLD GATE";
            _guildCityMoreOpen060 = false;
            var body = CreatePage(
                GuildPageTitle063(),
                GuildPageSubtitle063(),
                BackFromGuildPage063);
            BuildGuildCityWorldGate023(body, coordinator, state);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Keeps the authored 10-12 room track inside the captured viewport while
        /// retaining the production ScrollRect and layout hierarchy.
        /// </summary>
        public void FocusFirstHourGoldAdventureTrack084()
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            var target = GameObject.Find("Adventure Compact Progress Strip 084")
                ?.GetComponent<RectTransform>();
            FocusFirstHourGoldSmokeTarget084(target, "Adventure Board room strip");
        }

        public void FocusFirstHourGoldAdventureMissionBrief084()
        {
            if (!FirstHourGoldSmokeRequested071())
                throw new InvalidOperationException(
                    "First Hour Gold smoke presentation requires its explicit command-line flag.");
            var target = FindObjectsByType<RectTransform>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value => value != null &&
                    value.gameObject.activeInHierarchy &&
                    (value.name.StartsWith("STORY QUEST  •  ",
                         StringComparison.Ordinal) ||
                     value.name.StartsWith("GUILD CONTRACT  •  ",
                         StringComparison.Ordinal) ||
                     value.name.StartsWith("WORLD CRISIS  •  ",
                         StringComparison.Ordinal)));
            FocusFirstHourGoldSmokeTarget084(target, "Adventure Board mission brief");
        }

        private void FocusFirstHourGoldSmokeTarget084(
            RectTransform target,
            string label)
        {
            if (_activeScroll == null || _activeScroll.content == null ||
                _activeScroll.viewport == null || target == null)
                throw new InvalidOperationException(
                    "The shipping " + label + " is unavailable for smoke capture.");

            Canvas.ForceUpdateCanvases();
            var contentHeight = _activeScroll.content.rect.height;
            var viewportHeight = _activeScroll.viewport.rect.height;
            var overflow = Mathf.Max(0f, contentHeight - viewportHeight);
            if (overflow <= 0.5f)
            {
                _activeScroll.verticalNormalizedPosition = 1f;
                return;
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _activeScroll.content, target);
            var targetCenterFromTop = -bounds.center.y;
            var desiredOffset = Mathf.Clamp(
                targetCenterFromTop - viewportHeight * 0.5f,
                0f,
                overflow);
            _activeScroll.verticalNormalizedPosition = 1f - desiredOffset / overflow;
            Canvas.ForceUpdateCanvases();
        }
    }
}
