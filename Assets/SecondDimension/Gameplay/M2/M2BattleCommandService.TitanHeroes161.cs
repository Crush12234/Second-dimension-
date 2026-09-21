using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanHeroes161;

namespace SecondDimension.Gameplay.M2
{
    [Serializable]
    public sealed class TitanHeroTargetShare161
    {
        [JsonConstructor]
        public TitanHeroTargetShare161(string memberId,int[] channelAmounts)
        {MemberId=memberId;ChannelAmounts=channelAmounts==null?Array.Empty<int>():(int[])channelAmounts.Clone();}
        public string MemberId{get;}public int[] ChannelAmounts{get;}
    }
    [Serializable]
    public sealed class TitanHeroActionPlan161
    {
        [JsonConstructor]
        public TitanHeroActionPlan161(string heroId,string artId,string unionId,string targetUnionId,int round,int budget,IReadOnlyList<TitanHeroTargetShare161> targets)
        {HeroId=heroId;ArtId=artId;UnionId=unionId;TargetUnionId=targetUnionId;Round=round;Budget=budget;
         Targets=Array.AsReadOnly((targets??Array.Empty<TitanHeroTargetShare161>()).ToArray());}
        public string HeroId{get;}public string ArtId{get;}public string UnionId{get;}public string TargetUnionId{get;}
        public int Round{get;}public int Budget{get;}public IReadOnlyList<TitanHeroTargetShare161> Targets{get;}
    }
    public sealed partial class M2BattleCommandService
    {
        internal static IEnumerable<BattleForecastState> BuildTitanHeroForecasts161(CampaignState campaign,BattleState battle,M2CombatContent content,BattleUnionState union)
        {
            var runtime=battle.TitanHeroes161;
            if(runtime==null||union.Side!=BattleSide.Player||union.IsDefeated||union.Retreated||union.CurrentAp<TitanHeroCatalog161.SharedAp)yield break;
            foreach(var caster in union.Members)
            {
                if(caster.Downed||caster.CurrentMp<TitanHeroCatalog161.PersonalMp||runtime.UsedHeroIds.Contains(caster.MemberId))continue;
                foreach(var frozen in runtime.Abilities.Where(a=>a.HeroId==caster.MemberId))
                {
                    var recipe=TitanHeroCatalog161.Recipe(frozen.ArtId);
                    foreach(var target in recipe.IsFriendly?battle.PlayerUnions:battle.EnemyUnions)
                    {
                        if(target.Retreated)continue;
                        var targets=target.Members.Where(m=>recipe.TargetScope=="ONE_FALLEN_ALLY"?m.Downed:!m.Downed).ToArray();
                        if(recipe.TargetScope=="ONE_FALLEN_ALLY")
                        {
                            foreach(var fallen in targets)
                                yield return BuildTitanHeroForecast161(campaign,battle,content,union,caster,target,new[]{fallen},frozen,recipe);
                            continue;
                        }
                        if(target.IsDefeated||targets.Length==0||targets.Length>64)continue;
                        if(recipe.Channels.All(c=>c=="HEAL")&&targets.All(m=>m.CurrentHp==m.MaximumHp)&&recipe.Secondary!="CLEANSE_ONE")continue;
                        yield return BuildTitanHeroForecast161(campaign,battle,content,union,caster,target,targets,frozen,recipe);
                    }
                }
            }
        }

