using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Self-authenticating post-battle equipment receipt. ReceiptId and the item
    /// instance are derived from AuthorityHash; reopening results cannot reroll it.
    /// </summary>
    [Serializable]
    public sealed class LootReceipt070
    {
        [JsonConstructor]
        public LootReceipt070(
            string receiptId,
            string authorityHash,
            string campaignGuid,
            long campaignSeed,
            string sourceRewardId,
            string contractId,
            string boardId,
            string encounterId,
            string battleResultHash,
            BattleOutcome outcome,
            int missionRank,
            string weaponDefinitionId,
            string sourceMarkerTag,
            EquipmentItemState equipmentReward)
        {
            ReceiptId = Require(receiptId, nameof(receiptId));
            AuthorityHash = Require(authorityHash, nameof(authorityHash));
            CampaignGuid = Require(campaignGuid, nameof(campaignGuid));
            CampaignSeed = campaignSeed;
            SourceRewardId = Require(sourceRewardId, nameof(sourceRewardId));
            ContractId = Require(contractId, nameof(contractId));
            BoardId = Require(boardId, nameof(boardId));
            EncounterId = Require(encounterId, nameof(encounterId));
            BattleResultHash = Require(battleResultHash, nameof(battleResultHash));
            if (outcome == BattleOutcome.InProgress) throw new ArgumentOutOfRangeException(nameof(outcome));
            if (missionRank < 1 || missionRank > 7) throw new ArgumentOutOfRangeException(nameof(missionRank));
            Outcome = outcome;
            MissionRank = missionRank;
            WeaponDefinitionId = Require(weaponDefinitionId, nameof(weaponDefinitionId));
            SourceMarkerTag = Require(sourceMarkerTag, nameof(sourceMarkerTag));
            EquipmentReward = equipmentReward ?? throw new ArgumentNullException(nameof(equipmentReward));
        }

        public string ReceiptId { get; }
        public string AuthorityHash { get; }
        public string CampaignGuid { get; }
        public long CampaignSeed { get; }
        public string SourceRewardId { get; }
        public string ContractId { get; }
        public string BoardId { get; }
        public string EncounterId { get; }
        public string BattleResultHash { get; }
        public BattleOutcome Outcome { get; }
        public int MissionRank { get; }
        public string WeaponDefinitionId { get; }
        public string SourceMarkerTag { get; }
        public EquipmentItemState EquipmentReward { get; }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable receipt value is required.", parameter)
                : value;
    }

    /// <summary>
    /// Version 70 weapon projection used by inventory, reward and presentation
    /// without exposing CA002 raw IDs to the player-facing surface.
    /// </summary>
    public sealed class WeaponLootDefinition070
    {
        internal WeaponLootDefinition070(
            string definitionId,
            string displayName,
            string weaponFamilyId,
            IReadOnlyList<string> validSlotIds,
            IReadOnlyList<string> equipmentTags,
            string rarity,
            int progressionTier,
            int missionRankMinimum,
            bool ordinaryLootEligible,
            bool foundAsDrop)
        {
            DefinitionId = definitionId;
            DisplayName = displayName;
            WeaponFamilyId = weaponFamilyId;
            ValidSlotIds = validSlotIds;
            EquipmentTags = equipmentTags;
            Rarity = rarity;
            ProgressionTier = progressionTier;
            MissionRankMinimum = missionRankMinimum;
            OrdinaryLootEligible = ordinaryLootEligible;
            FoundAsDrop = foundAsDrop;
        }

        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string WeaponFamilyId { get; }
        public IReadOnlyList<string> ValidSlotIds { get; }
        public IReadOnlyList<string> EquipmentTags { get; }
        public string Rarity { get; }
        public int ProgressionTier { get; }
        public int MissionRankMinimum { get; }
        public bool OrdinaryLootEligible { get; }
        public bool FoundAsDrop { get; }
    }

    /// <summary>
    /// Resolves one deterministic real weapon from WEAPON_CATALOG_264 and applies
    /// it to shared Guild inventory exactly once. The owned equipment instance is
    /// also the durable receipt marker, so later equipping it cannot allow a clone.
    /// </summary>
    public sealed class LootResolver070
    {
        public const string RulesVersion = "LOOT_RESOLVER_070_V1";
        public const int RequiredWeaponAuthorityCount = 264;

        private readonly Dictionary<string, WeaponLootDefinition070> _weapons;
        private readonly IReadOnlyList<WeaponLootDefinition070> _ordinaryDrops;
        private readonly IReadOnlyList<string> _weaponFamilyIds;

        private LootResolver070(Dictionary<string, WeaponLootDefinition070> weapons)
        {
            _weapons = weapons ?? throw new ArgumentNullException(nameof(weapons));
            var ordinary = _weapons.Values
                .Where(value => value.OrdinaryLootEligible && value.FoundAsDrop)
                .OrderBy(value => value.DefinitionId, StringComparer.Ordinal)
                .ToList();
            _ordinaryDrops = ordinary.AsReadOnly();
            var families = _weapons.Values.Select(value => value.WeaponFamilyId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
            _weaponFamilyIds = families.AsReadOnly();
            ValidateAuthority();
        }

        public int WeaponAuthorityCount => _weapons.Count;
        public int OrdinaryDropCount => _ordinaryDrops.Count;
        public int WeaponFamilyCount => _weaponFamilyIds.Count;
        public IReadOnlyList<string> WeaponFamilyIds => _weaponFamilyIds;

        public static LootResolver070 LoadFromContentRoot(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot))
                throw new ArgumentException("Content root is required.", nameof(contentRoot));
            var path = Path.Combine(
                contentRoot,
                "CONTENT_AUTHORITY_002",
                "DATA",
                "WEAPON_CATALOG_264.json");
            return FromJson(File.ReadAllText(path));
        }

        public static LootResolver070 FromJson(string weaponCatalogJson)
        {
            var root = JObject.Parse(weaponCatalogJson ?? throw new ArgumentNullException(nameof(weaponCatalogJson)));
            var values = root["weapons"] as JArray ?? throw new InvalidDataException("weapons array is missing.");
            var weapons = new Dictionary<string, WeaponLootDefinition070>(StringComparer.Ordinal);
            foreach (var token in values)
            {
                var item = token as JObject ?? throw new InvalidDataException("Weapon entry must be an object.");
                var id = Required(item, "id");
                if (weapons.ContainsKey(id)) throw new InvalidDataException("Duplicate weapon ID: " + id + ".");
                var authority = item["contentAuthority002"] as JObject ??
                    throw new InvalidDataException("contentAuthority002 is missing for " + id + ".");
                weapons.Add(id, new WeaponLootDefinition070(
                    id,
                    Required(item, "displayName"),
                    Required(item, "weaponFamilyId"),
                    Strings(item["validSlots"]),
                    Strings(item["equipmentTags"]),
                    Required(authority, "rarity"),
                    RequiredInt(authority, "progressionTier"),
                    RequiredInt(authority, "missionRankMin"),
                    item["ordinaryLootEligible"]?.Value<bool>() ?? false,
                    authority["foundAsDrop"]?.Value<bool>() ?? false));
            }
            return new LootResolver070(weapons);
        }

        public bool ContainsWeapon(string definitionId) =>
            !string.IsNullOrWhiteSpace(definitionId) && _weapons.ContainsKey(definitionId);

        public WeaponLootDefinition070 Weapon(string definitionId) =>
            _weapons.TryGetValue(definitionId ?? string.Empty, out var value)
                ? value
                : throw new KeyNotFoundException("Unknown Version 70 weapon " + (definitionId ?? "<null>") + ".");

        public LootReceipt070 ResolveReceipt(
            CampaignState campaign,
            EncounterLaunchRequest017D encounter,
            BattleState terminalBattle,
            int missionRank)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (encounter == null) throw new ArgumentNullException(nameof(encounter));
            if (terminalBattle == null) throw new ArgumentNullException(nameof(terminalBattle));
            if (terminalBattle.Outcome == BattleOutcome.InProgress || terminalBattle.Reward == null)
                throw new ArgumentException("Terminal battle reward is required.", nameof(terminalBattle));
            return ResolveReceipt(
                campaign,
                encounter.ContractId,
                encounter.BoardId,
                encounter.EncounterId,
                terminalBattle.Reward.RewardId,
                terminalBattle.FinalStateHash,
                terminalBattle.Outcome,
                missionRank);
        }

        public LootReceipt070 ResolveReceipt(
            CampaignState campaign,
            string contractId,
            string boardId,
            string encounterId,
            string sourceRewardId,
            string battleResultHash,
            BattleOutcome outcome,
            int missionRank)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            contractId = Require(contractId, nameof(contractId));
            boardId = Require(boardId, nameof(boardId));
            encounterId = Require(encounterId, nameof(encounterId));
            sourceRewardId = Require(sourceRewardId, nameof(sourceRewardId));
            battleResultHash = Require(battleResultHash, nameof(battleResultHash));
            if (outcome == BattleOutcome.InProgress) throw new ArgumentOutOfRangeException(nameof(outcome));
            if (missionRank < 1 || missionRank > 7) throw new ArgumentOutOfRangeException(nameof(missionRank));

            var eligible = _ordinaryDrops
                .Where(value => value.MissionRankMinimum <= missionRank)
                .ToList();
            if (eligible.Count == 0)
                throw new InvalidOperationException("No ordinary weapon drop is legal for mission rank " + missionRank + ".");
            var highestTier = eligible.Max(value => value.ProgressionTier);
            eligible = eligible
                .Where(value => value.ProgressionTier == highestTier)
                .OrderBy(value => value.DefinitionId, StringComparer.Ordinal)
                .ToList();
            var rng = Pcg32.FromParts(
                RulesVersion,
                campaign.CampaignGuid,
                campaign.CampaignSeed,
                contractId,
                boardId,
                encounterId,
                sourceRewardId,
                battleResultHash,
                outcome,
                missionRank);
            var selected = eligible[(int)rng.NextBounded((uint)eligible.Count)];
            var authorityHash = CommitmentHash(
                campaign.CampaignGuid,
                campaign.CampaignSeed,
                sourceRewardId,
                contractId,
                boardId,
                encounterId,
                battleResultHash,
                outcome,
                missionRank,
                selected.DefinitionId);
            var receiptId = "LOOT_RECEIPT_070_" + authorityHash.Substring(0, 24).ToUpperInvariant();
            var sourceMarker = SourceMarker(campaign.CampaignGuid, sourceRewardId);
            var item = CreateEquipment(selected, authorityHash, receiptId, sourceMarker);
            return new LootReceipt070(
                receiptId,
                authorityHash,
                campaign.CampaignGuid,
                campaign.CampaignSeed,
                sourceRewardId,
                contractId,
                boardId,
                encounterId,
                battleResultHash,
                outcome,
                missionRank,
                selected.DefinitionId,
                sourceMarker,
                item);
        }

        public Result<CampaignState> ApplyExactlyOnce(CampaignState campaign, LootReceipt070 receipt)
        {
            if (campaign == null) return Result<CampaignState>.Failure("LOOT070_CAMPAIGN_REQUIRED");
            if (receipt == null) return Result<CampaignState>.Failure("LOOT070_RECEIPT_REQUIRED");
            try
            {
                if (!StringComparer.Ordinal.Equals(campaign.CampaignGuid, receipt.CampaignGuid) ||
                    campaign.CampaignSeed != receipt.CampaignSeed)
                    return Result<CampaignState>.Failure("LOOT070_RECEIPT_CAMPAIGN_MISMATCH");
                ValidateReceipt(receipt);
                if (!HasClaimedSourceReward(campaign, receipt.SourceRewardId))
                    return Result<CampaignState>.Failure("LOOT070_CLAIM_BATTLE_REWARD_FIRST");
                if (campaign.Guild.GuildCity == null)
                    return Result<CampaignState>.Failure("LOOT070_GUILD_CITY_REQUIRED");

                var appliedReceipts = campaign.Guild.GuildCity.Strategic017H?
                    .AppliedStrategicReceiptIds ?? Array.Empty<string>();
                if (Contains(appliedReceipts, receipt.ReceiptId))
                {
                    var existing = FindOwnedItem(campaign.Guild, receipt.EquipmentReward.InstanceId);
                    return existing == null || SameItem(existing, receipt.EquipmentReward)
                        ? Result<CampaignState>.Success(campaign)
                        : Result<CampaignState>.Failure("LOOT070_RECEIPT_INSTANCE_COLLISION");
                }
                if (Contains(appliedReceipts, receipt.SourceMarkerTag))
                    return Result<CampaignState>.Failure("LOOT070_SOURCE_REWARD_ALREADY_APPLIED");

                var owned = AllOwnedItems(campaign.Guild);
                for (var index = 0; index < owned.Count; index++)
                {
                    var current = owned[index];
                    if (StringComparer.Ordinal.Equals(current.InstanceId, receipt.EquipmentReward.InstanceId))
                    {
                        return SameItem(current, receipt.EquipmentReward)
                            ? Result<CampaignState>.Success(RecordReceipt(campaign, receipt, campaign.Guild.Inventory))
                            : Result<CampaignState>.Failure("LOOT070_RECEIPT_INSTANCE_COLLISION");
                    }
                    if (HasTag(current, receipt.ReceiptId))
                        return Result<CampaignState>.Failure("LOOT070_RECEIPT_MARKER_COLLISION");
                    if (HasTag(current, receipt.SourceMarkerTag))
                        return Result<CampaignState>.Failure("LOOT070_SOURCE_REWARD_ALREADY_APPLIED");
                }

                var inventory = new List<EquipmentItemState>(campaign.Guild.Inventory)
                {
                    receipt.EquipmentReward
                };
                return Result<CampaignState>.Success(RecordReceipt(campaign, receipt, inventory.AsReadOnly()));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure("LOOT070_RECEIPT_REJECTED: " + exception.Message);
            }
        }

        private void ValidateAuthority()
        {
            if (_weapons.Count != RequiredWeaponAuthorityCount)
                throw new InvalidDataException(
                    "Version 70 requires exactly 264 weapon definitions; found " + _weapons.Count + ".");
            if (_ordinaryDrops.Count == 0)
                throw new InvalidDataException("Version 70 weapon authority has no ordinary drops.");
            if (_weaponFamilyIds.Count != 12)
                throw new InvalidDataException("Version 70 requires all 12 weapon families.");
            foreach (var weapon in _weapons.Values)
            {
                if (weapon.ValidSlotIds == null || weapon.ValidSlotIds.Count == 0)
                    throw new InvalidDataException("Weapon has no valid slot: " + weapon.DefinitionId + ".");
                for (var index = 0; index < weapon.ValidSlotIds.Count; index++)
                    if (!EquipmentSlotIds.IsOpeningSlot(weapon.ValidSlotIds[index]))
                        throw new InvalidDataException(
                            "Weapon has unsupported inventory slot " + weapon.ValidSlotIds[index] + ".");
            }
        }

        private void ValidateReceipt(LootReceipt070 receipt)
        {
            if (!_weapons.TryGetValue(receipt.WeaponDefinitionId, out var weapon))
                throw new InvalidDataException("Receipt weapon is not in WEAPON_CATALOG_264.");
            var expectedHash = CommitmentHash(
                receipt.CampaignGuid,
                receipt.CampaignSeed,
                receipt.SourceRewardId,
                receipt.ContractId,
                receipt.BoardId,
                receipt.EncounterId,
                receipt.BattleResultHash,
                receipt.Outcome,
                receipt.MissionRank,
                receipt.WeaponDefinitionId);
            if (!StringComparer.Ordinal.Equals(expectedHash, receipt.AuthorityHash))
                throw new InvalidDataException("Receipt authority hash is invalid.");
            var expectedReceiptId = "LOOT_RECEIPT_070_" + expectedHash.Substring(0, 24).ToUpperInvariant();
            if (!StringComparer.Ordinal.Equals(expectedReceiptId, receipt.ReceiptId))
                throw new InvalidDataException("Receipt ID is invalid.");
            var expectedSourceMarker = SourceMarker(receipt.CampaignGuid, receipt.SourceRewardId);
            if (!StringComparer.Ordinal.Equals(expectedSourceMarker, receipt.SourceMarkerTag))
                throw new InvalidDataException("Receipt source marker is invalid.");
            var expectedItem = CreateEquipment(weapon, expectedHash, expectedReceiptId, expectedSourceMarker);
            if (!SameItem(expectedItem, receipt.EquipmentReward))
                throw new InvalidDataException("Receipt equipment projection is invalid.");
        }

        private static EquipmentItemState CreateEquipment(
            WeaponLootDefinition070 weapon,
            string authorityHash,
            string receiptId,
            string sourceMarker)
        {
            var tags = new List<string>(weapon.EquipmentTags);
            AddUnique(tags, "WEAPON");
            AddUnique(tags, weapon.WeaponFamilyId);
            AddUnique(tags, receiptId);
            AddUnique(tags, sourceMarker);
            tags.Sort(StringComparer.Ordinal);
            return new EquipmentItemState(
                "LOOT_ITEM_070_" + authorityHash.Substring(0, 24).ToUpperInvariant(),
                weapon.DefinitionId,
                weapon.DisplayName,
                weapon.ValidSlotIds,
                tags.AsReadOnly(),
                "QUALITY_" + weapon.Rarity,
                10000,
                false);
        }

        private static string CommitmentHash(
            string campaignGuid,
            long campaignSeed,
            string sourceRewardId,
            string contractId,
            string boardId,
            string encounterId,
            string battleResultHash,
            BattleOutcome outcome,
            int missionRank,
            string weaponDefinitionId) =>
            CanonicalJson.Sha256Hex(new
            {
                RulesVersion,
                campaignGuid,
                campaignSeed,
                sourceRewardId,
                contractId,
                boardId,
                encounterId,
                battleResultHash,
                Outcome = outcome.ToString(),
                missionRank,
                weaponDefinitionId
            });

        private static string SourceMarker(string campaignGuid, string sourceRewardId)
        {
            var hash = CanonicalJson.Sha256Hex(new
            {
                RulesVersion,
                campaignGuid,
                sourceRewardId,
                Kind = "LOOT_SOURCE"
            });
            return "LOOT_SOURCE_070_" + hash.Substring(0, 20).ToUpperInvariant();
        }

        private static bool HasClaimedSourceReward(CampaignState campaign, string sourceRewardId)
        {
            if (campaign.Guild.Development.HasClaimedReward(sourceRewardId)) return true;
            var reward = campaign.Battle?.Reward;
            return reward != null && reward.Claimed &&
                StringComparer.Ordinal.Equals(reward.RewardId, sourceRewardId);
        }

        private static CampaignState RecordReceipt(
            CampaignState campaign,
            LootReceipt070 receipt,
            IReadOnlyList<EquipmentItemState> inventory)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var applied = new List<string>(strategic.AppliedStrategicReceiptIds);
            if (!applied.Contains(receipt.ReceiptId)) applied.Add(receipt.ReceiptId);
            if (!applied.Contains(receipt.SourceMarkerTag)) applied.Add(receipt.SourceMarkerTag);
            applied.Sort(StringComparer.Ordinal);
            strategic = strategic.With(
                appliedStrategicReceiptIds: applied.AsReadOnly(),
                lastCheckpointId: "loot070_applied:" + receipt.ReceiptId);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: "loot070_applied");
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                inventory,
                campaign.Guild.Development).WithGuildCity(city);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static EquipmentItemState FindOwnedItem(GuildState guild, string instanceId)
        {
            var values = AllOwnedItems(guild);
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index].InstanceId, instanceId)) return values[index];
            return null;
        }

        private static IReadOnlyList<EquipmentItemState> AllOwnedItems(GuildState guild)
        {
            var result = new List<EquipmentItemState>(guild.Inventory);
            for (var recruitIndex = 0; recruitIndex < guild.Recruits.Count; recruitIndex++)
            {
                var assignments = guild.Recruits[recruitIndex].Equipment.Assignments;
                for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
                    result.Add(assignments[assignmentIndex].Item);
            }
            return result.AsReadOnly();
        }

        private static bool HasTag(EquipmentItemState item, string tag)
        {
            for (var index = 0; index < item.EquipmentTags.Count; index++)
                if (StringComparer.Ordinal.Equals(item.EquipmentTags[index], tag)) return true;
            return false;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (values == null) return false;
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index], value)) return true;
            return false;
        }

        private static bool SameItem(EquipmentItemState left, EquipmentItemState right) =>
            left != null && right != null &&
            StringComparer.Ordinal.Equals(
                CanonicalJson.Sha256Hex(left),
                CanonicalJson.Sha256Hex(right));

        private static void AddUnique(ICollection<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || values.Contains(value)) return;
            values.Add(value);
        }

        private static string Required(JObject item, string name)
        {
            var value = item[name]?.Value<string>();
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidDataException(name + " is missing.")
                : value;
        }

        private static int RequiredInt(JObject item, string name) =>
            item[name]?.Type == JTokenType.Integer
                ? item[name].Value<int>()
                : throw new InvalidDataException(name + " is missing.");

        private static IReadOnlyList<string> Strings(JToken token)
        {
            var result = new List<string>();
            if (token is JArray values)
            {
                foreach (var item in values)
                {
                    var value = item.Value<string>();
                    if (string.IsNullOrWhiteSpace(value))
                        throw new InvalidDataException("Stable ID array contains an empty value.");
                    result.Add(value);
                }
            }
            return result.AsReadOnly();
        }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable value is required.", parameter)
                : value;
    }
}
