using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.PeopleBonds026;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.People029;
using SecondDimension.Presentation.PeopleBonds026;
using SecondDimension.Presentation.RecruitChronicles025;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : IPeopleRuntimePresentationCoordinator029
    {
        RecruitChronicleCommandService025 _recruitChronicleCommands025;
        readonly PeopleBondService026 _peopleBondCommands026=new PeopleBondService026();
        RecruitChronicleBattleService025 _recruitChronicleBattle025;
        RecruitChronicleRegistry025 _recruitChronicleRegistry025;
        PeopleBondCatalog026 _peopleBondCatalog026;

        RecruitChronicleRegistry025 ChronicleRegistry025()=>_recruitChronicleRegistry025??(_recruitChronicleRegistry025=RecruitChronicleRegistry025.LoadFromResources());
        RecruitChronicleCommandService025 ChronicleCommands025()=>_recruitChronicleCommands025??(_recruitChronicleCommands025=new RecruitChronicleCommandService025(ChronicleRegistry025()));
        RecruitChronicleBattleService025 ChronicleBattles025()=>_recruitChronicleBattle025??(_recruitChronicleBattle025=new RecruitChronicleBattleService025(ChronicleRegistry025()));
        PeopleBondCatalog026 BondCatalog026()=>_peopleBondCatalog026??(_peopleBondCatalog026=PeopleBondRegistry026.Load());

        public PeopleRuntimePresentationState029 PeopleRuntime029
        {
            get { try { return BuildPeopleRuntime029(); } catch(Exception e) { return new PeopleRuntimePresentationState029{IsAvailable=false,Error=e.Message}; } }
        }

        public M1CommandResult StartPersonalQuest029(string boardId)
        {
            var board=ProjectPersonalQuestBoard029(ChronicleRegistry025().Board(boardId));
            return ApplyPeople029(ChronicleCommands025().StartPersonalQuest(_campaign,board),"Personal quest committed and saved without consuming an empty day.");
        }
        public M1CommandResult RecoverPersonalQuest029(string boardId)
        {
            var recovered=ChronicleCommands025().RecoverInvalidPersonalQuest(_campaign,boardId);
            if(!recovered.IsSuccess&&recovered.Errors.Any(value=>value.IndexOf(
                   "CHRONICLE025_RECOVERY_HANDOFF_LINK_UNPROVEN",StringComparison.Ordinal)>=0))
                return M1CommandResult.Failure("The old battle handoff cannot be matched safely. Nothing was changed. Keep this save and contact support rather than risking another quest or reward.");
            if(!recovered.IsSuccess&&recovered.Errors.Any(value=>value.IndexOf(
                   "CHRONICLE025_RECOVERY_RECORD_AMBIGUOUS",StringComparison.Ordinal)>=0))
                return M1CommandResult.Failure("More than one older Chronicle record could own this handoff. Nothing was changed; the save needs a manual authority check.");
            if(!recovered.IsSuccess&&recovered.Errors.Any(value=>value.IndexOf(
                   "CHRONICLE025_RECOVERY_HANDOFF_NOT_FOUND",StringComparison.Ordinal)>=0))
                return M1CommandResult.Failure("No battle handoff is still linked to this archived Chronicle. Nothing was changed.");
            return ApplyPeople029(recovered,
                "The older Chronicle and its exactly matched battle handoff were safely quarantined. No reward was granted or removed, completed Chronicles were untouched, and a clean board is ready when one exists.");
        }
        public M1CommandResult AdvancePersonalQuest029(string boardId,string destinationNodeId)
        {
            var registry=ChronicleRegistry025();var board=ProjectPersonalQuestBoard029(registry.Board(boardId));
            if(board==null)return M1CommandResult.Failure("Personal quest board is unavailable.");
            var destinationNode=RecruitChronicleRules025.FindNode(board,destinationNodeId);
            if(destinationNode==null)return M1CommandResult.Failure("Personal quest destination is unavailable.");
            var receipt=RecruitChronicleRules025.PersonalQuestMoveReceiptId(_campaign.CampaignSeed,boardId,destinationNodeId,board.recruitId);
            var advanced=ChronicleCommands025().AdvancePersonalQuest(_campaign,board,destinationNodeId,receipt);
            if(!advanced.IsSuccess)return ApplyPeople029(advanced,"Personal-quest node could not be committed.");
            if(string.IsNullOrWhiteSpace(destinationNode.rewardMemoryId))return ApplyPeople029(advanced,"Personal-quest node committed exactly once.");
            var earned=_peopleBondCommands026.ApplyPersonalQuestNodeMemory(advanced.Value,board,destinationNode,registry.Memory(destinationNode.rewardMemoryId),receipt);
            return ApplyPeople029(earned,"Personal-quest node and its authored relationship memory committed exactly once.");
        }
        public M1CommandResult EnterPersonalQuestBattle029(string boardId)
        {
            var board=ProjectPersonalQuestBoard029(ChronicleRegistry025().Board(boardId)); if(board==null)return M1CommandResult.Failure("Personal quest board is unavailable.");
            var unionIds=(_campaign?.Guild?.Unions??Array.Empty<UnionState>()).Where(x=>x.Kind==UnionKind.Normal&&x.MemberRecruitIds.Count>0&&x.MemberRecruitIds.Contains(board.recruitId)).Select(x=>x.UnionId).Take(10).ToList();
            if(unionIds.Count==0)unionIds.AddRange((_campaign?.Guild?.Unions??Array.Empty<UnionState>()).Where(x=>x.Kind==UnionKind.Normal&&x.MemberRecruitIds.Count>0).Select(x=>x.UnionId).Take(2));
            var committed=ChronicleBattles025().CommitEncounter(_campaign,board,unionIds.AsReadOnly()); var saved=ApplyAndPersist(committed,false,"Personal quest battle committed."); if(!saved.Succeeded)return saved;
            return StartCommittedGuildCityBattle017D();
        }
        public M1CommandResult ViewHallScene029(string sceneId)=>ApplyPeople029(ChronicleCommands025().ViewHallScene(_campaign,ChronicleRegistry025().HallScene(sceneId)),"Free Hall scene viewed. No operation or day was consumed.");
        public M1CommandResult DeferHallScene029(string sceneId)=>ApplyPeople029(ChronicleCommands025().DeferHallScene(_campaign,ChronicleRegistry025().HallScene(sceneId)),"Hall scene deferred without penalty. No operation, expiry, or departure consequence was introduced.");
        public M1CommandResult CompleteMentorship029(string lessonId,string mentorId,string studentId)
        {
            var registry=ChronicleRegistry025();
            if(!registry.MentorshipLessons.TryGetValue(lessonId,out var lesson))return M1CommandResult.Failure("Mentorship lesson is unavailable.");
            var memory=registry.Memory(lesson.relationshipMemoryId);
            var receipt=RecruitChronicleRules025.DeterministicReceiptId(_campaign.CampaignSeed,lessonId,mentorId,studentId);
            return ApplyPeople029(ChronicleCommands025().CompleteMentorship(_campaign,lesson,mentorId,studentId,receipt,memory),"Mentorship progress and its relationship memory were saved in parallel.");
        }
        public M1CommandResult RecordRelationshipMemory029(string memoryId,string firstRecruitId,string secondRecruitId,string sourceId)
        {
            var memory=ChronicleRegistry025().Memory(memoryId);
            var receipt=RecruitChronicleRules025.DeterministicReceiptId(_campaign.CampaignSeed,memoryId,sourceId??string.Empty,RecruitChronicleRules025.PairId(firstRecruitId,secondRecruitId));
            return ApplyPeople029(ChronicleCommands025().RecordMemory(_campaign,memory,firstRecruitId,secondRecruitId,receipt,sourceId),"Meaningful relationship memory committed exactly once.");
        }
        public M1CommandResult UnlockLegendTechnique029(string recruitId)
        {
            var registry=ChronicleRegistry025(); var profile=ProjectChronicleProfile029(registry.Profiles.Values.FirstOrDefault(value=>ChronicleIdentityMatches029(value.recruitId,recruitId)));
            if(profile==null)return M1CommandResult.Failure("This recruit has no authored Chronicle profile.");
            if(!registry.Legends.TryGetValue(profile.legendArchetypeId,out var legend))return M1CommandResult.Failure("Emergent Legend authority is unavailable.");
            if(!registry.Techniques.TryGetValue(profile.signatureTechniqueCandidateId,out var technique))return M1CommandResult.Failure("Signature Technique authority is unavailable.");
            return ApplyPeople029(ChronicleCommands025().UnlockLegendAndTechnique(_campaign,profile,legend,technique),"Emergent Legend and Signature Technique gates were applied.");
        }
        public M1CommandResult SetUnionBondDoctrine029(string unionId,string doctrineId)
        {
            if(!BondCatalog026().Doctrines.TryGetValue(doctrineId,out var doctrine))return M1CommandResult.Failure("Bond doctrine is unavailable.");
            return ApplyPeople029(_peopleBondCommands026.SetUnionDoctrine(_campaign,unionId,doctrine),"Union bond doctrine saved. It influences complete Forecast families only.");
        }

        M1CommandResult ApplyPeople029(Result<CampaignState> result,string message)=>ApplyAndPersist(result,true,message);

        PeopleRuntimePresentationState029 BuildPeopleRuntime029()
        {
            var registry=ChronicleRegistry025(); var bondsCatalog=BondCatalog026();
            var playable=_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020??CampaignPlayableState020.Default();
            var chronicles=playable.RecruitChronicles025??RecruitChronicleState025.Default(); var bonds=playable.PeopleBonds026??PeopleBondState026.Default();
            var signedRecruits=(_campaign?.Guild?.Recruits??Array.Empty<RecruitState>()).Where(x=>x.AuthorityKind==RecruitAuthorityKind.Normal).ToArray();
            var signed=new HashSet<string>(StringComparer.Ordinal);
            var recruitNames=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var recruit in signedRecruits)
            {
                signed.Add(recruit.RecruitId);
                recruitNames[recruit.RecruitId]=string.IsNullOrWhiteSpace(recruit.DisplayName)?"Guild member":recruit.DisplayName;
                if(!string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId)){signed.Add(recruit.AuthoredStableRecruitId);recruitNames[recruit.AuthoredStableRecruitId]=recruitNames[recruit.RecruitId];}
                if(!string.IsNullOrWhiteSpace(recruit.TutorialAliasId)){signed.Add(recruit.TutorialAliasId);recruitNames[recruit.TutorialAliasId]=recruitNames[recruit.RecruitId];}
                if(!string.IsNullOrWhiteSpace(recruit.SignatureId)){signed.Add(recruit.SignatureId);recruitNames[recruit.SignatureId]=recruitNames[recruit.RecruitId];}
            }
            var questViews=new List<PersonalQuestView029>();
            foreach(var authoredProfile in registry.Profiles.Values.Where(x=>signed.Contains(x.recruitId)).OrderBy(x=>x.displayName,StringComparer.Ordinal))
            {
                var profile=ProjectChronicleProfile029(authoredProfile); if(profile==null)continue;
                recruitNames[profile.recruitId]=profile.displayName;
                foreach(var boardId in profile.personalQuestBoardIds??Array.Empty<string>())
                {
                    var board=ProjectPersonalQuestBoard029(registry.Board(boardId)); if(board==null)continue;
                    var progress=RecruitChronicleRules025.ActiveQuest(chronicles,_campaign.CampaignSeed,boardId)??
                        chronicles.PersonalQuests.FirstOrDefault(x=>x.Completed&&StringComparer.Ordinal.Equals(x.BoardId,boardId));
                    var view=RecruitChronicleBoardProjection084.Project(board,profile,progress);
                    var battleMatchesBoard=view.Started&&!view.Complete&&
                        RecruitChronicleBattleService025.IsPending(_campaign)&&
                        StringComparer.Ordinal.Equals(
                            RecruitChronicleBattleService025.BoardId(_campaign),boardId);
                    view.BattleInProgress=battleMatchesBoard&&
                        _campaign?.Battle?.Outcome==SecondDimension.Gameplay.M2.BattleOutcome.InProgress;
                    view.BattleRewardAwaitingClaim=battleMatchesBoard&&
                        _campaign?.Battle?.Phase==SecondDimension.Gameplay.M2.BattlePhase.Resolved&&
                        _campaign.Battle.Reward!=null&&!_campaign.Battle.Reward.Claimed;
                    if(progress!=null&&!progress.Completed&&
                       !RecruitChronicleRules025.ValidateProgress(_campaign,board,progress,out _))
                    {
                        view.NeedsRecovery=true;
                        view.RecoveryMessage="This older save cannot prove its current room. Repair it safely; earned Guild rewards and completed Chronicles stay untouched.";
                        view.RequiresCertifiedBattle=false;
                        view.Destinations=Array.Empty<PersonalQuestDestinationView029>();
                    }
                    questViews.Add(view);
                }
            }
            var representedBoards=new HashSet<string>(questViews.Select(value=>value.BoardId),StringComparer.Ordinal);
            foreach(var legacy in chronicles.PersonalQuests.Where(value=>!value.Completed&&
                        !RecruitChronicleRules025.IsArchivedLegacyQuest(chronicles,_campaign.CampaignSeed,value)&&
                        !representedBoards.Contains(value.BoardId)).OrderBy(value=>value.BoardId,StringComparer.Ordinal))
                questViews.Add(new PersonalQuestView029
                {
                    BoardId=legacy.BoardId,RecruitId=legacy.RecruitId,DisplayName="Legacy Chronicle Record",
                    RecruitDisplayName=recruitNames.TryGetValue(legacy.RecruitId,out var legacyName)?legacyName:"Guild member",
                    ChronicleTitle="Archived Road",ChapterLabel="RECOVERY NEEDED",WorldName="Guild Archive",
                    Theme="An older Chronicle record needs a safe authority check.",
                    StoryContext="The Guild kept this record instead of discarding it.",
                    Objective="Repair the saved board without changing earned rewards or completed stories.",
                    ProgressLabel="Legacy record preserved",CurrentNodeTitle="Unreadable Save Room",
                    CurrentRoomLabel="Unreadable Save Room",CurrentNodeDescription="The room record cannot be trusted yet.",
                    CurrentInstruction="Use the repair action below before continuing.",Started=true,NeedsRecovery=true,
                    RecoveryMessage="This older save cannot prove its current room. Repair it safely; earned Guild rewards and completed Chronicles stay untouched."
                });
            var archivedIncomplete=chronicles.PersonalQuests.Where(value=>!value.Completed&&
                    RecruitChronicleRules025.IsArchivedLegacyQuest(chronicles,_campaign.CampaignSeed,value))
                .OrderBy(value=>value.BoardId,StringComparer.Ordinal).ThenBy(value=>value.RecruitId,StringComparer.Ordinal).ToArray();
            var pendingRequest=_campaign?.Guild?.GuildCity?.PendingEncounter;
            var pendingReturn=_campaign?.Guild?.GuildCity?.PendingBattleReturn;
            PersonalQuestProgressState025 orphanedHandoff=null;
            if(pendingRequest!=null)
            {
                var claimedBoardId=pendingRequest.ContractId.StartsWith(RecruitChronicleBattleService025.ContractPrefix,StringComparison.Ordinal)
                    ?pendingRequest.ContractId.Substring(RecruitChronicleBattleService025.ContractPrefix.Length):pendingRequest.BoardId;
                orphanedHandoff=archivedIncomplete.FirstOrDefault(value=>StringComparer.Ordinal.Equals(value.BoardId,claimedBoardId)||
                    StringComparer.Ordinal.Equals(value.BoardId,pendingRequest.BoardId));
            }
            else if(pendingReturn!=null&&archivedIncomplete.Length>0)orphanedHandoff=archivedIncomplete[0];
            if(orphanedHandoff!=null)
            {
                var exactLaunchPresent=pendingRequest!=null;
                var handoffView=questViews.FirstOrDefault(value=>
                    StringComparer.Ordinal.Equals(value.BoardId,orphanedHandoff.BoardId)&&
                    StringComparer.Ordinal.Equals(value.RecruitId,orphanedHandoff.RecruitId));
                if(handoffView==null)
                {
                    handoffView=new PersonalQuestView029
                    {
                        BoardId=orphanedHandoff.BoardId,RecruitId=orphanedHandoff.RecruitId,
                        RecruitDisplayName=recruitNames.TryGetValue(orphanedHandoff.RecruitId,out var archivedName)?archivedName:"Guild member"
                    };
                    questViews.Add(handoffView);
                }
                handoffView.DisplayName="Archived Chronicle Handoff";
                handoffView.ChronicleTitle="Sealed Battle Road";handoffView.ChapterLabel="BATTLE HANDOFF RECOVERY";
                handoffView.WorldName="Guild Archive";handoffView.Theme="An older battle doorway is still holding the Chronicle table.";
                handoffView.StoryContext="The saved story record remains archived and its completed history will not be rewritten.";
                handoffView.Objective="Quarantine only the battle handoff that can be proven to belong to this exact member and room.";
                handoffView.ProgressLabel="Archived story record preserved";handoffView.CurrentNodeTitle="Sealed Doorway";
                handoffView.CurrentRoomLabel="Sealed Doorway";handoffView.CurrentNodeDescription="The old doorway must be checked before another battle can use it.";
                handoffView.CurrentInstruction="Use the safety check below. It never grants or removes a reward.";
                handoffView.Started=true;handoffView.Complete=false;handoffView.NeedsRecovery=true;
                handoffView.RecoveryRequiresSupport=!exactLaunchPresent;
                handoffView.RequiresCertifiedBattle=false;handoffView.Destinations=Array.Empty<PersonalQuestDestinationView029>();
                handoffView.RecoveryMessage=exactLaunchPresent
                    ?"Every saved board, member, room, Union, battle, and return identity must match before this doorway is quarantined. If one detail differs, nothing changes."
                    :"The return has lost its launch record, so it cannot be matched automatically. The safety check will fail closed and leave every campaign record unchanged.";
            }
            var recruitViews=new List<RecruitPeopleView029>();
            foreach(var authoredProfile in registry.Profiles.Values.Where(x=>signed.Contains(x.recruitId)).OrderBy(x=>x.displayName,StringComparer.Ordinal))
            {
                var profile=ProjectChronicleProfile029(authoredProfile); if(profile==null)continue;
                var boardIds=profile.personalQuestBoardIds??Array.Empty<string>();
                var profileQuests=questViews.Where(x=>StringComparer.Ordinal.Equals(x.RecruitId,profile.recruitId)).ToArray();
                var firstBoardId=boardIds.FirstOrDefault()??string.Empty;
                var nextQuest=profileQuests.FirstOrDefault(x=>!x.Started);
                var active=profileQuests.Count(x=>x.Started&&!x.Complete);
                var complete=profileQuests.Count(x=>x.Complete);
                recruitViews.Add(new RecruitPeopleView029{
                    RecruitId=profile.recruitId,DisplayName=profile.displayName,ChronicleTitle=profile.chronicleTitle,
                    WorldId=profile.worldId,WorldName=RecruitChronicleBoardProjection084.FriendlyWorldName(profile.worldId),
                    BoardId=firstBoardId,NextBoardId=nextQuest?.BoardId??string.Empty,NextQuestLabel=nextQuest?.ChapterLabel??string.Empty,
                    QuestProgressLabel=complete+" of "+profileQuests.Length+" Chronicles returned",
                    QuestStarted=profileQuests.Any(x=>x.Started),QuestComplete=profileQuests.Length>0&&complete==profileQuests.Length,
                    LegendUnlocked=chronicles.UnlockedLegendIds.Contains(profile.legendArchetypeId),
                    SignatureTechniqueUnlocked=chronicles.UnlockedSignatureTechniqueIds.Contains(profile.signatureTechniqueCandidateId),
                    MeaningfulMemories=RecruitChronicleRules025.MeaningfulMemoryCount(chronicles,profile.recruitId),
                    TotalQuests=profileQuests.Length,ActiveQuests=active,CompletedQuests=complete});
            }
            var hall=new List<HallSceneView029>();
            foreach(var scene in registry.HallEvents.Values.OrderBy(x=>x.displayName,StringComparer.Ordinal))
            {
                var participants=scene.participantRecruitIds??Array.Empty<string>(); if(participants.Length>0&&!participants.All(signed.Contains))continue;
                hall.Add(new HallSceneView029{SceneId=scene.sceneId,DisplayName=scene.displayName,Summary=scene.summary,LocationTag=scene.locationTag,LocationName=RecruitChronicleBoardProjection084.FriendlyTag(scene.locationTag,"Guild Hall"),Viewed=chronicles.ViewedHallSceneIds.Contains(scene.sceneId),Deferred=RecruitChronicleRules025.HasDeferredHallScene(chronicles,_campaign.CampaignSeed,scene.sceneId)});
                if(hall.Count>=24)break;
            }
            var pairViews=bonds.Pairs.OrderByDescending(x=>PeopleBondService026.TierRank(x.TierId)).ThenByDescending(x=>x.Trust).Select(x=>new BondPairView029{PairId=x.PairId,FirstRecruitId=x.FirstRecruitId,SecondRecruitId=x.SecondRecruitId,FirstDisplayName=recruitNames.TryGetValue(x.FirstRecruitId,out var firstName)?firstName:"Guild member",SecondDisplayName=recruitNames.TryGetValue(x.SecondRecruitId,out var secondName)?secondName:"Guild member",TierId=x.TierId,TierDisplayName=bondsCatalog.Tiers.TryGetValue(x.TierId,out var tier)?tier.displayName:"New Companions",TierRank=PeopleBondService026.TierRank(x.TierId),Trust=x.Trust,Respect=x.Respect,Familiarity=x.Familiarity,SharedMemories=x.SharedMemoryCount}).ToList();
            var doctrineViews=bondsCatalog.Doctrines.Values.OrderBy(x=>x.displayName,StringComparer.Ordinal).Select(x=>new DoctrineView029{DoctrineId=x.doctrineId,DisplayName=x.displayName,MinimumBondTierId=x.minimumBondTierId,PreferredForecastIntent=x.preferredForecastIntent,Summary=x.summary}).ToList();
            var unionViews=new List<UnionPeopleView029>();
            foreach(var union in _campaign?.Guild?.Unions??Array.Empty<UnionState>())
            {
                var bondState=bonds.Unions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,union.UnionId));
                var bondDoctrineId=bondState?.DoctrineId??string.Empty;
                unionViews.Add(new UnionPeopleView029{UnionId=union.UnionId,DisplayName=union.DisplayName,DoctrineId=union.DoctrineId??string.Empty,BondDoctrineId=bondDoctrineId,BondDoctrineName=bondsCatalog.Doctrines.TryGetValue(bondDoctrineId,out var doctrine)?doctrine.displayName:"Not chosen",MemberRecruitIds=union.MemberRecruitIds});
            }
            return new PeopleRuntimePresentationState029{IsAvailable=true,SignatureProfiles=registry.Profiles.Count,PersonalQuestBoards=registry.Boards.Count,PersonalQuestNodes=registry.Boards.Values.Sum(x=>x.nodes?.Length??0),HallScenes=registry.HallEvents.Count,MentorshipLessons=registry.MentorshipLessons.Count,BondPairs=bonds.Pairs.Count,LinkArts=bondsCatalog.LinkArts.Count,CreatorCodes=CreatorRegistry028().CodeCount,CreatorRooms=CreatorRegistry028().AllRooms.Count,SaveFormat=SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion,LastCheckpointId=string.IsNullOrWhiteSpace(chronicles.LastCheckpointId)?bonds.LastCheckpointId:chronicles.LastCheckpointId,Recruits=recruitViews.AsReadOnly(),Quests=questViews.AsReadOnly(),AvailableHallScenes=hall.AsReadOnly(),Bonds=pairViews.AsReadOnly(),Doctrines=doctrineViews.AsReadOnly(),Unions=unionViews.AsReadOnly()};
        }

        bool HasPendingPersonalQuestBattle029(CampaignState candidate)=>RecruitChronicleBattleService025.IsPending(candidate);
        Result<CampaignState> ApplyPersonalQuestBattleReturn029(CampaignState candidate)
        {
            var registry=ChronicleRegistry025();var boardId=RecruitChronicleBattleService025.BoardId(candidate);var board=ProjectPersonalQuestBoard029(registry.Board(boardId));
            if(board==null)return Result<CampaignState>.Failure("Personal quest board is unavailable.");
            var committedReturn=_guildCityBattleBridge.CommitBattleReturn(candidate);if(!committedReturn.IsSuccess)return committedReturn;candidate=committedReturn.Value;
            var beforePlayable=candidate?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020;
            var beforeQuest=RecruitChronicleRules025.ActiveQuest(beforePlayable?.RecruitChronicles025,candidate.CampaignSeed,board.boardId);
            var beforeNode=beforeQuest==null?null:RecruitChronicleRules025.FindNode(board,beforeQuest.CurrentNodeId);
            var destinationNode=RecruitChronicleRules025.FindNode(board,beforeNode?.nextNodeIds?.FirstOrDefault());
            var receipt=candidate.Guild.GuildCity.PendingBattleReturn?.ReceiptId??string.Empty;
            var returned=ChronicleBattles025().ApplyClaimedBattleReturn(candidate,board);if(!returned.IsSuccess)return returned;
            var afterPlayable=returned.Value.Guild.GuildCity.Strategic017H?.Campaign019?.Playable020;
            var afterQuest=RecruitChronicleRules025.ActiveQuest(afterPlayable?.RecruitChronicles025,returned.Value.CampaignSeed,board.boardId);
            if(destinationNode==null||afterQuest==null||!afterQuest.AppliedReceiptIds.Contains(receipt)||string.IsNullOrWhiteSpace(destinationNode.rewardMemoryId))return returned;
            return _peopleBondCommands026.ApplyPersonalQuestNodeMemory(returned.Value,board,destinationNode,registry.Memory(destinationNode.rewardMemoryId),receipt);
        }

        RecruitState RuntimeRecruitForChronicleId029(string authoredRecruitId)
        {
            if(string.IsNullOrWhiteSpace(authoredRecruitId))return null;
            return (_campaign?.Guild?.Recruits??Array.Empty<RecruitState>()).FirstOrDefault(value=>
                value.AuthorityKind==RecruitAuthorityKind.Normal&&(
                    StringComparer.Ordinal.Equals(value.RecruitId,authoredRecruitId)||
                    StringComparer.Ordinal.Equals(value.AuthoredStableRecruitId,authoredRecruitId)||
                    StringComparer.Ordinal.Equals(value.TutorialAliasId,authoredRecruitId)||
                    StringComparer.Ordinal.Equals(value.SignatureId,authoredRecruitId)));
        }

        bool ChronicleIdentityMatches029(string authoredRecruitId,string runtimeOrAuthoredRecruitId)
        {
            if(StringComparer.Ordinal.Equals(authoredRecruitId,runtimeOrAuthoredRecruitId))return true;
            var recruit=RuntimeRecruitForChronicleId029(authoredRecruitId);
            return recruit!=null&&StringComparer.Ordinal.Equals(recruit.RecruitId,runtimeOrAuthoredRecruitId);
        }

        PersonalQuestBoardDefinition025 ProjectPersonalQuestBoard029(PersonalQuestBoardDefinition025 authored)
        {
            if(authored==null)return null;
            var recruit=RuntimeRecruitForChronicleId029(authored.recruitId);
            if(recruit==null)return authored;
            return new PersonalQuestBoardDefinition025
            {
                boardId=authored.boardId,recruitId=recruit.RecruitId,displayName=authored.displayName,theme=authored.theme,
                entryNodeId=authored.entryNodeId,nodes=authored.nodes,deterministic=authored.deterministic,
                reloadCannotReroll=authored.reloadCannotReroll,failureRecoverable=authored.failureRecoverable,
                canCauseDeparture=authored.canCauseDeparture
            };
        }

        RecruitChronicleProfile025 ProjectChronicleProfile029(RecruitChronicleProfile025 authored)
        {
            if(authored==null)return null;
            var recruit=RuntimeRecruitForChronicleId029(authored.recruitId);
            if(recruit==null)return null;
            return new RecruitChronicleProfile025
            {
                profileId=authored.profileId,recruitId=recruit.RecruitId,displayName=authored.displayName,raceId=authored.raceId,
                worldId=authored.worldId,startingClassId=authored.startingClassId,chronicleTitle=authored.chronicleTitle,
                coreWound=authored.coreWound,ambition=authored.ambition,fear=authored.fear,
                personalQuestBoardIds=authored.personalQuestBoardIds,hallSceneIds=authored.hallSceneIds,
                signatureTechniqueCandidateId=authored.signatureTechniqueCandidateId,legendArchetypeId=authored.legendArchetypeId,
                mentorshipAffinityId=authored.mentorshipAffinityId,unionIdentityTraitId=authored.unionIdentityTraitId,
                visualHookId=authored.visualHookId,permanentRecruit=authored.permanentRecruit,
                canInvoluntarilyLeave=authored.canInvoluntarilyLeave,
                relationshipScenesCostOperation=authored.relationshipScenesCostOperation,
                relationshipScenesExpire=authored.relationshipScenesExpire
            };
        }

        CampaignState SynchronizePeopleCreator029BeforeSave(CampaignState candidate)
        {
            if(candidate?.Guild?.GuildCity==null)return candidate;
            var city=candidate.Guild.GuildCity; var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();
            var progress=strategic.Campaign019??CampaignProgressState019.Default(); var playable=progress.Playable020??CampaignPlayableState020.Default();
            playable=RecruitChronicleSynchronizer025.Synchronize(candidate,playable);
            progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:playable.LastCheckpointId);
            strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:progress.LastCheckpointId);
            city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:strategic.LastCheckpointId);
            return candidate.With(candidate.Guild.WithGuildCity(city),candidate.OpeningFlow);
        }
    }
}
