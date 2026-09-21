using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.SpecialRelic001;
using UnityEngine;

namespace SecondDimension.Presentation.SpecialRelic001
{
    /// <summary>
    /// Fail-closed reader for SPECIAL_RELIC_ULTIMATE_ART_PACK_001.  The source pack
    /// intentionally contains no AP, MP, potency, formation or mastery constants;
    /// this registry therefore exposes design records only.  Executable P0 behavior
    /// must pass through a separate explicitly authored balance adapter.
    /// </summary>
    public sealed class SpecialRelicRegistry001 : ISpecialRelicCatalog001
    {
        public const string BaseResourcePath001 = "SecondDimension/SpecialRelic001/Data/";
        public const int ExpectedRelicCount001 = 60;
        public const int ExpectedP0Count001 = 12;
        public const int ExpectedRewardHookCount001 = 12;

        private static readonly string[] RelicJsonFields001 =
        {
            "relicId", "name", "rarity", "kind", "effect", "growth",
            "ultimateArt", "summon", "publicCodeEligible"
        };

        private static readonly string[] ExpectedRewardHookSources001 =
        {
            "Story boss first clear",
            "Elite room",
            "Epic chest",
            "Tower floor 10",
            "Tower floor 25",
            "Tower floor 50",
            "Tower floor 100",
            "Master full Art branch",
            "Union Discipline rank 10",
            "Summon Resonance milestone",
            "Personal quest climax",
            "Creator giveaway code"
        };

        private readonly IReadOnlyDictionary<string, SpecialRelicRule001> _relicsById;
        private readonly HashSet<string> _p0Ids;
        private readonly IReadOnlyDictionary<string, SpecialRelicRewardHook001> _hooksBySource;

        private SpecialRelicRegistry001(
            IReadOnlyDictionary<string, SpecialRelicRule001> relicsById,
            IReadOnlyList<SpecialRelicRule001> allRelics,
            IReadOnlyList<SpecialRelicRule001> p0Relics,
            HashSet<string> p0Ids,
            IReadOnlyDictionary<string, SpecialRelicRewardHook001> hooksBySource,
            IReadOnlyList<SpecialRelicRewardHook001> rewardHooks)
        {
            _relicsById = relicsById;
            AllRelics = allRelics;
            P0Relics = p0Relics;
            _p0Ids = p0Ids;
            _hooksBySource = hooksBySource;
            RewardHooks = rewardHooks;
        }

        public IReadOnlyList<SpecialRelicRule001> AllRelics { get; }
        public IReadOnlyList<SpecialRelicRule001> P0Relics { get; }
        public IReadOnlyList<SpecialRelicRewardHook001> RewardHooks { get; }
        public int RelicCount => AllRelics.Count;
        public int P0Count => P0Relics.Count;

        public bool TryGetRelic(string relicId, out SpecialRelicRule001 relic) =>
            _relicsById.TryGetValue(relicId ?? string.Empty, out relic);

        public bool IsP0(string relicId) =>
            !string.IsNullOrWhiteSpace(relicId) && _p0Ids.Contains(relicId);

        public bool TryGetRewardHook(string source, out SpecialRelicRewardHook001 hook) =>
            _hooksBySource.TryGetValue(source ?? string.Empty, out hook);

        public static SpecialRelicRegistry001 Load()
        {
            return ParseAndValidate(
                LoadText("SPECIAL_RELIC_CATALOG_60_001"),
                LoadText("P0_SPECIAL_RELICS_12_001"),
                LoadText("SPECIAL_RELIC_REWARD_HOOKS_001"));
        }

