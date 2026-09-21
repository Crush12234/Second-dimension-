#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Presentation.PeopleBonds026;
using SecondDimension.Presentation.RecruitChronicles025;
using SecondDimension.Presentation.Release029;
using SecondDimension.Save;

namespace SecondDimension.Editor.Release029
{
    public static class FinalPeopleCreatorValidation029
    {
        [MenuItem("Second Dimension/Final People Creator 029/Validate")]
        public static void Validate()
        {
            SecondDimension.Editor.Creator028.CreatorCodesRoomsValidation028.Validate();
            var chronicles=RecruitChronicleRegistry025.LoadFromResources();
            var bonds=PeopleBondRegistry026.Load();
            var readiness=PeopleCreatorReadinessService029.BuildSnapshot();
            if(!readiness.IsReady) throw new InvalidOperationException(readiness.Error);
            if(chronicles.Profiles.Count!=300||chronicles.Boards.Count!=360||chronicles.Boards.Values.Sum(x=>x.nodes.Length)!=3600) throw new InvalidOperationException("Recruit Chronicle count gate failed.");
            if(chronicles.HallEvents.Count!=112||chronicles.MentorshipLessons.Count!=30||chronicles.Memories.Count!=48) throw new InvalidOperationException("People content gate failed.");
            if(bonds.Tiers.Count!=6||bonds.LinkArts.Count!=36||bonds.Doctrines.Count!=12||bonds.Techniques.Count!=30) throw new InvalidOperationException("People Bonds count gate failed.");
            if(SaveEnvelopeV1.CurrentFormatVersion!=11) throw new InvalidOperationException("029 requires save format 11.");
            Debug.Log("FINAL PEOPLE + CREATOR CONSOLIDATION 029 VALIDATION: PASS");
        }
        public static void ValidateFromCommandLine(){try{Validate();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
#endif
