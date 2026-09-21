using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        public const string GuildCityWorkshopRootName078 = "Guild City Workshop 078";

        private const int GuildCityWorkshopColumns078 = 3;
        private const int GuildCityWorkshopRows078 = 4;
        private const float GuildCityWorkshopGapX078 = 0.022f;
        private const float GuildCityWorkshopGapY078 = 0.026f;

        private bool _guildCityWorkshopChoosingFacility078;
        private int _guildCityWorkshopFacilityPage078;

        private sealed class GuildCityWorkshopFacilityChoice078
        {
            public GuildCity017D.GuildCityPlotView017D Plot;
            public GuildCity017D.GuildCityBuildingView017D Building;
        }

        public static IReadOnlyList<Rect> GuildCityWorkshopPlotRectsForVerification078(
            int visibleCount)
        {
            var count = Mathf.Clamp(
                visibleCount,
                0,
                GuildCityWorkshopColumns078 * GuildCityWorkshopRows078);
            if (count == 0) return Array.Empty<Rect>();

            const float startX = 0.035f;
            const float endX = 0.965f;
            const float startY = 0.055f;
            const float endY = 0.805f;
            var width = (endX - startX - GuildCityWorkshopGapX078 *
                         (GuildCityWorkshopColumns078 - 1)) /
                        GuildCityWorkshopColumns078;
            var height = (endY - startY - GuildCityWorkshopGapY078 *
                          (GuildCityWorkshopRows078 - 1)) /
                         GuildCityWorkshopRows078;
            var result = new Rect[count];
            for (var index = 0; index < count; index++)
            {
                var column = index % GuildCityWorkshopColumns078;
                var rowFromTop = index / GuildCityWorkshopColumns078;
                result[index] = new Rect(
                    startX + column * (width + GuildCityWorkshopGapX078),
                    endY - (rowFromTop + 1) * height -
                    rowFromTop * GuildCityWorkshopGapY078,
                    width,
                    height);
            }
            return Array.AsReadOnly(result);
        }

        public static string GuildCityWorkshopQuestionForVerification078(
            GuildCity017D.GuildCityPresentationState017D state,
            bool choosingNextFacility = false)
        {
            if (state == null) return "HOW WILL THE HALL GROW?";
            if (state.PlacedBuildingCount <= 0)
                return "WHAT WILL YOUR GUILD BUILD FIRST?";
            var hasUnstaffedFacility = (state.Plots ??
                                        Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                .Any(value => value != null &&
                              !string.IsNullOrWhiteSpace(value.BuildingId) &&
                              (value.StaffRecruitIds?.Count ?? 0) == 0);
            if (hasUnstaffedFacility || state.StaffedBuildingCount < state.PlacedBuildingCount)
                return "WHO WILL MAKE THIS FACILITY MATTER?";
            if (choosingNextFacility)
                return "WHAT WILL YOUR GUILD BUILD NEXT?";
            return "WHAT CHANGED BECAUSE YOU BUILT IT?";
        }

        public static int GuildCityWorkshopFacilityChoiceCountForVerification078(
            GuildCity017D.GuildCityPresentationState017D state) =>
            GuildCityWorkshopFacilityChoices078(state).Count;

        public static bool GuildCityWorkshopCanSubmitBuildForVerification078(
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityBuildingView017D building) =>
            state != null && building != null &&
            (state.CharterBuildCredits > 0 ||
             building.HasLevelOneCost && building.CanAffordLevelOneWithResources);

        public static string GuildCityWorkshopBuildCostLabelForVerification078(
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityBuildingView017D building)
        {
            if (state?.CharterBuildCredits > 0)
                return "CHARTER CREDIT  •  NO XP OR MATERIALS SPENT";
            if (state == null || building == null || !building.HasLevelOneCost)
                return "BUILD COST UNAVAILABLE";

            var balances = new List<string>
            {
                "HALL XP " + Math.Max(0, state.HallEnhancementXp) + "/" +
                Math.Max(0, building.LevelOneHallXpCost)
            };
            foreach (var requirement in (building.LevelOneMaterialRequirements ??
                                         Array.Empty<GuildCity017D.GuildCityMaterialRequirementView017D>())
                         .Where(value => value != null)
                         .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
                         .ThenBy(value => value.MaterialId, StringComparer.Ordinal))
            {
                var name = string.IsNullOrWhiteSpace(requirement.DisplayName)
                    ? (requirement.MaterialId ?? "MATERIAL").Replace("MAT_", string.Empty).Replace('_', ' ')
                    : requirement.DisplayName;
                balances.Add(name.ToUpperInvariant() + " " +
                             Math.Max(0, requirement.AvailableAmount) + "/" +
                             Math.Max(0, requirement.RequiredAmount));
            }

            return (building.CanAffordLevelOneWithResources ? "READY" : "NEED RESOURCES") +
                   "  •  " + string.Join("  •  ", balances);
        }

        public static IReadOnlyList<string> GuildCityWorkshopStaffCandidateIdsForVerification078(
            GuildCity017D.GuildCityPresentationState017D state) =>
            FirstOperationStaffCandidates078(state)
                .Select(value => value.RecruitId)
                .ToArray();

        private void BuildGuildCityWorkshop078(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activeContent = null;
            _activeScroll = null;

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                GuildCityWorkshopRootName078,
                new Color(0.008f, 0.014f, 0.022f, 1f));
            Stretch(root.rectTransform);
            _activePage = root.rectTransform;

            BuildLivingGuildBackdrop074(root.transform);
            var shade = RuntimeUi.AddPanel(
                root.transform,
                "Guild City Workshop Readability Shade 078",
                new Color(0.004f, 0.009f, 0.015f, _highContrast ? 0.72f : 0.58f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;
            BuildLivingGuildStatusBar074(root.transform, state);

            var headingPanel = RuntimeUi.AddPanel(
                root.transform,
                "Guild City Workshop Heading Panel 078",
                new Color(0.012f, 0.026f, 0.038f, 0.97f));
            AnchorGuildCityWorkshop078(
                headingPanel.rectTransform,
                new Rect(0.055f, 0.785f, 0.89f, 0.085f));
            M1PremiumUi.StylePanel(headingPanel, M1PremiumUi.Surface.WorldRibbon);
            var heading = RuntimeUi.AddText(
                headingPanel.transform,
                "Guild City Workshop Heading 078",
                "SKYHOME SETTLEMENT  •  " +
                GuildCityWorkshopQuestionForVerification078(
                    state,
                    _guildCityWorkshopChoosingFacility078),
                28,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(
                heading.rectTransform,
                new Rect(0.025f, 0.10f, 0.95f, 0.80f));
            ConfigureResponsiveText062(heading, 18, 29);
            heading.raycastTarget = false;

            var map = RuntimeUi.AddPanel(
                root.transform,
                "Guild City Workshop Plot Map 078",
                new Color(0.010f, 0.019f, 0.028f, 0.97f));
            AnchorGuildCityWorkshop078(map.rectTransform, new Rect(0.055f, 0.135f, 0.55f, 0.625f));
            M1PremiumUi.StylePanel(map, M1PremiumUi.Surface.WorldGlass);
            BuildGuildCityWorkshopPlotMap078(map.transform, state);

            var decision = RuntimeUi.AddPanel(
                root.transform,
                "Guild City Workshop Decision 078",
                new Color(0.018f, 0.030f, 0.040f, 0.98f));
            AnchorGuildCityWorkshop078(
                decision.rectTransform,
                new Rect(0.625f, 0.135f, 0.32f, 0.625f));
            M1PremiumUi.StylePanel(decision, M1PremiumUi.Surface.WorldPaper);
            var focus = BuildGuildCityWorkshopDecision078(
                decision.transform,
                coordinator,
                state);

            var back = RuntimeUi.AddButton(
                root.transform,
                "Guild City Workshop Return To Hall 078",
                "←  GUILD HALL",
                () =>
                {
                    _guildCityWorkshopChoosingFacility078 = false;
                    _guildCityWorkshopFacilityPage078 = 0;
                    _guildCityTab017D = "HALL";
                    BuildCurrentScreen();
                },
                RuntimeUi.MinimumTouchPixels,
                new Color(0.025f, 0.050f, 0.064f, 0.97f));
            AnchorGuildCityWorkshop078(
                back.GetComponent<RectTransform>(),
                new Rect(0.055f, 0.035f, 0.22f, 0.075f));
            ConfigureResponsiveText062(back.GetComponentInChildren<Text>(), 15, 22);

            var saved = RuntimeUi.AddText(
                root.transform,
                "Guild City Workshop Save State 078",
                !_localStatusPositive && !string.IsNullOrWhiteSpace(_localStatus)
                    ? "ORDER NOT SAVED  •  " + _localStatus
                    : "CITY ORDERS SAVE IMMEDIATELY  •  NO WAIT DAY",
                18,
                TextAnchor.MiddleRight,
                !_localStatusPositive && !string.IsNullOrWhiteSpace(_localStatus)
                    ? RuntimeUi.Warning
                    : RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(saved.rectTransform, new Rect(0.30f, 0.035f, 0.645f, 0.075f));
            ConfigureResponsiveText062(saved, 13, 18);
            saved.raycastTarget = false;

            (focus ?? back).Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject((focus ?? back).gameObject);
        }

        private void BuildGuildCityWorkshopPlotMap078(
            Transform parent,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var title = RuntimeUi.AddText(
                parent,
                "Guild City Workshop Plot Map Title 078",
                "12 PLOTS  •  BUILDINGS CHANGE THE HALL AND THE PEOPLE INSIDE IT",
                19,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(title.rectTransform, new Rect(0.035f, 0.83f, 0.93f, 0.12f));
            ConfigureResponsiveText062(title, 13, 19);
            title.raycastTarget = false;

            var plots = (state?.Plots ?? Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                .Where(value => value != null)
                .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                .Take(12)
                .ToArray();
            var rects = GuildCityWorkshopPlotRectsForVerification078(plots.Length);
            for (var index = 0; index < plots.Length; index++)
            {
                var plot = plots[index];
                var occupied = !string.IsNullOrWhiteSpace(plot.BuildingId);
                var color = !plot.Unlocked
                    ? new Color(0.024f, 0.030f, 0.036f, 0.96f)
                    : occupied
                        ? new Color(0.08f, 0.18f, 0.15f, 0.98f)
                        : new Color(0.06f, 0.10f, 0.13f, 0.98f);
                var card = RuntimeUi.AddPanel(
                    parent,
                    "Guild City Workshop Plot " + plot.PlotId + " 078",
                    color);
                AnchorGuildCityWorkshop078(card.rectTransform, rects[index]);
                M1PremiumUi.StylePanel(
                    card,
                    occupied
                        ? M1PremiumUi.Surface.Positive
                        : M1PremiumUi.Surface.WorldRibbon);
                var staffName = GuildCityWorkshopStaffName078(state, plot);
                var label = RuntimeUi.AddText(
                    card.transform,
                    "Guild City Workshop Plot Label " + plot.PlotId + " 078",
                    GuildCityWorkshopPlotLabel078(plot, staffName),
                    18,
                    TextAnchor.MiddleCenter,
                    !plot.Unlocked
                        ? RuntimeUi.MutedText
                        : occupied ? RuntimeUi.Positive : RuntimeUi.Text,
                    FontStyle.Bold);
                AnchorGuildCityWorkshop078(label.rectTransform, new Rect(0.06f, 0.06f, 0.88f, 0.88f));
                ConfigureResponsiveText062(label, 12, 18);
                label.raycastTarget = false;
            }
        }

        private Button BuildGuildCityWorkshopDecision078(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var question = RuntimeUi.AddText(
                parent,
                "Guild City Workshop Current Question 078",
                GuildCityWorkshopQuestionForVerification078(
                    state,
                    _guildCityWorkshopChoosingFacility078),
                25,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(question.rectTransform, new Rect(0.06f, 0.82f, 0.88f, 0.13f));
            ConfigureResponsiveText062(question, 16, 26);
            question.raycastTarget = false;

            var placedPlots = (state.Plots ?? Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.BuildingId))
                .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                .ToArray();

            if (placedPlots.Length == 0)
            {
                _guildCityWorkshopChoosingFacility078 = false;
                return BuildGuildCityWorkshopFacilityChoices078(parent, coordinator, state);
            }

            var unstaffedPlot = placedPlots.FirstOrDefault(value =>
                (value.StaffRecruitIds?.Count ?? 0) == 0);
            if (unstaffedPlot != null)
            {
                _guildCityWorkshopChoosingFacility078 = false;
                return BuildGuildCityWorkshopStaffChoices078(
                    parent,
                    coordinator,
                    state,
                    unstaffedPlot);
            }

            if (_guildCityWorkshopChoosingFacility078)
                return BuildGuildCityWorkshopFacilityChoices078(parent, coordinator, state);

            var placedPlot = placedPlots[0];

            var building = (state.Buildings ?? Array.Empty<GuildCity017D.GuildCityBuildingView017D>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.BuildingId, placedPlot.BuildingId));
            var staff = GuildCityWorkshopStaffName078(state, placedPlot);
            var result = RuntimeUi.AddText(
                parent,
                "Guild City Workshop Working Facility 078",
                (placedPlot.BuildingName ?? "FIRST FACILITY").ToUpperInvariant() +
                "  •  LEVEL " + Math.Max(1, placedPlot.BuildingLevel) +
                "\nSTAFF  •  " + staff.ToUpperInvariant() +
                "\n\nIMMEDIATE EFFECT\n" +
                FriendlyFirstOperationValue077(building?.EffectIdentity) +
                "\n\nADJACENCY BONUSES  •  " + Math.Max(0, state.AdjacencyBonusCount) +
                "\nEarn Hall XP and materials during meaningful operations, then add another facility here.",
                20,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(result.rectTransform, new Rect(0.075f, 0.24f, 0.85f, 0.54f));
            ConfigureResponsiveText062(result, 14, 21);
            result.raycastTarget = false;

            var canOpenAnotherBuild = GuildCityWorkshopFacilityChoices078(state).Count > 0;
            var returnButton = RuntimeUi.AddButton(
                parent,
                "Guild City Workshop Complete 078",
                "FACILITY IS WORKING",
                () =>
                {
                    var progression080 = _coordinator as
                        GuildCity017D.IGuildHallGuidedProgressionCoordinator080;
                    var acknowledged080 = progression080?.AcknowledgeFirstFacilityPayoff080();
                    if (acknowledged080 != null)
                    {
                        _localStatus = acknowledged080.Message;
                        _localStatusPositive = acknowledged080.Succeeded;
                    }
                    _guildCityTab017D = "HALL";
                    BuildCurrentScreen();
                },
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Positive);
            AnchorGuildCityWorkshop078(
                returnButton.GetComponent<RectTransform>(),
                canOpenAnotherBuild
                    ? new Rect(0.075f, 0.065f, 0.405f, 0.135f)
                    : new Rect(0.075f, 0.065f, 0.85f, 0.135f));
            ConfigureResponsiveText062(returnButton.GetComponentInChildren<Text>(), 16, 23);

            if (canOpenAnotherBuild)
            {
                var addAnother = RuntimeUi.AddButton(
                    parent,
                    "Guild City Workshop Add Facility 078",
                    "ADD NEXT FACILITY",
                    () =>
                    {
                        _guildCityWorkshopChoosingFacility078 = true;
                        _guildCityWorkshopFacilityPage078 = 0;
                        BuildCurrentScreen();
                    },
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                AnchorGuildCityWorkshop078(
                    addAnother.GetComponent<RectTransform>(),
                    new Rect(0.52f, 0.065f, 0.405f, 0.135f));
                ConfigureResponsiveText062(addAnother.GetComponentInChildren<Text>(), 15, 22);
            }
            return returnButton;
        }

        private Button BuildGuildCityWorkshopFacilityChoices078(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            const int pageSize = 3;
            var choices = GuildCityWorkshopFacilityChoices078(state).ToArray();
            var maximumPage = Math.Max(0, (choices.Length - 1) / pageSize);
            _guildCityWorkshopFacilityPage078 = Mathf.Clamp(
                _guildCityWorkshopFacilityPage078,
                0,
                maximumPage);
            var visible = choices
                .Skip(_guildCityWorkshopFacilityPage078 * pageSize)
                .Take(pageSize)
                .ToArray();

            Button firstInteractable = null;
            for (var index = 0; index < visible.Length; index++)
            {
                var choice = visible[index];
                var selected = choice;
                var canSubmit = GuildCityWorkshopCanSubmitBuildForVerification078(
                    state,
                    choice.Building);
                var button = RuntimeUi.AddButton(
                    parent,
                    "Guild City Workshop Build " + choice.Building.BuildingId + " 078",
                    choice.Building.DisplayName.ToUpperInvariant() + "\n" +
                    FriendlyFirstOperationValue077(choice.Building.EffectIdentity) + "\n" +
                    GuildCityWorkshopBuildCostLabelForVerification078(state, choice.Building),
                    () =>
                    {
                        var result = coordinator.PlaceGuildCityBuilding017D(
                            selected.Plot.PlotId,
                            selected.Building.BuildingId);
                        if (result != null && result.Succeeded)
                        {
                            _guildCityWorkshopChoosingFacility078 = false;
                            _guildCityWorkshopFacilityPage078 = 0;
                        }
                        ApplyGuildCity017D(result);
                    },
                    RuntimeUi.PrimaryTouchPixels,
                    canSubmit ? RuntimeUi.Accent : new Color(0.13f, 0.11f, 0.09f, 0.98f));
                AnchorGuildCityWorkshop078(
                    button.GetComponent<RectTransform>(),
                    new Rect(0.075f, 0.61f - index * 0.205f, 0.85f, 0.17f));
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 12, 19);
                button.interactable = canSubmit;
                if (firstInteractable == null && canSubmit) firstInteractable = button;
            }

            Button firstNavigation = null;
            if (maximumPage > 0)
            {
                var previous = RuntimeUi.AddButton(
                    parent,
                    "Guild City Workshop Previous Facility Page 078",
                    "←  PREVIOUS",
                    () =>
                    {
                        _guildCityWorkshopFacilityPage078 = Math.Max(
                            0,
                            _guildCityWorkshopFacilityPage078 - 1);
                        BuildCurrentScreen();
                    },
                    RuntimeUi.MinimumTouchPixels,
                    RuntimeUi.ButtonNormal);
                AnchorGuildCityWorkshop078(
                    previous.GetComponent<RectTransform>(),
                    new Rect(0.075f, 0.035f, 0.39f, 0.11f));
                previous.interactable = _guildCityWorkshopFacilityPage078 > 0;
                ConfigureResponsiveText062(previous.GetComponentInChildren<Text>(), 13, 18);
                if (previous.interactable) firstNavigation = previous;

                var next = RuntimeUi.AddButton(
                    parent,
                    "Guild City Workshop Next Facility Page 078",
                    "NEXT  →",
                    () =>
                    {
                        _guildCityWorkshopFacilityPage078 = Math.Min(
                            maximumPage,
                            _guildCityWorkshopFacilityPage078 + 1);
                        BuildCurrentScreen();
                    },
                    RuntimeUi.MinimumTouchPixels,
                    RuntimeUi.ButtonNormal);
                AnchorGuildCityWorkshop078(
                    next.GetComponent<RectTransform>(),
                    new Rect(0.535f, 0.035f, 0.39f, 0.11f));
                next.interactable = _guildCityWorkshopFacilityPage078 < maximumPage;
                ConfigureResponsiveText062(next.GetComponentInChildren<Text>(), 13, 18);
                if (firstNavigation == null && next.interactable) firstNavigation = next;

                var page = RuntimeUi.AddText(
                    parent,
                    "Guild City Workshop Facility Page 078",
                    "OPTIONS " + (_guildCityWorkshopFacilityPage078 + 1) + " / " +
                    (maximumPage + 1),
                    15,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.MutedText,
                    FontStyle.Bold);
                AnchorGuildCityWorkshop078(page.rectTransform, new Rect(0.30f, 0.145f, 0.40f, 0.045f));
                ConfigureResponsiveText062(page, 11, 15);
                page.raycastTarget = false;
            }

            if (choices.Length > 0) return firstInteractable ?? firstNavigation;
            var unavailable = RuntimeUi.AddText(
                parent,
                "Guild City Workshop No Legal Plot 078",
                "No unlocked plot matches an available facility. Complete Guild objectives to open more of Skyhome.",
                21,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(unavailable.rectTransform, new Rect(0.09f, 0.30f, 0.82f, 0.40f));
            ConfigureResponsiveText062(unavailable, 15, 22);
            unavailable.raycastTarget = false;
            return null;
        }

        private static IReadOnlyList<GuildCityWorkshopFacilityChoice078>
            GuildCityWorkshopFacilityChoices078(
                GuildCity017D.GuildCityPresentationState017D state)
        {
            if (state == null) return Array.Empty<GuildCityWorkshopFacilityChoice078>();
            var plots = state.Plots ?? Array.Empty<GuildCity017D.GuildCityPlotView017D>();
            var result = new List<GuildCityWorkshopFacilityChoice078>();
            foreach (var building in (state.Buildings ??
                                      Array.Empty<GuildCity017D.GuildCityBuildingView017D>())
                         .Where(value => value != null &&
                                         !string.IsNullOrWhiteSpace(value.BuildingId)))
            {
                var plot = plots
                    .Where(value => value != null && value.Unlocked &&
                                    string.IsNullOrWhiteSpace(value.BuildingId) &&
                                    StringComparer.Ordinal.Equals(
                                        value.DistrictId,
                                        building.DistrictId))
                    .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (plot == null) continue;
                result.Add(new GuildCityWorkshopFacilityChoice078
                {
                    Plot = plot,
                    Building = building
                });
            }

            return result
                .OrderByDescending(value => GuildCityWorkshopCanSubmitBuildForVerification078(
                    state,
                    value.Building))
                .ThenBy(value => value.Building.DisplayName, StringComparer.Ordinal)
                .ThenBy(value => value.Building.BuildingId, StringComparer.Ordinal)
                .ToArray();
        }

        private Button BuildGuildCityWorkshopStaffChoices078(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityPlotView017D plot)
        {
            var buildingName = string.IsNullOrWhiteSpace(plot.BuildingName)
                ? "THIS FACILITY"
                : plot.BuildingName.ToUpperInvariant();
            var candidates = FirstOperationStaffCandidates078(state).Take(3).ToArray();
            var movingParallelOrder = candidates.Any(value =>
                StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Training") ||
                StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Recovering"));
            var premise = RuntimeUi.AddText(
                parent,
                "Guild City Workshop Staff Premise 078",
                buildingName + " is built. " +
                (movingParallelOrder
                    ? "No active or reserve member is free. Choosing below moves that person's training or recovery order into city staff duty."
                    : "Staff work progresses beside the next operation; the adventurer always remains yours."),
                18,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorGuildCityWorkshop078(premise.rectTransform, new Rect(0.075f, 0.65f, 0.85f, 0.15f));
            ConfigureResponsiveText062(premise, 13, 19);
            premise.raycastTarget = false;

            Button first = null;
            for (var index = 0; index < candidates.Length; index++)
            {
                var candidate = candidates[index];
                var selected = candidate;
                var button = RuntimeUi.AddButton(
                    parent,
                    "Guild City Workshop Staff " + candidate.RecruitId + " 078",
                    candidate.RecruitName.ToUpperInvariant() + "\n" +
                    "CURRENT  •  " + FriendlyFirstOperationValue077(candidate.Kind),
                    () =>
                    {
                        var result = coordinator.AssignGuildCityStaff017D(
                            plot.PlotId,
                            selected.RecruitId);
                        if (result != null && result.Succeeded)
                        {
                            _guildCityWorkshopChoosingFacility078 = false;
                            _guildCityWorkshopFacilityPage078 = 0;
                        }
                        ApplyGuildCity017D(result);
                    },
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                AnchorGuildCityWorkshop078(
                    button.GetComponent<RectTransform>(),
                    new Rect(0.075f, 0.45f - index * 0.17f, 0.85f, 0.14f));
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 14, 20);
                if (first == null) first = button;
            }
            if (first == null)
            {
                var unavailable = RuntimeUi.AddText(
                    parent,
                    "Guild City Workshop No Staff Candidate 078",
                    "No guildmate can leave deployment or an archived assignment. Return after the current operation, then staff this facility here.",
                    19,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Warning,
                    FontStyle.Bold);
                AnchorGuildCityWorkshop078(unavailable.rectTransform, new Rect(0.09f, 0.28f, 0.82f, 0.30f));
                ConfigureResponsiveText062(unavailable, 14, 20);
                unavailable.raycastTarget = false;
            }
            return first;
        }

        private static IReadOnlyList<GuildCity017D.GuildCityAssignmentView017D>
            FirstOperationStaffCandidates078(
                GuildCity017D.GuildCityPresentationState017D state)
        {
            var eligible = (state?.Assignments ??
                            Array.Empty<GuildCity017D.GuildCityAssignmentView017D>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.RecruitId) &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Archived") &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Injured") &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Deployed") &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Staff"))
                .OrderBy(value => value.RecruitName, StringComparer.Ordinal)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToArray();
            var normallyAvailable = eligible
                .Where(value =>
                    !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Recovering") &&
                    !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Training"))
                .ToArray();
            if (normallyAvailable.Length > 0) return normallyAvailable;

            // Migrated or deliberately tiny rosters can have their only member in
            // a parallel order. AssignStaff authoritatively permits moving that
            // member, so expose the move only when no active/reserve option exists.
            return eligible
                .Where(value =>
                    StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Recovering") ||
                    StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Training"))
                .ToArray();
        }

        private static string GuildCityWorkshopPlotLabel078(
            GuildCity017D.GuildCityPlotView017D plot,
            string staffName)
        {
            var plotName = string.IsNullOrWhiteSpace(plot.PlotId)
                ? "PLOT"
                : plot.PlotId.Replace("GC017D_", string.Empty).Replace('_', ' ');
            if (!plot.Unlocked) return plotName + "\nLOCKED";
            if (string.IsNullOrWhiteSpace(plot.BuildingId)) return plotName + "\nOPEN";
            return (plot.BuildingName ?? "FACILITY").ToUpperInvariant() +
                   "  L" + Math.Max(1, plot.BuildingLevel) +
                   "\n" + (string.IsNullOrWhiteSpace(staffName)
                       ? "NEEDS STAFF"
                       : staffName.ToUpperInvariant());
        }

        private static string GuildCityWorkshopStaffName078(
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityPlotView017D plot)
        {
            var recruitId = plot?.StaffRecruitIds?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(recruitId)) return string.Empty;
            var assignment = (state?.Assignments ??
                              Array.Empty<GuildCity017D.GuildCityAssignmentView017D>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            return string.IsNullOrWhiteSpace(assignment?.RecruitName)
                ? recruitId
                : assignment.RecruitName;
        }

        private static void AnchorGuildCityWorkshop078(
            RectTransform target,
            Rect anchors)
        {
            if (target == null) return;
            target.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
            target.anchorMax = new Vector2(anchors.xMax, anchors.yMax);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
