using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;

namespace SecondDimension.Presentation.Campaign020
{
    public interface ICampaignRecoveryCoordinator150
    {
        M1CommandResult PauseCampaign150();
        bool CampaignPaused150 {get;}
    }
}
namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign020.ICampaignRecoveryCoordinator150
    {
        public bool CampaignPaused150 => _campaign?.Recovery150?.Paused == true;
        public M1CommandResult PauseCampaign150()
        {
            if(TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            Registry023();
            var result=CampaignRecoveryCommands150.Pause(_campaign,_campaignRules023);
            if(result.IsSuccess && ReferenceEquals(result.Value,_campaign)) return M1CommandResult.Success("Campaign checkpoint retained.");
            return ApplyAndPersist(result,true,"Campaign paused. Your route is saved; improve your guild and return to this quest.");
        }
    }
}
