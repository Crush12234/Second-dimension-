using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanTrials160;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TitanNativeCombat161Tests
    {
        static readonly string Project=Environment.GetEnvironmentVariable("SD_TITAN161_PROJECT")??@"C:\SecondDimension\GuildOfWorlds";
        static readonly string Stage=Environment.GetEnvironmentVariable("SD_TITAN161_STAGE")??@"C:\Users\simon\Documents\ChatGPT\second dimension\AlphaR50_20260919\Stage161";
        static TitanTrialCatalog160 catalog;
        static M2CombatContent content;
        static readonly M2BattleCommandService battles=new M2BattleCommandService();
        static readonly List<BattleState> terminals=new List<BattleState>();
        static readonly List<BattleState> warningSaves=new List<BattleState>();
        static CampaignState completed;

        static void Load()
        {
            if(catalog!=null)return;
            catalog=TitanTrialCatalog160.LoadFromDirectory(Path.Combine(Stage,"mirror/Assets/StreamingAssets/SecondDimension/TitanTrials160"));
            content=M2CombatContent.LoadFromDirectory(Path.Combine(Project,"Assets/StreamingAssets/Authority/CONTENT")).WithTitanTrials161(catalog);
        }
        static CampaignState PreparedGuild()
        {
            Load();
            var state=JObject.Parse(File.ReadAllText(Path.Combine(Project,"Assets/Tests/Fixtures/Tower098/R100_OldCompletedFloor010.json")))["CampaignState"].ToObject<CampaignState>();
            // Explicit isolated Hall10 test precondition. The army/earned native
            // fixture is retained; no Titan clear, terminal or reward is injected.
            var g=state.Guild;var development=g.Development.SetFacilityLevel(TownProgression159.StateIds[0],10,0);
            state=state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,development),state.OpeningFlow);
            Assert.That(IndependentProgression159.BlockReason(state),Is.Null,"fixture must be between safe decisions");
            return state;
        }
        static CampaignState Start(CampaignState state,int slot,int tier=1)
        {
            var quote=TitanTrialCommands161.Quote(state,content,catalog,slot,tier);
            Assert.That(quote.IsSuccess,Is.True,string.Join(";",quote.Errors));
            var start=TitanTrialCommands161.Begin(state,battles,content,catalog,quote.Value);
            Assert.That(start.IsSuccess,Is.True,string.Join(";",start.Errors));
            return start.Value;
        }
        static CampaignState Round(CampaignState state,bool guard)
        {
            var round=state.Battle.Round;
            foreach(var union in state.Battle.PlayerUnions.Where(u=>!u.IsDefeated&&!u.Retreated))
            {
                var legal=state.Battle.CommittedForecasts.Where(f=>f.UnionId==union.UnionId&&f.SharedApCost<=union.CurrentAp).ToArray();
                var forecast=guard?legal.FirstOrDefault(f=>f.CommandId=="CMD_GUARD"):
                    legal.Where(f=>f.CommandId!="CMD_RETREAT").OrderByDescending(f=>f.MemberActions.Sum(a=>(long)Math.Max(0,-a.PredictedHpDelta))).FirstOrDefault();
                Assert.That(forecast,Is.Not.Null,"legal native Forecast round "+round);
                var selected=battles.SelectForecast(state,union.UnionId,forecast.ForecastId);
                Assert.That(selected.IsSuccess,Is.True,string.Join(";",selected.Errors));state=selected.Value;
            }
            var result=battles.ConfirmRound(state,content);
            Assert.That(result.IsSuccess,Is.True,"round "+round+": "+string.Join(";",result.Errors));
            return result.Value;
        }
        static CampaignState Reload(CampaignState state)=>JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(state));
        static string SectorIdentity(CampaignState state)
        {
            // Native combat grows shared People Bonds and its containing UI
            // checkpoint label. Campaign/Tower/World operation identities and
            // progress must otherwise remain byte-for-byte unchanged.
            var value=JObject.FromObject(state.Guild.GuildCity.Strategic017H.Campaign019);
            value.Remove("LastCheckpointId");
            var playable=value["Playable020"] as JObject;
            playable?.Remove("PeopleBonds026");playable?.Remove("LastCheckpointId");
            return CanonicalJson.Sha256Hex(value);
        }
        static void CompleteAllTwelve()
        {
            if(completed!=null)return;
            var state=PreparedGuild();
            var checkpoint=SectorIdentity(state);
            for(int slot=1;slot<=12;slot++)
            {
                state=Start(state,slot);
                var opening=state.Battle;var stats=catalog.Trial(slot).Stats(1);
                Assert.That(opening.EnemyUnions.Count,Is.EqualTo(1));
                Assert.That(opening.EnemyUnions[0].Members[0].MaximumHp,Is.EqualTo(stats.MaximumHp));
                Assert.That(opening.EnemyUnions[0].Members[0].Attack,Is.EqualTo(stats.Attack));
                Assert.That(opening.PlayerUnions.Count,Is.EqualTo(state.TitanTrials160.Active.AlliedUnionIds.Count));
                Assert.That(opening.CommittedForecasts.Select(f=>f.UnionId).Distinct().Count(),Is.EqualTo(opening.PlayerUnions.Count));
                int guardRounds=slot==5?12:slot==12?8:slot==3||slot==9?5:4;
                while(state.Battle.Outcome==BattleOutcome.InProgress&&state.Battle.Round<=guardRounds)
                {
                    if(state.Battle.TitanRuntime161.Action.Kind=="WIND_UP")
                    {
                        var before=CanonicalJson.Sha256Hex(state);state=Reload(state);
                        Assert.That(CanonicalJson.Sha256Hex(state),Is.EqualTo(before),"warning save round-trip");
                        warningSaves.Add(state.Battle);
                    }
                    // Wound Phoenix before its warning so the first accepted
                    // channel demonstrates actual positive native healing.
                    state=Round(state,slot!=5||state.Battle.Round!=1);
                }
                while(state.Battle.Outcome==BattleOutcome.InProgress&&state.Battle.Round<=300)state=Round(state,false);
                Assert.That(state.Battle.Outcome,Is.EqualTo(BattleOutcome.Victory),"actual native clear slot "+slot);
                Assert.That(M2BattleCommandService.HasValidFinalStateHash090(state.Battle),Is.True);
                Assert.That(state.Battle.RoundRecords.Count,Is.EqualTo(state.Battle.Round));
                terminals.Add(state.Battle);
                var claimed=battles.ClaimBattleRewards(state);Assert.That(claimed.IsSuccess,Is.True,string.Join(";",claimed.Errors));
                var returned=TitanTrialCommands161.ApplyClaimedReturn(claimed.Value,catalog);
                Assert.That(returned.IsSuccess,Is.True,string.Join(";",returned.Errors));state=returned.Value;
                var exactHash=CanonicalJson.Sha256Hex(state);
                var replay=TitanTrialCommands161.ApplyClaimedReturn(state,catalog);
                Assert.That(replay.IsSuccess,Is.True,string.Join(";",replay.Errors));
                Assert.That(CanonicalJson.Sha256Hex(replay.Value),Is.EqualTo(exactHash),"return retry is a no-op");
                state=Reload(state);
                Assert.That(state.TitanTrials160.Progress(slot).HighestTier,Is.EqualTo(1));
                Assert.That(state.Guild.Recruits.Any(r=>r.RecruitId==catalog.Trial(slot).HeroId),Is.True);
                Assert.That(state.TitanTrials160.Settlements.Count,Is.EqualTo(slot));
                Assert.That(SectorIdentity(state),Is.EqualTo(checkpoint),"other sector checkpoint retained");
                Console.WriteLine("TITAN161_NATIVE_CLEAR slot="+slot+" rounds="+terminals[slot-1].Round+" reward="+terminals[slot-1].Reward.GuildTreasuryXpAward);
            }
            completed=state;
        }

        [Test,Timeout(600000)]
        public void AllTwelveUseNativeForecastCombatRewardsAndReloadExactlyOnce()
        {CompleteAllTwelve();Assert.That(completed.TitanTrials160.Settlements.Count,Is.EqualTo(12));}

        [Test,Timeout(600000)]
        public void AllTwelveExecuteTheirOwnWarningImpactAndRecoveryRecipes()
        {
            CompleteAllTwelve();
            for(int slot=1;slot<=12;slot++)
            {
                var battle=terminals[slot-1];
                Assert.That(battle.EventLog.Any(e=>e.EventType=="TITAN_WIND_UP161"),Is.True,"slot "+slot);
                Assert.That(battle.EventLog.Any(e=>e.EventType=="TITAN_RECOVERY161"),Is.True,"slot "+slot);
                Assert.That(battle.EventLog.Any(e=>e.EventType=="ENEMY_SUPPORT_FORECAST"&&e.ActorUnionId==battle.EnemyUnions[0].UnionId),Is.True,"visible warning slot "+slot);
                Assert.That(battle.EventLog.Any(e=>e.EventType=="RECOVERY"&&e.ActorUnionId==battle.EnemyUnions[0].UnionId),Is.True,"visible recovery slot "+slot);
            }
            Assert.That(terminals[2].EventLog.Any(e=>e.ArtId=="TITAN_TRIAL_003_VENOM"&&e.EventType=="ENEMY_HIT"),Is.True);
            Assert.That(terminals[4].EventLog.Count(e=>e.EventType=="TITAN_HEAL161"),Is.EqualTo(2));
            Assert.That(terminals[4].EventLog.Any(e=>e.EventType=="TITAN_HEAL161"&&e.Amount>0),Is.True);
            foreach(var heal in terminals[4].EventLog.Where(e=>e.EventType=="TITAN_HEAL161"))
                Assert.That(terminals[4].EventLog.Count(e=>e.Round==heal.Round&&e.EventType=="RESTORATION"&&e.Amount==heal.Amount&&
                    e.ActorMemberId==terminals[4].EnemyUnions[0].LeaderMemberId&&e.TargetMemberId==e.ActorMemberId),Is.EqualTo(1),"one visible actual healing delta");
            Assert.That(terminals[4].EventLog.Any(e=>e.ArtId=="TITAN_TRIAL_005_DAMAGE_IMPACT"&&e.EventType=="ENEMY_HIT"),Is.True);
            Assert.That(terminals[8].EventLog.Any(e=>e.EventType=="TITAN_BARRIER161"),Is.True);
            Assert.That(terminals[8].EventLog.Any(e=>e.EventType=="GUARD"&&e.ActorUnionId==terminals[8].EnemyUnions[0].UnionId&&e.Amount>0),Is.True);
            Assert.That(terminals[8].EventLog.Any(e=>e.EventType=="TITAN_BARRIER_EXPIRED161"),Is.True);
            Assert.That(terminals[10].EventLog.Any(e=>e.EventType=="TITAN_SLOW161"),Is.True);
            Assert.That(warningSaves.Where(b=>b.TitanRuntime161.Attempt.Slot==12).Select(b=>b.TitanRuntime161.Action.Channel),Does.Contain("MYSTIC_DAMAGE"));
        }

        [Test,Timeout(600000)]
        public void NativeSavedWarningCannotChangeChannelAndTerminalHashDetectsMutation()
        {
            CompleteAllTwelve();
            var state=Start(completed,1,2);state=Round(state,true);
            Assert.That(state.Battle.TitanRuntime161.Action.Kind,Is.EqualTo("WIND_UP"));
            var json=JObject.FromObject(state);
            json["Battle"]["TitanRuntime161"]["Action"]["Channel"]="PHYSICAL_DAMAGE";
            var altered=json.ToObject<CampaignState>();
            foreach(var union in altered.Battle.PlayerUnions.Where(u=>!u.IsDefeated&&!u.Retreated))
            {
                var f=altered.Battle.CommittedForecasts.First(x=>x.UnionId==union.UnionId&&x.CommandId=="CMD_GUARD");
                altered=battles.SelectForecast(altered,union.UnionId,f.ForecastId).Value;
            }
            Assert.That(battles.ConfirmRound(altered,content).IsSuccess,Is.False);
            var terminal=terminals[0];var enemy=terminal.EnemyUnions[0];var changed=enemy.With(members:new[]{enemy.Members[0].With(currentHp:1)});
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(terminal.With(enemyUnions:new[]{changed})),Is.False);
        }

        [Test]
        public void NativeHealingInterruptAndBarrierConsumeActualResolvedDamage()
        {
            CompleteAllTwelve();
            var phoenix=Start(completed,5);phoenix=Round(phoenix,true);
            phoenix=Round(phoenix,false);phoenix=Round(phoenix,false);
            Assert.That(phoenix.Battle.EventLog.Any(e=>e.EventType=="TITAN_HEAL_INTERRUPTED161"),Is.True,
                "two real native attack windows must interrupt the channel");
            Assert.That(phoenix.Battle.EventLog.Any(e=>e.EventType=="TITAN_HEAL161"),Is.False);
            Assert.That(phoenix.Battle.EventLog.Any(e=>e.EventType=="RESTORATION"&&e.ActorUnionId==phoenix.Battle.EnemyUnions[0].UnionId),Is.False);
            Assert.That(phoenix.Battle.EventLog.Any(e=>e.EventType=="ENEMY_SUPPORT_FORECAST"&&e.Text.Contains("interrupted")),Is.True);
            var rootlord=Start(completed,9);
            for(int i=0;i<3;i++)rootlord=Round(rootlord,true);
            Assert.That(rootlord.Battle.TitanRuntime161.BarrierRemaining,Is.GreaterThan(0));
            rootlord=Round(rootlord,false);
            Assert.That(rootlord.Battle.EventLog.Any(e=>e.EventType=="TITAN_BARRIER_ABSORB161"&&e.Amount>0),Is.True);
            Assert.That(rootlord.Battle.EventLog.Where(e=>e.EventType=="ALLY_PROTECTED"&&e.ActorUnionId==rootlord.Battle.EnemyUnions[0].UnionId).Sum(e=>e.Amount),
                Is.EqualTo(rootlord.Battle.EventLog.Where(e=>e.EventType=="TITAN_BARRIER_ABSORB161").Sum(e=>e.Amount)),"barrier playback uses absorbed amount without duplicate HP damage");
            Assert.That(rootlord.Battle.TitanRuntime161.BarrierRemaining,Is.EqualTo(0));
            Assert.That(rootlord.Battle.TitanRuntime161.ExposedThroughRound,Is.GreaterThanOrEqualTo(rootlord.Battle.Round));
        }

        [Test]
        public void NativeFailedAttemptSettlesWithoutHeroGoldOrBonusAndAllowsRetry()
        {
            var state=PreparedGuild();var guild=state.Guild;
            state=state.With(guild.With(guild.TreasuryXp,guild.Recruits,
                new[]{guild.Unions.First(u=>u.Kind==UnionKind.Normal)},guild.Inventory),state.OpeningFlow);
            state=Start(state,1);
            var attempt=state.TitanTrials160.Active.AttemptId;
            for(int round=0;round<300&&state.Battle.Outcome==BattleOutcome.InProgress;round++)
            {
                foreach(var union in state.Battle.PlayerUnions.Where(u=>!u.IsDefeated&&!u.Retreated))
                {
                    var retreat=state.Battle.CommittedForecasts.FirstOrDefault(f=>f.UnionId==union.UnionId&&f.CommandId=="CMD_RETREAT")??
                        state.Battle.CommittedForecasts.Single(f=>f.UnionId==union.UnionId&&f.CommandId=="CMD_GUARD");
                    var selected=battles.SelectForecast(state,union.UnionId,retreat.ForecastId);
                    Assert.That(selected.IsSuccess,Is.True,string.Join(";",selected.Errors));state=selected.Value;
                }
                var resolved=battles.ConfirmRound(state,content);
                Assert.That(resolved.IsSuccess,Is.True,string.Join(";",resolved.Errors));state=resolved.Value;
            }
            Assert.That(state.Battle.Outcome==BattleOutcome.Retreat||state.Battle.Outcome==BattleOutcome.Defeat,Is.True);
            var outcome=state.Battle.Outcome;
            Console.WriteLine("TITAN161_NATIVE_FAILURE outcome="+outcome+" rounds="+state.Battle.Round+" deployedUnions="+state.Battle.PlayerUnions.Count);
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(state.Battle),Is.True);
            var claim=battles.ClaimBattleRewards(state);
            Assert.That(claim.IsSuccess,Is.True,string.Join(";",claim.Errors));state=claim.Value;
            var nativeRoster=CanonicalJson.Sha256Hex(state.Guild.Recruits);
            var nativeInventory=CanonicalJson.Sha256Hex(state.Guild.Inventory);
            var nativeTreasury=state.Guild.TreasuryXp;
            var returned=TitanTrialCommands161.ApplyClaimedReturn(state,catalog);
            Assert.That(returned.IsSuccess,Is.True,string.Join(";",returned.Errors));state=Reload(returned.Value);
            Assert.That(state.TitanTrials160.Active,Is.Null);Assert.That(state.Guild.GuildCity.PendingEncounter,Is.Null);
            Assert.That(state.TitanTrials160.Progress(1).HighestTier,Is.EqualTo(0));
            Assert.That(state.TitanTrials160.Progress(1).PersonalGoldRank,Is.EqualTo(0));
            Assert.That(state.TitanTrials160.Progress(1).Victories,Is.EqualTo(0));
            Assert.That(state.TitanTrials160.Settlements.Single().Outcome,Is.EqualTo(outcome));
            Assert.That(CanonicalJson.Sha256Hex(state.Guild.Recruits),Is.EqualTo(nativeRoster));
            Assert.That(CanonicalJson.Sha256Hex(state.Guild.Inventory),Is.EqualTo(nativeInventory));
            Assert.That(state.Guild.TreasuryXp,Is.EqualTo(nativeTreasury));
            Assert.That(state.Guild.Recruits.Any(r=>r.RecruitId==catalog.Trial(1).HeroId),Is.False);
            var replay=TitanTrialCommands161.ApplyClaimedReturn(state,catalog);
            Assert.That(replay.IsSuccess,Is.True,string.Join(";",replay.Errors));
            Assert.That(CanonicalJson.Sha256Hex(replay.Value),Is.EqualTo(CanonicalJson.Sha256Hex(state)));
            var retried=Start(state,1);
            Assert.That(retried.TitanTrials160.Active.AttemptId,Is.Not.EqualTo(attempt));
            Assert.That(retried.TitanTrials160.Active.Tier,Is.EqualTo(1));
        }

        [Test]
        public void AbsoluteTitanProfileDoesNotFollowDeployedArmyOrTreasury()
        {
            var full=PreparedGuild();var guild=full.Guild;
            var small=full.With(guild.With(checked(guild.TreasuryXp+100000),guild.Recruits,
                new[]{guild.Unions.First(u=>u.Kind==UnionKind.Normal)},guild.Inventory),full.OpeningFlow);
            var largeBattle=Start(full,1);var smallBattle=Start(small,1);
            Assert.That(largeBattle.Battle.PlayerUnions.Count,Is.GreaterThan(smallBattle.Battle.PlayerUnions.Count));
            var a=largeBattle.Battle.EnemyUnions[0].Members[0];var b=smallBattle.Battle.EnemyUnions[0].Members[0];
            Assert.That(new[]{a.MaximumHp,a.Attack,a.MagicAttack},Is.EqualTo(new[]{b.MaximumHp,b.Attack,b.MagicAttack}));
            Assert.That(largeBattle.Guild.TreasuryXp,Is.EqualTo(full.Guild.TreasuryXp));
            Assert.That(smallBattle.Guild.TreasuryXp,Is.EqualTo(small.Guild.TreasuryXp));
        }

        [Test]
        public void LegacyBattleHasNoTitanFieldsAndRetainsItsCertifiedHash()
        {
            Load();
            var original=JObject.Parse(File.ReadAllText(Path.Combine(Project,"Assets/Tests/Fixtures/Tower098/R100_OldCompletedFloor010.json")))["CampaignState"].ToObject<CampaignState>();
            Assert.That(original.Battle.TitanRuntime161,Is.Null);Assert.That(original.Battle.TitanHeroes161,Is.Null);
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(original.Battle),Is.True);
            var serialized=JObject.FromObject(original.Battle);
            Assert.That(serialized.ContainsKey("TitanRuntime161"),Is.False);Assert.That(serialized.ContainsKey("TitanHeroes161"),Is.False);
        }
    }
}