        static BattleForecastState BuildTitanHeroForecast161(CampaignState campaign,BattleState battle,M2CombatContent content,BattleUnionState source,
            BattleMemberState caster,BattleUnionState target,BattleMemberState[] targets,FrozenTitanHeroAbility161 frozen,TitanHeroRecipe161 recipe)
        {
            // A single caster's ordinary clean native amount, shared across all targets/channels/bursts.
            long clean;
            if(recipe.IsFriendly)clean=18L+caster.MagicAttack/2;
            else
            {
                long weightedPower=0;
                for(int c=0;c<recipe.Channels.Count;c++)weightedPower+=(long)(recipe.Channels[c]=="MYSTIC_DAMAGE"?caster.MagicAttack:caster.Attack)*recipe.ChannelWeights[c];
                clean=10+(weightedPower/10000)*(source.FormationBenefitActive?105:100)/100;
            }
            int rankOne=CapTitan161((new BigInteger(clean)*recipe.BudgetBasisPoints+9999)/10000);
            int budget=frozen.Scale(rankOne);
            long sumHp=target.Members.Sum(m=>(long)m.CurrentHp),sumMax=target.Members.Sum(m=>(long)m.MaximumHp);
            if(recipe.WoundedThreshold>0&&new BigInteger(sumHp)*10000<new BigInteger(sumMax)*recipe.WoundedThreshold)
                budget=CapTitan161((new BigInteger(budget)*(10000+recipe.WoundedBonus)+9999)/10000);
            var channelTotals=AllocateTitanBudget161(budget,recipe.ChannelWeights.Select(v=>(long)v).ToArray());
            long[] weights=targets.Select(m=>recipe.Allocation=="MISSING_HP"?(long)m.MaximumHp-m.CurrentHp:
                recipe.Allocation=="MISSING_HP_PLUS_ONE"?(long)m.MaximumHp-m.CurrentHp+1:1L).ToArray();
            var amounts=new int[targets.Length][];for(int i=0;i<amounts.Length;i++)amounts[i]=new int[channelTotals.Length];
            for(int c=0;c<channelTotals.Length;c++)
            {var allocated=AllocateTitanBudget161(channelTotals[c],weights);for(int i=0;i<allocated.Length;i++)amounts[i][c]=allocated[i];}
            var plan=new TitanHeroActionPlan161(caster.MemberId,recipe.Id,source.UnionId,target.UnionId,battle.Round,budget,
                targets.Select((m,i)=>new TitanHeroTargetShare161(m.MemberId,amounts[i])).ToArray());
            string json=CanonicalJson.Serialize(plan),identity=CanonicalJson.Sha256Hex(plan);
            var kind=recipe.IsFriendly?BattleActionKind.Restoration:recipe.Channels[0]=="MYSTIC_DAMAGE"?BattleActionKind.Mystic:BattleActionKind.Martial;
            var actions=new List<BattlePlannedActionState>{new BattlePlannedActionState(caster.MemberId,caster.DisplayName,target.UnionId,targets[0].MemberId,
                recipe.Id,recipe.Name,kind,TitanHeroCatalog161.SharedAp,TitanHeroCatalog161.PersonalMp,
                recipe.IsFriendly?(recipe.Channels.Contains("BARRIER")?0:budget):-budget,0,0,
                "One shared "+budget+" point budget; "+recipe.Secondary+"; "+recipe.Bursts+" burst(s).",false,false,recipe.Id,kind==BattleActionKind.Mystic?"Mystic":"Martial")};
            var companion=BuildGoldCompanionForecast090(campaign,campaign.CampaignSeed,battle,source,caster.MemberId,300,
                battle.ForecastStateBasisHash,content,campaign.Rules);
            if(companion!=null)actions.AddRange(companion.MemberActions.Where(a=>a.ActorMemberId!=caster.MemberId));
            return new BattleForecastState("FORECAST_TITAN161_"+identity.Substring(0,16).ToUpperInvariant(),source.UnionId,
                "TITAN161_"+recipe.Id,"GOLD ART — "+recipe.Name,recipe.Name,"Personal Titan Art",target.UnionId,target.DisplayName,actions.AsReadOnly(),
                actions.Sum(a=>a.SharedApCost),0,actions.Sum(a=>a.PersonalMpCost),
                recipe.Name+": "+budget+" total points allocated over "+targets.Length+" committed target(s), not per target. "+
                (recipe.Secondary=="ARMOR_BREAK"?"Next friendly window: +15% received physical damage (native armor binding).":recipe.Secondary)+
                (recipe.BarrierScope=="MYSTIC_ONLY"?" Ward absorbs Mystic damage only.":""),
                "Once per hero per encounter; Eidran's echoes share one use. 6 AP + 10 personal MP, plus displayed companion costs.",
                "Personal Titan Arts do not duplicate the caster's normal action.","A missing committed target loses its share; an accepted use never returns.",identity,json);
        }

