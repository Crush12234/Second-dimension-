using System;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        private Result<CampaignState> ApplyAutomaticEarnedRecruitGrowth136(CampaignState candidate)
        {
            if (_guildCityRecruitment == null || candidate?.Guild == null ||
                GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(candidate)||
                // A run restart is a position-only transaction. Already-earned invitations
                // remain claimable through the normal explicit Guild reward action.
                SecondDimension.Gameplay.Campaign019.CampaignRunRecoveryCommands151.IsRestartReady(candidate))
                return Result<CampaignState>.Success(candidate);
            try
            {
                Registry019(); Registry023();
                return _guildCityRecruitment.ApplyAutomaticEarnedDuplicates136(candidate,
                    _campaignRules019, _campaignRules023);
            }
            catch (Exception e)
            {
                return Result<CampaignState>.Failure("RECRUIT_AUTO136_AUTHORITY_REQUIRED:" + e.Message);
            }
        }
    }
}
