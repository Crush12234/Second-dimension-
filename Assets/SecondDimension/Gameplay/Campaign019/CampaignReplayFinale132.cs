using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.Campaign019
{
    public sealed class CampaignReplayFinale132
    {
        public Result<CampaignState> CompleteAndRestart(CampaignState campaign,ICampaignRuleCatalog019 chapters,
            ICampaignPlayableCatalog020 steps,IWorldGateOperationsCatalog023 boards)
        {
            var progress=campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            var receipt=progress?.PendingReceipt;var battle=campaign?.Battle;
            if(progress?.ActiveOperation?.ChapterId!=CampaignMissionMap132.FinaleChapterId||
                receipt?.ChapterId!=CampaignMissionMap132.FinaleChapterId||battle?.Reward?.Claimed!=true||
                battle.Outcome!=BattleOutcome.Victory||receipt.AuthoritativeResultHash!=battle.FinalStateHash||
                receipt.ExistingEquipmentRewardReceiptId!=battle.Reward.RewardId)
                return Result<CampaignState>.Failure("CAMPAIGN132_CLAIMED_FINALE_REQUIRED");
            // Existing019 validates the real committed battle, its request hash,
            // rewards and the whole completed020 ledger before any cycle changes.
            var applied=new CampaignCommandService019().ApplyReceiptExactlyOnce(campaign,chapters,steps);
            if(!applied.IsSuccess)return applied;
            var closed=new CampaignPlayableCommandService020().CloseCompletedOperation(applied.Value,steps);
            if(!closed.IsSuccess)return closed;
            var cycle=CampaignReplayRules130.CurrentCycle(progress);
            var growth=CampaignReplayRules130.GrowthPercent(progress);
            var restarted=new CampaignReplayCommandService130().StartNextCycle(closed.Value,chapters,boards,cycle,growth);
            if(!restarted.IsSuccess)return restarted;
            var members=battle.EnemyUnions.SelectMany(union=>union.Members).ToArray();
            var proof=new CampaignFinaleProof132(battle.BattleId,battle.Reward.RewardId,battle.FinalStateHash,
                Grow(members.Sum(member=>(long)member.MaximumHp),growth,60L*CampaignReplayThreat130.MaximumMemberHp130),
                Grow(members.Sum(member=>(long)member.Attack),growth,60L*CampaignReplayThreat130.MaximumMemberOffense130),
                Grow(members.Sum(member=>(long)member.MagicAttack),growth,60L*CampaignReplayThreat130.MaximumMemberOffense130),
                battle.EnemyUnions.Count);
            var candidate=restarted.Value;var city=candidate.Guild.GuildCity;var next=city.Strategic017H.Campaign019;
            var replay=next.Replay130;var starts=replay.CycleStarts.ToArray();var old=starts[starts.Length-1];
            var id=CampaignReplayRules130.BoundaryReceiptId(candidate,old.CycleNumber,growth,
                old.OperationOrdinalAtStart,old.CampaignProgressAtStart,old.PreviousStateHash,proof);
            starts[starts.Length-1]=new CampaignCycleBoundary130(old.CycleNumber,old.OperationOrdinalAtStart,
                old.CampaignProgressAtStart,old.PreviousStateHash,id,proof);
            next=next.With(replay130:new CampaignReplayState130(replay.CurrentCycle,growth,
                replay.CurrentCycleCompletedChapterIds,starts),replaceReplay130:true,lastCheckpointId:id);
            city=city.With(strategic017H:city.Strategic017H.With(campaign019:next,replaceCampaign019:true,lastCheckpointId:id),
                replaceStrategic017H:true,lastCheckpointId:id);
            candidate=candidate.With(candidate.Guild.WithGuildCity(city),candidate.OpeningFlow);
            return CampaignReplayRules130.ValidateState(candidate,out var error)?Result<CampaignState>.Success(candidate):Result<CampaignState>.Failure(error);
        }
        static long Grow(long value,int growth,long ceiling)=>Math.Min(ceiling,(value*(100L+growth)+99L)/100L);
    }
}
