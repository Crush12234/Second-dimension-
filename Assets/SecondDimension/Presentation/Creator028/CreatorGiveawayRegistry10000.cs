using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SecondDimension.Gameplay.Creator028;

namespace SecondDimension.Presentation.Creator028
{
    /// <summary>
    /// Runtime-safe, hash-only additive catalog. The original Creator 028 catalog remains
    /// authoritative for its 300 IDs; this registry contributes the 10,000 giveaway IDs.
    /// </summary>
    public sealed class CreatorGiveawayRegistry10000 : ICreatorGiveawayCatalog10000
    {
        public const int ExpectedCodeCount = 10_000;
        public const int ExpectedRewardBundleCount = 282;
        public const int ExpectedEquipmentTemplateCount = 180;
        public const int ExpectedCreatorPowerCodeCount = 80;
        public const string CodeSetId = "CODESET_GIVEAWAY_10000_V1";

        private readonly Dictionary<string, CreatorGiveawayCodeRule10000> _codesByHash;
        private readonly Dictionary<string, CreatorGiveawayRewardBundle10000> _bundlesById;
        private readonly Dictionary<string, CreatorGiveawayRewardBundle10000> _equipmentByTemplateId;

        private CreatorGiveawayRegistry10000(
            Dictionary<string, CreatorGiveawayCodeRule10000> codesByHash,
            Dictionary<string, CreatorGiveawayRewardBundle10000> bundlesById,
            Dictionary<string, CreatorGiveawayRewardBundle10000> equipmentByTemplateId)
        {
            _codesByHash = codesByHash;
            _bundlesById = bundlesById;
            _equipmentByTemplateId = equipmentByTemplateId;
        }

        public int CodeCount => _codesByHash.Count;
        public int RewardBundleCount => _bundlesById.Count;
        public IReadOnlyList<CreatorGiveawayCodeRule10000> AllCodes =>
            _codesByHash.Values.OrderBy(value => value.CodeId, StringComparer.Ordinal).ToList().AsReadOnly();
        public IReadOnlyList<CreatorGiveawayRewardBundle10000> AllRewardBundles =>
            _bundlesById.Values.OrderBy(value => value.RewardBundleId, StringComparer.Ordinal).ToList().AsReadOnly();

        public bool TryGetCodeByHash(string sha256, out CreatorGiveawayCodeRule10000 rule) =>
            _codesByHash.TryGetValue(NormalizeHash(sha256), out rule);

        public bool TryGetRewardBundle(string rewardBundleId, out CreatorGiveawayRewardBundle10000 rule) =>
            _bundlesById.TryGetValue(rewardBundleId ?? string.Empty, out rule);

        public bool TryGetEquipmentTemplate(string templateId, out CreatorGiveawayRewardBundle10000 rule) =>
            _equipmentByTemplateId.TryGetValue(templateId ?? string.Empty, out rule);

        public static CreatorGiveawayRegistry10000 Load()
        {
            var codeAsset = LoadAsset("SecondDimension/Creator10000/Data/RUNTIME_SAFE_CREATOR_CODE_HASH_MANIFEST_10000_v1");
            var rewardAsset = LoadAsset("SecondDimension/Creator10000/Data/CREATOR_REWARD_BUNDLES_10000_v1");
            AssertHashOnlyManifest(codeAsset.text);

            var codeRoot = JsonConvert.DeserializeObject<CodeRoot10000>(codeAsset.text);
            var rewardRoot = JsonConvert.DeserializeObject<RewardRoot10000>(rewardAsset.text);
            if (codeRoot == null || rewardRoot == null)
                throw new InvalidOperationException("Creator giveaway runtime JSON could not be read.");

            var codes = (codeRoot.Codes ?? Array.Empty<CreatorGiveawayCodeRule10000>())
                .ToDictionary(value => NormalizeHash(value.Sha256), value => value, StringComparer.Ordinal);
            var bundles = (rewardRoot.Bundles ?? Array.Empty<CreatorGiveawayRewardBundle10000>())
                .ToDictionary(value => value.RewardBundleId, value => value, StringComparer.Ordinal);
            var equipment = bundles.Values
                .Where(value => StringComparer.Ordinal.Equals(value.GrantType, "EQUIPMENT_INSTANCE"))
                .ToDictionary(value => value.PrimaryId, value => value, StringComparer.Ordinal);

            var registry = new CreatorGiveawayRegistry10000(codes, bundles, equipment);
            registry.Validate(codeRoot, rewardRoot);
            return registry;
        }

