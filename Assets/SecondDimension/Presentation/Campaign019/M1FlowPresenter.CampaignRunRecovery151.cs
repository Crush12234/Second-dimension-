using UnityEngine;
namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        void DrawCampaignRunRestart151(Transform body,Campaign019.ICampaignPresentationCoordinator019 coordinator,int cycle)
        {
            if(!(coordinator is Campaign019.ICampaignRunRecoveryCoordinator151 recovery)||!recovery.CanRestartCampaign151)return;
            var panel=AddMessagePanel(body,"CAMPAIGN RESTART AVAILABLE",
                "Ten defeats on this quest are settled. You may keep trying, train in Tower, or restart this cycle from Quest 1. Restarting does not lower difficulty.",RuntimeUi.Warning);
            RuntimeUi.SetLayout(panel,preferredHeight:panel.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight+112f);
            RuntimeUi.AddButton(panel,"Review Campaign Run Restart 151","RESTART CAMPAIGN RUN",
                ()=>{
                    if(panel==null||!panel.gameObject.activeInHierarchy||!ReferenceEquals(_coordinator,coordinator))return;
                    var revision=recovery.CampaignRestartRevision151;
                    ShowConfirmation("RESTART CYCLE "+cycle+" FROM QUEST 1?",
                        "Replace this run's quest position. Keep heroes, XP, equipment, ascension, Arts, town, permanent rewards and Tower progress. Difficulty stays the same. Tower is available for earlier battles.",
                        "CONFIRM RESTART",()=>{
                            if(!ReferenceEquals(_coordinator,coordinator))return;
                            RunGuildCityCommand017D(()=>recovery.RestartCampaignRun151(revision,true));
                        },confirmColor:RuntimeUi.Warning);
                },104f,RuntimeUi.ButtonNormal);
        }
    }
}