        public static int[] AllocateTitanBudget161(int budget,IReadOnlyList<long> weights)
        {
            if(budget<0||weights==null||weights.Count>64||weights.Any(x=>x<0))throw new ArgumentException("TITAN161_ALLOCATION_INVALID");
            var result=new int[weights.Count];BigInteger total=0;foreach(long weight in weights)total+=weight;
            if(total==0)return result;
            // Cumulative division conserves exactly one budget with stable target-order remainders.
            BigInteger running=0;int allocated=0;
            for(int i=0;i<weights.Count;i++){running+=weights[i];int next=(int)(new BigInteger(budget)*running/total);result[i]=next-allocated;allocated=next;}
            return result;
        }
        static int CapTitan161(BigInteger value)=>value>int.MaxValue?int.MaxValue:value<0?0:(int)value;
        internal static bool IsTitanHeroForecast161(BattleForecastState forecast)=>forecast?.CommandId?.StartsWith("TITAN161_",StringComparison.Ordinal)==true;
        internal static bool TryBuildTitanHeroCompanion161(BattleForecastState forecast,out BattleForecastState companion)
        {
            companion=null;if(!IsTitanHeroForecast161(forecast)||forecast.MemberActions.Count<2)return false;
            var actions=forecast.MemberActions.Skip(1).ToArray();
            companion=new BattleForecastState(forecast.ForecastId+"_COMPANIONS",forecast.UnionId,"CMD_BALANCED","Titan Art — Union Follow-through",
                "The other members perform their displayed ordinary actions.","Complete Union command",forecast.TargetId,forecast.TargetName,actions,
                actions.Sum(a=>a.SharedApCost),0,actions.Sum(a=>a.PersonalMpCost),"Displayed companion actions",forecast.Risk,"",forecast.FallbackBehavior,
                forecast.GenerationIdentity+"|COMPANIONS",forecast.DeterministicDebugEvidence);return true;
        }

