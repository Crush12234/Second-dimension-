using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    /// <summary>
    /// Durable, action-based proof for the two short Guild Hall onboarding loops.
    /// These gates record player decisions; they never depend on elapsed time or a
    /// presentation-only flag, so save/reload cannot skip or strand the sequence.
    /// </summary>
    public static class GuildHallGuidedProgression080
    {
        public const string FirstContractUnionBriefingGate080 =
            "STORY_GATE_FIRST_CONTRACT_UNION_BRIEFING_080";
        public const string FirstFacilityPayoffGate080 =
            "STORY_GATE_FIRST_FACILITY_PAYOFF_080";
        public const string RecoveredLootInstancePrefix080 = "LOOT_ITEM_070_";

        public static bool HasFirstContractUnionBriefing080(GuildCityState017D city) =>
            HasGate080(city, FirstContractUnionBriefingGate080);

        public static bool HasFirstFacilityPayoff080(GuildCityState017D city) =>
            HasGate080(city, FirstFacilityPayoffGate080);

        public static bool HasWorkingFirstFacility080(GuildCityState017D city) =>
            city?.CityPlots != null && city.CityPlots.Any(plot =>
                plot != null &&
                !string.IsNullOrWhiteSpace(plot.BuildingId) &&
                plot.BuildingLevel > 0 &&
                (plot.StaffRecruitIds?.Count ?? 0) > 0);

        public static bool HasEquippedRecoveredLoot080(GuildState guild) =>
            guild?.Recruits != null && guild.Recruits.Any(recruit =>
                recruit?.Equipment?.Assignments != null &&
                recruit.Equipment.Assignments.Any(assignment =>
                    assignment?.Item != null &&
                    assignment.Item.InstanceId.StartsWith(
                        RecoveredLootInstancePrefix080,
                        StringComparison.Ordinal)));

        public static Result<CampaignState> ConfirmFirstContractUnionBriefing080(
            CampaignState campaign)
        {
            if (campaign?.Guild?.GuildCity == null || campaign.OpeningFlow == null)
                return Result<CampaignState>.Failure("GC080_GUILD_SAVE_REQUIRED");
            if (HasFirstContractUnionBriefing080(campaign.Guild.GuildCity))
                return Result<CampaignState>.Success(campaign);
            if (campaign.Guild.GuildCity.OperationOrdinal != 0 ||
                campaign.Guild.GuildCity.ActiveContract != null)
                return Result<CampaignState>.Failure(
                    "GC080_FIRST_CONTRACT_UNION_BRIEFING_WINDOW_CLOSED");
            if (campaign.Guild.GuildCity.RecruitmentBoard == null)
                return Result<CampaignState>.Failure(
                    "GC080_MEET_AN_APPLICANT_BEFORE_UNION_BRIEFING");
            if (campaign.Guild.Recruits.Count <
                GuildCityExpeditionService017D.FirstStoryMinimumRosterCount066)
                return Result<CampaignState>.Failure(
                    "GC080_FIRST_CONTRACT_RECRUIT_REQUIRED");
            if (campaign.OpeningFlow.Stage != OpeningStage.Complete ||
                !new M1CommandService().ValidateGuildUnionPlans(campaign.Guild).IsSuccess)
                return Result<CampaignState>.Failure(
                    "GC080_LEGAL_UNION_PLAN_REQUIRED");

            return AddGate080(
                campaign,
                FirstContractUnionBriefingGate080,
                "first_contract_union_briefing_confirmed_080");
        }

        public static Result<CampaignState> AcknowledgeFirstFacilityPayoff080(
            CampaignState campaign)
        {
            if (campaign?.Guild?.GuildCity == null)
                return Result<CampaignState>.Failure("GC080_GUILD_SAVE_REQUIRED");
            if (HasFirstFacilityPayoff080(campaign.Guild.GuildCity))
                return Result<CampaignState>.Success(campaign);
            if (campaign.Guild.GuildCity.OperationOrdinal < 1)
                return Result<CampaignState>.Failure(
                    "GC080_FIRST_OPERATION_REQUIRED");
            if (!HasWorkingFirstFacility080(campaign.Guild.GuildCity))
                return Result<CampaignState>.Failure(
                    "GC080_WORKING_FIRST_FACILITY_REQUIRED");

            return AddGate080(
                campaign,
                FirstFacilityPayoffGate080,
                "first_facility_payoff_acknowledged_080");
        }

        private static bool HasGate080(GuildCityState017D city, string gateId) =>
            city?.Strategic017H?.StoryGates != null &&
            city.Strategic017H.StoryGates.Contains(gateId, StringComparer.Ordinal);

        private static Result<CampaignState> AddGate080(
            CampaignState campaign,
            string gateId,
            string checkpointId)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var gates = new List<string>(strategic.StoryGates ?? Array.Empty<string>());
            if (!gates.Contains(gateId, StringComparer.Ordinal)) gates.Add(gateId);
            gates.Sort(StringComparer.Ordinal);
            strategic = strategic.With(
                storyGates: gates.AsReadOnly(),
                lastCheckpointId: checkpointId);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: checkpointId);
            return Result<CampaignState>.Success(campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow));
        }
    }
}
