using System;
using System.Linq;
using SecondDimension.Gameplay.SSSTenV4;

namespace SecondDimension.Presentation
{
    public interface ISssTenV4PresentationCoordinator090
    {
        M1CommandResult AscendSssHero090(string heroId);
    }

    public sealed partial class M1RuntimeCoordinator
    {
        public M1CommandResult AscendSssHero090(string heroId)
        {
            try
            {
                if (_campaign?.Guild == null)
                    return M1CommandResult.Failure("No active Guild campaign.");
                var recruit = SssTenV4Roster090.FindOwned(
                    _campaign.Guild.Recruits, heroId);
                if (recruit == null)
                    return M1CommandResult.Failure("Recruit this SSS hero first.");
                if (recruit.Progression.AscensionLevel >=
                    SecondDimension.Gameplay.State.RecruitAscensionRules089.MaximumLevel)
                    return M1CommandResult.Failure(
                        "A10 reached. No Ascension Credit was consumed.");
                var preview = SssTenV4HostRewards090.PreviewAscension(
                    _campaign, heroId);
                var nextRank = Math.Min(
                    SecondDimension.Gameplay.State.RecruitAscensionRules089.MaximumLevel,
                    recruit.Progression.AscensionLevel + 1);
                var requestId = _campaign.CampaignGuid + ":" +
                                SssTenV4Roster090.Get(heroId).HeroId +
                                ":A" + nextRank;
                return ApplyAndPersist(
                    SssTenV4HostRewards090.Ascend(
                        _campaign,
                        heroId,
                        requestId,
                        preview.CurrentRank,
                        preview.CreditCount,
                        preview.StateGuard),
                    true,
                    "Ascension complete: " + recruit.DisplayName +
                    " is now A" + nextRank +
                    ". Stats increased; Arts and mastery were preserved.");
            }
            catch (Exception exception)
            {
                return M1CommandResult.Failure(exception.Message);
            }
        }
    }
}
