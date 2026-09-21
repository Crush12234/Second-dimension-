using System;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Presentation.Campaign022
{
    public interface ITowerBattleStartCoordinator110
    {
        M1CommandResult StartTowerBattle110();
    }
}

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign022.ITowerBattleStartCoordinator110
    {
        // The optional async transaction bundle supplies the shared path lease
        // check. Without that bundle this synchronous path uses the normal store.
        partial void CheckTowerBattleStartLease110(ref M1CommandResult blocked);

        public M1CommandResult StartTowerBattle110()
        {
            M1CommandResult blocked = null;
            CheckTowerBattleStartLease110(ref blocked);
            if (blocked != null) return blocked;
            using var timing = BeginTowerTiming110("manual-start-transaction");
            var source = _campaign;
            var city = source?.Guild?.GuildCity;
            if (city == null) return M1CommandResult.Failure("The Tower is unavailable.");
            if (source.Battle != null &&
                (source.Battle.Phase != BattlePhase.Resolved || source.Battle.Outcome == BattleOutcome.InProgress))
                return M1CommandResult.Failure("Finish the active battle before starting another Tower floor.");
            if (source.Battle?.Reward != null && !source.Battle.Reward.Claimed)
                return M1CommandResult.Failure("Claim the battle reward before starting another Tower floor.");
            if (city.PendingBattleReturn != null)
                return M1CommandResult.Failure("Finish banking the current battle before starting another Tower floor.");

            var registry = Registry022();
            var candidate = source;
            var active = ProgressionState081(candidate).ActiveAbyssOperation;
            if (active == null)
            {
                var begun = _campaignCommands022.BeginTowerFloor094(candidate, registry);
                timing?.Mark("begin-floor-authority");
                if (!begun.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(begun.Errors));
                candidate = begun.Value;
                active = ProgressionState081(candidate).ActiveAbyssOperation;
            }

            if (active.Status == AbyssOperationStatus022.Active)
            {
                // The shared boundary helper also banks victories. This entry
                // accepts only the prefix before a battle, never its aftermath.
                if (!registry.AbyssOperations.TryGetValue(active.OperationDefinitionId, out var operation))
                    return M1CommandResult.Failure("CAMPAIGN022_ABYSS_OPERATION_UNKNOWN");
                if (active.CurrentStepIndex < 0 || active.CurrentStepIndex >= operation.steps.Length)
                    return M1CommandResult.Failure("CAMPAIGN022_ABYSS_STEP_RANGE");
                if (!string.IsNullOrWhiteSpace(active.ExistingBattleRewardReceiptId))
                    return M1CommandResult.Failure("Bank the current Tower victory before starting another floor.");
                for (var index = 0; index < active.CurrentStepIndex; index++)
                    if (operation.steps[index].requiresBattle)
                        return M1CommandResult.Failure("Bank the current Tower victory before starting another floor.");

                var prepared = AdvanceTowerToBattleBoundary108(candidate, registry);
                timing?.Mark("prepare-floor-authorities");
                if (!prepared.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(prepared.Errors));
                candidate = prepared.Value;
                active = ProgressionState081(candidate).ActiveAbyssOperation;
                if (active == null || active.Status != AbyssOperationStatus022.Active)
                    return M1CommandResult.Failure("The Tower floor is not ready for battle.");
            }
            else if (active.Status != AbyssOperationStatus022.AwaitingBattle ||
                     active.PendingReceipt != null || city.PendingEncounter == null)
                return M1CommandResult.Failure("Bank the current Tower victory before starting another floor.");

            // An AwaitingBattle save may contain a committed encounter but still
            // retain the prior claimed battle. Revalidate that exact request;
            // do not feed it into the result/banking half of the boundary helper.
            var encounter = _campaignCommands022.CommitAbyssBattleEncounter(candidate, registry);
            timing?.Mark("commit-or-validate-encounter");
            if (!encounter.IsSuccess) return M1CommandResult.Failure(FriendlyErrors(encounter.Errors));
            var started = _guildCityBattleBridge.StartCertifiedEncounter(
                encounter.Value, _battleCommands, _combatContent, _encounterRosterResolver070);
            timing?.Mark("initialize-battle");
            // Preparation neither claims combat rewards nor grants recruits.
            // Keep the standard final preparation/validation and publish only
            // after the one durable save contains both encounter and battle.
            return ApplyGuildCityAndPersist(started, "Tower battle started.");
        }
    }
}
