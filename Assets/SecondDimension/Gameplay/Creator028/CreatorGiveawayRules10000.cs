using System;

namespace SecondDimension.Gameplay.Creator028
{
    [Serializable]
    public sealed class CreatorGiveawayCodeRule10000
    {
        public string CodeId;
        public string Sha256;
        public string RewardBundleId;
        public string InstanceSeed;
        public bool ExactOncePerCampaign;
        public string SaveFlag;
        public bool CreatorPowerFlag;
        public bool AutoEquip;
        public string NormalizationVersion;
    }

    [Serializable]
    public sealed class CreatorGiveawayRewardBundle10000
    {
        public string RewardBundleId;
        public string Category;
        public string Label;
        public string Rarity;
        public string GrantType;
        public string TargetMode;
        public string PrimaryId;
        public int AmountOrQuantity;
        public string WeaponFamily;
        public string Pattern;
        public string PowerMode;
        public bool CreatorPowerFlag;
        public string StoryGate;
        public bool CodeOnly;
        public string NaturalAcquisition;
        public string AuthorityReference;
        public string PayloadJson;
        public int CodeCount;
    }

    public interface ICreatorGiveawayCatalog10000
    {
        bool TryGetCodeByHash(string sha256, out CreatorGiveawayCodeRule10000 rule);
        bool TryGetRewardBundle(string rewardBundleId, out CreatorGiveawayRewardBundle10000 rule);
        bool TryGetEquipmentTemplate(string templateId, out CreatorGiveawayRewardBundle10000 rule);
        int CodeCount { get; }
        int RewardBundleCount { get; }
    }
}