        internal static bool TryResolveTitanHeroForecast161(CampaignState campaign,BattleState battle,int sourceUnionIndex,BattleForecastState forecast,
            List<BattleUnionState> players,List<BattleUnionState> enemies,List<BattleEventState> events,ref TitanHeroBattleRuntime161 runtime,out string failure)
        {
            failure=null;
            try
            {
                if(!IsTitanHeroForecast161(forecast)||runtime==null||sourceUnionIndex<0||sourceUnionIndex>=players.Count||forecast.MemberActions.Count<1)
                    throw new InvalidOperationException("TITAN161_FORECAST_INVALID");
                var plan=JsonConvert.DeserializeObject<TitanHeroActionPlan161>(forecast.DeterministicDebugEvidence);
                var action=forecast.MemberActions[0];var source=players[sourceUnionIndex];
                int casterIndex=source.FindMemberIndex(plan.HeroId);
                var frozen=runtime.Abilities.SingleOrDefault(a=>a.HeroId==plan.HeroId&&a.ArtId==plan.ArtId);
                if(frozen==null||runtime.UsedHeroIds.Contains(plan.HeroId)||casterIndex<0||source.Members[casterIndex].Downed||
                   !campaign.Guild.Recruits.Any(r=>r.RecruitId==plan.HeroId)||plan.Round!=battle.Round||source.UnionId!=plan.UnionId||forecast.UnionId!=source.UnionId||
                   forecast.CommandId!="TITAN161_"+plan.ArtId||action.ActorMemberId!=plan.HeroId||action.ArtId!=plan.ArtId||
                   action.SharedApCost!=TitanHeroCatalog161.SharedAp||action.PersonalMpCost!=TitanHeroCatalog161.PersonalMp||
                   source.CurrentAp<forecast.SharedApCost||source.Members[casterIndex].CurrentMp<TitanHeroCatalog161.PersonalMp||
                   forecast.SharedApCost!=forecast.MemberActions.Sum(a=>a.SharedApCost)||forecast.CombinedMpCost!=forecast.MemberActions.Sum(a=>a.PersonalMpCost)||
                   CanonicalJson.Sha256Hex(plan)!=forecast.GenerationIdentity||plan.Targets.Count<1||plan.Targets.Count>64||
                   plan.Targets.Select(t=>t.MemberId).Distinct().Count()!=plan.Targets.Count)
                    throw new InvalidOperationException("TITAN161_COMMITTED_PLAN_MISMATCH");
                var recipe=TitanHeroCatalog161.Recipe(plan.ArtId);
                if(plan.Targets.Any(t=>t.ChannelAmounts.Length!=recipe.Channels.Count||t.ChannelAmounts.Any(n=>n<0))||
                   plan.Targets.Sum(t=>t.ChannelAmounts.Sum(n=>(long)n))>plan.Budget||
                   recipe.TargetScope=="ONE_FALLEN_ALLY"&&plan.Targets.Count!=1)
                    throw new InvalidOperationException("TITAN161_BUDGET_MISMATCH");
                var caster=source.Members[casterIndex];var members=source.Members.ToArray();
                members[casterIndex]=caster.With(currentMp:caster.CurrentMp-TitanHeroCatalog161.PersonalMp,guarding:false);
                players[sourceUnionIndex]=source.With(members:members,currentAp:source.CurrentAp-TitanHeroCatalog161.SharedAp,guarding:false);
                runtime=runtime.With(usedHeroIds:runtime.UsedHeroIds.Concat(new[]{plan.HeroId}));
                SetTitanHeroRuntime161(events,runtime);
                // Native presentation starts the Union chapter at this authority
                // event. The following Titan audit event never applies an effect.
                events.Add(Event(events.Count,battle.Round,"FORECAST_COMMITTED",BattleSide.Player,source.UnionId,caster.MemberId,forecast.CommandId,
                    source.DisplayName+" commits "+recipe.Name+"; all displayed companion actions follow within this same Union command.",forecast.SharedApCost,
                    source.UnionId,caster.MemberId,plan.TargetUnionId,action.TargetMemberId));
                events.Add(Event(events.Count,battle.Round,"TITAN_GOLD_COMMITTED",BattleSide.Player,source.UnionId,caster.MemberId,plan.ArtId,
                    caster.DisplayName+" commits "+recipe.Name+" for 6 AP and 10 personal MP; the encounter use is spent.",6,source.UnionId,caster.MemberId,plan.TargetUnionId,action.TargetMemberId));
                var targets=recipe.IsFriendly?players:enemies;
                int targetIndex=FindUnionIndex(targets,plan.TargetUnionId);
                if(targetIndex>=0&&recipe.Secondary=="CLEANSE_ONE")CleanseOneTitanHero161(battle.Round,targetIndex,targets,action,events);
                bool successfulDamage=false;
                for(int c=0;c<recipe.Channels.Count;c++)
                    for(int burst=0;burst<recipe.Bursts;burst++)
                        foreach(var share in plan.Targets)
                        {
                            int amount=share.ChannelAmounts[c]/recipe.Bursts+(burst<share.ChannelAmounts[c]%recipe.Bursts?1:0);
                            if(amount<=0)continue;
                            targetIndex=FindUnionIndex(targets,plan.TargetUnionId);if(targetIndex<0||targets[targetIndex].Retreated)continue;
                            int memberIndex=targets[targetIndex].FindMemberIndex(share.MemberId);if(memberIndex<0)continue;
                            var target=targets[targetIndex];var member=target.Members[memberIndex];string channel=recipe.Channels[c];
                            if(channel=="REVIVE_HP")
                            {
                                if(!member.Downed)continue;
                                int revived=Math.Min(amount,member.MaximumHp);if(revived<=0)continue;
                                var changed=target.Members.ToArray();changed[memberIndex]=member.With(currentHp:revived,stabilized:false,guarding:false);
                                targets[targetIndex]=target.With(members:changed,cohesion:Math.Max(1,target.Cohesion),engagement:EngagementState.Reinforcing);
                                TitanHeroImpact161(events,battle,source,caster,plan,target,member,"TITAN_REVIVE",revived);continue;
                            }
                            if(member.Downed||target.IsDefeated)continue;
                            if(channel=="HEAL")
                            {
                                int healed=Math.Min(amount,member.MaximumHp-member.CurrentHp);if(healed<=0)continue;
                                var changed=target.Members.ToArray();changed[memberIndex]=member.With(currentHp:member.CurrentHp+healed);
                                targets[targetIndex]=target.With(members:changed);
                                TitanHeroImpact161(events,battle,source,caster,plan,target,member,"TITAN_HEAL",healed);
                            }
                            else if(channel=="BARRIER")
                            {
                                runtime=GetTitanHeroRuntime161(events)??runtime;
                                int ward=Math.Min(amount,member.MaximumHp);
                                runtime=AddTitanEffect161(runtime,new TitanHeroTimedEffect161("BARRIER",target.UnionId,member.MemberId,source.UnionId,recipe.Id,ward,battle.Round,battle.Round+1,recipe.BarrierScope));
                                SetTitanHeroRuntime161(events,runtime);
                                TitanHeroImpact161(events,battle,source,caster,plan,target,member,"TITAN_BARRIER",ward);
                            }
                            else
                            {
                                var kind=channel=="MYSTIC_DAMAGE"?BattleActionKind.Mystic:BattleActionKind.Martial;
                                int hit=(target.Guarding||member.Guarding)?Math.Max(1,amount/2):amount;
                                var strike=new BattlePlannedActionState(caster.MemberId,caster.DisplayName,target.UnionId,member.MemberId,recipe.Id,recipe.Name,kind,0,0,-hit,0,0,"Committed target share",false,false,recipe.Id);
                                int actual=ResolveAttack(battle.Round,strike,players,enemies,events,BattleSide.Player,hit,0,0);
                                runtime=GetTitanHeroRuntime161(events)??runtime;
                                successfulDamage|=actual>0;
                            }
                        }
                if(successfulDamage&&(recipe.Secondary=="ARMOR_BREAK"||recipe.Secondary=="SLOW"||recipe.Secondary=="THREAT"))
                {
                    runtime=GetTitanHeroRuntime161(events)??runtime;
                    runtime=AddTitanEffect161(runtime,new TitanHeroTimedEffect161(recipe.Secondary,plan.TargetUnionId,"",source.UnionId,recipe.Id,
                        recipe.Secondary=="THREAT"?1:1500,battle.Round+1,battle.Round+1));
                    SetTitanHeroRuntime161(events,runtime);
                    events.Add(Event(events.Count,battle.Round,"TITAN_STATUS",BattleSide.Player,source.UnionId,caster.MemberId,recipe.Id,
                        recipe.Secondary+" applied for the next uncommitted window.",recipe.Secondary=="THREAT"?1:1500,source.UnionId,caster.MemberId,plan.TargetUnionId,""));
                }
                runtime=GetTitanHeroRuntime161(events)??runtime;return true;
            }
            catch(Exception e){failure="TITAN161_RESOLVE_REJECTED:"+e.Message;return false;}
        }

