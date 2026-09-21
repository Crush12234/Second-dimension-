using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Rank projection from the authored Hero Master. This is deliberately an
    /// adapter value and does not replace the existing recruitment authorities.
    /// </summary>
    public enum HeroMasterRank087
    {
        B,
        A,
        S,
        SS
    }

    /// <summary>
    /// One trusted, immutable Hero Master record. Only records that pass the
    /// adapter's authoring checks are exposed through this type.
    /// </summary>
    public sealed class HeroMaster300Hero087
    {
        internal HeroMaster300Hero087(
            int rosterId,
            string stableId,
            string name,
            string race,
            string role,
            string weapon,
            string sourcePackage,
            HeroMasterRank087 rank,
            string gameEntityId,
            string generationCode,
            string artTree1,
            string artTree2,
            int hp,
            int ap,
            int strength,
            int defense,
            int agility,
            int magic,
            int resistance,
            int statBudget,
            int recruitCostXp,
            string ssGenerationCode,
            string finalArtId,
            string finalArtName,
            string finalArtType,
            string finalArtEffect,
            string finalArtUnlock)
        {
            RosterId = rosterId;
            StableId = stableId;
            Name = name;
            Race = race;
            Role = role;
            Weapon = weapon;
            SourcePackage = sourcePackage;
            Rank = rank;
            GameEntityId = gameEntityId;
            GenerationCode = generationCode;
            ArtTree1 = artTree1;
            ArtTree2 = artTree2;
            Hp = hp;
            Ap = ap;
            Strength = strength;
            Defense = defense;
            Agility = agility;
            Magic = magic;
            Resistance = resistance;
            StatBudget = statBudget;
            RecruitCostXp = recruitCostXp;
            SsGenerationCode = ssGenerationCode;
            FinalArtId = finalArtId;
            FinalArtName = finalArtName;
            FinalArtType = finalArtType;
            FinalArtEffect = finalArtEffect;
            FinalArtUnlock = finalArtUnlock;
        }

        public int RosterId { get; }
        public string StableId { get; }
        public string Name { get; }
        public string Race { get; }
        public string Role { get; }
        public string Weapon { get; }
        public string SourcePackage { get; }
        public HeroMasterRank087 Rank { get; }
        public string GameEntityId { get; }
        public string GenerationCode { get; }
        public string ArtTree1 { get; }
        public string ArtTree2 { get; }
        public int Hp { get; }
        public int Ap { get; }
        public int Strength { get; }
        public int Defense { get; }
        public int Agility { get; }
        public int Magic { get; }
        public int Resistance { get; }
        public int StatBudget { get; }
        public int RecruitCostXp { get; }
        public string SsGenerationCode { get; }
        public string FinalArtId { get; }
        public string FinalArtName { get; }
        public string FinalArtType { get; }
        public string FinalArtEffect { get; }
        public string FinalArtUnlock { get; }

        /// <summary>
        /// This is only an eligibility projection. The existing Applicant Board
        /// remains responsible for selection, timing, costs, and persistence.
        /// </summary>
        public bool IsNormalApplicantEligible => Rank != HeroMasterRank087.SS;
    }

    /// <summary>
    /// Identity-safe diagnostic for a record that cannot enter runtime adapters.
    /// Raw untrusted field values are intentionally not echoed to player-facing UI.
    /// </summary>
    public sealed class HeroMaster300Quarantine087
    {
        internal HeroMaster300Quarantine087(
            int rosterId,
            string stableId,
            IReadOnlyList<string> reasons)
        {
            RosterId = rosterId;
            StableId = stableId;
            Reasons = reasons ?? throw new ArgumentNullException(nameof(reasons));
        }

        public int RosterId { get; }
        public string StableId { get; }
        public IReadOnlyList<string> Reasons { get; }
    }

    /// <summary>
    /// Strict import boundary for the supplied HERO_MASTER_001_300 JSON shape.
    /// It exposes trusted definitions to existing systems but performs no random
    /// selection, signing, Art creation, save mutation, or combat behavior.
    /// </summary>
    public sealed class HeroMaster300Catalog087
    {
        public const int ExpectedHeroCount = 300;
        public const int ExpectedBCount = 90;
        public const int ExpectedACount = 90;
        public const int ExpectedSCount = 90;
        public const int ExpectedSsCount = 30;

        private static readonly Regex SafeAuthorityId = new Regex(
            "^[A-Z][A-Z0-9_]{2,127}$",
            RegexOptions.CultureInvariant);

        private static readonly Regex GenerationCode = new Regex(
            "^SDG-(B|A|S|SS)-([0-9]{3})-[0-9A-F]{8}$",
            RegexOptions.CultureInvariant);

        private static readonly Regex SsGenerationCode = new Regex(
            "^SD-SS-[0-9]{2}-[A-Z0-9_]+$",
            RegexOptions.CultureInvariant);

        private static readonly HashSet<string> QuarantinedAuthoringValues =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TBD",
                "UNKNOWN",
                "UNASSIGNED",
                "PLACEHOLDER",
                "N/A",
                "NONE_YET"
            };

        private readonly Dictionary<int, HeroMaster300Hero087> _acceptedByRosterId;
        private readonly Dictionary<string, HeroMaster300Hero087> _acceptedByStableId;
        private readonly Dictionary<string, HeroMaster300Hero087> _ssByCode;
        private readonly Dictionary<int, HeroMaster300Quarantine087> _quarantineByRosterId;

        private HeroMaster300Catalog087(
            IReadOnlyList<HeroMaster300Hero087> acceptedHeroes,
            IReadOnlyList<HeroMaster300Quarantine087> quarantinedHeroes)
        {
            AcceptedHeroes = acceptedHeroes;
            QuarantinedHeroes = quarantinedHeroes;
            _acceptedByRosterId = new Dictionary<int, HeroMaster300Hero087>();
            _acceptedByStableId = new Dictionary<string, HeroMaster300Hero087>(StringComparer.Ordinal);
            _ssByCode = new Dictionary<string, HeroMaster300Hero087>(StringComparer.OrdinalIgnoreCase);
            _quarantineByRosterId = new Dictionary<int, HeroMaster300Quarantine087>();

            var applicantCandidates = new List<HeroMaster300Hero087>();
            foreach (var hero in AcceptedHeroes)
            {
                _acceptedByRosterId.Add(hero.RosterId, hero);
                _acceptedByStableId.Add(hero.StableId, hero);
                if (hero.IsNormalApplicantEligible)
                    applicantCandidates.Add(hero);
                else
                    _ssByCode.Add(hero.SsGenerationCode, hero);
            }

            foreach (var record in QuarantinedHeroes)
                _quarantineByRosterId.Add(record.RosterId, record);

            NormalApplicantCandidates = applicantCandidates.AsReadOnly();
        }

        public IReadOnlyList<HeroMaster300Hero087> AcceptedHeroes { get; }
        public IReadOnlyList<HeroMaster300Quarantine087> QuarantinedHeroes { get; }
        public IReadOnlyList<HeroMaster300Hero087> NormalApplicantCandidates { get; }
        public int SourceRecordCount => ExpectedHeroCount;

        public static HeroMaster300Catalog087 FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Hero Master JSON is required.", nameof(json));

            JObject root;
            try
            {
                root = JObject.Parse(json, new JsonLoadSettings
                {
                    CommentHandling = CommentHandling.Ignore,
                    LineInfoHandling = LineInfoHandling.Ignore
                });
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("Hero Master JSON is malformed.", exception);
            }

            var declaredCount = RequiredStructuralInt(root, "count", "catalog");
            if (declaredCount != ExpectedHeroCount)
                throw Invalid("Hero Master declared count must be exactly 300.");

            var declaredRanks = root["rank_distribution"] as JObject
                ?? throw Invalid("Hero Master rank_distribution object is missing.");
            ValidateDeclaredRankCount(declaredRanks, "B", ExpectedBCount);
            ValidateDeclaredRankCount(declaredRanks, "A", ExpectedACount);
            ValidateDeclaredRankCount(declaredRanks, "S", ExpectedSCount);
            ValidateDeclaredRankCount(declaredRanks, "SS", ExpectedSsCount);

            var entries = root["heroes"] as JArray
                ?? throw Invalid("Hero Master heroes array is missing.");
            if (entries.Count != ExpectedHeroCount)
                throw Invalid("Hero Master heroes array must contain exactly 300 records.");

            var seenRosterIds = new HashSet<int>();
            var seenStableIds = new HashSet<string>(StringComparer.Ordinal);
            var seenEntityIds = new HashSet<string>(StringComparer.Ordinal);
            var seenGenerationCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenSsCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var actualRanks = NewRankCounts();
            var accepted = new List<HeroMaster300Hero087>();
            var quarantined = new List<HeroMaster300Quarantine087>();

            for (var index = 0; index < entries.Count; index++)
            {
                var item = entries[index] as JObject
                    ?? throw Invalid("Hero Master record " + index + " must be an object.");

                var rosterId = RequiredStructuralInt(item, "roster_id", "record " + index);
                if (rosterId < 1 || rosterId > ExpectedHeroCount || !seenRosterIds.Add(rosterId))
                    throw Invalid("Hero Master roster IDs must be unique values from 1 through 300.");

                var stableId = RequiredAuthorityId(item, "stable_id", rosterId);
                var gameEntityId = RequiredAuthorityId(item, "game_entity_id", rosterId);
                if (!seenStableIds.Add(stableId))
                    throw Invalid("Hero Master stable IDs must be unique.");
                if (!seenEntityIds.Add(gameEntityId))
                    throw Invalid("Hero Master game entity IDs must be unique.");

                var rankText = RequiredStructuralString(item, "rank", rosterId);
                if (!TryParseRank(rankText, out var rank))
                    throw Invalid("Hero Master rank is invalid for roster ID " + rosterId + ".");
                actualRanks[rank]++;

                var generationCode = RequiredStructuralString(item, "generation_code", rosterId);
                ValidateGenerationCode(generationCode, rankText, rosterId);
                if (!seenGenerationCodes.Add(generationCode))
                    throw Invalid("Hero Master generation codes must be unique.");

                var ssCode = RequiredDeclaredString(item, "ss_generation_code", rosterId);
                if (rank == HeroMasterRank087.SS)
                {
                    if (string.IsNullOrWhiteSpace(ssCode) || !SsGenerationCode.IsMatch(ssCode))
                        throw Invalid("Every SS Hero Master record requires a valid SS generation code.");
                    if (!seenSsCodes.Add(ssCode))
                        throw Invalid("Hero Master SS generation codes must be unique.");
                }
                else if (!string.IsNullOrWhiteSpace(ssCode))
                {
                    throw Invalid("Only SS Hero Master records may define an SS generation code.");
                }

                var reasons = new List<string>();
                var name = AuthoringString(item, "name", reasons);
                var race = AuthoringString(item, "race", reasons);
                var role = AuthoringString(item, "role", reasons);
                var weapon = AuthoringString(item, "weapon", reasons);
                var sourcePackage = AuthoringString(item, "source_package", reasons);
                var artTree1 = AuthoringTree(item, "art_tree_1", reasons);
                var artTree2 = AuthoringTree(item, "art_tree_2", reasons);

                var hp = AuthoringInt(item, "HP", reasons);
                var ap = AuthoringInt(item, "AP", reasons);
                var strength = AuthoringInt(item, "STR", reasons);
                var defense = AuthoringInt(item, "DEF", reasons);
                var agility = AuthoringInt(item, "AGI", reasons);
                var magic = AuthoringInt(item, "MAG", reasons);
                var resistance = AuthoringInt(item, "RES", reasons);
                var statBudget = AuthoringInt(item, "stat_budget", reasons);
                var recruitCostXp = AuthoringInt(item, "recruit_cost_xp", reasons);
                ValidateStats(
                    hp,
                    ap,
                    strength,
                    defense,
                    agility,
                    magic,
                    resistance,
                    statBudget,
                    recruitCostXp,
                    reasons);

                var finalArtId = DeclaredAuthoringString(item, "final_art_id", reasons);
                var finalArtName = DeclaredAuthoringString(item, "final_art_name", reasons);
                var finalArtType = DeclaredAuthoringString(item, "final_art_type", reasons);
                var finalArtEffect = DeclaredAuthoringString(item, "final_art_effect", reasons);
                var finalArtUnlock = DeclaredAuthoringString(item, "final_art_unlock", reasons);
                ValidateFinalArt(
                    rank,
                    finalArtId,
                    finalArtName,
                    finalArtType,
                    finalArtEffect,
                    finalArtUnlock,
                    reasons);

                if (reasons.Count > 0)
                {
                    quarantined.Add(new HeroMaster300Quarantine087(
                        rosterId,
                        stableId,
                        reasons.AsReadOnly()));
                    continue;
                }

                accepted.Add(new HeroMaster300Hero087(
                    rosterId,
                    stableId,
                    name,
                    race,
                    role,
                    weapon,
                    sourcePackage,
                    rank,
                    gameEntityId,
                    generationCode,
                    artTree1,
                    artTree2,
                    hp,
                    ap,
                    strength,
                    defense,
                    agility,
                    magic,
                    resistance,
                    statBudget,
                    recruitCostXp,
                    ssCode,
                    finalArtId,
                    finalArtName,
                    finalArtType,
                    finalArtEffect,
                    finalArtUnlock));
            }

            for (var rosterId = 1; rosterId <= ExpectedHeroCount; rosterId++)
                if (!seenRosterIds.Contains(rosterId))
                    throw Invalid("Hero Master roster IDs must be contiguous from 1 through 300.");

            ValidateActualRankCount(actualRanks, HeroMasterRank087.B, ExpectedBCount);
            ValidateActualRankCount(actualRanks, HeroMasterRank087.A, ExpectedACount);
            ValidateActualRankCount(actualRanks, HeroMasterRank087.S, ExpectedSCount);
            ValidateActualRankCount(actualRanks, HeroMasterRank087.SS, ExpectedSsCount);

            accepted.Sort((left, right) => left.RosterId.CompareTo(right.RosterId));
            quarantined.Sort((left, right) => left.RosterId.CompareTo(right.RosterId));
            return new HeroMaster300Catalog087(accepted.AsReadOnly(), quarantined.AsReadOnly());
        }

        public bool TryGetAcceptedHero(int rosterId, out HeroMaster300Hero087 hero) =>
            _acceptedByRosterId.TryGetValue(rosterId, out hero);

        public bool TryGetAcceptedHero(string stableId, out HeroMaster300Hero087 hero) =>
            _acceptedByStableId.TryGetValue(stableId ?? string.Empty, out hero);

        public bool TryGetQuarantine(int rosterId, out HeroMaster300Quarantine087 record) =>
            _quarantineByRosterId.TryGetValue(rosterId, out record);

        /// <summary>
        /// Looks up only trusted SS records. Quarantined SS records deliberately
        /// cannot be redeemed until their authoring data is repaired and reloaded.
        /// </summary>
        public bool TryFindSsByCode(string code, out HeroMaster300Hero087 hero)
        {
            hero = null;
            return !string.IsNullOrWhiteSpace(code)
                   && _ssByCode.TryGetValue(code.Trim(), out hero);
        }

        public bool IsNormalApplicantEligible(int rosterId) =>
            _acceptedByRosterId.TryGetValue(rosterId, out var hero)
            && hero.IsNormalApplicantEligible;

        private static Dictionary<HeroMasterRank087, int> NewRankCounts() =>
            new Dictionary<HeroMasterRank087, int>
            {
                { HeroMasterRank087.B, 0 },
                { HeroMasterRank087.A, 0 },
                { HeroMasterRank087.S, 0 },
                { HeroMasterRank087.SS, 0 }
            };

        private static void ValidateDeclaredRankCount(JObject distribution, string rank, int expected)
        {
            var actual = RequiredStructuralInt(distribution, rank, "rank_distribution");
            if (actual != expected)
                throw Invalid("Hero Master declared " + rank + " rank count must be " + expected + ".");
        }

        private static void ValidateActualRankCount(
            IReadOnlyDictionary<HeroMasterRank087, int> actual,
            HeroMasterRank087 rank,
            int expected)
        {
            if (actual[rank] != expected)
                throw Invalid("Hero Master actual " + rank + " rank count must be " + expected + ".");
        }

        private static bool TryParseRank(string value, out HeroMasterRank087 rank)
        {
            switch (value)
            {
                case "B": rank = HeroMasterRank087.B; return true;
                case "A": rank = HeroMasterRank087.A; return true;
                case "S": rank = HeroMasterRank087.S; return true;
                case "SS": rank = HeroMasterRank087.SS; return true;
                default: rank = default(HeroMasterRank087); return false;
            }
        }

        private static void ValidateGenerationCode(string code, string rank, int rosterId)
        {
            var match = GenerationCode.Match(code);
            if (!match.Success
                || !StringComparer.Ordinal.Equals(match.Groups[1].Value, rank)
                || !StringComparer.Ordinal.Equals(
                    match.Groups[2].Value,
                    rosterId.ToString("D3", CultureInfo.InvariantCulture)))
            {
                throw Invalid("Hero Master generation code does not match its rank and roster ID.");
            }
        }

        private static string RequiredAuthorityId(JObject item, string property, int rosterId)
        {
            var value = RequiredStructuralString(item, property, rosterId);
            if (!SafeAuthorityId.IsMatch(value))
                throw Invalid("Hero Master " + property + " is malformed for roster ID " + rosterId + ".");
            return value;
        }

        private static string RequiredStructuralString(JObject item, string property, int rosterId)
        {
            var token = item[property];
            if (token == null || token.Type != JTokenType.String)
                throw Invalid("Hero Master " + property + " is missing for roster ID " + rosterId + ".");
            var value = token.Value<string>()?.Trim();
            if (string.IsNullOrWhiteSpace(value))
                throw Invalid("Hero Master " + property + " is missing for roster ID " + rosterId + ".");
            return value;
        }

        private static string RequiredDeclaredString(JObject item, string property, int rosterId)
        {
            var token = item[property];
            if (token == null || token.Type != JTokenType.String)
                throw Invalid("Hero Master " + property + " must be declared as text for roster ID "
                              + rosterId + ".");
            return token.Value<string>()?.Trim() ?? string.Empty;
        }

        private static int RequiredStructuralInt(JObject item, string property, string scope)
        {
            var token = item[property];
            if (token == null || token.Type != JTokenType.Integer)
                throw Invalid("Hero Master " + property + " is missing from " + scope + ".");
            try
            {
                return token.Value<int>();
            }
            catch (Exception exception) when (exception is OverflowException || exception is FormatException)
            {
                throw Invalid("Hero Master " + property + " is outside the supported integer range.");
            }
        }

        private static string AuthoringString(JObject item, string property, ICollection<string> reasons)
        {
            var token = item[property];
            if (token == null || token.Type != JTokenType.String)
            {
                reasons.Add(property + " is missing or not text.");
                return string.Empty;
            }

            var value = token.Value<string>()?.Trim() ?? string.Empty;
            if (IsQuarantinedAuthoringValue(value))
                reasons.Add(property + " is incomplete or malformed.");
            return value;
        }

        private static string AuthoringTree(JObject item, string property, ICollection<string> reasons)
        {
            var value = AuthoringString(item, property, reasons);
            if (!string.IsNullOrWhiteSpace(value)
                && (!value.StartsWith("TREE_", StringComparison.Ordinal)
                    || !SafeAuthorityId.IsMatch(value)))
            {
                reasons.Add(property + " is not an Art-tree authority ID.");
            }
            return value;
        }

        private static string DeclaredAuthoringString(
            JObject item,
            string property,
            ICollection<string> reasons)
        {
            var token = item[property];
            if (token == null || token.Type != JTokenType.String)
            {
                reasons.Add(property + " is missing or not text.");
                return string.Empty;
            }
            return token.Value<string>()?.Trim() ?? string.Empty;
        }

        private static int AuthoringInt(JObject item, string property, ICollection<string> reasons)
        {
            var token = item[property];
            if (token == null || token.Type != JTokenType.Integer)
            {
                reasons.Add(property + " is missing or not an integer.");
                return 0;
            }

            try
            {
                return token.Value<int>();
            }
            catch (Exception exception) when (exception is OverflowException || exception is FormatException)
            {
                reasons.Add(property + " is outside the supported integer range.");
                return 0;
            }
        }

        private static void ValidateStats(
            int hp,
            int ap,
            int strength,
            int defense,
            int agility,
            int magic,
            int resistance,
            int statBudget,
            int recruitCostXp,
            ICollection<string> reasons)
        {
            if (hp <= 0) reasons.Add("HP must be positive.");
            if (ap <= 0) reasons.Add("AP must be positive.");
            if (strength < 0 || defense < 0 || agility < 0 || magic < 0 || resistance < 0)
                reasons.Add("Core stats cannot be negative.");
            if (statBudget <= 0) reasons.Add("stat_budget must be positive.");
            if ((long)strength + defense + agility + magic + resistance != statBudget)
                reasons.Add("stat_budget does not equal STR + DEF + AGI + MAG + RES.");
            if (recruitCostXp <= 0) reasons.Add("recruit_cost_xp must be positive.");
        }

        private static void ValidateFinalArt(
            HeroMasterRank087 rank,
            string id,
            string name,
            string type,
            string effect,
            string unlock,
            ICollection<string> reasons)
        {
            var values = new[] { id, name, type, effect, unlock };
            if (rank == HeroMasterRank087.SS)
            {
                for (var index = 0; index < values.Length; index++)
                    if (IsQuarantinedAuthoringValue(values[index]))
                        reasons.Add("SS final Art metadata is incomplete or malformed.");
                if (!string.IsNullOrWhiteSpace(id) && !SafeAuthorityId.IsMatch(id))
                    reasons.Add("final_art_id is not an authority ID.");
            }
            else
            {
                for (var index = 0; index < values.Length; index++)
                    if (!string.IsNullOrWhiteSpace(values[index]))
                    {
                        reasons.Add("Only SS records may bind a Final Art.");
                        break;
                    }
            }
        }

        private static bool IsQuarantinedAuthoringValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var trimmed = value.Trim();
            if (QuarantinedAuthoringValues.Contains(trimmed)) return true;
            if (trimmed.StartsWith("AUDIT_", StringComparison.OrdinalIgnoreCase)
                || trimmed.IndexOf("LIVE_REGISTRY", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            for (var index = 0; index < trimmed.Length; index++)
                if (char.IsControl(trimmed[index])) return true;
            return false;
        }

        private static InvalidDataException Invalid(string message) =>
            new InvalidDataException(message);
    }
}
