using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
namespace SecondDimension.Presentation.Campaign019
{
    public interface ICampaignRunRecoveryCoordinator151
    {
        bool CanRestartCampaign151 {get;}
        string CampaignRestartRevision151 {get;}
        M1CommandResult RestartCampaignRun151(string expectedRevision,bool confirmed);
    }
}
namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign019.ICampaignRunRecoveryCoordinator151
    {
        public bool CanRestartCampaign151=>!TowerWriteBusy116()&&CampaignRunRecoveryCommands151.IsEligible(_campaign);
        public string CampaignRestartRevision151=>CanonicalJson.Sha256Hex(_campaign);
        public M1CommandResult RestartCampaignRun151(string expectedRevision,bool confirmed)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            Registry023();
            var result=CampaignRunRecoveryCommands151.Restart(_campaign,_campaignRules023,expectedRevision,confirmed);
            if(result.IsSuccess&&ReferenceEquals(result.Value,_campaign))return M1CommandResult.Success("Campaign checkpoint retained.");
            return ApplyAndPersist(result,true,"Campaign run restarted at Quest 1 in the same cycle. Your earned progress is retained.");
        }
    }
}