        static void TitanHeroImpact161(List<BattleEventState> events,BattleState battle,BattleUnionState source,BattleMemberState caster,TitanHeroActionPlan161 plan,
            BattleUnionState target,BattleMemberState member,string type,int amount)
        {
            string text=caster.DisplayName+" uses "+TitanHeroCatalog161.Recipe(plan.ArtId).Name+" on "+member.DisplayName+": "+amount+" "+type.Substring(6).ToLowerInvariant()+".";
            events.Add(Event(events.Count,battle.Round,type,BattleSide.Player,target.UnionId,member.MemberId,plan.ArtId,text,amount,
                source.UnionId,caster.MemberId,target.UnionId,member.MemberId));
            string native=type=="TITAN_HEAL"?"RESTORATION":type=="TITAN_REVIVE"?"REVIVED":"ALLY_PROTECTED";
            // Exactly one native numeric impact drives the existing HP/pose ledger.
            // The audit event above is deliberately not a second native impact.
            events.Add(Event(events.Count,battle.Round,native,BattleSide.Player,target.UnionId,member.MemberId,plan.ArtId,text,amount,
                source.UnionId,caster.MemberId,target.UnionId,member.MemberId));
        }

        static void CleanseOneTitanHero161(int round,int targetIndex,List<BattleUnionState> targets,BattlePlannedActionState action,List<BattleEventState> events)
        {
            var target=targets[targetIndex];bool removed=TryCleanseTitanHarmful161(target.UnionId,events);
            if(!removed&&target.Engagement==EngagementState.Broken)
            {targets[targetIndex]=target.With(engagement:EngagementState.Reinforcing,cohesion:Math.Max(1,target.Cohesion));removed=true;}
            if(removed)
            {
                string sourceUnionId=FindMemberUnionId(targets,action.ActorMemberId);
                events.Add(Event(events.Count,round,"TITAN_CLEANSE_ONE",BattleSide.Player,target.UnionId,action.TargetMemberId,action.ArtId,
                    action.ActorName+" removes one harmful effect before healing.",1,sourceUnionId,action.ActorMemberId,target.UnionId,action.TargetMemberId));
                // CLEANSED means a full cleanse to the older boss reducer; this
                // one-instance cleanse uses native support playback instead.
                events.Add(Event(events.Count,round,"ALLY_SUPPORT",BattleSide.Player,target.UnionId,action.TargetMemberId,action.ArtId,
                    action.ActorName+" cleanses one harmful effect.",1,sourceUnionId,action.ActorMemberId,target.UnionId,action.TargetMemberId));
            }
        }

