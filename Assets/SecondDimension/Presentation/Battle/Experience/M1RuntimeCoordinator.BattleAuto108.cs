using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator :
        IM2BattleAutoPlanCoordinator108,
        IM2BattleAutoRoundCoordinator108
    {
        public M1CommandResult SelectAutoForecastPlan108(
            IReadOnlyList<M2AutoForecastSelection108> selections)
        {
            var planned = BuildAutoPlan108(selections);
            if (!planned.IsSuccess)
                return M1CommandResult.Failure(FriendlyErrors(planned.Errors));

            return ApplyAndPersist(
                planned,
                notify: true,
                "Complete Auto Union plan selected and saved.");
        }

        public M1CommandResult ResolveAutoForecastPlan108(
            IReadOnlyList<M2AutoForecastSelection108> selections)
        {
            var planned = BuildAutoPlan108(selections);
            if (!planned.IsSuccess)
                return M1CommandResult.Failure(FriendlyErrors(planned.Errors));
            return ApplyAndPersist(
                _battleCommands.ConfirmRound(planned.Value, _combatContent),
                notify: true,
                "Complete Auto Union plan resolved and saved.");
        }

        private Result<CampaignState> BuildAutoPlan108(
            IReadOnlyList<M2AutoForecastSelection108> selections)
        {
            if (selections == null || selections.Count == 0)
                return Result<CampaignState>.Failure("A Union needs your command.");

            var candidate = _campaign;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var selection in selections)
            {
                if (selection == null || string.IsNullOrWhiteSpace(selection.UnionId) ||
                    string.IsNullOrWhiteSpace(selection.ForecastId) || !seen.Add(selection.UnionId))
                    return Result<CampaignState>.Failure("The complete Auto plan is invalid.");

                var selected = _battleCommands.SelectForecast(
                    candidate, selection.UnionId, selection.ForecastId);
                if (!selected.IsSuccess) return selected;
                candidate = selected.Value;
            }
            return Result<CampaignState>.Success(candidate);
        }
    }
}
