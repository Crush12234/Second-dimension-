using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Gameplay.TitanTrials160
{
    // A native-battle commitment planner, not a second damage resolver. The
    // eventual M2 adapter must persist the commitment before resolving its
    // effects and atomically save NextWindow with that native round result.
    public sealed class TitanActionCommitment160
    {
        public string Identity{get;} public string BattleId{get;} public int Round{get;}
        public string AttemptId{get;} public string PriorWindowIdentity{get;}
        public string BattleBasisIdentity{get;} public TitanActionDefinition160 Action{get;}
        public string Channel{get;} public IReadOnlyList<string> TargetUnionIds{get;}
        public IReadOnlyList<int> BudgetSharesBasisPoints{get;}
        public TitanBossWindow160 NextWindow{get;}
        internal TitanActionCommitment160(TitanAttempt160 attempt,BattleState battle,TitanActionDefinition160 action,
            string channel,IReadOnlyList<string> targets,TitanBossWindow160 next)
        {
            AttemptId=attempt.AttemptId;BattleId=battle.BattleId;Round=battle.Round;Action=action;Channel=channel;
            PriorWindowIdentity=CanonicalJson.Sha256Hex(attempt.BossWindow);
            BattleBasisIdentity=M2BattleCommandService.AuthoritativeStateHash(battle);
            TargetUnionIds=Array.AsReadOnly(targets.ToArray());
            // Missing targets keep their original share; an M2 adapter must
            // fizzle it rather than redistribute or silently retarget it.
            var shares=new int[targets.Count];
            for(int i=0;i<shares.Length;i++)shares[i]=action.TotalBudgetBasisPoints/shares.Length+
                (i<action.TotalBudgetBasisPoints%shares.Length?1:0);
            BudgetSharesBasisPoints=Array.AsReadOnly(shares);NextWindow=next;
            Identity=CanonicalJson.Sha256Hex(new{AttemptId,BattleId,Round,PriorWindowIdentity,BattleBasisIdentity,
                Action.Key,Channel,TargetUnionIds,BudgetSharesBasisPoints,NextWindow});
        }
    }
    public static class TitanBossPlanner160
    {
        public static string BossUnionId(TitanTrialDefinition160 trial)=>trial.Id+"_UNION";
        public static string BossMemberId(TitanTrialDefinition160 trial)=>trial.Id+"_BOSS";

        public static TitanActionCommitment160 Prepare(TitanTrialCatalog160 catalog,TitanAttempt160 attempt,BattleState battle)
        {
            if(catalog==null||attempt==null||battle==null)throw new ArgumentNullException();
            var trial=catalog.Trial(attempt.Slot);var window=attempt.BossWindow;
            if(attempt.CatalogIdentity!=catalog.Identity||attempt.RecipeIdentity!=trial.RecipeIdentity||battle.BattleId!=attempt.BattleId)
                throw new InvalidOperationException("TITAN160_COMMITMENT_OWNER_MISMATCH");
            if(battle.Outcome!=BattleOutcome.InProgress||battle.Phase!=BattlePhase.ForecastSelection||
               battle.Round!=checked(window.LastResolvedRound+1)||
               window.LastResolvedRound>0&&!battle.RoundRecords.Any(r=>r.Round==window.LastResolvedRound))
                throw new InvalidOperationException("TITAN160_NATIVE_FORECAST_WINDOW_REQUIRED");
            if(!battle.PlayerUnions.Where(u=>attempt.AlliedUnionIds.Contains(u.UnionId)).Select(u=>u.UnionId).OrderBy(x=>x,StringComparer.Ordinal)
                .SequenceEqual(attempt.AlliedUnionIds.OrderBy(x=>x,StringComparer.Ordinal)) ||
               battle.PlayerUnions.Any(u=>!attempt.AlliedUnionIds.Contains(u.UnionId)&&
                   !u.Members.All(m=>SecondDimension.Gameplay.SSSTenV4.SssBattleIntegration090.IsSyntheticMember090(m.MemberId))))
                throw new InvalidOperationException("TITAN160_DEPLOYMENT_CHANGED");
            if(battle.EnemyUnions.Count!=1||battle.EnemyUnions[0].UnionId!=BossUnionId(trial)||
               battle.EnemyUnions[0].Members.Count!=1||battle.EnemyUnions[0].Members[0].MemberId!=BossMemberId(trial))
                throw new InvalidOperationException("TITAN160_NATIVE_BOSS_BINDING_REQUIRED");
            var boss=battle.EnemyUnions[0].Members[0];var stats=trial.Stats(attempt.Tier);
            if(boss.MaximumHp!=stats.MaximumHp||boss.Attack!=stats.Attack||boss.MagicAttack!=stats.MagicAttack||boss.Downed)
                throw new InvalidOperationException("TITAN160_FIXED_PROFILE_MISMATCH");
            bool exhausted=window.ActionIndex==0?window.HealChannelsAccepted>=2:window.ExhaustedHealCycle;
            var cycle=trial.Phases[window.PhaseIndex].Cycle;
            if(trial.Slot==5&&exhausted)
                cycle=((JArray)JObject.Parse(trial.BossRecipeJson)["cycle_when_heal_channels_exhausted"]).Values<string>().ToArray();
            var action=trial.Action(cycle[window.ActionIndex]);
            string channel=action.Channel,warningKey=window.WarningKey,warningChannel=window.WarningChannel;
            int warningRound=window.WarningRound,warningHp=window.WarningBossHp,heals=window.HealChannelsAccepted;
            IReadOnlyList<string> warningTargets=window.WarningTargets,targets=Array.Empty<string>();
            if(action.Kind=="WIND_UP")
            {
                if(window.WarningKey.Length!=0)throw new InvalidOperationException("TITAN160_WARNING_ALREADY_PENDING");
                var impact=trial.Action(action.Announces);
                channel=ActualChannel(impact,window.CompletedCycles);
                targets=SelectTargets(action.FreezeTargets,attempt,battle,window,trial);
                warningKey=action.Key;warningChannel=channel;warningRound=battle.Round;warningHp=boss.CurrentHp;warningTargets=targets;
                if(impact.Special=="FINITE_HEAL_CHANNEL")heals=checked(heals+1);
            }
            else if(action.Kind=="IMPACT")
            {
                var warning=trial.Action(action.RequiresWarning);
                if(warningKey!=action.RequiresWarning||battle.Round-warningRound<warning.MinimumForecastOpportunities||
                   !battle.RoundRecords.Any(r=>r.Round==warningRound))
                    throw new InvalidOperationException("TITAN160_WARNED_FORECAST_REQUIRED");
                targets=warningTargets;channel=warningChannel;
                warningKey="";warningChannel="";warningRound=0;warningHp=0;warningTargets=Array.Empty<string>();
            }
            else if(action.Kind=="DAMAGE")targets=SelectTargets(action.Targets,attempt,battle,window,trial);
            else if(window.WarningKey.Length!=0)throw new InvalidOperationException("TITAN160_WARNING_NOT_CONSUMED");
            int nextIndex=window.ActionIndex+1,cycles=window.CompletedCycles,phase=window.PhaseIndex;
            if(nextIndex==cycle.Count)
            {
                if(action.Kind!="RECOVERY")throw new InvalidOperationException("TITAN160_RECOVERY_REQUIRED");
                nextIndex=0;cycles=checked(cycles+1);
                long hpBp=(long)boss.CurrentHp*10000/boss.MaximumHp;
                for(int i=phase+1;i<trial.Phases.Count;i++)if(hpBp<=trial.Phases[i].ThresholdBasisPoints)phase=i;
            }
            var next=new TitanBossWindow160(phase,nextIndex,cycles,battle.Round,heals,exhausted,
                warningKey,warningChannel,warningRound,warningTargets,warningHp);
            return new TitanActionCommitment160(attempt,battle,action,channel,targets,next);
        }
        static string ActualChannel(TitanActionDefinition160 action,int cycles)=>
            action.Special=="ALTERNATE_MYSTIC_BREATH"&&cycles%2==1?"MYSTIC_DAMAGE":action.Channel;
        static IReadOnlyList<string> SelectTargets(string policy,TitanAttempt160 attempt,BattleState battle,
            TitanBossWindow160 window,TitanTrialDefinition160 trial)
        {
            if(policy=="SELF")return new[]{BossUnionId(trial)};
            // The saved deployment order is the round-robin formation order.
            var living=battle.PlayerUnions.Where(u=>!u.Retreated&&!u.IsDefeated).Select(u=>u.UnionId).ToArray();
            int count=policy=="ALL_LIVING_ENEMY_UNIONS"?living.Length:policy=="TWO_LIVING_ENEMY_UNIONS"?2:
                policy=="ONE_LIVING_ENEMY_UNION"?1:throw new InvalidOperationException("TITAN160_TARGET_POLICY_UNBOUND:"+policy);
            if(living.Length==0)return Array.Empty<string>();
            int start=(window.CompletedCycles+window.PhaseIndex)%living.Length;
            return Enumerable.Range(0,Math.Min(count,living.Length)).Select(i=>living[(start+i)%living.Length]).ToArray();
        }
    }
}
