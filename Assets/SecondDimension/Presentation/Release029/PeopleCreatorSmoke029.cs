using UnityEngine;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Presentation.PeopleBonds026;
using SecondDimension.Presentation.RecruitChronicles025;

namespace SecondDimension.Presentation.Release029
{
    public sealed class PeopleCreatorSmoke029 : MonoBehaviour
    {
        void Start()
        {
            var readiness=PeopleCreatorReadinessService029.BuildSnapshot();
            var chronicles=RecruitChronicleRegistry025.LoadFromResources();
            var bonds=PeopleBondRegistry026.Load();
            var creator=CreatorRegistry028.Load();
            Debug.Log("029 SMOKE — ready="+readiness.IsReady+" profiles="+chronicles.Profiles.Count+" boards="+chronicles.Boards.Count+" linkArts="+bonds.LinkArts.Count+" codes="+creator.CodeCount+" rooms="+creator.RoomCount);
        }
    }
}
