using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Presentation;
using SecondDimension.Presentation.People029;
using SecondDimension.Presentation.RecruitChronicles025;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class RecruitChronicleBoard084PlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDownChronicleBoards084()
        {
            foreach(var presenter in UnityEngine.Object.FindObjectsByType<M1FlowPresenter>(
                        FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(presenter.name.StartsWith("Recruit Chronicle PlayMode Presenter 084",StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(presenter.gameObject);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                        FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.name.StartsWith("Recruit Chronicle PlayMode Canvas 084",StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActiveBoardFitsBothSupportedResolutionsAndPerformsSavedRoomReveal()
        {
            var registry=RecruitChronicleRegistry025.LoadFromResources();
            var profile=registry.Profiles.Values.First(value=>value.personalQuestBoardIds.Length==2);
            var board=registry.Boards[profile.personalQuestBoardIds[0]];
            var pausedBoard=registry.Boards[profile.personalQuestBoardIds[1]];
            var progress=new PersonalQuestProgressState025(board.boardId,board.recruitId,board.nodes[4].nodeId,
                board.nodes.Take(3).Select(value=>value.nodeId).ToArray(),Array.Empty<string>(),false,string.Empty,"playmode_branch");
            var pausedProgress=new PersonalQuestProgressState025(pausedBoard.boardId,pausedBoard.recruitId,
                pausedBoard.entryNodeId,Array.Empty<string>(),Array.Empty<string>(),false,string.Empty,"playmode_paused");
            var quest=RecruitChronicleBoardProjection084.Project(board,profile,progress);
            var pausedQuest=RecruitChronicleBoardProjection084.Project(pausedBoard,profile,pausedProgress);
            var state=new PeopleRuntimePresentationState029
            {
                IsAvailable=true,SignatureProfiles=300,PersonalQuestBoards=360,PersonalQuestNodes=3600,
                Quests=new[]{pausedQuest,quest},Recruits=Array.Empty<RecruitPeopleView029>(),
                AvailableHallScenes=Array.Empty<HallSceneView029>(),Bonds=Array.Empty<BondPairView029>(),
                Doctrines=Array.Empty<DoctrineView029>(),Unions=Array.Empty<UnionPeopleView029>()
            };
            var coordinator=new ChroniclePeopleCoordinator084(state);
            Assert.That(M1FlowPresenter.PersonalQuestRevealDuration029,Is.EqualTo(0.18f));

            foreach(var resolution in new[]{new Vector2(1280f,800f),new Vector2(1920f,1080f)})
            {
                var presenterObject=new GameObject("Recruit Chronicle PlayMode Presenter 084 "+resolution.x);
                var presenter=presenterObject.AddComponent<M1FlowPresenter>();
                var canvasObject=new GameObject("Recruit Chronicle PlayMode Canvas 084 "+resolution.x,
                    typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                var canvas=canvasObject.GetComponent<Canvas>();
                canvas.renderMode=RenderMode.WorldSpace;
                var canvasRect=canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta=resolution;
                var bodyObject=new GameObject("Recruit Chronicle PlayMode Body 084",typeof(RectTransform),typeof(VerticalLayoutGroup));
                var body=bodyObject.GetComponent<RectTransform>();
                body.SetParent(canvasRect,false);body.anchorMin=Vector2.zero;body.anchorMax=Vector2.one;
                body.offsetMin=Vector2.zero;body.offsetMax=Vector2.zero;
                var layout=bodyObject.GetComponent<VerticalLayoutGroup>();
                layout.padding=new RectOffset(0,0,0,0);layout.spacing=12f;layout.childControlWidth=true;
                layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
                var build=typeof(M1FlowPresenter).GetMethod("BuildPeopleRuntime029",
                    BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(build,Is.Not.Null);
                build.Invoke(presenter,new object[]{body,coordinator,state});
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(body);
                Canvas.ForceUpdateCanvases();

                var boardPanels=body.GetComponentsInChildren<RectTransform>(true).Where(value=>
                    value.parent==body&&StringComparer.Ordinal.Equals(
                        value.name,"Personal Quest Board "+board.boardId)).ToArray();
                Assert.That(boardPanels,Has.Length.EqualTo(1),resolution+" must render one active board only.");
                Assert.That(body.transform.GetChild(0).name,
                    Does.StartWith("People Compact Header ACTIVE PERSONAL CHRONICLE"));
                var visible=string.Join("\n",body.GetComponentsInChildren<Text>(true).Select(value=>value.text));
                Assert.That(visible,Does.Contain("1 legacy Chronicle is safely paused"));
                Assert.That(visible,Does.Contain("YOUR PAWN"));
                Assert.That(visible,Does.Contain("FACE DOWN"));
                Assert.That(visible,Does.Contain("CHOOSE LEFT PATH"));
                Assert.That(visible,Does.Contain("CHOOSE RIGHT PATH"));
                Assert.That(visible,Does.Not.Contain("DANGER ROOM"));
                Assert.That(visible,Does.Not.Contain("TURNING POINT"));
                Assert.That(visible,Does.Not.Contain(": Step "));
                Assert.That(visible,Does.Not.Contain(" step tests "));
                foreach(var raw in new[]{board.boardId,pausedBoard.boardId,profile.recruitId,profile.worldId}
                            .Concat(board.nodes.Select(value=>value.nodeId)))
                    Assert.That(visible.IndexOf(raw,StringComparison.OrdinalIgnoreCase),Is.EqualTo(-1),raw);

                var primary=body.GetComponentsInChildren<Button>(true).First(value=>
                    value.name.StartsWith("Personal Quest Move "+board.boardId,StringComparison.Ordinal));
                Assert.That(primary.interactable,Is.True);
                Assert.That(primary.GetComponent<LayoutElement>().minHeight,Is.GreaterThanOrEqualTo(64f));
                AssertInside(body,primary.GetComponent<RectTransform>(),resolution+" primary Chronicle action");

                var currentTile=body.GetComponentsInChildren<RectTransform>(true).Single(value=>
                    value.name.StartsWith("Personal Quest Tile ",StringComparison.Ordinal)&&
                    value.name.EndsWith(quest.CurrentNodeId,StringComparison.Ordinal));
                var group=currentTile.GetComponent<CanvasGroup>();
                Assert.That(group,Is.Not.Null);
                Assert.That(currentTile.localScale.x,Is.LessThan(0.95f),
                    "The current room must begin with the physical tile-flip settle.");
                Assert.That(group.alpha,Is.LessThan(1f));
                yield return new WaitForSecondsRealtime(M1FlowPresenter.PersonalQuestRevealDuration029+0.08f);
                Assert.That(currentTile.localScale.x,Is.EqualTo(1f).Within(0.001f));
                Assert.That(currentTile.localScale.y,Is.EqualTo(1f).Within(0.001f));
                Assert.That(currentTile.localScale.z,Is.EqualTo(1f).Within(0.001f));
                Assert.That(group.alpha,Is.EqualTo(1f).Within(0.001f));

                UnityEngine.Object.Destroy(presenterObject);
                UnityEngine.Object.Destroy(canvasObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator OrphanedLegacyBattleReturnExplainsSupportWithoutOfferingAFailingRepair()
        {
            var held=new PersonalQuestView029
            {
                BoardId="CHRONICLE_RAW_HELD_084",
                RecruitId="RECRUIT_RAW_HELD_084",
                RecruitDisplayName="Guild member",
                ChapterLabel="BATTLE HANDOFF RECOVERY",
                WorldName="Guild Archive",
                Started=true,
                NeedsRecovery=true,
                RecoveryRequiresSupport=true,
                RecoveryMessage="The return has lost its launch record, so it cannot be matched automatically."
            };
            var available=new PersonalQuestView029
            {
                BoardId="CHRONICLE_RAW_AVAILABLE_084",
                RecruitId="RECRUIT_RAW_AVAILABLE_084",
                RecruitDisplayName="Maren",
                ChapterLabel="CHAPTER I",
                WorldName="Skyhome",
                Theme="A bell calls beneath the gate.",
                Objective="Bring every worker home."
            };
            var state=new PeopleRuntimePresentationState029
            {
                IsAvailable=true,
                Quests=new[]{available,held},
                Recruits=Array.Empty<RecruitPeopleView029>(),
                AvailableHallScenes=Array.Empty<HallSceneView029>(),
                Bonds=Array.Empty<BondPairView029>(),
                Doctrines=Array.Empty<DoctrineView029>(),
                Unions=Array.Empty<UnionPeopleView029>()
            };

            foreach(var resolution in new[]{new Vector2(1280f,800f),new Vector2(1920f,1080f)})
            {
                var harness=CreateProductionHarness084(resolution,"SupportOnlyLegacy");
                var coordinator=new ChroniclePeopleCoordinator084(state);
                var build=typeof(M1FlowPresenter).GetMethod("BuildPeopleRuntime029",
                    BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(build,Is.Not.Null);
                build.Invoke(harness.Presenter,new object[]{harness.Body,coordinator,state});
                RebuildProductionHarness084(harness);

                var visible=string.Join("\n",harness.Body.GetComponentsInChildren<Text>(true)
                    .Select(value=>value.text));
                Assert.That(visible,Does.Contain("OLDER CHRONICLE HELD SAFELY"));
                Assert.That(visible,Does.Contain("MANUAL SAVE CHECK NEEDED"));
                Assert.That(visible,Does.Contain("contact support"));
                Assert.That(visible,Does.Not.Contain(held.BoardId));
                Assert.That(visible,Does.Not.Contain(held.RecruitId));
                Assert.That(harness.Body.GetComponentsInChildren<Button>(true).Any(value=>
                    value.name.StartsWith("Personal Quest Recovery Action ",StringComparison.Ordinal)),Is.False,
                    "The known fail-closed orphan must not offer a button guaranteed to fail.");
                Assert.That(harness.Body.GetComponentsInChildren<Button>(true).Any(value=>
                    value.name.StartsWith("Open Chronicle Board ",StringComparison.Ordinal)),Is.False,
                    "The global orphaned battle return blocks starting another Chronicle.");
                var back=harness.Body.GetComponentsInChildren<Button>(true).Single(value=>
                    value.name=="Return from held Chronicle 084");
                Assert.That(back.interactable,Is.True);
                AssertInside(harness.Viewport,back.GetComponent<RectTransform>(),
                    resolution+" orphaned Chronicle return action");

                UnityEngine.Object.Destroy(harness.Presenter.gameObject);
                UnityEngine.Object.Destroy(harness.Canvas.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ChronicleBattleDetoursStayVisibleAtBothSupportedResolutions()
        {
            foreach(var resolution in new[]{new Vector2(1280f,800f),new Vector2(1920f,1080f)})
            foreach(var battleCase in new[]
                    {
                        new
                        {
                            InProgress=true,AwaitingClaim=false,
                            Button="Return to Personal Quest Battle CHRONICLE_BATTLE_HIDDEN_084",
                            Copy="RETURN TO THE UNION BATTLE"
                        },
                        new
                        {
                            InProgress=false,AwaitingClaim=true,
                            Button="Open Personal Quest Battle Results CHRONICLE_BATTLE_HIDDEN_084",
                            Copy="OPEN BATTLE RESULTS & CLAIM REWARD"
                        }
                    })
            {
                var quest=new PersonalQuestView029
                {
                    BoardId="CHRONICLE_BATTLE_HIDDEN_084",
                    RecruitDisplayName="Maren",
                    ChapterLabel="CHAPTER I",
                    WorldName="Skyhome",
                    Theme="A bell calls beneath the gate.",
                    Objective="Stand together and bring every worker home.",
                    ProgressLabel="Room 6 of 10",
                    CurrentRoomLabel="Danger Room",
                    CurrentInstruction="Defeat the bell chamber guard with your friendly Unions.",
                    Started=true,
                    RequiresCertifiedBattle=true,
                    BattleInProgress=battleCase.InProgress,
                    BattleRewardAwaitingClaim=battleCase.AwaitingClaim,
                    Tiles=Array.Empty<PersonalQuestTileView029>(),
                    Destinations=Array.Empty<PersonalQuestDestinationView029>()
                };
                var state=new PeopleRuntimePresentationState029
                {
                    IsAvailable=true,
                    Quests=new[]{quest},
                    Recruits=Array.Empty<RecruitPeopleView029>(),
                    AvailableHallScenes=Array.Empty<HallSceneView029>(),
                    Bonds=Array.Empty<BondPairView029>(),
                    Doctrines=Array.Empty<DoctrineView029>(),
                    Unions=Array.Empty<UnionPeopleView029>()
                };
                var harness=CreateProductionHarness084(resolution,"ChronicleBattleDetour");
                var build=typeof(M1FlowPresenter).GetMethod("BuildPeopleRuntime029",
                    BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.That(build,Is.Not.Null);
                build.Invoke(harness.Presenter,new object[]{harness.Body,
                    new ChroniclePeopleCoordinator084(state),state});
                RebuildProductionHarness084(harness);

                var visible=string.Join("\n",harness.Body.GetComponentsInChildren<Text>(true)
                    .Select(value=>value.text));
                Assert.That(visible,Does.Contain(battleCase.Copy));
                Assert.That(harness.Body.GetComponentsInChildren<Button>(true).Any(value=>
                    value.name=="Personal Quest Battle "+quest.BoardId),Is.False,
                    "An existing battle must not offer a second enter command.");
                var primary=harness.Body.GetComponentsInChildren<Button>(true).Single(value=>
                    value.name==battleCase.Button);
                Assert.That(primary.interactable,Is.True);
                AssertInside(harness.Viewport,primary.GetComponent<RectTransform>(),
                    resolution+" Chronicle battle detour");

                UnityEngine.Object.Destroy(harness.Presenter.gameObject);
                UnityEngine.Object.Destroy(harness.Canvas.gameObject);
                yield return null;
            }
        }

        static ProductionHarness084 CreateProductionHarness084(Vector2 resolution,string suffix)
        {
            var presenterObject=new GameObject(
                "Recruit Chronicle PlayMode Presenter 084 "+suffix+" "+resolution.x);
            var presenter=presenterObject.AddComponent<M1FlowPresenter>();
            var canvasObject=new GameObject(
                "Recruit Chronicle PlayMode Canvas 084 "+suffix+" "+resolution.x,
                typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasObject.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.WorldSpace;
            var canvasRect=canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta=resolution;
            var screenObject=new GameObject("Recruit Chronicle Production Screen 084",typeof(RectTransform));
            var screen=screenObject.GetComponent<RectTransform>();
            screen.SetParent(canvasRect,false);screen.anchorMin=Vector2.zero;screen.anchorMax=Vector2.one;
            screen.offsetMin=new Vector2(96f,42f);screen.offsetMax=new Vector2(-96f,-42f);
            typeof(M1FlowPresenter).GetField("_screenRoot",BindingFlags.Instance|BindingFlags.NonPublic)
                ?.SetValue(presenter,screen);
            typeof(M1FlowPresenter).GetField("_coordinator",BindingFlags.Instance|BindingFlags.NonPublic)
                ?.SetValue(presenter,new PageCoordinator084());
            var create=typeof(M1FlowPresenter).GetMethod("CreatePage",
                BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(create,Is.Not.Null);
            var body=(RectTransform)create.Invoke(presenter,new object[]{"RECRUIT CHRONICLES","PERSONAL BOARD STORIES",null});
            var scroll=body.GetComponentInParent<ScrollRect>();
            Assert.That(scroll,Is.Not.Null);
            Assert.That(scroll.content,Is.SameAs(body));
            Assert.That(scroll.viewport.GetComponent<Mask>(),Is.Not.Null);
            return new ProductionHarness084(presenter,canvas,body,scroll.viewport);
        }

        static void RebuildProductionHarness084(ProductionHarness084 harness)
        {
            Canvas.ForceUpdateCanvases();
            var page=harness.Viewport.GetComponentInParent<ScrollRect>()?.transform.parent as RectTransform;
            if(page!=null)LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(harness.Body);
            if(page!=null)LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            LayoutRebuilder.ForceRebuildLayoutImmediate(harness.Body);
            Canvas.ForceUpdateCanvases();
        }

        static void AssertInside(RectTransform outer,RectTransform inner,string label)
        {
            var outerCorners=new Vector3[4];var innerCorners=new Vector3[4];
            outer.GetWorldCorners(outerCorners);inner.GetWorldCorners(innerCorners);
            Assert.That(innerCorners.Min(value=>value.x),Is.GreaterThanOrEqualTo(outerCorners.Min(value=>value.x)-0.5f),label);
            Assert.That(innerCorners.Max(value=>value.x),Is.LessThanOrEqualTo(outerCorners.Max(value=>value.x)+0.5f),label);
            Assert.That(innerCorners.Min(value=>value.y),Is.GreaterThanOrEqualTo(outerCorners.Min(value=>value.y)-0.5f),label);
            Assert.That(innerCorners.Max(value=>value.y),Is.LessThanOrEqualTo(outerCorners.Max(value=>value.y)+0.5f),label);
        }

        readonly struct ProductionHarness084
        {
            public ProductionHarness084(M1FlowPresenter presenter,Canvas canvas,
                RectTransform body,RectTransform viewport)
            {Presenter=presenter;Canvas=canvas;Body=body;Viewport=viewport;}
            public M1FlowPresenter Presenter{get;}
            public Canvas Canvas{get;}
            public RectTransform Body{get;}
            public RectTransform Viewport{get;}
        }

        sealed class PageCoordinator084:IM1PresentationCoordinator
        {
            public event Action Changed;
            public M1PresentationState State{get;}=new M1PresentationState
            {
                HasCampaign=true,GuildXpIntoCurrentLevel=25,
                GuildXpRequiredForNextLevel=100,TreasuryXp=40
            };
            public M1CommandResult CreateGuild(M1NewGuildIntent intent)=>Pass084();
            public M1CommandResult SignRecruit(string recruitId)=>Pass084();
            public M1CommandResult EquipItem(string recruitId,string slotId,string itemId)=>Pass084();
            public M1CommandResult UnequipItem(string recruitId,string slotId)=>Pass084();
            public M1CommandResult SetEquipmentLock(string recruitId,string slotId,bool locked)=>Pass084();
            public M1CommandResult CompleteEquipmentReview()=>Pass084();
            public M1CommandResult AddUnion()=>Pass084();
            public M1CommandResult RemoveUnion(int unionIndex)=>Pass084();
            public M1CommandResult AssignRecruitToUnion(string recruitId,int unionIndex,int slotIndex)=>Pass084();
            public M1CommandResult UnassignRecruitFromUnion(string recruitId)=>Pass084();
            public M1CommandResult SetUnionLeader(int unionIndex,string recruitId)=>Pass084();
            public M1CommandResult SetFormation(int unionIndex,string formationId)=>Pass084();
            public M1CommandResult SetDoctrine(int unionIndex,string doctrineId)=>Pass084();
            public M1CommandResult SaveAndReloadProof()=>Pass084();
            static M1CommandResult Pass084()=>M1CommandResult.Success("Saved for test.");
#pragma warning disable 67
            void PreserveChanged084()=>Changed?.Invoke();
#pragma warning restore 67
        }

        sealed class ChroniclePeopleCoordinator084:IPeopleRuntimePresentationCoordinator029
        {
            public ChroniclePeopleCoordinator084(PeopleRuntimePresentationState029 state){PeopleRuntime029=state;}
            public PeopleRuntimePresentationState029 PeopleRuntime029{get;}
            public M1CommandResult StartPersonalQuest029(string boardId)=>M1CommandResult.Success();
            public M1CommandResult RecoverPersonalQuest029(string boardId)=>M1CommandResult.Success();
            public M1CommandResult AdvancePersonalQuest029(string boardId,string destinationNodeId)=>M1CommandResult.Success();
            public M1CommandResult EnterPersonalQuestBattle029(string boardId)=>M1CommandResult.Success();
            public M1CommandResult ViewHallScene029(string sceneId)=>M1CommandResult.Success();
            public M1CommandResult DeferHallScene029(string sceneId)=>M1CommandResult.Success();
            public M1CommandResult CompleteMentorship029(string lessonId,string mentorId,string studentId)=>M1CommandResult.Success();
            public M1CommandResult RecordRelationshipMemory029(string memoryId,string firstRecruitId,string secondRecruitId,string sourceId)=>M1CommandResult.Success();
            public M1CommandResult UnlockLegendTechnique029(string recruitId)=>M1CommandResult.Success();
            public M1CommandResult SetUnionBondDoctrine029(string unionId,string doctrineId)=>M1CommandResult.Success();
        }
    }
}
