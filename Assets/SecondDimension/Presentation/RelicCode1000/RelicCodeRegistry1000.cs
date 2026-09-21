using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.RelicCode1000;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Presentation.SpecialRelic001;
using UnityEngine;

namespace SecondDimension.Presentation.RelicCode1000
{
    /// <summary>
    /// Hash-only runtime registry for the additive 1,000-code relic patch. It rejects
    /// schema drift and never loads plaintext/owner material into a public build.
    /// </summary>
    public sealed class RelicCodeRegistry1000 : IRelicCodeCatalog1000
    {
        public const int ExpectedCodeCount = 1_000;
        public const int ExpectedRewardBundleCount = 64;
        public const int ExpectedDirectBundleCount = 60;
        public const int ExpectedCacheBundleCount = 4;
        public const string ManifestResourcePath =
            "SecondDimension/RelicCode1000/Data/RUNTIME_SAFE_RELIC_CODE_HASH_MANIFEST_1000_v1";
        public const string BundleResourcePath =
            "SecondDimension/RelicCode1000/Data/RELIC_CODE_REWARD_BUNDLES_64_v1";

        private const string DuplicatePolicy =
            "CONVERT_TO_RELIC_RESONANCE_AND_EVOLUTION_MATERIAL";
        private const string DenialNote =
            "Never grants Great Covenant, Margin, Author, Founder, or Kael authority.";

