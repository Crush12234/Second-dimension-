using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.RecruitChronicles025
{
    public sealed class RecruitChronicleBattleService025
    {
        public const string ContractPrefix="PERSONAL_QUEST_029_";
        readonly IRecruitChronicleBoardAuthority025 _boardAuthority;
        readonly RecruitChronicleCommandService025 _commands;
        public RecruitChronicleBattleService025(IRecruitChronicleBoardAuthority025 boardAuthority)
        {_boardAuthority=boardAuthority;_commands=new RecruitChronicleCommandService025(boardAuthority);}
        public Result<CampaignState> CommitEncounter(
            CampaignState campaign,
            PersonalQuestBoardDefinition025 board,
            IReadOnlyList<string> alliedUnionIds)
        {
            if(campaign?.Guild?.GuildCity==null)return Result<CampaignState>.Failure("CHRONICLE025_CAMPAIGN_REQUIRED");
            if(!RecruitChronicleBoardAuthorityRules025.TryValidate(campaign,_boardAuthority,board,out _,out var authorityError))return Result<CampaignState>.Failure(authorityError);
            var playable=campaign.Guild.GuildCity.Strategic017H?.Campaign019?.Playable020;
            if(playable?.ActiveOperation!=null)return Result<CampaignState>.Failure("CHRONICLE025_CAMPAIGN_OPERATION_ACTIVE");
            var quest=RecruitChronicleRules025.ActiveQuest(playable?.RecruitChronicles025,campaign.CampaignSeed,board.boardId);
            if(quest==null||quest.Completed)return Result<CampaignState>.Failure("CHRONICLE025_ACTIVE_QUEST_REQUIRED");
            if(!StringComparer.Ordinal.Equals(quest.RecruitId,board.recruitId)||!RecruitChronicleRules025.IsSignedNormalRecruit(campaign,board.recruitId))
                return Result<CampaignState>.Failure("CHRONICLE025_QUEST_RECRUIT_MISMATCH");
            if(!RecruitChronicleRules025.ValidateProgress(campaign,board,quest,out var progressError))return Result<CampaignState>.Failure(progressError);
            var node=RecruitChronicleRules025.FindNode(board,quest.CurrentNodeId);
            if(node==null||!StringComparer.Ordinal.Equals(node.kind,"BATTLE"))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_NODE_REQUIRED");
            if(!TryCanonicalAlliedUnions(campaign,alliedUnionIds,out var allies,out var alliedError))return Result<CampaignState>.Failure(alliedError);
            var city=campaign.Guild.GuildCity;
            if(city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CHRONICLE025_APPLY_PENDING_BATTLE_RETURN_FIRST");
            if(city.PendingEncounter!=null)
            {
                if(!ValidateCanonicalRequest(campaign,board,quest,node,city.PendingEncounter,out var requestError))return Result<CampaignState>.Failure(requestError);
                if(!SequenceEqual(city.PendingEncounter.AlliedUnionIds,allies))return Result<CampaignState>.Failure("CHRONICLE025_ALLIED_UNION_IDENTITY_MISMATCH");
                var existingAuthority=GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(city.PendingEncounter);
                if(!campaign.Guild.Development.HasAdventureAuthority(existingAuthority))return Result<CampaignState>.Failure("CHRONICLE025_ENCOUNTER_AUTHORITY_INVALID");
                return Result<CampaignState>.Success(campaign);
            }
            if(campaign.Battle!=null&&campaign.Battle.Phase!=BattlePhase.Resolved)return Result<CampaignState>.Failure("CHRONICLE025_FINISH_ACTIVE_BATTLE_FIRST");
            var request=CreateCanonicalRequest(campaign,board,node,allies);
            var requestAuthority=GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request);
            var development=campaign.Guild.Development;
            if(development.HasAdventureAuthority(requestAuthority))return Result<CampaignState>.Failure("CHRONICLE025_ENCOUNTER_AUTHORITY_ALREADY_COMMITTED");
            if(!development.CanRecordAdventureAuthority(requestAuthority))return Result<CampaignState>.Failure("CHRONICLE025_ADVENTURE_AUTHORITY_LEDGER_FULL");
            development=development.RecordAdventureAuthority(requestAuthority);
            campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development),campaign.OpeningFlow);
            city=city.With(pendingEncounter:request,replacePendingEncounter:true,lastCheckpointId:"personal_quest_battle_committed:"+board.boardId);
            return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
        }

        public Result<CampaignState> ApplyClaimedBattleReturn(CampaignState campaign,PersonalQuestBoardDefinition025 board)
        {
            if(campaign?.Guild?.GuildCity==null)return Result<CampaignState>.Failure("CHRONICLE025_CAMPAIGN_REQUIRED");
            if(!RecruitChronicleBoardAuthorityRules025.TryValidate(campaign,_boardAuthority,board,out _,out var authorityError))return Result<CampaignState>.Failure(authorityError);
            var playable=campaign.Guild.GuildCity.Strategic017H?.Campaign019?.Playable020;
            var quest=RecruitChronicleRules025.ActiveQuest(playable?.RecruitChronicles025,campaign.CampaignSeed,board.boardId);
            if(quest==null||quest.Completed)return Result<CampaignState>.Failure("CHRONICLE025_ACTIVE_QUEST_REQUIRED");
            if(!StringComparer.Ordinal.Equals(quest.RecruitId,board.recruitId))return Result<CampaignState>.Failure("CHRONICLE025_QUEST_RECRUIT_MISMATCH");
            if(!RecruitChronicleRules025.ValidateProgress(campaign,board,quest,out var progressError))return Result<CampaignState>.Failure(progressError);
            var node=RecruitChronicleRules025.FindNode(board,quest.CurrentNodeId);
            if(node==null||!StringComparer.Ordinal.Equals(node.kind,"BATTLE"))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_NODE_REQUIRED");
            if(!ValidateCanonicalTerminalReturn(campaign,board,quest,node,false,out var returnError))return Result<CampaignState>.Failure(returnError);
            var returnReceiptId=campaign.Guild.GuildCity.PendingBattleReturn.ReceiptId;
            var candidate=campaign;
            if(campaign.Battle.Outcome==BattleOutcome.Victory)
            {
                var next=node.nextNodeIds?.FirstOrDefault();
                if(string.IsNullOrWhiteSpace(next))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_RETURN_NODE_REQUIRED");
                var advanced=_commands.AdvancePersonalQuestBattleReturn(candidate,board,next);
                if(!advanced.IsSuccess)return advanced;candidate=advanced.Value;
            }
            var city=candidate.Guild.GuildCity;
            var applied=city.AppliedBattleReturnIds.ToList();
            if(applied.Contains(returnReceiptId))return Result<CampaignState>.Failure("CHRONICLE025_BATTLE_RETURN_ALREADY_APPLIED");
            applied.Add(returnReceiptId);applied.Sort(StringComparer.Ordinal);
            city=city.With(pendingEncounter:null,replacePendingEncounter:true,pendingBattleReturn:null,replacePendingBattleReturn:true,
                appliedBattleReturnIds:applied.AsReadOnly(),lastCheckpointId:"personal_quest_battle_return:"+board.boardId);
            return Result<CampaignState>.Success(candidate.With(candidate.Guild.WithGuildCity(city),candidate.OpeningFlow));
        }
        internal static bool ValidateCanonicalBattleReturn(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 quest,PersonalQuestNodeDefinition025 node,out string error)=>
            ValidateCanonicalTerminalReturn(campaign,board,quest,node,true,out error);

        static bool ValidateCanonicalTerminalReturn(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 quest,PersonalQuestNodeDefinition025 node,bool requireVictory,out string error)
        {
            error=string.Empty;
            var city=campaign?.Guild?.GuildCity;var request=city?.PendingEncounter;var battleReturn=city?.PendingBattleReturn;var battle=campaign?.Battle;
            if(request==null||battleReturn==null||battle==null){error="CHRONICLE025_PENDING_BATTLE_RETURN_REQUIRED";return false;}
            if(!ValidateCanonicalRequest(campaign,board,quest,node,request,out error))return false;
            if(!campaign.Guild.Development.HasAdventureAuthority(
                   GuildCityBattleBridgeService017D
                       .EncounterRequestAuthorityId084(request)))
            {error="CHRONICLE025_ENCOUNTER_AUTHORITY_INVALID";return false;}
            if(!StringComparer.Ordinal.Equals(battle.BattleId,request.BattleId)){error="CHRONICLE025_BATTLE_ID_MISMATCH";return false;}
            if(battle.Phase!=BattlePhase.Resolved||battle.Outcome==BattleOutcome.InProgress||battle.Reward==null||!battle.Reward.Claimed)
            {error="CHRONICLE025_CLAIM_REWARD_FIRST";return false;}
            if(requireVictory&&battle.Outcome!=BattleOutcome.Victory){error="CHRONICLE025_VICTORY_REQUIRED";return false;}
            if(battle.Reward.Outcome!=battle.Outcome){error="CHRONICLE025_BATTLE_REWARD_OUTCOME_MISMATCH";return false;}
            var battleAllies=battle.PlayerUnions.Select(x=>x.UnionId).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            if(!SequenceEqual(battleAllies,request.AlliedUnionIds)){error="CHRONICLE025_BATTLE_ALLIED_UNION_MISMATCH";return false;}
            if(!IsSha256(battle.FinalStateHash)||!M2BattleCommandService.HasValidFinalStateHash090(battle))
            {error="CHRONICLE025_BATTLE_HASH_INVALID";return false;}
            if(!campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId))
            {error="CHRONICLE025_EXISTING_REWARD_LINK_REQUIRED";return false;}
            var hash=CanonicalJson.Sha256Hex(new{request.RequestId,battle.BattleId,battle.FinalStateHash,battle.Outcome,battle.Reward.RewardId});
            var expectedReceiptId="BATTLE_RETURN_"+hash.Substring(0,24).ToUpperInvariant();
            if(!StringComparer.Ordinal.Equals(battleReturn.ReceiptId,expectedReceiptId)||
               !StringComparer.Ordinal.Equals(battleReturn.LaunchRequestId,request.RequestId)||
               !StringComparer.Ordinal.Equals(battleReturn.BattleRunId,request.BattleId)||
               !StringComparer.Ordinal.Equals(battleReturn.Outcome,battle.Outcome.ToString())||
               !StringComparer.Ordinal.Equals(battleReturn.BattleResultHash,battle.FinalStateHash)||
               !StringComparer.Ordinal.Equals(battleReturn.EquipmentRewardReceiptId,battle.Reward.RewardId)||
               !StringComparer.Ordinal.Equals(battleReturn.ReturnCheckpointId,request.ReturnCheckpointId)||battleReturn.Applied)
            {error="CHRONICLE025_BATTLE_RETURN_IDENTITY_MISMATCH";return false;}
            if(city.AppliedBattleReturnIds.Contains(battleReturn.ReceiptId)){error="CHRONICLE025_BATTLE_RETURN_ALREADY_APPLIED";return false;}
            return true;
        }

        static EncounterLaunchRequest017D CreateCanonicalRequest(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestNodeDefinition025 node,IReadOnlyList<string> allies)
        {
            var seed=CanonicalJson.Sha256Hex(new{campaign.CampaignSeed,board.boardId,node.nodeId,allies});
            return new EncounterLaunchRequest017D(
                "PQ_ENCOUNTER_029_"+seed.Substring(0,24).ToUpperInvariant(),ContractPrefix+board.boardId,
                "PQ_EXPEDITION_029_"+board.boardId,board.boardId,node.nodeId,
                string.IsNullOrWhiteSpace(node.battleProfileId)?"PQ_BATTLE_PROFILE_029":node.battleProfileId,
                "BATTLE_PQ_029_"+seed.Substring(0,18).ToUpperInvariant(),BattleObjective(board),
                1+(int)(Convert.ToUInt32(seed.Substring(0,8),16)%3u),seed,allies,Array.Empty<string>(),
                new[]{"OBJECTIVE_PERSONAL_QUEST_029","OBJECTIVE_"+node.nodeId},
                new[]{"PERSONAL_QUEST_029","RECRUIT_"+board.recruitId},10,0,0,
                "RETURN_PERSONAL_QUEST_029_"+node.nodeId,CanonicalJson.Sha256Hex(campaign));
        }

        static bool ValidateCanonicalRequest(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 quest,PersonalQuestNodeDefinition025 node,EncounterLaunchRequest017D request,out string error)
        {
            error=string.Empty;
            if(campaign?.Guild?.GuildCity==null||board==null||quest==null||node==null||request==null||
               !StringComparer.Ordinal.Equals(quest.BoardId,board.boardId)||!StringComparer.Ordinal.Equals(quest.RecruitId,board.recruitId)||
               !StringComparer.Ordinal.Equals(quest.CurrentNodeId,node.nodeId))
            {error="CHRONICLE025_ACTIVE_QUEST_IDENTITY_MISMATCH";return false;}
            if(!TryCanonicalAlliedUnions(campaign,request.AlliedUnionIds,out var allies,out error))return false;
            var seed=CanonicalJson.Sha256Hex(new{campaign.CampaignSeed,board.boardId,node.nodeId,allies});
            var expectedObjectives=new[]{"OBJECTIVE_PERSONAL_QUEST_029","OBJECTIVE_"+node.nodeId}.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            var expectedModifiers=new[]{"PERSONAL_QUEST_029","RECRUIT_"+board.recruitId}.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            var expectedObjective=BattleObjective(board);
            if(!StringComparer.Ordinal.Equals(request.RequestId,"PQ_ENCOUNTER_029_"+seed.Substring(0,24).ToUpperInvariant())||
               !StringComparer.Ordinal.Equals(request.ContractId,ContractPrefix+board.boardId)||
               !StringComparer.Ordinal.Equals(request.ExpeditionId,"PQ_EXPEDITION_029_"+board.boardId)||
               !StringComparer.Ordinal.Equals(request.BoardId,board.boardId)||!StringComparer.Ordinal.Equals(request.NodeId,node.nodeId)||
               !StringComparer.Ordinal.Equals(request.EncounterId,string.IsNullOrWhiteSpace(node.battleProfileId)?"PQ_BATTLE_PROFILE_029":node.battleProfileId)||
               !StringComparer.Ordinal.Equals(request.BattleId,"BATTLE_PQ_029_"+seed.Substring(0,18).ToUpperInvariant())||
               !StringComparer.Ordinal.Equals(request.Objective,expectedObjective)||
               request.EnemyUnionCount!=1+(int)(Convert.ToUInt32(seed.Substring(0,8),16)%3u)||
               !StringComparer.Ordinal.Equals(request.CanonicalSeedIdentity,seed)||!SequenceEqual(request.AlliedUnionIds,allies)||
               request.ReserveUnionIds.Count!=0||!SequenceEqual(request.ObjectiveIds,expectedObjectives)||
               !SequenceEqual(request.RouteModifiers,expectedModifiers)||request.Supplies!=10||request.Fatigue!=0||request.Urgency!=0||
               !StringComparer.Ordinal.Equals(request.ReturnCheckpointId,"RETURN_PERSONAL_QUEST_029_"+node.nodeId)||!IsSha256(request.PreBattleStateHash))
            {error="CHRONICLE025_PERSONAL_REQUEST_MISMATCH";return false;}
            return true;
        }

        static bool TryCanonicalAlliedUnions(CampaignState campaign,IReadOnlyList<string> requestedIds,
            out IReadOnlyList<string> allies,out string error)
        {
            allies=Array.Empty<string>();error=string.Empty;
            if(requestedIds==null||requestedIds.Count==0){error="CHRONICLE025_ALLIED_UNION_REQUIRED";return false;}
            if(requestedIds.Count>10){error="CHRONICLE025_ALLIED_UNION_LIMIT";return false;}
            var canonical=new List<string>();
            for(var index=0;index<requestedIds.Count;index++)
            {
                var unionId=requestedIds[index];
                if(string.IsNullOrWhiteSpace(unionId)||canonical.Contains(unionId)){error="CHRONICLE025_ALLIED_UNION_IDS_INVALID";return false;}
                var union=campaign?.Guild?.Unions?.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,unionId));
                if(union==null){error="CHRONICLE025_ALLIED_UNION_NOT_OWNED";return false;}
                if(union.Kind!=UnionKind.Normal){error="CHRONICLE025_ALLIED_UNION_NORMAL_REQUIRED";return false;}
                if(union.MemberRecruitIds==null||union.MemberRecruitIds.Count==0){error="CHRONICLE025_ALLIED_UNION_NONEMPTY_REQUIRED";return false;}
                canonical.Add(unionId);
            }
            canonical.Sort(StringComparer.Ordinal);allies=canonical.AsReadOnly();return true;
        }

        static bool SequenceEqual(IReadOnlyList<string> left,IReadOnlyList<string> right)
        {if(left==null||right==null||left.Count!=right.Count)return false;for(var i=0;i<left.Count;i++)if(!StringComparer.Ordinal.Equals(left[i],right[i]))return false;return true;}
        static string BattleObjective(PersonalQuestBoardDefinition025 board)
        {
            var title=board?.displayName??"Personal Chronicle";var marker=title.IndexOf(" Personal Quest",StringComparison.OrdinalIgnoreCase);
            var name=marker>0?title.Substring(0,marker):"your Guild companion";
            return "Stand with "+name+" and win this danger room together. "+(board?.theme??string.Empty);
        }
        static bool IsSha256(string value)
        {if(string.IsNullOrWhiteSpace(value)||value.Length!=64)return false;for(var i=0;i<value.Length;i++)if(!Uri.IsHexDigit(value[i]))return false;return true;}

        public static bool IsPending(CampaignState campaign)=>campaign?.Guild?.GuildCity?.PendingEncounter!=null&&campaign.Guild.GuildCity.PendingEncounter.ContractId.StartsWith(ContractPrefix,StringComparison.Ordinal);
        public static string BoardId(CampaignState campaign)=>IsPending(campaign)?campaign.Guild.GuildCity.PendingEncounter.ContractId.Substring(ContractPrefix.Length):string.Empty;
    }
}
