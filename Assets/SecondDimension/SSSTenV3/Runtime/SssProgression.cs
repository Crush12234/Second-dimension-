using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SecondDimension.SSS.V3
{
    [Serializable] public sealed class TotalDefeatRow
    {
        public string heroId,totalDefeats="0";
        public TotalDefeatRow Copy() { return new TotalDefeatRow {heroId=heroId,totalDefeats=totalDefeats}; }
    }
    [Serializable] public sealed class WorldProgress
    {
        public string highestClearedTowerFloor="0",campaignBattleWins="0",uniqueCampaignStageCompletions="0";
        public List<string> completedStageKeys=new List<string>();
        public WorldProgress Copy() { return new WorldProgress {highestClearedTowerFloor=highestClearedTowerFloor,campaignBattleWins=campaignBattleWins,
            uniqueCampaignStageCompletions=uniqueCampaignStageCompletions,completedStageKeys=new List<string>(completedStageKeys)}; }
    }
    [Serializable] public sealed class SssSave
    {
        public int schemaVersion=3;
        public string revision="0";
        public CovenantProgressSave familyProgress=new CovenantProgressSave();
        public List<TotalDefeatRow> totalDefeats=new List<TotalDefeatRow>();
        public WorldProgress world=new WorldProgress();
        // Reference persistence shape. At scale, attach these keys to the existing indexed
        // reward journal. Do not maintain a second gameplay/reward authority.
        public List<string> killReceipts=new List<string>(),victoryReceipts=new List<string>(),codeReceipts=new List<string>();
        public List<SssRewardOffer> rewardOffers=new List<SssRewardOffer>();
        public SssSave Copy() { return new SssSave {schemaVersion=schemaVersion,revision=revision,familyProgress=familyProgress.Copy(),
            totalDefeats=totalDefeats.Select(x=>x.Copy()).ToList(),world=world.Copy(),killReceipts=new List<string>(killReceipts),
            victoryReceipts=new List<string>(victoryReceipts),codeReceipts=new List<string>(codeReceipts),rewardOffers=rewardOffers.Select(x=>x.Copy()).ToList()}; }
    }
    [Serializable] public sealed class VictoryFact
    {
        public string encounterInstanceId,mode,towerFloor="0",stageCompletionKey;
        public bool won,rewardEligible,completedCampaignStage;
    }
    [Serializable] public sealed class SssGrowthView
    {
        public string counter,rank,nextMilestone,remaining,powerNumerator;
        public int powerDenominator=100;
        public bool unlocked=true;
    }
    public sealed class SssReduction
    {
        public SssSave next;
        public bool duplicate;
        public List<GrowthNotice> notices=new List<GrowthNotice>();
    }
    public static class SssGrowth
    {
        public static SssGrowthView Total(string total,int percent=2)
        {
            if(percent<=0)throw new ArgumentOutOfRangeException("percent");
            BigInteger n=ExactNumbers.Read(total),rank=n/10;
            return new SssGrowthView {counter=ExactNumbers.Write(n),rank=ExactNumbers.Write(rank),nextMilestone=ExactNumbers.Write((rank+1)*10),
                remaining=ExactNumbers.Write(10-n%10),powerNumerator=ExactNumbers.Write(100+rank*percent)};
        }
        public static SssGrowthView World(WorldProgress world,int percent=1)
        {
            if(world==null || percent<=0)throw new ArgumentException("Invalid world progression.");
            BigInteger t=ExactNumbers.Read(world.highestClearedTowerFloor),c=ExactNumbers.Read(world.uniqueCampaignStageCompletions),rank=t/10+c/10;
            return new SssGrowthView {counter=ExactNumbers.Write(t+c),rank=ExactNumbers.Write(rank),nextMilestone="Tower or campaign next multiple of 10",
                remaining="T:"+ExactNumbers.Write(10-t%10)+";C:"+ExactNumbers.Write(10-c%10),powerNumerator=ExactNumbers.Write(100+rank*percent)};
        }
        public static string TotalCount(SssSave s,string hero)
        {
            string id=SssHeroes.CanonicalId(hero);var row=s.totalDefeats.SingleOrDefault(x=>x.heroId==id);return row==null?"0":row.totalDefeats;
        }
        public static SssGrowthView ForHero(SssSave s,string hero,string familyId=null)
        {
            string id=SssHeroes.CanonicalId(hero);var kind=SssHeroes.Scaling(id);
            if(kind==SssScaling.WorldProgress)return World(s.world);
            if(kind==SssScaling.TotalDefeats)return Total(TotalCount(s,id));
            ExactNumbers.RequireId(familyId);
            var v=CovenantMastery.View(CovenantMastery.Count(s.familyProgress,id,familyId));
            return new SssGrowthView {counter=v.totalDefeats,rank=v.rank,nextMilestone=v.nextMilestone,remaining=v.defeatsRemaining,powerNumerator=v.powerNumerator,unlocked=v.unlocked};
        }
        public static BigInteger ScaleRating(BigInteger baseRating,SssGrowthView growth)
        {
            if(baseRating.Sign<0 || growth==null || !growth.unlocked)throw new ArgumentException("Invalid unlocked power.");
            return (baseRating*ExactNumbers.Read(growth.powerNumerator)+99)/100;
        }
    }
    public static class SssProgression
    {
        static void CheckUnique(List<string> list)
        { if(list==null || list.Any(String.IsNullOrWhiteSpace) || list.Count!=list.Distinct(StringComparer.Ordinal).Count())throw new ArgumentException("Invalid receipt list."); }
        public static void Validate(SssSave s)
        {
            if(s==null || s.schemaVersion!=3 || s.familyProgress==null || s.totalDefeats==null || s.world==null || s.rewardOffers==null)
                throw new ArgumentException("Expected V3 save; migrate old data explicitly.");
            ExactNumbers.Read(s.revision);CovenantProgression.Validate(s.familyProgress);
            CheckUnique(s.killReceipts);CheckUnique(s.victoryReceipts);CheckUnique(s.codeReceipts);CheckUnique(s.world.completedStageKeys);
            ExactNumbers.Read(s.world.highestClearedTowerFloor);ExactNumbers.Read(s.world.campaignBattleWins);
            if(ExactNumbers.Read(s.world.uniqueCampaignStageCompletions)<s.world.completedStageKeys.Count)throw new ArgumentException("Invalid completion count.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in s.totalDefeats) {
                if(row==null || row.heroId!=SssHeroes.CanonicalId(row.heroId) || !SssHeroes.IsSss(row.heroId) || SssHeroes.Scaling(row.heroId)!=SssScaling.TotalDefeats || !ids.Add(row.heroId))throw new ArgumentException("Invalid total-defeat row.");
                ExactNumbers.Read(row.totalDefeats);
            }
            if(s.rewardOffers.Any(x=>x==null || String.IsNullOrWhiteSpace(x.key)) || s.rewardOffers.Select(x=>x.key).Distinct().Count()!=s.rewardOffers.Count)
                throw new ArgumentException("Duplicate or invalid reward offer.");
            foreach(var o in s.rewardOffers) {
                if(o.success && (!o.eligible || !SssHeroes.IsSss(o.heroId) || o.heroId!=SssHeroes.CanonicalId(o.heroId)))throw new ArgumentException("Invalid SSS offer hero/eligibility.");
                if(o.chanceBasisPoints<0 || o.chanceBasisPoints>10000 || o.chanceRoll<0 || o.chanceRoll>=10000)throw new ArgumentException("Invalid saved offer roll.");
                if(o.claimed && !o.success)throw new ArgumentException("Failed offer cannot be claimed.");
                if(!o.success && !String.IsNullOrEmpty(o.heroId))throw new ArgumentException("Failed offer cannot own a hero.");
            }
        }
        // Atomic pure reducer: the host commits this returned snapshot together with the
        // NORMAL death/reward receipt; callbacks/animation frames must not award progress.
        public static SssReduction Defeat(SssSave current,ConfirmedEnemyDefeat fact,IBaseFamilyResolver resolver)
        {
            Validate(current);
            if(fact==null || resolver==null || fact.deployedHeroIds==null)throw new ArgumentNullException();
            string receipt=ExactNumbers.Key(fact.encounterInstanceId,fact.enemySpawnId);
            var r=new SssReduction {next=current.Copy()};
            if(!fact.hostile || !fact.rewardEligible || fact.friendlyOrUnrewardedSummon)return r;
            if(current.killReceipts.Contains(receipt)) {r.duplicate=true;return r;}
            var participants=fact.deployedHeroIds.Select(SssHeroes.CanonicalId).Distinct(StringComparer.Ordinal).ToList();
            if(participants.Any(x=>SssHeroes.IsSss(x) && SssHeroes.Scaling(x)==SssScaling.FamilyDefeats)) {
                var canon=new ConfirmedEnemyDefeat {encounterInstanceId=fact.encounterInstanceId,enemySpawnId=fact.enemySpawnId,
                    enemyDefinitionOrVariantId=fact.enemyDefinitionOrVariantId,hostile=true,rewardEligible=true,deployedHeroIds=participants};
                var fr=CovenantProgression.Apply(current.familyProgress,canon,resolver);
                r.next.familyProgress=fr.next;r.notices.AddRange(fr.notices);
            }
            foreach(string id in participants.Where(x=>SssHeroes.IsSss(x) && SssHeroes.Scaling(x)==SssScaling.TotalDefeats).OrderBy(x=>x,StringComparer.Ordinal)) {
                var row=r.next.totalDefeats.SingleOrDefault(x=>x.heroId==id);
                if(row==null) {row=new TotalDefeatRow {heroId=id};r.next.totalDefeats.Add(row);}
                BigInteger old=ExactNumbers.Read(row.totalDefeats),now=old+1;row.totalDefeats=ExactNumbers.Write(now);
                if(now/10>old/10)r.notices.Add(new GrowthNotice {heroId=id,baseFamilyId="ALL_FAMILIES",totalDefeats=row.totalDefeats,
                    oldRank=ExactNumbers.Write(old/10),newRank=ExactNumbers.Write(now/10),firstUnlock=false});
            }
            r.next.killReceipts.Add(receipt);r.next.totalDefeats=r.next.totalDefeats.OrderBy(x=>x.heroId,StringComparer.Ordinal).ToList();
            r.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);return r;
        }
        public static SssReduction Victory(SssSave current,VictoryFact fact)
        {
            Validate(current);if(fact==null)throw new ArgumentNullException("fact");
            ExactNumbers.RequireId(fact.encounterInstanceId);
            if(fact.mode!="Campaign" && fact.mode!="Tower")throw new ArgumentException("Only real Campaign/Tower victories.");
            string receipt=ExactNumbers.Key(fact.mode,fact.encounterInstanceId);var r=new SssReduction {next=current.Copy()};
            if(!fact.won || !fact.rewardEligible)return r;
            if(current.victoryReceipts.Contains(receipt)) {r.duplicate=true;return r;}
            if(fact.mode=="Tower") {
                var floor=ExactNumbers.Read(fact.towerFloor);
                if(floor<1)throw new ArgumentException("Floor must be positive.");
                r.next.world.highestClearedTowerFloor=ExactNumbers.Write(BigInteger.Max(ExactNumbers.Read(current.world.highestClearedTowerFloor),floor));
            } else {
                r.next.world.campaignBattleWins=ExactNumbers.Write(ExactNumbers.Read(current.world.campaignBattleWins)+1);
                if(fact.completedCampaignStage) {
                    ExactNumbers.RequireId(fact.stageCompletionKey);
                    if(!current.world.completedStageKeys.Contains(fact.stageCompletionKey)) {
                        r.next.world.completedStageKeys.Add(fact.stageCompletionKey);
                        r.next.world.uniqueCampaignStageCompletions=ExactNumbers.Write(ExactNumbers.Read(current.world.uniqueCampaignStageCompletions)+1);
                    }
                }
            }
            r.next.victoryReceipts.Add(receipt);r.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);return r;
        }
    }
}
