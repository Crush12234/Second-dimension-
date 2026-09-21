using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.PeopleBonds026;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.RecruitChronicles025
{
    public sealed class RecruitChronicleCommandService025
    {
        readonly PeopleBondService026 _bonds=new PeopleBondService026();
        readonly IRecruitChronicleBoardAuthority025 _boardAuthority;
        public RecruitChronicleCommandService025(IRecruitChronicleBoardAuthority025 boardAuthority){_boardAuthority=boardAuthority;}
        public Result<CampaignState> StartPersonalQuest(CampaignState campaign,PersonalQuestBoardDefinition025 board)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);
            if(GuildCityExpeditionService017D.HasUnresolvedAdventureOutsideChronicle084(
                   campaign))
                return Result<CampaignState>.Failure(
                    "CHRONICLE025_FINISH_ACTIVE_ADVENTURE_FIRST");
            if(!RecruitChronicleBoardAuthorityRules025.TryValidate(campaign,_boardAuthority,board,out _,out error))return Result<CampaignState>.Failure(error);
            if(!RecruitChronicleRules025.IsSignedNormalRecruit(campaign,board.recruitId))return Result<CampaignState>.Failure("CHRONICLE025_SIGNED_RECRUIT_REQUIRED");
            var existing=RecruitChronicleRules025.ActiveQuest(state,campaign.CampaignSeed,board.boardId);
            if(existing!=null)
            {if(!RecruitChronicleRules025.ValidateProgress(campaign,board,existing,out error))return Result<CampaignState>.Failure(error);return Result<CampaignState>.Success(campaign);}
            if(state.PersonalQuests.Any(x=>!x.Completed&&!RecruitChronicleRules025.IsArchivedLegacyQuest(state,campaign.CampaignSeed,x)))return Result<CampaignState>.Failure("CHRONICLE025_FINISH_ACTIVE_QUEST_FIRST");
            if(state.PersonalQuests.Any(x=>x.Completed&&StringComparer.Ordinal.Equals(x.BoardId,board.boardId)))return Result<CampaignState>.Success(campaign);
            var list=state.PersonalQuests.ToList();list.Add(new PersonalQuestProgressState025(board.boardId,board.recruitId,board.entryNodeId,Array.Empty<string>(),Array.Empty<string>(),false,string.Empty,"quest_started:"+board.boardId));state=state.With(personalQuests:list.AsReadOnly(),lastCheckpointId:"quest_started:"+board.boardId);
            return Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,"quest_started:"+board.boardId);
        }
        public Result<CampaignState> RecoverInvalidPersonalQuest(CampaignState campaign,string boardId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);
            if(string.IsNullOrWhiteSpace(boardId))return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_BOARD_REQUIRED");
            var unfinished=state.PersonalQuests.Where(value=>!value.Completed&&StringComparer.Ordinal.Equals(value.BoardId,boardId)).ToArray();
            var active=unfinished.Where(value=>!RecruitChronicleRules025.IsArchivedLegacyQuest(state,campaign.CampaignSeed,value)).ToArray();
            if(active.Length>1)return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_RECORD_AMBIGUOUS");
            var archived=unfinished.Where(value=>RecruitChronicleRules025.IsArchivedLegacyQuest(state,campaign.CampaignSeed,value)).ToArray();
            if(active.Length==0&&archived.Length>1)return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_RECORD_AMBIGUOUS");
            var alreadyArchived=active.Length==0;
            var legacy=active.FirstOrDefault()??archived.FirstOrDefault();
            if(legacy==null)return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_ACTIVE_QUEST_REQUIRED");
            PersonalQuestBoardDefinition025 projected=null;
            if(!RecruitChronicleBoardAuthorityRules025.TryProjectCanonicalBoard(campaign,_boardAuthority,
                   boardId,legacy.RecruitId,out projected))
                foreach(var recruit in (campaign.Guild.Recruits??Array.Empty<RecruitState>()).OrderBy(value=>value.RecruitId,StringComparer.Ordinal))
                    if(RecruitChronicleBoardAuthorityRules025.TryProjectCanonicalBoard(campaign,_boardAuthority,
                        boardId,recruit.RecruitId,out projected))break;
            if(!alreadyArchived&&projected!=null&&StringComparer.Ordinal.Equals(projected.recruitId,legacy.RecruitId)&&
               RecruitChronicleRules025.ValidateProgress(campaign,projected,legacy,out _))
                return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_NOT_REQUIRED");

            var request=city.PendingEncounter;var battleReturn=city.PendingBattleReturn;
            if(request==null&&battleReturn!=null)
                return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_HANDOFF_LINK_UNPROVEN");
            var requestClaimsBoard=LegacyRequestClaimsBoard(request,boardId);
            var quarantineHandoff=false;var clearRelatedBattle=false;
            if(requestClaimsBoard)
            {
                if(!TryProveLegacyHandoff(campaign,state,legacy,request,battleReturn,out clearRelatedBattle))
                    return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_HANDOFF_LINK_UNPROVEN");
                quarantineHandoff=true;
            }
            else if(alreadyArchived)
                return Result<CampaignState>.Failure("CHRONICLE025_RECOVERY_HANDOFF_NOT_FOUND");

            var archiveReceipt=RecruitChronicleRules025.LegacyQuestArchiveReceiptId(campaign.CampaignSeed,legacy);
            var receipts=state.AppliedReceiptIds.ToList();if(!receipts.Contains(archiveReceipt))receipts.Add(archiveReceipt);
            if(quarantineHandoff)
            {
                var handoffReceipt=RecruitChronicleRules025.LegacyQuestHandoffArchiveReceiptId(campaign.CampaignSeed,legacy,
                    request.RequestId,request.BattleId,battleReturn?.ReceiptId);
                if(!receipts.Contains(handoffReceipt))receipts.Add(handoffReceipt);
                city=city.With(pendingEncounter:null,replacePendingEncounter:true,pendingBattleReturn:null,
                    replacePendingBattleReturn:true,lastCheckpointId:"quest_handoff_recovered:"+boardId);
            }
            var list=state.PersonalQuests.ToList();
            var archivedState=state.With(appliedReceiptIds:receipts.AsReadOnly());
            var hasCanonicalActive=list.Any(value=>!value.Completed&&StringComparer.Ordinal.Equals(value.BoardId,boardId)&&
                !RecruitChronicleRules025.IsArchivedLegacyQuest(archivedState,campaign.CampaignSeed,value));
            if(projected!=null&&!hasCanonicalActive&&!list.Any(value=>value.Completed&&StringComparer.Ordinal.Equals(value.BoardId,boardId)))
                list.Add(new PersonalQuestProgressState025(projected.boardId,projected.recruitId,projected.entryNodeId,
                    Array.Empty<string>(),Array.Empty<string>(),false,string.Empty,"quest_recovered:"+boardId));
            var checkpoint=quarantineHandoff?"quest_handoff_recovered:"+boardId:"quest_recovered:"+boardId;
            state=state.With(personalQuests:list.AsReadOnly(),appliedReceiptIds:receipts.AsReadOnly(),lastCheckpointId:checkpoint);
            var recovered=Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,checkpoint);
            if(!recovered.IsSuccess||!clearRelatedBattle)return recovered;
            return Result<CampaignState>.Success(recovered.Value.WithBattle(null));
        }

        static bool LegacyRequestClaimsBoard(EncounterLaunchRequest017D request,string boardId)
        {
            if(request==null)return false;
            return StringComparer.Ordinal.Equals(request.BoardId,boardId)||
                   StringComparer.Ordinal.Equals(request.ContractId,RecruitChronicleBattleService025.ContractPrefix+boardId)||
                   StringComparer.Ordinal.Equals(request.ExpeditionId,"PQ_EXPEDITION_029_"+boardId);
        }

        static bool TryProveLegacyHandoff(CampaignState campaign,RecruitChronicleState025 state,
            PersonalQuestProgressState025 legacy,EncounterLaunchRequest017D request,BattleReturnReceipt017D battleReturn,
            out bool clearRelatedBattle)
        {
            clearRelatedBattle=false;
            if(campaign?.Guild?.GuildCity==null||state==null||legacy==null||request==null)return false;
            if(state.PersonalQuests.Count(value=>!value.Completed&&
                   StringComparer.Ordinal.Equals(value.BoardId,legacy.BoardId)&&
                   StringComparer.Ordinal.Equals(value.RecruitId,legacy.RecruitId)&&
                   StringComparer.Ordinal.Equals(value.CurrentNodeId,legacy.CurrentNodeId))!=1)return false;
            if(!TryLegacyAllies(campaign,request.AlliedUnionIds,out var allies))return false;
            var seed=CanonicalJson.Sha256Hex(new{campaign.CampaignSeed,boardId=legacy.BoardId,nodeId=legacy.CurrentNodeId,allies});
            var objectives=new[]{"OBJECTIVE_PERSONAL_QUEST_029","OBJECTIVE_"+legacy.CurrentNodeId}.OrderBy(value=>value,StringComparer.Ordinal).ToArray();
            var modifiers=new[]{"PERSONAL_QUEST_029","RECRUIT_"+legacy.RecruitId}.OrderBy(value=>value,StringComparer.Ordinal).ToArray();
            if(!StringComparer.Ordinal.Equals(request.RequestId,"PQ_ENCOUNTER_029_"+seed.Substring(0,24).ToUpperInvariant())||
               !StringComparer.Ordinal.Equals(request.ContractId,RecruitChronicleBattleService025.ContractPrefix+legacy.BoardId)||
               !StringComparer.Ordinal.Equals(request.ExpeditionId,"PQ_EXPEDITION_029_"+legacy.BoardId)||
               !StringComparer.Ordinal.Equals(request.BoardId,legacy.BoardId)||
               !StringComparer.Ordinal.Equals(request.NodeId,legacy.CurrentNodeId)||
               !StringComparer.Ordinal.Equals(request.BattleId,"BATTLE_PQ_029_"+seed.Substring(0,18).ToUpperInvariant())||
               request.EnemyUnionCount!=1+(int)(Convert.ToUInt32(seed.Substring(0,8),16)%3u)||
               !StringComparer.Ordinal.Equals(request.CanonicalSeedIdentity,seed)||
               !SequenceEqual025(request.AlliedUnionIds,allies)||request.ReserveUnionIds.Count!=0||
               !SequenceEqual025(request.ObjectiveIds,objectives)||!SequenceEqual025(request.RouteModifiers,modifiers)||
               request.Supplies!=10||request.Fatigue!=0||request.Urgency!=0||
               !StringComparer.Ordinal.Equals(request.ReturnCheckpointId,"RETURN_PERSONAL_QUEST_029_"+legacy.CurrentNodeId)||
               !IsSha256025(request.PreBattleStateHash))return false;

            var battle=campaign.Battle;
            var relatedBattle=battle!=null&&StringComparer.Ordinal.Equals(battle.BattleId,request.BattleId);
            if(relatedBattle)
            {
                var battleAllies=battle.PlayerUnions.Select(value=>value.UnionId).OrderBy(value=>value,StringComparer.Ordinal).ToArray();
                if(!SequenceEqual025(battleAllies,allies))return false;
                clearRelatedBattle=true;
            }
            if(battleReturn==null)return true;
            if(!relatedBattle||battle.Phase!=BattlePhase.Resolved||battle.Outcome==BattleOutcome.InProgress||battle.Reward==null||
               !IsSha256025(battle.FinalStateHash)||
               !M2BattleCommandService.HasValidFinalStateHash090(battle))return false;
            var receiptHash=CanonicalJson.Sha256Hex(new
                {request.RequestId,battle.BattleId,battle.FinalStateHash,battle.Outcome,battle.Reward.RewardId});
            var expectedReceipt="BATTLE_RETURN_"+receiptHash.Substring(0,24).ToUpperInvariant();
            return StringComparer.Ordinal.Equals(battleReturn.ReceiptId,expectedReceipt)&&
                   StringComparer.Ordinal.Equals(battleReturn.LaunchRequestId,request.RequestId)&&
                   StringComparer.Ordinal.Equals(battleReturn.BattleRunId,request.BattleId)&&
                   StringComparer.Ordinal.Equals(battleReturn.Outcome,battle.Outcome.ToString())&&
                   StringComparer.Ordinal.Equals(battleReturn.BattleResultHash,battle.FinalStateHash)&&
                   StringComparer.Ordinal.Equals(battleReturn.EquipmentRewardReceiptId,battle.Reward.RewardId)&&
                   StringComparer.Ordinal.Equals(battleReturn.ReturnCheckpointId,request.ReturnCheckpointId)&&
                   !battleReturn.Applied&&!campaign.Guild.GuildCity.AppliedBattleReturnIds.Contains(battleReturn.ReceiptId);
        }

        static bool TryLegacyAllies(CampaignState campaign,IReadOnlyList<string> requested,out IReadOnlyList<string> allies)
        {
            allies=Array.Empty<string>();if(requested==null||requested.Count==0||requested.Count>10)return false;
            var result=new List<string>();
            foreach(var unionId in requested)
            {
                if(string.IsNullOrWhiteSpace(unionId)||result.Contains(unionId))return false;
                var union=campaign.Guild.Unions.FirstOrDefault(value=>StringComparer.Ordinal.Equals(value.UnionId,unionId));
                if(union==null||union.Kind!=UnionKind.Normal||union.MemberRecruitIds==null||union.MemberRecruitIds.Count==0)return false;
                result.Add(unionId);
            }
            result.Sort(StringComparer.Ordinal);allies=result.AsReadOnly();return true;
        }

        static bool SequenceEqual025(IReadOnlyList<string> left,IReadOnlyList<string> right)
        {if(left==null||right==null||left.Count!=right.Count)return false;for(var index=0;index<left.Count;index++)if(!StringComparer.Ordinal.Equals(left[index],right[index]))return false;return true;}
        static bool IsSha256025(string value)
        {if(string.IsNullOrWhiteSpace(value)||value.Length!=64)return false;for(var index=0;index<value.Length;index++)if(!Uri.IsHexDigit(value[index]))return false;return true;}
        public Result<CampaignState> AdvancePersonalQuest(CampaignState campaign,PersonalQuestBoardDefinition025 board,string destinationNodeId,string receiptId,string existingBattleRewardReceiptId="")
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);
            if(!RecruitChronicleBoardAuthorityRules025.TryValidate(campaign,_boardAuthority,board,out _,out error))return Result<CampaignState>.Failure(error);
            var list=state.PersonalQuests.ToList();var index=list.FindIndex(x=>!x.Completed&&StringComparer.Ordinal.Equals(x.BoardId,board.boardId)&&!RecruitChronicleRules025.IsArchivedLegacyQuest(state,campaign.CampaignSeed,x));if(index<0)return Result<CampaignState>.Failure("CHRONICLE025_QUEST_NOT_STARTED");var q=list[index];
            if(!StringComparer.Ordinal.Equals(q.RecruitId,board.recruitId))return Result<CampaignState>.Failure("CHRONICLE025_QUEST_RECRUIT_MISMATCH");
            if(!RecruitChronicleRules025.ValidateProgress(campaign,board,q,out error))return Result<CampaignState>.Failure(error);
            if(q.Completed)return Result<CampaignState>.Failure("CHRONICLE025_QUEST_ALREADY_COMPLETED");
            var current=RecruitChronicleRules025.FindNode(board,q.CurrentNodeId);if(current==null)return Result<CampaignState>.Failure("CHRONICLE025_CURRENT_NODE_REQUIRED");
            if(StringComparer.Ordinal.Equals(current.kind,"BATTLE"))return Result<CampaignState>.Failure("CHRONICLE025_CERTIFIED_BATTLE_RETURN_REQUIRED");
            if(!string.IsNullOrWhiteSpace(existingBattleRewardReceiptId))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_REWARD_FOR_REGULAR_MOVE_REJECTED");
            if(!RecruitChronicleRules025.CanMove(board,q.CurrentNodeId,destinationNodeId))return Result<CampaignState>.Failure("CHRONICLE025_ILLEGAL_NODE_TRANSITION");
            var expected=RecruitChronicleRules025.PersonalQuestMoveReceiptId(campaign.CampaignSeed,board.boardId,destinationNodeId,q.RecruitId);
            if(!StringComparer.Ordinal.Equals(receiptId,expected))return Result<CampaignState>.Failure("CHRONICLE025_CANONICAL_MOVE_RECEIPT_REQUIRED");
            if(q.AppliedReceiptIds.Contains(expected))return Result<CampaignState>.Failure("CHRONICLE025_RECEIPT_ALREADY_APPLIED");
            var destination=RecruitChronicleRules025.FindNode(board,destinationNodeId);if(destination==null)return Result<CampaignState>.Failure("CHRONICLE025_DESTINATION_NODE_REQUIRED");
            return CommitQuestMove(campaign,city,strategic,progress,playable,state,list,index,q,destinationNodeId,expected,q.ExistingBattleRewardReceiptId,StringComparer.Ordinal.Equals(destination.kind,"RETURN"));
        }

        internal Result<CampaignState> AdvancePersonalQuestBattleReturn(CampaignState campaign,PersonalQuestBoardDefinition025 board,string destinationNodeId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);
            if(!RecruitChronicleBoardAuthorityRules025.TryValidate(campaign,_boardAuthority,board,out _,out error))return Result<CampaignState>.Failure(error);
            var list=state.PersonalQuests.ToList();var index=list.FindIndex(x=>!x.Completed&&StringComparer.Ordinal.Equals(x.BoardId,board.boardId)&&!RecruitChronicleRules025.IsArchivedLegacyQuest(state,campaign.CampaignSeed,x));if(index<0)return Result<CampaignState>.Failure("CHRONICLE025_QUEST_NOT_STARTED");var q=list[index];
            if(!StringComparer.Ordinal.Equals(q.RecruitId,board.recruitId))return Result<CampaignState>.Failure("CHRONICLE025_QUEST_RECRUIT_MISMATCH");
            if(!RecruitChronicleRules025.ValidateProgress(campaign,board,q,out error))return Result<CampaignState>.Failure(error);
            if(q.Completed)return Result<CampaignState>.Failure("CHRONICLE025_QUEST_ALREADY_COMPLETED");
            var current=RecruitChronicleRules025.FindNode(board,q.CurrentNodeId);if(current==null||!StringComparer.Ordinal.Equals(current.kind,"BATTLE"))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_NODE_REQUIRED");
            if(!RecruitChronicleRules025.CanMove(board,q.CurrentNodeId,destinationNodeId))return Result<CampaignState>.Failure("CHRONICLE025_ILLEGAL_NODE_TRANSITION");
            if(!RecruitChronicleBattleService025.ValidateCanonicalBattleReturn(campaign,board,q,current,out var battleReturnError))return Result<CampaignState>.Failure(battleReturnError);
            var battleReturn=city.PendingBattleReturn;var rewardId=campaign.Battle.Reward.RewardId;
            if(q.AppliedReceiptIds.Contains(battleReturn.ReceiptId)||city.AppliedBattleReturnIds.Contains(battleReturn.ReceiptId))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_RETURN_ALREADY_APPLIED");
            var destination=RecruitChronicleRules025.FindNode(board,destinationNodeId);if(destination==null)return Result<CampaignState>.Failure("CHRONICLE025_DESTINATION_NODE_REQUIRED");
            var proof=RecruitChronicleRules025.PersonalQuestBattleProofReceiptId(campaign.CampaignSeed,
                board.boardId,current.nodeId,destinationNodeId,q.RecruitId,battleReturn.ReceiptId,rewardId);
            return CommitQuestMove(campaign,city,strategic,progress,playable,state,list,index,q,destinationNodeId,
                battleReturn.ReceiptId,rewardId,StringComparer.Ordinal.Equals(destination.kind,"RETURN"),proof);
        }

        static Result<CampaignState> CommitQuestMove(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,
            CampaignProgressState019 progress,CampaignPlayableState020 playable,RecruitChronicleState025 state,List<PersonalQuestProgressState025> list,
            int index,PersonalQuestProgressState025 q,string destinationNodeId,string receiptId,string battleRewardReceiptId,bool finished,
            string additionalReceiptId=null)
        {
            var completed=q.CompletedNodeIds.ToList();if(!completed.Contains(q.CurrentNodeId))completed.Add(q.CurrentNodeId);var receipts=q.AppliedReceiptIds.ToList();receipts.Add(receiptId);if(!string.IsNullOrWhiteSpace(additionalReceiptId))receipts.Add(additionalReceiptId);
            q=q.With(currentNodeId:destinationNodeId,completedNodeIds:completed.AsReadOnly(),appliedReceiptIds:receipts.AsReadOnly(),completed:finished,existingBattleRewardReceiptId:battleRewardReceiptId,lastCheckpointId:"quest_node:"+destinationNodeId);list[index]=q;state=state.With(personalQuests:list.AsReadOnly(),lastCheckpointId:"quest_node:"+destinationNodeId);
            return Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,"quest_node:"+destinationNodeId);
        }
        public Result<CampaignState> ViewHallScene(CampaignState campaign,HallSocialEventDefinition025 scene)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);if(scene==null)return Result<CampaignState>.Failure("CHRONICLE025_SCENE_REQUIRED");if(scene.operationCost!=0||scene.expires||scene.canCauseDeparture)return Result<CampaignState>.Failure("CHRONICLE025_SCENE_LAW_VIOLATION");if(state.ViewedHallSceneIds.Contains(scene.sceneId))return Result<CampaignState>.Success(campaign);var viewed=state.ViewedHallSceneIds.ToList();viewed.Add(scene.sceneId);state=state.With(viewedHallSceneIds:viewed.AsReadOnly(),lastCheckpointId:"hall_scene:"+scene.sceneId);return Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,"hall_scene:"+scene.sceneId);
        }
        public Result<CampaignState> DeferHallScene(CampaignState campaign,HallSocialEventDefinition025 scene)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);
            if(scene==null||string.IsNullOrWhiteSpace(scene.sceneId))return Result<CampaignState>.Failure("CHRONICLE025_SCENE_REQUIRED");
            if(scene.operationCost!=0||scene.expires||scene.canCauseDeparture||!scene.deferWithoutPenalty)return Result<CampaignState>.Failure("CHRONICLE025_SCENE_DEFERRAL_LAW_VIOLATION");
            var receipt=RecruitChronicleRules025.HallSceneDeferralReceiptId(campaign.CampaignSeed,scene.sceneId);if(state.AppliedReceiptIds.Contains(receipt))return Result<CampaignState>.Success(campaign);
            if(state.ViewedHallSceneIds.Contains(scene.sceneId))return Result<CampaignState>.Failure("CHRONICLE025_SCENE_ALREADY_VIEWED");
            var receipts=state.AppliedReceiptIds.ToList();receipts.Add(receipt);state=state.With(appliedReceiptIds:receipts.AsReadOnly(),lastCheckpointId:"hall_scene_deferred:"+scene.sceneId);
            return Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,"hall_scene_deferred:"+scene.sceneId);
        }
        public Result<CampaignState> CompleteMentorship(CampaignState campaign,MentorshipLessonDefinition025 lesson,string mentorId,string studentId,string receiptId,RelationshipMemoryDefinition025 memory)
        {
            if(lesson==null)return Result<CampaignState>.Failure("CHRONICLE025_LESSON_REQUIRED");if(lesson.operationCost!=0||lesson.canCauseDeparture||!lesson.parallelProgress)return Result<CampaignState>.Failure("CHRONICLE025_MENTORSHIP_LAW_VIOLATION");
            var applied=_bonds.ApplyMemory(campaign,memory,mentorId,studentId,receiptId,"MENTORSHIP:"+lesson.lessonId);if(!applied.IsSuccess)return applied;campaign=applied.Value;
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);if(state.CompletedMentorshipLessonIds.Contains(lesson.lessonId))return Result<CampaignState>.Success(campaign);var completed=state.CompletedMentorshipLessonIds.ToList();completed.Add(lesson.lessonId);state=state.With(completedMentorshipLessonIds:completed.AsReadOnly(),lastCheckpointId:"mentorship:"+lesson.lessonId);return Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,"mentorship:"+lesson.lessonId);
        }
        public Result<CampaignState> RecordMemory(CampaignState campaign,RelationshipMemoryDefinition025 memory,string firstId,string secondId,string receiptId,string sourceId)=>_bonds.ApplyMemory(campaign,memory,firstId,secondId,receiptId,sourceId);
        public Result<CampaignState> UnlockLegendAndTechnique(CampaignState campaign,RecruitChronicleProfile025 profile,LegendArchetypeDefinition025 legend,SignatureTechniqueCandidateDefinition025 technique)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var state,out var error))return Result<CampaignState>.Failure(error);if(profile==null||legend==null||technique==null)return Result<CampaignState>.Failure("CHRONICLE025_LEGEND_CONTENT_REQUIRED");var recruit=campaign.Guild.Recruits.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.RecruitId,profile.recruitId));if(recruit==null)return Result<CampaignState>.Failure("CHRONICLE025_RECRUIT_REQUIRED");
            var completed=state.PersonalQuests.Count(x=>StringComparer.Ordinal.Equals(x.RecruitId,profile.recruitId)&&x.Completed);var mems=RecruitChronicleRules025.MeaningfulMemoryCount(state,profile.recruitId);var mastery=RecruitChronicleRules025.TotalMastery(recruit);if(completed<legend.requiredPersonalQuestCount||mems<legend.requiredMeaningfulMemories||mastery<legend.requiredMasteryPoints)return Result<CampaignState>.Failure("CHRONICLE025_LEGEND_GATES_NOT_MET");if(!RecruitChronicleRules025.HasCompletedBoard(state,technique.requiredBoardId)||mastery<technique.requiredMasteryPoints)return Result<CampaignState>.Failure("CHRONICLE025_TECHNIQUE_GATES_NOT_MET");
            var legends=state.UnlockedLegendIds.ToList();if(!legends.Contains(legend.archetypeId))legends.Add(legend.archetypeId);var techniques=state.UnlockedSignatureTechniqueIds.ToList();if(!techniques.Contains(technique.candidateId))techniques.Add(technique.candidateId);state=state.With(unlockedLegendIds:legends.AsReadOnly(),unlockedSignatureTechniqueIds:techniques.AsReadOnly(),lastCheckpointId:"legend:"+profile.recruitId);return Success(campaign,city,strategic,progress,playable,state,playable.PeopleBonds026,"legend:"+profile.recruitId);
        }
        static bool TryContext(CampaignState c,out GuildCityState017D city,out GuildCityStrategicState017H strategic,out CampaignProgressState019 progress,out CampaignPlayableState020 playable,out RecruitChronicleState025 state,out string error){city=null;strategic=null;progress=null;playable=null;state=null;error=string.Empty;if(c?.Guild?.GuildCity==null){error="CHRONICLE025_CAMPAIGN_REQUIRED";return false;}city=c.Guild.GuildCity;strategic=city.Strategic017H??GuildCityStrategicState017H.Default();progress=strategic.Campaign019??CampaignProgressState019.Default();playable=progress.Playable020??CampaignPlayableState020.Default();state=playable.RecruitChronicles025??RecruitChronicleState025.Default();return true;}
        static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,CampaignProgressState019 progress,CampaignPlayableState020 playable,RecruitChronicleState025 state,PeopleBondState026 bonds,string checkpoint){playable=playable.With(recruitChronicles025:state,replaceRecruitChronicles025:true,peopleBonds026:bonds??PeopleBondState026.Default(),replacePeopleBonds026:true,lastCheckpointId:checkpoint);progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:checkpoint);strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:checkpoint);city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:checkpoint);return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));}
    }
}
