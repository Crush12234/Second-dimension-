using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign019;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class CampaignReplayPresentation130Tests
    {
        [Test]
        public void NextCycleNeedsActualButtonAndClosedOrChangedOwnerCannotDispatch130()
        {
            var host=new GameObject("Campaign Replay Presenter 130");
            var canvasObject=new GameObject("Campaign Replay Canvas 130",typeof(RectTransform),typeof(Canvas));
            var screen=new GameObject("Replay Screen 130",typeof(RectTransform));
            screen.transform.SetParent(canvasObject.transform,false);
            var body=new GameObject("Replay Story List 130",typeof(RectTransform));
            body.transform.SetParent(screen.transform,false);
            var presenter=host.AddComponent<M1FlowPresenter>();
            var owner=new Coordinator130();
            Set(presenter,"_canvas",canvasObject.GetComponent<Canvas>());
            Set(presenter,"_screenRoot",screen.GetComponent<RectTransform>());
            Set(presenter,"_coordinator",owner);
            Set(presenter,"_screen",(M1Screen)(-1));
            try
            {
                typeof(M1FlowPresenter).GetMethod("BuildGuildCityCampaign019",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(presenter,new object[]{body.transform,owner,owner.Campaign019});
                var button=body.GetComponentsInChildren<Button>(true).Single(value=>value.name=="Start Next Story Cycle 130");
                Assert.That(button.GetComponentInChildren<Text>().text,Is.EqualTo("START CYCLE 2"));
                Assert.That(owner.Calls,Is.Zero,"Building a completion card never starts the replay.");
                body.SetActive(false);
                button.onClick.Invoke();
                Assert.That(owner.Calls,Is.Zero,"Retained listener from a closed view is inert.");
                body.SetActive(true);
                Set(presenter,"_coordinator",new Coordinator130());
                button.onClick.Invoke();
                Assert.That(owner.Calls,Is.Zero,"An old view never writes to its detached owner.");
                Set(presenter,"_coordinator",owner);
                button.onClick.Invoke();
                Assert.That(owner.Calls,Is.EqualTo(1));
                Assert.That(owner.ExpectedCycle,Is.EqualTo(1));
                Assert.That(owner.Growth,Is.EqualTo(25));
                Assert.That((string)Get(presenter,"_localStatus"),Is.EqualTo("Synthetic cycle save failed; retry remains deliberate."));
                owner.Fail=false;
                button.onClick.Invoke();
                Assert.That(owner.Calls,Is.EqualTo(2),"Save failure never auto-retries.");
                Assert.That(owner.Committed,Is.EqualTo(1));
                button.onClick.Invoke();
                Assert.That(owner.Committed,Is.EqualTo(1),"The frozen expected-cycle value rejects duplicate commit.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        static object Get(object target,string name)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        sealed class Coordinator130:IM1PresentationCoordinator,ICampaignPresentationCoordinator019,ICampaignReplayPresentationCoordinator130
        {
            public event Action Changed{add{}remove{}}
            public M1PresentationState State{get;}=new M1PresentationState();
            public CampaignPresentationState019 Campaign019{get;}=new CampaignPresentationState019{
                IsAvailable=true,CanStartNextCycle130=true,CurrentCycle130=1,CycleCompleted130=82,CycleTotal130=82,ReplayGrowthPercent130=25};
            public int Calls,ExpectedCycle,Growth,Committed;
            public bool Fail=true;
            public M1CommandResult StartNextCampaignCycle130(int expectedCurrentCycle,int growthPercent=25)
            {
                Calls++;ExpectedCycle=expectedCurrentCycle;Growth=growthPercent;
                if(expectedCurrentCycle!=Campaign019.CurrentCycle130)return M1CommandResult.Failure("Cycle already changed.");
                if(Fail)return M1CommandResult.Failure("Synthetic cycle save failed; retry remains deliberate.");
                Committed++;Campaign019.CurrentCycle130++;Campaign019.CanStartNextCycle130=false;
                return M1CommandResult.Success("Synthetic next cycle saved.");
            }
            static M1CommandResult Unexpected()=>throw new InvalidOperationException("Unexpected command in isolated replay UI fixture.");
            public M1CommandResult RefreshCampaign019()=>Unexpected();
            public M1CommandResult StartChapter019(string id)=>Unexpected();
            public M1CommandResult EnterCampaignCertifiedBattle019()=>Unexpected();
            public M1CommandResult CommitCampaignNonCombat019(string outcome)=>Unexpected();
            public M1CommandResult ApplyCampaignReceipt019()=>Unexpected();
            public M1CommandResult CreateGuild(M1NewGuildIntent intent)=>Unexpected();
            public M1CommandResult SignRecruit(string id)=>Unexpected();
            public M1CommandResult EquipItem(string recruit,string slot,string item)=>Unexpected();
            public M1CommandResult UnequipItem(string recruit,string slot)=>Unexpected();
            public M1CommandResult SetEquipmentLock(string recruit,string slot,bool locked)=>Unexpected();
            public M1CommandResult CompleteEquipmentReview()=>Unexpected();
            public M1CommandResult AddUnion()=>Unexpected();
            public M1CommandResult RemoveUnion(int index)=>Unexpected();
            public M1CommandResult AssignRecruitToUnion(string recruit,int union,int slot)=>Unexpected();
            public M1CommandResult UnassignRecruitFromUnion(string recruit)=>Unexpected();
            public M1CommandResult SetUnionLeader(int union,string recruit)=>Unexpected();
            public M1CommandResult SetFormation(int union,string formation)=>Unexpected();
            public M1CommandResult SetDoctrine(int union,string doctrine)=>Unexpected();
            public M1CommandResult SaveAndReloadProof()=>Unexpected();
        }
    }
}
