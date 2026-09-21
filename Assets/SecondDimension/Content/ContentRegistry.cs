using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Content
{
    public sealed class ContentRegistry
    {
        private readonly Dictionary<string, StableIdRegistryEntry> _entries;

        private ContentRegistry(Dictionary<string, StableIdRegistryEntry> entries, ContentValidationReport report)
        {
            _entries = entries;
            Validation = report;
        }

        public ContentValidationReport Validation { get; }
        public int Count => _entries.Count;

        public bool Contains(string stableId) =>
            !string.IsNullOrWhiteSpace(stableId) && _entries.ContainsKey(stableId);

        public static ContentRegistry LoadAndValidate(string authorityRoot)
        {
            if (string.IsNullOrWhiteSpace(authorityRoot))
            {
                throw new ArgumentException("Authority root is required.", nameof(authorityRoot));
            }

            var report = new ContentValidationReport();
            var freezeRoot = Path.Combine(authorityRoot, "IMPLEMENTATION_FREEZE");
            var registryPath = Path.Combine(freezeRoot, "STABLE_ID_REGISTRY.json");
            var shadowPath = Path.Combine(freezeRoot, "ID_DEFINITION_SHADOW_LEDGER.json");
            var assetPath = Path.Combine(freezeRoot, "ASSET_IMPORT_PRIORITY_P0_P1_P2.json");

            if (!File.Exists(registryPath) || !File.Exists(shadowPath) || !File.Exists(assetPath))
            {
                report.Error($"Frozen registry inputs are missing under {freezeRoot}.");
                return new ContentRegistry(new Dictionary<string, StableIdRegistryEntry>(StringComparer.Ordinal), report);
            }

            var jsonSettings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };
            var registry = JsonConvert.DeserializeObject<StableIdRegistryDocument>(File.ReadAllText(registryPath), jsonSettings);
            var shadow = JsonConvert.DeserializeObject<ShadowLedgerDocument>(File.ReadAllText(shadowPath), jsonSettings);
            var assets = JsonConvert.DeserializeObject<AssetPriorityDocument>(File.ReadAllText(assetPath), jsonSettings);

            var byId = new Dictionary<string, StableIdRegistryEntry>(StringComparer.Ordinal);
            var sourceCache = new Dictionary<string, JToken>(StringComparer.Ordinal);

            foreach (var entry in registry?.Entries ?? new List<StableIdRegistryEntry>())
            {
                if (string.IsNullOrWhiteSpace(entry.StableId))
                {
                    report.Error("Stable-ID registry contains an empty ID.");
                    continue;
                }

                if (byId.ContainsKey(entry.StableId))
                {
                    report.Error($"Duplicate canonical stable ID: {entry.StableId}.");
                    continue;
                }
                byId.Add(entry.StableId, entry);

                ValidateCanonicalLocation(authorityRoot, entry, sourceCache, report);
            }

            report.CanonicalIdCount = byId.Count;
            report.ShadowConflictCount = shadow?.ShadowConflictCount ?? 0;
            report.P0AssetReferenceCount = CountP0Assets(assets, byId, report);

            if (registry == null)
            {
                report.Error("Stable-ID registry could not be deserialized.");
            }
            else if (registry.CanonicalStableIdCount != byId.Count)
            {
                report.Error($"Registry expected {registry.CanonicalStableIdCount} IDs but loaded {byId.Count}.");
            }

            ValidateShadowPrecedence(shadow, byId, report);
            return new ContentRegistry(byId, report);
        }

        private static void ValidateCanonicalLocation(
            string authorityRoot,
            StableIdRegistryEntry entry,
            IDictionary<string, JToken> sourceCache,
            ContentValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(entry.CanonicalSource) || string.IsNullOrWhiteSpace(entry.CanonicalPath))
            {
                report.Error($"{entry.StableId}: canonical source/path missing.");
                return;
            }

            var sourcePath = Path.GetFullPath(Path.Combine(authorityRoot, entry.CanonicalSource));
            var normalizedRoot = Path.GetFullPath(authorityRoot) + Path.DirectorySeparatorChar;
            if (!sourcePath.StartsWith(normalizedRoot, StringComparison.Ordinal))
            {
                report.Error($"{entry.StableId}: canonical source escapes the authority root.");
                return;
            }

            if (!sourceCache.TryGetValue(sourcePath, out var source))
            {
                if (!File.Exists(sourcePath))
                {
                    report.Error($"{entry.StableId}: canonical source does not exist: {entry.CanonicalSource}.");
                    return;
                }

                try
                {
                    source = JToken.Parse(File.ReadAllText(sourcePath));
                    sourceCache[sourcePath] = source;
                }
                catch (Exception exception)
                {
                    report.Error($"{entry.StableId}: cannot parse {entry.CanonicalSource}: {exception.Message}");
                    return;
                }
            }

            var token = JsonAuthorityPath.Resolve(source, entry.CanonicalPath);
            var actual = token?.Type == JTokenType.String ? token.Value<string>() : token?.ToString();
            if (!StringComparer.Ordinal.Equals(actual, entry.StableId))
            {
                report.Error(
                    $"{entry.StableId}: canonical path {entry.CanonicalPath} in {entry.CanonicalSource} resolved to '{actual ?? "<missing>"}'.");
            }
        }

        private static void ValidateShadowPrecedence(
            ShadowLedgerDocument shadow,
            IReadOnlyDictionary<string, StableIdRegistryEntry> registry,
            ContentValidationReport report)
        {
            if (shadow == null)
            {
                report.Error("Shadow ledger could not be deserialized.");
                return;
            }

            if (shadow.Entries.Count != shadow.ShadowConflictCount)
            {
                report.Error(
                    $"Shadow ledger expected {shadow.ShadowConflictCount} conflicts but contains {shadow.Entries.Count} entries.");
            }

            foreach (var entry in shadow.Entries)
            {
                if (!registry.TryGetValue(entry.StableId, out var canonical))
                {
                    report.Error($"Shadow ledger references unknown stable ID {entry.StableId}.");
                }
                else if (!StringComparer.Ordinal.Equals(canonical.CanonicalSource, entry.CanonicalSource))
                {
                    report.Error($"Shadow precedence differs for {entry.StableId}.");
                }
            }
        }

        private static int CountP0Assets(
            AssetPriorityDocument assets,
            IReadOnlyDictionary<string, StableIdRegistryEntry> registry,
            ContentValidationReport report)
        {
            if (assets == null)
            {
                report.Error("Asset priority authority could not be deserialized.");
                return 0;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var count = 0;
            foreach (var entry in assets.Entries)
            {
                if (!seen.Add(entry.Id)) report.Error($"Duplicate asset priority ID {entry.Id}.");
                if (!registry.ContainsKey(entry.Id)) report.Error($"Asset priority ID {entry.Id} is absent from the stable-ID registry.");
                if (StringComparer.Ordinal.Equals(entry.ImplementationFreezeTier, "P0")) count++;
            }

            if (assets.Counts.TryGetValue("P0", out var expected) && expected != count)
            {
                report.Error($"Asset authority expected {expected} P0 references but loaded {count}.");
            }

            return count;
        }
    }
}
