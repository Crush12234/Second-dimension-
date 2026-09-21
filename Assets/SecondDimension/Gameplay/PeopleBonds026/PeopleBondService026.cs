using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.RecruitChronicles025;

namespace SecondDimension.Gameplay.PeopleBonds026
{
    public sealed class PeopleBondService026
    {
        static readonly (string id,int trust,int memories)[] Tiers={
            ("BOND_TIER026_0",0,0),("BOND_TIER026_1",10,2),("BOND_TIER026_2",25,4),
            ("BOND_TIER026_3",45,7),("BOND_TIER026_4",70,11),("BOND_TIER026_5",90,16)};

        public Result<CampaignState> ApplyMemory(CampaignState campaign,RelationshipMemoryDefinition025 definition,string firstRecruitId,string secondRecruitId,string receiptId,string sourceId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var chronicles,out var bonds,out var error))return Result<CampaignState>.Failure(error);
            if(definition==null)return Result<CampaignState>.Failure("PEOPLE026_MEMORY_DEFINITION_REQUIRED");
            if(!RecruitChronicleRules025.IsSignedNormalRecruit(campaign,firstRecruitId)||!RecruitChronicleRules025.IsSignedNormalRecruit(campaign,secondRecruitId))return Result<CampaignState>.Failure("PEOPLE026_SIGNED_NORMAL_RECRUITS_REQUIRED");
            receiptId=string.IsNullOrWhiteSpace(receiptId)?RecruitChronicleRules025.DeterministicReceiptId(campaign.CampaignSeed,definition.memoryId,sourceId??string.Empty,RecruitChronicleRules025.PairId(firstRecruitId,secondRecruitId)):receiptId;
            if(bonds.AppliedReceiptIds.Contains(receiptId)||chronicles.AppliedReceiptIds.Contains(receiptId))return Result<CampaignState>.Success(campaign);
            var pairId=BondPairState026.CanonicalPairId(firstRecruitId,secondRecruitId);var pairs=bonds.Pairs.ToList();var index=pairs.FindIndex(x=>StringComparer.Ordinal.Equals(x.PairId,pairId));
            var pair=index>=0?pairs[index]:new BondPairState026(pairId,firstRecruitId,secondRecruitId,0,0,0,0,"BOND_TIER026_0",Array.Empty<string>());
            var memoryReceipts=pair.AppliedMemoryReceiptIds.ToList();memoryReceipts.Add(receiptId);
            var trust=Math.Max(-100,Math.Min(100,pair.Trust+definition.trustDelta*Math.Max(1,definition.strength)));
            var respect=Math.Max(-100,Math.Min(100,pair.Respect+definition.respectDelta*Math.Max(1,definition.strength)));
            var familiarity=Math.Max(-100,Math.Min(100,pair.Familiarity+Math.Max(1,definition.strength)));
            pair=pair.With(trust:trust,respect:respect,familiarity:familiarity,sharedMemoryCount:pair.SharedMemoryCount+1,tierId:TierId(trust,pair.SharedMemoryCount+1),appliedMemoryReceiptIds:memoryReceipts.AsReadOnly());
            if(index>=0)pairs[index]=pair;else pairs.Add(pair);
            var bondReceipts=bonds.AppliedReceiptIds.ToList();bondReceipts.Add(receiptId);bonds=bonds.With(pairs:pairs.AsReadOnly(),appliedReceiptIds:bondReceipts.AsReadOnly(),lastCheckpointId:"memory:"+receiptId);
            var history=chronicles.RelationshipMemories.ToList();history.Add(new RelationshipMemoryProgressState025(receiptId,firstRecruitId,secondRecruitId,definition.memoryId,definition.strength,sourceId));
            var chronReceipts=chronicles.AppliedReceiptIds.ToList();chronReceipts.Add(receiptId);chronicles=chronicles.With(relationshipMemories:history.AsReadOnly(),appliedReceiptIds:chronReceipts.AsReadOnly(),lastCheckpointId:"memory:"+receiptId);
            return Success(campaign,city,strategic,progress,playable,chronicles,bonds,"people_memory_applied");
        }

        public Result<CampaignState> ApplyPersonalQuestNodeMemory(CampaignState campaign,PersonalQuestBoardDefinition025 board,PersonalQuestNodeDefinition025 destinationNode,RelationshipMemoryDefinition025 definition,string committedQuestNodeReceiptId)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var chronicles,out var bonds,out var error))return Result<CampaignState>.Failure(error);
            if(board==null||destinationNode==null)return Result<CampaignState>.Failure("PEOPLE026_QUEST_NODE_CONTENT_REQUIRED");
            var authoredNode=RecruitChronicleRules025.FindNode(board,destinationNode.nodeId);
            if(authoredNode==null||!StringComparer.Ordinal.Equals(authoredNode.rewardMemoryId,destinationNode.rewardMemoryId)||string.IsNullOrWhiteSpace(authoredNode.rewardMemoryId))return Result<CampaignState>.Failure("PEOPLE026_QUEST_NODE_MEMORY_REQUIRED");
            if(definition==null||!StringComparer.Ordinal.Equals(definition.memoryId,authoredNode.rewardMemoryId))return Result<CampaignState>.Failure("PEOPLE026_QUEST_NODE_MEMORY_MISMATCH");
            if(string.IsNullOrWhiteSpace(committedQuestNodeReceiptId))return Result<CampaignState>.Failure("PEOPLE026_QUEST_NODE_RECEIPT_REQUIRED");
            var quest=chronicles.PersonalQuests.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.BoardId,board.boardId));
            if(quest==null||!StringComparer.Ordinal.Equals(quest.RecruitId,board.recruitId)||!StringComparer.Ordinal.Equals(quest.CurrentNodeId,authoredNode.nodeId)||!quest.AppliedReceiptIds.Contains(committedQuestNodeReceiptId))return Result<CampaignState>.Failure("PEOPLE026_COMMITTED_QUEST_NODE_REQUIRED");
            var union=(campaign.Guild.Unions??Array.Empty<UnionState>()).Where(x=>x.Kind==UnionKind.Normal&&x.MemberRecruitIds.Contains(board.recruitId)).OrderBy(x=>x.UnionId,StringComparer.Ordinal).FirstOrDefault();
            if(union==null)return Result<CampaignState>.Failure("PEOPLE026_QUEST_RECRUIT_UNION_REQUIRED");
            var companionId=(union.MemberRecruitIds??Array.Empty<string>()).Where(x=>!StringComparer.Ordinal.Equals(x,board.recruitId)&&RecruitChronicleRules025.IsSignedNormalRecruit(campaign,x)).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).FirstOrDefault();
            if(string.IsNullOrWhiteSpace(companionId))return Result<CampaignState>.Failure("PEOPLE026_QUEST_COMPANION_REQUIRED");
            var pairId=RecruitChronicleRules025.PairId(board.recruitId,companionId);
            var memoryReceiptId=RecruitChronicleRules025.DeterministicReceiptId(campaign.CampaignSeed,committedQuestNodeReceiptId,definition.memoryId,board.recruitId);
            var existingMemory=chronicles.RelationshipMemories.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ReceiptId,memoryReceiptId));
            var existingPair=bonds.Pairs.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.PairId,pairId));
            var bondReceipt=bonds.AppliedReceiptIds.Contains(memoryReceiptId);
            var chronicleReceipt=chronicles.AppliedReceiptIds.Contains(memoryReceiptId);
            var pairReceipt=existingPair!=null&&existingPair.AppliedMemoryReceiptIds.Contains(memoryReceiptId);
            if(existingMemory!=null||bondReceipt||chronicleReceipt||pairReceipt)
            {
                var expectedSourceId="PERSONAL_QUEST_NODE:"+committedQuestNodeReceiptId;
                var matchingMemory=existingMemory!=null&&StringComparer.Ordinal.Equals(existingMemory.MemoryId,definition.memoryId)&&
                    existingMemory.Strength==Math.Max(1,definition.strength)&&StringComparer.Ordinal.Equals(existingMemory.SourceId,expectedSourceId)&&
                    StringComparer.Ordinal.Equals(RecruitChronicleRules025.PairId(existingMemory.FirstRecruitId,existingMemory.SecondRecruitId),pairId);
                return matchingMemory&&bondReceipt&&chronicleReceipt&&pairReceipt
                    ?Result<CampaignState>.Success(campaign)
                    :Result<CampaignState>.Failure("PEOPLE026_QUEST_MEMORY_RECEIPT_MISMATCH");
            }
            return ApplyMemory(campaign,definition,board.recruitId,companionId,memoryReceiptId,"PERSONAL_QUEST_NODE:"+committedQuestNodeReceiptId);
        }

        public Result<CampaignState> SetUnionDoctrine(CampaignState campaign,string unionId,UnionBondDoctrineDefinition026 definition)
        {
            if(!TryContext(campaign,out var city,out var strategic,out var progress,out var playable,out var chronicles,out var bonds,out var error))return Result<CampaignState>.Failure(error);
            if(definition==null||string.IsNullOrWhiteSpace(definition.doctrineId)||definition.directMemberSelection)return Result<CampaignState>.Failure("PEOPLE026_DOCTRINE_CONTENT_REQUIRED");
            if(!TryTierRank(definition.minimumBondTierId,out var requiredTierRank))return Result<CampaignState>.Failure("PEOPLE026_DOCTRINE_TIER_REQUIRED");
            var union=campaign.Guild.Unions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,unionId));if(union==null)return Result<CampaignState>.Failure("PEOPLE026_UNION_REQUIRED");
            var strongest=StrongestPair(bonds,union.MemberRecruitIds);
            if(strongest==null||TierRank(strongest.TierId)<requiredTierRank)return Result<CampaignState>.Failure("PEOPLE026_DOCTRINE_BOND_TIER_REQUIRED");
            var states=bonds.Unions.ToList();var index=states.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,unionId));var state=index>=0?states[index]:new UnionBondRuntimeState026(unionId,string.Empty,Array.Empty<string>(),0);
            state=state.With(doctrineId:definition.doctrineId);if(index>=0)states[index]=state;else states.Add(state);bonds=bonds.With(unions:states.AsReadOnly(),lastCheckpointId:"doctrine:"+unionId);
            return Success(campaign,city,strategic,progress,playable,chronicles,bonds,"people_doctrine_saved");
        }

        public static string TierId(int trust,int memories){var result="BOND_TIER026_0";for(var i=0;i<Tiers.Length;i++)if(trust>=Tiers[i].trust&&memories>=Tiers[i].memories)result=Tiers[i].id;return result;}
        public static int TierRank(string id){for(var i=0;i<Tiers.Length;i++)if(StringComparer.Ordinal.Equals(Tiers[i].id,id))return i;return 0;}
        static bool TryTierRank(string id,out int rank){for(var i=0;i<Tiers.Length;i++)if(StringComparer.Ordinal.Equals(Tiers[i].id,id)){rank=i;return true;}rank=0;return false;}
        public static BondPairState026 StrongestPair(PeopleBondState026 state,IReadOnlyList<string> memberIds)
        {if(state?.Pairs==null||memberIds==null)return null;var set=new HashSet<string>(memberIds,StringComparer.Ordinal);return state.Pairs.Where(x=>set.Contains(x.FirstRecruitId)&&set.Contains(x.SecondRecruitId)).OrderByDescending(x=>TierRank(x.TierId)).ThenByDescending(x=>x.Trust).ThenBy(x=>x.PairId,StringComparer.Ordinal).FirstOrDefault();}

        static bool TryContext(CampaignState c,out GuildCityState017D city,out GuildCityStrategicState017H strategic,out CampaignProgressState019 progress,out CampaignPlayableState020 playable,out RecruitChronicleState025 chronicles,out PeopleBondState026 bonds,out string error)
        {city=null;strategic=null;progress=null;playable=null;chronicles=null;bonds=null;error=string.Empty;if(c?.Guild?.GuildCity==null){error="PEOPLE026_CAMPAIGN_REQUIRED";return false;}city=c.Guild.GuildCity;strategic=city.Strategic017H??GuildCityStrategicState017H.Default();progress=strategic.Campaign019??CampaignProgressState019.Default();playable=progress.Playable020??CampaignPlayableState020.Default();chronicles=playable.RecruitChronicles025??RecruitChronicleState025.Default();bonds=playable.PeopleBonds026??PeopleBondState026.Default();return true;}
        static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,CampaignProgressState019 progress,CampaignPlayableState020 playable,RecruitChronicleState025 chronicles,PeopleBondState026 bonds,string checkpoint)
        {playable=playable.With(recruitChronicles025:chronicles,replaceRecruitChronicles025:true,peopleBonds026:bonds,replacePeopleBonds026:true,lastCheckpointId:checkpoint);progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:checkpoint);strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:checkpoint);city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:checkpoint);return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));}
    }
}
