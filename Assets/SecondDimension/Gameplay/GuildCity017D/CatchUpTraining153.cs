using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    [Serializable]
    public sealed class CatchUpCourse153
    {
        [JsonConstructor]
        public CatchUpCourse153(string courseId, string heroId, int targetLevel,
            long startXp, long targetXp, int credits, int enrolledOperationOrdinal,
            string excludedBattleId)
        {
            CourseId = CatchUpTraining153.Key(courseId);
            HeroId = CatchUpTraining153.Key(heroId);
            if (targetLevel < 1 || startXp < 0 || targetXp < startXp ||
                credits < 0 || credits > 5 || enrolledOperationOrdinal < 0 ||
                RecruitGrowthRules152.Threshold(targetLevel) != targetXp)
                throw new ArgumentException("Invalid saved Catch Up course.");
            TargetLevel = targetLevel; StartXp = startXp; TargetXp = targetXp;
            Credits = credits; EnrolledOperationOrdinal = enrolledOperationOrdinal;
            ExcludedBattleId = excludedBattleId ?? string.Empty;
        }
        public string CourseId { get; }
        public string HeroId { get; }
        public int TargetLevel { get; }
        public long StartXp { get; }
        public long TargetXp { get; }
        public int Credits { get; }
        public int EnrolledOperationOrdinal { get; }
        public string ExcludedBattleId { get; }
        // Equivalent to ceil(gap/5), without overflow near Int64.MaxValue.
        [JsonIgnore]
        public long XpPerVictory => (TargetXp - StartXp) / 5 +
            ((TargetXp - StartXp) % 5 == 0 ? 0 : 1);
        public CatchUpCourse153 WithCredit() => new CatchUpCourse153(CourseId, HeroId,
            TargetLevel, StartXp, TargetXp, checked(Credits + 1),
            EnrolledOperationOrdinal, ExcludedBattleId);
    }

    [Serializable]
    public sealed class CatchUpState153
    {
        public const string CurrentPolicy = "CATCHUP_ORIGIN_REFERENCE_R50_V1";
        public const string PreServiceMigration = "NATIVE_PRE_CATCHUP_153_ZERO_BASELINE";
        public const string UnknownOrigin = "ORIGIN_RECONCILIATION_REQUIRED";
        [JsonConstructor]
        public CatchUpState153(string profileId, string policy, string originProvenance,
            IReadOnlyList<CatchUpCourse153> courses)
        {
            ProfileId = CatchUpTraining153.Key(profileId);
            if (policy != CurrentPolicy || (originProvenance != PreServiceMigration &&
                originProvenance != UnknownOrigin))
                throw new ArgumentException("Unsupported Catch Up policy or migration.");
            Policy = policy; OriginProvenance = originProvenance;
            var copy = new List<CatchUpCourse153>(courses ?? Array.Empty<CatchUpCourse153>());
            if (copy.Count > 6 || copy.Any(x => x == null) ||
                copy.Select(x => x.HeroId).Distinct(StringComparer.Ordinal).Count() != copy.Count ||
                copy.Select(x => x.CourseId).Distinct(StringComparer.Ordinal).Count() != copy.Count)
                throw new ArgumentException("Catch Up requires at most six distinct heroes and courses.");
            Courses = copy.OrderBy(x => x.CourseId, StringComparer.Ordinal).ToList().AsReadOnly();
        }
        public string ProfileId { get; }
        public string Policy { get; }
        public string OriginProvenance { get; }
        public IReadOnlyList<CatchUpCourse153> Courses { get; }
        [JsonIgnore]
        public bool OriginsAvailable => OriginProvenance == PreServiceMigration;
        public CatchUpState153 WithCourses(IReadOnlyList<CatchUpCourse153> courses) =>
            new CatchUpState153(ProfileId, Policy, OriginProvenance, courses);
    }

    public sealed class CatchUpQuote153
    {
        internal CatchUpQuote153(string profile, string revision, string requestId,
            bool retiring, IReadOnlyList<string> selections, int targetLevel)
        {
            ProfileId = profile; Revision = revision; RequestId = requestId;
            Retiring = retiring; Selections = selections; TargetLevel = targetLevel;
        }
        public string ProfileId { get; }
        public string Revision { get; }
        public string RequestId { get; }
        public bool Retiring { get; }
        public IReadOnlyList<string> Selections { get; }
        public int TargetLevel { get; }
    }

    // This service only returns immutable candidates. The existing coordinator saves
    // the full candidate atomically; it must not publish a candidate before save success.
    public static class CatchUpTraining153
    {
        internal static string Key(string value) => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Stable Catch Up identity is required.") : value;

        public static string NativeClass(string value)
        {
            const string prefix = "CLASS_TEND_";
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal)
                ? "CLASS_" + value.Substring(prefix.Length) : value ?? string.Empty;
        }

        static bool Eligible(RecruitState hero) => hero != null &&
            ProtectedActorPolicy.CanUseNormalEquipment(hero);

        static void CheckBattleVitals(RecruitState hero, RecruitProgressionState progress)
        {
            if (hero == null || progress == null)
                throw new InvalidOperationException("Catch Up hero is missing.");
            checked
            {
                int hp = hero.MaximumHp + progress.MaximumHpBonus;
                int mp = hero.MaximumMp + progress.MaximumMpBonus;
                if (hp <= 0 || mp < 0)
                    throw new OverflowException("Catch Up needs a wider native battle-vitals migration.");
            }
        }

        static void SafeBoundary(CampaignState state)
        {
            var blocked = IndependentProgression159.BlockReason(state);
            if (blocked != null) throw new InvalidOperationException(blocked);
        }

        static CatchUpState153 Read(CampaignState state)
        {
            var training = state?.CatchUp153;
            if (training == null)
                throw new InvalidOperationException("Initialize verified pre-service training history first.");
            if (training.ProfileId != state.CampaignGuid)
                throw new InvalidOperationException("Training belongs to another profile.");
            return training;
        }

        // Explicit migration, never called by a UI read. Invoke only for a verified
        // native pre-service profile, then durably save through the existing authority.
        // A profile already marked unknown is deliberately not repaired by this method.
        public static Result<CampaignState> InitializeKnownPreService(CampaignState state)
        {
            try
            {
                SafeBoundary(state);
                if (state.CatchUp153 != null)
                {
                    if (!Read(state).OriginsAvailable)
                        throw new InvalidOperationException("Training origin history requires reconciliation.");
                    return Result<CampaignState>.Success(state);
                }
                // No Catch Up implementation existed in this verified native schema.
                // Paid Growth152 records retain their existing exact origin aggregate.
                var heroes = state.Guild.Recruits.Select(hero =>
                {
                    if (!Eligible(hero)) return hero;
                    var progress = hero.Progression.MigrateGrowth152(NativeClass(hero.ClassTendencyId));
                    CheckBattleVitals(hero, progress);
                    return hero.WithProgression(progress);
                }).ToArray();
                var guild = state.Guild.With(state.Guild.TreasuryXp, heroes,
                    state.Guild.Unions, state.Guild.Inventory);
                return Result<CampaignState>.Success(state.With(guild, state.OpeningFlow)
                    .WithCatchUp153(new CatchUpState153(state.CampaignGuid,
                        CatchUpState153.CurrentPolicy, CatchUpState153.PreServiceMigration,
                        Array.Empty<CatchUpCourse153>())));
            }
            catch (Exception e) when (Expected(e)) { return Failure(e); }
        }

        // For an import whose prior awards cannot be reconstructed. Do not call the
        // known-pre-service migration merely because an imported optional field is null.
        public static Result<CampaignState> MarkOriginUnavailable(CampaignState state)
        {
            try
            {
                SafeBoundary(state);
                if (state.CatchUp153 != null) Read(state);
                return Result<CampaignState>.Success(state.WithCatchUp153(new CatchUpState153(
                    state.CampaignGuid, CatchUpState153.CurrentPolicy, CatchUpState153.UnknownOrigin,
                    state.CatchUp153?.Courses ?? Array.Empty<CatchUpCourse153>())));
            }
            catch (Exception e) when (Expected(e)) { return Failure(e); }
        }

        // Nullable origin in a newly recruited legacy-version hero is known zero only
        // because this profile carries the explicit pre-service migration provenance.
        static RecruitProgressionState CoherentProgress(CatchUpState153 training, RecruitState hero)
        {
            if (!training.OriginsAvailable)
                throw new InvalidOperationException("Training origin history requires reconciliation.");
            var p = hero.Progression;
            if (p.ProgressionVersion152 == 0)
            {
                var reconciled = p.MigrateGrowth152(NativeClass(hero.ClassTendencyId));
                if (reconciled.Level != p.Level)
                    throw new InvalidOperationException("Apply already-earned banked hero levels before enrolling.");
                return reconciled;
            }
            if (!p.CatchUpOriginXp152.HasValue || p.CatchUpOriginXp152.Value < 0 ||
                p.CatchUpOriginXp152.Value > p.TotalPersonalXp)
                throw new InvalidOperationException("Training XP origins are inconsistent.");
            return p;
        }

        public static Result<int> Benchmark(CampaignState state)
        {
            try
            {
                var training = Read(state);
                if (!training.OriginsAvailable)
                    throw new InvalidOperationException("Training origin history requires reconciliation.");
                var levels = state.Guild.Recruits.Where(Eligible).Select(hero =>
                {
                    var p = CoherentProgress(training, hero);
                    return RecruitGrowthRules152.Level(checked(p.TotalPersonalXp - p.CatchUpOriginXp152.Value));
                }).OrderByDescending(level => level).Take(10).ToArray();
                if (levels.Length == 0) throw new InvalidOperationException("No eligible owned heroes.");
                return Result<int>.Success(checked((int)(levels.Sum(level => (long)level) / levels.Length)));
            }
            catch (Exception e) when (Expected(e)) { return Result<int>.Failure(e.Message); }
        }

        static string Request(CampaignState state, string revision, bool retiring, IReadOnlyList<string> ids) =>
            "CATCHUP153_" + CanonicalJson.Sha256Hex(new
            { state.CampaignGuid, Revision = revision, Retiring = retiring, Selections = ids,
                Policy = CatchUpState153.CurrentPolicy }).Substring(0, 24).ToUpperInvariant();

        public static Result<CatchUpQuote153> QuoteEnrollment(CampaignState state, IEnumerable<string> heroIds) =>
            Quote(state, heroIds, false);
        public static Result<CatchUpQuote153> QuoteRetirement(CampaignState state, IEnumerable<string> courseIds) =>
            Quote(state, courseIds, true);

        static Result<CatchUpQuote153> Quote(CampaignState state, IEnumerable<string> ids, bool retiring)
        {
            try
            {
                SafeBoundary(state); var training = Read(state);
                var selected = (ids ?? throw new ArgumentException("Choose a hero or course."))
                    .Select(Key).Take(7).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                if (selected.Length < 1 || selected.Length > 6 ||
                    selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                    throw new ArgumentException("Select up to six distinct heroes or courses.");
                int target = 0;
                if (retiring)
                {
                    if (selected.Any(id => !training.Courses.Any(course => course.CourseId == id)))
                        throw new InvalidOperationException("The selected course has changed.");
                }
                else
                {
                    if (!training.OriginsAvailable)
                        throw new InvalidOperationException("Training origin history requires reconciliation.");
                    if (!state.Guild.Development.Facilities.Any(f =>
                        f.FacilityId == "FACILITY_TRAINING_HALL" && f.Level >= 1))
                        throw new InvalidOperationException("Training Grounds level 1 is required.");
                    if (training.Courses.Count + selected.Length > 6)
                        throw new InvalidOperationException("All six training slots are occupied.");
                    var benchmark = Benchmark(state);
                    if (!benchmark.IsSuccess) throw new InvalidOperationException(string.Join(" ", benchmark.Errors));
                    target = benchmark.Value;
                    var targetXp = RecruitGrowthRules152.Threshold(target);
                    foreach (var id in selected)
                    {
                        var hero = state.Guild.Recruits.FirstOrDefault(x => x.RecruitId == id);
                        if (!Eligible(hero) || training.Courses.Any(course => course.HeroId == id))
                            throw new InvalidOperationException("Choose an eligible owned hero without an active course.");
                        if (CoherentProgress(training, hero).TotalPersonalXp >= targetXp)
                            throw new InvalidOperationException("This hero has already reached the current training target.");
                    }
                }
                var revision = CanonicalJson.Sha256Hex(state);
                return Result<CatchUpQuote153>.Success(new CatchUpQuote153(state.CampaignGuid,
                    revision, Request(state, revision, retiring, selected), retiring,
                    Array.AsReadOnly(selected), target));
            }
            catch (Exception e) when (Expected(e)) { return Result<CatchUpQuote153>.Failure(e.Message); }
        }

        // UI supplies the exact saved quote only after its explicit confirmation.
        public static Result<CampaignState> Confirm(CampaignState state, CatchUpQuote153 quote)
        {
            try
            {
                if (state == null || quote == null || quote.ProfileId != state.CampaignGuid ||
                    quote.RequestId != Request(state, quote.Revision, quote.Retiring, quote.Selections))
                    throw new InvalidOperationException("Review this training selection again.");
                // Retry precedes freshness, facility and retired-course checks.
                if (state.Guild.Development.HasAdventureAuthority(quote.RequestId))
                    return Result<CampaignState>.Success(state);
                if (CanonicalJson.Sha256Hex(state) != quote.Revision)
                    throw new InvalidOperationException("Your Guild changed. Review the training selection again.");
                var renewed = Quote(state, quote.Selections, quote.Retiring);
                if (!renewed.IsSuccess) throw new InvalidOperationException(string.Join(" ", renewed.Errors));
                if (renewed.Value.RequestId != quote.RequestId || renewed.Value.TargetLevel != quote.TargetLevel)
                    throw new InvalidOperationException("Training target changed. Review it again.");
                if (state.Guild.Development.AppliedAdventureAuthorityIds.Count >=
                    GuildDevelopmentState.AdventureAuthorityEntryLimit)
                    throw new InvalidOperationException("Reward history needs a capacity update before changing training.");
                var training = Read(state);
                var courses = training.Courses.ToList();
                var heroes = state.Guild.Recruits.ToArray();
                if (quote.Retiring) courses.RemoveAll(course => quote.Selections.Contains(course.CourseId));
                else foreach (var id in quote.Selections)
                {
                    var index = Array.FindIndex(heroes, hero => hero.RecruitId == id);
                    var hero = heroes[index];
                    var p = CoherentProgress(training, hero);
                    CheckBattleVitals(hero, p);
                    heroes[index] = hero.WithProgression(p);
                    courses.Add(new CatchUpCourse153(quote.RequestId + "_" +
                        CanonicalJson.Sha256Hex(id).Substring(0, 16).ToUpperInvariant(), id,
                        quote.TargetLevel, p.TotalPersonalXp, RecruitGrowthRules152.Threshold(quote.TargetLevel),
                        0, state.Guild.GuildCity.OperationOrdinal, state.Battle?.BattleId));
                }
                var development = state.Guild.Development.RecordAdventureAuthority(quote.RequestId);
                var guild = state.Guild.With(state.Guild.TreasuryXp, heroes,
                    state.Guild.Unions, state.Guild.Inventory, development);
                return Result<CampaignState>.Success(state.With(guild, state.OpeningFlow)
                    .WithCatchUp153(training.WithCourses(courses)));
            }
            catch (Exception e) when (Expected(e)) { return Failure(e); }
        }

        // A shared acquisition helper instead of a second XP system. This can move
        // unchanged into RecruitProgressionState if preferred. No normal XP path calls it.
        public static RecruitProgressionState GainCatchUpPersonalXp153(
            RecruitProgressionState before, long actualAmount, string classId)
        {
            if (before == null || before.ProgressionVersion152 != 1 || !before.CatchUpOriginXp152.HasValue || actualAmount <= 0)
                throw new ArgumentException("Verified Catch Up XP origin is required.");
            var p = before.GainPersonalXp(actualAmount, NativeClass(classId));
            return new RecruitProgressionState(p.Level, p.TotalPersonalXp, p.MaximumHpBonus,
                p.MaximumMpBonus, p.StrengthBonus, p.DefenseBonus, p.AgilityBonus,
                p.MagicBonus, p.WillBonus, p.LearnedArtIds, p.ArtMastery, p.UnlockedTreeIds,
                p.AscensionLevel, p.ProgressionVersion152,
                checked(before.CatchUpOriginXp152.Value + actualAmount));
        }

        // Match actual existing native encounter identity. Registry-backed battle/return
        // validation still happens in the existing caller; this does not replace it.
        static bool SupportedEncounter(CampaignState source)
        {
            var city = source.Guild.GuildCity; var encounter = city.PendingEncounter;
            if (encounter == null || encounter.BattleId != source.Battle.BattleId) return false;
            var campaign = city.Strategic017H?.Campaign019;
            var old = campaign?.ActiveOperation;
            if (old != null && old.BattleCommitted &&
                encounter.RequestId == "CAMPAIGN_ENCOUNTER_" + old.RequestId) return true;
            var playable = campaign?.Playable020;
            var tower = playable?.Progression022?.ActiveAbyssOperation;
            var world = playable?.WorldGate023?.ActiveOperation;
            bool matched = (tower != null && encounter.ExpeditionId == tower.OperationInstanceId &&
                    encounter.CanonicalSeedIdentity == tower.CanonicalSeedIdentity) ||
                (world != null && city.Expedition != null &&
                    encounter.ExpeditionId == city.Expedition.ExpeditionId &&
                    encounter.BoardId == world.BoardId &&
                    encounter.CanonicalSeedIdentity == world.CanonicalSeedIdentity);
            return matched && source.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(encounter));
        }

        public static Result<CampaignState> ApplyClaimedVictory(CampaignState source, CampaignState claimed)
        {
            try
            {
                if (source == null || claimed == null || source.CampaignGuid != claimed.CampaignGuid)
                    throw new InvalidOperationException("Catch Up settlement profile mismatch.");
                if (source.CatchUp153 == null || source.CatchUp153.Courses.Count == 0)
                    return Result<CampaignState>.Success(claimed);
                var training = Read(source); var afterTraining = Read(claimed);
                if (CanonicalJson.Sha256Hex(training) != CanonicalJson.Sha256Hex(afterTraining))
                    throw new InvalidOperationException("Training changed during ordinary reward settlement.");
                var battle = source.Battle; var reward = battle?.Reward;
                if (battle == null || reward == null || battle.Outcome != BattleOutcome.Victory ||
                    battle.Phase != BattlePhase.Resolved || reward.Claimed ||
                    source.Guild.Development.HasClaimedReward(reward.RewardId) || !SupportedEncounter(source))
                    return Result<CampaignState>.Success(claimed);
                if (reward.Outcome != BattleOutcome.Victory || claimed.Battle?.BattleId != battle.BattleId ||
                    claimed.Battle.Reward?.RewardId != reward.RewardId || !claimed.Battle.Reward.Claimed ||
                    !claimed.Guild.Development.HasClaimedReward(reward.RewardId))
                    throw new InvalidOperationException("Ordinary victory rewards must settle before Catch Up.");
                // Unknown prefix must never become a fabricated known-zero origin. We
                // preserve courses and normal rewards, with no new service grant.
                if (!training.OriginsAvailable) return Result<CampaignState>.Success(claimed);
                var heroes = claimed.Guild.Recruits.ToArray();
                var courses = training.Courses.ToArray(); bool changed = false;
                for (int i = 0; i < courses.Length; i++)
                {
                    var course = courses[i];
                    if (course.ExcludedBattleId == battle.BattleId ||
                        course.EnrolledOperationOrdinal > source.Guild.GuildCity.OperationOrdinal) continue;
                    int index = Array.FindIndex(heroes, hero => hero.RecruitId == course.HeroId);
                    if (index < 0 || !Eligible(heroes[index]))
                        throw new InvalidOperationException("A saved Catch Up trainee is missing or no longer eligible.");
                    var hero = heroes[index]; var p = CoherentProgress(training, hero);
                    if (p.TotalPersonalXp < course.StartXp)
                        throw new InvalidOperationException("Catch Up XP has moved behind its recorded starting point.");
                    // Decimal avoids multiplication overflow in this consistency check.
                    decimal priorFloor = Math.Min((decimal)course.TargetXp,
                        (decimal)course.StartXp + (decimal)course.XpPerVictory * course.Credits);
                    if (p.TotalPersonalXp < priorFloor)
                        throw new InvalidOperationException("Catch Up credit history needs reconciliation.");
                    if (p.TotalPersonalXp >= course.TargetXp) continue;
                    if (course.Credits >= 5)
                        throw new InvalidOperationException("Completed Catch Up course is below its recorded target.");
                    long amount = Math.Min(course.XpPerVictory, course.TargetXp - p.TotalPersonalXp);
                    if (amount <= 0) continue;
                    var gained = GainCatchUpPersonalXp153(p, amount, hero.ClassTendencyId);
                    CheckBattleVitals(hero, gained);
                    heroes[index] = hero.WithProgression(gained);
                    courses[i] = course.WithCredit(); changed = true;
                }
                if (!changed) return Result<CampaignState>.Success(claimed);
                var guild = claimed.Guild.With(claimed.Guild.TreasuryXp, heroes,
                    claimed.Guild.Unions, claimed.Guild.Inventory);
                return Result<CampaignState>.Success(claimed.With(guild, claimed.OpeningFlow)
                    .WithCatchUp153(training.WithCourses(courses)));
            }
            catch (Exception e) when (Expected(e)) { return Failure(e); }
        }

        static bool Expected(Exception e) => e is ArgumentException || e is InvalidOperationException || e is OverflowException;
        static Result<CampaignState> Failure(Exception e) => Result<CampaignState>.Failure("Catch Up: " + e.Message);
    }
}
