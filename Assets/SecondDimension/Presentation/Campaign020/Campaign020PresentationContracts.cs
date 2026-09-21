using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation.Campaign020
{
 public interface ICampaignPlayablePresentationCoordinator020
 {
   CampaignPlayablePresentationState020 CampaignPlayable020{get;}
   M1CommandResult StartPlayableChapter020(string chapterId);
   M1CommandResult CommitPlayableStep020(string outcome);
   M1CommandResult ApplyPlayableStep020();
   M1CommandResult EnterPlayableBattle020();
   M1CommandResult CommitPlayableBattleStepResult020();
   M1CommandResult FinalizePlayableChapter020();
    M1CommandResult ApplyPlayableChapterResult020();
  M1CommandResult RecoverLegacyPlayableQuest020();
  M1CommandResult RecoverInsertedBattleBoundary020();
 }
 public sealed class CampaignPlayablePresentationState020
 {
    public bool IsAvailable; public string Error; public string ActiveOperationId; public string ActiveChapterId; public string OperationTitle; public string WorldId; public string WorldName; public string Status; public int CurrentStepIndex; public int TotalSteps; public string PendingStepReceiptId; public string PendingOutcome; public string PendingReward; public bool ExistingBattleRewardReferenced; public bool BattleInProgress; public bool AwaitingBattleRewardClaim; public bool CampaignEpilogueUnlocked; public bool LegacyRecoveryRequired; public bool InsertedBattleBoundaryRecoveryRequired; public IReadOnlyList<CampaignStepView020> Steps=Array.Empty<CampaignStepView020>(); public IReadOnlyList<RepeatableContractView020> Repeatables=Array.Empty<RepeatableContractView020>(); public IReadOnlyList<WorldContentSummary020> WorldSummaries=Array.Empty<WorldContentSummary020>();
 }
 public sealed class CampaignStepView020 { public string StepId; public string Kind; public string TileType; public string ActionLabel; public string RewardPreview; public string Title; public string Description; public string Status; public bool RequiresCertifiedBattle; public bool ConsumesOperation; public bool IsWorldBoard; }
 public sealed class RepeatableContractView020 { public string ContractId; public string WorldId; public string Title; public string OperationType; public bool Unlocked; public int BattleCount; public int EventCount; }
 public sealed class WorldContentSummary020 { public string WorldId; public string DisplayName; public bool GateUnlocked; public int EnemyArchetypes; public int RecruitOrigins; public int LootEntries; public int RepeatableContracts; public int CrisisOperations; public string StandingTier; }
}
