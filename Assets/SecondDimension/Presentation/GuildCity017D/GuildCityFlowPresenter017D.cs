using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private const string FirstContractKeyArtResource062 =
            "SecondDimension/Art/Backgrounds/CONTRACT_BELL_BENEATH_GATE_KEY_ART_V62";
        private const string GuildHallHomeBackgroundResource063 =
            "SecondDimension/Art/Guided063/GUILD_HALL_HOME_BACKGROUND_V63";
        private const string ExpeditionRouteMapResource063 =
            "SecondDimension/Art/Guided063/EXPEDITION_ROUTE_MAP_V63";
        private const string PartyStrategyRoomResource063 =
            "SecondDimension/Art/Guided063/PARTY_STRATEGY_ROOM_V63";
        private const string FirstStoryContractId065 = "CONTRACT_BELL_BENEATH_GATE";
        private const string FirstStoryBoardId066 = "BOARD_BELL_BENEATH_GATE";
        private const string FirstPlayableStoryBoardId069 = "BOARD_BELL_BENEATH_GATE_069";
        private const string FirstHourStoryBoardId071 = "BOARD_BELL_BENEATH_GATE_071";
        private const int FirstStoryMinimumRosterCount066 =
            SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                .FirstStoryMinimumRosterCount066;
        private const string SecondStoryContractId065 = "CONTRACT_LINES_NOT_RETURNED";
        private const string ThirdStoryContractId065 = "CONTRACT_RELIEF_ROAD";
        public const float ContractBoardFeaturedDetailsHeight074 = 544f;
        private const float ContractBoardNameHeight074 = 52f;
        private const float ContractBoardSponsorHeight074 = 36f;
        private const float ContractBoardHookHeight074 = 70f;
        private const float ContractBoardObjectiveHeight074 = 84f;
        private const float ContractBoardStakesHeight074 = 96f;
        private const float ContractBoardRecruitGateHeight074 = 48f;
        private const float ContractBoardActionHeight074 = 102f;
        private const float ContractBoardDetailsVerticalPadding074 = 16f;
        private const float ContractBoardDetailsSpacing074 = 5f;
        private static readonly Dictionary<string, Sprite> VisualSliceSprites062 =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private string _guildCityTab017D = "HALL";
        private bool _guildCityMoreOpen060;
        private bool _partyGuildRecordVerified063;
        private string _expeditionLeadRecruitId065;
        private int _expeditionApproachIndex065;
        private bool _expeditionDiceRolling065;
        private bool _expeditionMovementPending065;
        private string _expeditionMovementFromNodeId065;
        private string _expeditionMovementToNodeId065;
        private string _guildApplicantSelectedId066;
        private int _guildApplicantPage066;
        private int _guildMemberPage067;
        private int _guildDutyPage097;
        private const int GuildDutiesPageSize097 = 6;
        private GuildCity017D.OuterGateworksExploration066 _outerGateworksExploration066;
        private GuildCity017D.FirstHourFieldCheckpoint076 _outerGateworksCheckpoint076;

        private static bool IsFirstStoryBoard069(string boardId)
        {
            return StringComparer.Ordinal.Equals(boardId, FirstPlayableStoryBoardId069) ||
                   StringComparer.Ordinal.Equals(boardId, FirstHourStoryBoardId071) ||
                   StringComparer.Ordinal.Equals(boardId, FirstStoryBoardId066);
        }

        private sealed class ExpeditionMapLine065
        {
            public RectTransform Transform;
            public Vector2 From;
            public Vector2 To;
        }

        public static float ContractBoardFeaturedRequiredHeightForVerification074(
            bool includesRecruitGate)
        {
            var childCount = includesRecruitGate ? 7 : 6;
            return ContractBoardDetailsVerticalPadding074 +
                   ContractBoardNameHeight074 +
                   ContractBoardSponsorHeight074 +
                   ContractBoardHookHeight074 +
                   ContractBoardObjectiveHeight074 +
                   ContractBoardStakesHeight074 +
                   ContractBoardActionHeight074 +
                   (includesRecruitGate ? ContractBoardRecruitGateHeight074 : 0f) +
                   (childCount - 1) * ContractBoardDetailsSpacing074;
        }

        private static readonly string[][] ExpeditionRouteEdges065 =
        {
            new[] { "N00", "N01" },
            new[] { "N01", "N02" }, new[] { "N01", "N04" },
            new[] { "N02", "N03" }, new[] { "N03", "N06" },
            new[] { "N04", "N05" }, new[] { "N05", "N06" },
            new[] { "N06", "N07" }, new[] { "N06", "N08" },
            new[] { "N07", "N10" }, new[] { "N08", "N09" },
            new[] { "N09", "N10" },
            new[] { "N10", "N11" }, new[] { "N10", "N12" },
            new[] { "N11", "N13" }, new[] { "N12", "N13" },
            new[] { "N13", "N14" }
        };

        private void BuildGuildOperations017D()
        {
            var coordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            var state = coordinator?.GuildCity017D;
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, EarnedRecruitTab094) &&
                _coordinator is M1RuntimeCoordinator)
            {
                BuildEarnedCampaignRecruits094();
                return;
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "CONSEQUENCES") &&
                coordinator != null &&
                state != null &&
                state.IsAvailable)
            {
                BuildFirstOperationConsequences077(coordinator, state);
                return;
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "CHAPTER2") &&
                coordinator != null &&
                state != null &&
                state.IsAvailable)
            {
                if (ShouldShowChapterTwoOpening076(state))
                    BuildChapterTwoOpening076(coordinator, state);
                else
                    BuildExpeditionBoardExperience074(null, coordinator, state);
                return;
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "HALL") &&
                coordinator != null &&
                state != null &&
                state.IsAvailable)
            {
                BuildLivingGuildHub074(coordinator, state);
                return;
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "EXPEDITION") &&
                coordinator != null &&
                state != null &&
                state.IsAvailable)
            {
                BuildExpeditionBoardExperience074(null, coordinator, state);
                return;
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "APPLICANTS") &&
                coordinator != null &&
                state != null &&
                state.IsAvailable)
            {
                BuildRecruitmentDesk074(coordinator, state);
                return;
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "CITY") &&
                coordinator != null &&
                state != null &&
                state.IsAvailable)
            {
                BuildGuildCityWorkshop078(coordinator, state);
                return;
            }
            if (_guildCityTab017D == "TOWN" || _guildCityTab017D == "MERCHANT153")
            { BuildTown153(); return; }
            if (_guildCityTab017D == "TITANS")
            { BuildTitans161(); return; }
            if (_guildCityTab017D == "CATCHUP153")
            { BuildCatchUp153(); return; }
            var body = CreatePage(
                GuildPageTitle063(),
                GuildPageSubtitle063(),
                BackFromGuildPage063);
            if (coordinator == null || state == null || !state.IsAvailable)
            {
                AddStatus(body, state?.Error ?? "Guild/city coordinator is unavailable.", false);
                return;
            }

            AddGuildMobileNavigation062(_activePage);
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "HALL"))
                BuildGuildHallHero062(body, coordinator, state);

            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "GUIDE"))
                AddOpeningGuideBanner017G(body, coordinator, state);
            var openingContractsComplete156 =
                IsStoryContractCompleted065(state, FirstStoryContractId065) &&
                IsStoryContractCompleted065(state, SecondStoryContractId065) &&
                IsStoryContractCompleted065(state, ThirdStoryContractId065);
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "CAMPAIGN") ||
                (openingContractsComplete156 &&
                 (StringComparer.Ordinal.Equals(_guildCityTab017D, "CONTRACTS") ||
                  StringComparer.Ordinal.Equals(_guildCityTab017D, "WORLD GATE"))))
            {
                var missionTabs122 = AddRow(body, "Mission Story and Contracts Tabs 122", 10f, 118f);
                AddGuildCityTab017D(missionTabs122, "CAMPAIGN", "STORY");
                AddGuildCityTab017D(missionTabs122, "CONTRACTS",
                    openingContractsComplete156 ? "OPENING CONTRACTS" : "CONTRACTS");
                if (openingContractsComplete156)
                    AddGuildCityTab017D(missionTabs122, "WORLD GATE", "GUILD CONTRACTS");
            }

            switch (_guildCityTab017D)
            {
                case "LEDGER": BuildGuildmasterLedger024(body, coordinator, state); break;
                case "APPLICANTS": BuildGuildCityApplicants017D(body, coordinator, state); break;
                case "DEVELOPMENT": BuildGuildMemberDevelopment067(body, coordinator, state); break;
                case "CITY": BuildGuildCityBuildMode017D(body, coordinator, state); break;
                case "CONTRACTS": BuildGuildCityContracts017D(body, coordinator, state); break;
                case "PARTY": BuildGuildCityParty063(body, coordinator, state); break;
                case "EXPEDITION": BuildExpeditionBoardExperience074(body, coordinator, state); break;
                case "RELATIONSHIPS": BuildGuildCityRelationships017D(body, coordinator, state); break;
                case "PEOPLE":
                    var peopleCoordinator029 = _coordinator as People029.IPeopleRuntimePresentationCoordinator029;
                    BuildPeopleRuntime029(body, peopleCoordinator029, peopleCoordinator029?.PeopleRuntime029);
                    break;
                case "DEFENSE":
                    var strategicCoordinator017H = _coordinator as GuildCity017H.IGuildCityStrategicPresentationCoordinator017H;
                    BuildGuildCityDefense017H(body, strategicCoordinator017H, strategicCoordinator017H?.GuildCityStrategic017H);
                    break;
                case "CHRONICLE":
                    var canonCoordinator017H = _coordinator as GuildCity017H.IGuildCityStrategicPresentationCoordinator017H;
                    BuildGuildCityChronicle017H(body, canonCoordinator017H, canonCoordinator017H?.GuildCityStrategic017H);
                    break;
                case "GUIDE": BuildGuildCityGuide017G(body, coordinator, state); break;
                case "WORLD GATE":
                    var worldGateCoordinator023 = _coordinator as Campaign023.ICampaignWorldGatePresentationCoordinator023;
                    BuildGuildCityWorldGate023(body, worldGateCoordinator023, worldGateCoordinator023?.CampaignWorldGate023);
                    break;
                case "PROGRESSION":
                    var progressionCoordinator022 = _coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
                    BuildGuildCityProgression022(body, progressionCoordinator022, progressionCoordinator022?.CampaignProgression022);
                    break;
                case "ABYSS":
                    var abyssCoordinator022 = _coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
                    BuildGuildCityAbyss022(body, abyssCoordinator022, abyssCoordinator022?.CampaignProgression022);
                    break;
                case "CODES":
                    var creatorCoordinator028 = _coordinator as Creator028.ICreatorAccessPresentationCoordinator028;
                    BuildCreatorCodesRooms028(body, creatorCoordinator028, creatorCoordinator028?.CreatorAccess028);
                    break;
                case "COVENANTS":
                    var covenantCoordinator022 = _coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
                    BuildGuildCityCovenants022(body, covenantCoordinator022, covenantCoordinator022?.CampaignProgression022);
                    break;
                case "DETAILS": BuildGuildDetailsHub068(body, coordinator, state); break;
                case "DUTIES":
                    AddMessagePanel(body, "RETURN MEMBERS TO ADVENTURE",
                        "RESERVE releases a member from training or recovery. It does not change their Union or heal them.",
                        RuntimeUi.Accent);
                    BuildGuildMemberDuties097(body, state.Assignments, (recruitId, kind) =>
                        ApplyGuildCity017D(coordinator.SetGuildCityAssignment017D(recruitId, kind)));
                    break;
                case "CAMPAIGN":
                    if (TryBuildCampaignResumeBoundary158(body, state)) break;
                    var playableCoordinator020 = _coordinator as Campaign020.ICampaignPlayablePresentationCoordinator020;
                    BuildGuildCityCampaign020(body, playableCoordinator020, playableCoordinator020?.CampaignPlayable020);
                    var campaignCoordinator019 = _coordinator as Campaign019.ICampaignPresentationCoordinator019;
                    BuildGuildCityCampaign019(body, campaignCoordinator019, campaignCoordinator019?.Campaign019);
                    break;
                case "HALL": BuildGuildHallAtAGlance063(body, coordinator, state); break;
                default: BuildLivingGuildHall017D(body, coordinator, state); break;
            }
        }

        private void BackFromGuildPage063()
        {
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "DUTIES"))
            {
                _guildCityTab017D = "HALL";
                Navigate(M1Screen.UnionBuilder);
                return;
            }
            if (_returnToWalkableHall069 && _coordinator is M1RuntimeCoordinator)
            {
                ReturnToWalkableHall069();
                return;
            }
            if (!StringComparer.Ordinal.Equals(_guildCityTab017D, "HALL"))
            {
                _guildCityTab017D = "HALL";
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
                return;
            }
            Navigate(M1Screen.MainMenu);
        }

        private string GuildPageTitle063()
        {
            switch (_guildCityTab017D)
            {
                case "CONTRACTS": return "CONTRACTS";
                case "PARTY": return "PARTY";
                case "EXPEDITION": return "QUEST";
                case "DEVELOPMENT": return "MEMBER DEVELOPMENT";
                case "DETAILS": return "GUILD DETAILS";
                case "DUTIES": return "MEMBER DUTIES";
                case "APPLICANTS": return "RECRUIT ADVENTURERS";
                case "PEOPLE": return "MEMBER STORIES";
                case "RELATIONSHIPS": return "BONDS & MEMORIES";
                case "CITY": return "HALL UPGRADES";
                case "CHRONICLE": return "CHRONICLE";
                case "ABYSS": return "ENDLESS TOWER";
                case "GUIDE": return "HELP";
                default: return "GUILD HALL";
            }
        }

        private string GuildPageSubtitle063()
        {
            switch (_guildCityTab017D)
            {
                case "CONTRACTS": return "CHOOSE ONE STORY FOR YOUR GUILD TO ANSWER";
                case "PARTY": return "YOUR MEMBERS • YOUR UNION PLANS • ONE SHARED MISSION";
                case "EXPEDITION": return "THE BELL BENEATH SKYHOME";
                case "DEVELOPMENT": return "PERMANENT CLASS, WEAPON, ROLE, AND ART GROWTH";
                case "DETAILS": return "OPTIONAL GUILD MANAGEMENT";
                case "DUTIES": return "TRAINING • RECOVERY • RETURN TO ADVENTURE";
                case "APPLICANTS": return "MEET PEOPLE WHO WANT TO JOIN YOUR GUILD";
                case "PEOPLE": return "YOUR COMPANIONS AND THEIR STORIES";
                case "RELATIONSHIPS": return "MEMORIES MADE THROUGH ADVENTURE";
                case "CITY": return "MAKE THE GUILD HALL FEEL LIKE HOME";
                case "CHRONICLE": return "THE STORY YOUR GUILD HAS WRITTEN";
                case "ABYSS": return "FIGHT UPWARD • CLAIM REWARDS • RETURN STRONGER";
                case "GUIDE": return "WHERE TO GO NEXT";
                default: return "SKYHOME • YOUR COMPANIONS AND THE ROAD AHEAD";
            }
        }

        private void AddFirstContractJourney063(
            Transform body,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var ribbon = RuntimeUi.AddPanel(body, "First Contract Journey Ribbon 063", Color.white);
            RuntimeUi.SetLayout(ribbon, preferredHeight: 116f);
            M1PremiumUi.StylePanel(ribbon, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(
                ribbon.transform,
                new RectOffset(16, 16, 7, 7),
                2f,
                TextAnchor.MiddleCenter);

            var steps = new GameObject(
                "First Contract Journey Steps 063",
                typeof(RectTransform),
                typeof(LayoutElement)).GetComponent<RectTransform>();
            steps.SetParent(ribbon.transform, false);
            RuntimeUi.SetLayout(steps, preferredHeight: 48f);
            RuntimeUi.AddHorizontalLayout(steps, new RectOffset(0, 0, 0, 0), 7f, TextAnchor.MiddleCenter);

            var currentStep = CurrentFirstContractStep063(state);
            var journey = IsStoryContractCompleted065(state, FirstStoryContractId065)
                ? new[] { "WAYGLASS", "REWARDS", "GROWTH", "DOOR INSIDE" }
                : new[] { "CHARTER", "ORDER", "UNIONS", "LANTERN ROAD", "GATEHOUSE" };
            var currentIndex = Array.IndexOf(journey, currentStep);
            if (currentIndex < 0) currentIndex = 0;
            for (var index = 0; index < journey.Length; index++)
                AddJourneyStep063(steps, journey[index], journey[index], index < currentIndex, index == currentIndex);

            AddResponsiveText062(
                ribbon.transform,
                "First Contract Guide Sentence 063",
                FirstContractGuideSentence063(state),
                18,
                28,
                48f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
        }

        private string CurrentFirstContractStep063(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            if (IsStoryContractCompleted065(state, FirstStoryContractId065))
                return state != null && state.HasActiveContract ? "DOOR INSIDE" : "GROWTH";
            if (NeedsFirstAdditionalRecruit066(state, false)) return "CHARTER";
            if (state != null && state.HasPendingEncounter) return "GATEHOUSE";
            if (state != null && state.Expedition != null) return "LANTERN ROAD";
            if (state != null && state.HasActiveContract) return "UNIONS";
            return "ORDER";
        }

        private void AddPersistentStoryObjective065(
            Transform body,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var needsFirstAdditionalRecruit = NeedsFirstAdditionalRecruit066(state, firstComplete);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            var thirdComplete = IsStoryContractCompleted065(state, ThirdStoryContractId065);
            var objective = RuntimeUi.AddPanel(body, "Persistent Guild Story Objective 065", Color.white);
            RuntimeUi.SetLayout(objective, preferredHeight: 154f);
            M1PremiumUi.StylePanel(objective, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddHorizontalLayout(
                objective.transform,
                new RectOffset(20, 20, 10, 10),
                14f,
                TextAnchor.MiddleLeft);

            var copy = new GameObject(
                "Persistent Guild Story Objective Copy 065",
                typeof(RectTransform),
                typeof(LayoutElement)).GetComponent<RectTransform>();
            copy.SetParent(objective.transform, false);
            RuntimeUi.SetLayout(copy, preferredHeight: 132f, flexibleWidth: 1f);
            RuntimeUi.AddVerticalLayout(copy, new RectOffset(4, 4, 2, 2), 2f, TextAnchor.MiddleLeft);
            AddResponsiveText062(
                copy,
                "Persistent Guild Story Chapter 065",
                StoryChapterHeading065(firstComplete, secondComplete, thirdComplete),
                24,
                36,
                42f,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AddResponsiveText062(
                copy,
                "Persistent Guild Story Next Step 065",
                StoryNextObjective065(state, firstComplete, secondComplete, thirdComplete),
                18,
                28,
                72f,
                RuntimeUi.Text,
                FontStyle.Bold);

            var actionLabel = StoryNextActionLabel065(state, firstComplete, secondComplete, thirdComplete);
            var contractBoardOwnsBeginAction =
                StringComparer.Ordinal.Equals(_guildCityTab017D, "CONTRACTS") &&
                (StringComparer.Ordinal.Equals(actionLabel, "BEGIN CHAPTER 1") ||
                 StringComparer.Ordinal.Equals(actionLabel, "BEGIN CHAPTER 2") ||
                 StringComparer.Ordinal.Equals(actionLabel, "BEGIN CHAPTER 3"));
            // The featured contract card owns the actual begin/accept action on
            // this screen. A second identical objective button only rebuilds the
            // same page and can make the player appear stuck between chapters.
            if (string.IsNullOrWhiteSpace(actionLabel) || contractBoardOwnsBeginAction) return;
            var action = RuntimeUi.AddButton(
                objective.transform,
                "Persistent Guild Story Next Action 065",
                actionLabel,
                () => OpenStoryNextStep065(state, firstComplete, secondComplete, thirdComplete),
                242f,
                RuntimeUi.Accent);
            ConfigureResponsiveText062(action.GetComponentInChildren<Text>(), 18, 28);
        }

        private static string StoryChapterHeading065(
            bool firstComplete,
            bool secondComplete,
            bool thirdComplete)
        {
            if (!firstComplete) return "CHAPTER 1  •  THE BELL BENEATH SKYHOME";
            if (!secondComplete) return "CHAPTER 2  •  THE DOOR INSIDE";
            if (!thirdComplete) return "CHAPTER 3  •  KEEP THE RELIEF ROAD OPEN";
            return "OPENING CONTRACTS  •  3 OF 3 COMPLETE";
        }

        private string StoryNextObjective065(
            GuildCity017D.GuildCityPresentationState017D state,
            bool firstComplete,
            bool secondComplete,
            bool thirdComplete)
        {
            if (state == null) return "NEXT: Return to the Guild Hall and ask the Guild Guide for the marked story objective.";
            if (state.HasUnclaimedBattleReward)
                return "NEXT: Claim the battle reward. Character XP, Art mastery, equipment, and Guild growth are saved together.";
            var guidedStage080 = CurrentGuidedHallStage080(state);
            var guidedObjective080 = GuidedHallObjectiveForVerification080(guidedStage080);
            if (!string.IsNullOrWhiteSpace(guidedObjective080))
                return "NEXT: " + guidedObjective080;
            if (NeedsFirstAdditionalRecruit066(state, firstComplete))
                return "NEXT: Meet the people waiting at the recruitment desk and permanently recruit at least one new adventurer.";
            if (!firstComplete)
            {
                if (state.HasPendingEncounter) return "NEXT: Enter battle and keep Lantern Road open for the missing patrol.";
                if (state.Expedition != null && ExpeditionBoardProjection074
                    .HasFirstHourPatrolRescueProof074(state.Expedition.ObjectiveFlags))
                    return "NEXT: The patrol and Wayglass are secure. Follow the marked road home to Skyhome.";
                if (state.Expedition != null && HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED"))
                    return "NEXT: Lantern Road is clear—but the patrol is still trapped. Continue to the old Gatehouse and recover the Wayglass.";
                if (state.Expedition != null)
                    return "NEXT: Follow Lantern Road's gold waymarkers to the missing patrol at the old Gatehouse.";
                if (state.HasActiveContract)
                    return "NEXT: Review your ready Union plans, then take the Lantern Road order from the Party page.";
                return "NEXT: Answer the rope-less bell, take Kiri's Lantern Road order, and recover the missing Wayglass.";
            }
            if (NeedsFirstOperationConsequences077(state))
            {
                switch (FirstOperationConsequenceStageForVerification077(state))
                {
                    case FirstOperationConsequenceStage077.RecoveryOrTraining:
                        return "NEXT: Decide who recovers and who trains after the Lantern Road operation.";
                    case FirstOperationConsequenceStage077.RelationshipMemory:
                        return "NEXT: Keep one relationship memory from the road in your Guild Hall.";
                    case FirstOperationConsequenceStage077.FacilityChoice:
                        return "NEXT: Choose the first facility that will shape your Guild Hall.";
                    default:
                        return "NEXT: Assign one available adventurer to make your first facility work beside the next operation.";
                }
            }
            if (!secondComplete)
            {
                if (IsStoryContractActive065(state, SecondStoryContractId065))
                {
                    if (NeedsChapterTwoUnionRepair076(state))
                        return "NEXT: Repair the active Union plans before beginning the Wayglass descent.";
                    return ShouldShowChapterTwoOpening076(state)
                        ? "NEXT: Return to the Wayglass threshold and follow the survey crew's brass line."
                        : "NEXT: Follow the impossible route marks and find the door shown inside Skyhome.";
                }
                return "NEXT: Bring the recovered Wayglass to the Hall table and open its impossible map—The Door Inside.";
            }
            if (!thirdComplete)
            {
                if (IsStoryContractActive065(state, ThirdStoryContractId065))
                    return "NEXT: Keep the medicine convoy moving and defend the relief road.";
                return "NEXT: Begin Chapter 3 and keep Skyhome's relief road open.";
            }
            return "NEXT: Three opening contracts complete. Continue the story from CAMPAIGN.";
        }

        private string StoryNextActionLabel065(
            GuildCity017D.GuildCityPresentationState017D state,
            bool firstComplete,
            bool secondComplete,
            bool thirdComplete)
        {
            if (state == null) return "RETURN TO HALL";
            var campaignRoute158 = CurrentMainCampaignRoute158(state);
            if (!string.IsNullOrWhiteSpace(campaignRoute158) &&
                campaignRoute158 != "CAMPAIGN" && campaignRoute158 != "EXPEDITION")
                return "OPEN CAMPAIGN";
            var activeAction110 = ActiveAdventureActionLabel110(campaignRoute158);
            if (!string.IsNullOrWhiteSpace(activeAction110)) return activeAction110;
            if (state.HasUnclaimedBattleReward) return "VIEW BATTLE REWARD";
            var guidedStage080 = CurrentGuidedHallStage080(state);
            var guidedAction080 = GuidedHallActionForVerification080(guidedStage080);
            if (!string.IsNullOrWhiteSpace(guidedAction080)) return guidedAction080;
            if (NeedsFirstAdditionalRecruit066(state, firstComplete)) return "MEET NEW APPLICANTS";
            if (!firstComplete)
            {
                if (state.HasPendingEncounter) return "ENTER STORY BATTLE";
                if (state.Expedition != null) return "CONTINUE CHAPTER 1";
                if (state.HasActiveContract) return "REVIEW PARTY";
                return "BEGIN CHAPTER 1";
            }
            if (NeedsFirstOperationConsequences077(state))
                return FirstOperationConsequenceActionForVerification077(state);
            if (!secondComplete)
            {
                if (!IsStoryContractActive065(state, SecondStoryContractId065))
                    return "BEGIN CHAPTER 2";
                if (NeedsChapterTwoUnionRepair076(state))
                    return "FIX UNION PLANS";
                return ShouldShowChapterTwoOpening076(state)
                    ? "BEGIN WAYGLASS DESCENT"
                    : "CONTINUE CHAPTER 2";
            }
            if (!thirdComplete)
                return IsStoryContractActive065(state, ThirdStoryContractId065)
                    ? "CONTINUE CHAPTER 3"
                    : "BEGIN CHAPTER 3";
            return "OPEN CAMPAIGN";
        }

        private void OpenStoryNextStep065(
            GuildCity017D.GuildCityPresentationState017D state,
            bool firstComplete,
            bool secondComplete,
            bool thirdComplete)
        {
            if (thirdComplete)
            {
                OpenMainCampaign158();
                return;
            }
            if (TryResumeMainCampaign158()) return;
            if (state == null)
            {
                _guildCityTab017D = "HALL";
                BuildCurrentScreen();
                return;
            }
            if (state.HasUnclaimedBattleReward)
            {
                Navigate(M1Screen.BattleResults);
                return;
            }
            if (TryOpenCurrentGuidedHallStep080(state)) return;
            if (NeedsFirstAdditionalRecruit066(state, firstComplete))
            {
                _guildCityTab017D = "APPLICANTS";
                BuildCurrentScreen();
                return;
            }
            if (!firstComplete)
            {
                if (state.HasPendingEncounter)
                {
                    var firstHourCoordinator076 =
                        (GuildCity017D.IGuildCityPresentationCoordinator017D)_coordinator;
                    if (ShouldEnterGuidedFirstHourField076(firstHourCoordinator076))
                        EnterExpeditionBoard074(firstHourCoordinator076);
                    else
                        EnterCommittedGuildCityBattle017D(firstHourCoordinator076);
                    return;
                }
                if (state.Expedition != null &&
                    IsFirstStoryBoard069(state.Expedition.BoardId))
                {
                    EnterExpeditionBoard074(
                        (GuildCity017D.IGuildCityPresentationCoordinator017D)_coordinator);
                    return;
                }
                _guildCityTab017D = state.Expedition != null
                    ? "EXPEDITION"
                    : state.HasActiveContract ? "PARTY" : "CONTRACTS";
                BuildCurrentScreen();
                return;
            }
            if (NeedsFirstOperationConsequences077(state))
            {
                OpenFirstOperationConsequences077();
                return;
            }
            if (!secondComplete)
            {
                if (IsStoryContractActive065(state, SecondStoryContractId065) &&
                    NeedsChapterTwoUnionRepair076(state))
                {
                    OpenChapterTwoUnionRepair076();
                    return;
                }
                _guildCityTab017D = ShouldShowChapterTwoOpening076(state)
                    ? "CHAPTER2"
                    : IsStoryContractActive065(state, SecondStoryContractId065)
                        ? (state.Expedition == null ? "PARTY" : "EXPEDITION")
                        : "CONTRACTS";
                BuildCurrentScreen();
                return;
            }
            if (!thirdComplete)
            {
                _guildCityTab017D = IsStoryContractActive065(state, ThirdStoryContractId065)
                    ? (state.Expedition == null ? "PARTY" : "EXPEDITION")
                    : "CONTRACTS";
                BuildCurrentScreen();
                return;
            }
            _guildCityTab017D = "CAMPAIGN";
            _guildCityMoreOpen060 = true;
            BuildCurrentScreen();
        }

        private static bool NeedsFirstAdditionalRecruit066(
            GuildCity017D.GuildCityPresentationState017D state,
            bool firstStoryComplete)
        {
            return state != null &&
                   !firstStoryComplete &&
                   state.TotalRecruitCount < FirstStoryMinimumRosterCount066;
        }

        private static bool IsStoryContractCompleted065(
            GuildCity017D.GuildCityPresentationState017D state,
            string contractId)
        {
            if (state == null) return false;
            if (state.Contracts != null && state.Contracts.Any(contract =>
                contract != null &&
                StringComparer.Ordinal.Equals(contract.ContractId, contractId) &&
                contract.IsFailed)) return false;
            if (state.Contracts != null && state.Contracts.Any(contract =>
                contract != null &&
                StringComparer.Ordinal.Equals(contract.ContractId, contractId) &&
                contract.IsCompleted)) return true;

            // Historical completion is already projected into Contracts from
            // exact reward receipts. Attempt counts and unrelated battle rewards
            // cannot prove that this particular chapter was ever completed.
            return false;
        }

        private static bool IsStoryContractActive065(
            GuildCity017D.GuildCityPresentationState017D state,
            string contractId) =>
            state?.Contracts != null && state.Contracts.Any(contract =>
                contract != null &&
                StringComparer.Ordinal.Equals(contract.ContractId, contractId) &&
                contract.IsActive && !contract.IsCompleted && !contract.IsFailed);

        private static bool HasExpeditionFlag065(
            GuildCity017D.GuildCityPresentationState017D state,
            string flag) =>
            state?.Expedition?.ObjectiveFlags != null && state.Expedition.ObjectiveFlags.Any(value =>
                StringComparer.Ordinal.Equals(value, flag));

        private static bool HasExpeditionFlagContaining065(
            GuildCity017D.GuildCityPresentationState017D state,
            string fragment) =>
            state?.Expedition?.ObjectiveFlags != null && state.Expedition.ObjectiveFlags.Any(value =>
                value != null && value.IndexOf(fragment, StringComparison.Ordinal) >= 0);

        private static void AddJourneyStep063(
            Transform parent,
            string name,
            string label,
            bool complete,
            bool current)
        {
            var step = RuntimeUi.AddPanel(parent, "First Contract Step " + name + " 063", Color.white);
            RuntimeUi.SetLayout(step, preferredHeight: 46f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(
                step,
                current ? M1PremiumUi.Surface.WorldPaper : M1PremiumUi.Surface.WorldGlass);
            var color = current
                ? RuntimeUi.Warning
                : complete ? RuntimeUi.Positive : RuntimeUi.MutedText;
            var text = RuntimeUi.AddText(
                step.transform,
                "First Contract Step Label " + name + " 063",
                (complete ? "✓  " : current ? "◆  " : "·  ") + label,
                22,
                TextAnchor.MiddleCenter,
                color,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            ConfigureResponsiveText062(text, 14, 22);
        }

        private string FirstContractGuideSentence063(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var guidedStage080 = CurrentGuidedHallStage080(state);
            var guidedObjective080 = GuidedHallObjectiveForVerification080(guidedStage080);
            if (!string.IsNullOrWhiteSpace(guidedObjective080))
                return "Guild Guide: " + guidedObjective080;
            if (NeedsFirstAdditionalRecruit066(state, firstComplete))
                return "Guild Guide: Invite one permanent adventurer to the charter before Kiri issues the Lantern Road order.";
            if (firstComplete && !IsStoryContractCompleted065(state, SecondStoryContractId065))
            {
                if (IsStoryContractActive065(state, SecondStoryContractId065))
                    return "Guild Guide: Chapter 2 is active. Follow the Wayglass route beneath Skyhome.";
                return "Guild Guide: The patrol is home and the Wayglass is awake. Open its map to begin The Door Inside.";
            }
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "CONTRACTS"))
                return state != null && state.HasActiveContract
                    ? "Guild Guide: The bell contract is accepted. Continue to the quest when your party is ready."
                    : "Guild Guide: Read the featured request, then accept it to begin your first mission.";
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "PARTY"))
                return "Guild Guide: These Union plans are answering the bell. Review them, then continue.";
            if (StringComparer.Ordinal.Equals(_guildCityTab017D, "EXPEDITION"))
            {
                if (state != null && state.HasPendingEncounter)
                    return "Guild Guide: The enemy is just ahead. Enter battle when you are ready.";
                if (HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED") &&
                    !ExpeditionBoardProjection074.HasFirstHourPatrolRescueProof074(
                        state?.Expedition?.ObjectiveFlags))
                    return "Guild Guide: That battle cleared Lantern Road. Keep moving—the patrol and Wayglass are still inside the Gatehouse.";
                if (state != null && state.Expedition != null)
                    return "Guild Guide: Take Lantern Road and follow the gold waymarkers to the old Gatehouse.";
                return "Guild Guide: Your party is formed. Begin the Lantern Road rescue to leave Skyhome.";
            }
            if (_guildCityMoreOpen060)
                return "Guild Guide: Optional guild systems are open below. Return to Hall when you are finished.";
            return state != null && state.HasActiveContract
                ? "Guild Guide: Your first contract is accepted. Continue to the quest to depart."
                : "Guild Guide: One beginner contract is marked. Open it to start your first story.";
        }

        private void AddGuildCityLedger017D(Transform parent, string heading, string value, Color color)
        {
            var panel = AddColumnPanel(parent, heading, 1f, 170f);
            RuntimeUi.AddText(panel, heading + " Heading", heading, 28, TextAnchor.MiddleCenter,
                RuntimeUi.MutedText, FontStyle.Bold);
            RuntimeUi.AddText(panel, heading + " Value", value, 54, TextAnchor.MiddleCenter,
                color, FontStyle.Bold);
        }

        private void AddGuildCityTab017D(Transform parent, string tab, string label = null)
        {
            var button = RuntimeUi.AddButton(parent, "Guild City Tab " + tab, label ?? tab,
                () => OpenTownService153(tab),
                118f, StringComparer.Ordinal.Equals(_guildCityTab017D, tab) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 20, 34);
        }

        private void AddGuildMobileNavigation062(Transform page)
        {
            if (page == null || LoopNavigationAvailable164) return;
            var navigation = RuntimeUi.AddPanel(page, "Guild Mobile Bottom Navigation 062", Color.white);
            RuntimeUi.SetLayout(navigation, preferredHeight: 148f, flexibleHeight: 0f);
            M1PremiumUi.StylePanel(navigation, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddHorizontalLayout(navigation.transform, new RectOffset(14, 14, 8, 8), 10f, TextAnchor.MiddleCenter);

            var missions = RuntimeUi.AddButton(
                navigation.transform,
                "Guild Mobile Nav Missions 084",
                "CAMPAIGN",
                OpenMissions084,
                RuntimeUi.MinimumTouchPixels,
                IsMissionTab084(_guildCityTab017D)
                    ? RuntimeUi.Accent
                    : RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(missions.GetComponentInChildren<Text>(), 20, 34);
            AddGuildMobileNavButton062(
                navigation.transform,
                "Guild Mobile Nav Recruit 084",
                "RECRUIT",
                "APPLICANTS");
            var unions = RuntimeUi.AddButton(
                navigation.transform,
                "Guild Mobile Nav Unions 084",
                "UNIONS",
                OpenHallParty069,
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(unions.GetComponentInChildren<Text>(), 20, 34);
            var inventory = RuntimeUi.AddButton(
                navigation.transform,
                "Guild Mobile Nav Inventory 068",
                "INVENTORY",
                () => OpenInventoryForRecruit068(_selectedRecruitId),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(inventory.GetComponentInChildren<Text>(), 20, 34);
            AddGuildMobileNavButton062(
                navigation.transform,
                "Guild Mobile Nav Infinite Tower 084",
                "INFINITE TOWER",
                "ABYSS");
            AddGuildMobileNavButton062(
                navigation.transform,
                "Guild Mobile Nav Enter Code 084",
                "ENTER CODE",
                "CODES");
        }

        public static string MissionLandingTabForVerification084(
            bool hasActiveBoardQuest,
            bool hasActiveWorldGate,
            bool hasActiveCampaign,
            bool hasAvailableStory = false)
        {
            if (hasActiveBoardQuest) return "EXPEDITION";
            if (hasActiveWorldGate) return "WORLD GATE";
            if (hasActiveCampaign || hasAvailableStory) return "CAMPAIGN";
            return "CONTRACTS";
        }

        public static bool AvailableStoryMissionForVerification122(
            bool openingContractsComplete,
            Campaign019.CampaignPresentationState019 campaign)
        {
            // Reuse the existing chapter availability projection; opening contracts
            // retain their current route and command authorities remain unchanged.
            return openingContractsComplete && campaign?.IsAvailable == true &&
                (campaign.CanStartNextCycle130 ||
                 (campaign.Chapters?.Any(chapter => chapter != null && !chapter.Completed &&
                    StringComparer.Ordinal.Equals(chapter.Status, "AVAILABLE")) ?? false));
        }

        private static bool IsMissionTab084(string tab)
        {
            return StringComparer.Ordinal.Equals(tab, "CONTRACTS") ||
                   StringComparer.Ordinal.Equals(tab, "EXPEDITION") ||
                   StringComparer.Ordinal.Equals(tab, "WORLD GATE") ||
                   StringComparer.Ordinal.Equals(tab, "CAMPAIGN");
        }

        private void OpenMissions084()
        {
            var guildCity =
                (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            var openingContractsComplete122 =
                IsStoryContractCompleted065(guildCity, FirstStoryContractId065) &&
                IsStoryContractCompleted065(guildCity, SecondStoryContractId065) &&
                IsStoryContractCompleted065(guildCity, ThirdStoryContractId065);
            if (openingContractsComplete122)
            {
                OpenMainCampaign158();
                return;
            }
            if (TryResumeMainCampaign158()) return;
            var activeBoardQuest = guildCity?.Expedition != null &&
                SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D
                    .UsesBoardQuestRewards081(guildCity.Expedition.BoardId);
            var worldGate =
                (_coordinator as Campaign023.ICampaignWorldGatePresentationCoordinator023)
                ?.CampaignWorldGate023;
            var campaign =
                (_coordinator as Campaign020.ICampaignPlayablePresentationCoordinator020)
                ?.CampaignPlayable020;

            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = MissionLandingTabForVerification084(
                activeBoardQuest,
                !string.IsNullOrWhiteSpace(worldGate?.ActiveOperationId),
                !string.IsNullOrWhiteSpace(campaign?.ActiveOperationId),
                AvailableStoryMissionForVerification122(
                    openingContractsComplete122,
                    openingContractsComplete122
                        ? (_coordinator as Campaign019.ICampaignPresentationCoordinator019)?.Campaign019
                        : null));
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        // Campaign entry is a read-only choice. Only the existing deliberate
        // chapter-start command may release an idle Tower through its receipt.
        public static bool CanBrowseCampaignFromIdleTowerForVerification158(
            Campaign022.CampaignProgressionPresentationState022 tower,
            GuildCity017D.GuildCityPresentationState017D city,
            M2BattleView battle)
        {
            return tower?.IsAvailable == true && city != null &&
                tower.ActiveAbyssOperationId?.StartsWith(
                    SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022
                        .TowerOperationPrefix094, StringComparison.Ordinal) == true &&
                tower.ActiveAbyssStatus == "Active" &&
                !tower.LegacyTowerRecoveryRequired && !tower.LegacyTowerRecoveryRequiresSupport &&
                string.IsNullOrWhiteSpace(tower.TowerAuthorityError094) &&
                !tower.HasPendingAbyssStepReceipt && !tower.HasPendingAbyssBattleReceipt &&
                !tower.TowerBattleInProgress && !tower.TowerBattleRewardAwaitingClaim &&
                !city.HasPendingEncounter && !city.HasPendingBattleReturn &&
                !city.HasUnclaimedBattleReward &&
                (battle == null || (battle.IsResolved && battle.Reward?.Claimed != false));
        }

        public static string MainCampaignRouteForVerification158(
            string activeRoute, bool canBrowseFromIdleTower, bool worldGateIsCampaign)
        {
            if (activeRoute == "ABYSS" && canBrowseFromIdleTower) return string.Empty;
            if (activeRoute == "WORLD GATE" && worldGateIsCampaign) return "CAMPAIGN";
            return activeRoute ?? string.Empty;
        }

        private string CurrentMainCampaignRoute158(GuildCity017D.GuildCityPresentationState017D state)
        {
            var route = CurrentActiveAdventureRoute110(state);
            if (route == "ABYSS")
                return MainCampaignRouteForVerification158(route,
                    CanBrowseCampaignFromIdleTowerForVerification158(
                        (_coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022)
                            ?.CampaignProgression022, state, M2BattleViewAccess098.Read(_coordinator)), false);
            if (route == "WORLD GATE")
                return MainCampaignRouteForVerification158(route, false,
                    (_coordinator as Campaign023.ICampaignWorldGatePresentationCoordinator023)
                        ?.CampaignWorldGate023?.ActiveOperationKind == "CHAPTER");
            return route;
        }

        private void OpenMainCampaign158()
        {
            if (TryRouteLoopService164("CAMPAIGN")) return;
            CloseWalkableGuildHall069();
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "CAMPAIGN";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        private bool TryResumeMainCampaign158()
        {
            var state = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            var route = CurrentMainCampaignRoute158(state);
            if (string.IsNullOrWhiteSpace(route)) return false;
            // Opening card expeditions still resume their current story step.
            if (route == "EXPEDITION") return TryResumeActiveAdventure110();
            OpenMainCampaign158();
            return true;
        }

        private bool TryBuildCampaignResumeBoundary158(
            Transform body, GuildCity017D.GuildCityPresentationState017D state)
        {
            var route = CurrentMainCampaignRoute158(state);
            if (string.IsNullOrWhiteSpace(route) || route == "CAMPAIGN") return false;
            string copy;
            string action;
            switch (route)
            {
                case "BATTLE":
                    copy = "A battle is in progress. Continue it before returning to the campaign's three-card quests.";
                    action = "CONTINUE CURRENT BATTLE";
                    break;
                case "BATTLE RESULTS":
                    copy = "A saved battle result needs attention before the campaign can continue.";
                    action = "VIEW SAVED BATTLE RESULT";
                    break;
                case "ABYSS":
                    copy = "Your current Tower run needs attention. Finish it or return safely from the Tower, then continue the campaign.";
                    action = "RETURN TO CURRENT TOWER";
                    break;
                case "WORLD GATE":
                    copy = "Your current Guild contract is still active. Finish it before starting the next campaign quest.";
                    action = "RETURN TO GUILD CONTRACT";
                    break;
                case "ENCOUNTER":
                    copy = "Your current quest has a committed encounter. Resolve it before continuing the campaign.";
                    action = "CONTINUE COMMITTED ENCOUNTER";
                    break;
                default:
                    copy = "Your current quest is still active. Finish it before starting the next campaign quest.";
                    action = "CONTINUE CURRENT QUEST";
                    break;
            }
            var panel = AddMessagePanel(body, "MAIN CAMPAIGN", copy, RuntimeUi.Warning);
            var owner = _coordinator;
            RuntimeUi.AddButton(panel, "Campaign required resume 158", action,
                () =>
                {
                    if (panel == null || !panel.gameObject.activeInHierarchy ||
                        !ReferenceEquals(_coordinator, owner)) return;
                    if (!TryResumeActiveAdventure110()) OpenMainCampaign158();
                },
                122f, RuntimeUi.Accent);
            return true;
        }

        // Navigation reads the shared battle first, then returns to the operation
        // which still owns its receipts. It never clears an operation or claims
        // a reward merely because the player opened Missions or Continue.
        public static string ActiveAdventureRouteForVerification110(
            M2BattleView battle,
            bool hasUnclaimedBattleReward,
            bool hasPendingEncounter,
            bool hasPendingBattleReturn,
            bool hasActiveTower,
            bool hasActiveWorldGate,
            bool hasActiveCampaign,
            bool hasExpedition)
        {
            if (battle != null && !battle.IsResolved) return "BATTLE";
            if (hasUnclaimedBattleReward || battle?.Reward?.Claimed == false)
                return "BATTLE RESULTS";
            if (hasActiveTower) return "ABYSS";
            if (hasActiveWorldGate) return "WORLD GATE";
            if (hasActiveCampaign) return "CAMPAIGN";
            if (hasExpedition) return "EXPEDITION";
            if (hasPendingBattleReturn) return "BATTLE RESULTS";
            if (hasPendingEncounter) return "ENCOUNTER";
            return string.Empty;
        }

        private string CurrentActiveAdventureRoute110(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var battle = M2BattleViewAccess098.Read(_coordinator);
            // Avoid building unrelated operation projections at the common
            // battle/reward boundary, including the affected saved victory.
            if (battle != null && !battle.IsResolved) return "BATTLE";
            if (state?.HasUnclaimedBattleReward == true || battle?.Reward?.Claimed == false)
                return "BATTLE RESULTS";
            var tower = (_coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022)
                ?.CampaignProgression022;
            if (!string.IsNullOrWhiteSpace(tower?.ActiveAbyssOperationId)) return "ABYSS";
            var worldGate = (_coordinator as Campaign023.ICampaignWorldGatePresentationCoordinator023)
                ?.CampaignWorldGate023;
            if (!string.IsNullOrWhiteSpace(worldGate?.ActiveOperationId)) return "WORLD GATE";
            var playable = (_coordinator as Campaign020.ICampaignPlayablePresentationCoordinator020)
                ?.CampaignPlayable020;
            var hasCampaign = !string.IsNullOrWhiteSpace(playable?.ActiveOperationId);
            if (!hasCampaign)
            {
                var chapter = (_coordinator as Campaign019.ICampaignPresentationCoordinator019)?.Campaign019;
                hasCampaign = !string.IsNullOrWhiteSpace(chapter?.ActiveRequestId) ||
                              !string.IsNullOrWhiteSpace(chapter?.PendingReceiptId);
            }
            return ActiveAdventureRouteForVerification110(battle,
                state?.HasUnclaimedBattleReward == true,
                state?.HasPendingEncounter == true,
                state?.HasPendingBattleReturn == true,
                false, false, hasCampaign, state?.Expedition != null);
        }

        private static string ActiveAdventureActionLabel110(string route)
        {
            switch (route)
            {
                case "BATTLE": return "CONTINUE BATTLE";
                case "BATTLE RESULTS": return "VIEW BATTLE REWARD";
                case "ENCOUNTER": return "ENTER COMMITTED BATTLE";
                case "ABYSS": return "CONTINUE TOWER";
                case "WORLD GATE":
                case "CAMPAIGN":
                case "EXPEDITION": return "CONTINUE CURRENT QUEST";
                default: return string.Empty;
            }
        }

        private bool TryResumeActiveAdventure110()
        {
            var coordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            var state = coordinator?.GuildCity017D;
            var route = CurrentActiveAdventureRoute110(state);
            if (string.IsNullOrWhiteSpace(route)) return false;
            CloseWalkableGuildHall069();
            _localStatus = string.Empty;
            _localStatusPositive = true;
            if (route == "BATTLE" || route == "BATTLE RESULTS")
                Navigate(route == "BATTLE" ? M1Screen.Battle : M1Screen.BattleResults);
            else if (route == "ENCOUNTER" && coordinator != null)
                EnterCommittedGuildCityBattle017D(coordinator);
            else
            {
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = route;
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
            }
            return true;
        }

        private void OpenAdventure068()
        {
            var state = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            OpenStoryNextStep065(
                state,
                IsStoryContractCompleted065(state, FirstStoryContractId065),
                IsStoryContractCompleted065(state, SecondStoryContractId065),
                IsStoryContractCompleted065(state, ThirdStoryContractId065));
        }

        private void BuildGuildDetailsHub068(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddMessagePanel(
                body,
                "OPTIONAL MANAGEMENT",
                "The story and Quick Battle stay on HOME. Open these only when you want to manage the Guild.",
                RuntimeUi.MutedText);

            var firstRow = AddRow(body, "Guild Details First Row 068", 16f, 190f);
            AddGuildDetailButton068(firstRow, "RECRUIT ADVENTURERS", "APPLICANTS");
            AddGuildDetailButton068(firstRow, "TRAIN MEMBERS", "DEVELOPMENT");
            AddGuildDetailButton068(firstRow, "MEMBER STORIES", "PEOPLE");
            AddGuildDetailButton068(firstRow, "BONDS & MEMORIES", "RELATIONSHIPS");

            var secondRow = AddRow(body, "Guild Details Second Row 068", 16f, 190f);
            RuntimeUi.AddButton(secondRow, "Guild Details Edit Unions 068", "EDIT UNIONS", () => Navigate(M1Screen.UnionBuilder), 176f);
            AddGuildDetailButton068(secondRow, "HALL UPGRADES", "CITY");
            AddGuildDetailButton068(secondRow, "CHRONICLE", "CHRONICLE");
            RuntimeUi.AddButton(secondRow, "Guild Details Title And Options 068", "TITLE & OPTIONS", () => Navigate(M1Screen.MainMenu), 176f);

            var bonusRow = AddRow(body, "Guild Details Bonus Code Row 081", 16f, 104f);
            RuntimeUi.AddButton(
                bonusRow,
                "Guild Details Redeem Code 081",
                "REDEEM A BONUS CODE",
                () =>
                {
                    _guildCityTab017D = "CODES";
                    BuildCurrentScreen();
                },
                268f,
                RuntimeUi.Accent);

            if (IsStoryContractCompleted065(state, ThirdStoryContractId065))
            {
                var unlocked = AddRow(body, "Guild Details Unlocked Systems 068", 16f, 190f);
                AddGuildDetailButton068(unlocked, "CAMPAIGN", "CAMPAIGN");
                AddGuildDetailButton068(unlocked, "WORLD GATE", "WORLD GATE");
                AddGuildDetailButton068(unlocked, "CITY DEFENSE", "DEFENSE");
                AddGuildDetailButton068(unlocked, "PROGRESSION", "PROGRESSION");
            }
        }

        private void AddGuildDetailButton068(Transform parent, string label, string tab)
        {
            RuntimeUi.AddButton(
                parent,
                "Guild Details " + tab + " 068",
                label,
                () =>
                {
                    OpenTownService153(tab);
                },
                176f,
                RuntimeUi.ButtonNormal);
        }

        private void AddGuildMobileNavButton062(Transform parent, string name, string label, string tab)
        {
            var button = RuntimeUi.AddButton(
                parent,
                name,
                label,
                () =>
                {
                    OpenTownService153(tab);
                },
                RuntimeUi.MinimumTouchPixels,
                StringComparer.Ordinal.Equals(_guildCityTab017D, tab)
                    ? RuntimeUi.Accent
                    : RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 20, 34);
        }

        private static Image AddVisualSliceArtwork062(
            Transform parent,
            string name,
            string resourceKey,
            float flexibleWidth,
            float preferredHeight,
            string fallbackLabel)
        {
            var frame = RuntimeUi.AddPanel(parent, name + " Frame", Color.white);
            RuntimeUi.SetLayout(frame, preferredHeight: preferredHeight, flexibleWidth: flexibleWidth);
            M1PremiumUi.StylePanel(frame, M1PremiumUi.Surface.WorldPaper);

            var artwork = RuntimeUi.AddPanel(frame.transform, name, Color.white);
            Stretch(artwork.rectTransform);
            artwork.rectTransform.offsetMin = new Vector2(10f, 10f);
            artwork.rectTransform.offsetMax = new Vector2(-10f, -10f);
            artwork.raycastTarget = false;

            var sprite = ResolveVisualSliceSprite062(resourceKey);
            if (sprite != null)
            {
                artwork.sprite = sprite;
                artwork.type = Image.Type.Simple;
                artwork.preserveAspect = true;
                artwork.color = Color.white;
                return artwork;
            }

            artwork.sprite = null;
            artwork.color = new Color(0.035f, 0.055f, 0.07f, 0.92f);
            var fallback = RuntimeUi.AddText(
                artwork.transform,
                name + " Fallback",
                fallbackLabel,
                32,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            Stretch(fallback.rectTransform);
            fallback.raycastTarget = false;
            ConfigureResponsiveText062(fallback, 20, 32);
            return artwork;
        }

        private static Sprite ResolveVisualSliceSprite062(string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey)) return null;
            if (VisualSliceSprites062.TryGetValue(resourceKey, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>(resourceKey);
            if (texture == null)
            {
                VisualSliceSprites062[resourceKey] = null;
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + "_RUNTIME_SPRITE_062";
            VisualSliceSprites062[resourceKey] = sprite;
            return sprite;
        }

        private void BuildGuildHallHero062(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            var thirdComplete = IsStoryContractCompleted065(state, ThirdStoryContractId065);
            var hero = RuntimeUi.AddPanel(body, "Guild Hall First Contract Card 062", Color.white);
            RuntimeUi.SetLayout(hero, preferredHeight: 540f);
            M1PremiumUi.StylePanel(hero, M1PremiumUi.Surface.WorldGlass);
            hero.color = new Color(1f, 1f, 1f, _highContrast ? 1f : 0.88f);
            RuntimeUi.AddHorizontalLayout(hero.transform, new RectOffset(24, 24, 20, 20), 24f, TextAnchor.MiddleLeft);

            AddVisualSliceArtwork062(
                hero.transform,
                "Guild Hall Guide Character Art 062",
                GuildHallHomeBackgroundResource063,
                1.72f,
                500f,
                "SKYHOME GUILD HALL");

            var copy = new GameObject("Guild Hall Story Invitation 062", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            copy.SetParent(hero.transform, false);
            RuntimeUi.SetLayout(copy, preferredHeight: 500f, flexibleWidth: 0.88f);
            RuntimeUi.AddVerticalLayout(copy, new RectOffset(16, 16, 12, 12), 8f, TextAnchor.MiddleLeft);
            AddResponsiveText062(
                copy,
                "Guild Hall Welcome 062",
                StoryChapterHeading065(firstComplete, secondComplete, thirdComplete),
                30,
                48,
                82f,
                RuntimeUi.Text,
                FontStyle.Bold);
            AddResponsiveText062(
                copy,
                "Guild Hall Invitation 062",
                StoryNextObjective065(state, firstComplete, secondComplete, thirdComplete),
                24,
                36,
                126f,
                RuntimeUi.Text);
            AddResponsiveText062(
                copy,
                "Guild Hall Promise 062",
                "ONE CLEAR NEXT STEP • QUICK BATTLE ALWAYS AVAILABLE",
                22,
                30,
                40f,
                RuntimeUi.Positive,
                FontStyle.Bold);

            var actions = AddRow(copy, "Guild Hall Play Actions 068", 12f, 122f);
            RuntimeUi.AddButton(
                actions,
                "Guild Hall Continue Story 068",
                StoryNextActionLabel065(state, firstComplete, secondComplete, thirdComplete),
                () => OpenStoryNextStep065(state, firstComplete, secondComplete, thirdComplete),
                116f,
                RuntimeUi.Accent);
            RuntimeUi.AddButton(
                actions,
                "Guild Hall Quick Battle 068",
                BattleNowLabel060(),
                OpenBattleNow060,
                116f,
                RuntimeUi.Positive);
        }

        private void BuildGuildHallAtAGlance063(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var glance = RuntimeUi.AddPanel(body, "Guild Hall At A Glance 063", Color.white);
            RuntimeUi.SetLayout(glance, preferredHeight: 124f);
            M1PremiumUi.StylePanel(glance, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddHorizontalLayout(
                glance.transform,
                new RectOffset(20, 20, 12, 12),
                12f,
                TextAnchor.MiddleCenter);

            AddHallGlanceItem063(
                glance.transform,
                "Adventurers",
                Math.Max(state.TotalRecruitCount, _coordinator.State.Recruits?.Count ?? 0) + " ADVENTURERS",
                "OPERATION " + state.OperationOrdinal + " • Signed and staying");
            AddHallGlanceItem063(
                glance.transform,
                "Unions",
                Math.Max(state.NormalUnionCount, _coordinator.State.UsedUnionCount) + " UNIONS READY",
                "HALL XP " + state.HallEnhancementXp + " • Formation saved");
            AddHallGlanceItem063(
                glance.transform,
                "Next Story",
                state.HasPendingEncounter
                    ? "BATTLE AHEAD"
                    : state.HasActiveContract
                        ? "ROUTE READY"
                        : IsStoryContractCompleted065(state, FirstStoryContractId065)
                            ? "CHAPTER 2 READY"
                            : "CONTRACT WAITING",
                "TRUST " + state.CivicTrust + " • " +
                (state.HasActiveContract
                    ? ActiveStoryContractName065(state)
                    : IsStoryContractCompleted065(state, FirstStoryContractId065)
                        ? "The Door Inside"
                        : "Talk to the Guild Guide"));

            if (IsStoryContractCompleted065(state, FirstStoryContractId065))
            {
                var recruitment = RuntimeUi.AddPanel(body, "Post Quest Recruitment Opportunity 066", Color.white);
                RuntimeUi.SetLayout(recruitment, preferredHeight: 150f);
                M1PremiumUi.StylePanel(recruitment, M1PremiumUi.Surface.WorldPaper);
                RuntimeUi.AddHorizontalLayout(
                    recruitment.transform,
                    new RectOffset(20, 20, 10, 10),
                    14f,
                    TextAnchor.MiddleLeft);
                RuntimeUi.AddText(
                    recruitment.transform,
                    "Post Quest Recruitment Copy 066",
                    "THE LANTERN PATROL CAME HOME\nMeet the rescued adventurers, hear what the Wayglass showed them, and welcome new guildmates.",
                    28,
                    TextAnchor.MiddleLeft,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                RuntimeUi.AddButton(
                    recruitment.transform,
                    "Post Quest Meet More Applicants 066",
                    "MEET MORE APPLICANTS",
                    () =>
                    {
                        _guildCityTab017D = "APPLICANTS";
                        BuildCurrentScreen();
                    },
                    120f,
                    RuntimeUi.Accent);
            }

            if (ShouldShowGuildGrowthReveal065(state))
                AddGuildGrowthReveal065(body, state);

            if (!_guildCityMoreOpen060) return;

            AddMessagePanel(
                body,
                "OPTIONAL GUILD MANAGEMENT",
                "Recruitment, equipment, Union editing, city building, and advanced records are available below.",
                RuntimeUi.MutedText);
            var facilities = AddRow(body, "Guild Hall Hotspots 017D", 16f, 190f);
            var applicants = RuntimeUi.AddButton(
                facilities,
                "Applicants 017D",
                "RECRUIT",
                () => { _guildCityTab017D = "APPLICANTS"; BuildCurrentScreen(); },
                176f);
            M1PremiumUi.StyleLocationHotspot(applicants, "A", "APPLICANTS");
            var equipment = RuntimeUi.AddButton(
                facilities,
                "Equipment 017D",
                "EQUIPMENT",
                () => OpenInventoryForRecruit068(_selectedRecruitId),
                176f);
            M1PremiumUi.StyleLocationHotspot(equipment, "EQ", "LOADOUTS");
            var unions = RuntimeUi.AddButton(
                facilities,
                "Unions 017D",
                "EDIT UNIONS",
                () => Navigate(M1Screen.UnionBuilder),
                176f);
            M1PremiumUi.StyleLocationHotspot(unions, "III", "FORMATION");
            var city = RuntimeUi.AddButton(
                facilities,
                "City 017D",
                "UPGRADE HALL",
                () => { _guildCityTab017D = "CITY"; BuildCurrentScreen(); },
                176f);
            M1PremiumUi.StyleLocationHotspot(city, "CITY", "SKYHOME");
            RuntimeUi.AddButton(
                body,
                "Guild Hall Practice Battle 062",
                "PRACTICE BATTLE",
                OpenBattleNow060,
                RuntimeUi.MinimumTouchPixels);
        }

        private static void AddHallGlanceItem063(
            Transform parent,
            string name,
            string heading,
            string detail)
        {
            var item = RuntimeUi.AddPanel(parent, "Guild Hall Glance " + name + " 063", Color.white);
            RuntimeUi.SetLayout(item, preferredHeight: 98f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(item, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(item.transform, new RectOffset(16, 16, 8, 8), 2f, TextAnchor.MiddleCenter);
            AddResponsiveText062(
                item.transform,
                "Guild Hall Glance Heading " + name + " 063",
                heading,
                22,
                32,
                42f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            AddResponsiveText062(
                item.transform,
                "Guild Hall Glance Detail " + name + " 063",
                detail,
                18,
                26,
                30f,
                RuntimeUi.Text,
                FontStyle.Normal,
                TextAnchor.MiddleCenter);
        }

        private static string ActiveStoryContractName065(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var active = state?.Contracts?.FirstOrDefault(contract =>
                contract != null && contract.IsActive && !contract.IsCompleted && !contract.IsFailed);
            return active?.DisplayName ?? "Active story contract";
        }

        private static bool ShouldShowGuildGrowthReveal065(
            GuildCity017D.GuildCityPresentationState017D state) =>
            IsStoryContractCompleted065(state, FirstStoryContractId065) ||
            HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED");

        private void AddGuildGrowthReveal065(
            Transform body,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var recruits = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(recruit => recruit != null)
                .OrderByDescending(recruit => recruit.TotalPersonalXp)
                .ThenByDescending(recruit => recruit.ArtMastery?.Sum(art => art?.MasteryPoints ?? 0) ?? 0)
                .ThenBy(recruit => recruit.DisplayName, StringComparer.Ordinal)
                .Take(3)
                .ToArray();
            if (recruits.Length == 0) return;

            var reveal = RuntimeUi.AddPanel(body, "First Contract Guild Growth Reveal 065", Color.white);
            RuntimeUi.SetLayout(reveal, preferredHeight: 566f);
            M1PremiumUi.StylePanel(reveal, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(reveal.transform, new RectOffset(22, 22, 12, 12), 8f, TextAnchor.MiddleLeft);
            AddResponsiveText062(
                reveal.transform,
                "First Contract Guild Growth Heading 065",
                "FIRST CONTRACT  •  MEMBER GROWTH SAVED",
                26,
                42,
                50f,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AddResponsiveText062(
                reveal.transform,
                "First Contract Guild Growth Explanation 065",
                "This growth is saved with your Guild. XP, learned Arts, equipment, and bonds remain when you return.",
                18,
                28,
                54f,
                RuntimeUi.Text);

            var cards = AddRow(reveal.transform, "First Contract Three Member Growth Cards 065", 12f, 260f);
            foreach (var recruit in recruits)
                AddMemberGrowthCard065(cards, recruit, state.Relationships);

            var consequence = "HALL CONSEQUENCE  •  " + state.HallEnhancementXp + " HALL XP  •  TRUST " + state.CivicTrust;
            var project = state.Plots == null
                ? null
                : state.Plots.Where(plot => plot != null && !plot.Unlocked)
                    .OrderByDescending(plot => plot.ConstructionProgress)
                    .FirstOrDefault();
            if (project != null)
                consequence += "  •  NEXT CITY PROJECT " + project.ConstructionProgress + "/" +
                               global::SecondDimension.Gameplay.GuildCity017D.GuildCityCommandService017D.OpeningPlotUnlockProgress;
            if (state.UnviewedRelationshipCount > 0)
                consequence += "  •  " + state.UnviewedRelationshipCount +
                               (state.UnviewedRelationshipCount == 1
                                   ? " NEW HALL MEMORY"
                                   : " NEW HALL MEMORIES");
            AddResponsiveText062(
                reveal.transform,
                "First Contract Hall Consequence 065",
                consequence,
                18,
                28,
                44f,
                RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            var actions = AddRow(reveal.transform, "First Contract Growth Actions 065", 12f, 76f);
            if (state.UnviewedRelationshipCount > 0)
                RuntimeUi.AddButton(
                    actions,
                    "View First Contract Hall Memory 065",
                    "VIEW NEW HALL MEMORY",
                    () =>
                    {
                        _guildCityTab017D = "RELATIONSHIPS";
                        _guildCityMoreOpen060 = true;
                        BuildCurrentScreen();
                    },
                    190f,
                    RuntimeUi.Positive);
            if (IsStoryContractCompleted065(state, FirstStoryContractId065) &&
                !IsStoryContractCompleted065(state, SecondStoryContractId065))
                RuntimeUi.AddButton(
                    actions,
                    "Begin Chapter Two From Growth 065",
                    "BEGIN CHAPTER 2",
                    () =>
                    {
                        _guildCityTab017D = "CONTRACTS";
                        BuildCurrentScreen();
                    },
                    190f,
                    RuntimeUi.Accent);
        }

        private static void AddMemberGrowthCard065(
            Transform parent,
            M1RecruitLoadoutView recruit,
            IReadOnlyList<GuildCity017D.GuildCityRelationshipView017D> relationships)
        {
            var card = RuntimeUi.AddPanel(parent, "Member Growth " + recruit.RecruitId + " 065", Color.white);
            RuntimeUi.SetLayout(card, preferredHeight: 250f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(card.transform, new RectOffset(14, 14, 8, 8), 3f, TextAnchor.MiddleLeft);

            AddResponsiveText062(
                card.transform,
                "Member Growth Name " + recruit.RecruitId + " 065",
                (recruit.DisplayName ?? recruit.RecruitId).ToUpperInvariant() + "  •  LV " + Math.Max(1, recruit.Level),
                20,
                30,
                38f,
                RuntimeUi.Accent,
                FontStyle.Bold);
            var nextLevel = recruit.XpRequiredForNextLevel <= 0
                ? "MAX LEVEL"
                : recruit.XpIntoCurrentLevel + "/" + recruit.XpRequiredForNextLevel + " TO NEXT LEVEL";
            AddResponsiveText062(
                card.transform,
                "Member Growth XP " + recruit.RecruitId + " 065",
                "PERSONAL XP " + recruit.TotalPersonalXp + "  •  " + nextLevel,
                16,
                24,
                34f,
                RuntimeUi.Positive,
                FontStyle.Bold);

            var art = recruit.ArtMastery?
                .Where(value => value != null)
                .OrderByDescending(value => value.MasteryPoints)
                .ThenByDescending(value => value.MeaningfulUses)
                .FirstOrDefault();
            var artLine = art == null
                ? "ART GROWTH  •  No meaningful use recorded yet"
                : "ART GROWTH  •  " + art.DisplayName + "  •  MASTERY " + art.MasteryPoints +
                  "  •  USES " + art.MeaningfulUses;
            AddResponsiveText062(
                card.transform,
                "Member Growth Art " + recruit.RecruitId + " 065",
                artLine,
                15,
                22,
                48f,
                art == null ? RuntimeUi.MutedText : RuntimeUi.Warning,
                FontStyle.Bold);

            var equipped = recruit.Slots?
                .Where(slot => slot != null && !string.IsNullOrWhiteSpace(slot.EquippedItemId))
                .OrderBy(slot => slot.SlotId != null && slot.SlotId.IndexOf("WEAPON", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
                .FirstOrDefault();
            var bond = relationships?
                .Where(value => value != null &&
                    (StringComparer.Ordinal.Equals(value.FirstRecruitId, recruit.RecruitId) ||
                     StringComparer.Ordinal.Equals(value.SecondRecruitId, recruit.RecruitId)))
                .OrderByDescending(value => value.Strength)
                .FirstOrDefault();
            var durableLine = "GEAR  •  " + (equipped?.EquippedItemName ?? "No equipped reward yet") +
                              "\nBOND  •  " + (bond == null ? "No shared memory yet" : bond.Summary + "  (" + bond.Strength + ")");
            AddResponsiveText062(
                card.transform,
                "Member Growth Durable Results " + recruit.RecruitId + " 065",
                durableLine,
                14,
                21,
                72f,
                RuntimeUi.Text);
        }

        private void BuildGuildCityParty063(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            var unions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null && value.MemberRecruitIds.Count > 0)
                .OrderBy(value => value.Index)
                .ToArray();

            AddPartyPrimaryAction063(body, coordinator, state);

            var partyArtwork = AddVisualSliceArtwork062(
                body,
                "Party Strategy Room Artwork 063",
                PartyStrategyRoomResource063,
                1f,
                410f,
                "SKYHOME STRATEGY ROOM");

            if (unions.Length == 0)
            {
                var availability = RuntimeUi.AddPanel(
                    partyArtwork.transform,
                    "Party Record Availability 063",
                    new Color(0.02f, 0.04f, 0.05f, 0.88f));
                availability.rectTransform.anchorMin = new Vector2(0.24f, 0.28f);
                availability.rectTransform.anchorMax = new Vector2(0.76f, 0.72f);
                availability.rectTransform.offsetMin = Vector2.zero;
                availability.rectTransform.offsetMax = Vector2.zero;
                M1PremiumUi.StylePanel(availability, M1PremiumUi.Surface.WorldPaper);
                var summary = RuntimeUi.AddText(
                    availability.transform,
                    "Party Record Availability Text 063",
                    _coordinator.State.OpeningUnionsLegal
                        ? "SAVED UNION PLANS ARE READY\nLive portraits will appear when the party finishes loading."
                        : "FORM TWO READY UNIONS\nOpen Union Builder and place every signed adventurer before departing.",
                    28,
                    TextAnchor.MiddleCenter,
                    _coordinator.State.OpeningUnionsLegal ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold);
                Stretch(summary.rectTransform);
                summary.rectTransform.offsetMin = new Vector2(18f, 12f);
                summary.rectTransform.offsetMax = new Vector2(-18f, -12f);
                ConfigureResponsiveText062(summary, 18, 28);
            }
            else
            {
                var unionRow = RuntimeUi.AddPanel(
                    partyArtwork.transform,
                    "Party Union Cards 063",
                    Color.clear);
                Stretch(unionRow.rectTransform);
                unionRow.rectTransform.offsetMin = new Vector2(22f, 22f);
                unionRow.rectTransform.offsetMax = new Vector2(-22f, -22f);
                RuntimeUi.AddHorizontalLayout(
                    unionRow.transform,
                    new RectOffset(8, 8, 8, 8),
                    14f,
                    TextAnchor.MiddleCenter);
                for (var index = 0; index < unions.Length; index++)
                {
                    if (index > 0)
                    {
                        var spacer = new GameObject(
                            "Party Strategy Room Safe Zone " + index + " 063",
                            typeof(RectTransform),
                            typeof(LayoutElement)).GetComponent<RectTransform>();
                        spacer.SetParent(unionRow.transform, false);
                        RuntimeUi.SetLayout(spacer, preferredHeight: 1f, flexibleWidth: 0.12f);
                    }
                    AddPartyUnionCard063(unionRow.transform, unions[index], recruits);
                }
            }

            var edit = AddRow(body, "Party Secondary Actions 063", 12f, 104f);
            RuntimeUi.AddButton(
                edit,
                "Party Edit Unions 063",
                "EDIT UNIONS",
                () => Navigate(M1Screen.UnionBuilder),
                RuntimeUi.MinimumTouchPixels);
            RuntimeUi.AddButton(
                edit,
                "Party Edit Loadouts 063",
                "EDIT LOADOUTS",
                () => OpenInventoryForRecruit068(_selectedRecruitId),
                RuntimeUi.MinimumTouchPixels);
            RuntimeUi.AddButton(
                edit,
                "Party Verify Guild Record 063",
                "CHECK PARTY READINESS",
                VerifyGuildRecordFromParty063,
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);

            if (_partyGuildRecordVerified063)
            {
                AddMessagePanel(
                    body,
                    "FOUNDING PARTY READY",
                    "Your " + state.TotalRecruitCount + " members have ready equipment and Union plans.",
                    RuntimeUi.Positive);
                var proof = AddRow(body, "Premium Completion Medallions", 12f, 112f);
                M1PremiumUi.AddProofMedallion(
                    proof,
                    "Party Recruit Proof 063",
                    "VI",
                    "ADVENTURERS",
                    state.TotalRecruitCount + " MEMBERS",
                    _coordinator.State.AllSixSigned);
                M1PremiumUi.AddProofMedallion(
                    proof,
                    "Party Equipment Proof 063",
                    "EQ",
                    "EQUIPMENT",
                    "READY",
                    _coordinator.State.OpeningEquipmentLegal);
                M1PremiumUi.AddProofMedallion(
                    proof,
                    "Party Union Proof 063",
                    _coordinator.State.UsedUnionCount.ToString(),
                    "UNIONS",
                    _coordinator.State.UsedUnionCount + " READY",
                    _coordinator.State.OpeningUnionsLegal);
            }

            if (!string.IsNullOrWhiteSpace(_localStatus))
                AddStatus(body, _localStatus, _localStatusPositive);
        }

        private void AddPartyPrimaryAction063(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            var thirdComplete = IsStoryContractCompleted065(state, ThirdStoryContractId065);
            var secondActive = !secondComplete &&
                               IsStoryContractActive065(state, SecondStoryContractId065);
            var thirdActive = !thirdComplete &&
                              IsStoryContractActive065(state, ThirdStoryContractId065);
            var readyFor = secondActive
                ? "The Door Inside"
                : thirdActive
                    ? "the relief-road operation"
                    : !firstComplete
                        ? "the Lantern Patrol rescue"
                        : "the current Guild operation";
            var next = RuntimeUi.AddPanel(body, "Party Next Action 063", Color.white);
            RuntimeUi.SetLayout(next, preferredHeight: 160f);
            M1PremiumUi.StylePanel(next, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(next.transform, new RectOffset(24, 24, 10, 10), 4f, TextAnchor.MiddleCenter);
            AddResponsiveText062(
                next.transform,
                "Party Readiness Summary 063",
                _coordinator.State.OpeningUnionsLegal
                    ? _coordinator.State.UsedUnionCount + " Union plans are ready for " + readyFor + "."
                    : "Finish forming ready Union plans before departing.",
                20,
                30,
                38f,
                _coordinator.State.OpeningUnionsLegal ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            Button ready;
            if (ShouldShowChapterTwoOpening076(state))
            {
                ready = RuntimeUi.AddButton(
                    next.transform,
                    "Return To Chapter Two Wayglass 076",
                    "RETURN TO WAYGLASS",
                    () =>
                    {
                        _guildCityTab017D = "CHAPTER2";
                        BuildCurrentScreen();
                    },
                    96f,
                    RuntimeUi.Accent);
            }
            else if (state.HasActiveContract && state.Expedition == null)
            {
                ready = RuntimeUi.AddButton(
                    next.transform,
                    "First Contract Begin Expedition 062",
                    "BEGIN EXPEDITION",
                    () => BeginFirstContractFromParty063(coordinator),
                    96f,
                    RuntimeUi.Accent);
            }
            else
            {
                ready = RuntimeUi.AddButton(
                    next.transform,
                    "Party Ready For Expedition 063",
                    state.HasActiveContract
                        ? secondActive
                            ? "CONTINUE THE DOOR INSIDE"
                            : thirdActive
                                ? "CONTINUE RELIEF ROAD"
                                : "CONTINUE QUEST"
                        : firstComplete
                            ? "VIEW NEXT STORY ORDER"
                            : "VIEW FIRST CONTRACT",
                    () =>
                    {
                        _guildCityTab017D = state.HasActiveContract ? "EXPEDITION" : "CONTRACTS";
                        BuildCurrentScreen();
                    },
                    96f,
                    RuntimeUi.Accent);
            }
            ready.interactable = _coordinator.State.OpeningUnionsLegal;
            if (ready.interactable)
            {
                ready.Select();
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(
                        ready.gameObject);
            }
        }

        private static void AddPartyUnionCard063(
            Transform parent,
            M1UnionView union,
            IReadOnlyList<M1RecruitLoadoutView> recruits)
        {
            var card = RuntimeUi.AddPanel(parent, "Party Union " + union.Index + " 063", Color.white);
            RuntimeUi.SetLayout(card, preferredHeight: 320f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(card.transform, new RectOffset(18, 18, 12, 12), 8f, TextAnchor.MiddleCenter);

            var leader = recruits.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, union.LeaderRecruitId));
            AddResponsiveText062(
                card.transform,
                "Party Union Heading " + union.Index + " 063",
                string.IsNullOrWhiteSpace(union.DisplayName) ? "UNION " + (union.Index + 1) : union.DisplayName.ToUpperInvariant(),
                28,
                42,
                44f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            AddResponsiveText062(
                card.transform,
                "Party Union Leader " + union.Index + " 063",
                "Leader: " + (leader?.DisplayName ?? "Choose a leader") +
                "  •  HP " + union.CombinedCurrentHp + "/" + union.CombinedMaximumHp,
                20,
                30,
                38f,
                union.IsLegal ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            var members = AddRow(card.transform, "Party Union Members " + union.Index + " 063", 8f, 190f);
            foreach (var memberId in union.MemberRecruitIds.Take(3))
            {
                var recruit = recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, memberId));
                if (recruit == null) continue;
                var portrait = RuntimeUi.AddButton(
                    members,
                    "Party Member " + recruit.RecruitId + " 063",
                    recruit.DisplayName + "\n" + recruit.ObservedClass + " • LV " + Math.Max(1, recruit.Level),
                    null,
                    170f,
                    new Color(0.035f, 0.055f, 0.07f, 0.76f));
                portrait.interactable = false;
                AddPortraitToButton(portrait, recruit, large: false);
                M1PremiumUi.AddClassCrest(portrait, recruit.ClassSymbol, recruit.ObservedClass);
                ConfigureResponsiveText062(portrait.GetComponentInChildren<Text>(), 18, 28);
            }
        }

        private void VerifyGuildRecordFromParty063()
        {
            var result = _coordinator.SaveAndReloadProof();
            _localStatusPositive = result != null && result.Succeeded;
            _localStatus = _localStatusPositive
                ? "Party readiness confirmed. Your roster, equipment, and Union plans are saved."
                : "Party readiness could not be checked. Please review the highlighted items and try again.";
            _partyGuildRecordVerified063 = _localStatusPositive;
            BuildCurrentScreen();
        }

        private void BeginFirstContractFromParty063(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            var result = coordinator.StartGuildCityExpedition017D();
            _localStatus = result?.Message ?? "The expedition could not begin.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
            {
                if (ShouldEnterGuidedFirstHourField076(coordinator))
                {
                    EnterExpeditionBoard074(coordinator);
                    return;
                }
                _guildCityTab017D = ShouldShowChapterTwoOpening076(coordinator.GuildCity017D)
                    ? "CHAPTER2"
                    : "EXPEDITION";
            }
            BuildCurrentScreen();
        }

        private string BattleNowLabel060()
        {
            var battle = _coordinator?.State?.Battle;
            if (battle != null && !battle.IsResolved) return "RESUME BATTLE";
            if (battle != null && battle.IsResolved && battle.Reward != null && !battle.Reward.Claimed)
                return "CLAIM BATTLE REWARDS";
            var city = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            return city != null && city.HasPendingEncounter ? "ENTER STORY BATTLE" : "PLAY A BATTLE NOW";
        }

        private void OpenBattleNow060()
        {
            var battle = _coordinator?.State?.Battle;
            if (battle != null && !battle.IsResolved)
            {
                Navigate(M1Screen.Battle);
                return;
            }
            if (battle != null && battle.IsResolved && battle.Reward != null && !battle.Reward.Claimed)
            {
                Navigate(M1Screen.BattleResults);
                return;
            }
            var city = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            if (city != null && city.HasPendingEncounter)
            {
                EnterCommittedGuildCityBattle017D((GuildCity017D.IGuildCityPresentationCoordinator017D)_coordinator);
                return;
            }
            BeginTutorialBattle();
        }

        private string AdventureLabel060()
        {
            var city = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)?.GuildCity017D;
            if (city == null) return "RUN THE GUILD";
            if (city.HasPendingEncounter) return "ENTER STORY BATTLE";
            if (!city.HasActiveContract) return "CHOOSE A STORY CONTRACT";
            return city.Expedition == null ? "START EXPEDITION" : "CONTINUE EXPEDITION";
        }

        private void ContinueAdventure060()
        {
            var coordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            var city = coordinator?.GuildCity017D;
            if (city == null)
            {
                Navigate(M1Screen.Complete);
                return;
            }
            if (city.HasPendingEncounter)
            {
                EnterCommittedGuildCityBattle017D(coordinator);
                return;
            }
            _guildCityTab017D = ShouldShowChapterTwoOpening076(city)
                ? "CHAPTER2"
                : city.HasActiveContract ? "EXPEDITION" : "CONTRACTS";
            Navigate(M1Screen.GuildOperations);
        }

        private void BuildLivingGuildHall017D(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddResponsiveText062(
                body,
                "Guild Hall Home Promise 062",
                "THE GUILD IS A HOME, NOT A TIMER.",
                30,
                46,
                76f,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AddResponsiveText062(
                body,
                "Active City Effects 017D",
                state.ActiveCityEffects.Count == 0
                    ? "The Ruined Annex is ready for its first improvement."
                    : string.Join("  •  ", state.ActiveCityEffects.Take(3)),
                22,
                32,
                66f,
                RuntimeUi.Warning,
                FontStyle.Bold);

            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            if (recruits.Count > 0)
            {
                var companions = AddRow(body, "Guild Hall Companion Portraits 062", 18f, 260f);
                foreach (var recruit in recruits.Take(4))
                {
                    var captured = recruit;
                    var portrait = RuntimeUi.AddButton(
                        companions,
                        "Guild Hall Companion " + captured.RecruitId + " 062",
                        captured.DisplayName + "\n" + captured.ObservedClass + " • LV " + Math.Max(1, captured.Level),
                        () =>
                        {
                            _selectedRecruitId = captured.RecruitId;
                            _guildCityTab017D = "DEVELOPMENT";
                            BuildCurrentScreen();
                        },
                        250f,
                        new Color(0.025f, 0.045f, 0.055f, 0.72f));
                    AddPortraitToButton(portrait, captured, large: false);
                    M1PremiumUi.AddClassCrest(portrait, captured.ClassSymbol, captured.ObservedClass);
                    ConfigureResponsiveText062(portrait.GetComponentInChildren<Text>(), 22, 34);
                }
            }

            var facilities = AddRow(body, "Guild Hall Hotspots 017D", 16f, 220f);
            var applicants = RuntimeUi.AddButton(facilities, "Applicants 017D", "APPLICANTS",
                () => { _guildCityTab017D = "APPLICANTS"; BuildCurrentScreen(); }, 210f);
            M1PremiumUi.StyleLocationHotspot(applicants, "A", "RECRUITMENT");
            var equipment = RuntimeUi.AddButton(facilities, "Equipment 017D", "EQUIPMENT",
                () => OpenInventoryForRecruit068(_selectedRecruitId), 210f);
            M1PremiumUi.StyleLocationHotspot(equipment, "EQ", "ARMORY");
            var unions = RuntimeUi.AddButton(facilities, "Unions 017D", "UNIONS",
                () => Navigate(M1Screen.UnionBuilder), 210f);
            M1PremiumUi.StyleLocationHotspot(unions, "III", "COMMAND");
            var city = RuntimeUi.AddButton(facilities, "City 017D", "BUILD CITY",
                () => { _guildCityTab017D = "CITY"; BuildCurrentScreen(); }, 210f, RuntimeUi.Accent);
            M1PremiumUi.StyleLocationHotspot(city, "CITY", "SETTLEMENT", selected: true);
            var development = RuntimeUi.AddButton(facilities, "Member Development 067", "DEVELOP",
                () => { _guildCityTab017D = "DEVELOPMENT"; BuildCurrentScreen(); }, 210f);
            M1PremiumUi.StyleLocationHotspot(development, "XP", "MEMBER PATHS");

            if (!_guildCityMoreOpen060) return;

            BuildGuildMemberDuties097(body, state.Assignments, (recruitId, kind) =>
                ApplyGuildCity017D(coordinator.SetGuildCityAssignment017D(recruitId, kind)));
        }

        private void BuildGuildMemberDuties097(
            Transform body,
            IReadOnlyList<GuildCity017D.GuildCityAssignmentView017D> source,
            Action<string, string> assign)
        {
            // Preserve the authoritative order and exact assignment IDs. Paging is
            // presentation only: members after the first six must remain reachable.
            var assignments = (source ?? Array.Empty<GuildCity017D.GuildCityAssignmentView017D>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId)).ToArray();
            var lastPage = Mathf.Max(0, (assignments.Length - 1) / GuildDutiesPageSize097);
            _guildDutyPage097 = Mathf.Clamp(_guildDutyPage097, 0, lastPage);
            var start = _guildDutyPage097 * GuildDutiesPageSize097;
            var visibleCount = Math.Min(GuildDutiesPageSize097, assignments.Length - start);
            var roster = AddColumnPanel(body, "Parallel Member Assignments 017D", 1f,
                300f + visibleCount * 132f);
            var dutyHeading097 = RuntimeUi.AddText(roster, "Parallel Assignment Heading 017D",
                "MEMBER DUTIES — OPTIONAL MANAGEMENT", 38, TextAnchor.MiddleLeft,
                RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.SetLayout(dutyHeading097, preferredHeight: 60f).minHeight = 60f;
            var dutyPageSummary097 = RuntimeUi.AddText(roster, "Member Duty Page Summary 097",
                assignments.Length == 0 ? "No member duties are available yet." :
                    "MEMBERS " + (start + 1) + "–" + (start + visibleCount) + " OF " + assignments.Length +
                    "  •  PAGE " + (_guildDutyPage097 + 1) + " / " + (lastPage + 1),
                28, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            // Reserve real line heights: text preferred-size calculation can run
            // before its width is settled when this scroll page first opens.
            RuntimeUi.SetLayout(dutyPageSummary097, preferredHeight: 48f).minHeight = 48f;
            if (assignments.Length > GuildDutiesPageSize097)
            {
                var pages = AddRow(roster, "Guild Member Duty Page Controls 097", 12f, 86f);
                var previous = RuntimeUi.AddButton(pages, "Previous Member Duties 097", "← PREVIOUS MEMBERS", () =>
                {
                    _guildDutyPage097 = Mathf.Max(0, _guildDutyPage097 - 1);
                    BuildCurrentScreen();
                }, 82f);
                previous.interactable = _guildDutyPage097 > 0;
                var next = RuntimeUi.AddButton(pages, "Next Member Duties 097", "MORE MEMBERS →", () =>
                {
                    _guildDutyPage097 = Mathf.Min(lastPage, _guildDutyPage097 + 1);
                    BuildCurrentScreen();
                }, 82f);
                next.interactable = _guildDutyPage097 < lastPage;
            }
            foreach (var assignment in assignments.Skip(start).Take(GuildDutiesPageSize097))
            {
                var row = AddRow(roster, "Assignment " + assignment.RecruitId, 12f, 120f);
                RuntimeUi.AddText(row, "Assignment Summary " + assignment.RecruitId,
                    assignment.RecruitName + "  •  " + assignment.Kind.ToUpperInvariant() +
                    (string.IsNullOrWhiteSpace(assignment.FacilityId) ? string.Empty : "  •  " + CleanHallStageName(assignment.FacilityId)) +
                    "\nRECOVERY " + assignment.RecoveryProgress + "  •  TRAINING " + assignment.TrainingProgress +
                    "  •  DUTY " + assignment.DutyProgress,
                    30, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
                RuntimeUi.AddButton(row, "Train " + assignment.RecruitId, "TRAIN",
                    () => assign(assignment.RecruitId, "Training"), 108f);
                RuntimeUi.AddButton(row, "Recover " + assignment.RecruitId, "RECOVER",
                    () => assign(assignment.RecruitId, "Recovering"), 108f);
                RuntimeUi.AddButton(row, "Reserve " + assignment.RecruitId, "RESERVE",
                    () => assign(assignment.RecruitId, "Reserve"), 108f);
            }
        }

        private void BuildGuildMemberDevelopment067(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddMessagePanel(
                body,
                "PERMANENT MEMBER PATHS",
                "Every recruit has a fixed class identity, weapon family, and role path. Growth outlook changes starting breadth and training pace—not whether a person is usable. There are no pulls, stars, expiry, or automatic departures.",
                RuntimeUi.Accent);
            RuntimeUi.AddText(
                body,
                "Member Development Resources 067",
                "XP TO SPEND  •  " + state.TreasuryXp +
                "     TRAINING GROUNDS REDUCE CLASS / ART SESSION COSTS",
                30,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);

            var members = GuildCity017D.GuildMemberDevelopmentBridge067.Members067(coordinator)
                .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToArray();
            if (members.Length == 0)
            {
                AddMessagePanel(body, "NO ACTIVE MEMBERS", "Recruit a member before opening Development.",
                    RuntimeUi.Warning);
                return;
            }
            if (members.All(value => !StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId)))
                _selectedRecruitId = members[0].RecruitId;
            var selectedIndex = Array.FindIndex(members, value =>
                StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId));
            if (selectedIndex >= 0 && selectedIndex / 3 != _guildMemberPage067)
                _guildMemberPage067 = selectedIndex / 3;
            _guildMemberPage067 = Mathf.Clamp(_guildMemberPage067, 0,
                Mathf.Max(0, (members.Length - 1) / 3));

            var cards = AddRow(body, "Guild Member Path Cards 067", 16f, 360f);
            foreach (var member in members.Skip(_guildMemberPage067 * 3).Take(3))
            {
                var captured = member;
                var selected = StringComparer.Ordinal.Equals(captured.RecruitId, _selectedRecruitId);
                var button = RuntimeUi.AddButton(
                    cards,
                    "Guild Member Path Card " + captured.RecruitId + " 067",
                    captured.DisplayName.ToUpperInvariant() + "\n" +
                    captured.ClassSymbol + "  " + FriendlyGrowthPath067(captured.StartingClassId) +
                    "  •  LV " + Math.Max(1, captured.Level) + "\n" +
                    "GROWTH — " + captured.PotentialBand + "\n" +
                    "WEAPON — " + FriendlyGrowthPath067(captured.FixedWeaponFamilyId),
                    () =>
                    {
                        _selectedRecruitId = captured.RecruitId;
                        BuildCurrentScreen();
                    },
                    350f,
                    selected ? RuntimeUi.Accent : Color.white);
                M1PremiumUi.StyleDossierCard(button, selected);
                AddPortraitToButton(
                    button,
                    captured.RecruitId,
                    captured.VisualSeed,
                    captured.RaceId,
                    captured.PortraitAuthorityId,
                    captured.DisplayName,
                    M1VisualAssets.HumanizeRace(captured.RaceId),
                    large: true,
                    roleIdentity: captured.StartingClassId);
                M1PremiumUi.AddClassCrest(button, captured.ClassSymbol,
                    FriendlyGrowthPath067(captured.StartingClassId));
            }

            if (members.Length > 3)
            {
                var pages = AddRow(body, "Guild Member Path Page Controls 067", 12f, 86f);
                var previous = RuntimeUi.AddButton(pages, "Previous Guild Members 067", "← PREVIOUS MEMBERS", () =>
                {
                    _guildMemberPage067 = Mathf.Max(0, _guildMemberPage067 - 1);
                    _selectedRecruitId = members[_guildMemberPage067 * 3].RecruitId;
                    BuildCurrentScreen();
                }, 82f);
                previous.interactable = _guildMemberPage067 > 0;
                var next = RuntimeUi.AddButton(pages, "Next Guild Members 067", "MORE MEMBERS →", () =>
                {
                    _guildMemberPage067 = Mathf.Min(
                        Mathf.Max(0, (members.Length - 1) / 3),
                        _guildMemberPage067 + 1);
                    _selectedRecruitId = members[_guildMemberPage067 * 3].RecruitId;
                    BuildCurrentScreen();
                }, 82f);
                next.interactable = (_guildMemberPage067 + 1) * 3 < members.Length;
            }

            var chosen = members.First(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId));
            RuntimeUi.AddText(
                body,
                "Selected Guild Member Development Heading 067",
                chosen.DisplayName.ToUpperInvariant() + "  " + chosen.ClassSymbol +
                "     LEVEL " + Math.Max(1, chosen.Level),
                44,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);

            RuntimeUi.AddText(
                body,
                "Selected Guild Member Progress Summary 070",
                "LEVEL " + Math.Max(1, chosen.Level) + "  •  " +
                (chosen.XpRequiredForNextLevel <= 0
                    ? "MAX LEVEL"
                    : chosen.XpIntoCurrentLevel + "/" + chosen.XpRequiredForNextLevel + " XP TO NEXT LEVEL") +
                "  •  " + chosen.LearnedArtCount + " ARTS LEARNED  •  " + chosen.PotentialBand,
                28,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);

            var treeRow070 = AddRow(body, "Four Member Art Trees 070", 14f, 330f);
            var visibleTrees070 = (chosen.TreeSlots070 ??
                                   Array.Empty<GuildCity017D.GuildMemberTreeSlotView070>())
                .Where(tree => tree != null)
                .Take(4)
                .ToArray();
            foreach (var tree in visibleTrees070)
            {
                var slotLabel070 = string.IsNullOrWhiteSpace(tree.SlotLabel)
                    ? tree.Active ? "ACTIVE PATH" : "LOCKED PATH"
                    : tree.SlotLabel;
                var treeName070 = string.IsNullOrWhiteSpace(tree.TreeDisplayName)
                    ? "Path revealed through training"
                    : tree.TreeDisplayName;
                var progress070 = string.IsNullOrWhiteSpace(tree.ProgressSummary)
                    ? tree.Active ? "Ready to earn progress in battle." : "Earn through Guild training."
                    : tree.ProgressSummary;
                var nextUnlock070 = string.IsNullOrWhiteSpace(tree.NextUnlockName)
                    ? "Progress this path to reveal its next Art"
                    : tree.NextUnlockName;
                var treeCard070 = AddColumnPanel(
                    treeRow070,
                    "Member Tree " + slotLabel070 + " 070",
                    1f,
                    320f);
                RuntimeUi.AddText(
                    treeCard070,
                    "Member Tree Slot " + slotLabel070 + " 070",
                    slotLabel070,
                    23,
                    TextAnchor.MiddleLeft,
                    tree.Active ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold);
                RuntimeUi.AddText(
                    treeCard070,
                    "Member Tree Name " + treeName070 + " 070",
                    treeName070.ToUpperInvariant(),
                    30,
                    TextAnchor.MiddleLeft,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                RuntimeUi.AddText(
                    treeCard070,
                    "Member Tree Progress " + treeName070 + " 070",
                    progress070 + "\nNEXT • " + nextUnlock070,
                    23,
                    TextAnchor.UpperLeft,
                    RuntimeUi.Text);
                if (tree.Earnable && !tree.Active && !string.IsNullOrWhiteSpace(tree.TreeId))
                {
                    var capturedTree070 = tree;
                    var unlockTree070 = RuntimeUi.AddButton(
                        treeCard070,
                        "Unlock Member Tree " + capturedTree070.TreeId + " 070",
                        "UNLOCK " + treeName070.ToUpperInvariant() + "\n" +
                        Math.Max(0, capturedTree070.UnlockCostTreasuryXp) + " XP",
                        () => ApplyGuildCity017D(
                            GuildCity017D.GuildMemberDevelopmentBridge067.UnlockTree070(
                                coordinator,
                                chosen.RecruitId,
                                capturedTree070.TreeId)),
                        82f,
                        RuntimeUi.Accent);
                    unlockTree070.interactable = capturedTree070.CanUnlock;
                    if (!capturedTree070.CanUnlock &&
                        !string.IsNullOrWhiteSpace(capturedTree070.UnlockUnavailableReason))
                        RuntimeUi.AddText(
                            treeCard070,
                            "Member Tree Unlock Status " + capturedTree070.TreeId + " 070",
                            capturedTree070.UnlockUnavailableReason,
                            18,
                            TextAnchor.UpperLeft,
                            RuntimeUi.Warning);
                }
            }
            if (visibleTrees070.Length == 0)
                AddMessagePanel(treeRow070, "ART PATHS ARE BEING PREPARED",
                    "Train this member or complete an expedition to reveal their personal paths.",
                    RuntimeUi.Warning);

            DrawPaidHeroLevels152(body,coordinator as M1RuntimeCoordinator,chosen.RecruitId);
            var actions = AddRow(body, "Member Development Actions 067", 14f, 130f);
            var classTraining = RuntimeUi.AddButton(
                actions,
                "Class Training " + chosen.RecruitId + " 067",
                "CLASS TRAINING\n" + chosen.TrainingCostTreasuryXp + " XP",
                () => ApplyGuildCity017D(
                    GuildCity017D.GuildMemberDevelopmentBridge067.TrainMember067(
                        coordinator,
                        chosen.RecruitId,
                        global::SecondDimension.Gameplay.GuildCity017D.GuildCityRecruitmentService017D.ClassTrainingFocus067)),
                120f,
                RuntimeUi.Accent);
            classTraining.interactable = chosen.CanClassTrain;
            var artPractice = RuntimeUi.AddButton(
                actions,
                "Art Practice " + chosen.RecruitId + " 067",
                "ART PRACTICE\n" + chosen.TrainingCostTreasuryXp + " XP",
                () => ApplyGuildCity017D(
                    GuildCity017D.GuildMemberDevelopmentBridge067.TrainMember067(
                        coordinator,
                        chosen.RecruitId,
                        global::SecondDimension.Gameplay.GuildCity017D.GuildCityRecruitmentService017D.ArtPracticeFocus067)),
                120f,
                RuntimeUi.Positive);
            artPractice.interactable = chosen.CanPracticeArt;
            RuntimeUi.AddButton(actions, "Development Equipment " + chosen.RecruitId + " 067", "EQUIPMENT", () =>
            {
                OpenInventoryForRecruit068(chosen.RecruitId);
            }, 120f);
            RuntimeUi.AddButton(actions, "Development Union " + chosen.RecruitId + " 067", "UNION PLANS", () =>
            {
                _selectedRecruitId = chosen.RecruitId;
                Navigate(M1Screen.UnionBuilder);
            }, 120f);
            if (!chosen.CanClassTrain || !chosen.CanPracticeArt)
                AddMessagePanel(body, "TRAINING STATUS", chosen.TrainingUnavailableReason,
                    chosen.CanClassTrain ? RuntimeUi.Text : RuntimeUi.Warning);
        }

        private static string FriendlyGrowthPath067(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return "NONE YET";
            var value = stableId;
            foreach (var prefix in new[]
                     {
                         "WEAPON_FAMILY_", "TREE_CA002_WPN_", "TREE_CA002_ROLE_",
                         "TREE_CA002_MYS_", "TREE_CA002_MYSTIC_", "CLASS_TEND_", "CLASS_", "ART009_", "SKILL_"
                     })
                if (value.StartsWith(prefix, StringComparison.Ordinal))
                {
                    value = value.Substring(prefix.Length);
                    break;
                }
            return value.Replace('_', ' ');
        }

        private void BuildGuildCityApplicants017D(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddMessagePanel(body, "MEET NEW ADVENTURERS",
                "People from Skyhome and beyond have asked to join your Guild. Interview anyone who interests you. Recruited members stay permanently, arrive with usable gear, and can join your Unions in every quest and battle.",
                RuntimeUi.Accent);
            RuntimeUi.AddText(
                body,
                "Applicant Board Player Resources 066",
                "YOUR GUILD  •  " + state.TotalRecruitCount + "/" + state.RosterCapacity + " MEMBERS" +
                "     XP TO SPEND  •  " + state.TreasuryXp,
                32,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            if (!state.HasRecruitmentBoard)
            {
                var pendingLeadNames = state.PendingExpeditionRecruitLeadNames089 ??
                    Array.Empty<string>();
                AddMessagePanel(
                    body,
                    pendingLeadNames.Count > 0
                        ? "AN EXPEDITION CONTACT IS WAITING"
                        : "EARNED CONTACTS APPEAR HERE",
                    pendingLeadNames.Count > 0
                        ? string.Join(", ", pendingLeadNames) +
                          " answered your expedition invitation. Post the notice to add this named hero to the Applicant Board."
                        : "Continue your quests to earn named contacts. Your existing recruits and saved invitations stay with the Guild.",
                    RuntimeUi.Text);
                if (state.CanInviteEarnedContacts124)
                    RuntimeUi.AddButton(body, "Commit Recurring Board 017D",
                    "INVITE EARNED CONTACT & OPEN BOARD",
                    () => ApplyGuildCity017D(coordinator.CommitGuildCityApplicantBoard017D()),
                    RuntimeUi.PrimaryTouchPixels, RuntimeUi.Accent);
                AddApplicantNextQuestHook066(body, state);
                return;
            }

            var waitingLeadNames089 = state.PendingExpeditionRecruitLeadNames089 ??
                Array.Empty<string>();
            if (state.CanInviteEarnedContacts124)
            {
                AddMessagePanel(
                    body,
                    "NAMED EXPEDITION CONTACT",
                    string.Join(", ", waitingLeadNames089) +
                    " is ready for a formal Guild interview. Add the earned contact to this board without closing the current interviews.",
                    RuntimeUi.Positive);
                RuntimeUi.AddButton(
                    body,
                    "Invite Expedition Recruit Lead 089",
                    "ADD NAMED CONTACT TO THIS BOARD",
                    () => ApplyGuildCity017D(
                        coordinator.CommitGuildCityApplicantBoard017D()),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Positive);
            }

            var applicants = state.Applicants
                .OrderBy(value => value.IsSigned)
                .ThenBy(value => value.Slot)
                .ToArray();
            if (applicants.Length == 0)
            {
                AddMessagePanel(body, "EVERY INTERVIEW IS COMPLETE",
                    "Continue your story quest to earn more named contacts. Current invitations remain saved.",
                    RuntimeUi.Positive);
                AddApplicantNextQuestHook066(body, state);
                return;
            }

            if (applicants.All(value => !StringComparer.Ordinal.Equals(
                    value.RecruitId, _guildApplicantSelectedId066)))
                _guildApplicantSelectedId066 = applicants[0].RecruitId;
            _guildApplicantPage066 = Mathf.Clamp(
                _guildApplicantPage066,
                0,
                Mathf.Max(0, (applicants.Length - 1) / 3));

            var cardRow = AddRow(body, "Applicant Interview Cards 066", 16f, 410f);
            foreach (var applicant in applicants.Skip(_guildApplicantPage066 * 3).Take(3))
            {
                var captured = applicant;
                var capturedPath = GuildCity017D.GuildMemberDevelopmentBridge067.Applicant067(
                    coordinator, captured.RecruitId);
                var selected = StringComparer.Ordinal.Equals(
                    captured.RecruitId, _guildApplicantSelectedId066);
                var button = RuntimeUi.AddButton(
                    cardRow,
                    "Applicant Interview Card " + captured.RecruitId + " 066",
                    captured.DisplayName.ToUpperInvariant() + "\n" +
                    M1VisualAssets.HumanizeRace(captured.RaceId) + " FROM " +
                    FriendlyApplicantLabel066(captured.WorldId).ToUpperInvariant() + "\n" +
                    captured.ClassSymbol + "  " + FriendlyApplicantLabel066(captured.ClassTendencyId).ToUpperInvariant() + "\n" +
                    "GROWTH — " + (capturedPath?.PotentialBand ?? "UNDER REVIEW") + "\n" +
                    (captured.IsSigned ? "NOW IN YOUR GUILD" : "SELECT TO INTERVIEW"),
                    () =>
                    {
                        _guildApplicantSelectedId066 = captured.RecruitId;
                        BuildCurrentScreen();
                    },
                    390f,
                    selected ? RuntimeUi.Accent : Color.white);
                M1PremiumUi.StyleDossierCard(button, selected);
                AddPortraitToButton(
                    button,
                    captured.RecruitId,
                    captured.VisualSeed,
                    captured.RaceId,
                    captured.PortraitAuthorityId,
                    captured.DisplayName,
                    M1VisualAssets.HumanizeRace(captured.RaceId),
                    large: true,
                    roleIdentity: captured.ClassTendencyId);
                M1PremiumUi.AddClassCrest(
                    button,
                    captured.ClassSymbol,
                    FriendlyApplicantLabel066(captured.ClassTendencyId));
            }

            if (applicants.Length > 3)
            {
                var pages = AddRow(body, "Applicant Interview Page Controls 066", 12f, 86f);
                var previous = RuntimeUi.AddButton(pages, "Previous Applicant Interviews 066", "← PREVIOUS PEOPLE", () =>
                {
                    _guildApplicantPage066 = Mathf.Max(0, _guildApplicantPage066 - 1);
                    BuildCurrentScreen();
                }, 82f);
                previous.interactable = _guildApplicantPage066 > 0;
                var next = RuntimeUi.AddButton(pages, "Next Applicant Interviews 066", "MORE PEOPLE →", () =>
                {
                    _guildApplicantPage066 = Mathf.Min(
                        Mathf.Max(0, (applicants.Length - 1) / 3),
                        _guildApplicantPage066 + 1);
                    BuildCurrentScreen();
                }, 82f);
                next.interactable = (_guildApplicantPage066 + 1) * 3 < applicants.Length;
            }

            var chosen = applicants.FirstOrDefault(value => StringComparer.Ordinal.Equals(
                value.RecruitId, _guildApplicantSelectedId066));
            if (chosen != null)
                AddPlayableApplicantInterview066(body, coordinator, state, chosen, applicants.Length);

            AddApplicantNextQuestHook066(body, state);
            if (state.CanInviteEarnedContacts124)
                RuntimeUi.AddButton(body, "Invite New Applicant Group 066",
                "POST NOTICE FOR EARNED CONTACTS",
                () => ShowConfirmation(
                    "POST AN EARNED CONTACT NOTICE?",
                    "Your current interviews stay on this board. The existing notice fee is charged only if an earned contact fits. Excess contacts wait until an interview is complete.",
                    "POST NOTICE",
                    () => ApplyGuildCity017D(coordinator.RefreshGuildCityApplicantBoard017D())),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.MutedText);
        }

        private void AddPlayableApplicantInterview066(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityApplicantView017D applicant,
            int applicantCount)
        {
            var path = GuildCity017D.GuildMemberDevelopmentBridge067.Applicant067(
                coordinator, applicant.RecruitId);
            var interview = AddRow(body, "Playable Applicant Interview " + applicant.RecruitId + " 066", 18f, 690f);
            var portrait = RuntimeUi.AddPanel(
                interview,
                "Applicant Interview Portrait " + applicant.RecruitId + " 066",
                RuntimeUi.Accent);
            RuntimeUi.SetLayout(portrait, preferredWidth: 480f, preferredHeight: 680f, flexibleWidth: 0f);
            PopulatePortraitFrame(
                portrait,
                applicant.RecruitId,
                applicant.VisualSeed,
                applicant.RaceId,
                applicant.PortraitAuthorityId,
                applicant.DisplayName,
                M1VisualAssets.HumanizeRace(applicant.RaceId),
                applicant.DisplayName + "\n" + FriendlyApplicantLabel066(applicant.ClassTendencyId),
                roleIdentity: applicant.ClassTendencyId);

            var conversation = AddColumnPanel(interview, "Applicant Conversation 066", 1.35f, 680f);
            RuntimeUi.AddText(
                conversation,
                "Applicant Interview Name 066",
                applicant.DisplayName.ToUpperInvariant() + "  " + applicant.ClassSymbol,
                46,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.AddText(
                conversation,
                "Applicant Interview Plain Details 066",
                "ROLE — " + FriendlyApplicantLabel066(applicant.ClassTendencyId) +
                "\nGROWTH OUTLOOK — " + (path?.PotentialBand ?? "UNDER REVIEW") +
                (path != null && path.PotentialScore > 0 ? "  •  " + path.PotentialScore : string.Empty) +
                "\nWEAPON FAMILY — " + FriendlyGrowthPath067(path?.FixedWeaponFamilyId) +
                "\nWEAPON PATH — " + FriendlyGrowthPath067(path?.WeaponTreeId) +
                "\nPRIMARY ROLE PATH — " + FriendlyGrowthPath067(path?.PrimaryRoleTreeId) +
                "\nSECONDARY ROLE PATH — " + FriendlyGrowthPath067(path?.SecondaryRoleTreeId) +
                "\nSTARTING ADVANTAGE — " + (path?.StartingAdvantage ?? "A focused class foundation") +
                "\nPERSONALITY — " + applicant.TraitSummary +
                "\nSTARTING GEAR — " + applicant.EquipmentSummary +
                "\nHOW THEY HANDLE PRESSURE — " + applicant.ObservedSummary +
                "\nWHY THEY CAME — " + applicant.PersonalHook,
                30,
                TextAnchor.UpperLeft,
                RuntimeUi.Text);

            var actions = AddColumnPanel(interview, "Applicant Interview Choices 066", 0.76f, 680f);
            AddMessagePanel(
                actions,
                applicant.IsSigned ? "RECRUITED PERMANENTLY" : "YOUR DECISION",
                applicant.IsSigned
                    ? "Their starter gear is equipped, and they begin safely in Reserve. Review the loadout or place them in a Union when you are ready."
                    : applicant.SigningCostTreasuryXp == 0
                        ? "Your founding charter covers this first invitation, or you may politely decline the interview."
                        : "Recruit for " + applicant.SigningCostTreasuryXp + " XP, or politely decline this interview.",
                applicant.IsSigned ? RuntimeUi.Positive : RuntimeUi.Warning);
            if (applicant.IsSigned)
            {
                RuntimeUi.AddButton(actions, "Equip Recruited Member 066", "REVIEW EQUIPPED GEAR", () =>
                {
                    OpenInventoryForRecruit068(applicant.RecruitId);
                }, 112f, RuntimeUi.Accent);
                RuntimeUi.AddButton(actions, "Place Recruited Member 066", "PLACE IN A UNION", () =>
                {
                    _selectedRecruitId = applicant.RecruitId;
                    _screen = M1Screen.UnionBuilder;
                    _localStatus = "Both active Unions begin full. Add a Union plan, or tap an occupied slot to make room, then add " + applicant.DisplayName + ".";
                    _localStatusPositive = true;
                    BuildCurrentScreen();
                }, 112f);
                RuntimeUi.AddButton(actions, "Develop Recruited Member 067", "OPEN MEMBER DEVELOPMENT", () =>
                {
                    _selectedRecruitId = applicant.RecruitId;
                    _guildCityTab017D = "DEVELOPMENT";
                    BuildCurrentScreen();
                }, 112f, RuntimeUi.Positive);
            }
            else
            {
                var recruit = RuntimeUi.AddButton(
                    actions,
                    "Recruit Permanent Applicant " + applicant.RecruitId + " 066",
                    applicant.CanAfford
                        ? "RECRUIT PERMANENTLY"
                        : "NEED " + applicant.SigningCostTreasuryXp + " XP",
                    () => ApplyGuildCity017D(coordinator.SignGuildCityApplicant017D(applicant.RecruitId)),
                    118f,
                    RuntimeUi.Accent);
                recruit.interactable = applicant.CanAfford;
                var decline = RuntimeUi.AddButton(
                    actions,
                    "Decline Applicant " + applicant.RecruitId + " 066",
                    "DECLINE FOR THIS BOARD",
                    () => ShowConfirmation(
                        "DECLINE THIS INTERVIEW?",
                        applicant.DisplayName + " will leave this applicant board. You can still meet the other people here.",
                        "DECLINE INTERVIEW",
                        () => ApplyGuildCity017D(coordinator.DeclineGuildCityApplicant017D(applicant.RecruitId))),
                    104f,
                    RuntimeUi.ButtonNormal);
                decline.interactable = applicantCount > 1;
            }
        }

        private void AddApplicantNextQuestHook066(
            Transform body,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var needsFirstRecruit = NeedsFirstAdditionalRecruit066(state, firstComplete);
            var next = RuntimeUi.AddPanel(body, "Applicant Next Story Hook 066", Color.white);
            RuntimeUi.SetLayout(next, preferredHeight: 176f);
            M1PremiumUi.StylePanel(next, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(next.transform, new RectOffset(20, 20, 10, 10), 5f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(
                next.transform,
                "Applicant Next Story Heading 066",
                needsFirstRecruit
                    ? "NEXT: INVITE ONE ADVENTURER"
                    : state.HasActiveContract ? "NEXT: RETURN TO LANTERN ROAD" : "NEXT: ANSWER THE BELL",
                34,
                TextAnchor.MiddleCenter,
                RuntimeUi.Positive,
                FontStyle.Bold);
            RuntimeUi.AddText(
                next.transform,
                "Applicant Next Story Explanation 066",
                needsFirstRecruit
                    ? "Choose someone above and recruit them permanently. Then Kiri can issue the Lantern Road order."
                    : state.HasActiveContract
                    ? "Your party is waiting on Lantern Road. New members can be equipped before you return."
                    : "A rope-less bell is ringing beneath Skyhome. Meet your party, then follow Lantern Road to the missing patrol and Wayglass.",
                26,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text);
            var continueButton = RuntimeUi.AddButton(
                next.transform,
                "Applicant Continue Story 066",
                needsFirstRecruit
                    ? "RECRUIT SOMEONE TO CONTINUE"
                    : state.HasActiveContract ? "RETURN TO LANTERN ROAD" : "VIEW THE LANTERN ROAD ORDER",
                ContinueAdventure060,
                92f,
                RuntimeUi.Accent);
            continueButton.interactable = !needsFirstRecruit;
        }

        private static string FriendlyApplicantLabel066(string stableId)
        {
            if (string.IsNullOrWhiteSpace(stableId)) return "Unknown";
            if (StringComparer.Ordinal.Equals(stableId, "WORLD_GATE_01")) return "Skyhome Gate";
            var value = stableId;
            foreach (var prefix in new[] { "CLASS_", "WORLD_", "RACE_", "LEAD_" })
            {
                if (!value.StartsWith(prefix, StringComparison.Ordinal)) continue;
                value = value.Substring(prefix.Length);
                break;
            }
            return string.Join(" ", value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => word.Length == 0
                    ? string.Empty
                    : char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
        }

        private void BuildGuildCityBuildMode017D(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddMessagePanel(body, "SNAP-PLOT CITY BUILDING",
                "Twelve visible plots surround the Ruined Annex; six begin usable. Buildings change the settlement, accept staff, unlock real systems, and persist. No real-time timers or empty Wait Day.",
                RuntimeUi.Accent);
            RuntimeUi.AddText(body, "Materials 017D",
                "MATERIALS  •  " + string.Join("  •  ", state.MaterialSummaries),
                34, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold);
            AddCityOverviewVisual017E(body, state);
            AddCityNarrative017F(body, state);

            foreach (var plot in state.Plots)
            {
                var panel = AddColumnPanel(body, "City Plot " + plot.PlotId, 1f, 245f);
                RuntimeUi.AddText(panel, "Plot Heading " + plot.PlotId,
                    plot.PlotId + "  •  " + CleanHallStageName(plot.DistrictId) + "  •  " + plot.Size,
                    36, TextAnchor.MiddleLeft, plot.Unlocked ? RuntimeUi.Accent : RuntimeUi.MutedText,
                    FontStyle.Bold);
                if (!plot.Unlocked)
                {
                    RuntimeUi.AddText(panel, "Plot Locked " + plot.PlotId,
                        "LOCKED — CITY PROJECT PROGRESS " + plot.ConstructionProgress + "/" +
                        global::SecondDimension.Gameplay.GuildCity017D.GuildCityCommandService017D.OpeningPlotUnlockProgress,
                        30, TextAnchor.MiddleLeft,
                        plot.ConstructionProgress > 0 ? RuntimeUi.Warning : RuntimeUi.MutedText, FontStyle.Bold);
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(plot.BuildingId))
                {
                    RuntimeUi.AddText(panel, "Building " + plot.PlotId,
                        plot.BuildingName.ToUpperInvariant() + "  •  LEVEL " + plot.BuildingLevel +
                        "  •  STAFF " + plot.StaffRecruitIds.Count,
                        34, TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold);
                    var actions = AddRow(panel, "Building Actions " + plot.PlotId, 12f, 118f);
                    RuntimeUi.AddButton(actions, "Upgrade " + plot.PlotId, "UPGRADE",
                        () => ApplyGuildCity017D(coordinator.UpgradeGuildCityBuilding017D(plot.PlotId)), 105f);
                    var firstRecruit = state.Assignments.FirstOrDefault(value =>
                        !StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Archived"));
                    if (firstRecruit != null)
                    {
                        RuntimeUi.AddButton(actions, "Staff " + plot.PlotId, "STAFF " + firstRecruit.RecruitName.ToUpperInvariant(),
                            () => ApplyGuildCity017D(coordinator.AssignGuildCityStaff017D(plot.PlotId, firstRecruit.RecruitId)), 105f);
                    }
                    continue;
                }

                var legal = state.Buildings.Where(value =>
                    StringComparer.Ordinal.Equals(value.DistrictId, plot.DistrictId)).Take(4).ToArray();
                RuntimeUi.AddText(panel, "Empty Plot " + plot.PlotId,
                    "OPEN PLOT — CHOOSE A FACILITY", 30, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
                var choices = AddRow(panel, "Building Choices " + plot.PlotId, 10f, 120f);
                foreach (var building in legal)
                {
                    var selectedBuilding = building;
                    RuntimeUi.AddButton(choices, "Place " + selectedBuilding.BuildingId + " on " + plot.PlotId,
                        selectedBuilding.DisplayName.ToUpperInvariant(),
                        () => ApplyGuildCity017D(coordinator.PlaceGuildCityBuilding017D(plot.PlotId,
                            selectedBuilding.BuildingId)), 105f,
                        state.CharterBuildCredits > 0 ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                }
            }
        }

        private void BuildGuildCityContracts017D(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddResponsiveText062(
                body,
                "Contract Board Invitation 062",
                IsStoryContractCompleted065(state, FirstStoryContractId065)
                    ? "NEXT GUILD CHRONICLE CHAPTER"
                    : "CHAPTER 1  •  FEATURED FIRST CONTRACT",
                34,
                52,
                64f,
                RuntimeUi.Text,
                FontStyle.Bold);
            AddResponsiveText062(
                body,
                "Contract Board Promise 062",
                IsStoryContractCompleted065(state, FirstStoryContractId065)
                    ? "The recovered Wayglass now points beneath Skyhome. Open it to reveal The Door Inside."
                    : "A missing patrol, the Lantern Road, and a Wayglass lead your Guild toward the old Gatehouse.",
                24,
                34,
                54f,
                RuntimeUi.MutedText);

            var contracts = (state.Contracts ?? Array.Empty<GuildCity017D.GuildCityContractView017D>())
                .OrderBy(contract => StoryContractOrder065(state, contract))
                .ThenBy(contract => contract.ContractId, StringComparer.Ordinal)
                .ToArray();
            for (var contractIndex = 0; contractIndex < contracts.Length; contractIndex++)
            {
                var contract = contracts[contractIndex];
                if (contractIndex == 0)
                {
                    var featured = RuntimeUi.AddPanel(body, "Featured First Contract 062", Color.white);
                    RuntimeUi.SetLayout(featured, preferredHeight: 578f);
                    M1PremiumUi.StylePanel(featured, M1PremiumUi.Surface.WorldPaper);
                    RuntimeUi.AddVerticalLayout(
                        featured.transform,
                        new RectOffset(30, 30, 17, 17),
                        0f,
                        TextAnchor.MiddleCenter);

                    // The contract art now reads as a backdrop instead of competing
                    // with story copy for horizontal space on a 1280x800 screen.
                    var keyArt = RuntimeUi.AddPanel(
                        featured.transform,
                        "First Contract Key Art Backdrop 074",
                        Color.white);
                    Stretch(keyArt.rectTransform);
                    keyArt.sprite = ResolveVisualSliceSprite062(FirstContractKeyArtResource062);
                    keyArt.type = Image.Type.Simple;
                    keyArt.preserveAspect = false;
                    keyArt.raycastTarget = false;
                    var keyArtLayout = keyArt.gameObject.AddComponent<LayoutElement>();
                    keyArtLayout.ignoreLayout = true;

                    var keyArtShade = RuntimeUi.AddPanel(
                        featured.transform,
                        "First Contract Key Art Readability Shade 074",
                        new Color(0.015f, 0.020f, 0.024f, 0.76f));
                    Stretch(keyArtShade.rectTransform);
                    keyArtShade.raycastTarget = false;
                    var shadeLayout = keyArtShade.gameObject.AddComponent<LayoutElement>();
                    shadeLayout.ignoreLayout = true;

                    var details = new GameObject(
                        "Featured Contract Details 062",
                        typeof(RectTransform),
                        typeof(LayoutElement)).GetComponent<RectTransform>();
                    details.SetParent(featured.transform, false);
                    RuntimeUi.SetLayout(
                        details,
                        preferredHeight: ContractBoardFeaturedDetailsHeight074,
                        flexibleWidth: 1f);
                    RuntimeUi.AddVerticalLayout(
                        details,
                        new RectOffset(10, 10, 8, 8),
                        5f,
                        TextAnchor.MiddleLeft);
                    var contractName = AddResponsiveText062(
                        details,
                        "Featured Contract Name 062",
                        contract.DisplayName.ToUpperInvariant(),
                        38,
                        58,
                        ContractBoardNameHeight074,
                        contract.IsActive ? RuntimeUi.Positive : RuntimeUi.Accent,
                        FontStyle.Bold);
                    contractName.verticalOverflow = VerticalWrapMode.Truncate;

                    var sponsor = AddResponsiveText062(
                        details,
                        "Featured Contract Sponsor 062",
                        contract.Sponsor.ToUpperInvariant() + "  •  " + CleanHallStageName(contract.Family),
                        22,
                        30,
                        ContractBoardSponsorHeight074,
                        RuntimeUi.MutedText,
                        FontStyle.Bold);
                    sponsor.verticalOverflow = VerticalWrapMode.Truncate;

                    var hook = AddResponsiveText062(
                        details,
                        "Featured Contract Hook 062",
                        contract.Hook,
                        22,
                        32,
                        ContractBoardHookHeight074,
                        RuntimeUi.Text);
                    hook.verticalOverflow = VerticalWrapMode.Truncate;

                    var objective = RuntimeUi.AddPanel(
                        details,
                        "Featured Contract Objective 074",
                        new Color(0.055f, 0.105f, 0.115f, 0.92f));
                    RuntimeUi.SetLayout(objective, preferredHeight: ContractBoardObjectiveHeight074);
                    M1PremiumUi.StylePanel(objective, M1PremiumUi.Surface.WorldGlass);
                    var objectiveText = RuntimeUi.AddText(
                        objective.transform,
                        "Featured Contract Objective Text 074",
                        "OBJECTIVE  •  " + contract.PrimaryObjective,
                        30,
                        TextAnchor.MiddleLeft,
                        RuntimeUi.Positive,
                        FontStyle.Bold);
                    Stretch(objectiveText.rectTransform);
                    objectiveText.rectTransform.offsetMin = new Vector2(18f, 6f);
                    objectiveText.rectTransform.offsetMax = new Vector2(-18f, -6f);
                    ConfigureResponsiveText062(objectiveText, 18, 30);
                    objectiveText.verticalOverflow = VerticalWrapMode.Truncate;

                    var stakes = RuntimeUi.AddPanel(
                        details,
                        "Featured Contract Risk And Reward 076",
                        new Color(0.040f, 0.030f, 0.015f, 0.94f));
                    RuntimeUi.SetLayout(stakes, preferredHeight: ContractBoardStakesHeight074);
                    M1PremiumUi.StylePanel(stakes, M1PremiumUi.Surface.WorldRibbon);
                    var risk = RuntimeUi.AddText(
                        stakes.transform,
                        "Featured Contract Risk 076",
                        StoryContractRiskForecast076(contract.ContractId),
                        21,
                        TextAnchor.MiddleLeft,
                        RuntimeUi.Warning,
                        FontStyle.Bold);
                    risk.rectTransform.anchorMin = new Vector2(0.018f, 0.50f);
                    risk.rectTransform.anchorMax = new Vector2(0.982f, 0.98f);
                    risk.rectTransform.offsetMin = Vector2.zero;
                    risk.rectTransform.offsetMax = Vector2.zero;
                    ConfigureResponsiveText062(risk, 16, 21);
                    risk.verticalOverflow = VerticalWrapMode.Truncate;
                    var reward = RuntimeUi.AddText(
                        stakes.transform,
                        "Featured Contract Reward 062",
                        "REWARD  •  " + contract.GuildXp + " XP TO SPEND  •  " + contract.HallXp + " HALL XP",
                        21,
                        TextAnchor.MiddleLeft,
                        RuntimeUi.Positive,
                        FontStyle.Bold);
                    reward.rectTransform.anchorMin = new Vector2(0.018f, 0.02f);
                    reward.rectTransform.anchorMax = new Vector2(0.982f, 0.50f);
                    reward.rectTransform.offsetMin = Vector2.zero;
                    reward.rectTransform.offsetMax = Vector2.zero;
                    ConfigureResponsiveText062(reward, 16, 21);
                    reward.verticalOverflow = VerticalWrapMode.Truncate;

                    if (contract.IsCompleted)
                    {
                        var complete = AddResponsiveText062(
                            details,
                            "Featured Contract Complete 065",
                            "CHAPTER COMPLETE  •  REWARDS AND CONSEQUENCES SAVED",
                            20,
                            28,
                            50f,
                            RuntimeUi.Positive,
                            FontStyle.Bold,
                            TextAnchor.MiddleCenter);
                        complete.verticalOverflow = VerticalWrapMode.Truncate;
                    }
                    else if (!contract.IsActive || contract.IsFailed)
                    {
                        var guidedStage080 = CurrentGuidedHallStage080(state);
                        if (GuidedStageBlocksContract080(
                                guidedStage080,
                                contract.ContractId))
                        {
                            var gate = AddResponsiveText062(
                                details,
                                "Guided Contract Gate 080",
                                GuidedHallObjectiveForVerification080(guidedStage080),
                                19,
                                28,
                                ContractBoardRecruitGateHeight074,
                                RuntimeUi.Warning,
                                FontStyle.Bold,
                                TextAnchor.MiddleCenter);
                            gate.verticalOverflow = VerticalWrapMode.Truncate;
                            var completeGuidedStep080 = RuntimeUi.AddButton(
                                details,
                                "Complete Guided Hall Step 080",
                                GuidedHallActionForVerification080(guidedStage080),
                                () => TryOpenCurrentGuidedHallStep080(state),
                                ContractBoardActionHeight074,
                                RuntimeUi.Accent);
                            ConfigureResponsiveText062(
                                completeGuidedStep080.GetComponentInChildren<Text>(),
                                18,
                                28);
                        }
                        else
                        {
                            var selectedContract = contract;
                            var accept = RuntimeUi.AddButton(
                                details,
                                "Featured Contract Accept 062",
                                StoryAcceptLabel065(contract.ContractId),
                                () => AcceptFeaturedContract062(coordinator, selectedContract.ContractId),
                                ContractBoardActionHeight074,
                                RuntimeUi.Accent);
                            ConfigureResponsiveText062(accept.GetComponentInChildren<Text>(), 18, 28);
                        }
                    }
                    else
                    {
                        var review = RuntimeUi.AddButton(
                            details,
                            "Featured Contract Open Expedition 062",
                            "REVIEW PARTY",
                            () =>
                            {
                                _guildCityTab017D = "PARTY";
                                BuildCurrentScreen();
                            },
                            ContractBoardActionHeight074,
                            RuntimeUi.Positive);
                        ConfigureResponsiveText062(review.GetComponentInChildren<Text>(), 18, 28);
                    }
                    continue;
                }

                // Additional contracts are optional during onboarding. MORE reveals the
                // legacy board after the featured mission rather than crowding the fold.
                if (!_guildCityMoreOpen060) continue;

                var panel = AddColumnPanel(body, "Contract " + contract.ContractId, 1f, 430f);
                RuntimeUi.AddText(panel, "Contract Name " + contract.ContractId,
                    contract.DisplayName.ToUpperInvariant() + "  •  " + contract.Sponsor,
                    42, TextAnchor.MiddleLeft,
                    contract.IsActive ? RuntimeUi.Positive : RuntimeUi.Accent, FontStyle.Bold);
                AddContractVisual017E(panel, contract);
                AddContractNarrative017F(panel, contract);
                RuntimeUi.AddText(panel, "Contract Hook " + contract.ContractId,
                    FormatContractDetail017E(contract),
                    30, TextAnchor.UpperLeft, RuntimeUi.Text);
                if (contract.IsCompleted)
                    RuntimeUi.AddText(panel, "Contract Complete " + contract.ContractId,
                        "CHAPTER COMPLETE — REWARDS, MEMBER GROWTH, AND HALL CONSEQUENCES SAVED",
                        28, TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold);
                else if (!contract.IsActive || contract.IsFailed)
                {
                    var selectedContract = contract;
                    RuntimeUi.AddButton(panel, "Accept " + contract.ContractId, "ACCEPT CONTRACT",
                        () => AcceptFeaturedContract062(
                            coordinator,
                            selectedContract.ContractId),
                        118f, RuntimeUi.Accent);
                }
                else
                    RuntimeUi.AddButton(panel, "Open Active Expedition " + contract.ContractId, "PREPARE EXPEDITION",
                        () => OpenTownService153("EXPEDITION"), 118f, RuntimeUi.Positive);
            }
        }

        private void AcceptFeaturedContract062(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string contractId)
        {
            var guidedStage080 = CurrentGuidedHallStage080(coordinator?.GuildCity017D);
            if (GuidedStageBlocksContract080(guidedStage080, contractId))
            {
                _localStatus = GuidedHallObjectiveForVerification080(guidedStage080);
                _localStatusPositive = false;
                TryOpenCurrentGuidedHallStep080(coordinator?.GuildCity017D);
                return;
            }
            var result = coordinator.AcceptGuildCityContract017D(contractId);
            _localStatusPositive = result != null && result.Succeeded;
            var beginsChapterTwo = StringComparer.Ordinal.Equals(
                contractId,
                SecondStoryContractId065);
            _localStatus = _localStatusPositive
                ? (beginsChapterTwo
                    ? "Chapter 2 opened. The Wayglass is projecting a sealed stair beneath the Hall."
                    : "Lantern Road order accepted. Your party and route are saved.")
                : result?.Message ?? "Something went wrong—try accepting the contract again.";
            if (_localStatusPositive)
                _guildCityTab017D = beginsChapterTwo ? "CHAPTER2" : "PARTY";
            BuildCurrentScreen();
        }

        private static int StoryContractOrder065(
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityContractView017D contract)
        {
            if (contract == null) return int.MaxValue;
            if (contract.IsActive && !contract.IsCompleted && !contract.IsFailed) return -10;
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            if (!firstComplete && StringComparer.Ordinal.Equals(contract.ContractId, FirstStoryContractId065)) return 0;
            if (firstComplete && !secondComplete && StringComparer.Ordinal.Equals(contract.ContractId, SecondStoryContractId065)) return 0;
            if (secondComplete && StringComparer.Ordinal.Equals(contract.ContractId, ThirdStoryContractId065)) return 0;
            if (StringComparer.Ordinal.Equals(contract.ContractId, FirstStoryContractId065)) return 10;
            if (StringComparer.Ordinal.Equals(contract.ContractId, SecondStoryContractId065)) return 20;
            if (StringComparer.Ordinal.Equals(contract.ContractId, ThirdStoryContractId065)) return 30;
            return 100;
        }

        private static string StoryAcceptLabel065(string contractId)
        {
            if (StringComparer.Ordinal.Equals(contractId, FirstStoryContractId065)) return "ACCEPT CONTRACT";
            if (StringComparer.Ordinal.Equals(contractId, SecondStoryContractId065)) return "BEGIN CHAPTER 2";
            if (StringComparer.Ordinal.Equals(contractId, ThirdStoryContractId065)) return "BEGIN CHAPTER 3";
            return "ACCEPT CONTRACT";
        }

        private static string StoryContractRiskForecast076(string contractId)
        {
            if (StringComparer.Ordinal.Equals(contractId, FirstStoryContractId065))
                return "RISK  •  3 UNION BATTLES  •  TILE EVENTS SPEND SUPPLIES";
            if (StringComparer.Ordinal.Equals(contractId, SecondStoryContractId065))
                return "STAKES  •  FOLLOW SELLA AND ORRA'S BRASS LINE  •  EXPOSE THE WAYGLASS FORGERY";
            if (StringComparer.Ordinal.Equals(contractId, ThirdStoryContractId065))
                return "RISK  •  CONVOY LOSSES RISE IF THE RELIEF ROAD STALLS";
            return "RISK  •  REVIEW THE OBJECTIVE BEFORE COMMITTING YOUR UNIONS";
        }

        private void BuildGuildCityExpedition017D(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            if (!state.HasActiveContract && state.Expedition == null)
            {
                AddMessagePanel(body, "CHOOSE A CONTRACT FIRST",
                    "Your Guild Guide has one beginner request waiting on the contract board.", RuntimeUi.Warning);
                RuntimeUi.AddButton(body, "Open Contracts 017D", "OPEN CONTRACT BOARD",
                    () => { _guildCityTab017D = "CONTRACTS"; BuildCurrentScreen(); },
                    RuntimeUi.PrimaryTouchPixels, RuntimeUi.Accent);
                return;
            }

            var usesWalkableGateworks = UsesWalkableGateworks066(state);
            if (state.Expedition == null)
            {
                AddMessagePanel(body, "READY TO LEAVE SKYHOME",
                    usesWalkableGateworks
                        ? "Your party will follow Lantern Road on foot. Read the broken waymarkers, find the missing patrol, and reach the old Gatehouse before its threat reaches Skyhome."
                        : "Your Unions are ready to begin the active contract route.",
                    RuntimeUi.Accent);
                RuntimeUi.AddButton(body, "First Contract Begin Expedition 062", "BEGIN EXPEDITION",
                    () => BeginFirstContractFromParty063(coordinator),
                    RuntimeUi.PrimaryTouchPixels, RuntimeUi.Accent);
                if (usesWalkableGateworks) AddWalkableGateworksPreview066(body);
                else AddFirstExpeditionMap065(body, null);
                return;
            }

            var expedition = state.Expedition;
            var status = RuntimeUi.AddPanel(body, "Expedition Plain Language Status 063", Color.white);
            RuntimeUi.SetLayout(status, preferredHeight: 112f);
            M1PremiumUi.StylePanel(status, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(status.transform, new RectOffset(20, 20, 8, 8), 4f, TextAnchor.MiddleLeft);
            AddResponsiveText062(
                status.transform,
                "Expedition Heading 017D",
                ExpeditionLocationHeading063(expedition),
                22,
                36,
                42f,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AddResponsiveText062(
                status.transform,
                "Expedition State 017D",
                ExpeditionSituation063(expedition),
                18,
                28,
                42f,
                RuntimeUi.Text);

            if (!usesWalkableGateworks)
            {
                BuildInheritedDynamicExpeditionBoard065(body, coordinator, state, expedition);
                return;
            }

            AddWalkableGateworksEntry066(body, coordinator, state);
            if (HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED"))
                AddPostBattleGrowthCheckpoint065(body, state);

            if (state.HasPendingEncounter)
            {
                AddMessagePanel(body, "THE GATEHOUSE THREAT BLOCKS THE ROAD",
                    "Enter Lantern Road, approach the enemy line, and use ACT to begin the Union battle.",
                    RuntimeUi.Warning);
                return;
            }

            if (expedition.CanFinalizeOperation)
            {
                var failed = StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Failed");
                var extracted = StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Extracted");
                AddMessagePanel(
                    body,
                    failed ? "RETREAT THROUGH THE OLD GATE" :
                        extracted ? "RETURN AFTER EXTRACTION" : "RETURN THROUGH THE OLD GATE",
                    failed
                        ? "The operation failed. Re-enter Lantern Road and walk back to the glowing Skyhome exit to bring your party home."
                        : extracted
                            ? "Your party extracted from the operation. Re-enter Lantern Road and walk back to the glowing Skyhome exit."
                            : StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Completed")
                                ? "The patrol and Wayglass are secure. Re-enter Lantern Road and walk back to the glowing Skyhome exit."
                                : "The operation has ended. Re-enter Lantern Road and walk back to the glowing Skyhome exit.",
                    failed ? RuntimeUi.Error : extracted ? RuntimeUi.Warning : RuntimeUi.Positive);
                return;
            }

            if (!expedition.ResolutionComplete &&
                (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK")))
            {
                AddMessagePanel(body, "A HAZARD BLOCKS THE ROAD",
                    "Enter Lantern Road, walk to the highlighted obstruction, and interact to roll 2d6.",
                    RuntimeUi.Warning);
                return;
            }

            if (expedition.HasCommittedCheckAtCurrentNode)
                AddCommittedDiceResult065(body, state, expedition);

            if (expedition.CanCommitEncounter)
            {
                AddMessagePanel(body, "GATEHOUSE THREAT AHEAD",
                    "Enter Lantern Road, approach the enemy line, and use ACT when you are ready to fight.",
                    RuntimeUi.Warning);
                return;
            }

            if (expedition.CanMove && expedition.LinkedNodeIds != null && expedition.LinkedNodeIds.Count > 0)
            {
                if (HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED"))
                {
                    AddMessagePanel(
                        body,
                        "THE QUEST IS NOT OVER",
                        ExpeditionBoardProjection074.HasFirstHourPatrolRescueProof074(
                            state?.Expedition?.ObjectiveFlags)
                            ? "The patrol and Wayglass are secure. Choose the connected road home to Skyhome."
                            : "Lantern Road is clear, but the missing patrol is still inside the old Gatehouse. Continue on foot.",
                        RuntimeUi.Accent);
                }
                AddMessagePanel(body, "CONTINUE ON FOOT",
                    expedition.LinkedNodeIds.Count > 1
                        ? "Lantern Road forks ahead. Walk to the highlighted waymarker, interact, then choose the upper road or service tunnel with 1 or 2."
                        : "Your next Lantern Road objective is highlighted. Walk there and interact to continue.",
                    RuntimeUi.Accent);
                return;
            }

            AddMessagePanel(
                body,
                "THE PARTY IS REGROUPING",
                "Your next route will appear as soon as the current event finishes.",
                RuntimeUi.MutedText);
        }

        private static bool UsesWalkableGateworks066(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            if (state?.Expedition != null)
                return IsFirstStoryBoard069(state.Expedition.BoardId);
            return IsStoryContractActive065(state, FirstStoryContractId065);
        }

        private void BuildInheritedDynamicExpeditionBoard065(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            AddFirstExpeditionMap065(body, expedition);
            AddExpeditionTravelLedger065(body, expedition);
            if (HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED"))
                AddPostBattleGrowthCheckpoint065(body, state);

            if (state.HasPendingEncounter)
            {
                RuntimeUi.AddButton(body, "First Contract Enter Battle 062", "ENTER BATTLE",
                    () => EnterCommittedGuildCityBattle017D(coordinator),
                    RuntimeUi.PrimaryTouchPixels, RuntimeUi.Accent);
                return;
            }

            if (expedition.CanFinalizeOperation)
            {
                RuntimeUi.AddButton(body, "Finalize Operation 017D", "RETURN TO GUILD",
                    () => FinalizeStoryOperation065(coordinator),
                    RuntimeUi.PrimaryTouchPixels, RuntimeUi.Accent);
                return;
            }

            if (!expedition.ResolutionComplete &&
                (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK")))
            {
                AddInteractiveExpeditionEvent065(body, coordinator, state, expedition);
                return;
            }

            if (expedition.HasCommittedCheckAtCurrentNode)
                AddCommittedDiceResult065(body, state, expedition);

            if (expedition.CanCommitEncounter)
            {
                RuntimeUi.AddButton(body, "Commit Encounter 017D", "APPROACH THE ENCOUNTER",
                    () => ApplyGuildCity017D(coordinator.CommitGuildCityEncounter017D(expedition.CurrentEncounterId)),
                    RuntimeUi.PrimaryTouchPixels, RuntimeUi.Warning);
                return;
            }

            if (expedition.CanMove && expedition.LinkedNodeIds != null && expedition.LinkedNodeIds.Count > 0)
            {
                if (HasExpeditionFlagContaining065(state, "ENCOUNTER_CLEARED"))
                {
                    AddMessagePanel(
                        body,
                        "THE QUEST IS NOT OVER",
                        ExpeditionBoardProjection074.HasFirstHourPatrolRescueProof074(
                            state?.Expedition?.ObjectiveFlags)
                            ? "The patrol and Wayglass are secure. Choose the connected road home to Skyhome."
                            : "Lantern Road is clear, but the missing patrol is still inside the old Gatehouse. Choose a connected route and continue.",
                        RuntimeUi.Accent);
                }
                AddSelectableExpeditionRoutes065(body, coordinator, expedition);
                return;
            }

            AddMessagePanel(
                body,
                "THE PARTY IS REGROUPING",
                "Your next route will appear as soon as the current event finishes.",
                RuntimeUi.MutedText);
        }

        private void AddWalkableGateworksPreview066(Transform body)
        {
            var panel = RuntimeUi.AddPanel(body, "Walkable Outer Gateworks Preview 066", Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 230f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(24, 24, 16, 16), 7f, TextAnchor.MiddleLeft);
            AddResponsiveText062(panel.transform, "Walkable Preview Heading 066",
                "CHAPTER 1 • LANTERN ROAD TO THE OLD GATEHOUSE", 27, 40, 54f,
                RuntimeUi.Accent, FontStyle.Bold);
            AddResponsiveText062(panel.transform, "Walkable Preview Description 066",
                "Move with WASD, arrows, a left stick, or the touch stick. Roll with SPACE, B, or ROLL. Near people and markers, use E, A, or ACT.",
                21, 30, 88f, RuntimeUi.Text);
            AddResponsiveText062(panel.transform, "Walkable Preview Promise 066",
                "Follow the gold waymarkers, find the missing patrol, recover the Wayglass, and stop the Gatehouse threat.",
                19, 27, 38f, RuntimeUi.Positive, FontStyle.Bold);
        }

        private void AddWalkableGateworksEntry066(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var panel = RuntimeUi.AddPanel(body, "Walkable Outer Gateworks Entry 066", Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 278f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(24, 24, 14, 14), 7f, TextAnchor.MiddleLeft);
            AddResponsiveText062(panel.transform, "Walkable Gateworks Heading 066",
                "QUEST LOCATION • LANTERN ROAD / OLD GATEHOUSE", 27, 42, 58f,
                RuntimeUi.Accent, FontStyle.Bold);
            AddResponsiveText062(panel.transform, "Walkable Gateworks Objective 066",
                WalkableGateworksObjective066(state.Expedition),
                21, 31, 92f, RuntimeUi.Text);
            var enter = RuntimeUi.AddButton(panel.transform, "Enter Walkable Outer Gateworks 066",
                state.HasPendingEncounter ? "ENTER LANTERN ROAD • APPROACH ENEMY" : "ENTER LANTERN ROAD",
                () => EnterExpeditionBoard074(coordinator), 96f, RuntimeUi.Accent);
            ConfigureResponsiveText062(enter.GetComponentInChildren<Text>(), 23, 36);
            AddResponsiveText062(panel.transform, "Walkable Gateworks Controls 066",
                "WASD / ARROWS MOVE  •  SPACE DODGE-ROLL  •  E INTERACT  •  ESC RETURN TO QUEST LOG",
                17, 24, 32f, RuntimeUi.MutedText, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        private static string WalkableGateworksObjective066(
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            if (expedition?.CanFinalizeOperation == true)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Failed"))
                    return "The operation failed. Follow the glowing Skyhome markers through the ruins and bring your party home through the Skyhome exit.";
                if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Extracted"))
                    return "Your party has extracted. Follow the glowing Skyhome markers through the ruins to the Skyhome exit, then return to the Guild Hall.";
                if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Completed"))
                    return "The patrol and Wayglass are secure. Follow the glowing Skyhome markers along the return route and bring everyone home to the Guild Hall.";
                return "The operation has ended. Follow the glowing Skyhome markers through the ruins to the Skyhome exit.";
            }
            return "Follow Lantern Road to the old Gatehouse. Read the broken waymarkers, find the missing patrol, recover the Wayglass, and confront the threat hunting them.";
        }

        private void EnterOuterGateworks066(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if (coordinator == null || coordinator.GuildCity017D?.Expedition == null)
            {
                _localStatus = "Start the expedition before entering Lantern Road.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                return;
            }
            if (!IsGuidedFirstHourBoardForVerification076(
                    coordinator.GuildCity017D.Expedition.BoardId))
            {
                _localStatus = "Only the guided Lantern Road operation opens in the field.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                return;
            }
            if (_outerGateworksExploration066 != null) return;

            CloseWalkableGuildHall069();
            CloseGuildApplicantConversation069();
            CloseCompactInventory069();
            SuspendStudioAmbienceForWorld076();
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            try
            {
                _outerGateworksExploration066 =
                    gameObject.AddComponent<GuildCity017D.OuterGateworksExploration066>();
                _outerGateworksExploration066.Begin066(
                    coordinator,
                    ReturnFromOuterGateworks066,
                    () => EnterBattleFromOuterGateworks066(coordinator),
                    _outerGateworksCheckpoint076);
            }
            catch (Exception exception)
            {
                var failedExploration = _outerGateworksExploration066;
                _outerGateworksExploration066 = null;
                if (failedExploration != null)
                {
                    failedExploration.Shutdown066();
                    Destroy(failedExploration);
                }
                if (_canvas != null) _canvas.gameObject.SetActive(true);
                _localStatus = "Lantern Road could not open. Please try again.";
                _localStatusPositive = false;
                Debug.LogException(exception, this);
                BuildCurrentScreen();
            }
        }

        /// <summary>
        /// The Release 071 corridor is retained only for old save migration and
        /// diagnostic access.  The production first hour now uses the complete
        /// expedition board, where branches, camps, checks, optional objectives,
        /// and consequences remain visible instead of repeating a side-scroll
        /// backdrop between every story beat.
        /// </summary>
        public static bool IsGuidedFirstHourBoardForVerification076(string boardId) => false;

        private static bool ShouldEnterGuidedFirstHourField076(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator) =>
            coordinator is M1RuntimeCoordinator &&
            IsGuidedFirstHourBoardForVerification076(
                coordinator.GuildCity017D?.Expedition?.BoardId);

        /// <summary>
        /// Production play uses the deterministic fixed expedition presentation.
        /// Keeping this gate centralized also prevents an older 071 save from
        /// silently reopening the retired corridor presentation.
        /// </summary>
        private void EnterExpeditionBoard074(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if (coordinator == null || coordinator.GuildCity017D?.Expedition == null)
            {
                _localStatus = "Start the expedition before opening the route board.";
                _localStatusPositive = false;
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "HALL";
                BuildCurrentScreen();
                return;
            }

            if (ShouldEnterGuidedFirstHourField076(coordinator))
            {
                EnterOuterGateworks066(coordinator);
                return;
            }

            OpenExpeditionMissionBrief074(coordinator);
        }

        private void OpenExpeditionMissionBrief074(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            if(TryRouteLoopService164("EXPEDITION"))return;
            if (coordinator == null || coordinator.GuildCity017D?.Expedition == null)
            {
                _localStatus = "The saved operation is no longer active.";
                _localStatusPositive = false;
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "HALL";
                BuildCurrentScreen();
                return;
            }

            CloseOuterGateworks066();
            if (_canvas != null) _canvas.gameObject.SetActive(true);
            if (_screenRoot != null) _screenRoot.gameObject.SetActive(true);
            _returnToWalkableHall069 = true;
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "EXPEDITION";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        private void ReturnFromOuterGateworks066()
        {
            CloseOuterGateworks066();
            var coordinator = _coordinator as
                GuildCity017D.IGuildCityPresentationCoordinator017D;
            var state = coordinator?.GuildCity017D;
            var finalized = state?.Expedition == null;
            _localStatus = finalized
                ? "Your party returned to Skyhome."
                : "Mission brief opened. Your exact field position is saved.";
            _localStatusPositive = true;
            if (!finalized)
            {
                OpenExpeditionMissionBrief074(coordinator);
                return;
            }
            _guildCityTab017D = "HALL";
            BuildCurrentScreen();
        }

        private void EnterBattleFromOuterGateworks066(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            CloseOuterGateworks066();
            EnterCommittedGuildCityBattle017D(coordinator);
        }

        private void CloseOuterGateworks066()
        {
            var exploration = _outerGateworksExploration066;
            _outerGateworksExploration066 = null;
            if (exploration != null)
            {
                _outerGateworksCheckpoint076 = exploration.CaptureFieldCheckpoint076();
                exploration.Shutdown066();
                Destroy(exploration);
            }
            if (_canvas != null) _canvas.gameObject.SetActive(true);
        }

        private void AddFirstExpeditionMap065(
            Transform body,
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            var artwork = AddVisualSliceArtwork062(
                body,
                "First Expedition Illustrated Route Map 063",
                ExpeditionRouteMapResource063,
                1f,
                540f,
                "LANTERN ROAD TO THE OLD GATEHOUSE");

            var overlay = new GameObject(
                "Playable Expedition Board Overlay 065",
                typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.SetParent(artwork.transform, false);
            Stretch(overlay);

            var lines = new List<ExpeditionMapLine065>();
            for (var index = 0; index < ExpeditionRouteEdges065.Length; index++)
            {
                var edge = ExpeditionRouteEdges065[index];
                var image = RuntimeUi.AddPanel(
                    overlay,
                    "Expedition Route " + edge[0] + " to " + edge[1] + " 065",
                    new Color(0.95f, 0.72f, 0.22f, 0.7f));
                image.raycastTarget = false;
                image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                lines.Add(new ExpeditionMapLine065
                {
                    Transform = image.rectTransform,
                    From = ExpeditionNodePosition065(edge[0]),
                    To = ExpeditionNodePosition065(edge[1])
                });
            }

            var currentNodeId = expedition?.CurrentNodeId ?? "N00";
            var visited = expedition?.VisitedNodeIds ?? Array.Empty<string>();
            var linked = expedition?.LinkedNodeIds ?? Array.Empty<string>();
            for (var nodeIndex = 0; nodeIndex <= 14; nodeIndex++)
            {
                var nodeId = "N" + nodeIndex.ToString("00");
                var isCurrent = StringComparer.Ordinal.Equals(nodeId, currentNodeId);
                var isVisited = visited.Contains(nodeId);
                var isLinked = linked.Contains(nodeId);
                var node = RuntimeUi.AddPanel(
                    overlay,
                    "Expedition Board Node " + nodeId + " 065",
                    isCurrent
                        ? RuntimeUi.Accent
                        : isLinked ? RuntimeUi.Warning
                        : isVisited ? RuntimeUi.Positive
                        : new Color(0.08f, 0.12f, 0.15f, 0.92f));
                node.raycastTarget = false;
                var position = ExpeditionNodePosition065(nodeId);
                node.rectTransform.anchorMin = position;
                node.rectTransform.anchorMax = position;
                node.rectTransform.anchoredPosition = Vector2.zero;
                node.rectTransform.sizeDelta = isCurrent ? new Vector2(56f, 56f) : new Vector2(38f, 38f);
                var label = RuntimeUi.AddText(
                    node.transform,
                    "Expedition Board Node Label " + nodeId + " 065",
                    nodeIndex.ToString(),
                    isCurrent ? 24 : 17,
                    TextAnchor.MiddleCenter,
                    isCurrent ? Color.black : Color.white,
                    FontStyle.Bold);
                Stretch(label.rectTransform);
                label.raycastTarget = false;
            }

            var token = RuntimeUi.AddPanel(
                overlay,
                "Visible Union Party Token 065",
                new Color(0.06f, 0.82f, 0.95f, 1f));
            token.raycastTarget = false;
            token.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            token.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            token.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            token.rectTransform.sizeDelta = new Vector2(88f, 88f);
            var tokenText = RuntimeUi.AddText(
                token.transform,
                "Visible Union Party Token Label 065",
                "UNION\nPARTY",
                18,
                TextAnchor.MiddleCenter,
                Color.black,
                FontStyle.Bold);
            Stretch(tokenText.rectTransform);
            tokenText.raycastTarget = false;
            token.transform.SetAsLastSibling();

            StartCoroutine(LayoutAndAnimateExpeditionMap065(
                overlay,
                lines,
                token.rectTransform,
                currentNodeId));
        }

        private static void AddExpeditionTravelLedger065(
            Transform body,
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            var row = AddRow(body, "Expedition Travel Ledger 065", 10f, 94f);
            AddExpeditionTravelMetric065(row, "SUPPLIES", expedition.Supplies.ToString(), RuntimeUi.Positive);
            AddExpeditionTravelMetric065(row, "FATIGUE", expedition.Fatigue.ToString(), RuntimeUi.Warning);
            AddExpeditionTravelMetric065(row, "THREAT", expedition.Threat.ToString(), RuntimeUi.Warning);
            AddExpeditionTravelMetric065(row, "URGENCY", expedition.Urgency.ToString(), RuntimeUi.Accent);
        }

        private static void AddExpeditionTravelMetric065(
            Transform parent,
            string name,
            string value,
            Color color)
        {
            var panel = RuntimeUi.AddPanel(parent, "Expedition Metric " + name + " 065", Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 84f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldGlass);
            var text = RuntimeUi.AddText(
                panel.transform,
                "Expedition Metric Text " + name + " 065",
                name + "  " + value,
                24,
                TextAnchor.MiddleCenter,
                color,
                FontStyle.Bold);
            Stretch(text.rectTransform);
        }

        private void AddSelectableExpeditionRoutes065(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            AddMessagePanel(
                body,
                expedition.LinkedNodeIds.Count > 1 ? "CHOOSE YOUR ROUTE" : "CHOOSE THE NEXT ROAD",
                expedition.LinkedNodeIds.Count > 1
                    ? "Each connected road is a real choice. Select the destination your party will walk toward."
                    : "Choose the connected place your party will travel toward.",
                RuntimeUi.Accent);
            var routes = AddRow(body, "Selectable Expedition Routes 065", 14f, 170f);
            foreach (var destinationNodeId in expedition.LinkedNodeIds)
            {
                var capturedDestination = destinationNodeId;
                var definition = GuildCity017E.GuildCityOpeningExperienceRegistry017E.Node(
                    expedition.BoardId,
                    capturedDestination);
                var label = definition == null
                    ? "WALK TO " + capturedDestination
                    : "WALK TO " + definition.displayName.ToUpperInvariant() + "\n" + definition.routeCostSummary;
                var button = RuntimeUi.AddButton(
                    routes,
                    "Move To " + capturedDestination,
                    label,
                    () => MoveGuildCityExpedition065(coordinator, expedition, capturedDestination),
                    154f,
                    RuntimeUi.Accent);
                button.interactable = !_expeditionMovementPending065;
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 18, 28);
            }
        }

        private void MoveGuildCityExpedition065(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityExpeditionView017D expedition,
            string destinationNodeId)
        {
            var result = coordinator.MoveGuildCityExpedition017D(destinationNodeId);
            _localStatus = result?.Message ?? "Something went wrong—choose the road again.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
            {
                _expeditionMovementFromNodeId065 = expedition.CurrentNodeId;
                _expeditionMovementToNodeId065 = destinationNodeId;
                _expeditionMovementPending065 = true;
            }
            BuildCurrentScreen();
        }

        private void AddInteractiveExpeditionEvent065(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            AddExpeditionNarrative017F(body, expedition);
            var participants = state.Assignments
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId))
                .Take(4)
                .ToArray();
            if (participants.Length == 0)
            {
                AddMessagePanel(
                    body,
                    "CHOOSE A GUIDE",
                    "Assign one adventurer before attempting this blocked passage.",
                    RuntimeUi.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_expeditionLeadRecruitId065) ||
                participants.All(value => !StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    _expeditionLeadRecruitId065)))
                _expeditionLeadRecruitId065 = participants[0].RecruitId;

            AddMessagePanel(
                body,
                "1 • CHOOSE WHO LEADS",
                "The lead adventurer faces the challenge; a partner can assist on the careful approach.",
                RuntimeUi.Accent);
            var leaders = AddRow(body, "Expedition Event Leaders 065", 10f, 116f);
            foreach (var participant in participants)
            {
                var captured = participant;
                var selected = StringComparer.Ordinal.Equals(
                    captured.RecruitId,
                    _expeditionLeadRecruitId065);
                var button = RuntimeUi.AddButton(
                    leaders,
                    "Expedition Event Leader " + captured.RecruitId + " 065",
                    (selected ? "◆ " : string.Empty) + captured.RecruitName.ToUpperInvariant(),
                    () =>
                    {
                        _expeditionLeadRecruitId065 = captured.RecruitId;
                        BuildCurrentScreen();
                    },
                    104f,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 17, 26);
            }

            var narrative = GuildCity017F.GuildCityNarrativeRegistry017F.Event(expedition.CurrentEventId);
            var prompts = narrative?.choicePrompts == null || narrative.choicePrompts.Length == 0
                ? new[]
                {
                    "Work together and secure the route.",
                    "Move quickly and trust the lead adventurer."
                }
                : narrative.choicePrompts.Take(2).ToArray();
            _expeditionApproachIndex065 = Math.Max(0, Math.Min(_expeditionApproachIndex065, prompts.Length - 1));

            AddMessagePanel(
                body,
                "2 • CHOOSE AN APPROACH",
                "Your approach changes the modifier and whether a partner assists.",
                RuntimeUi.Accent);
            var choices = AddRow(body, "Expedition Event Choices 065", 12f, 154f);
            for (var index = 0; index < prompts.Length; index++)
            {
                var capturedIndex = index;
                var selected = capturedIndex == _expeditionApproachIndex065;
                var button = RuntimeUi.AddButton(
                    choices,
                    "Expedition Event Choice " + capturedIndex + " 065",
                    (selected ? "◆ " : string.Empty) + prompts[capturedIndex] +
                    (capturedIndex == 0 ? "\nPARTNER ASSISTS • +2" : "\nLEAD ACTS ALONE • +0"),
                    () =>
                    {
                        _expeditionApproachIndex065 = capturedIndex;
                        BuildCurrentScreen();
                    },
                    142f,
                    selected ? RuntimeUi.Warning : RuntimeUi.ButtonNormal);
                ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), 17, 25);
            }

            var dice = RuntimeUi.AddPanel(body, "Expedition 2D6 Dice Tray 065", Color.white);
            RuntimeUi.SetLayout(dice, preferredHeight: 250f);
            M1PremiumUi.StylePanel(dice, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(dice.transform, new RectOffset(22, 22, 12, 12), 8f, TextAnchor.MiddleCenter);
            AddResponsiveText062(
                dice.transform,
                "Expedition 2D6 Instruction 065",
                "3 • ROLL TWO SIX-SIDED DICE",
                24,
                36,
                44f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            var diceFaces = AddRow(dice.transform, "Expedition Animated Dice Faces 065", 18f, 88f);
            var dieOne = AddDiceFace065(diceFaces, "Expedition Die One 065");
            var dieTwo = AddDiceFace065(diceFaces, "Expedition Die Two 065");
            var lead = participants.First(value => StringComparer.Ordinal.Equals(
                value.RecruitId,
                _expeditionLeadRecruitId065));
            var assistant = _expeditionApproachIndex065 == 0
                ? participants.FirstOrDefault(value => !StringComparer.Ordinal.Equals(
                    value.RecruitId,
                    lead.RecruitId))
                : null;
            var modifier = _expeditionApproachIndex065 == 0 ? 2 : 0;
            var roll = RuntimeUi.AddButton(
                dice.transform,
                "Commit Check 017D",
                _expeditionDiceRolling065
                    ? "ROLLING..."
                    : "ROLL 2D6",
                () => BeginExpeditionDiceRoll065(
                    coordinator,
                    expedition,
                    lead.RecruitId,
                    assistant?.RecruitId ?? string.Empty,
                    modifier,
                    dieOne,
                    dieTwo),
                92f,
                RuntimeUi.Warning);
            roll.interactable = !_expeditionDiceRolling065;
            AddResponsiveText062(
                dice.transform,
                "Expedition Dice Commitment Notice 065",
                "Roll once to see how this approach turns out.",
                17,
                24,
                30f,
                RuntimeUi.MutedText,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
        }

        private static Text AddDiceFace065(Transform parent, string name)
        {
            var face = RuntimeUi.AddPanel(parent, name + " Frame", Color.white);
            RuntimeUi.SetLayout(face, preferredHeight: 82f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(face, M1PremiumUi.Surface.WorldGlass);
            var text = RuntimeUi.AddText(
                face.transform,
                name,
                "[ ? ]",
                46,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            Stretch(text.rectTransform);
            return text;
        }

        private void BeginExpeditionDiceRoll065(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityExpeditionView017D expedition,
            string actorRecruitId,
            string assistantRecruitId,
            int modifier,
            Text dieOne,
            Text dieTwo)
        {
            if (_expeditionDiceRolling065) return;
            _expeditionDiceRolling065 = true;
            StartCoroutine(RollAndCommitExpeditionCheck065(
                coordinator,
                string.IsNullOrWhiteSpace(expedition.CurrentEventId)
                    ? "EVENT_COLLAPSED_HANDRAIL"
                    : expedition.CurrentEventId,
                actorRecruitId,
                assistantRecruitId,
                modifier,
                dieOne,
                dieTwo));
        }

        private IEnumerator RollAndCommitExpeditionCheck065(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            string eventId,
            string actorRecruitId,
            string assistantRecruitId,
            int modifier,
            Text dieOne,
            Text dieTwo)
        {
            for (var frame = 0; frame < 10; frame++)
            {
                if (dieOne == null || dieTwo == null) break;
                dieOne.text = "[ " + ((frame * 5 + 2) % 6 + 1) + " ]";
                dieTwo.text = "[ " + ((frame * 3 + 4) % 6 + 1) + " ]";
                yield return new WaitForSecondsRealtime(0.085f);
            }

            var result = coordinator.ResolveGuildCityCheck017D(
                eventId,
                actorRecruitId,
                assistantRecruitId,
                modifier);
            _localStatus = result?.Message ?? "Something went wrong—try the 2d6 check again.";
            _localStatusPositive = result != null && result.Succeeded;
            _expeditionDiceRolling065 = false;
            BuildCurrentScreen();
        }

        private static void AddCommittedDiceResult065(
            Transform body,
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            var actor = state.Assignments.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, expedition.LastCheckActorRecruitId));
            var result = RuntimeUi.AddPanel(body, "Committed Expedition 2D6 Result 065", Color.white);
            RuntimeUi.SetLayout(result, preferredHeight: 174f);
            M1PremiumUi.StylePanel(result, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(result.transform, new RectOffset(22, 22, 10, 10), 5f, TextAnchor.MiddleCenter);
            AddResponsiveText062(
                result.transform,
                "Committed Expedition Dice Values 065",
                "2D6 RESULT • [ " + expedition.LastCheckDieOne + " ] + [ " + expedition.LastCheckDieTwo +
                " ] " + SignedModifier065(expedition.LastCheckModifier) + " = " + expedition.LastCheckTotal,
                28,
                42,
                62f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            AddResponsiveText062(
                result.transform,
                "Committed Expedition Dice Outcome 065",
                (actor?.RecruitName ?? "Your lead adventurer") + " • " +
                HumanizePresentationId(expedition.LastCheckOutcome),
                21,
                30,
                44f,
                expedition.LastCheckTotal >= 7 ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
        }

        private static string SignedModifier065(int modifier) =>
            modifier >= 0 ? "+ " + modifier : "- " + Math.Abs(modifier);

        private IEnumerator LayoutAndAnimateExpeditionMap065(
            RectTransform overlay,
            IReadOnlyList<ExpeditionMapLine065> lines,
            RectTransform token,
            string currentNodeId)
        {
            yield return null;
            if (overlay == null || token == null) yield break;
            LayoutExpeditionRouteLines065(overlay, lines);

            var destination = ExpeditionMapPixelPosition065(
                overlay,
                ExpeditionNodePosition065(currentNodeId));
            if (!_expeditionMovementPending065 ||
                !StringComparer.Ordinal.Equals(currentNodeId, _expeditionMovementToNodeId065) ||
                _reducedMotion)
            {
                token.anchoredPosition = destination;
                if (_reducedMotion && _expeditionMovementPending065)
                {
                    _expeditionMovementPending065 = false;
                    _expeditionMovementFromNodeId065 = null;
                    _expeditionMovementToNodeId065 = null;
                }
                yield break;
            }

            var origin = ExpeditionMapPixelPosition065(
                overlay,
                ExpeditionNodePosition065(_expeditionMovementFromNodeId065));
            token.anchoredPosition = origin;
            var elapsed = 0f;
            const float duration = 1.15f;
            while (elapsed < duration && token != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                token.anchoredPosition = Vector2.LerpUnclamped(origin, destination, progress);
                token.localScale = Vector3.one * (1f + Mathf.Sin(progress * Mathf.PI * 6f) * 0.07f);
                yield return null;
            }
            if (token != null)
            {
                token.anchoredPosition = destination;
                token.localScale = Vector3.one;
            }
            _expeditionMovementPending065 = false;
            _expeditionMovementFromNodeId065 = null;
            _expeditionMovementToNodeId065 = null;
            if (this != null && isActiveAndEnabled) BuildCurrentScreen();
        }

        private static void LayoutExpeditionRouteLines065(
            RectTransform overlay,
            IReadOnlyList<ExpeditionMapLine065> lines)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (line?.Transform == null) continue;
                var start = ExpeditionMapPixelPosition065(overlay, line.From);
                var end = ExpeditionMapPixelPosition065(overlay, line.To);
                var delta = end - start;
                line.Transform.anchoredPosition = (start + end) * 0.5f;
                line.Transform.sizeDelta = new Vector2(delta.magnitude, 7f);
                line.Transform.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private static Vector2 ExpeditionMapPixelPosition065(
            RectTransform overlay,
            Vector2 normalizedPosition)
        {
            var width = overlay.rect.width;
            var height = overlay.rect.height;
            return new Vector2(
                (normalizedPosition.x - 0.5f) * width,
                (normalizedPosition.y - 0.5f) * height);
        }

        private static Vector2 ExpeditionNodePosition065(string nodeId)
        {
            switch (nodeId)
            {
                case "N00": return new Vector2(0.16f, 0.48f);
                case "N01": return new Vector2(0.25f, 0.45f);
                case "N02": return new Vector2(0.33f, 0.60f);
                case "N03": return new Vector2(0.43f, 0.64f);
                case "N04": return new Vector2(0.34f, 0.29f);
                case "N05": return new Vector2(0.44f, 0.32f);
                case "N06": return new Vector2(0.53f, 0.47f);
                case "N07": return new Vector2(0.62f, 0.30f);
                case "N08": return new Vector2(0.62f, 0.61f);
                case "N09": return new Vector2(0.70f, 0.65f);
                case "N10": return new Vector2(0.73f, 0.47f);
                case "N11": return new Vector2(0.81f, 0.31f);
                case "N12": return new Vector2(0.82f, 0.55f);
                case "N13": return new Vector2(0.88f, 0.47f);
                case "N14": return new Vector2(0.94f, 0.54f);
                default: return new Vector2(0.16f, 0.48f);
            }
        }

        private void FinalizeStoryOperation065(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            var result = coordinator.FinalizeGuildCityOperation017D();
            _localStatus = result?.Message ?? "The operation could not be finalized.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
            {
                // Finalization and persistence are synchronous. Route from the
                // freshly projected authority so the mandatory first-operation
                // report cannot be hidden behind an extra Hall click.
                _guildCityTab017D = NeedsFirstOperationConsequences077(
                        coordinator.GuildCity017D)
                    ? "CONSEQUENCES"
                    : "HALL";
                _guildCityMoreOpen060 = false;
            }
            BuildCurrentScreen();
        }

        private void AddPostBattleGrowthCheckpoint065(
            Transform body,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var members = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(recruit => recruit != null)
                .OrderByDescending(recruit => recruit.TotalPersonalXp)
                .ThenBy(recruit => recruit.DisplayName, StringComparer.Ordinal)
                .Take(3)
                .ToArray();
            if (members.Length == 0) return;
            var panel = RuntimeUi.AddPanel(body, "Post Battle Saved Growth Checkpoint 065", Color.white);
            RuntimeUi.SetLayout(panel, preferredHeight: 174f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(18, 18, 8, 8), 3f, TextAnchor.MiddleLeft);
            AddResponsiveText062(
                panel.transform,
                "Post Battle Saved Growth Heading 065",
                "BATTLE GROWTH SAVED  •  GUILD LV " + Math.Max(1, _coordinator.State.GuildLevel) +
                "  •  HALL XP " + state.HallEnhancementXp,
                20,
                30,
                38f,
                RuntimeUi.Positive,
                FontStyle.Bold);
            foreach (var recruit in members)
            {
                var art = recruit.ArtMastery?
                    .Where(value => value != null)
                    .OrderByDescending(value => value.MasteryPoints)
                    .FirstOrDefault();
                AddResponsiveText062(
                    panel.transform,
                    "Post Battle Saved Growth " + recruit.RecruitId + " 065",
                    (recruit.DisplayName ?? recruit.RecruitId) + "  •  LV " + Math.Max(1, recruit.Level) +
                    "  •  PERSONAL XP " + recruit.TotalPersonalXp +
                    (art == null ? string.Empty : "  •  " + art.DisplayName + " MASTERY " + art.MasteryPoints),
                    15,
                    23,
                    32f,
                    RuntimeUi.Text,
                    FontStyle.Bold);
            }
        }

        private static string ExpeditionLocationHeading063(
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            if (expedition == null) return "LANTERN ROAD TO THE OLD GATEHOUSE";
            if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "ENCOUNTER"))
                return "AT THE GATEHOUSE BREACH";
            if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK"))
                return "ON LANTERN ROAD";
            return "AT THE OLD GATEHOUSE";
        }

        private static string ExpeditionSituation063(
            GuildCity017D.GuildCityExpeditionView017D expedition)
        {
            if (expedition == null) return "Your party is preparing to leave Skyhome.";
            var supplies = expedition.Supplies > 6 ? "Supplies are plentiful" : "Supplies are running low";
            var danger = expedition.Threat <= 2 ? "danger is manageable" : "danger is rising";
            return supplies + ", " + danger + ", and the Wayglass signal is still ahead.";
        }

        private void BuildGuildCityRelationships017D(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            AddMessagePanel(body, "RELATIONSHIPS ARE GROWTH, NOT UPKEEP",
                "Memories come from shared missions, camps, protection, healing, mentorship, staffing, success, failure, and recovery. Scenes cost zero operations, never expire, and never make a signed member leave.",
                RuntimeUi.Positive);
            AddRelationshipNarrative017F(body, state);
            foreach (var memory in state.Relationships)
            {
                var panel = AddRow(body, "Relationship " + memory.MemoryId, 14f, 145f);
                RuntimeUi.AddText(panel, "Relationship Summary " + memory.MemoryId,
                    memory.Summary + "\n" + memory.FirstRecruitId + " ↔ " + memory.SecondRecruitId +
                    "  •  STRENGTH " + memory.Strength + "  •  " + (memory.Viewed ? "VIEWED" : "AVAILABLE FREE"),
                    30, TextAnchor.MiddleLeft, memory.Viewed ? RuntimeUi.MutedText : RuntimeUi.Text, FontStyle.Bold);
                if (!memory.Viewed && !string.IsNullOrWhiteSpace(memory.SceneId))
                    RuntimeUi.AddButton(panel, "View Relationship " + memory.MemoryId, "VIEW FREE SCENE",
                        () => ApplyGuildCity017D(coordinator.ViewGuildCityRelationshipScene017D(memory.SceneId)), 110f,
                        RuntimeUi.Accent);
            }
            if (state.Relationships.Count == 0)
            {
                AddMessagePanel(body, "NO SHARED MEMORIES YET",
                    "Deploy members together, protect or heal one another, share a camp, train, mentor, or staff the same facility. Meaningful play will create free Hall scenes automatically.",
                    RuntimeUi.MutedText);
            }
        }

        private void EnterCommittedGuildCityBattle017D(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator)
        {
            var result = coordinator.StartCommittedGuildCityBattle017D();
            _localStatus = result?.Message ?? "Something went wrong—try entering battle again.";
            _localStatusPositive = result != null && result.Succeeded;
            if (result != null && result.Succeeded) Navigate(M1Screen.Battle);
            else BuildCurrentScreen();
        }

        private void ApplyGuildCity017D(M1CommandResult result)
        {
            _localStatus = result?.Message ?? "Something went wrong—try again.";
            _localStatusPositive = result != null && result.Succeeded;
            BuildCurrentScreen();
        }
    }
}
