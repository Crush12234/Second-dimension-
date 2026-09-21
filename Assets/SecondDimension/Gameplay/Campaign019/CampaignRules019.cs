using System;
using System.Collections.Generic;

namespace SecondDimension.Gameplay.Campaign019
{
    public sealed class CampaignArcRule019
    {
        public string ArcId; public string WorldId; public string[] ChapterIds; public string[] UnlockGates; public bool RequiresOwnerApproval;
    }
    public sealed class CampaignChapterRule019
    {
        public string ChapterId; public string ArcId; public string WorldId; public string[] MapIds; public string SiegeId; public string PrimaryObjective; public int GuildXp; public int HallXp; public int Materials; public bool BattleRequired; public int EnemyUnionCount; public string BattleId; public string[] StoryGates;
    }
    public interface ICampaignRuleCatalog019
    {
        bool TryGetArc(string arcId,out CampaignArcRule019 arc);
        bool TryGetChapter(string chapterId,out CampaignChapterRule019 chapter);
        string LastChapterId(string arcId);
    }
}
