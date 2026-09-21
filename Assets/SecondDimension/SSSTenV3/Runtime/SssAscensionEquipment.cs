// V4 extension. Namespace/path intentionally preserved to update the V3 feature
// instead of registering a second SSS runtime. These are transaction plans, NOT
// an alternate inventory, stat engine, or automatic save implementation.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace SecondDimension.SSS.V3
{
    [Serializable] public sealed class SssAscensionRow
    {
        public string heroId;
        public int rank;
        public SssAscensionRow Copy() { return (SssAscensionRow)MemberwiseClone(); }
    }
    [Serializable] public sealed class SssAscensionSave
    {
        public int schemaVersion=1;
        public string revision="0";
        public List<SssAscensionRow> heroes=new List<SssAscensionRow>();
        public List<string> appliedRequestIds=new List<string>();
        public SssAscensionSave Copy()
        {
            return new SssAscensionSave {schemaVersion=schemaVersion, revision=revision,
                heroes=heroes.Select(x=>x.Copy()).ToList(), appliedRequestIds=new List<string>(appliedRequestIds)};
        }
    }
    public sealed class SssAscensionPlan
    {
        public SssAscensionSave next;
        public bool apply, duplicate;
        public string reason, heroId, creditItemId, requestId, expectedRevision, expectedInventoryCreditCount;
        public int priorRank, nextRank, creditQuantityToConsume;
    }
    public static class SssAscension
    {
        public const int MaximumRank=10;
        internal static string RequireHero(string id)
        {
            id=SssHeroes.CanonicalId(id);
            if(!SssHeroes.IsSss(id))throw new ArgumentException("Unknown SSS hero.");
            return id;
        }
        internal static void UniqueKeys(List<string> keys)
        {
            if(keys==null || keys.Any(String.IsNullOrWhiteSpace) || keys.Count!=keys.Distinct(StringComparer.Ordinal).Count())
                throw new ArgumentException("Invalid or duplicate receipt keys.");
        }
        public static void Validate(SssAscensionSave state)
        {
            if(state==null || state.schemaVersion!=1 || state.heroes==null)throw new ArgumentException("Invalid ascension state.");
            ExactNumbers.Read(state.revision);UniqueKeys(state.appliedRequestIds);
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in state.heroes)
                if(row==null || row.heroId!=RequireHero(row.heroId) || row.rank<0 || row.rank>MaximumRank || !seen.Add(row.heroId))
                    throw new ArgumentException("Invalid ascension hero/rank.");
        }
        public static int Rank(SssAscensionSave state,string heroId)
        {
            Validate(state);heroId=RequireHero(heroId);
            var row=state.heroes.SingleOrDefault(x=>x.heroId==heroId);return row==null?0:row.rank;
        }
        // One manually spent hero-bound credit => exactly one rank, A0 through A10.
        // The caller MUST atomically compare both revision AND inventory quantity,
        // consume the credit, update the existing hero's rank/stats, and save next.
        // Never publish next or consume a credit for a failed host transaction.
        public static SssAscensionPlan Plan(SssAscensionSave current,string heroId,string requestId,
            string expectedRevision,string liveHeroBoundCredits,IEnumerable<string> ownedHeroIds,bool legalPreparationBoundary)
        {
            Validate(current);heroId=RequireHero(heroId);ExactNumbers.RequireId(requestId);
            ExactNumbers.Read(expectedRevision);BigInteger credits=ExactNumbers.Read(liveHeroBoundCredits);
            if(ownedHeroIds==null)throw new ArgumentNullException("ownedHeroIds");
            int rank=Rank(current,heroId);
            var plan=new SssAscensionPlan {next=current.Copy(),heroId=heroId,requestId=requestId,
                priorRank=rank,nextRank=rank,creditItemId="SSS_ASCENSION_CREDIT_"+heroId.Substring(4),
                expectedRevision=expectedRevision,expectedInventoryCreditCount=liveHeroBoundCredits};
            if(current.appliedRequestIds.Contains(requestId)) {plan.duplicate=true;plan.reason="Already applied.";return plan;}
            if(ExactNumbers.Read(current.revision)!=ExactNumbers.Read(expectedRevision)) {plan.reason="Stale ascension preview; refresh.";return plan;}
            if(!ownedHeroIds.Select(SssHeroes.CanonicalId).Contains(heroId,StringComparer.Ordinal)) {plan.reason="Recruit this hero first.";return plan;}
            if(!legalPreparationBoundary) {plan.reason="Ascend at a legal preparation boundary, not during action resolution.";return plan;}
            if(rank==MaximumRank) {plan.reason="Ascension 10 reached; mastery remains uncapped. No credit consumed.";return plan;}
            if(credits<1) {plan.reason="One matching hero-bound ascension credit is required.";return plan;}
            var row=plan.next.heroes.SingleOrDefault(x=>x.heroId==heroId);
            if(row==null) {row=new SssAscensionRow {heroId=heroId};plan.next.heroes.Add(row);}
            row.rank=rank+1;plan.next.appliedRequestIds.Add(requestId);
            plan.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            plan.nextRank=row.rank;plan.creditQuantityToConsume=1;plan.apply=true;
            plan.reason="Apply one rank in the existing ascension/inventory transaction; do not reset kills or world progression.";
            return plan;
        }
    }
    [Serializable] public sealed class SssHuntRow
    {
        public string baseFamilyId,totalDefeats="0";
        public SssHuntRow Copy() {return (SssHuntRow)MemberwiseClone();}
    }
    [Serializable] public sealed class SssHuntSave
    {
        public int schemaVersion=1;
        public string revision="0";
        public List<SssHuntRow> families=new List<SssHuntRow>();
        public List<string> creditedSpawns=new List<string>();
        public SssHuntSave Copy()
        {
            return new SssHuntSave {schemaVersion=schemaVersion,revision=revision,
                families=families.Select(x=>x.Copy()).ToList(),creditedSpawns=new List<string>(creditedSpawns)};
        }
    }
    public sealed class SssHuntReduction
    {
        public SssHuntSave next;
        public bool applied,duplicate;
        public string reason;
    }
    public static class SssWeaponHunt
    {
        // Frozen quest version: later family additions do NOT retroactively move
        // this goal. Use the actual canonical IDs, not a count or display name.
        public const string QuestId="SSS_OMEGA_HUNT_070_V1";
        public const int DefeatsRequiredPerFamily=1000;
        public static string[] RequiredFamilies()
        {
            return Enumerable.Range(1,70).Select(i=>"ENEMY_REC_"+i.ToString("000",CultureInfo.InvariantCulture)).ToArray();
        }
        public static void Validate(SssHuntSave state)
        {
            if(state==null || state.schemaVersion!=1 || state.families==null)throw new ArgumentException("Invalid weapon-hunt state.");
            ExactNumbers.Read(state.revision);SssAscension.UniqueKeys(state.creditedSpawns);
            var required=new HashSet<string>(RequiredFamilies(),StringComparer.Ordinal);
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in state.families)
            {
                if(row==null || !required.Contains(row.baseFamilyId) || !seen.Add(row.baseFamilyId))throw new ArgumentException("Unknown/duplicate hunt family.");
                ExactNumbers.Read(row.totalDefeats);
            }
        }
        // Guild-wide quest ledger, independent of the hero-specific growth ledgers.
        // Count the same real kill once, not once per deployed SSS hero. Prefer binding
        // these fields to an existing reliable all-time bestiary/death journal.
        public static SssHuntReduction Defeat(SssHuntSave current,ConfirmedEnemyDefeat fact,IBaseFamilyResolver resolver)
        {
            Validate(current);if(fact==null || resolver==null)throw new ArgumentNullException();
            var result=new SssHuntReduction {next=current.Copy()};
            if(!fact.hostile || !fact.rewardEligible || fact.friendlyOrUnrewardedSummon) {result.reason="Not an eligible hostile defeat.";return result;}
            string receipt=ExactNumbers.Key(fact.encounterInstanceId,fact.enemySpawnId);
            if(current.creditedSpawns.Contains(receipt)) {result.duplicate=true;result.reason="Already credited.";return result;}
            string family;
            if(!resolver.TryResolve(fact.enemyDefinitionOrVariantId,out family))throw new ArgumentException("Unmapped enemy identity; do not guess its family.");
            if(!RequiredFamilies().Contains(family,StringComparer.Ordinal)) {result.reason="Outside the frozen 70-family weapon challenge.";return result;}
            var row=result.next.families.SingleOrDefault(x=>x.baseFamilyId==family);
            if(row==null) {row=new SssHuntRow {baseFamilyId=family};result.next.families.Add(row);}
            row.totalDefeats=ExactNumbers.Write(ExactNumbers.Read(row.totalDefeats)+1);
            result.next.creditedSpawns.Add(receipt);result.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            result.applied=true;result.reason="Commit with the original enemy-death receipt, never from a VFX callback.";
            return result;
        }
        public static int CompletedFamilies(SssHuntSave state)
        {
            Validate(state);return state.families.Count(x=>ExactNumbers.Read(x.totalDefeats)>=DefeatsRequiredPerFamily);
        }
        public static bool Complete(SssHuntSave state) {return CompletedFamilies(state)==70;}
        public static string RemainingDefeats(SssHuntSave state)
        {
            Validate(state);BigInteger sum=0;
            foreach(string family in RequiredFamilies())
            {
                var row=state.families.SingleOrDefault(x=>x.baseFamilyId==family);
                sum+=BigInteger.Max(0,DefeatsRequiredPerFamily-(row==null?BigInteger.Zero:ExactNumbers.Read(row.totalDefeats)));
            }
            return ExactNumbers.Write(sum);
        }
        // Initial migration may use complete, reliable per-family historical totals.
        // Do not SUM the three specialist counters: the same kill credited all three.
        public static SssHuntSave FromVerifiedHistory(IEnumerable<SssHuntRow> rows,IEnumerable<string> realReceiptKeys)
        {
            if(rows==null || realReceiptKeys==null)throw new ArgumentNullException();
            var state=new SssHuntSave {families=rows.Select(x=>x==null?null:x.Copy()).ToList(),
                creditedSpawns=realReceiptKeys.ToList()};Validate(state);return state;
        }
    }
    public sealed class SssWeaponGrantPlan
    {
        public SssSave next;
        public bool grant,autoEquip=false;
        public string heroId,itemId,receipt,reason;
    }
    public static class SssSignatureWeapons
    {
        public static string ItemId(string hero) {return "SSS_WEAPON_"+SssAscension.RequireHero(hero).Substring(4);}
        public static string EntitlementReceipt(string hero) {return ExactNumbers.Key("SssUniqueWeapon",SssAscension.RequireHero(hero));}
        // Both the quest claim and code path share EntitlementReceipt. The host must
        // serialize inventory + SSS receipts together and preserve the entitlement
        // even if the weapon is later sold/discarded. No second free unique item.
        public static SssWeaponGrantPlan PlanHuntClaim(SssSave current,SssHuntSave hunt,string hero,
            IEnumerable<string> ownedHeroes,IEnumerable<string> ownedUniqueItems,bool liveOmegaDefinitionResolved)
        {
            SssProgression.Validate(current);SssWeaponHunt.Validate(hunt);hero=SssAscension.RequireHero(hero);
            if(ownedHeroes==null || ownedUniqueItems==null)throw new ArgumentNullException();
            string item=ItemId(hero),receipt=EntitlementReceipt(hero);
            var p=new SssWeaponGrantPlan {next=current.Copy(),heroId=hero,itemId=item,receipt=receipt};
            if(current.codeReceipts.Contains(receipt)) {p.reason="Signature weapon already claimed by quest or code.";return p;}
            if(!ownedHeroes.Select(SssHeroes.CanonicalId).Contains(hero,StringComparer.Ordinal)) {p.reason="Recruit this hero first.";return p;}
            if(!SssWeaponHunt.Complete(hunt)) {p.reason="Defeat 1,000 enemies in EACH of the 70 required base families.";return p;}
            if(!liveOmegaDefinitionResolved) {p.reason="Live Omega-equivalent item definition must be bound before grant.";return p;}
            p.grant=!ownedUniqueItems.Contains(item,StringComparer.Ordinal);
            p.next.codeReceipts.Add(receipt);p.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            p.reason=p.grant?"Grant once via the existing inventory/reward transaction; never auto-equip.":"Existing unique weapon acknowledged; no duplicate.";
            return p;
        }
    }
}
