// SSS_TEN_V3: supersedes the unfinished SS trio; retain legacy IDs through migration.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SecondDimension.SSS.V3
{
    [Serializable] public sealed class CovenantStats
    {
        public string maxHp="0", maxMp="0", attack="0", magic="0", defense="0", resistance="0", agility="0";
        public BigInteger[] Values() { return new[]{maxHp,maxMp,attack,magic,defense,resistance,agility}.Select(ExactNumbers.Read).ToArray(); }
        public static CovenantStats From(BigInteger[] n)
        {
            if(n==null || n.Length!=7)throw new ArgumentException("Seven raw stat ratings required.");
            string[] s=n.Select(ExactNumbers.Write).ToArray();
            return new CovenantStats { maxHp=s[0],maxMp=s[1],attack=s[2],magic=s[3],defense=s[4],resistance=s[5],agility=s[6] };
        }
        public CovenantStats Copy() { return From(Values()); }
        public CovenantStats Scale(string defeats,int percent)
        { return From(Values().Select(n=>CovenantMastery.Scale(n,defeats,percent)).ToArray()); }
    }
    [Serializable] public sealed class OriginalMemberSnapshot
    {
        public string memberId;
        public CovenantStats stats=new CovenantStats();
        public string currentHp="0",currentMp="0";
        // Opaque existing engine data. The adapter MUST age statuses in battle and preserve
        // injuries, equipment, learned Arts and roster IDs. This is NOT a status engine.
        public string hostPersistentStateJson="{}";
        public OriginalMemberSnapshot Copy() { return new OriginalMemberSnapshot { memberId=memberId,stats=stats.Copy(),currentHp=currentHp,currentMp=currentMp,hostPersistentStateJson=hostPersistentStateJson }; }
        public void Validate()
        {
            ExactNumbers.RequireId(memberId);if(stats==null)throw new ArgumentException("Missing original stats.");var n=stats.Values();
            if(n[0]<=0 || ExactNumbers.Read(currentHp)>n[0] || ExactNumbers.Read(currentMp)>n[1])
                throw new ArgumentException("Invalid original member resource state.");
        }
    }
    [Serializable] public sealed class UnionTransformationSave
    {
        public int schemaVersion=3;
        public string encounterInstanceId,sourceUnionId,heroId,baseFamilyId,masteryRank;
        public bool active=true,defeated,restored,hpWasChanged,mpWasChanged;
        public List<OriginalMemberSnapshot> originalMembers=new List<OriginalMemberSnapshot>();
        public CovenantStats unscaledCombinedStats=new CovenantStats();
        public CovenantStats transformedStats=new CovenantStats();
        public string currentHp="0",currentMp="0";
        public UnionTransformationSave Copy()
        {
            return new UnionTransformationSave { schemaVersion=schemaVersion,encounterInstanceId=encounterInstanceId,
                sourceUnionId=sourceUnionId,heroId=heroId,baseFamilyId=baseFamilyId,masteryRank=masteryRank,
                active=active,defeated=defeated,restored=restored,hpWasChanged=hpWasChanged,mpWasChanged=mpWasChanged,
                originalMembers=originalMembers.Select(x=>x.Copy()).ToList(),unscaledCombinedStats=unscaledCombinedStats.Copy(),
                transformedStats=transformedStats.Copy(),currentHp=currentHp,currentMp=currentMp };
        }
    }
    public sealed class TransformationEnd
    {
        public UnionTransformationSave next;
        public List<OriginalMemberSnapshot> restoredMembers;
        public bool sourceUnionDefeated;
    }
    public static class CovenantTransformation
    {
        static BigInteger Sum(IEnumerable<BigInteger> numbers) { return numbers.Aggregate(BigInteger.Zero,(a,b)=>a+b); }
        static BigInteger Pool(IEnumerable<OriginalMemberSnapshot> m,bool hp,bool maximum)
        {
            return Sum(m.Select(x=>ExactNumbers.Read(hp?(maximum?x.stats.maxHp:x.currentHp):(maximum?x.stats.maxMp:x.currentMp))));
        }
        static BigInteger Project(BigInteger current,BigInteger oldMax,BigInteger newMax)
        {
            if(current.Sign<0 || oldMax.Sign<0 || newMax.Sign<0 || current>oldMax)throw new ArgumentOutOfRangeException();
            if(current==0 || oldMax==0 || newMax==0)return BigInteger.Zero;
            return BigInteger.Max(BigInteger.One,current*newMax/oldMax);
        }
        public static UnionTransformationSave Begin(string encounterId,string sourceUnionId,string heroId,string familyId,
            IList<OriginalMemberSnapshot> originalMembers,CovenantStats friendlyFormTemplate,string defeats,
            int percentPerExtraRank=CovenantMastery.DefaultPercentPerExtraRank)
        {
            ExactNumbers.RequireId(encounterId);ExactNumbers.RequireId(sourceUnionId);ExactNumbers.RequireId(familyId);
            if(heroId!=CovenantHeroes.Shapeshifter)throw new ArgumentException("Only the Shapeshifter performs Union Metamorphosis.");
            if(originalMembers==null || originalMembers.Count==0 || friendlyFormTemplate==null)throw new ArgumentException("Missing form input.");
            foreach(var m in originalMembers) { if(m==null)throw new ArgumentException("Null member.");m.Validate(); }
            if(originalMembers.Select(x=>x.memberId).Distinct(StringComparer.Ordinal).Count()!=originalMembers.Count)
                throw new ArgumentException("A Union member may contribute only once.");
            var caster=originalMembers.SingleOrDefault(x=>x.memberId==heroId);
            if(caster==null || ExactNumbers.Read(caster.currentHp)==0)throw new InvalidOperationException("Living caster must belong to Union.");
            var view=CovenantMastery.View(defeats,percentPerExtraRank);
            if(!view.unlocked)throw new InvalidOperationException("Transformation family not unlocked.");
            // All original members contribute once, including their zero-current-HP state
            // when downed. Never add an already-aggregated Union total again.
            BigInteger[] combined=friendlyFormTemplate.Values();
            foreach(var m in originalMembers) { var v=m.stats.Values();for(int i=0;i<combined.Length;i++)combined[i]+=v[i]; }
            var raw=CovenantStats.From(combined);var scaled=raw.Scale(defeats,percentPerExtraRank);
            var snapshot=new UnionTransformationSave { encounterInstanceId=encounterId,sourceUnionId=sourceUnionId,
                heroId=heroId,baseFamilyId=familyId,masteryRank=view.rank,originalMembers=originalMembers.Select(x=>x.Copy()).ToList(),
                unscaledCombinedStats=raw,transformedStats=scaled,
                currentHp=ExactNumbers.Write(Project(Pool(originalMembers,true,false),Pool(originalMembers,true,true),ExactNumbers.Read(scaled.maxHp))),
                currentMp=ExactNumbers.Write(Project(Pool(originalMembers,false,false),Pool(originalMembers,false,true),ExactNumbers.Read(scaled.maxMp))) };
            return snapshot;
        }
        public static void Validate(UnionTransformationSave state)
        {
            if(state==null || state.schemaVersion!=3 || state.originalMembers==null || state.originalMembers.Count==0)
                throw new ArgumentException("Invalid transformation save.");
            ExactNumbers.RequireId(state.encounterInstanceId);ExactNumbers.RequireId(state.sourceUnionId);ExactNumbers.RequireId(state.baseFamilyId);
            if(state.heroId!=CovenantHeroes.Shapeshifter || ExactNumbers.Read(state.masteryRank)<1)throw new ArgumentException("Invalid form owner/rank.");
            foreach(var m in state.originalMembers) { if(m==null)throw new ArgumentException("Null original member."); m.Validate(); }
            if(state.originalMembers.Select(x=>x.memberId).Distinct(StringComparer.Ordinal).Count()!=state.originalMembers.Count ||
                !state.originalMembers.Any(x=>x.memberId==state.heroId))throw new ArgumentException("Invalid original member IDs.");
            if(state.unscaledCombinedStats==null || state.transformedStats==null)throw new ArgumentException("Missing transformation stat snapshot.");
            var raw=state.unscaledCombinedStats.Values();var now=state.transformedStats.Values();
            if(raw[0]<=0 || now[0]<=0 || ExactNumbers.Read(state.currentHp)>now[0] || ExactNumbers.Read(state.currentMp)>now[1])
                throw new ArgumentException("Invalid transformation resources.");
            if(state.restored && state.active)throw new ArgumentException("Restored form cannot remain active.");
            if(ExactNumbers.Read(state.currentHp)==0 && !state.defeated)throw new ArgumentException("Zero-HP form must be marked defeated.");
            if(state.defeated && ExactNumbers.Read(state.currentHp)!=0)throw new ArgumentException("Defeated form must have zero HP.");
        }
        public static UnionTransformationSave WithResources(UnionTransformationSave state,string hp,string mp)
        {
            Validate(state);
            if(!state.active || state.restored || state.defeated)throw new InvalidOperationException("Form is not a living active Union.");
            BigInteger h=ExactNumbers.Read(hp),m=ExactNumbers.Read(mp);
            if(h>ExactNumbers.Read(state.transformedStats.maxHp)||m>ExactNumbers.Read(state.transformedStats.maxMp))
                throw new ArgumentOutOfRangeException("Resources exceed maximum.");
            var next=state.Copy();next.hpWasChanged|=h!=ExactNumbers.Read(state.currentHp);next.mpWasChanged|=m!=ExactNumbers.Read(state.currentMp);
            next.currentHp=ExactNumbers.Write(h);next.currentMp=ExactNumbers.Write(m);next.defeated=h==0;
            return next;
        }
        // Apply growth only at a legal simulation/Forecast boundary, never while an action
        // is already executing. Recompute from the frozen unscaled template, not scaled stats.
        public static UnionTransformationSave RebaseMastery(UnionTransformationSave state,string defeats,
            int percentPerExtraRank=CovenantMastery.DefaultPercentPerExtraRank)
        {
            Validate(state);
            if(state.restored || !state.active)throw new InvalidOperationException("No active form.");
            MasteryView view=CovenantMastery.View(defeats,percentPerExtraRank);
            BigInteger rank=ExactNumbers.Read(view.rank),old=ExactNumbers.Read(state.masteryRank);
            if(rank<old)throw new InvalidOperationException("Progress cannot silently decrease.");
            if(rank==old)return state.Copy();
            var next=state.Copy();next.transformedStats=state.unscaledCombinedStats.Scale(defeats,percentPerExtraRank);next.masteryRank=view.rank;
            next.currentHp=ExactNumbers.Write(Project(ExactNumbers.Read(state.currentHp),ExactNumbers.Read(state.transformedStats.maxHp),ExactNumbers.Read(next.transformedStats.maxHp)));
            next.currentMp=ExactNumbers.Write(Project(ExactNumbers.Read(state.currentMp),ExactNumbers.Read(state.transformedStats.maxMp),ExactNumbers.Read(next.transformedStats.maxMp)));
            return next;
        }
        // Largest-remainder allocation avoids changing total resources or healing damaged
        // members just because the six original sprites reappear. Stable slot-order ties.
        public static BigInteger[] Allocate(BigInteger total,BigInteger[] weights)
        {
            if(total.Sign<0 || weights==null || weights.Any(x=>x.Sign<0))throw new ArgumentOutOfRangeException();
            BigInteger denominator=Sum(weights);var values=new BigInteger[weights.Length];
            if(denominator==0) { if(total!=0)throw new ArgumentException("No allocation capacity.");return values; }
            if(total>denominator)throw new ArgumentException("Allocation exceeds capacity.");
            var remainder=new BigInteger[weights.Length];
            for(int i=0;i<weights.Length;i++) { values[i]=total*weights[i]/denominator;remainder[i]=total*weights[i]%denominator; }
            int missing=(int)(total-Sum(values));
            foreach(int i in Enumerable.Range(0,weights.Length).OrderByDescending(x=>remainder[x]).ThenBy(x=>x).Take(missing))values[i]++;
            return values;
        }
        static BigInteger[] RestorePool(UnionTransformationSave state,bool hp)
        {
            var original=state.originalMembers;
            BigInteger[] before=original.Select(x=>ExactNumbers.Read(hp?x.currentHp:x.currentMp)).ToArray();
            BigInteger[] maxima=original.Select(x=>ExactNumbers.Read(hp?x.stats.maxHp:x.stats.maxMp)).ToArray();
            bool[] wasAlive=original.Select(x=>ExactNumbers.Read(x.currentHp)>0).ToArray();
            // A form defeat is an original-Union defeat, not a free second health bar.
            if(hp && state.defeated)return new BigInteger[before.Length];
            bool changed=hp?state.hpWasChanged:state.mpWasChanged;
            if(!changed)return before;
            BigInteger current=ExactNumbers.Read(hp?state.currentHp:state.currentMp);
            BigInteger formMax=ExactNumbers.Read(hp?state.transformedStats.maxHp:state.transformedStats.maxMp);
            BigInteger target=formMax==0?BigInteger.Zero:current*Sum(maxima)/formMax;
            BigInteger[] capacity=maxima.Select((x,i)=>hp && !wasAlive[i]?BigInteger.Zero:x).ToArray();
            target=BigInteger.Min(Sum(capacity),target);
            // Preserve a surviving Union at one HP instead of rounding its pooled HP
            // down to zero while its transformed body is still alive.
            if(hp && current>0 && target==0 && Sum(capacity)>0)target=BigInteger.One;
            BigInteger start=Sum(before);
            if(target<=start)return Allocate(target,before);
            BigInteger[] extraCapacity=capacity.Select((x,i)=>x-before[i]).ToArray();
            BigInteger[] extra=Allocate(target-start,extraCapacity);
            return before.Select((x,i)=>x+extra[i]).ToArray();
        }
        // Invoke at the encounter terminal result (including retreat). Mid-battle voluntary
        // exit/recast is not supported. Existing revival rules require an explicit adapter.
        public static TransformationEnd EndBattle(UnionTransformationSave state)
        {
            Validate(state);
            if(state.restored || !state.active)throw new InvalidOperationException("Transformation already restored.");
            var next=state.Copy();BigInteger[] hp=RestorePool(state,true),mp=RestorePool(state,false);
            var members=state.originalMembers.Select(x=>x.Copy()).ToList();
            for(int i=0;i<members.Count;i++) { members[i].currentHp=ExactNumbers.Write(hp[i]);members[i].currentMp=ExactNumbers.Write(mp[i]); }
            next.active=false;next.restored=true;
            return new TransformationEnd { next=next,restoredMembers=members,sourceUnionDefeated=state.defeated };
        }
    }
}
