using System;
using System.Linq;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.FirstHour072;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Narrow application seam for the rebuilt first-hour route. It reuses the existing
    /// deterministic commands while deliberately avoiding SaveAndReloadProof, whose
    /// Release 071 compatibility behavior injects four unseen roster members.
    /// </summary>
    public sealed partial class M1RuntimeCoordinator : IFirstHourOpeningCoordinator072
    {
        /// <summary>
        /// Boot enables this only for the explicit 072 route. It prevents the Release
        /// 071 resume compatibility migration from adding unseen recruits after the
        /// player has visibly formed the six-founder company.
        /// </summary>
        public static bool SuppressAutomaticFirstHourRosterMigration072 { get; set; }

        public const string FirstHourHallBreachBattleId072 =
            FirstHourOpeningState072.HallBreachBattleId;

        public string FirstHourCampaignGuid072 => _campaign?.CampaignGuid ?? string.Empty;

        public M1CommandResult CompleteFirstHourFoundingCompany072()
        {
            if (_campaign == null)
                return M1CommandResult.Failure("Create the Guild before confirming its founding Unions.");
            return ApplyAndPersist(
                _commands.CompleteOpening(_campaign),
                notify: true,
                "The six founders and their two Unions are saved. The Hall bell is sounding below you.");
        }

        public M1CommandResult StartFirstHourHallBreach072()
        {
            if (_campaign == null || _combatContent == null)
                return M1CommandResult.Failure("Hall Breach battle authority is unavailable.");

            var alliedUnionIds = _campaign.Guild.Unions
                .Where(value => value != null && value.Kind == UnionKind.Normal &&
                                value.MemberRecruitIds.Count > 0)
                .Take(2)
                .Select(value => value.UnionId)
                .ToArray();
            if (alliedUnionIds.Length != 2)
                return M1CommandResult.Failure(
                    "Confirm two active founding Unions before entering the Hall Breach.");

            var campaignForBattle = _campaign;
            var previousBattle = campaignForBattle.Battle;
            if (previousBattle != null &&
                StringComparer.Ordinal.Equals(
                    previousBattle.BattleId,
                    FirstHourHallBreachBattleId072) &&
                previousBattle.Outcome != SecondDimension.Gameplay.M2.BattleOutcome.InProgress &&
                !StringComparer.OrdinalIgnoreCase.Equals(previousBattle.Outcome.ToString(), "Victory"))
            {
                // A failed opening attempt is deliberately discarded. Victory rewards,
                // equipment, XP, and mastery are never claimable or farmable on retry.
                campaignForBattle = campaignForBattle.WithBattle(null);
            }

            var started = _battleCommands.StartEncounterBattle(
                campaignForBattle,
                _combatContent,
                FirstHourHallBreachBattleId072,
                "Protect the Guild Hall while Kael contains the breach.",
                enemyUnionCount: 1,
                routeModifiers: Array.Empty<string>(),
                alliedUnionIds: alliedUnionIds,
                guaranteeOpeningBreakthrough: true);
            return ApplyAndPersist(
                started,
                notify: true,
                "Hall Breach committed. Choose one complete forecast for each founding Union.");
        }
    }
}
