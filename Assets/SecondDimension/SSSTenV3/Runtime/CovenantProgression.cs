// SSS_TEN_V3: supersedes the unfinished SS trio; retain legacy IDs through migration.
// Engine-independent domain code. Bind reducers to the EXISTING reward/save transaction.
// Save arbitrary-precision counters as decimal strings; never as float/double/Unity int.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace SecondDimension.SSS.V3
{
    public enum CovenantKind { Tamer, Summoner, Shapeshifter }

    public static class CovenantHeroes
    {
        public const string Tamer = "SSS_RYLEN_STONEBOND";
        public const string Summoner = "SSS_ELYSIA_NIGHTCALL";
        public const string Shapeshifter = "SSS_VAELIS_MANYFORM";
        public static bool TryKind(string id, out CovenantKind kind)
        {
            kind = CovenantKind.Tamer;
            if (id == Tamer) return true;
            if (id == Summoner) { kind = CovenantKind.Summoner; return true; }
            if (id == Shapeshifter) { kind = CovenantKind.Shapeshifter; return true; }
            return false;
        }
    }

    public static class ExactNumbers
    {
        public static BigInteger Read(string value)
        {
            if (String.IsNullOrEmpty(value) || value.Any(c => c < '0' || c > '9'))
                throw new ArgumentException("Expected a nonnegative decimal integer string.");
            return BigInteger.Parse(value, CultureInfo.InvariantCulture);
        }
        public static string Write(BigInteger value)
        {
            if (value.Sign < 0) throw new ArgumentOutOfRangeException("value");
            return value.ToString(CultureInfo.InvariantCulture);
        }
        public static string RequireId(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value != value.Trim())
                throw new ArgumentException("Missing or padded stable ID.");
            return value;
        }
        public static string Key(string a, string b)
        {
            RequireId(a); RequireId(b);
            return a.Length.ToString(CultureInfo.InvariantCulture) + ":" + a + b;
        }
    }

    [Serializable] public sealed class CounterRow
    {
        public string heroId;
        public string baseFamilyId;
        public string totalDefeats = "0";
        public CounterRow Copy() { return new CounterRow { heroId=heroId, baseFamilyId=baseFamilyId, totalDefeats=totalDefeats }; }
    }
    [Serializable] public sealed class CovenantProgressSave
    {
        public int schemaVersion = 3;
        public string revision = "0";
        public List<CounterRow> counters = new List<CounterRow>();
        // Kept for correctness; integrate with existing immutable reward receipts at scale.
        // Never discard receipts merely because battle ended or ten kills were reached.
        public List<string> creditedEnemySpawns = new List<string>();
        public CovenantProgressSave Copy()
        {
            return new CovenantProgressSave { schemaVersion=schemaVersion, revision=revision,
                counters=counters.Select(x=>x.Copy()).ToList(), creditedEnemySpawns=new List<string>(creditedEnemySpawns) };
        }
    }
    [Serializable] public sealed class ConfirmedEnemyDefeat
    {
        public string encounterInstanceId;
        public string enemySpawnId;
        public string enemyDefinitionOrVariantId;
        public bool hostile;
        public bool rewardEligible;
        public bool friendlyOrUnrewardedSummon;
        // From battle participation at defeat time, NOT current display actors.
        // Includes heroes represented by an active transformation; excludes guild bench.
        public List<string> deployedHeroIds = new List<string>();
    }
    [Serializable] public sealed class GrowthNotice
    {
        public string heroId, baseFamilyId, totalDefeats, oldRank, newRank;
        public bool firstUnlock;
    }
    public sealed class ProgressReduction
    {
        public CovenantProgressSave next;
        public bool duplicate;
        public List<GrowthNotice> notices = new List<GrowthNotice>();
    }
    public interface IBaseFamilyResolver
    {
        // Must use the catalog mapping, not cosmetic display names or string guessing.
        bool TryResolve(string enemyDefinitionOrVariantId, out string baseFamilyId);
    }

    [Serializable] public sealed class MasteryView
    {
        public string totalDefeats, rank, nextMilestone, defeatsRemaining, powerNumerator;
        public int powerDenominator = 100;
        public int progressToNextTen;
        public bool unlocked;
    }
    public static class CovenantMastery
    {
        public const int DefaultPercentPerExtraRank = 2; // New tuning default, not a preexisting game law.
        public static MasteryView View(string totalDefeats, int percentPerExtraRank = DefaultPercentPerExtraRank)
        {
            if (percentPerExtraRank <= 0) throw new ArgumentOutOfRangeException("percentPerExtraRank");
            BigInteger kills=ExactNumbers.Read(totalDefeats), rank=kills/10;
            BigInteger extra=BigInteger.Max(BigInteger.Zero,rank-1);
            return new MasteryView { totalDefeats=ExactNumbers.Write(kills), rank=ExactNumbers.Write(rank),
                unlocked=kills>=10, progressToNextTen=(int)(kills%10),
                nextMilestone=ExactNumbers.Write((rank+1)*10), defeatsRemaining=ExactNumbers.Write(10-kills%10),
                powerNumerator=ExactNumbers.Write(100 + extra*percentPerExtraRank) };
        }
        public static BigInteger Scale(BigInteger unscaled, string totalDefeats, int percentPerExtraRank=DefaultPercentPerExtraRank)
        {
            if (unscaled.Sign<0) throw new ArgumentOutOfRangeException("unscaled");
            MasteryView v=View(totalDefeats,percentPerExtraRank);
            if (!v.unlocked) throw new InvalidOperationException("Family is not unlocked.");
            // Round up at the final stat boundary. Retain numerator in damage/preview math
            // where the live stat engine supports exact fractional ratings.
            return (unscaled*ExactNumbers.Read(v.powerNumerator)+99)/100;
        }
        public static string Count(CovenantProgressSave save, string heroId, string familyId)
        {
            CounterRow row=save.counters.SingleOrDefault(x=>x.heroId==heroId && x.baseFamilyId==familyId);
            return row==null ? "0" : row.totalDefeats;
        }
    }

    public static class CovenantProgression
    {
        public static void Validate(CovenantProgressSave save)
        {
            if (save==null || save.schemaVersion!=3 || save.counters==null || save.creditedEnemySpawns==null)
                throw new ArgumentException("Invalid V3 progress save; migrate explicitly.");
            ExactNumbers.Read(save.revision);
            var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in save.counters)
            {
                CovenantKind kind;
                if (row==null || !CovenantHeroes.TryKind(row.heroId,out kind)) throw new ArgumentException("Unknown hero.");
                ExactNumbers.Read(row.totalDefeats);
                if (!keys.Add(ExactNumbers.Key(row.heroId,row.baseFamilyId))) throw new ArgumentException("Duplicate counter.");
            }
            if (save.creditedEnemySpawns.Any(String.IsNullOrEmpty) ||
                save.creditedEnemySpawns.Count!=save.creditedEnemySpawns.Distinct(StringComparer.Ordinal).Count())
                throw new ArgumentException("Invalid or duplicate enemy-spawn receipt.");
        }
        public static ProgressReduction Apply(CovenantProgressSave current, ConfirmedEnemyDefeat defeated,
            IBaseFamilyResolver families, int percentPerExtraRank=CovenantMastery.DefaultPercentPerExtraRank)
        {
            Validate(current);
            if (defeated==null || families==null || defeated.deployedHeroIds==null) throw new ArgumentNullException();
            if (percentPerExtraRank<=0) throw new ArgumentOutOfRangeException("percentPerExtraRank");
            string receipt=ExactNumbers.Key(defeated.encounterInstanceId,defeated.enemySpawnId);
            var result=new ProgressReduction { next=current.Copy() };
            if (!defeated.hostile || !defeated.rewardEligible || defeated.friendlyOrUnrewardedSummon) return result;
            if (current.creditedEnemySpawns.Contains(receipt)) { result.duplicate=true; return result; }
            string family;
            if (!families.TryResolve(defeated.enemyDefinitionOrVariantId,out family))
                throw new InvalidOperationException("Unmapped enemy family; do not silently lose a real defeat.");
            ExactNumbers.RequireId(family);
            // Build a new snapshot. Caller atomically commits this and the normal kill/reward
            // receipt using the existing save authority and optimistic revision/transaction.
            foreach (string hero in defeated.deployedHeroIds.Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal))
            {
                CovenantKind kind;
                if (!CovenantHeroes.TryKind(hero,out kind)) continue;
                CounterRow row=result.next.counters.SingleOrDefault(x=>x.heroId==hero && x.baseFamilyId==family);
                if (row==null) { row=new CounterRow { heroId=hero,baseFamilyId=family }; result.next.counters.Add(row); }
                BigInteger old=ExactNumbers.Read(row.totalDefeats), now=old+1;
                row.totalDefeats=ExactNumbers.Write(now);
                if (now/10 > old/10) result.notices.Add(new GrowthNotice { heroId=hero,baseFamilyId=family,
                    totalDefeats=row.totalDefeats,oldRank=ExactNumbers.Write(old/10),newRank=ExactNumbers.Write(now/10),firstUnlock=old<10 && now>=10 });
            }
            result.next.creditedEnemySpawns.Add(receipt);
            result.next.creditedEnemySpawns.Sort(StringComparer.Ordinal);
            result.next.counters=result.next.counters.OrderBy(x=>x.heroId,StringComparer.Ordinal).ThenBy(x=>x.baseFamilyId,StringComparer.Ordinal).ToList();
            result.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            return result;
        }
        public static CounterRow MigrateLegacy(string heroId,string familyId,string savedCount,bool oldUnlocked)
        {
            CovenantKind kind;
            if (!CovenantHeroes.TryKind(heroId,out kind)) throw new ArgumentException("Unknown hero.");
            ExactNumbers.RequireId(familyId);
            // A capped legacy counter cannot reconstruct lost lifetime defeats. Keep the
            // known minimum; replay only an authoritative historical defeat journal.
            BigInteger known=String.IsNullOrEmpty(savedCount)?BigInteger.Zero:ExactNumbers.Read(savedCount);
            if (oldUnlocked) known=BigInteger.Max(10,known);
            return new CounterRow { heroId=heroId,baseFamilyId=familyId,totalDefeats=ExactNumbers.Write(known) };
        }
    }
}
