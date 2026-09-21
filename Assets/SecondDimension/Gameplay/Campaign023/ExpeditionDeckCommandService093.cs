using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign023
{
    public sealed partial class ExpeditionDeckCommandService089
    {
        // No load-time grant or reset. Recover only the exact old boundary proved by
        // its still-current certified battle and both persisted authority ledgers.
        public Result<CampaignState> RecoverVerifiedOptionalBattleCost093(
            CampaignState campaign, IWorldGateOperationsCatalog023 catalog)
        {
            var runtime = Runtime(campaign);
            var operation = runtime?.ActiveOperation;
            var deck = operation?.ExpeditionDeck089;
            if (deck == null || (operation.OptionalBattleCosts093?.Count ?? 0) > 0)
                return Result<CampaignState>.Success(campaign);
            var battleReceipts = deck.AppliedReceipts.Where(value => value.RequiresCertifiedBattle).ToArray();
            if (battleReceipts.Length == 0) return Result<CampaignState>.Success(campaign);
            const string failure = "EXPEDITION093_OLD_BATTLE_COST_REQUIRES_VERIFIED_CHECKPOINT";
            var battle = campaign.Battle;
            var city = campaign.Guild.GuildCity;
            if (catalog == null || battleReceipts.Length != 1 || battle?.Reward == null || !battle.Reward.Claimed ||
                battle.Phase != BattlePhase.Resolved || !M2BattleCommandService.HasValidFinalStateHash090(battle) ||
                city.PendingEncounter != null || city.PendingBattleReturn != null ||
                !catalog.TryGetBoard(operation.DefinitionId, out var board))
                return Result<CampaignState>.Failure(failure);
            var receipt = battleReceipts[0];
            var card = deck.CurrentRow.Concat(deck.DrawPile).Concat(deck.DiscardPile).Concat(deck.BanishedCards)
                .FirstOrDefault(value => value.CardId == receipt.CardId);
            if (receipt.NodeId != operation.CurrentNodeId || receipt.BattleId != battle.BattleId ||
                !ExpeditionDeckService089.IsOptionalBattleCard089(card) || !_deck.ValidateCommittedReceipt(deck, receipt) ||
                !campaign.Guild.Development.HasAdventureAuthority(receipt.ReceiptId))
                return Result<CampaignState>.Failure(failure);
            var supplies = catalog.RewardPolicy.TravelSupplyReserveDefault;
            var fatigue = 0;
            var urgency = 0;
            var cursor = board.StartNodeId;
            foreach (var ignored in operation.AppliedReceipts)
            {
                var route = operation.AppliedReceipts.FirstOrDefault(value => value.NodeId == cursor);
                if (route == null || !campaign.Guild.Development.HasAdventureAuthority(route.ReceiptId))
                    return Result<CampaignState>.Failure(failure);
                supplies = Math.Max(0, supplies + route.SupplyDelta);
                fatigue = Math.Max(0, fatigue + route.FatigueDelta);
                urgency = Math.Max(0, urgency + route.UrgencyDelta);
                cursor = route.NextNodeId;
            }
            if (cursor != operation.CurrentNodeId) return Result<CampaignState>.Failure(failure);
            var beforeBattle = operation.With(supplies: supplies, fatigue: fatigue, urgency: urgency);
            var request = CreateOptionalBattleRequest089(campaign, beforeBattle, card, receipt);
            if (!campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request)) ||
                !GuildCityBattleBridgeService017D.TryCreateCanonicalBattleReturn084(
                    battle, request, city.OperationOrdinal, out var returned, out _) ||
                returned.ReceiptId != receipt.BattleReturnReceiptId ||
                !campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(returned)) ||
                city.AppliedBattleReturnIds.Count(value => value == returned.ReceiptId) != 1 ||
                !campaign.Guild.Development.HasClaimedReward(returned.EquipmentRewardReceiptId) ||
                operation.Supplies != Math.Max(0, supplies - returned.SupplyConsumption) ||
                operation.Fatigue != Math.Max(0, fatigue + returned.FatigueDelta) ||
                operation.Urgency != Math.Max(0, urgency + returned.UrgencyDelta) ||
                city.Expedition == null || city.Expedition.Supplies != operation.Supplies ||
                city.Expedition.Fatigue != operation.Fatigue || city.Expedition.Urgency != operation.Urgency)
                return Result<CampaignState>.Failure(failure);
            var cost = new WorldGateOptionalBattleCost093(operation.OperationId, receipt.NodeId,
                receipt.ReceiptId, request, returned);
            var authority = WorldGateOptionalBattleCost093.AuthorityId(cost);
            var development = campaign.Guild.Development;
            if (!development.HasAdventureAuthority(authority))
            {
                if (!development.CanRecordAdventureAuthority(authority))
                    return Result<CampaignState>.Failure("EXPEDITION093_COST_AUTHORITY_LEDGER_FULL");
                development = development.RecordAdventureAuthority(authority);
            }
            var candidate = ReplaceRuntime(campaign, runtime.With(activeOperation: operation.With(
                optionalBattleCosts093: new[] { cost }), replaceActiveOperation: true), development: development);
            if (!CampaignWorldGateCommandService023.ValidateActiveAuthority093(candidate, catalog, out var validation))
                return Result<CampaignState>.Failure(failure + ":" + validation);
            return Result<CampaignState>.Success(candidate);
        }
    }
}
