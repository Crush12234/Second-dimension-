using System;
using System.Collections.Generic;
using System.Linq;
namespace SecondDimension.SSS.V3
{
    public enum SssEffect { TamedUnion,GuestSummon,Transformation,HealAllAllies,DamageAllEnemies,RallyAllAllies,BarrierAllAllies,StormAllEnemies,EclipseAllEnemies,WorldsongAllAllies }
    [Serializable] public sealed class SssUnionTarget
    { public string unionId;public bool allied,living,targetable; }
    [Serializable] public sealed class SssCommandContext
    {
        public GoldCommandContext covenant=new GoldCommandContext();
        public bool signatureCooldownReady=true;
        public List<SssUnionTarget> unions=new List<SssUnionTarget>();
    }
    [Serializable] public sealed class SssCommandPlan
    {
        public string id,label,heroId,sourceUnionId,encounterId,powerNumerator="100";
        public int powerDenominator=100;
        public string style="gold";
        public bool consumesWholeUnionChoice=true,usesLiveApMp=true;
        public SssEffect effect;
        public List<string> targetUnionIds=new List<string>();
        public GoldCommandPlan covenantPlan;
        public int proposedBuffDurationRounds=3;
    }
    public static class SssGoldCommands
    {
        public static bool TryPlan(SssCommandContext c,SssSave state,out SssCommandPlan plan,out string reason)
        {
            plan=null;reason=null;SssProgression.Validate(state);
            if(c==null || c.covenant==null || c.unions==null)throw new ArgumentNullException("context");
            var k=c.covenant;string hero=SssHeroes.CanonicalId(k.casterHeroId);int index=Array.IndexOf(SssHeroes.All,hero);
            if(index<0){reason="Unknown SSS hero.";return false;}
            if(k.sourceMemberIds==null || k.sourceMemberIds.Select(SssHeroes.CanonicalId).Count(x=>x==hero)!=1) {reason="Hero must be in this Union.";return false;}
            if(k.sourceMemberIds.Select(SssHeroes.CanonicalId).Distinct().Count()!=k.sourceMemberIds.Count) {reason="Duplicate Union member identity.";return false;}
            if(!k.legalForecastPhase){reason="Wait for a legal complete Union Forecast.";return false;}
            if(!k.casterAliveAndCanAct){reason="Hero cannot act.";return false;}
            if(k.sourceUnionAlreadyTransformed){reason="Original member commands are suppressed while transformed.";return false;}
            if(!k.liveCostAffordable){reason="Insufficient existing Union AP / caster MP.";return false;}
            if(!c.signatureCooldownReady){reason="Signature Art cooldown is not ready.";return false;}
            ExactNumbers.RequireId(k.sourceUnionId);ExactNumbers.RequireId(k.encounterInstanceId);
            plan=new SssCommandPlan {id="SSS_CMD_"+hero.Substring(4),label=SssHeroes.Signature(hero),heroId=hero,
                sourceUnionId=k.sourceUnionId,encounterId=k.encounterInstanceId,effect=(SssEffect)index};
            if(index<3) {
                // Canonicalize legacy references without mutating the live/context object.
                var canon=new GoldCommandContext {encounterInstanceId=k.encounterInstanceId,sourceUnionId=k.sourceUnionId,casterHeroId=hero,
                    sourceMemberIds=k.sourceMemberIds.Select(SssHeroes.CanonicalId).ToList(),preparedFamilyIds=k.preparedFamilyIds==null?null:new List<string>(k.preparedFamilyIds),
                    legalForecastPhase=k.legalForecastPhase,casterAliveAndCanAct=k.casterAliveAndCanAct,sourceUnionAlreadyTransformed=k.sourceUnionAlreadyTransformed,
                    liveCostAffordable=k.liveCostAffordable,guestSpaceAvailable=k.guestSpaceAvailable,alreadyUsedThisBattle=k.alreadyUsedThisBattle,alreadyOwnsActiveGuest=k.alreadyOwnsActiveGuest};
                GoldCommandPlan familyPlan;
                if(!CovenantUnionCommands.TryPlan(canon,state.familyProgress,out familyPlan,out reason)){plan=null;return false;}
                plan.covenantPlan=familyPlan;return true;
            }
            bool allies=index==3 || index==5 || index==6 || index==9;
            if(c.unions.Any(x=>x==null || String.IsNullOrWhiteSpace(x.unionId)))throw new ArgumentException("Invalid Union target list.");
            plan.targetUnionIds=c.unions.Where(x=>x.allied==allies && x.living && x.targetable).Select(x=>x.unionId).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToList();
            if(plan.targetUnionIds.Count==0){plan=null;reason="No legal living target Unions.";return false;}
            plan.powerNumerator=SssGrowth.ForHero(state,hero).powerNumerator;return true;
        }
    }
    // Implement against the current runtime, NOT by instantiating an independent battle.
    public interface ISssBattleHost
    {
        SssSave ReadSssState();
        SssCommandContext ReadCurrentContext(string unionId,string heroId);
        // Must revalidate target life/eligibility at EACH actual execution. In ONE host
        // transaction: pay live resources; dispatch the supported live effects; spawn or
        // transform; commit usage/cooldown/save. Roll back every side effect on failure.
        bool TryCommit(SssCommandPlan plan,out string reason);
    }
    public static class SssCommandExecutor
    {
        public static bool Execute(ISssBattleHost host,string unionId,string heroId,out string reason)
        {
            if(host==null)throw new ArgumentNullException("host");SssCommandPlan plan;
            if(!SssGoldCommands.TryPlan(host.ReadCurrentContext(unionId,heroId),host.ReadSssState(),out plan,out reason))return false;
            return host.TryCommit(plan,out reason);
        }
    }
}
