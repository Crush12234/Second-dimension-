using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    /// <summary>
    /// One authoritative choice from the physical three-card row used by the
    /// opening Guild quests. The authored board still owns story nodes and
    /// locked battles; these cards add the repeatable reward layer around it.
    /// </summary>
    public sealed class GuildQuestCardOffer090
    {
        public string ReceiptId165 { get; set; }
        public string EffectKind165 { get; set; }
        public int D20165 { get; set; }
        public int WheelSector165 { get; set; } = -1;
        public bool EffectRolled165 { get; set; }
        public bool Lucky165 { get; set; }
        public string CardId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RewardPreview { get; set; }
        public string RiskLabel { get; set; }
        public string DestinationNodeId { get; set; }
        public string DestinationLabel { get; set; }
        public string RarityId { get; set; }
        public string ItemName { get; set; }
        public int PhysicalPower { get; set; }
        public int MysticPower { get; set; }
        public int TreasuryXpDelta { get; set; }
        public int TreasuryXpCost { get; set; }
        public int SupplyDelta { get; set; }
        public int FatigueDelta { get; set; }
        public int ThreatDelta { get; set; }
        public int CheckModifierDelta { get; set; }
        public int DieOne { get; set; }
        public int DieTwo { get; set; }
        public int Target { get; set; }
        public int FateCheckModifier { get; set; }
        public string EncounterId { get; set; }
        public int EnemyUnionCount { get; set; }
        public bool IsPermanentHeroBoon { get; set; }
        public string HeroRecruitId { get; set; }
        public string HeroName { get; set; }
        public string RecruitApplicantId { get; set; }
        public bool CanChoose { get; set; }
        public string LockedReason { get; set; }
    }

    public sealed partial class GuildCityExpeditionService017D
    {
        public const int MinimumQuestCardRounds090 = 10;
        public const string QuestCardReceiptPrefix090 = "QUEST_CARD090_APPLIED_";
        public const string QuestCardRunBoonPrefix090 = "QUEST_CARD090_RUN_BOON_";
        public const string QuestCardRunScarPrefix090 = "QUEST_CARD090_RUN_SCAR_";

        static readonly string[][] QuestCardSchedule090 =
        {
            new[] { "CHEST", "FATE", "BOON" },
            new[] { "MERCHANT", "RECRUIT", "BATTLE" },
            new[] { "CHEST", "SCAR", "XP" },
            new[] { "BOON", "MERCHANT", "FATE" },
            new[] { "RECRUIT", "BATTLE", "XP" },
            // One occasional free invitation chance among ten three-card rows.
            // Existing RECRUIT entries remain normal XP-paid signing offers.
            new[] { "FATE", "FREE_RECRUIT", "BOON" },
            new[] { "CHEST", "MERCHANT", "SCAR" },
            new[] { "RECRUIT", "XP", "BATTLE" },
            new[] { "BOON", "CHEST", "SCAR" },
            new[] { "MERCHANT", "BATTLE", "XP" }
        };

        public IReadOnlyList<GuildQuestCardOffer090> BuildQuestCardRow090(
            CampaignState campaign,
            GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment = null)
        {
            var expedition = campaign?.Guild?.GuildCity?.Expedition;
            if (campaign?.Guild == null || content == null || expedition == null ||
                expedition.Status != ExpeditionStatus017D.Active ||
                !UsesBoardQuestRewards081(expedition.BoardId) ||
                expedition.PendingQuestFate165 != null ||
                campaign.Guild.GuildCity.PendingEncounter != null ||
                campaign.Guild.GuildCity.PendingBattleReturn != null)
                return Array.Empty<GuildQuestCardOffer090>();

            var board = content.Board(expedition.BoardId);
            var current = board.Node(expedition.CurrentNodeId);
            if (!IsCurrentNodeResolved(expedition, current) ||
                current.Links == null || current.Links.Length == 0)
                return Array.Empty<GuildQuestCardOffer090>();
            if (StringComparer.Ordinal.Equals(current.Kind, "OPTIONAL_ELITE") &&
                !IsEncounterCleared(expedition, current))
                return Array.Empty<GuildQuestCardOffer090>();

            var round = QuestCardRoundCount090(expedition.ObjectiveFlags);
            var categories = QuestCardSchedule090[round % QuestCardSchedule090.Length];
            var destinations = current.Links
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => BoardRoomShuffleKey081(
                    expedition.ExpeditionId, expedition.CurrentNodeId, value),
                    StringComparer.Ordinal)
                .ToArray();
            if (destinations.Length == 0)
                return Array.Empty<GuildQuestCardOffer090>();

            var result = new List<GuildQuestCardOffer090>(3);
            for (var index = 0; index < 3; index++)
            {
                var category = categories[index];
                var destination = destinations[index % destinations.Length];
                var offer = BuildQuestCardOffer090(
                    campaign, content, recruitment, expedition, round, index,
                    category, destination);
                result.Add(offer);
            }
            return result.AsReadOnly();
        }

        public Result<CampaignState> CommitQuestCard090(
            CampaignState campaign,
            GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment,
            string cardId)
        {
            if (campaign == null || content == null)
                return Result<CampaignState>.Failure("QUEST_CARD090_INPUT_REQUIRED");
            if (string.IsNullOrWhiteSpace(cardId))
                return Result<CampaignState>.Failure("QUEST_CARD090_CARD_REQUIRED");

            var row = BuildQuestCardRow090(campaign, content, recruitment);
            var card = row.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.CardId, cardId));
            if (card == null)
                return Result<CampaignState>.Failure("QUEST_CARD090_NOT_IN_CURRENT_ROW");
            if (!card.CanChoose)
                return Result<CampaignState>.Failure(
                    string.IsNullOrWhiteSpace(card.LockedReason)
                        ? "QUEST_CARD090_CHOICE_LOCKED"
                        : card.LockedReason);

            if (!string.IsNullOrEmpty(card.EffectKind165))
                return SealQuestFate165(campaign, card);
            return ApplyQuestCard165(campaign, content, recruitment, card);
        }

        private Result<CampaignState> ApplyQuestCard165(CampaignState campaign,
            GuildCityContent017D content, GuildCityRecruitmentService017D recruitment,
            GuildQuestCardOffer090 card)
        {
            var receiptId = QuestCardReceiptPrefix090 + card.CardId;
            if (campaign.Guild.Development.HasAdventureAuthority(receiptId))
                return Result<CampaignState>.Failure(
                    "QUEST_CARD090_RECEIPT_ALREADY_APPLIED");
            if (!campaign.Guild.Development.CanRecordAdventureAuthority(receiptId))
                return Result<CampaignState>.Failure(
                    "QUEST_CARD090_ADVENTURE_AUTHORITY_LEDGER_FULL");

            var candidate = campaign;
            if (StringComparer.Ordinal.Equals(card.Category, "RECRUIT"))
            {
                if (recruitment == null || string.IsNullOrWhiteSpace(
                        card.RecruitApplicantId))
                    return Result<CampaignState>.Failure(
                        "QUEST_CARD090_RECRUITMENT_UNAVAILABLE");
                var signed = recruitment.SignApplicant(
                    candidate, card.RecruitApplicantId);
                if (!signed.IsSuccess)
                    return Result<CampaignState>.Failure(signed.Errors.ToArray());
                candidate = signed.Value;
            }

            var moved = CommitMove(candidate, content, card.DestinationNodeId);
            if (!moved.IsSuccess)
                return Result<CampaignState>.Failure(moved.Errors.ToArray());
            candidate = moved.Value;

            var guild = candidate.Guild;
            var city = guild.GuildCity;
            var expedition = city.Expedition;
            var objectives = AddUnique(expedition.ObjectiveFlags, receiptId);
            var treasury = guild.TreasuryXp;
            var inventory = new List<EquipmentItemState>(guild.Inventory);
            var recruits = new List<RecruitState>(guild.Recruits);

            if (card.TreasuryXpCost > 0)
            {
                if (treasury < card.TreasuryXpCost)
                    return Result<CampaignState>.Failure(
                        "QUEST_CARD090_TREASURY_XP_INSUFFICIENT");
                treasury -= card.TreasuryXpCost;
            }
            treasury = checked(treasury + Math.Max(0, card.TreasuryXpDelta));

            if (StringComparer.Ordinal.Equals(card.Category, "CHEST") ||
                StringComparer.Ordinal.Equals(card.Category, "MERCHANT"))
            {
                var item = CreateQuestCardEquipment090(card.CardId, receiptId,
                    StringComparer.Ordinal.Equals(card.Category, "MERCHANT"));
                if (!inventory.Any(value => value != null &&
                    StringComparer.Ordinal.Equals(value.InstanceId,
                        item.InstanceId)))
                    inventory.Add(item);
            }

            for (var effectIndex = 0; effectIndex < Math.Abs(card.CheckModifierDelta); effectIndex++)
                objectives = AddUnique(objectives,
                    (card.CheckModifierDelta > 0 ? QuestCardRunBoonPrefix090 : QuestCardRunScarPrefix090) +
                    card.CardId + (effectIndex == 0 ? "" : "_" + effectIndex));

            if (card.IsPermanentHeroBoon &&
                !string.IsNullOrWhiteSpace(card.HeroRecruitId))
            {
                for (var index = 0; index < recruits.Count; index++)
                {
                    var recruit = recruits[index];
                    if (recruit == null || !StringComparer.Ordinal.Equals(
                            recruit.RecruitId, card.HeroRecruitId)) continue;
                    var progression = recruit.Progression ??
                                      RecruitProgressionState.Default();
                    recruits[index] = recruit.WithProgression(
                        new RecruitProgressionState(
                            progression.Level,
                            progression.TotalPersonalXp,
                            checked(progression.MaximumHpBonus + 8),
                            checked(progression.MaximumMpBonus + 2),
                            checked(progression.StrengthBonus + 1),
                            checked(progression.DefenseBonus + 1),
                            checked(progression.AgilityBonus + 1),
                            checked(progression.MagicBonus + 1),
                            checked(progression.WillBonus + 1),
                            progression.LearnedArtIds,
                            progression.ArtMastery,
                            progression.UnlockedTreeIds,
                            progression.AscensionLevel, progression.ProgressionVersion152, progression.CatchUpOriginXp152));
                    objectives = AddUnique(objectives,
                        "QUEST_CARD090_PERMANENT_BOON_" + card.CardId + "_" +
                        card.HeroRecruitId);
                    break;
                }
            }

            var updatedExpedition = expedition.With(
                supplies: Math.Max(0, checked(expedition.Supplies +
                                               card.SupplyDelta)),
                fatigue: Math.Max(0, checked(expedition.Fatigue +
                                              card.FatigueDelta)),
                threat: Math.Max(0, checked(expedition.Threat +
                                             card.ThreatDelta)),
                objectiveFlags: objectives,
                lastCheckpointId: "quest_card_090_" + card.CardId);
            var development = guild.Development.RecordAdventureAuthority(receiptId);

            EncounterLaunchRequest017D pendingEncounter = null;
            if (StringComparer.Ordinal.Equals(card.Category, "BATTLE"))
            {
                if (city.ActiveContract == null)
                    return Result<CampaignState>.Failure(
                        "QUEST_CARD090_ACTIVE_CONTRACT_REQUIRED");
                var alliedUnionIds = UnionBattlePlanRules132.Read(candidate)
                    .Where(value => value != null &&
                                    value.Kind == UnionKind.Normal &&
                                    value.MemberRecruitIds.Count > 0)
                    .Take(10)
                    .Select(value => value.UnionId)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (alliedUnionIds.Length == 0)
                    return Result<CampaignState>.Failure(
                        "QUEST_CARD090_ALLIED_UNION_REQUIRED");

                var battleHash = CanonicalJson.Sha256Hex(new
                {
                    Rule = "QUEST_CARD_RANDOM_BATTLE_090",
                    candidate.CampaignSeed,
                    card.CardId,
                    card.EncounterId,
                    card.EnemyUnionCount,
                    updatedExpedition.ExpeditionId,
                    updatedExpedition.CurrentNodeId,
                    AlliedUnionIds = alliedUnionIds
                });
                var virtualNodeId = "QUEST_CARD_BATTLE_NODE090_" +
                                    battleHash.Substring(0, 16).ToUpperInvariant();
                var routeModifiers = new List<string>
                {
                    "QUEST_CARD_RANDOM_BATTLE_090"
                };
                if (updatedExpedition.Fatigue >= 8)
                    routeModifiers.Add("HIGH_FATIGUE");
                if (updatedExpedition.Urgency <= 4)
                    routeModifiers.Add("URGENT_OBJECTIVE");
                pendingEncounter = new EncounterLaunchRequest017D(
                    "ENCOUNTER_REQUEST_" +
                    battleHash.Substring(0, 24).ToUpperInvariant(),
                    city.ActiveContract.ContractId,
                    updatedExpedition.ExpeditionId,
                    updatedExpedition.BoardId,
                    virtualNodeId,
                    card.EncounterId,
                    "BATTLE_QUEST_CARD090_" +
                    battleHash.Substring(0, 20).ToUpperInvariant(),
                    "Defeat the wandering threat revealed by the card, then continue the quest.",
                    Math.Max(1, Math.Min(10, card.EnemyUnionCount)),
                    battleHash,
                    alliedUnionIds,
                    Array.Empty<string>(),
                    new[] { "OBJECTIVE_QUEST_CARD_BATTLE_090" },
                    routeModifiers.AsReadOnly(),
                    updatedExpedition.Supplies,
                    updatedExpedition.Fatigue,
                    updatedExpedition.Urgency,
                    "RETURN_QUEST_CARD090_" +
                    battleHash.Substring(0, 20).ToUpperInvariant(),
                    CanonicalJson.Sha256Hex(updatedExpedition));
                var requestAuthority = GuildCityBattleBridgeService017D
                    .EncounterRequestAuthorityId084(pendingEncounter);
                if (development.HasAdventureAuthority(requestAuthority))
                    return Result<CampaignState>.Failure(
                        "QUEST_CARD090_BATTLE_AUTHORITY_ALREADY_COMMITTED");
                if (!development.CanRecordAdventureAuthority(requestAuthority))
                    return Result<CampaignState>.Failure(
                        "QUEST_CARD090_ADVENTURE_AUTHORITY_LEDGER_FULL");
                development = development.RecordAdventureAuthority(requestAuthority);
                updatedExpedition = updatedExpedition.With(
                    status: ExpeditionStatus017D.AwaitingBattle,
                    lastCheckpointId: "quest_card_090_battle_committed");
            }
            city = city.With(
                expedition: updatedExpedition,
                replaceExpedition: true,
                pendingEncounter: pendingEncounter,
                replacePendingEncounter: pendingEncounter != null,
                lastCheckpointId: pendingEncounter == null
                    ? "quest_card_090_applied"
                    : "quest_card_090_battle_launch_committed");
            guild = guild.With(
                treasury,
                recruits.AsReadOnly(),
                guild.Unions,
                inventory.AsReadOnly(),
                development).WithGuildCity(city);
            var committedCard = candidate.With(guild, candidate.OpeningFlow);
            if (StringComparer.Ordinal.Equals(card.Category, "CHEST"))
            {
                // Existing heroes retain their gear; newcomer handling remains below.
                if (recruitment != null)
                    committedCard = recruitment.ApplyChestRecruit092(committedCard, card.CardId);
            }
            if (StringComparer.Ordinal.Equals(card.Category, "FREE_RECRUIT"))
            {
                // The normal move/card receipt is authoritative. The shared
                // invitation service reprojects the original row and records a
                // winning promise atomically; it never hires during an adventure.
                return recruitment.RecordEarnedQuestCardRecruit094(
                    committedCard, campaign, content, card.CardId);
            }
            return Result<CampaignState>.Success(committedCard);
        }

        public static int QuestCardRoundCount090(
            IReadOnlyList<string> objectiveFlags)
        {
            var flags = objectiveFlags ?? Array.Empty<string>();
            var count = 0;
            for (var index = 0; index < flags.Count; index++)
                if ((flags[index] ?? string.Empty).StartsWith(
                        QuestCardReceiptPrefix090, StringComparison.Ordinal))
                    count++;
            return count;
        }

        public static int QuestCardRunCheckModifier090(
            IReadOnlyList<string> objectiveFlags)
        {
            var flags = objectiveFlags ?? Array.Empty<string>();
            var boons = 0;
            var scars = 0;
            for (var index = 0; index < flags.Count; index++)
            {
                var value = flags[index] ?? string.Empty;
                if (value.StartsWith(QuestCardRunBoonPrefix090,
                        StringComparison.Ordinal)) boons++;
                if (value.StartsWith(QuestCardRunScarPrefix090,
                        StringComparison.Ordinal)) scars++;
            }
            return Math.Max(-3, Math.Min(3, boons - scars));
        }

        public static EquipmentItemState CreateQuestCardEquipment090(
            string cardId,
            string receiptId,
            bool merchant)
        {
            var hash = CanonicalJson.Sha256Hex(new
            {
                Rule = "QUEST_CARD_EQUIPMENT_090",
                Card = cardId ?? string.Empty,
                Receipt = receiptId ?? string.Empty,
                Merchant = merchant
            });
            var roll = Convert.ToInt32(hash.Substring(0, 4), 16);
            var qualityId = roll < 32768 ? "QUALITY_COMMON" :
                roll < 49152 ? "QUALITY_UNCOMMON" :
                roll < 57344 ? "QUALITY_RARE" :
                roll < 62464 ? "QUALITY_EPIC" :
                roll < 64880 ? "QUALITY_LEGENDARY" : "QUALITY_GODLY";
            if (merchant && (StringComparer.Ordinal.Equals(
                    qualityId, "QUALITY_COMMON") ||
                StringComparer.Ordinal.Equals(
                    qualityId, "QUALITY_UNCOMMON")))
                qualityId = "QUALITY_RARE";
            var qualityName = qualityId.Substring("QUALITY_".Length);
            var kind = Convert.ToInt32(hash.Substring(4, 2), 16) % 4;
            string definition;
            string name;
            string slot;
            string[] tags;
            switch (kind)
            {
                case 0:
                    definition = "QUEST090_SKYGLASS_BLADE";
                    name = "Skyglass Blade";
                    slot = EquipmentSlotIds.MainHand;
                    tags = new[] { "QUEST_CARD_090", "WF01_SWORD", "SWORD" };
                    break;
                case 1:
                    definition = "QUEST090_STARLANTERN_STAFF";
                    name = "Starlantern Staff";
                    slot = EquipmentSlotIds.MainHand;
                    tags = new[] { "QUEST_CARD_090", "WF09_STAFF", "STAFF" };
                    break;
                case 2:
                    definition = "QUEST090_RIFTWARD_COAT";
                    name = "Riftward Coat";
                    slot = EquipmentSlotIds.BodyArmor;
                    tags = new[] { "QUEST_CARD_090", "ARMOR" };
                    break;
                default:
                    definition = "QUEST090_WAYFINDER_RELIC";
                    name = "Wayfinder Relic";
                    slot = EquipmentSlotIds.AccessoryOne;
                    tags = new[] { "QUEST_CARD_090", "WF12_HYBRID_RELIC", "RELIC" };
                    break;
            }
            var identity = new string((receiptId ?? hash)
                .Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (identity.Length > 28)
                identity = identity.Substring(identity.Length - 28);
            return new EquipmentItemState(
                "LOOT_ITEM_070_QUEST090_" + identity,
                definition + "_" + qualityName,
                FriendlyQuality090(qualityName) + " " + name,
                new[] { slot }, tags, qualityId, 10000, false);
        }

        GuildQuestCardOffer090 BuildQuestCardOffer090(
            CampaignState campaign,
            GuildCityContent017D content,
            GuildCityRecruitmentService017D recruitment,
            ExpeditionState017D expedition,
            int round,
            int index,
            string requestedCategory,
            string destinationNodeId)
        {
            var category = requestedCategory;
            var destination = content.Board(expedition.BoardId)
                .Node(destinationNodeId);
            if (StringComparer.Ordinal.Equals(category, "BATTLE") &&
                StringComparer.Ordinal.Equals(destination.Kind, "EXIT"))
                category = "XP";
            var applicant = category == "RECRUIT"
                ? AvailableQuestApplicant090(campaign, recruitment, round)
                : null;
            if (category == "RECRUIT" && applicant == null)
                category = "XP";
            var freeRecruit094 = category == "FREE_RECRUIT" && recruitment != null
                ? recruitment.PreviewFreeQuestRecruit094(campaign, CanonicalJson.Sha256Hex(new
                {
                    Rule = "OPENING_QUEST_FREE_RECRUIT_SELECTION_094",
                    campaign.CampaignGuid,
                    campaign.CampaignSeed,
                    expedition.ExpeditionId,
                    expedition.CurrentNodeId,
                    Round = round,
                    Slot = index,
                    Destination = destinationNodeId
                }))
                : null;
            // Old/catalog-less contexts retain this slot's original XP card.
            if (category == "FREE_RECRUIT" && freeRecruit094 == null)
                category = "XP";
            var targetSelectionHash = CanonicalJson.Sha256Hex(new
            {
                Rule = "FIRST_HOUR_QUEST_DECK_TARGET_090",
                expedition.ExpeditionId,
                expedition.CurrentNodeId,
                Round = round,
                Slot = index,
                Category = category,
                Destination = destinationNodeId
            });
            var boonHero = StringComparer.Ordinal.Equals(category, "BOON")
                ? QuestCardHero090(campaign, targetSelectionHash)
                : null;
            var permanentBoon = boonHero != null &&
                                Convert.ToInt32(
                                    targetSelectionHash.Substring(8, 2), 16) < 28;
            var hash = CanonicalJson.Sha256Hex(new
            {
                Rule = "FIRST_HOUR_QUEST_DECK_090",
                expedition.ExpeditionId,
                expedition.CurrentNodeId,
                Round = round,
                Slot = index,
                Category = category,
                Destination = destinationNodeId,
                Applicant = QuestApplicantIdentity090(campaign, applicant),
                PermanentBoonHero = permanentBoon
                    ? boonHero.RecruitId
                    : string.Empty
            });
            if (category == "FREE_RECRUIT")
                hash = CanonicalJson.Sha256Hex(new
                {
                    Rule = "OPENING_QUEST_FREE_RECRUIT_IDENTITY_094",
                    OriginalCardHash = hash,
                    StableId = freeRecruit094.StableId
                });
            // Do not change existing categories' hashes or historical receipts.
            var cardId = "QUESTCARD090_" + hash.Substring(0, 24).ToUpperInvariant();
            var offer = new GuildQuestCardOffer090
            {
                CardId = cardId,
                Category = category,
                DestinationNodeId = destinationNodeId,
                DestinationLabel = string.IsNullOrWhiteSpace(
                    destination.DisplayName)
                    ? "A new room"
                    : destination.DisplayName,
                CanChoose = true,
                RiskLabel = "SAFE",
                Title = "Road to " + (destination.DisplayName ?? destinationNodeId),
                Description = destination.Summary ??
                              "Choose this card to move the Guild pawn forward.",
                RewardPreview = "Advance the story",
                Target = 7
            };

            switch (category)
            {
                case "CHEST":
                {
                    var item = CreateQuestCardEquipment090(cardId,
                        QuestCardReceiptPrefix090 + cardId, false);
                    var power = M2EquipmentPowerPolicy087.Resolve(item);
                    offer.RarityId = item.QualityId;
                    offer.ItemName = item.DisplayName;
                    offer.PhysicalPower = power.PhysicalAttack;
                    offer.MysticPower = power.MysticAttack;
                    offer.TreasuryXpDelta = 4;
                    offer.Title = FriendlyQuality090(item.QualityId) + " Chest";
                    offer.Description = "Open a physical treasure chest, keep the equipment permanently, then move the pawn.";
                    offer.RewardPreview = item.DisplayName + "  •  PWR +" +
                        power.PhysicalAttack + "  •  MYS +" + power.MysticAttack +
                        "  •  +4 XP";
                    var rareHero092 = recruitment?.PreviewChestRecruit092(campaign, cardId);
                    if (rareHero092 != null)
                    {
                        offer.HeroRecruitId = rareHero092.StableId;
                        offer.HeroName = rareHero092.Name;
                        offer.RewardPreview += "  •  " + rareHero092.Rank + " HERO INVITATION: " +
                            rareHero092.Name + " • claim FREE at Recruitment";
                    }
                    break;
                }
                case "MERCHANT":
                {
                    var item = CreateQuestCardEquipment090(cardId,
                        QuestCardReceiptPrefix090 + cardId, true);
                    var power = M2EquipmentPowerPolicy087.Resolve(item);
                    offer.RarityId = item.QualityId;
                    offer.ItemName = item.DisplayName;
                    offer.PhysicalPower = power.PhysicalAttack;
                    offer.MysticPower = power.MysticAttack;
                    offer.TreasuryXpCost = MerchantCost090(item.QualityId);
                    offer.Title = "Wayglass Merchant";
                    offer.Description = "Spend Guild XP now for a permanent item. Exact combat gains are shown before purchase.";
                    offer.RewardPreview = item.DisplayName + "  •  −" +
                        offer.TreasuryXpCost + " XP  •  PWR +" +
                        power.PhysicalAttack + "  •  MYS +" + power.MysticAttack;
                    offer.CanChoose = campaign.Guild.TreasuryXp >=
                                      offer.TreasuryXpCost;
                    offer.LockedReason = "Need " + offer.TreasuryXpCost +
                                         " Guild XP for this merchant offer.";
                    break;
                }
                case "RECRUIT":
                {
                    var duplicate = recruitment?.DescribeHeroMasterDuplicate089(
                        campaign, applicant);
                    var signing = recruitment?.DescribeApplicantSigning090(
                        campaign, applicant);
                    var cost = signing?.EffectiveCostTreasuryXp ??
                               GuildCityRecruitmentService017D
                                   .EffectiveSigningCostTreasuryXp(
                                       campaign,
                                       applicant.SigningCostTreasuryXp);
                    offer.Title = duplicate?.IsDuplicateOffer == true
                        ? "Ascend " + applicant.DisplayName
                        : "Recruit " + applicant.DisplayName;
                    offer.Description = duplicate?.IsDuplicateOffer == true
                        ? duplicate.Summary
                        : applicant.DisplayName +
                          " will join permanently and become available to your Unions.";
                    offer.RewardPreview = duplicate?.IsDuplicateOffer == true
                        ? duplicate.Summary + "  •  −" + cost + " XP"
                        : "Permanent recruit  •  −" + cost + " XP";
                    offer.RecruitApplicantId = applicant.RecruitId;
                    offer.TreasuryXpCost = 0;
                    offer.CanChoose = signing?.CanSign == true;
                    offer.LockedReason = signing?.LockedReason ??
                                         "Recruitment is unavailable.";
                    break;
                }
                case "FREE_RECRUIT":
                {
                    offer.HeroRecruitId = freeRecruit094.StableId;
                    offer.HeroName = freeRecruit094.Name;
                    offer.DieOne = 1 + Convert.ToInt32(hash.Substring(10, 2), 16) % 6;
                    offer.DieTwo = 1 + Convert.ToInt32(hash.Substring(12, 2), 16) % 6;
                    offer.FateCheckModifier = QuestCardRunCheckModifier090(expedition.ObjectiveFlags);
                    offer.Title = "A Fellow Adventurer";
                    offer.Description = "Roll two dice to earn " + freeRecruit094.Name +
                        "'s invitation. A success saves this new ally for free recruitment after the adventure.";
                    offer.RiskLabel = "FREE RECRUIT CHANCE  •  2D6";
                    offer.RewardPreview = "Pass: " + freeRecruit094.Name +
                        " invitation, no XP cost  •  Miss: continue the quest";
                    // Neither the roll nor invitation grants currency, bypasses
                    // the safe claim boundary or changes normal paid hiring.
                    offer.TreasuryXpCost = 0;
                    offer.TreasuryXpDelta = 0;
                    break;
                }
                case "BOON":
                {
                    offer.CheckModifierDelta = 1;
                    offer.SupplyDelta = 1;
                    offer.Title = permanentBoon
                        ? "Heroic Breakthrough"
                        : "Guild Banner's Blessing";
                    offer.Description = permanentBoon
                        ? boonHero.DisplayName +
                          " turns this omen into permanent personal growth."
                        : "Carry this blessing for the rest of the quest. Future dice checks gain +1.";
                    offer.RewardPreview = permanentBoon
                        ? boonHero.DisplayName +
                          " permanently gains +8 HP, +2 MP, +1 all core stats"
                        : "Quest boon  •  +1 checks  •  +1 supply";
                    offer.IsPermanentHeroBoon = permanentBoon;
                    offer.HeroRecruitId = permanentBoon
                        ? boonHero.RecruitId
                        : string.Empty;
                    offer.HeroName = permanentBoon
                        ? boonHero.DisplayName
                        : string.Empty;
                    break;
                }
                case "SCAR":
                    offer.CheckModifierDelta = -1;
                    offer.FatigueDelta = 2;
                    offer.ThreatDelta = 1;
                    offer.TreasuryXpDelta = 24;
                    offer.RiskLabel = "DANGEROUS";
                    offer.Title = "Power at a Price";
                    offer.Description = "Take the richer road, but carry its curse for the rest of this quest.";
                    offer.RewardPreview = "+24 XP  •  quest scar: −1 checks, +2 fatigue, +1 threat";
                    break;
                case "FATE":
                {
                    offer.DieOne = 1 + Convert.ToInt32(hash.Substring(10, 2), 16) % 6;
                    offer.DieTwo = 1 + Convert.ToInt32(hash.Substring(12, 2), 16) % 6;
                    offer.FateCheckModifier = QuestCardRunCheckModifier090(
                        expedition.ObjectiveFlags);
                    var success = offer.DieOne + offer.DieTwo +
                                  offer.FateCheckModifier >= offer.Target;
                    offer.RiskLabel = "2D6 CHECK  •  QUEST " +
                                      SignedQuestModifier090(
                                          offer.FateCheckModifier);
                    offer.Title = "Test Your Fate";
                    offer.Description =
                        "Choose the card to throw two physical dice. Boons and scars change the saved roll; the result is revealed only after commitment.";
                    offer.TreasuryXpDelta = success ? 20 : 3;
                    offer.FatigueDelta = success ? -1 : 1;
                    offer.RewardPreview =
                        "Pass: +20 XP, −1 fatigue  •  Miss: +3 XP, +1 fatigue";
                    break;
                }
                case "BATTLE":
                {
                    var enemyUnions = Math.Max(1, Math.Min(4,
                        1 + round / 3));
                    offer.EncounterId = "QUEST_CARD090_WANDERING_THREAT_" +
                                        (round % 10 + 1).ToString("00");
                    offer.EnemyUnionCount = enemyUnions;
                    offer.RiskLabel = "OPTIONAL BATTLE";
                    offer.Title = enemyUnions >= 4
                        ? "Elite Warband"
                        : enemyUnions >= 2
                            ? "Enemy Patrol"
                            : "Wandering Threat";
                    offer.Description =
                        "Win an existing Union Forecast battle to return to this quest route. Defeat ends the expedition.";
                    offer.RewardPreview = enemyUnions + " enemy Union" +
                        (enemyUnions == 1 ? string.Empty : "s") +
                        "  •  victory: battle loot + Art growth + continue";
                    offer.CanChoose = UnionBattlePlanRules132.Read(campaign).Any(value =>
                        value != null && value.Kind == UnionKind.Normal &&
                        value.MemberRecruitIds.Count > 0);
                    offer.LockedReason =
                        "Build at least one active Union before taking a battle card.";
                    break;
                }
                default:
                    offer.Category = "XP";
                    offer.TreasuryXpDelta = 14 + (round % 6) * 2;
                    offer.Title = "Guild Experience";
                    offer.Description = "Take the clear road and bank Guild XP for recruits, merchants, and upgrades.";
                    offer.RewardPreview = "+" + offer.TreasuryXpDelta +
                                          " spendable Guild XP";
                    break;
            }
            return DecorateQuestFate165(campaign, expedition, round, index, offer);
        }

        static ApplicantSnapshotState AvailableQuestApplicant090(
            CampaignState campaign,
            GuildCityRecruitmentService017D recruitment,
            int round)
        {
            var applicants = campaign?.Guild?.GuildCity?.RecruitmentBoard
                ?.Applicants;
            if (recruitment == null || applicants == null ||
                applicants.Count == 0) return null;
            var available = applicants.Where(value =>
                {
                    if (value == null) return false;
                    if (string.IsNullOrWhiteSpace(
                            recruitment.OwnedRecruitIdForApplicant089(
                                campaign, value))) return true;
                    return recruitment.DescribeHeroMasterDuplicate089(
                        campaign, value).IsDuplicateOffer;
                })
                .OrderBy(value => value.DisplayName, StringComparer.Ordinal)
                .ToArray();
            return available.Length == 0
                ? null
                : available[round % available.Length];
        }

        static string QuestApplicantIdentity090(
            CampaignState campaign,
            ApplicantSnapshotState applicant)
        {
            if (applicant == null) return string.Empty;
            return CanonicalJson.Sha256Hex(new
            {
                Rule = "QUEST_CARD_APPLICANT_IDENTITY_090",
                StableId = applicant.AuthoredStableRecruitId ?? string.Empty,
                RecruitId = applicant.RecruitId ?? string.Empty,
                EffectiveSigningCostTreasuryXp = GuildCityRecruitmentService017D
                    .EffectiveSigningCostTreasuryXp(
                        campaign, applicant.SigningCostTreasuryXp),
                CanonicalApplicantSnapshotSha256 =
                    CanonicalJson.Sha256Hex(applicant)
            });
        }

        static string SignedQuestModifier090(int value) =>
            value >= 0 ? "+" + value : value.ToString();

        static RecruitState QuestCardHero090(CampaignState campaign,
            string hash)
        {
            var activeIds = new List<string>();
            foreach (var union in campaign?.Guild?.Unions ??
                     Array.Empty<UnionState>())
                if (union != null && union.Kind == UnionKind.Normal)
                    foreach (var memberId in union.MemberRecruitIds)
                        if (!activeIds.Contains(memberId)) activeIds.Add(memberId);
            var heroes = (campaign?.Guild?.Recruits ??
                          Array.Empty<RecruitState>())
                .Where(value => value != null &&
                    (activeIds.Count == 0 || activeIds.Contains(value.RecruitId)))
                .OrderBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToArray();
            if (heroes.Length == 0) return null;
            var pick = Convert.ToInt32(hash.Substring(14, 2), 16) % heroes.Length;
            return heroes[pick];
        }

        static int MerchantCost090(string qualityId)
        {
            switch ((qualityId ?? string.Empty).ToUpperInvariant())
            {
                case "QUALITY_GODLY": return 140;
                case "QUALITY_LEGENDARY": return 90;
                case "QUALITY_EPIC": return 65;
                case "QUALITY_RARE": return 45;
                case "QUALITY_UNCOMMON": return 30;
                default: return 20;
            }
        }

        static string FriendlyQuality090(string quality)
        {
            var value = (quality ?? string.Empty).Replace("QUALITY_", string.Empty);
            if (string.IsNullOrWhiteSpace(value)) return "Common";
            return value.Substring(0, 1).ToUpperInvariant() +
                   value.Substring(1).ToLowerInvariant();
        }
    }
}
