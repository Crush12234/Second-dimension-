using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.GuildCity017G;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CreatorGiveaway10000EditModeTests
    {
        private const string RecruitId = "RECRUIT_CREATOR10000_TEST";

        [Test]
        public void RuntimeCatalogHasExactCountsAndRewardCoverage()
        {
            var registry = CreatorGiveawayRegistry10000.Load();

            Assert.That(registry.CodeCount, Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedCodeCount));
            Assert.That(registry.RewardBundleCount,
                Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedRewardBundleCount));
            Assert.That(registry.AllRewardBundles.Count,
                Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedRewardBundleCount));
            Assert.That(registry.AllRewardBundles.Count(value =>
                    StringComparer.Ordinal.Equals(value.GrantType, "EQUIPMENT_INSTANCE")),
                Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedEquipmentTemplateCount));
            Assert.That(registry.AllRewardBundles.Sum(value => value.CodeCount),
                Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedCodeCount));
            Assert.That(registry.AllRewardBundles.Where(value => value.CreatorPowerFlag)
                    .Sum(value => value.CodeCount),
                Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedCreatorPowerCodeCount));
            Assert.That(registry.AllRewardBundles.Where(value =>
                        StringComparer.Ordinal.Equals(value.GrantType, "EQUIPMENT_INSTANCE"))
                    .Select(value => value.WeaponFamily).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(12));
        }

        [Test]
        public void RuntimeManifestIsHashOnlyAndEnforcesOfflineSecurityPolicy()
        {
            var asset = Resources.Load<TextAsset>(
                "SecondDimension/Creator10000/Data/RUNTIME_SAFE_CREATOR_CODE_HASH_MANIFEST_10000_v1");
            Assert.That(asset, Is.Not.Null);
            var root = JObject.Parse(asset.text);
            var codes = (JArray)root["codes"];
            var allowedFields = new HashSet<string>(new[]
            {
                "codeId", "sha256", "rewardBundleId", "instanceSeed", "exactOncePerCampaign",
                "saveFlag", "creatorPowerFlag", "autoEquip", "normalizationVersion"
            }, StringComparer.Ordinal);

            Assert.That(root.Value<int>("schemaVersion"), Is.EqualTo(1));
            Assert.That(root.Value<string>("codeSetId"),
                Is.EqualTo(CreatorGiveawayRegistry10000.CodeSetId));
            Assert.That(root.Value<int>("publicBuildPlaintextCodes"), Is.Zero);
            Assert.That(root.Value<bool>("globalOneUseCurrentlyEnforced"), Is.False);
            Assert.That(root.Value<string>("redemptionScope"),
                Is.EqualTo("EXACT_ONCE_PER_CAMPAIGN_OFFLINE"));
            Assert.That(codes, Has.Count.EqualTo(CreatorGiveawayRegistry10000.ExpectedCodeCount));
            Assert.That(asset.text, Does.Not.Contain("\"code\":"));
            Assert.That(asset.text, Does.Not.Contain("plaintextCode"));

            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var codeIds = new HashSet<string>(StringComparer.Ordinal);
            var saveFlags = new HashSet<string>(StringComparer.Ordinal);
            var creatorPowerCodes = 0;
            foreach (var token in codes.OfType<JObject>())
            {
                Assert.That(token.Properties().Select(property => property.Name),
                    Is.EquivalentTo(allowedFields), token.Value<string>("codeId"));
                Assert.That(IsSha256(token.Value<string>("sha256")), Is.True,
                    token.Value<string>("codeId"));
                Assert.That(hashes.Add(token.Value<string>("sha256")), Is.True,
                    "Duplicate hash: " + token.Value<string>("codeId"));
                Assert.That(codeIds.Add(token.Value<string>("codeId")), Is.True);
                Assert.That(saveFlags.Add(token.Value<string>("saveFlag")), Is.True);
                Assert.That(token.Value<bool>("exactOncePerCampaign"), Is.True);
                Assert.That(token.Value<bool>("autoEquip"), Is.False);
                Assert.That(token.Value<string>("normalizationVersion"),
                    Is.EqualTo("UPPER_ALNUM_V1"));
                if (token.Value<bool>("creatorPowerFlag")) creatorPowerCodes++;
            }
            Assert.That(creatorPowerCodes,
                Is.EqualTo(CreatorGiveawayRegistry10000.ExpectedCreatorPowerCodeCount));
        }

        [TestCase(" alpha-beta 42 ", "ALPHABETA42")]
        [TestCase("CrEaToR_omega!", "CREATOROMEGA")]
        [TestCase("A.B/C\\D-09", "ABCD09")]
        public void NormalizationUsesUppercaseAsciiAlphanumericOnly(string raw, string expected)
        {
            Assert.That(CreatorGiveawayCommandService10000.NormalizeCode10000(raw), Is.EqualTo(expected));
            Assert.That(CreatorGiveawayCommandService10000.HashCode10000(raw),
                Is.EqualTo(CreatorGiveawayCommandService10000.HashCode10000(expected)));
            Assert.That(CreatorGiveawayCommandService10000.HashCode10000(raw), Has.Length.EqualTo(64));
            Assert.That(IsSha256(CreatorGiveawayCommandService10000.HashCode10000(raw)), Is.True);
        }

        [Test]
        public void PersonalXpGrantTargetsRecruitAndRecordsGrowthLedger()
        {
            var catalog = new SyntheticCatalog().Add(
                "synthetic-personal-xp",
                "SYNTH_CODE_PERSONAL_XP",
                XpBundle("SYNTH_BUNDLE_PERSONAL_XP", "PERSONAL_XP", 450,
                    "PLAYER_SELECT_CHARACTER"));
            var source = CampaignWithRecruit(10001);

            var updated = RequireSuccess(new CreatorGiveawayCommandService10000().RedeemCode(
                source, catalog, " SYNTHETIC personal-xp ", RecruitId, false));
            var recruit = updated.Guild.Recruits.Single(value => value.RecruitId == RecruitId);
            var access = Access(updated);

            Assert.That(recruit.Progression.TotalPersonalXp, Is.EqualTo(450));
            Assert.That(access.RedeemedCodeIds, Is.EquivalentTo(new[] { "SYNTH_CODE_PERSONAL_XP" }));
            Assert.That(access.AppliedReceiptIds, Has.Count.EqualTo(1));
            Assert.That(access.GrowthAllocations10000, Has.Count.EqualTo(1));
            Assert.That(access.GrowthAllocations10000[0].XpType, Is.EqualTo("PERSONAL_XP"));
            Assert.That(access.GrowthAllocations10000[0].TargetId, Is.EqualTo(RecruitId));
            Assert.That(access.GrowthAllocations10000[0].Amount, Is.EqualTo(450));
            Assert.That(access.GrowthAllocations10000[0].ReceiptId,
                Is.EqualTo(access.AppliedReceiptIds.Single()));
        }

        [Test]
        public void PrepItemGrantIsExactOnceAndDuplicateRedemptionIsNoOp()
        {
            var catalog = new SyntheticCatalog().Add(
                "synthetic-prep-item",
                "SYNTH_CODE_PREP",
                ItemBundle("SYNTH_BUNDLE_PREP", "GROWTH_BOOST_ITEM", "PREP_FORTUNE_LANTERN", 2));
            var service = new CreatorGiveawayCommandService10000();
            const string adventureAuthority = "ADVAUTH084_CREATOR_PRESERVATION";
            var seeded = CampaignFactory.CreateM0Proof(10002);
            var seededDevelopment = seeded.Guild.Development
                .RecordAdventureAuthority(adventureAuthority);
            seeded = seeded.With(seeded.Guild.With(seeded.Guild.TreasuryXp,
                seeded.Guild.Recruits, seeded.Guild.Unions, seeded.Guild.Inventory,
                seededDevelopment), seeded.OpeningFlow);
            var first = RequireSuccess(service.RedeemCode(
                seeded, catalog, "synthetic-prep-item", string.Empty, false));
            var beforeDuplicate = CanonicalJson.Serialize(first);
            var second = RequireSuccess(service.RedeemCode(
                first, catalog, "SYNTHETIC PREP ITEM", string.Empty, false));

            Assert.That(first.Guild.GuildCity.Materials.Single(value =>
                value.MaterialId == "PREP_FORTUNE_LANTERN").Amount, Is.EqualTo(2));
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(beforeDuplicate));
            Assert.That(Access(second).RedeemedCodeIds, Has.Count.EqualTo(1));
            Assert.That(Access(second).AppliedReceiptIds, Has.Count.EqualTo(1));
            Assert.That(Access(second).GrowthAllocations10000, Is.Empty);
            Assert.That(first.Guild.Development.HasAdventureAuthority(
                adventureAuthority), Is.True);
            Assert.That(second.Guild.Development.HasAdventureAuthority(
                adventureAuthority), Is.True);
        }

        [Test]
        public void GuildDevelopmentMutationsPreserveAdventureAuthorityLedger()
        {
            const string adventureAuthority = "ADVAUTH084_DEVELOPMENT_PRESERVATION";
            var development = GuildDevelopmentState.Default()
                .RecordAdventureAuthority(adventureAuthority)
                .RecordBattleReward("BATTLE_REWARD084_PRESERVE", 20, 20);
            Assert.That(development.HasAdventureAuthority(adventureAuthority), Is.True);

            development = development.SpendHallEnhancementXp(5);
            Assert.That(development.HasAdventureAuthority(adventureAuthority), Is.True);

            development = development.SetFacilityLevel(
                "FACILITY_TRAINING_HALL", 1, 10);
            Assert.That(development.HasAdventureAuthority(adventureAuthority), Is.True);
            Assert.That(development.AppliedAdventureAuthorityIds,
                Is.EqualTo(new[] { adventureAuthority }));
        }

        [Test, Timeout(120000)]
        public void AdventureAuthorityLedgerAcceptsItsFinalEntryThenFailsClosedAtCap()
        {
            var source = GuildDevelopmentState.Default();
            var ids = Enumerable.Range(0,
                    GuildDevelopmentState.AdventureAuthorityEntryLimit - 1)
                .Select(value => "ADVAUTH084_CAP_" + value.ToString("D6"))
                .ToArray();
            var nearCap = new GuildDevelopmentState(source.HallStageIndex,
                source.HallStageId, source.HallEnhancementXp,
                source.LifetimeTreasuryXpEarned, source.Facilities,
                source.ClaimedBattleRewardIds, ids);
            const string finalId = "ADVAUTH084_CAP_FINAL";
            Assert.That(nearCap.CanRecordAdventureAuthority(finalId), Is.True);

            var atCap = nearCap.RecordAdventureAuthority(finalId);
            Assert.That(atCap.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(GuildDevelopmentState.AdventureAuthorityEntryLimit));
            Assert.That(atCap.CanRecordAdventureAuthority("ADVAUTH084_CAP_OVERFLOW"),
                Is.False);
            Assert.That(atCap.RecordAdventureAuthority(finalId), Is.SameAs(atCap),
                "Idempotent replay of an existing authority remains safe at the cap.");
            Assert.Throws<InvalidOperationException>(() =>
                atCap.RecordAdventureAuthority("ADVAUTH084_CAP_OVERFLOW"));
            Assert.That(atCap.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(GuildDevelopmentState.AdventureAuthorityEntryLimit));
        }

        [Test]
        public void EquipmentGrantCreatesDeterministicInventoryItemAndNeverAutoEquips()
        {
            var equipment = EquipmentBundle(
                "SYNTH_BUNDLE_EQUIPMENT", "SYNTH_WF01_BALANCED", false, "STANDARD_SAFE");
            var catalog = new SyntheticCatalog().Add(
                "synthetic-equipment",
                "SYNTH_CODE_EQUIPMENT",
                equipment);

            var updated = RequireSuccess(new CreatorGiveawayCommandService10000().RedeemCode(
                CampaignWithRecruit(10003), catalog, "synthetic-equipment", string.Empty, false));
            var item = updated.Guild.Inventory.Single();

            Assert.That(item.InstanceId, Does.StartWith("CRITEM10000_"));
            Assert.That(item.DefinitionId, Is.EqualTo("SYNTH_WF01_BALANCED"));
            Assert.That(item.EquipmentTags, Contains.Item("CREATOR_GIVEAWAY"));
            Assert.That(item.EquipmentTags, Contains.Item("WF01_SWORD"));
            Assert.That(item.EquipmentTags, Does.Not.Contain("CREATOR_OMEGA"));
            Assert.That(updated.Guild.Recruits.Single().Equipment.Assignments, Is.Empty);
        }

        [Test]
        public void OmegaRequiresConfirmationMarksCampaignAndRemainsUnequipped()
        {
            var omega = EquipmentBundle(
                "SYNTH_BUNDLE_OMEGA", "SYNTH_WF12_OMEGA", true, "FULLY_AWAKENED_CREATOR",
                "WF12_HYBRID_RELIC", "OMEGA");
            var catalog = new SyntheticCatalog().Add(
                "synthetic-omega",
                "SYNTH_CODE_OMEGA",
                omega);
            var source = CampaignWithRecruit(10004);
            var service = new CreatorGiveawayCommandService10000();

            var denied = service.RedeemCode(
                source, catalog, "synthetic-omega", string.Empty, false);
            Assert.That(denied.IsSuccess, Is.False);
            Assert.That(denied.Errors, Contains.Item("CREATOR10000_POWER_CONFIRMATION_REQUIRED"));
            Assert.That(source.Guild.Inventory, Is.Empty);
            Assert.That(Access(source).UnlockedContentIds,
                Does.Not.Contain(CreatorGiveawayCommandService10000.CreatorPowerUsedFlag));

            var accepted = RequireSuccess(service.RedeemCode(
                source, catalog, "synthetic-omega", string.Empty, true));
            var item = accepted.Guild.Inventory.Single();
            Assert.That(Access(accepted).UnlockedContentIds,
                Contains.Item(CreatorGiveawayCommandService10000.CreatorPowerUsedFlag));
            Assert.That(item.EquipmentTags, Contains.Item("CREATOR_OMEGA"));
            Assert.That(item.EquipmentTags, Contains.Item("NONCANON_SANDBOX"));
            Assert.That(item.EquipmentTags, Contains.Item("PVP_INELIGIBLE"));
            Assert.That(accepted.Guild.Recruits.Single().Equipment.Assignments, Is.Empty);
        }

        [Test]
        public void MultiGrantCommitsAllRewardsUnderOneReceipt()
        {
            var payload = new JObject
            {
                ["grantType"] = "MULTI_GRANT",
                ["grants"] = new JArray(
                    new JObject
                    {
                        ["grantType"] = "XP_VOUCHER",
                        ["xpType"] = "GUILD_TREASURY_XP",
                        ["amount"] = 1250,
                        ["targetMode"] = "AUTOMATIC"
                    },
                    new JObject
                    {
                        ["grantType"] = "INVENTORY_ITEM",
                        ["itemId"] = "PREP_CACHE_KEY",
                        ["quantity"] = 3,
                        ["targetMode"] = "AUTOMATIC"
                    })
            };
            var catalog = new SyntheticCatalog().Add(
                "synthetic-multi",
                "SYNTH_CODE_MULTI",
                Bundle("SYNTH_BUNDLE_MULTI", "MULTI_GRANT", "AUTOMATIC", payload));

            var updated = RequireSuccess(new CreatorGiveawayCommandService10000().RedeemCode(
                CampaignFactory.CreateM0Proof(10005), catalog, "synthetic-multi", string.Empty, false));
            var access = Access(updated);

            Assert.That(updated.Guild.TreasuryXp, Is.EqualTo(1250));
            Assert.That(updated.Guild.Development.LifetimeTreasuryXpEarned, Is.EqualTo(1250));
            Assert.That(updated.Guild.GuildCity.Materials.Single(value =>
                value.MaterialId == "PREP_CACHE_KEY").Amount, Is.EqualTo(3));
            Assert.That(access.AppliedReceiptIds, Has.Count.EqualTo(1));
            Assert.That(access.GrowthAllocations10000, Has.Count.EqualTo(1));
            Assert.That(access.GrowthAllocations10000[0].ReceiptId,
                Is.EqualTo(access.AppliedReceiptIds.Single()));
            Assert.That(access.GrowthAllocations10000[0].TargetId,
                Is.EqualTo("GLOBAL:GUILD_TREASURY_XP"));
        }

        [TestCase("GUILD_TREASURY_XP")]
        [TestCase("HALL_ENHANCEMENT_XP")]
        public void GlobalDevelopmentXpKeepsBattleRewardAndOpeningCoachEvidenceUnchanged(
            string xpType)
        {
            var catalog = new SyntheticCatalog().Add(
                "synthetic-development-" + xpType,
                "SYNTH_CODE_" + xpType,
                XpBundle("SYNTH_BUNDLE_" + xpType, xpType, 375,
                    xpType == "GUILD_TREASURY_XP" ? "GLOBAL_GUILD" : "GLOBAL_HALL"));
            var source = CampaignFactory.CreateM0Proof(10008);
            var beforeBattleReceipts = source.Guild.Development.ClaimedBattleRewardIds.ToArray();
            var service = new CreatorGiveawayCommandService10000();

            var updated = RequireSuccess(service.RedeemCode(
                source, catalog, "synthetic-development-" + xpType, string.Empty, false));
            var duplicate = RequireSuccess(service.RedeemCode(
                updated, catalog, "synthetic-development-" + xpType, string.Empty, false));
            var access = Access(updated);
            var coach = GuildCityOpeningCoach017G.Evaluate(new GuildCityOpeningSnapshot017G
            {
                ClaimedBattleRewardCount = updated.Guild.Development.ClaimedBattleRewardIds.Count
            });

            Assert.That(updated.Guild.Development.ClaimedBattleRewardIds,
                Is.EqualTo(beforeBattleReceipts),
                "Creator development XP must not impersonate a claimed battle reward.");
            Assert.That(access.AppliedReceiptIds, Has.Count.EqualTo(1));
            Assert.That(access.RedeemedCodeIds, Has.Count.EqualTo(1));
            Assert.That(CanonicalJson.Serialize(duplicate),
                Is.EqualTo(CanonicalJson.Serialize(updated)),
                "The dedicated Creator ledger must still enforce exact-once redemption.");
            Assert.That(coach.Single(value => value.Id == "GUIDE_ENTER_BATTLE").Completed,
                Is.False);
            Assert.That(coach.Single(value => value.Id == "GUIDE_CLAIM_REWARD").Completed,
                Is.False);
        }

        [Test]
        public void SixtyMembersWithTwoArtTargetsKeepAllOneHundredTwentyPagedOptions()
        {
            var options = new List<CreatorRewardTargetOption028>();
            for (var member = 1; member <= 60; member++)
            for (var tree = 1; tree <= 2; tree++)
                options.Add(new CreatorRewardTargetOption028
                {
                    TargetId = "MEMBER_" + member.ToString("00") + "||ART_" + tree,
                    DisplayName = "Member " + member,
                    Detail = "Art path " + tree
                });

            var preserved = CreatorRewardTargetPaging10000.PreserveAll(options);
            var coordinatorSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "Creator028",
                "M1RuntimeCoordinator.Creator028.cs"));

            Assert.That(preserved, Has.Count.EqualTo(120));
            Assert.That(preserved.Select(value => value.TargetId).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(120));
            Assert.That(preserved.Last().TargetId, Is.EqualTo("MEMBER_60||ART_2"));
            Assert.That(CreatorRewardTargetPaging10000.PageSize, Is.EqualTo(8));
            Assert.That(CreatorRewardTargetPaging10000.PageCount(preserved.Count), Is.EqualTo(15));
            Assert.That(coordinatorSource,
                Does.Contain("CreatorRewardTargetPaging10000.PreserveAll(options)"));
            Assert.That(coordinatorSource, Does.Not.Contain("Take(80)"));
        }

        [Test]
        public void InvalidMultiGrantRollsBackEarlierSyntheticGrants()
        {
            var payload = new JObject
            {
                ["grantType"] = "MULTI_GRANT",
                ["grants"] = new JArray(
                    new JObject
                    {
                        ["grantType"] = "XP_VOUCHER",
                        ["xpType"] = "GUILD_TREASURY_XP",
                        ["amount"] = 999,
                        ["targetMode"] = "AUTOMATIC"
                    },
                    new JObject
                    {
                        ["grantType"] = "NOT_A_REAL_GRANT",
                        ["targetMode"] = "AUTOMATIC"
                    })
            };
            var catalog = new SyntheticCatalog().Add(
                "synthetic-invalid-multi",
                "SYNTH_CODE_INVALID_MULTI",
                Bundle("SYNTH_BUNDLE_INVALID_MULTI", "MULTI_GRANT", "AUTOMATIC", payload));
            var source = CampaignFactory.CreateM0Proof(10006);
            var before = CanonicalJson.Serialize(source);

            var result = new CreatorGiveawayCommandService10000().RedeemCode(
                source, catalog, "synthetic-invalid-multi", string.Empty, false);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item("CREATOR10000_GRANT_TYPE_UNKNOWN"));
            Assert.That(CanonicalJson.Serialize(source), Is.EqualTo(before));
            Assert.That(source.Guild.TreasuryXp, Is.Zero);
            Assert.That(Access(source).RedeemedCodeIds, Is.Empty);
            Assert.That(Access(source).AppliedReceiptIds, Is.Empty);
        }

        [Test]
        public void GrowthLedgerAndRedeemedRewardsSurviveAtomicSaveRoundTrip()
        {
            var catalog = new SyntheticCatalog()
                .Add("synthetic-save-xp", "SYNTH_CODE_SAVE_XP",
                    XpBundle("SYNTH_BUNDLE_SAVE_XP", "PERSONAL_XP", 725,
                        "PLAYER_SELECT_CHARACTER"))
                .Add("synthetic-save-prep", "SYNTH_CODE_SAVE_PREP",
                    ItemBundle("SYNTH_BUNDLE_SAVE_PREP", "INVENTORY_ITEM", "PREP_RECALL_BEACON", 1));
            var service = new CreatorGiveawayCommandService10000();
            var withXp = RequireSuccess(service.RedeemCode(
                CampaignWithRecruit(10007), catalog, "synthetic-save-xp", RecruitId, false));
            var redeemed = RequireSuccess(service.RedeemCode(
                withXp, catalog, "synthetic-save-prep", string.Empty, false));
            var path = Path.Combine(Path.GetTempPath(),
                "second_dimension_creator10000_" + Guid.NewGuid().ToString("N") + ".json");

            try
            {
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(
                    redeemed, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(path);

                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(CanonicalJson.Serialize(loaded.Value.CampaignState),
                    Is.EqualTo(CanonicalJson.Serialize(redeemed)));
                var access = Access(loaded.Value.CampaignState);
                Assert.That(access.RedeemedCodeIds,
                    Is.EquivalentTo(new[] { "SYNTH_CODE_SAVE_XP", "SYNTH_CODE_SAVE_PREP" }));
                Assert.That(access.AppliedReceiptIds, Has.Count.EqualTo(2));
                Assert.That(access.GrowthAllocations10000, Has.Count.EqualTo(1));
                Assert.That(access.GrowthAllocations10000[0].XpType, Is.EqualTo("PERSONAL_XP"));
                Assert.That(access.GrowthAllocations10000[0].TargetId, Is.EqualTo(RecruitId));
                Assert.That(access.GrowthAllocations10000[0].Amount, Is.EqualTo(725));
                Assert.That(loaded.Value.CampaignState.Guild.GuildCity.Materials.Single(value =>
                    value.MaterialId == "PREP_RECALL_BEACON").Amount, Is.EqualTo(1));
            }
            finally
            {
                DeleteIfPresent(path);
                DeleteIfPresent(path + ".bak");
                DeleteIfPresent(path + ".tmp");
            }
        }

        private static CreatorGiveawayRewardBundle10000 XpBundle(
            string bundleId,
            string xpType,
            int amount,
            string targetMode)
        {
            var payload = new JObject
            {
                ["grantType"] = "XP_VOUCHER",
                ["xpType"] = xpType,
                ["amount"] = amount,
                ["targetMode"] = targetMode,
                ["exactOnce"] = true
            };
            return Bundle(bundleId, "XP_VOUCHER", targetMode, payload, xpType, amount);
        }

        private static CreatorGiveawayRewardBundle10000 ItemBundle(
            string bundleId,
            string grantType,
            string itemId,
            int quantity)
        {
            var payload = new JObject
            {
                ["grantType"] = grantType,
                ["itemId"] = itemId,
                ["quantity"] = quantity,
                ["targetMode"] = "AUTOMATIC",
                ["exactOnce"] = true
            };
            return Bundle(bundleId, grantType, "AUTOMATIC", payload, itemId, quantity);
        }

        private static CreatorGiveawayRewardBundle10000 EquipmentBundle(
            string bundleId,
            string templateId,
            bool creatorPower,
            string powerMode,
            string family = "WF01_SWORD",
            string pattern = "BALANCED")
        {
            var payload = new JObject
            {
                ["grantType"] = "EQUIPMENT_INSTANCE",
                ["templateId"] = templateId,
                ["weaponFamily"] = family,
                ["pattern"] = pattern,
                ["quality"] = creatorPower ? "CREATOR" : "EPIC",
                ["manualEquip"] = true,
                ["autoEquip"] = false,
                ["pvpEligible"] = !creatorPower,
                ["targetMode"] = "AUTOMATIC"
            };
            return Bundle(bundleId, "EQUIPMENT_INSTANCE", "AUTOMATIC", payload,
                templateId, 1, creatorPower, powerMode, family, pattern,
                creatorPower ? "CREATOR" : "EPIC");
        }

        private static CreatorGiveawayRewardBundle10000 Bundle(
            string bundleId,
            string grantType,
            string targetMode,
            JObject payload,
            string primaryId = "SYNTHETIC",
            int amountOrQuantity = 1,
            bool creatorPower = false,
            string powerMode = "STANDARD_SAFE",
            string family = "",
            string pattern = "",
            string rarity = "COMMON") =>
            new CreatorGiveawayRewardBundle10000
            {
                RewardBundleId = bundleId,
                Category = "TEST",
                Label = bundleId,
                Rarity = rarity,
                GrantType = grantType,
                TargetMode = targetMode,
                PrimaryId = primaryId,
                AmountOrQuantity = amountOrQuantity,
                WeaponFamily = family,
                Pattern = pattern,
                PowerMode = powerMode,
                CreatorPowerFlag = creatorPower,
                StoryGate = creatorPower ? "CREATOR_POWER_CONFIRMATION" : "NONE",
                CodeOnly = false,
                NaturalAcquisition = "TEST_ONLY",
                AuthorityReference = "TEST_ONLY",
                PayloadJson = payload.ToString(Formatting.None),
                CodeCount = 1
            };

        private static CampaignState CampaignWithRecruit(long seed)
        {
            var source = CampaignFactory.CreateM0Proof(seed);
            var recruit = new RecruitState(RecruitId, 100, 100, 25, 25);
            var guild = source.Guild.With(source.Guild.TreasuryXp,
                new[] { recruit }, source.Guild.Unions, source.Guild.Inventory, source.Guild.Development);
            return source.With(guild, source.OpeningFlow);
        }

        private static CreatorAccessState028 Access(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.CreatorAccess028;

        private static CampaignState RequireSuccess(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            foreach (var character in value)
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f') ||
                      (character >= 'A' && character <= 'F')))
                    return false;
            return true;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private sealed class SyntheticCatalog : ICreatorGiveawayCatalog10000
        {
            private readonly Dictionary<string, CreatorGiveawayCodeRule10000> _codes =
                new Dictionary<string, CreatorGiveawayCodeRule10000>(StringComparer.Ordinal);
            private readonly Dictionary<string, CreatorGiveawayRewardBundle10000> _bundles =
                new Dictionary<string, CreatorGiveawayRewardBundle10000>(StringComparer.Ordinal);
            private readonly Dictionary<string, CreatorGiveawayRewardBundle10000> _equipment =
                new Dictionary<string, CreatorGiveawayRewardBundle10000>(StringComparer.Ordinal);

            public int CodeCount => _codes.Count;
            public int RewardBundleCount => _bundles.Count;

            public SyntheticCatalog Add(
                string plaintext,
                string codeId,
                CreatorGiveawayRewardBundle10000 bundle)
            {
                var hash = CreatorGiveawayCommandService10000.HashCode10000(plaintext);
                _codes.Add(hash, new CreatorGiveawayCodeRule10000
                {
                    CodeId = codeId,
                    Sha256 = hash,
                    RewardBundleId = bundle.RewardBundleId,
                    InstanceSeed = "SYNTH_SEED_" + codeId,
                    ExactOncePerCampaign = true,
                    SaveFlag = "REDEEMED_" + codeId,
                    CreatorPowerFlag = bundle.CreatorPowerFlag,
                    AutoEquip = false,
                    NormalizationVersion = "UPPER_ALNUM_V1"
                });
                _bundles.Add(bundle.RewardBundleId, bundle);
                if (StringComparer.Ordinal.Equals(bundle.GrantType, "EQUIPMENT_INSTANCE"))
                    _equipment.Add(bundle.PrimaryId, bundle);
                return this;
            }

            public bool TryGetCodeByHash(string sha256, out CreatorGiveawayCodeRule10000 rule) =>
                _codes.TryGetValue((sha256 ?? string.Empty).Trim().ToUpperInvariant(), out rule);

            public bool TryGetRewardBundle(
                string rewardBundleId,
                out CreatorGiveawayRewardBundle10000 rule) =>
                _bundles.TryGetValue(rewardBundleId ?? string.Empty, out rule);

            public bool TryGetEquipmentTemplate(
                string templateId,
                out CreatorGiveawayRewardBundle10000 rule) =>
                _equipment.TryGetValue(templateId ?? string.Empty, out rule);
        }
    }
}
