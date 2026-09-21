using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    [Serializable]
    public sealed class BattleDefenseMember163
    {
        [JsonConstructor]
        public BattleDefenseMember163(string memberId, int defense)
        {
            if (string.IsNullOrWhiteSpace(memberId)) throw new ArgumentException("DEF163_MEMBER_REQUIRED");
            if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
            MemberId = memberId; Defense = defense;
        }
        public string MemberId { get; }
        public int Defense { get; }
    }

    /// <summary>
    /// Immutable encounter-start authority. Missing on old battles means the
    /// original damage and reward rules; never reconstruct it from a loaded Guild.
    /// </summary>
    [Serializable]
    public sealed class M2BattlePolicy163
    {
        public const string Version = "M2_DEFENSE_REPLAY_ECONOMY_163_V1";
        [JsonConstructor]
        public M2BattlePolicy163(string policyVersion, IReadOnlyList<BattleDefenseMember163> members,
            long replayHpNumerator, long replayHpDenominator)
        {
            if (policyVersion != Version) throw new ArgumentException("M2_POLICY163_VERSION_INVALID");
            if (replayHpDenominator <= 0 || replayHpNumerator < replayHpDenominator)
                throw new ArgumentException("M2_POLICY163_REPLAY_RATIO_INVALID");
            var copy = (members ?? Array.Empty<BattleDefenseMember163>()).ToArray();
            if (copy.Any(m => m == null) || copy.Select(m => m.MemberId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
                throw new ArgumentException("M2_POLICY163_DEFENSE_MEMBERS_INVALID");
            PolicyVersion = policyVersion;
            Members = Array.AsReadOnly(copy.OrderBy(m => m.MemberId, StringComparer.Ordinal).ToArray());
            ReplayHpNumerator = replayHpNumerator; ReplayHpDenominator = replayHpDenominator;
        }
        public string PolicyVersion { get; }
        public IReadOnlyList<BattleDefenseMember163> Members { get; }
        public long ReplayHpNumerator { get; }
        public long ReplayHpDenominator { get; }
        public bool HasReplayBonus => ReplayHpNumerator > ReplayHpDenominator;

        public static M2BattlePolicy163 Capture(CampaignState campaign, IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> authoredEnemies, IReadOnlyList<string> routes)
        {
            var deployed = new HashSet<string>(players.SelectMany(u => u.Members).Select(m => m.MemberId), StringComparer.Ordinal);
            var members = campaign.Guild.Recruits.Where(r => deployed.Contains(r.RecruitId))
                .Select(r => new BattleDefenseMember163(r.RecruitId, r.Progression.DefenseBonus)).ToArray();
            long numerator = 1, denominator = 1;
            var replay = CampaignReplayThreat130.ParseCommittedRoutes(routes);
            if (replay != null)
            {
                // Both sides use the same committed enemy roster, independent of
                // player count/power. This includes per-member rounding, the saved
                // previous-finale floor and the existing native stat ceilings.
                var before = EnemyForceProfile094.ApplyProgression138(authoredEnemies, routes);
                var after = CampaignReplayThreat130.ApplyToEnemyUnions132(before, replay);
                denominator = before.Sum(u => u.Members.Sum(m => (long)m.MaximumHp));
                numerator = after.Sum(u => u.Members.Sum(m => (long)m.MaximumHp));
                var gcd = GreatestCommonDivisor(numerator, denominator);
                numerator /= gcd; denominator /= gcd;
            }
            return new M2BattlePolicy163(Version, members, numerator, denominator);
        }

        // DEF is physical resistance. Guard applies first; wards/barriers apply
        // afterward. Exact rational arithmetic avoids floating-point platforms.
        // At DEF 300 resistance is 50%; DEF 450+ reaches the 60% ceiling.
        public int PhysicalDamage(string targetMemberId, BattleActionKind kind, int damage)
        {
            if (damage <= 0 || (kind != BattleActionKind.Martial && kind != BattleActionKind.Tactical)) return damage;
            var defense = Members.FirstOrDefault(m => m.MemberId == targetMemberId)?.Defense ?? 0;
            if (defense == 0) return damage;
            var denominator = 300L + defense;
            var reduced = (300L * damage + denominator - 1) / denominator;
            var ceiling = (2L * damage + 4) / 5;
            return (int)Math.Max(1, Math.Max(reduced, ceiling));
        }

        public long ScaleReplayReward(long amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var scaled = ((BigInteger)amount * ReplayHpNumerator + ReplayHpDenominator - 1) / ReplayHpDenominator;
            if (scaled > long.MaxValue) throw new OverflowException("M2_REPLAY163_REWARD_CAPACITY_EXCEEDED");
            return (long)scaled;
        }
        internal M2BattlePolicy163 GameplaySnapshot() =>
            HasReplayBonus ? new M2BattlePolicy163(PolicyVersion, Members, 1, 1) : this;
        static long GreatestCommonDivisor(long a, long b)
        {
            if (a <= 0 || b <= 0) throw new InvalidOperationException("M2_REPLAY163_ENEMY_HP_REQUIRED");
            while (b != 0) { var remainder = a % b; a = b; b = remainder; }
            return a;
        }
    }
}
