using System; using System.Collections.Generic;
namespace SecondDimension.Presentation.Campaign021
{
 public interface ICampaignPresentationCoordinator021 { CampaignPresentationState021 CampaignPresentation021{get;} }
 public sealed class CampaignPresentationState021 { public bool IsAvailable; public string Error; public string ActiveWorldId; public string ActiveWorldName; public string ActiveChapterId; public string ActiveChapterTitle; public string BackgroundResource; public string FrameResource; public string AmbientAudioCueId; public string EnemyPackId; public string BossId; public string LootProfileId; public int WorldThemes; public int EnemyProfiles; public int BossProfiles; public int RecruitProfiles; public int LootIcons; public int MaterialIcons; public int AudioCues; public IReadOnlyList<WorldPresentationSummary021> Worlds=Array.Empty<WorldPresentationSummary021>(); }
 public sealed class WorldPresentationSummary021 { public string WorldId; public string DisplayName; public string ProductionStatus; public int EnemyCount; public int RecruitCount; public int LootCount; public int ChapterCount; public string MaterialLanguage; }
}
