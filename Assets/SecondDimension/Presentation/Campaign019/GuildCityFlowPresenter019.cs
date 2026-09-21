using System;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void RunGuildCityCommand017D(Func<M1CommandResult> command)
        {
            ApplyGuildCity017D(command == null
                ? M1CommandResult.Failure("The Guild command is unavailable.")
                : command());
        }

        private void EnterCampaignBattle019(Campaign019.ICampaignPresentationCoordinator019 coordinator)
        {
            var result = coordinator?.EnterCampaignCertifiedBattle019();
            _localStatus = result?.Message ?? "The campaign battle command returned no result.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive) Navigate(M1Screen.Battle);
            else BuildCurrentScreen();
        }

        private void BuildGuildCityCampaign019(
            Transform body,
            Campaign019.ICampaignPresentationCoordinator019 coordinator,
            Campaign019.CampaignPresentationState019 state)
        {
            if (coordinator == null || state == null || !state.IsAvailable)
            {
                AddMessagePanel(body, "WORLD GATES CAMPAIGN",
                    state?.Error ?? "Campaign authority unavailable.", RuntimeUi.Warning);
                return;
            }

            var playableController020 = coordinator as Campaign020.ICampaignPlayablePresentationCoordinator020;
            if(playableController020?.CampaignPlayable020!=null&&
               !string.IsNullOrWhiteSpace(
                   playableController020.CampaignPlayable020.ActiveOperationId))return;

            AddResponsiveText062(body, "Story Quest List Heading", "MAIN CAMPAIGN",
                24, 36, 58f, RuntimeUi.Accent, FontStyle.Bold);
            if(state.CurrentCycle130>1||state.CanStartNextCycle130)
                AddResponsiveText062(body,"Story Cycle Progress 130",
                    "CYCLE "+state.CurrentCycle130+"  •  "+SecondDimension.Gameplay.Campaign020.CampaignMissionMap132.CompletedCount(state.Chapters.Where(value=>value.Completed).Select(value=>value.ChapterId))+" / 81 MISSIONS",
                    24,32,54f,RuntimeUi.Text,FontStyle.Bold);
            if(state.CanStartNextCycle130&&coordinator is Campaign019.ICampaignReplayPresentationCoordinator130 replay130)
            {
                var cycle130=state.CurrentCycle130;
                var growth130=state.ReplayGrowthPercent130;
                var next130=AddMessagePanel(body,"THIS STORY CYCLE IS COMPLETE",
                    "Play all story quests again with enemies "+growth130+"% stronger each cycle. Your heroes, equipment and earned rewards stay with you.",RuntimeUi.Accent);
                RuntimeUi.AddButton(next130,"Start Next Story Cycle 130","START CYCLE "+(cycle130+1),
                    ()=>{
                        if(next130==null||!next130.gameObject.activeInHierarchy||
                           !ReferenceEquals(_coordinator,coordinator))return;
                        RunGuildCityCommand017D(()=>replay130.StartNextCampaignCycle130(cycle130,growth130));
                    },122f,RuntimeUi.Accent);
            }
            DrawCampaignRunRestart151(body,coordinator,state.CurrentCycle130);
            var chapters=state.Chapters
                .Where(value=>value.Status=="AVAILABLE"||value.Active)
                .OrderBy(value=>value.Active?0:1)
                .ThenBy(value=>value.Title,StringComparer.Ordinal)
                .Take(3).ToArray();
            foreach(var chapter in chapters)
            {
                var chapterPanel=AddMessagePanel(body,
                    SecondDimension.Gameplay.Campaign020.CampaignMissionMap132.Label(chapter.ChapterId)+"  •  "+
                    chapter.Title.ToUpperInvariant(),
                    chapter.Region+
                    "\nSTORY  •  "+chapter.Briefing+
                    "\nOBJECTIVE  •  "+chapter.Objective+
                    "\nWHY IT MATTERS  •  "+chapter.CityConsequence+
                    "\nGUIDE  •  "+chapter.GuideTip,
                    chapter.Active?RuntimeUi.Warning:RuntimeUi.Text);
                var chapterId=chapter.ChapterId;
                if(playableController020!=null)
                    RuntimeUi.AddButton(chapterPanel,
                        "Start compact playable campaign chapter "+chapterId+" 084",
                        chapter.Active ? ((coordinator as Campaign020.ICampaignRecoveryCoordinator150)?.CampaignPaused150 == true ? "RESUME CAMPAIGN" : "CONTINUE THIS STORY QUEST") : "START THIS STORY QUEST",
                        ()=>StartCampaignQuest131(playableController020, chapterId),
                        122f,RuntimeUi.Accent);
                else if(chapter.Status=="AVAILABLE")
                    RuntimeUi.AddButton(chapterPanel,
                        "Start compact campaign chapter "+chapterId+" 084",
                        "START THIS STORY QUEST",
                        ()=>RunGuildCityCommand017D(
                            ()=>coordinator.StartChapter019(chapterId)),
                        108f,RuntimeUi.Accent);
                // Story disclosure must retain its full wrapped height at the
                // selected text size, with the Start action placed below it.
                foreach(var text in chapterPanel.GetComponentsInChildren<UnityEngine.UI.Text>())
                {
                    if(text.transform.parent!=chapterPanel)continue;
                    var layout=text.GetComponent<UnityEngine.UI.LayoutElement>();
                    if(layout!=null)layout.preferredHeight=-1f;
                    text.resizeTextForBestFit=false;
                }
                UseContentDrivenBoardPanelHeight084(chapterPanel);
            }
            if(chapters.Length==0&&!state.CanStartNextCycle130)
                AddMessagePanel(body,"NO NEW QUEST READY",
                    "Finish the current Guild operation or claim its saved result to unlock the next story quest.",
                    RuntimeUi.ButtonNormal);
            if (state.AwaitingBattleRewardClaim)
                AddMessagePanel(body, "BATTLE RESULT AWAITS CLAIM",
                    "Open Battle Results and manually claim the existing deterministic equipment reward. The campaign receipt will then be committed automatically without creating a second item.",
                    RuntimeUi.Warning);
        }
    }
}
