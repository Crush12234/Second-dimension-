using System;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign020
{
    // Exploration remains the saved three-card089 deck. Authored020 steps are
    // committed one at a time as visible interruptions, never fabricated choices.
    public sealed class CampaignCardFlow132
    {
        readonly CampaignPlayableCommandService020 _steps = new CampaignPlayableCommandService020();
        readonly CampaignCommandService019 _chapters = new CampaignCommandService019();
        readonly CampaignWorldGateCommandService023 _boards = new CampaignWorldGateCommandService023();

        public Result<CampaignState> FinishBoardAndPrepare(CampaignState campaign,
            ICampaignPlayableCatalog020 steps, ICampaignRuleCatalog019 chapters,
            IWorldGateOperationsCatalog023 boards)
        {
            var gate = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.WorldGate023?.ActiveOperation;
            if (gate == null || gate.OperationKind != "CHAPTER")
                return Result<CampaignState>.Failure("CAMPAIGN132_CHAPTER_BOARD_REQUIRED");
            var final = _boards.FinalizeOperation(campaign, boards);
            if (!final.IsSuccess) return final;
            var committed = _steps.CommitCompletedWorldBoardStep(final.Value, steps);
            if (!committed.IsSuccess) return committed;
            var applied = _steps.ApplyStepReceiptExactlyOnce(committed.Value, steps);
            return applied.IsSuccess ? PrepareNextInterruption(applied.Value, steps, chapters) : applied;
        }

        public Result<CampaignState> ApplyStoryAndPrepare(CampaignState campaign,
            ICampaignPlayableCatalog020 steps, ICampaignRuleCatalog019 chapters)
        {
            var applied = _steps.ApplyStepReceiptExactlyOnce(campaign, steps);
            return applied.IsSuccess ? PrepareNextInterruption(applied.Value, steps, chapters) : applied;
        }

        public Result<CampaignState> PrepareNextInterruption(CampaignState campaign,
            ICampaignPlayableCatalog020 steps, ICampaignRuleCatalog019 chapters)
        {
            var city = campaign?.Guild?.GuildCity;
            var progress = city?.Strategic017H?.Campaign019;
            var op = progress?.Playable020?.ActiveOperation;
            if (op == null || steps == null || chapters == null ||
                !steps.TryGetBlueprint(op.ChapterId, out var blueprint))
                return Result<CampaignState>.Failure("CAMPAIGN132_ACTIVE_CHAPTER_REQUIRED");
            blueprint = CampaignReplayBattle134.Effective(campaign, blueprint);
            // A prepared interruption is immutable until its own reveal resolves.
            if (op.PendingReceipt != null)
                return Result<CampaignState>.Success(campaign);
            if (progress.Playable020.WorldGate023?.ActiveOperation != null)
                return Result<CampaignState>.Success(campaign);
            if (op.Status == CampaignPlayableOperationStatus020.ReadyToFinalize)
            {
                if (progress.PendingReceipt != null) return Result<CampaignState>.Success(campaign);
                if (blueprint.RequiresCertifiedBattle)
                    return Result<CampaignState>.Failure("CAMPAIGN132_CLAIMED_CHAPTER_BATTLE_RECEIPT_REQUIRED");
                return _chapters.CommitNonCombatReceipt(campaign, chapters, "SUCCESS");
            }
            if (op.CurrentStepIndex < 0 || op.CurrentStepIndex >= blueprint.Steps.Count)
                return Result<CampaignState>.Failure("CAMPAIGN132_STEP_OUT_OF_RANGE");
            var step = blueprint.Steps[op.CurrentStepIndex];
            if (CampaignAdventureRules084.IsWorldBoardStep084(step))
                return Result<CampaignState>.Success(campaign);
            if (step.RequiresCertifiedBattle)
            {
                if (op.Status == CampaignPlayableOperationStatus020.AwaitingBattle)
                {
                    if (campaign.Battle == null || campaign.Battle.Outcome == BattleOutcome.InProgress ||
                        campaign.Battle.Reward == null || !campaign.Battle.Reward.Claimed || city.PendingEncounter != null)
                        return Result<CampaignState>.Success(campaign);
                    return _steps.CommitBattleStepReceipt(campaign, steps);
                }
                var committed = _chapters.CommitCertifiedBattle(campaign, chapters);
                return committed.IsSuccess ? _steps.MarkBattleCommitted(committed.Value, steps) : committed;
            }
            // Existing validation authenticates the complete prior step ledger,
            // including the WORLD_BOARD completion proof. No flags are advanced
            // on projections, constructor load or a failed command.
            return _steps.CommitNonBattleStep(campaign, steps, "SUCCESS");
        }
    }
}
