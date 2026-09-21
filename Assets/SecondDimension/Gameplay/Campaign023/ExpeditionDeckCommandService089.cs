using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign023
{
    /// <summary>
    /// Atomic adapter between the Expedition Deck and Campaign023. World Gate
    /// continues to own graph movement, checks, battles, and base rewards.
    /// </summary>
    public sealed partial class ExpeditionDeckCommandService089
    {
        readonly CampaignWorldGateCommandService023 _worldGate;
        readonly ExpeditionDeckService089 _deck;

        public ExpeditionDeckCommandService089(
            CampaignWorldGateCommandService023 worldGate = null,
            ExpeditionDeckService089 deck = null)
        {
            _worldGate = worldGate ?? new CampaignWorldGateCommandService023();
            _deck = deck ?? new ExpeditionDeckService089();
        }

        public Result<CampaignState> CommitRouteCard(
            CampaignState campaign,
            IWorldGateOperationsCatalog023 catalog,
            string cardId,
            string actorRecruitId,
            string assistantRecruitId)
        {
            if (campaign != null)
            {
                var recovered = RecoverVerifiedOptionalBattleCost093(campaign, catalog);
                if (!recovered.IsSuccess) return recovered;
                campaign = recovered.Value;
            }
            if (!TryActive(campaign, catalog, out var runtime, out var operation,
                    out var node, out var error))
                return Result<CampaignState>.Failure(error);
            var committed = _deck.CommitCard(campaign.CampaignSeed, operation, node,
                cardId, actorRecruitId, assistantRecruitId, campaign);
            if (!committed.IsSuccess)
                return Result<CampaignState>.Failure(committed.Errors.ToArray());
            var selectedDeck = committed.Value;
            var selectedCard = selectedDeck.CurrentRow.FirstOrDefault(value =>
                value != null && selectedDeck.PendingReceipt != null &&
                StringComparer.Ordinal.Equals(value.CardId,
                    selectedDeck.PendingReceipt.CardId));
            if (selectedCard == null)
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_SELECTED_CARD_MISSING");
            var stagedRuntime = runtime.With(
                activeOperation: operation.With(expeditionDeck089: selectedDeck,
                    replaceExpeditionDeck089: true), replaceActiveOperation: true,
                lastCheckpointId: selectedCard.AdvancesRoute
                    ? "expedition_089_route_card_committed"
                    : "expedition_089_encounter_card_committed");
            CampaignState staged;
            if (ExpeditionDeckService089.IsOptionalBattleCard089(selectedCard))
            {
                try
                {
                    var request = CreateOptionalBattleRequest089(campaign,
                        operation, selectedCard, selectedDeck.PendingReceipt, newCommit094: true);
                    var requestAuthority = GuildCityBattleBridgeService017D
                        .EncounterRequestAuthorityId084(request);
                    var development = campaign.Guild.Development;
                    if (development.HasAdventureAuthority(requestAuthority))
                        return Result<CampaignState>.Failure(
                            "EXPEDITION089_BATTLE_AUTHORITY_ALREADY_COMMITTED");
                    if (!development.CanRecordAdventureAuthority(
                            requestAuthority))
                        return Result<CampaignState>.Failure(
                            "EXPEDITION089_ADVENTURE_AUTHORITY_LEDGER_FULL");
                    development = development.RecordAdventureAuthority(
                        requestAuthority);
                    var city = campaign.Guild.GuildCity;
                    var expedition = city.Expedition.With(
                        status: ExpeditionStatus017D.AwaitingBattle,
                        lastCheckpointId:
                        "expedition_089_optional_battle_committed");
                    city = city.With(expedition: expedition,
                        replaceExpedition: true, pendingEncounter: request,
                        replacePendingEncounter: true,
                        lastCheckpointId:
                        "expedition_089_optional_battle_committed");
                    staged = ReplaceRuntime(campaign, stagedRuntime,
                        development: development, cityState: city);
                }
                catch (Exception exception)
                {
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_BATTLE_COMMIT_FAILED:" +
                        exception.Message);
                }
            }
            else staged = ReplaceRuntime(campaign, stagedRuntime);
            // The first row at a safe room is a genuine deck encounter. It owns
            // its own exact-once receipt and reward, but it must not consume or
            // bypass the authored World Gate edge. The following row chooses the
            // route through the existing World Gate command authority.
            if (!selectedCard.AdvancesRoute)
            {
                var luck165=selectedDeck.PendingReceipt?.Effect132?.LuckChargeId165;
                if(luck165!=null)
                {
                    try{staged=TownLuckConsumables165.ConsumeForSeal(staged,luck165,selectedDeck.PendingReceipt.ReceiptId);}
                    catch(Exception exception){return Result<CampaignState>.Failure("EXPEDITION165_LUCK_SEAL_FAILED:"+exception.Message);}
                }
                return Result<CampaignState>.Success(staged);
            }
            var modifier = node.CheckDifficulty > 0
                ? selectedDeck.PendingReceipt.BaseCheckModifier : 0;
            var worldGate = _worldGate.CommitNodeChoice(staged, catalog,
                selectedDeck.PendingReceipt.ChoiceId, actorRecruitId,
                assistantRecruitId, modifier);
            if (!worldGate.IsSuccess)
                return Result<CampaignState>.Failure(worldGate.Errors.ToArray());
            var updated = worldGate.Value;
            var updatedRuntime = Runtime(updated);
            var updatedOperation = updatedRuntime.ActiveOperation;
            try
            {
                var finalCardReceipt = _deck.FinalizeAgainstWorldGate(
                    updatedOperation.ExpeditionDeck089,
                    updatedOperation.PendingReceipt);
                var finalDeck = updatedOperation.ExpeditionDeck089.With(
                    pendingReceipt: finalCardReceipt, replacePendingReceipt: true);
                finalDeck = _deck.SealRouteFate132(finalDeck,updatedOperation.PendingReceipt,updated);
                updatedOperation = updatedOperation.With(expeditionDeck089: finalDeck,
                    replaceExpeditionDeck089: true,
                    lastCheckpointId: "expedition_089_route_revealed");
                var sealed165=ReplaceRuntime(updated,updatedRuntime.With(activeOperation:updatedOperation,
                    replaceActiveOperation:true,lastCheckpointId:updatedOperation.LastCheckpointId));
                var luck165=finalDeck.PendingReceipt?.Effect132?.LuckChargeId165;
                if(luck165!=null)sealed165=TownLuckConsumables165.ConsumeForSeal(sealed165,luck165,finalDeck.PendingReceipt.ReceiptId);
                return Result<CampaignState>.Success(sealed165);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_RECEIPT_FINALIZE_FAILED:" + exception.Message);
            }
        }

        public Result<CampaignState> ApplyWorldGateReceiptExactlyOnce(
            CampaignState campaign,
            IWorldGateOperationsCatalog023 catalog)
        {
            if (!TryActive(campaign, catalog, out var runtime, out var operation,
                    out var node, out var error))
                return Result<CampaignState>.Failure(error);
            var deck = operation.ExpeditionDeck089;
            var cardReceipt = deck?.PendingReceipt;
            if(cardReceipt?.Effect132?.LuckChargeId165!=null&&!TownLuckConsumables165.WasConsumedFor(campaign,
                cardReceipt.Effect132.LuckChargeId165,cardReceipt.ReceiptId))return Result<CampaignState>.Failure("EXPEDITION165_LUCK_SEAL_RECEIPT_REQUIRED");
            var selectedCard = cardReceipt == null ? null :
                deck.CurrentRow.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.CardId,
                        cardReceipt.CardId));
            var advancesRoute = selectedCard?.AdvancesRoute ?? true;
            if (cardReceipt != null)
            {
                var deckReceiptValid = selectedCard != null &&
                    _deck.ValidateCommittedReceipt(deck, cardReceipt) &&
                    StringComparer.Ordinal.Equals(cardReceipt.NodeId,
                        operation.CurrentNodeId);
                var routeReceiptValid = !advancesRoute ||
                    (operation.PendingReceipt != null &&
                     StringComparer.Ordinal.Equals(cardReceipt.NodeId,
                         operation.PendingReceipt.NodeId) &&
                     StringComparer.Ordinal.Equals(cardReceipt.ChoiceId,
                         operation.PendingReceipt.ChoiceId));
                var encounterReceiptValid = advancesRoute ||
                    operation.PendingReceipt == null;
                if (!deckReceiptValid || !routeReceiptValid ||
                    !encounterReceiptValid)
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_PENDING_RECEIPT_INVALID");
                if (ExpeditionDeckService089.IsRouteFate132(cardReceipt.Effect132) &&
                    !_deck.ValidateRouteFatePair132(operation))
                    return Result<CampaignState>.Failure("EXPEDITION132_ROUTE_FATE_PAIR_INVALID");
                if (cardReceipt.Effect132 != null && !cardReceipt.Effect132.IsRolled132)
                    return Result<CampaignState>.Failure("EXPEDITION132_ROLL_BEFORE_COLLECTING");
                if (cardReceipt.RequiresCertifiedBattle &&
                    string.IsNullOrWhiteSpace(
                        cardReceipt.BattleReturnReceiptId))
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_COMPLETE_OPTIONAL_BATTLE_FIRST");
                if (campaign.Guild.Development.HasAdventureAuthority(
                        cardReceipt.ReceiptId))
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_CARD_RECEIPT_ALREADY_APPLIED");
                if (!campaign.Guild.Development.CanRecordAdventureAuthority(
                        cardReceipt.ReceiptId))
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_ADVENTURE_AUTHORITY_LEDGER_FULL");
            }

            CampaignState updated;
            if (cardReceipt != null && !advancesRoute)
                updated = campaign;
            else
            {
                var applied = _worldGate.ApplyNodeReceiptExactlyOnce(campaign, catalog);
                if (!applied.IsSuccess)
                    return Result<CampaignState>.Failure(applied.Errors.ToArray());
                updated = applied.Value;
            }
            var nextRuntime = Runtime(updated);
            var nextOperation = nextRuntime.ActiveOperation;
            if (deck == null || nextOperation == null)
                return Result<CampaignState>.Success(updated);

            if (cardReceipt == null)
            {
                if (node.RequiresCertifiedBattle)
                {
                    var battleDeck = _deck.AdvanceLockedBattle(
                        nextOperation.ExpeditionDeck089, node.NodeId,
                        nextOperation.Status == WorldGateOperationStatus023.ReadyToFinalize
                            ? string.Empty : nextOperation.CurrentNodeId);
                    nextOperation = nextOperation.With(expeditionDeck089: battleDeck,
                        replaceExpeditionDeck089: true);
                    return Result<CampaignState>.Success(ReplaceRuntime(updated,
                        nextRuntime.With(activeOperation: nextOperation,
                            replaceActiveOperation: true)));
                }
                return Result<CampaignState>.Success(updated);
            }

            try
            {
                var nextDeckNodeId = advancesRoute &&
                                     nextOperation.Status ==
                                     WorldGateOperationStatus023.ReadyToFinalize
                    ? string.Empty
                    : nextOperation.CurrentNodeId;
                var advanced = _deck.Advance(nextOperation.ExpeditionDeck089,
                    nextDeckNodeId);
                nextOperation = nextOperation.With(expeditionDeck089: advanced,
                    replaceExpeditionDeck089: true,
                    lastCheckpointId: advancesRoute
                        ? "expedition_089_route_reward_applied"
                        : "expedition_089_encounter_reward_applied");
                var leads = new List<string>(nextRuntime.ExpeditionRecruitLeadIds089);
                if (!SssTenV4AcquisitionService090.IsCampaignContract090(
                        selectedCard) &&
                    !string.IsNullOrWhiteSpace(cardReceipt.RecruitStableId) &&
                    !leads.Contains(cardReceipt.RecruitStableId))
                    leads.Add(cardReceipt.RecruitStableId);
                leads.Sort(StringComparer.Ordinal);
                nextRuntime = nextRuntime.With(activeOperation: nextOperation,
                    replaceActiveOperation: true,
                    expeditionRecruitLeadIds089: leads.AsReadOnly(),
                    lastCheckpointId: nextOperation.LastCheckpointId);

                var city = updated.Guild.GuildCity;
                var materials = MergeMaterials(city.Materials,
                    cardReceipt.MaterialIds);
                var development = updated.Guild.Development
                    .RecordAdventureAuthority(cardReceipt.ReceiptId);
                var treasury = updated.Guild.TreasuryXp;
                var inventory = new List<EquipmentItemState>(
                    updated.Guild.Inventory);
                var recruits = new List<RecruitState>(
                    updated.Guild.Recruits);
                if (cardReceipt.TreasuryXpCost > 0)
                {
                    if (treasury < cardReceipt.TreasuryXpCost)
                        return Result<CampaignState>.Failure(
                            "EXPEDITION089_TREASURY_XP_INSUFFICIENT");
                    treasury -= cardReceipt.TreasuryXpCost;
                }
                if (selectedCard != null &&
                    StringComparer.Ordinal.Equals(selectedCard.Category, "CHEST") &&
                    cardReceipt.MaterialIds.Count > 0)
                {
                    var equipment = ExpeditionDeckService089
                        .ChestEquipmentReward089(selectedCard.CardId,
                            cardReceipt.ReceiptId);
                    if (!inventory.Any(value => value != null &&
                        StringComparer.Ordinal.Equals(value.InstanceId,
                            equipment.InstanceId)))
                        inventory.Add(equipment);
                }
                if (selectedCard != null &&
                    StringComparer.Ordinal.Equals(
                        selectedCard.Category, "MERCHANT"))
                {
                    var equipment = ExpeditionDeckService089
                        .MerchantEquipmentReward089(selectedCard.CardId,
                            cardReceipt.ReceiptId);
                    if (!inventory.Any(value => value != null &&
                        StringComparer.Ordinal.Equals(value.InstanceId,
                            equipment.InstanceId)))
                        inventory.Add(equipment);
                }
                if (selectedCard != null &&
                    !string.IsNullOrWhiteSpace(
                        selectedCard.PermanentHeroEffectId) &&
                    ReceiptSucceeded089(cardReceipt) &&
                    !ExpeditionDeckService089.IsRouteFate132(cardReceipt.Effect132))
                {
                    var recruitIndex = recruits.FindIndex(value =>
                        value != null && StringComparer.Ordinal.Equals(
                            value.RecruitId,
                            selectedCard.PermanentHeroRecruitId));
                    if (recruitIndex < 0)
                        return Result<CampaignState>.Failure(
                            "EXPEDITION089_PERMANENT_HERO_MISSING");
                    var recruit = recruits[recruitIndex];
                    var progression = recruit.Progression ??
                                      RecruitProgressionState.Default();
                    var traits = new List<string>(
                        progression.UnlockedTreeIds);
                    if (!traits.Contains(selectedCard.PermanentHeroEffectId))
                        traits.Add(selectedCard.PermanentHeroEffectId);
                    traits.Sort(StringComparer.Ordinal);
                    recruits[recruitIndex] = recruit.WithProgression(
                        progression.WithUnlockedTrees(traits.AsReadOnly()));
                }
                ExpeditionDeckService089.ApplyEffect132(cardReceipt, recruits, inventory);
                if (cardReceipt.GuildXp > 0 && cardReceipt.HallXp > 0)
                {
                    development = development.RecordBattleReward(
                        cardReceipt.ReceiptId, cardReceipt.GuildXp,
                        cardReceipt.HallXp);
                    treasury = checked(treasury + cardReceipt.GuildXp);
                }
                var committed=ReplaceRuntime(updated,
                    nextRuntime, materials, development, treasury,
                    inventory.AsReadOnly(), recruits.AsReadOnly());
                // Earned gear stays in Inventory until an explicit equipment command.
                var earnedRecruit = GuildCityRecruitmentService017D.RecordEarnedCardRecruit094(
                    committed, selectedCard, cardReceipt);
                if (!earnedRecruit.IsSuccess) return earnedRecruit;
                committed = earnedRecruit.Value;
                return SssTenV4AcquisitionService090.IsCampaignContract090(
                        selectedCard)
                    ?SssTenV4AcquisitionService090
                        .ApplySelectedCampaignContract090(
                            committed,selectedCard,cardReceipt)
                    :Result<CampaignState>.Success(committed);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_CARD_APPLY_FAILED:" + exception.Message);
            }
        }

        public bool HasPendingOptionalBattle089(CampaignState campaign)
        {
            var runtime = Runtime(campaign);
            var operation = runtime?.ActiveOperation;
            var receipt = operation?.ExpeditionDeck089?.PendingReceipt;
            var card = receipt == null ? null :
                operation.ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                    value != null && StringComparer.Ordinal.Equals(
                        value.CardId, receipt.CardId));
            var request = campaign?.Guild?.GuildCity?.PendingEncounter;
            return ExpeditionDeckService089.IsOptionalBattleCard089(card) &&
                   receipt.RequiresCertifiedBattle && request != null &&
                   StringComparer.Ordinal.Equals(request.RequestId,
                       receipt.BattleRequestId) &&
                   StringComparer.Ordinal.Equals(request.BattleId,
                       receipt.BattleId) &&
                   campaign.Guild.Development.HasAdventureAuthority(
                       GuildCityBattleBridgeService017D
                           .EncounterRequestAuthorityId084(request));
        }

        // Optional encounters belong to the selected card, so their request
        // node intentionally differs from the board's current route node.
        // Recognize only the unchanged mirror and exact committed request.
        public bool HasCanonicalOptionalBattleHandoff104(CampaignState campaign)
        {
            if (!HasPendingOptionalBattle089(campaign)) return false;
            var city = campaign.Guild.GuildCity;
            var operation = Runtime(campaign).ActiveOperation;
            var deck = operation.ExpeditionDeck089;
            var receipt = deck.PendingReceipt;
            if (!CampaignWorldGateCommandService023
                    .HasCanonicalLegacyExpeditionMirror104(SecondDimension.Gameplay.Campaign020.CampaignRecoveryCommands150.WorldIdentityCity(campaign), operation) ||
                !_deck.ValidateCommittedReceipt(deck, receipt) ||
                !StringComparer.Ordinal.Equals(receipt.NodeId,
                    operation.CurrentNodeId)) return false;
            var card = deck.CurrentRow.First(value => value != null &&
                StringComparer.Ordinal.Equals(value.CardId, receipt.CardId));
            var expected = CreateOptionalBattleRequest089(campaign,
                operation, card, receipt);
            var request = city.PendingEncounter;
            if (!StringComparer.Ordinal.Equals(CanonicalJson.Serialize(request),
                    CanonicalJson.Serialize(expected))) return false;
            var battleReturn = city.PendingBattleReturn;
            return battleReturn == null ||
                (StringComparer.Ordinal.Equals(battleReturn.LaunchRequestId,
                    request.RequestId) &&
                 StringComparer.Ordinal.Equals(battleReturn.BattleRunId,
                    request.BattleId));
        }

        public bool HasUnresolvedOptionalBattleReceipt089(
            CampaignState campaign)
        {
            var operation = Runtime(campaign)?.ActiveOperation;
            var receipt = operation?.ExpeditionDeck089?.PendingReceipt;
            if (receipt == null || !receipt.RequiresCertifiedBattle ||
                !string.IsNullOrWhiteSpace(receipt.BattleReturnReceiptId))
                return false;
            var card = operation.ExpeditionDeck089.CurrentRow.FirstOrDefault(
                value => value != null && StringComparer.Ordinal.Equals(
                    value.CardId, receipt.CardId));
            return ExpeditionDeckService089.IsOptionalBattleCard089(card);
        }

        public Result<CampaignState> SynchronizeOptionalBattleAfterClaim089(
            CampaignState campaign,
            IWorldGateOperationsCatalog023 catalog)
        {
            if (!TryActive(campaign, catalog, out var runtime,
                    out var operation, out _, out var error))
                return Result<CampaignState>.Failure(error);
            var deck = operation.ExpeditionDeck089;
            var pending = deck?.PendingReceipt;
            var card = pending == null ? null : deck.CurrentRow.FirstOrDefault(
                value => value != null && StringComparer.Ordinal.Equals(
                    value.CardId, pending.CardId));
            if (!ExpeditionDeckService089.IsOptionalBattleCard089(card) ||
                pending == null || !pending.RequiresCertifiedBattle ||
                !string.IsNullOrWhiteSpace(pending.BattleReturnReceiptId))
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_OPTIONAL_BATTLE_RECEIPT_REQUIRED");
            var city = campaign.Guild.GuildCity;
            if (city.PendingEncounter != null ||
                city.PendingBattleReturn != null)
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_BATTLE_RETURN_NOT_APPLIED");
            var battle = campaign.Battle;
            if (battle?.Reward == null || !battle.Reward.Claimed ||
                battle.Phase != BattlePhase.Resolved ||
                !StringComparer.Ordinal.Equals(battle.BattleId,
                    pending.BattleId) ||
                !M2BattleCommandService.HasValidFinalStateHash090(battle) ||
                !campaign.Guild.Development.HasClaimedReward(
                    battle.Reward.RewardId))
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_BATTLE_AUTHORITY_INVALID");
            try
            {
                var request = CreateOptionalBattleRequest089(campaign,
                    operation, card, pending);
                if (!campaign.Guild.Development.HasAdventureAuthority(
                        GuildCityBattleBridgeService017D
                            .EncounterRequestAuthorityId084(request)))
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_BATTLE_REQUEST_AUTHORITY_INVALID");
                if (!GuildCityBattleBridgeService017D
                        .TryCreateCanonicalBattleReturn084(battle, request,
                            city.OperationOrdinal, out var battleReturn,
                            out var returnError))
                    return Result<CampaignState>.Failure(returnError);
                if (city.AppliedBattleReturnIds.Count(value =>
                        StringComparer.Ordinal.Equals(value,
                            battleReturn.ReceiptId)) != 1 ||
                    !campaign.Guild.Development.HasAdventureAuthority(
                        GuildCityBattleBridgeService017D
                            .BattleReturnApplyAuthorityId084(battleReturn)))
                    return Result<CampaignState>.Failure(
                        "EXPEDITION089_BATTLE_RETURN_AUTHORITY_INVALID");

                var finalizedDeck = _deck.FinalizeOptionalBattle089(
                    deck, battle, battleReturn);
                var cost = new WorldGateOptionalBattleCost093(operation.OperationId,
                    pending.NodeId, pending.ReceiptId, request, battleReturn);
                var costAuthority = WorldGateOptionalBattleCost093.AuthorityId(cost);
                var costs = new List<WorldGateOptionalBattleCost093>(
                    operation.OptionalBattleCosts093 ?? Array.Empty<WorldGateOptionalBattleCost093>());
                if (costs.Any(value => value.CardReceiptId == pending.ReceiptId) ||
                    campaign.Guild.Development.HasAdventureAuthority(costAuthority))
                    return Result<CampaignState>.Failure("EXPEDITION093_COST_ALREADY_RECORDED");
                if (!campaign.Guild.Development.CanRecordAdventureAuthority(costAuthority))
                    return Result<CampaignState>.Failure("EXPEDITION093_COST_AUTHORITY_LEDGER_FULL");
                costs.Add(cost);
                // The Guild City bridge is the sole battle-return authority.
                // Carry its real expedition costs back into the World Gate
                // mirror before reopening the same room; otherwise an optional
                // fight would silently erase its supply/fatigue consequences.
                var returnedExpedition = city.Expedition;
                operation = operation.With(
                    supplies: returnedExpedition.Supplies,
                    fatigue: returnedExpedition.Fatigue,
                    urgency: returnedExpedition.Urgency,
                    threat: returnedExpedition.Threat,
                    expeditionDeck089: finalizedDeck,
                    optionalBattleCosts093: costs.AsReadOnly(),
                    replaceExpeditionDeck089: true,
                    lastCheckpointId:
                    "expedition_089_optional_battle_returned");
                runtime = runtime.With(activeOperation: operation,
                    replaceActiveOperation: true,
                    lastCheckpointId: operation.LastCheckpointId);
                var expedition = city.Expedition.With(
                    status: ExpeditionStatus017D.Active,
                    supplies: operation.Supplies,
                    fatigue: operation.Fatigue,
                    urgency: operation.Urgency,
                    threat: operation.Threat,
                    lastCheckpointId: operation.LastCheckpointId);
                city = city.With(expedition: expedition,
                    replaceExpedition: true,
                    lastCheckpointId: operation.LastCheckpointId);
                var staged = ReplaceRuntime(campaign, runtime,
                    cityState: city, development: campaign.Guild.Development.RecordAdventureAuthority(costAuthority));
                return ApplyWorldGateReceiptExactlyOnce(staged, catalog);
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_BATTLE_RETURN_FAILED:" +
                    exception.Message);
            }
        }

        public Result<CampaignState> AcknowledgeTutorial(CampaignState campaign)
        {
            var runtime = Runtime(campaign);
            if (runtime == null)
                return Result<CampaignState>.Failure(
                    "EXPEDITION089_RUNTIME_REQUIRED");
            if (runtime.ExpeditionDeckTutorialSeen089)
                return Result<CampaignState>.Success(campaign);
            return Result<CampaignState>.Success(ReplaceRuntime(campaign,
                runtime.With(expeditionDeckTutorialSeen089: true,
                    lastCheckpointId: "expedition_089_tutorial_seen")));
        }

        static bool TryActive(CampaignState campaign,
            IWorldGateOperationsCatalog023 catalog,
            out WorldGateRuntimeState023 runtime,
            out WorldGateOperationState023 operation,
            out WorldGateNodeRule023 node,
            out string error)
        {
            runtime = Runtime(campaign);
            operation = runtime?.ActiveOperation;
            node = null;
            error = string.Empty;
            if (campaign?.Guild?.GuildCity == null || catalog == null)
            {
                error = "EXPEDITION089_CAMPAIGN_REQUIRED";
                return false;
            }
            if (operation == null)
            {
                error = "EXPEDITION089_ACTIVE_OPERATION_REQUIRED";
                return false;
            }
            if (!catalog.TryGetBoard(operation.DefinitionId, out var board))
            {
                error = "EXPEDITION089_BOARD_UNKNOWN";
                return false;
            }
            var activeOperation = operation;
            node = board.Nodes.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.NodeId,
                    activeOperation.CurrentNodeId));
            if (node == null)
            {
                error = "EXPEDITION089_NODE_UNKNOWN";
                return false;
            }
            return true;
        }

        static WorldGateRuntimeState023 Runtime(CampaignState campaign) =>
            campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
                .Playable020?.WorldGate023;

        static CampaignState ReplaceRuntime(
            CampaignState campaign,
            WorldGateRuntimeState023 runtime,
            IReadOnlyList<GuildMaterialState017D> materials = null,
            GuildDevelopmentState development = null,
            long? treasuryXp = null,
            IReadOnlyList<EquipmentItemState> inventory = null,
            IReadOnlyList<RecruitState> recruits = null,
            GuildCityState017D cityState = null)
        {
            var guild = campaign.Guild;
            var city = cityState ?? guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(worldGate023: runtime,
                replaceWorldGate023: true, lastCheckpointId: runtime.LastCheckpointId);
            progress = progress.With(playable020: playable,
                replacePlayable020: true, lastCheckpointId: runtime.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: runtime.LastCheckpointId);
            city = city.With(materials: materials ?? city.Materials,
                strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: runtime.LastCheckpointId);
            guild = guild.With(treasuryXp ?? guild.TreasuryXp,
                recruits ?? guild.Recruits,
                guild.Unions, inventory ?? guild.Inventory,
                development ?? guild.Development)
                .WithGuildCity(city);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        static EncounterLaunchRequest017D CreateOptionalBattleRequest089(
            CampaignState campaign,
            WorldGateOperationState023 operation,
            ExpeditionRouteCardState089 card,
            ExpeditionCardReceipt089 receipt, bool newCommit094 = false)
        {
            var city = campaign?.Guild?.GuildCity ??
                       throw new InvalidOperationException(
                           "Guild City is required for an optional battle.");
            if (operation == null || card == null || receipt == null ||
                city.ActiveContract == null || city.Expedition == null ||
                !receipt.RequiresCertifiedBattle ||
                string.IsNullOrWhiteSpace(receipt.BattleRequestId) ||
                string.IsNullOrWhiteSpace(receipt.BattleId) ||
                string.IsNullOrWhiteSpace(receipt.BattlePreStateHash) ||
                string.IsNullOrWhiteSpace(
                    receipt.BattleReturnCheckpointId))
                throw new InvalidOperationException(
                    "The saved optional battle linkage is incomplete.");
            var replayProgress130 = city.Strategic017H?.Campaign019;
            var replayCycle130 = StringComparer.Ordinal.Equals(operation.OperationKind, "CHAPTER")
                ? CampaignReplayRules130.CycleForCommittedOrdinal(replayProgress130,
                    city.ActiveContract.AcceptedOperationOrdinal) : 1;
            var committedRoute130 = CampaignReplayThreat130.AppendFrozenRoutes132(new[]
                {
                    "EXPEDITION_OPTIONAL_BATTLE_089",
                    "EXPEDITION_DECK_TIER_" + operation.ExpeditionDeck089.ProgressionTier
                }, replayProgress130, replayCycle130);
            var legacy094 = new EncounterLaunchRequest017D(
                receipt.BattleRequestId,
                city.ActiveContract.ContractId,
                city.Expedition.ExpeditionId,
                operation.BoardId,
                "EXPEDITION_CARD_NODE089_" +
                card.CardId.Substring(Math.Max(0, card.CardId.Length - 16)),
                card.EncounterId,
                receipt.BattleId,
                "Defeat the wandering threat, claim the certified battle " +
                "reward, then continue choosing cards in this quest room.",
                CampaignReplayThreat130.MinimumUnionCount132(card.EnemyUnionCount, committedRoute130),
                operation.CanonicalSeedIdentity,
                operation.AlliedUnionIds,
                Array.Empty<string>(),
                new[] { "OBJECTIVE_EXPEDITION_CARD_BATTLE_089" },
                committedRoute130,
                operation.Supplies,
                operation.Fatigue,
                operation.Urgency,
                receipt.BattleReturnCheckpointId,
                receipt.BattlePreStateHash);
            return EnemyForceProfile094.ForRequest094(campaign, legacy094,
                CampaignReplayThreat130.ForceChapter132(EnemyForceProfile094.CampaignChapter094(campaign, operation.DefinitionId),committedRoute130),
                newCommit094);
        }

        static bool ReceiptSucceeded089(ExpeditionCardReceipt089 receipt) =>
            receipt != null &&
            (receipt.Outcome ?? string.Empty).IndexOf("SETBACK",
                StringComparison.OrdinalIgnoreCase) < 0 &&
            (receipt.Outcome ?? string.Empty).IndexOf("DEFEAT",
                StringComparison.OrdinalIgnoreCase) < 0 &&
            (receipt.Outcome ?? string.Empty).IndexOf("FAIL",
                StringComparison.OrdinalIgnoreCase) < 0;

        static IReadOnlyList<GuildMaterialState017D> MergeMaterials(
            IReadOnlyList<GuildMaterialState017D> existing,
            IReadOnlyList<string> additions)
        {
            var result = new List<GuildMaterialState017D>(
                existing ?? Array.Empty<GuildMaterialState017D>());
            if (additions != null)
                foreach (var id in additions)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    var current = result.FirstOrDefault(value =>
                        StringComparer.Ordinal.Equals(value.MaterialId, id));
                    if (current == null) result.Add(new GuildMaterialState017D(id, 1));
                    else
                    {
                        result.Remove(current);
                        result.Add(current.WithAmount(checked(current.Amount + 1)));
                    }
                }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(
                left.MaterialId, right.MaterialId));
            return result.AsReadOnly();
        }
    }
}
