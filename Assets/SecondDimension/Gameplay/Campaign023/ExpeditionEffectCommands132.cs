using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign023
{
    public sealed partial class ExpeditionDeckCommandService089
    {
        public Result<CampaignState> RollCommittedEffect132(CampaignState campaign,
            IWorldGateOperationsCatalog023 catalog, string expectedReceiptId)
        {
            if (!TryActive(campaign,catalog,out var runtime,out var operation,out var node,out var error))
                return Result<CampaignState>.Failure(error);
            var routeFate = ExpeditionDeckService089.IsRouteFate132(operation.ExpeditionDeck089?.PendingReceipt?.Effect132);
            if (operation.Status != WorldGateOperationStatus023.Active ||
                (routeFate ? !_deck.ValidateRouteFatePair132(operation) : operation.PendingReceipt != null) ||
                campaign.Guild.Development.HasAdventureAuthority(expectedReceiptId))
                return Result<CampaignState>.Failure("EXPEDITION132_ROLL_NOT_AVAILABLE");
            var rolled = _deck.RollEffect132(operation.ExpeditionDeck089,campaign,expectedReceiptId);
            if (!rolled.IsSuccess) return Result<CampaignState>.Failure(rolled.Errors.ToArray());
            var updated = operation.With(expeditionDeck089:rolled.Value,replaceExpeditionDeck089:true,
                lastCheckpointId:"expedition_132_fate_rolled");
            return Result<CampaignState>.Success(ReplaceRuntime(campaign,runtime.With(activeOperation:updated,
                replaceActiveOperation:true,lastCheckpointId:updated.LastCheckpointId)));
        }
    }
}
