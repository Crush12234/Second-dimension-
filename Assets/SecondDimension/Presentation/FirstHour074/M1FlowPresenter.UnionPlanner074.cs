using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.M1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        public const int UnionPlannerVisibleTabLimit074 = 3;
        public const int UnionPlannerVisibleChoiceLimit074 = 3;
        public const int UnionPlannerReservePageSize074 = 4;
        public const int UnionPlannerReserveColumns074 = 4;
        public const int UnionPlannerMemberSlotColumns078 = 3;
        public const int UnionPlannerMemberSlotRows078 = 2;
        public const int UnionPlannerReserveLabelMinimumFontSize074 = 18;
        public const int UnionPlannerReserveLabelMaximumFontSize079 = 22;
        public const int UnionPlannerMemberLabelMinimumFontSize074 = 18;
        public const int UnionPlannerMemberLabelMaximumFontSize074 = 21;
        public const float UnionPlannerMinimumSupportedAspect074 = 1.6f;

        private static readonly Rect UnionPlannerMemberCaptionRailRegion074 =
            new Rect(0.025f, 0.015f, 0.950f, 0.375f);
        private static readonly Rect UnionPlannerMemberPortraitRegion074 =
            new Rect(0.050f, 0.410f, 0.900f, 0.550f);

        private static readonly Rect[] UnionPlannerMajorRegionValues074 =
        {
            new Rect(0.012f, 0.872f, 0.976f, 0.116f), // Command bar.
            new Rect(0.012f, 0.385f, 0.530f, 0.467f), // Selected Union, six slots.
            new Rect(0.012f, 0.130f, 0.530f, 0.235f), // Reserve, one calm row.
            new Rect(0.555f, 0.130f, 0.433f, 0.722f), // Formation and intent.
            new Rect(0.012f, 0.012f, 0.976f, 0.105f)  // Save and return.
        };

        private static readonly IReadOnlyList<Rect> UnionPlannerMajorRegionsReadOnly074 =
            Array.AsReadOnly(UnionPlannerMajorRegionValues074);

        private static readonly Rect UnionPlannerForceOverviewTileRegion076 =
            new Rect(0.025f, 0.055f, 0.950f, 0.690f);
        private const float UnionPlannerForceOverviewHorizontalGap076 = 0.014f;
        private const float UnionPlannerForceOverviewVerticalGap076 = 0.025f;

        private bool _unionAdvanced132;
        private bool UnionEditable132 => !(_coordinator is IUnionPlanningCoordinator132 planning132) || planning132.UnionPlanEditable132;
        private Rect UnionRegion132(int index) => _unionAdvanced132 || index != 1 && index != 2
            ? UnionPlannerMajorRegionValues074[index]
            : index == 1 ? new Rect(0.012f, 0.385f, 0.976f, 0.467f) : new Rect(0.012f, 0.130f, 0.976f, 0.235f);
        private int _unionReservePage074;
        private string _unionSelectedReserve109;
        private int _unionFormationPage076 = -1;
        private int _unionDoctrinePage076 = -1;
        private bool _focusedUnionPlannerStoryReturn076;

        public static IReadOnlyList<Rect> UnionPlannerMajorRegionsForVerification074 =>
            UnionPlannerMajorRegionsReadOnly074;

        public static Rect UnionPlannerMemberCaptionRailForVerification074 =>
            UnionPlannerMemberCaptionRailRegion074;

        public static Rect UnionPlannerMemberPortraitForVerification074 =>
            UnionPlannerMemberPortraitRegion074;

        public static string UnionPlannerRoleDesignationForVerification074(
            string classIdentity)
        {
            return M1VisualAssets.ClassColorDesignation(classIdentity);
        }

        public static Color UnionPlannerRoleColorForVerification074(string classIdentity)
        {
            return M1PremiumUi.ClassColor(classIdentity);
        }

        public static string PostRescueForceOverviewHeadingForVerification078(
            IReadOnlyList<M1UnionView> unions,
            int reserveCount)
        {
            var activeUnions = (unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                (value.MemberRecruitIds?.Count ?? 0) > 0)
                .ToArray();
            var deployedCount = activeUnions
                .SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Count();
            return "GATE-EATER TEAM  •  " + deployedCount + " DEPLOYED  •  " +
                   activeUnions.Length + " UNIONS  •  " + Math.Max(0, reserveCount) +
                   " HOLD THE HALL";
        }

        public static IReadOnlyList<Rect> UnionPlannerForceOverviewTileRectsForVerification076()
        {
            const int columns = 3;
            const int rows = 2;
            var width = (UnionPlannerForceOverviewTileRegion076.width -
                         UnionPlannerForceOverviewHorizontalGap076 * (columns - 1)) / columns;
            var height = (UnionPlannerForceOverviewTileRegion076.height -
                          UnionPlannerForceOverviewVerticalGap076 * (rows - 1)) / rows;
            var rects = new Rect[columns * rows];
            for (var index = 0; index < rects.Length; index++)
            {
                var column = index % columns;
                var rowFromTop = index / columns;
                rects[index] = new Rect(
                    UnionPlannerForceOverviewTileRegion076.xMin +
                    column * (width + UnionPlannerForceOverviewHorizontalGap076),
                    UnionPlannerForceOverviewTileRegion076.yMax -
                    (rowFromTop + 1) * height -
                    rowFromTop * UnionPlannerForceOverviewVerticalGap076,
                    width,
                    height);
            }
            return Array.AsReadOnly(rects);
        }

        /// <summary>
        /// Returns the current three-tab page while guaranteeing that the selected
        /// Union is represented. State order is preserved because it is the player's
        /// saved plan order, not a display sort.
        /// </summary>
        public static IReadOnlyList<int> UnionPlannerVisibleUnionIndicesForVerification074(
            IReadOnlyList<M1UnionView> unions,
            int selectedUnionIndex)
        {
            var valid = (unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .ToArray();
            if (valid.Length == 0) return Array.Empty<int>();

            var selectedPosition = Array.FindIndex(
                valid,
                value => value.Index == selectedUnionIndex);
            if (selectedPosition < 0) selectedPosition = 0;
            var pageStart = selectedPosition / UnionPlannerVisibleTabLimit074 *
                            UnionPlannerVisibleTabLimit074;
            return Array.AsReadOnly(valid
                .Skip(pageStart)
                .Take(UnionPlannerVisibleTabLimit074)
                .Select(value => value.Index)
                .ToArray());
        }

        /// <summary>
        /// Starter screens show only three readable choices. A later choice already
        /// held by a migrated save replaces the third starter card so it is never
        /// hidden or silently changed.
        /// </summary>
        public static IReadOnlyList<M1ChoiceView> UnionPlannerVisibleChoicesForVerification074(
            IReadOnlyList<M1ChoiceView> choices,
            string selectedId)
        {
            var valid = (choices ?? Array.Empty<M1ChoiceView>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .ToArray();
            if (valid.Length <= UnionPlannerVisibleChoiceLimit074)
                return Array.AsReadOnly(valid);

            var selected = valid.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.Id, selectedId));
            var visible = valid.Take(UnionPlannerVisibleChoiceLimit074).ToList();
            if (selected != null && visible.All(value =>
                    !StringComparer.Ordinal.Equals(value.Id, selected.Id)))
                visible[visible.Count - 1] = selected;
            return Array.AsReadOnly(visible.ToArray());
        }

        public static int UnionPlannerUnlockedChoiceCountForVerification076(
            int totalChoiceCount,
            bool firstStoryComplete,
            bool secondStoryComplete)
        {
            if (totalChoiceCount <= 0) return 0;
            if (!firstStoryComplete)
                return Math.Min(UnionPlannerVisibleChoiceLimit074, totalChoiceCount);
            if (!secondStoryComplete)
                return Math.Min(UnionPlannerVisibleChoiceLimit074 * 2, totalChoiceCount);
            return totalChoiceCount;
        }

        public static IReadOnlyList<M1ChoiceView> UnionPlannerUnlockedChoicesForVerification076(
            IReadOnlyList<M1ChoiceView> choices,
            string selectedId,
            bool firstStoryComplete,
            bool secondStoryComplete)
        {
            var valid = (choices ?? Array.Empty<M1ChoiceView>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .ToArray();
            var unlockedCount = UnionPlannerUnlockedChoiceCountForVerification076(
                valid.Length,
                firstStoryComplete,
                secondStoryComplete);
            var unlocked = valid.Take(unlockedCount).ToList();
            var selected = valid.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.Id, selectedId));
            if (selected != null && unlocked.All(value =>
                    !StringComparer.Ordinal.Equals(value.Id, selected.Id)))
                unlocked.Add(selected);
            return Array.AsReadOnly(unlocked.ToArray());
        }

        public static int UnionPlannerChoicePageCountForVerification076(int unlockedChoiceCount)
        {
            if (unlockedChoiceCount <= 0) return 1;
            return Math.Max(
                1,
                Mathf.CeilToInt(unlockedChoiceCount /
                                (float)UnionPlannerVisibleChoiceLimit074));
        }

        public static IReadOnlyList<M1ChoiceView> UnionPlannerChoicePageForVerification076(
            IReadOnlyList<M1ChoiceView> unlockedChoices,
            int page)
        {
            var valid = (unlockedChoices ?? Array.Empty<M1ChoiceView>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Id))
                .ToArray();
            var pageCount = UnionPlannerChoicePageCountForVerification076(valid.Length);
            var safePage = Mathf.Clamp(page, 0, pageCount - 1);
            return Array.AsReadOnly(valid
                .Skip(safePage * UnionPlannerVisibleChoiceLimit074)
                .Take(UnionPlannerVisibleChoiceLimit074)
                .ToArray());
        }

        public static int UnionPlannerReservePageCountForVerification074(int reserveCount)
        {
            if (reserveCount <= 0) return 1;
            return Mathf.Max(1, Mathf.CeilToInt(
                reserveCount / (float)UnionPlannerReservePageSize074));
        }

        /// <summary>
        /// A fixed, one-screen Union command table. It intentionally avoids the old
        /// graph-heavy, vertically scrolling builder: team, reserve, formation,
        /// intent, spendable XP, and the one completion action stay visible together.
        /// </summary>
        private void BuildUnionPlanner074()
        {
            var state = _coordinator?.State ?? new M1PresentationState();
            if (!_returnToExpeditionAfterUnionReview076)
                _focusedUnionPlannerStoryReturn076 = false;
            var unions = (state.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .ToArray();
            if (unions.Length > 0 && unions.All(value => value.Index != _selectedUnionIndex))
                _selectedUnionIndex = unions[0].Index;
            var union = unions.FirstOrDefault(value => value.Index == _selectedUnionIndex) ??
                        unions.FirstOrDefault();
            if (union != null) _selectedUnionIndex = union.Index;

            var recruits = (state.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId))
                .ToArray();
            var assignedIds = new HashSet<string>(
                unions.SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>()),
                StringComparer.Ordinal);
            var reserve = recruits
                .Where(value => !assignedIds.Contains(value.RecruitId))
                .OrderBy(value => UnionPlannerRoleSortOrder074(value.ObservedClass))
                .ThenBy(value => value.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (!reserve.Any(value => StringComparer.Ordinal.Equals(value.RecruitId, _unionSelectedReserve109)))
                _unionSelectedReserve109 = null;
            var reservePageCount = UnionPlannerReservePageCountForVerification074(reserve.Length);
            _unionReservePage074 = Mathf.Clamp(_unionReservePage074, 0, reservePageCount - 1);

            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activeContent = null;
            _activeScroll = null;

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                "Union Planner 074",
                new Color(0.008f, 0.014f, 0.022f, _highContrast ? 0.98f : 0.88f));
            Stretch(root.rectTransform);
            root.raycastTarget = false;
            M1PremiumUi.StylePage(root, _highContrast);
            root.color = new Color(1f, 1f, 1f, _highContrast ? 0.98f : 0.90f);
            BuildMenuBackdrop090(
                root.transform,
                "Union Planner Illustrated Background 090",
                "SecondDimension/Art/Backgrounds/BG_UNION_STRATEGY_CHAMBER",
                _highContrast ? 0.62f : 0.42f);
            _activePage = root.rectTransform;

            var topButtons = new List<Button>();
            var memberButtons = new List<Button>();
            var reserveButtons = new List<Button>();
            var formationButtons = new List<Button>();
            var doctrineButtons = new List<Button>();

            BuildUnionPlannerCommandBar074(
                root.transform,
                state,
                unions,
                union,
                recruits.Length - reserve.Length,
                topButtons);
            BuildUnionPlannerMembers074(
                root.transform,
                union,
                recruits,
                memberButtons);
            var showPostRescueForceOverview076 =
                _returnToExpeditionAfterUnionReview076 &&
                state.OpeningUnionsLegal &&
                unions.Length >= 6 &&
                recruits.Length >= 20 &&
                assignedIds.Count >= 18;
            if (_unionAdvanced132 && showPostRescueForceOverview076)
                BuildPostRescueForceOverview076(
                    root.transform,
                    unions,
                    recruits,
                    reserve.Length,
                    reserveButtons);
            else
                BuildUnionPlannerReserve074(
                    root.transform,
                    union,
                    reserve,
                    reservePageCount,
                    reserveButtons);
            if (_unionAdvanced132) BuildUnionPlannerChoices074(
                root.transform,
                state,
                union,
                formationButtons,
                doctrineButtons);
            var save = BuildUnionPlannerFooter074(root.transform, state, union, topButtons);
            var focusStoryReturn = _returnToExpeditionAfterUnionReview076 &&
                                   !_focusedUnionPlannerStoryReturn076;

            ConfigureUnionPlannerNavigation074(
                topButtons,
                memberButtons,
                reserveButtons,
                formationButtons,
                doctrineButtons,
                save,
                focusStoryReturn);
            if (focusStoryReturn && save != null && save.interactable)
                _focusedUnionPlannerStoryReturn076 = true;
        }

        private void BuildUnionPlannerCommandBar074(
            Transform parent,
            M1PresentationState state,
            IReadOnlyList<M1UnionView> unions,
            M1UnionView selectedUnion,
            int assignedCount,
            ICollection<Button> navigationButtons)
        {
            var bar = AddUnionPlannerPanel074(
                parent,
                "Union Planner Command Bar 074",
                UnionPlannerMajorRegionValues074[0],
                M1PremiumUi.Surface.WorldRibbon);

            var back = AddUnionPlannerButton074(
                bar.transform,
                "Union Planner Back 074",
                "←  BACK",
                BackFromUnionBuilder068,
                new Rect(0.010f, 0.105f, 0.100f, 0.790f),
                RuntimeUi.ButtonNormal,
                18,
                28);
            navigationButtons.Add(back);

            var findHero155 = AddUnionPlannerButton074(
                bar.transform, "Find Union Hero 155",
                "FIND HERO\n" + assignedCount + " / " +
                Math.Max(assignedCount, state.Recruits?.Count ?? 0) + " PLACED",
                FindUnionHero155,
                new Rect(0.122f, 0.105f, 0.165f, 0.790f),
                RuntimeUi.ButtonNormal, 18, 28);
            navigationButtons.Add(findHero155);

            var tabHost = RuntimeUi.AddPanel(
                bar.transform,
                "Union Planner Team Tabs 074",
                new Color(0.012f, 0.022f, 0.032f, 0.72f));
            AnchorUnionPlanner074(tabHost.rectTransform, new Rect(0.300f, 0.080f, 0.420f, 0.840f));
            tabHost.raycastTarget = false;

            var validUnions = (unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .ToArray();
            var visibleIndices = UnionPlannerVisibleUnionIndicesForVerification074(
                validUnions,
                selectedUnion?.Index ?? _selectedUnionIndex);
            var visible = visibleIndices
                .Select(index => validUnions.First(value => value.Index == index))
                .ToList();
            var selectedPosition = selectedUnion == null
                ? 0
                : Array.FindIndex(validUnions, value => value.Index == selectedUnion.Index);
            if (selectedPosition < 0) selectedPosition = 0;
            var pageStart = selectedPosition / UnionPlannerVisibleTabLimit074 *
                            UnionPlannerVisibleTabLimit074;
            var canCreatePlan = validUnions.Length < state.MaximumUnionPlanCount;
            var hasPreviousPage = pageStart > 0;
            var hasNextPage = pageStart + UnionPlannerVisibleTabLimit074 <
                              validUnions.Length;
            // Creation has its own labelled touch target on every page. The
            // small arrows only page through teams; they never create one.
            var hasPageControls = validUnions.Length > UnionPlannerVisibleTabLimit074;
            var tabStart = hasPageControls ? 0.105f : 0f;
            var tabEnd = hasPageControls ? 0.895f : 1f;

            if (hasPageControls)
            {
                var previous = AddUnionPlannerButton074(
                    tabHost.transform,
                    "Previous Union Tab Page 074",
                    "‹",
                    () => SelectUnionPlannerPage074(
                        validUnions,
                        Mathf.Max(0, pageStart - UnionPlannerVisibleTabLimit074)),
                    new Rect(0f, 0f, 0.095f, 1f),
                    RuntimeUi.ButtonNormal,
                    22,
                    34);
                previous.interactable = hasPreviousPage;
                navigationButtons.Add(previous);
            }

            var visibleSlotCount = Mathf.Max(1, visible.Count);
            var availableWidth = tabEnd - tabStart;
            var gap = 0.012f;
            var slotWidth = (availableWidth - gap * (visibleSlotCount - 1)) / visibleSlotCount;
            for (var index = 0; index < visible.Count; index++)
            {
                var candidate = visible[index];
                var capturedIndex = candidate.Index;
                var active = selectedUnion != null && candidate.Index == selectedUnion.Index;
                var name = M1UnionIdentity076.ResolveTab(
                        candidate.UnionId,
                        candidate.DisplayName,
                        candidate.Index)
                    .ToUpperInvariant();
                var members = candidate.MemberRecruitIds?.Count ?? 0;
                var tab = AddUnionPlannerButton074(
                    tabHost.transform,
                    "Union Planner Tab " + candidate.Index + " 074",
                    name + "\n" +
                    members + "/" + NormalUnionPlanRules.MaximumMembersPerUnion +
                    "  •  " + (candidate.IsLegal ? "READY" : "NEEDS SETUP"),
                    () =>
                    {
                        _selectedUnionIndex = capturedIndex;
                        _unionReservePage074 = 0;
                        _unionFormationPage076 = -1;
                        _unionDoctrinePage076 = -1;
                        BuildCurrentScreen();
                    },
                    new Rect(
                        tabStart + index * (slotWidth + gap),
                        0f,
                        slotWidth,
                        1f),
                    active ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                    16,
                    25);
                // Accent already communicates selection, so do not spend scarce
                // label width on a redundant diamond. These command tabs have an
                // intentional name/readiness two-line contract; tab-only padding
                // leaves every authored compact name comfortable on one line.
                var tabLabel = tab.GetComponentInChildren<Text>();
                ConfigureAuthoredCompactText076(
                    tabLabel,
                    22,
                    24);
                tabLabel.rectTransform.offsetMin = new Vector2(12f, 8f);
                tabLabel.rectTransform.offsetMax = new Vector2(-12f, -8f);
                tabLabel.lineSpacing = 0.92f;
                ConfigureUnionPlannerDropTarget074(
                    tab.gameObject,
                    candidate.Index,
                    members < NormalUnionPlanRules.MaximumMembersPerUnion
                        ? members
                        : NormalUnionPlanRules.MaximumMembersPerUnion - 1,
                    availableForReserve:
                        members < NormalUnionPlanRules.MaximumMembersPerUnion,
                    availableForAssigned: true);
                navigationButtons.Add(tab);
            }

            if (hasPageControls)
            {
                var next = AddUnionPlannerButton074(
                    tabHost.transform,
                    "Next Union Tab Page 074",
                    hasNextPage ? "›" : "•",
                    hasNextPage
                        ? (Action)(() => SelectUnionPlannerPage074(
                            validUnions,
                            Mathf.Min(
                                validUnions.Length - 1,
                                pageStart + UnionPlannerVisibleTabLimit074)))
                        : (Action)null,
                    new Rect(0.905f, 0f, 0.095f, 1f),
                    RuntimeUi.ButtonNormal,
                    22,
                    34);
                next.interactable = hasNextPage;
                navigationButtons.Add(next);
            }

            var addUnion165 = AddUnionPlannerButton074(
                bar.transform,
                "Add Union165",
                "+ ADD UNION\n" + validUnions.Length + " / " + state.MaximumUnionPlanCount,
                () => CreateUnionPlannerPlan078(validUnions.Length == 0 ? 0 : validUnions.Max(value => value.Index) + 1),
                new Rect(0.730f, 0.105f, 0.120f, 0.790f),
                RuntimeUi.ButtonNormal, 26, 32);
            addUnion165.interactable = canCreatePlan && UnionEditable132;
            addUnion165.gameObject.AddComponent<UnionAddTouch165>();
            navigationButtons.Add(addUnion165);
            var development = AddUnionPlannerButton074(
                bar.transform,
                "Heroes Development 110",
                "DEVELOPMENT\nXP " + FormatProgressionNumber(state.TreasuryXp),
                () => OpenLivingGuildMemberDevelopment076(_selectedRecruitId),
                new Rect(0.860f, 0.105f, 0.130f, 0.790f),
                RuntimeUi.ButtonNormal, 26, 32);
            navigationButtons.Add(development);
            foreach (var link in new[] { addUnion165, development })
            {
                var label = link.GetComponentInChildren<Text>();
                label.rectTransform.offsetMin = new Vector2(8f, 8f);
                label.rectTransform.offsetMax = new Vector2(-8f, -8f);
            }
        }

        public static string UnionPlannerScaleCopyForVerification078(long spendableMemberXp) =>
            "XP TO SPEND  " + FormatProgressionNumber(spendableMemberXp) +
            "\nFIELD  •  10 UNIONS × 6 = 60" +
            "\nHOUSING III GOAL  •  CAP 75";

        public static string UnionPlannerMemberCardLabelForVerification079(
            string displayName,
            string observedClass,
            int slotIndex,
            bool isLegal)
        {
            var name = string.IsNullOrWhiteSpace(displayName)
                ? "MEMBER"
                : displayName.Trim().ToUpperInvariant();
            // Keep ordinary two-word names on the one identity line reserved by
            // the studio caption rail. A non-breaking word gap lets best-fit retain
            // the complete name and leaves the second line for role/slot status.
            if (name.Length <= 20)
                name = string.Join("\u00A0", name.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries));
            var role = string.IsNullOrWhiteSpace(observedClass)
                ? "ADVENTURER"
                : observedClass.Trim().ToUpperInvariant();
            return name + "\n" + role + "  •  " +
                   (isLegal
                       ? slotIndex == 0 ? "LEADER" : "SLOT " + (slotIndex + 1)
                       : "CHECK LOADOUT");
        }

        private void SelectUnionPlannerPage074(
            IReadOnlyList<M1UnionView> unions,
            int targetPosition)
        {
            if (unions == null || unions.Count == 0) return;
            var safePosition = Mathf.Clamp(targetPosition, 0, unions.Count - 1);
            var target = unions[safePosition];
            if (target == null) return;
            _selectedUnionIndex = target.Index;
            _unionReservePage074 = 0;
            _unionFormationPage076 = -1;
            _unionDoctrinePage076 = -1;
            BuildCurrentScreen();
        }

        private void CreateUnionPlannerPlan078(int expectedIndex)
        {
            if (_coordinator == null) return;
            _selectedUnionIndex = Mathf.Max(0, expectedIndex);
            _unionReservePage074 = 0;
            _unionFormationPage076 = -1;
            _unionDoctrinePage076 = -1;
            Execute(_coordinator.AddUnion());
        }

        private void BuildUnionPlannerMembers074(
            Transform parent,
            M1UnionView union,
            IReadOnlyList<M1RecruitLoadoutView> recruits,
            ICollection<Button> navigationButtons)
        {
            var panel = AddUnionPlannerPanel074(
                parent,
                "Union Planner Selected Team 074",
                UnionRegion132(1),
                M1PremiumUi.Surface.WorldGlass);
            var unionName = union == null
                ? "NO UNION SELECTED"
                : M1UnionIdentity076.Resolve(union.UnionId, union.DisplayName, union.Index)
                    .ToUpperInvariant();
            var memberCount = union?.MemberRecruitIds?.Count ?? 0;
            ConfigureUnionPlannerDropTarget074(
                panel.gameObject,
                union?.Index ?? -1,
                memberCount < NormalUnionPlanRules.MaximumMembersPerUnion
                    ? memberCount
                    : NormalUnionPlanRules.MaximumMembersPerUnion - 1,
                availableForReserve: union != null &&
                                     memberCount < NormalUnionPlanRules.MaximumMembersPerUnion,
                availableForAssigned: union != null &&
                                      memberCount < NormalUnionPlanRules.MaximumMembersPerUnion);
            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Selected Team Heading 074",
                unionName + "   •   " + memberCount + "/" +
                NormalUnionPlanRules.MaximumMembersPerUnion + "   •   " +
                (union?.IsLegal == true ? "READY" : "NEEDS SETUP"),
                new Rect(0.025f, 0.845f, 0.950f, 0.125f),
                18,
                30,
                union?.IsLegal == true ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Member Hint 074",
                string.IsNullOrWhiteSpace(_unionSelectedReserve109)
                    ? "SELECT A RESERVE, THEN A SLOT  •  CLICK A MEMBER TO REMOVE"
                    : "CHOOSE THE NEXT EMPTY SLOT OR AN OCCUPIED SLOT TO SWAP",
                new Rect(0.025f, 0.755f, 0.950f, 0.085f),
                16,
                23,
                RuntimeUi.MutedText,
                FontStyle.Normal,
                TextAnchor.MiddleLeft);

            var memberIds = union?.MemberRecruitIds ?? Array.Empty<string>();
            const float left = 0.020f;
            const float right = 0.980f;
            const float bottom = 0.035f;
            const float top = 0.735f;
            const float horizontalGap = 0.014f;
            const float verticalGap = 0.022f;
            var slotWidth = (right - left -
                             horizontalGap * (UnionPlannerMemberSlotColumns078 - 1)) /
                            UnionPlannerMemberSlotColumns078;
            var slotHeight = (top - bottom -
                              verticalGap * (UnionPlannerMemberSlotRows078 - 1)) /
                             UnionPlannerMemberSlotRows078;
            for (var slotIndex = 0;
                 slotIndex < NormalUnionPlanRules.MaximumMembersPerUnion;
                 slotIndex++)
            {
                var memberId = slotIndex < memberIds.Count ? memberIds[slotIndex] : null;
                var member = (recruits ?? Array.Empty<M1RecruitLoadoutView>())
                    .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, memberId));
                var capturedId = memberId;
                var capturedSlotIndex = slotIndex;
                var column = slotIndex % UnionPlannerMemberSlotColumns078;
                var rowFromTop = slotIndex / UnionPlannerMemberSlotColumns078;
                var label = member == null
                    ? "SLOT " + (slotIndex + 1) +
                      (slotIndex == 0 ? "  •  LEADER" : string.Empty) +
                      (slotIndex == memberIds.Count
                          ? "  •  EMPTY\nSELECT RESERVE, THEN HERE"
                          : "  •  EMPTY\nFILL EARLIER SLOTS FIRST")
                    : UnionPlannerMemberCardLabelForVerification079(
                        member.DisplayName,
                        member.ObservedClass,
                        slotIndex,
                        member.IsLegal);
                var button = AddUnionPlannerButton074(
                    panel.transform,
                    "Union Planner Member Slot " + slotIndex + " 074",
                    label,
                    () => ClickUnionPlannerSlot109(union?.Index ?? -1, capturedSlotIndex, capturedId),
                    new Rect(
                        left + column * (slotWidth + horizontalGap),
                        top - (rowFromTop + 1) * slotHeight -
                        rowFromTop * verticalGap,
                        slotWidth,
                        slotHeight),
                    slotIndex == 0 && member != null
                        ? RuntimeUi.Accent
                        : RuntimeUi.PanelRaised,
                    17,
                    27);
                button.interactable = UnionEditable132 && (member != null ||
                    (union != null && slotIndex == memberIds.Count &&
                     !string.IsNullOrWhiteSpace(_unionSelectedReserve109)));
                if (button.interactable) navigationButtons.Add(button);
                ConfigureUnionPlannerDropTarget074(
                    button.gameObject,
                    union?.Index ?? -1,
                    slotIndex,
                    availableForReserve: union != null && member == null,
                    availableForAssigned: union != null);
                if (member != null)
                {
                    var memberLabel = button.GetComponentInChildren<Text>();
                    AddMenuStandeeToButton090(button, member, large: true);
                    ApplyUnionPlannerRoleScheme074(button, member, large: true);
                    ConfigureUnionPlannerMemberCardText074(button, memberLabel);
                    var drag = button.gameObject.AddComponent<UnionPlannerRecruitDrag074>();
                    drag.Configure(
                        member.RecruitId,
                        UnionPlannerRoleDesignationForVerification074(member.ObservedClass),
                        UnionPlannerRoleColorForVerification074(member.ObservedClass),
                        isAssigned: true,
                        canDrag: UnionEditable132,
                        assign: AssignUnionPlannerRecruit074);
                }
            }
        }

        private void BuildPostRescueForceOverview076(
            Transform parent,
            IReadOnlyList<M1UnionView> unions,
            IReadOnlyList<M1RecruitLoadoutView> recruits,
            int reserveCount,
            ICollection<Button> navigationButtons)
        {
            var panel = AddUnionPlannerPanel074(
                parent,
                "Post Rescue Force Overview 076",
                UnionRegion132(2),
                M1PremiumUi.Surface.WorldRibbon);
            AddUnionPlannerText074(
                panel.transform,
                "Post Rescue Force Overview Heading 076",
                PostRescueForceOverviewHeadingForVerification078(unions, reserveCount),
                new Rect(0.025f, 0.785f, 0.950f, 0.155f),
                17,
                27,
                RuntimeUi.Positive,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            var rects = UnionPlannerForceOverviewTileRectsForVerification076();
            var visible = (unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null)
                .Take(rects.Count)
                .ToArray();
            for (var index = 0; index < visible.Length; index++)
            {
                var candidate = visible[index];
                var capturedIndex = candidate.Index;
                var memberIds = candidate.MemberRecruitIds ?? Array.Empty<string>();
                var leader = recruits?.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, candidate.LeaderRecruitId));
                if (leader == null && memberIds.Count > 0)
                    leader = recruits?.FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(value.RecruitId, memberIds[0]));
                var card = AddUnionPlannerButton074(
                    panel.transform,
                    "Post Rescue Force Union " + candidate.Index + " 076",
                    M1UnionIdentity076.ResolveTab(
                            candidate.UnionId,
                            candidate.DisplayName,
                            candidate.Index)
                        .ToUpperInvariant() + "\n" +
                    "LEADS  " + (leader?.DisplayName ?? "UNASSIGNED") + "\n" +
                    memberIds.Count + "/" + NormalUnionPlanRules.MaximumMembersPerUnion +
                    "  •  " + (candidate.IsLegal ? "READY" : "CHECK PLAN"),
                    () =>
                    {
                        _selectedUnionIndex = capturedIndex;
                        _unionReservePage074 = 0;
                        _unionFormationPage076 = -1;
                        _unionDoctrinePage076 = -1;
                        BuildCurrentScreen();
                    },
                    rects[index],
                    candidate.Index == _selectedUnionIndex
                        ? RuntimeUi.Accent
                        : RuntimeUi.ButtonNormal,
                    14,
                    20);
                if (leader != null)
                {
                    AddMenuStandeeToButton090(card, leader, large: false);
                    ApplyUnionPlannerRoleScheme074(card, leader, large: false);
                }
                ConfigureUnionPlannerDropTarget074(
                    card.gameObject,
                    candidate.Index,
                    memberIds.Count < NormalUnionPlanRules.MaximumMembersPerUnion
                        ? memberIds.Count
                        : NormalUnionPlanRules.MaximumMembersPerUnion - 1,
                    availableForReserve:
                        memberIds.Count < NormalUnionPlanRules.MaximumMembersPerUnion,
                    availableForAssigned: true);
                ConfigureUnionPlannerReserveCardText074(card.GetComponentInChildren<Text>());
                SetUnionPlannerExplicitNavigation074(card);
                navigationButtons.Add(card);
            }
        }

        private void BuildUnionPlannerReserve074(
            Transform parent,
            M1UnionView union,
            IReadOnlyList<M1RecruitLoadoutView> reserve,
            int pageCount,
            ICollection<Button> navigationButtons)
        {
            var panel = AddUnionPlannerPanel074(
                parent,
                "Union Planner Reserve 074",
                UnionRegion132(2),
                M1PremiumUi.Surface.WorldRibbon);
            var count = reserve?.Count ?? 0;
            var full = (union?.MemberRecruitIds?.Count ?? 0) >=
                       NormalUnionPlanRules.MaximumMembersPerUnion;
            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Reserve Heading 074",
                "RESERVE  •  " + count +
                (pageCount > 1 ? "  •  PAGE " + (_unionReservePage074 + 1) + " / " + pageCount : string.Empty),
                new Rect(0.025f, 0.765f, 0.350f, 0.180f),
                18,
                29,
                count == 0 ? RuntimeUi.Positive : RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Reserve Instruction 074",
                count == 0
                    ? "Everyone has a team. Remove a member above to reorganize."
                    : !string.IsNullOrWhiteSpace(_unionSelectedReserve109)
                        ? "Selected. Choose a slot above; tap this reserve again to cancel."
                        : full
                            ? "Select a reserve, then an occupied slot to swap."
                            : "Select a reserve, then the next empty slot above.",
                new Rect(0.375f, 0.765f, 0.600f, 0.180f),
                16,
                23,
                RuntimeUi.MutedText,
                FontStyle.Normal,
                TextAnchor.MiddleRight);

            if (count == 0)
            {
                AddUnionPlannerText074(
                    panel.transform,
                    "Union Planner Empty Reserve 074",
                    "ALL MEMBERS ASSIGNED",
                    new Rect(0.025f, 0.095f, 0.950f, 0.590f),
                    19,
                    31,
                    RuntimeUi.Positive,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                return;
            }

            var hasPaging = pageCount > 1;
            var page = reserve
                .Skip(_unionReservePage074 * UnionPlannerReservePageSize074)
                .Take(UnionPlannerReservePageSize074)
                .ToArray();
            var cardsStart = hasPaging ? 0.125f : page.Length == 1 ? 0.120f : 0.020f;
            var cardsEnd = hasPaging ? 0.875f : page.Length == 1 ? 0.880f : 0.980f;
            if (hasPaging)
            {
                var previous = AddUnionPlannerButton074(
                    panel.transform,
                    "Previous Reserve Page 074",
                    "‹",
                    () =>
                    {
                        _unionReservePage074 = Mathf.Max(0, _unionReservePage074 - 1);
                        BuildCurrentScreen();
                    },
                    new Rect(0.015f, 0.095f, 0.095f, 0.590f),
                    RuntimeUi.ButtonNormal,
                    22,
                    34);
                previous.interactable = _unionReservePage074 > 0;
                SetUnionPlannerExplicitNavigation074(previous);
                navigationButtons.Add(previous);
            }

            // A finite page keeps one calm row and never scrolls. A lone Hall
            // reserve is centered and widened so their name and role read like a
            // person, not an oversized empty strip.
            const float horizontalGap = 0.014f;
            const float verticalGap = 0.025f;
            const float contentBottom = 0.070f;
            const float contentTop = 0.705f;
            var columnCount = Mathf.Min(
                UnionPlannerReserveColumns074,
                Mathf.Max(1, page.Length));
            var rowCount = Mathf.Max(1, Mathf.CeilToInt(page.Length / (float)columnCount));
            var cardWidth = (cardsEnd - cardsStart - horizontalGap * (columnCount - 1)) /
                            columnCount;
            var cardHeight = (contentTop - contentBottom - verticalGap * (rowCount - 1)) /
                             rowCount;
            for (var index = 0; index < page.Length; index++)
            {
                var recruit = page[index];
                var captured = recruit;
                var column = index % columnCount;
                var row = index / columnCount;
                var card = AddUnionPlannerButton074(
                    panel.transform,
                    "Union Planner Reserve Member " + recruit.RecruitId + " 074",
                    (StringComparer.Ordinal.Equals(recruit.RecruitId, _unionSelectedReserve109) ? "SELECTED  •  " : string.Empty) +
                    (recruit.DisplayName ?? "MEMBER").ToUpperInvariant() + "\n" +
                    (recruit.ObservedClass ?? "ADVENTURER").ToUpperInvariant() +
                    "  •  LV " + Math.Max(1, recruit.Level),
                    () => SelectUnionPlannerReserve109(captured.RecruitId),
                    new Rect(
                        cardsStart + column * (cardWidth + horizontalGap),
                        contentTop - (row + 1) * cardHeight - row * verticalGap,
                        cardWidth,
                        cardHeight),
                    RuntimeUi.ButtonNormal,
                    UnionPlannerReserveLabelMinimumFontSize074,
                    21);
                card.interactable = union != null && UnionEditable132;
                var reserveLabel = card.GetComponentInChildren<Text>();
                AddMenuStandeeToButton090(card, recruit, large: false);
                ApplyUnionPlannerRoleScheme074(card, recruit, large: false);
                if (StringComparer.Ordinal.Equals(recruit.RecruitId, _unionSelectedReserve109))
                {
                    var selectedColors = card.colors;
                    selectedColors.normalColor = RuntimeUi.Accent;
                    card.colors = selectedColors;
                }
                ConfigureUnionPlannerReserveCardText074(reserveLabel);
                SetUnionPlannerExplicitNavigation074(card);
                var drag = card.gameObject.AddComponent<UnionPlannerRecruitDrag074>();
                drag.Configure(
                    recruit.RecruitId,
                    UnionPlannerRoleDesignationForVerification074(recruit.ObservedClass),
                    UnionPlannerRoleColorForVerification074(recruit.ObservedClass),
                    isAssigned: false,
                    canDrag: union != null && UnionEditable132,
                    assign: AssignUnionPlannerRecruit074);
                navigationButtons.Add(card);
            }

            if (hasPaging)
            {
                var next = AddUnionPlannerButton074(
                    panel.transform,
                    "Next Reserve Page 074",
                    "›",
                    () =>
                    {
                        _unionReservePage074 = Mathf.Min(pageCount - 1, _unionReservePage074 + 1);
                        BuildCurrentScreen();
                    },
                    new Rect(0.890f, 0.095f, 0.095f, 0.590f),
                    RuntimeUi.ButtonNormal,
                    22,
                    34);
                next.interactable = _unionReservePage074 + 1 < pageCount;
                SetUnionPlannerExplicitNavigation074(next);
                navigationButtons.Add(next);
            }
        }

        private static void SetUnionPlannerExplicitNavigation074(Button button)
        {
            if (button == null) return;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            button.navigation = navigation;
        }

        private static void ConfigureUnionPlannerReserveCardText074(Text label)
        {
            if (label == null) return;
            label.gameObject.name = "Union Planner Reserve Card Label 074 [Readable 079]";
            label.rectTransform.anchorMin = new Vector2(0.34f, 0.04f);
            label.rectTransform.anchorMax = new Vector2(0.78f, 0.96f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            ConfigureAuthoredCompactText076(
                label,
                UnionPlannerReserveLabelMinimumFontSize074,
                UnionPlannerReserveLabelMaximumFontSize079);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.lineSpacing = 0.95f;
            label.alignment = TextAnchor.MiddleLeft;
        }

        private static void ConfigureUnionPlannerMemberCardText074(Button card, Text label)
        {
            if (label == null) return;
            label.gameObject.name =
                "Union Planner Active Member Label 074 [Compact Fixed 074] [Studio Caption Rail 076]";
            label.rectTransform.anchorMin = new Vector2(0.055f, 0.035f);
            label.rectTransform.anchorMax = new Vector2(0.945f, 0.375f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.fontSize = UnionPlannerMemberLabelMaximumFontSize074;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = UnionPlannerMemberLabelMinimumFontSize074;
            label.resizeTextMaxSize = UnionPlannerMemberLabelMaximumFontSize074;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.lineSpacing = 0.84f;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            if (card == null) return;
            var captionRail = RuntimeUi.AddPanel(
                card.transform,
                "Union Planner Member Caption Rail 076",
                Color.white);
            captionRail.raycastTarget = false;
            AnchorUnionPlanner074(
                captionRail.rectTransform,
                UnionPlannerMemberCaptionRailRegion074);
            M1PremiumUi.StyleMemberCaptionRail076(captionRail);
            captionRail.transform.SetSiblingIndex(0);
            label.transform.SetAsLastSibling();

            var portraits = card.GetComponentsInChildren<Image>(true);
            for (var index = 0; index < portraits.Length; index++)
            {
                var portrait = portraits[index];
                if (portrait == null || !portrait.name.StartsWith("Portrait Frame ", StringComparison.Ordinal))
                    continue;
                portrait.rectTransform.anchorMin = UnionPlannerMemberPortraitRegion074.min;
                portrait.rectTransform.anchorMax = UnionPlannerMemberPortraitRegion074.max;
                portrait.rectTransform.offsetMin = Vector2.zero;
                portrait.rectTransform.offsetMax = Vector2.zero;
                break;
            }
        }

        private void SelectUnionPlannerReserve109(string recruitId)
        {
            if (!UnionEditable132) return;
            _unionSelectedReserve109 = StringComparer.Ordinal.Equals(_unionSelectedReserve109, recruitId)
                ? null : recruitId;
            _localStatus = string.Empty;
            BuildCurrentScreen();
        }

        private void ClickUnionPlannerSlot109(int unionIndex, int slotIndex, string occupantId)
        {
            if (!UnionEditable132) return;
            var reserveId = _unionSelectedReserve109;
            if (string.IsNullOrWhiteSpace(reserveId))
            {
                if (string.IsNullOrWhiteSpace(occupantId)) return;
                _unionReservePage074 = 0;
                Execute(_coordinator.UnassignRecruitFromUnion(occupantId));
                return;
            }
            if (string.IsNullOrWhiteSpace(occupantId))
            {
                CommitUnionPlannerReserve109(reserveId, unionIndex, slotIndex, null);
                return;
            }
            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            var incoming = recruits.FirstOrDefault(value => value.RecruitId == reserveId);
            var outgoing = recruits.FirstOrDefault(value => value.RecruitId == occupantId);
            ShowConfirmation(
                "SWAP UNION MEMBER",
                (incoming?.DisplayName ?? "Selected reserve") + " will take slot " + (slotIndex + 1) +
                ". " + (outgoing?.DisplayName ?? "Current member") + " will return to reserve.",
                "SWAP MEMBERS",
                () => CommitUnionPlannerReserve109(reserveId, unionIndex, slotIndex, occupantId));
        }

        private void CommitUnionPlannerReserve109(string recruitId, int unionIndex, int slotIndex, string occupantId)
        {
            if (!(_coordinator is IUnionReserveAssignmentCoordinator109 authority))
            {
                Execute(M1CommandResult.Failure("Reserve placement is unavailable. Reopen this Union screen."));
                return;
            }
            var result132 = authority.AssignReserveRecruitToUnion109(recruitId, unionIndex, slotIndex, occupantId);
            if (result132.Succeeded)
            {
                _unionSelectedReserve109 = null;
                _unionReservePage074 = 0;
            }
            _selectedUnionIndex = unionIndex;
            Execute(result132);
        }

        private void AssignUnionPlannerRecruit074(
            string recruitId,
            int unionIndex,
            int slotIndex)
        {
            if (_coordinator == null || !UnionEditable132 || string.IsNullOrWhiteSpace(recruitId) || unionIndex < 0)
                return;
            _selectedUnionIndex = unionIndex;
            _unionSelectedReserve109 = null;
            _unionReservePage074 = 0;
            Execute(_coordinator.AssignRecruitToUnion(
                recruitId,
                unionIndex,
                Mathf.Max(0, slotIndex)));
        }

        private static void ConfigureUnionPlannerDropTarget074(
            GameObject targetObject,
            int unionIndex,
            int slotIndex,
            bool availableForReserve,
            bool availableForAssigned)
        {
            if (targetObject == null) return;
            var target = targetObject.GetComponent<UnionPlannerDropTarget074>();
            if (target == null) target = targetObject.AddComponent<UnionPlannerDropTarget074>();
            target.Configure(
                unionIndex,
                slotIndex,
                availableForReserve && unionIndex >= 0,
                availableForAssigned && unionIndex >= 0);
        }

        private static void ApplyUnionPlannerRoleScheme074(
            Button card,
            M1RecruitLoadoutView recruit,
            bool large)
        {
            if (card == null || recruit == null) return;
            var designation = UnionPlannerRoleDesignationForVerification074(
                recruit.ObservedClass);
            var roleColor = UnionPlannerRoleColorForVerification074(recruit.ObservedClass);
            var rail = RuntimeUi.AddPanel(
                card.transform,
                "Union Planner Role Color " + designation + " 074",
                roleColor);
            AnchorUnionPlanner074(
                rail.rectTransform,
                large
                    ? new Rect(
                        UnionPlannerMemberPortraitRegion074.xMin,
                        UnionPlannerMemberPortraitRegion074.yMax - 0.035f,
                        UnionPlannerMemberPortraitRegion074.width,
                        0.035f)
                    : new Rect(0.035f, 0.845f, 0.265f, 0.035f));
            rail.raycastTarget = false;
            M1PremiumUi.AddClassCrest(card, recruit.ClassSymbol, recruit.ObservedClass);
        }

        private static int UnionPlannerRoleSortOrder074(string classIdentity)
        {
            switch (UnionPlannerRoleDesignationForVerification074(classIdentity))
            {
                case "WARRIOR": return 0;
                case "GUARDIAN": return 1;
                case "PRIEST": return 2;
                case "MAGE": return 3;
                case "RANGER": return 4;
                case "ROGUE": return 5;
                default: return 6;
            }
        }

        private void BuildUnionPlannerChoices074(
            Transform parent,
            M1PresentationState state,
            M1UnionView union,
            ICollection<Button> formationNavigation,
            ICollection<Button> doctrineNavigation)
        {
            var city = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)
                ?.GuildCity017D;
            var foundingChapter = city != null &&
                                  !IsStoryContractCompleted065(city, FirstStoryContractId065);
            var firstStoryComplete = city != null &&
                                     IsStoryContractCompleted065(city, FirstStoryContractId065);
            var secondStoryComplete = city != null &&
                                      IsStoryContractCompleted065(city, SecondStoryContractId065);
            var panel = AddUnionPlannerPanel074(
                parent,
                "Union Planner Formation And Intent 074",
                UnionPlannerMajorRegionValues074[3],
                M1PremiumUi.Surface.WorldGlass);

            if (foundingChapter)
            {
                AddUnionPlannerText074(
                    panel.transform,
                    "Union Planner Founding Formation Heading 076",
                    "CHOOSE ONE BATTLE FORMATION",
                    new Rect(0.025f, 0.875f, 0.950f, 0.095f),
                    18,
                    29,
                    RuntimeUi.Accent,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft);
                AddUnionPlannerText074(
                    panel.transform,
                    "Union Planner Founding Formation Lesson 076",
                    "Pick a stance. Members automatically use the Arts they have learned.",
                    new Rect(0.025f, 0.735f, 0.950f, 0.125f),
                    15,
                    22,
                    RuntimeUi.Text,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft);
                AddUnionPlannerChoiceCards074(
                    panel.transform,
                    "Formation",
                    state.Formations,
                    union?.FormationId,
                    new Rect(0.025f, 0.305f, 0.950f, 0.405f),
                    union == null
                        ? (Func<string, M1CommandResult>)null
                        : id => _coordinator.SetFormation(union.Index, id),
                    UnionPlannerFormationEffect074,
                    formationNavigation);

                var doctrine = (state.Doctrines ?? Array.Empty<M1ChoiceView>())
                    .FirstOrDefault(value => value != null &&
                                             StringComparer.Ordinal.Equals(
                                                 value.Id,
                                                 union?.DoctrineId));
                AddUnionPlannerText074(
                    panel.transform,
                    "Union Planner Founding Automatic Tactics 076",
                    "BATTLE ORDERS ADAPT FOR YOU  •  " +
                    (doctrine == null || string.IsNullOrWhiteSpace(doctrine.DisplayName)
                        ? "READ THE FIELD"
                        : doctrine.DisplayName.ToUpperInvariant()) +
                    "\nForecasts react to health, AP, morale, and enemy pressure.",
                    new Rect(0.025f, 0.125f, 0.950f, 0.150f),
                    14,
                    21,
                    RuntimeUi.Positive,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft);

                var foundingHiddenFormationCount076 = Math.Max(
                    0,
                    (state.Formations?.Count ?? 0) - UnionPlannerVisibleChoiceLimit074);
                AddUnionPlannerText074(
                    panel.transform,
                    "Union Planner Founding Earned Later 076",
                    foundingHiddenFormationCount076 > 0
                        ? foundingHiddenFormationCount076 + " MORE FORMATIONS UNLOCK THROUGH PLAY"
                        : "MORE FORMATIONS UNLOCK THROUGH PLAY",
                    new Rect(0.025f, 0.025f, 0.950f, 0.075f),
                    14,
                    20,
                    RuntimeUi.MutedText,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                return;
            }

            var unlockedFormations076 = UnionPlannerUnlockedChoicesForVerification076(
                state.Formations,
                union?.FormationId,
                firstStoryComplete,
                secondStoryComplete);
            var unlockedDoctrines076 = UnionPlannerUnlockedChoicesForVerification076(
                state.Doctrines,
                union?.DoctrineId,
                firstStoryComplete,
                secondStoryComplete);
            _unionFormationPage076 = NormalizeUnionPlannerChoicePage076(
                unlockedFormations076,
                union?.FormationId,
                _unionFormationPage076);
            _unionDoctrinePage076 = NormalizeUnionPlannerChoicePage076(
                unlockedDoctrines076,
                union?.DoctrineId,
                _unionDoctrinePage076);
            var formationPageCount076 = UnionPlannerChoicePageCountForVerification076(
                unlockedFormations076.Count);
            var doctrinePageCount076 = UnionPlannerChoicePageCountForVerification076(
                unlockedDoctrines076.Count);

            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Formation Heading 074",
                "FORMATION  •  " + unlockedFormations076.Count + " OF " +
                (state.Formations?.Count ?? 0) + " UNLOCKED  •  PAGE " +
                (_unionFormationPage076 + 1) + "/" + formationPageCount076,
                new Rect(0.025f, 0.885f, 0.620f, 0.085f),
                17,
                24,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            AddUnionPlannerChoiceCards074(
                panel.transform,
                "Formation",
                UnionPlannerChoicePageForVerification076(
                    unlockedFormations076,
                    _unionFormationPage076),
                union?.FormationId,
                new Rect(0.025f, 0.555f, 0.950f, 0.315f),
                union == null
                    ? (Func<string, M1CommandResult>)null
                    : id => _coordinator.SetFormation(union.Index, id),
                UnionPlannerFormationEffect074,
                formationNavigation);
            AddUnionPlannerPager076(
                panel.transform,
                "Formation",
                _unionFormationPage076,
                formationPageCount076,
                new Rect(0.660f, 0.885f, 0.315f, 0.085f),
                page => NavigateUnionPlannerChoicePage076(true, page),
                formationNavigation);

            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Doctrine Heading 074",
                "BATTLE INTENT  •  " + unlockedDoctrines076.Count + " OF " +
                (state.Doctrines?.Count ?? 0) + " UNLOCKED  •  PAGE " +
                (_unionDoctrinePage076 + 1) + "/" + doctrinePageCount076,
                new Rect(0.025f, 0.465f, 0.620f, 0.075f),
                17,
                24,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            AddUnionPlannerChoiceCards074(
                panel.transform,
                "Doctrine",
                UnionPlannerChoicePageForVerification076(
                    unlockedDoctrines076,
                    _unionDoctrinePage076),
                union?.DoctrineId,
                new Rect(0.025f, 0.145f, 0.950f, 0.305f),
                union == null
                    ? (Func<string, M1CommandResult>)null
                    : id => _coordinator.SetDoctrine(union.Index, id),
                UnionPlannerDoctrineEffect074,
                doctrineNavigation);
            AddUnionPlannerPager076(
                panel.transform,
                "Doctrine",
                _unionDoctrinePage076,
                doctrinePageCount076,
                new Rect(0.660f, 0.465f, 0.315f, 0.075f),
                page => NavigateUnionPlannerChoicePage076(false, page),
                doctrineNavigation);

            var hiddenFormationCount = Math.Max(
                0,
                (state.Formations?.Count ?? 0) - unlockedFormations076.Count);
            var hiddenDoctrineCount = Math.Max(
                0,
                (state.Doctrines?.Count ?? 0) - unlockedDoctrines076.Count);
            AddUnionPlannerText074(
                panel.transform,
                "Union Planner Earned Later 074",
                hiddenFormationCount + hiddenDoctrineCount > 0
                    ? "CHAPTER I REWARD ACTIVE  •  " +
                      (hiddenFormationCount + hiddenDoctrineCount) +
                      " MORE PLANS EARNED BY COMPLETING CHAPTER 2"
                    : "ALL CURRENT FORMATIONS AND BATTLE INTENTS UNLOCKED",
                new Rect(0.025f, 0.025f, 0.950f, 0.095f),
                15,
                22,
                RuntimeUi.MutedText,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
        }

        private static int NormalizeUnionPlannerChoicePage076(
            IReadOnlyList<M1ChoiceView> choices,
            string selectedId,
            int currentPage)
        {
            var pageCount = UnionPlannerChoicePageCountForVerification076(
                choices?.Count ?? 0);
            if (currentPage >= 0) return Mathf.Clamp(currentPage, 0, pageCount - 1);
            var selectedIndex = -1;
            for (var index = 0; index < (choices?.Count ?? 0); index++)
            {
                if (!StringComparer.Ordinal.Equals(choices[index]?.Id, selectedId)) continue;
                selectedIndex = index;
                break;
            }
            return selectedIndex < 0
                ? 0
                : Mathf.Clamp(
                    selectedIndex / UnionPlannerVisibleChoiceLimit074,
                    0,
                    pageCount - 1);
        }

        private static void AddUnionPlannerPager076(
            Transform parent,
            string family,
            int page,
            int pageCount,
            Rect region,
            Action<int> navigate,
            ICollection<Button> navigationButtons)
        {
            const float gap = 0.02f;
            var buttonWidth = (region.width - gap) * 0.5f;
            var previous = AddUnionPlannerButton074(
                parent,
                "Union Planner " + family + " Previous Page 076",
                "PREV",
                () => navigate(Math.Max(0, page - 1)),
                new Rect(region.x, region.y, buttonWidth, region.height),
                RuntimeUi.ButtonNormal,
                13,
                19);
            previous.interactable = page > 0;
            navigationButtons.Add(previous);

            var next = AddUnionPlannerButton074(
                parent,
                "Union Planner " + family + " Next Page 076",
                "NEXT",
                () => navigate(Math.Min(pageCount - 1, page + 1)),
                new Rect(region.x + buttonWidth + gap, region.y, buttonWidth, region.height),
                RuntimeUi.ButtonNormal,
                13,
                19);
            next.interactable = page + 1 < pageCount;
            navigationButtons.Add(next);
        }

        private void NavigateUnionPlannerChoicePage076(bool formation, int page)
        {
            if (formation) _unionFormationPage076 = page;
            else _unionDoctrinePage076 = page;
            BuildCurrentScreen();

            var family = formation ? "Formation" : "Doctrine";
            var prefix = "Union Planner " + family + " ";
            var firstChoice = _screenRoot
                ?.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(value => value != null &&
                                         value.interactable &&
                                         value.name.StartsWith(prefix, StringComparison.Ordinal) &&
                                         value.name.IndexOf(" Page 076", StringComparison.Ordinal) < 0);
            if (firstChoice == null) return;
            firstChoice.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstChoice.gameObject);
        }

        private void AddUnionPlannerChoiceCards074(
            Transform parent,
            string family,
            IReadOnlyList<M1ChoiceView> choices,
            string selectedId,
            Rect region,
            Func<string, M1CommandResult> command,
            Func<M1ChoiceView, string> describe,
            ICollection<Button> navigationButtons)
        {
            var visible = UnionPlannerVisibleChoicesForVerification074(choices, selectedId);
            const float gap = 0.014f;
            var cardWidth = (region.width - gap * (UnionPlannerVisibleChoiceLimit074 - 1)) /
                            UnionPlannerVisibleChoiceLimit074;
            for (var index = 0; index < UnionPlannerVisibleChoiceLimit074; index++)
            {
                var anchors = new Rect(
                    region.x + index * (cardWidth + gap),
                    region.y,
                    cardWidth,
                    region.height);
                if (index >= visible.Count)
                {
                    var locked = AddUnionPlannerButton074(
                        parent,
                        "Union Planner Locked " + family + " " + index + " 074",
                        "LOCKED\nEARN THROUGH PLAY",
                        null,
                        anchors,
                        RuntimeUi.PanelRaised,
                        16,
                        24);
                    locked.interactable = false;
                    continue;
                }

                var choice = visible[index];
                var captured = choice;
                var active = StringComparer.Ordinal.Equals(choice.Id, selectedId);
                var name = string.IsNullOrWhiteSpace(choice.DisplayName)
                    ? HumanizePresentationId(choice.Id)
                    : choice.DisplayName.Trim();
                var button = AddUnionPlannerButton074(
                    parent,
                    "Union Planner " + family + " " + choice.Id + " 074",
                    (StringComparer.Ordinal.Equals(family, "Formation")
                        ? UnionPlannerFormationDiagramForVerification078(choice.Id) + "\n"
                        : string.Empty) +
                    (active ? "◆  " : string.Empty) + name.ToUpperInvariant() + "\n" +
                    describe(choice),
                    command == null ? (Action)null : () => Execute(command(captured.Id)),
                    anchors,
                    active ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                    15,
                    24);
                button.interactable = command != null;
                navigationButtons.Add(button);
            }
        }

        public static string UnionPlannerFormationDiagramForVerification078(string formationId)
        {
            switch (formationId ?? string.Empty)
            {
                case "FORMATION_SHIELD_WALL":
                    return "●  ●  ●\n●  ●  ●";
                case "FORMATION_WEDGE":
                    return "    ●\n  ●  ●\n●  ●  ●";
                case "FORMATION_SKIRMISH":
                case "FORMATION_SKIRMISH_LINE":
                    return "●      ●      ●\n  ●      ●      ●";
                default:
                    return "●  ●  ●";
            }
        }

        private Button BuildUnionPlannerFooter074(
            Transform parent,
            M1PresentationState state,
            M1UnionView union,
            ICollection<Button> navigationButtons)
        {
            var footer = AddUnionPlannerPanel074(
                parent,
                "Union Planner Save Bar 074",
                UnionPlannerMajorRegionValues074[4],
                M1PremiumUi.Surface.WorldRibbon);
            var ready = union != null && state.OpeningUnionsLegal;
            var city = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)
                ?.GuildCity017D;
            var foundingChapter = city != null &&
                                  !IsStoryContractCompleted065(city, FirstStoryContractId065);
            var message = !string.IsNullOrWhiteSpace(_localStatus)
                ? _localStatus
                : _coordinator is IUnionPlanningCoordinator132 planning132 ? planning132.UnionPlanStatus132
                : ready
                    ? _returnToChapterTwoAfterUnionRepair076
                        ? "READY  •  These Union plans can now take a Wayglass route."
                        : _returnToExpeditionAfterUnionReview076
                            ? "READY  •  The patrol is safe. These Unions face the Gate-Eater next."
                            : foundingChapter
                                ? "READY  •  Every active Union has members and one clear formation."
                                : "READY  •  Every active Union has members, a formation, and a battle intent."
                    : "TO FINISH  •  Assign every required member and complete each active Union.";
            AddUnionPlannerText074(
                footer.transform,
                "Union Planner Save Guidance 074",
                message,
                new Rect(
                    0.020f,
                    0.120f,
                    0.240f,
                    0.760f),
                16,
                25,
                (!string.IsNullOrWhiteSpace(_localStatus) ? _localStatusPositive : ready && UnionEditable132) ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);
            var duties = AddUnionPlannerButton074(
                footer.transform,
                "Union Planner Member Duties 097",
                "MEMBER\nDUTIES",
                () =>
                {
                    _guildCityTab017D = "DUTIES";
                    _guildCityMoreOpen060 = false;
                    Navigate(M1Screen.GuildOperations);
                },
                new Rect(0.280f, 0.055f, 0.195f, 0.890f),
                RuntimeUi.ButtonNormal,
                15,
                24);
            duties.interactable = city != null && city.IsAvailable;
            // Keep the optional shortcut reachable from the existing explicit
            // navigation chain as well as by touch, without adding a home tile.
            navigationButtons?.Add(duties);
            var options132 = AddUnionPlannerButton074(footer.transform, "Union Planner Options 132",
                _unionAdvanced132 ? "HERO PLACEMENT" : "FORMATION & INTENT",
                () => { _unionAdvanced132 = !_unionAdvanced132; BuildCurrentScreen(); },
                new Rect(0.490f, 0.055f, 0.225f, 0.890f), RuntimeUi.ButtonNormal, 15, 24);
            navigationButtons?.Add(options132);
            var save = AddUnionPlannerButton074(
                footer.transform,
                "Union Planner Save And Return 074",
                _returnToChapterTwoAfterUnionRepair076
                    ? "SAVE & RETURN TO WAYGLASS"
                    : _returnToExpeditionAfterUnionReview076
                        ? "SAVE & FACE THE GATE-EATER"
                        : "SAVE & RETURN TO GUILD",
                SaveUnions,
                new Rect(0.735f, 0.055f, 0.250f, 0.890f),
                RuntimeUi.Accent,
                18,
                29);
            save.interactable = ready;
            return save;
        }

        public static string SuggestedUnionPreviewCopy087(
            IReadOnlyList<M1RecruitLoadoutView> recruits)
        {
            var groups = (recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .GroupBy(value => M1CommandService.SuggestedUnionRole087(
                    value.ObservedClass), StringComparer.Ordinal)
                .OrderBy(value => SuggestedUnionRoleOrder087(value.Key))
                .Select(value => value.Key + " ×" + value.Count())
                .ToArray();
            return "PREVIEW  •  " +
                   (groups.Length == 0
                       ? "NO AVAILABLE MEMBERS"
                       : string.Join("  •  ", groups)) +
                   "  •  Press Apply Role Teams to confirm.";
        }

        static int SuggestedUnionRoleOrder087(string role)
        {
            switch (role)
            {
                case "HEALER": return 0;
                case "TANK": return 1;
                case "MAGE": return 2;
                case "RANGER": return 3;
                case "ROGUE": return 4;
                case "WARRIOR": return 5;
                default: return 6;
            }
        }

        private static string UnionPlannerFormationEffect074(M1ChoiceView choice)
        {
            if (choice == null) return "Earn through Guild play.";
            var starter = StarterFormationDescription073(choice.Id);
            if (!starter.StartsWith("A specialized", StringComparison.Ordinal)) return starter;
            switch (choice.Id)
            {
                case "FORMATION_ARCANE_CIRCLE": return "CHANNEL  •  steadies mystic and support plans";
                case "FORMATION_CRESCENT": return "COUNTER  •  watches flanks under pressure";
                case "FORMATION_RESCUE_COLUMN": return "RESCUE  •  protects recovery and extraction";
                case "FORMATION_ARROWHEAD": return "RUSH  •  drives through one decisive opening";
                case "FORMATION_VEILED_ECHELON": return "AMBUSH  •  conceals a tactical strike";
                default: return "SPECIALIST  •  earned for a later campaign need";
            }
        }

        private static string UnionPlannerDoctrineEffect074(M1ChoiceView choice)
        {
            if (choice == null) return "Earn through Guild play.";
            var starter = StarterDoctrineDescription073(choice.Id);
            if (!starter.StartsWith("A specialized", StringComparison.Ordinal)) return starter;
            switch (choice.Id)
            {
                case "DOCTRINE_MYSTIC_PRESSURE": return "Favors mystic pressure and support forecasts";
                case "DOCTRINE_CONSERVATIVE": return "Favors low-cost guard and retreat forecasts";
                case "DOCTRINE_RESCUE_FIRST": return "Favors rescue, healing, and safe returns";
                case "DOCTRINE_OBJECTIVE_FIRST": return "Favors mission progress over raw damage";
                case "DOCTRINE_AMBUSH": return "Favors surprise, tactics, and focused pressure";
                case "DOCTRINE_SUPPORT_NETWORK": return "Favors plans that strengthen allied Unions";
                default: return "A specialist intent earned later in the campaign";
            }
        }

        private static Image AddUnionPlannerPanel074(
            Transform parent,
            string name,
            Rect anchors,
            M1PremiumUi.Surface surface)
        {
            var panel = RuntimeUi.AddPanel(parent, name, Color.white);
            AnchorUnionPlanner074(panel.rectTransform, anchors);
            M1PremiumUi.StylePanel(panel, surface);
            return panel;
        }

        private static Button AddUnionPlannerButton074(
            Transform parent,
            string name,
            string label,
            Action action,
            Rect anchors,
            Color color,
            int minimumFontSize,
            int maximumFontSize)
        {
            var button = RuntimeUi.AddButton(
                parent,
                name,
                label,
                action,
                RuntimeUi.MinimumTouchPixels,
                color);
            AnchorUnionPlanner074(button.GetComponent<RectTransform>(), anchors);
            ConfigureResponsiveText062(
                button.GetComponentInChildren<Text>(),
                minimumFontSize,
                maximumFontSize);
            return button;
        }

        private static Text AddUnionPlannerText074(
            Transform parent,
            string name,
            string value,
            Rect anchors,
            int minimumFontSize,
            int maximumFontSize,
            Color color,
            FontStyle style,
            TextAnchor alignment)
        {
            var text = RuntimeUi.AddText(
                parent,
                name,
                value,
                maximumFontSize,
                alignment,
                color,
                style);
            AnchorUnionPlanner074(text.rectTransform, anchors);
            ConfigureResponsiveText062(text, minimumFontSize, maximumFontSize);
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureUnionPlannerNavigation074(
            IReadOnlyList<Button> top,
            IReadOnlyList<Button> members,
            IReadOnlyList<Button> reserve,
            IReadOnlyList<Button> formations,
            IReadOnlyList<Button> doctrines,
            Button save,
            bool focusStoryReturn)
        {
            var topActive = ActiveUnionPlannerButtons074(top);
            var memberActive = ActiveUnionPlannerButtons074(members);
            var reserveActive = ActiveUnionPlannerButtons074(reserve);
            var formationActive = ActiveUnionPlannerButtons074(formations);
            var doctrineActive = ActiveUnionPlannerButtons074(doctrines);
            var saveActive = save != null && save.interactable ? save : null;

            LinkUnionPlannerRow074(
                topActive,
                null,
                FirstUnionPlannerButton074(memberActive, reserveActive, formationActive, doctrineActive));
            LinkUnionPlannerRow074(
                memberActive,
                FirstUnionPlannerButton074(topActive),
                FirstUnionPlannerButton074(reserveActive, formationActive, doctrineActive, saveActive == null
                    ? Array.Empty<Button>()
                    : new[] { saveActive }));
            LinkUnionPlannerRow074(
                reserveActive,
                FirstUnionPlannerButton074(memberActive, topActive),
                FirstUnionPlannerButton074(formationActive, doctrineActive, saveActive == null
                    ? Array.Empty<Button>()
                    : new[] { saveActive }));
            LinkUnionPlannerRow074(
                formationActive,
                FirstUnionPlannerButton074(reserveActive, memberActive, topActive),
                FirstUnionPlannerButton074(
                    doctrineActive,
                    saveActive == null ? Array.Empty<Button>() : new[] { saveActive }));
            LinkUnionPlannerRow074(
                doctrineActive,
                FirstUnionPlannerButton074(formationActive),
                saveActive);

            if (saveActive != null)
            {
                SetUnionPlannerNavigation074(
                    saveActive,
                    FirstUnionPlannerButton074(doctrineActive, formationActive, reserveActive),
                    FirstUnionPlannerButton074(topActive),
                    FirstUnionPlannerButton074(doctrineActive, reserveActive, memberActive),
                    FirstUnionPlannerButton074(topActive));
            }

            var initial = focusStoryReturn && saveActive != null
                ? saveActive
                : topActive.FirstOrDefault(value =>
                      value.name.IndexOf("Union Planner Tab", StringComparison.Ordinal) >= 0 &&
                      value.colors.normalColor == RuntimeUi.Accent) ??
                  FirstUnionPlannerButton074(topActive, memberActive, reserveActive,
                      formationActive, doctrineActive);
            if (initial == null) initial = saveActive;
            if (initial == null) return;
            initial.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(initial.gameObject);
        }

        private static Button[] ActiveUnionPlannerButtons074(IReadOnlyList<Button> buttons)
        {
            return (buttons ?? Array.Empty<Button>())
                .Where(value => value != null && value.interactable && value.gameObject.activeInHierarchy)
                .ToArray();
        }

        private static Button FirstUnionPlannerButton074(params IReadOnlyList<Button>[] groups)
        {
            if (groups == null) return null;
            foreach (var group in groups)
            {
                if (group == null) continue;
                for (var index = 0; index < group.Count; index++)
                    if (group[index] != null && group[index].interactable)
                        return group[index];
            }
            return null;
        }

        private static void LinkUnionPlannerRow074(
            IReadOnlyList<Button> row,
            Button up,
            Button down)
        {
            if (row == null || row.Count == 0) return;
            for (var index = 0; index < row.Count; index++)
            {
                var current = row[index];
                SetUnionPlannerNavigation074(
                    current,
                    row[(index - 1 + row.Count) % row.Count],
                    row[(index + 1) % row.Count],
                    up,
                    down);
            }
        }

        private static void SetUnionPlannerNavigation074(
            Selectable selectable,
            Selectable left,
            Selectable right,
            Selectable up,
            Selectable down)
        {
            if (selectable == null) return;
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            selectable.navigation = navigation;
        }

        private static void AnchorUnionPlanner074(RectTransform rect, Rect anchors)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
            rect.anchorMax = new Vector2(anchors.xMax, anchors.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
