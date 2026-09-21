using System;
using System.Linq;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.RecruitChronicles025
{
    public static class RecruitChronicleSynchronizer025
    {
        public static CampaignPlayableState020 Synchronize(CampaignState campaign,CampaignPlayableState020 playable)
        {
            if(playable==null)playable=CampaignPlayableState020.Default();var chronicles=playable.RecruitChronicles025??RecruitChronicleState025.Default();var bonds=playable.PeopleBonds026??PeopleBonds026.PeopleBondState026.Default();
            // Synchronization is intentionally additive. Signed members are never deleted when a content profile is unavailable.
            if(campaign?.Guild?.Recruits==null)return playable.With(recruitChronicles025:chronicles,replaceRecruitChronicles025:true,peopleBonds026:bonds,replacePeopleBonds026:true);
            var valid=new System.Collections.Generic.HashSet<string>(campaign.Guild.Recruits.Where(x=>x.AuthorityKind==RecruitAuthorityKind.Normal).Select(x=>x.RecruitId),StringComparer.Ordinal);
            var quests=chronicles.PersonalQuests.Where(x=>valid.Contains(x.RecruitId)).ToList();
            if(quests.Count!=chronicles.PersonalQuests.Count)chronicles=chronicles.With(personalQuests:quests.AsReadOnly(),lastCheckpointId:"chronicles_synchronized");
            return playable.With(recruitChronicles025:chronicles,replaceRecruitChronicles025:true,peopleBonds026:bonds,replacePeopleBonds026:true);
        }
    }
}
