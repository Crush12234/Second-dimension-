using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        public static int RecruitmentSelectedApplicantPageForVerification074(
            IReadOnlyList<GuildCityApplicantView017D> orderedApplicants,
            string selectedRecruitId,
            int currentPage,
            int pageSize = 3)
        {
            if (pageSize <= 0) pageSize = 3;
            var count = orderedApplicants?.Count ?? 0;
            if (count <= 0) return 0;

            for (var index = 0; index < count; index++)
            {
                var applicant = orderedApplicants[index];
                if (applicant != null && StringComparer.Ordinal.Equals(
                        applicant.RecruitId,
                        selectedRecruitId))
                    return index / pageSize;
            }

            return Math.Min(Math.Max(0, currentPage), (count - 1) / pageSize);
        }

        private void BuildRecruitmentDesk074(
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            RuntimeUi.EnsureEventSystem();
            RuntimeUi.ClearChildren(_screenRoot);
            _activePage = null;
            _activeContent = null;
            _activeScroll = null;

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                "Recruitment Desk 074",
                new Color(0.012f, 0.020f, 0.028f, 1f));
            Stretch(root.rectTransform);
            BuildMenuBackdrop090(
                root.transform,
                "Recruitment Desk Illustrated Background 090",
                "SecondDimension/Art/Backgrounds/BG_RECRUIT_DOSSIER_ALCOVE",
                _highContrast ? 0.58f : 0.20f);

            var back = RuntimeUi.AddButton(
                root.transform,
                "Recruitment Return To Hall 074",
                "←  GUILD HALL",
                () =>
                {
                    _guildCityTab017D = "HALL";
                    BuildCurrentScreen();
                },
                72f,
                RuntimeUi.ButtonNormal);
            AnchorRecruitment074(back.GetComponent<RectTransform>(), 0.012f, 0.875f, 0.125f, 0.985f);
            ConfigureResponsiveText062(back.GetComponentInChildren<Text>(), 16, 23);
            // Global BACK retains the precise prior location and replaces this small duplicate.
            back.gameObject.SetActive(!LoopNavigationAvailable164);

            var header = RuntimeUi.AddPanel(
                root.transform,
                "Recruitment Desk Header 074",
                new Color(0.025f, 0.040f, 0.052f, 0.98f));
            AnchorRecruitment074(header.rectTransform, LoopNavigationAvailable164 ? .025f : .135f, .875f, .988f, .985f);
            M1PremiumUi.StylePanel(header, M1PremiumUi.Surface.WorldRibbon);
            var heading = RuntimeUi.AddText(
                header.transform,
                "Recruitment Desk Heading 074",
                "RECRUITMENT DESK\nReview saved interviews. Missions earn named contacts and XP.",
                27,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorRecruitment074(heading.rectTransform, 0.025f, 0.08f, 0.63f, 0.92f);
            ConfigureResponsiveText062(heading, 17, 29);
            var resources = RuntimeUi.AddText(
                header.transform,
                "Recruitment Resources 074",
                "ROSTER  " + state.TotalRecruitCount + " / " + state.RosterCapacity +
                "     XP TO SPEND  " + state.TreasuryXp,
                22,
                TextAnchor.MiddleRight,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorRecruitment074(resources.rectTransform, 0.64f, 0.10f, 0.975f, 0.90f);
            ConfigureResponsiveText062(resources, 14, 22);

            if (!state.HasRecruitmentBoard)
            {
                BuildRecruitmentNotice074(root.transform, coordinator, state);
                return;
            }

            var applicants = (state.Applicants ?? Array.Empty<GuildCityApplicantView017D>())
                .Where(value => value != null)
                .OrderByDescending(value => value.IsAscensionMerge)
                .ThenBy(value => value.IsSigned)
                .ThenBy(value => value.Slot)
                .ToArray();
            if (applicants.Length == 0)
            {
                BuildRecruitmentEmpty074(root.transform, coordinator, state);
                return;
            }

            if (applicants.All(value => !StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    _guildApplicantSelectedId066)))
                _guildApplicantSelectedId066 = applicants[0].RecruitId;
            _guildApplicantPage066 = RecruitmentSelectedApplicantPageForVerification074(
                applicants,
                _guildApplicantSelectedId066,
                _guildApplicantPage066);
            var selected = applicants.First(value => StringComparer.Ordinal.Equals(
                value.RecruitId,
                _guildApplicantSelectedId066));

            BuildRecruitmentPortrait074(root.transform, selected);
            var primary = BuildRecruitmentDecision074(root.transform, coordinator, state, selected);
            var choices = BuildRecruitmentCandidateList074(
                root.transform, applicants, coordinator, state);
            ConfigureRecruitmentNavigation074(choices, primary);
        }

        private void BuildRecruitmentNotice074(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            var pendingLeadNames = state?.PendingExpeditionRecruitLeadNames089 ??
                Array.Empty<string>();
            var hasExpeditionContact = pendingLeadNames.Count > 0;
            var contactLabel = RecruitmentLeadLabel074(pendingLeadNames);
            var card = RuntimeUi.AddPanel(
                parent,
                "Post Recruitment Notice 074",
                new Color(0.035f, 0.045f, 0.050f, 0.98f));
            AnchorRecruitment074(card.rectTransform, 0.20f, 0.20f, 0.80f, 0.79f);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(card.transform, new RectOffset(48, 48, 34, 34), 16f, TextAnchor.MiddleCenter);
            AddResponsiveText062(
                card.transform,
                "Recruitment Notice Title 074",
                hasExpeditionContact
                    ? "EXPEDITION CONTACT ARRIVED"
                    : "THE DESK IS QUIET",
                30,
                44,
                76f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            AddResponsiveText062(
                card.transform,
                "Recruitment Notice Story 074",
                hasExpeditionContact
                    ? contactLabel +
                      " answered your expedition invitation. Open the desk to meet this earned hero. Your saved interviews remain available."
                    : "Continue your missions to earn named contacts. Your existing recruits and saved invitations stay with the Guild.",
                20,
                29,
                145f,
                RuntimeUi.Text,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
            var action = RuntimeUi.AddButton(
                card.transform,
                "Post Applicant Notice 074",
                state.CanInviteEarnedContacts124
                    ? "OPEN DESK & MEET " + contactLabel
                    : "PLAY MISSIONS & EARN CONTACTS",
                state.CanInviteEarnedContacts124
                    ? (Action)(() => ApplyGuildCity017D(coordinator.CommitGuildCityApplicantBoard017D()))
                    : OpenMissions084,
                106f,
                RuntimeUi.Accent);
            FocusRecruitmentPrimary074(action);
        }

        private void BuildRecruitmentEmpty074(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            var card = RuntimeUi.AddPanel(
                parent,
                "Recruitment Interviews Complete 074",
                new Color(0.035f, 0.045f, 0.050f, 0.98f));
            AnchorRecruitment074(card.rectTransform, 0.20f, 0.22f, 0.80f, 0.78f);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(card.transform, new RectOffset(48, 48, 34, 34), 16f, TextAnchor.MiddleCenter);
            AddResponsiveText062(card.transform, "Recruitment Complete 074", "INTERVIEWS COMPLETE", 28, 42, 72f,
                RuntimeUi.Positive, FontStyle.Bold, TextAnchor.MiddleCenter);
            AddResponsiveText062(card.transform, "Recruitment Complete Copy 074",
                state.CanInviteEarnedContacts124
                    ? "An earned contact is ready for an interview. Your existing members stay with the Guild."
                    : "Continue your missions to earn more named contacts. Your recruits and saved invitations remain available.",
                20, 28, 112f, RuntimeUi.Text, FontStyle.Normal, TextAnchor.MiddleCenter);
            var action = RuntimeUi.AddButton(
                card.transform,
                "Post New Applicant Notice 074",
                state.CanInviteEarnedContacts124
                    ? "INVITE EARNED CONTACT"
                    : "PLAY MISSIONS & EARN CONTACTS",
                state.CanInviteEarnedContacts124
                    ? (Action)(() => ApplyGuildCity017D(coordinator.CommitGuildCityApplicantBoard017D()))
                    : OpenMissions084,
                102f,
                RuntimeUi.Accent);
            FocusRecruitmentPrimary074(action);
        }

        private void BuildRecruitmentPortrait074(
            Transform parent,
            GuildCityApplicantView017D applicant)
        {
            var frame = RuntimeUi.AddPanel(
                parent,
                "Selected Applicant Portrait 074",
                Color.clear);
            AnchorRecruitment074(frame.rectTransform, 0.025f, 0.250f, 0.445f, 0.855f);
            PopulateMenuStandeeFrame091(
                frame,
                applicant.RecruitId,
                applicant.VisualSeed,
                applicant.RaceId,
                applicant.PortraitAuthorityId,
                applicant.DisplayName,
                applicant.DisplayName.ToUpperInvariant() + "\n" +
                RecruitmentRole096(applicant).ToUpperInvariant(),
                applicant.ClassTendencyId,
                equipmentIdentity: applicant.EquipmentSummary,
                sceneIntegrated: true);
        }

        private Button BuildRecruitmentDecision074(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            GuildCityApplicantView017D applicant)
        {
            var path = GuildMemberDevelopmentBridge067.Applicant067(coordinator, applicant.RecruitId);
            var compact163 = UnityEngine.Screen.width < 1000 || UnityEngine.Screen.height < 570 || _textScale >= 1.25f;
            var card = RuntimeUi.AddPanel(
                parent,
                "Applicant Decision Card 074",
                new Color(0.035f, 0.045f, 0.050f, 0.985f));
            AnchorRecruitment074(card.rectTransform, 0.475f, 0.250f, 0.975f, 0.855f);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldRibbon);

            var name = RuntimeUi.AddText(
                card.transform,
                "Applicant Decision Name 074",
                applicant.DisplayName.ToUpperInvariant() + "  " + applicant.ClassSymbol,
                34,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorRecruitment074(name.rectTransform, 0.055f, 0.835f, 0.945f, 0.97f);
            ConfigureResponsiveText062(name, 20, 36);

            var identity = RuntimeUi.AddText(
                card.transform,
                "Applicant Decision Identity 074",
                "ROLE  •  " + RecruitmentRole096(applicant).ToUpperInvariant() +
                "\nGROWTH  •  " + (path?.PotentialBand ?? "UNDER REVIEW").ToUpperInvariant() +
                "\nTRAINING  •  " + FriendlyGrowthPath067(path?.FixedWeaponFamilyId).ToUpperInvariant() +
                (string.IsNullOrWhiteSpace(applicant.AuthoredWeaponStyle096) ? string.Empty :
                    "\nWEAPON STYLE  •  " + applicant.AuthoredWeaponStyle096.ToUpperInvariant()) +
                "\nTRAITS  •  " + PlayerFacingApplicantCopy074(
                    applicant.TraitSummary,
                    "Still getting to know them") +
                (applicant.IsSigned ? "\nEQUIPPED GEAR  •  " : "\nSTARTING GEAR  •  ") +
                RecruitmentEquipmentSummaryForVerification074(
                    applicant.EquipmentSummary),
                22,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorRecruitment074(identity.rectTransform, 0.055f, 0.485f, 0.945f, 0.815f);
            ConfigureResponsiveText062(identity, 15, 23);
            identity.verticalOverflow = VerticalWrapMode.Truncate;

            var story = RuntimeUi.AddText(
                card.transform,
                "Applicant Personal Hook 074",
                applicant.IsAscensionMerge
                    ? (compact163 ? string.Empty : "MATCHING HERO COPY\n") +
                      PlayerFacingApplicantCopy074(
                          applicant.DuplicateMergePreview,
                          "Merge this exact hero copy into your permanent member.")
                    : "WHY THEY CAME\n" + PlayerFacingApplicantCopy074(
                        applicant.PersonalHook,
                        "They are looking for a Guild worth calling home."),
                20,
                TextAnchor.UpperLeft,
                RuntimeUi.Warning,
                FontStyle.Italic);
            AnchorRecruitment074(story.rectTransform, 0.055f, 0.335f, 0.945f, 0.465f);
            ConfigureResponsiveText062(story, 14, 21);
            story.verticalOverflow = VerticalWrapMode.Truncate;

            var cost = RuntimeUi.AddText(
                card.transform,
                "Applicant Permanent Cost 074",
                applicant.IsAscensionMerge
                    ? "DUPLICATE MERGE  •  COST " +
                      applicant.SigningCostTreasuryXp +
                      " XP  •  USES NO ROSTER SLOT"
                    : applicant.IsSigned
                    ? "PERMANENT MEMBER  •  READY IN RESERVE"
                    : "PERMANENT RECRUIT  •  COST " + applicant.SigningCostTreasuryXp + " XP",
                19,
                TextAnchor.MiddleCenter,
                applicant.IsSigned || applicant.IsAscensionMerge
                    ? RuntimeUi.Positive
                    : RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorRecruitment074(cost.rectTransform, 0.055f, 0.250f, 0.945f, 0.330f);
            ConfigureResponsiveText062(cost, 14, 20);

            Button primary;
            if (applicant.IsAscensionMerge)
            {
                var xpNeeded = Math.Max(
                    0L,
                    applicant.SigningCostTreasuryXp - Math.Max(0L, state?.TreasuryXp ?? 0L));
                var mergeLabel = StringComparer.OrdinalIgnoreCase.Equals(
                        applicant.DuplicateMergeKind, "Ascension")
                    ? "ASCEND " + applicant.DisplayName.ToUpperInvariant() +
                      "  " + applicant.NextAscensionLevel + "/" +
                      global::SecondDimension.Gameplay.State.RecruitAscensionRules089.MaximumLevel
                    : "MERGE COPY  •  " +
                      FriendlyDuplicateMergeKind074(applicant.DuplicateMergeKind);
                primary = RuntimeUi.AddButton(
                    card.transform,
                    "Merge Hero Duplicate Primary 089",
                    applicant.CanAfford
                        ? mergeLabel
                        : "PLAY A MISSION\nEARN " + xpNeeded + " MORE XP",
                    applicant.CanAfford
                        ? (Action)(() => ApplyGuildCity017D(
                            coordinator.SignGuildCityApplicant017D(applicant.RecruitId)))
                        : (Action)OpenMissions084,
                    104f,
                    RuntimeUi.Positive);
            }
            else if (!applicant.IsSigned)
            {
                var xpNeeded = Math.Max(
                    0L,
                    applicant.SigningCostTreasuryXp - Math.Max(0L, state?.TreasuryXp ?? 0L));
                var rosterFull163 = state != null && state.TotalRecruitCount >= state.RosterCapacity;
                primary = RuntimeUi.AddButton(
                    card.transform,
                    "Recruit Applicant Primary 074",
                    rosterFull163
                        ? "ROSTER FULL\n" + state.TotalRecruitCount + " / " + state.RosterCapacity + " MEMBERS"
                        : applicant.CanAfford
                        ? "RECRUIT " + applicant.DisplayName.ToUpperInvariant()
                        : xpNeeded > 0 ? "PLAY A MISSION\nEARN " + xpNeeded + " MORE XP" : "RECRUIT UNAVAILABLE",
                    rosterFull163 ? (Action)(() => { }) : applicant.CanAfford
                        ? (Action)(() => ApplyGuildCity017D(
                            coordinator.SignGuildCityApplicant017D(applicant.RecruitId)))
                        : (Action)OpenMissions084,
                    104f,
                    RuntimeUi.Accent);
                primary.interactable = !rosterFull163 && (applicant.CanAfford || xpNeeded > 0);
            }
            else
            {
                primary = RuntimeUi.AddButton(
                    card.transform,
                    "Place Applicant In Union 074",
                    "PLACE IN A UNION",
                    () =>
                    {
                        _selectedRecruitId = string.IsNullOrWhiteSpace(
                            applicant.OwnedRecruitId)
                            ? applicant.RecruitId
                            : applicant.OwnedRecruitId;
                        _screen = M1Screen.UnionBuilder;
                        BuildCurrentScreen();
                    },
                    104f,
                    RuntimeUi.Positive);
            }
            AnchorRecruitment074(primary.GetComponent<RectTransform>(), 0.055f, compact163 ? 0.010f : 0.045f, 0.945f, 0.235f);
            ConfigureResponsiveText062(primary.GetComponentInChildren<Text>(), 17, 25);
            var primaryRect = primary.GetComponent<RectTransform>();
            primary.gameObject.AddComponent<MinimumTownTarget164>().Bounds =
                new Rect(primaryRect.anchorMin.x,primaryRect.anchorMin.y,primaryRect.anchorMax.x-primaryRect.anchorMin.x,primaryRect.anchorMax.y-primaryRect.anchorMin.y);
            return primary;
        }

        public static string RecruitmentEquipmentSummaryForVerification074(string equipmentSummary)
        {
            var visible = PlayerFacingApplicantCopy074(equipmentSummary, string.Empty)
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Select(value => new global::SecondDimension.Gameplay.GuildCity017D.PlayerFacingLabelService070()
                    .DefaultLabel(value, value, "Field gear"))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return visible.Length == 0 ? "Field kit" : string.Join("  •  ", visible);
        }

        private static string RecruitmentRole096(GuildCityApplicantView017D applicant) =>
            string.IsNullOrWhiteSpace(applicant.AuthoredRole096)
                ? FriendlyApplicantLabel066(applicant.ClassTendencyId) : applicant.AuthoredRole096;

        private static string PlayerFacingApplicantCopy074(string value, string fallback)
        {
            var visible = string.IsNullOrWhiteSpace(value) ? fallback : value;
            if (string.IsNullOrWhiteSpace(visible)) return string.Empty;
            visible = visible.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (visible.Contains("  ")) visible = visible.Replace("  ", " ");
            return visible;
        }

        private static string FriendlyDuplicateMergeKind074(string kind)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(kind, "TreeUnlocked"))
                return "UNLOCK SKILL TREE";
            if (StringComparer.OrdinalIgnoreCase.Equals(kind, "ArtLeveled"))
                return "LEVEL UP ART";
            if (StringComparer.OrdinalIgnoreCase.Equals(kind, "VeteranTraining"))
                return "VETERAN TRAINING";
            return "ASCENSION";
        }

        private List<Button> BuildRecruitmentCandidateList074(
            Transform parent,
            IReadOnlyList<GuildCityApplicantView017D> applicants,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            var panel = RuntimeUi.AddPanel(
                parent,
                "Applicant Shortlist 074",
                new Color(0.018f, 0.030f, 0.042f, 0.98f));
            AnchorRecruitment074(panel.rectTransform, .025f, .010f, .975f, .240f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldRibbon);

            var title = RuntimeUi.AddText(
                panel.transform,
                "Applicant Shortlist Title 074",
                "TODAY'S APPLICANTS",
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorRecruitment074(title.rectTransform, 0.020f, 0.825f, 0.735f, 0.985f);
            ConfigureResponsiveText062(title, 13, 20);

            var pendingLeadNames = state?.PendingExpeditionRecruitLeadNames089 ??
                Array.Empty<string>();
            var hasExpeditionContact = pendingLeadNames.Count > 0;
            var boardAction = RuntimeUi.AddButton(
                panel.transform,
                hasExpeditionContact
                    ? "Invite Expedition Recruit Lead 089"
                    : "Refresh Applicant Group 074",
                state.CanInviteEarnedContacts124
                    ? "★  INVITE " + RecruitmentLeadLabel074(pendingLeadNames)
                    : hasExpeditionContact
                        ? "CONTACTS QUEUED\nCOMPLETE AN INTERVIEW"
                        : "PLAY MISSIONS\nEARN CONTACTS",
                state.CanInviteEarnedContacts124
                    ? (Action)(() => ApplyGuildCity017D(coordinator.CommitGuildCityApplicantBoard017D()))
                    : OpenMissions084,
                76f,
                hasExpeditionContact ? RuntimeUi.Positive : RuntimeUi.ButtonNormal);
            boardAction.interactable = !hasExpeditionContact || state.CanInviteEarnedContacts124;
            AnchorRecruitment074(
                boardAction.GetComponent<RectTransform>(),
                0.685f, 0.040f, 0.840f, 0.750f);
            ConfigureResponsiveText062(
                boardAction.GetComponentInChildren<Text>(), 12, 18);

            var buttons = new List<Button>();
            var visible = applicants.Skip(_guildApplicantPage066 * 3).Take(3).ToArray();
            for (var index = 0; index < visible.Length; index++)
            {
                var captured = visible[index];
                var selected = StringComparer.Ordinal.Equals(captured.RecruitId, _guildApplicantSelectedId066);
                var button = RuntimeUi.AddButton(
                    panel.transform,
                    "Applicant Shortlist " + captured.RecruitId + " 074",
                     (selected ? "◆  " : string.Empty) + captured.DisplayName.ToUpperInvariant() +
                     "\n" + RecruitmentRole096(captured).ToUpperInvariant() +
                     "  •  " + (captured.IsAscensionMerge
                         ? "ASCENSION READY"
                         : captured.IsSigned ? "RECRUITED" : "AVAILABLE"),
                    () =>
                    {
                        _guildApplicantSelectedId066 = captured.RecruitId;
                        BuildCurrentScreen();
                    },
                    142f,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                AnchorRecruitment074(
                    button.GetComponent<RectTransform>(),
                    0.015f + index * 0.220f,
                    0.040f,
                    0.225f + index * 0.220f,
                    0.750f);
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 14, 21);
                AddMenuApplicantStandeeToButton090(button, captured);
                buttons.Add(button);
            }

            if (applicants.Count > 3)
            {
                var previous = RuntimeUi.AddButton(panel.transform, "Previous Applicant Page 074", "←", () =>
                {
                    _guildApplicantPage066 = Mathf.Max(0, _guildApplicantPage066 - 1);
                    _guildApplicantSelectedId066 = applicants[_guildApplicantPage066 * 3].RecruitId;
                    BuildCurrentScreen();
                }, 60f, RuntimeUi.ButtonNormal);
                AnchorRecruitment074(previous.GetComponent<RectTransform>(), 0.850f, 0.040f, 0.915f, 0.750f);
                var previousLabel = previous.GetComponentInChildren<Text>();
                ConfigureResponsiveText062(previousLabel, 14, 22);
                previousLabel.rectTransform.offsetMin = new Vector2(12f, 4f);
                previousLabel.rectTransform.offsetMax = new Vector2(-12f, -4f);
                previous.interactable = _guildApplicantPage066 > 0;
                var next = RuntimeUi.AddButton(panel.transform, "Next Applicant Page 074", "→", () =>
                {
                    _guildApplicantPage066 = Mathf.Min((applicants.Count - 1) / 3, _guildApplicantPage066 + 1);
                    _guildApplicantSelectedId066 = applicants[_guildApplicantPage066 * 3].RecruitId;
                    BuildCurrentScreen();
                }, 60f, RuntimeUi.ButtonNormal);
                AnchorRecruitment074(next.GetComponent<RectTransform>(), 0.925f, 0.040f, 0.990f, 0.750f);
                var nextLabel = next.GetComponentInChildren<Text>();
                ConfigureResponsiveText062(nextLabel, 14, 22);
                nextLabel.rectTransform.offsetMin = new Vector2(12f, 4f);
                nextLabel.rectTransform.offsetMax = new Vector2(-12f, -4f);
                next.interactable = (_guildApplicantPage066 + 1) * 3 < applicants.Count;
                buttons.Add(previous);
                buttons.Add(next);
            }
            buttons.Add(boardAction);
            foreach (var control in buttons)
            {
                var rect = control.GetComponent<RectTransform>();
                control.gameObject.AddComponent<MinimumTownTarget164>().Bounds =
                    new Rect(rect.anchorMin.x,rect.anchorMin.y,rect.anchorMax.x-rect.anchorMin.x,rect.anchorMax.y-rect.anchorMin.y);
            }
            return buttons;
        }

        private static string RecruitmentLeadLabel074(
            IReadOnlyList<string> pendingLeadNames)
        {
            if (pendingLeadNames == null || pendingLeadNames.Count == 0)
                return "NAMED CONTACT";
            if (pendingLeadNames.Count == 1)
                return (pendingLeadNames[0] ?? "NAMED CONTACT").ToUpperInvariant();
            return pendingLeadNames.Count + " NAMED CONTACTS";
        }

        private static void ConfigureRecruitmentNavigation074(
            IReadOnlyList<Button> choices,
            Button primary)
        {
            if (choices == null || choices.Count == 0)
            {
                primary?.Select();
                return;
            }
            for (var index = 0; index < choices.Count; index++)
            {
                var navigation = choices[index].navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = primary;
                navigation.selectOnDown = choices[(index + 1) % choices.Count];
                navigation.selectOnLeft = choices[(index - 1 + choices.Count) % choices.Count];
                navigation.selectOnRight = choices[(index + 1) % choices.Count];
                choices[index].navigation = navigation;
            }
            if (primary != null)
            {
                var navigation = primary.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnRight = choices[0];
                navigation.selectOnUp = primary;
                navigation.selectOnDown = choices[0];
                navigation.selectOnLeft = primary;
                primary.navigation = navigation;
            }
            choices[0].Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
        }

        private static void FocusRecruitmentPrimary074(Button action)
        {
            if (action == null) return;
            var navigation = action.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = action;
            navigation.selectOnDown = action;
            navigation.selectOnLeft = action;
            navigation.selectOnRight = action;
            action.navigation = navigation;
            action.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(action.gameObject);
        }

        private static void AnchorRecruitment074(
            RectTransform rect,
            float xMin,
            float yMin,
            float xMax,
            float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
