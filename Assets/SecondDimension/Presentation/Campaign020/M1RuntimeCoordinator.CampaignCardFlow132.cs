using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation.Campaign020
{
    public sealed class CampaignInterruptionView132
    {
        public string Identity; public string Kind; public string Title; public string Description;
        public string OperationId; public int StepIndex;
    }
    public interface ICampaignCardFlowCoordinator132
    {
        CampaignInterruptionView132 CampaignInterruption132 { get; }
        M1CommandResult CompleteCampaignInterruption132(string expectedIdentity);
        M1CommandResult ResumeCampaignInterruption132(string operationId, int stepIndex);
    }
}
namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign020.ICampaignCardFlowCoordinator132
    {
        readonly CampaignCardFlow132 _campaignCardFlow132 = new CampaignCardFlow132();
        public Campaign020.CampaignInterruptionView132 CampaignInterruption132
        {
            get
            {
                var view = CampaignPlayable020;
                if (view == null || !view.IsAvailable || string.IsNullOrWhiteSpace(view.ActiveOperationId) ||
                    view.LegacyRecoveryRequired || view.InsertedBattleBoundaryRecoveryRequired) return null;
                var progress = _campaign.Guild.GuildCity.Strategic017H.Campaign019;
                var op = progress.Playable020.ActiveOperation;
                if (progress.Playable020.WorldGate023?.ActiveOperation != null) return null;
                var step = view.Steps?.FirstOrDefault(value => value.Status == "CURRENT");
                var result = new Campaign020.CampaignInterruptionView132 {
                    OperationId = op.OperationId, StepIndex = op.CurrentStepIndex,
                    Title = step?.Title ?? view.OperationTitle,
                    Description = step?.Description ?? "The Guild returns from its journey."
                };
                if (op.PendingReceipt != null)
                {
                    result.Identity = op.PendingReceipt.ReceiptId; result.Kind = "STORY";
                    result.Description += "\n\n" + (view.PendingReward ?? "Saved story outcome"); return result;
                }
                if (op.Status == CampaignPlayableOperationStatus020.ReadyToFinalize && progress.PendingReceipt != null)
                {
                    result.Identity = progress.PendingReceipt.ReceiptId; result.Kind = "CHAPTER";
                    result.Title = "CHAPTER COMPLETE"; result.Description = view.OperationTitle + "\nThe saved chapter rewards and world consequences are ready.";
                    return result;
                }
                if (step?.IsWorldBoard == true || view.BattleInProgress || view.AwaitingBattleRewardClaim) return null;
                var request = _campaign.Guild.GuildCity.PendingEncounter;
                if (step?.RequiresCertifiedBattle == true && request != null &&
                    op.Status == CampaignPlayableOperationStatus020.AwaitingBattle)
                {
                    result.Identity = request.RequestId; result.Kind = "BOSS"; return result;
                }
                // Older saves can stop before a fixed story receipt was committed.
                // The explicit resume control prepares that existing authority;
                // merely browsing a saved quest never creates or applies rewards.
                result.Kind = "RESUME"; result.Identity = null; return result;
            }
        }
        public M1CommandResult ResumeCampaignInterruption132(string operationId, int stepIndex)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            var op = _campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.ActiveOperation;
            if (op == null || op.OperationId != operationId || op.CurrentStepIndex != stepIndex)
                return M1CommandResult.Failure("CAMPAIGN132_STORY_BOUNDARY_CHANGED");
            Registry019(); Registry020();
            var prepared = _campaignCardFlow132.PrepareNextInterruption(_campaign, _campaignRules020, _campaignRules019);
            if (prepared.IsSuccess && ReferenceEquals(prepared.Value, _campaign)) return M1CommandResult.Success();
            return ApplyAndPersist(prepared, true, "Saved story interruption ready.");
        }
        public M1CommandResult CompleteCampaignInterruption132(string expectedIdentity)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            var view = CampaignInterruption132;
            if (view == null || string.IsNullOrWhiteSpace(expectedIdentity) || view.Identity != expectedIdentity)
                return M1CommandResult.Failure("CAMPAIGN132_INTERRUPTION_ALREADY_CHANGED");
            if (view.Kind == "BOSS") return EnterPlayableBattle020();
            if (view.Kind == "CHAPTER") return ApplyPlayableChapterResult020();
            if (view.Kind == "STORY") return ApplyPlayableStep020();
            return M1CommandResult.Failure("CAMPAIGN132_COMMITTED_INTERRUPTION_REQUIRED");
        }
    }
}
