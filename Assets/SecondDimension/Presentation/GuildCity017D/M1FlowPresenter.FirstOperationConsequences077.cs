using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public enum FirstOperationConsequenceStage077
    {
        NotRequired,
        RecoveryOrTraining,
        RelationshipMemory,
        FacilityChoice,
        StaffFacility,
        Complete
    }

    public sealed partial class M1FlowPresenter
    {
        public const string FirstOperationConsequencesRootName077 =
            "First Operation Guild Consequences 077";
        public const string FirstOperationRecoveryHeading077 =
            "WHO RECOVERS OR TRAINS NEXT?";
        public const string FirstOperationRelationshipHeading077 =
            "WHAT WILL YOUR GUILD REMEMBER?";
        public const string FirstOperationFacilityHeading077 =
            "WHAT WILL YOUR GUILD BUILD FIRST?";
        public const string FirstOperationStaffHeading078 =
            "WHO WILL MAKE THIS FACILITY MATTER?";

        private const string FirstOperationHomecomingSourceId077 =
            "FIRST_OPERATION_HOMECOMING_077";
        private const string FirstOperationHomecomingSceneId077 =
            "REL_SCENE_FIRST_OPERATION_HOMECOMING_077";

        private sealed class FirstOperationFacilityChoice077
        {
            public GuildCity017D.GuildCityPlotView017D Plot;
            public GuildCity017D.GuildCityBuildingView017D Building;
        }

        /// <summary>
        /// The first-operation report is derived entirely from persisted Guild
        /// authority. No presentation-only bit can strand a reload between steps.
        /// Operation 2+ and already-started Chapter 2 saves deliberately bypass it.
        /// </summary>
        public static FirstOperationConsequenceStage077
            FirstOperationConsequenceStageForVerification077(
                GuildCity017D.GuildCityPresentationState017D state)
        {
            if (state == null || state.OperationOrdinal != 1 ||
                !IsStoryContractCompleted065(state, FirstStoryContractId065) ||
                IsStoryContractCompleted065(state, SecondStoryContractId065) ||
                IsStoryContractActive065(state, SecondStoryContractId065))
            {
                return FirstOperationConsequenceStage077.NotRequired;
            }

            var candidates = FirstOperationAssignmentCandidates077(state);
            var hasRecoveryOrTraining = (state.Assignments ??
                                         Array.Empty<GuildCity017D.GuildCityAssignmentView017D>())
                .Any(value => value != null &&
                              (StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Recovering") ||
                               StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Training")));
            if (!hasRecoveryOrTraining && candidates.Count > 0)
                return FirstOperationConsequenceStage077.RecoveryOrTraining;

            var relationships = state.Relationships ??
                                Array.Empty<GuildCity017D.GuildCityRelationshipView017D>();
            if (!relationships.Any(value => value != null && value.Viewed) &&
                (relationships.Any(value => value != null) || candidates.Count >= 2))
            {
                return FirstOperationConsequenceStage077.RelationshipMemory;
            }

            if (state.PlacedBuildingCount <= 0 &&
                FirstOperationFacilityChoices077(state).Count > 0)
            {
                return FirstOperationConsequenceStage077.FacilityChoice;
            }

            var firstUnstaffed = (state.Plots ??
                                  Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.BuildingId) &&
                                (value.StaffRecruitIds?.Count ?? 0) == 0)
                .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (state.StaffedBuildingCount <= 0 && firstUnstaffed != null)
            {
                return FirstOperationConsequenceStage077.StaffFacility;
            }

            return FirstOperationConsequenceStage077.Complete;
        }

        public static bool NeedsFirstOperationConsequences077(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var stage = FirstOperationConsequenceStageForVerification077(state);
            return stage != FirstOperationConsequenceStage077.NotRequired &&
                   stage != FirstOperationConsequenceStage077.Complete;
        }

        public static string FirstOperationConsequenceActionForVerification077(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            switch (FirstOperationConsequenceStageForVerification077(state))
            {
                case FirstOperationConsequenceStage077.RecoveryOrTraining:
                    return "SET RECOVERY & TRAINING";
                case FirstOperationConsequenceStage077.RelationshipMemory:
                    return "REMEMBER THE OPERATION";
                case FirstOperationConsequenceStage077.FacilityChoice:
                    return "CHOOSE YOUR FIRST FACILITY";
                case FirstOperationConsequenceStage077.StaffFacility:
                    return "STAFF YOUR FIRST FACILITY";
                default:
                    return string.Empty;
            }
        }

        public static string FirstOperationPremiseForVerification077(
            FirstOperationConsequenceStage077 stage) =>
            FirstOperationPremise077(stage);

        public static string FirstOperationRecoveryChoiceLabel077(int progress) =>
            "RECOVERY  •  REST AT THE GUILD\n+" +
            Math.Max(0, progress) + " RECOVERY PROGRESS  •  STAYS HOME";

        public static string FirstOperationTrainingChoiceLabel077(int progress) =>
            "TRAINING  •  BUILD FUTURE GROWTH\n+" +
            Math.Max(0, progress) + " TRAINING PROGRESS  •  STAYS HOME";

        private void OpenFirstOperationConsequences077()
        {
            var state = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)
                ?.GuildCity017D;
            _guildCityTab017D = NeedsFirstOperationConsequences077(state)
                ? "CONSEQUENCES"
                : "HALL";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        private void BuildFirstOperationConsequences077(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var stage = FirstOperationConsequenceStageForVerification077(state);
            if (stage == FirstOperationConsequenceStage077.NotRequired ||
                stage == FirstOperationConsequenceStage077.Complete)
            {
                _guildCityTab017D = "HALL";
                BuildCurrentScreen();
                return;
            }

            BuildPhoneSimpleHomecoming085(coordinator, state);
        }

        /// <summary>
        /// The Hall used to stop the player for four separate management reports.
        /// The phone-simple loop keeps those durable systems, but presents one
        /// readable homecoming and applies safe defaults behind one continue tap.
        /// </summary>
        private void BuildPhoneSimpleHomecoming085(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {

            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activeContent = null;
            _activeScroll = null;

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                FirstOperationConsequencesRootName077,
                new Color(0.008f, 0.014f, 0.022f, 1f));
            Stretch(root.rectTransform);
            _activePage = root.rectTransform;

            BuildLivingGuildBackdrop074(root.transform);
            var shade = RuntimeUi.AddPanel(
                root.transform,
                "Phone Simple Homecoming Shade 085",
                new Color(0.004f, 0.009f, 0.015f, _highContrast ? 0.64f : 0.48f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;
            BuildLivingGuildStatusBar074(root.transform, state);

            var headingPanel = RuntimeUi.AddPanel(
                root.transform,
                "Phone Simple Homecoming Heading Panel 085",
                new Color(0.012f, 0.022f, 0.034f, 0.97f));
            AnchorFirstOperation077(headingPanel.rectTransform,
                new Rect(0.075f, 0.675f, 0.85f, 0.19f));
            M1PremiumUi.StylePanel(headingPanel, M1PremiumUi.Surface.WorldPaper);

            var heading = RuntimeUi.AddText(
                headingPanel.transform,
                "Phone Simple Homecoming Heading 085",
                "MISSION COMPLETE  •  THE PATROL IS HOME",
                34,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorFirstOperation077(heading.rectTransform, new Rect(0.035f, 0.50f, 0.93f, 0.38f));
            ConfigureResponsiveText062(heading, 22, 34);
            heading.raycastTarget = false;

            var premise = RuntimeUi.AddText(
                headingPanel.transform,
                "Phone Simple Homecoming Premise 085",
                "The Wayglass is safe. Your Guild handles recovery and opens the next story automatically.",
                21,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorFirstOperation077(premise.rectTransform, new Rect(0.035f, 0.08f, 0.93f, 0.38f));
            ConfigureResponsiveText062(premise, 16, 21);
            premise.raycastTarget = false;

            var summary = RuntimeUi.AddPanel(
                root.transform,
                "Phone Simple Homecoming Summary Panel 085",
                new Color(0.010f, 0.019f, 0.028f, 0.97f));
            AnchorFirstOperation077(summary.rectTransform,
                new Rect(0.16f, 0.255f, 0.68f, 0.36f));
            M1PremiumUi.StylePanel(summary, M1PremiumUi.Surface.WorldGlass);

            var summaryText = RuntimeUi.AddText(
                summary.transform,
                "Phone Simple Homecoming Summary 085",
                "WAYGLASS SECURED\n" +
                "Your company recovers together.\n\n" +
                "GUILD MEMORY SAVED\n" +
                "The Lantern Road victory becomes part of the Hall.\n\n" +
                "HOME BASE READY\n" +
                "Your first staffed bonus is selected automatically.",
                25,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorFirstOperation077(summaryText.rectTransform,
                new Rect(0.055f, 0.07f, 0.89f, 0.86f));
            ConfigureResponsiveText062(summaryText, 17, 25);
            summaryText.raycastTarget = false;

            var continueButton = RuntimeUi.AddButton(
                root.transform,
                "Phone Simple Homecoming Continue 085",
                "CONTINUE TO GUILD HALL  →",
                () => CompletePhoneSimpleHomecoming085(coordinator),
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorFirstOperation077(continueButton.GetComponent<RectTransform>(),
                new Rect(0.29f, 0.075f, 0.42f, 0.125f));
            ConfigureResponsiveText062(
                continueButton.GetComponentInChildren<Text>(), 20, 30);

            var commandFailed = !_localStatusPositive &&
                                !string.IsNullOrWhiteSpace(_localStatus);
            var save = RuntimeUi.AddText(
                root.transform,
                "Phone Simple Homecoming Save Promise 085",
                commandFailed
                    ? "ORDER NOT SAVED  •  " + _localStatus
                    : "ONE TAP  •  RECOVERY, MEMORY & HOME BASE SAVE AUTOMATICALLY",
                19,
                TextAnchor.MiddleCenter,
                commandFailed ? RuntimeUi.Warning : RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorFirstOperation077(save.rectTransform,
                new Rect(0.15f, 0.018f, 0.70f, 0.045f));
            ConfigureResponsiveText062(save, 14, 19);
            save.raycastTarget = false;

            continueButton.Select();
        }

        private void CompletePhoneSimpleHomecoming085(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if (coordinator == null)
            {
                FailPhoneSimpleHomecoming085(null);
                return;
            }

            for (var guard = 0; guard < 8; guard++)
            {
                var state = coordinator.GuildCity017D;
                var stage = FirstOperationConsequenceStageForVerification077(state);
                if (stage == FirstOperationConsequenceStage077.NotRequired ||
                    stage == FirstOperationConsequenceStage077.Complete)
                    break;

                M1CommandResult result;
                switch (stage)
                {
                    case FirstOperationConsequenceStage077.RecoveryOrTraining:
                    {
                        var candidate = FirstOperationAssignmentCandidates077(state)
                            .FirstOrDefault();
                        if (candidate == null)
                        {
                            FailPhoneSimpleHomecoming085(
                                "No available guildmate could receive the recovery order.");
                            return;
                        }
                        result = coordinator.SetGuildCityAssignment017D(
                            candidate.RecruitId, "Recovering");
                        break;
                    }
                    case FirstOperationConsequenceStage077.RelationshipMemory:
                    {
                        var memory = (state.Relationships ??
                                      Array.Empty<GuildCity017D.GuildCityRelationshipView017D>())
                            .FirstOrDefault(value => value != null && !value.Viewed &&
                                !string.IsNullOrWhiteSpace(value.SceneId));
                        if (memory != null)
                        {
                            result = coordinator.ViewGuildCityRelationshipScene017D(
                                memory.SceneId);
                            break;
                        }

                        var pair = FirstOperationAssignmentCandidates077(state)
                            .Take(2)
                            .ToArray();
                        if (pair.Length < 2)
                        {
                            FailPhoneSimpleHomecoming085(
                                "Two guildmates are required to save the road-home memory.");
                            return;
                        }
                        var summary = pair[0].RecruitName + " and " +
                                      pair[1].RecruitName +
                                      " carried the Wayglass home together.";
                        result = coordinator.AddGuildCityRelationshipMemory017D(
                            pair[0].RecruitId,
                            pair[1].RecruitId,
                            FirstOperationHomecomingSourceId077,
                            summary,
                            2,
                            FirstOperationHomecomingSceneId077);
                        if (result != null && result.Succeeded)
                            result = coordinator.ViewGuildCityRelationshipScene017D(
                                FirstOperationHomecomingSceneId077);
                        break;
                    }
                    case FirstOperationConsequenceStage077.FacilityChoice:
                    {
                        var choice = FirstOperationFacilityChoices077(state)
                            .OrderBy(value => (value?.Building?.EffectIdentity ?? string.Empty)
                                .IndexOf("Guild XP", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? 0
                                    : 1)
                            .ThenBy(value => value?.Building?.DisplayName,
                                StringComparer.Ordinal)
                            .FirstOrDefault();
                        if (choice?.Plot == null || choice.Building == null)
                        {
                            FailPhoneSimpleHomecoming085(
                                "No ready home-base bonus could be selected.");
                            return;
                        }
                        result = coordinator.PlaceGuildCityBuilding017D(
                            choice.Plot.PlotId,
                            choice.Building.BuildingId);
                        break;
                    }
                    default:
                    {
                        var plot = (state.Plots ??
                                    Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                            .Where(value => value != null &&
                                            !string.IsNullOrWhiteSpace(value.BuildingId) &&
                                            (value.StaffRecruitIds?.Count ?? 0) == 0)
                            .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                            .FirstOrDefault();
                        var staff = FirstOperationStaffCandidates078(state)
                            .FirstOrDefault();
                        if (plot == null || staff == null)
                        {
                            FailPhoneSimpleHomecoming085(
                                "The new home-base bonus could not find an available guildmate.");
                            return;
                        }
                        result = coordinator.AssignGuildCityStaff017D(
                            plot.PlotId,
                            staff.RecruitId);
                        break;
                    }
                }

                if (result == null || !result.Succeeded)
                {
                    FailPhoneSimpleHomecoming085(result?.Message);
                    return;
                }
            }

            if (FirstOperationConsequenceStageForVerification077(
                    coordinator.GuildCity017D) != FirstOperationConsequenceStage077.Complete)
            {
                FailPhoneSimpleHomecoming085(
                    "The automatic homecoming did not reach a complete saved state.");
                return;
            }

            var guided = coordinator as
                GuildCity017D.IGuildHallGuidedProgressionCoordinator080;
            if (guided != null &&
                !coordinator.GuildCity017D.FirstFacilityPayoffAcknowledged080)
            {
                var acknowledged = guided.AcknowledgeFirstFacilityPayoff080();
                if (acknowledged == null || !acknowledged.Succeeded)
                {
                    FailPhoneSimpleHomecoming085(acknowledged?.Message);
                    return;
                }
            }

            _localStatusPositive = true;
            _localStatus = "Homecoming saved. Chapter 2 is ready at Missions.";
            _guildCityTab017D = "HALL";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        private void FailPhoneSimpleHomecoming085(string message)
        {
            _localStatusPositive = false;
            _localStatus = string.IsNullOrWhiteSpace(message)
                ? "The homecoming could not be saved. Try again."
                : message;
            BuildCurrentScreen();
        }

        private Button BuildFirstOperationAssignmentChoices077(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var candidates = FirstOperationAssignmentCandidates077(state).Take(3).ToArray();
            Button first = null;
            for (var index = 0; index < candidates.Length; index++)
            {
                var candidate = candidates[index];
                var y = 0.695f - index * 0.315f;
                var name = RuntimeUi.AddText(
                    parent,
                    "First Operation Assignment Member " + candidate.RecruitId + " 077",
                    candidate.RecruitName.ToUpperInvariant() + "\nCURRENT  •  " +
                    FriendlyFirstOperationValue077(candidate.Kind),
                    22,
                    TextAnchor.MiddleLeft,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                AnchorFirstOperation077(name.rectTransform, new Rect(0.035f, y, 0.27f, 0.245f));
                ConfigureResponsiveText062(name, 15, 22);
                name.raycastTarget = false;

                var selected = candidate;
                var recovery = RuntimeUi.AddButton(
                    parent,
                    "First Operation Choose Recovery " + candidate.RecruitId + " 077",
                    FirstOperationRecoveryChoiceLabel077(
                        state.RecoveryProgressPerOperation),
                    () => ApplyFirstOperationAssignment077(
                        coordinator, selected.RecruitId, "Recovering"),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Positive);
                AnchorFirstOperation077(recovery.GetComponent<RectTransform>(),
                    new Rect(0.325f, y, 0.305f, 0.245f));
                ConfigureResponsiveText062(recovery.GetComponentInChildren<Text>(), 15, 22);

                var training = RuntimeUi.AddButton(
                    parent,
                    "First Operation Choose Training " + candidate.RecruitId + " 077",
                    FirstOperationTrainingChoiceLabel077(
                        state.TrainingProgressPerOperation),
                    () => ApplyFirstOperationAssignment077(
                        coordinator, selected.RecruitId, "Training"),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                AnchorFirstOperation077(training.GetComponent<RectTransform>(),
                    new Rect(0.650f, y, 0.315f, 0.245f));
                ConfigureResponsiveText062(training.GetComponentInChildren<Text>(), 15, 22);
                if (first == null) first = recovery;
            }
            return first;
        }

        private Button BuildFirstOperationRelationshipChoices077(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var memories = (state.Relationships ??
                            Array.Empty<GuildCity017D.GuildCityRelationshipView017D>())
                .Where(value => value != null && !value.Viewed &&
                                !string.IsNullOrWhiteSpace(value.SceneId))
                .Take(3)
                .ToArray();
            Button first = null;
            for (var index = 0; index < memories.Length; index++)
            {
                var memory = memories[index];
                var y = 0.685f - index * 0.315f;
                var selected = memory;
                var button = RuntimeUi.AddButton(
                    parent,
                    "First Operation Remember " + memory.MemoryId + " 077",
                    FirstOperationMemoryLabel077(memory, state, index),
                    () => ApplyFirstOperationRelationship077(coordinator, selected.SceneId),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                AnchorFirstOperation077(button.GetComponent<RectTransform>(),
                    new Rect(0.055f, y, 0.89f, 0.25f));
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 15, 22);
                if (first == null) first = button;
            }
            if (first != null) return first;

            var pair = FirstOperationAssignmentCandidates077(state).Take(2).ToArray();
            if (pair.Length < 2) return null;
            var remember = RuntimeUi.AddButton(
                parent,
                "First Operation Remember Homecoming 077",
                "KEEP THE ROAD-HOME MEMORY\n" +
                pair[0].RecruitName.ToUpperInvariant() + "  ↔  " +
                pair[1].RecruitName.ToUpperInvariant(),
                () => ApplyFirstOperationHomecomingMemory077(
                    coordinator, pair[0], pair[1]),
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorFirstOperation077(remember.GetComponent<RectTransform>(),
                new Rect(0.12f, 0.37f, 0.76f, 0.30f));
            ConfigureResponsiveText062(remember.GetComponentInChildren<Text>(), 17, 26);
            return remember;
        }

        private Button BuildFirstOperationFacilityChoices077(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var choices = FirstOperationFacilityChoices077(state).Take(3).ToArray();
            Button first = null;
            for (var index = 0; index < choices.Length; index++)
            {
                var choice = choices[index];
                var y = 0.685f - index * 0.315f;
                var selected = choice;
                var button = RuntimeUi.AddButton(
                    parent,
                    "First Operation Build " + choice.Building.BuildingId + " 077",
                    choice.Building.DisplayName.ToUpperInvariant() + "\n" +
                    FriendlyFirstOperationValue077(choice.Building.EffectIdentity),
                    () => ApplyFirstOperationFacility077(
                        coordinator,
                        selected.Plot.PlotId,
                        selected.Building.BuildingId),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                AnchorFirstOperation077(button.GetComponent<RectTransform>(),
                    new Rect(0.055f, y, 0.89f, 0.25f));
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 15, 22);
                if (first == null) first = button;
            }
            return first;
        }

        private Button BuildFirstOperationStaffChoices078(
            Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var plot = (state.Plots ??
                        Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.BuildingId) &&
                                (value.StaffRecruitIds?.Count ?? 0) == 0)
                .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (plot == null) return null;

            var building = (state.Buildings ??
                            Array.Empty<GuildCity017D.GuildCityBuildingView017D>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.BuildingId, plot.BuildingId));
            var candidates = FirstOperationStaffCandidates078(state).Take(3).ToArray();
            Button first = null;
            for (var index = 0; index < candidates.Length; index++)
            {
                var candidate = candidates[index];
                var y = 0.685f - index * 0.315f;
                var selected = candidate;
                var button = RuntimeUi.AddButton(
                    parent,
                    "First Operation Staff " + candidate.RecruitId + " 078",
                    FirstOperationStaffChoiceLabel078(
                        candidate.RecruitName,
                        plot.BuildingName,
                        building?.EffectIdentity),
                    () => CompleteFirstOperationCommand077(
                        coordinator,
                        coordinator.AssignGuildCityStaff017D(
                            plot.PlotId,
                            selected.RecruitId)),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                AnchorFirstOperation077(
                    button.GetComponent<RectTransform>(),
                    new Rect(0.055f, y, 0.89f, 0.25f));
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 15, 22);
                if (first == null) first = button;
            }
            if (first == null)
            {
                var unavailable = RuntimeUi.AddText(
                    parent,
                    "First Operation No Staff Candidate 078",
                    "THIS FACILITY STILL NEEDS STAFF\nNo guildmate can leave deployment or an archived assignment. The staffing decision remains open until someone is available.",
                    22,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Warning,
                    FontStyle.Bold);
                AnchorFirstOperation077(
                    unavailable.rectTransform,
                    new Rect(0.10f, 0.26f, 0.80f, 0.46f));
                ConfigureResponsiveText062(unavailable, 15, 23);
                unavailable.raycastTarget = false;
            }
            return first;
        }

        private void ApplyFirstOperationAssignment077(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string recruitId,
            string assignmentKind)
        {
            CompleteFirstOperationCommand077(
                coordinator,
                coordinator.SetGuildCityAssignment017D(recruitId, assignmentKind));
        }

        private void ApplyFirstOperationRelationship077(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string sceneId)
        {
            CompleteFirstOperationCommand077(
                coordinator,
                coordinator.ViewGuildCityRelationshipScene017D(sceneId));
        }

        private void ApplyFirstOperationHomecomingMemory077(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityAssignmentView017D first,
            GuildCity017D.GuildCityAssignmentView017D second)
        {
            var summary = first.RecruitName + " and " + second.RecruitName +
                          " carried the Wayglass home together and remembered who made room for the other.";
            var added = coordinator.AddGuildCityRelationshipMemory017D(
                first.RecruitId,
                second.RecruitId,
                FirstOperationHomecomingSourceId077,
                summary,
                2,
                FirstOperationHomecomingSceneId077);
            if (added == null || !added.Succeeded)
            {
                CompleteFirstOperationCommand077(coordinator, added);
                return;
            }
            CompleteFirstOperationCommand077(
                coordinator,
                coordinator.ViewGuildCityRelationshipScene017D(
                    FirstOperationHomecomingSceneId077));
        }

        private void ApplyFirstOperationFacility077(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string plotId,
            string buildingId)
        {
            CompleteFirstOperationCommand077(
                coordinator,
                coordinator.PlaceGuildCityBuilding017D(plotId, buildingId));
        }

        private void CompleteFirstOperationCommand077(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            M1CommandResult result)
        {
            _localStatus = result?.Message ??
                           "The Guild order could not be saved. Nothing was changed.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
            {
                _guildCityTab017D = NeedsFirstOperationConsequences077(
                    coordinator?.GuildCity017D)
                    ? "CONSEQUENCES"
                    : "HALL";
            }
            BuildCurrentScreen();
        }

        private static IReadOnlyList<GuildCity017D.GuildCityAssignmentView017D>
            FirstOperationAssignmentCandidates077(
                GuildCity017D.GuildCityPresentationState017D state)
        {
            return (state?.Assignments ??
                    Array.Empty<GuildCity017D.GuildCityAssignmentView017D>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.RecruitId) &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Archived") &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Injured") &&
                                !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Staff"))
                .OrderBy(value => value.RecruitName, StringComparer.Ordinal)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<FirstOperationFacilityChoice077>
            FirstOperationFacilityChoices077(
                GuildCity017D.GuildCityPresentationState017D state)
        {
            if (state == null || state.CharterBuildCredits <= 0)
                return Array.Empty<FirstOperationFacilityChoice077>();

            var plots = state.Plots ?? Array.Empty<GuildCity017D.GuildCityPlotView017D>();
            var result = new List<FirstOperationFacilityChoice077>();
            foreach (var building in (state.Buildings ??
                                      Array.Empty<GuildCity017D.GuildCityBuildingView017D>())
                         .Where(value => value != null &&
                                         !string.IsNullOrWhiteSpace(value.BuildingId))
                         .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
                         .ThenBy(value => value.BuildingId, StringComparer.Ordinal))
            {
                var plot = plots
                    .Where(value => value != null && value.Unlocked && value.RoadConnected &&
                                    string.IsNullOrWhiteSpace(value.BuildingId) &&
                                    StringComparer.Ordinal.Equals(
                                        value.DistrictId, building.DistrictId))
                    .OrderBy(value => value.PlotId, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (plot == null) continue;
                result.Add(new FirstOperationFacilityChoice077
                {
                    Plot = plot,
                    Building = building
                });
            }
            return result;
        }

        private static string FirstOperationHeading077(
            FirstOperationConsequenceStage077 stage)
        {
            switch (stage)
            {
                case FirstOperationConsequenceStage077.RecoveryOrTraining:
                    return FirstOperationRecoveryHeading077;
                case FirstOperationConsequenceStage077.RelationshipMemory:
                    return FirstOperationRelationshipHeading077;
                case FirstOperationConsequenceStage077.FacilityChoice:
                    return FirstOperationFacilityHeading077;
                default:
                    return FirstOperationStaffHeading078;
            }
        }

        private static string FirstOperationPremise077(
            FirstOperationConsequenceStage077 stage)
        {
            switch (stage)
            {
                case FirstOperationConsequenceStage077.RecoveryOrTraining:
                    return "Recovery banks progress without healing HP. Training builds future growth. Both stay home until you choose Reserve.";
                case FirstOperationConsequenceStage077.RelationshipMemory:
                    return "The road changed people, not just numbers. Choose one shared memory to keep at the Hall.";
                case FirstOperationConsequenceStage077.FacilityChoice:
                    return "Your charter earned one real improvement. Choose the facility that says what this Guild will become.";
                default:
                    return "Choose who will staff the new facility. Their work activates its benefit while the next operation is away.";
            }
        }

        private static string FirstOperationStepLabel077(
            FirstOperationConsequenceStage077 stage)
        {
            switch (stage)
            {
                case FirstOperationConsequenceStage077.RecoveryOrTraining:
                    return "REPORT 1 OF 4";
                case FirstOperationConsequenceStage077.RelationshipMemory:
                    return "REPORT 2 OF 4";
                case FirstOperationConsequenceStage077.FacilityChoice:
                    return "REPORT 3 OF 4";
                default:
                    return "REPORT 4 OF 4";
            }
        }

        public static string FirstOperationMemoryLabelForVerification077(
            GuildCity017D.GuildCityRelationshipView017D memory,
            GuildCity017D.GuildCityPresentationState017D state,
            int choiceIndex) =>
            FirstOperationMemoryLabel077(memory, state, choiceIndex);

        private static string FirstOperationMemoryLabel077(
            GuildCity017D.GuildCityRelationshipView017D memory,
            GuildCity017D.GuildCityPresentationState017D state,
            int choiceIndex)
        {
            if (memory == null) return "KEEP A SHARED MEMORY";
            var first = FirstOperationMemoryRecruitName077(
                state,
                memory.FirstRecruitId);
            var second = FirstOperationMemoryRecruitName077(
                state,
                memory.SecondRecruitId);
            return first.ToUpperInvariant() + "  +  " + second.ToUpperInvariant() +
                   "  •  STRENGTH " + Math.Max(0, memory.Strength) +
                   "\n" + FirstOperationEncounterMemorySummary077(memory, choiceIndex);
        }

        public static string FirstOperationStaffChoiceLabel078(
            string recruitName,
            string buildingName,
            string effectIdentity)
        {
            var person = string.IsNullOrWhiteSpace(recruitName)
                ? "GUILDMATE"
                : recruitName.Trim().ToUpperInvariant();
            var firstName = person.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "THEIR";
            var facility = string.IsNullOrWhiteSpace(buildingName)
                ? "FIRST FACILITY"
                : buildingName.Trim().ToUpperInvariant();
            return person + "  →  STAFF " + facility +
                   "\n" + firstName + "'S WORK " +
                   FirstOperationFacilityBenefit078(effectIdentity);
        }

        private static string FirstOperationMemoryRecruitName077(
            GuildCity017D.GuildCityPresentationState017D state,
            string recruitId)
        {
            var assignment = (state?.Assignments ??
                              Array.Empty<GuildCity017D.GuildCityAssignmentView017D>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            if (!string.IsNullOrWhiteSpace(assignment?.RecruitName))
                return assignment.RecruitName.Trim();
            return string.IsNullOrWhiteSpace(recruitId)
                ? "Guildmate"
                : FriendlyFirstOperationValue077(recruitId)
                    .Replace("RECRUIT ", string.Empty);
        }

        private static string FirstOperationEncounterMemorySummary077(
            GuildCity017D.GuildCityRelationshipView017D memory,
            int choiceIndex)
        {
            var summary = memory.Summary?.Trim() ?? string.Empty;
            var generic = string.IsNullOrWhiteSpace(summary) ||
                          StringComparer.OrdinalIgnoreCase.Equals(
                              summary,
                              "They completed the encounter together.");
            if (!generic) return summary;

            var identity = ((memory.SceneId ?? string.Empty) + " " +
                            (memory.MemoryId ?? string.Empty)).ToUpperInvariant();
            if (identity.Contains("HALL_BREACH"))
                return "They held the Guild Hall breach and protected Skyhome together.";
            if (identity.Contains("LANTERN_ROAD_AMBUSH"))
                return "They broke the Lantern Road ambush and reopened Zorin's trail together.";
            if (identity.Contains("GATE_EATER"))
                return "They stood with Zorin's patrol and stopped the Gate-Eater together.";
            switch (Math.Max(0, choiceIndex) % 3)
            {
                case 0:
                    return "They held the Guild Hall breach and protected Skyhome together.";
                case 1:
                    return "They broke the Lantern Road ambush and reopened Zorin's trail together.";
                default:
                    return "They brought the Lantern Patrol and Wayglass safely home together.";
            }
        }

        private static string FirstOperationFacilityBenefit078(string effectIdentity)
        {
            var effect = effectIdentity ?? string.Empty;
            if (effect.IndexOf("Personal", StringComparison.OrdinalIgnoreCase) >= 0 &&
                effect.IndexOf("Union", StringComparison.OrdinalIgnoreCase) >= 0 &&
                effect.IndexOf("Guild XP", StringComparison.OrdinalIgnoreCase) >= 0)
                return "EARNS PERSONAL, UNION & GUILD XP";
            if (effect.IndexOf("recovery", StringComparison.OrdinalIgnoreCase) >= 0)
                return "INCREASES RECOVERY PROGRESS";
            if (effect.IndexOf("training", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ADVANCES TRAINING & GROWTH";
            if (effect.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0 ||
                effect.IndexOf("supply", StringComparison.OrdinalIgnoreCase) >= 0)
                return "IMPROVES GUILD SUPPLIES";
            return string.IsNullOrWhiteSpace(effect)
                ? "ACTIVATES ITS GUILD BENEFIT"
                : "ACTIVATES " + FriendlyFirstOperationValue077(effect);
        }

        private static string FriendlyFirstOperationValue077(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "A SAVED GUILD IMPROVEMENT";
            var words = value.Trim().Replace('_', ' ').Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Select(word => word.ToUpperInvariant()));
        }

        private static void AnchorFirstOperation077(RectTransform target, Rect anchors)
        {
            target.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
            target.anchorMax = new Vector2(anchors.xMax, anchors.yMax);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
