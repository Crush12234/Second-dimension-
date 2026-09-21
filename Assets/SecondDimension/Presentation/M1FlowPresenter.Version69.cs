using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private bool _firstHourCharterRoute071;
        private int _foundingBriefingPage076;

        /// <summary>
        /// Creates the deterministic opening party through the same saved gameplay
        /// commands as the detailed charter flow after the focused story charter.
        /// Players may still use CUSTOMIZE FOUNDERS when they want the full editor.
        /// </summary>
        private void BeginPlayableGuild069()
        {
            BeginPlayableGuild069Core(enterHallDirectlyAfterPreparation071: false);
        }

        /// <summary>
        /// Completing the authored Market arrival is itself the player's charter
        /// commitment. Prepare the same deterministic saved party, then continue
        /// into the playable Hall without inserting presentation dashboards.
        /// </summary>
        private void BeginPlayableGuildFromArrival071()
        {
            if (string.IsNullOrWhiteSpace(_guildmasterDraft))
                _guildmasterDraft = "Skyhome Guildmaster";

            _firstHourCharterRoute071 = true;
            BeginPlayableGuild069Core(enterHallDirectlyAfterPreparation071: true);
        }

        private void BeginPlayableGuild069Core(bool enterHallDirectlyAfterPreparation071)
        {
            if (string.IsNullOrWhiteSpace(_guildmasterDraft))
            {
                _localStatus = "Enter a Guildmaster name, then start the adventure.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                return;
            }

            // Keep compatibility coordinators on their established one-click path.
            // The shipping runtime receives the authored arrival before any campaign
            // state is created, then returns here either from the Market Hall door
            // or from the emergency-charter recovery surface.
            if (_coordinator is M1RuntimeCoordinator && !_firstHourCharterRoute071)
            {
                _localStatus = string.Empty;
                _localStatusPositive = false;
                _screen = M1Screen.FirstHourOpening;
                BuildCurrentScreen();
                return;
            }

            var intent = NewGuildIntent069(_guildmasterDraft.Trim(), _selectedModeId);
            var selectedMode = (_coordinator.State.Modes ?? Array.Empty<M1ModeView>())
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.Id, _selectedModeId));
            if (selectedMode != null && selectedMode.RequiresPermanentConsequenceConfirmation)
            {
                ShowConfirmation(
                    "IRON GUILD WARNING",
                    "Iron Guild increases enemy pressure, injury severity, and recovery demands. Signed members remain permanent.",
                    "START IRON ADVENTURE",
                    () => CreatePlayableGuild069(
                        intent,
                        launchQuickBattle: false,
                        enterHallDirectlyAfterPreparation071: enterHallDirectlyAfterPreparation071));
                return;
            }

            CreatePlayableGuild069(
                intent,
                launchQuickBattle: false,
                enterHallDirectlyAfterPreparation071: enterHallDirectlyAfterPreparation071);
        }

        private void BeginQuickBattle069()
        {
            if (!_coordinator.State.HasCampaign)
            {
                var intent = NewGuildIntent069("Skyhome Guildmaster", "Standard");
                CreatePlayableGuild069(intent, launchQuickBattle: true);
                return;
            }

            if (!_coordinator.State.OpeningUnionsLegal)
            {
                _localStatus = "Quick Battle is ready as soon as the founding party is complete. Continue setup from the gold action.";
                _localStatusPositive = false;
                PlayNowFromTitle062();
                return;
            }

            OpenBattleNow060();
        }

        private M1NewGuildIntent NewGuildIntent069(string guildmasterName, string modeId)
        {
            return new M1NewGuildIntent
            {
                GuildmasterName = guildmasterName,
                ModeId = string.IsNullOrWhiteSpace(modeId) ? "Standard" : modeId,
                TutorialDepthId = "Fast Charter",
                TextScale = _textScale,
                HighContrast = _highContrast,
                ReducedMotion = _reducedMotion
            };
        }

        private void CreatePlayableGuild069(
            M1NewGuildIntent intent,
            bool launchQuickBattle,
            bool enterHallDirectlyAfterPreparation071 = false)
        {
            M1CommandResult last = null;
            var previousBuilding = _building;
            _building = true;
            try
            {
                last = launchQuickBattle
                    ? RunPlayableGuildCommands069(intent)
                    : _coordinator.CreateGuild(intent);
            }
            finally
            {
                _building = previousBuilding;
                _localStatus = last?.Message ?? "The playable opening could not be prepared.";
                _localStatusPositive = last != null && last.Succeeded;
            }

            if (last == null || !last.Succeeded)
            {
                _screen = _coordinator.State.ResumeScreen;
                BuildCurrentScreen();
                return;
            }

            if (launchQuickBattle)
            {
                OpenBattleNow060();
                return;
            }

            // The player now meets the exact six applicants the campaign will sign.
            // The prior route silently signed a different canonical board after
            // displaying recurring applicants, which made the roster feel arbitrary.
            ResetFoundingPreparation078();
            _foundingBriefingPage076 = 0;
            _screen = M1Screen.FirstHourGuildReady;
            _localStatus = enterHallDirectlyAfterPreparation071
                ? "Charter saved. Meet the people who answered it."
                : string.Empty;
            _localStatusPositive = true;
            BuildCurrentScreen();
        }

        private M1CommandResult RunPlayableGuildCommands069(M1NewGuildIntent intent)
        {
            var result = _coordinator.CreateGuild(intent);
            if (!result.Succeeded) return result;

            return RunPlayableGuildPreparation069();
        }

        private M1CommandResult RunPlayableGuildPreparation069()
        {
            M1CommandResult result;

            var founderIds = (_coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(6)
                .Select(value => value.RecruitId)
                .ToArray();
            if (founderIds.Length != 6)
                return M1CommandResult.Failure(
                    "The six founding companions could not be prepared. Use CUSTOMIZE FOUNDERS and try again.");

            foreach (var recruitId in founderIds)
            {
                var applicant = (_coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>())
                    .FirstOrDefault(value => value != null &&
                                             StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
                if (applicant != null && applicant.IsSigned) continue;
                result = _coordinator.SignRecruit(recruitId);
                if (!result.Succeeded) return result;
            }

            result = _coordinator.CompleteEquipmentReview();
            if (!result.Succeeded) return result;

            result = BuildSafeOpeningUnions069(founderIds);
            if (!result.Succeeded) return result;

            result = _coordinator.SaveAndReloadProof();
            if (!result.Succeeded) return result;

            if (_firstHourCharterRoute071 && _coordinator is M1RuntimeCoordinator)
            {
                result = FillFirstHourReadyUnions071(founderIds);
                if (!result.Succeeded) return result;
                result = _coordinator.SaveAndReloadProof();
            }
            return result;
        }

        private void WelcomeFoundingCompany076()
        {
            M1CommandResult result = null;
            var previousBuilding = _building;
            _building = true;
            try
            {
                result = RunPlayableGuildPreparation069();
            }
            finally
            {
                _building = previousBuilding;
                _localStatus = result?.Message ??
                               "The founding company could not be recorded.";
                _localStatusPositive = result != null && result.Succeeded;
            }

            if (result == null || !result.Succeeded)
            {
                var recruitCount = _coordinator.State.Recruits?.Count ?? 0;
                var unionCount = _coordinator.State.Unions?.Count ?? 0;
                if (ShouldRecoverFoundingPreparationInUnionPlanner076(
                        result != null && result.Succeeded,
                        recruitCount,
                        unionCount))
                {
                    var firstUnion = _coordinator.State.Unions
                        .FirstOrDefault(value => value != null);
                    if (firstUnion != null) _selectedUnionIndex = firstUnion.Index;
                    _localStatus =
                        "An older partial party was found. Drag the color-coded members into complete Unions, then choose SAVE & RETURN TO GUILD.";
                    _localStatusPositive = false;
                    _screen = M1Screen.UnionBuilder;
                    BuildCurrentScreen();
                    return;
                }

                BuildCurrentScreen();
                return;
            }

            _foundingBriefingPage076 = 1;
            BuildCurrentScreen();
        }

        public static bool ShouldRecoverFoundingPreparationInUnionPlanner076(
            bool preparationSucceeded,
            int recruitCount,
            int unionCount)
        {
            return !preparationSucceeded && recruitCount >= 6 && unionCount > 0;
        }

        private M1CommandResult FillFirstHourReadyUnions071(string[] founderIds)
        {
            var recruits = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            var unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                value.MemberRecruitIds != null &&
                                value.MemberRecruitIds.Count > 0)
                .Take(3)
                .ToArray();
            var charterRecruits = recruits
                .Where(value => !founderIds.Contains(value.RecruitId, StringComparer.Ordinal))
                .Take(4)
                .ToArray();
            if (unions.Length != 3 || charterRecruits.Length != 4)
            {
                return M1CommandResult.Failure(
                    "The ten-person charter could not be arranged safely. Open CUSTOMIZE FOUNDERS to review the roster.");
            }

            // Bessa is the named ready reserve for the first deployment. Tala,
            // Orren, and Vaelis each complete one three-person Union, preserving
            // the director's road/flank/rear story roles and the 3-member cap.
            var reserve = charterRecruits.FirstOrDefault(value =>
                              value.DisplayName != null &&
                              value.DisplayName.StartsWith("Bessa ", StringComparison.OrdinalIgnoreCase)) ??
                          charterRecruits[2];
            var activeCharter = charterRecruits
                .Where(value => !ReferenceEquals(value, reserve))
                .Take(3)
                .ToArray();
            for (var index = 0; index < activeCharter.Length; index++)
            {
                var union = unions[index];
                if (union.MemberRecruitIds.Count >= 3)
                {
                    return M1CommandResult.Failure(
                        "A first-hour Union is already full. Open CUSTOMIZE FOUNDERS to review it safely.");
                }
                var assignment = _coordinator.AssignRecruitToUnion(
                    activeCharter[index].RecruitId,
                    union.Index,
                    union.MemberRecruitIds.Count);
                if (!assignment.Succeeded) return assignment;
            }

            unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                value.MemberRecruitIds != null &&
                                value.MemberRecruitIds.Count > 0)
                .Take(3)
                .ToArray();
            var activeIds = unions
                .SelectMany(value => value.MemberRecruitIds)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var reserveCount = recruits.Count(value =>
                !activeIds.Contains(value.RecruitId, StringComparer.Ordinal));
            if (unions.Length != 3 ||
                unions.Any(value => value.MemberRecruitIds.Count != 3) ||
                activeIds.Length != 9 ||
                reserveCount != 1 ||
                !_coordinator.State.OpeningUnionsLegal)
            {
                return M1CommandResult.Failure(
                    "The first deployment must contain three three-person Unions and one reserve.");
            }

            return M1CommandResult.Success(
                "Three active Unions are saved. " + reserve.DisplayName +
                " is the named reserve and can rotate in at the Party Table.");
        }

        private M1CommandResult BuildSafeOpeningUnions069(string[] founderIds)
        {
            var unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            var shippingRuntime = _coordinator is M1RuntimeCoordinator;
            var targetUnionCount = shippingRuntime ? 3 : 2;
            if (unions.Count == 0)
            {
                var add = _coordinator.AddUnion();
                if (!add.Succeeded) return add;
                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            }

            while (unions.Count < targetUnionCount)
            {
                var add = _coordinator.AddUnion();
                if (!add.Succeeded) return add;
                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            }

            // Remove only empty overflow drafts above this route's target so an
            // existing player's occupied choices are never discarded.
            while (unions.Count > targetUnionCount)
            {
                var overflow = unions.Last();
                if (overflow.MemberRecruitIds != null && overflow.MemberRecruitIds.Count > 0)
                    return M1CommandResult.Failure("The opening party contains an unexpected occupied Union. Open CUSTOMIZE FOUNDERS to review it safely.");
                var remove = _coordinator.RemoveUnion(overflow.Index);
                if (!remove.Succeeded) return remove;
                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            }

            // CreateGuild already supplies two automatic three-person founder
            // Unions. Adding the third draft above therefore does not imply that
            // this method created the first two drafts. Recognize the exact fresh
            // 3+3+0 founder layout itself, then split it into the intended 2+2+2.
            // The set check keeps this path from rewriting an established roster.
            var freshFounderSet069 = new HashSet<string>(founderIds, StringComparer.Ordinal);
            var freshFounderAssignments069 = unions
                .Take(3)
                .SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>())
                .ToArray();
            var hasFreshThreeUnionLayout069 = shippingRuntime &&
                                               unions.Count >= 3 &&
                                               unions[0].MemberRecruitIds.Count == 3 &&
                                               unions[1].MemberRecruitIds.Count == 3 &&
                                               unions[2].MemberRecruitIds.Count == 0 &&
                                               freshFounderAssignments069.Length == 6 &&
                                               freshFounderSet069.SetEquals(freshFounderAssignments069);
            if (hasFreshThreeUnionLayout069)
            {
                var thirdUnion = unions[2];
                var firstFounderToMove069 = unions[0].MemberRecruitIds[2];
                var secondFounderToMove069 = unions[1].MemberRecruitIds[2];
                var moveThirdFounder = _coordinator.AssignRecruitToUnion(
                    firstFounderToMove069, thirdUnion.Index, 0);
                if (!moveThirdFounder.Succeeded) return moveThirdFounder;

                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
                thirdUnion = unions[2];
                var moveSixthFounder = _coordinator.AssignRecruitToUnion(
                    secondFounderToMove069, thirdUnion.Index, 1);
                if (!moveSixthFounder.Succeeded) return moveSixthFounder;

                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            }

            if (!_coordinator.State.OpeningUnionsLegal)
            {
                for (var index = 0; index < founderIds.Length; index++)
                {
                    var unionIndex = index < 3 ? unions[0].Index : unions[1].Index;
                    var slotIndex = index % 3;
                    var assignment = _coordinator.AssignRecruitToUnion(founderIds[index], unionIndex, slotIndex);
                    if (!assignment.Succeeded) return assignment;
                }

                unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
                var formationId = (_coordinator.State.Formations ?? Array.Empty<M1ChoiceView>())
                    .FirstOrDefault()?.Id;
                var doctrineId = (_coordinator.State.Doctrines ?? Array.Empty<M1ChoiceView>())
                    .FirstOrDefault()?.Id;
                foreach (var union in unions.Take(targetUnionCount))
                {
                    if (!string.IsNullOrWhiteSpace(formationId))
                    {
                        var formation = _coordinator.SetFormation(union.Index, formationId);
                        if (!formation.Succeeded) return formation;
                    }
                    if (!string.IsNullOrWhiteSpace(doctrineId))
                    {
                        var doctrine = _coordinator.SetDoctrine(union.Index, doctrineId);
                        if (!doctrine.Succeeded) return doctrine;
                    }
                }
            }

            var usedUnionCount = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Count(value => value.MemberRecruitIds != null && value.MemberRecruitIds.Count > 0);
            var targetIsReady = !shippingRuntime || usedUnionCount >= targetUnionCount;
            return _coordinator.State.OpeningUnionsLegal && targetIsReady
                ? M1CommandResult.Success(
                    "Your six founders are equipped and arranged into " +
                    targetUnionCount + " ready Unions.")
                : M1CommandResult.Failure(
                    "The automatic party could not be validated. Open CUSTOMIZE FOUNDERS to review the Union plans.");
        }

        /// <summary>
        /// First of two focused opening screens. This is intentionally local
        /// presentation state: the authoritative campaign is created only after
        /// the player signs the emergency charter through the existing command path.
        /// </summary>
        private void BuildFirstHourOpening071()
        {
            if (string.IsNullOrWhiteSpace(_guildmasterDraft))
                _guildmasterDraft = "Skyhome Guildmaster";

            var body = CreatePage(
                "SKYHOME ARRIVAL",
                "CHAPTER 1  •  THE BELL BENEATH SKYHOME  •  STEP 1 OF 2",
                () =>
                {
                    _firstHourCharterRoute071 = false;
                    Navigate(M1Screen.NewGuild);
                });

            var arrival = AddRow(body, "First Hour Skyhome Arrival 071", 22f, 690f);
            AddVisualSliceArtwork062(
                arrival,
                "First Hour Bell Key Art 071",
                FirstContractKeyArtResource062,
                1.24f,
                650f,
                "THE BELL BENEATH SKYHOME");

            var story = AddColumnPanel(
                arrival,
                "THE EMERGENCY CHARTER",
                0.96f,
                650f);
            AddResponsiveText062(
                story,
                "First Hour Arrival Lead 071",
                "Skyhome hangs between broken worlds. Tonight, a bell is ringing beneath its Guild Hall—yet the bell has no rope.",
                23,
                34,
                132f,
                RuntimeUi.Text,
                FontStyle.Bold,
                TextAnchor.UpperLeft);
            AddMessagePanel(
                story,
                "KIRI AETHERHEART  •  FOUNDER",
                "“The Founders guard the gates. Your guild protects whoever falls between them.”",
                RuntimeUi.Warning);
            AddMessagePanel(
                story,
                "MAREN HOLT  •  FIRST COMPANION",
                "“Then give us a charter, and we'll carry our share. Tell us who needs help first.”",
                RuntimeUi.Accent);

            var promise = AddRow(body, "First Hour Charter Promise 071", 18f, 430f);
            var marenPortrait = RuntimeUi.AddPanel(
                promise,
                "Maren Holt Charter Portrait 071",
                Color.white);
            RuntimeUi.SetLayout(
                marenPortrait,
                preferredHeight: 410f,
                flexibleWidth: 0.72f);
            PopulatePortraitFrame(
                marenPortrait,
                "SIGREC_MAREN_HOLT",
                "FH071_MAREN_HOLT",
                "HUMAN",
                "SIGREC_MAREN_HOLT",
                "Maren Holt",
                "Human • Skyhome",
                "MAREN HOLT\nGUARDIAN • FIRST COMPANION");

            var terms = AddColumnPanel(
                promise,
                "WHAT YOU ARE SIGNING",
                1.48f,
                410f);
            AddResponsiveText062(
                terms,
                "Emergency Charter Terms 071",
                "Take command of the ruined Skyhome Guild. Your first Union is already volunteering. Answer the impossible bell, find the missing Lantern Patrol, and bring them home.",
                23,
                34,
                150f,
                RuntimeUi.Text,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            M1PremiumUi.AddSectionDivider(terms, "GUILDMASTER SIGNATURE", "✦");
            var guildmasterName = RuntimeUi.AddInputField(
                terms,
                "First Hour Guildmaster Name 071",
                "Enter a Guildmaster name",
                28);
            guildmasterName.text = _guildmasterDraft;
            guildmasterName.onValueChanged.AddListener(value =>
                _guildmasterDraft = value ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(_localStatus))
                AddStatus(terms, _localStatus, _localStatusPositive);
            RuntimeUi.AddButton(
                terms,
                "Sign Emergency Charter 071",
                "SIGN EMERGENCY CHARTER",
                () =>
                {
                    _firstHourCharterRoute071 = true;
                    _localStatus = "Emergency charter signed. Your guild is being assembled and saved.";
                    _localStatusPositive = true;
                    BeginPlayableGuild069();
                },
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
        }

        private void BuildFirstHourGuildReady071()
        {
            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activeContent = null;
            _activeScroll = null;

            var stage = RuntimeUi.AddPanel(
                _screenRoot,
                "Founding Company Presentation 076",
                new Color(0.006f, 0.014f, 0.022f, _highContrast ? 0.98f : 0.91f));
            Stretch(stage.rectTransform);
            stage.raycastTarget = false;
            _activePage = stage.rectTransform;
            M1PremiumUi.StylePage(stage, _highContrast);

            // Shipping players make five small, visible Guildmaster decisions
            // before the rescue roster is finalized.  Compatibility coordinators
            // retain the established summary surface, while the runtime path uses
            // the same persisted recruit/equipment/Union commands as the full UI.
            if (ShouldBuildFoundingPreparation078())
            {
                BuildFoundingPreparation078(stage.transform);
                return;
            }

            if (_foundingBriefingPage076 <= 0 ||
                !_coordinator.State.OpeningUnionsLegal ||
                (_coordinator.State.Recruits?.Count ?? 0) < 10)
            {
                _foundingBriefingPage076 = 0;
                BuildFoundingApplicants076(stage.transform);
                return;
            }

            BuildFoundingUnionBriefing076(stage.transform);
        }

        private static readonly Rect FoundingApplicantGridRegion076 =
            new Rect(0.018f, 0.285f, 0.964f, 0.435f);

        public const float FoundingCompanyUnionCardHeightFraction078 = 0.32f;

        private static readonly Rect FoundingCompanyTeamJobRibbonRegion078 =
            new Rect(0.34f, 0.75f, 0.63f, 0.19f);

        private static readonly Rect FoundingCompanyLeaderClassPlateRegion078 =
            new Rect(0.34f, 0.615f, 0.63f, 0.095f);

        public static Rect FoundingCompanyTeamJobRibbonRectForVerification078 =>
            FoundingCompanyTeamJobRibbonRegion078;

        public static Rect FoundingCompanyLeaderClassPlateRectForVerification078 =>
            FoundingCompanyLeaderClassPlateRegion078;

        public static IReadOnlyList<Rect> FoundingApplicantCardRectsForVerification076(
            int visibleCount)
        {
            var count = Mathf.Clamp(visibleCount, 0, 6);
            if (count == 0) return Array.Empty<Rect>();

            var rects = new List<Rect>(count);
            const int columns = 3;
            const float horizontalGap = 0.012f;
            const float verticalGap = 0.018f;
            var region = FoundingApplicantGridRegion076;
            var cardWidth = (region.width - horizontalGap * (columns - 1)) / columns;
            var cardHeight = (region.height - verticalGap) / 2f;
            for (var index = 0; index < count; index++)
            {
                var column = index % columns;
                var rowFromTop = index / columns;
                rects.Add(new Rect(
                    region.xMin + column * (cardWidth + horizontalGap),
                    region.yMax - (rowFromTop + 1) * cardHeight - rowFromTop * verticalGap,
                    cardWidth,
                    cardHeight));
            }
            return rects.AsReadOnly();
        }

        private void BuildFoundingApplicants076(Transform parent)
        {
            var applicants = (_coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(6)
                .ToArray();

            AddFoundingHeading076(
                parent,
                "THE SIX WHO ANSWERED",
                "FOUNDING DESK  •  SIX VOLUNTEERS ANSWERED YOUR CHARTER");

            var story = RuntimeUi.AddPanel(
                parent,
                "Founding Applicants Story 076",
                new Color(0.025f, 0.045f, 0.055f, 0.96f));
            AnchorStudioRect076(
                story.rectTransform,
                new Vector2(0.018f, 0.735f),
                new Vector2(0.982f, 0.835f));
            M1PremiumUi.StylePanel(story, M1PremiumUi.Surface.WorldRibbon);
            var quote = RuntimeUi.AddText(
                story.transform,
                "Kiri Founding Quote 076",
                "KIRI  •  “The Lantern Patrol is missing. Six recruits answered. Learn their strengths, then form the rescue team.”",
                24,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorStudioRect076(
                quote.rectTransform,
                new Vector2(0.035f, 0.12f),
                new Vector2(0.965f, 0.88f));
            ConfigureResponsiveText062(quote, 17, 26);

            var cardRects = FoundingApplicantCardRectsForVerification076(applicants.Length);
            for (var index = 0; index < 6; index++)
            {
                if (index >= applicants.Length) break;
                var applicant = applicants[index];
                var role = UnionPlannerRoleDesignationForVerification074(
                    applicant.ObservedClass);
                var roleColor = UnionPlannerRoleColorForVerification074(
                    applicant.ObservedClass);
                var card = RuntimeUi.AddButton(
                    parent,
                    "Founding Applicant " + applicant.RecruitId + " 076",
                    (applicant.DisplayName ?? "Recruit").ToUpperInvariant() + "\n" +
                    (applicant.ObservedClass ?? "Role pending"),
                    null,
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.018f, 0.040f, 0.060f, 0.98f));
                card.interactable = false;
                AnchorStudioRect076(
                    card.GetComponent<RectTransform>(),
                    cardRects[index].min,
                    cardRects[index].max);
                AddPortraitToButton(
                    card,
                    applicant,
                    large: false,
                    cropToFill: true);
                var label = card.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.rectTransform.anchorMax = new Vector2(0.78f, 0.68f);
                    label.rectTransform.offsetMax = Vector2.zero;
                }
                var roleBadge = RuntimeUi.AddPanel(
                    card.transform,
                    "Founding Applicant Role Color " + applicant.RecruitId + " 076",
                    roleColor);
                roleBadge.raycastTarget = false;
                AnchorStudioRect076(
                    roleBadge.rectTransform,
                    new Vector2(0.34f, 0.72f),
                    new Vector2(0.78f, 0.94f));
                var roleText = RuntimeUi.AddText(
                    roleBadge.transform,
                    "Founding Applicant Role " + applicant.RecruitId + " 076",
                    role,
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Background,
                    FontStyle.Bold);
                Stretch(roleText.rectTransform);
                roleText.raycastTarget = false;
                ConfigureResponsiveText062(roleText, 13, 20);
                M1PremiumUi.AddClassCrest(card, applicant.ClassSymbol, applicant.ObservedClass);
                if (label != null) ConfigureResponsiveText062(label, 13, 20);
            }

            var terms = RuntimeUi.AddPanel(
                parent,
                "Founding Applicants Terms 076",
                new Color(0.012f, 0.030f, 0.045f, 0.96f));
            AnchorStudioRect076(
                terms.rectTransform,
                new Vector2(0.018f, 0.035f),
                new Vector2(0.705f, 0.255f));
            M1PremiumUi.StylePanel(terms, M1PremiumUi.Surface.EtchedGlass);
            var termsText = RuntimeUi.AddText(
                terms.transform,
                "Founding Applicants Terms Text 076",
                "CLASS COLORS HELP YOU READ EACH ROLE\nBlue Guardian • Red Warrior • Green Ranger • Orange Rogue • Violet Mage • Gold Priest",
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorStudioRect076(
                termsText.rectTransform,
                new Vector2(0.035f, 0.26f),
                new Vector2(0.965f, 0.90f));
            ConfigureResponsiveText062(termsText, 16, 24);

            if (!string.IsNullOrWhiteSpace(_localStatus) && !_localStatusPositive)
            {
                var status = RuntimeUi.AddText(
                    terms.transform,
                    "Founding Applicants Failure 076",
                    "SETUP NEEDS ATTENTION  •  " + _localStatus,
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Warning,
                    FontStyle.Bold);
                AnchorStudioRect076(
                    status.rectTransform,
                    new Vector2(0.035f, 0.035f),
                    new Vector2(0.965f, 0.28f));
                ConfigureResponsiveText062(status, 14, 19);
            }

            var welcome = RuntimeUi.AddButton(
                parent,
                "Welcome Founding Company 076",
                applicants.Length == 6
                    ? "WELCOME THE SIX FOUNDERS"
                    : "FOUNDERS ARE STILL ARRIVING",
                applicants.Length == 6 ? (Action)WelcomeFoundingCompany076 : null,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                welcome.GetComponent<RectTransform>(),
                new Vector2(0.725f, 0.055f),
                new Vector2(0.982f, 0.225f));
            welcome.interactable = applicants.Length == 6;
            ConfigureResponsiveText062(welcome.GetComponentInChildren<Text>(), 18, 28);
            if (welcome.interactable) welcome.Select();
        }

        private void BuildFoundingUnionBriefing076(Transform parent)
        {
            var recruits = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Take(10)
                .ToArray();
            var unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                value.MemberRecruitIds != null &&
                                value.MemberRecruitIds.Count > 0)
                .Take(3)
                .ToArray();
            var reserveRecruits = recruits
                .Where(recruit => !unions.Any(union =>
                    union.MemberRecruitIds.Any(memberId =>
                        StringComparer.Ordinal.Equals(memberId, recruit.RecruitId))))
                .ToArray();

            AddFoundingHeading076(
                parent,
                "YOUR FOUNDING COMPANY",
                recruits.Length + " GUILDMATES  •  " + unions.Length +
                " FIELD UNIONS  •  " + reserveRecruits.Length + " IN RESERVE");

            const float unionStartX = 0.018f;
            const float unionGap = 0.014f;
            const float unionWidth = 0.312f;
            for (var index = 0; index < Math.Min(3, unions.Length); index++)
            {
                var union = unions[index];
                var members = union.MemberRecruitIds
                    .Select(recruitId => recruits.FirstOrDefault(value =>
                        StringComparer.Ordinal.Equals(value.RecruitId, recruitId)))
                    .Where(value => value != null)
                    .ToArray();
                var leader = members.FirstOrDefault();
                var wingNames = members.Skip(1).Select(value => value.DisplayName).ToArray();
                var leaderRole = UnionPlannerRoleDesignationForVerification074(
                    leader?.ObservedClass);
                var leaderRoleColor = UnionPlannerRoleColorForVerification074(
                    leader?.ObservedClass);
                var teamJob = FoundingDestinationRibbonForVerification078(index);
                var teamJobColor = FoundingDestinationColorForVerification078(index);
                var panel = RuntimeUi.AddButton(
                    parent,
                    "Founding Union Summary " + union.Index + " 076",
                    M1UnionIdentity076.Resolve(union.UnionId, union.DisplayName, union.Index)
                        .ToUpperInvariant() + "\n" +
                    M1UnionIdentity076.FormationRole(union.FormationId) + "\n" +
                    "LEADER  •  " + (leader?.DisplayName ?? "Unassigned") + "\n" +
                    (wingNames.Length == 0
                        ? "READY FOR ASSIGNMENT"
                        : "WITH  •  " + string.Join("  •  ", wingNames)),
                    null,
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.018f, 0.040f, 0.060f, 0.98f));
                panel.interactable = false;
                AnchorStudioRect076(
                    panel.GetComponent<RectTransform>(),
                    new Vector2(unionStartX + index * (unionWidth + unionGap), 0.505f),
                    new Vector2(unionStartX + index * (unionWidth + unionGap) + unionWidth, 0.825f));
                M1PremiumUi.StylePanel(
                    panel.image,
                    union.IsLegal ? M1PremiumUi.Surface.Positive : M1PremiumUi.Surface.WorldRibbon);
                if (leader != null)
                    AddPortraitToButton(panel, leader, large: false, cropToFill: true);
                var label = panel.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    label.rectTransform.anchorMax = new Vector2(0.97f, 0.59f);
                    label.rectTransform.offsetMax = Vector2.zero;
                }
                var roleBadge = RuntimeUi.AddPanel(
                    panel.transform,
                    "Founding Union Leader Role Color " + union.Index + " 076",
                    teamJobColor);
                roleBadge.raycastTarget = false;
                AnchorStudioRect076(
                    roleBadge.rectTransform,
                    FoundingCompanyTeamJobRibbonRegion078.min,
                    FoundingCompanyTeamJobRibbonRegion078.max);
                var roleText = RuntimeUi.AddText(
                    roleBadge.transform,
                    "Founding Union Leader Role " + union.Index + " 076",
                    teamJob,
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Background,
                    FontStyle.Bold);
                Stretch(roleText.rectTransform);
                roleText.raycastTarget = false;
                ConfigureResponsiveText062(roleText, 13, 20);
                var leaderClassPlate = RuntimeUi.AddPanel(
                    panel.transform,
                    "Founding Union Leader Class Plate " + union.Index + " 076",
                    new Color(0.006f, 0.018f, 0.030f, 0.98f));
                leaderClassPlate.raycastTarget = false;
                AnchorStudioRect076(
                    leaderClassPlate.rectTransform,
                    FoundingCompanyLeaderClassPlateRegion078.min,
                    FoundingCompanyLeaderClassPlateRegion078.max);
                var leaderClassAccent = RuntimeUi.AddPanel(
                    leaderClassPlate.transform,
                    "Founding Union Leader Class Accent " + union.Index + " 076",
                    leaderRoleColor);
                leaderClassAccent.raycastTarget = false;
                AnchorStudioRect076(
                    leaderClassAccent.rectTransform,
                    new Vector2(0f, 0f),
                    new Vector2(0.025f, 1f));
                var leaderClass = RuntimeUi.AddText(
                    leaderClassPlate.transform,
                    "Founding Union Leader Class " + union.Index + " 076",
                    "LEADER  •  " + leaderRole,
                    18,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                AnchorStudioRect076(
                    leaderClass.rectTransform,
                    new Vector2(0.055f, 0f),
                    new Vector2(0.98f, 1f));
                ConfigureResponsiveText062(leaderClass, 16, 18);
                leaderClass.horizontalOverflow = HorizontalWrapMode.Overflow;
                leaderClass.raycastTarget = false;
                if (label != null) ConfigureResponsiveText062(label, 13, 19);
            }

            var reserve = RuntimeUi.AddPanel(
                parent,
                "Founding Reserve Strip 076",
                new Color(0.012f, 0.030f, 0.045f, 0.96f));
            AnchorStudioRect076(
                reserve.rectTransform,
                new Vector2(0.018f, 0.405f),
                new Vector2(0.982f, 0.480f));
            M1PremiumUi.StylePanel(reserve, M1PremiumUi.Surface.EtchedGlass);
            var reserveNames = reserveRecruits
                .Select(value => value.DisplayName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            var reserveText = RuntimeUi.AddText(
                reserve.transform,
                "Founding Reserve Strip Text 076",
                reserveRecruits.Length == 0
                    ? "RESERVE  •  0 READY AT HALL  •  ALL GUILDMATES ASSIGNED"
                    : "RESERVE  •  " + reserveRecruits.Length + " READY AT HALL  •  " +
                      string.Join("  •  ", reserveNames),
                20,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorStudioRect076(
                reserveText.rectTransform,
                new Vector2(0.025f, 0.08f),
                new Vector2(0.975f, 0.92f));
            ConfigureResponsiveText062(reserveText, 15, 22);

            var order = RuntimeUi.AddPanel(
                parent,
                "Founding First Order 076",
                new Color(0.025f, 0.055f, 0.065f, 0.98f));
            AnchorStudioRect076(
                order.rectTransform,
                new Vector2(0.018f, 0.055f),
                new Vector2(0.690f, 0.375f));
            M1PremiumUi.StylePanel(order, M1PremiumUi.Surface.Positive);
            var orderText = RuntimeUi.AddText(
                order.transform,
                "Founding First Order Text 076",
                "FIRST OBJECTIVE\nEnter the Hall. Accept THE BELL BENEATH SKYHOME, then follow the gold marker to Lantern Road.",
                21,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorStudioRect076(
                orderText.rectTransform,
                new Vector2(0.06f, 0.06f),
                new Vector2(0.94f, 0.94f));
            ConfigureResponsiveText062(orderText, 14, 22);

            var enter = RuntimeUi.AddButton(
                parent,
                "Enter Walkable Guild Hall 071",
                "ENTER SKYHOME GUILD HALL",
                () =>
                {
                    _firstHourCharterRoute071 = false;
                    _screen = M1Screen.GuildOperations;
                    _guildCityTab017D = "HALL";
                    _guildCityMoreOpen060 = false;
                    _localStatus = "Emergency charter saved. Follow the gold objective inside the Hall.";
                    _localStatusPositive = true;
                    BuildCurrentScreen();
                },
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorStudioRect076(
                enter.GetComponent<RectTransform>(),
                new Vector2(0.708f, 0.055f),
                new Vector2(0.982f, 0.375f));
            ConfigureResponsiveText062(enter.GetComponentInChildren<Text>(), 18, 28);
            enter.Select();
        }

        private static void AddFoundingHeading076(
            Transform parent,
            string heading,
            string subtitle)
        {
            var ribbon = RuntimeUi.AddPanel(
                parent,
                "Founding Presentation Heading 076",
                new Color(0.006f, 0.016f, 0.030f, 0.97f));
            AnchorStudioRect076(
                ribbon.rectTransform,
                new Vector2(0.018f, 0.855f),
                new Vector2(0.982f, 0.982f));
            M1PremiumUi.StylePanel(ribbon, M1PremiumUi.Surface.WorldRibbon);
            var title = RuntimeUi.AddText(
                ribbon.transform,
                "Founding Presentation Title 076",
                heading + "\n" + subtitle,
                32,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorStudioRect076(
                title.rectTransform,
                new Vector2(0.025f, 0.08f),
                new Vector2(0.975f, 0.92f));
            ConfigureResponsiveText062(title, 19, 34);
        }

        private static string FirstHourHumanizeId071(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "READY FORMATION";
            var cleaned = value
                .Replace("FORMATION_", string.Empty)
                .Replace("DOCTRINE_", string.Empty)
                .Replace('_', ' ')
                .Trim();
            return string.IsNullOrWhiteSpace(cleaned)
                ? "READY FORMATION"
                : cleaned.ToUpperInvariant();
        }
    }
}
