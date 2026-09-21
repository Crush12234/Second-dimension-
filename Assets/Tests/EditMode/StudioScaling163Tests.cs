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
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class StudioScaling163Tests
    {
        // Fixtures are controlled cohorts driven by the pinned production161
        // DLL. They are not personal saves or claimed earned progression.
        static string Fixtures => Environment.GetEnvironmentVariable("SD_SCALING163_FIXTURES") ??
            Path.Combine(Application.dataPath,"Tests","Fixtures","Scaling163");
        static M2CombatContent content;
        static M2CombatContent Content => content ?? (content = M2CombatContent.LoadFromDirectory(
            Environment.GetEnvironmentVariable("SD_SCALING163_CONTENT") ?? Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT")));
        static readonly M2BattleCommandService Commands = new M2BattleCommandService();
        static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Static;
        static T Need<T>(Result<T> result) { Assert.That(result.IsSuccess,Is.True,string.Join(";",result.Errors)); return result.Value; }
        static CampaignState Load(string name) => JsonConvert.DeserializeObject<CampaignState>(File.ReadAllText(Path.Combine(Fixtures,name)));
        static CampaignState Reload(CampaignState c) => JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(c));
        static CampaignState Start(CampaignState c,IReadOnlyList<string> routes=null) => Need(Commands.StartEncounterBattle(
            c.WithBattle(null),Content,"AUDIT163_POLICY","Controlled native policy test",1,routes));
        static CampaignState Guard(CampaignState c)
        {
            foreach(var u in c.Battle.PlayerUnions.Where(u=>!u.IsDefeated&&!u.Retreated))
                c=Need(Commands.SelectForecast(c,u.UnionId,c.Battle.CommittedForecasts.First(f=>f.UnionId==u.UnionId&&f.CommandId=="CMD_GUARD").ForecastId));
            return Need(Commands.ConfirmRound(c,Content));
        }
        static CampaignState Victory(CampaignState c)
        {
            for(int i=0;i<30&&c.Battle.Outcome==BattleOutcome.InProgress;i++)
            {
                foreach(var u in c.Battle.PlayerUnions.Where(u=>!u.IsDefeated&&!u.Retreated))
                {
                    var f=c.Battle.CommittedForecasts.Where(f=>f.UnionId==u.UnionId)
                        .OrderByDescending(f=>f.MemberActions.Sum(a=>Math.Max(0,-a.PredictedHpDelta))).First();
                    c=Need(Commands.SelectForecast(c,u.UnionId,f.ForecastId));
                }
                c=Need(Commands.ConfirmRound(c,Content));
            }
            Assert.That(c.Battle.Outcome,Is.EqualTo(BattleOutcome.Victory));return c;
        }
        [Test]
        public void LegacyActiveBattleAndPendingClaimKeepExactNativeBytes163()
        {
            var c=Load("legacy_initial.json");Assert.That(c.Battle.Progression163,Is.Null);
            Assert.That(CanonicalJson.Serialize(c.Battle).Contains("Progression163"),Is.False);
            for(int i=0;i<3;i++)c=Guard(c);
            Assert.That(CanonicalJson.Serialize(c),Is.EqualTo(File.ReadAllText(Path.Combine(Fixtures,"legacy_after3.json"))));
            var pending=Load("legacy_pending.json");Assert.That(M2BattleCommandService.HasValidFinalStateHash090(pending.Battle),Is.True);
            Assert.That(CanonicalJson.Serialize(Need(Commands.ClaimBattleRewards(pending))),
                Is.EqualTo(File.ReadAllText(Path.Combine(Fixtures,"legacy_claimed.json"))));
        }
        [Test]
        public void FrozenDefenseChangesRealNativePhysicalHitsAndReloadsExactly163()
        {
            var source=Load("legacy_initial.json");var json=JObject.FromObject(source.WithBattle(null));
            foreach(var r in json["Guild"]["Recruits"])r["Progression"]["DefenseBonus"]=(int)r["Progression"]["DefenseBonus"]+1000;
            var low=Start(source);var high=Start(json.ToObject<CampaignState>());var reloaded=Reload(high);
            Assert.That(high.Battle.Progression163.Members.All(m=>m.Defense==1048),Is.True);
            for(int i=0;i<3;i++){low=Guard(low);high=Guard(high);reloaded=Guard(reloaded);}
            Assert.That(CanonicalJson.Serialize(high),Is.EqualTo(CanonicalJson.Serialize(reloaded)));
            Func<CampaignState,int> damage=c=>c.Battle.EventLog.Where(e=>e.EventType=="ENEMY_HIT"||e.EventType=="INTERCEPTION").Sum(e=>e.Amount);
            Assert.That(damage(high),Is.GreaterThan(0).And.LessThan(damage(low)));
        }
        [TestCase(1)] [TestCase(100)] [TestCase(int.MaxValue)]
        public void PhysicalCapDoesNotTouchMysticHealingOrSavedVenom163(int damage)
        {
            var c=Start(Load("legacy_initial.json"));var member=c.Battle.PlayerUnions[0].Members[0].MemberId;
            var p=new M2BattlePolicy163(M2BattlePolicy163.Version,new[]{new BattleDefenseMember163(member,int.MaxValue)},1,1);
            Assert.That(p.PhysicalDamage(member,BattleActionKind.Martial,damage),Is.EqualTo((2L*damage+4)/5));
            Assert.That(p.PhysicalDamage(member,BattleActionKind.Tactical,damage),Is.EqualTo((2L*damage+4)/5));
            Assert.That(p.PhysicalDamage(member,BattleActionKind.Mystic,damage),Is.EqualTo(damage));
            Assert.That(p.PhysicalDamage(member,BattleActionKind.Restoration,damage),Is.EqualTo(damage));
            var events=new List<BattleEventState>();
            typeof(M2BattleCommandService).GetMethod("BeginProgressionRound163",Private).Invoke(null,new object[]{c.Battle,events});
            var actual=(int)typeof(M2BattleCommandService).GetMethod("AdjustNativeDamage161",Private).Invoke(null,new object[]{1,
                BattleSide.Enemy,"enemy",c.Battle.PlayerUnions[0].UnionId,member,BattleActionKind.Martial,damage,
                new List<BattleUnionState>(c.Battle.PlayerUnions),events,"TITAN_TRIAL_003_VENOM"});
            Assert.That(actual,Is.EqualTo(damage));
        }
        [Test]
        public void ActualReplayVictoryFreezesExactRatioAndClaimsOnlyOnce163()
        {
            var source=Load("legacy_pending.json");var first=Victory(Start(source));
            var cycle=Start(source,CampaignReplayThreat130.AppendToRoutes(null,5,25));var saved=Reload(cycle);var p=cycle.Battle.Progression163;
            cycle=Victory(cycle);saved=Victory(saved);Assert.That(CanonicalJson.Serialize(cycle),Is.EqualTo(CanonicalJson.Serialize(saved)));
            Assert.That(cycle.Battle.Reward.GuildTreasuryXpAward,Is.EqualTo(p.ScaleReplayReward(first.Battle.Reward.GuildTreasuryXpAward)));
            Assert.That(cycle.Battle.Reward.MemberRewards[0].PersonalXp,Is.EqualTo(p.ScaleReplayReward(first.Battle.Reward.MemberRewards[0].PersonalXp)));
            Assert.That(cycle.Battle.Reward.HallEnhancementXpAward,Is.EqualTo(p.ScaleReplayReward(first.Battle.Reward.HallEnhancementXpAward)));
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(cycle.Battle),Is.True);
            var claimed=Need(Commands.ClaimBattleRewards(Reload(cycle)));
            Assert.That(claimed.Guild.TreasuryXp-cycle.Guild.TreasuryXp,Is.EqualTo(cycle.Battle.Reward.GuildTreasuryXpAward));
            Assert.That(CanonicalJson.Serialize(Need(Commands.ClaimBattleRewards(claimed))),Is.EqualTo(CanonicalJson.Serialize(claimed)));
        }
        [Test]
        public void ExactReplayArithmeticAndNativeTutorialReplayKeepBounds163()
        {
            Assert.That(new M2BattlePolicy163(M2BattlePolicy163.Version,null,1,1).ScaleReplayReward(long.MaxValue),Is.EqualTo(long.MaxValue));
            Assert.Throws<OverflowException>(()=>new M2BattlePolicy163(M2BattlePolicy163.Version,null,2,1).ScaleReplayReward(long.MaxValue));
            var source=Load("legacy_initial.json").WithBattle(null);
            var c=Guard(Need(Commands.StartTutorialBattle(source,Content)));
            Assert.That(Commands.ReplayTutorialBattle(c,Content).IsSuccess,Is.True);
        }
    }
}
