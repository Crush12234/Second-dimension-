using System;
using System.Collections.Generic;

namespace SecondDimension.Gameplay.RelicCode1000
{
    [Serializable]
    public sealed class RelicCodeRule1000
    {
        public RelicCodeRule1000(
            string codeId,
            string sha256,
            string rewardBundleId,
            string relicId,
            string category,
            bool exactOncePerCampaign)
        {
            CodeId = Require(codeId, nameof(codeId));
            Sha256 = Require(sha256, nameof(sha256));
            RewardBundleId = Require(rewardBundleId, nameof(rewardBundleId));
            RelicId = relicId ?? string.Empty;
            Category = Require(category, nameof(category));
            ExactOncePerCampaign = exactOncePerCampaign;
        }

        public string CodeId { get; }
        public string Sha256 { get; }
        public string RewardBundleId { get; }
        public string RelicId { get; }
        public string Category { get; }
        public bool ExactOncePerCampaign { get; }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    [Serializable]
    public sealed class RelicRewardBundle1000
    {
        public RelicRewardBundle1000(
            string rewardBundleId,
            string type,
            string relicId,
            string duplicatePolicy,
            bool manualEquipOnly,
            string notes,
            string pool,
            bool committedOnRedeem)
        {
            RewardBundleId = Require(rewardBundleId, nameof(rewardBundleId));
            Type = Require(type, nameof(type));
            RelicId = relicId ?? string.Empty;
            DuplicatePolicy = duplicatePolicy ?? string.Empty;
            ManualEquipOnly = manualEquipOnly;
            Notes = notes ?? string.Empty;
            Pool = pool ?? string.Empty;
            CommittedOnRedeem = committedOnRedeem;
        }

        public string RewardBundleId { get; }
        public string Type { get; }
        public string RelicId { get; }
        public string DuplicatePolicy { get; }
        public bool ManualEquipOnly { get; }
        public string Notes { get; }
        public string Pool { get; }
        public bool CommittedOnRedeem { get; }
        public bool IsDirect => StringComparer.Ordinal.Equals(Type, "SPECIAL_RELIC_GRANT");
        public bool IsCache => StringComparer.Ordinal.Equals(Type, "RELIC_CACHE");

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", parameter)
                : value;
    }

    public interface IRelicCodeCatalog1000
    {
        bool TryGetCodeByHash(string sha256, out RelicCodeRule1000 rule);
        bool TryGetRewardBundle(string rewardBundleId, out RelicRewardBundle1000 rule);
        IReadOnlyList<RelicCodeRule1000> AllCodes { get; }
        IReadOnlyList<RelicRewardBundle1000> AllRewardBundles { get; }
        int CodeCount { get; }
        int RewardBundleCount { get; }
    }
}
