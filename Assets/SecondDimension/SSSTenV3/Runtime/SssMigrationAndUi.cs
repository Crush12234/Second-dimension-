using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
namespace SecondDimension.SSS.V3
{
    public static class SssMigration
    {
        // Deserialize the OLD family's JSON into this field-compatible DTO before calling.
        // Does not invent kills that the old capped counter failed to retain.
        public static SssSave FromLegacyFamilyData(CovenantProgressSave old,string realHighestTower="0",
            string realCampaignWins="0",IEnumerable<string> realCompletedStageKeys=null)
        {
            if(old==null || old.counters==null || old.creditedEnemySpawns==null)throw new ArgumentException("Missing legacy save.");
            var next=new SssSave();
            foreach(var row in old.counters) {
                if(row==null)throw new ArgumentException("Null legacy row.");
                var id=SssHeroes.CanonicalId(row.heroId);CovenantKind k;
                if(!CovenantHeroes.TryKind(id,out k))throw new ArgumentException("Legacy row is not one of the three covenant heroes.");
                ExactNumbers.RequireId(row.baseFamilyId);BigInteger n=ExactNumbers.Read(row.totalDefeats);
                var prior=next.familyProgress.counters.SingleOrDefault(x=>x.heroId==id && x.baseFamilyId==row.baseFamilyId);
                if(prior==null)next.familyProgress.counters.Add(new CounterRow {heroId=id,baseFamilyId=row.baseFamilyId,totalDefeats=ExactNumbers.Write(n)});
                else prior.totalDefeats=ExactNumbers.Write(BigInteger.Max(n,ExactNumbers.Read(prior.totalDefeats)));
            }
            next.familyProgress.creditedEnemySpawns=old.creditedEnemySpawns.Distinct(StringComparer.Ordinal).ToList();
            next.killReceipts=new List<string>(next.familyProgress.creditedEnemySpawns);
            next.world.highestClearedTowerFloor=ExactNumbers.Write(ExactNumbers.Read(realHighestTower));
            next.world.campaignBattleWins=ExactNumbers.Write(ExactNumbers.Read(realCampaignWins));
            next.world.completedStageKeys=(realCompletedStageKeys ?? new string[0]).Distinct(StringComparer.Ordinal).ToList();
            next.world.uniqueCampaignStageCompletions=next.world.completedStageKeys.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            SssProgression.Validate(next);return next;
        }
    }
    [Serializable] public sealed class SssTrackerRow
    {
        public string key,label,total,rank,nextMilestone,remaining,powerNumerator;
        public int powerDenominator=100;
        public bool unlocked;
    }
    public static class SssCharacterSheet
    {
        // Bind these view models to the existing character sheet, not a new dashboard.
        public static List<SssTrackerRow> Rows(SssSave state,string heroId,SssFamilyCatalog families)
        {
            SssProgression.Validate(state);string id=SssHeroes.CanonicalId(heroId);var scaling=SssHeroes.Scaling(id);
            var result=new List<SssTrackerRow>();
            if(scaling==SssScaling.FamilyDefeats) {
                if(families==null || families.families==null)throw new ArgumentNullException("families");
                foreach(var family in families.families)result.Add(Row(family.baseFamilyId,family.displayName,SssGrowth.ForHero(state,id,family.baseFamilyId)));
            } else if(scaling==SssScaling.TotalDefeats)result.Add(Row("ALL_FAMILIES","All eligible enemies defeated while deployed",SssGrowth.ForHero(state,id)));
            else {
                var v=SssGrowth.ForHero(state,id);
                result.Add(Row("WORLD_MASTERY","Combined Tower and Campaign mastery",v));
                result.Add(new SssTrackerRow {key="TOWER",label="Highest cleared Tower floor",total=state.world.highestClearedTowerFloor,
                    nextMilestone=ExactNumbers.Write((ExactNumbers.Read(state.world.highestClearedTowerFloor)/10+1)*10),unlocked=true});
                result.Add(new SssTrackerRow {key="CAMPAIGN",label="Unique completed Campaign stages",total=state.world.uniqueCampaignStageCompletions,
                    nextMilestone=ExactNumbers.Write((ExactNumbers.Read(state.world.uniqueCampaignStageCompletions)/10+1)*10),unlocked=true});
            }
            return result;
        }
        static SssTrackerRow Row(string key,string label,SssGrowthView v)
        {return new SssTrackerRow {key=key,label=label,total=v.counter,rank=v.rank,nextMilestone=v.nextMilestone,remaining=v.remaining,powerNumerator=v.powerNumerator,unlocked=v.unlocked};}
    }
}
