using System;
using SecondDimension.Gameplay.GuildCity017D;

namespace SecondDimension.Presentation
{
    public enum GuildHallGuidedStage080
    {
        Ready,
        MeetApplicant,
        RecruitAdventurer,
        ConfirmSixSlotUnion,
        SetFirstOperationConsequences,
        ReviewFirstFacilityPayoff,
        EquipRecoveredLoot
    }

    public sealed partial class M1FlowPresenter
    {
        public static GuildHallGuidedStage080 GuidedHallStageForVerification080(
            GuildCity017D.GuildCityPresentationState017D state,
            bool firstStoryComplete,
            bool secondStoryComplete)
        {
            if (state == null || state.HasUnclaimedBattleReward)
                return GuildHallGuidedStage080.Ready;

            if (!firstStoryComplete)
            {
                if (state.HasActiveContract || state.Expedition != null)
                    return GuildHallGuidedStage080.Ready;
                // The current opening grants a complete ten-person founding company
                // and ready Union plans before the Hall opens. Do not send that
                // player into a paid recruitment step at 0 XP; the first useful
                // action is the story mission that earns recruitment currency.
                if (state.TotalRecruitCount >=
                        GuildCityExpeditionService017D.FirstStoryMinimumRosterCount066 &&
                    state.NormalUnionCount > 0)
                    return GuildHallGuidedStage080.Ready;
                if (!state.HasRecruitmentBoard)
                    return GuildHallGuidedStage080.MeetApplicant;
                if (state.TotalRecruitCount <
                    GuildCityExpeditionService017D.FirstStoryMinimumRosterCount066)
                    return GuildHallGuidedStage080.RecruitAdventurer;
                if (!state.FirstContractUnionBriefingConfirmed080)
                    return GuildHallGuidedStage080.ConfirmSixSlotUnion;
                return GuildHallGuidedStage080.Ready;
            }

            if (secondStoryComplete || state.HasActiveContract || state.Expedition != null)
                return GuildHallGuidedStage080.Ready;
            if (NeedsFirstOperationConsequences077(state))
                return GuildHallGuidedStage080.SetFirstOperationConsequences;
            // Home-base acknowledgement and recovered gear remain available from
            // the six Hall actions, but they no longer block the next mission.
            return GuildHallGuidedStage080.Ready;
        }

        public static string GuidedHallObjectiveForVerification080(
            GuildHallGuidedStage080 stage)
        {
            switch (stage)
            {
                case GuildHallGuidedStage080.MeetApplicant:
                    return "Meet the first applicant. They can help find the missing Wayglass on Lantern Road.";
                case GuildHallGuidedStage080.RecruitAdventurer:
                    return "Recruit one permanent adventurer for the missing Wayglass patrol on Lantern Road.";
                case GuildHallGuidedStage080.ConfirmSixSlotUnion:
                    return "Review the Union's six visible slots. Confirm the saved assignment.";
                case GuildHallGuidedStage080.SetFirstOperationConsequences:
                    return "Set recovery, keep one shared memory, then build and staff your first facility.";
                case GuildHallGuidedStage080.ReviewFirstFacilityPayoff:
                    return "Visit Skyhome Settlement. See what your staffed facility changes.";
                case GuildHallGuidedStage080.EquipRecoveredLoot:
                    return "Equip one recovered weapon in the Armory before Chapter 2.";
                default:
                    return string.Empty;
            }
        }

        public static string GuidedHallActionForVerification080(
            GuildHallGuidedStage080 stage)
        {
            switch (stage)
            {
                case GuildHallGuidedStage080.MeetApplicant:
                    return "MEET AN APPLICANT";
                case GuildHallGuidedStage080.RecruitAdventurer:
                    return "RECRUIT AN ADVENTURER";
                case GuildHallGuidedStage080.ConfirmSixSlotUnion:
                    return "CONFIRM 6-SLOT UNION";
                case GuildHallGuidedStage080.SetFirstOperationConsequences:
                    return "SET GUILD HOMECOMING";
                case GuildHallGuidedStage080.ReviewFirstFacilityPayoff:
                    return "SEE FACILITY PAYOFF";
                case GuildHallGuidedStage080.EquipRecoveredLoot:
                    return "EQUIP RECOVERED LOOT";
                default:
                    return string.Empty;
            }
        }