        private void Validate(CodeRoot10000 codeRoot, RewardRoot10000 rewardRoot)
        {
            if (!StringComparer.Ordinal.Equals(codeRoot.CodeSetId, CodeSetId) ||
                !StringComparer.Ordinal.Equals(rewardRoot.CodeSetId, CodeSetId) ||
                codeRoot.SchemaVersion != 1 || rewardRoot.SchemaVersion != 1)
                throw new InvalidOperationException("Creator giveaway code-set authority mismatch.");
            if (codeRoot.PublicBuildPlaintextCodes != 0 || codeRoot.GlobalOneUseCurrentlyEnforced)
                throw new InvalidOperationException("Creator giveaway offline/security policy mismatch.");
            if (codeRoot.Codes == null || codeRoot.Codes.Length != ExpectedCodeCount ||
                codeRoot.CodeCount != 0 && codeRoot.CodeCount != ExpectedCodeCount ||
                CodeCount != ExpectedCodeCount)
                throw new InvalidOperationException("Creator giveaway code count invalid.");
            if (rewardRoot.Bundles == null || rewardRoot.Bundles.Length != ExpectedRewardBundleCount ||
                rewardRoot.RewardBundleCount != ExpectedRewardBundleCount ||
                RewardBundleCount != ExpectedRewardBundleCount)
                throw new InvalidOperationException("Creator giveaway reward-bundle count invalid.");
            if (_equipmentByTemplateId.Count != ExpectedEquipmentTemplateCount)
                throw new InvalidOperationException("Creator giveaway equipment-template count invalid.");

            var codeIds = new HashSet<string>(StringComparer.Ordinal);
            var saveFlags = new HashSet<string>(StringComparer.Ordinal);
            var creatorPowerCodes = 0;
            foreach (var code in codeRoot.Codes)
            {
                if (code == null || string.IsNullOrWhiteSpace(code.CodeId) || !codeIds.Add(code.CodeId) ||
                    string.IsNullOrWhiteSpace(code.SaveFlag) || !saveFlags.Add(code.SaveFlag) ||
                    !IsSha256(code.Sha256) || !code.ExactOncePerCampaign || code.AutoEquip ||
                    !StringComparer.Ordinal.Equals(code.NormalizationVersion, "UPPER_ALNUM_V1") ||
                    !_bundlesById.TryGetValue(code.RewardBundleId ?? string.Empty, out var bundle) ||
                    bundle.CreatorPowerFlag != code.CreatorPowerFlag)
                    throw new InvalidOperationException("Creator giveaway code law failed: " + (code?.CodeId ?? "<null>"));
                if (code.CreatorPowerFlag) creatorPowerCodes++;
            }
            if (creatorPowerCodes != ExpectedCreatorPowerCodeCount)
                throw new InvalidOperationException("Creator giveaway power-code count invalid.");

            var representedFamilies = new HashSet<string>(StringComparer.Ordinal);
            var representedPowerModes = new HashSet<string>(StringComparer.Ordinal);
            var summedCodes = 0;
            foreach (var bundle in rewardRoot.Bundles)
            {
                if (bundle == null || string.IsNullOrWhiteSpace(bundle.RewardBundleId) ||
                    string.IsNullOrWhiteSpace(bundle.Label) || string.IsNullOrWhiteSpace(bundle.GrantType) ||
                    string.IsNullOrWhiteSpace(bundle.TargetMode) || string.IsNullOrWhiteSpace(bundle.PayloadJson) ||
                    bundle.CodeCount <= 0)
                    throw new InvalidOperationException("Creator giveaway reward law failed.");
                var payload = JObject.Parse(bundle.PayloadJson);
                if (!StringComparer.Ordinal.Equals((string)payload["grantType"], bundle.GrantType))
                    throw new InvalidOperationException("Creator giveaway payload grant mismatch: " + bundle.RewardBundleId);
                summedCodes = checked(summedCodes + bundle.CodeCount);
                representedPowerModes.Add(bundle.PowerMode ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(bundle.WeaponFamily)) representedFamilies.Add(bundle.WeaponFamily);
                if (bundle.CreatorPowerFlag &&
                    (!StringComparer.Ordinal.Equals(bundle.PowerMode, "FULLY_AWAKENED_CREATOR") ||
                     !StringComparer.Ordinal.Equals(bundle.StoryGate, "CREATOR_POWER_CONFIRMATION")))
                    throw new InvalidOperationException("Creator power confirmation law failed: " + bundle.RewardBundleId);
            }
            if (summedCodes != ExpectedCodeCount || representedFamilies.Count != 12 ||
                !representedPowerModes.Contains("LEGACY_AWAKENING") ||
                !representedPowerModes.Contains("FULLY_AWAKENED_CREATOR"))
                throw new InvalidOperationException("Creator giveaway reward coverage invalid.");
        }

        private static void AssertHashOnlyManifest(string json)
        {
            var root = JObject.Parse(json);
            var codes = root["codes"] as JArray ?? throw new InvalidOperationException("Creator giveaway codes array missing.");
            var permitted = new HashSet<string>(new[]
            {
                "codeId", "sha256", "rewardBundleId", "instanceSeed", "exactOncePerCampaign",
                "saveFlag", "creatorPowerFlag", "autoEquip", "normalizationVersion"
            }, StringComparer.Ordinal);
            foreach (var token in codes)
            {
                if (!(token is JObject entry) || entry.Properties().Any(property => !permitted.Contains(property.Name)))
                    throw new InvalidOperationException("Creator giveaway manifest contains a non-runtime field.");
            }
        }

        private static TextAsset LoadAsset(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Missing runtime-safe Creator giveaway resource: " + path);
            return asset;
        }

        private static string NormalizeHash(string hash) => (hash ?? string.Empty).Trim().ToUpperInvariant();

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

        [Serializable]
        private sealed class CodeRoot10000
        {
            public int SchemaVersion;
            public string CodeSetId;
            public int CodeCount;
            public bool GlobalOneUseCurrentlyEnforced;
            public int PublicBuildPlaintextCodes;
            public CreatorGiveawayCodeRule10000[] Codes;
        }

        [Serializable]
        private sealed class RewardRoot10000
        {
            public int SchemaVersion;
            public string CodeSetId;
            public int RewardBundleCount;
            public CreatorGiveawayRewardBundle10000[] Bundles;
        }
    }
}
