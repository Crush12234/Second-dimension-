using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.SpecialRelic001
{
    /// <summary>
    /// Runtime-safe design authority for one additive Special Relic.  Numeric battle
    /// behavior is deliberately supplied by a separate, versioned P0 adapter because
    /// the Library catalog contains presentation intent rather than combat constants.
    /// </summary>
    [Serializable]
    public sealed class SpecialRelicRule001
    {
        [JsonConstructor]
        public SpecialRelicRule001(
            string relicId,
            string name,
            string rarity,
            string kind,
            string effect,
            string growth,
            string ultimateArt,
            string summon,
            string publicCodeEligible)
        {
            RelicId = relicId ?? string.Empty;
            Name = name ?? string.Empty;
            Rarity = rarity ?? string.Empty;
            Kind = kind ?? string.Empty;
            Effect = effect ?? string.Empty;
            Growth = growth ?? string.Empty;
            UltimateArt = ultimateArt ?? string.Empty;
            Summon = summon ?? string.Empty;
            PublicCodeEligible = publicCodeEligible ?? string.Empty;
        }

        [JsonProperty("relicId")] public string RelicId { get; }
        [JsonProperty("name")] public string Name { get; }
        [JsonProperty("rarity")] public string Rarity { get; }
        [JsonProperty("kind")] public string Kind { get; }
        [JsonProperty("effect")] public string Effect { get; }
        [JsonProperty("growth")] public string Growth { get; }
        [JsonProperty("ultimateArt")] public string UltimateArt { get; }
        [JsonProperty("summon")] public string Summon { get; }
        [JsonProperty("publicCodeEligible")] public string PublicCodeEligible { get; }

        public bool IsPublicCodeEligible =>
            StringComparer.Ordinal.Equals(PublicCodeEligible, "YES");
    }

    [Serializable]
    public sealed class SpecialRelicRewardHook001
    {
        public SpecialRelicRewardHook001(string source, string reward)
        {
            Source = source ?? string.Empty;
            Reward = reward ?? string.Empty;
        }

        public string Source { get; }
        public string Reward { get; }
    }

    /// <summary>
    /// Read-only catalog seam shared by Creator-code, inventory, progression and
    /// complete-Union-Forecast adapters.  It never grants or mutates state itself.
    /// </summary>
    public interface ISpecialRelicCatalog001
    {
        bool TryGetRelic(string relicId, out SpecialRelicRule001 relic);
        bool IsP0(string relicId);
        bool TryGetRewardHook(string source, out SpecialRelicRewardHook001 hook);

        IReadOnlyList<SpecialRelicRule001> AllRelics { get; }
        IReadOnlyList<SpecialRelicRule001> P0Relics { get; }
        IReadOnlyList<SpecialRelicRewardHook001> RewardHooks { get; }
        int RelicCount { get; }
        int P0Count { get; }
    }
}
