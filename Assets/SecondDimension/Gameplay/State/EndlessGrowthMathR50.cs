// OFFLINE/PREPARED CANDIDATE. Not a combat resolver, wallet, inventory, or save.
// Integrate only after the real stat/serialization/price pipeline has been reviewed.
// Illustrative economy coefficients (100/10000) MUST NOT be installed as approved tuning.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace SecondDimension.Prepared.EndlessMathR10
{
    // Read-only presentation/calculation result, not a persisted game state.
    public sealed class QuadraticRecruitXpRead
    {
        public readonly BigInteger TotalXp, Level, LevelFloorXp, NextLevelFloorXp;
        public readonly BigInteger IntoLevelXp, MissingToNextXp;
        public readonly int ProgressBasisPoints;
        internal QuadraticRecruitXpRead(BigInteger totalXp, BigInteger level, BigInteger floorXp, BigInteger nextXp)
        {
            if (floorXp > totalXp || totalXp >= nextXp || nextXp <= floorXp)
                throw new ArgumentException("INVALID_QUADRATIC_XP_INTERVAL");
            TotalXp = totalXp; Level = level; LevelFloorXp = floorXp; NextLevelFloorXp = nextXp;
            IntoLevelXp = totalXp - floorXp; MissingToNextXp = nextXp - totalXp;
            ProgressBasisPoints = checked((int)(IntoLevelXp * 10000 / (nextXp - floorXp)));
        }
    }
    public static class EndlessGrowthMath
    {
        static readonly Dictionary<int, long[]> Prefixes = new Dictionary<int, long[]>();
        static readonly object PrefixLock = new object();
        static void Natural(BigInteger value, string name, int min = 0)
        { if (value < min) throw new ArgumentOutOfRangeException(name); }

        public static BigInteger CeilDiv(BigInteger numerator, BigInteger denominator)
        {
            Natural(numerator, nameof(numerator)); Natural(denominator, nameof(denominator), 1);
            return (numerator + denominator - 1) / denominator;
        }
        public static string ToCanonicalDecimal(BigInteger value)
        { Natural(value, nameof(value)); return value.ToString(CultureInfo.InvariantCulture); }
        public static BigInteger ParseCanonicalDecimal(string value)
        {
            if (String.IsNullOrEmpty(value) || (value.Length > 1 && value[0] == '0'))
                throw new FormatException("Canonical nonnegative decimal required");
            foreach (char c in value) if (c < '0' || c > '9') throw new FormatException("ASCII digits required");
            return BigInteger.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }
        public static BigInteger PositiveStat(BigInteger cleanBase, IEnumerable<BigInteger> contributionsBps, bool protectedStat = false)
        {
            Natural(cleanBase, nameof(cleanBase));
            if (contributionsBps == null) throw new ArgumentNullException(nameof(contributionsBps));
            BigInteger sum = 0;
            foreach (BigInteger c in contributionsBps) { Natural(c, "contribution"); sum += c; }
            return protectedStat ? cleanBase : CeilDiv(cleanBase * (10000 + sum), 10000);
        }
        public static BigInteger AscensionAfterTen(BigInteger statAtTen, BigInteger ascension, BigInteger authoredPerTier)
        {
            Natural(statAtTen, nameof(statAtTen)); Natural(ascension, nameof(ascension), 10);
            Natural(authoredPerTier, nameof(authoredPerTier));
            return statAtTen + (ascension - 10) * authoredPerTier;
        }
        public static BigInteger InfusionUnitsForLevel(BigInteger level)
        { Natural(level, nameof(level)); return level * (level + 1) / 2; }
        public static BigInteger IntegerSquareRoot(BigInteger n)
        {
            Natural(n, nameof(n)); if (n < 2) return n;
            int seedBits = checked((n.ToByteArray().Length * 8 + 1) / 2);
            BigInteger x = BigInteger.One << seedBits;
            while (true) {
                BigInteger next = (x + n / x) / 2;
                if (next >= x) return x;
                x = next;
            }
        }
        public static BigInteger InfusionLevel(BigInteger units)
        { Natural(units, nameof(units)); return (IntegerSquareRoot(8 * units + 1) - 1) / 2; }
        public static BigInteger NextIllustrativeCost(BigInteger basePrice, BigInteger postThreshold)
        {
            Natural(basePrice, nameof(basePrice), 1); Natural(postThreshold, nameof(postThreshold));
            return CeilDiv(basePrice * (100 + postThreshold) * (100 + postThreshold), 10000);
        }
        // R48: exact arithmetic for an explicitly supplied quadratic XP curve.
        // a=50 matches supplied RECRUIT_PROGRESSION_021_V1 thresholds at levels1..100.
        // Extending beyond100 is a NEW native migration decision, never a hidden unlock.
        // No default coefficient, native approval, class-growth grant, wallet or save here.
        public static BigInteger QuadraticRecruitXpThreshold(BigInteger level, BigInteger coefficient)
        {
            Natural(level, nameof(level), 1); Natural(coefficient, nameof(coefficient), 1);
            return coefficient * level * (level - 1);
        }
        public static BigInteger LevelForQuadraticRecruitXp(BigInteger totalXp, BigInteger coefficient)
        {
            Natural(totalXp, nameof(totalXp)); Natural(coefficient, nameof(coefficient), 1);
            // Integer division is intentional: level*(level-1) is an integer.
            // Solve L*(L-1) <= floor(XP/a) with an integer square root; no level loop.
            return (BigInteger.One + IntegerSquareRoot(BigInteger.One + 4 * (totalXp / coefficient))) / 2;
        }
        public static QuadraticRecruitXpRead ReadQuadraticRecruitXp(BigInteger totalXp, BigInteger coefficient)
        {
            BigInteger level = LevelForQuadraticRecruitXp(totalXp, coefficient);
            return new QuadraticRecruitXpRead(totalXp, level,
                QuadraticRecruitXpThreshold(level, coefficient),
                QuadraticRecruitXpThreshold(level + 1, coefficient));
        }
        public static BigInteger PersonalXpMissingToQuadraticLevel(BigInteger totalXp, BigInteger targetLevel, BigInteger coefficient)
        {
            Natural(totalXp, nameof(totalXp));
            return BigInteger.Max(BigInteger.Zero, QuadraticRecruitXpThreshold(targetLevel, coefficient) - totalXp);
        }
        public static BigInteger TreasuryCostForPersonalXp(BigInteger missingPersonalXp, BigInteger treasuryNumerator, BigInteger personalXpDenominator)
        {
            Natural(missingPersonalXp, nameof(missingPersonalXp));
            Natural(treasuryNumerator, nameof(treasuryNumerator), 1);
            Natural(personalXpDenominator, nameof(personalXpDenominator), 1);
            // Caller supplies an approved native exchange rate. No shipping rate implied.
            // Round ONCE for the confirmed grant. +5 must not bill five full thresholds.
            return CeilDiv(missingPersonalXp * treasuryNumerator, personalXpDenominator);
        }
        static BigInteger SquareSum(BigInteger n) { return n * (n + 1) * (2 * n + 1) / 6; }
        static long[] Prefix(int priceMod)
        {
            lock (PrefixLock) {
                long[] found;
                if (Prefixes.TryGetValue(priceMod, out found)) return found;
                var result = new long[10001];
                for (int r=0; r<10000; r++) {
                    long rem = ((long)priceMod * r * r) % 10000L;
                    result[r+1] = result[r] + ((10000L-rem) % 10000L);
                }
                // Bound cache; evicting changes performance, never arithmetic.
                if (Prefixes.Count >= 64) Prefixes.Clear();
                Prefixes.Add(priceMod,result); return result;
            }
        }
        public static BigInteger BulkIllustrativeCost(BigInteger basePrice, BigInteger startPostThreshold, BigInteger count)
        {
            Natural(basePrice, nameof(basePrice),1); Natural(startPostThreshold,nameof(startPostThreshold));
            Natural(count,nameof(count)); if(count.IsZero) return BigInteger.Zero;
            BigInteger start = 100 + startPostThreshold;
            BigInteger squares = SquareSum(start+count-1)-SquareSum(start-1);
            long[] prefix = Prefix((int)(basePrice % 10000));
            BigInteger full = count / 10000;
            int remainder = (int)(count % 10000), offset=(int)(start % 10000);
            int stop=Math.Min(offset+remainder,10000), wrap=Math.Max(0,offset+remainder-10000);
            BigInteger correction=full*prefix[10000]+prefix[stop]-prefix[offset]+prefix[wrap];
            BigInteger total=basePrice*squares+correction;
            if(total%10000!=0) throw new InvalidOperationException("Bulk rounding invariant");
            return total/10000;
        }
        public static BigInteger MaximumAffordableLevels(BigInteger basePrice, BigInteger startPostThreshold, BigInteger wallet, BigInteger reserve)
        {
            Natural(basePrice,nameof(basePrice),1); Natural(startPostThreshold,nameof(startPostThreshold));
            Natural(wallet,nameof(wallet)); Natural(reserve,nameof(reserve));
            BigInteger budget=BigInteger.Max(0,wallet-reserve), lo=0, hi=1;
            while(BulkIllustrativeCost(basePrice,startPostThreshold,hi)<=budget){lo=hi;hi*=2;}
            while(lo+1<hi){
                BigInteger mid=(lo+hi)/2;
                if(BulkIllustrativeCost(basePrice,startPostThreshold,mid)<=budget)lo=mid;else hi=mid;
            }
            return lo;
        }
    }
}
