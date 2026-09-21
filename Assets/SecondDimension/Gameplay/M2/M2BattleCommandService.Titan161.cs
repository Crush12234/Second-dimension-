using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanTrials160;
using SecondDimension.Gameplay.TitanHeroes161;

namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2BattleCommandService
    {
        private static string ExtendTitanHash161(BattleState battle,string legacyHash)
        {
            if(battle.TitanRuntime161==null&&battle.TitanHeroes161==null)return legacyHash;
            return CanonicalJson.Sha256Hex(new{OriginalBattleHash=legacyHash,battle.TitanRuntime161,battle.TitanHeroes161});
        }
        // A context belongs to one explicit native round event buffer. It is
        // neither thread-local nor global current-battle state, so nested/test
        // battles cannot inherit another battle's effects. All hits already
        // carry this buffer through the native resolver, including original SSS.
        private sealed class TitanRoundContext161
        {
            internal BattleState Battle;
            internal TitanBossRuntime161 Boss;
            internal TitanHeroBattleRuntime161 Heroes;
            internal readonly List<TitanVenomTick161> Venom;
            internal readonly List<TitanSlow161> Slows;
            internal int Barrier, BarrierExpiry, Exposed;
            internal long ChannelDamage;
            internal TitanRoundContext161(BattleState battle)
            {
                Battle=battle;Boss=battle.TitanRuntime161;Heroes=battle.TitanHeroes161;
                Venom=new List<TitanVenomTick161>(Boss?.Venom??Array.Empty<TitanVenomTick161>());
                Slows=new List<TitanSlow161>(Boss?.Slows??Array.Empty<TitanSlow161>());
                Barrier=Boss?.BarrierRemaining??0;BarrierExpiry=Boss?.BarrierExpiresAfterRound??0;
                Exposed=Boss?.ExposedThroughRound??0;ChannelDamage=Boss?.HealingChannelDamage??0;
            }
        }
        private static readonly ConditionalWeakTable<List<BattleEventState>,TitanRoundContext161> TitanRounds161 =
            new ConditionalWeakTable<List<BattleEventState>,TitanRoundContext161>();

        public static IReadOnlyList<string> TitanArmyUnionIds161(CampaignState campaign,M2CombatContent content) =>
            CreatePlayerUnionsForRules101(campaign,content,null,10,true).Select(u=>u.UnionId).ToArray();

        public static EncounterRoster070 CreateTitanRoster161(M2CombatContent content,TitanAttempt160 attempt)
        {
            if(content?.TitanTrials161==null||attempt==null||content.TitanTrials161.Identity!=attempt.CatalogIdentity)
                throw new InvalidOperationException("TITAN161_BOUND_CATALOG_REQUIRED");
            var trial=content.TitanTrials161.Trial(attempt.Slot);
            if(trial.RecipeIdentity!=attempt.RecipeIdentity)throw new InvalidOperationException("TITAN161_RECIPE_MISMATCH");
            var enemy=content.TitanEnemy161(attempt.Slot,attempt.Tier);
            var member=new EncounterEnemyMember070(TitanBossPlanner160.BossMemberId(trial),enemy.Id,trial.SourceFamilyId,1,enemy);
            var source=new M2EnemyUnionDefinition(TitanBossPlanner160.BossUnionId(trial),trial.Name,enemy.Id,"TITAN_SINGLE_161",10,100,new[]{enemy.Id},1000);
            var union=new EncounterEnemyUnion070(source.Id,source.Id,source,new[]{member},new[]{trial.SourceFamilyId});
            return new EncounterRoster070("TITAN_ROSTER161_"+attempt.AttemptId,
                CanonicalJson.Sha256Hex(new{attempt.AttemptId,attempt.CatalogIdentity,attempt.RecipeIdentity,attempt.Tier}),
                new[]{union},new[]{trial.SourceFamilyId});
        }

        private static TitanAttempt160 ValidateTitanStart161(CampaignState campaign,M2CombatContent content,
            EncounterLaunchRequest017D request,string battleId,EncounterRoster070 roster,IReadOnlyList<BattleUnionState> players)
        {
            var active=campaign.TitanTrials160?.Active;
            if(active==null||active.BattleId!=battleId)
            {
                if(battleId.StartsWith("TITAN_BATTLE160_",StringComparison.Ordinal))throw new InvalidOperationException("TITAN161_OWNER_REQUIRED");
                return null;
            }
            if(request==null||request.BattleId!=active.BattleId||request.EnemyUnionCount!=1||
                campaign.Guild.GuildCity?.PendingEncounter==null||
                CanonicalJson.Serialize(request)!=CanonicalJson.Serialize(campaign.Guild.GuildCity.PendingEncounter)||
                !campaign.Guild.Development.HasAdventureAuthority(GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request))||
                roster==null||CanonicalJson.Serialize(roster)!=CanonicalJson.Serialize(CreateTitanRoster161(content,active))||
                !players.Select(u=>u.UnionId).SequenceEqual(active.AlliedUnionIds))
                throw new InvalidOperationException("TITAN161_COMMITTED_START_MISMATCH");
            return active;
        }

        private static IReadOnlyList<BattleUnionState> CreateFixedTitanEnemies161(M2CombatContent content,TitanAttempt160 attempt)
        {
            var trial=content.TitanTrials161.Trial(attempt.Slot);var stats=trial.Stats(attempt.Tier);var enemy=content.TitanEnemy161(attempt.Slot,attempt.Tier);
            var boss=new BattleMemberState(TitanBossPlanner160.BossMemberId(trial),trial.Name,enemy.Id,
                stats.MaximumHp,stats.MaximumHp,100,100,stats.Attack,stats.MagicAttack,new[]{"TITAN161"},false,false,false,
                enemy.ArtIds,0,0,"",enemyArtBaseId090:trial.SourceFamilyId,
                enemyArtVariantId090:trial.SourceFamilyId+"_VAR_01",visualVariantSeed090:1);
            return new[]{new BattleUnionState(TitanBossPlanner160.BossUnionId(trial),trial.Name,BattleSide.Enemy,
                boss.MemberId,new[]{boss},"TITAN_SINGLE_161","Titan",true,"",10,10,100,10000,EngagementState.Open,false,false,0)};
        }

        public static bool HasValidTitanOwner161(CampaignState campaign,M2CombatContent content)
        {
            var runtime=campaign?.Battle?.TitanRuntime161;var active=campaign?.TitanTrials160?.Active;
            return runtime!=null&&active!=null&&content?.TitanTrials161!=null&&runtime.Attempt.AttemptId==active.AttemptId&&
                runtime.Attempt.BattleId==campaign.Battle.BattleId&&runtime.Attempt.CatalogIdentity==content.TitanTrials161.Identity&&
                runtime.Attempt.RecipeIdentity==content.TitanTrials161.Trial(active.Slot).RecipeIdentity;
        }

        private static BattleState CommitTitanForecast161(CampaignState campaign,BattleState battle,M2CombatContent content)
        {
            var runtime=battle.TitanRuntime161;
            if(runtime==null)return battle;
            if(campaign.TitanTrials160?.Active?.AttemptId!=runtime.Attempt.AttemptId||content.TitanTrials161==null)
                throw new InvalidOperationException("TITAN161_FORECAST_OWNER_REQUIRED");
            if(runtime.Action!=null)
            {
                if(runtime.Action.Round!=battle.Round)throw new InvalidOperationException("TITAN161_STALE_ACTION");
                return battle;
            }
            var plan=BindTitanHeroInfluence161(battle,TitanBossPlanner160.Prepare(content.TitanTrials161,runtime.Attempt,battle));
            var order=battle.PlayerUnions.Select((u,index)=>new{Union=u,Index=index,
                Speed=TitanUnionSpeed161(campaign,u,runtime,battle.Round)}).OrderByDescending(x=>x.Speed).ThenBy(x=>x.Index).Select(x=>x.Union.UnionId).ToArray();
            var action=TitanBossAction161.From(plan,order,TitanHeroSpeedBasisPoints161(battle.TitanHeroes161,battle.Round,battle.EnemyUnions[0].UnionId));
            var resetChannel=action.Kind=="WIND_UP"&&action.Channel=="SELF_HEAL";
            runtime=new TitanBossRuntime161(runtime.Attempt,action,runtime.Venom,runtime.Slows,runtime.BarrierRemaining,
                runtime.BarrierExpiresAfterRound,runtime.ExposedThroughRound,resetChannel?0:runtime.HealingChannelDamage);
            var log=new List<BattleEventState>(battle.EventLog);
            var description=TitanTelegraphText161(content.TitanTrials161.Trial(runtime.Attempt.Slot),action);
            log.Add(Event(log.Count,battle.Round,"TITAN_FORECAST161",BattleSide.Enemy,
                battle.EnemyUnions[0].UnionId,battle.EnemyUnions[0].LeaderMemberId,action.Key,description,0));
            return battle.With(titanRuntime161:runtime,eventLog:log.AsReadOnly());
        }

        private static long TitanUnionSpeed161(CampaignState campaign,BattleUnionState union,TitanBossRuntime161 runtime,int round)
        {
            var members=union.Members.Where(m=>!m.Downed).Select(m=>campaign.Guild.Recruits.FirstOrDefault(r=>r.RecruitId==m.MemberId)).Where(r=>r!=null).ToArray();
            long speed=members.Length==0?10000:members.Sum(r=>10000L+100L*r.TacticalAptitude+100L*r.Progression.AgilityBonus)/members.Length;
            return runtime.Slows.Any(s=>s.TargetUnionId==union.UnionId&&s.ExpiresAfterRound>=round)?speed*8500/10000:speed;
        }

        private static TitanActionCommitment160 BindTitanHeroInfluence161(BattleState battle,TitanActionCommitment160 plan)
        {
            // Threat only influences an as-yet uncommitted single-target attack
            // or warning. An impact always retains its earlier warning targets.
            if((plan.Action.Kind!="WIND_UP"&&plan.Action.Kind!="DAMAGE")||plan.TargetUnionIds.Count!=1||
                !plan.Channel.EndsWith("_DAMAGE",StringComparison.Ordinal))return plan;
            var forced=TitanHeroForcedTarget161(battle.TitanHeroes161,battle.Round,battle.EnemyUnions[0].UnionId);
            if(string.IsNullOrEmpty(forced)||!battle.PlayerUnions.Any(u=>u.UnionId==forced&&IsActive(u)))return plan;
            var next=plan.NextWindow;
            if(plan.Action.Kind=="WIND_UP")next=new TitanBossWindow160(next.PhaseIndex,next.ActionIndex,next.CompletedCycles,
                next.LastResolvedRound,next.HealChannelsAccepted,next.ExhaustedHealCycle,next.WarningKey,next.WarningChannel,
                next.WarningRound,new[]{forced},next.WarningBossHp);
            return new TitanActionCommitment160(battle.TitanRuntime161.Attempt,battle,plan.Action,plan.Channel,new[]{forced},next);
        }

        public static string TitanTelegraph161(BattleState battle)
        {
            var action=battle?.TitanRuntime161?.Action;
            if(action==null)return "";
            var targets=string.Join(", ",action.Targets.Select(id=>battle.PlayerUnions.FirstOrDefault(u=>u.UnionId==id)?.DisplayName??battle.EnemyUnions.FirstOrDefault(u=>u.UnionId==id)?.DisplayName??id));
            return action.Kind=="RECOVERY"?"The Titan is recovering. Press the opening.":
                (action.Kind=="WIND_UP"?"Warning: ":action.Kind=="IMPACT"?"Impact: ":"Next: ")+action.Name+" · "+action.Channel.Replace('_',' ')+" · "+targets;
        }
        public static string TitanForecastNotice161(BattleState battle)
        {
            var runtime=battle?.TitanRuntime161;if(runtime==null)return "";
            var parts=new List<string>{TitanTelegraph161(battle)};
            if(runtime.BarrierRemaining>0)parts.Add("Barrier "+runtime.BarrierRemaining+" · expires after round "+runtime.BarrierExpiresAfterRound);
            if(runtime.ExposedThroughRound>=battle.Round)parts.Add("Exposed: +20% damage received through round "+runtime.ExposedThroughRound);
            if(runtime.Venom.Count>0)parts.Add("Venom: "+string.Join(", ",runtime.Venom.Select(v=>
                (battle.PlayerUnions.FirstOrDefault(u=>u.UnionId==v.TargetUnionId)?.DisplayName??v.TargetUnionId)+" tick at round "+v.DueRound)));
            if(runtime.Slows.Count>0)parts.Add("Slow 15%: "+string.Join(", ",runtime.Slows.Select(s=>
                (battle.PlayerUnions.FirstOrDefault(u=>u.UnionId==s.TargetUnionId)?.DisplayName??s.TargetUnionId)+" through round "+s.ExpiresAfterRound)));
            if(runtime.Action?.EnemySpeedBasisPoints<10000)parts.Add("Titan initiative: "+runtime.Action.EnemySpeedBasisPoints/100+"% this round; the warned action keeps its timing.");
            return string.Join("\n",parts.Where(x=>!string.IsNullOrEmpty(x)));
        }
        private static string TitanTelegraphText161(TitanTrialDefinition160 trial,TitanBossAction161 action)=>
            trial.Action(action.Key).PlayerCopy+" ["+action.Channel+"] "+string.Join(", ",action.Targets);

        private static void BeginTitanRound161(BattleState battle,List<BattleEventState> events)
        {
            if(battle.TitanRuntime161!=null||battle.TitanHeroes161!=null)
                TitanRounds161.Add(events,new TitanRoundContext161(battle));
        }
        private static void ValidateTitanAction161(BattleState battle,M2CombatContent content)
        {
            if(battle.TitanRuntime161==null)return;
            var runtime=battle.TitanRuntime161;var action=runtime.Action;
            var expected=BindTitanHeroInfluence161(battle,TitanBossPlanner160.Prepare(content.TitanTrials161,runtime.Attempt,battle));
            if(action==null||action.Round!=battle.Round||action.Key!=expected.Action.Key||action.Kind!=expected.Action.Kind||action.Name!=expected.Action.PlayerCopy||
                action.Channel!=expected.Channel||!action.Targets.SequenceEqual(expected.TargetUnionIds)||
                action.EnemySpeedBasisPoints!=TitanHeroSpeedBasisPoints161(battle.TitanHeroes161,battle.Round,battle.EnemyUnions[0].UnionId)||
                !action.BudgetShares.SequenceEqual(expected.BudgetSharesBasisPoints)||
                CanonicalJson.Serialize(action.NextWindow)!=CanonicalJson.Serialize(expected.NextWindow))
                throw new InvalidOperationException("TITAN161_SAVED_FORECAST_MISMATCH");
        }
        internal static TitanHeroBattleRuntime161 GetTitanHeroRuntime161(List<BattleEventState> events)=>
            TitanRounds161.TryGetValue(events,out var context)?context.Heroes:null;
        internal static void SetTitanHeroRuntime161(List<BattleEventState> events,TitanHeroBattleRuntime161 runtime)
        {
            if(TitanRounds161.TryGetValue(events,out var context))context.Heroes=runtime;
        }

        private static int AdjustNativeDamage161(int round,BattleSide side,string actorUnionId,string targetUnionId,
            string targetMemberId,BattleActionKind kind,int damage,List<BattleUnionState> targets,List<BattleEventState> events,
            string sourceArtId163 = null)
        {
            // Lingering venom is a saved status tick, even though the legacy
            // impact playback uses the physical channel. DEF does not erase it.
            if(sourceArtId163!="TITAN_TRIAL_003_VENOM")
                damage=AdjustDefenseDamage163(targetMemberId,kind,damage,events);
            if(damage<=0||!TitanRounds161.TryGetValue(events,out var context))return damage;
            damage=AdjustTitanHeroDamage161(round,side,actorUnionId,targetUnionId,targetMemberId,kind,damage,events,ref context.Heroes);
            if(context.Boss==null||side!=BattleSide.Player||targetUnionId!=context.Battle.EnemyUnions[0].UnionId)return damage;
            if(context.Exposed>=round)damage=checked((int)Math.Min(int.MaxValue,((long)damage*12000+9999)/10000));
            if(context.Barrier>0)
            {
                int absorbed=Math.Min(context.Barrier,damage);context.Barrier-=absorbed;damage-=absorbed;
                events.Add(Event(events.Count,round,"TITAN_BARRIER_ABSORB161",BattleSide.Enemy,targetUnionId,targetMemberId,
                    "TITAN_TRIAL_009_IMPACT","Heartwood Mantle absorbs "+absorbed+" damage.",absorbed,actorUnionId,"",targetUnionId,targetMemberId));
                // Protection is not HP loss: INTERCEPTION would animate an
                // additional, false HP decrement in the native playback.
                events.Add(Event(events.Count,round,"ALLY_PROTECTED",BattleSide.Enemy,targetUnionId,targetMemberId,
                    "TITAN_TRIAL_009_IMPACT","Heartwood Mantle absorbs "+absorbed+" damage.",absorbed,
                    targetUnionId,targetMemberId,targetUnionId,targetMemberId));
                if(context.Barrier==0)
                {
                    context.Exposed=checked(round+1);context.BarrierExpiry=0;
                    events.Add(Event(events.Count,round,"TITAN_EXPOSED161",BattleSide.Enemy,targetUnionId,targetMemberId,
                        "TITAN_TRIAL_009_RECOVER","Heartwood breaks: the Titan takes 20% more damage through the next round.",2000));
                }
            }
            bool channel=context.Boss.Action?.Channel=="SELF_HEAL"||context.Boss.Attempt.BossWindow.WarningChannel=="SELF_HEAL";
            if(channel)
            {
                var target=targets.FirstOrDefault(u=>u.UnionId==targetUnionId)?.Members.FirstOrDefault(m=>m.MemberId==targetMemberId);
                context.ChannelDamage=checked(context.ChannelDamage+Math.Min(damage,target?.CurrentHp??0));
            }
            return damage;
        }

        private static bool ResolveTitanEnemyTurn161(BattleState battle,M2CombatContent content,List<BattleUnionState> players,
            List<BattleUnionState> enemies,List<BattleEventState> events)
        {
            if(battle.TitanRuntime161==null)return false;
            if(!TitanRounds161.TryGetValue(events,out var context))throw new InvalidOperationException("TITAN161_ROUND_CONTEXT_REQUIRED");
            var runtime=context.Boss;var action=runtime.Action;
            if(action==null||action.Round!=battle.Round)throw new InvalidOperationException("TITAN161_COMMITTED_ACTION_REQUIRED");
            var trial=content.TitanTrials161.Trial(runtime.Attempt.Slot);var recipe=trial.Action(action.Key);
            if(recipe.Kind!=action.Kind||action.BudgetShares.Sum()!=recipe.TotalBudgetBasisPoints)
                throw new InvalidOperationException("TITAN161_COMMITTED_RECIPE_MISMATCH");
            var boss=enemies[0].Members[0];
            if(boss.Downed)return true;
            if(action.Kind=="WIND_UP"||action.Kind=="RECOVERY")
            {
                events.Add(Event(events.Count,battle.Round,action.Kind=="WIND_UP"?"TITAN_WIND_UP161":"TITAN_RECOVERY161",
                    BattleSide.Enemy,enemies[0].UnionId,boss.MemberId,action.Key,recipe.PlayerCopy,0));
                TitanSupportEvent161(events,battle.Round,enemies[0],action,
                    action.Kind=="WIND_UP"?"ENEMY_SUPPORT_FORECAST":"RECOVERY",recipe.PlayerCopy,0);
                return true;
            }
            if(action.Channel=="SELF_HEAL")
            {
                if(context.ChannelDamage*10000>=boss.MaximumHp*800L)
                {
                    events.Add(Event(events.Count,battle.Round,"TITAN_HEAL_INTERRUPTED161",BattleSide.Player,enemies[0].UnionId,boss.MemberId,
                        action.Key,"Native damage interrupts the healing channel.",0));
                    TitanSupportEvent161(events,battle.Round,enemies[0],action,"ENEMY_SUPPORT_FORECAST",
                        "Sunfire Renewal is interrupted. No HP is restored.",0);
                }
                else
                {
                    int heal=Math.Min(boss.MaximumHp-boss.CurrentHp,checked((int)((boss.MaximumHp*600L+9999)/10000)));
                    enemies[0]=enemies[0].With(members:new[]{boss.With(currentHp:boss.CurrentHp+heal)});
                    events.Add(Event(events.Count,battle.Round,"TITAN_HEAL161",BattleSide.Enemy,enemies[0].UnionId,boss.MemberId,action.Key,
                        "Sunfire Renewal restores "+heal+" HP (6% maximum).",heal));
                    TitanSupportEvent161(events,battle.Round,enemies[0],action,"RESTORATION",
                        "Sunfire Renewal restores "+heal+" HP (6% maximum).",heal);
                }
                context.ChannelDamage=0;return true;
            }
            if(action.Channel=="SELF_BARRIER")
            {
                context.Barrier=checked((int)((boss.MaximumHp*800L+9999)/10000));context.BarrierExpiry=checked(battle.Round+1);
                events.Add(Event(events.Count,battle.Round,"TITAN_BARRIER161",BattleSide.Enemy,enemies[0].UnionId,boss.MemberId,action.Key,
                    "Heartwood Mantle: "+context.Barrier+" barrier until the end of next round.",context.Barrier));
                TitanSupportEvent161(events,battle.Round,enemies[0],action,"GUARD",
                    "Heartwood Mantle: "+context.Barrier+" barrier until the end of next round.",context.Barrier);
                return true;
            }
            int offense=action.Channel=="MYSTIC_DAMAGE"?boss.MagicAttack:boss.Attack;
            int totalClean=checked((int)(((long)offense*action.BudgetShares.Sum()+9999)/10000));
            for(int i=0;i<action.Targets.Count;i++)
            {
                // Scale one clean budget, then split it. Rounding per target
                // would silently multiply damage as the army grows.
                int clean=totalClean/action.Targets.Count+(i<totalClean%action.Targets.Count?1:0);
                if(recipe.Special=="VENOM_TWO_TICKS")
                {
                    int first=(int)(clean*6000L/10000),second=(int)(clean*2000L/10000),third=clean-first-second;
                    TitanHit161(battle.Round,enemies,players,events,action.Targets[i],action.Key,recipe.PlayerCopy,action.Channel,first);
                    context.Venom.Add(new TitanVenomTick161(action.Targets[i],second,battle.Round));
                    context.Venom.Add(new TitanVenomTick161(action.Targets[i],third,checked(battle.Round+1)));
                }
                else
                {
                    int dealt=TitanHit161(battle.Round,enemies,players,events,action.Targets[i],action.Key,recipe.PlayerCopy,action.Channel,clean);
                    if(recipe.Special=="FIXED_SLOW"&&dealt>0)
                    {
                        context.Slows.RemoveAll(x=>x.TargetUnionId==action.Targets[i]);
                        context.Slows.Add(new TitanSlow161(action.Targets[i],checked(battle.Round+1)));
                        events.Add(Event(events.Count,battle.Round,"TITAN_SLOW161",BattleSide.Enemy,action.Targets[i],"",action.Key,
                            "Voidspore slows this Union's next Forecast initiative by 15%; committed actions keep their order.",1500));
                    }
                }
            }
            return true;
        }

        private static void TitanSupportEvent161(List<BattleEventState> events,int round,BattleUnionState boss,
            TitanBossAction161 action,string type,string text,int amount)
        {
            events.Add(Event(events.Count,round,type,BattleSide.Enemy,boss.UnionId,boss.LeaderMemberId,action.Key,text,amount,
                boss.UnionId,boss.LeaderMemberId,boss.UnionId,boss.LeaderMemberId));
        }

        private static int TitanHit161(int round,List<BattleUnionState> attackers,List<BattleUnionState> targets,
            List<BattleEventState> events,string targetUnionId,string artId,string artName,string channel,int cleanDamage)
        {
            var targetUnion=targets.FirstOrDefault(u=>u.UnionId==targetUnionId&&IsActive(u));
            if(targetUnion==null||cleanDamage<=0)
            {
                events.Add(Event(events.Count,round,"TITAN_SHARE_FIZZLE161",BattleSide.Enemy,targetUnionId,"",artId,
                    "The warned target is absent. Its damage share is lost.",0));return 0;
            }
            int index=FirstGuardingMemberIndex(targetUnion);if(index<0)index=FirstActiveMemberIndex(targetUnion);
            if(index<0)return 0;
            var boss=attackers[0].Members[0];var target=targetUnion.Members[index];
            var action=new BattlePlannedActionState(boss.MemberId,boss.DisplayName,targetUnionId,target.MemberId,artId,artName,
                channel=="MYSTIC_DAMAGE"?BattleActionKind.Mystic:BattleActionKind.Martial,0,0,-cleanDamage,-3,-300,
                "Committed Titan action",true,false,"TITAN161");
            return ResolveAttack(round,action,attackers,targets,events,BattleSide.Enemy);
        }

        internal static void CleanseTitanEffects161(string unionId,List<BattleEventState> events)
        {
            if(!TitanRounds161.TryGetValue(events,out var context))return;
            int removed=context.Venom.RemoveAll(x=>x.TargetUnionId==unionId)+context.Slows.RemoveAll(x=>x.TargetUnionId==unionId);
            if(removed>0)events.Add(Event(events.Count,context.Battle.Round,"TITAN_CLEANSED161",BattleSide.Player,unionId,"","",
                "Cleansing removes lingering venom and slow.",removed));
        }
        internal static bool TryCleanseTitanHarmful161(string unionId,List<BattleEventState> events)
        {
            if(!TitanRounds161.TryGetValue(events,out var context))return false;
            // One venom application is one harmful effect even when it has two
            // remaining ticks. Cleanse removes that application, not unrelated effects.
            bool removed=context.Venom.RemoveAll(v=>v.TargetUnionId==unionId)>0;
            if(!removed)removed=context.Slows.RemoveAll(s=>s.TargetUnionId==unionId)>0;
            if(removed)events.Add(Event(events.Count,context.Battle.Round,"TITAN_CLEANSED161",BattleSide.Player,unionId,"","",
                "One lingering Titan effect is cleansed.",1));
            return removed;
        }

        private static void EndTitanRound161(BattleState battle,List<BattleUnionState> players,List<BattleUnionState> enemies,
            List<BattleEventState> events)
        {
            if(!TitanRounds161.TryGetValue(events,out var context))return;
            if(context.Boss!=null)
            {
                // Native cleansing events remove only the matching Union's
                // outstanding effects before their due end-of-round tick.
                foreach(var cleansed in events.Where(e=>e.EventType=="CLEANSED").Select(e=>string.IsNullOrEmpty(e.TargetUnionId)?e.UnionId:e.TargetUnionId).Distinct().ToArray())
                    CleanseTitanEffects161(cleansed,events);
                if(!AllDefeated(enemies)&&CountActive(players)>0)
                    foreach(var tick in context.Venom.Where(x=>x.DueRound==battle.Round).ToArray())
                        TitanHit161(battle.Round,enemies,players,events,tick.TargetUnionId,"TITAN_TRIAL_003_VENOM","Lingering Venom","PHYSICAL_DAMAGE",tick.CleanDamage);
                context.Venom.RemoveAll(x=>x.DueRound<=battle.Round);
                context.Slows.RemoveAll(x=>x.ExpiresAfterRound<=battle.Round);
                if(context.Barrier>0&&context.BarrierExpiry<=battle.Round)
                {
                    context.Barrier=0;context.BarrierExpiry=0;context.Exposed=checked(battle.Round+1);
                    events.Add(Event(events.Count,battle.Round,"TITAN_BARRIER_EXPIRED161",BattleSide.Enemy,enemies[0].UnionId,enemies[0].LeaderMemberId,
                        "TITAN_TRIAL_009_RECOVER","The mantle expires, exposing heartwood through the next round.",2000));
                }
                var next=context.Boss.Action.NextWindow;
                if(context.Boss.Action.Kind=="RECOVERY")
                {
                    int phase=next.PhaseIndex;var boss=enemies[0].Members[0];long hpBp=boss.CurrentHp*10000L/boss.MaximumHp;
                    if(hpBp<=6500)phase=Math.Max(phase,1);if(hpBp<=3000)phase=2;
                    next=new TitanBossWindow160(phase,next.ActionIndex,next.CompletedCycles,next.LastResolvedRound,next.HealChannelsAccepted,
                        next.ExhaustedHealCycle,next.WarningKey,next.WarningChannel,next.WarningRound,next.WarningTargets,next.WarningBossHp);
                }
                context.Boss=new TitanBossRuntime161(context.Boss.Attempt.WithWindow(next),null,context.Venom,context.Slows,
                    context.Barrier,context.BarrierExpiry,context.Exposed,context.ChannelDamage);
            }
            if(context.Heroes!=null)EndTitanHeroRound161(battle.Round,ref context.Heroes);
        }

        private static TitanBossRuntime161 TitanRuntimeAfterRound161(List<BattleEventState> events)=>
            TitanRounds161.TryGetValue(events,out var context)?context.Boss:null;
    }
}
