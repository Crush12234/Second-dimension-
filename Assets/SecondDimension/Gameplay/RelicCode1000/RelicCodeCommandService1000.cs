using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.RelicCode1000
{
    /// <summary>
    /// Applies hash-only relic codes to immutable campaign state. Equipment enters Guild
    /// inventory and is never auto-equipped. Duplicate conversion is deliberately local
    /// until a broader material economy authority is authored.
    /// </summary>
    public sealed class RelicCodeCommandService1000
    {
        public const string DuplicateMaterialId = "MATERIAL_RESONANCE_THREAD";
        public const string RelicEquipmentTag = "SPECIAL_RELIC_001";

        public Result<CampaignState> RedeemCode(
            CampaignState campaign,
            IRelicCodeCatalog1000 codeCatalog,
            ISpecialRelicCatalog001 relicCatalog,
            string input,
            CampaignProgressionCommandService022 campaignProgressionService = null,
            ICampaignRegistry022 campaignRegistry = null)
        {
            if (!TryContext(campaign, out var city, out var strategic, out var progress,
                    out var playable, out var access, out var error))
                return Result<CampaignState>.Failure(error);
            if (codeCatalog == null || relicCatalog == null)
                return Result<CampaignState>.Failure("RELICCODE1000_CATALOG_REQUIRED");

            var hash = CreatorGiveawayCommandService10000.HashCode10000(input);
            if (string.IsNullOrWhiteSpace(hash) || !codeCatalog.TryGetCodeByHash(hash, out var code))
                return Result<CampaignState>.Failure("RELICCODE1000_CODE_INVALID");
            if (!code.ExactOncePerCampaign)
                return Result<CampaignState>.Failure("RELICCODE1000_EXACT_ONCE_REQUIRED");
            if (!codeCatalog.TryGetRewardBundle(code.RewardBundleId, out var bundle))
                return Result<CampaignState>.Failure("RELICCODE1000_BUNDLE_UNKNOWN");

            if (!TryResolveOutcome(campaign, code, bundle, relicCatalog,
                    out var relic, out var outcomeHash, out error))
                return Result<CampaignState>.Failure(error);

            var receiptId = "RELICREC1000_" + outcomeHash.Substring(0, 24).ToUpperInvariant();
            var alreadyRedeemed = access.RedeemedCodeIds.Contains(code.CodeId);
            var matchingReceipts = access.RelicReceipts1000.Where(value =>
                StringComparer.Ordinal.Equals(value.CodeId, code.CodeId) ||
                StringComparer.Ordinal.Equals(value.ReceiptId, receiptId)).ToArray();
            var appliedReceiptCount = access.AppliedReceiptIds.Count(value =>
                StringComparer.Ordinal.Equals(value, receiptId));
            if (alreadyRedeemed)
            {
                if (matchingReceipts.Length == 1 && appliedReceiptCount == 1 &&
                    ReceiptMatches(matchingReceipts[0], code, bundle, relic, outcomeHash, receiptId) &&
                    ReceiptMaterialProofMatches(city, access.RelicReceipts1000, relicCatalog) &&
                    CountOwnedDefinition(campaign.Guild, relic.RelicId) == 1)
                {
                    if (relicCatalog.IsP0(relic.RelicId) &&
                        StringComparer.Ordinal.Equals(relic.Kind, "INVOCATION"))
                    {
                        if (campaignProgressionService == null || campaignRegistry == null)
                            return Result<CampaignState>.Failure(
                                "RELICCODE1000_P0_INVOCATION_ADAPTER_REQUIRED");
                        var validation = campaignProgressionService.GrantSpecialInvocationRelic(
                            campaign, campaignRegistry, relicCatalog, relic.RelicId, receiptId);
                        if (!validation.IsSuccess ||
                            !StringComparer.Ordinal.Equals(
                                CanonicalJson.Serialize(validation.Value),
                                CanonicalJson.Serialize(campaign)))
                            return Result<CampaignState>.Failure(
                                "RELICCODE1000_P0_INVOCATION_LEDGER_MISMATCH");
                    }
                    else if (relicCatalog.IsP0(relic.RelicId) &&
                             StringComparer.Ordinal.Equals(relic.Kind, "ULTIMATE_ART") &&
                             !TryValidateP0UltimateOwnership(
                                 campaign.Guild, campaign.CampaignGuid, relic.RelicId, out error))
                        return Result<CampaignState>.Failure(
                            error ?? "RELICCODE1000_P0_ULTIMATE_LEDGER_MISMATCH");
                    else if (!matchingReceipts[0].DuplicateConverted &&
                             !relicCatalog.IsP0(relic.RelicId) &&
                             !TryValidateGenericGrantOwnership(
                                 campaign.Guild, campaign.CampaignGuid, code, bundle, relic,
                                 receiptId, out error))
                        return Result<CampaignState>.Failure(error);
                    return Result<CampaignState>.Success(campaign);
                }
                return Result<CampaignState>.Failure("RELICCODE1000_LEDGER_MISMATCH");
            }
            if (matchingReceipts.Length != 0 || appliedReceiptCount != 0)
                return Result<CampaignState>.Failure("RELICCODE1000_LEDGER_MISMATCH");

            var ownedCount = CountOwnedDefinition(campaign.Guild, relic.RelicId);
            if (ownedCount > 1)
                return Result<CampaignState>.Failure("RELICCODE1000_RELIC_OWNERSHIP_CORRUPT");

            var guild = campaign.Guild;
            var p0Invocation = relicCatalog.IsP0(relic.RelicId) &&
                StringComparer.Ordinal.Equals(relic.Kind, "INVOCATION");
            var p0Ultimate = relicCatalog.IsP0(relic.RelicId) &&
                StringComparer.Ordinal.Equals(relic.Kind, "ULTIMATE_ART");
            if (p0Ultimate && ownedCount == 1 &&
                !TryValidateP0UltimateOwnership(
                    guild, campaign.CampaignGuid, relic.RelicId, out error))
                return Result<CampaignState>.Failure(error);
            if (p0Invocation)
            {
                if (campaignProgressionService == null || campaignRegistry == null)
                    return Result<CampaignState>.Failure(
                        "RELICCODE1000_P0_INVOCATION_ADAPTER_REQUIRED");
                var granted = campaignProgressionService.GrantSpecialInvocationRelic(
                    campaign, campaignRegistry, relicCatalog, relic.RelicId, receiptId);
                if (!granted.IsSuccess) return granted;
                campaign = granted.Value;
                if (!TryContext(campaign, out city, out strategic, out progress,
                        out playable, out access, out error))
                    return Result<CampaignState>.Failure(error);
                guild = campaign.Guild;
            }

            var duplicateConverted = ownedCount == 1;
            var materialQuantity = 0;
            if (duplicateConverted)
            {
                if (!TryDuplicateQuantity(relic.Rarity, out materialQuantity))
                    return Result<CampaignState>.Failure("RELICCODE1000_RARITY_UNSUPPORTED");
                city = city.With(
                    materials: MergeMaterial(city.Materials, DuplicateMaterialId, materialQuantity),
                    lastCheckpointId: "relic_code_1000_duplicate_converted");
            }
            else if (!p0Invocation)
            {
                var instanceId = p0Ultimate
                    ? "SPECIALRELIC001_" + CanonicalJson.Sha256Hex(new
                    {
                        campaign.CampaignGuid,
                        relic.RelicId,
                        UniqueOwnedRelic = true,
                        Authority = SpecialRelicUltimateArtBattleHook001.AuthorityVersion
                    }).Substring(0, 24).ToUpperInvariant()
                    : GenericRelicInstanceId(
                        campaign.CampaignGuid, code, bundle, relic, receiptId);
                if (OwnedInstanceIdExists(guild, instanceId))
                    return Result<CampaignState>.Failure("RELICCODE1000_INSTANCE_ID_COLLISION");
                EquipmentItemState grantItem;
                if (p0Ultimate)
                {
                    if (!SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                            relic.RelicId, instanceId, out grantItem, out error))
                        return Result<CampaignState>.Failure(error);
                }
                else grantItem = CreateGenericRelicItem(instanceId, relic);
                var inventory = new List<EquipmentItemState>(guild.Inventory)
                {
                    grantItem
                };
                guild = guild.With(guild.TreasuryXp, guild.Recruits, guild.Unions,
                    inventory.AsReadOnly(), guild.Development);
                if (p0Ultimate && !TryValidateP0UltimateOwnership(
                        guild, campaign.CampaignGuid, relic.RelicId, out error))
                    return Result<CampaignState>.Failure(error);
            }

            var relicReceipts = new List<CreatorRelicReceipt1000>(access.RelicReceipts1000)
            {
                new CreatorRelicReceipt1000(
                    code.CodeId,
                    bundle.RewardBundleId,
                    relic.RelicId,
                    duplicateConverted,
                    duplicateConverted ? DuplicateMaterialId : string.Empty,
                    materialQuantity,
                    outcomeHash,
                    receiptId)
            };
            access = access.With(
                redeemedCodeIds: AddStable(access.RedeemedCodeIds, code.CodeId),
                appliedReceiptIds: AddStable(access.AppliedReceiptIds, receiptId),
                lastCheckpointId: "relic_code_1000_redeemed",
                relicReceipts1000: relicReceipts.AsReadOnly());

            playable = playable.With(
                creatorAccess028: access,
                replaceCreatorAccess028: true,
                lastCheckpointId: access.LastCheckpointId);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: access.LastCheckpointId);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: access.LastCheckpointId);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: access.LastCheckpointId);
            guild = guild.WithGuildCity(city);
            return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
        }

        public static bool TryResolveOutcome(
            CampaignState campaign,
            RelicCodeRule1000 code,
            RelicRewardBundle1000 bundle,
            ISpecialRelicCatalog001 relicCatalog,
            out SpecialRelicRule001 relic,
            out string outcomeHash,
            out string error)
        {
            relic = null;
            outcomeHash = string.Empty;
            error = string.Empty;
            if (campaign == null || code == null || bundle == null || relicCatalog == null)
            {
                error = "RELICCODE1000_OUTCOME_INPUT_REQUIRED";
                return false;
            }
            outcomeHash = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                code.CodeId,
                BundleId = bundle.RewardBundleId
            }).ToUpperInvariant();

            if (bundle.IsDirect)
            {
                if (!StringComparer.Ordinal.Equals(code.RelicId, bundle.RelicId) ||
                    !relicCatalog.TryGetRelic(bundle.RelicId, out relic) ||
                    !relic.IsPublicCodeEligible)
                {
                    error = "RELICCODE1000_DIRECT_RELIC_INVALID";
                    return false;
                }
                return true;
            }
            if (!bundle.IsCache || !bundle.CommittedOnRedeem)
            {
                error = "RELICCODE1000_CACHE_POLICY_INVALID";
                return false;
            }

            var pool = BuildPool(bundle.Pool, relicCatalog);
            if (pool.Count == 0)
            {
                error = "RELICCODE1000_CACHE_POOL_EMPTY";
                return false;
            }
            var sample = uint.Parse(outcomeHash.Substring(0, 8),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            relic = pool[(int)(sample % (uint)pool.Count)];
            return true;
        }

        private static IReadOnlyList<SpecialRelicRule001> BuildPool(
            string poolId, ISpecialRelicCatalog001 catalog)
        {
            IEnumerable<SpecialRelicRule001> query;
            switch (poolId)
            {
                case "INVOCATION_EPIC_PLUS":
                    query = catalog.AllRelics.Where(value =>
                        StringComparer.Ordinal.Equals(value.Kind, "INVOCATION") &&
                        (StringComparer.Ordinal.Equals(value.Rarity, "Epic") ||
                         StringComparer.Ordinal.Equals(value.Rarity, "Legendary")));
                    break;
                case "ULTIMATE_ART_EPIC_PLUS":
                    query = catalog.AllRelics.Where(value =>
                        StringComparer.Ordinal.Equals(value.Kind, "ULTIMATE_ART") &&
                        (StringComparer.Ordinal.Equals(value.Rarity, "Epic") ||
                         StringComparer.Ordinal.Equals(value.Rarity, "Legendary")));
                    break;
                case "LEGENDARY_RELICS":
                    query = catalog.AllRelics.Where(value =>
                        StringComparer.Ordinal.Equals(value.Rarity, "Legendary"));
                    break;
                case "MYTHIC_WEIGHTED_RELICS":
                    // The pack provides no executable weights. The local adapter is therefore
                    // an explicit deterministic sorted-uniform draw across the 12 Mythic hybrids.
                    query = catalog.AllRelics.Where(value =>
                        StringComparer.Ordinal.Equals(value.Kind, "HYBRID") &&
                        StringComparer.Ordinal.Equals(value.Rarity, "Mythic"));
                    break;
                default: return Array.Empty<SpecialRelicRule001>();
            }
            return query.Where(value => value.IsPublicCodeEligible)
                .OrderBy(value => value.RelicId, StringComparer.Ordinal)
                .ToList().AsReadOnly();
        }

        private static EquipmentItemState CreateGenericRelicItem(
            string instanceId, SpecialRelicRule001 relic)
        {
            return new EquipmentItemState(
                instanceId,
                relic.RelicId,
                relic.Name + " • SEALED",
                new[] { EquipmentSlotIds.ToolRelic },
                new[]
                {
                    RelicEquipmentTag,
                    "RELIC_KIND_" + StableToken(relic.Kind),
                    "RELIC_RARITY_" + StableToken(relic.Rarity),
                    "MANUAL_EQUIP_ONLY",
                    "SEALED_DATA_ONLY"
                },
                "QUALITY_" + StableToken(relic.Rarity),
                10_000,
                false);
        }

        private static bool ReceiptMatches(
            CreatorRelicReceipt1000 receipt,
            RelicCodeRule1000 code,
            RelicRewardBundle1000 bundle,
            SpecialRelicRule001 relic,
            string outcomeHash,
            string receiptId) =>
            receipt != null &&
            StringComparer.Ordinal.Equals(receipt.CodeId, code.CodeId) &&
            StringComparer.Ordinal.Equals(receipt.BundleId, bundle.RewardBundleId) &&
            StringComparer.Ordinal.Equals(receipt.ResolvedRelicId, relic.RelicId) &&
            StringComparer.Ordinal.Equals(receipt.OutcomeHash, outcomeHash) &&
            StringComparer.Ordinal.Equals(receipt.ReceiptId, receiptId) &&
            ReceiptConversionLawMatches(receipt, relic.Rarity);

        private static bool ReceiptConversionLawMatches(
            CreatorRelicReceipt1000 receipt, string rarity)
        {
            if (receipt == null) return false;
            if (!receipt.DuplicateConverted)
                return receipt.MaterialId.Length == 0 && receipt.MaterialQuantity == 0;
            return TryDuplicateQuantity(rarity, out var expectedQuantity) &&
                StringComparer.Ordinal.Equals(receipt.MaterialId, DuplicateMaterialId) &&
                receipt.MaterialQuantity == expectedQuantity;
        }

        private static bool ReceiptMaterialProofMatches(
            GuildCityState017D city,
            IReadOnlyList<CreatorRelicReceipt1000> receipts,
            ISpecialRelicCatalog001 relicCatalog)
        {
            if (receipts == null || relicCatalog == null) return false;
            var required = 0;
            foreach (var receipt in receipts)
            {
                if (receipt == null || !relicCatalog.TryGetRelic(
                        receipt.ResolvedRelicId, out var relic) ||
                    !ReceiptConversionLawMatches(receipt, relic.Rarity)) return false;
                if (receipt.DuplicateConverted)
                    required = checked(required + receipt.MaterialQuantity);
            }
            if (required == 0) return true;
            return city != null && city.Materials.Any(value =>
                StringComparer.Ordinal.Equals(value.MaterialId, DuplicateMaterialId) &&
                value.Amount >= required);
        }

        private static bool TryValidateP0UltimateOwnership(
            GuildState guild, string campaignGuid, string relicId, out string error)
        {
            error = string.Empty;
            if (!SpecialRelicUltimateArtBattleHook001.TryValidateGuildP0SealOwnership(
                    guild, out error)) return false;
            var owned = OwnedItemsByDefinition(guild, relicId);
            var expectedHash = CanonicalJson.Sha256Hex(new
            {
                CampaignGuid = campaignGuid,
                RelicId = relicId,
                UniqueOwnedRelic = true,
                Authority = SpecialRelicUltimateArtBattleHook001.AuthorityVersion
            });
            var expectedInstanceId = "SPECIALRELIC001_" +
                expectedHash.Substring(0, 24).ToUpperInvariant();
            if (owned.Count != 1 ||
                !SpecialRelicUltimateArtBattleHook001.TryValidateP0SealItem(
                    owned[0], out var rule, out error) ||
                !StringComparer.Ordinal.Equals(rule.RelicId, relicId) ||
                !StringComparer.Ordinal.Equals(owned[0].InstanceId, expectedInstanceId) ||
                owned[0].ConditionBasisPoints !=
                    SpecialRelicUltimateArtBattleHook001.GrantConditionBasisPoints)
            {
                if (string.IsNullOrWhiteSpace(error))
                    error = "RELICCODE1000_P0_ULTIMATE_LEDGER_MISMATCH";
                return false;
            }
            return true;
        }

        private static bool TryValidateGenericGrantOwnership(
            GuildState guild,
            string campaignGuid,
            RelicCodeRule1000 code,
            RelicRewardBundle1000 bundle,
            SpecialRelicRule001 relic,
            string receiptId,
            out string error)
        {
            error = string.Empty;
            var owned = OwnedItemsByDefinition(guild, relic.RelicId);
            if (owned.Count != 1)
            {
                error = "RELICCODE1000_GENERIC_ITEM_LEDGER_MISMATCH";
                return false;
            }
            var expected = CreateGenericRelicItem(
                GenericRelicInstanceId(campaignGuid, code, bundle, relic, receiptId), relic);
            if (!StringComparer.Ordinal.Equals(
                    CanonicalJson.Serialize(owned[0]), CanonicalJson.Serialize(expected)))
            {
                error = "RELICCODE1000_GENERIC_ITEM_LEDGER_MISMATCH";
                return false;
            }
            return true;
        }

        private static string GenericRelicInstanceId(
            string campaignGuid,
            RelicCodeRule1000 code,
            RelicRewardBundle1000 bundle,
            SpecialRelicRule001 relic,
            string receiptId)
        {
            var itemHash = CanonicalJson.Sha256Hex(new
            {
                CampaignGuid = campaignGuid,
                code.CodeId,
                bundle.RewardBundleId,
                relic.RelicId,
                receiptId
            });
            return "RELICITEM1000_" + itemHash.Substring(0, 24).ToUpperInvariant();
        }

        private static int CountOwnedDefinition(GuildState guild, string definitionId)
        {
            var count = guild.Inventory.Count(value =>
                StringComparer.Ordinal.Equals(value.DefinitionId, definitionId));
            foreach (var recruit in guild.Recruits)
                count += recruit.Equipment.Assignments.Count(value =>
                    StringComparer.Ordinal.Equals(value.Item.DefinitionId, definitionId));
            return count;
        }

        private static IReadOnlyList<EquipmentItemState> OwnedItemsByDefinition(
            GuildState guild, string definitionId)
        {
            var result = new List<EquipmentItemState>();
            result.AddRange(guild.Inventory.Where(value =>
                StringComparer.Ordinal.Equals(value.DefinitionId, definitionId)));
            foreach (var recruit in guild.Recruits)
                result.AddRange(recruit.Equipment.Assignments
                    .Select(value => value.Item)
                    .Where(value => StringComparer.Ordinal.Equals(
                        value.DefinitionId, definitionId)));
            return result.AsReadOnly();
        }

        private static bool OwnedInstanceIdExists(GuildState guild, string instanceId)
        {
            if (guild.Inventory.Any(value =>
                    StringComparer.Ordinal.Equals(value.InstanceId, instanceId))) return true;
            return guild.Recruits.Any(recruit => recruit.Equipment.Assignments.Any(value =>
                StringComparer.Ordinal.Equals(value.Item.InstanceId, instanceId)));
        }

        private static bool TryDuplicateQuantity(string rarity, out int quantity)
        {
            switch (rarity)
            {
                case "Rare": quantity = 1; return true;
                case "Epic": quantity = 2; return true;
                case "Legendary": quantity = 3; return true;
                case "Mythic": quantity = 4; return true;
                default: quantity = 0; return false;
            }
        }

        private static IReadOnlyList<GuildMaterialState017D> MergeMaterial(
            IReadOnlyList<GuildMaterialState017D> source, string materialId, int amount)
        {
            var result = new List<GuildMaterialState017D>(source ?? Array.Empty<GuildMaterialState017D>());
            var index = result.FindIndex(value =>
                StringComparer.Ordinal.Equals(value.MaterialId, materialId));
            if (index >= 0)
                result[index] = result[index].WithAmount(checked(result[index].Amount + amount));
            else result.Add(new GuildMaterialState017D(materialId, amount));
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.MaterialId, right.MaterialId));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> AddStable(IReadOnlyList<string> source, string value)
        {
            var result = new List<string>(source ?? Array.Empty<string>());
            if (!result.Contains(value)) result.Add(value);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static string StableToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "UNKNOWN";
            var characters = value.ToUpperInvariant().Select(character =>
                (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9')
                    ? character
                    : '_').ToArray();
            return new string(characters);
        }

        private static bool TryContext(
            CampaignState campaign,
            out GuildCityState017D city,
            out GuildCityStrategicState017H strategic,
            out CampaignProgressState019 progress,
            out CampaignPlayableState020 playable,
            out CreatorAccessState028 access,
            out string error)
        {
            city = null;
            strategic = null;
            progress = null;
            playable = null;
            access = null;
            error = string.Empty;
            if (campaign?.Guild?.GuildCity == null)
            {
                error = "RELICCODE1000_CAMPAIGN_REQUIRED";
                return false;
            }
            city = campaign.Guild.GuildCity;
            strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            playable = progress.Playable020 ?? CampaignPlayableState020.Default();
            access = playable.CreatorAccess028 ?? CreatorAccessState028.Default();
            return true;
        }
    }
}
