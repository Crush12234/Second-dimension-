using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.PeopleBonds026;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Creator028
{
    /// <summary>
    /// Applies the additive hash-only giveaway set through the existing campaign state.
    /// No reward is committed until any required target and Creator-power confirmation
    /// are supplied; the returned immutable CampaignState is then saved atomically by M1.
    /// </summary>
    public sealed class CreatorGiveawayCommandService10000
    {
        public const string TargetSeparator = "||";
        public const string CreatorPowerUsedFlag = "CREATOR_POWER_USED";

        public static string NormalizeCode10000(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var builder = new StringBuilder();
            var upper = input.Trim().ToUpperInvariant();
            for (var index = 0; index < upper.Length; index++)
            {
                var character = upper[index];
                if ((character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9'))
                    builder.Append(character);
            }
            return builder.ToString();
        }

        public static string HashCode10000(string input)
        {
            var normalized = NormalizeCode10000(input);
            if (normalized.Length == 0) return string.Empty;
            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                return BitConverter.ToString(digest).Replace("-", string.Empty);
            }
        }

        public static string RequiredTargetMode(CreatorGiveawayRewardBundle10000 bundle)
        {
            if (bundle == null) return string.Empty;
            if (!StringComparer.Ordinal.Equals(bundle.GrantType, "MULTI_GRANT"))
                return IsImmediateTargetMode(bundle.TargetMode) ? bundle.TargetMode : string.Empty;
            var payload = JObject.Parse(bundle.PayloadJson);
            var grants = payload["grants"] as JArray;
            if (grants == null) return string.Empty;
            foreach (var token in grants.OfType<JObject>())
            {
                var mode = (string)token["targetMode"] ?? string.Empty;
                if (IsImmediateTargetMode(mode)) return mode;
            }
            return string.Empty;
        }

        public Result<CampaignState> RedeemCode(
            CampaignState campaign,
            ICreatorGiveawayCatalog10000 catalog,
            string input,
            string targetId,
            bool creatorPowerConfirmed)
        {
            if (!TryContext(campaign, out var city, out var strategic, out var progress,
                    out var playable, out var access, out var error) || catalog == null)
                return Result<CampaignState>.Failure(error ?? "CREATOR10000_INPUT_REQUIRED");

            var hash = HashCode10000(input);
            if (string.IsNullOrEmpty(hash) || !catalog.TryGetCodeByHash(hash, out var code))
                return Result<CampaignState>.Failure("CREATOR10000_CODE_INVALID");
            if (!code.ExactOncePerCampaign || code.AutoEquip)
                return Result<CampaignState>.Failure("CREATOR10000_CODE_POLICY_INVALID");
            if (access.RedeemedCodeIds.Contains(code.CodeId))
                return Result<CampaignState>.Success(campaign);
            if (!catalog.TryGetRewardBundle(code.RewardBundleId, out var bundle))
                return Result<CampaignState>.Failure("CREATOR10000_REWARD_UNKNOWN");

            var requiredTarget = RequiredTargetMode(bundle);
            if (!string.IsNullOrWhiteSpace(requiredTarget) && string.IsNullOrWhiteSpace(targetId))
                return Result<CampaignState>.Failure("CREATOR10000_TARGET_REQUIRED");
            if ((code.CreatorPowerFlag || bundle.CreatorPowerFlag) && !creatorPowerConfirmed)
                return Result<CampaignState>.Failure("CREATOR10000_POWER_CONFIRMATION_REQUIRED");

            var receiptHash = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                code.CodeId,
                code.RewardBundleId,
                code.InstanceSeed
            });
            var receiptId = "CRGIFT10000_" + receiptHash.Substring(0, 24).ToUpperInvariant();
            if (access.AppliedReceiptIds.Contains(receiptId))
                return Result<CampaignState>.Failure("CREATOR10000_LEDGER_MISMATCH");

            var guild = campaign.Guild;
            var bonds = playable.PeopleBonds026 ?? PeopleBondState026.Default();
            var growthAllocations = new List<CreatorGrowthAllocation10000>(access.GrowthAllocations10000);
            var payload = JObject.Parse(bundle.PayloadJson);
            var grantIndex = 0;
            if (StringComparer.Ordinal.Equals(bundle.GrantType, "MULTI_GRANT"))
            {
                var grants = payload["grants"] as JArray;
                if (grants == null || grants.Count == 0)
                    return Result<CampaignState>.Failure("CREATOR10000_MULTI_GRANT_EMPTY");
                foreach (var token in grants)
                {
                    if (!(token is JObject grant))
                        return Result<CampaignState>.Failure("CREATOR10000_MULTI_GRANT_INVALID");
                    var currentGrantIndex = grantIndex++;
                    if (!ApplyGrant(campaign, catalog, code, bundle, grant, targetId, receiptId,
                            currentGrantIndex, ref guild, ref city, ref bonds, out error))
                        return Result<CampaignState>.Failure(error);
                    RecordGrowthAllocation(grant, targetId, receiptId, currentGrantIndex, growthAllocations);
                }
            }
            else if (!ApplyGrant(campaign, catalog, code, bundle, payload, targetId, receiptId,
                         grantIndex, ref guild, ref city, ref bonds, out error))
            {
                return Result<CampaignState>.Failure(error);
            }
            else RecordGrowthAllocation(payload, targetId, receiptId, grantIndex, growthAllocations);

            var contentFlags = access.UnlockedContentIds;
            if (code.CreatorPowerFlag || bundle.CreatorPowerFlag)
                contentFlags = AddStable(contentFlags, CreatorPowerUsedFlag);
            access = access.With(
                redeemedCodeIds: AddStable(access.RedeemedCodeIds, code.CodeId),
                unlockedContentIds: contentFlags,
                appliedReceiptIds: AddStable(access.AppliedReceiptIds, receiptId),
                lastCheckpointId: "creator_giveaway_10000_redeemed",
                growthAllocations10000: growthAllocations.AsReadOnly());

            playable = playable.With(
                creatorAccess028: access,
                replaceCreatorAccess028: true,
                peopleBonds026: bonds,
                replacePeopleBonds026: true,
                lastCheckpointId: access.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: access.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: access.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: access.LastCheckpointId);
            guild = guild.WithGuildCity(city);
            return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
        }

        private static bool ApplyGrant(
            CampaignState campaign,
            ICreatorGiveawayCatalog10000 catalog,
            CreatorGiveawayCodeRule10000 code,
            CreatorGiveawayRewardBundle10000 owningBundle,
            JObject grant,
            string targetId,
            string receiptId,
            int grantIndex,
            ref GuildState guild,
            ref GuildCityState017D city,
            ref PeopleBondState026 bonds,
            out string error)
        {
            error = string.Empty;
            var type = (string)grant["grantType"] ?? (string)grant["type"] ?? string.Empty;
            switch (type)
            {
                case "XP_VOUCHER":
                    return ApplyXpVoucher(grant, targetId, receiptId, ref guild, ref bonds, out error);

                case "GROWTH_BOOST_ITEM":
                case "INVENTORY_ITEM":
                    var itemId = (string)grant["itemId"] ?? owningBundle.PrimaryId;
                    var quantity = Math.Max(1, (int?)grant["quantity"] ?? owningBundle.AmountOrQuantity);
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        error = "CREATOR10000_INVENTORY_ITEM_INVALID";
                        return false;
                    }
                    city = city.With(materials:MergeMaterial(city.Materials, itemId, quantity),
                        lastCheckpointId: "creator_giveaway_inventory_added");
                    return true;

                case "EQUIPMENT_INSTANCE":
                    var templateId = (string)grant["templateId"] ?? owningBundle.PrimaryId;
                    if (!catalog.TryGetEquipmentTemplate(templateId, out var template))
                    {
                        error = "CREATOR10000_EQUIPMENT_TEMPLATE_UNKNOWN";
                        return false;
                    }
                    return AddEquipment(campaign, code, template, grant, receiptId, grantIndex, ref guild, out error);

                default:
                    error = "CREATOR10000_GRANT_TYPE_UNKNOWN";
                    return false;
            }
        }

        private static bool ApplyXpVoucher(
            JObject grant,
            string targetId,
            string receiptId,
            ref GuildState guild,
            ref PeopleBondState026 bonds,
            out string error)
        {
            error = string.Empty;
            var xpType = (string)grant["xpType"] ?? string.Empty;
            var amount = (long?)grant["amount"] ?? 0;
            if (amount <= 0)
            {
                error = "CREATOR10000_XP_AMOUNT_INVALID";
                return false;
            }
            switch (xpType)
            {
                case "PERSONAL_XP":
                    return GrantPersonalXp(targetId, amount, ref guild, out error);
                case "ART_MASTERY_XP":
                    return GrantArtMastery(targetId, amount, false, ref guild, out error);
                case "WEAPON_PROFICIENCY_XP":
                    return GrantArtMastery(targetId, amount, true, ref guild, out error);
                case "UNION_DISCIPLINE_XP":
                    return GrantUnionDiscipline(targetId, amount, ref guild, out error);
                case "RELATIONSHIP_GROWTH_XP":
                    return GrantRelationshipGrowth(targetId, amount, receiptId, guild, ref bonds, out error);
                case "GUILD_TREASURY_XP":
                    GrantDevelopmentXp(amount, 0, ref guild);
                    return true;
                case "HALL_ENHANCEMENT_XP":
                    GrantDevelopmentXp(0, amount, ref guild);
                    return true;
                default:
                    error = "CREATOR10000_XP_TYPE_UNKNOWN";
                    return false;
            }
        }

        private static void RecordGrowthAllocation(
            JObject grant,
            string targetId,
            string receiptId,
            int grantIndex,
            List<CreatorGrowthAllocation10000> allocations)
        {
            var type = (string)grant["grantType"] ?? (string)grant["type"] ?? string.Empty;
            if (!StringComparer.Ordinal.Equals(type, "XP_VOUCHER")) return;
            var xpType = (string)grant["xpType"] ?? string.Empty;
            var amount = (long?)grant["amount"] ?? 0;
            var resolvedTarget = string.IsNullOrWhiteSpace(targetId) ? "GLOBAL:" + xpType : targetId;
            allocations.Add(new CreatorGrowthAllocation10000(
                receiptId + ":" + grantIndex,
                receiptId,
                xpType,
                resolvedTarget,
                amount));
        }

        private static bool GrantPersonalXp(string recruitId, long amount, ref GuildState guild, out string error)
        {
            error = string.Empty;
            var recruits = new List<RecruitState>(guild.Recruits);
            var index = recruits.FindIndex(value => StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            if (index < 0 || recruits[index].AuthorityKind != RecruitAuthorityKind.Normal)
            {
                error = "CREATOR10000_CHARACTER_TARGET_INVALID";
                return false;
            }
            var recruit = recruits[index];
            recruits[index] = recruit.WithProgression(recruit.Progression.GainPersonalXp(amount, recruit.ClassTendencyId));
            guild = guild.With(guild.TreasuryXp, recruits.AsReadOnly(), guild.Unions, guild.Inventory, guild.Development);
            return true;
        }

        private static bool GrantArtMastery(
            string targetId,
            long amount,
            bool weaponTarget,
            ref GuildState guild,
            out string error)
        {
            error = string.Empty;
            var parts = SplitTarget(targetId);
            var expected = weaponTarget ? 3 : 2;
            if (parts.Length != expected)
            {
                error = "CREATOR10000_ART_TARGET_INVALID";
                return false;
            }
            var recruitId = parts[0];
            var artId = parts[parts.Length - 1];
            var recruits = new List<RecruitState>(guild.Recruits);
            var index = recruits.FindIndex(value => StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            if (index < 0 || recruits[index].AuthorityKind != RecruitAuthorityKind.Normal ||
                !recruits[index].Progression.LearnedArtIds.Contains(artId))
            {
                error = "CREATOR10000_ART_TARGET_INVALID";
                return false;
            }
            var recruit = recruits[index];
            var mastery = new List<RecruitArtMasteryState>(recruit.Progression.ArtMastery);
            var masteryIndex = mastery.FindIndex(value => StringComparer.Ordinal.Equals(value.ArtId, artId));
            var current = masteryIndex >= 0 ? mastery[masteryIndex] : null;
            var added = amount > int.MaxValue ? int.MaxValue : (int)amount;
            var points = current == null ? added : (int)Math.Min(int.MaxValue, (long)current.MasteryPoints + added);
            var updated = new RecruitArtMasteryState(artId, current?.Discipline ?? "CREATOR_REWARD",
                current?.MeaningfulUses ?? 0, points);
            if (masteryIndex >= 0) mastery[masteryIndex] = updated;
            else mastery.Add(updated);
            recruits[index] = recruit.WithProgression(recruit.Progression.WithArts(
                recruit.Progression.LearnedArtIds, mastery.AsReadOnly()));
            guild = guild.With(guild.TreasuryXp, recruits.AsReadOnly(), guild.Unions, guild.Inventory, guild.Development);
            return true;
        }

        private static bool GrantUnionDiscipline(string unionId, long amount, ref GuildState guild, out string error)
        {
            error = string.Empty;
            var unions = new List<UnionState>(guild.Unions);
            var index = unions.FindIndex(value => StringComparer.Ordinal.Equals(value.UnionId, unionId));
            if (index < 0 || unions[index].Kind != UnionKind.Normal)
            {
                error = "CREATOR10000_UNION_TARGET_INVALID";
                return false;
            }
            var union = unions[index];
            var cohesionGain = (int)Math.Max(25, Math.Min(2_000, amount / 5));
            unions[index] = new UnionState(union.UnionId, union.DisplayName, union.Kind,
                union.LeaderRecruitId, union.MemberRecruitIds, union.FormationId, union.DoctrineId,
                union.SharedAp, Math.Min(10_000, union.CohesionBasisPoints + cohesionGain), union.MemberPositions);
            guild = guild.With(guild.TreasuryXp, guild.Recruits, unions.AsReadOnly(), guild.Inventory, guild.Development);
            return true;
        }

        private static bool GrantRelationshipGrowth(
            string targetId,
            long amount,
            string receiptId,
            GuildState guild,
            ref PeopleBondState026 bonds,
            out string error)
        {
            error = string.Empty;
            var parts = SplitTarget(targetId);
            if (parts.Length != 2 || StringComparer.Ordinal.Equals(parts[0], parts[1]) ||
                !IsNormalRecruit(guild, parts[0]) || !IsNormalRecruit(guild, parts[1]))
            {
                error = "CREATOR10000_RELATIONSHIP_TARGET_INVALID";
                return false;
            }
            var pairId = BondPairState026.CanonicalPairId(parts[0], parts[1]);
            var pairs = new List<BondPairState026>(bonds.Pairs);
            var index = pairs.FindIndex(value => StringComparer.Ordinal.Equals(value.PairId, pairId));
            var pair = index >= 0
                ? pairs[index]
                : new BondPairState026(pairId, parts[0], parts[1], 0, 0, 0, 0,
                    "BOND_TIER026_0", Array.Empty<string>());
            var gain = (int)Math.Max(1, Math.Min(25, (amount + 999) / 1000));
            var trust = Math.Min(100, pair.Trust + gain);
            var respect = Math.Min(100, pair.Respect + gain);
            var familiarity = Math.Min(100, pair.Familiarity + gain);
            pair = pair.With(trust: trust, respect: respect, familiarity: familiarity,
                tierId: PeopleBondService026.TierId(trust, pair.SharedMemoryCount),
                appliedMemoryReceiptIds: AddStable(pair.AppliedMemoryReceiptIds, receiptId));
            if (index >= 0) pairs[index] = pair;
            else pairs.Add(pair);
            bonds = bonds.With(pairs: pairs.AsReadOnly(),
                appliedReceiptIds: AddStable(bonds.AppliedReceiptIds, receiptId),
                lastCheckpointId: "creator_relationship_growth");
            return true;
        }

        private static void GrantDevelopmentXp(long treasuryXp, long hallXp, ref GuildState guild)
        {
            var development = guild.Development ?? GuildDevelopmentState.Default();
            development = new GuildDevelopmentState(
                development.HallStageIndex,
                development.HallStageId,
                checked(development.HallEnhancementXp + hallXp),
                checked(development.LifetimeTreasuryXpEarned + treasuryXp),
                development.Facilities,
                development.ClaimedBattleRewardIds,
                development.AppliedAdventureAuthorityIds);
            guild = guild.With(checked(guild.TreasuryXp + treasuryXp), guild.Recruits,
                guild.Unions, guild.Inventory, development);
        }

        private static bool AddEquipment(
            CampaignState campaign,
            CreatorGiveawayCodeRule10000 code,
            CreatorGiveawayRewardBundle10000 template,
            JObject grant,
            string receiptId,
            int grantIndex,
            ref GuildState guild,
            out string error)
        {
            error = string.Empty;
            var templatePayload = JObject.Parse(template.PayloadJson);
            var templateId = (string)grant["templateId"] ?? template.PrimaryId;
            var family = (string)templatePayload["weaponFamily"] ?? template.WeaponFamily;
            var pattern = (string)templatePayload["pattern"] ?? template.Pattern;
            var quality = (string)templatePayload["quality"] ?? template.Rarity;
            if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(family) ||
                string.IsNullOrWhiteSpace(pattern) || (bool?)templatePayload["autoEquip"] == true)
            {
                error = "CREATOR10000_EQUIPMENT_POLICY_INVALID";
                return false;
            }

            var itemHash = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                code.CodeId,
                code.InstanceSeed,
                templateId,
                receiptId,
                grantIndex
            });
            var instanceId = "CRITEM10000_" + itemHash.Substring(0, 24).ToUpperInvariant();
            if (guild.Inventory.Any(value => StringComparer.Ordinal.Equals(value.InstanceId, instanceId)) ||
                guild.Recruits.SelectMany(value => value.Equipment.Assignments)
                    .Any(value => StringComparer.Ordinal.Equals(value.Item.InstanceId, instanceId)))
            {
                error = "CREATOR10000_EQUIPMENT_INSTANCE_DUPLICATE";
                return false;
            }

            var tags = WeaponTags(family, pattern, template.PowerMode,
                template.CreatorPowerFlag, (bool?)templatePayload["pvpEligible"]);
            var slots = WeaponSlots(family);
            var item = new EquipmentItemState(instanceId, templateId, template.Label, slots, tags,
                "QUALITY_" + StableToken(quality), 10_000, false);
            var inventory = new List<EquipmentItemState>(guild.Inventory) { item };
            guild = guild.With(guild.TreasuryXp, guild.Recruits, guild.Unions,
                inventory.AsReadOnly(), guild.Development);
            return true;
        }

        private static IReadOnlyList<string> WeaponSlots(string family)
        {
            var result = new List<string> { EquipmentSlotIds.MainHand };
            if (StringComparer.Ordinal.Equals(family, "WF07_SHIELD")) result.Add(EquipmentSlotIds.OffHand);
            if (StringComparer.Ordinal.Equals(family, "WF10_CATALYST_FOCUS") ||
                StringComparer.Ordinal.Equals(family, "WF11_ENGINEERING_TOOL") ||
                StringComparer.Ordinal.Equals(family, "WF12_HYBRID_RELIC"))
                result.Add(EquipmentSlotIds.ToolRelic);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> WeaponTags(
            string family,
            string pattern,
            string powerMode,
            bool creatorPower,
            bool? pvpEligible)
        {
            var result = new List<string>
            {
                family,
                FamilyBattleTag(family),
                "CREATOR_GIVEAWAY",
                "MODIFIED_WEAPON",
                "CREATOR_PATTERN_" + StableToken(pattern),
                "POWER_MODE_" + StableToken(powerMode)
            };
            if (StringComparer.Ordinal.Equals(powerMode, "LEGACY_AWAKENING"))
            {
                result.Add("ULTIMATE_LEGACY");
                result.Add("AWAKENS_THROUGH_STORY_AND_USE");
            }
            if (creatorPower)
            {
                result.Add("CREATOR_OMEGA");
                result.Add("NONCANON_SANDBOX");
                result.Add("PVP_INELIGIBLE");
            }
            else if (pvpEligible == false) result.Add("PVP_INELIGIBLE");
            return result.Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).ToList().AsReadOnly();
        }

        private static string FamilyBattleTag(string family)
        {
            switch (family)
            {
                case "WF01_SWORD": return "SWORD";
                case "WF02_GREAT_WEAPON": return "GREAT_AXE";
                case "WF03_AXE": return "AXE";
                case "WF04_SPEAR_POLEARM": return "POLEARM";
                case "WF05_BOW": return "BOW";
                case "WF06_DAGGER": return "DAGGER";
                case "WF07_SHIELD": return "SHIELD";
                case "WF08_GAUNTLET": return "GAUNTLET";
                case "WF09_STAFF": return "STAFF";
                case "WF10_CATALYST_FOCUS": return "CATALYST";
                case "WF11_ENGINEERING_TOOL": return "ENGINEERING_TOOL";
                case "WF12_HYBRID_RELIC": return "RELIC";
                default: return "WEAPON";
            }
        }

        private static string StableToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "STANDARD";
            var builder = new StringBuilder();
            foreach (var character in value.ToUpperInvariant())
            {
                if ((character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9'))
                    builder.Append(character);
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_') builder.Append('_');
            }
            return builder.ToString().Trim('_');
        }

        private static string[] SplitTarget(string targetId) =>
            (targetId ?? string.Empty).Split(new[] { TargetSeparator }, StringSplitOptions.None);

        private static bool IsImmediateTargetMode(string mode) =>
            !string.IsNullOrWhiteSpace(mode) && mode.StartsWith("PLAYER_SELECT_", StringComparison.Ordinal) &&
            !StringComparer.Ordinal.Equals(mode, "PLAYER_SELECT_TARGET_WHEN_USED");

        private static bool IsNormalRecruit(GuildState guild, string recruitId) =>
            guild.Recruits.Any(value => StringComparer.Ordinal.Equals(value.RecruitId, recruitId) &&
                                        value.AuthorityKind == RecruitAuthorityKind.Normal);

        private static IReadOnlyList<GuildMaterialState017D> MergeMaterial(
            IReadOnlyList<GuildMaterialState017D> existing,
            string itemId,
            int quantity)
        {
            var result = new List<GuildMaterialState017D>(existing ?? Array.Empty<GuildMaterialState017D>());
            var current = result.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.MaterialId, itemId));
            if (current != null)
            {
                result.Remove(current);
                result.Add(current.WithAmount(checked(current.Amount + quantity)));
            }
            else result.Add(new GuildMaterialState017D(itemId, quantity));
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.MaterialId, right.MaterialId));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> AddStable(IReadOnlyList<string> values, string value)
        {
            var result = new List<string>(values ?? Array.Empty<string>());
            if (!string.IsNullOrWhiteSpace(value) && !result.Contains(value)) result.Add(value);
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
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
                error = "CREATOR10000_CAMPAIGN_REQUIRED";
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
