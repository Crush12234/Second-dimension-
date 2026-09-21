using System;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private GuildCity017D.WalkableGuildHall069 _walkableGuildHall069;
        private GuildCity017D.GuildApplicantConversation069 _guildApplicantConversation069;
        private CompactInventoryPresenter069 _compactInventory069;
        private bool _hallTransitionInProgress069;
        private bool _returnToWalkableHall069;
        private bool _showMiraWayglassBriefing069;

        private const string MiraWayglassBriefingCopy069 =
            "MIRA  •  “The Wayglass is opening beneath us. The Door Inside is marked at the contract board.”";

        private string LivingHallObjectiveCopy069(string fallback)
        {
            if (!_showMiraWayglassBriefing069) return fallback;
            if (_localStatusPositive) return MiraWayglassBriefingCopy069;
            return string.IsNullOrWhiteSpace(_localStatus)
                ? "Mira could not open the Wayglass. Try the Hall guide again."
                : "MIRA  •  " + _localStatus;
        }

        private bool ShouldUseWalkableGuildHall069(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            // The shipping runtime gets the playable world. Small compatibility
            // coordinators and isolated legacy presentation tests keep their
            // established menu-only contract.
            return coordinator != null && state != null && state.IsAvailable &&
                   _coordinator is M1RuntimeCoordinator;
        }

        private bool EnterWalkableGuildHall069(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            if (!ShouldUseWalkableGuildHall069(coordinator, state)) return false;
            if (_walkableGuildHall069 != null && _walkableGuildHall069.IsActive069)
            {
                SuspendStudioAmbienceForWorld076();
                if (_canvas != null) _canvas.gameObject.SetActive(false);
                _walkableGuildHall069.Refresh069();
                return true;
            }

            CloseGuildApplicantConversation069();
            CloseOuterGateworks066();
            SuspendStudioAmbienceForWorld076();
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            try
            {
                _walkableGuildHall069 = gameObject.AddComponent<GuildCity017D.WalkableGuildHall069>();
                _walkableGuildHall069.Begin069(
                    new GuildCity017D.GuildHallDestinationCallbacks069
                    {
                        Guide = OpenHallGuide069,
                        Applicants = OpenHallApplicants069,
                        Inventory = OpenHallInventory069,
                        Party = OpenHallParty069,
                        Contract = BeginOrResumeHallAdventure069,
                        Practice = OpenHallPracticeBattle069,
                        QuickPlay = ContinueStoryQuickPlay070,
                        Exit = ExitWalkableHall069
                    },
                    ResolveWalkableHallObjective069);
                _walkableGuildHall069.SetProgressionSummaryProvider073(
                    BuildHallProgressionSummary073);
                _returnToWalkableHall069 = false;
                return true;
            }
            catch (Exception exception)
            {
                var failed = _walkableGuildHall069;
                _walkableGuildHall069 = null;
                if (failed != null)
                {
                    failed.Shutdown069();
                    Destroy(failed);
                }
                if (_canvas != null) _canvas.gameObject.SetActive(true);
                _localStatus = "The Guild Hall could not open. The focused Guild controls remain available.";
                _localStatusPositive = false;
                Debug.LogException(exception, this);
                return false;
            }
        }

        private GuildCity017D.GuildHallObjective069 ResolveWalkableHallObjective069()
        {
            var state = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            if (state == null)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.GuideDestinationId069,
                    "Talk to Mira to recover the Guild's next objective.");

            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            var thirdComplete = IsStoryContractCompleted065(state, ThirdStoryContractId065);
            var secondActive = !secondComplete &&
                               IsStoryContractActive065(state, SecondStoryContractId065);
            var thirdActive = !thirdComplete &&
                              IsStoryContractActive065(state, ThirdStoryContractId065);
            if (state.HasUnclaimedBattleReward)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Claim the saved XP, Art mastery, and earned equipment from the last battle.");
            var guidedStage080 = CurrentGuidedHallStage080(state);
            if (guidedStage080 != GuildHallGuidedStage080.Ready)
                return new GuildCity017D.GuildHallObjective069(
                    GuidedHallDestinationId080(guidedStage080),
                    GuidedHallObjectiveForVerification080(guidedStage080));
            if (secondActive)
            {
                if (NeedsChapterTwoUnionRepair076(state))
                    return new GuildCity017D.GuildHallObjective069(
                        GuildCity017D.WalkableGuildHall069.PartyDestinationId069,
                        "Repair the active Union plans at the strategy table before beginning the Wayglass descent.");
                if (ShouldShowChapterTwoOpening076(state))
                    return new GuildCity017D.GuildHallObjective069(
                        GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                        "Return to the Wayglass threshold and follow the survey crew's brass line beneath the Hall.");
                if (state.HasPendingEncounter)
                    return new GuildCity017D.GuildHallObjective069(
                        GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                        "The survey crew's route has reached a threat. Return beneath the Hall and command your Unions through it.");
                if (state.Expedition != null)
                    return new GuildCity017D.GuildHallObjective069(
                        GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                        "Resume The Door Inside at the saved Wayglass route and find the missing survey crew.");
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Open the recovered Wayglass and begin the one-tap search beneath the Hall.");
            }
            if (thirdActive)
            {
                if (state.Expedition != null || state.HasPendingEncounter)
                    return new GuildCity017D.GuildHallObjective069(
                        GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                        "Resume the saved relief-road operation and keep Skyhome's medicine convoy moving.");
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Begin the relief-road operation and protect Skyhome's medicine convoy.");
            }
            if (NeedsFirstAdditionalRecruit066(state, firstComplete))
            {
                if (!state.HasRecruitmentBoard)
                    return new GuildCity017D.GuildHallObjective069(
                        GuildCity017D.WalkableGuildHall069.GuideDestinationId069,
                        "Speak with Mira. She will introduce the permanent recruitment desk.");
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ApplicantsDestinationId069,
                    "Meet one applicant and recruit a seventh permanent adventurer.");
            }
            if (state.HasPendingEncounter)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Your Unions reached the Gatehouse threat. Return to the contract board and enter battle.");
            if (state.Expedition != null)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Resume the exact Lantern Road checkpoint and follow the gold waymarkers to the old Gatehouse.");
            if (state.HasActiveContract)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Begin Kiri's Lantern Road order with your ready Unions.");
            if (!firstComplete)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Recover the missing Wayglass on Lantern Road.");
            if (!secondComplete && NeedsFirstOperationConsequences077(state))
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.GuideDestinationId069,
                    "The patrol is home. Set recovery, keep one shared memory, then build and staff your first Hall facility.");
            if (!secondComplete && NeedsGuidedFirstHallImprovement069(state))
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.GuideDestinationId069,
                    "The patrol is home. Open the Wayglass with Mira to begin The Door Inside.");
            if (secondComplete && !thirdComplete)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Begin Chapter 3 and keep Skyhome's relief road open at the contract board.");
            if (thirdComplete)
                return new GuildCity017D.GuildHallObjective069(
                    GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                    "Three opening contracts complete. Continue the story from Campaign.");
            return new GuildCity017D.GuildHallObjective069(
                GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                "The recovered Wayglass reveals The Door Inside at the contract board.");
        }

        private string BuildHallProgressionSummary073()
        {
            var state = _coordinator?.State;
            if (state == null) return "GUILD XP  —  •  XP TO SPEND  —";
            return "GUILD LV " + Math.Max(1, state.GuildLevel) +
                   "  •  GUILD XP " + state.GuildXpIntoCurrentLevel.ToString("N0") +
                   " / " + state.GuildXpRequiredForNextLevel.ToString("N0") +
                   "  •  XP TO SPEND " + state.TreasuryXp.ToString("N0");
        }

        private bool RefreshActiveVersion69Experience069()
        {
            if (_hallTransitionInProgress069) return true;
            if (_walkableSkyhomeArrival071 != null && _walkableSkyhomeArrival071.IsActive071)
                return true;
            if (_outerGateworksExploration066 != null &&
                _outerGateworksExploration066.IsActiveForVerification076)
            {
                _outerGateworksExploration066.RefreshAuthoritativeState076();
                return true;
            }
            if (_walkableGuildHall069 != null && _walkableGuildHall069.IsActive069)
            {
                _walkableGuildHall069.Refresh069();
                return true;
            }
            if (_guildApplicantConversation069 != null && _guildApplicantConversation069.IsOpen069)
            {
                _guildApplicantConversation069.Refresh069();
                return true;
            }
            if (_compactInventory069 != null && _compactInventory069.IsRunningForTests)
            {
                // The inventory owns its coordinator subscription. Suppress the
                // parent screen rebuild and let that component refresh exactly once.
                return true;
            }
            return false;
        }

        private void CloseVersion69Experiences069()
        {
            CloseWalkableSkyhomeArrival071();
            CloseOuterGateworks066();
            CloseWalkableGuildHall069();
            CloseGuildApplicantConversation069();
            CloseCompactInventory069();
        }

        private void CloseWalkableGuildHall069()
        {
            var hall = _walkableGuildHall069;
            _walkableGuildHall069 = null;
            if (hall != null)
            {
                hall.Shutdown069();
                Destroy(hall);
            }
            if (_canvas != null) _canvas.gameObject.SetActive(true);
        }

        private void OpenHallGuide069()
        {
            var state = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            if (state == null) return;
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            if (TryOpenCurrentGuidedHallStep080(state)) return;
            if (NeedsFirstAdditionalRecruit066(state, firstComplete))
            {
                OpenHallApplicants069();
                return;
            }
            if (firstComplete && NeedsFirstOperationConsequences077(state))
            {
                OpenFirstOperationConsequences077();
                return;
            }
            if (firstComplete && NeedsGuidedFirstHallImprovement069(state))
            {
                _hallTransitionInProgress069 = true;
                try
                {
                    var result = ApplyGuidedFirstHallImprovement069(
                        _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D);
                    _localStatus = result?.Message ??
                        "Mira could not complete the first Hall improvement.";
                    _localStatusPositive = result != null && result.Succeeded;
                    _showMiraWayglassBriefing069 = true;
                    if (_walkableGuildHall069 != null && _walkableGuildHall069.IsActive069)
                    {
                        if (_localStatusPositive)
                        {
                            _walkableGuildHall069.Refresh069(
                                GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                                MiraWayglassBriefingCopy069 +
                                " Review The Door Inside at the contract board.");
                        }
                        else
                        {
                            _walkableGuildHall069.Refresh069(
                                GuildCity017D.WalkableGuildHall069.GuideDestinationId069,
                                _localStatus + " Speak with Mira to try the saved improvement again.");
                        }
                    }
                    else
                    {
                        // The shipping Hall is the fixed, one-screen Living Hub. Rebuild
                        // it after Mira's saved action so the player sees her briefing,
                        // the objective move to CONTRACTS, and the new Chapter 2 CTA.
                        _screen = M1Screen.GuildOperations;
                        _guildCityTab017D = "HALL";
                        _guildCityMoreOpen060 = false;
                        BuildCurrentScreen();
                    }
                }
                finally
                {
                    _hallTransitionInProgress069 = false;
                }
                return;
            }

            if (firstComplete && !secondComplete &&
                IsStoryContractActive065(state, SecondStoryContractId065))
            {
                // Mira and the dominant Hall CTA must reach the same authored
                // Wayglass decision. Never let her hotspot silently start an
                // expedition and skip the Chapter 2 briefing.
                OpenStoryNextStep065(
                    state,
                    firstComplete,
                    secondComplete,
                    IsStoryContractCompleted065(state, ThirdStoryContractId065));
                return;
            }
            BeginOrResumeHallAdventure069();
        }

        /// <summary>
        /// The first-hour Hall gate is satisfied by one visible, saved facility,
        /// not by sending a new player into the full city-management dashboard.
        /// </summary>
        public static bool NeedsGuidedFirstHallImprovement069(
            GuildCity017D.GuildCityPresentationState017D state) =>
            state != null && state.PlacedBuildingCount == 0;

        /// <summary>
        /// Uses the existing city command boundary to make one deterministic first
        /// improvement. Contract House is preferred because it makes the next story
        /// destination readable; migrated content falls back to the first legal
        /// district-compatible facility without inventing save state.
        /// </summary>
        public static M1CommandResult ApplyGuidedFirstHallImprovement069(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if (coordinator == null)
                return M1CommandResult.Failure(
                    "The Hall improvement desk is unavailable. No city changes were made.");

            var state = coordinator.GuildCity017D;
            if (state == null || !state.IsAvailable)
                return M1CommandResult.Failure(
                    "Skyhome's building record is unavailable. No city changes were made.");
            if (!NeedsGuidedFirstHallImprovement069(state))
                return M1CommandResult.Success(
                    "Your first Hall facility is already built and saved.");

            const string preferredBuildingId = "GC017D_BUILD_CONTRACT_HOUSE";
            var selection =
                (from building in (state.Buildings ?? Array.Empty<GuildCity017D.GuildCityBuildingView017D>())
                 where building != null && !string.IsNullOrWhiteSpace(building.BuildingId)
                 from plot in (state.Plots ?? Array.Empty<GuildCity017D.GuildCityPlotView017D>())
                 where plot != null && plot.Unlocked && plot.RoadConnected &&
                       string.IsNullOrWhiteSpace(plot.BuildingId) &&
                       StringComparer.Ordinal.Equals(plot.DistrictId, building.DistrictId)
                 orderby StringComparer.Ordinal.Equals(building.BuildingId, preferredBuildingId) ? 0 : 1,
                     plot.PlotId, building.BuildingId
                 select new { Plot = plot, Building = building }).FirstOrDefault();

            if (selection == null)
                return M1CommandResult.Failure(
                    "Mira could not find an unlocked, road-connected plot for the first Hall facility. No city changes were made.");

            var placed = coordinator.PlaceGuildCityBuilding017D(
                selection.Plot.PlotId,
                selection.Building.BuildingId);
            if (placed == null || !placed.Succeeded)
                return M1CommandResult.Failure(
                    "The first Hall improvement could not be saved: " +
                    (placed?.Message ?? "the city command returned no result."));

            var displayName = string.IsNullOrWhiteSpace(selection.Building.DisplayName)
                ? "Hall facility"
                : selection.Building.DisplayName;
            return M1CommandResult.Success(
                displayName + " established in the Hall with the Guild charter and saved.");
        }

        public static bool HasRecruitableApplicant069(
            GuildCity017D.GuildCityPresentationState017D state) =>
            state?.Applicants != null && state.Applicants.Any(value =>
                value != null && !value.IsSigned && value.CanAfford);

        /// <summary>
        /// Repairs a migrated empty/stale board before opening the one-person
        /// interview. The authoritative commit/refresh commands remain responsible
        /// for generation, cost rules, roster capacity, persistence, and identity.
        /// </summary>
        public static M1CommandResult PrepareRequiredApplicantBoard069(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if (coordinator == null)
                return M1CommandResult.Failure(
                    "The recruitment desk is unavailable. Return to the Hall and try again.");

            var state = coordinator.GuildCity017D;
            if (state == null || !state.IsAvailable)
                return M1CommandResult.Failure(
                    "The recruitment record is unavailable. Return to the Hall and try again.");
            if (state.TotalRecruitCount > 6)
                return M1CommandResult.Success("Your permanent seventh adventurer is already in the Guild.");

            M1CommandResult prepared = null;
            if (!HasRecruitableApplicant069(state) && state.CanInviteEarnedContacts124)
            {
                prepared = coordinator.CommitGuildCityApplicantBoard017D();
                if (prepared == null || !prepared.Succeeded)
                    return M1CommandResult.Failure(
                        "The earned contact could not be invited: " +
                        (prepared?.Message ?? "the recruitment command returned no result."));
                state = coordinator.GuildCity017D;
            }

            if (!HasRecruitableApplicant069(state))
                return M1CommandResult.Failure(
                    "No recruitable applicant is available for the required seventh place. Existing interviews are saved. Return to the Hall to review the charter companions and earned contacts.");

            return M1CommandResult.Success(
                prepared != null && !string.IsNullOrWhiteSpace(prepared.Message)
                    ? prepared.Message
                    : "A permanent applicant is ready to meet you.");
        }

        private void OpenHallApplicants069()
        {
            var coordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            if (coordinator == null) return;
            _hallTransitionInProgress069 = true;
            CloseWalkableGuildHall069();
            CloseGuildApplicantConversation069();
            try
            {
                var state = coordinator.GuildCity017D;
                if (state != null &&
                    state.TotalRecruitCount < FirstStoryMinimumRosterCount066)
                {
                    var board = PrepareRequiredApplicantBoard069(coordinator);
                    _localStatus = board?.Message ??
                        "The recruitment desk could not prepare today's interviews.";
                    _localStatusPositive = board != null && board.Succeeded;
                    if (!_localStatusPositive)
                    {
                        RecoverHallFromApplicantFailure069(
                            _localStatus,
                            GuildCity017D.WalkableGuildHall069.GuideDestinationId069);
                        return;
                    }
                }
                else if (state != null && !state.HasRecruitmentBoard && state.CanInviteEarnedContacts124)
                {
                    var board = coordinator.CommitGuildCityApplicantBoard017D();
                    _localStatus = board?.Message ??
                        "The recruitment desk could not prepare today's optional interviews.";
                    _localStatusPositive = board != null && board.Succeeded;
                    if (!_localStatusPositive)
                    {
                        RecoverHallFromApplicantFailure069(
                            _localStatus,
                            GuildCity017D.WalkableGuildHall069.ApplicantsDestinationId069);
                        return;
                    }
                }

                if (_canvas != null) _canvas.gameObject.SetActive(true);
                if (_screenRoot != null) _screenRoot.gameObject.SetActive(true);
                _returnToWalkableHall069 = true;
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "APPLICANTS";
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                RecoverHallFromApplicantFailure069(
                    "The Recruitment Desk could not open. You are back in the Guild Hall; speak with Mira to try again.",
                    GuildCity017D.WalkableGuildHall069.GuideDestinationId069);
            }
            finally
            {
                _hallTransitionInProgress069 = false;
            }
        }

        private void RecoverHallFromApplicantFailure069(string message, string retryDestinationId)
        {
            CloseGuildApplicantConversation069();
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            if (_screenRoot != null) _screenRoot.gameObject.SetActive(true);
            _localStatus = string.IsNullOrWhiteSpace(message)
                ? "No recruitable applicant is available. You are back in the Guild Hall."
                : message;
            _localStatusPositive = false;
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "HALL";
            BuildCurrentScreen();
            if (_walkableGuildHall069 != null && _walkableGuildHall069.IsActive069)
            {
                _walkableGuildHall069.Refresh069(
                    retryDestinationId,
                    _localStatus + " Follow the gold marker to retry.");
            }
        }

        private M1CommandResult RecruitHallApplicant069(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string recruitId)
        {
            var recruitCountBefore = coordinator?.GuildCity017D?.TotalRecruitCount ?? 0;
            var result = coordinator?.SignGuildCityApplicant017D(recruitId) ??
                         M1CommandResult.Failure("The recruitment command was unavailable.");
            var stateAfter = coordinator?.GuildCity017D;
            if (!ShouldReturnToHallAfterRequiredRecruit069(
                    recruitCountBefore,
                    result,
                    stateAfter)) return result;

            _localStatus = string.IsNullOrWhiteSpace(result.Message)
                ? "Your seventh adventurer joined permanently and is ready for the party."
                : result.Message;
            _localStatusPositive = true;
            ReturnToWalkableHall069();
            return result;
        }

        public static bool ShouldReturnToHallAfterRequiredRecruit069(
            int recruitCountBefore,
            M1CommandResult result,
            GuildCity017D.GuildCityPresentationState017D stateAfter) =>
            recruitCountBefore <= 6 && result != null && result.Succeeded &&
            stateAfter != null && stateAfter.TotalRecruitCount > 6;

        private void CloseGuildApplicantConversation069()
        {
            var conversation = _guildApplicantConversation069;
            _guildApplicantConversation069 = null;
            if (conversation != null)
            {
                conversation.Shutdown069();
                Destroy(conversation);
            }
            if (_screenRoot != null) _screenRoot.gameObject.SetActive(true);
        }

        private void OpenHallInventory069()
        {
            if (TryRouteLoopService164("EQUIPMENT")) return;
            var preferred = (_coordinator?.State?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .FirstOrDefault()?.RecruitId;
            _hallTransitionInProgress069 = true;
            CloseWalkableGuildHall069();
            try
            {
                if (_canvas != null) _canvas.gameObject.SetActive(true);
                if (_screenRoot != null) _screenRoot.gameObject.SetActive(false);
                _compactInventory069 = gameObject.AddComponent<CompactInventoryPresenter069>();
                _compactInventory069.Begin(
                    LoopInventoryHost164(),
                    _coordinator,
                    preferred,
                    ReturnToWalkableHall069);
                RefreshLoopNavigation164();
            }
            catch (Exception exception)
            {
                CloseCompactInventory069();
                _localStatus = "The armory could not open. Return to the Hall and try again.";
                _localStatusPositive = false;
                Debug.LogException(exception, this);
                _guildCityTab017D = "HALL";
                BuildCurrentScreen();
            }
            finally
            {
                _hallTransitionInProgress069 = false;
            }
        }

        /// <summary>
        /// Uses the same two-column RPG armory from every post-opening equipment
        /// entry point. The mandatory opening review keeps its established guided
        /// flow, while Hall/member/applicant pages no longer fall back to the large
        /// three-column equipment dashboard.
        /// </summary>
        private void OpenFocusedCompactInventory069(string preferredRecruitId)
        {
            if (!_loopRouting164 && LoopNavigationAvailable164)
            { OpenLoopDestination164("HEROES", preferredRecruitId); return; }
            if (!(_coordinator is M1RuntimeCoordinator) || _canvas == null)
            {
                Navigate(M1Screen.Equipment);
                return;
            }

            try
            {
                CloseCompactInventory069();
                _canvas.gameObject.SetActive(true);
                if (_screenRoot != null) _screenRoot.gameObject.SetActive(false);
                _compactInventory069 = gameObject.AddComponent<CompactInventoryPresenter069>();
                _compactInventory069.Begin(
                    LoopInventoryHost164(),
                    _coordinator,
                    preferredRecruitId,
                    ReturnFromFocusedCompactInventory069);
                RefreshLoopNavigation164();
            }
            catch (Exception exception)
            {
                CloseCompactInventory069();
                _localStatus = "The compact armory could not open. The standard equipment page remains available.";
                _localStatusPositive = false;
                Debug.LogException(exception, this);
                Navigate(M1Screen.Equipment);
            }
        }

        /// <summary>
        /// Opens a newly claimed Creator weapon on a real compatible recruit/slot.
        /// Selection is preview-only; the existing Armory authority performs the
        /// manual equip after the player reviews the before/after values.
        /// </summary>
        private void OpenCreatorRewardInventory087(string itemId)
        {
            var target = (_coordinator?.State?.Recruits ??
                          Array.Empty<M1RecruitLoadoutView>())
                .Where(recruit => recruit != null)
                .SelectMany(recruit => (recruit.Slots ??
                    Array.Empty<M1EquipmentSlotView>())
                    .Where(slot => slot != null &&
                        (slot.Choices ?? Array.Empty<M1EquipmentChoiceView>())
                        .Any(choice => choice != null &&
                            StringComparer.Ordinal.Equals(choice.ItemId, itemId)))
                    .Select(slot => new { Recruit = recruit, Slot = slot }))
                .FirstOrDefault();
            if (target == null)
            {
                _localStatus =
                    "The weapon is saved, but no compatible adventurer is currently available.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                return;
            }

            OpenFocusedCompactInventory069(target.Recruit.RecruitId);
            if (_compactInventory069 == null) return;
            _compactInventory069.TrySelectSlot(target.Slot.SlotId);
            _compactInventory069.TrySelectItem(itemId);
        }

        private void ReturnFromFocusedCompactInventory069()
        {
            // The button is labelled BACK TO HALL; always honor that promise.
            // Returning to the underlying Party dashboard made the Armory feel
            // trapped in nested menus and contradicted the player-facing route.
            ReturnToWalkableHall069();
        }

        private void CloseCompactInventory069()
        {
            var inventory = _compactInventory069;
            _compactInventory069 = null;
            if (inventory != null)
            {
                inventory.Shutdown();
                Destroy(inventory);
            }
            if (_screenRoot != null) _screenRoot.gameObject.SetActive(true);
        }

        private void OpenHallParty069()
        {
            if (TryRouteLoopService164("UNIONS")) return;
            LeaveHallForFocusedPage069(() => Navigate(M1Screen.UnionBuilder));
        }

        private void OpenHallPracticeBattle069()
        {
            _hallTransitionInProgress069 = true;
            CloseWalkableGuildHall069();
            try
            {
                OpenBattleNow060();
            }
            finally
            {
                _hallTransitionInProgress069 = false;
            }
        }

        private void ContinueStoryQuickPlay070()
        {
            if (TryResumeMainCampaign158()) return;
            var runtime070 = _coordinator as M1RuntimeCoordinator;
            var coordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            if (runtime070 == null || coordinator == null)
            {
                _localStatus = "Quick Play is unavailable until the Guild save is loaded.";
                _localStatusPositive = false;
                _walkableGuildHall069?.Refresh069(
                    GuildCity017D.WalkableGuildHall069.GuideDestinationId069,
                    _localStatus);
                return;
            }
            if (TryOpenCurrentGuidedHallStep080(coordinator.GuildCity017D))
            {
                return;
            }

            // Continue Story must advance the authored story once the opening
            // contract is finished. The contract service still permits deliberate
            // replays; this shortcut must not silently accept Lantern Road again.
            var story093 = coordinator.GuildCity017D;
            if (ShouldOpenNextStoryInsteadOfQuickPlay093(story093,
                    runtime070.State.Battle != null && !runtime070.State.Battle.IsResolved))
            {
                _hallTransitionInProgress069 = true;
                CloseWalkableGuildHall069();
                try
                {
                    _returnToWalkableHall069 = true;
                    OpenStoryNextStep065(story093, true,
                        IsStoryContractCompleted065(story093, SecondStoryContractId065),
                        IsStoryContractCompleted065(story093, ThirdStoryContractId065));
                }
                finally { _hallTransitionInProgress069 = false; }
                return;
            }

            var routed070 = runtime070.ContinueStoryQuickPlay070(FirstStoryContractId065);
            if (!routed070.IsSuccess)
            {
                _localStatus = new global::SecondDimension.Gameplay.GuildCity017D.PlayerFacingLabelService070()
                    .QuickPlayFailureMessage(routed070.Errors);
                _localStatusPositive = false;
                var objective070 = ResolveWalkableHallObjective069();
                _walkableGuildHall069?.Refresh069(
                    objective070.TargetHotspotId,
                    _localStatus);
                return;
            }

            var outcome070 = routed070.Value;
            _localStatus = outcome070.Title + ". " + outcome070.Guidance;
            _localStatusPositive = true;
            switch (outcome070.Destination)
            {
                case global::SecondDimension.Gameplay.GuildCity017D.QuickPlayDestination070.Recruitment:
                    OpenHallApplicants069();
                    return;
                case global::SecondDimension.Gameplay.GuildCity017D.QuickPlayDestination070.PartyPreparation:
                    OpenHallParty069();
                    return;
                case global::SecondDimension.Gameplay.GuildCity017D.QuickPlayDestination070.Battle:
                    CloseWalkableGuildHall069();
                    var battleState070 = coordinator.GuildCity017D;
                    if (battleState070.HasUnclaimedBattleReward)
                        Navigate(M1Screen.BattleResults);
                    else if (battleState070.HasPendingEncounter &&
                             ShouldEnterGuidedFirstHourField076(coordinator))
                        EnterExpeditionBoard074(coordinator);
                    else if (battleState070.HasPendingEncounter)
                        EnterCommittedGuildCityBattle017D(coordinator);
                    else
                        Navigate(M1Screen.Battle);
                    return;
                case global::SecondDimension.Gameplay.GuildCity017D.QuickPlayDestination070.Expedition:
                    CloseWalkableGuildHall069();
                    EnterExpeditionBoard074(coordinator);
                    return;
                default:
                    var objective070 = ResolveWalkableHallObjective069();
                    _walkableGuildHall069?.Refresh069(
                        objective070.TargetHotspotId,
                        _localStatus);
                    return;
            }
        }

        public static bool ShouldOpenNextStoryInsteadOfQuickPlay093(
            GuildCity017D.GuildCityPresentationState017D state, bool hasActiveBattle) =>
            state != null && !hasActiveBattle && !state.HasActiveContract &&
            !state.HasPendingEncounter && !state.HasPendingBattleReturn &&
            !state.HasUnclaimedBattleReward && state.Expedition == null &&
            IsStoryContractCompleted065(state, FirstStoryContractId065);

        private void BeginOrResumeHallAdventure069()
        {
            if (TryResumeMainCampaign158()) return;
            var coordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            var state = coordinator?.GuildCity017D;
            if (coordinator == null || state == null) return;
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            if (TryOpenCurrentGuidedHallStep080(state)) return;
            if (NeedsFirstAdditionalRecruit066(state, firstComplete))
            {
                OpenHallApplicants069();
                return;
            }

            _hallTransitionInProgress069 = true;
            CloseWalkableGuildHall069();
            try
            {
                state = coordinator.GuildCity017D;
                if (state.HasUnclaimedBattleReward)
                {
                    Navigate(M1Screen.BattleResults);
                    return;
                }
                if (state.HasPendingEncounter)
                {
                    if (ShouldEnterGuidedFirstHourField076(coordinator))
                        EnterExpeditionBoard074(coordinator);
                    else
                        EnterCommittedGuildCityBattle017D(coordinator);
                    return;
                }

                // Version 63-68 saves may still hold the retired, sprawling first
                // rescue route.  Only the real runtime owns this save migration;
                // fake/presentation coordinators cannot erase or rewrite a campaign.
                var runtime069 = _coordinator as M1RuntimeCoordinator;
                if (runtime069 != null && runtime069.NeedsLegacyFirstRescueMigration069())
                {
                    var migrated069 = runtime069.MigrateLegacyFirstRescue069();
                    _localStatus = migrated069?.Message ??
                        "The older first-story route could not be updated safely.";
                    _localStatusPositive = migrated069 != null && migrated069.Succeeded;
                    if (!_localStatusPositive)
                    {
                        _returnToWalkableHall069 = true;
                        _guildCityTab017D = "HALL";
                        BuildCurrentScreen();
                        return;
                    }
                    state = coordinator.GuildCity017D;
                }
                if (state.Expedition != null)
                {
                    EnterExpeditionBoard074(coordinator);
                    return;
                }

                if (!state.HasActiveContract)
                {
                    if (firstComplete)
                    {
                        _returnToWalkableHall069 = true;
                        OpenStoryNextStep065(
                            state,
                            firstComplete,
                            IsStoryContractCompleted065(state, SecondStoryContractId065),
                            IsStoryContractCompleted065(state, ThirdStoryContractId065));
                        return;
                    }
                    var accepted = coordinator.AcceptGuildCityContract017D(FirstStoryContractId065);
                    _localStatus = accepted?.Message ?? "The Lantern Road order could not be accepted.";
                    _localStatusPositive = accepted != null && accepted.Succeeded;
                    if (!_localStatusPositive)
                    {
                        _guildCityTab017D = "HALL";
                        BuildCurrentScreen();
                        return;
                    }
                }

                var started = coordinator.StartGuildCityExpedition017D();
                _localStatus = started?.Message ?? "The Lantern Road expedition could not begin.";
                _localStatusPositive = started != null && started.Succeeded;
                if (!_localStatusPositive)
                {
                    _returnToWalkableHall069 = true;
                    _guildCityTab017D = "PARTY";
                    BuildCurrentScreen();
                    return;
                }

                EnterExpeditionBoard074(coordinator);
            }
            finally
            {
                _hallTransitionInProgress069 = false;
            }
        }

        private void LeaveHallForFocusedPage069(Action destination)
        {
            _hallTransitionInProgress069 = true;
            _returnToWalkableHall069 = true;
            CloseWalkableGuildHall069();
            try
            {
                destination?.Invoke();
            }
            finally
            {
                _hallTransitionInProgress069 = false;
            }
        }

        private void ReturnToWalkableHall069()
        {
            if (TryRouteLoopService164("HALL")) return;
            // Existing inventory/party callbacks retain this compatibility name,
            // but always return to the direct illustrated Guild home.
            CloseWalkableGuildHall069();
            CloseWalkableSkyhomeArrival071();
            CloseGuildApplicantConversation069();
            CloseCompactInventory069();
            _returnToWalkableHall069 = false;
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "HALL";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        private void ExitWalkableHall069()
        {
            _hallTransitionInProgress069 = true;
            CloseWalkableGuildHall069();
            try
            {
                Navigate(M1Screen.MainMenu);
            }
            finally
            {
                _hallTransitionInProgress069 = false;
            }
        }
    }
}
