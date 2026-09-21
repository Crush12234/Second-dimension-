using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class LootResolver070Tests
    {
        private const string SourceRewardId = "BATTLE_REWARD_070_TEST";
        private LootResolver070 _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = LootResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
        }

        [Test]
        public void CompleteWeaponAuthorityProvidesTwelveFamiliesAndOrdinaryDrops()
        {
            Assert.That(_resolver.WeaponAuthorityCount, Is.EqualTo(264));
            Assert.That(_resolver.OrdinaryDropCount, Is.EqualTo(237));
            Assert.That(_resolver.WeaponFamilyCount, Is.EqualTo(12));
            Assert.That(_resolver.WeaponFamilyIds, Does.Contain("WEAPON_FAMILY_SWORD"));
            Assert.That(_resolver.WeaponFamilyIds, Does.Contain("WEAPON_FAMILY_BOW"));
            Assert.That(_resolver.WeaponFamilyIds, Does.Contain("WEAPON_FAMILY_HYBRID_RELIC_WEAPON"));
        }

        [Test]
        public void SameBattleReceiptAlwaysResolvesTheSameRealAuthorityWeapon()
        {
            var campaign = ClaimedCampaign(20260828L, SourceRewardId);
            var first = Resolve(campaign, SourceRewardId, "BATTLE_RESULT_HASH_A", 2);
            var reopened = Resolve(campaign, SourceRewardId, "BATTLE_RESULT_HASH_A", 2);

            Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            Assert.That(_resolver.ContainsWeapon(first.WeaponDefinitionId), Is.True);
            Assert.That(first.EquipmentReward.DefinitionId, Is.EqualTo(first.WeaponDefinitionId));
            Assert.That(first.EquipmentReward.DisplayName,
                Is.EqualTo(_resolver.Weapon(first.WeaponDefinitionId).DisplayName));
            Assert.That(first.EquipmentReward.DisplayName, Is.Not.EqualTo("First-Gate Sword"));
        }

        [Test]
        public void ApplyingReceiptTwiceAddsOneSharedInventoryItem()
        {
            var campaign = ClaimedCampaign(7107L, SourceRewardId);
            var receipt = Resolve(campaign, SourceRewardId, "BATTLE_RESULT_HASH_ONCE", 1);
            var before = campaign.Guild.Inventory.Count;

            var first = Require(_resolver.ApplyExactlyOnce(campaign, receipt));
            var reopened = Require(_resolver.ApplyExactlyOnce(first, receipt));

            Assert.That(first.Guild.Inventory.Count, Is.EqualTo(before + 1));
            Assert.That(reopened.Guild.Inventory.Count, Is.EqualTo(first.Guild.Inventory.Count));
            Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            var item = first.Guild.Inventory.Single(value => value.InstanceId == receipt.EquipmentReward.InstanceId);
            Assert.That(item.EquipmentTags, Does.Contain(receipt.ReceiptId));
            Assert.That(item.EquipmentTags, Does.Contain(receipt.SourceMarkerTag));
            Assert.That(item.EquipmentTags, Does.Contain(_resolver.Weapon(receipt.WeaponDefinitionId).WeaponFamilyId));
            Assert.That(first.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds,
                Does.Contain(receipt.ReceiptId));
            Assert.That(first.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds,
                Does.Contain(receipt.SourceMarkerTag));
        }

        [Test]
        public void EquippedReceiptItemStillBlocksASecondCopy()
        {
            var campaign = ClaimedCampaign(8012L, SourceRewardId);
            var receipt = Resolve(campaign, SourceRewardId, "BATTLE_RESULT_HASH_EQUIPPED", 1);
            campaign = Require(_resolver.ApplyExactlyOnce(campaign, receipt));
            var recruit = new RecruitState("R070", 100, 100, 10, 10).WithEquipment(
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, receipt.EquipmentReward)
                }));
            var guild = new GuildState(
                campaign.Guild.GuildId,
                campaign.Guild.TreasuryXp,
                new[] { recruit },
                campaign.Guild.Unions,
                new EquipmentItemState[0],
                campaign.Guild.Development,
                campaign.Guild.GuildCity);
            var equipped = campaign.With(guild, campaign.OpeningFlow);

            var reopened = Require(_resolver.ApplyExactlyOnce(equipped, receipt));

            Assert.That(reopened.Guild.Inventory, Is.Empty);
            Assert.That(reopened.Guild.Recruits[0].Equipment.Find(EquipmentSlotIds.MainHand).Item.InstanceId,
                Is.EqualTo(receipt.EquipmentReward.InstanceId));
            Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(CanonicalJson.Sha256Hex(equipped)));
        }

        [Test]
        public void PersistedReceiptLedgerDoesNotRegrantAnItemThatLeftInventory()
        {
            var campaign = ClaimedCampaign(8113L, SourceRewardId);
            var receipt = Resolve(campaign, SourceRewardId, "BATTLE_RESULT_HASH_DISMANTLED", 1);
            campaign = Require(_resolver.ApplyExactlyOnce(campaign, receipt));
            var withoutItem = new GuildState(
                campaign.Guild.GuildId,
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                new EquipmentItemState[0],
                campaign.Guild.Development,
                campaign.Guild.GuildCity);
            campaign = campaign.With(withoutItem, campaign.OpeningFlow);

            var reopened = Require(_resolver.ApplyExactlyOnce(campaign, receipt));

            Assert.That(reopened.Guild.Inventory, Is.Empty,
                "A claimed receipt remains spent after sale, dismantling, or another authorized item sink.");
            Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
        }

        [Test]
        public void SourceRewardCannotBeReusedForASecondReroll()
        {
            var campaign = ClaimedCampaign(9001L, SourceRewardId);
            var firstReceipt = Resolve(campaign, SourceRewardId, "BATTLE_RESULT_HASH_ONE", 1);
            campaign = Require(_resolver.ApplyExactlyOnce(campaign, firstReceipt));
            var rerolled = _resolver.ResolveReceipt(
                campaign,
                "CONTRACT_LINES_NOT_RETURNED",
                "BOARD_LINES_NOT_RETURNED",
                "ENCOUNTER_MISSING_SURVEY_TEAM",
                SourceRewardId,
                "BATTLE_RESULT_HASH_TWO",
                BattleOutcome.Victory,
                1);

            var rejected = _resolver.ApplyExactlyOnce(campaign, rerolled);

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain("LOOT070_SOURCE_REWARD_ALREADY_APPLIED"));
            Assert.That(campaign.Guild.Inventory, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnclaimedBattleRewardCannotCreateInventoryLoot()
        {
            var campaign = CampaignFactory.CreateM0Proof(117L);
            var receipt = Resolve(campaign, "UNCLAIMED_REWARD", "BATTLE_RESULT_HASH_UNCLAIMED", 1);

            var rejected = _resolver.ApplyExactlyOnce(campaign, receipt);

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain("LOOT070_CLAIM_BATTLE_REWARD_FIRST"));
            Assert.That(campaign.Guild.Inventory, Is.Empty);
        }

        [Test]
        public void DeterministicCampaignsCanReachManyWeaponFamilies()
        {
            var families = new HashSet<string>();
            for (var seed = 1L; seed <= 96L; seed++)
            {
                var source = SourceRewardId + "_" + seed;
                var campaign = ClaimedCampaign(seed, source);
                var receipt = Resolve(campaign, source, "BATTLE_RESULT_" + seed, 1);
                families.Add(_resolver.Weapon(receipt.WeaponDefinitionId).WeaponFamilyId);
            }

            Assert.That(families.Count, Is.GreaterThanOrEqualTo(8),
                "The shared inventory reward pool must visibly use the 264-weapon authority.");
        }

        private LootReceipt070 Resolve(
            CampaignState campaign,
            string sourceRewardId,
            string battleResultHash,
            int missionRank)
        {
            return _resolver.ResolveReceipt(
                campaign,
                "CONTRACT_BELL_BENEATH_GATE",
                "BOARD_BELL_BENEATH_GATE_069",
                "ENCOUNTER_GATE_GNAWER_RESCUE",
                sourceRewardId,
                battleResultHash,
                BattleOutcome.Victory,
                missionRank);
        }

        private static CampaignState ClaimedCampaign(long seed, string sourceRewardId)
        {
            var campaign = CampaignFactory.CreateM0Proof(seed);
            var development = campaign.Guild.Development.RecordBattleReward(sourceRewardId, 1, 1);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState Require(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
