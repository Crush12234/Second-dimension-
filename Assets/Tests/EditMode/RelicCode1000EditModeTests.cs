using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.RelicCode1000;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Presentation.RelicCode1000;
using SecondDimension.Presentation.SpecialRelic001;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class RelicCode1000EditModeTests
    {
        private const string DuplicatePolicy =
            "CONVERT_TO_RELIC_RESONANCE_AND_EVOLUTION_MATERIAL";
        private const string DenialNote =
            "Never grants Great Covenant, Margin, Author, Founder, or Kael authority.";

        [Test]
        public void RuntimeRegistriesHaveExactCountsMappingsPoolsAndHashRecognition083()
        {
            var relics = SpecialRelicRegistry001.Load();
            var codes = RelicCodeRegistry1000.Load();

            Assert.That(relics.RelicCount, Is.EqualTo(60));
            Assert.That(relics.P0Count, Is.EqualTo(12));
            Assert.That(relics.RewardHooks, Has.Count.EqualTo(12));
            Assert.That(codes.CodeCount, Is.EqualTo(1000));
            Assert.That(codes.RewardBundleCount, Is.EqualTo(64));
            Assert.That(codes.AllRewardBundles.Count(value => value.IsDirect), Is.EqualTo(60));
            Assert.That(codes.AllRewardBundles.Count(value => value.IsCache), Is.EqualTo(4));

            var categoryCounts = codes.AllCodes.GroupBy(value => value.Category)
                .ToDictionary(value => value.Key, value => value.Count(), StringComparer.Ordinal);
            Assert.That(categoryCounts["RELIC_INVOCATION"], Is.EqualTo(480));
            Assert.That(categoryCounts["RELIC_ULTIMATE_ART"], Is.EqualTo(360));
            Assert.That(categoryCounts["RELIC_MYTHIC_HYBRID"], Is.EqualTo(120));
            Assert.That(categoryCounts["RELIC_CACHE"], Is.EqualTo(40));

            for (var index = 0; index < codes.AllCodes.Count; index++)
            {
                var code = codes.AllCodes[index];
                Assert.That(code.CodeId,
                    Is.EqualTo("RELICCODE001_" + (index + 1).ToString("0000")));
                Assert.That(code.ExactOncePerCampaign, Is.True);
                Assert.That(codes.TryGetCodeByHash(code.Sha256.ToUpperInvariant(), out var found),
                    Is.True, code.CodeId);
                Assert.That(found.CodeId, Is.EqualTo(code.CodeId));
            }

            Assert.That(relics.AllRelics.Count(value =>
                value.Kind == "INVOCATION" &&
                (value.Rarity == "Epic" || value.Rarity == "Legendary")), Is.EqualTo(16));
            Assert.That(relics.AllRelics.Count(value =>
                value.Kind == "ULTIMATE_ART" &&
                (value.Rarity == "Epic" || value.Rarity == "Legendary")), Is.EqualTo(24));
            Assert.That(relics.AllRelics.Count(value => value.Rarity == "Legendary"), Is.EqualTo(18));
            Assert.That(relics.AllRelics.Count(value =>
                value.Kind == "HYBRID" && value.Rarity == "Mythic"), Is.EqualTo(12));
        }

        [Test]
        public void StrictLoadersRejectUnknownDuplicateAndTrailingJson083()
        {
            var specialCatalog = LoadText(
                "SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_CATALOG_60_001");
            var p0Catalog = LoadText(
                "SecondDimension/SpecialRelic001/Data/P0_SPECIAL_RELICS_12_001");
            var hooks = LoadText(
                "SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_REWARD_HOOKS_001");
            var manifest = LoadText(
                RelicCodeRegistry1000.ManifestResourcePath);
            var bundles = LoadText(
                RelicCodeRegistry1000.BundleResourcePath);
            var special = SpecialRelicRegistry001.Load();

            var specialUnknown = (JArray)JToken.Parse(specialCatalog);
            ((JObject)specialUnknown[0])["ownerCode"] = "FORBIDDEN";
            Assert.Throws<InvalidOperationException>(() =>
                SpecialRelicRegistry001.ParseAndValidate(
                    specialUnknown.ToString(Formatting.None), p0Catalog, hooks));
            var specialDuplicate = ReplaceFirst(specialCatalog,
                "\"relicId\": \"RELIC001_INV_001\"",
                "\"relicId\": \"RELIC001_INV_001\", \"relicId\": \"RELIC001_INV_001\"");
            Assert.Throws<InvalidOperationException>(() =>
                SpecialRelicRegistry001.ParseAndValidate(specialDuplicate, p0Catalog, hooks));
            Assert.Throws<InvalidOperationException>(() =>
                SpecialRelicRegistry001.ParseAndValidate(specialCatalog + "{}", p0Catalog, hooks));

            var manifestUnknown = (JArray)JToken.Parse(manifest);
            ((JObject)manifestUnknown[0])["plaintextCode"] = "FORBIDDEN";
            Assert.Throws<InvalidOperationException>(() => RelicCodeRegistry1000.Load(
                manifestUnknown.ToString(Formatting.None), bundles, special));
            var manifestDuplicate = ReplaceFirst(manifest,
                "\"codeId\": \"RELICCODE001_0001\"",
                "\"codeId\": \"RELICCODE001_0001\", \"codeId\": \"RELICCODE001_0001\"");
            Assert.Throws<InvalidOperationException>(() =>
                RelicCodeRegistry1000.Load(manifestDuplicate, bundles, special));
            Assert.Throws<InvalidOperationException>(() =>
                RelicCodeRegistry1000.Load(manifest + "{}", bundles, special));
        }

        [Test]
        public void RuntimeFilesAreClosedHashOnlyAndForbidAuthorityGrantShapes083()
        {
            var specialDirectory = Path.Combine(Application.dataPath, "Resources", "SecondDimension",
                "SpecialRelic001", "Data");
            var codeDirectory = Path.Combine(Application.dataPath, "Resources", "SecondDimension",
                "RelicCode1000", "Data");
            Assert.That(RuntimeNames(specialDirectory), Is.EquivalentTo(new[]
            {
                "P0_SPECIAL_RELICS_12_001.json",
                "SPECIAL_RELIC_CATALOG_60_001.json",
                "SPECIAL_RELIC_REWARD_HOOKS_001.csv"
            }));
            Assert.That(RuntimeNames(codeDirectory), Is.EquivalentTo(new[]
            {
                "RELIC_CODE_REWARD_BUNDLES_64_v1.json",
                "RUNTIME_SAFE_RELIC_CODE_HASH_MANIFEST_1000_v1.json"
            }));

            var manifest = LoadText(RelicCodeRegistry1000.ManifestResourcePath);
            foreach (var forbidden in new[]
                     {
                         "plaintextCode", "ownerCode", "rawCode", "password", "secret"
                     }) Assert.That(manifest, Does.Not.Contain("\"" + forbidden + "\""));

            var bundles = (JArray)JToken.Parse(LoadText(RelicCodeRegistry1000.BundleResourcePath));
            Assert.That(bundles.OfType<JObject>().Select(value => (string)value["type"])
                .Distinct(StringComparer.Ordinal),
                Is.EquivalentTo(new[] { "SPECIAL_RELIC_GRANT", "RELIC_CACHE" }));
            foreach (var direct in bundles.OfType<JObject>().Where(value =>
                         StringComparer.Ordinal.Equals((string)value["type"], "SPECIAL_RELIC_GRANT")))
            {
                Assert.That((string)direct["duplicatePolicy"], Is.EqualTo(DuplicatePolicy));
                Assert.That((bool)direct["manualEquipOnly"], Is.True);
                Assert.That((string)direct["notes"], Is.EqualTo(DenialNote));
            }

            var forbiddenArtifacts = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(path => path.Replace('\\', '/').ToUpperInvariant())
                .Where(path => path.Contains("RELIC") && path.Contains("CODE") &&
                    new[] { "OWNER", "PLAINTEXT", "PLAIN_TEXT", "RAW_CODE", "UNHASHED", "CODEBOOK" }
                        .Any(path.Contains))
                .ToArray();
            Assert.That(forbiddenArtifacts, Is.Empty);
        }

        [Test]
        public void DirectGenericRelicCreatesDeterministicManualInventoryItemExactlyOnce083()
        {
            const string relicId = "RELIC001_INV_007";
            var catalog = SyntheticDirect("direct generic relic", "TEST_RELIC_DIRECT_GENERIC", relicId);
            var service = new RelicCodeCommandService1000();
            var source = CampaignFactory.CreateM0Proof(83001);

            var first = RequireSuccess(service.RedeemCode(
                source, catalog, SpecialRelicRegistry001.Load(), " DIRECT-generic_relic "));
            var item = first.Guild.Inventory.Single(value => value.DefinitionId == relicId);
            var access = Access(first);

            Assert.That(item.InstanceId, Does.StartWith("RELICITEM1000_"));
            Assert.That(item.DisplayName, Does.EndWith("• SEALED"));
            Assert.That(item.ValidSlotIds, Is.EqualTo(new[] { EquipmentSlotIds.ToolRelic }));
            Assert.That(item.EquipmentTags, Contains.Item("MANUAL_EQUIP_ONLY"));
            Assert.That(item.EquipmentTags, Contains.Item("SEALED_DATA_ONLY"));
            Assert.That(first.Guild.Recruits.SelectMany(value => value.Equipment.Assignments)
                .Any(value => value.Item.DefinitionId == relicId), Is.False);
            Assert.That(access.RelicReceipts1000, Has.Count.EqualTo(1));
            Assert.That(access.RelicReceipts1000[0].DuplicateConverted, Is.False);
            var coordinatorSource = File.ReadAllText(Path.Combine(Application.dataPath,
                "SecondDimension", "Presentation", "Creator028",
                "M1RuntimeCoordinator.Creator028.cs"));
            Assert.That(coordinatorSource, Does.Contain("Sealed Special Relic"));
            Assert.That(coordinatorSource, Does.Contain("combat adapter is not active yet"));

            var second = RequireSuccess(service.RedeemCode(
                first, catalog, SpecialRelicRegistry001.Load(), "direct generic relic"));
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));

            var tamperedItem = new EquipmentItemState(
                item.InstanceId, item.DefinitionId, item.DisplayName.Replace(" • SEALED", string.Empty),
                item.ValidSlotIds, new[] { "SPECIAL_RELIC_001" }, item.QualityId,
                item.ConditionBasisPoints, item.PlayerLocked);
            var tamperedGuild = first.Guild.With(
                first.Guild.TreasuryXp, first.Guild.Recruits, first.Guild.Unions,
                first.Guild.Inventory.Where(value => value.InstanceId != item.InstanceId)
                    .Concat(new[] { tamperedItem }).ToArray(), first.Guild.Development);
            var tampered = first.With(tamperedGuild, first.OpeningFlow);
            var tamperedResult = service.RedeemCode(
                tampered, catalog, SpecialRelicRegistry001.Load(), "direct generic relic");
            Assert.That(tamperedResult.IsSuccess, Is.False);
            Assert.That(tamperedResult.Errors,
                Contains.Item("RELICCODE1000_GENERIC_ITEM_LEDGER_MISMATCH"));
        }

        [Test]
        public void P0InvocationDirectCodeCreatesArtifactAndInventoryAtomically083()
        {
            const string relicId = "RELIC001_INV_001";
            var catalog = SyntheticDirect("p0 invocation direct", "TEST_RELIC_P0_INV", relicId);
            var relics = SpecialRelicRegistry001.Load();
            var service = new RelicCodeCommandService1000();
            var progression = new CampaignProgressionCommandService022();
            var registry = CampaignRegistry022.LoadFromResources();

            var first = RequireSuccess(service.RedeemCode(
                CampaignFactory.CreateM0Proof(83002), catalog, relics,
                "P0-invocation_direct", progression, registry));
            var item = first.Guild.Inventory.Single(value => value.DefinitionId == relicId);
            var state = Progression(first);

            Assert.That(item.ValidSlotIds, Is.EqualTo(new[] { EquipmentSlotIds.ToolRelic }));
            Assert.That(first.Guild.Recruits.SelectMany(value => value.Equipment.Assignments)
                .Any(value => value.Item.InstanceId == item.InstanceId), Is.False);
            Assert.That(state.InvocationArtifacts.Count(value => value.InstanceId == item.InstanceId),
                Is.EqualTo(1));
            Assert.That(state.AppliedReceiptIds.Count(value =>
                value.StartsWith("SPRELGRANT001_", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(Access(first).RelicReceipts1000.Single().ResolvedRelicId,
                Is.EqualTo(relicId));

            var second = RequireSuccess(service.RedeemCode(
                first, catalog, relics, "p0 invocation direct", progression, registry));
            Assert.That(CanonicalJson.Serialize(second), Is.EqualTo(CanonicalJson.Serialize(first)));
        }

        [Test]
        public void P0UltimateDirectAndCacheOutcomesUseExactActiveSealInventoryContract083()
        {
            var relics = SpecialRelicRegistry001.Load();
            var directCatalog = SyntheticDirect(
                "p0 ultimate direct", "TEST_RELIC_P0_ART", "RELIC001_ART_001");
            var service = new RelicCodeCommandService1000();
            var direct = RequireSuccess(service.RedeemCode(
                CampaignFactory.CreateM0Proof(83003), directCatalog, relics, "p0 ultimate direct"));
            var directItem = direct.Guild.Inventory.Single(value =>
                value.DefinitionId == "RELIC001_ART_001");
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryValidateP0SealItem(
                directItem, out var directRule, out var directError), Is.True, directError);
            Assert.That(directRule.UnderlyingArtId, Is.EqualTo("TREE_CA002_WPN_SWORD_N11"));
            Assert.That(Access(direct).UnlockedContentIds, Does.Not.Contain("RELIC001_ART_001"));

            var cacheBundle = CacheBundle("TEST_CACHE_ULTIMATE", "ULTIMATE_ART_EPIC_PLUS");
            var cacheSource = CampaignFactory.CreateM0Proof(83100);
            SyntheticRelicCodeCatalog cacheCatalog = null;
            string cacheInput = null;
            SpecialRelicRule001 outcome = null;
            for (var ordinal = 0; ordinal < 200; ordinal++)
            {
                var candidateInput = "p0 ultimate cache " + ordinal;
                var candidateCatalog = SyntheticCache(
                    candidateInput, "TEST_RELIC_CACHE_ART_" + ordinal, cacheBundle);
                var code = candidateCatalog.AllCodes.Single();
                RelicCodeCommandService1000.TryResolveOutcome(cacheSource, code, cacheBundle, relics,
                    out var resolved, out _, out _);
                if (relics.IsP0(resolved.RelicId) && resolved.Kind == "ULTIMATE_ART")
                {
                    cacheCatalog = candidateCatalog;
                    cacheInput = candidateInput;
                    outcome = resolved;
                    break;
                }
            }
            Assert.That(cacheCatalog, Is.Not.Null, "Expected deterministic sample to reach one P0 Art.");
            var cached = RequireSuccess(service.RedeemCode(
                cacheSource, cacheCatalog, relics, cacheInput));
            var cachedItem = cached.Guild.Inventory.Single(value =>
                value.DefinitionId == outcome.RelicId);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryValidateP0SealItem(
                cachedItem, out _, out var cacheError), Is.True, cacheError);
            Assert.That(Access(cached).RelicReceipts1000.Single().ResolvedRelicId,
                Is.EqualTo(outcome.RelicId));
        }

        [Test]
        public void NaturallyGrantedP0DuplicatesRemainExactOnceAcrossRetryAndReload083()
        {
            var relics = SpecialRelicRegistry001.Load();
            var progressionService = new CampaignProgressionCommandService022();
            var registry = CampaignRegistry022.LoadFromResources();
            var service = new RelicCodeCommandService1000();

            const string invocationId = "RELIC001_INV_001";
            var invocationNatural = RequireSuccess(progressionService.GrantSpecialInvocationRelic(
                CampaignFactory.CreateM0Proof(83301), registry, relics, invocationId,
                "NATURAL_RELIC_REWARD_INV_001"));
            var invocationCatalog = SyntheticDirect(
                "natural invocation duplicate", "TEST_RELIC_NATURAL_INV_DUP", invocationId);
            var invocationRedeemed = RequireSuccess(service.RedeemCode(
                invocationNatural, invocationCatalog, relics, "natural invocation duplicate",
                progressionService, registry));
            Assert.That(Access(invocationRedeemed).RelicReceipts1000.Single().DuplicateConverted,
                Is.True);
            Assert.That(Access(invocationRedeemed).RelicReceipts1000.Single().MaterialQuantity,
                Is.EqualTo(1));
            Assert.That(CountOwnedDefinition(invocationRedeemed, invocationId), Is.EqualTo(1));
            var invocationRetry = RequireSuccess(service.RedeemCode(
                Reload(invocationRedeemed), invocationCatalog, relics,
                "natural invocation duplicate", progressionService, registry));
            Assert.That(CanonicalJson.Serialize(invocationRetry),
                Is.EqualTo(CanonicalJson.Serialize(invocationRedeemed)));

            const string ultimateId = "RELIC001_ART_001";
            var ultimateNatural = CampaignFactory.CreateM0Proof(83302);
            var ultimateHash = CanonicalJson.Sha256Hex(new
            {
                ultimateNatural.CampaignGuid,
                RelicId = ultimateId,
                UniqueOwnedRelic = true,
                Authority = SpecialRelicUltimateArtBattleHook001.AuthorityVersion
            });
            var ultimateInstanceId = "SPECIALRELIC001_" +
                ultimateHash.Substring(0, 24).ToUpperInvariant();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                ultimateId, ultimateInstanceId, out var naturalSeal, out var sealError),
                Is.True, sealError);
            var ultimateGuild = ultimateNatural.Guild.With(
                ultimateNatural.Guild.TreasuryXp,
                ultimateNatural.Guild.Recruits,
                ultimateNatural.Guild.Unions,
                ultimateNatural.Guild.Inventory.Concat(new[] { naturalSeal }).ToArray(),
                ultimateNatural.Guild.Development);
            ultimateNatural = ultimateNatural.With(ultimateGuild, ultimateNatural.OpeningFlow);
            var ultimateCatalog = SyntheticDirect(
                "natural ultimate duplicate", "TEST_RELIC_NATURAL_ART_DUP", ultimateId);
            var ultimateRedeemed = RequireSuccess(service.RedeemCode(
                ultimateNatural, ultimateCatalog, relics, "natural ultimate duplicate"));
            Assert.That(Access(ultimateRedeemed).RelicReceipts1000.Single().DuplicateConverted,
                Is.True);
            Assert.That(Access(ultimateRedeemed).RelicReceipts1000.Single().MaterialQuantity,
                Is.EqualTo(2));
            Assert.That(CountOwnedDefinition(ultimateRedeemed, ultimateId), Is.EqualTo(1));
            var ultimateRetry = RequireSuccess(service.RedeemCode(
                Reload(ultimateRedeemed), ultimateCatalog, relics, "natural ultimate duplicate"));
            Assert.That(CanonicalJson.Serialize(ultimateRetry),
                Is.EqualTo(CanonicalJson.Serialize(ultimateRedeemed)));
        }

        [TestCase("RELIC001_INV_007", 1, true)]
        [TestCase("RELIC001_INV_009", 2, false)]
        [TestCase("RELIC001_INV_019", 3, false)]
        [TestCase("RELIC001_HYB_001", 4, false)]
        public void DuplicateRelicConvertsByExactTierAcrossInventoryAndAssignments083(
            string relicId, int expectedQuantity, bool equipped)
        {
            var source = CampaignWithOwnedRelic(83400 + expectedQuantity, relicId, equipped, false);
            var catalog = SyntheticDirect(
                "duplicate relic " + expectedQuantity,
                "TEST_RELIC_DUPLICATE_" + expectedQuantity,
                relicId);

            var updated = RequireSuccess(new RelicCodeCommandService1000().RedeemCode(
                source, catalog, SpecialRelicRegistry001.Load(),
                "duplicate relic " + expectedQuantity));
            var material = updated.Guild.GuildCity.Materials.Single(value =>
                value.MaterialId == RelicCodeCommandService1000.DuplicateMaterialId);
            var receipt = Access(updated).RelicReceipts1000.Single();

            Assert.That(material.Amount, Is.EqualTo(expectedQuantity));
            Assert.That(CountOwnedDefinition(updated, relicId), Is.EqualTo(1));
            Assert.That(receipt.DuplicateConverted, Is.True);
            Assert.That(receipt.MaterialId,
                Is.EqualTo(RelicCodeCommandService1000.DuplicateMaterialId));
            Assert.That(receipt.MaterialQuantity, Is.EqualTo(expectedQuantity));
        }

        [Test]
        public void MoreThanOneOwnedDefinitionFailsClosedWithoutMutation083()
        {
            const string relicId = "RELIC001_INV_007";
            var source = CampaignWithOwnedRelic(83500, relicId, true, true);
            var before = CanonicalJson.Serialize(source);
            var result = new RelicCodeCommandService1000().RedeemCode(
                source,
                SyntheticDirect("corrupt relic", "TEST_RELIC_CORRUPT", relicId),
                SpecialRelicRegistry001.Load(),
                "corrupt relic");

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item("RELICCODE1000_RELIC_OWNERSHIP_CORRUPT"));
            Assert.That(CanonicalJson.Serialize(source), Is.EqualTo(before));
            Assert.That(Access(source).RelicReceipts1000, Is.Empty);
        }

        [Test]
        public void MythicCacheOutcomeIsDeterministicAcrossReloadAndSaveRoundTrip083()
        {
            var relics = SpecialRelicRegistry001.Load();
            var bundle = CacheBundle("TEST_CACHE_MYTHIC", "MYTHIC_WEIGHTED_RELICS");
            var catalog = SyntheticCache("mythic cache", "TEST_RELIC_CACHE_MYTHIC", bundle);
            var source = CampaignFactory.CreateM0Proof(83600);
            var reloadedSource = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(source));
            var code = catalog.AllCodes.Single();

            Assert.That(RelicCodeCommandService1000.TryResolveOutcome(
                source, code, bundle, relics, out var firstOutcome, out var firstHash, out var firstError),
                Is.True, firstError);
            Assert.That(RelicCodeCommandService1000.TryResolveOutcome(
                reloadedSource, code, bundle, relics,
                out var reloadedOutcome, out var reloadedHash, out var reloadedError),
                Is.True, reloadedError);
            Assert.That(reloadedOutcome.RelicId, Is.EqualTo(firstOutcome.RelicId));
            Assert.That(reloadedHash, Is.EqualTo(firstHash));
            Assert.That(firstOutcome.Kind, Is.EqualTo("HYBRID"));
            Assert.That(firstOutcome.Rarity, Is.EqualTo("Mythic"));

            var redeemed = RequireSuccess(new RelicCodeCommandService1000().RedeemCode(
                source, catalog, relics, "MYTHIC-cache"));
            var receipt = Access(redeemed).RelicReceipts1000.Single();
            Assert.That(receipt.ResolvedRelicId, Is.EqualTo(firstOutcome.RelicId));
            Assert.That(receipt.OutcomeHash, Is.EqualTo(firstHash));

            var path = Path.Combine(Path.GetTempPath(),
                "second_dimension_relic1000_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(
                    redeemed, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                Assert.That(CanonicalJson.Serialize(loaded.Value.CampaignState),
                    Is.EqualTo(CanonicalJson.Serialize(redeemed)));
                Assert.That(Access(loaded.Value.CampaignState).RelicReceipts1000.Single().OutcomeHash,
                    Is.EqualTo(firstHash));
            }
            finally
            {
                DeleteIfPresent(path);
                DeleteIfPresent(path + ".bak");
                DeleteIfPresent(path + ".tmp");
            }
        }

        [Test]
        public void InvalidCacheRollsBackAndOldSavesDefaultToEmptyRelicReceipts083()
        {
            var source = CampaignFactory.CreateM0Proof(83700);
            var invalid = CacheBundle("TEST_CACHE_INVALID", "FORBIDDEN_AUTHORITY_POOL");
            var result = new RelicCodeCommandService1000().RedeemCode(
                source,
                SyntheticCache("invalid cache", "TEST_RELIC_INVALID_CACHE", invalid),
                SpecialRelicRegistry001.Load(),
                "invalid cache");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item("RELICCODE1000_CACHE_POOL_EMPTY"));
            Assert.That(Access(source).RedeemedCodeIds, Is.Empty);
            Assert.That(Access(source).AppliedReceiptIds, Is.Empty);
            Assert.That(Access(source).RelicReceipts1000, Is.Empty);

            var raw = JObject.Parse(JsonConvert.SerializeObject(source));
            var access = (JObject)raw["Guild"]?["GuildCity"]?["Strategic017H"]?
                ["Campaign019"]?["Playable020"]?["CreatorAccess028"];
            Assert.That(access, Is.Not.Null);
            access.Remove("RelicReceipts1000");
            var restored = raw.ToObject<CampaignState>();
            Assert.That(Access(restored).RelicReceipts1000, Is.Empty);
        }

        [Test]
        public void CorruptCreatorLedgersAndTamperedReceiptProofFailClosed083()
        {
            const string relicId = "RELIC001_INV_007";
            var relics = SpecialRelicRegistry001.Load();
            var catalog = SyntheticDirect("ledger relic", "TEST_RELIC_LEDGER", relicId);
            var source = CampaignWithOwnedRelic(83800, relicId, false, false);
            var code = catalog.AllCodes.Single();
            var bundle = catalog.AllRewardBundles.Single();
            RelicCodeCommandService1000.TryResolveOutcome(source, code, bundle, relics,
                out _, out var outcomeHash, out _);
            var receiptId = "RELICREC1000_" + outcomeHash.Substring(0, 24).ToUpperInvariant();

            var redeemedOnly = WithAccess(source, Access(source).With(
                redeemedCodeIds: new[] { code.CodeId }));
            var redeemedResult = new RelicCodeCommandService1000().RedeemCode(
                redeemedOnly, catalog, relics, "ledger relic");
            Assert.That(redeemedResult.IsSuccess, Is.False);
            Assert.That(redeemedResult.Errors, Contains.Item("RELICCODE1000_LEDGER_MISMATCH"));

            var appliedOnly = WithAccess(source, Access(source).With(
                appliedReceiptIds: new[] { receiptId }));
            var appliedResult = new RelicCodeCommandService1000().RedeemCode(
                appliedOnly, catalog, relics, "ledger relic");
            Assert.That(appliedResult.IsSuccess, Is.False);
            Assert.That(appliedResult.Errors, Contains.Item("RELICCODE1000_LEDGER_MISMATCH"));

            var tamperedReceipt = new CreatorRelicReceipt1000(
                code.CodeId, bundle.RewardBundleId, relicId, true,
                RelicCodeCommandService1000.DuplicateMaterialId, 99, outcomeHash, receiptId);
            var tampered = WithAccess(source, Access(source).With(
                redeemedCodeIds: new[] { code.CodeId },
                appliedReceiptIds: new[] { receiptId },
                relicReceipts1000: new[] { tamperedReceipt }));
            var tamperedResult = new RelicCodeCommandService1000().RedeemCode(
                tampered, catalog, relics, "ledger relic");
            Assert.That(tamperedResult.IsSuccess, Is.False);
            Assert.That(tamperedResult.Errors, Contains.Item("RELICCODE1000_LEDGER_MISMATCH"));

            var secondReceipt = new CreatorRelicReceipt1000(
                code.CodeId, bundle.RewardBundleId, relicId, false,
                string.Empty, 0, new string('A', 64), "RELICREC1000_DUPLICATE");
            Assert.Throws<ArgumentException>(() => Access(source).With(
                relicReceipts1000: new[] { tamperedReceipt, secondReceipt }));
        }

        [Test]
        public void RedeemedP0InvocationRequiresArtifactAndGrantReceiptProof083()
        {
            const string relicId = "RELIC001_INV_001";
            var catalog = SyntheticDirect("p0 ledger relic", "TEST_RELIC_P0_LEDGER", relicId);
            var relics = SpecialRelicRegistry001.Load();
            var service = new RelicCodeCommandService1000();
            var progressionService = new CampaignProgressionCommandService022();
            var registry = CampaignRegistry022.LoadFromResources();
            var redeemed = RequireSuccess(service.RedeemCode(
                CampaignFactory.CreateM0Proof(83900), catalog, relics, "p0 ledger relic",
                progressionService, registry));

            var missingArtifact = WithProgression(redeemed, Progression(redeemed).With(
                invocationArtifacts: Array.Empty<InvocationArtifactState022>()));
            var missingArtifactResult = service.RedeemCode(
                missingArtifact, catalog, relics, "p0 ledger relic", progressionService, registry);
            Assert.That(missingArtifactResult.IsSuccess, Is.False);

            var receiptFiltered = Progression(redeemed).AppliedReceiptIds.Where(value =>
                !value.StartsWith("SPRELGRANT001_", StringComparison.Ordinal)).ToArray();
            var missingGrantReceipt = WithProgression(redeemed, Progression(redeemed).With(
                appliedReceiptIds: receiptFiltered));
            var missingReceiptResult = service.RedeemCode(
                missingGrantReceipt, catalog, relics, "p0 ledger relic",
                progressionService, registry);
            Assert.That(missingReceiptResult.IsSuccess, Is.False);
            Assert.That(missingReceiptResult.Errors,
                Contains.Item("RELICCODE1000_P0_INVOCATION_LEDGER_MISMATCH"));
        }

        [Test]
        public void RedeemedP0UltimateRejectsTamperedSealShape083()
        {
            const string relicId = "RELIC001_ART_001";
            var catalog = SyntheticDirect("p0 seal ledger", "TEST_RELIC_P0_SEAL_LEDGER", relicId);
            var relics = SpecialRelicRegistry001.Load();
            var service = new RelicCodeCommandService1000();
            var redeemed = RequireSuccess(service.RedeemCode(
                CampaignFactory.CreateM0Proof(83910), catalog, relics, "p0 seal ledger"));
            var item = redeemed.Guild.Inventory.Single(value => value.DefinitionId == relicId);
            var tamperedItem = new EquipmentItemState(
                item.InstanceId, item.DefinitionId, "Tampered Seal",
                new[] { EquipmentSlotIds.ToolRelic }, new[] { "WRONG_TAG" },
                "EPIC", 10000, false);
            var tamperedInventory = redeemed.Guild.Inventory
                .Where(value => value.InstanceId != item.InstanceId)
                .Concat(new[] { tamperedItem }).ToArray();
            var tamperedGuild = redeemed.Guild.With(
                redeemed.Guild.TreasuryXp, redeemed.Guild.Recruits, redeemed.Guild.Unions,
                tamperedInventory, redeemed.Guild.Development);
            var tampered = redeemed.With(tamperedGuild, redeemed.OpeningFlow);

            var result = service.RedeemCode(tampered, catalog, relics, "p0 seal ledger");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.Any(value =>
                value.IndexOf("P0_ULTIMATE", StringComparison.Ordinal) >= 0), Is.True,
                string.Join("\n", result.Errors));

            var malformedExisting = CampaignWithOwnedRelic(83911, relicId, false, false);
            var malformedExistingResult = service.RedeemCode(
                malformedExisting, catalog, relics, "p0 seal ledger");
            Assert.That(malformedExistingResult.IsSuccess, Is.False);
            Assert.That(malformedExistingResult.Errors.Any(value =>
                value.IndexOf("P0_ULTIMATE", StringComparison.Ordinal) >= 0), Is.True,
                "A new code must not convert an invalid pre-existing P0 seal as a duplicate.");
        }

        [Test]
        public void DuplicateReceiptMaterialProofIsAggregateAcrossLedger083()
        {
            const string relicId = "RELIC001_INV_019";
            var bundle = DirectBundle(relicId);
            var catalog = new SyntheticRelicCodeCatalog()
                .Add("legend duplicate one", "TEST_RELIC_LEGEND_DUP_1", relicId,
                    "RELIC_INVOCATION", bundle)
                .Add("legend duplicate two", "TEST_RELIC_LEGEND_DUP_2", relicId,
                    "RELIC_INVOCATION", bundle);
            var relics = SpecialRelicRegistry001.Load();
            var service = new RelicCodeCommandService1000();
            var first = RequireSuccess(service.RedeemCode(
                CampaignWithOwnedRelic(83920, relicId, false, false),
                catalog, relics, "legend duplicate one"));
            var second = RequireSuccess(service.RedeemCode(
                first, catalog, relics, "legend duplicate two"));
            Assert.That(Access(second).RelicReceipts1000.Count(value => value.DuplicateConverted),
                Is.EqualTo(2));
            Assert.That(second.Guild.GuildCity.Materials.Single(value =>
                value.MaterialId == RelicCodeCommandService1000.DuplicateMaterialId).Amount,
                Is.EqualTo(6));

            var loweredMaterials = second.Guild.GuildCity.Materials.Select(value =>
                value.MaterialId == RelicCodeCommandService1000.DuplicateMaterialId
                    ? value.WithAmount(3)
                    : value).ToArray();
            var city = second.Guild.GuildCity.With(materials: loweredMaterials);
            var tampered = second.With(second.Guild.WithGuildCity(city), second.OpeningFlow);
            var normallyReloadedAfterSpend = Reload(tampered);
            Assert.That(Access(normallyReloadedAfterSpend).RelicReceipts1000, Has.Count.EqualTo(2),
                "A lower spendable balance must not prevent ordinary save deserialization.");
            var result = service.RedeemCode(
                normallyReloadedAfterSpend, catalog, relics, "legend duplicate one");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item("RELICCODE1000_LEDGER_MISMATCH"));
        }

        [Test]
        public void CoordinatorPreservesLegacyAndCreatorCodesAfterRelicFirstLookup083()
        {
            var legacy = CreatorRegistry028.Load();
            var giveaway = CreatorGiveawayRegistry10000.Load();
            var relic = RelicCodeRegistry1000.Load();
            Assert.That(legacy.CodeCount, Is.EqualTo(300));
            Assert.That(giveaway.CodeCount, Is.EqualTo(10000));
            Assert.That(relic.CodeCount, Is.EqualTo(1000));
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var codeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var code in legacy.AllCodes)
            {
                Assert.That(hashes.Add(code.CodeHash.Trim().ToUpperInvariant()), Is.True, code.CodeId);
                Assert.That(codeIds.Add(code.CodeId), Is.True, code.CodeId);
            }
            foreach (var code in giveaway.AllCodes)
            {
                Assert.That(hashes.Add(code.Sha256.Trim().ToUpperInvariant()), Is.True, code.CodeId);
                Assert.That(codeIds.Add(code.CodeId), Is.True, code.CodeId);
            }
            foreach (var code in relic.AllCodes)
            {
                Assert.That(hashes.Add(code.Sha256.Trim().ToUpperInvariant()), Is.True, code.CodeId);
                Assert.That(codeIds.Add(code.CodeId), Is.True, code.CodeId);
            }
            Assert.That(hashes, Has.Count.EqualTo(11300));
            Assert.That(codeIds, Has.Count.EqualTo(11300));

            var source = File.ReadAllText(Path.Combine(Application.dataPath, "SecondDimension",
                "Presentation", "Creator028", "M1RuntimeCoordinator.Creator028.cs"));
            var previewStart = source.IndexOf("PreviewCreatorCode028", StringComparison.Ordinal);
            var redeemStart = source.IndexOf("RedeemCreatorCode028(string input)=>", StringComparison.Ordinal);
            var preview = source.Substring(previewStart, redeemStart - previewStart);
            Assert.That(preview.IndexOf("RelicCodeRegistry1000()", StringComparison.Ordinal),
                Is.LessThan(preview.IndexOf("CreatorGiveawayRegistry10000()", StringComparison.Ordinal)));
            Assert.That(source, Does.Contain("reg.CodeCount+giveaway.CodeCount+relicCodes.CodeCount"));
            Assert.That(source, Does.Contain("_creatorGiveawayCommands10000.RedeemCode("));
            Assert.That(source, Does.Contain("RedeemLegacyCreatorCode028(input)"));
        }

        [Test]
        public void Release089BuilderPinsAllRelicResourcesAndExpandedRecruitEvidenceCounts089()
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_CATALOG_60_001.json", "9B463F49CD82E51984A8498B36252C09A3FE0BD780829DFB34144DE3F039E909" },
                { "Resources/SecondDimension/SpecialRelic001/Data/P0_SPECIAL_RELICS_12_001.json", "978B794447079C83739A06BCC5562B78CF5FB215D592D3AD096F9B198B306796" },
                { "Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_REWARD_HOOKS_001.csv", "F0C575E8853FA018372411436B00AECE20517726C266BCCC7CBEBEBF011C44B8" },
                { "Resources/SecondDimension/RelicCode1000/Data/RUNTIME_SAFE_RELIC_CODE_HASH_MANIFEST_1000_v1.json", "13D67DF4AF699DDD8B381F53AF87193D16808BD4D48AE407E5FBA2D951E7C299" },
                { "Resources/SecondDimension/RelicCode1000/Data/RELIC_CODE_REWARD_BUNDLES_64_v1.json", "642A84BE335385B3EDFB6CDCB368BBA2BD079F9186F4F586355F8378D8E5E5D4" }
            };
            var buildSource = File.ReadAllText(Path.Combine(Application.dataPath, "Editor",
                "SecondDimension", "Release072", "FirstHourGoldWindowsBuild.cs"));
            var smokeSource = File.ReadAllText(Path.Combine(Application.dataPath, "SecondDimension",
                "Presentation", "FirstHour071", "FirstHourGoldSmoke071.cs"));

            foreach (var pair in expected)
            {
                var absolute = Path.Combine(Application.dataPath,
                    pair.Key.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(ComputeSha256(absolute), Is.EqualTo(pair.Value), pair.Key);
                Assert.That(buildSource, Does.Contain("Assets/" + pair.Key));
                Assert.That(buildSource, Does.Contain(pair.Value));
            }
            Assert.That(buildSource, Does.Contain("SECOND-DIMENSION-ALPHA-132"));
            Assert.That(buildSource, Does.Contain("ALPHA 132"));
            Assert.That(buildSource, Does.Contain("PlayerVersion = \"0.132.0-alpha\""));
            Assert.That(buildSource, Does.Contain("SECOND_DIMENSION_FIRST_HOUR_GOLD_SMOKE_084_1"));
            Assert.That(buildSource, Does.Contain("FIRST_HOUR_GOLD_SMOKE_084.json"));
            Assert.That(buildSource, Does.Contain("SmokeExpectedScreenshotCount = 75"));
            Assert.That(buildSource, Does.Contain("SmokeExpectedGateCount = 58"));
            Assert.That(buildSource,
                Does.Contain("appliedReceiptCount = access.AppliedReceiptIds.Count"));
            Assert.That(buildSource, Does.Contain("distinctHashes.Count != 11_300"));
            Assert.That(buildSource, Does.Contain("SpecialRelicP0Combat001Tests.cs"));
            Assert.That(buildSource, Does.Contain("SpecialRelicP0Invocation001Tests.cs"));
            Assert.That(buildSource,
                Does.Contain("GrantShapeRequiresManualToolRelicEquipmentAndExactUnion"));
            Assert.That(buildSource,
                Does.Contain("GrandRestorationPreservesCrossUnionTargetWhenAdapterIsRequested"));
            Assert.That(buildSource,
                Does.Contain("MultipleEligibleSealsChooseOneDeterministicallyAcrossReplayReloadAndRound"));
            Assert.That(buildSource,
                Does.Contain("InvocationGrowthAndReceiptsRoundTripThroughExistingV11Save"));
            Assert.That(smokeSource, Does.Contain("SECOND-DIMENSION-ALPHA-132"));
            Assert.That(smokeSource, Does.Contain("0.132.0-alpha"));
            Assert.That(File.ReadAllText(Path.Combine(Application.dataPath, "..", "ProjectSettings",
                "ProjectSettings.asset")), Does.Contain("bundleVersion: 0.132.0-alpha"));
        }

        private static SyntheticRelicCodeCatalog SyntheticDirect(
            string plaintext, string codeId, string relicId)
        {
            var bundle = DirectBundle(relicId);
            var category = relicId.Contains("_INV_") ? "RELIC_INVOCATION" :
                relicId.Contains("_ART_") ? "RELIC_ULTIMATE_ART" : "RELIC_MYTHIC_HYBRID";
            return new SyntheticRelicCodeCatalog().Add(
                plaintext, codeId, relicId, category, bundle);
        }

        private static RelicRewardBundle1000 DirectBundle(string relicId) =>
            new RelicRewardBundle1000(
                "RB_TEST_DIRECT_" + relicId, "SPECIAL_RELIC_GRANT", relicId,
                DuplicatePolicy, true, DenialNote, string.Empty, false);

        private static SyntheticRelicCodeCatalog SyntheticCache(
            string plaintext, string codeId, RelicRewardBundle1000 bundle) =>
            new SyntheticRelicCodeCatalog().Add(
                plaintext, codeId, string.Empty, "RELIC_CACHE", bundle);

        private static RelicRewardBundle1000 CacheBundle(string bundleId, string pool) =>
            new RelicRewardBundle1000(
                bundleId, "RELIC_CACHE", string.Empty, string.Empty, false,
                string.Empty, pool, true);

        private static CampaignState CampaignWithOwnedRelic(
            long seed, string relicId, bool equipped, bool addSecondInventory)
        {
            var source = CampaignFactory.CreateM0Proof(seed);
            var first = TestRelicItem("TEST_RELIC_OWNED_A_" + seed, relicId);
            var recruit = new RecruitState("TEST_RELIC_RECRUIT_" + seed, 100, 100, 25, 25);
            var inventory = new List<EquipmentItemState>();
            if (equipped)
                recruit = recruit.WithEquipment(new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic, first)
                }));
            else inventory.Add(first);
            if (addSecondInventory)
                inventory.Add(TestRelicItem("TEST_RELIC_OWNED_B_" + seed, relicId));
            var guild = source.Guild.With(source.Guild.TreasuryXp,
                new[] { recruit }, Array.Empty<UnionState>(), inventory.AsReadOnly(),
                source.Guild.Development);
            return source.With(guild, source.OpeningFlow);
        }

        private static EquipmentItemState TestRelicItem(string instanceId, string relicId) =>
            new EquipmentItemState(instanceId, relicId, relicId,
                new[] { EquipmentSlotIds.ToolRelic }, new[] { "TEST_RELIC" },
                "QUALITY_TEST", 10000, false);

        private static CampaignState WithAccess(CampaignState campaign, CreatorAccessState028 access)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                creatorAccess028: access, replaceCreatorAccess028: true);
            progress = progress.With(playable020: playable, replacePlayable020: true);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState WithProgression(
            CampaignState campaign, CampaignProgressionState022 progressionState)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                progression022: progressionState, replaceProgression022: true);
            progress = progress.With(playable020: playable, replacePlayable020: true);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CreatorAccessState028 Access(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.CreatorAccess028;

        private static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;

        private static int CountOwnedDefinition(CampaignState campaign, string definitionId) =>
            campaign.Guild.Inventory.Count(value => value.DefinitionId == definitionId) +
            campaign.Guild.Recruits.SelectMany(value => value.Equipment.Assignments)
                .Count(value => value.Item.DefinitionId == definitionId);

        private static CampaignState RequireSuccess(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static CampaignState Reload(CampaignState campaign) =>
            JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));

        private static string LoadText(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            Assert.That(asset, Is.Not.Null, resourcePath);
            return asset.text;
        }

        private static string[] RuntimeNames(string directory) =>
            Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();

        private static string ReplaceFirst(string source, string oldValue, string newValue)
        {
            var index = source.IndexOf(oldValue, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), oldValue);
            return source.Substring(0, index) + newValue + source.Substring(index + oldValue.Length);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private sealed class SyntheticRelicCodeCatalog : IRelicCodeCatalog1000
        {
            private readonly Dictionary<string, RelicCodeRule1000> _codes =
                new Dictionary<string, RelicCodeRule1000>(StringComparer.Ordinal);
            private readonly Dictionary<string, RelicRewardBundle1000> _bundles =
                new Dictionary<string, RelicRewardBundle1000>(StringComparer.Ordinal);

            public IReadOnlyList<RelicCodeRule1000> AllCodes =>
                _codes.Values.OrderBy(value => value.CodeId, StringComparer.Ordinal).ToArray();
            public IReadOnlyList<RelicRewardBundle1000> AllRewardBundles =>
                _bundles.Values.OrderBy(value => value.RewardBundleId, StringComparer.Ordinal).ToArray();
            public int CodeCount => _codes.Count;
            public int RewardBundleCount => _bundles.Count;

            public SyntheticRelicCodeCatalog Add(
                string plaintext,
                string codeId,
                string relicId,
                string category,
                RelicRewardBundle1000 bundle)
            {
                var hash = CreatorGiveawayCommandService10000.HashCode10000(plaintext);
                _codes.Add(hash, new RelicCodeRule1000(
                    codeId, hash, bundle.RewardBundleId, relicId, category, true));
                if (!_bundles.ContainsKey(bundle.RewardBundleId))
                    _bundles.Add(bundle.RewardBundleId, bundle);
                return this;
            }

            public bool TryGetCodeByHash(string sha256, out RelicCodeRule1000 rule) =>
                _codes.TryGetValue((sha256 ?? string.Empty).Trim().ToUpperInvariant(), out rule);

            public bool TryGetRewardBundle(
                string rewardBundleId, out RelicRewardBundle1000 rule) =>
                _bundles.TryGetValue(rewardBundleId ?? string.Empty, out rule);
        }
    }
}
