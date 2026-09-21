using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Content
{
    [Serializable]
    public sealed class StableIdRegistryDocument
    {
        [JsonProperty("freezeVersion")]
        public string FreezeVersion { get; set; }

        [JsonProperty("canonicalStableIdCount")]
        public int CanonicalStableIdCount { get; set; }

        [JsonProperty("entries")]
        public List<StableIdRegistryEntry> Entries { get; set; } = new List<StableIdRegistryEntry>();
    }

    [Serializable]
    public sealed class StableIdRegistryEntry
    {
        [JsonProperty("stable_id")]
        public string StableId { get; set; }

        [JsonProperty("canonical_source")]
        public string CanonicalSource { get; set; }

        [JsonProperty("canonical_path")]
        public string CanonicalPath { get; set; }

        [JsonProperty("occurrences")]
        public int Occurrences { get; set; }

        [JsonProperty("distinct_shapes")]
        public int DistinctShapes { get; set; }
    }

    [Serializable]
    public sealed class ShadowLedgerDocument
    {
        [JsonProperty("freezeVersion")]
        public string FreezeVersion { get; set; }

        [JsonProperty("shadowConflictCount")]
        public int ShadowConflictCount { get; set; }

        [JsonProperty("entries")]
        public List<ShadowLedgerEntry> Entries { get; set; } = new List<ShadowLedgerEntry>();
    }

    [Serializable]
    public sealed class ShadowLedgerEntry
    {
        [JsonProperty("stable_id")]
        public string StableId { get; set; }

        [JsonProperty("canonical_source")]
        public string CanonicalSource { get; set; }

        [JsonProperty("shadow_sources")]
        public List<string> ShadowSources { get; set; } = new List<string>();
    }

    [Serializable]
    public sealed class AssetPriorityDocument
    {
        [JsonProperty("counts")]
        public Dictionary<string, int> Counts { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

        [JsonProperty("entries")]
        public List<AssetPriorityEntry> Entries { get; set; } = new List<AssetPriorityEntry>();
    }

    [Serializable]
    public sealed class AssetPriorityEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("implementationFreezeTier")]
        public string ImplementationFreezeTier { get; set; }

        [JsonProperty("verticalSliceRequired")]
        public bool VerticalSliceRequired { get; set; }
    }
}