        static TitanHeroBattleRuntime161 AddTitanEffect161(TitanHeroBattleRuntime161 runtime,TitanHeroTimedEffect161 incoming)
        {
            var effects=runtime.Effects.ToList();var existing=effects.FirstOrDefault(e=>e.Kind==incoming.Kind&&e.UnionId==incoming.UnionId&&e.MemberId==incoming.MemberId&&e.Scope==incoming.Scope);
            if(existing!=null)
            {
                if(existing.Amount>incoming.Amount&&existing.ThroughRound>=incoming.FromRound)return runtime;
                effects.Remove(existing);
            }
            effects.Add(incoming);return runtime.With(effects:effects);
        }

        internal static int AdjustTitanHeroDamage161(int round,BattleSide actingSide,string actorUnionId,string targetUnionId,string targetMemberId,BattleActionKind kind,int damage,
            List<BattleEventState> events,ref TitanHeroBattleRuntime161 runtime)
        {
            if(runtime==null||damage<=0)return damage;
            if(actingSide==BattleSide.Player&&kind!=BattleActionKind.Mystic&&runtime.Effects.Any(e=>e.Kind=="ARMOR_BREAK"&&e.UnionId==targetUnionId&&e.FromRound<=round&&e.ThroughRound>=round))
                damage=CapTitan161((new BigInteger(damage)*11500+9999)/10000);
            var effects=runtime.Effects.ToList();
            var ward=effects.Where(e=>e.Kind=="BARRIER"&&e.UnionId==targetUnionId&&e.MemberId==targetMemberId&&e.FromRound<=round&&e.ThroughRound>=round&&
                (e.Scope!="MYSTIC_ONLY"||kind==BattleActionKind.Mystic)).OrderByDescending(e=>e.Amount).ThenBy(e=>e.ArtId,StringComparer.Ordinal).FirstOrDefault();
            if(ward!=null)
            {
                int blocked=Math.Min(damage,ward.Amount);damage-=blocked;effects.Remove(ward);if(ward.Amount>blocked)effects.Add(ward.WithAmount(ward.Amount-blocked));
                runtime=runtime.With(effects:effects);
                if(blocked>0)events.Add(Event(events.Count,round,"TITAN_BARRIER_ABSORB",actingSide,targetUnionId,targetMemberId,ward.ArtId,
                    "The Titan ward absorbs "+blocked+" damage.",blocked,actorUnionId,"",targetUnionId,targetMemberId));
            }
            return damage;
        }
        internal static void EndTitanHeroRound161(int completedRound,ref TitanHeroBattleRuntime161 runtime)
        {if(runtime!=null)runtime=runtime.With(effects:runtime.Effects.Where(e=>e.ThroughRound>completedRound));}
        internal static string TitanHeroForcedTarget161(TitanHeroBattleRuntime161 runtime,int round,string enemyUnionId)=>runtime?.Effects
            .Where(e=>e.Kind=="THREAT"&&e.UnionId==enemyUnionId&&e.FromRound<=round&&e.ThroughRound>=round)
            .OrderBy(e=>e.ArtId,StringComparer.Ordinal).Select(e=>e.SourceUnionId).FirstOrDefault();
        internal static int TitanHeroSpeedBasisPoints161(TitanHeroBattleRuntime161 runtime,int round,string enemyUnionId)=>runtime?.Effects
            .Any(e=>e.Kind=="SLOW"&&e.UnionId==enemyUnionId&&e.FromRound<=round&&e.ThroughRound>=round)==true?8500:10000;
    }
}
