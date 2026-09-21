using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Presentation.PeopleBonds026;
using SecondDimension.Presentation.RecruitChronicles025;
using UnityEngine;

namespace SecondDimension.Presentation.Release029
{
    [Serializable]
    public sealed class FinalPeopleCreatorManifest029
    {
        public string contentVersion; public string title; public int saveFormatVersion; public int signatureRecruitProfiles; public int personalQuestBoards;
        public int personalQuestNodes; public int freeHallEvents; public int linkArts; public int creatorCodes; public int creatorRooms; public int creatorInvitations;
        public string[] hardLaws=Array.Empty<string>();
    }
    public sealed class PeopleCreatorHealthSnapshot029
    {
        public bool IsReady; public string Error=string.Empty; public FinalPeopleCreatorManifest029 Manifest;
        public IReadOnlyList<string> SummaryLines=Array.Empty<string>();
    }
    public static class PeopleCreatorReadinessService029
    {
        const string ManifestPath="SecondDimension/Release029/Data/FinalPeopleCreatorManifest029";
        public static PeopleCreatorHealthSnapshot029 BuildSnapshot()
        {
            try
            {
                var asset=Resources.Load<TextAsset>(ManifestPath); if(asset==null)throw new InvalidOperationException("Missing Final People + Creator 029 manifest.");
                var manifest=JsonConvert.DeserializeObject<FinalPeopleCreatorManifest029>(asset.text); if(manifest==null)throw new InvalidOperationException("Invalid Final People + Creator 029 manifest.");
                var chronicles=RecruitChronicleRegistry025.LoadFromResources(); var bonds=PeopleBondRegistry026.Load(); var creator=CreatorRegistry028.Load();
                Check(chronicles.Profiles.Count,manifest.signatureRecruitProfiles,"signature profiles"); Check(chronicles.Boards.Count,manifest.personalQuestBoards,"personal quest boards");
                var nodes=0;foreach(var board in chronicles.Boards.Values)nodes+=board.nodes?.Length??0;Check(nodes,manifest.personalQuestNodes,"personal quest nodes");
                Check(chronicles.HallEvents.Count,manifest.freeHallEvents,"free Hall scenes");Check(bonds.LinkArts.Count,manifest.linkArts,"Link Arts");
                Check(creator.CodeCount,manifest.creatorCodes,"creator codes");Check(creator.AllRooms.Count,manifest.creatorRooms,"creator rooms");Check(creator.InvitationCount,manifest.creatorInvitations,"creator invitations");
                if(manifest.saveFormatVersion!=SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion)throw new InvalidOperationException("People/Creator manifest save format differs from active save format.");
                var lines=new List<string>{"People/Creator 029 ready","300 Chronicle profiles • 360 personal quests • 3,600 nodes","112 free Hall scenes • 36 complete-Forecast Link Arts","300 Creator Codes • 133 Creator Rooms • save v"+manifest.saveFormatVersion};
                return new PeopleCreatorHealthSnapshot029{IsReady=true,Manifest=manifest,SummaryLines=lines.AsReadOnly()};
            }
            catch(Exception e){return new PeopleCreatorHealthSnapshot029{IsReady=false,Error=e.Message};}
        }
        static void Check(int actual,int expected,string label){if(actual!=expected)throw new InvalidOperationException(label+" mismatch: "+actual+" != "+expected);}
    }
}
