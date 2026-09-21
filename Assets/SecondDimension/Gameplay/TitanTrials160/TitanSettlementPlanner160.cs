using System;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.TitanTrials160
{
    // This is a transaction proposal. It never grants an SSS, grants Gold,
    // claims native XP, or persists progress by itself. The coordinator must
    // bind all grants and this state in one native ApplyAndPersist operation.
    public sealed class TitanSettlementProposal160
    {
        public TitanTrialState160 StateAfterNativeGrants{get;}
        public TitanSettlement160 Receipt{get;}public bool AlreadyApplied{get;}
        public string FirstClearHeroId{get;}public string PersonalGoldArtId{get;}public int PersonalGoldRank{get;}
        public bool RequiresRepeatBonusResolution{get;}public long VictoryOrdinal{get;}
        internal TitanSettlementProposal160(TitanTrialState160 state,TitanSettlement160 receipt,bool applied,
            string hero,string gold,int rank,bool repeat,long wins)
        {StateAfterNativeGrants=state;Receipt=receipt;AlreadyApplied=applied;FirstClearHeroId=hero;
         PersonalGoldArtId=gold;PersonalGoldRank=rank;RequiresRepeatBonusResolution=repeat;VictoryOrdinal=wins;}
    }
    public static class TitanSettlementPlanner160
    {
        public static TitanSettlementProposal160 Prepare(CampaignState campaign,TitanTrialState160 state,TitanTrialCatalog160 catalog)
        {
            if(campaign==null||state==null||catalog==null||state.ProfileId!=campaign.CampaignGuid)
                throw new ArgumentException("TITAN160_SETTLEMENT_PROFILE_MISMATCH");
            var battle=campaign.Battle;
            if(battle==null||battle.Outcome==BattleOutcome.InProgress||battle.Reward==null||!battle.Reward.Claimed||
               battle.Reward.Outcome!=battle.Outcome||!M2BattleCommandService.HasValidFinalStateHash090(battle))
                throw new InvalidOperationException("TITAN160_VERIFIED_NATIVE_REWARD_REQUIRED");
            var old=state.Settlements.SingleOrDefault(x=>x.BattleId==battle.BattleId);
            if(old!=null)
            {
                if(old.NativeBattleRewardId!=battle.Reward.RewardId||old.FinalBattleHash!=battle.FinalStateHash||old.Outcome!=battle.Outcome)
                    throw new InvalidOperationException("TITAN160_SETTLEMENT_REPLAY_MISMATCH");
                return new TitanSettlementProposal160(state,old,true,"","",0,false,state.Progress(old.Slot).Victories);
            }
            var active=state.Active;
            if(active==null||active.BattleId!=battle.BattleId||active.CatalogIdentity!=catalog.Identity||
               active.RecipeIdentity!=catalog.Trial(active.Slot).RecipeIdentity)
                throw new InvalidOperationException("TITAN160_ACTIVE_OWNER_REQUIRED");
            var trial=catalog.Trial(active.Slot);var stats=trial.Stats(active.Tier);
            if(battle.EnemyUnions.Count!=1||battle.EnemyUnions[0].UnionId!=TitanBossPlanner160.BossUnionId(trial)||
               battle.EnemyUnions[0].Members.Count!=1||battle.EnemyUnions[0].Members[0].MemberId!=TitanBossPlanner160.BossMemberId(trial)||
               battle.EnemyUnions[0].Members[0].MaximumHp!=stats.MaximumHp||
               battle.Outcome==BattleOutcome.Victory&&!battle.EnemyUnions[0].IsDefeated)
                throw new InvalidOperationException("TITAN160_TERMINAL_BOSS_MISMATCH");
            var prior=state.Progress(active.Slot);
            string id="TITAN_RECEIPT160_"+CanonicalJson.Sha256Hex(new{state.ProfileId,active.AttemptId,
                battle.BattleId,battle.FinalStateHash,battle.Reward.RewardId}).ToUpperInvariant();
            var receipt=new TitanSettlement160(active.AttemptId,active.Slot,active.Tier,battle.BattleId,
                battle.FinalStateHash,battle.Reward.RewardId,battle.Outcome,id);
            var next=new TitanTrialState160(state.ProfileId,state.AttemptOrdinal,null,state.Settlements.Concat(new[]{receipt}).ToArray());
            bool won=battle.Outcome==BattleOutcome.Victory,first=won&&prior.HighestTier==0;
            bool rank=won&&active.Tier>prior.HighestTier;
            long wins=next.Progress(active.Slot).Victories;
            return new TitanSettlementProposal160(next,receipt,false,first?trial.HeroId:"",rank?trial.GoldArtId:"",
                rank?active.Tier:0,won&&wins%2==0,wins);
        }
    }
}
