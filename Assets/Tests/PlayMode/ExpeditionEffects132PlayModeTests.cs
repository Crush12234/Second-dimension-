using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class ExpeditionEffects132PlayModeTests
    {
        GameObject _host,_screen,_events;
        M1FlowPresenter _presenter;
        [SetUp] public void Setup132()
        {
            _host=new GameObject("Effects132 test owner");_presenter=_host.AddComponent<M1FlowPresenter>();_presenter.enabled=false;
            _screen=new GameObject("Effects132 canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            _screen.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=_screen.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution=new Vector2(2796,1290);scale.matchWidthOrHeight=.5f;
            if(EventSystem.current==null)_events=new GameObject("Effects132 EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            typeof(M1FlowPresenter).GetField("_screenRoot",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(_presenter,_screen.GetComponent<RectTransform>());
        }
        [TearDown]public void Cleanup132()
        {
            UnityEngine.Object.DestroyImmediate(_host);UnityEngine.Object.DestroyImmediate(_screen);
            if(_events!=null)UnityEngine.Object.DestroyImmediate(_events);
        }
        void Build132(EffectCoordinator132 fake) => typeof(M1FlowPresenter)
            .GetMethod("BuildFullScreenWorldGateEvent110",BindingFlags.Instance|BindingFlags.NonPublic)
            .Invoke(_presenter,new object[]{fake,fake.CampaignWorldGate023});
        Button Button132(string name)=>_screen.GetComponentsInChildren<Button>(false).Single(x=>x.name==name);
        string Text132(string name)=>_screen.GetComponentsInChildren<Text>(false).Single(x=>x.name==name).text;
        static void Click132(Button button)
        {
            Assert.That(button.interactable,Is.True);
            var rect=button.GetComponent<RectTransform>();
            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(EventSystem.current){position=point};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject==button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform),Is.True,
                "A different visible surface intercepts the actual control.");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator ActualRollControlConcealsResultAndSkipOnlySettlesSavedD20ThenCollectsOnce132() =>
            ActualRollControl132(new EffectCoordinator132());

        [UnityTest]
        public IEnumerator PairedRouteSurfaceUsesCardIdForRollAndWorldGateIdForCollect132()
        {
            var fake=new EffectCoordinator132();
            fake.PendingExpeditionEffect132.SurfaceReceiptId="WORLD_GATE_PAIRED_RECEIPT132";
            fake.CampaignWorldGate023.PendingReceiptId="WORLD_GATE_PAIRED_RECEIPT132";
            Assert.That(fake.PendingExpeditionEffect132.ReceiptId,Is.Not.EqualTo(fake.CampaignWorldGate023.PendingReceiptId));
            return ActualRollControl132(fake);
        }

        IEnumerator ActualRollControl132(EffectCoordinator132 fake)
        {
            // Wiring fake deliberately stores a known outcome; authority tests
            // separately prove the real Roll/save/reload/apply transaction.
            Build132(fake);yield return null;yield return null;
            Assert.That(fake.Rolls,Is.Zero);Assert.That(fake.Applies,Is.Zero);
            Assert.That(Text132("Fate Event Reward 132"),Does.Not.Contain("Fixture Hero"));
            Assert.That(_screen.GetComponentsInChildren<Text>(false).Any(x=>x.name.StartsWith("D20 Face ")),Is.False);
            Assert.That(Button132("Committed Expedition Event Skip 110").interactable,Is.False);
            Click132(Button132("Committed Fate Action 132"));Assert.That(fake.Rolls,Is.EqualTo(1));
            // This focused fixture has no full IM1 coordinator; render the
            // changed saved view exactly as the real owner Changed rebuild does.
            Build132(fake);yield return null;yield return null;
            Assert.That(_screen.GetComponentsInChildren<Text>(true).Count(x=>x.name.StartsWith("D20 Face ")),Is.EqualTo(20));
            Assert.That(Text132("Saved Fate Face 132"),Is.Empty);
            Assert.That(Text132("Fate Event Reward 132"),Does.Not.Contain("Fixture Hero"));
            Assert.That(Button132("Committed Fate Action 132").interactable,Is.False);
            Click132(Button132("Committed Expedition Event Skip 110"));yield return null;yield return null;
            Assert.That(fake.Rolls,Is.EqualTo(1));Assert.That(fake.Applies,Is.Zero);
            Assert.That(Text132("Saved Fate Face 132"),Is.EqualTo("20"));
            Assert.That(Text132("Fate Event Reward 132"),Does.Contain("Fixture Hero"));
            Click132(Button132("Committed Fate Action 132"));
            Assert.That(fake.Applies,Is.EqualTo(1));
            Click132(Button132("Committed Fate Action 132"));Assert.That(fake.Applies,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator D20BlockingFirstFrameStillRequiresVisibleMotionBeforeCollect402() =>
            BlockedFirstFrame132(false);

        [UnityTest]
        public IEnumerator WheelBlockingFirstFrameStillRequiresVisibleMotionBeforeCollect402() =>
            BlockedFirstFrame132(true);

        IEnumerator BlockedFirstFrame132(bool wheel)
        {
            // Same honest wiring fake as the existing pointer test. The real
            // authority/save tests remain separate; here the stall reproduces
            // synchronous save/PNG work in the activation frame itself.
            var fake=new EffectCoordinator132();
            if(wheel)
            {
                fake.PendingExpeditionEffect132.Kind=ExpeditionDeckService089.Wheel132;
                fake.PendingExpeditionEffect132.Title="Fortune Wheel";
            }
            Build132(fake);yield return null;yield return null;
            Click132(Button132("Committed Fate Action 132"));
            Assert.That(fake.Rolls,Is.EqualTo(1));Build132(fake);
            var effect=_screen.GetComponentInChildren<CommittedFateEffect132>();
            var graphic=effect.GetComponentInChildren<FatePolyhedronGraphic132>();
            var bornFrame=Time.frameCount;
            Assert.That(effect.Settled132,Is.False);
            // Intentionally block longer than either complete effect. This is
            // a regression trigger, not a fabricated animation-clock advance.
            System.Threading.Thread.Sleep(3100);
            yield return null;yield return null;
            Assert.That(effect.Settled132,Is.False,"The stalled first frame must not skip the saved-result animation.");
            Assert.That(Text132("Saved Fate Face 132"),Is.Empty);
            Assert.That(Button132("Committed Fate Action 132").interactable,Is.False);
            Assert.That(fake.Applies,Is.Zero);
            Assert.That(_screen.GetComponentsInChildren<Transform>(false).Any(x=>
                x.name=="Fate Table Shadow 132" || x.name=="Moving Dice Shadow 132"),Is.False);
            if(!wheel)Assert.That(effect.GetComponentInChildren<FateSoftShadow132>(),Is.Not.Null);
            var previousAngle=graphic.Angle132;var previousRotation=graphic.Rotation132;
            var movingFrames=0;var deadline=Time.realtimeSinceStartup+20f;
            while(!effect.Settled132 && Time.realtimeSinceStartup<deadline)
            {
                Assert.That(Button132("Committed Fate Action 132").interactable,Is.False);
                yield return null;
                if(wheel ? Mathf.Abs(graphic.Angle132-previousAngle)>.01f :
                    Quaternion.Angle(graphic.Rotation132,previousRotation)>.01f)movingFrames++;
                previousAngle=graphic.Angle132;previousRotation=graphic.Rotation132;
            }
            Assert.That(effect.Settled132,Is.True,"The capped animation must still finish normally.");
            Assert.That(Time.frameCount-bornFrame,Is.GreaterThanOrEqualTo(wheel?84:63));
            Assert.That(movingFrames,Is.GreaterThanOrEqualTo(30),"Observe distinct actual mesh poses across rendered player frames.");
            Assert.That(fake.Rolls,Is.EqualTo(1));Assert.That(fake.Applies,Is.Zero);
            Assert.That(Button132("Committed Fate Action 132").interactable,Is.True);
            Assert.That(Text132("Saved Fate Face 132"),Is.EqualTo(wheel?"WIN!":"20"));
            Click132(Button132("Committed Fate Action 132"));
            Assert.That(fake.Applies,Is.EqualTo(1));
            Click132(Button132("Committed Fate Action 132"));Assert.That(fake.Applies,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NaturalD20MotionSettlesAfterRealFramesAndHideCancelsCallbacks132()
        {
            var rect=_screen.GetComponent<RectTransform>();var count=0;
            var effect=CommittedFateEffect132.Play132(rect,false,17,Color.cyan,false,()=>count++);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(effect.Settled132,Is.False);Assert.That(count,Is.Zero);
            Assert.That(effect.GetComponentsInChildren<Text>(false).Any(x=>x.name.StartsWith("D20 Face ") && x.enabled),Is.True);
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(effect.Settled132,Is.True);Assert.That(count,Is.EqualTo(1));
            var die=effect.GetComponentInChildren<FatePolyhedronGraphic132>();
            Assert.That(die.FaceFacing132(16),Is.GreaterThan(.999f),"The saved17 is the actual forward-facing icosahedron face.");
            var landed=effect.GetComponentsInChildren<Text>().Single(x=>x.name=="D20 Face 17");
            Assert.That(landed.enabled,Is.True);Assert.That(landed.text,Is.EqualTo("17"));
            Assert.That(landed.rectTransform.anchoredPosition.magnitude,Is.LessThan(.05f));
            var readout=effect.GetComponentsInChildren<Text>().Single(x=>x.name=="Saved Fate Face 132");
            Assert.That(readout.text,Is.EqualTo("17"));Assert.That(readout.rectTransform.anchorMax.y,Is.LessThan(.15f));
            Assert.That(effect.GetComponentInChildren<FateSoftShadow132>(),Is.Not.Null);
            // All twenty supplied outcomes have an actual front-face orientation;
            // only17 above is played through the full real-time motion.
            for(var result=1;result<=20;result++)
            {
                die.Rotation132=FatePolyhedronGraphic132.ResultFacingRotation132(result);
                Assert.That(die.FaceFacing132(result-1),Is.GreaterThan(.999f));
            }
            die.Rotation132=FatePolyhedronGraphic132.ResultFacingRotation132(17);
            effect.Settle132();Assert.That(count,Is.EqualTo(1));
            var cancelled=CommittedFateEffect132.Play132(rect,false,20,Color.yellow,false,()=>count++);
            cancelled.gameObject.SetActive(false);cancelled.Settle132();Assert.That(count,Is.EqualTo(1));
        }

        [Test]
        public void CommittedGlassRequiresReceiptAndContinueIsOnceWhileCancelNeverApplies132()
        {
            var rect=_screen.GetComponent<RectTransform>();var count=0;
            Assert.Throws<ArgumentException>(()=>CommittedGlassShatter132.Play132(rect,"","Story","Text",false,true,()=>count++));
            var effect=CommittedGlassShatter132.Play132(rect,"STEP132","Story","Text",true,true,()=>count++);
            Assert.That(count,Is.Zero,"Reduced motion never applies before owner cancellation can bind.");
            effect.Skip132();effect.Skip132();Assert.That(count,Is.EqualTo(1));
            var cancelled=CommittedGlassShatter132.Play132(rect,"STEP132B","Story","Text",false,false,()=>count++);
            cancelled.Cancel132();cancelled.Skip132();Assert.That(count,Is.EqualTo(1));
        }

        sealed class EffectCoordinator132 : ICampaignWorldGatePresentationCoordinator023,IExpeditionEffectCoordinator132
        {
            public int Rolls,Applies;
            public string[] MysteryEffectCardIds132=>Array.Empty<string>();
            public ExpeditionEffectView132 PendingExpeditionEffect132 {get;}=new ExpeditionEffectView132{
                ReceiptId="FATE_RECEIPT132",Kind=ExpeditionDeckService089.BoonD20132,Title="Wayglass Blessing",
                Rolled=false,D20=0,WheelSector=-1,Result="",Reward=""};
            public CampaignWorldGatePresentationState023 CampaignWorldGate023 {get;}=new CampaignWorldGatePresentationState023{
                IsAvailable=true,ActiveOperationId="FATE_OP132",ActiveOperationKind="CONTRACT",ActiveBoardTitle="The Glass Road",
                PendingReceiptId="FATE_RECEIPT132",CurrentNode=new NodeView023{NodeId="NODE132",Title="Mystery",Description="A road."}};
            public M1CommandResult RollCommittedExpeditionEffect132(string id)
            {
                if(id!=PendingExpeditionEffect132.ReceiptId || PendingExpeditionEffect132.Rolled)return M1CommandResult.Failure("Already rolled");
                Rolls++;PendingExpeditionEffect132.Rolled=true;
                if(PendingExpeditionEffect132.Kind==ExpeditionDeckService089.Wheel132)
                {
                    PendingExpeditionEffect132.WheelSector=2;
                    PendingExpeditionEffect132.Result="GEAR WON";
                    PendingExpeditionEffect132.Reward="Fixture Gear";
                }
                else
                {
                    PendingExpeditionEffect132.D20=20;
                    PendingExpeditionEffect132.Result="NATURAL 20 • PERMANENT BOON";
                    PendingExpeditionEffect132.Reward="Fixture Hero gains +1 permanent Expedition checks.";
                }
                return M1CommandResult.Success();
            }
            public M1CommandResult ApplyWorldGateReceipt023(){Applies++;CampaignWorldGate023.PendingReceiptId="";return M1CommandResult.Success();}
            M1CommandResult Unexpected()=>throw new InvalidOperationException("Unrelated command dispatched.");
            public M1CommandResult BeginWorldGateOperation023(string id)=>Unexpected();
            public M1CommandResult CommitWorldGateChoice023(string id)=>Unexpected();
            public M1CommandResult CommitAutomaticWorldGateRoom023()=>Unexpected();
            public M1CommandResult EnterWorldGateBattle023()=>Unexpected();
            public M1CommandResult FinalizeWorldGateOperation023()=>Unexpected();
            public M1CommandResult TravelWorldGate023(string id)=>Unexpected();
            public M1CommandResult RecoverLegacyWorldGateQuest023()=>Unexpected();
        }
    }
}
