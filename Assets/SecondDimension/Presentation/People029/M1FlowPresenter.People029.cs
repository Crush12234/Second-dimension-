using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        const int PeopleQuestShelfPageSize029=6;
        public const float PersonalQuestRevealDuration029=0.18f;
        int _peopleQuestShelfPage029;
        readonly HashSet<string> _settledPersonalQuestTiles029=new HashSet<string>(StringComparer.Ordinal);

        void BuildPeopleRuntime029(Transform body, People029.IPeopleRuntimePresentationCoordinator029 coordinator, People029.PeopleRuntimePresentationState029 state)
        {
            if(coordinator==null||state==null||!state.IsAvailable)
            {
                AddStatus(body,state?.Error??"People runtime is unavailable.",false); return;
            }

            var supportOnlyQuests=state.Quests.Where(x=>x.Started&&!x.Complete&&
                    x.NeedsRecovery&&x.RecoveryRequiresSupport)
                .OrderBy(x=>x.BoardId,StringComparer.Ordinal).ToArray();
            var activeQuests=state.Quests.Where(x=>x.Started&&!x.Complete&&
                    !x.RecoveryRequiresSupport)
                .OrderByDescending(x=>x.NeedsRecovery).ThenBy(x=>x.BoardId,StringComparer.Ordinal).ToArray();
            if(activeQuests.Length>0)
            {
                var paused=Math.Max(0,activeQuests.Length-1)+supportOnlyQuests.Length;
                AddPeopleCompactHeader029(body,"ACTIVE PERSONAL CHRONICLE",
                    "Your party is standing in a revealed room"+(paused>0?" • "+paused+" legacy Chronicle"+(paused==1?" is":"s are")+" safely paused":""));
                BuildPersonalQuestPrimaryAction029(body,coordinator,activeQuests[0]);
                BuildPersonalQuestBoard029(body,coordinator,activeQuests[0]);
                return;
            }

            if(supportOnlyQuests.Length>0)
            {
                var held=supportOnlyQuests[0];
                AddPeopleCompactHeader029(body,"OLDER CHRONICLE HELD SAFELY",
                    "No quest or reward will be changed automatically");
                RuntimeUi.AddButton(body,"Return from held Chronicle 084",
                    "RETURN TO THE GUILD HALL",()=>
                    {
                        _guildCityTab017D="HALL";
                        BuildCurrentScreen();
                    },82f,RuntimeUi.ButtonNormal);
                var support=AddMessagePanel(body,"MANUAL SAVE CHECK NEEDED",
                    held.RecoveryMessage+"\n\nThis older battle return no longer has the launch record needed for a safe automatic match. Other Chronicle boards cannot begin until that doorway is repaired. Keep this save and contact support; do not delete or restart it.",
                    RuntimeUi.Warning);
                UseContentDrivenBoardPanelHeight084(support);
                return;
            }

            var availableQuests=state.Quests.Where(x=>!x.Started).ToArray();
            var pageCount=PersonalQuestShelfPageCountForVerification029(availableQuests.Length);
            _peopleQuestShelfPage029=Mathf.Clamp(_peopleQuestShelfPage029,0,pageCount-1);
            var shelfHeader=AddPeopleCompactHeader029(body,"CHRONICLE BOARD SHELF",
                availableQuests.Length+" personal stor"+(availableQuests.Length==1?"y":"ies")+" ready • page "+(_peopleQuestShelfPage029+1)+" of "+pageCount);
            if(pageCount>1)
            {
                RuntimeUi.AddButton(shelfHeader,"Previous Chronicle Shelf Page 029","◀ PREVIOUS",()=>{_peopleQuestShelfPage029=Math.Max(0,_peopleQuestShelfPage029-1);BuildCurrentScreen();},68f,RuntimeUi.ButtonNormal);
                RuntimeUi.AddButton(shelfHeader,"Next Chronicle Shelf Page 029","NEXT ▶",()=>{_peopleQuestShelfPage029=Math.Min(pageCount-1,_peopleQuestShelfPage029+1);BuildCurrentScreen();},68f,RuntimeUi.Accent);
            }
            if(availableQuests.Length==0)
                AddMessagePanel(body,"EVERY AVAILABLE CHRONICLE HAS RETURNED","Sign another member with an authored Chronicle profile to place a new board on the table.",RuntimeUi.MutedText);
            foreach(var quest in PersonalQuestShelfPageForVerification029(availableQuests,_peopleQuestShelfPage029))
            {
                var panel=AddPeopleColumnPanel029(body,"Available Chronicle "+quest.BoardId,
                    "CHRONICLE BOARD",1f,235f);
                RuntimeUi.AddText(panel,"Available Chronicle Heading "+quest.BoardId,
                    quest.RecruitDisplayName.ToUpperInvariant()+" • "+quest.ChapterLabel+" • "+quest.WorldName,
                    25,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
                RuntimeUi.AddText(panel,"Available Chronicle Story "+quest.BoardId,
                    quest.Theme+"\nOBJECTIVE • "+quest.Objective,
                    20,TextAnchor.MiddleLeft,RuntimeUi.Text);
                RuntimeUi.AddButton(panel,"Open Chronicle Board "+quest.BoardId,"PLACE THIS BOARD ON THE TABLE",()=>ApplyPeople029(coordinator.StartPersonalQuest029(quest.BoardId)),82f,RuntimeUi.Accent);
            }

            var completedQuests=state.Quests.Where(x=>x.Complete).Take(4).ToArray();
            if(completedQuests.Length>0)
            {
                AddPeopleCompactHeader029(body,"RECENTLY COMPLETED CHRONICLES","Returned safely to the Guild");
                foreach(var quest in completedQuests)
                    AddMessagePanel(body,quest.RecruitDisplayName.ToUpperInvariant()+" • "+quest.ChapterLabel,
                        "★ "+quest.ProgressLabel+"\n"+quest.Theme,RuntimeUi.Positive);
            }

            AddPeopleCompactHeader029(body,"GUILD RELATIONSHIPS & RECORDS","Optional detail • the Chronicle shelf above is the main action");
            AddMessagePanel(body,"PERMANENT PEOPLE • EARNED BONDS • COMPLETE FORECAST LINK ARTS",
                "Signed members never leave because of morale or missed scenes. Personal quests are deterministic boards, Hall scenes cost zero operations, and Link Arts appear only as complete Union Forecast choices.",RuntimeUi.Positive);
            var summary=AddRow(body,"People Runtime Summary 029",12f,155f);
            AddPeopleStat029(summary,"PROFILES",state.SignatureProfiles.ToString(),RuntimeUi.Accent);
            AddPeopleStat029(summary,"QUESTS",state.PersonalQuestBoards.ToString(),RuntimeUi.Warning);
            AddPeopleStat029(summary,"NODES",state.PersonalQuestNodes.ToString(),RuntimeUi.Text);
            AddPeopleStat029(summary,"LINK ARTS",state.LinkArts.ToString(),BattleCohesion);
            AddPeopleStat029(summary,"CODES",state.CreatorCodes.ToString(),RuntimeUi.Positive);
            AddPeopleStat029(summary,"ROOMS",state.CreatorRooms.ToString(),RuntimeUi.MutedText);

            AddPeopleSectionHeader029(body,"SIGNED MEMBER CHRONICLES");
            if(state.Recruits.Count==0)AddMessagePanel(body,"NO MATCHED CHRONICLE PROFILES","Sign a permanent member with an authored Chronicle profile to open their personal story board.",RuntimeUi.MutedText);
            foreach(var recruit in state.Recruits.Take(16))
            {
                var panel=AddRow(body,"People Recruit "+recruit.RecruitId,12f,170f);
                RuntimeUi.AddText(panel,"People Recruit Text "+recruit.RecruitId,
                    recruit.DisplayName.ToUpperInvariant()+"\n"+recruit.ChronicleTitle+" • "+recruit.WorldName+"\nMEMORIES "+recruit.MeaningfulMemories+" • "+recruit.QuestProgressLabel+" • LEGEND "+(recruit.LegendUnlocked?"UNLOCKED":"LOCKED"),
                    27,TextAnchor.MiddleLeft,RuntimeUi.Text,FontStyle.Bold);
                if(recruit.QuestComplete&&!recruit.SignatureTechniqueUnlocked)
                    RuntimeUi.AddButton(panel,"Unlock Legend "+recruit.RecruitId,"CHECK LEGEND GATES",()=>ApplyPeople029(coordinator.UnlockLegendTechnique029(recruit.RecruitId)),120f,RuntimeUi.Warning);
            }

            AddPeopleSectionHeader029(body,"FREE HALL SCENES");
            foreach(var scene in state.AvailableHallScenes.Where(x=>!x.Viewed).Take(12))
            {
                var panel=AddRow(body,"People Hall Scene "+scene.SceneId,12f,145f);
                RuntimeUi.AddText(panel,"People Hall Scene Text "+scene.SceneId,scene.DisplayName.ToUpperInvariant()+" • "+scene.LocationName+(scene.Deferred?" • DEFERRED WITHOUT PENALTY":"")+"\n"+scene.Summary,26,TextAnchor.MiddleLeft,RuntimeUi.Text);
                RuntimeUi.AddButton(panel,"People Hall Scene View "+scene.SceneId,"VIEW FREE SCENE",()=>ApplyPeople029(coordinator.ViewHallScene029(scene.SceneId)),105f,RuntimeUi.Positive);
                if(!scene.Deferred)RuntimeUi.AddButton(panel,"People Hall Scene Defer "+scene.SceneId,"DEFER WITHOUT PENALTY",()=>ApplyPeople029(coordinator.DeferHallScene029(scene.SceneId)),105f,RuntimeUi.ButtonNormal);
            }

            AddPeopleSectionHeader029(body,"EARNED BONDS");
            if(state.Bonds.Count==0)AddMessagePanel(body,"NO BOND PAIRS YET","Deploy members together, protect, heal, rescue, camp, mentor, or staff them together. Meaningful events create exact-once memories and bond tiers.",RuntimeUi.MutedText);
            foreach(var bond in state.Bonds.Take(16))
            {
                AddMessagePanel(body,bond.FirstDisplayName+" ↔ "+bond.SecondDisplayName,
                    bond.TierDisplayName+" • TRUST "+bond.Trust+" • RESPECT "+bond.Respect+" • FAMILIARITY "+bond.Familiarity+" • MEMORIES "+bond.SharedMemories,
                    bond.TierRank>=3?RuntimeUi.Positive:RuntimeUi.Text);
            }

            AddPeopleSectionHeader029(body,"UNION BOND DOCTRINES");
            foreach(var union in state.Unions.Take(10))
            {
                var panel=AddPeopleColumnPanel029(body,"People Union "+union.UnionId,
                    union.DisplayName.ToUpperInvariant(),1f,230f);
                RuntimeUi.AddText(panel,"People Union Heading "+union.UnionId,union.DisplayName.ToUpperInvariant()+"\nCURRENT BOND DOCTRINE: "+union.BondDoctrineName.ToUpperInvariant(),27,TextAnchor.MiddleLeft,RuntimeUi.Text,FontStyle.Bold);
                var choices=AddRow(panel,"People Union Doctrine Choices "+union.UnionId,8f,105f);
                foreach(var doctrine in state.Doctrines.Take(6))
                {
                    var d=doctrine;
                    RuntimeUi.AddButton(choices,"People Doctrine "+union.UnionId+d.DoctrineId,d.DisplayName.ToUpperInvariant(),()=>ApplyPeople029(coordinator.SetUnionBondDoctrine029(union.UnionId,d.DoctrineId)),95f,StringComparer.Ordinal.Equals(union.BondDoctrineId,d.DoctrineId)?RuntimeUi.Accent:RuntimeUi.ButtonNormal);
                }
            }
        }

        void BuildPersonalQuestBoard029(Transform body,People029.IPeopleRuntimePresentationCoordinator029 coordinator,People029.PersonalQuestView029 quest)
        {
            var panel=AddPeopleColumnPanel029(body,"Personal Quest Board "+quest.BoardId,
                "PERSONAL CHRONICLE BOARD",1f,quest.NeedsRecovery?430f:680f);
            RuntimeUi.AddText(panel,"Personal Chronicle Heading "+quest.BoardId,
                quest.RecruitDisplayName.ToUpperInvariant()+" • "+quest.ChapterLabel+" • "+quest.WorldName,
                27,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
            RuntimeUi.AddText(panel,"Personal Chronicle Context "+quest.BoardId,
                quest.Theme+"\nOBJECTIVE • "+quest.Objective,
                20,TextAnchor.MiddleLeft,RuntimeUi.Text);
            if(quest.NeedsRecovery)
            {
                var recovery=RuntimeUi.AddPanel(panel,"Personal Quest Recovery "+quest.BoardId,RuntimeUi.PanelRaised);
                RuntimeUi.SetLayout(recovery,preferredHeight:170f);
                M1PremiumUi.StylePanel(recovery,M1PremiumUi.Surface.Warning);
                RuntimeUi.AddVerticalLayout(recovery.transform,new RectOffset(20,20,12,12),5f);
                RuntimeUi.AddText(recovery.transform,"Personal Quest Recovery Heading "+quest.BoardId,
                    "OLDER SAVE • SAFE NO-REWARD REPAIR",22,TextAnchor.MiddleLeft,RuntimeUi.Warning,FontStyle.Bold);
                RuntimeUi.AddText(recovery.transform,"Personal Quest Recovery Copy "+quest.BoardId,
                    quest.RecoveryMessage,18,TextAnchor.MiddleLeft,RuntimeUi.Text);
                return;
            }
            var current=RuntimeUi.AddPanel(panel,"Current Personal Quest Room "+quest.BoardId,RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(current,preferredHeight:132f);
            M1PremiumUi.StylePanel(current,M1PremiumUi.Surface.Warning);
            RuntimeUi.AddVerticalLayout(current.transform,new RectOffset(20,20,10,10),3f);
            RuntimeUi.AddText(current.transform,"Current Personal Quest Room Heading "+quest.BoardId,
                "ROOM REVEALED • "+quest.CurrentRoomLabel.ToUpperInvariant(),21,TextAnchor.MiddleLeft,RuntimeUi.Warning,FontStyle.Bold);
            RuntimeUi.AddText(current.transform,"Current Personal Quest Room Copy "+quest.BoardId,
                quest.CurrentInstruction+"\n"+quest.ProgressLabel,18,TextAnchor.MiddleLeft,RuntimeUi.Text);
            BuildPersonalQuestTrack029(panel,quest);
        }

        void BuildPersonalQuestPrimaryAction029(Transform body,
            People029.IPeopleRuntimePresentationCoordinator029 coordinator,
            People029.PersonalQuestView029 quest)
        {
            if(quest.NeedsRecovery)
            {
                if(!quest.RecoveryRequiresSupport)
                    RuntimeUi.AddButton(body,"Personal Quest Recovery Action "+quest.BoardId,
                        "SAFELY REPAIR THIS CHRONICLE",()=>ApplyPeople029(
                            coordinator.RecoverPersonalQuest029(quest.BoardId)),
                        92f,RuntimeUi.Warning);
                return;
            }
            var choices=AddRow(body,"Personal Quest Choices "+quest.BoardId,10f,100f);
            if(quest.RequiresCertifiedBattle)
            {
                if(quest.BattleInProgress)
                    RuntimeUi.AddButton(choices,"Return to Personal Quest Battle "+quest.BoardId,
                        "RETURN TO THE UNION BATTLE",()=>Navigate(M1Screen.Battle),82f,RuntimeUi.Warning);
                else if(quest.BattleRewardAwaitingClaim)
                    RuntimeUi.AddButton(choices,"Open Personal Quest Battle Results "+quest.BoardId,
                        "OPEN BATTLE RESULTS & CLAIM REWARD",()=>Navigate(M1Screen.Battle),82f,RuntimeUi.Positive);
                else
                    RuntimeUi.AddButton(choices,"Personal Quest Battle "+quest.BoardId,
                        "ENTER CERTIFIED UNION BATTLE",()=>EnterPersonalQuestBattle029(coordinator,quest.BoardId),82f,RuntimeUi.Warning);
            }
            else
            {
                foreach(var route in quest.Destinations)
                {
                    var destination=route;
                    RuntimeUi.AddButton(choices,"Personal Quest Move "+quest.BoardId+destination.DestinationNodeId,
                        destination.ButtonLabel,()=>ApplyPeople029(coordinator.AdvancePersonalQuest029(quest.BoardId,destination.DestinationNodeId)),82f,RuntimeUi.Accent);
                }
            }
        }

        void BuildPersonalQuestTrack029(Transform parent,People029.PersonalQuestView029 quest)
        {
            var track=RuntimeUi.AddPanel(parent,"Personal Quest Face Down Track "+quest.BoardId,
                new Color(0.006f,0.016f,0.028f,0.96f));
            RuntimeUi.SetLayout(track,preferredHeight:210f);
            M1PremiumUi.StylePanel(track,M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(track.transform,new RectOffset(10,10,8,8),7f);
            for(var rowIndex=0;rowIndex<2;rowIndex++)
            {
                var row=RuntimeUi.AddPanel(track.transform,"Personal Quest Track Row "+rowIndex+" "+quest.BoardId,Color.clear);
                RuntimeUi.SetLayout(row,preferredHeight:92f);
                RuntimeUi.AddHorizontalLayout(row.transform,new RectOffset(2,2,2,2),7f,TextAnchor.MiddleCenter);
                foreach(var tile in quest.Tiles.Skip(rowIndex*5).Take(5))
                {
                    var card=RuntimeUi.AddPanel(row.transform,"Personal Quest Tile "+tile.NodeId,Color.white);
                    RuntimeUi.SetLayout(card,preferredHeight:84f,flexibleWidth:1f);
                    var group=card.gameObject.GetComponent<CanvasGroup>();
                    if(group==null)group=card.gameObject.AddComponent<CanvasGroup>();
                    group.alpha=1f;
                    M1PremiumUi.StylePanel(card,
                        tile.IsCurrent?M1PremiumUi.Surface.Warning:
                        tile.IsCleared?M1PremiumUi.Surface.Positive:
                        tile.IsRevealed?M1PremiumUi.Surface.WorldPaper:
                        tile.IsUnchosenRoute?M1PremiumUi.Surface.Iron:M1PremiumUi.Surface.WorldGlass);
                    var copy=RuntimeUi.AddText(card.transform,"Personal Quest Tile Copy "+tile.NodeId,
                        tile.DisplayLabel,15,TextAnchor.MiddleCenter,
                        tile.IsCurrent?RuntimeUi.Warning:
                        tile.IsCleared?RuntimeUi.Positive:
                        tile.IsRevealed?RuntimeUi.Accent:RuntimeUi.MutedText,FontStyle.Bold);
                    Stretch(copy.rectTransform);
                    copy.rectTransform.offsetMin=new Vector2(5f,4f);
                    copy.rectTransform.offsetMax=new Vector2(-5f,-4f);
                    ConfigureAuthoredCompactText076(copy,10,15);
                    if(tile.IsCurrent)
                    {
                        var revealKey=quest.BoardId+"|"+quest.CurrentNodeId;
                        var firstReveal=_settledPersonalQuestTiles029.Add(revealKey);
                        if(_reducedMotion||!firstReveal||!Application.isPlaying||!isActiveAndEnabled)
                        {
                            group.alpha=1f;
                            card.rectTransform.localScale=Vector3.one;
                        }
                        else StartCoroutine(RevealPersonalQuestTile029(card.rectTransform,group));
                    }
                }
            }
        }

        IEnumerator RevealPersonalQuestTile029(RectTransform tile,CanvasGroup group)
        {
            if(tile==null||group==null)yield break;
            if(_reducedMotion)
            {
                group.alpha=1f;tile.localScale=Vector3.one;yield break;
            }
            var home=tile.anchoredPosition;
            var startScale=new Vector3(0.08f,1f,1f);
            tile.localScale=startScale;
            tile.anchoredPosition=home+new Vector2(0f,10f);
            group.alpha=0.42f;
            var elapsed=0f;
            const float duration=PersonalQuestRevealDuration029;
            while(elapsed<duration&&tile!=null&&group!=null)
            {
                elapsed+=Time.unscaledDeltaTime;
                var t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(elapsed/duration));
                tile.localScale=Vector3.Lerp(startScale,Vector3.one,t);
                tile.anchoredPosition=Vector2.Lerp(home+new Vector2(0f,10f),home,t);
                group.alpha=Mathf.Lerp(0.42f,1f,t);
                yield return null;
            }
            if(tile!=null){tile.localScale=Vector3.one;tile.anchoredPosition=home;}
            if(group!=null)group.alpha=1f;
        }

        static RectTransform AddPeopleColumnPanel029(Transform parent,string objectName,
            string visibleHeading,float flexibleWidth,float preferredHeight)
        {
            var panel=RuntimeUi.AddPanel(parent,objectName,RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel,preferredHeight:preferredHeight,flexibleWidth:flexibleWidth);
            M1PremiumUi.StylePanel(panel,M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(panel.transform,new RectOffset(26,26,22,22),12f);
            M1PremiumUi.AddSectionDivider(panel.transform,visibleHeading);
            return panel.rectTransform;
        }

        public static int PersonalQuestShelfPageCountForVerification029(int boardCount)
        {
            return Math.Max(1,(Math.Max(0,boardCount)+PeopleQuestShelfPageSize029-1)/PeopleQuestShelfPageSize029);
        }

        public static System.Collections.Generic.IReadOnlyList<People029.PersonalQuestView029> PersonalQuestShelfPageForVerification029(
            System.Collections.Generic.IReadOnlyList<People029.PersonalQuestView029> quests,int page)
        {
            var source=quests??Array.Empty<People029.PersonalQuestView029>();
            var pageCount=PersonalQuestShelfPageCountForVerification029(source.Count);
            var safePage=Mathf.Clamp(page,0,pageCount-1);
            return source.Skip(safePage*PeopleQuestShelfPageSize029).Take(PeopleQuestShelfPageSize029).ToArray();
        }



        void EnterPersonalQuestBattle029(People029.IPeopleRuntimePresentationCoordinator029 coordinator,string boardId)
        {
            var result=coordinator.EnterPersonalQuestBattle029(boardId); _localStatus=result?.Message??"Personal battle returned no result."; _localStatusPositive=result!=null&&result.Succeeded; if(result!=null&&result.Succeeded)Navigate(M1Screen.Battle);else BuildCurrentScreen();
        }

        Transform AddPeopleCompactHeader029(Transform parent,string heading,string detail)
        {
            var panel=RuntimeUi.AddPanel(parent,"People Compact Header "+heading,RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel,preferredHeight:82f);
            M1PremiumUi.StylePanel(panel,M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddHorizontalLayout(panel.transform,new RectOffset(20,20,8,8),8f,TextAnchor.MiddleCenter);
            var text=RuntimeUi.AddText(panel.transform,"People Compact Header Copy "+heading,
                heading+" • "+detail,22,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
            RuntimeUi.SetLayout(text,flexibleWidth:1f);
            return panel.transform;
        }

        void AddPeopleSectionHeader029(Transform parent,string heading)
        {
            AddMessagePanel(parent,heading,"",RuntimeUi.Accent);
        }

        void AddPeopleStat029(Transform parent,string heading,string value,Color color)
        {
            var panel=AddColumnPanel(parent,"People Stat "+heading,1f,145f);
            RuntimeUi.AddText(panel,"People Stat Heading "+heading,heading,22,TextAnchor.MiddleCenter,RuntimeUi.MutedText,FontStyle.Bold);
            RuntimeUi.AddText(panel,"People Stat Value "+heading,value,42,TextAnchor.MiddleCenter,color,FontStyle.Bold);
        }
        void ApplyPeople029(M1CommandResult result)
        {
            _localStatus=result?.Message??"People command returned no result.";
            _localStatusPositive=result!=null&&result.Succeeded;
            BuildCurrentScreen();
        }
    }
}