        private GuildHallGuidedStage080 CurrentGuidedHallStage080(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            return GuidedHallStageForVerification080(
                state,
                IsStoryContractCompleted065(state, FirstStoryContractId065),
                IsStoryContractCompleted065(state, SecondStoryContractId065));
        }

        private void ConfirmGuidedFirstContractUnionBriefing080()
        {
            var state = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)
                ?.GuildCity017D;
            if (CurrentGuidedHallStage080(state) !=
                GuildHallGuidedStage080.ConfirmSixSlotUnion)
                return;

            var progression = _coordinator as
                GuildCity017D.IGuildHallGuidedProgressionCoordinator080;
            if (progression == null) return;
            var result = progression.ConfirmFirstContractUnionBriefing080();
            if (result != null && result.Succeeded) return;
            _localStatus = result?.Message ??
                "The Union assignment could not be confirmed. No plan was changed.";
            _localStatusPositive = false;
        }

        private bool TryOpenCurrentGuidedHallStep080(
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var stage = CurrentGuidedHallStage080(state);
            switch (stage)
            {
                case GuildHallGuidedStage080.MeetApplicant:
                case GuildHallGuidedStage080.RecruitAdventurer:
                    OpenHallApplicants069();
                    return true;
                case GuildHallGuidedStage080.ConfirmSixSlotUnion:
                    OpenHallParty069();
                    return true;
                case GuildHallGuidedStage080.SetFirstOperationConsequences:
                    OpenFirstOperationConsequences077();
                    return true;
                case GuildHallGuidedStage080.ReviewFirstFacilityPayoff:
                    _guildCityTab017D = "CITY";
                    _guildCityMoreOpen060 = false;
                    BuildCurrentScreen();
                    return true;
                case GuildHallGuidedStage080.EquipRecoveredLoot:
                    OpenHallInventory069();
                    return true;
                default:
                    return false;
            }
        }

        private static bool GuidedStageBlocksContract080(
            GuildHallGuidedStage080 stage,
            string contractId)
        {
            if (StringComparer.Ordinal.Equals(contractId, FirstStoryContractId065))
                return stage == GuildHallGuidedStage080.MeetApplicant ||
                       stage == GuildHallGuidedStage080.RecruitAdventurer ||
                       stage == GuildHallGuidedStage080.ConfirmSixSlotUnion;
            if (StringComparer.Ordinal.Equals(contractId, SecondStoryContractId065))
                return stage == GuildHallGuidedStage080.SetFirstOperationConsequences ||
                       stage == GuildHallGuidedStage080.ReviewFirstFacilityPayoff ||
                       stage == GuildHallGuidedStage080.EquipRecoveredLoot;
            return false;
        }

        private static string GuidedHallDestinationId080(
            GuildHallGuidedStage080 stage)
        {
            switch (stage)
            {
                case GuildHallGuidedStage080.MeetApplicant:
                case GuildHallGuidedStage080.RecruitAdventurer:
                    return GuildCity017D.WalkableGuildHall069.RecruitmentDestinationId069;
                case GuildHallGuidedStage080.ConfirmSixSlotUnion:
                    return GuildCity017D.WalkableGuildHall069.PartyDestinationId069;
                case GuildHallGuidedStage080.ReviewFirstFacilityPayoff:
                    return LivingGuildHubCityDestinationId078;
                case GuildHallGuidedStage080.EquipRecoveredLoot:
                    return GuildCity017D.WalkableGuildHall069.ArmoryDestinationId069;
                default:
                    return GuildCity017D.WalkableGuildHall069.GuideDestinationId069;
            }
        }
    }
}
