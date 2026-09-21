using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.RecruitChronicles025
{
    public static class RecruitChronicleRules025
    {
        public static bool IsSignedNormalRecruit(CampaignState campaign,string recruitId)=>campaign?.Guild?.Recruits!=null&&campaign.Guild.Recruits.Any(x=>StringComparer.Ordinal.Equals(x.RecruitId,recruitId)&&x.AuthorityKind==RecruitAuthorityKind.Normal);
        public static PersonalQuestNodeDefinition025 FindNode(PersonalQuestBoardDefinition025 board,string nodeId)=>board?.nodes?.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.nodeId,nodeId));
        public static bool CanMove(PersonalQuestBoardDefinition025 board,string currentNodeId,string destinationNodeId)
        {var n=FindNode(board,currentNodeId);return n!=null&&(n.nextNodeIds??Array.Empty<string>()).Contains(destinationNodeId,StringComparer.Ordinal);}
        public static string DeterministicReceiptId(long campaignSeed,string sourceId,string actionId,string actorId)=>"RCPT025_"+SecondDimension.Determinism.CanonicalJson.Sha256Hex(new{campaignSeed,sourceId,actionId,actorId}).Substring(0,20).ToUpperInvariant();
        public static string PersonalQuestMoveReceiptId(long campaignSeed,string boardId,string destinationNodeId,string recruitId)=>DeterministicReceiptId(campaignSeed,boardId,destinationNodeId,recruitId);
        public static string PersonalQuestBattleProofReceiptId(long campaignSeed,string boardId,string sourceNodeId,
            string destinationNodeId,string recruitId,string battleReturnReceiptId,string rewardId)=>
            "PQ_BATTLE_PROOF025_"+SecondDimension.Determinism.CanonicalJson.Sha256Hex(new
            {campaignSeed,boardId,sourceNodeId,destinationNodeId,recruitId,battleReturnReceiptId,rewardId}).Substring(0,24).ToUpperInvariant();
        public static string LegacyQuestArchiveReceiptId(long campaignSeed,PersonalQuestProgressState025 progress)=>
            "CHRONICLE_ARCHIVE025_"+SecondDimension.Determinism.CanonicalJson.Sha256Hex(new{campaignSeed,progress}).Substring(0,24).ToUpperInvariant();
        public static string LegacyQuestHandoffArchiveReceiptId(long campaignSeed,PersonalQuestProgressState025 progress,
            string requestId,string battleId,string battleReturnReceiptId)=>
            "CHRONICLE_HANDOFF_ARCHIVE025_"+SecondDimension.Determinism.CanonicalJson.Sha256Hex(new
            {campaignSeed,progress,requestId,battleId,battleReturnReceiptId=battleReturnReceiptId??string.Empty,action="NO_REWARD_QUARANTINE"})
                .Substring(0,24).ToUpperInvariant();
        public static bool IsArchivedLegacyQuest(RecruitChronicleState025 state,long campaignSeed,PersonalQuestProgressState025 progress)=>
            state?.AppliedReceiptIds?.Contains(LegacyQuestArchiveReceiptId(campaignSeed,progress))==true;
        public static PersonalQuestProgressState025 ActiveQuest(RecruitChronicleState025 state,long campaignSeed,string boardId)=>
            state?.PersonalQuests?.FirstOrDefault(value=>!value.Completed&&StringComparer.Ordinal.Equals(value.BoardId,boardId)&&
                !IsArchivedLegacyQuest(state,campaignSeed,value));
        public static string HallSceneDeferralReceiptId(long campaignSeed,string sceneId)=>"HALL_DEFERRED025_"+SecondDimension.Determinism.CanonicalJson.Sha256Hex(new{campaignSeed,sceneId,action="DEFER_WITHOUT_PENALTY"}).Substring(0,20).ToUpperInvariant();
        public static bool HasDeferredHallScene(RecruitChronicleState025 state,long campaignSeed,string sceneId)=>state?.AppliedReceiptIds?.Contains(HallSceneDeferralReceiptId(campaignSeed,sceneId))==true;
        public static string PairId(string first,string second)=>PeopleBonds026.BondPairState026.CanonicalPairId(first,second);
        public static int TotalMastery(RecruitState recruit)=>recruit?.Progression?.ArtMastery==null?0:recruit.Progression.ArtMastery.Sum(x=>x.MasteryPoints);
        public static bool HasCompletedBoard(RecruitChronicleState025 state,string boardId)=>state?.PersonalQuests?.Any(x=>StringComparer.Ordinal.Equals(x.BoardId,boardId)&&x.Completed)==true;
        public static int MeaningfulMemoryCount(RecruitChronicleState025 state,string recruitId)=>state?.RelationshipMemories?.Count(x=>StringComparer.Ordinal.Equals(x.FirstRecruitId,recruitId)||StringComparer.Ordinal.Equals(x.SecondRecruitId,recruitId))??0;

        public static bool ValidateProgress(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 progress,out string error)
        {
            error=string.Empty;
            if(campaign?.Guild?.GuildCity==null||board==null||progress==null||
               !StringComparer.Ordinal.Equals(progress.BoardId,board.boardId)||
               !StringComparer.Ordinal.Equals(progress.RecruitId,board.recruitId))
            {error="CHRONICLE025_PROGRESS_IDENTITY_MISMATCH";return false;}
            var current=FindNode(board,progress.CurrentNodeId);
            if(current==null){error="CHRONICLE025_PROGRESS_CURRENT_NODE_INVALID";return false;}
            if(progress.Completed!=StringComparer.Ordinal.Equals(current.kind,"RETURN"))
            {error="CHRONICLE025_PROGRESS_COMPLETION_INVALID";return false;}
            var paths=new List<List<PersonalQuestNodeDefinition025>>();
            CollectPaths(board,board.entryNodeId,current.nodeId,new HashSet<string>(StringComparer.Ordinal),new List<PersonalQuestNodeDefinition025>(),paths);
            foreach(var path in paths)
            {
                var expectedCompleted=new HashSet<string>(path.Take(Math.Max(0,path.Count-1)).Select(x=>x.nodeId),StringComparer.Ordinal);
                if(!expectedCompleted.SetEquals(progress.CompletedNodeIds??Array.Empty<string>()))continue;
                if(ReceiptLedgerMatches(campaign,board,progress,path))return true;
            }
            error="CHRONICLE025_PROGRESS_PATH_OR_RECEIPT_INVALID";return false;
        }

        static void CollectPaths(PersonalQuestBoardDefinition025 board,string nodeId,string destinationId,
            ISet<string> visited,IReadOnlyList<PersonalQuestNodeDefinition025> prefix,ICollection<List<PersonalQuestNodeDefinition025>> paths)
        {
            if(!visited.Add(nodeId))return;var node=FindNode(board,nodeId);if(node==null)return;
            var path=new List<PersonalQuestNodeDefinition025>(prefix){node};
            if(StringComparer.Ordinal.Equals(nodeId,destinationId)){paths.Add(path);return;}
            foreach(var next in node.nextNodeIds??Array.Empty<string>())
                CollectPaths(board,next,destinationId,new HashSet<string>(visited,StringComparer.Ordinal),path,paths);
        }

        static bool ReceiptLedgerMatches(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 progress,IReadOnlyList<PersonalQuestNodeDefinition025> path)
        {
            var remaining=new HashSet<string>(progress.AppliedReceiptIds??Array.Empty<string>(),StringComparer.Ordinal);
            var crossedBattle=false;
            for(var index=0;index<path.Count-1;index++)
            {
                var source=path[index];var destination=path[index+1];string receipt;
                if(StringComparer.Ordinal.Equals(source.kind,"BATTLE"))
                {
                    crossedBattle=true;
                    receipt=remaining.FirstOrDefault(value=>value.StartsWith("BATTLE_RETURN_",StringComparison.Ordinal)&&
                        campaign.Guild.GuildCity.AppliedBattleReturnIds.Contains(value));
                    var rewardId=progress.ExistingBattleRewardReceiptId;
                    if(!string.IsNullOrWhiteSpace(receipt)&&!string.IsNullOrWhiteSpace(rewardId))
                    {
                        var proof=PersonalQuestBattleProofReceiptId(campaign.CampaignSeed,board.boardId,source.nodeId,
                            destination.nodeId,progress.RecruitId,receipt,rewardId);
                        if(!remaining.Remove(proof))
                        {
                            if(!LegacyBattleReturnMatchesCurrentBattle(campaign,board,progress,source,receipt))return false;
                        }
                    }
                    else if(!string.IsNullOrWhiteSpace(receipt)&&
                            !LegacyBattleReturnMatchesCurrentBattle(campaign,board,progress,source,receipt))return false;
                    if(string.IsNullOrWhiteSpace(receipt)&&campaign.Battle?.Reward!=null)
                    {
                        var legacy=DeterministicReceiptId(campaign.CampaignSeed,board.boardId,
                            "BATTLE_RETURN:"+campaign.Battle.FinalStateHash,progress.RecruitId);
                        if(remaining.Contains(legacy)&&LegacyBattleMatches(campaign,board,progress,source))receipt=legacy;
                    }
                    if(string.IsNullOrWhiteSpace(receipt))return false;
                }
                else
                {
                    var canonical=PersonalQuestMoveReceiptId(campaign.CampaignSeed,board.boardId,destination.nodeId,progress.RecruitId);
                    var legacy=DeterministicReceiptId(campaign.CampaignSeed,board.boardId,destination.nodeId,"PERSONAL_QUEST");
                    receipt=remaining.Contains(canonical)?canonical:remaining.Contains(legacy)?legacy:string.Empty;
                    if(string.IsNullOrWhiteSpace(receipt))return false;
                }
                remaining.Remove(receipt);
            }
            if(remaining.Count!=0)return false;
            if(!crossedBattle)return string.IsNullOrWhiteSpace(progress.ExistingBattleRewardReceiptId);
            return (!string.IsNullOrWhiteSpace(progress.ExistingBattleRewardReceiptId)&&
                    campaign.Guild.Development.HasClaimedReward(progress.ExistingBattleRewardReceiptId))||
                   (string.IsNullOrWhiteSpace(progress.ExistingBattleRewardReceiptId)&&campaign.Battle?.Reward!=null&&
                    campaign.Battle.Reward.Claimed&&campaign.Guild.Development.HasClaimedReward(campaign.Battle.Reward.RewardId));
        }

        static bool LegacyBattleReturnMatchesCurrentBattle(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 progress,PersonalQuestNodeDefinition025 source,string receipt)
        {
            if(!LegacyBattleMatches(campaign,board,progress,source))return false;
            var battle=campaign.Battle;var allies=battle.PlayerUnions.Select(x=>x.UnionId).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            var seed=SecondDimension.Determinism.CanonicalJson.Sha256Hex(new{campaign.CampaignSeed,boardId=board.boardId,nodeId=source.nodeId,allies});
            var requestId="PQ_ENCOUNTER_029_"+seed.Substring(0,24).ToUpperInvariant();
            var expectedBattleId="BATTLE_PQ_029_"+seed.Substring(0,18).ToUpperInvariant();
            if(!StringComparer.Ordinal.Equals(battle.BattleId,expectedBattleId))return false;
            var hash=SecondDimension.Determinism.CanonicalJson.Sha256Hex(new
            {RequestId=requestId,BattleId=battle.BattleId,FinalStateHash=battle.FinalStateHash,Outcome=battle.Outcome,RewardId=battle.Reward.RewardId});
            return StringComparer.Ordinal.Equals(receipt,"BATTLE_RETURN_"+hash.Substring(0,24).ToUpperInvariant());
        }

        static bool LegacyBattleMatches(CampaignState campaign,PersonalQuestBoardDefinition025 board,
            PersonalQuestProgressState025 progress,PersonalQuestNodeDefinition025 source)
        {
            var battle=campaign?.Battle;
            return battle?.Reward!=null&&battle.Phase==SecondDimension.Gameplay.M2.BattlePhase.Resolved&&
                   battle.Outcome==SecondDimension.Gameplay.M2.BattleOutcome.Victory&&battle.Reward.Claimed&&
                   (string.IsNullOrWhiteSpace(progress.ExistingBattleRewardReceiptId)||
                    StringComparer.Ordinal.Equals(battle.Reward.RewardId,progress.ExistingBattleRewardReceiptId))&&
                   SecondDimension.Gameplay.M2.M2BattleCommandService.HasValidFinalStateHash090(battle)&&
                   campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId)&&
                   !string.IsNullOrWhiteSpace(source?.nodeId)&&!string.IsNullOrWhiteSpace(board?.boardId);
        }
    }
}
