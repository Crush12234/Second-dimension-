using System.Linq; using UnityEngine;
namespace SecondDimension.Presentation
{
 public sealed partial class M1FlowPresenter
 {
  void BuildGuildCityCampaign021(Transform body,Campaign021.ICampaignPresentationCoordinator021 coordinator,Campaign021.CampaignPresentationState021 state)
  {
   if(state==null||!state.IsAvailable){AddMessagePanel(body,"CAMPAIGN PRESENTATION 021",state?.Error??"Campaign presentation authority unavailable.",RuntimeUi.Warning);return;}
   AddMessagePanel(body,"WORLD IDENTITY & PRESENTATION 021","Active world: "+state.ActiveWorldName+(string.IsNullOrWhiteSpace(state.ActiveChapterTitle)?"":"\nActive chapter: "+state.ActiveChapterTitle)+"\nTheme/frame: "+state.BackgroundResource+" • "+state.FrameResource+"\nEnemy pack: "+state.EnemyPackId+(string.IsNullOrWhiteSpace(state.BossId)?"":" • Boss: "+state.BossId)+"\nLoot authority: "+state.LootProfileId,RuntimeUi.Accent);
   AddMessagePanel(body,"IMPLEMENTATION-READY ASSET COVERAGE","World themes "+state.WorldThemes+" • Enemy profiles "+state.EnemyProfiles+" • Boss profiles "+state.BossProfiles+" • Recruit origins "+state.RecruitProfiles+" • Loot icons "+state.LootIcons+" • Materials "+state.MaterialIcons+" • Audio cues "+state.AudioCues+"\nAll assets use stable IDs and remain replaceable without changing gameplay authority.",RuntimeUi.Positive);
   AddMessagePanel(body,"WORLD PRESENTATION PACKS",string.Join("\n",state.Worlds.Select(x=>x.DisplayName+" — chapters "+x.ChapterCount+", enemies "+x.EnemyCount+", recruit origins "+x.RecruitCount+", loot "+x.LootCount+" — "+x.MaterialLanguage)),RuntimeUi.ButtonNormal);
  }
 }
}
