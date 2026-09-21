using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign023
{
    /// <summary>Exact existing battle authorities, retained for interleaved route-resource replay.</summary>
    public sealed class WorldGateOptionalBattleCost093
    {
        [JsonConstructor]
        public WorldGateOptionalBattleCost093(string operationId, string nodeId, string cardReceiptId,
            EncounterLaunchRequest017D request, BattleReturnReceipt017D battleReturn)
        {
            OperationId = operationId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            CardReceiptId = cardReceiptId ?? string.Empty;
            Request = request;
            BattleReturn = battleReturn;
        }

        public string OperationId { get; }
        public string NodeId { get; }
        public string CardReceiptId { get; }
        public EncounterLaunchRequest017D Request { get; }
        public BattleReturnReceipt017D BattleReturn { get; }
        public static string AuthorityId(WorldGateOptionalBattleCost093 value) =>
            "EXPCOSTAUTH093_" + CanonicalJson.Sha256Hex(value).Substring(0, 24).ToUpperInvariant();

        public static bool Validate(CampaignState campaign, WorldGateOperationState023 operation,
            WorldGateBoardRule023 board, IReadOnlyList<WorldGateOptionalBattleCost093> costs)
        {
            var entries = costs ?? Array.Empty<WorldGateOptionalBattleCost093>();
            var cardIds = new HashSet<string>(StringComparer.Ordinal);
            var returnIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in entries)
            {
                var request = value?.Request;
                var returned = value?.BattleReturn;
                if (request == null || returned == null || operation == null || board == null ||
                    !cardIds.Add(value.CardReceiptId) || !returnIds.Add(returned.ReceiptId) ||
                    value.OperationId != operation.OperationId ||
                    !board.Nodes.Any(node => node.NodeId == value.NodeId && !node.RequiresCertifiedBattle) ||
                    (!operation.CompletedNodeIds.Contains(value.NodeId) && operation.CurrentNodeId != value.NodeId) ||
                    request.ContractId != operation.DefinitionId || request.BoardId != operation.BoardId ||
                    request.CanonicalSeedIdentity != operation.CanonicalSeedIdentity ||
                    !request.AlliedUnionIds.SequenceEqual(operation.AlliedUnionIds, StringComparer.Ordinal) ||
                    !request.RouteModifiers.Contains("EXPEDITION_OPTIONAL_BATTLE_089") ||
                    returned.LaunchRequestId != request.RequestId || returned.BattleRunId != request.BattleId ||
                    returned.ReturnCheckpointId != request.ReturnCheckpointId ||
                    campaign?.Guild?.Development == null ||
                    !campaign.Guild.Development.HasAdventureAuthority(AuthorityId(value)) ||
                    !campaign.Guild.Development.HasAdventureAuthority(value.CardReceiptId) ||
                    !campaign.Guild.Development.HasAdventureAuthority(
                        GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request)) ||
                    !campaign.Guild.Development.HasAdventureAuthority(
                        GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(returned)) ||
                    !campaign.Guild.Development.HasClaimedReward(returned.EquipmentRewardReceiptId) ||
                    campaign.Guild.GuildCity.AppliedBattleReturnIds.Count(id => id == returned.ReceiptId) != 1)
                    return false;
            }
            return true;
        }

        public static bool ApplyAtNode(IReadOnlyList<WorldGateOptionalBattleCost093> costs, string nodeId,
            ref int supplies, ref int fatigue, ref int urgency)
        {
            foreach (var value in costs ?? Array.Empty<WorldGateOptionalBattleCost093>())
            {
                if (value.NodeId != nodeId) continue;
                // The recorded encounter must have started from this exact replay
                // state, so reordering, duplication or arbitrary resource deltas fail.
                if (value.Request.Supplies != supplies || value.Request.Fatigue != fatigue ||
                    value.Request.Urgency != urgency) return false;
                supplies = Math.Max(0, supplies - value.BattleReturn.SupplyConsumption);
                fatigue = Math.Max(0, fatigue + value.BattleReturn.FatigueDelta);
                urgency = Math.Max(0, urgency + value.BattleReturn.UrgencyDelta);
            }
            return true;
        }
    }
}