        public static SpecialRelicRegistry001 ParseAndValidate(
            string catalogJson,
            string p0Json,
            string rewardHooksCsv)
        {
            try
            {
                var all = ParseRelicArrayStrict(catalogJson, "full catalog");
                var p0 = ParseRelicArrayStrict(p0Json, "P0 catalog");
                var hooks = ParseRewardHooksStrict(rewardHooksCsv);

                Require(all.Length == ExpectedRelicCount001, "full catalog count mismatch");
                Require(p0.Length == ExpectedP0Count001, "P0 catalog count mismatch");
                Require(hooks.Count == ExpectedRewardHookCount001, "reward-hook count mismatch");

                var relicMap = new Dictionary<string, SpecialRelicRule001>(StringComparer.Ordinal);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var relic in all)
                {
                    ValidateRelic(relic);
                    Require(!relicMap.ContainsKey(relic.RelicId), "duplicate relic ID: " + relic.RelicId);
                    relicMap.Add(relic.RelicId, relic);
                    Require(names.Add(relic.Name), "duplicate relic name: " + relic.Name);
                }

                ValidateExactCatalogShape(relicMap);

                var expectedP0Ids = ExpectedP0Ids();
                var p0Ids = new HashSet<string>(StringComparer.Ordinal);
                var canonicalP0 = new List<SpecialRelicRule001>();
                for (var index = 0; index < p0.Length; index++)
                {
                    var p0Relic = p0[index];
                    ValidateRelic(p0Relic);
                    Require(StringComparer.Ordinal.Equals(p0Relic.RelicId, expectedP0Ids[index]),
                        "P0 order/identity mismatch at index " + index);
                    Require(p0Ids.Add(p0Relic.RelicId), "duplicate P0 relic ID: " + p0Relic.RelicId);
                    Require(relicMap.TryGetValue(p0Relic.RelicId, out var canonical),
                        "P0 relic missing from full catalog: " + p0Relic.RelicId);
                    Require(EqualRecord(canonical, p0Relic),
                        "P0 relic differs from full catalog: " + p0Relic.RelicId);
                    canonicalP0.Add(canonical);
                }

                var hooksBySource = new Dictionary<string, SpecialRelicRewardHook001>(StringComparer.Ordinal);
                for (var index = 0; index < hooks.Count; index++)
                {
                    var hook = hooks[index];
                    Require(StringComparer.Ordinal.Equals(hook.Source, ExpectedRewardHookSources001[index]),
                        "reward-hook order/identity mismatch at index " + index);
                    Require(!string.IsNullOrWhiteSpace(hook.Reward),
                        "reward-hook outcome is empty: " + hook.Source);
                    Require(!hooksBySource.ContainsKey(hook.Source),
                        "duplicate reward-hook source: " + hook.Source);
                    hooksBySource.Add(hook.Source, hook);
                }

                var orderedAll = relicMap.Values.OrderBy(value => value.RelicId, StringComparer.Ordinal).ToArray();
                return new SpecialRelicRegistry001(
                    relicMap,
                    Array.AsReadOnly(orderedAll),
                    canonicalP0.AsReadOnly(),
                    p0Ids,
                    hooksBySource,
                    hooks.AsReadOnly());
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Invalid("parse failed: " + exception.Message);
            }
        }

