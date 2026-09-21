using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanHeroes161;
using SecondDimension.Gameplay.TitanTrials160;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TitanHeroNative161Tests
    {
        static M2CombatContent Content;
        static M2CombatContent Combat()=>Content??(Content=M2CombatContent.LoadFromDirectory(
            Environment.GetEnvironmentVariable("SD_HERO161_CONTENT")??Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT")));

        [Test]
        public void NativeHeroCatalogAndAllEffectRecipesMatchTheAuthoredAuthority161()
        {
            string root=Environment.GetEnvironmentVariable("SD_HERO161_SOURCE")??Path.Combine(Application.streamingAssetsPath,"SecondDimension","TitanTrials160");
            var characters=(JArray)JObject.Parse(File.ReadAllText(Path.Combine(root,"TITAN_CHARACTER_BLUEPRINTS.json")))["characters"];
            var recipes=(JArray)JObject.Parse(File.ReadAllText(Path.Combine(root,"TITAN_EFFECT_RECIPES.json")))["recipes"];
            Assert.That(characters.Count,Is.EqualTo(TitanHeroCatalog161.All.Count));
            Assert.That(recipes.Count,Is.EqualTo(TitanHeroCatalog161.AllRecipes.Count));
            foreach(var source in characters)
            {
                Assert.That(TitanHeroCatalog161.TryGet((string)source["logical_hero_id"],out var hero),Is.True);
                Assert.That(hero.DisplayName,Is.EqualTo((string)source["display_name"]));
                var weights=source["stat_allocation_weights"];
                Assert.That(new[]{hero.HpWeight,hero.MpWeight,hero.AttackWeight,hero.MagicWeight,hero.DefenseWeight,hero.ResistanceWeight,hero.AgilityWeight},
                    Is.EqualTo(new[]{"hp","mp","attack","magic","defense","resistance","agility"}.Select(key=>(int)weights[key])));
            }
            foreach(var source in recipes)
            {
                var recipe=TitanHeroCatalog161.Recipe((string)source["id"]);
                Assert.That(recipe.Name,Is.EqualTo((string)source["name"]));
                Assert.That(recipe.BudgetBasisPoints,Is.EqualTo((int)source["rank_one_budget_bp"]));
                Assert.That(recipe.TargetScope,Is.EqualTo((string)source["target_scope"]));
                Assert.That(recipe.Allocation,Is.EqualTo((string)source["allocation"]));
                Assert.That(recipe.Bursts,Is.EqualTo((int)source["bursts"]));
                Assert.That(recipe.Secondary,Is.EqualTo((string)source["secondary"]["kind"]));
                Assert.That(recipe.BarrierScope,Is.EqualTo((string)source["barrier_scope"]));
                Assert.That(recipe.Channels,Is.EqualTo(source["channels"].Select(c=>(string)c["kind"])));
                Assert.That(recipe.ChannelWeights,Is.EqualTo(source["channels"].Select(c=>(int)c["weight_bp"])));
            }
        }

        [Test]
        public void ThirteenRealHeroesGrantOnceAndCopiesUseExistingBoundAscension161()
        {
            var oldIds=SssTenV4Roster090.All.Select(h=>h.HeroId).ToArray();
            Assert.That(oldIds.Length,Is.EqualTo(10));
            var empty=new RecruitState("BASE161",100,100,20,20);
            var guild=new GuildState("GUILD161",100,new[]{empty},Array.Empty<UnionState>());
            var state=new CampaignState("PROFILE161",161,"1.0",ModeRuleSnapshot.StandardDefaults(),guild.WithGuildCity(GuildCityState017D.Default(guild.Recruits)));
            foreach(var definition in TitanHeroCatalog161.All)
            {
                Assert.That(definition.HpWeight+definition.MpWeight+definition.AttackWeight+definition.MagicWeight+definition.DefenseWeight+definition.ResistanceWeight+definition.AgilityWeight,Is.EqualTo(100));
                state=TitanHeroRewards161.GrantCopy(state,definition.HeroId,"FIRST_"+definition.HeroId);
                var hero=state.Guild.Recruits.Single(r=>r.RecruitId==definition.HeroId);
                Assert.That(hero.AuthorityKind,Is.EqualTo(RecruitAuthorityKind.Normal));
                Assert.That(hero.Progression.Level,Is.EqualTo(1));
                Assert.That(hero.Progression.AscensionLevel,Is.Zero);
                Assert.That(hero.Progression.LearnedArtIds.Count,Is.EqualTo(3));
                Assert.That(state.Guild.GuildCity.MemberAssignments.Single(a=>a.RecruitId==hero.RecruitId).Kind,Is.EqualTo(GuildMemberAssignmentKind017D.Reserve));
                string first=CanonicalJson.Serialize(state);
                Assert.That(CanonicalJson.Serialize(TitanHeroRewards161.GrantCopy(state,definition.HeroId,"FIRST_"+definition.HeroId)),Is.EqualTo(first));
                state=TitanHeroRewards161.GrantCopy(state,definition.HeroId,"SECOND_"+definition.HeroId);
                Assert.That(SssTenV4Inventory090.AscensionCreditCount(state,definition.HeroId),Is.EqualTo(1));
                Assert.That(state.Guild.Recruits.Count(r=>r.RecruitId==hero.RecruitId),Is.EqualTo(1));
                var preview=SssTenV4HostRewards090.PreviewAscension(state,definition.HeroId);
                state=Require(SssTenV4HostRewards090.Ascend(state,definition.HeroId,"ASCEND_"+hero.RecruitId,preview.CurrentRank,preview.CreditCount,preview.StateGuard));
                Assert.That(state.Guild.Recruits.Single(r=>r.RecruitId==hero.RecruitId).Progression.AscensionLevel,Is.EqualTo(1));
                Assert.That(SssTenV4Inventory090.AscensionCreditCount(state,definition.HeroId),Is.Zero);
            }
            Assert.That(state.Guild.Recruits.Count,Is.EqualTo(14));
            Assert.That(state.Guild.TreasuryXp,Is.EqualTo(100));
            Assert.That(SssTenV4Roster090.All.Select(h=>h.HeroId),Is.EqualTo(oldIds));
            Assert.That(JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(state)).Guild.Recruits.Count,Is.EqualTo(14));
        }

        [TestCase(0,"EIDRAN")][TestCase(49,"EIDRAN")][TestCase(50,"MATCHED_COPY")]
        [TestCase(2549,"MATCHED_COPY")][TestCase(2550,"NONE")][TestCase(9999,"NONE")]
        public void RepeatTicketRangesAndSharedFirstUnlockGuaranteeAreExact161(int ticket,string expected)
        {
            Assert.That(TitanHeroRewards161.RepeatResultForTicket161(ticket,1,false),Is.EqualTo(expected));
            Assert.That(TitanHeroRewards161.RepeatResultForTicket161(ticket,99,false),Is.EqualTo(expected));
            Assert.That(TitanHeroRewards161.RepeatResultForTicket161(ticket,100,false),Is.EqualTo("EIDRAN"));
            Assert.That(TitanHeroRewards161.RepeatResultForTicket161(ticket,100,true),Is.EqualTo(expected));
        }

        [TestCase("HERO_TITAN_001")][TestCase("HERO_TITAN_013")]
        public void ClaimedRepeatDisplayReadsTheExactGrantReceiptInsteadOfRerolling161(string granted)
        {
            var state=Campaign(1);state=state.With(state.Guild.WithGuildCity(GuildCityState017D.Default(state.Guild.Recruits,state.Guild.Unions)),state.OpeningFlow);
            var receipt=new TitanSettlement160("ATTEMPT161",1,1,"BATTLE161","HASH161","REWARD161",BattleOutcome.Victory,"SETTLEMENT161");
            string overall="TITAN_HERO_GRANT161_"+receipt.RewardReceiptId;
            // Already-owned before the repeat, including Eidran, must still yield
            // the recorded bonus identity when the reward became an Ascension credit.
            state=TitanHeroRewards161.GrantCopy(state,TitanHeroCatalog161.EidranId,"FIRST_EIDRAN161");
            Assert.That(TitanHeroRewards161.RepeatGrantedHero161(state,receipt),Is.Null);
            state=TitanHeroRewards161.GrantCopy(state,granted,overall+"_REPEAT");
            Assert.That(TitanHeroRewards161.RepeatGrantedHero161(state,receipt),Is.Null,"A partial candidate is not a claimed reward.");
            state=state.WithSssV4090(state.SssV4090.With(specialUseReceiptIds:state.SssV4090.SpecialUseReceiptIds.Concat(new[]{overall})));
            Assert.That(TitanHeroRewards161.RepeatGrantedHero161(state,receipt),Is.EqualTo(granted));
            var reload=JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(state));
            Assert.That(TitanHeroRewards161.RepeatGrantedHero161(reload,receipt),Is.EqualTo(granted));
            Assert.That(SssTenV4Inventory090.AscensionCreditCount(reload,granted),Is.EqualTo(1));
        }

        [Test]
        public void SharedBudgetIsConservedAcrossTargetsBurstsChannelsAndHugeHistory161()
        {
            foreach(int amount in new[]{0,1,2,7,127,int.MaxValue})
            {
                var channels=M2BattleCommandService.AllocateTitanBudget161(amount,new long[]{5000,5000});
                Assert.That(channels.Sum(n=>(long)n),Is.EqualTo(amount));
                foreach(int channel in channels)
                {
                    var shares=M2BattleCommandService.AllocateTitanBudget161(channel,new long[]{1,4,0,7});
                    Assert.That(shares.Sum(n=>(long)n),Is.EqualTo(channel));Assert.That(shares[2],Is.Zero);
                    Assert.That(shares.SelectMany(n=>M2BattleCommandService.AllocateTitanBudget161(n,new long[]{1,1,1})).Sum(n=>(long)n),Is.EqualTo(channel));
                }
            }
            Assert.That(new FrozenTitanHeroAbility161("HERO_TITAN_001","GOLD_TITAN_001",2,1,1).Scale(101),Is.EqualTo(107));
            Assert.That(new FrozenTitanHeroAbility161("HERO_TITAN_013","ECHO_TITAN_001",1,2,2).Scale(101),Is.EqualTo(107));
            Assert.That(new FrozenTitanHeroAbility161("HERO_TITAN_013","ECHO_TITAN_001",1,long.MaxValue,int.MaxValue).Scale(int.MaxValue),Is.EqualTo(int.MaxValue));
        }

        [TestCase(1,false)][TestCase(2,false)][TestCase(3,false)][TestCase(4,false)]
        [TestCase(5,false)][TestCase(6,false)][TestCase(7,false)][TestCase(8,false)]
        [TestCase(9,false)][TestCase(10,false)][TestCase(11,false)][TestCase(12,false)]
        [TestCase(1,true)][TestCase(2,true)][TestCase(3,true)][TestCase(4,true)]
        [TestCase(5,true)][TestCase(6,true)][TestCase(7,true)][TestCase(8,true)]
        [TestCase(9,true)][TestCase(10,true)][TestCase(11,true)][TestCase(12,true)]
        public void EveryGoldAndEchoUsesNativeRoundAndOnePersonalCharge161(int slot,bool echo)
        {
            var content=Combat();var commands=new M2BattleCommandService();
            var state=Require(commands.StartTutorialBattle(Campaign(echo?13:slot),content));
            string heroId=TitanHeroCatalog161.HeroId(echo?13:slot),art=(echo?"ECHO_TITAN_":"GOLD_TITAN_")+slot.ToString("000");
            var recipe=TitanHeroCatalog161.Recipe(art);
            var players=state.Battle.PlayerUnions.Select(u=>u.With(members:u.Members.Select(m=>
                m.MemberId=="ALLY161"?m.With(currentHp:recipe.TargetScope=="ONE_FALLEN_ALLY"?0:1):m.With(currentHp:Math.Max(1,m.MaximumHp/2))).ToArray())).ToArray();
            var enemies=state.Battle.EnemyUnions.Select(u=>u.With(members:u.Members.Select(m=>WithLargeHp(m)).ToArray())).ToArray();
            var frozen=new TitanHeroBattleRuntime161(new[]{new FrozenTitanHeroAbility161(heroId,art,2,2,1)});
            var battle=state.Battle.With(playerUnions:players,enemyUnions:enemies,titanHeroes161:frozen);
            state=state.WithBattle(battle);
            battle=(BattleState)typeof(M2BattleCommandService).GetMethod("CommitForecasts",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{state,battle,content});
            state=state.WithBattle(battle);
            var gold=battle.CommittedForecasts.First(f=>f.CommandId=="TITAN161_"+art&&f.TargetId==(recipe.IsFriendly?players[0].UnionId:enemies[0].UnionId));
            Assert.That(gold.MemberActions[0].ActorMemberId,Is.EqualTo(heroId));
            Assert.That(gold.MemberActions.Count(a=>a.ActorMemberId==heroId),Is.EqualTo(1));
            Assert.That(gold.MemberActions[0].SharedApCost,Is.EqualTo(6));
            Assert.That(gold.MemberActions[0].PersonalMpCost,Is.EqualTo(10));
            var plan=JsonConvert.DeserializeObject<TitanHeroActionPlan161>(gold.DeterministicDebugEvidence);
            Assert.That(plan.Targets.Sum(t=>t.ChannelAmounts.Sum(n=>(long)n)),Is.LessThanOrEqualTo(plan.Budget));
            int beforeMp=battle.PlayerUnions[0].Members.Single(m=>m.MemberId==heroId).CurrentMp;
            state=Require(commands.SelectForecast(state,gold.UnionId,gold.ForecastId));
            string selected=CanonicalJson.Serialize(state);
            var resolved=Require(commands.ConfirmRound(state,content));
            Assert.That(resolved.Battle.TitanHeroes161.UsedHeroIds,Does.Contain(heroId));
            Assert.That(resolved.Battle.PlayerUnions[0].Members.Single(m=>m.MemberId==heroId).CurrentMp,Is.EqualTo(beforeMp-10));
            Assert.That(resolved.Battle.EventLog.Count(e=>e.EventType=="TITAN_GOLD_COMMITTED"&&e.ArtId==art),Is.EqualTo(1));
            Assert.That(resolved.Battle.EventLog.Count(e=>e.EventType=="FORECAST_COMMITTED"&&e.ArtId==gold.CommandId&&e.ActorUnionId==gold.UnionId),Is.EqualTo(1));
            foreach(var mapping in new[]{new[]{"TITAN_HEAL","RESTORATION"},new[]{"TITAN_REVIVE","REVIVED"},new[]{"TITAN_BARRIER","ALLY_PROTECTED"}})
            {
                var audit=resolved.Battle.EventLog.Where(e=>e.ArtId==art&&e.EventType==mapping[0]).ToArray();
                var impacts=resolved.Battle.EventLog.Where(e=>e.ArtId==art&&e.EventType==mapping[1]).ToArray();
                Assert.That(impacts.Length,Is.EqualTo(audit.Length));
                Assert.That(impacts.Select(e=>new{e.TargetUnionId,e.TargetMemberId,e.Amount}),Is.EqualTo(audit.Select(e=>new{e.TargetUnionId,e.TargetMemberId,e.Amount})),
                    "Each actual support result has exactly one canonical visual impact; the Titan audit cannot apply HP twice.");
            }
            Assert.That(resolved.Battle.CommittedForecasts.Any(f=>f.CommandId.StartsWith("TITAN161_")&&f.MemberActions[0].ActorMemberId==heroId),Is.False);
            Assert.That(resolved.Battle.EventLog.Any(e=>e.ArtId==art&&(e.EventType=="TITAN_BARRIER"||e.EventType=="TITAN_HEAL"||e.EventType=="TITAN_REVIVE"||e.EventType.EndsWith("_HIT"))),Is.True);
            var replay=Require(commands.ConfirmRound(JsonConvert.DeserializeObject<CampaignState>(selected),content));
            Assert.That(CanonicalJson.Serialize(replay),Is.EqualTo(CanonicalJson.Serialize(resolved)),"The persisted commitment replays exact costs, effects, uses and traces.");
            var reload=JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(resolved));
            Assert.That(reload.Battle.TitanHeroes161.UsedHeroIds,Does.Contain(heroId));
            Assert.That(reload.Battle.TitanHeroes161.Abilities.Single().Rank,Is.EqualTo(2));
        }

        [TestCase("GOLD_TITAN_008")][TestCase("ECHO_TITAN_008")]
        public void WardsAbsorbOnlyTheirScopeAndExpireWithoutRestoringAUse161(string artId)
        {
            var runtime=new TitanHeroBattleRuntime161(new[]{new FrozenTitanHeroAbility161("HERO_TITAN_008","GOLD_TITAN_008",1,1,1)},
                new[]{"HERO_TITAN_008"},new[]{new TitanHeroTimedEffect161("BARRIER","U","M","S",artId,50,1,2,TitanHeroCatalog161.Recipe(artId).BarrierScope)});
            var method=typeof(M2BattleCommandService).GetMethod("AdjustTitanHeroDamage161",BindingFlags.NonPublic|BindingFlags.Static);
            var events=new List<BattleEventState>();
            object[] args={1,BattleSide.Enemy,"E","U","M",BattleActionKind.Martial,40,events,runtime};
            Assert.That((int)method.Invoke(null,args),Is.EqualTo(40));
            args[5]=BattleActionKind.Mystic;Assert.That((int)method.Invoke(null,args),Is.Zero);
            runtime=(TitanHeroBattleRuntime161)args[8];Assert.That(runtime.Effects.Single().Amount,Is.EqualTo(10));
            args[6]=25;args[8]=runtime;Assert.That((int)method.Invoke(null,args),Is.EqualTo(15));
            runtime=(TitanHeroBattleRuntime161)args[8];Assert.That(runtime.Effects,Is.Empty);
            var end=typeof(M2BattleCommandService).GetMethod("EndTitanHeroRound161",BindingFlags.NonPublic|BindingFlags.Static);
            object[] finish={2,runtime};end.Invoke(null,finish);runtime=(TitanHeroBattleRuntime161)finish[1];
            Assert.That(runtime.UsedHeroIds,Does.Contain("HERO_TITAN_008"));
        }

        [Test]
        public void LostCommittedTargetsFizzleWithoutRetargetAndStillSpendExactlyOneCharge161()
        {
            var commands=new M2BattleCommandService();var state=Require(commands.StartTutorialBattle(Campaign(1),Combat()));
            var gold=state.Battle.CommittedForecasts.First(f=>f.CommandId=="TITAN161_GOLD_TITAN_001");
            var players=state.Battle.PlayerUnions.ToList();var enemies=new List<BattleUnionState>();
            var events=new List<BattleEventState>();var runtime=state.Battle.TitanHeroes161;
            int beforeAp=players[0].CurrentAp,beforeMp=players[0].Members[0].CurrentMp;
            var method=typeof(M2BattleCommandService).GetMethod("TryResolveTitanHeroForecast161",BindingFlags.Static|BindingFlags.NonPublic);
            object[] args={state,state.Battle,0,gold,players,enemies,events,runtime,null};
            Assert.That((bool)method.Invoke(null,args),Is.True,args[8]?.ToString());
            runtime=(TitanHeroBattleRuntime161)args[7];
            Assert.That(players[0].CurrentAp,Is.EqualTo(beforeAp-6));
            Assert.That(players[0].Members[0].CurrentMp,Is.EqualTo(beforeMp-10));
            Assert.That(runtime.UsedHeroIds,Is.EqualTo(new[]{"HERO_TITAN_001"}));
            Assert.That(events.Any(e=>e.EventType.EndsWith("_HIT")),Is.False);
            string spent=CanonicalJson.Serialize(players);
            Assert.That((bool)method.Invoke(null,args),Is.False);
            Assert.That(CanonicalJson.Serialize(players),Is.EqualTo(spent));
            Assert.That(state.Battle.PlayerUnions[0].CurrentAp,Is.EqualTo(beforeAp));
        }

        [Test]
        public void FrozenSnapshotAndSharedEidranUseSurviveCopiesAndReload161()
        {
            var state=Require(new M2BattleCommandService().StartTutorialBattle(Campaign(13),Combat()));
            Assert.That(state.Battle.TitanHeroes161,Is.Null,"An owned Eidran with no actual clears has no echo entitlement.");
            var runtime=new TitanHeroBattleRuntime161(new[]{
                new FrozenTitanHeroAbility161("HERO_TITAN_013","ECHO_TITAN_001",1,3,2),
                new FrozenTitanHeroAbility161("HERO_TITAN_013","ECHO_TITAN_002",1,5,3)},new[]{"HERO_TITAN_013"});
            var battle=state.Battle.With(titanHeroes161:runtime).With(round:2).WithReward(null);
            var reloaded=JsonConvert.DeserializeObject<BattleState>(CanonicalJson.Serialize(battle));
            Assert.That(CanonicalJson.Serialize(reloaded.TitanHeroes161),Is.EqualTo(CanonicalJson.Serialize(runtime)));
            var build=typeof(M2BattleCommandService).GetMethod("BuildTitanHeroForecasts161",BindingFlags.NonPublic|BindingFlags.Static);
            var offers=(IEnumerable<BattleForecastState>)build.Invoke(null,new object[]{state,reloaded,Combat(),reloaded.PlayerUnions[0]});
            Assert.That(offers,Is.Empty,"Changing Eidran's selected echo cannot restore his shared charge.");
            Assert.That(M2BattleCommandService.AuthoritativeStateHash(reloaded),Is.Not.EqualTo(M2BattleCommandService.AuthoritativeStateHash(state.Battle.With(round:2))));
            Assert.That(M1RuntimeCoordinator.TitanHeroProgressionSummary161(state,"HERO_TITAN_001"),Does.Contain("PERSONAL GOLD RANK 1"));
            Assert.That(M1RuntimeCoordinator.TitanHeroProgressionSummary161(state,"HERO_TITAN_013"),Does.Contain("0 / 12 ECHOES"));
            Assert.That(M1RuntimeCoordinator.TitanHeroProgressionSummary161(state,"HERO_TITAN_013"),Does.Contain("ONE SHARED USE"));
            Assert.That(M1RuntimeCoordinator.TitanHeroProgressionSummary161(state,"SSS_RYLEN_STONEBOND"),Is.Empty);
        }

        static CampaignState Campaign(int slot)
        {
            var hero=SssTenV4Roster090.MaterializeGrant(TitanHeroCatalog161.HeroId(slot)).Recruit;
            var ally=new RecruitState("ALLY161",200,200,30,30);var other=new RecruitState("ALLY162",200,200,30,30);
            var recruits=new[]{hero,ally,other};var union=new UnionState("UNION161","Titan Test",UnionKind.Normal,"ALLY161",
                recruits.Select(r=>r.RecruitId).ToArray(),"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",30,10000);
            var guild=new GuildState("GUILD161",100,recruits,new[]{union});
            var profile=new NewGuildProfileState("Hero Test Guildmaster",GameMode.Standard,TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),false);
            var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,false,"autosave_unions");
            return new CampaignState("PROFILE_HERO161",161,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);
        }
        static BattleMemberState WithLargeHp(BattleMemberState member)
        {
            var json=JObject.Parse(CanonicalJson.Serialize(member));json["MaximumHp"]=100000;json["CurrentHp"]=100000;json["Attack"]=1;json["MagicAttack"]=1;
            return json.ToObject<BattleMemberState>();
        }
        static CampaignState Require(Result<CampaignState> result)
        {Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));return result.Value;}
    }
}
