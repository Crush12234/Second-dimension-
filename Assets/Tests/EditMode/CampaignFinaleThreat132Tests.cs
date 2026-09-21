using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignFinaleThreat132Tests
    {
        [Test]
        public void EarliestReplayForceExceedsPriorFinaleTotalsEvenWithSmallerAuthoredMembers132()
        {
            // Numerical regression: prior finale had10 Unions of6 members, each
            //1000HP/100Attack/50Magic. New early content has only2 members/Union.
            var floor=new CampaignFinaleProof132("ACTUAL_FINALE_ID","ACTUAL_REWARD_ID",new string('a',64),75000,7500,3750,10);
            var tag=CampaignReplayThreat130.FormatFinaleTag132(2,25,floor);
            var profile=CampaignReplayThreat130.ParseCommittedRoutes(new[]{tag});
            Assert.That(CampaignReplayThreat130.MinimumUnionCount132(1,new[]{tag}),Is.EqualTo(10));
            var force=EnemyForceProfile094.ForChapter094(CampaignReplayThreat130.ForceChapter132(1,new[]{tag}),1);
            Assert.That(force.UnionCount094,Is.EqualTo(10));Assert.That(force.MemberCount094,Is.EqualTo(6));
            var input=Enumerable.Range(0,10).Select(Union).ToArray();var before=CanonicalJson.Serialize(input);
            var next=CampaignReplayThreat130.ApplyToEnemyUnions132(input,profile);
            var members=next.SelectMany(union=>union.Members).ToArray();
            Assert.That(members.Sum(member=>(long)member.MaximumHp),Is.GreaterThanOrEqualTo(75000));
            Assert.That(members.Sum(member=>(long)member.Attack),Is.GreaterThanOrEqualTo(7500));
            Assert.That(members.Sum(member=>(long)member.MagicAttack),Is.GreaterThanOrEqualTo(3750));
            Assert.That(next.Count,Is.EqualTo(10));
            Assert.That(CanonicalJson.Serialize(input),Is.EqualTo(before));
            for(var i=0;i<input.Length;i++)
            {
                var original=JObject.FromObject(input[i]);var scaled=JObject.FromObject(next[i]);
                foreach(var token in new[]{original,scaled})foreach(var member in token["Members"])
                    foreach(var field in new[]{"CurrentHp","MaximumHp","Attack","MagicAttack"})((JObject)member).Remove(field);
                Assert.That(JToken.DeepEquals(original,scaled),Is.True,"No identity/MP/AP/Art/formation/reward rewrite.");
            }
            Assert.Throws<InvalidOperationException>(()=>CampaignReplayThreat130.ApplyToEnemyUnions132(input.Take(1).ToArray(),profile));
            var laterTag=CampaignReplayThreat130.FormatFinaleTag132(3,25,new CampaignFinaleProof132(
                "LATER_FINALE","LATER_REWARD",new string('b',64),93750,9375,4700,10));
            var later=CampaignReplayThreat130.ApplyToEnemyUnions132(input,CampaignReplayThreat130.ParseCommittedRoutes(new[]{laterTag}));
            Assert.That(later.SelectMany(union=>union.Members).Sum(member=>(long)member.MaximumHp),Is.GreaterThan(members.Sum(member=>(long)member.MaximumHp)));
            Assert.That(CampaignReplayThreat130.AppendProfile132(new[]{"OTHER"},profile),Is.EqualTo(new[]{"OTHER",tag}));
            Assert.That(CanonicalJson.Serialize(CampaignReplayThreat130.ApplyToEnemyUnions132(input,profile)),Is.EqualTo(CanonicalJson.Serialize(next)));
        }

        [Test]
        public void OldCycleBoundaryCanonicalNullAndV1PolicyRemainExact132()
        {
            var boundary=new CampaignCycleBoundary130(2,82,82,new string('a',64),"OLD_RECEIPT");
            var json=CanonicalJson.Serialize(boundary);Assert.That(json,Does.Not.Contain("Finale132"));
            Assert.That(CanonicalJson.Serialize(new CampaignCycleBoundary130(2,82,82,new string('a',64),"OLD_RECEIPT",null)),Is.EqualTo(json));
            var source=new[]{Union(0)};
            Assert.That(CampaignReplayThreat130.ApplyToEnemyUnions132(source,null),Is.SameAs(source));
            var profile=CampaignReplayThreat130.ParseCommittedRoutes(new[]{"CAMPAIGN_REPLAY130_V1_C2_P25"});
            Assert.That(CanonicalJson.Serialize(CampaignReplayThreat130.ApplyToEnemyUnions132(source,profile)),
                Is.EqualTo(CanonicalJson.Serialize(new[]{CampaignReplayThreat130.ApplyToEnemyUnion(source[0],profile)})));
        }

        [TestCase("CAMPAIGN_REPLAY130_V2_C2_P25_H0_A1_M1_U1")]
        [TestCase("CAMPAIGN_REPLAY130_V2_C2_P25_H60000001_A1_M1_U1")]
        [TestCase("CAMPAIGN_REPLAY130_V2_C2_P25_H1_A600001_M1_U1")]
        [TestCase("CAMPAIGN_REPLAY130_V2_C2_P25_H1_A1_M1_U11")]
        public void InvalidFrozenAggregateFloorCannotBeUsed132(string invalid)
        {Assert.Throws<InvalidOperationException>(()=>CampaignReplayThreat130.ParseCommittedRoutes(new[]{invalid}));}

        static BattleUnionState Union(int index)
        {
            var members=Enumerable.Range(0,2).Select(n=>new BattleMemberState("MEMBER_"+index+"_"+n,
                "Authored enemy","CLASS_EXISTING",40,40,5,10,4,3,new[]{"SWORD"},false,false,false,
                new[]{"ART_EXISTING"},0,0,string.Empty)).ToArray();
            return new BattleUnionState("UNION_"+index,"Authored Union",BattleSide.Enemy,members[0].MemberId,
                members,"FORMATION_LINE","Line",false,string.Empty,20,20,80,8000,EngagementState.Flanking,false,false,0);
        }
    }
}