        private static readonly IReadOnlyDictionary<string, string> CachePools =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "RB_RELIC_CACHE_INVOCATION_EPIC", "INVOCATION_EPIC_PLUS" },
                { "RB_RELIC_CACHE_ULTIMATE_EPIC", "ULTIMATE_ART_EPIC_PLUS" },
                { "RB_RELIC_CACHE_LEGENDARY", "LEGENDARY_RELICS" },
                { "RB_RELIC_CACHE_MYTHIC_CHANCE", "MYTHIC_WEIGHTED_RELICS" }
            };

        private readonly Dictionary<string, RelicCodeRule1000> _codesByHash;
        private readonly Dictionary<string, RelicRewardBundle1000> _bundlesById;
        private readonly IReadOnlyList<RelicCodeRule1000> _allCodes;
        private readonly IReadOnlyList<RelicRewardBundle1000> _allBundles;

        private RelicCodeRegistry1000(
            IReadOnlyList<RelicCodeRule1000> codes,
            IReadOnlyList<RelicRewardBundle1000> bundles)
        {
            _allCodes = codes;
            _allBundles = bundles;
            _codesByHash = codes.ToDictionary(
                value => NormalizeHash(value.Sha256), value => value, StringComparer.Ordinal);
            _bundlesById = bundles.ToDictionary(
                value => value.RewardBundleId, value => value, StringComparer.Ordinal);
        }

        public IReadOnlyList<RelicCodeRule1000> AllCodes => _allCodes;
        public IReadOnlyList<RelicRewardBundle1000> AllRewardBundles => _allBundles;
        public int CodeCount => _allCodes.Count;
        public int RewardBundleCount => _allBundles.Count;

        public bool TryGetCodeByHash(string sha256, out RelicCodeRule1000 rule) =>
            _codesByHash.TryGetValue(NormalizeHash(sha256), out rule);

        public bool TryGetRewardBundle(string rewardBundleId, out RelicRewardBundle1000 rule) =>
            _bundlesById.TryGetValue(rewardBundleId ?? string.Empty, out rule);

        public static RelicCodeRegistry1000 Load() => Load(
            LoadAsset(ManifestResourcePath).text,
            LoadAsset(BundleResourcePath).text,
            SpecialRelicRegistry001.Load());

        public static RelicCodeRegistry1000 Load(
            string manifestJson,
            string bundleJson,
            ISpecialRelicCatalog001 relicCatalog)
        {
            if (relicCatalog == null) throw new ArgumentNullException(nameof(relicCatalog));
            var codeArray = ParseStrictArray(manifestJson, "Relic code manifest");
            var bundleArray = ParseStrictArray(bundleJson, "Relic reward bundle catalog");
            var codes = ParseCodes(codeArray);
            var bundles = ParseBundles(bundleArray);
            var registry = new RelicCodeRegistry1000(codes, bundles);
            registry.Validate(relicCatalog);
            return registry;
        }

        private static IReadOnlyList<RelicCodeRule1000> ParseCodes(JArray array)
        {
            if (array.Count != ExpectedCodeCount)
                throw new InvalidOperationException("Relic code manifest must contain exactly 1,000 entries.");
            var result = new List<RelicCodeRule1000>(array.Count);
            for (var index = 0; index < array.Count; index++)
            {
                var entry = RequireObject(array[index], "Relic code entry");
                RequireExactProperties(entry,
                    "codeId", "sha256", "rewardBundleId", "relicId", "category",
                    "exactOncePerCampaign");
                result.Add(new RelicCodeRule1000(
                    RequireString(entry, "codeId"),
                    RequireString(entry, "sha256"),
                    RequireString(entry, "rewardBundleId"),
                    RequireString(entry, "relicId", allowEmpty: true),
                    RequireString(entry, "category"),
                    RequireBoolean(entry, "exactOncePerCampaign")));
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<RelicRewardBundle1000> ParseBundles(JArray array)
        {
            if (array.Count != ExpectedRewardBundleCount)
                throw new InvalidOperationException("Relic reward catalog must contain exactly 64 entries.");
            var result = new List<RelicRewardBundle1000>(array.Count);
            for (var index = 0; index < array.Count; index++)
            {
                var entry = RequireObject(array[index], "Relic reward bundle");
                var type = RequireString(entry, "type");
                if (StringComparer.Ordinal.Equals(type, "SPECIAL_RELIC_GRANT"))
                {
                    RequireExactProperties(entry, "rewardBundleId", "type", "relicId",
                        "duplicatePolicy", "manualEquipOnly", "notes");
                    result.Add(new RelicRewardBundle1000(
                        RequireString(entry, "rewardBundleId"), type,
                        RequireString(entry, "relicId"),
                        RequireString(entry, "duplicatePolicy"),
                        RequireBoolean(entry, "manualEquipOnly"),
                        RequireString(entry, "notes"), string.Empty, false));
                }
                else if (StringComparer.Ordinal.Equals(type, "RELIC_CACHE"))
                {
                    RequireExactProperties(entry, "rewardBundleId", "type", "pool",
                        "committedOnRedeem");
                    result.Add(new RelicRewardBundle1000(
                        RequireString(entry, "rewardBundleId"), type, string.Empty,
                        string.Empty, false, string.Empty,
                        RequireString(entry, "pool"),
                        RequireBoolean(entry, "committedOnRedeem")));
                }
                else throw new InvalidOperationException("Forbidden relic reward type: " + type);
            }
            return result.AsReadOnly();
        }

        private void Validate(ISpecialRelicCatalog001 relicCatalog)
        {
            if (CodeCount != ExpectedCodeCount || _codesByHash.Count != ExpectedCodeCount)
                throw new InvalidOperationException("Relic code IDs or SHA-256 hashes are not unique.");
            if (RewardBundleCount != ExpectedRewardBundleCount ||
                _bundlesById.Count != ExpectedRewardBundleCount)
                throw new InvalidOperationException("Relic reward bundle IDs are not unique.");
            if (relicCatalog.RelicCount != ExpectedDirectBundleCount)
                throw new InvalidOperationException("Relic catalog coverage must be exactly 60.");

            var directCount = 0;
            var cacheCount = 0;
            foreach (var bundle in _allBundles)
            {
                if (bundle.IsDirect)
                {
                    directCount++;
                    if (!StringComparer.Ordinal.Equals(
                            bundle.RewardBundleId, "RB_RELIC_DIRECT_" + bundle.RelicId) ||
                        !StringComparer.Ordinal.Equals(bundle.DuplicatePolicy, DuplicatePolicy) ||
                        !bundle.ManualEquipOnly ||
                        !StringComparer.Ordinal.Equals(bundle.Notes, DenialNote) ||
                        !relicCatalog.TryGetRelic(bundle.RelicId, out var relic) ||
                        !relic.IsPublicCodeEligible)
                        throw new InvalidOperationException(
                            "Direct relic bundle policy failed: " + bundle.RewardBundleId);
                }
                else if (bundle.IsCache)
                {
                    cacheCount++;
                    if (!bundle.CommittedOnRedeem ||
                        !CachePools.TryGetValue(bundle.RewardBundleId, out var expectedPool) ||
                        !StringComparer.Ordinal.Equals(bundle.Pool, expectedPool))
                        throw new InvalidOperationException(
                            "Relic cache bundle policy failed: " + bundle.RewardBundleId);
                }
                else throw new InvalidOperationException("Forbidden relic reward bundle.");
            }
            if (directCount != ExpectedDirectBundleCount || cacheCount != ExpectedCacheBundleCount)
                throw new InvalidOperationException("Relic reward distribution must be 60 direct plus 4 cache.");

            foreach (var relic in relicCatalog.AllRelics)
                if (!_bundlesById.ContainsKey("RB_RELIC_DIRECT_" + relic.RelicId))
                    throw new InvalidOperationException("Relic has no direct reward bundle: " + relic.RelicId);

            var categories = new Dictionary<string, int>(StringComparer.Ordinal);
            var bundleUses = new Dictionary<string, int>(StringComparer.Ordinal);
            var seenCodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < _allCodes.Count; index++)
            {
                var code = _allCodes[index];
                var expectedCodeId = "RELICCODE001_" + (index + 1).ToString("0000");
                if (!StringComparer.Ordinal.Equals(code.CodeId, expectedCodeId) ||
                    !seenCodeIds.Add(code.CodeId) || !IsSha256(code.Sha256) ||
                    !code.ExactOncePerCampaign ||
                    !_bundlesById.TryGetValue(code.RewardBundleId, out var bundle))
                    throw new InvalidOperationException("Relic code law failed: " + code.CodeId);

                Increment(categories, code.Category);
                Increment(bundleUses, code.RewardBundleId);
                if (bundle.IsDirect)
                {
                    if (!StringComparer.Ordinal.Equals(code.RelicId, bundle.RelicId) ||
                        !relicCatalog.TryGetRelic(code.RelicId, out var relic) ||
                        !StringComparer.Ordinal.Equals(code.Category, CategoryFor(relic.Kind)))
                        throw new InvalidOperationException(
                            "Relic code-to-catalog mapping failed: " + code.CodeId);
                }
                else if (!StringComparer.Ordinal.Equals(code.Category, "RELIC_CACHE") ||
                         code.RelicId.Length != 0)
                    throw new InvalidOperationException("Relic cache code mapping failed: " + code.CodeId);
            }

            RequireCount(categories, "RELIC_INVOCATION", 480);
            RequireCount(categories, "RELIC_ULTIMATE_ART", 360);
            RequireCount(categories, "RELIC_MYTHIC_HYBRID", 120);
            RequireCount(categories, "RELIC_CACHE", 40);
            if (bundleUses.Count != ExpectedRewardBundleCount)
                throw new InvalidOperationException("All 64 relic reward bundles must be represented.");
            foreach (var bundle in _allBundles)
            {
                var expectedUses = bundle.IsCache ? 10 : UsesPerDirectRelic(relicCatalog, bundle.RelicId);
                RequireCount(bundleUses, bundle.RewardBundleId, expectedUses);
            }
        }

        private static int UsesPerDirectRelic(ISpecialRelicCatalog001 catalog, string relicId)
        {
            if (!catalog.TryGetRelic(relicId, out var relic)) return -1;
            switch (relic.Kind)
            {
                case "INVOCATION": return 20;
                case "ULTIMATE_ART": return 15;
                case "HYBRID": return 10;
                default: return -1;
            }
        }

        private static string CategoryFor(string kind)
        {
            switch (kind)
            {
                case "INVOCATION": return "RELIC_INVOCATION";
                case "ULTIMATE_ART": return "RELIC_ULTIMATE_ART";
                case "HYBRID": return "RELIC_MYTHIC_HYBRID";
                default: throw new InvalidOperationException("Unknown special relic kind: " + kind);
            }
        }

        private static void RequireCount(
            IReadOnlyDictionary<string, int> counts, string id, int expected)
        {
            if (!counts.TryGetValue(id, out var actual) || actual != expected)
                throw new InvalidOperationException(
                    "Relic distribution mismatch for " + id + ": " + actual + "/" + expected + ".");
        }

        private static void Increment(IDictionary<string, int> counts, string id)
        {
            counts.TryGetValue(id, out var value);
            counts[id] = value + 1;
        }

        private static JArray ParseStrictArray(string json, string label)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException(label + " is empty.");
            try
            {
                using (var stringReader = new StringReader(json))
                using (var reader = new JsonTextReader(stringReader))
                {
                    var token = JToken.ReadFrom(reader, new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                        CommentHandling = CommentHandling.Ignore,
                        LineInfoHandling = LineInfoHandling.Load
                    });
                    while (reader.Read())
                        if (reader.TokenType != JsonToken.Comment)
                            throw new InvalidOperationException(label + " contains trailing JSON.");
                    if (!(token is JArray array))
                        throw new InvalidOperationException(label + " root must be an array.");
                    return array;
                }
            }
            catch (JsonReaderException exception)
            {
                throw new InvalidOperationException(label + " contains invalid JSON.", exception);
            }
        }

        private static JObject RequireObject(JToken token, string label) =>
            token as JObject ?? throw new InvalidOperationException(label + " must be an object.");

        private static void RequireExactProperties(JObject value, params string[] expected)
        {
            var allowed = new HashSet<string>(expected, StringComparer.Ordinal);
            if (value.Properties().Count() != allowed.Count ||
                value.Properties().Any(property => !allowed.Contains(property.Name)) ||
                allowed.Any(name => !value.Properties().Any(property =>
                    StringComparer.Ordinal.Equals(property.Name, name))))
                throw new InvalidOperationException("Relic runtime JSON contains missing or unknown fields.");
        }

        private static string RequireString(JObject value, string field, bool allowEmpty = false)
        {
            var token = value[field];
            if (token == null || token.Type != JTokenType.String)
                throw new InvalidOperationException("Relic field must be a string: " + field);
            var result = (string)token;
            if (!allowEmpty && string.IsNullOrWhiteSpace(result))
                throw new InvalidOperationException("Relic field is required: " + field);
            return result ?? string.Empty;
        }

        private static bool RequireBoolean(JObject value, string field)
        {
            var token = value[field];
            if (token == null || token.Type != JTokenType.Boolean)
                throw new InvalidOperationException("Relic field must be boolean: " + field);
            return (bool)token;
        }

        private static TextAsset LoadAsset(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
                throw new InvalidOperationException("Missing runtime-safe relic resource: " + path);
            return asset;
        }

        private static string NormalizeHash(string value) =>
            (value ?? string.Empty).Trim().ToUpperInvariant();

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F'))) return false;
            }
            return true;
        }
    }
}
