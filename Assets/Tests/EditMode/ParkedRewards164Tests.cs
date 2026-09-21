using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Navigation164;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ParkedRewards164Tests
    {
        // The portable fixtures are controlled cohorts already driven to their
        // terminal result by production161 native combat, never outcome edits.
        static string Fixtures => Environment.GetEnvironmentVariable("SD_SCALING163_FIXTURES") ??
            Path.Combine(Application.dataPath,"Tests","Fixtures","Scaling163");
        static M2CombatContent content;
        static M2CombatContent Content => content ?? (content=M2CombatContent.LoadFromDirectory(
            Environment.GetEnvironmentVariable("SD_SCALING163_CONTENT") ?? Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT")));
        static readonly M2BattleCommandService Commands=new M2BattleCommandService();
        static T Need<T>(Result<T> result) {Assert.That(result.IsSuccess,Is.True,string.Join(";",result.Errors));return result.Value;}
        static CampaignState Load(string name)=>JsonConvert.DeserializeObject<CampaignState>(File.ReadAllText(Path.Combine(Fixtures,name)));
        static CampaignState Reload(CampaignState c)=>JsonConvert.DeserializeObject<CampaignState>(CanonicalJson.Serialize(c));
        static CampaignState Switch(CampaignState c,string loop)=>Need(LoopCheckpoint164.Switch(c,loop));
        static RecruitProgressionState Progress(CampaignState c,string id)=>c.Guild.Recruits.First(x=>x.RecruitId==id).Progression;
        static CampaignState Victory(CampaignState c)
        {
            for(int i=0;i<30&&c.Battle.Outcome==BattleOutcome.InProgress;i++)
            {
                foreach(var u in c.Battle.PlayerUnions.Where(x=>!x.IsDefeated&&!x.Retreated))
                {
                    var f=c.Battle.CommittedForecasts.Where(x=>x.UnionId==u.UnionId)
                        .OrderByDescending(x=>x.MemberActions.Sum(a=>Math.Max(0,-a.PredictedHpDelta))).First();
                    c=Need(Commands.SelectForecast(c,u.UnionId,f.ForecastId));
                }
                c=Need(Commands.ConfirmRound(c,Content));
            }
            Assert.That(c.Battle.Outcome,Is.EqualTo(BattleOutcome.Victory));
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(c.Battle),Is.True);return c;
        }
        static CampaignState TrainParticipant(CampaignState c,string[] ids,out string id)
        {
            var quotes=ids.Select(x=>HeroPaidLevels152.Quote(c,x,1)).Where(x=>x.IsSuccess&&x.Value.Affordable).ToArray();
            Assert.That(quotes.Length,Is.GreaterThan(0));var q=quotes[0].Value;id=q.RecruitId;
            var wallet=c.Guild.TreasuryXp;c=Need(HeroPaidLevels152.Confirm(c,q));
            Assert.That(c.Guild.TreasuryXp,Is.EqualTo(wallet-q.Cost));return c;
        }
        [Test]
        public void TerminalParkPaidTrainingClaimsExactXpAndRetryIsIdempotent164()
        {
            var c=Load("legacy_pending.json");var battle=CanonicalJson.Serialize(c.Battle);
            var ids=c.Battle.Reward.MemberRewards.Select(x=>x.MemberId).ToArray();
            c=Switch(c,LoopCheckpoint164.Town);c=TrainParticipant(c,ids,out var id);
            c=Switch(Reload(c),LoopCheckpoint164.Campaign);
            Assert.That(CanonicalJson.Serialize(c.Battle),Is.EqualTo(battle));
            var reward=c.Battle.Reward.MemberRewards.First(x=>x.MemberId==id);var before=Progress(c,id);
            var member=c.Battle.PlayerUnions.SelectMany(x=>x.Members).First(x=>x.MemberId==id);
            var expected=before.GainPersonalXp(reward.PersonalXp,member.ClassId);
            c=Need(Commands.ClaimBattleRewards(c));var actual=Progress(c,id);
            Assert.That(actual.TotalPersonalXp,Is.EqualTo(checked(before.TotalPersonalXp+reward.PersonalXp)));
            Assert.That(actual.Level,Is.EqualTo(expected.Level));Assert.That(actual.MaximumHpBonus,Is.EqualTo(expected.MaximumHpBonus));
            var claimed=CanonicalJson.Serialize(c);c=Reload(c);
            Assert.That(CanonicalJson.Serialize(Need(Commands.ClaimBattleRewards(c))),Is.EqualTo(claimed));
        }
        [Test]
        public void AnotherNativeBattleAddsOnlyEachBattlesMasteryDelta164()
        {
            var first=Load("legacy_pending.json");var pending=first.Battle;
            var c=Switch(first,LoopCheckpoint164.Tower);
            c=Need(Commands.StartEncounterBattle(c,Content,"TOWER_AUDIT164_SECOND","Controlled native interleaving",1));
            c=Victory(c);c=Need(Commands.ClaimBattleRewards(c));
            var otherProgress=c.Guild.Recruits.ToDictionary(x=>x.RecruitId,x=>x.Progression);
            c=Switch(Reload(c),LoopCheckpoint164.Campaign);
            Assert.That(CanonicalJson.Serialize(c.Battle),Is.EqualTo(CanonicalJson.Serialize(pending)));
            c=Need(Commands.ClaimBattleRewards(c));int nonzero=0;
            foreach(var reward in pending.Reward.MemberRewards)
            {
                var original=Progress(first,reward.MemberId);var other=otherProgress[reward.MemberId];var final=Progress(c,reward.MemberId);
                Assert.That(final.TotalPersonalXp,Is.EqualTo(checked(other.TotalPersonalXp+reward.PersonalXp)));
                var member=pending.PlayerUnions.SelectMany(x=>x.Members).First(x=>x.MemberId==reward.MemberId);
                foreach(var art in member.ArtProgress)
                {
                    var baseline=original.ArtMastery.FirstOrDefault(x=>x.ArtId==art.ArtId);
                    var shared=other.ArtMastery.FirstOrDefault(x=>x.ArtId==art.ArtId);
                    var result=final.ArtMastery.First(x=>x.ArtId==art.ArtId);
                    var delta=art.MasteryPoints-(baseline?.MasteryPoints??0);if(delta>0&&shared?.MasteryPoints>0)nonzero++;
                    Assert.That(result.MasteryPoints,Is.EqualTo((shared?.MasteryPoints??0)+delta));
                    Assert.That(result.MeaningfulUses,Is.EqualTo((shared?.MeaningfulUses??0)+art.MeaningfulUses-(baseline?.MeaningfulUses??0)));
                }
                Assert.That(other.LearnedArtIds.All(x=>final.LearnedArtIds.Contains(x)),Is.True);
            }
            Assert.That(nonzero,Is.GreaterThan(0),"Both real battles must earn mastery for the same art.");
            Assert.That(CanonicalJson.Serialize(Need(Commands.ClaimBattleRewards(Reload(c)))),Is.EqualTo(CanonicalJson.Serialize(c)));
        }
        [Test]
        public void ActiveParkTrainingThenNativeFinalizationUsesFreshProjection164()
        {
            var c=Need(Commands.StartEncounterBattle(Load("legacy_initial.json").WithBattle(null),Content,
                "AUDIT164_ACTIVE","Controlled active checkpoint",1));
            var frozen=CanonicalJson.Serialize(c.Battle);var ids=c.Battle.PlayerUnions.SelectMany(x=>x.Members).Select(x=>x.MemberId).ToArray();
            c=Switch(c,LoopCheckpoint164.Town);c=TrainParticipant(c,ids,out var id);
            c=Switch(Reload(c),LoopCheckpoint164.Campaign);Assert.That(CanonicalJson.Serialize(c.Battle),Is.EqualTo(frozen));
            c=Victory(c);var reward=c.Battle.Reward.MemberRewards.First(x=>x.MemberId==id);var previous=Progress(c,id);
            Assert.That(c.Loops164.Find(LoopCheckpoint164.Campaign).RewardMembers,Is.Null);
            c=Need(Commands.ClaimBattleRewards(c));Assert.That(Progress(c,id).TotalPersonalXp,Is.EqualTo(previous.TotalPersonalXp+reward.PersonalXp));
        }
        [Test]
        public void LegacyNullCheckpointClaimKeepsExactNativeBytes164()
        {
            var c=Load("legacy_pending.json");Assert.That(c.Loops164,Is.Null);
            Assert.That(CanonicalJson.Serialize(Need(Commands.ClaimBattleRewards(c))),
                Is.EqualTo(File.ReadAllText(Path.Combine(Fixtures,"legacy_claimed.json"))));
        }
        [Test]
        public void SettledCheckpointDoesNotBlockFreshNativeBattleReusingId164()
        {
            var c=Switch(Switch(Load("legacy_pending.json"),LoopCheckpoint164.Town),LoopCheckpoint164.Campaign);
            var old=c.Battle.Reward.RewardId;var id=c.Battle.BattleId;
            c=Need(Commands.ClaimBattleRewards(c));
            c=Victory(Need(Commands.StartEncounterBattle(c.WithBattle(null),Content,id,"Controlled repeated battle identity",1)));
            Assert.That(c.Battle.Reward.RewardId,Is.Not.EqualTo(old));
            Assert.That(c.Loops164.Find(LoopCheckpoint164.Campaign).Battle.Reward.RewardId,Is.EqualTo(old));
            var before=c.Guild.TreasuryXp;var award=c.Battle.Reward.GuildTreasuryXpAward;
            c=Need(Commands.ClaimBattleRewards(c));Assert.That(c.Guild.TreasuryXp,Is.EqualTo(before+award));
        }
        [Test]
        public void ParkedAuthorityTamperingFailsDeserialization164()
        {
            var c=Switch(Load("legacy_pending.json"),LoopCheckpoint164.Town);
            var json=JObject.Parse(CanonicalJson.Serialize(c));
            var baseline=json["Loops164"]["Checkpoints"][0]["RewardMembers"][0]["Progression"];
            baseline["TotalPersonalXp"]=(long)baseline["TotalPersonalXp"]+1;
            var error=Assert.Catch<Exception>(()=>json.ToObject<CampaignState>());
            Assert.That(error.ToString(),Does.Contain("Loop checkpoint integrity mismatch"));
        }
        [Test]
        public void AdditiveMasteryOverflowRejectsAtomically164()
        {
            var c=Switch(Load("legacy_pending.json"),LoopCheckpoint164.Town);
            var pending=c.Loops164.Find(LoopCheckpoint164.Campaign).Battle;
            var member=pending.PlayerUnions.SelectMany(x=>x.Members).First(x=>x.ArtProgress.Any(a=>a.MasteryPoints>0));
            var art=member.ArtProgress.First(x=>x.MasteryPoints>0);
            // Controlled shared-progression boundary, with the native terminal
            // battle, reward and checkpoint authority left unchanged.
            var json=JObject.Parse(CanonicalJson.Serialize(c));
            var recruit=json["Guild"]["Recruits"].First(x=>(string)x["RecruitId"]==member.MemberId);
            recruit["Progression"]["ArtMastery"]=JArray.FromObject(new[]{new RecruitArtMasteryState(art.ArtId,art.Discipline,1,int.MaxValue)});
            c=Switch(json.ToObject<CampaignState>(),LoopCheckpoint164.Campaign);var before=CanonicalJson.Serialize(c);
            Assert.That(Commands.ClaimBattleRewards(c).IsSuccess,Is.False);
            Assert.That(CanonicalJson.Serialize(c),Is.EqualTo(before));
            Assert.That(c.Guild.Development.HasClaimedReward(c.Battle.Reward.RewardId),Is.False);
        }
    }
}
