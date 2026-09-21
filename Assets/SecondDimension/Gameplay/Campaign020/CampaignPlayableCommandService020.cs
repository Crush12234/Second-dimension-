using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign020
{
    public sealed class CampaignPlayableCommandService020
    {
        static readonly HashSet<string> InsertedBattleChapters084=
            new HashSet<string>(new[]
            {
                "CH018_003","CH018_006","CH018_009","CH018_012",
                "CH018_015","CH018_018","CH018_025","CH018_028",
                "CH018_031","CH018_037","CH018_040","CH018_045",
                "CH018_048","CH018_053","CH018_056","CH018_061",
                "CH018_064","CH018_069","CH018_072","CH018_077",
                "CH018_080"
            },StringComparer.Ordinal);

        public static bool IsInsertedBattleChapter084(string chapterId) =>
            !string.IsNullOrWhiteSpace(chapterId)&&
            InsertedBattleChapters084.Contains(chapterId);

        public Result<CampaignState> BeginOperation(CampaignState campaign,ICampaignPlayableCatalog020 catalog,string chapterId)
        {
            if(campaign?.Guild?.GuildCity==null||catalog==null||string.IsNullOrWhiteSpace(chapterId))return Result<CampaignState>.Failure("CAMPAIGN020_INPUT_REQUIRED");
            if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"CAMPAIGN"))return Result<CampaignState>.Failure("Resume the saved Campaign before starting another quest.");
            var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();var progress=strategic.Campaign019??CampaignProgressState019.Default();var playable=progress.Playable020??CampaignPlayableState020.Default();
            if(progress.ActiveChapterId!=chapterId)return Result<CampaignState>.Failure("CAMPAIGN020_START_CHAPTER019_FIRST");
            if(!catalog.TryGetBlueprint(chapterId,out var blueprint))return Result<CampaignState>.Failure("CAMPAIGN020_BLUEPRINT_UNKNOWN");
            blueprint=CampaignReplayBattle134.Effective(campaign,blueprint);
            if(!CampaignAdventureRules084.IsCompatible084(blueprint,out var compatibilityError))return Result<CampaignState>.Failure("CAMPAIGN020_BLUEPRINT_UNSUPPORTED:"+compatibilityError);
            if(playable.ActiveOperation!=null)
            {
                if(playable.ActiveOperation.ChapterId!=chapterId)
                    return Result<CampaignState>.Failure("CAMPAIGN020_OPERATION_ALREADY_ACTIVE");
                if(!ValidateActiveOperation084(campaign,progress,playable.ActiveOperation,
                    blueprint,catalog,false,out var activeError))
                    return Result<CampaignState>.Failure(activeError);
                return Result<CampaignState>.Success(campaign);
            }
            if(GuildCityExpeditionService017D.HasUnresolvedAdventureOutsideChapter084(
                   campaign))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_FINISH_ACTIVE_ADVENTURE_FIRST");
            if(!StringComparer.Ordinal.Equals(playable.ContentAuthorityVersion,
                   CampaignPlayableState020.ContentVersion))
                playable=ReissuePlayable084(playable,null);
            var identity=OperationIdentity151(campaign,progress,chapterId,blueprint.BlueprintId,progress.CampaignProgress);
            var operationId="OP020_"+identity.Substring(0,24).ToUpperInvariant();
            var op=new CampaignPlayableOperationState020(operationId,blueprint.BlueprintId,chapterId,blueprint.WorldId,identity,0,CampaignPlayableOperationStatus020.Active,Array.Empty<string>(),Array.Empty<string>(),null,string.Empty,"campaign020_operation_started");
            var grantHash=CampaignPlayableBeginGrantHash084(campaign,operationId,
                blueprint.BlueprintId,chapterId,blueprint.WorldId,identity,
                progress.CampaignProgress);
            var grant=new CampaignPlayableBeginGrant020(operationId,
                blueprint.BlueprintId,chapterId,blueprint.WorldId,identity,
                progress.CampaignProgress,grantHash);
            playable=playable.With(activeOperation:op,replaceActiveOperation:true,
                activeOperationGrant:grant,replaceActiveOperationGrant:true,
                activeStepLedger:Array.Empty<CampaignStepReceipt020>(),
                lastCheckpointId:"campaign020_operation_started");
            return Success(campaign,city,strategic,progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:"campaign020_operation_started"));
        }

        public Result<CampaignState> CommitNonBattleStep(CampaignState campaign,ICampaignPlayableCatalog020 catalog,string outcome)
        {
            if(!TryGet(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var op,out var blueprint,out var error))return Result<CampaignState>.Failure(error);
            if(op.Status!=CampaignPlayableOperationStatus020.Active)return Result<CampaignState>.Failure("CAMPAIGN020_OPERATION_NOT_ACTIVE");
            if(op.CurrentStepIndex>=blueprint.Steps.Count)return Result<CampaignState>.Failure("CAMPAIGN020_STEP_OUT_OF_RANGE");var step=blueprint.Steps[op.CurrentStepIndex];
            if(CampaignAdventureRules084.IsWorldBoardStep084(step))return Result<CampaignState>.Failure("CAMPAIGN020_WORLD_BOARD_EXPEDITION_REQUIRED");
            if(step.RequiresCertifiedBattle)return Result<CampaignState>.Failure("CAMPAIGN020_CERTIFIED_BATTLE_REQUIRED");if(op.PendingReceipt!=null)return Result<CampaignState>.Success(campaign);
            var result=string.IsNullOrWhiteSpace(outcome)?"SUCCESS":outcome;
            if(!StringComparer.Ordinal.Equals(result,"SUCCESS"))return Result<CampaignState>.Failure("CAMPAIGN020_OUTCOME_UNSUPPORTED");
            var hash=CanonicalJson.Sha256Hex(new{op.OperationId,step.StepId,result,
                op.CanonicalSeedIdentity});
            var material=blueprint.MaterialIds.Count>0?new[]{new WorldMaterialAmount020(blueprint.MaterialIds[op.CurrentStepIndex%blueprint.MaterialIds.Count],step.ConsumesOperation?2:0)}:Array.Empty<WorldMaterialAmount020>();
            var receipt=new CampaignStepReceipt020("STEPREC020_"+hash.Substring(0,24).ToUpperInvariant(),op.OperationId,step.StepId,result,"NONBATTLE:"+hash,string.Empty,step.ConsumesOperation?2:0,step.ConsumesOperation?2:0,material,0);
            op=op.With(pendingReceipt:receipt,replacePendingReceipt:true,lastCheckpointId:"campaign020_step_committed");playable=playable.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:"campaign020_step_committed");
            return Success(campaign,city,strategic,progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:"campaign020_step_committed"));
        }

        /// <summary>
        /// Advances the one WORLD_BOARD tile only after Campaign 023 has finalized
        /// the matching authored chapter board.  This closes the former generic
        /// nonbattle shortcut without changing existing saves or reward authority.
        /// </summary>
        public Result<CampaignState> CommitCompletedWorldBoardStep(
            CampaignState campaign,
            ICampaignPlayableCatalog020 catalog)
        {
            if(!TryGet(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var op,out var blueprint,out var error))return Result<CampaignState>.Failure(error);
            if(op.Status!=CampaignPlayableOperationStatus020.Active)return Result<CampaignState>.Failure("CAMPAIGN020_OPERATION_NOT_ACTIVE");
            if(op.CurrentStepIndex>=blueprint.Steps.Count)return Result<CampaignState>.Failure("CAMPAIGN020_STEP_OUT_OF_RANGE");
            var step=blueprint.Steps[op.CurrentStepIndex];
            if(!CampaignAdventureRules084.IsWorldBoardStep084(step))return Result<CampaignState>.Failure("CAMPAIGN020_WORLD_BOARD_STEP_REQUIRED");
            if(op.PendingReceipt!=null)return Result<CampaignState>.Success(campaign);
            var runtime=playable.WorldGate023;
            if(!TryExpectedWorldGateCompletionProof084(
                   campaign,city,catalog,op.ChapterId,out var completionProof))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_COMPLETED_WORLD_BOARD_PROOF_REQUIRED");
            var expectedWorldGateOperationId=completionProof.OperationId;
            var expectedWorldGateProofHash=completionProof.CompletionLedgerHash;
            if(runtime==null||runtime.ActiveOperation!=null||
               !runtime.CompletedDefinitionIds.Contains(op.ChapterId)||
               string.IsNullOrWhiteSpace(expectedWorldGateOperationId)||
               !StringComparer.Ordinal.Equals(runtime.LastCheckpointId,
                   "world_gate_023_operation_finalized:"+expectedWorldGateOperationId))
                return Result<CampaignState>.Failure("CAMPAIGN020_COMPLETED_WORLD_BOARD_PROOF_REQUIRED");
            var result="WORLD_GATE_BOARD_COMPLETE";
            var hash=CanonicalJson.Sha256Hex(new{op.OperationId,step.StepId,result,
                WorldGateOperationId=expectedWorldGateOperationId,
                WorldGateCompletionLedgerHash=expectedWorldGateProofHash,
                op.CanonicalSeedIdentity});
            var material=blueprint.MaterialIds.Count>0?new[]{new WorldMaterialAmount020(blueprint.MaterialIds[op.CurrentStepIndex%blueprint.MaterialIds.Count],0)}:Array.Empty<WorldMaterialAmount020>();
            var receipt=new CampaignStepReceipt020("STEPREC020_"+
                hash.Substring(0,24).ToUpperInvariant(),op.OperationId,step.StepId,
                result,"NONBATTLE:"+hash,string.Empty,0,0,material,0,
                expectedWorldGateOperationId,expectedWorldGateProofHash);
            op=op.With(pendingReceipt:receipt,replacePendingReceipt:true,lastCheckpointId:"campaign020_world_board_step_committed");
            playable=playable.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:"campaign020_world_board_step_committed");
            return Success(campaign,city,strategic,progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:"campaign020_world_board_step_committed"));
        }

        public Result<CampaignState> MarkBattleCommitted(CampaignState campaign,ICampaignPlayableCatalog020 catalog)
        {
            if(!TryGet(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var op,out var blueprint,out var error))return Result<CampaignState>.Failure(error);
            if(op.CurrentStepIndex>=blueprint.Steps.Count||!blueprint.Steps[op.CurrentStepIndex].RequiresCertifiedBattle)return Result<CampaignState>.Failure("CAMPAIGN020_BATTLE_STEP_REQUIRED");if(!HasCanonicalCampaignEncounter084(progress,op,city.PendingEncounter))return Result<CampaignState>.Failure("CAMPAIGN020_EXISTING_ENCOUNTER_REQUIRED");
            op=op.With(status:CampaignPlayableOperationStatus020.AwaitingBattle,lastCheckpointId:"campaign020_battle_committed");playable=playable.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:"campaign020_battle_committed");
            return Success(campaign,city,strategic,progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:"campaign020_battle_committed"));
        }

        public Result<CampaignState> CommitBattleStepReceipt(CampaignState campaign,ICampaignPlayableCatalog020 catalog)
        {
            if(!TryGet(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var op,out var blueprint,out var error))return Result<CampaignState>.Failure(error);
            if(op.Status!=CampaignPlayableOperationStatus020.AwaitingBattle)return Result<CampaignState>.Failure("CAMPAIGN020_AWAITING_BATTLE_REQUIRED");if(city.PendingEncounter!=null||city.PendingBattleReturn!=null)return Result<CampaignState>.Failure("CAMPAIGN020_APPLY_EXISTING_BATTLE_RETURN_FIRST");
            if(!HasCanonicalClaimedCampaignBattle084(campaign,progress,op))return Result<CampaignState>.Failure("CAMPAIGN020_CLAIM_EXISTING_REWARD_FIRST");if(op.PendingReceipt!=null)return Result<CampaignState>.Success(campaign);
            var step=blueprint.Steps[op.CurrentStepIndex];var hash=CanonicalJson.Sha256Hex(new{op.OperationId,step.StepId,campaign.Battle.FinalStateHash,campaign.Battle.Reward.RewardId});
            var material=blueprint.MaterialIds.Count>0?new[]{new WorldMaterialAmount020(blueprint.MaterialIds[op.CurrentStepIndex%blueprint.MaterialIds.Count],4)}:Array.Empty<WorldMaterialAmount020>();
            var receipt=new CampaignStepReceipt020("STEPREC020_"+hash.Substring(0,24).ToUpperInvariant(),op.OperationId,step.StepId,campaign.Battle.Outcome.ToString(),campaign.Battle.FinalStateHash,campaign.Battle.Reward.RewardId,4,4,material,0);
            op=op.With(pendingReceipt:receipt,replacePendingReceipt:true,existingBattleRewardReceiptId:campaign.Battle.Reward.RewardId,lastCheckpointId:"campaign020_battle_step_receipt_committed");playable=playable.With(activeOperation:op,replaceActiveOperation:true,lastCheckpointId:"campaign020_battle_step_receipt_committed");
            return Success(campaign,city,strategic,progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:"campaign020_battle_step_receipt_committed"));
        }

        public Result<CampaignState> ApplyStepReceiptExactlyOnce(CampaignState campaign,ICampaignPlayableCatalog020 catalog)
        {
            if(!TryGet(campaign,catalog,out var city,out var strategic,out var progress,out var playable,out var op,out var blueprint,out var error))return Result<CampaignState>.Failure(error);
            var receipt=op.PendingReceipt;if(receipt==null)return Result<CampaignState>.Failure("CAMPAIGN020_STEP_RECEIPT_REQUIRED");
            if(op.AppliedReceiptIds.Contains(receipt.ReceiptId))
            {
                if(!StringComparer.Ordinal.Equals(receipt.OperationId,op.OperationId)||
                   !op.CompletedStepIds.Contains(receipt.StepId))
                    return Result<CampaignState>.Failure("CAMPAIGN020_STALE_APPLIED_RECEIPT_INVALID");
                op=op.With(pendingReceipt:null,replacePendingReceipt:true,
                    lastCheckpointId:"campaign020_stale_receipt_cleared");
                playable=playable.With(activeOperation:op,replaceActiveOperation:true,
                    lastCheckpointId:op.LastCheckpointId);
                return Success(campaign,city,strategic,progress.With(
                    playable020:playable,replacePlayable020:true,
                    lastCheckpointId:op.LastCheckpointId));
            }
            var globallyClaimed=(receipt.GuildXp>0||receipt.HallXp>0)&&
                campaign.Guild.Development.HasClaimedReward(receipt.ReceiptId);
            if(campaign.Guild.Development.HasAdventureAuthority(receipt.ReceiptId))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_GLOBAL_STEP_AUTHORITY_ALREADY_APPLIED");
            if(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count>=
               GuildDevelopmentState.AdventureAuthorityEntryLimit)
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_ADVENTURE_AUTHORITY_LEDGER_FULL");
            if(op.CurrentStepIndex>=blueprint.Steps.Count)return Result<CampaignState>.Failure("CAMPAIGN020_STEP_RECEIPT_MISMATCH");
            var currentStep=blueprint.Steps[op.CurrentStepIndex];
            if(!ValidateStepReceipt084(campaign,city,playable,op,blueprint,catalog,
                currentStep,
                op.CurrentStepIndex,receipt,false,out var receiptError))
                return Result<CampaignState>.Failure(receiptError);
            var applied=new List<string>(op.AppliedReceiptIds){receipt.ReceiptId};
            applied.Sort(StringComparer.Ordinal);
            var proofs=new List<CampaignStepReceipt020>(op.AppliedReceipts){receipt};
            proofs.Sort((left,right)=>StringComparer.Ordinal.Compare(
                left.StepId,right.StepId));
            var completed=new List<string>(op.CompletedStepIds);
            if(!completed.Contains(receipt.StepId))completed.Add(receipt.StepId);
            completed.Sort(StringComparer.Ordinal);
            var materials=globallyClaimed?playable.WorldMaterials:
                MergeMaterials(playable.WorldMaterials,receipt.Materials);
            var next=op.CurrentStepIndex+1;
            var adoptedLegacyResults=false;
            if(InsertedBattleChapters084.Contains(op.ChapterId)&&
               currentStep.RequiresCertifiedBattle&&
               op.CurrentStepIndex==blueprint.Steps.Count-2&&
               TryCreateLegacyInsertedResultsReceipt084(op,blueprint,
                    out var legacyResultsReceipt)&&
               campaign.Guild.Development.HasAdventureAuthority(
                    legacyResultsReceipt.ReceiptId)&&
               campaign.Guild.Development.HasAdventureAuthority(
                    InsertedBattleRecoveryAuthorityId084(op,
                        legacyResultsReceipt))&&
               !campaign.Guild.Development.HasClaimedReward(
                    InsertedBattleRecoveryAuthorityId084(op,
                        legacyResultsReceipt))&&
               !campaign.Guild.Development.HasClaimedReward(
                    legacyResultsReceipt.ReceiptId)&&
               !applied.Contains(legacyResultsReceipt.ReceiptId)&&
               !completed.Contains(legacyResultsReceipt.StepId))
            {
                applied.Add(legacyResultsReceipt.ReceiptId);
                applied.Sort(StringComparer.Ordinal);
                proofs.Add(legacyResultsReceipt);
                proofs.Sort((left,right)=>StringComparer.Ordinal.Compare(
                    left.StepId,right.StepId));
                completed.Add(legacyResultsReceipt.StepId);
                completed.Sort(StringComparer.Ordinal);
                next++;
                adoptedLegacyResults=true;
            }
            var status=next>=blueprint.Steps.Count
                ?CampaignPlayableOperationStatus020.ReadyToFinalize
                :CampaignPlayableOperationStatus020.Active;
            op=op.With(currentStepIndex:next,status:status,
                completedStepIds:completed.AsReadOnly(),
                appliedReceiptIds:applied.AsReadOnly(),pendingReceipt:null,
                replacePendingReceipt:true,
                lastCheckpointId:adoptedLegacyResults
                    ?"campaign020_inserted_battle_recovery_completed"
                    :status==CampaignPlayableOperationStatus020.ReadyToFinalize
                        ?"campaign020_ready_to_finalize":"campaign020_step_applied",
                appliedReceipts:proofs.AsReadOnly());
            playable=playable.With(activeOperation:op,replaceActiveOperation:true,
                activeStepLedger:proofs.AsReadOnly(),worldMaterials:materials,
                lastCheckpointId:op.LastCheckpointId);var updatedProgress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:op.LastCheckpointId);
            var development=campaign.Guild.Development.RecordAdventureAuthority(
                receipt.ReceiptId);if(!globallyClaimed&&(receipt.GuildXp>0||receipt.HallXp>0)&&!development.HasClaimedReward(receipt.ReceiptId))development=development.RecordBattleReward(receipt.ReceiptId,Math.Max(1,receipt.GuildXp),Math.Max(1,receipt.HallXp));var guild=campaign.Guild.With(campaign.Guild.TreasuryXp+(globallyClaimed?0:receipt.GuildXp),campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development).WithGuildCity(city.With(strategic017H:strategic.With(campaign019:updatedProgress,replaceCampaign019:true,lastCheckpointId:op.LastCheckpointId),replaceStrategic017H:true,lastCheckpointId:op.LastCheckpointId));return Result<CampaignState>.Success(campaign.With(guild,campaign.OpeningFlow));
        }

        public Result<CampaignState> CloseCompletedOperation(CampaignState campaign,ICampaignPlayableCatalog020 catalog)
        {
            if(!TryGet(campaign,catalog,out var city,out var strategic,out var progress,
                   out var playable,out var op,out var blueprint,out var error))
            {
                if(!StringComparer.Ordinal.Equals(error,
                       "CAMPAIGN020_ACTIVE_OPERATION_AUTHORITY_INVALID")||
                   !TryGet(campaign,catalog,out city,out strategic,out progress,
                       out playable,out op,out blueprint,out error,true))
                    return Result<CampaignState>.Failure(error);
            }
            if(op.Status!=CampaignPlayableOperationStatus020.ReadyToFinalize&&
               op.Status!=CampaignPlayableOperationStatus020.Complete)
                return Result<CampaignState>.Failure("CAMPAIGN020_OPERATION_NOT_COMPLETE");
            if(progress.PendingReceipt!=null||progress.ActiveOperation!=null||
               !string.IsNullOrWhiteSpace(progress.ActiveChapterId)||
               !progress.CompletedChapterIds.Contains(op.ChapterId)||
               !CampaignReplayRules130.CurrentCompleted(progress).Contains(op.ChapterId)||
               progress.AppliedReceiptIds.Count==0)
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_APPLY_CHAPTER_RECEIPT_FIRST");
            var unlocked=new List<string>(playable.UnlockedRepeatableContractIds);for(var i=0;i<blueprint.RepeatableUnlockIds.Count;i++)if(!unlocked.Contains(blueprint.RepeatableUnlockIds[i]))unlocked.Add(blueprint.RepeatableUnlockIds[i]);
            AddChapterCountRepeatableUnlocks110(campaign,catalog,progress,unlocked);
            unlocked.Sort(StringComparer.Ordinal);var gates=new List<WorldGateProgress020>(playable.WorldGates);var found=false;for(var i=0;i<gates.Count;i++)if(gates[i].WorldId==blueprint.WorldId){gates[i]=gates[i].With(unlocked:true,visitCount:gates[i].VisitCount,localTimeCounter:gates[i].LocalTimeCounter+1);found=true;break;}if(!found)gates.Add(new WorldGateProgress020(blueprint.WorldId,true,1,"INDEPENDENT_COOPERATION",1));gates.Sort((a,b)=>StringComparer.Ordinal.Compare(a.WorldId,b.WorldId));
            var epilogue=playable.CampaignEpilogueUnlocked||progress.CompletedChapterIds.Count>=82;playable=playable.With(activeOperation:null,replaceActiveOperation:true,activeOperationGrant:null,replaceActiveOperationGrant:true,activeStepLedger:Array.Empty<CampaignStepReceipt020>(),unlockedRepeatableContractIds:unlocked.AsReadOnly(),worldGates:gates.AsReadOnly(),campaignEpilogueUnlocked:epilogue,lastCheckpointId:"campaign020_operation_closed");
            return Success(campaign,city,strategic,progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:"campaign020_operation_closed"));
        }

        static void AddChapterCountRepeatableUnlocks110(CampaignState campaign,
            ICampaignPlayableCatalog020 catalog,CampaignProgressState019 progress,
            List<string> unlocked)
        {
            if(!(catalog is ICampaignRepeatableUnlockCatalog020 rules))return;
            var runtime=progress.Playable020?.WorldGate023;
            if(runtime==null||catalog.WorldGateProofCatalog023==null||
               !CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                   campaign,catalog.WorldGateProofCatalog023,runtime))return;
            var completed=new HashSet<string>(progress.CompletedChapterIds,
                StringComparer.Ordinal);
            var counts=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(var proof in runtime.CompletionProofs)
            {
                if(!completed.Contains(proof.DefinitionId)||
                   !runtime.CompletedDefinitionIds.Contains(proof.DefinitionId)||
                   !catalog.TryGetBlueprint(proof.DefinitionId,out var blueprint)||
                   !StringComparer.Ordinal.Equals(proof.WorldId,blueprint.WorldId))
                    continue;
                counts.TryGetValue(blueprint.WorldId,out var count);
                counts[blueprint.WorldId]=count+1;
            }
            foreach(var rule in rules.RepeatableUnlockRules)
                if(rule.UnlockedByChapterCount>0&&
                   counts.TryGetValue(rule.WorldId,out var count)&&
                   count>=rule.UnlockedByChapterCount&&
                   !unlocked.Contains(rule.ContractId))unlocked.Add(rule.ContractId);
        }

        public bool IsReadyForChapterReceipt084(CampaignState campaign,
            ICampaignPlayableCatalog020 catalog,string chapterId,out string error)
        {
            error="CAMPAIGN020_OPERATION_NOT_READY";
            if(!TryGet(campaign,catalog,out _,out _,out _,out _,out var operation,
                   out var blueprint,out var authorityError))
            {error=authorityError;return false;}
            if(!StringComparer.Ordinal.Equals(operation.ChapterId,chapterId)||
               operation.Status!=CampaignPlayableOperationStatus020.ReadyToFinalize||
               operation.PendingReceipt!=null||
               operation.CurrentStepIndex!=blueprint.Steps.Count)
                return false;
            error=string.Empty;
            return true;
        }

        /// <summary>
        /// Release 084 added a certified battle immediately before the preserved
        /// RESULTS tile on 21 authored chapter boards. Saves made at that old
        /// boundary need a narrow ledger repair before normal commands may resume.
        /// </summary>
        public bool RequiresInsertedBattleBoundaryRecovery084(
            CampaignState campaign,ICampaignPlayableCatalog020 catalog)
        {
            if(campaign?.Guild?.GuildCity==null||catalog==null)return false;
            var city=campaign.Guild.GuildCity;
            var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();
            var progress=strategic.Campaign019??CampaignProgressState019.Default();
            var playable=progress.Playable020??CampaignPlayableState020.Default();
            var operation=playable.ActiveOperation;
            if(operation==null||
               !catalog.TryGetBlueprint(operation.ChapterId,out var blueprint))
                return false;
            return TryClassifyInsertedBattleBoundaryRecovery084(campaign,city,
                progress,playable,operation,blueprint,catalog,out _,out _);
        }

        public Result<CampaignState> RecoverInsertedBattleBoundary084(
            CampaignState campaign,ICampaignPlayableCatalog020 catalog)
        {
            if(campaign?.Guild?.GuildCity==null||catalog==null)
                return Result<CampaignState>.Failure("CAMPAIGN020_INPUT_REQUIRED");
            var city=campaign.Guild.GuildCity;
            var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();
            var progress=strategic.Campaign019??CampaignProgressState019.Default();
            var playable=progress.Playable020??CampaignPlayableState020.Default();
            var operation=playable.ActiveOperation;
            if(operation==null||
               !catalog.TryGetBlueprint(operation.ChapterId,out var blueprint)||
               !TryClassifyInsertedBattleBoundaryRecovery084(campaign,city,
                    progress,playable,operation,blueprint,catalog,out var kind,
                    out var legacyResultsReceipt))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_INSERTED_BATTLE_RECOVERY_NOT_REQUIRED");

            var battleIndex=blueprint.Steps.Count-2;
            IReadOnlyList<string> completed=operation.CompletedStepIds;
            IReadOnlyList<string> applied=operation.AppliedReceiptIds;
            IReadOnlyList<CampaignStepReceipt020> proofs=operation.AppliedReceipts;
            var checkpoint="campaign020_inserted_battle_pending_result_recovered";
            var authorityCount=campaign.Guild.Development
                .AppliedAdventureAuthorityIds.Count;
            if(kind==InsertedBattleBoundaryRecoveryKind084.PendingLegacyResults&&
               authorityCount>GuildDevelopmentState.AdventureAuthorityEntryLimit-2)
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_ADVENTURE_AUTHORITY_LEDGER_FULL");
            if(kind==InsertedBattleBoundaryRecoveryKind084.AppliedLegacyResults)
            {
                var migrationAuthority=InsertedBattleRecoveryAuthorityId084(
                    operation,legacyResultsReceipt);
                var markerAlreadyRecorded=campaign.Guild.Development
                    .HasAdventureAuthority(migrationAuthority);
                if(markerAlreadyRecorded&&campaign.Guild.Development
                       .HasClaimedReward(migrationAuthority))
                    return Result<CampaignState>.Failure(
                        "CAMPAIGN020_INSERTED_BATTLE_RECOVERY_AUTHORITY_INVALID");
                var slotsRequired=markerAlreadyRecorded?1:2;
                if(authorityCount>
                   GuildDevelopmentState.AdventureAuthorityEntryLimit-slotsRequired)
                    return Result<CampaignState>.Failure(
                        "CAMPAIGN020_ADVENTURE_AUTHORITY_LEDGER_FULL");
                completed=operation.CompletedStepIds.Where(value=>
                    !StringComparer.Ordinal.Equals(value,
                        legacyResultsReceipt.StepId)).ToArray();
                applied=operation.AppliedReceiptIds.Where(value=>
                    !StringComparer.Ordinal.Equals(value,
                        legacyResultsReceipt.ReceiptId)).ToArray();
                proofs=operation.AppliedReceipts.Where(value=>
                    !StringComparer.Ordinal.Equals(value.ReceiptId,
                        legacyResultsReceipt.ReceiptId)).ToArray();
                checkpoint="campaign020_inserted_battle_applied_result_recovered";
                var development=campaign.Guild.Development
                    .RecordAdventureAuthority(migrationAuthority);
                campaign=campaign.With(campaign.Guild.With(
                    campaign.Guild.TreasuryXp,campaign.Guild.Recruits,
                    campaign.Guild.Unions,campaign.Guild.Inventory,development),
                    campaign.OpeningFlow);
            }
            operation=operation.With(currentStepIndex:battleIndex,
                status:CampaignPlayableOperationStatus020.Active,
                completedStepIds:completed,appliedReceiptIds:applied,
                pendingReceipt:null,replacePendingReceipt:true,
                lastCheckpointId:checkpoint,appliedReceipts:proofs);
            playable=playable.With(activeOperation:operation,
                replaceActiveOperation:true,activeStepLedger:proofs,
                lastCheckpointId:checkpoint);
            progress=progress.With(playable020:playable,replacePlayable020:true,
                lastCheckpointId:checkpoint);
            return Success(campaign,city,strategic,progress);
        }

        enum InsertedBattleBoundaryRecoveryKind084
        {
            None,
            PendingLegacyResults,
            AppliedLegacyResults
        }

        static bool TryCreateLegacyInsertedResultsReceipt084(
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,
            out CampaignStepReceipt020 receipt)
        {
            receipt=null;
            if(operation==null||blueprint?.Steps==null||blueprint.Steps.Count<2)
                return false;
            var battleIndex=blueprint.Steps.Count-2;
            var battle=blueprint.Steps[battleIndex];
            var results=blueprint.Steps[battleIndex+1];
            if(!blueprint.RequiresCertifiedBattle||battle==null||results==null||
               !battle.RequiresCertifiedBattle||!battle.ConsumesOperation||
               !StringComparer.Ordinal.Equals(battle.Kind,"CERTIFIED_BATTLE")||
               !StringComparer.Ordinal.Equals(battle.StepId,
                    "STEP020_"+operation.ChapterId+"_BATTLE")||
               results.RequiresCertifiedBattle||results.ConsumesOperation||
               !StringComparer.Ordinal.Equals(results.Kind,"RESULTS"))return false;
            const string outcome="SUCCESS";
            var hash=CanonicalJson.Sha256Hex(new
            {
                operation.OperationId,results.StepId,result=outcome,
                operation.CanonicalSeedIdentity
            });
            var materials=blueprint.MaterialIds.Count>0
                ?new[]{new WorldMaterialAmount020(
                    blueprint.MaterialIds[battleIndex%blueprint.MaterialIds.Count],0)}
                :Array.Empty<WorldMaterialAmount020>();
            receipt=new CampaignStepReceipt020(
                "STEPREC020_"+hash.Substring(0,24).ToUpperInvariant(),
                operation.OperationId,results.StepId,outcome,"NONBATTLE:"+hash,
                string.Empty,0,0,materials,0);
            return true;
        }

        static string InsertedBattleRecoveryAuthorityId084(
            CampaignPlayableOperationState020 operation,
            CampaignStepReceipt020 legacyResultsReceipt)
        {
            var hash=CanonicalJson.Sha256Hex(new
            {
                Authority="CAMPAIGN020_INSERTED_BATTLE_RECOVERY_084",
                operation.OperationId,operation.BlueprintId,operation.ChapterId,
                LegacyResultsReceiptId=legacyResultsReceipt.ReceiptId
            });
            return "MIGAUTH020_"+hash.Substring(0,24).ToUpperInvariant();
        }

        static bool TryClassifyInsertedBattleBoundaryRecovery084(
            CampaignState campaign,GuildCityState017D city,
            CampaignProgressState019 progress,CampaignPlayableState020 playable,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,ICampaignPlayableCatalog020 catalog,
            out InsertedBattleBoundaryRecoveryKind084 kind,
            out CampaignStepReceipt020 legacyResultsReceipt)
        {
            kind=InsertedBattleBoundaryRecoveryKind084.None;
            legacyResultsReceipt=null;
            if(campaign==null||city==null||progress==null||playable==null||
               operation==null||blueprint==null||catalog==null||
               !IsInsertedBattleChapter084(operation.ChapterId)||
               RequiresLegacyRecovery084(playable)||blueprint.Steps.Count<2||
               !CampaignAdventureRules084.IsCompatible084(blueprint,out _))
                return false;
            var battleIndex=blueprint.Steps.Count-2;
            var resultIndex=blueprint.Steps.Count-1;
            var battle=blueprint.Steps[battleIndex];
            var results=blueprint.Steps[resultIndex];
            if(!blueprint.RequiresCertifiedBattle||battle==null||results==null||
               !battle.RequiresCertifiedBattle||!battle.ConsumesOperation||
               !StringComparer.Ordinal.Equals(battle.Kind,"CERTIFIED_BATTLE")||
               !StringComparer.Ordinal.Equals(battle.StepId,
                    "STEP020_"+operation.ChapterId+"_BATTLE")||
               results.RequiresCertifiedBattle||results.ConsumesOperation||
               !StringComparer.Ordinal.Equals(results.Kind,"RESULTS")||
               !StringComparer.Ordinal.Equals(operation.ChapterId,
                    blueprint.ChapterId)||
               !StringComparer.Ordinal.Equals(operation.BlueprintId,
                    blueprint.BlueprintId)||
               !StringComparer.Ordinal.Equals(operation.WorldId,blueprint.WorldId)||
               !StringComparer.Ordinal.Equals(progress.ActiveChapterId,
                    operation.ChapterId)||progress.ActiveOperation==null||
               progress.ActiveOperation.BattleCommitted||progress.PendingReceipt!=null||
               !StringComparer.Ordinal.Equals(progress.ActiveOperation.ChapterId,
                    operation.ChapterId)||
               !StringComparer.Ordinal.Equals(progress.ActiveOperation.WorldId,
                    operation.WorldId)||
               !string.IsNullOrWhiteSpace(operation.ExistingBattleRewardReceiptId)||
               !ValidateBeginGrant084(campaign,progress,playable,operation,
                    blueprint,false)||
               !MatchesOperationIdentity084(campaign,progress,operation,blueprint,
                    false)||
               !SameStepLedger084(playable.ActiveStepLedger,
                    operation.AppliedReceipts)||
               GuildCityExpeditionService017D
                    .HasUnresolvedAdventureOutsideChapter084(campaign)||
               !ValidateInsertedBattleRecoveryPrefix084(campaign,city,playable,
                    operation,blueprint,catalog,battleIndex))
                return false;

            if(operation.Status==CampaignPlayableOperationStatus020.Active&&
               StringComparer.Ordinal.Equals(operation.LastCheckpointId,
                    "campaign020_step_committed")&&
               StringComparer.Ordinal.Equals(playable.LastCheckpointId,
                    "campaign020_step_committed")&&
               StringComparer.Ordinal.Equals(progress.LastCheckpointId,
                    "campaign020_step_committed")&&
               operation.CurrentStepIndex==battleIndex&&
               operation.CompletedStepIds.Count==battleIndex&&
               operation.AppliedReceiptIds.Count==battleIndex&&
               operation.AppliedReceipts.Count==battleIndex&&
               operation.PendingReceipt!=null&&
               !operation.CompletedStepIds.Contains(results.StepId)&&
               !operation.AppliedReceiptIds.Contains(
                    operation.PendingReceipt.ReceiptId)&&
               !campaign.Guild.Development.HasAdventureAuthority(
                    operation.PendingReceipt.ReceiptId)&&
               !campaign.Guild.Development.HasClaimedReward(
                    operation.PendingReceipt.ReceiptId)&&
               ValidateStepReceipt084(campaign,city,playable,operation,blueprint,
                    catalog,results,battleIndex,operation.PendingReceipt,false,out _))
            {
                kind=InsertedBattleBoundaryRecoveryKind084.PendingLegacyResults;
                legacyResultsReceipt=operation.PendingReceipt;
                return IsZeroRewardReceipt084(legacyResultsReceipt);
            }

            if(operation.Status!=CampaignPlayableOperationStatus020.ReadyToFinalize||
               !StringComparer.Ordinal.Equals(operation.LastCheckpointId,
                    "campaign020_ready_to_finalize")||
               !StringComparer.Ordinal.Equals(playable.LastCheckpointId,
                    "campaign020_ready_to_finalize")||
               !StringComparer.Ordinal.Equals(progress.LastCheckpointId,
                    "campaign020_ready_to_finalize")||
               operation.CurrentStepIndex!=resultIndex||
               operation.PendingReceipt!=null||
               operation.CompletedStepIds.Count!=resultIndex||
               operation.AppliedReceiptIds.Count!=resultIndex||
               operation.AppliedReceipts.Count!=resultIndex||
               operation.CompletedStepIds.Contains(battle.StepId)||
               operation.AppliedReceipts.Any(value=>
                    StringComparer.Ordinal.Equals(value.StepId,battle.StepId)))
                return false;
            var expectedCompleted=blueprint.Steps.Take(battleIndex)
                .Select(value=>value.StepId).Concat(new[]{results.StepId})
                .OrderBy(value=>value,StringComparer.Ordinal).ToArray();
            if(!operation.CompletedStepIds.SequenceEqual(expectedCompleted,
                   StringComparer.Ordinal))return false;
            var matches=operation.AppliedReceipts.Where(value=>value!=null&&
                StringComparer.Ordinal.Equals(value.StepId,results.StepId)).ToArray();
            if(matches.Length!=1||
               !operation.AppliedReceiptIds.Contains(matches[0].ReceiptId)||
               !campaign.Guild.Development.HasAdventureAuthority(
                    matches[0].ReceiptId)||
               campaign.Guild.Development.HasClaimedReward(matches[0].ReceiptId)||
               campaign.Guild.Development.HasAdventureAuthority(
                    InsertedBattleRecoveryAuthorityId084(operation,matches[0]))||
               campaign.Guild.Development.HasClaimedReward(
                    InsertedBattleRecoveryAuthorityId084(operation,matches[0]))||
               !ValidateStepReceipt084(campaign,city,playable,operation,blueprint,
                    catalog,results,battleIndex,matches[0],true,out _)||
               !IsZeroRewardReceipt084(matches[0]))return false;
            kind=InsertedBattleBoundaryRecoveryKind084.AppliedLegacyResults;
            legacyResultsReceipt=matches[0];
            return true;
        }

        static bool ValidateInsertedBattleRecoveryPrefix084(
            CampaignState campaign,GuildCityState017D city,
            CampaignPlayableState020 playable,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,ICampaignPlayableCatalog020 catalog,
            int prefixCount)
        {
            var expected=blueprint.Steps.Take(prefixCount)
                .Select(value=>value.StepId).OrderBy(value=>value,
                    StringComparer.Ordinal).ToArray();
            if(operation.CompletedStepIds.Count<expected.Length||
               expected.Any(value=>!operation.CompletedStepIds.Contains(value))||
               operation.AppliedReceipts.Count<expected.Length||
               operation.AppliedReceipts.Any(value=>value==null||
                    string.IsNullOrWhiteSpace(value.StepId)||
                    string.IsNullOrWhiteSpace(value.ReceiptId))||
               operation.AppliedReceipts.Select(value=>value.ReceiptId)
                    .Distinct(StringComparer.Ordinal).Count()!=
                    operation.AppliedReceipts.Count||
               operation.AppliedReceipts.Select(value=>value.StepId)
                    .Distinct(StringComparer.Ordinal).Count()!=
                    operation.AppliedReceipts.Count)return false;
            var byStep=operation.AppliedReceipts.ToDictionary(
                value=>value.StepId,value=>value,StringComparer.Ordinal);
            for(var index=0;index<prefixCount;index++)
            {
                var step=blueprint.Steps[index];
                if(!byStep.TryGetValue(step.StepId,out var receipt)||
                   !operation.AppliedReceiptIds.Contains(receipt.ReceiptId)||
                   !campaign.Guild.Development.HasAdventureAuthority(
                        receipt.ReceiptId)||
                   !ValidateStepReceipt084(campaign,city,playable,operation,
                        blueprint,catalog,step,index,receipt,true,out _))return false;
            }
            return true;
        }

        static bool IsZeroRewardReceipt084(CampaignStepReceipt020 receipt) =>
            receipt!=null&&receipt.GuildXp==0&&receipt.HallXp==0&&
            receipt.Materials.All(value=>value!=null&&value.Amount==0)&&
            string.IsNullOrWhiteSpace(
                receipt.ExistingEquipmentRewardReceiptId);

        public Result<CampaignState> AbortUnverifiableLegacyOperation084(
            CampaignState campaign)
        {
            if(campaign?.Guild?.GuildCity==null)
                return Result<CampaignState>.Failure("CAMPAIGN020_INPUT_REQUIRED");
            var city=campaign.Guild.GuildCity;
            var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();
            var progress=strategic.Campaign019??CampaignProgressState019.Default();
            var playable=progress.Playable020??CampaignPlayableState020.Default();
            if(!RequiresLegacyRecovery084(playable))
                return Result<CampaignState>.Failure(
                    "CAMPAIGN020_LEGACY_RECOVERY_NOT_REQUIRED");
            var worldGate=playable.WorldGate023??WorldGateRuntimeState023.Default();
            if(!CanRecoverLegacyHandoff084(campaign,city,strategic,progress,playable,
                   worldGate))
                 return Result<CampaignState>.Failure(
                     "CAMPAIGN020_LEGACY_RECOVERY_HANDOFF_LINK_UNPROVEN");
             if(worldGate.ActiveOperation!=null)
             {
                 var nested=new CampaignWorldGateCommandService023()
                     .AbortUnverifiableLegacyOperation084(campaign);
                 if(!nested.IsSuccess)return Result<CampaignState>.Failure(
                     nested.Errors.Count>0?nested.Errors[0]:
                         "CAMPAIGN020_NESTED_WORLD_GATE_RECOVERY_FAILED");
                 campaign=nested.Value;city=campaign.Guild.GuildCity;
                 strategic=city.Strategic017H??GuildCityStrategicState017H.Default();
                 progress=strategic.Campaign019??CampaignProgressState019.Default();
                 playable=progress.Playable020??CampaignPlayableState020.Default();
                 worldGate=playable.WorldGate023??WorldGateRuntimeState023.Default();
             }
             else if(CampaignWorldGateCommandService023.RequiresLegacyRecovery084(
                         worldGate))
             {
                 worldGate=CampaignWorldGateCommandService023
                     .CreateRecoveredLegacyRuntime084(campaign,worldGate,
                         city.OperationOrdinal,
                         "campaign020_legacy_run_recovered_no_reward");
                 if(worldGate==null)return Result<CampaignState>.Failure(
                     "CAMPAIGN020_WORLD_GATE_LEGACY_RECOVERY_FAILED");
             }
             else worldGate=new WorldGateRuntimeState023(
                 WorldGateRuntimeState023.ContentVersion,null,worldGate.CurrentWorldId,
                worldGate.UnlockedWorldIds,worldGate.WorldStandings,
                worldGate.CompletedDefinitionIds,worldGate.UnlockedRecruitOriginIds,
                 worldGate.RepeatableStreakDefinitionId,worldGate.RepeatableStreakCount,
                 worldGate.TravelSupplies,"campaign020_legacy_run_recovered_no_reward",
                 worldGate.LastCompletionProof,worldGate.CompletionProofs,
                 worldGate.AuthorityChainBaseHash,
                 worldGate.AuthorityChainBaseOperationOrdinal,
                 worldGate.AuthorityChainBaseStandings,
                 worldGate.AuthorityChainBaseUnlockedOrigins,
                  worldGate.AuthorityChainBaseUnlockedWorlds,
                  worldGate.AuthorityChainBaseCompletedDefinitions,
                  worldGate.AuthorityEntries,worldGate.AuthorityChainHash,
                  worldGate.AuthorityChainBaseCurrentWorldId,
                  worldGate.AuthorityChainBaseTravelSupplies,
                  worldGate.AuthorityChainBaseRepeatableStreakDefinitionId,
                  worldGate.AuthorityChainBaseRepeatableStreakCount,
                  worldGate.AuthorityChainMigrationReceiptId,
                  expeditionRecruitLeadIds089:worldGate.ExpeditionRecruitLeadIds089,
                  expeditionDeckTutorialSeen089:
                      worldGate.ExpeditionDeckTutorialSeen089);
            playable=ReissuePlayable084(playable,null,worldGate);
            progress=progress.With(activeChapterId:string.Empty,activeOperation:null,
                replaceActiveOperation:true,pendingReceipt:null,
                replacePendingReceipt:true,playable020:playable,
                replacePlayable020:true,
                lastCheckpointId:"campaign020_legacy_run_recovered_no_reward");
            strategic=strategic.With(campaign019:progress,replaceCampaign019:true,
                lastCheckpointId:progress.LastCheckpointId);
            city=city.With(activeContract:null,replaceActiveContract:true,
                expedition:null,replaceExpedition:true,pendingEncounter:null,
                replacePendingEncounter:true,pendingBattleReturn:null,
                replacePendingBattleReturn:true,strategic017H:strategic,
                replaceStrategic017H:true,lastCheckpointId:progress.LastCheckpointId);
            return Result<CampaignState>.Success(campaign.With(
                campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
        }

        static bool CanRecoverLegacyHandoff084(CampaignState campaign,
            GuildCityState017D city,GuildCityStrategicState017H strategic,
            CampaignProgressState019 progress,CampaignPlayableState020 playable,
            WorldGateRuntimeState023 worldGate)
        {
            var legacyOperation=playable?.ActiveOperation;
            if(campaign==null||city==null||strategic==null||progress==null||
               legacyOperation==null||
               strategic.ActiveDefense!=null||
               playable.Progression022?.ActiveAbyssOperation!=null||
               campaign.Battle?.Outcome==BattleOutcome.InProgress||
               (campaign.Battle?.Reward!=null&&!campaign.Battle.Reward.Claimed))
                return false;
            var chronicles=playable.RecruitChronicles025;
            if(chronicles?.PersonalQuests!=null)
                for(var index=0;index<chronicles.PersonalQuests.Count;index++)
                    if(chronicles.PersonalQuests[index]!=null&&
                       !chronicles.PersonalQuests[index].Completed&&
                       !RecruitChronicleRules025.IsArchivedLegacyQuest(chronicles,
                           campaign.CampaignSeed,
                           chronicles.PersonalQuests[index]))
                         return false;
            if(!string.IsNullOrWhiteSpace(progress.ActiveChapterId)&&
               !StringComparer.Ordinal.Equals(progress.ActiveChapterId,
                   legacyOperation.ChapterId))return false;
            if(progress.ActiveOperation!=null&&
               (!StringComparer.Ordinal.Equals(progress.ActiveChapterId,
                    legacyOperation.ChapterId)||
                !StringComparer.Ordinal.Equals(progress.ActiveOperation.ChapterId,
                    legacyOperation.ChapterId)||
                !StringComparer.Ordinal.Equals(progress.ActiveOperation.WorldId,
                    legacyOperation.WorldId)))return false;
            if(progress.PendingReceipt!=null&&
               (progress.ActiveOperation==null||
                !StringComparer.Ordinal.Equals(progress.PendingReceipt.ChapterId,
                    legacyOperation.ChapterId)||
                !StringComparer.Ordinal.Equals(progress.PendingReceipt.RequestId,
                    progress.ActiveOperation.RequestId)))return false;
            var nested=worldGate?.ActiveOperation;
            if(nested==null)
                return city.ActiveContract==null&&city.Expedition==null&&
                       city.PendingEncounter==null&&city.PendingBattleReturn==null;
            return CampaignWorldGateCommandService023.RequiresLegacyRecovery084(
                       worldGate)&&
                   CampaignWorldGateCommandService023
                       .HasCanonicalLegacyHandoff084(city,nested);
        }

        public static bool RequiresLegacyRecovery084(
            CampaignPlayableState020 playable)
        {
            var operation=playable?.ActiveOperation;
            return operation!=null&&
                   (!StringComparer.Ordinal.Equals(playable.ContentAuthorityVersion,
                         CampaignPlayableState020.ContentVersion)||
                    playable.ActiveOperationGrant==null||
                    playable.ActiveStepLedger==null||
                    playable.ActiveStepLedger.Count!=operation.CurrentStepIndex||
                    operation.AppliedReceipts.Count!=operation.CurrentStepIndex||
                    operation.AppliedReceiptIds.Count!=operation.AppliedReceipts.Count||
                    operation.CompletedStepIds.Count!=operation.CurrentStepIndex);
        }

        static bool TryGet(CampaignState campaign,ICampaignPlayableCatalog020 catalog,out GuildCityState017D city,out GuildCityStrategicState017H strategic,out CampaignProgressState019 progress,out CampaignPlayableState020 playable,out CampaignPlayableOperationState020 op,out CampaignBlueprintRule020 blueprint,out string error,bool allowAppliedChapterProgress=false)
        {
            city=null;strategic=null;progress=null;playable=null;op=null;blueprint=null;error="";if(campaign?.Guild?.GuildCity==null||catalog==null){error="CAMPAIGN020_INPUT_REQUIRED";return false;}city=campaign.Guild.GuildCity;strategic=city.Strategic017H??GuildCityStrategicState017H.Default();progress=strategic.Campaign019??CampaignProgressState019.Default();playable=progress.Playable020??CampaignPlayableState020.Default();op=playable.ActiveOperation;if(op==null){error="CAMPAIGN020_ACTIVE_OPERATION_REQUIRED";return false;}if(RequiresLegacyRecovery084(playable)){error="CAMPAIGN020_LEGACY_OPERATION_RECOVERY_REQUIRED";return false;}if(!catalog.TryGetBlueprint(op.ChapterId,out blueprint)){error="CAMPAIGN020_BLUEPRINT_UNKNOWN";return false;}blueprint=CampaignReplayBattle134.Effective(campaign,blueprint);if(!CampaignAdventureRules084.IsCompatible084(blueprint,out var compatibilityError)){error="CAMPAIGN020_BLUEPRINT_UNSUPPORTED:"+compatibilityError;return false;}
            if(TryClassifyInsertedBattleBoundaryRecovery084(campaign,city,progress,
                   playable,op,blueprint,catalog,out _,out _))
            {
                error="CAMPAIGN020_INSERTED_BATTLE_RECOVERY_REQUIRED";
                return false;
            }
            return ValidateActiveOperation084(campaign,progress,op,blueprint,
                catalog,allowAppliedChapterProgress,out error);
        }
        static bool ValidateActiveOperation084(
            CampaignState campaign,
            CampaignProgressState019 progress,
            CampaignPlayableOperationState020 op,
            CampaignBlueprintRule020 blueprint,
            ICampaignPlayableCatalog020 catalog,
            bool allowAppliedChapterProgress,
            out string error)
        {
            error="CAMPAIGN020_ACTIVE_OPERATION_AUTHORITY_INVALID";
            if(campaign==null||progress==null||op==null||blueprint==null||
               !StringComparer.Ordinal.Equals(op.ChapterId,blueprint.ChapterId)||
               !StringComparer.Ordinal.Equals(op.BlueprintId,blueprint.BlueprintId)||
               !StringComparer.Ordinal.Equals(op.WorldId,blueprint.WorldId)||
               op.CurrentStepIndex<0||op.CurrentStepIndex>blueprint.Steps.Count)
                return false;
            var terminal=op.Status==CampaignPlayableOperationStatus020.ReadyToFinalize||
                         op.Status==CampaignPlayableOperationStatus020.Complete;
            if(terminal!=(op.CurrentStepIndex==blueprint.Steps.Count))return false;
            if(!terminal&&op.CurrentStepIndex>=blueprint.Steps.Count)return false;
            if(op.Status==CampaignPlayableOperationStatus020.AwaitingBattle&&
               !blueprint.Steps[op.CurrentStepIndex].RequiresCertifiedBattle)return false;
            var expectedCompleted=blueprint.Steps.Take(op.CurrentStepIndex)
                .Select(value=>value.StepId).OrderBy(value=>value,StringComparer.Ordinal)
                .ToArray();
            var playable=progress.Playable020;
            var city=campaign.Guild?.GuildCity;
            if(playable==null||city==null||
               !StringComparer.Ordinal.Equals(playable.ContentAuthorityVersion,
                    CampaignPlayableState020.ContentVersion)||
               !ValidateBeginGrant084(campaign,progress,playable,op,blueprint,
                    allowAppliedChapterProgress)||
               !SameStepLedger084(playable.ActiveStepLedger,op.AppliedReceipts)||
               op.CompletedStepIds.Count!=op.CurrentStepIndex||
               op.AppliedReceiptIds.Count!=op.CurrentStepIndex||
               op.AppliedReceipts.Count!=op.CurrentStepIndex||
               !op.CompletedStepIds.SequenceEqual(expectedCompleted,StringComparer.Ordinal)||
               !ValidateAppliedReceiptLedger084(campaign,city,playable,op,blueprint,
                    catalog))
                return false;
            // Campaign019 applies its chapter receipt immediately before C020 closes,
            // so only that close call may observe the cleared active chapter and the
            // advanced campaign-progress value. Close grants no step rewards.
            if(allowAppliedChapterProgress)
            {
                if(progress.ActiveOperation!=null||
                   !string.IsNullOrWhiteSpace(progress.ActiveChapterId)||
                   !progress.CompletedChapterIds.Contains(op.ChapterId)||
               !CampaignReplayRules130.CurrentCompleted(progress).Contains(op.ChapterId)||
                   progress.PendingReceipt!=null||progress.AppliedReceiptIds.Count==0||
                   !MatchesOperationIdentity084(campaign,progress,op,blueprint,true))
                    return false;
                error=string.Empty;
                return true;
            }
            if(!StringComparer.Ordinal.Equals(progress.ActiveChapterId,op.ChapterId))
                return false;
            if(!MatchesOperationIdentity084(campaign,progress,op,blueprint,false))
                return false;
            error=string.Empty;
            return true;
        }
        internal static bool HasCanonicalActiveOperation084(
            CampaignState campaign,
            CampaignProgressState019 progress,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,
            ICampaignPlayableCatalog020 catalog) =>
            ValidateActiveOperation084(campaign,progress,operation,blueprint,catalog,false,
                out _);
        static bool TryExpectedWorldGateCompletionProof084(
            CampaignState campaign,
            GuildCityState017D city,
            ICampaignPlayableCatalog020 catalog,
            string definitionId,
            out WorldGateCompletionProof023 proof)
        {
            proof=null;
            var runtime=city?.Strategic017H?.Campaign019?.Playable020?.WorldGate023;
            if(campaign==null||city==null||catalog?.WorldGateProofCatalog023==null||
               runtime==null||runtime.ActiveOperation!=null||
                string.IsNullOrWhiteSpace(definitionId)||
               !runtime.CompletedDefinitionIds.Contains(definitionId)||
               !CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                    campaign,catalog.WorldGateProofCatalog023,runtime))return false;
            var matches=runtime.CompletionProofs.Where(value=>value!=null&&
                StringComparer.Ordinal.Equals(value.DefinitionId,definitionId)).ToArray();
            if(matches.Length!=1||
               !CampaignReplayRules130.IsCurrentCycleProof(
                   city.Strategic017H.Campaign019,matches[0])||
               !StringComparer.Ordinal.Equals(runtime.LastCheckpointId,
                   "world_gate_023_operation_finalized:"+matches[0].OperationId))
                return false;
            proof=matches[0];
            return true;
        }
        static bool ValidateStepReceipt084(
            CampaignState campaign,
            GuildCityState017D city,
            CampaignPlayableState020 playable,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,
            ICampaignPlayableCatalog020 catalog,
            CampaignStepRule020 step,
            int stepIndex,
            CampaignStepReceipt020 receipt,
            bool historical,
            out string error)
        {
            error="CAMPAIGN020_STEP_RECEIPT_INVALID";
            if(receipt==null||receipt.AppliedVersion!=0||
               !StringComparer.Ordinal.Equals(receipt.OperationId,operation.OperationId)||
               !StringComparer.Ordinal.Equals(receipt.StepId,step.StepId))return false;
            string hash;
            string authoritativeHash;
            string existingReward=string.Empty;
            string outcome;
            int guildXp;
            int hallXp;
            int materialAmount;
            var externalAuthorityProofId=string.Empty;
            var externalAuthorityProofHash=string.Empty;
            if(step.RequiresCertifiedBattle)
            {
                var battle=campaign?.Battle;
                var canonicalBattle=historical
                    ?HasCanonicalHistoricalCampaignBattle084(campaign,operation,
                        receipt)
                    :HasCanonicalClaimedCampaignBattle084(campaign,
                        city?.Strategic017H?.Campaign019,operation);
                if(!canonicalBattle||
                   battle==null||
                   string.IsNullOrWhiteSpace(
                        operation.ExistingBattleRewardReceiptId)||
                   !StringComparer.Ordinal.Equals(
                        operation.ExistingBattleRewardReceiptId,
                        battle.Reward.RewardId))return false;
                hash=CanonicalJson.Sha256Hex(new
                {
                    operation.OperationId,step.StepId,battle.FinalStateHash,
                    battle.Reward.RewardId
                });
                authoritativeHash=battle.FinalStateHash;
                existingReward=battle.Reward.RewardId;
                outcome=battle.Outcome.ToString();
                guildXp=4;
                hallXp=4;
                materialAmount=4;
            }
            else
            {
                var worldBoard=CampaignAdventureRules084.IsWorldBoardStep084(step);
                outcome=worldBoard?"WORLD_GATE_BOARD_COMPLETE":"SUCCESS";
                if(worldBoard)
                {
                    if(!TryExpectedWorldGateCompletionProof084(
                           campaign,city,catalog,operation.ChapterId,
                           out var completionProof))return false;
                    var expectedOperationId=completionProof.OperationId;
                    var runtime=playable?.WorldGate023;
                    if(runtime==null||runtime.ActiveOperation!=null||
                       !runtime.CompletedDefinitionIds.Contains(operation.ChapterId)||
                       string.IsNullOrWhiteSpace(expectedOperationId)||
                       !StringComparer.Ordinal.Equals(runtime.LastCheckpointId,
                           "world_gate_023_operation_finalized:"+expectedOperationId))
                        return false;
                    externalAuthorityProofId=expectedOperationId;
                    externalAuthorityProofHash=completionProof.CompletionLedgerHash;
                }
                hash=worldBoard
                    ?CanonicalJson.Sha256Hex(new
                    {
                        operation.OperationId,step.StepId,result=outcome,
                        WorldGateOperationId=externalAuthorityProofId,
                        WorldGateCompletionLedgerHash=externalAuthorityProofHash,
                        operation.CanonicalSeedIdentity
                    })
                    :CanonicalJson.Sha256Hex(new
                    {
                        operation.OperationId,step.StepId,result=outcome,
                        operation.CanonicalSeedIdentity
                    });
                authoritativeHash="NONBATTLE:"+hash;
                guildXp=worldBoard?0:(step.ConsumesOperation?2:0);
                hallXp=worldBoard?0:(step.ConsumesOperation?2:0);
                materialAmount=worldBoard?0:(step.ConsumesOperation?2:0);
            }
            var expectedId="STEPREC020_"+hash.Substring(0,24).ToUpperInvariant();
            var expectedMaterials=blueprint.MaterialIds.Count>0
                ?new[]{new WorldMaterialAmount020(
                    blueprint.MaterialIds[stepIndex%
                        blueprint.MaterialIds.Count],materialAmount)}
                :Array.Empty<WorldMaterialAmount020>();
            if(!StringComparer.Ordinal.Equals(receipt.ReceiptId,expectedId)||
               !StringComparer.Ordinal.Equals(receipt.Outcome,outcome)||
               !StringComparer.Ordinal.Equals(receipt.AuthoritativeHash,authoritativeHash)||
               !StringComparer.Ordinal.Equals(receipt.ExistingEquipmentRewardReceiptId,
                    existingReward)||
               !StringComparer.Ordinal.Equals(receipt.ExternalAuthorityProofId,
                    externalAuthorityProofId)||
               !StringComparer.Ordinal.Equals(receipt.ExternalAuthorityProofHash,
                    externalAuthorityProofHash)||
               receipt.GuildXp!=guildXp||receipt.HallXp!=hallXp||
                !SameMaterials084(receipt.Materials,expectedMaterials))return false;
            error=string.Empty;
            return true;
        }
        static bool ValidateAppliedReceiptLedger084(
            CampaignState campaign,
            GuildCityState017D city,
            CampaignPlayableState020 playable,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,
            ICampaignPlayableCatalog020 catalog)
        {
            if(operation.AppliedReceipts.Count!=operation.AppliedReceiptIds.Count||
               operation.AppliedReceipts.Select(value=>value?.ReceiptId)
                   .Any(string.IsNullOrWhiteSpace)||
               operation.AppliedReceipts.Select(value=>value.ReceiptId)
                   .Distinct(StringComparer.Ordinal).Count()!=
                   operation.AppliedReceipts.Count||
               operation.AppliedReceipts.Select(value=>value.StepId)
                   .Distinct(StringComparer.Ordinal).Count()!=
                   operation.AppliedReceipts.Count||
               !operation.AppliedReceipts.Select(value=>value.ReceiptId)
                   .OrderBy(value=>value,StringComparer.Ordinal)
                   .SequenceEqual(operation.AppliedReceiptIds,StringComparer.Ordinal))
                return false;
            var byStep=operation.AppliedReceipts.ToDictionary(
                value=>value.StepId,value=>value,StringComparer.Ordinal);
            for(var index=0;index<operation.CurrentStepIndex;index++)
            {
                var step=blueprint.Steps[index];
                if(!byStep.TryGetValue(step.StepId,out var receipt)||
                   !campaign.Guild.Development.HasAdventureAuthority(
                       receipt.ReceiptId))return false;
                var canonical=ValidateStepReceipt084(campaign,city,playable,
                    operation,blueprint,catalog,step,index,receipt,true,out _);
                if(!canonical&&
                   InsertedBattleChapters084.Contains(operation.ChapterId)&&
                   index==blueprint.Steps.Count-1&&
                   TryCreateLegacyInsertedResultsReceipt084(operation,blueprint,
                        out var legacyResultsReceipt)&&
                   StringComparer.Ordinal.Equals(receipt.ReceiptId,
                        legacyResultsReceipt.ReceiptId)&&
                   StringComparer.Ordinal.Equals(CanonicalJson.Serialize(receipt),
                        CanonicalJson.Serialize(legacyResultsReceipt))&&
                   campaign.Guild.Development.HasAdventureAuthority(
                        InsertedBattleRecoveryAuthorityId084(operation,
                            legacyResultsReceipt))&&
                   !campaign.Guild.Development.HasClaimedReward(
                        InsertedBattleRecoveryAuthorityId084(operation,
                            legacyResultsReceipt))&&
                   !campaign.Guild.Development.HasClaimedReward(receipt.ReceiptId))
                    canonical=true;
                if(!canonical)return false;
            }
            return true;
        }
        static bool HasCanonicalCampaignEncounter084(
            CampaignProgressState019 progress,
            CampaignPlayableOperationState020 operation,
            EncounterLaunchRequest017D encounter)
        {
            var story=progress?.ActiveOperation;
            return story!=null&&encounter!=null&&story.BattleCommitted&&
                   StringComparer.Ordinal.Equals(story.ChapterId,operation.ChapterId)&&
                   StringComparer.Ordinal.Equals(progress.ActiveChapterId,operation.ChapterId)&&
                   StringComparer.Ordinal.Equals(encounter.RequestId,
                       "CAMPAIGN_ENCOUNTER_"+story.RequestId)&&
                   StringComparer.Ordinal.Equals(encounter.ContractId,operation.ChapterId)&&
                   StringComparer.Ordinal.Equals(encounter.CanonicalSeedIdentity,
                       story.CanonicalSeedIdentity)&&
                   StringComparer.Ordinal.Equals(encounter.ReturnCheckpointId,
                       story.ReturnCheckpointId)&&
                   !string.IsNullOrWhiteSpace(encounter.BattleId);
        }
        static bool MatchesOperationIdentity084(
            CampaignState campaign,
            CampaignProgressState019 progress,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,
            bool allowEarlierProgress)
        {
            var maximum=progress.CampaignProgress;
            // One authored Campaign019 receipt changes campaign progress by far
            // less than this closed-world ceiling (current maximum is 285).
            // The bound also prevents a corrupt int-max save from causing a long scan.
            var minimum=allowEarlierProgress?Math.Max(0,maximum-4096):maximum;
            for(var campaignProgress=minimum;;campaignProgress++)
            {
                var chapterId=operation.ChapterId;
                var identity=OperationIdentity151(campaign,progress,chapterId,blueprint.BlueprintId,campaignProgress);
                var operationId="OP020_"+identity.Substring(0,24).ToUpperInvariant();
                if(StringComparer.Ordinal.Equals(operation.CanonicalSeedIdentity,identity)&&
                   StringComparer.Ordinal.Equals(operation.OperationId,operationId))
                    return true;
                if(campaignProgress==maximum)break;
            }
            return false;
        }
        static string OperationIdentity151(CampaignState campaign,CampaignProgressState019 progress,
            string chapterId,string blueprintId,int campaignProgress)
        {
            // Preserve every pre-restart identity. A confirmed new run has a distinct
            // durable authority; progress totals and prior reward ledgers never reset.
            var legacy=CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,campaign.CampaignSeed,
                chapterId,BlueprintId=blueprintId,CampaignProgress=campaignProgress});
            var run=CampaignRunRecoveryCommands151.ActiveRun(progress);
            return run?.HasCurrentRun==true?CanonicalJson.Sha256Hex(new{
                LegacyIdentity=legacy,RunAuthority151=run.Boundaries.Last().RequestId}):legacy;
        }
        static string CampaignPlayableBeginGrantHash084(CampaignState campaign,
            string operationId,string blueprintId,string chapterId,string worldId,
            string canonicalSeedIdentity,int campaignProgressAtBegin) =>
            CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,campaign.CampaignSeed,operationId,blueprintId,
                chapterId,worldId,canonicalSeedIdentity,campaignProgressAtBegin,
                Authority="CAMPAIGN_PLAYABLE_BEGIN_020_2.1"
            });
        static bool ValidateBeginGrant084(CampaignState campaign,
            CampaignProgressState019 progress,CampaignPlayableState020 playable,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint,bool allowAppliedChapterProgress)
        {
            var grant=playable?.ActiveOperationGrant;
            if(grant==null||campaign==null||progress==null||operation==null||
               blueprint==null||
               !StringComparer.Ordinal.Equals(grant.OperationId,operation.OperationId)||
               !StringComparer.Ordinal.Equals(grant.BlueprintId,operation.BlueprintId)||
               !StringComparer.Ordinal.Equals(grant.ChapterId,operation.ChapterId)||
               !StringComparer.Ordinal.Equals(grant.WorldId,operation.WorldId)||
               !StringComparer.Ordinal.Equals(grant.CanonicalSeedIdentity,
                    operation.CanonicalSeedIdentity)||
               grant.CampaignProgressAtBegin>progress.CampaignProgress||
               (!allowAppliedChapterProgress&&
                grant.CampaignProgressAtBegin!=progress.CampaignProgress))return false;
            var expectedIdentity=OperationIdentity151(campaign,progress,grant.ChapterId,blueprint.BlueprintId,grant.CampaignProgressAtBegin);
            return StringComparer.Ordinal.Equals(grant.CanonicalSeedIdentity,
                       expectedIdentity)&&
                   StringComparer.Ordinal.Equals(grant.OperationId,
                       "OP020_"+expectedIdentity.Substring(0,24).ToUpperInvariant())&&
                   StringComparer.Ordinal.Equals(grant.AuthorityHash,
                       CampaignPlayableBeginGrantHash084(campaign,grant.OperationId,
                           grant.BlueprintId,grant.ChapterId,grant.WorldId,
                           grant.CanonicalSeedIdentity,grant.CampaignProgressAtBegin));
        }
        static bool SameStepLedger084(IReadOnlyList<CampaignStepReceipt020> left,
            IReadOnlyList<CampaignStepReceipt020> right)
        {
            var first=left??Array.Empty<CampaignStepReceipt020>();
            var second=right??Array.Empty<CampaignStepReceipt020>();
            return first.Count==second.Count&&
                   StringComparer.Ordinal.Equals(CanonicalJson.Serialize(first),
                       CanonicalJson.Serialize(second));
        }
        static IReadOnlyList<string> ExpectedAppliedReceiptIds084(
            CampaignState campaign,
            CampaignPlayableOperationState020 operation,
            CampaignBlueprintRule020 blueprint)
        {
            var result=new List<string>();
            for(var index=0;index<operation.CurrentStepIndex;index++)
            {
                var step=blueprint.Steps[index];
                string hash;
                if(step.RequiresCertifiedBattle)
                {
                    var battle=campaign?.Battle;
                    if(battle?.Reward==null||!battle.Reward.Claimed)return Array.Empty<string>();
                    hash=CanonicalJson.Sha256Hex(new
                    {
                        operation.OperationId,step.StepId,battle.FinalStateHash,
                        battle.Reward.RewardId
                    });
                }
                else
                {
                    var outcome=CampaignAdventureRules084.IsWorldBoardStep084(step)
                        ?"WORLD_GATE_BOARD_COMPLETE":"SUCCESS";
                    hash=CanonicalJson.Sha256Hex(new
                    {
                        operation.OperationId,step.StepId,result=outcome,
                        operation.CanonicalSeedIdentity
                    });
                }
                result.Add("STEPREC020_"+hash.Substring(0,24).ToUpperInvariant());
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }
        static bool HasCanonicalClaimedCampaignBattle084(
            CampaignState campaign,
            CampaignProgressState019 progress,
            CampaignPlayableOperationState020 operation)
        {
            var battle=campaign?.Battle;
            var story=progress?.ActiveOperation;
            var receipt=progress?.PendingReceipt;
            return story!=null&&story.BattleCommitted&&receipt!=null&&battle!=null&&
                   battle.Phase==BattlePhase.Resolved&&battle.Reward!=null&&
                   battle.Reward.Claimed&&
                   StringComparer.Ordinal.Equals(story.ChapterId,operation.ChapterId)&&
                   StringComparer.Ordinal.Equals(progress.ActiveChapterId,
                       operation.ChapterId)&&
                   StringComparer.Ordinal.Equals(receipt.RequestId,story.RequestId)&&
                   StringComparer.Ordinal.Equals(receipt.ChapterId,operation.ChapterId)&&
                   StringComparer.Ordinal.Equals(receipt.AuthoritativeResultHash,
                       battle.FinalStateHash)&&
                   StringComparer.Ordinal.Equals(receipt.ExistingEquipmentRewardReceiptId,
                       battle.Reward.RewardId)&&
                   StringComparer.OrdinalIgnoreCase.Equals(receipt.Outcome,
                       battle.Outcome.ToString())&&
                   M2BattleCommandService.HasValidFinalStateHash090(battle)&&
                    campaign.Guild.Development.HasClaimedReward(
                        battle.Reward.RewardId);
        }
        static bool HasCanonicalHistoricalCampaignBattle084(
            CampaignState campaign,
            CampaignPlayableOperationState020 operation,
            CampaignStepReceipt020 receipt)
        {
            var battle=campaign?.Battle;
            return operation!=null&&receipt!=null&&battle!=null&&
                   battle.Phase==BattlePhase.Resolved&&battle.Reward!=null&&
                   battle.Reward.Claimed&&
                   !string.IsNullOrWhiteSpace(operation.ExistingBattleRewardReceiptId)&&
                   StringComparer.Ordinal.Equals(operation.ExistingBattleRewardReceiptId,
                       battle.Reward.RewardId)&&
                   StringComparer.Ordinal.Equals(receipt.ExistingEquipmentRewardReceiptId,
                       battle.Reward.RewardId)&&
                   StringComparer.Ordinal.Equals(receipt.AuthoritativeHash,
                       battle.FinalStateHash)&&
                   StringComparer.OrdinalIgnoreCase.Equals(receipt.Outcome,
                       battle.Outcome.ToString())&&
                   M2BattleCommandService.HasValidFinalStateHash090(battle)&&
                   campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId);
        }
        static bool SameMaterials084(
            IReadOnlyList<WorldMaterialAmount020> first,
            IReadOnlyList<WorldMaterialAmount020> second)
        {
            var left=(first??Array.Empty<WorldMaterialAmount020>())
                .OrderBy(value=>value.MaterialId,StringComparer.Ordinal).ToArray();
            var right=(second??Array.Empty<WorldMaterialAmount020>())
                .OrderBy(value=>value.MaterialId,StringComparer.Ordinal).ToArray();
            if(left.Length!=right.Length)return false;
            for(var index=0;index<left.Length;index++)
                if(!StringComparer.Ordinal.Equals(left[index].MaterialId,
                       right[index].MaterialId)||left[index].Amount!=right[index].Amount)
                    return false;
            return true;
        }
        static IReadOnlyList<WorldMaterialAmount020> MergeMaterials(IReadOnlyList<WorldMaterialAmount020> source,IReadOnlyList<WorldMaterialAmount020> add){var r=new List<WorldMaterialAmount020>(source??Array.Empty<WorldMaterialAmount020>());if(add!=null)for(var i=0;i<add.Count;i++){if(add[i].Amount<=0)continue;var found=false;for(var j=0;j<r.Count;j++)if(r[j].MaterialId==add[i].MaterialId){r[j]=r[j].WithAmount(r[j].Amount+add[i].Amount);found=true;break;}if(!found)r.Add(add[i]);}r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId));return r.AsReadOnly();}
        static CampaignPlayableState020 ReissuePlayable084(
            CampaignPlayableState020 source,
            CampaignPlayableOperationState020 activeOperation,
            WorldGateRuntimeState023 worldGate=null) =>
            new CampaignPlayableState020(
                CampaignPlayableState020.ContentVersion,activeOperation,
                source.UnlockedRepeatableContractIds,
                source.CompletedRepeatableInstanceIds,source.WorldGates,
                source.WorldMaterials,source.CampaignEpilogueUnlocked,
                source.LastCheckpointId,source.Progression022,worldGate??source.WorldGate023,
                source.CreatorAccess028,source.RecruitChronicles025,source.PeopleBonds026,
                null,Array.Empty<CampaignStepReceipt020>());
        static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city,GuildCityStrategicState017H strategic,CampaignProgressState019 progress){var updatedStrategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:progress.LastCheckpointId);var updatedCity=city.With(strategic017H:updatedStrategic,replaceStrategic017H:true,lastCheckpointId:progress.LastCheckpointId);return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(updatedCity),campaign.OpeningFlow));}
    }
}