        private static SpecialRelicRule001[] ParseRelicArrayStrict(string json, string label)
        {
            if (string.IsNullOrWhiteSpace(json)) throw Invalid(label + " is empty");
            JToken root;
            using (var textReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(textReader)
            {
                DateParseHandling = DateParseHandling.None,
                SupportMultipleContent = true,
                MaxDepth = 64
            })
            {
                root = JToken.ReadFrom(jsonReader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                    CommentHandling = CommentHandling.Load,
                    LineInfoHandling = LineInfoHandling.Load
                });
                Require(!jsonReader.Read(), label + " contains trailing JSON content");
            }
            if (!(root is JArray array)) throw Invalid(label + " root must be an array");
            var permitted = new HashSet<string>(RelicJsonFields001, StringComparer.Ordinal);
            foreach (var token in array)
            {
                if (!(token is JObject entry)) throw Invalid(label + " contains a non-object record");
                var properties = entry.Properties().ToArray();
                Require(properties.Length == RelicJsonFields001.Length,
                    label + " record field count mismatch");
                foreach (var field in RelicJsonFields001)
                {
                    Require(entry.TryGetValue(field, StringComparison.Ordinal, out var value),
                        label + " record is missing field " + field);
                    Require(value.Type == JTokenType.String,
                        label + " field must be a string: " + field);
                }
                Require(properties.All(property => permitted.Contains(property.Name)),
                    label + " contains a non-runtime field");
            }

            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                DateParseHandling = DateParseHandling.None
            };
            return root.ToObject<SpecialRelicRule001[]>(JsonSerializer.Create(settings)) ??
                   throw Invalid(label + " did not deserialize");
        }

        private static List<SpecialRelicRewardHook001> ParseRewardHooksStrict(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) throw Invalid("reward hooks CSV is empty");
            var normalized = csv.Replace("\r\n", "\n");
            Require(normalized.IndexOf('\r') < 0, "reward hooks CSV has invalid line endings");
            var rawLines = normalized.Split('\n');
            var lineCount = rawLines.Length;
            while (lineCount > 0 && rawLines[lineCount - 1].Length == 0) lineCount--;
            Require(lineCount == ExpectedRewardHookCount001 + 1, "reward hooks CSV row count mismatch");
            Require(StringComparer.Ordinal.Equals(rawLines[0], "source,reward"),
                "reward hooks CSV header mismatch");

            var result = new List<SpecialRelicRewardHook001>();
            for (var index = 1; index < lineCount; index++)
            {
                var line = rawLines[index];
                var separator = line.IndexOf(',');
                Require(separator > 0 && separator == line.LastIndexOf(','),
                    "reward hooks CSV row must contain exactly two cells");
                var source = line.Substring(0, separator).Trim();
                var reward = line.Substring(separator + 1).Trim();
                Require(source.Length > 0 && reward.Length > 0,
                    "reward hooks CSV contains an empty cell");
                result.Add(new SpecialRelicRewardHook001(source, reward));
            }
            return result;
        }

        private static void ValidateRelic(SpecialRelicRule001 relic)
        {
            Require(relic != null, "relic record is null");
            Require(!string.IsNullOrWhiteSpace(relic.RelicId) &&
                    !string.IsNullOrWhiteSpace(relic.Name) &&
                    !string.IsNullOrWhiteSpace(relic.Rarity) &&
                    !string.IsNullOrWhiteSpace(relic.Kind) &&
                    !string.IsNullOrWhiteSpace(relic.Effect) &&
                    !string.IsNullOrWhiteSpace(relic.Growth),
                "relic record has an empty authority field");
            Require(relic.IsPublicCodeEligible, "relic must be explicitly public-code eligible: " + relic.RelicId);
            Require(!HasControlCharacters(relic.RelicId) && !HasControlCharacters(relic.Name),
                "relic identity contains control characters");

            switch (relic.Kind)
            {
                case "INVOCATION":
                    Require(relic.RelicId.StartsWith("RELIC001_INV_", StringComparison.Ordinal) &&
                            relic.UltimateArt.Length == 0 && relic.Summon.Length > 0 &&
                            relic.Effect.IndexOf("Union Forecast", StringComparison.Ordinal) >= 0,
                        "Invocation relic law mismatch: " + relic.RelicId);
                    break;
                case "ULTIMATE_ART":
                    Require(relic.RelicId.StartsWith("RELIC001_ART_", StringComparison.Ordinal) &&
                            relic.UltimateArt.Length > 0 && relic.Summon.Length == 0 &&
                            relic.Effect.IndexOf("complete Union Forecast", StringComparison.Ordinal) >= 0,
                        "Ultimate Art relic law mismatch: " + relic.RelicId);
                    break;
                case "HYBRID":
                    Require(relic.RelicId.StartsWith("RELIC001_HYB_", StringComparison.Ordinal) &&
                            relic.UltimateArt.Length > 0 && relic.Summon.Length > 0 &&
                            relic.Effect.IndexOf("never", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            relic.Effect.IndexOf("true Great Covenant", StringComparison.Ordinal) >= 0,
                        "Mythic hybrid relic law mismatch: " + relic.RelicId);
                    break;
                default:
                    throw Invalid("unknown relic kind: " + relic.Kind);
            }
        }

        private static void ValidateExactCatalogShape(
            IReadOnlyDictionary<string, SpecialRelicRule001> relics)
        {
            var expectedIds = ExpectedAllIds();
            Require(relics.Count == expectedIds.Count && expectedIds.All(relics.ContainsKey),
                "relic stable-ID set mismatch");
            Require(relics.Values.Count(value => value.Kind == "INVOCATION") == 24 &&
                    relics.Values.Count(value => value.Kind == "ULTIMATE_ART") == 24 &&
                    relics.Values.Count(value => value.Kind == "HYBRID") == 12,
                "relic kind distribution mismatch");
            Require(relics.Values.Count(value => value.Rarity == "Rare") == 8 &&
                    relics.Values.Count(value => value.Rarity == "Epic") == 22 &&
                    relics.Values.Count(value => value.Rarity == "Legendary") == 18 &&
                    relics.Values.Count(value => value.Rarity == "Mythic") == 12,
                "relic rarity distribution mismatch");
            Require(relics.Values.All(value =>
                    value.Rarity == "Rare" || value.Rarity == "Epic" ||
                    value.Rarity == "Legendary" || value.Rarity == "Mythic"),
                "unknown relic rarity");
            Require(relics.Values.Where(value => value.Kind == "INVOCATION")
                    .Select(value => value.Summon).Distinct(StringComparer.Ordinal).Count() == 24,
                "Invocation summon identities must be unique");
            Require(relics.Values.Where(value => value.Kind == "ULTIMATE_ART")
                    .Select(value => value.UltimateArt).Distinct(StringComparer.Ordinal).Count() == 24,
                "Ultimate Art identities must be unique");
        }

        private static List<string> ExpectedAllIds()
        {
            var result = new List<string>();
            AddRange(result, "RELIC001_INV_", 24);
            AddRange(result, "RELIC001_ART_", 24);
            AddRange(result, "RELIC001_HYB_", 12);
            return result;
        }

        private static List<string> ExpectedP0Ids()
        {
            var result = new List<string>();
            AddRange(result, "RELIC001_INV_", 6);
            AddRange(result, "RELIC001_ART_", 6);
            return result;
        }

        private static void AddRange(ICollection<string> values, string prefix, int count)
        {
            for (var index = 1; index <= count; index++)
                values.Add(prefix + index.ToString("000"));
        }

        private static bool EqualRecord(SpecialRelicRule001 left, SpecialRelicRule001 right)
        {
            return left != null && right != null &&
                   StringComparer.Ordinal.Equals(left.RelicId, right.RelicId) &&
                   StringComparer.Ordinal.Equals(left.Name, right.Name) &&
                   StringComparer.Ordinal.Equals(left.Rarity, right.Rarity) &&
                   StringComparer.Ordinal.Equals(left.Kind, right.Kind) &&
                   StringComparer.Ordinal.Equals(left.Effect, right.Effect) &&
                   StringComparer.Ordinal.Equals(left.Growth, right.Growth) &&
                   StringComparer.Ordinal.Equals(left.UltimateArt, right.UltimateArt) &&
                   StringComparer.Ordinal.Equals(left.Summon, right.Summon) &&
                   StringComparer.Ordinal.Equals(left.PublicCodeEligible, right.PublicCodeEligible);
        }

        private static bool HasControlCharacters(string value)
        {
            if (value == null) return true;
            for (var index = 0; index < value.Length; index++)
                if (char.IsControl(value[index])) return true;
            return false;
        }

        private static string LoadText(string fileName)
        {
            var asset = Resources.Load<TextAsset>(BaseResourcePath001 + fileName);
            if (asset == null) throw Invalid("missing runtime resource: " + fileName);
            return asset.text;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw Invalid(message);
        }

        private static InvalidOperationException Invalid(string message) =>
            new InvalidOperationException("SPECIAL_RELIC001_INVALID: " + message);
    }
}
