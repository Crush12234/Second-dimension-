using System;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public enum QuickPlayDestination070
    {
        PartyPreparation,
        Recruitment,
        Expedition,
        Battle,
        GuildHall
    }

    /// <summary>
    /// A player-facing routing result for the single Continue Story / Quick Play
    /// action.  Stable requirement IDs remain internal; Title and Guidance are
    /// deliberately plain-language strings suitable for the default view.
    /// </summary>
    public sealed class QuickPlayOutcome070
    {
        public QuickPlayOutcome070(
            CampaignState campaign,
            QuickPlayDestination070 destination,
            string title,
            string guidance,
            string contractId,
            string requirementId,
            bool reusedSavedUnionPlan,
            bool acceptedContract,
            bool startedExpedition)
        {
            Campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
            Destination = destination;
            Title = string.IsNullOrWhiteSpace(title) ? "Continue Story" : title;
            Guidance = guidance ?? string.Empty;
            ContractId = contractId ?? string.Empty;
            RequirementId = requirementId ?? string.Empty;
            ReusedSavedUnionPlan = reusedSavedUnionPlan;
            AcceptedContract = acceptedContract;
            StartedExpedition = startedExpedition;
        }

        public CampaignState Campaign { get; }
        public QuickPlayDestination070 Destination { get; }
        public string Title { get; }
        public string Guidance { get; }
        public string ContractId { get; }
        public string RequirementId { get; }
        public bool ReusedSavedUnionPlan { get; }
        public bool AcceptedContract { get; }
        public bool StartedExpedition { get; }
        public bool RequiresPlayerAction => !string.IsNullOrEmpty(RequirementId);
    }

    /// <summary>
    /// Chooses the shortest legal route back into the active Guild story.  It may
    /// accept the marked contract and start its expedition, but it never signs a
    /// recruit, changes equipment, or edits the player's saved Union plan.
    /// </summary>
    public sealed class QuickPlayCommandService070
    {
        public const string PartyPreparationRequirementId = "PARTY_PREPARATION_REQUIRED";
        public const string RecruitmentRequirementId = "RECRUITMENT_REQUIRED";

        private readonly GuildCityExpeditionService017D _expeditions;
        private readonly M1CommandService _opening;

        public QuickPlayCommandService070()
            : this(new GuildCityExpeditionService017D(), new M1CommandService())
        {
        }

        internal QuickPlayCommandService070(
            GuildCityExpeditionService017D expeditions,
            M1CommandService opening)
        {
            _expeditions = expeditions ?? throw new ArgumentNullException(nameof(expeditions));
            _opening = opening ?? throw new ArgumentNullException(nameof(opening));
        }

        public Result<QuickPlayOutcome070> ContinueStory(
            CampaignState campaign,
            GuildCityContent017D content,
            string markedStoryContractId,
            GuildCityStrategicContent017H strategicContent = null)
        {
            if (campaign == null || content == null)
                return Result<QuickPlayOutcome070>.Failure("QUICK_PLAY_070_INPUT_REQUIRED");
            if (string.IsNullOrWhiteSpace(markedStoryContractId))
                return Result<QuickPlayOutcome070>.Failure("QUICK_PLAY_070_MARKED_STORY_CONTRACT_REQUIRED");

            var activeContractId = ActiveContractId(campaign);

            // Saved playable state always wins. A player must be able to resume a
            // battle or expedition even if a later roster edit made the current Hall
            // plan invalid for starting a new contract.
            if (campaign.Battle != null && campaign.Battle.Outcome == BattleOutcome.InProgress)
                return Resume(campaign, QuickPlayDestination070.Battle, "Resume Battle",
                    "Your saved battle is ready to continue.", activeContractId);

            var city = campaign.Guild.GuildCity;
            if (city == null)
                return Result<QuickPlayOutcome070>.Failure("QUICK_PLAY_070_GUILD_CITY_REQUIRED");
            var expedition = city.Expedition;

            if (city.PendingBattleReturn != null)
                return Resume(campaign, QuickPlayDestination070.Battle, "Finish Battle Rewards",
                    "Review the result and return to the expedition.", activeContractId);

            if (city.PendingEncounter != null ||
                (expedition != null && expedition.Status == ExpeditionStatus017D.AwaitingBattle))
                return Resume(campaign, QuickPlayDestination070.Battle, "Enter Battle",
                    "The expedition encounter is ready.", activeContractId);

            if (expedition != null)
            {
                if (expedition.Status == ExpeditionStatus017D.Active)
                    return Resume(campaign, QuickPlayDestination070.Expedition, "Resume Expedition",
                        "Continue from your last saved location.", activeContractId);

                // A terminal expedition still owns a physical walk-home/finalize
                // interaction. Route back into that space instead of leaving the
                // player in the Hall with a message and no actionable destination.
                return Resume(campaign, QuickPlayDestination070.Expedition, "Complete Your Return",
                    "Follow the home marker and finish the expedition return.", activeContractId);
            }

            // Quick Play reuses the exact saved plan. An invalid plan is a focused
            // preparation route, never permission to reshuffle or fill it for the player.
            // This gate belongs after all resumable gameplay has been handled.
            var unionValidation = _opening.ValidateGuildUnionPlans(campaign.Guild);
            if (!unionValidation.IsSuccess)
            {
                return Result<QuickPlayOutcome070>.Success(new QuickPlayOutcome070(
                    campaign,
                    QuickPlayDestination070.PartyPreparation,
                    "Prepare Your Party",
                    "Assign at least two saved Unions with one to six owned adventurers each. Up to ten ready Unions can deploy; then choose Quick Play again.",
                    activeContractId,
                    PartyPreparationRequirementId,
                    reusedSavedUnionPlan: false,
                    acceptedContract: false,
                    startedExpedition: false));
            }

            if (city.ActiveContract != null &&
                !city.ActiveContract.Completed && !city.ActiveContract.Failed)
            {
                return StartAcceptedContract(campaign, content, strategicContent,
                    city.ActiveContract.ContractId, acceptedContract: false);
            }

            if (!content.Contracts.ContainsKey(markedStoryContractId))
                return Result<QuickPlayOutcome070>.Failure("QUICK_PLAY_070_MARKED_STORY_CONTRACT_NOT_FOUND");

            // The first story contract intentionally requires the player's first
            // recurring recruit.  Quick Play points to recruitment but never signs one.
            if (StringComparer.Ordinal.Equals(
                    markedStoryContractId,
                    GuildCityExpeditionService017D.FirstStoryContractId066) &&
                campaign.Guild.Recruits.Count <
                GuildCityExpeditionService017D.FirstStoryMinimumRosterCount066)
            {
                return Result<QuickPlayOutcome070>.Success(new QuickPlayOutcome070(
                    campaign,
                    QuickPlayDestination070.Recruitment,
                    "Recruit One Adventurer",
                    "Meet the applicants and personally choose one permanent guild member before the first rescue.",
                    markedStoryContractId,
                    RecruitmentRequirementId,
                    reusedSavedUnionPlan: true,
                    acceptedContract: false,
                    startedExpedition: false));
            }

            var accepted = _expeditions.AcceptContract(campaign, content, markedStoryContractId);
            if (!accepted.IsSuccess)
                return Result<QuickPlayOutcome070>.Failure(CopyErrors(accepted.Errors));

            return StartAcceptedContract(accepted.Value, content, strategicContent,
                markedStoryContractId, acceptedContract: true);
        }

        private Result<QuickPlayOutcome070> StartAcceptedContract(
            CampaignState campaign,
            GuildCityContent017D content,
            GuildCityStrategicContent017H strategicContent,
            string contractId,
            bool acceptedContract)
        {
            var started = _expeditions.StartExpedition(campaign, content, strategicContent);
            if (!started.IsSuccess)
                return Result<QuickPlayOutcome070>.Failure(CopyErrors(started.Errors));

            return Result<QuickPlayOutcome070>.Success(new QuickPlayOutcome070(
                started.Value,
                QuickPlayDestination070.Expedition,
                "Begin Expedition",
                "Your saved Unions are ready. Enter the marked story route.",
                contractId,
                string.Empty,
                reusedSavedUnionPlan: true,
                acceptedContract: acceptedContract,
                startedExpedition: true));
        }

        private static Result<QuickPlayOutcome070> Resume(
            CampaignState campaign,
            QuickPlayDestination070 destination,
            string title,
            string guidance,
            string contractId)
        {
            return Result<QuickPlayOutcome070>.Success(new QuickPlayOutcome070(
                campaign,
                destination,
                title,
                guidance,
                contractId,
                string.Empty,
                reusedSavedUnionPlan: true,
                acceptedContract: false,
                startedExpedition: false));
        }

        private static string ActiveContractId(CampaignState campaign)
        {
            return campaign?.Guild?.GuildCity?.ActiveContract?.ContractId ?? string.Empty;
        }

        private static string[] CopyErrors(System.Collections.Generic.IReadOnlyList<string> errors)
        {
            if (errors == null || errors.Count == 0) return new[] { "QUICK_PLAY_070_OPERATION_FAILED" };
            var copy = new string[errors.Count];
            for (var index = 0; index < errors.Count; index++) copy[index] = errors[index];
            return copy;
        }
    }
}
