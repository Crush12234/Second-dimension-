using SecondDimension.Core;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        private M1CommandResult ApplyEarnedRecruitmentBoard124(
            Result<CampaignState> result, string successMessage)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            if (_guildCityContent == null)
                return M1CommandResult.Failure("Guild/city authority is unavailable. " + _startupNotice);
            // A full board or an empty earned-lead queue is a real no-op: do not
            // rewrite the envelope, consume XP, publish Changed or run reward prep.
            if (result.IsSuccess && ReferenceEquals(result.Value, _campaign))
                return M1CommandResult.Success(
                    "No new earned contacts fit on this board. Current interviews remain available; continue your quests to earn more contacts.");
            return ApplyGuildCityAndPersist(result, successMessage);
        }
    }
}
